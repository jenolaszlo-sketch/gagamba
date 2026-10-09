using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Windows;

/// <summary>Windows lifecycle provider over kill-on-close Job Objects.</summary>
public sealed class WindowsExecutionProvider : IExecutionProvider
{
    public const uint TerminateExitCode = 99;

    // Gate linearizes admission, release, terminal removal and disposal.
    // Prepared -> LaunchAdmission -> WindowsExecutionState -> terminal task.
    private readonly object _gate = new();
    private bool _disposed;
    private Task? _disposeTask;
    private TaskCompletionSource? _launchesDrained;
    private readonly Dictionary<Guid, OwnedJob> _preparations = new();
    // Weak keys retain spent-token idempotence only while the caller retains
    // the exact issued object; forged equal records do not gain provenance.
    private readonly ConditionalWeakTable<PreparedExecution, StrongBox<byte>> _issued = new();
    private readonly Dictionary<Guid, PreparedExecution> _activeTokens = new();
    private readonly HashSet<LaunchAdmission> _launches = new();
    private readonly List<LaunchAdmission> _failedAdmissions = new();
    private readonly Dictionary<Guid, WindowsExecutionState> _executions = new();
    private readonly Dictionary<Guid, (WeakReference<ExecutionHandle> Handle, Task<CompletionResult> Result)> _completed = new();
    private int _completedSincePrune;

    // Internal fault injection is limited to tests; it never relaxes the
    // production assignment/resume path.
    internal bool FailAssignmentForTest { get; set; }
    internal bool FailResumeForTest { get; set; }
    internal int CleanupConfirmationFailuresForTest;

    private sealed class LaunchAdmission(OwnedJob job)
    {
        public OwnedJob Job { get; } = job;
        public SafeWaitHandle? Process { get; set; }
        public SafeWaitHandle? Thread { get; set; }
        public bool Transferred { get; set; }
        public bool Assigned { get; set; }
        public WindowsExecutionState? Running { get; set; }

        public string? Cleanup(bool suppressConfirmation = false)
        {
            Thread?.Dispose();
            Thread = null;
            if (Transferred) return null;
            if (Process is not null)
            {
                // Assignment failure is the dangerous case: the job cannot
                // kill an unassigned suspended root. Keep its process handle
                // owned if termination cannot be confirmed.
                bool killed = NativeMethods.TerminateProcess(Process, TerminateExitCode);
                int killError = killed ? 0 : Marshal.GetLastWin32Error();
                if (Assigned) Job.Terminate(TerminateExitCode);
                uint wait = NativeMethods.WaitForSingleObject(Process, 5000);
                if (wait != NativeMethods.WAIT_OBJECT_0 || suppressConfirmation)
                {
                    int waitError = wait == NativeMethods.WAIT_FAILED ? Marshal.GetLastWin32Error() : 0;
                    return $"suspended-root cleanup unconfirmed: TerminateProcess err=0x{killError:X}, wait=0x{wait:X}, waitErr=0x{waitError:X}";
                }
                Process.Dispose();
                Process = null;
            }
            Job.Dispose();
            return null;
        }
    }

