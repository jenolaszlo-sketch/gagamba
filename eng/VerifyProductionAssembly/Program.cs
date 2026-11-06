using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

const string Usage = "Usage: VerifyProductionAssembly (--production|--instrumented) <M1-assembly.dll> [--dump]";

if (args.Length < 2 || args.Length > 3 || (args[0] != "--production" && args[0] != "--instrumented") ||
    (args.Length > 2 && args[2] != "--dump"))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var production = args[0] == "--production";
var dump = args.Length == 3;
var path = Path.GetFullPath(args[1]);
if (!File.Exists(path))
{
    Console.Error.WriteLine($"ERROR: assembly not found: {path}");
    return 2;
}

var hookTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Gagamba.Execution.Linux.M1.Bootstrap.Int01QualificationFault",
    "Gagamba.Execution.Linux.M1.Bootstrap.Int01Qualification",
    "Gagamba.Execution.Linux.M1.Preparation.CopyTestHooks",
    "Gagamba.Execution.Linux.M1.Preparation.BarrierDeathException",
};
var hookProperties = new HashSet<string>(StringComparer.Ordinal)
{
    "Gagamba.Execution.Linux.M1.Bootstrap.OwnerSafetyMonitor::QualificationNativeStop",
    "Gagamba.Execution.Linux.M1.Launch.BubblewrapAdapter::QualificationBeforeCaptureStore",
    "Gagamba.Execution.Linux.M1.Native.JournalFileStorage::QualificationBeforeRename",
};
var coreTypes = new HashSet<string>(StringComparer.Ordinal)
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
    "Gagamba.Execution.Linux.M1.Lifecycle.OwnerManifestAdmissionGate",
};
// Exact case/stage/evidence strings taken from Bootstrap/Int01QualificationFault.cs.
var faultStrings = new[]
{
    "controller-before-preparation", "controller-during-preparation", "recovery-retention-death",
    "owner-before-retention", "owner-mid-staging", "missing-domain-ack", "missing-staging-ack",
    "lost-retention-ack", "wrong-descriptor-role-count", "integrated-stop-before-mint",
    "controller-release-race", "recovery-release-race", "integrated-stop-after-intent-before-write", "owner-ambiguous-delivery",
    "integrated-lost-release-ack", "b4-overflow-blocked", "b4-recovery-loss-blocked",
    "b4-status-backpressure", "b4-controller-loss-blocked", "blocked-observer-native-stop",
    "actual-recovery-release-impersonation", "integrated-ambiguous-intent", "b4-store-failure",
    "partial-scm-rights", "owner-after-intent", "Unsupported INT01 qualification fault",
    "owner-status-pipe-full", "B4 status backpressure was not established",
    "INT01 qualification barrier reached original cutoff", "fault-", "continue-",
};
var forbiddenProdStrings = new[]
{
    "FakeDomainClock", "RelativeTestClock", "TestFixture",
    "NotAttemptedStopTestFixture", "Int01QualificationFault", "Int01Qualification",
    "CopyTestHooks", "BarrierDeathException", "QualificationNativeStop",
    "QualificationBeforeCaptureStore", "QualificationBeforeRename",
};
// Debug-only authority factories must be absent from the production candidate.
var forbiddenAuthorityFactories = new[]
{
    "Grant::ForTest", "Qualified::ForTest", "OwnerReleaseGenerationGate::VerifyObserved",
};

using var stream = File.OpenRead(path);
using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
if (!pe.HasMetadata)
{
    Console.Error.WriteLine("ERROR: input is not a managed PE assembly");
    return 2;
}
var md = pe.GetMetadataReader();
var assembly = md.GetAssemblyDefinition();
var assemblyName = md.GetString(assembly.Name);
var module = md.GetModuleDefinition();
var mvid = md.GetGuid(module.Mvid);
var sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var configuration = GetConfiguration(md);
var types = ReadTypes(md);
var propertyNames = ReadPropertyNames(md);
var metadataNames = ReadMetadataNames(md);
var userStrings = new List<string>();
var methodBodies = new List<(string Name, byte[] Il)>();
var ilRefs = new List<string>();
var badIl = new List<string>();
if (dump)
{
    Console.WriteLine("--- TYPE AND MEMBER INVENTORY ---");
    foreach (var typeName in types.OrderBy(x => x, StringComparer.Ordinal)) Console.WriteLine("TYPE " + typeName);
}
ReadMethodBodies(pe, md, methodBodies, ilRefs, badIl, dump);

