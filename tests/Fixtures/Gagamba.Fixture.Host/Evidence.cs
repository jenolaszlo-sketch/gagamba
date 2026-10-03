// Evidence report v1: collector + strict validator.
// Envelope follows docs/fixture-protocol.md. No invented hashes, no fake providers,
// no empty passing reports, summary derived from cases (never worker totals).
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Gagamba.Fixture.Host;

public static class ProbeManifest
{
    public const int Version = 1;

    // Trusted mandatory IDs for GW-1A availability probing. Launch itself is
    // informational: NotRun-when-gated never fails the availability aggregate.
    public static readonly IReadOnlyList<string> MandatoryIds = new[]
    {
        "W1-OS-INVENTORY",
        "W1-MODULE-LOCATION",
        "W1-EXPORT-LOOKUP",
        "W1-EXPORT-TABLE",
        "W1-JOB-MEMBERSHIP",
        "W1-HEADER-SURVEY",
        "W1-SCHEMA-SOURCE",
    };

    public static readonly IReadOnlyList<string> InformationalIds = new[]
    {
        "W1-MINIMAL-LAUNCH",
    };
}

public static class FixtureManifest
{
    public const int Version = 2;

    // Trusted mandatory IDs for GP-1B (GP-1A F1/F2 + protocol + F3 lifecycle).
    // Not worker input. v2 appends F3; GP-1A reports are superseded, not revalidated.
    public static readonly IReadOnlyList<string> MandatoryIds = new[]
    {
        "F1-READ-SENTINEL",
        "F1-WRITE-OUTPUT",
        "F1-EXIT-CODE",
        "F1-SCOPE-ISOLATION",
        "F2-STDOUT-CAPTURE",
        "F2-STDERR-CAPTURE",
        "F2-STDIN-BOUND",
        "F2-HANG-STOP",
        "F2-OUTPUT-LIMIT",
        "P-READY-CONTINUE-SEQUENCE",
        "P-REJECT-UNKNOWN-VERSION",
        "P-REJECT-UNKNOWN-KIND",
        "P-REJECT-BAD-SEQUENCE",
        "P-REJECT-OVERSIZE",
        "P-REJECT-MISMATCH-IDENTITY",
        "EVIDENCE-VALID",
        "F3-CHILD-GRANDCHILD",
        "F3-EARLY-EXIT",
        "F3-BARRIER",
        "F3-ORPHAN-STOP",
    };
}

public static class EvidenceKinds
{
    public const string FixtureSelfTest = "FixtureSelfTest";
    public const string CapabilityProbe = "CapabilityProbe";
    public const string BackendConformance = "BackendConformance";
    public const string Workload = "Workload";
    public const string PackageQualification = "PackageQualification";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        FixtureSelfTest, CapabilityProbe, BackendConformance, Workload, PackageQualification,
    };
}

public static class CaseOutcomes
{
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string Unsupported = "Unsupported";
    public const string NotRun = "NotRun";

    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        Passed, Failed, Unsupported, NotRun,
    };
}

public sealed record ManifestEntry(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("sha256")] string Sha256);

public sealed record SourceInfo(
    [property: JsonPropertyName("commit")] string? Commit,
    [property: JsonPropertyName("dirty")] bool Dirty,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("manifest")] List<ManifestEntry> Manifest);

public sealed record EnvironmentInfo(
    [property: JsonPropertyName("os")] string Os,
    [property: JsonPropertyName("osVersion")] string OsVersion,
    [property: JsonPropertyName("architecture")] string Architecture,
    [property: JsonPropertyName("rid")] string Rid,
    [property: JsonPropertyName("filesystem")] string Filesystem,
    [property: JsonPropertyName("outerEnvironment")] string OuterEnvironment,
    [property: JsonPropertyName("setupPrivilege")] string SetupPrivilege,
    [property: JsonPropertyName("targetPrivilege")] string TargetPrivilege,
    [property: JsonPropertyName("dotnet")] string Dotnet);

public sealed record BackendInfo(
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("helperVersion")] string? HelperVersion);

public sealed record ProfileInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("requestedHash")] string? RequestedHash,
    [property: JsonPropertyName("preparedHash")] string? PreparedHash);

public sealed record CaseRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("controlOutcome")] string? ControlOutcome,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("durationMs")] long DurationMs,
    [property: JsonPropertyName("evidenceRef")] string? EvidenceRef);

