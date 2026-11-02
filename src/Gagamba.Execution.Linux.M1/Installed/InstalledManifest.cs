using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Restricted;

namespace Gagamba.Execution.Linux.M1.Installed;

internal static class InstallJson
{
    internal const int MetadataLimit = 256 * 1024;

    internal static void Require(bool condition, string diagnostic)
    {
        if (!condition) throw new InvalidDataException(diagnostic);
    }

    internal static JsonElement Read(byte[] bytes, bool canonical = false)
    {
        Require(bytes.Length is > 0 and <= MetadataLimit, "metadata size");
        try
        {
            using var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 24 });
            var root = doc.RootElement;
            CheckUnique(root);
            if (canonical)
            {
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                    root.WriteTo(writer);
                Require(bytes.AsSpan().SequenceEqual(stream.GetBuffer().AsSpan(0, (int)stream.Length)), "noncanonical JSON bytes");
            }
            return root.Clone();
        }
        catch (JsonException ex) { throw new InvalidDataException("invalid JSON", ex); }
    }

    private static void CheckUnique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject())
            {
                Require(names.Add(p.Name), "duplicate property");
                CheckUnique(p.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) CheckUnique(item);
    }

    internal static void Keys(JsonElement obj, string fields, bool ordered = true)
    {
        Require(obj.ValueKind == JsonValueKind.Object, "object expected");
        var actual = obj.EnumerateObject().Select(p => p.Name);
        var expected = fields.Split(' ');
        Require(ordered ? actual.SequenceEqual(expected) : actual.ToHashSet().SetEquals(expected), "unknown, missing or unordered fields");
    }

    internal static string String(JsonElement obj, string key, int max = 256)
    {
        var value = obj.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, key);
        var s = value.GetString()!;
        Require(s.Length > 0 && s.Length <= max && s.All(c => c is >= ' ' and <= '~') && s.Trim() == s, key);
        return s;
    }

    internal static long Int(JsonElement obj, string key)
    {
        var v = obj.GetProperty(key);
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt64(out long n) ||
            v.GetRawText() != n.ToString(System.Globalization.CultureInfo.InvariantCulture))
            throw new InvalidDataException(key);
        return n;
    }

    internal static string Hex(JsonElement obj, string key, int length = 64)
    {
        var s = String(obj, key);
        Require(IsHex(s, length), key);
        return s;
    }

    internal static bool IsHex(string s, int length) => s.Length == length && s.Any(c => c != '0') &&
        s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}

