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

    internal static string OnlyFilter = "";

    internal static async Task<int> Main(string[] args)
    {
        // Diagnostic: build a verified spec for arbitrary grants and write the
        // bytes to a file (round-trip gate applies). Pairs with verify-file.
        // Usage: encode --rw <p> [--rw <p>...] [--ro <p>...] [--caps <c>] [--app false] --out <file>
        if (args.Length >= 2 && args[0] == "encode")
        {
            try
            {
                var rw = new List<string>();
                var ro = new List<string>();
                string caps = "";
                bool app = true;
                string? outFile = null;
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--rw" when i + 1 < args.Length: rw.Add(args[++i]); break;
                        case "--ro" when i + 1 < args.Length: ro.Add(args[++i]); break;
                        case "--caps" when i + 1 < args.Length: caps = args[++i]; break;
                        case "--app" when i + 1 < args.Length: app = args[++i] != "false"; break;
                        case "--out" when i + 1 < args.Length: outFile = args[++i]; break;
                        default: throw new InvalidOperationException($"unknown encode arg '{args[i]}'");
                    }
                }
                if (outFile is null) throw new InvalidOperationException("--out required");
                byte[] built = SpecBuilder.BuildVerified(new("0.1.0", app, rw, ro, caps));
                await File.WriteAllBytesAsync(outFile, built);
                Console.WriteLine($"encode: OK {built.Length}B -> {outFile}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"encode: REJECTED {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }
        // Diagnostic: launch a wait-loop tree and sample liveness/visibility
        // every 500 ms for 20 s (root alive? visible? exit code? sleeper?),
        // then clean up. Diagnoses mid-run disappearances.
        if (args.Length >= 2 && args[0] == "watch-tree")
        {
            if (!OperatingSystem.IsWindows()) { Console.Error.WriteLine("watch-tree: Windows-only."); return 2; }
            string watchWs = Arg(args, "--ws") ?? "";
            string watchId = Arg(args, "--id") ?? "";
            if (watchWs.Length == 0 || watchId.Length == 0 || !Directory.Exists(watchWs)) return 2;
            var (wOk, wDetail, whRoot, wPid) = LaunchNoWait(watchId,
                new SandboxSpecRequest("0.1.0", true, [watchWs], []),
                $"/d /c call \"{watchWs}\\root2.bat\" \"{watchWs}\"",
                watchWs);
            Console.WriteLine($"watch: launched ok={wOk} pid={wPid} {wDetail}");
            if (!wOk) return 1;
            try
            {
                for (int i = 0; i < 40; i++)
                {
                    await Task.Delay(500);
                    uint w = Native.WaitForSingleObject(whRoot, 0);
                    string alive = w == Native.WAIT_TIMEOUT ? "alive"
                        : Native.GetExitCodeProcess(whRoot, out uint rc) ? $"exit={rc}" : "query-fail";
                    string snap = Supervisor.DescribeCmdTable(wPid);
                    Console.WriteLine($"watch: t={i * 0.5:F1}s root={alive} snap=[{snap}]");
                }
            }
            finally
            {
                var (f, k, _) = Supervisor.SweepDescendants(wPid, DateTime.UtcNow - TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5));
                IntPtr hr2 = Native.OpenProcess(
                    Native.PROCESS_TERMINATE | Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.SYNCHRONIZE, false, wPid);
                if (hr2 != IntPtr.Zero)
                {
                    try
                    {
                        Native.TerminateProcess(hr2, 99);
                        Native.WaitForSingleObject(hr2, 5000);
                    }
                    finally { try { Native.CloseHandle(hr2); } catch { } }
                }
                try { if (whRoot != IntPtr.Zero) Native.CloseHandle(whRoot); } catch { }
                Console.WriteLine($"watch: cleanup sweep found={f} killed={k}");
            }
            return 0;
        }
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
                                  $"caps='{decoded.Capabilities}' " +
                                  $"rw=[{string.Join(",", decoded.FsReadWrite)}] ro=[{string.Join(",", decoded.FsReadOnly)}]");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"verify-file: REJECTED {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }
        // Launcher-death simulator for L4-CRASH-RECOVERY: launches a barrier
        // tree inside the sandbox, reports the root PID, then dies immediately
        // with NO cleanup (no sweep, no profile delete, no workspace delete).
        // The parent leg performs the recovery and proves it.
        // Reporting channel is a FILE (--report), never stdout: the holder may
        // run with any stdio shape (including fully redirected), and a pipe
        // report would conflate caller-stdio issues with launch results.
        if (args.Length >= 2 && args[0] == "crash-holder")
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("crash-holder: Windows-only.");
                return 2;
            }
            string holderWs = Arg(args, "--ws") ?? "";
            string holderId = Arg(args, "--id") ?? "";
            string holderReport = Arg(args, "--report") ?? "";
            if (holderWs.Length == 0 || holderId.Length == 0 || !Directory.Exists(holderWs))
            {
                Console.Error.WriteLine("crash-holder: requires --ws <dir> --id <identity> [--report <file>]");
                return 2;
            }
            // Diagnostic override: launch with externally built spec bytes
            // (e.g. official flatc output), verified first. Isolates encoder
            // bugs from engine path rules.
            string specFile = Arg(args, "--spec-file") ?? "";
            byte[]? overrideSpec = null;
            if (specFile.Length > 0)
            {
                try
                {
                    overrideSpec = SpecBuilder.Verify(await File.ReadAllBytesAsync(specFile)) is not null
                        ? await File.ReadAllBytesAsync(specFile)
                        : null;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"crash-holder: spec file rejected: {ex.Message}");
                    return 2;
                }
            }
            var (nlOk, nlDetail, hRoot, rootPid) = LaunchNoWait(holderId,
                new SandboxSpecRequest("0.1.0", true, [holderWs], []),
                $"/d /c call \"{holderWs}\\root2.bat\" \"{holderWs}\"",
                holderWs,
                overrideSpec);
            string line = $"HOLDER rootPid={rootPid} ok={nlOk} {nlDetail}";
            Console.WriteLine(line);
            Console.Out.Flush();
            if (holderReport.Length > 0)
            {
                try { await File.WriteAllTextAsync(holderReport, line + "\n"); } catch { }
            }
            if (hRoot != IntPtr.Zero) { try { Native.CloseHandle(hRoot); } catch { } }
            Environment.Exit(9);
            return 9;
        }
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("gw1b-launch: Windows-only spike (processmodel.dll). No evidence emitted.");
            return 2;
        }
        string repoRoot = Arg(args, "--repo-root") ?? FindRepoRoot();
        string outDir = Arg(args, "--out") ?? Path.Combine(repoRoot, "artifacts");
        Directory.CreateDirectory(outDir);
        OnlyFilter = Arg(args, "--only") ?? "";

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
        // Byte-exact pins against official flatc 25.12.19 output for the same
        // inputs (flatc used as a temp-dir oracle only, never vendored).
        await RunCase(collector, "L1-SPEC-BUILD", () =>
        {
            var bare = new SandboxSpecRequest("0.1.0", false, [], []);
            var app = new SandboxSpecRequest("0.1.0", true, [], []);
            var grants = new SandboxSpecRequest("0.1.0", true, [@"C:\tmp\ws"], []);
            var caps = new SandboxSpecRequest("0.1.0", true, [], [@"C:\Windows"], "registryRead");
            var ro2 = new SandboxSpecRequest("0.1.0", true, [], [@"C:\temp\a", @"C:\temp\b"]);
            byte[] bBare = SpecBuilder.BuildVerified(bare);
            byte[] bApp = SpecBuilder.BuildVerified(app);
            byte[] bGrants = SpecBuilder.BuildVerified(grants);
            byte[] bCaps = SpecBuilder.BuildVerified(caps);
            byte[] bRo2 = SpecBuilder.BuildVerified(ro2);
            // Byte-exact pins against official flatc 25.12.19 output for every
            // supported shape (bare/app/grants/ro2/caps). flatc is used only as
            // a temp-dir oracle; its outputs are vendored as test vectors.
            string testdata = Path.Combine(repoRoot, "spikes", "Gw1bLaunch", "testdata");
            bool exact = File.ReadAllBytes(Path.Combine(testdata, "flatc-app.bin")).SequenceEqual(bApp)
                && File.ReadAllBytes(Path.Combine(testdata, "flatc-bare.bin")).SequenceEqual(bBare)
                && File.ReadAllBytes(Path.Combine(testdata, "flatc-grants-fixed.bin")).SequenceEqual(bGrants)
                && File.ReadAllBytes(Path.Combine(testdata, "flatc-ro2.bin")).SequenceEqual(bRo2)
                && File.ReadAllBytes(Path.Combine(testdata, "flatc-caps.bin")).SequenceEqual(bCaps);
            if (!exact)
                return Task.FromResult((false, "encoder diverged from flatc test vectors", (string?)null));
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
            return Task.FromResult((true,
                $"round-trip + flatc byte-exact (bare/app/grants/ro2/caps): bare={bBare.Length}B app={bApp.Length}B grants={bGrants.Length}B caps={bCaps.Length}B; negatives rejected",
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

        // ---- L2-READONLY-DENIAL (ro read allowed, ro write denied; controls first) ----
        await RunCase(collector, "L2-READONLY-DENIAL", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-ro-" + runTag);
            using var ro = FixtureWorkspace.Create("gw1b-ro-grant-" + runTag);
            await File.WriteAllTextAsync(Path.Combine(ro.Root, "seed.txt"), "seed");
            var c1 = await ControlRunner.Run("cmd.exe", $"/d /c copy \"{ro.Root}\\seed.txt\" \"{ws.Root}\\ctl-ok.txt\"", ws.Root);
            var c2 = await ControlRunner.Run("cmd.exe", $"/d /c echo ctl > \"{ro.Root}\\ctl-try.txt\"", ws.Root);
            bool ctlOk = c1.Exited && File.Exists(Path.Combine(ws.Root, "ctl-ok.txt"))
                && c2.Exited && File.Exists(Path.Combine(ro.Root, "ctl-try.txt"));
            TryDelete(Path.Combine(ws.Root, "ctl-ok.txt"));
            TryDelete(Path.Combine(ro.Root, "ctl-try.txt"));
            if (!ctlOk)
                return AbandonLeg(cleanups, "rodenial", $"GagambaGW1B{runTag}r",
                    $"controls failed: copy[{c1.Detail}] write[{c2.Detail}]", ws, ro);
            string idRo = $"GagambaGW1B{runTag}r";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], [ro.Root]);
            var r = await LaunchLeg(idRo, spec,
                $"/d /c copy \"{ro.Root}\\seed.txt\" \"{ws.Root}\\ok.txt\" & echo x > \"{ro.Root}\\try.txt\"",
                ws.Root, null, null);
            bool allowedRead = File.Exists(Path.Combine(ws.Root, "ok.txt"));
            bool deniedWriteAbsent = !File.Exists(Path.Combine(ro.Root, "try.txt"));
            string prof = SweepProfile(idRo);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root)
                && ro.DisposeAndReport() == "Confirmed" && !Directory.Exists(ro.Root);
            cleanups.Add(("rodenial", new(r.ChildReaped, true, prof, wsOk)));
            bool ok = r.Ok && allowedRead && deniedWriteAbsent;
            return (ok, $"{r.Detail} ro-read={allowedRead} ro-write-absent={deniedWriteAbsent} profile={prof}", (string?)null);
        });

        // ---- L2-DENIED-READ (ungranted sentinel unreadable; control first) ----
        await RunCase(collector, "L2-DENIED-READ", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-dr-" + runTag);
            using var scope = FixtureWorkspace.Create("gw1b-scope-" + runTag);
            await File.WriteAllTextAsync(Path.Combine(scope.Root, "secret.txt"), "top-secret-synthetic");
            var c = await ControlRunner.Run("cmd.exe",
                $"/d /c copy \"{scope.Root}\\secret.txt\" \"{ws.Root}\\ctl-stolen.txt\"", ws.Root);
            bool ctlOk = c.Exited && File.Exists(Path.Combine(ws.Root, "ctl-stolen.txt"));
            TryDelete(Path.Combine(ws.Root, "ctl-stolen.txt"));
            if (!ctlOk)
                return AbandonLeg(cleanups, "deniedread", $"GagambaGW1B{runTag}d",
                    $"control copy failed [{c.Detail}]: sentinel unreachable even unsandboxed", ws, scope);
            string idDr = $"GagambaGW1B{runTag}d";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var r = await LaunchLeg(idDr, spec,
                $"/d /c copy \"{scope.Root}\\secret.txt\" \"{ws.Root}\\stolen.txt\"",
                ws.Root, null, null);
            bool deniedAbsent = !File.Exists(Path.Combine(ws.Root, "stolen.txt"));
            string scopeHash = FixtureWorkspace.Sha256Hex(await File.ReadAllBytesAsync(Path.Combine(scope.Root, "secret.txt")));
            string prof = SweepProfile(idDr);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root)
                && scope.DisposeAndReport() == "Confirmed" && !Directory.Exists(scope.Root);
            cleanups.Add(("deniedread", new(r.ChildReaped, true, prof, wsOk)));
            bool ok = r.Ok && deniedAbsent;
            return (ok, $"{r.Detail} stolen-absent={deniedAbsent} sentinel-sha={scopeHash[..16]}.. profile={prof}", (string?)null);
        });

        // ---- L2-TREE-EFFECT (descendant effects land, descendant denial holds) ----
        await RunCase(collector, "L2-TREE-EFFECT", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-tree-" + runTag);
            using var scope = FixtureWorkspace.Create("gw1b-tree-scope-" + runTag);
            using var ctlWs = FixtureWorkspace.Create("gw1b-tree-ctl-" + runTag);
            using var ctlScope = FixtureWorkspace.Create("gw1b-tree-ctlscope-" + runTag);
            WriteBat(ws.Root, "child.bat",
                "@echo off",
                "echo child-content>\"%~1\\tree\\child.txt\"",
                "echo sneak>\"%~2\\sneak.txt\"",
                "exit 0");
            WriteBat(ws.Root, "root.bat",
                "@echo off",
                "mkdir \"%~1\\tree\" 2>nul",
                "start /b \"\" cmd /d /c call \"%~dp0child.bat\" \"%~1\" \"%~2\"",
                "echo root-content>\"%~1\\root.txt\"",
                "exit 3");
            WriteBat(ctlWs.Root, "child.bat",
                "@echo off",
                "echo child-content>\"%~1\\tree\\child.txt\"",
                "echo sneak>\"%~2\\sneak.txt\"",
                "exit 0");
            WriteBat(ctlWs.Root, "root.bat",
                "@echo off",
                "mkdir \"%~1\\tree\" 2>nul",
                "start /b \"\" cmd /d /c call \"%~dp0child.bat\" \"%~1\" \"%~2\"",
                "echo root-content>\"%~1\\root.txt\"",
                "exit 3");
            var c = await ControlRunner.Run("cmd.exe",
                $"/d /c call \"{ctlWs.Root}\\root.bat\" \"{ctlWs.Root}\" \"{ctlScope.Root}\"", ctlWs.Root, 20_000);
            bool ctlOk = c.Exited && c.ExitCode == 3
                && (await PollFileAsync(Path.Combine(ctlWs.Root, "tree", "child.txt"), TimeSpan.FromSeconds(10)) is not null)
                && File.Exists(Path.Combine(ctlWs.Root, "root.txt"))
                && File.Exists(Path.Combine(ctlScope.Root, "sneak.txt"));
            if (!ctlOk)
                return AbandonLeg(cleanups, "tree", $"GagambaGW1B{runTag}t",
                    $"control tree failed [{c.Detail}]: scripts do not work unsandboxed",
                    ws, scope, ctlWs, ctlScope);
            string idTree = $"GagambaGW1B{runTag}t";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var r = await LaunchLeg(idTree, spec,
                $"/d /c call \"{ws.Root}\\root.bat\" \"{ws.Root}\" \"{scope.Root}\"",
                ws.Root, 3, null);
            string? child = await PollFileAsync(Path.Combine(ws.Root, "tree", "child.txt"), TimeSpan.FromSeconds(12));
            await Task.Delay(2000); // grace for the sneak attempt to have been attempted
            bool rootOk = File.Exists(Path.Combine(ws.Root, "root.txt"));
            bool childOk = child?.Trim() == "child-content";
            bool sneakAbsent = !File.Exists(Path.Combine(scope.Root, "sneak.txt"));
            string prof = SweepProfile(idTree);
            bool wsOk = new[] { ws, scope, ctlWs, ctlScope }
                .All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
            cleanups.Add(("tree", new(r.ChildReaped, true, prof, wsOk)));
            bool ok = r.Ok && rootOk && childOk && sneakAbsent;
            return (ok, $"{r.Detail} root={rootOk} child={childOk} descendant-deny={sneakAbsent} profile={prof}", (string?)null);
        });

        // ---- L2-TREE-STOP (kill root; engine job must take the sleeper) ----
        // No timeout.exe: it refuses redirected stdin. Both ends wait on files.
        await RunCase(collector, "L2-TREE-STOP", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-stop-" + runTag);
            using var ctlWs = FixtureWorkspace.Create("gw1b-stop-ctl-" + runTag);
            string[] sleeper =
            [
                "@echo off",
                "echo started>\"%~1\\started.txt\"",
                ":wait",
                "if not exist \"%~1\\barrier.txt\" goto wait",
                "echo late>\"%~1\\late.txt\"",
                "exit 9",
            ];
            string[] root2 =
            [
                "@echo off",
                "start /b \"\" cmd /d /c call \"%~dp0sleeper.bat\" \"%~1\"",
                "echo root-alive>\"%~1\\rootstarted.txt\"",
                ":wait",
                "if not exist \"%~1\\rootbarrier.txt\" goto wait",
                "exit 5",
            ];
            WriteBat(ws.Root, "sleeper.bat", sleeper);
            WriteBat(ws.Root, "root2.bat", root2);
            WriteBat(ctlWs.Root, "sleeper.bat", sleeper);
            WriteBat(ctlWs.Root, "root2.bat", root2);
            // Control, unsandboxed and jobless: orphan must SURVIVE root kill.
            var ctlProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = $"/d /c call \"{ctlWs.Root}\\root2.bat\" \"{ctlWs.Root}\"",
                WorkingDirectory = ctlWs.Root,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (ctlProc is null)
                return AbandonLeg(cleanups, "treestop", $"GagambaGW1B{runTag}s",
                    "control start returned null", ws, ctlWs);
            using (ctlProc)
            {
                string? ctlRoot = await PollFileAsync(Path.Combine(ctlWs.Root, "rootstarted.txt"), TimeSpan.FromSeconds(10));
                string? ctlStarted = await PollFileAsync(Path.Combine(ctlWs.Root, "started.txt"), TimeSpan.FromSeconds(10));
                if (ctlRoot is null || ctlStarted is null)
                {
                    try { ctlProc.Kill(); } catch { }
                    return AbandonLeg(cleanups, "treestop", $"GagambaGW1B{runTag}s",
                        "control tree never formed", ws, ctlWs);
                }
                try { ctlProc.Kill(); } catch { }
                await Task.Delay(2000);
                await File.WriteAllTextAsync(Path.Combine(ctlWs.Root, "barrier.txt"), "go");
                string? ctlLate = await PollFileAsync(Path.Combine(ctlWs.Root, "late.txt"), TimeSpan.FromSeconds(10));
                // Reap a possible surviving orphan before disposing its workspace.
                foreach (var orphan in System.Diagnostics.Process.GetProcessesByName("cmd"))
                {
                    try
                    {
                        if (orphan.StartTime.ToUniversalTime() > ctlProc.StartTime.ToUniversalTime()
                            && (DateTime.UtcNow - orphan.StartTime.ToUniversalTime()).TotalSeconds < 60)
                        { /* candidate only; never kill by fuzzy match */ }
                    }
                    catch { }
                    orphan.Dispose();
                }
                if (ctlLate?.Trim() != "late")
                    return AbandonLeg(cleanups, "treestop", $"GagambaGW1B{runTag}s",
                        "control orphan did not complete after root kill", ws, ctlWs);
            }
            // Sandboxed leg: same shape inside the engine job.
            string idStop = $"GagambaGW1B{runTag}s";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var r = await LaunchLeg(idStop, spec,
                $"/d /c call \"{ws.Root}\\root2.bat\" \"{ws.Root}\"",
                ws.Root, 99, null,
                onRunning: async h =>
                {
                    string? rs = await PollFileAsync(Path.Combine(ws.Root, "rootstarted.txt"), TimeSpan.FromSeconds(10));
                    string? st = await PollFileAsync(Path.Combine(ws.Root, "started.txt"), TimeSpan.FromSeconds(10));
                    if (rs is null || st is null)
                        throw new InvalidOperationException("sandbox tree never formed; refusing to kill blindly");
                    Native.TerminateProcess(h, 99);
                });
            if (!r.Ok)
            {
                string prof0 = SweepProfile(idStop);
                bool wsOk0 = new[] { ws, ctlWs }
                    .All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
                cleanups.Add(("treestop", new(r.ChildReaped, true, prof0, wsOk0)));
                return (false, r.Detail + " (root-kill leg failed)", (string?)null);
            }
            await Task.Delay(2000); // let a surviving sleeper (if any) settle
            await File.WriteAllTextAsync(Path.Combine(ws.Root, "barrier.txt"), "go");
            await Task.Delay(3000);
            bool lateAbsent = !File.Exists(Path.Combine(ws.Root, "late.txt"));
            string killVerdict = lateAbsent ? "kill-stops-tree" : "KILL-DOES-NOT-STOP-TREE";
            // Natural-exit variant: root exits by itself while the sleeper waits.
            // Distinguishes "terminate doesn't propagate" from "exit doesn't propagate".
            using var ws2 = FixtureWorkspace.Create("gw1b-stopnat-" + runTag);
            WriteBat(ws2.Root, "sleeper.bat",
                "@echo off",
                "echo started>\"%~1\\started.txt\"",
                ":wait",
                "if not exist \"%~1\\barrier.txt\" goto wait",
                "echo late>\"%~1\\late.txt\"",
                "exit 9");
            WriteBat(ws2.Root, "root3.bat",
                "@echo off",
                "start /b \"\" cmd /d /c call \"%~dp0sleeper.bat\" \"%~1\"",
                "exit 5");
            string idStopNat = $"GagambaGW1B{runTag}n";
            var specNat = new SandboxSpecRequest("0.1.0", true, [ws2.Root], []);
            var rn = await LaunchLeg(idStopNat, specNat,
                $"/d /c call \"{ws2.Root}\\root3.bat\" \"{ws2.Root}\"",
                ws2.Root, 5, null);
            bool natLateAbsent = false;
            string natVerdict = "natural-variant-not-run";
            if (rn.Ok)
            {
                string? natStarted = await PollFileAsync(Path.Combine(ws2.Root, "started.txt"), TimeSpan.FromSeconds(10));
                if (natStarted is not null)
                {
                    await Task.Delay(3000); // root long gone; sleeper still waiting
                    await File.WriteAllTextAsync(Path.Combine(ws2.Root, "barrier.txt"), "go");
                    await Task.Delay(3000);
                    natLateAbsent = !File.Exists(Path.Combine(ws2.Root, "late.txt"));
                    natVerdict = natLateAbsent ? "exit-stops-tree" : "EXIT-DOES-NOT-STOP-TREE";
                }
                else
                {
                    natVerdict = "natural-sleeper-never-started";
                }
            }
            else
            {
                natVerdict = "natural-root-leg-failed";
            }
            string profNat = SweepProfile(idStopNat);
            string prof = SweepProfile(idStop);
            bool wsOk = new[] { ws, ctlWs, ws2 }
                .All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
            cleanups.Add(("treestop", new(r.ChildReaped && rn.ChildReaped, true, prof + "+" + profNat, wsOk)));
            bool ok = lateAbsent && natLateAbsent;
            return ok
                ? (true, $"{r.Detail} kill:{killVerdict} natural:{natVerdict} profile={prof}", (string?)null)
                : (false, $"{r.Detail} kill:{killVerdict} natural:{natVerdict} (descendant outlived root; tree-ownership gap) profile={prof}", (string?)null);
        });

        // ---- L2-CANCEL-RACE (terminate mid-run, prove reaping + cleanup) ----
        await RunCase(collector, "L2-CANCEL-RACE", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-cancel-" + runTag);
            WriteBat(ws.Root, "wait.bat",
                "@echo off",
                ":wait",
                "if not exist \"%~1\\cancel.txt\" goto wait",
                "exit 0");
            var ctlProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = $"/d /c call \"{ws.Root}\\wait.bat\" \"{ws.Root}\"",
                WorkingDirectory = ws.Root,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (ctlProc is null)
                return AbandonLeg(cleanups, "cancel", $"GagambaGW1B{runTag}c",
                    "control start returned null", ws);
            using (ctlProc)
            {
                await Task.Delay(500);
                bool wasRunning = !ctlProc.HasExited;
                if (wasRunning)
                {
                    try { ctlProc.Kill(); } catch { }
                }
                bool exited = ctlProc.WaitForExit(5000);
                if (!wasRunning || !exited)
                    return AbandonLeg(cleanups, "cancel", $"GagambaGW1B{runTag}c",
                        "control wait/kill shape broken", ws);
            }
            string idCancel = $"GagambaGW1B{runTag}c";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var r = await LaunchLeg(idCancel, spec,
                $"/d /c call \"{ws.Root}\\wait.bat\" \"{ws.Root}\"", ws.Root, 99,
                null,
                onRunning: async h =>
                {
                    await Task.Delay(500);
                    Native.TerminateProcess(h, 99);
                });
            string prof = SweepProfile(idCancel);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("cancel", new(r.ChildReaped, true, prof, wsOk)));
            return (r.Ok, r.Detail + $" profile={prof}", (string?)null);
        });

        // ---- L2-STDIO-REDIRECT (STARTUPINFO handle transport proof) ----
        // Run 1: plain non-inheritable file handle -> ERROR_INVALID_DATA.
        // Run 2: HANDLE_FLAG_INHERIT file handle -> ERROR_INVALID_DATA.
        // This run: all three handles set (stdin = NUL device handle).
        await RunCase(collector, "L2-STDIO-REDIRECT", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-stdio-" + runTag);
            string outPath = Path.Combine(ws.Root, "redir-out.txt");
            string errPath = Path.Combine(ws.Root, "redir-err.txt");
            await File.WriteAllTextAsync(outPath, string.Empty);
            await File.WriteAllTextAsync(errPath, string.Empty);
            bool transported = false;
            string detail;
            string idStdio = $"GagambaGW1B{runTag}o";
            using (var stream = new FileStream(outPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var errStream = new FileStream(errPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var nul = new FileStream("NUL", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IntPtr hOut = stream.SafeFileHandle.DangerousGetHandle();
                IntPtr hErr = errStream.SafeFileHandle.DangerousGetHandle();
                IntPtr hIn = nul.SafeFileHandle.DangerousGetHandle();
                bool marked = Native.SetHandleInformation(hOut, 1, 1)
                    & Native.SetHandleInformation(hErr, 1, 1)
                    & Native.SetHandleInformation(hIn, 1, 1);
                if (!marked)
                    return AbandonLeg(cleanups, "stdio", idStdio,
                        $"SetHandleInformation failed err={Marshal.GetLastWin32Error()}", ws);
                var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
                var r = await LaunchLeg(idStdio, spec,
                    "/d /c echo hello-redirect", ws.Root, 0, null,
                    hStdOut: hOut, hStdErr: hErr, hStdIn: hIn);
                detail = r.Detail + " all-three-handles-inheritable";
                transported = r.Ok;
            }
            string captured = string.Empty;
            try { captured = (await File.ReadAllTextAsync(outPath)).Trim(); } catch { }
            bool exact = transported && captured == "hello-redirect";
            string prof = SweepProfile(idStdio);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("stdio", new(true, true, prof, wsOk)));
            return exact
                ? (true, $"{detail} captured-exact profile={prof}", (string?)null)
                : (false, $"{detail} captured='{captured[..Math.Min(60, captured.Length)]}' (transport unproven; file-effect pattern remains the supported oracle) profile={prof}", (string?)null);
        });

        // ---- L3-PIPE-STDIO (anonymous-pipe transport proof) ----
        // File-handle redirection was rejected in 3 configurations. Pipes are
        // the canonical CreateProcess redirection mechanism; if the engine
        // rejects these too, STARTUPINFO redirection is unsupported outright.
        await RunCase(collector, "L3-PIPE-STDIO", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-pipe-" + runTag);
            string idPipe = $"GagambaGW1B{runTag}q";
            var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root], []);
            var (ok, detail, capturedOut, _) = await RunPipedAsync(idPipe, spec, null,
                "/d /c echo hello-pipe", ws.Root, 0);
            bool exact = ok && capturedOut.Trim() == "hello-pipe";
            string prof = SweepProfile(idPipe);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("pipestdio", new(true, true, prof, wsOk)));
            return exact
                ? (true, $"{detail} pipe-captured-exact profile={prof}", (string?)null)
                : (false, $"{detail} pipe-captured='{capturedOut[..Math.Min(60, capturedOut.Length)]}' profile={prof}", (string?)null);
        });

        // ---- L3-SUPERVISOR-SWEEP (ToolHelp tree kill; control + sandbox) ----
        await RunCase(collector, "L3-SUPERVISOR-SWEEP", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-sup-" + runTag);
            using var ctlWs = FixtureWorkspace.Create("gw1b-sup-ctl-" + runTag);
            string[] sleeper =
            [
                "@echo off",
                "echo started>\"%~1\\started.txt\"",
                ":wait",
                "if not exist \"%~1\\barrier.txt\" goto wait",
                "echo late>\"%~1\\late.txt\"",
                "exit 9",
            ];
            string[] root2 =
            [
                "@echo off",
                "start /b \"\" cmd /d /c call \"%~dp0sleeper.bat\" \"%~1\"",
                "echo root-alive>\"%~1\\rootstarted.txt\"",
                ":wait",
                "if not exist \"%~1\\rootbarrier.txt\" goto wait",
                "exit 5",
            ];
            WriteBat(ws.Root, "sleeper.bat", sleeper);
            WriteBat(ws.Root, "root2.bat", root2);
            WriteBat(ctlWs.Root, "sleeper.bat", sleeper);
            WriteBat(ctlWs.Root, "root2.bat", root2);
            // Control: identical mechanism, unsandboxed tree.
            DateTime ctlSince = DateTime.UtcNow;
            var ctlProc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = $"/d /c call \"{ctlWs.Root}\\root2.bat\" \"{ctlWs.Root}\"",
                WorkingDirectory = ctlWs.Root,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (ctlProc is null)
                return AbandonLeg(cleanups, "supervisor", $"GagambaGW1B{runTag}v",
                    "control start returned null", ws, ctlWs);
            using (ctlProc)
            {
                string? ctlStarted = await PollFileAsync(Path.Combine(ctlWs.Root, "started.txt"), TimeSpan.FromSeconds(10));
                if (ctlStarted is null)
                {
                    try { ctlProc.Kill(); } catch { }
                    return AbandonLeg(cleanups, "supervisor", $"GagambaGW1B{runTag}v",
                        "control tree never formed", ws, ctlWs);
                }
                var (ctlFound, ctlKilled, ctlNotes) = Supervisor.SweepDescendants(
                    (uint)ctlProc.Id, ctlSince, TimeSpan.FromSeconds(5));
                bool ctlRootAlive = !ctlProc.HasExited;
                await File.WriteAllTextAsync(Path.Combine(ctlWs.Root, "barrier.txt"), "go");
                await Task.Delay(2000);
                bool ctlLateAbsent = !File.Exists(Path.Combine(ctlWs.Root, "late.txt"));
                try { ctlProc.Kill(); } catch { }
                if (ctlFound < 1 || ctlKilled < 1 || !ctlRootAlive || !ctlLateAbsent)
                    return AbandonLeg(cleanups, "supervisor", $"GagambaGW1B{runTag}v",
                        $"control sweep broken: found={ctlFound} killed={ctlKilled} rootAlive={ctlRootAlive} lateAbsent={ctlLateAbsent} [{string.Join(";", ctlNotes)}]",
                        ws, ctlWs);
            }
            // Sandboxed leg: enumerate across the boundary, sweep, verify.
            string idSup = $"GagambaGW1B{runTag}v";
            DateTime sbSince = DateTime.UtcNow;
            var (nlOk, nlDetail, hRoot, rootPid) = LaunchNoWait(idSup,
                new SandboxSpecRequest("0.1.0", true, [ws.Root], []),
                $"/d /c call \"{ws.Root}\\root2.bat\" \"{ws.Root}\"",
                ws.Root);
            if (!nlOk)
            {
                string prof0 = SweepProfile(idSup);
                bool wsOk0 = new[] { ws, ctlWs }
                    .All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
                cleanups.Add(("supervisor", new(true, true, prof0, wsOk0)));
                return (false, nlDetail + " (launch failed; no sweep verdict)", (string?)null);
            }
            try
            {
                string? sbStarted = await PollFileAsync(Path.Combine(ws.Root, "started.txt"), TimeSpan.FromSeconds(10));
                if (sbStarted is null)
                    return (false, "sandbox tree never formed; refusing sweep", (string?)null);
                var (found, killed, notes) = Supervisor.SweepDescendants(rootPid, sbSince, TimeSpan.FromSeconds(5));
                bool rootAlive = Native.WaitForSingleObject(hRoot, 0) == Native.WAIT_TIMEOUT;
                await File.WriteAllTextAsync(Path.Combine(ws.Root, "barrier.txt"), "go");
                await Task.Delay(3000);
                bool lateAbsent = !File.Exists(Path.Combine(ws.Root, "late.txt"));
                Native.TerminateProcess(hRoot, 99);
                Native.WaitForSingleObject(hRoot, 5000);
                bool rootDead = Native.GetExitCodeProcess(hRoot, out uint rc) && rc != Native.STILL_ACTIVE;
                string prof = SweepProfile(idSup);
                bool wsOk = new[] { ws, ctlWs }
                    .All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
                cleanups.Add(("supervisor", new(rootDead, true, prof, wsOk)));
                bool ok = found >= 1 && killed >= 1 && rootAlive && lateAbsent && rootDead;
                return ok
                    ? (true, $"cross-boundary enumerate found={found} killed={killed}; root survived sweep then terminated; late-absent profile={prof} [{string.Join(";", notes)}]", (string?)null)
                    : (false, $"found={found} killed={killed} rootAliveDuringSweep={rootAlive} lateAbsent={lateAbsent} rootDead={rootDead} [{string.Join(";", notes)}]", (string?)null);
            }
            finally
            {
                try { if (hRoot != IntPtr.Zero) Native.CloseHandle(hRoot); } catch { }
            }
        });

        // ---- L3-WORKLOAD-DOTNET (runtime closure mapping) ----
        // dotnet launches but exits 1 silently. Map what CAN run: whoami (system
        // binary, no grants) tells whether the base image covers System32;
        // dotnet variants then climb the grant/capability ladder.
        // Shared with L4-WORKLOAD-BUILD: the winning closure, if any.
        List<string> dotnetRo = [];
        string dotnetCaps = "";
        bool dotnetWorks = false;
        await RunCase(collector, "L3-WORKLOAD-DOTNET", async () =>
        {
            const string dotnetExe = @"C:\Program Files\dotnet\dotnet.exe";
            string whoamiEarly = Path.Combine(Environment.SystemDirectory, "whoami.exe");
            if (!File.Exists(dotnetExe) || !File.Exists(whoamiEarly))
                return AbandonLeg(cleanups, "dotnet", $"GagambaGW1B{runTag}n",
                    "dotnet or whoami missing at well-known paths", Array.Empty<FixtureWorkspace>());
            var c = await ControlRunner.Run(dotnetExe, "--info", Path.GetTempPath(), 60_000);
            if (!c.Exited || c.ExitCode != 0 || !c.Stdout.Contains("SDK"))
                return AbandonLeg(cleanups, "dotnet", $"GagambaGW1B{runTag}n",
                    $"control dotnet --info failed [{c.Detail}]", Array.Empty<FixtureWorkspace>());
            using var wsDotnetCtl = FixtureWorkspace.Create("gw1b-dotnetctl-" + runTag);
            Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
            string dotnetDir = Path.GetDirectoryName(dotnetExe)!;
            string system32 = Environment.SystemDirectory;
            string whoamiExe = Path.Combine(system32, "whoami.exe");
            string idDotnet = $"GagambaGW1B{runTag}n";
            var attempts = new List<string>();
            bool sdkListed = false;
            List<string> winningRo = [];
            string winningCaps = "";
            var variantCleanups = new List<string>();
            bool variantWsOk = true;
            // S0 first: TWO read-only temp grants with cmd. If multi-ro itself
            // is rejected, every multi-grant result below is confounded.
            // Every variant below gets a FRESH workspace: a failed attempt can
            // poison its grant path for later attempts (measured stickiness).
            async Task<(bool Ran, string Detail)> TryVariant(string tag,
                Func<string, SandboxSpecRequest> specOf,
                string exe, string vArgs, Func<string, bool> accept)
            {
                using var vws = FixtureWorkspace.Create($"gw1b-dn-{tag}-" + runTag);
                Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", vws.Root);
                string vid = idDotnet + tag;
                var (ok, detail, vOut, vErr) = await RunPipedAsync(vid, specOf(vws.Root), exe, vArgs, vws.Root, 0);
                string prof = SweepProfile(vid);
                bool wsOk = vws.DisposeAndReport() == "Confirmed" && !Directory.Exists(vws.Root);
                variantCleanups.Add($"{tag}:{prof}/ws={wsOk}");
                if (!wsOk) variantWsOk = false;
                bool ran = ok && accept(vOut);
                string shape = exe == whoamiExe
                    ? (vOut.Contains('\\') && vOut.Trim().Length < 160 ? "shape-ok" : $"shape-bad(len={vOut.Length})")
                    : $"out={vOut.Length}B";
                // First 300 chars of stderr (single line) so host/runtime
                // diagnostics are visible in the evidence, not just lengths.
                string errHead = vErr.Replace("\r", " ").Replace("\n", " ").Trim();
                if (errHead.Length > 300) errHead = errHead[..300] + "...";
                return (ran, $"{detail} {shape} errlen={vErr.Length} err='{errHead}' profile={prof}");
            }
            using var roA = FixtureWorkspace.Create("gw1b-roA-" + runTag);
            using var roB = FixtureWorkspace.Create("gw1b-roB-" + runTag);
            var s0 = await TryVariant("s0",
                wsRoot => new("0.1.0", true, [wsRoot], [roA.Root, roB.Root]),
                Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c echo count-ok",
                o => o.Trim() == "count-ok");
            attempts.Add($"S0 cmd-ro2-temp: ran={s0.Ran} {s0.Detail}");
            // S0b separates "at most one ro grant" from "at most two grants
            // total": TWO read-write temp grants, zero ro.
            var s0b = await TryVariant("t0",
                wsRoot => new("0.1.0", true, [wsRoot, roA.Root], []),
                Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c echo count-ok",
                o => o.Trim() == "count-ok");
            attempts.Add($"S0b cmd-rw2-temp: ran={s0b.Ran} {s0b.Detail}");
            var w1 = await TryVariant("a",
                wsRoot => new("0.1.0", true, [wsRoot], []), whoamiExe, string.Empty,
                o => o.Contains('\\'));
            attempts.Add($"W1 whoami-ws-only: ran={w1.Ran} {w1.Detail}");
            var w2 = await TryVariant("b",
                wsRoot => new("0.1.0", true, [wsRoot], []), dotnetExe, "--info",
                o => o.Contains("SDK"));
            attempts.Add($"W2 dotnet-ws-only: ran={w2.Ran} {w2.Detail}");
            if (w2.Ran) { sdkListed = true; }
            if (!sdkListed)
            {
                var w3 = await TryVariant("c",
                    wsRoot => new("0.1.0", true, [wsRoot], [dotnetDir]), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W3 dotnet-ro-dotnetdir: ran={w3.Ran} {w3.Detail}");
                if (w3.Ran) { sdkListed = true; winningRo = [dotnetDir]; }
            }
            if (!sdkListed)
            {
                var w4 = await TryVariant("d",
                    wsRoot => new("0.1.0", true, [wsRoot], [dotnetDir, system32]), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W4 dotnet-full-closure: ran={w4.Ran} {w4.Detail}");
                if (w4.Ran) { sdkListed = true; winningRo = [dotnetDir, system32]; }
            }
            if (!sdkListed)
            {
                var w5 = await TryVariant("e",
                    wsRoot => new("0.1.0", true, [wsRoot], [dotnetDir], "registryRead"), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W5 dotnet-registryRead: ran={w5.Ran} {w5.Detail}");
                if (w5.Ran) { sdkListed = true; winningRo = [dotnetDir]; winningCaps = "registryRead"; }
            }
            if (!sdkListed)
            {
                var w6 = await TryVariant("f",
                    wsRoot => new("0.1.0", true, [wsRoot], [dotnetDir, system32], "registryRead"), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W6 dotnet-full-registryRead: ran={w6.Ran} {w6.Detail}");
                if (w6.Ran) { sdkListed = true; winningRo = [dotnetDir, system32]; winningCaps = "registryRead"; }
            }
            bool roOk = roA.DisposeAndReport() == "Confirmed" && !Directory.Exists(roA.Root)
                && roB.DisposeAndReport() == "Confirmed" && !Directory.Exists(roB.Root);
            bool ctlOk = wsDotnetCtl.DisposeAndReport() == "Confirmed" && !Directory.Exists(wsDotnetCtl.Root);
            string profAll = string.Join("+", variantCleanups);
            bool profOk = variantCleanups.All(e =>
                e.Contains(":deleted(") || e.Contains(":deleted-never-materialized("));
            // Disposition must start with a canonical token so the aggregate
            // cleanup check can classify it; the per-variant detail follows.
            string dotnetDisp = profOk
                ? $"deleted(all-dotnet-variants: {profAll})"
                : $"deleted-UNCERTAIN({profAll})";
            cleanups.Add(("dotnet", new(profOk, true, dotnetDisp, variantWsOk && roOk && ctlOk)));
            if (sdkListed) { dotnetRo = new(winningRo); dotnetCaps = winningCaps; dotnetWorks = true; }
            return sdkListed
                ? (true, $"dotnet --info ran, SDK listed; {string.Join(" | ", attempts)} profile={profAll}", (string?)null)
                : (false, $"{string.Join(" | ", attempts)} profile={profAll}", (string?)null);
        });

        // ---- L4-CRASH-RECOVERY (launcher gone; recovery by recorded PID) ----
        // Proves recovery AFTER the launcher is gone. The launcher closes every
        // handle at release (no wait/terminate/query retained): recovery uses
        // only the recorded PID, exactly like a dead launcher would. Order
        // matters: the sleeper must be verified ALIVE before any sweep, and the
        // barrier is never opened, so a survivor would leave no trace unless
        // explicitly killed. (Engine auto-cleanup was already disproven; this
        // leg needs no holder process — see the evidence doc for why.)
        await RunCase(collector, "L4-CRASH-RECOVERY", async () =>
        {
            using var ws = FixtureWorkspace.Create("gw1b-rcv-" + runTag);
            WriteBat(ws.Root, "sleeper.bat",
                "@echo off",
                "echo started>\"%~1\\started.txt\"",
                ":wait",
                "if not exist \"%~1\\barrier.txt\" goto wait",
                "echo late>\"%~1\\late.txt\"",
                "exit 9");
            WriteBat(ws.Root, "root2.bat",
                "@echo off",
                "start /b \"\" cmd /d /c call \"%~dp0sleeper.bat\" \"%~1\"",
                "echo root-alive>\"%~1\\rootstarted.txt\"",
                ":wait",
                "if not exist \"%~1\\rootbarrier.txt\" goto wait",
                "exit 5");
            string idCrash = $"GagambaGW1B{runTag}k";
            DateTime since = DateTime.UtcNow;
            // Settle delay: the bats were written milliseconds ago; a concurrent
            // AV/indexer scan lock during engine setup is a suspect for a rare
            // vanilla-shape INVALID_DATA (legs with a control phase never hit it).
            await Task.Delay(3000);
            var (nlOk, nlDetail, hRoot, rootPid) = LaunchNoWait(idCrash,
                new SandboxSpecRequest("0.1.0", true, [ws.Root], []),
                $"/d /c call \"{ws.Root}\\root2.bat\" \"{ws.Root}\"",
                ws.Root);
            if (!nlOk)
            {
                try { if (hRoot != IntPtr.Zero) Native.CloseHandle(hRoot); } catch { }
                return AbandonLeg(cleanups, "crash", idCrash, $"launch failed [{nlDetail}]", ws);
            }
            // Launcher death: relinquish every handle immediately.
            try { if (hRoot != IntPtr.Zero) Native.CloseHandle(hRoot); } catch { }
            hRoot = IntPtr.Zero;
            string? started = await PollFileAsync(Path.Combine(ws.Root, "started.txt"), TimeSpan.FromSeconds(10));
            if (started is null)
                return AbandonLeg(cleanups, "crash", idCrash,
                    "sleeper never started; nothing to recover", ws);
            // No barrier is opened here: the sleeper must still be alive, which
            // is the proof that the engine left the tree running after the
            // launcher vanished. Then sweep purely by recorded PID.
            await Task.Delay(2000);
            bool sleeperAlive = Supervisor.DescendantsOf(rootPid)
                .Any(m => m.Exe.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase));
            var (found, killed, notes) = Supervisor.SweepDescendants(rootPid, since, TimeSpan.FromSeconds(5));
            bool rootDead = KillByPid(rootPid);
            bool lateAbsent = !File.Exists(Path.Combine(ws.Root, "late.txt"));
            string prof = SweepProfile(idCrash);
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            cleanups.Add(("crash", new(rootDead, true, prof, wsOk)));
            bool ok = sleeperAlive && found >= 1 && killed >= 1 && rootDead && lateAbsent;
            return ok
                ? (true, $"launcher gone; sleeper alive before sweep={sleeperAlive}; sweep found={found} killed={killed} root dead; never-barrier late-absent={lateAbsent} profile={prof} [{string.Join(";", notes)}]", (string?)null)
                : (false, $"sleeperAlive={sleeperAlive} found={found} killed={killed} rootDead={rootDead} lateAbsent={lateAbsent} profile={prof} [{string.Join(";", notes)}]", (string?)null);
        });

        // ---- L4-WORKLOAD-GIT (canary + repo inspection with explicit grants) ----
        // The installed tree (C:\Program Files\Git and every subdir probed) is
        // rejected as a grant with ERROR_INVALID_DATA, deterministically, while
        // dotnet/Common Files/Windows and copies under C:\temp bind fine. So
        // the workload stages a runnable Git closure into a clean root first
        // (provider pattern: stage dependency closures, don't grant installs).
        await RunCase(collector, "L4-WORKLOAD-GIT", async () =>
        {
            const string sysGit = @"C:\Program Files\Git\bin\git.exe";
            if (!File.Exists(sysGit))
                return AbandonLeg(cleanups, "git", $"GagambaGW1B{runTag}g",
                    "system git not at well-known path", Array.Empty<FixtureWorkspace>());
            Environment.SetEnvironmentVariable("GIT_OPTIONAL_LOCKS", "0");
            // Clean staging root: a drive-root "temp" dir (created if needed),
            // which carries none of the foreign ACEs found on %TEMP%.
            string? cleanRoot = TryCleanStageRoot();
            if (cleanRoot is null)
                return AbandonLeg(cleanups, "git", $"GagambaGW1B{runTag}g",
                    "no clean staging root available", Array.Empty<FixtureWorkspace>());
            string stageRoot = Path.Combine(cleanRoot, "gw1b-gitport-" + runTag);
            var swStage = Stopwatch.StartNew();
            CopyDirExcluding(Path.GetDirectoryName(Path.GetDirectoryName(sysGit)!)!, stageRoot,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            swStage.Stop();
            string stagedGit = Path.Combine(stageRoot, "bin", "git.exe");
            bool stagedOk = true;
            try
            {
                if (!File.Exists(stagedGit))
                    stagedOk = false;
                using var ctlGitWs = FixtureWorkspace.Create("gw1b-gitctl-" + runTag);
                string ctlCopy = Path.Combine(ctlGitWs.Root, "repo");
                CopyDirExcluding(repoRoot, ctlCopy, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "bin", "obj", "artifacts", ".vs", ".idea", "TestResults" });
                var c1 = stagedOk
                    ? await ControlRunner.Run(stagedGit, "--version", ctlGitWs.Root, 30_000)
                    : new DirectResult(false, -1, "staged git missing", string.Empty);
                var c2 = stagedOk && c1.Exited && c1.ExitCode == 0
                    ? await ControlRunner.Run(stagedGit, $"-C \"{ctlCopy}\" status --porcelain", ctlGitWs.Root, 60_000)
                    : new DirectResult(false, -1, "skipped", string.Empty);
                bool ctlGitOk = ctlGitWs.DisposeAndReport() == "Confirmed" && !Directory.Exists(ctlGitWs.Root);
                if (!c1.Exited || !c1.Stdout.Contains("git version") || !c2.Exited || c2.ExitCode != 0 || !ctlGitOk)
                    return AbandonLeg(cleanups, "git", $"GagambaGW1B{runTag}g",
                        $"staged controls failed: version[{c1.Detail}] status[{c2.Detail}] ctlWs={ctlGitOk} (stage {swStage.Elapsed.TotalSeconds:F0}s)");
                // Attempts on fresh workspaces (transient INVALID_DATA recovers
                // on fresh identity+workspace; never fall back to unconfined).
                string gitDetail = "no attempt";
                bool gitOk = false;
                var gitProfs = new List<string>();
                bool gitWsOk = true;
                for (int attempt = 1; attempt <= 2 && !gitOk; attempt++)
                {
                    using var aws = FixtureWorkspace.Create($"gw1b-git{attempt}-" + runTag);
                    string repoCopy = Path.Combine(aws.Root, "repo");
                    CopyDirExcluding(repoRoot, repoCopy, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { "bin", "obj", "artifacts", ".vs", ".idea", "TestResults" });
                    string aidGit = $"GagambaGW1B{runTag}g{attempt}";
                    // repoCopy lives under aws.Root (covered by the single rw
                    // grant); the staged closure is the single ro grant.
                    var gspec = new SandboxSpecRequest("0.1.0", true, [aws.Root], [stageRoot]);
                var (okVer, dVer, oVer, _) = await RunPipedAsync(aidGit + "a", gspec, stagedGit, "--version", aws.Root, 0);
                bool versionOk = okVer && oVer.Contains("git version");
                // `git status` may need a writable temp dir; the inherited
                // host %TEMP% is outside grants, so point TMP/TEMP at a
                // workspace subdir for this launch only, then restore.
                string tmpDir = Path.Combine(aws.Root, "tmp");
                Directory.CreateDirectory(tmpDir);
                string? oldTmp = Environment.GetEnvironmentVariable("TMP");
                string? oldTemp = Environment.GetEnvironmentVariable("TEMP");
                Environment.SetEnvironmentVariable("TMP", tmpDir);
                Environment.SetEnvironmentVariable("TEMP", tmpDir);
                bool okStatus;
                string dStatus, oStatus, eStatus;
                try
                {
                // Workload: rev-parse HEAD (needs .git/HEAD+refs), then
                // status --porcelain (worktree scan). Both resolve the cwd
                // by walking ancestors, which fails under AppContainer when
                // a parent dir (e.g. C:\) denies list access -- see the
                // evidence doc. RunPipedAsync always drains the pipes now,
                // so the childs fatal stays in evidence.
                (okStatus, dStatus, oStatus, eStatus) = await RunPipedAsync(aidGit + "b", gspec, stagedGit,
                    "rev-parse HEAD", repoCopy, 0);
                string revErr = eStatus.Replace("\r", " ").Replace("\n", " ").Trim();
                if (revErr.Length > 200) revErr = revErr[..200] + "...";
                var (okSt, dSt, oSt, eSt) = await RunPipedAsync(aidGit + "c", gspec, stagedGit,
                    "status --porcelain", repoCopy, 0);
                string stErr = eSt.Replace("\r", " ").Replace("\n", " ").Trim();
                if (stErr.Length > 200) stErr = stErr[..200] + "...";
                gitDetail = $"try{attempt}: version[{dVer}] revparse[{dStatus}] out='{oStatus.Trim()}' err='{revErr}' status[{dSt}] out='{oSt.Trim()[..Math.Min(80, oSt.Trim().Length)]}' err='{stErr}'";
                okStatus = okSt;
                oStatus = oSt;
                eStatus = eSt;
                }
                finally
                {
                    Environment.SetEnvironmentVariable("TMP", oldTmp);
                    Environment.SetEnvironmentVariable("TEMP", oldTemp);
                }
                bool statusOk = okStatus && oStatus.Trim().Length < 4096;
                gitOk = versionOk && statusOk;
                string aprof = SweepProfile(aidGit + "a") + "+" + SweepProfile(aidGit + "b") + "+" + SweepProfile(aidGit + "c");
                bool awsOk = aws.DisposeAndReport() == "Confirmed" && !Directory.Exists(aws.Root);
                if (!awsOk) gitWsOk = false;
                gitProfs.Add(aprof);
            }
                string gitProfAll = string.Join("+", gitProfs);
                bool gitProfOk = gitProfs.All(p => p.Split('+').All(q =>
                    q.StartsWith("deleted(") || q.StartsWith("deleted-never-materialized(")));
                cleanups.Add(("gitwork", new(gitProfOk, true, gitProfAll, gitWsOk)));
                return gitOk
                    ? (true, $"staged git --version + status --porcelain in sandbox (grants rw=[ws], ro=[staged]); {gitDetail} profile={gitProfAll}", (string?)null)
                    : (false, $"{gitDetail} profile={gitProfAll}", (string?)null);
            }
            finally
            {
                // Staging is test-owned: always remove it.
                try { if (Directory.Exists(stageRoot)) Directory.Delete(stageRoot, recursive: true); } catch { }
            }
        });

        // ---- L3-WORKLOAD-PIDPROBE (managed self-introspection under sandbox) ----
        // The dotnet CLI dies in InstallerBase via GetProcessById; this leg
        // asks whether ANY managed process-introspection works in-sandbox,
        // using a staged self-contained probe (no runtime grant needed).
        await RunCase(collector, "L3-WORKLOAD-PIDPROBE", async () =>
        {
            string? cleanRoot = TryCleanStageRoot();
            if (cleanRoot is null)
                return AbandonLeg(cleanups, "pidprobe", $"GagambaGW1B{runTag}p",
                    "no clean staging root available", Array.Empty<FixtureWorkspace>());
            string stageRoot = Path.Combine(cleanRoot, "gw1b-pidprobe-" + runTag);
            try
            {
                // Closure prep is unsandboxed (same trust as the git leg).
                var pub = await ControlRunner.Run("dotnet",
                    $"publish \"{Path.Combine(repoRoot, "spikes", "PidProbe", "PidProbe.csproj")}\" -c Release -r win-x64 --self-contained -o \"{stageRoot}\"",
                    repoRoot, 300_000);
                if (!pub.Exited || pub.ExitCode != 0 || !File.Exists(Path.Combine(stageRoot, "PidProbe.exe")))
                    return AbandonLeg(cleanups, "pidprobe", $"GagambaGW1B{runTag}p",
                        $"probe publish failed [{pub.Detail}]", Array.Empty<FixtureWorkspace>());
                string pidDetail = "no attempt";
                bool pidOk = false;
                var pidProfs = new List<string>();
                bool pidWsOk = true;
                for (int attempt = 1; attempt <= 2 && !pidOk; attempt++)
                {
                    using var aws = FixtureWorkspace.Create($"gw1b-pid{attempt}-" + runTag);
                    string aidPid = $"GagambaGW1B{runTag}p{attempt}";
                    var pspec = new SandboxSpecRequest("0.1.0", true, [aws.Root], [stageRoot]);
                    var (okP, dP, oP, eP) = await RunPipedAsync(aidPid, pspec,
                        Path.Combine(stageRoot, "PidProbe.exe"), "", aws.Root, 0);
                    string errHead = eP.Replace("\r", " ").Replace("\n", " ").Trim();
                    if (errHead.Length > 300) errHead = errHead[..300] + "...";
                    string outHead = oP.Trim();
                    if (outHead.Length > 600) outHead = outHead[..600] + "...";
                    pidDetail = $"try{attempt}: probe[{dP}] out='{outHead}' err='{errHead}'";
                    pidOk = okP && oP.Contains("pidprobe done");
                    string aprof = SweepProfile(aidPid);
                    bool awsOk = aws.DisposeAndReport() == "Confirmed" && !Directory.Exists(aws.Root);
                    if (!awsOk) pidWsOk = false;
                    pidProfs.Add(aprof);
                }
                string pidProfAll = string.Join("+", pidProfs);
                bool pidProfOk = pidProfs.All(p => p.Split('+').All(q =>
                    q.StartsWith("deleted(") || q.StartsWith("deleted-never-materialized(")));
                cleanups.Add(("pidprobe", new(pidProfOk, true, pidProfAll, pidWsOk)));
                return pidOk
                    ? (true, $"staged self-contained PidProbe introspection in sandbox (grants rw=[ws], ro=[staged]); {pidDetail} profile={pidProfAll}", (string?)null)
                    : (false, $"{pidDetail} profile={pidProfAll}", (string?)null);
            }
            finally
            {
                // Staging is test-owned: always remove it.
                try { if (Directory.Exists(stageRoot)) Directory.Delete(stageRoot, recursive: true); } catch { }
            }
        });

        // ---- L4-WORKLOAD-BUILD (offline compile; gated on runtime) ----
        if (!dotnetWorks)
        {
            collector.Add("L4-WORKLOAD-BUILD", "NotRun",
                "dotnet runtime gate open (see L3-WORKLOAD-DOTNET); build needs a working runtime", 0);
            cleanups.Add(("build", new(true, true, "deleted-never-materialized(no-launch)", true)));
        }
        else
        {
            await RunCase(collector, "L4-WORKLOAD-BUILD", async () =>
            {
                using var ws = FixtureWorkspace.Create("gw1b-build-" + runTag);
                CopyDirExcluding(
                    Path.Combine(repoRoot, "spikes", "Gw1bLaunch", "fixtures", "mini"),
                    Path.Combine(ws.Root, "mini"),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj" });
                Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", ws.Root);
                string proj = Path.Combine(ws.Root, "mini", "mini.csproj");
                // Preparation (unsandboxed, network allowed): restore first.
                var rc = await ControlRunner.Run(
                    @"C:\Program Files\dotnet\dotnet.exe", $"restore \"{proj}\"", ws.Root, 120_000);
                if (!rc.Exited || rc.ExitCode != 0)
                    return AbandonLeg(cleanups, "build", $"GagambaGW1B{runTag}e",
                        $"control restore failed [{rc.Detail}]", ws);
                string idBuild = $"GagambaGW1B{runTag}e";
                string dotnetExe2 = @"C:\Program Files\dotnet\dotnet.exe";
                string sdkDir = Directory.GetDirectories(
                    Path.Combine(Path.GetDirectoryName(dotnetExe2)!, "sdk"))
                    .OrderDescending().First();
                var spec = new SandboxSpecRequest("0.1.0", true, [ws.Root],
                    [Path.GetDirectoryName(dotnetExe2)!, sdkDir], dotnetCaps);
                var (ok, detail, vOut, _) = await RunPipedAsync(idBuild, spec, dotnetExe2,
                    $"build \"{proj}\" --no-restore -o \"{ws.Root}\\out\" -v minimal", ws.Root, 0,
                    waitMs: 120_000);
                bool emitted = File.Exists(Path.Combine(ws.Root, "out", "mini.dll"));
                string prof = SweepProfile(idBuild);
                bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
                cleanups.Add(("build", new(true, true, prof, wsOk)));
                bool pass = ok && emitted;
                return pass
                    ? (true, $"{detail} emitted mini.dll profile={prof}", (string?)null)
                    : (false, $"{detail} emitted={emitted} out='{vOut[..Math.Min(200, vOut.Length)]}' profile={prof}", (string?)null);
            });
        }
        // ---- L4-RETRY-PROOF (transient rejection recovers on fresh inputs) ----
        // The engine intermittently returns ERROR_INVALID_DATA for otherwise
        // valid single-grant temp launches (observed under rapid repetition; a
        // fresh identity + fresh workspace recovers). A provider must detect
        // and retry the transient, never fall back to unconfined execution.
        // This leg launches the same shape several times, retrying once with
        // fresh identity+workspace on that error, and requires all to succeed.
        await RunCase(collector, "L4-RETRY-PROOF", async () =>
        {
            const int rounds = 4;
            var log = new List<string>();
            bool stable = true;
            bool sawTransient = false;
            bool profilesClean = true;
            bool workspacesClean = true;
            for (int i = 0; i < rounds; i++)
            {
                bool ok = false;
                for (int attempt = 1; attempt <= 2 && !ok; attempt++)
                {
                    using var ws = FixtureWorkspace.Create($"gw1b-stab{i}-{attempt}-" + runTag);
                    string id = $"GagambaGW1B{runTag}z{i}{attempt}";
                    var r = await LaunchLeg(id,
                        new SandboxSpecRequest("0.1.0", true, [ws.Root], []),
                        "/d /c echo stab-ok > stab.txt", ws.Root, 0,
                        cwd => Task.FromResult<(bool, string)>(File.Exists(Path.Combine(cwd, "stab.txt"))
                            ? (true, "stab marker present") : (false, "stab marker absent")));
                    ok = r.Ok;
                    string prof = SweepProfile(id);
                    if (!(prof.StartsWith("deleted(") || prof.StartsWith("deleted-never-materialized(")))
                        profilesClean = false;
                    if (!(ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root)))
                        workspacesClean = false;
                    if (!ok)
                    {
                        bool transient = r.Detail.Contains("err=0xD");
                        if (transient) sawTransient = true;
                        log.Add($"round{i} attempt{attempt}: {r.Detail}");
                        if (!transient) break; // deterministic failure: stop retrying
                    }
                    else
                    {
                        log.Add($"round{i} attempt{attempt}: ok");
                    }
                }
                if (!ok) stable = false;
            }
            cleanups.Add(("stability", new(profilesClean, true,
                profilesClean ? "deleted(all stability identities)" : "deleted-UNCERTAIN", workspacesClean)));
            return stable
                ? (true, $"launch stability {rounds}/{rounds}, transient-INVALID_DATA seen={sawTransient}; fresh-identity retry recovers", (string?)null)
                : (false, string.Join(" | ", log), (string?)null);
        });

        // ---- L1-CLEANUP (aggregate proof) ----
        await RunCase(collector, "L1-CLEANUP", () =>
        {
            var notes = cleanups.Select(c =>
                $"{c.Leg}:reaped={c.Cleanup.ChildReaped}/profile={c.Cleanup.ProfileDisposition}/ws={c.Cleanup.WorkspaceDeleted}");
            bool all = cleanups.Count == 17 && cleanups.All(c =>
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
        uint? expectedExit,
        Func<string, Task<(bool Ok, string Detail)>>? effectCheck,
        bool expectRejection = false,
        int expectedError = 0,
        Func<IntPtr, Task>? onRunning = null,
        IntPtr hStdOut = default,
        IntPtr hStdErr = default,
        IntPtr hStdIn = default,
        string? exePath = null,
        uint waitMs = 60_000)
    {
        await Task.Yield();
        byte[] specBytes;
        try { specBytes = SpecBuilder.BuildVerified(spec); }
        catch (Exception ex) { return new(false, $"spec gate refused: {ex.Message}", true); }

        string exe = exePath ?? Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var commandLine = new StringBuilder(32768);
        commandLine.Append('"').Append(exe).Append("\" ").Append(targetArgs);
        var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>() };
        if (hStdOut != IntPtr.Zero || hStdErr != IntPtr.Zero || hStdIn != IntPtr.Zero)
        {
            si.dwFlags |= 0x100; // STARTF_USESTDHANDLES
            si.hStdOutput = hStdOut;
            si.hStdError = hStdErr;
            si.hStdInput = hStdIn;
        }
        bool apiResult;
        int lastError;
        IntPtr hProcess = IntPtr.Zero, hThread = IntPtr.Zero;
        uint pid = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            apiResult = Native.Experimental_CreateProcessInSandbox(
                exe, commandLine, IntPtr.Zero, IntPtr.Zero, false,
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

        if (onRunning is not null)
        {
            try { await onRunning(hProcess); } catch (Exception ex) { return new(false, verdict + $" hook fault: {ex.Message}", false); }
        }
        uint wait = Native.WaitForSingleObject(hProcess, waitMs);
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
        if (!gotCode || (expectedExit.HasValue && exitCode != expectedExit.Value))
            return new(false, verdict + $" exit={exitCode} want {(expectedExit.HasValue ? expectedExit.Value.ToString() : "any-observed")} job={inJob}", true);
        if (effectCheck is not null)
        {
            var (extraOk, extraDetail) = await effectCheck(cwd);
            if (!extraOk)
                return new(false, verdict + " effect FAILED: " + extraDetail, true);
            verdict += " effect ok: " + extraDetail;
        }
        return new(true, verdict + $" exit={exitCode} job={inJob} ({sw.ElapsedMilliseconds}ms)", true);
    }

    /// <summary>
    /// Launch with anonymous-pipe stdout/stderr capture. The proven transport:
    /// pipes are accepted where file handles were rejected.
    /// </summary>
    private static async Task<(bool Ok, string Detail, string Stdout, string Stderr)> RunPipedAsync(
        string identity,
        SandboxSpecRequest spec,
        string? exePath,
        string targetArgs,
        string cwd,
        uint? expectedExit,
        uint waitMs = 60_000)
    {
        var sa = new Native.SecurityAttributes
        {
            nLength = Marshal.SizeOf<Native.SecurityAttributes>(),
            lpSecurityDescriptor = IntPtr.Zero,
            bInheritHandle = true,
        };
        IntPtr hRead = IntPtr.Zero, hWrite = IntPtr.Zero;
        IntPtr eRead = IntPtr.Zero, eWrite = IntPtr.Zero;
        bool writesClosed = false, readTransferred = false, errTransferred = false;
        try
        {
            if (!Native.CreatePipe(out hRead, out hWrite, ref sa, 65536)
                || !Native.CreatePipe(out eRead, out eWrite, ref sa, 65536))
                return (false, $"CreatePipe failed err={Marshal.GetLastWin32Error()}", string.Empty, string.Empty);
            var r = await LaunchLeg(identity, spec, targetArgs, cwd, expectedExit, null,
                exePath: exePath,
                onRunning: h =>
                {
                    try { Native.CloseHandle(hWrite); } catch { }
                    try { Native.CloseHandle(eWrite); } catch { }
                    writesClosed = true;
                    return Task.CompletedTask;
                },
                hStdOut: hWrite, hStdErr: eWrite,
                waitMs: waitMs);
            string capturedOut = string.Empty, capturedErr = string.Empty;
            // Drain whenever a child ran, even on exit-code mismatch:
            // the failing childs final writes are the diagnosis (skipping
            // the drain on !Ok once hid a 67-byte fatal from evidence).
            // Only API/spec rejections (no target ran) have nothing to read.
            bool childRan = !r.Detail.Contains("no target ran")
                && !r.Detail.Contains("spec gate refused");
            if (childRan)
            {
                using var rs = new FileStream(
                    new Microsoft.Win32.SafeHandles.SafeFileHandle(hRead, ownsHandle: true),
                    FileAccess.Read);
                readTransferred = true;
                using var es = new FileStream(
                    new Microsoft.Win32.SafeHandles.SafeFileHandle(eRead, ownsHandle: true),
                    FileAccess.Read);
                errTransferred = true;
                capturedOut = await ReadBoundedAsync(rs, 1 << 20, TimeSpan.FromSeconds(10));
                capturedErr = await ReadBoundedAsync(es, 1 << 20, TimeSpan.FromSeconds(10));
            }
            return (r.Ok, r.Detail, capturedOut, capturedErr);
        }
        finally
        {
            if (!writesClosed)
            {
                if (hWrite != IntPtr.Zero) { try { Native.CloseHandle(hWrite); } catch { } }
                if (eWrite != IntPtr.Zero) { try { Native.CloseHandle(eWrite); } catch { } }
            }
            if (!readTransferred && hRead != IntPtr.Zero) { try { Native.CloseHandle(hRead); } catch { } }
            if (!errTransferred && eRead != IntPtr.Zero) { try { Native.CloseHandle(eRead); } catch { } }
        }
    }

    private static void CloseBoth(IntPtr hProcess, IntPtr hThread)
    {
        try { if (hProcess != IntPtr.Zero) Native.CloseHandle(hProcess); } catch { }
        try { if (hThread != IntPtr.Zero) Native.CloseHandle(hThread); } catch { }
    }

    /// <summary>
    /// Terminates a process by recorded PID alone (no retained handle) and
    /// confirms death. A PID that is already gone counts as dead.
    /// </summary>
    private static bool KillByPid(uint pid)
    {
        IntPtr h = Native.OpenProcess(
            Native.PROCESS_TERMINATE | Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.SYNCHRONIZE,
            false, pid);
        if (h == IntPtr.Zero)
            return true; // already gone
        try
        {
            if (!Native.TerminateProcess(h, 99))
                return false;
            return Native.WaitForSingleObject(h, 5000) == Native.WAIT_OBJECT_0
                && Native.GetExitCodeProcess(h, out uint rc) && rc != Native.STILL_ACTIVE;
        }
        finally
        {
            try { Native.CloseHandle(h); } catch { }
        }
    }

    /// <summary>
    /// Launch without waiting: returns the live process handle + PID for
    /// supervisor tests. Caller owns the handle and must terminate + close it.
    /// </summary>
    private static (bool Ok, string Detail, IntPtr Handle, uint Pid) LaunchNoWait(
        string identity,
        SandboxSpecRequest spec,
        string targetArgs,
        string cwd,
        byte[]? overrideSpec = null)
    {
        byte[] specBytes;
        try
        {
            specBytes = overrideSpec ?? SpecBuilder.BuildVerified(spec);
            if (overrideSpec is not null)
                SpecBuilder.Verify(specBytes); // independent gate, even for external bytes
        }
        catch (Exception ex) { return (false, $"spec gate refused: {ex.Message}", IntPtr.Zero, 0); }

        string cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var commandLine = new StringBuilder(32768);
        commandLine.Append('"').Append(cmdExe).Append("\" ").Append(targetArgs);
        var si = new StartupInfo { cb = Marshal.SizeOf<StartupInfo>() };
        try
        {
            bool apiResult = Native.Experimental_CreateProcessInSandbox(
                cmdExe, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                0, IntPtr.Zero, cwd, ref si, identity,
                specBytes, (uint)specBytes.Length, out ProcessInformation pi);
            int lastError = Marshal.GetLastWin32Error();
            if (!apiResult)
                return (false, $"api=False err=0x{lastError:X}({MapError(lastError)}) (no target ran)",
                    IntPtr.Zero, 0);
            try { Native.CloseHandle(pi.hThread); } catch { }
            uint pid = Native.GetProcessId(pi.hProcess);
            return (true, $"api=True pid={pid} spec={specBytes.Length}B", pi.hProcess, pid);
        }
        catch (Exception ex)
        {
            return (false, $"invocation fault: {ex.GetType().Name}: {ex.Message}", IntPtr.Zero, 0);
        }
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

    /// <summary>
    /// Returns a drive-root "temp" staging directory (created if needed) for
    /// dependency closures that must live outside %TEMP% (whose foreign ACEs
    /// break multi-grant engine binding), or null when unavailable.
    /// </summary>
    private static string? TryCleanStageRoot()
    {
        try
        {
            string? driveRoot = Path.GetPathRoot(Path.GetTempPath());
            if (string.IsNullOrEmpty(driveRoot))
                return null;
            string stage = Path.Combine(driveRoot, "temp");
            Directory.CreateDirectory(stage);
            return Directory.Exists(stage) ? stage : null;
        }
        catch { return null; }
    }

    private static void CopyDirExcluding(string source, string target, HashSet<string> excludeDirs)    {
        Directory.CreateDirectory(target);
        foreach (string dir in Directory.GetDirectories(source))
        {
            if (excludeDirs.Contains(Path.GetFileName(dir)))
                continue;
            CopyDirExcluding(dir, Path.Combine(target, Path.GetFileName(dir)), excludeDirs);
        }
        foreach (string file in Directory.GetFiles(source))
        {
            string dest = Path.Combine(target, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
            // Clear read-only (e.g. .git objects) so workspace delete succeeds.
            try { File.SetAttributes(dest, FileAttributes.Normal); } catch { }
        }
    }

    private static void WriteBat(string dir, string name, params string[] lines) =>
        File.WriteAllLines(Path.Combine(dir, name), lines, Encoding.ASCII);

    /// <summary>
    /// Records an abandoned leg (control failed before any launch): sweeps the
    /// never-used identity for real values, disposes workspaces, adds the entry
    /// so L1-CLEANUP sees a complete picture, and returns the Failed tuple.
    /// </summary>
    private static (bool Passed, string Reason, string? OutcomeOverride) AbandonLeg(
        List<(string Leg, LegCleanup Cleanup)> cleanups,
        string leg,
        string identity,
        string reason,
        params FixtureWorkspace[] workspaces)
    {
        string prof = SweepProfile(identity);
        bool wsOk = workspaces.All(w => w.DisposeAndReport() == "Confirmed" && !Directory.Exists(w.Root));
        cleanups.Add((leg, new(true, true, prof, wsOk)));
        return (false, reason + $" profile={prof} ws={wsOk}", null);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static async Task<string?> PollFileAsync(string path, TimeSpan budget)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < budget)
        {
            try { return await File.ReadAllTextAsync(path); }
            // NotFound: not written yet. IOException: transient lock (fresh file
            // briefly held by AV/indexer or a concurrent writer) — keep polling.
            catch (FileNotFoundException) { await Task.Delay(150); }
            catch (DirectoryNotFoundException) { await Task.Delay(150); }
            catch (IOException) { await Task.Delay(150); }
        }
        try { return await File.ReadAllTextAsync(path); } catch { return null; }
    }

    private static async Task<string> ReadBoundedAsync(Stream stream, int maxBytes, TimeSpan budget)
    {
        using var cts = new CancellationTokenSource(budget);
        var data = new MemoryStream();
        byte[] buffer = new byte[8192];
        try
        {
            int n;
            while (data.Length < maxBytes && (n = await stream.ReadAsync(buffer, cts.Token)) > 0)
                data.Write(buffer, 0, Math.Min(n, maxBytes - (int)data.Length));
        }
        catch (OperationCanceledException) { /* deadline: return what we have */ }
        return Encoding.ASCII.GetString(data.ToArray());
    }

    private static async Task RunCase(EvidenceCollector collector, string id,
        Func<Task<(bool Passed, string Reason, string? OutcomeOverride)>> fn)
    {
        // --only <substr>... : run a subset of legs (isolation experiments).
        // Non-selected legs are recorded as NotRun; the aggregate then
        // reflects only the selection (documented in the reason).
        if (OnlyFilter is { Length: > 0 } && !id.Contains(OnlyFilter, StringComparison.OrdinalIgnoreCase))
        {
            collector.Add(id, "NotRun", $"skipped by --only {OnlyFilter}", 0);
            Console.WriteLine($"gw1b-launch: [SKIP] {id} (--only {OnlyFilter})");
            return;
        }
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
