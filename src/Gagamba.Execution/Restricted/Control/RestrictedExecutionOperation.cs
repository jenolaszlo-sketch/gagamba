using System.Collections.Immutable;
using System.Security.Cryptography;
using Gagamba.Execution.Restricted.Contracts;

namespace Gagamba.Execution.Restricted.Control;

/// <summary>Original owner monotonic time binding. These are comparisons, never caller
/// knobs. Clock implementation and C3 recovery validation remain separately gated.</summary>
public sealed class RestrictedDeadlines
{
    internal RestrictedDeadlines(long acceptedAtNs, long preparationExpiresAtNs, long setupDeadlineNs,
        long overallDeadlineNs, string clockId, Guid bootId, Guid epochId, ulong frequencyHz, Guid generationId)
    {
        if (acceptedAtNs <= 0 || preparationExpiresAtNs <= acceptedAtNs || setupDeadlineNs <= acceptedAtNs ||
            overallDeadlineNs <= setupDeadlineNs || frequencyHz == 0 || bootId == Guid.Empty ||
            epochId == Guid.Empty || generationId == Guid.Empty)
            throw new ArgumentException("Original clock identity and positive ordered deadlines are required.");
        AcceptedAtNs = acceptedAtNs; PreparationExpiresAtNs = preparationExpiresAtNs;
        SetupDeadlineNs = setupDeadlineNs; OverallDeadlineNs = overallDeadlineNs;
        ClockId = ControlValidation.Id(clockId, nameof(clockId)); BootId = bootId;
        EpochId = epochId; FrequencyHz = frequencyHz; GenerationId = generationId;
    }
    public long AcceptedAtNs { get; }
    public long PreparationExpiresAtNs { get; }
    public long SetupDeadlineNs { get; }
    public long OverallDeadlineNs { get; }
    public string ClockId { get; }
    public Guid BootId { get; }
    public Guid EpochId { get; }
    public ulong FrequencyHz { get; }
    public Guid GenerationId { get; }
}

/// <summary>Opaque, bounded owner receipt bytes with their exact digest. B1 defines no
/// receipt wire format, parser or successful production establishment mapping.</summary>
public sealed class RestrictedEstablishmentReceipt
{
    internal RestrictedEstablishmentReceipt(ushort version, ReadOnlySpan<byte> bytes)
    {
        if (version == 0 || bytes.Length is < 1 or > 32768)
            throw new ArgumentException("A versioned bounded receipt is required.");
        EvidenceVersion = version; Bytes = ImmutableArray.Create(bytes);
        Sha256 = ImmutableArray.Create(SHA256.HashData(bytes));
    }
    public ushort EvidenceVersion { get; }
    public ImmutableArray<byte> Bytes { get; }
    public ImmutableArray<byte> Sha256 { get; }
}

/// <summary>Provider-origin view tied to one operation instance. It cannot be publicly
/// constructed, deserialized, cloned or reconstructed from its passive reference.</summary>
public sealed class PreparedRestrictedExecution
{
    internal PreparedRestrictedExecution(object association, RestrictedOwnerPreparation source)
    {
        Association = association; Reference = source.Reference; Manifest = source.Manifest;
        Provider = source.Provider; Deadlines = source.Deadlines; Establishment = source.Establishment;
    }
    internal object Association { get; }
    public RestrictedPreparationReference Reference { get; }
    public RestrictedManifestView Manifest { get; }
    public RestrictedProviderDescriptor Provider { get; }
    public RestrictedDeadlines Deadlines { get; }
    public RestrictedEstablishmentReceipt Establishment { get; }
}

// Internal contract seam. The B3 composition binds it to the single
// authoritative owner transaction (real preparation, capability mint and
// owner-side release arbiter). Only descriptors, identities and decisions
// cross this seam; no capability, handle or bearer is transferred.
internal interface IRestrictedOperationOwner
{
    ValueTask<RestrictedOwnerPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs);
    // Owner-side release admission. The owner keeps the capability and the
    // sole RELEASE writer; the correlation is an admission binding only.
    ValueTask<RestrictedOwnerReleaseResult> ReleaseAsync(ImmutableArray<byte> approvalCorrelationSha256);
    ValueTask<RestrictedStopReply> StopAsync(RestrictedStopReason reason);
    ValueTask<RestrictedCleanup> DisposeAsync();
    ValueTask<RestrictedCaptureRead> ReadCaptureAsync(RestrictedCaptureStream stream, ulong offset, int count) =>
        ValueTask.FromResult(new RestrictedCaptureRead(RestrictedCaptureReadKind.Unavailable, []));
    // Cached, nonblocking facts only: never journal I/O or an authenticated
    // channel round trip while the facade's local ordering gate is held.
    RestrictedReleaseDecision? RetainedRelease { get; }
    bool IsLive(RestrictedPreparationReference reference);
}

