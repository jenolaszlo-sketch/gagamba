using System.Reflection;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

// Native endpoint fault controls complement the real-provider qualification.
// The lifecycle task is deliberately supplied here; these do not qualify launchd.
public sealed class CapturePipeTests
{
    [Fact]
    public async Task Extra_writer_exceeding_grace_reports_incomplete_and_joins_readers()
    {
        if (!OperatingSystem.IsMacOS()) { Assert.Skip("requires native macOS FIFO endpoints"); return; }
        string directory = Directory.CreateTempSubdirectory("gagamba-capture-grace-").FullName;
        MacCapturePipes? capture = null;
        try
        {
            capture = MacCapturePipes.Create(directory,
                new OutputCaptureOptions(64, 64, 128, true, TimeSpan.FromMilliseconds(100)));
            using var extraWriter = File.OpenWrite(capture.StdoutPath);
            capture.BindCompletion(Task.FromResult<CompletionResult>(new CompletionResult.NaturalExit(0)));
            var result = await capture.Session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(OutputCaptureStatus.DrainIncomplete, result.Status);
            await capture.CleanupCompletion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            if (capture is not null)
            {
                capture.Abort();
                await capture.CleanupCompletion.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Native_read_failure_is_explicit_and_joins_both_readers()
    {
        if (!OperatingSystem.IsMacOS()) { Assert.Skip("requires native macOS FIFO endpoints"); return; }
        string directory = Directory.CreateTempSubdirectory("gagamba-capture-read-fault-").FullName;
        MacCapturePipes? capture = null;
        try
        {
            capture = MacCapturePipes.Create(directory, new OutputCaptureOptions(64, 64, 128, true));
            // Close and poison the exact owning handle while no native read
            // can run. The next read uses -1, never a recycled numeric FD.
            object stdout = typeof(MacCapturePipes).GetField("_stdout", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(capture)!;
            object gate = stdout.GetType().GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(stdout)!;
            var handle = (SafeFileHandle)stdout.GetType().GetField("_reader", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(stdout)!;
            lock (gate)
            {
                handle.Dispose();
                typeof(System.Runtime.InteropServices.SafeHandle).GetMethod("SetHandle",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(handle, [new IntPtr(-1)]);
            }
            capture.BindCompletion(Task.FromResult<CompletionResult>(new CompletionResult.NaturalExit(0)));
            var result = await capture.Session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(OutputCaptureStatus.ReadFailed, result.Status);
            await capture.CleanupCompletion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally
        {
            if (capture is not null)
            {
                capture.Abort();
                await capture.CleanupCompletion.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            Directory.Delete(directory, recursive: true);
        }
    }
}
