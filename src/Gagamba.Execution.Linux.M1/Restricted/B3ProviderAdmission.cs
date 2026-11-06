using Gagamba.Execution.Linux.M1.Installed;

namespace Gagamba.Execution.Linux.M1.Restricted;

// Normal qualified construction only. This narrows the constructor boundary;
// it grants no manifest, RELEASE or validation authority. Slice E must supply
// its distinct validation context atomically; no validation exception exists.
internal static class B3ProviderAdmission
{
    internal static void Require(B3FixtureProfile profile, LoadedInstallation? retained)
    {
        if (!profile.QualifiedInstallation)
        {
            if (retained is not null)
                throw new InvalidDataException("legacy B3 construction cannot carry installed provider authority");
            return;
        }

        if (retained is null)
            throw new InvalidDataException("qualified B3 construction requires retained provider authority");
        var original = retained.Selection;
        if (original.State != InstalledState.Qualified || original.Manifest is null || original.Identity is null ||
            original.QualificationId is null || original.QualificationRecordSha256 is null)
            throw new InvalidDataException("qualified B3 construction provider selection refused");

        // Re-enter the actual provider-role verifier, not merely the S2 tuple
        // supplied by a caller. Compare against the factory's first selection;
        // a later generation or qualification cannot replace that selection.
        var observed = LoadedInstallation.Verify("provider", retained.Identity);
        ReleaseGenerationGate.RequireStillQualified(observed.Selection, original);
        RequireEligible(original, retained.VerifyFinalAdmission());
    }

    // Shared exact-tuple decision. Production obtains its grant only through
    // the retained LoadedInstallation's protected fixed-root verifier above.
    internal static void RequireEligible(InstalledResult retained, FinalAdmissionVerifier.Grant? grant)
    {
        ArgumentNullException.ThrowIfNull(retained);
        if (grant is null || !AdmissionAuthority.IsFinalAdmissionEligible(retained, grant))
            throw new InvalidDataException("qualified B3 construction final admission refused");
        ReleaseGenerationGate.RequireStillQualified(grant.Selection, retained);
    }
}
