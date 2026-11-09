using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class Program
{
    private const string Ns = "Gagamba.Execution.Linux.M1.Validation";
    private static readonly BindingFlags StaticAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly BindingFlags InstanceAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static int _pass;
    private static int _fail;

    private static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { _pass++; Console.WriteLine($"RESULT|{name}|PASS|{detail}"); }
        else { _fail++; Console.WriteLine($"RESULT|{name}|FAIL|{detail}"); }
    }

    private static int Main(string[] args)
    {
        var repo = args.Length > 0 ? args[0] : @"C:\Users\Laszlos\source\repos\Gagamba";
        var debugM1 = Path.Combine(repo, "src", "Gagamba.Execution.Linux.M1", "bin", "Debug", "net10.0", "Gagamba.Execution.Linux.M1.dll");
        var releaseM1 = Path.Combine(repo, "src", "Gagamba.Execution.Linux.M1", "bin", "Release", "net10.0", "Gagamba.Execution.Linux.M1.dll");
        if (!File.Exists(debugM1)) { Console.WriteLine($"FATAL|missing {debugM1}"); return 2; }
        var execution = Path.Combine(Path.GetDirectoryName(debugM1)!, "Gagamba.Execution.dll");
        AssemblyLoadContext.Default.LoadFromAssemblyPath(execution);
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(debugM1);
        Console.WriteLine($"INFO|debug_m1|{debugM1}");
        Console.WriteLine($"INFO|debug_m1_sha256|{Sha256File(debugM1)}");

        ReflectionApi.Bind(assembly, debugM1);
        var codec = assembly.GetType($"{Ns}.CanonicalJson")!;
        var hashMethod = codec.GetMethod("Hash", StaticAll, null, [typeof(byte[])], null)!;
        var isLowerHex = codec.GetMethod("IsLowerHex", StaticAll, null, [typeof(string), typeof(int)], null)!;

        // ---------- Q1-B: independent canonical-byte vectors ----------
        var vectors = new (string Name, Func<byte[]> Bytes, bool Accept, string? Failure)[]
        {
            ("minimal-object", () => Bytes("{}"), true, null),
            ("multi-key-canonical", () => Bytes("{\"a\":1,\"b\":\"x\",\"c\":true}"), true, null),
            ("key-order-wrong", () => Bytes("{\"b\":1,\"a\":2}"), false, "not strictly ascending"),
            ("duplicate-keys", () => Bytes("{\"a\":1,\"a\":2}"), false, "not strictly ascending"),
            ("empty-string", () => Bytes("{\"a\":\"\"}"), false, "CJSON-1 string"),
            ("printable-chars", () => Bytes("{\"a\":\" ~!#$%&'()*+,-./:;<=>?@[]^_`{|}~\"}"), true, null),
            ("backslash-escape", () => Bytes("{\"a\":\"x\\\\y\"}"), true, null),
            ("quote-escape", () => Bytes("{\"a\":\"x\\\"y\"}"), true, null),
            ("unsupported-newline-escape", () => Bytes("{\"a\":\"x\\ny\"}"), false, "CJSON-1 string"),
            ("raw-control-char", () => Concat(Bytes("{\"a\":\""), [0x01], Bytes("\"}")), false, "ASCII printable"),
            ("non-ascii-utf8", () => Bytes("{\"a\":\"caf\u00e9\"}"), false, "ASCII printable"),
            ("bom-prefix", () => Concat([0xEF, 0xBB, 0xBF], Bytes("{}")), false, "ASCII printable"),
            ("leading-space", () => Bytes(" {}"), false, "noncanonical"),
            ("trailing-space", () => Bytes("{} "), false, "noncanonical"),
            ("trailing-newline", () => Bytes("{}\n"), false, "ASCII printable"),
            ("trailing-data", () => Bytes("{} {}"), false, "invalid CJSON-1"),
            ("unsigned-zero", () => Bytes("{\"a\":0}"), true, null),
            ("unsigned-max", () => Bytes("{\"a\":9223372036854775807}"), true, null),
            ("leading-zero", () => Bytes("{\"a\":01}"), false, "invalid CJSON-1"),
            ("negative", () => Bytes("{\"a\":-1}"), false, "unsigned canonical integer"),
            ("decimal", () => Bytes("{\"a\":1.5}"), false, "unsigned canonical integer"),
            ("exponent", () => Bytes("{\"a\":1e2}"), false, "unsigned canonical integer"),
            ("overflow-int64", () => Bytes("{\"a\":9223372036854775808}"), false, "unsigned canonical integer"),
            ("null-value", () => Bytes("{\"a\":null}"), true, null),
            ("nested-array-object", () => Bytes("{\"a\":[1,{\"b\":2}]}"), true, null),
            ("depth-three", () => Bytes("{\"a\":[[[1]]]}"), true, null),
            ("depth-nine", () => Bytes("{\"a\":[[[[[[[[[1]]]]]]]]]}"), false, "invalid CJSON-1"),
            ("truncated", () => Bytes("{\"a\":"), false, "invalid CJSON-1"),
            ("empty-input", () => [], false, "CJSON-1 size"),
            ("root-array", () => Bytes("[]"), false, "root object"),
            ("unicode-escape", () => Bytes("{\"a\":\"\\u0041\"}"), false, "noncanonical"),
            ("slash-escape", () => Bytes("{\"a\":\"a\\/b\"}"), false, "noncanonical"),
        };
        foreach (var vector in vectors)
        {
            var bytes = vector.Bytes();
            var ok = ReflectionApi.TryParseBytes(bytes, out _, out var failure);
            var match = ok == vector.Accept && (vector.Failure is null || failure.Contains(vector.Failure, StringComparison.Ordinal));
            Check($"cjson/{vector.Name}", match, $"accept={ok} failure={failure}");
        }

        var atLimit = Bytes("{\"a\":\"" + new string('x', 65_536 - 8) + "\"}");
        var overLimit = Bytes("{\"a\":\"" + new string('x', 65_536 - 7) + "\"}");
        Check("cjson/size-at-limit", atLimit.Length == 65_536 && ReflectionApi.TryParseBytes(atLimit, out _, out _), $"len={atLimit.Length}");
        var overOk = ReflectionApi.TryParseBytes(overLimit, out _, out var overFailure);
        Check("cjson/size-over-limit", overLimit.Length == 65_537 && !overOk && overFailure == "CJSON-1 size", $"len={overLimit.Length} failure={overFailure}");
        for (var arrays = 6; arrays <= 9; arrays++)
        {
            var probe = "{\"a\":" + new string('[', arrays) + "1" + new string(']', arrays) + "}";
            var ok = ReflectionApi.TryParseBytes(Bytes(probe), out _, out var failure);
            Console.WriteLine($"INFO|depth-boundary|arrays={arrays}|accept={ok}|{failure}");
        }

        // ---------- Q1-C: determinism, immutability, known vectors ----------
        {
            var bytes = Bytes("{\"a\":1,\"b\":\"x\",\"c\":true}");
            var copy = (byte[])bytes.Clone();
            var ok1 = ReflectionApi.TryParseBytes(bytes, out var root1, out var f1);
            var ok2 = ReflectionApi.TryParseBytes(bytes, out var root2, out var f2);
            var encoded = ReflectionApi.Encode(root1);
            var h1 = (string)hashMethod.Invoke(null, [bytes])!;
            var h2 = (string)hashMethod.Invoke(null, [copy])!;
            Check("determinism/parse-twice", ok1 && ok2 && f1 == f2 && f1.Length == 0);
            Check("determinism/encode-equals-input", encoded.AsSpan().SequenceEqual(bytes));
            Check("determinism/hash-stable", h1 == h2 && h1 == ShaBytes(bytes));
            Check("determinism/no-input-mutation", bytes.AsSpan().SequenceEqual(copy));
            Check("determinism/hash-known-vectors",
                (string)hashMethod.Invoke(null, [Bytes("{}")])! == Sha("{}") && Sha("") == "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        }

        // ---------- Q1-D: reviewer-owned positive chains ----------
        var b2 = BuildB2Pre("case-r8");
        var b3 = BuildB3NoReleasePre("case-r9");
        var rel = BuildB3ReleasePost("case-r7");
        LogChain("b2-pre", b2);
        LogChain("b3-no-release-pre", b3);
        LogChain("b3-release-post", rel);
        Check("chain/b2-reviewer", ReflectionApi.TryChain(Bytes(b2.M), Bytes(b2.F), Bytes(b2.S), Bytes(b2.K), out var b2Result, out var b2Failure), b2Failure);
        Check("chain/b3-reviewer", ReflectionApi.TryChain(Bytes(b3.M), Bytes(b3.F), Bytes(b3.S), Bytes(b3.K), out var b3Result, out var b3Failure), b3Failure);
        Check("chain/b3-release-reviewer", ReflectionApi.TryChain(Bytes(rel.M), Bytes(rel.F), Bytes(rel.S), Bytes(rel.K), out var relResult, out var relFailure), relFailure);
        Check("chain/b2-independent-digests",
            Sha(b2.M) == b2.Hm && Sha(b2.F) == b2.Hf && Sha(b2.S) == b2.Hs && Sha(b2.K) == b2.Hk &&
            b2.Hm != b3.Hm && b2.Hk != b3.Hk);
        if (b3Result is not null)
            Check("chain/b3-verifier-digests",
                ReadChain(b3Result, "Hm") == Sha(b3.M) && ReadChain(b3Result, "Hf") == Sha(b3.F) &&
                ReadChain(b3Result, "Hs") == Sha(b3.S) && ReadChain(b3Result, "Hk") == Sha(b3.K));
        Check("artifact/reviewer-per-artifact",
            ReflectionApi.TryOne(ReflectionApi.TryManifest, Bytes(b3.M), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryPolicy, Bytes(b3.F), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryScript, Bytes(b3.S), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryCatalog, Bytes(b3.K), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryManifest, Bytes(b2.M), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryPolicy, Bytes(b2.F), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryScript, Bytes(b2.S), out _) &&
            ReflectionApi.TryOne(ReflectionApi.TryCatalog, Bytes(b2.K), out _));

        // ---------- Q1-F: self-consistent semantic invalidity (all hashes rebuilt) ----------
        void Semantic(string name, Chain variant, string expected)
        {
            var ok = ReflectionApi.TryChain(Bytes(variant.M), Bytes(variant.F), Bytes(variant.S), Bytes(variant.K), out _, out var failure);
            Check($"semantic/{name}", !ok && failure == expected, $"accept={ok} failure={failure} expected={expected}");
        }

        Semantic("case-id-disagreement", Rehash(b3 with { S = b3.S.Replace("case-r9", "case-r8") }), "case id disagreement");
        Semantic("phase-disagreement", Rehash(b3 with { F = ReplaceFirst(b3.F, "\"phase\":\"pre\"", "\"phase\":\"post\"") }), "phase disagreement");
        Semantic("topology-disagreement", Rehash(b3 with { K = ReplaceFirst(b3.K, "\"topology\":\"b3-integrated\"", "\"topology\":\"b2-lifeline\"") }), "topology disagreement");
        Semantic("session-limit-disagreement", Rehash(b3 with { F = ReplaceFirst(b3.F, "\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":500000000000") }), "session limit disagreement");
        Semantic("timeout-beyond-session", Rehash(b3 with
        {
            F = ReplaceFirst(b3.F, "\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":1000000000"),
            K = ReplaceFirst(b3.K, "\"maximumSessionNs\":600000000000", "\"maximumSessionNs\":1000000000"),
            S = b3.S.Replace("\"timeoutNs\":500000000", "\"timeoutNs\":2000000000"),
        }), "S action timeout exceeds the F maximum session");
        Semantic("release-template-disagreement", Rehash(rel with
        {
            F = ReplaceFirst(rel.F, "\"allowRelease\":true", "\"allowRelease\":false"),
            K = ReplaceFirst(rel.K, "\"allowRelease\":true", "\"allowRelease\":false"),
        }), "release template disagreement");
        Semantic("authority-field-injection", Rehash(b3 with { K = b3.K.Replace("\"allowRelease\":false,", "\"allowRelease\":false,\"approved\":true,") }), "K field set");
        Semantic("unsupported-k-version", Rehash(b3 with { K = ReplaceFirst(b3.K, "\"schemaVersion\":1", "\"schemaVersion\":2") }), "schemaVersion integer");
        Semantic("bad-boot-id", Rehash(b3 with { F = ReplaceFirst(b3.F, "\"bootId\":\"11111111-2222-4333-8444-555555555555\"", "\"bootId\":\"11111111-2222-4333-8444-55555555555A\"") }), "F bootId");
        var mOverflow = Rehash(b3 with { M = b3.M.Replace("\"length\":111", "\"length\":16777216").Replace("\"length\":222", "\"length\":16777216") });
        Check("semantic/m-aggregate-overflow",
            !ReflectionApi.TryChain(Bytes(mOverflow.M), Bytes(mOverflow.F), Bytes(mOverflow.S), Bytes(mOverflow.K), out _, out var mOverflowFailure) && mOverflowFailure == "M aggregate length",
            mOverflowFailure);
        var mExtension = b3 with { M = b3.M.Replace("\"destination\":\"/coordinator\",", "\"destination\":\"/coordinator\",\"extra\":1,") };
        Check("semantic/m-object-extension",
            !ReflectionApi.TryOne(ReflectionApi.TryManifest, Bytes(mExtension.M), out var mExtFailure) && mExtFailure == "M object field set",
            mExtFailure);

        // ---------- Q1-G: identity substitution ----------
        void CheckSub(string name, Chain variant, string expected)
        {
            var ok = ReflectionApi.TryChain(Bytes(variant.M), Bytes(variant.F), Bytes(variant.S), Bytes(variant.K), out _, out var failure);
            Check(name, !ok && failure == expected, $"accept={ok} failure={failure} expected={expected}");
        }

        CheckSub("sub/m-replaced", b3 with { M = b2.M }, "F fixtureManifestSha256 does not bind M");
        CheckSub("sub/s-replaced", b3 with { S = b2.S }, "S fixtureManifestSha256 does not bind M");
        CheckSub("sub/stale-k",
            b3 with { S = ReplaceFirst(b3.S, "\"timeoutNs\":500000000", "\"timeoutNs\":600000000") },
            "K actionScriptSha256 does not bind S");
        CheckSub("sub/cross-chain-k", b3 with { K = b2.K }, "K fixtureManifestSha256 does not bind M");
        var asserted = Rehash(b3 with { F = b3.F.Replace(b3.Hm, new string('f', 64), StringComparison.Ordinal) });
        CheckSub("sub/f-self-asserted-hash", asserted, "F fixtureManifestSha256 does not bind M");

        // ---------- Q1-H: F02 parity ----------
        var f02 = assembly.GetType("Gagamba.Execution.Linux.M1.Installed.FinalAcceptanceCodec")!;
        var parseRecord = f02.GetMethod("ParseRecord", StaticAll, null, [typeof(byte[])], null)!;
        var parseSelector = f02.GetMethod("ParseSelector", StaticAll, null, [typeof(byte[])], null)!;
        var f02Hash = f02.GetMethod("Hash", StaticAll, null, [typeof(byte[])], null)!;
        var f02IsLowerHex = f02.GetMethod("IsLowerHex", StaticAll, null, [typeof(string), typeof(int)], null)!;
        var recordBytes = Bytes(F02Record());
        var selectorBytes = Bytes(F02Selector((string)f02Hash.Invoke(null, [recordBytes])!));
        Check("f02/canonical-record-both-accept", ReflectionApi.TryParseBytes(recordBytes, out _, out _) && F02Accepts(parseRecord, recordBytes));
        Check("f02/canonical-selector-both-accept", ReflectionApi.TryParseBytes(selectorBytes, out _, out _) && F02Accepts(parseSelector, selectorBytes));
        Check("f02/hash-parity", (string)f02Hash.Invoke(null, [recordBytes])! == (string)hashMethod.Invoke(null, [recordBytes])!);
        Check("f02/lowerhex-parity",
            (bool)f02IsLowerHex.Invoke(null, ["abcdef", 6])! == (bool)isLowerHex.Invoke(null, ["abcdef", 6])! &&
            (bool)f02IsLowerHex.Invoke(null, ["ABC", 3])! == (bool)isLowerHex.Invoke(null, ["ABC", 3])!);
        {
            var record = F02Record();
            var h3 = new string('3', 64);
            var cases = new (string Name, byte[] Bytes)[]
            {
                ("trailing-space", Bytes(record + " ")),
                ("bom", Concat([0xEF, 0xBB, 0xBF], Bytes(record))),
                ("duplicate-field", Bytes(record.Replace("\"acceptance\":\"final-native-accepted\",", "\"acceptance\":\"final-native-accepted\",\"acceptance\":\"final-native-accepted\","))),
                ("unicode-escape", Bytes(record.Replace("\"Q1\"", "\"Q\\u0031\""))),
                ("float-schema", Bytes(record.Replace("\"schemaVersion\":1", "\"schemaVersion\":1.0"))),
            };
            foreach (var (name, bytes) in cases)
            {
                var mineReject = !ReflectionApi.TryParseBytes(bytes, out _, out _);
                var f02Reject = !F02Accepts(parseRecord, bytes);
                Check($"f02/reject-{name}", mineReject && f02Reject, $"mineReject={mineReject} f02Reject={f02Reject}");
            }

            var nullHost = Bytes(record.Replace($"\"hostId\":\"{h3}\"", "\"hostId\":null"));
            Check("f02/null-divergence-schema-scoped",
                ReflectionApi.TryParseBytes(nullHost, out _, out _) && !F02Accepts(parseRecord, nullHost));
            var extra = Bytes(record.Replace($"\"hostId\":\"{h3}\",", $"\"extra\":1,\"hostId\":\"{h3}\","));
            Check("f02/unknown-field-schema-scoped",
                ReflectionApi.TryParseBytes(extra, out _, out _) && !F02Accepts(parseRecord, extra));
        }

        // ---------- Q1-K: production reachability (independent scanner) ----------
        ScanReachability(assembly, "debug");
        if (File.Exists(releaseM1))
        {
            Console.WriteLine($"INFO|release_m1_sha256|{Sha256File(releaseM1)}");
            var alc = new AssemblyLoadContext("q1-release-scan", isCollectible: true);
            try
            {
                ScanReachability(alc.LoadFromAssemblyPath(releaseM1), "release");
            }
            finally
            {
                alc.Unload();
            }
        }
        else
        {
            Check("reachability/release-assembly-present", false, releaseM1);
        }

        // ---------- Q1-J: non-authoritative surface ----------
        AuthoritySurface(assembly);

        // ---------- Q1-I: robustness (mutations, truncations, oversize) ----------
        Robustness(b3);

        Console.WriteLine($"SUMMARY|pass={_pass}|fail={_fail}");
        return _fail == 0 ? 0 : 1;
    }

    // ---------------- helpers ----------------

    private static void LogChain(string name, Chain chain) =>
        Console.WriteLine($"INFO|reviewer-chain|{name}|" +
                          $"M_len={Bytes(chain.M).Length}|Hm={chain.Hm}|" +
                          $"F_len={Bytes(chain.F).Length}|Hf={chain.Hf}|" +
                          $"S_len={Bytes(chain.S).Length}|Hs={chain.Hs}|" +
                          $"K_len={Bytes(chain.K).Length}|Hk={chain.Hk}");

    private static string ReadChain(object chain, string property) =>
        (string)chain.GetType().GetProperty(property, InstanceAll)!.GetValue(chain)!;

    private static bool F02Accepts(MethodInfo method, byte[] bytes)
    {
        try
        {
            method.Invoke(null, [bytes]);
            return true;
        }
        catch (TargetInvocationException)
        {
            return false;
        }
    }

    private static void ScanReachability(Assembly assembly, string label)
    {
        try
        {
            var types = SafeTypes(assembly);
            var validationTypes = types.Where(t => t.Namespace == Ns).ToArray();
            Check($"reachability/{label}-validation-types-present", validationTypes.Length > 0, $"count={validationTypes.Length}");
            Check($"reachability/{label}-no-exported-types",
                assembly.GetExportedTypes().All(t => t.Namespace != Ns));
            var targets = new HashSet<int>();
            foreach (var type in validationTypes)
                foreach (var member in Members(type))
                    targets.Add(member.MetadataToken);
            var hits = 0;
            var scanned = 0;
            foreach (var type in types.Where(t => t.Namespace != Ns))
            {
                foreach (var method in Members(type))
                {
                    var body = method.GetMethodBody();
                    if (body is null) continue;
                    scanned++;
                    foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                        if (targets.Contains(token)) hits++;
                }
            }

            Check($"reachability/{label}-no-production-caller", hits == 0, $"hits={hits} scanned={scanned} targets={targets.Count}");

            var verifierType = assembly.GetType($"{Ns}.FrozenContractVerifier")!;
            var chainMethod = verifierType.GetMethod("TryVerifyChain", StaticAll)!;
            var manifestMethod = verifierType.GetMethod("VerifyManifest", StaticAll)!;
            var control = MethodTokens(chainMethod.GetMethodBody()!.GetILAsByteArray()!).Contains(manifestMethod.MetadataToken);
            Check($"reachability/{label}-scanner-positive-control", control);

            var forbidden = new[] { "System.IO.File", "System.IO.Directory", "System.IO.FileStream", "System.IO.Path", "System.Environment", "System.Diagnostics.Process", "System.Net.", "System.Runtime.InteropServices" };
            var badDependency = 0;
            foreach (var type in validationTypes)
                foreach (var method in Members(type))
                {
                    var body = method.GetMethodBody();
                    if (body is null) continue;
                    foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                    {
                        MethodBase? resolved;
                        try { resolved = method.Module.ResolveMethod(token); }
                        catch { continue; }
                        if (resolved is null) continue;
                        var declaring = resolved.DeclaringType?.FullName ?? "";
                        if (forbidden.Any(prefix => declaring.StartsWith(prefix, StringComparison.Ordinal))) badDependency++;
                    }
                }

            Check($"reachability/{label}-no-forbidden-dependencies", badDependency == 0, $"bad={badDependency}");
        }
        catch (ReflectionTypeLoadException exception)
        {
            Check($"reachability/{label}-type-load", false, exception.Message);
        }
    }

    private static void AuthoritySurface(Assembly assembly)
    {
        var types = SafeTypes(assembly).Where(t => t.Namespace == Ns).ToArray();
        var tokens = new[] { "Approved", "Authorized", "Permit", "Authority", "TrustedCapability", "Capability" };
        var badNames = 0;
        var badTypes = 0;
        var authorityReferences = 0;
        foreach (var type in types)
        {
            if (type.IsPublic || type.IsNestedPublic) badTypes++;
            if (tokens.Any(token => type.Name.Contains(token, StringComparison.Ordinal))) badNames++;
            foreach (var member in type.GetMembers(AllMembers))
            {
                if (tokens.Any(token => member.Name.Contains(token, StringComparison.Ordinal))) badNames++;
                if (member is MethodBase method)
                {
                    var candidates = method.GetParameters().Select(p => p.ParameterType)
                        .Append(method is MethodInfo info ? info.ReturnType : typeof(void));
                    foreach (var candidate in candidates)
                        if (candidate.FullName is { } full &&
                            (full.Contains("OwnerAuthorityContext") || full.Contains("LoadedInstallation") ||
                             full.Contains("Grant") || full.Contains("Approval")))
                            authorityReferences++;
                }
            }
        }

        Check("authority/no-authority-shaped-names", badNames == 0, $"bad={badNames}");
        Check("authority/no-public-types", badTypes == 0, $"bad={badTypes}");
        Check("authority/no-authority-type-references", authorityReferences == 0, $"refs={authorityReferences}");
        Check("authority/no-rwcp-or-approval-records",
            types.All(t => !t.Name.Contains("Review", StringComparison.Ordinal)
                && !t.Name.Contains("Acceptance", StringComparison.Ordinal)
                && !t.Name.Contains("Approval", StringComparison.Ordinal)));
    }

    private static void Robustness(Chain chain)
    {
        var slots = new[] { chain.M, chain.F, chain.S, chain.K }.Select(Bytes).ToArray();
        var threw = 0;
        var silent = 0;
        var acceptedBySlot = new int[4];
        for (var round = 0; round < 400; round++)
        {
            var slot = round % 4;
            var bytes = (byte[])slots[slot].Clone();
            var index = (round * 7) % bytes.Length;
            bytes[index] = (byte)((bytes[index] + round + 1) % 256);
            try
            {
                var ok = ReflectionApi.TryChain(
                    slot == 0 ? bytes : Bytes(chain.M),
                    slot == 1 ? bytes : Bytes(chain.F),
                    slot == 2 ? bytes : Bytes(chain.S),
                    slot == 3 ? bytes : Bytes(chain.K),
                    out _, out var failure);
                if (!ok && failure.Length == 0) silent++;
                if (ok) acceptedBySlot[slot]++;
            }
            catch
            {
                threw++;
            }
        }

        // A mutated M/F/S byte always changes a referenced identity and must refuse.
        // A mutated K byte that stays schema-valid is a legitimately different
        // catalog object (new Hk) and may verify on its own bindings; log it.
        Check("robustness/mutations-never-throw", threw == 0, $"threw={threw}");
        Check("robustness/mutations-fail-with-message", silent == 0, $"silent={silent}");
        Check("robustness/mutated-mfs-identities-refused",
            acceptedBySlot[0] == 0 && acceptedBySlot[1] == 0 && acceptedBySlot[2] == 0,
            $"m={acceptedBySlot[0]} f={acceptedBySlot[1]} s={acceptedBySlot[2]}");
        Console.WriteLine($"INFO|robustness|valid-new-k-identities={acceptedBySlot[3]}");

        var truncationThrew = 0;
        var k = Bytes(chain.K);
        for (var cut = 1; cut < 12; cut++)
        {
            var truncated = k[..(k.Length - cut * 5)];
            try
            {
                ReflectionApi.TryChain(Bytes(chain.M), Bytes(chain.F), Bytes(chain.S), truncated, out _, out _);
            }
            catch
            {
                truncationThrew++;
            }
        }

        Check("robustness/truncations-never-throw", truncationThrew == 0, $"threw={truncationThrew}");

        var oversize = new byte[65_537];
        Array.Fill(oversize, (byte)'a');
        try
        {
            var ok = ReflectionApi.TryChain(oversize, Bytes(chain.F), Bytes(chain.S), Bytes(chain.K), out _, out var failure);
            Check("robustness/oversize-rejected", !ok && failure == "CJSON-1 size", failure);
        }
        catch (Exception exception)
        {
            Check("robustness/oversize-rejected", false, exception.GetType().Name);
        }
    }

    // ---------------- reviewer-owned chain construction ----------------

    private static Chain BuildB2Pre(string caseId)
    {
        var m = "{\"caseId\":\"" + caseId + "\",\"objects\":[],\"schemaVersion\":1,\"topology\":\"b2-lifeline\"}";
        var hm = Sha(m);
        var f = "{\"allowRelease\":false,\"bootId\":\"99999999-8888-4777-8666-555555555555\",\"caseId\":\"" + caseId +
                "\",\"copy\":null,\"fixtureManifestSha256\":\"" + hm + "\",\"gid\":3000,\"hostId\":\"" + new string('4', 64) +
                "\",\"hostPaths\":null,\"inputEnrollmentId\":null,\"invocation\":null,\"maximumSessionNs\":600000000000,\"phase\":\"pre\",\"policyKind\":\"b2-lifeline-v1\",\"resources\":null,\"sandbox\":null,\"schemaVersion\":1,\"toolEnrollmentId\":null,\"topology\":\"b2-lifeline\",\"uid\":3000}";
        var hf = Sha(f);
        var s = "{\"actions\":[{\"index\":0,\"op\":\"connect-b2\",\"timeoutNs\":500000000},{\"index\":1,\"op\":\"inspect\",\"timeoutNs\":500000000},{\"index\":2,\"op\":\"stop\",\"timeoutNs\":500000000},{\"index\":3,\"op\":\"cleanup\",\"timeoutNs\":500000000}],\"caseId\":\"" + caseId +
                "\",\"fixtureManifestSha256\":\"" + hm + "\",\"fixturePolicySha256\":\"" + hf + "\",\"phase\":\"pre\",\"schemaVersion\":1,\"topology\":\"b2-lifeline\"}";
        var hs = Sha(s);
        var k = "{\"actionScriptSha256\":\"" + hs + "\",\"allowRelease\":false,\"artifactSetSha256\":\"" + new string('5', 64) +
                "\",\"caseId\":\"" + caseId + "\",\"controllerClosureSha256\":\"" + new string('6', 64) +
                "\",\"driverClosureSha256\":\"" + new string('7', 64) + "\",\"fixtureManifestSha256\":\"" + hm +
                "\",\"fixturePolicySha256\":\"" + hf + "\",\"hostId\":\"" + new string('4', 64) +
                "\",\"maximumSessionNs\":600000000000,\"phase\":\"pre\",\"platformInventorySha256\":\"" + new string('8', 64) +
                "\",\"profileId\":\"reviewer\",\"profileVersion\":\"1.0\",\"qualificationId\":null,\"qualificationRecordSha256\":null,\"rid\":\"linux-x64\",\"schemaVersion\":1,\"scope\":\"review\",\"sourceCommit\":\"" + new string('9', 40) +
                "\",\"sourceTree\":\"" + new string('a', 40) + "\",\"topology\":\"b2-lifeline\"}";
        return new Chain(m, f, s, k, hm, hf, hs, Sha(k));
    }

    private static Chain BuildB3NoReleasePre(string caseId)
    {
        var m = "{\"caseId\":\"" + caseId + "\",\"objects\":[" +
                "{\"destination\":\"/coordinator\",\"length\":111,\"mode\":\"0555\",\"path\":\"coordinator\",\"role\":\"coordinator\",\"sha256\":\"" + new string('b', 64) + "\"}," +
                "{\"destination\":\"fd:6\",\"length\":222,\"mode\":\"0444\",\"path\":\"filter\",\"role\":\"filter\",\"sha256\":\"" + new string('c', 64) + "\"}," +
                "{\"destination\":\"/input\",\"length\":333,\"mode\":\"0444\",\"path\":\"input\",\"role\":\"input\",\"sha256\":\"" + new string('d', 64) + "\"}," +
                "{\"destination\":\"/tool\",\"length\":444,\"mode\":\"0555\",\"path\":\"tool\",\"role\":\"tool\",\"sha256\":\"" + new string('e', 64) + "\"}" +
                "],\"schemaVersion\":1,\"topology\":\"b3-integrated\"}";
        var hm = Sha(m);
        var f = "{\"allowRelease\":false,\"bootId\":\"11111111-2222-4333-8444-555555555555\",\"caseId\":\"" + caseId + "\"," +
                "\"copy\":{\"aggregateBytes\":16777216,\"hooks\":false,\"maximumObjectBytes\":16777216,\"maximumObjects\":4}," +
                "\"fixtureManifestSha256\":\"" + hm + "\",\"gid\":2000,\"hostId\":\"" + new string('3', 64) + "\"," +
                "\"hostPaths\":{\"delegatedParent\":{\"device\":11,\"gid\":2000,\"inode\":5001,\"mode\":\"0755\",\"path\":\"/sys/fs/cgroup\",\"uid\":2000}," +
                "\"journalParent\":{\"device\":11,\"gid\":2000,\"inode\":5002,\"mode\":\"0700\",\"path\":\"/var/lib/gagamba/r-journal\",\"uid\":2000}," +
                "\"stagingParent\":{\"device\":11,\"gid\":2000,\"inode\":5003,\"mode\":\"0700\",\"path\":\"/var/lib/gagamba/r-staging\",\"uid\":2000}}," +
                "\"inputEnrollmentId\":\"input-r\",\"invocation\":{\"argv\":[\"--in\",\"/input\"],\"environment\":[{\"name\":\"HOME\",\"value\":\"/tmp\"},{\"name\":\"LANG\",\"value\":\"C\"}],\"inputPath\":\"/input\",\"mode\":\"work\",\"stdinEof\":true,\"toolPath\":\"/tool\",\"workDirectory\":\"/tmp\"}," +
                "\"maximumSessionNs\":600000000000,\"phase\":\"pre\",\"policyKind\":\"b3-static-fixture-v1\"," +
                "\"resources\":{\"captureAggregateBytes\":131072,\"configBytes\":16384,\"coordinatorStatusBytes\":16384,\"coreDisabled\":true,\"corePatternObserved\":null,\"expiresLeadNs\":1000000000,\"fileDescriptors\":64,\"finalDrainNs\":1000000000,\"helperStatusBytes\":16384,\"inodeQualification\":\"PendingQualification\",\"journalBytes\":65536,\"memoryBytes\":134217728,\"overallTimeoutNs\":30000000000,\"receiptBytes\":32768,\"runtimeDumpsDisabled\":false,\"setupTimeoutNs\":10000000000,\"shmBytes\":1048576,\"stagingBytes\":16777216,\"stagingObjects\":128,\"stderrBytes\":65536,\"stdoutBytes\":65536,\"stopAllowanceNs\":5000000000,\"swapBytes\":0,\"tasks\":48,\"tmpBytes\":7340032}," +
                "\"sandbox\":{\"capabilities\":[],\"namespaces\":[\"ipc\",\"mount\",\"net\",\"pid\",\"user\",\"uts\"],\"networkQualification\":\"PendingQualification\",\"noAmbientEnvironment\":true,\"noExtraInheritedFds\":true,\"noIpcEndpoints\":true,\"noNewPrivs\":true,\"readOnlyDestinations\":[\"/coordinator\",\"/input\",\"/tool\"],\"scratchNoexec\":false,\"toolExecutable\":true,\"writableStores\":[{\"bytes\":1048576,\"hasInodeBudget\":false,\"inodeBudget\":0,\"path\":\"/dev/shm\"},{\"bytes\":7340032,\"hasInodeBudget\":false,\"inodeBudget\":0,\"path\":\"/tmp\"}]}," +
                "\"schemaVersion\":1,\"toolEnrollmentId\":\"tool-r\",\"topology\":\"b3-integrated\",\"uid\":2000}";
        var hf = Sha(f);
        var s = "{\"actions\":[{\"index\":0,\"op\":\"prepare-b3\",\"timeoutNs\":500000000},{\"index\":1,\"op\":\"inspect\",\"timeoutNs\":500000000},{\"index\":2,\"op\":\"stop\",\"timeoutNs\":500000000},{\"index\":3,\"op\":\"cleanup\",\"timeoutNs\":500000000}],\"caseId\":\"" + caseId +
                "\",\"fixtureManifestSha256\":\"" + hm + "\",\"fixturePolicySha256\":\"" + hf + "\",\"phase\":\"pre\",\"schemaVersion\":1,\"topology\":\"b3-integrated\"}";
        var hs = Sha(s);
        var k = "{\"actionScriptSha256\":\"" + hs + "\",\"allowRelease\":false,\"artifactSetSha256\":\"" + new string('5', 64) +
                "\",\"caseId\":\"" + caseId + "\",\"controllerClosureSha256\":\"" + new string('6', 64) +
                "\",\"driverClosureSha256\":\"" + new string('7', 64) + "\",\"fixtureManifestSha256\":\"" + hm +
                "\",\"fixturePolicySha256\":\"" + hf + "\",\"hostId\":\"" + new string('3', 64) +
                "\",\"maximumSessionNs\":600000000000,\"phase\":\"pre\",\"platformInventorySha256\":\"" + new string('8', 64) +
                "\",\"profileId\":\"reviewer\",\"profileVersion\":\"1.0\",\"qualificationId\":null,\"qualificationRecordSha256\":null,\"rid\":\"linux-x64\",\"schemaVersion\":1,\"scope\":\"review\",\"sourceCommit\":\"" + new string('9', 40) +
                "\",\"sourceTree\":\"" + new string('a', 40) + "\",\"topology\":\"b3-integrated\"}";
        return new Chain(m, f, s, k, hm, hf, hs, Sha(k));
    }

    private static Chain BuildB3ReleasePost(string caseId)
    {
        var pre = BuildB3NoReleasePre(caseId);
        var m = pre.M;
        var hm = Sha(m);
        var f = ReplaceFirst(pre.F, "\"phase\":\"pre\"", "\"phase\":\"post\"");
        f = ReplaceFirst(f, "\"allowRelease\":false", "\"allowRelease\":true");
        var hf = Sha(f);
        var s = ReplaceFirst(pre.S,
            "\"actions\":[{\"index\":0,\"op\":\"prepare-b3\",\"timeoutNs\":500000000},{\"index\":1,\"op\":\"inspect\",\"timeoutNs\":500000000},{\"index\":2,\"op\":\"stop\",\"timeoutNs\":500000000},{\"index\":3,\"op\":\"cleanup\",\"timeoutNs\":500000000}]",
            "\"actions\":[{\"index\":0,\"op\":\"prepare-b3\",\"timeoutNs\":500000000},{\"index\":1,\"op\":\"inspect\",\"timeoutNs\":500000000},{\"index\":2,\"op\":\"release\",\"timeoutNs\":500000000},{\"index\":3,\"op\":\"inspect\",\"timeoutNs\":500000000},{\"index\":4,\"op\":\"stop\",\"timeoutNs\":500000000},{\"index\":5,\"op\":\"cleanup\",\"timeoutNs\":500000000}]");
        s = ReplaceFirst(s, "\"phase\":\"pre\"", "\"phase\":\"post\"");
        s = s.Replace(pre.Hf, hf, StringComparison.Ordinal);
        var hs = Sha(s);
        var k = ReplaceFirst(pre.K, "\"allowRelease\":false", "\"allowRelease\":true");
        k = ReplaceFirst(k, "\"phase\":\"pre\"", "\"phase\":\"post\"");
        k = ReplaceFirst(k, "\"qualificationId\":null,\"qualificationRecordSha256\":null",
            "\"qualificationId\":\"Q-r\",\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"");
        k = k.Replace(pre.Hf, hf, StringComparison.Ordinal);
        k = k.Replace(pre.Hs, hs, StringComparison.Ordinal);
        return new Chain(m, f, s, k, hm, hf, hs, Sha(k));
    }

    private static Chain Rehash(Chain chain)
    {
        var m = chain.M;
        var hm = Sha(m);
        var f = chain.F.Replace(chain.Hm, hm, StringComparison.Ordinal);
        var s = chain.S.Replace(chain.Hm, hm, StringComparison.Ordinal);
        var k = chain.K.Replace(chain.Hm, hm, StringComparison.Ordinal);
        var hf = Sha(f);
        s = s.Replace(chain.Hf, hf, StringComparison.Ordinal);
        k = k.Replace(chain.Hf, hf, StringComparison.Ordinal);
        var hs = Sha(s);
        k = k.Replace(chain.Hs, hs, StringComparison.Ordinal);
        return new Chain(m, f, s, k, hm, hf, hs, Sha(k));
    }

    private static string ReplaceFirst(string source, string find, string replace)
    {
        var index = source.IndexOf(find, StringComparison.Ordinal);
        if (index < 0) throw new InvalidOperationException($"not found: {find}");
        return source[..index] + replace + source[(index + find.Length)..];
    }

    private static string Sha(string text) => ShaBytes(Encoding.UTF8.GetBytes(text));

    private static string ShaBytes(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Concat(byte[] first, byte[] second, byte[] third)
    {
        var result = new byte[first.Length + second.Length + third.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        third.CopyTo(result, first.Length + second.Length);
        return result;
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    private static string F02Record()
    {
        string H(char c) => new(c, 64);
        string H40(char c) => new(c, 40);
        return "{" +
               "\"acceptance\":\"final-native-accepted\"," +
               "\"acceptedAtUtc\":\"2026-11-02T00:00:00Z\"," +
               $"\"artifactSetSha256\":\"{H('1')}\"," +
               "\"buildConfiguration\":\"Production\"," +
               $"\"candidateEvidenceSha256\":\"{H('2')}\"," +
               $"\"hostId\":\"{H('3')}\"," +
               $"\"independentReviewSha256\":\"{H('4')}\"," +
               "\"limitations\":[\"portable test only\"]," +
               $"\"nativeGateResultsSha256\":\"{H('5')}\"," +
               $"\"ownerDecisionSha256\":\"{H('6')}\"," +
               $"\"platformInventorySha256\":\"{H('7')}\"," +
               $"\"postpublicationEvidenceSha256\":\"{H('8')}\"," +
               $"\"prepublicationEvidenceSha256\":\"{H('9')}\"," +
               "\"profileId\":\"profile\"," +
               "\"profileVersion\":\"1.0\"," +
               "\"qualificationId\":\"Q1\"," +
               $"\"qualificationPlanSha256\":\"{H('a')}\"," +
               $"\"qualificationRecordSha256\":\"{H('b')}\"," +
               $"\"reviewCommit\":\"{H40('c')}\"," +
               "\"rid\":\"linux-x64\"," +
               "\"schemaVersion\":1," +
               "\"scope\":\"test\"," +
               $"\"sourceCommit\":\"{H40('d')}\"," +
               $"\"sourceTree\":\"{H40('e')}\"" +
               "}";
    }

    private static string F02Selector(string finalRecordSha256)
    {
        string H(char c) => new(c, 64);
        return "{" +
               "\"accepted\":true," +
               $"\"artifactSetSha256\":\"{H('1')}\"," +
               $"\"finalRecordSha256\":\"{finalRecordSha256}\"," +
               $"\"ownerDecisionSha256\":\"{H('6')}\"," +
               "\"qualificationId\":\"Q1\"," +
               $"\"qualificationRecordSha256\":\"{H('b')}\"," +
               "\"schemaVersion\":1" +
               "}";
    }

    private static Type[] SafeTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException exception) { return exception.Types.Where(t => t is not null).Cast<Type>().ToArray(); }
    }

    private static IEnumerable<MethodBase> Members(Type type) =>
        type.GetMethods(AllMembers).Concat<MethodBase>(type.GetConstructors(AllMembers));

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    private static IEnumerable<int> MethodTokens(byte[] il)
    {
        var position = 0;
        while (position < il.Length)
        {
            ushort key = il[position++];
            if (key == 0xfe)
            {
                if (position >= il.Length) yield break;
                key = (ushort)(0xfe00 | il[position++]);
            }

            if (!OpCodesByValue.TryGetValue(key, out var opcode)) yield break;
            var operand = ReadOperand(opcode, il, ref position);
            if (opcode.OperandType == OperandType.InlineMethod) yield return unchecked((int)operand);
        }
    }

    private static long ReadOperand(OpCode opcode, byte[] il, ref int position) =>
        opcode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => ReadByte(il, ref position),
            OperandType.InlineVar => ReadUInt16(il, ref position),
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig
                or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or OperandType.InlineI => ReadInt32(il, ref position),
            OperandType.ShortInlineR => ReadInt32(il, ref position),
            OperandType.InlineI8 or OperandType.InlineR => ReadInt64(il, ref position),
            OperandType.InlineSwitch => ReadSwitch(il, ref position),
            _ => throw new BadImageFormatException($"unsupported operand {opcode.OperandType}"),
        };

    private static byte ReadByte(byte[] il, ref int position)
    {
        if (position + 1 > il.Length) throw new BadImageFormatException("truncated");
        return il[position++];
    }

    private static ushort ReadUInt16(byte[] il, ref int position)
    {
        if (position + 2 > il.Length) throw new BadImageFormatException("truncated");
        var value = BitConverter.ToUInt16(il, position);
        position += 2;
        return value;
    }

    private static int ReadInt32(byte[] il, ref int position)
    {
        if (position + 4 > il.Length) throw new BadImageFormatException("truncated");
        var value = BitConverter.ToInt32(il, position);
        position += 4;
        return value;
    }

    private static long ReadInt64(byte[] il, ref int position)
    {
        if (position + 8 > il.Length) throw new BadImageFormatException("truncated");
        var value = BitConverter.ToInt64(il, position);
        position += 8;
        return value;
    }

    private static long ReadSwitch(byte[] il, ref int position)
    {
        if (position + 4 > il.Length) throw new BadImageFormatException("truncated");
        var count = BitConverter.ToInt32(il, position);
        position += 4;
        if (count < 0 || count > (il.Length - position) / 4) throw new BadImageFormatException("invalid switch");
        position += count * 4;
        return count;
    }

    private sealed record Chain(string M, string F, string S, string K, string Hm, string Hf, string Hs, string Hk);
}

/// <summary>Static reflection facade so robustness helpers can invoke the chain verifier.</summary>
internal static class ReflectionApi
{
    private static readonly BindingFlags StaticAll = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static MethodInfo _tryChain = null!;
    private static MethodInfo _tryParse = null!;

    internal static MethodInfo TryManifest { get; private set; } = null!;
    internal static MethodInfo TryPolicy { get; private set; } = null!;
    internal static MethodInfo TryScript { get; private set; } = null!;
    internal static MethodInfo TryCatalog { get; private set; } = null!;
    internal static MethodInfo EncodeMethod { get; private set; } = null!;

    internal static void Bind(Assembly assembly, string debugPath)
    {
        var codec = assembly.GetType("Gagamba.Execution.Linux.M1.Validation.CanonicalJson")!;
        var verifier = assembly.GetType("Gagamba.Execution.Linux.M1.Validation.FrozenContractVerifier")!;
        EncodeMethod = codec.GetMethod("Encode", StaticAll, null, [typeof(JsonElement)], null)!;
        _tryParse = codec.GetMethod("TryParse", StaticAll, null, [typeof(byte[]), typeof(JsonElement).MakeByRefType(), typeof(string).MakeByRefType()], null)!;
        Type Contract(string name) => assembly.GetType($"Gagamba.Execution.Linux.M1.Validation.{name}")!;
        TryManifest = verifier.GetMethod("TryVerifyManifest", StaticAll, null, [typeof(byte[]), Contract("FixtureManifestContract").MakeByRefType(), typeof(string).MakeByRefType()], null)!;
        TryPolicy = verifier.GetMethod("TryVerifyPolicy", StaticAll, null, [typeof(byte[]), Contract("FixturePolicyContract").MakeByRefType(), typeof(string).MakeByRefType()], null)!;
        TryScript = verifier.GetMethod("TryVerifyActionScript", StaticAll, null, [typeof(byte[]), Contract("ActionScriptContract").MakeByRefType(), typeof(string).MakeByRefType()], null)!;
        TryCatalog = verifier.GetMethod("TryVerifyCatalog", StaticAll, null, [typeof(byte[]), Contract("CatalogContract").MakeByRefType(), typeof(string).MakeByRefType()], null)!;
        _tryChain = verifier.GetMethod("TryVerifyChain", StaticAll, null,
            [typeof(byte[]), typeof(byte[]), typeof(byte[]), typeof(byte[]), Contract("FrozenContractChain").MakeByRefType(), typeof(string).MakeByRefType()], null)!;
    }

    internal static bool TryParseBytes(byte[] bytes, out JsonElement root, out string failure)
    {
        var call = new object?[] { bytes, null, null };
        var ok = (bool)_tryParse.Invoke(null, call)!;
        root = ok ? (JsonElement)call[1]! : default;
        failure = (string?)call[2] ?? string.Empty;
        return ok;
    }

    internal static byte[] Encode(JsonElement element) => (byte[])EncodeMethod.Invoke(null, [element])!;

    internal static bool TryOne(MethodInfo method, byte[] bytes, out string failure)
    {
        var call = new object?[] { bytes, null, null };
        var ok = (bool)method.Invoke(null, call)!;
        failure = (string?)call[2] ?? string.Empty;
        return ok;
    }

    internal static bool TryChain(byte[] m, byte[] f, byte[] s, byte[] k, out object? chain, out string failure)
    {
        var call = new object?[] { m, f, s, k, null, null };
        var ok = (bool)_tryChain.Invoke(null, call)!;
        chain = call[4];
        failure = (string?)call[5] ?? string.Empty;
        return ok;
    }
}
