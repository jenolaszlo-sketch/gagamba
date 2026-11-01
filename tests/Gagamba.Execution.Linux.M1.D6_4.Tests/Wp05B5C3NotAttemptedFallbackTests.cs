// B5 T7 (R1-T13 through R1-T15): the fixed D6.4 NotAttempted stop fixture.
// Only the stop result is substituted; planning, identity verification, the
// real fallback population observation, journal persistence and aggregation
// all execute in the shared production Reconciler source.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.D6_4.Tests;

public sealed class Wp05B5C3NotAttemptedFallbackTests
{
    private static OwnershipSnapshot Record() =>
        RecoveryTestSupport.V3Snapshot(
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);

    private static (OwnershipJournal Journal, JournalWriterLease Writer) SeededWriter()
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.True(journal.WriteNext(lease!, Record()).Ok);
        return (journal, lease!);
    }

    private static ReconcileResult Run(
        FakeDomainBackend backend,
        OwnershipJournal journal,
        JournalWriterLease writer,
        out FakeDomainClock clock,
        out int cleanerCalls)
    {
        clock = new FakeDomainClock { NowNanoseconds = 1_000_000_000 };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var calls = 0;
        IAllocationCleaner cleaner = new CountingCleaner(() => calls++);
        var result = new Reconciler(clock, TimeSpan.Zero, new NotAttemptedStopTestFixture())
            .Reconcile(journal.ReadLatest().Snapshot!, "boot-1", lease, cleaner, journal, writer);
        cleanerCalls = calls;
        return result;
    }

    private static ComponentState State(ReconcileResult result, CleanupComponents component) =>
        Assert.Single(result.Components!, c => c.Component == component).State;

    [Fact] // R1-T13
    public void NotAttemptedWithTrustedEmptyFallbackConfirmsEmptinessOnly()
    {
        var backend = new FakeDomainBackend { Populated = false };
        var (journal, writer) = SeededWriter();
        using (writer)
        {
            var result = Run(backend, journal, writer, out var clock, out var cleanerCalls);

            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);

            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(1, backend.ReadPopulationCalls);
            Assert.Equal(0, backend.RemoveChildCalls);
            Assert.Equal(0, cleanerCalls);
            Assert.Equal(0, clock.Delays);
            Assert.Equal(0, clock.NowNanoseconds - 1_000_000_000);

            var durable = journal.ReadLatest().Snapshot!;
            Assert.Equal(
                (byte)(CleanupComponents.DomainRemoval | CleanupComponents.DomainEmpty),
                durable.CompletedCleanup);
            Assert.NotEqual(OwnershipCleanupState.Confirmed, durable.Cleanup);
        }
    }

    [Fact] // R1-T14
    public void NotAttemptedWithPopulatedFallbackStaysUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true };
        var (journal, writer) = SeededWriter();
        using (writer)
        {
            var result = Run(backend, journal, writer, out var clock, out var cleanerCalls);

            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);

            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(1, backend.ReadPopulationCalls);
            Assert.Equal(0, backend.RemoveChildCalls);
            Assert.Equal(0, cleanerCalls);
            Assert.Equal(0, clock.Delays);

            Assert.Equal((byte)CleanupComponents.DomainRemoval,
                journal.ReadLatest().Snapshot!.CompletedCleanup);
        }
    }

    [Fact] // R1-T15
    public void NotAttemptedWithUnavailableFallbackStaysUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true, ReadFault = true };
        var (journal, writer) = SeededWriter();
        using (writer)
        {
            var result = Run(backend, journal, writer, out var clock, out var cleanerCalls);

            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
            Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
            Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.JournalFinalization));
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);

            Assert.Equal(0, backend.WriteKillCalls);
            Assert.Equal(1, backend.ReadPopulationCalls);
            Assert.Equal(0, backend.RemoveChildCalls);
            Assert.Equal(0, cleanerCalls);
            Assert.Equal(0, clock.Delays);

            Assert.Equal((byte)CleanupComponents.DomainRemoval,
                journal.ReadLatest().Snapshot!.CompletedCleanup);
        }
    }

    private sealed class CountingCleaner : IAllocationCleaner
    {
        private readonly Action _onCall;

        internal CountingCleaner(Action onCall) => _onCall = onCall;

        public AllocationCleanResult CleanOne(AllocationEntry entry)
        {
            _onCall();
            return new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty);
        }
    }
}
