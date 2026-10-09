using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.MacOS.Tests")]

namespace Gagamba.Execution.MacOS;

internal enum HelperFailure { None, Timeout, Start, OutputLimit, ReapUnconfirmed }
internal sealed record HelperResult(int Rc, string Output, string Stderr,
    HelperFailure Failure = HelperFailure.None)
{
    public bool Ok => Failure == HelperFailure.None && Rc == 0;
    public string Diagnostic => Failure == HelperFailure.None
        ? $"rc={Rc}: {Launchd.Excerpt(Stderr.Length > 0 ? Stderr : Output)}"
        : $"{Failure}: {Launchd.Excerpt(Stderr.Length > 0 ? Stderr : Output)}";
}

internal interface ILaunchdTransport
{
    HelperResult Run(string verb, string target, string? extraArg = null, TimeSpan? timeout = null);
}

// The actual launchctl process is always named by an absolute trusted path.
// Output limits are structural limits: excess data is drained, then refused.
internal sealed class ProcessLaunchdTransport(string helperPath = "/bin/launchctl") : ILaunchdTransport
{
    internal const int MaxStdoutChars = 256 * 1024;
    internal const int MaxStderrChars = 8 * 1024;
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public HelperResult Run(string verb, string target, string? extraArg = null, TimeSpan? timeout = null) =>
        RunAsync(verb, target, extraArg, timeout ?? DefaultTimeout).GetAwaiter().GetResult();

    private async Task<HelperResult> RunAsync(string verb, string target, string? extraArg, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        var psi = new ProcessStartInfo(helperPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(verb);
        psi.ArgumentList.Add(target);
        if (extraArg is not null) psi.ArgumentList.Add(extraArg);
        Process? process = null;
        Task<Process?>? starting = null;
        try
        {
            starting = Task.Run(() => Process.Start(psi));
            process = await starting.WaitAsync(deadline.Token).ConfigureAwait(false);
            if (process is null) return new HelperResult(127, "helper did not start", "", HelperFailure.Start);
            Task<(string Text, bool Overflow)> stdout = DrainAsync(process.StandardOutput,
                MaxStdoutChars, deadline.Token);
            Task<(string Text, bool Overflow)> stderr = DrainAsync(process.StandardError,
                MaxStderrChars, deadline.Token);
            await Task.WhenAll(process.WaitForExitAsync(deadline.Token), stdout, stderr).ConfigureAwait(false);
            var outResult = await stdout.ConfigureAwait(false);
            var errResult = await stderr.ConfigureAwait(false);
            return new HelperResult(process.ExitCode, outResult.Text, errResult.Text,
                outResult.Overflow || errResult.Overflow ? HelperFailure.OutputLimit : HelperFailure.None);
        }
        catch (OperationCanceledException)
        {
            if (process is null && starting is not null)
            {
                // A delayed Process.Start cannot be interrupted. Own its eventual
                // return and kill it, while reporting cleanup as unconfirmed now.
                _ = starting.ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result is { } late)
                    {
                        try { late.Kill(entireProcessTree: true); late.WaitForExit(5000); }
                        catch { }
                        finally { late.Dispose(); }
                    }
                }, TaskScheduler.Default);
                return new HelperResult(124, "helper start exceeded deadline", "", HelperFailure.ReapUnconfirmed);
            }
            if (process is not null && !await KillAndReapAsync(process).ConfigureAwait(false))
                return new HelperResult(124, "helper deadline and reap unconfirmed", "", HelperFailure.ReapUnconfirmed);
            return new HelperResult(124, "helper deadline exceeded", "", HelperFailure.Timeout);
        }
        catch (Exception ex)
        {
            if (process is not null && !process.HasExited)
            {
                if (!await KillAndReapAsync(process).ConfigureAwait(false))
                    return new HelperResult(127, $"helper fault and reap unconfirmed: {ex.GetType().Name}",
                        "", HelperFailure.ReapUnconfirmed);
            }
            return new HelperResult(127, $"helper start/io fault: {ex.GetType().Name}: {ex.Message}",
                "", HelperFailure.Start);
        }
        finally { process?.Dispose(); }
    }

    private static async Task<bool> KillAndReapAsync(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return process.HasExited;
        }
        catch { return false; }
    }

    private static async Task<(string Text, bool Overflow)> DrainAsync(StreamReader reader,
        int limit, CancellationToken token)
    {
        char[] buffer = new char[4096];
        var retained = new StringBuilder(Math.Min(limit, 8192));
        bool overflow = false;
        while (true)
        {
            int count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
            if (count == 0) break;
            int remaining = limit - retained.Length;
            if (remaining > 0) retained.Append(buffer, 0, Math.Min(remaining, count));
            if (count > remaining) overflow = true;
        }
        return (retained.ToString(), overflow);
    }
}

