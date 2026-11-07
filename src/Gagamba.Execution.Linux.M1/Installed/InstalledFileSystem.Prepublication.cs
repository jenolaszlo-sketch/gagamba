namespace Gagamba.Execution.Linux.M1.Installed;

// E-1 (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-IMPL-01). Isolated prepublication
// integrity verification. This is a deliberate, self-contained mirror of
// VerifyActive's fixed-root active-selection and exact payload read so that E-1
// cannot alter existing qualified verification. It reuses the same private
// descriptor-relative read primitives, performs the accepted host/platform
// checks that VerifyActive's fresh branch skips, and returns integrity evidence
// only. It grants no qualification, admission or execution authority and is not
// called from any production entry path.
internal static partial class InstalledFileSystem
{
    /// <summary>
    /// Verifies the fixed-root active selection and full payload closure without
    /// requiring a qualification record, then requires the accepted host/platform
    /// baseline. Returns integrity evidence bound to the verified manifest. Throws
    /// <see cref="InvalidDataException"/> when any evidence is missing,
    /// inconsistent or untrusted. Native protected success is pending; on an
    /// unprovisioned host this refuses.
    /// </summary>
    internal static InstalledIntegrityEvidence VerifyActiveIntegrity()
    {
        try
        {
            EnsureLinuxX64();
            using var root = OpenAbsoluteDirectory(InstallRoot, trustedAncestors: true, missingUnavailable: true);
            RequireDirectory(root.Snapshot, true, 0, true);
            using var installations = OpenDirectory(root.Value, "installations", trusted: true, uid: 0, requireExact0755: true);
            using var qualifications = OpenDirectory(root.Value, "qualifications", trusted: true, uid: 0, requireExact0755: true);

            var rootBefore = root.Snapshot;
            var installationsBefore = installations.Snapshot;
            var qualificationsBefore = qualifications.Snapshot;
            byte[] activeBytes = ReadTrustedFile(root.Value, "active", 0, 0, 0x124, 128, true, out var activeBefore);
            string digest = InstalledMetadata.Active(activeBytes);

            using var generation = OpenDirectory(installations.Value, digest, trusted: true, uid: 0, requireExact0755: true);
            var generationBefore = generation.Snapshot;
            byte[] manifestBytes = ReadTrustedFile(generation.Value, "manifest.json", 0, 0, 0x124, MaxMetadataBytes, true, out var manifestBefore);
            var manifest = InstalledManifest.Parse(manifestBytes);
            SameDigest(manifest.Digest, digest, "active generation and manifest digest disagree");
            byte[] receiptBytes = ReadTrustedFile(generation.Value, "install-receipt.json", 0, 0, 0x124, MaxMetadataBytes, true, out var receiptBefore);
            _ = InstalledMetadata.Receipt(receiptBytes, manifest);

            var scan = CreateScan(manifest, trusted: true);
            scan.Files.Add("manifest.json", MetadataArtifact("metadata-manifest", "manifest.json", manifestBytes));
            scan.Files.Add("install-receipt.json", MetadataArtifact("metadata-receipt", "install-receipt.json", receiptBytes));
            Walk(generation.Value, "", 0, scan, includeRootMetadata: true);
            EnsureExact(scan, includeReceipt: true);
            InstalledPayload.CheckDependencies(manifest, scan.Configs);

            // Complete accepted host/platform verification. VerifyActive's fresh
            // (null-qualification) branch returns InstalledUnqualified before this
            // point; the prepublication projection requires the full host evidence.
            InstalledPayload.CheckHost(manifest);

            // No protected store may change across the whole verification.
            RecheckSelectors(root.Value, installations.Value, qualifications.Value,
                digest, rootBefore, installationsBefore, qualificationsBefore, activeBefore,
                generationBefore, manifestBefore, receiptBefore, null, default);

            return new InstalledIntegrityEvidence(digest, manifest,
                InstallationIdentity.RoleHash(manifest, 1), InstallationIdentity.RoleHash(manifest, 0),
                InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"));
        }
        catch (NotSupportedException ex) { throw new InvalidDataException("prepublication integrity is unavailable: " + ex.Message, ex); }
        catch (DllNotFoundException ex) { throw new InvalidDataException("prepublication integrity is unavailable: " + ex.Message, ex); }
        catch (EntryPointNotFoundException ex) { throw new InvalidDataException("prepublication integrity is unavailable: " + ex.Message, ex); }
        catch (BadImageFormatException ex) { throw new InvalidDataException("prepublication integrity is unavailable: " + ex.Message, ex); }
    }
}
