using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using Gagamba.Execution;

namespace Gagamba.Runtime;

/// <summary>Why orchestration stopped waiting for an execution. A provider
/// observation failure never becomes proof that cleanup completed.</summary>
public enum RunCause { NaturalExit, NonzeroExit, CallerCancellation, Deadline, ExplicitStop, OutputOverflow, PrepareFailure, LaunchFailure, ObservationFailure }
public enum CleanupStatus { Confirmed, Failed, Unknown }

/// <summary>Small redacted receipt. Arguments, environment values and output
/// are never retained here. The fingerprint uses a process-local secret key.</summary>
public sealed record RunReceipt(Guid CorrelationId, string Platform, string InvocationFingerprint,
    DateTimeOffset StartedAt, DateTimeOffset CompletedAt, RunCause Cause,
    int? RootExitCode, CleanupStatus Cleanup, bool StopRequestAccepted,
    IReadOnlyList<string> Reasons, OutputCaptureResult? Output = null,
    IReadOnlyList<string>? EffectiveCapabilities = null)
{
    public TimeSpan Duration => CompletedAt - StartedAt;
}

public sealed record RunOptions(TimeSpan? Deadline = null,
    TimeSpan? StopGrace = null, OutputCaptureOptions? Capture = null);

/// <summary>Runs one invocation over the existing provider lifecycle. Caller
/// cancellation and explicit stop each request termination; the underlying
/// WaitForCompletionAsync token remains observation-only.</summary>
public static class ExecutionOrchestrator
{
    private static readonly byte[] FingerprintKey = RandomNumberGenerator.GetBytes(32);

