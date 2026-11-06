// WP05 release/stop arbitration. ReleaseIntent is durably acknowledged
// before the live gate; the gate performs the runtime winner decision,
// capability consumption and exactly one nonblocking RELEASE write; the
// committed delivery observation is persisted after the gate. No journal I/O
// occurs while the arbitration gate is held.
// P3 (owner decision 3.2): after the durable intent the owner-side
// pre-RELEASE active-generation verification runs outside every lock; its
// refusal is the last final-gate entry check before capability consumption,
// keeps the conservative post-intent shape and submits the existing stop
// path outside the gates as a best-effort durable tail. Retained decisions
// still win first and a duplicate never creates a second attempt.
using System.Collections.Immutable;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Restricted.Control;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Execution;

internal sealed class ReleaseArbiter
{
    private readonly PreparedTransaction _capability;
    private readonly OwnershipJournal _journal;
    private readonly JournalWriterLease _writer;
    private readonly ITransactionClock _clock;
    private readonly Func<ReleasePrerequisites> _prerequisites;
    private readonly IReleaseWriteChannel _transport;
    // P3 owner pre-RELEASE generation verification. A construction-time
    // dependency wired only from the sealed owner phase for a retained
    // qualified authority context; never a settable caller callback, Boolean
    // or wire echo. Null keeps legacy behavior unchanged.
    private readonly Func<OwnerGenerationGateRefusal?>? _ownerGenerationGate;
    private readonly object _gate = new();
    private readonly object _journalGate = new();
    internal Action<string>? TestBarrier { get; set; }
    private bool _stopAccepted;
    private bool _releaseRuntimeCommitted;
    internal bool RuntimeReleaseCommitted => Volatile.Read(ref _releaseRuntimeCommitted);
    private ReleaseDecision? _retainedRelease;
    private StopDecision? _retainedStop;
    // R1-F03 Option A: the first successfully admitted approval correlation
    // binds this operation. A later pre-retention mismatch refuses only that
    // call; it never replaces the binding, consumes capability, retains a
    // decision or writes anything. The binding is runtime state only; no
    // journal field or manifest schema carries it.
    private bool _hasAdmittedCorrelation;
    private ImmutableArray<byte> _admittedCorrelation;

    internal ReleaseArbiter(
        PreparedTransaction capability,
        OwnershipJournal journal,
        JournalWriterLease writer,
        ITransactionClock clock,
        Func<ReleasePrerequisites> prerequisites,
        IReleaseWriteChannel transport,
        Func<OwnerGenerationGateRefusal?>? ownerGenerationGate = null)
    {
        _capability = capability ?? throw new ArgumentNullException(nameof(capability));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _prerequisites = prerequisites ?? throw new ArgumentNullException(nameof(prerequisites));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _ownerGenerationGate = ownerGenerationGate;
    }

