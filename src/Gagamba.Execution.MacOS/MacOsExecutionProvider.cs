// GM-2 macOS execution provider: IExecutionProvider over launchd jobs +
// process groups. Native profile only: no watchdog composition, no PG-kill
// fallback. Launch succeeds only after launchd owns the job (bootstrap)
// AND the target demonstrably runs under it (print shows running); a
// private poll proves readiness without any PID crossing the contract.
// Terminate is bootout (authoritative; stop is never sufficient). Cleanup
// always attempts bootout. Environment comes exclusively from the spec
// (direct plist EnvironmentVariables: spec granted, ambient never
// inherited; launchd adds its session vars — documented platform delta,
// see evidence; no trampoline, GP-3 untouched).
namespace Gagamba.Execution.MacOS;

public sealed class MacOsExecutionProvider : IExecutionProvider
{
    private readonly object _gate = new();
    private readonly string _domain;
    private bool _disposed;
    private readonly Dictionary<Guid, OwnedJob> _preparations = new();
    private readonly Dictionary<Guid, ExecutionRecord> _executions = new();

    private sealed record ExecutionRecord(OwnedJob Job, bool Terminated);

    /// <param name="domain">launchd domain, default gui/&lt;uid&gt;.
    /// Validated at Prepare (print must succeed).</param>
    public MacOsExecutionProvider(string? domain = null)
    {
        _domain = string.IsNullOrWhiteSpace(domain) ? Launchd.UserDomain() : domain!;
    }

    public PlatformCapabilities Describe() => WellKnownPlatforms.MacOs;

