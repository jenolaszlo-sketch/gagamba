// WP03 bounded ownership snapshot: the durable recovery record. It holds
// identities and allocation inventory only — never release credentials,
// unbounded logs, raw input or arbitrary destructive paths. Serialization
// is a bounded little-endian codec with an explicit checksum so torn or
// malformed records are detected. A record is evidence, never authority:
// live retained handles remain authoritative (Section 10/14).
using System.Buffers.Binary;
using System.Text;

namespace Gagamba.Execution.Linux.M1.Recovery;

internal enum OwnershipWriterKind : byte
{
    RecoveryScope = 0,
    Owner = 1,
}

internal enum OwnershipCleanupState : byte
{
    NotStarted = 0,
    InProgress = 1,
    Confirmed = 2,
    Failed = 3,
    Unknown = 4,
}

internal enum AllocationKind : byte
{
    Staging = 0,
    Domain = 1,
}

internal enum PreparationStage : byte
{
    None = 0,
    AllocationIntent = 1,
    AllocationRegistered = 2,
    Prepared = 3,
}

/// <summary>One accounted allocation the recovery scope can clean by
/// retained parent + identity, never by arbitrary path.</summary>
internal sealed record AllocationEntry(
    AllocationKind Kind,
    string Name,
    ulong Length,
    byte[] Digest);

/// <summary>Preparation identity carried in the durable record. Bounded,
/// non-secret corroboration only; it can never resurrect a capability.</summary>
internal sealed record RecordedPreparation(
    Guid ExecutionId,
    Guid IssuerId,
    Guid GenerationId,
    Guid PreparationId,
    byte[] ManifestDigest);

/// <summary>Immutable ownership snapshot. Versioned; sequence-monotonic;
/// checksummed over the canonical payload.</summary>
internal sealed record OwnershipSnapshot(
    ushort Version,
    long Sequence,
    Guid ExecutionId,
    string BootId,
    ulong CgroupMountId,
    string CgroupMountFsType,
    ulong ParentDevice,
    ulong ParentInode,
    string DomainName,
    ulong DomainDevice,
    ulong DomainInode,
    Guid LeaseGeneration,
    OwnershipWriterKind Writer,
    OwnershipCleanupState Cleanup,
    PreparationStage Stage,
    IReadOnlyList<AllocationEntry> Allocations,
    RecordedPreparation? Preparation);

/// <summary>Bounded codec for ownership snapshots. Refuses overlarge or
/// malformed records; the checksum covers every byte before it.</summary>
internal static class OwnershipSnapshotCodec
{
    internal const ushort CurrentVersion = 1;
    internal const int MaxSnapshotBytes = 65536;
    internal const int MaxAllocations = 128;
    internal const int MaxNameBytes = 256;
    internal const int MaxBootIdBytes = 64;
    internal const int MaxMountFsBytes = 32;

    internal static byte[] Encode(OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Allocations.Count > MaxAllocations)
        {
            throw new ArgumentException("Too many allocations for one snapshot.", nameof(snapshot));
        }

        using var body = new MemoryStream();
        WriteU16(body, snapshot.Version);
        WriteI64(body, snapshot.Sequence);
        WriteGuid(body, snapshot.ExecutionId);
        WriteString(body, snapshot.BootId, MaxBootIdBytes, nameof(snapshot.BootId));
        WriteU64(body, snapshot.CgroupMountId);
        WriteString(body, snapshot.CgroupMountFsType, MaxMountFsBytes, nameof(snapshot.CgroupMountFsType));
        WriteU64(body, snapshot.ParentDevice);
        WriteU64(body, snapshot.ParentInode);
        WriteString(body, snapshot.DomainName, MaxNameBytes, nameof(snapshot.DomainName));
        WriteU64(body, snapshot.DomainDevice);
        WriteU64(body, snapshot.DomainInode);
        WriteGuid(body, snapshot.LeaseGeneration);
        body.WriteByte((byte)snapshot.Writer);
        body.WriteByte((byte)snapshot.Cleanup);
        body.WriteByte((byte)snapshot.Stage);
        WriteU32(body, checked((uint)snapshot.Allocations.Count));
        foreach (var allocation in snapshot.Allocations)
        {
            body.WriteByte((byte)allocation.Kind);
            WriteString(body, allocation.Name, MaxNameBytes, nameof(allocation.Name));
            WriteU64(body, allocation.Length);
            WriteBytes(body, allocation.Digest, nameof(allocation.Digest));
        }

        if (snapshot.Preparation is null)
        {
            body.WriteByte(0);
        }
        else
        {
            body.WriteByte(1);
            WriteGuid(body, snapshot.Preparation.ExecutionId);
            WriteGuid(body, snapshot.Preparation.IssuerId);
            WriteGuid(body, snapshot.Preparation.GenerationId);
            WriteGuid(body, snapshot.Preparation.PreparationId);
            WriteBytes(body, snapshot.Preparation.ManifestDigest, nameof(snapshot.Preparation));
        }

        var payload = body.ToArray();
        if (payload.Length + 32 > MaxSnapshotBytes)
        {
            throw new ArgumentException("Ownership snapshot exceeds the 64 KiB bound.", nameof(snapshot));
        }

