# Managed tests, compiled reachability and revision gates

## Executed managed qualification

Windows, .NET SDK `10.0.401`, project
`tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj`.
From Gagamba root, `$root` was
`Join-Path ([IO.Path]::GetTempPath()) 'gagamba-e3-v3pre-1c7fb67'`,
outside the checkout. Run against the source/test tree identified in
[traceability](normative-traceability.md).

```powershell
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --artifacts-path (Join-Path $root 'managed') --nologo --filter 'FullyQualifiedName~ValidationPreCodec|FullyQualifiedName~InstallationWireTests' --logger 'trx;LogFileName=final.trx' --results-directory (Join-Path $root 'results')
```

| Cases reported by TRX | Passed | Failed | Skipped | Total |
|---|---:|---:|---:|---:|
| New bootstrap and refusal | 64 | 0 | 0 | 64 |
| New binary and refusal | 58 | 0 | 0 | 58 |
| Existing InstallationWireTests regression | 24 | 0 | 0 | 24 |
| Final filtered run, exit 0 | 146 | 0 | 0 | 146 |

The initial app-sandbox temporary path yielded a Windows testhost startup
error before any tests. Moving artifacts outside that path resolved it.
The earlier real-temp run passed 145 cases. The final run covers the
literal all-zero capability-digest control and the actual ReleaseRequest
opcode exclusion. No native cases or broad M1 suite were run.

## Compiled evidence

Executed all eight project/configuration builds with exit 0, zero
warnings and errors, by varying `$configuration` between Debug and
Release and `$project` across the following paths.

```powershell
dotnet build $project -c $configuration --artifacts-path (Join-Path $root 'qualified') --nologo
```

- `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`
- `src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj`
- `src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj`
- `eng/InstallTool/InstallTool.csproj`

The source reference walk found Owner, Recovery and InstallTool as the
non-test consumers. Their copied M1 DLLs hash-match the qualified M1 DLL
in their respective configuration. The scanner below reads PE/CLI
metadata without loading checked assemblies. Execute as
`dotnet run --file $scan -- @files` with `$scan` resolving to the C# below
saved outside the repository, and `$files` the eight built files
`$root/qualified/bin/<project>/<configuration>/<assembly>.dll`. Include
the actual managed test M1 copy and test DLL under
`$root/managed/bin/Gagamba.Execution.Linux.M1.Tests/debug/` to exercise
the test caller positive control.

| Assembly/configuration | SHA-256 | Bodies | Consumer hits / codec TypeRefs |
|---|---|---:|---:|
| M1 Debug | `ecc05531aa8e662f8ff345c892d9bf08bbbf3067dc3f1ecee3be68d857eade47` | 4,753 | 0 / 0 |
| Owner Debug | `a49f4815d41523bfb91e6870158ef2fbcbf462c9b1740852212b3a7bd70ddeed` | 3 | 0 / 0 |
| Recovery Debug | `774214be06ea5c9a5b8c5f699efeeea6b0d6dc4e8bd40105a16da8e53136879c` | 2 | 0 / 0 |
| InstallTool Debug | `d7ee0f15565ff5599875136340defd753f09f6fb17e3fe83a805b46f56babab5` | 3 | 0 / 0 |
| M1 Release | `fcccf7cf3003c14d40333ac34e044ee96017d02a37c530710e1c5195c08633f5` | 4,600 | 0 / 0 |
| Owner Release | `1f146b4f6ab86d935417a5464e9248c109131ddd94b05d17fe33dd5df7b3818b` | 3 | 0 / 0 |
| Recovery Release | `3b7a76a9bc1198d38bcddd73a3b599e575dd851810ef6268b844133362936d2f` | 2 | 0 / 0 |
| InstallTool Release | `68e849407c50b1bff272d9accea41141f63bb6834658f7dd1b64d2939653f815` | 3 | 0 / 0 |
| M1 Debug copy used by tests | `c2c3127ce3eee635bf1b4e34679bc23cff6f213d2e8b6aee6e13756f7cbb2894` | 4,753 | 0 / 0 |
| Tests Debug, positive control | `fa5cf77e14f284673d5dccb6ab8201a57fbb485211c7f94e0d4a114d8262be44` | 4,902 | 61 / 9 |

Both scanner runs exited 0. Each M1 included ten internal codec type
definitions including the private schema rule type and generated nested
metadata. The qualified library scan examined 8,700/8,413 type/member
signature checks and 37,712/36,496 IL metadata operands in Debug/Release.
Both scans found zero production inbound calls, constructor/delegate or
type token hits, static initialization callers, or codec-naming user
strings outside codec code. The positive test DLL yielded 14 distinct
calling methods, including the bootstrap verification helper and binary
check helper. The initial scratch scan misidentified the nested framework
`JsonElement.ObjectEnumerator` as an unqualified outbound type. The
recorded scan resolves nested type references through their metadata
scope. No compiled production artifact was changed by that correction.

