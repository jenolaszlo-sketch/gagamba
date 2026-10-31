using System.Diagnostics;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Restricted;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed record B3IntegratedProfile(PinnedExecutable Recovery,PinnedExecutable Owner,ExperimentalRuntimePin Runtime,string EvidenceDirectory);
internal sealed record IntegratedRecoverySpecification(B3FixtureProfile Profile,Guid Issuer,Guid StoreGeneration,
    long AcceptedAtNs,long ExpiresAtNs,DirectProcessIdentity Controller,ExperimentalRuntimePin Runtime);

internal static class IntegratedRecoveryHost
{
    // One bounded preparation worker; this thread remains the native safety monitor and original reaper.
    internal static int Run(RecoveryConfiguration config,M1RecoveryScope scope,RecoveryWire wire,int control,int controllerFd,string ownerPath)
    {
        var spec=config.Integrated!;
        spec.Runtime.VerifyCurrent();
        foreach(var directory in new[] {spec.Profile.SourceRootPath,spec.Profile.StagingParentPath,spec.Profile.JournalDirectory,config.EvidenceDirectory})
            ExperimentalRuntimePin.VerifyPrivateDirectory(directory);
        ExperimentalRuntimePin.VerifyPrivateClosure(config.Owner); ExperimentalRuntimePin.VerifyPrivateClosure(config.Recovery);
        if(config.DelegatedParent is null || config.DelegatedParent!=spec.Profile.DelegatedParentPath
            || spec.Profile.OwnerDllPath!=ownerPath || spec.Controller.Pid!=config.ControllerPid
            || spec.Controller.Uid!=config.ControllerUid || spec.Controller!=(DirectProcessIdentity.Current() with {Pid=config.ControllerPid})
            || !DirectPeerIdentity.LiveReference(controllerFd,spec.Controller) || !DirectPeerIdentity.FeatureAvailable()
            || spec.AcceptedAtNs<=0 || spec.ExpiresAtNs<=spec.AcceptedAtNs || spec.ExpiresAtNs-spec.AcceptedAtNs>B3FixtureProfile.MaxExpiresLeadNanoseconds)
            throw new IOException("Integrated experimental prerequisites refused");
        foreach(var pair in new[] {(spec.Profile.ToolSourceName,spec.Profile.ToolSha256),
            (spec.Profile.InputSourceName,spec.Profile.InputSha256),(spec.Profile.CoordinatorSourceName,spec.Profile.CoordinatorSha256),
            (spec.Profile.FilterSourceName,Launch.BubblewrapAdapter.FilterSha256)})
            if(!B3Qualification.VerifySha256(Path.Combine(spec.Profile.SourceRootPath,pair.Item1),pair.Item2))
                throw new IOException("Integrated fixture closure refused");
        var stateGate=new object(); var sendGate=new object();
        Process? owner=null; var ownerPidfd=-1; var rendezvousSent=false;
        var controllerLost=false; var ownerExited=false; var ownerReaped=false;
        var controlUsable=true; var nativeStop="Unproven"; var writerFence=false;
        var domainRemoved=false; var domainEmpty=false; var stagingRemoved=false; var journalFinalized=false;
        var obligation=new RecoveryObligation();
        using var journalStorage=new JournalFileStorage();
        var journal=new OwnershipJournal(journalStorage,spec.Profile.JournalDirectory);
        using var launch=new IntegratedOwnerLaunch(new OwnerDirectIdentity(config.Session,spec.Controller,
            DirectProcessIdentity.Current(),spec.Runtime),controllerFd,spec.AcceptedAtNs,
            (process,pidfd)=> {lock(stateGate) {owner=process;ownerPidfd=pidfd;}},
            (rendezvous,pidfd)=>
            {
                lock(sendGate)
                {
                    if(Int01Qualification.Is(spec.Profile.QualificationFault,"actual-recovery-release-impersonation"))
                    {
                        using var attacker=new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.Unix,System.Net.Sockets.SocketType.Stream,System.Net.Sockets.ProtocolType.Unspecified);
                        attacker.ReceiveTimeout=1000;
                        attacker.Connect(new System.Net.Sockets.UnixDomainSocketEndPoint(rendezvous.Address));
                        try { attacker.Send(new byte[128]); _=attacker.Receive(new byte[1]); }
                        catch(System.Net.Sockets.SocketException) { }
                        Int01Qualification.Reached(spec.Profile.QualificationFault,"recovery-before-rendezvous");
                    }
                    var payload=JsonSerializer.SerializeToUtf8Bytes(rendezvous);
                    if(!FdPassing.TrySendTransfer(control,[pidfd],wire.Create(RecoveryMessage.Rendezvous,payload),out _))
                        throw new IOException("Direct rendezvous/lifetime publication failed");
                    Volatile.Write(ref rendezvousSent,true);
                }
            });
        var preparation=Task.Run(()=>
        {
            if(!CgroupInterop.TryReadBootId(out var boot,out _) || !journal.TryAcquireWriter(out var writer,out _) || writer is null)
                throw new IOException("Integrated intent authority unavailable");
            using(writer)
            {
                var request=new OwnerPreparationRequest(ownerPath,"int-"+config.Execution.ToString("N"),config.Execution,
                    config.Generation,boot,B3PreparationOwner.BuildPlan(spec.Profile),journal,writer,spec.Issuer,spec.StoreGeneration,
                    _=>throw new InvalidOperationException("Recovery cannot authorize a manifest"),spec.ExpiresAtNs,
                    EnableHeldCoordinator:true,Integrated:launch,QualificationFault:spec.Profile.QualificationFault);
                return scope.StartOwnerPreparation(request,CancellationToken.None);
            }
        });
        Task? cleanup=null;
        RecoverySnapshot Snapshot()
        {
            lock(stateGate) return new(Environment.ProcessId,owner?.Id??0,config.ControllerPid,RecoveryInterop.GetSid(0),scope.SubreaperEnabled,
                controllerLost,ownerExited,obligation.Count,obligation.Cause,obligation.Deadline,obligation.OwnerAcknowledged,
                NativeStop:nativeStop,RetainedDomains:scope.RetainedDomainCount,DomainRemoved:domainRemoved,
                OwnerReaped:ownerReaped,WriterFence:writerFence,StagingRemoved:stagingRemoved,JournalFinalized:journalFinalized,DomainEmpty:domainEmpty);
        }
        void Stop(string cause)
        {
            if(obligation.Request(cause,Stopwatch.GetTimestamp())) _=launch.RequestStop();
            // Independently available throughout preparation, ACK waits, observation and cleanup.
            nativeStop=scope.SafetyStopRetained();
        }
        var ownerKillAttempted=false;
        while(true)
        {
            if(!PidFd.TryExited(controllerFd,0,out var exited,out var error))
            {if(!RecoveryInterop.IsTransientPidfdError(error)) {controllerLost=true;Stop("ControllerEvidenceLost");}}
            else if(exited) {controllerLost=true;Stop("ControllerExited");}
            if(controlUsable && Volatile.Read(ref rendezvousSent))
            {
                if(FdPassing.TryReceiveMessage(control,0,out var frame,out error))
                {
                    if(!wire.Accept(frame,out var type,out _)) {controlUsable=false;Stop("InvalidControllerFrame");}
                    else if(type==RecoveryMessage.Stop) Stop("ControllerStop");
                    else if(type==RecoveryMessage.Inspect)
                    {
                        lock(sendGate)
                            if(!FdPassing.TrySendMessage(control,wire.Create(RecoveryMessage.Snapshot,JsonSerializer.SerializeToUtf8Bytes(Snapshot())),out _))
                            {controlUsable=false;controllerLost=true;Stop("ControllerTransportLost");}
                    }
                    else {controlUsable=false;Stop("OriginalAssociationRefused");}
                }
                else if(!RecoveryInterop.IsTransientReceiveError(error)) {controlUsable=false;controllerLost=true;Stop("ControllerTransportLost");}
            }
            Process? current; int pidfd;
            lock(stateGate) {current=owner;pidfd=ownerPidfd;}
            if(current is not null && !ownerExited)
            {
                if(!PidFd.TryExited(pidfd,0,out exited,out error))
                {if(!RecoveryInterop.IsTransientPidfdError(error)) Stop("OwnerEvidenceLost");}
                else if(exited)
                {
                    ownerExited=true;Stop("OwnerExited");
                    ownerReaped=scope.WaitForOwner(current,TimeSpan.FromSeconds(1)).Exited
                        && scope.ReapAdoptedChildren(TimeSpan.Zero,out _,out _);
                }
            }
            if(ownerExited && !ownerReaped && current is not null)
                ownerReaped=scope.WaitForOwner(current,TimeSpan.Zero).Exited && scope.ReapAdoptedChildren(TimeSpan.Zero,out _,out _);
            if(preparation.IsCompleted)
            {
                if(preparation.IsFaulted || preparation.IsCanceled || !preparation.Result.Ok) Stop("PreparationFailed");
                else if(!scope.RetentionValid) Stop("NativeRetentionLost");
                // Retention is NOT a stop acceptance ACK or controller release authorization.
            }
            if(launch.ObserveOwnerStop()) obligation.OwnerAcknowledged=true;
            if(OwnerSafetyMonitor.Now()>=Math.Min(spec.ExpiresAtNs,spec.AcceptedAtNs+30_000_000_000L)) Stop("OriginalOverallExpired");
            if(obligation.Count!=0)
            {
                nativeStop=scope.SafetyStopRetained();
                if(!ownerExited && current is not null && !ownerKillAttempted && Stopwatch.GetTimestamp()>=obligation.Deadline)
                {ownerKillAttempted=true;_=PidFd.TrySignal(pidfd,9,out _);}
            }
            if(ownerExited && ownerReaped && preparation.IsCompleted && cleanup is null)
            {
                cleanup=Task.Run(()=>
                {
                    // Exit, reaping, writer fencing and durable reconciliation are separate verified events.
                    var read=journal.ReadLatest();
                    if(!read.Ok || read.Snapshot is null || !CgroupInterop.TryReadBootId(out var currentBoot,out _)
                        || currentBoot!=read.Snapshot.BootId || !journal.TryAcquireWriter(out var writer,out _) || writer is null) return;
                    using(writer)
                    {
                        lock(stateGate) writerFence=true;
                        var cleaner=scope.RetainedStagingParents.Count==1 && scope.RetainedStagingDirectories.Count==1
                            ?new B3ScopeCleaner(scope.RetainedStagingParents[0].DangerousGetHandle().ToInt32(),
                                scope.RetainedStagingDirectories[0].DangerousGetHandle().ToInt32()):null;
                        var lease=scope.RetainedRecoveryLeases.Count==1?scope.RetainedRecoveryLeases[0]:null;
                        // No new waiting allowance is created after the original stop obligation. This
                        // immediate cleanup attempt is NOT full sealed C3/B5 deadline reconciliation.
                        var result=new Reconciler(new Clock(),TimeSpan.Zero).Reconcile(read.Snapshot,currentBoot,
                            lease,cleaner,journal,writer);
                        bool Confirmed(CleanupComponents component)=>result.Components?.Any(x=>x.Component==component && x.State==ComponentState.Confirmed)==true;
                        lock(stateGate)
                        {domainEmpty=Confirmed(CleanupComponents.DomainEmpty);domainRemoved=Confirmed(CleanupComponents.DomainRemoval);stagingRemoved=Confirmed(CleanupComponents.Staging);
                            journalFinalized=Confirmed(CleanupComponents.JournalFinalization);}
                    }
                    var finalJournal=journal.ReadLatest();
                    if(finalJournal.Ok && finalJournal.Snapshot is { } final)
                        RecoveryEvidence.Write(config.EvidenceDirectory,"integrated-journal-final.json",new {
                            final.ExecutionId,final.LeaseGeneration,final.Sequence,
                            Release=final.Release,Exec=final.Exec,Root=final.Root,Stop=final.Stop,Cleanup=final.Cleanup});
                    RecoveryEvidence.Write(config.EvidenceDirectory,"integrated-final.json",Snapshot());
                });
            }
            if(cleanup?.IsCompleted==true)
            {
                // Keep final observations available briefly; no allowance/authority is renewed.
                if(controllerLost || !controlUsable) return cleanup.IsCompletedSuccessfully?0:3;
            }
            if(current is null && preparation.IsCompleted) return 3;
            Thread.Sleep(10);
        }
    }
    private sealed class Clock:IDomainClock
    {
        public long NowNanoseconds=>OwnerSafetyMonitor.Now();
        public void Delay(TimeSpan duration)=>Thread.Sleep(duration);
    }
}
