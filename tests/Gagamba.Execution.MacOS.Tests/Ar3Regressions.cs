using System.Collections.Concurrent;
using System.Diagnostics;
using Gagamba.Execution;
using Gagamba.Execution.MacOS;
using Xunit;
#pragma warning disable xUnit1051 // Explicit tokens are part of these cancellation and disposal races.

namespace Gagamba.Execution.MacOS.Tests;

public sealed class Ar3Regressions
{
    private sealed class Fake(Func<string, string, string?, HelperResult> response) : ILaunchdTransport
    {
        public ConcurrentQueue<string> Calls { get; } = new();
        public string? PlistPath { get; private set; }
        public HelperResult Run(string verb, string target, string? extraArg = null, TimeSpan? timeout = null)
        {
            Calls.Enqueue(verb);
            if (verb == "bootstrap") PlistPath = extraArg;
            return response(verb, target, extraArg);
        }
    }

    private static readonly ExecutionRequirements NoRequirements =
        new(Array.Empty<ExecutionRequirement>());
    private static ProcessStartSpec Process() => new("/bin/sh", "-c \"exit 17\"",
        Path.GetTempPath(), new Dictionary<string, string>());
    private static PreparedExecution Prepare(MacOsExecutionProvider provider) =>
        Assert.IsType<PrepareResult.Accepted>(provider.Prepare(NoRequirements)).Prepared;
    private static ExecutionHandle Launch(MacOsExecutionProvider provider) =>
        Assert.IsType<LaunchResult.Started>(provider.Launch(Prepare(provider), Process())).Handle;
    private static HelperResult Ok(string output = "") => new(0, output, "");
    private static HelperResult Absent() => new(113, "Could not find service \"org.gagamba.exec.x\" in domain", "");

