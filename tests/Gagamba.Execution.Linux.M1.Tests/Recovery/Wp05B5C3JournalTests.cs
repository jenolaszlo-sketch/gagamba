// B5 T5: journal outcomes. Operation failures and persistence uncertainty stay
// distinct; persistence uncertainty never fabricates CleanupConfirmed and never
// becomes an invented operation failure.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3JournalTests
{
    private const long Accepted = 10_000_000_000;
    private const long Deadline = Accepted + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;

    private static OwnershipSnapshot Record(byte completed = (byte)CleanupComponents.DomainRemoval) =>
        RecoveryTestSupport.V3Snapshot(
            stop: new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: completed);

    private static OwnershipSnapshot FullyConfirmedRecord() =>
        Record((byte)(CleanupComponents.DomainStop
            | CleanupComponents.DomainEmpty
            | CleanupComponents.Staging
            | CleanupComponents.DomainRemoval));

    private static OwnershipSnapshot StagingRecord() =>
        RecoveryTestSupport.V3Snapshot(
            basis: RecoveryTestSupport.Snapshot(allocations: new[]
            {
                new AllocationEntry(AllocationKind.Staging, "tool-000.staged", 10, new byte[32], 0xABCD, 200, true),
            }),
            stop: new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);

    private static ComponentState State(ReconcileResult result, CleanupComponents component) =>
        Assert.Single(result.Components!, c => c.Component == component).State;

    private static (OwnershipJournal Journal, JournalWriterLease Writer, FakeJournalStorage Storage) NewWriter()
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!, storage);
    }

    private static JournalIoResult? PersistentRenameFault(string step) =>
        step == "rename" ? JournalIoResult.Failure("injected rename failure") : null;

    [Fact]
    public void ComponentProgressPersistenceFailureIsUnknownNotFailed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer, storage) = NewWriter();
        storage.Fault = PersistentRenameFault;
        using (writer)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(Record(), "boot-1", lease, null, journal, writer);

            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.NotEqual(ReconcileOutcome.Failed, result.Outcome);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact]
    public void FinalPersistenceFailureIsUnknownNotFailed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            var seeded = journal.WriteNext(writer, FullyConfirmedRecord());
            Assert.True(seeded.Ok, seeded.Error);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = PersistentRenameFault;
        using (writer2)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(journal2.ReadLatest().Snapshot!, "boot-1", lease, null, journal2, writer2);

            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.NotEqual(ReconcileOutcome.Failed, result.Outcome);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact]
    public void MixedOperationAndPersistenceFailureStaysFailed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FailingCleaner();
        var (journal, writer, storage) = NewWriter();
        storage.Fault = PersistentRenameFault;
        using (writer)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(StagingRecord(), "boot-1", lease, cleaner, journal, writer);

            Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.Staging));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
            Assert.Contains("failed", result.Detail, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EarlierProgressFailureKeepsFinalVerdictUnknownEvenIfFinalWriteSucceeds()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, Record()).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        // Fail exactly the next rename (the stop progress write); later writes
        // are allowed so the final commit genuinely succeeds.
        storage2.FailAt("rename", 1);
        using (writer2)
        {
            var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(journal2.ReadLatest().Snapshot!, "boot-1", lease, null, journal2, writer2);

            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);

            var durable = journal2.ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.NotEqual(OwnershipCleanupState.Confirmed, durable.Snapshot!.Cleanup);
        }
    }

    [Fact]
    public void MissingWriterLeaseCannotProduceCleanupConfirmed()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(Record(), "boot-1", lease, null, null, null);

        Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void RepeatedReconciliationAfterUncertainFinalizationConfirmsOnlyAfterRealSuccess()
    {
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var backend = new FakeDomainBackend { Populated = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, FullyConfirmedRecord()).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = PersistentRenameFault;
        using (writer2)
        {
            var first = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(journal2.ReadLatest().Snapshot!, "boot-1", lease, null, journal2, writer2);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, first.Outcome);
        }

        // Repeat with a healthy journal and unchanged record: no destructive
        // work is repeated and the deadline is untouched.
        storage.Fault = null;
        var (journal3, writer3, _) = NewWriterFrom(storage);
        using (writer3)
        {
            var record = journal3.ReadLatest().Snapshot!;
            var second = new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(record, "boot-1", lease, null, journal3, writer3);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, second.Outcome);
            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(record.Stop, journal3.ReadLatest().Snapshot!.Stop);
        }
    }

    private static (OwnershipJournal Journal, JournalWriterLease Writer, FakeJournalStorage Storage)
        NewWriterFrom(FakeJournalStorage storage)
    {
        var journal = new OwnershipJournal(storage, "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!, storage);
    }

    private sealed class FailingCleaner : IAllocationCleaner
    {
        public AllocationCleanResult CleanOne(AllocationEntry entry) =>
            new(AllocationCleanOutcome.Failed, "cleaner operation failed");
    }
}
