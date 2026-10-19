// Disposable-process diagnostic for the untagged SIGPIPE mutation. It records
// the disposition before and after ReleasePipeWrite.WriteOnce, checks the
// disposition inherited by an exec'd child, restores the original disposition,
// and reports the observation. It runs in a dedicated test host.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[Trait("Category", "ProcessGlobalHostState")]
public sealed class Wp05SigpipeDispositionTests
{
    private const int SigPipe = 13;
    private const long SigIgn = 1;

    private readonly ITestOutputHelper _output;

    public Wp05SigpipeDispositionTests(ITestOutputHelper output) => _output = output;

    [DllImport("libc", EntryPoint = "sigaction", SetLastError = true)]
    private static extern int Sigaction(int signal, IntPtr action, IntPtr previous);

    [Fact]
    public void WriteOnceMutatesSigpipeOnlyIfTheHostDidNotAlreadyIgnoreIt()
    {
        if (!OperatingSystem.IsLinux()) Assert.Skip("linux only");

        var baseline = Query();
        var baselineHandler = Handler(baseline);
        _output.WriteLine($"baseline_sigpipe_handler=0x{baselineHandler.ToInt64():x}");

        Assert.True(NativeLaunchInterop.TryCreatePipe(out var read, out var write, out var pipeError), pipeError);
        using (read!)
        using (write!)
        {
            read!.Dispose();
            var result = ReleasePipeWrite.WriteOnce(
                checked((int)write!.DangerousGetHandle()), new byte[128]);
            Assert.Equal(ReleasePipeWriteResult.BrokenPipe, result);
        }

        var after = Query();
        var afterHandler = Handler(after);
        _output.WriteLine($"after_writeonce_sigpipe_handler=0x{afterHandler.ToInt64():x}");
        Assert.Equal(SigIgn, afterHandler.ToInt64());

        var source = System.IO.Path.Combine(RepositoryRoot(), "tests/Fixtures/wp05-sigpipe-probe.c");
        var executable = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "wp05-sigpipe-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Equal(0, Run("gcc", "-O2", "-o", executable, source));
            var child = Run(executable);
            _output.WriteLine($"child_sigpipe_handler_class={child} (0=SIG_DFL,1=SIG_IGN,2=handler)");
            Assert.Equal(1, child);
        }
        finally
        {
            if (File.Exists(executable)) File.Delete(executable);
        }

        Restore(baseline);
        Assert.Equal(baselineHandler, Handler(Query()));
        _output.WriteLine("sigpipe_disposition_restored");
    }

    private static byte[] Query()
    {
        var buffer = Marshal.AllocHGlobal(256);
        try
        {
            Marshal.Copy(new byte[256], 0, buffer, 256);
            if (Sigaction(SigPipe, IntPtr.Zero, buffer) != 0)
                throw new InvalidOperationException(
                    $"sigaction(SIGPIPE, NULL) errno {Marshal.GetLastPInvokeError()}");
            var result = new byte[256];
            Marshal.Copy(buffer, result, 0, 256);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void Restore(byte[] disposition)
    {
        var buffer = Marshal.AllocHGlobal(256);
        try
        {
            Marshal.Copy(disposition, 0, buffer, 256);
            if (Sigaction(SigPipe, buffer, IntPtr.Zero) != 0)
                throw new InvalidOperationException(
                    $"sigaction(SIGPIPE, restore) errno {Marshal.GetLastPInvokeError()}");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IntPtr Handler(byte[] disposition) =>
        (IntPtr)BitConverter.ToInt64(disposition, 0);

    private int Run(string fileName, params string[] arguments)
    {
        var start = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"failed to start {fileName}");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(15_000), $"{fileName} did not exit");
        if (stdout.Length != 0) _output.WriteLine($"{fileName} stdout: {stdout.Trim()}");
        if (stderr.Length != 0) _output.WriteLine($"{fileName} stderr: {stderr.Trim()}");
        return process.ExitCode;
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var root = System.IO.Path.GetFullPath(configured);
            if (File.Exists(System.IO.Path.Combine(root, "Gagamba.sln"))) return root;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(System.IO.Path.Combine(directory.FullName, "Gagamba.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Gagamba repository root not found");
    }
}


