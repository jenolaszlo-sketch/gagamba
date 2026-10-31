using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class OwnerSafetyDisposalTests
{
    private sealed class Clock : ITransactionClock { public long NowNanoseconds => OwnerSafetyMonitor.Now(); }
    private sealed class Transport : IReleaseWriteChannel
    {
        public int Attempts;
        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        { Attempts++; return ReleasePipeWriteResult.Delivered; }
    }
    [Fact]
    public void DisposalAcceptsDurableStopBeforeClosingMonitor()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("Linux pidfds required");
        if (Environment.GetEnvironmentVariable("GAGAMBA_INT01_NATIVE_PRIMITIVES") != "1") Assert.Skip("explicit native profile required");
        var identity = DirectProcessIdentity.Current();
        Assert.True(PidFd.TryOpen(identity.Pid, out var c, out _));
        Assert.True(PidFd.TryOpen(identity.Pid, out var r, out _));
        var pair = new int[2]; Assert.Equal(0, DirectUnix.SocketPair(1, 5|DirectUnix.Cloexec|DirectUnix.Nonblock, 0, pair));
        using var sender = new SafeFileHandle((IntPtr)pair[0], true);
        var runtime = new ExperimentalRuntimePin("/unexecuted",new string('0',64),"/unexecuted",new string('0',64),"/unexecuted",new string('0',64),"0");
        var now = OwnerSafetyMonitor.Now();
        var monitor = new OwnerSafetyMonitor(c,r,pair[1],new(Guid.NewGuid(),identity,identity,runtime),now,now+30_000_000_000L);
        var manifest = SampleManifest(); var clock = new Clock();
        var store = new PreparedTransaction.PreparationStore(RecoveryTestSupport.Issuer,RecoveryTestSupport.Generation,clock);
        var binding = new PreparationIdentity(RecoveryTestSupport.Execution,RecoveryTestSupport.Issuer,
            RecoveryTestSupport.Generation,RecoveryTestSupport.Preparation,ManifestCodec.ComputeDigestBytes(manifest));
        using var capability = store.Mint(manifest,binding,now+30_000_000_000L,TestClosure.Empty());
        var journal = new OwnershipJournal(new FakeJournalStorage(),"/b4-disposal");
        Assert.True(journal.TryAcquireWriter(out var writer,out _)); using var lease = writer!;
        Assert.True(journal.WriteNext(lease,RecoveryTestSupport.Snapshot(stage:PreparationStage.Prepared,
            preparation:new(binding.ExecutionId,binding.IssuerId,binding.GenerationId,binding.PreparationId,binding.ManifestDigest.ToArray()))).Ok);
        var transport = new Transport();
        var arbiter = new ReleaseArbiter(capability,journal,lease,clock,()=>new(true,true,true,true),transport);
        monitor.BindArbiter(arbiter);
        monitor.Dispose();
        Assert.True(monitor.StopLatched);
        Assert.False(capability.IsLive);
        Assert.NotNull(journal.ReadLatest().Snapshot!.Stop);
        Assert.NotEqual(ReleaseDecisionKind.Released, arbiter.RequestRelease(new(binding,now+10_000_000_000L,now+30_000_000_000L)).Kind);
        Assert.Equal(0,transport.Attempts);
    }
}