    public PlatformCapabilities Describe()
    { lock (_gate) { ObjectDisposedException.ThrowIf(_disposed, this); return WellKnownPlatforms.Windows; } }

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        lock (_gate) ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ExecutionRequirements.TrySnapshot(requirements, out var snapshot, out var inputError))
            return new PrepareResult.Rejected(new[] { inputError });
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.Windows, snapshot!.Required, snapshot.Preferred);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!r.Accepted) return new PrepareResult.Rejected(r.Unmet);
            // Allocation and registration are one gate-owned transition.
            var (job, error) = OwnedJob.Create();
            if (job is null) return new PrepareResult.Rejected(new[] { error });
            var prep = new PreparedExecution(
                WellKnownPlatforms.Windows.Platform, Guid.NewGuid(), r.Met);
            _preparations.Add(prep.PreparationId, job);
            _issued.Add(prep, new StrongBox<byte>(0));
            _activeTokens.Add(prep.PreparationId, prep);
            return new PrepareResult.Accepted(prep);
        }
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (preparation is null || !_issued.TryGetValue(preparation, out var token)
                || token.Value == 1)
                return new DiscardResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            if (token.Value == 2) return new DiscardResult.Discarded(preparation);
            if (_preparations.Remove(preparation.PreparationId, out var job))
                job.Dispose();
            token.Value = 2;
            _activeTokens.Remove(preparation.PreparationId);
            return new DiscardResult.Discarded(preparation);
        }
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        lock (_gate) ObjectDisposedException.ThrowIf(_disposed, this);
        if (!ProcessStartSpec.TrySnapshot(process, windows: true, out var invocation, out var inputError))
            return new LaunchResult.Failed(new[] { inputError });
        LaunchAdmission admission;
        lock (_gate)
        {
            if (_disposed) return new LaunchResult.Failed(new[] { "disposal won before admission" });
            if (prepared is null || !_issued.TryGetValue(prepared, out var token)
                || token.Value != 0
                || !_preparations.Remove(prepared.PreparationId, out var job))
                return new LaunchResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            token.Value = 1;
            _activeTokens.Remove(prepared.PreparationId);
            admission = new LaunchAdmission(job);
            _launches.Add(admission);
        }

        IntPtr environment = IntPtr.Zero;
        LaunchResult result = new LaunchResult.Failed(new[] { "launch did not complete" });
        string? cleanupFailure = null;
        try
        {
            if (string.IsNullOrWhiteSpace(invocation!.Executable))
                result = new LaunchResult.Failed(new[] { "no executable" });
            else if (!TryBuildEnvironment(invocation.Environment, out environment, out string envError))
                result = new LaunchResult.Failed(new[] { envError });
            else
                result = LaunchInner(admission, invocation, environment);
        }
        catch (Exception ex)
        {
            result = new LaunchResult.Failed(new[] { $"launch fault: {ex.GetType().Name}: {ex.Message}" });
        }
        finally
        {
            try
            {
                if (environment != IntPtr.Zero) Marshal.FreeHGlobal(environment);
                cleanupFailure = admission.Cleanup(TakeCleanupFaultForTest());
            }
            catch (Exception ex) { cleanupFailure = $"launch cleanup fault: {ex.GetType().Name}: {ex.Message}"; }
            finally
            {
                lock (_gate)
                {
                    if (cleanupFailure is not null) _failedAdmissions.Add(admission);
                    _launches.Remove(admission);
                    if (_launches.Count == 0) _launchesDrained?.TrySetResult();
                }
            }
        }
        if (cleanupFailure is null) return result;
        admission.Running?.Stop();
        return new LaunchResult.Failed(new[] { cleanupFailure });
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        WindowsExecutionState? state;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (execution.Provider != WellKnownPlatforms.Windows.Platform)
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
        Task<CompletionResult> terminal;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (execution.Provider != WellKnownPlatforms.Windows.Platform)
                return new ValueTask<CompletionResult>(new CompletionResult.Failed(
                    new[] { "unknown execution: not issued by this provider" }));
            if (_executions.TryGetValue(execution.ExecutionId, out var state))
                terminal = state.Completion;
            else if (!_completed.TryGetValue(execution.ExecutionId, out var completed)
                || !completed.Handle.TryGetTarget(out _))
                return new ValueTask<CompletionResult>(new CompletionResult.Failed(
                    new[] { "unknown execution: not issued by this provider" }));
            else terminal = completed.Result;
        }
        // Cancellation applies only to this observation. The execution's
        // terminal task, root exit and cleanup remain owned by its state.
        return new ValueTask<CompletionResult>(terminal.WaitAsync(cancellationToken));
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposeTask is not null)
            {
                // A native cleanup fault retains the failed admission. A
                // later disposal call may retry that still-owned resource;
                // the first call never reports successful cleanup.
                if (_disposeTask.IsFaulted && _failedAdmissions.Count > 0)
                    _disposeTask = RetryFailedCleanupsAsync();
                return new ValueTask(_disposeTask);
            }
            _disposed = true; // disposal's admission linearization point
            var prepared = _preparations.Values.ToArray();
            _preparations.Clear();
            _activeTokens.Clear();
            var running = _executions.Values.ToArray();
            if (_launches.Count > 0)
                _launchesDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = DisposeCoreAsync(prepared, running, _launchesDrained?.Task);
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync(OwnedJob[] prepared,
        WindowsExecutionState[] running, Task? launchesDrained)
    {
        await Task.Yield();
        foreach (var job in prepared) job.Dispose();
        foreach (var state in running) state.Stop();
        if (launchesDrained is not null) await launchesDrained.ConfigureAwait(false);
        var outcomes = await Task.WhenAll(running.Select(state => state.Completion)).ConfigureAwait(false);
        await CleanupFailedAdmissionsAsync().ConfigureAwait(false);
        if (outcomes.OfType<CompletionResult.Failed>().Any())
            throw new InvalidOperationException("Windows execution observation or cleanup failed during disposal");
    }

    private async Task RetryFailedCleanupsAsync()
    {
        await Task.Yield();
        await CleanupFailedAdmissionsAsync().ConfigureAwait(false);
    }

    private Task CleanupFailedAdmissionsAsync()
    {
        LaunchAdmission[] failed;
        lock (_gate) failed = _failedAdmissions.ToArray();
        var errors = new List<string>();
        foreach (var admission in failed)
        {
            string? error = admission.Cleanup(TakeCleanupFaultForTest());
            if (error is not null) errors.Add(error);
            else { lock (_gate) _failedAdmissions.Remove(admission); }
        }
        if (errors.Count != 0)
            throw new InvalidOperationException("Windows launch cleanup unconfirmed: " + string.Join("; ", errors));
        return Task.CompletedTask;
    }

    private LaunchResult LaunchInner(LaunchAdmission admission, ProcessStartSpec process, IntPtr environment)
    {
        var commandLine = new StringBuilder(32768);
        if (process.ArgumentVector is null)
            commandLine.Append('"').Append(process.Executable).Append("\" ").Append(process.Arguments);
        else
            commandLine.Append(WindowsCommandLine.Serialize(
                new[] { process.Executable }.Concat(process.ArgumentVector)));
        var si = new NativeMethods.StartupInfo { cb = Marshal.SizeOf<NativeMethods.StartupInfo>() };
        if (!NativeMethods.CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, false,
            NativeMethods.CREATE_SUSPENDED | NativeMethods.CREATE_UNICODE_ENVIRONMENT,
            environment, string.IsNullOrWhiteSpace(process.WorkingDirectory) ? null : process.WorkingDirectory,
            ref si, out var pi))
        {
            int error = Marshal.GetLastWin32Error();
            return new LaunchResult.Failed(new[] { $"CreateProcess err=0x{error:X} (no target ran)" });
        }

        // Own both raw handles immediately. If wrapping one throws, the
        // admission's finally path closes the wrapped one and the other raw.
        try { admission.Process = new SafeWaitHandle(pi.hProcess, ownsHandle: true); }
        catch { NativeMethods.CloseHandle(pi.hProcess); NativeMethods.CloseHandle(pi.hThread); throw; }
        try { admission.Thread = new SafeWaitHandle(pi.hThread, ownsHandle: true); }
        catch { NativeMethods.CloseHandle(pi.hThread); throw; }

        int assignError = 6;
        if (FailAssignmentForTest || !admission.Job.TryAssign(admission.Process, out assignError))
            return new LaunchResult.Failed(new[] {
                $"assign rejected err=0x{(FailAssignmentForTest ? 6 : assignError):X}; suspended target cleanup owned" });

        admission.Assigned = true;

        lock (_gate)
        {
            if (_disposed)
                return new LaunchResult.Failed(new[] { "disposal won before target release" });
            if (FailResumeForTest || NativeMethods.ResumeThread(admission.Thread) == uint.MaxValue)
            {
                int error = FailResumeForTest ? 6 : Marshal.GetLastWin32Error();
                return new LaunchResult.Failed(new[] { $"resume fault err=0x{error:X}; target cleanup owned" });
            }
            var handle = new ExecutionHandle(WellKnownPlatforms.Windows.Platform, Guid.NewGuid());
            var state = new WindowsExecutionState(handle, admission.Job, admission.Process, OnTerminal);
            _executions.Add(handle.ExecutionId, state);
            admission.Transferred = true;
            admission.Running = state;
            admission.Process = null;
            state.Start();
            return new LaunchResult.Started(handle);
        }
    }

    private void OnTerminal(WindowsExecutionState state, CompletionResult result)
    {
        lock (_gate)
        {
            if (_executions.TryGetValue(state.Handle.ExecutionId, out var current)
                && ReferenceEquals(current, state))
                _executions.Remove(state.Handle.ExecutionId);
            // Weak-keyed retention permits repeated waits on the issued
            // handle without retaining every past execution indefinitely.
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

    private bool TakeCleanupFaultForTest()
    {
        while (true)
        {
            int remaining = Volatile.Read(ref CleanupConfirmationFailuresForTest);
            if (remaining <= 0) return false;
            if (Interlocked.CompareExchange(ref CleanupConfirmationFailuresForTest,
                remaining - 1, remaining) == remaining) return true;
        }
    }

    internal static bool TryAssign(OwnedJob job, IntPtr process, out int error) =>
        job.TryAssign(process, out error);

    /// <summary>Encode only explicitly requested environment entries.</summary>
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
            if (block != IntPtr.Zero) Marshal.FreeHGlobal(block);
            block = IntPtr.Zero;
            error = $"environment encode fault: {ex.GetType().Name}";
            return false;
        }
    }
}
