using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class ValidationPreCodec
{
    private const int MaximumBootstrapBytes = 8192;
    private const long MaximumInterval = 120_000_000_000;
    private const string OwnerDll = "Gagamba.Execution.Linux.M1.Owner.dll";
    private const string FixtureDirectory = "/opt/gagamba/validation-fixtures/";
    private static readonly UTF8Encoding StrictUtf8 = new(false, throwOnInvalidBytes: true);

    internal static ValidationPreRefusal DecodeBootstrap(
        ReadOnlySpan<byte> payload, ValidationPreContext context,
        ReadOnlySpan<byte> originalCapabilityDigest, JsonElement originalProjection,
        out ValidationPreBootstrapData? data)
    {
        data = null;
        if (!context.IsPre)
            return ValidationPreRefusal.Context;
        if (payload.Length is < 1 or > MaximumBootstrapBytes)
            return ValidationPreRefusal.Size;
        if (!IsUtf8(payload) || payload.StartsWith("\uFEFF"u8))
            return ValidationPreRefusal.Encoding;
        if (originalCapabilityDigest.Length != 32)
            return ValidationPreRefusal.Expectation;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload.ToArray());
        }
        catch (JsonException)
        {
            return ValidationPreRefusal.Encoding;
        }

        using (document)
        {
            var root = document.RootElement;
            var rule = context.Topology == ValidationPreTopology.B2 ? Rule.B2 : Rule.Integrated;
            if (!ValidObject(root, rule))
                return ValidationPreRefusal.Schema;

            // ValidScalar has verified the literal 64-byte hex token before decoding.
            Span<byte> claimedCapability = stackalloc byte[32];
            var capability = root.GetProperty("AuthorityHash").GetRawText().AsSpan(1, 64);
            for (var i = 0; i < claimedCapability.Length; i++)
                claimedCapability[i] = (byte)((HexNibble(capability[i * 2]) << 4) | HexNibble(capability[i * 2 + 1]));

            if (!claimedCapability.SequenceEqual(originalCapabilityDigest))
                return ValidationPreRefusal.Comparison;
            if (!RelationshipsMatch(root, context.Topology))
                return ValidationPreRefusal.Relationship;
            var projectionResult = CompareProjection(root, originalProjection, rule);
            if (projectionResult != ValidationPreRefusal.None)
                return projectionResult;
            data = new ValidationPreBootstrapData(context.Topology, root.Clone());
            return ValidationPreRefusal.None;
        }
    }

    private static bool IsUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            _ = StrictUtf8.GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool ValidObject(JsonElement value, Rule rule)
    {
        if (value.ValueKind != JsonValueKind.Object)
            return false;

        var fields = Schemas[rule];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            string name;
            try
            {
                name = property.Name;
            }
            catch (InvalidOperationException)
            {
                // JsonDocument postpones unescaping property names. An invalid surrogate
                // escape can fail here even after the structural parse succeeds.
                return false;
            }

            if (!seen.Add(name) || !fields.TryGetValue(name, out var field))
                return false;

            var valid = Schemas.ContainsKey(field)
                ? ValidObject(property.Value, field)
                : ValidScalar(property.Value, field);
            if (!valid)
                return false;
        }

        return seen.Count == fields.Count;
    }

    private static bool ValidScalar(JsonElement value, Rule rule)
    {
        switch (rule)
        {
            case Rule.Null: return value.ValueKind == JsonValueKind.Null;
            case Rule.False: return value.ValueKind == JsonValueKind.False;
            case Rule.V3: return Integer(value, 3, 3);
            case Rule.One: return Integer(value, 1, 1);
            case Rule.Four: return Integer(value, 4, 4);
            case Rule.Budget: return Integer(value, 16_777_216, 16_777_216);
            case Rule.Pid:
            case Rule.Principal: return Integer(value, 1, int.MaxValue);
            case Rule.PositiveInt64: return Integer(value, 1, long.MaxValue);
            case Rule.Lead: return Integer(value, 1, MaximumInterval);
        }

        if (value.ValueKind != JsonValueKind.String)
            return false;

        if (rule == Rule.CapabilityHex)
        {
            var raw = value.GetRawText();
            return raw.Length == 66 && Hex(raw.AsSpan(1, 64), upper: false);
        }

        string text;
        try
        {
            text = value.GetString()!;
        }
        catch (InvalidOperationException)
        {
            // The value is a live string element. Failure to unescape it is a malformed token.
            return false;
        }

        return rule switch
        {
            Rule.Guid => IsGuid(text),
            Rule.Secret32 => IsCanonicalSecret(text),
            Rule.Path => IsPath(text),
            Rule.Sha => NonzeroHash(text, upper: false),
            Rule.UpperSha => NonzeroHash(text, upper: true),
            Rule.PidNs => Namespace(text, "pid:["),
            Rule.UserNs => Namespace(text, "user:["),
            Rule.RuntimeVersion => text.StartsWith("10.0.", StringComparison.Ordinal)
                && DecimalDigits(text.AsSpan(5))
                && int.TryParse(text.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out _),
            Rule.ValidId => IsValidId(text),
            Rule.ProfileId => text == ValidationPreContext.ProfileId,
            Rule.ProfileVersion => text == ValidationPreContext.ProfileVersion,
            Rule.PreMode => text == "validation-pre",
            Rule.Tool => text == "tool",
            Rule.Input => text == "input",
            Rule.Coordinator => text == "coordinator",
            Rule.Filter => text == "filter",
            _ => throw new InvalidOperationException("Unexpected v3-pre scalar rule.")
        };
    }

    private static bool Integer(JsonElement value, long min, long max) =>
        value.ValueKind == JsonValueKind.Number && DecimalDigits(value.GetRawText())
        && value.TryGetInt64(out var number) && number >= min && number <= max;

    private static bool DecimalDigits(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || (text.Length > 1 && text[0] == '0'))
            return false;
        foreach (var digit in text)
            if (digit is < '0' or > '9')
                return false;
        return true;
    }

    private static bool Hex(ReadOnlySpan<char> value, bool upper)
    {
        var first = upper ? 'A' : 'a';
        var last = upper ? 'F' : 'f';
        foreach (var ch in value)
            if (!(ch is >= '0' and <= '9') && (ch < first || ch > last))
                return false;
        return true;
    }

    private static int HexNibble(char ch) => ch <= '9' ? ch - '0' : ch - 'a' + 10;

    private static bool NonzeroHash(ReadOnlySpan<char> value, bool upper) =>
        value.Length == 64 && Hex(value, upper) && value.ContainsAnyExcept('0');

    private static bool IsGuid(string text)
    {
        if (text.Length != 36 || !Guid.TryParseExact(text, "D", out var guid) || guid == Guid.Empty)
            return false;
        return text == guid.ToString("D");
    }

    private static bool IsCanonicalSecret(string text)
    {
        Span<byte> decoded = stackalloc byte[32];
        return text.Length == 44 && text[^1] == '='
            && Convert.TryFromBase64String(text, decoded, out var written) && written == 32
            && Convert.ToBase64String(decoded) == text;
    }

    private static bool IsPath(string text)
    {
        if (text.Length is < 2 or > 1024 || text[0] != '/')
            return false;
        foreach (var part in text.AsSpan(1).Split('/'))
        {
            var component = text.AsSpan(1)[part];
            if (component.IsEmpty || component.SequenceEqual(".") || component.SequenceEqual(".."))
                return false;
            foreach (var ch in component)
                if (ch is < '!' or > '~' or '\\')
                    return false;
        }
        return true;
    }

    private static bool Namespace(string text, string prefix)
    {
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || text[^1] != ']')
            return false;
        var number = text.AsSpan(prefix.Length, text.Length - prefix.Length - 1);
        return DecimalDigits(number)
            && ulong.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var inode)
            && inode > 0;
    }

    private static bool IsValidId(string text)
    {
        if (text.Length is < 1 or > 128 || text[0] == '.' || text[^1] == '.')
            return false;
        foreach (var ch in text)
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '.' or '_' or '-'))
                return false;
        return true;
    }

    private static string Text(JsonElement element, string member) => element.GetProperty(member).GetString()!;

    private static long Number(JsonElement element, string member) => element.GetProperty(member).GetInt64();

    private static bool RelationshipsMatch(JsonElement root, ValidationPreTopology topology)
    {
        var integrated = root.GetProperty("Integrated");
        var identity = topology == ValidationPreTopology.B2
            ? root.GetProperty("B2InstallationIdentity")
            : integrated.GetProperty("InstallationIdentity");

        var installation = Text(identity, "Generation");
        var directory = "/opt/gagamba/installations/" + installation;
        var owner = root.GetProperty("Owner");
        if (installation != Text(identity, "ArtifactSetSha256")
            || Text(root.GetProperty("Recovery"), "Directory") != directory + "/recovery"
            || Text(owner, "Directory") != directory + "/owner")
            return false;

        if (topology == ValidationPreTopology.B2)
            return true;

        var controller = integrated.GetProperty("Controller");
        var interval = Number(integrated, "ExpiresAtNs") - Number(integrated, "AcceptedAtNs");
        if (Number(controller, "Pid") != Number(root, "ControllerPid")
            || Number(controller, "Uid") != Number(root, "ControllerUid")
            || interval is <= 0 or > MaximumInterval)
            return false;

        var runtime = integrated.GetProperty("Runtime");
        var runtimeVersion = Text(runtime, "RuntimeVersion");
        // Both spellings have already been checked against their ASCII SHA domains.
        if (Text(runtime, "HostPath") != "/usr/lib/dotnet/dotnet"
            || Text(runtime, "RuntimeDirectory") != "/usr/lib/dotnet/shared/Microsoft.NETCore.App/" + runtimeVersion
            || Text(runtime, "HostFxrDirectory") != "/usr/lib/dotnet/host/fxr/" + runtimeVersion
            || !string.Equals(Text(runtime, "RuntimeInventorySha256"), Text(identity, "RuntimeInventorySha256"),
                StringComparison.OrdinalIgnoreCase))
            return false;

        var profile = integrated.GetProperty("Profile");
        var source = Text(profile, "SourceRootPath");
        var suffix = "/va-" + Text(root, "Execution").Replace("-", "", StringComparison.Ordinal);
        return Text(profile, "DelegatedParentPath") == Text(root, "DelegatedParent")
            && Text(profile, "OwnerDllPath") == Text(owner, "Directory") + "/" + OwnerDll
            && Text(profile, "OwnerDllSha256") == Text(owner.GetProperty("Sha256"), OwnerDll)
            && Text(profile, "CoordinatorSha256") == Text(identity, "CoordinatorSha256")
            && source.StartsWith(FixtureDirectory, StringComparison.Ordinal)
            && source.Length == FixtureDirectory.Length + 64
            && Hex(source.AsSpan(FixtureDirectory.Length), upper: false)
            && Text(profile, "StagingParentPath").EndsWith(suffix, StringComparison.Ordinal)
            && Text(profile, "JournalDirectory").EndsWith(suffix, StringComparison.Ordinal);
    }

    // This is caller-supplied typed data (a JsonElement object), not a second bootstrap.
    // Wire constants are checked by the schema. The adopted profile also binds its fixed policy claims.
    private static ValidationPreRefusal CompareProjection(JsonElement claim, JsonElement original, Rule rule)
    {
        try
        {
            return CompareProjectionObject(claim, original, rule);
        }
        catch (ObjectDisposedException)
        {
            return ValidationPreRefusal.Expectation;
        }
    }

    private static ValidationPreRefusal CompareProjectionObject(JsonElement claim, JsonElement original, Rule rule)
    {
        if (original.ValueKind != JsonValueKind.Object)
            return ValidationPreRefusal.Expectation;

        var fields = Schemas[rule];
        var supplied = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in original.EnumerateObject())
        {
            string name;
            try
            {
                name = property.Name;
            }
            catch (InvalidOperationException)
            {
                // The projection can contain a member name that cannot be decoded.
                return ValidationPreRefusal.Expectation;
            }

            if (fields.TryGetValue(name, out var field) && RequiresProjection(field)
                && !supplied.TryAdd(name, property.Value))
                return ValidationPreRefusal.Expectation;
        }

        foreach (var (member, field) in fields)
        {
            if (!RequiresProjection(field))
                continue;

            var policy = field is Rule.Tool or Rule.Input or Rule.Coordinator or Rule.Filter or Rule.Budget or Rule.Four;
            if (!supplied.TryGetValue(member, out var expectedMember))
                return policy ? ValidationPreRefusal.Expectation : ValidationPreRefusal.Comparison;

            var claimedMember = claim.GetProperty(member);
            if (policy && claimedMember.ValueKind != expectedMember.ValueKind)
                return ValidationPreRefusal.Expectation;

            if (Schemas.ContainsKey(field))
            {
                var result = CompareProjectionObject(claimedMember, expectedMember, field);
                if (result != ValidationPreRefusal.None)
                    return result;
            }
            else
            {
                var result = CompareProjectionScalar(claimedMember, expectedMember);
                if (result != ValidationPreRefusal.None)
                    return result;
            }
        }

        return ValidationPreRefusal.None;
    }

    private static ValidationPreRefusal CompareProjectionScalar(JsonElement claim, JsonElement expected)
    {
        var kind = expected.ValueKind;
        try
        {
            return JsonElement.DeepEquals(claim, expected)
                ? ValidationPreRefusal.None
                : ValidationPreRefusal.Comparison;
        }
        catch (InvalidOperationException) when (kind == JsonValueKind.String)
        {
            // JSON string values can defer decoding of malformed surrogate escapes.
            return ValidationPreRefusal.Expectation;
        }
        catch (ArgumentOutOfRangeException ex) when (kind == JsonValueKind.Number && ex.ParamName == "exponent")
        {
            return ValidationPreRefusal.Expectation;
        }
    }

    private static bool RequiresProjection(Rule field) => field is not
        (Rule.V3 or Rule.One or Rule.PreMode or Rule.CapabilityHex or Rule.False or Rule.Null
            or Rule.ProfileId or Rule.ProfileVersion);

    private enum Rule
    {
        B2, Integrated, IntegratedBody, RecoveryPin, OwnerPin, RecoveryHashes, OwnerHashes, PreIdentity, Controller, Runtime, Profile, V3, Guid, Pid, Principal, Secret32, Path, Null, PositiveInt64, PreMode, CapabilityHex, Sha, One, ProfileId, ProfileVersion, False, PidNs, UserNs, UpperSha, RuntimeVersion, Tool, Input, Coordinator, Filter, ValidId, Budget, Four, Lead
    }

    // The 11 closed objects from the adopted schema annex. Every listed member is required.
    private static readonly Dictionary<Rule, Dictionary<string, Rule>> Schemas = new()
    {
        [Rule.B2] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Version"] = Rule.V3,
            ["Execution"] = Rule.Guid,
            ["Generation"] = Rule.Guid,
            ["Session"] = Rule.Guid,
            ["ControllerPid"] = Rule.Pid,
            ["ControllerUid"] = Rule.Principal,
            ["Secret"] = Rule.Secret32,
            ["Recovery"] = Rule.RecoveryPin,
            ["Owner"] = Rule.OwnerPin,
            ["EvidenceDirectory"] = Rule.Path,
            ["DelegatedParent"] = Rule.Null,
            ["Integrated"] = Rule.Null,
            ["B2InstallationIdentity"] = Rule.PreIdentity,
            ["B2DeadlineNs"] = Rule.PositiveInt64,
            ["AuthorityMode"] = Rule.PreMode,
            ["AuthorityHash"] = Rule.CapabilityHex,
        },
        [Rule.Integrated] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Version"] = Rule.V3,
            ["Execution"] = Rule.Guid,
            ["Generation"] = Rule.Guid,
            ["Session"] = Rule.Guid,
            ["ControllerPid"] = Rule.Pid,
            ["ControllerUid"] = Rule.Principal,
            ["Secret"] = Rule.Secret32,
            ["Recovery"] = Rule.RecoveryPin,
            ["Owner"] = Rule.OwnerPin,
            ["EvidenceDirectory"] = Rule.Path,
            ["DelegatedParent"] = Rule.Path,
            ["Integrated"] = Rule.IntegratedBody,
            ["AuthorityMode"] = Rule.PreMode,
            ["AuthorityHash"] = Rule.CapabilityHex,
        },
        [Rule.IntegratedBody] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Profile"] = Rule.Profile,
            ["Issuer"] = Rule.Guid,
            ["StoreGeneration"] = Rule.Guid,
            ["AcceptedAtNs"] = Rule.PositiveInt64,
            ["ExpiresAtNs"] = Rule.PositiveInt64,
            ["Controller"] = Rule.Controller,
            ["Runtime"] = Rule.Runtime,
            ["InstallationIdentity"] = Rule.PreIdentity,
        },
        [Rule.RecoveryPin] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Directory"] = Rule.Path,
            ["Sha256"] = Rule.RecoveryHashes,
        },
        [Rule.OwnerPin] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Directory"] = Rule.Path,
            ["Sha256"] = Rule.OwnerHashes,
        },
        [Rule.RecoveryHashes] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Gagamba.Execution.Linux.M1.Recovery.dll"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.Recovery.deps.json"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.dll"] = Rule.Sha,
            ["Gagamba.Execution.dll"] = Rule.Sha,
        },
        [Rule.OwnerHashes] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Gagamba.Execution.Linux.M1.Owner.dll"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.Owner.deps.json"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json"] = Rule.Sha,
            ["Gagamba.Execution.Linux.M1.dll"] = Rule.Sha,
            ["Gagamba.Execution.dll"] = Rule.Sha,
        },
        [Rule.PreIdentity] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["ArtifactSetSha256"] = Rule.Sha,
            ["OwnerClosureSha256"] = Rule.Sha,
            ["RecoveryClosureSha256"] = Rule.Sha,
            ["CoordinatorSha256"] = Rule.Sha,
            ["SeccompFilterSha256"] = Rule.Sha,
            ["RuntimeInventorySha256"] = Rule.Sha,
            ["Generation"] = Rule.Sha,
            ["ManifestSchema"] = Rule.One,
            ["OwnerProtocol"] = Rule.One,
            ["CoordinatorProtocol"] = Rule.One,
            ["Bootstrap"] = Rule.One,
            ["ProfileId"] = Rule.ProfileId,
            ["ProfileVersion"] = Rule.ProfileVersion,
            ["Qualified"] = Rule.False,
            ["qualificationId"] = Rule.Null,
            ["qualificationRecordSha256"] = Rule.Null,
        },
        [Rule.Controller] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["Pid"] = Rule.Pid,
            ["Uid"] = Rule.Principal,
            ["Gid"] = Rule.Principal,
            ["PidNamespace"] = Rule.PidNs,
            ["UserNamespace"] = Rule.UserNs,
        },
        [Rule.Runtime] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["HostPath"] = Rule.Path,
            ["HostSha256"] = Rule.Sha,
            ["RuntimeDirectory"] = Rule.Path,
            ["RuntimeInventorySha256"] = Rule.UpperSha,
            ["HostFxrDirectory"] = Rule.Path,
            ["HostFxrInventorySha256"] = Rule.UpperSha,
            ["RuntimeVersion"] = Rule.RuntimeVersion,
        },
        [Rule.Profile] = new Dictionary<string, Rule>(StringComparer.Ordinal)
        {
            ["OwnerDllPath"] = Rule.Path,
            ["DelegatedParentPath"] = Rule.Path,
            ["SourceRootPath"] = Rule.Path,
            ["StagingParentPath"] = Rule.Path,
            ["JournalDirectory"] = Rule.Path,
            ["OwnerDllSha256"] = Rule.Sha,
            ["ToolSourceName"] = Rule.Tool,
            ["InputSourceName"] = Rule.Input,
            ["CoordinatorSourceName"] = Rule.Coordinator,
            ["FilterSourceName"] = Rule.Filter,
            ["ToolSha256"] = Rule.Sha,
            ["InputSha256"] = Rule.Sha,
            ["CoordinatorSha256"] = Rule.Sha,
            ["ToolEnrollmentId"] = Rule.ValidId,
            ["InputEnrollmentId"] = Rule.ValidId,
            ["TotalBudgetBytes"] = Rule.Budget,
            ["MaxObjects"] = Rule.Four,
            ["ExpiresLeadNanoseconds"] = Rule.Lead,
            ["IntegratedRecovery"] = Rule.Null,
        },
    };
}
