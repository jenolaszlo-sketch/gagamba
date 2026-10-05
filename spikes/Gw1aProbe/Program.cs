// GW-1A Windows availability probe. Private spike, read-only by construction:
// inventory, module/export resolution, header/schema survey, launch gating.
// This binary contains NO P/Invoke for the sandboxed-launch API: the target
// API is surveyed and resolved but never invoked. Minimal launch stays NotRun
// until a verified schema is pinned and reviewed (GW-1B).
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text;
using Gagamba.Fixture.Host;

internal static class Gw1aProbe
{
    // Documented entry points (Microsoft Learn, 2026-06-01) plus the query
    // candidate observed in MXC source (resolution only, never invoked here).
    private static readonly string[] TargetExports =
    [
        "Experimental_CreateProcessInSandbox",
        "Experimental_CreateProcessAsUserInSandbox",
        "Experimental_QuerySandboxSupport",
    ];

    internal static async Task<int> Main(string[] args)
    {
        string repoRoot = Arg(args, "--repo-root") ?? FindRepoRoot();
        string outDir = Arg(args, "--out") ?? Path.Combine(repoRoot, "artifacts");
        Directory.CreateDirectory(outDir);

        var collector = new EvidenceCollector(
            repoRoot,
            EvidenceKinds.CapabilityProbe,
            manifestExtraDirs: ["spikes/Gw1aProbe"],
            manifestExtraFiles: ["eng/probe.*", "docs/research-plan.md"]);
        string runId = collector.RunId;
        Console.WriteLine($"gw1a-probe: runId={runId}");

        bool isWindows = OperatingSystem.IsWindows();
        string? modulePath = isWindows
            ? Path.Combine(Environment.SystemDirectory, "processmodel.dll")
            : null;
        var lookup = new Dictionary<string, bool>(StringComparer.Ordinal);
        string verdict = "UNKNOWN";

        await RunCase(collector, "W1-OS-INVENTORY", () =>
        {
            var v = Environment.OSVersion.Version;
            string display = ReadRegistryString(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion")
                ?? ReadRegistryString(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId")
                ?? "unavailable";
            string devMode = ReadRegistryDword(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock",
                "AllowDevelopmentWithoutDevLicense") is { } d ? $"devmode={d}" : "devmode=unavailable";
            string detail = isWindows
                ? $"build={v} display={display} {devMode} arch={RuntimeInformation.OSArchitecture}"
                : $"non-Windows: {RuntimeInformation.OSDescription.Trim()} arch={RuntimeInformation.OSArchitecture}";
            return Task.FromResult<(bool, string, string?/*outcome override*/)>(
                (true, detail, null));
        });

        await RunCase(collector, "W1-MODULE-LOCATION", () =>
        {
            if (!isWindows || modulePath is null)
                return Task.FromResult((true, "Unsupported: Windows-only module check on non-Windows host", (string?)"Unsupported"));
            if (!File.Exists(modulePath))
                return Task.FromResult((false, $"ABSENT: {modulePath} not present: BaseContainer tier unavailable on this build", (string?)null));
            var info = FileVersionInfo.GetVersionInfo(modulePath);
            long len = new FileInfo(modulePath).Length;
            return Task.FromResult((true,
                $"present: {modulePath} file={info.FileVersion} product={info.ProductVersion} bytes={len}", (string?)null));
        });

        await RunCase(collector, "W1-EXPORT-LOOKUP", () =>
        {
            if (!isWindows || modulePath is null || !File.Exists(modulePath))
                return Task.FromResult((true, "Unsupported: module absent or non-Windows host", (string?)"Unsupported"));
            IntPtr handle = IntPtr.Zero;
            try
            {
                // Full System32 path: at least as strict as the documented
                // LOAD_LIBRARY_SEARCH_SYSTEM32 pattern. Read-only mapping.
                handle = NativeLibrary.Load(modulePath);
                var parts = new List<string>();
                foreach (string name in TargetExports)
                {
                    bool ok = NativeLibrary.TryGetExport(handle, name, out _);
                    lookup[name] = ok;
                    parts.Add($"{name}={(ok ? "resolved" : "absent")}");
                }
                return Task.FromResult((true, "dynamic-load resolution: " + string.Join("; ", parts), (string?)null));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"load/resolve failed: {ex.GetType().Name}: {ex.Message}", (string?)null));
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    try { NativeLibrary.Free(handle); } catch { }
                }
            }
        });

