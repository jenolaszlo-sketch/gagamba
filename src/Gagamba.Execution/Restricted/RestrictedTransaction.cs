// WP01 transaction vocabulary: release/exec/result dispositions, outcomes,
// capture and cleanup records, transaction states and the terminal
// observation. All types are internal; outcome semantics follow plan
// Sections 6/8/13 exactly. Unknown means insufficient trusted observation,
// never a zero exit or a cleanup result.
using System.Collections.Generic;

namespace Gagamba.Execution.Restricted;

/// <summary>Release disposition: uncommitted, committed, or conservatively
/// may-have-executed after durable intent without proven delivery.</summary>
internal enum ReleaseDisposition : byte
{
    NotCommitted = 0,
    Committed = 1,
    MayHaveExecuted = 2,
}

/// <summary>Exec disposition: never attempted, failed before exec with the
/// trusted errno, confirmed, or unknown.</summary>
internal enum ExecDisposition : byte
{
    NotAttempted = 0,
    FailedBeforeExec = 1,
    Confirmed = 2,
    Unknown = 3,
}

/// <summary>Target root result kind.</summary>
internal enum RootResultKind : byte
{
    Unknown = 0,
    NotStarted = 1,
    Exited = 2,
    Signalled = 3,
}

/// <summary>Target root result. A wrapper 128+signal convention is never a
/// target signal result; only this trusted record carries one.</summary>
internal sealed record RootResult
{
    public RootResult(RootResultKind kind, int exitCode, int signal, bool coreDumped, int rawWaitStatus)
    {
        Kind = kind;
        ExitCode = exitCode;
        Signal = signal;
        CoreDumped = coreDumped;
        RawWaitStatus = rawWaitStatus;
    }

    public RootResultKind Kind { get; }
    public int ExitCode { get; }
    public int Signal { get; }
    public bool CoreDumped { get; }
    public int RawWaitStatus { get; }
}

/// <summary>Helper/wrapper result kind. Separate from the target root even
/// though the shape matches.</summary>
internal enum HelperResultKind : byte
{
    Unknown = 0,
    NotStarted = 1,
    Exited = 2,
    Signalled = 3,
}

/// <summary>Helper/wrapper result. Kept separate from <see cref="RootResult"/>
/// in every observation.</summary>
internal sealed record HelperResult
{
    public HelperResult(HelperResultKind kind, int exitCode, int signal, bool coreDumped, int rawWaitStatus)
    {
        Kind = kind;
        ExitCode = exitCode;
        Signal = signal;
        CoreDumped = coreDumped;
        RawWaitStatus = rawWaitStatus;
    }

    public HelperResultKind Kind { get; }
    public int ExitCode { get; }
    public int Signal { get; }
    public bool CoreDumped { get; }
    public int RawWaitStatus { get; }
}

/// <summary>Fine execution outcome. A normal nonzero target exit is
/// TargetFailed, never a restriction violation.</summary>
internal enum ExecutionOutcome : byte
{
    Unsupported = 0,
    FailedToEstablish = 1,
    CancelledBeforeRelease = 2,
    Succeeded = 3,
    TargetFailed = 4,
    Stopped = 5,
    Unknown = 6,
}

/// <summary>First-accepted stop cause. The first cause is stable; later
/// causes are bounded diagnostics that cannot authorize release or replace
/// the result.</summary>
internal enum StopCauseKind : byte
{
    CallerStop = 0,
    CallerLoss = 1,
    SetupExpired = 2,
    SetupFault = 3,
    OutputOverflow = 4,
    OutputReadFailure = 5,
    RecoveryTakeover = 6,
    TransportFailure = 7,
}

/// <summary>How a terminal capture observation ended.</summary>
internal enum FinalDrainDisposition : byte
{
    Complete = 0,
    Incomplete = 1,
    Expired = 2,
}

/// <summary>Capture result: stored and observed byte counters per stream and
/// total, overflow/read-failure/truncation flags, EOF/completeness and final
/// drain disposition.</summary>
internal sealed record CaptureResult
{
    public CaptureResult(
        ulong storedStdoutBytes,
        ulong observedStdoutBytes,
        ulong storedStderrBytes,
        ulong observedStderrBytes,
        ulong storedTotalBytes,
        ulong observedTotalBytes,
        bool overflowStdout,
        bool overflowStderr,
        bool readFailure,
        bool truncated,
        bool stdoutEof,
        bool stderrEof,
        bool complete,
        FinalDrainDisposition drainDisposition)
    {
        StoredStdoutBytes = storedStdoutBytes;
        ObservedStdoutBytes = observedStdoutBytes;
        StoredStderrBytes = storedStderrBytes;
        ObservedStderrBytes = observedStderrBytes;
        StoredTotalBytes = storedTotalBytes;
        ObservedTotalBytes = observedTotalBytes;
        OverflowStdout = overflowStdout;
        OverflowStderr = overflowStderr;
        ReadFailure = readFailure;
        Truncated = truncated;
        StdoutEof = stdoutEof;
        StderrEof = stderrEof;
        Complete = complete;
        DrainDisposition = drainDisposition;
    }

