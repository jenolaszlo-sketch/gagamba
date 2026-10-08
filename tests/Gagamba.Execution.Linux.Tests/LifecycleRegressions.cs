using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Gagamba.Execution;
using Gagamba.Execution.Linux;
using Xunit;
#pragma warning disable xUnit1051 // These outer watchdogs are independent of the provider's observation token.

namespace Gagamba.Execution.Linux.Tests;

public sealed class LifecycleRegressions
{
    private sealed class Scope : IAsyncDisposable
    {
        public readonly string Parent = Path.Combine(
            Environment.GetEnvironmentVariable("GAGAMBA_TEST_CGROUP_ROOT") ?? "/sys/fs/cgroup",
            "gagamba-ar2-" + Guid.NewGuid().ToString("N"));
        public readonly string Workspace = Path.Combine(Path.GetTempPath(), "gagamba-ar2-" + Guid.NewGuid().ToString("N"));
        public LinuxExecutionProvider Provider = null!;
        public bool ExpectDisposeFailure;

        public Scope()
        {
            if (!OperatingSystem.IsLinux()) Assert.Skip("Linux native test");
            try { Directory.CreateDirectory(Parent); }
            catch { Assert.Skip("Writable cgroup v2 delegation required"); }
            if (!File.Exists(Path.Combine(Parent, "cgroup.kill")))
                Assert.Skip("cgroup v2 kill unavailable");
            Directory.CreateDirectory(Workspace);
            Provider = new LinuxExecutionProvider(Parent);
        }

