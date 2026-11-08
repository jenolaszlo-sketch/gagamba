using System.Reflection;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Tests.Lifecycle;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installed;

// F-02 (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01): explicit parity
// controls for the two security-sensitive installation-verification paths.
//
// Contract (docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f02-arch-decision-01.md):
//   * The fixed-root integrity prefix (steps 1-20: active selection, manifest
//     parse + digest binding, exact payload/dependency closure, store stability)
//     is performed by BOTH InstalledFileSystem.VerifyActive() and
//     InstalledFileSystem.VerifyActiveIntegrity().
//   * Qualification-record association (SelectedRecordName/AcceptRecord) is
//     qualified-path only and must never appear in the prepublication path.
//   * CheckHost is required on the prepublication path and on the qualified
//     admitted branch.
//   * The prepublication path constructs InstalledIntegrityEvidence; the
//     qualified path returns InstalledResult states. The result types never merge.
//
// Evidence levels:
//   * Static: IL metadata-token presence and first-offset order via IlCallScanner.
//     Token order proves static emission order in the straight-line prefix; it
//     does NOT prove full control-flow dominance over every branch, and it cannot
//     prove that a called helper's internal semantics are unchanged.
//   * Behavioral: fail-closed refusal of both paths on an unprovisioned host.
//   * Out of scope (documented): full behavioral equivalence on a provisioned
//     fixed-root store, runtime loaded-role verification-to-use, and native
//     differential parity. Those remain Slice N obligations. RELEASE absence of
//     the Debug-only ForTest seam is established by the canonical production
//     assembly verifier (eng/VerifyProductionAssembly), not by this test project,
//     which references the Debug M1 assembly.
public sealed class InstalledVerificationParityTests
{
    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly Type FileSystem = typeof(InstalledFileSystem);
    private static readonly MethodInfo VerifyActive = Unique(FileSystem, "VerifyActive");
    private static readonly MethodInfo VerifyActiveIntegrity = Unique(FileSystem, "VerifyActiveIntegrity");

