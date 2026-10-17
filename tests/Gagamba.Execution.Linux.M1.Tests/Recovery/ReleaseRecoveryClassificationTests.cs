// WP05 durable recovery classification tests: release disposition stays
// conservative after owner loss; trusted observations narrow exec/root/outcome;
// cleanup remains independent. No runtime orchestration is exercised.
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class ReleaseRecoveryClassificationTests
{
    [Fact]
    public void NoReleaseIsNotCommittedAndNotStarted()
    {
        var classification = ReleaseRecoveryClassifier.Classify(RecoveryTestSupport.Snapshot());
        Assert.Equal(ReleaseDisposition.NotCommitted, classification.Release);
        Assert.Equal(ExecDisposition.NotAttempted, classification.Exec);
        Assert.Equal(RootResultKind.NotStarted, classification.Root);
        Assert.Equal(ExecutionOutcome.FailedToEstablish, classification.Outcome);
        Assert.Equal(OwnershipCleanupState.NotStarted, classification.Cleanup);
    }

    [Fact]
    public void StopBeforeReleaseIsCancelledWhenCallerCaused()
    {
        var snapshot = RecoveryTestSupport.Snapshot() with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Stop = RecoveryTestSupport.Stop(cause: StopCauseKind.CallerStop),
        };
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.NotCommitted, classification.Release);
        Assert.Equal(ExecutionOutcome.CancelledBeforeRelease, classification.Outcome);
        Assert.Equal(RootResultKind.NotStarted, classification.Root);
    }

    [Fact]
    public void IntentIsMayHaveExecutedAndUnknown()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent());
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(ExecDisposition.NotAttempted, classification.Exec);
        Assert.Equal(RootResultKind.Unknown, classification.Root);
        Assert.Equal(ExecutionOutcome.Unknown, classification.Outcome);
    }

    [Fact]
    public void CommittedWithoutObservationRemainsMayHaveExecutedUnknown()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted());
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(DeliveryResult.Delivered, classification.Delivery);
        Assert.Equal(ExecDisposition.NotAttempted, classification.Exec);
        Assert.Equal(RootResultKind.Unknown, classification.Root);
        Assert.Equal(ExecutionOutcome.Unknown, classification.Outcome);
    }

    [Fact]
    public void FailedBeforeExecEstablishesNotStarted()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecFailed(2));
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(ExecDisposition.FailedBeforeExec, classification.Exec);
        Assert.Equal(2, classification.ExecErrno);
        Assert.Equal(RootResultKind.NotStarted, classification.Root);
        Assert.Equal(ExecutionOutcome.FailedToEstablish, classification.Outcome);
    }

    [Fact]
    public void ConfirmedWithUnknownRootStaysUnknown()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootUnknown());
        var classification = ReleaseRecoveryClassifier.Classify(
            snapshot, domainEmpty: true, captureComplete: true);
        Assert.Equal(ExecDisposition.Confirmed, classification.Exec);
        Assert.Equal(RootResultKind.Unknown, classification.Root);
        Assert.Equal(ExecutionOutcome.Unknown, classification.Outcome);
    }

    [Fact]
    public void ConfirmedExitedRequiresCompletionFactsForAggregateOutcome()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited(0));
        var incomplete = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(RootResultKind.Exited, incomplete.Root);
        Assert.Equal(ExecutionOutcome.Unknown, incomplete.Outcome);

        var complete = ReleaseRecoveryClassifier.Classify(
            snapshot, domainEmpty: true, captureComplete: true);
        Assert.Equal(ExecutionOutcome.Succeeded, complete.Outcome);
    }

    [Fact]
    public void ConfirmedNonzeroAndSignalledOutcomes()
    {
        var nonzero = ReleaseRecoveryClassifier.Classify(
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootExited(7)),
            domainEmpty: true, captureComplete: true);
        Assert.Equal(ExecutionOutcome.TargetFailed, nonzero.Outcome);
        Assert.Equal(7, nonzero.ExitCode);

        var signalled = ReleaseRecoveryClassifier.Classify(
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootSignalled(15)),
            domainEmpty: true, captureComplete: true);
        Assert.Equal(RootResultKind.Signalled, signalled.Root);
        Assert.Equal(15, signalled.Signal);
        Assert.Equal(ExecutionOutcome.TargetFailed, signalled.Outcome);
    }

    [Fact]
    public void StopAfterCommitIsStoppedAndPreservesRelease()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited(0));
        var classification = ReleaseRecoveryClassifier.Classify(
            snapshot, domainEmpty: true, captureComplete: true);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(RootResultKind.Exited, classification.Root);
        Assert.Equal(ExecutionOutcome.Stopped, classification.Outcome);
    }

    [Fact]
    public void CleanupConfirmedDoesNotRewriteReleaseOrExecution()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent(),
            cleanup: OwnershipCleanupState.Confirmed,
            completedCleanup: 0x1F);
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(ExecutionOutcome.Unknown, classification.Outcome);
        Assert.Equal(OwnershipCleanupState.Confirmed, classification.Cleanup);
    }

    [Fact]
    public void TimestampsDoNotDecideReleaseStopWinner()
    {
        var stopBeforeIntent = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent(intentAt: 5_000),
            stop: RecoveryTestSupport.Stop(acceptedAt: 2_000));
        var intentBeforeStop = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent(intentAt: 1_000),
            stop: RecoveryTestSupport.Stop(acceptedAt: 2_000));

        var first = ReleaseRecoveryClassifier.Classify(stopBeforeIntent);
        var second = ReleaseRecoveryClassifier.Classify(intentBeforeStop);
        Assert.Equal(first.Release, second.Release);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, first.Release);
        Assert.NotEqual(ReleaseDisposition.NotCommitted, first.Release);
    }
}
