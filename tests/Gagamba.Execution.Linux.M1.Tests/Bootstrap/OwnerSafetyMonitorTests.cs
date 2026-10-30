using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class OwnerSafetyMonitorTests
{
    private sealed class Clock:ITransactionClock {public long NowNanoseconds=>OwnerSafetyMonitor.Now();}
    [Theory][InlineData("recovery")][InlineData("controller")][InlineData("lifeline")][InlineData("stop")]
    public void IndependentLossLatchConsumesCapabilityPublishedAfterLoss(string loss)
    {
        if(!OperatingSystem.IsLinux()) Assert.Skip("Linux independent lifetime monitor required");
        if(Environment.GetEnvironmentVariable("GAGAMBA_INT01_NATIVE_PRIMITIVES")!="1") Assert.Skip("explicit INT01 primitive profile required");
        Assert.True(RecoveryInterop.IsUnprivileged);
        using var helper=Process.Start(new ProcessStartInfo("/bin/sleep","5") {UseShellExecute=false})!;
        var pair=new int[2];Assert.Equal(0,DirectUnix.SocketPair(1,5|DirectUnix.Cloexec|DirectUnix.Nonblock,0,pair));
        using var sender=new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)pair[0],true);
        var current=DirectProcessIdentity.Current();
        var controller=current with {Pid=loss=="controller"?helper.Id:current.Pid};
        var recovery=current with {Pid=loss=="controller"?current.Pid:helper.Id};
        Assert.True(PidFd.TryOpen(controller.Pid,out var c,out _));Assert.True(PidFd.TryOpen(recovery.Pid,out var r,out _));
        var runtime=new ExperimentalRuntimePin("/unexecuted",new string('0',64),"/unexecuted",new string('0',64),"/unexecuted",new string('0',64),"0");
        var now=OwnerSafetyMonitor.Now();
        using var monitor=new OwnerSafetyMonitor(c,r,pair[1],new OwnerDirectIdentity(Guid.NewGuid(),controller,recovery,runtime),now,now+30_000_000_000L);
        try
        {
            if(loss is "recovery" or "controller") {helper.Kill();Assert.True(helper.WaitForExit(1000));}
            else if(loss=="lifeline") sender.Dispose();
            else Assert.True(FdPassing.TrySendMessage(pair[0],[1],out _));
            Assert.True(SpinWait.SpinUntil(()=>monitor.StopLatched,1000),"loss must be latched independently of a request reader");
            var manifest=SampleManifest(); var issuer=Guid.NewGuid();var generation=Guid.NewGuid();
            var identity=new PreparationIdentity(Guid.NewGuid(),issuer,generation,Guid.NewGuid(),ManifestCodec.ComputeDigestBytes(manifest));
            var store=new PreparedTransaction.PreparationStore(issuer,generation,new Clock());
            using var capability=store.Mint(manifest,identity,now+30_000_000_000L,TestClosure.Empty());
            Assert.False(monitor.PublishCapability(capability));Assert.False(capability.IsLive);
            Assert.False(monitor.ControllerAlive);Assert.False(monitor.RecoveryAlive);
        }
        finally {if(!helper.HasExited) helper.Kill();Assert.True(helper.WaitForExit(1000));}
    }
    [Fact] public void RecoveryProtocolHasNoManifestOrReleaseCommand()
    {
        Assert.All(Enum.GetNames<RecoveryMessage>(),name=>Assert.DoesNotContain("Release",name));
        Assert.All(Enum.GetNames<RecoveryMessage>(),name=>Assert.DoesNotContain("Manifest",name));
        Assert.False(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.DomainRecoveryAuthority));
        Assert.False(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.StartAuthority));
        Assert.False(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.DirectBootstrap));
        Assert.True(OwnerProtocolChannel.DirectControl(OwnerProtocolMessageType.ManifestAuthorization));
    }
    [Fact] public void RecoveryOnlyChannelRejectsReleaseAndManifestBeforeAnyIo()
    {
        // The deliberately invalid descriptor proves refusal happens at authority routing, before syscalls.
        using var channel=new OwnerProtocolChannel(123456,Guid.NewGuid(),Guid.NewGuid(),false) {RecoveryOnly=true};
        Assert.False(channel.TrySend(OwnerProtocolMessageType.ReleaseRequest,[1],out var error));
        Assert.Contains("authority role",error);
        Assert.False(channel.TrySend(OwnerProtocolMessageType.ManifestAuthorization,[1],out _));
        Assert.False(channel.TryReceive(OwnerProtocolMessageType.ReleaseRequest,out _,out _));
    }
}
