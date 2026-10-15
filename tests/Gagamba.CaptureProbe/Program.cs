using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution;
using Gagamba.Runtime;

namespace Gagamba.CaptureProbe;

internal static class Program
{
    private const int WaitSeconds = 30;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> Main(string[] args)
    {
        if (!TryArguments(args, out var mode, out var outputPath, out var argumentError))
        {
            Console.Error.WriteLine(argumentError);
            return 2;
        }
        if (mode == "qualify")
            return await CaptureQualification.RunAsync(outputPath);

        var report = new ProbeReport
        {
            Mode = mode,
            Framework = RuntimeInformation.FrameworkDescription,
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            SourceSha = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            NativeHostRequired = "macOS ARM64 (GitHub macos-15 ARM64 runner)",
            StartedAtUtc = DateTimeOffset.UtcNow,
            TargetFramework = "net10.0",
            Net8Status = "Not applicable: current Gagamba projects target net10.0 only.",
        };

        int exitCode = 1;
        try
        {
            if (mode != "baseline")
                throw new ArgumentException("Supported modes are baseline and qualify.");
            if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                throw new PlatformNotSupportedException("This native probe requires macOS ARM64.");

            report.NativeHostGuardPassed = true;
            exitCode = await RunBaselineAsync(report);
            if (!report.Passed) exitCode = 1;
        }
        catch (Exception ex)
        {
            report.Error = $"{ex.GetType().Name}: {ex.Message}";
            exitCode = 1;
        }
        finally
        {
            report.EndedAtUtc = DateTimeOffset.UtcNow;
            report.ExitCode = exitCode;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllTextAsync(outputPath,
                    JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not write JSON report: {ex.GetType().Name}: {ex.Message}");
                exitCode = 1;
            }
        }

        Console.WriteLine(Path.GetFullPath(outputPath));
        return exitCode;
    }

    private static async Task<int> RunBaselineAsync(ProbeReport report)
    {
        string nonce = Guid.NewGuid().ToString("N");
        string workspace = Path.Combine(Path.GetTempPath(), "gagamba-capture-probe", nonce);
        Directory.CreateDirectory(workspace);
        string captureMarkerPath = Path.Combine(workspace, "captured-target-marker.txt");
        string ordinaryMarkerPath = Path.Combine(workspace, "ordinary-target-marker.txt");
        string captureScript = "printf 'stdout-" + nonce + "\\n'; " +
                               "printf 'stderr-" + nonce + "\\n' >&2; " +
                               "printf '%s\\n' '" + nonce + "' > '" + captureMarkerPath + "'; exit 0";
        string ordinaryScript = "printf 'stdout-" + nonce + "\\n'; " +
                                "printf 'stderr-" + nonce + "\\n' >&2; " +
                                "printf '%s\\n' '" + nonce + "' > '" + ordinaryMarkerPath + "'; exit 0";
        var captureSpec = ProcessStartSpec.Vector("/bin/sh", ["-c", captureScript], workspace,
            new Dictionary<string, string> { ["PATH"] = "/usr/bin:/bin" });
        var ordinarySpec = ProcessStartSpec.Vector("/bin/sh", ["-c", ordinaryScript], workspace,
            new Dictionary<string, string> { ["PATH"] = "/usr/bin:/bin" });
        report.Nonce = nonce;
        report.Workspace = workspace;
        report.CaptureMarkerPath = captureMarkerPath;
        report.OrdinaryMarkerPath = ordinaryMarkerPath;
        report.CaptureCommand = new[] { "/bin/sh", "-c", captureScript };
        report.OrdinaryCommand = new[] { "/bin/sh", "-c", ordinaryScript };
        report.CaptureOptions = new CaptureBudget(1024, 1024, 2048);

        var runtime = ExecutionRuntime.Create();
        report.HasProvider = runtime.HasProvider;
        report.Provider = runtime.Describe().Platform;
        var requirements = new ExecutionRequirements(new[]
        {
            ExecutionRequirement.Require(ExecutionCapability.UnitTermination,
                CapabilityLevel.Partial, reason: "macOS launchd process-group lifecycle"),
            ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit,
                CapabilityLevel.Partial),
            ExecutionRequirement.Require(ExecutionCapability.RecursiveMembership,
                CapabilityLevel.Partial, reason: "macOS launchd process-group membership"),
        });

