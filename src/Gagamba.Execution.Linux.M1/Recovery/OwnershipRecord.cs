// WP03 bounded ownership snapshot: the durable recovery record. It holds
// identities and allocation inventory only — never release credentials,
// unbounded logs, raw input or arbitrary destructive paths. Serialization
// is a bounded little-endian codec with an explicit checksum so torn or
// malformed records are detected. A record is evidence, never authority:
// live retained handles remain authoritative (Section 10/14).
using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Restricted;

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
    StagingDirectory = 2,
}

internal enum PreparationStage : byte
{
    None = 0,
    AllocationIntent = 1,
    AllocationRegistered = 2,
    Prepared = 3,
}

/// <summary>Durable release phase tag. A null <see cref="ReleaseRecord"/> is
/// the absent phase; the enum is explicit on the wire.</summary>
internal enum ReleaseTag : byte
{
    Absent = 0,
    Intent = 1,
    Committed = 2,
}

/// <summary>Owner-observed release transport result. Delivered means only the
/// owner's full 128-byte pipe write returned successfully; it does not prove
/// coordinator consumption, exec or target execution.</summary>
internal enum DeliveryResult : byte
{
    NotAttempted = 0,
    Delivered = 1,
    WouldBlock = 2,
    BrokenPipe = 3,
    Partial = 4,
    Unknown = 5,
}

/// <summary>Trusted exec classification derived by the coordinator's sole
/// target-reaper/executable-object observations and reported to the owner.
/// A null <see cref="ExecObservation"/> is NotAttempted.</summary>
internal enum ExecTag : byte
{
    NotAttempted = 0,
    FailedBeforeExec = 1,
    Confirmed = 2,
    Unknown = 3,
}

/// <summary>Evidence basis for a confirmed exec. All bases are produced by
/// the trusted coordinator; the owner validates the frame and persists the
/// reported fact. The owner is not the target reaper.</summary>
internal enum ExecBasis : byte
{
    None = 0,
    CoordinatorFrame = 1,
    ExecutableObjectIdentity = 2,
    ReviewedNormalExitRule = 3,
}

/// <summary>Presence tag for a durable root observation. A null
/// <see cref="RootObservation"/> is NotObserved.</summary>
internal enum RootTag : byte
{
    NotObserved = 0,
    Observed = 1,
}

/// <summary>Trusted target root result kind.</summary>
internal enum RootKind : byte
{
    Unknown = 0,
    NotStarted = 1,
    Exited = 2,
    Signalled = 3,
}

/// <summary>Durable release record. Intent carries the exact preparation
/// identity through the snapshot's RecordedPreparation and the accepted
/// absolute deadlines. Committed carries the intent fields unchanged plus
/// the post-gate observation. It is evidence, never release authority.</summary>
internal sealed record ReleaseRecord(
    ReleaseTag Tag,
    long IntentAtNanoseconds,
    long SetupDeadlineNanoseconds,
    long OverallDeadlineNanoseconds,
    long? CommittedAtNanoseconds = null,
    DeliveryResult DeliveryResult = DeliveryResult.NotAttempted);

/// <summary>First accepted authoritative stop. The five-second confirmation
/// allowance is encoded exactly in StopDeadline; later transitions cannot
/// erase or rewrite this record.</summary>
internal sealed record StopRecord(
    long StopAcceptedAtNanoseconds,
    StopCauseKind Cause,
    long StopDeadlineNanoseconds);

/// <summary>Durable trusted exec observation. Owner-persisted evidence from a
/// validated coordinator report; recovery reads it and never invents it.</summary>
internal sealed record ExecObservation(
    ExecTag Tag,
    int Errno = 0,
    ExecBasis Basis = ExecBasis.None);

/// <summary>Durable trusted root observation. Raw wait data is diagnostic
/// unless the kind is a known target Exited/Signalled result with confirmed
/// exec; recovery never upgrades Unknown from transport or cleanup facts.</summary>
internal sealed record RootObservation(
    RootKind Kind,
    int ExitCode = 0,
    int Signal = 0,
    bool CoreDumped = false,
    bool RawWaitPresent = false,
    int RawWaitStatus = 0);

/// <summary>One accounted allocation the recovery scope can clean by
/// retained parent + identity, never by arbitrary path. Device/Inode are
/// the captured creation identity used for identity-checked removal;
/// Confirmed marks durable per-object accounting (unconfirmed entries are
/// planned-but-unverified names).</summary>
internal sealed record AllocationEntry(
    AllocationKind Kind,
    string Name,
    ulong Length,
    byte[] Digest,
    ulong Device,
    ulong Inode,
    bool Confirmed);