var errors = new List<string>();
if (!StringComparer.Ordinal.Equals(assemblyName, "Gagamba.Execution.Linux.M1") &&
    (production || !StringComparer.Ordinal.Equals(assemblyName, "Gagamba.Execution.Linux.M1.TestFixture")))
    errors.Add($"assembly name is '{assemblyName}', expected Gagamba.Execution.Linux.M1");

var foundHookTypes = hookTypes.Where(types.Contains).OrderBy(x => x, StringComparer.Ordinal).ToArray();
var foundHookProperties = hookProperties.Where(propertyNames.Contains).OrderBy(x => x, StringComparer.Ordinal).ToArray();
var missingCore = coreTypes.Where(x => !types.Contains(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
if (missingCore.Length > 0) errors.Add("missing core types: " + string.Join(", ", missingCore));
if (production)
{
    if (foundHookTypes.Length > 0) errors.Add("qualification hook types remain: " + string.Join(", ", foundHookTypes));
    if (foundHookProperties.Length > 0) errors.Add("qualification hook properties remain: " + string.Join(", ", foundHookProperties));
    var foundAuthorityFactories = forbiddenAuthorityFactories
        .Where(label => methodBodies.Any(body => body.Name == label))
        .OrderBy(x => x, StringComparer.Ordinal).ToArray();
    if (foundAuthorityFactories.Length > 0)
        errors.Add("debug-only authority factories remain: " + string.Join(", ", foundAuthorityFactories));
    var forbiddenMetadata = metadataNames.Where(IsForbiddenName).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    if (forbiddenMetadata.Length > 0) errors.Add("forbidden metadata names/references: " + string.Join(", ", forbiddenMetadata));
    var forbiddenUserStrings = userStrings.Where(s => faultStrings.Contains(s, StringComparer.Ordinal) ||
        forbiddenProdStrings.Any(f => s.Contains(f, StringComparison.OrdinalIgnoreCase))).Distinct(StringComparer.Ordinal).ToArray();
    if (forbiddenUserStrings.Length > 0) errors.Add("forbidden user strings: " + string.Join(" | ", forbiddenUserStrings));
    var forbiddenIlRefs = ilRefs.Where(IsForbiddenName).Distinct(StringComparer.Ordinal).ToArray();
    if (forbiddenIlRefs.Length > 0) errors.Add("forbidden IL token references: " + string.Join(", ", forbiddenIlRefs));
    if (!string.Equals(configuration, "Release", StringComparison.OrdinalIgnoreCase))
        errors.Add($"assembly configuration is '{configuration ?? "<missing>"}', expected Release");
}
else
{
    var missingHookTypes = hookTypes.Where(x => !types.Contains(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    var missingHookProps = hookProperties.Where(x => !propertyNames.Contains(x)).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    if (missingHookTypes.Length > 0) errors.Add("instrumented hook types missing: " + string.Join(", ", missingHookTypes));
    if (missingHookProps.Length > 0) errors.Add("instrumented hook properties missing: " + string.Join(", ", missingHookProps));
    if (configuration is null || !(string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(configuration, "Test", StringComparison.OrdinalIgnoreCase)))
        errors.Add($"assembly configuration is '{configuration ?? "<missing>"}', expected Debug or Test");
}
if (badIl.Count > 0) errors.Add("malformed/unreadable IL bodies: " + string.Join("; ", badIl));

Console.WriteLine($"Assembly: {assemblyName}");
Console.WriteLine($"Configuration: {configuration ?? "<missing>"}");
Console.WriteLine($"MVID: {mvid:D}");
Console.WriteLine($"SHA-256: {sha}");
Console.WriteLine($"Mode: {(production ? "production" : "instrumented")}");
Console.WriteLine($"Type-name entries: {types.Count}; methods with bodies decoded: {methodBodies.Count}; user strings: {userStrings.Count}");
Console.WriteLine($"Seven surfaces: {foundHookTypes.Length}/4 hook types and {foundHookProperties.Length}/3 hook properties present");
Console.WriteLine($"Core production types: {coreTypes.Count - missingCore.Length}/{coreTypes.Count} present");
if (errors.Count == 0)
{
    Console.WriteLine("RESULT: PASS");
    return 0;
}
Console.Error.WriteLine("RESULT: FAIL");
foreach (var error in errors) Console.Error.WriteLine("  - " + error);
return 1;

bool IsForbiddenName(string value)
{
    var names = new[]
    {
        "Int01QualificationFault", "Int01Qualification", "CopyTestHooks", "BarrierDeathException",
        "QualificationNativeStop", "QualificationBeforeCaptureStore", "QualificationBeforeRename",
        "FakeDomainClock", "RelativeTestClock", "NotAttemptedStopTestFixture", "TestFixture",
    };
    return names.Any(n => value.Contains(n, StringComparison.OrdinalIgnoreCase));
}

string? GetConfiguration(MetadataReader reader)
{
    foreach (var caHandle in reader.GetAssemblyDefinition().GetCustomAttributes())
    {
        var ca = reader.GetCustomAttribute(caHandle);
        var ctorName = GetAttributeTypeName(reader, ca.Constructor);
        if (!ctorName.EndsWith("AssemblyConfigurationAttribute", StringComparison.Ordinal)) continue;
        try
        {
            var blob = reader.GetBlobReader(ca.Value);
            if (blob.ReadUInt16() != 1) return null;
            return blob.ReadSerializedString();
        }
        catch { return null; }
    }
    return null;
}

HashSet<string> ReadTypes(MetadataReader reader)
{
    var result = new HashSet<string>(StringComparer.Ordinal);
    foreach (var h in reader.TypeDefinitions)
    {
        var td = reader.GetTypeDefinition(h);
        var ns = reader.GetString(td.Namespace);
        var name = reader.GetString(td.Name);
        result.Add(ns.Length == 0 ? name : ns + "." + name);
        foreach (var nh in td.GetNestedTypes())
        {
            var nested = reader.GetTypeDefinition(nh);
            result.Add((ns.Length == 0 ? name : ns + "." + name) + "+" + reader.GetString(nested.Name));
        }
    }
    return result;
}

HashSet<string> ReadPropertyNames(MetadataReader reader)
{
    var result = new HashSet<string>(StringComparer.Ordinal);
    foreach (var typeHandle in reader.TypeDefinitions)
    {
        var type = reader.GetTypeDefinition(typeHandle);
        var typeName = GetTypeName(reader, typeHandle);
        foreach (var h in type.GetProperties()) result.Add(typeName + "::" + reader.GetString(reader.GetPropertyDefinition(h).Name));
    }
    return result;
}

List<string> ReadMetadataNames(MetadataReader reader)
{
    var result = new List<string>();
    foreach (var h in reader.TypeDefinitions)
    {
        var t = reader.GetTypeDefinition(h);
        result.Add(reader.GetString(t.Name)); result.Add(reader.GetString(t.Namespace));
        foreach (var f in t.GetFields()) result.Add(reader.GetString(reader.GetFieldDefinition(f).Name));
        foreach (var m in t.GetMethods()) result.Add(reader.GetString(reader.GetMethodDefinition(m).Name));
        foreach (var p in t.GetProperties()) result.Add(reader.GetString(reader.GetPropertyDefinition(p).Name));
    }
    foreach (var h in reader.TypeReferences)
    {
        var t = reader.GetTypeReference(h); result.Add(reader.GetString(t.Name)); result.Add(reader.GetString(t.Namespace));
    }
    foreach (var h in reader.MemberReferences) result.Add(reader.GetString(reader.GetMemberReference(h).Name));
    foreach (var h in reader.AssemblyReferences) result.Add(reader.GetString(reader.GetAssemblyReference(h).Name));
    for (var row = 1; row <= reader.GetTableRowCount(TableIndex.ModuleRef); row++)
        result.Add(reader.GetString(reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(row)).Name));
    // Custom attribute fixed arguments (including InternalsVisibleTo assembly identities)
    // live in blobs, not in the ordinary metadata name tables.
    foreach (var h in reader.GetAssemblyDefinition().GetCustomAttributes())
    {
        var attribute = reader.GetCustomAttribute(h);
        result.Add(GetAttributeTypeName(reader, attribute.Constructor));
        result.Add(Encoding.UTF8.GetString(reader.GetBlobBytes(attribute.Value)));
    }
    return result.Where(s => !string.IsNullOrEmpty(s)).ToList();
}

void ReadMethodBodies(PEReader reader, MetadataReader metadata, List<(string Name, byte[] Il)> bodies,
    List<string> references, List<string> malformed, bool emitDump)
{
    var ops = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => unchecked((ushort)o.Value), o => o);
    foreach (var h in metadata.MethodDefinitions)
    {
        var method = metadata.GetMethodDefinition(h);
        if (method.RelativeVirtualAddress == 0) continue;
        var type = metadata.GetTypeDefinition(method.GetDeclaringType());
        var typeName = metadata.GetString(type.Name);
        var methodName = metadata.GetString(method.Name);
        var label = typeName + "::" + methodName;
        try
        {
            var body = reader.GetMethodBody(method.RelativeVirtualAddress);
            var il = body.GetILBytes()?.ToArray() ?? Array.Empty<byte>();
            bodies.Add((label, il));
            var pos = 0;
            while (pos < il.Length)
            {
                var start = pos;
                ushort key = il[pos++];
                if (key == 0xfe)
                {
                    if (pos >= il.Length) throw new BadImageFormatException("truncated two-byte opcode");
                    key = (ushort)(0xfe00 | il[pos++]);
                }
                if (!ops.TryGetValue(key, out var op)) throw new BadImageFormatException($"unknown opcode 0x{key:x4}");
                var operand = ReadOperand(op, il, ref pos);
                string? resolved = null;
                if (op.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig or
                    OperandType.InlineTok or OperandType.InlineType)
                {
                    resolved = ResolveToken(metadata, operand);
                    if (resolved is not null) references.Add(resolved);
                }
                else if (op.OperandType == OperandType.InlineString)
                {
                    var token = unchecked((int)operand);
                    if ((token & unchecked((int)0xff000000)) == 0x70000000)
                    {
                        var s = metadata.GetUserString(MetadataTokens.UserStringHandle(token & 0x00ffffff));
                        userStrings.Add(s); resolved = "\"" + s + "\"";
                    }
                }
                if (emitDump) Console.WriteLine($"IL {label} +0x{start:x4}: {op.Name} {(resolved ?? Convert.ToString(operand, System.Globalization.CultureInfo.InvariantCulture))}");
            }
        }
        catch (Exception ex) { malformed.Add(label + ": " + ex.Message); }
    }
}

string ResolveToken(MetadataReader reader, long raw)
{
    var token = unchecked((int)raw);
    try
    {
        var h = MetadataTokens.Handle(token);
        return h.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName(reader, (TypeDefinitionHandle)h),
            HandleKind.TypeReference => GetTypeReferenceName(reader, (TypeReferenceHandle)h),
            HandleKind.TypeSpecification => "TypeSpec#" + token.ToString("x8"),
            HandleKind.MethodDefinition => GetMethodName(reader, (MethodDefinitionHandle)h),
            HandleKind.FieldDefinition => GetFieldName(reader, (FieldDefinitionHandle)h),
            HandleKind.MemberReference => GetMemberReferenceName(reader, (MemberReferenceHandle)h),
            HandleKind.MethodSpecification => "MethodSpec#" + token.ToString("x8"),
            HandleKind.StandaloneSignature => "Signature#" + token.ToString("x8"),
            _ => h.Kind + "#" + token.ToString("x8"),
        };
    }
    catch { return "unresolved-token#" + token.ToString("x8"); }
}

string GetTypeName(MetadataReader reader, TypeDefinitionHandle h)
{
    var t = reader.GetTypeDefinition(h); var ns = reader.GetString(t.Namespace); var name = reader.GetString(t.Name);
    return ns.Length == 0 ? name : ns + "." + name;
}
string GetTypeReferenceName(MetadataReader reader, TypeReferenceHandle h)
{
    var t = reader.GetTypeReference(h); var ns = reader.GetString(t.Namespace); var name = reader.GetString(t.Name);
    return ns.Length == 0 ? name : ns + "." + name;
}
string GetMethodName(MetadataReader reader, MethodDefinitionHandle h)
{
    var m = reader.GetMethodDefinition(h); return GetTypeName(reader, m.GetDeclaringType()) + "::" + reader.GetString(m.Name);
}
string GetFieldName(MetadataReader reader, FieldDefinitionHandle h)
{
    var f = reader.GetFieldDefinition(h); return GetTypeName(reader, f.GetDeclaringType()) + "::" + reader.GetString(f.Name);
}
string GetMemberReferenceName(MetadataReader reader, MemberReferenceHandle h)
{
    var m = reader.GetMemberReference(h); return GetEntityName(reader, m.Parent) + "::" + reader.GetString(m.Name);
}
string GetEntityName(MetadataReader reader, EntityHandle h) => h.Kind switch
{
    HandleKind.TypeDefinition => GetTypeName(reader, (TypeDefinitionHandle)h),
    HandleKind.TypeReference => GetTypeReferenceName(reader, (TypeReferenceHandle)h),
    HandleKind.MethodDefinition => GetMethodName(reader, (MethodDefinitionHandle)h),
    HandleKind.ModuleReference => reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)h).Name),
    _ => h.Kind.ToString(),
};
string GetAttributeTypeName(MetadataReader reader, EntityHandle ctor)
{
    if (ctor.Kind == HandleKind.MemberReference)
    {
        var parent = reader.GetMemberReference((MemberReferenceHandle)ctor).Parent;
        return parent.Kind == HandleKind.TypeReference ? GetTypeReferenceName(reader, (TypeReferenceHandle)parent) : GetEntityName(reader, parent);
    }
    if (ctor.Kind == HandleKind.MethodDefinition) return GetTypeName(reader, reader.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType());
    return string.Empty;
}

