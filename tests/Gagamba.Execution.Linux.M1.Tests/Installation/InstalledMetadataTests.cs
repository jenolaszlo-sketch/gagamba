using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Installed;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Installation;

public sealed class InstalledMetadataTests
{
    private const string GoldenDigest = "8f9527205a66941dba2da98ee473886315a418a201a3f326fd60193e539054b0";
    internal static byte[] Vector()
    {
        using var s = typeof(InstalledMetadataTests).Assembly.GetManifestResourceStream("B6.ManifestV1")!;
        using var bytes = new MemoryStream();
        s.CopyTo(bytes);
        return bytes.ToArray();
    }

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    [Fact]
    public void CanonicalVectorMatchesIndependentSha256()
    {
        var bytes = Vector();
        Assert.Equal(GoldenDigest,Convert.ToHexStringLower(SHA256.HashData(bytes)));
        var m = InstalledManifest.Parse(bytes);
        Assert.Equal(GoldenDigest,m.Digest);
        Assert.Equal(14,m.Artifacts.Count);
        var root = JsonNode.Parse(bytes)!;
        root["source"]!["commit"] = new string('c',40);
        Assert.NotEqual(GoldenDigest,InstalledManifest.Parse(Bytes(root)).Digest);
    }

    [Theory]
    [InlineData("whitespace")][InlineData("duplicate-field")][InlineData("unknown-field")][InlineData("self-digest")]
    [InlineData("reordered")][InlineData("duplicate-path")][InlineData("duplicate-role")][InlineData("unknown-role")]
    [InlineData("wrong-schema")][InlineData("string-number")][InlineData("numeric-alias")][InlineData("unknown-kind")]
    [InlineData("bad-digest")][InlineData("empty")][InlineData("missing")][InlineData("extra")]
    [InlineData("negative-length")][InlineData("oversize")][InlineData("mode")][InlineData("uid")][InlineData("gid")]
    [InlineData("nlink")][InlineData("rid")][InlineData("protocol")][InlineData("policy")][InlineData("instrumented")]
    public void RefusesMalformedManifest(string mutation)
    {
        var bytes = Vector();
        Assert.Equal(GoldenDigest,InstalledManifest.Parse(bytes).Digest);
        var root = JsonNode.Parse(bytes)!;
        var a = root["artifacts"]!.AsArray();
        var item = a[0]!;
        var original = Encoding.UTF8.GetString(bytes);
        switch (mutation)
        {
            case "whitespace": bytes = Encoding.UTF8.GetBytes(original+"\n"); break;
            case "duplicate-field": bytes = Encoding.UTF8.GetBytes(original.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1")); break;
            case "numeric-alias": bytes = Encoding.UTF8.GetBytes(original.Replace("\"uid\":0","\"uid\":-0")); break;
            case "unknown-field": root["foo"] = true; break;
            case "self-digest": root["artifactSetSha256"] = GoldenDigest; break;
            case "reordered": a.RemoveAt(0); a.Add(item); break;
            case "duplicate-path": a[1]!["path"] = item["path"]!.GetValue<string>(); break;
            case "duplicate-role": a[1]!["role"] = item["role"]!.GetValue<string>(); break;
            case "unknown-role": item["role"] = "fixture"; break;
            case "wrong-schema": root["schemaVersion"] = 2; break;
            case "string-number": root["schemaVersion"] = "1"; break;
            case "unknown-kind": item["kind"] = "script"; break;
            case "bad-digest": item["sha256"] = "abc"; break;
            case "empty": a.Clear(); break;
            case "missing": a.RemoveAt(0); break;
            case "extra": a.Add(item.DeepClone()); break;
            case "negative-length": item["length"] = -1; break;
            case "oversize": item["length"] = long.MaxValue; break;
            case "mode": item["mode"] = "4755"; break;
            case "uid": item["uid"] = 1000; break;
            case "gid": item["gid"] = 1000; break;
            case "nlink": item["nlink"] = 2; break;
            case "rid": root["platform"]!["rid"] = "linux-arm64"; break;
            case "protocol": root["protocol"]!["bootstrap"] = 2; break;
            case "policy": root["policy"]!["authoritySha256"] = new string('f',64); break;
            case "instrumented": item["buildIdentity"] = "Test"; break;
            default: throw new ArgumentException(mutation);
        }
        if (mutation is not ("whitespace" or "duplicate-field" or "numeric-alias")) bytes = Bytes(root);
        Assert.Throws<InvalidDataException>(() => InstalledManifest.Parse(bytes));
    }

    [Theory]
    [InlineData("/native/exec-coordinator")][InlineData("native//exec-coordinator")][InlineData("native/../exec-coordinator")]
    [InlineData("native/.")][InlineData("native/exec-coordinator\0")][InlineData("native\\exec-coordinator")]
    [InlineData("native/exec-coordinatоr")][InlineData("owner/Gagamba.Execution.Linux.M1.TestFixture.dll")]
    public void PathIsExact(string invalid)
    {
        var root = JsonNode.Parse(Vector())!;
        Assert.Equal(GoldenDigest,InstalledManifest.Parse(Bytes(root)).Digest);
        root["artifacts"]![0]!["path"] = invalid;
        Assert.Throws<InvalidDataException>(() => InstalledManifest.Parse(Bytes(root)));
    }

    internal static JsonObject Record(InstalledManifest m) => new()
    {
        ["qualificationId"]="Q1", ["artifactSetSha256"]=m.Digest, ["profileId"]=InstalledManifest.Profile,
        ["profileVersion"]="1", ["rid"]="linux-x64", ["scope"]=InstalledManifest.Scope, ["host"]="fixture-wsl",
        ["sourceCommit"]=m.SourceCommit, ["sourceTree"]=m.SourceTree, ["evidenceSha256"]=new string('a',64),
        ["reviewCommit"]=new string('b',40), ["acceptedAtUtc"]="2026-11-02T00:00:00Z",
        ["limitations"]=new JsonArray("portable test only"), ["buildConfiguration"]="Production"
    };

    internal static JsonObject Receipt(InstalledManifest m,byte[]? record) => new()
    {
        ["schemaVersion"]=1, ["artifactSetSha256"]=m.Digest, ["manifestFileSha256"]=m.Digest,
        ["installedByUid"]=0, ["installedAtUtc"]="2026-11-02T00:00:00Z", ["candidatePath"]="/fixture",
        ["installerVersion"]="1", ["expectedDigestSource"]=record == null ? "unverified-candidate" : "qualification-record",
        ["qualificationId"]=record == null ? null : "Q1", ["qualificationRecordSha256"]=record == null ? null : InstallJson.Hash(record)
    };

    [Theory]
    [InlineData("fresh")][InlineData("missing")][InlineData("wrong-hash")][InlineData("multiple")][InlineData("stale")]
    [InlineData("other-digest")][InlineData("rid")][InlineData("profile")][InlineData("producer")]
    [InlineData("configuration")][InlineData("conflict")][InlineData("source")][InlineData("receipt-digest")]
    [InlineData("record-id")][InlineData("ambiguous-receipt")]
    public void T20cUsesOnlyReceiptSelectedAcceptedBytes(string mutation)
    {
        // The dictionary represents only the root-controlled record namespace. Producer files live outside it.
        var m = InstalledManifest.Parse(Vector());
        var record = Record(m);
        var bytes = Bytes(record);
        var receipt = Receipt(m,bytes);
        var trusted = new Dictionary<string,byte[]>(StringComparer.Ordinal){["Q1.json"]=bytes};
        bool Associated()
        {
            try
            {
                var r = InstalledMetadata.Receipt(Bytes(receipt),m);
                if (!trusted.TryGetValue(InstalledMetadata.SelectedRecordName(r),out var selected)) return false;
                InstalledMetadata.AcceptRecord(selected,r,m);
                return true;
            }
            catch (InvalidDataException) { return false; }
        }
        Assert.True(Associated());
        switch (mutation)
        {
            case "fresh": receipt=Receipt(m,null); break;
            case "missing": trusted.Remove("Q1.json"); break;
            case "wrong-hash": receipt["qualificationRecordSha256"]=new string('d',64); break;
            case "multiple": trusted["Q2.json"]=[0]; break;
            case "stale": trusted["Q2.json"]=bytes; record["scope"]="old"; break;
            case "other-digest": record["artifactSetSha256"]=new string('d',64); break;
            case "rid": record["rid"]="linux-arm64"; break;
            case "profile": record["profileId"]="other"; break;
            case "producer": trusted.Clear(); break;
            case "configuration": record["buildConfiguration"]="Test"; break;
            case "conflict": record["Qualified"]=true; break;
            case "source": record["sourceTree"]=new string('e',40); break;
            case "receipt-digest": receipt["manifestFileSha256"]=new string('e',64); break;
            case "record-id": record["qualificationId"]="q1"; break;
            case "ambiguous-receipt": receipt["expectedDigestSource"]="unverified-candidate"; break;
        }
        var changed = Bytes(record);
        if (!changed.SequenceEqual(bytes))
        {
            trusted["Q1.json"]=changed;
            receipt["qualificationRecordSha256"]=InstallJson.Hash(changed);
        }
        Assert.Equal(mutation == "multiple",Associated());
    }

    [Theory]
    [InlineData("")][InlineData(".")][InlineData("..")][InlineData(".leading")][InlineData("trailing.")]
    [InlineData("Q/1")][InlineData("Q\\1")][InlineData("a b")][InlineData("Ｑ1")][InlineData("Q1\0")]
    public void ValidateQualificationComponentBeforeLookup(string id)
    {
        Assert.True(InstalledMetadata.ValidId(new string('a',128)));
        Assert.True(InstalledMetadata.ValidId("A-1_b.2"));
        Assert.False(InstalledMetadata.ValidId(new string('a',129)));
        Assert.False(InstalledMetadata.ValidId(id));
        Assert.Throws<InvalidDataException>(() => InstalledMetadata.SelectedRecordName(new(id,GoldenDigest)));
    }

    [Fact]
    public void ActiveHasOnlyOneCanonicalEncoding()
    {
        Assert.Equal(GoldenDigest,InstalledMetadata.Active(Encoding.ASCII.GetBytes(GoldenDigest+"\n")));
        foreach (var value in new[]{GoldenDigest,GoldenDigest+"\r\n",GoldenDigest.ToUpperInvariant()+"\n",new string('0',64)+"\n"})
            Assert.Throws<InvalidDataException>(() => InstalledMetadata.Active(Encoding.ASCII.GetBytes(value)));
    }

    [Fact]
    public void RelabelingInstrumentedAssemblyCannotMakeItProduction()
    {
        var bytes = File.ReadAllBytes(typeof(InstalledManifest).Assembly.Location);
        var artifact = new InstalledArtifact("owner-m1","managed","owner/Gagamba.Execution.Linux.M1.dll",InstallJson.Hash(bytes),bytes.Length,"0444");
        Assert.Throws<InvalidDataException>(() => InstalledPayload.Check(artifact,bytes));
    }
}
