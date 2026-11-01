// B5 failing-before regression. This test reproduces the documented C3
// violation against the pre-B5 tree: a persisted StopRecord whose absolute
// deadline is already long past is ignored, and reconciliation receives a
// fresh caller-supplied five-second allowance. It is retained after the fix
// as the permanent no-reset regression.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3FailingBeforeTests
{
    [Fact]
    public void CallerAllowanceCannotOverrideExpiredPersistedStopDeadline()
    {
        // The durable stop was accepted at 1s and its deadline was 6s.
        // Recovery now runs at 100s, i.e. the original window closed 94s ago.
        var stop = new StopRecord(
            StopAcceptedAtNanoseconds: 1_000_000_000,
            Cause: StopCauseKind.CallerStop,
            StopDeadlineNanoseconds: 6_000_000_000);

        var record = RecoveryTestSupport.V3Snapshot(
            stop: stop,
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);

        var clock = new FakeDomainClock { NowNanoseconds = 100_000_000_000 };
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var clockBefore = clock.NowNanoseconds;

        var result = new Reconciler(clock, TimeSpan.FromSeconds(5))
            .Reconcile(record, "boot-1", lease, null, null, null);

        // Pre-B5 the reconciler waited a fresh five seconds (200 x 25ms polls)
        // and reported an operation failure. The durable deadline must instead
        // authorise exactly one immediate kill-plus-observation with no wait.
        Assert.Equal(0, clock.Delays);
        Assert.Equal(clockBefore, clock.NowNanoseconds);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }
}
