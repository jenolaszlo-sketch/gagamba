// GW-2 Windows execution provider: IExecutionProvider over Job Objects.
// Negotiation reflects the GP-2 Windows profile; suspend-assign-resume has
// no escape window; assignment failure terminates the suspended target and
// proves it dead (fail-closed); termination uses the job, never PID lists;
// disposal closes job handles (kill-on-close does the teardown). No quotas,
// graceful shutdown, output capture, or telemetry.
using System.Runtime.InteropServices;
using System.Text;

namespace Gagamba.Execution.Windows;

public sealed class WindowsExecutionProvider : IExecutionProvider
{
    public const uint TerminateExitCode = 99;

    private readonly object _gate = new();
    private bool _disposed;
    private readonly Dictionary<Guid, OwnedJob> _preparations = new();
    private readonly Dictionary<Guid, OwnedJob> _executions = new();

    public PlatformCapabilities Describe() => WellKnownPlatforms.Windows;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.Windows, requirements.Required, requirements.Preferred);
        if (!r.Accepted)
            return new PrepareResult.Rejected(r.Unmet);
        // Allocate the domain at preparation so a caller that never launches
        // can reclaim it explicitly through Discard.
        var (job, jobError) = OwnedJob.Create();
        if (job is null)
            return new PrepareResult.Rejected(new[] { jobError });
        var prep = new PreparedExecution(
            WellKnownPlatforms.Windows.Platform, Guid.NewGuid(), r.Met);
        lock (_gate) _preparations[prep.PreparationId] = job;
        return new PrepareResult.Accepted(prep);
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (preparation.Provider != WellKnownPlatforms.Windows.Platform)
            return new DiscardResult.Failed(
                new[] { "unknown preparation: not issued by this provider" });
        // Idempotent: a preparation already launched or discarded is a no-op.
        OwnedJob? job;
        lock (_gate) _preparations.Remove(preparation.PreparationId, out job);
        try { job?.Dispose(); } catch { }
        return new DiscardResult.Discarded(preparation);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        OwnedJob? job;
        lock (_gate)
        {
            if (prepared.Provider != WellKnownPlatforms.Windows.Platform
                || !_preparations.Remove(prepared.PreparationId, out job))
                return new LaunchResult.Failed(
                    new[] { "unknown preparation: not issued by this provider" });
            // Single-use: consuming the preparation removes it above, success
            // or failure, so it can never become a reusable authority.
        }
        if (string.IsNullOrWhiteSpace(process.Executable))
        {
            try { job.Dispose(); } catch { }
            return new LaunchResult.Failed(new[] { "no executable" });
        }
        if (!TryBuildEnvironment(process.Environment, out IntPtr env, out string envError))
        {
            try { job.Dispose(); } catch { }
            return new LaunchResult.Failed(new[] { envError });
        }
        try
        {
            return LaunchInner(job, process, env);
        }
        finally
        {
            if (env != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(env); } catch { }
            }
        }
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        OwnedJob? job;
        lock (_gate)
        {
            if (execution.Provider != WellKnownPlatforms.Windows.Platform
                || !_executions.TryGetValue(execution.ExecutionId, out job))
                return new TerminateResult.Failed(
                    new[] { "unknown execution: not issued by this provider" });
        }
        // Idempotent: terminating twice (or terminating the already-dead)
        // succeeds. Cleanup races must not become errors.
        var (ok, err) = job.Terminate(TerminateExitCode);
        if (!ok && !IsGone(job))
            return new TerminateResult.Failed(new[] { err });
        return new TerminateResult.Terminated(execution);
    }

    public ValueTask DisposeAsync()
    {
        List<OwnedJob> owned;
        List<OwnedJob> prepared;
        lock (_gate)
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            owned = new List<OwnedJob>(_executions.Values);
            _executions.Clear();
            prepared = new List<OwnedJob>(_preparations.Values);
            _preparations.Clear();
        }
        // Closing each job fires KILL_ON_JOB_CLOSE: the kernel tears down
        // every remaining tree. No TerminateJobObject needed on this path.
        foreach (var job in owned)
        {
            try { job.Dispose(); } catch { }
        }
        // Unlaunched preparations hold a job handle; close it too.
        foreach (var job in prepared)
        {
            try { job.Dispose(); } catch { }
        }
        return ValueTask.CompletedTask;
    }

    private LaunchResult LaunchInner(OwnedJob job, ProcessStartSpec process, IntPtr env)
    {
        var commandLine = new StringBuilder(32768);
        commandLine.Append('"').Append(process.Executable).Append("\" ").Append(process.Arguments);
        var si = new NativeMethods.StartupInfo { cb = Marshal.SizeOf<NativeMethods.StartupInfo>() };
        bool created;
        NativeMethods.ProcessInformation pi;
        try
        {
            created = NativeMethods.CreateProcessW(null, commandLine,
                IntPtr.Zero, IntPtr.Zero, false,
                NativeMethods.CREATE_SUSPENDED | NativeMethods.CREATE_UNICODE_ENVIRONMENT,
                env, string.IsNullOrWhiteSpace(process.WorkingDirectory) ? null : process.WorkingDirectory,
                ref si, out pi);
        }
        catch (Exception ex)
        {
            job.Dispose();
            return new LaunchResult.Failed(new[] { $"invocation fault: {ex.GetType().Name}" });
        }
        if (!created)
        {
            int err = Marshal.GetLastWin32Error();
            job.Dispose();
            return new LaunchResult.Failed(new[] { $"CreateProcess err=0x{err:X} (no target ran)" });
        }
        uint pid = NativeMethods.GetProcessId(pi.hProcess);
        if (!TryAssign(job, pi.hProcess, out int assignErr))
        {
            // Fail closed: the target never ran unsandboxed... it never ran
            // at all (still suspended). Terminate it, prove it dead.
            try { NativeMethods.TerminateProcess(pi.hProcess, TerminateExitCode); } catch { }
            CloseQuiet(pi.hProcess);
            CloseQuiet(pi.hThread);
            job.Dispose();
            bool dead = WaitPidDead(pid, 5000);
            return new LaunchResult.Failed(new[] {
                $"assign rejected err=0x{assignErr:X}; suspended target terminated, dead={dead}" });
        }
        if (NativeMethods.ResumeThread(pi.hThread) == uint.MaxValue)
        {
            int err = Marshal.GetLastWin32Error();
            try { NativeMethods.TerminateProcess(pi.hProcess, TerminateExitCode); } catch { }
            CloseQuiet(pi.hProcess);
            CloseQuiet(pi.hThread);
            job.Dispose();
            bool dead = WaitPidDead(pid, 5000);
            return new LaunchResult.Failed(new[] {
                $"resume fault err=0x{err:X}; target terminated, dead={dead}" });
        }
        CloseQuiet(pi.hThread);
        CloseQuiet(pi.hProcess);
        var handle = new ExecutionHandle(WellKnownPlatforms.Windows.Platform, Guid.NewGuid());
        lock (_gate) _executions[handle.ExecutionId] = job;
        return new LaunchResult.Started(handle);
    }

    /// <summary>
    /// Assign with classified failure. Separated for direct unit coverage:
    /// a dead/incompatible target must fail here, never reach resume.
    /// </summary>
    internal static bool TryAssign(OwnedJob job, IntPtr process, out int error) =>
        TryAssign(job.DangerousHandle, process, out error);

    internal static bool TryAssign(IntPtr job, IntPtr process, out int error)
    {
        error = 0;
        if (job == IntPtr.Zero || process == IntPtr.Zero)
        {
            error = 6; // ERROR_INVALID_HANDLE
            return false;
        }
        if (NativeMethods.AssignProcessToJobObject(job, process))
            return true;
        error = Marshal.GetLastWin32Error();
        return false;
    }

    private static bool IsGone(OwnedJob job) => job.IsClosed;

    private static void CloseQuiet(IntPtr h)
    {
        try { if (h != IntPtr.Zero) NativeMethods.CloseHandle(h); } catch { }
    }

    private static bool WaitPidDead(uint pid, int budgetMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < budgetMs)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                h = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION | NativeMethods.SYNCHRONIZE,
                    false, pid);
                if (h == IntPtr.Zero) return true;
                if (NativeMethods.GetExitCodeProcess(h, out uint rc) && rc != NativeMethods.STILL_ACTIVE)
                    return true;
            }
            catch { return true; }
            finally { CloseQuiet(h); }
            Thread.Sleep(100);
        }
        return false;
    }

    /// <summary>
    /// Child environment is built EXCLUSIVELY from the spec: sorted,
    /// double-null-terminated block. No ambient inheritance. Rejects
    /// unencodable entries (NUL bytes, '=' in names, empty names,
    /// case-insensitive duplicates) before any process starts.
    /// </summary>
    internal static bool TryBuildEnvironment(
        IReadOnlyDictionary<string, string> spec, out IntPtr block, out string error)
    {
        block = IntPtr.Zero;
        error = "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in spec)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Key.Contains('=') || kv.Key.Contains('\0'))
            {
                error = $"bad environment name: '{kv.Key}'";
                return false;
            }
            if (kv.Value.Contains('\0'))
            {
                error = $"NUL byte in environment value for '{kv.Key}'";
                return false;
            }
            if (!seen.Add(kv.Key))
            {
                error = $"duplicate environment variable: '{kv.Key}'";
                return false;
            }
        }
        string text = string.Join("\0",
            spec.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value}")) + "\0";
        // Manual double-null-terminated block: StringToHGlobalUni must not
        // be trusted with interior NULs (measured 0x57 from CreateProcess).
        char[] chars = text.ToCharArray();
        try
        {
            block = Marshal.AllocHGlobal((chars.Length + 1) * 2);
            Marshal.Copy(chars, 0, block, chars.Length);
            Marshal.WriteInt16(block, chars.Length * 2, 0);
            return true;
        }
        catch (Exception ex)
        {
            if (block != IntPtr.Zero)
            {
                try { Marshal.FreeHGlobal(block); } catch { }
                block = IntPtr.Zero;
            }
            error = $"environment encode fault: {ex.GetType().Name}";
            return false;
        }
    }
}
