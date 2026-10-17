using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Native;

/// <summary>Owns the single wait obligation for a child returned by posix_spawn.
/// pidfd readiness proves exit; waitid(P_PIDFD) consumes the child status.
/// Repeated cleanup observes the cached result and never waits a second time.</summary>
internal sealed class PidFdChildReaper : IDisposable
{
    private readonly object _gate = new();
    private readonly SafeFileHandle _pidFd;
    private bool _waitAttempted;
    private bool _reaped;
    private int _exitCode = -1;
    private string _waitError = string.Empty;
    private int _waitCalls;
    private bool _disposed;

    internal PidFdChildReaper(SafeFileHandle pidFd)
    {
        ArgumentNullException.ThrowIfNull(pidFd);
        if (pidFd.IsClosed || pidFd.IsInvalid)
        {
            throw new ArgumentException("a live pidfd is required", nameof(pidFd));
        }
        _pidFd = pidFd;
    }

    internal int WaitCalls
    {
        get { lock (_gate) return _waitCalls; }
    }

    internal bool TryReap(int timeoutMilliseconds, out int exitCode, out string error)
    {
        lock (_gate)
        {
            exitCode = _exitCode;
            error = _waitError;
            if (_disposed)
            {
                error = "wrapper pidfd reaper is disposed";
                return false;
            }
            if (_waitAttempted)
            {
                return _reaped;
            }

            var fd = _pidFd.DangerousGetHandle().ToInt32();
            if (!PidFd.TryExited(fd, timeoutMilliseconds, out var exited, out error))
            {
                return false;
            }
            if (!exited)
            {
                error = "wrapper pidfd did not signal exit within the bound";
                return false;
            }

            _waitCalls++;
            if (!NativeLaunchInterop.TryWaitPidFd(fd, out _exitCode, out _waitError))
            {
                _waitAttempted = true;
                exitCode = -1;
                error = _waitError;
                return false;
            }

            _waitAttempted = true;
            _reaped = true;
            exitCode = _exitCode;
            error = string.Empty;
            return true;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _pidFd.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
