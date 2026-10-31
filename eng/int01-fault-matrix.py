#!/usr/bin/env python3
"""Actual-process INT01 fault qualification. No cgroup provisioning or elevation."""
import argparse,copy,importlib.util,json,os,pathlib,shutil,signal,subprocess,tempfile,time
spec=importlib.util.spec_from_file_location('int01_core',pathlib.Path(__file__).with_name('int01-native-matrix.py'))
core=importlib.util.module_from_spec(spec);spec.loader.exec_module(core)
STAGES={
 'controller-before-preparation':('controller-ready','controller','kill'),
 'controller-during-preparation':('owner-before-allocation','controller','kill'),
 'recovery-retention-death':('owner-domain-retained','recovery','kill'),
 'owner-before-retention':('owner-before-domain-transfer','owner','kill'),
 'owner-mid-staging':('owner-first-object-accounted','owner','kill'),
 'missing-domain-ack':('recovery-before-domain-ack','recovery','omit'),
 'missing-staging-ack':('recovery-before-staging-ack','recovery','omit'),
 'lost-retention-ack':('recovery-before-prepared-ack','recovery','omit'),
 'wrong-descriptor-role-count':('owner-domain-transfer','owner','malform'),
 'partial-scm-rights':('owner-domain-transfer','owner','malform'),
 'integrated-stop-before-mint':('owner-before-mint','owner','stop'),
 'controller-release-race':('release-after-intent','controller','kill'),
 'recovery-release-race':('release-after-intent','recovery','kill'),
 'owner-after-intent':('release-after-intent','owner','kill'),
 'integrated-stop-after-intent-before-write':('release-after-intent','owner','stop'),
 'owner-ambiguous-delivery':('release-after-write','owner','self-kill'),
 'integrated-lost-release-ack':('owner-before-release-reply','owner','drop-reply'),
 'blocked-observer-native-stop':('owner-before-observation','controller','kill'),
 'actual-recovery-release-impersonation':('recovery-before-rendezvous','recovery','impersonate'),
 'integrated-ambiguous-intent':('owner-intent-storage','owner','filesystem-conflict'),
}
assert set(STAGES)==set(core.PENDING_CASES)-set(core.IMPLEMENTED_FAULT_CASES)

def wait_file(path,seconds=10):
    end=time.monotonic()+seconds
    while time.monotonic()<end:
        if path.exists():return json.loads(path.read_text())
        time.sleep(.005)
    raise AssertionError('requested injection stage was not reached: '+str(path))

def wait_exit(fd,seconds=12):
    end=time.monotonic()+seconds
    while time.monotonic()<end and not core.exited(fd):time.sleep(.01)
    assert core.exited(fd),'actual owner exit not established'