/// <summary>Preparation identity carried in the durable record. Bounded,
/// non-secret corroboration only; it can never resurrect a capability.</summary>
internal sealed record RecordedPreparation(
    Guid ExecutionId,
    Guid IssuerId,
    Guid GenerationId,
    Guid PreparationId,
    byte[] ManifestDigest);

/// <summary>Immutable ownership snapshot. Versioned; sequence-monotonic;
/// checksummed over the canonical payload. CompletedCleanup is a bitmask
/// of independently confirmed cleanup components for idempotent,
/// monotonic recovery progress.</summary>
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
    ulong StagingParentDevice,
    ulong StagingParentInode,
    IReadOnlyList<AllocationEntry> Allocations,
    RecordedPreparation? Preparation,
    byte CompletedCleanup = 0,
    ReleaseRecord? Release = null,
    StopRecord? Stop = null,
    ExecObservation? Exec = null,
    RootObservation? Root = null);

/// <summary>Bounded codec for ownership snapshots. Refuses overlarge or
/// malformed records; the checksum covers every byte before it.</summary>
internal static class OwnershipSnapshotCodec
{
    internal const ushort LegacyVersion = 2;
    internal const ushort CurrentVersion = 3;
    internal const int MaxSnapshotBytes = 65536;
    internal const long StopConfirmationAllowanceNanoseconds = 5_000_000_000;
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

