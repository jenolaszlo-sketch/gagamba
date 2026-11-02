// Independent S2-Q1 reviewer probes. NOT part of the reviewed candidate; injected only into
// the isolated review copy to falsify the live implementation. Every negative probe first
// asserts a valid positive control (the golden vector parses to the golden digest).
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class ReviewerProbeTests
{
    private const string Golden = "8f9527205a66941dba2da98ee473886315a418a201a3f326fd60193e539054b0";
    private static byte[] Vec() => InstalledMetadataTests.Vector();
    private static string Text() => Encoding.UTF8.GetString(Vec());
    private static byte[] Raw(string s) => Encoding.UTF8.GetBytes(s);
    private static byte[] B(JsonNode n) => Encoding.UTF8.GetBytes(n.ToJsonString());

    private static void RejectManifest(byte[] mutated)
    {
        Assert.Equal(Golden, InstalledManifest.Parse(Vec()).Digest); // positive control
        Assert.Throws<InvalidDataException>(() => InstalledManifest.Parse(mutated));
    }

    private static JsonObject ReceiptFor(InstalledManifest m, byte[]? record) => new()
    {
        ["schemaVersion"]=1,["artifactSetSha256"]=m.Digest,["manifestFileSha256"]=m.Digest,["installedByUid"]=0,
        ["installedAtUtc"]="2026-11-02T00:00:00Z",["candidatePath"]="/fixture",["installerVersion"]="1",
        ["expectedDigestSource"]= record==null? "unverified-candidate":"qualification-record",
        ["qualificationId"]= record==null? null:"Q1",["qualificationRecordSha256"]= record==null? null: InstallJson.Hash(record)
    };

    private static JsonObject RecordFor(InstalledManifest m) => new()
    {
        ["qualificationId"]="Q1",["artifactSetSha256"]=m.Digest,["profileId"]=InstalledManifest.Profile,["profileVersion"]="1",
        ["rid"]="linux-x64",["scope"]=InstalledManifest.Scope,["host"]="fixture-wsl",["sourceCommit"]=m.SourceCommit,
        ["sourceTree"]=m.SourceTree,["evidenceSha256"]=new string('a',64),["reviewCommit"]=new string('b',40),
        ["acceptedAtUtc"]="2026-11-02T00:00:00Z",["limitations"]=new JsonArray("portable test only"),["buildConfiguration"]="Production"
    };

    // ---- canonical codec boundary probes (not in the producer's list) ----

    [Fact] public void P01_UppercaseArtifactHashRefused()
        => RejectManifest(Raw(Text().Replace(
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b",
            "5E55E92B4228013E88A9FB34AC4674C5B9A485E831DB7FF6D86F02DF7C68C73B")));

    [Fact] public void P02_AllZeroArtifactHashRefused()
        => RejectManifest(Raw(Text().Replace(
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b", new string('0',64))));

    [Fact] public void P03_TruncatedHashRefused()
        => RejectManifest(Raw(Text().Replace(
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b",
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73")));

    [Fact] public void P04_OverlongHashRefused()
        => RejectManifest(Raw(Text().Replace(
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b",
            "5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b0")));

    [Fact] public void P05_NestedDuplicatePropertyRefused()
        => RejectManifest(Raw(Text().Replace("\"source\":{\"commit\":\"",
            "\"source\":{\"commit\":\"" + new string('c',40) + "\",\"commit\":\"")));

    [Fact] public void P06_ArtifactFieldReorderRefused()
        => RejectManifest(Raw(Text().Replace("{\"role\":\"coordinator\",\"kind\":\"native\"",
            "{\"kind\":\"native\",\"role\":\"coordinator\"")));

    [Fact] public void P07_ModeWithoutLeadingZeroRefused()
        => RejectManifest(Raw(Text().Replace("\"mode\":\"0555\"","\"mode\":\"555\"")));

    [Fact] public void P08_NoncanonicalDecimalLengthRefused()
        => RejectManifest(Raw(Text().Replace("\"length\":878792","\"length\":878792.0")));

    [Fact] public void P09_StringUidRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["artifacts"]![0]!["uid"] = "0";
        RejectManifest(B(root));
    }

    [Fact] public void P10_ExtraToolchainFieldRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["source"]!["toolchain"]!["extra"] = "x";
        RejectManifest(B(root));
    }

    [Fact] public void P11_RequiredFalseRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["artifacts"]![0]!["required"] = false;
        RejectManifest(B(root));
    }

    [Fact] public void P12_SurvivesControllerMismatchRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["artifacts"]![0]!["survivesController"] = true; // coordinator must be false
        RejectManifest(B(root));
    }

    [Fact] public void P13_ConsumerMismatchRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["artifacts"]![0]!["consumer"] = "owner";
        RejectManifest(B(root));
    }

    [Fact] public void P14_ProfileVersionMismatchRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["profileVersion"] = "2";
        RejectManifest(B(root));
    }

    [Fact] public void P15_OwnerProtocolMismatchRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["protocol"]!["ownerProtocol"] = 2;
        RejectManifest(B(root));
    }

    [Fact] public void P16_PolicyProfileMismatchRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        root["policy"]!["profileId"] = "other";
        RejectManifest(B(root));
    }

    [Fact] public void P17_ReorderedFeaturesRefused()
    {
        var root = JsonNode.Parse(Vec())!;
        var features = root["platform"]!["requiredFeatures"]!.AsArray();
        var first = features[0]!.GetValue<string>();
        features[0] = features[1]!.GetValue<string>();
        features[1] = first;
        RejectManifest(B(root));
    }

    // ---- receipt / record association probes ----

    [Fact] public void P18_ValidTripleAcceptsAssociation_PositiveControl()
    {
        var m = InstalledManifest.Parse(Vec());
        var record = RecordFor(m);
        var rb = B(record);
        var receipt = InstalledMetadata.Receipt(B(ReceiptFor(m, rb)), m);
        Assert.Equal("Q1", receipt.QualificationId);
        // Pure association accepts the self-consistent triple. It returns void: no identity.
        InstalledMetadata.AcceptRecord(rb, receipt, m);
    }

    [Fact] public void P19_ReceiptInstalledByNonZeroRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var receipt = ReceiptFor(m, null);
        receipt["installedByUid"] = 1000;
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.Receipt(B(receipt), m));
    }

    [Fact] public void P20_ReceiptUnknownDigestSourceRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var receipt = ReceiptFor(m, null);
        receipt["expectedDigestSource"] = "root-says-trusted";
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.Receipt(B(receipt), m));
    }

    [Fact] public void P21_ReceiptManifestFileHashMismatchRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var receipt = ReceiptFor(m, null);
        receipt["manifestFileSha256"] = new string('e',64);
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.Receipt(B(receipt), m));
    }

    [Fact] public void P22_ReceiptQualificationIdWithSlashRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var record = RecordFor(m);
        var receipt = ReceiptFor(m, B(record));
        receipt["qualificationId"] = "a/b";
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.Receipt(B(receipt), m));
    }

    [Fact] public void P23_RecordBuildConfigurationDebugRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var record = RecordFor(m);
        record["buildConfiguration"] = "Debug";
        var rb = B(record);
        var receipt = InstalledMetadata.Receipt(B(ReceiptFor(m, rb)), m);
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.AcceptRecord(rb, receipt, m));
    }

    [Fact] public void P24_RecordTooManyLimitationsRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var record = RecordFor(m);
        var limits = new JsonArray();
        for (int i = 0; i < 33; i++) limits.Add("limitation " + i);
        record["limitations"] = limits;
        var rb = B(record);
        var receipt = InstalledMetadata.Receipt(B(ReceiptFor(m, rb)), m);
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.AcceptRecord(rb, receipt, m));
    }

    [Fact] public void P25_RecordExtraFieldRefused()
    {
        var m = InstalledManifest.Parse(Vec());
        var record = RecordFor(m);
        record["accepted"] = true; // a bare self-asserted acceptance flag must not help
        var rb = B(record);
        var receipt = InstalledMetadata.Receipt(B(ReceiptFor(m, rb)), m);
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.AcceptRecord(rb, receipt, m));
    }

    [Fact] public void P26_ActiveCanonicalBoundary()
    {
        Assert.Equal("8f9527205a66941dba2da98ee473886315a418a201a3f326fd60193e539054b0",
            InstalledMetadata.Active(Encoding.ASCII.GetBytes(Golden + "\n")));
        foreach (var bad in new[]{ Golden, Golden + "\n\n", Golden + "\nx", Golden + "\r\n",
            Golden.ToUpperInvariant() + "\n", new string('0',64) + "\n" })
            Assert.Throws<InvalidDataException>(() => InstalledMetadata.Active(Encoding.ASCII.GetBytes(bad)));
    }

    [Fact] public void P27_QualificationIdGrammarBoundary()
    {
        Assert.True(InstalledMetadata.ValidId(new string('a',128)));
        Assert.False(InstalledMetadata.ValidId(new string('a',129)));
        foreach (var bad in new[]{ "", ".", "..", ".x", "x.", "a/b", "a\\b", "a b", "a\tb" })
            Assert.False(InstalledMetadata.ValidId(bad));
    }

    // ---- trust separation: only fixed-root inspection may report qualification ----

    [Fact] public void P28_FixedRootInspectionIsTheOnlyQualifiedSource()
    {
        string report = InstalledArtifactInspection.InspectActive();
        Assert.False(report.StartsWith("Qualified ", StringComparison.Ordinal),
            "reviewer host must not report a qualified installation: " + report);
    }
}
