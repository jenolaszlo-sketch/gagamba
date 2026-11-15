using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationPreCodecBootstrapTests
{
    private static byte[] Payload(int topology) => ValidationPreCodecVectors.Payload(topology);

    private static ValidationPreContext Context(int topology, ushort version = 3) =>
        new(version, (ValidationPreTopology)topology);

    private static void Verify(int topology, byte[] bytes, ValidationPreRefusal expected,
        ushort version = 3, byte[]? digest = null, JsonElement? projection = null)
    {
        var actual = ValidationPreCodec.DecodeBootstrap(bytes, Context(topology, version),
            digest ?? ValidationPreCodecVectors.CapabilityDigest,
            projection ?? ValidationPreCodecVectors.Projection(topology), out var value);
        Assert.Equal(expected, actual);
        if (expected == ValidationPreRefusal.None)
        {
            Assert.NotNull(value);
            Assert.Equal(Context(topology).Topology, value.Topology);
            Assert.Equal(3, value.Fields.GetProperty("Version").GetInt32());
        }
        else
        {
            Assert.Null(value);
        }
    }

    [Theory]
    [InlineData(1, 2693, "52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf")]
    [InlineData(2, 4519, "46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c")]
    public void CompleteWitnesses(int topology, int length, string sha256)
    {
        var bytes = Payload(topology);
        Assert.Equal(length, bytes.Length);
        Assert.Equal(sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        Verify(topology, bytes, ValidationPreRefusal.None);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryWitnessMemberIsRequiredAndHasASpecificType(int topology)
    {
        var root = JsonNode.Parse(Payload(topology))!.AsObject();
        int CheckedMembers(JsonObject obj)
        {
            var count = 0;
            foreach (var (name, original) in obj.ToArray())
            {
                count++;
                if (original is JsonObject child)
                    count += CheckedMembers(child);

                obj.Remove(name);
                Verify(topology, Encoding.UTF8.GetBytes(root.ToJsonString()), ValidationPreRefusal.Schema);
                obj[name] = new JsonArray();
                Verify(topology, Encoding.UTF8.GetBytes(root.ToJsonString()), ValidationPreRefusal.Schema);
                obj[name] = original;
            }
            return count;
        }

        // Shared role and identity inventories occur once in each witness.
        Assert.Equal(topology == 1 ? 46 : 83, CheckedMembers(root));
        Verify(topology, Encoding.UTF8.GetBytes(root.ToJsonString()), ValidationPreRefusal.None);
    }

    [Theory]
    [InlineData("B2InstallationIdentity")]
    [InlineData("B2DeadlineNs")]
    public void B2OnlyMembersHaveIndependentPresenceRules(string member)
    {
        var b2 = JsonNode.Parse(Payload(1))!.AsObject();
        var integrated = JsonNode.Parse(Payload(2))!.AsObject();
        var value = b2[member]!.DeepClone();

        b2[member] = null;
        Verify(1, Encoding.UTF8.GetBytes(b2.ToJsonString()), ValidationPreRefusal.Schema);
        b2.Remove(member);
        Verify(1, Encoding.UTF8.GetBytes(b2.ToJsonString()), ValidationPreRefusal.Schema);

        integrated[member] = null;
        Verify(2, Encoding.UTF8.GetBytes(integrated.ToJsonString()), ValidationPreRefusal.Schema);
        integrated[member] = value;
        Verify(2, Encoding.UTF8.GetBytes(integrated.ToJsonString()), ValidationPreRefusal.Schema);
    }

    [Fact]
    public void Q1E01EscapedNameDecodesToTheRequiredMember()
    {
        Verify(2, Replace(Payload(2), "\"AuthorityHash\"", """ "\u0041uthorityHash" """.Trim()),
            ValidationPreRefusal.None);
    }

    [Fact]
    public void Q1E01RefusesDuplicateDecodedNamesAtRootAndInsideObjects()
    {
        var digest = Convert.ToHexStringLower(ValidationPreCodecVectors.CapabilityDigest);
        Verify(2, Replace(Payload(2), "\"AuthorityHash\":", $"""
            "\u0041uthorityHash":"{digest}","AuthorityHash":
            """), ValidationPreRefusal.Schema);
        Verify(2, Replace(Payload(2), "\"Controller\":{\"Pid\":123", """ "Controller":{"P\u0069d":123,"Pid":123 """.Trim()),
            ValidationPreRefusal.Schema);
    }

    [Fact]
    public void Q1E01RefusesEscapedDigestValueEvenWhenItDecodesToTheMatchingDigest()
    {
        var digest = Convert.ToHexStringLower(ValidationPreCodecVectors.CapabilityDigest);
        Verify(1, Replace(Payload(1), digest, @"\u0062" + digest[1..]), ValidationPreRefusal.Schema);
    }

    [Theory]
    [InlineData("Qualified", "true")]
    [InlineData("Qualified", "\"false\"")]
    [InlineData("Qualified", "0")]
    [InlineData("Qualified", "null")]
    [InlineData("qualificationId", "\"record\"")]
    [InlineData("qualificationId", "{}")]
    [InlineData("qualificationRecordSha256", "\"\"")]
    [InlineData("qualificationRecordSha256", "{}")]
    public void NestedQualificationStateCannotChange(string name, string replacement)
    {
        var original = name == "Qualified" ? "false" : "null";
        foreach (var topology in new[] { 1, 2 })
            Verify(topology, Replace(Payload(topology), $"\"{name}\":{original}", $"\"{name}\":{replacement}"),
                ValidationPreRefusal.Schema);
    }

    [Theory]
    [InlineData("uppercase")]
    [InlineData("mixed case")]
    [InlineData("prefix")]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("space")]
    [InlineData("nonhex")]
    [InlineData("alternate encoding")]
    [InlineData("null")]
    [InlineData("number")]
    public void AuthorityHashRejectsOtherLexicalForms(string form)
    {
        var h = Convert.ToHexStringLower(ValidationPreCodecVectors.CapabilityDigest);
        var value = form switch
        {
            "uppercase" => h.ToUpperInvariant(),
            "mixed case" => "B" + h[1..],
            "prefix" => "0x" + h,
            "short" => h[..^1],
            "long" => h + "0",
            "space" => " " + h[1..],
            "nonhex" => "x" + h[1..],
            "alternate encoding" => Convert.ToBase64String(ValidationPreCodecVectors.CapabilityDigest),
            _ => null
        };
        var original = "\"" + h + "\"";
        var replacement = value != null ? "\"" + value + "\"" : form == "null" ? "null" : "3";
        Verify(1, Replace(Payload(1), original, replacement), ValidationPreRefusal.Schema);
    }

    [Fact]
    public void CapabilityComparisonUsesOnlyTheSeparateExpectation()
    {
        var payload = Payload(1);
        var digest = ValidationPreCodecVectors.CapabilityDigest;
        Verify(1, payload, ValidationPreRefusal.None, digest: digest);
        digest[^1] ^= 0x80;
        Verify(1, payload, ValidationPreRefusal.Comparison, digest: digest);
        Verify(1, payload, ValidationPreRefusal.Expectation, digest: []);
        Verify(1, payload, ValidationPreRefusal.Expectation, digest: digest[..31]);
        Verify(1, payload, ValidationPreRefusal.Expectation, digest: [.. digest, 0]);
        Verify(1, payload, ValidationPreRefusal.Expectation, projection: default(JsonElement));
        using var missing = JsonDocument.Parse("{}");
        Verify(1, payload, ValidationPreRefusal.Comparison, projection: missing.RootElement);
    }

    [Fact]
    public void LiteralAllZeroCapabilityHashHasNoAdditionalLexicalRestriction()
    {
        var original = Convert.ToHexStringLower(ValidationPreCodecVectors.CapabilityDigest);
        var zeros = new string('0', 64);
        Verify(1, Replace(Payload(1), original, zeros), ValidationPreRefusal.None, digest: new byte[32]);
        Verify(1, Replace(Payload(1), original, zeros), ValidationPreRefusal.Comparison);
    }

    [Theory]
    [InlineData(8191, true)]
    [InlineData(8192, true)]
    [InlineData(8193, false)]
    public void PayloadBoundaryCountsWholeUtf8Input(int count, bool passes)
    {
        var p = Payload(2);
        var padded = new byte[count];
        padded.AsSpan().Fill((byte)' ');
        p.CopyTo(padded, 1);
        padded[0] = (byte)'\t';
        padded[^1] = (byte)'\n';
        Verify(2, padded, passes ? ValidationPreRefusal.None : ValidationPreRefusal.Size);
    }

    [Fact]
    public void InvalidUtf8AndTrailingJsonRefuse()
    {
        var p = Payload(1);
        var invalid = (byte[])p.Clone();
        var at = invalid.AsSpan().IndexOf("case-1"u8);
        Assert.True(at > 0);
        invalid[at] = 0xC0;
        invalid[at + 1] = 0xAF;
        foreach (var bytes in new byte[][] { invalid, [0xef, 0xbb, 0xbf, .. p], [.. p, (byte)'{', (byte)'}'],
                     [.. p[..^1], (byte)',', (byte)'}'], [.. "//note\n"u8, ..p] })
            Verify(1, bytes, ValidationPreRefusal.Encoding);
        Verify(1, [], ValidationPreRefusal.Size);
    }

    [Theory]
    [InlineData("/var/lib/gagamba/evidence/case-1", @"/var/lib/gagamba/evidence/\ud800")]
    [InlineData("/var/lib/gagamba/evidence/case-1", @"/var/lib/gagamba/evidence/\udc00")]
    [InlineData("/var/lib/gagamba/evidence/case-1", "/var/lib/gagamba/evidence/\u00e9")]
    [InlineData("\"EvidenceDirectory\"", @"""\ud800EvidenceDirectory""")]
    [InlineData("\"Version\":3", "\"Version\":3,\"extra\":3")]
    [InlineData("\"Recovery\":{\"Directory\":", "\"Recovery\":{\"Extra\":null,\"Directory\":")]
    [InlineData("\"ControllerPid\":123", "\"ControllerPid\":1e2")]
    [InlineData("\"ControllerPid\":123", "\"ControllerPid\":2147483648")]
    [InlineData("\"B2DeadlineNs\":3000000000", "\"B2DeadlineNs\":9223372036854775808")]
    [InlineData("\"ControllerUid\":1000", "\"ControllerUid\":0")]
    [InlineData("\"B2DeadlineNs\":3000000000", "\"B2DeadlineNs\":-1")]
    [InlineData("\"EvidenceDirectory\":\"/var/lib", "\"EvidenceDirectory\":\"/../var/lib")]
    public void MalformedOrOutOfDomainMembersRefuse(string from, string to) =>
        Verify(1, Replace(Payload(1), from, to), ValidationPreRefusal.Schema);

    [Fact]
    public void PermittedWhitespaceOrderAndEscapesPreserveContent()
    {
        var root = JsonNode.Parse(Payload(1))!.AsObject();
        var reordered = new JsonObject(root.Reverse().Select(pair =>
            KeyValuePair.Create(pair.Key, pair.Value?.DeepClone())));
        var bytes = Encoding.UTF8.GetBytes("\n\t" + reordered.ToJsonString() + "\r\n");
        Verify(1, Replace(bytes, "/var/lib/gagamba/evidence/", @"\/var/lib/gagamba/evidence/"),
            ValidationPreRefusal.None);
    }

    [Theory]
    [InlineData("\"Pid\":123", "\"Pid\":124")]
    [InlineData("\"Uid\":1000", "\"Uid\":1001")]
    [InlineData("\"ExpiresAtNs\":3000000000", "\"ExpiresAtNs\":1000000000")]
    [InlineData("/usr/lib/dotnet/host/fxr/10.0.0", "/usr/lib/dotnet/host/fxr/10.0.1")]
    [InlineData("\"DelegatedParentPath\":\"/sys/fs/cgroup/gagamba\"", "\"DelegatedParentPath\":\"/sys/fs/cgroup/other\"")]
    [InlineData("\"JournalDirectory\":\"/var/lib/gagamba/journal/va-00000000000000000000000000000001\"", "\"JournalDirectory\":\"/var/lib/gagamba/journal/va-ffff\"")]
    public void IntegratedRelationshipsAreEnforcedBeforeSuppliedProjectionComparison(string from, string to) =>
        Verify(2, Replace(Payload(2), from, to), ValidationPreRefusal.Relationship);

    [Theory]
    [InlineData("\"ControllerPid\":123", "\"ControllerPid\":124")]
    [InlineData("\"B2DeadlineNs\":3000000000", "\"B2DeadlineNs\":3000000001")]
    [InlineData("case-1", "case-2")]
    public void IndependentlySuppliedProjectionMustMatch(string from, string to) =>
        Verify(1, Replace(Payload(1), from, to), ValidationPreRefusal.Comparison);

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("4")]
    [InlineData("3.0")]
    [InlineData("3e0")]
    [InlineData("\"3\"")]
    public void NewCodecRefusesLegacyOrOtherVersionTokens(string token) =>
        Verify(1, Replace(Payload(1), "\"Version\":3", "\"Version\":" + token), ValidationPreRefusal.Schema);

    [Fact]
    public void SelectedContextAndPayloadCannotMixVersionsOrTopologies()
    {
        Verify(1, Payload(1), ValidationPreRefusal.Context, version: 2);
        Verify(0, Payload(1), ValidationPreRefusal.Context);
        Verify(2, Payload(1), ValidationPreRefusal.Schema);
        Verify(1, Payload(2), ValidationPreRefusal.Schema);
        Verify(1, Replace(Payload(1), "validation-pre", "validation-post"), ValidationPreRefusal.Schema);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ExistingBootstrapDecoderStillRefusesTheAdoptedV3Bytes(int topology)
    {
        var exception = Record.Exception(() => RecoveryConfiguration.Decode(Payload(topology)));
        Assert.True(exception is JsonException or InvalidDataException, exception?.GetType().Name);
    }

    private static byte[] Replace(byte[] bytes, string from, string to)
    {
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains(from, text);
        return Encoding.UTF8.GetBytes(text.Replace(from, to, StringComparison.Ordinal));
    }
}
