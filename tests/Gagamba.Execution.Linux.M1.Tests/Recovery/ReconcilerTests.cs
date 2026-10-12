// WP03 recovery-planner/reconciler tests: eligibility derived from the
// durable record, current boot and actual live lease — never caller
// booleans. Cross-boot refusal, restart-only quarantine, mismatch
// refusal, destructive cleanup only with live corroborated authority, and
// idempotence.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class ReconcilerTests
{
    private sealed class FakeCleaner : IAllocationCleaner
    {
        public int Calls;
        public bool Fail;

        public DomainIoResult CleanOne(AllocationEntry entry)
        {
            Calls++;
            return Fail ? DomainIoResult.Failure("clean fault") : DomainIoResult.Success;
        }
    }

    private static OwnershipSnapshot Record() => RecoveryTestSupport.Snapshot(
        allocations: new[] { new AllocationEntry(AllocationKind.Staging, "tool-000.staged", 10, new byte[32]) });

    private static Reconciler NewReconciler() => new(new FakeDomainClock(), TimeSpan.FromSeconds(5));

    private const string Boot = "boot-1";

    [Fact]
    public void CrossBootRefused()
    {
        var result = NewReconciler().Reconcile(Record(), "different-boot", null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void RestartOnlyQuarantinesWithoutAuthority()
    {
        var result = NewReconciler().Reconcile(Record(), Boot, null, null);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void MismatchedLeaseRefused()
    {
        // Record asserts child inode 999; the live lease is child inode 200.
        var record = Record() with { DomainInode = 999 };
        var lease = new DomainLease(new FakeDomainBackend(), RecoveryTestSupport.Identity());
        var result = NewReconciler().Reconcile(record, Boot, lease, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
    }

    [Fact]
    public void LiveCorroboratedAuthorityCleans()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner();
        var result = NewReconciler().Reconcile(Record(), Boot, lease, cleaner);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.True(result.DestructiveActionTaken);
        Assert.Equal(1, cleaner.Calls);
    }

    [Fact]
    public void CleanerFailureReportsFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var result = NewReconciler().Reconcile(Record(), Boot, lease, new FakeCleaner { Fail = true });
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void ReplacementRefusesRemoval()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true, Replacement = true, ReplacementInode = 999 };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var result = NewReconciler().Reconcile(Record(), Boot, lease, new FakeCleaner());
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void ReconcilerIsIdempotent()
    {
        var first = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease1 = new DomainLease(first, RecoveryTestSupport.Identity());
        var r1 = NewReconciler().Reconcile(Record(), Boot, lease1, new FakeCleaner());

        var second = new FakeDomainBackend { Populated = false };
        var lease2 = new DomainLease(second, RecoveryTestSupport.Identity());
        var r2 = NewReconciler().Reconcile(Record(), Boot, lease2, new FakeCleaner());

        Assert.Equal(ReconcileOutcome.CleanupConfirmed, r1.Outcome);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, r2.Outcome);
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
            // BootId "other" is cross-boot (Refused); the rest are mismatch (Refused).
            var result = NewReconciler().Reconcile(snapshot, Boot, lease, null);
            Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        }
    }
}
