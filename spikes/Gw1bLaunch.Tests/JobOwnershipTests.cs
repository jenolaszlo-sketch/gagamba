// GQ-1 unit coverage for the fail-closed branch: the in-leg evidence
// proves the success/nesting paths on this host, but the denial path
// (assign fails -> terminate target, record classified, no fallback) never
// triggers here. An invalid job handle forces it deterministically.
// Win32-only: vacuous pass elsewhere (the leg, not this file, owns the
// cross-platform story).
using Gagamba.Spikes.Gw1bLaunch;
using Xunit;

namespace Gw1bLaunch.Tests;

public sealed class JobOwnershipTests
{
    private static string CmdExe => Path.Combine(Environment.SystemDirectory, "cmd.exe");

    [Fact]
    public async Task InvalidJobHandle_FailsClosedWithNoEscape()
    {
        if (!OperatingSystem.IsWindows()) return;
        await Task.Yield();
        var (ok, detail, h, pid) = JobOwnership.LaunchIntoJob(
            new IntPtr(0x1234), CmdExe, "/d /c ping -n 30 127.0.0.1 > NUL", Path.GetTempPath());
        Assert.False(ok);
        Assert.Contains("no unmanaged execution", detail, StringComparison.Ordinal);
        Assert.Equal(IntPtr.Zero, h);
        Assert.NotEqual(0u, pid);
        // The fail-closed path terminates the just-spawned target: nothing
        // may escape merely because assignment was denied.
        Assert.True(JobOwnership.WaitAllDead([pid], 10_000));
    }

    [Fact]
    public void KillOnCloseJob_TerminateKillsRootWithCode()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (hJob, _) = JobOwnership.CreateKillOnCloseJob();
        Assert.NotEqual(IntPtr.Zero, hJob);
        try
        {
            var (ok, _, hProc, pid) = JobOwnership.LaunchIntoJob(
                hJob, CmdExe, "/d /c ping -n 30 127.0.0.1 > NUL", Path.GetTempPath());
            Assert.True(ok);
            try
            {
                Assert.True(JobOwnership.IsPidInJob(pid));
                Assert.True(Native.TerminateJobObject(hJob, 99));
                Assert.True(JobOwnership.WaitAllDead([pid], 5_000));
                Assert.True(JobOwnership.TryExitCode(pid, out uint code));
                Assert.Equal(99u, code);
            }
            finally
            {
                try { Native.CloseHandle(hProc); } catch { }
            }
        }
        finally
        {
            try { Native.CloseHandle(hJob); } catch { }
        }
    }
}