Owner/Recovery dispatch, project references, SDK compile globs and friend
declarations are unchanged. InstallTool and core dependencies are unchanged.
No registrations, reflection hooks, public surfaces, feature switches,
host or native call sites were added. Static metadata inspection covers
literal names and statically resolvable references. Computed reflection
and arbitrary external invocation are outside its proof, with the
unchanged source/build activation paths providing separate evidence.
The tests are the only known callers of the new internal API.

## Unchanged RELEASE verifier and P4

The two actual inputs were the exact qualified core DLLs above under
`$root/qualified/bin/Gagamba.Execution.Linux.M1/release/` and
`$root/qualified/bin/Gagamba.Execution.Linux.M1/debug/`.

```powershell
dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production $releaseM1
dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production $debugM1
```

- Release exit **0**, `RESULT: PASS`, 0/4 hook types, 0/3 hook properties,
  14/14 required types.
- Debug-as-production exit **1**, `RESULT: FAIL`. The run reported
  `assembly configuration is 'Debug', expected Release`, forbidden hook
  types/properties and Debug-only authority factories. The verifier decoded
  the input and found 14/14 required types. This is the intended product
  policy failure.

The authorized new parsers and comparisons trigger exact-revision review
of refusal, downgrade and possible consumer paths. The executed legacy
refusal controls, zero inbound production references and six-path
source delta preserve the existing sink boundaries. All pre-existing
source, project, build, verifier and test files match the entry baseline.
The production reverse-call inventory from
`git grep -n -E 'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(' HEAD -- src`
has the previously accepted three call sites for each S10/S11 callee
and no S28 production construction. Since the new assembly surfaces
introduce no production route or effect, the accepted upstream
first-effect dominance and O-R1/O-R2 restrictions still apply.
O-R3 refusal-reason typing and the observation-to-effect residual remain
open. This is producer evidence on the exact code bytes. Independent
P4 assessment is pending, and E-4/E-6 re-audits remain mandatory.
`NATIVE-PENDING` and all 331 inherited native skips remain unqualified.

## Reproduce the metadata scan

Save the following as `Scan.cs` outside the worktree and run the
file-based .NET SDK 10 command above. The scanner is review evidence.

~~~csharp
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!)
    .ToDictionary(op => unchecked((ushort)op.Value));
