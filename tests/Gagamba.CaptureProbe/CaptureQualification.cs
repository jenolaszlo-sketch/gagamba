using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gagamba.Execution;
using Gagamba.Runtime;

namespace Gagamba.CaptureProbe;

internal static class CaptureQualification
{
    private const int MandatoryCaseCount = 14;
    private static readonly TimeSpan BoundedWait = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> RunAsync(string outputPath)
    {
        var report = new QualificationReport
        {
            SourceSha = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            Framework = RuntimeInformation.FrameworkDescription,
            TargetFramework = "net10.0",
            Net8Status = "Not applicable: current Gagamba projects target net10.0 only.",
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            MandatoryCaseCount = MandatoryCaseCount,
            StartedAtUtc = DateTimeOffset.UtcNow,
        };
        int exitCode = 1;
        ExecutionRuntime? runtime = null;
        try
        {
            if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
                throw new PlatformNotSupportedException("Native qualification requires macOS ARM64.");
            report.NativeHostGuardPassed = true;
            if (string.IsNullOrWhiteSpace(report.SourceSha))
                throw new InvalidOperationException("GITHUB_SHA is required to bind qualification to exact source.");

            var native = ExecutionRuntime.Create();
            runtime = native;
            report.Provider = native.Describe().Platform;
            report.ProviderAvailable = native.HasProvider;
            if (!native.HasProvider) throw new InvalidOperationException("No native provider selected.");

            await CaseAsync(report, "normal-streams-exact-bytes", async c =>
            {
                const string stdout = "stdout-normal-α\n";
                const string stderr = "stderr-normal-β\n";
                var run = await DirectAsync(native, c, PythonWrite(stdout, stderr),
                    new OutputCaptureOptions(128, 128, 256, RetainBytes: true));
                Check(run.Terminal is CompletionResult.NaturalExit { RootExitCode: 0 }, "root did not exit 0");
                Check(run.Output.Status == OutputCaptureStatus.Complete, "capture was not Complete");
                Check(Text(run.Output.StdoutRetained) == stdout, "stdout bytes differ");
                Check(Text(run.Output.StderrRetained) == stderr, "stderr bytes differ");
                RunReceipt receipt = await ExecutionOrchestrator.RunAsync(native, Requirements(),
                    c.Python(PythonWrite("combined-out\n", "combined-err\n")),
                    new RunOptions(Deadline: BoundedWait,
                        Capture: new OutputCaptureOptions(128, 128, 256, RetainBytes: true)));
                Check(receipt.Cause == RunCause.NaturalExit && receipt.RootExitCode == 0,
                    "successful combined receipt did not preserve NaturalExit(0)");
                Check(receipt.Cleanup == CleanupStatus.Confirmed, "successful combined cleanup was not confirmed");
                Check(receipt.Output?.Status == OutputCaptureStatus.Complete,
                    "successful combined receipt did not require Complete output");
                return new { run.Terminal, run.Output.Status, run.Output.StdoutBytesSeen,
                    run.Output.StderrBytesSeen, Stdout = Text(run.Output.StdoutRetained),
                    Stderr = Text(run.Output.StderrRetained), CombinedCause = receipt.Cause,
                    CombinedExitCode = receipt.RootExitCode, CombinedCleanup = receipt.Cleanup,
                    CombinedOutputStatus = receipt.Output?.Status, CombinedReasons = receipt.Reasons };
            });

            await CaseAsync(report, "exact-individual-and-combined-limits", async c =>
            {
                const int each = 32;
                var run = await DirectAsync(native, c, PythonWrite("x".PadRight(each, 'x'),
                    "y".PadRight(each, 'y')), new OutputCaptureOptions(each, each, each * 2, true));
                Check(run.Terminal is CompletionResult.NaturalExit { RootExitCode: 0 }, "root did not exit 0");
                Check(run.Output.Status == OutputCaptureStatus.Complete, "exact limits overflowed");
                Check(run.Output.StdoutBytesSeen == each && run.Output.StderrBytesSeen == each,
                    "stream byte counts were not exactly at each limit");
                Check(run.Output.StdoutRetained.Length == each && run.Output.StderrRetained.Length == each,
                    "retained bytes did not equal per-stream limits");
                return new { run.Terminal, run.Output.Status, run.Output.StdoutBytesSeen,
                    run.Output.StderrBytesSeen, StdoutRetained = run.Output.StdoutRetained.Length,
                    StderrRetained = run.Output.StderrRetained.Length };
            });

            await OverflowCaseAsync(report, native, "finite-stdout-overflow", stdout: true, stderr: false);
            await OverflowCaseAsync(report, native, "finite-stderr-overflow", stdout: false, stderr: true);

            await CaseAsync(report, "finite-dual-stream-high-pressure-drain", async c =>
            {
                const int count = 256 * 1024;
                string code = "import sys,threading\n" +
                    "def w(s,b):\n  s.write(b*" + count + "); s.flush()\n" +
                    "a=threading.Thread(target=w,args=(sys.stdout.buffer,b'O'))\n" +
                    "b=threading.Thread(target=w,args=(sys.stderr.buffer,b'E'))\n" +
                    "a.start(); b.start(); a.join(); b.join()\n";
                var run = await DirectAsync(native, c, code,
                    new OutputCaptureOptions(1024, 1024, 2048, true));
                Check(run.Terminal is CompletionResult.NaturalExit { RootExitCode: 0 }, "root did not exit 0");
                Check(run.Output.Status == OutputCaptureStatus.Overflow, "dual pressure was not marked Overflow");
                Check(run.Output.StdoutBytesSeen == count && run.Output.StderrBytesSeen == count,
                    "both streams were not drained through their finite payloads");
                Check(run.Output.StdoutRetained.Length <= 1024 && run.Output.StderrRetained.Length <= 1024,
                    "retained bytes exceeded budgets");
                return new { run.Terminal, run.Output.Status, run.Output.StdoutBytesSeen,
                    run.Output.StderrBytesSeen, StdoutRetained = run.Output.StdoutRetained.Length,
                    StderrRetained = run.Output.StderrRetained.Length };
            });

            await CaseAsync(report, "orchestrator-overflow-stops-with-confirmed-cleanup", async c =>
            {
                const int bytes = 2 * 1024 * 1024;
                string code = "import sys; sys.stdout.buffer.write(b'Z'*" + bytes + "); sys.stdout.flush()";
                RunReceipt receipt = await ExecutionOrchestrator.RunAsync(native, Requirements(),
                    c.Python(code), new RunOptions(Deadline: TimeSpan.FromSeconds(10),
                        StopGrace: TimeSpan.FromSeconds(5), Capture: new OutputCaptureOptions(4096, 4096, 8192, true)));
                Check(receipt.Cause == RunCause.OutputOverflow, $"cause was {receipt.Cause}");
                Check(receipt.Cleanup == CleanupStatus.Confirmed, $"cleanup was {receipt.Cleanup}");
                Check(receipt.Output?.Status == OutputCaptureStatus.Overflow, "receipt did not preserve Overflow");
                Check(receipt.Output?.StdoutBytesSeen is > 4096 and <= bytes,
                    "overflow was not observed within the bounded finite payload");
                return new { receipt.Cause, receipt.RootExitCode, receipt.Cleanup,
                    OutputStatus = receipt.Output?.Status, StdoutBytesSeen = receipt.Output?.StdoutBytesSeen,
                    StderrBytesSeen = receipt.Output?.StderrBytesSeen, receipt.Reasons };
            });

            await OrchestratedExitCaseAsync(report, native);
            await OrchestratedStopCaseAsync(report, native, RunCause.Deadline);
            await OrchestratedStopCaseAsync(report, native, RunCause.CallerCancellation);
            await ExplicitStopWithDescendantAsync(report, native);
            await ObservationCancellationCaseAsync(report, native);
            await InvalidOptionsCaseAsync(report, native);
            await RootExitDescendantCaseAsync(report, native);
            await RepeatedCaptureCaseAsync(report, native);

            exitCode = report.Cases.Count == MandatoryCaseCount
                && report.Cases.All(x => x.Status == "Passed") ? 0 : 1;
        }
        catch (Exception ex)
        {
            report.FatalError = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            if (runtime is not null)
            {
                try
                {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(65));
                    report.ProviderDispose = "Succeeded";
                }
                catch (Exception ex)
                {
                    report.ProviderDispose = $"Failed: {ex.GetType().Name}: {ex.Message}";
                    report.FatalError ??= "Native provider disposal failed.";
                    exitCode = 1;
                }
            }
            report.EndedAtUtc = DateTimeOffset.UtcNow;
            report.PassedCaseCount = report.Cases.Count(x => x.Status == "Passed");
            report.FailedCaseCount = report.Cases.Count(x => x.Status == "Failed");
            report.ExitCode = exitCode;
            try
            {
                outputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllTextAsync(outputPath,
                    JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Could not write qualification report: {ex.GetType().Name}: {ex.Message}");
                exitCode = 1;
            }
        }
        Console.WriteLine(Path.GetFullPath(outputPath));
        return exitCode;
    }

