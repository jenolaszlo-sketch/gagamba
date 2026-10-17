using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class PidFdChildReaperTests
{
    [Fact]
    public void ReapsExitedDirectChildWithZeroStatus()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux pidfd and waitid(P_PIDFD)");

        using var reaper = SpawnAndOpenPidFd(0);
        Assert.True(reaper.TryReap(5000, out var exitCode, out var error), error);
        Assert.Equal(0, exitCode);
        Assert.Equal(1, reaper.WaitCalls);
    }

    [Fact]
    public void ReapsExitedDirectChildWithNonzeroStatus()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux pidfd and waitid(P_PIDFD)");

        using var reaper = SpawnAndOpenPidFd(37);
        Assert.True(reaper.TryReap(5000, out var exitCode, out var error), error);
        Assert.Equal(37, exitCode);
        Assert.Equal(1, reaper.WaitCalls);
    }

    [Fact]
    public void RepeatedCleanupConsumesChildStatusExactlyOnce()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires Linux pidfd and waitid(P_PIDFD)");

        using var reaper = SpawnAndOpenPidFd(23);
        Assert.True(reaper.TryReap(5000, out var firstExitCode, out var firstError), firstError);
        Assert.True(reaper.TryReap(0, out var secondExitCode, out var secondError), secondError);
        Assert.Equal(23, firstExitCode);
        Assert.Equal(firstExitCode, secondExitCode);
        Assert.Equal(1, reaper.WaitCalls);
    }

    private static PidFdChildReaper SpawnAndOpenPidFd(int exitCode)
    {
        var argv = new string?[] { "/bin/sh", "-c", $"exit {exitCode}", null };
        var envp = new string?[] { null };
        var spawnResult = PosixSpawn(out var pid, "/bin/sh", IntPtr.Zero, IntPtr.Zero, argv, envp);
        Assert.Equal(0, spawnResult);

        if (!PidFd.TryOpen(pid, out var fd, out var error))
        {
            _ = WaitPid(pid, out _, 0);
            Assert.Fail($"pidfd_open failed: {error}");
        }

        if (!PreparationInterop.HasCloexec(fd))
        {
            _ = NativeLaunchInterop.Close(fd);
            _ = WaitPid(pid, out _, 0);
            Assert.Fail("pidfd_open did not return a close-on-exec descriptor");
        }

        return new PidFdChildReaper(new SafeFileHandle(new IntPtr(fd), ownsHandle: true));
    }

    [DllImport("libc", EntryPoint = "posix_spawn")]
    private static extern int PosixSpawn(
        out int pid,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        IntPtr fileActions,
        IntPtr attributes,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] argv,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] envp);

    [DllImport("libc", EntryPoint = "waitpid", SetLastError = true)]
    private static extern int WaitPid(int pid, out int status, int options);
}
