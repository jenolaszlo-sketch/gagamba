using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Tests.Bootstrap;

var routes = new (byte Type, byte Sender, byte Recipient)[]
    { (4, 2, 3), (4, 3, 4), (5, 3, 2), (5, 4, 3), (6, 2, 4), (7, 4, 2) };
var guidWire = Convert.FromHexString("33221100554477668899aabbccddeeffd4c3b2a1f6e508170923456789abcdef403020106050807090a0b0c0d0e0f001");
var ids = new Gava1Ids(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
    Guid.Parse("a1b2c3d4-e5f6-1708-0923-456789abcdef"), Guid.Parse("10203040-5060-7080-90a0-b0c0d0e0f001"));
var capability = SHA256.HashData("E-4 Q1 synthetic capability"u8);
var catalog = SHA256.HashData("E-4 Q1 synthetic catalog"u8);
var original = new Gava1Originals(ids, capability, catalog);
var failures = 0;
var passed = 0;
Console.WriteLine("rule\tcase\texpected\tobserved\toutput\tstatus\tinput-sha256");

void Check(string rule, string label, string expected, string actual, bool output, bool ok, byte[]? bytes = null)
{
    Console.WriteLine($"{rule}\t{label}\t{expected}\t{actual}\t{output}\t{(ok ? "PASS" : "FAIL")}\t{(bytes is null ? "-" : Convert.ToHexString(SHA256.HashData(bytes)))}");
    if (ok) passed++; else failures++;
}

static byte[] Frame(byte type, byte sender, byte[] wireIds, byte[] cap, byte[] cat)
{
    byte[] length = type is 4 or 6 ? [64, 0, 0, 0] : [32, 0, 0, 0];
    byte[] header = [0x47, 0x41, 0x56, 0x41, 1, 0, type, sender, .. length, 1, 0, 0, 0, .. wireIds];
    return type is 4 or 6 ? [.. header, .. cap, .. cat] : [.. header, .. cap];
}

static Gava1Expectation Expect(byte t, byte s, byte r, Gava1Originals o) =>
    new((Gava1Message)t, (Gava1Role)s, (Gava1Role)r, 1, o, true, true, true, t != 4);

void Decode(string rule, string label, byte[] frame, Gava1Expectation expected, Gava1Refusal want)
{
    try
    {
        var actual = Gava1ContextCodec.Decode(frame, expected, out var data);
        var output = data is not null;
        var valueOk = want == Gava1Refusal.None
            ? data is not null && data.Message == expected.Message && data.Sender == expected.Sender &&
              data.Recipient == expected.Recipient && data.Originals.Matches(expected.Originals!)
            : !output;
        Check(rule, label, want.ToString(), actual.ToString(), output, actual == want && valueOk, frame);
    }
    catch (Exception ex) { Check(rule, label, want.ToString(), ex.GetType().Name, false, false, frame); }
}

