using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;

var output = args[0];
Directory.CreateDirectory(output);
using var results = File.CreateText(Path.Combine(output, "results.jsonl"));
int checks = 0, failures = 0;
var capability = Hash("original-capability");
var session = Guid.Parse(Id(3));
var binary = new ValidationPreBinaryExpectations(Hash("install")) {
    Session = session, OwnerClosureSha256 = Hash("owner"), RecoveryClosureSha256 = Hash("recovery"),
    CoordinatorSha256 = Hash("coord"), SeccompFilterSha256 = Hash("filter"),
    RuntimeInventorySha256 = Hash("runtime"), ProfileId = "ae4-linux-fixed-fixture-v1", ProfileVersion = "1"
};
const string core = "Gagamba.Execution.Linux.M1.";
var occurrences = new int[2];
var schemas = new Dictionary<string, int>();
string schemaText = File.ReadAllText(args[1]);
string schema = "";
var rows = new Dictionary<string, Dictionary<string,string>>();
foreach (var line in schemaText.Split('\n').Select(l => l.TrimEnd('\r'))) {
    if (line.StartsWith("## ")) schema = line[3..];
    if (!line.StartsWith("| " + (char)96) || schema == "Explicit prohibitions") continue;
    var columns = line.Split('|', StringSplitOptions.TrimEntries);
    if (columns[3] != "Yes") continue;
    if (!rows.TryGetValue(schema, out var members)) rows.Add(schema, members = new());
    members.Add(columns[1].Trim((char)96), columns[2]);
}
if (rows.Count != 11 || rows.Sum(s => s.Value.Count) != 99) throw new InvalidDataException("Schema count");