public sealed record CleanupInfo(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("diagnostics")] string Diagnostics);

public sealed record ReportSummary(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("passed")] int Passed,
    [property: JsonPropertyName("failed")] int Failed,
    [property: JsonPropertyName("unsupported")] int Unsupported,
    [property: JsonPropertyName("notRun")] int NotRun,
    [property: JsonPropertyName("mandatoryComplete")] bool MandatoryComplete,
    [property: JsonPropertyName("aggregate")] string Aggregate);

public sealed record EvidenceReport(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("evidenceKind")] string EvidenceKind,
    [property: JsonPropertyName("runId")] string RunId,
    [property: JsonPropertyName("startedUtc")] DateTime StartedUtc,
    [property: JsonPropertyName("endedUtc")] DateTime EndedUtc,
    [property: JsonPropertyName("source")] SourceInfo Source,
    [property: JsonPropertyName("environment")] EnvironmentInfo Environment,
    [property: JsonPropertyName("backend")] BackendInfo? Backend,
    [property: JsonPropertyName("profile")] ProfileInfo Profile,
    [property: JsonPropertyName("cases")] List<CaseRecord> Cases,
    [property: JsonPropertyName("cleanup")] CleanupInfo Cleanup,
    [property: JsonPropertyName("summary")] ReportSummary Summary);

public static class EvidenceValidator
{
    public static List<string> Validate(EvidenceReport report)
    {
        var errors = new List<string>();
        if (report.SchemaVersion != 1)
            errors.Add("schemaVersion must be 1");
        if (!EvidenceKinds.All.Contains(report.EvidenceKind))
            errors.Add($"unknown evidenceKind '{report.EvidenceKind}'");
        if (string.IsNullOrWhiteSpace(report.RunId))
            errors.Add("runId missing");
        if (report.StartedUtc.Kind != DateTimeKind.Utc || report.EndedUtc.Kind != DateTimeKind.Utc)
            errors.Add("timestamps must be UTC");
        if (!(report.StartedUtc < report.EndedUtc))
            errors.Add("startedUtc must precede endedUtc");

        // Source: commit or explicit reason, dirty flag, SHA-256 manifest.
        if (report.Source is null) errors.Add("source missing");
        else
        {
            if (report.Source.Commit is null)
            {
                if (string.IsNullOrWhiteSpace(report.Source.Reason))
                    errors.Add("source.commit null requires reason");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(report.Source.Commit, "^[0-9a-f]{40}$"))
                errors.Add("source.commit must be 40-hex");
            if (report.Source.Manifest.Count == 0)
                errors.Add("source.manifest must not be empty");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in report.Source.Manifest)
            {
                if (string.IsNullOrWhiteSpace(m.Path) || !seen.Add(m.Path))
                    errors.Add($"bad/duplicate manifest path '{m.Path}'");
                if (!System.Text.RegularExpressions.Regex.IsMatch(m.Sha256 ?? "", "^[0-9a-f]{64}$"))
                    errors.Add($"bad manifest sha for '{m.Path}'");
            }
        }

        if (report.Environment is null) errors.Add("environment missing");
        else
        {
            foreach (string? f in new[] { report.Environment.Os, report.Environment.OsVersion, report.Environment.Architecture, report.Environment.Rid, report.Environment.Filesystem, report.Environment.OuterEnvironment, report.Environment.SetupPrivilege, report.Environment.TargetPrivilege, report.Environment.Dotnet })
                if (string.IsNullOrWhiteSpace(f))
                    errors.Add("environment field missing (use explicit unavailable explanation)");
        }

        // Backend: self-test and probe runs test no provider, so neither may
        // name one (no fake production provider).
        if (string.Equals(report.EvidenceKind, EvidenceKinds.FixtureSelfTest, StringComparison.Ordinal)
            || string.Equals(report.EvidenceKind, EvidenceKinds.CapabilityProbe, StringComparison.Ordinal))
        {
            if (report.Backend is not null && (report.Backend.Provider is not null || report.Backend.HelperVersion is not null))
                errors.Add($"{report.EvidenceKind} backend must be null");
        }