foreach (var (t, s, r) in routes)
{
    var route = $"{t}-{s}-{r}";
    var frame = Frame(t, s, guidWire, capability, catalog);
    var exp = Expect(t, s, r, original);
    Decode("G01-G05", $"{route}/exact", frame, exp, Gava1Refusal.None);
    foreach (int n in Enumerable.Range(0, frame.Length))
        Decode("G02", $"{route}/prefix-{n}", frame[..n], exp, Gava1Refusal.Framing);
    foreach (int n in new[] { frame.Length + 1, 192, 193, 256 })
        Decode("G02", $"{route}/record-length-{n}", [.. frame, .. new byte[n - frame.Length]], exp, Gava1Refusal.Framing);
    foreach (int n in new[] { 128, 129 })
    {
        var full = new byte[64 + n];
        frame.CopyTo(full, 0);
        full[8] = (byte)n;
        Decode("G02", $"{route}/complete-oversize-payload-{n}", full, exp, Gava1Refusal.Framing);
    }
    if (t is 4 or 6)
        Decode("G03-G04", $"{route}/swapped-digest-fields", Frame(t, s, guidWire, catalog, capability), exp, Gava1Refusal.Comparison);
    else
        Decode("G03-G04", $"{route}/ack-with-catalog", [.. frame, .. catalog], exp, Gava1Refusal.Framing);
    foreach (var hex in new[] { "00000000", "20000000", "40000000", "80000000", "81000000", "ffffffff", "00000020", "00000040" })
    {
        if (hex == Convert.ToHexString(frame.AsSpan(8, 4)).ToLowerInvariant()) continue;
        var changed = frame.ToArray();
        Convert.FromHexString(hex).CopyTo(changed, 8);
        Decode("G01-G02", $"{route}/declared-{hex}", changed, exp, Gava1Refusal.Framing);
    }
    foreach (var (offset, width, rule, refusal) in new[] { (0, 4, "G01", Gava1Refusal.Framing),
        (4, 2, "G01", Gava1Refusal.Framing), (6, 2, "G03-G05", Gava1Refusal.Comparison),
        (12, 4, "G05", Gava1Refusal.Comparison), (16, frame.Length - 16, "G01-G05", Gava1Refusal.Comparison) })
        foreach (int i in Enumerable.Range(offset, width))
        {
            var changed = frame.ToArray();
            changed[i] ^= 0x01;
            Decode(rule, $"{route}/bit-at-{i}", changed, exp, refusal);
        }
    foreach (int offset in new[] { 16, 32, 48 })
    {
        var empty = frame.ToArray();
        empty.AsSpan(offset, 16).Clear();
        Decode("G01", $"{route}/empty-guid-{offset}", empty, exp, Gava1Refusal.Comparison);
        var otherOrder = frame.ToArray();
        Array.Reverse(otherOrder, offset, 4);
        Decode("G01", $"{route}/alternate-guid-order-{offset}", otherOrder, exp, Gava1Refusal.Comparison);
    }
    foreach (var (name, bad) in new (string, Gava1Expectation)[] {
        ("default", default), ("missing-original", exp with { Originals = null }),
        ("spent", exp with { Pending = false }), ("wrong-association", exp with { OriginalAssociationMatches = false }),
        ("invalid-ancillary", exp with { AncillaryMatches = false }), ("sequence-2", exp with { Sequence = 2 }),
        ("sequence-max", exp with { Sequence = uint.MaxValue }) })
        Decode("G05-G07/G09", $"{route}/{name}", frame, bad, Gava1Refusal.Context);
    foreach (int width in new[] { 0, 31, 33 })
    {
        Decode("G03-G05/G09", $"{route}/capability-width-{width}", frame,
            exp with { Originals = new(ids, new byte[width], catalog) }, Gava1Refusal.Context);
        Decode("G03-G05/G09", $"{route}/catalog-width-{width}", frame,
            exp with { Originals = new(ids, capability, new byte[width]) }, Gava1Refusal.Context);
    }
    if (t != 4)
        Decode("G03-G05", $"{route}/missing-prior-context", frame,
            exp with { PriorContextEstablished = false }, Gava1Refusal.Context);
    else
        Decode("G03/G05", $"{route}/also-with-prior-context", frame,
            exp with { PriorContextEstablished = true }, Gava1Refusal.None);
    var capSource = capability.ToArray();
    var catSource = catalog.ToArray();
    var snapshot = new Gava1Originals(ids, capSource, catSource);
    Array.Clear(capSource);
    Array.Clear(catSource);
    var retained = Gava1ContextCodec.Decode(frame, exp with { Originals = snapshot }, out var data);
    var wireHash = SHA256.HashData(frame);
    Array.Clear(frame);
    if (data is not null)
    {
        Array.Clear(data.Originals.CapabilityDigest);
        Array.Clear(data.Originals.CatalogDigest);
    }
    var owned = data is not null && data.Originals.CapabilityDigest.SequenceEqual(capability) &&
        data.Originals.CatalogDigest.SequenceEqual(catalog) && data.Originals.Ids == ids;
    Check("G09/V03", $"{route}/snapshot-{Convert.ToHexString(wireHash)}", "retained exact values", retained.ToString(), data is not null, owned);
}

