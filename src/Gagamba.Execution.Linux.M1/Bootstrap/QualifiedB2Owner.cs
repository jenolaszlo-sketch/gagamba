using System.Text.Json;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class QualifiedB2Owner
{
    internal static int Run(int fd,Guid execution,Guid generation)
    {
        using var life=new SafeFileHandle((IntPtr)fd,true);
        var seq=new OwnerProtocolSequencer(execution,generation);
        int recovery=RecoveryInterop.GetParentPid();
        if(!RecoveryInterop.IsUnprivileged || !RecoveryInterop.Protect(fd) || !RecoveryInterop.VerifyPeer(fd,recovery,RecoveryInterop.GetUid()) ||
            !PidFd.TryOpen(recovery,out int recoveryFd,out _)) return 3;
        using var recoveryPin=new SafeFileHandle((IntPtr)recoveryFd,true);
        bool stopped=false,ack=false,bootstrapReceived=false;string cause="RecoveryLifelineLost";string? evidence=null;
        long lifetime=long.MaxValue;
        void Stop(string reason)
        {
            if(stopped) return;stopped=true;cause=reason;
            ack=FdPassing.TrySendMessage(fd,seq.Create(OwnerProtocolMessageType.B2StopObserved),out _);
        }
        void Service()
        {
            if(stopped) throw new IOException("B2 owner stopped");
            if(!DirectPeerIdentity.LiveReference(recoveryFd,B2ProcessIdentity.Identity(recovery)) ||
                RecoveryInterop.GetParentPid()!=recovery || InstallationDeadline.Now>=lifetime)
            {Stop("RecoveryLostOrLifetimeExpired");throw new IOException("B2 owner lifetime");}
            if(bootstrapReceived)
            {
                if(FdPassing.TryReceiveMessage(fd,0,out var f,out var error))
                {
                    if(!seq.TryAccept(f,OwnerProtocolMessageType.B2SafetyStop,out var m,out _) || m!.Payload.Length!=0)
                        Stop("InvalidRecoveryFrame");else Stop("RecoveryStopRequested");
                }
                else if(!RecoveryInterop.IsTransientReceiveError(error)) Stop("RecoveryLifelineLost");
                if(stopped) throw new IOException("stop preempts owner evidence");
            }
        }
        try
        {
            B2ProcessIdentity.Procfs();B2ProcessIdentity.RequireLive(recoveryFd,recovery);B2ProcessIdentity.CheckProcess(recovery,null);
            if(!FdPassing.TryReceiveMessage(fd,5000,out var startFrame,out _) ||
                !seq.TryAccept(startFrame,OwnerProtocolMessageType.B2StartLifeline,out var startMessage,out _)) return 3;
            var start=JsonSerializer.Deserialize<B2OwnerStart>(startMessage!.Payload);
            if(start is null || start.RecoveryPid!=recovery || !Path.IsPathFullyQualified(start.EvidenceDirectory)) return 3;
            evidence=start.EvidenceDirectory;
            Service();
            if(!FdPassing.TrySendMessage(fd,seq.Create(OwnerProtocolMessageType.B2LifelineReady),out _)) return 3;
            lifetime=checked(InstallationDeadline.Now+35_000_000_000L);
            B2Binding binding;SafeFileHandle controllerPin;
            long bootstrapDeadline=Math.Min(InstallationDeadline.Create(),lifetime);
            while(true)
            {
                Service();
                if(FdPassing.TryReceiveRecord(fd,InstallationDeadline.Remaining(bootstrapDeadline,10),out var record,out var error))
                {
                    using(record!)
                    {
                        if(OwnerProtocol.TryDecode(record!.Payload,execution,generation,out var stop,out _) && stop!.Type==OwnerProtocolMessageType.B2SafetyStop)
                        {
                            if(record.DescriptorCount!=0 || stop.Payload.Length!=0 || !seq.TryAccept(record.Payload,OwnerProtocolMessageType.B2SafetyStop,out _,out _))
                                throw new IOException("invalid stop");Stop("RecoveryStopRequested");throw new IOException("stopped before bootstrap");
                        }
                        if(!seq.TryAccept(record.Payload,OwnerProtocolMessageType.B2EchoBootstrap,out var message,out _)) throw new IOException("bootstrap transition");
                        binding=InstallationWire.ReadBinding(message!.Payload);InstallationDeadline.Validate(binding.DeadlineNs);
                        if(binding.OwnerPid!=Environment.ProcessId || binding.RecoveryPid!=recovery) throw new IOException("bootstrap roles");
                        controllerPin=B2ProcessIdentity.ReceivePin(record,binding.ControllerPid);break;
                    }
                }
                if(!RecoveryInterop.IsTransientReceiveError(error)) throw new IOException("bootstrap lost");
            }
            bootstrapReceived=true;
            using(controllerPin)
            {
                long deadline=Math.Min(binding.DeadlineNs,InstallationDeadline.Now+5_000_000_000L);
                void Live()
                {Service();B2ProcessIdentity.RequireLive(controllerPin.DangerousGetHandle().ToInt32(),binding.ControllerPid);}
                using var socket=B2EvidenceSocket.Connect(binding.Session,deadline,Live);
                B2ProcessIdentity.VerifyPeer(socket.Descriptor,controllerPin.DangerousGetHandle().ToInt32(),binding,false);
                var evidenceSequence=new OwnerProtocolSequencer(execution,generation);
                var bytes=socket.Receive(evidenceSequence,OwnerProtocolMessageType.B2OwnerExpectation,B2ProcessIdentity.Identity(binding.ControllerPid),
                    controllerPin.DangerousGetHandle().ToInt32(),deadline,Live);
                var expected=InstallationWire.ReadExpectation(bytes,binding.Session);
                var selection=InstallationDeadline.Verify(()=>LoadedInstallation.Verify("owner",expected),deadline,Live);
                Live();B2ProcessIdentity.VerifyPeer(socket.Descriptor,controllerPin.DangerousGetHandle().ToInt32(),binding,false);
                socket.Send(evidenceSequence,OwnerProtocolMessageType.OwnerInstallationEcho,InstallationWire.Echo(selection.Identity,1),deadline,Live);
            }
            // The independent original recovery pin/lifeline remains until stop or original lifetime.
            while(!stopped) {Service();Thread.Sleep(10);}
        }
        catch {Stop(cause=="RecoveryLifelineLost"?"InstallationRefused":cause);}
        using var diagnostics=evidence is null?null:new RecoveryDiagnosticWriter(evidence);
        diagnostics?.Post("owner-stop.json",new{OwnerPid=Environment.ProcessId,RecoveryPid=recovery,
            Cause=cause,StopCount=1,AcknowledgementSent=ack,ReleaseAuthority=false,NativeStop="Unproven"});
        return 0;
    }
}
