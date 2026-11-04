using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Preparation.Native;

// Actual Release assemblies and production RequireMapped are observed in a
// private, unprivileged copy. This deliberately cannot construct a trusted
// LoadedInstallation or replace S2's root-owned verification.
internal static class B6LoadedRoleProbe
{
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    internal static int Run(string mode,string output)
    {
        var observations=new List<object>();bool mapped=false,mutationDetected=false,unqualified=true;string? reason=null;
        string root=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        try
        {
            if(!OperatingSystem.IsLinux() || !root.StartsWith("/tmp/gagamba-s3-loaded-",StringComparison.Ordinal))throw new IOException("explicit disposable loaded-role copy required");
            var entry=Assembly.GetExecutingAssembly();var m1=typeof(LoadedInstallation).Assembly;var contract=Assembly.Load("Gagamba.Execution");
            var required=new[]{entry,m1,contract};
            var check=typeof(InstalledFileSystem).GetMethod("RequireMapped",BindingFlags.NonPublic|BindingFlags.Static)!;
            void Verify(string path)
            {
                using var file=File.OpenRead(path);int fd=file.SafeFileHandle.DangerousGetHandle().ToInt32();
                if(PreparationInterop.StatFd(fd,out var stat)!=0)throw new IOException("actual fstat");
                var snapshot=new InstalledFileSystem.Snapshot(stat.Device,stat.Inode,stat.Mode,0,0,stat.LinkCount,stat.Size,stat.Blocks512,0,0,0,0);
                try{check.Invoke(null,[path,snapshot]);}catch(TargetInvocationException e) when(e.InnerException is not null){throw e.InnerException;}
                observations.Add(new{Path=path,Sha256=Hash(path),stat.Device,stat.Inode,stat.Size,Mapped=true});
            }
            foreach(var a in required)
            {
                if(a.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration!="Release")throw new IOException("actual Release assembly required");
                Verify(a.Location);
            }
            mapped=true;
            string target=mode=="entry-replaced"?entry.Location:m1.Location;
            string before=Hash(target);
            if(mode is "entry-replaced" or "dependency-replaced")
            {
                byte[] bytes=File.ReadAllBytes(target);bytes[^1]^=1;File.WriteAllBytes(target+".replacement",bytes);File.Move(target+".replacement",target,true);
                if(Hash(target)==before)throw new IOException("mutation did not change bytes");
                try{Verify(target);}catch(InvalidDataException e){mutationDetected=true;reason=e.Message;}
            }
            else if(mode is "same-filename" or "wrong-location")
            {
                string directory=Path.Combine(root,"foreign");Directory.CreateDirectory(directory);string shadow=Path.Combine(directory,Path.GetFileName(target));
                var bytes=File.ReadAllBytes(target);if(mode=="same-filename")bytes[^1]^=1;File.WriteAllBytes(shadow,bytes);
                try{Verify(shadow);}catch(InvalidDataException e){mutationDetected=true;reason=e.Message;}
            }
            else if(mode=="missing-component")
            {
                File.Delete(target);
                try{Verify(target);}catch(FileNotFoundException e){mutationDetected=true;reason=e.GetType().Name;}
            }
            else if(mode=="unexpected-assembly")
            {
                string extra=Path.Combine(root,"extra");Directory.CreateDirectory(extra);string copy=Path.Combine(extra,Path.GetFileName(m1.Location));File.Copy(m1.Location,copy);
                var context=new AssemblyLoadContext("isolated-extra",true);var foreign=context.LoadFromAssemblyPath(copy);
                mutationDetected=AppDomain.CurrentDomain.GetAssemblies().Count(a=>a.GetName().Name==m1.GetName().Name)>1 && foreign.Location!=m1.Location;
                observations.Add(new{UnexpectedAssembly=foreign.GetName().Name,foreign.Location,DuplicateObserved=mutationDetected,Scope="actual inventory fact; full production allowlist remains Q"});
                context.Unload();
            }
            else if(mode=="dynamic-assembly")
            {
                var dynamic=AssemblyBuilder.DefineDynamicAssembly(new("S3.Isolated.Unexpected"),AssemblyBuilderAccess.Run);
                _=dynamic.DefineDynamicModule("probe").DefineType("UnexpectedLoadedType").CreateType();
                mutationDetected=dynamic.IsDynamic && AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.IsDynamic && a.GetName().Name==dynamic.GetName().Name);
                observations.Add(new{DynamicObserved=mutationDetected,Scope="actual inventory fact; full production allowlist remains Q"});
            }
            else if(mode!="matching")throw new IOException("unknown observation mode");
            try{_=LoadedInstallation.Verify("provider");unqualified=false;}catch(InvalidDataException e){observations.Add(new{S2Refusal=e.Message});}
            bool success=mapped && unqualified && (mode=="matching" || mutationDetected);
            File.WriteAllText(output,JsonSerializer.Serialize(new{Scope="Native mapping and assembly observations; no installed trust, full closure or loader qualification",Mode=mode,PositiveMapped=mapped,MutationDetected=mutationDetected,TrustedInstallationReturned=!unqualified,Reason=reason,Observations=observations,Passed=success},new JsonSerializerOptions{WriteIndented=true}));
            return success?0:5;
        }
        catch(Exception e)
        {
            File.WriteAllText(output,JsonSerializer.Serialize(new{Mode=mode,Passed=false,Error=e.ToString(),Observations=observations}));return 4;
        }
    }
}
