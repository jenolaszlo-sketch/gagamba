using System.Reflection;
using System.Reflection.Emit;

namespace Gagamba.Execution.Linux.M1.Tests.Lifecycle;

/// <summary>Minimal IL metadata-token scanner used by deterministic owner
/// wiring tests. It reads only call/newobj tokens from methods in the M1
/// assembly; it does not execute the method or substitute for a native run.</summary>
internal static class IlCallScanner
{
    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    internal static IReadOnlyList<int> CallOffsets(MethodBase method, MemberInfo target)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(target);
        var body = method.GetMethodBody() ?? throw new InvalidOperationException(
            $"{method.DeclaringType?.Name}.{method.Name} has no IL body");
        var il = body.GetILAsByteArray() ?? Array.Empty<byte>();
        var token = target.MetadataToken;
        var offsets = new List<int>();
        var position = 0;
        while (position < il.Length)
        {
            var start = position;
            ushort key = il[position++];
            if (key == 0xfe)
            {
                if (position >= il.Length)
                    throw new BadImageFormatException("truncated two-byte opcode");
                key = (ushort)(0xfe00 | il[position++]);
            }

            if (!OpCodesByValue.TryGetValue(key, out var opcode))
                throw new BadImageFormatException($"unknown opcode 0x{key:x4}");
            var operand = ReadOperand(opcode, il, ref position);
            if (opcode.OperandType is OperandType.InlineMethod or OperandType.InlineField
                or OperandType.InlineTok or OperandType.InlineType
                && unchecked((int)operand) == token)
            {
                offsets.Add(start);
            }
        }

        return offsets;
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
        Require(il, position, 1);
        return il[position++];
    }

    private static ushort ReadUInt16(byte[] il, ref int position)
    {
        Require(il, position, 2);
        var value = BitConverter.ToUInt16(il, position);
        position += 2;
        return value;
    }

    private static int ReadInt32(byte[] il, ref int position)
    {
        Require(il, position, 4);
        var value = BitConverter.ToInt32(il, position);
        position += 4;
        return value;
    }

    private static long ReadInt64(byte[] il, ref int position)
    {
        Require(il, position, 8);
        var value = BitConverter.ToInt64(il, position);
        position += 8;
        return value;
    }

    private static long ReadSwitch(byte[] il, ref int position)
    {
        Require(il, position, 4);
        var count = BitConverter.ToInt32(il, position);
        position += 4;
        if (count < 0 || count > (il.Length - position) / 4)
            throw new BadImageFormatException("invalid switch table");
        position += count * 4;
        return count;
    }

    private static void Require(byte[] il, int position, int count)
    {
        if (position + count > il.Length)
            throw new BadImageFormatException("truncated operand");
    }
}
