using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Lifecycle;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Gagamba.Execution.Restricted.Control;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Restricted;

// P4-03 provider admission. Synthetic selections and grants below exercise
// deterministic decisions only; no synthetic fixture is treated as protected authority.
public sealed class B3ProviderAdmissionTests
{
    private const string RecordHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Host = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string OwnerDecision = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    private static InstalledManifest Manifest() => InstalledManifest.Parse(InstalledMetadataTests.Vector());

    private static InstalledManifest ChangedManifest()
    {
        var root = JsonNode.Parse(InstalledMetadataTests.Vector())!;
        root["source"]!["commit"] = new string('c', 40);
        return InstalledManifest.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
    }

    private static InstalledResult Qualified(string id = "Q1", string hash = RecordHash) =>
        InstalledResult.Qualified(Manifest(), id, hash);

    private static LoadedInstallation Loaded(InstalledResult selection) =>
        (LoadedInstallation)typeof(LoadedInstallation)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke([selection]);

    private static B3FixtureProfile Profile(string journalDirectory, bool qualified = true,
        B3IntegratedProfile? integrated = null) => new(
        OwnerDllPath: "owner.dll",
        OwnerDllSha256: new string('0', 64),
        DelegatedParentPath: "parent",
        SourceRootPath: "source",
        StagingParentPath: "staging",
        JournalDirectory: journalDirectory,
        ToolSourceName: "tool",
        InputSourceName: "input",
        CoordinatorSourceName: "coordinator",
        FilterSourceName: "filter",
        ToolSha256: new string('0', 64),
        InputSha256: new string('0', 64),
        CoordinatorSha256: new string('0', 64),
        ToolEnrollmentId: "tool-enrollment",
        InputEnrollmentId: "input-enrollment",
        TotalBudgetBytes: 1,
        MaxObjects: 4,
        ExpiresLeadNanoseconds: 1,
        IntegratedRecovery: integrated)
        { QualifiedInstallation = qualified };

    private static B3IntegratedProfile Integrated() => new(
        new PinnedExecutable("recovery", new()),
        new PinnedExecutable("owner", new()),
        new ExperimentalRuntimePin("host", "0", "runtime", "0", "hostfxr", "0", "version"),
        "evidence");

