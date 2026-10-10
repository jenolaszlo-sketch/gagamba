"""Disposable P04 only. Root provisions, runs all positive controls, and
observes; manager (UID 65534) runs restricted candidates only. Only root can
place processes across delegation scopes (measured kernel rule), so positives
are root-spawned into the outer-bounded leaf with a private identically-sized
/tmp; candidates self-place inside the delegated subtree as in P02/P03.
Candidate composition is otherwise byte-identical to frozen P02/P03. No
production code. Every case is bounded by the outer cgroup and the observer
watchdog. Never remove target-provided paths.
"""
import argparse
import ctypes
import hashlib
import json
import os
import pathlib
import re
import resource
import struct
import subprocess
import sys
import threading
import time
import uuid

UID = 65534
PIN = '523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6'
CLONE_NEWNS = 0x00020000
MS_PRIVATE = 1 << 18
MS_REC = 1 << 14
MS_BIND = 4096

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
def bounds(fsize=True):
    # RLIMIT_FSIZE is harness-side safety only. Measured 2026-10-10: a 1 MiB
    # file-size limit breaks CoreCLR startup immediately (0x8007000E), so
    # .NET cases run without it; their storage bound stays the 8 MiB tmpfs
    # plus manager capture caps. Native cases keep the 1 MiB backstop.
    if fsize:
        resource.setrlimit(resource.RLIMIT_FSIZE, (1048576,1048576))
    resource.setrlimit(resource.RLIMIT_NOFILE,(64,64))
    resource.setrlimit(resource.RLIMIT_CORE,(0,0))
    resource.setrlimit(resource.RLIMIT_CPU,(12,12))
def filter_bytes():
    # Frozen P02/P03 composition, byte-identical. x86_64 only.
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

_libc = ctypes.CDLL(None, use_errno=True)
_libc.mount.argtypes = [ctypes.c_char_p, ctypes.c_char_p, ctypes.c_char_p, ctypes.c_ulong, ctypes.c_char_p]
_libc.mount.restype = ctypes.c_int
_libc.unshare.argtypes = [ctypes.c_int]
_libc.unshare.restype = ctypes.c_int

def parse_events(text):
    out={}
    for line in text.splitlines():
        parts=line.split()
        if len(parts)==2:
            try:out[parts[0]]=int(parts[1])
            except ValueError:out[parts[0]]=parts[1]
    return out
def leaf_counters(leaf):
    return {'peak':read(leaf/'memory.peak'),'events':parse_events(read(leaf/'memory.events')),
            'pids_events':parse_events(read(leaf/'pids.events'))}
def ev_get(events, key):
    v=events.get(key,0)
    return v if isinstance(v,int) else 0

FD_OK=re.compile(r'^fd \d+ (pipe:\[\d+\]|/proc/\d+/fd|/dev/null)$')
KNOWN_RW={'/','/proc','/dev','/dev/pts','/dev/shm','/tmp','/sys','/run','/target','/input','/app',
           '/dev/null','/dev/zero','/dev/full','/dev/random','/dev/urandom','/dev/tty',
           '/lib64','/usr','/usr/lib','/usr/lib/x86_64-linux-gnu'}
def check_fd(stdout):
    return [ln for ln in stdout.splitlines() if ln.startswith('fd ') and not FD_OK.match(ln)]
def check_mounts(stdout):
    bad=[];inside=False
    for ln in stdout.splitlines():
        if ln=='mounts<<':inside=True;continue
        if ln=='>>':inside=False;continue
        if inside:
            parts=ln.split()
            if len(parts)>=6 and 'rw' in parts[5].split(',') and parts[4] not in KNOWN_RW:
                bad.append(ln)
    return bad
def created_threads(stdout):
    m=re.search(r'^threads_created (\d+)',stdout,flags=re.M)
    return int(m.group(1)) if m else None