/// <summary>Owner-origin release decision for one admitted call. The typed
/// evidence keeps persistent, delivered and ambiguous outcomes separate; a
/// delivered write is never proof of coordinator consumption or exec.</summary>
internal sealed record RestrictedOwnerReleaseResult(
    RestrictedReleaseKind Kind,
    RestrictedReleaseEvidence Evidence,
    RestrictedDelivery Delivery,
    RestrictedEvidenceState DurableCommit,
    bool? LiveExecutionExcluded,
    RestrictedRequestBinding Binding,
    RestrictedRefusalReason Reason,
    RestrictedOwnerExecutionFacts? Execution = null);

/// <summary>Trusted-native post-release facts. Absent axes stay null or
/// Unknown; fixture output digests are reported separately by the owner-side
/// observation path and never classify exec.</summary>
internal sealed record RestrictedOwnerExecutionFacts(
    RestrictedOutcome Outcome,
    RestrictedExecResult? Exec,
    RestrictedProcessResult? Root,
    RestrictedProcessResult? Helper,
    RestrictedDomainState Domain,
    long? ObservedAtNs, RestrictedCaptureEvidence? Capture = null);

internal sealed record RestrictedOwnerPreparation(RestrictedPreparationReference Reference,
    RestrictedManifestView Manifest, RestrictedProviderDescriptor Provider, RestrictedDeadlines Deadlines,
    RestrictedEstablishmentReceipt Establishment);

internal sealed class RestrictedOwnerPrepareResult
{
    internal RestrictedOwnerPrepareResult(RestrictedPrepareKind kind, RestrictedOwnerPreparation? preparation = null)
    {
        if (kind is not (RestrictedPrepareKind.Prepared or RestrictedPrepareKind.Failed or RestrictedPrepareKind.Unknown) ||
            (kind == RestrictedPrepareKind.Prepared) != (preparation is not null))
            throw new ArgumentException("Owner preparation result requires explicit evidence, failure or uncertainty.");
        Kind = kind; Preparation = preparation;
    }
    internal RestrictedPrepareKind Kind { get; }
    internal RestrictedOwnerPreparation? Preparation { get; }
}

