using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Launch;

/// <summary>Private live HELD state. The control writer is never exposed;
/// WP04 can close it for abort but has no way to write a release token.</summary>
internal sealed class HeldExecution : IDisposable
{
    private SafeFileHandle? _controlWriter;
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
