using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

public sealed class FrozenContractNegativeTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static string Edit(string source, params (string Find, string Replace)[] edits)
    {
        var text = source;
        foreach (var (find, replace) in edits)
        {
            Assert.Contains(find, text, StringComparison.Ordinal);
            text = text.Replace(find, replace, StringComparison.Ordinal);
        }

        return text;
    }

    private static byte[] Mutate(string source, params (string Find, string Replace)[] edits) =>
        Bytes(Edit(source, edits));

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>Rebuilds the B3 chain hashes after a non-hash field edit so the
    /// targeted disagreement rule (not a hash binding) is the first refusal.</summary>
    private static (byte[] M, byte[] F, byte[] S, byte[] K) Rebuild3(string m, string f, string s, string k)
    {
        var hm = Sha(Bytes(m));
        f = f.Replace(ValidationFixtures.Hm3, hm, StringComparison.Ordinal);
        s = s.Replace(ValidationFixtures.Hm3, hm, StringComparison.Ordinal);
        k = k.Replace(ValidationFixtures.Hm3, hm, StringComparison.Ordinal);
        var hf = Sha(Bytes(f));
        s = s.Replace(ValidationFixtures.Hf3, hf, StringComparison.Ordinal);
        k = k.Replace(ValidationFixtures.Hf3, hf, StringComparison.Ordinal);
        var hs = Sha(Bytes(s));
        k = k.Replace(ValidationFixtures.Hs3, hs, StringComparison.Ordinal);
        return (Bytes(m), Bytes(f), Bytes(s), Bytes(k));
    }

    private static string ChainFailure(byte[] m, byte[] f, byte[] s, byte[] k)
    {
        Assert.False(FrozenContractVerifier.TryVerifyChain(m, f, s, k, out var chain, out var failure));
        Assert.Null(chain);
        Assert.NotEqual(string.Empty, failure);
        return failure;
    }

    private static void ManifestFailure(string expected, byte[] bytes)
    {
        Assert.False(FrozenContractVerifier.TryVerifyManifest(bytes, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void PolicyFailure(string expected, byte[] bytes)
    {
        Assert.False(FrozenContractVerifier.TryVerifyPolicy(bytes, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void ScriptFailure(string expected, byte[] bytes)
    {
        Assert.False(FrozenContractVerifier.TryVerifyActionScript(bytes, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void CatalogFailure(string expected, byte[] bytes)
    {
        Assert.False(FrozenContractVerifier.TryVerifyCatalog(bytes, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    [Fact]
    public void ManifestRefusesMissingField() =>
        ManifestFailure("M field set", Mutate(ValidationFixtures.M2,
            ("\"schemaVersion\":1,", string.Empty)));

    [Fact]
    public void ManifestRefusesDuplicateByteKey() =>
        ManifestFailure("CJSON-1 keys are not strictly ascending", Mutate(ValidationFixtures.M2,
            ("\"caseId\":\"case-2\",", "\"caseId\":\"case-2\",\"caseId\":\"case-2\",")));

    [Fact]
    public void ManifestRefusesB2LatentObject()
    {
        var snapshot = "\"objects\":[{\"destination\":\"/tool\",\"length\":1,\"mode\":\"0555\"," +
                       "\"path\":\"tool\",\"role\":\"tool\",\"sha256\":\"" + new string('a', 64) + "\"}]";
        ManifestFailure("M B2 objects", Mutate(ValidationFixtures.M2, ("\"objects\":[]", snapshot)));
    }

    [Fact]
    public void ManifestRefusesB3RoleOrder() =>
        ManifestFailure("M object role order", Mutate(ValidationFixtures.M3,
            ("\"role\":\"coordinator\"", "\"role\":\"filter\"")));

    [Fact]
    public void ManifestRefusesB3WrongMode() =>
        ManifestFailure("M object mode", Mutate(ValidationFixtures.M3,
            ("\"mode\":\"0555\",\"path\":\"coordinator\"", "\"mode\":\"0444\",\"path\":\"coordinator\"")));

    [Fact]
    public void ManifestRefusesB3WrongDestination() =>
        ManifestFailure("M object destination", Mutate(ValidationFixtures.M3,
            ("\"destination\":\"fd:6\"", "\"destination\":\"/input\"")));

    [Fact]
    public void ManifestRefusesB3ObjectExtension() =>
        ManifestFailure("M object field set", Mutate(ValidationFixtures.M3,
            ("\"destination\":\"/coordinator\",", "\"destination\":\"/coordinator\",\"extra\":1,")));

    [Fact]
    public void ManifestRefusesAggregateLengthOverflow() =>
        ManifestFailure("M aggregate length", Mutate(ValidationFixtures.M3,
            ("\"length\":1000", "\"length\":16777216"),
            ("\"length\":2000", "\"length\":16777216")));

    [Fact]
    public void PolicyRefusesB2RuleViolations()
    {
        PolicyFailure("F B2 release", Mutate(ValidationFixtures.F2,
            ("\"allowRelease\":false", "\"allowRelease\":true")));
        PolicyFailure("F B2 sandbox", Mutate(ValidationFixtures.F2,
            ("\"sandbox\":null", "\"sandbox\":{}")));
        PolicyFailure("F B2 toolEnrollmentId", Mutate(ValidationFixtures.F2,
            ("\"toolEnrollmentId\":null", "\"toolEnrollmentId\":\"tool-1\"")));
    }

    [Fact]
    public void PolicyRefusesSchemaViolations()
    {
        PolicyFailure("F policy kind", Mutate(ValidationFixtures.F2,
            ("\"policyKind\":\"b2-lifeline-v1\"", "\"policyKind\":\"b2-lifeline-v2\"")));
        PolicyFailure("F bootId", Mutate(ValidationFixtures.F2,
            ("\"bootId\":\"00000000-0000-4000-8000-000000000000\"",
                "\"bootId\":\"00000000-0000-4000-8000-00000000000A\"")));
        PolicyFailure("uid integer", Mutate(ValidationFixtures.F2, ("\"uid\":1000", "\"uid\":0")));
        PolicyFailure("F B2 topology", Mutate(ValidationFixtures.F3,
            ("\"policyKind\":\"b3-static-fixture-v1\"", "\"policyKind\":\"b2-lifeline-v1\"")));
    }

    [Fact]
    public void PolicyRefusesHostPathViolations()
    {
        PolicyFailure("F hostPaths stagingParent mode", Mutate(ValidationFixtures.F3,
            ("\"mode\":\"0700\",\"path\":\"/var/lib/gagamba/staging\"",
                "\"mode\":\"0755\",\"path\":\"/var/lib/gagamba/staging\"")));
        PolicyFailure("F hostPaths field set", Mutate(ValidationFixtures.F3,
            ("\"journalParent\":", "\"journalParents\":")));
        PolicyFailure("F hostPaths delegatedParent uid", Mutate(ValidationFixtures.F3,
            ("\"path\":\"/sys/fs/cgroup\",\"uid\":1000", "\"path\":\"/sys/fs/cgroup\",\"uid\":1001")));
    }

    [Fact]
    public void PolicyRefusesFixedObjectViolations()
    {
        PolicyFailure("F copy hooks", Mutate(ValidationFixtures.F3,
            ("\"hooks\":false", "\"hooks\":true")));
        PolicyFailure("F invocation argv length", Mutate(ValidationFixtures.F3,
            ("\"argv\":[\"--in\",\"/input\"]", "\"argv\":[\"/input\"]")));
        PolicyFailure("F sandbox networkQualification", Mutate(ValidationFixtures.F3,
            ("\"networkQualification\":\"PendingQualification\"",
                "\"networkQualification\":\"Qualified\"")));
        PolicyFailure("F sandbox store 0 path", Mutate(ValidationFixtures.F3,
            ("\"path\":\"/dev/shm\"", "\"path\":\"/tmp\"")));
        PolicyFailure("memoryBytes integer", Mutate(ValidationFixtures.F3,
            ("\"memoryBytes\":134217728", "\"memoryBytes\":134217727")));
        PolicyFailure("F resources corePatternObserved", Mutate(ValidationFixtures.F3,
            ("\"corePatternObserved\":null", "\"corePatternObserved\":\"none\"")));
        PolicyFailure("F resources expiresLeadNs session bound", Mutate(ValidationFixtures.F3,
            ("\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":999")));
        PolicyFailure("F toolEnrollmentId", Mutate(ValidationFixtures.F3,
            ("\"toolEnrollmentId\":\"tool-1\"", "\"toolEnrollmentId\":\"tool/1\"")));
    }

    [Fact]
    public void ScriptRefusesTemplateAndActionViolations()
    {
        ScriptFailure("S action template", Mutate(ValidationFixtures.S2,
            ("\"op\":\"connect-b2\"", "\"op\":\"prepare-b3\"")));
        ScriptFailure("index integer", Mutate(ValidationFixtures.S2,
            ("{\"index\":1,\"op\":\"inspect\"", "{\"index\":2,\"op\":\"inspect\"")));
        ScriptFailure("S action op", Mutate(ValidationFixtures.S2,
            ("\"op\":\"inspect\"", "\"op\":\"snapshot\"")));
        ScriptFailure("S action field set", Mutate(ValidationFixtures.S2,
            ("{\"index\":0,\"op\":\"connect-b2\"", "{\"extra\":1,\"index\":0,\"op\":\"connect-b2\"")));
        ScriptFailure("timeoutNs integer", Mutate(ValidationFixtures.S2,
            ("\"timeoutNs\":1000000000", "\"timeoutNs\":0")));
    }

    [Fact]
    public void CatalogRefusesFieldSetViolations()
    {
        CatalogFailure("K field set", Mutate(ValidationFixtures.K2,
            ("\"profileVersion\":\"1.0\",", string.Empty)));
        CatalogFailure("K field set", Mutate(ValidationFixtures.K2,
            ("\"caseId\":\"case-2\",", "\"caseId\":\"case-2\",\"catalogReviewSha256\":\""
                + new string('c', 64) + "\",")));
        CatalogFailure("K field set", Mutate(ValidationFixtures.K2,
            ("\"allowRelease\":false,", "\"allowRelease\":false,\"approved\":true,")));
    }

    [Fact]
    public void CatalogRefusesValueViolations()
    {
        CatalogFailure("K pre qualificationId", Mutate(ValidationFixtures.K2,
            ("\"qualificationId\":null", "\"qualificationId\":\"Q1\"")));
        CatalogFailure("qualificationId string", Mutate(ValidationFixtures.K3,
            ("\"qualificationId\":\"Q1\"", "\"qualificationId\":null")));
        CatalogFailure("K caseId", Mutate(ValidationFixtures.K2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"case/2\"")));
        CatalogFailure("sourceCommit hex", Mutate(ValidationFixtures.K2,
            ("\"sourceCommit\":\"0000000000000000000000000000000000000000\"",
                "\"sourceCommit\":\"000000000000000000000000000000000000000A\"")));
        CatalogFailure("schemaVersion integer", Mutate(ValidationFixtures.K2,
            ("\"schemaVersion\":1", "\"schemaVersion\":2")));
        CatalogFailure("K phase", Mutate(ValidationFixtures.K2,
            ("\"phase\":\"pre\"", "\"phase\":\"pending\"")));
        CatalogFailure("maximumSessionNs integer", Mutate(ValidationFixtures.K2,
            ("\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":600000000001")));
        CatalogFailure("allowRelease boolean", Mutate(ValidationFixtures.K2,
            ("\"allowRelease\":false", "\"allowRelease\":\"false\"")));
    }

    [Fact]
    public void ChainRefusesBindingViolations()
    {
        var dead = new string('d', 64);
        Assert.Equal("F fixtureManifestSha256 does not bind M", ChainFailure(
            Bytes(ValidationFixtures.M2),
            Mutate(ValidationFixtures.F2, ($"\"{ValidationFixtures.Hm2}\"", $"\"{dead}\"")),
            Bytes(ValidationFixtures.S2), Bytes(ValidationFixtures.K2)));
        Assert.Equal("S fixtureManifestSha256 does not bind M", ChainFailure(
            Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Mutate(ValidationFixtures.S2, ($"\"{ValidationFixtures.Hm2}\"", $"\"{dead}\"")),
            Bytes(ValidationFixtures.K2)));
        Assert.Equal("S fixturePolicySha256 does not bind F", ChainFailure(
            Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Mutate(ValidationFixtures.S2, ($"\"{ValidationFixtures.Hf2}\"", $"\"{dead}\"")),
            Bytes(ValidationFixtures.K2)));
        Assert.Equal("K fixturePolicySha256 does not bind F", ChainFailure(
            Bytes(ValidationFixtures.M3), Bytes(ValidationFixtures.F3), Bytes(ValidationFixtures.S3),
            Mutate(ValidationFixtures.K3, ($"\"{ValidationFixtures.Hf3}\"", $"\"{dead}\""))));
    }

    [Fact]
    public void ChainRefusesCrossArtifactDisagreements()
    {
        var caseId = Rebuild3(ValidationFixtures.M3, ValidationFixtures.F3,
            Edit(ValidationFixtures.S3, ("\"caseId\":\"case-3\"", "\"caseId\":\"case-4\"")),
            ValidationFixtures.K3);
        Assert.Equal("case id disagreement", ChainFailure(caseId.M, caseId.F, caseId.S, caseId.K));

        var phase = Rebuild3(ValidationFixtures.M3,
            Edit(ValidationFixtures.F3, ("\"phase\":\"post\"", "\"phase\":\"pre\"")),
            ValidationFixtures.S3, ValidationFixtures.K3);
        Assert.Equal("phase disagreement", ChainFailure(phase.M, phase.F, phase.S, phase.K));

        var topology = Rebuild3(ValidationFixtures.M3, ValidationFixtures.F3, ValidationFixtures.S3,
            Edit(ValidationFixtures.K3,
                ("\"topology\":\"b3-integrated\"", "\"topology\":\"b2-lifeline\"")));
        Assert.Equal("topology disagreement",
            ChainFailure(topology.M, topology.F, topology.S, topology.K));

        var host = Rebuild3(ValidationFixtures.M3,
            Edit(ValidationFixtures.F3,
                ($"\"{new string('a', 64)}\"", $"\"{new string('b', 64)}\"")),
            ValidationFixtures.S3, ValidationFixtures.K3);
        Assert.Equal("host id disagreement", ChainFailure(host.M, host.F, host.S, host.K));

        var release = Rebuild3(ValidationFixtures.M3, ValidationFixtures.F3, ValidationFixtures.S3,
            Edit(ValidationFixtures.K3, ("\"allowRelease\":true", "\"allowRelease\":false")));
        Assert.Equal("allow release disagreement",
            ChainFailure(release.M, release.F, release.S, release.K));

        var template = Rebuild3(ValidationFixtures.M3,
            Edit(ValidationFixtures.F3, ("\"allowRelease\":true", "\"allowRelease\":false")),
            ValidationFixtures.S3,
            Edit(ValidationFixtures.K3, ("\"allowRelease\":true", "\"allowRelease\":false")));
        Assert.Equal("release template disagreement",
            ChainFailure(template.M, template.F, template.S, template.K));

        var session = Rebuild3(ValidationFixtures.M3,
            Edit(ValidationFixtures.F3,
                ("\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":500000000000")),
            ValidationFixtures.S3, ValidationFixtures.K3);
        Assert.Equal("session limit disagreement",
            ChainFailure(session.M, session.F, session.S, session.K));

        var timeout = Rebuild3(ValidationFixtures.M3,
            Edit(ValidationFixtures.F3,
                ("\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":1000000000")),
            Edit(ValidationFixtures.S3, ("\"timeoutNs\":1000000000", "\"timeoutNs\":2000000000")),
            Edit(ValidationFixtures.K3,
                ("\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":1000000000")));
        Assert.Equal("S action timeout exceeds the F maximum session",
            ChainFailure(timeout.M, timeout.F, timeout.S, timeout.K));

        Assert.Equal("K fixtureManifestSha256 does not bind M", ChainFailure(
            Bytes(ValidationFixtures.M3), Bytes(ValidationFixtures.F3),
            Bytes(ValidationFixtures.S3), Bytes(ValidationFixtures.K2)));
    }

    [Fact]
    public void ChainRefusesByteLevelViolations()
    {
        Assert.Equal("noncanonical CJSON-1 representation", ChainFailure(
            Bytes(ValidationFixtures.M2 + " "), Bytes(ValidationFixtures.F2),
            Bytes(ValidationFixtures.S2), Bytes(ValidationFixtures.K2)));
        var truncated = Bytes(ValidationFixtures.K2);
        Assert.Equal("invalid CJSON-1", ChainFailure(
            Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Bytes(ValidationFixtures.S2), truncated[..^1]));
    }
}
