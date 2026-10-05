// GL-2 Linux execution provider: IExecutionProvider over cgroup v2.
// Native profile only: no watchdog composition, no process-group fallback.
// Atomic placement via posix_spawn + SETCGROUP (CLONE_INTO_CGROUP): Launch
// succeeds only after the child is inside the execution domain. Terminate
// writes cgroup.kill (never PID enumeration); Dispose kills the remainder
// and removes the cgroup. Environment comes exclusively from the spec.
using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux;

public sealed class LinuxExecutionProvider : IExecutionProvider
{
    private readonly object _gate = new();
    private readonly string _cgroupParent;
    private bool _disposed;
    private readonly HashSet<Guid> _preparations = new();
    private readonly Dictionary<Guid, ExecutionRecord> _executions = new();
    private bool _placementChecked;
    private bool _placementOk;
    private string _placementWhy = "";

    private sealed record ExecutionRecord(OwnedCgroup Group, int RootPid, bool Terminated);

    /// <param name="cgroupParent">Delegated cgroup subtree roots go here
    /// (must exist and accept new sub-cgroups; validated at Prepare).</param>
    public LinuxExecutionProvider(string cgroupParent)
    {
        if (string.IsNullOrWhiteSpace(cgroupParent))
            throw new ArgumentException("cgroup parent required", nameof(cgroupParent));
        _cgroupParent = cgroupParent;
    }

