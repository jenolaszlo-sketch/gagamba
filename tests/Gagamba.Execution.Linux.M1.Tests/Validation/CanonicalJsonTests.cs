using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

public sealed class CanonicalJsonTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Theory]
    [InlineData(ValidationFixtures.M2, ValidationFixtures.Hm2)]
    [InlineData(ValidationFixtures.F2, ValidationFixtures.Hf2)]
    [InlineData(ValidationFixtures.S2, ValidationFixtures.Hs2)]
    [InlineData(ValidationFixtures.K2, ValidationFixtures.Hk2)]
    [InlineData(ValidationFixtures.M3, ValidationFixtures.Hm3)]
    [InlineData(ValidationFixtures.F3, ValidationFixtures.Hf3)]
    [InlineData(ValidationFixtures.S3, ValidationFixtures.Hs3)]
    [InlineData(ValidationFixtures.K3, ValidationFixtures.Hk3)]
    public void AcceptedFixturesParseEncodeAndMatchIndependentDigests(string fixture, string expectedDigest)
    {
        var bytes = Bytes(fixture);
        Assert.True(CanonicalJson.TryParse(bytes, out var root, out var failure), failure);
        Assert.Equal(string.Empty, failure);
        Assert.Equal(bytes, CanonicalJson.Encode(root));
        Assert.Equal(expectedDigest, CanonicalJson.Hash(bytes));
    }

    [Fact]
    public void HashMatchesThePlatformPrimitiveAndKnownVector()
    {
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            CanonicalJson.Hash([]));
        var bytes = Bytes(ValidationFixtures.M2);
        Assert.Equal(Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(bytes)), CanonicalJson.Hash(bytes));
    }

    [Theory]
    [InlineData("a", 1, true)]
    [InlineData("0f", 2, true)]
    [InlineData("A", 1, false)]
    [InlineData("f", 2, false)]
    [InlineData("g", 1, false)]
    public void LowerHexHelperMatchesTheAcceptedConvention(string value, int length, bool expected) =>
        Assert.Equal(expected, CanonicalJson.IsLowerHex(value, length));

    [Fact]
    public void NullValuesAreStructurallyPermittedForSchemaLevelRules()
    {
        var bytes = Bytes("{\"a\":null}");
        Assert.True(CanonicalJson.TryParse(bytes, out var root, out _));
        Assert.Equal(bytes, CanonicalJson.Encode(root));
    }

    [Theory]
    [InlineData("trailing-space")]
    [InlineData("trailing-newline")]
    [InlineData("leading-space")]
    [InlineData("inner-whitespace")]
    [InlineData("bom")]
    [InlineData("non-ascii")]
    [InlineData("duplicate-keys")]
    [InlineData("multiple-objects")]
    [InlineData("unicode-escape")]
    [InlineData("slash-escape")]
    [InlineData("negative-number")]
    [InlineData("float-number")]
    [InlineData("exponent-number")]
    [InlineData("leading-zero")]
    [InlineData("empty-string")]
    [InlineData("empty-input")]
    [InlineData("root-array")]
    [InlineData("root-number")]
    [InlineData("root-string")]
    [InlineData("excess-depth")]
    public void RejectsNoncanonicalByteForms(string mutation)
    {
        var valid = ValidationFixtures.M2;
        byte[] bytes;
        switch (mutation)
        {
            case "trailing-space":
                bytes = Bytes(valid + " ");
                break;
            case "trailing-newline":
                bytes = Bytes(valid + "\n");
                break;
            case "leading-space":
                bytes = Bytes(" " + valid);
                break;
            case "inner-whitespace":
                bytes = Bytes("{\"a\": 1}");
                break;
            case "bom":
                bytes = [0xEF, 0xBB, 0xBF, .. Bytes(valid)];
                break;
            case "non-ascii":
                bytes = Bytes("{\"a\":\"\u00e9\"}");
                break;
            case "duplicate-keys":
                bytes = Bytes("{\"a\":1,\"a\":1}");
                break;
            case "multiple-objects":
                bytes = Bytes(valid + "{}");
                break;
            case "unicode-escape":
                bytes = Bytes("{\"a\":\"\\u0041\"}");
                break;
            case "slash-escape":
                bytes = Bytes("{\"a\":\"a\\/b\"}");
                break;
            case "negative-number":
                bytes = Bytes("{\"a\":-1}");
                break;
            case "float-number":
                bytes = Bytes("{\"a\":1.0}");
                break;
            case "exponent-number":
                bytes = Bytes("{\"a\":1e0}");
                break;
            case "leading-zero":
                bytes = Bytes("{\"a\":01}");
                break;
            case "empty-string":
                bytes = Bytes("{\"a\":\"\"}");
                break;
            case "empty-input":
                bytes = [];
                break;
            case "root-array":
                bytes = Bytes("[1]");
                break;
            case "root-number":
                bytes = Bytes("1");
                break;
            case "root-string":
                bytes = Bytes("\"x\"");
                break;
            case "excess-depth":
                bytes = Bytes("{\"a\":[[[[[[[[[1]]]]]]]]]}");
                break;
            default:
                throw new ArgumentException(mutation);
        }

        Assert.False(CanonicalJson.TryParse(bytes, out _, out var failure));
        Assert.NotEqual(string.Empty, failure);
    }

    [Fact]
    public void RejectsInputsOverTheAcceptedSizeLimit()
    {
        var bytes = new byte[CanonicalJson.MaximumBytes + 1];
        Array.Fill(bytes, (byte)'a');
        Assert.False(CanonicalJson.TryParse(bytes, out _, out var failure));
        Assert.Equal("CJSON-1 size", failure);
    }

    [Fact]
    public void EncodeRefusesNoncanonicalValueTrees()
    {
        using var document = JsonDocument.Parse(Bytes("{\"a\":1.0}"));
        Assert.Throws<InvalidDataException>(() => CanonicalJson.Encode(document.RootElement));
    }

    [Fact]
    public void RefusalMessagesAreDeterministicAndInputsAreNotMutated()
    {
        var bytes = Bytes("{\"a\":1.0}");
        var copy = (byte[])bytes.Clone();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.False(CanonicalJson.TryParse(bytes, out _, out var failure));
            Assert.Equal("CJSON-1 unsigned canonical integer", failure);
        }

        Assert.Equal(copy, bytes);
    }
}
