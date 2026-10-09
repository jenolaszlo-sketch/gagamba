namespace Gagamba.Execution.MacOS;

// One task owns the terminal observation. Waiter cancellation never stops it.
internal sealed class MacExecutionState(ExecutionHandle handle, OwnedJob job,
    Action<MacExecutionState, CompletionResult> onTerminal)
{
    private readonly object _gate = new();
    private readonly TaskCompletionSource<CompletionResult> _terminal =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _stopRequested;
    public ExecutionHandle Handle { get; } = handle;
    public OwnedJob Job { get; } = job;
    public Task<CompletionResult> Completion => _terminal.Task;

    public void Start() => _ = ObserveAsync();

    public TerminateResult Stop()
    {
        lock (_gate)
        {
            if (_stopRequested) return new TerminateResult.Terminated(Handle);
            var cleanup = Job.Cleanup();
            if (!cleanup.Ok) return new TerminateResult.Failed(new[] { cleanup.Error });
            _stopRequested = true;
            return new TerminateResult.Terminated(Handle);
        }
    }

    private async Task ObserveAsync()
    {
        CompletionResult result;
        try
        {
            while (true)
            {
                JobObservation observation = Job.Observe();
                if (observation.Kind == JobObservationKind.Running)
                { await Task.Delay(200).ConfigureAwait(false); continue; }
                if (observation.Kind == JobObservationKind.Unknown)
                { result = new CompletionResult.Failed(new[] { "launchd observation failed: " + observation.Reason }); break; }
                if (observation.Kind == JobObservationKind.Terminal)
                {
                    var cleanup = Job.Cleanup();
                    if (!cleanup.Ok)
                        result = new CompletionResult.Failed(new[] { cleanup.Error });
                    else
                    {
                        bool stopped;
                        lock (_gate) stopped = _stopRequested;
                        result = stopped ? new CompletionResult.Terminated()
                            : new CompletionResult.NaturalExit(observation.ExitCode!.Value);
                    }
                    break;
                }
                bool requested;
                lock (_gate) requested = _stopRequested;
                result = requested ? new CompletionResult.Terminated()
                    : new CompletionResult.Failed(new[] { "launchd job absent without recorded terminal status" });
                break;
            }
        }
        catch (Exception ex)
        { result = new CompletionResult.Failed(new[] { $"launchd observation fault: {ex.GetType().Name}: {ex.Message}" }); }
        _terminal.TrySetResult(result);
        onTerminal(this, result);
    }
}
