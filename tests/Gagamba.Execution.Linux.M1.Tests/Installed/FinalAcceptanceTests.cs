using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installed;

public sealed class FinalAcceptanceTests
{
    private const string Host = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string RecordHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string OwnerDecision = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    private static InstalledManifest Manifest() => InstalledManifest.Parse(InstalledMetadataTests.Vector());

    private static InstalledResult Selection(InstalledManifest manifest, string? raw = null) =>
        InstalledResult.Qualified(manifest, "Q1", raw ?? RecordHash);

    private static JsonObject Record(InstalledManifest manifest, string? qualificationRecordSha256 = null,
        string? ownerDecisionSha256 = null, string? hostId = null)
    {
        return new JsonObject
        {
            ["acceptance"] = "final-native-accepted",
            ["acceptedAtUtc"] = "2026-11-02T00:00:00Z",
            ["artifactSetSha256"] = manifest.Digest,
            ["buildConfiguration"] = "Production",
            ["candidateEvidenceSha256"] = new string('1', 64),
            ["hostId"] = hostId ?? Host,
            ["independentReviewSha256"] = new string('2', 64),
            ["limitations"] = new JsonArray("portable test only"),
            ["nativeGateResultsSha256"] = new string('3', 64),
            ["ownerDecisionSha256"] = ownerDecisionSha256 ?? OwnerDecision,
            ["platformInventorySha256"] = InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"),
            ["postpublicationEvidenceSha256"] = new string('4', 64),
            ["prepublicationEvidenceSha256"] = new string('5', 64),
            ["profileId"] = InstalledManifest.Profile,
            ["profileVersion"] = InstalledManifest.Version,
            ["qualificationId"] = "Q1",
            ["qualificationPlanSha256"] = new string('6', 64),
            ["qualificationRecordSha256"] = qualificationRecordSha256 ?? RecordHash,
            ["reviewCommit"] = new string('7', 40),
            ["rid"] = InstalledManifest.Rid,
            ["schemaVersion"] = 1,
            ["scope"] = InstalledManifest.Scope,
            ["sourceCommit"] = manifest.SourceCommit,
            ["sourceTree"] = manifest.SourceTree,
        };
    }

    private static JsonObject Selector(byte[] recordBytes, InstalledManifest manifest,
        string? qualificationId = "Q1", string? qualificationRecordSha256 = null,
        string? finalRecordSha256 = null, string? ownerDecisionSha256 = null, bool accepted = true)
    {
        return new JsonObject
        {
            ["accepted"] = accepted,
            ["artifactSetSha256"] = manifest.Digest,
            ["finalRecordSha256"] = finalRecordSha256 ?? FinalAcceptanceCodec.Hash(recordBytes),
            ["ownerDecisionSha256"] = ownerDecisionSha256 ?? OwnerDecision,
            ["qualificationId"] = qualificationId ?? "Q1",
            ["qualificationRecordSha256"] = qualificationRecordSha256 ?? RecordHash,
            ["schemaVersion"] = 1,
        };
    }

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    private static byte[] CanonicalBytes(JsonObject node) => Bytes(node);

    private static FinalAdmissionBinding Evaluate(InstalledManifest manifest, byte[] selectorBytes,
        byte[] recordBytes, string host = Host, string? raw = null) =>
        FinalAdmissionEvaluation.Evaluate(selectorBytes, recordBytes, host,
            FinalAdmissionExpectation.FromSelection(Selection(manifest, raw)));

    [Fact]
    public void CanonicalRecordAndSelectorBindToRetainedS2Selection()
    {
        var manifest = Manifest();
        var recordBytes = CanonicalBytes(Record(manifest));
        var selectorBytes = CanonicalBytes(Selector(recordBytes, manifest));
        var binding = Evaluate(manifest, selectorBytes, recordBytes);

        Assert.Equal(manifest.Digest, binding.Record.ArtifactSetSha256);
        Assert.Equal(RecordHash, binding.Record.QualificationRecordSha256);
        Assert.Equal(Host, binding.Record.HostId);
        Assert.Equal(FinalAcceptanceCodec.Hash(recordBytes), binding.FinalRecordSha256);
        Assert.Equal(OwnerDecision, binding.Selector.OwnerDecisionSha256);
        Assert.True(AdmissionAuthority.IsFinalAdmissionBindingEligible(Selection(manifest), binding));
    }