internal sealed record InstalledArtifact(string Role, string Kind, string Path, string Sha256, long Length, string Mode);
internal sealed record InstalledManifest(string Digest, string SourceCommit, string SourceTree,
    JsonElement Platform, IReadOnlyList<InstalledArtifact> Artifacts)
{
    internal const string Profile = B3FixtureProfile.ProfileId;
    internal const string Version = "1";
    internal const string Rid = "linux-x64";
    internal const string Scope = "WSL2-x86_64";
    internal const int FileLimit = 32 * 1024 * 1024;
    internal const long TotalLimit = 128L * 1024 * 1024;
    internal static readonly string[] Features = ["cgroup2","openat2","pidfd","SO_PEERPIDFD","subreaper","userns","seccomp"];

    internal static InstalledManifest Parse(byte[] bytes)
    {
        var root = InstallJson.Read(bytes, true);
        InstallJson.Keys(root, "schemaVersion profileId profileVersion source platform protocol policy artifacts");
        InstallJson.Require(InstallJson.Int(root,"schemaVersion") == 1 && InstallJson.String(root,"profileId") == Profile &&
            InstallJson.String(root,"profileVersion") == Version, "unsupported installation profile");
        var source = root.GetProperty("source");
        InstallJson.Keys(source,"commit tree toolchain buildCommandDigest");
        var commit = InstallJson.Hex(source,"commit",40);
        var tree = InstallJson.Hex(source,"tree",40);
        InstallJson.Hex(source,"buildCommandDigest");
        var toolchain = source.GetProperty("toolchain");
        const string tools = "dotnet gcc python distro kernel";
        InstallJson.Keys(toolchain,tools);
        foreach (var key in tools.Split(' ')) InstallJson.String(toolchain,key);

        var platform = root.GetProperty("platform");
        InstallJson.Keys(platform,"rid architecture kernelMin glibcMin dotnetRuntime dotnetHostSha256 runtimeInventorySha256 hostFxrInventorySha256 bubblewrap loaderSha256 requiredFeatures");
        InstallJson.Require(InstallJson.String(platform,"rid") == Rid && InstallJson.String(platform,"architecture") == "x86_64" &&
            InstallJson.String(platform,"glibcMin") == "2.43", "unsupported platform");
        InstallJson.String(platform,"kernelMin");
        var runtime = InstallJson.String(platform,"dotnetRuntime");
        InstallJson.Require(System.Version.TryParse(runtime, out var version) && version.Major == 10 && version.Minor == 0 &&
            version.Build >= 0 && version.Revision < 0 && version.ToString() == runtime, "runtime version");
        foreach (var key in new[] {"dotnetHostSha256","runtimeInventorySha256","hostFxrInventorySha256","loaderSha256"})
            InstallJson.Hex(platform,key);
        var bwrap = platform.GetProperty("bubblewrap");
        InstallJson.Keys(bwrap,"path version sha256 package");
        InstallJson.Require(InstallJson.String(bwrap,"path") == HelperInstallation.PinnedPath &&
            InstallJson.String(bwrap,"version") == HelperInstallation.PinnedVersion &&
            InstallJson.Hex(bwrap,"sha256") == HelperInstallation.PinnedSha256, "bubblewrap baseline");
        InstallJson.String(bwrap,"package");
        var features = platform.GetProperty("requiredFeatures");
        InstallJson.Require(features.ValueKind == JsonValueKind.Array && features.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String) &&
            features.EnumerateArray().Select(e => e.GetString()).SequenceEqual(Features), "feature baseline");

        var protocol = root.GetProperty("protocol");
        InstallJson.Keys(protocol,"manifestSchema implementationId ownerProtocol coordinatorProtocol bootstrap coordinatorHeaderBytes releaseFrameBytes releaseSequence");
        InstallJson.Require(InstallJson.Int(protocol,"manifestSchema") == 1 &&
            InstallJson.String(protocol,"implementationId") == "linux-m1-native-transaction" &&
            InstallJson.Int(protocol,"ownerProtocol") == OwnerProtocol.CurrentVersion && InstallJson.Int(protocol,"coordinatorProtocol") == 1 &&
            InstallJson.Int(protocol,"bootstrap") == 1 && InstallJson.Int(protocol,"coordinatorHeaderBytes") == CoordinatorProtocol.HeaderBytes &&
            InstallJson.Int(protocol,"releaseFrameBytes") == CoordinatorProtocol.ReleaseFrameBytes &&
            InstallJson.Int(protocol,"releaseSequence") == (long)CoordinatorProtocol.ReleaseSequence, "protocol mismatch");
        var policy = root.GetProperty("policy");
        InstallJson.Keys(policy,"profileId seccompFilterSha256 seccompFilterLength bubblewrapArgsSha256 authoritySha256 resourcesSha256");
        InstallJson.Require(InstallJson.String(policy,"profileId") == Profile &&
            InstallJson.Hex(policy,"seccompFilterSha256") == BubblewrapAdapter.FilterSha256 &&
            InstallJson.Int(policy,"seccompFilterLength") == (long)BubblewrapAdapter.FilterLength, "filter policy");
        InstalledPolicy.Check(policy);
        return new(InstallJson.Hash(bytes),commit,tree,platform,ReadArtifacts(root.GetProperty("artifacts")));
    }

    private static IReadOnlyList<InstalledArtifact> ReadArtifacts(JsonElement array)
    {
        InstallJson.Require(array.ValueKind == JsonValueKind.Array && array.GetArrayLength() == 14, "closure count");
        var artifacts = new List<InstalledArtifact>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? previous = null;
        foreach (var item in array.EnumerateArray())
        {
            InstallJson.Keys(item,"role kind path sha256 length mode uid gid nlink rid buildIdentity required consumer survivesController");
            var role = InstallJson.String(item,"role");
            InstallJson.Require(string.CompareOrdinal(previous,role) < 0, "duplicate or unordered role");
            previous = role;
            var path = InstallJson.String(item,"path");
            var parts = path.Split('/');
            InstallJson.Require(parts.Length == 2 && seen.Add(path), "artifact path");
            var dir = parts[0];
            var file = parts[1];
            string kind, consumer = dir, mode = "0444", expectedRole;
            bool survives = dir is "owner" or "recovery";
            if (dir == "native")
            {
                consumer = "coordinator";
                (expectedRole,kind,mode) = file switch {
                    "exec-coordinator" => ("coordinator","native","0555"),
                    "seccomp-filter.bin" => ("seccomp-filter","policy","0444"),
                    _ => throw new InvalidDataException("unknown native artifact") };
            }
            else
            {
                InstallJson.Require(survives || dir == "provider", "invalid component");
                string entry = "Gagamba.Execution.Linux.M1." + (dir == "owner" ? "Owner" : "Recovery");
                string suffix = file switch {
                    "Gagamba.Execution.dll" => "contract", "Gagamba.Execution.Linux.M1.dll" => "m1",
                    _ when survives && file == entry + ".dll" => "entry",
                    _ when survives && file == entry + ".deps.json" => "deps",
                    _ when survives && file == entry + ".runtimeconfig.json" => "runtimeconfig",
                    _ => throw new InvalidDataException("unknown production artifact") };
                expectedRole = dir + "-" + suffix;
                kind = suffix is "deps" or "runtimeconfig" ? "config" : "managed";
            }
            var length = InstallJson.Int(item,"length");
            InstallJson.Require(role == expectedRole && InstallJson.String(item,"kind") == kind &&
                InstallJson.String(item,"mode") == mode && InstallJson.Int(item,"uid") == 0 && InstallJson.Int(item,"gid") == 0 &&
                InstallJson.Int(item,"nlink") == 1 && InstallJson.String(item,"rid") == Rid &&
                InstallJson.String(item,"buildIdentity") == "Production" && InstallJson.String(item,"consumer") == consumer &&
                item.GetProperty("required").ValueKind == JsonValueKind.True &&
                item.GetProperty("survivesController").ValueKind == (survives ? JsonValueKind.True : JsonValueKind.False) &&
                length is > 0 and <= FileLimit, "artifact metadata");
            var hash = InstallJson.Hex(item,"sha256");
            InstallJson.Require(role != "seccomp-filter" || (hash == BubblewrapAdapter.FilterSha256 && length == (long)BubblewrapAdapter.FilterLength), "filter identity");
            artifacts.Add(new(role,kind,path,hash,length,mode));
        }
        var required = new List<string>{"coordinator","seccomp-filter"};
        foreach (var dir in new[]{"provider","owner","recovery"})
        {
            required.AddRange(new[]{dir+"-contract",dir+"-m1"});
            if (dir != "provider") required.AddRange(new[]{dir+"-entry",dir+"-deps",dir+"-runtimeconfig"});
        }
        InstallJson.Require(required.All(r => artifacts.Any(a => a.Role == r)) && artifacts.Sum(a => a.Length) <= TotalLimit,
            "incomplete or oversized closure");
        foreach (var group in artifacts.Where(a => a.Kind == "managed").GroupBy(a => a.Path.Split('/')[1]))
            InstallJson.Require(group.Select(a => (a.Sha256,a.Length)).Distinct().Count() == 1, "inconsistent shared assembly");
        return artifacts.AsReadOnly();
    }
}
