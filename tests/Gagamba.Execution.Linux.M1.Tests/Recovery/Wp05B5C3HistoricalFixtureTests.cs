// B5 T7: the accepted D6.2/D6.3 historical contract fixtures R1-T1 through
// R1-T12. R1-T13 through R1-T15 execute in the dedicated D6_4 fixture build.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3HistoricalFixtureTests
{
    private const long Accepted = 10_000_000_000;
    private const long Deadline = Accepted + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;

    private static OwnershipSnapshot Record(
        byte completed = (byte)CleanupComponents.DomainRemoval,
        params AllocationEntry[] allocations) =>
        RecoveryTestSupport.V3Snapshot(
            basis: RecoveryTestSupport.Snapshot(allocations: allocations),
            stop: new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: completed);

    private static (OwnershipJournal Journal, JournalWriterLease Writer, FakeJournalStorage Storage) NewWriter()
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!, storage);
    }

    private static ComponentState State(ReconcileResult result, CleanupComponents component) =>
        Assert.Single(result.Components!, c => c.Component == component).State;

    private static ReconcileResult Run(
        OwnershipSnapshot record,
        FakeDomainBackend backend,
        long now,
        IAllocationCleaner? cleaner = null,
        OwnershipJournal? journal = null,
        JournalWriterLease? writer = null)
    {
        var clock = new FakeDomainClock { NowNanoseconds = now };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        return new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease, cleaner, journal, writer);
    }

    [Fact] // R1-T1
    public void KillWriteFailureIsFailedWithNoFabricatedProgress()
    {
        var backend = new FakeDomainBackend { Populated = true, KillFault = true };
        var (journal, writer, _) = NewWriter();
        using (writer)
        {
            var result = Run(Record(), backend, Deadline - 1_000_000_000, journal: journal, writer: writer);
            Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
            var durable = journal.ReadLatest().Snapshot!;
            Assert.Equal((byte)CleanupComponents.DomainRemoval, durable.CompletedCleanup);
        }
    }

    [Fact] // R1-T2
    public void KillAcceptedButPopulatedIsUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var (journal, writer, _) = NewWriter();
        using (writer)
        {
            var result = Run(Record(), backend, Deadline - 100_000_000, journal: journal, writer: writer);
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact] // R1-T3
    public void KillDispatchUnknownIsConservative()
    {
        var backend = new FakeDomainBackend { Populated = true, KillDispatchUnknown = true };
        var result = Run(Record(), backend, Deadline - 1_000_000_000);
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.Equal(1, backend.WriteKillCalls);
        Assert.True(result.DestructiveActionTaken);
    }

    [Fact] // R1-T4
    public void PopulationObservationUnavailableIsUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false, ReadFault = true };
        var result = Run(Record(), backend, Deadline - 1_000_000_000);
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact] // R1-T5
    public void ConfirmedEmptyProducesExactDurableBits()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var (journal, writer, _) = NewWriter();
        using (writer)
        {
            var result = Run(Record(), backend, Deadline - 1_000_000_000, journal: journal, writer: writer);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            var durable = journal.ReadLatest().Snapshot!;
            Assert.Equal(
                (byte)(CleanupComponents.DomainRemoval
                    | CleanupComponents.DomainStop
                    | CleanupComponents.DomainEmpty),
                durable.CompletedCleanup);
            Assert.Equal(OwnershipCleanupState.Confirmed, durable.Cleanup);
        }
    }

    [Fact] // R1-T6
    public void StopProgressPersistenceFailureIsUnknownNotFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, Record()).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = step => step == "rename" ? JournalIoResult.Failure("injected") : null;
        using (writer2)
        {
            var result = Run(journal2.ReadLatest().Snapshot!, backend, Deadline - 1_000_000_000,
                journal: journal2, writer: writer2);
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact] // R1-T7
    public void FinalPersistenceFailureAfterConfirmedEmptyIsUnknown()
    {
        var backend = new FakeDomainBackend { Populated = false };
        var (journal, writer, storage) = NewWriter();
        var allBits = (byte)(CleanupComponents.DomainStop
            | CleanupComponents.DomainEmpty
            | CleanupComponents.Staging
            | CleanupComponents.DomainRemoval);
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, Record(allBits)).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = step => step == "rename" ? JournalIoResult.Failure("injected") : null;
        using (writer2)
        {
            var result = Run(journal2.ReadLatest().Snapshot!, backend, Deadline - 1_000_000_000,
                journal: journal2, writer: writer2);
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact] // R1-T8
    public void GenuineOperationFailureIsFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var cleaner = new FailedCleaner();
        var result = Run(Record(allocations: StagingAllocation()), backend, Deadline - 1_000_000_000, cleaner);
        Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.Staging));
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact] // R1-T9
    public void InvalidCleanupAuthorityIsConservative()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var record = Record(allocations: StagingAllocation());

        var crossBoot = Run(record with { BootId = "boot-2" }, backend, Deadline - 1_000_000_000);
        Assert.Equal(ReconcileOutcome.Refused, crossBoot.Outcome);

        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var clock = new FakeDomainClock { NowNanoseconds = Deadline - 1_000_000_000 };
        var restartOnly = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", null, null, null, null);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, restartOnly.Outcome);

        var missingCleaner = Run(record, backend, Deadline - 1_000_000_000, null);
        Assert.Equal(ComponentState.Unknown, State(missingCleaner, CleanupComponents.Staging));
    }

    [Fact] // R1-T10
    public void IdentityMismatchRefusesBeforeStop()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false, DomainInode = 999 };
        var result = Run(Record(), backend, Deadline - 1_000_000_000);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.Equal(0, backend.WriteKillCalls);
    }

    [Fact] // R1-T11
    public void MixedComponentAndPersistenceResultsKeepBothFacts()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var cleaner = new FailedCleaner();
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, Record(allocations: StagingAllocation())).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = step => step == "rename" ? JournalIoResult.Failure("injected") : null;
        using (writer2)
        {
            var result = Run(journal2.ReadLatest().Snapshot!, backend, Deadline - 1_000_000_000,
                cleaner, journal2, writer2);
            Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.Staging));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
        }
    }

    [Fact] // R1-T12
    public void RepeatedReconciliationAfterUncertainFinalizationDoesNotResetProgress()
    {
        var backend = new FakeDomainBackend { Populated = false };
        var allBits = (byte)(CleanupComponents.DomainStop
            | CleanupComponents.DomainEmpty
            | CleanupComponents.Staging
            | CleanupComponents.DomainRemoval);
        var (journal, writer, storage) = NewWriter();
        using (writer)
        {
            Assert.True(journal.WriteNext(writer, Record(allBits)).Ok);
        }

        var (journal2, writer2, storage2) = NewWriterFrom(storage);
        storage2.Fault = step => step == "rename" ? JournalIoResult.Failure("injected") : null;
        using (writer2)
        {
            var first = Run(journal2.ReadLatest().Snapshot!, backend, Deadline - 1_000_000_000,
                journal: journal2, writer: writer2);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, first.Outcome);
        }

        storage.Fault = null;
        var (journal3, writer3, _) = NewWriterFrom(storage);
        using (writer3)
        {
            var record = journal3.ReadLatest().Snapshot!;
            var second = Run(record, backend, Deadline - 2_000_000_000, journal: journal3, writer: writer3);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, second.Outcome);
            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(record.Stop, journal3.ReadLatest().Snapshot!.Stop);
        }
    }

    private static AllocationEntry[] StagingAllocation() =>
        new[] { new AllocationEntry(AllocationKind.Staging, "tool-000.staged", 10, new byte[32], 0xABCD, 200, true) };

    private static (OwnershipJournal Journal, JournalWriterLease Writer, FakeJournalStorage Storage)
        NewWriterFrom(FakeJournalStorage storage)
    {
        var journal = new OwnershipJournal(storage, "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!, storage);
    }

    private sealed class FailedCleaner : IAllocationCleaner
    {
        public AllocationCleanResult CleanOne(AllocationEntry entry) =>
            new(AllocationCleanOutcome.Failed, "cleaner operation failed");
    }
}
