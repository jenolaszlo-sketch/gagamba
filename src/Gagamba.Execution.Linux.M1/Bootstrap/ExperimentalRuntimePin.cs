using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Trusted experimental inputs, not B6 installation attestation. Pin the complete runtime
// inventory and hostfxr inventory rather than accepting only the entry DLL's hash.
internal sealed record ExperimentalRuntimePin(string HostPath,string HostSha256,string RuntimeDirectory,
    string RuntimeInventorySha256,string HostFxrDirectory,string HostFxrInventorySha256,string RuntimeVersion)
{
    internal void VerifyCurrent()
    {
        if(!OperatingSystem.IsLinux() || !RecoveryInterop.IsUnprivileged
            || Environment.Version.ToString()!=RuntimeVersion
            || Path.GetFullPath(RuntimeEnvironment.GetRuntimeDirectory()).TrimEnd('/')!=RuntimeDirectory.TrimEnd('/'))
            throw new IOException("Experimental runtime/version refused");
        VerifyLaunch();
    }
    internal void VerifyLaunch()
    {
        if(!OperatingSystem.IsLinux() || !Protected(HostPath,false,rootOnly:true)
            || !Protected(RuntimeDirectory,true,rootOnly:true) || !Protected(HostFxrDirectory,true,rootOnly:true)
            || !Restricted.B3Qualification.VerifySha256(HostPath,HostSha256)
            || Inventory(RuntimeDirectory)!=RuntimeInventorySha256 || Inventory(HostFxrDirectory)!=HostFxrInventorySha256)
            throw new IOException("Experimental runtime closure refused");
    }
    internal static string Inventory(string directory)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var files=Directory.GetFiles(directory,"*",SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        if(files.Length is <1 or >2048) throw new IOException("Runtime inventory bound refused");
        foreach(var file in files)
        {
            if(!Protected(file,false,rootOnly:true)) throw new IOException("Runtime dependency mutable/untrusted");
            using var stream=File.OpenRead(file);
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(directory,file)+"\0"));
            hash.AppendData(SHA256.HashData(stream));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    internal static void VerifyPrivateDirectory(string directory)
    {
        if(!Protected(directory,true,rootOnly:false)) throw new IOException("Experimental data directory unprotected");
    }
    internal static void VerifyPrivateClosure(PinnedExecutable executable)
    {
        if(!Protected(executable.Directory,true,rootOnly:false)) throw new IOException("Experimental launch directory unprotected");
        foreach(var name in executable.Sha256.Keys)
            if(!Protected(Path.Combine(executable.Directory,name),false,rootOnly:false))
                throw new IOException("Experimental dependency unprotected");
    }
    private static bool Protected(string path,bool directory,bool rootOnly)
    {
        if(!OperatingSystem.IsLinux() || !Path.IsPathFullyQualified(path) || !AncestorsProtected(path)
            || PreparationInterop.OpenDirectory("/",out var root)!=0) return false;
        using var rootHandle=new SafeFileHandle((IntPtr)root,true);
        if(PreparationInterop.OpenRelative(root,path.TrimStart('/'),PreparationInterop.O_RDONLY|PreparationInterop.O_CLOEXEC
            |(directory?PreparationInterop.O_DIRECTORY:0),PreparationInterop.RequiredResolve,out var fd)!=0) return false;
        using var handle=new SafeFileHandle((IntPtr)fd,true);
        if(PreparationInterop.StatFd(fd,out var stat)!=0 || (stat.Mode&0x12)!=0) return false; // no group/other write
        if(!TryOwner(fd,out var owner)) return false;
        return rootOnly ? owner==0 : owner==RecoveryInterop.GetUid() && (!directory || (stat.Mode&0x1ff)==0x1c0);
    }
    [DllImport("libc",EntryPoint="fstat",SetLastError=true)]
    private static extern int StatX64(int fd,out PreparationInterop.StatX64 stat);
    [DllImport("libc",EntryPoint="fstat",SetLastError=true)]
    private static extern int StatArm64(int fd,out PreparationInterop.StatArm64 stat);
    internal static bool TryOwner(int fd,out uint owner)
    {
        owner=uint.MaxValue;
        if(RuntimeInformation.ProcessArchitecture==Architecture.X64 && StatX64(fd,out var x)==0) {owner=x.StUid;return true;}
        if(RuntimeInformation.ProcessArchitecture==Architecture.Arm64 && StatArm64(fd,out var a)==0) {owner=a.StUid;return true;}
        return false;
    }
    private static bool AncestorsProtected(string path)
    {
        if(PreparationInterop.OpenDirectory("/",out var root)!=0) return false;
        using var rootHandle=new SafeFileHandle((IntPtr)root,true);
        for(var directory=Path.GetDirectoryName(path);!string.IsNullOrEmpty(directory) && directory!="/";directory=Path.GetDirectoryName(directory))
        {
            if(PreparationInterop.OpenRelative(root,directory.TrimStart('/'),PreparationInterop.O_RDONLY|PreparationInterop.O_DIRECTORY
                |PreparationInterop.O_CLOEXEC,PreparationInterop.RequiredResolve,out var fd)!=0) return false;
            using var handle=new SafeFileHandle((IntPtr)fd,true);
            if(PreparationInterop.StatFd(fd,out var stat)!=0 || !TryOwner(fd,out var owner)
                || (owner!=0 && owner!=RecoveryInterop.GetUid())
                || ((stat.Mode&0x12)!=0 && !(owner==0 && (stat.Mode&0x200)!=0))) return false;
        }
        return true;
    }
    internal ProcessStartInfo StartInfo(string assembly)
    {
        VerifyLaunch();
        var start=new ProcessStartInfo(HostPath) {UseShellExecute=false,RedirectStandardInput=true,
            RedirectStandardOutput=true,RedirectStandardError=true};
        start.Environment.Clear();
        start.Environment["PATH"]="/usr/bin:/bin";
        start.Environment["DOTNET_ROOT"]=Path.GetDirectoryName(HostPath)!;
        start.Environment["DOTNET_MULTILEVEL_LOOKUP"]="0";
        start.Environment["DOTNET_EnableDiagnostics"]="0";
        // Runtime version is verified inside BOTH helpers before any native authority is allocated.
        start.Environment["DOTNET_ROLL_FORWARD"]="LatestPatch";
        start.ArgumentList.Add(assembly); return start;
    }
}
