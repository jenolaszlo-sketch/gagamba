using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Lifecycle;

// P2 (GAGAMBA-RC2B-06-B6-ALT-B-P2-IMPL-01): a new qualified manifest
// authorization requires a fresh protected final-admission grant bound to the
// owner's first retained S2 selection. These tests bind the production
// decision and the exact owner wiring without pretending that a protected-root
// positive or Slice E validation authorization exists.
public sealed class OwnerManifestAdmissionGateTests
{
    private const string Host = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string RecordHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string OwnerDecision = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    private static InstalledManifest Manifest() => InstalledManifest.Parse(InstalledMetadataTests.Vector());

    private static InstalledManifest ChangedManifest()
    {
        var root = JsonNode.Parse(InstalledMetadataTests.Vector())!;
        root["source"]!["commit"] = new string('c', 40);
        return InstalledManifest.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
    }

    private static InstalledResult Bound() => InstalledResult.Qualified(Manifest(), "Q1", RecordHash);

    private static JsonObject FinalRecord(InstalledManifest manifest, string qualificationId = "Q1",
        string qualificationRecordSha256 = RecordHash) =>
        new()
        {
            ["acceptance"] = "final-native-accepted",
            ["acceptedAtUtc"] = "2026-11-02T00:00:00Z",
            ["artifactSetSha256"] = manifest.Digest,
            ["buildConfiguration"] = "Production",
            ["candidateEvidenceSha256"] = new string('1', 64),
            ["hostId"] = Host,
            ["independentReviewSha256"] = new string('2', 64),
            ["limitations"] = new JsonArray("portable test only"),
            ["nativeGateResultsSha256"] = new string('3', 64),
            ["ownerDecisionSha256"] = OwnerDecision,
            ["platformInventorySha256"] = InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"),
            ["postpublicationEvidenceSha256"] = new string('4', 64),
            ["prepublicationEvidenceSha256"] = new string('5', 64),
            ["profileId"] = InstalledManifest.Profile,
            ["profileVersion"] = InstalledManifest.Version,
            ["qualificationId"] = qualificationId,
            ["qualificationPlanSha256"] = new string('6', 64),
            ["qualificationRecordSha256"] = qualificationRecordSha256,
            ["reviewCommit"] = new string('7', 40),
            ["rid"] = InstalledManifest.Rid,
            ["schemaVersion"] = 1,
            ["scope"] = InstalledManifest.Scope,
            ["sourceCommit"] = manifest.SourceCommit,
            ["sourceTree"] = manifest.SourceTree,
        };

    private static JsonObject Selector(byte[] recordBytes, InstalledManifest manifest,
        string qualificationId = "Q1", string qualificationRecordSha256 = RecordHash,
        bool accepted = true) =>
        new()
        {
            ["accepted"] = accepted,
            ["artifactSetSha256"] = manifest.Digest,
            ["finalRecordSha256"] = FinalAcceptanceCodec.Hash(recordBytes),
            ["ownerDecisionSha256"] = OwnerDecision,
            ["qualificationId"] = qualificationId,
            ["qualificationRecordSha256"] = qualificationRecordSha256,
            ["schemaVersion"] = 1,
        };

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static FinalAdmissionVerifier.Grant GrantFor(InstalledResult selection)
    {
        var manifest = selection.Manifest ?? throw new InvalidOperationException("selection manifest is absent");
        var recordBytes = Bytes(FinalRecord(manifest, selection.QualificationId!, selection.QualificationRecordSha256!));
        var selectorBytes = Bytes(Selector(recordBytes, manifest, selection.QualificationId!,
            selection.QualificationRecordSha256!));
        var binding = FinalAdmissionEvaluation.Evaluate(selectorBytes, recordBytes, Host,
            FinalAdmissionExpectation.FromSelection(selection));
        return FinalAdmissionVerifier.Grant.ForTest(binding, selection);
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
            BindingFlags.Instance)
            .Single(method => method.Name == name && method.DeclaringType == type);

    private static MethodBase DefaultConstructor(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(constructor => constructor.GetParameters().Length == 0);

    [Fact]
    public void ExactRetainedTupleWithProtectedGrantIsAccepted()
    {
        var selection = Bound();

        var error = Record.Exception(() =>
            OwnerManifestAdmissionGate.RequireEligible(selection, GrantFor(selection)));

        Assert.Null(error);
    }

    [Fact]
    public void RetainedQualifiedContextWithoutProtectedAdmissionRefuses()
    {
        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(Bound(), null));
    }

