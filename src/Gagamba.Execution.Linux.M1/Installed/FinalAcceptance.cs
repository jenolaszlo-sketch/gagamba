using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Installed;

/// <summary>Exact final-admission record, schema 1. SchemaVersion is retained for
/// audit even though the codec accepts only 1.</summary>
internal sealed record FinalAcceptanceRecord(
    int SchemaVersion,
    string Acceptance,
    string AcceptedAtUtc,
    string ArtifactSetSha256,
    string BuildConfiguration,
    string CandidateEvidenceSha256,
    string HostId,
    string IndependentReviewSha256,
    IReadOnlyList<string> Limitations,
    string NativeGateResultsSha256,
    string OwnerDecisionSha256,
    string PlatformInventorySha256,
    string PostpublicationEvidenceSha256,
    string PrepublicationEvidenceSha256,
    string ProfileId,
    string ProfileVersion,
    string QualificationId,
    string QualificationPlanSha256,
    string QualificationRecordSha256,
    string ReviewCommit,
    string Rid,
    string Scope,
    string SourceCommit,
    string SourceTree);

/// <summary>Protected runtime selector / administrative approval, schema 1. The
/// selector has exactly the seven approval fields; runtime never reads the
/// administrative /etc store.</summary>
internal sealed record FinalAdmissionSelector(
    bool Accepted,
    string ArtifactSetSha256,
    string FinalRecordSha256,
    string OwnerDecisionSha256,
    string QualificationId,
    string QualificationRecordSha256,
    int SchemaVersion);

/// <summary>Values supplied by one retained successful S2 selection. The final
/// record must bind to these exact values; none may come from the caller.</summary>
internal sealed record FinalAdmissionExpectation(
    string ArtifactSetSha256,
    string QualificationId,
    string QualificationRecordSha256,
    string ProfileId,
    string ProfileVersion,
    string Rid,
    string Scope,
    string PlatformInventorySha256,
    string SourceCommit,
    string SourceTree)
{
    internal static FinalAdmissionExpectation FromSelection(InstalledResult selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var manifest = selection.Manifest ?? throw new InvalidDataException("S2 manifest is absent");
        var qualificationId = selection.QualificationId ?? throw new InvalidDataException("S2 qualification id is absent");
        var qualificationRecordSha256 = selection.QualificationRecordSha256 ??
            throw new InvalidDataException("S2 qualification record hash is absent");
        return new(manifest.Digest, qualificationId, qualificationRecordSha256,
            InstalledManifest.Profile, InstalledManifest.Version, InstalledManifest.Rid, InstalledManifest.Scope,
            InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"),
            manifest.SourceCommit, manifest.SourceTree);
    }
}

/// <summary>A pure, non-authoritative result of exact selector/record parsing
/// and S2 binding. It cannot by itself grant admission; only the installed
/// verifier constructs the private grant type.</summary>
internal sealed record FinalAdmissionBinding(
    FinalAdmissionSelector Selector,
    FinalAcceptanceRecord Record,
    string FinalRecordSha256);

/// <summary>CJSON-1 and exact final-admission schemas. This codec performs no
/// filesystem access and accepts no caller-supplied expected hash.</summary>
internal static class FinalAcceptanceCodec
{
    internal const int MaxBytes = 65_536;
    private const int MaxText = 128;
    private const int MaxLimitations = 32;
    private const int MaxLimitationBytes = 512;

    // Both arrays are in ascending ASCII order, which is also the required CJSON-1 order.
    private static readonly string[] RecordFields =
    [
        "acceptance", "acceptedAtUtc", "artifactSetSha256", "buildConfiguration",
        "candidateEvidenceSha256", "hostId", "independentReviewSha256", "limitations",
        "nativeGateResultsSha256", "ownerDecisionSha256", "platformInventorySha256",
        "postpublicationEvidenceSha256", "prepublicationEvidenceSha256", "profileId",
        "profileVersion", "qualificationId", "qualificationPlanSha256",
        "qualificationRecordSha256", "reviewCommit", "rid", "schemaVersion", "scope",
        "sourceCommit", "sourceTree",
    ];

    private static readonly string[] SelectorFields =
    [
        "accepted", "artifactSetSha256", "finalRecordSha256", "ownerDecisionSha256",
        "qualificationId", "qualificationRecordSha256", "schemaVersion",
    ];

