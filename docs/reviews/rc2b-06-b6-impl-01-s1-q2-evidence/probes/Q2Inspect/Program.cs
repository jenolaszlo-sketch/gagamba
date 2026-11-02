// Q2 independent binary inspection probe (reviewer-owned).
// Independent of eng/VerifyProductionAssembly. Uses System.Reflection.Metadata only.
// Usage: Q2Inspect (--production|--instrumented) <assembly.dll>
// Exit 0 = expectations met; exit 1 = violation; exit 2 = usage error.
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

var production = args.Length == 2 && args[0] == "--production";
var instrumented = args.Length == 2 && args[0] == "--instrumented";
var closed = args.Length == 2 && args[0] == "--closed"; // owner/recovery closure: raw-scan + config only
if ((!production && !instrumented && !closed) || args.Length != 2)
{ Console.Error.WriteLine("Usage: Q2Inspect (--production|--instrumented|--closed) <assembly.dll>"); return 2; }
var path = args[1];

// The accepted seven qualification-only surfaces (namespace-level type names
// verified against SafeCopyIn.cs / Int01QualificationFault.cs / property declarations).
string[] hookTypes =
{
    "Gagamba.Execution.Linux.M1.Bootstrap.Int01QualificationFault",
    "Gagamba.Execution.Linux.M1.Bootstrap.Int01Qualification",
    "Gagamba.Execution.Linux.M1.Preparation.CopyTestHooks",
    "Gagamba.Execution.Linux.M1.Preparation.BarrierDeathException",
};
string[] hookProps =
{
    "Gagamba.Execution.Linux.M1.Bootstrap.OwnerSafetyMonitor::QualificationNativeStop",
    "Gagamba.Execution.Linux.M1.Launch.BubblewrapAdapter::QualificationBeforeCaptureStore",
    "Gagamba.Execution.Linux.M1.Native.JournalFileStorage::QualificationBeforeRename",
};
string[] coreTypes =
{
    "Gagamba.Execution.Linux.M1.Bootstrap.OwnerSafetyMonitor",
    "Gagamba.Execution.Linux.M1.Launch.BubblewrapAdapter",
    "Gagamba.Execution.Linux.M1.Preparation.SafeCopyIn",
    "Gagamba.Execution.Linux.M1.Native.JournalFileStorage",
    "Gagamba.Execution.Linux.M1.Restricted.B3Qualification",
    "Gagamba.Execution.Linux.M1.Lifecycle.M1RecoveryScope",
    "Gagamba.Execution.Linux.M1.Lifecycle.OwnerPreparationTransaction",
    "Gagamba.Execution.Linux.M1.Lifecycle.HeldReleasePhase",
    "Gagamba.Execution.Linux.M1.Lifecycle.OwnerPreparationSpecification",
    "Gagamba.Execution.Linux.M1.Bootstrap.OwnerDirectConnection",
    "Gagamba.Execution.Linux.M1.Bootstrap.IntegratedRecoveryHost",
    "Gagamba.Execution.Linux.M1.Lifecycle.StopwatchDomainClock",
};

using var fs = File.OpenRead(path);
using var pe = new PEReader(fs, PEStreamOptions.LeaveOpen);
if (!pe.HasMetadata) { Console.Error.WriteLine("not a managed PE"); return 1; }
var md = pe.GetMetadataReader();
var asm = md.GetAssemblyDefinition();
var asmName = md.GetString(asm.Name);
var mvid = md.GetGuid(md.GetModuleDefinition().Mvid);
var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

string? config = null;
foreach (var cah in asm.GetCustomAttributes())
{
    var ca = md.GetCustomAttribute(cah);
    var ctor = ca.Constructor;
    string? name = null;
    if (ctor.Kind == HandleKind.MemberReference)
    {
        var mr = md.GetMemberReference((MemberReferenceHandle)ctor);
        if (mr.Parent.Kind == HandleKind.TypeReference)
            name = md.GetString(md.GetTypeReference((TypeReferenceHandle)mr.Parent).Name);
    }
    if (name != "AssemblyConfigurationAttribute") continue;
    var blob = md.GetBlobReader(ca.Value);
    blob.ReadUInt16();
    config = blob.ReadSerializedString();
}

var types = new HashSet<string>(StringComparer.Ordinal);
int methodCount = 0, fieldCount = 0, propCount = 0;
var propIndex = new HashSet<string>(StringComparer.Ordinal);
var nestedNames = new HashSet<string>(StringComparer.Ordinal);
foreach (var th in md.TypeDefinitions)
{
    var td = md.GetTypeDefinition(th);
    var ns = md.GetString(td.Namespace);
    var nm = md.GetString(td.Name);
    var full = ns.Length == 0 ? nm : ns + "." + nm;
    types.Add(full);
    foreach (var mh in td.GetMethods()) methodCount++;
    foreach (var fh in td.GetFields()) fieldCount++;
    foreach (var ph in td.GetProperties()) { propCount++; propIndex.Add(full + "::" + md.GetString(md.GetPropertyDefinition(ph).Name)); }
    foreach (var nth in td.GetNestedTypes())
    {
        var nt = md.GetTypeDefinition(nth);
        nestedNames.Add(full + "+" + md.GetString(nt.Name));
    }
}

