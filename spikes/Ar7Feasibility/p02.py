"""Disposable P02 only. Root provisions/observes; manager and targets are UID 65534.
No production code. Every attempt is bounded by an independent outer cgroup and
30-second root observer. Never remove target-provided paths.
"""
import argparse, ctypes, hashlib, json, os, pathlib, resource, select, signal
import socket, struct, subprocess, sys, time, uuid, threading

UID = 65534
PIN = '523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6'

def sha(path): return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()
def read(path):
    try: return pathlib.Path(path).read_text().strip()
    except OSError as e: return 'UNKNOWN: ' + str(e)
def write(path, value): pathlib.Path(path).write_text(str(value))
def run(cmd, **kw):
    p = subprocess.run(cmd, capture_output=True, text=True, timeout=15, **kw)
    return dict(argv=cmd, rc=p.returncode, stdout=p.stdout[:32768], stderr=p.stderr[:8192])
def leaf_own(path):
    os.chown(path, UID, UID)
    for p in path.iterdir():
        if p.is_file(): os.chown(p, UID, UID)
def bounds():
    resource.setrlimit(resource.RLIMIT_FSIZE, (1048576,1048576))
    resource.setrlimit(resource.RLIMIT_NOFILE,(64,64))
    resource.setrlimit(resource.RLIMIT_CORE,(0,0))
    resource.setrlimit(resource.RLIMIT_CPU,(12,12))
def filter_bytes():
    # x86_64 only; reject compatibility/x32. EPERM for authority syscalls.
    # clone3 ENOSYS enables glibc fork/thread fallback; legacy clone rejects all NEW*.
    ins=[]
    def emit(code,jt,jf,k):ins.append(struct.pack('HBBI',code,jt,jf,k))
    emit(0x20,0,0,4);emit(0x15,1,0,0xc000003e);emit(0x06,0,0,0x80000000)
    emit(0x20,0,0,0);emit(0x35,0,1,0x40000000);emit(0x06,0,0,0x00050001)
    for nr in (41,53,101,155,161,165,166,272,308,310,311,424,434,438,428,429,430,431,432,442):
        emit(0x15,0,1,nr);emit(0x06,0,0,0x00050001)
    emit(0x15,0,1,435);emit(0x06,0,0,0x00050026)
    emit(0x15,0,3,56);emit(0x20,0,0,16);emit(0x45,0,1,0x7e020080);emit(0x06,0,0,0x00050001)
    emit(0x06,0,0,0x7fff0000)
    return b''.join(ins)

