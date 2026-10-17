// WP05 release arbitration implementation tests: durable intent ordering,
// single-use capability, one release write, stop/release winner, transport
// mapping, and deterministic crash-window barriers. No target is executed.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class ReleaseArbiterTests
{
    private sealed class FakeClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; } = 1_000;
    }

    private sealed class FakeTransport : IReleaseWriteChannel
    {
        public FakeTransport(ReleasePipeWriteResult result) => Result = result;

        public ReleasePipeWriteResult Result { get; }
        public int Attempts { get; private set; }
        public byte[] LastFrame { get; private set; } = Array.Empty<byte>();

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            LastFrame = frame.ToArray();
            return Result;
        }
    }

    private sealed class CrashException : Exception
    {
    }

    private sealed class Harness : IDisposable
    {
        public Harness(ReleasePipeWriteResult result = ReleasePipeWriteResult.Delivered)
        {
            Clock = new FakeClock();
            var manifest = SampleManifest();
            Identity = new PreparationIdentity(
                RecoveryTestSupport.Execution,
                RecoveryTestSupport.Issuer,
                RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation,
                ManifestCodec.ComputeDigestBytes(manifest));
            Store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = Store.Mint(manifest, Identity, 1_000_000_000, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, "/release");
            Assert.True(Journal.TryAcquireWriter(out var lease, out var error), error);
            Lease = lease!;
            var preparation = new RecordedPreparation(
                Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                Identity.PreparationId, Identity.ManifestDigest.ToArray());
            var seed = RecoveryTestSupport.Snapshot(
                stage: PreparationStage.Prepared, preparation: preparation);
            Assert.True(Journal.WriteNext(Lease, seed).Ok);
            Transport = new FakeTransport(result);
            Arbiter = new ReleaseArbiter(
                Capability, Journal, Lease, Clock, () => Prerequisites, Transport);
        }

        public FakeClock Clock { get; }
        public PreparationIdentity Identity { get; }
        public PreparedTransaction.PreparationStore Store { get; }
        public PreparedTransaction Capability { get; }
        public FakeJournalStorage Storage { get; }
        public OwnershipJournal Journal { get; }
        public JournalWriterLease Lease { get; }
        public FakeTransport Transport { get; }
        public ReleaseArbiter Arbiter { get; }
        public ReleasePrerequisites Prerequisites { get; set; } = new(true, true, true, true);
        public long SetupDeadline { get; set; } = 10_000;
        public long OverallDeadline { get; set; } = 30_000;

        public ReleaseRequest Request => new(Identity, SetupDeadline, OverallDeadline);

        public OwnershipSnapshot Latest()
        {
            var read = Journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            return read.Snapshot!;
        }

        public void Dispose()
        {
            Capability.Dispose();
            Lease.Dispose();
        }
    }

    [Fact]
    public void ValidReleaseWritesExactlyOnceAndPersistsCommit()
    {
        using var h = new Harness();
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.Delivered, decision.Delivery);
        Assert.True(decision.DurableCommitted);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(CoordinatorProtocol.ReleaseFrameBytes, h.Transport.LastFrame.Length);
        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, latest.Release.DeliveryResult);

        var duplicate = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(latest.Sequence, h.Latest().Sequence);
    }

    [Fact]
    public void MissingPrerequisitesRefuseBeforeIntent()
    {
        using var h = new Harness();
        h.Prerequisites = new ReleasePrerequisites(false, true, true, true);
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
    }

    [Fact]
    public void StalePrerequisiteAfterIntentRefusesAtGateConservatively()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name =>
        {
            if (name == "after-intent")
            {
                h.Prerequisites = new ReleasePrerequisites(true, false, true, true);
            }
        };
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void StopFirstForbidsReleaseWithoutIntent()
    {
        using var h = new Harness();
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        var latest = h.Latest();
        Assert.NotNull(latest.Stop);
        Assert.Null(latest.Release);
    }

    [Fact]
    public void StopWinsAfterDurableIntentBeforeGate()
    {
        using var h = new Harness();
        StopDecision? stop = null;
        h.Arbiter.TestBarrier = name =>
        {
            if (name == "after-intent")
            {
                stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
            }
        };
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.NotNull(stop);
        Assert.True(stop!.DurableStop);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.False(h.Capability.IsLive);
        var latest = h.Latest();
        Assert.NotNull(latest.Release);
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
        Assert.NotNull(latest.Stop);
    }

    [Fact]
    public void StopAfterReleasePreservesCommittedHistory()
    {
        using var h = new Harness();
        Assert.Equal(ReleaseDecisionKind.Released, h.Arbiter.RequestRelease(h.Request).Kind);
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted && stop.DurableStop);
        Assert.True(stop.ReleaseAlreadyCommitted);
        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.NotNull(latest.Stop);
    }

    [Fact]
    public void TransportFailuresRemainConservativelyCommitted()
    {
        foreach (var result in new[]
        {
            ReleasePipeWriteResult.WouldBlock,
            ReleasePipeWriteResult.BrokenPipe,
            ReleasePipeWriteResult.Partial,
            ReleasePipeWriteResult.Unknown,
        })
        {
            using var h = new Harness(result);
            var decision = h.Arbiter.RequestRelease(h.Request);
            Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
            Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
            Assert.Equal(1, h.Transport.Attempts);
            Assert.True(decision.DurableCommitted);
            var latest = h.Latest();
            Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
            Assert.NotEqual(DeliveryResult.NotAttempted, latest.Release.DeliveryResult);
            Assert.Equal(
                result == ReleasePipeWriteResult.Unknown
                    || result == ReleasePipeWriteResult.Delivered,
                !decision.LiveExecutionExcluded);
        }
    }

    [Fact]
    public void CrashAfterIntentLeavesIntentAndNoWrite()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name => { if (name == "after-intent") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
        Assert.True(h.Capability.IsLive);
    }

    [Fact]
    public void CrashAfterConsumeBeforeWriteLeavesIntentAndConsumedCapability()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name => { if (name == "after-consume") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void CrashAfterWriteBeforeCommittedLeavesIntentAndDeliveryUnrecorded()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name => { if (name == "after-write") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void CrashAfterCommittedPreservesCommittedEvidence()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name => { if (name == "after-committed") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));
        Assert.Equal(1, h.Transport.Attempts);
        var latest = h.Latest();
        Assert.NotNull(latest.Release);
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, latest.Release.DeliveryResult);
    }

    [Fact]
    public void FailedIntentPersistenceForbidsRelease()
    {
        using var h = new Harness();
        h.Storage.FailAt("write", 1);
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.True(h.Capability.IsLive);
    }

    [Fact]
    public void DuplicateStopIsIdempotent()
    {
        using var h = new Harness();
        var first = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        var second = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.True(second.Duplicate);
        Assert.Equal(first.StopDeadlineNanoseconds, second.StopDeadlineNanoseconds);
    }

    [Fact]
    public void ExpiryAfterIntentRefusesAtGateWithoutWrite()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name =>
        {
            if (name == "after-intent")
            {
                h.Clock.NowNanoseconds = h.SetupDeadline;
            }
        };
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
    }

    [Fact]
    public void NewArbiterCannotResendOverDurableIntent()
    {
        using var h = new Harness();
        h.Arbiter.TestBarrier = name => { if (name == "after-intent") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));

        var secondStore = new PreparedTransaction.PreparationStore(
            RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, h.Clock);
        var secondCapability = secondStore.Mint(
            SampleManifest(), h.Identity, 1_000_000_000, TestClosure.Empty());
        using (secondCapability)
        {
            var secondArbiter = new ReleaseArbiter(
                secondCapability, h.Journal, h.Lease, h.Clock,
                () => new ReleasePrerequisites(true, true, true, true), h.Transport);
            var decision = secondArbiter.RequestRelease(h.Request);
            Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
            Assert.Equal(0, h.Transport.Attempts);
        }
    }

    [Fact]
    public void ExpiredSetupDeadlineRefusesBeforeIntent()
    {
        using var h = new Harness();
        h.Clock.NowNanoseconds = h.SetupDeadline;
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
    }
}
