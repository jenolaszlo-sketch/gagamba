// RC2A-02A harness self-tests (test-only). These prove the qualification
// machinery itself: the raw template rebuilds the committed golden literal
// byte-for-byte, its recorded offsets address the intended fields, and the
// assertion helpers actually detect injected differences (so a green suite is
// evidence, not vacuity).
using System.Buffers.Binary;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

public sealed class QualificationHarnessSelfTests
{
    [Fact]
    public void Q2A_Harness01_WireTemplateRebuildsCommittedGolden()
    {
        var golden = QualificationModelTools.GoldenBytes();
        Assert.Equal(873, golden.Length);
        var built = QualificationWireTemplate.Golden().Build(recordRegions: true);
        Assert.Equal(golden, built.Bytes);
        Assert.Equal(QualificationGolden.DigestHex, QualificationHash.Sha256Hex(built.Bytes));
        Assert.Equal("82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3", QualificationGolden.DigestHex);
    }

    [Fact]
    public void Q2A_Harness02_WireTemplateOffsetsAddressTheIntendedFields()
    {
        var built = QualificationWireTemplate.Golden().Build();
        var bytes = built.Bytes;

        Assert.Equal(0, built.Offsets["schema"]);
        Assert.Equal(2, built.Offsets["implementation"]);
        Assert.Equal(27u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(built.Offsets["implementation"])));
        Assert.Equal(33, built.Offsets["namespaces.count"]);
        Assert.Equal(QualificationModelTools.Bytes32(0x01), bytes.AsSpan(built.Offsets["authority.filterSha"], 32).ToArray());
        Assert.Equal(312ul, BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(built.Offsets["authority.filterLength"])));
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(built.Offsets["invocation.mode"])));
        Assert.Equal("work", System.Text.Encoding.UTF8.GetString(
            bytes, built.Offsets["invocation.mode"] + 4, 4));
        Assert.Equal(1, bytes[built.Offsets["channels.stdinEof"]]);
        Assert.Equal(bytes.Length, built.Offsets["channels.aggregateLimit"] + 8);
        Assert.Equal(0, bytes[built.Offsets["core.patternPresent"]]);
    }

    [Fact]
    public void Q2A_Harness03_TemplateAndReferenceEncoderAgreeOnTheSampleGraph()
    {
        var created = QualificationWireTemplate.Golden().Build();
        Assert.Equal(QualificationModelTools.ReferenceEncode(QualificationModelTools.Baseline()), created.Bytes);
    }

    [Fact]
    public void Q2A_Harness04_GraphComparerDetectsInjectedDifferences()
    {
        var model = QualificationModelTools.Baseline();
        var view = new RestrictedManifestView(model);
        QualificationModelTools.AssertGraph(model, view);
        var different = QualificationModelTools.Model(mode: "other");
        Assert.ThrowsAny<Exception>(() => QualificationModelTools.AssertGraph(different, view));
        var differentBudget = QualificationModelTools.WithBudget(
            model, "Memory", new Gagamba.Execution.Restricted.ResourceBudget(
                Gagamba.Execution.Restricted.BudgetValueKind.Exact, 1, "bytes",
                Gagamba.Execution.Restricted.QualificationDisposition.Mandatory));
        Assert.ThrowsAny<Exception>(() => QualificationModelTools.AssertGraph(differentBudget, view));
    }

    [Fact]
    public void Q2A_Harness05_RefusalHarnessDetectsWrongCodeAndWrongOffset()
    {
        var correct = new WireRefusalCase(
            "Q2A-SELF-01", "AuthorityManifest.NoNewPrivs", "tag", "malformed",
            "self-test: tag 2 on the free boolean is InvalidTag at the field start",
            ManifestDecodeError.InvalidTag,
            () => QualificationWireTemplate.Golden().Build("authority.noNewPrivs", w => w.Tag(2)),
            "defect");
        correct.Execute();

        var wrongCode = new WireRefusalCase(
            "Q2A-SELF-02", "AuthorityManifest.NoNewPrivs", "tag", "malformed",
            "self-test: deliberately wrong expected code must fail",
            ManifestDecodeError.InvalidUtf8,
            () => QualificationWireTemplate.Golden().Build("authority.noNewPrivs", w => w.Tag(2)),
            "defect");
        Assert.ThrowsAny<Exception>(wrongCode.Execute);

        var wrongOffset = new WireRefusalCase(
            "Q2A-SELF-03", "AuthorityManifest.NoNewPrivs", "tag", "malformed",
            "self-test: deliberately wrong offset key must fail",
            ManifestDecodeError.InvalidTag,
            () => QualificationWireTemplate.Golden().Build("authority.noNewPrivs", w => w.Tag(2)),
            "schema");
        Assert.ThrowsAny<Exception>(wrongOffset.Execute);
    }

    [Fact]
    public void Q2A_Harness06_ValidityHarnessDetectsRejectedMutationAndStaleExpectation()
    {
        var accepted = new ValidMutationCase(
            "Q2A-SELF-04", "InvocationManifest.Mode", "valid", "self-test: mode change accepted",
            QualificationModelTools.Baseline,
            model => QualificationModelTools.WithInvocation(model, "Mode", "other"),
            (_, view) => Assert.Equal("other", view.Invocation.Mode));
        accepted.Execute();

        var rejected = new ValidMutationCase(
            "Q2A-SELF-05", "InvocationManifest.Mode", "valid", "self-test: forced-invalid mode must fail the case",
            QualificationModelTools.Baseline,
            model => QualificationModelTools.WithInvocation(model, "Mode", string.Empty),
            (_, _) => { });
        Assert.ThrowsAny<Exception>(rejected.Execute);

        var stale = new ValidMutationCase(
            "Q2A-SELF-06", "InvocationManifest.Mode", "valid", "self-test: unchanged value must fail the case",
            QualificationModelTools.Baseline,
            model => QualificationModelTools.WithInvocation(model, "Mode", "work"),
            (_, _) => { });
        Assert.ThrowsAny<Exception>(stale.Execute);
    }

    [Fact]
    public void Q2A_Harness07_GoldenLiteralIsBoundToItsPublishedDigest()
    {
        var golden = QualificationModelTools.GoldenBytes();
        Assert.Equal(
            "82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3",
            QualificationHash.Sha256Hex(golden));
        var decoded = RestrictedManifestCodec.Decode(golden);
        Assert.Null(decoded.Error);
        var view = Assert.IsType<RestrictedManifestView>(decoded.Value);
        Assert.Equal(golden, RestrictedManifestCodec.Encode(view));
        Assert.Equal(Convert.FromHexString(QualificationGolden.DigestHex), RestrictedManifestCodec.ComputeSha256(view).ToArray());
        Assert.True(RestrictedManifestCodec.VerifySha256(view, Convert.FromHexString(QualificationGolden.DigestHex)));
    }
}
