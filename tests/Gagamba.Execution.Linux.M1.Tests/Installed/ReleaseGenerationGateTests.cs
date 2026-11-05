using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installed;

// Q10 (GAGAMBA-RC2B-06-B6) deterministic decision tests: the pre-RELEASE
// active-generation gate must refuse on every fresh-observation failure and
// must never let an earlier admission-time verification substitute for the
// fresh reread. The trusted fixed-root reader itself is exercised only by the
// separately qualified native suites; these tests bind the production
// decision logic (ReleaseGenerationGate, shared with
// LoadedInstallation.Revalidate) to synthetic InstalledResult observations.
public sealed class ReleaseGenerationGateTests
{
    private const string RecordHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static InstalledManifest Manifest() => InstalledManifest.Parse(InstalledMetadataTests.Vector());

    private static InstalledManifest ChangedManifest()
    {
        var root = JsonNode.Parse(InstalledMetadataTests.Vector())!;
        root["source"]!["commit"] = new string('c', 40);
        return InstalledManifest.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
    }

    private static InstalledResult Bound() => InstalledResult.Qualified(Manifest(), "Q1", RecordHash);

    [Fact]
    public void UnchangedActiveGenerationIsVerified()
    {
        // Same manifest, qualification id and record hash: the fresh
        // observation confirms the bound generation is still active.
        var error = Record.Exception(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Qualified(Manifest(), "Q1", RecordHash), Bound()));
        Assert.Null(error);
    }

    [Fact]
    public void ChangedActiveGenerationRefusesAsUnqualified()
    {
        // A deployment switched the active generation to a different artifact
        // set: refuse; never adopt the newer selection.
        var exception = Record.Exception(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash), Bound()));
        Assert.IsType<InvalidDataException>(exception);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation,
            ReleaseGenerationGate.MapFailure(exception!));
    }

    [Fact]
    public void MissingActiveSelectorRefuses()
    {
        var exception = Record.Exception(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Unavailable("active selector missing"), Bound()));
        Assert.IsType<InvalidDataException>(exception);
    }

    [Fact]
    public void UnqualifiedFreshSelectionRefuses()
    {
        // An installed but never-accepted generation is not an active
        // qualified selection and cannot carry a release.
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Fresh(Manifest()), Bound()));
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Unqualified("record refused"), Bound()));
    }

    [Fact]
    public void BoundGenerationRecordChangeRefuses()
    {
        // Same artifact set but a replaced qualification record: the bound
        // approval no longer matches the active selection.
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Qualified(Manifest(), "Q1", new string('9', 64)), Bound()));
    }

    [Fact]
    public void QualificationIdChangeRefuses()
    {
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Qualified(Manifest(), "Q2", RecordHash), Bound()));
    }

    [Fact]
    public void EarlierAdmittedSelectionDoesNotSatisfyFreshReread()
    {
        // The bound selection passed admission (it is Qualified by
        // construction); Q10 still requires the FRESH observation to confirm
        // it. Both a vanished active selection and a deployment change refuse
        // despite the earlier successful admission check.
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Unavailable("active selector missing"), Bound()));
        Assert.Throws<InvalidDataException>(() =>
            ReleaseGenerationGate.RequireStillQualified(
                InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash), Bound()));
    }

    [Fact]
    public void UnavailableVerificationObservationMapsToInstallationUnavailable()
    {
        // The trusted reader could not complete (filesystem or access
        // failure): refuse as unavailable, never as a confirmed mismatch.
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable,
            ReleaseGenerationGate.MapFailure(new IOException("fixed root unreadable")));
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable,
            ReleaseGenerationGate.MapFailure(new UnauthorizedAccessException("denied")));
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable,
            ReleaseGenerationGate.MapFailure(new InvalidOperationException("unexpected")));
    }

    [Fact]
    public void MissingRetainedSelectionRefusesFailClosed()
    {
        var (verified, reason, error) = ReleaseGenerationGate.RequireBoundGenerationActive(null);
        Assert.False(verified);
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable, reason);
        Assert.StartsWith("q10:", error);
    }
}
