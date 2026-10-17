using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Launch;

internal enum CoordinatorStatusKind : ushort
{
    Held = 1,
    ProtocolFailed = 6,
}

internal sealed record CoordinatorFrame(
    CoordinatorStatusKind Kind,
    ulong Sequence,
    Guid ExecutionId,
    Guid PreparationId,
    byte[] ManifestDigest,
    byte[] Payload);

internal sealed record CoordinatorHeldReport(
    ushort Flags,
    uint Uid,
    uint EffectiveUid,
    uint Gid,
    uint EffectiveGid,
    uint CoordinatorNamespacePid,
    uint NoNewPrivs,
    uint SeccompMode,
    uint Dumpable,
    ulong CapEff,
    ulong CapPrm,
    ulong CapAmb,
    ulong NoFileSoft,
    ulong NoFileHard,
    ulong CoreSoft,
    ulong CoreHard,
    IReadOnlyList<ulong> NamespaceInodes,
    IReadOnlyList<(ulong Device, ulong Inode)> Objects,
    string Cgroup,
    string MountInfo);

internal static class CoordinatorProtocol
{
    internal const int HeaderBytes = 96;
    internal const int MaxFrameBytes = 4096;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("GGM1CTL\0");

    internal static bool TryDecode(
        ReadOnlySpan<byte> frame,
        Guid expectedExecutionId,
        Guid expectedPreparationId,
        ReadOnlySpan<byte> expectedDigest,
        ulong expectedSequence,
        out CoordinatorFrame? decoded,
        out string error)
    {
        decoded = null;
        error = string.Empty;
        if (frame.Length < HeaderBytes || frame.Length > MaxFrameBytes)
        {
            error = "coordinator frame length is outside the bound";
            return false;
        }

        if (!frame[..8].SequenceEqual(Magic)
            || BinaryPrimitives.ReadUInt16LittleEndian(frame[8..]) != 1)
        {
            error = "coordinator protocol magic/version mismatch";
            return false;
        }

        var kind = (CoordinatorStatusKind)BinaryPrimitives.ReadUInt16LittleEndian(frame[10..]);
        var total = BinaryPrimitives.ReadUInt32LittleEndian(frame[12..]);
        var sequence = BinaryPrimitives.ReadUInt64LittleEndian(frame[16..]);
        var execution = FromRfc4122(frame.Slice(24, 16));
        var preparation = FromRfc4122(frame.Slice(40, 16));
        if (kind is not (CoordinatorStatusKind.Held or CoordinatorStatusKind.ProtocolFailed)
            || total != frame.Length || sequence != expectedSequence
            || execution != expectedExecutionId || preparation != expectedPreparationId
            || expectedDigest.Length != 32 || !frame.Slice(56, 32).SequenceEqual(expectedDigest)
            || frame[88..96].IndexOfAnyExcept((byte)0) >= 0)
        {
            error = "coordinator frame identity, sequence, kind or length mismatch";
            return false;
        }

        decoded = new CoordinatorFrame(kind, sequence, execution, preparation,
            frame.Slice(56, 32).ToArray(), frame[HeaderBytes..].ToArray());
        return true;
    }

    internal static bool TryDecodeHeldReport(
        CoordinatorFrame frame,
        out CoordinatorHeldReport? report,
        out string error)
    {
        report = null;
        error = string.Empty;
        if (frame.Kind != CoordinatorStatusKind.Held)
        {
            error = "coordinator did not report HELD";
            return false;
        }

        var p = frame.Payload.AsSpan();
        const int fixedBytes = 194;
        if (p.Length < fixedBytes + 4 || BinaryPrimitives.ReadUInt16LittleEndian(p) != 1)
        {
            error = "HELD report version/length mismatch";
            return false;
        }

        var flags = BinaryPrimitives.ReadUInt16LittleEndian(p[2..]);
        if (flags != 0x000f)
        {
            error = "HELD report flags do not prove trusted setup, no-init, release-disabled and HELD";
            return false;
        }

        var cgroupLength = BinaryPrimitives.ReadUInt16LittleEndian(p[192..]);
        if (cgroupLength > 256 || 194 + cgroupLength + 2 > p.Length)
        {
            error = "HELD cgroup identity length is invalid";
            return false;
        }

        var mountLengthOffset = 194 + cgroupLength;
        var mountLength = BinaryPrimitives.ReadUInt16LittleEndian(p[mountLengthOffset..]);
        var mountOffset = mountLengthOffset + 2;
        if (mountLength == 0 || mountOffset + mountLength != p.Length)
        {
            error = "HELD mount inventory length is invalid";
            return false;
        }

        var namespaces = new ulong[6];
        for (var i = 0; i < namespaces.Length; i++)
            namespaces[i] = BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(96 + i * 8));
        if (namespaces.Any(value => value == 0))
        {
            error = "HELD namespace identity is incomplete";
            return false;
        }

        var objects = new (ulong Device, ulong Inode)[3];
        for (var i = 0; i < objects.Length; i++)
            objects[i] = (
                BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(144 + i * 16)),
                BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(152 + i * 16)));

        report = new CoordinatorHeldReport(
            flags,
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(4)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(8)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(12)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(16)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(20)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(24)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(28)),
            BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(32)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(40)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(48)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(56)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(64)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(72)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(80)),
            BinaryPrimitives.ReadUInt64LittleEndian(frame.Payload.AsSpan(88)),
            namespaces, objects,
            Encoding.UTF8.GetString(p.Slice(194, cgroupLength)),
            Encoding.UTF8.GetString(p.Slice(mountOffset, mountLength)));
        return true;
    }

    internal static byte[] GuidToRfcBytes(Guid value) => ManifestCodec.GuidToRfc4122Bytes(value);

    private static Guid FromRfc4122(ReadOnlySpan<byte> bytes)
    {
        Span<byte> mixed = stackalloc byte[16];
        mixed[0] = bytes[3]; mixed[1] = bytes[2]; mixed[2] = bytes[1]; mixed[3] = bytes[0];
        mixed[4] = bytes[5]; mixed[5] = bytes[4]; mixed[6] = bytes[7]; mixed[7] = bytes[6];
        bytes[8..16].CopyTo(mixed[8..]);
        return new Guid(mixed);
    }
}
