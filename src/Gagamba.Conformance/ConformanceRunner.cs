// One conformance runner over the frozen SPI. It measures the provider's
// actual behavior and compares to ConformanceMatrix; a drift fails loudly.
// Behavioral legs that cannot even prepare (e.g. Linux without a delegated
// cgroup) report Skipped with the reason, never a false pass.
using Gagamba.Execution;

namespace Gagamba.Conformance;

public sealed record ConformanceOptions(string Workspace);

public static class ConformanceRunner
{
    private const string Marker = "CONF_GRANTED";
    private const string Ambient = "CONF_AMBIENT";

    public static async Task<ConformanceReport> RunAsync(
        Func<IExecutionProvider> factory, ConformanceOptions options)
    {
        Directory.CreateDirectory(options.Workspace);
        ConformanceScripts.Write(options.Workspace);
        var caps = factory().Describe();
        bool known = ConformanceMatrix.TryForPlatform(caps.Platform, out var expected);

        var legs = new List<ConformanceLeg>
        {
            CapabilityMatrix(caps, known ? expected : null),
            OpaqueHandles(factory()),
            await Prepare(factory),
            await SingleUse(factory, options),
            await WorkingDirectory(factory, options),
            await NoAmbientInherit(factory, options),
            await UnitTermination(factory, options),
            await RootExit(factory, options),
            await DisposeCleanup(factory, options),
            await Completion(factory, options),
            await SetsidEscape(factory, options, known ? expected : null),
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
        bool denied = second is LaunchResult.Failed;
        p.Terminate(st.Handle);
        return new("single-use", denied ? ConformanceOutcome.Passed : ConformanceOutcome.Failed,
            denied ? "second launch with a spent preparation refused" : "second launch unexpectedly accepted");
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
        bool ok = await PollAsync(() => File.Exists(probe), 15000);
        string seen = ok ? File.ReadAllText(probe).Trim() : "";
        p.Terminate(st.Handle);
        return new("working-directory", ok ? ConformanceOutcome.Passed : ConformanceOutcome.Failed,
            ok ? $"relative write landed in workspace; child cwd={seen}" : "relative cwd.txt never appeared in workspace");
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
            bool ok = await PollAsync(() => File.Exists(dump), 15000);
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
        if (!await PollAsync(() => Fresh(root) && Fresh(child), 15000))
        {
            p.Terminate(st.Handle);
            return new("unit-termination", ConformanceOutcome.Failed, "root+child heartbeats never both fresh");
        }
        var term = p.Terminate(st.Handle);
        bool frozen = await FrozenAsync(root) && await FrozenAsync(child);
        return term is TerminateResult.Terminated && frozen
            ? new("unit-termination", ConformanceOutcome.Passed, "root+child frozen after terminate")
            : new("unit-termination", ConformanceOutcome.Failed,
                $"terminate={term.GetType().Name} frozen={frozen}");
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
        if (!await PollAsync(() => Fresh(child), 15000))
        {
            p.Terminate(st.Handle);
            return new("root-exit", ConformanceOutcome.Failed, "survivor heartbeat never fresh after root exit");
        }
        var term = p.Terminate(st.Handle);
        bool frozen = await FrozenAsync(child);
        return term is TerminateResult.Terminated && frozen
            ? new("root-exit", ConformanceOutcome.Passed, "handle terminated a post-root-exit survivor")
            : new("root-exit", ConformanceOutcome.Failed,
                $"terminate={term.GetType().Name} frozen={frozen}");
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
        if (!await PollAsync(() => Fresh(root) && Fresh(child), 15000))
        {
            await p.DisposeAsync();
            return new("dispose-cleanup", ConformanceOutcome.Failed, "live tree never materialized");
        }
        await p.DisposeAsync();
        bool frozen = await FrozenAsync(root) && await FrozenAsync(child);
        return frozen
            ? new("dispose-cleanup", ConformanceOutcome.Passed, "live tree frozen by provider disposal")
            : new("dispose-cleanup", ConformanceOutcome.Failed, "tree survived provider disposal");
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
            if (result is not CompletionResult.NaturalExit { RootExitCode: 17 })
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
        string hbLeaf = Path.Combine(o.Workspace, "hb-leaf");
        if (!await PollAsync(() => Fresh(hbEsc), 15000))
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
                bool escDead = await FrozenAsync(hbEsc);
                return term is TerminateResult.Terminated && escDead
                    ? new("setsid-escape", ConformanceOutcome.Passed, "setsid escapee was still owned and died (resistant)")
                    : new("setsid-escape", ConformanceOutcome.Failed, $"escapee survived (expected resistant): frozen={escDead}");
            }
            // Observed: the same-PG leaf dies, the session escapee survives.
            bool leafDead = await FrozenAsync(hbLeaf);
            bool escAlive = Fresh(hbEsc) || await PollAsync(() => Fresh(hbEsc), 3000);
            return leafDead && escAlive
                ? new("setsid-escape", ConformanceOutcome.Passed, "escape observed: same-PG leaf died, escapee survived")
                : new("setsid-escape", ConformanceOutcome.Failed, $"leafDead={leafDead} escapeeAlive={escAlive}");
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

    private static bool Safe(Func<bool> cond)
    {
        try { return cond(); } catch { return false; }
    }
}
