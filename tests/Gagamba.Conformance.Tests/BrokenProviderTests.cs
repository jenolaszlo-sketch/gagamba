using Gagamba.Conformance;
using Gagamba.Execution;
using Xunit;

namespace Gagamba.Conformance.Tests;

public sealed class BrokenProviderTests
{
    private sealed class NeverLaunchesProvider : IExecutionProvider
    {
        private readonly PreparedExecution _prepared = new("broken", Guid.NewGuid(), []);
        private readonly ExecutionHandle _execution = new("broken", Guid.NewGuid());
        public PlatformCapabilities Describe() => OperatingSystem.IsWindows()
            ? WellKnownPlatforms.Windows : OperatingSystem.IsLinux()
            ? WellKnownPlatforms.Linux : WellKnownPlatforms.MacOs;
        public PrepareResult Prepare(ExecutionRequirements _) => new PrepareResult.Accepted(_prepared);
        public DiscardResult Discard(PreparedExecution _) => new DiscardResult.Discarded(_prepared);
        public LaunchResult Launch(PreparedExecution _, ProcessStartSpec __) =>
            new LaunchResult.Started(_execution); // Lies: no target runs.
        public TerminateResult Terminate(ExecutionHandle _) =>
            new TerminateResult.Terminated(_execution); // Lies: no cleanup proof.
        public ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle _,
            CancellationToken cancellationToken = default) =>
            new(new CompletionResult.NaturalExit(17)); // Lies: no root exited.
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task FakeLaunchAndCompletionCannotPassBehavioralLegs()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        { Assert.Skip("known conformance platform required"); return; }
        string ws = Path.Combine(Path.GetTempPath(), "gagamba-broken-" + Guid.NewGuid().ToString("N"));
        try
        {
            var report = await ConformanceRunner.RunAsync(() => new NeverLaunchesProvider(),
                new ConformanceOptions(ws, PollMilliseconds: 200));
            Assert.Equal(ConformanceOutcome.Failed, report.Outcome("single-use"));
            Assert.Equal(ConformanceOutcome.Failed, report.Outcome("unit-termination"));
            Assert.Equal(ConformanceOutcome.Failed, report.Outcome("completion"));
        }
        finally { if (Directory.Exists(ws)) Directory.Delete(ws, recursive: true); }
    }
}
