// F-02-PARITY-Q1 independent IL probe (reviewer-owned, external scratch only).
// Modes:
//   calls   <asm> <type> <method>  - resolved call/member references in IL order
//   ilhash  <asm> <type> <method>  - SHA-256 + length of the method body IL
//   callers <asm> <type> <method>  - methods whose IL contains the target metadata
//                                    token in InlineMethod/Field/Tok/Type operands
//                                    (mirrors IlCallScanner token matching)
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

internal static class Program
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<ushort, OpCode> Ops = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(OpCode))
        .Select(f => (OpCode)f.GetValue(null)!)
        .ToDictionary(o => unchecked((ushort)o.Value));

    private static void Main(string[] args)
    {
        var mode = args[0];
        var assembly = Assembly.LoadFrom(Path.GetFullPath(args[1]));

        if (mode == "info")
        {
            var info = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .Cast<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "(none)";
            Console.WriteLine($"MVID {assembly.ManifestModule.ModuleVersionId} INFO {info}");
            return;
        }

        if (mode == "exports")
        {
            var exported = assembly.GetExportedTypes();
            Console.WriteLine($"exported type count: {exported.Length}");
            foreach (var t in exported) Console.WriteLine(t.FullName);
            return;
        }

        var type = assembly.GetType(args[2], throwOnError: true)!;
        var method = type.GetMethods(All).Single(m => m.Name == args[3]);

        switch (mode)
        {
            case "calls":
                foreach (var (offset, name) in Calls(method)) Console.WriteLine($"{offset:D6} {name}");
                break;
            case "ilhash":
            {
                var il = method.GetMethodBody()!.GetILAsByteArray()!;
                Console.WriteLine($"{Convert.ToHexString(SHA256.HashData(il))} {il.Length}");
                break;
            }
            case "callers":
                foreach (var t in assembly.GetTypes())
                {
                    var members = t.GetMethods(All).Cast<MethodBase>().Concat(t.GetConstructors(All));
                    foreach (var mb in members)
                    {
                        var body = mb.GetMethodBody();
                        if (body is null) continue;
                        if (ContainsToken(body.GetILAsByteArray()!, method.MetadataToken))
                            Console.WriteLine($"{t.FullName}::{mb.Name}");
                    }
                }
                break;
            default:
                throw new InvalidOperationException("unknown mode " + mode);
        }
    }

    private static IReadOnlyList<(int Offset, string Name)> Calls(MethodBase method)
    {
        var body = method.GetMethodBody() ?? throw new InvalidOperationException("no body");
        var il = body.GetILAsByteArray() ?? Array.Empty<byte>();
        var result = new List<(int, string)>();
        var position = 0;
        while (position < il.Length)
        {
            var start = position;
            ushort key = il[position++];
            if (key == 0xfe) key = (ushort)(0xfe00 | il[position++]);
            if (!Ops.TryGetValue(key, out var opcode)) throw new BadImageFormatException($"opcode 0x{key:x4}");
            var operand = ReadOperand(opcode, il, ref position);
            if (opcode.OperandType is OperandType.InlineMethod or OperandType.InlineTok)
            {
                string name;
                try
                {
                    var member = method.Module.ResolveMember((int)operand, method.GetGenericArguments(), null);
                    name = member.DeclaringType is null ? member.Name : member.DeclaringType.FullName + "::" + member.Name;
                }
                catch (Exception ex)
                {
                    name = $"<unresolved token 0x{operand:x8}: {ex.GetType().Name}>";
                }
                result.Add((start, name));
            }
        }
        return result;
    }

    private static bool ContainsToken(byte[] il, int token)
    {
        var position = 0;
        while (position < il.Length)
        {
            ushort key = il[position++];
            if (key == 0xfe) key = (ushort)(0xfe00 | il[position++]);
            if (!Ops.TryGetValue(key, out var opcode)) throw new BadImageFormatException($"opcode 0x{key:x4}");
            var operand = ReadOperand(opcode, il, ref position);
            if (opcode.OperandType is OperandType.InlineMethod or OperandType.InlineField
                or OperandType.InlineTok or OperandType.InlineType
                && unchecked((int)operand) == token)
                return true;
        }
        return false;
    }

    private static long ReadOperand(OpCode opcode, byte[] il, ref int position)
    {
        switch (opcode.OperandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return il[position++];
            case OperandType.InlineVar:
            {
                var value = BitConverter.ToUInt16(il, position);
                position += 2;
                return value;
            }
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.InlineI:
            case OperandType.ShortInlineR:
            {
                var value = BitConverter.ToInt32(il, position);
                position += 4;
                return value;
            }
            case OperandType.InlineI8:
            case OperandType.InlineR:
            {
                var value = BitConverter.ToInt64(il, position);
                position += 8;
                return value;
            }
            case OperandType.InlineSwitch:
            {
                var count = BitConverter.ToInt32(il, position);
                position += 4 + 4 * count;
                return count;
            }
            default:
                throw new BadImageFormatException($"operand kind {opcode.OperandType}");
        }
    }
}
