using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Direct-call and surface inspection for the E-2-P1 core. This proves
/// the absence of direct callers in the compiled assembly and the absence of a
/// public export; it does not claim transitive unreachability beyond the
/// inspected direct-call tokens.</summary>
public sealed class ValidationReachabilityTests
{
    private const string ValidationNamespace = "Gagamba.Execution.Linux.M1.Validation";

    private static readonly BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly string[] ForbiddenNameTokens =
        ["Approved", "Authorized", "Permit", "Authority", "Trusted", "Capability"];

    private static readonly string[] ForbiddenDependencyPrefixes =
    [
        "System.IO.File", "System.IO.Directory", "System.IO.FileStream", "System.IO.Path",
        "System.Environment", "System.Diagnostics.Process", "System.Net.", "System.Runtime.InteropServices",
    ];

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    private static IEnumerable<MethodBase> Members(Type type) =>
        type.GetMethods(AllMembers).Concat<MethodBase>(type.GetConstructors(AllMembers));

    private static IEnumerable<Type> ValidationTypes() =>
        typeof(FrozenContractVerifier).Assembly.GetTypes()
            .Where(type => type.Namespace == ValidationNamespace);

    [Fact]
    public void ValidationCoreExportsNoPublicApi()
    {
        var assembly = typeof(FrozenContractVerifier).Assembly;
        Assert.DoesNotContain(assembly.GetExportedTypes(), type => type.Namespace == ValidationNamespace);
        var validationTypes = ValidationTypes().ToArray();
        Assert.NotEmpty(validationTypes);
        foreach (var type in validationTypes)
        {
            Assert.False(type.IsPublic, type.FullName);
            Assert.False(type.IsNestedPublic, type.FullName);
        }
    }

    [Fact]
    public void ValidationMemberNamesDoNotImplyAuthority()
    {
        foreach (var type in ValidationTypes())
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
        var caller = typeof(FrozenContractVerifier)
            .GetMethod("TryVerifyChain", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TryVerifyChain not found");
        var target = typeof(FrozenContractVerifier)
            .GetMethod("VerifyManifest", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("VerifyManifest not found");
        var body = caller.GetMethodBody() ?? throw new InvalidOperationException("no body");
        Assert.Contains(target.MetadataToken, MethodTokens(body.GetILAsByteArray() ?? []));
    }

    [Fact]
    public void NoProductionCallerReachesTheValidationCore()
    {
        var assembly = typeof(FrozenContractVerifier).Assembly;
        var targets = new HashSet<int>();
        foreach (var type in ValidationTypes())
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
    public void ValidationCoreReferencesNoFilesystemProcessOrEnvironmentMembers()
    {
        foreach (var type in ValidationTypes())
        {
            foreach (var method in Members(type))
            {
                var body = method.GetMethodBody();
                if (body is null) continue;
                foreach (var token in MethodTokens(body.GetILAsByteArray() ?? []))
                {
                    MethodBase? resolved = null;
                    try
                    {
                        resolved = method.Module.ResolveMethod(token);
                    }
                    catch (Exception)
                    {
                        // Unresolvable generic-context tokens cannot name a
                        // forbidden dependency in this assembly's core.
                    }

                    if (resolved is null) continue;
                    var declaring = resolved.DeclaringType?.FullName ?? string.Empty;
                    Assert.DoesNotContain(ForbiddenDependencyPrefixes,
                        prefix => declaring.StartsWith(prefix, StringComparison.Ordinal));
                }
            }
        }
    }

    [Fact]
    public void CompiledReleaseAssemblyHasNoProductionCallerAndNoPublicExport()
    {
        var repoRoot = FindRepositoryRoot();
        if (repoRoot is null) Assert.Skip("repository root not found from the test directory");
        var releasePath = Path.Combine(repoRoot, "src", "Gagamba.Execution.Linux.M1", "bin", "Release",
            "net10.0", "Gagamba.Execution.Linux.M1.dll");
        if (!File.Exists(releasePath))
            Assert.Skip("the source Release M1 assembly is not built in this environment");
        var loadContext = new AssemblyLoadContext("e2p1-release-scan", isCollectible: true);
        try
        {
            using var stream = File.OpenRead(releasePath);
            var assembly = loadContext.LoadFromStream(stream);
            Assert.DoesNotContain(assembly.GetExportedTypes(), type => type.Namespace == ValidationNamespace);
            var releaseTypes = SafeTypes(assembly);
            var validationTypes = releaseTypes.Where(type => type.Namespace == ValidationNamespace).ToArray();
            Assert.NotEmpty(validationTypes);
            var targets = new HashSet<int>();
            foreach (var type in validationTypes)
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
