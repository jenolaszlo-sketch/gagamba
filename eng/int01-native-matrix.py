#!/usr/bin/env python3
"""Producer-only INT01 qualification. Never provisions cgroups or elevates.
Full security matrix remains mandatory; this runner implements the core actual-process
cases below. Other required fault injections are explicitly pending, never passed.
"""
import argparse, copy, hashlib, json, os, pathlib, select, shutil, signal, subprocess, tempfile, time

CORE_CASES=('positive-one-release','stop-before-release','controller-sigkill-held','recovery-sigkill-held',
            'owner-sigkill-held','controller-sigkill-after-release','recovery-sigkill-after-release','owner-sigkill-after-release')
PENDING_CASES=('controller-before-preparation','controller-during-preparation','controller-release-race',
 'recovery-retention-death','recovery-release-race','owner-before-retention','owner-mid-staging','owner-after-intent',
 'owner-ambiguous-delivery','missing-domain-ack','missing-staging-ack','wrong-descriptor-role-count',
 'partial-scm-rights','lost-retention-ack','blocked-observer-native-stop','simultaneous-controller-recovery-death',
 'integrated-stop-before-mint','integrated-stop-after-intent-before-write','integrated-ambiguous-intent',
 'integrated-lost-release-ack','integrated-original-deadline','integrated-owner-live-takeover-refusal',
 'integrated-post-exit-writer-fence','actual-recovery-release-impersonation')

def emit(path,value):
    path.write_text(json.dumps(value,indent=2)+'\n')

def prerequisites(profile):
    checks=[]
    def check(name,ok,detail): checks.append({'Name':name,'Passed':ok,'Detail':detail})
    check('unprivileged',os.getuid()!=0,f'uid={os.getuid()} gid={os.getgid()}')
    fields=dict(line.split(':',1) for line in pathlib.Path('/proc/self/status').read_text().splitlines() if ':' in line)
    for cap in ('CapInh','CapPrm','CapEff','CapAmb'): check(cap,int(fields[cap].strip(),16)==0,fields[cap].strip())
    delegated=os.environ.get('GAGAMBA_DELEGATED_CGROUP','')
    check('operator-delegation-declared',bool(delegated),'GAGAMBA_DELEGATED_CGROUP='+delegated)
    if delegated:
        parent=pathlib.Path(delegated)
        check('delegated-parent-exists',parent.is_dir(),str(parent))
        if parent.is_dir():
            controls=(parent/'cgroup.subtree_control').read_text().split()
            check('memory-pids-delegated',{'memory','pids'}<=set(controls),' '.join(controls))
            membership=pathlib.Path('/proc/self/cgroup').read_text().strip().split('::')[-1]
            expected='/'+str(parent.relative_to('/sys/fs/cgroup'))+'/host'
            check('host-relationship',membership==expected,f'actual={membership} expected={expected}')
            check('operator-write-authority',os.access(parent,os.W_OK),str(parent))
    else:
        check('host-relationship',False,pathlib.Path('/proc/self/cgroup').read_text().strip()+'; no authorized parent')
    check('pinned-integrated-profile',bool(profile) and pathlib.Path(profile).is_file(),profile or 'not provided')
    controller=os.environ.get('GAGAMBA_B2_CONTROLLER_DLL','')
    check('pinned-controller-artifact',bool(controller) and pathlib.Path(controller).is_file(),controller or 'not provided')
    return checks

def read_line(process,timeout=15):
    ready,_,_=select.select([process.stdout],[],[],timeout)
    if not ready: raise AssertionError('bounded controller response timed out')
    line=process.stdout.readline(65537)
    if not line or len(line)>65536: raise AssertionError('controller response lost/oversized')
    return line.strip()

def command(process,value):
    process.stdin.write(value+'\n');process.stdin.flush();line=read_line(process)
    return json.loads(line) if line[:1] in '{[n' else line

def exited(fd):
    poll=select.poll();poll.register(fd,select.POLLIN);return bool(poll.poll(0))

def kill_exact(fd): signal.pidfd_send_signal(fd,signal.SIGKILL)

