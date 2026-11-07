using System.Reflection;
using System.Text.Json;

// Independent F-01 Q2 adversarial probe (GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-Q2).
// Pure reflection; no compile-time reference to M1. Exercises the semantic
// construction boundary with independently designed cases, including swapped
// owner/recovery roles and role-specific artifact substitution, which the
// producer's six tests do not cover.
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
    static string Refusal(Exception? ex) => ex is null ? "ACCEPTED" : "refused(" + ex.GetType().Name + ")";

    const BindingFlags NP = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    static int Main(string[] args)
    {
        var debugDll = args[0];
        var releaseDll = args[1];
        var manifestPath = args[2];

        var asm = Assembly.LoadFrom(debugDll);
        var projType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProjection")!;
        var evType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstalledIntegrityEvidence")!;
        var manifestType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstalledManifest")!;
        var artifactType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstalledArtifact")!;
        var provType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProvenance")!;
        var identityType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstallationIdentity")!;
        var installJsonType = asm.GetType("Gagamba.Execution.Linux.M1.Installed.InstallJson")!;

        var manifestCtor = manifestType.GetConstructors(NP).Single(c => c.GetParameters().Length == 5);
        var artifactCtor = artifactType.GetConstructors(NP).Single(c => c.GetParameters().Length == 6);
        var evCtor = evType.GetConstructors(NP).Single(c => c.GetParameters().Length == 5);
        var fromEvidence = projType.GetMethod("FromEvidence", BindingFlags.NonPublic | BindingFlags.Static)!;
        var roleHash = identityType.GetMethod("RoleHash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var isHex = installJsonType.GetMethod("IsHex", BindingFlags.NonPublic | BindingFlags.Static)!;
        var installHex = installJsonType.GetMethod("Hex", BindingFlags.NonPublic | BindingFlags.Static)!;
        var protectedProv = Enum.GetValues(provType).GetValue(0)!;

        var manifestBytes = File.ReadAllBytes(manifestPath);
        var manifest = manifestType.GetMethod("Parse", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { manifestBytes })!;
        var digest = (string)manifestType.GetProperty("Digest")!.GetValue(manifest)!;
        var sourceCommit = (string)manifestType.GetProperty("SourceCommit")!.GetValue(manifest)!;
        var sourceTree = (string)manifestType.GetProperty("SourceTree")!.GetValue(manifest)!;
        var platform = manifestType.GetProperty("Platform")!.GetValue(manifest)!;
        var artifacts = (System.Collections.IEnumerable)manifestType.GetProperty("Artifacts")!.GetValue(manifest)!;
        using var json = JsonDocument.Parse(manifestBytes);
        var runtimeInv = json.RootElement.GetProperty("platform").GetProperty("runtimeInventorySha256").GetString()!;

        string RH(object m, byte role) => (string)roleHash.Invoke(null, new object[] { m, role })!;
        Array TypedArray(IEnumerable<object> items)
        {
            var list = items.ToList();
            var array = Array.CreateInstance(artifactType, list.Count);
            for (int i = 0; i < list.Count; i++) array.SetValue(list[i], i);
            return array;
        }
        object MakeManifest(object plat, object arts) => manifestCtor.Invoke(new[] { (object)digest, sourceCommit, sourceTree, plat, arts });
        object MakeArtifact(string role, string kind, string path, string sha, long len, string mode) => artifactCtor.Invoke(new object[] { role, kind, path, sha, len, mode });
        object MakeEvidence(string d, object m, string oc, string rc, string ri) => evCtor.Invoke(new object[] { d, m, oc, rc, ri });
        object? Invoke(object ev) { try { return fromEvidence.Invoke(null, new object[] { ev, protectedProv }); } catch (Exception ex) { return Unwrap(ex); } }

        // Materialize artifact list so we can rebuild a mutated one.
        var artifactList = artifacts.Cast<object>().ToList();
        object OwnerEntry() => artifactList.Single(a => (string)artifactType.GetProperty("Role")!.GetValue(a)! == "owner-entry");

        Console.WriteLine("=== SEMANTIC CONSTRUCTION (independent cases) ===");

        var good = MakeEvidence(digest, manifest, RH(manifest, 1), RH(manifest, 0), runtimeInv);
        Check("P1.valid-evidence-accepted", Invoke(good) is not Exception, Refusal(Invoke(good) as Exception));

        Check("P2.B5-wrong-owner-refused", Invoke(MakeEvidence(digest, manifest, new string('a', 64), RH(manifest, 0), runtimeInv)) is InvalidDataException, "");
        Check("P3.B6-wrong-recovery-refused", Invoke(MakeEvidence(digest, manifest, RH(manifest, 1), new string('b', 64), runtimeInv)) is InvalidDataException, "");
        Check("P4.B7-wrong-runtime-refused", Invoke(MakeEvidence(digest, manifest, RH(manifest, 1), RH(manifest, 0), new string('c', 64))) is InvalidDataException, "");

        var ownerIsDistinctFromRecovery = RH(manifest, 1) != RH(manifest, 0);
        Obs("P5.roles-are-distinct", ownerIsDistinctFromRecovery ? "distinct" : "COINCIDENT", "owner vs recovery RoleHash");
        Check("P5b.swapped-owner-recovery-refused",
            Invoke(MakeEvidence(digest, manifest, RH(manifest, 0), RH(manifest, 1), runtimeInv)) is InvalidDataException,
            "swapped closures");

        // Role-specific artifact substitution: change the owner-entry hash only.
        var owner = OwnerEntry();
        var mutatedOwner = MakeArtifact("owner-entry",
            (string)artifactType.GetProperty("Kind")!.GetValue(owner)!,
            (string)artifactType.GetProperty("Path")!.GetValue(owner)!,
            new string('f', 64),
            (long)artifactType.GetProperty("Length")!.GetValue(owner)!,
            (string)artifactType.GetProperty("Mode")!.GetValue(owner)!);
        var replaced = TypedArray(artifactList.Select(a => (string)artifactType.GetProperty("Role")!.GetValue(a)! == "owner-entry" ? mutatedOwner : a));
        var mutatedManifest = MakeManifest(platform, replaced);
        Obs("P6.owner-artifact-substitution-changes-rolehash", RH(mutatedManifest, 1) != RH(manifest, 1) ? "changed" : "UNCHANGED", "");
        Check("P6b.owner-artifact-substitution-with-old-closure-refused",
            Invoke(MakeEvidence(digest, mutatedManifest, RH(manifest, 1), RH(manifest, 0), runtimeInv)) is InvalidDataException, "");
        Check("P6c.recovery-unchanged-still-valid-on-owner-mutation",
            Invoke(MakeEvidence(digest, mutatedManifest, RH(mutatedManifest, 1), RH(mutatedManifest, 0), runtimeInv)) is not Exception,
            "consistent tuple for mutated manifest builds");

        Check("P7.substituted-digest-refused",
            Invoke(MakeEvidence(new string('e', 64), manifest, RH(manifest, 1), RH(manifest, 0), runtimeInv)) is InvalidDataException, "");

        var emptyManifest = MakeManifest(platform, TypedArray(Enumerable.Empty<object>()));
        var missingRoles = Invoke(MakeEvidence(digest, emptyManifest, new string('a', 64), new string('b', 64), new string('c', 64)));
        Check("P8.missing-role-material-fails-closed", missingRoles is Exception, Refusal(missingRoles as Exception));

        using var emptyPlatformDoc = JsonDocument.Parse("{}");
        var noRuntime = MakeManifest(emptyPlatformDoc.RootElement.Clone(), TypedArray(artifactList));
        var missingRuntime = Invoke(MakeEvidence(digest, noRuntime, RH(noRuntime, 1), RH(noRuntime, 0), new string('a', 64)));
        Check("P9.missing-runtime-metadata-fails-closed", missingRuntime is Exception, Refusal(missingRuntime as Exception));

        Check("P10.contradictory-digest-and-closure-refused",
            Invoke(MakeEvidence(new string('e', 64), manifest, new string('a', 64), RH(manifest, 0), runtimeInv)) is InvalidDataException, "");

        Console.WriteLine("=== IMMUTABILITY / PROVENANCE / PROMOTION ===");
        var proj = Invoke(good)!;
        var props = projType.GetProperties(NP | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Check("P11.projection-immutable", props.All(p => p.SetMethod is null), "");
        Check("P12.null-qualification-association",
            projType.GetProperty("QualificationId", NP)!.GetValue(proj) is null &&
            projType.GetProperty("QualificationRecordSha256", NP)!.GetValue(proj) is null, "");

        var authorityTypes = new[] { "OwnerAuthorityContext", "Qualified", "Validation", "LoadedInstallation", "InstalledResult", "Grant", "ProductionActivationAuthorization" };
        bool Authority(Type? t) => t is not null && authorityTypes.Any(n => (t.FullName ?? t.Name).Contains(n, StringComparison.Ordinal));
        var members = projType.GetMethods(NP | BindingFlags.Static | BindingFlags.DeclaredOnly).Cast<MemberInfo>()
            .Concat(projType.GetProperties(NP | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Concat(projType.GetFields(NP | BindingFlags.Static | BindingFlags.DeclaredOnly)).ToArray();
        var promoted = members.Where(m => m switch
        {
            MethodInfo mi => Authority(mi.ReturnType) || mi.GetParameters().Any(p => Authority(p.ParameterType) || p.ParameterType == typeof(bool)),
            PropertyInfo pi => Authority(pi.PropertyType),
            FieldInfo fi => Authority(fi.FieldType),
            _ => false,
        }).Select(m => m.Name).ToArray();
        Check("P13.no-authority-promotion", promoted.Length == 0, string.Join(",", promoted));

        var ctors = projType.GetConstructors(NP).ToArray();
        Check("P14.only-private-ctors", ctors.Length > 0 && ctors.All(c => c.IsPrivate), $"ctors={ctors.Length}");
        Check("P15.FromEvidence-private", fromEvidence.IsPrivate, "");
        Check("P16.Verify-internal-only-type-factory",
            projType.GetMethods(NP | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => m.ReturnType == projType && (m.IsPublic || m.IsAssembly))
                .All(m => m.GetParameters().Length == 0 || m.GetParameters().Single().ParameterType == evType), "");

        Console.WriteLine("=== RELEASE ASSEMBLY ===");
        var alc = new System.Runtime.Loader.AssemblyLoadContext("rel-only", isCollectible: true);
        var rel = alc.LoadFromAssemblyPath(releaseDll);
        var relProj = rel.GetType("Gagamba.Execution.Linux.M1.Installed.PrepublicationIntegrityProjection")!;
        Check("R1.release-no-ForTest", relProj.GetMethod("ForTest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is null, "");
        Check("R2.release-verify-present", relProj.GetMethod("Verify", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is not null, "");
        Check("R3.release-private-ctors", relProj.GetConstructors(NP).All(c => c.IsPrivate), "");
        Check("R4.release-internal-type", !relProj.IsPublic, "");

        Console.WriteLine($"SUMMARY\tfailures={_fail}");
        return 0;
    }
}
