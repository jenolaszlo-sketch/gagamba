using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Gagamba.Execution.Linux.M1.Tests.Lifecycle;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

// P4-02 F01 deterministic checks. Positive data below is deliberately
// synthetic: it exercises exact tuple decisions and never claims a protected
// installation or final-admission read.
public sealed class RecoveryInitializationAdmissionTests
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

    private static InstalledResult Bound(string qualificationId = "Q1", string recordHash = RecordHash) =>
        InstalledResult.Qualified(Manifest(), qualificationId, recordHash);

    private static JsonObject FinalRecord(InstalledManifest manifest, string qualificationId,
        string qualificationRecordSha256) =>
        new()
        {
            ["acceptance"] = "final-native-accepted",
            ["acceptedAtUtc"] = "2026-11-02T00:00:00Z",
            ["artifactSetSha256"] = manifest.Digest,
            ["buildConfiguration"] = "Production",
            ["candidateEvidenceSha256"] = new string('1', 64),
            ["hostId"] = Host,
            ["independentReviewSha256"] = new string('2', 64),
            ["limitations"] = new JsonArray("simulated test fixture"),
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
        string qualificationId, string qualificationRecordSha256) =>
        new()
        {
            ["accepted"] = true,
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
        var manifest = selection.Manifest!;
        var qualificationId = selection.QualificationId!;
        var recordHash = selection.QualificationRecordSha256!;
        var recordBytes = Bytes(FinalRecord(manifest, qualificationId, recordHash));
        var selectorBytes = Bytes(Selector(recordBytes, manifest, qualificationId, recordHash));
        var binding = FinalAdmissionEvaluation.Evaluate(selectorBytes, recordBytes, Host,
            FinalAdmissionExpectation.FromSelection(selection));
        return FinalAdmissionVerifier.Grant.ForTest(binding, selection);
    }

    private static LoadedInstallation Loaded(InstalledResult selection) =>
        (LoadedInstallation)typeof(LoadedInstallation)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke([selection]);

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Single(method => method.Name == name && method.DeclaringType == type);

    private static ConstructorInfo Constructor(Type type, int parameterCount) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(constructor => constructor.GetParameters().Length == parameterCount);

    [Fact]
    public void ExactSyntheticS2TupleAndGrantPassSharedEligibilityDecision()
    {
        var selection = Bound();

        var error = Record.Exception(() => RecoveryInitializationAdmission.RequireEligible(
            selection, selection.Identity!, GrantFor(selection)));

        Assert.Null(error);
    }

    [Fact]
    public void MissingGrantAndAuthenticatedS2AloneRefuse()
    {
        var selection = Bound();

        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(selection, selection.Identity!, null));
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.Require(null, selection.Identity));
    }

    [Fact]
    public void WrongFullIdentityQualificationIdAndRecordHashRefuse()
    {
        var retained = Bound();
        var grant = GrantFor(retained);
        var otherIdentity = retained.Identity! with { Generation = new string('9', 64) };
        var wrongManifest = InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash);
        var wrongId = Bound("Q2");
        var wrongHash = Bound("Q1", new string('9', 64));

        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(retained, otherIdentity, grant));
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(wrongManifest, retained.Identity!, grant));
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(wrongId, retained.Identity!, grant));
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(wrongHash, retained.Identity!, grant));
    }

    [Fact]
    public void FreshGrantObservationCannotReplaceTheRetainedQualification()
    {
        var retained = Bound();
        var accepted = GrantFor(retained);
        var replacedObservation = FinalAdmissionVerifier.Grant.ForTest(accepted.Binding, Bound("Q2"));

        Assert.Throws<InvalidDataException>(() => RecoveryInitializationAdmission.RequireEligible(
            retained, retained.Identity!, replacedObservation));
        Assert.Equal("Q1", retained.QualificationId);
        Assert.Equal(RecordHash, retained.QualificationRecordSha256);
    }

    [Fact]
    public void WithdrawnGrantRefusesAndSyntheticLoadedSelectionCannotPassProtectedRead()
    {
        var selection = Bound();
        var accepted = GrantFor(selection);
        var withdrawn = FinalAdmissionVerifier.Grant.ForTest(
            accepted.Binding with { Selector = accepted.Binding.Selector with { Accepted = false } },
            accepted.Selection);
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireEligible(selection, selection.Identity!, withdrawn));

        // The reflection-created object is test-only simulation. The local
        // Require path must still enter the real fixed-root verifier; this host
        // has no provisioned protected positive, so it refuses closed.
        Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.Require(Loaded(selection), selection.Identity));
    }

    [Fact]
    public void DirectIntegratedRunRefusesMissingRetainedContextBeforeInitializationEffects()
    {
        var selection = Bound();
        var config = IntegratedConfig(selection.Identity!);
        var journal = config.Integrated!.Profile.JournalDirectory;
        var initializationEntries = 0;
        RecoveryInitializationTestFixture.BeforeInitialization.Value = () => initializationEntries++;

        Exception? refusal;
        try
        {
            refusal = Record.Exception(() => IntegratedRecoveryHost.Run(
                config, null!, null!, 17, 18, "unused-owner-path", installed: null));
        }
        finally
        {
            RecoveryInitializationTestFixture.BeforeInitialization.Value = null;
        }

        Assert.Equal(0, initializationEntries);
        var error = Assert.IsType<InvalidDataException>(refusal);
        Assert.Contains("retained installed authority", error.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void DirectIntegratedRunRejectsMismatchedRetainedIdentityBeforeInitializationEffects()
    {
        var selection = Bound();
        var config = IntegratedConfig(InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash).Identity!);
        var journal = config.Integrated!.Profile.JournalDirectory;
        var initializationEntries = 0;
        RecoveryInitializationTestFixture.BeforeInitialization.Value = () => initializationEntries++;

        Exception? refusal;
        try
        {
            refusal = Record.Exception(() => IntegratedRecoveryHost.Run(
                config, null!, null!, 17, 18, "unused-owner-path", Loaded(selection)));
        }
        finally
        {
            RecoveryInitializationTestFixture.BeforeInitialization.Value = null;
        }

        Assert.Equal(0, initializationEntries);
        var error = Assert.IsType<InvalidDataException>(refusal);
        Assert.Contains("installation binding", error.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(journal));
    }

    [Fact]
    public void LocalGateDominatesJournalWriterAndLaunchInitializationOnActualRunWiring()
    {
        var run = Method(typeof(IntegratedRecoveryHost), "Run");
        var gate = Method(typeof(RecoveryInitializationAdmission), "RequireIntegrated");
        var gateOffset = Assert.Single(IlCallScanner.CallOffsets(run, gate));
        var effects = new (string Name, MethodBase Method)[]
        {
            ("journal storage construction", Constructor(typeof(JournalFileStorage), 0)),
            ("ownership journal construction", Constructor(typeof(OwnershipJournal), 3)),
            ("integrated owner launch construction", Constructor(typeof(IntegratedOwnerLaunch), 6)),
        };

        foreach (var (name, effect) in effects)
        {
            var offsets = IlCallScanner.CallOffsets(run, effect);
            Assert.NotEmpty(offsets);
            Assert.True(gateOffset < offsets[0], $"initialization admission must precede {name}");
        }
    }

    [Fact]
    public void LegacyIntegratedContextRejectsInstalledSelectionAndDoesNotFallBack()
    {
        var config = IntegratedConfig(null) with { Version = 1 };
        var loaded = Loaded(Bound());

        var error = Assert.Throws<InvalidDataException>(() =>
            RecoveryInitializationAdmission.RequireIntegrated(config, loaded, 17, 18));

        Assert.Contains("legacy recovery cannot carry installed authority", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacySetupAttemptIsObservedThenRefusesAtNativePrerequisites()
    {
        var config = IntegratedConfig(null) with { Version = 1 };
        var entries = 0;
        RecoveryInitializationTestFixture.BeforeInitialization.Value = () => entries++;
        try
        {
            // This is an attempted-setup control for the observer, not native
            // success. Its deliberately wrong runtime fails VerifyCurrent.
            Assert.Throws<IOException>(() => IntegratedRecoveryHost.Run(
                config, null!, null!, 17, 18, "unused-owner-path"));
        }
        finally
        {
            RecoveryInitializationTestFixture.BeforeInitialization.Value = null;
        }
        Assert.Equal(1, entries);
        Assert.False(Directory.Exists(config.Integrated!.Profile.JournalDirectory));
    }

    [Fact]
    public void RecoveryEchoWorkerChecksAdmissionBeforeOwnerLaunchPlanAndParentAcquisition()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var require = Method(typeof(RecoveryInitializationAdmission), "Require");
        var workers = typeof(QualifiedRecoveryEcho).GetNestedTypes(flags)
            .SelectMany(type => type.GetMethods(flags))
            .Where(method => method.GetMethodBody() is not null &&
                IlCallScanner.CallOffsets(method, require).Count != 0).ToArray();
        var worker = Assert.Single(workers);
        var gateOffset = Assert.Single(IlCallScanner.CallOffsets(worker, require));
        var verifyOffset = Assert.Single(IlCallScanner.CallOffsets(worker, Method(typeof(LoadedInstallation), "Verify")));
        var planOffset = Assert.Single(IlCallScanner.CallOffsets(worker, Method(typeof(QualifiedLauncher.InstalledPlan), "Prepare")));
        Assert.True(verifyOffset < gateOffset && gateOffset < planOffset);

        var host = Method(typeof(RecoveryHost), "RunCore");
        var echoOffset = Assert.Single(IlCallScanner.CallOffsets(host, Method(typeof(QualifiedRecoveryEcho), "Run")));
        var parentOffsets = IlCallScanner.CallOffsets(host, Method(typeof(M1RecoveryScope), "TryOpenDelegatedParent"));
        Assert.Equal(2, parentOffsets.Count);
        // The first source branch is explicitly Version==1. The installed
        // integrated branch is the second call, following successful echo.
        Assert.True(parentOffsets[0] < echoOffset && echoOffset < parentOffsets[1]);
    }

    private static RecoveryConfiguration IntegratedConfig(InstallationIdentity? identity)
    {
        string root = Path.Combine(Path.GetTempPath(), "gagamba-recovery-init-" + Guid.NewGuid().ToString("N"));
        var runtime = new ExperimentalRuntimePin("/unused/dotnet", new string('a', 64), "/unused/runtime",
            new string('b', 64), "/unused/hostfxr", new string('c', 64), "0.0.0");
        var profile = new B3FixtureProfile("/unused/owner.dll", new string('d', 64), "/unused/parent",
            Path.Combine(root, "source"), Path.Combine(root, "staging"), Path.Combine(root, "journal"),
            "tool", "input", "coordinator", "filter", new string('e', 64), new string('f', 64),
            new string('1', 64), "tool-id", "input-id", 1024, 1, 1);
        var spec = new IntegratedRecoverySpecification(profile, Guid.NewGuid(), Guid.NewGuid(),
            1, 2, new DirectProcessIdentity(123, 1000, 1000, "pid", "user"), runtime, identity);
        var pin = new PinnedExecutable("/unused", new Dictionary<string, string>());
        return new RecoveryConfiguration(2, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123, 1000,
            new byte[32], pin, pin, Path.Combine(root, "evidence"), Integrated: spec);
    }
}