def manager(a):
    assert os.getuid()==UID and os.getgid()==UID
    # Disposable trusted manager permits ptrace-style controls from its same-UID
    # fixture siblings. Never changes Yama globally or another process's policy.
    ctypes.CDLL(None).prctl(0x59616d61,ctypes.c_ulong(-1),0,0,0)
    cg=pathlib.Path(a.cg); root=pathlib.Path(a.root)
    out={'uid':os.getuid(),'gid':os.getgid(),'status':read('/proc/self/status'),'cgroup':read('/proc/self/cgroup')}
    listeners=[]; control=None
    try:
        # Same outer safety domain, no namespace/seccomp restrictions on controls.
        (cg/'sibling').mkdir();out['sibling_create']=True
        write(cg/'sibling/memory.max',67108864);write(cg/'sibling/pids.max',16)
        out['manager_limits']={n:read(cg/'sibling'/n) for n in ('memory.max','pids.max','cgroup.procs')}
        control=subprocess.Popen([str(root/'worker'),'hold'],stdout=subprocess.PIPE,text=True,close_fds=True,preexec_fn=bounds)
        address=control.stdout.readline().strip()
        write(cg/'sibling/cgroup.procs',control.pid)
        out['control_pid']=control.pid
        out['control_membership']=read(f'/proc/{control.pid}/cgroup')
        socket_path=str(root/'daemon.sock'); abstract='ar7-P02-'+a.nonce
        for address in (socket_path,'\0'+abstract):
            s=socket.socket(socket.AF_UNIX);s.bind(address);s.listen(4);s.setblocking(False);listeners.append(s)
        # Keep one known control FD open; child execs close everything except seccomp.
        authority=os.open(cg/'cgroup.procs',os.O_WRONLY|os.O_CLOEXEC)
        args=[str(cg),str(os.getpid()),str(control.pid),socket_path,abstract,a.nonce,str(root/'input'),address,str(authority)]
        time.sleep(.1)
        out['positive']=run([str(root/'worker'),'positive',*args],preexec_fn=bounds)
        out['endpoint_requests_positive']=drain(listeners)
        out['controls_after_positive']={n:read(cg/n) for n in ('memory.max','pids.max','cgroup.subtree_control')}
        positive=out['positive']
        out['positive_gate']=(positive['rc']==0 and 'ptrace_control rc=0' in positive['stdout']
            and 'process_vm_readv rc=1' in positive['stdout']
            and 'mkdir_cgroup rc=0' in positive['stdout']
            and out['controls_after_positive']['memory.max']=='134217728'
            and out['controls_after_positive']['pids.max']=='48'
            and {r['endpoint'] for r in out['endpoint_requests_positive']}=={0,1}
            and all(r['request']==a.nonce for r in out['endpoint_requests_positive']))
        if not out['positive_gate']:raise RuntimeError('positive gate failed: INCONCLUSIVE, no target release')
        # Useful rootless helper startup is a prerequisite, not a denial result.
        common=['/usr/bin/bwrap','--unshare-user','--unshare-pid','--unshare-net','--unshare-ipc','--unshare-uts',
                '--uid','0','--gid','0','--disable-userns','--assert-userns-disabled','--cap-drop','ALL',
                '--die-with-parent','--new-session','--clearenv','--ro-bind',str(root/'worker'),'/target',
                '--ro-bind',str(root/'input'),'/input','--proc','/proc','--dev','/dev',
                '--size','8388608','--tmpfs','/tmp','--dir','/sys','--dir','/run','--chdir','/tmp']
        f=os.memfd_create('ar7-P02-seccomp',os.MFD_CLOEXEC);os.write(f,filter_bytes());os.lseek(f,0,0)
        out['filter_sha256']=hashlib.sha256(filter_bytes()).hexdigest()
        def enter_target():
            write(cg/'target/cgroup.procs',os.getpid());bounds()
        out['candidate']=run(common+['--seccomp',str(f),'--','/target','candidate',*args],pass_fds=(f,),preexec_fn=enter_target)
        os.close(f);os.close(authority)
        out['endpoint_requests_candidate']=drain(listeners)
        out['controls_after_candidate']={str(p.relative_to(cg)):read(p) for p in
            (cg/'memory.max',cg/'pids.max',cg/'cgroup.subtree_control',cg/'sibling/memory.max',cg/'sibling/pids.max')}
        out['target_domain_after']=read(cg/'target/cgroup.events')
    except Exception as e: out['error']=repr(e)
    finally:
        if control:
            control.terminate()
            try:control.wait(timeout=2)
            except subprocess.TimeoutExpired:control.kill();control.wait()
        for s in listeners:s.close()
        out['manager_final_cgroup']=read('/proc/self/cgroup')
    print(json.dumps(out),flush=True)

def drain(listeners):
    out=[]
    for i,s in enumerate(listeners):
        while True:
            try:c,_=s.accept()
            except BlockingIOError:break
            with c:
                c.settimeout(.5)
                out.append({'endpoint':i,'peer':struct.unpack('3i',c.getsockopt(socket.SOL_SOCKET,socket.SO_PEERCRED,12)),
                            'request':c.recv(256).decode(errors='replace'),'time':time.time()})
    return out

