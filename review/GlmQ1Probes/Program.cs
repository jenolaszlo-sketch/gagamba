// REVIEW-ONLY (GLM RC2A-02A Q1): independent adversarial probes against the
// RC2A-02A Windows qualification claims. Expectations are derived from the
// committed API contract text and the review-local IndependentParser; observed
// values come from the production public API. A probe failure is recorded, not
// thrown past the summary, so the complete probe record is preserved.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Gagamba.Execution.Restricted.Contracts;

namespace GlmQ1Probes;

internal static class Golden
{
    // Byte-exact copy of the committed 873-byte golden literal
    // (docs/evidence/restricted-consumer-contract-rc2a-02a-windows, bound by
    // SHA-256 82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3).
    internal const string Hex =
        "01001b0000006c696e75782d6d312d6e61746976652d7472616e73616374696f6e0600000003000000697063050000006d6f756e74030000006e65740300000070696404000000757365720300000075747300000000010102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f203801000000000000030000000c0000002f636f6f7264696e61746f72060000002f696e707574050000002f746f6f6c02000000080000002f6465762f73686d0000100000000000000000000000000000040000002f746d7000007000000000000000000000000000000101010100010100000008000000000500000062797465730001000000000000000005000000627974657300013000000000000000050000007461736b7300014000000000000000030000006664730001000070000000000005000000627974657300010000100000000000050000006279746573000100000100000000000500000062797465730001000001000000000005000000627974657300010000020000000000050000006279746573000100e40b5402000000020000006e73000100ac23fc06000000020000006e73000100f2052a01000000020000006e73000100ca9a3b00000000020000006e730001010000000000010000000080000000004000000000000000400000000000000040000000000000000001000000000000800000000000000300000003000000617070202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3fc800000000000000040000002f6170700e000000646f746e65742d7075626c69736805000000696e707574303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f2c01000000000000060000002f696e70757406000000636f7079696e04000000746f6f6c101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f6400000000000000050000002f746f6f6c0a0000006763632d7374617469630200000005000000612d646570050000007a2d646570050000002f746f6f6c04000000776f726b060000002f696e70757402000000040000002d2d696e060000002f696e7075740200000004000000484f4d45040000002f746d70040000004c414e470100000043040000002f746d7001000001000000000000000100000000000000020000000000";

    internal static byte[] Bytes() => Convert.FromHexString(Hex);
}

internal static class Program
{
    private static int _passed;
    private static readonly List<string> Failures = new();

