using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Bootstrap;

namespace Gagamba.Execution.Linux.M1.Installed;

// No injectable trust source. Every actor independently re-enters S2's fixed-root verifier.
internal sealed class LoadedInstallation
{
    internal QualifiedLauncher.InstalledPlan? OwnerLaunch {get;set;}
    internal InstalledResult Selection {get;}
    internal InstallationIdentity Identity => Selection.Identity!;
    internal InstalledManifest Manifest => Selection.Manifest!;
    internal string Root => "/opt/gagamba/installations/"+Identity.Generation;
    private LoadedInstallation(InstalledResult selection) {Selection=selection;}
    internal static LoadedInstallation Verify(string role,InstallationIdentity? expected=null)
    {
        var state=InstalledFileSystem.VerifyActive();
        if(state.State!=InstalledState.Qualified || state.Identity is null || state.Manifest is null || state.QualificationId is null)
            throw new InvalidDataException("qualified installed selection unavailable");
        var verified=new LoadedInstallation(state);
        if(expected is not null && expected!=verified.Identity) throw new InvalidDataException("installation selection changed");
        verified.VerifyLoaded(role);return verified;
    }
    internal void Revalidate()
    {
        var next=InstalledFileSystem.VerifyActive();
        if(next.State!=InstalledState.Qualified || next.Identity!=Identity || next.QualificationId!=Selection.QualificationId)
            throw new InvalidDataException("original S2 selection no longer qualified");
    }
    internal PinnedExecutable Executable(string role)
    {
        if(role is not ("owner" or "recovery")) throw new InvalidDataException("executable role");
        return new(Root+"/"+role,Manifest.Artifacts.Where(a=>a.Path.StartsWith(role+"/",StringComparison.Ordinal))
            .ToDictionary(a=>Path.GetFileName(a.Path),a=>a.Sha256,StringComparer.Ordinal));
    }
    internal ExperimentalRuntimePin Runtime()
    {
        var p=Manifest.Platform;string version=InstallJson.String(p,"dotnetRuntime");
        return new("/usr/lib/dotnet/dotnet",InstallJson.Hex(p,"dotnetHostSha256"),
            "/usr/lib/dotnet/shared/Microsoft.NETCore.App/"+version,InstallJson.Hex(p,"runtimeInventorySha256").ToUpperInvariant(),
            "/usr/lib/dotnet/host/fxr/"+version,InstallJson.Hex(p,"hostFxrInventorySha256").ToUpperInvariant(),version);
    }
    private void VerifyLoaded(string role)
    {
        if(role is not ("provider" or "recovery" or "owner")) throw new InvalidDataException("loaded role");
        // Force both private dependencies before taking the assembly inventory.
        var m1=typeof(LoadedInstallation).Assembly;
        var contract=Assembly.Load("Gagamba.Execution");
        var entry=Assembly.GetEntryAssembly() ?? throw new InvalidDataException("entry absent");
        var required=new[]{m1,contract}.Concat(role=="provider"?[]:new[]{entry}).ToArray();
        var runtime=Runtime();runtime.VerifyCurrent();
        var paths=Manifest.Artifacts.Where(a=>a.Path.StartsWith(role+"/",StringComparison.Ordinal)).ToArray();
        foreach(var artifact in paths) InstalledFileSystem.VerifyConsumedFile(Root+"/"+artifact.Path,artifact.Sha256,artifact.Length,
            artifact.Kind=="managed");
        foreach(var assembly in required)
        {
            var path=assembly.Location;
            var artifact=paths.SingleOrDefault(a=>Root+"/"+a.Path==path);
            if(assembly.IsDynamic || string.IsNullOrEmpty(path) || artifact is null || artifact.Kind!="managed")
                throw new InvalidDataException("actual role assembly outside installed closure");
            var configuration=assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
            if(configuration!="Release") throw new InvalidDataException("loaded instrumented assembly");
        }
        var requiredNames=required.Select(a=>a.GetName().Name).ToHashSet(StringComparer.Ordinal);
        var loaded=AppDomain.CurrentDomain.GetAssemblies();
        foreach(var assembly in loaded)
        {
            if(assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) throw new InvalidDataException("uncertain loaded assembly");
            if(required.Contains(assembly)) continue;
            // The provider's installed role contains these two assemblies. The trusted
            // embedding controller application is outside that role, not an attested helper.
            if(role=="provider" && !requiredNames.Contains(assembly.GetName().Name) &&
                !File.Exists(Path.Combine(runtime.RuntimeDirectory,assembly.GetName().Name+".dll"))) continue;
            if(requiredNames.Contains(assembly.GetName().Name) || !assembly.Location.StartsWith(runtime.RuntimeDirectory+"/",StringComparison.Ordinal))
                throw new InvalidDataException("foreign or duplicate loaded assembly");
            InstalledFileSystem.VerifyPlatformConsumption(assembly.Location);
        }
        if(role!="provider")
        {
            string prefix=Root+"/"+role+"/Gagamba.Execution.Linux.M1."+(role=="owner"?"Owner":"Recovery");
            var deps=(AppContext.GetData("APP_CONTEXT_DEPS_FILES") as string ?? "").Split(';',StringSplitOptions.RemoveEmptyEntries);
            if(!deps.Contains(prefix+".deps.json",StringComparer.Ordinal)) throw new InvalidDataException("actual deps selection");
            var args=System.Text.Encoding.UTF8.GetString(File.ReadAllBytes("/proc/self/cmdline")).Split('\0');
            if(!HasPair(args,"--runtimeconfig",prefix+".runtimeconfig.json") || !HasPair(args,"--depsfile",prefix+".deps.json"))
                throw new InvalidDataException("actual runtimeconfig selection");
        }
        if(new FileInfo("/proc/self/exe").LinkTarget!=runtime.HostPath) throw new InvalidDataException("actual host path");
        InstalledFileSystem.VerifyConsumedFile(runtime.HostPath,runtime.HostSha256,null,true);
        InstalledFileSystem.VerifyConsumedFile("/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2",InstallJson.Hex(Manifest.Platform,"loaderSha256"),null,true);
        foreach(string path in InstalledFileSystem.MappedPaths())
        {
            if(path.StartsWith(Root+"/",StringComparison.Ordinal) || path==runtime.HostPath || path=="/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2") continue;
            // Runtime/hostfxr content is covered by S2's inventory. Native OS-package closure
            // is protected and observed here; its full B6-T20a qualification remains separate.
            if(!path.StartsWith(runtime.RuntimeDirectory+"/",StringComparison.Ordinal) &&
                !path.StartsWith(runtime.HostFxrDirectory+"/",StringComparison.Ordinal) &&
                !path.StartsWith("/usr/lib/x86_64-linux-gnu/",StringComparison.Ordinal))
                throw new InvalidDataException("foreign loaded mapping");
            InstalledFileSystem.VerifyPlatformConsumption(path);
        }
        Revalidate();
    }
    private static bool HasPair(string[] args,string key,string value)
    {int i=Array.IndexOf(args,key);return i>=0 && i+1<args.Length && args[i+1]==value && args.Count(s=>s==key)==1;}
}