foreach (bool b2 in new[] { true, false }) {
    var domain = b2 ? "B2" : "Integrated";
    var topology = b2 ? ValidationPreTopology.B2 : ValidationPreTopology.Integrated;
    var wire = Witness(b2, projection: false);
    var projection = Witness(b2, projection: true);
    string json = wire.ToJsonString();
    string original = projection.ToJsonString();
    var bytes = Encoding.UTF8.GetBytes(json);
    File.WriteAllBytes(Path.Combine(output, domain + ".json"), bytes);
    File.WriteAllText(Path.Combine(output, domain + "-projection.json"), original);
    using var expected = JsonDocument.Parse(original);
    var ctx = new ValidationPreContext(3, topology);
    void Run(string name, string text, string want = "refuse", JsonElement? exp = null, byte[]? digest = null) =>
        Bootstrap(domain + "/" + name, Encoding.UTF8.GetBytes(text), ctx, exp ?? expected.RootElement, digest ?? capability, want);
    Run("full", json, "None");
    var detachedBytes = bytes.ToArray();
    var detachedStatus = ValidationPreCodec.DecodeBootstrap(detachedBytes, ctx, capability, expected.RootElement, out var detached);
    Array.Fill(detachedBytes, (byte)0);
    Record(domain + "/result-detached-from-payload", "None",
        detachedStatus == ValidationPreRefusal.None && detached?.Fields.GetProperty("Execution").GetString() == Id(1)
            ? "None" : "invalid detached result", domain + ".json");
    Run("escaped-key", json.Replace("\"AuthorityHash\"", "\"\\u0041uthorityHash\""), "None");
    Run("duplicate-escaped-key", "{\"\\u0041uthorityHash\":\"" + Hex(capability) + "\"," + json[1..]);
    Run("escaped-value", json.Replace(Hex(capability), "\\u00" + ((byte)Hex(capability)[0]).ToString("x2") + Hex(capability)[1..]));
    Run("uppercase", json.Replace(Hex(capability), Convert.ToHexString(capability)));
    Run("truncated-hash", json.Replace(Hex(capability), Hex(capability)[..63]));
    Run("uppercase-lookalike", json.Replace(Hex(capability), "ａ" + Hex(capability)[1..]));
    Run("hash-prefix", json.Replace(Hex(capability), "0x" + Hex(capability)));
    Run("hash-zero", json.Replace(Hex(capability), new string('0', 64)), "None", digest: new byte[32]);
    Run("hash-expectation-short", json, digest: new byte[31]);
    Run("hash-expectation-long", json, digest: new byte[33]);
    Run("hash-expectation-missing", json, digest: []);
    for (int i = 0; i < 32; i++) {
        var digest = capability.ToArray();
        digest[i] ^= 0x80;
        Run($"hash-byte-{i}", json, digest: digest);
    }
    foreach (var limit in new[] { 8191, 8192, 8193 })
        Run("byte-bound-" + limit, json.PadRight(limit), limit <= 8192 ? "None" : "Size");
    foreach (string malformed in new[] {
        "", "[]", "null", json[..^1], json + "{}", json + "\0", "// hi\n" + json,
        json.Insert(json.Length - 1, ","), "\uFEFF" + json, json.Replace("\"EvidenceDirectory\"", "\"\\ud800\""),
        json.Replace("/q1/evidence", "/q1/\\udfff"), json.Replace("/q1/evidence", "/q1/\\ud800"),
        json.Replace("/q1/evidence", "/q1/\\u00zz"), json.Replace("/q1/evidence", "/q1/\\ud83d\\ude00")
    }) Run("malformed-" + checks, malformed);
    foreach (byte[] invalid in new byte[][] { [0xc0, 0xaf], [0xed, 0xa0, 0x80], [0xf4, 0x90, 0x80, 0x80], [0xff], [0xe2, 0x82] })
        Bootstrap(domain + "/invalid-utf8-" + Convert.ToHexString(invalid), [.. bytes[..^1], .. invalid], ctx, expected.RootElement, capability, "Encoding");
    Run("alternate-whitespace-order-escapes",
        " \t\r\n" + new JsonObject(wire.Reverse().Select(p => KeyValuePair.Create(p.Key, p.Value?.DeepClone()))).ToJsonString()
            .Replace("/q1/evidence", "\\/q1\\/evid\\u0065nce") + "\n", "None");
    foreach (string number in new[] { "1", "2", "4", "03", "+3", "3.0", "3e0", "-3", "\"3\"", "9223372036854775808" })
        Run("version-" + number, json.Replace("\"Version\":3", "\"Version\":" + number));
    var multibyte = json.Replace("/q1/evidence", "/q1/evidénce");
    Run("multibyte-8192", multibyte.PadRight(8192 - 1), "Schema");
    Run("multibyte-8193", multibyte.PadRight(8192), "Size");
    foreach (ushort version in new ushort[] { 0, 1, 2, 4, 65535 })
        Bootstrap(domain + "/context-version-" + version, bytes, new(version, topology), expected.RootElement, capability, "Context");
    Bootstrap(domain + "/opposite-topology", bytes, new(3, b2 ? ValidationPreTopology.Integrated : ValidationPreTopology.B2), expected.RootElement, capability, "Schema");
    Bootstrap(domain + "/undefined-topology", bytes, new(3, (ValidationPreTopology)0), expected.RootElement, capability, "Context");
    Bootstrap(domain + "/missing-projection", bytes, ctx, default, capability, "Expectation");
    if (!b2) {
        foreach (string name in new[] { "B2DeadlineNs", "B2InstallationIdentity" })
            Run("prohibited-null/" + name, "{\"" + name + "\":null," + json[1..]);
    }
    Walk(wire, domain, []);
    void Walk(JsonObject obj, string shape, string[] path) {
        var rules = rows[shape];
        if (!obj.Select(p => p.Key).ToHashSet(StringComparer.Ordinal).SetEquals(rules.Keys))
            throw new InvalidDataException(shape + " witness/schema mismatch");
        schemas[shape] = rules.Count;
        string label = string.Join('/', path);
        var unknown = (JsonObject)wire.DeepClone();
        At(unknown, path).Add("q1Unknown", null);
        Run(label + "/unknown", unknown.ToJsonString(), "Schema");
        foreach (var (key, value) in obj) {
            occurrences[b2 ? 0 : 1]++;
            string item = label + "/" + key;
            var modified = (JsonObject)wire.DeepClone();
            At(modified, path).Remove(key);
            Run(item + "/missing", modified.ToJsonString(), "Schema");
            modified = (JsonObject)wire.DeepClone();
            At(modified, path)[key] = new JsonArray();
            Run(item + "/kind", modified.ToJsonString(), "Schema");
            var originalObject = obj.ToJsonString();
            var serialized = "\"" + key + "\":" + (value?.ToJsonString() ?? "null");
            Run(item + "/duplicate", json.Replace(originalObject, "{" + serialized + "," + originalObject[1..]), "Schema");
            if (value != null) {
                modified = (JsonObject)wire.DeepClone();
                At(modified, path)[key] = null;
                Run(item + "/null", modified.ToJsonString(), "Schema");
            }
            if (value is JsonObject child) Walk(child, rules[key], [..path, key]);
            else if (At(projection, path).ContainsKey(key)) {
                var x = (JsonObject)projection.DeepClone();
                At(x, path).Remove(key);
                using var missing = JsonDocument.Parse(x.ToJsonString());
                Run(item + "/expectation-missing", json, "refuse", exp: missing.RootElement);
                x = (JsonObject)projection.DeepClone();
                At(x, path)[key] = Alternate(rules[key], value);
                using var different = JsonDocument.Parse(x.ToJsonString());
                Run(item + "/expectation-mismatch", json, "refuse", exp: different.RootElement);
            }
            foreach (JsonNode bad in Invalid(rules[key])) {
                modified = (JsonObject)wire.DeepClone();
                At(modified, path)[key] = bad;
                Run(item + "/scalar/" + checks, modified.ToJsonString(), "Schema");
            }
        }
    }
    using var ambiguous = JsonDocument.Parse("{\"\\u0053ession\":\"" + Id(99) + "\"," + original[1..]);
    Run("ambiguous-original-session", json, "refuse", exp: ambiguous.RootElement);
    using var badSurrogate = JsonDocument.Parse(original.Replace("/q1/evidence", "/q1/\\ud800"));
    Run("invalid-original-scalar", json, "refuse", exp: badSurrogate.RootElement);
    var disposed = JsonDocument.Parse(original);
    var disposedElement = disposed.RootElement;
    disposed.Dispose();
    Run("disposed-original", json, "refuse", exp: disposedElement);
    if (!b2) {
        (string Path, JsonNode Value)[] relationships = [
            ("Integrated/Controller/Pid", 42), ("Integrated/Controller/Uid", 42),
            ("Integrated/ExpiresAtNs", 20), ("Integrated/ExpiresAtNs", 120000000022L),
            ("Integrated/Runtime/HostPath", "/q1/dotnet"), ("Integrated/Runtime/RuntimeDirectory", "/q1/runtime"),
            ("Integrated/Runtime/HostFxrDirectory", "/q1/hostfxr"), ("Integrated/Runtime/RuntimeInventorySha256", Convert.ToHexString(Hash("other"))),
            ("Integrated/Profile/DelegatedParentPath", "/q1/wrong"), ("Integrated/Profile/OwnerDllPath", "/q1/owner.dll"),
            ("Integrated/Profile/OwnerDllSha256", Hex(Hash("other"))), ("Integrated/Profile/CoordinatorSha256", Hex(Hash("other"))),
            ("Integrated/Profile/SourceRootPath", "/q1/source"), ("Integrated/Profile/StagingParentPath", "/q1/wrong"),
            ("Integrated/Profile/JournalDirectory", "/q1/wrong") ];
        foreach (var (path, val) in relationships) Relation(path, val);
        var max = (JsonObject)wire.DeepClone();
        At(max, ["Integrated"])["AcceptedAtNs"] = long.MaxValue - 120000000000;
        At(max, ["Integrated"])["ExpiresAtNs"] = long.MaxValue;
        var maxProjection = (JsonObject)projection.DeepClone();
        At(maxProjection, ["Integrated"])["AcceptedAtNs"] = long.MaxValue - 120000000000;
        At(maxProjection, ["Integrated"])["ExpiresAtNs"] = long.MaxValue;
        using var edge = JsonDocument.Parse(maxProjection.ToJsonString());
        Run("max-interval-int64", max.ToJsonString(), "None", exp: edge.RootElement);
    }
    Relation((b2 ? "B2InstallationIdentity" : "Integrated/InstallationIdentity") + "/Generation", Hex(Hash("other")));
    Relation("Recovery/Directory", "/q1/recovery");
    Relation("Owner/Directory", "/q1/owner");
    void Relation(string path, JsonNode val) {
        var parts = path.Split('/');
        var m = (JsonObject)wire.DeepClone();
        At(m, parts[..^1])[parts[^1]] = val.DeepClone();
        var p = (JsonObject)projection.DeepClone();
        At(p, parts[..^1])[parts[^1]] = val.DeepClone();
        using var independent = JsonDocument.Parse(p.ToJsonString());
        Run("relationship/" + path + "/" + checks, m.ToJsonString(), "Relationship", exp: independent.RootElement);
    }
    Record(domain + "/legacy-recovery-pre-refusal", "JsonException", Invoke(() => RecoveryConfiguration.Decode(bytes)), Convert.ToHexString(SHA256.HashData(bytes)));
}
if (schemas.Count != 11 || !occurrences.SequenceEqual(new[] { 46, 83 }))
    throw new InvalidDataException("Unexpected independent schema occurrence count");