/// <summary>One trusted operation, with a local stop latch independent of owner ACKs.
/// B1 has no public creation path and does not submit physical release requests.</summary>
public sealed class RestrictedExecutionOperation : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly object _association = new();
    private readonly IRestrictedOperationOwner _owner;
    private readonly RestrictedProviderDescriptor _provider;
    private Task<RestrictedPrepareResult>? _preparationTask;
    private RestrictedPreparationInputs? _inputs;
    private PreparedRestrictedExecution? _prepared;
    private RestrictedPreparationReference? _identity;
    private RestrictedStopReply? _stop;
    private Task _stopTask = Task.CompletedTask;
    private Task? _disposeTask;
    private bool _disposed;
    private bool _captureReadInFlight;
    private ulong _sequence;
    private RestrictedCleanup _cleanup = RestrictedCleanup.Unknown;
    // R1-F03 Option A admission state. The first call that passes every local
    // pre-retention gate binds the operation's approval correlation; a later
    // mismatch only refuses that call. No journal field carries this.
    private bool _hasAdmittedCorrelation;
    private ImmutableArray<byte> _admittedCorrelation;
    private bool _releaseSubmissionInFlight;
    private Task<RestrictedOwnerReleaseResult>? _releaseSubmission;
    private RestrictedReleaseDecision? _releaseDecision;
    private RestrictedOwnerExecutionFacts? _executionFacts;

    internal RestrictedExecutionOperation(RestrictedProviderDescriptor provider, IRestrictedOperationOwner owner)
    { _provider = provider; _owner = owner; }

    // Deterministic contract tests may await the submitted ACK task; this is not
    // a public notification protocol and never waits for a release winner.
    internal Task StopSubmission { get { lock (_gate) return _stopTask; } }

    // Deterministic contract tests may await the in-flight release submission's
    // cached outcome. It is never a caller-visible waiter or retry loop.
    internal Task? ReleaseSubmission { get { lock (_gate) return _releaseSubmission; } }

    public ValueTask<RestrictedPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs,
        CancellationToken waitCancellation = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        TaskCompletionSource<RestrictedPrepareResult>? completion = null;
        Task<RestrictedPrepareResult> task;
        lock (_gate)
        {
            if (_disposed) return ValueTask.FromResult(RefusedPrepare(RestrictedRefusalReason.Disposed));
            if (_stop is not null) return ValueTask.FromResult(CancelledPrepare());
            if (_preparationTask is not null)
            {
                if (!_inputs!.Matches(inputs))
                    return ValueTask.FromResult(RefusedPrepare(RestrictedRefusalReason.PreparationAlreadyStarted));
                task = _preparationTask;
            }
            else
            {
                _inputs = inputs;
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                task = _preparationTask = completion.Task;
            }
        }
        if (completion is not null) _ = CompletePreparationAsync(inputs, completion);
        return new(task.WaitAsync(waitCancellation));
    }

    private async Task CompletePreparationAsync(RestrictedPreparationInputs inputs,
        TaskCompletionSource<RestrictedPrepareResult> completion)
    {
        RestrictedPrepareResult result;
        try
        {
            var ownerReply = await _owner.PrepareAsync(inputs).ConfigureAwait(false);
            var source = ownerReply.Preparation;
            lock (_gate)
            {
                // This gate is the local mint/stop ordering point. A stop before
                // it prevents publishing a capability even if the owner finishes.
                if (_disposed) result = RefusedPrepare(RestrictedRefusalReason.Disposed);
                else if (_stop is not null) result = CancelledPrepare();
                else if (ownerReply.Kind != RestrictedPrepareKind.Prepared)
                    result = new(ownerReply.Kind, RestrictedRefusalReason.OwnerUnavailable);
                else if (source is null)
                    result = new(RestrictedPrepareKind.Unknown, RestrictedRefusalReason.OwnerUnavailable);
                else if (!ReferenceEquals(source.Provider, _provider) ||
                         source.Reference.GenerationId != source.Deadlines.GenerationId ||
                         !RestrictedManifestCodec.VerifySha256(source.Manifest, source.Reference.ManifestSha256.AsSpan()) ||
                         !_owner.IsLive(source.Reference))
                    result = RefusedPrepare(RestrictedRefusalReason.InvalidBinding);
                else
                {
                    _prepared = new(_association, source);
                    _identity = new(source.Reference.ExecutionId, source.Reference.IssuerId,
                        source.Reference.GenerationId, source.Reference.PreparationId, source.Reference.ManifestSha256.AsSpan());
                    _sequence++;
                    result = new(RestrictedPrepareKind.Prepared, RestrictedRefusalReason.None, _prepared);
                }
            }
        }
        catch (Exception)
        {
            // A lost prepare reply cannot be turned into a safe fresh attempt.
            // The original task is retained, and stop remains independently callable.
            lock (_gate)
                result = _stop is not null ? CancelledPrepare() :
                    new(RestrictedPrepareKind.Unknown, RestrictedRefusalReason.OwnerUnavailable);
        }
        completion.TrySetResult(result);
    }

    public ValueTask<RestrictedReleaseReply> RequestReleaseAsync(PreparedRestrictedExecution exactPreparation,
        ImmutableArray<byte> approvalCorrelationSha256 = default, CancellationToken waitCancellation = default)
    {
        ArgumentNullException.ThrowIfNull(exactPreparation);
        // No operation is cancelled by a wait token; only this caller's wait is.
        waitCancellation.ThrowIfCancellationRequested();
        Task<RestrictedOwnerReleaseResult>? submission = null;
        lock (_gate)
        {
            if (!ReferenceEquals(exactPreparation, _prepared) ||
                !ReferenceEquals(exactPreparation.Association, _association) ||
                !_identity!.Equals(exactPreparation.Reference) ||
                !ReferenceEquals(exactPreparation.Provider, _provider) ||
                !RestrictedManifestCodec.VerifySha256(exactPreparation.Manifest, _identity.ManifestSha256.AsSpan()))
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                    RestrictedRequestBinding.Mismatch, RestrictedRefusalReason.InvalidBinding));

            var validCorrelation = approvalCorrelationSha256.IsDefault || approvalCorrelationSha256.Length == 32;
            // Read-only retained observation. C4 facts (the operation's own
            // cached decision or the owner's cached retained view) survive
            // stop, disposal and invalid or mismatching caller correlation.
            var retained = _releaseDecision ?? _owner.RetainedRelease;
            if (retained is not null)
            {
                var original = retained.ApprovalCorrelationSha256;
                var binding = !validCorrelation ? RestrictedRequestBinding.InvalidArgument :
                    original.IsDefault == approvalCorrelationSha256.IsDefault &&
                    original.AsSpan().SequenceEqual(approvalCorrelationSha256.AsSpan())
                        ? RestrictedRequestBinding.Accepted : RestrictedRequestBinding.Mismatch;
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Duplicate,
                    binding, !validCorrelation ? RestrictedRefusalReason.InvalidCorrelation :
                        binding == RestrictedRequestBinding.Mismatch ? RestrictedRefusalReason.InvalidBinding :
                            RestrictedRefusalReason.None, retained));
            }
            if (!validCorrelation)
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                    RestrictedRequestBinding.InvalidArgument, RestrictedRefusalReason.InvalidCorrelation));
            // R1-F03 Option A: a pre-retention mismatch against the admitted
            // binding refuses only this call. The admitted correlation, the
            // capability, the deadlines and any retained decision state are
            // untouched, and no RELEASE can result from this call.
            if (_hasAdmittedCorrelation
                && !CorrelationEqual(_admittedCorrelation, approvalCorrelationSha256))
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                    RestrictedRequestBinding.Mismatch, RestrictedRefusalReason.InvalidBinding));
            var refusal = _disposed ? RestrictedRefusalReason.Disposed : _stop is not null
                ? RestrictedRefusalReason.StopRequested : !_owner.IsLive(_identity)
                    ? RestrictedRefusalReason.InvalidBinding : RestrictedRefusalReason.None;
            if (refusal != RestrictedRefusalReason.None)
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                    RestrictedRequestBinding.Unknown, refusal));
            if (_releaseSubmissionInFlight)
                // C4 concurrent loser: refused, never queued or retried, and
                // never a second physical attempt.
                return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                    RestrictedRequestBinding.Accepted, RestrictedRefusalReason.ReleaseInProgress));
            if (!_hasAdmittedCorrelation)
            {
                // The first fully admissible call atomically binds the
                // operation's approval correlation.
                _admittedCorrelation = approvalCorrelationSha256.IsDefault
                    ? default
                    : ImmutableArray.Create(approvalCorrelationSha256.AsSpan());
                _hasAdmittedCorrelation = true;
            }
            _releaseSubmissionInFlight = true;
            _sequence++;
            // Owner I/O never runs under the local ordering gate.
            submission = _releaseSubmission = SubmitReleaseAsync(approvalCorrelationSha256);
        }
        return new(AwaitReleaseReplyAsync(submission!, waitCancellation));
    }

    private static bool CorrelationEqual(ImmutableArray<byte> left, ImmutableArray<byte> right) =>
        left.IsDefault == right.IsDefault && (left.IsDefault || left.AsSpan().SequenceEqual(right.AsSpan()));

    private async Task<RestrictedReleaseReply> AwaitReleaseReplyAsync(
        Task<RestrictedOwnerReleaseResult> submission, CancellationToken waitCancellation)
    {
        var result = await submission.WaitAsync(waitCancellation).ConfigureAwait(false);
        RestrictedReleaseDecision? decision;
        lock (_gate)
        {
            decision = _releaseDecision;
        }
        if (result.Kind == RestrictedReleaseKind.Duplicate && decision is null)
            return new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                result.Binding, RestrictedRefusalReason.OwnerUnavailable);
        var carriesDecision = result.Kind is RestrictedReleaseKind.Duplicate or RestrictedReleaseKind.Released
            or RestrictedReleaseKind.Stopped or RestrictedReleaseKind.AmbiguousPersistence
            || result.Evidence == RestrictedReleaseEvidence.MayHaveExecuted;
        return new RestrictedReleaseReply(result.Kind, result.Binding, result.Reason,
            carriesDecision ? decision : null);
    }

    private async Task<RestrictedOwnerReleaseResult> SubmitReleaseAsync(ImmutableArray<byte> correlation)
    {
        RestrictedOwnerReleaseResult result;
        try
        {
            result = await _owner.ReleaseAsync(correlation).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A lost release reply is never a safe fresh attempt: Unknown is
            // recorded, no decision is invented and stop stays independently
            // callable.
            result = new RestrictedOwnerReleaseResult(RestrictedReleaseKind.Unknown,
                RestrictedReleaseEvidence.Unknown, RestrictedDelivery.Unknown, RestrictedEvidenceState.Unknown,
                null, RestrictedRequestBinding.Unknown, RestrictedRefusalReason.OwnerUnavailable);
        }
        lock (_gate)
        {
            _releaseSubmissionInFlight = false;
            _executionFacts = result.Execution ?? _executionFacts;
            if (result.Kind == RestrictedReleaseKind.Duplicate)
            {
                // The owner already holds the retained decision.
                _releaseDecision ??= _owner.RetainedRelease;
            }
            else if (result.Kind is RestrictedReleaseKind.Released or RestrictedReleaseKind.Stopped
                     or RestrictedReleaseKind.AmbiguousPersistence
                     || result.Evidence == RestrictedReleaseEvidence.MayHaveExecuted)
            {
                // First retained decision, including owner-side retained
                // refusals (MayHaveExecuted): C4 facts survive every later
                // call, stop and disposal, and never get overwritten.
                if (_releaseDecision is null)
                    _releaseDecision = BuildDecision(result);
            }
            _sequence++;
        }
        return result;
    }

    private RestrictedReleaseDecision BuildDecision(RestrictedOwnerReleaseResult result)
    {
        var kind = result.Kind == RestrictedReleaseKind.Unknown
            ? RestrictedReleaseKind.Refused
            : result.Kind;
        var evidence = result.DurableCommit == RestrictedEvidenceState.Confirmed
            ? RestrictedReleaseEvidence.Committed
            : result.Evidence == RestrictedReleaseEvidence.NotCommitted
                ? RestrictedReleaseEvidence.NotCommitted
                : RestrictedReleaseEvidence.MayHaveExecuted;
        var delivery = result.Delivery;
        var durableCommit = result.DurableCommit;
        if (kind == RestrictedReleaseKind.AmbiguousPersistence)
        {
            // Ambiguous persistence can never assert a committed or delivered write.
            evidence = RestrictedReleaseEvidence.MayHaveExecuted;
            delivery = RestrictedDelivery.NotAttempted;
            durableCommit = RestrictedEvidenceState.Unknown;
        }
        if (evidence == RestrictedReleaseEvidence.NotCommitted)
            delivery = RestrictedDelivery.NotAttempted;
        return new RestrictedReleaseDecision(kind, evidence, delivery, durableCommit,
            result.LiveExecutionExcluded, _admittedCorrelation, RestrictedEvidenceState.Unknown);
    }

    public ValueTask<RestrictedStopReply> RequestStopAsync(RestrictedStopReason reason,
        CancellationToken waitCancellation = default)
    {
        ControlValidation.Tag(reason);
        bool start;
        TaskCompletionSource? completion = null;
        RestrictedStopReply reply;
        lock (_gate)
        {
            start = _stop is null;
            if (start)
            {
                _stop = new(RestrictedStopSubmission.LocallyQueued, false, reason);
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _stopTask = completion.Task;
                _sequence++;
            }
            reply = start ? _stop! : _stop!.AsDuplicate();
        }
        if (start)
        {
            // Owner submission is never made while holding the mint/stop gate.
            _ = CompleteStopAsync(reason, completion!);
        }
        // Submission/latch occurs even if this caller no longer wants to wait.
        waitCancellation.ThrowIfCancellationRequested();
        return ValueTask.FromResult(reply);
    }

    private async Task CompleteStopAsync(RestrictedStopReason firstReason, TaskCompletionSource completion)
    {
        RestrictedStopReply reply;
        try
        {
            reply = await _owner.StopAsync(firstReason).ConfigureAwait(false);
            if (reply.FirstReason != firstReason || reply.Duplicate)
                throw new InvalidOperationException("Owner stop acknowledgement conflicts with first submission.");
        }
        catch (Exception)
        {
            reply = new(RestrictedStopSubmission.Unknown, false, firstReason);
        }
        lock (_gate) { _stop = reply; _sequence++; }
        completion.TrySetResult();
    }

    public ValueTask<RestrictedObservation> ObserveAsync(ulong afterSequence = 0,
        CancellationToken waitCancellation = default)
    {
        waitCancellation.ThrowIfCancellationRequested();
        lock (_gate)
        {
            // Latest local snapshot only. GapOrLoss stays explicit until a
            // bounded event stream exists (B4). Only axes the live owner
            // actually observed are filled; everything else stays unknown.
            var facts = _executionFacts;
            var provenance = _releaseDecision is null && facts is null
                ? RestrictedProvenance.Unknown
                : RestrictedProvenance.LiveOwner;
            return ValueTask.FromResult(new RestrictedObservation(_sequence, _identity, _cleanup, _stop,
                _releaseDecision, provenance, facts?.ObservedAtNs, gapOrLoss: true,
                facts?.Outcome, facts?.Exec, facts?.Root, facts?.Helper,
                facts?.Domain ?? RestrictedDomainState.Unknown, facts?.Capture));
        }
    }

    /// <summary>Reads at most 16 KiB from fixed owner custody. Cancellation ends only this wait.</summary>
    public ValueTask<RestrictedCaptureRead> ReadCaptureAsync(RestrictedCaptureStream stream, ulong offset, int count,
        CancellationToken waitCancellation = default)
    {
        ControlValidation.Tag(stream);
        if (count is < 0 or > 16384 || offset > 65536) throw new ArgumentOutOfRangeException(nameof(count));
        waitCancellation.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_disposed || _prepared is null || _captureReadInFlight)
                return ValueTask.FromResult(new RestrictedCaptureRead(RestrictedCaptureReadKind.Unavailable, []));
            _captureReadInFlight = true;
        }
        return new(ReadCaptureCoreAsync(stream, offset, count).WaitAsync(waitCancellation));
    }
    private async Task<RestrictedCaptureRead> ReadCaptureCoreAsync(RestrictedCaptureStream stream, ulong offset, int count)
    {
        try { return await _owner.ReadCaptureAsync(stream, offset, count).ConfigureAwait(false); }
        catch { return new(RestrictedCaptureReadKind.Unavailable, []); }
        finally { lock (_gate) _captureReadInFlight = false; }
    }

    public ValueTask DisposeAsync()
    {
        TaskCompletionSource? completion = null;
        Task task;
        lock (_gate)
        {
            if (_disposeTask is not null) return new(_disposeTask);
            _disposed = true;
            _sequence++;
            completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            task = _disposeTask = completion.Task;
        }
        _ = CompleteDisposalAsync(completion);
        return new(task);
    }

    private async Task CompleteDisposalAsync(TaskCompletionSource completion)
    {
        // Disposal consumes local authority and queues stop; returning from
        // IAsyncDisposable by itself is never evidence of successful cleanup.
        await RequestStopAsync(RestrictedStopReason.Disposal).ConfigureAwait(false);
        RestrictedCleanup cleanup;
        try { cleanup = await _owner.DisposeAsync().ConfigureAwait(false); }
        catch (Exception) { cleanup = RestrictedCleanup.Unknown; }
        lock (_gate) { _cleanup = cleanup; _sequence++; }
        completion.TrySetResult();
    }

    private static RestrictedPrepareResult RefusedPrepare(RestrictedRefusalReason reason) =>
        new(RestrictedPrepareKind.Refused, reason);
    private static RestrictedPrepareResult CancelledPrepare() =>
        new(RestrictedPrepareKind.CancelledBeforeRelease, RestrictedRefusalReason.StopRequested);
}
