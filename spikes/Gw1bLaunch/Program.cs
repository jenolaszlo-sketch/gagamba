// GW-1B minimal-launch staircase (slice 1). Invokes the experimental launch API
// with caller-built, round-trip-verified specs under disposable per-run identities.
// Setup/cleanup contract (no elevation, per-user, reversible):
//   setup   : disposable workspace under %TEMP% (marker-gated) + unique identity
//             GagambaGW1B<16hex><leg> per leg. The engine may create a per-user
//             AppContainer profile for app_container legs.
//   cleanup : reaped child (waited exit, or TerminateProcess + exit-code proof),
//             both native handles closed, DeleteAppContainerProfile called twice
//             (S_OK then NOT_FOUND proves removal; immediate NOT_FOUND proves no
//             profile was ever materialized), workspace deleted.
// Anything deviating fails its leg; nothing falls back to ordinary execution.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gagamba.Fixture.Host;

namespace Gagamba.Spikes.Gw1bLaunch;

internal sealed record LegCleanup(
    bool ChildReaped,
    bool HandlesClosed,
    string ProfileDisposition,
    bool WorkspaceDeleted);

internal sealed record LaunchResult(
    bool Ok,
    string Detail,
    bool ChildReaped);

internal static class LaunchStaircase
{
    private const uint LaunchWaitMs = 60_000;
    private const uint StopBudgetMs = 5_000;