Console.WriteLine($"Schema rows={schemas.Values.Sum()} unique schemas={schemas.Count} occurrences B2={occurrences[0]} integrated={occurrences[1]}");

var b2Context = new ValidationPreContext(3, ValidationPreTopology.B2);
var intContext = new ValidationPreContext(3, ValidationPreTopology.Integrated);
byte[] tail = PackProfile();
var memberOffsets = new Dictionary<int, (byte[] Bytes, int[] Digests, int[] Layout)> {
    [9] = (Echo(0), [0, 32], [64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75]),
    [36] = (Echo(1), [0, 32], [64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75]),
    [37] = (Pack37(), [0, 16, 48, 80, 112, 144, 176], Enumerable.Range(208, 38).ToArray()),
    [10] = (new byte[] { 1, 0, 0, 0 }.Concat(Hash("install")).ToArray(), [4], [0, 1, 2, 3]),
    [12] = (session.ToByteArray().Concat(Hash("install")).ToArray(), [0, 16], [])
};
foreach (var (msg, (payload, digests, layout)) in memberOffsets) {
    File.WriteAllBytes(Path.Combine(output, $"message-{msg}.bin"), payload);
    foreach (var ctx in new[] { b2Context, intContext }) {
        bool b2 = ctx.Topology == ValidationPreTopology.B2;
        string prefix = $"{ctx.Topology}/{msg}";
        bool excluded = !b2 && (msg == 37 || msg == 12);
        Bin(prefix + "/full", payload, ctx, msg, true, binary, excluded ? "Context" : "None");
        if (excluded) continue;
        if (msg is 9 or 36) {
            byte[] extended = [..payload, ..tail];
            Bin(prefix + "/extended", extended, ctx, msg, true, binary, b2 ? "Size" : "None");
            if (!b2) {
                File.WriteAllBytes(Path.Combine(output, $"message-{msg}-integrated-extended.bin"), extended);
                for (int i = 76; i < 105; i++)
                    Bin(prefix + $"/tail-byte-{i}", Change(extended, i, 0x80), ctx, msg, true, binary, "Layout");
                foreach (int i in new[] { 77, 78, 103, 104 })
                    Bin(prefix + $"/partial-tail-{i}", extended[..i], ctx, msg, true, binary, "Size");
                foreach (byte len in new byte[] { 0, 1, 25, 27, 64, 65, 255 })
                    Bin(prefix + $"/prefix-76-value-{len}", Replace(extended, 76, len), ctx, msg, true, binary, "Layout");
                foreach (byte len in new byte[] { 0, 2, 64, 65, 255 })
                    Bin(prefix + $"/prefix-103-value-{len}", Replace(extended, 103, len), ctx, msg, true, binary, "Layout");
                Bin(prefix + "/duplicate-tail", [..extended, ..tail], ctx, msg, true, binary, "Size");
                Bin(prefix + "/extended-trailing", [..extended, 0], ctx, msg, true, binary, "Size");
                var noProfile = new ValidationPreBinaryExpectations(binary.ArtifactSetSha256) {
                    RecoveryClosureSha256 = binary.RecoveryClosureSha256, OwnerClosureSha256 = binary.OwnerClosureSha256
                };
                Bin(prefix + "/base-missing-profile", payload, ctx, msg, true, noProfile, "Expectation");
                Bin(prefix + "/extended-missing-profile", extended, ctx, msg, true, noProfile, "Expectation");
            }
            Bin(prefix + "/opposite-role", payload, ctx, msg == 9 ? 36 : 9, true, binary, "Layout");
        }
        foreach (int at in digests) {
            int width = at == 0 && (msg is 12 or 37) ? 16 : 32;
            for (int i = at; i < at + width; i++)
                Bin(prefix + $"/comparison-byte-{i}", Change(payload, i, 0x80), ctx, msg, true, binary, "Comparison");
        }
        foreach (int at in layout)
            Bin(prefix + $"/layout-byte-{at}", Change(payload, at, 0x80), ctx, msg, true, binary, "Layout");
        if (msg is 9 or 36 or 37) {
            int first = msg == 37 ? 208 : 64;
            int status = msg == 37 ? 216 : 71;
            Bin(prefix + "/reversed-u16", Replace(Replace(payload, first, 0), first + 1, 1), ctx, msg, true, binary, "Layout");
            Bin(prefix + "/qualified-status", Replace(payload, status, 1), ctx, msg, true, binary, "Layout");
        }
        if (msg is 12 or 37) {
            var network = payload.ToArray();
            session.ToByteArray(bigEndian: true).CopyTo(network, 0);
            Bin(prefix + "/network-guid", network, ctx, msg, true, binary, "Comparison");
            Bin(prefix + "/missing-session", payload, ctx, msg, true, new(binary.ArtifactSetSha256) {
                ProfileId = binary.ProfileId, ProfileVersion = binary.ProfileVersion
            }, "Expectation");
        }
        if (msg is 10 or 12)
            Bin(prefix + "/without-predecessor", payload, ctx, msg, false, binary, "Context");
        if (msg == 10) {
            for (byte reason = 1; reason <= 5; reason++) {
                var refusal = Replace(Replace(payload, 0, 0), 1, reason);
                Bin(prefix + $"/valid-refusal-{reason}", refusal, ctx, msg, true, binary, "PeerRefusal");
                Bin(prefix + $"/refusal-wrong-digest-{reason}", Change(refusal, 35, 1), ctx, msg, true, binary, "Comparison");
            }
            Bin(prefix + "/zero-zero", Replace(payload, 0, 0), ctx, msg, true, binary, "Layout");
            Bin(prefix + "/one-five", Replace(payload, 1, 5), ctx, msg, true, binary, "Layout");
            Bin(prefix + "/zero-six", Replace(Replace(payload, 0, 0), 1, 6), ctx, msg, true, binary, "Layout");
        }
        foreach (int cut in new[] { 0, 1, payload.Length / 2, payload.Length - 1 })
            Bin(prefix + "/truncated-" + cut, payload[..cut], ctx, msg, true, binary, "Size");
        Bin(prefix + "/trailing", [..payload, 0], ctx, msg, true, binary, "Size");
        Bin(prefix + "/missing-expectations", payload, ctx, msg, true, null, "Expectation");
        Bin(prefix + "/wrong-expectation-width", payload, ctx, msg, true, new(new byte[31]), "Expectation");
        foreach (ushort version in new ushort[] { 0, 1, 2, 4, 65535 })
            Bin(prefix + "/version-" + version, payload, new(version, ctx.Topology), msg, true, binary, "Context");
        Bin(prefix + "/undefined-topology", payload, new(3, (ValidationPreTopology)0), msg, true, binary, "Context");
    }
}
Bin("ReleaseRequest/excluded", new byte[32], intContext, 27, true, binary, "Context");

