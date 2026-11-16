using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

var ilOpCodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(o => (ushort)o.Value);
bool e4 = Environment.GetEnvironmentVariable("GAGAMBA_SCAN_E4") == "1";
string[] codec = e4 ? ["Gava1ContextCodec", "Gava1ContextData", "Gava1Originals", "Gava1Ids",
    "Gava1Expectation", "Gava1PreBootstrap", "Gava1Message", "Gava1Role", "Gava1Refusal", "Gava1PreTopology"] : ["ValidationPreCodec", "ValidationPreCodecBinary", "ValidationPreContext",
    "ValidationPreTopology", "ValidationPreRefusal", "ValidationPreBootstrapData", "ValidationPreMessage",
    "ValidationPreBinaryExpectations", "ValidationPreBinaryContext"];
const string ns = "Gagamba.Execution.Linux.M1.Bootstrap.";
bool IsCodec(string type) => codec.Any(n => type == ns + n || type.StartsWith(ns + n + "+", StringComparison.Ordinal));
bool ContainsCodec(string types) => codec.Any(n => System.Text.RegularExpressions.Regex.IsMatch(types,
    System.Text.RegularExpressions.Regex.Escape(ns + n) + @"(?![A-Za-z0-9_])"));
bool Literal(string s) => codec.Any(n => s.Contains(n, StringComparison.OrdinalIgnoreCase));
int failed = 0;
foreach (string file in args) {
    using var stream = File.OpenRead(file);
    using var pe = new PEReader(stream);
    var md = pe.GetMetadataReader();
    var names = new Names();
    string assembly = md.GetString(md.GetAssemblyDefinition().Name);
    bool test = assembly.EndsWith(".Tests", StringComparison.Ordinal);
    bool core = assembly == "Gagamba.Execution.Linux.M1";
    List<string> hits = [], codecTypes = [], visibility = [], outbound = [], bootCalls = [], binaryCalls = [];
    int bodies = 0, operands = 0, signatures = 0, literals = 0, moduleInitializers = 0;
    var module = md.GetModuleDefinition();
    var definition = md.GetAssemblyDefinition();
    var moduleId = md.GetGuid(module.Mvid);
    string Signature(MethodSignature<string> s) => s.ReturnType + "(" + string.Join(",", s.ParameterTypes) + ")";
    string Name(EntityHandle e) => e.Kind switch {
        HandleKind.TypeDefinition => names.GetTypeFromDefinition(md, (TypeDefinitionHandle)e, 0),
        HandleKind.TypeReference => names.GetTypeFromReference(md, (TypeReferenceHandle)e, 0),
        HandleKind.TypeSpecification => md.GetTypeSpecification((TypeSpecificationHandle)e).DecodeSignature(names, null),
        HandleKind.MethodDefinition => Method((MethodDefinitionHandle)e),
        HandleKind.FieldDefinition => Field((FieldDefinitionHandle)e),
        HandleKind.MemberReference => Member((MemberReferenceHandle)e),
        HandleKind.MethodSpecification => Name(md.GetMethodSpecification((MethodSpecificationHandle)e).Method) + "<" +
            string.Join(",", md.GetMethodSpecification((MethodSpecificationHandle)e).DecodeSignature(names, null)) + ">",
        HandleKind.StandaloneSignature => Standalone((StandaloneSignatureHandle)e),
        _ when e.IsNil => "",
        _ => e.Kind.ToString()
    };
    string Method(MethodDefinitionHandle h) {
        var m = md.GetMethodDefinition(h);
        return Name(m.GetDeclaringType()) + "::" + md.GetString(m.Name) + " " + Signature(m.DecodeSignature(names, null));
    }
    string Field(FieldDefinitionHandle h) {
        var f = md.GetFieldDefinition(h);
        return Name(f.GetDeclaringType()) + "::" + md.GetString(f.Name) + " " + f.DecodeSignature(names, null);
    }
    string Member(MemberReferenceHandle h) {
        var m = md.GetMemberReference(h);
        string sig = m.GetKind() == MemberReferenceKind.Method
            ? Signature(m.DecodeMethodSignature(names, null)) : m.DecodeFieldSignature(names, null);
        return Name(m.Parent) + "::" + md.GetString(m.Name) + " " + sig;
    }
    string Standalone(StandaloneSignatureHandle h) {
        var sig = md.GetStandaloneSignature(h);
        return sig.GetKind() == StandaloneSignatureKind.Method
            ? Signature(sig.DecodeMethodSignature(names, null))
            : string.Join(",", sig.DecodeLocalSignature(names, null));
    }
    void Check(string site, string signature) {
        signatures++;
        if (ContainsCodec(signature)) hits.Add(site + " -> " + signature);
    }
    void Attributes(string site, CustomAttributeHandleCollection attributes) {
        foreach (var h in attributes) {
            var a = md.GetCustomAttribute(h);
            Check(site + " custom attribute", Name(a.Constructor));
            // Serialized type names in attributes can also support discovery.
            if (Literal(System.Text.Encoding.UTF8.GetString(md.GetBlobBytes(a.Value))))
                hits.Add(site + " custom attribute data names codec");
        }
    }
    void Generic(string site, GenericParameterHandleCollection parameters) {
        foreach (var h in parameters)
            foreach (var c in md.GetGenericParameter(h).GetConstraints())
                Check(site + " constraint", Name(md.GetGenericParameterConstraint(c).Type));
    }
    Attributes("<assembly>", definition.GetCustomAttributes());
    Attributes("<module>", module.GetCustomAttributes());
    foreach (var typeHandle in md.TypeDefinitions) {
        var t = md.GetTypeDefinition(typeHandle);
        string name = Name(typeHandle);
        bool inside = IsCodec(name);
        if (inside) {
            codecTypes.Add(name);
            if ((t.Attributes & TypeAttributes.VisibilityMask) is TypeAttributes.Public or TypeAttributes.NestedPublic)
                visibility.Add(name);
        } else {
            Attributes(name, t.GetCustomAttributes());
            Generic(name, t.GetGenericParameters());
            if (!t.BaseType.IsNil) Check(name + " base", Name(t.BaseType));
            foreach (var i in t.GetInterfaceImplementations())
                Check(name + " interface", Name(md.GetInterfaceImplementation(i).Interface));
            foreach (var f in t.GetFields()) {
                Check(name + " field", Name(f));
                Attributes(name + " field", md.GetFieldDefinition(f).GetCustomAttributes());
            }
            foreach (var ph in t.GetProperties()) {
                var p = md.GetPropertyDefinition(ph);
                Check(name + " property", Signature(p.DecodeSignature(names, null)));
                Attributes(name + " property", p.GetCustomAttributes());
            }
            foreach (var eh in t.GetEvents())
                Check(name + " event", Name(md.GetEventDefinition(eh).Type));
            foreach (var ih in t.GetMethodImplementations()) {
                var i = md.GetMethodImplementation(ih);
                Check(name + " override", Name(i.MethodBody) + " " + Name(i.MethodDeclaration));
            }
        }
        foreach (var mh in t.GetMethods()) {
            var m = md.GetMethodDefinition(mh);
            string label = name + "::" + md.GetString(m.Name);
            if (!inside) {
                Check(label, Name(mh));
                Attributes(label, m.GetCustomAttributes());
                Generic(label, m.GetGenericParameters());
            }
            if (m.RelativeVirtualAddress == 0) continue;
            if (label == "<Module>::.cctor") moduleInitializers++;
            bodies++;
            var body = pe.GetMethodBody(m.RelativeVirtualAddress);
            if (!inside) {
                if (!body.LocalSignature.IsNil) Check(label + " locals", Name(body.LocalSignature));
                foreach (var handler in body.ExceptionRegions.Where(h => h.Kind == ExceptionRegionKind.Catch))
                    Check(label + " catch", Name(handler.CatchType));
            }
            var r = body.GetILReader();
            while (r.RemainingBytes != 0) {
                int offset = r.Offset;
                ushort raw = r.ReadByte();
                if (raw == 0xfe) raw = (ushort)(0xfe00 | r.ReadByte());
                var op = ilOpCodes[raw];
                switch (op.OperandType) {
                    case OperandType.InlineMethod: case OperandType.InlineType: case OperandType.InlineField:
                    case OperandType.InlineSig: case OperandType.InlineTok:
                        operands++;
                        var token = MetadataTokens.EntityHandle(r.ReadInt32());
                        string target = Name(token);
                        if (!inside && ContainsCodec(target)) {
                            hits.Add($"{label} IL_{offset:x4} {op.Name} -> {target}");
                            if (op == OpCodes.Call || op == OpCodes.Callvirt) {
                                if (target.Contains(e4 ? "Gava1ContextCodec::Decode" : "ValidationPreCodec::DecodeBootstrap")) bootCalls.Add(label);
                                if (target.Contains(e4 ? "Gava1ContextCodec::MatchPreBootstrap" : "ValidationPreCodecBinary::ValidatePayload")) binaryCalls.Add(label);
                            }
                        }
                        if (inside && target.StartsWith("Gagamba.", StringComparison.Ordinal) &&
                            !ContainsCodec(target)) outbound.Add(label + " -> " + target);
                        break;
                    case OperandType.InlineString:
                        literals++;
                        var literal = md.GetUserString(MetadataTokens.UserStringHandle(r.ReadInt32() & 0x00ffffff));
                        if (!inside && Literal(literal)) hits.Add(label + " string names codec");
                        break;
                    case OperandType.InlineSwitch:
                        int count = r.ReadInt32();
                        r.Offset = checked(r.Offset + count * 4);
                        break;
                    default:
                        r.Offset += op.OperandType switch {
                            OperandType.InlineNone => 0,
                            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                            OperandType.InlineVar => 2,
                            OperandType.InlineBrTarget or OperandType.InlineI or OperandType.ShortInlineR => 4,
                            OperandType.InlineI8 or OperandType.InlineR => 8,
                            _ => throw new InvalidDataException("Unhandled IL operand " + op.OperandType)
                        };
                        break;
                }
            }
        }
    }
    var typeRefs = md.TypeReferences.Select(h => Name(h)).Where(IsCodec).ToArray();
    var namedResources = md.ManifestResources.Select(h => md.GetString(md.GetManifestResource(h).Name)).Where(Literal).ToArray();
    var scope = md.AssemblyReferences.Select(h => md.GetString(md.GetAssemblyReference(h).Name)).ToArray();
    bool pass = test ? bootCalls.Count > 0 && binaryCalls.Count > 0
        : hits.Count == 0 && outbound.Count == 0 && typeRefs.Length == 0 && visibility.Count == 0
          && namedResources.Length == 0 && (!core || codecTypes.Count >= codec.Length);
    if (!pass) failed++;
    Console.WriteLine(JsonSerializer.Serialize(new {
        file, assembly, moduleId, sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file))),
        pass, bodies, operands, signatures, literals, moduleInitializers, scope, typeRefs, codecTypes, visibility,
        namedResources, outbound = outbound.Distinct(), knownBootstrapCallers = bootCalls.Distinct(),
        knownBinaryCallers = binaryCalls.Distinct(), hits
    }));
}
return failed == 0 && args.Length > 0 ? 0 : 1;

