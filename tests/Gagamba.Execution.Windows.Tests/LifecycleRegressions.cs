using System.Collections;
using System.Diagnostics;
using Gagamba.Execution;
using Gagamba.Execution.Windows;
using Xunit;
#pragma warning disable xUnit1051 // Independent outer watchdogs are intentional for lifecycle regressions.

namespace Gagamba.Execution.Windows.Tests;

public sealed class LifecycleRegressions
{
    private static string Cmd => Path.Combine(Environment.SystemDirectory, "cmd.exe");
    private static string Ping => Path.Combine(Environment.SystemDirectory, "PING.EXE");

    private static PreparedExecution Prep(WindowsExecutionProvider provider) =>
        Assert.IsType<PrepareResult.Accepted>(provider.Prepare(
            new ExecutionRequirements(Array.Empty<ExecutionRequirement>()))).Prepared;

    private static string Workspace()
    {
        string path = Path.Combine(Path.GetTempPath(), "ar1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static ProcessStartSpec Child(string workspace, string command) =>
        new(Cmd, "/d /c " + command, workspace, new Dictionary<string, string>());

    private static async Task<bool> Until(Func<bool> predicate, int milliseconds = 5000)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds)
        {
            if (predicate()) return true;
            await Task.Delay(25);
        }
        return predicate();
    }

