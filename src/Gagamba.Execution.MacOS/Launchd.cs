// launchctl interop owned by the macOS provider. Everything goes through
// the launchctl CLI (no private APIs, no PID handling): bootstrap installs
// a unique job, kickstart starts it, print observes readiness, bootout
// removes it. stop is never used: it manipulates the running instance
// without removing the service (M9). Nothing here throws for expected
// failures; every call classifies into (rc, truncated output).
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.MacOS.Tests")]

namespace Gagamba.Execution.MacOS;

internal static class Launchd
{
    internal const int TimeoutMs = 30000;

    [DllImport("libc")]
    internal static extern uint getuid();

    internal static string UserDomain()
    {
        try { return $"gui/{getuid()}"; }
        catch { return "gui/501"; }
    }

    internal static (int Rc, string Output) Run(string verb, string target, string? extraArg = null)
    {
        var psi = new ProcessStartInfo("launchctl", "")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(verb);
        psi.ArgumentList.Add(target);
        if (extraArg is not null) psi.ArgumentList.Add(extraArg);
        var sb = new StringBuilder();
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return (127, "launchctl did not start");
            sb.Append(p.StandardOutput.ReadToEnd());
            sb.Append(p.StandardError.ReadToEnd());
            if (!p.WaitForExit(TimeoutMs))
            {
                try { p.Kill(); } catch { }
                return (124, "launchctl timed out");
            }
            string s = sb.ToString();
            return (p.ExitCode, s.Length > 2000 ? s[..2000] : s);
        }
        catch (Exception ex)
        {
            return (127, $"launchctl fault: {ex.GetType().Name}");
        }
    }

    internal static (int Rc, string Output) Bootstrap(string domain, string plistPath) =>
        Run("bootstrap", domain, plistPath);

    internal static (int Rc, string Output) Kickstart(string serviceTarget) =>
        Run("kickstart", serviceTarget);

    internal static (int Rc, string Output) Bootout(string serviceTarget) =>
        Run("bootout", serviceTarget);

    internal static (int Rc, string Output) Print(string serviceTarget) =>
        Run("print", serviceTarget);

    /// <summary>True only when launchd reports the job actually running.
    /// The pid line, when present, is deliberately never read: no PID
    /// crosses into the provider, let alone the contract. The comparison is
    /// an exact match: instantly-exited on-demand jobs report
    /// `state = not running`, and substring matching misreads that as
    /// running, hanging completion forever.</summary>
    internal static bool IsRunning(string printOutput)
    {
        foreach (string line in printOutput.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("state =", StringComparison.Ordinal))
                return t.Equals("state = running", StringComparison.Ordinal);
        }
        return false;
    }

    /// <summary>Terminal state of a launchd job as reported by `print`.
    /// Running is true only while the job root is alive; a non-zero print rc
    /// means the service is no longer loaded (terminal). ExitCode is present
    /// once launchd records a last exit.</summary>
    internal readonly record struct JobState(bool Running, int? ExitCode);

    internal static JobState ParseState(string printOutput, int printRc)
    {
        bool running = false;
        int? exitCode = null;
        foreach (string line in printOutput.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("state =", StringComparison.Ordinal))
                running = t.Equals("state = running", StringComparison.Ordinal);
            else if (t.Contains("exit code", StringComparison.OrdinalIgnoreCase))
            {
                int eq = t.LastIndexOf('=');
                if (eq >= 0 && int.TryParse(t[(eq + 1)..].Trim(), out int code)) exitCode = code;
            }
        }
        // A non-zero rc means the service is not loaded at all: terminal.
        return new JobState(printRc == 0 && running, exitCode);
    }

    /// <summary>Split a command line into argv (whitespace, quotes, backslash).</summary>
    internal static string[] SplitArguments(string commandLine)
    {
        var args = new List<string>();
        var cur = new StringBuilder();
        bool inSingle = false, inDouble = false, have = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\\' && i + 1 < commandLine.Length && !inSingle)
            {
                cur.Append(commandLine[i + 1]);
                have = true;
                i++;
            }
            else if (c == '\'' && !inDouble) { inSingle = !inSingle; have = true; }
            else if (c == '"' && !inSingle) { inDouble = !inDouble; have = true; }
            else if (char.IsWhiteSpace(c) && !inSingle && !inDouble)
            {
                if (have) { args.Add(cur.ToString()); cur.Clear(); have = false; }
            }
            else { cur.Append(c); have = true; }
        }
        if (have) args.Add(cur.ToString());
        return args.ToArray();
    }
}
