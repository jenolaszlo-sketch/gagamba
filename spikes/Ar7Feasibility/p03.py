"""Disposable P03 only. Root provisions/observes; manager and targets are UID 65534.
Candidate composition is byte-identical to frozen P02 (same helper argv, same
cBPF filter, same cgroup leaves). No production code. Every attempt is bounded
by an independent outer cgroup and a 30-second root observer. Never remove
target-provided paths.
"""
import argparse, ctypes, hashlib, json, os, pathlib, resource, select, socket
import struct, subprocess, sys, time, uuid, threading

UID = 65534
PIN = '523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6'
META_ADDR = '169.254.169.254'

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
    # Frozen P02 composition, byte-identical. x86_64 only; reject compat/x32.
    # EPERM for authority syscalls; clone3 ENOSYS for libc fallback.
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

class Log:
    def __init__(self): self.lock=threading.Lock(); self.items=[]
    def add(self, **kw):
        kw['t']=time.time()
        with self.lock: self.items.append(kw)

def recvn(c, n):
    data=b''
    while len(data)<n:
        chunk=c.recv(n-len(data))
        if not chunk: break
        data+=chunk
    return data

def read_http(c):
    data=b''
    while b'\r\n\r\n' not in data and len(data)<4096:
        try: chunk=c.recv(4096-len(data))
        except socket.timeout: break
        if not chunk: break
        data+=chunk
    return data

def dns_reply(q):
    if len(q)<12: return None
    qd=struct.unpack('!H',q[4:6])[0]
    if qd!=1: return None
    i=12
    while i<len(q) and q[i]!=0:
        ln=q[i]; i+=1+ln
        if i>len(q): return None
    i+=1
    if i+4>len(q): return None
    question=q[12:i+4]
    return q[0:2]+b'\x81\x80'+q[4:6]+b'\x00\x01\x00\x00\x00\x00'+question+b'\xc0\x0c\x00\x01\x00\x01\x00\x00\x00\x3c\x00\x04\x7f\x00\x00\x01'

