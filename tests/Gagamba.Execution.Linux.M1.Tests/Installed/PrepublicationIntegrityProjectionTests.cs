using System.Reflection;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installed;

// E-1 (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-IMPL-01): the non-forgeable
// prepublication integrity projection. These tests bind the type, its
// construction boundary and its non-forgeability. Positive construction is
// deterministic/test-only; the real protected construction route is
// native-pending and refuses on an unprovisioned host. No test here claims a
// protected positive or validation authority.
public sealed class PrepublicationIntegrityProjectionTests
{
    private static InstalledManifest Manifest() => InstalledManifest.Parse(InstalledMetadataTests.Vector());

    private static InstalledIntegrityEvidence Evidence(InstalledManifest manifest) =>
        new(manifest.Digest, manifest, InstallationIdentity.RoleHash(manifest, 1),
            InstallationIdentity.RoleHash(manifest, 0), InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"));

    [Fact]
    public void VerifyRefusesOnANonLinuxHost()
    {
        if (OperatingSystem.IsLinux())
            Assert.Skip("Protected fixed-root store is required to assert this refusal on Linux; native-pending.");
        Assert.Throws<InvalidDataException>(() => PrepublicationIntegrityProjection.Verify());
    }

    [Fact]
    public void TestSeamPreservesIdentityAndNullQualification()
    {
        var manifest = Manifest();
        var projection = PrepublicationIntegrityProjection.ForTest(Evidence(manifest));

        Assert.Equal(manifest.Digest, projection.ArtifactSetSha256);
        Assert.Equal(manifest.SourceCommit, projection.SourceCommit);
        Assert.Equal(manifest.SourceTree, projection.SourceTree);
        Assert.Equal(InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"), projection.RuntimeInventorySha256);
        Assert.Equal(InstallationIdentity.RoleHash(manifest, 1), projection.OwnerClosureSha256);
        Assert.Equal(InstallationIdentity.RoleHash(manifest, 0), projection.RecoveryClosureSha256);
        Assert.Null(projection.QualificationId);
        Assert.Null(projection.QualificationRecordSha256);
        Assert.Equal(PrepublicationIntegrityProvenance.DeterministicTestOnly, projection.Provenance);
    }

    [Fact]
    public void TestSeamIsDeterministic()
    {
        var first = PrepublicationIntegrityProjection.ForTest(Evidence(Manifest()));
        var second = PrepublicationIntegrityProjection.ForTest(Evidence(Manifest()));

        Assert.Equal(first.ArtifactSetSha256, second.ArtifactSetSha256);
        Assert.Equal(first.SourceCommit, second.SourceCommit);
        Assert.Equal(first.SourceTree, second.SourceTree);
        Assert.Equal(first.RuntimeInventorySha256, second.RuntimeInventorySha256);
        Assert.Equal(first.OwnerClosureSha256, second.OwnerClosureSha256);
        Assert.Equal(first.RecoveryClosureSha256, second.RecoveryClosureSha256);
        Assert.Equal(first.Provenance, second.Provenance);
        Assert.Null(first.QualificationId);
        Assert.Null(second.QualificationId);
    }

    [Fact]
    public void ProjectionStateIsImmutable()
    {
        var type = typeof(PrepublicationIntegrityProjection);
        Assert.True(type.IsSealed);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void ProjectionHasOnlyPrivateConstructors()
    {
        var constructors = typeof(PrepublicationIntegrityProjection).GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotEmpty(constructors);
        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));
    }

    [Fact]
    public void ProjectionHasNoCallerBooleanOrResultFactory()
    {
        var type = typeof(PrepublicationIntegrityProjection);
        // Caller-accessible factories only: private helpers are not construction
        // routes for ordinary callers.
        var factories = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType == type && (method.IsPublic || method.IsAssembly)).ToArray();

        Assert.NotEmpty(factories);
        Assert.All(factories, factory =>
        {
            var parameters = factory.GetParameters();
            Assert.True(parameters.Length == 0 ||
                (parameters.Length == 1 && parameters[0].ParameterType == typeof(InstalledIntegrityEvidence)));
            Assert.DoesNotContain(parameters, parameter =>
                parameter.ParameterType == typeof(bool) || parameter.ParameterType == typeof(InstalledResult));
        });
    }

