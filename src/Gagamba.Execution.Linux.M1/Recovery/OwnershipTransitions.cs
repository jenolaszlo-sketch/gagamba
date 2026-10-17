// WP05 v3 pure snapshot-transition planners. These helpers preserve every
// existing durable fact and add exactly one authorized release/stop/observation
// fact. They never write release authority and never perform transport.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Recovery;

internal static class OwnershipTransitions
{
    internal static bool TryPlanReleaseIntent(
        OwnershipSnapshot current,
        long intentAtNanoseconds,
        long setupDeadlineNanoseconds,
        long overallDeadlineNanoseconds,
        out OwnershipSnapshot? next,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        next = null;
        error = string.Empty;
        if (current.Release is not null)
        {
            error = "release intent already exists";
            return false;
        }

        if (current.Stop is not null)
        {
            error = "release intent is forbidden after a durable stop";
            return false;
        }

        if (current.Exec is not null || current.Root is not null)
        {
            error = "release intent cannot follow observations";
            return false;
        }

        if (current.Stage != PreparationStage.Prepared || current.Preparation is null)
        {
            error = "release intent requires a Prepared snapshot";
            return false;
        }

        if (intentAtNanoseconds <= 0
            || setupDeadlineNanoseconds <= 0
            || overallDeadlineNanoseconds <= 0
            || setupDeadlineNanoseconds >= overallDeadlineNanoseconds)
        {
            error = "release deadlines are invalid";
            return false;
        }

        var candidate = current with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Release = new ReleaseRecord(
                ReleaseTag.Intent,
                intentAtNanoseconds,
                setupDeadlineNanoseconds,
                overallDeadlineNanoseconds),
        };
        return TryStampAndValidate(current, candidate, out next, out error);
    }

    internal static bool TryPlanReleaseCommitted(
        OwnershipSnapshot current,
        long committedAtNanoseconds,
        DeliveryResult deliveryResult,
        out OwnershipSnapshot? next,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        next = null;
        error = string.Empty;
        var release = current.Release;
        if (release is null || release.Tag != ReleaseTag.Intent)
        {
            error = "release commit requires a prior durable intent";
            return false;
        }

        if (deliveryResult == DeliveryResult.NotAttempted)
        {
            error = "release commit requires a concrete delivery result";
            return false;
        }

        if (committedAtNanoseconds <= 0 || committedAtNanoseconds < release.IntentAtNanoseconds)
        {
            error = "release commit timestamp is invalid";
            return false;
        }

        var candidate = current with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Release = release with
            {
                Tag = ReleaseTag.Committed,
                CommittedAtNanoseconds = committedAtNanoseconds,
                DeliveryResult = deliveryResult,
            },
        };
        return TryStampAndValidate(current, candidate, out next, out error);
    }

    internal static bool TryPlanStop(
        OwnershipSnapshot current,
        long acceptedAtNanoseconds,
        StopCauseKind cause,
        out OwnershipSnapshot? next,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        next = null;
        error = string.Empty;
        if (current.Stop is not null)
        {
            error = "the first accepted stop is immutable";
            return false;
        }

        if (acceptedAtNanoseconds <= 0)
        {
            error = "stop acceptance timestamp is invalid";
            return false;
        }

        long deadline;
        try
        {
            checked
            {
                deadline = acceptedAtNanoseconds
                    + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;
            }
        }
        catch (OverflowException)
        {
            error = "stop deadline overflows";
            return false;
        }

        var candidate = current with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Stop = new StopRecord(acceptedAtNanoseconds, cause, deadline),
        };
        return TryStampAndValidate(current, candidate, out next, out error);
    }

    internal static bool TryPlanExecObservation(
        OwnershipSnapshot current,
        ExecObservation observation,
        out OwnershipSnapshot? next,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(observation);
        next = null;
        error = string.Empty;
        if (current.Release is null || current.Release.Tag != ReleaseTag.Committed)
        {
            error = "exec observation requires a committed release";
            return false;
        }

        if (current.Exec is not null)
        {
            if (current.Exec == observation)
            {
                next = current; // already durable: idempotent no-op, do not write
                return true;
            }

            error = "exec observation is single-assignment";
            return false;
        }

        if (current.Root is not null)
        {
            error = "root observation cannot precede exec";
            return false;
        }

        if (observation.Tag == ExecTag.NotAttempted)
        {
            error = "exec observation requires a non-not-attempted tag";
            return false;
        }

        var candidate = current with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Exec = observation,
        };
        return TryStampAndValidate(current, candidate, out next, out error);
    }

    internal static bool TryPlanRootObservation(
        OwnershipSnapshot current,
        RootObservation observation,
        out OwnershipSnapshot? next,
        out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(observation);
        next = null;
        error = string.Empty;
        if (current.Release is null || current.Release.Tag != ReleaseTag.Committed)
        {
            error = "root observation requires a committed release";
            return false;
        }

        if (current.Exec is null)
        {
            error = "root observation requires an exec observation";
            return false;
        }

        if (current.Root is not null)
        {
            if (current.Root == observation)
            {
                next = current; // already durable: idempotent no-op, do not write
                return true;
            }

            error = "root observation is single-assignment";
            return false;
        }

        var candidate = current with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Root = observation,
        };
        return TryStampAndValidate(current, candidate, out next, out error);
    }

    private static bool TryStampAndValidate(
        OwnershipSnapshot current,
        OwnershipSnapshot candidate,
        out OwnershipSnapshot? next,
        out string error)
    {
        next = null;
        long sequence;
        try
        {
            checked { sequence = current.Sequence + 1; }
        }
        catch (OverflowException)
        {
            error = "sequence overflow";
            return false;
        }

        var stamped = candidate with { Sequence = sequence };
        if (!OwnershipTransitionValidator.TryValidate(current, stamped, out error))
        {
            return false;
        }

        next = stamped;
        return true;
    }
}
