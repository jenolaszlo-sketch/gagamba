using Gagamba.Execution;
using Gagamba.Execution.Windows;
using Gagamba.Runtime;
using Xunit;

namespace Gagamba.Execution.Windows.Tests;

public sealed class Ar5WindowsTests
{
    private static ProcessStartSpec Cmd(string command) => new(
        Path.Combine(Environment.SystemDirectory, "cmd.exe"),
        "/d /c " + command, Path.GetTempPath(),
        new Dictionary<string, string> { ["SYSTEMROOT"] =
            Environment.GetFolderPath(Environment.SpecialFolder.Windows) });

    [Fact]
    public async Task NativeCaptureDrainsBothStreamsAndRedactsByDefault()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var provider = new WindowsExecutionProvider();
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]), Cmd("echo stdout & echo stderr 1>&2"),
            new RunOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(1024, 1024, 2048)),
            callerCancellation: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.True(receipt.Cause == RunCause.NaturalExit,
            $"cause={receipt.Cause}; reasons={string.Join(';', receipt.Reasons)}");
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.Equal(OutputCaptureStatus.Complete, receipt.Output?.Status);
        Assert.True(receipt.Output?.StdoutBytesSeen > 0);
        Assert.True(receipt.Output?.StderrBytesSeen > 0);
        Assert.Empty(receipt.Output!.StdoutRetained.ToArray());
        Assert.Empty(receipt.Output.StderrRetained.ToArray());
    }

    [Fact]
    public async Task OutputOverflowRequestsTermination()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var provider = new WindowsExecutionProvider();
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]),
            Cmd("for /L %i in (1,1,5000) do @echo very-long-output-record-%i"),
            new RunOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(32, 32, 32)),
            callerCancellation: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.OutputOverflow, receipt.Cause);
        Assert.Equal(OutputCaptureStatus.Overflow, receipt.Output?.Status);
        Assert.True(receipt.Output!.StdoutBytesSeen > 32);
    }

    [Fact]
    public async Task StderrFloodDoesNotBlockStdoutDrain()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var provider = new WindowsExecutionProvider();
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]),
            Cmd("for /L %i in (1,1,5000) do @echo stderr-record-%i 1>&2"),
            new RunOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(32, 32, 64)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.OutputOverflow, receipt.Cause);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.Equal(OutputCaptureStatus.Overflow, receipt.Output?.Status);
        Assert.True(receipt.Output!.StderrBytesSeen > 32);
    }

    [Fact]
    public async Task DeadlineTerminatesInsteadOfOnlyCancellingWait()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var provider = new WindowsExecutionProvider();
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]), Cmd("%SYSTEMROOT%\\System32\\ping.exe -n 10 127.0.0.1 >nul"),
            new RunOptions(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.Deadline, receipt.Cause);
        Assert.True(receipt.StopRequestAccepted);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
    }

    [Fact]
    public async Task CapturedStdinIsReadableEof()
    {
        if (!OperatingSystem.IsWindows()) { Assert.Skip("Windows Job Object"); return; }
        await using var provider = new WindowsExecutionProvider();
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]), Cmd("%SYSTEMROOT%\\System32\\more.com"),
            new RunOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(1024, 1024, 2048, RetainBytes: true)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.NaturalExit, receipt.Cause);
        Assert.Equal(0, receipt.RootExitCode);
        Assert.Equal(OutputCaptureStatus.Complete, receipt.Output?.Status);
        Assert.Equal(0, receipt.Output!.StderrBytesSeen);
    }
}
