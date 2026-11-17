using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Validation;
using Binding = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyBinding;
using Evidence = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyEvidence;
using Refusal = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyRefusal;

internal static partial class Probe
{
    private static string Hex(ReadOnlySpan<byte> b) => Convert.ToHexStringLower(b);
    private static string Hash(byte[] b) => Hex(SHA256.HashData(b));

    private static void Composition(string[] templates)
    {
        Check(templates.Length == 2, "supply the two frozen bootstrap grammar templates");
        for (int topology = 1; topology <= 2; topology++)
        {
            string template = File.ReadAllText(templates[topology - 1]);
            var expected = Witness(topology);
            var original = Witness(topology).Bind();

            Case($"R16/{topology}/composed", () =>
            {
                var observed = Handoff(template, topology, Witness(topology));
                using var model = Create(expected, observed);
            });
            foreach (string changed in new[] { "catalog", "capability", "session", "execution", "lease" })
            {
                Case($"R16/{topology}/matching-suspects/{changed}", () =>
                {
                    var suspect = Witness(topology);
                    suspect = changed switch
                    {
                        "session" => suspect with { Ids = suspect.Ids with { Session = Label(322) } },
                        "execution" => suspect with { Ids = suspect.Ids with { Execution = Label(323) } },
                        "lease" => suspect with { Ids = suspect.Ids with { Lease = Label(324) } },
                        _ => suspect
                    };
                    if (changed is "catalog" or "capability")
                        suspect.Bytes[changed == "catalog" ? 7 : 14] = Digest("wire alternate " + changed);
                    AgainstOriginal(expected, original, Handoff(template, topology, suspect));
                });
            }

            var body = BootstrapText(template, expected);
            var projection = JsonSerializer.Deserialize<JsonElement>(body);
            var e3Failures = new (string, string, byte[], ValidationPreRefusal)[]
            {
                ("duplicate-decoded-name", body.Insert(1, "\"Vers\\u0069on\":3,"), expected.Bytes[14]!, ValidationPreRefusal.Schema),
                ("lone-surrogate-key", body.Insert(1, "\"\\ud800\":3,"), expected.Bytes[14]!, ValidationPreRefusal.Schema),
                ("escaped-digest-token", body.Replace(Hex(expected.Bytes[14]!),
                    $"\\u{(int)Hex(expected.Bytes[14]!)[0]:x4}" + Hex(expected.Bytes[14]!)[1..], StringComparison.Ordinal),
                    expected.Bytes[14]!, ValidationPreRefusal.Schema),
                ("truncated-release-witness", body, Digest("unrelated release witness")[..31], ValidationPreRefusal.Expectation),
                ("32-byte-release-witness", body, Digest("unrelated release witness"), ValidationPreRefusal.Comparison)
            };
            foreach (var (field, badBody, cap, code) in e3Failures)
            {
                Case($"R16/{topology}/E3/{field}", () =>
                {
                    var actual = ValidationPreCodec.DecodeBootstrap(Encoding.UTF8.GetBytes(badBody), expected.Pre,
                        cap, projection, out var result);
                    Check(actual == code && result is null, $"E-3 expected {code}/null got {actual}/{result is not null}");
                });
            }

            var (wire, context) = Frame(topology, expected);
            foreach (var (name, modified, exp, code) in new[]
            {
                ("lost-original", wire, context with { OriginalAssociationMatches = false }, Gava1Refusal.Context),
                ("unsupported-sequence", wire, context with { Sequence = 2 }, Gava1Refusal.Context),
                ("truncation", wire[..127], context, Gava1Refusal.Framing),
                ("trailing", wire.Concat(new byte[] { 0 }).ToArray(), context, Gava1Refusal.Framing),
                ("swapped-digests", wire[..64].Concat(wire[96..]).Concat(wire[64..96]).ToArray(), context, Gava1Refusal.Comparison),
                ("wrong-catalog", wire, context with { Originals = new(expected.Ids, expected.Bytes[14]!, Digest("catalog alternate")) }, Gava1Refusal.Comparison),
            })
            {
                Case($"R16/{topology}/E4/{name}", () =>
                {
                    var actual = Gava1ContextCodec.Decode(modified, exp, out var result);
                    Check(actual == code && result is null, $"E-4 expected {code}/null got {actual}/{result is not null}");
                });
            }
        }
        DetachedChain();
    }

