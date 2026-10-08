using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux;

/// <summary>Linux cgroup-v2 provider. A successful launch is born in its cgroup.</summary>
public sealed class LinuxExecutionProvider : IExecutionProvider
{
    private readonly object _gate = new();
    private readonly string _cgroupParent;
    private bool _disposed;
    private Task? _disposeTask;
    private TaskCompletionSource? _launchesDrained;
    private readonly Dictionary<Guid, OwnedCgroup> _preparations = new();
    private readonly HashSet<OwnedCgroup> _launches = new();
    private readonly HashSet<OwnedCgroup> _failedGroups = new();
    private readonly List<string> _terminalFailures = new();
    private readonly Dictionary<Guid, LinuxExecutionState> _executions = new();
    private readonly Dictionary<Guid, (WeakReference<ExecutionHandle> Handle, Task<CompletionResult> Result)> _completed = new();
    private int _completedSincePrune;
    private bool _placementChecked;
    private bool _placementOk;
    private string _placementWhy = "";
    private string? _probeCleanupFailure;

    // Test seam rejects prerequisites before a native call or workload can run.
    internal string? MissingSymbolForTest { get; set; }
    internal int? ProbeSpawnErrnoForTest { get; set; }
    internal string? DenyCgroupOperationForTest { get; set; }
    internal int PlacementProbeRunsForTest { get; private set; }

    public LinuxExecutionProvider(string cgroupParent)
    {
        if (string.IsNullOrWhiteSpace(cgroupParent))
            throw new ArgumentException("cgroup parent required", nameof(cgroupParent));
        _cgroupParent = cgroupParent;
    }