    public static async Task<RunReceipt> RunAsync(IExecutionProvider provider,
        ExecutionRequirements requirements, ProcessStartSpec process,
        RunOptions? options = null, CancellationToken callerCancellation = default,
        CancellationToken explicitStop = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var elapsed = Stopwatch.StartNew();
        options ??= new RunOptions();
        if (options.Deadline is { } deadline && deadline < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "deadline must be nonnegative");
        TimeSpan grace = options.StopGrace ?? TimeSpan.FromSeconds(5);
        if (grace <= TimeSpan.Zero || grace > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(options), "stop grace must be positive and at most one minute");
        if (options.Capture is { } requestedCapture && !requestedCapture.TryValidate(out string captureError))
            throw new ArgumentException(captureError, nameof(options));
        if (!ExecutionRequirements.TrySnapshot(requirements, out var req, out var reqError))
            return Failed(RunCause.PrepareFailure, "invalid requirements");
        if (!ProcessStartSpec.TrySnapshot(process, OperatingSystem.IsWindows(), out var spec, out var specError))
            return Failed(RunCause.LaunchFailure, "invalid process specification");

        Guid correlation = Guid.NewGuid();
        DateTimeOffset started = DateTimeOffset.UtcNow;
        string platform = provider.Describe().Platform;
        string fingerprint = Fingerprint(spec!);
        IReadOnlyList<string>? effective = null;
        RunReceipt Receipt(RunCause cause, int? exit, CleanupStatus cleanup,
            bool stopAccepted, OutputCaptureResult? output = null, params string[] reasons) =>
            new(correlation, platform, fingerprint, started, DateTimeOffset.UtcNow,
                cause, exit, cleanup, stopAccepted,
                Array.AsReadOnly(reasons.Where(r => r.Length != 0).ToArray()), output, effective);
        RunReceipt Failed(RunCause cause, string reason) =>
            new(Guid.NewGuid(), "unselected", "", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                cause, null, CleanupStatus.Confirmed, false, Array.AsReadOnly(new[] { reason }));

        if (callerCancellation.IsCancellationRequested)
            return Receipt(RunCause.CallerCancellation, null, CleanupStatus.Confirmed, false);
        PrepareResult prep = provider.Prepare(req!);
        if (prep is PrepareResult.Rejected rejected)
            return Receipt(RunCause.PrepareFailure, null, CleanupStatus.Confirmed, false,
                null, "prepare rejected");
        var token = ((PrepareResult.Accepted)prep).Prepared;
        effective = Array.AsReadOnly(token.Met.ToArray());
        if (callerCancellation.IsCancellationRequested || explicitStop.IsCancellationRequested)
        {
            DiscardResult discarded = provider.Discard(token);
            return Receipt(callerCancellation.IsCancellationRequested
                    ? RunCause.CallerCancellation : RunCause.ExplicitStop,
                null, discarded is DiscardResult.Discarded ? CleanupStatus.Confirmed : CleanupStatus.Failed,
                false, null, discarded is DiscardResult.Failed ? "discard failed" : "");
        }
        OutputCaptureSession? capture = null;
        ExecutionHandle handle;
        if (options.Capture is { } captureOptions)
        {
            if (provider is not IOutputCaptureProvider captureProvider)
            {
                var discard = provider.Discard(token);
                return Receipt(RunCause.LaunchFailure, null,
                    discard is DiscardResult.Discarded ? CleanupStatus.Confirmed : CleanupStatus.Failed,
                    false, null, "capture unsupported");
            }
            CaptureLaunchResult launch = captureProvider.LaunchCaptured(token, spec!, captureOptions);
            if (launch is CaptureLaunchResult.Failed)
            {
                // Capture rejection is prelaunch by contract. Other failures
                // may have consumed the token or started a target.
                var discard = provider.Discard(token);
                return Receipt(RunCause.LaunchFailure, null,
                    discard is DiscardResult.Discarded ? CleanupStatus.Confirmed : CleanupStatus.Unknown,
                    false, null, "capture launch rejected or failed");
            }
            var startedCapture = (CaptureLaunchResult.Started)launch;
            handle = startedCapture.Handle;
            capture = startedCapture.Capture;
        }
        else
        {
            LaunchResult launch = provider.Launch(token, spec!);
            if (launch is LaunchResult.Failed)
                return Receipt(RunCause.LaunchFailure, null, CleanupStatus.Unknown,
                    false, null, "launch failed");
            handle = ((LaunchResult.Started)launch).Handle;
        }
        async Task<RunReceipt> RecoverObservationFaultAsync()
        {
            capture?.Abort();
            bool accepted = false;
            try { accepted = provider.Terminate(handle) is TerminateResult.Terminated; }
            catch { /* Retain uncertainty when a concurrent dispose owns stop. */ }
            CompletionResult? observed = null;
            try
            {
                observed = await provider.WaitForCompletionAsync(handle).AsTask()
                    .WaitAsync(grace).ConfigureAwait(false);
            }
            catch { /* A failed observer cannot prove domain cleanup. */ }
            OutputCaptureResult? output = null;
            if (capture is not null)
            {
                try { output = await capture.Completion.WaitAsync(grace).ConfigureAwait(false); }
                catch { /* Bounded capture observation is also uncertain. */ }
            }
            return Receipt(RunCause.ObservationFailure,
                observed is CompletionResult.NaturalExit natural ? natural.RootExitCode : null,
                observed is CompletionResult.NaturalExit or CompletionResult.Terminated
                    ? CleanupStatus.Confirmed : CleanupStatus.Unknown,
                accepted, output, "provider observation failed");
        }
        Task<CompletionResult> completion;
        try { completion = provider.WaitForCompletionAsync(handle).AsTask(); }
        catch { return await RecoverObservationFaultAsync().ConfigureAwait(false); }
        using var timers = new CancellationTokenSource();
        Task deadlineTask = options.Deadline is { } span
            ? Task.Delay(span > elapsed.Elapsed ? span - elapsed.Elapsed : TimeSpan.Zero, timers.Token)
            : Task.Delay(Timeout.InfiniteTimeSpan, timers.Token);
        Task cancelTask = Task.Delay(Timeout.InfiniteTimeSpan, callerCancellation);
        Task explicitTask = Task.Delay(Timeout.InfiniteTimeSpan, explicitStop);
        Task overflowTask = capture?.Overflowed ?? Task.Delay(Timeout.InfiniteTimeSpan);
        Task winner = await Task.WhenAny(completion, deadlineTask, cancelTask, explicitTask,
            overflowTask).ConfigureAwait(false);
        timers.Cancel();
        if (completion.IsCompleted || winner == completion)
        {
            CompletionResult observed;
            try { observed = await completion.ConfigureAwait(false); }
            catch { return await RecoverObservationFaultAsync().ConfigureAwait(false); }
            OutputCaptureResult? output = null;
            if (capture is not null)
            {
                try { output = await capture.Completion.WaitAsync(
                    (options.Capture?.PipeGrace ?? TimeSpan.FromSeconds(5)) + TimeSpan.FromSeconds(1))
                    .ConfigureAwait(false); }
                catch (TimeoutException) { capture.Abort(); }
                if (output is null)
                    return Receipt(RunCause.ObservationFailure, null, CleanupStatus.Unknown,
                        false, null, "output observation timed out");
            }
            if (output?.Status == OutputCaptureStatus.Overflow)
                return Receipt(RunCause.OutputOverflow,
                    observed is CompletionResult.NaturalExit exited ? exited.RootExitCode : null,
                    observed is CompletionResult.Failed ? CleanupStatus.Unknown : CleanupStatus.Confirmed,
                    false, output, "output budget exceeded");
            if (output is { Status: OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed })
                return Receipt(RunCause.ObservationFailure, null, CleanupStatus.Unknown,
                    false, output, "output observation incomplete");
            return observed switch
            {
                CompletionResult.NaturalExit natural => Receipt(natural.RootExitCode == 0
                        ? RunCause.NaturalExit : RunCause.NonzeroExit,
                    natural.RootExitCode, CleanupStatus.Confirmed, false, output),
                CompletionResult.Terminated => Receipt(RunCause.ExplicitStop, null,
                    CleanupStatus.Confirmed, true, output),
                CompletionResult.Failed => Receipt(RunCause.ObservationFailure,
                    null, CleanupStatus.Unknown, false, output, "provider observation failed"),
                _ => Receipt(RunCause.ObservationFailure, null, CleanupStatus.Unknown, false, output,
                    "unknown completion result"),
            };
        }
        RunCause stopCause = winner == deadlineTask ? RunCause.Deadline
            : winner == cancelTask ? RunCause.CallerCancellation
            : winner == overflowTask ? RunCause.OutputOverflow : RunCause.ExplicitStop;
        TerminateResult stop;
        try { stop = provider.Terminate(handle); }
        catch (Exception ex)
        {
            capture?.Abort();
            return Receipt(stopCause, null, CleanupStatus.Unknown, false, null,
                $"termination fault: {ex.GetType().Name}");
        }
        bool accepted = stop is TerminateResult.Terminated;
        CompletionResult? terminal = null;
        try { terminal = await completion.WaitAsync(grace).ConfigureAwait(false); }
        catch (TimeoutException) { }
        if (terminal is null) capture?.Abort();
        OutputCaptureResult? stoppedOutput = null;
        if (capture is not null)
        {
            try { stoppedOutput = await capture.Completion.WaitAsync(
                grace + (options.Capture?.PipeGrace ?? TimeSpan.FromSeconds(5)) + TimeSpan.FromSeconds(1))
                .ConfigureAwait(false); }
            catch (TimeoutException) { capture.Abort(); }
        }
        CleanupStatus status = terminal switch
        {
            CompletionResult.NaturalExit or CompletionResult.Terminated => CleanupStatus.Confirmed,
            CompletionResult.Failed => CleanupStatus.Unknown,
            _ => CleanupStatus.Unknown,
        };
        if (stoppedOutput is { Status: OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed })
            status = CleanupStatus.Unknown;
        if (capture is not null && stoppedOutput is null) status = CleanupStatus.Unknown;
        string stopReason = stop is TerminateResult.Failed ? "terminate failed" : "";
        string observationReason = terminal is CompletionResult.Failed
            ? "provider observation failed" : terminal is null ? "terminal observation timed out" : "";
        return Receipt(stopCause, terminal is CompletionResult.NaturalExit naturalResult
            ? naturalResult.RootExitCode : null, status, accepted, stoppedOutput,
            stopReason, observationReason);
    }


    private static string Fingerprint(ProcessStartSpec spec)
    {
        var data = new StringBuilder();
        static void Add(StringBuilder sb, string value) => sb.Append(value.Length).Append(':').Append(value);
        Add(data, spec.Executable);
        Add(data, spec.WorkingDirectory);
        if (spec.ArgumentVector is { } vector)
            foreach (var arg in vector) Add(data, arg);
        else Add(data, spec.Arguments);
        foreach (var pair in spec.Environment.OrderBy(p => p.Key, StringComparer.Ordinal))
        { Add(data, pair.Key); Add(data, pair.Value); }
        return Convert.ToHexString(HMACSHA256.HashData(FingerprintKey,
            Encoding.UTF8.GetBytes(data.ToString())));
    }
}