    public PrepareResult Prepare(ExecutionRequirements requirements)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Native profile only: composition is a separate, explicit layer.
        var r = ExecutionNegotiator.Negotiate(
            WellKnownPlatforms.MacOs, requirements.Required, requirements.Preferred);
        if (!r.Accepted)
            return new PrepareResult.Rejected(r.Unmet);
        if (!CheckDomain(out string why))
            return new PrepareResult.Rejected(new[] { why });
        // Allocate the private job directory at preparation so a caller that
        // never launches can reclaim it through Discard.
        OwnedJob job;
        try { job = OwnedJob.Create(_domain); }
        catch (Exception ex)
        {
            return new PrepareResult.Rejected(new[] { $"job directory unavailable: {ex.GetType().Name}" });
        }
        var prep = new PreparedExecution(
            WellKnownPlatforms.MacOs.Platform, Guid.NewGuid(), r.Met);
        lock (_gate) _preparations[prep.PreparationId] = job;
        return new PrepareResult.Accepted(prep);
    }

    public DiscardResult Discard(PreparedExecution preparation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (preparation.Provider != WellKnownPlatforms.MacOs.Platform)
            return new DiscardResult.Failed(
                new[] { "unknown preparation: not issued by this provider" });
        // Idempotent: a preparation already launched or discarded is a no-op.
        OwnedJob? job;
        lock (_gate) _preparations.Remove(preparation.PreparationId, out job);
        try { job?.Dispose(); } catch { }
        return new DiscardResult.Discarded(preparation);
    }

    public LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        OwnedJob? job;
        lock (_gate)
        {
            if (prepared.Provider != WellKnownPlatforms.MacOs.Platform
                || !_preparations.Remove(prepared.PreparationId, out job))
                return new LaunchResult.Failed(
                    new[] { "unknown preparation: not issued by this provider" });
            // Single-use token: one Launch attempt consumes it.
        }
        if (string.IsNullOrWhiteSpace(process.Executable))
        {
            try { job.Dispose(); } catch { }
            return new LaunchResult.Failed(new[] { "no executable" });
        }
        if (!TryBuildEnvironment(process.Environment, out var env, out string envError))
        {
            try { job.Dispose(); } catch { }
            return new LaunchResult.Failed(new[] { envError });
        }
        return LaunchInner(job, process, env!);
    }

    public TerminateResult Terminate(ExecutionHandle execution)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExecutionRecord? rec;
        lock (_gate)
        {
            if (execution.Provider != WellKnownPlatforms.MacOs.Platform
                || !_executions.TryGetValue(execution.ExecutionId, out rec))
                return new TerminateResult.Failed(
                    new[] { "unknown execution: not issued by this provider" });
        }
        // Idempotent: an already-terminated domain still reports success,
        // and the record stays so repeats classify the same way.
        if (rec.Terminated) return new TerminateResult.Terminated(execution);
        var (ok, err) = rec.Job.Bootout();
        if (!ok)
            return new TerminateResult.Failed(new[] { err });
        lock (_gate)
        {
            if (_executions.TryGetValue(execution.ExecutionId, out var cur))
                _executions[execution.ExecutionId] = cur with { Terminated = true };
        }
        return new TerminateResult.Terminated(execution);
    }

    public async ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle execution,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ExecutionRecord? rec;
        lock (_gate)
        {
            if (execution.Provider != WellKnownPlatforms.MacOs.Platform
                || !_executions.TryGetValue(execution.ExecutionId, out rec))
                return new CompletionResult.Failed(
                    new[] { "unknown execution: not issued by this provider" });
        }
        // Observe launchd job termination and its recorded exit status. An
        // escaped setsid() descendant is outside the launchd-managed domain
        // and does not block completion (macOS advertises only Partial).
        int? exitCode = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool terminated;
            lock (_gate) terminated = _executions.TryGetValue(execution.ExecutionId, out var cur) && cur.Terminated;
            if (terminated) break;
            var (rc, output) = Launchd.Print(rec.Job.ServiceTarget);
            var state = Launchd.ParseState(output, rc);
            if (state.ExitCode is int code) exitCode = code;
            if (!state.Running) break; // terminal: exited or no longer loaded
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }
        bool wasTerminated;
        lock (_gate)
        {
            wasTerminated = _executions.TryGetValue(execution.ExecutionId, out var cur) && cur.Terminated;
            _executions.Remove(execution.ExecutionId); // completion consumes the handle
        }
        try { rec.Job.Dispose(); } catch { } // bootout + delete the private directory
        if (wasTerminated) return new CompletionResult.Terminated();
        return exitCode is int finalCode
            ? new CompletionResult.NaturalExit(finalCode)
            : new CompletionResult.Failed(new[] { "launchd exit status unavailable" });
    }

    public ValueTask DisposeAsync()
    {
        List<ExecutionRecord> owned;
        List<OwnedJob> prepared;
        lock (_gate)
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            owned = new List<ExecutionRecord>(_executions.Values);
            _executions.Clear();
            prepared = new List<OwnedJob>(_preparations.Values);
            _preparations.Clear();
        }
        foreach (var rec in owned)
        {
            try
            {
                rec.Job.Bootout();
                rec.Job.Dispose();
            }
            catch { }
        }
        foreach (var job in prepared)
        {
            try { job.Dispose(); } catch { }
        }
        return ValueTask.CompletedTask;
    }

    private LaunchResult LaunchInner(OwnedJob job, ProcessStartSpec process, IReadOnlyDictionary<string, string> env)
    {
        LaunchResult Fail(string reason)
        {
            try
            {
                try { job.Bootout(); } catch { }
                job.Dispose();
            }
            catch { }
            return new LaunchResult.Failed(new[] { reason });
        }
        try { job.WritePlist(process.Executable, Launchd.SplitArguments(process.Arguments), process.WorkingDirectory, env); }
        catch (Exception ex)
        {
            return Fail($"plist write failed: {ex.GetType().Name}");
        }
        var (brc, bout) = Launchd.Bootstrap(_domain, job.PlistPath);
        if (brc != 0)
            return Fail($"bootstrap refused rc={brc}: {FirstLine(bout)} (no job loaded)");
        var (krc, kout) = Launchd.Kickstart(job.ServiceTarget);
        if (krc != 0)
            return Fail($"kickstart refused rc={krc}: {FirstLine(kout)} (booted out)");
        // Launch invariant: success only after the target demonstrably
        // runs under the owned job. Failure here boots out immediately.
        if (!WaitRunning(job, 15000))
            return Fail("job never reached running (booted out; target may not exist)");
        var handle = new ExecutionHandle(WellKnownPlatforms.MacOs.Platform, Guid.NewGuid());
        lock (_gate) _executions[handle.ExecutionId] = new ExecutionRecord(job, false);
        return new LaunchResult.Started(handle);
    }

    private static bool WaitRunning(OwnedJob job, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var (rc, out_) = Launchd.Print(job.ServiceTarget);
                var state = Launchd.ParseState(out_, rc);
                // Demonstrably ran: observed running now, or exited with a
                // recorded code before the first observation. Instant-exit
                // commands report `state = not running` (never `running`);
                // the recorded code is the proof of execution.
                if (rc == 0 && (state.Running || state.ExitCode.HasValue)) return true;
            }
            catch { }
            Thread.Sleep(200);
        }
        try
        {
            var (rc, out_) = Launchd.Print(job.ServiceTarget);
            var state = Launchd.ParseState(out_, rc);
            return rc == 0 && (state.Running || state.ExitCode.HasValue);
        }
        catch { return false; }
    }

    private static string FirstLine(string s)
    {
        int i = s.IndexOf('\n');
        string line = (i < 0 ? s : s[..i]).Trim();
        return line.Length > 160 ? line[..160] : line;
    }

    /// <summary>macOS env is case-sensitive (Unix): duplicates compare
    /// ordinally. Names must be non-empty, contain no '=' or NUL; values
    /// must contain no NUL.</summary>
    internal static bool TryBuildEnvironment(
        IReadOnlyDictionary<string, string> spec,
        out IReadOnlyDictionary<string, string>? env, out string error)
    {
        env = null;
        error = "";
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in spec.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(kv.Key)
                || kv.Key.Contains('=', StringComparison.Ordinal)
                || kv.Key.Contains('\0'))
            {
                error = $"invalid environment name {Format(kv.Key)} (empty, '=' or NUL)";
                return false;
            }
            if (kv.Value.Contains('\0'))
            {
                error = $"NUL byte in value of {Format(kv.Key)}";
                return false;
            }
            if (!seen.Add(kv.Key))
            {
                error = $"duplicate environment name {Format(kv.Key)}";
                return false;
            }
            clean[kv.Key] = kv.Value;
        }
        env = clean;
        return true;
    }

    private static string Format(string name) =>
        string.IsNullOrEmpty(name) ? "(empty)" : "'" + name + "'";

    private bool CheckDomain(out string why)
    {
        // Probe: print the domain itself. No job runs; failure classifies
        // as missing bootstrap rights, never as success.
        why = "";
        try
        {
            var (rc, out_) = Launchd.Print(_domain);
            if (rc != 0)
            {
                why = $"launchd domain '{_domain}' unavailable (no bootstrap rights?): rc={rc}";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            why = $"launchd unavailable: {ex.GetType().Name}";
            return false;
        }
    }
}
