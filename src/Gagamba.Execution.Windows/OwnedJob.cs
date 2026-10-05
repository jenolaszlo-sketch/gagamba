// One Job Object owned for exactly one activity lifetime. Kill on close,
// no breakaway flags, no quotas. Deterministic disposal: every native
// handle closes exactly once; closing the job is what kills the tree
// (TerminateJobObject is used only for explicit cancellation).
using System.Runtime.InteropServices;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Windows.Tests")]

namespace Gagamba.Execution.Windows;

internal sealed class OwnedJob : IDisposable
{
    private IntPtr _job;
    private bool _disposed;

    private OwnedJob(IntPtr job) => _job = job;

    public bool IsClosed => _job == IntPtr.Zero;

    /// <summary>Native handle for job API calls. Internal only: never
    /// exposed through the public contract (see opacity tests).</summary>
    internal IntPtr DangerousHandle => _disposed ? IntPtr.Zero : _job;

    public static (OwnedJob? Job, string Error) Create()
    {
        IntPtr h = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
        if (h == IntPtr.Zero)
            return (null, $"CreateJobObject err=0x{Marshal.GetLastWin32Error():X}");
        var info = new NativeMethods.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new NativeMethods.JobObjectBasicLimitInformation
            {
                LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
            },
        };
        if (!NativeMethods.SetInformationJobObject(h,
                NativeMethods.JobObjectExtendedLimitInformationClass,
                ref info, (uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>()))
        {
            int err = Marshal.GetLastWin32Error();
            try { NativeMethods.CloseHandle(h); } catch { }
            return (null, $"SetInformationJobObject err=0x{err:X}");
        }
        return (new OwnedJob(h), "");
    }

    /// <summary>Explicit cancellation: terminate the whole job, keep it owned.</summary>
    public (bool Ok, string Error) Terminate(uint exitCode)
    {
        if (_disposed || _job == IntPtr.Zero)
            return (false, "job already closed");
        if (!NativeMethods.TerminateJobObject(_job, exitCode))
            return (false, $"TerminateJobObject err=0x{Marshal.GetLastWin32Error():X}");
        return (true, "");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Closing the last handle fires KILL_ON_JOB_CLOSE: this close IS
        // the tree teardown for the dispose path. Exactly once by flag.
        if (_job != IntPtr.Zero)
        {
            try { NativeMethods.CloseHandle(_job); } catch { }
            _job = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
