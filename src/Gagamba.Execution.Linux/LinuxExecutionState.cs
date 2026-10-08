using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux;

// Sole waitpid owner. Caller cancellation never interrupts this task.
internal sealed class LinuxExecutionState(
    ExecutionHandle handle, OwnedCgroup group, int rootPid,
    Action<LinuxExecutionState, CompletionResult> onTerminal)
{
    private readonly object _gate = new();
    private readonly TaskCompletionSource<CompletionResult> _terminal =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _stopRequested;
    private DateTime _stopAt;
    public ExecutionHandle Handle { get; } = handle;
    public OwnedCgroup Group { get; } = group;
    public int RootPid { get; } = rootPid;
    public Task<CompletionResult> Completion => _terminal.Task;

    public void Start() => _ = ObserveAsync();

    public TerminateResult Stop()
    {
        lock (_gate)
        {
            if (Group.IsRemoved && !_terminal.Task.IsCompleted)
                return new TerminateResult.Failed(new[] { "execution already terminal" });
            if (_terminal.Task.IsCompleted)
                return _terminal.Task.Result is CompletionResult.Terminated
                    ? new TerminateResult.Terminated(Handle)
                    : new TerminateResult.Failed(new[] { "execution already terminal" });
            if (_stopRequested) return new TerminateResult.Terminated(Handle);
            var killed = Group.Kill();
            if (!killed.Ok) return new TerminateResult.Failed(new[] { killed.Error });
            _stopRequested = true;
            _stopAt = DateTime.UtcNow;
            return new TerminateResult.Terminated(Handle);
        }
    }

    private async Task ObserveAsync()
    {
        CompletionResult result;
        try
        {
            int status = 0;
            while (true)
            {
                int rc = NativeMethods.waitpid(RootPid, out status, NativeMethods.WNOHANG);
                if (rc == RootPid) break;
                if (rc < 0)
                {
                    int errno = Marshal.GetLastWin32Error();
                    if (errno == NativeMethods.EINTR) continue;
                    throw new InvalidOperationException(errno == NativeMethods.ECHILD
                        ? "root reaped outside the execution owner (ECHILD)"
                        : $"waitpid root fault errno={errno}");
                }
                await Task.Delay(50).ConfigureAwait(false);
            }
            while (true)
            {
                var population = Group.Population();
                if (population.Error.Length != 0) throw new InvalidOperationException(population.Error);
                if (population.Empty) break;
                // An unbounded descendant is expected during normal waits;
                // disposal and Terminate supply a kill and then a bound.
                DateTime stopAt;
                lock (_gate) stopAt = _stopAt;
                if (stopAt != default && DateTime.UtcNow - stopAt > TimeSpan.FromSeconds(5))
                    throw new TimeoutException("cgroup did not empty after kill");
                await Task.Delay(50).ConfigureAwait(false);
            }
            string? cleanup = await Group.StopAndRemoveAsync(false, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            if (cleanup is not null) throw new InvalidOperationException(cleanup);
            bool terminated;
            lock (_gate) terminated = _stopRequested;
            result = terminated ? new CompletionResult.Terminated()
                : new CompletionResult.NaturalExit((status & 0x7f) == 0
                    ? (status >> 8) & 0xff : 128 + (status & 0x7f));
        }
        catch (Exception ex)
        {
            result = new CompletionResult.Failed(new[] { $"Linux observation/cleanup fault: {ex.GetType().Name}: {ex.Message}" });
        }
        _terminal.TrySetResult(result);
        onTerminal(this, result);
    }
}
