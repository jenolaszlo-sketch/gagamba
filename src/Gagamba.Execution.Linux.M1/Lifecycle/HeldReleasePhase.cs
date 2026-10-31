// B3 held phase of the one authoritative owner transaction. The owner keeps
// the live capability, the durable writer lease and the sole RELEASE writer;
// the controller may only request admission over its authenticated,
// identity-bound channel. Stop/abort arbitration stays inside the existing
// ReleaseArbiter: at most one physical RELEASE write exists, and every
// post-release fact is derived from the trusted coordinator status sequence,
// the retained reaper, the bounded capture drains and the retained domain
// handles.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal sealed class HeldReleasePhase
{
    // The fixture coordinator/exec path is the only post-release producer of
    // these facts; capture drains are bounded like every other B3 buffer.
    private const long ObservationAllowanceNanoseconds = 6_000_000_000L;
    private const string ReleaseAfterIntentBarrier = "release-after-intent";

    private readonly OwnerPreparationSpecification _request;
    private readonly OwnerProtocolChannel _channel;
    private readonly BubblewrapAdapter _launcher;
    private readonly HeldExecution _execution;
    private readonly DomainLease _primaryLease;
    private readonly OwnershipJournal _journal;
    private readonly JournalWriterLease _writer;
    private readonly PreparedTransaction _capability;
    private readonly ReleaseArbiter _arbiter;
    private readonly SafeFileHandle? _scopePidFd;
    private readonly long _setupDeadline;
    private readonly long _overallDeadline;
    private bool _pendingAbortHeld;
    private readonly OwnerSafetyMonitor? _safety;

    internal HeldReleasePhase(
        OwnerPreparationSpecification request,
        OwnerProtocolChannel channel,
        BubblewrapAdapter launcher,
        HeldExecution execution,
        DomainLease primaryLease,
        OwnershipJournal journal,
        JournalWriterLease writer,
        PreparedTransaction capability,
        long setupDeadline,
        long overallDeadline,
        OwnerSafetyMonitor? safety = null)
    {
        _safety = safety;
        _request = request;
        _channel = channel;
        _launcher = launcher;
        _execution = execution;
        _primaryLease = primaryLease;
        _journal = journal;
        _writer = writer;
        _capability = capability;
        _setupDeadline = setupDeadline;
        _overallDeadline = overallDeadline;
        if (safety is null && PidFd.TryOpen(RecoveryInterop.GetParentPid(), out var parentFd, out _)
            && PreparationInterop.HasCloexec(parentFd))
        {
            _scopePidFd = new SafeFileHandle(new IntPtr(parentFd), ownsHandle: true);
        }

        _arbiter = new ReleaseArbiter(capability, journal, writer, new OwnerClock(),
            Prerequisites, execution)
        {
            TestBarrier = HandleArbiterBarrier,
        };
        safety?.BindArbiter(_arbiter);
    }

    /// <summary>Sends the owner-origin HELD establishment frame, then serves
    /// exactly one bounded request at a time until abort, controller loss or
    /// the retained overall deadline. Returns the owner process exit code.</summary>
    internal int Run(byte[] coordinatorStatusFrame)
    {
        if (_safety?.StopLatched == true) return Abort();
        var heldReadyPayload = new byte[17 + coordinatorStatusFrame.Length];
        heldReadyPayload[0] = 1; // independent fixture constructor-marker oracle checked before HELD is accepted
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(
            heldReadyPayload.AsSpan(1, 8), checked((ulong)_setupDeadline));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(
            heldReadyPayload.AsSpan(9, 8), checked((ulong)_overallDeadline));
        coordinatorStatusFrame.CopyTo(heldReadyPayload, 17);
        if (!_channel.TrySend(OwnerProtocolMessageType.HeldReady, heldReadyPayload, out _))
        {
            return Abort();
        }

        while (true)
        {
            if (_pendingAbortHeld || _safety?.StopLatched == true)
            {
                return Abort();
            }

            if (!_channel.TryReceiveAllowedWithin(250, out var message, out var error,
                    OwnerProtocolMessageType.AbortHeld,
                    OwnerProtocolMessageType.StopRequest,
                    OwnerProtocolMessageType.ReleaseRequest, OwnerProtocolMessageType.CaptureRead))
            {
                if (error == "control record timed out")
                {
                    if (NowNanoseconds() >= _overallDeadline)
                    {
                        // No renewed allowance: the retained overall deadline
                        // ends the held lifetime without a controller request.
                        var expired = Abort();
                        return expired == 0 ? 53 : expired;
                    }

                    continue;
                }

                // Controller loss or transport failure: stop and clean up
                // without any release decision.
                return Abort();
            }

            switch (message!.Type)
            {
                case OwnerProtocolMessageType.AbortHeld:
                    return Abort();
                case OwnerProtocolMessageType.StopRequest:
                {
                    if (!Enum.IsDefined(typeof(StopCauseKind), message.Payload[0]))
                    {
                        return Abort();
                    }

                    SendStopDecision(_arbiter.RequestStop((StopCauseKind)message.Payload[0]));
                    break;
                }
                case OwnerProtocolMessageType.CaptureRead:
                {
                    var payload = message.Payload;
                    var offset = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(1));
                    var count = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(9));
                    byte[] response = [0];
                    if (payload[0] <= 1 && offset <= ProtectedCapture.StreamCeiling && count is >= 0 and <= ProtectedCapture.ReadCeiling
                        && _execution.Capture is { } capture)
                    {
                        var bytes = capture.Read(payload[0] == 0, offset, count);
                        response = new byte[1+bytes.Length]; response[0] = 1; bytes.CopyTo(response, 1);
                    }
                    _channel.TrySend(OwnerProtocolMessageType.CaptureBytes, response, out _);
                    break;
                }
                case OwnerProtocolMessageType.ReleaseRequest:
                    HandleReleaseRequest(message);
                    break;
            }
        }
    }

    private void HandleReleaseRequest(OwnerProtocolMessage message)
    {
        if (!OwnerHeldWireCodec.TryDecodeReleaseRequest(message.Payload, out var request, out var decodeError)
            || request is null)
        {
            SendReleaseDecision(RefusedRelease("malformed release request: " + decodeError,
                ReleaseCorrelationBinding.InvalidArgument, RestrictedRefusalReason.InvalidCorrelation));
            return;
        }

        if (request.ExecutionId != _capability.Identity.ExecutionId
            || request.PreparationId != _capability.Identity.PreparationId
            || request.GenerationId != _capability.Identity.GenerationId
            || !request.ManifestDigest.AsSpan().SequenceEqual(_capability.Identity.ManifestDigest.AsSpan()))
        {
            // Wrong operation/generation: refuse without consuming anything.
            SendReleaseDecision(RefusedRelease("release request identity does not match the live capability",
                ReleaseCorrelationBinding.Unknown, RestrictedRefusalReason.InvalidBinding));
            return;
        }

        var correlation = request.ApprovalCorrelationSha256 is { } bytes
            ? ImmutableArray.Create(bytes)
            : default(ImmutableArray<byte>);
        var decision = _arbiter.RequestRelease(new ReleaseRequest(
            _capability.Identity, _setupDeadline, _overallDeadline, correlation));
        if(_request.QualificationFault is { } fault)
            Int01Qualification.Write(fault,"owner-release-decision",new { Kind=decision.Kind.ToString(), decision.Delivery, decision.DurableCommitted, PhysicalReleaseAttempts=_execution.PhysicalReleaseAttempts });
        if(Int01Qualification.Reached(_request.QualificationFault,"owner-before-release-reply"))
        { _channel.Dispose();_pendingAbortHeld=true;return; }
        SendReleaseDecision(decision);
        if (decision.Kind == ReleaseDecisionKind.Released)
        {
            _safety?.Released();
            if (Int01Qualification.Is(_request.QualificationFault,"b4-status-backpressure"))
                Int01Qualification.ObserveFullStatusPipe(_request.QualificationFault!,
                    _execution.CoordinatorStatusReader.DangerousGetHandle().ToInt32(), Math.Min(_overallDeadline,NowNanoseconds()+2_000_000_000L));
            Int01Qualification.Point(_request.QualificationFault,"owner-before-observation",_overallDeadline);
            var facts = ObserveExecution(decision);
            if(_request.QualificationFault is { } observedFault) Int01Qualification.Write(observedFault,"owner-execution-facts",facts);
            PersistObservations(facts, decision);
            SendExecutionOutcome(facts);
        }
    }

    private ReleaseDecision RefusedRelease(string reason, ReleaseCorrelationBinding binding,
        RestrictedRefusalReason refusalReason) =>
        new(ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted, DeliveryResult.NotAttempted,
            DurableCommitted: false, LiveExecutionExcluded: true, reason, binding, refusalReason);

    private void SendReleaseDecision(ReleaseDecision decision) =>
        _channel.TrySend(OwnerProtocolMessageType.ReleaseDecision,
            OwnerHeldWireCodec.EncodeReleaseDecision(decision), out _);

    private void SendStopDecision(StopDecision decision) =>
        _channel.TrySend(OwnerProtocolMessageType.StopDecision,
            OwnerHeldWireCodec.EncodeStopDecision(decision), out _);

    private void SendExecutionOutcome(OwnerExecutionFacts facts) =>
        _channel.TrySend(OwnerProtocolMessageType.ExecutionOutcome,
            OwnerHeldWireCodec.EncodeExecutionOutcome(facts, $"observed at {NowNanoseconds()}"), out _);

    /// <summary>Nonblocking, truthful arbitration prerequisites: the WP04
    /// resource limits are installed and readable on the retained lease, the
    /// bounded capture/status readers are open, the caller's own request is
    /// being serviced on this live association, and the recovery-scope parent
    /// has not exited. Anything unavailable refuses by construction.</summary>
    private ReleasePrerequisites Prerequisites()
    {
        var resources = _safety?.ResourcesValid ?? _primaryLease.TryReadWp04Limits(out _, out _);
        var capture = _execution.Capture?.Healthy == true && _execution.StdoutReader is { IsClosed: false, IsInvalid: false }
            && _execution.StderrReader is { IsClosed: false, IsInvalid: false }
            && _execution.CoordinatorStatusReader is { IsClosed: false, IsInvalid: false };
        if (_safety is not null)
            return new ReleasePrerequisites(resources,capture,_safety.ControllerAlive,_safety.RecoveryAlive);
        var recoveryAlive = false;
        if (_scopePidFd is not null)
        {
            recoveryAlive = PidFd.TryExited(
                    _scopePidFd.DangerousGetHandle().ToInt32(), 0, out var exited, out var error)
                && !exited && error.Length == 0;
        }

        return new ReleasePrerequisites(resources, capture, CallerAlive: true, recoveryAlive);
    }

    /// <summary>Deterministic release-phase barrier: when the fixture profile
    /// selects it, the owner announces the paused arbitration and processes a
    /// stop or abort before the release gate continues. Only outside-lock
    /// arbitration points are pausable.</summary>
    private void HandleArbiterBarrier(string name)
    {
        if(_request.QualificationFault is { } fault)
        {
            if(name=="after-intent" && Int01Qualification.Stage(fault.Case)=="release-after-intent")
            {
                Int01Qualification.Write(fault,"owner-before-physical-release",new {PhysicalReleaseAttempts=_execution.PhysicalReleaseAttempts});
                Int01Qualification.Point(fault,"release-after-intent",_setupDeadline,()=>_safety?.StopLatched==true);
            }
            if(name=="after-write" && Int01Qualification.Reached(fault,"release-after-write"))
            {
                Int01Qualification.Write(fault,"physical-release",new { PhysicalReleaseAttempts=_execution.PhysicalReleaseAttempts });
                Process.GetCurrentProcess().Kill();
                Thread.Sleep(Timeout.Infinite); // SIGKILL must terminate this exact owner, never proceed.
            }
        }
        if (!string.Equals(_request.TestBarrier, ReleaseAfterIntentBarrier, StringComparison.Ordinal)
            || !string.Equals(name, "after-intent", StringComparison.Ordinal))
        {
            return;
        }

        if (!_channel.TrySend(OwnerProtocolMessageType.BarrierReached,
                Encoding.UTF8.GetBytes(ReleaseAfterIntentBarrier), out _))
        {
            return;
        }

        while (true)
        {
            if (!_channel.TryReceiveAllowed(out var message, out _, OwnerProtocolMessageType.Continue,
                    OwnerProtocolMessageType.StopRequest, OwnerProtocolMessageType.AbortHeld))
            {
                _pendingAbortHeld = true;
                return;
            }

            if (message!.Type == OwnerProtocolMessageType.Continue)
            {
                return;
            }

            if (message.Type == OwnerProtocolMessageType.StopRequest)
            {
                if (Enum.IsDefined(typeof(StopCauseKind), message.Payload[0]))
                {
                    SendStopDecision(_arbiter.RequestStop((StopCauseKind)message.Payload[0]));
                }

                continue;
            }

            // AbortHeld during arbitration is arbitrated as a stop before the
            // release gate continues; the abort handshake is deferred.
            _pendingAbortHeld = true;
            SendStopDecision(_arbiter.RequestStop(StopCauseKind.CallerStop));
            return;
        }
    }

    private int Abort()
    {
        var abort = _launcher.AbortAsync(_execution, _primaryLease, CancellationToken.None)
            .GetAwaiter().GetResult();
        var payload = new byte[]
        {
            abort.Stopped ? (byte)1 : (byte)0,
            abort.NoTargetInitializationMarkerAbsent ? (byte)1 : (byte)0,
            _execution.ReleaseAttempted ? (byte)1 : (byte)0,
        };
        return _channel.TrySend(OwnerProtocolMessageType.HeldAborted, payload, out _)
            ? abort.Stopped ? 0 : 51
            : abort.Stopped ? 48 : 49;
    }

    /// <summary>Post-release observation derived from trusted native sources
    /// only. Missing frames or unavailable readers stay Unknown; the fixture
    /// output digest is a separate fact and never classifies exec.</summary>
    private OwnerExecutionFacts ObserveExecution(ReleaseDecision decision)
    {
        var deadline = Math.Min(_overallDeadline, NowNanoseconds() + ObservationAllowanceNanoseconds);
        var exec = ExecTag.Unknown;
        var execErrno = 0;
        var root = RootKind.Unknown;
        var exitCode = 0;
        var signal = 0;
        var coreDumped = false;
        var rawWaitPresent = false;
        var rawWaitStatus = 0;

        var statusDeadline = decision.Delivery == DeliveryResult.Delivered
            ? deadline
            : Math.Min(deadline, NowNanoseconds() + 1_000_000_000L);
        try
        {
            var reader = new CoordinatorStatusReader(
                new FdStatusFrameSource(_execution.CoordinatorStatusReader.DangerousGetHandle().ToInt32()),
                _capability.Identity.ExecutionId, _capability.Identity.PreparationId,
                _capability.Identity.ManifestDigest.AsSpan(), initialSequence: 2);
            var deadlineLocal = statusDeadline;
            while (deadlineLocal > 0 && reader.TryReadNext(deadlineLocal, out var observation, out _))
            {
                if (observation!.ProtocolFailed)
                {
                    break;
                }

                if (observation.Exec is not null)
                {
                    exec = observation.Exec.Tag;
                    execErrno = observation.Exec.Errno;
                }

                if (observation.Root is not null)
                {
                    root = observation.Root.Kind;
                    exitCode = observation.Root.ExitCode;
                    signal = observation.Root.Signal;
                    coreDumped = observation.Root.CoreDumped;
                    rawWaitPresent = observation.Root.RawWaitPresent;
                    rawWaitStatus = observation.Root.RawWaitStatus;
                    break;
                }

                // Root follows the exec frame; give it the remaining allowance.
                deadlineLocal = statusDeadline;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or OverflowException)
        {
            exec = ExecTag.Unknown;
        }

        var wrapperExited = _execution.WrapperReaper.TryReap(5000, out var wrapperExitCode, out _);
        var captured = _execution.Capture!.Finish();
        var stdout = captured.Stdout; var stderr = captured.Stderr;
        var population = _primaryLease.ObservePopulation();
        var outcome = Classify(decision.Delivery, exec, root, exitCode);
        if (!captured.Complete || !population.Known || !population.Empty || _safety?.StopLatched == true)
            outcome = RestrictedOutcome.Unknown; // known exec/root survive missing completion axes

        return new OwnerExecutionFacts(
            outcome, exec, execErrno, root, exitCode, signal, coreDumped,
            rawWaitPresent, rawWaitStatus, wrapperExited, wrapperExitCode,
            population.Known, population.Known && !population.Empty,
            (ulong)stdout.Bytes.Length, stdout.Seen, stdout.Eof, stdout.Overflow,
            (ulong)stderr.Bytes.Length, stderr.Seen, stderr.Eof, stderr.Overflow,
            stdout.Failed || stderr.Failed,
            stdout.Digest, stderr.Digest,
            ObservedAtNanoseconds: NowNanoseconds(),PhysicalReleaseAttempts:_execution.PhysicalReleaseAttempts,
            StdoutReadFailure:stdout.Failed,StderrReadFailure:stderr.Failed,CaptureDrainExpired:captured.Expired);
    }

    private static RestrictedOutcome Classify(DeliveryResult delivery, ExecTag exec, RootKind root, int exitCode)
    {
        if (delivery != DeliveryResult.Delivered)
        {
            return RestrictedOutcome.Unknown;
        }

        if (exec == ExecTag.FailedBeforeExec)
        {
            return RestrictedOutcome.FailedToEstablish;
        }

        if (exec != ExecTag.Confirmed)
        {
            return RestrictedOutcome.Unknown;
        }

        return root switch
        {
            RootKind.Exited when exitCode == 0 => RestrictedOutcome.Succeeded,
            RootKind.Exited => RestrictedOutcome.TargetFailed,
            RootKind.Signalled => RestrictedOutcome.TargetFailed,
            _ => RestrictedOutcome.Unknown,
        };
    }

    /// <summary>Persists the coordinator-derived exec/root observation after a
    /// durably committed release. The transition model admits exactly one
    /// durable fact group per mutation, so exec and root are two sequential
    /// acknowledged writes. Best effort: a refused write leaves the outcome
    /// facts reported to the controller and the journal unchanged.</summary>
    private void PersistObservations(OwnerExecutionFacts facts, ReleaseDecision decision)
    {
        if (!decision.DurableCommitted || facts.Exec is not (ExecTag.Confirmed or ExecTag.FailedBeforeExec))
        {
            return;
        }

        var execWrite = _journal.WriteMutation(_writer, current =>
        {
            if (!OwnershipTransitions.TryPlanExecObservation(current,
                    new ExecObservation(facts.Exec, facts.ExecErrno, ExecBasis.CoordinatorFrame),
                    out var afterExec, out var execError))
            {
                throw new InvalidOperationException(execError);
            }

            return afterExec!;
        });
        if (!execWrite.Ok || facts.Root == RootKind.Unknown || !facts.RawWaitPresent)
        {
            return;
        }

        _journal.WriteMutation(_writer, current =>
        {
            if (!OwnershipTransitions.TryPlanRootObservation(current,
                    new RootObservation(facts.Root, facts.ExitCode, facts.Signal, facts.CoreDumped,
                        facts.RawWaitPresent, facts.RawWaitStatus),
                    out var afterRoot, out var rootError))
            {
                throw new InvalidOperationException(rootError);
            }

            return afterRoot!;
        });
    }

    private static int Fd(SafeFileHandle handle) => checked((int)handle.DangerousGetHandle());

    private static long NowNanoseconds()
    {
        var timestamp = Stopwatch.GetTimestamp();
        return (timestamp / Stopwatch.Frequency * 1_000_000_000L)
            + (timestamp % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency);
    }

    private sealed class OwnerClock : ITransactionClock
    {
        public long NowNanoseconds => HeldReleasePhase.NowNanoseconds();
    }
}