    public ulong StoredStdoutBytes { get; }
    public ulong ObservedStdoutBytes { get; }
    public ulong ObservedStderrBytes { get; }
    public ulong StoredStderrBytes { get; }
    public ulong StoredTotalBytes { get; }
    public ulong ObservedTotalBytes { get; }
    public bool OverflowStdout { get; }
    public bool OverflowStderr { get; }
    public bool ReadFailure { get; }
    public bool Truncated { get; }
    public bool StdoutEof { get; }
    public bool StderrEof { get; }
    public bool Complete { get; }
    public FinalDrainDisposition DrainDisposition { get; }
}

/// <summary>Cleanup state. Confirmed requires every required cleanup item;
/// WP01 observes domain emptiness only, so model-derived reports stay
/// Unknown unless a later package fills the components.</summary>
internal enum CleanupState : byte
{
    Confirmed = 0,
    Failed = 1,
    Unknown = 2,
}

/// <summary>Cleanup report with separate component results. Null components
/// are unknown, never false.</summary>
internal sealed record CleanupReport
{
    public CleanupReport(
        CleanupState state,
        bool? domainEmpty,
        bool? reaped,
        bool? stagingRemoved,
        bool? journalFinalized)
    {
        State = state;
        DomainEmpty = domainEmpty;
        Reaped = reaped;
        StagingRemoved = stagingRemoved;
        JournalFinalized = journalFinalized;
    }

    public CleanupState State { get; }
    public bool? DomainEmpty { get; }
    public bool? Reaped { get; }
    public bool? StagingRemoved { get; }
    public bool? JournalFinalized { get; }
}

/// <summary>Transaction states. Terminal states are Completed, Failed,
/// Cancelled and Unknown; a published terminal observation is immutable.</summary>
internal enum TransactionState : byte
{
    Preparing = 0,
    Prepared = 1,
    Owned = 2,
    ReleaseCommitted = 3,
    Running = 4,
    Stopping = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8,
    Unknown = 9,
}

/// <summary>Refusal vocabulary for invalid transition attempts. A refusal
/// never mutates model state.</summary>
internal enum InvariantViolation : byte
{
    None = 0,
    InvalidStateOrder = 1,
    Expired = 2,
    Foreign = 3,
    Consumed = 4,
    Aborted = 5,
    StoppedFirst = 6,
    TransportFailed = 7,
}

/// <summary>Immutable terminal observation: identities, states and the
/// distinct result fields above, requested/established manifests, evidence
/// version and monotonic transition sequence.</summary>
internal sealed record TransactionObservation
{
    public TransactionObservation(
        PreparationIdentity? identity,
        RestrictedManifest? requestedManifest,
        RestrictedManifest? establishedManifest,
        TransactionState state,
        ExecutionOutcome? outcome,
        RootResult root,
        HelperResult helper,
        ExecDisposition exec,
        CaptureResult? capture,
        CleanupReport cleanup,
        ReleaseDisposition release,
        StopCauseKind? firstStopCause,
        IReadOnlyList<string> transitionNames,
        IReadOnlyList<long> transitionAtNs,
        int evidenceVersion)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(cleanup);
        ArgumentNullException.ThrowIfNull(transitionNames);
        ArgumentNullException.ThrowIfNull(transitionAtNs);
        Identity = identity;
        RequestedManifest = requestedManifest;
        EstablishedManifest = establishedManifest;
        State = state;
        Outcome = outcome;
        Root = root;
        Helper = helper;
        Exec = exec;
        Capture = capture;
        Cleanup = cleanup;
        Release = release;
        FirstStopCause = firstStopCause;
        TransitionNames = transitionNames;
        TransitionAtNs = transitionAtNs;
        EvidenceVersion = evidenceVersion;
    }

    public PreparationIdentity? Identity { get; }
    public RestrictedManifest? RequestedManifest { get; }
    public RestrictedManifest? EstablishedManifest { get; }
    public TransactionState State { get; }
    public ExecutionOutcome? Outcome { get; }
    public RootResult Root { get; }
    public HelperResult Helper { get; }
    public ExecDisposition Exec { get; }
    public CaptureResult? Capture { get; }
    public CleanupReport Cleanup { get; }
    public ReleaseDisposition Release { get; }
    public StopCauseKind? FirstStopCause { get; }
    public IReadOnlyList<string> TransitionNames { get; }
    public IReadOnlyList<long> TransitionAtNs { get; }
    public int EvidenceVersion { get; }
}
