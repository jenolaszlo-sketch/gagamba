using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal static class QualifiedRecoveryConnection
{
    internal static RecoveryConnection Start(RecoveryConfiguration requested)
    {
        requested.Validate(); // delegated qualified B2 refuses before any open/allocation
        if(requested.Version!=2 || !RecoveryInterop.Supported || !RecoveryInterop.IsUnprivileged)
            throw new IOException("qualified bootstrap requires unprivileged Linux");
        var selected=LoadedInstallation.Verify("provider");
        var supplied=requested.B2InstallationIdentity??requested.Integrated!.InstallationIdentity;
        if(supplied!=selected.Identity) throw new InvalidDataException("requested identity differs from S2");
        long deadline=InstallationDeadline.Create(requested.Integrated?.ExpiresAtNs??requested.B2DeadlineNs);
        InstallationDeadline.Validate(deadline);
        var config=requested with {Session=Guid.NewGuid(),ControllerPid=Environment.ProcessId,ControllerUid=RecoveryInterop.GetUid(),
            Secret=RandomNumberGenerator.GetBytes(32),Recovery=selected.Executable("recovery"),Owner=selected.Executable("owner"),
            B2DeadlineNs=requested.Integrated is null?deadline:null};
        if(config.Integrated is { } integrated) config=config with{Integrated=integrated with{Controller=DirectProcessIdentity.Current(),
            Runtime=selected.Runtime(),InstallationIdentity=selected.Identity,
            Profile=integrated.Profile with{OwnerDllPath=config.Owner.Verify("Gagamba.Execution.Linux.M1.Owner")}}};
        var wire=new RecoveryWire(config.Execution,config.Generation,2,config.Integrated is not null,true);
        B2EvidenceSocket? listener=null;Process? process=null;SafeFileHandle? recoveryPin=null,ownerPin=null;
        int control=-1,childControl=-1,credential=-1,childCredential=-1;bool bootstrapDelivered=false;
        B2Binding? binding=null;B2InstallationBarrier? barrier=null;
        void Pins()
        {
            _=InstallationDeadline.Remaining(deadline);
            if(process is not null && recoveryPin is not null) B2ProcessIdentity.RequireLive(recoveryPin.DangerousGetHandle().ToInt32(),process.Id);
            if(binding is not null && ownerPin is not null) B2ProcessIdentity.RequireLive(ownerPin.DangerousGetHandle().ToInt32(),binding.OwnerPid);
        }
        void Service()
        {
            Pins();var poll=new DirectUnix.PollFd{Fd=control,Events=1|0x2000};
            if(DirectUnix.Poll(ref poll,(UIntPtr)1,0)<0 || poll.Revents!=0) throw new IOException("unexpected control/loss during owner evidence");
        }
        ReceivedFdRecord Receive(RecoveryMessage expected)
        {
            while(true)
            {
                Pins();
                if(FdPassing.TryReceiveRecord(control,InstallationDeadline.Remaining(deadline,10),out var record,out var error))
                {
                    if(wire.Accept(record!.Payload,out var type,out _) && type==expected) return record;
                    record.Dispose();throw new IOException("qualified recovery transition refused");
                }
                if(!RecoveryInterop.IsTransientReceiveError(error)) throw new IOException("qualified recovery transport lost");
            }
        }
        byte[] Payload(ReceivedFdRecord record)=>record.Payload[OwnerProtocol.HeaderBytes..];
        void Send(RecoveryMessage type,byte[] payload)
        {
            Pins();var frame=wire.Create(type,payload);
            try{if(!FdPassing.TrySendMessage(control,frame,out _)) throw new IOException("qualified control send failed");}
            finally{if(type==RecoveryMessage.Associate) CryptographicOperations.ZeroMemory(frame);}
        }
        try
        {
            if(config.Integrated is null) listener=B2EvidenceSocket.Listen(config.Session);
            (control,childControl)=QualifiedLauncher.Pair();(credential,childCredential)=QualifiedLauncher.Pair();
            var recoveryPlan=InstallationDeadline.Verify(()=>QualifiedLauncher.InstalledPlan.Prepare(selected,"recovery"),deadline,()=>{});
            process=recoveryPlan.Spawn([],(childControl,3,"GAGAMBA_B2_CONTROL_FD"),(childCredential,4,"GAGAMBA_B2_CREDENTIAL_FD"));
            if(!PidFd.TryOpen(process.Id,out int pin,out _)) throw new IOException("original recovery pin unavailable");
            recoveryPin=new((IntPtr)pin,true);Pins();
            if(config.Integrated is null) barrier=new(selected.Identity,config.Session,Environment.ProcessId,process.Id,deadline);
            PreparationInterop.CloseFd(childControl);childControl=-1;PreparationInterop.CloseFd(childCredential);childCredential=-1;
            byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(config);
            try
            {
                if(bytes.Length>8192 || !FdPassing.TrySendMessage(credential,bytes,out _)) throw new IOException("v2 bootstrap delivery failed");
                bootstrapDelivered=true;
            }
            finally {CryptographicOperations.ZeroMemory(bytes);PreparationInterop.CloseFd(credential);credential=-1;}
            var auth=new byte[48];config.Session.TryWriteBytes(auth);config.Secret.CopyTo(auth,16);
            try {Send(RecoveryMessage.Associate,auth);}finally{CryptographicOperations.ZeroMemory(auth);}
            using(var echo=Receive(RecoveryMessage.InstallationEchoRequest))
            {
                if(echo.DescriptorCount!=0) throw new IOException("recovery echo rights refused");
                var reason=InstallationWire.CompareEcho(Payload(echo),selected.Identity,0,config.Integrated is not null);
                Send(RecoveryMessage.InstallationEchoResponse,InstallationWire.Response(selected.Identity,reason));
                if(reason!=InstallationMismatch.None) throw new IOException("recovery loaded identity mismatch");
                barrier?.Recovery(Payload(echo),InstallationDeadline.Now);
            }
            OwnerRendezvous? rendezvous=null;
            if(config.Integrated is null)
            {
                using(var bound=Receive(RecoveryMessage.B2OwnerBound))
                {
                    binding=InstallationWire.ReadBinding(Payload(bound));
                    if(binding.Session!=config.Session || binding.ControllerPid!=Environment.ProcessId || binding.RecoveryPid!=process.Id ||
                        binding.DeadlineNs!=deadline) throw new IOException("original B2 binding refused");
                    ownerPin=B2ProcessIdentity.ReceivePin(bound,binding.OwnerPid);
                    barrier!.Bound(binding,InstallationDeadline.Now);
                }
                using(var evidence=listener!.Accept(deadline,Service))
                {
                    B2ProcessIdentity.VerifyPeer(evidence.Descriptor,ownerPin.DangerousGetHandle().ToInt32(),binding,true);
                    var seq=new OwnerProtocolSequencer(config.Execution,config.Generation);
                    evidence.Send(seq,OwnerProtocolMessageType.B2OwnerExpectation,InstallationWire.Expectation(config.Session,selected.Identity),deadline,Service);
                    var ownerEcho=evidence.Receive(seq,OwnerProtocolMessageType.OwnerInstallationEcho,B2ProcessIdentity.Identity(binding.OwnerPid),
                        ownerPin.DangerousGetHandle().ToInt32(),deadline,Service);
                    if(InstallationWire.CompareEcho(ownerEcho,selected.Identity,1)!=InstallationMismatch.None) throw new IOException("owner identity mismatch");
                    B2ProcessIdentity.VerifyPeer(evidence.Descriptor,ownerPin.DangerousGetHandle().ToInt32(),binding,true);Service();
                    barrier!.Owner(ownerEcho,InstallationDeadline.Now);
                }
                _=InstallationDeadline.Verify(()=>{selected.Revalidate();return true;},deadline,Service);
                Service();Send(RecoveryMessage.B2OwnerEchoAccepted,barrier!.Confirmation(InstallationDeadline.Now));
                using(var ready=Receive(RecoveryMessage.Ready))
                {
                    if(ready.DescriptorCount!=0) throw new IOException("Ready rights refused");
                    barrier!.Ready(Payload(ready),InstallationDeadline.Now);
                }
                _=InstallationDeadline.Verify(()=>{selected.Revalidate();return true;},deadline,Service);
                Service();barrier!.Commit(InstallationDeadline.Now); // one local commit after final S2/live/stop observations
            }
            else
            {
                using var record=Receive(RecoveryMessage.Rendezvous);
                rendezvous=JsonSerializer.Deserialize<OwnerRendezvous>(Payload(record))??throw new IOException("missing rendezvous");
                var a=rendezvous.Association;a.Validate();
                if(rendezvous.Version!=1 || rendezvous.LifetimeRole!=DirectBootstrapRole.OwnerProcessReference ||
                    a.Execution!=config.Execution || a.Generation!=config.Integrated.StoreGeneration || a.OriginalSession!=config.Session ||
                    a.Controller!=config.Integrated.Controller || a.Recovery!=B2ProcessIdentity.Identity(process.Id)) throw new IOException("integrated original association refused");
                ownerPin=B2ProcessIdentity.ReceivePin(record,a.Owner.Pid);
                _=InstallationDeadline.Verify(()=>{selected.Revalidate();return true;},deadline,Service);Pins();
            }
            var connection=new RecoveryConnection(process,wire,control){Rendezvous=rendezvous,OwnerReference=ownerPin,RecoveryReference=recoveryPin,
                Installation=selected,InstallationDeadlineNs=deadline};
            process=null;control=-1;ownerPin=null;recoveryPin=null;return connection;
        }
        catch {barrier?.Stop("StartupRefused");throw;}
        finally
        {
            listener?.Dispose();
            if(process is not null)
            {
                if(bootstrapDelivered && control>=0) _=FdPassing.TrySendMessage(control,wire.Create(RecoveryMessage.Stop),out _);
                // Once O could exist, detached R must survive to stop/reap it. EOF remains a stop signal.
                if(!bootstrapDelivered) {try{if(!process.HasExited) process.Kill();_=QualifiedLauncher.TryReap(process,5000,out _);}catch(InvalidOperationException){}}
                process.Dispose();
            }
            foreach(int fd in new[]{control,childControl,credential,childCredential}) if(fd>=0) PreparationInterop.CloseFd(fd);
            recoveryPin?.Dispose();ownerPin?.Dispose();CryptographicOperations.ZeroMemory(config.Secret);
        }
    }
}
