// INT-01 Alternative B. Bootstrap supplies lifetime references, never a release bearer.
// All actors must inhabit the same PID and user namespaces. No PID-only fallback.
using System.Globalization;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed record DirectProcessIdentity(int Pid, uint Uid, uint Gid, string PidNamespace, string UserNamespace)
{
    internal static DirectProcessIdentity Current() => new(Environment.ProcessId, RecoveryInterop.GetUid(),
        DirectUnix.GetGid(), Namespace("pid"), Namespace("user"));
    private static string Namespace(string name) => new FileInfo("/proc/self/ns/" + name).LinkTarget
        ?? throw new IOException("Namespace identity unavailable");
    internal void Validate()
    {
        var current = Current();
        if (Pid <= 0 || Uid == 0 || string.IsNullOrEmpty(PidNamespace) || string.IsNullOrEmpty(UserNamespace)
            || PidNamespace != current.PidNamespace || UserNamespace != current.UserNamespace)
            throw new InvalidDataException("Direct peer namespace/identity refused");
    }
}

internal static class DirectPeerIdentity
{
    internal static bool LiveReference(int fd, DirectProcessIdentity expected)
    {
        if (!PreparationInterop.HasCloexec(fd) || !PidFd.TryExited(fd, 0, out var exited, out _) || exited)
            return false;
        // fdinfo is read for an ALREADY retained kernel reference, never to reopen a numeric PID.
        var pid = File.ReadLines("/proc/self/fdinfo/" + fd.ToString(CultureInfo.InvariantCulture))
            .SingleOrDefault(line => line.StartsWith("Pid:", StringComparison.Ordinal));
        return pid is not null && int.TryParse(pid.AsSpan(4).Trim(), out var value) && value == expected.Pid
            && PidFd.TryExited(fd, 0, out exited, out _) && !exited;
    }

    internal static SafeFileHandle Verify(int socket, DirectProcessIdentity expected, int retainedPidfd)
    {
        expected.Validate();
        if (!LiveReference(retainedPidfd, expected)) throw new IOException("Expected peer no longer live");
        uint length = 12;
        if (DirectUnix.PeerCredentials(socket, 1, 17, out var peer, ref length) != 0 || length != 12
            || peer.Pid != expected.Pid || peer.Uid != expected.Uid || peer.Gid != expected.Gid)
            throw new IOException("Exact direct peer credentials refused");
        length = 4;
        // Linux SO_PEERPIDFD (77), not pidfd_open(peer.Pid). Unsupported is terminal.
        if (DirectUnix.IntOption(socket, 1, 77, out var fd, ref length) != 0)
            throw new IOException("SO_PEERPIDFD unavailable");
        var handle = new SafeFileHandle((IntPtr)fd, true);
        try
        {
            if (length != 4 || !LiveReference(fd, expected) || !LiveReference(retainedPidfd, expected))
                throw new IOException("Direct peer lifetime refused");
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }

    internal static bool FeatureAvailable()
    {
        if (!RecoveryInterop.Supported || !RecoveryInterop.IsUnprivileged) return false;
        var pair = new int[2];
        if (DirectUnix.SocketPair(1, 1 | DirectUnix.Cloexec, 0, pair) != 0) return false;
        try
        {
            if (!PidFd.TryOpen(Environment.ProcessId, out var pidfd, out _)) return false;
            using var retained = new SafeFileHandle((IntPtr)pidfd, true);
            using var peer = Verify(pair[0], DirectProcessIdentity.Current(), pidfd);
            return !peer.IsInvalid;
        }
        catch (IOException) { return false; }
        finally { foreach (var fd in pair) PreparationInterop.CloseFd(fd); }
    }
}

internal static class DirectUnix
{
    internal const int Cloexec = 0x80000;
    internal const int Nonblock = 0x800;
    [StructLayout(LayoutKind.Sequential)] internal struct PollFd { internal int Fd; internal short Events; internal short Revents; }
    [StructLayout(LayoutKind.Sequential)] internal struct Iovec { internal IntPtr Base; internal UIntPtr Length; }
    [StructLayout(LayoutKind.Sequential)] internal struct Message
    {
        internal IntPtr Name; internal uint NameLength; internal IntPtr Vectors; internal UIntPtr VectorCount;
        internal IntPtr Control; internal UIntPtr ControlLength; internal int Flags;
    }
    [DllImport("libc", EntryPoint = "getgid")] internal static extern uint GetGid();
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    internal static extern int PeerCredentials(int fd, int level, int option, out RecoveryInterop.Peer value, ref uint length);
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    internal static extern int IntOption(int fd, int level, int option, out int value, ref uint length);
    [DllImport("libc", EntryPoint = "socket", SetLastError = true)] internal static extern int Socket(int domain, int type, int protocol);
    [DllImport("libc", EntryPoint = "socketpair", SetLastError = true)] internal static extern int SocketPair(int domain, int type, int protocol, int[] pair);
    [DllImport("libc", EntryPoint = "bind", SetLastError = true)] internal static extern int Bind(int fd, byte[] address, uint length);
    [DllImport("libc", EntryPoint = "connect", SetLastError = true)] internal static extern int Connect(int fd, byte[] address, uint length);
    [DllImport("libc", EntryPoint = "listen", SetLastError = true)] internal static extern int Listen(int fd, int backlog);
    [DllImport("libc", EntryPoint = "accept4", SetLastError = true)] internal static extern int Accept(int fd, IntPtr address, IntPtr length, int flags);
    [DllImport("libc", EntryPoint = "poll", SetLastError = true)] internal static extern int Poll(ref PollFd fd, UIntPtr count, int timeout);
    [DllImport("libc", EntryPoint = "send", SetLastError = true)] internal static extern long Send(int fd, IntPtr data, UIntPtr length, int flags);
    [DllImport("libc", EntryPoint = "recvmsg", SetLastError = true)] internal static extern long Receive(int fd, ref Message message, int flags);
    [DllImport("libc", EntryPoint = "shutdown", SetLastError = true)] internal static extern int Shutdown(int fd, int how);
    [DllImport("libc", EntryPoint = "fchmodat", SetLastError = true)] internal static extern int ChmodAt(int fd, string name, uint mode, int flags);

    internal static byte[] Address(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path);
        if (!Path.IsPathFullyQualified(path) || bytes.Length is < 1 or > 107 || bytes.Contains((byte)0))
            throw new InvalidDataException("Unix rendezvous path refused");
        var address = new byte[3 + bytes.Length]; address[0] = 1; bytes.CopyTo(address, 2); return address;
    }
}
