using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

internal sealed class Names : ISignatureTypeProvider<string, int>
{
    internal static string Qualify(string ns, string name) => ns.Length == 0 ? name : ns + "." + name;

    public string GetTypeFromDefinition(MetadataReader md, TypeDefinitionHandle handle, byte kind)
    {
        var def = md.GetTypeDefinition(handle);
        var parent = def.GetDeclaringType();
        return parent.IsNil ? Qualify(md.GetString(def.Namespace), md.GetString(def.Name))
            : GetTypeFromDefinition(md, parent, 0) + "+" + md.GetString(def.Name);
    }

    public string GetTypeFromReference(MetadataReader md, TypeReferenceHandle handle, byte kind)
    {
        var type = md.GetTypeReference(handle);
        return type.ResolutionScope.Kind == HandleKind.TypeReference
            ? GetTypeFromReference(md, (TypeReferenceHandle)type.ResolutionScope, 0) + "+" + md.GetString(type.Name)
            : Qualify(md.GetString(type.Namespace), md.GetString(type.Name));
    }
    public string GetTypeFromSpecification(MetadataReader md, int context, TypeSpecificationHandle handle, byte kind) =>
        md.GetTypeSpecification(handle).DecodeSignature(this, context);
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + shape.Rank + "]";
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetPinnedType(string elementType) => elementType + " pinned";
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType + " " + modifier;
    public string GetGenericInstantiation(string genericType, ImmutableArray<string> arguments) =>
        genericType + "<" + string.Join(",", arguments) + ">";
    public string GetGenericMethodParameter(int context, int index) => "!!" + index;
    public string GetGenericTypeParameter(int context, int index) => "!" + index;
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr" + Render(signature);
    internal static string Render(MethodSignature<string> sig) => "(" + string.Join(",", sig.ParameterTypes) + ")->" + sig.ReturnType;
}

internal static class Audit
{
    private const string Namespace = "Gagamba.Execution.Linux.M1.Bootstrap.";
    private const string Model = Namespace + "ValidationCustody";
    private static readonly string[] Prior =
        ["ValidationPreCodec", "ValidationPreCodecBinary", "Gava1ContextCodec", "PrepublicationIntegrityProjection",
         "FrozenContractVerifier", "DetachedEvidenceVerifier"];
    private static readonly string[] Sinks =
        ["RecoveryConnection::Start", "IntegratedRecoveryHost::Run", "::StartOwnerPreparation", "::TrySpawnOwner",
         "::RunTransaction", "HeldReleasePhase::.ctor", "B3PreparationOwner::.ctor", "PreparationOwnershipCoordinator::.ctor",
         "ReleaseArbiter::", "ReleaseGenerationGate::", "RecoveryInitializationAdmission::Require"];
    private static readonly string[] AllowedValues = [Namespace + "ValidationPreContext", Namespace + "Gava1Ids"];
    private static readonly string[] AllowedTestTypes =
        ["ValidationCustodyModelTests", "ValidationCustodyModelLifecycleTests", "ValidationCustodyModelCompositionTests",
         "ValidationCustodyModelVectors"];
    private static readonly Dictionary<ushort, OpCode> Ops = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(op => unchecked((ushort)op.Value));
    private static readonly Names TypeNames = new();

    private static bool ModelName(string name) => name.Contains(Model, StringComparison.Ordinal);
    private static bool Previous(string name) => Prior.Any(p => name.Contains(p + "::", StringComparison.Ordinal));
    private static bool KnownSink(string name) => Sinks.Any(s => name.Contains(s, StringComparison.Ordinal));
    private static bool AllowedOutbound(string name) =>
        name.StartsWith(Model, StringComparison.Ordinal) || name.StartsWith("System.", StringComparison.Ordinal) ||
        AllowedValues.Any(n => name.StartsWith(n + "::", StringComparison.Ordinal) || name == n) ||
        name == Namespace + "ValidationPreTopology";

    private sealed record Edge(string From, string Kind, string To);