var missingHooks = hookTypes.Where(t => !types.Contains(t)).ToArray();
var presentHooks = hookTypes.Where(t => types.Contains(t)).ToArray();
var missingProps = hookProps.Where(p => !propIndex.Contains(p)).ToArray();
var presentProps = hookProps.Where(p => propIndex.Contains(p)).ToArray();
var missingCore = coreTypes.Where(t => !types.Contains(t)).ToArray();

// Raw byte scan: independent second channel over the whole PE for hook markers
// (covers UTF-8 metadata heap strings and UTF-16 user strings).
var bytes = File.ReadAllBytes(path);
string[] markers = { "Int01Qualification", "CopyTestHooks", "BarrierDeathException",
    "QualificationNativeStop", "QualificationBeforeCaptureStore", "QualificationBeforeRename",
    "FakeDomainClock", "RelativeTestClock", "TestFixture" };
var rawHits = new SortedSet<string>(StringComparer.Ordinal);
foreach (var m in markers)
{
    if (ContainsUtf8(bytes, m) || ContainsUtf16(bytes, m)) rawHits.Add(m);
}

Console.WriteLine($"Assembly: {asmName}");
Console.WriteLine($"Configuration: {config ?? "<missing>"}");
Console.WriteLine($"MVID: {mvid:D}");
Console.WriteLine($"SHA-256: {sha}");
Console.WriteLine($"Types: {types.Count} (nested distinct: {nestedNames.Count}); Methods: {methodCount}; Fields: {fieldCount}; Properties: {propCount}");
Console.WriteLine($"Hook types present: {presentHooks.Length}/4 [{string.Join(", ", presentHooks)}]");
Console.WriteLine($"Hook properties present: {presentProps.Length}/3 [{string.Join(", ", presentProps)}]");
Console.WriteLine($"Missing core types: {missingCore.Length}/12 [{string.Join(", ", missingCore)}]");
Console.WriteLine($"Raw byte-scan marker hits: {(rawHits.Count == 0 ? "<none>" : string.Join(", ", rawHits))}");

var problems = new List<string>();
if (production)
{
    if (presentHooks.Length > 0) problems.Add("hook types present in production");
    if (presentProps.Length > 0) problems.Add("hook properties present in production");
    if (missingCore.Length > 0) problems.Add("core safety types missing");
    if (!string.Equals(config, "Release", StringComparison.OrdinalIgnoreCase)) problems.Add("configuration is not Release");
    if (rawHits.Count > 0) problems.Add("raw hook/test markers found in PE bytes: " + string.Join(", ", rawHits));
}
else if (closed)
{
    // Owner/recovery are small referencing assemblies; M1 core types live in the
    // referenced M1 assembly, so only hook absence + label + raw markers are checked.
    if (presentHooks.Length > 0) problems.Add("hook types present in closure");
    if (presentProps.Length > 0) problems.Add("hook properties present in closure");
    if (!string.Equals(config, "Release", StringComparison.OrdinalIgnoreCase)) problems.Add("configuration is not Release");
    if (rawHits.Count > 0) problems.Add("raw hook/test markers found in PE bytes: " + string.Join(", ", rawHits));
}
else
{
    if (missingHooks.Length > 0) problems.Add("hook types missing in instrumented");
    if (missingProps.Length > 0) problems.Add("hook properties missing in instrumented");
    if (missingCore.Length > 0) problems.Add("core safety types missing");
    if (config is null || !(config.Equals("Debug", StringComparison.OrdinalIgnoreCase) || config.Equals("Test", StringComparison.OrdinalIgnoreCase)))
        problems.Add("configuration is not Debug/Test");
    if (!rawHits.Contains("Int01Qualification")) problems.Add("raw hook markers absent in instrumented");
}
if (problems.Count > 0) { Console.Error.WriteLine("Q2-INSPECT: FAIL"); foreach (var p in problems) Console.Error.WriteLine("  - " + p); return 1; }
Console.WriteLine("Q2-INSPECT: PASS");
return 0;

static bool ContainsUtf8(byte[] b, string s)
{
    var pat = Encoding.UTF8.GetBytes(s);
    return IndexOf(b, pat) >= 0;
}
static bool ContainsUtf16(byte[] b, string s)
{
    var pat = Encoding.Unicode.GetBytes(s);
    return IndexOf(b, pat) >= 0;
}
static int IndexOf(byte[] hay, byte[] needle)
{
    for (int i = 0; i <= hay.Length - needle.Length; i++)
    {
        int j = 0;
        for (; j < needle.Length && hay[i + j] == needle[j]; j++) ;
        if (j == needle.Length) return i;
    }
    return -1;
}