        OwnershipSnapshotValidator.Validate(snapshot);
        var wireVersion = WireVersion(snapshot);
        using var body = new MemoryStream();
        WriteU16(body, wireVersion);
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
        body.WriteByte(snapshot.CompletedCleanup);
        WriteU64(body, snapshot.StagingParentDevice);
        WriteU64(body, snapshot.StagingParentInode);
        WriteU32(body, checked((uint)snapshot.Allocations.Count));
        foreach (var allocation in snapshot.Allocations)
        {
            body.WriteByte((byte)allocation.Kind);
            WriteString(body, allocation.Name, MaxNameBytes, nameof(allocation.Name));
            WriteU64(body, allocation.Length);
            WriteBytes(body, allocation.Digest, nameof(allocation.Digest));
            WriteU64(body, allocation.Device);
            WriteU64(body, allocation.Inode);
            body.WriteByte(allocation.Confirmed ? (byte)1 : (byte)0);
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

        if (wireVersion == CurrentVersion)
        {
            WriteRelease(body, snapshot.Release);
            WriteStop(body, snapshot.Stop);
            WriteExec(body, snapshot.Exec);
            WriteRoot(body, snapshot.Root);
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

    /// <summary>Lazy schema selection: pre-release snapshots remain v2; the
    /// first WP05 fact upgrades the complete snapshot to v3.</summary>
    internal static ushort WireVersion(OwnershipSnapshot snapshot) =>
        snapshot.Release is null && snapshot.Stop is null && snapshot.Exec is null && snapshot.Root is null
            ? LegacyVersion
            : CurrentVersion;

    private static void WriteRelease(Stream body, ReleaseRecord? release)
    {
        if (release is null)
        {
            body.WriteByte((byte)ReleaseTag.Absent);
            return;
        }

        body.WriteByte((byte)release.Tag);
        WriteI64(body, release.IntentAtNanoseconds);
        WriteI64(body, release.SetupDeadlineNanoseconds);
        WriteI64(body, release.OverallDeadlineNanoseconds);
        if (release.Tag == ReleaseTag.Committed)
        {
            WriteI64(body, release.CommittedAtNanoseconds!.Value);
            body.WriteByte((byte)release.DeliveryResult);
        }
    }

    private static void WriteStop(Stream body, StopRecord? stop)
    {
        if (stop is null)
        {
            body.WriteByte(0);
            return;
        }

        body.WriteByte(1);
        WriteI64(body, stop.StopAcceptedAtNanoseconds);
        body.WriteByte((byte)stop.Cause);
        WriteI64(body, stop.StopDeadlineNanoseconds);
    }

    private static void WriteExec(Stream body, ExecObservation? exec)
    {
        if (exec is null)
        {
            body.WriteByte((byte)ExecTag.NotAttempted);
            return;
        }

        body.WriteByte((byte)exec.Tag);
        if (exec.Tag == ExecTag.FailedBeforeExec)
        {
            WriteU32(body, checked((uint)exec.Errno));
        }
        else if (exec.Tag == ExecTag.Confirmed)
        {
            body.WriteByte((byte)exec.Basis);
        }
    }

    private static void WriteRoot(Stream body, RootObservation? root)
    {
        if (root is null)
        {
            body.WriteByte((byte)RootTag.NotObserved);
            return;
        }

        body.WriteByte((byte)RootTag.Observed);
        body.WriteByte((byte)root.Kind);
        WriteU32(body, checked((uint)root.ExitCode));
        WriteU32(body, checked((uint)root.Signal));
        body.WriteByte(root.CoreDumped ? (byte)1 : (byte)0);
        body.WriteByte(root.RawWaitPresent ? (byte)1 : (byte)0);
        if (root.RawWaitPresent)
        {
            WriteU32(body, checked((uint)root.RawWaitStatus));
        }
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
            if (version is not (LegacyVersion or CurrentVersion))
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
            var completed = ReadByte(payload, ref offset);
            var stagingParentDevice = ReadU64(payload, ref offset);
            var stagingParentInode = ReadU64(payload, ref offset);
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
                var device = ReadU64(payload, ref offset);
                var inode = ReadU64(payload, ref offset);
                var confirmed = ReadByte(payload, ref offset);
                if (confirmed != 0 && confirmed != 1)
                {
                    error = "invalid allocation confirmation tag";
                    return false;
                }

                allocations.Add(new AllocationEntry(kind, name, length, digest, device, inode, confirmed == 1));
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

            ReleaseRecord? release = null;
            StopRecord? stop = null;
            ExecObservation? execObservation = null;
            RootObservation? root = null;
            if (version == CurrentVersion)
            {
                var releaseTag = (ReleaseTag)ReadByte(payload, ref offset);
                if (releaseTag == ReleaseTag.Intent)
                {
                    release = new ReleaseRecord(
                        ReleaseTag.Intent,
                        ReadI64(payload, ref offset),
                        ReadI64(payload, ref offset),
                        ReadI64(payload, ref offset));
                }
                else if (releaseTag == ReleaseTag.Committed)
                {
                    var intentAt = ReadI64(payload, ref offset);
                    var setupDeadline = ReadI64(payload, ref offset);
                    var overallDeadline = ReadI64(payload, ref offset);
                    var committedAt = ReadI64(payload, ref offset);
                    var delivery = (DeliveryResult)ReadByte(payload, ref offset);
                    release = new ReleaseRecord(
                        ReleaseTag.Committed, intentAt, setupDeadline, overallDeadline,
                        committedAt, delivery);
                }
                else if (releaseTag != ReleaseTag.Absent)
                {
                    error = "invalid release tag";
                    return false;
                }

                var stopTag = ReadByte(payload, ref offset);
                if (stopTag == 1)
                {
                    stop = new StopRecord(
                        ReadI64(payload, ref offset),
                        (StopCauseKind)ReadByte(payload, ref offset),
                        ReadI64(payload, ref offset));
                }
                else if (stopTag != 0)
                {
                    error = "invalid stop tag";
                    return false;
                }

                var execTag = (ExecTag)ReadByte(payload, ref offset);
                if (execTag == ExecTag.FailedBeforeExec)
                {
                    execObservation = new ExecObservation(execTag, checked((int)ReadU32(payload, ref offset)));
                }
                else if (execTag == ExecTag.Confirmed)
                {
                    execObservation = new ExecObservation(execTag, Basis: (ExecBasis)ReadByte(payload, ref offset));
                }
                else if (execTag == ExecTag.Unknown)
                {
                    execObservation = new ExecObservation(execTag);
                }
                else if (execTag != ExecTag.NotAttempted)
                {
                    error = "invalid exec tag";
                    return false;
                }

                var rootTag = (RootTag)ReadByte(payload, ref offset);
                if (rootTag == RootTag.Observed)
                {
                    var kind = (RootKind)ReadByte(payload, ref offset);
                    var exitCode = checked((int)ReadU32(payload, ref offset));
                    var signal = checked((int)ReadU32(payload, ref offset));
                    var core = ReadByte(payload, ref offset);
                    var rawPresent = ReadByte(payload, ref offset);
                    if (core > 1 || rawPresent > 1)
                    {
                        error = "invalid root observation flags";
                        return false;
                    }

                    var rawStatus = rawPresent == 1 ? checked((int)ReadU32(payload, ref offset)) : 0;
                    root = new RootObservation(kind, exitCode, signal, core == 1, rawPresent == 1, rawStatus);
                }
                else if (rootTag != RootTag.NotObserved)
                {
                    error = "invalid root tag";
                    return false;
                }
            }

            if (offset != payload.Length)
            {
                error = "trailing bytes in snapshot";
                return false;
            }

            var candidate = new OwnershipSnapshot(
                version, sequence, executionId, bootId, mountId, mountFs,
                parentDev, parentIno, domainName, domainDev, domainIno,
                lease, writer, cleanup, stage,
                StagingParentDevice: stagingParentDevice,
                StagingParentInode: stagingParentInode,
                Allocations: allocations, Preparation: preparation,
                CompletedCleanup: completed,
                Release: release, Stop: stop, Exec: execObservation, Root: root);
            if (!OwnershipSnapshotValidator.TryValidate(candidate, out error))
            {
                return false;
            }

            snapshot = candidate;
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