        try
        {
            var preparation = runtime.Prepare(requirements);
            report.Prepare = Describe(preparation);
            if (preparation is not PrepareResult.Accepted accepted)
                return Fail(report, "Capability preparation was not accepted.");

            PreparedExecution token = accepted.Prepared;
            var capture = runtime.LaunchCaptured(token, captureSpec,
                new OutputCaptureOptions(1024, 1024, 2048, RetainBytes: true));
            report.CaptureLaunch = Describe(capture);
            report.CaptureMarkerPresentAfterCaptureAttempt = File.Exists(captureMarkerPath);
            if (capture is not CaptureLaunchResult.Failed refused)
            {
                if (capture is CaptureLaunchResult.Started started)
                {
                    report.UnexpectedCaptureStart = true;
                    var stop = runtime.Terminate(started.Handle);
                    report.UnexpectedCaptureStop = Describe(stop);
                    using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(WaitSeconds));
                    report.UnexpectedCaptureCompletion = Describe(
                        await runtime.WaitForCompletionAsync(started.Handle, wait.Token));
                }
                return Fail(report, "Baseline expected captured launch to refuse before dispatch.");
            }

            if (report.CaptureMarkerPresentAfterCaptureAttempt == true)
                return Fail(report, "Target marker exists after captured-launch refusal.");

            var ordinary = runtime.Launch(token, ordinarySpec);
            report.OrdinaryLaunch = Describe(ordinary);
            if (ordinary is not LaunchResult.Started launched)
                return Fail(report, "The same preparation was not reusable for ordinary launch.");

            using (var wait = new CancellationTokenSource(TimeSpan.FromSeconds(WaitSeconds)))
            {
                try
                {
                    var terminal = await runtime.WaitForCompletionAsync(launched.Handle, wait.Token);
                    report.OrdinaryCompletion = Describe(terminal);
                    report.NaturalExitCode = terminal is CompletionResult.NaturalExit natural
                        ? natural.RootExitCode : null;
                    report.TerminalStateVerified = terminal is CompletionResult.NaturalExit { RootExitCode: 0 };
                }
                catch (OperationCanceledException)
                {
                    report.OrdinaryCompletion = "TimedOut";
                    report.OrdinaryStop = Describe(runtime.Terminate(launched.Handle));
                }
            }

