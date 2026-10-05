// GQ-1 kernel-backed tree ownership: one Job Object per activity, kill on
// close, no breakaway flags, suspend-assign-resume (no escape window).
// The handle is held for the execution domain's lifetime; closing it (or
// dying with it open) terminates the tree. No quotas, completion ports,
// UI restrictions, or telemetry: ownership and reliable tree death only.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gw1bLaunch.Tests")]

namespace Gagamba.Spikes.Gw1bLaunch;

internal static class JobOwnership
{
    public const uint StillActive = 259;

    public static string CmdExe => Path.Combine(Environment.SystemDirectory, "cmd.exe");

    /// <summary>Sleeper command: ~30s, quiet, no stdin pitfalls.</summary>
    public static string SleepArgs => "/d /c ping -n 31 127.0.0.1 > NUL";

    /// <summary>Create a Job Object with KILL_ON_JOB_CLOSE and nothing else.</summary>
    public static (IntPtr Handle, string Detail) CreateKillOnCloseJob()
    {
        IntPtr h = Native.CreateJobObjectW(IntPtr.Zero, null);
        if (h == IntPtr.Zero)
            return (IntPtr.Zero, $"CreateJobObject err=0x{Marshal.GetLastWin32Error():X}");
        var info = new Native.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new Native.JobObjectBasicLimitInformation
            {
                LimitFlags = Native.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
            },
        };
        if (!Native.SetInformationJobObject(h, Native.JobObjectExtendedLimitInformationClass,
                ref info, (uint)Marshal.SizeOf<Native.JobObjectExtendedLimitInformation>()))
        {
            int err = Marshal.GetLastWin32Error();
            try { Native.CloseHandle(h); } catch { }
            return (IntPtr.Zero, $"SetInformationJobObject err=0x{err:X}");
        }
        return (h, "job created (KILL_ON_JOB_CLOSE, no breakaway, no quotas)");
    }

    /// <summary>
    /// Launch suspended, assign to the job, then resume: no window exists in
    /// which the root could spawn descendants outside Gagamba ownership.
    /// Returns the process handle (caller owns it) and PID.
    /// </summary>
    public static (bool Ok, string Detail, IntPtr Process, uint Pid) LaunchIntoJob(
        IntPtr hJob, string exe, string args, string cwd)
    {
        var commandLine = new StringBuilder(32768);
        commandLine.Append('"').Append(exe).Append("\" ").Append(args);
        var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>() };
        bool created;
        uint lastError;
        ProcessInformation pi;
        try
        {
            created = Native.CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero,
                false, Native.CREATE_SUSPENDED, IntPtr.Zero, cwd, ref si, out pi);
            lastError = (uint)Marshal.GetLastWin32Error();
        }
        catch (Exception ex)
        {
            return (false, $"invocation fault: {ex.GetType().Name}: {ex.Message}", IntPtr.Zero, 0);
        }
        if (!created)
            return (false, $"CreateProcess err=0x{lastError:X} (no target ran)", IntPtr.Zero, 0);
        uint pid = Native.GetProcessId(pi.hProcess);
        if (!Native.AssignProcessToJobObject(hJob, pi.hProcess))
        {
            int err = Marshal.GetLastWin32Error();
            try { Native.TerminateProcess(pi.hProcess, 99); } catch { }
            CloseBoth(pi.hProcess, pi.hThread);
            // PID retained for verification: the target was terminated, and
            // callers (and tests) can prove nothing escaped.
            return (false, $"assign rejected err=0x{err:X} (no unmanaged execution: target terminated)", IntPtr.Zero, pid);
        }
        if (Native.ResumeThread(pi.hThread) == uint.MaxValue)
        {
            int err = Marshal.GetLastWin32Error();
            try { Native.TerminateProcess(pi.hProcess, 99); } catch { }
            CloseBoth(pi.hProcess, pi.hThread);
            return (false, $"resume fault err=0x{err:X}", IntPtr.Zero, pid);
        }
        try { Native.CloseHandle(pi.hThread); } catch { }
        return (true, $"launched suspended+assigned+resumed pid={pid}", pi.hProcess, pid);
    }

    public static bool IsInJob(IntPtr hProcess)
    {
        try { return Native.IsProcessInJob(hProcess, IntPtr.Zero, out bool r) && r; }
        catch { return false; }
    }

    public static bool IsPidInJob(uint pid)
    {
        IntPtr h = OpenQuery(pid);
        if (h == IntPtr.Zero) return false;
        try { return IsInJob(h); }
        finally { try { Native.CloseHandle(h); } catch { } }
    }

    public static bool IsDead(uint pid)
    {
        IntPtr h = OpenQuery(pid);
        if (h == IntPtr.Zero) return true; // gone (or never existed)
        try { return Native.GetExitCodeProcess(h, out uint rc) && rc != StillActive; }
        finally { try { Native.CloseHandle(h); } catch { } }
    }

    public static bool TryExitCode(uint pid, out uint code)
    {
        code = 0;
        IntPtr h = OpenQuery(pid);
        if (h == IntPtr.Zero) return false;
        try { return Native.GetExitCodeProcess(h, out code); }
        finally { try { Native.CloseHandle(h); } catch { } }
    }

    /// <summary>Poll until every PID is dead (or budget expires).</summary>
    public static bool WaitAllDead(IEnumerable<uint> pids, int budgetMs)
    {
        var sw = Stopwatch.StartNew();
        var remaining = new HashSet<uint>(pids);
        while (remaining.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
        {
            remaining.RemoveWhere(IsDead);
            if (remaining.Count > 0) Thread.Sleep(100);
        }
        return remaining.Count == 0;
    }

    private static IntPtr OpenQuery(uint pid)
    {
        try
        {
            return Native.OpenProcess(
                Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.SYNCHRONIZE,
                false, pid);
        }
        catch { return IntPtr.Zero; }
    }

    private static void CloseBoth(IntPtr hProcess, IntPtr hThread)
    {
        try { if (hProcess != IntPtr.Zero) Native.CloseHandle(hProcess); } catch { }
        try { if (hThread != IntPtr.Zero) Native.CloseHandle(hThread); } catch { }
    }
}
