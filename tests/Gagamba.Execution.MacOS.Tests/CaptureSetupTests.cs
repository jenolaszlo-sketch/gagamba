using System.Reflection;
using Gagamba.Execution;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class CaptureSetupTests
{
    [Fact]
    public async Task Partial_fifo_setup_failure_refuses_before_dispatch_and_preserves_preparation()
    {
        if (!OperatingSystem.IsMacOS()) { Assert.Skip("requires native macOS FIFO setup"); return; }
        string workspace = Directory.CreateTempSubdirectory("gagamba-capture-setup-").FullName;
        var provider = new MacOsExecutionProvider();
        try
        {
            var token = Assert.IsType<PrepareResult.Accepted>(provider.Prepare(new ExecutionRequirements([]))).Prepared;
            // Test-only introspection locates this exact owned preparation,
            // not a global temp-directory scan or another execution's files.
            var field = typeof(MacOsExecutionProvider).GetField("_preparations", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var owned = (Dictionary<Guid, OwnedJob>)field.GetValue(provider)!;
            var job = owned[token.PreparationId];
            string blockedEndpoint = Path.Combine(job.WorkDir, "stderr.capture.fifo");
            await File.WriteAllTextAsync(blockedEndpoint, "existing-sentinel", TestContext.Current.CancellationToken);
            string capturedMarker = Path.Combine(workspace, "captured");
            string ordinaryMarker = Path.Combine(workspace, "ordinary");
            var refused = provider.LaunchCaptured(token,
                ProcessStartSpec.Vector("/bin/sh", ["-c", "printf captured > \"$1\"", "gpm5", capturedMarker], workspace),
                new OutputCaptureOptions(64, 64, 128, true));
            var failure = Assert.IsType<CaptureLaunchResult.Failed>(refused);
            Assert.Contains(failure.Reasons, r => r.Contains("before dispatch", StringComparison.Ordinal));
            Assert.False(File.Exists(capturedMarker));
            Assert.False(File.Exists(Path.Combine(job.WorkDir, "stdout.capture.fifo")));
            Assert.Equal("existing-sentinel", await File.ReadAllTextAsync(blockedEndpoint, TestContext.Current.CancellationToken));

            var launch = Assert.IsType<LaunchResult.Started>(provider.Launch(token,
                ProcessStartSpec.Vector("/bin/sh", ["-c", "printf ordinary > \"$1\"", "gpm5", ordinaryMarker], workspace)));
            var completion = await provider.WaitForCompletionAsync(launch.Handle, TestContext.Current.CancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            Assert.Equal(0, Assert.IsType<CompletionResult.NaturalExit>(completion).RootExitCode);
            Assert.Equal("ordinary", await File.ReadAllTextAsync(ordinaryMarker, TestContext.Current.CancellationToken));
            Assert.False(File.Exists(capturedMarker));
        }
        finally
        {
            // Cleanup retains its independent deadline after test cancellation.
            await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(65), CancellationToken.None);
            Directory.Delete(workspace, recursive: true);
        }
    }
}
