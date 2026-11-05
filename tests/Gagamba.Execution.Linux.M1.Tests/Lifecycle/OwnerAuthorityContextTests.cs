using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Lifecycle;

// P1 (GAGAMBA-RC2B-06-B6-ALT-B-P1-IMPL-01): retain the owner's first verified
// installation selection and compare later echoes against that exact tuple.
// These tests bind the retention decision, not a new admission or release gate.
public sealed class OwnerAuthorityContextTests
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
    public void FirstVerifiedSelectionIsRetainedExactly()
    {
        var bound = Bound();
        var context = OwnerAuthorityContext.Qualified.ForTest(bound);

        Assert.Same(bound, context.Selection);
        Assert.Equal(bound.Identity, context.Identity);
        Assert.Same(bound.Manifest, context.Manifest);
        Assert.Equal("Q1", context.QualificationId);
        Assert.Equal(RecordHash, context.QualificationRecordSha256);
    }

    [Fact]
    public void LaterEchoWithExactRetainedTupleIsAccepted()
    {
        var context = Retained();

        context.RequireEcho(InstalledResult.Qualified(Manifest(), "Q1", RecordHash));

        Assert.Equal(Bound().Identity, context.Identity);
        Assert.Equal("Q1", context.QualificationId);
        Assert.Equal(RecordHash, context.QualificationRecordSha256);
    }

    [Fact]
    public void LaterEchoWithChangedGenerationRefusesAndDoesNotReplaceFirstTuple()
    {
        var context = Retained();
        var firstIdentity = context.Identity;
        var firstManifest = context.Manifest;

        Assert.Throws<InvalidDataException>(() =>
            context.RequireEcho(InstalledResult.Qualified(ChangedManifest(), "Q1", RecordHash)));

        Assert.Equal(firstIdentity, context.Identity);
        Assert.Same(firstManifest, context.Manifest);
        Assert.Equal("Q1", context.QualificationId);
        Assert.Equal(RecordHash, context.QualificationRecordSha256);
    }

    [Fact]
    public void LaterEchoWithChangedQualificationIdRefuses()
    {
        var context = Retained();

        Assert.Throws<InvalidDataException>(() =>
            context.RequireEcho(InstalledResult.Qualified(Manifest(), "Q2", RecordHash)));

        Assert.Equal("Q1", context.QualificationId);
    }

    [Fact]
    public void LaterEchoWithChangedQualificationRecordRefuses()
    {
        var context = Retained();

        Assert.Throws<InvalidDataException>(() =>
            context.RequireEcho(InstalledResult.Qualified(Manifest(), "Q1", new string('9', 64))));

        Assert.Equal(RecordHash, context.QualificationRecordSha256);
    }

    [Fact]
    public void LaterEchoWithUnqualifiedObservationRefuses()
    {
        var context = Retained();

        Assert.Throws<InvalidDataException>(() =>
            context.RequireEcho(InstalledResult.Fresh(Manifest())));
        Assert.Throws<InvalidDataException>(() =>
            context.RequireEcho(InstalledResult.Unqualified("test")));
    }

    [Fact]
    public void LegacyContextCannotBePromotedToQualified()
    {
        var legacy = OwnerAuthorityContext.LegacyContext;

        Assert.IsType<OwnerAuthorityContext.Legacy>(legacy);
        Assert.IsNotType<OwnerAuthorityContext.Qualified>(legacy);
    }

    [Fact]
    public void ValidationContextHasNoCallerConstructibleFactory()
    {
        var validation = typeof(OwnerAuthorityContext).GetNestedType(
            "Validation", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(validation);

        var constructors = validation!.GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotEmpty(constructors);
        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));

        var factories = validation
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType == validation || method.ReturnType == typeof(OwnerAuthorityContext));
        Assert.Empty(factories);

        var outerFactories = typeof(OwnerAuthorityContext)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType == validation);
        Assert.Empty(outerFactories);
    }

    [Fact]
    public void AuthorityContextsHaveNoCallerBooleanSelector()
    {
        var types = new[] { typeof(OwnerAuthorityContext) }
            .Concat(typeof(OwnerAuthorityContext)
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            .ToArray();

        var callables = types
            .SelectMany(type => type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Cast<MethodBase>()
                .Concat(type.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)))
            .ToArray();

        Assert.DoesNotContain(callables,
            callable => callable.GetParameters().Any(parameter => parameter.ParameterType == typeof(bool)));

        var properties = types
            .SelectMany(type => type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(bool));
    }

    [Fact]
    public void AuthorityContextBaseHasNoPublicConstructor()
    {
        var constructors = typeof(OwnerAuthorityContext).GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.All(constructors, constructor => Assert.True(constructor.IsPrivate));
    }
}
