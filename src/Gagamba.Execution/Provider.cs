// GP-3 execution-provider SPI: the smallest boundary that negotiates,
// prepares, launches, terminates, and disposes an execution domain.
// Handles are opaque: no PIDs, handles, cgroup paths, PGIDs, or labels
// cross this contract. Providers own all OS mechanics.
namespace Gagamba.Execution;

/// <summary>Requested guarantees for one activity.</summary>
public sealed record ExecutionRequirements(
    IReadOnlyList<ExecutionRequirement> Required,
    IReadOnlyList<ExecutionRequirement>? Preferred = null);

/// <summary>What to run inside a prepared domain.</summary>
public sealed record ProcessStartSpec(
    string Executable,
    string Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment)
{
    public static ProcessStartSpec Simple(
        string executable, string arguments = "", string workingDirectory = "",
        IReadOnlyDictionary<string, string>? environment = null) =>
        new(executable, arguments, workingDirectory,
            environment ?? new Dictionary<string, string>());
}

/// <summary>
/// Opaque preparation: proves negotiation happened with THIS provider.
/// The token lets the provider recognize its own preparations; callers
/// cannot read anything from it.
/// </summary>
public sealed record PreparedExecution(
    string Provider,
    Guid PreparationId,
    IReadOnlyList<string> Met);

/// <summary>
/// Opaque running execution. No process IDs, no native handles.
/// Root exit does not invalidate it while descendants remain owned.
/// </summary>
public sealed record ExecutionHandle(
    string Provider,
    Guid ExecutionId);

/// <summary>Preparation outcome: usable domain or classified refusal.</summary>
public abstract record PrepareResult
{
    private PrepareResult() { }
    public sealed record Accepted(PreparedExecution Prepared) : PrepareResult;
    public sealed record Rejected(IReadOnlyList<string> Reasons) : PrepareResult;
}

/// <summary>Launch outcome: running handle or classified refusal.</summary>
public abstract record LaunchResult
{
    private LaunchResult() { }
    public sealed record Started(ExecutionHandle Handle) : LaunchResult;
    public sealed record Failed(IReadOnlyList<string> Reasons) : LaunchResult;
}

/// <summary>Termination outcome. Uses the domain primitive, never PID lists.</summary>
public abstract record TerminateResult
{
    private TerminateResult() { }
    public sealed record Terminated(ExecutionHandle Handle) : TerminateResult;
    public sealed record Failed(IReadOnlyList<string> Reasons) : TerminateResult;
}

/// <summary>
/// Reclamation outcome for a preparation that will not be launched. Discarding
/// is how a lifecycle that stops after Prepare (for example a pre-launch
/// authority denial) returns any domain resources the provider allocated.
/// </summary>
public abstract record DiscardResult
{
    private DiscardResult() { }
    public sealed record Discarded(PreparedExecution Prepared) : DiscardResult;
    public sealed record Failed(IReadOnlyList<string> Reasons) : DiscardResult;
}

/// <summary>
/// How a launched execution reached its terminal state. Completion means the
/// root invocation has terminated AND the provider-controlled execution domain
/// has reached its terminal state (no owned processes remain), not merely that
/// the root exited.
/// </summary>
public abstract record CompletionResult
{
    private CompletionResult() { }
    /// <summary>The root invocation exited on its own. Any exit code is a
    /// natural exit; the caller decides whether a non-zero code is a failure.</summary>
    public sealed record NaturalExit(int RootExitCode) : CompletionResult;
    /// <summary>The execution was terminated (explicitly) and the domain
    /// reached its terminal state. No portable root exit code is claimed.</summary>
    public sealed record Terminated : CompletionResult;
    /// <summary>The handle is foreign/stale or the wait faulted. Fail closed.</summary>
    public sealed record Failed(IReadOnlyList<string> Reasons) : CompletionResult;
}

/// <summary>
/// Minimal provider boundary: negotiation → preparation → launch →
/// lifecycle control → disposal. Providers must throw
/// <see cref="ObjectDisposedException"/> once disposed.
/// </summary>
public interface IExecutionProvider : IAsyncDisposable
{
    /// <summary>What this platform guarantees (the GP-2 matrix row).</summary>
    PlatformCapabilities Describe();

    /// <summary>Negotiate requirements; fail closed with reasons.</summary>
    PrepareResult Prepare(ExecutionRequirements requirements);

    /// <summary>Reclaim a preparation that will not be launched. Idempotent,
    /// single-use, and safe before launch; a preparation not issued by this
    /// provider fails closed. After a successful discard the preparation can
    /// never launch.</summary>
    DiscardResult Discard(PreparedExecution preparation);

    /// <summary>Launch a process as the domain root. Suspend-assign-resume
    /// semantics where the platform supports them; never an escape window.</summary>
    LaunchResult Launch(PreparedExecution prepared, ProcessStartSpec process);

    /// <summary>Terminate the whole domain via its own primitive.</summary>
    TerminateResult Terminate(ExecutionHandle execution);

    /// <summary>
    /// Wait until the root has terminated and the provider-controlled domain is
    /// empty, then reclaim the handle's provider resources (like Discard
    /// consumes a preparation). The cancellation token cancels the wait only;
    /// it never terminates the execution — terminate explicitly to interrupt a
    /// running domain. A handle not issued by this provider fails closed.
    /// </summary>
    ValueTask<CompletionResult> WaitForCompletionAsync(ExecutionHandle execution,
        CancellationToken cancellationToken = default);
}
