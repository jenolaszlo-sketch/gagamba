// WP05 durable recovery classification. This is a pure projection of the
// durable snapshot, not owner/coordinator orchestration and not release
// authority. Release disposition stays conservative after owner loss;
// trusted observations narrow exec/root/target status. Cleanup remains a
// separate field and never rewrites execution history.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Recovery;

internal sealed record DurableRecoveryClassification(
    ReleaseDisposition Release,
    DeliveryResult? Delivery,
    ExecDisposition Exec,
    int ExecErrno,
    RootResultKind Root,
    int ExitCode,
    int Signal,
    bool CoreDumped,
    ExecutionOutcome Outcome,
    OwnershipCleanupState Cleanup);

internal static class ReleaseRecoveryClassifier
{
    /// <summary>Classifies durable facts only. <paramref name="domainEmpty"/>
    /// and <paramref name="captureComplete"/> are separate completion facts;
    /// without them a known target result does not become an aggregate
    /// Succeeded/TargetFailed outcome.</summary>
    internal static DurableRecoveryClassification Classify(
        OwnershipSnapshot snapshot,
        bool domainEmpty = false,
        bool captureComplete = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        OwnershipSnapshotValidator.Validate(snapshot);

        var release = snapshot.Release is null
            ? ReleaseDisposition.NotCommitted
            : ReleaseDisposition.MayHaveExecuted;
        var exec = snapshot.Exec switch
        {
            null => ExecDisposition.NotAttempted,
            { Tag: ExecTag.FailedBeforeExec } => ExecDisposition.FailedBeforeExec,
            { Tag: ExecTag.Confirmed } => ExecDisposition.Confirmed,
            _ => ExecDisposition.Unknown,
        };
        var root = snapshot.Root is not null
            ? (RootResultKind)(byte)snapshot.Root.Kind
            : snapshot.Exec?.Tag == ExecTag.FailedBeforeExec || snapshot.Release is null
                ? RootResultKind.NotStarted
                : RootResultKind.Unknown;

        var outcome = ClassifyOutcome(snapshot, exec, root, domainEmpty, captureComplete);
        return new DurableRecoveryClassification(
            release,
            snapshot.Release?.DeliveryResult,
            exec,
            snapshot.Exec?.Errno ?? 0,
            root,
            snapshot.Root?.ExitCode ?? 0,
            snapshot.Root?.Signal ?? 0,
            snapshot.Root?.CoreDumped ?? false,
            outcome,
            snapshot.Cleanup);
    }

    private static ExecutionOutcome ClassifyOutcome(
        OwnershipSnapshot snapshot,
        ExecDisposition exec,
        RootResultKind root,
        bool domainEmpty,
        bool captureComplete)
    {
        if (snapshot.Release is null)
        {
            return snapshot.Stop?.Cause is StopCauseKind.CallerStop or StopCauseKind.CallerLoss
                ? ExecutionOutcome.CancelledBeforeRelease
                : ExecutionOutcome.FailedToEstablish;
        }

        if (snapshot.Release.Tag != ReleaseTag.Committed)
        {
            return ExecutionOutcome.Unknown;
        }

        if (exec == ExecDisposition.FailedBeforeExec)
        {
            return ExecutionOutcome.FailedToEstablish;
        }

        if (exec != ExecDisposition.Confirmed)
        {
            return ExecutionOutcome.Unknown;
        }

        if (root is not (RootResultKind.Exited or RootResultKind.Signalled))
        {
            return ExecutionOutcome.Unknown;
        }

        if (snapshot.Stop is not null)
        {
            return ExecutionOutcome.Stopped;
        }

        if (!domainEmpty || !captureComplete)
        {
            return ExecutionOutcome.Unknown;
        }

        return root == RootResultKind.Exited && snapshot.Root!.ExitCode == 0
            ? ExecutionOutcome.Succeeded
            : ExecutionOutcome.TargetFailed;
    }
}
