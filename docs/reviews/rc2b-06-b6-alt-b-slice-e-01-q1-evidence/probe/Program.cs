using System.Reflection;

// Independent E-1 qualification probe (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-Q1).
// Pure reflection; does not reference M1 at compile time. Exercises the
// construction boundary, the internal evidence cross-check, refusal behavior,
// and the Debug/Release seam separation. Exit 0 = observer ran; decisions are
// printed as observations, not assertions of the producer's claims.
static class Probe
{
    static int _fail;
    static void Obs(string id, string result, string detail = "")
    {
        Console.WriteLine($"{id}\t{result}\t{detail}");
    }
    static void Check(string id, bool ok, string detail = "")
    {
        if (!ok) _fail++;
        Obs(id, ok ? "PASS" : "FAIL", detail);
    }

    static Exception Unwrap(Exception ex) => ex is TargetInvocationException tie && tie.InnerException is not null ? tie.InnerException : ex;
    static string Type(bool b) => b ? "accepted" : "refused";

    static int Main(string[] args)
    {
        var debugDll = args[0];
        var releaseDll = args[1];
        var manifestPath = args[2];

        var asm = Assembly.LoadFrom(debugDll);
        var projType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProjection")!;
        var evType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstalledIntegrityEvidence")!;
        var manifestType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstalledManifest")!;
        var provType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProvenance")!;
        var identityType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstallationIdentity")!;

        Console.WriteLine("=== TYPE SURFACE ===");
        var ctors = projType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Check("A1.private-ctors", ctors.Length > 0 && ctors.All(c => c.IsPrivate), $"ctors={ctors.Length}");

        var factories = projType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.ReturnType == projType).ToArray();
        Obs("A2.factories", string.Join(",", factories.Select(f => f.Name + "(" + string.Join(",", f.GetParameters().Select(p => p.ParameterType.Name)) + ")")), "");
        Check("A2b.no-bool-or-result-factory", factories.All(f => !f.GetParameters().Any(p => p.ParameterType == typeof(bool) ||
            p.ParameterType.Name.Contains("InstalledResult"))), "");
        Check("A2c.is-internal-not-public", !projType.IsPublic && !projType.IsNestedPublic, "");

        var props = projType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Check("A3.read-only-properties", props.All(p => p.SetMethod is null), string.Join(",", props.Select(p => p.Name)));

        var evCtors = evType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var evCtor5 = evCtors.Single(c => c.GetParameters().Length == 5);
        Check("A4.evidence-is-internal-record", evCtor5.IsAssembly || evCtor5.IsPublic, $"visibility={evCtor5.Attributes}");