var legacyIdentity = new Gagamba.Execution.Linux.M1.Installed.InstallationIdentity(
    Hex(Hash("install")), Hex(Hash("owner")), Hex(Hash("recovery")), Hex(Hash("coord")), Hex(Hash("filter")), Hex(Hash("runtime")),
    1, 1, 1, 1, "ae4-linux-fixed-fixture-v1", "1", Hex(Hash("install")), true);
foreach (var (msg, role) in new (int, byte)[] { (9, 0), (36, 1) }) {
    byte[] pre = memberOffsets[msg].Bytes;
    byte[] qualified = Replace(pre, 71, 1);
    File.WriteAllBytes(Path.Combine(output, $"legacy-{msg}.bin"), qualified);
    Record($"legacy/{msg}/qualified-match", "None", InstallationWire.CompareEcho(qualified, legacyIdentity, role).ToString(), Convert.ToHexString(qualified));
    Record($"legacy/{msg}/pre-refused", "False", InstallationWire.EchoShape(pre, role).ToString(), Convert.ToHexString(pre));
    Record($"legacy/{msg}/integrated-qualified-match", "None", InstallationWire.CompareEcho([..qualified, ..tail], legacyIdentity, role, true).ToString(), Convert.ToHexString(qualified));
    Record($"legacy/{msg}/integrated-pre-refused", "False", InstallationWire.EchoShape([..pre, ..tail], role, true).ToString(), Convert.ToHexString(pre));
}
byte[] pre37 = memberOffsets[37].Bytes;
byte[] qualified37 = Replace(pre37, 216, 1);
Record("legacy/37/qualified-match", "None", Invoke(() => {
    if (InstallationWire.ReadExpectation(qualified37, session) != legacyIdentity) throw new InvalidOperationException();
}), Convert.ToHexString(qualified37));
Record("legacy/37/pre-refused", "InvalidDataException", Invoke(() => InstallationWire.ReadExpectation(pre37, session)), Convert.ToHexString(pre37));
Record("legacy/10/equivalent-in-legacy-context", "None", Invoke(() => InstallationWire.AcceptResponse(memberOffsets[10].Bytes, legacyIdentity)), "message-10.bin");
Record("legacy/12/equivalent-in-legacy-context", "None", Invoke(() => InstallationWire.AcceptConfirmation(memberOffsets[12].Bytes, session, legacyIdentity)), "message-12.bin");

