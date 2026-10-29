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

// Internal contract seam only. There is no production implementation or registration.
// B3 must bind it to the authenticated owner/store, qualify source/receipt consistency,
// and recheck identity, original cutoffs and capability state at the native arbiter.
internal interface IRestrictedOperationOwner
{
    ValueTask<RestrictedOwnerPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs);
    ValueTask<RestrictedStopReply> StopAsync(RestrictedStopReason reason);
    ValueTask<RestrictedCleanup> DisposeAsync();
    // Cached, nonblocking facts only: never journal I/O or an authenticated
    // channel round trip while the facade's local ordering gate is held.
    RestrictedReleaseDecision? RetainedRelease { get; }
    bool IsLive(RestrictedPreparationReference reference);
}

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
    private ulong _sequence;
    private RestrictedCleanup _cleanup = RestrictedCleanup.Unknown;

    internal RestrictedExecutionOperation(RestrictedProviderDescriptor provider, IRestrictedOperationOwner owner)
    { _provider = provider; _owner = owner; }

    // Deterministic contract tests may await the submitted ACK task; this is not
    // a public notification protocol and never waits for a release winner.
    internal Task StopSubmission { get { lock (_gate) return _stopTask; } }

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
        // No operation is cancelled by a wait token. This method does no owner I/O.
        waitCancellation.ThrowIfCancellationRequested();
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
            // Read-only retained observation. B1 cannot create this owner decision.
            // C4 facts survive stop, disposal, invalid/mismatching caller correlation.
            var retained = _owner.RetainedRelease;
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
            var reason = _disposed ? RestrictedRefusalReason.Disposed : _stop is not null
                ? RestrictedRefusalReason.StopRequested : !_owner.IsLive(_identity)
                    ? RestrictedRefusalReason.InvalidBinding : RestrictedRefusalReason.LiveReleaseNotImplemented;
            // NO correlation admission/binding or physical release here. In
            // particular, this is not an implementation of pre-retention mismatch.
            return ValueTask.FromResult(new RestrictedReleaseReply(RestrictedReleaseKind.Refused,
                RestrictedRequestBinding.Unknown, reason));
        }
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
            // B1 latest local snapshot only. GapOrLoss is explicit; no native
            // observation time, terminal outcome or successful cleanup is invented.
            return ValueTask.FromResult(new RestrictedObservation(_sequence, _identity, _cleanup, _stop));
        }
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