        await RunCase(collector, "W1-EXPORT-TABLE", () =>
        {
            if (!isWindows || modulePath is null || !File.Exists(modulePath))
                return Task.FromResult((true, "Unsupported: module absent or non-Windows host", (string?)"Unsupported"));
            try
            {
                var (machine, total, sandbox, query) = PeExports.Scan(modulePath);
                var agree = new List<string>();
                foreach (var kv in lookup)
                {
                    bool inTable = sandbox.Concat(query).Contains(kv.Key, StringComparer.Ordinal);
                    agree.Add($"{kv.Key}:GetExport={kv.Value}/table={inTable}{(kv.Value == inTable ? "" : " MISMATCH")}");
                }
                string cross = agree.Count > 0 ? "; cross-check: " + string.Join(", ", agree) : "; lookup not run yet";
                if (agree.Any(a => a.Contains("MISMATCH", StringComparison.Ordinal)))
                    return Task.FromResult((false, $"machine={machine} exports={total}{cross}", (string?)null));
                string sb = sandbox.Count == 0 ? "none" : string.Join(",", sandbox.Take(20));
                string q = query.Count == 0 ? "none" : string.Join(",", query.Take(20));
                return Task.FromResult((true,
                    $"machine={machine} exports={total} sandbox=[{sb}] query/support=[{q}]{cross}", (string?)null));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"PE scan failed: {ex.GetType().Name}: {ex.Message}", (string?)null));
            }
        });

        await RunCase(collector, "W1-JOB-MEMBERSHIP", () =>
        {
            if (!isWindows)
                return Task.FromResult((true, "Unsupported: Job Objects are Windows-only", (string?)"Unsupported"));
            try
            {
                bool inJob = JobProbe.IsInJob();
                return Task.FromResult((true,
                    inJob
                        ? "in-job=TRUE: nested-job rules apply to any engine-created job (see docs ui_restrictions note)"
                        : "in-job=FALSE: no outer job constrains engine job creation", (string?)null));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"job query failed: {ex.GetType().Name}: {ex.Message}", (string?)null));
            }
        });

        await RunCase(collector, "W1-HEADER-SURVEY", () =>
        {
            // Microsoft Learn Requirements table: "Header: Not publicly available
            // (use GetProcAddress)". Verify locally: no *sandbox* header in the
            // installed Windows SDK, if any.
            string sdk = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Windows Kits", "10", "Include");
            string local;
            if (isWindows && Directory.Exists(sdk))
            {
                var hits = new List<string>();
                foreach (string ver in Directory.GetDirectories(sdk))
                {
                    string um = Path.Combine(ver, "um");
                    if (!Directory.Exists(um))
                        continue;
                    hits.AddRange(Directory.GetFiles(um, "*sandbox*.h")
                        .Select(f => Path.GetRelativePath(sdk, f)));
                    if (hits.Count >= 10)
                        break;
                }
                local = hits.Count == 0
                    ? $"SDK present at {sdk} but no *sandbox*.h under um/ (matches documented no-public-header)"
                    : $"unexpected sandbox headers: {string.Join(",", hits.Take(5))}";
            }
            else if (!isWindows)
            {
                local = "no Windows SDK expected on non-Windows host";
            }
            else
            {
                local = $"no Windows SDK Include dir at {sdk}; survey via documentation only";
            }
            return Task.FromResult((true,
                $"documented: no public header, use GetProcAddress (Learn 2026-06-01). local: {local}", (string?)null));
        });

        await RunCase(collector, "W1-SCHEMA-SOURCE", () =>
        {
            // Look for a pinned schema file first; none is expected in GW-1A.
            var pinned = new List<string>();
            foreach (string dir in new[] { Path.Combine(repoRoot, "spikes"), Path.Combine(repoRoot, "eng") })
            {
                if (!Directory.Exists(dir))
                    continue;
                pinned.AddRange(Directory.GetFiles(dir, "*.fbs", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/')));
            }
            if (pinned.Count > 0)
                return Task.FromResult((true,
                    $"PINNED+CANDIDATE (review required before use): {string.Join(",", pinned)}", (string?)null));
            return Task.FromResult((true,
                "UNPINNED: no *.fbs in spikes/ or eng/. surveyed sources: " +
                "(1) Learn createprocessinsandbox 2026-06-01: SBOX file id, version must be 0.1.0, " +
                "SandboxSpec.fbs fields version/app_container/integrity/disallow_win32k/ui_restrictions/" +
                "capabilities/fs_read_write/fs_read_only/network_policy.proxy; " +
                "(2) microsoft/mxc main external/windows-sdk/BaseContainerSpecification.fbs (MIT) " +
                "+ ProcessSecurityEnvironment.fbs PSEC successor on 25H2+; " +
                "pin requires commit pin + license check + flatc --conform + review (GW-1B)", (string?)null));
        });

        collector.Add("W1-MINIMAL-LAUNCH", "NotRun",
            "gated: launch requires resolved export + pinned+reviewed spec; " +
            "this probe contains no launch P/Invoke by construction, so the API was never invoked",
            0);

        bool exportResolved = lookup.TryGetValue("Experimental_CreateProcessInSandbox", out bool r) && r;
        bool schemaPinned = false; // GW-1A pins no schema; see W1-SCHEMA-SOURCE.
        verdict = !isWindows ? "NON-WINDOWS-HOST"
            : modulePath is null || !File.Exists(modulePath) ? "MODULE-ABSENT"
            : !exportResolved ? "EXPORT-ABSENT"
            : schemaPinned ? "AVAILABLE"
            : "EXPORT-PRESENT-SCHEMA-UNPINNED";

        var report = collector.Finish("Confirmed",
            "read-only probe: mapped one system DLL, created no processes and no test-owned files besides this report",
            $"verdict={verdict}");
        var errors = EvidenceValidator.Validate(report);

        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        string outPath = Path.Combine(outDir, $"windows-probe-{report.RunId}.json");
        await File.WriteAllTextAsync(outPath, json, Encoding.UTF8);

        Console.WriteLine($"gw1a-probe: verdict={verdict}");
        Console.WriteLine($"gw1a-probe: cases={report.Summary.Total} passed={report.Summary.Passed} " +
                          $"unsupported={report.Summary.Unsupported} notRun={report.Summary.NotRun} " +
                          $"mandatoryComplete={report.Summary.MandatoryComplete} aggregate={report.Summary.Aggregate}");
        Console.WriteLine($"gw1a-probe: report={outPath} validatorErrors={errors.Count}");
        foreach (string e in errors)
            Console.Error.WriteLine($"gw1a-probe: invalid: {e}");
        return string.Equals(report.Summary.Aggregate, "Passed", StringComparison.Ordinal) && errors.Count == 0 ? 0 : 1;
    }

    private delegate Task<(bool Passed, string Reason, string? OutcomeOverride)> CaseFn();

    private static async Task RunCase(EvidenceCollector collector, string id, CaseFn fn)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (passed, reason, outcomeOverride) = await fn();
            sw.Stop();
            string outcome = outcomeOverride ?? (passed ? "Passed" : "Failed");
            collector.Add(id, outcome, reason, sw.ElapsedMilliseconds);
            Console.WriteLine($"gw1a-probe: [{outcome.ToUpperInvariant()}] {id} ({sw.ElapsedMilliseconds}ms) {reason}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            collector.Add(id, "Failed", $"exception {ex.GetType().Name}: {ex.Message}", sw.ElapsedMilliseconds);
            Console.WriteLine($"gw1a-probe: [FAILED] {id} exception {ex.GetType().Name}: {ex.Message}");
        }
        await Task.CompletedTask;
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

    private static string? ReadRegistryString(string key, string value)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return null;
            return Microsoft.Win32.Registry.GetValue(key, value, null) as string;
        }
        catch { return null; }
    }

    private static long? ReadRegistryDword(string key, string value)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return null;
            object? v = Microsoft.Win32.Registry.GetValue(key, value, null);
            return v switch
            {
                int i => i,
                long l => l,
                _ => null,
            };
        }
        catch { return null; }
    }
}

