// Native controls for the test-only termination observer. These run in a
// dedicated test host because they spawn children and peek at wait status.
// They use posix_spawn directly (not System.Diagnostics.Process) so the
// observer's waitid(WNOWAIT) and the final waitpid are the only status
// consumers, mirroring the production pidfd/reaper relationship.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[Trait("Category", "ProcessGlobalReaping")]
public sealed class Wp05TerminationObserverTests
{
    private const int SIGKILL = 9;

    [DllImport("libc", EntryPoint = "posix_spawn", SetLastError = true)]
    private static extern int PosixSpawn(
        out int pid, string path, IntPtr fileActions, IntPtr attributes, IntPtr argv, IntPtr envp);

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Kill(int pid, int signal);

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("linux only");
    }

    [Fact]
    public void NormalExit137IsObservedAsExitedWithStatus137()
    {
        SkipUnlessLinux();
        var pid = Spawn("/bin/sh", "sh", "-c", "exit 137");
        Assert.True(PidFd.TryOpen(pid, out var pidFd, out var pidFdError), pidFdError);
        using var handle = new SafeFileHandle((IntPtr)pidFd, ownsHandle: true);
        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pidFd, pid);
        using var observer = Wp05TerminationObserver.StartForPidFd(pidFd, identity);

        var termination = observer.WaitForResult(10_000);
        var reaped = NativeLaunchInterop.WaitPid(pid, out var rawStatus, 0);

        Assert.Equal(pid, reaped);
        Assert.True(WIFEXITED(rawStatus), $"raw wait 0x{rawStatus:x} was not a normal exit");
        Assert.Equal(137, WEXITSTATUS(rawStatus));
        Assert.Equal(ObservedTerminationKind.Exited, termination.Kind);
        Assert.Equal(1, termination.RawCode);
        Assert.Equal(137, termination.ExitStatus);
        Assert.Null(termination.Signal);
        Assert.Equal(TerminationObservationSource.PidFdWaitIdWnowait, termination.Source);
        Assert.Equal(pid, termination.Identity.Pid);
        Assert.Equal(pidFd, termination.Identity.PidFd);
    }

    [Fact]
    public void SigkillIsObservedAsKilledBySignal9()
    {
        SkipUnlessLinux();
        var pid = Spawn("/bin/sleep", "sleep", "30");
        Assert.True(PidFd.TryOpen(pid, out var pidFd, out var pidFdError), pidFdError);
        using var handle = new SafeFileHandle((IntPtr)pidFd, ownsHandle: true);
        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pidFd, pid);
        using var observer = Wp05TerminationObserver.StartForPidFd(pidFd, identity);

        Assert.Equal(0, Kill(pid, SIGKILL));
        var termination = observer.WaitForResult(10_000);
        var reaped = NativeLaunchInterop.WaitPid(pid, out var rawStatus, 0);

        Assert.Equal(pid, reaped);
        Assert.True(WIFSIGNALED(rawStatus), $"raw wait 0x{rawStatus:x} was not signal termination");
        Assert.Equal(SIGKILL, WTERMSIG(rawStatus));
        Assert.Equal(ObservedTerminationKind.Killed, termination.Kind);
        Assert.Equal(2, termination.RawCode);
        Assert.Null(termination.ExitStatus);
        Assert.Equal(SIGKILL, termination.Signal);
        Assert.Equal(TerminationObservationSource.PidFdWaitIdWnowait, termination.Source);
        Assert.Equal(pid, termination.Identity.Pid);
        Assert.Equal(pidFd, termination.Identity.PidFd);
    }

    [Fact]
    public void ExpectedIdentityComesFromOwnedPidFdBeforeObservation()
    {
        SkipUnlessLinux();
        var pid = Spawn("/bin/sh", "sh", "-c", "exit 137");
        Assert.True(PidFd.TryOpen(pid, out var pidFd, out var pidFdError), pidFdError);
        var preparation = new PreparationIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new byte[32]);
        using var reaper = new PidFdChildReaper(
            new SafeFileHandle((IntPtr)pidFd, ownsHandle: true));
        using var observer = Wp05TerminationObserver.StartForReaper(reaper, preparation);

        Assert.Equal(pid, observer.ExpectedIdentity.Pid);
        Assert.Equal(pidFd, observer.ExpectedIdentity.PidFd);
        Assert.Equal(preparation.ExecutionId, observer.ExpectedIdentity.ExecutionId);
        Assert.Equal(preparation.GenerationId, observer.ExpectedIdentity.GenerationId);
        Assert.Equal(preparation.PreparationId, observer.ExpectedIdentity.PreparationId);

        var termination = observer.WaitForResult(10_000);
        Assert.Equal(ObservedTerminationKind.Exited, termination.Kind);
        Assert.Equal(pid, termination.Identity.Pid);
        Assert.Equal(pidFd, termination.Identity.PidFd);
        Assert.Equal(pid, NativeLaunchInterop.WaitPid(pid, out _, 0));
    }

    private static int Spawn(string path, params string[] arguments)
    {
        var encoded = new IntPtr[arguments.Length + 1];
        IntPtr argv = IntPtr.Zero;
        IntPtr envp = IntPtr.Zero;
        try
        {
            for (var i = 0; i < arguments.Length; i++)
                encoded[i] = Marshal.StringToCoTaskMemUTF8(arguments[i]);
            encoded[^1] = IntPtr.Zero;

            argv = Marshal.AllocHGlobal(IntPtr.Size * encoded.Length);
            Marshal.Copy(encoded, 0, argv, encoded.Length);
            envp = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(envp, IntPtr.Zero);

            var result = PosixSpawn(out var pid, path, IntPtr.Zero, IntPtr.Zero, argv, envp);
            if (result != 0)
                throw new InvalidOperationException(
                    $"posix_spawn {path} errno {Marshal.GetLastPInvokeError()}");
            return pid;
        }
        finally
        {
            foreach (var pointer in encoded)
            {
                if (pointer != IntPtr.Zero) Marshal.FreeCoTaskMem(pointer);
            }
            if (argv != IntPtr.Zero) Marshal.FreeHGlobal(argv);
            if (envp != IntPtr.Zero) Marshal.FreeHGlobal(envp);
        }
    }

    private static bool WIFEXITED(int status) => (status & 0x7F) == 0;

    private static int WEXITSTATUS(int status) => (status >> 8) & 0xFF;

    private static bool WIFSIGNALED(int status) =>
        ((status & 0x7F) + 1) >> 1 > 0;

    private static int WTERMSIG(int status) => status & 0x7F;
}