        if (report.Profile is null || report.Profile.Name != "offline-process-v1")
            errors.Add("profile.name must be offline-process-v1");
        else if ((string.Equals(report.EvidenceKind, EvidenceKinds.FixtureSelfTest, StringComparison.Ordinal)
                  || string.Equals(report.EvidenceKind, EvidenceKinds.CapabilityProbe, StringComparison.Ordinal))
                 && (report.Profile.RequestedHash is not null || report.Profile.PreparedHash is not null))
            errors.Add($"{report.EvidenceKind} must not invent requested/prepared hashes");

        // Mandatory IDs are per evidence kind: fixture runs prove fixtures,
        // probes answer availability. Informational probe legs (e.g. gated
        // launch) may be NotRun without failing the aggregate.
        IReadOnlyList<string> mandatory =
            string.Equals(report.EvidenceKind, EvidenceKinds.CapabilityProbe, StringComparison.Ordinal)
                ? ProbeManifest.MandatoryIds
                : FixtureManifest.MandatoryIds;

        if (report.Cases.Count == 0)
            errors.Add("cases must not be empty");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in report.Cases)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || !ids.Add(c.Id))
                errors.Add($"bad/duplicate case id '{c.Id}'");
            if (!CaseOutcomes.All.Contains(c.Outcome))
                errors.Add($"unknown outcome '{c.Outcome}' for '{c.Id}'");
            if (c.ControlOutcome is not null && !CaseOutcomes.All.Contains(c.ControlOutcome))
                errors.Add($"unknown controlOutcome for '{c.Id}'");
            if (string.IsNullOrWhiteSpace(c.Reason))
                errors.Add($"missing reason for '{c.Id}'");
            if (c.DurationMs < 0)
                errors.Add($"negative duration for '{c.Id}'");
            if (c.EvidenceRef is not null && (c.EvidenceRef.Contains("..") || Path.IsPathRooted(c.EvidenceRef)))
                errors.Add($"evidenceRef must be run-relative for '{c.Id}'");
        }
        foreach (string mandatoryId in mandatory)
            if (!ids.Contains(mandatoryId))
                errors.Add($"missing mandatory case '{mandatoryId}'");

        if (report.Cleanup is null) errors.Add("cleanup missing");
        else if (report.Cleanup.Status is not ("Confirmed" or "Failed" or "Unknown"))
            errors.Add("cleanup.status must be Confirmed/Failed/Unknown");

        // Summary recomputed, never trusted.
        int passed = report.Cases.Count(c => c.Outcome == CaseOutcomes.Passed);
        int failed = report.Cases.Count(c => c.Outcome == CaseOutcomes.Failed);
        int unsupported = report.Cases.Count(c => c.Outcome == CaseOutcomes.Unsupported);
        int notRun = report.Cases.Count(c => c.Outcome == CaseOutcomes.NotRun);
        bool mandatoryComplete = mandatory.All(id =>
            report.Cases.Any(c => c.Id == id && c.Outcome == CaseOutcomes.Passed));
        string aggregate = (failed == 0 && mandatoryComplete && passed > 0
                            && string.Equals(report.Cleanup?.Status, "Confirmed", StringComparison.Ordinal))
            ? CaseOutcomes.Passed : CaseOutcomes.Failed;

        if (report.Summary.Total != report.Cases.Count) errors.Add("summary.total mismatch");
        if (report.Summary.Passed != passed) errors.Add("summary.passed mismatch");
        if (report.Summary.Failed != failed) errors.Add("summary.failed mismatch");
        if (report.Summary.Unsupported != unsupported) errors.Add("summary.unsupported mismatch");
        if (report.Summary.NotRun != notRun) errors.Add("summary.notRun mismatch");
        if (report.Summary.MandatoryComplete != mandatoryComplete) errors.Add("summary.mandatoryComplete mismatch");
        if (!string.Equals(report.Summary.Aggregate, aggregate, StringComparison.Ordinal))
            errors.Add($"summary.aggregate must be {aggregate}");
        if (string.Equals(aggregate, CaseOutcomes.Passed, StringComparison.Ordinal) && passed == 0)
            errors.Add("empty-success reports never pass");

        return errors;
    }

    public static void ThrowIfInvalid(EvidenceReport report)
    {
        var errors = Validate(report);
        if (errors.Count > 0)
            throw new InvalidOperationException("evidence invalid: " + string.Join("; ", errors));
    }
}

