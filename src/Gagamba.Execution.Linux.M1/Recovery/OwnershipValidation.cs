// WP05 v3 durable schema validation. The schema is evidence, never release
// authority. These validators reject malformed, contradictory or regressing
// records at the decode and transition boundaries, without introducing a
// second journal or any release transport.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Recovery;

internal static class OwnershipSnapshotValidator
{
    internal static void Validate(OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!TryValidate(snapshot, out var error))
        {
            throw new ArgumentException(error, nameof(snapshot));
        }
    }

    internal static bool TryValidate(OwnershipSnapshot snapshot, out string error)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        error = string.Empty;
        if (snapshot.Version is not (OwnershipSnapshotCodec.LegacyVersion or OwnershipSnapshotCodec.CurrentVersion))
        {
            error = $"unsupported snapshot version {snapshot.Version}";
            return false;
        }

        if (snapshot.Sequence < 0)
        {
            error = "snapshot sequence is negative";
            return false;
        }

        if (snapshot.Version == OwnershipSnapshotCodec.LegacyVersion
            && (snapshot.Release is not null || snapshot.Stop is not null
                || snapshot.Exec is not null || snapshot.Root is not null))
        {
            error = "version 2 cannot carry release, stop or observation facts";
            return false;
        }

        if (snapshot.Allocations.Count > OwnershipSnapshotCodec.MaxAllocations)
        {
            error = "allocation count out of bounds";
            return false;
        }

        if (snapshot.Release is not null)
        {
            if (!TryValidateRelease(snapshot, out error)) return false;
        }
        else if (snapshot.Exec is not null || snapshot.Root is not null)
        {
            error = "exec/root observations require a committed release";
            return false;
        }

        if (snapshot.Stop is not null && !TryValidateStop(snapshot.Stop, out error))
        {
            return false;
        }

        if (snapshot.Exec is not null && !TryValidateExec(snapshot, out error))
        {
            return false;
        }

        if (snapshot.Root is not null && !TryValidateRoot(snapshot, out error))
        {
            return false;
        }

        return true;
    }

    private static bool TryValidateRelease(OwnershipSnapshot snapshot, out string error)
    {
        error = string.Empty;
        var release = snapshot.Release!;
        if (snapshot.Stage != PreparationStage.Prepared || snapshot.Preparation is null)
        {
            error = "release facts require a completed Prepared snapshot";
            return false;
        }

        if (snapshot.Preparation.ExecutionId != snapshot.ExecutionId)
        {
            error = "release preparation execution identity does not match the snapshot";
            return false;
        }

        if (snapshot.Preparation.ManifestDigest.Length != 32)
        {
            error = "release preparation digest is not 32 bytes";
            return false;
        }

        if (release.Tag is not (ReleaseTag.Intent or ReleaseTag.Committed))
        {
            error = "invalid release tag";
            return false;
        }

        if (release.IntentAtNanoseconds <= 0
            || release.SetupDeadlineNanoseconds <= 0
            || release.OverallDeadlineNanoseconds <= 0
            || release.SetupDeadlineNanoseconds >= release.OverallDeadlineNanoseconds)
        {
            error = "release deadlines are invalid";
            return false;
        }

        if (release.Tag == ReleaseTag.Intent)
        {
            if (release.CommittedAtNanoseconds is not null
                || release.DeliveryResult != DeliveryResult.NotAttempted)
            {
                error = "release intent cannot carry committed fields";
                return false;
            }
        }
        else
        {
            if (release.CommittedAtNanoseconds is null
                || release.CommittedAtNanoseconds.Value <= 0
                || release.CommittedAtNanoseconds.Value < release.IntentAtNanoseconds)
            {
                error = "committed release timestamp is invalid";
                return false;
            }

            if (!IsDefinedDelivery(release.DeliveryResult)
                || release.DeliveryResult == DeliveryResult.NotAttempted)
            {
                error = "committed release requires a concrete delivery result";
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateStop(StopRecord stop, out string error)
    {
        error = string.Empty;
        if (stop.StopAcceptedAtNanoseconds <= 0 || stop.StopDeadlineNanoseconds <= 0)
        {
            error = "stop timestamps are invalid";
            return false;
        }

        if (!IsDefinedStopCause(stop.Cause))
        {
            error = "invalid stop cause";
            return false;
        }

        long expected;
        try
        {
            checked
            {
                expected = stop.StopAcceptedAtNanoseconds
                    + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;
            }
        }
        catch (OverflowException)
        {
            error = "stop deadline overflows";
            return false;
        }

        if (stop.StopDeadlineNanoseconds != expected)
        {
            error = "stop deadline does not match the five-second allowance";
            return false;
        }

        return true;
    }

    private static bool TryValidateExec(OwnershipSnapshot snapshot, out string error)
    {
        error = string.Empty;
        var release = snapshot.Release;
        var exec = snapshot.Exec!;
        if (release is null || release.Tag != ReleaseTag.Committed)
        {
            error = "exec observation requires a committed release";
            return false;
        }

        if (!IsDefinedExecTag(exec.Tag) || exec.Tag == ExecTag.NotAttempted)
        {
            error = "invalid exec tag";
            return false;
        }

        switch (exec.Tag)
        {
            case ExecTag.FailedBeforeExec:
                if (exec.Errno <= 0 || exec.Basis != ExecBasis.None)
                {
                    error = "failed-before-exec requires a positive errno and no basis";
                    return false;
                }

                if (!IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "failed-before-exec requires delivered or unknown transport evidence";
                    return false;
                }

                break;
            case ExecTag.Confirmed:
                if (exec.Errno != 0
                    || exec.Basis is not (ExecBasis.CoordinatorFrame
                        or ExecBasis.ExecutableObjectIdentity
                        or ExecBasis.ReviewedNormalExitRule))
                {
                    error = "confirmed exec requires a valid basis and no errno";
                    return false;
                }

                if (!IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "confirmed exec requires delivered or unknown transport evidence";
                    return false;
                }

                break;
            case ExecTag.Unknown:
                if (exec.Errno != 0 || exec.Basis != ExecBasis.None)
                {
                    error = "unknown exec carries no errno or basis";
                    return false;
                }

                break;
        }

        return true;
    }

    private static bool TryValidateRoot(OwnershipSnapshot snapshot, out string error)
    {
        error = string.Empty;
        var release = snapshot.Release;
        var exec = snapshot.Exec;
        var root = snapshot.Root!;
        if (release is null || release.Tag != ReleaseTag.Committed || exec is null)
        {
            error = "root observation requires a committed release and exec observation";
            return false;
        }

        if (!IsDefinedRootKind(root.Kind))
        {
            error = "invalid root kind";
            return false;
        }

        if (root.RawWaitPresent && root.RawWaitStatus < 0)
        {
            error = "raw wait status is negative";
            return false;
        }

        switch (root.Kind)
        {
            case RootKind.Unknown:
                if (exec.Tag is not (ExecTag.Confirmed or ExecTag.Unknown))
                {
                    error = "unknown root requires confirmed or unknown exec";
                    return false;
                }

                if (exec.Tag == ExecTag.Confirmed && !IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "confirmed exec with unknown root requires delivered or unknown transport evidence";
                    return false;
                }

                if (root.ExitCode != 0 || root.Signal != 0 || root.CoreDumped)
                {
                    error = "unknown root cannot carry target exit/signal data";
                    return false;
                }

                break;
            case RootKind.NotStarted:
                if (exec.Tag != ExecTag.FailedBeforeExec)
                {
                    error = "not-started root requires failed-before-exec";
                    return false;
                }

                if (!IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "not-started root requires delivered or unknown transport evidence";
                    return false;
                }

                if (root.ExitCode != 0 || root.Signal != 0 || root.CoreDumped)
                {
                    error = "not-started root cannot carry target exit/signal data";
                    return false;
                }

                break;
            case RootKind.Exited:
                if (exec.Tag != ExecTag.Confirmed)
                {
                    error = "exited root requires confirmed exec";
                    return false;
                }

                if (!IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "exited root requires delivered or unknown transport evidence";
                    return false;
                }

                if (root.ExitCode is < 0 or > 255 || root.Signal != 0 || root.CoreDumped)
                {
                    error = "exited root has invalid exit/signal data";
                    return false;
                }

                if (root.RawWaitPresent && !MatchesExited(root.RawWaitStatus, root.ExitCode))
                {
                    error = "raw wait does not match the exited root";
                    return false;
                }

                break;
            case RootKind.Signalled:
                if (exec.Tag != ExecTag.Confirmed)
                {
                    error = "signalled root requires confirmed exec";
                    return false;
                }

                if (!IsTrustedStatusDelivery(release.DeliveryResult))
                {
                    error = "signalled root requires delivered or unknown transport evidence";
                    return false;
                }

                if (root.Signal is < 1 or > 64 || root.ExitCode != 0)
                {
                    error = "signalled root has invalid signal data";
                    return false;
                }

                if (root.RawWaitPresent && !MatchesSignalled(root.RawWaitStatus, root.Signal, root.CoreDumped))
                {
                    error = "raw wait does not match the signalled root";
                    return false;
                }

                break;
        }

        if (exec.Basis == ExecBasis.ReviewedNormalExitRule && root.Kind != RootKind.Exited)
        {
            error = "reviewed normal-exit confirmation requires an exited root";
            return false;
        }

        return true;
    }

    private static bool IsDefinedDelivery(DeliveryResult value) =>
        (byte)value is >= (byte)DeliveryResult.NotAttempted and <= (byte)DeliveryResult.Unknown;

    private static bool IsDefinedStopCause(StopCauseKind value) =>
        (byte)value is >= (byte)StopCauseKind.CallerStop and <= (byte)StopCauseKind.TransportFailure;

    private static bool IsDefinedExecTag(ExecTag value) =>
        (byte)value is >= (byte)ExecTag.NotAttempted and <= (byte)ExecTag.Unknown;

    private static bool IsDefinedRootKind(RootKind value) =>
        (byte)value is >= (byte)RootKind.Unknown and <= (byte)RootKind.Signalled;

    private static bool IsTrustedStatusDelivery(DeliveryResult value) =>
        value is DeliveryResult.Delivered or DeliveryResult.Unknown;

    private static bool MatchesExited(int rawWait, int exitCode) =>
        (rawWait & 0x7f) == 0 && ((rawWait >> 8) & 0xff) == exitCode;

    private static bool MatchesSignalled(int rawWait, int signal, bool coreDumped) =>
        (rawWait & 0x7f) == signal && (((rawWait & 0x80) != 0) == coreDumped);
}

internal static class OwnershipTransitionValidator
{
    internal static bool TryValidate(OwnershipSnapshot previous, OwnershipSnapshot next, out string error)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(next);
        error = string.Empty;
        if (!OwnershipSnapshotValidator.TryValidate(previous, out error))
        {
            error = $"previous snapshot invalid: {error}";
            return false;
        }

        if (!OwnershipSnapshotValidator.TryValidate(next, out error))
        {
            error = $"next snapshot invalid: {error}";
            return false;
        }

        long expected;
        try
        {
            checked { expected = previous.Sequence + 1; }
        }
        catch (OverflowException)
        {
            error = "sequence overflow";
            return false;
        }

        if (next.Sequence != expected)
        {
            error = $"sequence must advance by exactly one (expected {expected}, got {next.Sequence})";
            return false;
        }

        if (OwnershipSnapshotCodec.WireVersion(next) < OwnershipSnapshotCodec.WireVersion(previous))
        {
            error = "durable schema version must not regress";
            return false;
        }

        if (!IdentityEqual(previous, next, out error))
        {
            return false;
        }

        if ((next.CompletedCleanup & previous.CompletedCleanup) != previous.CompletedCleanup)
        {
            error = "completed-cleanup facts must not regress";
            return false;
        }

        var releaseChanged = !Equals(previous.Release, next.Release);
        var stopChanged = !Equals(previous.Stop, next.Stop);
        var execChanged = !Equals(previous.Exec, next.Exec);
        var rootChanged = !Equals(previous.Root, next.Root);
        var cleanupChanged = previous.Cleanup != next.Cleanup
            || previous.CompletedCleanup != next.CompletedCleanup;
        var factChanges = (releaseChanged ? 1 : 0) + (stopChanged ? 1 : 0)
            + (execChanged ? 1 : 0) + (rootChanged ? 1 : 0);

        if (factChanges == 0)
        {
            if (!cleanupChanged)
            {
                error = "transition changes no durable fact";
                return false;
            }

            return true;
        }

        if (factChanges != 1)
        {
            error = "exactly one release/stop/observation fact may change per transition";
            return false;
        }

        if (cleanupChanged)
        {
            error = "cleanup progress and a release/stop/observation fact require separate transitions";
            return false;
        }

        if (releaseChanged)
        {
            return ValidateReleaseTransition(previous, next, out error);
        }

        if (stopChanged)
        {
            if (previous.Stop is not null)
            {
                error = "the first accepted stop is immutable";
                return false;
            }

            return true;
        }

        if (execChanged)
        {
            if (previous.Exec is not null)
            {
                error = "exec observation is single-assignment and cannot change";
                return false;
            }

            if (previous.Release is null || previous.Release.Tag != ReleaseTag.Committed)
            {
                error = "exec observation requires a committed release";
                return false;
            }

            if (previous.Root is not null)
            {
                error = "root observation cannot precede exec";
                return false;
            }

            return true;
        }

        if (previous.Root is not null)
        {
            error = "root observation is single-assignment and cannot change";
            return false;
        }

        if (previous.Exec is null
            || previous.Release is null
            || previous.Release.Tag != ReleaseTag.Committed)
        {
            error = "root observation requires a committed release and exec observation";
            return false;
        }

        return true;
    }

    private static bool ValidateReleaseTransition(
        OwnershipSnapshot previous, OwnershipSnapshot next, out string error)
    {
        error = string.Empty;
        if (previous.Release is null)
        {
            if (next.Release!.Tag != ReleaseTag.Intent)
            {
                error = "committed release requires a prior acknowledged intent";
                return false;
            }

            if (previous.Stop is not null)
            {
                error = "release intent is forbidden after a durable stop";
                return false;
            }

            return true;
        }

        if (next.Release is null)
        {
            error = "release facts must not disappear";
            return false;
        }

        if (previous.Release.Tag == ReleaseTag.Committed)
        {
            error = "committed release is immutable";
            return false;
        }

        if (next.Release.Tag != ReleaseTag.Committed)
        {
            error = "release intent is immutable and cannot become absent or change identity";
            return false;
        }

        if (next.Release.IntentAtNanoseconds != previous.Release.IntentAtNanoseconds
            || next.Release.SetupDeadlineNanoseconds != previous.Release.SetupDeadlineNanoseconds
            || next.Release.OverallDeadlineNanoseconds != previous.Release.OverallDeadlineNanoseconds)
        {
            error = "release identity/deadlines are immutable";
            return false;
        }

        return true;
    }

    private static bool IdentityEqual(OwnershipSnapshot previous, OwnershipSnapshot next, out string error)
    {
        error = string.Empty;
        if (previous.ExecutionId != next.ExecutionId
            || !string.Equals(previous.BootId, next.BootId, StringComparison.Ordinal)
            || previous.CgroupMountId != next.CgroupMountId
            || !string.Equals(previous.CgroupMountFsType, next.CgroupMountFsType, StringComparison.Ordinal)
            || previous.ParentDevice != next.ParentDevice
            || previous.ParentInode != next.ParentInode
            || !string.Equals(previous.DomainName, next.DomainName, StringComparison.Ordinal)
            || previous.DomainDevice != next.DomainDevice
            || previous.DomainInode != next.DomainInode
            || previous.LeaseGeneration != next.LeaseGeneration
            || previous.Writer != next.Writer
            || previous.Stage != next.Stage
            || previous.StagingParentDevice != next.StagingParentDevice
            || previous.StagingParentInode != next.StagingParentInode)
        {
            error = "durable identity fields must not change";
            return false;
        }

        if (!PreparationEqual(previous.Preparation, next.Preparation))
        {
            error = "RecordedPreparation identity must not change";
            return false;
        }

        if (previous.Allocations.Count != next.Allocations.Count)
        {
            error = "allocation inventory must not change";
            return false;
        }

        for (var index = 0; index < previous.Allocations.Count; index++)
        {
            var left = previous.Allocations[index];
            var right = next.Allocations[index];
            if (left.Kind != right.Kind
                || !string.Equals(left.Name, right.Name, StringComparison.Ordinal)
                || left.Length != right.Length
                || !left.Digest.AsSpan().SequenceEqual(right.Digest)
                || left.Device != right.Device
                || left.Inode != right.Inode
                || left.Confirmed != right.Confirmed)
            {
                error = "allocation inventory must not change";
                return false;
            }
        }

        return true;
    }

    private static bool PreparationEqual(RecordedPreparation? left, RecordedPreparation? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return left.ExecutionId == right.ExecutionId
            && left.IssuerId == right.IssuerId
            && left.GenerationId == right.GenerationId
            && left.PreparationId == right.PreparationId
            && left.ManifestDigest.AsSpan().SequenceEqual(right.ManifestDigest);
    }
}