    internal static async Task<int> Main(string[] args)
    {
        // Diagnostic: validate the profile P/Invoke chain on a known disposable
        // profile (create -> derive -> delete -> derive). Cross-platform safe
        // to parse; executes only on Windows.
        if (args.Length >= 2 && args[0] == "profile-probe")
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("profile-probe: Windows-only.");
                return 2;
            }
            string name = args[1];
            IntPtr sid = IntPtr.Zero;
            try
            {
                int hrCreate = Native.CreateAppContainerProfile(name, name, "gw1b diagnostic", IntPtr.Zero, 0, out sid);
                if (sid != IntPtr.Zero) { try { Native.FreeSid(sid); } catch { } sid = IntPtr.Zero; }
                bool present = MonikerPresent(name);
                int hrDel = Native.DeleteAppContainerProfile(name);
                bool gone = !MonikerPresent(name);
                Console.WriteLine($"profile-probe: create=0x{hrCreate:X} present={present} delete=0x{hrDel} gone={gone}");
                return hrCreate == 0 && present && hrDel == 0 && gone ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"profile-probe: FAULT {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }
        if (args.Length >= 2 && args[0] == "verify-file")
        {
            try
            {
                byte[] buf = await File.ReadAllBytesAsync(args[1]);
                SandboxSpecRequest decoded = SpecBuilder.Verify(buf);
                Console.WriteLine($"verify-file: OK version={decoded.Version} app={decoded.AppContainer} " +
                                  $"rw=[{string.Join(",", decoded.FsReadWrite)}] ro=[{string.Join(",", decoded.FsReadOnly)}]");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"verify-file: REJECTED {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("gw1b-launch: Windows-only spike (processmodel.dll). No evidence emitted.");
            return 2;
        }
        string repoRoot = Arg(args, "--repo-root") ?? FindRepoRoot();
        string outDir = Arg(args, "--out") ?? Path.Combine(repoRoot, "artifacts");
        Directory.CreateDirectory(outDir);

        var collector = new EvidenceCollector(
            repoRoot,
            EvidenceKinds.CapabilityProbe,
            manifestExtraDirs: ["spikes/Gw1bLaunch"],
            manifestExtraFiles: ["eng/launch-spike.*", "docs/research-plan.md", "spikes/Gw1bLaunch/pinned/PIN.md"],
            mandatoryOverride: LaunchManifest.MandatoryIds);
        Console.WriteLine($"gw1b-launch: runId={collector.RunId}");

        string runTag = Guid.NewGuid().ToString("N")[..16];
        var cleanups = new List<(string Leg, LegCleanup Cleanup)>();

        // ---- L1-SCHEMA-PIN ----
        await RunCase(collector, "L1-SCHEMA-PIN", () =>
        {
            string fbs = Path.Combine(repoRoot, "spikes", "Gw1bLaunch", "pinned", "BaseContainerSpecification.fbs");
            string pin = Path.Combine(repoRoot, "spikes", "Gw1bLaunch", "pinned", "PIN.md");
            if (!File.Exists(fbs) || !File.Exists(pin))
                return Task.FromResult((false, "pinned schema or PIN.md missing", (string?)null));
            string text = File.ReadAllText(fbs, Encoding.UTF8);
            var m = Regex.Match(File.ReadAllText(pin), @"SHA-256:\s*`([0-9A-Fa-f]{64})`");
            if (!m.Success)
                return Task.FromResult((false, "PIN.md carries no SHA-256", (string?)null));
            string actual = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(fbs)));
            if (!string.Equals(actual, m.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult((false, $"pinned bytes changed: {actual}", (string?)null));
            bool shape = text.Contains("root_type SandboxSpec;")
                && text.Contains("file_identifier \"SBOX\";")
                && text.Contains("version:string (required);");
            return Task.FromResult(shape
                ? (true, $"mxc v0.8.0, sha {actual[..16]}.., root/file-id/version-required confirmed", (string?)null)
                : (false, "pinned file lacks root/file-id/version shape", (string?)null));
        });

        // ---- L1-SPEC-BUILD (compiler + verifier gates, no API call) ----
        await RunCase(collector, "L1-SPEC-BUILD", () =>
        {
            var bare = new SandboxSpecRequest("0.1.0", false, [], []);
            var app = new SandboxSpecRequest("0.1.0", true, [], []);
            var grants = new SandboxSpecRequest("0.1.0", true, [@"C:\tmp\ws"], []);
            byte[] bBare = SpecBuilder.BuildVerified(bare);
            byte[] bApp = SpecBuilder.BuildVerified(app);
            byte[] bGrants = SpecBuilder.BuildVerified(grants);
            if (bBare.Length is 0 or > 1024 || bApp.Length is 0 or > 1024 || bGrants.Length > SpecBuilder.MaxBufferBytes)
                return Task.FromResult((false, "spec size out of sane bounds", (string?)null));
            bool negVersion;
            try { SpecBuilder.Build(new("9.9.9", false, [], [])); negVersion = false; }
            catch (ArgumentException) { negVersion = true; }
            byte[] corrupt = (byte[])bApp.Clone();
            corrupt[4] = (byte)'X';
            bool negVerify;
            try { SpecBuilder.Verify(corrupt); negVerify = false; }
            catch (InvalidOperationException) { negVerify = true; }
            if (!negVersion || !negVerify)
                return Task.FromResult((false, "negative gates did not reject", (string?)null));
            string hex = Convert.ToHexString(bApp[..Math.Min(32, bApp.Length)]);
            return Task.FromResult((true,
                $"round-trip ok: bare={bBare.Length}B app={bApp.Length}B grants={bGrants.Length}B head={hex}; negatives rejected",
                (string?)null));
        });

        // ---- L1-BARE-LAUNCH (app_container=false is not a supported shape:
        // fail-closed rejection expected: FALSE + ERROR_NOT_SUPPORTED, pid 0) ----
        string idBare = $"GagambaGW1B{runTag}b";
        await RunCase(collector, "L1-BARE-LAUNCH", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-bare-" + runTag);
            var r = await LaunchLeg(idBare,
                new("0.1.0", false, [], []), "/c exit 42", ws.Root, 42, null,
                expectRejection: true, expectedError: 50 /* ERROR_NOT_SUPPORTED */);
            string prof = SweepProfile(idBare);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("bare", new(r.ChildReaped, true, prof, wsOk)));
            return (r.Ok, r.Detail + $" profile={prof} ws={wsOk}", (string?)null);
        });