internal static class JobProbe
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(IntPtr hProcess, IntPtr hJob, out bool result);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    public static bool IsInJob()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Job Objects are Windows-only");
        if (!IsProcessInJob(GetCurrentProcess(), IntPtr.Zero, out bool result))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return result;
    }
}

internal static class PeExports
{
    public static (string Machine, int Total, List<string> Sandbox, List<string> Query) Scan(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int pe = BitConverter.ToInt32(bytes, 0x3C);
        if (BitConverter.ToUInt16(bytes, pe) != 0x4550) // "PE\0\0"
            throw new InvalidOperationException("missing PE signature");
        ushort machine = BitConverter.ToUInt16(bytes, pe + 4);
        ushort sections = BitConverter.ToUInt16(bytes, pe + 6);
        ushort optSize = BitConverter.ToUInt16(bytes, pe + 20);
        ushort magic = BitConverter.ToUInt16(bytes, pe + 24);
        // DataDirectory array starts at +96 (PE32) / +112 (PE32+) *within* the
        // optional header, which itself starts at pe+24.
        int exportDirOffset = magic == 0x10b ? pe + 24 + 96 : magic == 0x20b ? pe + 24 + 112
            : throw new InvalidOperationException($"unknown optional magic 0x{magic:X}");
        int exportRva = BitConverter.ToInt32(bytes, exportDirOffset);
        if (exportRva == 0)
            return (MachineName(machine), 0, [], []);
        int exportOff = RvaToOffset(bytes, pe, optSize, sections, exportRva);
        int numNames = BitConverter.ToInt32(bytes, exportOff + 24);
        int namesRva = BitConverter.ToInt32(bytes, exportOff + 32);
        int namesOff = RvaToOffset(bytes, pe, optSize, sections, namesRva);
        var sandbox = new List<string>();
        var query = new List<string>();
        for (int i = 0; i < numNames; i++)
        {
            int nameRva = BitConverter.ToInt32(bytes, namesOff + i * 4);
            int nameOff = RvaToOffset(bytes, pe, optSize, sections, nameRva);
            int end = nameOff;
            while (end < bytes.Length && bytes[end] != 0 && end - nameOff < 512)
                end++;
            string name = Encoding.ASCII.GetString(bytes, nameOff, end - nameOff);
            if (name.Contains("sandbox", StringComparison.OrdinalIgnoreCase))
            {
                if (sandbox.Count < 50)
                    sandbox.Add(name);
            }
            else if ((name.Contains("query", StringComparison.OrdinalIgnoreCase)
                      || name.Contains("support", StringComparison.OrdinalIgnoreCase))
                     && (name.Contains("sandbox", StringComparison.OrdinalIgnoreCase)
                         || name.Contains("security", StringComparison.OrdinalIgnoreCase)
                         || name.Contains("process", StringComparison.OrdinalIgnoreCase)))
            {
                if (query.Count < 50)
                    query.Add(name);
            }
        }
        return (MachineName(machine), numNames, sandbox, query);
    }

    private static int RvaToOffset(byte[] bytes, int pe, ushort optSize, ushort sections, int rva)
    {
        int secBase = pe + 24 + optSize;
        for (int i = 0; i < sections; i++)
        {
            int off = secBase + i * 40;
            int vaddr = BitConverter.ToInt32(bytes, off + 12);
            int vsize = BitConverter.ToInt32(bytes, off + 8);
            int rawSize = BitConverter.ToInt32(bytes, off + 16);
            int rawPtr = BitConverter.ToInt32(bytes, off + 20);
            int span = Math.Max(vsize, rawSize);
            if (rva >= vaddr && rva < vaddr + span)
                return rawPtr + (rva - vaddr);
        }
        throw new InvalidOperationException($"RVA 0x{rva:X} not in any section");
    }

    private static string MachineName(ushort m) => m switch
    {
        0x8664 => "x64",
        0xAA64 => "ARM64",
        0x14c => "x86",
        _ => $"unknown(0x{m:X})",
    };
}
