using Gagamba.Execution;
using Gagamba.Runtime;
using Xunit;

namespace Gagamba.Runtime.Tests;

public sealed class Ar5OrchestratorTests
{
    private static readonly ExecutionRequirements Empty = new([]);
    private static ProcessStartSpec Spec() => ProcessStartSpec.Vector(
        "secret-executable", ["secret-argument"], Path.GetTempPath(),
        new Dictionary<string, string> { ["SECRET_KEY"] = "secret-value" });

    private sealed class Fake : IExecutionProvider
    {
        private readonly TaskCompletionSource<CompletionResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly PreparedExecution _token =
            new(WellKnownPlatforms.Windows.Platform, Guid.NewGuid(), []);
        private readonly ExecutionHandle _handle =
            new(WellKnownPlatforms.Windows.Platform, Guid.NewGuid());
        public int TerminateCalls { get; private set; }
        public int LaunchCalls { get; private set; }
        public bool RejectPrepare { get; init; }
        public bool FailDiscard { get; init; }
        public bool FailTerminate { get; init; }
        public bool ThrowFirstWait { get; init; }
        private int _waits;
        public PlatformCapabilities Describe() => WellKnownPlatforms.Windows;
        public PrepareResult Prepare(ExecutionRequirements _) => RejectPrepare
            ? new PrepareResult.Rejected(["secret-value"])
            : new PrepareResult.Accepted(_token);
        public DiscardResult Discard(PreparedExecution _) => FailDiscard
            ? new DiscardResult.Failed(["secret-value"])
            : new DiscardResult.Discarded(_token);
        public LaunchResult Launch(PreparedExecution _, ProcessStartSpec __)
        { LaunchCalls++; return new LaunchResult.Started(_handle); }
        public TerminateResult Terminate(ExecutionHandle _)
        {
            TerminateCalls++;
            if (FailTerminate) return new TerminateResult.Failed(["secret-value"]);
            _completion.TrySetResult(new CompletionResult.Terminated());
            return new TerminateResult.Terminated(_handle);
        }
        public ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle _,
            CancellationToken cancellationToken = default)
        {
            if (ThrowFirstWait && Interlocked.Increment(ref _waits) == 1)
                throw new InvalidOperationException("secret-value");
            return new(_completion.Task.WaitAsync(cancellationToken));
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Finish(CompletionResult result) => _completion.TrySetResult(result);
    }

    [Theory]
    [InlineData(0, RunCause.NaturalExit)]
    [InlineData(7, RunCause.NonzeroExit)]
    public async Task NaturalAndNonzeroRemainDistinct(int code, RunCause cause)
    {
        await using var provider = new Fake();
        provider.Finish(new CompletionResult.NaturalExit(code));
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            callerCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(cause, receipt.Cause);
        Assert.Equal(code, receipt.RootExitCode);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.True(receipt.CompletedAt >= receipt.StartedAt);
        Assert.NotEqual(Guid.Empty, receipt.CorrelationId);
        Assert.Equal(64, receipt.InvocationFingerprint.Length);
        Assert.DoesNotContain("secret", receipt.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitStopRequestsTerminationAndObservesTerminal()
    {
        await using var provider = new Fake();
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            explicitStop: stop.Token, callerCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.ExplicitStop, receipt.Cause);
        Assert.Equal(0, provider.LaunchCalls);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
    }

    [Fact]
    public async Task CallerCancellationAfterLaunchTerminatesExecution()
    {
        await using var provider = new Fake();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var task = ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            callerCancellation: caller.Token);
        await Task.Delay(30, TestContext.Current.CancellationToken);
        caller.Cancel();
        var receipt = await task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.CallerCancellation, receipt.Cause);
        Assert.Equal(1, provider.TerminateCalls);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
    }

    [Fact]
    public async Task ExplicitStopAfterLaunchTerminatesExecution()
    {
        await using var provider = new Fake();
        using var stop = new CancellationTokenSource();
        var task = ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            callerCancellation: TestContext.Current.CancellationToken,
            explicitStop: stop.Token);
        await Task.Delay(30, TestContext.Current.CancellationToken);
        stop.Cancel();
        var receipt = await task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.ExplicitStop, receipt.Cause);
        Assert.Equal(1, provider.TerminateCalls);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
    }

    [Fact]
    public async Task ObservationFailureAndProviderReasonsStayUnknownAndRedacted()
    {
        await using var provider = new Fake();
        provider.Finish(new CompletionResult.Failed(["secret-value"]));
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            callerCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.ObservationFailure, receipt.Cause);
        Assert.Equal(CleanupStatus.Unknown, receipt.Cleanup);
        Assert.DoesNotContain("secret", receipt.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CaptureUnsupportedDiscardsBeforeLaunchAndReportsCleanupFailure()
    {
        await using var provider = new Fake { FailDiscard = true };
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            new RunOptions(Capture: new OutputCaptureOptions(1, 1, 2)),
            callerCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.LaunchFailure, receipt.Cause);
        Assert.Equal(CleanupStatus.Failed, receipt.Cleanup);
        Assert.Equal(0, provider.LaunchCalls);
        Assert.DoesNotContain("secret", receipt.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FailedTerminationLeavesCleanupUnknownAndRedactsProviderReason()
    {
        await using var provider = new Fake { FailTerminate = true };
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            new RunOptions(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.Deadline, receipt.Cause);
        Assert.False(receipt.StopRequestAccepted);
        Assert.Equal(CleanupStatus.Unknown, receipt.Cleanup);
        Assert.DoesNotContain("secret", receipt.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SynchronousWaitFaultStillStopsAndObservesCleanup()
    {
        await using var provider = new Fake { ThrowFirstWait = true };
        var receipt = await ExecutionOrchestrator.RunAsync(provider, Empty, Spec(),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.ObservationFailure, receipt.Cause);
        Assert.Equal(1, provider.TerminateCalls);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.DoesNotContain("secret", receipt.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
