using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Linux x64/arm64 only, matching the existing FdPassing ABI. No privileges acquired.
internal static class RecoveryInterop
{
    [StructLayout(LayoutKind.Sequential)] internal struct Peer { internal int Pid; internal uint Uid; internal uint Gid; }
    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    private static extern int GetSocketOption(int fd, int level, int option, out Peer peer, ref uint length);
    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)] private static extern int Fcntl(int fd, int command, int value);
    [DllImport("libc", EntryPoint = "getuid")] internal static extern uint GetUid();
    [DllImport("libc", EntryPoint = "geteuid")] internal static extern uint GetEffectiveUid();
    [DllImport("libc", EntryPoint = "getppid")] internal static extern int GetParentPid();
    [DllImport("libc", EntryPoint = "setsid")] private static extern int SetSid();
    [DllImport("libc", EntryPoint = "getsid")] internal static extern int GetSid(int pid);
    [DllImport("libc", EntryPoint = "getpgid")] internal static extern int GetProcessGroup(int pid);
    [DllImport("libc", EntryPoint = "prctl")] private static extern int Prctl(int option, out int value, ulong a, ulong b, ulong c);

    internal static bool Supported => OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64;
    internal static bool IsUnprivileged => GetUid() != 0 && GetUid() == GetEffectiveUid()
        && File.ReadLines("/proc/self/status").Any(line => line.StartsWith("CapEff:", StringComparison.Ordinal)
            && line[7..].Trim().All(c => c == '0'));

    internal static bool MatchesPeer(Peer actual, int pid, uint uid) => pid > 0 && actual.Pid == pid && actual.Uid == uid && uid != 0;
    internal static bool VerifyPeer(int fd, int expectedPid, uint uid)
    {
        uint size = 12;
        return GetSocketOption(fd, 1 /* SOL_SOCKET */, 17 /* SO_PEERCRED */, out var peer, ref size) == 0
            && size == 12 && MatchesPeer(peer, expectedPid, uid);
    }

    [DllImport("libc", EntryPoint = "getsockopt", SetLastError = true)]
    private static extern int GetIntSocketOption(int fd, int level, int option, out int value, ref uint length);
    internal static bool DistinctSockets(int first, int second)
    {
        uint a = 4, b = 4;
        return GetIntSocketOption(first, 1, 3 /* SO_TYPE */, out var x, ref a) == 0 && x == 5
            && GetIntSocketOption(second, 1, 3, out var y, ref b) == 0 && y == 5
            && PreparationInterop.StatFd(first, out var one) == 0 && PreparationInterop.StatFd(second, out var two) == 0
            && (one.Device != two.Device || one.Inode != two.Inode);
    }

    // The existing bounded record primitive reports syscall interruption explicitly.
    // Retry observation on the next loop tick; never treat EINTR/EAGAIN as peer loss,
    // and never retry a send or renew the independently retained stop deadline.
    internal static bool IsTransientReceiveError(string error) => error is
        "control record timed out" or "poll errno 4" or "recvmsg errno 4" or "recvmsg errno 11";

    internal static bool IsTransientPidfdError(string error) => error == "pidfd poll errno 4";

    internal static bool Protect(int fd, bool nonblocking = true)
    {
        var flags = Fcntl(fd, 3 /* F_GETFL */, 0);
        return fd > 2 && flags >= 0 && Fcntl(fd, 2 /* F_SETFD */, 1) == 0
            && (!nonblocking || Fcntl(fd, 4 /* F_SETFL */, flags | 0x800) == 0)
            && PreparationInterop.HasCloexec(fd);
    }

    internal static bool DetachLifetime() => Prctl(2 /* PR_GET_PDEATHSIG */, out var signal, 0, 0, 0) == 0
        && signal == 0 && SetSid() == Environment.ProcessId
        && GetSid(0) == Environment.ProcessId && GetProcessGroup(0) == Environment.ProcessId;
}