    [Fact]
    public void ProjectionCannotBePromotedToAuthority()
    {
        var forbidden = new[]
        {
            typeof(OwnerAuthorityContext), typeof(OwnerAuthorityContext.Qualified),
            typeof(OwnerAuthorityContext.Validation), typeof(OwnerAuthorityContext.Legacy),
            typeof(LoadedInstallation), typeof(InstalledResult), typeof(FinalAdmissionVerifier.Grant),
            typeof(ProductionActivationAuthorization), typeof(bool),
        };
        var type = typeof(PrepublicationIntegrityProjection);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Assert.DoesNotContain(method.ReturnType, forbidden);
            Assert.DoesNotContain(method.GetParameters(), parameter => forbidden.Contains(parameter.ParameterType));
        }
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Assert.DoesNotContain(property.PropertyType, forbidden);
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Assert.DoesNotContain(field.FieldType, forbidden);
    }

    [Fact]
    public void ProjectionIsInternalAndNotPublicApi()
    {
        var type = typeof(PrepublicationIntegrityProjection);
        Assert.False(type.IsPublic);
        Assert.False(type.IsNestedPublic);
        var publicInstanceMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName && method.DeclaringType == type)
            .Select(method => method.Name).ToArray();
        Assert.True(publicInstanceMethods.Length == 0,
            "unexpected public members: " + string.Join(", ", publicInstanceMethods));
    }

    [Fact]
    public void ProjectionIsNotReferencedByAnyOtherTypeSignature()
    {
        var assembly = typeof(PrepublicationIntegrityProjection).Assembly;
        var target = typeof(PrepublicationIntegrityProjection);
        var owners = new List<string>();

        foreach (var type in assembly.GetTypes())
        {
            if (type == target) continue;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType == target ||
                    method.GetParameters().Any(parameter => parameter.ParameterType == target))
                    owners.Add(type.FullName + "." + method.Name);
            }
            foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (constructor.GetParameters().Any(parameter => parameter.ParameterType == target))
                    owners.Add(type.FullName + "..ctor");
            }
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.PropertyType == target) owners.Add(type.FullName + "." + property.Name);
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType == target) owners.Add(type.FullName + "." + field.Name);
            }
        }

        Assert.True(owners.Count == 0, "unexpected projection references: " + string.Join(", ", owners));
    }

    [Fact]
    public void StructuralGateAcceptsConsistentEvidence()
    {
        var projection = PrepublicationIntegrityProjection.ForTest(Evidence(Manifest()));
        Assert.Equal(PrepublicationIntegrityProvenance.DeterministicTestOnly, projection.Provenance);
        Assert.Null(projection.QualificationId);
    }

    [Theory]
    [InlineData("wrong-artifact-digest")]
    [InlineData("short-artifact-digest")]
    [InlineData("uppercase-artifact-digest")]
    [InlineData("non-hex-owner-closure")]
    [InlineData("non-hex-recovery-closure")]
    [InlineData("non-hex-runtime-inventory")]
    [InlineData("null-manifest")]
    public void StructuralGateRefusesContradictoryOrMalformedEvidence(string mutation)
    {
        var manifest = Manifest();
        var evidence = Evidence(manifest) with { };
        evidence = mutation switch
        {
            "wrong-artifact-digest" => evidence with { ArtifactSetSha256 = new string('e', 64) },
            "short-artifact-digest" => evidence with { ArtifactSetSha256 = new string('e', 63) },
            "uppercase-artifact-digest" => evidence with { ArtifactSetSha256 = manifest.Digest.ToUpperInvariant() },
            "non-hex-owner-closure" => evidence with { OwnerClosureSha256 = new string('z', 64) },
            "non-hex-recovery-closure" => evidence with { RecoveryClosureSha256 = new string('z', 64) },
            "non-hex-runtime-inventory" => evidence with { RuntimeInventorySha256 = new string('z', 64) },
            "null-manifest" => evidence with { Manifest = null! },
            _ => throw new ArgumentException(mutation),
        };

        Assert.Throws<InvalidDataException>(() => PrepublicationIntegrityProjection.ForTest(evidence));
    }

    // F-01 semantic integrity (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-IMPL-01):
    // every derived field must equal the accepted manifest projection, not merely be
    // well formed. These bind the three relationships added to FromEvidence.
    [Fact]
    public void SemanticGateAcceptsFullyConsistentEvidence()
    {
        var manifest = Manifest();
        var projection = PrepublicationIntegrityProjection.ForTest(Evidence(manifest));

        Assert.Equal(InstallationIdentity.RoleHash(manifest, 1), projection.OwnerClosureSha256);
        Assert.Equal(InstallationIdentity.RoleHash(manifest, 0), projection.RecoveryClosureSha256);
        Assert.Equal(InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"), projection.RuntimeInventorySha256);
        Assert.Null(projection.QualificationId);
        Assert.Null(projection.QualificationRecordSha256);
        Assert.Equal(PrepublicationIntegrityProvenance.DeterministicTestOnly, projection.Provenance);
    }

    // Reproduces the three independent Q1 cases B5/B6/B7: a structurally valid but
    // semantically incorrect derived field must now be refused, not accepted.
    [Theory]
    [InlineData("b5-incorrect-owner-closure")]
    [InlineData("b6-incorrect-recovery-closure")]
    [InlineData("b7-incorrect-runtime-inventory")]
    public void SemanticGateRefusesWellFormedButIncorrectDerivedFields(string mutation)
    {
        var manifest = Manifest();
        var evidence = Evidence(manifest);
        evidence = mutation switch
        {
            "b5-incorrect-owner-closure" => evidence with { OwnerClosureSha256 = new string('e', 64) },
            "b6-incorrect-recovery-closure" => evidence with { RecoveryClosureSha256 = new string('e', 64) },
            "b7-incorrect-runtime-inventory" => evidence with { RuntimeInventorySha256 = new string('e', 64) },
            _ => throw new ArgumentException(mutation),
        };

        Assert.Throws<InvalidDataException>(() => PrepublicationIntegrityProjection.ForTest(evidence));
    }

    [Fact]
    public void SemanticGateRefusesManifestMissingRequiredRoleMaterial()
    {
        var manifest = Manifest();
        var withoutRoles = manifest with { Artifacts = Array.Empty<InstalledArtifact>() };
        var evidence = new InstalledIntegrityEvidence(withoutRoles.Digest, withoutRoles,
            new string('a', 64), new string('b', 64), new string('c', 64));

        // RoleHash resolves each required role artifact with Single and throws when absent.
        Assert.Throws<InvalidOperationException>(() => PrepublicationIntegrityProjection.ForTest(evidence));
    }

    [Fact]
    public void SemanticGateRefusesManifestMissingRuntimeInventoryMetadata()
    {
        var manifest = Manifest();
        using var document = System.Text.Json.JsonDocument.Parse("{}");
        var withoutRuntime = manifest with { Platform = document.RootElement.Clone() };
        var evidence = new InstalledIntegrityEvidence(withoutRuntime.Digest, withoutRuntime,
            InstallationIdentity.RoleHash(manifest, 1), InstallationIdentity.RoleHash(manifest, 0), new string('a', 64));

        // The platform lacks the runtime-inventory property; the manifest value cannot be read.
        Assert.Throws<KeyNotFoundException>(() => PrepublicationIntegrityProjection.ForTest(evidence));
    }
}
