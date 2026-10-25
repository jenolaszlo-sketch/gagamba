// RC2A-02A Q2A-V canonical vector qualification (test-only). Every vector is
// checked through three independent representations: the committed literal hex,
// the test-only raw wire template, and the independent ReferenceEncoder over the
// committed construction recipe. Expected digests are recomputed with BCL
// SHA-256, never with the production codec.
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

public sealed class Q2A_V_VectorQualificationTests
{
    public static IEnumerable<object[]> VectorIds() =>
        QualificationVectorRecipes.All.Select(vector => new object[] { vector.Id });

    [Theory]
    [MemberData(nameof(VectorIds))]
    public void Q2A_V_VectorIsCanonicalAndIndependentlyVerifiable(string id)
    {
        var recipe = QualificationVectorRecipes.All.Single(v => v.Id == id);
        Assert.True(QualificationVectorBytes.ById.TryGetValue(id, out var committed), $"No committed bytes for {id}.");
        var expected = recipe.Expected();

        var bytes = Convert.FromHexString(committed.Hex);
        Assert.Equal(committed.Hex, Convert.ToHexString(bytes).ToLowerInvariant());
        Assert.Equal(committed.Sha256, QualificationHash.Sha256Hex(bytes));

        var rawRebuild = expected.Build();
        Assert.Equal(bytes, rawRebuild.Bytes);
        Assert.Equal(recipe.ExpectedSupports, RestrictedManifestCodec.Supports(expected.SchemaVersion, expected.ImplementationId));

        var model = recipe.Materialize();
        Assert.Equal(bytes, QualificationModelTools.ReferenceEncode(model));
        Assert.Equal(bytes, QualificationModelTools.ProductionEncode(model));

        var decoded = RestrictedManifestCodec.Decode(bytes);
        Assert.Null(decoded.Error);
        Assert.Null(decoded.Offset);
        var view = Assert.IsType<RestrictedManifestView>(decoded.Value);
        QualificationVectorVerifier.VerifyGraph(expected, view);
        Assert.Equal(bytes, RestrictedManifestCodec.Encode(view));
        Assert.Equal(Convert.FromHexString(committed.Sha256), RestrictedManifestCodec.ComputeSha256(view).ToArray());
        Assert.True(RestrictedManifestCodec.VerifySha256(view, Convert.FromHexString(committed.Sha256)));
    }