var failures = 0;
foreach (var file in args)
{
    using var stream = File.OpenRead(file);
    using var pe = new PEReader(stream);
    var md = pe.GetMetadataReader();
    var provider = new TargetSignature();
    var assembly = md.GetString(md.GetAssemblyDefinition().Name);
    var core = assembly == "Gagamba.Execution.Linux.M1";
    var test = assembly == "Gagamba.Execution.Linux.M1.Tests";
    var scanErrors = new List<string>();
    var hits = new List<string>();
    var bodies = 0;
    var tokens = 0;
    var signatureChecks = 0;
    var targets = 0;
    var calls = new HashSet<string>();
    var outsideDependencies = new HashSet<string>();

    bool Target(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => provider.GetTypeFromDefinition(md, (TypeDefinitionHandle)handle, 0),
        HandleKind.TypeReference => provider.GetTypeFromReference(md, (TypeReferenceHandle)handle, 0),
        HandleKind.TypeSpecification => provider.GetTypeFromSpecification(md, null, (TypeSpecificationHandle)handle, 0),
        HandleKind.MethodDefinition => Target(md.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType()),
        HandleKind.FieldDefinition => Target(md.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType()),
        HandleKind.MemberReference => Target(md.GetMemberReference((MemberReferenceHandle)handle).Parent),
        HandleKind.MethodSpecification => Target(md.GetMethodSpecification((MethodSpecificationHandle)handle).Method)
            || md.GetMethodSpecification((MethodSpecificationHandle)handle).DecodeSignature(provider, null).Any(b => b),
        HandleKind.StandaloneSignature => Signature((StandaloneSignatureHandle)handle),
        _ => false
    };

    bool Signature(StandaloneSignatureHandle handle)
    {
        var signature = md.GetStandaloneSignature(handle);
        return signature.GetKind() == StandaloneSignatureKind.LocalVariables
            ? signature.DecodeLocalSignature(provider, null).Any(b => b)
            : TargetSignature.Method(signature.DecodeMethodSignature(provider, null));
    }

    foreach (var td in md.TypeDefinitions)
    {
        var type = md.GetTypeDefinition(td);
        var topTarget = Target(td);
        var ordinaryTypeName = md.GetString(type.Name);
        if (topTarget)
        {
            targets++;
            var visibility = type.Attributes & TypeAttributes.VisibilityMask;
            if (visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic)
                scanErrors.Add("public codec type " + ordinaryTypeName);
        }
        else
        {
            signatureChecks++;
            if (!type.BaseType.IsNil && Target(type.BaseType))
                hits.Add(ordinaryTypeName + " base");
            foreach (var handle in type.GetInterfaceImplementations())
                if (Target(md.GetInterfaceImplementation(handle).Interface)) hits.Add(ordinaryTypeName + " interface");
            foreach (var handle in type.GetFields())
            {
                signatureChecks++;
                if (md.GetFieldDefinition(handle).DecodeSignature(provider, null)) hits.Add(ordinaryTypeName + " field");
            }
            foreach (var handle in type.GetProperties())
            {
                signatureChecks++;
                if (TargetSignature.Method(md.GetPropertyDefinition(handle).DecodeSignature(provider, null)))
                    hits.Add(ordinaryTypeName + " property");
            }
            foreach (var handle in type.GetEvents())
                if (Target(md.GetEventDefinition(handle).Type)) hits.Add(ordinaryTypeName + " event");
            CheckAttributes(type.GetCustomAttributes(), ordinaryTypeName);
            CheckConstraints(type.GetGenericParameters(), ordinaryTypeName);
        }
        foreach (var methodHandle in type.GetMethods())
        {
            var method = md.GetMethodDefinition(methodHandle);
            var label = ordinaryTypeName + "::" + md.GetString(method.Name);
            if (!topTarget)
            {
                signatureChecks++;
                if (TargetSignature.Method(method.DecodeSignature(provider, null))) hits.Add(label + " signature");
                CheckAttributes(method.GetCustomAttributes(), label);
                CheckConstraints(method.GetGenericParameters(), label);
            }

            if (method.RelativeVirtualAddress == 0)
                continue;
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            if (!topTarget && !body.LocalSignature.IsNil && Target(body.LocalSignature)) hits.Add(label + " local signature");
            foreach (var region in body.ExceptionRegions)
                if (!topTarget && region.Kind == ExceptionRegionKind.Catch && Target(region.CatchType))
                    hits.Add(label + " exception type");
            var bytes = body.GetILReader();
            bodies++;
            while (bytes.RemainingBytes > 0)
            {
                var first = bytes.ReadByte();
                var opcode = opcodes[first == 0xfe ? (ushort)(0xfe00 | bytes.ReadByte()) : first];
                switch (opcode.OperandType)
                {
                    case OperandType.InlineType:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineSig:
                        tokens++;
                        var entity = MetadataTokens.EntityHandle(bytes.ReadInt32());
                        if (Target(entity) && !topTarget)
                        {
                            hits.Add(label + " " + opcode.Name);
                            if (opcode == OpCodes.Call || opcode == OpCodes.Callvirt)
                                calls.Add(label);
                        }
                        if (topTarget && !Target(entity) && OutboundType(entity) is { } outbound
                            && !outbound.StartsWith("System.", StringComparison.Ordinal)
                            && !outbound.StartsWith(".<PrivateImplementationDetails>", StringComparison.Ordinal))
                            outsideDependencies.Add(label + " -> " + outbound);
                        break;
                    case OperandType.InlineString:
                        var name = md.GetUserString(MetadataTokens.UserStringHandle(bytes.ReadInt32() & 0xffffff));
                        if (!topTarget && name.Contains("ValidationPreCodec", StringComparison.Ordinal))
                            hits.Add(label + " user string");
                        break;
                    case OperandType.InlineSwitch:
                        var count = bytes.ReadInt32();
                        bytes.Offset = checked(bytes.Offset + 4 * count);
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        bytes.ReadByte();
                        break;
                    case OperandType.InlineVar:
                        bytes.ReadUInt16();
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        bytes.ReadInt32();
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        bytes.ReadInt64();
                        break;
                    case OperandType.InlineNone:
                        break;
                    default:
                        throw new InvalidDataException(opcode.OperandType.ToString());
                }
            }
        }
    }

    void CheckAttributes(CustomAttributeHandleCollection collection, string label)
    {
        foreach (var entry in collection)
            if (Target(md.GetCustomAttribute(entry).Constructor))
                hits.Add(label + " attribute");
    }

    void CheckConstraints(GenericParameterHandleCollection collection, string label)
    {
        foreach (var parameter in collection)
            foreach (var constraint in md.GetGenericParameter(parameter).GetConstraints())
                if (Target(md.GetGenericParameterConstraint(constraint).Type))
                    hits.Add(label + " constraint");
    }

    string? OutboundType(EntityHandle entity) => entity.Kind switch
    {
        HandleKind.MethodDefinition => OutboundType(md.GetMethodDefinition((MethodDefinitionHandle)entity).GetDeclaringType()),
        HandleKind.FieldDefinition => OutboundType(md.GetFieldDefinition((FieldDefinitionHandle)entity).GetDeclaringType()),
        HandleKind.MemberReference => OutboundType(md.GetMemberReference((MemberReferenceHandle)entity).Parent),
        HandleKind.MethodSpecification => OutboundType(md.GetMethodSpecification((MethodSpecificationHandle)entity).Method),
        HandleKind.TypeDefinition => md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)entity).Namespace) + "."
            + md.GetString(md.GetTypeDefinition((TypeDefinitionHandle)entity).Name),
        HandleKind.TypeReference => QualifiedReference((TypeReferenceHandle)entity),
        _ => null
    };

    string QualifiedReference(TypeReferenceHandle handle)
    {
        var type = md.GetTypeReference(handle);
        return (type.ResolutionScope.Kind == HandleKind.TypeReference
                ? QualifiedReference((TypeReferenceHandle)type.ResolutionScope)
                : md.GetString(type.Namespace)) + "." + md.GetString(type.Name);
    }

    var externalRefs = md.TypeReferences.Count(h => Target(h));
    if (core && targets == 0) scanErrors.Add("missing codec type definitions");
    if (test && (externalRefs == 0 || calls.Count == 0)) scanErrors.Add("test caller positive control did not fire");
    if (!test && (hits.Count != 0 || externalRefs != 0)) scanErrors.Add("production inbound target reference");
    if (outsideDependencies.Count != 0) scanErrors.Add("codec depends on code outside the codec/framework");
    Console.WriteLine($"{assembly} {Path.GetDirectoryName(file)!.Split(Path.DirectorySeparatorChar)[^1]} "
        + $"types={targets} method-bodies={bodies} signatures={signatureChecks} tokens={tokens} "
        + $"target-type-refs={externalRefs} outside-hits={hits.Count} call-sites={calls.Count} "
        + $"outside-dependencies={outsideDependencies.Count} errors={scanErrors.Count} "
        + $"sha256={Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)))}");
    foreach (var item in outsideDependencies.Concat(scanErrors)) Console.WriteLine(item);
    if (!test) foreach (var hit in hits) Console.WriteLine(hit);
    if (test) foreach (var call in calls.Order()) Console.WriteLine("positive: " + call);
    failures += scanErrors.Count;
}
return failures == 0 ? 0 : 1;

