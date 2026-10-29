using System.Collections.Immutable;
using Gagamba.Execution.Restricted.Contracts;

namespace Gagamba.Execution.Restricted.Control;

// Zero is always unknown/unobserved, never successful evidence.
public enum RestrictedPrepareKind { Unknown = 0, Prepared, Refused, CancelledBeforeRelease, Failed }
public enum RestrictedReleaseKind { Unknown = 0, Refused, Released, Duplicate, Stopped, AmbiguousPersistence }
public enum RestrictedRequestBinding { Unknown = 0, Accepted, Mismatch, InvalidArgument }
public enum RestrictedReleaseEvidence { Unknown = 0, NotCommitted, Committed, MayHaveExecuted }
public enum RestrictedDelivery { Unknown = 0, NotAttempted, Delivered, WouldBlock, BrokenPipe, Partial }
public enum RestrictedEvidenceState { Unknown = 0, Confirmed, Failed, Refused }
public enum RestrictedStopSubmission { Unknown = 0, LocallyQueued, RuntimeAccepted, Refused }
public enum RestrictedStopCompletion { Unknown = 0, NotObserved, Confirmed, Failed, Refused }
public enum RestrictedStopReason { CallerStop = 1, Deadline = 2, Disposal = 3, Invalidation = 4 }
public enum RestrictedOutcome { Unknown = 0, Unsupported, FailedToEstablish, CancelledBeforeRelease, Succeeded, TargetFailed, Stopped }
public enum RestrictedDomainState { Unknown = 0, Empty, Populated }
public enum RestrictedProcessKind { Unknown = 0, NotStarted, Exited, Signalled }
public enum RestrictedExecKind { Unknown = 0, NotAttempted, FailedBeforeExec, Confirmed }
public enum RestrictedProvenance { Unknown = 0, LiveOwner, DurableJournal, Recovery }
public enum RestrictedInspectionKind { Unavailable = 0, Observed, Refused }
public enum RestrictedReconciliationKind { Unknown = 0, Refused, Completed, Failed }
public enum RestrictedFinalDrain { Unknown = 0, Complete, Incomplete, Expired }

public sealed class RestrictedPrepareResult
{
    internal RestrictedPrepareResult(RestrictedPrepareKind kind, RestrictedRefusalReason reason,
        PreparedRestrictedExecution? preparation = null)
    {
        if ((kind == RestrictedPrepareKind.Prepared) != (preparation is not null))
            throw new ArgumentException("Only Prepared carries a preparation.");
        Kind = ControlValidation.Tag(kind); Reason = ControlValidation.Tag(reason); Preparation = preparation;
    }
    public RestrictedPrepareKind Kind { get; }
    public RestrictedRefusalReason Reason { get; }
    public PreparedRestrictedExecution? Preparation { get; }
}

