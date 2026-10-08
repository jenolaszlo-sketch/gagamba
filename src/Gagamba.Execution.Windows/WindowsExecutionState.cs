using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Windows;

// Exactly one owner for the running job, root handle, termination intent and
// terminal outcome. Waiters only observe Completion; none can reap or close it.
internal sealed class WindowsExecutionState
{
    private readonly object _gate = new();
    private readonly OwnedJob _job;
    private readonly SafeWaitHandle _process;
    private readonly Action<WindowsExecutionState, CompletionResult> _onTerminal;
    private readonly TaskCompletionSource<CompletionResult> _terminal =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _stopping;
    private bool _finished;
    private string? _failure;
    private CompletionResult? _result;

    public WindowsExecutionState(ExecutionHandle handle, OwnedJob job,
        SafeWaitHandle process, Action<WindowsExecutionState, CompletionResult> onTerminal)
    {
        Handle = handle;
        _job = job;
        _process = process;
        _onTerminal = onTerminal;
    }

    public ExecutionHandle Handle { get; }
    public Task<CompletionResult> Completion => _terminal.Task;

    public void Start() => _ = ObserveGuardedAsync();

    private async Task ObserveGuardedAsync()
    {
        try { await ObserveAsync().ConfigureAwait(false); }
        catch (Exception ex)
        {
            // A callback or native interop exception must not strand disposal
            // waiting on a completion task that nobody will ever settle.
            Finish(new CompletionResult.Failed(new[] { $"observer fault: {ex.GetType().Name}: {ex.Message}" }));
        }
    }

    public TerminateResult Stop()
    {
        lock (_gate)
        {
            if (_finished) return _result is CompletionResult.Terminated
                ? new TerminateResult.Terminated(Handle)
                : new TerminateResult.Failed(new[] { "execution already terminal without confirmed termination" });
            if (_stopping) return _failure is null
                ? new TerminateResult.Terminated(Handle)
                : new TerminateResult.Failed(new[] { _failure });
            _stopping = true;
            (bool ok, string error) request;
            try { request = _job.Terminate(WindowsExecutionProvider.TerminateExitCode); }
            catch (Exception ex) { request = (false, $"TerminateJobObject fault: {ex.GetType().Name}"); }
            var (ok, error) = request;
            if (!ok)
            {
                _failure = error;
                // Kill-on-close is the independent fallback. The observer
                // retains the root process handle until stop is observed.
                _job.Dispose();
                return new TerminateResult.Failed(new[] { error });
            }
            return new TerminateResult.Terminated(Handle);
        }
    }

    private async Task ObserveAsync()
    {
        // No native polling before the first yield. The state, not a caller,
        // owns this observer and its SafeHandles until terminal publication.
        await Task.Yield();
        var failureTimer = new Stopwatch();
        while (true)
        {
            CompletionResult? result = null;
            lock (_gate)
            {
                if (_failure is not null)
                {
                    if (!failureTimer.IsRunning) failureTimer.Start();
                    uint wait = NativeMethods.WaitForSingleObject(_process, 0);
                    if (wait == NativeMethods.WAIT_OBJECT_0 || failureTimer.Elapsed > TimeSpan.FromSeconds(5))
                        result = new CompletionResult.Failed(new[] { _failure,
                            wait == NativeMethods.WAIT_OBJECT_0 ? "root stopped after fallback" : "root stop unconfirmed" });
                }
                else
                {
                    uint wait = NativeMethods.WaitForSingleObject(_process, 0);
                    if (wait == NativeMethods.WAIT_FAILED)
                    {
                        _failure = $"WaitForSingleObject err=0x{Marshal.GetLastWin32Error():X}";
                        _job.Dispose();
                    }
                    else
                    {
                        bool rootExited = wait == NativeMethods.WAIT_OBJECT_0;
                        uint exitCode = 0;
                        if (rootExited && !NativeMethods.GetExitCodeProcess(_process, out exitCode))
                        {
                            _failure = $"GetExitCodeProcess err=0x{Marshal.GetLastWin32Error():X}";
                            _job.Dispose();
                        }
                        else
                        {
                            var count = _job.ActiveProcessCount();
                            if (!count.Ok)
                            {
                                _failure = count.Error;
                                _job.Dispose();
                            }
                            else if (rootExited && count.Active == 0)
                                result = _stopping
                                    ? new CompletionResult.Terminated()
                                    : new CompletionResult.NaturalExit(unchecked((int)exitCode));
                        }
                    }
                }
            }
            if (result is not null)
            {
                Finish(result);
                return;
            }
            await Task.Delay(25).ConfigureAwait(false);
        }
    }

    private void Finish(CompletionResult result)
    {
        lock (_gate)
        {
            if (_finished) return;
            _finished = true;
            try { _job.Dispose(); }
            catch (Exception ex) { result = new CompletionResult.Failed(new[] { $"job cleanup: {ex.GetType().Name}" }); }
            try { _process.Dispose(); }
            catch (Exception ex) { result = new CompletionResult.Failed(new[] { $"process cleanup: {ex.GetType().Name}" }); }
            _result = result;
        }
        try { _onTerminal(this, result); }
        finally { _terminal.TrySetResult(result); }
    }
}
