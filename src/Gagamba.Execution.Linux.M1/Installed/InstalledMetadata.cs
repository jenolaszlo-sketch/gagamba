using System.Globalization;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Installed;

internal sealed record InstalledReceipt(string? QualificationId, string? QualificationRecordSha256);

// Only VerifyActive supplies a trusted record. Metadata comparison by itself is never an installation decision.
internal static class InstalledMetadata
{
    internal static string Active(byte[] bytes)
    {
        InstallJson.Require(bytes.Length == 65 && bytes[^1] == (byte)'\n', "invalid active pointer");
        var digest = System.Text.Encoding.ASCII.GetString(bytes,0,64);
        InstallJson.Require(InstallJson.IsHex(digest,64), "invalid active digest");
        return digest;
    }

    internal static bool ValidId(string s) => s.Length is >= 1 and <= 128 && s[0] != '.' && s[^1] != '.' &&
        s.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-');

    internal static InstalledReceipt Receipt(byte[] bytes, InstalledManifest manifest)
    {
        var r = InstallJson.Read(bytes);
        InstallJson.Keys(r,"schemaVersion artifactSetSha256 manifestFileSha256 installedByUid installedAtUtc candidatePath installerVersion expectedDigestSource qualificationId qualificationRecordSha256",false);
        InstallJson.Require(InstallJson.Int(r,"schemaVersion") == 1 && InstallJson.Int(r,"installedByUid") == 0 &&
            InstallJson.Hex(r,"artifactSetSha256") == manifest.Digest && InstallJson.Hex(r,"manifestFileSha256") == manifest.Digest, "receipt anchor");
        Utc(r,"installedAtUtc");
        InstallJson.String(r,"candidatePath",4096);
        InstallJson.String(r,"installerVersion");
        string source = InstallJson.String(r,"expectedDigestSource");
        if (source == "unverified-candidate")
        {
            InstallJson.Require(r.GetProperty("qualificationId").ValueKind == JsonValueKind.Null &&
                r.GetProperty("qualificationRecordSha256").ValueKind == JsonValueKind.Null, "conflicting fresh receipt");
            return new(null,null);
        }
        InstallJson.Require(source == "qualification-record", "unrecognized receipt digest source");
        var id = InstallJson.String(r,"qualificationId",128);
        InstallJson.Require(ValidId(id), "invalid qualification identifier");
        return new(id,InstallJson.Hex(r,"qualificationRecordSha256"));
    }

    internal static string SelectedRecordName(InstalledReceipt receipt)
    {
        if (receipt.QualificationId is not { } id || !ValidId(id))
            throw new InvalidDataException("no selectable qualification record");
        return id + ".json";
    }

    internal static void AcceptRecord(byte[] bytes, InstalledReceipt receipt, InstalledManifest manifest)
    {
        InstallJson.Require(receipt.QualificationId != null && InstallJson.Hash(bytes) == receipt.QualificationRecordSha256,
            "qualification record hash");
        var r = InstallJson.Read(bytes);
        InstallJson.Keys(r,"qualificationId artifactSetSha256 profileId profileVersion rid scope host sourceCommit sourceTree evidenceSha256 reviewCommit acceptedAtUtc limitations buildConfiguration",false);
        InstallJson.Require(InstallJson.String(r,"qualificationId") == receipt.QualificationId &&
            InstallJson.Hex(r,"artifactSetSha256") == manifest.Digest && InstallJson.String(r,"profileId") == InstalledManifest.Profile &&
            InstallJson.String(r,"profileVersion") == InstalledManifest.Version && InstallJson.String(r,"rid") == InstalledManifest.Rid &&
            InstallJson.String(r,"scope") == InstalledManifest.Scope && InstallJson.String(r,"buildConfiguration") == "Production" &&
            InstallJson.Hex(r,"sourceCommit",40) == manifest.SourceCommit && InstallJson.Hex(r,"sourceTree",40) == manifest.SourceTree,
            "qualification record binding");
        InstallJson.String(r,"host");
        InstallJson.Hex(r,"evidenceSha256");
        InstallJson.Hex(r,"reviewCommit",40);
        Utc(r,"acceptedAtUtc");
        var limits = r.GetProperty("limitations");
        InstallJson.Require(limits.ValueKind == JsonValueKind.Array && limits.GetArrayLength() <= 32 &&
            limits.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String && e.GetString() is {Length: > 0 and <= 512} s &&
                s.All(c => c is >= ' ' and <= '~')), "malformed qualification limitations");
    }

    private static void Utc(JsonElement obj,string key)
    {
        string time = InstallJson.String(obj,key);
        InstallJson.Require(DateTimeOffset.TryParseExact(time,["yyyy-MM-dd'T'HH:mm:ss'Z'","yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"],
            CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,out _),key);
    }
}

internal enum InstalledState { Unavailable, Unqualified, InstalledUnqualified, Qualified }

internal sealed class InstalledResult
{
    private InstalledResult(InstalledState state, string reason, string? digest = null, string? qualificationId = null,
        string? qualificationRecordSha256 = null, InstalledManifest? manifest = null)
        => (State, Reason, Digest, QualificationId, QualificationRecordSha256, Manifest, Identity) =
            (state, reason, digest, qualificationId, qualificationRecordSha256, manifest,
                manifest is null ? null : InstallationIdentity.Project(manifest));

    internal InstalledManifest? Manifest { get; }
    internal InstallationIdentity? Identity { get; }
    internal InstalledState State { get; }
    internal string Reason { get; }
    internal string? Digest { get; }
    internal string? QualificationId { get; }
    // The raw receipt-selected record hash is retained so final admission can bind
    // to the exact S2 approval rather than only its identifier.
    internal string? QualificationRecordSha256 { get; }
    internal static InstalledResult Unavailable(string reason) => new(InstalledState.Unavailable,reason);
    internal static InstalledResult Unqualified(string reason) => new(InstalledState.Unqualified,reason);
    internal static InstalledResult Fresh(InstalledManifest manifest) => new(InstalledState.InstalledUnqualified,"no selected acceptance",manifest.Digest);
    internal static InstalledResult Qualified(InstalledManifest manifest,string id,string recordSha256) =>
        new(InstalledState.Qualified,"accepted installed record",manifest.Digest,id,recordSha256,manifest);
}