def run_case(base,case,report):
    profile=copy.deepcopy(base)
    workspace=pathlib.Path(tempfile.mkdtemp(prefix='gagamba-int01-',dir=pathlib.Path(base['SourceRootPath']).parent))
    for name in ('source','staging','journal','evidence'): (workspace/name).mkdir(mode=0o700)
    for field in ('ToolSourceName','InputSourceName','CoordinatorSourceName','FilterSourceName'):
        source=pathlib.Path(base['SourceRootPath'])/base[field]
        assert source.stat().st_size<=16*1024*1024
        target=workspace/'source'/base[field];shutil.copyfile(source,target);target.chmod(0o600)
    profile.update(SourceRootPath=str(workspace/'source'),StagingParentPath=str(workspace/'staging'),JournalDirectory=str(workspace/'journal'))
    profile['IntegratedRecovery']['EvidenceDirectory']=str(workspace/'evidence')
    runtime=profile['IntegratedRecovery']['Runtime'];controller=os.environ['GAGAMBA_B2_CONTROLLER_DLL']
    facts={'Case':case,'Workspace':str(workspace),'Status':'Failed','Cleanup':'Unknown'}
    refs={};process=None
    try:
        process=subprocess.Popen([runtime['HostPath'],controller,'--int01-controller'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,
            stderr=(workspace/'controller.stderr').open('w'),text=True,bufsize=1,
            env={'PATH':'/usr/bin:/bin','DOTNET_ROOT':str(pathlib.Path(runtime['HostPath']).parent),'DOTNET_EnableDiagnostics':'0','DOTNET_ROLL_FORWARD':'LatestPatch'})
        refs['controller']=os.pidfd_open(process.pid)
        process.stdin.write(json.dumps(profile)+'\n');process.stdin.flush();assert read_line(process)=='READY'
        assert command(process,'prepare')=='PREPARING'
        prepared=command(process,'prepared');assert prepared['Kind']==1,prepared
        snapshot=command(process,'inspect');facts['Held']=snapshot
        assert snapshot['ControllerPid']==process.pid and snapshot['RetainedDomains']==1 and not snapshot['ReleaseAuthority']
        for actor in ('recovery','owner'): refs[actor]=os.pidfd_open(snapshot[actor.title()+'Pid'])
        owner_status=pathlib.Path(f"/proc/{snapshot['OwnerPid']}/status").read_text()
        ppid=int(next(x.split(':')[1] for x in owner_status.splitlines() if x.startswith('PPid:')))
        assert ppid==snapshot['RecoveryPid'],(ppid,snapshot)
        facts['ActualOwnerParent']=ppid
        facts['RetainedPeerPidfds']={actor:os.readlink(f'/proc/self/fd/{fd}') for actor,fd in refs.items()}
        if case=='stop-before-release':
            facts['Stop']=command(process,'stop');facts['LaterRelease']=command(process,'release')
            assert facts['LaterRelease']['Kind']!=2
        elif case=='positive-one-release' or case.endswith('after-release'):
            release=command(process,'release');facts['Release']=release;assert release['Kind']==2,release
            observation=command(process,'observe');facts['Execution']=observation
            assert observation['PhysicalReleaseAttempts']==1,observation
            if case=='positive-one-release':
                duplicate=command(process,'release');assert duplicate['Kind']==3,duplicate
                assert command(process,'observe')['PhysicalReleaseAttempts']==1
        if '-sigkill-' in case:
            actor=case.split('-')[0];kill_exact(refs[actor]);facts['KilledActualActor']=actor
            deadline=time.monotonic()+12
            if actor=='controller':
                assert not exited(refs['recovery']),'recovery died with original controller'
                final_path=workspace/'evidence'/'integrated-final.json'
                while time.monotonic()<deadline and not final_path.exists(): time.sleep(.02)
                assert final_path.exists(),'recovery completion unavailable'
                final=json.loads(final_path.read_text());facts['RecoveryFinal']=final
                assert final['OwnerReaped'] and final['WriterFence'],final
                assert exited(refs['owner']),'actual owner exit not proved'
                facts['Cleanup']='Confirmed' if final['DomainEmpty'] and final['StagingRemoved'] and final['JournalFinalized'] else 'Unknown'
            else:
                while time.monotonic()<deadline and not exited(refs['owner']): time.sleep(.02)
                assert exited(refs['owner']),'actual surviving owner did not stop/exit'
                facts['PostFault']=command(process,'observe')
                if actor=='owner':
                    final=command(process,'inspect')
                    while time.monotonic()<deadline and not (final and final['OwnerReaped'] and final['WriterFence']):
                        time.sleep(.02);final=command(process,'inspect')
                    facts['RecoveryFinal']=final
                    assert final is not None and final['OwnerReaped'] and final['WriterFence'] and not final['ReleaseAuthority']
                if case.endswith('after-release'):
                    assert facts['PostFault']['Observation']['Release']==facts['Execution']['Observation']['Release'],facts
                else:
                    later=command(process,'release');facts['LaterRelease']=later;assert later['Kind']!=2,later
                # Recovery death supplies no independent reaping/fence proof: cleanup remains Unknown.
        else:
            cleanup=command(process,'dispose');facts['FinalObservation']=cleanup
            assert cleanup['Cleanup']['State']==1,cleanup
            facts['Cleanup']='Confirmed'
        facts['Status']='Passed'
    except Exception as error:
        facts['Error']=str(error)
    finally:
        # Test watchdog cleanup is distinct from observed recovery/owner stopping. No guessed PIDs or privileged fallback.
        for actor in ('controller','owner','recovery'):
            if actor in refs and not exited(refs[actor]):
                try: kill_exact(refs[actor])
                except ProcessLookupError: pass
        if process is not None:
            try: process.wait(timeout=5)
            except subprocess.TimeoutExpired: facts['WatchdogCleanup']='Unknown'
        for fd in refs.values(): os.close(fd)
        emit(report/(case+'.json'),facts)
    return facts['Status']=='Passed'

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--profile',default=os.environ.get('GAGAMBA_INT01_PROFILE'))
    parser.add_argument('--report',required=True);parser.add_argument('--preflight-only',action='store_true');args=parser.parse_args()
    report=pathlib.Path(args.report);report.mkdir(parents=True,exist_ok=True)
    checks=prerequisites(args.profile);ready=all(x['Passed'] for x in checks)
    evidence={'Host':os.uname()._asdict() if hasattr(os.uname(),'_asdict') else list(os.uname()),'Checks':checks,
        'CoreCases':list(CORE_CASES),'AdditionalRequiredCases':list(PENDING_CASES),
        'NativeRun':'NotRun' if not ready or args.preflight_only else 'Requested',
        'Disposition':'BLOCKED BY ENVIRONMENTAL PREREQUISITE' if not ready else 'PREREQUISITES ONLY',
        'AdditionalRequiredCasesStatus':'Pending; not qualified by the core runner'}
    emit(report/'preflight.json',evidence)
    print(json.dumps(evidence,indent=2))
    if not ready: return 2
    if args.preflight_only:return 0
    base=json.loads(pathlib.Path(args.profile).read_text());passed=sum(run_case(base,case,report) for case in CORE_CASES)
    print(f'{passed} passed / {len(CORE_CASES)-passed} failed / {len(PENDING_CASES)} additional required cases pending')
    return 0 if passed==len(CORE_CASES) else 1
if __name__=='__main__': raise SystemExit(main())