var w = Witness(true, false);
w.Remove("AuthorityHash");
w.Remove("AuthorityMode");
var wIdentity = (JsonObject)w["B2InstallationIdentity"]!;
wIdentity.Remove("qualificationId");
wIdentity.Remove("qualificationRecordSha256");
wIdentity["Qualified"] = true;
w["Version"] = 2;
w["EvidenceDirectory"] = Path.GetFullPath(Path.Combine(output, "evidence"));
var legacyBootstrap = Encoding.UTF8.GetBytes(w.ToJsonString());
File.WriteAllBytes(Path.Combine(output, "legacy-b2.json"), legacyBootstrap);
Record("legacy/b2-qualified-bootstrap", "None", Invoke(() => RecoveryConfiguration.Decode(legacyBootstrap)), "legacy-b2.json");
using (var exp = JsonDocument.Parse(Witness(true, true).ToJsonString()))
    Bootstrap("legacy/b2-qualified-refused-by-v3", legacyBootstrap, b2Context, exp.RootElement, capability, "Schema");
w["Version"] = 1;
var downgrade = Encoding.UTF8.GetBytes(w.ToJsonString());
File.WriteAllBytes(Path.Combine(output, "legacy-downgrade.json"), downgrade);
Record("legacy/qualified-to-v1", "InvalidDataException", Invoke(() => RecoveryConfiguration.Decode(downgrade)), "legacy-downgrade.json");
w.Remove("B2InstallationIdentity");
w.Remove("B2DeadlineNs");
var legacyV1 = Encoding.UTF8.GetBytes(w.ToJsonString());
File.WriteAllBytes(Path.Combine(output, "legacy-v1.json"), legacyV1);
Record("legacy/unqualified-v1-bootstrap", "None", Invoke(() => RecoveryConfiguration.Decode(legacyV1)), "legacy-v1.json");
using (var exp = JsonDocument.Parse(Witness(true, true).ToJsonString()))
    Bootstrap("legacy/v1-refused-by-v3", legacyV1, b2Context, exp.RootElement, capability, "Schema");
w["Version"] = 3;
Record("legacy/version-alone-refused", "InvalidDataException", Invoke(() => RecoveryConfiguration.Decode(Encoding.UTF8.GetBytes(w.ToJsonString()))), w.ToJsonString());

