using System.Globalization;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class B2ProcessIdentity
{
    [DllImport("libc",EntryPoint="statfs",SetLastError=true)] private static extern int StatFs(string path,IntPtr buffer);
    internal static DirectProcessIdentity Identity(int pid)=>DirectProcessIdentity.Current() with{Pid=pid};
    internal static void RequireLive(int fd,int pid)
    {if(fd<=2 || !DirectPeerIdentity.LiveReference(fd,Identity(pid))) throw new IOException("original process pin lost");}
    internal static void VerifyPeer(int socket,int retained,B2Binding binding,bool controller)
    {
        binding.Validate();Procfs();
        int peer=controller?binding.OwnerPid:binding.ControllerPid;
        if(Environment.ProcessId!=(controller?binding.ControllerPid:binding.OwnerPid)) throw new IOException("local B2 role");
        using var livePeer=DirectPeerIdentity.Verify(socket,Identity(peer),retained);
        RequireLive(retained,peer);
        CheckProcess(binding.ControllerPid,null);CheckProcess(binding.RecoveryPid,binding.ControllerPid);CheckProcess(binding.OwnerPid,binding.RecoveryPid);
        if(!controller && RecoveryInterop.GetParentPid()!=binding.RecoveryPid) throw new IOException("owner reparented");
        RequireLive(livePeer.DangerousGetHandle().ToInt32(),peer);RequireLive(retained,peer);
    }
    internal static void CheckProcess(int pid,int? parent)
    {
        var status=File.ReadAllLines("/proc/"+pid.ToString(CultureInfo.InvariantCulture)+"/status");
        string Value(string key)=>status.Single(s=>s.StartsWith(key+":",StringComparison.Ordinal)).Split(':',2)[1].Trim();
        var uid=Value("Uid").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        var gid=Value("Gid").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        if(uid.Length!=4 || gid.Length!=4 || uid.Any(s=>s!=RecoveryInterop.GetUid().ToString(CultureInfo.InvariantCulture)) ||
            gid.Any(s=>s!=DirectUnix.GetGid().ToString(CultureInfo.InvariantCulture)) || RecoveryInterop.GetUid()==0 || DirectUnix.GetGid()==0 ||
            Value("CapEff").Any(c=>c!='0') || Value("CapPrm").Any(c=>c!='0') ||
            (parent.HasValue && Value("PPid")!=parent.Value.ToString(CultureInfo.InvariantCulture))) throw new IOException("B2 credentials/lineage refused");
        foreach(string ns in new[]{"pid","user","time"})
        {
            var actual=new FileInfo("/proc/"+pid+"/ns/"+ns).LinkTarget;
            if(actual is null || actual!=new FileInfo("/proc/self/ns/"+ns).LinkTarget) throw new IOException("B2 namespace refused");
        }
    }
    internal static void Procfs()
    {
        var buffer=Marshal.AllocHGlobal(256);
        try {if(StatFs("/proc",buffer)!=0 || Marshal.ReadInt64(buffer)!=0x9fa0) throw new IOException("trusted procfs unavailable");}
        finally {Marshal.FreeHGlobal(buffer);}
    }
    internal static SafeFileHandle ReceivePin(ReceivedFdRecord record,int pid)
    {
        if(record.DescriptorCount!=1) throw new IOException("exactly one process reference required");
        int fd=record.BorrowDescriptor(0);RequireLive(fd,pid);
        return new((IntPtr)record.DetachDescriptors()[0],true);
    }
}
