// One conformance runner over the frozen SPI. It measures the provider's
// actual behavior and compares to ConformanceMatrix; a drift fails loudly.
// Behavioral legs that cannot even prepare (e.g. Linux without a delegated
// cgroup) report Skipped with the reason, never a false pass.
using Gagamba.Execution;
using System.Runtime.InteropServices;

namespace Gagamba.Conformance;

public sealed record ConformanceOptions(string Workspace, int PollMilliseconds = 15000);

public static class ConformanceRunner
{
    private const string Marker = "CONF_GRANTED";
    private const string Ambient = "CONF_AMBIENT";

    public static async Task<ConformanceReport> RunAsync(
        Func<IExecutionProvider> factory, ConformanceOptions options)
    {
        Directory.CreateDirectory(options.Workspace);
        ConformanceOptions Leg(string name)
        {
            string workspace = Path.Combine(options.Workspace,
                name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workspace);
            ConformanceScripts.Write(workspace);
            return new ConformanceOptions(workspace, options.PollMilliseconds);
        }
        await using var inspectionProvider = factory();
        var caps = inspectionProvider.Describe();
        bool known = ConformanceMatrix.TryForPlatform(caps.Platform, out var expected);

        var legs = new List<ConformanceLeg>
        {
            CapabilityMatrix(caps, known ? expected : null),
            OpaqueHandles(inspectionProvider),
            await Prepare(factory),
            await SingleUse(factory, Leg("single-use")),
            await WorkingDirectory(factory, Leg("working-directory")),
            await NoAmbientInherit(factory, Leg("no-ambient-inherit")),
            await UnitTermination(factory, Leg("unit-termination")),
            await RootExit(factory, Leg("root-exit")),
            await DisposeCleanup(factory, Leg("dispose-cleanup")),
            await Completion(factory, Leg("completion")),
            await SetsidEscape(factory, Leg("setsid-escape"), known ? expected : null),
        };
        return new ConformanceReport(caps.Platform, legs);
    }

    private static ExecutionRequirements RequirementsFor(PlatformCapabilities caps)
    {
        var reqs = new List<ExecutionRequirement>();
        foreach (var cap in new[] { ExecutionCapability.UnitTermination, ExecutionCapability.SurvivesRootExit })
            if (caps.TryGet(cap, out var g) && g.Level != CapabilityLevel.Absent)
                reqs.Add(ExecutionRequirement.Require(cap, g.Level, allowConstructed: false));
        return new ExecutionRequirements(reqs);
    }

    private static ConformanceLeg CapabilityMatrix(PlatformCapabilities caps, PlatformConformance? expected)
    {
        if (expected is null)
            return new("capability-matrix", ConformanceOutcome.Failed,
                $"no expected matrix for platform '{caps.Platform}'");
        var diffs = new List<string>();
        Check(ExecutionCapability.UnitTermination, expected.UnitTermination);
        Check(ExecutionCapability.SurvivesRootExit, expected.SurvivesRootExit);
        Check(ExecutionCapability.EscapeResistant, expected.EscapeResistant);
        return diffs.Count == 0
            ? new("capability-matrix", ConformanceOutcome.Passed,
                $"unit={expected.UnitTermination}, rootExit={expected.SurvivesRootExit}, escapeResistant={expected.EscapeResistant}")
            : new("capability-matrix", ConformanceOutcome.Failed, string.Join("; ", diffs));

        void Check(ExecutionCapability cap, CapabilityLevel want)
        {
            caps.TryGet(cap, out var g);
            if (g is null || g.Level != want)
                diffs.Add($"{cap}: expected {want}, got {(g is null ? "unlisted" : g.Level.ToString())}");
        }
    }

