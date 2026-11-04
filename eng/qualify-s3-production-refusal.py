#!/usr/bin/env python3
"""External driver of actual Release Recovery/Owner; v1 control vs fixed-root v2 refusal.
No S2 bypass, permission change, installed generation or qualification publication.
"""
import argparse,base64,ctypes,hashlib,json,os,pathlib,select,shutil,signal,socket,struct,subprocess,time,uuid
p=argparse.ArgumentParser();p.add_argument('--recovery',type=pathlib.Path,required=True);p.add_argument('--owner',type=pathlib.Path,required=True);p.add_argument('--artifacts',type=pathlib.Path,required=True);o=p.parse_args()
assert os.getuid()!=0 and os.getgid()!=0;assert ctypes.CDLL(None).prctl(36,1,0,0,0)==0
o.artifacts.mkdir(parents=True,exist_ok=True)
root=pathlib.Path('/tmp/gagamba-s3-production-refusal-'+uuid.uuid4().hex);root.mkdir(mode=0o700)
pins={}
for role,entry in [('recovery',o.recovery),('owner',o.owner)]:
    dest=root/role;dest.mkdir(mode=0o700);names=[entry.name,entry.stem+'.deps.json',entry.stem+'.runtimeconfig.json','Gagamba.Execution.Linux.M1.dll','Gagamba.Execution.dll']
    hashes={}
    for name in names:
        data=(entry.parent/name).read_bytes();(dest/name).write_bytes(data);(dest/name).chmod(0o600);hashes[name]=hashlib.sha256(data).hexdigest()
    pins[role]={'Directory':str(dest),'Sha256':hashes}
identity=dict(zip(['ArtifactSetSha256','OwnerClosureSha256','RecoveryClosureSha256','CoordinatorSha256','SeccompFilterSha256','RuntimeInventorySha256'],[str(i)*64 for i in range(1,7)]))
identity.update(ManifestSchema=1,OwnerProtocol=1,CoordinatorProtocol=1,Bootstrap=1,ProfileId='ae4-linux-fixed-fixture-v1',ProfileVersion='1',Generation='1'*64,Qualified=True)
results=[]
for version in [1,2]:
    evidence=o.artifacts/('v'+str(version));evidence.mkdir(mode=0o700)
    c,child_c=socket.socketpair(socket.AF_UNIX,socket.SOCK_SEQPACKET|socket.SOCK_CLOEXEC)
    cred,child_cred=socket.socketpair(socket.AF_UNIX,socket.SOCK_SEQPACKET|socket.SOCK_CLOEXEC)
    execution,lease,session=[uuid.uuid4() for _ in range(3)];secret=os.urandom(32)
    config=dict(Version=version,Execution=str(execution),Generation=str(lease),Session=str(session),ControllerPid=os.getpid(),ControllerUid=os.getuid(),
                Secret=base64.b64encode(secret).decode(),Recovery=pins['recovery'],Owner=pins['owner'],EvidenceDirectory=str(evidence.resolve()))
    if version==2:config.update(B2InstallationIdentity=identity,B2DeadlineNs=time.monotonic_ns()+7_000_000_000)
    env=os.environ.copy();env.update(GAGAMBA_B2_CONTROL_FD=str(child_c.fileno()),GAGAMBA_B2_CREDENTIAL_FD=str(child_cred.fileno()))
    def frame(kind,sequence,payload=b''):return struct.pack('<IHBB',0x4d314f70,1,kind,0)+execution.bytes_le+lease.bytes_le+struct.pack('<II',sequence,len(payload))+payload
    argv=['/usr/lib/dotnet/dotnet',str(root/'recovery'/o.recovery.name)]
    observed=[];owner_pin=None;ready=None;output=b''
    with (evidence/'process.log').open('wb') as log:
        proc=subprocess.Popen(argv,env=env,pass_fds=(child_c.fileno(),child_cred.fileno()),stdin=subprocess.DEVNULL,stdout=log,stderr=subprocess.STDOUT)
        pin=os.pidfd_open(proc.pid);child_c.close();child_cred.close()
        try:
            cred.send(json.dumps(config,separators=(',',':')).encode());cred.close();c.send(frame(1,1,session.bytes_le+secret));secret=b'';del config['Secret']
            c.settimeout(10)
            output=c.recv(8192)
            if output:
                assert output[:8]==struct.pack('<IHBB',0x4d314f70,1,2,0),(version,output[:8].hex())
                assert version==1,'unqualified v2 must not emit Ready/9'
                ready=json.loads(output[48:]);assert ready['StopCount']==0 and not ready['ReleaseAuthority']
                assert ready['ControllerPid']==os.getpid() and ready['RecoveryPid']==proc.pid
                owner_pin=os.pidfd_open(ready['OwnerPid']);observed.append(dict(role='owner',pid=ready['OwnerPid'],pinLive=not bool(select.select([owner_pin],[],[],0)[0])))
                c.send(frame(3,2))
                stop_path=evidence/'owner-exit.json';end=time.monotonic()+5
                while not stop_path.exists() and time.monotonic()<end:time.sleep(.01)
                assert select.select([owner_pin],[],[],0)[0],'actual owner exit required'
                # Original R performed waitpid. No numeric-PID disappearance inference.
                assert stop_path.exists()
                stop=json.loads(stop_path.read_text());assert stop['StopCount']==1 and stop['OwnerExited'] and not stop['ReleaseAuthority']
                c.send(frame(4,3));snapshot=c.recv(8192);assert snapshot[6]==5
                retained=json.loads(snapshot[48:]);assert retained['StopCount']==1 and retained['StopDeadline']==stop['StopDeadline']
                # Legacy R deliberately retains its original 30-second bootstrap
                # lifetime even after O exits. Do not mistake retention for a hang.
                proc.wait(timeout=35);assert proc.returncode==0
            else:
                assert version==2;proc.wait(timeout=10);assert proc.returncode==3
                assert select.select([pin],[],[],0)[0]
                assert not (evidence/'ready.json').exists()
        finally:
            c.close();cred.close()
            if proc.poll() is None:signal.pidfd_send_signal(pin,signal.SIGKILL);proc.wait(timeout=5)
            if owner_pin is not None:
                if not select.select([owner_pin],[],[],0)[0]:signal.pidfd_send_signal(owner_pin,signal.SIGKILL)
                os.close(owner_pin)
            os.close(pin)
    results.append(dict(version=version,scope='actual Release helpers; legacy positive or qualified-path refusal, never trusted v2 positive',command=argv,configurationWithoutSecret=config,
                        recoveryPid=proc.pid,exitCode=proc.returncode,receivedType=output[6] if output else None,ready=ready,processObservations=observed,passed=True))
    print('actual Release v'+str(version), 'legacy Ready/Stop/reap control PASS' if version==1 else 'fixed-root qualified refusal PASS',flush=True)
(o.artifacts/'results.json').write_text(json.dumps(dict(passed=2,failed=0,skipped=0,uid=os.getuid(),kernel=os.uname().release,privateCopy=str(root),pins=pins,results=results,
    qualifications='No trusted-root positive or full loader claim; test copies retained, actual children reaped'),indent=2)+'\n')
