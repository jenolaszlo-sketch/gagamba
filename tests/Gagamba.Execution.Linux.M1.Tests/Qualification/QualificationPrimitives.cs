// RC2A-02A qualification primitives (test-only). This file is part of the
// RC2A-02A Windows qualification suite; it adds no production code. The raw
// wire writer reimplements the schema-1 grammar byte-for-byte for test input
// construction and offset recording. It never calls ManifestCodec or
// RestrictedManifestCodec; agreement with the committed golden literal is
// asserted by the harness self-tests.
using System.Collections.ObjectModel;
using System.Text;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationUtf8
{
    internal static readonly Encoding Strict =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}

/// <summary>Raw byte writer with no canonicalization. Tests build both valid
/// and deliberately malformed wire images with it.</summary>
internal sealed class QualificationWireWriter
{
    private readonly List<byte> data = new(1024);

    internal int Position => data.Count;

    internal void U8(byte value) => data.Add(value);

    internal void U16(ushort value)
    {
        U8((byte)(value & 0xFF));
        U8((byte)((value >> 8) & 0xFF));
    }

    internal void U32(uint value)
    {
        U8((byte)(value & 0xFF));
        U8((byte)((value >> 8) & 0xFF));
        U8((byte)((value >> 16) & 0xFF));
        U8((byte)((value >> 24) & 0xFF));
    }

    internal void U64(ulong value)
    {
        for (var shift = 0; shift < 64; shift += 8)
        {
            U8((byte)((value >> shift) & 0xFF));
        }
    }

    internal void Bool(bool value) => U8(value ? (byte)1 : (byte)0);

    internal void Tag(byte value) => U8(value);

    internal void Text(string value)
    {
        var bytes = QualificationUtf8.Strict.GetBytes(value);
        U32((uint)bytes.Length);
        data.AddRange(bytes);
    }

    /// <summary>Length-prefixed content that may be invalid UTF-8 on purpose.</summary>
    internal void TextRaw(ReadOnlySpan<byte> utf8)
    {
        U32((uint)utf8.Length);
        data.AddRange(utf8);
    }

    internal void Raw(ReadOnlySpan<byte> bytes) => data.AddRange(bytes);

    internal byte[] ToArray() => data.ToArray();
}

/// <summary>One recorded byte range of a built image. ValueEligible marks bytes
/// whose field semantics can represent an alternative value (digest content,
/// scalar content, free-text content, free booleans, budget kind/qualification
/// tags); structural bytes can never survive a single-byte mutation.</summary>
internal sealed record QualificationWireRegion(int Start, int Length, bool ValueEligible, string SlotId);

internal sealed class QualificationWireBuild
{
    internal required byte[] Bytes { get; init; }

    internal required IReadOnlyDictionary<string, int> Offsets { get; init; }

    internal int DefectOffset { get; init; } = -1;

    internal required string Description { get; init; }

    internal IReadOnlyList<QualificationWireRegion> Regions { get; init; } = Array.Empty<QualificationWireRegion>();

    internal int OffsetOf(string key) =>
        key == "defect"
            ? DefectOffset >= 0
                ? DefectOffset
                : throw new InvalidOperationException("No defect was written for this build.")
            : Offsets.TryGetValue(key, out var offset)
                ? offset
                : throw new InvalidOperationException($"Unknown wire offset key '{key}'.");
}

internal static class QualificationHash
{
    internal static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();

    internal static IReadOnlyDictionary<string, int> Freeze(Dictionary<string, int> offsets) =>
        new ReadOnlyDictionary<string, int>(offsets);
}