        var checksum = System.Security.Cryptography.SHA256.HashData(payload);
        var encoded = new byte[payload.Length + checksum.Length];
        Buffer.BlockCopy(payload, 0, encoded, 0, payload.Length);
        Buffer.BlockCopy(checksum, 0, encoded, payload.Length, checksum.Length);
        return encoded;
    }

    internal static bool TryDecode(ReadOnlySpan<byte> encoded, out OwnershipSnapshot? snapshot, out string error)
    {
        snapshot = null;
        error = string.Empty;
        if (encoded.Length < 32 || encoded.Length > MaxSnapshotBytes)
        {
            error = "snapshot length out of bounds";
            return false;
        }

        var payloadLength = encoded.Length - 32;
        var payload = encoded[..payloadLength].ToArray();
        var expected = encoded[payloadLength..].ToArray();
        var actual = System.Security.Cryptography.SHA256.HashData(payload);
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            error = "checksum mismatch (torn or malformed record)";
            return false;
        }

        try
        {
            var offset = 0;
            var version = ReadU16(payload, ref offset);
            if (version != CurrentVersion)
            {
                error = $"unsupported snapshot version {version}";
                return false;
            }

            var sequence = ReadI64(payload, ref offset);
            var executionId = ReadGuid(payload, ref offset);
            var bootId = ReadString(payload, ref offset, MaxBootIdBytes);
            var mountId = ReadU64(payload, ref offset);
            var mountFs = ReadString(payload, ref offset, MaxMountFsBytes);
            var parentDev = ReadU64(payload, ref offset);
            var parentIno = ReadU64(payload, ref offset);
            var domainName = ReadString(payload, ref offset, MaxNameBytes);
            var domainDev = ReadU64(payload, ref offset);
            var domainIno = ReadU64(payload, ref offset);
            var lease = ReadGuid(payload, ref offset);
            var writer = (OwnershipWriterKind)ReadByte(payload, ref offset);
            var cleanup = (OwnershipCleanupState)ReadByte(payload, ref offset);
            var stage = (PreparationStage)ReadByte(payload, ref offset);
            var count = ReadU32(payload, ref offset);
            if (count > MaxAllocations)
            {
                error = "allocation count out of bounds";
                return false;
            }

            var allocations = new List<AllocationEntry>((int)count);
            for (var i = 0; i < count; i++)
            {
                var kind = (AllocationKind)ReadByte(payload, ref offset);
                var name = ReadString(payload, ref offset, MaxNameBytes);
                var length = ReadU64(payload, ref offset);
                var digest = ReadBytes(payload, ref offset);
                allocations.Add(new AllocationEntry(kind, name, length, digest));
            }

            RecordedPreparation? preparation = null;
            var hasPreparation = ReadByte(payload, ref offset);
            if (hasPreparation == 1)
            {
                var exec = ReadGuid(payload, ref offset);
                var issuer = ReadGuid(payload, ref offset);
                var generation = ReadGuid(payload, ref offset);
                var prep = ReadGuid(payload, ref offset);
                var digest = ReadBytes(payload, ref offset);
                preparation = new RecordedPreparation(exec, issuer, generation, prep, digest);
            }
            else if (hasPreparation != 0)
            {
                error = "invalid preparation presence tag";
                return false;
            }

            if (offset != payload.Length)
            {
                error = "trailing bytes in snapshot";
                return false;
            }

            snapshot = new OwnershipSnapshot(
                version, sequence, executionId, bootId, mountId, mountFs,
                parentDev, parentIno, domainName, domainDev, domainIno,
                lease, writer, cleanup, stage, allocations, preparation);
            return true;
        }
        catch (Exception ex)
        {
            error = $"malformed snapshot: {ex.GetType().Name}";
            return false;
        }
    }

    private static void WriteU16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteU32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteU64(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteI64(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteGuid(Stream stream, Guid value) =>
        stream.Write(value.ToByteArray());

    private static void WriteBytes(Stream stream, byte[] value, string parameter)
    {
        if (value.Length > 4096)
        {
            throw new ArgumentException("Field exceeds the bounded byte length.", parameter);
        }

        WriteU32(stream, checked((uint)value.Length));
        stream.Write(value);
    }

    private static void WriteString(Stream stream, string value, int maxBytes, string parameter)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? throw new ArgumentNullException(parameter));
        if (bytes.Length > maxBytes)
        {
            throw new ArgumentException($"Field exceeds {maxBytes} bytes.", parameter);
        }

        WriteU32(stream, checked((uint)bytes.Length));
        stream.Write(bytes);
    }

    private static ushort ReadU16(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(buffer[offset..]);
        offset += 2;
        return value;
    }

    private static uint ReadU32(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(buffer[offset..]);
        offset += 4;
        return value;
    }

    private static ulong ReadU64(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(buffer[offset..]);
        offset += 8;
        return value;
    }

    private static long ReadI64(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(buffer[offset..]);
        offset += 8;
        return value;
    }

    private static byte ReadByte(ReadOnlySpan<byte> buffer, ref int offset) =>
        buffer[offset++];

    private static Guid ReadGuid(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var bytes = buffer.Slice(offset, 16).ToArray();
        offset += 16;
        return new Guid(bytes);
    }

    private static byte[] ReadBytes(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var length = ReadU32(buffer, ref offset);
        if (length > 4096)
        {
            throw new InvalidOperationException("bounded byte field exceeds the maximum length");
        }

        var value = buffer.Slice(offset, (int)length).ToArray();
        offset += (int)length;
        return value;
    }

    private static string ReadString(ReadOnlySpan<byte> buffer, ref int offset, int maxBytes)
    {
        var length = ReadU32(buffer, ref offset);
        if (length > (uint)maxBytes)
        {
            throw new InvalidOperationException("bounded string field exceeds the maximum length");
        }

        var value = Encoding.UTF8.GetString(buffer.Slice(offset, (int)length));
        offset += (int)length;
        return value;
    }
}