    [Fact]
    public void JobDirectoryDoesNotExposePlistOrLogsToOtherUsers()
    {
        if (OperatingSystem.IsWindows()) { Assert.Skip("Unix permissions"); return; }
        using var job = OwnedJob.Create("gui/501", new Fake((_, _, _) => Ok()));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            File.GetUnixFileMode(job.WorkDir));
    }

    [Theory]
    [InlineData("state = waiting\n", 0)]
    [InlineData("state = runningish\n", 0)]
    [InlineData("state = not running\n", 0)]
    [InlineData("state = not running\nlast exit code = x\n", 0)]
    [InlineData("state = not running\nlast exit code = (never exited)\n", 0)]
    [InlineData("state = running\nlast exit code = malformed\n", 0)]
    [InlineData("", 0)]
    [InlineData("permission denied", 1)]
    [InlineData("launchctl timed out", 124)]
    public void UnknownObservationsNeverMeanTerminal(string output, int rc)
    {
        var observation = Launchd.Observe(new HelperResult(rc, output, ""));
        Assert.Equal(JobObservationKind.Unknown, observation.Kind);
    }

    [Fact]
    public void RecognizedAbsentAndSignalExitAreDistinct()
    {
        Assert.Equal(JobObservationKind.ConfirmedNotFound, Launchd.Observe(Absent()).Kind);
        var signal = Launchd.Observe(Ok("state = not running\nlast exit code = -9\n"));
        Assert.Equal(JobObservationKind.Terminal, signal.Kind);
        Assert.Equal(137, signal.ExitCode);
        Assert.Equal(JobObservationKind.Running,
            Launchd.Observe(Ok("state = running\n")).Kind);
        Assert.Equal(JobObservationKind.Running,
            Launchd.Observe(Ok("state = running\nlast exit code = (never exited)\n")).Kind);
    }

    [Fact]
    public async Task CancelledConcurrentAndLateWaitersShareTerminalResult()
    {
        bool exiting = false, removed = false;
        var fake = new Fake((verb, target, _) => verb switch
        {
            "print" when target == "gui/501" => Ok(),
            "print" when removed => Absent(),
            "print" => Ok(exiting ? "state = not running\nlast exit code = 17\n" : "state = running\n"),
            "bootout" => Bootout(),
            _ => Ok(),
        });
        HelperResult Bootout() { removed = true; return Ok(); }
        await using var provider = new MacOsExecutionProvider("gui/501", fake);
        var handle = Launch(provider);
        using var cancellation = new CancellationTokenSource();
        var first = provider.WaitForCompletionAsync(handle, cancellation.Token).AsTask();
        var second = provider.WaitForCompletionAsync(handle).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first);
        exiting = true;
        Assert.Equal(17, Assert.IsType<CompletionResult.NaturalExit>(
            await second.WaitAsync(TimeSpan.FromSeconds(5))).RootExitCode);
        Assert.Equal(17, Assert.IsType<CompletionResult.NaturalExit>(
            await provider.WaitForCompletionAsync(handle)).RootExitCode);
        Assert.Equal(1, fake.Calls.Count(call => call == "bootout"));
        Assert.False(File.Exists(fake.PlistPath));
    }

    [Fact]
    public async Task FailedBootoutAndFailedPrintRemainUnknownUntilRetry()
    {
        bool unknown = false, allowCleanup = false;
        var fake = new Fake((verb, target, _) => verb switch
        {
            "print" when target == "gui/501" => Ok(),
            "print" when allowCleanup => Absent(),
            "print" when unknown => new HelperResult(1, "permission denied", ""),
            "print" => Ok("state = running\n"),
            "bootout" when allowCleanup => Ok(),
            "bootout" => new HelperResult(1, "permission denied", ""),
            _ => Ok(),
        });
        var provider = new MacOsExecutionProvider("gui/501", fake);
        try
        {
            var handle = Launch(provider);
            unknown = true;
            var observed = Assert.IsType<CompletionResult.Failed>(
                await provider.WaitForCompletionAsync(handle).AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.Contains("observation failed", observed.Reasons.Single());
            Assert.IsType<TerminateResult.Failed>(provider.Terminate(handle));
            Assert.True(File.Exists(fake.PlistPath), "uncertain cleanup discarded its only job metadata");
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.DisposeAsync());
            allowCleanup = true;
            await provider.DisposeAsync();
            Assert.False(File.Exists(fake.PlistPath));
        }
        finally
        {
            allowCleanup = true;
            try { await provider.DisposeAsync(); } catch { }
        }
    }

    [Fact]
    public async Task TerminationDuringObservationHasOneStableResult()
    {
        int removed = 0;
        var fake = new Fake((verb, target, _) => verb switch
        {
            "print" when target == "gui/501" => Ok(),
            "print" => Volatile.Read(ref removed) == 1 ? Absent() : Ok("state = running\n"),
            "bootout" => Remove(),
            _ => Ok(),
        });
        HelperResult Remove() { Interlocked.Exchange(ref removed, 1); return Ok(); }
        await using var provider = new MacOsExecutionProvider("gui/501", fake);
        var handle = Launch(provider);
        var waiter = provider.WaitForCompletionAsync(handle).AsTask();
        Assert.IsType<TerminateResult.Terminated>(provider.Terminate(handle));
        Assert.IsType<CompletionResult.Terminated>(await waiter.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsType<CompletionResult.Terminated>(await provider.WaitForCompletionAsync(handle));
        Assert.False(File.Exists(fake.PlistPath));
    }

    [Fact]
    public async Task DisposalDuringObservationJoinsOwnedState()
    {
        int removed = 0;
        var fake = new Fake((verb, target, _) => verb switch
        {
            "print" when target == "gui/501" => Ok(),
            "print" => Volatile.Read(ref removed) == 1 ? Absent() : Ok("state = running\n"),
            "bootout" => Remove(),
            _ => Ok(),
        });
        HelperResult Remove() { Interlocked.Exchange(ref removed, 1); return Ok(); }
        var provider = new MacOsExecutionProvider("gui/501", fake);
        var handle = Launch(provider);
        var waiter = provider.WaitForCompletionAsync(handle).AsTask();
        await provider.DisposeAsync();
        Assert.IsType<CompletionResult.Terminated>(await waiter.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(File.Exists(fake.PlistPath));
    }

    [Fact]
    public async Task DisposalWinningBeforeKickstartPreventsRelease()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        bool removed = false;
        var fake = new Fake((verb, target, _) =>
        {
            if (verb == "print" && target == "gui/501") return Ok();
            if (verb == "bootstrap")
            { entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException(); return Ok(); }
            if (verb == "bootout") { removed = true; return Ok(); }
            if (verb == "print") return removed ? Absent() : Ok("state = running\n");
            return Ok();
        });
        var provider = new MacOsExecutionProvider("gui/501", fake);
        try
        {
            var prep = Prepare(provider);
            var launching = Task.Run(() => provider.Launch(prep, Process()));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            var disposing = provider.DisposeAsync().AsTask();
            Assert.Throws<ObjectDisposedException>(() => provider.Prepare(NoRequirements));
            release.Set();
            Assert.IsType<LaunchResult.Failed>(await launching.WaitAsync(TimeSpan.FromSeconds(5)));
            await disposing.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.DoesNotContain("kickstart", fake.Calls);
            Assert.False(File.Exists(fake.PlistPath));
        }
        finally { release.Set(); try { await provider.DisposeAsync(); } catch { } }
    }

    [Fact]
    public async Task TransportDrainsBothPipesAndKeepsFullStructuralOutput()
    {
        if (OperatingSystem.IsWindows()) { Assert.Skip("Unix helper shim"); return; }
        string dir = Path.Combine(Path.GetTempPath(), "gagamba-ar3-helper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "helper");
        try
        {
            File.WriteAllText(path, "#!/bin/sh\ni=0\nwhile [ $i -lt 3500 ]; do printf x; printf y >&2; i=$((i+1)); done\nprintf '\\nstate = not running\\nlast exit code = 17\\n'\n");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var transport = new ProcessLaunchdTransport(path);
            var result = transport.Run("print", "ignored", timeout: TimeSpan.FromSeconds(5));
            Assert.Equal(HelperFailure.None, result.Failure);
            Assert.True(result.Output.Length > 2000);
            Assert.Equal(17, Launchd.Observe(result).ExitCode);
            Assert.True(result.Stderr.Length > 2000);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task TransportTimeoutKillsAndReapsHelperWithOpenPipes()
    {
        if (OperatingSystem.IsWindows()) { Assert.Skip("Unix helper shim"); return; }
        string dir = Path.Combine(Path.GetTempPath(), "gagamba-ar3-helper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "helper");
        string pidPath = Path.Combine(dir, "pid");
        try
        {
            File.WriteAllText(path, $"#!/bin/sh\necho $$ > '{pidPath}'\nwhile true; do printf x; printf y >&2; done\n");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var timer = Stopwatch.StartNew();
            var result = new ProcessLaunchdTransport(path).Run("print", "ignored",
                timeout: TimeSpan.FromMilliseconds(300));
            Assert.Equal(HelperFailure.Timeout, result.Failure);
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(3));
            Assert.True(File.Exists(pidPath), "helper never reached the test workload");
            int pid = int.Parse(File.ReadAllText(pidPath).Trim());
            Assert.Throws<ArgumentException>(() => System.Diagnostics.Process.GetProcessById(pid));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TransportRejectsMissingHelper()
    {
        var missing = new ProcessLaunchdTransport(
            Path.Combine(Path.GetTempPath(), "missing-ar3-" + Guid.NewGuid().ToString("N")))
            .Run("print", "ignored", timeout: TimeSpan.FromSeconds(1));
        Assert.Equal(HelperFailure.Start, missing.Failure);
    }

    [Fact]
    public void TransportDrainsOversizedOutputThenRejectsIt()
    {
        if (OperatingSystem.IsWindows()) { Assert.Skip("Unix helper shim"); return; }
        string dir = Path.Combine(Path.GetTempPath(), "gagamba-ar3-helper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "helper");
        try
        {
            File.WriteAllText(path, "#!/bin/sh\nhead -c 270000 /dev/zero | tr '\\000' x\nhead -c 10000 /dev/zero | tr '\\000' y >&2\nprintf '\\nstate = running\\n'\n");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var result = new ProcessLaunchdTransport(path).Run("print", "ignored", timeout: TimeSpan.FromSeconds(5));
            Assert.Equal(HelperFailure.OutputLimit, result.Failure);
            Assert.Equal(ProcessLaunchdTransport.MaxStdoutChars, result.Output.Length);
            Assert.Equal(ProcessLaunchdTransport.MaxStderrChars, result.Stderr.Length);
            Assert.Equal(JobObservationKind.Unknown, Launchd.Observe(result).Kind);
        }
        finally { Directory.Delete(dir, true); }
    }
}