COMMON=['/usr/bin/bwrap','--unshare-user','--unshare-pid','--unshare-net','--unshare-ipc','--unshare-uts',
        '--uid','0','--gid','0','--disable-userns','--assert-userns-disabled','--cap-drop','ALL',
        '--die-with-parent','--new-session','--clearenv','--ro-bind','{root}/worker','/target',
        '--ro-bind','{root}/input','/input','--proc','/proc','--dev','/dev',
        '--size','8388608','--tmpfs','/tmp','--dir','/sys','--dir','/run','--chdir','/tmp']

# kind, mode, N, pos_rc, cand_rc, pos_markers, cand_markers, oom, pidsmax, crule, extra
NATIVE_CASES=[
 ('native','mem',64,0,0,['touched_sum'],['touched_sum'],0,0,None,None),
 ('native','mem',192,0,137,['touched_sum'],[],1,0,None,None),
 ('native','threads',8,0,0,[],[],0,0,('eq',8),None),
 ('native','threads',40,0,0,[],[],0,0,('eq',40),None),
 ('native','threads',60,0,0,[],[],0,1,('lt',60),None),
 ('native','scratch-fill',0,0,0,['scratch_bytes'],['scratch_bytes'],0,0,None,None),
 ('native','inode-fill',0,0,0,['inode_count'],['inode_count'],0,0,None,None),
 ('native','fd-consume',0,0,0,['fd_count'],['fd_count'],0,0,None,None),
 ('native','shm-probe',0,0,0,['shm_touched'],['shm_touched'],0,0,None,None),
 ('native','shm-path',0,0,0,['shm_bytes'],['shm_bytes'],0,0,None,'shm'),
 ('native','oomscore',0,0,0,['oom_lower rc=-1','oom_raise rc=3'],['oom_lower rc=-1','oom_raise rc=3'],0,0,None,None),
 ('native','mountinfo',0,0,0,['mountinfo_read'],['mountinfo_read'],0,0,None,'mounts'),
 ('native','emptydir-probe',0,0,0,['sys_create rc=-1'],['sys_remove rc=0'],0,0,None,None),
 ('native','big-output',0,17,17,[],[],0,0,None,'bigout'),
 ('native','crash-abort',0,-6,134,['marker_write'],['marker_write'],0,0,None,'crash'),
 ('native','null-writes',0,0,0,['null_bytes','rofile_write rc=-1 errno=13'],['null_bytes','rofile_write rc=-1 errno=13'],0,0,None,None),
]
# kind, name, alloc, threads, pos_rc, cand_rc, pos_markers, cand_markers, oom
DOTNET_CASES=[
 ('dotnet','hello',0,0,0,0,['complete'],['complete'],0),
 ('dotnet','alloc64',64,0,0,0,['touched_sum','complete'],['touched_sum','complete'],0),
 ('dotnet','alloc160',160,0,0,137,['touched_sum','complete'],[],1),
]
def case_id(case):
    return (case[0]+'-'+case[1]+'-'+str(case[2])) if case[0]=='native' else case[1]

def launch(cmd, cwd=None, env=None, preexec=None, pass_fds=(), timeout=20):
    p=subprocess.Popen(cmd,stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
                       cwd=cwd,env=env,close_fds=True,pass_fds=pass_fds,preexec_fn=preexec)
    try:
        stdout,stderr=p.communicate(timeout=timeout)
        return dict(rc=p.returncode,out_len=len(stdout),err_len=len(stderr),
                    stdout=stdout.decode(errors='replace')[:32768],stderr=stderr.decode(errors='replace')[:8192],timeout=False)
    except subprocess.TimeoutExpired:
        p.kill();stdout,stderr=p.communicate()
        return dict(rc=p.returncode,out_len=len(stdout),err_len=len(stderr),
                    stdout=stdout.decode(errors='replace')[:32768],stderr=stderr.decode(errors='replace')[:8192],timeout=True)

