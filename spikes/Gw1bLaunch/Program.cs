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
        // dotnet launches but exits 1 silently. Map the closure from below:
        // whoami (system binary, no grants) tells whether the base image
        // covers System32; dotnet variants then climb the grant ladder.
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
            using var ws = FixtureWorkspace.Create("gw1b-dotnet-" + runTag);
            Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", ws.Root);
            Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
            Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
            Environment.SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
            string dotnetDir = Path.GetDirectoryName(dotnetExe)!;
            string system32 = Environment.SystemDirectory;
            string whoamiExe = Path.Combine(system32, "whoami.exe");
            string idDotnet = $"GagambaGW1B{runTag}n";
            var attempts = new List<string>();
            bool sdkListed = false;
            // W1 maps the base image (whoami needs no grants but its image).
            // W2-W4 then place dotnet against that map. whoami output is
            // shape-checked only, never recorded (PII). Fresh identity per
            // variant: reuse after failure risks 0xB7 contamination.
            // V1: dotnet + full grants. V2: cmd + full grants (same shape).
            // V3: cmd + dotnetDir only. V4: cmd + system32 only.
            // V5: dotnet + narrowest passing ro set (when identified).
            async Task<(bool Ran, string Detail)> TryVariant(string suffix, SandboxSpecRequest vSpec,
                string exe, string vArgs, Func<string, bool> accept)
            {
                string vid = idDotnet + suffix;
                var (ok, detail, vOut, vErr) = await RunPipedAsync(vid, vSpec, exe, vArgs, ws.Root, 0);
                string prof = SweepProfile(vid);
                bool ran = ok && accept(vOut);
                string shape = exe == whoamiExe
                    ? (vOut.Contains('\\') && vOut.Trim().Length < 160 ? "shape-ok" : $"shape-bad(len={vOut.Length})")
                    : $"out={vOut.Length}B";
                return (ran, $"{detail} {shape} errlen={vErr.Length} profile={prof}");
            }
            var w1 = await TryVariant("a",
                new("0.1.0", true, [ws.Root], []), whoamiExe, string.Empty,
                o => o.Contains('\\'));
            attempts.Add($"W1 whoami-ws-only: ran={w1.Ran} {w1.Detail}");
            var w2 = await TryVariant("b",
                new("0.1.0", true, [ws.Root], []), dotnetExe, "--info",
                o => o.Contains("SDK"));
            attempts.Add($"W2 dotnet-ws-only: ran={w2.Ran} {w2.Detail}");
            if (w2.Ran) { sdkListed = true; }
            if (!sdkListed)
            {
                var w3 = await TryVariant("c",
                    new("0.1.0", true, [ws.Root], [dotnetDir]), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W3 dotnet-ro-dotnetdir: ran={w3.Ran} {w3.Detail}");
                if (w3.Ran) { sdkListed = true; }
            }
            if (!sdkListed)
            {
                var w4 = await TryVariant("d",
                    new("0.1.0", true, [ws.Root], [dotnetDir, system32]), dotnetExe, "--info",
                    o => o.Contains("SDK"));
                attempts.Add($"W4 dotnet-full-closure: ran={w4.Ran} {w4.Detail}");
                if (w4.Ran) { sdkListed = true; }
            }
            string prof = string.Join("+", new[] { "a", "b", "c", "d" }
                .Select(s => SweepProfile(idDotnet + s)));
            bool wsOk = ws.DisposeAndReport() == "Confirmed" && !Directory.Exists(ws.Root);
            bool profOk = prof.Split('+').All(p =>
                p.StartsWith("deleted(") || p.StartsWith("deleted-never-materialized("));
            cleanups.Add(("dotnet", new(profOk, true, prof, wsOk)));
            return sdkListed
                ? (true, $"dotnet --info ran, SDK listed; {string.Join(" | ", attempts)} profile={prof}", (string?)null)
                : (false, $"{string.Join(" | ", attempts)} profile={prof}", (string?)null);
        });

        // ---- L1-CLEANUP (aggregate proof) ----
        await RunCase(collector, "L1-CLEANUP", () =>
        {
            var notes = cleanups.Select(c =>
                $"{c.Leg}:reaped={c.Cleanup.ChildReaped}/profile={c.Cleanup.ProfileDisposition}/ws={c.Cleanup.WorkspaceDeleted}");
            bool all = cleanups.Count == 12 && cleanups.All(c =>
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
        string? exePath = null)
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
        uint? expectedExit)
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
                hStdOut: hWrite, hStdErr: eWrite);
            string capturedOut = string.Empty, capturedErr = string.Empty;
            if (r.Ok)
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
    /// Launch without waiting: returns the live process handle + PID for
    /// supervisor tests. Caller owns the handle and must terminate + close it.
    /// </summary>
    private static (bool Ok, string Detail, IntPtr Handle, uint Pid) LaunchNoWait(
        string identity,
        SandboxSpecRequest spec,
        string targetArgs,
        string cwd)
    {
        byte[] specBytes;
        try { specBytes = SpecBuilder.BuildVerified(spec); }
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