foreach (byte t in new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 255 })
    foreach (byte s in new byte[] { 0, 1, 2, 3, 4, 255 })
        foreach (byte r in new byte[] { 0, 1, 2, 3, 4, 255 })
            Decode("G03-G05", $"route/{t}-{s}-{r}", Frame(t, s, guidWire, capability, catalog),
                Expect(t, s, r, original), routes.Contains((t, s, r)) ? Gava1Refusal.None : Gava1Refusal.Context);

var witnessWire = Convert.FromHexString("000000000000000000000000000000030000000000000000000000000000000100000000000000000000000000000002");
var witnessIds = new Gava1Ids(Guid.Parse("00000000-0000-0000-0000-000000000003"),
    Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000002"));
var witnessCap = SHA256.HashData("abc"u8);
byte[] release = Convert.FromHexString("3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e");
foreach (int t in new[] { 1, 2 })
{
    var payload = ValidationPreCodecVectors.Payload(t);
    var project = ValidationPreCodecVectors.Projection(t);
    var e3 = new ValidationPreContext(3, (ValidationPreTopology)t);
    var parsed = ValidationPreCodec.DecodeBootstrap(payload, e3, witnessCap, project, out var decoded);
    Check("G08/E3-3", $"handoff/{t}/E3", "None", parsed.ToString(), decoded is not null, parsed == ValidationPreRefusal.None && decoded is not null, payload);
    if (decoded is null) continue;
    var p = decoded.Fields;
    var boot = new Gava1PreBootstrap(p.GetProperty("Version").GetUInt16(), (Gava1PreTopology)decoded.Topology,
        p.GetProperty("AuthorityMode").GetString(), new(p.GetProperty("Session").GetGuid(), p.GetProperty("Execution").GetGuid(),
            p.GetProperty("Generation").GetGuid()), Convert.FromHexString(p.GetProperty("AuthorityHash").GetString()!));
    var topology = (Gava1PreTopology)t;
    var witness = new Gava1Originals(witnessIds, witnessCap, catalog);
    var init = Frame(4, 3, witnessWire, witnessCap, catalog);
    var decodedInit = Gava1ContextCodec.Decode(init, Expect(4, 3, 4, witness), out var context);
    Check("G03/G08", $"handoff/{t}/INIT", "None", decodedInit.ToString(), context is not null,
        decodedInit == Gava1Refusal.None && context is not null, init);

    void Handoff(string label, Gava1PreBootstrap a, Gava1ContextData? b, Gava1PreTopology c, Gava1Originals? d, Gava1Refusal want)
    {
        try
        {
            var observed = Gava1ContextCodec.MatchPreBootstrap(a, b, c, d, out var output);
            var owned = want == Gava1Refusal.None ? output is not null && output.Matches(witness) : output is null;
            Check("G08-G10/E3-3-5", $"handoff/{t}/{label}", want.ToString(), observed.ToString(), output is not null, observed == want && owned);
        }
        catch (Exception ex) { Check("G08-G10", $"handoff/{t}/{label}", want.ToString(), ex.GetType().Name, false, false); }
    }

    Handoff("exact", boot, context, topology, witness, Gava1Refusal.None);
    Handoff("default", default, context, topology, witness, Gava1Refusal.Context);
    Handoff("missing-context", boot, null, topology, witness, Gava1Refusal.Context);
    Handoff("missing-original", boot, context, topology, null, Gava1Refusal.Context);
    Handoff("wrong-topology", boot, context, (Gava1PreTopology)(3 - t), witness, Gava1Refusal.Context);
    Handoff("wrong-version", boot with { Version = 2 }, context, topology, witness, Gava1Refusal.Context);
    foreach (string? phase in new[] { null, "Validation-Pre", "qualified", "validation-post" })
        Handoff($"phase-{phase ?? "missing"}", boot with { Mode = phase }, context, topology, witness, Gava1Refusal.Context);
    Handoff("short-capability", boot with { Capability = witnessCap.AsMemory(0, 31) }, context, topology, witness, Gava1Refusal.Context);
    Handoff("session", boot with { Ids = witnessIds with { Session = ids.Session } }, context, topology, witness, Gava1Refusal.Comparison);
    Handoff("execution", boot with { Ids = witnessIds with { Execution = ids.Execution } }, context, topology, witness, Gava1Refusal.Comparison);
    Handoff("lease", boot with { Ids = witnessIds with { Lease = ids.Lease } }, context, topology, witness, Gava1Refusal.Comparison);
    var installation = t == 1 ? p.GetProperty("B2InstallationIdentity") : p.GetProperty("Integrated").GetProperty("InstallationIdentity");
    var installationGeneration = Convert.FromHexString(installation.GetProperty("Generation").GetString()!);
    Handoff("installation-generation-as-lease", boot with { Ids = witnessIds with { Lease = new Guid(installationGeneration.AsSpan(0, 16)) } },
        context, topology, witness, Gava1Refusal.Comparison);
    if (t == 2)
    {
        var storeGeneration = p.GetProperty("Integrated").GetProperty("StoreGeneration").GetGuid();
        Handoff("store-generation-as-lease", boot with { Ids = witnessIds with { Lease = storeGeneration } },
            context, topology, witness, Gava1Refusal.Comparison);
    }
    Handoff("release-as-capability", boot with { Capability = release }, context, topology, witness, Gava1Refusal.Comparison);
    Handoff("release-as-catalog", boot, context, topology, new(witnessIds, witnessCap, release), Gava1Refusal.Comparison);
    Gava1ContextCodec.Decode(Frame(7, 4, witnessWire, witnessCap, catalog), Expect(7, 4, 2, witness), out var ack);
    Handoff("ack-as-handoff-context", boot, ack, topology, witness, Gava1Refusal.Context);
    Gava1ContextCodec.Decode(Frame(6, 2, witnessWire, witnessCap, catalog), Expect(6, 2, 4, witness), out var direct);
    Handoff("direct-context", boot, direct, topology, witness, Gava1Refusal.None);
    Gava1ContextCodec.Decode(Frame(4, 3, witnessWire, release, catalog), Expect(4, 3, 4, new(witnessIds, release, catalog)), out var agreeing);
    Handoff("agreeing-wrong-inputs", boot with { Capability = release }, agreeing, topology, witness, Gava1Refusal.Comparison);
    var fail = ValidationPreCodec.DecodeBootstrap(payload, e3, release, project, out var rejected);
    Check("G08/E3-3", $"handoff/{t}/independent-digest", "refusal", fail.ToString(), rejected is not null,
        fail != ValidationPreRefusal.None && rejected is null, payload);
}

var sensitivity = Frame(4, 2, guidWire, capability, catalog);
sensitivity[127] ^= 0x80;
var sensitivityExpectation = Expect(4, 2, 3, original);
Decode("G03/I02", "sensitivity/exact-core", sensitivity, sensitivityExpectation, Gava1Refusal.Comparison);
var mutant = Gava1SensitivityCopy.Decode(sensitivity, sensitivityExpectation, out var mutantData);
Check("G03/I02", "sensitivity/disposable-omitted-catalog-check", "detectable acceptance", mutant.ToString(), mutantData is not null,
    mutant == Gava1Refusal.None && mutantData is not null, sensitivity);
Console.Error.WriteLine($"supplemental rows passed={passed} failed={failures}");
return failures == 0 ? 0 : 1;
