#!/usr/bin/env python3
"""Nonprivileged PN protocol topology qualification, never installation trust.

Run explicit isolated Release B2Controller fixture. This supervisor becomes a
subreaper for its own disposable descendants only. No cgroups or install state.
"""
import argparse, ctypes, hashlib, json, os, pathlib, signal, subprocess, time, unittest, uuid

p=argparse.ArgumentParser()
p.add_argument('--fixture',type=pathlib.Path,required=True)
p.add_argument('--artifacts',type=pathlib.Path,required=True)
p.add_argument('--mode',action='append')
p.add_argument('--production-owner',type=pathlib.Path)
options=p.parse_args()
assert os.getuid()!=0 and os.getgid()!=0, 'run as a normal Linux user'
assert options.fixture.is_file()
options.artifacts.mkdir(parents=True,exist_ok=True)
libc=ctypes.CDLL(None,use_errno=True)
assert libc.prctl(36,1,0,0,0)==0, 'fixture supervisor subreaper unavailable'
RESULTS=[]
MODES=['valid','sibling-connector','recovery-connector','recovery-writer','wrong11','wrong38','missing11',
       'wrong-digest','wrong-closure','wrong-execution','wrong-lease','prior-session','duplicate36','duplicate11','duplicate12',
       'nested36','missing24','drop10','drop36','drop12','dropReady','late12','no-half-close','prematureReady',
       'owner-before36','owner-queued36','owner-after12','recovery-before38','recovery-after38','recovery-after11',
       'controller-hash-death','controller-after36-death','controller-after12-death','lifeline-loss','stop-before38','stop-stalled','recovery-stalled-death']
MODES += ['controller-recovery-worker-death','controller-stalled-owner-death','controller-stalled-recovery-death','wrong12','replay12',
          'echo-on-lifeline','release-from-recovery','release-on-lifeline','release-to-recovery','withhold26','signals-refusal','late36']
MODES += ['integrated-valid','integrated-wrong-recovery','integrated-wrong-closure','integrated-wrong-digest','integrated-first-stage',
          'integrated-duplicate36','integrated-b2-11','integrated-b2-12','integrated-b2-37','integrated-b2-38',
          'integrated-wrong-session','integrated-stop-stalled','integrated-controller-death','integrated-owner-death','integrated-recovery-death']
if options.production_owner:MODES.append('production-owner-refusal')

def events(directory,role):
    f=directory/(role+'.jsonl')
    return [json.loads(line) for line in f.read_text().splitlines()] if f.exists() else []

def result(directory,role):
    f=directory/(role+'-result.json')
    return json.loads(f.read_text()) if f.exists() else None

def observe_known(directory):
    observed={}
    for role in ['controller','recovery','owner','decoy']:
        es=events(directory,role)
        if es: observed[role]=es[0]['Pid']
    for e in events(directory,'recovery'):
        if e['Event']=='spawn-owner' and 'owner' not in observed:observed['owner']=e['Facts']['OwnerPid']
    return observed

