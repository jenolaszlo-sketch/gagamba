using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Xunit;
namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class RecoveryB2NativeTests
{
    private sealed class FaultingPidfd(bool dieBeforeRetention) : IPidFdProvider
    {
        public bool TryOpen(int pid, out int fd, out string error)
        {
            if (!dieBeforeRetention) { fd = -1; error = "injected unavailable pidfd"; return false; }
            if (!PidFd.TryOpen(pid, out fd, out error)) return false;
            // This callback receives ONLY the newly spawned direct test child.
            // Retain its pidfd first, then inject exit before the scope stores it.
            using var child = Process.GetProcessById(pid);
            child.Kill(); Assert.True(child.WaitForExit(5000));
            return true;
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void MissingPidfdOrExitBeforeRetentionNeverCreatesRecoveryAuthority(bool dieBeforeRetention)
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("requires unprivileged Linux pidfd; Windows is not qualification");
        Assert.True(RecoveryInterop.IsUnprivileged);
        var owner = Environment.GetEnvironmentVariable("GAGAMBA_OWNER_DLL");
        Assert.True(owner is not null && File.Exists(owner), "explicit qualified owner artifact required");
        using var scope = new M1RecoveryScope(new FaultingPidfd(dieBeforeRetention));
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var local, out var childFd, out var error), error);
        using var localHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)local, true);
        var spawn = scope.TrySpawnOwner(owner!, ["--exec", Guid.NewGuid().ToString("D"), "--lease", Guid.NewGuid().ToString("D")], childFd);
        Assert.Empty(scope.RetainedRecoveryLeases);
        Assert.Empty(scope.RetainedStagingDirectories);
        if (!dieBeforeRetention) { Assert.False(spawn.Ok); Assert.Null(spawn.Process); }
        else
        {
            Assert.True(spawn.Ok, spawn.Error);
            Assert.True(scope.TryOwnerPidFdExited(spawn.Process!, 1000, out var exited, out error), error);
            Assert.True(exited);
        }
    }
}