    public PlatformCapabilities Describe() => WellKnownPlatforms.Linux;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Native profile only: composition is a separate, explicit layer.
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.Linux, requirements.Required, requirements.Preferred);
        if (!r.Accepted)
            return new PrepareResult.Rejected(r.Unmet);
        if (!CheckDelegation(out string why))
            return new PrepareResult.Rejected(new[] { why });
        // The SETCGROUP flag value is a glibc-version detail that fails
        // SILENTLY (wrong bit = USEVFORK, child born outside the domain).
        // Prove atomic placement with a live probe once per provider: a
        // sacrificial sleeper must appear in its own cgroup.procs, else
        // every Prepare here rejects. No downgrade, no trust in constants.
        if (!_placementChecked)
        {
            _placementChecked = true;
            _placementOk = VerifyAtomicPlacement(out _placementWhy);
        }
        if (!_placementOk)
            return new PrepareResult.Rejected(new[] { _placementWhy });
        var prep = new PreparedExecution(
            WellKnownPlatforms.Linux.Platform, Guid.NewGuid(), r.Met);
        lock (_gate) _preparations.Add(prep.PreparationId);
        return new PrepareResult.Accepted(prep);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            if (prepared.Provider != WellKnownPlatforms.Linux.Platform
                || !_preparations.Contains(prepared.PreparationId))
                return new LaunchResult.Failed(
                    new[] { "unknown preparation: not issued by this provider" });
            // Single-use token: one Launch attempt consumes it.
            _preparations.Remove(prepared.PreparationId);
        }
        if (string.IsNullOrWhiteSpace(process.Executable))
            return new LaunchResult.Failed(new[] { "no executable" });
        if (!TryBuildEnvironment(process.Environment, out string?[] envp, out string envError))
            return new LaunchResult.Failed(new[] { envError });
        return LaunchInner(process, envp!);
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExecutionRecord? rec;
        lock (_gate)
        {
            if (execution.Provider != WellKnownPlatforms.Linux.Platform
                || !_executions.TryGetValue(execution.ExecutionId, out rec))
                return new TerminateResult.Failed(
                    new[] { "unknown execution: not issued by this provider" });
        }
        // Idempotent: killing an empty/gone cgroup still succeeds, and the
        // record stays so repeats classify the same way.
        var (ok, err) = rec.Group.Kill();
        if (!ok)
            return new TerminateResult.Failed(new[] { err });
        ReapRoot(rec.RootPid);
        lock (_gate)
        {
            if (_executions.TryGetValue(execution.ExecutionId, out var cur))
                _executions[execution.ExecutionId] = cur with { Terminated = true };
        }
        return new TerminateResult.Terminated(execution);
    }

    public ValueTask DisposeAsync()
    {
        List<ExecutionRecord> owned;
        lock (_gate)
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            owned = new List<ExecutionRecord>(_executions.Values);
            _executions.Clear();
            _preparations.Clear();
        }
        foreach (var rec in owned)
        {
            try
            {
                rec.Group.Kill();
                ReapRoot(rec.RootPid);
                rec.Group.Dispose();
            }
            catch { }
        }
        return ValueTask.CompletedTask;
    }

    private LaunchResult LaunchInner(ProcessStartSpec process, string?[] envp)
    {
        var (group, gerr) = OwnedCgroup.Create(_cgroupParent);
        if (group is null)
            return new LaunchResult.Failed(new[] { gerr });
        IntPtr attr = IntPtr.Zero;
        IntPtr fa = IntPtr.Zero;
        int cfd = -1;
        try
        {
            attr = Marshal.AllocHGlobal(1024);
            if (NativeMethods.posix_spawnattr_init(attr) != 0)
                return Fail(group, "spawnattr_init failed");
            if (NativeMethods.posix_spawnattr_setflags(attr, NativeMethods.POSIX_SPAWN_SETCGROUP) != 0)
                return Fail(group, "spawnattr_setflags failed");
            fa = Marshal.AllocHGlobal(1024);
            if (NativeMethods.posix_spawn_file_actions_init(fa) != 0)
                return Fail(group, "spawn file_actions_init failed");
            if (!string.IsNullOrWhiteSpace(process.WorkingDirectory)
                && NativeMethods.posix_spawn_file_actions_addchdir_np(fa, process.WorkingDirectory) != 0)
                return Fail(group, $"spawn addchdir refused for '{process.WorkingDirectory}'");
            cfd = NativeMethods.open(group.Path,
                NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY | NativeMethods.O_CLOEXEC);
            if (cfd < 0)
                return Fail(group, $"cgroup fd open failed err={Marshal.GetLastWin32Error()}");
            if (NativeMethods.posix_spawnattr_setcgroup_np(attr, cfd) != 0)
                return Fail(group, "spawnattr_setcgroup refused (EOPNOTSUPP-class: no CLONE_INTO_CGROUP support)");
            string[] argv = new[] { process.Executable }
                .Concat(NativeMethods.SplitArguments(process.Arguments)).ToArray();
            string?[] argvZ = new string?[argv.Length + 1];
            Array.Copy(argv, argvZ, argv.Length);
            int rc = NativeMethods.posix_spawn(out int pid, process.Executable,
                fa, attr, argvZ, envp);
            if (rc != 0)
                return Fail(group, ClassifySpawnError(rc));
            var handle = new ExecutionHandle(WellKnownPlatforms.Linux.Platform, Guid.NewGuid());
            lock (_gate) _executions[handle.ExecutionId] = new ExecutionRecord(group, pid, false);
            return new LaunchResult.Started(handle);
        }
        finally
        {
            if (cfd >= 0)
            {
                try { NativeMethods.close(cfd); } catch { }
            }
            if (attr != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.posix_spawnattr_destroy(attr);
                    Marshal.FreeHGlobal(attr);
                }
                catch { }
            }
            if (fa != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.posix_spawn_file_actions_destroy(fa);
                    Marshal.FreeHGlobal(fa);
                }
                catch { }
            }
        }

        LaunchResult Fail(OwnedCgroup g, string reason)
        {
            try { g.Dispose(); } catch { }
            return new LaunchResult.Failed(new[] { reason });
        }
    }

    private bool VerifyAtomicPlacement(out string why)
    {
        why = "";
        var (group, gerr) = OwnedCgroup.Create(_cgroupParent);
        if (group is null) { why = gerr; return false; }
        IntPtr attr = IntPtr.Zero;
        int cfd = -1;
        int pid = -1;
        try
        {
            attr = Marshal.AllocHGlobal(1024);
            if (NativeMethods.posix_spawnattr_init(attr) != 0) { why = "probe: spawnattr_init failed"; return false; }
            if (NativeMethods.posix_spawnattr_setflags(attr, NativeMethods.POSIX_SPAWN_SETCGROUP) != 0) { why = "probe: setflags failed"; return false; }
            cfd = NativeMethods.open(group.Path,
                NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY | NativeMethods.O_CLOEXEC);
            if (cfd < 0) { why = "probe: cgroup fd open failed"; return false; }
            if (NativeMethods.posix_spawnattr_setcgroup_np(attr, cfd) != 0) { why = "probe: setcgroup refused"; return false; }
            string?[] argv = new string?[] { "/bin/sleep", "30", null };
            string?[] envp = new string?[] { "PATH=/usr/bin:/bin", null };
            int rc = NativeMethods.posix_spawn(out pid, "/bin/sleep", IntPtr.Zero, attr, argv, envp);
            if (rc != 0) { why = $"probe: spawn refused errno={rc}"; return false; }
            // The child must be BORN inside: procs lists it without any
            // parent-side move. Absent = flag/bit ignored = fail closed.
            string procs = "";
            try { procs = File.ReadAllText(System.IO.Path.Combine(group.Path, "cgroup.procs")); } catch { }
            bool born = false;
            foreach (string line in procs.Split('\n'))
                if (line.Trim() == pid.ToString()) { born = true; break; }
            if (!born) { why = "probe: child not born inside its cgroup (SETCGROUP bit ignored on this glibc)"; return false; }
            return true;
        }
        finally
        {
            if (pid > 0)
            {
                try { NativeMethods.kill(pid, 9); } catch { }
                try { NativeMethods.waitpid(pid, out _, 0); } catch { }
            }
            if (cfd >= 0) { try { NativeMethods.close(cfd); } catch { } }
            if (attr != IntPtr.Zero)
            {
                try { NativeMethods.posix_spawnattr_destroy(attr); Marshal.FreeHGlobal(attr); } catch { }
            }
            try { group.Dispose(); } catch { }
        }
    }

    private static string ClassifySpawnError(int errno) => errno switch
    {
        NativeMethods.EOPNOTSUPP =>
            "spawn refused EOPNOTSUPP: no race-free CLONE_INTO_CGROUP support (kernel/glibc too old)",
        NativeMethods.ENOENT =>
            "spawn refused ENOENT: executable not found",
        _ => $"spawn refused errno={errno} (no target running outside the domain)",
    };

    private bool CheckDelegation(out string why)
    {
        // Probe: create and remove one directory here. No workload runs;
        // failure classifies as missing delegation, never as success.
        why = "";
        string probe = "";
        try
        {
            probe = System.IO.Path.Combine(_cgroupParent,
                "gagamba-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            if (!File.Exists(System.IO.Path.Combine(probe, "cgroup.kill")))
            {
                why = $"not a cgroup v2 mount at '{_cgroupParent}' (no cgroup.kill)";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            why = $"cgroup delegation unavailable at '{_cgroupParent}': {ex.GetType().Name}";
            return false;
        }
        finally
        {
            if (probe.Length > 0)
            {
                try { Directory.Delete(probe); } catch { }
            }
        }
    }

    internal static bool TryBuildEnvironment(
        IReadOnlyDictionary<string, string> spec, out string?[] envp, out string error)
    {
        envp = Array.Empty<string?>();
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
        var arr = spec.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}").ToList<string?>();
        arr.Add(null);
        envp = arr.ToArray();
        return true;
    }

    private static void ReapRoot(int pid)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 5000)
            {
                int rc = NativeMethods.waitpid(pid, out _, NativeMethods.WNOHANG);
                if (rc != 0) return; // reaped, or ECHILD (already gone)
                Thread.Sleep(100);
            }
        }
        catch { }
    }
}