/// <summary>The retained owner decision, independent of this caller's disposition.
/// Delivered is a full owner write, never proof of coordinator consumption or exec.</summary>
public sealed class RestrictedReleaseDecision
{
    internal RestrictedReleaseDecision(RestrictedReleaseKind kind, RestrictedReleaseEvidence evidence,
        RestrictedDelivery delivery, RestrictedEvidenceState durableCommit, bool? liveExecutionExcluded,
        ImmutableArray<byte> correlation, RestrictedEvidenceState correlationRetention)
    {
        if (kind is RestrictedReleaseKind.Duplicate or RestrictedReleaseKind.Unknown)
            throw new ArgumentException("A retained decision must have an original kind.");
        if (!correlation.IsDefault && correlation.Length != 32)
            throw new ArgumentException("Correlation must be absent or exactly 32 bytes.");
        if (kind == RestrictedReleaseKind.AmbiguousPersistence &&
            (evidence != RestrictedReleaseEvidence.MayHaveExecuted || delivery != RestrictedDelivery.NotAttempted ||
             durableCommit == RestrictedEvidenceState.Confirmed))
            throw new ArgumentException("Ambiguous persistence cannot assert committed or delivered.");
        if (durableCommit == RestrictedEvidenceState.Confirmed && evidence != RestrictedReleaseEvidence.Committed)
            throw new ArgumentException("Confirmed durable commit requires committed evidence.");
        if (evidence == RestrictedReleaseEvidence.NotCommitted && delivery != RestrictedDelivery.NotAttempted)
            throw new ArgumentException("Definite pre-intent refusal requires no delivery attempt.");
        if (correlation.IsDefault && correlationRetention == RestrictedEvidenceState.Confirmed)
            throw new ArgumentException("An absent correlation cannot be durably retained.");
        Kind = ControlValidation.Tag(kind); Evidence = ControlValidation.Tag(evidence);
        Delivery = ControlValidation.Tag(delivery); DurableCommit = ControlValidation.Tag(durableCommit);
        LiveExecutionExcluded = liveExecutionExcluded;
        ApprovalCorrelationSha256 = correlation.IsDefault ? default : ImmutableArray.Create(correlation.AsSpan());
        CorrelationRetention = ControlValidation.Tag(correlationRetention);
    }
    public RestrictedReleaseKind Kind { get; }
    public RestrictedReleaseEvidence Evidence { get; }
    public RestrictedDelivery Delivery { get; }
    public RestrictedEvidenceState DurableCommit { get; }
    public bool? LiveExecutionExcluded { get; }
    public ImmutableArray<byte> ApprovalCorrelationSha256 { get; }
    public RestrictedEvidenceState CorrelationRetention { get; }
}

public sealed class RestrictedReleaseReply
{
    internal RestrictedReleaseReply(RestrictedReleaseKind kind, RestrictedRequestBinding binding,
        RestrictedRefusalReason reason, RestrictedReleaseDecision? original = null)
    {
        if ((kind == RestrictedReleaseKind.Duplicate) != (original is not null))
            throw new ArgumentException("Only Duplicate carries a retained decision in B1.");
        Kind = ControlValidation.Tag(kind); Binding = ControlValidation.Tag(binding);
        Reason = ControlValidation.Tag(reason); OriginalDecision = original;
    }
    public RestrictedReleaseKind Kind { get; }
    public RestrictedRequestBinding Binding { get; }
    public RestrictedRefusalReason Reason { get; }
    public RestrictedReleaseDecision? OriginalDecision { get; }
}

public sealed class RestrictedStopReply
{
    internal RestrictedStopReply(RestrictedStopSubmission submission, bool duplicate, RestrictedStopReason firstReason,
        bool? releaseAlreadyCommitted = null, long? acceptedAtNs = null, long? deadlineNs = null,
        RestrictedEvidenceState durableStop = RestrictedEvidenceState.Unknown)
    {
        if (acceptedAtNs is <= 0 || deadlineNs is <= 0 || (deadlineNs.HasValue &&
            (!acceptedAtNs.HasValue || deadlineNs.Value <= acceptedAtNs.Value)))
            throw new ArgumentException("Known stop times must be positive and ordered.");
        if (submission != RestrictedStopSubmission.RuntimeAccepted &&
            (acceptedAtNs.HasValue || deadlineNs.HasValue || durableStop == RestrictedEvidenceState.Confirmed))
            throw new ArgumentException("Local submission is not owner acceptance.");
        if (durableStop == RestrictedEvidenceState.Confirmed && (!acceptedAtNs.HasValue || !deadlineNs.HasValue))
            throw new ArgumentException("Durable stop requires its original acceptance and deadline.");
        Submission = ControlValidation.Tag(submission); Duplicate = duplicate;
        FirstReason = ControlValidation.Tag(firstReason); ReleaseAlreadyCommitted = releaseAlreadyCommitted;
        AcceptedAtNs = acceptedAtNs; DeadlineNs = deadlineNs; DurableStop = ControlValidation.Tag(durableStop);
    }
    internal RestrictedStopReply AsDuplicate() => new(Submission, true, FirstReason,
        ReleaseAlreadyCommitted, AcceptedAtNs, DeadlineNs, DurableStop);
    public RestrictedStopSubmission Submission { get; }
    public bool Duplicate { get; }
    public RestrictedStopReason FirstReason { get; }
    public bool? ReleaseAlreadyCommitted { get; }
    public long? AcceptedAtNs { get; }
    public long? DeadlineNs { get; }
    public RestrictedEvidenceState DurableStop { get; }
}

