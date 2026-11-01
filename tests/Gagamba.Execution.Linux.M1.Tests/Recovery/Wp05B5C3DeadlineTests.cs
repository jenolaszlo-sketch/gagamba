// B5 T2/T3: authoritative durable deadline arithmetic and Policy A. The
// persisted absolute deadline is the only bound; expiry authorises exactly
// one identity-checked kill plus one immediate population observation.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3DeadlineTests
{
    private const long Accepted = 10_000_000_000;
    private const long Deadline = Accepted + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;

    private static OwnershipSnapshot Record(byte completed = (byte)CleanupComponents.DomainRemoval) =>
        RecoveryTestSupport.V3Snapshot(
            stop: new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: completed);

    private static (DomainLease Lease, FakeDomainBackend Backend) LiveLease(
        bool populated = true, bool killAccepted = false)
    {
        var backend = new FakeDomainBackend { Populated = populated, KillAccepted = killAccepted };
        return (new DomainLease(backend, RecoveryTestSupport.Identity()), backend);
    }

    private static (OwnershipJournal Journal, JournalWriterLease Writer) NewWriter()
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!);
    }

    private static ComponentState State(ReconcileResult result, CleanupComponents component) =>
        Assert.Single(result.Components!, c => c.Component == component).State;

    [Fact]
    public void FutureDeadlineUsesOnlyTheRemainingWindow()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var (lease, _) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(40, clock.Delays);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void NearExpiryUsesOnlyTheActualRemainingTime()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 100_000_000 };
        var (lease, _) = LiveLease();

        _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(4, clock.Delays);
    }

    [Fact]
    public void ExactExpiryAppliesPolicyAWithoutRenewedWait()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(0, clock.Delays);
        Assert.Equal(1, backend.WriteKillCalls);
        Assert.Equal(1, backend.ReadPopulationCalls);
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void ExpiredDeadlineAppliesPolicyAWithoutRenewedWait()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline + 360_000_000_000 };
        var (lease, backend) = LiveLease();

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(0, clock.Delays);
        Assert.Equal(1, backend.WriteKillCalls);
        Assert.Equal(1, backend.ReadPopulationCalls);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void PolicyADoesNotSkipTheFinalPopulationObservation()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline + 1 };
        var (lease, backend) = LiveLease(populated: true, killAccepted: false);

        _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(1, backend.WriteKillCalls);
        Assert.Equal(1, backend.ReadPopulationCalls);
        Assert.Equal(0, clock.Delays);
    }

    [Fact]
    public void PolicyADefinitiveKillFailureRemainsFailed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline + 1 };
        var backend = new FakeDomainBackend { Populated = true, KillFault = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(0, clock.Delays);
        Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void PolicyAObservationUnavailableIsUncertainNotFailed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline + 1 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false, ReadFault = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(0, clock.Delays);
        Assert.Equal(1, backend.ReadPopulationCalls);
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void RepeatedReconciliationNeverExtendsTheAbsoluteDeadline()
    {
        var record = Record();
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 2_000_000_000 };
        var (lease, _) = LiveLease();
        _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease, null, null, null);
        Assert.Equal(80, clock.Delays);

        clock.NowNanoseconds = Deadline - 1_000_000_000;
        var (lease2, backend2) = LiveLease();
        _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease2, null, null, null);

        Assert.Equal(80 + 40, clock.Delays);
        Assert.Equal(new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline), record.Stop);
    }

    [Fact]
    public void PartialCleanupDoesNotResetTheDeadline()
    {
        // DomainStop+DomainEmpty already durable; only later components remain.
        var record = Record((byte)(CleanupComponents.DomainStop | CleanupComponents.DomainEmpty));
        var clock = new FakeDomainClock { NowNanoseconds = Deadline + 1 };
        var (lease, backend) = LiveLease(populated: false);
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(0, backend.ReadPopulationCalls);
            Assert.Equal(0, clock.Delays);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);

            var durable = journal.ReadLatest();
            Assert.True(durable.Ok);
            Assert.Equal(record.Stop, durable.Snapshot!.Stop);
        }
    }

    [Fact]
    public void CallerAllowanceCannotExtendTheDurableDeadline()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 500_000_000 };
        var (lease, _) = LiveLease();

        _ = new Reconciler(clock, TimeSpan.FromHours(1))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(20, clock.Delays);
    }
}
