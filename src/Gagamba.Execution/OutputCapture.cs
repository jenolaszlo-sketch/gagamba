using System.Buffers;

namespace Gagamba.Execution;

/// <summary>Byte budgets for stdout and stderr. Limits apply to retained data;
/// both streams continue draining after overflow to avoid blocking writers.
/// Retention is opt-in and never appears in the default orchestration receipt.</summary>
public sealed record OutputCaptureOptions(int StdoutBytes, int StderrBytes, int TotalBytes,
    bool RetainBytes = false, TimeSpan? PipeGrace = null)
{
    public const int MaxAllowedBytes = 16 * 1024 * 1024;
    public bool TryValidate(out string error)
    {
        error = "";
        if (StdoutBytes < 0 || StderrBytes < 0 || TotalBytes < 0
            || StdoutBytes > MaxAllowedBytes || StderrBytes > MaxAllowedBytes
            || TotalBytes > MaxAllowedBytes * 2)
        { error = "output budgets must be nonnegative and within the fixed memory ceiling"; return false; }
        if (PipeGrace is { } grace && (grace <= TimeSpan.Zero || grace > TimeSpan.FromMinutes(1)))
        { error = "pipe grace must be positive and at most one minute"; return false; }
        return true;
    }
}

public enum OutputCaptureStatus { Complete, Overflow, DrainIncomplete, ReadFailed }

public sealed record OutputCaptureResult(OutputCaptureStatus Status,
    long StdoutBytesSeen, long StderrBytesSeen,
    ReadOnlyMemory<byte> StdoutRetained, ReadOnlyMemory<byte> StderrRetained);

public abstract record CaptureLaunchResult
{
    private CaptureLaunchResult() { }
    public sealed record Started(ExecutionHandle Handle, OutputCaptureSession Capture) : CaptureLaunchResult;
    public sealed record Failed(IReadOnlyList<string> Reasons) : CaptureLaunchResult;
}

/// <summary>Optional provider capability. Unsupported capture must fail
/// before consuming a preparation or releasing a target.</summary>
public interface IOutputCaptureProvider
{
    CaptureLaunchResult LaunchCaptured(PreparedExecution prepared,
        ProcessStartSpec process, OutputCaptureOptions options);
}

/// <summary>Owns two concurrent byte drains and their finite post-completion
/// grace. It never retains more than the configured budgets.</summary>
public sealed class OutputCaptureSession
{
    private readonly object _gate = new();
    private readonly Stream _stdout;
    private readonly Stream _stderr;
    private readonly OutputCaptureOptions _options;
    private readonly TaskCompletionSource _overflow =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _abort =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _totalSeen;
    private long _stdoutSeen;
    private long _stderrSeen;
    private bool _overflowed;

    public Task Overflowed => _overflow.Task;
    public Task<OutputCaptureResult> Completion { get; }

    /// <summary>Stops ownership of pipes when lifecycle observation cannot
    /// reach a terminal state inside the caller's bounded cleanup window.</summary>
    public void Abort() => _abort.TrySetResult();

    public OutputCaptureSession(Stream stdout, Stream stderr,
        Task<CompletionResult> providerCompletion, OutputCaptureOptions options)
    {
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);
        ArgumentNullException.ThrowIfNull(providerCompletion);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.TryValidate(out string error)) throw new ArgumentException(error, nameof(options));
        _stdout = stdout;
        _stderr = stderr;
        _options = options;
        Completion = CollectAsync(providerCompletion);
    }

    private async Task<OutputCaptureResult> CollectAsync(Task<CompletionResult> providerCompletion)
    {
        Task<DrainResult> stdout = DrainAsync(_stdout, _options.StdoutBytes, isStdout: true);
        Task<DrainResult> stderr = DrainAsync(_stderr, _options.StderrBytes, isStdout: false);
        try
        {
            try
            {
                Task first = await Task.WhenAny(providerCompletion, _abort.Task).ConfigureAwait(false);
                if (first == providerCompletion) await providerCompletion.ConfigureAwait(false);
                else
                {
                    _stdout.Dispose(); _stderr.Dispose();
                    return new OutputCaptureResult(OutputCaptureStatus.DrainIncomplete,
                        _stdoutSeen, _stderrSeen, ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty);
                }
            }
            catch { /* The output status remains separate from lifecycle failure. */ }
            var both = Task.WhenAll(stdout, stderr);
            try { await both.WaitAsync(_options.PipeGrace ?? TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
            catch (TimeoutException)
            {
                _stdout.Dispose(); _stderr.Dispose();
                return new OutputCaptureResult(OutputCaptureStatus.DrainIncomplete,
                    _stdoutSeen, _stderrSeen, ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty);
            }
            catch (Exception)
            {
                return new OutputCaptureResult(OutputCaptureStatus.ReadFailed,
                    _stdoutSeen, _stderrSeen, ReadOnlyMemory<byte>.Empty, ReadOnlyMemory<byte>.Empty);
            }
            DrainResult outResult = await stdout.ConfigureAwait(false);
            DrainResult errResult = await stderr.ConfigureAwait(false);
            return new OutputCaptureResult(_overflowed ? OutputCaptureStatus.Overflow : OutputCaptureStatus.Complete,
                outResult.Seen, errResult.Seen, outResult.Retained, errResult.Retained);
        }
        finally { _stdout.Dispose(); _stderr.Dispose(); }
    }

    private async Task<DrainResult> DrainAsync(Stream stream, int streamLimit, bool isStdout)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        using var retained = new MemoryStream();
        long seen = 0;
        try
        {
            while (true)
            {
                int count = await stream.ReadAsync(buffer.AsMemory(0, 8192)).ConfigureAwait(false);
                if (count == 0) break;
                seen += count;
                lock (_gate)
                {
                    long before = _totalSeen;
                    _totalSeen += count;
                    if (isStdout) _stdoutSeen += count; else _stderrSeen += count;
                    if (seen > streamLimit || _totalSeen > _options.TotalBytes)
                    { _overflowed = true; _overflow.TrySetResult(); }
                    if (_options.RetainBytes)
                    {
                        long room = Math.Min(streamLimit - retained.Length,
                            _options.TotalBytes - before);
                        if (room > 0) retained.Write(buffer, 0, (int)Math.Min(room, count));
                    }
                }
            }
            return new DrainResult(seen,
                _options.RetainBytes ? retained.ToArray() : ReadOnlyMemory<byte>.Empty);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private sealed record DrainResult(long Seen, ReadOnlyMemory<byte> Retained);
}