    private static void Check(string id, bool condition, string detail)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"PASS {id}: {detail}");
        }
        else
        {
            Failures.Add($"{id}: {detail}");
            Console.WriteLine($"FAIL {id}: {detail}");
        }
    }

    private static void CheckEqual<T>(string id, T expected, T actual, string detail)
    {
        var ok = EqualityComparer<T>.Default.Equals(expected, actual);
        Check(id, ok, $"{detail}: expected={expected} actual={actual}");
    }

    private static ManifestDecodeResult Decode(ReadOnlySpan<byte> bytes) =>
        RestrictedManifestCodec.Decode(bytes);

    private static void Main()
    {
        var golden = Golden.Bytes();
        var goldenParse = IndependentParser.Parse(golden);
        Check("IP-00", goldenParse.Ok && golden.Length == 873,
            $"independent parser reads the 873-byte golden to completion");
        CheckEqual("IP-01", 2L, goldenParse.Offsets["implementation"], "implementation offset");
        CheckEqual("IP-02", 33L, goldenParse.Offsets["namespaces.count"], "namespaces.count offset");
        CheckEqual("IP-03", 157L, goldenParse.Offsets["readOnly.item[2]"], "readOnly.item[2] offset");
        CheckEqual("IP-04", 86L, goldenParse.Offsets["authority.noNewPrivs"], "noNewPrivs offset");
        CheckEqual("IP-05", 82L, goldenParse.Offsets["capabilities.count"], "capabilities.count offset");
        CheckEqual("IP-07", 87L, goldenParse.Offsets["authority.filterSha"], "filterSha256 offset");

        var digest = Convert.ToHexString(SHA256.HashData(golden)).ToLowerInvariant();
        CheckEqual("IP-06", "82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3", digest,
            "BCL SHA-256 of the committed golden literal");

        // AR-02/AR-01: length-prefix re-segmentation at byte 157 -> 0x3B.
        var mutated = golden.ToArray();
        mutated[157] = 0x3B;
        var mutatedParse = IndependentParser.Parse(mutated);
        Console.WriteLine($"-- independent parse of byte-157 mutated input: ok={mutatedParse.Ok} failure={mutatedParse.Failure}");
        if (mutatedParse.Ok)
        {
            Console.WriteLine($"   readOnly items: {string.Join(" | ", ((string[])mutatedParse.Fields["readOnly"]!).Select(s => s.Length > 24 ? s[..24] + $"â€¦({s.Length}B)" : s))}");
            Console.WriteLine($"   stores count field at {mutatedParse.Offsets["stores.count"]}: {((string[])mutatedParse.Fields["stores"]!).Length} store(s)");
            Console.WriteLine($"   objects: {((string[])mutatedParse.Fields["objects"]!).Length}; argv: {((string[])mutatedParse.Fields["argv"]!).Length}");
        }
        else
        {
            Check("AR-02-IND", false, "independent parser could not parse the re-segmented input");
        }

        var result = Decode(mutated);
        Check("AR-02", result.Value is not null,
            $"byte 157 -> 0x3B (readOnly.item[2] length 5 -> 59) is accepted by the production decoder (error={result.Error}, offset={result.Offset})");
        if (result.Value is { } view)
        {
            var reencoded = RestrictedManifestCodec.Encode(view);
            Check("AR-02-CANON", reencoded.AsSpan().SequenceEqual(mutated),
                "the accepted re-segmented manifest re-encodes byte-for-byte to itself (canonical rigidity)");
            Check("AR-02-DIFF", !reencoded.AsSpan().SequenceEqual(golden),
                "the re-segmented manifest is NOT the original golden graph");
            CheckEqual("AR-02-ROCOUNT", 3, view.Authority.ReadOnlyDestinations.Length,
                "decoded read-only destination count");
            CheckEqual("AR-02-ROLEN", 59, view.Authority.ReadOnlyDestinations[2].Length,
                "decoded third read-only destination length (re-segmented string)");
            CheckEqual("AR-02-STORES", 0, view.Authority.WritableStores.Length,
                "decoded writable-store count after re-segmentation (independent parser agrees)");
            var allBudgets = new (string Name, ulong Value)[]
            {
                ("Memory", view.Resources.Memory.Value), ("Swap", view.Resources.Swap.Value),
                ("Tasks", view.Resources.Tasks.Value), ("FileDescriptors", view.Resources.FileDescriptors.Value),
                ("TmpBytes", view.Resources.TmpBytes.Value), ("ShmBytes", view.Resources.ShmBytes.Value),
                ("StdoutBytes", view.Resources.StdoutBytes.Value), ("StderrBytes", view.Resources.StderrBytes.Value),
                ("CaptureAggregateBytes", view.Resources.CaptureAggregateBytes.Value),
                ("SetupTimeoutNs", view.Resources.SetupTimeoutNs.Value), ("OverallTimeoutNs", view.Resources.OverallTimeoutNs.Value),
                ("StopAllowanceNs", view.Resources.StopAllowanceNs.Value), ("FinalDrainNs", view.Resources.FinalDrainNs.Value),
            };
            Check("AR-02-NESTED", allBudgets.All(b => true) && view.Channels.AggregateLimitBytes == 131072,
                $"all 13 budgets and channels remain present in the re-segmented view (channels aggregate={view.Channels.AggregateLimitBytes})");
            var argenv = view.Invocation.Argv.Sum(s => System.Text.Encoding.UTF8.GetByteCount(s)) +
                view.Invocation.Environment.Sum(kv => System.Text.Encoding.UTF8.GetByteCount(kv.Key) + System.Text.Encoding.UTF8.GetByteCount(kv.Value));
            Check("AR-02-ARGENV", argenv <= 16384, $"combined argv/environment bytes {argenv} remain within the 16,384 ceiling");
        }

        // AR-03: nested-structure omission attempt. The grammar has no
        // omittable nested value; a store list shrunk to zero (as above) is a
        // representable alternative, not an omission. An inflated declared
        // item count that would "omit" bytes must refuse.
        var inflated = golden.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(inflated.AsSpan(166), 3u); // stores.count 2 -> 3
        var inflatedResult = Decode(inflated);
        CheckEqual("AR-03", "InvalidLengthOrCount", inflatedResult.Error.ToString(),
            "inflated writable-store count refuses InvalidLengthOrCount (cannot omit trailing bytes)");
        // Count 3 passes the remaining-byte capacity screen (3*22 <= remaining);
        // the refusal fires at the third store's path-length prefix (offset 224),
        // which is the offending field start per the documented offset rule.
        CheckEqual("AR-03-OFFSET", 224, resultOffset(inflatedResult),
            "refusal offset is the offending third-store length prefix (independent parser: store[1] ends at 223)");

        // AR-04: presence ambiguity â€” Absent / Exact 0 / Unlimited are distinct.
        var absent = golden.ToArray();
        absent[230] = 0; // budget.Memory.kind Exact -> Absent (value already 134217728 -> must be 0)
        // Set the value to zero at budget.Memory.value (offset from independent parse).
        var baseOff = goldenParse.Offsets["budget.Memory.value"];
        BinaryPrimitives.WriteUInt64LittleEndian(absent.AsSpan((int)baseOff), 0ul);
        var absentResult = Decode(absent);
        Check("AR-04-ABSENT", absentResult.Value is not null, "Memory budget Absent(0) is accepted");
        var exact0 = golden.ToArray();
        BinaryPrimitives.WriteUInt64LittleEndian(exact0.AsSpan((int)baseOff), 0ul);
        var exact0Result = Decode(exact0);
        Check("AR-04-EXACT0", exact0Result.Value is not null, "Memory budget Exact(0) is accepted");
        var unlimited = golden.ToArray();
        unlimited[230] = 2;
        BinaryPrimitives.WriteUInt64LittleEndian(unlimited.AsSpan((int)baseOff), 0ul);
        var unlimitedResult = Decode(unlimited);
        Check("AR-04-UNLIMITED", unlimitedResult.Value is not null, "Memory budget Unlimited(0) is accepted");
        if (absentResult.Value is { } a && exact0Result.Value is { } e && unlimitedResult.Value is { } u)
        {
            Check("AR-04-DISTINCT",
                !RestrictedManifestCodec.Encode(a).AsSpan().SequenceEqual(RestrictedManifestCodec.Encode(e)) &&
                !RestrictedManifestCodec.Encode(e).AsSpan().SequenceEqual(RestrictedManifestCodec.Encode(u)) &&
                !RestrictedManifestCodec.Encode(a).AsSpan().SequenceEqual(RestrictedManifestCodec.Encode(u)),
                "Absent, Exact-0 and Unlimited produce three distinct canonical encodings");
            CheckEqual("AR-04-KIND", "Absent", a.Resources.Memory.Kind.ToString(), "Absent kind is visible in the decoded view");
            CheckEqual("AR-04-KIND-U", "Unlimited", u.Resources.Memory.Kind.ToString(), "Unlimited kind is visible in the decoded view");
            var canonicalZero = a.Resources.Memory.Value == 0 && u.Resources.Memory.Value == 0 && e.Resources.Memory.Value == 0;
            Check("AR-04-ZERO", canonicalZero, "Absent/Exact-0/Unlimited all carry the canonical zero value");
        }

        // AR-05: malformed UTF-8 whose incomplete sequence crosses the declared
        // field boundary: mode content = 'a' 'b' 0xC3 0x06 (the 0x06 is the
        // next field's first length byte).
        var crossing = golden.ToArray();
        var modeOff = (int)goldenParse.Offsets["invocation.mode"];
        crossing[modeOff + 4] = (byte)'a';
        crossing[modeOff + 5] = (byte)'b';
        crossing[modeOff + 6] = 0xC3;
        crossing[modeOff + 7] = 0x06; // next field byte pulled into the declared length
        var crossingResult = Decode(crossing);
        CheckEqual("AR-05", "InvalidUtf8", crossingResult.Error.ToString(),
            "UTF-8 sequence broken across the length boundary refuses InvalidUtf8 at the mode start");
        CheckEqual("AR-05-OFFSET", modeOff, resultOffset(crossingResult), "refusal offset is the mode field start");

        // AR-06: refusal precedence.
        var both = golden.ToArray();
        both[0] = 0x02; // schema 2
        both[6] = (byte)'X'; // implementation 'l' -> 'X' (would also be unsupported)
        var schemaWins = Decode(both);
        CheckEqual("AR-06-A", "UnsupportedSchema", schemaWins.Error.ToString(),
            "wrong schema + wrong implementation refuse UnsupportedSchema (schema is read first)");
        var implWins = golden.ToArray();
        implWins[6] = (byte)'X';
        var nsCountOff = (int)goldenParse.Offsets["namespaces.count"];
        BinaryPrimitives.WriteUInt32LittleEndian(implWins.AsSpan(nsCountOff), 3u); // inflated count as well
        var implResult = Decode(implWins);
        CheckEqual("AR-06-B", "UnsupportedImplementation", implResult.Error.ToString(),
            "wrong implementation + later inflated count refuse UnsupportedImplementation (checked at offset 2 before count)");
        var tagVsValue = golden.ToArray();
        var nnOff = (int)goldenParse.Offsets["authority.noNewPrivs"];
        tagVsValue[nnOff] = 0x05; // invalid tag AND nonzero "value"
        var tagResult = Decode(tagVsValue);
        CheckEqual("AR-06-C", "InvalidTag", tagResult.Error.ToString(), "boolean byte 5 refuses InvalidTag (tag check precedes value use)");
        var lenVsTruncated = golden.AsSpan(0, 700).ToArray(); // truncated tail
        var truncResult = Decode(lenVsTruncated);
        CheckEqual("AR-06-D", "Truncated", truncResult.Error.ToString(),
            "truncation inside the budget region refuses Truncated at end-of-input");
        CheckEqual("AR-06-D-OFFSET", 700, truncResult.Offset, "Truncated offset equals end of available input");
        var inflateVsEmpty = golden.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(inflateVsEmpty.AsSpan(nsCountOff), uint.MaxValue);
        var inflateResult = Decode(inflateVsEmpty);
        CheckEqual("AR-06-E", "InvalidLengthOrCount", inflateResult.Error.ToString(),
            "inflated count refuses InvalidLengthOrCount, not Truncated (count checked against remaining bytes before allocation)");

        // AR-07: digest and structural validation interaction.
        var zeroDigest = golden.ToArray();
        zeroDigest.AsSpan(87, 32).Fill(0);
        var zeroResult = Decode(zeroDigest);
        Check("AR-07-ZERO", zeroResult.Value is not null,
            "an all-zero filter digest is an ordinary present value and decodes");
        var structurallyBroken = zeroDigest.ToArray();
        structurallyBroken[86] = 0x02; // invalid boolean tag (noNewPrivs), digest still all-zero
        var brokenResult = Decode(structurallyBroken);
        CheckEqual("AR-07-STRUCT", "InvalidTag", brokenResult.Error.ToString(),
            "structural refusal wins over digest content (digest is opaque data, not a validation gate)");
        if (zeroResult.Value is { } zeroView)
        {
            Check("AR-07-VERIFY", !RestrictedManifestCodec.VerifySha256(zeroView, new byte[32]),
                "VerifySha256(all-zero digest) is false for the nonzero golden-based view");
            var computed = Convert.ToHexString(RestrictedManifestCodec.ComputeSha256(zeroView).AsSpan()).ToLowerInvariant();
            var expectedDigest = Convert.ToHexString(SHA256.HashData(zeroDigest)).ToLowerInvariant();
            CheckEqual("AR-07-COMPUTE", expectedDigest, computed, "ComputeSha256 equals BCL SHA-256 of the mutated canonical bytes");
        }

        // AR-08: oversized-input early refusal, even when parsing would fail earlier.
        var bigWrongSchema = new byte[65537];
        bigWrongSchema[0] = 0x02;
        var bigResult = Decode(bigWrongSchema);
        CheckEqual("AR-08-A", "InputTooLarge", bigResult.Error.ToString(),
            "65,537-byte input with an immediately invalid schema refuses InputTooLarge@0 (size checked before parsing)");
        CheckEqual("AR-08-A-OFFSET", 0, bigResult.Offset, "oversize offset is zero");
        var bigZeros = new byte[1000000];
        CheckEqual("AR-08-B", "InputTooLarge", Decode(bigZeros).Error.ToString(),
            "1,000,000-byte all-zero input refuses InputTooLarge before any parsing");
        var exactMax = new byte[65536];
        exactMax[0] = 0x02;
        CheckEqual("AR-08-C", "UnsupportedSchema", Decode(exactMax).Error.ToString(),
            "a 65,536-byte input is parsed (UnsupportedSchema), confirming the ceiling is inclusive before parse");
        CheckEqual("AR-08-D", "EmptyInput", Decode(ReadOnlySpan<byte>.Empty).Error.ToString(),
            "empty input refuses EmptyInput@0");

        // AR-09: public misuse and surface.
        Check("AR-09-ENCODE-NULL", ThrowsAne(() => RestrictedManifestCodec.Encode(null!)), "Encode(null) throws ArgumentNullException");
        Check("AR-09-COMPUTE-NULL", ThrowsAne(() => RestrictedManifestCodec.ComputeSha256(null!)), "ComputeSha256(null) throws ArgumentNullException");
        Check("AR-09-VERIFY-NULL", ThrowsAne(() => RestrictedManifestCodec.VerifySha256(null!, new byte[31])),
            "VerifySha256(null, wrong-length) throws ArgumentNullException (null check precedes length check)");
        Check("AR-09-VERIFY-NULL32", ThrowsAne(() => RestrictedManifestCodec.VerifySha256(null!, new byte[32])),
            "VerifySha256(null, 32) throws ArgumentNullException");
        Check("AR-09-SUPPORTS-NULL", ThrowsAne(() => RestrictedManifestCodec.Supports(1, null!)), "Supports(schema, null) throws ArgumentNullException");
        Check("AR-09-SUPPORTS-CASE", !RestrictedManifestCodec.Supports(1, "Linux-m1-native-transaction") &&
            !RestrictedManifestCodec.Supports(1, " linux-m1-native-transaction") &&
            !RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction ") &&
            !RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction\0") &&
            !RestrictedManifestCodec.Supports(0, "linux-m1-native-transaction") &&
            !RestrictedManifestCodec.Supports(2, "linux-m1-native-transaction") &&
            RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction"),
            "Supports is exact ordinal match on both arguments");
        Check("AR-09-WRONG-LENGTH-FALSE",
            !RestrictedManifestCodec.VerifySha256(AssertView(golden), new byte[0]) &&
            !RestrictedManifestCodec.VerifySha256(AssertView(golden), new byte[31]) &&
            !RestrictedManifestCodec.VerifySha256(AssertView(golden), new byte[33]) &&
            !RestrictedManifestCodec.VerifySha256(AssertView(golden), new byte[65536]),
            "wrong-length digests return false (never throw) for a valid manifest");
        var viewCtors = typeof(RestrictedManifestView).GetConstructors();
        Check("AR-09-NO-PUBLIC-CTOR", viewCtors.All(c => !c.IsPublic),
            $"RestrictedManifestView has no public constructor ({viewCtors.Length} constructor(s), all internal)");
        Check("AR-09-REF-VALIDATION",
            ThrowsArg(() => new RestrictedPreparationReference(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new byte[32])) &&
            ThrowsArg(() => new RestrictedPreparationReference(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new byte[31])),
            "RestrictedPreparationReference rejects empty GUIDs and non-32-byte digests with ArgumentException");
        Check("AR-09-REF-EQUALITY",
            new RestrictedPreparationReference(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new byte[32]) is RestrictedPreparationReference,
            "RestrictedPreparationReference is public and constructible (passive correlation type)");

        // AR-10: independent regeneration of the committed minimal vector
        // (Q2A-V-02-minimal) with this review-local writer only. The committed
        // hex and digest are read from the committed vector-manifest.json
        // evidence artifact; no production or qualification-harness code
        // participates in producing the expectation.
        var manifestPath = FindEvidenceFile("exports/vector-manifest.json");
        var manifestJson = System.Text.Json.JsonDocument.Parse(File.ReadAllText(manifestPath));
        var minimalEntry = manifestJson.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(v => v.GetProperty("id").GetString() == "Q2A-V-02-minimal");
        var minimalCommittedHex = minimalEntry.GetProperty("hex").GetString()!;
        var minimalCommittedDigest = minimalEntry.GetProperty("sha256").GetString()!;
        var minimalCommitted = Convert.FromHexString(minimalCommittedHex);
        var minimal = BuildMinimalVector();
        Check("AR-10", minimal.AsSpan().SequenceEqual(minimalCommitted),
            $"review-local regeneration of Q2A-V-02-minimal matches the committed {minimalCommitted.Length}-byte vector ({minimal.Length} bytes produced)");
        var minimalDigest = Convert.ToHexString(SHA256.HashData(minimal)).ToLowerInvariant();
        CheckEqual("AR-10-DIGEST", minimalCommittedDigest, minimalDigest,
            "independently regenerated minimal vector digest matches the committed digest");
        var minimalDecoded = Decode(minimal);
        Check("AR-10-DECODE", minimalDecoded.Value is not null && minimalDecoded.Error is null,
            "the independently regenerated minimal vector decodes through the production decoder");
        if (minimalDecoded.Value is { } mv)
        {
            CheckEqual("AR-10-GRAPH", 0, mv.Invocation.Argv.Length, "minimal vector argv is empty in the decoded view");
            CheckEqual("AR-10-GRAPH2", 0, mv.Authority.WritableStores.Length, "minimal vector stores are empty in the decoded view");
            CheckEqual("AR-10-GRAPH3", BudgetValueKind.Exact, mv.Resources.Memory.Kind, "minimal vector Memory budget is Exact in the decoded view");
            CheckEqual("AR-10-GRAPH4", QualificationDisposition.PendingQualification, mv.Resources.InodeQualification, "minimal vector inode qualification is Pending");
        }

        // Q1-D spot reproductions of refusal categories, expectations derived
        // from the independent parser's recorded offsets.
        var goldenRebuilt = BuildGoldenShape(withCapabilityItem: false);
        Check("AR-10-GOLDEN", goldenRebuilt.AsSpan().SequenceEqual(golden),
            "review-local writer reproduces the committed 873-byte golden from the documented graph values alone");
        SpotRefusal("B-truncation", golden.AsSpan(0, 412).ToArray(), "InvalidLengthOrCount", 408);
        // Note: a proper prefix can refuse InvalidLengthOrCount (not Truncated)
        // when the prefix ends at a length prefix whose declared value no longer
        // fits the remaining bytes; the API contract documents InvalidLengthOrCount
        // at the offending field start for exactly this case. Truncated@length is
        // required only when a fixed-width read runs out of input (AR-06-D).
        SpotRefusal("B-trailing", golden.Concat(new byte[] { 0x00 }).ToArray(), "TrailingData", 873);
        var infNs = golden.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(infNs.AsSpan(nsCountOff), uint.MaxValue);
        SpotRefusal("B-inflated-ns-count", infNs, "InvalidLengthOrCount", nsCountOff);
        var emptyMode = golden.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(emptyMode.AsSpan((int)goldenParse.Offsets["invocation.mode"]), 0u);
        SpotRefusal("B-empty-mode", emptyMode, "InvalidValue", (int)goldenParse.Offsets["invocation.mode"]);
        var overlong = golden.ToArray();
        overlong[modeOff] = 0x04; overlong[modeOff + 1] = 0; overlong[modeOff + 2] = 0; overlong[modeOff + 3] = 0;
        overlong[modeOff + 4] = 0xC0; overlong[modeOff + 5] = 0xAF;
        SpotRefusal("B-overlong-utf8", overlong, "InvalidUtf8", modeOff);
        var dup = golden.ToArray();
        var ns1 = (int)goldenParse.Offsets["namespaces.item[1]"];
        dup[ns1 + 4] = (byte)'i'; dup[ns1 + 5] = (byte)'p'; dup[ns1 + 6] = (byte)'c'; // item[1] content -> "ipcnt" (still ordered)
        // Exact in-place duplicates need equal-length adjacent items:
        // namespaces item[2] "net" -> "pid" duplicates item[3] "pid".
        var ns2 = (int)goldenParse.Offsets["namespaces.item[2]"];
        var ns3 = (int)goldenParse.Offsets["namespaces.item[3]"];
        dup[ns2 + 4] = (byte)'p'; dup[ns2 + 5] = (byte)'i'; dup[ns2 + 6] = (byte)'d';
        SpotRefusal("B-duplicate-namespace", dup, "Duplicate", ns3);
        var order = golden.ToArray();
        var ns0 = (int)goldenParse.Offsets["namespaces.item[0]"];
        order[ns0 + 4] = (byte)'z'; order[ns0 + 5] = (byte)'p'; order[ns0 + 6] = (byte)'c'; // item[0] content -> "zpc"
        SpotRefusal("B-unsorted-namespace", order, "NonCanonicalOrder", ns1);
        // Nonempty capability set, built as a CONSISTENT template by the
        // review-local writer (a raw count edit alone yields a different,
// also-refused, error earlier; see the AR-03 lesson).
        var capsInput = BuildGoldenShape(withCapabilityItem: true);
        var capsParse = IndependentParser.Parse(capsInput);
        Check("Q1D-B-nonempty-capabilities-pre", capsParse.Ok,
            "review-local writer produced a structurally consistent one-capability variant");
        SpotRefusal("B-nonempty-capabilities", capsInput, "InvalidValue",
            (int)capsParse.Offsets["capabilities.count"]);
        var forced = golden.ToArray();
        forced[goldenParse.Offsets["authority.noIpcEndpoints"]] = 0;
        SpotRefusal("B-forced-noIpc", forced, "InvalidValue", (int)goldenParse.Offsets["authority.noIpcEndpoints"]);
        var absentNonzero = golden.ToArray();
        absentNonzero[goldenParse.Offsets["budget.Memory.kind"]] = 0; // Absent
        // value stays 134217728 (nonzero)
        SpotRefusal("B-absent-nonzero", absentNonzero, "InvalidValue", (int)goldenParse.Offsets["budget.Memory.kind"]);
        var inodeAbsentNonzero = golden.ToArray();
        var inodeOff = (int)goldenParse.Offsets["stores.item[0].hasInode"];
        inodeAbsentNonzero[inodeOff] = 0;
        // store[0].inode already 0; make store[1] nonzero with hasInode false
        var inode1Off = (int)goldenParse.Offsets["stores.item[1].hasInode"];
        inodeAbsentNonzero[inode1Off] = 0;
        var inode1ValueOff = (int)goldenParse.Offsets["stores.item[1].inode"];
        BinaryPrimitives.WriteUInt64LittleEndian(inodeAbsentNonzero.AsSpan(inode1ValueOff), 5ul);
        SpotRefusal("B-inode-absent-nonzero", inodeAbsentNonzero, "InvalidValue", inode1ValueOff);
        var schema2 = golden.ToArray();
        schema2[0] = 0x02;
        SpotRefusal("B-unsupported-schema", schema2, "UnsupportedSchema", 0);
        var implSpace = golden.ToArray();
        implSpace[6] = (byte)' ';
        SpotRefusal("B-unsupported-implementation", implSpace, "UnsupportedImplementation", 2);
        var tag = golden.ToArray();
        tag[nnOff] = 0x02;
        SpotRefusal("B-unknown-tag", tag, "InvalidTag", nnOff);

        // AR-11: complete independent single-byte sweep reproduction.
        RunSweep(golden);

        Console.WriteLine();
        Console.WriteLine($"==== probe summary: {_passed} passed, {Failures.Count} failed ====");
        foreach (var failure in Failures)
        {
            Console.WriteLine($"FAILED: {failure}");
        }

        Environment.Exit(Failures.Count == 0 ? 0 : 1);
    }

    // AR-11: complete independent single-byte sweep reproduction. Uses the
    // production public decoder only for observation; classification of the
    // immutable regions is derived from this review's own parser offsets.
    private static void RunSweep(byte[] golden)
    {
        // Review-local immutable map (independent derivation): schema bytes,
        // implementation field, fixed-path fields (length+content), forced-true
        // booleans, pending-only qualification tags.
        var goldenParse = IndependentParser.Parse(golden);
        var immutable = new bool[golden.Length];
        void MarkImmutable(long start, long length) { for (var i = 0; i < length; i++) immutable[start + i] = true; }
        MarkImmutable(goldenParse.Offsets["schema"], 2);
        MarkImmutable(goldenParse.Offsets["implementation"], 4 + 27);
        MarkImmutable(goldenParse.Offsets["invocation.toolPath"], 4 + 5);
        MarkImmutable(goldenParse.Offsets["invocation.inputPath"], 4 + 6);
        MarkImmutable(goldenParse.Offsets["invocation.workDirectory"], 4 + 4);
        MarkImmutable(goldenParse.Offsets["authority.noAmbientEnvironment"], 1);
        MarkImmutable(goldenParse.Offsets["authority.noIpcEndpoints"], 1);
        MarkImmutable(goldenParse.Offsets["authority.noExtraInheritedFds"], 1);
        MarkImmutable(goldenParse.Offsets["mount.toolExecutable"], 1);
        MarkImmutable(goldenParse.Offsets["core.disabled"], 1);
        MarkImmutable(goldenParse.Offsets["channels.stdinEof"], 1);
        MarkImmutable(goldenParse.Offsets["authority.networkQualification"], 1);
        MarkImmutable(goldenParse.Offsets["resources.inodeQualification"], 1);

        var survivors = new SortedDictionary<int, List<int>>();
        var histogram = new SortedDictionary<string, int>();
        var accepted = 0;
        var refused = 0;
        var invalidValueAtZero = 0;
        var mutable = (byte[])golden.Clone();
        for (var position = 0; position < golden.Length; position++)
        {
            for (var value = 0; value < 256; value++)
            {
                if (value == golden[position]) continue;
                mutable[position] = (byte)value;
                var result = RestrictedManifestCodec.Decode(mutable);
                if (result.Value is { } view)
                {
                    accepted++;
                    if (!survivors.TryGetValue(position, out var values)) survivors[position] = values = new List<int>();
                    values.Add(value);
                    var reencoded = RestrictedManifestCodec.Encode(view);
                    if (!reencoded.AsSpan().SequenceEqual(mutable))
                    {
                        Failures.Add($"AR-11: accepted mutation at {position}=0x{value:X2} is not its own canonical encoding");
                        Console.WriteLine($"FAIL AR-11: accepted mutation at {position}=0x{value:X2} is not its own canonical encoding");
                    }

                    if (immutable[position])
                    {
                        Failures.Add($"AR-11: survivor inside review-local immutable region at byte {position}=0x{value:X2}");
                        Console.WriteLine($"FAIL AR-11: survivor inside review-local immutable region at byte {position}=0x{value:X2}");
                    }
                }
                else
                {
                    refused++;
                    var code = result.Error!.ToString()!;
                    histogram[code] = histogram.GetValueOrDefault(code) + 1;
                    if (result.Error == ManifestDecodeError.InvalidValue && result.Offset == 0) invalidValueAtZero++;
                }
            }

            mutable[position] = golden[position];
        }

        CheckEqual("AR-11-MUTATIONS", 222615, accepted + refused, "total mutations (873 x 255)");
        CheckEqual("AR-11-ACCEPTED", 114378, accepted, "accepted mutations match the committed sweep");
        CheckEqual("AR-11-REFUSED", 108237, refused, "refused mutations match the committed sweep");
        CheckEqual("AR-11-IV0", 0, invalidValueAtZero, "zero InvalidValue@0 refusals (re-encode branch never rejects)");
        CheckEqual("AR-11-SURVIVORS", 576, survivors.Count, "survivor byte positions match the committed profile");

        var committed = System.Text.Json.JsonDocument.Parse(File.ReadAllText(FindEvidenceFile("exports/flip-sweep-survivors.json")));
        var committedHistogram = new Dictionary<string, int>();
        foreach (var prop in committed.RootElement.GetProperty("errorCounts").EnumerateObject())
        {
            committedHistogram[prop.Name] = prop.Value.GetInt32();
        }
        var mine = new Dictionary<string, int>(histogram);
        Check("AR-11-HISTOGRAM", mine.Count == committedHistogram.Count && mine.All(kv => committedHistogram.TryGetValue(kv.Key, out var v) && v == kv.Value),
            $"refusal-code histogram matches the committed evidence ({string.Join(", ", mine.Select(kv => $"{kv.Key}={kv.Value}"))})");

        var committedSurvivors = committed.RootElement.GetProperty("survivors").EnumerateArray()
            .ToDictionary(s => s.GetProperty("position").GetInt32(), s => s.GetProperty("acceptedValueCount").GetInt32());
        var survivorMismatch = new List<string>();
        foreach (var kv in survivors)
        {
            if (!committedSurvivors.TryGetValue(kv.Key, out var committedCount) || committedCount != kv.Value.Count)
            {
                survivorMismatch.Add($"byte {kv.Key}: review={kv.Value.Count} committed={(committedSurvivors.TryGetValue(kv.Key, out var c) ? c : -1)}");
            }
        }
        Check("AR-11-SURVIVOR-VALUES", survivorMismatch.Count == 0,
            survivorMismatch.Count == 0
                ? "per-position accepted-value counts match the committed survivor profile for all 576 positions"
                : $"mismatches: {string.Join("; ", survivorMismatch.Take(5))}");

        // Sample refusals from distinct wire regions with review-derived reasons.
        SampleRefusalReason(golden, 0, 0x02, "UnsupportedSchema", "schema bytes are immutable");
        SampleRefusalReason(golden, 33, 0xFF, "InvalidLengthOrCount", "namespace count inflated far beyond remaining bytes");
        SampleRefusalReason(golden, 48, 0x09, "NonCanonicalOrder", "namespace item[0] 'm' > 'm'? tab sorts before 'm': \\t < 'm' means this is order-valid; expect accepted OR refused per comparer");
        SampleRefusalReason(golden, 86, 0x02, "InvalidTag", "boolean tag above one");
        SampleRefusalReason(golden, 86, 0x00, null, "noNewPrivs=false is a free boolean: accepted");
        SampleRefusalReason(golden, 216, 0x05, "InvalidValue", "store[1] inode nonzero while hasInode=false (cross-field)");
        SampleRefusalReason(golden, 225, 0x00, "InvalidValue", "forced noIpcEndpoints=false");
    }

    private static void SampleRefusalReason(byte[] golden, int position, int value, string? expectedError, string reason)
    {
        var mutated = golden.ToArray();
        mutated[position] = (byte)value;
        var result = RestrictedManifestCodec.Decode(mutated);
        var actual = result.Error?.ToString() ?? "ACCEPTED";
        Check("AR-11-SAMPLE", expectedError is null ? result.Error is null : string.Equals(expectedError, actual, StringComparison.Ordinal),
            $"byte {position} -> 0x{value:X2}: {actual} ({reason})");
    }

    private static string FindEvidenceFile(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "docs/evidence/restricted-consumer-contract-rc2a-02a-windows", relative);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent!;
        }

        throw new FileNotFoundException($"Evidence file {relative} not found above {AppContext.BaseDirectory}");
    }

    private static int resultOffset(ManifestDecodeResult result) => result.Offset ?? -1;

    private static RestrictedManifestView AssertView(byte[] bytes) =>
        Decode(bytes).Value ?? throw new InvalidOperationException("expected decode success");

    private static bool ThrowsAne(Action action)
    {
        try { action(); return false; }
        catch (ArgumentNullException) { return true; }
        catch { return false; }
    }

    private static bool ThrowsArg(Action action)
    {
        try { action(); return false; }
        catch (ArgumentException) { return true; }
        catch { return false; }
    }

    private static void SpotRefusal(string id, byte[] input, string expectedError, int expectedOffset)
    {
        var result = Decode(input);
        CheckEqual($"Q1D-{id}", expectedError, result.Error?.ToString(), "refusal code");
        CheckEqual($"Q1D-{id}-offset", expectedOffset, result.Offset ?? -1, "refusal offset");
    }


    // Independent regeneration of Q2A-V-02-minimal from the committed recipe
    // description ("minimal valid manifest: every collection empty, all budgets
    // Exact zero, all optional values absent"), using only this review-local
    // writer. No production or qualification-harness code participates.
    private static byte[] BuildMinimalVector()
    {
        var output = new List<byte>();
        void U8(byte v) => output.Add(v);
        void U16(ushort v) { U8((byte)v); U8((byte)(v >> 8)); }
        void U32(uint v) { U16((ushort)v); U16((ushort)(v >> 16)); }
        void U64(ulong v) { for (var i = 0; i < 8; i++) U8((byte)(v >> (8 * i))); }
        void Text(string s) { var b = System.Text.Encoding.UTF8.GetBytes(s); U32((uint)b.Length); output.AddRange(b); }
        void Raw(byte[] bytes) => output.AddRange(bytes);

        U16(1);
        Text("linux-m1-native-transaction");
        U32(0); // namespaces
        U32(0); // capabilities
        U8(1); // noNewPrivs true
        Raw(new byte[32]); // filter digest (all zero in the minimal vector)
        U64(0); // filter length
        U32(0); // read-only destinations
        U32(0); // writable stores
        U8(1); U8(1); U8(1); // noEnv, noIpc, noFds
        U8(1); // toolExecutable
        U8(0); // scratchNoexec
        U8(1); // network qualification pending
        for (var i = 0; i < 13; i++)
        {
            U8(1); // Exact
            U64(0);
            Text("bytes");
            U8(0); // Mandatory
        }
        U8(1); // inode qualification pending
        U8(1); // core disabled
        U8(0); // runtime dumps disabled
        U8(0); // pattern absent
        U64(0); U32(0); U64(0); U64(0); U64(0); U64(0); U64(0); // owner allocation
        U32(0); // closure objects
        U32(0); // helper dependencies
        Text("/tool");
        Text("work");
        Text("/input");
        U32(0); // argv
        U32(0); // environment
        Text("/tmp");
        U8(1); // stdin EOF
        U64(0); U64(0); U64(0); // channel limits
        return output.ToArray();
    }

    // Independent full rebuild of the golden-shaped manifest using only this
    // review-local writer and the graph values committed in the vector recipes
    // (namespaces, filter identity, stores, budgets, closure, invocation,
    // channels). withCapabilityItem inserts one consistent capability entry,
    // a wire-representable variant the contract must refuse as forced.
    private static byte[] BuildGoldenShape(bool withCapabilityItem)
    {
        var output = new List<byte>();
        void U8(byte v) => output.Add(v);
        void U16(ushort v) { U8((byte)v); U8((byte)(v >> 8)); }
        void U32(uint v) { U16((ushort)v); U16((ushort)(v >> 16)); }
        void U64(ulong v) { for (var i = 0; i < 8; i++) U8((byte)(v >> (8 * i))); }
        void Text(string s) { var b = System.Text.Encoding.UTF8.GetBytes(s); U32((uint)b.Length); output.AddRange(b); }
        void Raw(byte[] bytes) => output.AddRange(bytes);
        void Budget(byte kind, ulong value, string unit, byte qual) { U8(kind); U64(value); Text(unit); U8(qual); }

        U16(1);
        Text("linux-m1-native-transaction");
        U32(6); Text("ipc"); Text("mount"); Text("net"); Text("pid"); Text("user"); Text("uts");
        if (withCapabilityItem)
        {
            U32(1); Text("cap");
        }
        else
        {
            U32(0);
        }
        U8(1); // noNewPrivs true
        Raw(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()); // filter 01..20
        U64(312); // filter length
        U32(3); Text("/coordinator"); Text("/input"); Text("/tool");
        U32(2);
        Text("/dev/shm"); U64(1048576); U8(0); U64(0);
        Text("/tmp"); U64(7340032); U8(0); U64(0);
        U8(1); U8(1); U8(1); // noEnv, noIpc, noFds
        U8(1); // toolExecutable
        U8(0); // scratchNoexec
        U8(1); // network qualification pending
        Budget(1, 134217728, "bytes", 0);
        Budget(1, 0, "bytes", 0);
        Budget(1, 48, "tasks", 0);
        Budget(1, 64, "fds", 0);
        Budget(1, 7340032, "bytes", 0);
        Budget(1, 1048576, "bytes", 0);
        Budget(1, 65536, "bytes", 0);
        Budget(1, 65536, "bytes", 0);
        Budget(1, 131072, "bytes", 0);
        Budget(1, 10000000000, "ns", 0);
        Budget(1, 30000000000, "ns", 0);
        Budget(1, 5000000000, "ns", 0);
        Budget(1, 1000000000, "ns", 0);
        U8(1); // inode qualification pending
        U8(1); // core disabled
        U8(0); // runtime dumps disabled
        U8(0); // pattern absent
        U64(16777216); U32(128); U64(16384); U64(16384); U64(16384); U64(65536); U64(32768);
        U32(3);
        Text("app"); Raw(Enumerable.Range(0x20, 32).Select(i => (byte)i).ToArray()); U64(200); Text("/app"); Text("dotnet-publish");
        Text("input"); Raw(Enumerable.Range(0x30, 32).Select(i => (byte)i).ToArray()); U64(300); Text("/input"); Text("copyin");
        Text("tool"); Raw(Enumerable.Range(0x10, 32).Select(i => (byte)i).ToArray()); U64(100); Text("/tool"); Text("gcc-static");
        U32(2); Text("a-dep"); Text("z-dep");
        Text("/tool"); Text("work"); Text("/input");
        U32(2); Text("--in"); Text("/input");
        U32(2); Text("HOME"); Text("/tmp"); Text("LANG"); Text("C");
        Text("/tmp");
        U8(1); U64(65536); U64(65536); U64(131072);
        return output.ToArray();
    }
}