class Topology(unittest.TestCase):
    def exercise(self,mode):
        directory=options.artifacts/(mode+'-'+uuid.uuid4().hex)
        directory.mkdir()
        if mode=='production-owner-refusal':
            self.assertTrue(options.production_owner.is_file())
            (directory/'production-owner-path.txt').write_text(str(options.production_owner.resolve()))
        argv=['dotnet',str(options.fixture.resolve()),'--b6-topology','controller',mode,str(directory.resolve())]
        (directory/'command.json').write_text(json.dumps(argv,indent=2)+'\n')
        started=time.monotonic_ns();known={};cleanup={}
        with (directory/'outer.log').open('w') as log:
            child=subprocess.Popen(argv,stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT)
            try:
                child.wait(timeout=25)
                known=observe_known(directory)
                # Controller is reaped by Popen. After a deliberate C/R death,
                # only this supervisor's known fixture descendants are adopted.
                end=time.monotonic()+10
                for role,pid in known.items():
                    if role=='controller': cleanup[role]='reaped by Popen';continue
                    while True:
                        try:
                            got,status=os.waitpid(pid,os.WNOHANG)
                            if got==pid: cleanup[role]={'adoptedReaped':True,'status':status};break
                            if time.monotonic()>end: self.fail('known descendant did not terminate: '+role)
                        except ChildProcessError:
                            # Original R/C owns its child and recorded native waitpid.
                            self.assertFalse(pathlib.Path('/proc',str(pid)).exists(),f'{role} still present without reaping proof')
                            cleanup[role]='already reaped by original parent';break
                        time.sleep(.005)
            finally:
                if child.poll() is None: child.kill();child.wait(timeout=5)
                for role,pid in observe_known(directory).items():
                    # Cleanup never authenticates an actor. Confirm the fresh test
                    # directory in its command line before signalling a live target.
                    proc=pathlib.Path('/proc',str(pid),'cmdline')
                    if proc.exists():
                        try:
                            if str(directory.resolve()).encode() in proc.read_bytes():
                                fd=os.pidfd_open(pid);signal.pidfd_send_signal(fd,signal.SIGKILL);os.close(fd)
                        except (ProcessLookupError,FileNotFoundError): pass
                    try: os.waitpid(pid,0)
                    except ChildProcessError: pass
        c,r,o=[result(directory,role) for role in ['controller','recovery','owner']]
        ec,er,eo=[events(directory,role) for role in ['controller','recovery','owner']]
        cn,rn,on=[[e['Event'] for e in es] for es in [ec,er,eo]]
        intentional_controller_death=mode in ['controller-hash-death','controller-after36-death','controller-after12-death','controller-recovery-worker-death','integrated-controller-death']
        if intentional_controller_death:
            self.assertEqual(-signal.SIGKILL,child.returncode)
            self.assertIsNone(c)
            self.assertNotIn('PN-ready',cn)
        else:
            self.assertEqual(0,child.returncode,(directory/'outer.log').read_text())
            self.assertIsNotNone(c,json.dumps(ec))
            self.assertTrue(c['RecoveryReaped'])
            self.assertEqual(mode in ['valid','withhold26','integrated-valid'],c['Accepted'],json.dumps(c))
            self.assertFalse(c['ReleaseAuthority']);self.assertEqual(0,c['PhysicalReleaseAttempts'])
            if 'NativeHandlesBefore' in c:self.assertEqual(c['NativeHandlesBefore'],c['NativeHandlesAfter'],json.dumps(c))
        if r:
            self.assertEqual(1,r['StopCount'],json.dumps(r))
            self.assertFalse(r['ReleaseAuthority']);self.assertEqual('Unproven',r['NativeStop'])
            self.assertEqual(0,r['RetainedDomains'])
            if r['OwnerPid'] is not None:self.assertTrue(r['OwnerReaped'],json.dumps(r))
        if mode.startswith('integrated-'):
            if c:
                self.assertFalse(c['Held']);self.assertEqual(0,c['B2EvidenceSockets'])
                if mode in ['integrated-wrong-recovery','integrated-wrong-closure','integrated-wrong-digest','integrated-first-stage','integrated-wrong-session','integrated-b2-11','integrated-b2-12','integrated-b2-37','integrated-b2-38']:
                    self.assertFalse(c['EchoAccepted']);self.assertFalse(c['DescriptionDecoded'])
            for es in [ec,er,eo]:
                self.assertFalse(any(e['Event'] in ['send24','accept24','send38','accept38','accept11','send37','accept37'] for e in es))
                if mode!='integrated-b2-11':self.assertFalse(any(e['Event']=='send11' for e in es))
            if mode=='integrated-valid':
                for seq,names in [(cn,['send1','accept9','send10','accept8','GID1-established','accept36','description-decoded']),
                                  (rn,['accept1','send9','accept10','spawn-owner','send32','send8','stop','owner-reaped']),
                                  (on,['GID1-established','send36','send-description','stopped'])]:
                    positions=[seq.index(n) for n in names];self.assertEqual(sorted(positions),positions)
            if mode=='integrated-wrong-recovery':self.assertNotIn('spawn-owner',rn)
            if mode=='integrated-stop-stalled':self.assertIn('worker-blocked',on);self.assertIn('stopped',on)
        elif mode=='valid':
            self.assertTrue(r['Confirmed'] and r['ReadySent'] and r['OwnerAcknowledged'])
            self.assertTrue(o['EchoSent'] and o['Stopped'] and o['AcknowledgementSent'])
            for seq,names in [(cn,['send1','accept9','send10','accept11','peer-owner-verified','send37','record-owner-verified','send12','accept2','PN-ready']),
                              (rn,['accept1','send9','accept10','spawn-owner','send23','accept24','send38','send11','accept12','send2','stop','accept26','owner-reaped']),
                              (on,['accept23','send24','accept38','peer-controller-verified','accept37','send36','accept25','stopped'])]:
                positions=[seq.index(n) for n in names];self.assertEqual(sorted(positions),positions)
                for name in names:self.assertEqual(1,seq.count(name),name)
            self.assertTrue(next(e for e in ec if e['Event']=='record-owner-verified')['Facts']['KernelCredentials'])
            self.assertTrue(next(e for e in eo if e['Event']=='accept37')['Facts']['KernelCredentials'])
        elif mode not in ['dropReady','owner-after12','controller-after12-death','withhold26']:
            if r:self.assertFalse(r['ReadySent'],json.dumps(r))
        if mode=='drop10':self.assertNotIn('spawn-owner',rn);self.assertNotIn('send24',on)
        if mode=='controller-recovery-worker-death':self.assertIn('worker-blocked',rn);self.assertNotIn('spawn-owner',rn)
        if mode in ['controller-stalled-owner-death','controller-stalled-recovery-death']:self.assertIn('worker-blocked',cn);self.assertNotIn('send12',cn)
        if mode=='withhold26':
            self.assertTrue(r['Confirmed'] and r['ReadySent'] and r['OwnerReaped']);self.assertFalse(r['OwnerAcknowledged']);self.assertEqual('Unproven',r['NativeStop'])
        if mode=='signals-refusal':
            self.assertGreater(c['SignalsObserved'],0)
            self.assertLess(c['RefusedAtNs']-c['DeadlineNs'],500_000_000,'signals renewed the original deadline')
        if mode=='late36':
            late=next(e for e in eo if e['Event']=='late36-attempt');self.assertGreaterEqual(late['AtNs'],c['DeadlineNs'])
            self.assertNotIn('PN-ready',cn)
        if mode in ['missing24','stop-before38','recovery-before38']:self.assertNotIn('send38',rn);self.assertNotIn('send11',rn)
        if mode=='drop12':self.assertFalse(r['Confirmed']);self.assertFalse(r['ReadySent'])
        if mode=='dropReady':self.assertTrue(r['Confirmed']);self.assertFalse(c['ReadyReceived']);self.assertFalse(c['Accepted'])
        if mode=='recovery-writer':self.assertIn('recovery-wrote36',rn);self.assertIn('actual evidence sender refused',c['Refusal'])
        if mode=='production-owner-refusal':
            self.assertIn('accept24',rn);self.assertIn('send38',rn);self.assertIn('send11',rn)
            self.assertNotIn('record-owner-verified',cn);self.assertNotIn('send12',cn)
            self.assertTrue(r['OwnerReaped']);self.assertEqual(0,r['OwnerExitCode'])
        if mode in ['controller-hash-death','stop-stalled','recovery-stalled-death']:
            self.assertIn('worker-blocked',on);self.assertIn('stopped',on)
            stopped=next(e['AtNs'] for e in eo if e['Event']=='stopped')
            triggered=(next(e['AtNs'] for e in er if e['Event']=='stop') if r else next(e['AtNs'] for e in ec if e['Event']=='send37'))
            self.assertLess(stopped-triggered,2_000_000_000,'worker delayed independent stop')
        for value in [c,r,o]:
            if value:self.assertFalse(value['ReleaseAuthority'])
        record={'mode':mode,'scope':'PN actual native C-R-O mechanism, synthetic identity, no installation qualification',
                'directory':str(directory),'command':argv,'elapsedNs':time.monotonic_ns()-started,'pids':known,'cleanup':cleanup,
                'controller':c,'recovery':r,'owner':o,'passed':True}
        RESULTS.append(record)

for mode in options.mode or MODES:
    assert mode in MODES,mode
    setattr(Topology,'test_'+mode.replace('-','_'),lambda self,mode=mode:self.exercise(mode))
suite=unittest.defaultTestLoader.loadTestsFromTestCase(Topology)
run=unittest.TextTestRunner(verbosity=2).run(suite)
metadata={'fixture':str(options.fixture.resolve()),'fixtureSha256':hashlib.sha256(options.fixture.read_bytes()).hexdigest(),
          'uid':os.getuid(),'gid':os.getgid(),'kernel':os.uname().release,'root':{'uid':os.stat('/').st_uid,'gid':os.stat('/').st_gid,'mode':oct(os.stat('/').st_mode&0o7777)},
          'commands':len(RESULTS),'run':run.testsRun,'failures':len(run.failures),'errors':len(run.errors),'skipped':len(run.skipped),'results':RESULTS}
(options.artifacts/'results.json').write_text(json.dumps(metadata,indent=2)+'\n')
raise SystemExit(0 if run.wasSuccessful() else 1)
