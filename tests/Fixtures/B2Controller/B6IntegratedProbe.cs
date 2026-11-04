using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

// Three real actors, 9/10, rendezvous8 and actual GID1/OwnerProtocolChannel.
// This is a writerless authentication/order probe, NOT a preparation transaction:
// it never emits WriterReady, creates a domain, reaches HELD or supplies authority.
internal sealed partial class B6TopologyProbe
{
    private int ControllerIntegrated()
    {
        using var supervisor=new M1RecoveryScope();Check(supervisor.TryInitialize().Ok,"probe subreaper");
        var runtime=B6EvidenceProbe.RuntimePin();Deadline=InstallationDeadline.Create();
        var pair=QualifiedLauncher.Pair();using var control=new SafeFileHandle((IntPtr)pair.Parent,true);
        using var recovery=QualifiedLauncher.Start(runtime,Entry,Args("recovery"),(pair.Child,3,"GAGAMBA_B2_CONTROL_FD"));PreparationInterop.CloseFd(pair.Child);
        using var rpin=Pin(recovery.Id);SafeFileHandle? opin=null;OwnerProtocolChannel? channel=null;
        var wire=new RecoveryWire(Execution,Lease,2,true,true);bool accepted=false,staged=false,echo=false;string? refusal=null;
        void Live(){_=InstallationDeadline.Remaining(Deadline);B2ProcessIdentity.RequireLive(rpin.DangerousGetHandle().ToInt32(),recovery.Id);}
        try
        {
            var auth=new byte[48];Session.TryWriteBytes(auth);Send(pair.Parent,wire.Create(RecoveryMessage.Associate,auth));
            using(var record=Read(pair.Parent,Live))
            {
                var payload=Accept(record,wire,RecoveryMessage.InstallationEchoRequest);
                Check(InstallationWire.CompareEcho(payload,Identity,0,true)==InstallationMismatch.None,"integrated recovery identity refused");
                Send(pair.Parent,wire.Create(RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(Identity,InstallationMismatch.None)));
            }
            OwnerRendezvous rendezvous;
            using(var record=Read(pair.Parent,Live))
            {
                rendezvous=JsonSerializer.Deserialize<OwnerRendezvous>(Accept(record,wire,RecoveryMessage.Rendezvous,1))!;
                var a=rendezvous.Association;a.Validate();
                Check(a.Execution==Execution && a.Generation==Lease && a.OriginalSession==Session && a.Controller==DirectProcessIdentity.Current() && a.Recovery==B2ProcessIdentity.Identity(recovery.Id),"rendezvous association");
                opin=B2ProcessIdentity.ReceivePin(record,a.Owner.Pid);
            }
            var association=rendezvous.Association;
            if(Mode=="integrated-wrong-session")association=association with{OriginalSession=Guid.NewGuid()};
            long directDeadline=DirectStream.Deadline(InstallationDeadline.Remaining(Deadline));
            var direct=DirectSession.Establish(DirectOwnerListener.Connect(rendezvous.Address,directDeadline),association,opin.DangerousGetHandle().ToInt32(),false,directDeadline);
            channel=new OwnerProtocolChannel(direct,Execution,Lease){InstallationOwner=false};Event("GID1-established",new{OwnerPid=association.Owner.Pid,PeerPinLive=DirectPeerIdentity.LiveReference(direct.PeerProcessDescriptor,association.Owner)});
            if(Mode=="integrated-controller-death"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode=="integrated-recovery-death")_=PidFd.TrySignal(rpin.DangerousGetHandle().ToInt32(),9,out _);
            if(Mode=="integrated-stop-stalled")
            {
                WaitEvent("owner","worker-blocked",Live);Send(pair.Parent,wire.Create(RecoveryMessage.Stop));
            }
            Check(channel.TryReceiveAllowedWithin(InstallationDeadline.Remaining(Deadline),out var message,out _,OwnerProtocolMessageType.OwnerInstallationEcho),"first qualified owner message must be36");
            Check(InstallationWire.CompareEcho(message!.Payload,Identity,1,true)==InstallationMismatch.None,"integrated owner identity refused");
            Live();B2ProcessIdentity.RequireLive(opin.DangerousGetHandle().ToInt32(),association.Owner.Pid);echo=true;Event("accept36");
            Check(channel.TryReceiveAllowedWithin(InstallationDeadline.Remaining(Deadline),out var objects,out _,OwnerProtocolMessageType.StagedObjects) &&
                OwnerPreparationSpecificationCodec.TryDecodeStagedObjects(objects!.Payload,out var description,out _) && description!.Count==1,"staging description after36 required");
            staged=true;Event("description-decoded",new{Authority=false,ActualStaging=false});accepted=true;
        }
        catch(Exception e){refusal=e.Message;Event("refused",new{Reason=e.Message});}
        finally
        {
            channel?.Dispose();_=FdPassing.TrySendMessage(pair.Parent,wire.Create(RecoveryMessage.Stop),out _);control.Dispose();opin?.Dispose();
            bool reaped=QualifiedLauncher.TryReap(recovery,8000,out int code);if(!reaped){_=PidFd.TrySignal(rpin.DangerousGetHandle().ToInt32(),9,out _);reaped=QualifiedLauncher.TryReap(recovery,3000,out code);}
            string meta=Path.Combine(Dir,"owner.jsonl");if(File.Exists(meta))
            {int pid=JsonDocument.Parse(File.ReadLines(meta).First()).RootElement.GetProperty("Pid").GetInt32();long end=InstallationDeadline.Now+3_000_000_000L;while(WaitPid(pid,out _,1)==0 && InstallationDeadline.Now<end)Thread.Sleep(2);}
            Result(new{Scope="PN integrated GID1/order mechanism; no writer, HELD, S2 or qualified production return",Mode,Accepted=accepted,EchoAccepted=echo,DescriptionDecoded=staged,Held=false,B2EvidenceSockets=0,RecoveryReaped=reaped,RecoveryExitCode=code,ReleaseAuthority=false,PhysicalReleaseAttempts=0,Refusal=refusal});
        }
        return 0;
    }
    private int RecoveryIntegrated()
    {
        Check(RecoveryInterop.Protect(3) && RecoveryInterop.VerifyPeer(3,RecoveryInterop.GetParentPid(),RecoveryInterop.GetUid()),"original C socket");
        using var control=new SafeFileHandle((IntPtr)3,true);int controller=RecoveryInterop.GetParentPid();using var cpin=Pin(controller);
        Check(RecoveryInterop.DetachLifetime(),"detached R");using var scope=new M1RecoveryScope();Check(scope.TryInitialize().Ok,"R subreaper");
        var wire=new RecoveryWire(Execution,Lease,2,true,false);var seq=new OwnerProtocolSequencer(Execution,Lease);var obligation=new RecoveryObligation();
        Process? owner=null;int opin=-1;SafeFileHandle? protocol=null,safety=null;bool reaped=false;string? refusal=null;
        void Service()
        {
            _=InstallationDeadline.Remaining(Deadline);B2ProcessIdentity.RequireLive(cpin.DangerousGetHandle().ToInt32(),controller);
            if(FdPassing.TryReceiveMessage(3,0,out var f,out var error))
            {Check(wire.Accept(f,out var type,out _) && type==RecoveryMessage.Stop,"stop-only recovery control");throw new IOException("ControllerStop");}
            else Check(RecoveryInterop.IsTransientReceiveError(error),"ControllerTransportLost");
        }
        try
        {
            using(var record=Read(3,()=>B2ProcessIdentity.RequireLive(cpin.DangerousGetHandle().ToInt32(),controller)))_=Accept(record,wire,RecoveryMessage.Associate);
            byte[] echo=InstallationWire.Echo(Identity,0);if(Mode=="integrated-wrong-recovery")echo[32]^=1;
            Send(3,wire.Create(RecoveryMessage.InstallationEchoRequest,echo));
            using(var record=Read(3,()=>B2ProcessIdentity.RequireLive(cpin.DangerousGetHandle().ToInt32(),controller)))InstallationWire.AcceptResponse(Accept(record,wire,RecoveryMessage.InstallationEchoResponse),Identity);
            var runtime=B6EvidenceProbe.RuntimePin();var pair=QualifiedLauncher.Pair();protocol=new((IntPtr)pair.Parent,true);
            var spawn=scope.TrySpawnOwner(Entry,Args("owner"),pair.Child,runtime,qualified:true);Check(spawn.Ok,spawn.Error);owner=spawn.Process!;opin=spawn.PidFd;
            Event("spawn-owner",new{OwnerPid=owner.Id,OriginalObject=true});
            var a=new DirectAssociation(Execution,Lease,Session,B2ProcessIdentity.Identity(controller),B2ProcessIdentity.Identity(owner.Id),DirectProcessIdentity.Current());
            var safe=QualifiedLauncher.Pair();safety=new((IntPtr)safe.Parent,true);
            try{Check(FdPassing.TrySendTransfer(pair.Parent,[cpin.DangerousGetHandle().ToInt32(),safe.Child],seq.Create(OwnerProtocolMessageType.DirectBootstrap,DirectBootstrapPacket.Encode(a)),out _),"32 transfer");Event("send32",new{Roles="original C pidfd, independent safety lifeline",Rights=2});}
            finally{PreparationInterop.CloseFd(safe.Child);}
            string address;
            using(var record=Read(pair.Parent,Service))
            {if(record.DescriptorCount!=0 || !seq.TryAccept(record.Payload,OwnerProtocolMessageType.DirectRendezvous,out var m,out _))throw new IOException("owner listener");address=Encoding.UTF8.GetString(m!.Payload);}
            var rendezvous=new OwnerRendezvous(address,a);byte[] frame=wire.Create(RecoveryMessage.Rendezvous,JsonSerializer.SerializeToUtf8Bytes(rendezvous));
            if(Mode is "integrated-b2-11" or "integrated-b2-12")frame[6]=(byte)(Mode.EndsWith("11",StringComparison.Ordinal)?11:12);
            Check(scope.SendOriginalB2OwnerReference(owner,3,frame),"original O rendezvous reference");Frame("send",frame,1);
            while(true){Service();Check(DirectPeerIdentity.LiveReference(opin,a.Owner),"OwnerExited");Thread.Sleep(2);}
        }
        catch(Exception e)
        {
            refusal=e.Message;obligation.Request(refusal,Stopwatch.GetTimestamp());Event("stop",new{Cause=refusal,Count=obligation.Count,Deadline=obligation.Deadline});
            if(safety is not null)_=FdPassing.TrySendMessage(safety.DangerousGetHandle().ToInt32(),[1],out _);
        }
        finally
        {
            if(owner is not null)
            {
                var exit=scope.WaitForOwner(owner,TimeSpan.FromSeconds(2));
                if(!exit.Exited){_=PidFd.TrySignal(opin,9,out _);exit=scope.WaitForOwner(owner,TimeSpan.FromSeconds(3));}
                reaped=exit.Exited;Event("owner-reaped",new{Exited=exit.Exited,exit.ExitCode});
            }
            protocol?.Dispose();safety?.Dispose();Result(new{Scope="PN integrated authentication/order only",Mode,OwnerPid=owner?.Id,StopCount=obligation.Count,OwnerReaped=reaped,ReleaseAuthority=false,NativeStop="Unproven",RetainedDomains=0,B2EvidenceSockets=0,Refusal=refusal});
        }
        return 0;
    }
    private int OwnerIntegrated()
    {
        int recovery=RecoveryInterop.GetParentPid();using var protocol=new SafeFileHandle((IntPtr)3,true);Check(RecoveryInterop.Protect(3) && RecoveryInterop.VerifyPeer(3,recovery,RecoveryInterop.GetUid()),"original R socket");
        using var originalR=Pin(recovery);var seq=new OwnerProtocolSequencer(Execution,Lease);OwnerSafetyMonitor? monitor=null;bool sent=false,stopped=false;string? refusal=null;
        try
        {
            using var record=Read(3,()=>B2ProcessIdentity.RequireLive(originalR.DangerousGetHandle().ToInt32(),recovery));
            if(record.DescriptorCount!=2 || !seq.TryAccept(record.Payload,OwnerProtocolMessageType.DirectBootstrap,out var m,out _))throw new IOException("32/roles required");
            var a=DirectBootstrapPacket.Decode(m!.Payload);Check(a.Owner==DirectProcessIdentity.Current() && a.Recovery==B2ProcessIdentity.Identity(recovery) && a.OriginalSession==Session && a.Execution==Execution && a.Generation==Lease,"immutable direct association");
            B2ProcessIdentity.RequireLive(record.BorrowDescriptor(0),a.Controller.Pid);
            int[] rights=record.DetachDescriptors();int rcopy=PreparationInterop.Dup(originalR.DangerousGetHandle().ToInt32());
            Check(rcopy>2,"original R duplicate");
            var identity=new OwnerDirectIdentity(Session,a.Controller,a.Recovery,B6EvidenceProbe.RuntimePin());
            monitor=new OwnerSafetyMonitor(rights[0],rcopy,rights[1],identity,InstallationDeadline.Now,Deadline);
            using var listener=DirectOwnerListener.Create("/tmp");Send(3,seq.Create(OwnerProtocolMessageType.DirectRendezvous,Encoding.UTF8.GetBytes(listener.Address)));
            long limit=DirectStream.Deadline(InstallationDeadline.Remaining(Deadline));
            using var direct=DirectSession.Establish(listener.Accept(limit),a,monitor.ControllerDescriptor,true,limit);listener.SealAssociation();Event("GID1-established");
            using var channel=new OwnerProtocolChannel(direct,Execution,Lease){InstallationOwner=true};monitor.BindController(channel);
            void Service(){if(monitor.StopLatched)throw new IOException("independent safety stop");_=InstallationDeadline.Remaining(Deadline);}
            if(Mode=="integrated-stop-stalled")_=InstallationDeadline.Verify(()=>{Event("worker-blocked");new ManualResetEventSlim(false).Wait();return false;},Deadline,Service);
            if(Mode=="integrated-owner-death"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode is "integrated-b2-37" or "integrated-b2-38")
            {
                var type=Mode.EndsWith("37",StringComparison.Ordinal)?OwnerProtocolMessageType.B2OwnerExpectation:OwnerProtocolMessageType.B2EchoBootstrap;
                byte[] p=type==OwnerProtocolMessageType.B2OwnerExpectation?InstallationWire.Expectation(Session,Identity):InstallationWire.Binding(new(Session,a.Controller.Pid,recovery,Environment.ProcessId,Deadline));
                direct.SendControl(new OwnerProtocolSequencer(Execution,Lease).Create(type,p));Event("injected-b2-only",new{Type=(byte)type});
            }
            else
            {
                byte[] payload=InstallationWire.Echo(Identity,1);if(Mode=="integrated-wrong-closure")payload[32]^=1;if(Mode=="integrated-wrong-digest")payload[0]^=1;
                var description=OwnerPreparationSpecificationCodec.EncodeStagedObjects([new("fixture",new byte[32],0,0,0,"non-authoritative-marker")]);
                if(Mode=="integrated-first-stage")Check(channel.TrySend(OwnerProtocolMessageType.StagedObjects,description,out _),"stage marker");
                Check(channel.TrySend(OwnerProtocolMessageType.OwnerInstallationEcho,payload,out _),"echo36");sent=true;Event("send36");
                if(Mode=="integrated-duplicate36")Check(channel.TrySend(OwnerProtocolMessageType.OwnerInstallationEcho,payload,out _),"duplicate36");
                Check(channel.TrySend(OwnerProtocolMessageType.StagedObjects,description,out _),"description");Event("send-description",new{ActualStaging=false});
            }
            while(true){Service();Thread.Sleep(2);}
        }
        catch(Exception e){refusal=e.Message;stopped=true;Event("stopped",new{Reason=refusal,SafetyLatched=monitor?.StopLatched});}
        finally{monitor?.Dispose();Result(new{Scope="PN GID1/order; no writer or HELD",Mode,EchoSent=sent,Stopped=stopped,ReleaseAuthority=false,B2EvidenceSockets=0,Refusal=refusal});}
        return 0;
    }
}
