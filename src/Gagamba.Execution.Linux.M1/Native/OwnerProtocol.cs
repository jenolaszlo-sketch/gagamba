// G2-03 owner/recovery transaction control. This is a private inherited
// SOCK_SEQPACKET protocol: frames are bounded, identity-bound and ordered.
// It carries descriptions and acknowledgements only; capabilities and
// release authority have no wire representation.
using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal enum OwnerProtocolMessageType : byte
{
    Start = 1,
    WriterReady = 2,
    StartAuthority = 3,
    DomainRecoveryAuthority = 4,
    DomainAuthorityRetained = 5,
    DomainRegistered = 6,
    DomainRegistrationRetained = 7,
    StagingAuthority = 8,
    StagingAuthorityRetained = 9,
    StagingAccounted = 10,
    StagingMayProceed = 11,
    StagedObjects = 12,
    ManifestAuthorization = 13,
    PreparedDescription = 14,
    PreparedAcknowledgement = 15,
    BarrierReached = 16,
    Continue = 17,
    Completed = 18,
    Aborted = 19,
}

internal sealed record OwnerProtocolMessage(
    OwnerProtocolMessageType Type,
    Guid ExecutionId,
    Guid LeaseGeneration,
    uint Sequence,
    byte[] Payload);

/// <summary>Versioned, bounded wire frame for the inherited owner channel.
/// Every frame binds to the exact execution and generation, and its
/// monotonically increasing sequence prevents duplicate/stale messages.</summary>
internal static class OwnerProtocol
{
    internal const uint Magic = 0x4D314F50; // M1OP
    internal const ushort CurrentVersion = 1;
    internal const int HeaderBytes = 4 + 2 + 1 + 1 + 16 + 16 + 4 + 4;
    internal const int MaxPayloadBytes = 60 * 1024;
    internal const int MaxFrameBytes = HeaderBytes + MaxPayloadBytes;

    internal static bool IsKnownBarrierPayload(ReadOnlySpan<byte> payload)
    {
        var value = Encoding.UTF8.GetString(payload);
        return value is "after-intent" or "after-domain-retained" or "after-registered"
            or "after-staging-accounted" or "mid-staging" or "after-copy"
            or "after-prepared-before-mint";
    }

    internal static bool HasValidControlPayloadShape(OwnerProtocolMessage message) =>
        message.Type switch
        {
            OwnerProtocolMessageType.WriterReady
                or OwnerProtocolMessageType.DomainAuthorityRetained
                or OwnerProtocolMessageType.DomainRegistered
                or OwnerProtocolMessageType.DomainRegistrationRetained
                or OwnerProtocolMessageType.StagingAuthorityRetained
                or OwnerProtocolMessageType.StagingMayProceed
                or OwnerProtocolMessageType.PreparedAcknowledgement
                or OwnerProtocolMessageType.Continue => message.Payload.Length == 0,
            OwnerProtocolMessageType.StagingAccounted => message.Payload.Length == 16,
            OwnerProtocolMessageType.BarrierReached => IsKnownBarrierPayload(message.Payload),
            OwnerProtocolMessageType.Aborted => message.Payload.Length <= 1024,
            _ => !IsTransferType(message.Type),
        };

    internal static bool IsTransferType(OwnerProtocolMessageType type) =>
        type is OwnerProtocolMessageType.StartAuthority
            or OwnerProtocolMessageType.DomainRecoveryAuthority
            or OwnerProtocolMessageType.StagingAuthority;

    internal static byte[] Encode(OwnerProtocolMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Payload);
        if (!Enum.IsDefined(message.Type))
        {
            throw new ArgumentOutOfRangeException(nameof(message), "Unknown owner protocol message type.");
        }

        if (message.ExecutionId == Guid.Empty || message.LeaseGeneration == Guid.Empty)
        {
            throw new ArgumentException("Owner protocol identities must be non-empty.", nameof(message));
        }

        if (message.Payload.Length > MaxPayloadBytes)
        {
            throw new ArgumentException("Owner protocol payload exceeds its bound.", nameof(message));
        }

