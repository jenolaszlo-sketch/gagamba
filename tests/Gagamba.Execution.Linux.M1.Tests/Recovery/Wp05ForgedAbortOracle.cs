// WP05 qualification-harness oracle for the forged-identity abort path.
// This is test-only logic. It validates the accepted abort contract with
// structured kernel termination evidence and authorized-stop provenance, not
// an undocumented exact wrapper exit code. Production termination behavior is
// unchanged; the test-only observer uses waitid(P_PIDFD, WNOWAIT).
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

internal static class Wp05ForgedAbortOracle
{
    internal readonly record struct DecisionState(
        ReleaseDecisionKind Kind,
        ReleaseDisposition DurableRelease,
        DeliveryResult Delivery,
        bool DurableCommitted);

    internal readonly record struct JournalState(
        bool Ok,
        PreparationStage Stage,
        bool HasRelease,
        bool HasStop,
        string Error);

    // Retained only for the preserved independent hostile probe's 5-argument
    // call. A bare numeric exit status is never sufficient evidence.
    internal readonly record struct AbortState(
        bool Stopped,
        bool WrapperReaped,
        string Error,
        bool NoTargetInitializationMarkerAbsent);

    /// <summary>Compatibility overload for the preserved hostile probe. It
    /// always rejects: a numeric wrapper exit code cannot distinguish normal
    /// exit from SIGKILL or prove authorized-stop provenance.</summary>
    internal static string? Validate(
        DecisionState decision,
        bool capabilityLive,
        JournalState journal,
        AbortState abort,
        int wrapperExitCode)
    {
        _ = decision;
        _ = capabilityLive;
        _ = journal;
        _ = abort;
        return $"wrapper exit {wrapperExitCode} without raw termination classification and " +
               "authorized-stop provenance is insufficient evidence";
    }

    internal static string? Validate(
        DecisionState decision,
        bool capabilityLive,
        JournalState journal,
        AuthorizedStopEvidence stop,
        ObservedTermination termination)
    {
        if (decision.Kind != ReleaseDecisionKind.Refused)
            return $"forged request was not refused: {decision.Kind}";
        if (decision.DurableRelease != ReleaseDisposition.NotCommitted)
            return $"forged request produced durable release state: {decision.DurableRelease}";
        if (decision.Delivery != DeliveryResult.NotAttempted)
            return $"forged request touched the release transport: {decision.Delivery}";
        if (decision.DurableCommitted)
            return "forged request reported a durable committed release";
        if (!capabilityLive)
            return "forged request consumed the release capability";
        if (!journal.Ok)
            return $"journal read failed after the forged abort: {journal.Error}";
        if (journal.Stage != PreparationStage.Prepared)
            return $"journal stage changed after the forged abort: {journal.Stage}";
        if (journal.HasRelease)
            return "a release fact exists after the forged abort";
        if (journal.HasStop)
            return "a stop fact was introduced by the forged abort";

        if (!stop.Identity.IsUsable)
            return "authorized stop identity is missing or unusable";
        if (!stop.Stopped)
            return $"authorized abort did not stop the domain: {stop.Error}";
        if (!stop.WrapperReaped)
            return $"authorized abort did not reap the wrapper: {stop.Error}";
        if (!string.IsNullOrEmpty(stop.Error))
            return $"authorized abort reported an error: {stop.Error}";
        if (!stop.NoTargetInitializationMarkerAbsent)
            return "the fixture constructor marker appeared before the abort completed";

        if (!termination.IsUsable)
            return $"structured termination evidence is unavailable or inconsistent: {termination.Error}";
        if (termination.Identity != stop.Identity)
            return "termination identity does not match the authorized execution/stop identity";

        return termination.Kind switch
        {
            ObservedTerminationKind.Exited => ValidateNormalExit(termination),
            ObservedTerminationKind.Killed => ValidateKilled(termination),
            ObservedTerminationKind.Dumped =>
                "dumped termination is not an accepted abort outcome",
            _ => "termination outcome is unknown; it cannot support an authorized-stop conclusion",
        };
    }

    private static string? ValidateNormalExit(ObservedTermination termination)
    {
        if (termination.RawCode != 1)
            return $"normal exit observation has unexpected raw code {termination.RawCode}";
        if (termination.Signal is not null)
            return "normal exit observation incorrectly carries a signal";
        if (termination.ExitStatus != 133)
        {
            return $"normal exit status {termination.ExitStatus} is not the documented " +
                   "coordinator self-exit (133); a numeric 137 is not an authorized SIGKILL";
        }

        return null;
    }

    private static string? ValidateKilled(ObservedTermination termination)
    {
        if (termination.RawCode != 2)
            return $"signal termination observation has unexpected raw code {termination.RawCode}";
        if (termination.ExitStatus is not null)
            return "signal termination observation incorrectly carries an exit status";
        if (termination.Signal != 9)
        {
            return $"terminating signal {termination.Signal} is not SIGKILL; only the " +
                   "authorized domain stop's SIGKILL is accepted";
        }

        return null;
    }
}

