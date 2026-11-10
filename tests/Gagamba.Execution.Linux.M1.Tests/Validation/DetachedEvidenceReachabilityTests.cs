using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Direct-call and surface inspection for the E-2-P2 detached-evidence
/// core. This proves the absence of direct callers in the inspected Debug and
/// compiled Release method bodies, the absence of a public export, and the
/// absence of P2 result types in production signatures. Retaining P1-Q1-F05:
/// direct-call absence is the supported static evidence scope, not an exhaustive
/// transitive or reflection-aware reachability proof.</summary>
public sealed class DetachedEvidenceReachabilityTests
{
    private const string ValidationNamespace = "Gagamba.Execution.Linux.M1.Validation";
    private const string ReleaseAssemblyPath = "src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll";

    private static readonly BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] ForbiddenNameTokens =
    [
        "Approved", "Authorized", "Permit", "Authority", "Trusted", "Capability", "Eligible",
        "ValidationAuthority", "VerifyApproval",
    ];

    private static readonly string[] AuthorizedP1Surface =
    [
        "FrozenContractVerifier.TryVerifyCatalog",
        "CanonicalJson.Parse",
        "CanonicalJson.Hash",
        "CanonicalJson.IsLowerHex",
        "FrozenSchemaRules.IsValidId",
    ];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    private static Type[] DetachedEvidenceTypes() =>
    [
        typeof(DetachedEvidenceVerifier),
        typeof(ReviewRecordContract),
        typeof(OwnerReceiptRecordContract),
        typeof(AdminRecordContentContract),
        typeof(DetachedEvidenceBindingsContract),
    ];

    private static IEnumerable<MethodBase> Members(Type type) =>
        type.GetMethods(AllMembers).Concat<MethodBase>(type.GetConstructors(AllMembers));

    private static IEnumerable<MemberInfo> SignatureMembers(Type type) =>
        type.GetMembers(AllMembers);

    private static IEnumerable<Type> SignatureTypes(MemberInfo member) =>
        member switch
        {
            MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType)
                .Prepend(method.ReturnType),
            ConstructorInfo constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType),
            FieldInfo field => [field.FieldType],
            PropertyInfo property => [property.PropertyType],
            _ => [],
        };

    private static MethodBase? Resolve(MethodBase member, int token)
    {
        try
        {
            return member.Module.ResolveMethod(token);
        }
        catch (Exception)
        {
            // Unresolvable generic-context tokens cannot name an authority
            // dependency in this assembly's core.
            return null;
        }
    }

    [Fact]
    public void DetachedEvidenceCoreExportsNoPublicApi()
    {
        var assembly = typeof(DetachedEvidenceVerifier).Assembly;
        Assert.DoesNotContain(assembly.GetExportedTypes(), type => type.Namespace == ValidationNamespace);
        foreach (var type in DetachedEvidenceTypes())
        {
            Assert.False(type.IsPublic, type.FullName);
            Assert.False(type.IsNestedPublic, type.FullName);
        }
    }

    [Fact]
    public void DetachedEvidenceMemberNamesDoNotImplyAuthority()
    {
        foreach (var type in DetachedEvidenceTypes())
        {
            Assert.DoesNotContain(ForbiddenNameTokens,
                token => type.Name.Contains(token, StringComparison.Ordinal));
            foreach (var member in type.GetMembers(AllMembers))
                Assert.DoesNotContain(ForbiddenNameTokens,
                    token => member.Name.Contains(token, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CallerScannerDetectsKnownIntraCoreCalls()
    {
        // Positive control for the reachability method itself.
        var caller = typeof(DetachedEvidenceVerifier)
            .GetMethod("TryVerifyDetachedEvidenceChain", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TryVerifyDetachedEvidenceChain not found");
        var target = typeof(DetachedEvidenceVerifier)
            .GetMethod("VerifyReviewRecord", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("VerifyReviewRecord not found");
        var body = caller.GetMethodBody() ?? throw new InvalidOperationException("no body");
        Assert.Contains(target.MetadataToken, MethodTokens(body.GetILAsByteArray() ?? []));
    }

    [Fact]
    public void NoProductionCallerReachesTheDetachedEvidenceCore()
    {
        var assembly = typeof(DetachedEvidenceVerifier).Assembly;
        var targets = new HashSet<int>();
        foreach (var type in DetachedEvidenceTypes())
            foreach (var member in Members(type))
                targets.Add(member.MetadataToken);
        Assert.NotEmpty(targets);

        var offenders = new List<string>();
        foreach (var type in assembly.GetTypes().Where(type => type.Namespace != ValidationNamespace))
        {
            foreach (var method in Members(type))
            {
                var body = method.GetMethodBody();
                if (body is null) continue;
                foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                    if (targets.Contains(token))
                        offenders.Add($"{type.FullName}.{method.Name} references token 0x{token:x8}");
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void ProductionSignaturesDoNotConsumeDetachedEvidenceResults()
    {
        var assembly = typeof(DetachedEvidenceVerifier).Assembly;
        var detachedTypes = DetachedEvidenceTypes().ToHashSet();
        var offenders = new List<string>();
        foreach (var type in assembly.GetTypes().Where(type => type.Namespace != ValidationNamespace))
        {
            foreach (var member in SignatureMembers(type))
            foreach (var referenced in SignatureTypes(member))
                if (detachedTypes.Contains(referenced))
                    offenders.Add($"{type.FullName}.{member.Name} uses {referenced.FullName}");
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void DetachedEvidenceCoreReferencesOnlyValidationTypesAndTheBaseClassLibrary()
    {
        foreach (var type in DetachedEvidenceTypes())
        {
            foreach (var member in Members(type))
            {
                var body = member.GetMethodBody();
                if (body is null) continue;
                foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                {
                    var resolved = Resolve(member, token);
                    var declaringNamespace = resolved?.DeclaringType?.Namespace;
                    if (declaringNamespace is null ||
                        !declaringNamespace.StartsWith("Gagamba", StringComparison.Ordinal)) continue;
                    Assert.True(declaringNamespace == ValidationNamespace,
                        $"{type.Name}.{member.Name} references {resolved!.DeclaringType!.FullName}");
                }
            }

            foreach (var member in SignatureMembers(type))
            foreach (var referenced in SignatureTypes(member))
            {
                var referencedNamespace = referenced.Namespace;
                if (referencedNamespace is null ||
                    !referencedNamespace.StartsWith("Gagamba", StringComparison.Ordinal)) continue;
                Assert.True(referencedNamespace == ValidationNamespace,
                    $"{type.Name}.{member.Name} exposes {referenced.FullName}");
            }
        }
    }

    [Fact]
    public void DetachedEvidenceCallsOnlyItselfConsumesTheCatalogContractAndUsesTheAuthorizedP1Surface()
    {
        var ownTypeNames = DetachedEvidenceTypes().Select(type => type.FullName!).ToHashSet();
        var offenders = new List<string>();
        foreach (var type in DetachedEvidenceTypes())
        {
            foreach (var member in Members(type))
            {
                var body = member.GetMethodBody();
                if (body is null) continue;
                foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                {
                    var resolved = Resolve(member, token);
                    var declaring = resolved?.DeclaringType;
                    if (declaring?.Namespace != ValidationNamespace) continue;
                    if (ownTypeNames.Contains(declaring.FullName!)) continue;
                    // Compiler-generated nested types (lambdas, record helpers)
                    // belong to their P2 declaring type.
                    if (declaring.IsNested && declaring.DeclaringType is { } outer &&
                        ownTypeNames.Contains(outer.FullName!)) continue;
                    // Reading the verified catalog result is inherent to the
                    // authorized TryVerifyCatalog reuse; no other P1 interface
                    // may be called.
                    if (declaring == typeof(CatalogContract) && resolved!.Name.StartsWith("get_", StringComparison.Ordinal))
                        continue;
                    var surface = $"{declaring.Name}.{resolved!.Name}";
                    if (!AuthorizedP1Surface.Contains(surface))
                        offenders.Add($"{type.Name}.{member.Name} calls {declaring.FullName}.{resolved.Name}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void DetachedEvidenceVerifierIsAStaticInternalClassWithoutInstanceState()
    {
        var type = typeof(DetachedEvidenceVerifier);
        Assert.True(type.IsAbstract && type.IsSealed);
        Assert.False(type.IsPublic);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        foreach (var method in type.GetMethods(AllMembers))
            Assert.True(method.IsStatic, method.Name);
        Assert.Empty(type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public void EveryTryVerifyEntryRequiresCatalogAndPrecedingRecordBytes()
    {
        // There is no single-byte[]-only verify entry: record content cannot be
        // verified without the catalog and preceding detached records.
        var entries = typeof(DetachedEvidenceVerifier).GetMethods(AllMembers)
            .Where(method => method.Name.StartsWith("TryVerify", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(4, entries.Length);
        foreach (var method in entries)
            Assert.True(method.GetParameters().Count(parameter => parameter.ParameterType == typeof(byte[])) >= 2,
                method.Name);
    }

    [Fact]
    public void CompiledReleaseAssemblyHasNoProductionCallerAndNoPublicExportForDetachedEvidence()
    {
        var repoRoot = FindRepositoryRoot();
        if (repoRoot is null) Assert.Skip("repository root not found from the test directory");
        var releasePath = Path.Combine(repoRoot, ReleaseAssemblyPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(releasePath))
            Assert.Skip("the source Release M1 assembly is not built in this environment");
        var loadContext = new AssemblyLoadContext("e2p2-release-scan", isCollectible: true);
        try
        {
            using var stream = File.OpenRead(releasePath);
            var assembly = loadContext.LoadFromStream(stream);
            Assert.DoesNotContain(assembly.GetExportedTypes(), type => type.Namespace == ValidationNamespace);
            var releaseTypes = SafeTypes(assembly);
            var detachedTypeNames = DetachedEvidenceTypes().Select(type => type.FullName!).ToHashSet();
            var releaseDetachedTypes = releaseTypes
                .Where(type => detachedTypeNames.Contains(type.FullName!)).ToArray();
            Assert.Equal(detachedTypeNames.Count, releaseDetachedTypes.Length);
            foreach (var type in releaseDetachedTypes)
            {
                Assert.False(type.IsPublic, type.FullName);
                Assert.False(type.IsNestedPublic, type.FullName);
            }

            var targets = new HashSet<int>();
            foreach (var type in releaseDetachedTypes)
                foreach (var member in Members(type))
                    targets.Add(member.MetadataToken);
            var offenders = new List<string>();
            foreach (var type in releaseTypes.Where(type => type.Namespace != ValidationNamespace))
            {
                foreach (var method in Members(type))
                {
                    var body = method.GetMethodBody();
                    if (body is null) continue;
                    foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                        if (targets.Contains(token))
                            offenders.Add($"{type.FullName}.{method.Name} references token 0x{token:x8}");
                }
            }

            Assert.Empty(offenders);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var level = 0; level < 12 && directory is not null; level++, directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
                return directory.FullName;
        return null;
    }

    private static Type[] SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }

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
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar =>
                ReadByte(il, ref position),
            OperandType.InlineVar => ReadUInt16(il, ref position),
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod
                or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok
                or OperandType.InlineType or OperandType.InlineI => ReadInt32(il, ref position),
            OperandType.ShortInlineR => ReadInt32(il, ref position),
            OperandType.InlineI8 or OperandType.InlineR => ReadInt64(il, ref position),
            OperandType.InlineSwitch => ReadSwitch(il, ref position),
            _ => throw new BadImageFormatException($"unsupported operand kind {opcode.OperandType}"),
        };

    private static byte ReadByte(byte[] il, ref int position)
    {
        if (position + 1 > il.Length) throw new BadImageFormatException("truncated operand");
        return il[position++];
    }

    private static ushort ReadUInt16(byte[] il, ref int position)
    {
        if (position + 2 > il.Length) throw new BadImageFormatException("truncated operand");
        var value = BitConverter.ToUInt16(il, position);
        position += 2;
        return value;
    }

    private static int ReadInt32(byte[] il, ref int position)
    {
        if (position + 4 > il.Length) throw new BadImageFormatException("truncated operand");
        var value = BitConverter.ToInt32(il, position);
        position += 4;
        return value;
    }

    private static long ReadInt64(byte[] il, ref int position)
    {
        if (position + 8 > il.Length) throw new BadImageFormatException("truncated operand");
        var value = BitConverter.ToInt64(il, position);
        position += 8;
        return value;
    }

    private static long ReadSwitch(byte[] il, ref int position)
    {
        if (position + 4 > il.Length) throw new BadImageFormatException("truncated operand");
        var count = BitConverter.ToInt32(il, position);
        position += 4;
        if (count < 0 || count > (il.Length - position) / 4)
            throw new BadImageFormatException("invalid switch table");
        position += count * 4;
        return count;
    }
}