long ReadOperand(OpCode op, byte[] il, ref int pos)
{
    return op.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => ReadByte(il, ref pos),
        OperandType.InlineVar => ReadUInt16(il, ref pos),
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig or
        OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or OperandType.InlineI => ReadInt32(il, ref pos),
        OperandType.ShortInlineR => ReadInt32(il, ref pos),
        OperandType.InlineI8 or OperandType.InlineR => ReadInt64(il, ref pos),
        OperandType.InlineSwitch => ReadSwitch(il, ref pos),
        _ => throw new BadImageFormatException("unsupported operand kind " + op.OperandType),
    };
}
byte ReadByte(byte[] bytes, ref int pos) { Require(bytes, pos, 1); return bytes[pos++]; }
ushort ReadUInt16(byte[] bytes, ref int pos) { Require(bytes, pos, 2); var value = BitConverter.ToUInt16(bytes, pos); pos += 2; return value; }
int ReadInt32(byte[] bytes, ref int pos) { Require(bytes, pos, 4); var value = BitConverter.ToInt32(bytes, pos); pos += 4; return value; }
long ReadInt64(byte[] bytes, ref int pos) { Require(bytes, pos, 8); var value = BitConverter.ToInt64(bytes, pos); pos += 8; return value; }
void Require(byte[] bytes, int pos, int count) { if (pos + count > bytes.Length) throw new BadImageFormatException("truncated operand"); }
long ReadSwitch(byte[] il, ref int pos)
{
    if (pos + 4 > il.Length) throw new BadImageFormatException("truncated switch operand");
    var count = BitConverter.ToInt32(il, pos); pos += 4;
    if (count < 0 || count > (il.Length - pos) / 4) throw new BadImageFormatException("invalid switch table");
    pos += count * 4;
    return count;
}