internal static partial class InstalledFileSystem
{
    // Required managed images are checked individually with RequireMapped, including
    // read-only IL mappings. This additional inventory concerns persisted native code.
    // CLR JIT/IPC anonymous memory is not a disk artifact or a new identity source.
    internal static bool ExecutableFileMapping(string line,out string path)
    {
        var p=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);path=string.Empty;
        if(p.Length<6 || !p[1].Contains('x') || !p[5].StartsWith('/') || p[5].StartsWith("/memfd:",StringComparison.Ordinal)) return false;
        path=string.Join(" ",p.Skip(5));return true;
    }
    internal static IEnumerable<string> MappedPaths()=>File.ReadLines("/proc/self/maps")
        .Select(line=>ExecutableFileMapping(line,out var path)?path:null).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
    internal static void VerifyPlatformConsumption(string path)
    {
        using var dir=OpenAbsoluteDirectory(Path.GetDirectoryName(path)!,true);
        using var file=OpenRegular(dir.Value,Path.GetFileName(path),true,64*1024*1024,0,0);
        InstallJson.Require((file.Snapshot.Permissions&0x12)==0,"writable consumed runtime");
        RequireMapped(path,file.Snapshot);Stable(file.Value,file.Snapshot,path);Reacquire(dir.Value,Path.GetFileName(path),file.Snapshot,false);
    }
    internal static void VerifyConsumedFile(string path,string digest,long? size,bool mapped)
    {
        using var dir=OpenAbsoluteDirectory(Path.GetDirectoryName(path)!,true);
        using var file=OpenRegular(dir.Value,Path.GetFileName(path),true,64*1024*1024,0,0);
        InstallJson.Require((file.Snapshot.Permissions&0x12)==0 && (!size.HasValue || file.Snapshot.Size==size.Value),"consumed file metadata");
        using var handle=new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)file.Value,false);
        using var stream=new FileStream(handle,FileAccess.Read);
        InstallJson.Require(Convert.ToHexStringLower(SHA256.HashData(stream))==digest,"consumed bytes");
        if(mapped) RequireMapped(path,file.Snapshot);
        Stable(file.Value,file.Snapshot,path);Reacquire(dir.Value,Path.GetFileName(path),file.Snapshot,false);
    }
    private static void RequireMapped(string path,Snapshot stat)
    {
        bool found=false;
        foreach(var line in File.ReadLines("/proc/self/maps"))
        {
            var p=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(p.Length<6 || string.Join(" ",p.Skip(5))!=path) continue;
            var dev=p[3].Split(':');
            if(dev.Length!=2 || !ulong.TryParse(p[4],out var inode)) throw new InvalidDataException("mapping identity grammar");
            ulong major=Convert.ToUInt64(dev[0],16),minor=Convert.ToUInt64(dev[1],16);
            ulong device=(minor&0xff)|((major&0xfff)<<8)|((minor&~0xffUL)<<12)|((major&~0xfffUL)<<32);
            InstallJson.Require(device==stat.Dev && inode==stat.Ino,"mapped inode differs");found=true;
        }
        InstallJson.Require(found,"required file not mapped");
    }
}
