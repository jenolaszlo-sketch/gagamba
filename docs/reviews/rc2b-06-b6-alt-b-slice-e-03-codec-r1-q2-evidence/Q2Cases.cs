using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;

internal static class Q2Cases
{
    internal static int Run(string output, Func<bool, bool, JsonObject> witness, byte[] capability)
    {
        using var rows = File.CreateText(Path.Combine(output, "q2-results.jsonl"));
        int total = 0, misses = 0;
        var expectedIds = new HashSet<string>();
        void Record(string id, string expected, string actual, byte[]? wire = null, byte[]? projection = null)
        {
            if (!expectedIds.Add(id)) throw new InvalidDataException(id);
            total++;
            bool pass = expected == "Refusal" ? actual != "None" && !actual.StartsWith("throw ") && !actual.StartsWith("out ") :
                expected == actual;
            if (!pass)
            {
                misses++;
                Console.WriteLine($"Q2 {id}: {expected} -> {actual}");
                var safe = string.Concat(id.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_'));
                if (wire != null) File.WriteAllBytes(Path.Combine(output, safe + ".wire"), wire);
                if (projection != null) File.WriteAllBytes(Path.Combine(output, safe + ".projection"), projection);
            }
            rows.WriteLine(JsonSerializer.Serialize(new { id, expected, actual, pass,
                wireSha256 = wire is null ? null : Convert.ToHexStringLower(SHA256.HashData(wire)),
                projectionSha256 = projection is null ? null : Convert.ToHexStringLower(SHA256.HashData(projection)) }));
        }

        foreach (bool b2 in new[] { true, false })
        {
            var prefix = b2 ? "B2" : "Integrated";
            var topology = b2 ? ValidationPreTopology.B2 : ValidationPreTopology.Integrated;
            var context = new ValidationPreContext(3, topology);
            var wire = witness(b2, false);
            var projection = witness(b2, true);
            string original = projection.ToJsonString(), claim = wire.ToJsonString();
            byte[] goodWire = Bytes(claim), goodProjection = Bytes(original);
            string Eval(byte[] wireBytes, JsonElement originalElement, byte[]? digest = null)
            {
                try {
                    var result = ValidationPreCodec.DecodeBootstrap(wireBytes, context, digest ?? capability, originalElement, out var data);
                    if ((result == ValidationPreRefusal.None) != (data is not null)) return "out " + result;
                    return result.ToString();
                } catch (Exception failure) { return "throw " + failure.GetType().Name; }
            }
            void Test(string id, string want, byte[] wireBytes, byte[] projectionBytes)
            {
                using var document = JsonDocument.Parse(projectionBytes.AsMemory());
                Record($"{prefix}/{id}", want, Eval(wireBytes, document.RootElement), wireBytes, projectionBytes);
            }
            void P(string id, string value, string want = "Refusal") => Test(id, want, goodWire, Bytes(value));
            void W(string id, string value, string want = "Refusal") => Test(id, want, Bytes(value), goodProjection);
            P("complete", original, "None");
            Record(prefix + "/undefined", "Expectation", Eval(goodWire, default), goodWire);
            var expired = JsonDocument.Parse(goodProjection);
            var disposed = expired.RootElement;
            var owned = disposed.Clone();
            expired.Dispose();
            Record(prefix + "/disposed", "Expectation", Eval(goodWire, disposed), goodWire, goodProjection);
            Record(prefix + "/clone-before-dispose", "None", Eval(goodWire, owned), goodWire, goodProjection);
            foreach (string kind in new[] { "null", "[]", "3", "false", "\"x\"" })
                P("root-kind/" + kind, kind, "Expectation");
            P("root-empty", "{}");
            P("opposite-projection", witness(!b2, true).ToJsonString());
            W("opposite-wire", witness(!b2, false).ToJsonString());

            foreach (string name in new[] { "Session", "EvidenceDirectory", "Owner", "Recovery" })
            {
                string escaped = EscapeName(name);
                string single = original.Replace(Quoted(name), Quoted(escaped), StringComparison.Ordinal);
                P("projection-single-escaped/" + name, single, "None");
                string malformed = original.Replace(Quoted(name), "\"\\uD800\"", StringComparison.Ordinal);
                P("projection-malformed-name/" + name, malformed, "Expectation");
            }

            // Apply the duplicate controls separately at each object instance, using exact
            // object substrings from the independent witness and both alias orderings.
            void Duplicates(JsonObject obj, string label, bool expected)
            {
                string text = obj.ToJsonString();
                foreach (var (member, item) in obj)
                {
                    string spelling = Quoted(member);
                    string escaped = Quoted(EscapeName(member));
                    string value = item?.ToJsonString() ?? "null";
                    string plain = text.Replace(spelling + ":" + value, spelling + ":" + value + "," + spelling + ":" + value, StringComparison.Ordinal);
                    string before = text.Replace(spelling + ":" + value, escaped + ":" + value + "," + spelling + ":" + value, StringComparison.Ordinal);
                    string after = text.Replace(spelling + ":" + value, spelling + ":" + value + "," + escaped + ":" + value, StringComparison.Ordinal);
                    foreach (var (variation, changed) in new[] { ("same", plain), ("alias-before", before), ("alias-after", after) })
                    {
                        if (changed == text) throw new InvalidDataException(label + "/" + member);
                        var name = $"{(expected ? "projection" : "wire")}-duplicate/{label}/{member}/{variation}";
                        if (expected) P(name, original.Replace(text, changed, StringComparison.Ordinal), "Expectation");
                        else W(name, claim.Replace(text, changed, StringComparison.Ordinal));
                    }
                    if (item is JsonObject nested) Duplicates(nested, label + "/" + member, expected);
                }
            }
            Duplicates(wire, "root", false);
            Duplicates(projection, "root", true);
            foreach (bool aliasFirst in new[] { true, false })
            foreach (bool matchingFirst in new[] { true, false })
            {
                const string session = "Session";
                string right = projection[session]!.ToJsonString();
                string wrong = "\"915edb2b-daf8-4fe9-8d5a-cf24b5800941\"";
                string firstName = aliasFirst ? EscapeName(session) : session;
                string lastName = aliasFirst ? session : EscapeName(session);
                var combined = $"{Quoted(firstName)}:{(matchingFirst ? right : wrong)},{Quoted(lastName)}:{(matchingFirst ? wrong : right)}";
                P($"projection-conflicting-order/{aliasFirst}/{matchingFirst}", original.Replace(Quoted(session) + ":" + right, combined), "Expectation");
                W($"wire-conflicting-order/{aliasFirst}/{matchingFirst}", claim.Replace(Quoted(session) + ":" + right, combined), "Schema");
            }

            // These projections are syntactically indexable as JSON. The member values
            // are unusable as the independently verified strings they purport to contain.
            foreach (string token in new[] { "/q1/\\uD800", "/q1/\\uDC00", "/q1/\\uD800\\u0058" })
                P("malformed-expected-value/" + token, original.Replace("/q1/evidence", token));
            int offset = original.IndexOf("/q1/evidence", StringComparison.Ordinal);
            byte[] invalidUtf8 = [..goodProjection[..offset], 0xc0, 0xaf, ..goodProjection[(offset + "/q1/evidence".Length)..]];
            Test("projection-invalid-utf8", "Refusal", goodWire, invalidUtf8);
            foreach (string badNumber in new[] { "72e2147483648", "72e-2147483649", "72e9999999999999999999999999", "0e99999999", "72.0000000001" })
                P("uid-number/" + badNumber, original.Replace("\"ControllerUid\":72", "\"ControllerUid\":" + badNumber));

            string digestHex = Convert.ToHexStringLower(capability);
            W("authority-escaped-key", claim.Replace("\"AuthorityHash\"", "\"\\u0041uthorityHash\""), "None");
            W("authority-literal-token-escape", claim.Replace(digestHex, $"\\u{(int)digestHex[0]:x4}" + digestHex[1..]), "Schema");
            W("authority-decoded-name-duplicate", claim.Insert(1, $"\"\\u0041uthorityHash\":\"{digestHex}\","), "Schema");
            var digestInput = (byte[])capability.Clone();
            Record(prefix + "/digest-before-change", "None", Eval(goodWire, owned, digestInput), goodWire, goodProjection);
            digestInput[0] ^= 0x80;
            Record(prefix + "/digest-next-call-uses-supplied-bytes", "Comparison", Eval(goodWire, owned, digestInput), goodWire, goodProjection);

            if (b2) continue;
            (string Name, string Matching, string Opposed)[] policy = [
                ("ToolSourceName", "\"tool\"", "\"TOOL\""),
                ("InputSourceName", "\"input\"", "\"input/\""),
                ("CoordinatorSourceName", "\"coordinator\"", "\"other\""),
                ("FilterSourceName", "\"filter\"", "\"fiℓter\""),
                ("TotalBudgetBytes", "16777216", "16777217"),
                ("MaxObjects", "4", "0")];
            foreach (var (name, matching, opposed) in policy)
            {
                var fixedClaim = Quoted(name) + ":" + matching;
                if (!original.Contains(fixedClaim, StringComparison.Ordinal) || !claim.Contains(fixedClaim, StringComparison.Ordinal))
                    throw new InvalidDataException(name);
                P("policy/agree/" + name, original, "None");
                P("policy/opposed/" + name, original.Replace(fixedClaim, Quoted(name) + ":" + opposed), "Comparison");
                var missing = (JsonObject)projection.DeepClone();
                missing["Integrated"]!["Profile"]!.AsObject().Remove(name);
                P("policy/absent/" + name, missing.ToJsonString(), "Expectation");
                foreach (var wrong in new[] { "null", "[]", "{}", "false", matching[0] == '"' ? "42" : "\"4\"" })
                    P($"policy/wrong-type/{name}/{wrong}", original.Replace(fixedClaim, Quoted(name) + ":" + wrong), "Expectation");
                string malformed = matching[0] == '"' ? "\"\\uD800\"" : matching + "e2147483648";
                P("policy/malformed/" + name, original.Replace(fixedClaim, Quoted(name) + ":" + malformed));
                W("policy/wrong-wire/" + name, claim.Replace(fixedClaim, Quoted(name) + ":" + opposed), "Schema");
            }
            foreach (string invalidObject in new[] { "null", "[]", "true", "1", "\"x\"" })
            {
                string profile = projection["Integrated"]!["Profile"]!.ToJsonString();
                P("policy/invalid-container/" + invalidObject, original.Replace(profile, invalidObject), "Expectation");
            }
            var mixed = (JsonObject)projection.DeepClone();
            mixed["Integrated"]!["Profile"]!["CoordinatorSourceName"] = "other";
            mixed["Integrated"]!["Profile"]!["MaxObjects"] = null;
            P("policy/mixed-invalid", mixed.ToJsonString());
            string semantic = original.Replace("\"MaxObjects\":4", "\"MaxObjects\":4.00e0")
                .Replace("\"TotalBudgetBytes\":16777216", "\"TotalBudgetBytes\":167772160e-1");
            P("policy/numeric-semantic-form", semantic, "None");
            W("policy/numeric-noncanonical-wire", claim.Replace("\"MaxObjects\":4", "\"MaxObjects\":4.00e0"), "Schema");
        }

        byte[] Digest(string seed) => SHA256.HashData(Bytes("Q2 independent " + seed));
        byte[] Confirmation(ReadOnlySpan<byte> digest) => [1, 0, 0, 0, ..digest];
        var context10 = new ValidationPreBinaryContext(new(3, ValidationPreTopology.B2),
            ValidationPreMessage.RecoveryEchoResponse, true);
        void Check10(string id, string want, byte[] input, ValidationPreBinaryExpectations expectation)
        {
            string actual;
            try { actual = ValidationPreCodecBinary.ValidatePayload(input, context10, expectation).ToString(); }
            catch (Exception failure) { actual = "throw " + failure.GetType().Name; }
            Record("binary/" + id, want, actual, input);
        }
        var shared = Digest("initial");
        var firstDigest = (byte[])shared.Clone();
        var first = new ValidationPreBinaryExpectations(shared);
        var firstValid = Confirmation(firstDigest);
        Check10("initial", "None", firstValid, first);
        Digest("changed").CopyTo(shared, 0);
        var second = new ValidationPreBinaryExpectations(shared);
        var secondValid = Confirmation(shared);
        Check10("first-after-original-mutation", "None", firstValid, first);
        Check10("first-refuses-second", "Comparison", secondValid, first);
        Check10("second-accepts-second", "None", secondValid, second);
        Check10("second-refuses-first", "Comparison", firstValid, second);
        Array.Fill(shared, (byte)0x9b);
        Check10("second-after-further-mutation", "None", secondValid, second);
        Check10("first-after-further-mutation", "None", firstValid, first);
        if (MemoryMarshal.TryGetArray(first.ArtifactSetSha256, out var visible))
            visible.Array![visible.Offset] ^= 0x40;
        Check10("first-after-exposed-view-write", "None", firstValid, first);
        Check10("first-refuses-mutated-view", "Comparison", Confirmation(firstDigest.Select((b,i) => (byte)(i == 0 ? b ^ 0x40 : b)).ToArray()), first);
        var aliasedInput = Confirmation(Digest("aliased"));
        var aliased = new ValidationPreBinaryExpectations(aliasedInput.AsMemory(4));
        var originalInput = (byte[])aliasedInput.Clone();
        Check10("alias-original", "None", aliasedInput, aliased);
        aliasedInput[35] ^= 0x40;
        Check10("alias-changed-candidate", "Comparison", aliasedInput, aliased);
        Check10("alias-retained-digest", "None", originalInput, aliased);
        using (var owner = new ArrayOwner(Digest("owned"))) {
            var payload = Confirmation(owner.GetSpan());
            var retained = new ValidationPreBinaryExpectations(owner.Memory);
            ((IDisposable)owner).Dispose();
            Check10("owner-disposed-after-snapshot", "None", payload, retained);
        }

        var initial = Enumerable.Range(0, 6).Select(i => Digest("field-" + i)).ToArray();
        var inputs = initial.Select(x => x.ToArray()).ToArray();
        var sessionGuid = Guid.Parse("69ef4516-1a31-4939-91a8-52dc280b09aa");
        var bound = new ValidationPreBinaryExpectations(inputs[0]) {
            OwnerClosureSha256 = inputs[1], RecoveryClosureSha256 = inputs[2], CoordinatorSha256 = inputs[3],
            SeccompFilterSha256 = inputs[4], RuntimeInventorySha256 = inputs[5], Session = sessionGuid,
            ProfileId = "ae4-linux-fixed-fixture-v1", ProfileVersion = "1" };
        var before37 = new byte[246];
        sessionGuid.TryWriteBytes(before37);
        for (int i = 0; i < inputs.Length; i++) initial[i].CopyTo(before37, 16 + 32*i);
        foreach (var at in new[] { 208, 210, 212, 214 }) before37[at] = 1;
        before37[217] = 26;
        "ae4-linux-fixed-fixture-v1"u8.CopyTo(before37.AsSpan(218));
        before37[244] = 1; before37[245] = (byte)'1';
        var context37 = new ValidationPreBinaryContext(new(3, ValidationPreTopology.B2), ValidationPreMessage.B2OwnerExpectation);
        ReadOnlyMemory<byte>[] access = [bound.ArtifactSetSha256, bound.OwnerClosureSha256, bound.RecoveryClosureSha256,
            bound.CoordinatorSha256, bound.SeccompFilterSha256, bound.RuntimeInventorySha256];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i][0] ^= 0x80;
            if (MemoryMarshal.TryGetArray(access[i], out var view)) view.Array![view.Offset + 31] ^= 0x20;
            var changed = before37.ToArray();
            changed[16 + i * 32] ^= 0x80;
            Record($"binary/field-{i}-reject-input-write", "Comparison", ValidationPreCodecBinary.ValidatePayload(changed, context37, bound).ToString(), changed);
            changed = before37.ToArray();
            changed[47 + i * 32] ^= 0x20;
            Record($"binary/field-{i}-reject-view-write", "Comparison", ValidationPreCodecBinary.ValidatePayload(changed, context37, bound).ToString(), changed);
            Record($"binary/field-{i}-repeat-original", "None", ValidationPreCodecBinary.ValidatePayload(before37, context37, bound).ToString(), before37);
        }
        Console.WriteLine($"Q2 supplemental assertions={total} matched={total - misses} unexpected={misses}");
        return misses;
    }

    private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
    private static string Quoted(string value) => "\"" + value + "\"";
    private static string EscapeName(string name) => $"\\u{(int)name[0]:x4}" + name[1..];

    private sealed class ArrayOwner(byte[] bytes) : MemoryManager<byte>
    {
        private byte[]? data = bytes;
        public override Span<byte> GetSpan() => data ?? throw new ObjectDisposedException(nameof(ArrayOwner));
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() => throw new NotSupportedException();
        protected override void Dispose(bool disposing) => data = null;
    }
}
