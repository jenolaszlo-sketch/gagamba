namespace Gagamba.Execution.Linux.M1.Installed;

/// <summary>
/// E-1 (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-IMPL-01). A distinct, immutable,
/// internal prepublication integrity projection for the fixed-root active
/// selection. It represents verified integrity only: it carries no
/// qualification association, grants no execution, manifest, staging, HELD or
/// RELEASE authority, and cannot satisfy any owner authority context. It is
/// unreachable from every production entry path in this package.
///
/// Non-forgeability: the constructor is private. The only construction route in
/// RELEASE is <see cref="Verify"/>, which re-enters the accepted fixed-root
/// verification and host checks. No caller-supplied digest, identity, manifest,
/// result object, Boolean or claimed verification outcome can manufacture a
/// protected projection. A Debug-only <c>ForTest</c> seam is compile-time
/// excluded from RELEASE and is tagged
/// <see cref="PrepublicationIntegrityProvenance.DeterministicTestOnly"/> so it
/// cannot be confused with protected provenance.
/// </summary>
internal sealed class PrepublicationIntegrityProjection
{
    private PrepublicationIntegrityProjection(string artifactSetSha256, string sourceCommit, string sourceTree,
        string runtimeInventorySha256, string ownerClosureSha256, string recoveryClosureSha256,
        PrepublicationIntegrityProvenance provenance)
    {
        ArtifactSetSha256 = artifactSetSha256;
        SourceCommit = sourceCommit;
        SourceTree = sourceTree;
        RuntimeInventorySha256 = runtimeInventorySha256;
        OwnerClosureSha256 = ownerClosureSha256;
        RecoveryClosureSha256 = recoveryClosureSha256;
        Provenance = provenance;
    }

    /// <summary>The retained active artifact-set digest (same value as the
    /// selected generation directory name).</summary>
    internal string ArtifactSetSha256 { get; }

    internal string SourceCommit { get; }

    internal string SourceTree { get; }

    internal string RuntimeInventorySha256 { get; }

    internal string OwnerClosureSha256 { get; }

    internal string RecoveryClosureSha256 { get; }

    internal PrepublicationIntegrityProvenance Provenance { get; }

    // The prepublication association is always null by contract. The projection
    // never carries an installed qualification ID or raw record hash, and it is
    // never an InstalledResult or a LoadedInstallation.
    internal string? QualificationId => null;

    internal string? QualificationRecordSha256 => null;

    /// <summary>
    /// Single production construction route. It performs the accepted fixed-root
    /// active/payload/platform verification and refuses (throws
    /// <see cref="InvalidDataException"/>) on any missing, inconsistent or
    /// untrusted evidence. E-1 does not call it from any production entry path and
    /// wires no consumer.
    /// </summary>
    internal static PrepublicationIntegrityProjection Verify() =>
        FromEvidence(InstalledFileSystem.VerifyActiveIntegrity(),
            PrepublicationIntegrityProvenance.ProtectedActiveSelection);

    private static PrepublicationIntegrityProjection FromEvidence(InstalledIntegrityEvidence evidence,
        PrepublicationIntegrityProvenance provenance)
    {
        // Defense in depth: even an internals-visible evidence value must be well
        // formed before it can shape a projection. This is not the primary
        // non-forgeability boundary (that is the private constructor plus the
        // absence of any caller-fed RELEASE factory).
        if (evidence is null) throw new InvalidDataException("prepublication evidence absent");
        var manifest = evidence.Manifest;
        if (manifest is null) throw new InvalidDataException("prepublication manifest absent");
        InstallJson.Require(InstallJson.IsHex(evidence.ArtifactSetSha256, 64) &&
            evidence.ArtifactSetSha256 == manifest.Digest,
            "prepublication artifact digest mismatch");
        InstallJson.Require(InstallJson.IsHex(evidence.RuntimeInventorySha256, 64) &&
            InstallJson.IsHex(evidence.OwnerClosureSha256, 64) &&
            InstallJson.IsHex(evidence.RecoveryClosureSha256, 64),
            "prepublication closure digest");
        // F-01 semantic integrity (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-IMPL-01):
        // bind every derived field to a deterministic function of the authoritative
        // manifest rather than trusting well-formed values. In RELEASE the manifest
        // is produced only by the fixed-root verifier supplied through Verify(), so
        // these checks make the projection the accepted role-closure and platform
        // projection of the verified active selection. RoleHash throws when the
        // manifest lacks a required role artifact, and InstallJson.Hex throws when
        // the platform runtime-inventory metadata is absent or malformed, so both
        // fail closed without any fallback.
        InstallJson.Require(evidence.OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1) &&
            evidence.RecoveryClosureSha256 == InstallationIdentity.RoleHash(manifest, 0),
            "prepublication role closure mismatch");
        InstallJson.Require(evidence.RuntimeInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"),
            "prepublication runtime inventory mismatch");
        InstallJson.Require(provenance is PrepublicationIntegrityProvenance.ProtectedActiveSelection
            or PrepublicationIntegrityProvenance.DeterministicTestOnly, "prepublication provenance");
        return new PrepublicationIntegrityProjection(evidence.ArtifactSetSha256, manifest.SourceCommit,
            manifest.SourceTree, evidence.RuntimeInventorySha256, evidence.OwnerClosureSha256,
            evidence.RecoveryClosureSha256, provenance);
    }

#if !GAGAMBA_PRODUCTION
    /// <summary>
    /// Debug/test-only construction seam. It is excluded from RELEASE and is
    /// permanently tagged <see cref="PrepublicationIntegrityProvenance.DeterministicTestOnly"/>
    /// so synthetic evidence can never be mistaken for a protected projection. It
    /// fabricates no authority and is not reachable from production execution.
    /// </summary>
    internal static PrepublicationIntegrityProjection ForTest(InstalledIntegrityEvidence evidence) =>
        FromEvidence(evidence, PrepublicationIntegrityProvenance.DeterministicTestOnly);
#endif
}

/// <summary>Provenance of a <see cref="PrepublicationIntegrityProjection"/>. It
/// keeps protected evidence distinguishable from deterministic test data.</summary>
internal enum PrepublicationIntegrityProvenance
{
    ProtectedActiveSelection,
    DeterministicTestOnly,
}

/// <summary>
/// Integrity evidence gathered by the isolated prepublication verifier. It is a
/// pure value, not authority: only
/// <see cref="PrepublicationIntegrityProjection"/> can turn it into a projection,
/// and in RELEASE only through the verified construction route. Callers that hold
/// an instance cannot manufacture a projection from it without re-verification.
/// </summary>
internal sealed record InstalledIntegrityEvidence(string ArtifactSetSha256, InstalledManifest Manifest,
    string OwnerClosureSha256, string RecoveryClosureSha256, string RuntimeInventorySha256);
