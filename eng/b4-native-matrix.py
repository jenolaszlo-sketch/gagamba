#!/usr/bin/env python3
"""Bounded B4 producer qualification; consumes an existing unprivileged delegation.
No provisioning, elevation, release retry, journal adoption or cleanup promotion.
"""
import argparse,array,copy,fcntl,hashlib,importlib.util,json,os,pathlib,shutil,subprocess,tempfile,termios,time
def module(name,file):
    spec=importlib.util.spec_from_file_location(name,pathlib.Path(__file__).with_name(file));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
core=module('int01_core','int01-native-matrix.py')
faults=module('int01_faults','int01-fault-matrix.py')
CASES={
 'copied-custody-positive':(0,'default',None),
 'stdout-overflow':(1,'default',None),
 'stderr-overflow':(2,'default',None),
 'simultaneous-overflow':(3,'default',None),
 'target-status-is-data':(4,'default',None),
 'overflow-with-blocked-observer':(1,'default','b4-overflow-blocked'),
 'recovery-loss-with-blocked-observer':(5,'default','b4-recovery-loss-blocked'),
 'controller-loss-with-blocked-observer':(5,'default','b4-controller-loss-blocked'),
 'full-status-buffer-stop':(5,'status-flood','b4-status-backpressure'),
 'setup-output-overflow':(0,'setup-flood',None),
 'setup-constructor-marker':(0,'setup-marker',None),
 'capture-store-failure':(0,'default','b4-store-failure'),
 'root-exit-descendant-writer':(6,'keep-init',None),
}
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def population(domain):
    return {'AtNs':time.monotonic_ns(),'Inode':domain.stat().st_ino,'Events':(domain/'cgroup.events').read_text(),'Procs':(domain/'cgroup.procs').read_text()}
