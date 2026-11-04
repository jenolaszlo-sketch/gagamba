using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

[Collection("S3NativeProcessState")]
public sealed class InstallationTransferNativeTests
{
    [DllImport("libc",EntryPoint="pipe2",SetLastError=true)] private static extern int Pipe(int[] fds,int flags);
    [Theory][InlineData("uid")][InlineData("gid")][InlineData("pidns")][InlineData("userns")]
    public void KernelPeerFactsCannotBeReplacedByClaimedCredentialsOrNamespaces(string field)
    {
        if(!OperatingSystem.IsLinux())Assert.Skip("Linux kernel peer facts required");
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE") is null)Assert.Skip("explicit S3 native profile required");
        var pair=QualifiedLauncher.Pair();using var first=new SafeFileHandle((IntPtr)pair.Parent,true);using var second=new SafeFileHandle((IntPtr)pair.Child,true);
        Assert.True(PidFd.TryOpen(Environment.ProcessId,out int original,out _));using var pin=new SafeFileHandle((IntPtr)original,true);
        var expected=DirectProcessIdentity.Current();using(var valid=DirectPeerIdentity.Verify(pair.Parent,expected,original))Assert.True(PreparationInterop.HasCloexec(valid.DangerousGetHandle().ToInt32()));
        var wrong=field switch{"uid"=>expected with{Uid=expected.Uid+1},"gid"=>expected with{Gid=expected.Gid+1},
            "pidns"=>expected with{PidNamespace="pid:[0]"},_=>expected with{UserNamespace="user:[0]"}};
        Assert.ThrowsAny<Exception>(()=>{using var refused=DirectPeerIdentity.Verify(pair.Parent,wrong,original);});
        Assert.True(DirectPeerIdentity.LiveReference(original,expected));
    }
    [Theory]
    [InlineData(11,"original")][InlineData(38,"original")]
    [InlineData(11,"file")][InlineData(38,"file")][InlineData(11,"pipe")][InlineData(38,"pipe")]
    [InlineData(11,"socket")][InlineData(38,"socket")][InlineData(11,"dead")][InlineData(38,"dead")]
    [InlineData(11,"zero")][InlineData(38,"zero")][InlineData(11,"two")][InlineData(38,"two")]
    [InlineData(11,"unrelated")][InlineData(38,"unrelated")]
    [InlineData(11,"overflow")][InlineData(38,"overflow")]
    public void Actual11And38ReferencesAreTypedLiveExclusiveAndDisposed(int kind,string mutation)
    {
        if(!OperatingSystem.IsLinux())Assert.Skip("non-root Linux descriptor qualification required");
        if(Environment.GetEnvironmentVariable("GAGAMBA_S3_EVIDENCE_FIXTURE") is null)Assert.Skip("explicit native S3 profile required");
        Assert.True(RecoveryInterop.IsUnprivileged);
        var pair=QualifiedLauncher.Pair();using var send=new SafeFileHandle((IntPtr)pair.Parent,true);using var receive=new SafeFileHandle((IntPtr)pair.Child,true);
        Assert.True(PidFd.TryOpen(Environment.ProcessId,out int original,out _));using var originalPin=new SafeFileHandle((IntPtr)original,true);
        using var child=mutation is "dead" or "unrelated"?Process.Start(new ProcessStartInfo("/bin/sleep","20"){UseShellExecute=false}):null;
        int other=-1;SafeFileHandle? otherPin=null;FileStream? regular=null;var temporary=new List<SafeFileHandle>();
        string path=Path.Combine(Path.GetTempPath(),"gagamba-s3-reference-"+Guid.NewGuid().ToString("N"));
        try
        {
            int candidate=original;
            if(child is not null)
            {
                Assert.True(PidFd.TryOpen(child.Id,out other,out _));otherPin=new((IntPtr)other,true);candidate=other;
                if(mutation=="dead"){child.Kill();Assert.True(child.WaitForExit(3000));}
            }
            if(mutation=="file") {File.WriteAllText(path,"harmless non-authority sentinel");regular=File.OpenRead(path);candidate=regular.SafeFileHandle.DangerousGetHandle().ToInt32();}
            if(mutation=="pipe")
            {var ends=new int[2];Assert.Equal(0,Pipe(ends,0x80000|0x800));foreach(int fd in ends)temporary.Add(new((IntPtr)fd,true));candidate=ends[1];}
            if(mutation=="socket")
            {var extra=QualifiedLauncher.Pair();temporary.Add(new((IntPtr)extra.Parent,true));temporary.Add(new((IntPtr)extra.Child,true));candidate=extra.Parent;}
            int[] rights=mutation=="zero"?[]:mutation=="two"?[original,original]:mutation=="overflow"?Enumerable.Repeat(original,253).ToArray():[candidate];
            var b=kind==11?new B2Binding(InstallationWireTests.Session,1,2,Environment.ProcessId,InstallationDeadline.Create()):
                new B2Binding(InstallationWireTests.Session,Environment.ProcessId,1,2,InstallationDeadline.Create());
            byte[] payload=InstallationWire.Binding(b);
            byte[] frame=kind==11?new RecoveryWire(InstallationWireTests.Execution,InstallationWireTests.Lease,2,false,false).Create(RecoveryMessage.B2OwnerBound,payload):
                new OwnerProtocolSequencer(InstallationWireTests.Execution,InstallationWireTests.Lease).Create(OwnerProtocolMessageType.B2EchoBootstrap,payload);
            Assert.True(rights.Length==0?FdPassing.TrySendMessage(pair.Parent,frame,out _):FdPassing.TrySendTransfer(pair.Parent,rights,frame,out _));
            if(mutation=="overflow")
            {
                static int Pidfds()=>Directory.GetFiles("/proc/self/fd").Count(p=>new FileInfo(p).LinkTarget=="anon_inode:[pidfd]");
                int before=Pidfds();Assert.False(FdPassing.TryReceiveRecord(pair.Child,1000,out var truncated,out var truncation));
                Assert.Null(truncated);Assert.Contains("MSG_CTRUNC",truncation);Assert.Equal(before,Pidfds());
                Assert.True(DirectPeerIdentity.LiveReference(original,DirectProcessIdentity.Current()));return;
            }
            Assert.True(FdPassing.TryReceiveRecord(pair.Child,1000,out var record,out var error),error);
            int[] copies;
            using(record!)
            {
                copies=Enumerable.Range(0,record!.DescriptorCount).Select(record.BorrowDescriptor).ToArray();
                Assert.All(copies,fd=>Assert.True(PreparationInterop.HasCloexec(fd)));
                Assert.Equal(b,InstallationWire.ReadBinding(record.Payload[48..]));
                if(mutation=="original")
                {using var accepted=B2ProcessIdentity.ReceivePin(record,Environment.ProcessId);Assert.True(DirectPeerIdentity.LiveReference(accepted.DangerousGetHandle().ToInt32(),DirectProcessIdentity.Current()));}
                else Assert.Throws<IOException>(()=>B2ProcessIdentity.ReceivePin(record,Environment.ProcessId));
            }
            Assert.All(copies,fd=>Assert.False(PreparationInterop.HasCloexec(fd)));
            Assert.True(PreparationInterop.HasCloexec(original));Assert.True(DirectPeerIdentity.LiveReference(original,DirectProcessIdentity.Current()));
        }
        finally
        {
            if(child is not null && !child.HasExited){child.Kill();Assert.True(child.WaitForExit(3000));}
            otherPin?.Dispose();regular?.Dispose();foreach(var handle in temporary)handle.Dispose();if(File.Exists(path))File.Delete(path);
        }
    }
}