def manager(a):
    assert os.getuid()==UID and os.getgid()==UID
    cg=pathlib.Path(a.cg); root=pathlib.Path(a.root); app=pathlib.Path(a.app_dir)
    out={'uid':os.getuid(),'gid':os.getgid(),'status':read('/proc/self/status'),'cgroup':read('/proc/self/cgroup')}
    dotnet_ok=(app/'P04Workload').exists() and os.access(app/'P04Workload',os.X_OK)
    out['dotnet_staging']={'dir':str(app),'present':dotnet_ok,
        'binary_sha256':sha(app/'P04Workload') if dotnet_ok else None}
    # Dynamic apphost needs its loader + lib closure ro-bound at absolute
    # paths (workload provisioning, like /app itself — no enforcement
    # change). Resolve via ldd, bind real files, remount loader dirs ro.
    dep_argv=[];dep_record={}
    if dotnet_ok:
        try:
            sons=[]
            for binary in (app/'P04Workload',app/'libcoreclr.so'):
                lr=run(['ldd',str(binary)])
                if lr['rc']!=0 or 'not found' in lr['stdout']:
                    raise RuntimeError('ldd failed for '+binary.name)
                for line in lr['stdout'].splitlines():
                    line=line.strip()
                    if '=>' in line:
                        son=line.split('=>')[1].strip().split(' ')[0]
                    elif line.startswith('/'):
                        son=line.split(' ')[0]
                    else:
                        continue
                    if son not in sons:sons.append(son)
            parents=[]
            for son in sons:
                real=os.path.realpath(son)
                dep_record[son]={'real':real,'sha256':sha(real)}
                parent=os.path.dirname(son)
                if parent not in parents:parents.append(parent)
            parents.sort(key=len)
            for p in parents:dep_argv+=['--dir',p]
            for son in sons:dep_argv+=['--ro-bind',dep_record[son]['real'],son]
            # No --remount-ro: it applies to mount points, not plain dirs
            # (measured: "Unable to find in mount table"). Loader dirs stay
            # writable private tmpfs, memcg-bounded and destroyed at teardown
            # — same class as /sys//run, covered by the mount inventory.
        except Exception as e:
            out['dotnet_loader_error']=repr(e);dep_argv=[];dep_record={};dotnet_ok=False
    out['dotnet_loader']={'binds':dep_record,'argv':dep_argv,'ok':dotnet_ok}
    out['input_sha256_before']=sha(root/'input')
    try:
        gates=json.loads((root/'gates.json').read_text())
        positives=json.loads((root/'positives.json').read_text())
    except OSError as e:
        out['error']='gate file missing: '+repr(e);print(json.dumps(out),flush=True);return
    out['positives_embedded']=sorted(positives.keys())
    try:
        out['positives']={k:positives[k] for k in sorted(positives.keys())}
    except Exception as e:
        out['positives_embed_error']=repr(e)
    cases=[]; halted=False; seccomp_fd=None
    try:
        (cg/'sibling').mkdir();out['sibling_create']=True
        write(cg/'sibling/memory.max',67108864);write(cg/'sibling/pids.max',16)
        def enter_target_native():
            write(cg/'target/cgroup.procs',os.getpid());bounds()
        def enter_target_dotnet():
            write(cg/'target/cgroup.procs',os.getpid());bounds(fsize=False)
        seccomp_fd=os.memfd_create('ar7-P04-seccomp',os.MFD_CLOEXEC)
        os.write(seccomp_fd,filter_bytes());os.lseek(seccomp_fd,0,0)
        out['filter_sha256']=hashlib.sha256(filter_bytes()).hexdigest()
        common=[s.format(root=root) for s in COMMON]
        for case in NATIVE_CASES+DOTNET_CASES:
            kind=case[0]
            if kind=='dotnet' and not dotnet_ok:
                cases.append({'case':case[1],'leg':'INCONCLUSIVE (staging absent)'})
                continue
            dname=case_id(case)
            if kind=='native':
                _,mode,N,pos_rc,cand_rc,pos_markers,cand_markers,oom,pidsmax,crule,extra=case
            else:
                _,_,alloc,threads,pos_rc,cand_rc,pos_markers,cand_markers,oom=case
                N=(alloc,threads);mode=None;crule=None;extra=None;pidsmax=0
            if halted:
                cases.append({'case':dname,'status':'NOT RUN (halted after BLOCKED leg)'})
                continue
            rec={'case':dname,'N':N}
            g=gates.get(dname)
            if not isinstance(g,dict) or not g.get('ok'):
                rec['leg']='INCONCLUSIVE (positive gate failed; candidate not released)'
                cases.append(rec);continue
            files_before=sorted(p.name for p in root.iterdir())
            bt_before=leaf_counters(cg/'target')
            # The pass_fds dup shares the memfd offset: every bwrap consumes
            # the filter bytes, so rewind before each launch or later
            # candidates read an empty program (measured EINVAL cause).
            os.lseek(seccomp_fd,0,os.SEEK_SET)
            if kind=='native':
                argv=(common+['--seccomp',str(seccomp_fd),'--','/target',mode,str(cg),str(root),a.nonce,'/input',str(N)])
                cand=launch(argv,preexec=enter_target_native,pass_fds=(seccomp_fd,))
            else:
                argv=(common+['--ro-bind',str(app),'/app']+dep_argv+['--setenv','HOME','/tmp','--setenv','DOTNET_GCServer','0',
                              '--seccomp',str(seccomp_fd),'--','/app/P04Workload',str(alloc),str(threads),'/input','/tmp'])
                cand=launch(argv,preexec=enter_target_dotnet,pass_fds=(seccomp_fd,))
            rec['candidate']=cand
            bt_after=leaf_counters(cg/'target')
            oom_delta=ev_get(bt_after['events'],'oom_kill')-ev_get(bt_before['events'],'oom_kill')
            pmax_delta=ev_get(bt_after['pids_events'],'max')-ev_get(bt_before['pids_events'],'max')
            rec['counter_deltas']={'oom_kill':oom_delta,
                                   'pids_max':pmax_delta,
                                   'target_peak':bt_after['peak']}
            c=created_threads(cand['stdout']) if kind=='native' else None
            fd_bad=check_fd(cand['stdout']) if kind=='native' else []
            mounts_bad=check_mounts(cand['stdout']) if extra=='mounts' else []
            rec['candidate_fd_bad']=fd_bad;rec['candidate_mounts_bad']=mounts_bad
            basic=(cand['rc']==cand_rc and not cand['timeout']
                   and all(m in cand['stdout'] for m in cand_markers) and not fd_bad)
            if kind=='native' and mode=='big-output':
                basic=basic and cand['out_len']>=1048576
            if kind=='native' and mode=='threads' and crule is not None and crule[0]=='eq':
                basic=basic and c==crule[1]
            over_capable=((kind=='native' and mode=='mem' and N==192 and cand['rc']==0)
                          or (crule is not None and crule[0]=='lt' and c==crule[1]))
            if over_capable:
                leg='BLOCKED (limit not enforced)'
            elif (basic and oom_delta==oom and (pidsmax==0 or pmax_delta>=1)
                  and not mounts_bad
                  and not (crule is not None and crule[0]=='lt' and (c is None or c>=crule[1]))):
                leg='SUPPORTED-leg'
            elif cand['rc']==cand_rc and oom==1 and oom_delta==0:
                leg='INCONCLUSIVE (expected OOM without oom_kill event)'
            else:
                leg='INCONCLUSIVE (candidate mismatch; under review)'
            if leg.startswith('BLOCKED'):
                halted=True
            rec['leg']=leg
            files_after=sorted(p.name for p in root.iterdir())
            rec['workspace_delta']=[n for n in files_after if n not in files_before]
            cases.append(rec)
        out['cases']=cases
        out['halted']=halted
        out['input_sha256_after']=sha(root/'input')
    except Exception as e:out['error']=repr(e)
    finally:
        if seccomp_fd is not None:
            try:os.close(seccomp_fd)
            except OSError:pass
        out['manager_final_cgroup']=read('/proc/self/cgroup')
    print(json.dumps(out),flush=True)

