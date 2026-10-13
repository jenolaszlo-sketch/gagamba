// WP03 retained owner-pidfd contract (G2-10). The dedicated recovery scope
// obtains a pidfd for the exact per-execution owner direct child and
// retains it independently. The pidfd proves owner lifecycle only: it is
// never domain/target kill authority, never enters the SCM_RIGHTS domain
// transfer, never enters a future coordinator FD map, and is never
// persisted as durable authority. Missing or invalid pidfd support refuses
// the native M1 path; there is no PID-polling, /proc or journal-PID
// fallback.
using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux.M1.Native;

/// <summary>Opens a retained pidfd for an exact process. Injectable so a
/// deterministic test can prove the missing-pidfd refusal; production uses
/// the real libc syscall.</summary>
internal interface IPidFdProvider
{
    bool TryOpen(int pid, out int fd, out string error);
}

internal sealed class RealPidFdProvider : IPidFdProvider
{
    internal static readonly RealPidFdProvider Instance = new();

    private RealPidFdProvider()
    {
    }

    public bool TryOpen(int pid, out int fd, out string error) => PidFd.TryOpen(pid, out fd, out error);
}

internal static class PidFd
{
    // pidfd_open(2) is syscall 434 on x86_64 and arm64.
    private const long SysPidfdOpen = 434;
    private const short PollIn = 0x001;
    private const short PollErr = 0x008;
    private const short PollHup = 0x010;

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long Syscall(long number, long arg1, long arg2, long arg3);

    [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static extern int Poll([In, Out] PollFd[] fds, ulong nfds, int timeout);

    /// <summary>Opens a pidfd for the exact PID. The kernel returns it
    /// O_CLOEXEC; the caller must still verify and treat any non-CLOEXEC
    /// result as refusal.</summary>
    internal static bool TryOpen(int pid, out int fd, out string error)
    {
        fd = -1;
        error = string.Empty;
        if (!OperatingSystem.IsLinux())
        {
            error = "Unsupported: not Linux";
            return false;
        }

        if (pid <= 0)
        {
            error = "invalid owner pid";
            return false;
        }

        try
        {
            var result = Syscall(SysPidfdOpen, pid, 0, 0);
            if (result < 0)
            {
                error = $"pidfd_open errno {Marshal.GetLastPInvokeError()}";
                return false;
            }

            fd = (int)result;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            fd = -1;
            error = "Unsupported: pidfd_open unavailable";
            return false;
        }
    }

    /// <summary>Polls the retained pidfd. A readable pidfd means the owner
    /// process has terminated (it may still be an unreaped zombie). A
    /// bounded timeout returning without readiness is a truthful "not
    /// exited", never an assumed exit.</summary>
    internal static bool TryExited(int fd, int timeoutMilliseconds, out bool exited, out string error)
    {
        exited = false;
        error = string.Empty;
        if (!OperatingSystem.IsLinux())
        {
            error = "Unsupported: not Linux";
            return false;
        }

        if (fd < 0)
        {
            error = "invalid pidfd";
            return false;
        }

        var fds = new[] { new PollFd { Fd = fd, Events = PollIn, Revents = 0 } };
        var rc = Poll(fds, 1, timeoutMilliseconds < 0 ? -1 : timeoutMilliseconds);
        if (rc < 0)
        {
            error = $"pidfd poll errno {Marshal.GetLastPInvokeError()}";
            return false;
        }

        if (rc == 0)
        {
            return true;
        }

        exited = (fds[0].Revents & (PollIn | PollErr | PollHup)) != 0;
        return true;
    }
}