var qualifiedIntegrated = Witness(false, false);
qualifiedIntegrated.Remove("AuthorityMode");
qualifiedIntegrated.Remove("AuthorityHash");
qualifiedIntegrated["Version"] = 2;
qualifiedIntegrated["EvidenceDirectory"] = Path.GetFullPath(Path.Combine(output, "integrated-evidence"));
var integratedIdentity = At(qualifiedIntegrated, ["Integrated", "InstallationIdentity"]);
integratedIdentity.Remove("qualificationId");
integratedIdentity.Remove("qualificationRecordSha256");
integratedIdentity["Qualified"] = true;
byte[] qualifiedIntBytes = Encoding.UTF8.GetBytes(qualifiedIntegrated.ToJsonString());
File.WriteAllBytes(Path.Combine(output, "legacy-integrated.json"), qualifiedIntBytes);
Record("legacy/integrated-qualified-bootstrap", "None", Invoke(() => RecoveryConfiguration.Decode(qualifiedIntBytes)), "legacy-integrated.json");
using (var exp = JsonDocument.Parse(Witness(false, true).ToJsonString()))
    Bootstrap("legacy/integrated-qualified-refused-by-v3", qualifiedIntBytes, intContext, exp.RootElement, capability, "Schema");
var reserializedLegacy = JsonSerializer.SerializeToUtf8Bytes(RecoveryConfiguration.Decode(qualifiedIntBytes));
File.WriteAllBytes(Path.Combine(output, "legacy-integrated-reserialized.json"), reserializedLegacy);
using (var legacyDoc = JsonDocument.Parse(reserializedLegacy)) {
    bool forbiddenNulls = new[] { "B2InstallationIdentity", "B2DeadlineNs" }
        .All(field => legacyDoc.RootElement.GetProperty(field).ValueKind == JsonValueKind.Null);
    Record("BB-02/legacy-emits-both-null-members", "True", forbiddenNulls.ToString(), "legacy-integrated-reserialized.json");
}
Record("BB-02/legacy-accepts-its-null-members", "None",
    Invoke(() => RecoveryConfiguration.Decode(reserializedLegacy)), "legacy-integrated-reserialized.json");

var alias = Hash("install");
var aliased = new ValidationPreBinaryExpectations(alias);
Bin("binary-expectation-alias/baseline", memberOffsets[10].Bytes, b2Context, 10, true, aliased, "None");
byte[] replacement = Hash("replacement-install");
replacement.CopyTo(alias, 0);
Bin("binary-expectation-alias/replaced", [1, 0, 0, 0, ..replacement], b2Context, 10, true, aliased, "Comparison");

Console.WriteLine($"Independent assertions={checks} passed={checks - failures} counterexamples={failures}");
return failures == 0 ? 0 : 1;

