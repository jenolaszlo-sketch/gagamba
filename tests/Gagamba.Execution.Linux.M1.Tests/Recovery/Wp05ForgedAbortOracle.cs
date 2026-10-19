// WP05 qualification-harness oracle for the forged-identity abort path.
// This is test-only logic. It validates the accepted abort contract, not an
// undocumented exact wrapper exit code. The production abort contract is
// HeldAbortOutcome(Stopped, WrapperReaped, Error, marker); a force-stop may
// kill the wrapper before the coordinator's own fail(33) exit.
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

    internal readonly record struct AbortState(
        bool Stopped,
        bool WrapperReaped,
        string Error,
        bool NoTargetInitializationMarkerAbsent);

    internal static string? Validate(
        DecisionState decision,
        bool capabilityLive,
        JournalState journal,
        AbortState abort,
        int wrapperExitCode)
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
        if (!abort.Stopped)
            return $"abort did not stop the domain: {abort.Error}";
        if (!abort.WrapperReaped)
            return $"abort did not reap the wrapper: {abort.Error}";
        if (!string.IsNullOrEmpty(abort.Error))
            return $"abort reported an error: {abort.Error}";
        if (!abort.NoTargetInitializationMarkerAbsent)
            return "the fixture constructor marker appeared before the abort completed";

        // AbortAsync closes control and then force-stops the domain before
        // reaping. Two outcomes are contractually permitted for this test:
        //   133 = the coordinator observed control EOF and executed fail(33);
        //   137 = this test's own AbortAsync cgroup stop SIGKILLed the wrapper
        //         before the coordinator could exit.
        // 139 is a SIGSEGV-derived value (128 + 11); under the accepted corrected
        // seccomp policy it is not an expected abort outcome and must fail.
        return wrapperExitCode switch
        {
            133 => null,
            137 => null,
            _ => $"wrapper exit {wrapperExitCode} is not an accepted abort outcome (133 or 137)",
        };
    }
}