sealed class TargetSignature : ISignatureTypeProvider<bool, object?>
{
    private static bool Match(string name) => name.StartsWith("ValidationPre", StringComparison.Ordinal)
        && !name.EndsWith("Tests", StringComparison.Ordinal) && name != "ValidationPreCodecVectors";

    public static bool Method(MethodSignature<bool> signature) => signature.ReturnType || signature.ParameterTypes.Any(b => b);

    public bool GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeDefinition(handle);
        return Match(reader.GetString(type.Name))
            || (type.IsNested && GetTypeFromDefinition(reader, type.GetDeclaringType(), rawTypeKind));
    }

    public bool GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = reader.GetTypeReference(handle);
        return Match(reader.GetString(type.Name)) || (type.ResolutionScope.Kind == HandleKind.TypeReference
            && GetTypeFromReference(reader, (TypeReferenceHandle)type.ResolutionScope, rawTypeKind));
    }

    public bool GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, context);
    public bool GetArrayType(bool element, ArrayShape shape) => element;
    public bool GetByReferenceType(bool element) => element;
    public bool GetFunctionPointerType(MethodSignature<bool> signature) => Method(signature);
    public bool GetGenericInstantiation(bool genericType, ImmutableArray<bool> arguments) => genericType || arguments.Any(b => b);
    public bool GetGenericMethodParameter(object? context, int index) => false;
    public bool GetGenericTypeParameter(object? context, int index) => false;
    public bool GetModifiedType(bool modifier, bool unmodified, bool required) => modifier || unmodified;
    public bool GetPinnedType(bool element) => element;
    public bool GetPointerType(bool element) => element;
    public bool GetPrimitiveType(PrimitiveTypeCode type) => false;
    public bool GetSZArrayType(bool element) => element;
}
~~~