    [Fact]
    public void EmptyLimitationsAndMaximumLimitationsAreAccepted()
    {
        var manifest = Manifest();
        var record = Record(manifest);
        record["limitations"] = new JsonArray();
        var bytes = CanonicalBytes(record);
        Assert.Empty(FinalAcceptanceCodec.ParseRecord(bytes).Limitations);

        var many = Record(manifest);
        many["limitations"] = new JsonArray(Enumerable.Range(0, 32)
            .Select(index => (JsonNode?)("limitation " + index)).ToArray());
        Assert.Equal(32, FinalAcceptanceCodec.ParseRecord(CanonicalBytes(many)).Limitations.Count);
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
    public void RefusesNoncanonicalCjson(string mutation)
    {
        var manifest = Manifest();
        var raw = Encoding.UTF8.GetString(CanonicalBytes(Record(manifest)));
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
                bytes = Encoding.UTF8.GetBytes(raw.Replace(InstalledManifest.Profile,
                    "ae4\\/linux-fixed-fixture-v1"));
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
            default:
                throw new ArgumentException(mutation);
        }
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(bytes));
    }

    [Theory]
    [InlineData("unknown-field")]
    [InlineData("missing-field")]
    [InlineData("unsorted")]
    [InlineData("wrong-acceptance")]
    [InlineData("wrong-build")]
    [InlineData("wrong-host")]
    [InlineData("bad-qualification-id")]
    [InlineData("bad-time")]
    [InlineData("bad-time-offset")]
    [InlineData("too-many-limitations")]
    [InlineData("empty-limitation")]
    [InlineData("long-limitation")]
    [InlineData("uppercase-hash")]
    [InlineData("short-hash")]
    [InlineData("long-hash")]
    [InlineData("null-host")]
    public void RefusesMalformedRecordSchema(string mutation)
    {
        var manifest = Manifest();
        var record = Record(manifest);
        switch (mutation)
        {
            case "unknown-field":
                record = InsertBefore(record, "hostId", "extra", "x");
                break;
            case "missing-field":
                record.Remove("limitations");
                break;
            case "unsorted":
                record = Reorder(record, "acceptedAtUtc", "acceptance");
                break;
            case "wrong-acceptance":
                record["acceptance"] = "accepted";
                break;
            case "wrong-build":
                record["buildConfiguration"] = "Test";
                break;
            case "wrong-host":
                record["hostId"] = "A" + new string('a', 63);
                break;
            case "bad-qualification-id":
                record["qualificationId"] = "Q/1";
                break;
            case "bad-time":
                record["acceptedAtUtc"] = "2026-02-30T00:00:00Z";
                break;
            case "bad-time-offset":
                record["acceptedAtUtc"] = "2026-11-02T00:00:00+00:00";
                break;
            case "too-many-limitations":
                record["limitations"] = new JsonArray(Enumerable.Range(0, 33)
                    .Select(index => (JsonNode?)("limitation " + index)).ToArray());
                break;
            case "empty-limitation":
                record["limitations"] = new JsonArray("");
                break;
            case "long-limitation":
                record["limitations"] = new JsonArray(new string('x', 513));
                break;
            case "uppercase-hash":
                record["artifactSetSha256"] = manifest.Digest.ToUpperInvariant();
                break;
            case "short-hash":
                record["artifactSetSha256"] = new string('a', 63);
                break;
            case "long-hash":
                record["artifactSetSha256"] = new string('a', 65);
                break;
            case "null-host":
                var raw = Encoding.UTF8.GetString(CanonicalBytes(record));
                Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(
                    Encoding.UTF8.GetBytes(raw.Replace("\"hostId\":\"" + Host + "\"", "\"hostId\":null"))));
                return;
            default:
                throw new ArgumentException(mutation);
        }
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(CanonicalBytes(record)));
    }

    [Fact]
    public void RefusesOversizeAndNonObjectRoots()
    {
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(new byte[65_537]));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord("[]"u8.ToArray()));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord("1"u8.ToArray()));
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseRecord(Array.Empty<byte>()));
    }

    [Theory]
    [InlineData("accepted-false")]
    [InlineData("wrong-schema")]
    [InlineData("unknown-field")]
    [InlineData("missing-field")]
    [InlineData("bad-qualification-id")]
    [InlineData("uppercase-hash")]
    [InlineData("short-hash")]
    [InlineData("boolean-not-true")]
    public void RefusesMalformedSelector(string mutation)
    {
        var manifest = Manifest();
        var recordBytes = CanonicalBytes(Record(manifest));
        var selector = Selector(recordBytes, manifest);
        switch (mutation)
        {
            case "accepted-false":
                selector["accepted"] = false;
                break;
            case "wrong-schema":
                selector["schemaVersion"] = 2;
                break;
            case "unknown-field":
                selector = InsertBefore(selector, "finalRecordSha256", "extra", "x");
                break;
            case "missing-field":
                selector.Remove("ownerDecisionSha256");
                break;
            case "bad-qualification-id":
                selector["qualificationId"] = "..";
                break;
            case "uppercase-hash":
                selector["artifactSetSha256"] = manifest.Digest.ToUpperInvariant();
                break;
            case "short-hash":
                selector["finalRecordSha256"] = new string('a', 63);
                break;
            case "boolean-not-true":
                selector["accepted"] = "true";
                break;
            default:
                throw new ArgumentException(mutation);
        }
        Assert.Throws<InvalidDataException>(() => FinalAcceptanceCodec.ParseSelector(CanonicalBytes(selector)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\n")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa \n")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\r\n")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg\n")]
    public void HostIdentityRequiresExactly64LowerHexAndOneLineFeed(string value) =>
        Assert.Throws<InvalidDataException>(() =>
            FinalAcceptanceCodec.ParseHostId(Encoding.ASCII.GetBytes(value)));

    [Fact]
    public void HostIdentityAcceptsExactProtectedEncoding()
    {
        Assert.Equal(Host, FinalAcceptanceCodec.ParseHostId(Encoding.ASCII.GetBytes(Host + "\n")));
    }

    [Theory]
    [InlineData("selector-artifact")]
    [InlineData("selector-id")]
    [InlineData("selector-raw")]
    [InlineData("selector-owner")]
    [InlineData("record-hash")]
    [InlineData("record-artifact")]
    [InlineData("record-id")]
    [InlineData("record-raw")]
    [InlineData("record-profile")]
    [InlineData("record-version")]
    [InlineData("record-rid")]
    [InlineData("record-scope")]
    [InlineData("record-platform")]
    [InlineData("record-source-commit")]
    [InlineData("record-source-tree")]
    [InlineData("record-host")]
    [InlineData("record-owner")]
    public void RefusesFinalBindingMismatch(string mutation)
    {
        var manifest = Manifest();
        var record = Record(manifest);
        JsonObject selector;
        switch (mutation)
        {
            case "selector-artifact":
                selector = Selector(CanonicalBytes(record), manifest);
                selector["artifactSetSha256"] = new string('e', 64);
                break;
            case "selector-id":
                selector = Selector(CanonicalBytes(record), manifest, qualificationId: "Q2");
                break;
            case "selector-raw":
                selector = Selector(CanonicalBytes(record), manifest, qualificationRecordSha256: new string('e', 64));
                break;
            case "selector-owner":
                selector = Selector(CanonicalBytes(record), manifest, ownerDecisionSha256: new string('e', 64));
                break;
            case "record-hash":
                var original = CanonicalBytes(record);
                selector = Selector(original, manifest);
                record["acceptedAtUtc"] = "2026-11-03T00:00:00Z";
                break;
            case "record-artifact":
                record["artifactSetSha256"] = new string('e', 64);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-id":
                record["qualificationId"] = "Q2";
                selector = Selector(CanonicalBytes(record), manifest, qualificationId: "Q2");
                break;
            case "record-raw":
                record["qualificationRecordSha256"] = new string('e', 64);
                selector = Selector(CanonicalBytes(record), manifest, qualificationRecordSha256: new string('e', 64));
                break;
            case "record-profile":
                record["profileId"] = "other";
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-version":
                record["profileVersion"] = "2";
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-rid":
                record["rid"] = "linux-arm64";
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-scope":
                record["scope"] = "other";
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-platform":
                record["platformInventorySha256"] = new string('e', 64);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-source-commit":
                record["sourceCommit"] = new string('e', 40);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-source-tree":
                record["sourceTree"] = new string('e', 40);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-host":
                record["hostId"] = new string('e', 64);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            case "record-owner":
                record["ownerDecisionSha256"] = new string('e', 64);
                selector = Selector(CanonicalBytes(record), manifest);
                break;
            default:
                throw new ArgumentException(mutation);
        }
        var recordBytes = CanonicalBytes(record);
        Assert.Throws<InvalidDataException>(() =>
            Evaluate(manifest, CanonicalBytes(selector), recordBytes));
    }

    [Fact]
    public void S2QualifiedAloneIsNotFinalOrProductionAdmission()
    {
        var manifest = Manifest();
        var selection = Selection(manifest);
        Assert.False(AdmissionAuthority.IsFinalAdmissionEligible(selection, null));
        Assert.False(AdmissionAuthority.IsFinalAdmissionBindingEligible(selection, null));
        Assert.False(AdmissionAuthority.IsProductionAdmissionAuthorized(selection, null, null));
    }

    [Fact]
    public void FinalBindingDoesNotSatisfyADifferentS2Selection()
    {
        var manifest = Manifest();
        var recordBytes = CanonicalBytes(Record(manifest));
        var binding = Evaluate(manifest, CanonicalBytes(Selector(recordBytes, manifest)), recordBytes);

        Assert.False(AdmissionAuthority.IsFinalAdmissionBindingEligible(
            InstalledResult.Fresh(manifest), binding));
        Assert.False(AdmissionAuthority.IsFinalAdmissionBindingEligible(
            Selection(manifest, new string('e', 64)), binding));

        var changed = new JsonObject();
        foreach (var property in JsonNode.Parse(InstalledMetadataTests.Vector())!.AsObject())
            changed[property.Key] = property.Value!.DeepClone();
        changed["source"]!["commit"] = new string('e', 40);
        var otherManifest = InstalledManifest.Parse(CanonicalBytes(changed));
        Assert.False(AdmissionAuthority.IsFinalAdmissionBindingEligible(
            Selection(otherManifest), binding));
    }

    [Fact]
    public void ProductionActivationHasNoFactoryInThisSlice()
    {
        var type = typeof(ProductionActivationAuthorization);
        Assert.All(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic),
            constructor => Assert.True(constructor.IsPrivate));
        Assert.DoesNotContain(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
            method => method.ReturnType == type && method.GetParameters().Length == 0);
    }

    private static JsonObject InsertBefore(JsonObject source, string before, string key, JsonNode? value)
    {
        var result = new JsonObject();
        foreach (var property in source)
        {
            if (property.Key == before) result[key] = value?.DeepClone();
            result[property.Key] = property.Value?.DeepClone();
        }
        return result;
    }

    private static JsonObject Reorder(JsonObject source, string first, string second)
    {
        var result = new JsonObject();
        result[first] = source[first]!.DeepClone();
        result[second] = source[second]!.DeepClone();
        foreach (var property in source)
            if (property.Key != first && property.Key != second)
                result[property.Key] = property.Value!.DeepClone();
        return result;
    }
}