void Bootstrap(string test, byte[] payload, ValidationPreContext context, JsonElement expected, byte[] digest, string want) {
    string actual;
    try {
        var code = ValidationPreCodec.DecodeBootstrap(payload, context, digest, expected, out var data);
        actual = code.ToString();
        if ((code == ValidationPreRefusal.None) != (data is not null)) actual = "invalid result publication: " + actual;
        else if (data is not null && (data.Topology != context.Topology || data.Fields.GetProperty("Version").GetInt32() != 3))
            actual = "invalid structural result";
    } catch (Exception ex) { actual = "throw " + ex.GetType().Name; }
    Record(test, want, actual, Hex(SHA256.HashData(payload)));
    if ((want == "refuse" ? actual == "None" || actual.StartsWith("throw ") : actual != want)) {
        string safe = string.Concat(test.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_'));
        File.WriteAllBytes(Path.Combine(output, safe + ".input"), payload);
        try {
            File.WriteAllText(Path.Combine(output, safe + ".expected.json"), expected.GetRawText());
        } catch (ObjectDisposedException) {
            File.WriteAllText(Path.Combine(output, safe + ".expected-lifecycle.txt"),
                "Dispose the JsonDocument parsed from this topology's projection.json before supplying its RootElement.");
        }
    }
}
void Bin(string test, byte[] payload, ValidationPreContext ctx, int msg, bool predecessor, ValidationPreBinaryExpectations? expectations, string want) {
    string outcome;
    try { outcome = ValidationPreCodecBinary.ValidatePayload(payload, new(ctx, (ValidationPreMessage)msg, predecessor), expectations).ToString(); }
    catch (Exception ex) { outcome = "throw " + ex.GetType().Name; }
    Record(test, want, outcome, Convert.ToHexString(payload));
}
void Record(string test, string expected, string actual, string vector) {
    bool matched = expected == "refuse" ? actual is not "None" && !actual.StartsWith("throw ") && !actual.StartsWith("invalid ") : expected == actual;
    checks++;
    if (!matched) { failures++; Console.WriteLine($"{test}: {expected} -> {actual}"); }
    results.WriteLine(JsonSerializer.Serialize(new { test, expected, actual, matched, vector }));
}
static string Invoke(Action action) {
    try { action(); return "None"; } catch (Exception ex) { return ex.GetType().Name; }
}
static string Id(int n) => $"7abb4591-d3c0-4012-a398-11447788{n:x4}";
static byte[] Hash(string text) => SHA256.HashData(Encoding.ASCII.GetBytes("Q1 independent " + text));
static string Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(bytes);
static JsonObject At(JsonObject obj, IEnumerable<string> path) {
    foreach (var key in path) obj = obj[key]!.AsObject();
    return obj;
}
static JsonNode Alternate(string rule, JsonNode? value) => rule switch {
    "Guid" => Id(777),
    "I64+" or "Lead" or "Pid" or "Principal" => 19,
    "Sha" => Hex(Hash("divergent")),
    "UpperSha" => Convert.ToHexString(Hash("divergent")),
    "Secret32" => Convert.ToBase64String(Hash("other-nonce")),
    "Path" => "/different/path",
    "RuntimeVersion" => "10.0.999",
    "ValidId" => "Other.id",
    "PidNs" => "pid:[7]",
    "UserNs" => "user:[7]",
    "Tool" or "Input" or "Coordinator" or "Filter" => "different-basename",
    "Budget" => 16777215,
    "Four" => 5,
    _ => throw new InvalidDataException(rule + " has unexpected expectation field: " + value)
};
static IEnumerable<JsonNode> Invalid(string rule) {
    object[] cases = rule switch {
        "V3" => [1, 2, 4, "3"],
        "One" => [0, 2, "1"],
        "Four" => [3, 5, "4"],
        "Budget" => [16_777_215, 16_777_217, "16777216"],
        "Pid" or "Principal" => [0, -1, 2147483648L, "2"],
        "I64+" => [0, -1, 9223372036854775808UL, "2"],
        "Lead" => [0, -1, 120000000001L, "1"],
        "Guid" => [Guid.Empty.ToString(), Id(1).ToUpperInvariant(), Id(1).Replace("-", ""), " " + Id(1)],
        "Secret32" => [new string('B', 43) + "=", new string('A', 43), new string('_', 43) + "=", " " + new string('A', 42) + "="],
        "Path" => ["q1", "/", "/q1//x", "/q1/..", "/q1/.", "/q1/", "/q1/x\\y", "/q1/ ", "/q1/é", "/" + new string('x', 1024)],
        "Sha" => [new string('0', 64), new string('a', 63), new string('A', 64), new string('g', 64)],
        "UpperSha" => [new string('0', 64), new string('A', 65), new string('a', 64), new string('G', 64)],
        "CapabilityHex" => [new string('A', 64), new string('a', 65), new string('g', 64)],
        "PidNs" => ["pid:[0]", "pid:[01]", "pid:[18446744073709551616]", "user:[1]", "pid:[]"],
        "UserNs" => ["user:[0]", "user:[01]", "user:[18446744073709551616]", "pid:[1]", "user:[]"],
        "RuntimeVersion" => ["10.0.01", "10.0.2147483648", "10.0.1-rc", "10.0", "9.0.1"],
        "ValidId" => ["", ".x", "x.", "a/b", "a b", new string('a', 129)],
        "False" => [true, 0, "false"],
        "Null" => [false, 0, "null"],
        "ProfileId" or "ProfileVersion" or "PreMode" or "Tool" or "Input" or "Coordinator" or "Filter" => ["", "x"],
        _ => []
    };
    foreach (var item in cases) yield return JsonSerializer.SerializeToNode(item)!;
}
JsonObject Witness(bool b2, bool projection) {
    var install = Hex(Hash("install"));
    var directory = "/opt/gagamba/installations/" + install;
    var ownerDll = core + "Owner.dll";
    var ownerHash = Hex(Hash("Owner.1"));
    string suffix = "/va-" + Id(1).Replace("-", "");
    var identity = new JsonObject {
        ["Generation"] = install, ["ArtifactSetSha256"] = install, ["OwnerClosureSha256"] = Hex(Hash("owner")),
        ["RecoveryClosureSha256"] = Hex(Hash("recovery")), ["CoordinatorSha256"] = Hex(Hash("coord")),
        ["SeccompFilterSha256"] = Hex(Hash("filter")), ["RuntimeInventorySha256"] = Hex(Hash("runtime"))
    };
    if (!projection) {
        identity["ManifestSchema"] = 1; identity["OwnerProtocol"] = 1; identity["CoordinatorProtocol"] = 1; identity["Bootstrap"] = 1;
        identity["ProfileId"] = "ae4-linux-fixed-fixture-v1"; identity["ProfileVersion"] = "1";
        identity["Qualified"] = false; identity["qualificationId"] = null; identity["qualificationRecordSha256"] = null;
    }
    JsonObject Pin(string role) => new() {
        ["Directory"] = directory + "/" + role.ToLowerInvariant(),
        ["Sha256"] = new JsonObject {
            [core + role + ".dll"] = Hex(Hash(role + ".1")), [core + role + ".deps.json"] = Hex(Hash(role + ".2")),
            [core + role + ".runtimeconfig.json"] = Hex(Hash(role + ".3")), [core + "dll"] = Hex(Hash(role + ".4")),
            ["Gagamba.Execution.dll"] = Hex(Hash(role + ".5"))
        }
    };
    var root = new JsonObject {
        ["Session"] = Id(3), ["Execution"] = Id(1), ["EvidenceDirectory"] = "/q1/evidence",
        ["ControllerUid"] = 72, ["ControllerPid"] = 47017, ["Generation"] = Id(2),
        ["Secret"] = Convert.ToBase64String(Hash("nonce")), ["Recovery"] = Pin("Recovery"), ["Owner"] = Pin("Owner")
    };
    if (!projection) {
        root["Version"] = 3; root["AuthorityHash"] = Hex(capability); root["AuthorityMode"] = "validation-pre";
    }
    if (b2) {
        if (!projection) { root["DelegatedParent"] = null; root["Integrated"] = null; }
        root["B2DeadlineNs"] = 302; root["B2InstallationIdentity"] = identity;
        return root;
    }
    var profile = new JsonObject {
        ["OwnerDllPath"] = directory + "/owner/" + ownerDll,
        ["DelegatedParentPath"] = "/q1/delegated", ["SourceRootPath"] = "/opt/gagamba/validation-fixtures/" + Hex(Hash("fixture")),
        ["StagingParentPath"] = "/q1/staging" + suffix, ["JournalDirectory"] = "/q1/journal" + suffix, ["OwnerDllSha256"] = ownerHash,
        ["ToolSha256"] = Hex(Hash("tool")), ["InputSha256"] = Hex(Hash("input")), ["CoordinatorSha256"] = Hex(Hash("coord")),
        ["ToolEnrollmentId"] = "Q1.Tool-001", ["InputEnrollmentId"] = "Q1.Input_002", ["ExpiresLeadNanoseconds"] = 120000000000L
    };
    profile["ToolSourceName"] = "tool"; profile["InputSourceName"] = "input"; profile["CoordinatorSourceName"] = "coordinator";
    profile["FilterSourceName"] = "filter"; profile["TotalBudgetBytes"] = 16777216; profile["MaxObjects"] = 4;
    if (!projection) profile["IntegratedRecovery"] = null;
    root["DelegatedParent"] = "/q1/delegated";
    root["Integrated"] = new JsonObject {
        ["Profile"] = profile, ["Issuer"] = Id(4), ["StoreGeneration"] = Id(5), ["AcceptedAtNs"] = 21, ["ExpiresAtNs"] = 22,
        ["Controller"] = new JsonObject { ["Pid"] = 47017, ["Uid"] = 72, ["Gid"] = 98, ["PidNamespace"] = "pid:[18446744073709551615]", ["UserNamespace"] = "user:[1]" },
        ["Runtime"] = new JsonObject {
            ["HostPath"] = "/usr/lib/dotnet/dotnet", ["HostSha256"] = Hex(Hash("host")),
            ["RuntimeDirectory"] = "/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.17",
            ["RuntimeInventorySha256"] = Convert.ToHexString(Hash("runtime")),
            ["HostFxrDirectory"] = "/usr/lib/dotnet/host/fxr/10.0.17",
            ["HostFxrInventorySha256"] = Convert.ToHexString(Hash("host-inventory")), ["RuntimeVersion"] = "10.0.17"
        },
        ["InstallationIdentity"] = identity
    };
    return root;
}
static byte[] PackProfile() {
    byte[] id = Encoding.UTF8.GetBytes("ae4-linux-fixed-fixture-v1");
    byte[] version = Encoding.UTF8.GetBytes("1");
    return [(byte)id.Length, ..id, (byte)version.Length, ..version];
}
static byte[] Echo(byte role) {
    byte[] payload = new byte[76];
    Hash("install").CopyTo(payload, 0);
    Hash(role == 0 ? "recovery" : "owner").CopyTo(payload, 32);
    foreach (int offset in new[] { 64, 66, 68 }) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset), 1);
    payload[70] = role;
    return payload;
}
byte[] Pack37() {
    byte[] payload = new byte[246];
    session.TryWriteBytes(payload);
    string[] digests = ["install", "owner", "recovery", "coord", "filter", "runtime"];
    for (int field = 0; field < digests.Length; field++) Hash(digests[field]).CopyTo(payload, 16 + 32 * field);
    foreach (int offset in new[] { 208, 210, 212, 214 }) BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(offset), 1);
    PackProfile().CopyTo(payload, 217);
    return payload;
}
static byte[] Change(byte[] bytes, int at, byte mask) => Replace(bytes, at, (byte)(bytes[at] ^ mask));
static byte[] Replace(byte[] bytes, int at, byte value) {
    var copy = bytes.ToArray();
    copy[at] = value;
    return copy;
}
