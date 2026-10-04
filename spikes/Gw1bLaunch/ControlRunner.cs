// Unsandboxed positive controls for GW-1B denial legs. Runs the same fixture
// commands directly (no sandbox) to prove each denial target is reachable
// without Gagamba. Controls live only in this spike, never in production paths.
using System.Diagnostics;
using System.Text;

namespace Gagamba.Spikes.Gw1bLaunch;

internal sealed record DirectResult(bool Exited, int ExitCode, string Detail, string Stdout);

internal static class ControlRunner
{
    public const int MaxStdoutChars = 65536;

    /// <summary>Runs a fixture command unsandboxed with bounded capture.</summary>
    public static async Task<DirectResult> Run(string exe, string args, string cwd, int timeoutMs = 30_000,
        bool createNoWindow = true)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = createNoWindow,
        };
        using var proc = Process.Start(psi);
        if (proc is null)
            return new(false, -1, "control start returned null", string.Empty);
        var sw = Stopwatch.StartNew();
        string stdout, stderr;
        try
        {
            // Fixture outputs are tiny (<1 KB); bounded wait prevents hangs.
            var outTask = proc.StandardOutput.ReadToEndAsync();
            var errTask = proc.StandardError.ReadToEndAsync();
            bool exited = await Task.Run(() => proc.WaitForExit(timeoutMs));
            if (!exited)
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                return new(false, -1, "control timed out and was killed", string.Empty);
            }
            stdout = await outTask;
            stderr = await errTask;
            if (stdout.Length > MaxStdoutChars)
                stdout = stdout[..MaxStdoutChars];
        }
        catch (Exception ex)
        {
            return new(false, -1, $"control capture fault: {ex.GetType().Name}", string.Empty);
        }
        sw.Stop();
        string shortOut = stdout.Trim();
        var detail = new StringBuilder($"exit={proc.ExitCode} ({sw.ElapsedMilliseconds}ms)");
        if (shortOut.Length > 0) detail.Append($" out='{shortOut[..Math.Min(80, shortOut.Length)]}'");
        if (stderr.Length > 0)
        {
            string shortErr = stderr.Trim();
            detail.Append($" err='{shortErr[..Math.Min(80, shortErr.Length)]}'");
        }
        return new(true, proc.ExitCode, detail.ToString(), stdout);
    }
}
