// WP05 release/stop arbitration. ReleaseIntent is durably acknowledged
// before the live gate; the gate performs the runtime winner decision,
// capability consumption and exactly one nonblocking RELEASE write; the
// committed delivery observation is persisted after the gate. No journal I/O
// occurs while the arbitration gate is held.
using Gagamba.Execution.Linux.M1.Launch;
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
    private readonly object _gate = new();
    private readonly object _journalGate = new();
    internal Action<string>? TestBarrier { get; set; }
    private bool _stopAccepted;
    private bool _releaseRuntimeCommitted;
    private ReleaseDecision? _retainedRelease;
    private StopDecision? _retainedStop;

    internal ReleaseArbiter(
        PreparedTransaction capability,
        OwnershipJournal journal,
        JournalWriterLease writer,
        ITransactionClock clock,
        Func<ReleasePrerequisites> prerequisites,
        IReleaseWriteChannel transport)
    {
        _capability = capability ?? throw new ArgumentNullException(nameof(capability));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _prerequisites = prerequisites ?? throw new ArgumentNullException(nameof(prerequisites));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    internal ReleaseDecision RequestRelease(ReleaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_gate)
        {
            if (_retainedRelease is not null)
            {
                return _retainedRelease with { Kind = ReleaseDecisionKind.Duplicate };
            }

            if (_stopAccepted)
            {
                _retainedRelease = Refused(
                    ReleaseDisposition.NotCommitted,
                    "release request refused: stop already won before durable intent");
                return _retainedRelease;
            }
        }

        var now = _clock.NowNanoseconds;
        if (now <= 0
            || request.SetupDeadlineNanoseconds <= 0
            || request.OverallDeadlineNanoseconds <= 0
            || request.SetupDeadlineNanoseconds >= request.OverallDeadlineNanoseconds
            || now >= request.SetupDeadlineNanoseconds
            || now >= request.OverallDeadlineNanoseconds
            || !_prerequisites().Satisfied
            || !_capability.IsLive
            || !IdentityEqual(request.Identity, _capability.Identity)
            || !_capability.Identity.ManifestDigest.AsSpan()
                .SequenceEqual(ManifestCodec.ComputeDigestBytes(_capability.Manifest).AsSpan()))
        {
            return Refused(ReleaseDisposition.NotCommitted,
                "release request refused before durable intent: identity, deadline, prerequisite or capability check failed");
        }

        lock (_journalGate)
        {
            if (!_writer.IsLive)
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    "release request refused: writer lease is no longer live");
            }

            var latest = _journal.ReadLatest();
            if (!latest.Ok || latest.Snapshot is null)
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    $"release request refused: no acknowledged preparation snapshot ({latest.Error})");
            }

            if (!PreparationMatches(latest.Snapshot, _capability.Identity))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    "release intent refused: acknowledged preparation identity does not match the capability");
            }

            if (!OwnershipTransitions.TryPlanReleaseIntent(
                    latest.Snapshot, now, request.SetupDeadlineNanoseconds,
                    request.OverallDeadlineNanoseconds, out var intent, out var intentError))
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    $"release intent refused: {intentError}");
            }

            if (!_journal.WriteTransition(_writer, latest.Snapshot, intent!).Ok)
            {
                return Refused(ReleaseDisposition.NotCommitted,
                    "release intent was not durably acknowledged");
            }
        }

        Barrier("after-intent");
        lock (_gate)
        {
            if (_retainedRelease is not null)
            {
                return _retainedRelease with { Kind = ReleaseDecisionKind.Duplicate };
            }

            if (_stopAccepted)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    "release gate refused: stop won during durable intent");
                return _retainedRelease;
            }

            var gateNow = _clock.NowNanoseconds;
            if (gateNow <= 0
                || gateNow >= request.SetupDeadlineNanoseconds
                || gateNow >= request.OverallDeadlineNanoseconds
                || !_prerequisites().Satisfied
                || !_capability.IsLive)
            {
                ConsumeCapability();
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    "release gate refused: expiry, prerequisite, liveness or capability check failed");
                return _retainedRelease;
            }

            Barrier("before-consume");
            if (!_capability.TryConsume(out var violation))
            {
                _retainedRelease = Refused(
                    ReleaseDisposition.MayHaveExecuted,
                    $"release gate refused: capability consumption rejected ({violation})");
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
                $"release won the gate; transport result {send}");
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

            if (now <= 0)
            {
                _stopAccepted = true;
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
                _stopAccepted = true;
                _retainedStop = new StopDecision(
                    true, false, _releaseRuntimeCommitted, false, cause, now, 0,
                    "stop accepted live; deadline overflow prevented durable stop evidence");
                return _retainedStop;
            }

            _stopAccepted = true;
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

    private ReleaseDecision Refused(ReleaseDisposition release, string reason) =>
        new(ReleaseDecisionKind.Refused, release, DeliveryResult.NotAttempted,
            DurableCommitted: false, LiveExecutionExcluded: true, reason);

    private void ConsumeCapability()
    {
        _ = _capability.TryConsume(out _);
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
