namespace Gagamba.Execution.Linux.M1.Installed;

/// <summary>
/// Final-admission runtime verifier. It reads only the three fixed protected
/// stores, derives the expected final hash from the protected selector selected
/// by the retained S2 artifact digest, and returns a private grant only after
/// exact CJSON-1, binding, host, stability and fresh-S2 checks. It never reads
/// the administrative /etc approval store and never accepts a caller hash.
/// </summary>
internal static class FinalAdmissionVerifier
{
    private const string HostDirectory = "/etc/gagamba";
    private const string HostFile = "host-id";
    private const string SelectorDirectory = "/opt/gagamba/final-admission-selectors";
    private const string RecordDirectory = "/opt/gagamba/final-admissions";

    /// <summary>Private immutable proof. The constructor is private and the only
    /// factory is the full verification routine below, so a caller cannot mint a
    /// grant from a bare binding, Boolean, selector or validation result.</summary>
    internal sealed class Grant
    {
        private Grant(FinalAdmissionBinding binding, InstalledResult selection)
            => (Binding, Selection) = (binding, selection);

        internal FinalAdmissionBinding Binding { get; }
        internal InstalledResult Selection { get; }

        internal static Grant VerifyInstalled(InstalledResult selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            try
            {
                var manifest = selection.Manifest;
                var identity = selection.Identity;
                var qualificationId = selection.QualificationId;
                var qualificationRecordSha256 = selection.QualificationRecordSha256;
                if (selection.State != InstalledState.Qualified || manifest is null || identity is null ||
                    qualificationId is null || qualificationRecordSha256 is null)
                    throw new InvalidDataException("final admission requires a qualified S2 selection");

                var expectation = FinalAdmissionExpectation.FromSelection(selection);

                using var hostDirectory = InstalledFileSystem.OpenProtectedDirectory(HostDirectory);
                using var selectorDirectory = InstalledFileSystem.OpenProtectedDirectory(SelectorDirectory);
                using var recordDirectory = InstalledFileSystem.OpenProtectedDirectory(RecordDirectory);

                var hostBytes = hostDirectory.ReadFile(HostFile, 65, out var hostSnapshot);
                var hostId = FinalAcceptanceCodec.ParseHostId(hostBytes);

                var selectorName = expectation.ArtifactSetSha256 + ".json";
                var selectorBytes = selectorDirectory.ReadFile(selectorName, FinalAcceptanceCodec.MaxBytes,
                    out var selectorSnapshot);
                var selector = FinalAcceptanceCodec.ParseSelector(selectorBytes);
                if (selector.ArtifactSetSha256 != expectation.ArtifactSetSha256 ||
                    selector.QualificationId != expectation.QualificationId ||
                    selector.QualificationRecordSha256 != expectation.QualificationRecordSha256)
                    throw new InvalidDataException("protected final selector does not bind the retained S2 selection");

                var recordName = selector.FinalRecordSha256 + ".json";
                var recordBytes = recordDirectory.ReadFile(recordName, FinalAcceptanceCodec.MaxBytes,
                    out var recordSnapshot);
                var binding = FinalAdmissionEvaluation.Evaluate(selectorBytes, recordBytes, hostId, expectation);

                // Revalidate the original S2 selection after final bytes were read.
                var fresh = InstalledFileSystem.VerifyActive();
                var freshManifest = fresh.Manifest;
                if (fresh.State != InstalledState.Qualified || fresh.Identity != identity ||
                    fresh.QualificationId != qualificationId ||
                    fresh.QualificationRecordSha256 != qualificationRecordSha256 ||
                    freshManifest is null || freshManifest.SourceCommit != manifest.SourceCommit ||
                    freshManifest.SourceTree != manifest.SourceTree)
                    throw new InvalidDataException("S2 selection changed during final admission verification");

                // No protected file may change across the complete verification. The
                // reader already pins and reacquires each inode; these exact-byte and
                // directory-snapshot rechecks close the multi-store window.
                var hostAgain = hostDirectory.ReadFile(HostFile, 65, out var hostSnapshotAgain);
                var selectorAgain = selectorDirectory.ReadFile(selectorName, FinalAcceptanceCodec.MaxBytes,
                    out var selectorSnapshotAgain);
                var recordAgain = recordDirectory.ReadFile(recordName, FinalAcceptanceCodec.MaxBytes,
                    out var recordSnapshotAgain);
                if (!hostBytes.AsSpan().SequenceEqual(hostAgain) || hostSnapshot != hostSnapshotAgain ||
                    !selectorBytes.AsSpan().SequenceEqual(selectorAgain) || selectorSnapshot != selectorSnapshotAgain ||
                    !recordBytes.AsSpan().SequenceEqual(recordAgain) || recordSnapshot != recordSnapshotAgain)
                    throw new InvalidDataException("protected final admission bytes changed during verification");

                using var hostDirectoryAgain = InstalledFileSystem.OpenProtectedDirectory(HostDirectory);
                using var selectorDirectoryAgain = InstalledFileSystem.OpenProtectedDirectory(SelectorDirectory);
                using var recordDirectoryAgain = InstalledFileSystem.OpenProtectedDirectory(RecordDirectory);
                if (hostDirectory.Snapshot != hostDirectoryAgain.Snapshot ||
                    selectorDirectory.Snapshot != selectorDirectoryAgain.Snapshot ||
                    recordDirectory.Snapshot != recordDirectoryAgain.Snapshot)
                    throw new InvalidDataException("protected final admission directory changed during verification");

                return new Grant(binding, fresh);
            }
            catch (NotSupportedException ex)
            {
                throw new InvalidDataException("final admission is unavailable: " + ex.Message, ex);
            }
            catch (DllNotFoundException ex)
            {
                throw new InvalidDataException("final admission is unavailable: " + ex.Message, ex);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new InvalidDataException("final admission is unavailable: " + ex.Message, ex);
            }
            catch (BadImageFormatException ex)
            {
                throw new InvalidDataException("final admission is unavailable: " + ex.Message, ex);
            }
        }

#if !GAGAMBA_PRODUCTION
        // Deterministic tests need protected-verification-shaped evidence on a
        // host without provisioned protected stores. Excluded from Release: the
        // production gate still obtains its only grant from VerifyInstalled.
        internal static Grant ForTest(FinalAdmissionBinding binding, InstalledResult selection)
        {
            ArgumentNullException.ThrowIfNull(binding);
            ArgumentNullException.ThrowIfNull(selection);
            return new Grant(binding, selection);
        }
#endif
    }

    internal static Grant VerifyInstalled(InstalledResult selection) => Grant.VerifyInstalled(selection);
}