def manager(a):
    assert os.getuid()==UID and os.getgid()==UID
    cg=pathlib.Path(a.cg); root=pathlib.Path(a.root)
    nonce=a.nonce; dtoken='daemon-'+nonce
    out={'uid':os.getuid(),'gid':os.getgid(),'status':read('/proc/self/status'),'cgroup':read('/proc/self/cgroup')}
    log=Log(); stop=threading.Event(); listeners=[]; threads=[]
    enabled=set(); ports={}; paths={}; abstract='ar7-P03-'+nonce
    try:
        (cg/'sibling').mkdir();out['sibling_create']=True
        write(cg/'sibling/memory.max',67108864);write(cg/'sibling/pids.max',16)
        out['manager_limits']={n:read(cg/'sibling'/n) for n in ('memory.max','pids.max','cgroup.procs')}
        def tcp_server(tag, family, addr, expect, reply):
            try:
                s=socket.socket(family,socket.SOCK_STREAM);s.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
                s.bind(addr);s.listen(4)
            except OSError as e:
                out[tag+'_bind_error']=repr(e);return None
            ports[tag]=s.getsockname()[1];enabled.add(tag)
            def loop():
                s.settimeout(0.3)
                while not stop.is_set():
                    try:c,peer=s.accept()
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    with c:
                        c.settimeout(2.0)
                        try:
                            data=recvn(c,len(expect))
                            ok=(data==expect)
                            if ok:c.sendall(reply)
                            log.add(phase=ph,endpoint=tag,matched=ok,request=data.decode('ascii','replace') if ok else 'MISMATCH:'+data.hex(),peer=str(peer))
                        except (socket.timeout,OSError) as e:
                            log.add(phase=ph,endpoint=tag,matched=False,request='TRANSPORT-ERROR:'+repr(e),peer='')
            t=threading.Thread(target=loop,daemon=True);t.start();threads.append(t);listeners.append(s)
            return s
        PH=['positive']
        # Generic TCP responders (nonce echo).
        tcp_server('tcp4',socket.AF_INET,('127.0.0.1',0),nonce.encode(),b'ok')
        tcp_server('tcp6',socket.AF_INET6,('::1',0),nonce.encode(),b'ok')
        # UDP responder.
        try:
            u=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);u.bind(('127.0.0.1',0))
            ports['udp']=u.getsockname()[1];enabled.add('udp')
            def udp_loop():
                u.settimeout(0.3)
                while not stop.is_set():
                    try:data,peer=u.recvfrom(256)
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    ok=(data==nonce.encode())
                    if ok:
                        try:u.sendto(b'ok',peer)
                        except OSError:pass
                    log.add(phase=ph,endpoint='udp',matched=ok,request=data.decode('ascii','replace') if ok else 'MISMATCH:'+data.hex(),peer=str(peer))
            t=threading.Thread(target=udp_loop,daemon=True);t.start();threads.append(t);listeners.append(u)
        except OSError as e:out['udp_bind_error']=repr(e)
        # DNS-like responder, fixed 5353 UDP+TCP per design; else INCONCLUSIVE.
        try:
            du=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);du.bind(('127.0.0.1',5353))
            dt=socket.socket(socket.AF_INET,socket.SOCK_STREAM);dt.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
            dt.bind(('127.0.0.1',5353));dt.listen(4)
            ports['dns']=5353;enabled.add('dns')
            def dns_udp():
                du.settimeout(0.3)
                while not stop.is_set():
                    try:data,peer=du.recvfrom(512)
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    rep=dns_reply(data);ok=(rep is not None and nonce.encode() in data)
                    if ok:
                        try:du.sendto(rep,peer)
                        except OSError:pass
                    log.add(phase=ph,endpoint='dns',matched=ok,request='qname-has-nonce' if ok else 'MISMATCH:'+data.hex(),peer=str(peer))
            def dns_tcp():
                dt.settimeout(0.3)
                while not stop.is_set():
                    try:c,peer=dt.accept()
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    with c:
                        c.settimeout(2.0)
                        try:
                            ln=recvn(c,2)
                            if len(ln)<2:raise OSError('short length')
                            data=recvn(c,struct.unpack('!H',ln)[0])
                            rep=dns_reply(data);ok=(rep is not None and nonce.encode() in data)
                            if ok:c.sendall(struct.pack('!H',len(rep))+rep)
                            log.add(phase=ph,endpoint='dns',matched=ok,request='qname-has-nonce' if ok else 'MISMATCH:'+data.hex(),peer=str(peer))
                        except (socket.timeout,OSError) as e:
                            log.add(phase=ph,endpoint='dns',matched=False,request='TRANSPORT-ERROR:'+repr(e),peer='')
            for fn in (dns_udp,dns_tcp):
                t=threading.Thread(target=fn,daemon=True);t.start();threads.append(t)
            listeners.extend((du,dt))
        except OSError as e:out['dns_bind_error']=repr(e)
        # Proxy-like and metadata responders.
        def http_server(tag, addr, check, reply):
            try:
                s=socket.socket(socket.AF_INET,socket.SOCK_STREAM);s.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
                s.bind(addr);s.listen(4)
            except OSError as e:
                out[tag+'_bind_error']=repr(e);return None
            ports[tag]=s.getsockname()[1];enabled.add(tag)
            def loop():
                s.settimeout(0.3)
                while not stop.is_set():
                    try:c,peer=s.accept()
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    with c:
                        c.settimeout(2.0)
                        try:
                            data=read_http(c)
                            ok=check(data)
                            if ok:c.sendall(reply)
                            head=data.split(b'\r\n')[0].decode('ascii','replace') if data else 'EMPTY'
                            log.add(phase=ph,endpoint=tag,matched=ok,request=head+' nonce-present' if ok else 'MISMATCH:'+data[:128].hex(),peer=str(peer))
                        except (socket.timeout,OSError) as e:
                            log.add(phase=ph,endpoint='proxy' if tag=='proxy' else tag,matched=False,request='TRANSPORT-ERROR:'+repr(e),peer='')
            t=threading.Thread(target=loop,daemon=True);t.start();threads.append(t);listeners.append(s)
            return s
        http_server('proxy',('127.0.0.1',0),lambda d:d.startswith(b'CONNECT ') and nonce.encode() in d,b'HTTP/1.1 200 Connection established\r\n\r\n')
        http_server('metadata',(META_ADDR,0),lambda d:d.startswith(b'GET /metadata') and nonce.encode() in d,b'HTTP/1.1 200 OK\r\nContent-Length: 26\r\n\r\n{"rack":"ws12","n":1}\n')
        # Unix responders: pathname with peer-credential gate + abstract.
        def unix_server(tag, addr, addrlen, expect, reply):
            try:
                s=socket.socket(socket.AF_UNIX,socket.SOCK_STREAM)
                if isinstance(addr,str) and not addr.startswith('\0'):
                    try:os.unlink(addr)
                    except OSError:pass
                s.bind(addr);s.listen(4)
            except OSError as e:
                out[tag+'_bind_error']=repr(e);return None
            enabled.add(tag)
            def loop():
                s.settimeout(0.3)
                while not stop.is_set():
                    try:c,_=s.accept()
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    with c:
                        c.settimeout(2.0)
                        try:
                            peer=struct.unpack('3i',c.getsockopt(socket.SOL_SOCKET,socket.SO_PEERCRED,12))
                            data=recvn(c,len(expect))
                            ok=(data==expect and peer[1]==UID)
                            if ok:c.sendall(reply)
                            log.add(phase=ph,endpoint=tag,matched=ok,request=data.decode('ascii','replace') if ok else 'MISMATCH:'+data.hex(),peer='%d/%d/%d'%peer)
                        except (socket.timeout,OSError) as e:
                            log.add(phase=ph,endpoint=tag,matched=False,request='TRANSPORT-ERROR:'+repr(e),peer='')
            t=threading.Thread(target=loop,daemon=True);t.start();threads.append(t);listeners.append(s)
            return s
        for name in ('chan.sock','agent.sock','daemon.sock'):
            paths[name]=str(root/name)
        unix_server('unix-path',paths['chan.sock'],None,nonce.encode(),b'ok')
        unix_server('unix-abstract','\0'+abstract,None,nonce.encode(),b'ok')
        # Agent-like framing: len16 + type(11) + nonce; empty identities answer.
        try:
            s=socket.socket(socket.AF_UNIX,socket.SOCK_STREAM)
            try:os.unlink(paths['agent.sock'])
            except OSError:pass
            s.bind(paths['agent.sock']);s.listen(4);enabled.add('agent')
            def agent_loop():
                s.settimeout(0.3)
                while not stop.is_set():
                    try:c,_=s.accept()
                    except socket.timeout:continue
                    except OSError:break
                    ph=PH[0]
                    with c:
                        c.settimeout(2.0)
                        try:
                            peer=struct.unpack('3i',c.getsockopt(socket.SOL_SOCKET,socket.SO_PEERCRED,12))
                            ln=recvn(c,2)
                            data=recvn(c,struct.unpack('!H',ln)[0]) if len(ln)==2 else b''
                            ok=(len(data)>=33 and data[0]==11 and data[1:33]==nonce.encode() and peer[1]==UID)
                            if ok:c.sendall(struct.pack('!H',5)+bytes([12,0,0,0,0]))
                            log.add(phase=ph,endpoint='agent',matched=ok,request='agent11-nonce' if ok else 'MISMATCH:'+data.hex(),peer='%d/%d/%d'%peer)
                        except (socket.timeout,OSError,struct.error) as e:
                            log.add(phase=ph,endpoint='agent',matched=False,request='TRANSPORT-ERROR:'+repr(e),peer='')
            t=threading.Thread(target=agent_loop,daemon=True);t.start();threads.append(t);listeners.append(s)
        except OSError as e:out['agent_bind_error']=repr(e)
        unix_server('daemon',paths['daemon.sock'],None,dtoken.encode(),b'ok')
        out['enabled']=sorted(enabled);out['ports']=dict(ports);out['paths']=dict(paths);out['abstract']=abstract
        # Worker argv: mode cg root nonce dtoken input tcp4 tcp6 udp dns proxy
        # meta tcp6ok metaok dnsok agent daemon generic abstract
        def wargs(mode):
            g=lambda k: str(ports.get(k,0))
            return [str(root/'worker'),mode,str(cg),str(root),nonce,dtoken,str(root/'input'),
                    g('tcp4'),g('tcp6'),g('udp'),g('dns'),g('proxy'),g('metadata'),
                    '1' if 'tcp6' in enabled else '0','1' if 'metadata' in enabled else '0','1' if 'dns' in enabled else '0',
                    paths['agent.sock'],paths['daemon.sock'],paths['chan.sock'],abstract]
        def launch(cmd, pass_fds=()):
            p=subprocess.Popen(cmd,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,close_fds=True,pass_fds=pass_fds,preexec_fn=bounds)
            try:
                stdout,stderr=p.communicate(('P03-REQ '+nonce+'\n').encode(),timeout=25)
                return dict(rc=p.returncode,stdout=stdout.decode(errors='replace')[:32768],stderr=stderr.decode(errors='replace')[:8192],timeout=False)
            except subprocess.TimeoutExpired:
                p.kill();stdout,stderr=p.communicate()
                return dict(rc=p.returncode,stdout=stdout.decode(errors='replace')[:32768],stderr=stderr.decode(errors='replace')[:8192],timeout=True)
        import re
        fd_ok_pat=re.compile(r'^fd \d+ (pipe:\[\d+\]|/proc/\d+/fd|/dev/null)$')
        def check_fd(stdout):
            bad=[ln for ln in stdout.splitlines() if ln.startswith('fd ') and not fd_ok_pat.match(ln)]
            return bad
        time.sleep(0.2)
        out['positive']=launch(wargs('positive'))
        pos_logs=[e for e in log.items if e['phase']=='positive']
        def pos_ok(tag):
            return any(e['endpoint']==tag and e['matched'] for e in pos_logs)
        required={t for t in enabled if t in ('tcp4','tcp6','udp','dns','proxy','metadata','unix-path','unix-abstract','agent','daemon')}
        pos=out['positive']
        out['positive_oracle']={t:pos_ok(t) for t in sorted(required)}
        out['positive_fd_bad']=check_fd(pos['stdout'])
        out['positive_gate']=(pos['rc']==0 and not pos['timeout']
            and 'input_read rc=32' in pos['stdout'] and 'stdio_echo rc=0' in pos['stdout']
            and 'ping_echo_reply rc=0' in pos['stdout'] and 'uring_ping_reply rc=0' in pos['stdout']
            and pos['stdout'].rstrip().endswith('complete') and not out['positive_fd_bad']
            and all(out['positive_oracle'].values()))
        out['controls_after_positive']={n:read(cg/n) for n in ('memory.max','pids.max','cgroup.subtree_control')}
        if not out['positive_gate']:raise RuntimeError('positive gate failed: INCONCLUSIVE, no target release')
        common=['/usr/bin/bwrap','--unshare-user','--unshare-pid','--unshare-net','--unshare-ipc','--unshare-uts',
                '--uid','0','--gid','0','--disable-userns','--assert-userns-disabled','--cap-drop','ALL',
                '--die-with-parent','--new-session','--clearenv','--ro-bind',str(root/'worker'),'/target',
                '--ro-bind',str(root/'input'),'/input','--proc','/proc','--dev','/dev',
                '--size','8388608','--tmpfs','/tmp','--dir','/sys','--dir','/run','--chdir','/tmp']
        f=os.memfd_create('ar7-P03-seccomp',os.MFD_CLOEXEC);os.write(f,filter_bytes());os.lseek(f,0,0)
        out['filter_sha256']=hashlib.sha256(filter_bytes()).hexdigest()
        def enter_target():
            write(cg/'target/cgroup.procs',os.getpid());bounds()
        PH[0]='candidate'
        out['candidate']=launch(common+['--seccomp',str(f),'--','/target',*wargs('candidate')[1:]],pass_fds=(f,))
        os.close(f)
        out['endpoint_requests_candidate']=[e for e in log.items if e['phase']=='candidate']
        out['controls_after_candidate']={str(p.relative_to(cg)):read(p) for p in
            (cg/'memory.max',cg/'pids.max',cg/'cgroup.subtree_control',cg/'sibling/memory.max',cg/'sibling/pids.max')}
        out['target_domain_after']=read(cg/'target/cgroup.events')
        cand_exact=[e for e in out['endpoint_requests_candidate'] if e['matched']]
        out['candidate_exact_requests']=cand_exact
        out['candidate_anomalies']=[e for e in out['endpoint_requests_candidate'] if not e['matched']]
        out['candidate_fd_bad']=check_fd(out['candidate']['stdout'])
    except Exception as e:out['error']=repr(e)
    finally:
        stop.set()
        for s in listeners:
            try:s.close()
            except OSError:pass
        for t in threads:t.join(timeout=3)
        out['manager_final_cgroup']=read('/proc/self/cgroup')
    print(json.dumps(out),flush=True)

