using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Behavioral parity between the new CJSON-1 codec and the accepted
/// F02 <see cref="FinalAcceptanceCodec"/> where the contracts overlap. The F02
/// codec exposes no canonical serializer, so parity is acceptance/rejection
/// behavior plus exact round-trip identity of accepted canonical bytes.</summary>
public sealed class Cjson1F02ParityTests
{
    private static string Hex(char value, int length) => new(value, length);

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static JsonObject Record() => new()
    {
        ["acceptance"] = "final-native-accepted",
        ["acceptedAtUtc"] = "2026-11-02T00:00:00Z",
        ["artifactSetSha256"] = Hex('1', 64),
        ["buildConfiguration"] = "Production",
        ["candidateEvidenceSha256"] = Hex('2', 64),
        ["hostId"] = Hex('3', 64),
        ["independentReviewSha256"] = Hex('4', 64),
        ["limitations"] = new JsonArray("portable test only"),
        ["nativeGateResultsSha256"] = Hex('5', 64),
        ["ownerDecisionSha256"] = Hex('6', 64),
        ["platformInventorySha256"] = Hex('7', 64),
        ["postpublicationEvidenceSha256"] = Hex('8', 64),
        ["prepublicationEvidenceSha256"] = Hex('9', 64),
        ["profileId"] = "profile",
        ["profileVersion"] = "1.0",
        ["qualificationId"] = "Q1",
        ["qualificationPlanSha256"] = Hex('a', 64),
        ["qualificationRecordSha256"] = Hex('b', 64),
        ["reviewCommit"] = Hex('c', 40),
        ["rid"] = "linux-x64",
        ["schemaVersion"] = 1,
        ["scope"] = "test",
        ["sourceCommit"] = Hex('d', 40),
        ["sourceTree"] = Hex('e', 40),
    };

    private static JsonObject Selector(string finalRecordSha256) => new()
    {
        ["accepted"] = true,
        ["artifactSetSha256"] = Hex('1', 64),
        ["finalRecordSha256"] = finalRecordSha256,
        ["ownerDecisionSha256"] = Hex('6', 64),
        ["qualificationId"] = "Q1",
        ["qualificationRecordSha256"] = Hex('b', 64),
        ["schemaVersion"] = 1,
    };

    private static JsonObject InsertBefore(JsonObject source, string target, string key, JsonNode? value)
    {
        var result = new JsonObject();
        foreach (var pair in source)
        {
            if (pair.Key == target) result[key] = value;
            result[pair.Key] = pair.Value?.DeepClone();
        }

        return result;
    }

    [Fact]
    public void CanonicalF02FixturesAreAcceptedByBothCodecs()
    {
        var recordBytes = Bytes(Record());
        var selectorBytes = Bytes(Selector(FinalAcceptanceCodec.Hash(recordBytes)));

        Assert.True(CanonicalJson.TryParse(recordBytes, out var recordRoot, out var failure), failure);
        Assert.Equal(recordBytes, CanonicalJson.Encode(recordRoot));
        Assert.NotNull(FinalAcceptanceCodec.ParseRecord(recordBytes));

        Assert.True(CanonicalJson.TryParse(selectorBytes, out var selectorRoot, out failure), failure);
        Assert.Equal(selectorBytes, CanonicalJson.Encode(selectorRoot));
        Assert.NotNull(FinalAcceptanceCodec.ParseSelector(selectorBytes));
    }

    [Fact]
    public void HashAndLowerHexPrimitivesMatchAcceptedF02Behavior()
    {
        foreach (var sample in new[] { ValidationFixtures.M2, ValidationFixtures.F3, "{}", "{\"a\":1}" })
        {
            var bytes = Encoding.UTF8.GetBytes(sample);
            Assert.Equal(FinalAcceptanceCodec.Hash(bytes), CanonicalJson.Hash(bytes));
        }

        foreach (var (value, length) in new[] { ("a", 1), ("abcdef", 6), ("A", 1), ("g", 1), ("abc", 2) })
            Assert.Equal(FinalAcceptanceCodec.IsLowerHex(value, length), CanonicalJson.IsLowerHex(value, length));
    }

