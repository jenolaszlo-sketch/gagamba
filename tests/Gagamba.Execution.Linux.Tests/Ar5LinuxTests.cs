using Gagamba.Execution;
using Gagamba.Execution.Linux;
using Gagamba.Runtime;
using Xunit;

namespace Gagamba.Execution.Linux.Tests;

public sealed class Ar5LinuxTests
{
    private const string Parent = "/sys/fs/cgroup/gagamba-ar5-tests";
    private static ProcessStartSpec Shell(string body) => ProcessStartSpec.Vector(
        "/bin/sh", ["-c", body], "/tmp", new Dictionary<string, string>());

    private static bool Usable()
    {
        try
        {
            Directory.CreateDirectory(Parent);
            string probe = Path.Combine(Parent, "probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            bool ok = File.Exists(Path.Combine(probe, "cgroup.kill"));
            Directory.Delete(probe);
            return ok;
        }
        catch { return false; }
    }

    [Fact]
    public async Task NativeCaptureConcurrentOutputAndOverflow()
    {
        if (!OperatingSystem.IsLinux() || !Usable())
        { Assert.Skip("requires delegated Linux cgroup-v2 subtree"); return; }
        await using var provider = new LinuxExecutionProvider(Parent);
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]),
            Shell("i=0; while [ $i -lt 10000 ]; do printf 'stdout-data-1234567890\\n'; printf 'stderr-data-1234567890\\n' >&2; i=$((i+1)); done"),
            new RunOptions(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(1024, 1024, 2048)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.True(receipt.Cause == RunCause.OutputOverflow,
            $"cause={receipt.Cause}; reasons={string.Join(';', receipt.Reasons)}");
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.Equal(OutputCaptureStatus.Overflow, receipt.Output?.Status);
        Assert.Empty(receipt.Output!.StdoutRetained.ToArray());
        Assert.Empty(receipt.Output.StderrRetained.ToArray());
    }

    [Fact]
    public async Task DeadlineStopsDescendantHeldPipes()
    {
        if (!OperatingSystem.IsLinux() || !Usable())
        { Assert.Skip("requires delegated Linux cgroup-v2 subtree"); return; }
        await using var provider = new LinuxExecutionProvider(Parent);
        var receipt = await ExecutionOrchestrator.RunAsync(provider,
            new ExecutionRequirements([]), Shell("/bin/sleep 30 & exit 0"),
            new RunOptions(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(5),
                new OutputCaptureOptions(1024, 1024, 2048)),
            callerCancellation: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        Assert.Equal(RunCause.Deadline, receipt.Cause);
        Assert.True(receipt.StopRequestAccepted);
        Assert.Equal(CleanupStatus.Confirmed, receipt.Cleanup);
        Assert.Equal(OutputCaptureStatus.Complete, receipt.Output?.Status);
    }
}