def run_case(base,case,native,report,baseline=False):
    mode,variant,fault=CASES[case]
    workspace=pathlib.Path(tempfile.mkdtemp(prefix='gagamba-b4-',dir=pathlib.Path(base['SourceRootPath']).parent))
    for n in ('source','staging','journal','evidence'):(workspace/n).mkdir(mode=0o700)
    profile=copy.deepcopy(base)
    for field in ('ToolSourceName','InputSourceName','CoordinatorSourceName','FilterSourceName'):
        source=pathlib.Path(base['SourceRootPath'])/base[field]
        if field=='ToolSourceName':source=native/('target-'+str(mode))
        if field=='CoordinatorSourceName':source=native/('coordinator-'+variant)
        target=workspace/'source'/base[field];shutil.copyfile(source,target);target.chmod(0o600)
    profile.update(SourceRootPath=str(workspace/'source'),StagingParentPath=str(workspace/'staging'),JournalDirectory=str(workspace/'journal'))
    profile.update(ToolSha256=digest(workspace/'source'/base['ToolSourceName']),CoordinatorSha256=digest(workspace/'source'/base['CoordinatorSourceName']))
    profile['IntegratedRecovery']['EvidenceDirectory']=str(workspace/'evidence')
    if fault:profile['QualificationFault']={'Case':'blocked-observer-native-stop' if baseline else fault,'EvidenceDirectory':str(workspace/'evidence')}
    facts={'Case':case,'Workspace':str(workspace),'Status':'Failed','Cleanup':'Unknown','Baseline':baseline,
           'ProfileSha256':hashlib.sha256(json.dumps(profile,sort_keys=True).encode()).hexdigest(),
           'TargetSha256':profile['ToolSha256'],'CoordinatorSha256':profile['CoordinatorSha256']}
    refs={};process=None;domain=None
    try:
        runtime=profile['IntegratedRecovery']['Runtime'];controller=os.environ['GAGAMBA_B2_CONTROLLER_DLL']
        process=subprocess.Popen([runtime['HostPath'],controller,'--int01-controller'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,
            stderr=(workspace/'controller.stderr').open('w'),text=True,bufsize=1,
            env={'PATH':'/usr/bin:/bin','DOTNET_ROOT':str(pathlib.Path(runtime['HostPath']).parent),'DOTNET_EnableDiagnostics':'0','DOTNET_ROLL_FORWARD':'LatestPatch'})
        refs['controller']=os.pidfd_open(process.pid)
        process.stdin.write(json.dumps(profile)+'\n');process.stdin.flush();facts['Startup']=core.read_line(process);assert facts['Startup']=='READY',facts['Startup']
        assert core.command(process,'prepare')=='PREPARING'
        prepared=core.command(process,'prepared');facts['Preparation']=prepared
        if case in ('setup-output-overflow','setup-constructor-marker'):
            assert prepared['Kind']!=1,'setup overflow must prevent publication'
        else:
            assert prepared['Kind']==1,prepared
        held=core.command(process,'inspect');facts['Held']=held
        assert held and not held['ReleaseAuthority']
        for role in ('owner','recovery'):
            try:refs[role]=os.pidfd_open(held[role.title()+'Pid'])
            except ProcessLookupError:
                assert case in ('setup-output-overflow','setup-constructor-marker') and role=='owner'
        end=time.monotonic()+3
        while True:
            read=core.command(process,'journal-status')
            if read['Ok'] and read['Snapshot'] is not None:break
            assert time.monotonic()<end,'acknowledged journal snapshot unavailable'
            time.sleep(.005)
        journal=read['Snapshot'];facts['InitialJournal']=journal
        domain=pathlib.Path(profile['DelegatedParentPath'])/journal['DomainName']
        if case in ('setup-output-overflow','setup-constructor-marker'):
            assert journal['Release'] is None
            facts['Status']='Passed'
        else:
            facts['PopulationBefore']=population(domain);assert 'populated 1' in facts['PopulationBefore']['Events']
            assert core.command(process,'begin-release')=='RELEASING'
            blocked=fault and 'blocked' in fault or case=='full-status-buffer-stop'
            if blocked:
                facts['Reached']=faults.wait_file(workspace/'evidence'/'fault-owner-before-observation.json')
                facts['PopulationAtBarrier']=population(domain)
                assert not core.exited(refs['owner'])
                if case=='full-status-buffer-stop':
                    # Owner samples only its own retained status FD; no /proc access bypass.
                    full=faults.wait_file(workspace/'evidence'/'fault-owner-status-pipe-full.json')
                    facts['StatusPipe']=full
                    assert full['Detail']['Pending']>=full['Detail']['MinimumFullBytes']
                    assert core.command(process,'queue-stop')=='STOP-QUEUED'
                elif case.startswith('recovery-loss'):core.kill_exact(refs['recovery'])
                elif case.startswith('controller-loss'):core.kill_exact(refs['controller'])
                start=time.monotonic_ns();end=time.monotonic()+2
                while time.monotonic()<end:
                    sample=population(domain)
                    if 'populated 0' in sample['Events']:break
                    time.sleep(.002)
                facts['PopulationBeforeOwnerExit']=sample;facts['OwnerAliveAtEmptySample']=not core.exited(refs['owner'])
                facts['StopLatencyUpperBoundNs']=time.monotonic_ns()-start
                if baseline:
                    facts['ExpectedBaselineDefect']=('populated 1' in sample['Events'] and not (workspace/'evidence'/'fault-owner-native-stop.json').exists())
                    assert facts['ExpectedBaselineDefect'],'baseline unexpectedly enforced capture stop'
                    facts['Status']='Reproduced';return True
                assert 'populated 0' in sample['Events'] and facts['OwnerAliveAtEmptySample'],'independent emptiness before owner exit required'
                facts['NativeStop']=faults.wait_file(workspace/'evidence'/'fault-owner-native-stop.json')
                assert facts['NativeStop']['Detail']['KernelAccepted']
                (workspace/'evidence'/'continue-owner-before-observation').touch()
            if not core.exited(refs['controller']):
                result=core.command(process,'release-result');facts['Release']=result
                facts['Observation']=core.command(process,'observe')
                assert facts['Observation']['PhysicalReleaseAttempts']==1
                obs=facts['Observation']['Observation'];capture=obs['Capture']
                facts['Capture']=capture
                if case=='copied-custody-positive':
                    assert obs['Root']['ExitCode']==0 and obs['Outcome']==4
                    for stream in ('stdout','stderr'):
                        read=core.command(process,'capture-'+stream);facts['Copied-'+stream]=read
                        assert read['Kind']==1 and bytes(read['Bytes'])==(stream+'-copy-control\n').encode()
                    assert capture['Stdout']['Complete'] and capture['Stderr']['Complete']
                elif case in ('stdout-overflow','stderr-overflow','simultaneous-overflow','overflow-with-blocked-observer'):
                    assert capture and capture['StoredBytes']<=131072 and obs['Outcome']!=4
                    assert capture['Stdout']['Overflow'] or capture['Stderr']['Overflow']
                    if case=='stdout-overflow':assert capture['Stdout']['StoredBytes']==65536 and capture['Stdout']['ObservedBytes']>65536
                    if case=='stderr-overflow':assert capture['Stderr']['StoredBytes']==65536 and capture['Stderr']['ObservedBytes']>65536
                elif case=='target-status-is-data':
                    assert obs['Root']['ExitCode']==7 and obs['Outcome']!=4,'target output cannot classify trusted status'
                    read=core.command(process,'capture-stdout');facts['Copied-stdout']=read
                    assert b'Confirmed' in bytes(read['Bytes'])
                elif case=='capture-store-failure':
                    assert capture and (capture['Stdout']['ReadFailure'] or capture['Stderr']['ReadFailure']) and obs['Outcome']!=4
                    native_stop=faults.wait_file(workspace/'evidence'/'fault-owner-native-stop.json');facts['NativeStop']=native_stop
                    assert native_stop['Detail']['Cause']=='OutputReadFailure' and native_stop['Detail']['KernelAccepted']
                elif case=='root-exit-descendant-writer':
                    assert obs['Root']['ExitCode']==0 and capture['Stdout']['Complete']
                    read=core.command(process,'capture-stdout');facts['Copied-stdout']=read
                    assert b'root-exiting' in bytes(read['Bytes']) and b'descendant-writer' in bytes(read['Bytes'])
            facts['Status']='Passed'
        if not core.exited(refs['controller']):facts['Disposal']=core.command(process,'dispose')
        final=workspace/'evidence'/'integrated-final.json';end=time.monotonic()+8
        while time.monotonic()<end and not final.exists() and not core.exited(refs['recovery']):time.sleep(.01)
        if final.exists():
            facts['RecoveryFinal']=json.loads(final.read_text())
            facts['CleanupEvidence']=core.cleanup_classification.classify_final_record(
                facts['RecoveryFinal'],core.cleanup_classification.durable_verdict(workspace/'evidence'))
            facts['Cleanup']=facts['CleanupEvidence']['State']
        if case.startswith('recovery-loss'):assert facts['Cleanup']=='Unknown','loss of reaper cannot be promoted'
        if domain.exists():facts['PopulationBeforeWatchdog']=population(domain)
    except Exception as error:facts['Status']='Failed';facts['Error']=repr(error)
    finally:
        facts['WatchdogActions']=[]
        for role in ('controller','owner','recovery'):
            if role in refs and not core.exited(refs[role]):
                try:core.kill_exact(refs[role]);facts['WatchdogActions'].append(role+':SIGKILL')
                except ProcessLookupError:pass
        if process:
            try:process.wait(timeout=5)
            except subprocess.TimeoutExpired:facts['WatchdogWait']='Unknown'
        for fd in refs.values():os.close(fd)
        facts['FaultEvidence']={p.name:json.loads(p.read_text()) for p in (workspace/'evidence').glob('fault-*.json')}
        core.emit(report/(case+'.json'),facts)
    return facts['Status']=='Passed'
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--profile',default=os.environ.get('GAGAMBA_INT01_PROFILE'))
    parser.add_argument('--native',type=pathlib.Path,required=True);parser.add_argument('--report',type=pathlib.Path,required=True)
    parser.add_argument('--case',choices=tuple(CASES));parser.add_argument('--baseline',action='store_true');args=parser.parse_args()
    args.report.mkdir(parents=True,exist_ok=True);checks=core.prerequisites(args.profile);core.emit(args.report/'preflight.json',{'Checks':checks})
    if not all(c['Passed'] for c in checks):return 2
    cases=(args.case,) if args.case else tuple(CASES);base=json.loads(pathlib.Path(args.profile).read_text())
    passed=sum(run_case(base,c,args.native,args.report,args.baseline) for c in cases)
    core.emit(args.report/'results.json',{'Passed':passed,'Failed':len(cases)-passed,'Skipped':0,'Cases':list(cases)})
    print(f'{passed}/{len(cases)} B4 cases passed',flush=True);return int(passed!=len(cases))
if __name__=='__main__':raise SystemExit(main())