    private static string BootstrapText(string template, Vector vector) =>
        template.Replace("00000000-0000-0000-0000-000000000001", vector.Ids.Execution.ToString("D"), StringComparison.Ordinal)
                .Replace("va-00000000000000000000000000000001", "va-" + vector.Ids.Execution.ToString("N"), StringComparison.Ordinal)
                .Replace("00000000-0000-0000-0000-000000000002", vector.Ids.Lease.ToString("D"), StringComparison.Ordinal)
                .Replace("00000000-0000-0000-0000-000000000003", vector.Ids.Session.ToString("D"), StringComparison.Ordinal)
                .Replace("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                    Hex(vector.Bytes[14]!), StringComparison.Ordinal);

    private static (byte[] Frame, Gava1Expectation Expectation) Frame(int topology, Vector vector)
    {
        var frame = new byte[128];
        "GAVA"u8.CopyTo(frame);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4), 1);
        var message = topology == 1 ? Gava1Message.Init : Gava1Message.DirectContext;
        var recipient = topology == 1 ? Gava1Role.Recovery : Gava1Role.Owner;
        frame[6] = (byte)message;
        frame[7] = (byte)Gava1Role.Controller;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(8), 64);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(12), 1);
        vector.Ids.Session.TryWriteBytes(frame.AsSpan(16, 16));
        vector.Ids.Execution.TryWriteBytes(frame.AsSpan(32, 16));
        vector.Ids.Lease.TryWriteBytes(frame.AsSpan(48, 16));
        vector.Bytes[14]!.CopyTo(frame, 64);
        vector.Bytes[7]!.CopyTo(frame, 96);
        return (frame, new(message, Gava1Role.Controller, recipient, 1,
            new(vector.Ids, vector.Bytes[14]!, vector.Bytes[7]!), true, true, true, true));
    }

    private static Binding Handoff(string template, int topology, Vector vector)
    {
        var projection = JsonSerializer.Deserialize<JsonElement>(BootstrapText(template, vector));
        var payload = Encoding.UTF8.GetBytes(BootstrapText(template, vector));
        var e3 = ValidationPreCodec.DecodeBootstrap(payload, vector.Pre, vector.Bytes[14]!, projection, out var parsed);
        Check(e3 == ValidationPreRefusal.None && parsed is not null, $"E-3 handoff got {e3}");
        var fields = parsed!.Fields;
        var ids = new Gava1Ids(fields.GetProperty("Session").GetGuid(), fields.GetProperty("Execution").GetGuid(),
            fields.GetProperty("Generation").GetGuid());
        var boot = new Gava1PreBootstrap(fields.GetProperty("Version").GetUInt16(),
            (Gava1PreTopology)parsed.Topology, fields.GetProperty("AuthorityMode").GetString(), ids,
            Convert.FromHexString(fields.GetProperty("AuthorityHash").GetString()!));
        var (frame, expectation) = Frame(topology, vector);
        var decoded = Gava1ContextCodec.Decode(frame, expectation, out var context);
        Check(decoded == Gava1Refusal.None && context is not null, $"E-4 decode got {decoded}");
        var e4 = Gava1ContextCodec.MatchPreBootstrap(boot, context, (Gava1PreTopology)topology,
            new(vector.Ids, vector.Bytes[14]!, vector.Bytes[7]!), out var handoff);
        Check(e4 == Gava1Refusal.None && handoff is not null, $"E-4 handoff got {e4}");
        Array.Clear(frame);
        Array.Clear(payload);
        Array.Clear(handoff!.CapabilityDigest);
        Array.Clear(handoff.CatalogDigest);
        var result = Witness(topology) with { Ids = handoff.Ids };
        result.Bytes[7] = handoff.CatalogDigest;
        result.Bytes[14] = handoff.CapabilityDigest;
        return result.Bind();
    }

    private static byte[] Canonical(params (string Key, object? Value)[] items)
    {
        var fields = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in items)
            fields.Add(key, value);
        return JsonSerializer.SerializeToUtf8Bytes(fields);
    }

    private static byte[][] NewChain(string selector)
    {
        const string caseId = "q1-chain-64", topology = "b2-lifeline", phase = "pre";
        string artifact = Hex(Digest("chain artifact"));
        string host = Hex(Digest("chain host"));
        string closureC = Hex(Digest("chain C"));
        string closureD = Hex(Digest("chain D"));
        const long maxNs = 21_000_000_000;
        var m = Canonical(("caseId", caseId), ("objects", Array.Empty<object>()),
            ("schemaVersion", 1), ("topology", topology));
        var f = Canonical(("allowRelease", false), ("bootId", "15549c84-20d0-49e6-9b2d-571ccdc1ef99"),
            ("caseId", caseId), ("copy", null), ("fixtureManifestSha256", Hash(m)), ("gid", 712),
            ("hostId", host), ("hostPaths", null), ("inputEnrollmentId", null), ("invocation", null),
            ("maximumSessionNs", maxNs), ("phase", phase), ("policyKind", "b2-lifeline-v1"),
            ("resources", null), ("sandbox", null), ("schemaVersion", 1), ("toolEnrollmentId", null),
            ("topology", topology), ("uid", 712));
        var actions = new[] { "connect-b2", "inspect", "stop", "cleanup" }
            .Select((op, index) => new { index, op, timeoutNs = 1_500_000_000 }).ToArray();
        var s = Canonical(("actions", actions), ("caseId", caseId), ("fixtureManifestSha256", Hash(m)),
            ("fixturePolicySha256", Hash(f)), ("phase", phase), ("schemaVersion", 1), ("topology", topology));
        var k = Canonical(("actionScriptSha256", Hash(s)), ("allowRelease", false),
            ("artifactSetSha256", artifact), ("caseId", caseId), ("controllerClosureSha256", closureC),
            ("driverClosureSha256", closureD), ("fixtureManifestSha256", Hash(m)), ("fixturePolicySha256", Hash(f)),
            ("hostId", host), ("maximumSessionNs", maxNs), ("phase", phase), ("platformInventorySha256", Hex(Digest(selector))),
            ("profileId", "q1-profile"), ("profileVersion", "1"), ("qualificationId", null), ("qualificationRecordSha256", null),
            ("rid", "linux-x64"), ("schemaVersion", 1), ("scope", "test"),
            ("sourceCommit", new string('3', 40)), ("sourceTree", new string('4', 40)), ("topology", topology));
        (string, object?)[] common = [("artifactSetSha256", artifact), ("caseId", caseId), ("catalogSha256", Hash(k)),
            ("hostId", host), ("phase", phase), ("qualificationId", null), ("qualificationRecordSha256", null),
            ("schemaVersion", 1), ("topology", topology)];
        var r = Canonical([..common, ("actionScriptSha256", Hash(s)), ("conclusion", "supported-for-bounded-validation"),
            ("fixtureManifestSha256", Hash(m)), ("fixturePolicySha256", Hash(f))]);
        var w = Canonical([..common, ("catalogReviewSha256", Hash(r)), ("decision", "accept-exact-bounded-validation-catalog")]);
        var p = Canonical([..common, ("accepted", true), ("catalogReviewSha256", Hash(r)), ("ownerCatalogAcceptanceSha256", Hash(w))]);
        return [m, f, s, k, r, w, p];
    }

    private static Vector ExpectedChain(byte[][] held)
    {
        var result = Witness(1) with { Case = "q1-chain-64" };
        result.Bytes[3] = Digest("chain C");
        result.Bytes[5] = Digest("chain D");
        result.Bytes[6] = Digest("chain artifact");
        for (int i = 0; i < held.Length; i++)
            result.Bytes[new[] { 8, 9, 10, 7, 11, 12, 13 }[i]] = SHA256.HashData(held[i]);
        return result;
    }

    private static Evidence? ChainEvidence(byte[]?[] chain, out string failure)
    {
        if (chain.Any(b => b is null))
        {
            failure = "missing-operand";
            return null;
        }
        if (!FrozenContractVerifier.TryVerifyChain(chain[0]!, chain[1]!, chain[2]!, chain[3]!, out var parsed, out failure))
        {
            Check(parsed is null, "P1 returned a partial chain");
            return null;
        }
        if (!DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(chain[3]!, chain[4]!, chain[5]!,
            chain[6]!, out var records, out failure))
        {
            Check(records is null, "P2 returned partial bindings");
            return null;
        }
        var result = Witness(1) with { Case = parsed!.Catalog.CaseId };
        var hashes = new[] { parsed.Catalog.ControllerClosureSha256, parsed.Catalog.DriverClosureSha256,
            parsed.Catalog.ArtifactSetSha256, parsed.Hk, parsed.Hm, parsed.Hf, parsed.Hs, records!.Hr, records.Hw, records.Hp };
        for (int i = 0; i < hashes.Length; i++)
            result.Bytes[new[] { 3, 5, 6, 7, 8, 9, 10, 11, 12, 13 }[i]] = Convert.FromHexString(hashes[i]);
        return Observe(result.Bind(), 0);
    }

    private static void DetachedChain()
    {
        var held = ExpectedChain(NewChain("reviewed platform Q1-A"));
        Case("R17/independent-canonical-chain", () =>
        {
            var observed = ChainEvidence(NewChain("reviewed platform Q1-A"), out var failure);
            Check(observed is not null, "nominal chain: " + failure);
            var result = Gagamba.Execution.Linux.M1.Bootstrap.ValidationCustodyModel.TryCreate(new(held.Bind()),
                observed, 0, false, out var model);
            using (model)
                Check(result == Refusal.None && model is not null, $"nominal chain model: {result}");
        });
        Case("R04-R17/recomputed-entire-wrong-chain", () =>
        {
            var observed = ChainEvidence(NewChain("reviewed platform Q1-B"), out var failure);
            Check(observed is not null, "alternate chain is not structurally valid: " + failure);
            Check(!held.Bytes[7]!.SequenceEqual(observed!.Binding.Catalog) &&
                !held.Bytes[13]!.SequenceEqual(observed.Binding.Approval), "rebound did not change Hk/Hp");
            RefuseCreate(Refusal.Comparison, new(held.Bind()), observed);
        });
        Case("R17/wrong-external-Hp-alone", () =>
        {
            var observed = ChainEvidence(NewChain("reviewed platform Q1-A"), out var failure);
            Check(observed is not null, failure);
            var wrong = ExpectedChain(NewChain("reviewed platform Q1-A"));
            wrong.Bytes[13] = Digest("synthetic external Hp");
            RefuseCreate(Refusal.Comparison, new(wrong.Bind()), observed);
        });
        for (int i = 0; i < 7; i++)
        {
            Case($"R17/missing/{i}", () =>
            {
                byte[]?[] incomplete = NewChain("reviewed platform Q1-A");
                incomplete[i] = null;
                var observed = ChainEvidence(incomplete, out var failure);
                Check(observed is null && failure == "missing-operand", failure);
            });
        }
        Case("R17/P2-stale-approval", () =>
        {
            var wrong = NewChain("reviewed platform Q1-A");
            wrong[6] = NewChain("reviewed platform Q1-B")[6];
            var observed = ChainEvidence(wrong, out var failure);
            Check(observed is null && failure == "P catalogSha256 does not bind K", failure);
        });
    }
}
