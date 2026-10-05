// GL-2 Linux provider tests. Behavior-based verification only: heartbeat
// files freeze when their writer dies (no PIDs cross the test boundary
// either). Needs Linux + a writable cgroup subtree; elsewhere (and where
// delegation is absent) tests pass vacuously with a comment — the dev WSL
// loop and a privileged runner execute them for real.
using Gagamba.Execution;
using Gagamba.Execution.Linux;
using Xunit;

namespace Gagamba.Execution.Linux.Tests;

public sealed class ProviderTests : IAsyncLifetime
{
    private const string Parent = "/sys/fs/cgroup/gagamba-gl2-tests";
    private static bool? _usable;
    private string _ws = "";
    private LinuxExecutionProvider _provider = new("/nonexistent");

    public async ValueTask InitializeAsync()
    {
        _ws = Path.Combine(Path.GetTempPath(), "gl2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ws);
        await File.WriteAllTextAsync(Path.Combine(_ws, "holder.sh"),
            "#!/bin/sh\ndir=\"$1\"; lock=\"$2\"\nwhile true; do date +%s > \"$dir/$lock\"; sleep 1; done\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "chain.sh"),
            "#!/bin/sh\ndir=\"$1\"; lock=\"$2\"; depth=\"$3\"\n" +
            "if [ \"$depth\" -gt 0 ]; then sh \"$dir/chain.sh\" \"$dir\" \"level$depth.lock\" $((depth - 1)) & fi\n" +
            "while true; do date +%s > \"$dir/$lock\"; sleep 1; done\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "exit-root.sh"),
            "#!/bin/sh\ndir=\"$1\"\nsh \"$dir/holder.sh\" \"$dir\" L4.lock &\nexit 0\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "esc-root.sh"),
            "#!/bin/sh\ndir=\"$1\"\npgid=$(ps -o pgid= -p $$ | tr -d ' '); sid=$(ps -o sid= -p $$ | tr -d ' ')\n" +
            "echo \"pgid=$pgid sid=$sid\" > \"$dir/ids-root.txt\"\ncat /proc/self/cgroup > \"$dir/cg-root.txt\"\n" +
            "setsid sh \"$dir/esc-inner.sh\" \"$dir\"\n" +
            "while true; do date +%s > \"$dir/hb-root\"; sleep 1; done\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "esc-inner.sh"),
            "#!/bin/sh\ndir=\"$1\"\npgid=$(ps -o pgid= -p $$ | tr -d ' '); sid=$(ps -o sid= -p $$ | tr -d ' ')\n" +
            "echo \"pgid=$pgid sid=$sid\" > \"$dir/ids-esc.txt\"\ncat /proc/self/cgroup > \"$dir/cg-esc.txt\"\n" +
            "while true; do date +%s > \"$dir/hb-esc\"; sleep 1; done\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "nested.sh"),
            "#!/bin/sh\ndir=\"$1\"\nCG=$(cut -d: -f3 /proc/self/cgroup)\n" +
            "mkdir -p \"/sys/fs/cgroup$CG/sub\"\necho $$ > \"/sys/fs/cgroup$CG/sub/cgroup.procs\"\n" +
            "sh \"$dir/holder.sh\" \"$dir\" hb-leaf &\n" +
            "while true; do date +%s > \"$dir/hb-root\"; sleep 1; done\n");
        _provider = new LinuxExecutionProvider(Parent);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try { await _provider.DisposeAsync(); } catch { }
        try { Directory.Delete(_ws, recursive: true); } catch { }
    }

    private static bool Usable()
    {
        if (_usable.HasValue) return _usable.Value;
        bool ok = false;
        try
        {
            Directory.CreateDirectory(Parent);
            string probe = Path.Combine(Parent, "gagamba-probe");
            Directory.CreateDirectory(probe);
            ok = File.Exists(Path.Combine(probe, "cgroup.kill"));
            try { Directory.Delete(probe); } catch { }
        }
        catch { ok = false; }
        _usable = ok;
        return ok;
    }

    // Heartbeat liveness: fresh = written within the window; frozen = mtime
    // unchanged across a 2.5s observation (death is permanent, so end-state
    // equality plus age proves the writer is gone).
    private static bool Fresh(string path, int maxAgeSec = 8)
    {
        try
        {
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
            return age.TotalSeconds is >= 0 and < 60 && age.TotalSeconds <= maxAgeSec;
        }
        catch { return false; }
    }

    private static async Task<bool> PollAsync(Func<bool> cond, int ms = 10000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            bool ok = false;
            try { ok = cond(); } catch { }
            if (ok) return true;
            await Task.Delay(200);
        }
        try { return cond(); } catch { return false; }
    }

