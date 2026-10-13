// WP03 SCM_RIGHTS descriptor transfer over one inherited Unix socketpair.
// The plan-selected mechanism for crossing the primary domain handles
// from the scope to the owner ("one inherited Unix socketpair solely for
// SCM_RIGHTS transfer, with a fixed role/count allowlist and
// CLOEXEC-on-receive"). The socket is SOCK_SEQPACKET so the kernel
// preserves the logical message boundary: one sendmsg is exactly one
// recvmsg record. A SOCK_STREAM endpoint would silently split an overlong
// send, letting a valid 51-byte prefix pass as the whole message; a
// message-preserving endpoint instead reports MSG_TRUNC for an overlong
// record and refuses any second queued record. Never filesystem-bound;
// never a service. Receive uses MSG_CMSG_CLOEXEC, inspects msg_flags,
// validates the exact control layout and payload, requires exactly one
// record, verifies every received descriptor has FD_CLOEXEC, and closes
// every received descriptor on any rejection path.
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

    // FIONREAD reports the byte length of the next queued record without
    // consuming it or installing any SCM_RIGHTS descriptors (unlike
    // MSG_PEEK, which duplicates them). 0 means no record is queued.
    private const ulong FIONREAD = 0x541B;

    [DllImport("libc", EntryPoint = "socketpair", SetLastError = true)]
    private static extern int SocketPair(int domain, int type, int protocol, int[] sv);

    [DllImport("libc", EntryPoint = "sendmsg", SetLastError = true)]
    private static extern long SendMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)]
    private static extern long RecvMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(int fd, int cmd, int arg);

    [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static extern int Ioctl(int fd, ulong request, out int value);

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

            var rc = SendMsg(sock, msg, 0);
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

    /// <summary>Receives descriptors with MSG_CMSG_CLOEXEC, validates the
    /// exact payload length and control layout, and verifies FD_CLOEXEC on
    /// every received descriptor. Any rejection closes all received FDs.</summary>
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

        var payloadBuffer = new byte[expectedPayloadLength];
        var ctrlSize = 16 + Align4(4 * expectedDescriptorCount);
        var ctrl = Marshal.AllocHGlobal(ctrlSize);
        var iov = Marshal.AllocHGlobal(16);
        var msg = Marshal.AllocHGlobal(56);
        var pinned = GCHandle.Alloc(payloadBuffer, GCHandleType.Pinned);
        var received = new List<int>();
        try
        {
            Zero(ctrl, ctrlSize);
            Marshal.WriteIntPtr(iov, 0, pinned.AddrOfPinnedObject());
            Marshal.WriteIntPtr(iov, 8, (IntPtr)payloadBuffer.Length);

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

            if (rc == 0)
            {
                error = "peer closed before descriptors were received";
                return false;
            }

            var msgFlags = Marshal.ReadInt32(msg, 48);
            if ((msgFlags & MSG_CTRUNC) != 0)
            {
                // Control data truncated: parse whatever arrived so we can
                // close it, then refuse.
                CloseControlFds(ctrl, received);
                error = "MSG_CTRUNC: control data truncated";
                return false;
            }

            if ((msgFlags & MSG_TRUNC) != 0 || rc != expectedPayloadLength)
            {
                CloseControlFds(ctrl, received);
                error = (msgFlags & MSG_TRUNC) != 0
                    ? $"overlong message: record exceeds the exact {expectedPayloadLength}-byte payload"
                    : $"payload length {rc} != {expectedPayloadLength}";
                return false;
            }

            var cmsgLen = Marshal.ReadInt64(ctrl, 0);
            var level = Marshal.ReadInt32(ctrl, 8);
            var type = Marshal.ReadInt32(ctrl, 12);
            var controllen = Marshal.ReadInt64(msg, 40);
            if (level != SOL_SOCKET || type != SCM_RIGHTS)
            {
                error = "no SCM_RIGHTS control message";
                return false;
            }

            var count = (int)((cmsgLen - 16) / 4);
            // Exactly one control message, no padding ambiguity, no extra
            // ancillary data.
            if (count != expectedDescriptorCount || cmsgLen != 16 + (4 * count) || controllen != 16 + Align4(4 * count))
            {
                CloseControlFds(ctrl, received);
                error = $"descriptor count/layout mismatch (count={count}, controllen={controllen})";
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var fd = Marshal.ReadInt32(ctrl, 16 + (4 * i));
                received.Add(fd);
            }

            // Verify the kernel actually produced CLOEXEC descriptors.
            foreach (var fd in received)
            {
                var flags = Fcntl(fd, 1 /*F_GETFD*/, 0);
                if (flags < 0 || (flags & FD_CLOEXEC) == 0)
                {
                    foreach (var open in received)
                    {
                        CloseFdChecked(open);
                    }

                    error = $"received descriptor {fd} lacks FD_CLOEXEC";
                    return false;
                }
            }

            // A message-preserving endpoint delivers exactly one record per
            // recvmsg. A second queued record means the peer sent more than
            // one logical frame; refuse rather than accept the first as the
            // whole transfer. FIONREAD reports the next record's length
            // without installing its descriptors.
            var ioctlResult = Ioctl(sock, FIONREAD, out var pendingRecord);
            if (ioctlResult != 0 || pendingRecord > 0)
            {
                foreach (var open in received)
                {
                    CloseFdChecked(open);
                }

                error = ioctlResult != 0
                    ? "socket state unavailable for frame-boundary check"
                    : $"extra queued frame ({pendingRecord} bytes)";
                return false;
            }

            fds = received.ToArray();
            payload = payloadBuffer;
            return true;
        }
        catch (DllNotFoundException)
        {
            foreach (var fd in received)
            {
                CloseFdChecked(fd);
            }

            error = "Unsupported: recvmsg unavailable";
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

    private static void CloseControlFds(IntPtr ctrl, List<int> received)
    {
        // Best-effort: read the count from the (possibly truncated) control
        // buffer and close what the kernel delivered.
        try
        {
            var cmsgLen = Marshal.ReadInt64(ctrl, 0);
            var count = (int)((cmsgLen - 16) / 4);
            for (var i = 0; i < count && i < 64; i++)
            {
                var fd = Marshal.ReadInt32(ctrl, 16 + (4 * i));
                if (fd >= 0)
                {
                    CloseFdChecked(fd);
                    received.Add(fd);
                }
            }
        }
        catch
        {
        }
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
