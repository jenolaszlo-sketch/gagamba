// Process launcher with bounded I/O and independent watchdog.
// GP-1A: single worker process only. Owned-tree guarantees arrive in GP-1B.
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace Gagamba.Fixture.Host;

public enum RawOutcome
{
    Exited,
    Timeout,
    OutputLimit,
    SpawnFailed,
}

public sealed record RawRunResult(
    RawOutcome Outcome,
    int? ExitCode,
    byte[] Stdout,
    byte[] Stderr,
    bool StdoutTruncated,
    bool StderrTruncated,
    long DurationMs,
    string? Error = null);

public sealed record ProtocolStep(string Op, JsonObject? Params);

public sealed record ProtocolSessionResult(
    List<string> WorkerLines,
    List<JsonObject> Results,
    int? ExitCode,
    long DurationMs);

public static class FixtureRunner
{
    public static TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds(30);
    public static TimeSpan DefaultStopBudget = TimeSpan.FromSeconds(5);
    public const long DefaultStdoutCap = 1 * 1024 * 1024;
    public const long DefaultStderrCap = 1 * 1024 * 1024;
    public const long DefaultStdinCap = 64 * 1024;

    public static string ResolveWorkerPath(string? hint = null)
    {
        if (!string.IsNullOrEmpty(hint))
        {
            if (File.Exists(hint)) return Path.GetFullPath(hint);
            string dll = Path.ChangeExtension(hint, ".dll");
            if (File.Exists(dll)) return Path.GetFullPath(dll);
        }

        string? env = Environment.GetEnvironmentVariable("GAGAMBA_WORKER_PATH");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
            return Path.GetFullPath(env);

        // Walk up from the caller base dir: <repo>/tests/Fixtures/<Proj>/bin/<cfg>/net10.0
        // and probe the sibling Worker output for both Debug and Release.
        string baseDir = AppContext.BaseDirectory;
        var candidates = new List<string>();
        for (string? dir = baseDir; dir is not null; dir = Path.GetDirectoryName(dir))
        {
            candidates.Add(Path.Combine(dir, "Gagamba.Fixture.Worker.exe"));
            candidates.Add(Path.Combine(dir, "Gagamba.Fixture.Worker.dll"));
            string sibling = Path.Combine(dir, "Gagamba.Fixture.Worker", "bin");
            if (Directory.Exists(sibling))
            {
                foreach (string cfg in new[] { "Release", "Debug" })
                {
                    candidates.Add(Path.Combine(sibling, cfg, "net10.0", "Gagamba.Fixture.Worker.exe"));
                    candidates.Add(Path.Combine(sibling, cfg, "net10.0", "Gagamba.Fixture.Worker.dll"));
                }
            }
            // Stop at tests/Fixtures level.
            if (Path.GetFileName(dir)?.Equals("Fixtures", StringComparison.OrdinalIgnoreCase) == true)
                break;
        }
        // Also probe relative to base dir directly (harness + worker side by side is not the layout).
        foreach (string c in candidates)
            if (File.Exists(c))
                return Path.GetFullPath(c);

        throw new FileNotFoundException(
            "Fixture worker not found. Build Gagamba.Fixture.Worker (Release) or set GAGAMBA_WORKER_PATH.");
    }