public sealed class RestrictedCleanup
{
    internal static RestrictedCleanup Unknown { get; } = new(RestrictedEvidenceState.Unknown,
        RestrictedEvidenceState.Unknown, RestrictedEvidenceState.Unknown, RestrictedEvidenceState.Unknown);
    internal RestrictedCleanup(RestrictedEvidenceState domainEmpty, RestrictedEvidenceState reaped,
        RestrictedEvidenceState stagingRemoved, RestrictedEvidenceState journalFinalized)
    {
        DomainEmpty = ControlValidation.Tag(domainEmpty); Reaped = ControlValidation.Tag(reaped);
        StagingRemoved = ControlValidation.Tag(stagingRemoved); JournalFinalized = ControlValidation.Tag(journalFinalized);
    }
    public RestrictedEvidenceState DomainEmpty { get; }
    public RestrictedEvidenceState Reaped { get; }
    public RestrictedEvidenceState StagingRemoved { get; }
    public RestrictedEvidenceState JournalFinalized { get; }
    public RestrictedEvidenceState State =>
        new[] { DomainEmpty, Reaped, StagingRemoved, JournalFinalized }.Contains(RestrictedEvidenceState.Failed)
            ? RestrictedEvidenceState.Failed
            : DomainEmpty == RestrictedEvidenceState.Confirmed && Reaped == RestrictedEvidenceState.Confirmed &&
              StagingRemoved == RestrictedEvidenceState.Confirmed && JournalFinalized == RestrictedEvidenceState.Confirmed
                ? RestrictedEvidenceState.Confirmed : RestrictedEvidenceState.Unknown;
}

public sealed class RestrictedProcessResult
{
    internal RestrictedProcessResult(RestrictedProcessKind kind, int? exitCode = null, int? signal = null,
        bool? coreDumped = null, int? rawWaitStatus = null)
    {
        if ((kind == RestrictedProcessKind.Exited) != exitCode.HasValue ||
            (kind == RestrictedProcessKind.Signalled) != (signal.HasValue && coreDumped.HasValue) ||
            (kind is RestrictedProcessKind.Exited or RestrictedProcessKind.Signalled) != rawWaitStatus.HasValue ||
            (kind != RestrictedProcessKind.Signalled && (signal.HasValue || coreDumped.HasValue)) || signal is <= 0)
            throw new ArgumentException("Process payload must match its tag.");
        Kind = ControlValidation.Tag(kind); ExitCode = exitCode; Signal = signal;
        CoreDumped = coreDumped; RawWaitStatus = rawWaitStatus;
    }
    public RestrictedProcessKind Kind { get; }
    public int? ExitCode { get; }
    public int? Signal { get; }
    public bool? CoreDumped { get; }
    public int? RawWaitStatus { get; }
}

public sealed class RestrictedExecResult
{
    internal RestrictedExecResult(RestrictedExecKind kind, int? errno = null, string? confirmationBasis = null)
    {
        if ((kind == RestrictedExecKind.FailedBeforeExec) != errno.HasValue || errno is <= 0 ||
            (kind == RestrictedExecKind.Confirmed) != (confirmationBasis is not null))
            throw new ArgumentException("Exec payload must match its tag.");
        Kind = ControlValidation.Tag(kind); Errno = errno;
        ConfirmationBasis = confirmationBasis is null ? null : ControlValidation.Id(confirmationBasis, nameof(confirmationBasis));
    }
    public RestrictedExecKind Kind { get; }
    public int? Errno { get; }
    public string? ConfirmationBasis { get; }
}