def run_case(base,case,report):
    stage,actor,action=STAGES[case]
    workspace=pathlib.Path(tempfile.mkdtemp(prefix='gagamba-qf02-',dir=pathlib.Path(base['SourceRootPath']).parent))
    for name in ('source','staging','journal','evidence'):(workspace/name).mkdir(mode=0o700)
    profile=copy.deepcopy(base)
    for field in ('ToolSourceName','InputSourceName','CoordinatorSourceName','FilterSourceName'):
        source=pathlib.Path(base['SourceRootPath'])/base[field];target=workspace/'source'/base[field]
        assert source.stat().st_size<=16*1024*1024
        shutil.copyfile(source,target);target.chmod(0o600)
    profile.update(SourceRootPath=str(workspace/'source'),StagingParentPath=str(workspace/'staging'),JournalDirectory=str(workspace/'journal'),QualificationFault={'Case':case,'EvidenceDirectory':str(workspace/'evidence')})
    profile['IntegratedRecovery']['EvidenceDirectory']=str(workspace/'evidence')
    facts={'Case':case,'Workspace':str(workspace),'Status':'Failed','Cleanup':'Unknown',
      'Expected':{'Actor':actor,'Stage':stage,'Action':action,'Invariant':'no unauthenticated or unretained release; original identity/deadlines; at most one physical write; uncertainty retained'}}
    refs={};process=None
    initial_domains={str(p) for p in pathlib.Path(base['DelegatedParentPath']).glob('int-*')}
    try:
        runtime=profile['IntegratedRecovery']['Runtime'];controller=os.environ['GAGAMBA_B2_CONTROLLER_DLL']
        process=subprocess.Popen([runtime['HostPath'],controller,'--int01-controller'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,
          stderr=(workspace/'controller.stderr').open('w'),text=True,bufsize=1,
          env={'PATH':'/usr/bin:/bin','DOTNET_ROOT':str(pathlib.Path(runtime['HostPath']).parent),'DOTNET_EnableDiagnostics':'0','DOTNET_ROLL_FORWARD':'LatestPatch'})
        refs['controller']=os.pidfd_open(process.pid)
        process.stdin.write(json.dumps(profile)+'\n');process.stdin.flush();assert core.read_line(process)=='READY'
        if stage=='controller-ready':
            facts['BeforePrepare']=core.command(process,'inspect');assert facts['BeforePrepare'] is None
            assert not list((workspace/'journal').iterdir())
            facts['ControllerKernelStatus']=pathlib.Path(f'/proc/{process.pid}/status').read_text()
            core.kill_exact(refs['controller']);wait_exit(refs['controller'])
            facts['InjectionReached']=True;facts['NoPreparationRequested']=True
            assert initial_domains=={str(p) for p in pathlib.Path(base['DelegatedParentPath']).glob('int-*')}
            facts['NoNewDomainCreated']=True
            facts['Status']='Passed';return True
        assert core.command(process,'prepare')=='PREPARING'
        release_phase=stage.startswith('release-') or stage in ('owner-before-release-reply','owner-before-observation','owner-intent-storage')
        if release_phase or action=='impersonate':
            prepared=core.command(process,'prepared');facts['Preparation']=prepared;assert prepared['Kind']==1,prepared
        else:
            prepared=None
        if release_phase:
            facts['Held']=core.command(process,'inspect')
            for role in ('owner','recovery'):refs[role]=os.pidfd_open(facts['Held'][role.title()+'Pid'])
            facts['HeldJournal']=core.command(process,'journal');assert facts['HeldJournal']['Release'] is None
            if action=='filesystem-conflict':(workspace/'evidence'/'arm-intent-storage').touch()
            assert core.command(process,'begin-release')=='RELEASING'
        marker=wait_file(workspace/'evidence'/('fault-'+stage+'.json'))
        facts['InjectionReceipt']=marker;facts['InjectionReached']=True
        assert marker['Case']==case and marker['Stage']==stage
        if not release_phase:
            snapshot=core.command(process,'inspect');facts['AtInjection']=snapshot
            assert snapshot is not None
            for role in ('owner','recovery'):refs[role]=os.pidfd_open(snapshot[role.title()+'Pid'])
        else:snapshot=facts['Held']
        assert not snapshot['ReleaseAuthority']
        facts['JournalAtInjection']=core.command(process,'journal')
        if stage=='release-after-intent':
            assert facts['JournalAtInjection']['Release'] is not None and facts['JournalAtInjection']['Release']['Tag']==1,'durable release Intent not observed at injection'
        domain=pathlib.Path(profile['DelegatedParentPath'])/((facts.get('HeldJournal') or facts['JournalAtInjection'])['DomainName'])
        facts['DomainPath']=str(domain)
        if domain.exists():facts['DomainBeforeFault']={'Events':(domain/'cgroup.events').read_text(),'Procs':(domain/'cgroup.procs').read_text(),'Inode':domain.stat().st_ino}
        owner_status=pathlib.Path(f"/proc/{snapshot['OwnerPid']}/status").read_text() if not core.exited(refs['owner']) else None
        if owner_status is not None:
            assert int(next(l.split(':')[1] for l in owner_status.splitlines() if l.startswith('PPid:')))==snapshot['RecoveryPid']
            facts['OwnerKernelStatus']=owner_status
        if action=='impersonate':
            rejected=wait_file(workspace/'evidence'/'fault-owner-refused-recovery-peer.json');facts['PeerRejection']=rejected
            assert rejected['Detail']['ActualPid']==snapshot['RecoveryPid'] and rejected['Detail']['ActualPid']!=snapshot['ControllerPid']
            release=core.command(process,'release');facts['Release']=release;assert release['Kind']==2
            observation=core.command(process,'observe');facts['Execution']=observation
            assert observation['PhysicalReleaseAttempts']==1 and observation['Observation']['Exec']['Kind']==3 and observation['Observation']['Root']['ExitCode']==0
            facts['Duplicate']=core.command(process,'release');assert facts['Duplicate']['Kind']==3
        elif action=='kill':
            assert snapshot[actor.title()+'Pid']==(process.pid if actor=='controller' else snapshot[actor.title()+'Pid'])
            core.kill_exact(refs[actor]);facts['Signal']={'Actor':actor,'Signal':'SIGKILL'}
            if case=='blocked-observer-native-stop':
                sample_end=time.monotonic()+2
                while time.monotonic()<sample_end:
                    preexit={'AtNs':time.monotonic_ns(),'Events':(domain/'cgroup.events').read_text(),'Inode':domain.stat().st_ino}
                    if 'populated 0' in preexit['Events']:break
                    time.sleep(.002)
                facts['PreOwnerExitPopulation']=preexit
                facts['OwnerAliveAtEmptySample']=not core.exited(refs['owner'])
                assert 'populated 0' in preexit['Events'] and facts['OwnerAliveAtEmptySample']
                (workspace/'evidence'/'continue-owner-before-observation').touch()
            wait_exit(refs['owner'])
            facts['OwnerExitPidfd']=True
            if domain.exists():facts['DomainAfterFault']={'Events':(domain/'cgroup.events').read_text(),'Procs':(domain/'cgroup.procs').read_text(),'Inode':domain.stat().st_ino}
            if case=='blocked-observer-native-stop':
                assert 'populated 1' in facts['DomainBeforeFault']['Events'],'blocked observer requires a populated native domain'
                facts['IndependentNativeStop']=wait_file(workspace/'evidence'/'fault-owner-native-stop.json')
                assert facts['IndependentNativeStop']['Detail']['KernelAccepted']
                assert 'populated 0' in facts['DomainAfterFault']['Events']
            if release_phase and stage=='release-after-intent':
                before=wait_file(workspace/'evidence'/'fault-owner-before-physical-release.json');facts['PreWriteCounter']=before
                assert before['Detail']['PhysicalReleaseAttempts']==0
                if actor!='owner':
                    decision=wait_file(workspace/'evidence'/'fault-owner-release-decision.json');facts['OwnerDecision']=decision
                    assert decision['Detail']['PhysicalReleaseAttempts']==0 and decision['Detail']['Kind']!='Released'
        elif action in ('omit','malform'):
            facts['BeforeContinuation']=core.command(process,'prepare-status');assert not facts['BeforeContinuation']['Prepared']
            (workspace/'evidence'/('continue-'+stage)).touch()
            result=core.command(process,'prepared');facts['Preparation']=result;assert result['Kind']!=1,result
            if action=='omit':
                facts['ActualOmission']=wait_file(workspace/'evidence'/'fault-recovery-ack-omitted.json')
                assert facts['ActualOmission']['Detail']['Withheld']
            if action=='malform':
                rejected=wait_file(workspace/'evidence'/'fault-recovery-transfer-rejected.json');facts['ActualTransferRejection']=rejected
                if case=='wrong-descriptor-role-count':
                    assert rejected['Detail']['ObservedRoleCount']==6 and rejected['Detail']['DescriptorCount']==7
                else:
                    assert rejected['Detail']['ExpectedDescriptorCount']==7 and 'descriptors 6/7' in rejected['Detail']['Error']
        elif action=='stop':
            assert core.command(process,'queue-stop')=='STOP-QUEUED'
            facts['NativeStop']=wait_file(workspace/'evidence'/'fault-owner-native-stop.json')
            assert facts['NativeStop']['Detail']['KernelAccepted']
            (workspace/'evidence'/('continue-'+stage)).touch()
            if release_phase:
                facts['Release']=core.command(process,'release-result');assert facts['Release']['Kind']!=2
                facts['OwnerDecision']=wait_file(workspace/'evidence'/'fault-owner-release-decision.json')
                assert facts['OwnerDecision']['Detail']['PhysicalReleaseAttempts']==0
            else:
                facts['Preparation']=core.command(process,'prepared');assert facts['Preparation']['Kind']!=1
        elif action=='self-kill':
            wait_exit(refs['owner']);facts['PhysicalWrite']=wait_file(workspace/'evidence'/'fault-physical-release.json')
            assert facts['PhysicalWrite']['Detail']['PhysicalReleaseAttempts']==1
            facts['Release']=core.command(process,'release-result');assert facts['Release']['Kind']!=2
        elif action=='drop-reply':
            facts['Release']=core.command(process,'release-result');assert facts['Release']['Kind']!=2
            facts['OwnerDecision']=wait_file(workspace/'evidence'/'fault-owner-release-decision.json')
            assert facts['OwnerDecision']['Detail']['Kind']=='Released' and facts['OwnerDecision']['Detail']['PhysicalReleaseAttempts']==1
            facts['LaterRelease']=core.command(process,'release');assert facts['LaterRelease']['Kind']!=2
        elif action=='filesystem-conflict':
            facts['Release']=core.command(process,'release-result');assert facts['Release']['Kind']==5,facts['Release']
            assert (workspace/'journal'/'gen.a').is_dir() and (workspace/'journal'/'gen.seal').exists()
            facts['OwnerDecision']=wait_file(workspace/'evidence'/'fault-owner-release-decision.json')
            assert facts['OwnerDecision']['Detail']['PhysicalReleaseAttempts']==0
            facts['LaterRelease']=core.command(process,'release');assert facts['LaterRelease']['Kind']==3
        if not release_phase and action not in ('impersonate',):
            assert not (workspace/'evidence'/'fault-owner-capability-published.json').exists(),'usable capability publication was observed'
        # Recovery's own final record, when available, is independent of harness watchdog cleanup.
        if not (actor=='recovery' and action=='kill') and action!='filesystem-conflict':
            if not core.exited(refs['controller']):
                try:facts['Disposal']=core.command(process,'dispose')
                except (AssertionError,BrokenPipeError) as e:facts['DisposalObservationError']=str(e)
            final_path=workspace/'evidence'/'integrated-final.json'
            end=time.monotonic()+8
            while time.monotonic()<end and not final_path.exists() and not core.exited(refs['recovery']):time.sleep(.01)
            if not final_path.exists():facts['RecoveryFinalObservation']='Unavailable before watchdog; cleanup remains Unknown'
            if final_path.exists():
                final=json.loads(final_path.read_text());facts['RecoveryFinal']=final
                facts['Cleanup']='Confirmed' if all(final[k] for k in ('OwnerReaped','WriterFence','DomainEmpty','StagingRemoved','JournalFinalized')) else 'Unknown'
        facts['Status']='Passed'
    except Exception as error:facts['Error']=repr(error)
    finally:
        facts['WatchdogActions']=[]
        for role in ('controller','owner','recovery'):
            if role in refs and not core.exited(refs[role]):
                try:core.kill_exact(refs[role]);facts['WatchdogActions'].append(role+':SIGKILL')
                except ProcessLookupError:pass
        if process is not None:
            try:process.wait(timeout=5)
            except subprocess.TimeoutExpired:facts['WatchdogCleanup']='Unknown'
        for fd in refs.values():os.close(fd)
        facts['FaultEvidence']={p.name:json.loads(p.read_text()) for p in (workspace/'evidence').glob('fault-*.json')}
        core.emit(report/(case+'.json'),facts)
    return facts['Status']=='Passed'

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--profile',default=os.environ.get('GAGAMBA_INT01_PROFILE'))
    parser.add_argument('--report',required=True);parser.add_argument('--case',choices=tuple(STAGES));args=parser.parse_args()
    report=pathlib.Path(args.report);report.mkdir(parents=True,exist_ok=True)
    checks=core.prerequisites(args.profile);core.emit(report/'preflight.json',{'Checks':checks})
    if not all(c['Passed'] for c in checks):return 2
    base=json.loads(pathlib.Path(args.profile).read_text());cases=(args.case,) if args.case else tuple(STAGES)
    core.emit(report/'expectations.json',{c:{'Stage':STAGES[c][0],'Actor':STAGES[c][1],'Fault':STAGES[c][2]} for c in cases})
    passed=sum(run_case(base,c,report) for c in cases)
    print(f'{passed} passed / {len(cases)-passed} failed / {len(cases)} requested')
    return int(passed!=len(cases))
if __name__=='__main__':raise SystemExit(main())
