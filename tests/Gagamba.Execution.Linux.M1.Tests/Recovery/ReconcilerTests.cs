// WP03 reconciler tests: component-wise truthful cleanup. Every required
// component is evaluated from live authority and actual outcomes; a
// missing cleaner or missing authority yields Unknown debt, never assumed
// success. Progress persists monotonically under the writer lease.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class ReconcilerTests
{
    private sealed class FakeCleaner : IAllocationCleaner
    {
        public int Calls;
        public AllocationCleanOutcome Outcome = AllocationCleanOutcome.Cleaned;

        public AllocationCleanResult CleanOne(AllocationEntry entry)
        {
            Calls++;
            return Outcome switch
            {
                AllocationCleanOutcome.Cleaned => new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty),
                AllocationCleanOutcome.AlreadyAbsent => new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty),
                AllocationCleanOutcome.Mismatch => new AllocationCleanResult(AllocationCleanOutcome.Mismatch, "identity mismatch"),
                _ => new AllocationCleanResult(AllocationCleanOutcome.Failed, "clean fault"),
            };
        }
    }

    private static OwnershipSnapshot Record() => RecoveryTestSupport.Snapshot(
        allocations: new[] { new AllocationEntry(AllocationKind.Staging, "tool-000.staged", 10, new byte[32], 0xABCD, 200, true) });

    private static Reconciler NewReconciler() => new(new FakeDomainClock(), TimeSpan.FromSeconds(5));

    private const string Boot = "boot-1";

    private static (OwnershipJournal Journal, JournalWriterLease Writer) NewWriter()
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!);
    }

    [Fact]
    public void CrossBootRefused()
    {
        var result = NewReconciler().Reconcile(Record(), "different-boot", null, null, null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void RestartOnlyQuarantinesWithoutAuthority()
    {
        var result = NewReconciler().Reconcile(Record(), Boot, null, null, null, null);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void MismatchedLeaseRefused()
    {
        var record = Record() with { DomainInode = 999 };
        var lease = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        var result = NewReconciler().Reconcile(record, Boot, lease, null, null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
    }

    [Fact]
    public void LiveCorroboratedAuthorityCleans()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner();
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = NewReconciler().Reconcile(Record(), Boot, lease, cleaner, journal, writer);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.True(result.DestructiveActionTaken);
            Assert.Equal(1, cleaner.Calls);
            Assert.All(result.Components!, c => Assert.Equal(ComponentState.Confirmed, c.State));
        }
    }

    [Fact]
    public void AstraNegativeStagingWithoutAuthorityIsUnknown()
    {
        // The exact G2-08 negative: a record with a staging allocation, no
        // cleanup authority and no cleaner must not return Confirmed.
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = NewReconciler().Reconcile(Record(), Boot, lease, null, journal, writer);
            Assert.NotEqual(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
            var staging = Assert.Single(result.Components!, c => c.Component == CleanupComponents.Staging);
            Assert.Equal(ComponentState.Unknown, staging.State);
        }
    }

    [Fact]
    public void StagingMismatchPreservesReplacementAndStaysUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner { Outcome = AllocationCleanOutcome.Mismatch };
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = NewReconciler().Reconcile(Record(), Boot, lease, cleaner, journal, writer);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
            var staging = Assert.Single(result.Components!, c => c.Component == CleanupComponents.Staging);
            Assert.Equal(ComponentState.Unknown, staging.State);
        }
    }

    [Fact]
    public void StagingFailureReportsFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner { Outcome = AllocationCleanOutcome.Failed };
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = NewReconciler().Reconcile(Record(), Boot, lease, cleaner, journal, writer);
            Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
        }
    }

    [Fact]
    public void CleanerFailureReportsFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var result = NewReconciler().Reconcile(Record(), Boot, lease, new FakeCleaner { Outcome = AllocationCleanOutcome.Failed }, null, null);
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void ReplacementRefusesRemoval()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true, Replacement = true, ReplacementInode = 999 };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            var result = NewReconciler().Reconcile(Record(), Boot, lease, new FakeCleaner(), journal, writer);
            Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        }
    }

    [Fact]
    public void CompletedProgressMakesRepeatIdempotent()
    {
        var (journal, writer) = NewWriter();
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner();
        ReconcileOutcome firstOutcome;
        using (writer)
        {
            firstOutcome = NewReconciler().Reconcile(Record(), Boot, lease, cleaner, journal, writer).Outcome;
        }

        Assert.Equal(ReconcileOutcome.CleanupConfirmed, firstOutcome);

        // Second reconciliation with fresh authority replays persisted
        // progress instead of redoing destructive work.
        var backend2 = new FakeDomainBackend { Populated = false };
        var lease2 = new DomainLease(backend2, RecoveryTestSupport.Identity());
        var cleaner2 = new FakeCleaner();
        using var writer2 = AcquireFresh(journal);
        var second = NewReconciler().Reconcile(journal.ReadLatest().Snapshot!, Boot, lease2, cleaner2, journal, writer2);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, second.Outcome);
        Assert.Equal(0, cleaner2.Calls);
        Assert.Equal(0, backend2.KillWrites);
    }

    [Fact]
    public void PartialProgressResumesWithoutRedoingConfirmedWork()
    {
        var (journal, writer) = NewWriter();
        // First attempt: stop succeeds, staging cleaner fails.
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        ReconcileOutcome firstOutcome;
        using (writer)
        {
            firstOutcome = NewReconciler().Reconcile(
                Record(), Boot, lease, new FakeCleaner { Outcome = AllocationCleanOutcome.Failed }, journal, writer).Outcome;
        }

        Assert.Equal(ReconcileOutcome.Failed, firstOutcome);

        // Second attempt with a working cleaner resumes: stop/empty are not
        // redone destructively, staging completes, removal completes.
        var backend2 = new FakeDomainBackend { Populated = false };
        var lease2 = new DomainLease(backend2, RecoveryTestSupport.Identity());
        var cleaner2 = new FakeCleaner();
        using var writer2 = AcquireFresh(journal);
        var second = NewReconciler().Reconcile(journal.ReadLatest().Snapshot!, Boot, lease2, cleaner2, journal, writer2);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, second.Outcome);
        Assert.Equal(0, backend2.KillWrites);
        Assert.Equal(1, cleaner2.Calls);
    }

    [Fact]
    public void MismatchDebtPersistsAcrossRepeats()
    {
        var (journal, writer) = NewWriter();
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        ReconcileOutcome firstOutcome;
        using (writer)
        {
            firstOutcome = NewReconciler().Reconcile(
                Record(), Boot, lease, new FakeCleaner { Outcome = AllocationCleanOutcome.Mismatch }, journal, writer).Outcome;
        }

        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, firstOutcome);
        using var writer2 = AcquireFresh(journal);
        var second = NewReconciler().Reconcile(
            journal.ReadLatest().Snapshot!, Boot,
            new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity()),
            new FakeCleaner { Outcome = AllocationCleanOutcome.Mismatch }, journal, writer2);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, second.Outcome);
    }

    [Fact]
    public void EveryIdentityFieldMismatchRefuses()
    {
        var snapshots = new[]
        {
            Record() with { ExecutionId = Guid.NewGuid() },
            Record() with { BootId = "other" },
            Record() with { CgroupMountId = 999 },
            Record() with { CgroupMountFsType = "tmpfs" },
            Record() with { ParentDevice = 0xDEAD },
            Record() with { ParentInode = 999 },
            Record() with { DomainName = "other" },
            Record() with { DomainDevice = 0xDEAD },
            Record() with { DomainInode = 999 },
            Record() with { LeaseGeneration = Guid.NewGuid() },
        };
        foreach (var snapshot in snapshots)
        {
            var lease = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
            var result = NewReconciler().Reconcile(snapshot, Boot, lease, null, null, null);
            Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        }
    }

    private static JournalWriterLease AcquireFresh(OwnershipJournal journal)
    {
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return lease!;
    }
}