        var frame = new byte[HeaderBytes + message.Payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(0), Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4), CurrentVersion);
        frame[6] = (byte)message.Type;
        frame[7] = 0;
        message.ExecutionId.TryWriteBytes(frame.AsSpan(8, 16));
        message.LeaseGeneration.TryWriteBytes(frame.AsSpan(24, 16));
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(40), message.Sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(44), checked((uint)message.Payload.Length));
        message.Payload.CopyTo(frame.AsSpan(HeaderBytes));
        return frame;
    }

    internal static bool TryDecode(
        ReadOnlySpan<byte> frame,
        Guid expectedExecutionId,
        Guid expectedLeaseGeneration,
        out OwnerProtocolMessage? message,
        out string error)
    {
        message = null;
        error = string.Empty;
        if (frame.Length < HeaderBytes || frame.Length > MaxFrameBytes)
        {
            error = "owner protocol frame length is outside the bound";
            return false;
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(frame) != Magic
            || BinaryPrimitives.ReadUInt16LittleEndian(frame[4..]) != CurrentVersion)
        {
            error = "owner protocol magic/version mismatch";
            return false;
        }

        var type = (OwnerProtocolMessageType)frame[6];
        if (!Enum.IsDefined(type) || frame[7] != 0)
        {
            error = "owner protocol type/reserved byte is invalid";
            return false;
        }

        var executionId = new Guid(frame.Slice(8, 16));
        var leaseGeneration = new Guid(frame.Slice(24, 16));
        if (executionId != expectedExecutionId || leaseGeneration != expectedLeaseGeneration)
        {
            error = "owner protocol transaction identity mismatch";
            return false;
        }

        var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(frame[44..]);
        if (payloadLength > MaxPayloadBytes || frame.Length != HeaderBytes + payloadLength)
        {
            error = "owner protocol payload length mismatch";
            return false;
        }

        message = new OwnerProtocolMessage(
            type,
            executionId,
            leaseGeneration,
            BinaryPrimitives.ReadUInt32LittleEndian(frame[40..]),
            frame[HeaderBytes..].ToArray());
        return true;
    }
}

/// <summary>Per-direction protocol sequencer. The caller supplies the
/// expected next message for its current transaction phase; a message is
/// accepted once, with no implicit retries or reconnect semantics.</summary>
internal sealed class OwnerProtocolSequencer
{
    private readonly Guid _executionId;
    private readonly Guid _leaseGeneration;
    private uint _nextSend;
    private uint _nextReceive;

    internal OwnerProtocolSequencer(Guid executionId, Guid leaseGeneration)
    {
        if (executionId == Guid.Empty || leaseGeneration == Guid.Empty)
        {
            throw new ArgumentException("Protocol identities must be non-empty.");
        }

        _executionId = executionId;
        _leaseGeneration = leaseGeneration;
    }

    internal byte[] Create(OwnerProtocolMessageType type, ReadOnlySpan<byte> payload = default)
    {
        var sequence = checked(_nextSend + 1);
        var frame = OwnerProtocol.Encode(new OwnerProtocolMessage(
            type, _executionId, _leaseGeneration, sequence, payload.ToArray()));
        _nextSend = sequence;
        return frame;
    }

    internal bool TryAccept(
        ReadOnlySpan<byte> frame,
        OwnerProtocolMessageType expectedType,
        out OwnerProtocolMessage? message,
        out string error)
    {
        if (!OwnerProtocol.TryDecode(frame, _executionId, _leaseGeneration, out message, out error))
        {
            return false;
        }

        return TryAcceptDecoded(message!, new[] { expectedType }, out message, out error);
    }

    internal bool TryAcceptDecoded(
        OwnerProtocolMessage candidate,
        IReadOnlyCollection<OwnerProtocolMessageType> allowedTypes,
        out OwnerProtocolMessage? message,
        out string error)
    {
        message = candidate;
        var expectedSequence = checked(_nextReceive + 1);
        if (!allowedTypes.Contains(candidate.Type))
        {
            error = $"unexpected owner protocol transition: type={candidate.Type}, sequence={candidate.Sequence}";
            message = null;
            return false;
        }

        if (candidate.Sequence != expectedSequence)
        {
            error = $"stale or duplicate owner protocol sequence {candidate.Sequence}; expected {expectedSequence}";
            message = null;
            return false;
        }

        _nextReceive = expectedSequence;
        error = string.Empty;
        return true;
    }
}

