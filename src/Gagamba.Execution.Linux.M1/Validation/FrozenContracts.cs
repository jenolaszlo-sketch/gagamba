namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Non-authoritative structural contract values for the accepted frozen
/// artifacts. None of these types establishes review, owner acceptance,
/// administrative approval, eligibility, invocation, or execution authority,
/// and none has a promotion path into an authority type.</summary>
internal sealed record FixtureManifestObjectContract(
    string Role, string Path, long Length, string Sha256, string Mode, string Destination);

internal sealed record FixtureManifestContract(
    int SchemaVersion, string CaseId, string Topology, IReadOnlyList<FixtureManifestObjectContract> Objects);

internal sealed record ActionContract(int Index, string Op, long TimeoutNs);

internal sealed record ActionScriptContract(
    int SchemaVersion,
    string CaseId,
    string Phase,
    string Topology,
    string FixtureManifestSha256,
    string FixturePolicySha256,
    IReadOnlyList<ActionContract> Actions,
    IReadOnlyList<string> ActionHashes)
{
    internal bool HasRelease => Actions.Any(action => action.Op == "release");
}

internal sealed record FixturePolicyContract(
    int SchemaVersion,
    string PolicyKind,
    string CaseId,
    string Phase,
    string Topology,
    string HostId,
    string BootId,
    string FixtureManifestSha256,
    bool AllowRelease,
    long MaximumSessionNs,
    long Uid,
    long Gid);

internal sealed record CatalogContract(
    int SchemaVersion,
    string CaseId,
    string Phase,
    string Topology,
    string ArtifactSetSha256,
    string SourceCommit,
    string SourceTree,
    string ProfileId,
    string ProfileVersion,
    string Rid,
    string Scope,
    string HostId,
    string PlatformInventorySha256,
    string? QualificationId,
    string? QualificationRecordSha256,
    string ControllerClosureSha256,
    string DriverClosureSha256,
    string FixtureManifestSha256,
    string FixturePolicySha256,
    string ActionScriptSha256,
    long MaximumSessionNs,
    bool AllowRelease);

internal sealed record FrozenContractChain(
    FixtureManifestContract Manifest,
    FixturePolicyContract Policy,
    ActionScriptContract Script,
    CatalogContract Catalog,
    string Hm,
    string Hf,
    string Hs,
    string Hk);