    internal ReleaseDecision RequestRelease(ReleaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_gate)
        {
            if (_retainedRelease is not null)
            {
                return _retainedRelease with
                {
                    Kind = ReleaseDecisionKind.Duplicate,
                    Binding = BindingFor(request.ApprovalCorrelationSha256),
                    RefusalReason = RestrictedRefusalReason.None,
                };
            }

            if (_stopAccepted)
            {
                _retainedRelease = Refused(
                    ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.StopRequested,
                    "release request refused: stop already won before durable intent",
                    BindingFor(request.ApprovalCorrelationSha256));
                return _retainedRelease;
            }

            if (!IsValidCorrelation(request.ApprovalCorrelationSha256))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.InvalidCorrelation,
                    "release request refused: approval correlation length is invalid",
                    ReleaseCorrelationBinding.InvalidArgument);
            }

            if (_hasAdmittedCorrelation
                && !CorrelationEqual(_admittedCorrelation, request.ApprovalCorrelationSha256))
            {
                // R1-F03 Option A: refuse only this call. No retention, no
                // capability consumption, no deadline reset, no RELEASE.
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.InvalidBinding,
                    "release request refused: approval correlation does not match the admitted binding",
                    ReleaseCorrelationBinding.Mismatch);
            }
        }

        var now = _clock.NowNanoseconds;
        if (now <= 0
            || request.SetupDeadlineNanoseconds <= 0
            || request.OverallDeadlineNanoseconds <= 0
            || request.SetupDeadlineNanoseconds >= request.OverallDeadlineNanoseconds
            || now >= request.SetupDeadlineNanoseconds
            || now >= request.OverallDeadlineNanoseconds)
        {
            return Refused(ReleaseDisposition.NotCommitted,
                RestrictedRefusalReason.DeadlineExpired,
                "release request refused before durable intent: deadline authority check failed",
                CurrentBinding(request.ApprovalCorrelationSha256));
        }

        if (!_prerequisites().Satisfied)
        {
            return Refused(ReleaseDisposition.NotCommitted,
                RestrictedRefusalReason.PrerequisitesUnavailable,
                "release request refused before durable intent: prerequisites are not satisfied",
                CurrentBinding(request.ApprovalCorrelationSha256));
        }

        if (!_capability.IsLive
            || !IdentityEqual(request.Identity, _capability.Identity)
            || !_capability.Identity.ManifestDigest.AsSpan()
                .SequenceEqual(ManifestCodec.ComputeDigestBytes(_capability.Manifest).AsSpan()))
        {
            return Refused(ReleaseDisposition.NotCommitted,
                RestrictedRefusalReason.InvalidBinding,
                "release request refused before durable intent: identity or capability binding check failed",
                CurrentBinding(request.ApprovalCorrelationSha256));
        }

        // Every non-retention gate passed: atomically admit this call's
        // correlation as the operation binding when none exists yet.
        lock (_gate)
        {
            if (_hasAdmittedCorrelation
                && !CorrelationEqual(_admittedCorrelation, request.ApprovalCorrelationSha256))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.InvalidBinding,
                    "release request refused: approval correlation does not match the admitted binding",
                    ReleaseCorrelationBinding.Mismatch);
            }

            if (!_hasAdmittedCorrelation)
            {
                _admittedCorrelation = request.ApprovalCorrelationSha256.IsDefault
                    ? default
                    : ImmutableArray.Create(request.ApprovalCorrelationSha256.AsSpan());
                _hasAdmittedCorrelation = true;
            }
        }

        JournalIoResult intentWrite;
        lock (_journalGate)
        {
            if (!_writer.IsLive)
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.RecoveryAuthorityUnavailable,
                    "release request refused: writer lease is no longer live",
                    ReleaseCorrelationBinding.Accepted);
            }

            var latest = _journal.ReadLatest();
            if (!latest.Ok || latest.Snapshot is null)
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.RecoveryAuthorityUnavailable,
                    $"release request refused: no acknowledged preparation snapshot ({latest.Error})",
                    ReleaseCorrelationBinding.Accepted);
            }

            if (!PreparationMatches(latest.Snapshot, _capability.Identity))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.InvalidBinding,
                    "release intent refused: acknowledged preparation identity does not match the capability",
                    ReleaseCorrelationBinding.Accepted);
            }

            if (!OwnershipTransitions.TryPlanReleaseIntent(
                    latest.Snapshot, now, request.SetupDeadlineNanoseconds,
                    request.OverallDeadlineNanoseconds, out var intent, out var intentError))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    RestrictedRefusalReason.RecoveryAuthorityUnavailable,
                    $"release intent refused: {intentError}",
                    ReleaseCorrelationBinding.Accepted);
            }

            intentWrite = _journal.WriteTransition(_writer, latest.Snapshot, intent!);
        }

        if (!intentWrite.Ok)
        {
            if (intentWrite.FailureKind == JournalFailureKind.AmbiguousAfterContent)
            {
                // The journal gate has been released. This barrier exposes the
                // approved stop-versus-retention ordering to deterministic tests.
                Barrier("after-ambiguous-intent");

                ReleaseDecision ambiguousDecision;
                lock (_gate)
                {
                    if (_retainedRelease is not null)
                    {
                        ambiguousDecision = _retainedRelease with
                        {
                            Kind = ReleaseDecisionKind.Duplicate,
                            Binding = BindingFor(request.ApprovalCorrelationSha256),
                        };
                    }
                    else if (_stopAccepted)
                    {
                        ConsumeCapability();
                        _retainedRelease = Refused(
                            ReleaseDisposition.MayHaveExecuted,
                            RestrictedRefusalReason.StopRequested,
                            "release gate refused: stop won during durable intent",
                            ReleaseCorrelationBinding.Accepted);
                        ambiguousDecision = _retainedRelease;
                    }
                    else
                    {
                        _retainedRelease = new ReleaseDecision(
                            ReleaseDecisionKind.AmbiguousPersistence,
                            ReleaseDisposition.MayHaveExecuted,
                            DeliveryResult.NotAttempted,
                            DurableCommitted: false,
                            LiveExecutionExcluded: true,
                            "release decision retained because ambiguous journal persistence was reported",
                            ReleaseCorrelationBinding.Accepted,
                            RestrictedRefusalReason.None);
                        ambiguousDecision = _retainedRelease;
                    }
                }

                // Both barriers and the C2 tail stop run after releasing _gate.
                Barrier("after-ambiguous-retention");
                StopDecision tailStop;
                try
                {
                    tailStop = RequestStop(StopCauseKind.CallerStop);
                }
                catch (Exception ex)
                {
                    if (ambiguousDecision.Kind == ReleaseDecisionKind.AmbiguousPersistence)
                    {
                        RecordAmbiguousTailStopLimitation(ex.Message);
                    }

                    throw;
                }

                if ((!tailStop.Accepted || !tailStop.DurableStop)
                    && ambiguousDecision.Kind == ReleaseDecisionKind.AmbiguousPersistence)
                {
                    RecordAmbiguousTailStopLimitation(tailStop.Reason);
                    lock (_gate)
                    {
                        ambiguousDecision = _retainedRelease!;
                    }
                }

                return ambiguousDecision;
            }

            return Refused(ReleaseDisposition.NotCommitted,
                RestrictedRefusalReason.RecoveryAuthorityUnavailable,
                "release intent was not durably acknowledged",
                ReleaseCorrelationBinding.Accepted);
        }

        Barrier("after-intent");

        // P3 owner-side pre-RELEASE active-generation verification. It runs
        // after durable intent and the outside-lock after-intent barrier and
        // outside _gate, _journalGate and every capability lock. No journal
        // I/O, protocol wait or unrelated I/O may follow the successful
        // observation before final gate entry. The result is consumed only
        // for this invocation: it is never cached across requests, adopted
        // or supplied by the controller, and any retained stop/deadline/
        // duplicate decision below wins over it unchanged.
        var ownerGenerationRefusal = InvokeOwnerGenerationGate();
        var ownerGateTailPending = false;
        lock (_gate)
        {
            if (_retainedRelease is not null)
            {
                return _retainedRelease with
                {
                    Kind = ReleaseDecisionKind.Duplicate,
                    Binding = BindingFor(request.ApprovalCorrelationSha256),
                };
            }

            if (_stopAccepted)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    RestrictedRefusalReason.StopRequested,
                    "release gate refused: stop won during durable intent",
                    ReleaseCorrelationBinding.Accepted);
                return _retainedRelease;
            }

            var gateNow = _clock.NowNanoseconds;
            if (gateNow <= 0
                || gateNow >= request.SetupDeadlineNanoseconds
                || gateNow >= request.OverallDeadlineNanoseconds)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    RestrictedRefusalReason.DeadlineExpired,
                    "release gate refused: deadline authority check failed",
                    ReleaseCorrelationBinding.Accepted);
                return _retainedRelease;
            }

            if (!_prerequisites().Satisfied)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    RestrictedRefusalReason.PrerequisitesUnavailable,
                    "release gate refused: prerequisites are not satisfied",
                    ReleaseCorrelationBinding.Accepted);
                return _retainedRelease;
            }

            if (!_capability.IsLive)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    RestrictedRefusalReason.InvalidBinding,
                    "release gate refused: capability liveness check failed",
                    ReleaseCorrelationBinding.Accepted);
                return _retainedRelease;
            }

            // P3 refusal: the last final-gate entry check before capability
            // consumption. The durable intent already exists, so the refusal
            // keeps the conservative post-intent shape, closes the capability
            // exactly once and must not reach the durable commit block. The
            // stop tail is submitted after this lock is released through the
            // existing stop path under the original context and C3 authority.
            if (ownerGenerationRefusal is not null)
            {
                ConsumeCapability();
                _retainedRelease = new ReleaseDecision(
                    ReleaseDecisionKind.Refused,
                    ReleaseDisposition.MayHaveExecuted,
                    DeliveryResult.NotAttempted,
                    DurableCommitted: false,
                    LiveExecutionExcluded: true,
                    $"release gate refused: owner pre-RELEASE generation verification failed ({ownerGenerationRefusal.Error})",
                    ReleaseCorrelationBinding.Accepted,
                    ownerGenerationRefusal.Reason);
                ownerGateTailPending = true;
            }
            else
            {
                Barrier("before-consume");
                if (!_capability.TryConsume(out var violation))
                {
                    _retainedRelease = Refused(
                        ReleaseDisposition.MayHaveExecuted,
                        RestrictedRefusalReason.InvalidBinding,
                        $"release gate refused: capability consumption rejected ({violation})",
                        ReleaseCorrelationBinding.Accepted);
                    return _retainedRelease;
                }

                Barrier("after-consume");
                _releaseRuntimeCommitted = true;
                var frame = CoordinatorProtocol.EncodeRelease(
                    _capability.Identity, request.OverallDeadlineNanoseconds);
                ReleasePipeWriteResult send;
                try
                {
                    send = _transport.TryWriteOnce(frame);
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or OverflowException)
                {
                    send = ReleasePipeWriteResult.Unknown;
                }

                Barrier("after-write");
                var delivery = MapDelivery(send);
                var liveExcluded = send is ReleasePipeWriteResult.WouldBlock
                    or ReleasePipeWriteResult.BrokenPipe
                    or ReleasePipeWriteResult.Partial;
                _retainedRelease = new ReleaseDecision(
                    ReleaseDecisionKind.Released,
                    ReleaseDisposition.MayHaveExecuted,
                    delivery,
                    DurableCommitted: false,
                    LiveExecutionExcluded: liveExcluded,
                    $"release won the gate; transport result {send}",
                    ReleaseCorrelationBinding.Accepted,
                    RestrictedRefusalReason.None);
            }
        }

        if (ownerGateTailPending)
        {
            SubmitOwnerGateRefusalStopTail();
            lock (_gate)
            {
                return _retainedRelease!;
            }
        }

        var committedAt = _clock.NowNanoseconds;
        var durableCommitted = false;
        var reason = _retainedRelease!.Reason;
        if (committedAt <= 0)
        {
            reason += "; committed evidence not persisted: monotonic clock unavailable";
        }
        else
        {
            lock (_journalGate)
            {
                var write = _journal.WriteMutation(_writer, current =>
                {
                    if (!OwnershipTransitions.TryPlanReleaseCommitted(
                        current, committedAt, _retainedRelease!.Delivery, out var next, out var error))
                    {
                        throw new InvalidOperationException(error);
                    }

                    return next!;
                });
                durableCommitted = write.Ok;
                if (!write.Ok)
                {
                    reason += $"; committed evidence not persisted: {write.Error}";
                }
            }
        }

        Barrier("after-committed");
        lock (_gate)
        {
            _retainedRelease = _retainedRelease! with
            {
                DurableCommitted = durableCommitted,
                Reason = reason,
            };
            return _retainedRelease;
        }
    }

    internal StopDecision RequestStop(StopCauseKind cause)
    {
        var now = _clock.NowNanoseconds;
        StopDecision decision;
        lock (_gate)
        {
            if (_retainedStop is not null)
            {
                return _retainedStop with { Duplicate = true };
            }

            ConsumeStopCapability();
            _stopAccepted = true;

            if (now <= 0)
            {
                _retainedStop = new StopDecision(
                    true, false, _releaseRuntimeCommitted, false, cause, 0, 0,
                    "stop accepted live; monotonic clock unavailable so no durable stop evidence was written");
                return _retainedStop;
            }

            long deadline;
            try
            {
                checked
                {
                    deadline = now + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;
                }
            }
            catch (OverflowException)
            {
                _retainedStop = new StopDecision(
                    true, false, _releaseRuntimeCommitted, false, cause, now, 0,
                    "stop accepted live; deadline overflow prevented durable stop evidence");
                return _retainedStop;
            }

            _retainedStop = new StopDecision(
                true, false, _releaseRuntimeCommitted, false, cause, now, deadline,
                "stop accepted; durable stop evidence pending");
            decision = _retainedStop;
        }

        var durable = false;
        var reason = decision.Reason;
        lock (_journalGate)
        {
            var write = _journal.WriteMutation(_writer, current =>
            {
                if (!OwnershipTransitions.TryPlanStop(
                    current, decision.StopAcceptedAtNanoseconds, cause, out var next, out var error))
                {
                    throw new InvalidOperationException(error);
                }

                return next!;
            });
            durable = write.Ok;
            if (!write.Ok)
            {
                reason += $"; durable stop evidence not persisted: {write.Error}";
            }
        }

        Barrier("after-stop");
        lock (_gate)
        {
            _retainedStop = decision with { DurableStop = durable, Reason = reason };
            return _retainedStop;
        }
    }

    private void Barrier(string name) => TestBarrier?.Invoke(name);

    private void RecordAmbiguousTailStopLimitation(string limitation)
    {
        lock (_gate)
        {
            if (_retainedRelease is { Kind: ReleaseDecisionKind.AmbiguousPersistence } retained)
            {
                _retainedRelease = retained with
                {
                    Reason = $"{retained.Reason}; tail stop limitation: {limitation}",
                };
            }
        }
    }

    // P3 owner pre-RELEASE generation verification. Verification availability
    // is not authorization: any failure to complete refuses fail-closed as
    // unverified, never as a pass.
    private OwnerGenerationGateRefusal? InvokeOwnerGenerationGate()
    {
        if (_ownerGenerationGate is null)
        {
            return null;
        }

        try
        {
            return _ownerGenerationGate();
        }
        catch (Exception exception)
        {
            return new OwnerGenerationGateRefusal(
                RestrictedRefusalReason.InstallationUnavailable,
                $"owner generation verification could not complete: {exception.Message}");
        }
    }

    // P3 stop tail for a generation-gate refusal: the stop is submitted
    // outside every gate (OwnerSafetyMonitor keeps its native stop ahead of
    // this best-effort durable settlement) under the original C3 authority.
    // A limitation is recorded on the retained refusal; the refusal is never
    // retried and never turned into a release.
    private void SubmitOwnerGateRefusalStopTail()
    {
        string limitation;
        try
        {
            var tailStop = RequestStop(StopCauseKind.SetupFault);
            if (tailStop.Accepted && tailStop.DurableStop)
            {
                return;
            }

            limitation = tailStop.Reason;
        }
        catch (Exception exception)
        {
            // The refusal is already retained and excludes live execution;
            // the limitation is recorded, never surfaced as a retry path.
            limitation = exception.Message;
        }

        RecordOwnerGateTailStopLimitation(limitation);
    }

    private void RecordOwnerGateTailStopLimitation(string limitation)
    {
        lock (_gate)
        {
            if (_retainedRelease is { Kind: ReleaseDecisionKind.Refused, RefusalReason:
                    RestrictedRefusalReason.UnqualifiedInstallation
                    or RestrictedRefusalReason.InstallationUnavailable } retained)
            {
                _retainedRelease = retained with
                {
                    Reason = $"{retained.Reason}; tail stop limitation: {limitation}",
                };
            }
        }
    }

    private ReleaseDecision Refused(ReleaseDisposition release, RestrictedRefusalReason refusalReason,
        string reason, ReleaseCorrelationBinding binding) =>
        new(ReleaseDecisionKind.Refused, release, DeliveryResult.NotAttempted,
            DurableCommitted: false, LiveExecutionExcluded: true, reason, binding, refusalReason);

    // R1-F03 Option A admission helpers. Absent and exactly 32 bytes are the
    // only valid forms; all-zero 32 bytes is a present correlation.
    private static bool IsValidCorrelation(ImmutableArray<byte> value) =>
        value.IsDefault || value.Length == 32;

    private static bool CorrelationEqual(ImmutableArray<byte> left, ImmutableArray<byte> right) =>
        left.IsDefault == right.IsDefault
        && (left.IsDefault || left.AsSpan().SequenceEqual(right.AsSpan()));

    private ReleaseCorrelationBinding BindingFor(ImmutableArray<byte> candidate) =>
        !IsValidCorrelation(candidate) ? ReleaseCorrelationBinding.InvalidArgument
        : !_hasAdmittedCorrelation ? ReleaseCorrelationBinding.Unknown
        : CorrelationEqual(_admittedCorrelation, candidate)
            ? ReleaseCorrelationBinding.Accepted
            : ReleaseCorrelationBinding.Mismatch;

    private ReleaseCorrelationBinding CurrentBinding(ImmutableArray<byte> candidate)
    {
        lock (_gate)
        {
            return BindingFor(candidate);
        }
    }

    private void ConsumeCapability()
    {
        _ = _capability.TryConsume(out _);
    }

    private void ConsumeStopCapability()
    {
        if (_capability.TryConsume(out var violation))
        {
            return;
        }

        if (_capability.IsLive)
        {
            throw new InvalidOperationException(
                $"stop capability invalidation failed: capability remains live after consumption refusal ({violation})");
        }
    }

    private static DeliveryResult MapDelivery(ReleasePipeWriteResult result) => result switch
    {
        ReleasePipeWriteResult.Delivered => DeliveryResult.Delivered,
        ReleasePipeWriteResult.WouldBlock => DeliveryResult.WouldBlock,
        ReleasePipeWriteResult.BrokenPipe => DeliveryResult.BrokenPipe,
        ReleasePipeWriteResult.Partial => DeliveryResult.Partial,
        _ => DeliveryResult.Unknown,
    };

    private static bool IdentityEqual(PreparationIdentity left, PreparationIdentity right) =>
        left.ExecutionId == right.ExecutionId
        && left.IssuerId == right.IssuerId
        && left.GenerationId == right.GenerationId
        && left.PreparationId == right.PreparationId
        && left.ManifestDigest.AsSpan().SequenceEqual(right.ManifestDigest.AsSpan());

    private static bool PreparationMatches(OwnershipSnapshot snapshot, PreparationIdentity identity) =>
        snapshot.ExecutionId == identity.ExecutionId
        && snapshot.Stage == PreparationStage.Prepared
        && snapshot.Preparation is not null
        && snapshot.Preparation.ExecutionId == identity.ExecutionId
        && snapshot.Preparation.IssuerId == identity.IssuerId
        && snapshot.Preparation.GenerationId == identity.GenerationId
        && snapshot.Preparation.PreparationId == identity.PreparationId
        && snapshot.Preparation.ManifestDigest.AsSpan().SequenceEqual(identity.ManifestDigest.AsSpan());
}
