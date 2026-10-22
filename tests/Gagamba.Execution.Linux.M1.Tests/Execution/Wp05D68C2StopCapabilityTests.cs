using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class Wp05D68C2StopCapabilityTests
{
    private sealed class CountingClock : ITransactionClock
    {
        private long _now = 1_000;

        public int Reads { get; private set; }

        public long Value
        {
            get => _now;
            set => _now = value;
        }

        public long NowNanoseconds
        {
            get
            {
                Reads++;
                return _now;
            }
        }
    }

    private sealed class RecordingTransport : IReleaseWriteChannel
    {
        public RecordingTransport(ReleasePipeWriteResult result) => Result = result;

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

    private sealed class Harness : IDisposable
    {
        public Harness(
            ReleasePipeWriteResult result = ReleasePipeWriteResult.Delivered,
            long expiry = 1_000_000_000)
        {
            Clock = new CountingClock();
            Manifest = SampleManifest();
            Identity = new PreparationIdentity(
                RecoveryTestSupport.Execution,
                RecoveryTestSupport.Issuer,
                RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation,
                ManifestCodec.ComputeDigestBytes(Manifest));
            Store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = Store.Mint(Manifest, Identity, expiry, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, "/wp05-d68-c2");
            Assert.True(Journal.TryAcquireWriter(out var lease, out var error), error);
            Lease = lease!;
            var preparation = new RecordedPreparation(
                Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                Identity.PreparationId, Identity.ManifestDigest.ToArray());
            var seed = RecoveryTestSupport.Snapshot(
                stage: PreparationStage.Prepared, preparation: preparation);
            Assert.True(Journal.WriteNext(Lease, seed).Ok);
            Transport = new RecordingTransport(result);
            Arbiter = NewArbiter();
        }

        public CountingClock Clock { get; }
        public RestrictedManifest Manifest { get; }
        public PreparationIdentity Identity { get; }
        public PreparedTransaction.PreparationStore Store { get; }
        public PreparedTransaction Capability { get; }
        public FakeJournalStorage Storage { get; }
        public OwnershipJournal Journal { get; }
        public JournalWriterLease Lease { get; }
        public RecordingTransport Transport { get; }
        public ReleaseArbiter Arbiter { get; }
        public ReleasePrerequisites Prerequisites { get; set; } = new(true, true, true, true);
        public long SetupDeadline { get; } = 10_000;
        public long OverallDeadline { get; } = 30_000;
        public ReleaseRequest Request => new(Identity, SetupDeadline, OverallDeadline);

        public ReleaseArbiter NewArbiter() => new(
            Capability, Journal, Lease, Clock, () => Prerequisites, Transport);

        public OwnershipSnapshot Latest()
        {
            var read = Journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            return read.Snapshot!;
        }

        public long Sequence() => Latest().Sequence;

        public void Dispose()
        {
            Capability.Dispose();
            Lease.Dispose();
        }
    }

    [Fact]
    public void C2_T01_NormalStopFirstConsumesCapabilityAndBlocksRelease()
    {
        using var h = new Harness();

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.False(stop.ReleaseAlreadyCommitted);
        Assert.False(h.Capability.IsLive);
        var latest = h.Latest();
        Assert.NotNull(latest.Stop);
        Assert.Null(latest.Release);

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void C2_T02_ClockUnavailableStopConsumesCapabilityWithoutDurableStop()
    {
        using var h = new Harness();
        h.Clock.Value = 0;

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Equal(0, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(h.Capability.IsLive);
        Assert.Null(h.Latest().Stop);

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void C2_T03_ExpiredCapabilityOverflowStopAcceptedWithoutDurableStop()
    {
        using var h = new Harness();
        h.Clock.Value = long.MaxValue - 2_000_000_000L;

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Equal(long.MaxValue - 2_000_000_000L, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(h.Capability.IsLive);
        Assert.Null(h.Latest().Stop);

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void C2_T03b_LiveCapabilityOverflowStopConsumesCapability()
    {
        using var h = new Harness(expiry: long.MaxValue - 1);
        var overflowNow = long.MaxValue - 2_000_000_000L;
        h.Clock.Value = overflowNow;

        Assert.True(h.Capability.ExpiresAtNs > overflowNow);
        Assert.True(h.Capability.IsLive);
        var baseline = h.Sequence();

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);

        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Equal(overflowNow, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.Null(h.Latest().Stop);
        Assert.False(h.Capability.IsLive);
        Assert.False(h.Capability.TryConsume(out var violation));
        Assert.Equal(InvariantViolation.Consumed, violation);
        Assert.Null(h.Latest().Release);
        Assert.Equal(baseline, h.Sequence());
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void C2_T04_StopRecordPersistenceFailureStillConsumesCapability()
    {
        using var h = new Harness();
        h.Storage.FailAt("write", 1);
        var baseline = h.Sequence();

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Null(h.Latest().Stop);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(baseline, h.Sequence());

        var second = h.NewArbiter();
        var release = second.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.False(release.DurableCommitted);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
        Assert.Equal(baseline, h.Sequence());
    }

    [Fact]
    public void C2_T05_DuplicateStopIsIdempotentWithoutSecondConsumeOrDeadline()
    {
        using var h = new Harness();

        var first = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(first.Accepted);
        Assert.True(first.DurableStop);
        Assert.False(h.Capability.IsLive);

        var readsBefore = h.Clock.Reads;
        var second = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        var readsAfter = h.Clock.Reads;

        Assert.True(second.Accepted);
        Assert.True(second.Duplicate);
        Assert.Equal(first.StopAcceptedAtNanoseconds, second.StopAcceptedAtNanoseconds);
        Assert.Equal(first.StopDeadlineNanoseconds, second.StopDeadlineNanoseconds);
        Assert.Equal(first.Reason, second.Reason);
        Assert.Equal(1, readsAfter - readsBefore);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(first.StopDeadlineNanoseconds, h.Latest().Stop!.StopDeadlineNanoseconds);
    }

    [Fact]
    public void C2_T06_ReleaseAfterStopWritesNoIntentAndNoPhysicalRelease()
    {
        using var h = new Harness();
        Assert.True(h.Arbiter.RequestStop(StopCauseKind.CallerStop).DurableStop);
        var baseline = h.Sequence();

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(baseline, h.Sequence());
        Assert.Null(h.Latest().Release);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void C2_T07_SecondArbiterAfterFailedDurableStopCannotRelease()
    {
        using var h = new Harness();
        h.Storage.FailAt("write", 1);

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Null(h.Latest().Stop);
        Assert.False(h.Capability.IsLive);
        var baseline = h.Sequence();

        var second = h.NewArbiter();
        var release = second.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.False(release.DurableCommitted);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(baseline, h.Sequence());
        Assert.Null(h.Latest().Release);
        Assert.Null(h.Latest().Stop);
    }

    [Fact]
    public void C2_T08_StopAfterDurableIntentStaysConservativeMayHaveExecuted()
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

        var release = h.Arbiter.RequestRelease(h.Request);

        Assert.NotNull(stop);
        Assert.True(stop!.Accepted);
        Assert.True(stop.DurableStop);
        Assert.False(stop.ReleaseAlreadyCommitted);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.False(release.DurableCommitted);
        Assert.Equal(0, h.Transport.Attempts);
        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
        Assert.NotNull(latest.Stop);
    }

    [Fact]
    public void C2_T09_ReleaseBeforeStopLeavesReleaseEvidenceUnchanged()
    {
        using var h = new Harness();

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Released, release.Kind);
        Assert.Equal(DeliveryResult.Delivered, release.Delivery);
        Assert.True(release.DurableCommitted);
        Assert.Equal(1, h.Transport.Attempts);
        var committed = h.Latest().Release!;
        Assert.Equal(ReleaseTag.Committed, committed.Tag);
        var committedSequence = h.Sequence();

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.True(stop.ReleaseAlreadyCommitted);
        Assert.False(h.Capability.IsLive);

        var latest = h.Latest();
        Assert.NotNull(latest.Stop);
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, latest.Release.DeliveryResult);
        Assert.Equal(committed.IntentAtNanoseconds, latest.Release.IntentAtNanoseconds);
        Assert.Equal(committed.CommittedAtNanoseconds, latest.Release.CommittedAtNanoseconds);
        Assert.Equal(committedSequence + 1, latest.Sequence);
        Assert.Equal(1, h.Transport.Attempts);

        var duplicate = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(DeliveryResult.Delivered, duplicate.Delivery);
        Assert.True(duplicate.DurableCommitted);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void C2_T10_StopAfterConsumedReleaseCannotReactivateCapability()
    {
        using var h = new Harness(ReleasePipeWriteResult.BrokenPipe);

        var release = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Released, release.Kind);
        Assert.Equal(DeliveryResult.BrokenPipe, release.Delivery);
        Assert.True(release.LiveExecutionExcluded);
        Assert.True(release.DurableCommitted);
        Assert.False(h.Capability.IsLive);

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.True(stop.ReleaseAlreadyCommitted);
        Assert.False(h.Capability.IsLive);

        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.Equal(DeliveryResult.BrokenPipe, latest.Release.DeliveryResult);
        Assert.NotNull(latest.Stop);

        var duplicate = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(DeliveryResult.BrokenPipe, duplicate.Delivery);
        Assert.True(duplicate.LiveExecutionExcluded);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void C2_T11_ReplacementContextCannotInheritConsumedCapability()
    {
        using var h = new Harness();
        Assert.True(h.Arbiter.RequestStop(StopCauseKind.CallerStop).DurableStop);
        Assert.False(h.Capability.IsLive);

        Assert.Throws<ArgumentException>(() =>
            h.Store.Mint(h.Manifest, h.Identity, 1_000_000_000, TestClosure.Empty()));

        var replacementStore = new PreparedTransaction.PreparationStore(
            RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, h.Clock);
        using var replacementCapability = replacementStore.Mint(
            h.Manifest, h.Identity, 1_000_000_000, TestClosure.Empty());
        var replacementArbiter = new ReleaseArbiter(
            replacementCapability, h.Journal, h.Lease, h.Clock,
            () => h.Prerequisites, h.Transport);

        var release = replacementArbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
        Assert.NotNull(h.Latest().Stop);
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public async Task C2_T12_CompetingStopAndReleaseHaveOneWinnerAndNoPhysicalRelease()
    {
        using var h = new Harness();
        using var atIntent = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        h.Arbiter.TestBarrier = name =>
        {
            if (name == "after-intent")
            {
                atIntent.Set();
                if (!resume.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("after-intent barrier timed out");
                }
            }
        };

        var cancellation = TestContext.Current.CancellationToken;
        var releaseTask = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
        Assert.True(atIntent.Wait(TimeSpan.FromSeconds(5), cancellation), "release did not reach durable intent");

        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.False(h.Capability.IsLive);

        resume.Set();
        var release = await releaseTask.WaitAsync(TimeSpan.FromSeconds(5), cancellation);

        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, release.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Delivery);
        Assert.Equal(0, h.Transport.Attempts);
        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
        Assert.NotNull(latest.Stop);

        var later = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, later.Kind);
        Assert.Equal(0, h.Transport.Attempts);
    }
}
