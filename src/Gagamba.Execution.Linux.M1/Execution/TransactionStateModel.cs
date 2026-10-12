// WP01 pure transition model: legal state movement, single-use release
// arbitration against fake dependencies, refusal vocabulary and terminal
// publication. No filesystem copy, P/Invoke, process launch, real release
// transport or provider registration. Outcome semantics follow plan
// Sections 6/8/13: a refused transition never mutates model state, a stop
// that wins forbids release, knowledge never regresses once recorded, and
// Unknown is never reconstructed.
using System.Collections.Immutable;
using Gagamba.Execution.Restricted;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Linux.M1.Tests")]

namespace Gagamba.Execution.Linux.M1.Execution;

/// <summary>Monotonic clock dependency. WP01 uses a manually advanced fake;
/// production provides CLOCK_MONOTONIC nanoseconds.</summary>
internal interface ITransactionClock
{
    long NowNanoseconds { get; }
}

/// <summary>Durable journal dependency. WP01 uses an in-memory fake that
/// records every append for ordering assertions.</summary>
internal interface ITransactionJournal
{
    void Append(string kind, string detail, long atNs);

    IReadOnlyList<JournalEntry> Entries { get; }
}

/// <summary>One journal record: kind, detail and monotonic timestamp.</summary>
internal sealed record JournalEntry
{
    public JournalEntry(string kind, string detail, long atNs)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        ArgumentNullException.ThrowIfNull(detail);
        Kind = kind;
        Detail = detail;
        AtNs = atNs;
    }

    public string Kind { get; }
    public string Detail { get; }
    public long AtNs { get; }
}

/// <summary>Single release-write outcome. Exactly one write is attempted;
/// every non-delivery requests stop without retry.</summary>
internal enum ReleaseSendResult : byte
{
    Delivered = 0,
    WouldBlock = 1,
    BrokenPipe = 2,
    Partial = 3,
}

/// <summary>Release transport dependency. WP01 uses a programmed fake; the
/// frame shape here (32-byte manifest digest) is a WP01-only test shape,
/// not the Section 8 128-byte production layout.</summary>
internal interface IReleaseTransport
{
    ReleaseSendResult TrySend(ReadOnlyMemory<byte> frame);
}

/// <summary>Owned prerequisites, one boolean per Section 6 precondition.
/// All must hold for <see cref="TransactionStateModel.MarkOwned"/>.</summary>
internal sealed record OwnedPrerequisites(bool DrainsActive, bool StatusReaderActive, bool ReadbacksAccepted, bool RecoveryAcknowledged)
{
    internal static OwnedPrerequisites Satisfied() => new(true, true, true, true);
}

/// <summary>Result of one transition attempt. A refusal carries a typed
/// <see cref="InvariantViolation"/> and never mutates model state.</summary>
internal readonly record struct TransitionAttempt(bool Applied, InvariantViolation Violation, string Detail)
{
    internal static TransitionAttempt Ok(string detail) => new(true, InvariantViolation.None, detail);

    internal static TransitionAttempt Refused(InvariantViolation violation, string detail) => new(false, violation, detail);
}

/// <summary>Pure transaction state model over an injected store, fake clock,
/// fake journal and fake release transport. All state movement in Sections 6
/// and 8 that WP01 can express without native mechanisms lives here.</summary>
internal sealed class TransactionStateModel
{
    private const int MaxLaterStopCauses = 8;
    private const int EvidenceVersion = 1;

    private readonly Preparation.PreparedTransaction.PreparationStore _store;
    private readonly ITransactionClock _clock;
    private readonly ITransactionJournal _journal;
    private readonly IReleaseTransport _transport;
    private readonly List<string> _transitionNames = new();
    private readonly List<long> _transitionAtNs = new();
    private readonly List<StopCauseKind> _laterStopCauses = new();
    private bool _execObserved;
    private bool _rootObserved;
    private bool _helperObserved;
    private bool _captureObserved;
    private bool _domainObserved;
    private PreparationIdentity? _lastIdentity;
    private RestrictedManifest? _lastManifest;
    private bool _releaseDecisionRetained;
    private PreparationIdentity? _releaseIdentity;
    private byte[]? _releaseDigest;
    private string _releaseDecisionDetail = string.Empty;