    [Fact]
    public async Task E1_WaitCallReturnsBeforeLiveRootFinishes()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        await using var provider = new WindowsExecutionProvider();
        try
        {
            var started = Assert.IsType<LaunchResult.Started>(provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\" & \"{Ping}\" -n 20 127.0.0.1 > NUL")));
            Assert.True(await Until(() => File.Exists(marker)), "child did not run");
            using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var returned = new TaskCompletionSource<ValueTask<CompletionResult>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var caller = Task.Run(() =>
            {
                try { returned.SetResult(provider.WaitForCompletionAsync(started.Handle, watchdog.Token)); }
                catch (Exception ex) { returned.SetException(ex); }
            });
            bool prompt = await Task.WhenAny(returned.Task, Task.Delay(500)) == returned.Task;
            provider.Terminate(started.Handle);
            await caller.WaitAsync(TimeSpan.FromSeconds(8));
            Assert.True(prompt, "WaitForCompletionAsync occupied its calling thread");
            Assert.IsType<CompletionResult.Terminated>(await (await returned.Task));
        }
        finally { try { Directory.Delete(ws, true); } catch { } }
    }

    [Fact]
    public async Task E2_DisposalWinsBeforeLaunchRelease()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        var provider = new WindowsExecutionProvider();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        try
        {
            var prep = Prep(provider);
            var environment = new BarrierDictionary(entered, release);
            var launching = Task.Run(() => provider.Launch(prep,
                new ProcessStartSpec(Cmd, $"/d /c echo ran > \"{marker}\"", ws, environment)));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)), "launch did not reach the owned transition");
            var disposing = provider.DisposeAsync().AsTask();
            // Probe admission through a spent token; this is a deterministic
            // shutdown barrier and does not allocate another native resource.
            Assert.True(await Until(() =>
            {
                try { provider.Discard(prep); return false; }
                catch (ObjectDisposedException) { return true; }
            }), "disposal did not close admission");
            release.Set();
            await disposing.WaitAsync(TimeSpan.FromSeconds(8));
            var result = await launching.WaitAsync(TimeSpan.FromSeconds(8));
            Assert.IsType<LaunchResult.Failed>(result);
            Assert.False(await Until(() => File.Exists(marker), 500), "target ran after shutdown won");
        }
        finally { release.Set(); await provider.DisposeAsync(); try { Directory.Delete(ws, true); } catch { } }
    }

    [Fact]
    public async Task E3_DisposeSettlesAnOutstandingWait()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        var provider = new WindowsExecutionProvider();
        try
        {
            var started = Assert.IsType<LaunchResult.Started>(provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\" & \"{Ping}\" -n 20 127.0.0.1 > NUL")));
            Assert.True(await Until(() => File.Exists(marker)), "child did not run");
            using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            using var begin = new ManualResetEventSlim();
            var disposing = Task.Run(async () =>
            {
                if (!begin.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException();
                await Task.Delay(100);
                await provider.DisposeAsync();
            });
            begin.Set();
            var waiting = provider.WaitForCompletionAsync(started.Handle, watchdog.Token);
            await disposing.WaitAsync(TimeSpan.FromSeconds(6));
            var result = await waiting;
            Assert.IsType<CompletionResult.Terminated>(result);
        }
        finally { await provider.DisposeAsync(); try { Directory.Delete(ws, true); } catch { } }
    }

    [Fact]
    public async Task PrepareRacingDisposalCannotRegisterAUsableJob()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsExecutionProvider();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var requirements = new ExecutionRequirements(new BarrierRequirements(entered, release));
        try
        {
            var preparing = Task.Run(() => provider.Prepare(requirements));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            await provider.DisposeAsync();
            release.Set();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await preparing.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally { release.Set(); await provider.DisposeAsync(); }
    }

    [Fact]
    public async Task CancelledObserverPreservesNaturalOutcomeForLaterWait()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        await using var provider = new WindowsExecutionProvider();
        try
        {
            var started = Assert.IsType<LaunchResult.Started>(provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\" & \"{Ping}\" -n 3 127.0.0.1 > NUL & exit 17")));
            Assert.True(await Until(() => File.Exists(marker)));
            using var cancel = new CancellationTokenSource();
            var first = provider.WaitForCompletionAsync(started.Handle, cancel.Token).AsTask();
            cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first);
            var natural = Assert.IsType<CompletionResult.NaturalExit>(
                await provider.WaitForCompletionAsync(started.Handle).AsTask().WaitAsync(TimeSpan.FromSeconds(8)));
            Assert.Equal(17, natural.RootExitCode);
            var again = Assert.IsType<CompletionResult.NaturalExit>(
                await provider.WaitForCompletionAsync(started.Handle));
            Assert.Equal(17, again.RootExitCode);
            Assert.IsType<TerminateResult.Failed>(provider.Terminate(started.Handle));
        }
        finally { try { Directory.Delete(ws, true); } catch { } }
    }

    [Fact]
    public async Task TwoWaitersAndTerminateShareOneTerminalResult()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        await using var provider = new WindowsExecutionProvider();
        try
        {
            var started = Assert.IsType<LaunchResult.Started>(provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\" & \"{Ping}\" -n 20 127.0.0.1 > NUL")));
            Assert.True(await Until(() => File.Exists(marker)));
            var first = provider.WaitForCompletionAsync(started.Handle).AsTask();
            var second = provider.WaitForCompletionAsync(started.Handle).AsTask();
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.IsType<TerminateResult.Terminated>(provider.Terminate(started.Handle));
            var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(8));
            Assert.All(results, result => Assert.IsType<CompletionResult.Terminated>(result));
            Assert.IsType<CompletionResult.Terminated>(
                await provider.WaitForCompletionAsync(started.Handle));
            Assert.IsType<CompletionResult.Terminated>(
                await provider.WaitForCompletionAsync(started.Handle with { }));
            Assert.IsType<TerminateResult.Terminated>(provider.Terminate(started.Handle));
            await provider.DisposeAsync();
            await provider.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await provider.WaitForCompletionAsync(started.Handle));
        }
        finally { try { Directory.Delete(ws, true); } catch { } }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedAssignmentOrResumeCannotRunTarget(bool assignment)
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        await using var provider = new WindowsExecutionProvider();
        try
        {
            provider.FailAssignmentForTest = assignment;
            provider.FailResumeForTest = !assignment;
            var result = provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\""));
            Assert.IsType<LaunchResult.Failed>(result);
            Assert.False(await Until(() => File.Exists(marker), 500));
        }
        finally { try { Directory.Delete(ws, true); } catch { } }
    }

    [Fact]
    public async Task CleanupUncertaintyRemainsOwnedAndDisposeCanRetry()
    {
        if (!OperatingSystem.IsWindows()) return;
        string ws = Workspace(), marker = Path.Combine(ws, "ran-" + Guid.NewGuid().ToString("N"));
        var provider = new WindowsExecutionProvider
        {
            FailAssignmentForTest = true,
            CleanupConfirmationFailuresForTest = 2,
        };
        try
        {
            var failed = Assert.IsType<LaunchResult.Failed>(provider.Launch(Prep(provider),
                Child(ws, $"echo ran > \"{marker}\"")));
            Assert.Contains(failed.Reasons, reason => reason.Contains("cleanup unconfirmed"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await provider.DisposeAsync());
            await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(File.Exists(marker));
        }
        finally { try { await provider.DisposeAsync(); } catch { } try { Directory.Delete(ws, true); } catch { } }
    }

    private sealed class BarrierDictionary(ManualResetEventSlim entered, ManualResetEventSlim release)
        : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => throw new KeyNotFoundException();
        public IEnumerable<string> Keys => Array.Empty<string>();
        public IEnumerable<string> Values => Array.Empty<string>();
        public int Count => 0;
        public bool ContainsKey(string key) => false;
        public bool TryGetValue(string key, out string value) { value = ""; return false; }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(7))) throw new TimeoutException();
            return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class BarrierRequirements(ManualResetEventSlim entered, ManualResetEventSlim release)
        : IReadOnlyList<ExecutionRequirement>
    {
        public int Count => 1;
        public ExecutionRequirement this[int index] =>
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination);
        public IEnumerator<ExecutionRequirement> GetEnumerator()
        {
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(7))) throw new TimeoutException();
            yield return this[0];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