def observer(a):
    assert os.getuid()==0
    nonce=uuid.uuid4().hex; root=pathlib.Path('/tmp')/('gagamba-ar7-P03-'+nonce)
    outer=pathlib.Path('/sys/fs/cgroup')/('gagamba-ar7-P03-'+nonce);cg=outer/'delegated'
    data={'probe':'P03','source_sha':a.source_sha,'nonce':nonce,'date_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),
          'host':{'kernel':os.uname().release,'arch':os.uname().machine,'distribution':read('/etc/os-release'),
          'glibc':os.confstr('CS_GNU_LIBC_VERSION'),'lsm':read('/sys/kernel/security/lsm'),
          'yama':read('/proc/sys/kernel/yama/ptrace_scope'),'userns_max':read('/proc/sys/user/max_user_namespaces'),
          'cgroup_mount':read('/proc/mounts'),'helper':run(['/usr/bin/bwrap','--version']),
          'helper_sha256':sha('/usr/bin/bwrap'),'helper_mode':oct(os.stat('/usr/bin/bwrap').st_mode&0o7777)},
          'paths':{'root':str(root),'outer':str(outer),'delegated':str(cg)},'observer_uid':os.getuid(),
          'source_hashes':{p.name:sha(p) for p in pathlib.Path(__file__).parent.glob('p03.*')}}
    p=None;clean=False;done=threading.Event();watcher=None;meta_added=False
    try:
        if data['host']['helper_sha256']!=PIN or os.stat('/usr/bin/bwrap').st_mode&0o6000:raise RuntimeError('helper pin/nonsetuid prerequisite changed')
        root.mkdir(mode=0o755)
        data['compile']=run(['/usr/bin/gcc','-static','-O2','-Wall','-Wextra','-o',str(root/'worker'),str(pathlib.Path(__file__).with_suffix('.c'))])
        if data['compile']['rc']:raise RuntimeError('compile failed before release')
        data['worker_sha256']=sha(root/'worker')
        (root/'input').write_text(nonce);os.chmod(root/'input',0o444)
        outer.mkdir();write(outer/'memory.max',268435456);write(outer/'pids.max',64)
        write(outer/'memory.swap.max',0);write(outer/'cgroup.subtree_control','+memory +pids')
        cg.mkdir();write(cg/'memory.max',134217728);write(cg/'pids.max',48)
        write(cg/'cgroup.subtree_control','+memory +pids');leaf_own(cg)
        for name in ('manager','target'):(cg/name).mkdir();leaf_own(cg/name)
        os.chown(root,UID,UID)
        meta=run(['/usr/sbin/ip','addr','add',META_ADDR+'/32','dev','lo'])
        data['metadata_provision']=meta;meta_added=(meta['rc']==0)
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
        if meta_added:
            data['metadata_removal']=run(['/usr/sbin/ip','addr','del',META_ADDR+'/32','dev','lo'])
        if not outer.exists():
            if root.exists():
                for name in ('chan.sock','agent.sock','daemon.sock','input','worker'):
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