    [Fact]
    public void Q2A_V_VectorSetIsCompleteDistinctAndBoundToRecipes()
    {
        Assert.Equal(23, QualificationVectorRecipes.All.Length);
        Assert.Equal(
            QualificationVectorRecipes.All.Length,
            QualificationVectorRecipes.All.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            QualificationVectorRecipes.All.Select(v => v.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            QualificationVectorBytes.ById.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.All(QualificationVectorRecipes.All, vector =>
        {
            Assert.False(string.IsNullOrWhiteSpace(vector.Purpose));
            Assert.False(string.IsNullOrWhiteSpace(vector.Recipe));
            Assert.True(vector.ExpectedSupports);
        });
        Assert.Equal(new[]
        {
            "Q2A-V-01-golden",
            "Q2A-V-02-minimal",
            "Q2A-V-03-empty-namespaces",
            "Q2A-V-04-nonascii-namespaces",
            "Q2A-V-05-empty-argv",
            "Q2A-V-06-empty-environment",
            "Q2A-V-07-empty-environment-value",
            "Q2A-V-08-budget-absent",
            "Q2A-V-09-budget-exact-zero",
            "Q2A-V-10-budget-unlimited",
            "Q2A-V-11-inode-budget-present",
            "Q2A-V-12-inode-budget-absent",
            "Q2A-V-13-nonewprivs-false",
            "Q2A-V-14-runtime-dumps-disabled",
            "Q2A-V-15-core-pattern-present",
            "Q2A-V-16-core-pattern-nul-supplementary",
            "Q2A-V-17-supplementary-plane",
            "Q2A-V-18-nonascii-utf8",
            "Q2A-V-19-max-argenv-16384",
            "Q2A-V-20-max-manifest-65536",
            "Q2A-V-21-duplicate-argv",
            "Q2A-V-22-empty-closure",
            "Q2A-V-23-inode-present-and-absent",
        }, QualificationVectorRecipes.All.Select(v => v.Id).ToArray());
    }

    [Fact]
    public void Q2A_V_PreservedGoldenVectorIsUnchanged()
    {
        Assert.Equal(873, QualificationGolden.Hex.Length / 2);
        Assert.Equal(
            "82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3",
            QualificationGolden.DigestHex);
        Assert.Equal(QualificationGolden.Hex, QualificationVectorBytes.GoldenHex);
        Assert.Equal(QualificationGolden.DigestHex, QualificationVectorBytes.GoldenSha256);
        Assert.Equal(QualificationGolden.Hex, QualificationVectorBytes.ById["Q2A-V-01-golden"].Hex);
        Assert.Equal(QualificationGolden.DigestHex, QualificationVectorBytes.ById["Q2A-V-01-golden"].Sha256);
        Assert.Equal(
            QualificationGolden.DigestHex,
            QualificationHash.Sha256Hex(Convert.FromHexString(QualificationGolden.Hex)));
        Assert.Equal(
            Convert.FromHexString(QualificationGolden.Hex),
            RestrictedManifestCodec.Encode(Assert.IsType<RestrictedManifestView>(
                RestrictedManifestCodec.Decode(Convert.FromHexString(QualificationGolden.Hex)).Value)));
    }

    [Fact]
    public void Q2A_V_DeclaredBoundaryVectorsMeasureTheBoundariesInUtf8Bytes()
    {
        var argenv = QualificationVectorRecipes.All.Single(v => v.Id == "Q2A-V-19-max-argenv-16384").Expected();
        var combined = argenv.Argv.Sum(QualificationUtf8.Strict.GetByteCount) +
            argenv.Environment.Sum(pair =>
                QualificationUtf8.Strict.GetByteCount(pair.Key) + QualificationUtf8.Strict.GetByteCount(pair.Value));
        Assert.Equal(16384, combined);
        Assert.Contains(argenv.Argv, item => item.Length < combined);

        var max = QualificationVectorRecipes.All.Single(v => v.Id == "Q2A-V-20-max-manifest-65536").Expected();
        Assert.Equal(65536, max.Build().Bytes.Length);
        Assert.Equal(
            65536,
            RestrictedManifestCodec.Encode(Assert.IsType<RestrictedManifestView>(
                RestrictedManifestCodec.Decode(max.Build().Bytes).Value)).Length);

        var minimal = QualificationVectorRecipes.All.Single(v => v.Id == "Q2A-V-02-minimal").Expected();
        Assert.Empty(minimal.Namespaces);
        Assert.Empty(minimal.Capabilities);
        Assert.Empty(minimal.ReadOnlyDestinations);
        Assert.Empty(minimal.Stores);
        Assert.Empty(minimal.Objects);
        Assert.Empty(minimal.HelperDependencies);
        Assert.Empty(minimal.Argv);
        Assert.Empty(minimal.Environment);
        Assert.All(QualificationWireTemplate.BudgetNames, name =>
        {
            var budget = minimal.Budget(name);
            Assert.Equal(Gagamba.Execution.Restricted.BudgetValueKind.Exact, budget.Kind);
            Assert.Equal(0ul, budget.Value);
        });
        Assert.True(minimal.Build().Bytes.Length < 873);
    }

    [Fact]
    public void Q2A_V_DistinctVectorsHaveDistinctCanonicalDigests()
    {
        var digests = QualificationVectorRecipes.All
            .Select(v => QualificationVectorBytes.ById[v.Id].Sha256)
            .ToArray();
        Assert.Equal(digests.Length, digests.Distinct(StringComparer.Ordinal).Count());
        Assert.All(digests, digest => Assert.Equal(64, digest.Length));
        Assert.All(QualificationVectorRecipes.All, vector =>
        {
            Assert.Equal(
                vector.Expected().Build().Bytes,
                Convert.FromHexString(QualificationVectorBytes.ById[vector.Id].Hex));
        });
    }
}
