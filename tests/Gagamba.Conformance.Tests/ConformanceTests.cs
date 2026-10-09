// Cross-platform conformance tests: run the shared runner against the real
// provider for this OS and assert the expected matrix. Windows/Linux/macOS
// CI each exercise their provider; Linux without a delegated cgroup reports
// Skipped honestly (never a false pass).
using Gagamba.Conformance;
using Gagamba.Execution;
using Gagamba.Execution.Linux;
using Gagamba.Execution.MacOS;
using Gagamba.Execution.Windows;
using Xunit;

namespace Gagamba.Conformance.Tests;

public sealed class ConformanceFixture : IAsyncLifetime
{
    private static bool Strict => Environment.GetEnvironmentVariable("GAGAMBA_QUALIFICATION") == "1";
    public string Workspace { get; } = Path.Combine(Path.GetTempPath(),
        "gagamba-conformance", Guid.NewGuid().ToString("N"));
    public ConformanceReport? Report { get; private set; }
    public bool Usable { get; private set; }
    public string Platform { get; private set; } = "unsupported";

    public async ValueTask InitializeAsync()
    {
        if (Strict)
        {
            string? sha = Environment.GetEnvironmentVariable("GAGAMBA_SOURCE_SHA")
                ?? Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (sha is null || !System.Text.RegularExpressions.Regex.IsMatch(sha, "^[0-9a-fA-F]{40}$"))
                throw new InvalidOperationException("qualification requires exact source SHA");
        }
        Directory.CreateDirectory(Workspace);
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            if (Strict) throw new InvalidOperationException("qualification host unsupported");
            return;
        }
        Report = await ConformanceRunner.RunAsync(NewProvider, new ConformanceOptions(Workspace));
        Platform = Report.Platform;
        Usable = Report.Outcome("prepare") == ConformanceOutcome.Passed;
        WriteEvidence(Report);
        if (Strict)
        {
            var required = ConformanceMatrix.LegNames.Where(n =>
                n != "setsid-escape" || !OperatingSystem.IsWindows());
            foreach (string name in required)
            {
                var leg = Report.Find(name);
                if (leg is null || leg.Outcome != ConformanceOutcome.Passed)
                    throw new InvalidOperationException(
                        $"mandatory conformance leg {name} was not Passed: {leg?.Outcome} ({leg?.Detail})");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        try { Directory.Delete(Workspace, recursive: true); }
        catch when (!Strict) { }
        return ValueTask.CompletedTask;
    }

    private static IExecutionProvider NewProvider()
    {
        if (OperatingSystem.IsWindows()) return new WindowsExecutionProvider();
        if (OperatingSystem.IsLinux()) return new LinuxExecutionProvider(
            Environment.GetEnvironmentVariable("GAGAMBA_LINUX_CGROUP") ?? "/sys/fs/cgroup");
        if (OperatingSystem.IsMacOS()) return new MacOsExecutionProvider();
        throw new PlatformNotSupportedException();
    }

    private void WriteEvidence(ConformanceReport report)
    {
        try
        {
            string? root = FindRepoRoot();
            if (root is null)
            {
                if (Strict) throw new InvalidOperationException("qualification source root unavailable");
                return;
            }
            string dir = Path.Combine(root, "artifacts");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var required = ConformanceMatrix.LegNames.Where(n =>
                n != "setsid-escape" || !OperatingSystem.IsWindows()).ToArray();
            bool cleanup = new[] { "unit-termination", "root-exit", "dispose-cleanup", "completion" }
                .All(n => report.Find(n)?.Outcome == ConformanceOutcome.Passed);
            string? launchdDomain = null;
            if (OperatingSystem.IsMacOS())
            {
                using var id = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = "/usr/bin/id", Arguments = "-u", RedirectStandardOutput = true,
                  UseShellExecute = false });
                launchdDomain = id is null ? null : "gui/" + id.StandardOutput.ReadToEnd().Trim();
                id?.WaitForExit();
            }
            bool delegatedLinux = false;
            if (OperatingSystem.IsLinux())
            {
                string uid = File.ReadLines("/proc/self/status")
                    .First(line => line.StartsWith("Uid:", StringComparison.Ordinal));
                string[] values = uid.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                delegatedLinux = values.Length > 2 && values[2] != "0"
                    && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GAGAMBA_LINUX_CGROUP"));
            }
            var payload = new
            {
                schema = "gagamba-conformance/v1",
                sourceSha = Environment.GetEnvironmentVariable("GAGAMBA_SOURCE_SHA")
                    ?? Environment.GetEnvironmentVariable("GITHUB_SHA"),
                runId = Path.GetFileName(Workspace),
                host = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                platform = report.Platform,
                usable = Usable,
                cleanup = cleanup ? "Confirmed" : "Unknown",
                skippedMandatory = required.Count(n => report.Find(n)?.Outcome == ConformanceOutcome.Skipped),
                unsupportedMandatory = required.Count(n => report.Find(n) is null),
                cgroupVersion = OperatingSystem.IsLinux() && File.Exists("/sys/fs/cgroup/cgroup.controllers") ? 2 : 0,
                delegated = delegatedLinux,
                launchdDomain,
                legs = report.Legs.Select(l => new { l.Name, outcome = l.Outcome.ToString(), l.Detail }),
            };
            string output = Strict
                ? Environment.GetEnvironmentVariable("GAGAMBA_QUALIFICATION_OUTPUT")
                    ?? throw new InvalidOperationException("qualification output path required")
                : Path.Combine(dir, $"conformance-{report.Platform}-{stamp}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(payload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch when (!Strict) { }
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Gagamba.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}

public sealed class ConformanceTests : IClassFixture<ConformanceFixture>
{
    private static readonly string[] Behavioral =
    {
        "single-use", "working-directory", "no-ambient-inherit",
        "unit-termination", "root-exit", "dispose-cleanup", "completion",
    };

    private readonly ConformanceFixture _f;

    public ConformanceTests(ConformanceFixture f) => _f = f;

    [Fact]
    public void CapabilityMatrixMatchesPlatform()
    {
        if (_f.Report is null) { Assert.Skip("unsupported conformance host"); return; }
        Assert.Equal(ConformanceOutcome.Passed, _f.Report.Outcome("capability-matrix"));
    }

    [Fact]
    public void PublicSurfaceStaysOpaque()
    {
        if (_f.Report is null) { Assert.Skip("unsupported conformance host"); return; }
        Assert.Equal(ConformanceOutcome.Passed, _f.Report.Outcome("opaque-handles"));
    }

    [Fact]
    public void BehavioralLegsMatchPlatformTruth()
    {
        if (_f.Report is null) { Assert.Skip("unsupported conformance host"); return; }
        if (!_f.Usable) { Assert.Skip("native prerequisite unavailable"); return; }
        foreach (string name in Behavioral)
        {
            var leg = _f.Report.Find(name);
            Assert.NotNull(leg);
            Assert.True(leg!.Outcome == ConformanceOutcome.Passed,
                $"{name}: expected Passed, got {leg.Outcome} ({leg.Detail})");
        }
    }

    [Fact]
    public void SetsidEscapeMatchesExpectedSemantics()
    {
        if (_f.Report is null) { Assert.Skip("unsupported conformance host"); return; }
        if (!_f.Usable) { Assert.Skip("native prerequisite unavailable"); return; }
        var leg = _f.Report.Find("setsid-escape");
        Assert.NotNull(leg);
        if (OperatingSystem.IsWindows())
            Assert.Equal(ConformanceOutcome.Skipped, leg!.Outcome);
        else
            Assert.True(leg!.Outcome == ConformanceOutcome.Passed,
                $"setsid-escape: expected Passed, got {leg.Outcome} ({leg.Detail})");
    }

    [Fact]
    public void ReportContainsEveryLeg()
    {
        if (_f.Report is null) { Assert.Skip("unsupported conformance host"); return; }
        foreach (string name in ConformanceMatrix.LegNames)
            Assert.True(_f.Report.Find(name) is not null, $"missing leg: {name}");
    }
}
