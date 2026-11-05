using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>
/// P2 owner-side boundary for new qualified manifest authority. A qualified
/// direct Start path may not reach the Prepared transition, capability mint or
/// HELD publication without a fresh protected final-admission grant bound to
/// the owner's first retained S2 selection.
///
/// This is a refusal gate only. It reuses the existing protected
/// <see cref="LoadedInstallation.VerifyFinalAdmission"/> and
/// <see cref="AdmissionAuthority.IsFinalAdmissionEligible"/> decision and does
/// not introduce a second trust root. It does not close the complete pre-effect
/// sink inventory (P4).
/// </summary>
internal static class OwnerManifestAdmissionGate
{
    /// <summary>Production entry. It re-enters the existing protected verifier
    /// through the retained <see cref="LoadedInstallation"/> and makes the single
    /// shared admission decision. No caller value, wire echo, profile flag or
    /// Boolean substitutes for the protected bytes.</summary>
    internal static bool TryRequire(OwnerAuthorityContext.Qualified qualified, out string error) =>
        TryRequireCore(qualified, static selection =>
            selection.RequireLoadedInstallation().VerifyFinalAdmission(), out error);

    /// <summary>The single production decision. A grant is private evidence
    /// created only by <see cref="FinalAdmissionVerifier"/>; a binding beside it
    /// is not authority. The retained selection is never replaced.</summary>
    internal static void RequireEligible(InstalledResult retained, FinalAdmissionVerifier.Grant? grant)
    {
        ArgumentNullException.ThrowIfNull(retained);
        if (grant is null)
        {
            throw new InvalidDataException("protected final admission is unavailable");
        }

        if (!AdmissionAuthority.IsFinalAdmissionEligible(retained, grant))
        {
            throw new InvalidDataException(
                "protected final admission does not bind the retained S2 selection");
        }

        // The grant's own fresh S2 observation must still be the first retained
        // tuple (identity, qualification id and raw record hash). A newer
        // generation is refused, never adopted.
        ReleaseGenerationGate.RequireStillQualified(grant.Selection, retained);
    }

    private static bool TryRequireCore(OwnerAuthorityContext.Qualified qualified,
        Func<OwnerAuthorityContext.Qualified, FinalAdmissionVerifier.Grant> verify, out string error)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(qualified);
            ArgumentNullException.ThrowIfNull(verify);
            var grant = verify(qualified);
            RequireEligible(qualified.Selection, grant);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = $"{ReleaseGenerationGate.MapFailure(exception)}: {exception.Message}";
            return false;
        }
    }
}