    // Resolve a unique member by name. A future overload makes this fail loudly so
    // the parity contract is re-reviewed rather than silently matched.
    private static MethodInfo Unique(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Single(method => method.Name == name);

    private static MethodInfo Target(Type type, string name) => Unique(type, name);

    private static readonly MemberInfo SelectedRecordName = Target(typeof(InstalledMetadata), "SelectedRecordName");
    private static readonly MemberInfo AcceptRecord = Target(typeof(InstalledMetadata), "AcceptRecord");
    private static readonly MemberInfo CheckHost = Target(typeof(InstalledPayload), "CheckHost");
    private static readonly MemberInfo CheckDependencies = Target(typeof(InstalledPayload), "CheckDependencies");
    private static readonly MemberInfo RecheckSelectors = Unique(FileSystem, "RecheckSelectors");
    private static readonly MemberInfo EvidenceConstructor = typeof(InstalledIntegrityEvidence)
        .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(constructor => constructor.GetParameters().Length == 5);

    // Shared integrity prefix, in the accepted order.
    private static readonly (string Label, MemberInfo Target)[] SharedPrefix =
    {
        ("EnsureLinuxX64", Unique(FileSystem, "EnsureLinuxX64")),
        ("OpenAbsoluteDirectory", Unique(FileSystem, "OpenAbsoluteDirectory")),
        ("RequireDirectory", Unique(FileSystem, "RequireDirectory")),
        ("OpenDirectory", Unique(FileSystem, "OpenDirectory")),
        ("ReadTrustedFile", Unique(FileSystem, "ReadTrustedFile")),
        ("InstalledMetadata.Active", Target(typeof(InstalledMetadata), "Active")),
        ("InstalledManifest.Parse", Target(typeof(InstalledManifest), "Parse")),
        ("SameDigest", Unique(FileSystem, "SameDigest")),
        ("InstalledMetadata.Receipt", Target(typeof(InstalledMetadata), "Receipt")),
        ("CreateScan", Unique(FileSystem, "CreateScan")),
        ("MetadataArtifact", Unique(FileSystem, "MetadataArtifact")),
        ("Walk", Unique(FileSystem, "Walk")),
        ("EnsureExact", Unique(FileSystem, "EnsureExact")),
        ("InstalledPayload.CheckDependencies", CheckDependencies),
    };

    private static bool References(MethodBase method, MemberInfo target) =>
        IlCallScanner.CallOffsets(method, target).Count > 0;

    private static string[] CallersOf(Assembly assembly, MethodBase target)
    {
        var callers = new List<string>();
        foreach (var type in assembly.GetTypes())
        {
            var members = type.GetMethods(AllDeclared).Cast<MethodBase>()
                .Concat(type.GetConstructors(AllDeclared));
            foreach (var member in members)
            {
                if (member.GetMethodBody() is null) continue;
                if (IlCallScanner.CallOffsets(member, target).Count == 0) continue;
                callers.Add(type.FullName + "." + member.Name);
            }
        }
        return callers.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void BothVerificationPathsResolveUniquely()
    {
        Assert.Equal("VerifyActive", VerifyActive.Name);
        Assert.Equal("VerifyActiveIntegrity", VerifyActiveIntegrity.Name);
        Assert.True(VerifyActive.IsAssembly, "VerifyActive must remain internal");
        Assert.True(VerifyActiveIntegrity.IsAssembly, "VerifyActiveIntegrity must remain internal");
    }

    [Fact]
    public void SharedIntegrityPrefixAppearsInBothPaths()
    {
        foreach (var (label, target) in SharedPrefix)
        {
            Assert.True(References(VerifyActive, target), $"VerifyActive no longer references {label}");
            Assert.True(References(VerifyActiveIntegrity, target), $"VerifyActiveIntegrity no longer references {label}");
        }
    }

    [Fact]
    public void SharedIntegrityPrefixOrderIsIdenticalInBothPaths()
    {
        foreach (var method in new[] { VerifyActive, VerifyActiveIntegrity })
        {
            var offsets = SharedPrefix
                .Select(entry =>
                {
                    var found = IlCallScanner.CallOffsets(method, entry.Target);
                    Assert.True(found.Count > 0, $"{method.Name} no longer references {entry.Label}");
                    return found[0];
                })
                .ToArray();

            for (var index = 1; index < offsets.Length; index++)
            {
                Assert.True(offsets[index - 1] < offsets[index],
                    $"{method.Name} shared-prefix static order diverged at {SharedPrefix[index].Label}");
            }
        }
    }

    [Fact]
    public void PrepublicationPathNeverEstablishesQualificationAssociation()
    {
        Assert.False(References(VerifyActiveIntegrity, SelectedRecordName),
            "prepublication verification must not select a qualification record");
        Assert.False(References(VerifyActiveIntegrity, AcceptRecord),
            "prepublication verification must not accept a qualification record");
    }

    [Fact]
    public void QualifiedPathRetainsRecordSelectionAndAcceptance()
    {
        Assert.True(References(VerifyActive, SelectedRecordName),
            "qualified verification must retain record selection");
        Assert.True(References(VerifyActive, AcceptRecord),
            "qualified verification must retain record acceptance");
    }

    [Fact]
    public void BothPathsRequireHostBaselineAndSelectorRecheck()
    {
        Assert.True(References(VerifyActive, CheckHost), "qualified verification must retain CheckHost");
        Assert.True(References(VerifyActiveIntegrity, CheckHost), "prepublication verification must retain CheckHost");
        Assert.True(References(VerifyActive, RecheckSelectors), "qualified verification must retain RecheckSelectors");
        Assert.True(References(VerifyActiveIntegrity, RecheckSelectors), "prepublication verification must retain RecheckSelectors");
    }

    [Fact]
    public void ResultTypesRemainDistinct()
    {
        Assert.True(References(VerifyActiveIntegrity, EvidenceConstructor),
            "prepublication verification must construct InstalledIntegrityEvidence");
        Assert.False(References(VerifyActive, EvidenceConstructor),
            "qualified verification must not construct InstalledIntegrityEvidence");

        var fresh = Target(typeof(InstalledResult), "Fresh");
        var qualified = Target(typeof(InstalledResult), "Qualified");
        Assert.True(References(VerifyActive, fresh), "qualified verification must retain InstalledResult.Fresh");
        Assert.True(References(VerifyActive, qualified), "qualified verification must retain InstalledResult.Qualified");
        Assert.False(References(VerifyActiveIntegrity, fresh), "prepublication verification must not return InstalledResult");
        Assert.False(References(VerifyActiveIntegrity, qualified), "prepublication verification must not return InstalledResult");
    }

    [Fact]
    public void IntentionalQualifiedAndPrepublicationDifferencesAreEncoded()
    {
        // This control exists so the parity suite is not flattened into a false
        // "both paths must be identical" rule: the asymmetries are the contract.
        Assert.True(References(VerifyActive, AcceptRecord) && !References(VerifyActiveIntegrity, AcceptRecord),
            "qualification-record acceptance must remain qualified-path only");
        Assert.True(References(VerifyActiveIntegrity, EvidenceConstructor) && !References(VerifyActive, EvidenceConstructor),
            "prepublication evidence construction must remain prepublication-only");
    }

    [Fact]
    public void PrepublicationConstructionRemainsProductionUnreachable()
    {
        var assembly = typeof(InstalledFileSystem).Assembly;

        Assert.Equal(
            new[] { "Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProjection.Verify" },
            CallersOf(assembly, VerifyActiveIntegrity));

        var verify = typeof(PrepublicationIntegrityProjection)
            .GetMethod("Verify", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(verify);
        Assert.True(CallersOf(assembly, verify!).Length == 0,
            "PrepublicationIntegrityProjection.Verify must have no production caller");

        var fromEvidence = typeof(PrepublicationIntegrityProjection)
            .GetMethod("FromEvidence", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(fromEvidence);
        Assert.True(fromEvidence!.IsPrivate, "FromEvidence must remain private");
    }

    [Fact]
    public void BothPathsFailClosedOnAnUnprovisionedHost()
    {
        if (OperatingSystem.IsLinux())
            Assert.Skip("Protected fixed-root store required for behavioral parity; native-pending (Slice N).");

        // The qualified path reports a typed refusal; the prepublication path
        // throws. Both must refuse; neither may return a trusted result.
        Assert.Equal(InstalledState.Unavailable, InstalledFileSystem.VerifyActive().State);
        Assert.Throws<InvalidDataException>(() => InstalledFileSystem.VerifyActiveIntegrity());
    }
}
