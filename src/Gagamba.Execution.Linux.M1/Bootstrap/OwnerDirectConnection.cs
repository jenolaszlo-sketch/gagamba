using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed class OwnerDirectConnection : IDisposable
{
    internal OwnerProtocolChannel Channel { get; }
    internal OwnerSafetyMonitor Safety { get; }
    private OwnerDirectConnection(OwnerProtocolChannel channel,OwnerSafetyMonitor safety) {Channel=channel;Safety=safety;}
    internal static OwnerDirectConnection Accept(OwnerPreparationSpecification request,OwnerProtocolChannel recovery)
    {
        var identity=request.DirectIdentity!;
        identity.Controller.Validate(); identity.Recovery.Validate();
        recovery.RecoveryOnly=true;
        // The concrete parent is the trusted recovery; never infer controller authority from parentage.
        if(identity.Recovery.Pid!=RecoveryInterop.GetParentPid() || identity.Recovery!=(DirectProcessIdentity.Current() with {Pid=identity.Recovery.Pid})
            || !RecoveryInterop.VerifyPeer(recovery.Socket,identity.Recovery.Pid,identity.Recovery.Uid)
            || !PidFd.TryOpen(identity.Recovery.Pid,out var recoveryFd,out _))
            throw new IOException("Original recovery parent identity refused");
        var recoveryReference=new SafeFileHandle((IntPtr)recoveryFd,true);
        OwnerSafetyMonitor? safety=null;
        int[] fds=[];
        try
        {
            if(!DirectPeerIdentity.LiveReference(recoveryFd,identity.Recovery))
                throw new IOException("Original recovery lifetime unavailable");
            // Payload is a bounded full association, and exactly two disjoint non-release roles are required.
            if(!recovery.TryReceiveTransfer(OwnerProtocolMessageType.DirectBootstrap,2,
                DirectBootstrapPacket.Encode(new DirectAssociation(request.ExecutionId,request.StoreGenerationId,
                    identity.OriginalSession,identity.Controller,DirectProcessIdentity.Current(),identity.Recovery)).Length,
                out var bootstrap,out fds,out _)) throw new IOException("Original controller lifetime evidence unavailable");
            var association=DirectBootstrapPacket.Decode(bootstrap!.Payload);
            if(association!=new DirectAssociation(request.ExecutionId,request.StoreGenerationId,identity.OriginalSession,
                identity.Controller,DirectProcessIdentity.Current(),identity.Recovery)
                || !DirectPeerIdentity.LiveReference(fds[0],identity.Controller))
                throw new IOException("Immutable direct association refused");
            uint size=4;
            uint credentialSize=12;
            if(DirectUnix.IntOption(fds[1],1,3,out var type,ref size)!=0 || type!=5 || !PreparationInterop.HasCloexec(fds[1])
                || !RecoveryInterop.Protect(fds[1])
                || DirectUnix.PeerCredentials(fds[1],1,17,out var peer,ref credentialSize)!=0 || credentialSize!=12
                || peer.Pid!=identity.Recovery.Pid || peer.Uid!=identity.Recovery.Uid || peer.Gid!=identity.Recovery.Gid
                || PreparationInterop.StatFd(fds[1],out var lifeStat)!=0 || PreparationInterop.StatFd(recovery.Socket,out var controlStat)!=0
                || (lifeStat.Device==controlStat.Device && lifeStat.Inode==controlStat.Inode))
                throw new IOException("Dedicated recovery safety role refused");
            safety=new OwnerSafetyMonitor(fds[0],recoveryFd,fds[1],identity,request.AcceptedAtNs,request.ExpiresAtNs);
            fds=[]; recoveryReference.SetHandleAsInvalid();
            using var listener=DirectOwnerListener.Create("/tmp");
            if(!recovery.TrySend(OwnerProtocolMessageType.DirectRendezvous,Encoding.UTF8.GetBytes(listener.Address),out _))
                throw new IOException("Rendezvous address publication failed");
            var deadline=DirectStream.Deadline((int)Math.Clamp((Math.Min(request.ExpiresAtNs,request.AcceptedAtNs+10_000_000_000L)
                -OwnerSafetyMonitor.Now())/1_000_000L,0,5000));
            for(var attempt=0;attempt<DirectOwnerListener.MaxAttempts && !safety.StopLatched;attempt++)
            {
                var stream=listener.Accept(deadline);
                try {using var checkedPeer=DirectPeerIdentity.Verify(stream.Descriptor,association.Controller,safety.ControllerDescriptor);}
                catch(IOException)
                {
                    if(Int01Qualification.Is(request.QualificationFault,"actual-recovery-release-impersonation"))
                    {
                        uint peerSize=12;
                        if(DirectUnix.PeerCredentials(stream.Descriptor,1,17,out var rejected,ref peerSize)==0)
                            Int01Qualification.Write(request.QualificationFault!,"owner-refused-recovery-peer",new { ActualPid=rejected.Pid, ExpectedControllerPid=association.Controller.Pid, ExpectedRecoveryPid=association.Recovery.Pid });
                    }
                    stream.Dispose();continue;
                }
                // A verified original-peer attempt consumes this association even when its ACK is lost.
                // Only unauthenticated OS peers may be rejected without consuming that attempt.
                {
                    var session=DirectSession.Establish(stream,association,safety.ControllerDescriptor,true,deadline);
                    try
                    {
                        listener.SealAssociation();
                        var channel=new OwnerProtocolChannel(session,request.ExecutionId,request.LeaseGeneration);
                        safety.BindController(channel);
                        return new OwnerDirectConnection(channel,safety);
                    }
                    catch { session.Dispose(); throw; }
                }
            }
            throw new IOException("Original direct association refused");
        }
        catch { safety?.Dispose(); foreach(var fd in fds) PreparationInterop.CloseFd(fd); throw; }
        finally {recoveryReference.Dispose();}
    }
    public void Dispose() => Channel.Dispose(); // Safety is disposed first by transaction while writer is still live.
}