def observer(a):
    assert os.getuid()==0
    nonce=uuid.uuid4().hex; root=pathlib.Path('/tmp')/('gagamba-ar7-P04-'+nonce)
    outer=pathlib.Path('/sys/fs/cgroup')/('gagamba-ar7-P04-'+nonce);cg=outer/'delegated'
    repo=pathlib.Path(__file__).resolve().parent.parent.parent
    staging=repo/'artifacts/ar7/P04/dotnet'
    data={'probe':'P04','source_sha':a.source_sha,'nonce':nonce,'date_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),
          'host':{'kernel':os.uname().release,'arch':os.uname().machine,'distribution':read('/etc/os-release'),
          'glibc':os.confstr('CS_GNU_LIBC_VERSION'),'lsm':read('/sys/kernel/security/lsm'),
          'yama':read('/proc/sys/kernel/yama/ptrace_scope'),'userns_max':read('/proc/sys/user/max_user_namespaces'),
          'cgroup_mount':read('/proc/mounts'),'helper':run(['/usr/bin/bwrap','--version']),
          'helper_sha256':sha('/usr/bin/bwrap'),'helper_mode':oct(os.stat('/usr/bin/bwrap').st_mode&0o7777)},
          'paths':{'root':str(root),'outer':str(outer),'delegated':str(cg)},'observer_uid':os.getuid(),
          'source_hashes':{p.name:sha(p) for p in list(pathlib.Path(__file__).parent.glob('p04.*'))+[pathlib.Path(__file__).parent/'dotnet'/'Program.cs',pathlib.Path(__file__).parent/'dotnet'/'P04Workload.csproj']},
          'dotnet_staging':{'dir':str(staging),'present':(staging/'P04Workload').exists(),
            'binary_sha256':sha(staging/'P04Workload') if (staging/'P04Workload').exists() else None}}
    p=None;clean=False;done=threading.Event();watcher=None
    try:
        if data['host']['helper_sha256']!=PIN or os.stat('/usr/bin/bwrap').st_mode&0o6000:raise RuntimeError('helper pin/nonsetuid prerequisite changed')
        root.mkdir(mode=0o755)
        data['compile']=run(['/usr/bin/gcc','-static','-O2','-pthread','-Wall','-Wextra','-o',str(root/'worker'),str(pathlib.Path(__file__).with_suffix('.c'))])
        if data['compile']['rc']:raise RuntimeError('compile failed before release')
        data['worker_sha256']=sha(root/'worker')
        (root/'input').write_text(nonce);os.chmod(root/'input',0o444)
        outer.mkdir();write(outer/'memory.max',268435456);write(outer/'pids.max',64)
        write(outer/'memory.swap.max',0);write(outer/'cgroup.subtree_control','+memory +pids')
        cg.mkdir();write(cg/'memory.max',134217728);write(cg/'pids.max',48)
        write(cg/'cgroup.subtree_control','+memory +pids');leaf_own(cg)
        for name in ('manager','target'):(cg/name).mkdir();leaf_own(cg/name)
        (outer/'unbounded').mkdir();leaf_own(outer/'unbounded')
        os.chown(root,UID,UID)
        unbounded=outer/'unbounded'
        def place_pos(dotnet):
            # Read workload files BEFORE the private /tmp hides the fixture
            # dir; write them into the fresh tmpfs after mounting.
            wdata = None if dotnet else open(str(root/'worker'),'rb').read()
            idata = open(str(root/'input'),'rb').read()
            if hasattr(os,'unshare'):
                os.unshare(CLONE_NEWNS)
            elif _libc.unshare(CLONE_NEWNS)!=0:
                raise OSError(ctypes.get_errno(),'unshare')
            if _libc.mount(None,b'/',None,MS_REC|MS_PRIVATE,None)!=0:
                raise OSError(ctypes.get_errno(),'remount-private')
            if _libc.mount(b'tmpfs',b'/tmp',b'tmpfs',0,b'size=8388608')!=0:
                raise OSError(ctypes.get_errno(),'mount-tmpfs')
            with open('/tmp/pos-input','wb') as fh:
                fh.write(idata)
            os.chmod('/tmp/pos-input',0o444)
            if not dotnet:
                with open('/tmp/pos-worker','wb') as fh:
                    fh.write(wdata)
                os.chmod('/tmp/pos-worker',0o755)
            write(unbounded/'cgroup.procs',os.getpid())
            os.setgroups([]);os.setgid(UID);os.setuid(UID);bounds(fsize=not dotnet)
        dotnet_ok=data['dotnet_staging']['present']
        positives={};gates={}
        all_cases=NATIVE_CASES+([c for c in DOTNET_CASES] if dotnet_ok else [])
        for case in all_cases:
            kind=case[0]
            if kind=='native':
                _,mode,N,pos_rc,_,pos_markers,_,_,_,crule,extra=case
                dname=case_id(case)
                cmd=['/tmp/pos-worker',mode,str(cg),str(root),nonce,'/tmp/pos-input',str(N)]
                kw=dict(cwd='/tmp',env=None)
                def pre():
                    place_pos(False)
            else:
                _,dname,alloc,threads,pos_rc,_,pos_markers,_,_=case
                cmd=[str(staging/'P04Workload'),str(alloc),str(threads),'/tmp/pos-input','/tmp']
                kw=dict(cwd='/tmp',env={'HOME':str(root),'DOTNET_GCServer':'0','PATH':os.environ.get('PATH','')})
                def pre():
                    place_pos(True)
            rec={'case':dname}
            bu_before=leaf_counters(unbounded)
            try:
                pos=launch(cmd,preexec=pre,**kw)
            except Exception as e:
                rec['positive']={'launch_error':repr(e)};rec['gate']=False
                positives[dname]=rec;gates[dname]={'ok':False,'reason':'launch failed'};continue
            rec['positive']=pos
            bu_after=leaf_counters(unbounded)
            rec['unbounded_oom_delta']=ev_get(bu_after['events'],'oom_kill')-ev_get(bu_before['events'],'oom_kill')
            ok=(pos['rc']==pos_rc and not pos['timeout']
                and all(m in pos['stdout'] for m in pos_markers))
            if kind=='native':
                ok=ok and not check_fd(pos['stdout'])
                if mode=='threads':
                    ok=ok and created_threads(pos['stdout'])==N
                if mode=='big-output':
                    ok=ok and pos['out_len']>=1048576
            rec['gate']=ok
            gates[dname]={'ok':ok}
            if kind=='native' and mode=='crash-abort' and (root/'crash-marker').exists():
                rec['positive_marker']=read(root/'crash-marker')
                (root/'crash-marker').unlink()
            if kind=='native' and mode=='shm-path':
                left=[x.name for x in pathlib.Path('/dev/shm').iterdir() if x.name.startswith('p04-')]
                rec['positive_shm_left']=left
                for x in pathlib.Path('/dev/shm').iterdir():
                    if x.name.startswith('p04-'):x.unlink()
            positives[dname]=rec
        # defense-in-depth sweeps (private tmpfs should already contain these)
        for pat,parent in (('fill-*','/tmp'),('inode-*','/tmp')):
            for x in pathlib.Path(parent).glob(pat):
                x.unlink()
        data['positives']={'count':len(positives),'gates_passed':sum(1 for g in gates.values() if g.get('ok'))}
        (root/'positives.json').write_text(json.dumps(positives))
        (root/'gates.json').write_text(json.dumps({k:{'ok':bool(v.get('ok'))} for k,v in gates.items()}))
        def drop():
            write(cg/'manager/cgroup.procs',os.getpid());os.setgroups([]);os.setgid(UID);os.setuid(UID);bounds()
        cmd=[sys.executable,__file__,'--manager','--root',str(root),'--cg',str(cg),'--nonce',nonce,'--app-dir',str(staging)]
        p=subprocess.Popen(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,preexec_fn=drop,close_fds=True)
        data['manager_pid']=p.pid
        data['external_process_samples']={}
        def observe():
            while not done.wait(.05):
                for path in (cg/'manager',cg/'target',cg/'sibling',outer/'unbounded'):
                    if not path.exists():continue
                    for pid in read(path/'cgroup.procs').split():
                        if not pid.isdecimal() or pid in data['external_process_samples']:continue
                        membership=read('/proc/'+pid+'/cgroup')
                        if str(outer).removeprefix('/sys/fs/cgroup') not in membership:continue
                        data['external_process_samples'][pid]={'cgroup':membership,'status':read('/proc/'+pid+'/status'),
                            'command':read('/proc/'+pid+'/cmdline')}
        watcher=threading.Thread(target=observe,daemon=True);watcher.start()
        try:
            stdout,stderr=p.communicate(timeout=480);data['manager_rc']=p.returncode
            data['manager_stderr']=stderr[:8192].decode(errors='replace')
            data['manager']=json.loads(stdout)
        except subprocess.TimeoutExpired:
            data['watchdog_fired']=True;write(outer/'cgroup.kill',1);stdout,stderr=p.communicate(timeout=5)
            data['partial_stdout']=stdout[:32768].decode(errors='replace')
        data['host_shm_p04_left']=[x.name for x in pathlib.Path('/dev/shm').iterdir() if x.name.startswith('p04-')]
        data['observer_domains']={str(q.relative_to(outer)):read(q/'cgroup.procs') for q in [outer,cg,*cg.iterdir(),*outer.iterdir()] if q.is_dir()}
    except Exception as e:data['setup_error']=repr(e)
    finally:
        done.set()
        if watcher:watcher.join(timeout=1)
        if outer.exists():
            try:
                write(outer/'cgroup.kill',1)
                if p:p.wait(timeout=5)
                for _ in range(30):
                    if 'populated 0' in read(outer/'cgroup.events'):break
                    time.sleep(.1)
                data['cleanup_events']=read(outer/'cgroup.events')
                if 'populated 0' not in data['cleanup_events']:raise RuntimeError('outer not empty')
                known={'manager','target','sibling','new-positive','new-candidate','unbounded'}
                unknown=([q.name for q in cg.iterdir() if q.is_dir() and q.name not in known]
                         +[q.name for q in outer.iterdir() if q.is_dir() and q.name not in known | {'delegated'}])
                if unknown:raise RuntimeError('unknown cgroup descendants preserved: '+repr(unknown))
                for parent in (cg,outer):
                    for name in sorted(known):
                        q=parent/name
                        if q.exists():q.rmdir()
                if cg.exists():cg.rmdir()
                outer.rmdir()
            except Exception as e:data['cleanup_error']=repr(e)
        if not outer.exists():
            if root.exists():
                for name in ('input','worker','crash-marker','positives.json','gates.json'):
                    q=root/name
                    if q.exists() or q.is_symlink():q.unlink()
                try:root.rmdir()
                except OSError as e:data['cleanup_error']=repr(e)
        data['cleanup_absence']={'outer':not outer.exists(),'workspace':not root.exists(),
                                'manager':p is None or p.poll() is not None,
                                'host_shm_clean':not data.get('host_shm_p04_left')}
        clean=all(data['cleanup_absence'].values()) and not data.get('cleanup_error')
        data['cleanup_confirmed']=clean
        dest=pathlib.Path(a.output);dest.mkdir(parents=True,exist_ok=True)
        (dest/(nonce+'.json')).write_text(json.dumps(data,indent=2))
        print(json.dumps(data,indent=2))
    return 0 if clean else 2

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--manager',action='store_true');ap.add_argument('--source-sha');ap.add_argument('--output');ap.add_argument('--root');ap.add_argument('--cg');ap.add_argument('--nonce');ap.add_argument('--app-dir');args=ap.parse_args()
    if args.manager:manager(args)
    else:sys.exit(observer(args))
