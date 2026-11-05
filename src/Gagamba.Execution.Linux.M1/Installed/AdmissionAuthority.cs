namespace Gagamba.Execution.Linux.M1.Installed;

/// <summary>
/// Future separate production-activation authority. This bounded slice has no
/// factory, no store and no current instance: final acceptance is eligibility,
/// not activation. A later commission must define the only source of an
/// accepted instance.
/// </summary>
internal sealed class ProductionActivationAuthorization
{
    private ProductionActivationAuthorization()
    {
    }
}

/// <summary>
/// Fail-closed separation of S2 Qualified, protected final admission and
/// production activation. A caller Boolean, legacy selector, validation grant
/// or bare final binding cannot satisfy the grant form.
/// </summary>
internal static class AdmissionAuthority
{
    internal static bool IsFinalAdmissionEligible(InstalledResult selection,
        FinalAdmissionVerifier.Grant? grant) =>
        grant is not null && BindingMatches(selection, grant.Binding);

    internal static bool IsProductionAdmissionAuthorized(InstalledResult selection,
        FinalAdmissionVerifier.Grant? grant, ProductionActivationAuthorization? activation) =>
        activation is not null && IsFinalAdmissionEligible(selection, grant);

    /// <summary>Pure predicate used by tests and by the grant form. A binding is
    /// evidence data, never authority by itself.</summary>
    internal static bool IsFinalAdmissionBindingEligible(InstalledResult selection,
        FinalAdmissionBinding? binding) =>
        binding is not null && BindingMatches(selection, binding);

    private static bool BindingMatches(InstalledResult selection, FinalAdmissionBinding binding)
    {
        var manifest = selection.Manifest;
        var record = binding.Record;
        var selector = binding.Selector;
        return selector.Accepted &&
            record.Acceptance == "final-native-accepted" &&
            record.BuildConfiguration == "Production" &&
            selection.State == InstalledState.Qualified && manifest is not null &&
            selection.Identity is not null &&
            selection.QualificationId == record.QualificationId &&
            selection.QualificationRecordSha256 == record.QualificationRecordSha256 &&
            manifest.Digest == record.ArtifactSetSha256 &&
            manifest.SourceCommit == record.SourceCommit &&
            manifest.SourceTree == record.SourceTree &&
            selector.ArtifactSetSha256 == record.ArtifactSetSha256 &&
            selector.QualificationId == record.QualificationId &&
            selector.QualificationRecordSha256 == record.QualificationRecordSha256 &&
            selector.OwnerDecisionSha256 == record.OwnerDecisionSha256 &&
            selector.FinalRecordSha256 == binding.FinalRecordSha256 &&
            record.ProfileId == InstalledManifest.Profile &&
            record.ProfileVersion == InstalledManifest.Version &&
            record.Rid == InstalledManifest.Rid &&
            record.Scope == InstalledManifest.Scope &&
            record.PlatformInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256");
    }
}
