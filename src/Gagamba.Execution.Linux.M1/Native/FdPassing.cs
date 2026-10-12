// WP03 SCM_RIGHTS descriptor transfer over one inherited Unix socketpair.
// This is the plan-selected mechanism for crossing the primary domain
// handles from the scope to the owner ("one inherited Unix socketpair
// solely for SCM_RIGHTS transfer, with a fixed role/count allowlist and
// CLOEXEC-on-receive"). Never filesystem-bound; never a service.
using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux.M1.Native;

internal static class FdPassing
{
    private const int AF_UNIX = 1;
    private const int SOCK_STREAM = 1;
    private const int SOL_SOCKET = 1;
    private const int SCM_RIGHTS = 1;
    private const int SOCK_CLOEXEC = 0x80000;

    [DllImport("libc", EntryPoint = "socketpair", SetLastError = true)]
    private static extern int SocketPair(int domain, int type, int protocol, int[] sv);

    [DllImport("libc", EntryPoint = "sendmsg", SetLastError = true)]
    private static extern long SendMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)]
    private static extern long RecvMsg(int sock, IntPtr msg, int flags);

    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    private static extern int Fcntl(int fd, int cmd, int arg);

    private const int F_SETFD = 2;
    private const int FD_CLOEXEC = 1;

    /// <summary>Creates a stream socketpair without CLOEXEC so the child end
    /// survives exec (the owner inherits it at a known fd number).</summary>
    internal static bool TryCreateInheritableSocketPair(out int parentEnd, out int childEnd, out string error)
    {
        parentEnd = -1;
        childEnd = -1;
        error = string.Empty;
        var sv = new int[2];
        try
        {
            if (SocketPair(AF_UNIX, SOCK_STREAM, 0, sv) != 0)
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

        // Child end must be inherited across exec; parent end stays CLOEXEC.
        Fcntl(sv[1], F_SETFD, 0);
        Fcntl(sv[0], F_SETFD, FD_CLOEXEC);
        parentEnd = sv[0];
        childEnd = sv[1];
        return true;
    }

    /// <summary>Sends descriptors with a one-byte payload.</summary>
    internal static bool TrySendFds(int sock, int[] fds, out string error)
    {
        error = string.Empty;
        var payload = new byte[] { 0x01 };
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

    /// <summary>Receives up to maxFds descriptors.</summary>
    internal static bool TryReceiveFds(int sock, int maxFds, out int[] fds, out string error)
    {
        fds = Array.Empty<int>();
        error = string.Empty;
        var payload = new byte[1];
        var ctrlSize = 16 + Align4(4 * maxFds);
        var ctrl = Marshal.AllocHGlobal(ctrlSize);
        var iov = Marshal.AllocHGlobal(16);
        var msg = Marshal.AllocHGlobal(56);
        var pinned = GCHandle.Alloc(payload, GCHandleType.Pinned);
        try
        {
            Zero(ctrl, ctrlSize);
            Marshal.WriteIntPtr(iov, 0, pinned.AddrOfPinnedObject());
            Marshal.WriteIntPtr(iov, 8, (IntPtr)payload.Length);

            Zero(msg, 56);
            Marshal.WriteIntPtr(msg, 16, iov);
            Marshal.WriteIntPtr(msg, 24, (IntPtr)1);
            Marshal.WriteIntPtr(msg, 32, ctrl);
            Marshal.WriteIntPtr(msg, 40, (IntPtr)ctrlSize);

            var rc = RecvMsg(sock, msg, 0);
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

            var level = Marshal.ReadInt32(ctrl, 8);
            var type = Marshal.ReadInt32(ctrl, 12);
            if (level != SOL_SOCKET || type != SCM_RIGHTS)
            {
                error = "no SCM_RIGHTS control message";
                return false;
            }

            var len = Marshal.ReadInt64(ctrl, 0);
            var count = (int)((len - 16) / 4);
            if (count <= 0 || count > maxFds)
            {
                error = $"unexpected descriptor count {count}";
                return false;
            }

            var received = new int[count];
            for (var i = 0; i < count; i++)
            {
                received[i] = Marshal.ReadInt32(ctrl, 16 + (4 * i));
            }

            fds = received;
            return true;
        }
        catch (DllNotFoundException)
        {
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

    private static int Align4(int n) => (n + 3) & ~3;

    private static void Zero(IntPtr p, int count)
    {
        for (var i = 0; i < count; i++)
        {
            Marshal.WriteByte(p, i, 0);
        }
    }
}