sealed class Names : ISignatureTypeProvider<string, object?> {
    static string Join(string ns, string n) => ns.Length == 0 ? n : ns + "." + n;
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte _) {
        var t = r.GetTypeDefinition(h);
        return t.IsNested ? GetTypeFromDefinition(r, t.GetDeclaringType(), 0) + "+" + r.GetString(t.Name)
            : Join(r.GetString(t.Namespace), r.GetString(t.Name));
    }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte _) {
        var t = r.GetTypeReference(h);
        return t.ResolutionScope.Kind == HandleKind.TypeReference
            ? GetTypeFromReference(r, (TypeReferenceHandle)t.ResolutionScope, 0) + "+" + r.GetString(t.Name)
            : Join(r.GetString(t.Namespace), r.GetString(t.Name));
    }
    public string GetTypeFromSpecification(MetadataReader r, object? ctx, TypeSpecificationHandle h, byte _) =>
        r.GetTypeSpecification(h).DecodeSignature(this, ctx);
    public string GetPrimitiveType(PrimitiveTypeCode code) => code.ToString();
    public string GetSZArrayType(string t) => t + "[]";
    public string GetArrayType(string t, ArrayShape s) => t + "[" + new string(',', s.Rank - 1) + "]";
    public string GetByReferenceType(string t) => t + "&";
    public string GetPointerType(string t) => t + "*";
    public string GetPinnedType(string t) => t + " pinned";
    public string GetGenericTypeParameter(object? ctx, int i) => "!" + i;
    public string GetGenericMethodParameter(object? ctx, int i) => "!!" + i;
    public string GetGenericInstantiation(string t, ImmutableArray<string> types) => t + "<" + string.Join(",", types) + ">";
    public string GetModifiedType(string modifier, string t, bool required) => t + " mod(" + modifier + ")";
    public string GetFunctionPointerType(MethodSignature<string> s) => s.ReturnType + "(" + string.Join(",", s.ParameterTypes) + ")*";
}