public sealed class RestrictedStreamCapture
{
    internal RestrictedStreamCapture(ulong storedBytes, ulong observedBytes, bool overflow, bool readFailure,
        bool truncated, bool eof, bool complete)
    {
        if (storedBytes > observedBytes || (complete && (!eof || overflow || readFailure || truncated)))
            throw new ArgumentException("Inconsistent capture facts.");
        StoredBytes = storedBytes; ObservedBytes = observedBytes; Overflow = overflow;
        ReadFailure = readFailure; Truncated = truncated; Eof = eof; Complete = complete;
    }
    public ulong StoredBytes { get; }
    public ulong ObservedBytes { get; }
    public bool Overflow { get; }
    public bool ReadFailure { get; }
    public bool Truncated { get; }
    public bool Eof { get; }
    public bool Complete { get; }
}

public sealed class RestrictedCaptureEvidence
{
    internal RestrictedCaptureEvidence(RestrictedStreamCapture stdout, RestrictedStreamCapture stderr,
        ulong storedLimit, ulong observedLimit, RestrictedFinalDrain finalDrain)
    {
        ArgumentNullException.ThrowIfNull(stdout); ArgumentNullException.ThrowIfNull(stderr);
        StoredBytes = checked(stdout.StoredBytes + stderr.StoredBytes);
        ObservedBytes = checked(stdout.ObservedBytes + stderr.ObservedBytes);
        if (StoredBytes > storedLimit || ObservedBytes > observedLimit)
            throw new ArgumentException("Capture exceeds retained quotas.");
        Stdout = stdout; Stderr = stderr; FinalDrain = ControlValidation.Tag(finalDrain);
    }
    public RestrictedStreamCapture Stdout { get; }
    public RestrictedStreamCapture Stderr { get; }
    public ulong StoredBytes { get; }
    public ulong ObservedBytes { get; }
    public RestrictedFinalDrain FinalDrain { get; }
}

/// <summary>Immutable snapshot. Absence of an axis means unobserved, not zero or success.
/// B1 emits only an explicitly unknown snapshot; B3/B4 must review receipt mappings.</summary>
public sealed class RestrictedObservation
{
    internal RestrictedObservation(ulong sequence, RestrictedPreparationReference? reference,
        RestrictedCleanup cleanup, RestrictedStopReply? stop)
    { Sequence = sequence; Reference = reference; Cleanup = cleanup; Stop = stop; }
    public ushort EvidenceVersion => 1;
    public ulong Sequence { get; }
    public RestrictedPreparationReference? Reference { get; }
    public RestrictedProvenance Provenance => RestrictedProvenance.Unknown;
    public long? ObservedAtNs => null;
    public bool GapOrLoss => true;
    public RestrictedReleaseDecision? Release => null;
    public RestrictedStopReply? Stop { get; }
    public RestrictedStopCompletion StopCompletion => RestrictedStopCompletion.Unknown;
    public RestrictedOutcome? Outcome => null;
    public RestrictedExecResult? Exec => null;
    public RestrictedProcessResult? Root => null;
    public RestrictedProcessResult? Helper => null;
    public RestrictedDomainState Domain => RestrictedDomainState.Unknown;
    public RestrictedCaptureEvidence? Capture => null;
    public RestrictedCleanup Cleanup { get; }
}

public sealed class RestrictedInspection
{
    internal RestrictedInspection(RestrictedInspectionKind kind, RestrictedObservation? observation)
    {
        if ((kind == RestrictedInspectionKind.Observed) != (observation is not null))
            throw new ArgumentException("Only Observed carries an observation.");
        Kind = ControlValidation.Tag(kind); Observation = observation;
    }
    public RestrictedInspectionKind Kind { get; }
    public RestrictedObservation? Observation { get; }
}

public sealed class RestrictedReconciliation
{
    internal RestrictedReconciliation(RestrictedReconciliationKind kind, RestrictedRefusalReason reason,
        RestrictedCleanup cleanup)
    { Kind = ControlValidation.Tag(kind); Reason = ControlValidation.Tag(reason); Cleanup = cleanup; }
    public RestrictedReconciliationKind Kind { get; }
    public RestrictedRefusalReason Reason { get; }
    public RestrictedCleanup Cleanup { get; }
}
