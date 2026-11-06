using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Lifecycle;

// P3 (GAGAMBA-RC2B-06-B6-ALT-B) deterministic owner pre-RELEASE
// generation-gate decision tests. VerifyObserved binds the same shared
// RequireStillQualified decision the production verification uses to
// synthetic observations; the trusted fixed-root positive stays
// native-pending. The production Verify entry is examined directly,
// including its fail-closed catch, the legacy null and the unreachable
// validation-case refusal that has no factory today.
public sealed class OwnerReleaseGenerationGateTests
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

    private static OwnerAuthorityContext.Qualified Retained() =>
        OwnerAuthorityContext.Qualified.ForTest(Bound());

    [Fact]
    public void UnchangedObservationVerifies()
    {
        // Same full identity, qualification id and record hash: the fresh
        // observation confirms the retained generation is still active.
        Assert.Null(OwnerReleaseGenerationGate.VerifyObserved(Bound(), Retained()));
    }

    [Fact]
    public void ReplacedGenerationRefusesAsUnqualified()
    {
        // A deployment switched the active generation: refuse as a completed
        // observation that no longer matches; never adopt the newer selection.
        var refusal = OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash), Retained());
        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, refusal!.Reason);
        Assert.Contains("original S2 selection no longer qualified", refusal.Error);
    }

    [Fact]
    public void MissingActiveSelectorRefuses()
    {
        var refusal = OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Unavailable("active selector missing"), Retained());
        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, refusal!.Reason);
    }

    [Fact]
    public void FreshAndUnqualifiedSelectionsRefuse()
    {
        // Installed but never accepted, and an explicitly refused record, are
        // not an active qualified selection and cannot carry a release.
        Assert.NotNull(OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Fresh(Manifest()), Retained()));
        Assert.NotNull(OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Unqualified("record refused"), Retained()));
    }

    [Fact]
    public void QualificationRecordHashChangeRefuses()
    {
        var refusal = OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Qualified(Manifest(), "Q1", new string('9', 64)), Retained());
        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, refusal!.Reason);
    }

    [Fact]
    public void QualificationIdChangeRefuses()
    {
        var refusal = OwnerReleaseGenerationGate.VerifyObserved(
            InstalledResult.Qualified(Manifest(), "Q2", RecordHash), Retained());
        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, refusal!.Reason);
    }

    [Fact]
    public void RefusalNeverAdoptsTheObservedSelection()
    {
        var retained = Retained();
        var observed = InstalledResult.Qualified(Manifest(), "Q2", RecordHash);

        Assert.NotNull(OwnerReleaseGenerationGate.VerifyObserved(observed, retained));

        // The retained tuple is unchanged: the observation is local to this
        // verification and is never cached in place of the retained selection.
        Assert.Equal("Q1", retained.QualificationId);
        Assert.Equal(RecordHash, retained.QualificationRecordSha256);
        Assert.Equal(Bound().Identity, retained.Selection.Identity);
    }

    [Fact]
    public void ProductionEntryRefusesWithoutAVerifiedInstallation()
    {
        // A retained context shaped for tests has no actual verified
        // LoadedInstallation, so the production entry refuses fail-closed
        // instead of inventing a positive observation.
        var refusal = OwnerReleaseGenerationGate.Verify(
            OwnerAuthorityContext.Qualified.ForTest(Bound()));

        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, refusal!.Reason);
        Assert.Contains("no verified loaded installation", refusal.Error);
    }

    [Fact]
    public void LegacyContextHasNoInstalledGenerationModel()
    {
        // Controller Q10 parity: the legacy experimental path carries no
        // installed selection and verifies nothing here.
        Assert.Null(OwnerReleaseGenerationGate.Verify(OwnerAuthorityContext.LegacyContext));
    }

    [Fact]
    public void ValidationContextRemainsUnreachableAndRefusesFailClosed()
    {
        var nested = typeof(OwnerAuthorityContext).GetNestedType(
            "Validation", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(nested);
        var constructors = nested!.GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));
        Assert.True(!nested
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Any(method => method.ReturnType == typeof(OwnerAuthorityContext)));

        var instance = (OwnerAuthorityContext)constructors.Single().Invoke([])!;
        var refusal = OwnerReleaseGenerationGate.Verify(instance);

        Assert.NotNull(refusal);
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable, refusal!.Reason);
        Assert.Contains("qualified or legacy", refusal.Error);
    }

    [Fact]
    public void GateExposesNoBooleanOrCallerCallbackSurface()
    {
        var gate = typeof(OwnerReleaseGenerationGate);
        var methods = gate.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
        Assert.DoesNotContain(methods, method =>
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(bool)));
        Assert.DoesNotContain(gate.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            property => property.PropertyType == typeof(bool));
        Assert.DoesNotContain(methods, method =>
            method.GetParameters().Any(parameter =>
                typeof(Delegate).IsAssignableFrom(parameter.ParameterType)));

        var verify = methods.Single(method => method.Name == "Verify");
        Assert.Equal(typeof(OwnerAuthorityContext), verify.GetParameters().Single().ParameterType);
    }
}