public sealed class EvidenceCollector
{
    private readonly string _runId;
    private readonly string _evidenceKind;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private readonly List<CaseRecord> _cases = new();
    private readonly string _repoRoot;
    private readonly List<string> _manifestExtraDirs;
    private readonly List<string> _manifestExtraFiles;

    public EvidenceCollector(
        string repoRoot,
        string evidenceKind = EvidenceKinds.FixtureSelfTest,
        IEnumerable<string>? manifestExtraDirs = null,
        IEnumerable<string>? manifestExtraFiles = null)
    {
        _repoRoot = repoRoot;
        _evidenceKind = evidenceKind;
        _manifestExtraDirs = manifestExtraDirs is null ? [] : new List<string>(manifestExtraDirs);
        _manifestExtraFiles = manifestExtraFiles is null ? [] : new List<string>(manifestExtraFiles);
        _runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..24];
    }

    public string RunId => _runId;

    public IReadOnlyList<CaseRecord> Cases => _cases;

    public void Add(string id, string outcome, string reason, long durationMs,
        string? controlOutcome = null, string? evidenceRef = null)
    {
        if (!CaseOutcomes.All.Contains(outcome))
            throw new ArgumentException($"unknown outcome {outcome}", nameof(outcome));
        _cases.Add(new(id, outcome, controlOutcome, reason, durationMs, evidenceRef));
    }

    public EvidenceReport Finish(string cleanupStatus, string disposition, string diagnostics)
    {
        DateTime ended = DateTime.UtcNow;
        var source = SourceCollector.Collect(_repoRoot, _manifestExtraDirs, _manifestExtraFiles);
        var env = EnvironmentCollector.Collect();
        // Backend null for FixtureSelfTest/CapabilityProbe. Profile hashes null (no policy prep yet).
        var profile = new ProfileInfo("offline-process-v1", null, null);
        BackendInfo? backend = null;
        var cleanup = new CleanupInfo(cleanupStatus, Bounded(disposition, 500), Bounded(diagnostics, 1000));

        int passed = _cases.Count(c => c.Outcome == CaseOutcomes.Passed);
        int failed = _cases.Count(c => c.Outcome == CaseOutcomes.Failed);
        int unsupported = _cases.Count(c => c.Outcome == CaseOutcomes.Unsupported);
        int notRun = _cases.Count(c => c.Outcome == CaseOutcomes.NotRun);
        IReadOnlyList<string> mandatory =
            string.Equals(_evidenceKind, EvidenceKinds.CapabilityProbe, StringComparison.Ordinal)
                ? ProbeManifest.MandatoryIds
                : FixtureManifest.MandatoryIds;
        bool mandatoryComplete = mandatory.All(id =>
            _cases.Any(c => c.Id == id && c.Outcome == CaseOutcomes.Passed));
        string aggregate = (failed == 0 && mandatoryComplete && passed > 0 && cleanupStatus == "Confirmed")
            ? CaseOutcomes.Passed : CaseOutcomes.Failed;
        var summary = new ReportSummary(_cases.Count, passed, failed, unsupported, notRun, mandatoryComplete, aggregate);

        var report = new EvidenceReport(1, _evidenceKind, _runId, _startedUtc, ended,
            source, env, backend, profile, new(_cases), cleanup, summary);
        return report;
    }

    private static string Bounded(string s, int max) =>
        s.Length <= max ? s : s[..max];
}

