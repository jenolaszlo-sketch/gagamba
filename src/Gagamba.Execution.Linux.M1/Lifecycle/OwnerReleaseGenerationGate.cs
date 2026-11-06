using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

// P3 (GAGAMBA-RC2B-06-B6, owner decision 3.2) owner-side pre-RELEASE
// active-generation verification. Immediately before entering the release
// arbiter's final runtime gate, a qualified operation re-enters the single
// shared Q10 decision and requires its retained selection to still be the
// active qualified installation: full identity plus qualification id and raw
// record hash, no null skips. The observation is never adopted, cached,
// supplied by the controller or reconstructed as wire/disk echo, and it never
// substitutes for the final gate's own rechecks. The controller-side Q10
// check stays additional and unchanged.
// Legacy operations have no installed-generation model and verify nothing
// here (controller Q10 parity); every other context refuses fail-closed.
internal static class OwnerReleaseGenerationGate
{
    internal static OwnerGenerationGateRefusal? Verify(OwnerAuthorityContext authority)
    {
        if (authority is OwnerAuthorityContext.Legacy)
        {
            return null;
        }

        if (authority is OwnerAuthorityContext.Qualified qualified)
        {
            try
            {
                var (verified, reason, error) =
                    ReleaseGenerationGate.RequireBoundGenerationActive(
                        qualified.RequireLoadedInstallation());
                return verified ? null : new OwnerGenerationGateRefusal(reason, error);
            }
            catch (Exception exception)
            {
                return new OwnerGenerationGateRefusal(
                    ReleaseGenerationGate.MapFailure(exception), exception.Message);
            }
        }

        return new OwnerGenerationGateRefusal(
            RestrictedRefusalReason.InstallationUnavailable,
            "owner pre-RELEASE generation verification requires a retained qualified or legacy owner authority context");
    }

#if !GAGAMBA_PRODUCTION
    // Debug-only decision entry for deterministic tests: it binds the exact
    // same shared RequireStillQualified decision the production verification
    // uses to a synthetic fresh observation, without a protected host. The
    // positive fixed-root observation and the race rows are separately
    // qualified native-pending. Excluded from Release together with
    // Qualified.ForTest so the production verifier keeps forbidding test hooks.
    internal static OwnerGenerationGateRefusal? VerifyObserved(
        InstalledResult observed, OwnerAuthorityContext.Qualified retained)
    {
        try
        {
            retained.RequireEcho(observed);
            return null;
        }
        catch (Exception exception)
        {
            return new OwnerGenerationGateRefusal(
                ReleaseGenerationGate.MapFailure(exception), exception.Message);
        }
    }
#endif
}
