using System.Diagnostics;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class QualifiedB2Recovery
{
    internal static int Run(RecoveryConfiguration config,M1RecoveryScope scope,RecoveryWire wire,int control,int controllerFd,
        LoadedInstallation selection)
    {
        long deadline=config.B2DeadlineNs!.Value;InstallationDeadline.Validate(deadline);
        using var diagnostics=new RecoveryDiagnosticWriter(config.EvidenceDirectory);
        var obligation=new RecoveryObligation();Process? owner=null;int ownerPin=-1;
        var pair=QualifiedLauncher.Pair();using var life=new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)pair.Parent,true);
        var seq=new OwnerProtocolSequencer(config.Execution,config.Generation);var custody=new object();
        bool boundSent=false,controlUsable=true,ownerUsable=true,controllerLost=false,ownerExited=false,ready=false,stopSent=false,confirmed=false,killed=false;
        long lifetime=long.MaxValue;B2RecoveryReadyBarrier? barrier=null;B2Binding? binding=null;
        RecoverySnapshot Snapshot()=>new(Environment.ProcessId,owner?.Id??0,config.ControllerPid,RecoveryInterop.GetSid(0),
            scope.SubreaperEnabled,controllerLost,ownerExited,obligation.Count,obligation.Cause,obligation.Deadline,obligation.OwnerAcknowledged);
        void Stop(string cause)
        {
            barrier?.Stop(cause);
            if(!obligation.Request(cause,Stopwatch.GetTimestamp())) return;
            if(owner is not null && ownerUsable)
            {
                stopSent=FdPassing.TrySendMessage(pair.Parent,seq.Create(OwnerProtocolMessageType.B2SafetyStop),out _);
                if(!stopSent) {life.Dispose();ownerUsable=false;}
            }
        }
        void Service()
        {
            if(!PidFd.TryExited(controllerFd,0,out bool exited,out _) || exited) {controllerLost=true;Stop("ControllerExited");}
            if(owner is not null && (!scope.TryOwnerPidFdExited(owner,0,out exited,out _) || exited))
            {ownerExited=scope.WaitForOwner(owner,TimeSpan.Zero).Exited;Stop("OwnerExited");}
            string error="control record timed out";
            if(controlUsable && FdPassing.TryReceiveMessage(control,0,out var frame,out error))
            {
                if(!wire.Accept(frame,out var type,out var payload)) {controlUsable=false;Stop("InvalidControllerFrame");}
                else if(type==RecoveryMessage.Stop) Stop("ControllerStop");
                else if(type==RecoveryMessage.B2OwnerEchoAccepted && boundSent && owner is not null && !ready && !confirmed && obligation.Count==0)
                {try{barrier!.Confirm(payload,InstallationDeadline.Now);confirmed=true;}catch{Stop("InvalidConfirmation");}}
                else if(type==RecoveryMessage.Inspect && ready)
                {if(!FdPassing.TrySendMessage(control,wire.Create(RecoveryMessage.Snapshot,JsonSerializer.SerializeToUtf8Bytes(Snapshot())),out _)) Stop("ControllerTransportLost");}
                else Stop("UnexpectedControllerTransition");
            }
            else if(controlUsable)
            {
                if(!RecoveryInterop.IsTransientReceiveError(error)) {controlUsable=false;controllerLost=true;Stop("InvalidControllerTransport");}
                var poll=new DirectUnix.PollFd{Fd=control,Events=0x2000};
                if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0 || poll.Revents!=0) {controlUsable=false;controllerLost=true;Stop("ControllerTransportLost");}
            }
            if(owner is not null && ownerUsable && stopSent && FdPassing.TryReceiveMessage(pair.Parent,0,out var ack,out _))
            {if(seq.TryAccept(ack,OwnerProtocolMessageType.B2StopObserved,out var ackMessage,out _) && ackMessage!.Payload.Length==0) obligation.OwnerAcknowledged=true;else Stop("OwnerProtocolLost");}
            if(!ready && InstallationDeadline.Now>=deadline) Stop("InstallationExpired");
            if(InstallationDeadline.Now>=lifetime) Stop("BootstrapLifetimeExpired");
        }
        void Positive() {Service();if(obligation.Count!=0) throw new IOException("stop preempts B2 qualification");_=InstallationDeadline.Remaining(deadline);}
        try
        {
            Positive();
            var plan=selection.OwnerLaunch??throw new IOException("verified owner launch plan missing");
            var spawn=scope.TrySpawnOwner(plan.Assembly,
                ["--exec",config.Execution.ToString("D"),"--lease",config.Generation.ToString("D"),"--b2-lifeline","2"],pair.Child,plan.Runtime,qualified:true,installedLaunch:plan);
            pair=(pair.Parent,-1);
            if(!spawn.Ok) throw new IOException("original qualified owner spawn refused");
            owner=spawn.Process!;ownerPin=spawn.PidFd;
            binding=new B2Binding(config.Session,config.ControllerPid,Environment.ProcessId,owner.Id,deadline);
            barrier=new(binding,selection.Identity);
            if(!FdPassing.TrySendMessage(pair.Parent,seq.Create(OwnerProtocolMessageType.B2StartLifeline,
                JsonSerializer.SerializeToUtf8Bytes(new B2OwnerStart(Environment.ProcessId,config.EvidenceDirectory))),out _)) throw new IOException("B2 start lost");
            long readyDeadline=Math.Min(deadline,InstallationDeadline.Now+3_000_000_000L);
            while(true)
            {
                Positive();
                if(FdPassing.TryReceiveMessage(pair.Parent,InstallationDeadline.Remaining(readyDeadline,10),out var frame,out var error))
                {
                    if(!seq.TryAccept(frame,OwnerProtocolMessageType.B2LifelineReady,out var m,out _) || m!.Payload.Length!=0)
                        throw new IOException("B2 lifeline ready refused");break;
                }
                if(!RecoveryInterop.IsTransientReceiveError(error)) throw new IOException("B2 lifeline lost");
            }
            lifetime=checked(InstallationDeadline.Now+30_000_000_000L);Positive();
            barrier.Lifeline(InstallationDeadline.Now);
            B2ProcessIdentity.RequireLive(ownerPin,owner.Id);B2ProcessIdentity.CheckProcess(owner.Id,Environment.ProcessId);
            lock(custody)
            {
                B2ProcessIdentity.RequireLive(controllerFd,config.ControllerPid);
                if(!FdPassing.TrySendTransfer(pair.Parent,[controllerFd],seq.Create(OwnerProtocolMessageType.B2EchoBootstrap,InstallationWire.Binding(binding)),out _))
                    throw new IOException("B2 controller reference lost");
            }
            Positive();
            lock(custody)
                if(!scope.SendOriginalB2OwnerReference(owner,control,wire.Create(RecoveryMessage.B2OwnerBound,InstallationWire.Binding(binding))))
                    throw new IOException("B2 owner reference lost");
            barrier.ReferencesSent(InstallationDeadline.Now);boundSent=true;
            while(!confirmed) {Positive();Thread.Sleep(2);}
            Positive();
            var snapshot=JsonSerializer.SerializeToUtf8Bytes(Snapshot());barrier.Ready(snapshot,InstallationDeadline.Now);
            Positive();
            if(!FdPassing.TrySendMessage(control,wire.Create(RecoveryMessage.Ready,snapshot),out _)) throw new IOException("qualified Ready lost");
            ready=true;
            diagnostics.Post("ready.json",Snapshot());
        }
        catch {Stop("InstallationRefused");}
        finally {if(pair.Child>=0) PreparationInterop.CloseFd(pair.Child);}
        if(owner is null) return 3;
        RecoverySnapshot? last=null;
        while(true)
        {
            Service();
            if(ownerUsable && !stopSent)
            {
                var poll=new DirectUnix.PollFd{Fd=pair.Parent,Events=1|0x2000};
                if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0 || poll.Revents!=0) Stop("OwnerLifelineLost");
            }
            if(obligation.Count!=0)
            {
                if(!ownerExited && !killed && Stopwatch.GetTimestamp()>=obligation.Deadline)
                {killed=true;_=PidFd.TrySignal(ownerPin,9,out _);}
                var s=Snapshot();if(s!=last) {diagnostics.Post("stop.json",s);last=s;}
                if(ownerExited) {diagnostics.Post("owner-exit.json",s);return 0;}
            }
            Thread.Sleep(10);
        }
    }
}