public static class SourceCollector
{
    /// <param name="extraDirs">Repo-relative dirs hashed recursively (*.cs, *.csproj).</param>
    /// <param name="extraFiles">Repo-relative exact files (may contain '*' wildcards for the file name).</param>
    public static SourceInfo Collect(
        string repoRoot,
        IEnumerable<string>? extraDirs = null,
        IEnumerable<string>? extraFiles = null)
    {
        string? commit = TryGit(repoRoot, "rev-parse HEAD")?.Trim();
        if (commit is not null && !System.Text.RegularExpressions.Regex.IsMatch(commit, "^[0-9a-f]{40}$"))
            commit = null;
        string status = TryGit(repoRoot, "status --porcelain") ?? "";
        bool dirty = !string.IsNullOrWhiteSpace(status);

        // Relevant inputs for GP-1A: fixture sources + contract + entrypoint.
        var files = new SortedSet<string>(StringComparer.Ordinal);
        string fixturesRoot = Path.Combine(repoRoot, "tests", "Fixtures");
        if (Directory.Exists(fixturesRoot))
        {
            foreach (string f in Directory.GetFiles(fixturesRoot, "*.cs", SearchOption.AllDirectories))
                files.Add(f);
            foreach (string f in Directory.GetFiles(fixturesRoot, "*.csproj", SearchOption.AllDirectories))
                files.Add(f);
        }
        string engDir = Path.Combine(repoRoot, "eng");
        if (Directory.Exists(engDir))
        {
            foreach (string f in Directory.GetFiles(engDir, "fixture-selftest.*", SearchOption.TopDirectoryOnly))
                files.Add(f);
        }
        string contract = Path.Combine(repoRoot, "docs", "fixture-protocol.md");
        if (File.Exists(contract))
            files.Add(contract);
        foreach (string dir in extraDirs ?? [])
        {
            string full = Path.Combine(repoRoot, dir.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(full))
                continue;
            foreach (string f in Directory.GetFiles(full, "*.cs", SearchOption.AllDirectories))
                files.Add(f);
            foreach (string f in Directory.GetFiles(full, "*.csproj", SearchOption.AllDirectories))
                files.Add(f);
        }
        foreach (string pattern in extraFiles ?? [])
        {
            string normalized = pattern.Replace('/', Path.DirectorySeparatorChar);
            string? dir = Path.GetDirectoryName(Path.Combine(repoRoot, normalized));
            if (dir is null || !Directory.Exists(dir))
                continue;
            foreach (string f in Directory.GetFiles(dir, Path.GetFileName(normalized), SearchOption.TopDirectoryOnly))
                files.Add(f);
        }

        var manifest = new List<ManifestEntry>();
        foreach (string f in files)
        {
            string rel = Path.GetRelativePath(repoRoot, f).Replace(Path.DirectorySeparatorChar, '/');
            if (rel.Contains("bin/") || rel.Contains("obj/")) continue;
            byte[] bytes = File.ReadAllBytes(f);
            manifest.Add(new(rel, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()));
        }

        string? reason = null;
        if (commit is null)
            reason = "git HEAD unavailable in this environment; dirty-tree input hashes recorded instead";
        return new(commit, dirty, reason, manifest);
    }

    private static string? TryGit(string repoRoot, string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", $"-C \"{repoRoot}\" {args}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return null;
            string stdout = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(10000);
            return proc.ExitCode == 0 ? stdout : null;
        }
        catch { return null; }
    }
}

public static class EnvironmentCollector
{
    public static EnvironmentInfo Collect()
    {
        string os = RuntimeInformation.OSDescription.Trim();
        string osVersion = Environment.OSVersion.VersionString;
        string arch = $"{RuntimeInformation.OSArchitecture}/{RuntimeInformation.ProcessArchitecture}";
        string rid = RuntimeInformation.RuntimeIdentifier;
        string filesystem = FilesystemOf(Path.GetTempPath());
        string outer = OuterEnvironment();
        string setupPriv = Privilege();
        string dotnet = $"runtime {Environment.Version}; {RuntimeInformation.FrameworkDescription}";
        return new(os, osVersion, arch, rid, filesystem, outer, setupPriv, setupPriv, dotnet);
    }

    private static string FilesystemOf(string path)
    {
        try
        {
            string root = Path.GetPathRoot(Path.GetFullPath(path)) ?? "?";
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (string.Equals(drive.Name, root, StringComparison.OrdinalIgnoreCase))
                    return $"{drive.DriveFormat} ({root})";
            }
            return $"unknown Format ({root})";
        }
        catch (Exception ex) { return $"unavailable: {ex.GetType().Name}"; }
    }

    private static string OuterEnvironment()
    {
        var parts = new List<string> { "host-direct" };
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WSL_DISTRO_NAME")))
            parts.Add("wsl:" + Environment.GetEnvironmentVariable("WSL_DISTRO_NAME"));
        if (File.Exists("/.dockerenv"))
            parts.Add("docker-container");
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            parts.Add("github-actions");
        return string.Join(",", parts);
    }

    private static string Privilege()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool admin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                return admin ? "elevated:Administrator" : "standard-user";
            }
            // Unix: uid 0 check omitted without native call; record user explicitly.
            return $"uid-unknown:user={Environment.UserName} (unavailable: no geteuid probe in GP-1A)";
        }
        catch (Exception ex) { return $"unavailable: {ex.GetType().Name}"; }
    }
}
