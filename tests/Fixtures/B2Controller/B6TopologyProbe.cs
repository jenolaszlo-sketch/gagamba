using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

// PN ONLY: independent, unprivileged three-process protocol driver. Production
// codecs, native authentication, launcher custody, barriers and stop obligation;
// explicitly synthetic installation identity, no LoadedInstallation, admission,
// journal, domain, capability, physical writer or RELEASE implementation.
internal sealed partial class B6TopologyProbe
{
    private string Role="",Mode="",Dir="";
    private Guid Session,Execution,Lease;
    private long Deadline;
    private static InstallationIdentity Identity=>B6EvidenceProbe.Synthetic;
    private static string Entry=>Assembly.GetExecutingAssembly().Location;
    private static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    private static int NativeHandles()=>Directory.GetFiles("/proc/self/fd").Select(p=>new FileInfo(p).LinkTarget)
        .Count(t=>t is not null && (t.StartsWith("socket:",StringComparison.Ordinal) || t=="anon_inode:[pidfd]"));
    [DllImport("libc",EntryPoint="waitpid",SetLastError=true)] private static extern int WaitPid(int pid,out int status,int options);
    private void Event(string name,object? facts=null)
    {
        File.AppendAllText(Path.Combine(Dir,Role+".jsonl"),JsonSerializer.Serialize(new{Event=name,AtNs=InstallationDeadline.Now,Pid=Environment.ProcessId,Facts=facts})+"\n");
    }
    private void Result(object result)=>File.WriteAllText(Path.Combine(Dir,Role+"-result.json"),JsonSerializer.Serialize(result,Json));
    private static void Check(bool ok,string message) {if(!ok)throw new IOException(message);}
    private string[] Args(string role)=>["--b6-topology",role,Mode,Dir,Session.ToString(),Execution.ToString(),Lease.ToString(),Deadline.ToString(System.Globalization.CultureInfo.InvariantCulture)];
    private SafeFileHandle Pin(int pid)
    {Check(PidFd.TryOpen(pid,out int fd,out _),"original spawn pin unavailable");return new((IntPtr)fd,true);}
    private void WaitEvent(string role,string name,Action service)
    {
        while(true)
        {
            service();_=InstallationDeadline.Remaining(Deadline);
            string p=Path.Combine(Dir,role+".jsonl");
            if(File.Exists(p) && File.ReadLines(p).Any(line=>JsonDocument.Parse(line).RootElement.GetProperty("Event").GetString()==name))return;
            Thread.Sleep(2);
        }
    }
    private ReceivedFdRecord Read(int fd,Action service,long? earlier=null)
    {
        while(true)
        {
            service();
            if(FdPassing.TryReceiveRecord(fd,InstallationDeadline.Remaining(Math.Min(Deadline,earlier??Deadline),5),out var record,out var error))return record!;
            if(!RecoveryInterop.IsTransientReceiveError(error))throw new IOException(error);
        }
    }
    private void Send(int fd,byte[] frame,int rights=0)
    {Check(FdPassing.TrySendMessage(fd,frame,out _),"fixture transport send");Frame("send",frame,rights);}
    private void Frame(string action,byte[] frame,int rights)
    {
        Event(action+frame[6],new{Magic=Convert.ToHexStringLower(frame.AsSpan(0,4)),Sequence=BitConverter.ToUInt32(frame,40),Rights=rights,PayloadBytes=frame.Length-48});
    }
    private byte[] Accept(ReceivedFdRecord record,RecoveryWire wire,RecoveryMessage expected,int rights=0)
    {
        Check(record.DescriptorCount==rights && wire.Accept(record.Payload,out var type,out _) && type==expected,"recovery phase/rights refused");
        Frame("accept",record.Payload,record.DescriptorCount);return record.Payload[48..];
    }
    internal static int Run(string[] args)
    {
        var p=new B6TopologyProbe{Role=args[1],Mode=args[2],Dir=Path.GetFullPath(args[3])};
        Directory.CreateDirectory(p.Dir);
        if(args.Length==8){p.Session=Guid.Parse(args[4]);p.Execution=Guid.Parse(args[5]);p.Lease=Guid.Parse(args[6]);p.Deadline=long.Parse(args[7],System.Globalization.CultureInfo.InvariantCulture);}
        else {p.Session=Guid.NewGuid();p.Execution=Guid.NewGuid();p.Lease=Guid.NewGuid();}
        try
        {
            Check(RecoveryInterop.IsUnprivileged,"non-root fixture required");
            p.Event("started",new{ParentPid=RecoveryInterop.GetParentPid(),Uid=RecoveryInterop.GetUid(),Gid=DirectUnix.GetGid(),Scope="PN synthetic identity; no production qualification"});
            return p.Role switch{"controller"=>p.Controller(),"recovery"=>p.Recovery(),"owner"=>p.Owner(),"decoy"=>p.Decoy(),_=>2};
        }
        catch(Exception e){p.Event("fatal",new{Error=e.GetType().Name,Reason=e.Message});return 4;}
    }
    private int Controller()
    {
        if(Mode.StartsWith("integrated-",StringComparison.Ordinal))return ControllerIntegrated();
        int handlesBefore=NativeHandles();
        int signals=0;using var registration=Mode=="signals-refusal"?PosixSignalRegistration.Create((PosixSignal)10,context=>{context.Cancel=true;Interlocked.Increment(ref signals);}):null;
        using var supervisor=new M1RecoveryScope();Check(supervisor.TryInitialize().Ok,"controller cleanup subreaper");
        var runtime=B6EvidenceProbe.RuntimePin();Deadline=InstallationDeadline.Create();
        var pair=QualifiedLauncher.Pair();using var control=new SafeFileHandle((IntPtr)pair.Parent,true);
        using var listener=B2EvidenceSocket.Listen(Session);
        using var recovery=QualifiedLauncher.Start(runtime,Entry,Args("recovery"),(pair.Child,3,"GAGAMBA_B2_CONTROL_FD"));
        using var recoveryPin=Pin(recovery.Id);PreparationInterop.CloseFd(pair.Child);
        var wire=new RecoveryWire(Execution,Lease,2,false,true);
        var barrier=new B2InstallationBarrier(Identity,Session,Environment.ProcessId,recovery.Id,Deadline);
        SafeFileHandle? ownerPin=null;B2Binding? binding=null;bool accepted=false,confirmed=false,ready=false;string? refusal=null;long? refusedAt=null;
        void Pins()
        {
            _=InstallationDeadline.Remaining(Deadline);B2ProcessIdentity.RequireLive(recoveryPin.DangerousGetHandle().ToInt32(),recovery.Id);
            if(ownerPin is not null)B2ProcessIdentity.RequireLive(ownerPin.DangerousGetHandle().ToInt32(),binding!.OwnerPid);
        }
        void Service()
        {
            Pins();var poll=new DirectUnix.PollFd{Fd=pair.Parent,Events=1|0x2000};
            Check(DirectUnix.Poll(ref poll,(UIntPtr)1,0)>=0 && poll.Revents==0,"pending recovery control/loss preempts evidence");
        }
        try
        {
            var auth=new byte[48];Session.TryWriteBytes(auth);SHA256.HashData(Session.ToByteArray()).CopyTo(auth,16);
            Send(pair.Parent,wire.Create(RecoveryMessage.Associate,auth));
            if(Mode=="controller-recovery-worker-death")
            {WaitEvent("recovery","worker-blocked",Pins);Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            using(var record=Read(pair.Parent,Pins))
            {
                byte[] echo=Accept(record,wire,RecoveryMessage.InstallationEchoRequest);
                Check(InstallationWire.CompareEcho(echo,Identity,0)==InstallationMismatch.None,"recovery identity refused");
                if(Mode!="drop10")Send(pair.Parent,wire.Create(RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(Identity,InstallationMismatch.None)));
                barrier.Recovery(echo,InstallationDeadline.Now);
            }
            using(var record=Read(pair.Parent,Pins))
            {
                byte[] bytes=Accept(record,wire,RecoveryMessage.B2OwnerBound,Mode=="missing11"?0:1);
                binding=InstallationWire.ReadBinding(bytes);barrier.Bound(binding,InstallationDeadline.Now);
                ownerPin=B2ProcessIdentity.ReceivePin(record,binding.OwnerPid);
                Event("original-pins",new{RecoveryPid=recovery.Id,OwnerPid=binding.OwnerPid,RecoveryLive=true,OwnerLive=true,Cloexec=PreparationInterop.HasCloexec(ownerPin.DangerousGetHandle().ToInt32())});
            }
            if(Mode=="release-to-recovery")Send(pair.Parent,new OwnerProtocolSequencer(Execution,Lease).Create(OwnerProtocolMessageType.ReleaseRequest,[1]));
            using(var evidence=listener.Accept(Deadline,Service))
            {
                B2ProcessIdentity.VerifyPeer(evidence.Descriptor,ownerPin.DangerousGetHandle().ToInt32(),binding,true);
                Event("peer-owner-verified",new{Expected=binding.OwnerPid,Uid=RecoveryInterop.GetUid(),Gid=DirectUnix.GetGid()});
                var seq=new OwnerProtocolSequencer(Execution,Lease);
                evidence.Send(seq,OwnerProtocolMessageType.B2OwnerExpectation,InstallationWire.Expectation(Session,Identity),Deadline,Service);Event("send37");
                if(Mode is "controller-hash-death" or "stop-stalled" or "recovery-stalled-death")
                {
                    WaitEvent("owner","worker-blocked",Service);
                    if(Mode=="controller-hash-death"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
                    if(Mode=="recovery-stalled-death")_ =PidFd.TrySignal(recoveryPin.DangerousGetHandle().ToInt32(),9,out _);
                    else Send(pair.Parent,wire.Create(RecoveryMessage.Stop));
                }
                if(Mode=="owner-queued36")
                {
                    WaitEvent("owner","send36",Service);
                    while(!PidFd.TryExited(ownerPin.DangerousGetHandle().ToInt32(),5,out bool exited,out _) || !exited){_=InstallationDeadline.Remaining(Deadline);}
                }
                byte[] echo=evidence.Receive(seq,OwnerProtocolMessageType.OwnerInstallationEcho,B2ProcessIdentity.Identity(binding.OwnerPid),ownerPin.DangerousGetHandle().ToInt32(),Deadline,Service);
                Event("record-owner-verified",new{Expected=binding.OwnerPid,ZeroRights=true,KernelCredentials=true,Drained=true});
                Check(InstallationWire.CompareEcho(echo,Identity,1)==InstallationMismatch.None,"owner installation identity refused");
                B2ProcessIdentity.VerifyPeer(evidence.Descriptor,ownerPin.DangerousGetHandle().ToInt32(),binding,true);Service();barrier.Owner(echo,InstallationDeadline.Now);
            }
            if(Mode=="controller-after36-death"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode is "controller-stalled-owner-death" or "controller-stalled-recovery-death")
                _=InstallationDeadline.Verify(()=>{Event("worker-blocked");new ManualResetEventSlim(false).Wait();return false;},Deadline,Service);
            Service(); // real production S2 revalidation is intentionally NOT synthesized
            var confirmation=barrier.Confirmation(InstallationDeadline.Now);
            if(Mode=="late12")while(InstallationDeadline.Now<Deadline+10_000_000L)Thread.Sleep(2);
            if(Mode=="wrong12")confirmation[16]^=1;
            byte[] confirmationFrame=wire.Create(RecoveryMessage.B2OwnerEchoAccepted,confirmation);
            if(Mode!="drop12") {Send(pair.Parent,confirmationFrame);confirmed=true;}
            if(Mode=="duplicate12")Send(pair.Parent,wire.Create(RecoveryMessage.B2OwnerEchoAccepted,confirmation));
            if(Mode=="replay12")Send(pair.Parent,confirmationFrame);
            if(Mode=="controller-after12-death"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            using(var record=Read(pair.Parent,Pins)){barrier.Ready(Accept(record,wire,RecoveryMessage.Ready),InstallationDeadline.Now);ready=true;}
            if(Mode=="owner-after12")
            {
                _=PidFd.TrySignal(ownerPin.DangerousGetHandle().ToInt32(),9,out _);
                while(!PidFd.TryExited(ownerPin.DangerousGetHandle().ToInt32(),5,out bool exited,out _) || !exited){_=InstallationDeadline.Remaining(Deadline);}
            }
            Service();barrier.Commit(InstallationDeadline.Now);accepted=barrier.Qualified;Event("PN-ready");
            Check(Mode is "valid" or "withhold26","negative mutation unexpectedly progressed");
        }
        catch(Exception e){refusal=e.Message;refusedAt=InstallationDeadline.Now;barrier.Stop("Refused");Event("refused",new{Reason=e.Message});}
        finally
        {
            _=FdPassing.TrySendMessage(pair.Parent,wire.Create(RecoveryMessage.Stop),out _);
            control.Dispose();ownerPin?.Dispose();
            bool reaped=QualifiedLauncher.TryReap(recovery,8000,out int code);
            if(!reaped){_=PidFd.TrySignal(recoveryPin.DangerousGetHandle().ToInt32(),9,out _);reaped=QualifiedLauncher.TryReap(recovery,3000,out code);}
            // On R death this fixture supervisor adopts its exact reported descendants.
            foreach(string role in new[]{"owner","decoy"})
            {
                string meta=Path.Combine(Dir,role+".jsonl");
                if(!File.Exists(meta))continue;
                int pid=JsonDocument.Parse(File.ReadLines(meta).First()).RootElement.GetProperty("Pid").GetInt32();
                long end=InstallationDeadline.Now+3_000_000_000L;
                while(WaitPid(pid,out _,1)==0 && InstallationDeadline.Now<end)Thread.Sleep(2);
            }
            listener.Dispose();recoveryPin.Dispose();recovery.Dispose();int handlesAfter=NativeHandles();
            Result(new{Scope="PN mechanism only; no S2 selection or production-qualified return",Mode,Accepted=accepted,ConfirmationSent=confirmed,ReadyReceived=ready,Refusal=refusal,RecoveryReaped=reaped,RecoveryExitCode=code,ReleaseAuthority=false,PhysicalReleaseAttempts=0,NativeHandlesBefore=handlesBefore,NativeHandlesAfter=handlesAfter,SignalsObserved=signals,DeadlineNs=Deadline,RefusedAtNs=refusedAt});
        }
        return 0;
    }
    private int Recovery()
    {
        if(Mode.StartsWith("integrated-",StringComparison.Ordinal))return RecoveryIntegrated();
        Check(RecoveryInterop.Protect(3) && RecoveryInterop.VerifyPeer(3,RecoveryInterop.GetParentPid(),RecoveryInterop.GetUid()),"original C socket");
        using var control=new SafeFileHandle((IntPtr)3,true);int controller=RecoveryInterop.GetParentPid();using var cpin=Pin(controller);
        Check(RecoveryInterop.DetachLifetime(),"detached R");using var scope=new M1RecoveryScope();Check(scope.TryInitialize().Ok,"R subreaper");
        var wire=new RecoveryWire(Execution,Lease,2,false,false);var seq=new OwnerProtocolSequencer(Execution,Lease);var obligation=new RecoveryObligation();
        Process? owner=null,decoy=null;int ownerFd=-1;SafeFileHandle? life=null;B2Binding? binding=null;B2RecoveryReadyBarrier? barrier=null;
        bool bound=false,confirmed=false,ready=false,ack=false,reaped=false;string? refusal=null;int ownerCode=-1;
        void Stop(string cause)
        {
            if(!obligation.Request(cause,Stopwatch.GetTimestamp()))return;
            barrier?.Stop(cause);Event("stop",new{Cause=cause,Count=obligation.Count,Deadline=obligation.Deadline,ReleaseAuthority=obligation.ReleaseAuthority});
            if(life is not null && !life.IsClosed)_=FdPassing.TrySendMessage(life.DangerousGetHandle().ToInt32(),seq.Create(OwnerProtocolMessageType.B2SafetyStop),out _);
        }
        void Service()
        {
            if(!DirectPeerIdentity.LiveReference(cpin.DangerousGetHandle().ToInt32(),B2ProcessIdentity.Identity(controller)))Stop("ControllerExited");
            if(owner is not null && PidFd.TryExited(ownerFd,0,out bool exited,out _) && exited)Stop("OwnerExited");
            if(FdPassing.TryReceiveMessage(3,0,out var frame,out var error))
            {
                if(!wire.Accept(frame,out var type,out var payload))Stop("InvalidControl");
                else if(type==RecoveryMessage.Stop)Stop("ControllerStop");
                else if(type==RecoveryMessage.B2OwnerEchoAccepted && bound && !confirmed && obligation.Count==0)
                {try{barrier!.Confirm(payload,InstallationDeadline.Now);confirmed=true;Frame("accept",frame,0);}catch{Stop("InvalidConfirmation");}}
                else Stop("UnexpectedControl");
            }
            else if(!RecoveryInterop.IsTransientReceiveError(error))Stop("ControllerTransportLost");
            if(!ready && InstallationDeadline.Now>=Deadline)Stop("InstallationExpired");
            if(obligation.Count!=0)throw new IOException(obligation.Cause);
        }
        try
        {
            using(var record=Read(3,()=>B2ProcessIdentity.RequireLive(cpin.DangerousGetHandle().ToInt32(),controller)))
            {
                var auth=Accept(record,wire,RecoveryMessage.Associate);using var association=new RecoveryAssociation(Session,SHA256.HashData(Session.ToByteArray()));Check(association.Authenticate(auth),"Associate");
            }
            if(Mode=="controller-recovery-worker-death")
                _=InstallationDeadline.Verify(()=>{Event("worker-blocked");new ManualResetEventSlim(false).Wait();return false;},Deadline,Service);
            Send(3,wire.Create(RecoveryMessage.InstallationEchoRequest,InstallationWire.Echo(Identity,0)));
            using(var record=Read(3,()=>B2ProcessIdentity.RequireLive(cpin.DangerousGetHandle().ToInt32(),controller)))InstallationWire.AcceptResponse(Accept(record,wire,RecoveryMessage.InstallationEchoResponse),Identity);
            var runtime=B6EvidenceProbe.RuntimePin();var pair=QualifiedLauncher.Pair();life=new((IntPtr)pair.Parent,true);
            string ownerEntry=Mode=="production-owner-refusal"?File.ReadAllText(Path.Combine(Dir,"production-owner-path.txt")).Trim():Entry;
            string[] ownerArgs=Mode=="production-owner-refusal"?["--exec",Execution.ToString("D"),"--lease",Lease.ToString("D"),"--b2-lifeline","2"]:Args("owner");
            var spawn=scope.TrySpawnOwner(ownerEntry,ownerArgs,pair.Child,runtime,qualified:true);Check(spawn.Ok,spawn.Error);
            owner=spawn.Process!;ownerFd=spawn.PidFd;Event("spawn-owner",new{OwnerPid=owner.Id,OriginalObject=true,Cloexec=PreparationInterop.HasCloexec(ownerFd)});
            binding=new(Session,controller,Environment.ProcessId,owner.Id,Deadline);barrier=new(binding,Identity);
            if(Mode=="prematureReady")Send(3,wire.Create(RecoveryMessage.Ready,JsonSerializer.SerializeToUtf8Bytes(new RecoverySnapshot(Environment.ProcessId,owner.Id,controller,Environment.ProcessId,true,false,false,0,null,null,false))));
            Send(pair.Parent,seq.Create(OwnerProtocolMessageType.B2StartLifeline,JsonSerializer.SerializeToUtf8Bytes(new B2OwnerStart(Environment.ProcessId,Dir))));
            using(var record=Read(pair.Parent,Service,InstallationDeadline.Now+3_000_000_000L))
            {Check(record.DescriptorCount==0 && seq.TryAccept(record.Payload,OwnerProtocolMessageType.B2LifelineReady,out var m,out _) && m!.Payload.Length==0,"24 required");Frame("accept",record.Payload,0);}
            barrier.Lifeline(InstallationDeadline.Now);Service();
            if(Mode=="recovery-before38"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode=="lifeline-loss"){life.Dispose();throw new IOException("deliberate lifeline loss");}
            if(Mode=="stop-before38"){Stop("ObservedStop");throw new IOException("stopped before38");}
            int controllerReference=Mode=="wrong38"?ownerFd:cpin.DangerousGetHandle().ToInt32();
            Check(FdPassing.TrySendTransfer(pair.Parent,[controllerReference],seq.Create(OwnerProtocolMessageType.B2EchoBootstrap,InstallationWire.Binding(binding)),out _),"38 transfer");Event("send38",new{Rights=1});
            if(Mode=="recovery-after38"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            byte[] boundFrame=wire.Create(RecoveryMessage.B2OwnerBound,InstallationWire.Binding(binding));
            if(Mode=="wrong11")Check(FdPassing.TrySendTransfer(3,[cpin.DangerousGetHandle().ToInt32()],boundFrame,out _),"wrong11 send");
            else if(Mode=="missing11")Send(3,boundFrame);
            else Check(scope.SendOriginalB2OwnerReference(owner,3,boundFrame),"original O export");
            Event("send11",new{Rights=Mode=="missing11"?0:1});barrier.ReferencesSent(InstallationDeadline.Now);bound=true;
            if(Mode=="duplicate11")Check(FdPassing.TrySendTransfer(3,[ownerFd],wire.Create(RecoveryMessage.B2OwnerBound,InstallationWire.Binding(binding)),out _),"duplicate11 send");
            if(Mode=="recovery-after11"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode=="nested36")
            {var nested=wire.Create(RecoveryMessage.Snapshot,InstallationWire.Echo(Identity,1));nested[6]=13;Send(3,nested);}
            if(Mode=="release-from-recovery")
            {var packet=wire.Create(RecoveryMessage.Snapshot,[1]);packet[6]=27;Send(3,packet);}
            if(Mode=="recovery-connector")
            {using var connection=B2EvidenceSocket.Connect(Session,Deadline,Service);Event("impostor-connected");while(true){Service();Thread.Sleep(2);}}
            if(Mode=="sibling-connector")
            {
                var extra=QualifiedLauncher.Pair();using var unused=new SafeFileHandle((IntPtr)extra.Parent,true);
                decoy=QualifiedLauncher.Start(runtime,Entry,Args("decoy"),(extra.Child,3,"GAGAMBA_BOOTSTRAP_FD"));PreparationInterop.CloseFd(extra.Child);
            }
            while(!confirmed)
            {
                Service();
                if(Mode is "controller-stalled-owner-death" or "controller-stalled-recovery-death")
                {
                    WaitEvent("controller","worker-blocked",Service);
                    if(Mode=="controller-stalled-owner-death")_=PidFd.TrySignal(ownerFd,9,out _);
                    else {Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
                }
                if(Mode=="recovery-writer" && FdPassing.TryReceiveRecord(pair.Parent,0,out var leaked,out _))
                {
                    using(leaked!)
                    {
                        Check(leaked!.DescriptorCount==1,"fixture leaked socket");int socket=leaked.BorrowDescriptor(0);
                        Send(socket,new OwnerProtocolSequencer(Execution,Lease).Create(OwnerProtocolMessageType.OwnerInstallationEcho,InstallationWire.Echo(Identity,1)));
                        DirectUnix.Shutdown(socket,1);Event("recovery-wrote36");
                    }
                }
                Thread.Sleep(2);
            }
            Service();byte[] snapshot=JsonSerializer.SerializeToUtf8Bytes(new RecoverySnapshot(Environment.ProcessId,owner.Id,controller,RecoveryInterop.GetSid(0),scope.SubreaperEnabled,false,false,0,null,null,false));
            barrier.Ready(snapshot,InstallationDeadline.Now);Service();
            if(Mode!="dropReady")Send(3,wire.Create(RecoveryMessage.Ready,snapshot));
            ready=Mode!="dropReady";Event("local-ready-gate",new{Sent=ready});
            while(true){Service();Thread.Sleep(2);}
        }
        catch(Exception e){refusal=e.Message;Stop("InstallationRefused");}
        finally
        {
            if(owner is not null)
            {
                long end=InstallationDeadline.Now+5_000_000_000L;bool signaled=false;
                while(InstallationDeadline.Now<end)
                {
                    if(life is not null && !life.IsClosed && FdPassing.TryReceiveMessage(life.DangerousGetHandle().ToInt32(),0,out var response,out _))
                        if(seq.TryAccept(response,OwnerProtocolMessageType.B2StopObserved,out var m,out _) && m!.Payload.Length==0){ack=true;Event("accept26");}
                    var exit=scope.WaitForOwner(owner,TimeSpan.Zero);if(exit.Exited){reaped=true;ownerCode=exit.ExitCode;Event("owner-reaped",new{ExitCode=ownerCode});break;}
                    if(!signaled && obligation.Deadline is { } d && Stopwatch.GetTimestamp()>=d){_=PidFd.TrySignal(ownerFd,9,out _);signaled=true;Event("stop-deadline-signal");}
                    Thread.Sleep(2);
                }
            }
            if(decoy is not null){decoy.Kill();Check(QualifiedLauncher.TryReap(decoy,3000,out _),"decoy reap");decoy.Dispose();}
            life?.Dispose();Result(new{Scope="PN mechanism only",Mode,OwnerPid=owner?.Id,Confirmed=confirmed,ReadySent=ready,StopCount=obligation.Count,StopCause=obligation.Cause,StopDeadline=obligation.Deadline,OwnerAcknowledged=ack,OwnerReaped=reaped,OwnerExitCode=ownerCode,ReleaseAuthority=obligation.ReleaseAuthority,NativeStop="Unproven",RetainedDomains=0,Refusal=refusal});
        }
        return 0;
    }
    private int Owner()
    {
        if(Mode.StartsWith("integrated-",StringComparison.Ordinal))return OwnerIntegrated();
        int recovery=RecoveryInterop.GetParentPid();using var life=new SafeFileHandle((IntPtr)3,true);Check(RecoveryInterop.Protect(3) && RecoveryInterop.VerifyPeer(3,recovery,RecoveryInterop.GetUid()),"original R lifeline");
        using var rpin=Pin(recovery);var seq=new OwnerProtocolSequencer(Execution,Lease);bool bound=false,stopped=false,echo=false,ack=false;string? refusal=null;
        void Service()
        {
            B2ProcessIdentity.RequireLive(rpin.DangerousGetHandle().ToInt32(),recovery);
            Check(RecoveryInterop.GetParentPid()==recovery,"original R lineage lost");
            var p=new DirectUnix.PollFd{Fd=3,Events=0x2000};Check(DirectUnix.Poll(ref p,(UIntPtr)1,0)>=0 && p.Revents==0,"lifeline lost");
            if(bound && FdPassing.TryReceiveMessage(3,0,out var frame,out var error))
            {
                Check(seq.TryAccept(frame,OwnerProtocolMessageType.B2SafetyStop,out var m,out _) && m!.Payload.Length==0,"stop-only lifeline");
                Frame("accept",frame,0);throw new IOException("RecoveryStopRequested");
            }
        }
        try
        {
            using(var record=Read(3,Service)){Check(record.DescriptorCount==0 && seq.TryAccept(record.Payload,OwnerProtocolMessageType.B2StartLifeline,out _,out _),"23 required");Frame("accept",record.Payload,0);}
            if(Mode=="echo-on-lifeline")Send(3,seq.Create(OwnerProtocolMessageType.OwnerInstallationEcho,InstallationWire.Echo(Identity,1)));
            else if(Mode=="release-on-lifeline")Send(3,seq.Create(OwnerProtocolMessageType.ReleaseRequest,[1]));
            else if(Mode!="missing24")Send(3,seq.Create(OwnerProtocolMessageType.B2LifelineReady));
            using var bootstrap=Read(3,Service);
            if(OwnerProtocol.TryDecode(bootstrap.Payload,Execution,Lease,out var stop,out _) && stop!.Type==OwnerProtocolMessageType.B2SafetyStop)
            {Check(bootstrap.DescriptorCount==0 && seq.TryAccept(bootstrap.Payload,OwnerProtocolMessageType.B2SafetyStop,out _,out _),"stop bootstrap");throw new IOException("RecoveryStopRequested");}
            Check(seq.TryAccept(bootstrap.Payload,OwnerProtocolMessageType.B2EchoBootstrap,out var message,out _),"38 required");
            var binding=InstallationWire.ReadBinding(message!.Payload);Check(binding.OwnerPid==Environment.ProcessId && binding.RecoveryPid==recovery && binding.Session==Session && binding.DeadlineNs==Deadline,"original bootstrap");
            using var cpin=B2ProcessIdentity.ReceivePin(bootstrap,binding.ControllerPid);bound=true;Frame("accept",bootstrap.Payload,1);
            if(Mode is "recovery-connector" or "sibling-connector")while(true){Service();_=InstallationDeadline.Remaining(Deadline);Thread.Sleep(2);}
            using var evidence=B2EvidenceSocket.Connect(Session,Deadline,Service);
            B2ProcessIdentity.VerifyPeer(evidence.Descriptor,cpin.DangerousGetHandle().ToInt32(),binding,false);Event("peer-controller-verified",new{Expected=binding.ControllerPid});
            var es=new OwnerProtocolSequencer(Execution,Lease);
            var expected=evidence.Receive(es,OwnerProtocolMessageType.B2OwnerExpectation,B2ProcessIdentity.Identity(binding.ControllerPid),cpin.DangerousGetHandle().ToInt32(),Deadline,Service);
            Check(InstallationWire.ReadExpectation(expected,Session)==Identity,"synthetic challenge mismatch");Event("accept37",new{KernelCredentials=true,ZeroRights=true,Drained=true});
            if(Mode is "controller-hash-death" or "stop-stalled" or "recovery-stalled-death")
            {
                _=InstallationDeadline.Verify(()=>{Event("worker-blocked");new ManualResetEventSlim(false).Wait();return false;},Deadline,Service);
                throw new IOException("blocked verifier unexpectedly returned");
            }
            if(Mode=="owner-before36"){Event("intentional-death");Process.GetCurrentProcess().Kill();Thread.Sleep(Timeout.Infinite);}
            if(Mode=="drop36")while(true){Service();_=InstallationDeadline.Remaining(Deadline);Thread.Sleep(2);}
            if(Mode=="signals-refusal")while(true)
            {Service();_=InstallationDeadline.Remaining(Deadline);_=PidFd.TrySignal(cpin.DangerousGetHandle().ToInt32(),10,out _);Thread.Sleep(5);}
            if(Mode=="late36")
            {
                while(InstallationDeadline.Now<=Deadline)Thread.Sleep(1);
                Event("late36-attempt",new{DeadlineNs=Deadline});
                bool sentLate=FdPassing.TrySendMessage(evidence.Descriptor,new OwnerProtocolSequencer(Execution,Lease).Create(OwnerProtocolMessageType.OwnerInstallationEcho,InstallationWire.Echo(Identity,1)),out _);
                Event("late36-result",new{Sent=sentLate});DirectUnix.Shutdown(evidence.Descriptor,1);
                while(true){Service();Thread.Sleep(2);}
            }
            if(Mode=="recovery-writer")
            {
                Check(FdPassing.TrySendTransfer(3,[evidence.Descriptor],seq.Create(OwnerProtocolMessageType.B2StopObserved),out _),"adversarial socket leak");
                Event("fixture-leaked-socket");while(true){Service();_=InstallationDeadline.Remaining(Deadline);Thread.Sleep(2);}
            }
            byte[] payload=InstallationWire.Echo(Identity,1);
            if(Mode=="wrong-digest")payload[0]^=1;if(Mode=="wrong-closure")payload[32]^=1;
            if(Mode is "wrong-execution" or "wrong-lease" or "duplicate36" or "no-half-close" or "prior-session")
            {
                var raw=new OwnerProtocolSequencer(Mode=="wrong-execution"?Guid.NewGuid():Execution,Mode=="wrong-lease"?Guid.NewGuid():Lease);
                var frame=raw.Create(OwnerProtocolMessageType.OwnerInstallationEcho,payload);
                if(Mode=="prior-session")frame=raw.Create(OwnerProtocolMessageType.OwnerInstallationEcho,payload); // next-sequence replay on a fresh connection
                Send(evidence.Descriptor,frame);
                if(Mode=="duplicate36")Send(evidence.Descriptor,raw.Create(OwnerProtocolMessageType.OwnerInstallationEcho,payload));
                if(Mode!="no-half-close")DirectUnix.Shutdown(evidence.Descriptor,1);
            }
            else evidence.Send(es,OwnerProtocolMessageType.OwnerInstallationEcho,payload,Deadline,Service);
            echo=true;Event("send36",new{Rights=0,Synthetic=true});
            if(Mode=="owner-queued36")return 0;
            while(true){Service();Thread.Sleep(2);}
        }
        catch(Exception e)
        {
            refusal=e.Message;stopped=true;ack=Mode!="withhold26" && FdPassing.TrySendMessage(3,seq.Create(OwnerProtocolMessageType.B2StopObserved),out _);Event("stopped",new{Reason=refusal,AckSent=ack});
        }
        finally{Result(new{Scope="PN synthetic identity only; no loaded-installation authority",Mode,EchoSent=echo,Stopped=stopped,AcknowledgementSent=ack,ReleaseAuthority=false,Refusal=refusal});}
        return 0;
    }
    private int Decoy()
    {
        using var control=new SafeFileHandle((IntPtr)3,true);using var evidence=B2EvidenceSocket.Connect(Session,Deadline,()=>{});Event("impostor-connected");
        while(InstallationDeadline.Now<Deadline)Thread.Sleep(2);return 0;
    }
}
