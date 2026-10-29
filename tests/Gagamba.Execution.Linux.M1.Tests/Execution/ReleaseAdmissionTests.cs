// RC2B-03 B3 R1-F03 Option A admission tests at the authoritative owner-side
// arbiter: first-binding admission, pre-retention mismatch refusal without
// consumption or retention, C4 duplicate projection and the exact C1/C2
// winner order. In-process; no native workload.
using System.Collections.Immutable;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class ReleaseAdmissionTests
{
    private static readonly ImmutableArray<byte> CorrelationA =
        ImmutableArray.Create(Enumerable.Repeat((byte)0x11, 32).ToArray());
    private static readonly ImmutableArray<byte> CorrelationB =
        ImmutableArray.Create(Enumerable.Repeat((byte)0x22, 32).ToArray());
    private static readonly ImmutableArray<byte> AllZero = ImmutableArray.Create(new byte[32]);

    private sealed class FakeClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; } = 1_000;
    }

    private sealed class FakeTransport : IReleaseWriteChannel
    {
        public int Attempts { get; private set; }

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            return ReleasePipeWriteResult.Delivered;
        }
    }

    private sealed class Harness : IDisposable
    {
        public Harness()
        {
            Clock = new FakeClock();
            var manifest = SampleManifest();
            Identity = new PreparationIdentity(
                RecoveryTestSupport.Execution, RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation, ManifestCodec.ComputeDigestBytes(manifest));
            Store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = Store.Mint(manifest, Identity, 1_000_000_000, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, "/admission");
            Assert.True(Journal.TryAcquireWriter(out var lease, out var error), error);
            Lease = lease!;
            var preparation = new RecordedPreparation(
                Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                Identity.PreparationId, Identity.ManifestDigest.ToArray());
            Assert.True(Journal.WriteNext(Lease,
                RecoveryTestSupport.Snapshot(stage: PreparationStage.Prepared, preparation: preparation)).Ok);
            Transport = new FakeTransport();
            Arbiter = new ReleaseArbiter(Capability, Journal, Lease, Clock,
                () => new ReleasePrerequisites(true, true, true, true), Transport);
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

        public ReleaseRequest Request(ImmutableArray<byte> correlation = default) =>
            new(Identity, 10_000, 30_000, correlation);

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
    public void FirstAdmissibleBindingSurvivesAPreRetentionRefusalAndMismatchDoesNotConsume()
    {
        using var h = new Harness();
        // Kill the writer fence after construction: the first call still
        // passes every admission gate, stays pre-retention and is refused.
        h.Lease.Dispose();
        var first = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Refused, first.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, first.DurableRelease);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, first.Binding);
        Assert.Null(h.Latest().Release);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.True(h.Capability.IsLive, "a pre-retention refusal must not consume the capability");

        var mismatch = h.Arbiter.RequestRelease(h.Request(CorrelationB));
        Assert.Equal(ReleaseDecisionKind.Refused, mismatch.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Mismatch, mismatch.Binding);
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, mismatch.RefusalReason);
        Assert.True(h.Capability.IsLive, "a mismatch must not consume the capability");
        Assert.Null(h.Latest().Release);

        // The original matching correlation may still be evaluated.
        var matching = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseCorrelationBinding.Accepted, matching.Binding);
        Assert.Equal(ReleaseDecisionKind.Refused, matching.Kind);
        Assert.Equal(RestrictedRefusalReason.RecoveryAuthorityUnavailable, matching.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void InvalidCorrelationLengthsRefuseWithTheirOwnBindingBeforeAnythingElse()
    {
        using var h = new Harness();
        var invalid = h.Arbiter.RequestRelease(h.Request(ImmutableArray.Create(new byte[31])));
        Assert.Equal(ReleaseDecisionKind.Refused, invalid.Kind);
        Assert.Equal(ReleaseCorrelationBinding.InvalidArgument, invalid.Binding);
        Assert.Equal(RestrictedRefusalReason.InvalidCorrelation, invalid.RefusalReason);
        Assert.True(h.Capability.IsLive);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);

        // The invalid form did not bind anything: a valid call still releases.
        var released = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Released, released.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, released.Binding);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void PostRetentionMismatchStaysDuplicateAndCannotWriteAgain()
    {
        using var h = new Harness();
        var released = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Released, released.Kind);
        var duplicate = h.Arbiter.RequestRelease(h.Request(CorrelationB));
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Mismatch, duplicate.Binding);
        Assert.Equal(released.DurableRelease, duplicate.DurableRelease);
        Assert.Equal(released.Delivery, duplicate.Delivery);
        var matching = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Duplicate, matching.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, matching.Binding);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Committed, h.Latest().Release!.Tag);
    }

    [Fact]
    public void AbsentAndAllZeroAreDistinctPresentForms()
    {
        using var h = new Harness();
        var released = h.Arbiter.RequestRelease(h.Request());
        Assert.Equal(ReleaseDecisionKind.Released, released.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, released.Binding);
        var allZero = h.Arbiter.RequestRelease(h.Request(AllZero));
        Assert.Equal(ReleaseDecisionKind.Duplicate, allZero.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Mismatch, allZero.Binding);
        var absent = h.Arbiter.RequestRelease(h.Request());
        Assert.Equal(ReleaseDecisionKind.Duplicate, absent.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, absent.Binding);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void StopWinsBeforeIntentAndTheStopFactStaysAuthoritative()
    {
        using var h = new Harness();
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.False(stop.ReleaseAlreadyCommitted);
        var release = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(RestrictedRefusalReason.StopRequested, release.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);
        var duplicateStop = h.Arbiter.RequestStop(StopCauseKind.SetupExpired);
        Assert.True(duplicateStop.Duplicate);
        Assert.Equal(StopCauseKind.CallerStop, duplicateStop.Cause);
    }

    [Fact]
    public void ReleaseWinsThenStopReportsTheCommittedReleaseAndKeepsOneWrite()
    {
        using var h = new Harness();
        var released = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Released, released.Kind);
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.ReleaseAlreadyCommitted);
        Assert.True(stop.DurableStop);
        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Committed, latest.Release!.Tag);
        Assert.NotNull(latest.Stop);
        var duplicate = h.Arbiter.RequestRelease(h.Request(CorrelationB));
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void DeadlineAndPrerequisiteRefusalsCarryTypedReasonsWithoutRetention()
    {
        using var h = new Harness();
        h.Clock.NowNanoseconds = 20_000; // past the setup deadline
        var expired = h.Arbiter.RequestRelease(h.Request(CorrelationA));
        Assert.Equal(ReleaseDecisionKind.Refused, expired.Kind);
        Assert.Equal(RestrictedRefusalReason.DeadlineExpired, expired.RefusalReason);
        Assert.Equal(ReleaseDisposition.NotCommitted, expired.DurableRelease);
        Assert.True(h.Capability.IsLive);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Latest().Release);

        using var blocked = new Harness();
        var refused = new ReleaseArbiter(blocked.Capability, blocked.Journal, blocked.Lease, blocked.Clock,
            () => new ReleasePrerequisites(false, true, true, true), blocked.Transport)
            .RequestRelease(blocked.Request(CorrelationA));
        Assert.Equal(RestrictedRefusalReason.PrerequisitesUnavailable, refused.RefusalReason);
        Assert.Equal(ReleaseDisposition.NotCommitted, refused.DurableRelease);
        Assert.Equal(0, blocked.Transport.Attempts);
        Assert.True(blocked.Capability.IsLive);
        Assert.Null(blocked.Latest().Release);
    }
}
