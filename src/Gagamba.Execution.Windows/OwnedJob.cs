using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Windows.Tests")]

namespace Gagamba.Execution.Windows;

// Owns one kill-on-close job. Native operations and closure share this gate;
// P/Invoke also takes a SafeHandle reference for the duration of each call.
internal sealed class OwnedJob : IDisposable
{
    private readonly object _gate = new();
    private readonly SafeWaitHandle _handle;
    private bool _disposed;

    private OwnedJob(SafeWaitHandle handle) => _handle = handle;

    public bool IsClosed { get { lock (_gate) return _disposed; } }

    public static (OwnedJob? Job, string Error) Create()
    {
        IntPtr raw = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
        if (raw == IntPtr.Zero)
            return (null, $"CreateJobObject err=0x{Marshal.GetLastWin32Error():X}");
        var handle = new SafeWaitHandle(raw, ownsHandle: true);
        try
        {
            var info = new NativeMethods.JobObjectExtendedLimitInformation
            {
                BasicLimitInformation = new NativeMethods.JobObjectBasicLimitInformation
                { LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE },
            };
            if (!NativeMethods.SetInformationJobObject(handle,
                NativeMethods.JobObjectExtendedLimitInformationClass, ref info,
                (uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>()))
            {
                int err = Marshal.GetLastWin32Error();
                handle.Dispose();
                return (null, $"SetInformationJobObject err=0x{err:X}");
            }
            return (new OwnedJob(handle), "");
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public bool TryAssign(SafeWaitHandle process, out int error)
    {
        lock (_gate)
        {
            if (_disposed) { error = 6; return false; }
            if (NativeMethods.AssignProcessToJobObject(_handle, process))
            { error = 0; return true; }
            error = Marshal.GetLastWin32Error();
            return false;
        }
    }

    // Test-only external process handle; the caller owns that handle.
    public bool TryAssign(IntPtr process, out int error)
    {
        lock (_gate)
        {
            if (_disposed || process == IntPtr.Zero) { error = 6; return false; }
            if (NativeMethods.AssignProcessToJobObject(_handle, process))
            { error = 0; return true; }
            error = Marshal.GetLastWin32Error();
            return false;
        }
    }

    public (bool Ok, string Error) Terminate(uint exitCode)
    {
        lock (_gate)
        {
            if (_disposed) return (false, "job already closed");
            if (!NativeMethods.TerminateJobObject(_handle, exitCode))
                return (false, $"TerminateJobObject err=0x{Marshal.GetLastWin32Error():X}");
            return (true, "");
        }
    }

    public (bool Ok, uint Active, string Error) ActiveProcessCount()
    {
        lock (_gate)
        {
            if (_disposed) return (false, 0, "job already closed");
            var info = default(NativeMethods.JobObjectBasicAccountingInformation);
            if (!NativeMethods.QueryInformationJobObject(_handle,
                NativeMethods.JobObjectBasicAccountingInformationClass, ref info,
                (uint)Marshal.SizeOf<NativeMethods.JobObjectBasicAccountingInformation>(), IntPtr.Zero))
                return (false, 0, $"QueryInformationJobObject err=0x{Marshal.GetLastWin32Error():X}");
            return (true, info.ActiveProcesses, "");
        }
    }

    public bool TryActiveProcessCount(out uint active)
    {
        var result = ActiveProcessCount();
        active = result.Active;
        return result.Ok;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _handle.Dispose();
        }
    }
}
