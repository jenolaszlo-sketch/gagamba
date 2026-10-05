// GM-2 macOS provider acceptance: production launchd semantics from
// GM-1A, carried as provider tests. Environment first (granted present,
// ambient absent), then lifecycle: bootout owns the domain, root exit and
// disposal clean same-PG survivors, failures leave nothing, handles stay
// opaque, and a setsid escapee is OBSERVED alive (M8 truth preserved:
// same-PG dies, the escapee does not).
//
// Runs only on macOS with bootstrap rights; everywhere else every test
// passes vacuously. Heartbeat liveness only: no PIDs cross the tests.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.MacOS;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class ProviderTests : IAsyncLifetime
{
    private static string Domain =>
        OperatingSystem.IsMacOS() ? $"gui/{Launchd.getuid()}" : "gui/0";
    private static bool? _usable;
    private readonly string _ws =
        Path.Combine(Path.GetTempPath(), "gagamba-macos-tests", Guid.NewGuid().ToString("N"));
    private MacOsExecutionProvider _provider = new(Domain);

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_ws);
        await File.WriteAllTextAsync(Path.Combine(_ws, "chain.sh"),
            "#!/bin/sh\n" +
            "dir=\"$1\"\nbase=\"$2\"\ndepth=\"$3\"\n" +
            "while true; do date +%s > \"$dir/$base\"; sleep 1; done &\n" +
            "if [ \"$depth\" -gt 0 ]; then\n" +
            "  sh \"$dir/chain.sh\" \"$dir\" \"level$depth.lock\" $(($depth - 1))\n" +
            "else\n" +
            "  while true; do sleep 60; done\n" +
            "fi\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "exit-root.sh"),
            "#!/bin/sh\ndir=\"$1\"\n" +
            "sh -c \"while true; do date +%s > \\\"$dir/L4.lock\\\"; sleep 1; done\" &\n" +
            "sleep 2\nexit 0\n");
        // Escape target (python3: macOS ships no setsid(1)). The launchd
        // job root is ALREADY a session leader, so setsid must come from a
        // non-leader descendant (GM-1A method note): root spawns a same-PG
        // leaf and a same-PG escaper; the escaper leaves the session. After
        // bootout the same-PG leaf must die while the escaper observably
        // survives (M8). All roles exit on the stopfile (test cleanup).
        await File.WriteAllTextAsync(Path.Combine(_ws, "esc.py"),
            "import os, sys, time\n" +
            "d = sys.argv[1]\n" +
            "F = os.path.abspath(__file__)\n" +
            "stopfile = os.path.join(d, 'stopfile')\n" +
            "mode = sys.argv[2]\n" +
            "\n" +
            "def beat(name):\n" +
            "    with open(os.path.join(d, name), 'w') as f:\n" +
            "        f.write('%d\\n' % int(time.time()))\n" +
            "\n" +
            "if mode == 'root':\n" +
            "    with open(os.path.join(d, 'ids-root.txt'), 'w') as f:\n" +
            "        f.write('sid=%d\\n' % os.getsid(0))\n" +
            "    os.spawnl(os.P_NOWAIT, sys.executable, sys.executable, F, d, 'leaf')\n" +
            "    os.spawnl(os.P_NOWAIT, sys.executable, sys.executable, F, d, 'escaper')\n" +
            "    while not os.path.exists(stopfile):\n" +
            "        time.sleep(0.2)\n" +
            "elif mode == 'leaf':\n" +
            "    while not os.path.exists(stopfile):\n" +
            "        beat('hb-leaf'); time.sleep(1)\n" +
            "elif mode == 'escaper':\n" +
            "    deadline = time.time() + 15\n" +
            "    while time.time() < deadline and not os.path.exists(os.path.join(d, 'hb-leaf')):\n" +
            "        time.sleep(0.1)\n" +
            "    try:\n" +
            "        os.setsid()\n" +
            "    except OSError as ex:\n" +
            "        with open(os.path.join(d, 'esc-error.txt'), 'w') as f:\n" +
            "            f.write('%s\\n' % ex)\n" +
            "        sys.exit(3)\n" +
            "    with open(os.path.join(d, 'ids-esc.txt'), 'w') as f:\n" +
            "        f.write('sid=%d\\n' % os.getsid(0))\n" +
            "    while not os.path.exists(stopfile):\n" +
            "        beat('hb-esc'); time.sleep(1)\n");
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
            var (rc, _) = Launchd.Print(Domain);
            ok = rc == 0;
        }
        catch { ok = false; }
        _usable = ok;
        return ok;
    }

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
        var sw = Stopwatch.StartNew();
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
        // macOS grants UnitTermination as Partial/Native (PG-scoped; no
        // subtree primitive) and SurvivesRootExit Full. Requiring Full
        // native termination would (correctly) be rejected.
        var prep = _provider.Prepare(new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination, CapabilityLevel.Partial),
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

    // ---- Environment first: granted present, ambient absent ----

    [Fact]
    public async Task ExplicitEnvironmentIsPresent()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        const string marker = "GM2_PROBE_XYZ";
        var h = MustLaunch("/bin/sh", "-c \"env > env-out.tmp; mv env-out.tmp env-out.txt\"",
            new Dictionary<string, string> { [marker] = "hello-mac" });
        Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "env-out.txt")), 15000),
            "env dump never appeared");
        string content = File.ReadAllText(Path.Combine(_ws, "env-out.txt"));
        Assert.Contains("hello-mac", content);
        _ = h;
    }

    [Fact]
    public async Task AmbientEnvironmentIsAbsent()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        const string ambient = "GM2_AMBIENT_XYZ";
        Environment.SetEnvironmentVariable(ambient, "ambient-value");
        try
        {
            var h = MustLaunch("/bin/sh", "-c \"env > env-amb.tmp; mv env-amb.tmp env-amb.txt\"",
                new Dictionary<string, string> { ["GM2_GRANTED_XYZ"] = "yes" });
            Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "env-amb.txt")), 15000),
                "env dump never appeared");
            string content = File.ReadAllText(Path.Combine(_ws, "env-amb.txt"));
            Assert.Contains("GM2_GRANTED_XYZ=yes", content);
            Assert.DoesNotContain("ambient-value", content);
            _ = h;
        }
        finally
        {
            Environment.SetEnvironmentVariable(ambient, null);
        }
    }

    [Fact]
    public async Task WorkingDirectoryIsHonored()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", "-c \"pwd > pwd.tmp; mv pwd.tmp pwd.txt\"");
        Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "pwd.txt")), 15000),
            "pwd dump never appeared (wrong cwd?)");
        string actual = File.ReadAllText(Path.Combine(_ws, "pwd.txt")).Trim();
        string expected = RealPath(_ws);
        Assert.Equal(expected, actual);
        _ = h;
    }

    private static string RealPath(string path)
    {
        try
        {
            var psi = new ProcessStartInfo("realpath", "")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(path);
            using var p = Process.Start(psi);
            if (p is null) return path;
            string out_ = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(10000);
            return p.ExitCode == 0 && out_.Length > 0 ? out_ : path;
        }
        catch { return path; }
    }

    // ---- Lifecycle ----

    [Fact]
    public async Task RootLaunch_BootoutWorks()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", "-c \"echo alive > marker.txt; sleep 30\"");
        Assert.True(await PollAsync(() => File.Exists(Path.Combine(_ws, "marker.txt")), 15000),
            "marker never appeared");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
    }

    [Fact]
    public async Task Tree_BootoutKillsAllThreeLevels()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/chain.sh\" \"{_ws}\" root.lock 2");
        string[] beats = { "root.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => beats.All(b => Fresh(Path.Combine(_ws, b))), 15000),
            "tree heartbeats never all fresh");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        foreach (string b in beats)
            Assert.True(await FrozenAsync(Path.Combine(_ws, b)), $"{b} kept beating after bootout");
    }

    [Fact]
    public async Task RootExit_HandleStaysValid_BootoutKillsChild()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/exit-root.sh\" \"{_ws}\"");
        Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "L4.lock")), 15000),
            "child heartbeat never fresh after root exit");
        // Root is long gone; the opaque handle must still terminate.
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.True(await FrozenAsync(Path.Combine(_ws, "L4.lock")), "survivor outlived bootout");
    }

    [Fact]
    public async Task TerminateIsIdempotent()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", "-c \"sleep 30\"");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
    }

    [Fact]
    public async Task DisposeCleansLiveTree()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var h = MustLaunch("/bin/sh", $"\"{_ws}/chain.sh\" \"{_ws}\" root.lock 2");
        string[] beats = { "root.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => beats.All(b => Fresh(Path.Combine(_ws, b))), 15000),
            "tree heartbeats never all fresh");
        await _provider.DisposeAsync();
        foreach (string b in beats)
            Assert.True(await FrozenAsync(Path.Combine(_ws, b)), $"{b} survived provider disposal");
        _provider = new MacOsExecutionProvider(Domain);
    }

    [Fact]
    public async Task FailedLaunchLeavesNothing()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        var prep = MustPrepareSimple();
        // Readiness (never reaches running) is what fails this closed:
        // bootstrap accepts any plist, kickstart may too. Slow by design.
        var failed = _provider.Launch(prep, new ProcessStartSpec(
            "/nonexistent-xyz-123/bin/nope", "", _ws, new Dictionary<string, string>()));
        var f = Assert.IsType<LaunchResult.Failed>(failed);
        Assert.NotEmpty(f.Reasons);
        Assert.False(File.Exists(Path.Combine(_ws, "should-not-exist.txt")),
            "target ran despite failure");
        // No loaded job may remain: best-effort list check.
        try
        {
            var psi = new ProcessStartInfo("launchctl", "")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("list");
            using var p = Process.Start(psi);
            if (p is not null)
            {
                string out_ = p.StandardOutput.ReadToEnd();
                p.WaitForExit(15000);
                if (p.ExitCode == 0)
                    Assert.DoesNotContain("org.gagamba.exec.", out_);
            }
        }
        catch { }
    }

    [Fact]
    public async Task SetsidEscapeIsObservedAliveNotKilled()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
        if (!File.Exists("/usr/bin/python3")) return;
        var h = MustLaunch("/usr/bin/python3", $"\"{_ws}/esc.py\" \"{_ws}\" root");
        try
        {
            Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "hb-esc")), 15000),
                "escaper heartbeat never fresh (setsid from non-leader expected)");
            Assert.False(File.Exists(Path.Combine(_ws, "esc-error.txt")),
                "setsid failed: " + (File.Exists(Path.Combine(_ws, "esc-error.txt"))
                    ? File.ReadAllText(Path.Combine(_ws, "esc-error.txt")) : ""));
            string escIds = File.ReadAllText(Path.Combine(_ws, "ids-esc.txt"));
            string rootIds = File.ReadAllText(Path.Combine(_ws, "ids-root.txt"));
            Assert.NotEqual(SidOf(rootIds), SidOf(escIds));
            Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(h));
            // M8 truth: the same-PG leaf dies with the domain, but the
            // session escapee observably survives it — escape, not kill.
            Assert.True(await FrozenAsync(Path.Combine(_ws, "hb-leaf")),
                "same-PG leaf survived bootout");
            Assert.True(await PollAsync(() => Fresh(Path.Combine(_ws, "hb-esc")), 10000),
                "escapee died: escape misreported as kill");
        }
        finally
        {
            try { File.WriteAllText(Path.Combine(_ws, "stopfile"), "stop"); } catch { }
        }
        _ = h;
    }

    [Fact]
    public async Task TokensFailClosed()
    {
        if (!OperatingSystem.IsMacOS() || !Usable()) return;
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
    public void ProviderSurfaceStaysOpaque()
    {
        var banned = new[] { "Pid", "JobHandle", "Cgroup", "Pgid", "Hwnd", "JobObject", "Launchd", "ProcessGroup", "Label", "Plist" };
        var offenders = new List<string>();
        foreach (var t in typeof(PlatformCapabilities).Assembly.GetTypes()
                     .Concat(typeof(MacOsExecutionProvider).Assembly.GetTypes())
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