    internal static FinalAcceptanceRecord ParseRecord(byte[] bytes)
    {
        var root = ParseCanonical(bytes);
        Require(root.EnumerateObject().Select(p => p.Name).SequenceEqual(RecordFields),
            "final record field set");
        var schemaVersion = CheckedInt(root, "schemaVersion");
        Require(schemaVersion == 1, "final record schema version");
        var qualificationId = Text(root, "qualificationId", MaxText);
        Require(InstalledMetadata.ValidId(qualificationId), "final record qualification id");
        return new(
            schemaVersion,
            Exact(root, "acceptance", "final-native-accepted", 64),
            Utc(root, "acceptedAtUtc"),
            Hex(root, "artifactSetSha256"),
            Exact(root, "buildConfiguration", "Production", 64),
            Hex(root, "candidateEvidenceSha256"),
            Hex(root, "hostId"),
            Hex(root, "independentReviewSha256"),
            Limitations(root),
            Hex(root, "nativeGateResultsSha256"),
            Hex(root, "ownerDecisionSha256"),
            Hex(root, "platformInventorySha256"),
            Hex(root, "postpublicationEvidenceSha256"),
            Hex(root, "prepublicationEvidenceSha256"),
            Text(root, "profileId", MaxText),
            Text(root, "profileVersion", MaxText),
            qualificationId,
            Hex(root, "qualificationPlanSha256"),
            Hex(root, "qualificationRecordSha256"),
            Hex(root, "reviewCommit", 40),
            Text(root, "rid", MaxText),
            Text(root, "scope", MaxText),
            Hex(root, "sourceCommit", 40),
            Hex(root, "sourceTree", 40));
    }

    internal static FinalAdmissionSelector ParseSelector(byte[] bytes)
    {
        var root = ParseCanonical(bytes);
        Require(root.EnumerateObject().Select(p => p.Name).SequenceEqual(SelectorFields),
            "final selector field set");
        var schemaVersion = CheckedInt(root, "schemaVersion");
        Require(schemaVersion == 1, "final selector schema version");
        Require(root.GetProperty("accepted").ValueKind == JsonValueKind.True,
            "final selector acceptance");
        var qualificationId = Text(root, "qualificationId", MaxText);
        Require(InstalledMetadata.ValidId(qualificationId), "final selector qualification id");
        return new(
            true,
            Hex(root, "artifactSetSha256"),
            Hex(root, "finalRecordSha256"),
            Hex(root, "ownerDecisionSha256"),
            qualificationId,
            Hex(root, "qualificationRecordSha256"),
            schemaVersion);
    }

    internal static string ParseHostId(byte[] bytes)
    {
        Require(bytes.Length == 65 && bytes[^1] == (byte)'\n', "host identity encoding");
        var value = Encoding.ASCII.GetString(bytes, 0, 64);
        Require(IsLowerHex(value, 64), "host identity");
        return value;
    }

    internal static bool IsLowerHex(string value, int length) =>
        value.Length == length && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static JsonElement ParseCanonical(byte[] bytes)
    {
        Require(bytes.Length is > 0 and <= MaxBytes, "CJSON-1 size");
        foreach (var value in bytes)
            Require(value is >= 0x20 and <= 0x7e, "CJSON-1 ASCII printable bytes");
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("invalid CJSON-1", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            Require(root.ValueKind == JsonValueKind.Object, "CJSON-1 root object");
            ValidateCanonical(root, 0);
            using var output = new MemoryStream();
            using (var writer = new Utf8JsonWriter(output,
                       new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                root.WriteTo(writer);
            }
            Require(output.ToArray().AsSpan().SequenceEqual(bytes),
                "noncanonical CJSON-1 representation");
            return root.Clone();
        }
    }

    private static void ValidateCanonical(JsonElement value, int depth)
    {
        Require(depth <= 8, "CJSON-1 depth");
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                string? previous = null;
                foreach (var property in value.EnumerateObject())
                {
                    ValidateName(property.Name);
                    if (previous is not null)
                        Require(string.CompareOrdinal(previous, property.Name) < 0,
                            "CJSON-1 keys are not strictly ascending");
                    previous = property.Name;
                    ValidateCanonical(property.Value, depth + 1);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray()) ValidateCanonical(item, depth + 1);
                break;
            case JsonValueKind.String:
                var text = value.GetString()!;
                Require(text.Length > 0 && text.All(c => c is >= ' ' and <= '~'),
                    "CJSON-1 string");
                break;
            case JsonValueKind.Number:
                Require(value.TryGetInt64(out var number) && number >= 0 &&
                        value.GetRawText() == number.ToString(CultureInfo.InvariantCulture),
                    "CJSON-1 unsigned canonical integer");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                break;
            default:
                throw new InvalidDataException("CJSON-1 value kind is not permitted");
        }
    }