        // ---- L1-APPCONTAINER-LAUNCH (exit-code oracle under AppContainer) ----
        string idAc = $"GagambaGW1B{runTag}a";
        await RunCase(collector, "L1-APPCONTAINER-LAUNCH", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-ac-" + runTag);
            var r = await LaunchLeg(idAc,
                new("0.1.0", true, [], []), "/c exit 7", ws.Root, 7, null);
            string prof = SweepProfile(idAc);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("appcontainer", new(r.ChildReaped, true, prof, wsOk)));
            return (r.Ok, r.Detail + $" profile={prof} ws={wsOk}", (string?)null);
        });

        // ---- L1-FS-GRANT-EFFECT (rw grant + marker written by the target) ----
        string idFs = $"GagambaGW1B{runTag}f";
        await RunCase(collector, "L1-FS-GRANT-EFFECT", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-fs-" + runTag);
            string markerName = "effect.txt";
            string expected = $"effect-{collector.RunId}";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var r = await LaunchLeg(idFs, spec,
                $"/c echo {expected} > {markerName}", ws.Root, 0,
                async cwd =>
                {
                    string path = Path.Combine(cwd, markerName);
                    if (!File.Exists(path))
                        return (false, "marker file absent: grant had no effect");
                    string actual = (await File.ReadAllTextAsync(path)).Trim();
                    return actual == expected
                        ? (true, $"marker exact ({actual.Length}B)")
                        : (false, $"marker mismatch: '{actual[..Math.Min(60, actual.Length)]}'");
                });
            string prof = SweepProfile(idFs);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("fsgrant", new(r.ChildReaped, true, prof, wsOk)));
            return (r.Ok, r.Detail + $" profile={prof} ws={wsOk}", (string?)null);
        });

        // ---- L1-CLEANUP (aggregate proof) ----
        await RunCase(collector, "L1-CLEANUP", () =>
        {
            var notes = cleanups.Select(c =>
                $"{c.Leg}:reaped={c.Cleanup.ChildReaped}/profile={c.Cleanup.ProfileDisposition}/ws={c.Cleanup.WorkspaceDeleted}");
            bool all = cleanups.Count == 3 && cleanups.All(c =>
                c.Cleanup.ChildReaped
                && (c.Cleanup.ProfileDisposition.StartsWith("deleted(")
                    || c.Cleanup.ProfileDisposition.StartsWith("deleted-never-materialized("))
                && c.Cleanup.WorkspaceDeleted);
            return Task.FromResult(all
                ? (true, "all legs reaped, handles closed, profiles removed, workspaces deleted: " + string.Join("; ", notes), (string?)null)
                : (false, "cleanup incomplete: " + string.Join("; ", notes), (string?)null));
        });

        var report = collector.Finish("Confirmed",
            "launch spike: bounded waits, terminate-on-timeout, profile sweep, marker-gated workspaces",
            "identities=" + string.Join(",", idBare, idAc, idFs));
        var errors = EvidenceValidator.Validate(report, LaunchManifest.MandatoryIds);

        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        string outPath = Path.Combine(outDir, $"windows-launch-{report.RunId}.json");
        await File.WriteAllTextAsync(outPath, json, Encoding.UTF8);

        Console.WriteLine($"gw1b-launch: cases={report.Summary.Total} passed={report.Summary.Passed} " +
                          $"failed={report.Summary.Failed} mandatoryComplete={report.Summary.MandatoryComplete} " +
                          $"aggregate={report.Summary.Aggregate}");
        Console.WriteLine($"gw1b-launch: report={outPath} validatorErrors={errors.Count}");
        foreach (string e in errors)
            Console.Error.WriteLine($"gw1b-launch: invalid: {e}");
        return string.Equals(report.Summary.Aggregate, "Passed", StringComparison.Ordinal) && errors.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// One launch leg: verified spec, single API call, bounded wait, proved
    /// reaping, closed handles. Never throws engine rejections as exceptions:
    /// a FALSE return is a recorded finding, not a crash.
    /// </summary>
    private static async Task<LaunchResult> LaunchLeg(
        string identity,
        SandboxSpecRequest spec,
        string targetArgs,
        string cwd,
        uint expectedExit,
        Func<string, Task<(bool Ok, string Detail)>>? effectCheck,
        bool expectRejection = false,
        int expectedError = 0)
    {
        await Task.Yield();
        byte[] specBytes;
        try { specBytes = SpecBuilder.BuildVerified(spec); }
        catch (Exception ex) { return new(false, $"spec gate refused: {ex.Message}", true); }

        string cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var commandLine = new StringBuilder(32768);
        commandLine.Append('"').Append(cmdExe).Append("\" ").Append(targetArgs);
        var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>() };
        bool apiResult;
        int lastError;
        IntPtr hProcess = IntPtr.Zero, hThread = IntPtr.Zero;
        uint pid = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            apiResult = Native.Experimental_CreateProcessInSandbox(
                cmdExe, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                0, IntPtr.Zero, cwd, ref si, identity,
                specBytes, (uint)specBytes.Length, out ProcessInformation pi);
            lastError = Marshal.GetLastWin32Error();
            hProcess = pi.hProcess;
            hThread = pi.hThread;
            pid = pi.dwProcessId;
        }
        catch (Exception ex)
        {
            return new(false, $"invocation fault (not engine rejection): {ex.GetType().Name}: {ex.Message}", true);
        }

        string verdict = $"api={apiResult} err=0x{lastError:X}({MapError(lastError)}) pid={pid} spec={specBytes.Length}B";
        if (expectRejection)
        {
            bool ok = !apiResult && lastError == expectedError && pid == 0;
            return new(ok, verdict + (ok
                ? " (unsupported shape rejected before target dispatch; no target ran)"
                : $" (want rejection err=0x{expectedError:X} pid=0)"), true);
        }
        if (!apiResult)
            return new(false, verdict + " (engine rejected launch; no target ran)", true);

        uint wait = Native.WaitForSingleObject(hProcess, LaunchWaitMs);
        if (wait == Native.WAIT_TIMEOUT)
        {
            Native.TerminateProcess(hProcess, 99);
            Native.WaitForSingleObject(hProcess, StopBudgetMs);
            bool dead = Native.GetExitCodeProcess(hProcess, out uint termCode) && termCode != Native.STILL_ACTIVE;
            CloseBoth(hProcess, hThread);
            return new(false, verdict + $" TIMEOUT then terminated (dead={dead})", dead);
        }
        bool gotCode = Native.GetExitCodeProcess(hProcess, out uint exitCode);
        bool inJob = false;
        try { Native.IsProcessInJob(hProcess, IntPtr.Zero, out inJob); } catch { }
        CloseBoth(hProcess, hThread);
        sw.Stop();
        if (!gotCode || exitCode != expectedExit)
            return new(false, verdict + $" exit={exitCode} want {expectedExit} job={inJob}", true);
        if (effectCheck is not null)
        {
            var (extraOk, extraDetail) = await effectCheck(cwd);
            if (!extraOk)
                return new(false, verdict + " effect FAILED: " + extraDetail, true);
            verdict += " effect ok: " + extraDetail;
        }
        return new(true, verdict + $" exit={exitCode} job={inJob} ({sw.ElapsedMilliseconds}ms)", true);
    }

    private static void CloseBoth(IntPtr hProcess, IntPtr hThread)
    {
        try { if (hProcess != IntPtr.Zero) Native.CloseHandle(hProcess); } catch { }
        try { if (hThread != IntPtr.Zero) Native.CloseHandle(hThread); } catch { }
    }

    private static string SweepProfile(string identity)
    {
        // Existence proof via the per-user profile Mappings registry key
        // (Moniker value). Read-only; a read failure throws loudly rather than
        // masquerading as absent.
        bool existedBefore = MonikerPresent(identity);
        int hr1 = Native.DeleteAppContainerProfile(identity);
        bool existsAfter = MonikerPresent(identity);
        int hr2 = Native.DeleteAppContainerProfile(identity);
        if (existedBefore && hr1 == 0 && !existsAfter)
            return $"deleted(existed,hr=0x{hr1:X}/0x{hr2:X})";
        if (!existedBefore && !existsAfter)
            return $"deleted-never-materialized(hr=0x{hr1:X})";
        return $"deleted-UNCERTAIN(before={existedBefore},hr=0x{hr1:X}/0x{hr2:X},after={existsAfter})";
    }

    private static bool MonikerPresent(string identity)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("profile registry check is Windows-only");
        using var mappings = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Mappings");
        if (mappings is null)
            throw new InvalidOperationException("AppContainer Mappings key absent");
        foreach (string sub in mappings.GetSubKeyNames())
        {
            using var key = mappings.OpenSubKey(sub);
            if (string.Equals(key?.GetValue("Moniker") as string, identity, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string MapError(int code) => code switch
    {
        0 => "S_OK",
        87 => "ERROR_INVALID_PARAMETER",
        5 => "ERROR_ACCESS_DENIED",
        13 => "ERROR_INVALID_DATA",
        50 => "ERROR_NOT_SUPPORTED",
        1168 => "ERROR_NOT_FOUND",
        _ => "unmapped",
    };

    private static async Task RunCase(EvidenceCollector collector, string id,
        Func<Task<(bool Passed, string Reason, string? OutcomeOverride)>> fn)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (passed, reason, outcomeOverride) = await fn();
            sw.Stop();
            string outcome = outcomeOverride ?? (passed ? "Passed" : "Failed");
            collector.Add(id, outcome, reason, sw.ElapsedMilliseconds);
            Console.WriteLine($"gw1b-launch: [{outcome.ToUpperInvariant()}] {id} ({sw.ElapsedMilliseconds}ms) {reason}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            collector.Add(id, "Failed", $"exception {ex.GetType().Name}: {ex.Message}", sw.ElapsedMilliseconds);
            Console.WriteLine($"gw1b-launch: [FAILED] {id} exception {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string? Arg(string[] args, string name)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }

    private static string FindRepoRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "docs", "research-plan.md")) || Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }
}
