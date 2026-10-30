using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Launch;

/// <summary>Private live HELD state. The control writer is never exposed; it
/// permits exactly one nonblocking 128-byte RELEASE write or an abort close.</summary>
internal sealed class HeldExecution : IDisposable, IReleaseWriteChannel
{
    private SafeFileHandle? _controlWriter;
    private int _releaseAttempted;
    private int _physicalReleaseAttempts;
    internal int PhysicalReleaseAttempts=>Volatile.Read(ref _physicalReleaseAttempts);
    private bool _disposed;

    internal HeldExecution(
        PidFdChildReaper wrapperReaper,
        SafeFileHandle controlWriter,
        SafeFileHandle coordinatorStatusReader,
        SafeFileHandle helperStatusReader,
        SafeFileHandle stdoutReader,
        SafeFileHandle stderrReader,
        int helperChildPid,
        CoordinatorHeldReport report)
    {
        WrapperReaper = wrapperReaper;
        _controlWriter = controlWriter;
        CoordinatorStatusReader = coordinatorStatusReader;
        HelperStatusReader = helperStatusReader;
        StdoutReader = stdoutReader;
        StderrReader = stderrReader;
        HelperChildPid = helperChildPid;
        Report = report;
    }

    internal PidFdChildReaper WrapperReaper { get; }
    internal SafeFileHandle CoordinatorStatusReader { get; }
    internal SafeFileHandle HelperStatusReader { get; }
    internal SafeFileHandle StdoutReader { get; }
    internal SafeFileHandle StderrReader { get; }
    internal int HelperChildPid { get; }
    internal CoordinatorHeldReport Report { get; }

    /// <summary>True once the sole physical RELEASE write was attempted. It is
    /// never authority: it only classifies post-release cleanup expectations.</summary>
    internal bool ReleaseAttempted => Volatile.Read(ref _releaseAttempted) != 0;

    /// <summary>Exactly one nonblocking RELEASE attempt. The writer remains
    /// owner-local and is never exposed; a second call cannot write again.</summary>
    internal ReleasePipeWriteResult TryWriteRelease(ReadOnlyMemory<byte> frame)
    {
        if (Interlocked.Exchange(ref _releaseAttempted, 1) != 0)
        {
            return ReleasePipeWriteResult.AlreadyAttempted;
        }

        var writer = _controlWriter;
        if (writer is null || writer.IsInvalid || writer.IsClosed)
        {
            return ReleasePipeWriteResult.Unknown;
        }

        Interlocked.Increment(ref _physicalReleaseAttempts);
        return ReleasePipeWrite.WriteOnce(
            checked((int)writer.DangerousGetHandle()), frame);
    }

    ReleasePipeWriteResult IReleaseWriteChannel.TryWriteOnce(ReadOnlyMemory<byte> frame) =>
        TryWriteRelease(frame);

    internal void CloseAbortControl() => Interlocked.Exchange(ref _controlWriter, null)?.Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseAbortControl();
        CoordinatorStatusReader.Dispose();
        HelperStatusReader.Dispose();
        StdoutReader.Dispose();
        StderrReader.Dispose();
        WrapperReaper.Dispose();
        GC.SuppressFinalize(this);
    }
}
