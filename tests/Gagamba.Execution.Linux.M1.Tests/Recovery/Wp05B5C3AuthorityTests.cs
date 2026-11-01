// B5 T6: authority preservation. A missing or invalid StopRecord never
// fabricates a deadline, release authority is never recreated or resent, and
// unexpected loss of ownership never authorises destructive action.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3AuthorityTests
{
    private const long Accepted = 10_000_000_000;
    private const long Deadline = Accepted + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;

    private static StopRecord Stop() => new(Accepted, StopCauseKind.CallerStop, Deadline);

    private static (OwnershipJournal Journal, JournalWriterLease Writer) NewWriter()
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!);
    }

    [Fact]
    public void NoStopRecordUsesASeparateCleanupAllowance()
    {
        var record = RecoveryTestSupport.Snapshot(
            cleanup: OwnershipCleanupState.InProgress,
            allocations: null);
        var clock = new FakeDomainClock { NowNanoseconds = 1_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);

            var durable = journal.ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.Null(durable.Snapshot!.Stop);
            Assert.Null(durable.Snapshot.Release);
        }
    }

    [Fact]
    public void AcceptedStopWithoutDurableRecordDoesNotFabricateADeadline()
    {
        // The stop was accepted live but no StopRecord was persisted (clock or
        // journal failure path). Only the separate caller cleanup allowance
        // applies, and nothing durable may invent a stop deadline.
        var record = RecoveryTestSupport.V3Snapshot(
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var clock = new FakeDomainClock { NowNanoseconds = 5_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = new Reconciler(clock, TimeSpan.FromMilliseconds(100))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            Assert.Equal(4, clock.Delays);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);

            var durable = journal.ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.Null(durable.Snapshot!.Stop);
            Assert.Null(durable.Snapshot.Release);
        }
    }

    [Fact]
    public void ValidStopRecordIsNeverRewritten()
    {
        var stop = Stop();
        var record = RecoveryTestSupport.V3Snapshot(
            stop: stop,
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            var durable = journal.ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.Equal(stop, durable.Snapshot!.Stop);
            Assert.Equal(stop.StopDeadlineNanoseconds, durable.Snapshot.Stop!.StopDeadlineNanoseconds);
        }
    }

    [Fact]
    public void RecoveryDoesNotRecreateOrResendRelease()
    {
        var record = RecoveryTestSupport.V3Snapshot(
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            var durable = journal.ReadLatest().Snapshot!;
            Assert.Null(durable.Release);
        }
    }

    [Fact]
    public void CommittedReleaseEvidenceIsNotAlteredByRecovery()
    {
        var release = RecoveryTestSupport.ReleaseCommitted();
        var record = RecoveryTestSupport.V3Snapshot(
            release: release,
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            _ = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal, writer);

            Assert.Equal(release, journal.ReadLatest().Snapshot!.Release);
        }
    }

    [Fact]
    public void ReleaseIntentAfterDurableStopIsForbidden()
    {
        var record = RecoveryTestSupport.V3Snapshot(
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress);
        Assert.False(OwnershipTransitions.TryPlanReleaseIntent(
            record, 11_000_000_000, 15_000_000_000, 20_000_000_000, out _, out _));
    }

    [Fact]
    public void LostOwnershipDoesNotAuthoriseDestructiveAction()
    {
        var record = RecoveryTestSupport.V3Snapshot(
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        lease.Dispose();
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void CrossBootStopRecordIsRefusedWithoutDestructiveAction()
    {
        var record = RecoveryTestSupport.V3Snapshot(
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-2", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact]
    public void StaleOwnershipIdentityIsRefusedBeforeStop()
    {
        var record = RecoveryTestSupport.V3Snapshot(
            stop: Stop(),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval) with { DomainInode = 999 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease, null, null, null);

        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
        Assert.Equal(0, backend.WriteKillCalls);
    }
}