    private static string TempJournal() => Path.Combine(Path.GetTempPath(), "gagamba-p403-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ExistingTwoArgumentQualifiedIntegratedConstructorRefusesMissingProviderBeforeJournal()
    {
        var journal = TempJournal();
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => new B3PreparationOwner(
                Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor()));
            Assert.Equal("qualified B3 construction requires retained provider authority", error.Message);
            Assert.False(Directory.Exists(journal));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Fact]
    public void ThreeArgumentConstructorRefusesNullProviderBeforeJournal()
    {
        var journal = TempJournal();
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => new B3PreparationOwner(
                Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor(), null));
            Assert.Equal("qualified B3 construction requires retained provider authority", error.Message);
            Assert.False(Directory.Exists(journal));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Fact]
    public void QualifiedIntegratedConstructionRejectsUnqualifiedAndUnavailableSelectionsBeforeJournal()
    {
        var manifest = Manifest();
        foreach (var selection in new[]
        {
            InstalledResult.Fresh(manifest),
            InstalledResult.Unqualified("no acceptance"),
            InstalledResult.Unavailable("selector missing"),
        })
        {
            var journal = TempJournal();
            try
            {
                var error = Assert.Throws<InvalidDataException>(() => new B3PreparationOwner(
                    Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor(), Loaded(selection)));
                Assert.False(Directory.Exists(journal));
                Assert.False(string.IsNullOrWhiteSpace(error.Message));
            }
            finally
            {
                if (Directory.Exists(journal)) Directory.Delete(journal);
            }
        }
    }

    [Fact]
    public void SyntheticQualifiedContextCannotSubstituteForProviderS2AndFinalAdmissionReads()
    {
        // This reflection-created object is only a negative fixture. The real
        // provider verifier must reject it on this unprovisioned host.
        var journal = TempJournal();
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => new B3PreparationOwner(
                Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor(), Loaded(Qualified())));
            Assert.False(Directory.Exists(journal));
            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Fact]
    public void PureExactSelectionAndGrantTuplePassesEligibilityDecision()
    {
        var selected = Qualified();
        B3ProviderAdmission.RequireEligible(selected, GrantFor(selected));
    }

    [Fact]
    public void MissingOrWithdrawnGrantRefuses()
    {
        var selected = Qualified();
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(selected, null));
        var grant = GrantFor(selected);
        var withdrawn = FinalAdmissionVerifier.Grant.ForTest(
            grant.Binding with { Selector = grant.Binding.Selector with { Accepted = false } }, grant.Selection);
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(selected, withdrawn));
    }

    [Fact]
    public void ChangedSelectionIdentityQualificationIdAndRecordHashRefuse()
    {
        var selected = Qualified();
        var grant = GrantFor(selected);
        var wrongIdentity = InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash);
        var wrongId = Qualified("Q2");
        var wrongHash = Qualified("Q1", new string('9', 64));

        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(wrongIdentity, grant));
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(wrongId, grant));
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(wrongHash, grant));
    }

    [Fact]
    public void GrantSelectionCannotReplaceOriginalS2Selection()
    {
        var selected = Qualified();
        var accepted = GrantFor(selected);
        var replaced = FinalAdmissionVerifier.Grant.ForTest(accepted.Binding, Qualified("Q2"));
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(selected, replaced));
        Assert.Equal("Q1", selected.QualificationId);
        Assert.Equal(RecordHash, selected.QualificationRecordSha256);
    }

    [Fact]
    public void ValidationShapedSelectionAndLegacyContextMixCannotGrantProviderAdmission()
    {
        var manifest = Manifest();
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.RequireEligible(
            InstalledResult.Fresh(manifest), GrantFor(Qualified())));
        Assert.Throws<InvalidDataException>(() => B3ProviderAdmission.Require(
            Profile(TempJournal(), qualified: false), Loaded(Qualified())));
    }

    [Fact]
    public void ProviderAdmissionDominatesScopeClientAndJournalSetupOnActualConstructor()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var ctor = typeof(B3PreparationOwner).GetConstructors(flags).Single(c => c.GetParameters().Length == 3);
        var gate = typeof(B3ProviderAdmission).GetMethod("Require", flags)!;
        var gateOffset = Assert.Single(IlCallScanner.CallOffsets(ctor, gate));
        var effects = new (string Name, MemberInfo Member)[]
        {
            ("legacy scope", typeof(M1RecoveryScope).GetConstructors(flags).Single()),
            ("integrated client", typeof(IntegratedB3Client).GetConstructors(flags).Single()),
            ("journal storage", typeof(JournalFileStorage).GetConstructors(flags).Single()),
            ("ownership journal", typeof(OwnershipJournal).GetConstructors(flags).Single(c => c.GetParameters().Length == 3)),
        };
        foreach (var (name, member) in effects)
        {
            var offsets = IlCallScanner.CallOffsets(ctor, member);
            Assert.NotEmpty(offsets);
            Assert.True(gateOffset < offsets[0], $"provider admission must precede {name}");
        }
    }
    [Fact]
    public void ExistingTwoArgumentLegacyConstructorStillWorksWithoutProviderAuthority()
    {
        var journal = TempJournal();
        try
        {
            using var owner = new B3PreparationOwner(
                Profile(journal, qualified: false), new RestrictedProviderDescriptor());
            Assert.True(Directory.Exists(journal));
            Assert.Null(typeof(B3PreparationOwner).GetField("_integrated", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(owner));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("qualification-id")]
    [InlineData("record-hash")]
    public void ActualConstructorCannotAcceptFabricatedOrMismatchedProviderSelections(string mutation)
    {
        // These negative fixtures cannot pass the real provider-role verifier.
        // Exact tuple mismatches are separately exercised by the shared decision.
        var selection = mutation switch
        {
            "identity" => InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash),
            "qualification-id" => Qualified("Q2"),
            _ => Qualified("Q1", new string('9', 64)),
        };
        var journal = TempJournal();
        B3PreparationOwner? escaped = null;
        try
        {
            Assert.Throws<InvalidDataException>(() => escaped = new B3PreparationOwner(
                Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor(), Loaded(selection)));
            Assert.Null(escaped);
            Assert.False(Directory.Exists(journal));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IncompleteQualificationMetadataRefusesBeforeProviderReadsAndConstruction(bool missingId)
    {
        var selection = InstalledResult.Qualified(Manifest(), missingId ? null! : "Q1",
            missingId ? RecordHash : null!);
        var journal = TempJournal();
        try
        {
            var error = Assert.Throws<InvalidDataException>(() => new B3PreparationOwner(
                Profile(journal, integrated: Integrated()), new RestrictedProviderDescriptor(), Loaded(selection)));
            Assert.Equal("qualified B3 construction provider selection refused", error.Message);
            Assert.False(Directory.Exists(journal));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Fact]
    public void TopologyRefusalPrecedesEvenACallerSuppliedInstalledContext()
    {
        var journal = TempJournal();
        try
        {
            Assert.Throws<InvalidOperationException>(() => new B3PreparationOwner(
                Profile(journal), new RestrictedProviderDescriptor(), Loaded(Qualified())));
            Assert.False(Directory.Exists(journal));
        }
        finally
        {
            if (Directory.Exists(journal)) Directory.Delete(journal);
        }
    }

    [Fact]
    public void FactoryUsesContextBearingConstructorAndDirectEntryDelegatesToItsGuard()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var constructors = typeof(B3PreparationOwner).GetConstructors(flags);
        var direct = constructors.Single(c => c.GetParameters().Length == 2);
        var guarded = constructors.Single(c => c.GetParameters().Length == 3);
        Assert.Single(IlCallScanner.CallOffsets(direct, guarded));
        var factory = typeof(B3Qualification).GetMethod("TryCreate", flags)!;
        var construction = Assert.Single(IlCallScanner.CallOffsets(factory, guarded));
        Assert.Empty(IlCallScanner.CallOffsets(factory, direct));
        var verify = typeof(LoadedInstallation).GetMethod("Verify", flags)!;
        Assert.True(Assert.Single(IlCallScanner.CallOffsets(factory, verify)) < construction);
    }

    [Fact]
    public void ProviderRoleAndFreshProtectedFinalCheckUseActualProductionWiring()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var require = typeof(B3ProviderAdmission).GetMethod("Require", flags)!;
        var role = Assert.Single(IlCallScanner.CallOffsets(require, typeof(LoadedInstallation).GetMethod("Verify", flags)!));
        var retainedComparison = Assert.Single(IlCallScanner.CallOffsets(require,
            typeof(ReleaseGenerationGate).GetMethod("RequireStillQualified", flags)!));
        var final = Assert.Single(IlCallScanner.CallOffsets(require,
            typeof(LoadedInstallation).GetMethod("VerifyFinalAdmission", flags)!));
        var decision = Assert.Single(IlCallScanner.CallOffsets(require,
            typeof(B3ProviderAdmission).GetMethod("RequireEligible", flags)!));
        Assert.True(role < retainedComparison && retainedComparison < final && final < decision);
    }

    private static JsonObject FinalRecord(InstalledManifest manifest, string id, string hash) => new()
    {
        ["acceptance"] = "final-native-accepted",
        ["acceptedAtUtc"] = "2026-11-02T00:00:00Z",
        ["artifactSetSha256"] = manifest.Digest,
        ["buildConfiguration"] = "Production",
        ["candidateEvidenceSha256"] = new string('1', 64),
        ["hostId"] = Host,
        ["independentReviewSha256"] = new string('2', 64),
        ["limitations"] = new JsonArray("synthetic decision fixture"),
        ["nativeGateResultsSha256"] = new string('3', 64),
        ["ownerDecisionSha256"] = OwnerDecision,
        ["platformInventorySha256"] = InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"),
        ["postpublicationEvidenceSha256"] = new string('4', 64),
        ["prepublicationEvidenceSha256"] = new string('5', 64),
        ["profileId"] = InstalledManifest.Profile,
        ["profileVersion"] = InstalledManifest.Version,
        ["qualificationId"] = id,
        ["qualificationPlanSha256"] = new string('6', 64),
        ["qualificationRecordSha256"] = hash,
        ["reviewCommit"] = new string('7', 40),
        ["rid"] = InstalledManifest.Rid,
        ["schemaVersion"] = 1,
        ["scope"] = InstalledManifest.Scope,
        ["sourceCommit"] = manifest.SourceCommit,
        ["sourceTree"] = manifest.SourceTree,
    };

    private static FinalAdmissionVerifier.Grant GrantFor(InstalledResult selection)
    {
        var manifest = selection.Manifest!;
        var id = selection.QualificationId!;
        var hash = selection.QualificationRecordSha256!;
        var record = Encoding.UTF8.GetBytes(FinalRecord(manifest, id, hash).ToJsonString());
        var selector = new JsonObject
        {
            ["accepted"] = true,
            ["artifactSetSha256"] = manifest.Digest,
            ["finalRecordSha256"] = FinalAcceptanceCodec.Hash(record),
            ["ownerDecisionSha256"] = OwnerDecision,
            ["qualificationId"] = id,
            ["qualificationRecordSha256"] = hash,
            ["schemaVersion"] = 1,
        };
        var binding = FinalAdmissionEvaluation.Evaluate(
            Encoding.UTF8.GetBytes(selector.ToJsonString()), record, Host,
            FinalAdmissionExpectation.FromSelection(selection));
        return FinalAdmissionVerifier.Grant.ForTest(binding, selection);
    }
}