/// <summary>Owns one endpoint of the inherited private channel and combines
/// bounded record IO with the per-direction sequence/identity checks.</summary>
internal sealed class OwnerProtocolChannel : IDisposable
{
    internal const int TransitionTimeoutMilliseconds = 30_000;

    private readonly int _socket;
    private readonly bool _ownsSocket;
    private readonly Guid _sequencerExecutionId;
    private readonly Guid _sequencerLeaseGeneration;
    private readonly OwnerProtocolSequencer _sequencer;
    private bool _disposed;

    internal OwnerProtocolChannel(int socket, Guid executionId, Guid leaseGeneration, bool ownsSocket)
    {
        if (socket < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(socket));
        }

        _socket = socket;
        _ownsSocket = ownsSocket;
        _sequencerExecutionId = executionId;
        _sequencerLeaseGeneration = leaseGeneration;
        _sequencer = new OwnerProtocolSequencer(executionId, leaseGeneration);
    }

    internal int Socket => _socket;

    internal bool TrySend(OwnerProtocolMessageType type, ReadOnlySpan<byte> payload, out string error)
    {
        if (_disposed)
        {
            error = "owner protocol channel is closed";
            return false;
        }

        var frame = _sequencer.Create(type, payload);
        return FdPassing.TrySendMessage(_socket, frame, out error);
    }

    internal bool TrySendTransfer(
        OwnerProtocolMessageType type,
        ReadOnlySpan<byte> payload,
        int[] descriptors,
        out string error)
    {
        if (_disposed)
        {
            error = "owner protocol channel is closed";
            return false;
        }

        var frame = _sequencer.Create(type, payload);
        return FdPassing.TrySendTransfer(_socket, descriptors, frame, out error);
    }

    internal bool TryReceive(
        OwnerProtocolMessageType expectedType,
        out OwnerProtocolMessage? message,
        out string error)
    {
        return TryReceiveAllowed(out message, out error, expectedType);
    }

    internal bool TryReceiveAllowed(
        out OwnerProtocolMessage? message,
        out string error,
        params OwnerProtocolMessageType[] allowedTypes)
    {
        message = null;
        if (_disposed)
        {
            error = "owner protocol channel is closed";
            return false;
        }

        if (allowedTypes is null || allowedTypes.Length == 0)
        {
            error = "owner protocol receive state has no allowed message types";
            return false;
        }

        if (!FdPassing.TryReceiveRecord(_socket, TransitionTimeoutMilliseconds, out var record, out error))
        {
            return false;
        }

        using (record!)
        {
            if (!OwnerProtocol.TryDecode(record!.Payload, _sequencerExecutionId, _sequencerLeaseGeneration,
                    out var decoded, out error))
            {
                return false;
            }

            if (OwnerProtocol.IsTransferType(decoded!.Type) || record.DescriptorCount != 0)
            {
                error = "unexpected descriptor-bearing owner message in a control state";
                return false;
            }

            if (!OwnerProtocol.HasValidControlPayloadShape(decoded))
            {
                error = $"owner control payload shape is invalid for {decoded.Type}";
                return false;
            }

            return _sequencer.TryAcceptDecoded(decoded, allowedTypes, out message, out error);
        }
    }

    internal bool TryReceiveTransfer(
        OwnerProtocolMessageType expectedType,
        int expectedDescriptorCount,
        int expectedPayloadBytes,
        out OwnerProtocolMessage? message,
        out int[] descriptors,
        out string error)
    {
        message = null;
        descriptors = Array.Empty<int>();
        if (_disposed)
        {
            error = "owner protocol channel is closed";
            return false;
        }

        if (!OwnerProtocol.IsTransferType(expectedType)
            || expectedDescriptorCount <= 0
            || expectedDescriptorCount > 64
            || expectedPayloadBytes < 0
            || expectedPayloadBytes > OwnerProtocol.MaxPayloadBytes)
        {
            error = "expected owner transfer shape is invalid";
            return false;
        }

        if (!FdPassing.TryReceiveRecord(_socket, TransitionTimeoutMilliseconds, out var record, out error))
        {
            return false;
        }

        using (record!)
        {
            if (!OwnerProtocol.TryDecode(record!.Payload, _sequencerExecutionId, _sequencerLeaseGeneration,
                    out var decoded, out error))
            {
                return false;
            }

            if (decoded!.Type != expectedType || !OwnerProtocol.IsTransferType(decoded.Type))
            {
                error = $"unexpected owner transfer type {decoded.Type}; expected {expectedType}";
                return false;
            }

            if (decoded.Payload.Length != expectedPayloadBytes)
            {
                error = $"owner protocol transfer payload length {decoded.Payload.Length} != {expectedPayloadBytes}";
                return false;
            }

            if (record.DescriptorCount != expectedDescriptorCount)
            {
                error = $"descriptor count mismatch ({record.DescriptorCount} != {expectedDescriptorCount})";
                return false;
            }

            if (!_sequencer.TryAcceptDecoded(decoded, new[] { expectedType }, out message, out error))
            {
                return false;
            }

            descriptors = record.DetachDescriptors();
            return true;
        }
    }

    /// <summary>Receives the transfer expected in this state, or a valid
    /// descriptor-free BarrierReached/Aborted control record. The decoded
    /// type, rather than packet length, selects the path.</summary>
    internal bool TryReceiveTransferOrControl(
        OwnerProtocolMessageType expectedTransferType,
        int expectedDescriptorCount,
        int expectedPayloadBytes,
        out OwnerProtocolMessage? message,
        out int[] descriptors,
        out string error)
    {
        message = null;
        descriptors = Array.Empty<int>();
        if (_disposed)
        {
            error = "owner protocol channel is closed";
            return false;
        }

        if (!OwnerProtocol.IsTransferType(expectedTransferType)
            || expectedDescriptorCount <= 0
            || expectedDescriptorCount > 64
            || expectedPayloadBytes < 0
            || expectedPayloadBytes > OwnerProtocol.MaxPayloadBytes)
        {
            error = "expected owner transfer state is invalid";
            return false;
        }

        if (!FdPassing.TryReceiveRecord(_socket, TransitionTimeoutMilliseconds, out var record, out error))
        {
            return false;
        }

        using (record!)
        {
            if (!OwnerProtocol.TryDecode(record!.Payload, _sequencerExecutionId, _sequencerLeaseGeneration,
                    out var decoded, out error))
            {
                return false;
            }

            var allowed = new[]
            {
                expectedTransferType,
                OwnerProtocolMessageType.BarrierReached,
                OwnerProtocolMessageType.Aborted,
            };
            if (!allowed.Contains(decoded!.Type))
            {
                error = $"unexpected owner transition {decoded.Type}; expected {expectedTransferType}, BarrierReached, or Aborted";
                return false;
            }

            if (decoded.Type == expectedTransferType)
            {
                if (!OwnerProtocol.IsTransferType(decoded.Type)
                    || decoded.Payload.Length != expectedPayloadBytes
                    || record.DescriptorCount != expectedDescriptorCount)
                {
                    error = "owner transfer payload or descriptor count did not match the expected shape";
                    return false;
                }

                if (!_sequencer.TryAcceptDecoded(decoded, allowed, out message, out error))
                {
                    return false;
                }

                descriptors = record.DetachDescriptors();
                return true;
            }

            if (record.DescriptorCount != 0)
            {
                error = "unexpected descriptors on owner control record";
                return false;
            }

            if (!OwnerProtocol.HasValidControlPayloadShape(decoded))
            {
                error = $"owner control payload shape is invalid for {decoded.Type}";
                return false;
            }

            return _sequencer.TryAcceptDecoded(decoded, allowed, out message, out error);
        }
    }

    /// <summary>Keeps the owner process and its local store/capability alive
    /// for the recovery-scope lifetime after Mint. No commands are accepted
    /// in this tranche; EOF from the retained scope endpoint ends the owner.</summary>
    internal bool WaitForScopeLifetime(out string error)
    {
        error = string.Empty;
        while (!_disposed)
        {
            if (!FdPassing.TryReceiveMessage(_socket, TransitionTimeoutMilliseconds, out _, out error))
            {
                if (error == "control record timed out")
                {
                    continue;
                }

                if (error == "peer closed the owner protocol channel")
                {
                    error = string.Empty;
                    return true;
                }

                return false;
            }

            error = "unexpected control message after preparation Mint";
            return false;
        }

        error = "owner protocol channel closed before scope lifetime ended";
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsSocket)
        {
            PreparationInterop.CloseFd(_socket);
        }

        GC.SuppressFinalize(this);
    }
}
