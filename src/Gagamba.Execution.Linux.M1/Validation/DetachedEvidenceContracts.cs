namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Non-authoritative content values for supplied detached evidence.
/// These types describe frozen-schema conformance and content-binding consistency
/// only. They establish no reviewer, owner, or administrator authority, no
/// protected acquisition or custody, no catalog approval or eligibility, no
/// invocation authorization, and no protected execution permission, and they have
/// no promotion path into any authority type.</summary>
internal sealed record ReviewRecordContract(
    int SchemaVersion,
    string ActionScriptSha256,
    string ArtifactSetSha256,
    string CaseId,
    string CatalogSha256,
    string Conclusion,
    string FixtureManifestSha256,
    string FixturePolicySha256,
    string HostId,
    string Phase,
    string? QualificationId,
    string? QualificationRecordSha256,
    string Topology);

internal sealed record OwnerReceiptRecordContract(
    int SchemaVersion,
    string ArtifactSetSha256,
    string CaseId,
    string CatalogReviewSha256,
    string CatalogSha256,
    string Decision,
    string HostId,
    string Phase,
    string? QualificationId,
    string? QualificationRecordSha256,
    string Topology);

internal sealed record AdminRecordContentContract(
    int SchemaVersion,
    string ArtifactSetSha256,
    string CaseId,
    string CatalogReviewSha256,
    string CatalogSha256,
    string HostId,
    string OwnerCatalogAcceptanceSha256,
    string Phase,
    string? QualificationId,
    string? QualificationRecordSha256,
    string Topology);

internal sealed record DetachedEvidenceBindingsContract(
    CatalogContract Catalog,
    ReviewRecordContract Review,
    OwnerReceiptRecordContract Receipt,
    AdminRecordContentContract AdminContent,
    string Hk,
    string Hr,
    string Hw,
    string Hp);
