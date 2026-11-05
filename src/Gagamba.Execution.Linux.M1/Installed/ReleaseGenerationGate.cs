using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Installed;

// Q10 (GAGAMBA-RC2B-06-B6, mandatory non-waiver): immediately before the
// controller sends a new ReleaseRequest it re-enters S2's fixed-root verifier
// and requires the operation's bound generation to still be the active
// qualified selection. A missing, malformed, unavailable, stale, replaced or
// mismatched observation refuses the release without a new request. The bound
// generation is never replaced and a newer selection is never adopted. The
// interval between the fresh observation and the owner's release effect is the
// accepted documented residual (a root actor can still change the installation
// after the read; the owner echo and the immutable-generation model remain the
// detection layers and the stop path keeps its original authority).
internal static class ReleaseGenerationGate
{
    // The single decision shared by admission-time Revalidate and the
    // pre-RELEASE gate: the fresh observation must be a Qualified selection
    // with the exact bound identity, qualification id and record hash.
    internal static void RequireStillQualified(InstalledResult next, InstalledResult bound)
    {
        InstallJson.Require(next.State == InstalledState.Qualified && next.Identity is not null
            && next.Identity == bound.Identity && next.QualificationId == bound.QualificationId
            && next.QualificationRecordSha256 == bound.QualificationRecordSha256,
            "original S2 selection no longer qualified");
    }

    // Refusal typing for a failed pre-RELEASE observation: an observation that
    // completed but no longer matches the bound generation is
    // UnqualifiedInstallation; an observation that could not complete
    // (missing selector, malformed trusted state, filesystem or crypto
    // failure) is InstallationUnavailable. Both refuse; neither adopts state.
    internal static RestrictedRefusalReason MapFailure(Exception exception) =>
        exception is InvalidDataException
            ? RestrictedRefusalReason.UnqualifiedInstallation
            : RestrictedRefusalReason.InstallationUnavailable;

    // Controller pre-RELEASE boundary. `bound` is the retained admission-time
    // selection of a qualified operation (the qualified recovery connection
    // retains it immutably). Null is refused fail-closed: a caller that
    // reaches this gate without a retained trusted installation has no
    // generation to re-read and must not release.
    internal static (bool Verified, RestrictedRefusalReason Reason, string Error) RequireBoundGenerationActive(
        LoadedInstallation? bound)
    {
        if (bound is null)
        {
            return (false, RestrictedRefusalReason.InstallationUnavailable,
                "q10: no retained trusted installation for the bound generation");
        }

        try
        {
            bound.Revalidate();
            return (true, RestrictedRefusalReason.None, string.Empty);
        }
        catch (Exception exception)
        {
            return (false, MapFailure(exception),
                $"q10 active-generation reread refused: {exception.Message}");
        }
    }
}