    public static (string FileName, string Arguments) BuildLaunch(string workerPath, string workerArgs)
    {
        if (workerPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return ("dotnet", $"\"{workerPath}\" {workerArgs}");
        return ($"\"{workerPath}\"", workerArgs);
    }

    /// <summary>
    /// Runs a raw (non-protocol) worker invocation with bounded capture.
    /// Independent watchdog kills the process on deadline or output overflow.
    /// </summary>
    public static async Task<RawRunResult> RunRawAsync(
        string workerPath,
        string workerArgs,
        byte[]? stdinBytes = null,
        TimeSpan? timeout = null,
        long stdoutCap = DefaultStdoutCap,
        long stderrCap = DefaultStderrCap,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TimeSpan limit = timeout ?? DefaultExecutionTimeout;
        if (stdinBytes is not null && stdinBytes.Length > DefaultStdinCap)
            return new(RawOutcome.SpawnFailed, null, [], [], false, false, 0, "stdin-exceeds-64KiB");

        var (fileName, arguments) = BuildLaunch(workerPath, workerArgs);
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        // Explicit minimal environment: do not inherit secrets beyond OS needs.
        // Keep Path/SystemRoot for loader; tests never depend on other host vars.
        Process? proc;
        try { proc = Process.Start(psi); }
        catch (Exception ex) { return new(RawOutcome.SpawnFailed, null, [], [], false, false, sw.ElapsedMilliseconds, ex.GetType().Name); }
        if (proc is null)
            return new(RawOutcome.SpawnFailed, null, [], [], false, false, sw.ElapsedMilliseconds, "start-null");

        using (proc)
        {
            var stdout = new MemoryStream();
            var stderr = new MemoryStream();
            bool stdoutOver = false;
            bool stderrOver = false;
            var stdoutDone = new TaskCompletionSource();
            var stderrDone = new TaskCompletionSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] buf = new byte[8192];
                    int n;
                    Stream s = proc.StandardOutput.BaseStream;
                    while ((n = await s.ReadAsync(buf, ct)) > 0)
                    {
                        if (stdoutOver)
                            continue; // discard excess after bound; memory stays capped
                        if (stdout.Length + n > stdoutCap) { stdoutOver = true; continue; }
                        stdout.Write(buf, 0, n);
                    }
                }
                catch { /* process exit races */ }
                finally { stdoutDone.TrySetResult(); }
            }, ct);
            _ = Task.Run(async () =>
            {
                try
                {
                    byte[] buf = new byte[8192];
                    int n;
                    Stream s = proc.StandardError.BaseStream;
                    while ((n = await s.ReadAsync(buf, ct)) > 0)
                    {
                        if (stderrOver)
                            continue;
                        if (stderr.Length + n > stderrCap) { stderrOver = true; continue; }
                        stderr.Write(buf, 0, n);
                    }
                }
                catch { }
                finally { stderrDone.TrySetResult(); }
            }, ct);

            try
            {
                if (stdinBytes is not null)
                {
                    await proc.StandardInput.BaseStream.WriteAsync(stdinBytes, ct);
                }
                proc.StandardInput.Close();
            }
            catch { /* closed/failed is a test failure downstream */ }

            // Independent watchdog: deadline + overflow both trigger stop.
            using var deadlineCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadlineCts.CancelAfter(limit);
            try
            {
                await proc.WaitForExitAsync(deadlineCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Deadline hit: stop with its own cleanup budget.
                TryKillTree(proc);
                bool exited = await WaitForExitBudget(proc, DefaultStopBudget);
                await Task.WhenAll(stdoutDone.Task, stderrDone.Task).WaitAsync(TimeSpan.FromSeconds(2));
                sw.Stop();
                if (!exited)
                    return new(RawOutcome.Timeout, null, stdout.ToArray(), stderr.ToArray(), stdoutOver, stderrOver, sw.ElapsedMilliseconds, "termination-unconfirmed");
                return new(RawOutcome.Timeout, proc.ExitCode, stdout.ToArray(), stderr.ToArray(), stdoutOver, stderrOver, sw.ElapsedMilliseconds);
            }

            // Overflow observed while process still running: stop and report OutputLimit.
            if (stdoutOver || stderrOver)
            {
                TryKillTree(proc);
                bool exited = await WaitForExitBudget(proc, DefaultStopBudget);
                await Task.WhenAll(stdoutDone.Task, stderrDone.Task).WaitAsync(TimeSpan.FromSeconds(2));
                sw.Stop();
                if (!exited)
                    return new(RawOutcome.Timeout, null, Truncate(stdout, stdoutCap), Truncate(stderr, stderrCap), true, true, sw.ElapsedMilliseconds, "termination-unconfirmed-after-overflow");
                return new(RawOutcome.OutputLimit, proc.ExitCode, Truncate(stdout, stdoutCap), Truncate(stderr, stderrCap), true, stderrOver, sw.ElapsedMilliseconds);
            }

            await Task.WhenAll(stdoutDone.Task, stderrDone.Task).WaitAsync(TimeSpan.FromSeconds(5));
            sw.Stop();
            return new(RawOutcome.Exited, proc.ExitCode, stdout.ToArray(), stderr.ToArray(), false, false, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Runs one protocol session: Ready -> Continue* -> Result* -> exit.
    /// Validates every worker line strictly; any violation throws.
    /// </summary>
    public static async Task<ProtocolSessionResult> RunProtocolAsync(
        string workerPath,
        string runId,
        string workerId,
        string workspace,
        IReadOnlyList<ProtocolStep> steps,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TimeSpan limit = timeout ?? DefaultExecutionTimeout;
        var (fileName, arguments) = BuildLaunch(workerPath,
            $"protocol --run-id {runId} --worker-id {workerId} --workspace \"{workspace}\"");
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("worker start returned null");
        var workerLines = new List<string>();
        var results = new List<JsonObject>();
        using var deadlineCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadlineCts.CancelAfter(limit);

        try
        {
            proc.StandardInput.AutoFlush = true;
            string? ready = await ReadLineBudget(proc.StandardOutput, deadlineCts.Token);
            if (ready is null)
                throw new InvalidOperationException("worker produced no Ready (timeout/eof)");
            workerLines.Add(ready);
            var vr = FixtureProtocol.ValidateWorkerLine(ready, runId, workerId, 0);
            if (!vr.Ok || vr.Message!["kind"]!.GetValue<string>() != "Ready")
                throw new InvalidOperationException($"invalid Ready: {vr.Reason}");

            int hostSeq = 1;
            int expectWorkerSeq = 1;
            foreach (var step in steps)
            {
                string cmd = FixtureProtocol.BuildContinue(runId, workerId, hostSeq++, step.Op, step.Params);
                await proc.StandardInput.WriteLineAsync(cmd.AsMemory(), deadlineCts.Token);
                string? resp = await ReadLineBudget(proc.StandardOutput, deadlineCts.Token);
                if (resp is null)
                    throw new InvalidOperationException($"worker eof after op {step.Op}");
                workerLines.Add(resp);
                var r = FixtureProtocol.ValidateWorkerLine(resp, runId, workerId, expectWorkerSeq++);
                if (!r.Ok)
                    throw new InvalidOperationException($"invalid worker line after {step.Op}: {r.Reason}");
                string kind = r.Message!["kind"]!.GetValue<string>();
                if (kind == "Error")
                    throw new InvalidOperationException($"worker Error after {step.Op}: {r.Message["payload"]}");
                if (kind != "Result")
                    throw new InvalidOperationException($"expected Result after {step.Op}, got {kind}");
                results.Add((JsonObject)r.Message["payload"]!.DeepClone());
                if (step.Op == "exit")
                    break;
            }

            proc.StandardInput.Close();
            bool exited = await WaitForExitBudget(proc, DefaultStopBudget);
            sw.Stop();
            if (!exited)
            {
                TryKillTree(proc);
                throw new InvalidOperationException("worker did not exit within stop budget");
            }
            return new(workerLines, results, proc.ExitCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            TryKillTree(proc);
            throw;
        }
    }

    private static async Task<string?> ReadLineBudget(StreamReader reader, CancellationToken ct)
    {
        try
        {
            // StreamReader.ReadLineAsync has no aborting CT overload on all runtimes;
            // race it against cancellation instead (watchdog).
            Task<string?> read = reader.ReadLineAsync();
            Task done = await Task.WhenAny(read, Task.Delay(Timeout.Infinite, ct));
            if (!ReferenceEquals(done, read))
                return null;
            return await read;
        }
        catch { return null; }
    }

    private static void TryKillTree(Process proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            try { if (!proc.HasExited) proc.Kill(); } catch { /* best effort */ }
        }
    }

    private static async Task<bool> WaitForExitBudget(Process proc, TimeSpan budget)
    {
        try { await proc.WaitForExitAsync(new CancellationTokenSource(budget).Token); return true; }
        catch (OperationCanceledException) { return !proc.HasExited ? false : true; }
    }

    private static byte[] Truncate(MemoryStream ms, long cap)
    {
        byte[] all = ms.ToArray();
        return all.Length <= cap ? all : all[..(int)cap];
    }
}