internal enum JobObservationKind { Running, Terminal, ConfirmedNotFound, Unknown }
internal sealed record JobObservation(JobObservationKind Kind, int? ExitCode, string Reason = "");

internal static class Launchd
{
    private static readonly ILaunchdTransport Default = new ProcessLaunchdTransport();

    [DllImport("libc")]
    internal static extern uint getuid();

    internal static string UserDomain()
    {
        try { return $"gui/{getuid()}"; }
        catch { return "gui/501"; }
    }

    // Compatibility facade for the existing native provider-test probe.
    internal static (int Rc, string Output) Run(string verb, string target, string? extraArg = null)
    {
        var result = Default.Run(verb, target, extraArg);
        return (result.Rc, result.Output.Length > 0 ? result.Output : result.Stderr);
    }
    internal static (int Rc, string Output) Bootstrap(string domain, string plistPath) =>
        Run("bootstrap", domain, plistPath);
    internal static (int Rc, string Output) Kickstart(string target) => Run("kickstart", target);
    internal static (int Rc, string Output) Bootout(string target) => Run("bootout", target);
    internal static (int Rc, string Output) Print(string target) => Run("print", target);

    internal static JobObservation Observe(HelperResult result)
    {
        if (result.Failure != HelperFailure.None)
            return new JobObservation(JobObservationKind.Unknown, null, result.Diagnostic);
        if (result.Rc != 0)
        {
            string[] lines = (result.Output + "\n" + result.Stderr).Split('\n');
            if (lines.Any(line => line.Trim().StartsWith("Could not find service ", StringComparison.Ordinal)
                && line.Contains(" in domain", StringComparison.Ordinal)))
                return new JobObservation(JobObservationKind.ConfirmedNotFound, null);
            return new JobObservation(JobObservationKind.Unknown, null, result.Diagnostic);
        }
        string? state = null;
        int? exit = null;
        bool malformedExit = false;
        foreach (string line in result.Output.Split('\n'))
        {
            string text = line.Trim();
            if (text.StartsWith("state =", StringComparison.Ordinal) && state is null)
                state = text["state =".Length..].Trim();
            else if (text.StartsWith("last exit code =", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(text["last exit code =".Length..].Trim(), out int code))
                    exit = code < 0 ? 128 - code : code;
                else malformedExit = true;
            }
            else if (text.StartsWith("last exit status =", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(text["last exit status =".Length..].Trim(), out int code))
                    exit = code < 0 ? 128 - code : code;
                else malformedExit = true;
            }
        }
        if (malformedExit)
            return new JobObservation(JobObservationKind.Unknown, null, "malformed launchd exit field");
        if (state == "running") return new JobObservation(JobObservationKind.Running, null);
        if (state is "not running" or "exited")
            return exit is int code
                ? new JobObservation(JobObservationKind.Terminal, code)
                : new JobObservation(JobObservationKind.Unknown, null, "terminal state lacks exit status");
        return new JobObservation(JobObservationKind.Unknown, null,
            state is null ? "launchd state missing" : $"unknown launchd state: {Excerpt(state)}");
    }

    internal static string Excerpt(string text) => text.Length > 160 ? text[..160] : text;
    internal static bool IsRunning(string output) =>
        Observe(new HelperResult(0, output, "")).Kind == JobObservationKind.Running;
    internal static bool IsRunningState(string line) => IsRunning(line);
    internal readonly record struct JobState(bool Running, int? ExitCode);
    internal static JobState ParseState(string output, int rc)
    {
        var result = Observe(new HelperResult(rc, output, ""));
        return new JobState(result.Kind == JobObservationKind.Running, result.ExitCode);
    }

    internal static string[] SplitArguments(string commandLine)
    {
        var args = new List<string>();
        var cur = new StringBuilder();
        bool inSingle = false, inDouble = false, have = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\\' && i + 1 < commandLine.Length && !inSingle)
            { cur.Append(commandLine[i + 1]); have = true; i++; }
            else if (c == '\'' && !inDouble) { inSingle = !inSingle; have = true; }
            else if (c == '"' && !inSingle) { inDouble = !inDouble; have = true; }
            else if (char.IsWhiteSpace(c) && !inSingle && !inDouble)
            { if (have) { args.Add(cur.ToString()); cur.Clear(); have = false; } }
            else { cur.Append(c); have = true; }
        }
        if (have) args.Add(cur.ToString());
        return args.ToArray();
    }
}