    [Theory]
    [InlineData("trailing-space")]
    [InlineData("trailing-newline")]
    [InlineData("bom")]
    [InlineData("non-ascii")]
    [InlineData("duplicate")]
    [InlineData("multiple-objects")]
    [InlineData("unicode-escape")]
    [InlineData("slash-escape")]
    [InlineData("negative-schema")]
    [InlineData("float-schema")]
    [InlineData("exponent-schema")]
    [InlineData("leading-zero")]
    [InlineData("empty-string")]
    public void NoncanonicalFormsAreRejectedByBothCodecs(string mutation)
    {
        var raw = Encoding.UTF8.GetString(Bytes(Record()));
        byte[] bytes;
        switch (mutation)
        {
            case "trailing-space":
                bytes = Encoding.UTF8.GetBytes(raw + " ");
                break;
            case "trailing-newline":
                bytes = Encoding.UTF8.GetBytes(raw + "\n");
                break;
            case "bom":
                bytes = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(raw)];
                break;
            case "non-ascii":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"Production\"", "\"Productión\""));
                break;
            case "duplicate":
                bytes = Encoding.UTF8.GetBytes(raw.Replace(
                    "\"acceptance\":\"final-native-accepted\",",
                    "\"acceptance\":\"final-native-accepted\",\"acceptance\":\"final-native-accepted\","));
                break;
            case "multiple-objects":
                bytes = Encoding.UTF8.GetBytes(raw + "{}");
                break;
            case "unicode-escape":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"Q1\"", "\"Q\\u0031\""));
                break;
            case "slash-escape":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"rid\":\"linux-x64\"", "\"rid\":\"linux\\/x64\""));
                break;
            case "negative-schema":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"schemaVersion\":1", "\"schemaVersion\":-1"));
                break;
            case "float-schema":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"schemaVersion\":1", "\"schemaVersion\":1.0"));
                break;
            case "exponent-schema":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"schemaVersion\":1", "\"schemaVersion\":1e0"));
                break;
            case "leading-zero":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"schemaVersion\":1", "\"schemaVersion\":01"));
                break;
            case "empty-string":
                bytes = Encoding.UTF8.GetBytes(raw.Replace("\"profile\"", "\"\""));
                break;
            default:
                throw new ArgumentException(mutation);
        }

        Assert.False(CanonicalJson.TryParse(bytes, out _, out _));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(bytes));
    }

    [Fact]
    public void NullAcceptanceDivergenceIsSchemaScoped()
    {
        var record = Record();
        record["hostId"] = null;
        var bytes = Bytes(record);
        // The shared CJSON-1 codec permits null structurally (E-2 B2 policies and
        // pre-phase catalogs require null fields); the F02 final-record schema
        // forbids null values entirely and refuses them in its own codec.
        Assert.True(CanonicalJson.TryParse(bytes, out _, out _));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(bytes));
        Assert.True(CanonicalJson.TryParse(Bytes(ValidationFixtures.F2), out _, out _));
        Assert.True(CanonicalJson.TryParse(Bytes(ValidationFixtures.K2), out _, out _));
    }

    [Fact]
    public void UnknownFieldRejectionIsSchemaScoped()
    {
        var withExtra = InsertBefore(Record(), "hostId", "extra", 1);
        var bytes = Bytes(withExtra);
        // The codec is schema-free and accepts a structurally canonical object;
        // the F02 schema rejects the extension, and the P1 K/M/F/S schemas do
        // the same for their own artifacts (see FrozenContractNegativeTests).
        Assert.True(CanonicalJson.TryParse(bytes, out _, out _));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(bytes));
    }
}