        public PreparedExecution Prepare()
        {
            var result = Provider.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>()));
            if (result is PrepareResult.Rejected rejected)
                Assert.Fail("Prepare rejected: " + string.Join("; ", rejected.Reasons));
            return Assert.IsType<PrepareResult.Accepted>(result).Prepared;
        }

        public ExecutionHandle Launch(string script)
        {
            var result = Provider.Launch(Prepare(), new ProcessStartSpec(
                "/bin/sh", "-c \"" + script + "\"", Workspace,
                new Dictionary<string, string> { ["PATH"] = "/usr/bin:/bin" }));
            return Assert.IsType<LaunchResult.Started>(result).Handle;
        }

        public async ValueTask DisposeAsync()
        {
            Exception? providerFailure = null;
            try { await Provider.DisposeAsync(); }
            catch (Exception ex) { providerFailure = ex; }
            try
            {
                if (Directory.Exists(Parent))
                {
                    try { File.WriteAllText(Path.Combine(Parent, "cgroup.kill"), "1"); } catch { }
                    await Until(() =>
                    {
                        try { return File.ReadAllText(Path.Combine(Parent, "cgroup.events")).Contains("populated 0"); }
                        catch { return false; }
                    }, 5000);
                    foreach (string dir in Directory.EnumerateDirectories(Parent, "*", SearchOption.AllDirectories)
                        .OrderByDescending(path => path.Length))
                        try { Directory.Delete(dir); } catch { }
                    Directory.Delete(Parent);
                }
            }
            finally { try { Directory.Delete(Workspace, true); } catch { } }
            if (providerFailure is not null && !ExpectDisposeFailure)
                throw new InvalidOperationException("unexpected provider disposal fault", providerFailure);
        }
    }

    private static async Task<bool> Until(Func<bool> condition, int milliseconds)
    {
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds)
        {
            if (condition()) return true;
            await Task.Delay(25);
        }
        return condition();
    }

    private static int RootPid(LinuxExecutionProvider provider)
    {
        var field = typeof(LinuxExecutionProvider).GetField("_executions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var records = (IDictionary)field.GetValue(provider)!;
        object record = records.Values.Cast<object>().Single();
        return (int)record.GetType().GetProperty("RootPid")!.GetValue(record)!;
    }

    [Fact]
    public async Task E8_CancelAfterRootReapPreservesExitCode()
    {
        await using var scope = new Scope();
        string root = Path.Combine(scope.Workspace, "root-" + Guid.NewGuid().ToString("N"));
        string child = Path.Combine(scope.Workspace, "child-" + Guid.NewGuid().ToString("N"));
        var handle = scope.Launch($"/bin/sh -c 'printf child > {child}; sleep 10' & printf root > {root}; exit 17");
        int pid = RootPid(scope.Provider);
        using var cancellation = new CancellationTokenSource();
        var first = scope.Provider.WaitForCompletionAsync(handle, cancellation.Token).AsTask();
        Assert.True(await Until(() => File.Exists(root) && File.Exists(child) && !Directory.Exists("/proc/" + pid), 5000),
            "root was not demonstrably reaped while the child remained active");
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first);
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var resumed = await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token);
        Assert.Equal(17, Assert.IsType<CompletionResult.NaturalExit>(resumed).RootExitCode);
    }

    [Fact]
    public async Task E9_UnrelatedInheritableFileDescriptorIsClosedInChild()
    {
        await using var scope = new Scope();
        string sentinel = Path.Combine(scope.Workspace, "sentinel-" + Guid.NewGuid().ToString("N"));
        string ran = Path.Combine(scope.Workspace, "ran-" + Guid.NewGuid().ToString("N"));
        int fd = Native.open(sentinel, 2 | 64 | 512, 384);
        Assert.True(fd >= 3, "sentinel descriptor not opened");
        try
        {
            Assert.Equal(0, Native.fcntl(fd, 2, 0)); // clear FD_CLOEXEC explicitly
            Assert.Equal(4, Native.write(fd, "host", 4));
            var handle = scope.Launch($"printf ran > {ran}; printf inherited > /proc/self/fd/{fd} 2>/dev/null");
            using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token);
            Assert.True(File.Exists(ran), "positive-control child did not run");
            Assert.DoesNotContain("inherited", File.ReadAllText(sentinel));
            Assert.Equal(6, Native.write(fd, "parent", 6)); // parent descriptor remains usable
        }
        finally { Native.close(fd); }
        Assert.True(File.ReadAllText(sentinel).Contains("parent"), "parent descriptor was altered");
    }

    [Fact]
    public async Task E9_InheritablePipeAndSocketAreClosedInChild()
    {
        await using var scope = new Scope();
        if (!File.Exists("/usr/bin/python3")) Assert.Skip("Python native FD probe unavailable");
        var pipe = new int[2];
        var socket = new int[2];
        Assert.Equal(0, Native.pipe2(pipe, 0));
        try
        {
            Assert.Equal(0, Native.socketpair(1, 1, 0, socket));
            try
            {
                Assert.Equal(0, Native.fcntl(pipe[1], 2, 0));
                Assert.Equal(0, Native.fcntl(socket[1], 2, 0));
                Assert.True(Native.fcntl(pipe[0], 4, 2048) >= 0);
                Assert.True(Native.fcntl(socket[0], 4, 2048) >= 0);
                string marker = Path.Combine(scope.Workspace, "ran-" + Guid.NewGuid().ToString("N"));
                string code = $"import os; open('{marker}','w').write('ran'); [os.write(fd,b'inherited') for fd in [{pipe[1]},{socket[1]}] if os.path.exists('/proc/self/fd/'+str(fd))]";
                var result = scope.Provider.Launch(scope.Prepare(), new ProcessStartSpec(
                    "/usr/bin/python3", "-c \"" + code + "\"", scope.Workspace,
                    new Dictionary<string, string>()));
                var handle = Assert.IsType<LaunchResult.Started>(result).Handle;
                using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                Assert.Equal(0, Assert.IsType<CompletionResult.NaturalExit>(
                    await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token)).RootExitCode);
                Assert.True(File.Exists(marker), "positive-control target did not run");
                var buffer = new byte[32];
                Assert.True(Native.read(pipe[0], buffer, (nuint)buffer.Length) <= 0,
                    "child wrote to inherited pipe");
                Assert.True(Native.read(socket[0], buffer, (nuint)buffer.Length) <= 0,
                    "child wrote to inherited socket");
                Assert.Equal(6, Native.write(pipe[1], "parent", 6));
                Assert.Equal(6, Native.read(pipe[0], buffer, (nuint)buffer.Length));
                Assert.Equal(6, Native.write(socket[1], "parent", 6));
                Assert.Equal(6, Native.read(socket[0], buffer, (nuint)buffer.Length));
            }
            finally { Native.close(socket[0]); Native.close(socket[1]); }
        }
        finally { Native.close(pipe[0]); Native.close(pipe[1]); }
    }

    [Fact]
    public async Task DisposalWinsBeforeAnAdmittedLaunchCanSpawn()
    {
        await using var scope = new Scope();
        string ran = Path.Combine(scope.Workspace, "ran-" + Guid.NewGuid().ToString("N"));
        var prep = scope.Prepare();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var environment = new BarrierDictionary(entered, release);
        var launching = Task.Run(() => scope.Provider.Launch(prep,
            new ProcessStartSpec("/bin/sh", $"-c \"printf ran > {ran}\"",
                scope.Workspace, environment)));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)), "launch did not reach the owned transition");
            var disposing = scope.Provider.DisposeAsync().AsTask();
            Assert.Throws<ObjectDisposedException>(() =>
                scope.Provider.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>())));
            release.Set();
            await disposing.WaitAsync(TimeSpan.FromSeconds(8));
            Assert.IsType<LaunchResult.Failed>(await launching.WaitAsync(TimeSpan.FromSeconds(8)));
            Assert.False(await Until(() => File.Exists(ran), 500), "target ran after disposal won");
        }
        finally { release.Set(); }
    }

    [Fact]
    public async Task ConcurrentFirstPrepareRunsOnePlacementProbe()
    {
        await using var scope = new Scope();
        var prepared = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Task.Run(scope.Prepare)));
        Assert.Equal(1, scope.Provider.PlacementProbeRunsForTest);
        foreach (var token in prepared)
            Assert.IsType<DiscardResult.Discarded>(scope.Provider.Discard(token));
    }

    [Fact]
    public async Task ConcurrentAndLateWaitersShareOneReapedResult()
    {
        await using var scope = new Scope();
        var handle = scope.Launch("sleep 1; exit 23");
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var first = scope.Provider.WaitForCompletionAsync(handle, watchdog.Token).AsTask();
        var second = scope.Provider.WaitForCompletionAsync(handle, watchdog.Token).AsTask();
        var results = await Task.WhenAll(first, second);
        Assert.All(results, result => Assert.Equal(23,
            Assert.IsType<CompletionResult.NaturalExit>(result).RootExitCode));
        Assert.Equal(23, Assert.IsType<CompletionResult.NaturalExit>(
            await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token)).RootExitCode);
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task DisposalDuringWaitCompletesTheOwnedObservation()
    {
        await using var scope = new Scope();
        var handle = scope.Launch("sleep 10");
        var waiter = scope.Provider.WaitForCompletionAsync(handle).AsTask();
        await scope.Provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(8));
        Assert.IsType<CompletionResult.Terminated>(await waiter.WaitAsync(TimeSpan.FromSeconds(8)));
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task MissingNativeSymbolRejectsStablyBeforeProbe()
    {
        await using var scope = new Scope();
        scope.Provider.MissingSymbolForTest = "posix_spawn_file_actions_addclosefrom_np";
        var requirements = new ExecutionRequirements(Array.Empty<ExecutionRequirement>());
        var first = Assert.IsType<PrepareResult.Rejected>(scope.Provider.Prepare(requirements));
        var second = Assert.IsType<PrepareResult.Rejected>(scope.Provider.Prepare(requirements));
        Assert.Equal(first.Reasons, second.Reasons);
        Assert.Contains("addclosefrom", first.Reasons.Single());
        Assert.Equal(0, scope.Provider.PlacementProbeRunsForTest);
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task UnsupportedKernelPlacementRejectsStably()
    {
        await using var scope = new Scope();
        scope.Provider.ProbeSpawnErrnoForTest = 95; // EOPNOTSUPP
        var requirements = new ExecutionRequirements(Array.Empty<ExecutionRequirement>());
        var first = Assert.IsType<PrepareResult.Rejected>(scope.Provider.Prepare(requirements));
        var second = Assert.IsType<PrepareResult.Rejected>(scope.Provider.Prepare(requirements));
        Assert.Equal(first.Reasons, second.Reasons);
        Assert.Contains("EOPNOTSUPP", first.Reasons.Single());
        Assert.Equal(1, scope.Provider.PlacementProbeRunsForTest);
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task DeniedKillIsReportedAndCanBeRetried()
    {
        await using var scope = new Scope();
        var handle = scope.Launch("sleep 10");
        scope.Provider.DenyCgroupOperationForTest = "kill";
        var refused = Assert.IsType<TerminateResult.Failed>(scope.Provider.Terminate(handle));
        Assert.Contains("injected denial", refused.Reasons.Single());
        scope.Provider.DenyCgroupOperationForTest = null;
        Assert.IsType<TerminateResult.Terminated>(scope.Provider.Terminate(handle));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        Assert.IsType<CompletionResult.Terminated>(await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token));
    }

    [Fact]
    public async Task DisposalReportsDeniedKillWithoutHangingAndRetries()
    {
        await using var scope = new Scope();
        var handle = scope.Launch("sleep 10");
        int pid = RootPid(scope.Provider);
        var waiter = scope.Provider.WaitForCompletionAsync(handle).AsTask();
        scope.Provider.DenyCgroupOperationForTest = "kill";
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.Provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(8)));
        Assert.Contains("cgroup.kill fault", failure.Message);
        scope.Provider.DenyCgroupOperationForTest = null;
        await scope.Provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(8));
        Assert.IsType<CompletionResult.Terminated>(await waiter.WaitAsync(TimeSpan.FromSeconds(8)));
        Assert.False(Directory.Exists("/proc/" + pid), "root was not reaped before disposal retry returned");
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task DeniedEventsReadIsFailedNotEmpty()
    {
        await using var scope = new Scope();
        var handle = scope.Launch("sleep 1");
        scope.Provider.DenyCgroupOperationForTest = "read";
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var failure = Assert.IsType<CompletionResult.Failed>(
            await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token));
        Assert.Contains("cgroup.events read fault", failure.Reasons.Single());
        scope.Provider.DenyCgroupOperationForTest = null;
        var disposal = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.Provider.DisposeAsync());
        Assert.Contains("cgroup.events read fault", disposal.Message);
        scope.ExpectDisposeFailure = true;
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task DeniedRemovalIsVisibleAndRetryable()
    {
        await using var scope = new Scope();
        var prepared = scope.Prepare();
        scope.Provider.DenyCgroupOperationForTest = "remove";
        var failure = Assert.IsType<DiscardResult.Failed>(scope.Provider.Discard(prepared));
        Assert.Contains("remove fault", failure.Reasons.Single());
        Assert.Single(Directory.EnumerateDirectories(scope.Parent));
        scope.Provider.DenyCgroupOperationForTest = null;
        await scope.Provider.DisposeAsync();
        Assert.Empty(Directory.EnumerateDirectories(scope.Parent));
    }

    [Fact]
    public async Task E10_RootWslSiblingMigrationIsAnObservedContainmentLimit()
    {
        await using var scope = new Scope();
        if (Native.geteuid() != 0) Assert.Skip("root WSL migration qualification only");
        string sibling = Path.Combine(scope.Parent, "sibling");
        Directory.CreateDirectory(sibling);
        string migrated = Path.Combine(scope.Workspace, "migrated");
        string survived = Path.Combine(scope.Workspace, "survived");
        string script = Path.Combine(scope.Workspace, "migrate.sh");
        File.WriteAllText(script, $"#!/bin/sh\necho $$ > {sibling}/cgroup.procs\nprintf yes > {migrated}\nsleep 2\nprintf yes > {survived}\n");
        var handle = scope.Launch($"/bin/sh {script} & /bin/sleep 15");
        Assert.True(await Until(() => File.Exists(migrated), 5000), "sibling migration was not demonstrated");
        Assert.IsType<TerminateResult.Terminated>(scope.Provider.Terminate(handle));
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        Assert.IsType<CompletionResult.Terminated>(await scope.Provider.WaitForCompletionAsync(handle, watchdog.Token));
        Assert.True(await Until(() => File.Exists(survived), 4000),
            "expected root-privileged sibling escape was not observable");
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

    private static class Native
    {
        [DllImport("libc", SetLastError = true)] public static extern int open(string path, int flags, int mode);
        [DllImport("libc", SetLastError = true)] public static extern int fcntl(int fd, int command, int value);
        [DllImport("libc", SetLastError = true)] public static extern nint write(int fd, string buffer, nuint count);
        [DllImport("libc")] public static extern int close(int fd);
        [DllImport("libc")] public static extern uint geteuid();
        [DllImport("libc", SetLastError = true)] public static extern int pipe2(int[] fds, int flags);
        [DllImport("libc", SetLastError = true)] public static extern int socketpair(int domain, int type, int protocol, int[] fds);
        [DllImport("libc", SetLastError = true)] public static extern nint read(int fd, byte[] buffer, nuint count);
    }
}