    private static int Inspect(string path)
    {
        using var file = File.OpenRead(path);
        using var pe = new PEReader(file, PEStreamOptions.PrefetchEntireImage);
        var md = pe.GetMetadataReader();
        string assembly = md.GetString(md.GetAssemblyDefinition().Name);
        bool tests = assembly == "Gagamba.Execution.Linux.M1.Tests";
        bool core = assembly == "Gagamba.Execution.Linux.M1";
        var allTypes = md.TypeDefinitions.ToDictionary(h => h, h => TypeNames.GetTypeFromDefinition(md, h, 0));
        var modelTypes = allTypes.Values.Where(ModelName).Order().ToArray();
        var fields = allTypes.SelectMany(p => md.GetTypeDefinition(p.Key).GetFields().Select(h => (h,p.Value)))
            .ToDictionary(p => p.h, p => p.Value);
        var edges = new SortedSet<(string From, string Kind, string To)>();
        var errors = new SortedSet<string>(StringComparer.Ordinal);
        var typeRefs = md.TypeReferences.Select(h => TypeNames.GetTypeFromReference(md, h, 0)).Where(ModelName).Order().ToArray();
        int bodies = 0, instructions = 0, signatures = 0;

        if (!core && modelTypes.Length != 0)
            errors.Add("Model definition outside core");
        if (!core && !tests && typeRefs.Length != 0)
            errors.Add("Production model TypeRef " + string.Join(", ", typeRefs));

        string Symbol(EntityHandle token)
        {
            switch (token.Kind)
            {
                case HandleKind.TypeDefinition:
                    return allTypes[(TypeDefinitionHandle)token];
                case HandleKind.TypeReference:
                    return TypeNames.GetTypeFromReference(md, (TypeReferenceHandle)token, 0);
                case HandleKind.TypeSpecification:
                    return TypeNames.GetTypeFromSpecification(md, 0, (TypeSpecificationHandle)token, 0);
                case HandleKind.MemberReference:
                    var mem = md.GetMemberReference((MemberReferenceHandle)token);
                    string signature = mem.GetKind() == MemberReferenceKind.Method
                        ? Names.Render(mem.DecodeMethodSignature(TypeNames, 0))
                        : ":" + mem.DecodeFieldSignature(TypeNames, 0);
                    return Symbol(mem.Parent) + "::" + md.GetString(mem.Name) + signature;
                case HandleKind.MethodDefinition:
                    var method = md.GetMethodDefinition((MethodDefinitionHandle)token);
                    return allTypes[method.GetDeclaringType()] + "::" + md.GetString(method.Name) +
                        Names.Render(method.DecodeSignature(TypeNames, 0));
                case HandleKind.FieldDefinition:
                    var fld = md.GetFieldDefinition((FieldDefinitionHandle)token);
                    return fields[(FieldDefinitionHandle)token] + "::" + md.GetString(fld.Name) + ":" +
                        fld.DecodeSignature(TypeNames, 0);
                case HandleKind.MethodSpecification:
                    var spec = md.GetMethodSpecification((MethodSpecificationHandle)token);
                    return Symbol(spec.Method) + "<" + string.Join(",", spec.DecodeSignature(TypeNames, 0)) + ">";
                case HandleKind.StandaloneSignature:
                    var stand = md.GetStandaloneSignature((StandaloneSignatureHandle)token);
                    return stand.GetKind() == StandaloneSignatureKind.Method
                        ? Names.Render(stand.DecodeMethodSignature(TypeNames, 0))
                        : string.Join(",", stand.DecodeLocalSignature(TypeNames, 0));
                default:
                    return token.Kind.ToString();
            }
        }

        void Observe(string from, string kind, string to)
        {
            bool owns = from.StartsWith(Model, StringComparison.Ordinal);
            bool target = ModelName(to);
            bool prior = Previous(to);
            if (owns || target || prior || KnownSink(to))
                edges.Add((from, kind, to));

            if (target && !owns)
            {
                var simpleType = from.Split("::", 2)[0].Split('+', 2)[0].Split('.').Last();
                if (!tests || !AllowedTestTypes.Contains(simpleType, StringComparer.Ordinal))
                    errors.Add("Unexpected incoming model reference " + from + " " + kind + " " + to);
            }
            if (owns && kind.StartsWith("IL ", StringComparison.Ordinal) && kind != "IL ldstr" &&
                !AllowedOutbound(to) && !to.StartsWith('('))
                errors.Add("Unexpected outbound model dependency " + from + " -> " + to);
            bool externalVerifier = prior &&
                !string.Equals(from.Split("::", 2)[0].Split('+', 2)[0], to.Split("::", 2)[0], StringComparison.Ordinal);
            // This unchanged P2 to P1 edge is verified against published E-4 source in audit-source.py.
            bool inheritedP2 = from == "Gagamba.Execution.Linux.M1.Validation.DetachedEvidenceVerifier::VerifyCatalog" &&
                to.StartsWith("Gagamba.Execution.Linux.M1.Validation.FrozenContractVerifier::TryVerifyCatalog(", StringComparison.Ordinal);
            if (externalVerifier && (owns || !tests) && !inheritedP2)
                errors.Add("Production verification consumer " + from + " -> " + to);
        }

        void Signature(string from, string kind, string value)
        {
            signatures++;
            if (ModelName(value))
                Observe(from, kind, value);
        }

        foreach (var (handle, typeName) in allTypes)
        {
            var type = md.GetTypeDefinition(handle);
            bool owns = ModelName(typeName);
            if (owns && (type.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic)
                errors.Add("Externally visible model type " + typeName);
            if (!type.BaseType.IsNil)
                Signature(typeName, "base", Symbol(type.BaseType));
            foreach (var item in type.GetInterfaceImplementations())
                Signature(typeName, "interface", Symbol(md.GetInterfaceImplementation(item).Interface));
            foreach (var parm in type.GetGenericParameters().Select(md.GetGenericParameter))
                foreach (var c in parm.GetConstraints())
                    Signature(typeName, "type parameter constraint", Symbol(md.GetGenericParameterConstraint(c).Type));
            foreach (var fld in type.GetFields().Select(md.GetFieldDefinition))
                Signature(typeName + "::" + md.GetString(fld.Name), "field", fld.DecodeSignature(TypeNames, 0));
            foreach (var prop in type.GetProperties().Select(md.GetPropertyDefinition))
                Signature(typeName + "::" + md.GetString(prop.Name), "property", Names.Render(prop.DecodeSignature(TypeNames, 0)));
            foreach (var e in type.GetEvents().Select(md.GetEventDefinition))
                Signature(typeName + "::" + md.GetString(e.Name), "event", Symbol(e.Type));
            foreach (var m in type.GetMethods().Select(md.GetMethodDefinition))
            {
                string name = md.GetString(m.Name);
                string from = typeName + "::" + name;
                Signature(from, "method", Names.Render(m.DecodeSignature(TypeNames, 0)));
                if (owns && ((m.Attributes & MethodAttributes.PinvokeImpl) != 0 || name == ".cctor"))
                    errors.Add("Unexpected model import or initializer " + from);
                if (from == Model + "Model::.ctor" && (m.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Private)
                    errors.Add("Model constructor is callable");
                foreach (var parm in m.GetGenericParameters().Select(md.GetGenericParameter))
                    foreach (var c in parm.GetConstraints())
                        Signature(from, "method parameter constraint", Symbol(md.GetGenericParameterConstraint(c).Type));

                if (m.RelativeVirtualAddress == 0)
                    continue;
                bodies++;
                var body = pe.GetMethodBody(m.RelativeVirtualAddress);
                if (!body.LocalSignature.IsNil)
                    Signature(from, "local", Symbol(body.LocalSignature));
                foreach (var region in body.ExceptionRegions)
                    if (!region.CatchType.IsNil)
                        Signature(from, "catch", Symbol(region.CatchType));
                var il = body.GetILReader();
                while (il.RemainingBytes != 0)
                {
                    instructions++;
                    ushort value = il.ReadByte();
                    if (value == 0xfe)
                        value = (ushort)(0xfe00 | il.ReadByte());
                    var op = Ops[value];
                    string kind = "IL " + op.Name;
                    switch (op.OperandType)
                    {
                        case OperandType.InlineNone: break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.ShortInlineI:
                        case OperandType.ShortInlineVar: il.ReadByte(); break;
                        case OperandType.InlineVar: il.ReadUInt16(); break;
                        case OperandType.InlineI:
                        case OperandType.InlineBrTarget:
                        case OperandType.ShortInlineR: il.ReadInt32(); break;
                        case OperandType.InlineI8:
                        case OperandType.InlineR: il.ReadInt64(); break;
                        case OperandType.InlineSwitch:
                            int count = il.ReadInt32();
                            for (int n = 0; n < count; n++) il.ReadInt32();
                            break;
                        case OperandType.InlineString:
                            string literal = md.GetUserString(MetadataTokens.UserStringHandle(il.ReadInt32() & 0xffffff));
                            if (literal.Contains("ValidationCustody", StringComparison.Ordinal))
                                Observe(from, kind, literal.StartsWith(Model, StringComparison.Ordinal)
                                    ? literal : Model + " [" + literal + "]");
                            break;
                        case OperandType.InlineField:
                        case OperandType.InlineMethod:
                        case OperandType.InlineSig:
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                            Observe(from, kind, Symbol(MetadataTokens.EntityHandle(il.ReadInt32())));
                            break;
                        default: throw new InvalidDataException("Unsupported opcode " + op);
                    }
                }
            }
        }
        var row = new {
            path, assembly, mvid = md.GetGuid(md.GetModuleDefinition().Mvid),
            sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))),
            bodies, instructions, signatures, modelTypes, typeRefs, errors,
            edges = edges.Select(e => new Edge(e.From, e.Kind, e.To))
        };
        Console.WriteLine(JsonSerializer.Serialize(row));
        return errors.Count == 0 ? 0 : 1;
    }

    private static int Main(string[] args)
    {
        int exit = 0;
        foreach (string path in File.ReadLines(args.Single()))
            exit |= Inspect(path);
        return exit;
    }
}