    public PlatformCapabilities Describe() => WellKnownPlatforms.Linux;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.Linux, requirements.Required, requirements.Preferred);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!r.Accepted) return new PrepareResult.Rejected(r.Unmet);
            if (!CheckPrerequisites(out string why))
                return new PrepareResult.Rejected(new[] { why });
            if (!CheckDelegation(out why))
                return new PrepareResult.Rejected(new[] { why });
            // The first self-test and all concurrent preparations share this gate.
            if (!_placementChecked)
            {
                _placementOk = VerifyAtomicPlacement(out _placementWhy);
                if (_probeCleanupFailure is not null)
                {
                    _placementOk = false;
                    _placementWhy = "probe cleanup: " + _probeCleanupFailure;
                }
                _placementChecked = true;
            }
            if (!_placementOk) return new PrepareResult.Rejected(new[] { _placementWhy });
            var (group, error) = OwnedCgroup.Create(_cgroupParent);
            if (group is null) return new PrepareResult.Rejected(new[] { error });
            group.FaultForTest = op => DenyCgroupOperationForTest == op;
            var prepared = new PreparedExecution(WellKnownPlatforms.Linux.Platform, Guid.NewGuid(), r.Met);
            _preparations.Add(prepared.PreparationId, group);
            return new PrepareResult.Accepted(prepared);
        }
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        OwnedCgroup? group;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (preparation.Provider != WellKnownPlatforms.Linux.Platform)
                return new DiscardResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            _preparations.Remove(preparation.PreparationId, out group);
        }
        if (group is not null)
        {
            string? error = group.StopAndRemoveAsync(false, TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            if (error is not null)
            {
                lock (_gate) _failedGroups.Add(group);
                return new DiscardResult.Failed(new[] { error });
            }
        }
        return new DiscardResult.Discarded(preparation);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        OwnedCgroup group;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (prepared.Provider != WellKnownPlatforms.Linux.Platform
                || !_preparations.Remove(prepared.PreparationId, out group!))
                return new LaunchResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            _launches.Add(group);
        }
        LaunchResult result;
        string? cleanupFailure = null;
        try
        {
            if (string.IsNullOrWhiteSpace(process.Executable))
                result = new LaunchResult.Failed(new[] { "no executable" });
            else if (!TryBuildEnvironment(process.Environment, out var envp, out string error))
                result = new LaunchResult.Failed(new[] { error });
            else
            {
                // Spawn and registration are one transition; disposal cannot win
                // between these two events. Expensive environment work is outside.
                lock (_gate)
                    result = _disposed
                        ? new LaunchResult.Failed(new[] { "disposal won before target spawn" })
                        : LaunchInner(group, process, envp);
            }
        }
        catch (Exception ex)
        {
            result = new LaunchResult.Failed(new[] { $"launch fault: {ex.GetType().Name}: {ex.Message}" });
        }
        finally
        {
            // The launched state takes the group; a failed launch retains it
            // until cleanup has been confirmed, including during disposal.
            lock (_gate)
            {
                if (!_executions.Values.Any(state => ReferenceEquals(state.Group, group)))
                {
                    string? error = group.StopAndRemoveAsync(true, TimeSpan.FromSeconds(5))
                        .GetAwaiter().GetResult();
                    if (error is not null) { _failedGroups.Add(group); cleanupFailure = error; }
                }
                _launches.Remove(group);
                if (_launches.Count == 0) _launchesDrained?.TrySetResult();
            }
        }
        return cleanupFailure is null ? result : new LaunchResult.Failed(new[] { cleanupFailure });
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        LinuxExecutionState? state;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (execution.Provider != WellKnownPlatforms.Linux.Platform)
                return new TerminateResult.Failed(new[] { "unknown execution: not issued by this provider" });
            if (!_executions.TryGetValue(execution.ExecutionId, out state))
            {
                if (_completed.TryGetValue(execution.ExecutionId, out var completed)
                    && completed.Handle.TryGetTarget(out _)
                    && completed.Result.Result is CompletionResult.Terminated)
                    return new TerminateResult.Terminated(execution);
                return new TerminateResult.Failed(new[] { "unknown or already terminal execution" });
            }
        }
        return state.Stop();
    }

    public ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle execution,
        CancellationToken cancellationToken = default)
    {
        Task<CompletionResult> result;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (execution.Provider != WellKnownPlatforms.Linux.Platform)
                return new ValueTask<CompletionResult>(new CompletionResult.Failed(
                    new[] { "unknown execution: not issued by this provider" }));
            if (_executions.TryGetValue(execution.ExecutionId, out var state)) result = state.Completion;
            else if (_completed.TryGetValue(execution.ExecutionId, out var completed)
                     && completed.Handle.TryGetTarget(out _)) result = completed.Result;
            else return new ValueTask<CompletionResult>(new CompletionResult.Failed(
                    new[] { "unknown execution: not issued by this provider" }));
        }
        return new ValueTask<CompletionResult>(result.WaitAsync(cancellationToken));
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposeTask is not null)
            {
                if (_disposeTask.IsFaulted && _failedGroups.Count > 0)
                    _disposeTask = RetryFailedGroupsAsync();
                return new ValueTask(_disposeTask);
            }
            _disposed = true;
            var prepared = _preparations.Values.ToArray();
            _preparations.Clear();
            if (_launches.Count > 0)
                _launchesDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = DisposeCoreAsync(prepared, _launchesDrained?.Task);
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync(OwnedCgroup[] prepared, Task? launchesDrained)
    {
        await Task.Yield();
        var errors = new List<string>();
        foreach (var group in prepared)
        {
            string? error = await group.StopAndRemoveAsync(false, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (error is not null) { errors.Add(error); lock (_gate) _failedGroups.Add(group); }
        }
        if (launchesDrained is not null) await launchesDrained.ConfigureAwait(false);
        LinuxExecutionState[] running;
        lock (_gate) running = _executions.Values.ToArray();
        var join = new List<LinuxExecutionState>();
        foreach (var state in running)
        {
            if (state.Stop() is TerminateResult.Failed failure
                && !state.Completion.IsCompleted && !state.Group.IsRemoved)
            {
                errors.AddRange(failure.Reasons);
                lock (_gate) _failedGroups.Add(state.Group);
            }
            else join.Add(state);
        }
        var results = await Task.WhenAll(join.Select(state => state.Completion)).ConfigureAwait(false);
        foreach (var result in results.OfType<CompletionResult.Failed>()) errors.AddRange(result.Reasons);
        lock (_gate) errors.AddRange(_terminalFailures);
        OwnedCgroup[] failed;
        lock (_gate) failed = _failedGroups.ToArray();
        foreach (var group in failed)
        {
            string? error = await group.StopAndRemoveAsync(true, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (error is not null) errors.Add(error);
            else lock (_gate) _failedGroups.Remove(group);
        }
        if (errors.Count != 0) throw new InvalidOperationException("Linux cleanup unconfirmed: " + string.Join("; ", errors));
    }

    private async Task RetryFailedGroupsAsync()
    {
        await Task.Yield();
        LinuxExecutionState[] running;
        lock (_gate) running = _executions.Values.ToArray();
        var errors = new List<string>();
        var join = new List<LinuxExecutionState>();
        foreach (var state in running)
        {
            if (state.Stop() is TerminateResult.Failed failure
                && !state.Completion.IsCompleted && !state.Group.IsRemoved)
                errors.AddRange(failure.Reasons);
            else join.Add(state);
        }
        var outcomes = await Task.WhenAll(join.Select(state => state.Completion)).ConfigureAwait(false);
        foreach (var outcome in outcomes.OfType<CompletionResult.Failed>()) errors.AddRange(outcome.Reasons);
        OwnedCgroup[] failed;
        lock (_gate) failed = _failedGroups.ToArray();
        foreach (var group in failed)
        {
            string? error = await group.StopAndRemoveAsync(true, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (error is not null) errors.Add(error);
            else lock (_gate) _failedGroups.Remove(group);
        }
        if (errors.Count > 0) throw new InvalidOperationException("Linux cleanup unconfirmed: " + string.Join("; ", errors));
    }

    private void OnTerminal(LinuxExecutionState state, CompletionResult result)
    {
        lock (_gate)
        {
            _executions.Remove(state.Handle.ExecutionId);
            if (result is CompletionResult.Failed failure)
            {
                _failedGroups.Add(state.Group);
                _terminalFailures.AddRange(failure.Reasons);
            }
            if (++_completedSincePrune >= 64)
            {
                _completedSincePrune = 0;
                foreach (var id in _completed.Where(pair => !pair.Value.Handle.TryGetTarget(out _))
                    .Select(pair => pair.Key).ToArray()) _completed.Remove(id);
            }
            _completed[state.Handle.ExecutionId] =
                (new WeakReference<ExecutionHandle>(state.Handle), Task.FromResult(result));
        }
    }

    private LaunchResult LaunchInner(OwnedCgroup group, ProcessStartSpec process, string?[] envp)
    {
        IntPtr attr = IntPtr.Zero, fa = IntPtr.Zero;
        bool attrReady = false, actionsReady = false;
        int cfd = -1;
        try
        {
            attr = Marshal.AllocHGlobal(1024);
            int rc = NativeMethods.posix_spawnattr_init(attr);
            if (rc != 0) return new LaunchResult.Failed(new[] { $"spawnattr_init errno={rc}" });
            attrReady = true;
            rc = NativeMethods.posix_spawnattr_setflags(attr, NativeMethods.POSIX_SPAWN_SETCGROUP);
            if (rc != 0) return new LaunchResult.Failed(new[] { $"spawnattr_setflags errno={rc}" });
            fa = Marshal.AllocHGlobal(1024);
            rc = NativeMethods.posix_spawn_file_actions_init(fa);
            if (rc != 0) return new LaunchResult.Failed(new[] { $"file_actions_init errno={rc}" });
            actionsReady = true;
            if (!string.IsNullOrWhiteSpace(process.WorkingDirectory))
            {
                rc = NativeMethods.posix_spawn_file_actions_addchdir_np(fa, process.WorkingDirectory);
                if (rc != 0) return new LaunchResult.Failed(new[] { $"spawn addchdir errno={rc}" });
            }
            // Keep trusted stdio 0/1/2; close every unrelated descriptor in
            // the child before target code, including callers' inheritable FDs.
            rc = NativeMethods.posix_spawn_file_actions_addclosefrom_np(fa, 3);
            if (rc != 0) return new LaunchResult.Failed(new[] { $"spawn addclosefrom errno={rc}" });
            cfd = NativeMethods.open(group.Path,
                NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY | NativeMethods.O_CLOEXEC);
            if (cfd < 0) return new LaunchResult.Failed(new[] { $"cgroup fd open errno={Marshal.GetLastWin32Error()}" });
            rc = NativeMethods.posix_spawnattr_setcgroup_np(attr, cfd);
            if (rc != 0) return new LaunchResult.Failed(new[] { $"spawnattr_setcgroup errno={rc}" });
            string[] argv = new[] { process.Executable }.Concat(NativeMethods.SplitArguments(process.Arguments)).ToArray();
            string?[] argvZ = new string?[argv.Length + 1];
            Array.Copy(argv, argvZ, argv.Length);
            rc = NativeMethods.posix_spawn(out int pid, process.Executable, fa, attr, argvZ, envp);
            if (rc != 0) return new LaunchResult.Failed(new[] { ClassifySpawnError(rc) });
            var handle = new ExecutionHandle(WellKnownPlatforms.Linux.Platform, Guid.NewGuid());
            var state = new LinuxExecutionState(handle, group, pid, OnTerminal);
            _executions.Add(handle.ExecutionId, state);
            state.Start();
            return new LaunchResult.Started(handle);
        }
        finally
        {
            if (cfd >= 0) NativeMethods.close(cfd);
            if (actionsReady) NativeMethods.posix_spawn_file_actions_destroy(fa);
            if (fa != IntPtr.Zero) Marshal.FreeHGlobal(fa);
            if (attrReady) NativeMethods.posix_spawnattr_destroy(attr);
            if (attr != IntPtr.Zero) Marshal.FreeHGlobal(attr);
        }
    }

    private bool VerifyAtomicPlacement(out string why)
    {
        why = "";
        PlacementProbeRunsForTest++;
        var (group, error) = OwnedCgroup.Create(_cgroupParent);
        if (group is null) { why = error; return false; }
        group.FaultForTest = op => DenyCgroupOperationForTest == op;
        IntPtr attr = IntPtr.Zero, fa = IntPtr.Zero;
        bool attrReady = false, actionsReady = false;
        int cfd = -1, pid = -1;
        try
        {
            attr = Marshal.AllocHGlobal(1024);
            int rc = NativeMethods.posix_spawnattr_init(attr);
            if (rc != 0) { why = $"probe: spawnattr_init errno={rc}"; return false; }
            attrReady = true;
            rc = NativeMethods.posix_spawnattr_setflags(attr, NativeMethods.POSIX_SPAWN_SETCGROUP);
            if (rc != 0) { why = $"probe: setflags errno={rc}"; return false; }
            fa = Marshal.AllocHGlobal(1024);
            rc = NativeMethods.posix_spawn_file_actions_init(fa);
            if (rc != 0) { why = $"probe: file_actions_init errno={rc}"; return false; }
            actionsReady = true;
            rc = NativeMethods.posix_spawn_file_actions_addclosefrom_np(fa, 3);
            if (rc != 0) { why = $"probe: addclosefrom errno={rc}"; return false; }
            cfd = NativeMethods.open(group.Path,
                NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY | NativeMethods.O_CLOEXEC);
            if (cfd < 0) { why = $"probe: cgroup fd open errno={Marshal.GetLastWin32Error()}"; return false; }
            rc = NativeMethods.posix_spawnattr_setcgroup_np(attr, cfd);
            if (rc != 0) { why = $"probe: setcgroup errno={rc}"; return false; }
            if (ProbeSpawnErrnoForTest is int injected)
            { why = $"probe: {ClassifySpawnError(injected)}"; return false; }
            rc = NativeMethods.posix_spawn(out pid, "/bin/sleep", fa, attr,
                new string?[] { "/bin/sleep", "5", null }, new string?[] { "PATH=/usr/bin:/bin", null });
            if (rc != 0) { why = $"probe: {ClassifySpawnError(rc)}"; return false; }
            string procs = File.ReadAllText(System.IO.Path.Combine(group.Path, "cgroup.procs"));
            if (!procs.Split('\n').Any(line => line.Trim() == pid.ToString()))
            { why = "probe: child not born inside its cgroup (SETCGROUP bit ignored)"; return false; }
            return true;
        }
        catch (Exception ex) { why = $"probe fault: {ex.GetType().Name}: {ex.Message}"; return false; }
        finally
        {
            if (pid > 0)
            {
                if (NativeMethods.kill(pid, 9) != 0 && Marshal.GetLastWin32Error() != NativeMethods.ESRCH)
                    _probeCleanupFailure = $"probe root kill errno={Marshal.GetLastWin32Error()}";
                var timer = System.Diagnostics.Stopwatch.StartNew();
                bool reaped = false;
                while (timer.Elapsed < TimeSpan.FromSeconds(6))
                {
                    int rc = NativeMethods.waitpid(pid, out _, NativeMethods.WNOHANG);
                    if (rc == pid) { reaped = true; break; }
                    if (rc < 0 && Marshal.GetLastWin32Error() != NativeMethods.EINTR) break;
                    Thread.Sleep(50);
                }
                if (!reaped) _probeCleanupFailure = "probe root reap unconfirmed";
            }
            if (cfd >= 0) NativeMethods.close(cfd);
            if (actionsReady) NativeMethods.posix_spawn_file_actions_destroy(fa);
            if (fa != IntPtr.Zero) Marshal.FreeHGlobal(fa);
            if (attrReady) NativeMethods.posix_spawnattr_destroy(attr);
            if (attr != IntPtr.Zero) Marshal.FreeHGlobal(attr);
            string? cleanup = group.StopAndRemoveAsync(true, TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            if (cleanup is not null) { _failedGroups.Add(group); _probeCleanupFailure = cleanup; }
        }
    }

    private bool CheckPrerequisites(out string why)
    {
        why = "";
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
        { why = "native Linux x86-64 glibc profile required"; return false; }
        nint libc;
        try
        {
            if (!NativeLibrary.TryLoad("libc.so.6", out libc))
            { why = "glibc libc.so.6 unavailable"; return false; }
        }
        catch (Exception ex)
        { why = $"glibc load fault: {ex.GetType().Name}: {ex.Message}"; return false; }
        try
        {
            string[] symbols = ["gnu_get_libc_version", "posix_spawn", "posix_spawnattr_init",
                "posix_spawnattr_destroy", "posix_spawnattr_setflags", "posix_spawnattr_setcgroup_np",
                "posix_spawn_file_actions_init", "posix_spawn_file_actions_destroy",
                "posix_spawn_file_actions_addchdir_np", "posix_spawn_file_actions_addclosefrom_np",
                "open", "close", "waitpid", "kill"];
            foreach (string symbol in symbols)
                if (symbol == MissingSymbolForTest || !NativeLibrary.TryGetExport(libc, symbol, out _))
                { why = $"glibc required symbol unavailable: {symbol}"; return false; }
            string? version = Marshal.PtrToStringAnsi(NativeMethods.gnu_get_libc_version());
            if (version is null || !version.StartsWith("2.43", StringComparison.Ordinal))
            { why = $"unqualified glibc ABI version: {version ?? "unknown"} (requires 2.43)"; return false; }
            return true;
        }
        finally { NativeLibrary.Free(libc); }
    }

    private static string ClassifySpawnError(int errno) => errno switch
    {
        NativeMethods.EOPNOTSUPP => "spawn refused EOPNOTSUPP: CLONE_INTO_CGROUP unsupported",
        NativeMethods.ENOSYS => "spawn refused ENOSYS: required kernel clone support unavailable",
        NativeMethods.EACCES => "spawn refused EACCES: cgroup placement permission denied",
        NativeMethods.ENOENT => "spawn refused ENOENT: executable not found",
        _ => $"spawn refused errno={errno} (no target running outside the domain)",
    };

    private bool CheckDelegation(out string why)
    {
        why = "";
        string probe = System.IO.Path.Combine(_cgroupParent, "gagamba-probe-" + Guid.NewGuid().ToString("N"));
        bool valid = false;
        try
        {
            Directory.CreateDirectory(probe);
            if (!File.Exists(System.IO.Path.Combine(probe, "cgroup.kill")))
                why = $"not a cgroup v2 mount at '{_cgroupParent}' (no cgroup.kill)";
            else valid = true;
        }
        catch (Exception ex)
        { why = $"cgroup delegation unavailable at '{_cgroupParent}': {ex.GetType().Name}: {ex.Message}"; }
        finally
        {
            try { if (Directory.Exists(probe)) Directory.Delete(probe); }
            catch (Exception ex)
            {
                _failedGroups.Add(OwnedCgroup.Adopt(probe));
                why = $"delegation probe cleanup fault: {ex.GetType().Name}: {ex.Message}";
            }
        }
        return valid && why.Length == 0;
    }

    internal static bool TryBuildEnvironment(IReadOnlyDictionary<string, string> spec,
        out string?[] envp, out string error)
    {
        envp = Array.Empty<string?>(); error = "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in spec)
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Key.Contains('=') || kv.Key.Contains('\0'))
            { error = $"bad environment name: '{kv.Key}'"; return false; }
            if (kv.Value.Contains('\0'))
            { error = $"NUL byte in environment value for '{kv.Key}'"; return false; }
            if (!seen.Add(kv.Key))
            { error = $"duplicate environment variable: '{kv.Key}'"; return false; }
        }
        var entries = spec.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}").ToList<string?>();
        entries.Add(null); envp = entries.ToArray(); return true;
    }
}