    private static async Task<bool> FrozenAsync(string path, int observeMs = 2500)
    {
        try
        {
            if (!File.Exists(path)) return false;
            var t1 = File.GetLastWriteTimeUtc(path);
            await Task.Delay(observeMs);
            if (!File.Exists(path)) return false;
            return File.GetLastWriteTimeUtc(path) == t1;
        }
        catch { return false; }
    }

    private ExecutionHandle MustLaunch(string exe, string args, Dictionary<string, string>? env = null)
    {
        var prep = _provider.Prepare(new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination),
            ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit),
        }));
        var accepted = prep as PrepareResult.Accepted;
        Assert.True(accepted is not null,
            prep is PrepareResult.Rejected rej ? "rejected: " + string.Join(" | ", rej.Reasons) : "no prep");
        var launched = _provider.Launch(accepted!.Prepared, new ProcessStartSpec(
            exe, args, _ws, env ?? new Dictionary<string, string>()));
        var started = launched as LaunchResult.Started;
        Assert.True(started is not null,
            launched is LaunchResult.Failed f ? "failed: " + string.Join(" | ", f.Reasons) : "no launch");
        return started!.Handle;
    }

    private static ExecutionRequirements StrictAgent() => new(new[]
    {
        ExecutionRequirement.Require(ExecutionCapability.UnitTermination),
        ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit),
        ExecutionRequirement.Require(ExecutionCapability.EscapeResistant),
    });

    [Fact]
    public async Task RootLaunch_TerminateWorks()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", "-c \"echo alive > marker.txt; sleep 30\"");
        Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "marker.txt"))),
            "marker never appeared");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
    }

    [Fact]
    public async Task Tree_TerminateKillsAllThreeLevels()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/chain.sh\" \"{_ws}\" root.lock 2");
        string[] beats = { "root.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => beats.All(b => Fresh(Path.Combine(_ws, b))), 15000),
            "tree heartbeats never all fresh");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        foreach (string b in beats)
            Assert.True(await FrozenAsync(Path.Combine(_ws, b)), $"{b} kept beating after terminate");
    }

    [Fact]
    public async Task RootExit_HandleStaysValid_TerminateKillsChild()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/exit-root.sh\" \"{_ws}\"");
        Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "L4.lock")), 15000),
            "child heartbeat never fresh after root exit");
        // Root is long gone; the opaque handle must still terminate.
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.True(await FrozenAsync(Path.Combine(_ws, "L4.lock")), "survivor outlived terminate");
    }

    [Fact]
    public async Task TerminateIsIdempotent()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", "-c \"sleep 30\"");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
    }

    [Fact]
    public async Task DisposeKillsLiveTreeWithoutTerminate()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/chain.sh\" \"{_ws}\" root.lock 2");
        string[] beats = { "root.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => beats.All(b => Fresh(Path.Combine(_ws, b))), 15000),
            "tree heartbeats never all fresh");
        await _provider.DisposeAsync();
        foreach (string b in beats)
            Assert.True(await FrozenAsync(Path.Combine(_ws, b)), $"{b} survived provider disposal");
        _provider = new LinuxExecutionProvider(Parent);
    }

    [Fact]
    public async Task FailedPreparationLeavesNothing()
    {
        if (!OperatingSystem.IsLinux()) return;
        await using var bogus = new LinuxExecutionProvider("/nonexistent-xyz-123/");
        var rejected = Assert.IsType<PrepareResult.Rejected>(bogus.Prepare(StrictAgent()));
        Assert.NotEmpty(rejected.Reasons);
        var strict = _provider.Prepare(StrictAgent());
        var strictRejected = Assert.IsType<PrepareResult.Rejected>(strict);
        Assert.Contains(strictRejected.Reasons, r => r.Contains("EscapeResistant"));
    }

    [Fact]
    public async Task ChildEnvironmentIsExclusive()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        const string marker = "GW2_PROBE_XYZ";
        const string ambient = "GW2_AMBIENT_XYZ";
        Environment.SetEnvironmentVariable(ambient, "ambient-value");
        try
        {
            var h = MustLaunch("/bin/sh", "-c \"env > env-out.txt\"",
                new Dictionary<string, string> { [marker] = "hello-lin" });
            Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "env-out.txt"))),
                "env dump never appeared");
            string content = File.ReadAllText(Path.Combine(_ws, "env-out.txt"));
            Assert.Contains("hello-lin", content);
            Assert.DoesNotContain("ambient-value", content);
            _ = h;
        }
        finally
        {
            Environment.SetEnvironmentVariable(ambient, null);
        }
    }

    [Fact]
    public async Task TokensFailClosed()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var prep = MustPrepareSimple();
        var ok = _provider.Launch(prep, new ProcessStartSpec(
            "/bin/sh", "-c \"sleep 30\"", _ws, new Dictionary<string, string>()));
        Assert.IsType<LaunchResult.Started>(ok);
        var again = _provider.Launch(prep, new ProcessStartSpec(
            "/bin/sh", "-c \"sleep 30\"", _ws, new Dictionary<string, string>()));
        Assert.IsType<LaunchResult.Failed>(again);
        var foreign = new PreparedExecution("other", Guid.NewGuid(), Array.Empty<string>());
        Assert.IsType<LaunchResult.Failed>(_provider.Launch(foreign,
            new ProcessStartSpec("/bin/sh", "", _ws, new Dictionary<string, string>())));
        Assert.IsType<TerminateResult.Failed>(_provider.Terminate(
            new ExecutionHandle("other", Guid.NewGuid())));
    }

    [Fact]
    public async Task NestedCgroupKilledRecursively()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/nested.sh\" \"{_ws}\"");
        Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "hb-root")) && Fresh(Path.Combine(_ws, "hb-leaf")), 15000),
            "nested heartbeats never fresh");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.True(await FrozenAsync(Path.Combine(_ws, "hb-root")), "root survived");
        Assert.True(await FrozenAsync(Path.Combine(_ws, "hb-leaf")), "nested leaf survived");
    }

    [Fact]
    public async Task SetsidEscapeStaysInDomainAndDies()
    {
        if (!OperatingSystem.IsLinux() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/esc-root.sh\" \"{_ws}\"");
        Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "hb-esc")), 15000),
            "escaper heartbeat never fresh");
        string rootIds = File.ReadAllText(Path.Combine(_ws, "ids-root.txt"));
        string escIds = File.ReadAllText(Path.Combine(_ws, "ids-esc.txt"));
        string rootCg = File.ReadAllText(Path.Combine(_ws, "cg-root.txt")).Trim();
        string escCg = File.ReadAllText(Path.Combine(_ws, "cg-esc.txt")).Trim();
        Assert.NotEqual(SidOf(rootIds), SidOf(escIds));
        Assert.Equal(rootCg, escCg);
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.True(await FrozenAsync(Path.Combine(_ws, "hb-esc")), "escapee survived cgroup.kill");
    }

    [Fact]
    public void ProviderSurfaceStaysOpaque()
    {
        var banned = new[] { "Pid", "JobHandle", "Cgroup", "Pgid", "Hwnd", "JobObject", "Launchd", "ProcessGroup" };
        var offenders = new List<string>();
        foreach (var t in typeof(PlatformCapabilities).Assembly.GetTypes()
                     .Concat(typeof(LinuxExecutionProvider).Assembly.GetTypes())
                     .Where(t => t.IsPublic || t.IsNestedPublic))
        {
            foreach (var m in t.GetMembers(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.DeclaredOnly))
            {
                Type? ret = (m as System.Reflection.PropertyInfo)?.PropertyType
                    ?? (m as System.Reflection.FieldInfo)?.FieldType
                    ?? (m as System.Reflection.MethodInfo)?.ReturnType;
                if (ret is not null && (ret == typeof(IntPtr) || ret == typeof(UIntPtr)
                    || typeof(System.Runtime.InteropServices.SafeHandle).IsAssignableFrom(ret)
                    || ret == typeof(System.Diagnostics.Process)))
                    offenders.Add($"{t.Name}.{m.Name}:{ret.Name}");
                if (banned.Any(w => m.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
                    offenders.Add($"{t.Name}.{m.Name}");
            }
        }
        Assert.True(offenders.Count == 0, "leaked native identity: " + string.Join(", ", offenders));
    }

    private PreparedExecution MustPrepareSimple()
    {
        var prep = _provider.Prepare(new ExecutionRequirements(Array.Empty<ExecutionRequirement>()));
        return Assert.IsType<PrepareResult.Accepted>(prep).Prepared;
    }

    private static string SidOf(string ids)
    {
        foreach (var part in ids.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "sid") return kv[1];
        }
        return "?";
    }
}