def observer(a):
    assert os.getuid()==0 # provisioning/observer only; all probe execution drops UID
    nonce=uuid.uuid4().hex; root=pathlib.Path('/tmp')/('gagamba-ar7-P02-'+nonce)
    outer=pathlib.Path('/sys/fs/cgroup')/('gagamba-ar7-P02-'+nonce);cg=outer/'delegated'
    data={'probe':'P02','source_sha':a.source_sha,'nonce':nonce,'date_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),
          'host':{'kernel':os.uname().release,'arch':os.uname().machine,'distribution':read('/etc/os-release'),
          'glibc':os.confstr('CS_GNU_LIBC_VERSION'),'lsm':read('/sys/kernel/security/lsm'),
          'yama':read('/proc/sys/kernel/yama/ptrace_scope'),'userns_max':read('/proc/sys/user/max_user_namespaces'),
          'cgroup_mount':read('/proc/mounts'),'helper':run(['/usr/bin/bwrap','--version']),
          'helper_sha256':sha('/usr/bin/bwrap'),'helper_mode':oct(os.stat('/usr/bin/bwrap').st_mode&0o7777)},
          'paths':{'root':str(root),'outer':str(outer),'delegated':str(cg)},'observer_uid':os.getuid(),
          'source_hashes':{p.name:sha(p) for p in pathlib.Path(__file__).parent.glob('p02.*')}}
    p=None;clean=False; done=threading.Event(); watcher=None
    try:
        if data['host']['helper_sha256']!=PIN or os.stat('/usr/bin/bwrap').st_mode&0o6000:raise RuntimeError('helper pin/nonsetuid prerequisite changed')
        root.mkdir(mode=0o755)
        data['compile']=run(['/usr/bin/gcc','-static','-O2','-Wall','-Wextra','-o',str(root/'worker'),str(pathlib.Path(__file__).with_suffix('.c'))])
        if data['compile']['rc']:raise RuntimeError('compile failed before release')
        data['worker_sha256']=sha(root/'worker')
        (root/'input').write_text(nonce);os.chmod(root/'input',0o444)
        # Root-only outer ceiling stays protected even if every candidate mutation succeeds.
        outer.mkdir();write(outer/'memory.max',268435456);write(outer/'pids.max',64)
        write(outer/'memory.swap.max',0);write(outer/'cgroup.subtree_control','+memory +pids')
        cg.mkdir();write(cg/'memory.max',134217728);write(cg/'pids.max',48)
        write(cg/'cgroup.subtree_control','+memory +pids');leaf_own(cg)
        for name in ('manager','target'):(cg/name).mkdir();leaf_own(cg/name)
        os.chown(root,UID,UID) # exactly this disposable fixture, required for the owned socket
        def drop():
            write(cg/'manager/cgroup.procs',os.getpid());os.setgroups([]);os.setgid(UID);os.setuid(UID);bounds()
        cmd=[sys.executable,__file__,'--manager','--root',str(root),'--cg',str(cg),'--nonce',nonce]
        p=subprocess.Popen(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,preexec_fn=drop,close_fds=True)
        data['manager_pid']=p.pid
        data['external_process_samples']={}
        def observe():
            while not done.wait(.05):
                for name in ('manager','target','sibling'):
                    for pid in read(cg/name/'cgroup.procs').split():
                        if not pid.isdecimal() or pid in data['external_process_samples']:continue
                        membership=read('/proc/'+pid+'/cgroup')
                        if str(outer).removeprefix('/sys/fs/cgroup') not in membership:continue
                        data['external_process_samples'][pid]={'cgroup':membership,'status':read('/proc/'+pid+'/status'),
                            'command':read('/proc/'+pid+'/cmdline')}
        watcher=threading.Thread(target=observe,daemon=True);watcher.start()
        try:
            stdout,stderr=p.communicate(timeout=30);data['manager_rc']=p.returncode
            data['manager_stderr']=stderr[:8192].decode(errors='replace')
            data['manager']=json.loads(stdout)
        except subprocess.TimeoutExpired:
            data['watchdog_fired']=True;write(outer/'cgroup.kill',1);stdout,stderr=p.communicate(timeout=3)
            data['partial_stdout']=stdout[:32768].decode(errors='replace')
        data['observer_domains']={str(q.relative_to(outer)):read(q/'cgroup.procs') for q in [outer,cg,*cg.iterdir()] if q.is_dir()}
    except Exception as e:data['setup_error']=repr(e)
    finally:
        done.set()
        if watcher:watcher.join(timeout=1)
        # External oracle kills/reaps before deleting. Paths are generated by this observer.
        if outer.exists():
            try:
                write(outer/'cgroup.kill',1)
                if p:p.wait(timeout=3)
                for _ in range(30):
                    if 'populated 0' in read(outer/'cgroup.events'):break
                    time.sleep(.1)
                data['cleanup_events']=read(outer/'cgroup.events')
                if 'populated 0' not in data['cleanup_events']:raise RuntimeError('outer not empty')
                known={'manager','target','sibling','new-positive','new-candidate'}
                unknown=[q.name for q in cg.iterdir() if q.is_dir() and q.name not in known] if cg.exists() else []
                if unknown:raise RuntimeError('unknown cgroup descendants preserved: '+repr(unknown))
                for name in sorted(known):
                    q=cg/name
                    if q.exists():q.rmdir()
                if cg.exists():cg.rmdir()
                outer.rmdir()
            except Exception as e:data['cleanup_error']=repr(e)
        if not outer.exists():
            # Limited explicit fixture leaves; never recursively follow target-created trees.
            if root.exists():
                for name in ('daemon.sock','input','worker'):
                    q=root/name
                    if q.exists() or q.is_symlink():q.unlink()
                try:root.rmdir()
                except OSError as e:data['cleanup_error']=repr(e)
        data['cleanup_absence']={'outer':not outer.exists(),'workspace':not root.exists(),
                                'manager':p is None or p.poll() is not None}
        clean=all(data['cleanup_absence'].values()) and not data.get('cleanup_error')
        data['cleanup_confirmed']=clean
        dest=pathlib.Path(a.output);dest.mkdir(parents=True,exist_ok=True)
        (dest/(nonce+'.json')).write_text(json.dumps(data,indent=2))
        print(json.dumps(data,indent=2))
    return 0 if clean else 2

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--manager',action='store_true');ap.add_argument('--source-sha');ap.add_argument('--output');ap.add_argument('--root');ap.add_argument('--cg');ap.add_argument('--nonce');args=ap.parse_args()
    if args.manager:manager(args)
    else:sys.exit(observer(args))
