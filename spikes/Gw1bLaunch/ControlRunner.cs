// Unsandboxed positive controls for GW-1B denial legs. Runs the same fixture
// commands directly (no sandbox) to prove each denial target is reachable
// without Gagamba. Controls live only in this spike, never in production paths.
using System.Diagnostics;
using System.Text;

namespace Gagamba.Spikes.Gw1bLaunch;

internal sealed record DirectResult(bool Exited, int ExitCode, string Detail);

internal static class ControlRunner
{
    /// <summary>Runs a fixture command unsandboxed with bounded capture.</summary>
    public static async Task<DirectResult> Run(string exe, string args, string cwd, int timeoutMs = 30_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi);
        if (proc is null)
            return new(false, -1, "control start returned null");
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
                return new(false, -1, "control timed out and was killed");
            }
            stdout = (await outTask).Trim();
            stderr = (await errTask).Trim();
        }
        catch (Exception ex)
        {
            return new(false, -1, $"control capture fault: {ex.GetType().Name}");
        }
        sw.Stop();
        var detail = new StringBuilder($"exit={proc.ExitCode} ({sw.ElapsedMilliseconds}ms)");
        if (stdout.Length > 0) detail.Append($" out='{stdout[..Math.Min(80, stdout.Length)]}'");
        if (stderr.Length > 0) detail.Append($" err='{stderr[..Math.Min(80, stderr.Length)]}'");
        return new(true, proc.ExitCode, detail.ToString());
    }
}
