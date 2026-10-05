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
    public string Workspace { get; } = Path.Combine(Path.GetTempPath(),
        "gagamba-conformance", Guid.NewGuid().ToString("N"));
    public ConformanceReport? Report { get; private set; }
    public bool Usable { get; private set; }
    public string Platform { get; private set; } = "unsupported";

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(Workspace);
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;
        Report = await ConformanceRunner.RunAsync(NewProvider, new ConformanceOptions(Workspace));
        Platform = Report.Platform;
        Usable = Report.Outcome("prepare") == ConformanceOutcome.Passed;
        WriteEvidence(Report);
    }

    public ValueTask DisposeAsync()
    {
        try { Directory.Delete(Workspace, recursive: true); } catch { }
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
            if (root is null) return;
            string dir = Path.Combine(root, "artifacts");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var payload = new
            {
                platform = report.Platform,
                usable = Usable,
                legs = report.Legs.Select(l => new { l.Name, outcome = l.Outcome.ToString(), l.Detail }),
            };
            File.WriteAllText(
                Path.Combine(dir, $"conformance-{report.Platform}-{stamp}.json"),
                System.Text.Json.JsonSerializer.Serialize(payload,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
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
        "unit-termination", "root-exit", "dispose-cleanup",
    };

    private readonly ConformanceFixture _f;

    public ConformanceTests(ConformanceFixture f) => _f = f;

    [Fact]
    public void CapabilityMatrixMatchesPlatform()
    {
        if (_f.Report is null) return;
        Assert.Equal(ConformanceOutcome.Passed, _f.Report.Outcome("capability-matrix"));
    }

    [Fact]
    public void PublicSurfaceStaysOpaque()
    {
        if (_f.Report is null) return;
        Assert.Equal(ConformanceOutcome.Passed, _f.Report.Outcome("opaque-handles"));
    }

    [Fact]
    public void BehavioralLegsMatchPlatformTruth()
    {
        if (_f.Report is null) return;
        foreach (string name in Behavioral)
        {
            var leg = _f.Report.Find(name);
            Assert.NotNull(leg);
            if (_f.Usable)
                Assert.True(leg!.Outcome == ConformanceOutcome.Passed,
                    $"{name}: expected Passed, got {leg.Outcome} ({leg.Detail})");
            else
                Assert.True(leg!.Outcome == ConformanceOutcome.Skipped,
                    $"{name}: expected Skipped on unusable host, got {leg.Outcome} ({leg.Detail})");
        }
    }

    [Fact]
    public void SetsidEscapeMatchesExpectedSemantics()
    {
        if (_f.Report is null) return;
        var leg = _f.Report.Find("setsid-escape");
        Assert.NotNull(leg);
        if (OperatingSystem.IsWindows())
            Assert.Equal(ConformanceOutcome.Skipped, leg!.Outcome);
        else if (_f.Usable)
            Assert.True(leg!.Outcome == ConformanceOutcome.Passed,
                $"setsid-escape: expected Passed, got {leg.Outcome} ({leg.Detail})");
        else
            Assert.Equal(ConformanceOutcome.Skipped, leg!.Outcome);
    }

    [Fact]
    public void ReportContainsEveryLeg()
    {
        if (_f.Report is null) return;
        foreach (string name in ConformanceMatrix.LegNames)
            Assert.True(_f.Report.Find(name) is not null, $"missing leg: {name}");
    }
}
