using System.Collections.Immutable;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

// Original controller's direct session. RecoveryConnection carries bootstrap/lifetime/stop
// facts only. This object never obtains the owner's capability, store or physical writer.
internal sealed class IntegratedB3Client : IDisposable
{
    private readonly object _exchange=new();
    private readonly object _publication=new();
    private RecoveryConnection? _recovery;
    private OwnerProtocolChannel? _channel;
    private OwnerPreparationOutcome? _held;
    private bool _stopRequested;
    private volatile bool _disposed;
    internal RecoverySnapshot? InspectRecovery()=>Volatile.Read(ref _recovery)?.Inspect();
    internal bool IsLive
    {
        get
        {
            // Never take the long request-exchange gate from the facade ordering gate.
            var recovery=Volatile.Read(ref _recovery);var channel=Volatile.Read(ref _channel);
            var fd=recovery?.OwnerReference;var retained=false;
            try
            {
                if(_disposed || fd is null || recovery?.Rendezvous is not { } r) return false;
                fd.DangerousAddRef(ref retained);
                return !_disposed && DirectPeerIdentity.LiveReference(fd.DangerousGetHandle().ToInt32(),r.Association.Owner)
                    && channel?.IsAlive==true;
            }
            catch(ObjectDisposedException) {return false;}
            finally {if(retained) fd!.DangerousRelease();}
        }
    }
    internal void RequestNativeStop()
    {
        RecoveryConnection? recovery;
        lock(_publication) {_stopRequested=true;recovery=_recovery;}
        _=recovery?.Send(RecoveryMessage.Stop);
    }
    internal OwnerPreparationOutcome Prepare(B3FixtureProfile profile,Guid execution,Guid lease,Guid issuer,Guid generation,
        long acceptedAt,long expiresAt,Func<IReadOnlyList<StagedObjectDescription>,(RestrictedManifest Manifest,PreparationIdentity Identity)> authorize)
    {
        var integrated=profile.IntegratedRecovery ?? throw new IOException("Missing integrated experimental pins");
        var config=new RecoveryConfiguration(1,execution,lease,Guid.NewGuid(),Environment.ProcessId,RecoveryInterop.GetUid(),[],
            integrated.Recovery,integrated.Owner,integrated.EvidenceDirectory,profile.DelegatedParentPath,
            new IntegratedRecoverySpecification(profile with {IntegratedRecovery=null},issuer,generation,acceptedAt,expiresAt,
                DirectProcessIdentity.Current(),integrated.Runtime));
        var recovery=RecoveryConnection.Start(config);
        lock(_publication)
        {
            if(_disposed) {_=recovery.Send(RecoveryMessage.Stop);recovery.Dispose();throw new IOException("Disposed original controller");}
            _recovery=recovery;if(_stopRequested) _=recovery.Send(RecoveryMessage.Stop);
        }
        lock(_exchange)
        {
            if(_disposed || _stopRequested) throw new IOException("Preparation stopped before direct association");
            var rendezvous=recovery.Rendezvous ?? throw new IOException("Missing original owner association");
            var deadline=DirectStream.Deadline((int)Math.Clamp((Math.Min(expiresAt,acceptedAt+10_000_000_000L)-OwnerSafetyMonitor.Now())/1_000_000L,0,5000));
            var session=DirectSession.Establish(DirectOwnerListener.Connect(rendezvous.Address,deadline),rendezvous.Association,
                recovery.OwnerReference!.DangerousGetHandle().ToInt32(),false,deadline);
            _channel=new OwnerProtocolChannel(session,execution,lease);
            if(!_channel.TryReceive(OwnerProtocolMessageType.StagedObjects,out var objects,out var error)
                || !OwnerPreparationSpecificationCodec.TryDecodeStagedObjects(objects!.Payload,out var staged,out error))
                throw new IOException("Integrated staged closure unavailable");
            var admitted=authorize(staged!);
            if(admitted.Identity.ExecutionId!=execution || admitted.Identity.IssuerId!=issuer || admitted.Identity.GenerationId!=generation
                || !admitted.Identity.ManifestDigest.AsSpan().SequenceEqual(ManifestCodec.ComputeDigestBytes(admitted.Manifest)))
                throw new IOException("Original controller manifest binding refused");
            if(!_channel.TrySend(OwnerProtocolMessageType.ManifestAuthorization,OwnerManifestAuthorizationCodec.Encode(
                new OwnerManifestAuthorization(admitted.Manifest,admitted.Identity)),out error)
                || !_channel.TryReceive(OwnerProtocolMessageType.PreparedDescription,out var prepared,out error)
                || !OwnerPreparationSpecificationCodec.TryDecodePreparedDescription(prepared!.Payload,out var description,out error)
                || description is null || description.ExecutionId!=execution || description.IssuerId!=issuer || description.GenerationId!=generation
                || description.PreparationId!=admitted.Identity.PreparationId
                || !description.ManifestDigest.AsSpan().SequenceEqual(admitted.Identity.ManifestDigest.AsSpan())
                || !_channel.TrySend(OwnerProtocolMessageType.PreparedAcknowledgement,[],out error)
                || !_channel.TryReceive(OwnerProtocolMessageType.HeldReady,out var held,out error))
                throw new IOException("Integrated preparation/retention/HELD barrier failed");
            var payload=held!.Payload;
            if(payload.Length<17 || payload[0]!=1 || !CoordinatorProtocol.TryDecode(payload.AsSpan(17),execution,
                description.PreparationId,description.ManifestDigest,1,out var frame,out error)
                || !CoordinatorProtocol.TryDecodeHeldReport(frame!,out var report,out error))
                throw new IOException("Integrated HELD evidence refused");
            var setup=System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(payload.AsSpan(1,8));
            var overall=System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(payload.AsSpan(9,8));
            if(setup!=Math.Min(acceptedAt+10_000_000_000L,expiresAt) || overall!=Math.Min(acceptedAt+30_000_000_000L,expiresAt)
                || setup<=acceptedAt || overall<=setup) throw new IOException("Original deadlines changed");
            var snapshot=recovery.Inspect();
            if(snapshot is null || snapshot.OwnerPid!=rendezvous.Association.Owner.Pid || snapshot.RecoveryPid!=recovery.Process.Id
                || snapshot.RetainedDomains!=1 || snapshot.ControllerLost || snapshot.OwnerExited || snapshot.StopCount!=0)
                throw new IOException("Actual recovery retention unavailable");
            // Numeric Process is a view only. The retained bootstrap pidfd remains the lifetime authority.
            var owner=System.Diagnostics.Process.GetProcessById(rendezvous.Association.Owner.Pid);
            return _held=new OwnerPreparationOutcome(false,description,owner,null,string.Empty,true,report,
                payload[17..],true,setup,overall);
        }
    }
    internal HeldOwnerReleaseOutcome Release(ImmutableArray<byte> correlation)
    {
        lock(_exchange)
        {
            if(_disposed || _channel is null || _held?.Description is not { } description)
                return new(false,null,null,"original direct release session unavailable");
            var request=OwnerHeldWireCodec.RequestFromIdentity(description.ExecutionId,description.PreparationId,description.GenerationId,
                description.ManifestDigest,correlation.AsSpan(),!correlation.IsDefault);
            if(!_channel.TrySend(OwnerProtocolMessageType.ReleaseRequest,OwnerHeldWireCodec.EncodeReleaseRequest(request),out var error)
                || !_channel.TryReceive(OwnerProtocolMessageType.ReleaseDecision,out var message,out error)
                || !OwnerHeldWireCodec.TryDecodeReleaseDecision(message!.Payload,out var decision,out error))
                return new(false,null,null,"direct release acknowledgement unavailable; no resend");
            OwnerExecutionFacts? facts=null;
            if(decision!.Kind==ReleaseDecisionKind.Released && _channel.TryReceive(OwnerProtocolMessageType.ExecutionOutcome,out message,out error))
                _=OwnerHeldWireCodec.TryDecodeExecutionOutcome(message!.Payload,out facts,out _,out error);
            return new(true,decision,facts,string.Empty);
        }
    }
    internal HeldOwnerStopOutcome Stop(StopCauseKind cause)
    {
        RequestNativeStop();
        lock(_exchange)
        {
            if(_disposed || _channel is null || !_channel.TrySend(OwnerProtocolMessageType.StopRequest,[(byte)cause],out _)
                || !_channel.TryReceive(OwnerProtocolMessageType.StopDecision,out var response,out _)
                || !OwnerHeldWireCodec.TryDecodeStopDecision(response!.Payload,out var decision,out _))
                return new(false,null,"native stop submitted; owner arbitration acknowledgement unknown");
            return new(true,decision,string.Empty);
        }
    }
    internal RestrictedCleanup Cleanup()
    {
        RequestNativeStop();
        lock(_exchange)
        {
            if(_disposed) return RestrictedCleanup.Unknown;
            lock(_publication) _disposed=true;
            if(_channel is not null)
            {
                if(_channel.TrySend(OwnerProtocolMessageType.AbortHeld,[],out _))
                    _=_channel.TryReceiveAllowedWithin(1000,out _,out _,OwnerProtocolMessageType.HeldAborted);
                _channel.Dispose();
            }
            var deadline=DirectStream.Deadline(8000); RecoverySnapshot? last=null;
            while(DirectStream.Remaining(deadline)>0)
            {
                var snapshot=_recovery?.Inspect();
                if(snapshot is not null) last=snapshot;
                if(last is {OwnerExited:true,OwnerReaped:true,WriterFence:true,JournalFinalized:true}) break;
                if(_recovery?.Process.HasExited==true) break;
                Thread.Sleep(10);
            }
            _held?.Owner?.Dispose();
            _recovery?.Dispose();
            return new RestrictedCleanup(last?.DomainEmpty==true?RestrictedEvidenceState.Confirmed:RestrictedEvidenceState.Unknown,
                last?.OwnerReaped==true?RestrictedEvidenceState.Confirmed:RestrictedEvidenceState.Unknown,
                last?.StagingRemoved==true?RestrictedEvidenceState.Confirmed:RestrictedEvidenceState.Unknown,
                last is {WriterFence:true,JournalFinalized:true}?RestrictedEvidenceState.Confirmed:RestrictedEvidenceState.Unknown);
        }
    }
    public void Dispose() => _=Cleanup();
}
