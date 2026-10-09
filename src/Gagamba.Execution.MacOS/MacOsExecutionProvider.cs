namespace Gagamba.Execution.MacOS;

/// <summary>Launchd lifecycle provider; same-process-group scope only.</summary>
public sealed class MacOsExecutionProvider : IExecutionProvider
{
    private readonly object _gate = new();
    private readonly string _domain;
    private readonly ILaunchdTransport _transport;
    private bool _disposed;
    private Task? _disposeTask;
    private TaskCompletionSource? _launchesDrained;
    private readonly Dictionary<Guid, OwnedJob> _preparations = new();
    private readonly HashSet<OwnedJob> _launches = new();
    private readonly HashSet<OwnedJob> _failedJobs = new();
    private readonly Dictionary<Guid, MacExecutionState> _executions = new();
    private readonly Dictionary<Guid, (WeakReference<ExecutionHandle> Handle, Task<CompletionResult> Result)> _completed = new();
    private readonly List<string> _terminalFailures = new();
    private int _completedSincePrune;

    public MacOsExecutionProvider(string? domain = null)
        : this(domain, new ProcessLaunchdTransport()) { }

    internal MacOsExecutionProvider(string? domain, ILaunchdTransport transport)
    {
        _domain = string.IsNullOrWhiteSpace(domain) ? Launchd.UserDomain() : domain!;
        _transport = transport;
    }

