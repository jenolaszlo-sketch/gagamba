// WP03 SCM_RIGHTS descriptor transfer over one inherited Unix socketpair.
// The plan-selected mechanism for crossing the primary domain handles
// from the scope to the owner ("one inherited Unix socketpair solely for
// SCM_RIGHTS transfer, with a fixed role/count allowlist and
// CLOEXEC-on-receive"). The socket is SOCK_SEQPACKET so the kernel
// preserves the logical message boundary: one sendmsg is exactly one
// recvmsg record. A SOCK_STREAM endpoint would silently split an overlong
// send, letting a valid 51-byte prefix pass as the whole message; a
// message-preserving endpoint instead reports MSG_TRUNC for an overlong
// record. Never filesystem-bound; never a service. Receive uses
// MSG_CMSG_CLOEXEC, inspects msg_flags, validates one complete bounded
// record, keeps each received descriptor under explicit ownership until
// protocol dispatch, and closes every descriptor on any rejection path.
// Queued records are consumed only when the state machine asks for the next
// transition.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal static class FdPassing
{
    private const int AF_UNIX = 1;
    private const int SOCK_SEQPACKET = 5;
    private const int SOL_SOCKET = 1;
    private const int SCM_RIGHTS = 1;

    private const int F_SETFD = 2;
    private const int FD_CLOEXEC = 1;

    private const int MSG_CMSG_CLOEXEC = 0x40000000;
    private const int MSG_CTRUNC = 0x8;
    private const int MSG_TRUNC = 0x20;
    private const int MSG_NOSIGNAL = 0x4000;
    private const int POLLIN = 0x001;


    [DllImport("libc", EntryPoint = "socketpair", SetLastError = true)]
    private static extern int SocketPair(int domain, int type, int protocol, int[] sv);

    [DllImport("libc", EntryPoint = "sendmsg", SetLastError = true)]
    private static extern long SendMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)]
    private static extern long RecvMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(int fd, int cmd, int arg);

    [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static extern int Poll(IntPtr fds, uint count, int timeoutMilliseconds);

    /// <summary>Creates a message-preserving (SOCK_SEQPACKET) socketpair
    /// without CLOEXEC on the child end so the owner inherits it at a known
    /// fd number; the parent end stays CLOEXEC. Fails closed if a
    /// message-preserving socket cannot be created; it never falls back to
    /// a stream endpoint.</summary>
    internal static bool TryCreateInheritableSocketPair(out int parentEnd, out int childEnd, out string error)
    {
        parentEnd = -1;
        childEnd = -1;
        error = string.Empty;
        var sv = new int[2];
        try
        {
            if (SocketPair(AF_UNIX, SOCK_SEQPACKET, 0, sv) != 0)
            {
                error = $"socketpair errno {Marshal.GetLastWin32Error()}";
                return false;
            }
        }
        catch (DllNotFoundException)
        {
            error = "Unsupported: socketpair unavailable";
            return false;
        }

        Fcntl(sv[1], F_SETFD, 0);
        Fcntl(sv[0], F_SETFD, FD_CLOEXEC);
        parentEnd = sv[0];
        childEnd = sv[1];
        return true;
    }

    /// <summary>Sends descriptors with the exact metadata payload.</summary>
    internal static bool TrySendTransfer(int sock, int[] fds, byte[] payload, out string error)
    {
        error = string.Empty;
        var cmsgLen = 16 + (4 * fds.Length);
        var ctrlSize = 16 + Align4(4 * fds.Length);
        var ctrl = Marshal.AllocHGlobal(ctrlSize);
        var iov = Marshal.AllocHGlobal(16);
        var msg = Marshal.AllocHGlobal(56);
        var pinned = GCHandle.Alloc(payload, GCHandleType.Pinned);
        try
        {
            Marshal.WriteInt64(ctrl, 0, cmsgLen);
            Marshal.WriteInt32(ctrl, 8, SOL_SOCKET);
            Marshal.WriteInt32(ctrl, 12, SCM_RIGHTS);
            for (var i = 0; i < fds.Length; i++)
            {
                Marshal.WriteInt32(ctrl, 16 + (4 * i), fds[i]);
            }

            Marshal.WriteIntPtr(iov, 0, pinned.AddrOfPinnedObject());
            Marshal.WriteIntPtr(iov, 8, (IntPtr)payload.Length);

            Zero(msg, 56);
            Marshal.WriteIntPtr(msg, 16, iov);
            Marshal.WriteIntPtr(msg, 24, (IntPtr)1);
            Marshal.WriteIntPtr(msg, 32, ctrl);
            Marshal.WriteIntPtr(msg, 40, (IntPtr)cmsgLen);

            var rc = SendMsg(sock, msg, MSG_NOSIGNAL);
            if (rc < 0)
            {
                error = $"sendmsg errno {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (rc != payload.Length)
            {
                error = $"partial send {rc}/{payload.Length}";
                return false;
            }

            return true;
        }
        catch (DllNotFoundException)
        {
            error = "Unsupported: sendmsg unavailable";
            return false;
        }
        finally
        {
            pinned.Free();
            Marshal.FreeHGlobal(ctrl);
            Marshal.FreeHGlobal(iov);
            Marshal.FreeHGlobal(msg);
        }
    }

    /// <summary>Sends one bounded SOCK_SEQPACKET control record without
    /// ancillary descriptors. This is reserved for protocol acknowledgments
    /// and descriptions; descriptor authority uses the strict transfer
    /// method above.</summary>
    internal static bool TrySendMessage(int sock, byte[] payload, out string error)
    {
        error = string.Empty;
        if (payload is null || payload.Length == 0 || payload.Length > OwnerProtocol.MaxFrameBytes)
        {
            error = "control record length is outside the owner protocol bound";
            return false;
        }

        var iov = Marshal.AllocHGlobal(16);
        var msg = Marshal.AllocHGlobal(56);
        var pinned = GCHandle.Alloc(payload, GCHandleType.Pinned);
        try
        {
            Marshal.WriteIntPtr(iov, 0, pinned.AddrOfPinnedObject());
            Marshal.WriteIntPtr(iov, 8, (IntPtr)payload.Length);
            Zero(msg, 56);
            Marshal.WriteIntPtr(msg, 16, iov);
            Marshal.WriteIntPtr(msg, 24, (IntPtr)1);

            var rc = SendMsg(sock, msg, MSG_NOSIGNAL);
            if (rc < 0)
            {
                error = $"sendmsg errno {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (rc != payload.Length)
            {
                error = $"partial control record {rc}/{payload.Length}";
                return false;
            }

            return true;
        }
        catch (DllNotFoundException)
        {
            error = "Unsupported: sendmsg unavailable";
            return false;
        }
        finally
        {
            pinned.Free();
            Marshal.FreeHGlobal(iov);
            Marshal.FreeHGlobal(msg);
        }
    }

    /// <summary>Waits for and consumes exactly one bounded SOCK_SEQPACKET record.
    /// Any delivered SCM_RIGHTS descriptors are retained by the returned value
    /// until the protocol dispatcher accepts or disposes the record.</summary>
    internal static bool TryReceiveRecord(
        int sock,
        int timeoutMilliseconds,
        out ReceivedFdRecord? record,
        out string error)
    {
        record = null;
        error = string.Empty;
        if (timeoutMilliseconds < 0)
        {
            error = "negative control-record timeout";
            return false;
        }

        var pollFd = Marshal.AllocHGlobal(8);
        try
        {
            Zero(pollFd, 8);
            Marshal.WriteInt32(pollFd, 0, sock);
            Marshal.WriteInt16(pollFd, 4, POLLIN);
            var ready = Poll(pollFd, 1, timeoutMilliseconds);
            if (ready < 0)
            {
                error = $"poll errno {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (ready == 0)
            {
                error = "control record timed out";
                return false;
            }

            var events = Marshal.ReadInt16(pollFd, 6);
            if ((events & POLLIN) == 0)
            {
                error = $"owner protocol socket was not readable (revents 0x{events:X4})";
                return false;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pollFd);
        }

        var buffer = new byte[OwnerProtocol.MaxFrameBytes];
        const int maxControlFds = 64;
        var ctrlSize = 16 + Align4(4 * maxControlFds);
        var ctrl = Marshal.AllocHGlobal(ctrlSize);
        var iov = Marshal.AllocHGlobal(16);
        var msg = Marshal.AllocHGlobal(56);
        var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        var received = new List<int>();
        try
        {
            Zero(ctrl, ctrlSize);
            Marshal.WriteIntPtr(iov, 0, pinned.AddrOfPinnedObject());
            Marshal.WriteIntPtr(iov, 8, (IntPtr)buffer.Length);
            Zero(msg, 56);
            Marshal.WriteIntPtr(msg, 16, iov);
            Marshal.WriteIntPtr(msg, 24, (IntPtr)1);
            Marshal.WriteIntPtr(msg, 32, ctrl);
            Marshal.WriteIntPtr(msg, 40, (IntPtr)ctrlSize);

            var rc = RecvMsg(sock, msg, MSG_CMSG_CLOEXEC);
            if (rc < 0)
            {
                error = $"recvmsg errno {Marshal.GetLastWin32Error()}";
                return false;
            }

            var controlLength = Marshal.ReadInt64(msg, 40);
            var controlBytes = controlLength <= 0
                ? 0
                : (int)Math.Min(controlLength, ctrlSize);
            var ancillaryValid = TryCaptureAncillaryDescriptors(
                ctrl, controlBytes, out var ancillaryError, received);
            var flags = Marshal.ReadInt32(msg, 48);

            if (rc == 0)
            {
                CloseReceived(received);
                error = controlBytes > 0
                    ? "zero-length record is not an owner protocol frame"
                    : "peer closed the owner protocol channel";
                return false;
            }

            if ((flags & MSG_CTRUNC) != 0)
            {
                CloseReceived(received);
                error = "MSG_CTRUNC: control data truncated";
                return false;
            }

            if ((flags & MSG_TRUNC) != 0 || rc > OwnerProtocol.MaxFrameBytes)
            {
                CloseReceived(received);
                error = "overlong owner protocol record";
                return false;
            }

            if (controlLength < 0 || controlLength > ctrlSize || !ancillaryValid)
            {
                CloseReceived(received);
                error = controlLength < 0 || controlLength > ctrlSize
                    ? "invalid ancillary control length"
                    : ancillaryError;
                return false;
            }

            foreach (var fd in received)
            {
                var descriptorFlags = Fcntl(fd, 1 /*F_GETFD*/, 0);
                if (descriptorFlags < 0 || (descriptorFlags & FD_CLOEXEC) == 0)
                {
                    CloseReceived(received);
                    error = $"received descriptor {fd} lacks FD_CLOEXEC";
                    return false;
                }
            }

            record = new ReceivedFdRecord(
                buffer.AsSpan(0, checked((int)rc)).ToArray(), received.ToArray());
            received.Clear();
            return true;
        }
        catch (DllNotFoundException)
        {
            CloseReceived(received);
            error = "Unsupported: recvmsg unavailable";
            return false;
        }
        catch (Exception ex)
        {
            CloseReceived(received);
            error = $"owner protocol record receive failed: {ex.GetType().Name}";
            return false;
        }
        finally
        {
            pinned.Free();
            Marshal.FreeHGlobal(ctrl);
            Marshal.FreeHGlobal(iov);
            Marshal.FreeHGlobal(msg);
        }
    }

    /// <summary>Receives one bounded record for a descriptor-free protocol state.
    /// A subsequent queued record remains queued for the next state transition.</summary>
    internal static bool TryReceiveMessage(
        int sock,
        int timeoutMilliseconds,
        out byte[] payload,
        out string error)
    {
        payload = Array.Empty<byte>();
        if (!TryReceiveRecord(sock, timeoutMilliseconds, out var received, out error))
        {
            return false;
        }

        using (received!)
        {
            if (received!.DescriptorCount != 0)
            {
                error = "unexpected ancillary data on a control record";
                return false;
            }

            payload = received.Payload;
            error = string.Empty;
            return true;
        }
    }

    /// <summary>Receives a raw exact-payload SCM_RIGHTS record. The protocol
    /// channel uses the same bounded record path and adds type/sequence checks.</summary>
    internal static bool TryReceiveTransfer(
        int sock,
        int expectedDescriptorCount,
        int expectedPayloadLength,
        out int[] fds,
        out byte[] payload,
        out string error)
    {
        fds = Array.Empty<int>();
        payload = Array.Empty<byte>();
        error = string.Empty;
        if (expectedDescriptorCount <= 0
            || expectedDescriptorCount > 64
            || expectedPayloadLength < 0
            || expectedPayloadLength > OwnerProtocol.MaxFrameBytes)
        {
            error = "expected transfer shape is outside the receive bound";
            return false;
        }

        if (!TryReceiveRecord(
                sock, OwnerProtocolChannel.TransitionTimeoutMilliseconds, out var received, out error))
        {
            return false;
        }

        using (received!)
        {
            if (received!.Payload.Length != expectedPayloadLength)
            {
                error = received.Payload.Length > expectedPayloadLength
                    ? $"overlong message: record exceeds the exact {expectedPayloadLength}-byte payload"
                    : $"payload length {received.Payload.Length} != {expectedPayloadLength}";
                return false;
            }

            if (received.DescriptorCount != expectedDescriptorCount)
            {
                error = $"descriptor count mismatch ({received.DescriptorCount} != {expectedDescriptorCount})";
                return false;
            }

            fds = received.DetachDescriptors();
            payload = received.Payload;
            error = string.Empty;
            return true;
        }
    }

    private static bool TryCaptureAncillaryDescriptors(
        IntPtr control,
        int controlBytes,
        out string error,
        List<int> received)
    {
        error = string.Empty;
        if (controlBytes == 0)
        {
            return true;
        }

        var offset = 0;
        var messageCount = 0;
        var valid = true;
        while (offset < controlBytes)
        {
            var remaining = controlBytes - offset;
            if (remaining < 16)
            {
                valid = false;
                error = "truncated ancillary header";
                break;
            }

            var length = Marshal.ReadInt64(control, offset);
            var level = Marshal.ReadInt32(control, offset + 8);
            var type = Marshal.ReadInt32(control, offset + 12);
            if (length < 16)
            {
                valid = false;
                error = "invalid ancillary header length";
                break;
            }

            var availableDataBytes = Math.Max(0, remaining - 16);
            var declaredDataBytes = length - 16;
            var readableDataBytes = (int)Math.Min(declaredDataBytes, availableDataBytes);
            if (level == SOL_SOCKET && type == SCM_RIGHTS)
            {
                messageCount++;
                var descriptorBytes = readableDataBytes - (readableDataBytes % 4);
                for (var i = 0; i < descriptorBytes / 4; i++)
                {
                    var fd = Marshal.ReadInt32(control, offset + 16 + (4 * i));
                    if (fd >= 0)
                    {
                        received.Add(fd);
                    }
                    else
                    {
                        valid = false;
                        error = "invalid received descriptor";
                    }
                }

                if (declaredDataBytes != readableDataBytes
                    || declaredDataBytes == 0
                    || declaredDataBytes % 4 != 0)
                {
                    valid = false;
                    error = "invalid SCM_RIGHTS control layout";
                }
            }
            else
            {
                valid = false;
                error = "unsupported ancillary control message";
            }

            if (length > remaining)
            {
                valid = false;
                if (string.IsNullOrEmpty(error))
                {
                    error = "ancillary control message exceeds its buffer";
                }

                break;
            }

            if (length == remaining)
            {
                offset = controlBytes;
                break;
            }

            var alignedLength = checked((int)((length + 7) & ~7L));
            if (alignedLength <= 0 || alignedLength > remaining)
            {
                valid = false;
                error = "invalid ancillary control alignment";
                break;
            }

            offset += alignedLength;
        }

        if (messageCount != 1)
        {
            valid = false;
            error = messageCount == 0
                ? "unexpected ancillary data"
                : "multiple ancillary control messages";
        }

        return valid;
    }

    private static void CloseReceived(List<int> received)
    {
        foreach (var fd in received)
        {
            CloseFdChecked(fd);
        }

        received.Clear();
    }

    private static void CloseFdChecked(int fd)
    {
        if (fd >= 0)
        {
            PreparationInterop.CloseFd(fd);
        }
    }
    private static int Align4(int n) => (n + 3) & ~3;

    private static void Zero(IntPtr p, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Marshal.WriteByte(p, i, 0);
        }
    }
}

/// <summary>Owns descriptors delivered with one bounded record. Descriptors
/// remain owned here until detached after complete protocol validation.</summary>
internal sealed class ReceivedFdRecord : IDisposable
{
    private int[]? _descriptors;

    internal ReceivedFdRecord(byte[] payload, int[] descriptors)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
    }

    internal byte[] Payload { get; }

    internal int DescriptorCount => _descriptors?.Length ?? 0;

    internal int[] DetachDescriptors()
    {
        var descriptors = _descriptors
            ?? throw new InvalidOperationException("record descriptors were already detached or disposed");
        _descriptors = null;
        return descriptors;
    }

    public void Dispose()
    {
        var descriptors = _descriptors;
        if (descriptors is null)
        {
            return;
        }

        _descriptors = null;
        foreach (var fd in descriptors)
        {
            if (fd >= 0)
            {
                PreparationInterop.CloseFd(fd);
            }
        }
    }
}