    private static void ValidateName(string name) =>
        Require(name.Length > 0 && name.All(c => c is >= ' ' and <= '~'),
            "CJSON-1 property name");

    private static string Text(JsonElement obj, string key, int maximum)
    {
        var value = obj.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, key);
        var text = value.GetString()!;
        Require(text.Length is > 0 && text.Length <= maximum, key);
        return text;
    }

    private static string Hex(JsonElement obj, string key, int length = 64)
    {
        var text = Text(obj, key, length);
        Require(IsLowerHex(text, length), key);
        return text;
    }

    private static string Exact(JsonElement obj, string key, string expected, int maximum)
    {
        var text = Text(obj, key, maximum);
        Require(text == expected, key);
        return text;
    }

    private static int CheckedInt(JsonElement obj, string key)
    {
        var value = obj.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.Number, key);
        var number = value.GetInt64();
        Require(number is >= 0 and <= int.MaxValue, key);
        return (int)number;
    }

    private static string Utc(JsonElement obj, string key)
    {
        var text = Text(obj, key, 20);
        Require(DateTime.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _), key);
        return text;
    }

    private static string[] Limitations(JsonElement obj)
    {
        var value = obj.GetProperty("limitations");
        Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= MaxLimitations,
            "limitations");
        var result = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            Require(item.ValueKind == JsonValueKind.String, "limitation");
            var text = item.GetString()!;
            Require(text.Length is > 0 and <= MaxLimitationBytes, "limitation");
            result.Add(text);
        }
        return result.ToArray();
    }

    internal static void Require(bool condition, string diagnostic) =>
        InstallJson.Require(condition, diagnostic);
}

/// <summary>Exact binding of protected selector and record bytes to one S2
/// selection and protected host identity. No filesystem or caller hash is
/// accepted here.</summary>
internal static class FinalAdmissionEvaluation
{
    internal static FinalAdmissionBinding Evaluate(byte[] selectorBytes, byte[] recordBytes,
        string hostId, FinalAdmissionExpectation expectation)
    {
        ArgumentNullException.ThrowIfNull(selectorBytes);
        ArgumentNullException.ThrowIfNull(recordBytes);
        ArgumentNullException.ThrowIfNull(hostId);
        ArgumentNullException.ThrowIfNull(expectation);
        FinalAcceptanceCodec.Require(FinalAcceptanceCodec.IsLowerHex(hostId, 64),
            "protected host identity");

        var selector = FinalAcceptanceCodec.ParseSelector(selectorBytes);
        var record = FinalAcceptanceCodec.ParseRecord(recordBytes);
        var finalRecordSha256 = FinalAcceptanceCodec.Hash(recordBytes);

        FinalAcceptanceCodec.Require(finalRecordSha256 == selector.FinalRecordSha256,
            "final record hash does not match the protected expected hash");
        FinalAcceptanceCodec.Require(selector.ArtifactSetSha256 == expectation.ArtifactSetSha256 &&
            selector.QualificationId == expectation.QualificationId &&
            selector.QualificationRecordSha256 == expectation.QualificationRecordSha256,
            "final selector does not bind the retained S2 selection");
        FinalAcceptanceCodec.Require(selector.QualificationId == record.QualificationId &&
            selector.QualificationRecordSha256 == record.QualificationRecordSha256,
            "final selector and record disagree on S2 identity");
        FinalAcceptanceCodec.Require(record.ArtifactSetSha256 == expectation.ArtifactSetSha256 &&
            record.QualificationId == expectation.QualificationId &&
            record.QualificationRecordSha256 == expectation.QualificationRecordSha256,
            "final record does not bind the retained S2 selection");
        FinalAcceptanceCodec.Require(record.ProfileId == expectation.ProfileId &&
            record.ProfileVersion == expectation.ProfileVersion &&
            record.Rid == expectation.Rid && record.Scope == expectation.Scope,
            "final record profile, version, RID or scope differs from S2");
        FinalAcceptanceCodec.Require(record.PlatformInventorySha256 == expectation.PlatformInventorySha256,
            "final record platform inventory differs from S2");
        FinalAcceptanceCodec.Require(record.SourceCommit == expectation.SourceCommit &&
            record.SourceTree == expectation.SourceTree,
            "final record source identity differs from S2");
        FinalAcceptanceCodec.Require(record.HostId == hostId,
            "final record host identity differs from the protected local identity");
        FinalAcceptanceCodec.Require(record.OwnerDecisionSha256 == selector.OwnerDecisionSha256,
            "final selector and record disagree on the owner decision");

        return new(selector, record, finalRecordSha256);
    }
}