    private static async Task OverflowCaseAsync(QualificationReport report,
        ExecutionRuntime runtime, string name, bool stdout, bool stderr)
    {
        await CaseAsync(report, name, async c =>
        {
            const int eachLimit = 64;
            const int eachWrite = 4096;
            string outText = stdout ? new string('O', eachWrite) : "";
            string errText = stderr ? new string('E', eachWrite) : "";
            var run = await DirectAsync(runtime, c, PythonWrite(outText, errText),
                new OutputCaptureOptions(eachLimit, eachLimit, eachLimit * 2, true));
            Check(run.Terminal is CompletionResult.NaturalExit { RootExitCode: 0 }, "root did not exit 0");
            Check(run.Output.Status == OutputCaptureStatus.Overflow, "status was not Overflow");
            Check(run.Output.StdoutBytesSeen == outText.Length && run.Output.StderrBytesSeen == errText.Length,
                "seen byte counts differ from finite writes");
            Check(run.Output.StdoutRetained.Length <= eachLimit && run.Output.StderrRetained.Length <= eachLimit,
                "retained bytes exceeded budgets");
            return new { run.Terminal, run.Output.Status, run.Output.StdoutBytesSeen,
                run.Output.StderrBytesSeen, StdoutRetained = run.Output.StdoutRetained.Length,
                StderrRetained = run.Output.StderrRetained.Length };
        });
    }