    public TransactionStateModel(
        Preparation.PreparedTransaction.PreparationStore store,
        ITransactionClock clock,
        ITransactionJournal journal,
        IReleaseTransport transport)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        State = TransactionState.Preparing;
        Release = ReleaseDisposition.NotCommitted;
        Exec = ExecDisposition.NotAttempted;
        ExecErrno = 0;
        Root = new RootResult(RootResultKind.Unknown, 0, 0, false, 0);
        Helper = new HelperResult(HelperResultKind.Unknown, 0, 0, false, 0);
    }

    public TransactionState State { get; private set; }

    public ReleaseDisposition Release { get; private set; }

    public ExecDisposition Exec { get; private set; }

    public int ExecErrno { get; private set; }

    public RootResult Root { get; private set; }

    public HelperResult Helper { get; private set; }

    public CaptureResult? Capture { get; private set; }

    public bool? DomainEmpty { get; private set; }

    public ExecutionOutcome? TerminalOutcome { get; private set; }

    public TransactionState? TerminalState { get; private set; }

    public CleanupReport? TerminalCleanup { get; private set; }

    public StopCauseKind? FirstStopCause { get; private set; }

    public IReadOnlyList<StopCauseKind> LaterStopCauses => _laterStopCauses;

    public TransactionObservation? TerminalObservation { get; private set; }

    public Preparation.PreparedTransaction? CurrentCapability { get; private set; }

    public bool TerminalPublished { get; private set; }

    public TransitionAttempt PublishPrepared(Preparation.PreparedTransaction capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (State != TransactionState.Preparing)
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "PublishPrepared requires Preparing.");
        }

        if (!_store.TryLookup(capability.Identity.PreparationId, out var live)
            || !ReferenceEquals(live, capability))
        {
            return TransitionAttempt.Refused(InvariantViolation.Foreign, "Capability is not a live store issuance.");
        }

        CurrentCapability = capability;
        _lastIdentity = capability.Identity;
        _lastManifest = capability.Manifest;
        State = TransactionState.Prepared;
        _journal.Append("prepared", capability.Identity.PreparationId.ToString(), _clock.NowNanoseconds);
        RecordTransition("PublishPrepared");
        return TransitionAttempt.Ok("Prepared.");
    }

    public TransitionAttempt MarkOwned(OwnedPrerequisites prerequisites)
    {
        ArgumentNullException.ThrowIfNull(prerequisites);
        if (State != TransactionState.Prepared)
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "MarkOwned requires Prepared.");
        }

        if (CurrentCapability is null
            || !_store.TryLookup(CurrentCapability.Identity.PreparationId, out var live)
            || !ReferenceEquals(live, CurrentCapability))
        {
            return TransitionAttempt.Refused(InvariantViolation.Foreign, "Capability is not a live store issuance.");
        }

        if (!prerequisites.DrainsActive || !prerequisites.StatusReaderActive
            || !prerequisites.ReadbacksAccepted || !prerequisites.RecoveryAcknowledged)
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Owned prerequisites are not all met.");
        }

        State = TransactionState.Owned;
        _journal.Append("owned", CurrentCapability.Identity.PreparationId.ToString(), _clock.NowNanoseconds);
        RecordTransition("MarkOwned");
        return TransitionAttempt.Ok("Owned.");
    }

    public TransitionAttempt TryCommitRelease(PreparationIdentity identity, byte[] manifestDigest)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(manifestDigest);
        // G1-R2: the committed release decision is retained independently
        // of current state and live capability. An identical retry in any
        // post-commit state returns the original decision with no further
        // transport, journal or state action; a conflicting identity
        // cannot cause another decision.
        if (_releaseDecisionRetained)
        {
            // Final G1-R2 identity correction: record equality on
            // PreparationIdentity compares the digest backing store, so a
            // value-equivalent reconstructed identity must be recognized
            // with the same structural semantics as initial authorization.
            if (_releaseIdentity is not null
                && IdentityValuesEqual(_releaseIdentity, identity)
                && _releaseDigest is not null
                && _releaseDigest.SequenceEqual(manifestDigest))
            {
                return TransitionAttempt.Ok(_releaseDecisionDetail);
            }

            return TransitionAttempt.Refused(InvariantViolation.Foreign, "Conflicting authorization cannot cause another decision.");
        }

        if (State == TransactionState.Stopping)
        {
            return TransitionAttempt.Refused(InvariantViolation.StoppedFirst, "A stop already won before release.");
        }

        if (State != TransactionState.Owned)
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "TryCommitRelease requires Owned.");
        }

        if (CurrentCapability is null || !AuthorizesIdentity(identity))
        {
            return TransitionAttempt.Refused(InvariantViolation.Foreign, "Authorization identity does not match the prepared capability.");
        }

        if (!manifestDigest.SequenceEqual(ManifestCodec.ComputeDigestBytes(CurrentCapability.Manifest)))
        {
            return TransitionAttempt.Refused(InvariantViolation.Foreign, "Authorization digest does not match the prepared manifest.");
        }

        if (CurrentCapability.ExpiresAtNs <= _clock.NowNanoseconds)
        {
            return TransitionAttempt.Refused(InvariantViolation.Expired, "Preparation expired before release.");
        }

        _journal.Append("release-intent", CurrentCapability.Identity.PreparationId.ToString(), _clock.NowNanoseconds);

        // Gate recheck after the durable write: a stop (including a
        // reentrant stop during the journal append itself) that won first
        // forbids release. Delivery stays unknown, so the disposition is
        // conservatively MayHaveExecuted.
        if (State != TransactionState.Owned)
        {
            Release = ReleaseDisposition.MayHaveExecuted;
            RecordTransition("CommitPreemptedByStop");
            return TransitionAttempt.Refused(InvariantViolation.StoppedFirst, "Stop won during the intent write.");
        }

        if (!_store.TryConsume(CurrentCapability, out var consumeViolation))
        {
            return TransitionAttempt.Refused(consumeViolation, "Capability could not be consumed.");
        }

        State = TransactionState.ReleaseCommitted;
        Release = ReleaseDisposition.Committed;
        RecordTransition("ReleaseCommitted");
        var send = _transport.TrySend(CurrentCapability.Identity.ManifestDigest.AsMemory());
        _journal.Append("release-delivered", send.ToString(), _clock.NowNanoseconds);
        if (send == ReleaseSendResult.Delivered)
        {
            RecordTransition("ReleaseDelivered");
            RetainReleaseDecision(CurrentCapability.Identity, manifestDigest, "Release committed and delivered.");
            return TransitionAttempt.Ok("Release committed and delivered.");
        }

        State = TransactionState.Stopping;
        Release = ReleaseDisposition.MayHaveExecuted;
        Root = new RootResult(RootResultKind.NotStarted, 0, 0, false, 0);
        RecordStopCause(StopCauseKind.TransportFailure);
        _journal.Append("release-failed", send.ToString(), _clock.NowNanoseconds);
        RecordTransition("ReleaseSendFailed");
        RetainReleaseDecision(CurrentCapability.Identity, manifestDigest, "Release write failed; stopping with MayHaveExecuted.");
        return TransitionAttempt.Ok("Release write failed; stopping with MayHaveExecuted.");
    }

    public TransitionAttempt AcceptStop(StopCauseKind cause)
    {
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        if (FirstStopCause is null)
        {
            FirstStopCause = cause;
            State = TransactionState.Stopping;
            if (CurrentCapability is not null)
            {
                _store.Invalidate(CurrentCapability);
                CurrentCapability = null;
            }

            _journal.Append("stop", cause.ToString(), _clock.NowNanoseconds);
            RecordTransition("AcceptStop");
        }
        else if (_laterStopCauses.Count < MaxLaterStopCauses)
        {
            _laterStopCauses.Add(cause);
            _journal.Append("stop-later", cause.ToString(), _clock.NowNanoseconds);
            RecordTransition("AcceptStopLater");
        }
        else
        {
            RecordTransition("AcceptStopLaterDropped");
        }

        return TransitionAttempt.Ok("Stop accepted.");
    }

    public TransitionAttempt ObserveExec(ExecDisposition disposition, int errno = 0)
    {
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        if (Release == ReleaseDisposition.NotCommitted
            || State is not (TransactionState.ReleaseCommitted or TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Exec cannot be observed in this state.");
        }

        if (_execObserved)
        {
            if (Exec == disposition && ExecErrno == errno)
            {
                return TransitionAttempt.Ok("Exec already recorded; duplicate is idempotent.");
            }

            return TransitionAttempt.Refused(InvariantViolation.Conflict, "Conflicting exec observation cannot replace recorded knowledge.");
        }

        Exec = disposition;
        ExecErrno = errno;
        _execObserved = true;
        if (disposition == ExecDisposition.Confirmed && State == TransactionState.ReleaseCommitted)
        {
            State = TransactionState.Running;
        }

        RecordTransition("ObserveExec");
        return TransitionAttempt.Ok("Exec recorded.");
    }

    public TransitionAttempt ObserveRoot(RootResult root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        if (Release == ReleaseDisposition.NotCommitted
            || State is not (TransactionState.ReleaseCommitted or TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Root cannot be observed in this state.");
        }

        if (_rootObserved)
        {
            if (Root.Equals(root))
            {
                return TransitionAttempt.Ok("Root already recorded; duplicate is idempotent.");
            }

            return TransitionAttempt.Refused(InvariantViolation.Conflict, "Conflicting root observation cannot replace recorded knowledge.");
        }

        Root = root;
        _rootObserved = true;
        RecordTransition("ObserveRoot");
        return TransitionAttempt.Ok("Root recorded.");
    }

    public TransitionAttempt ObserveHelper(HelperResult helper)
    {
        ArgumentNullException.ThrowIfNull(helper);
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        // Helper observations exist independently of target execution, so
        // held and pre-release teardown states admit them without any
        // release having occurred.
        if (State is not (TransactionState.Owned or TransactionState.ReleaseCommitted or TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Helper cannot be observed in this state.");
        }

        if (_helperObserved)
        {
            if (Helper.Equals(helper))
            {
                return TransitionAttempt.Ok("Helper already recorded; duplicate is idempotent.");
            }

            return TransitionAttempt.Refused(InvariantViolation.Conflict, "Conflicting helper observation cannot replace recorded knowledge.");
        }

        Helper = helper;
        _helperObserved = true;
        RecordTransition("ObserveHelper");
        return TransitionAttempt.Ok("Helper recorded.");
    }

    public TransitionAttempt ObserveCaptureTerminal(CaptureResult capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        // Capture termination can legitimately arrive during held and
        // pre-release teardown; it never implies target release.
        if (State is not (TransactionState.Owned or TransactionState.ReleaseCommitted or TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Capture cannot be observed in this state.");
        }

        if (_captureObserved)
        {
            if (Capture is not null && Capture.Equals(capture))
            {
                return TransitionAttempt.Ok("Capture already recorded; duplicate is idempotent.");
            }

            return TransitionAttempt.Refused(InvariantViolation.Conflict, "Conflicting capture observation cannot replace recorded knowledge.");
        }

        Capture = capture;
        _captureObserved = true;
        RecordTransition("ObserveCaptureTerminal");
        return TransitionAttempt.Ok("Capture recorded.");
    }

    public TransitionAttempt ObserveDomainEmpty(bool empty)
    {
        if (IsTerminal(State))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        if (State is not (TransactionState.Owned or TransactionState.ReleaseCommitted or TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Domain emptiness cannot be observed in this state.");
        }

        if (_domainObserved)
        {
            if (DomainEmpty == empty)
            {
                return TransitionAttempt.Ok("Domain observation already recorded; duplicate is idempotent.");
            }

            // G1-R1: domain emptiness is a progressive lifecycle observation,
            // not a single-assignment result. A populated domain may later
            // become empty; an established empty domain cannot regress.
            if (empty)
            {
                DomainEmpty = true;
                RecordTransition("ObserveDomainEmpty");
                return TransitionAttempt.Ok("Domain became empty; progression accepted.");
            }

            return TransitionAttempt.Refused(InvariantViolation.Conflict, "Established empty domain cannot regress to populated.");
        }

        DomainEmpty = empty;
        _domainObserved = true;
        RecordTransition("ObserveDomainEmpty");
        return TransitionAttempt.Ok("Domain observation recorded.");
    }

    public TransitionAttempt PublishTerminal()
    {
        if (TerminalPublished)
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "Terminal observation is immutable.");
        }

        if (State is not (TransactionState.Running or TransactionState.Stopping))
        {
            return TransitionAttempt.Refused(InvariantViolation.InvalidStateOrder, "PublishTerminal requires Running or Stopping.");
        }

        TransactionState terminal;
        ExecutionOutcome outcome;
        var root = Root;
        if (Release == ReleaseDisposition.NotCommitted)
        {
            root = new RootResult(RootResultKind.NotStarted, 0, 0, false, 0);
            Root = root;
            if (FirstStopCause is StopCauseKind.CallerStop or StopCauseKind.CallerLoss)
            {
                terminal = TransactionState.Cancelled;
                outcome = ExecutionOutcome.CancelledBeforeRelease;
            }
            else
            {
                terminal = TransactionState.Failed;
                outcome = ExecutionOutcome.FailedToEstablish;
            }
        }
        else if (Exec == ExecDisposition.FailedBeforeExec)
        {
            root = new RootResult(RootResultKind.NotStarted, 0, 0, false, 0);
            Root = root;
            terminal = TransactionState.Failed;
            outcome = ExecutionOutcome.FailedToEstablish;
        }
        else if (Exec == ExecDisposition.Confirmed
            && root.Kind is RootResultKind.Exited or RootResultKind.Signalled
            && DomainEmpty == true
            && Capture is { Complete: true })
        {
            // An accepted stop forbids aggregate success even when the root
            // later exits cleanly; the known root result is preserved while
            // the outcome records the stop.
            if (FirstStopCause is not null)
            {
                terminal = TransactionState.Failed;
                outcome = ExecutionOutcome.Stopped;
            }
            else if (root.Kind == RootResultKind.Exited && root.ExitCode == 0)
            {
                terminal = TransactionState.Completed;
                outcome = ExecutionOutcome.Succeeded;
            }
            else if (root.Kind == RootResultKind.Exited)
            {
                terminal = TransactionState.Completed;
                outcome = ExecutionOutcome.TargetFailed;
            }
            else
            {
                terminal = TransactionState.Completed;
                outcome = ExecutionOutcome.TargetFailed;
            }
        }
        else
        {
            terminal = TransactionState.Unknown;
            outcome = ExecutionOutcome.Unknown;
        }

        var cleanup = new CleanupReport(CleanupState.Unknown, DomainEmpty, null, null, null);
        TerminalState = terminal;
        TerminalOutcome = outcome;
        TerminalCleanup = cleanup;
        TerminalPublished = true;
        State = terminal;
        _journal.Append("terminal", $"{terminal}/{outcome}", _clock.NowNanoseconds);
        RecordTransition("PublishTerminal");
        TerminalObservation = new TransactionObservation(
            _lastIdentity,
            _lastManifest,
            EstablishedManifest.Absent,
            terminal,
            outcome,
            root,
            Helper,
            Exec,
            ExecErrno,
            Capture,
            cleanup,
            Release,
            FirstStopCause,
            _laterStopCauses.ToArray(),
            _transitionNames.ToArray(),
            _transitionAtNs.ToArray(),
            EvidenceVersion);
        return TransitionAttempt.Ok("Terminal published.");
    }

    private bool AuthorizesIdentity(PreparationIdentity identity) =>
        CurrentCapability is not null && IdentityValuesEqual(CurrentCapability.Identity, identity);

    // Shared structural authorization semantics for initial authorization
    // and retained-decision retries: UUID fields by value, digest contents
    // byte-for-byte, never ImmutableArray backing-store identity. Store
    // consumption still requires reference identity on the capability.
    private static bool IdentityValuesEqual(PreparationIdentity expected, PreparationIdentity presented) =>
        presented.ExecutionId.Equals(expected.ExecutionId)
        && presented.IssuerId.Equals(expected.IssuerId)
        && presented.GenerationId.Equals(expected.GenerationId)
        && presented.PreparationId.Equals(expected.PreparationId)
        && presented.ManifestDigest.SequenceEqual(expected.ManifestDigest);

    private void RetainReleaseDecision(PreparationIdentity identity, byte[] manifestDigest, string detail)
    {
        _releaseDecisionRetained = true;
        _releaseIdentity = identity;
        _releaseDigest = (byte[])manifestDigest.Clone();
        _releaseDecisionDetail = detail;
    }

    private static bool IsTerminal(TransactionState state) =>
        state is TransactionState.Completed or TransactionState.Failed or TransactionState.Cancelled or TransactionState.Unknown;

    private void RecordStopCause(StopCauseKind cause)
    {
        if (FirstStopCause is null)
        {
            FirstStopCause = cause;
        }
        else if (_laterStopCauses.Count < MaxLaterStopCauses)
        {
            _laterStopCauses.Add(cause);
        }
    }

    private void RecordTransition(string name)
    {
        _transitionNames.Add(name);
        _transitionAtNs.Add(_clock.NowNanoseconds);
    }
}
