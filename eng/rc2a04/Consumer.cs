using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Restricted.Contracts;

// Ordinary package consumer: no reflection, internal types or source/project references.
int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
void Argument(Action action, string parameter)
{
    try { action(); throw new Exception("Expected argument refusal: " + parameter); }
    catch (ArgumentException ex) { Check(ex.ParamName == parameter, "argument name " + parameter); }
}
using var json = JsonDocument.Parse(File.ReadAllText(args[0]));
int vectors = 0;
var omitted = new List<string>();
foreach (var vector in json.RootElement.GetProperty("vectors").EnumerateArray())
{
    string id = vector.GetProperty("id").GetString()!;
    if (!vector.TryGetProperty("hex", out var hex)) { omitted.Add(id); continue; }
    byte[] bytes = Convert.FromHexString(hex.GetString()!);
    byte[] digest = Convert.FromHexString(vector.GetProperty("sha256").GetString()!);
    Check(bytes.Length == vector.GetProperty("bytes").GetInt32(), id + " length");
    Check(SHA256.HashData(bytes).SequenceEqual(digest), id + " independent digest");
    var result = RestrictedManifestCodec.Decode(bytes);
    Check(result.Value is not null && result.Error is null, id + " decode");
    var view = result.Value!;
    Check(view.SchemaVersion == 1 && view.ImplementationId == "linux-m1-native-transaction", id + " identity");
    Check(RestrictedManifestCodec.Encode(view).SequenceEqual(bytes), id + " canonical roundtrip");
    Check(RestrictedManifestCodec.ComputeSha256(view).SequenceEqual(digest), id + " compute");
    Check(RestrictedManifestCodec.VerifySha256(view, digest), id + " matching digest");
    byte[] different = digest.ToArray(); different[0] ^= 1;
    Check(!RestrictedManifestCodec.VerifySha256(view, different), id + " differing digest");
    Check(!RestrictedManifestCodec.VerifySha256(view, new byte[32]), id + " zero is not wildcard");
    foreach (int length in new[] { 0, 31, 33 })
        Check(!RestrictedManifestCodec.VerifySha256(view, new byte[length]), id + " digest length " + length);
    bytes[0] ^= 1;
    Check(RestrictedManifestCodec.ComputeSha256(view).SequenceEqual(digest), id + " decode copies input");
    var encoded = RestrictedManifestCodec.Encode(view); encoded[0] ^= 1;
    Check(RestrictedManifestCodec.ComputeSha256(view).SequenceEqual(digest), id + " encode output independent");
    var digestCopy = RestrictedManifestCodec.ComputeSha256(view).ToArray(); digestCopy[0] ^= 1;
    Check(RestrictedManifestCodec.VerifySha256(view, digest), id + " digest output copy");
    if (id == "Q2A-V-01-golden")
    {
        Check(view.Authority.Namespaces.Length == 6 && view.Authority.NoNewPrivs, "nested authority");
        Check(view.Authority.WritableStores.Length == 2 && view.Authority.MountPolicy.ToolExecutable, "nested stores/mount");
        Check(view.Resources.Memory.Unit == "bytes" && view.Resources.CoreDumps.CoreDisabled, "nested resources");
        Check(view.Resources.OwnerAllocation.StagingBytes > 0 && view.Closure.Objects.Length == 3, "nested allocation/closure");
        Check(view.Invocation.Mode == "work" && view.Invocation.Argv.Length == 2 && view.Channels.StdinEof, "nested invocation/channels");
    }
    vectors++;
}
byte[] golden = Convert.FromHexString(json.RootElement.GetProperty("golden").GetProperty("hex").GetString()!);
for (int length = 0; length < golden.Length; length++)
{
    var refusal = RestrictedManifestCodec.Decode(golden.AsSpan(0, length));
    Check(refusal.Value is null && refusal.Error is not null, "truncated prefix " + length);
}
var trailing = RestrictedManifestCodec.Decode(golden.Concat(new byte[] { 0 }).ToArray());
Check(trailing.Value is null && trailing.Error is not null, "trailing data");
var corrupt = golden.ToArray(); corrupt[0] = 2;
Check(RestrictedManifestCodec.Decode(corrupt).Value is null, "unsupported schema refusal");
corrupt = golden.ToArray(); corrupt[6] = (byte)'X';
Check(RestrictedManifestCodec.Decode(corrupt).Value is null, "unknown implementation refusal");
Check(RestrictedManifestCodec.MaxCanonicalBytes == 65536, "public size constant");
var large = RestrictedManifestCodec.Decode(new byte[65537]);
Check(large.Error == ManifestDecodeError.InputTooLarge && large.Offset == 0 && large.Value is null, "over boundary");
Check(RestrictedManifestCodec.Decode(new byte[65536]).Error != ManifestDecodeError.InputTooLarge, "inclusive admission, invalid payload");
Check(RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction"), "Supports exact");
foreach (ushort schema in new ushort[] { 0, 2, ushort.MaxValue })
    Check(!RestrictedManifestCodec.Supports(schema, "linux-m1-native-transaction"), "unsupported schema");
foreach (string id in new[] { "", " ", "unknown", "Linux-m1-native-transaction", "linux-m1-native-transaction " })
    Check(!RestrictedManifestCodec.Supports(1, id), "unsupported id");
Argument(() => RestrictedManifestCodec.Supports(0, null!), "implementationId");
Argument(() => RestrictedManifestCodec.Encode(null!), "manifest");
Argument(() => RestrictedManifestCodec.VerifySha256(null!, new byte[31]), "manifest");
Guid[] ids = Enumerable.Range(1, 4).Select(x => new Guid(x, 0, 0, new byte[8])).ToArray();
byte[] hash = SHA256.HashData(golden);
RestrictedPreparationReference Make(byte[] value) => new(ids[0], ids[1], ids[2], ids[3], value);
var reference = Make(hash);
var same = Make(hash.ToArray());
Check(reference.ExecutionId == ids[0] && reference.IssuerId == ids[1] && reference.GenerationId == ids[2] && reference.PreparationId == ids[3], "reference IDs");
Check(reference.Equals(same) && reference.Equals((object)same) && reference.GetHashCode() == same.GetHashCode(), "content equality/hash within process");
hash[0] ^= 1;
Check(reference.Equals(same) && !reference.Equals(Make(hash)), "constructor copy/digest inequality");
var copied = reference.ManifestSha256.ToArray(); copied[0] ^= 1;
Check(reference.Equals(same), "reference output copy");
for (int index = 0; index < ids.Length; index++)
{
    var changed = ids.ToArray(); changed[index] = Guid.NewGuid();
    Check(!reference.Equals(new RestrictedPreparationReference(changed[0], changed[1], changed[2], changed[3], reference.ManifestSha256.AsSpan())), "ID inequality " + index);
    changed[index] = Guid.Empty;
    Argument(() => new RestrictedPreparationReference(changed[0], changed[1], changed[2], changed[3], hash), new[] { "executionId", "issuerId", "generationId", "preparationId" }[index]);
}
foreach (int length in new[] { 0, 31, 33 }) Argument(() => Make(new byte[length]), "manifestSha256");
Check(!((object)reference).Equals(null) && !reference.Equals(new object()), "reference null/type inequality");
Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", checks, literalVectors = vectors, recipeOnlyNotMaterialized = omitted, framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription }));
