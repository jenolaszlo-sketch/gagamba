// Independent reviewer probes (OpenCode, WP05 final harness qualification).
// These are test-only hostile probes against the WP05 qualification harness
// itself: the test-only termination observer and the forged-abort oracle.
// They spawn short-lived children and wait on specific pids/pidfds only;
// they never consume process-global wait state, so they stay untagged and
// run in the regular parallel phase. Added on the reviewer branch only;
// no production file was modified.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class Wp05OpencodeReviewerProbeTests
{
    private const int SIGKILL = 9;
    private const int O_RDONLY = 0;

    [DllImport("libc", EntryPoint = "posix_spawn", SetLastError = true)]
    private static extern int PosixSpawn(
        out int pid, string path, IntPtr fileActions, IntPtr attributes, IntPtr argv, IntPtr envp);

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Kill(int pid, int signal);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int Open(string path, int flags);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int Close(int fd);

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("linux only");
    }

    [Fact]
    public void UnrelatedChildIsNeverAttributedToTheObservedPidfd()
    {
        SkipUnlessLinux();
        var observedPid = Spawn("/bin/sh", "sh", "-c", "exit 137");
        var unrelatedPid = Spawn("/bin/sleep", "sleep", "30");
        try
        {
            Assert.True(PidFd.TryOpen(observedPid, out var observedFd, out var error), error);
            using var observedHandle = new SafeFileHandle((IntPtr)observedFd, ownsHandle: true);
            var identity = new OwnedWrapperIdentity(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), observedFd, observedPid);
            using var observer = Wp05TerminationObserver.StartForPidFd(observedFd, identity);

            var termination = observer.WaitForResult(10_000);
            Assert.Equal(ObservedTerminationKind.Exited, termination.Kind);
            Assert.Equal(137, termination.ExitStatus);
            Assert.Equal(observedPid, termination.Identity.Pid);
            Assert.Equal(observedFd, termination.Identity.PidFd);

            Assert.True(PidFd.TryOpen(unrelatedPid, out var unrelatedFd, out var unrelatedError),
                unrelatedError);
            using var unrelatedHandle = new SafeFileHandle((IntPtr)unrelatedFd, ownsHandle: true);
            Assert.True(PidFd.TryExited(unrelatedFd, 0, out var unrelatedExited, out var pollError),
                pollError);
            Assert.False(unrelatedExited, "the unrelated child exited while only the observed child was targeted");

            Assert.Equal(0, Kill(unrelatedPid, SIGKILL));
            Assert.True(PidFd.TryExited(unrelatedFd, 5_000, out unrelatedExited, out pollError), pollError);
            Assert.True(unrelatedExited, "the unrelated child did not react to its own SIGKILL");
            Assert.Equal(unrelatedPid, NativeLaunchInterop.WaitPid(unrelatedPid, out var unrelatedStatus, 0));
            Assert.Equal(SIGKILL, WTERMSIG(unrelatedStatus));

            Assert.Equal(observedPid, NativeLaunchInterop.WaitPid(observedPid, out var observedStatus, 0));
            Assert.Equal(137, WEXITSTATUS(observedStatus));
        }
        finally
        {
            ReapIfAlive(unrelatedPid);
        }
    }

    [Fact]
    public void PeekedStatusIsNotConsumedAndReapedPidfdObservationFailsClosed()
    {
        SkipUnlessLinux();
        var pid = Spawn("/bin/sh", "sh", "-c", "exit 137");
        Assert.True(PidFd.TryOpen(pid, out var pidFd, out var error), error);
        using var handle = new SafeFileHandle((IntPtr)pidFd, ownsHandle: true);
        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pidFd, pid);

        using (var observer = Wp05TerminationObserver.StartForPidFd(pidFd, identity))
        {
            var first = observer.WaitForResult(10_000);
            Assert.Equal(ObservedTerminationKind.Exited, first.Kind);
            Assert.Equal(137, first.ExitStatus);
        }

        Assert.Equal(pid, NativeLaunchInterop.WaitPid(pid, out var rawStatus, 0));
        Assert.Equal(137, WEXITSTATUS(rawStatus));

        using (var second = Wp05TerminationObserver.StartForPidFd(pidFd, identity))
        {
            var secondObservation = second.WaitForResult(10_000);
            Assert.Equal(ObservedTerminationKind.Unknown, secondObservation.Kind);
            Assert.False(string.IsNullOrEmpty(secondObservation.Error),
                "a reaped pidfd observation reported success without an error");
            Assert.Equal(TerminationObservationSource.None, secondObservation.Source);
            Assert.Null(secondObservation.ExitStatus);
            Assert.Null(secondObservation.Signal);
        }
    }

    [Fact]
    public void ObserverStartedAfterZombieStillObservesExitStatus()
    {
        SkipUnlessLinux();
        var pid = Spawn("/bin/sh", "sh", "-c", "exit 137");
        Assert.True(PidFd.TryOpen(pid, out var pidFd, out var error), error);
        using var handle = new SafeFileHandle((IntPtr)pidFd, ownsHandle: true);
        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), pidFd, pid);

        var exitedBeforeObserver = false;
        for (var attempt = 0; attempt < 200; ++attempt)
        {
            if (PidFd.TryExited(pidFd, 25, out exitedBeforeObserver, out var pollError))
            {
                break;
            }

            Assert.Fail($"pidfd poll failed while waiting for the zombie: {pollError}");
        }

        Assert.True(exitedBeforeObserver, "the child did not become a zombie before the late observer started");

        using var observer = Wp05TerminationObserver.StartForPidFd(pidFd, identity);
        var termination = observer.WaitForResult(10_000);
        Assert.Equal(ObservedTerminationKind.Exited, termination.Kind);
        Assert.Equal(137, termination.ExitStatus);
        Assert.Equal(pid, termination.Identity.Pid);
        Assert.Equal(pid, NativeLaunchInterop.WaitPid(pid, out _, 0));
    }

    [Fact]
    public void ClosedPidfdObservationFailsClosedWithoutHang()
    {
        SkipUnlessLinux();
        var staleFd = Open("/dev/null", O_RDONLY);
        Assert.True(staleFd >= 0, "open(/dev/null) failed");
        Assert.Equal(0, Close(staleFd));

        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), staleFd, 1);
        using var observer = Wp05TerminationObserver.StartForPidFd(staleFd, identity);

        var observation = observer.WaitForResult(10_000);
        Assert.Equal(ObservedTerminationKind.Unknown, observation.Kind);
        Assert.False(string.IsNullOrEmpty(observation.Error),
            "a closed pidfd observation reported success without an error");
        Assert.Equal(TerminationObservationSource.None, observation.Source);
        Assert.Null(observation.ExitStatus);
        Assert.Null(observation.Signal);
    }

    [Fact]
    public void BareWrapperExitCodeIsAlwaysRejected()
    {
        var decision = new Wp05ForgedAbortOracle.DecisionState(
            ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted,
            DeliveryResult.NotAttempted, DurableCommitted: false);
        var journal = new Wp05ForgedAbortOracle.JournalState(
            Ok: true, PreparationStage.Prepared, HasRelease: false, HasStop: false, Error: string.Empty);
        var abort = new Wp05ForgedAbortOracle.AbortState(
            Stopped: true, WrapperReaped: true, Error: string.Empty,
            NoTargetInitializationMarkerAbsent: true);

        foreach (var exitCode in new[] { 0, 133, 137 })
        {
            var rejection = Wp05ForgedAbortOracle.Validate(
                decision, capabilityLive: true, journal, abort, exitCode);
            Assert.False(string.IsNullOrEmpty(rejection),
                $"the bare exit-code overload accepted wrapper exit {exitCode}");
        }
    }

    [Fact]
    public void OracleAcceptsOnlyStructuredExit133OrSigkill9()
    {
        var identity = new OwnedWrapperIdentity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, 1);
        var decision = new Wp05ForgedAbortOracle.DecisionState(
            ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted,
            DeliveryResult.NotAttempted, DurableCommitted: false);
        var journal = new Wp05ForgedAbortOracle.JournalState(
            Ok: true, PreparationStage.Prepared, HasRelease: false, HasStop: false, Error: string.Empty);
        var stop = new AuthorizedStopEvidence(
            identity, Stopped: true, WrapperReaped: true, Error: string.Empty,
            NoTargetInitializationMarkerAbsent: true);

        var exited133 = new ObservedTermination(
            identity, ObservedTerminationKind.Exited, 1, 133, null,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.Null(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, exited133));

        var killed9 = new ObservedTermination(
            identity, ObservedTerminationKind.Killed, 2, null, SIGKILL,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.Null(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, killed9));

        var exited137 = new ObservedTermination(
            identity, ObservedTerminationKind.Exited, 1, 137, null,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, exited137));

        var killed15 = new ObservedTermination(
            identity, ObservedTerminationKind.Killed, 2, null, 15,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, killed15));

        var unknown = new ObservedTermination(
            identity, ObservedTerminationKind.Unknown, 0, null, null,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, unknown));

        var dumped = new ObservedTermination(
            identity, ObservedTerminationKind.Dumped, 3, null, 11,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, dumped));

        var forgedIdentity = identity with { PreparationId = Guid.NewGuid() };
        var mismatched = new ObservedTermination(
            forgedIdentity, ObservedTerminationKind.Exited, 1, 133, null,
            TerminationObservationSource.PidFdWaitIdWnowait, string.Empty);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, stop, mismatched));

        var unstoppped = stop with { Stopped = false };
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, journal, unstoppped, exited133));

        var noisyJournal = journal with { HasStop = true };
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, true, noisyJournal, stop, exited133));

        Assert.NotNull(Wp05ForgedAbortOracle.Validate(decision, capabilityLive: false, journal, stop, exited133));

        var released = decision with { Kind = ReleaseDecisionKind.Released };
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(released, true, journal, stop, exited133));
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

    private static void ReapIfAlive(int pid)
    {
        if (pid <= 0) return;
        if (!PidFd.TryOpen(pid, out var fd, out _)) return;
        using var handle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);
        if (PidFd.TryExited(fd, 0, out var exited, out _) && !exited)
        {
            Kill(pid, SIGKILL);
            NativeLaunchInterop.WaitPid(pid, out _, 0);
        }
        else if (exited)
        {
            NativeLaunchInterop.WaitPid(pid, out _, 0);
        }
    }

    private static int WEXITSTATUS(int status) => (status >> 8) & 0xFF;

    private static int WTERMSIG(int status) => status & 0x7F;
}