    private static ConformanceLeg OpaqueHandles(IExecutionProvider provider)
    {
        var banned = new[] { "Pid", "JobHandle", "Cgroup", "Pgid", "Hwnd",
            "JobObject", "Launchd", "ProcessGroup", "Label", "Plist" };
        var offenders = new List<string>();
        foreach (var t in provider.GetType().Assembly.GetTypes()
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
        return offenders.Count == 0
            ? new("opaque-handles", ConformanceOutcome.Passed, "no native identity on the public surface")
            : new("opaque-handles", ConformanceOutcome.Failed, string.Join(", ", offenders));
    }

    private static async Task<ConformanceLeg> Prepare(Func<IExecutionProvider> factory)
    {
        await using var p = factory();
        var caps = p.Describe();
        var prep = p.Prepare(RequirementsFor(caps));
        return prep is PrepareResult.Accepted
            ? new("prepare", ConformanceOutcome.Passed, "negotiation accepted for platform requirements")
            : new("prepare", ConformanceOutcome.Skipped,
                "host cannot host the domain: " + Reasons(prep));
    }

    private static async Task<ConformanceLeg> SingleUse(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("single-use", why);
        var first = LaunchTarget(p, prep!, o.Workspace, "hold", "hb0", Marker, "1");
        if (first is not LaunchResult.Started st)
            return new("single-use", ConformanceOutcome.Failed, "first launch refused: " + Reasons(first));
        var second = LaunchTarget(p, prep!, o.Workspace, "hold", "hb0b", Marker, "1");
        bool ran = await PollAsync(() => ReadyAndHeld(o.Workspace, "hb0"), o.PollMilliseconds);
        bool denied = second is LaunchResult.Failed;
        string lockProbe;
        try { lockProbe = LockHeld(Path.Combine(o.Workspace, "hb0.lock")) ? "held" : "released"; }
        catch (Exception ex) { lockProbe = ex.GetType().Name + $" (0x{ex.HResult:x8}): " + ex.Message; }
        string heartbeat = Path.Combine(o.Workspace, "hb0");
        bool heartbeatExists = File.Exists(heartbeat);
        bool heartbeatFresh = Fresh(heartbeat);
        p.Terminate(st.Handle);
        return new("single-use", denied && ran ? ConformanceOutcome.Passed : ConformanceOutcome.Failed,
            denied && ran ? "first workload ran; second launch with spent preparation refused"
                : $"firstRan={ran} secondDenied={denied} ready={ReadOr(Path.Combine(o.Workspace, "hb0.ready"))} lock={lockProbe} heartbeatExists={heartbeatExists} heartbeatFresh={heartbeatFresh} python={ReadOr(Path.Combine(o.Workspace, "hb0.python"))} error={ReadOr(Path.Combine(o.Workspace, "hb0.error"))} stderr={ReadSmall(Path.Combine(o.Workspace, "hb0.stderr"))}");
    }

    private static async Task<ConformanceLeg> WorkingDirectory(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("working-directory", why);
        var launched = LaunchTarget(p, prep!, o.Workspace, "cwd", "hb", Marker, "1");
        if (launched is not LaunchResult.Started st)
            return new("working-directory", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
        // The workload writes a RELATIVE cwd.txt: it appears in the
        // workspace only if the child's working directory is the workspace.
        string probe = Path.Combine(o.Workspace, "cwd.txt");
        bool ok = await PollAsync(() => File.Exists(probe), o.PollMilliseconds);
        string seen = ok ? File.ReadAllText(probe).Trim() : "";
        p.Terminate(st.Handle);
        // A relative write can land in this unique workspace only when the
        // target was started there. macOS may spell /var as /private/var.
        bool correct = ok;
        return new("working-directory", correct ? ConformanceOutcome.Passed : ConformanceOutcome.Failed,
            correct ? $"relative write landed in the unique workspace; reported cwd={seen}" : $"relative cwd did not match unique workspace; absolute={ReadOr(Path.Combine(o.Workspace, "cwd-absolute.txt"))}");
    }

    private static async Task<ConformanceLeg> NoAmbientInherit(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("no-ambient-inherit", why);
        Environment.SetEnvironmentVariable(Ambient, "must-not-appear");
        try
        {
            var launched = LaunchTarget(p, prep!, o.Workspace, "env", "hb", Marker, "yes");
            if (launched is not LaunchResult.Started st)
                return new("no-ambient-inherit", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
            string dump = Path.Combine(o.Workspace, "env.txt");
            bool ok = await PollAsync(() => File.Exists(dump), o.PollMilliseconds);
            string content = ok ? File.ReadAllText(dump) : "";
            p.Terminate(st.Handle);
            bool granted = content.Contains(Marker + "=", StringComparison.Ordinal);
            bool clean = !content.Contains("must-not-appear", StringComparison.Ordinal);
            return granted && clean
                ? new("no-ambient-inherit", ConformanceOutcome.Passed, "granted present, ambient absent")
                : new("no-ambient-inherit", ConformanceOutcome.Failed,
                    $"granted={granted} ambientAbsent={clean}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(Ambient, null);
        }
    }

    private static async Task<ConformanceLeg> UnitTermination(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("unit-termination", why);
        var launched = LaunchTarget(p, prep!, o.Workspace, "tree", "hb", Marker, "1");
        if (launched is not LaunchResult.Started st)
            return new("unit-termination", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
        string root = Path.Combine(o.Workspace, "root");
        string child = Path.Combine(o.Workspace, "child");
        if (!await PollAsync(() => ReadyAndHeld(o.Workspace, "root")
            && ReadyAndHeld(o.Workspace, "child"), o.PollMilliseconds))
        {
            p.Terminate(st.Handle);
            return new("unit-termination", ConformanceOutcome.Failed, "root+child heartbeats never both fresh");
        }
        var term = p.Terminate(st.Handle);
        bool terminal = await TerminalConfirmedAsync(p, st.Handle);
        bool released = await PollAsync(() => Released(o.Workspace, "root")
            && Released(o.Workspace, "child"), 5000);
        return term is TerminateResult.Terminated && terminal && released
            ? new("unit-termination", ConformanceOutcome.Passed, "root+child lifetime locks released after domain terminal")
            : new("unit-termination", ConformanceOutcome.Failed,
                $"terminate={term.GetType().Name} terminal={terminal} locksReleased={released}");
    }

    private static async Task<ConformanceLeg> RootExit(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("root-exit", why);
        var launched = LaunchTarget(p, prep!, o.Workspace, "exitroot", "hb", Marker, "1");
        if (launched is not LaunchResult.Started st)
            return new("root-exit", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
        string child = Path.Combine(o.Workspace, "child");
        if (!await PollAsync(() => ReadyAndHeld(o.Workspace, "child"), o.PollMilliseconds))
        {
            p.Terminate(st.Handle);
            return new("root-exit", ConformanceOutcome.Failed, "child lifetime lock never held before root exit");
        }
        File.WriteAllText(Path.Combine(o.Workspace, "root-exit-release"), "go");
        if (!await PollAsync(() => File.Exists(Path.Combine(o.Workspace, "root-exit-proof")), o.PollMilliseconds))
        {
            p.Terminate(st.Handle);
            return new("root-exit", ConformanceOutcome.Failed, "root exit proof absent after release");
        }
        if (p.Describe().Platform == WellKnownPlatforms.MacOs.Platform)
        {
            CompletionResult? outcome = null;
            try { outcome = await p.WaitForCompletionAsync(st.Handle).AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10)); }
            catch { }
            bool released = await PollAsync(() => Released(o.Workspace, "child"), 5000);
            return outcome is CompletionResult.NaturalExit && released
                ? new("root-exit", ConformanceOutcome.Passed,
                    "launchd terminal observed after root exit; same-PG child lock released")
                : new("root-exit", ConformanceOutcome.Failed,
                    $"terminal={outcome?.GetType().Name ?? "missing"} childLockReleased={released}");
        }
        bool survivorHeld = await PollAsync(() => ReadyAndHeld(o.Workspace, "child")
            && Fresh(child), 3000);
        bool pending;
        using (var probe = new CancellationTokenSource(TimeSpan.FromMilliseconds(400)))
        {
            try { await p.WaitForCompletionAsync(st.Handle, probe.Token); pending = false; }
            catch (OperationCanceledException) { pending = true; }
        }
        var term = p.Terminate(st.Handle);
        bool terminal = await TerminalConfirmedAsync(p, st.Handle);
        bool releasedAfterStop = await PollAsync(() => Released(o.Workspace, "child"), 5000);
        return survivorHeld && pending && term is TerminateResult.Terminated && terminal && releasedAfterStop
            ? new("root-exit", ConformanceOutcome.Passed,
                "root-exit proof, survivor lock held; wait pending until terminate, then domain terminal")
            : new("root-exit", ConformanceOutcome.Failed,
                $"survivorHeld={survivorHeld} pending={pending} terminate={term.GetType().Name} terminal={terminal} childLockReleased={releasedAfterStop}");
    }

    private static async Task<ConformanceLeg> DisposeCleanup(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
        {
            await p.DisposeAsync();
            return Skip("dispose-cleanup", why);
        }
        var launched = LaunchTarget(p, prep!, o.Workspace, "tree", "hb", Marker, "1");
        if (launched is not LaunchResult.Started)
        {
            await p.DisposeAsync();
            return new("dispose-cleanup", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
        }
        string root = Path.Combine(o.Workspace, "root");
        string child = Path.Combine(o.Workspace, "child");
        if (!await PollAsync(() => ReadyAndHeld(o.Workspace, "root")
            && ReadyAndHeld(o.Workspace, "child"), o.PollMilliseconds))
        {
            await p.DisposeAsync();
            return new("dispose-cleanup", ConformanceOutcome.Failed, "live tree never materialized");
        }
        await p.DisposeAsync();
        bool released = await PollAsync(() => Released(o.Workspace, "root")
            && Released(o.Workspace, "child"), 5000);
        return released
            ? new("dispose-cleanup", ConformanceOutcome.Passed,
                "live root+child lifetime locks released by provider disposal")
            : new("dispose-cleanup", ConformanceOutcome.Failed, "tree lock survived provider disposal");
    }

    private static async Task<ConformanceLeg> Completion(Func<IExecutionProvider> factory, ConformanceOptions o)
    {
        // Natural root exit code is observed.
        await using (var p = factory())
        {
            if (!TryPrepare(p, out var prep, out string why))
                return Skip("completion", why);
            var launched = LaunchTarget(p, prep!, o.Workspace, "exit17", "hb", Marker, "1");
            if (launched is not LaunchResult.Started natural)
                return new("completion", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
            var result = await p.WaitForCompletionAsync(natural.Handle, CancellationToken.None);
            string proof = Path.Combine(o.Workspace, "exit17-proof");
            bool ran = File.Exists(proof) && File.ReadAllText(proof).Trim() == o.Workspace;
            if (!ran || result is not CompletionResult.NaturalExit { RootExitCode: 17 })
                return new("completion", ConformanceOutcome.Failed, $"natural exit not observed: {result.GetType().Name}");
        }
        // Terminate while running completes as Terminated.
        await using (var p = factory())
        {
            if (!TryPrepare(p, out var prep, out string why))
                return Skip("completion", why);
            var launched = LaunchTarget(p, prep!, o.Workspace, "hold", "hb", Marker, "1");
            if (launched is not LaunchResult.Started running)
                return new("completion", ConformanceOutcome.Failed, "launch refused: " + Reasons(launched));
            if (!await PollAsync(() => ReadyAndHeld(o.Workspace, "hb"), o.PollMilliseconds))
                return new("completion", ConformanceOutcome.Failed, "terminable workload never acquired lifetime lock");
            p.Terminate(running.Handle);
            var result = await p.WaitForCompletionAsync(running.Handle, CancellationToken.None);
            if (result is not CompletionResult.Terminated)
                return new("completion", ConformanceOutcome.Failed, $"terminate completion: {result.GetType().Name}");
        }
        // Foreign handle fails closed.
        {
            await using var p = factory();
            var result = await p.WaitForCompletionAsync(
                new ExecutionHandle("other", Guid.NewGuid()), CancellationToken.None);
            if (result is not CompletionResult.Failed)
                return new("completion", ConformanceOutcome.Failed, "foreign handle did not fail closed");
        }
        return new("completion", ConformanceOutcome.Passed,
            "natural exit 17 observed, terminate->Terminated, foreign fail-closed");
    }

    private static async Task<ConformanceLeg> SetsidEscape(
        Func<IExecutionProvider> factory, ConformanceOptions o, PlatformConformance? expected)
    {
        if (expected is null)
            return new("setsid-escape", ConformanceOutcome.Failed, "no expected matrix");
        if (expected.Escape == EscapeExpectation.NotApplicable)
            return new("setsid-escape", ConformanceOutcome.Skipped,
                "no setsid on this OS; EscapeResistant attested structurally");
        string? python = ConformanceScripts.FindPython();
        if (python is null)
            return new("setsid-escape", ConformanceOutcome.Skipped, "python3 not found for the escaper");
        await using var p = factory();
        if (!TryPrepare(p, out var prep, out string why))
            return Skip("setsid-escape", why);
        var spec = new ProcessStartSpec(python,
            Quote(ConformanceScripts.UnixEscapePath(o.Workspace)) + " " + Quote(o.Workspace) + " root",
            o.Workspace, ConformanceScripts.UnixEnv(Marker, "1"));
        var launched = p.Launch(prep!, spec);
        if (launched is not LaunchResult.Started st)
            return new("setsid-escape", ConformanceOutcome.Failed, "esc-capture launch refused: " + Reasons(launched));
        string hbEsc = Path.Combine(o.Workspace, "hb-esc");
        if (!await PollAsync(() => ReadyAndHeld(o.Workspace, "leaf")
            && ReadyAndHeld(o.Workspace, "esc") && Fresh(hbEsc), o.PollMilliseconds))
        {
            p.Terminate(st.Handle);
            File.WriteAllText(Path.Combine(o.Workspace, "stopfile"), "stop");
            return new("setsid-escape", ConformanceOutcome.Failed,
                "escaper never ran (setsid failed?): " + ReadOr(Path.Combine(o.Workspace, "esc-error.txt")));
        }
        var term = p.Terminate(st.Handle);
        try
        {
            if (expected.Escape == EscapeExpectation.Resistant)
            {
                bool escDead = await PollAsync(() => Released(o.Workspace, "esc"), 5000);
                bool leafReleased = await PollAsync(() => Released(o.Workspace, "leaf"), 5000);
                return term is TerminateResult.Terminated && escDead && leafReleased
                    ? new("setsid-escape", ConformanceOutcome.Passed, "setsid escapee was still owned and died (resistant)")
                    : new("setsid-escape", ConformanceOutcome.Failed, $"escapee survived (expected resistant): releasedEsc={escDead} releasedLeaf={leafReleased}");
            }
            // Observed: the same-PG leaf dies, the session escapee survives.
            bool leafDead = await PollAsync(() => Released(o.Workspace, "leaf"), 5000);
            bool escAlive = ReadyAndHeld(o.Workspace, "esc") && Fresh(hbEsc);
            return term is TerminateResult.Terminated && leafDead && escAlive
                ? new("setsid-escape", ConformanceOutcome.Passed, "escape observed: same-PG leaf died, escapee survived")
                : new("setsid-escape", ConformanceOutcome.Failed, $"leafReleased={leafDead} escapeeHeld={escAlive}");
        }
        finally
        {
            try { File.WriteAllText(Path.Combine(o.Workspace, "stopfile"), "stop"); } catch { }
        }
    }

    // ---- helpers ----

    private static bool TryPrepare(IExecutionProvider p, out PreparedExecution? prep, out string why)
    {
        prep = null;
        var r = p.Prepare(RequirementsFor(p.Describe()));
        if (r is PrepareResult.Accepted a) { prep = a.Prepared; why = ""; return true; }
        why = "host cannot host the domain: " + Reasons(r);
        return false;
    }

    private static LaunchResult LaunchTarget(IExecutionProvider p, PreparedExecution prep,
        string ws, string mode, string name, string marker, string value)
    {
        string exe;
        string args;
        IReadOnlyDictionary<string, string> env;
        if (OperatingSystem.IsWindows())
        {
            exe = ConformanceScripts.FindPowerShell() ?? "powershell";
            args = $"-NoProfile -ExecutionPolicy Bypass -File {Quote(ConformanceScripts.WindowsWorkPath(ws))} " +
                   $"{mode} {Quote(ws)} {name}";
            env = ConformanceScripts.WindowsEnv(marker, value);
        }
        else
        {
            exe = "/bin/sh";
            args = $"{Quote(ConformanceScripts.UnixWorkPath(ws))} {mode} {Quote(ws)} {name}";
            env = ConformanceScripts.UnixEnv(marker, value);
        }
        return p.Launch(prep, new ProcessStartSpec(exe, args, ws, env));
    }

    private static string Quote(string s) => "\"" + s + "\"";

    private static string Reasons(object result) => result switch
    {
        PrepareResult.Rejected r => string.Join("; ", r.Reasons),
        LaunchResult.Failed f => string.Join("; ", f.Reasons),
        TerminateResult.Failed f => string.Join("; ", f.Reasons),
        _ => result.GetType().Name,
    };

    private static ConformanceLeg Skip(string name, string why) =>
        new(name, ConformanceOutcome.Skipped, why);

    private static string ReadOr(string path) =>
        File.Exists(path) ? File.ReadAllText(path).Trim() : "(no detail)";

    private static string ReadSmall(string path)
    {
        if (!File.Exists(path)) return "(no detail)";
        using var reader = new StreamReader(path);
        char[] excerpt = new char[256];
        int count = reader.ReadBlock(excerpt, 0, excerpt.Length);
        return new string(excerpt, 0, count).Trim();
    }

    private static bool ReadyAndHeld(string workspace, string name)
    {
        string ready = Path.Combine(workspace, name + ".ready");
        return File.Exists(ready) && File.ReadAllText(ready) == workspace
            && LockHeld(Path.Combine(workspace, name + ".lock"));
    }

    private static bool Released(string workspace, string name)
    {
        string path = Path.Combine(workspace, name + ".lock");
        return File.Exists(path) && !LockHeld(path);
    }

    private static bool LockHeld(string path)
    {
        if (!File.Exists(path)) return false;
        if (OperatingSystem.IsWindows())
        {
            try { using var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return false; }
            catch (IOException) { return true; }
        }
        FileStream stream;
        try { stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite); }
        catch (IOException ex) when (OperatingSystem.IsMacOS() && ex.HResult == 35)
        {
            // On macOS, .NET's open can reject a Python-held flock before our
            // explicit flock probe. EWOULDBLOCK (35) is the native contention
            // code observed on the qualification host; other I/O errors fail.
            return true;
        }
        using var opened = stream;
        if (OperatingSystem.IsMacOS())
        {
            int fd = checked((int)stream.SafeFileHandle.DangerousGetHandle());
            int result = flock(fd, 2 | 4); // LOCK_EX | LOCK_NB
            if (result == 0) { flock(fd, 8); return false; } // LOCK_UN
            int errno = Marshal.GetLastWin32Error();
            if (errno == 35) return true; // EWOULDBLOCK
            throw new IOException($"macOS flock probe errno={errno}");
        }
        try { stream.Lock(0, 1); stream.Unlock(0, 1); return false; }
        catch (IOException) { return true; }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int flock(int fd, int operation);

    private static async Task<bool> TerminalConfirmedAsync(IExecutionProvider provider,
        ExecutionHandle handle)
    {
        try { return await provider.WaitForCompletionAsync(handle).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10)) is CompletionResult.Terminated; }
        catch { return false; }
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

    private static async Task<bool> PollAsync(Func<bool> cond, int ms)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            if (Safe(cond)) return true;
            await Task.Delay(200);
        }
        return Safe(cond);
    }

    private static bool Safe(Func<bool> cond)
    {
        try { return cond(); } catch { return false; }
    }
}