            report.OrdinaryMarkerPresentAfterCompletion = File.Exists(ordinaryMarkerPath);
            if (report.OrdinaryMarkerPresentAfterCompletion == true)
                report.OrdinaryMarkerContents = await File.ReadAllTextAsync(ordinaryMarkerPath);
            report.CaptureMarkerPresentAfterOrdinaryCompletion = File.Exists(captureMarkerPath);
            report.Passed = !report.UnexpectedCaptureStart
                && report.CaptureMarkerPresentAfterCaptureAttempt == false
                && report.CaptureMarkerPresentAfterOrdinaryCompletion == false
                && report.TerminalStateVerified
                && report.OrdinaryMarkerPresentAfterCompletion == true
                && report.OrdinaryMarkerContents?.Trim() == nonce;
            if (!report.Passed)
                return Fail(report, "One or more baseline observations did not match the expected refusal/reuse control.");
            return 0;
        }
        finally
        {
            try
            {
                await runtime.DisposeAsync();
                report.ProviderDispose = "Succeeded";
            }
            catch (Exception ex)
            {
                report.ProviderDispose = $"Failed: {ex.GetType().Name}: {ex.Message}";
                report.Passed = false;
                report.Error ??= "Provider disposal did not confirm cleanup.";
            }
            report.CaptureMarkerPresentAfterProviderDispose = File.Exists(captureMarkerPath);
            if (report.CaptureMarkerPresentAfterProviderDispose == true)
            {
                report.Passed = false;
                report.Error ??= "Captured target marker appeared by provider disposal.";
            }

            try
            {
                if (Directory.Exists(workspace)) Directory.Delete(workspace, recursive: true);
                report.WorkspaceCleanup = "Confirmed absent";
            }
            catch (Exception ex)
            {
                report.WorkspaceCleanup = $"Failed: {ex.GetType().Name}: {ex.Message}";
                report.Passed = false;
                report.Error ??= "Temporary workspace cleanup failed.";
            }
            report.CaptureMarkerAbsentAfterCleanup = !File.Exists(captureMarkerPath);
            report.OrdinaryMarkerAbsentAfterCleanup = !File.Exists(ordinaryMarkerPath);
            if (report.CaptureMarkerAbsentAfterCleanup != true
                || report.OrdinaryMarkerAbsentAfterCleanup != true)
            {
                report.Passed = false;
                report.Error ??= "One or more target markers remained after workspace cleanup.";
            }
        }
    }

    private static int Fail(ProbeReport report, string message)
    {
        report.Passed = false;
        report.Error ??= message;
        return 1;
    }

    private static string Describe(object result) => JsonSerializer.Serialize(result, JsonOptions);

    private static bool TryArguments(string[] args, out string mode, out string outputPath, out string error)
    {
        mode = "";
        outputPath = "";
        error = "Usage: dotnet run --project tests/Gagamba.CaptureProbe -- --mode <baseline|qualify> --output <json>";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mode" && i + 1 < args.Length) mode = args[++i];
            else if (args[i] == "--output" && i + 1 < args.Length) outputPath = args[++i];
            else return false;
        }
        if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(outputPath)) return false;
        outputPath = Path.GetFullPath(outputPath);
        error = "";
        return true;
    }

    private sealed record CaptureBudget(int StdoutBytes, int StderrBytes, int TotalBytes);

    private sealed class ProbeReport
    {
        public string Mode { get; set; } = "";
        public string Framework { get; set; } = "";
        public string TargetFramework { get; set; } = "";
        public string Net8Status { get; set; } = "";
        public string OSDescription { get; set; } = "";
        public string OSArchitecture { get; set; } = "";
        public string ProcessArchitecture { get; set; } = "";
        public string? SourceSha { get; set; }
        public string NativeHostRequired { get; set; } = "";
        public bool NativeHostGuardPassed { get; set; }
        public bool HasProvider { get; set; }
        public string? Provider { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndedAtUtc { get; set; }
        public string? Nonce { get; set; }
        public string? Workspace { get; set; }
        public string? CaptureMarkerPath { get; set; }
        public string? OrdinaryMarkerPath { get; set; }
        public string[]? CaptureCommand { get; set; }
        public string[]? OrdinaryCommand { get; set; }
        public CaptureBudget? CaptureOptions { get; set; }
        public string? Prepare { get; set; }
        public string? CaptureLaunch { get; set; }
        public bool? CaptureMarkerPresentAfterCaptureAttempt { get; set; }
        public bool UnexpectedCaptureStart { get; set; }
        public string? UnexpectedCaptureStop { get; set; }
        public string? UnexpectedCaptureCompletion { get; set; }
        public string? OrdinaryLaunch { get; set; }
        public string? OrdinaryCompletion { get; set; }
        public string? OrdinaryStop { get; set; }
        public int? NaturalExitCode { get; set; }
        public bool TerminalStateVerified { get; set; }
        public bool? OrdinaryMarkerPresentAfterCompletion { get; set; }
        public string? OrdinaryMarkerContents { get; set; }
        public bool? CaptureMarkerPresentAfterOrdinaryCompletion { get; set; }
        public string? ProviderDispose { get; set; }
        public string? WorkspaceCleanup { get; set; }
        public bool? CaptureMarkerPresentAfterProviderDispose { get; set; }
        public bool? CaptureMarkerAbsentAfterCleanup { get; set; }
        public bool? OrdinaryMarkerAbsentAfterCleanup { get; set; }
        public bool Passed { get; set; }
        public int ExitCode { get; set; }
        public string? Error { get; set; }
    }
}
