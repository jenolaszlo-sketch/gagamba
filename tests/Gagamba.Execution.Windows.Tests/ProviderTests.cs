// GW-2 provider tests: the production path preserves GQ-1 semantics.
// No PIDs anywhere: trees prove life/death through exclusive file locks
// (a live holder denies our open; a dead tree releases all three).
// Windows-only binaries; vacuous pass elsewhere. Bounded polls only.
using Gagamba.Execution;
using Gagamba.Execution.Windows;
using Xunit;

namespace Gagamba.Execution.Windows.Tests;

public sealed class ProviderTreeTests : IAsyncLifetime
{
    private string _ws = "";
    private WindowsExecutionProvider _provider = new();

    private static string CmdExe =>
        Path.Combine(Environment.SystemDirectory, "cmd.exe");

    private static string PwshExe => Path.Combine(
        Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

    public async ValueTask InitializeAsync()
    {
        _ws = Path.Combine(Path.GetTempPath(), "gw2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ws);
        // holder.ps1: hold one named lock, sleep. chain.ps1: hold own lock,
        // spawn the next level down, sleep. exit-root.ps1: spawn a holder,
        // exit immediately (root gone, child remains).
        await File.WriteAllTextAsync(Path.Combine(_ws, "holder.ps1"),
            "param([string]$Dir, [string]$Lock)\n" +
            "$f=[System.IO.File]::Open((Join-Path $Dir $Lock),'OpenOrCreate','Write','None')\n" +
            "Start-Sleep -Seconds 30\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "chain.ps1"),
            "param([string]$Dir, [string]$Lock, [int]$Depth)\n" +
            "$f=[System.IO.File]::Open((Join-Path $Dir $Lock),'OpenOrCreate','Write','None')\n" +
            "if ($Depth -gt 0) { Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $Dir 'chain.ps1'),'-Dir',$Dir,'-Lock',(\"level$Depth.lock\"),'-Depth',($Depth - 1) -WindowStyle Hidden }\n" +
            "Start-Sleep -Seconds 30\n");
        await File.WriteAllTextAsync(Path.Combine(_ws, "exit-root.ps1"),
            "param([string]$Dir)\n" +
            "Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $Dir 'holder.ps1'),'-Dir',$Dir,'-Lock','L4.lock' -WindowStyle Hidden\n");
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        try { Directory.Delete(_ws, recursive: true); } catch { }
    }

    private PreparedExecution MustPrepare()
    {
        var prep = _provider.Prepare(new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination),
            ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit),
            ExecutionRequirement.Require(ExecutionCapability.EscapeResistant),
        }));
        return Assert.IsType<PrepareResult.Accepted>(prep).Prepared;
    }

    private static bool LockHeld(string path)
    {
        try
        {
            using var _ = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (FileNotFoundException) { return false; }
        catch (IOException) { return true; }
    }

    private static async Task<bool> PollAsync(Func<bool> cond, int ms = 8000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            bool ok = false;
            try { ok = cond(); } catch (IOException) { }
            if (ok) return true;
            await Task.Delay(100);
        }
        try { return cond(); } catch (IOException) { return false; }
    }

    private static Dictionary<string, string> TestEnv()
    {
        // Explicit-only environment (contract): the provider adds nothing,
        // so tools needing SystemRoot must declare it. No TEMP/TMP/PATH.
        return new Dictionary<string, string>
        {
            ["SYSTEMROOT"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            ["SYSTEMDRIVE"] = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\",
        };
    }

    private LaunchResult LaunchPs1(string script, string args)
    {
        var prep = MustPrepare();
        return _provider.Launch(prep, new ProcessStartSpec(
            PwshExe, $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.Combine(_ws, script)}\" {args}",
            _ws, TestEnv()));
    }

    [Fact]
    public async Task RootOnly_TerminateReleasesLock()
    {
        if (!OperatingSystem.IsWindows()) return;
        var started = Assert.IsType<LaunchResult.Started>(LaunchPs1("holder.ps1",
            $"-Dir \"{_ws}\" -Lock \"L0.lock\""));
        Assert.True(await PollAsync(() => LockHeld(Path.Combine(_ws, "L0.lock"))),
            "holder never took its lock");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(started.Handle));
        Assert.True(await PollAsync(() => !LockHeld(Path.Combine(_ws, "L0.lock"))),
            "lock still held after terminate");
    }

    [Fact]
    public async Task Tree_TerminateKillsAllThreeLevels()
    {
        if (!OperatingSystem.IsWindows()) return;
        var started = Assert.IsType<LaunchResult.Started>(LaunchPs1("chain.ps1",
            $"-Dir \"{_ws}\" -Lock \"root.lock\" -Depth 2"));
        string[] locks = { "root.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => locks.All(l => LockHeld(Path.Combine(_ws, l))), 15000),
            "tree never fully materialized");
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(started.Handle));
        Assert.True(await PollAsync(() => locks.All(l => !LockHeld(Path.Combine(_ws, l)))),
            "descendant survived terminate");
    }

    [Fact]
    public async Task RootExit_HandleStaysValid_TerminateKillsChild()
    {
        if (!OperatingSystem.IsWindows()) return;
        var started = Assert.IsType<LaunchResult.Started>(LaunchPs1("exit-root.ps1",
            $"-Dir \"{_ws}\""));
        Assert.True(await PollAsync(() => LockHeld(Path.Combine(_ws, "L4.lock")), 15000),
            "child never materialized after root exit");
        // Root is long gone; the handle must still terminate the survivor.
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(started.Handle));
        Assert.True(await PollAsync(() => !LockHeld(Path.Combine(_ws, "L4.lock"))),
            "survivor outlived terminate");
    }

    [Fact]
    public async Task TerminateIsIdempotent()
    {
        if (!OperatingSystem.IsWindows()) return;
        var started = Assert.IsType<LaunchResult.Started>(LaunchPs1("holder.ps1",
            $"-Dir \"{_ws}\" -Lock \"Li.lock\""));
        Assert.True(await PollAsync(() => LockHeld(Path.Combine(_ws, "Li.lock"))));
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(started.Handle));
        Assert.IsType<TerminateResult.Terminated>(_provider.Terminate(started.Handle));
    }

    [Fact]
    public async Task DisposeKillsLiveTreeWithoutTerminate()
    {
        if (!OperatingSystem.IsWindows()) return;
        var started = Assert.IsType<LaunchResult.Started>(LaunchPs1("chain.ps1",
            $"-Dir \"{_ws}\" -Lock \"droot.lock\" -Depth 2"));
        string[] locks = { "droot.lock", "level2.lock", "level1.lock" };
        Assert.True(await PollAsync(() => locks.All(l => LockHeld(Path.Combine(_ws, l))), 15000),
            "tree never fully materialized");
        await _provider.DisposeAsync();
        Assert.True(await PollAsync(() => locks.All(l => !LockHeld(Path.Combine(_ws, l)))),
            "tree survived provider disposal");
        _provider = new WindowsExecutionProvider();
    }

    [Fact]
    public async Task SingleUsePreparation()
    {
        if (!OperatingSystem.IsWindows()) return;
        var prep = MustPrepare();
        var first = _provider.Launch(prep, new ProcessStartSpec(
            CmdExe, "/d /c exit 0", _ws, new Dictionary<string, string>()));
        Assert.IsType<LaunchResult.Started>(first);
        var second = _provider.Launch(prep, new ProcessStartSpec(
            CmdExe, "/d /c exit 0", _ws, new Dictionary<string, string>()));
        var failed = Assert.IsType<LaunchResult.Failed>(second);
        Assert.Contains(failed.Reasons, r => r.Contains("single-use") || r.Contains("unknown preparation"));
    }

    [Fact]
    public async Task ForeignPreparationAndHandleFailClosed()
    {
        if (!OperatingSystem.IsWindows()) return;
        var foreignPrep = new PreparedExecution("other", Guid.NewGuid(), Array.Empty<string>());
        var badLaunch = _provider.Launch(foreignPrep, ProcessStartSpec.Simple(CmdExe));
        Assert.IsType<LaunchResult.Failed>(badLaunch);
        var badTerm = _provider.Terminate(new ExecutionHandle("other", Guid.NewGuid()));
        Assert.IsType<TerminateResult.Failed>(badTerm);
        var ownUnknown = _provider.Terminate(
            new ExecutionHandle(WellKnownPlatforms.Windows.Platform, Guid.NewGuid()));
        Assert.IsType<TerminateResult.Failed>(ownUnknown);
    }

    [Fact]
    public async Task ChildEnvironmentIsExclusive()
    {
        if (!OperatingSystem.IsWindows()) return;
        const string marker = "GW2_PROBE_XYZ";
        const string ambient = "GW2_AMBIENT_XYZ";
        Environment.SetEnvironmentVariable(ambient, "ambient-value");
        try
        {
            var prep = MustPrepare();
            var spec = new ProcessStartSpec(CmdExe, $"/d /c (set {marker}) > env-out.txt",
                _ws, new Dictionary<string, string> { [marker] = "hello-7" });
            var startedRaw = _provider.Launch(prep, spec);
            Assert.True(startedRaw is LaunchResult.Started,
                startedRaw is LaunchResult.Failed lf ? string.Join(" | ", lf.Reasons) : "unexpected");
            var started = (LaunchResult.Started)startedRaw;
            _ = started;
            string content = "";
            Assert.True(await PollAsync(() =>
            {
                try { content = File.ReadAllText(Path.Combine(_ws, "env-out.txt")); return true; }
                catch (IOException) { return false; }
            }), "env dump never appeared");
            Assert.Contains("hello-7", content);
            var prep2 = MustPrepare();
            var spec2 = new ProcessStartSpec(CmdExe, $"/d /c (set {ambient}) > env-out2.txt",
                _ws, new Dictionary<string, string> { ["UNRELATED"] = "1" });
            Assert.IsType<LaunchResult.Started>(_provider.Launch(prep2, spec2));
            string content2 = "";
            Assert.True(await PollAsync(() =>
            {
                try { content2 = File.ReadAllText(Path.Combine(_ws, "env-out2.txt")); return true; }
                catch (IOException) { return false; }
            }), "env dump 2 never appeared");
            Assert.DoesNotContain("ambient-value", content2);
            _ = started;
        }
        finally
        {
            Environment.SetEnvironmentVariable(ambient, null);
        }
    }

    [Fact]
    public async Task BadExecutableAndBadEnvironmentFailClosed()
    {
        if (!OperatingSystem.IsWindows()) return;
        var prep = MustPrepare();
        var bad = _provider.Launch(prep, new ProcessStartSpec(
            "C:\\nonexistent\\nope.exe", "", _ws, new Dictionary<string, string>()));
        Assert.IsType<LaunchResult.Failed>(bad);
        var prep2 = MustPrepare();
        var badEnv = _provider.Launch(prep2, new ProcessStartSpec(
            CmdExe, "", _ws, new Dictionary<string, string> { ["A=B"] = "x" }));
        var failed = Assert.IsType<LaunchResult.Failed>(badEnv);
        Assert.Contains(failed.Reasons, r => r.Contains("environment"));
    }

    [Fact]
    public void AssignDeadTargetFails()
    {
        if (!OperatingSystem.IsWindows()) return;
        var (job, err) = OwnedJobCreate();
        Assert.NotNull(job);
        try
        {
            using var sleeper = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(CmdExe, "/d /c ping -n 30 127.0.0.1 > NUL")
                { WorkingDirectory = _ws, UseShellExecute = false });
            Assert.NotNull(sleeper);
            sleeper.Kill();
            Assert.True(sleeper.WaitForExit(5000));
            bool ok = WindowsExecutionProvider.TryAssign(job!, sleeper.Handle, out int assignErr);
            Assert.False(ok);
            Assert.NotEqual(0, assignErr);
        }
        finally
        {
            job!.Dispose();
        }
    }

    private static (OwnedJob? Job, string Error) OwnedJobCreate() =>
        OwnedJob.Create();
}
