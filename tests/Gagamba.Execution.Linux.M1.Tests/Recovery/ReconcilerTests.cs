// WP03 recovery-planner/reconciler tests: cross-boot refusal, restart-only
// quarantine, mismatch refusal, destructive cleanup only with live
// corroborated authority, and idempotence.
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

    [Fact]
    public void CrossBootRefused()
    {
        var authority = new RecoveryAuthority(
            BootMatches: false, MountMatches: true, HasLiveDomainLease: true, LiveLeaseCorroborates: true, HasCleanupAuthority: true);
        var result = NewReconciler().Reconcile(Record(), authority, null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void MountMismatchRefused()
    {
        var authority = new RecoveryAuthority(true, false, true, true, true);
        var result = NewReconciler().Reconcile(Record(), authority, null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
    }

    [Fact]
    public void RestartOnlyQuarantinesWithoutAuthority()
    {
        var authority = new RecoveryAuthority(true, true, HasLiveDomainLease: false, LiveLeaseCorroborates: false, true);
        var result = NewReconciler().Reconcile(Record(), authority, null, null);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(result.DestructiveActionTaken);
    }

    [Fact]
    public void MismatchedLeaseRefused()
    {
        var authority = new RecoveryAuthority(true, true, true, LiveLeaseCorroborates: false, true);
        var result = NewReconciler().Reconcile(Record(), authority, null, null);
        Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
    }

    [Fact]
    public void LiveCorroboratedAuthorityCleans()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner();
        var authority = new RecoveryAuthority(true, true, true, true, true);
        var result = NewReconciler().Reconcile(Record(), authority, lease, cleaner);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.True(result.DestructiveActionTaken);
        Assert.Equal(1, cleaner.Calls);
    }

    [Fact]
    public void CleanerFailureReportsFailed()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var cleaner = new FakeCleaner { Fail = true };
        var authority = new RecoveryAuthority(true, true, true, true, true);
        var result = NewReconciler().Reconcile(Record(), authority, lease, cleaner);
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void ReplacementRefusesRemoval()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = true, Replacement = true, ReplacementInode = 999 };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var authority = new RecoveryAuthority(true, true, true, true, true);
        var result = NewReconciler().Reconcile(Record(), authority, lease, new FakeCleaner());
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void ReconcilerIsIdempotent()
    {
        var authority = new RecoveryAuthority(true, true, true, true, true);
        var first = new FakeDomainBackend { Populated = true, KillAccepted = true };
        var lease1 = new DomainLease(first, RecoveryTestSupport.Identity());
        var r1 = NewReconciler().Reconcile(Record(), authority, lease1, new FakeCleaner());

        var second = new FakeDomainBackend { Populated = false };
        var lease2 = new DomainLease(second, RecoveryTestSupport.Identity());
        var r2 = NewReconciler().Reconcile(Record(), authority, lease2, new FakeCleaner());

        Assert.Equal(ReconcileOutcome.CleanupConfirmed, r1.Outcome);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, r2.Outcome);
    }
}