        // ---- build a genuine manifest from the canonical golden vector ----
        var manifestBytes = File.ReadAllBytes(manifestPath);
        var parse = manifestType.GetMethod("Parse", BindingFlags.NonPublic | BindingFlags.Static)!;
        var manifest = parse.Invoke(null, new object[] { manifestBytes })!;
        var digest = (string)manifestType.GetProperty("Digest")!.GetValue(manifest)!;
        var roleHash = identityType.GetMethod("RoleHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ownerClosure = (string)roleHash.Invoke(null, new object[] { manifest, (byte)1 })!;
        var recClosure = (string)roleHash.Invoke(null, new object[] { manifest, (byte)0 })!;
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(manifestBytes);
        var runtimeInv = jsonDoc.RootElement.GetProperty("platform").GetProperty("runtimeInventorySha256").GetString()!;

        object MakeEvidence(string d, object m, string oc, string rc, string ri) =>
            evCtor5.Invoke(new object[] { d, m, oc, rc, ri });

        var fromEvidence = projType.GetMethod("FromEvidence", BindingFlags.NonPublic | BindingFlags.Static)!;
        var enumValues = Enum.GetValues(provType);
        var protectedProv = enumValues.GetValue(0)!;
        var testProv = enumValues.GetValue(1)!;

        Console.WriteLine("=== CONSTRUCTION / EVIDENCE CROSS-CHECK ===");
        object? InvokeFrom(object ev, object prov) { try { return fromEvidence.Invoke(null, new object[] { ev, prov }); } catch (Exception ex) { return Unwrap(ex); } }

        var good = MakeEvidence(digest, manifest, ownerClosure, recClosure, runtimeInv);
        var goodResult = InvokeFrom(good, protectedProv);
        Check("B1.consistent-evidence-builds", goodResult is not Exception, goodResult is Exception e ? e.GetType().Name : "projection");

        var wrongDigest = MakeEvidence(new string('e', 64), manifest, ownerClosure, recClosure, runtimeInv);
        Check("B2.mismatched-artifact-digest-refused", InvokeFrom(wrongDigest, protectedProv) is InvalidDataException, "");

        var shortDigest = MakeEvidence(new string('e', 63), manifest, ownerClosure, recClosure, runtimeInv);
        Check("B3.short-digest-refused", InvokeFrom(shortDigest, protectedProv) is InvalidDataException, "");

        var upperDigest = MakeEvidence(digest.ToUpperInvariant(), manifest, ownerClosure, recClosure, runtimeInv);
        Check("B4.uppercase-digest-refused", InvokeFrom(upperDigest, protectedProv) is InvalidDataException, "");

        var wrongOwner = MakeEvidence(digest, manifest, new string('a', 64), recClosure, runtimeInv);
        var wrongOwnerResult = InvokeFrom(wrongOwner, protectedProv);
        Obs("B5.wrong-but-wellformed-owner-closure", wrongOwnerResult is InvalidDataException ? "refused" : "ACCEPTED",
            "cross-check of closure against manifest.RoleHash is absent in FromEvidence");

        var wrongRec = MakeEvidence(digest, manifest, ownerClosure, new string('b', 64), runtimeInv);
        Obs("B6.wrong-but-wellformed-recovery-closure", InvokeFrom(wrongRec, protectedProv) is InvalidDataException ? "refused" : "ACCEPTED", "");

        var wrongRuntime = MakeEvidence(digest, manifest, ownerClosure, recClosure, new string('c', 64));
        Obs("B7.wrong-but-wellformed-runtime-inventory", InvokeFrom(wrongRuntime, protectedProv) is InvalidDataException ? "refused" : "ACCEPTED", "");

        var nonHex = MakeEvidence(digest, manifest, new string('z', 64), recClosure, runtimeInv);
        Check("B8.non-hex-closure-refused", InvokeFrom(nonHex, protectedProv) is InvalidDataException, "");

        var badProv = Enum.ToObject(provType, 999);
        Check("B9.undefined-provenance-refused", InvokeFrom(good, badProv) is InvalidDataException, "");

        Check("B10.null-manifest-refused", InvokeFrom(MakeEvidence(digest, null!, ownerClosure, recClosure, runtimeInv), protectedProv) is InvalidDataException, "");
        Check("B11.null-evidence-refused", InvokeFrom(null!, protectedProv) is InvalidDataException, "");

        Console.WriteLine("=== TEST SEAM / REFUSAL / IMMUTABILITY ===");
        try
        {
        var forTest = projType.GetMethod("ForTest", BindingFlags.NonPublic | BindingFlags.Static);
        Obs("C0.ForTest-found", forTest is null ? "NULL" : "present", "");
        const BindingFlags NP = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var ftResult = forTest!.Invoke(null, new object[] { good })!;
        Obs("C0b.ftResult-null", ftResult is null ? "NULL" : "ok", ftResult?.GetType().Name ?? "");
        var provProp = projType.GetProperty("Provenance", NP)!;
        var qualIdProp = projType.GetProperty("QualificationId", NP)!;
        var qualRecProp = projType.GetProperty("QualificationRecordSha256", NP)!;
        Check("C1.ForTest-tagged-test-only", testProv.Equals(provProp.GetValue(ftResult)), "");

        var qualId = qualIdProp.GetValue(ftResult);
        var qualRec = qualRecProp.GetValue(ftResult);
        Check("C2.null-qualification-association", qualId is null && qualRec is null, "");

        var goodProj = (object)goodResult;
        Check("C3.protected-route-tags-protected", protectedProv.Equals(provProp.GetValue(goodProj)), "");
        Check("C4.protected-result-null-qual", qualIdProp.GetValue(goodProj) is null &&
            qualRecProp.GetValue(goodProj) is null, "");

        var verify = projType.GetMethod("Verify", BindingFlags.NonPublic | BindingFlags.Static)!;
        object? verifyResult; try { verifyResult = verify.Invoke(null, Array.Empty<object>()); } catch (Exception ex) { verifyResult = Unwrap(ex); }
        Check("C5.Verify-refuses-unprovisioned", verifyResult is InvalidDataException, verifyResult is Exception ex2 ? ex2.GetType().Name : "projection");
        }
        catch (Exception ex) { Obs("C.error", ex.GetType().Name, ex.StackTrace ?? ex.Message); }

        Console.WriteLine("=== RELEASE ASSEMBLY ===");
        var alc = new System.Runtime.Loader.AssemblyLoadContext("release-only", isCollectible: true);
        var rel = alc.LoadFromAssemblyPath(releaseDll);
        var relProj = rel.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProjection")!;
        Check("D1.release-has-projection-type", relProj is not null, "");
        var relForTest = relProj.GetMethod("ForTest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Check("D2.release-no-ForTest-seam", relForTest is null, "");
        Check("D3.release-Verify-present", relProj.GetMethod("Verify", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is not null, "");
        Check("D4.release-private-ctors", relProj.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).All(c => c.IsPrivate), "");

        Console.WriteLine($"SUMMARY\tfailures={_fail}");
        return 0;
    }
}