    private static async Task OrchestratedExitCaseAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "nonzero-exit-preserves-capture", async c =>
        {
            const string stdout = "before-exit\n";
            const string stderr = "diagnostic\n";
            RunReceipt receipt = await ExecutionOrchestrator.RunAsync(runtime, Requirements(),
                c.Python(PythonWrite(stdout, stderr, exitCode: 7)),
                new RunOptions(Deadline: BoundedWait, Capture: new OutputCaptureOptions(128, 128, 256, true)));
            Check(receipt.Cause == RunCause.NonzeroExit && receipt.RootExitCode == 7,
                "nonzero exit cause/code was not preserved");
            Check(receipt.Cleanup == CleanupStatus.Confirmed, "cleanup was not confirmed");
            Check(receipt.Output?.Status == OutputCaptureStatus.Complete, "output was not Complete");
            Check(Text(receipt.Output!.StdoutRetained) == stdout && Text(receipt.Output.StderrRetained) == stderr,
                "captured bytes differ");
            return new { receipt.Cause, receipt.RootExitCode, receipt.Cleanup,
                OutputStatus = receipt.Output?.Status, receipt.StopRequestAccepted, receipt.Reasons };
        });
    }

    private static async Task OrchestratedStopCaseAsync(QualificationReport report,
        ExecutionRuntime runtime, RunCause expectedCause)
    {
        string name = expectedCause == RunCause.Deadline ? "deadline-during-live-output" : "caller-cancellation-during-live-output";
        await CaseAsync(report, name, async c =>
        {
            string ready = c.Path("ready");
            string code = "import pathlib,sys,time; pathlib.Path(sys.argv[1]).write_text('ready'); " +
                "sys.stdout.write('ready\\n'); sys.stdout.flush(); until=time.monotonic()+30; " +
                "exec(\"while time.monotonic()<until:\\n sys.stdout.write('.')\\n sys.stdout.flush()\\n time.sleep(.05)\")";
            using var cancel = new CancellationTokenSource();
            Task<RunReceipt> task = ExecutionOrchestrator.RunAsync(runtime, Requirements(),
                c.Python(code, ready),
                new RunOptions(Deadline: expectedCause == RunCause.Deadline ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(15),
                    StopGrace: TimeSpan.FromSeconds(5), Capture: new OutputCaptureOptions(256, 256, 512, true)),
                callerCancellation: expectedCause == RunCause.CallerCancellation ? cancel.Token : default);
            c.TrackCleanup(async () =>
            {
                RunReceipt cleanupReceipt = await task.WaitAsync(BoundedWait);
                if (cleanupReceipt.Cleanup != CleanupStatus.Confirmed
                    || cleanupReceipt.Output?.Status is OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed)
                    throw new InvalidOperationException($"orchestrator cleanup was {cleanupReceipt.Cleanup}/{cleanupReceipt.Output?.Status}");
            });
            await WaitFileAsync(ready);
            if (expectedCause == RunCause.CallerCancellation) cancel.Cancel();
            RunReceipt receipt = await task.WaitAsync(BoundedWait);
            Check(receipt.Cause == expectedCause, $"cause was {receipt.Cause}");
            Check(receipt.Cleanup == CleanupStatus.Confirmed, $"cleanup was {receipt.Cleanup}");
            Check(receipt.Output?.Status == OutputCaptureStatus.Complete, "bounded ready output was not Complete");
            return new { receipt.Cause, receipt.Cleanup, OutputStatus = receipt.Output?.Status,
                receipt.StopRequestAccepted, receipt.Reasons, ReadyMarkerObserved = File.Exists(ready) };
        });
    }

    private static async Task ExplicitStopWithDescendantAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "explicit-stop-live-output-same-pg-descendant", async c =>
        {
            string ready = c.Path("child-ready");
            string pidPath = c.Path("child-pid");
            string challenge = c.Path("challenge");
            string response = c.Path("response");
            string child = "import pathlib,sys,time; r=pathlib.Path(sys.argv[1]); p=pathlib.Path(sys.argv[2]); " +
                "ch=pathlib.Path(sys.argv[3]); rs=pathlib.Path(sys.argv[4]); p.write_text(str(__import__('os').getpid())); r.write_text('ready'); " +
                "n=0; until=time.monotonic()+30\nwhile time.monotonic()<until:\n  if ch.exists(): n+=1; rs.write_text(ch.read_text()+':'+str(n)); ch.unlink()\n  time.sleep(.03)";
            string parent = "import pathlib,subprocess,sys,time; " +
                "subprocess.Popen(['/usr/bin/python3','-c',sys.argv[1],*sys.argv[2:]]); " +
                "sys.stdout.write('live\\n'); sys.stdout.flush(); " +
                "until=time.monotonic()+30; exec(\"while time.monotonic()<until:\\n sys.stdout.write('.')\\n sys.stdout.flush()\\n time.sleep(.05)\")";
            using var stop = new CancellationTokenSource();
            Task<RunReceipt> running = ExecutionOrchestrator.RunAsync(runtime, Requirements(),
                c.Python(parent, child, ready, pidPath, challenge, response),
                new RunOptions(Deadline: TimeSpan.FromSeconds(15), StopGrace: TimeSpan.FromSeconds(5),
                    Capture: new OutputCaptureOptions(1024 * 1024, 1024 * 1024, 2 * 1024 * 1024, true)),
                explicitStop: stop.Token);
            c.TrackCleanup(async () =>
            {
                RunReceipt cleanupReceipt = await running.WaitAsync(BoundedWait);
                if (cleanupReceipt.Cleanup != CleanupStatus.Confirmed
                    || cleanupReceipt.Output?.Status is OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed)
                    throw new InvalidOperationException($"orchestrator cleanup was {cleanupReceipt.Cleanup}/{cleanupReceipt.Output?.Status}");
            });
            await WaitFileAsync(ready);
            await File.WriteAllTextAsync(challenge, "before");
            await WaitForTextAsync(response, "before:");
            int pid = int.Parse(await File.ReadAllTextAsync(pidPath));
            string identity = await ProcessIdentityAsync(pid);
            Check(!identity.Contains("unavailable=", StringComparison.Ordinal), "child start identity unavailable");
            stop.Cancel();
            RunReceipt receipt = await running.WaitAsync(BoundedWait);
            Check(receipt.Cause == RunCause.ExplicitStop, $"cause was {receipt.Cause}");
            Check(receipt.Cleanup == CleanupStatus.Confirmed, $"cleanup was {receipt.Cleanup}");
            Check(receipt.Output?.Status == OutputCaptureStatus.Complete, "stop output was not Complete");
            string before = await File.ReadAllTextAsync(response);
            await File.WriteAllTextAsync(challenge, "after");
            bool answeredAfterStop = await WaitForTextAsync(response, "after:", TimeSpan.FromMilliseconds(750), throwOnTimeout: false);
            string after = await File.ReadAllTextAsync(response);
            bool sameProcessIdentityAlive = await ProcessIdentityAliveAsync(pid, identity);
            Check(before == after && !answeredAfterStop && !after.StartsWith("after:", StringComparison.Ordinal),
                "same-PG child answered a post-stop challenge");
            Check(!sameProcessIdentityAlive, "child process identity remained live after confirmed stop");
            return new { receipt.Cause, receipt.Cleanup, OutputStatus = receipt.Output?.Status,
                receipt.StopRequestAccepted, ChildPid = pid, ChildProcessIdentity = identity,
                ChallengeAnsweredBeforeStop = before.StartsWith("before:"), ChallengeAnsweredAfterStop = answeredAfterStop,
                OriginalChildIdentityAliveAfterStop = sameProcessIdentityAlive,
                LaunchdServiceAbsence = "Not exposed through public provider API" };
        });
    }

    private static async Task ObservationCancellationCaseAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "waiter-cancellation-is-observation-only", async c =>
        {
            string ready = c.Path("ready");
            string challenge = c.Path("challenge");
            string response = c.Path("response");
            string code = "import pathlib,sys,time; r=pathlib.Path(sys.argv[1]); ch=pathlib.Path(sys.argv[2]); " +
                "rs=pathlib.Path(sys.argv[3]); r.write_text('ready'); sys.stdout.write('ready\\n'); sys.stdout.flush(); " +
                "until=time.monotonic()+12; exec(\"while time.monotonic()<until:\\n if ch.exists(): rs.write_text(ch.read_text())\\n time.sleep(.03)\")";
            var token = Accepted(runtime);
            var launch = runtime.LaunchCaptured(token, c.Python(code, ready, challenge, response),
                new OutputCaptureOptions(1024, 1024, 2048, true));
            var started = launch as CaptureLaunchResult.Started
                ?? throw new InvalidOperationException("captured launch did not start");
            Task<CompletionResult> lifecycle = runtime.WaitForCompletionAsync(started.Handle).AsTask();
            c.TrackCleanup(async () =>
            {
                if (!lifecycle.IsCompleted) runtime.Terminate(started.Handle);
                CompletionResult terminal = await lifecycle.WaitAsync(BoundedWait);
                OutputCaptureResult output = await started.Capture.Completion.WaitAsync(BoundedWait);
                if (terminal is CompletionResult.Failed
                    || output.Status is OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed)
                    throw new InvalidOperationException($"native cleanup result was {terminal.GetType().Name}/{output.Status}");
            });
            await WaitFileAsync(ready);
            using var waiter = new CancellationTokenSource();
            Task<CompletionResult> waitTask = runtime.WaitForCompletionAsync(started.Handle, waiter.Token).AsTask();
            waiter.Cancel();
            bool canceled = false;
            try { await waitTask; } catch (OperationCanceledException) { canceled = true; }
            Check(canceled, "waiter cancellation was not observed");
            await File.WriteAllTextAsync(challenge, "still-running");
            await WaitFileAsync(response);
            Check(await File.ReadAllTextAsync(response) == "still-running",
                "execution did not answer after observer cancellation");
            var stop = runtime.Terminate(started.Handle);
            var terminal = await runtime.WaitForCompletionAsync(started.Handle).AsTask().WaitAsync(BoundedWait);
            var output = await started.Capture.Completion.WaitAsync(BoundedWait);
            Check(stop is TerminateResult.Terminated && terminal is CompletionResult.Terminated,
                "explicit cleanup after waiter cancellation was not confirmed");
            Check(output.Status == OutputCaptureStatus.Complete, "capture drain was incomplete");
            return new { WaiterCanceled = canceled, ChallengeAnsweredAfterWaiterCancel = true,
                Stop = stop.GetType().Name, Terminal = terminal.GetType().Name, output.Status,
                output.StdoutBytesSeen, output.StderrBytesSeen };
        });
    }

    private static async Task InvalidOptionsCaseAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "invalid-capture-options-refuse-before-dispatch-and-preserve-preparation", async c =>
        {
            string capturedMarker = c.Path("captured-marker");
            string ordinaryMarker = c.Path("ordinary-marker");
            string nonce = c.Nonce;
            var token = Accepted(runtime);
            var invalid = runtime.LaunchCaptured(token,
                c.Python(PythonMark(), capturedMarker, nonce),
                new OutputCaptureOptions(OutputCaptureOptions.MaxAllowedBytes + 1, 1, 1, true));
            Check(invalid is CaptureLaunchResult.Failed, "over-ceiling options were not refused");
            bool capturedAbsent = !File.Exists(capturedMarker);
            Check(capturedAbsent, "invalid capture request dispatched target");
            var ordinary = runtime.Launch(token, c.Python(PythonMark(), ordinaryMarker, nonce));
            Check(ordinary is LaunchResult.Started, "same preparation was not reusable");
            var started = (LaunchResult.Started)ordinary;
            Task<CompletionResult> ordinaryCompletion = runtime.WaitForCompletionAsync(started.Handle).AsTask();
            c.TrackCleanup(async () =>
            {
                CompletionResult terminal = await ordinaryCompletion.WaitAsync(BoundedWait);
                if (terminal is CompletionResult.Failed) throw new InvalidOperationException("ordinary cleanup observation failed");
            });
            var terminal = await ordinaryCompletion.WaitAsync(BoundedWait);
            bool ordinaryPresent = await WaitFileAsync(ordinaryMarker);
            Check(terminal is CompletionResult.NaturalExit { RootExitCode: 0 } && ordinaryPresent,
                "ordinary positive control failed");
            Check(await File.ReadAllTextAsync(ordinaryMarker) == nonce, "ordinary marker nonce differed");
            Check(!File.Exists(capturedMarker), "refused capture marker appeared after positive control");
            return new { CaptureResult = invalid.GetType().Name,
                CaptureReasons = invalid is CaptureLaunchResult.Failed f ? f.Reasons : Array.Empty<string>(),
                CapturedMarkerAbsent = capturedAbsent, OrdinaryResult = ordinary.GetType().Name,
                Terminal = terminal.GetType().Name, OrdinaryMarkerPresent = ordinaryPresent,
                SamePreparationReused = true };
        });
    }

    private static async Task RootExitDescendantCaseAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "root-exit-same-pg-child-cleanup", async c =>
        {
            string ready = c.Path("child-ready");
            string pidPath = c.Path("child-pid");
            string challenge = c.Path("challenge");
            string response = c.Path("response");
            string release = c.Path("root-release");
            string child = "import pathlib,sys,time,os; r=pathlib.Path(sys.argv[1]); p=pathlib.Path(sys.argv[2]); " +
                "ch=pathlib.Path(sys.argv[3]); rs=pathlib.Path(sys.argv[4]); p.write_text(str(os.getpid())); r.write_text('ready'); " +
                "until=time.monotonic()+6; exec(\"while time.monotonic()<until:\\n if ch.exists(): rs.write_text(ch.read_text()+':answered'); ch.unlink()\\n time.sleep(.03)\")";
            string parent = "import subprocess,sys,time,pathlib; " +
                "subprocess.Popen(['/usr/bin/python3','-c',sys.argv[1],*sys.argv[2:]]); " +
                "release=pathlib.Path(sys.argv[6]); deadline=time.monotonic()+10; " +
                "exec(\"while time.monotonic()<deadline and not release.exists(): time.sleep(.02)\"); " +
                "sys.exit(0)";
            var token = Accepted(runtime);
            var launch = runtime.LaunchCaptured(token, c.Python(parent, child, ready, pidPath, challenge, response, release),
                new OutputCaptureOptions(1024, 1024, 2048, true));
            var started = launch as CaptureLaunchResult.Started
                ?? throw new InvalidOperationException("captured launch did not start");
            Task<CompletionResult> lifecycle = runtime.WaitForCompletionAsync(started.Handle).AsTask();
            c.TrackCleanup(async () =>
            {
                if (!lifecycle.IsCompleted) runtime.Terminate(started.Handle);
                CompletionResult terminal = await lifecycle.WaitAsync(BoundedWait);
                OutputCaptureResult output = await started.Capture.Completion.WaitAsync(BoundedWait);
                if (terminal is CompletionResult.Failed
                    || output.Status is OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed)
                    throw new InvalidOperationException($"native cleanup result was {terminal.GetType().Name}/{output.Status}");
            });
            await WaitFileAsync(ready);
            int pid = int.Parse(await File.ReadAllTextAsync(pidPath));
            await File.WriteAllTextAsync(challenge, "before-root-exit");
            await WaitForTextAsync(response, "before-root-exit:");
            var identity = await ProcessIdentityAsync(pid);
            Check(!identity.Contains("unavailable=", StringComparison.Ordinal), "child start identity unavailable");
            Check(!lifecycle.IsCompleted, "root exited before explicit release");
            var elapsed = Stopwatch.StartNew();
            await File.WriteAllTextAsync(release, c.Nonce);
            var terminal = await lifecycle.WaitAsync(BoundedWait);
            elapsed.Stop();
            var output = await started.Capture.Completion.WaitAsync(BoundedWait);
            string before = await File.ReadAllTextAsync(response);
            await File.WriteAllTextAsync(challenge, "after-root-exit");
            bool answeredAfterRootExit = await WaitForTextAsync(response, "after-root-exit:",
                TimeSpan.FromMilliseconds(750), throwOnTimeout: false);
            string after = await File.ReadAllTextAsync(response);
            bool sameProcessIdentityAlive = await ProcessIdentityAliveAsync(pid, identity);
            Check(terminal is CompletionResult.NaturalExit { RootExitCode: 0 }, "root completion was not natural exit 0");
            Check(output.Status == OutputCaptureStatus.Complete, "post-root-exit capture drain incomplete");
            Check(elapsed.Elapsed < TimeSpan.FromSeconds(4), "completion waited for the child's independent six-second deadline");
            Check(before == after && !answeredAfterRootExit && !after.StartsWith("after-root-exit:", StringComparison.Ordinal),
                "same-PG child answered after root-exit completion");
            Check(!sameProcessIdentityAlive, "same-PG child identity remained live after root-exit completion");
            return new { Terminal = terminal.GetType().Name, RootExitCode = 0, output.Status,
                CompletionElapsedMs = elapsed.ElapsedMilliseconds, ChildSelfDeadlineSeconds = 6,
                ChildPid = pid, ChildProcessIdentity = identity,
                ChallengeAnsweredBeforeRootExit = before.StartsWith("before-root-exit:"),
                ChallengeAnsweredAfterRootExit = answeredAfterRootExit,
                OriginalChildIdentityAliveAfterRootExit = sameProcessIdentityAlive,
                LaunchdServiceAbsence = "Not exposed through public provider API" };
        });
    }

    private static async Task RepeatedCaptureCaseAsync(QualificationReport report,
        ExecutionRuntime runtime)
    {
        await CaseAsync(report, "repeated-normal-capture-and-fd-growth-diagnostic", async c =>
        {
            async Task CycleAsync(int n)
            {
                var result = await DirectAsync(runtime, c,
                    PythonWrite($"cycle-{n}\n", $"err-{n}\n"), new OutputCaptureOptions(128, 128, 256, true));
                Check(result.Terminal is CompletionResult.NaturalExit { RootExitCode: 0 }
                    && result.Output.Status == OutputCaptureStatus.Complete, $"cycle {n} failed");
            }
            for (int i = 0; i < 3; i++) await CycleAsync(i); // warm up JIT and provider paths
            int before = CountFileDescriptors();
            for (int i = 0; i < 100; i++) await CycleAsync(i + 3);
            int after = CountFileDescriptors();
            int? growth = before >= 0 && after >= 0 ? after - before : null;
            Check(growth is not null && growth <= 4, $"file descriptor inventory unavailable or grew by {growth}");
            return new { WarmupCycles = 3, MeasuredCycles = 100, FileDescriptorsBefore = before,
                FileDescriptorsAfter = after, FileDescriptorGrowth = growth,
                InventoryMethod = "/dev/fd entry count; diagnostic only on non-macOS" };
        });
    }

    private static async Task<DirectResult> DirectAsync(ExecutionRuntime runtime,
        CaseContext context, string pythonCode, OutputCaptureOptions options)
    {
        var token = Accepted(runtime);
        var launch = runtime.LaunchCaptured(token, context.Python(pythonCode), options);
        if (launch is not CaptureLaunchResult.Started started)
        {
            var discard = runtime.Discard(token);
            throw new InvalidOperationException($"captured launch failed: {Describe(launch)}; discard={Describe(discard)}");
        }

        Task<CompletionResult> completion = runtime.WaitForCompletionAsync(started.Handle).AsTask();
        context.TrackCleanup(async () =>
        {
            if (!completion.IsCompleted) runtime.Terminate(started.Handle);
            CompletionResult terminal = await completion.WaitAsync(BoundedWait);
            OutputCaptureResult output = await started.Capture.Completion.WaitAsync(BoundedWait);
            if (terminal is CompletionResult.Failed
                || output.Status is OutputCaptureStatus.DrainIncomplete or OutputCaptureStatus.ReadFailed)
                throw new InvalidOperationException($"native cleanup result was {terminal.GetType().Name}/{output.Status}");
        });

        CompletionResult terminal;
        try
        {
            terminal = await completion.WaitAsync(BoundedWait);
        }
        catch (TimeoutException)
        {
            runtime.Terminate(started.Handle);
            await completion.WaitAsync(BoundedWait);
            throw new TimeoutException("direct captured workload exceeded bounded terminal wait");
        }
        OutputCaptureResult output = await started.Capture.Completion.WaitAsync(BoundedWait);
        return new DirectResult(terminal, output);
    }

    private static PreparedExecution Accepted(ExecutionRuntime runtime)
    {
        var result = runtime.Prepare(Requirements());
        if (result is PrepareResult.Accepted accepted) return accepted.Prepared;
        throw new InvalidOperationException($"provider preparation refused: {Describe(result)}");
    }

    private static ExecutionRequirements Requirements() => new(new[]
    {
        ExecutionRequirement.Require(ExecutionCapability.UnitTermination, CapabilityLevel.Partial),
        ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit, CapabilityLevel.Partial),
        ExecutionRequirement.Require(ExecutionCapability.RecursiveMembership, CapabilityLevel.Partial),
    });

    private static string PythonWrite(string stdout, string stderr, int exitCode = 0) =>
        "import sys; sys.stdout.write(" + JsonSerializer.Serialize(stdout) + "); sys.stdout.flush(); " +
        "sys.stderr.write(" + JsonSerializer.Serialize(stderr) + "); sys.stderr.flush(); sys.exit(" + exitCode + ")";

    private static string PythonMark() =>
        "import pathlib,sys; pathlib.Path(sys.argv[1]).write_text(sys.argv[2])";

    private static async Task CaseAsync(QualificationReport report, string name,
        Func<CaseContext, Task<object>> run)
    {
        var item = new CaseResult { Id = name, StartedAtUtc = DateTimeOffset.UtcNow };
        var context = new CaseContext();
        try
        {
            item.Nonce = context.Nonce;
            item.Workspace = context.Workspace;
            item.Observations = await run(context);
            item.Status = "Passed";
        }
        catch (Exception ex)
        {
            item.Status = "Failed";
            item.Error = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            item.EndedAtUtc = DateTimeOffset.UtcNow;
            var cleanupErrors = new List<string>();
            foreach (var cleanup in context.CleanupActions)
            {
                try { await cleanup(); }
                catch (Exception ex) { cleanupErrors.Add($"{ex.GetType().Name}: {ex.Message}"); }
            }
            try
            {
                if (cleanupErrors.Count == 0)
                {
                    Directory.Delete(context.Workspace, recursive: true);
                    item.Cleanup = "Confirmed: tracked executions completed and workspace absent";
                }
                else
                {
                    item.Cleanup = "Failed: " + string.Join("; ", cleanupErrors);
                    item.Status = "Failed";
                }
            }
            catch (Exception ex)
            {
                item.Cleanup = $"Failed: {ex.GetType().Name}: {ex.Message}";
                item.Status = "Failed";
                item.Error ??= "case workspace cleanup failed";
            }
            item.CleanupConfirmed = cleanupErrors.Count == 0 && !Directory.Exists(context.Workspace);
            if (!item.CleanupConfirmed) item.Status = "Failed";
            item.Commands.AddRange(context.Commands);
            report.Cases.Add(item);
        }
    }

    private static async Task<bool> WaitFileAsync(string path, TimeSpan? timeout = null)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < (timeout ?? BoundedWait))
        {
            if (File.Exists(path)) return true;
            await Task.Delay(25);
        }
        throw new TimeoutException($"bounded wait expired for marker {path}");
    }

    private static async Task<bool> WaitForTextAsync(string path, string prefix,
        TimeSpan? timeout = null, bool throwOnTimeout = true)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < (timeout ?? BoundedWait))
        {
            try
            {
                if (File.Exists(path) && (await File.ReadAllTextAsync(path)).StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            catch (IOException) { }
            await Task.Delay(25);
        }
        if (throwOnTimeout) throw new TimeoutException($"bounded challenge wait expired for {path}");
        return false;
    }

    private static async Task<string> ProcessIdentityAsync(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return $"pid={pid}; startTimeUtc={process.StartTime.ToUniversalTime():O}";
        }
        catch (Exception ex) { return $"pid={pid}; unavailable={ex.GetType().Name}"; }
    }

    private static Task<bool> ProcessIdentityAliveAsync(int pid, string expectedIdentity)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            string current = $"pid={pid}; startTimeUtc={process.StartTime.ToUniversalTime():O}";
            return Task.FromResult(!process.HasExited && current == expectedIdentity);
        }
        catch (ArgumentException) { return Task.FromResult(false); }
    }

    private static int CountFileDescriptors()
    {
        try { return Directory.EnumerateFileSystemEntries("/dev/fd").Count(); }
        catch { return -1; }
    }

    private static string Text(ReadOnlyMemory<byte> bytes) => System.Text.Encoding.UTF8.GetString(bytes.Span);
    private static string Describe(object result) => JsonSerializer.Serialize(result, JsonOptions);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record DirectResult(CompletionResult Terminal, OutputCaptureResult Output);

    private sealed class CaseContext
    {
        public string Nonce { get; } = Guid.NewGuid().ToString("N");
        public string Workspace { get; }
        public List<string[]> Commands { get; } = new();
        public List<Func<Task>> CleanupActions { get; } = new();
        public CaseContext() { Workspace = Directory.CreateTempSubdirectory("gagamba-capture-qualification-" + Nonce + "-").FullName; }
        public string Path(string name) => System.IO.Path.Combine(Workspace, name);
        public void TrackCleanup(Func<Task> cleanup) => CleanupActions.Add(cleanup);
        public ProcessStartSpec Python(string code, params string[] args)
        {
            string[] vector = new[] { "-c", code }.Concat(args).ToArray();
            Commands.Add(new[] { "/usr/bin/python3" }.Concat(vector).ToArray());
            return ProcessStartSpec.Vector("/usr/bin/python3", vector, Workspace,
                new Dictionary<string, string> { ["PATH"] = "/usr/bin:/bin" });
        }
    }

    private sealed class QualificationReport
    {
        public string? SourceSha { get; set; }
        public string Framework { get; set; } = "";
        public string TargetFramework { get; set; } = "";
        public string Net8Status { get; set; } = "";
        public string OSDescription { get; set; } = "";
        public string OSArchitecture { get; set; } = "";
        public string ProcessArchitecture { get; set; } = "";
        public bool NativeHostGuardPassed { get; set; }
        public string? Provider { get; set; }
        public bool ProviderAvailable { get; set; }
        public int MandatoryCaseCount { get; set; }
        public int PassedCaseCount { get; set; }
        public int FailedCaseCount { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndedAtUtc { get; set; }
        public List<CaseResult> Cases { get; } = new();
        public string? FatalError { get; set; }
        public string? ProviderDispose { get; set; }
        public int ExitCode { get; set; }
    }

    private sealed class CaseResult
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "NotRun";
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset EndedAtUtc { get; set; }
        public string? Nonce { get; set; }
        public string? Workspace { get; set; }
        public List<string[]> Commands { get; } = new();
        public object? Observations { get; set; }
        public string? Cleanup { get; set; }
        public bool CleanupConfirmed { get; set; }
        public string? Error { get; set; }
    }
}