    [Fact]
    public void ValidProcessAuthenticationAloneCannotGrantManifestAuthority()
    {
        // A retained context shaped from process/installation evidence but
        // without the actual verified LoadedInstallation cannot enter the
        // protected verifier. This models an authenticated Start with no
        // applicable final-admission decision.
        var retained = OwnerAuthorityContext.Qualified.ForTest(Bound());

        var admitted = OwnerManifestAdmissionGate.TryRequire(retained, out var error);

        Assert.False(admitted);
        Assert.StartsWith("UnqualifiedInstallation:", error, StringComparison.Ordinal);
    }

    [Fact]
    public void WrongInstallationIdentityRefuses()
    {
        var grant = GrantFor(Bound());
        var changed = InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash);

        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(changed, grant));
    }

    [Fact]
    public void WrongQualificationIdRefuses()
    {
        var grant = GrantFor(Bound());
        var changed = InstalledResult.Qualified(Manifest(), "Q2", RecordHash);

        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(changed, grant));
    }

    [Fact]
    public void WrongQualificationRecordHashRefuses()
    {
        var grant = GrantFor(Bound());
        var changed = InstalledResult.Qualified(Manifest(), "Q1", new string('9', 64));

        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(changed, grant));
    }

    [Fact]
    public void UnavailableProtectedAuthorityRefuses()
    {
        if (OperatingSystem.IsLinux())
        {
            // A provisioned protected-root positive is a separately authorised
            // native case; this deterministic suite does not invent one.
            Assert.Skip("protected final-admission refusal is covered deterministically on a non-protected host");
        }

        Assert.Throws<InvalidDataException>(() => FinalAdmissionVerifier.VerifyInstalled(Bound()));
        Assert.Throws<InvalidDataException>(() =>
            FinalAcceptanceCodec.ParseSelector(Encoding.UTF8.GetBytes("{}")));
    }

    [Fact]
    public void WithdrawnAcceptanceBeforeManifestGrantRefuses()
    {
        Assert.Throws<InvalidDataException>(() =>
            FinalAcceptanceCodec.ParseSelector(Bytes(Selector(
                Bytes(FinalRecord(Manifest())), Manifest(), accepted: false))));

        var grant = GrantFor(Bound());
        var withdrawn = FinalAdmissionVerifier.Grant.ForTest(
            grant.Binding with { Selector = grant.Binding.Selector with { Accepted = false } },
            grant.Selection);

        Assert.False(AdmissionAuthority.IsFinalAdmissionEligible(Bound(), withdrawn));
        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(Bound(), withdrawn));
    }

    [Fact]
    public void LaterEchoCannotSubstituteRetainedAuthority()
    {
        var retained = OwnerAuthorityContext.Qualified.ForTest(Bound());
        var replacement = InstalledResult.Qualified(Manifest(), "Q2", RecordHash);

        Assert.Throws<InvalidDataException>(() => retained.RequireEcho(replacement));

        var replacementGrant = GrantFor(replacement);
        Assert.Throws<InvalidDataException>(() =>
            OwnerManifestAdmissionGate.RequireEligible(retained.Selection, replacementGrant));

        Assert.Equal("Q1", retained.QualificationId);
        Assert.Equal(RecordHash, retained.QualificationRecordSha256);
    }

    [Fact]
    public void DirectIntegratedStartManifestGateIsWiredBeforePreparedAndMint()
    {
        var runTransaction = Method(typeof(OwnerPreparationTransaction), "RunTransaction");
        var gate = Method(typeof(OwnerManifestAdmissionGate), "TryRequire");
        var requireInstallation = Method(typeof(OwnerAuthorityContext.Qualified), "RequireLoadedInstallation");
        var decodeAuthorization = Method(typeof(OwnerManifestAuthorizationCodec), "TryDecode");
        var publish = Method(typeof(PreparationPublisher), "Publish");
        var journalConstructor = DefaultConstructor(typeof(JournalFileStorage));

        var gateOffsets = IlCallScanner.CallOffsets(runTransaction, gate);
        Assert.Single(gateOffsets);
        Assert.Single(IlCallScanner.CallOffsets(runTransaction, requireInstallation));

        var decodeOffsets = IlCallScanner.CallOffsets(runTransaction, decodeAuthorization);
        var publishOffsets = IlCallScanner.CallOffsets(runTransaction, publish);
        var journalOffsets = IlCallScanner.CallOffsets(runTransaction, journalConstructor);
        Assert.NotEmpty(decodeOffsets);
        Assert.NotEmpty(publishOffsets);
        Assert.NotEmpty(journalOffsets);

        // Ordering: protected context before the journal/writer, operation-bound
        // final admission after the manifest is known, and before the mint.
        Assert.True(IlCallScanner.CallOffsets(runTransaction, requireInstallation)[0] < journalOffsets[0],
            "the verified installation must be required before journal construction");
        Assert.True(gateOffsets[0] > decodeOffsets[0],
            "the final-admission gate must follow manifest authorization decoding");
        Assert.True(gateOffsets[0] < publishOffsets[0],
            "the final-admission gate must precede capability publication");
    }

    [Fact]
    public void ValidationContextRemainsNonConstructibleAndCannotSatisfyTheGate()
    {
        var validation = typeof(OwnerAuthorityContext).GetNestedType(
            "Validation", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(validation);
        Assert.All(validation!.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            constructor => Assert.True(constructor.IsPrivate));
        Assert.True(!validation
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Any(method => method.ReturnType == validation || method.ReturnType == typeof(OwnerAuthorityContext)));
        Assert.True(!typeof(OwnerAuthorityContext)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Any(method => method.ReturnType == validation));

        var gateMethods = typeof(OwnerManifestAdmissionGate)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToArray();
        Assert.All(gateMethods.Where(method => method.Name == "TryRequire"),
            method => Assert.Equal(typeof(OwnerAuthorityContext.Qualified),
                method.GetParameters()[0].ParameterType));
        Assert.DoesNotContain(gateMethods, method =>
            method.GetParameters().Any(parameter => parameter.ParameterType == validation));
        Assert.DoesNotContain(gateMethods, method =>
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(bool)));
        Assert.DoesNotContain(typeof(OwnerManifestAdmissionGate)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly),
            property => property.PropertyType == typeof(bool));
    }

    [Fact]
    public void LegacyContextCannotSatisfyInstalledManifestAdmission()
    {
        Assert.IsType<OwnerAuthorityContext.Legacy>(OwnerAuthorityContext.LegacyContext);
        Assert.IsNotType<OwnerAuthorityContext.Qualified>(OwnerAuthorityContext.LegacyContext);

        var tryRequire = Method(typeof(OwnerManifestAdmissionGate), "TryRequire");
        Assert.Equal(typeof(OwnerAuthorityContext.Qualified), tryRequire.GetParameters()[0].ParameterType);

        var gateMethods = typeof(OwnerManifestAdmissionGate)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(gateMethods, method => method.GetParameters().Any(parameter =>
            parameter.ParameterType == typeof(OwnerAuthorityContext) ||
            parameter.ParameterType == typeof(OwnerAuthorityContext.Legacy)));
    }

    [Fact]
    public void ManifestGateHasExactlyOneProductionCallSite()
    {
        var gate = Method(typeof(OwnerManifestAdmissionGate), "TryRequire");
        var callSites = new List<string>();
        foreach (var type in typeof(OwnerManifestAdmissionGate).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetMethodBody() is null)
                {
                    continue;
                }

                if (IlCallScanner.CallOffsets(method, gate).Count > 0)
                {
                    callSites.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        Assert.Equal(new[] { "OwnerPreparationTransaction.RunTransaction" },
            callSites.OrderBy(site => site, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void StoppingReconciliationAndRecoveryDoNotRequireRenewedManifestAdmission()
    {
        var gate = Method(typeof(OwnerManifestAdmissionGate), "TryRequire");
        var types = new[]
        {
            typeof(HeldReleasePhase),
            typeof(ReleaseArbiter),
            typeof(Reconciler),
            typeof(M1RecoveryScope),
            typeof(RecoveryConnection),
            typeof(QualifiedRecoveryConnection),
            typeof(IntegratedB3Client),
        };

        foreach (var type in types)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetMethodBody() is null)
                {
                    continue;
                }

                Assert.True(IlCallScanner.CallOffsets(method, gate).Count == 0,
                    $"{type.Name}.{method.Name} must not consume manifest admission");
            }
        }
    }
}