    public PlatformCapabilities Describe() => WellKnownPlatforms.MacOs;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.MacOs, requirements.Required, requirements.Preferred);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!r.Accepted) return new PrepareResult.Rejected(r.Unmet);
            var check = _transport.Run("print", _domain);
            if (!check.Ok)
                return new PrepareResult.Rejected(new[] {
                    $"launchd domain '{_domain}' unavailable: {check.Diagnostic}" });
            OwnedJob job;
            try { job = OwnedJob.Create(_domain, _transport); }
            catch (Exception ex)
            { return new PrepareResult.Rejected(new[] { $"job directory unavailable: {ex.GetType().Name}: {ex.Message}" }); }
            var prep = new PreparedExecution(WellKnownPlatforms.MacOs.Platform, Guid.NewGuid(), r.Met);
            _preparations.Add(prep.PreparationId, job);
            return new PrepareResult.Accepted(prep);
        }
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        OwnedJob? job;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (preparation.Provider != WellKnownPlatforms.MacOs.Platform)
                return new DiscardResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            _preparations.Remove(preparation.PreparationId, out job);
        }
        if (job is not null)
        {
            var cleanup = job.Cleanup();
            if (!cleanup.Ok)
            {
                lock (_gate) _failedJobs.Add(job);
                return new DiscardResult.Failed(new[] { cleanup.Error });
            }
        }
        return new DiscardResult.Discarded(preparation);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        OwnedJob job;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (prepared.Provider != WellKnownPlatforms.MacOs.Platform
                || !_preparations.Remove(prepared.PreparationId, out job!))
                return new LaunchResult.Failed(new[] { "unknown preparation: not issued by this provider" });
            _launches.Add(job);
        }
        LaunchResult result = new LaunchResult.Failed(new[] { "launch did not complete" });
        string? cleanupFailure = null;
        try
        {
            if (string.IsNullOrWhiteSpace(process.Executable))
                result = new LaunchResult.Failed(new[] { "no executable" });
            else if (!TryBuildEnvironment(process.Environment, out var env, out string envError))
                result = new LaunchResult.Failed(new[] { envError });
            else
                result = LaunchInner(job, process, env!);
        }
        catch (Exception ex)
        { result = new LaunchResult.Failed(new[] { $"launch fault: {ex.GetType().Name}: {ex.Message}" }); }
        finally
        {
            if (result is not LaunchResult.Started)
            {
                var cleanup = job.Cleanup();
                if (!cleanup.Ok) { cleanupFailure = cleanup.Error; lock (_gate) _failedJobs.Add(job); }
            }
            lock (_gate)
            {
                _launches.Remove(job);
                if (_launches.Count == 0) _launchesDrained?.TrySetResult();
            }
        }
        return cleanupFailure is null ? result : new LaunchResult.Failed(new[] { cleanupFailure });
    }

    private LaunchResult LaunchInner(OwnedJob job, ProcessStartSpec process,
        IReadOnlyDictionary<string, string> env)
    {
        job.WritePlist(process.Executable, Launchd.SplitArguments(process.Arguments),
            process.WorkingDirectory, env);
        var bootstrap = _transport.Run("bootstrap", _domain, job.PlistPath);
        if (!bootstrap.Ok) return new LaunchResult.Failed(new[] { "bootstrap refused: " + bootstrap.Diagnostic });
        job.MarkLoaded();
        HelperResult kickstart;
        lock (_gate)
        {
            if (_disposed) return new LaunchResult.Failed(new[] { "disposal won before target release" });
            kickstart = _transport.Run("kickstart", job.ServiceTarget);
        }
        if (!kickstart.Ok) return new LaunchResult.Failed(new[] { "kickstart refused: " + kickstart.Diagnostic });
        string? readiness = WaitRunning(job, TimeSpan.FromSeconds(15));
        if (readiness is not null) return new LaunchResult.Failed(new[] { readiness });
        lock (_gate)
        {
            if (_disposed) return new LaunchResult.Failed(new[] { "disposal won during launch readiness" });
            var handle = new ExecutionHandle(WellKnownPlatforms.MacOs.Platform, Guid.NewGuid());
            var state = new MacExecutionState(handle, job, OnTerminal);
            _executions.Add(handle.ExecutionId, state);
            state.Start();
            return new LaunchResult.Started(handle);
        }
    }

    private static string? WaitRunning(OwnedJob job, TimeSpan timeout)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (timer.Elapsed < timeout)
        {
            JobObservation seen = job.Observe();
            if (seen.Kind is JobObservationKind.Running or JobObservationKind.Terminal) return null;
            if (seen.Kind == JobObservationKind.Unknown
                && !seen.Reason.Contains("exit status", StringComparison.Ordinal))
                return "readiness observation failed: " + seen.Reason;
            Thread.Sleep(100);
        }
        return "job never reached running or recorded exit within readiness deadline";
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        MacExecutionState? state;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (execution.Provider != WellKnownPlatforms.MacOs.Platform)
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
            if (execution.Provider != WellKnownPlatforms.MacOs.Platform)
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
                if (_disposeTask.IsFaulted && (_failedJobs.Count > 0 || _executions.Count > 0))
                    _disposeTask = RetryFailedAsync();
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

    private async Task DisposeCoreAsync(OwnedJob[] prepared, Task? launchesDrained)
    {
        await Task.Yield();
        var errors = new List<string>();
        foreach (var job in prepared)
        {
            var cleanup = job.Cleanup();
            if (!cleanup.Ok) { errors.Add(cleanup.Error); lock (_gate) _failedJobs.Add(job); }
        }
        if (launchesDrained is not null) await launchesDrained.ConfigureAwait(false);
        MacExecutionState[] states;
        lock (_gate) states = _executions.Values.ToArray();
        var join = new List<MacExecutionState>();
        foreach (var state in states)
        {
            if (state.Stop() is TerminateResult.Failed failure && !state.Completion.IsCompleted)
            { errors.AddRange(failure.Reasons); lock (_gate) _failedJobs.Add(state.Job); }
            else join.Add(state);
        }
        var results = await Task.WhenAll(join.Select(state => state.Completion)).ConfigureAwait(false);
        foreach (var failed in results.OfType<CompletionResult.Failed>()) errors.AddRange(failed.Reasons);
        lock (_gate) errors.AddRange(_terminalFailures);
        await CleanupFailedAsync(errors).ConfigureAwait(false);
        if (errors.Count > 0)
            throw new InvalidOperationException("macOS cleanup/observation unconfirmed: " + string.Join("; ", errors));
    }

    private async Task RetryFailedAsync()
    {
        await Task.Yield();
        var errors = new List<string>();
        MacExecutionState[] states;
        lock (_gate) states = _executions.Values.ToArray();
        var join = new List<MacExecutionState>();
        foreach (var state in states)
        {
            // Observation failure is already delivered to the original waiters.
            // A disposal retry only needs to establish cleanup, not rewrite that result.
            if (state.Completion.IsCompleted) continue;
            if (state.Stop() is TerminateResult.Failed failure && !state.Completion.IsCompleted)
                errors.AddRange(failure.Reasons);
            else join.Add(state);
        }
        var results = await Task.WhenAll(join.Select(state => state.Completion)).ConfigureAwait(false);
        foreach (var failed in results.OfType<CompletionResult.Failed>()) errors.AddRange(failed.Reasons);
        await CleanupFailedAsync(errors).ConfigureAwait(false);
        if (errors.Count > 0)
            throw new InvalidOperationException("macOS cleanup unconfirmed: " + string.Join("; ", errors));
    }

    private Task CleanupFailedAsync(List<string> errors)
    {
        OwnedJob[] failed;
        lock (_gate) failed = _failedJobs.ToArray();
        foreach (var job in failed)
        {
            var cleanup = job.Cleanup();
            if (!cleanup.Ok) errors.Add(cleanup.Error);
            else lock (_gate) _failedJobs.Remove(job);
        }
        return Task.CompletedTask;
    }

    private void OnTerminal(MacExecutionState state, CompletionResult result)
    {
        lock (_gate)
        {
            if (result is CompletionResult.Failed failure)
            { _failedJobs.Add(state.Job); _terminalFailures.AddRange(failure.Reasons); }
            else _executions.Remove(state.Handle.ExecutionId);
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

    internal static bool TryBuildEnvironment(IReadOnlyDictionary<string, string> spec,
        out IReadOnlyDictionary<string, string>? env, out string error)
    {
        env = null; error = "";
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in spec.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(kv.Key) || kv.Key.Contains('=') || kv.Key.Contains('\0'))
            { error = $"invalid environment name '{kv.Key}' (empty, '=' or NUL)"; return false; }
            if (kv.Value is null || kv.Value.Contains('\0'))
            { error = $"NUL or null value of '{kv.Key}'"; return false; }
            if (!seen.Add(kv.Key))
            { error = $"duplicate environment name '{kv.Key}'"; return false; }
            clean[kv.Key] = kv.Value;
        }
        env = clean;
        return true;
    }
}
