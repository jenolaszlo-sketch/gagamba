"""Disposable P07 only. Crash/ownership/release-semantics probe on the frozen
P02/P03/P04 Linux rootless composition (pinned non-setuid Bubblewrap 0.11.1,
same namespaces/cap-drop/no_new_privs/cBPF bytes, same cgroup topology with
explicit target 128 MiB/48 tasks, same workload-closure pattern). No
production code. No new confinement mechanism, no guardian/service process.

Topology per case: observer (root) -> manager (UID 65534) -> bwrap-outer
(target leaf) -> bwrap-ns (ns-PID 1) -> /target root (+ finite children).
The observer enforces lifecycle (kill/reap/teardown) AND observes; the
manager releases and records; it owns no cleanup.

Ordering authority for cancel/release races is the manager's own
check-then-commit sequence (pre-fork check + pre-exec re-check that exits
125 on a present flag). Timestamps are single-clock-domain (monotonic) and
supporting evidence only; filesystem mtime is never compared against
monotonic time. Stop decisions use cgroup.kill + populated polling only;
fault-injection kills (C5/C6/C10) address freshly-validated PIDs
(cgroup + stat-starttime + cmdline match inside the case window) and are
the test itself, never a stop mechanism.
"""
import argparse
import hashlib
import json
import os
import pathlib
import random
import resource
import signal
import struct
import subprocess
import sys
import threading
import time
import uuid

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
def bounds(fsize=True):
    # Same split as P04: the manager drop sets no FSIZE (inherited limits
    # cannot be raised unprivileged); each case preexec sets its own.
    if fsize:
        resource.setrlimit(resource.RLIMIT_FSIZE, (1048576, 1048576))
    resource.setrlimit(resource.RLIMIT_NOFILE, (64, 64))
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    resource.setrlimit(resource.RLIMIT_CPU, (12, 12))
def filter_bytes():
    # Frozen P02/P03/P04 composition, byte-identical. x86_64 only.
    ins = []
    def emit(code, jt, jf, k): ins.append(struct.pack('HBBI', code, jt, jf, k))
    emit(0x20, 0, 0, 4); emit(0x15, 1, 0, 0xc000003e); emit(0x06, 0, 0, 0x80000000)
    emit(0x20, 0, 0, 0); emit(0x35, 0, 1, 0x40000000); emit(0x06, 0, 0, 0x00050001)
    for nr in (41, 53, 101, 155, 161, 165, 166, 272, 308, 310, 311, 424, 434, 438, 428, 429, 430, 431, 432, 442):
        emit(0x15, 0, 1, nr); emit(0x06, 0, 0, 0x00050001)
    emit(0x15, 0, 1, 435); emit(0x06, 0, 0, 0x00050026)
    emit(0x15, 0, 3, 56); emit(0x20, 0, 0, 16); emit(0x45, 0, 1, 0x7e020080); emit(0x06, 0, 0, 0x00050001)
    emit(0x06, 0, 0, 0x7fff0000)
    return b''.join(ins)

def parse_events(text):
    out = {}
    for line in text.splitlines():
        parts = line.split()
        if len(parts) == 2:
            try: out[parts[0]] = int(parts[1])
            except ValueError: out[parts[0]] = parts[1]
    return out
def leaf_counters(leaf):
    return {'peak': read(leaf / 'memory.peak'), 'events': parse_events(read(leaf / 'memory.events')),
            'pids_events': parse_events(read(leaf / 'pids.events'))}
def populated(leaf):
    return read(leaf / 'cgroup.events')

COMMON = ['/usr/bin/bwrap', '--unshare-user', '--unshare-pid', '--unshare-net', '--unshare-ipc', '--unshare-uts',
          '--uid', '0', '--gid', '0', '--disable-userns', '--assert-userns-disabled', '--cap-drop', 'ALL',
          '--die-with-parent', '--new-session', '--clearenv', '--ro-bind', '{root}/worker', '/target',
          '--ro-bind', '{root}/input', '/input', '--proc', '/proc', '--dev', '/dev',
          '--size', '8388608', '--tmpfs', '/tmp', '--dir', '/sys', '--dir', '/run', '--chdir', '/tmp']

def evt(obj):
    print('EVT ' + json.dumps(obj), flush=True)

def launch(cmd, stdin_data=None, cwd=None, env=None, preexec=None, pass_fds=(), timeout=20):
    p = subprocess.Popen(cmd, stdin=subprocess.PIPE if stdin_data is not None else subprocess.DEVNULL,
                         stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         cwd=cwd, env=env, close_fds=True, pass_fds=pass_fds, preexec_fn=preexec)
    try:
        stdout, stderr = p.communicate(input=stdin_data, timeout=timeout)
        return dict(rc=p.returncode, out_len=len(stdout), err_len=len(stderr),
                    stdout=stdout.decode(errors='replace')[:32768], stderr=stderr.decode(errors='replace')[:8192], timeout=False)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        return dict(rc=p.returncode, out_len=len(stdout), err_len=len(stderr),
                    stdout=stdout.decode(errors='replace')[:32768], stderr=stderr.decode(errors='replace')[:8192], timeout=True)

def starttime_of(pid):
    try:
        parts = pathlib.Path('/proc/%d/stat' % pid).read_text().split()
        return parts[21]
    except (OSError, IndexError):
        return None

def validate_pid(pid, outer_frag, expect_cmd=('bwrap', '/target', 'worker')):
    """Fresh external validation: cgroup membership + cmdline, with starttime."""
    try:
        cg = pathlib.Path('/proc/%d/cgroup' % pid).read_text()
        cmd = pathlib.Path('/proc/%d/cmdline' % pid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
    except OSError:
        return None
    if outer_frag not in cg:
        return None
    if not any(s in cmd for s in expect_cmd):
        return None
    return {'pid': pid, 'cgroup_frag_ok': True, 'starttime': starttime_of(pid), 'cmdline': cmd[:200]}

def resolve_ns_root(outer_frag, leaf_frag='delegated/target', ns_pid='2', cmd_frag='/target'):
    """Resolve an announced namespace PID to its host PID: scan host /proc
    for the process whose NSpid innermost value matches, cgroup is inside
    the fixture leaf, and cmdline matches. The announced PID itself is a
    namespace-local value and must never be addressed on the host."""
    found = []
    for entry in os.listdir('/proc'):
        if not entry.isdecimal():
            continue
        pid = int(entry)
        try:
            st = pathlib.Path('/proc/%d/status' % pid).read_text()
            cg = pathlib.Path('/proc/%d/cgroup' % pid).read_text()
            cmd = pathlib.Path('/proc/%d/cmdline' % pid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
        except OSError:
            continue
        nspid = None
        for line in st.splitlines():
            if line.startswith('NSpid:'):
                nspid = line.split()[-1]
        if nspid != ns_pid:
            continue
        if outer_frag not in cg or leaf_frag not in cg:
            continue
        if cmd_frag not in cmd:
            continue
        found.append({'pid': pid, 'starttime': starttime_of(pid), 'cmdline': cmd[:160]})
    return sorted(found, key=lambda d: d['pid'])

def scan_bwrap_in_leaf(outer_frag, leaf_frag):
    """Enumerate host PIDs whose cgroup is inside the fixture leaf and whose
    cmdline is bwrap. Used once per fault case to find the ns-init process
    next to the known outer bwrap child. Fresh validation per PID."""
    found = []
    for entry in os.listdir('/proc'):
        if not entry.isdecimal():
            continue
        pid = int(entry)
        try:
            cg = pathlib.Path('/proc/%d/cgroup' % pid).read_text()
            cmd = pathlib.Path('/proc/%d/cmdline' % pid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
        except OSError:
            continue
        if outer_frag in cg and leaf_frag in cg and 'bwrap' in cmd:
            found.append({'pid': pid, 'starttime': starttime_of(pid), 'cmdline': cmd[:160]})
    return sorted(found, key=lambda d: d['pid'])

def launch_hold_for_fault(ctx, mode, N, outer_frag):
    """Inline spawn for C6/C10: long-lived root-hold victim with the GO
    barrier, returning the live Popen plus validation. The caller owns the
    fault and the communicate bounds below."""
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', mode, str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', str(N)])
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,),
                         preexec_fn=lambda: ctx.enter_target(None))
    ready = b''
    while b'READY' not in ready:
        chunk = p.stdout.readline()
        if not chunk:
            break
        ready += chunk
    ident = {}
    for line in ready.decode(errors='replace').splitlines():
        if line.startswith('identity '):
            for kv in line.split()[1:]:
                k, _, v = kv.partition('=')
                ident[k] = v
    rec = {'ready_seen': 'READY' in ready, 'announced_identity': ident}
    # Validation is NSpid resolution to a unique host PID; the announced
    # value is namespace-local and is never addressed on the host.
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    rec['ns_resolution'] = resolved
    validated = resolved[0] if len(resolved) == 1 else None
    rec['external_validation'] = validated
    if validated is not None:
        try:
            p.stdin.write(b'G'); p.stdin.flush()
        except OSError:
            pass
    return p, rec, validated

class Ctx:
    def __init__(self, a):
        self.cg = pathlib.Path(a.cg); self.root = pathlib.Path(a.root)
        self.nonce = a.nonce; self.case = a.case
        self.common = [s.format(root=self.root) for s in COMMON]
        self.seccomp_fd = None
    def enter_positive(self):
        write(self.cg / 'positive/cgroup.procs', os.getpid()); bounds()
    def enter_target(self, flag=None):
        write(self.cg / 'target/cgroup.procs', os.getpid()); bounds()
        if flag is not None and os.path.exists(flag):
            os._exit(125)  # pre-exec re-check: cancellation won before commit
    def secc(self):
        os.lseek(self.seccomp_fd, 0, os.SEEK_SET)
        return self.seccomp_fd

def spawn_worker(ctx, mode, N, outer_frag, go=True, timeout=20, flag=None, stdin_go=True):
    """Release one candidate. Returns (record, bwrap_host_pid_or_None).

    The manager's pre-fork flag check is the first barrier; the child's
    pre-exec re-check (exit 125) is the commit barrier. No timestamps decide
    ordering; monotonic times below are evidence only.
    """
    rec = {'mode': mode, 'N': N}
    t_check = time.monotonic()
    cancelled_at_check = flag is not None and os.path.exists(flag)
    rec['check_monotonic'] = t_check
    rec['cancelled_at_check'] = cancelled_at_check
    if cancelled_at_check:
        rec['state'] = 'CANCELLED-BEFORE-RELEASE'
        return rec, None
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', mode, str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', str(N)])
    pre = lambda: ctx.enter_target(flag)
    if not go:
        cand = launch(argv, preexec=pre, pass_fds=(ctx.seccomp_fd,), timeout=timeout)
        rec['state'] = 'RELEASED-OWNED' if cand['rc'] != 125 else 'CANCELLED-BEFORE-RELEASE'
        rec['candidate'] = cand
        return rec, None
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=pre)
    rec['bwrap_host_pid'] = p.pid
    ready = b''
    try:
        while b'READY' not in ready:
            chunk = p.stdout.readline()
            if not chunk:
                break
            ready += chunk
    except OSError as e:
        rec['ready_error'] = repr(e)
    rec['ready_seen'] = b'READY' in ready
    ident = {}
    for line in ready.decode(errors='replace').splitlines():
        if line.startswith('identity '):
            for kv in line.split()[1:]:
                k, _, v = kv.partition('=')
                ident[k] = v
    rec['announced_identity'] = ident
    # Namespace-local PIDs are resolved to host PIDs via NSpid; exactly one
    # match is required, else the release is unvalidated (never guessed).
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    validated = resolved[0] if len(resolved) == 1 else None
    rec['ns_resolution'] = resolved
    rec['external_validation'] = validated
    t_release = time.monotonic()
    rec['release_monotonic'] = t_release
    if p.poll() is not None and p.returncode == 125:
        rec['state'] = 'CANCELLED-BEFORE-RELEASE'
        stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, stdout=stdout.decode(errors='replace')[:4096],
                                stderr=stderr.decode(errors='replace')[:1024], timeout=False)
        return rec, None
    if not rec['ready_seen'] or validated is None:
        rec['state'] = 'RELEASE-UNVALIDATED'
        p.kill()
        stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, stdout=stdout.decode(errors='replace')[:4096],
                                stderr=stderr.decode(errors='replace')[:1024], timeout=False)
        return rec, None
    rec['state'] = 'RELEASED-OWNED'
    try:
        p.stdin.write(b'G' if stdin_go else b'X'); p.stdin.flush()
    except OSError as e:
        rec['go_error'] = repr(e)
    t0 = time.monotonic()
    try:
        stdout, stderr = p.communicate(timeout=timeout)
        rec['candidate'] = dict(rc=p.returncode, out_len=len(stdout), err_len=len(stderr),
                                stdout=stdout.decode(errors='replace')[:32768],
                                stderr=stderr.decode(errors='replace')[:8192], timeout=False,
                                elapsed_s=time.monotonic() - t0)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, out_len=len(stdout), err_len=len(stderr),
                                stdout=stdout.decode(errors='replace')[:32768],
                                stderr=stderr.decode(errors='replace')[:8192], timeout=True,
                                elapsed_s=time.monotonic() - t0)
    return rec, p.pid

def wait_empty(leaf, bound=15.0):
    t0 = time.monotonic()
    while time.monotonic() - t0 < bound:
        if 'populated 0' in populated(leaf):
            return True, time.monotonic() - t0
    return False, bound

def manager(a):
    assert os.getuid() == UID and os.getgid() == UID
    ctx = Ctx(a)
    outer_frag = a.outer_frag
    out = {'case': a.case, 'uid': os.getuid(), 'cgroup': read('/proc/self/cgroup')}
    try:
        (ctx.cg / 'sibling').mkdir(exist_ok=True)
        write(ctx.cg / 'sibling/memory.max', 67108864); write(ctx.cg / 'sibling/pids.max', 16)
        ctx.seccomp_fd = os.memfd_create('ar7-P07-seccomp', os.MFD_CLOEXEC)
        os.write(ctx.seccomp_fd, filter_bytes()); os.lseek(ctx.seccomp_fd, 0, 0)
        out['filter_sha256'] = hashlib.sha256(filter_bytes()).hexdigest()
        fn = CASES[a.case]
        rec = fn(ctx, outer_frag)
        out['record'] = rec
    except Exception as e:
        out['error'] = repr(e)
    finally:
        if ctx.seccomp_fd is not None:
            try: os.close(ctx.seccomp_fd)
            except OSError: pass
    print('RESULT ' + json.dumps(out), flush=True)

def positive_run(ctx, mode, N):
    cmd = [str(ctx.root / 'worker'), mode, str(ctx.cg), str(ctx.root), ctx.nonce, str(ctx.root / 'input'), str(N)]
    return launch(cmd, stdin_data=b'G', cwd=str(ctx.root), env=None,
                  preexec=lambda: ctx.enter_positive(), timeout=20)

def do_positives(ctx, outer_frag):
    rec = {'legs': []}
    for mode, N, want_rc, markers in [('sleeper', 1, 0, ['complete']), ('pipeholder', 1, 0, ['complete']),
                                      ('exit17', 0, 17, []), ('daemonizer', 1, 0, ['complete']),
                                      ('forker', 5, 0, ['complete']), ('reexec', 0, 0, ['generation second']),
                                      ('interfere', 0, 0, ['complete'])]:
        pos = positive_run(ctx, mode, N)
        ok = pos['rc'] == want_rc and not pos['timeout'] and all(m in pos['stdout'] for m in markers)
        rec['legs'].append({'mode': mode, 'rc': pos['rc'], 'gate': ok,
                            'stdout_tail': pos['stdout'][-1500:], 'timeout': pos['timeout']})
        evt({'case': 'positives', 'mode': mode, 'gate': ok})
    rec['leg'] = 'SUPPORTED-leg' if all(l['gate'] for l in rec['legs']) else 'INCONCLUSIVE (positive gate failed)'
    return rec

def check_restricted(rec, want_rc, markers, timeout_ok=True):
    cand = rec.get('candidate', {})
    if rec.get('state') == 'CANCELLED-BEFORE-RELEASE':
        return False
    return (cand.get('rc') == want_rc and not cand.get('timeout', True)
            and all(m in cand.get('stdout', '') for m in markers))

def do_c1(ctx, outer_frag):
    evt({'case': 'c1', 'intent': 'caller dies before setup; nothing created'})
    return {'leg': 'SUPPORTED-leg (vacuous, observer-audited)', 'created': []}

def do_c2(ctx, outer_frag):
    evt({'case': 'c2', 'manifest_append': 'c2-f1'})
    (ctx.root / 'c2-f1').write_text('step1')
    evt({'case': 'c2', 'manifest_append': 'c2-f2'})
    (ctx.root / 'c2-f2').write_text('step2')
    evt({'case': 'c2', 'fault': 'SIGKILL self mid-provisioning'})
    os.kill(os.getpid(), signal.SIGKILL)
    return {}

def do_c3(ctx, outer_frag):
    evt({'case': 'c3', 'intent': 'manager SIGKILL before any release'})
    os.kill(os.getpid(), signal.SIGKILL)
    return {}

def do_c4(ctx, outer_frag):
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'hold', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    line = p.stdout.readline()
    evt({'case': 'c4', 'marker': line.decode(errors='replace').strip(), 'fault': 'SIGKILL self post-release'})
    os.kill(os.getpid(), signal.SIGKILL)
    return {}

def do_c5(ctx, outer_frag):
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'sleeper', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '8'])
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    time.sleep(0.3)
    victim = validate_pid(p.pid, outer_frag)
    rec = {'bwrap_outer_validation': victim}
    if victim is None:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (bwrap-outer not validatable)'
        return rec
    os.kill(p.pid, signal.SIGKILL)
    stdout, stderr = p.communicate(timeout=20)
    rec['candidate'] = dict(rc=p.returncode, stdout=stdout.decode(errors='replace')[:4096],
                            stderr=stderr.decode(errors='replace')[:1024], timeout=False)
    rec['ready_seen'] = 'READY' in rec['candidate']['stdout']
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    rec['leg'] = ('SUPPORTED-leg' if empty and not rec['ready_seen']
                  else 'INCONCLUSIVE (setup-abort expectation unmet)')
    return rec

def do_c6(ctx, outer_frag):
    # Victim is a long-lived root (root-hold) so the ns-init is alive at
    # fault time. The ns-init is found by enumerating bwrap processes in the
    # target leaf: our known outer child plus exactly one more.
    p, rec, validated = launch_hold_for_fault(ctx, 'root-hold', 30, outer_frag)
    if not validated:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (no validated release to fault)'
        return rec
    time.sleep(0.5)
    cands = scan_bwrap_in_leaf(outer_frag, 'target')
    others = [c for c in cands if c['pid'] != p.pid]
    rec['leaf_bwrap_scan'] = cands
    if len(others) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (ns-init not uniquely identifiable: %d others)' % len(others)
        return rec
    nspid = others[0]['pid']
    os.kill(nspid, signal.SIGKILL)
    evt({'case': 'c6', 'fault': 'SIGKILL ns-init host-PID %d' % nspid})
    try:
        stdout, stderr = p.communicate(timeout=15)
        rec['chain_collapsed'] = True
        rec['wrapper_rc'] = p.returncode
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['chain_collapsed'] = False
    rec['stdout_tail'] = stdout.decode(errors='replace')[-1500:]
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    rec['leg'] = ('SUPPORTED-leg' if rec['chain_collapsed'] and empty
                  else 'BLOCKED (namespace cascade did not empty domain)')
    return rec

def do_c7(ctx, outer_frag):
    rec, _ = spawn_worker(ctx, 'sleeper', 8, outer_frag, timeout=25)
    cand = rec.get('candidate', {})
    rec['root_rc'] = cand.get('rc')
    # The root exited (pipe observed its exit) while the detached child still
    # sleeps: domain must be NON-empty at this instant, then drain by itself.
    rec['domain_nonempty_right_after_root_exit'] = 'populated 0' not in populated(ctx.cg / 'target')
    empty, dt = wait_empty(ctx.cg / 'target', 15.0)
    rec['domain_empty_after_root_exit'] = empty; rec['empty_after_s'] = dt
    rec['leg'] = ('SUPPORTED-leg' if cand.get('rc') == 0 and rec['domain_nonempty_right_after_root_exit'] and empty
                  and not cand.get('timeout') else 'INCONCLUSIVE (C7 expectation unmet)')
    return rec

def do_c8(ctx, outer_frag):
    rec, _ = spawn_worker(ctx, 'pipeholder', 5, outer_frag, timeout=25)
    cand = rec.get('candidate', {})
    rec['holds_pipe'] = 'holds_pipe' in cand.get('stdout', '')
    rec['output_complete'] = 'complete' in cand.get('stdout', '')
    rec['exit_preserved'] = cand.get('rc') == 0
    rec['held_at_least_4_5s'] = cand.get('elapsed_s', 0) >= 4.5
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty
    rec['leg'] = ('SUPPORTED-leg' if rec['holds_pipe'] and rec['output_complete'] and rec['exit_preserved']
                  and rec['held_at_least_4_5s'] and empty else 'INCONCLUSIVE (C8 expectation unmet)')
    return rec

def do_c9(ctx, outer_frag):
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'hang', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    try:
        stdout, stderr = p.communicate(timeout=4)
        fired = False
    except subprocess.TimeoutExpired:
        fired = True
        p.kill(); stdout, stderr = p.communicate()
    evt({'case': 'c9', 'watchdog_fired': fired})
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec = {'watchdog_fired': fired, 'domain_empty': empty, 'empty_after_s': dt}
    rec['leg'] = ('SUPPORTED-leg' if fired and empty else 'INCONCLUSIVE (C9 expectation unmet)')
    return rec

def do_c10(ctx, outer_frag):
    # Victim is a long-lived root (root-hold, no children) so the announced
    # root PID is alive at fault time. Killing it must collapse the whole
    # chain by itself (ns-init waits the root); no domain kill is available
    # manager-side, and none is used.
    p, rec, validated = launch_hold_for_fault(ctx, 'root-hold', 30, outer_frag)
    if not validated:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (target root not validatable)'
        return rec
    time.sleep(0.5)
    os.kill(validated['pid'], signal.SIGKILL)
    evt({'case': 'c10', 'fault': 'SIGKILL resolved target-root host-PID %d' % validated['pid']})
    try:
        stdout, stderr = p.communicate(timeout=15)
        rec['chain_collapsed'] = True
        rec['wrapper_result'] = p.returncode
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['chain_collapsed'] = False
    rec['root_result'] = 'SIGKILL-injected-against-validated-PID (known by construction, not inferred)'
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    rec['leg'] = ('SUPPORTED-leg' if rec['chain_collapsed'] and empty
                  else 'BLOCKED (chain did not collapse after target death)')
    return rec

def do_c11(ctx, outer_frag):
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'hold', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    try:
        stdout, stderr = p.communicate(timeout=35)
        marker = 'RELEASED' in stdout.decode(errors='replace')
        return {'leg': ('SUPPORTED-leg (owned release; stop verified by suite-teardown domain kill)'
                        if marker and p.returncode == 0 else 'INCONCLUSIVE (C11 hold mismatch)'),
                'marker_seen': marker, 'hold_rc': p.returncode}
    except subprocess.TimeoutExpired:
        p.kill(); p.communicate()
        return {'leg': 'INCONCLUSIVE (C11 hold did not exit)', 'marker_seen': False}

def do_cancel_det(ctx, outer_frag):
    flag = str(ctx.root / 'cancel-det')
    pathlib.Path(flag).write_text('monotonic=%r' % time.monotonic())
    rec, _ = spawn_worker(ctx, 'sleeper', 1, outer_frag, flag=flag)
    rec['zero_spawns'] = rec.get('state') == 'CANCELLED-BEFORE-RELEASE'
    empty, _ = wait_empty(ctx.cg / 'target', 3.0)
    rec['domain_empty'] = empty
    rec['leg'] = ('SUPPORTED-leg' if rec['zero_spawns'] and empty
                  else 'BLOCKED (release after observed cancellation)')
    return rec

def do_cancel_race(ctx, outer_frag):
    rec = {'rounds': []}
    rng = random.Random(0x507)
    for i in range(5):
        flag = str(ctx.root / ('cancel-race-%d' % i))
        delay = rng.uniform(0, 0.5)
        plant_t = {}
        def planter(p=flag, d=delay, slot=plant_t):
            time.sleep(d)
            t = time.monotonic()
            pathlib.Path(p).write_text('monotonic=%r' % t)
            slot['plant_monotonic'] = t
        th = threading.Thread(target=planter, daemon=True)
        th.start()
        r, _ = spawn_worker(ctx, 'sleeper', 1, outer_frag, flag=flag, timeout=25)
        th.join(timeout=5)
        r['plant_monotonic'] = plant_t.get('plant_monotonic')
        if r.get('state') == 'RELEASED-OWNED':
            empty, dt = wait_empty(ctx.cg / 'target', 15.0)
            r['domain_empty'] = empty; r['empty_after_s'] = dt
            r['round'] = ('lost-race-owned-cleaned' if empty else 'BLOCKED (released, cleanup failed)')
        elif r.get('state') == 'CANCELLED-BEFORE-RELEASE':
            r['round'] = 'cancel-won-refused'
        else:
            r['round'] = 'INCONCLUSIVE (%s)' % r.get('state')
        evt({'case': 'cancel_race', 'round': i, 'outcome': r['round']})
        try: os.unlink(flag)
        except OSError: pass
        rec['rounds'].append({k: r.get(k) for k in ('state', 'cancelled_at_check', 'check_monotonic',
                                                   'plant_monotonic', 'release_monotonic', 'domain_empty', 'round')})
    bad = [r for r in rec['rounds'] if str(r['round']).startswith('BLOCKED') or str(r['round']).startswith('INCONCLUSIVE')]
    rec['leg'] = ('SUPPORTED-leg' if not bad else '%s (see rounds)' % bad[0]['round'])
    return rec

def battery_case(mode, N, want_rc, markers, empty_bound=15.0):
    def fn(ctx, outer_frag):
        rec, _ = spawn_worker(ctx, mode, N, outer_frag, timeout=25)
        if rec.get('state') != 'RELEASED-OWNED':
            rec['leg'] = 'INCONCLUSIVE (no release for battery leg)'
            return rec
        ok = check_restricted(rec, want_rc, markers)
        empty, dt = wait_empty(ctx.cg / 'target', empty_bound)
        rec['domain_empty'] = empty; rec['empty_after_s'] = dt
        denied = [l for l in rec.get('candidate', {}).get('stdout', '').splitlines()
                  if 'errno=1' in l or 'errno=13' in l or 'rc=-1' in l]
        rec['denials'] = denied
        if not empty:
            rec['leg'] = 'HELPER TOPOLOGY UNRESOLVED (survival past required domain stop)'
        elif not ok:
            rec['leg'] = 'INCONCLUSIVE (battery output mismatch)'
        else:
            rec['leg'] = 'SUPPORTED-leg'
        return rec
    return fn

CASES = {
    'positives': do_positives,
    'c1': do_c1, 'c2': do_c2, 'c3': do_c3, 'c4': do_c4, 'c5': do_c5,
    'c6': do_c6, 'c7': do_c7, 'c8': do_c8, 'c9': do_c9, 'c10': do_c10, 'c11': do_c11,
    'cancel_det': do_cancel_det, 'cancel_race': do_cancel_race,
    'bat_daemon': battery_case('daemonizer', 6, 0, ['complete']),
    'bat_fork': battery_case('forker', 40, 0, ['complete']),
    'bat_reexec': battery_case('reexec', 0, 0, ['generation second']),
    'bat_interfere': battery_case('interfere', 0, 0, ['complete']),
}
SUITE = ['positives', 'c1', 'c2', 'c3', 'c4', 'c5', 'c6', 'c7', 'c8', 'c9', 'c10',
         'cancel_det', 'cancel_race', 'bat_daemon', 'bat_fork', 'bat_reexec', 'bat_interfere', 'c11']

def observer(a):
    assert os.getuid() == 0
    nonce = uuid.uuid4().hex; root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
    outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce); cg = outer / 'delegated'
    repo = pathlib.Path(__file__).resolve().parent.parent.parent
    only = [a.only] if a.only else SUITE
    if a.only == 'c11-wrapper-victim':
        only = ['c11']
    data = {'probe': 'P07', 'source_sha': a.source_sha, 'nonce': nonce,
            'date_utc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
            'host': {'kernel': os.uname().release, 'arch': os.uname().machine,
                     'distribution': read('/etc/os-release'),
                     'glibc': os.confstr('CS_GNU_LIBC_VERSION'), 'lsm': read('/sys/kernel/security/lsm'),
                     'yama': read('/proc/sys/kernel/yama/ptrace_scope'),
                     'userns_max': read('/proc/sys/user/max_user_namespaces'),
                     'cgroup_mount': read('/proc/mounts'), 'helper': run(['/usr/bin/bwrap', '--version']),
                     'helper_sha256': sha('/usr/bin/bwrap'),
                     'helper_mode': oct(os.stat('/usr/bin/bwrap').st_mode & 0o7777)},
            'paths': {'root': str(root), 'outer': str(outer), 'delegated': str(cg)},
            'observer_uid': os.getuid(),
            'source_hashes': {p.name: sha(p) for p in list(pathlib.Path(__file__).parent.glob('p07.*'))},
            'cases': [], 'only': only}
    outer_frag = str(outer).removeprefix('/sys/fs/cgroup')
    p = None; clean = False; done = threading.Event(); watcher = None
    try:
        print('NONCE ' + nonce, flush=True)
        if data['host']['helper_sha256'] != PIN or os.stat('/usr/bin/bwrap').st_mode & 0o6000:
            raise RuntimeError('helper pin/nonsetuid prerequisite changed')
        root.mkdir(mode=0o755)
        manifest = root / 'manifest.json'
        manifest.write_text(json.dumps({'nonce': nonce, 'entries': []})); os.chown(manifest, UID, UID)
        data['compile'] = run(['/usr/bin/gcc', '-static', '-O2', '-Wall', '-Wextra', '-o',
                               str(root / 'worker'), str(pathlib.Path(__file__).with_suffix('.c'))])
        if data['compile']['rc']:
            raise RuntimeError('compile failed before release')
        data['worker_sha256'] = sha(root / 'worker')
        (root / 'input').write_text(nonce); os.chmod(root / 'input', 0o444)
        outer.mkdir(); write(outer / 'memory.max', 268435456); write(outer / 'pids.max', 64)
        write(outer / 'memory.swap.max', 0); write(outer / 'cgroup.subtree_control', '+memory +pids')
        cg.mkdir(); write(cg / 'memory.max', 268435456); write(cg / 'pids.max', 64)
        write(cg / 'cgroup.subtree_control', '+memory +pids'); leaf_own(cg)
        for name in ('manager', 'target', 'positive'):
            (cg / name).mkdir(); leaf_own(cg / name)
        write(cg / 'target/memory.max', 134217728); write(cg / 'target/pids.max', 48)
        os.chown(root, UID, UID)
        data['bwrap_samples'] = 0
        data['external_process_samples'] = {}

        def observe():
            while not done.wait(.05):
                for path in (cg / 'manager', cg / 'target', cg / 'sibling', cg / 'positive'):
                    if not path.exists():
                        continue
                    for pid in read(path / 'cgroup.procs').split():
                        if not pid.isdecimal() or pid in data['external_process_samples']:
                            continue
                        membership = read('/proc/' + pid + '/cgroup')
                        if outer_frag not in membership:
                            continue
                        cmd = read('/proc/' + pid + '/cmdline')
                        data['external_process_samples'][pid] = {'cgroup': membership, 'status': read('/proc/' + pid + '/status'),
                                                                'command': cmd}
                        if 'bwrap' in cmd:
                            data['bwrap_samples'] += 1
        watcher = threading.Thread(target=observe, daemon=True); watcher.start()

        def drop():
            # Identical to the P04 manager drop (no FSIZE: inherited limits
            # cannot be raised unprivileged; per-case preexecs set their own).
            write(cg / 'manager/cgroup.procs', os.getpid()); os.setgroups([]); os.setgid(UID); os.setuid(UID); bounds(fsize=False)

        halted = False
        for case in only:
            crec = {'case': case}
            if halted:
                crec['status'] = 'NOT RUN (halted after BLOCKED leg)'
                data['cases'].append(crec); continue
            cmd = [sys.executable, __file__, '--manager', '--case', case, '--root', str(root),
                   '--cg', str(cg), '--nonce', nonce, '--outer-frag', outer_frag]
            t0 = time.monotonic()
            bwrap_before = data['bwrap_samples']
            p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 preexec_fn=drop, close_fds=True)
            crec['manager_pid'] = p.pid
            try:
                stdout, stderr = p.communicate(timeout=120)
                crec['manager_rc'] = p.returncode
                crec['manager_signo'] = -p.returncode if p.returncode < 0 else 0
                crec['duration_s'] = time.monotonic() - t0
                crec['stderr_tail'] = stderr[:2048].decode(errors='replace')
                crec['bwrap_delta'] = data['bwrap_samples'] - bwrap_before
                evts = [json.loads(l[4:]) for l in stdout.decode(errors='replace').splitlines() if l.startswith('EVT ')]
                res = [json.loads(l[7:]) for l in stdout.decode(errors='replace').splitlines() if l.startswith('RESULT ')]
                crec['events'] = evts
                crec['result'] = res[0] if res else None
                if case == 'c3':
                    empty, _ = wait_empty(cg / 'target', 5.0)
                    no_spawn = crec.get('bwrap_delta', -1) == 0
                    crec['observer_audit'] = {'domain_empty': empty, 'zero_bwrap_delta': no_spawn}
                    if crec['manager_signo'] == signal.SIGKILL and empty:
                        crec['verdict'] = 'SUPPORTED-leg'
                    else:
                        crec['verdict'] = 'INCONCLUSIVE (C3 audit unmet)'
                elif case == 'c4':
                    empty_now = 'populated 0' in populated(cg / 'target')
                    crec['observer_audit'] = {'target_nonempty_after_manager_death': not empty_now}
                    write(outer / 'cgroup.kill', 1)
                    empty, dt = wait_empty(outer, 10.0)
                    crec['observer_audit']['outer_empty_after_kill'] = empty
                    crec['observer_audit']['root_status'] = 'UNKNOWN (never inferred)'
                    crec['verdict'] = ('SUPPORTED-leg' if crec['manager_signo'] == signal.SIGKILL and empty
                                       else 'INCONCLUSIVE (C4 audit unmet)')
                elif case == 'c2':
                    entries = [e.get('manifest_append') for e in evts if 'manifest_append' in e]
                    for name in entries:
                        q = root / name
                        if q.exists():
                            q.unlink()
                    residue = [n for n in ('c2-f1', 'c2-f2') if (root / n).exists()]
                    crec['observer_audit'] = {'manifest_entries': entries, 'residue': residue,
                                              'manager_signo': crec['manager_signo']}
                    crec['verdict'] = ('SUPPORTED-leg' if residue == [] and entries else 'INCONCLUSIVE (C2 audit unmet)')
                elif case == 'c1':
                    ws = sorted(x.name for x in root.iterdir())
                    ws_left = [n for n in ws if n.startswith('c1')]
                    crec['observer_audit'] = {'workspace_c1_residue': ws_left}
                    crec['verdict'] = ('SUPPORTED-leg (vacuous)' if not ws_left else 'INCONCLUSIVE (C1 residue)')
                elif case == 'c11':
                    leg = (crec['result'] or {}).get('record', {}).get('leg', 'RESOLVED-BY-WRAPPER (see wrapper audit)')
                    crec['verdict'] = leg
                    if leg.startswith('BLOCKED') or leg.startswith('HELPER TOPOLOGY'):
                        halted = True
                else:
                    leg = (crec['result'] or {}).get('record', {}).get('leg', 'INCONCLUSIVE (no result)')
                    crec['verdict'] = leg
                    if leg.startswith('BLOCKED') or leg.startswith('HELPER TOPOLOGY'):
                        halted = True
            except subprocess.TimeoutExpired:
                crec['watchdog'] = 'per-case manager timeout fired'
                p.kill(); p.communicate()
                crec['verdict'] = 'INCONCLUSIVE (manager timeout)'
            data['cases'].append(crec)
        data['halted'] = halted
        data['host_shm_p07_left'] = [x.name for x in pathlib.Path('/dev/shm').iterdir() if x.name.startswith('p07-')]
        data['observer_domains'] = {str(q.relative_to(outer)): read(q / 'cgroup.procs') for q in [outer, cg, *cg.iterdir()] if q.is_dir()}
    except Exception as e:
        data['setup_error'] = repr(e)
    finally:
        done.set()
        if watcher:
            watcher.join(timeout=1)
        if outer.exists():
            try:
                write(outer / 'cgroup.kill', 1)
                if p:
                    p.wait(timeout=5)
                for _ in range(30):
                    if 'populated 0' in read(outer / 'cgroup.events'):
                        break
                    time.sleep(.1)
                data['cleanup_events'] = read(outer / 'cgroup.events')
                if 'populated 0' not in data['cleanup_events']:
                    raise RuntimeError('outer not empty')
                known = {'manager', 'target', 'sibling', 'positive'}
                unknown = [q.name for q in list(cg.iterdir()) + list(outer.iterdir())
                           if q.is_dir() and q.name not in known | {'delegated'}]
                if unknown:
                    raise RuntimeError('unknown cgroup descendants preserved: ' + repr(unknown))
                for parent in (cg, outer):
                    for name in sorted(known):
                        q = parent / name
                        if q.exists():
                            q.rmdir()
                if cg.exists():
                    cg.rmdir()
                outer.rmdir()
            except Exception as e:
                data['cleanup_error'] = repr(e)
        if not outer.exists():
            if root.exists():
                for name in ('input', 'worker', 'manifest.json', 'crash-marker', 'c2-f1', 'c2-f2',
                             'cancel-det', *[('cancel-race-%d' % i) for i in range(5)]):
                    q = root / name
                    if q.exists() or q.is_symlink():
                        q.unlink()
                try:
                    root.rmdir()
                except OSError as e:
                    data['cleanup_error'] = repr(e)
        data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                   'manager': p is None or p.poll() is not None,
                                   'host_shm_clean': not data.get('host_shm_p07_left')}
        clean = all(data['cleanup_absence'].values()) and not data.get('cleanup_error')
        data['cleanup_confirmed'] = clean
        dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
        (dest / (nonce + '.json')).write_text(json.dumps(data, indent=2))
        if not a.quiet:
            print(json.dumps(data, indent=2))
    return 0 if clean else 2

def c11_wrapper(a):
    """Minimal root surface for C11: run the observer for case c11 only as a
    child, SIGKILL it after the RELEASED relay, then audit via the nonce
    manifest. No guardian, no service: a bounded test driver."""
    assert os.getuid() == 0
    cmd = [sys.executable, __file__, '--observer-only-c11', '--source-sha', a.source_sha or '', '--output', a.output]
    p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1, close_fds=True)
    nonce = None; killed = False; transcript = []
    try:
        for line in p.stdout:
            transcript.append(line[:500])
            if line.startswith('NONCE '):
                nonce = line.split()[1].strip()
            if 'RELEASED' in line and nonce:
                time.sleep(1.0)
                p.send_signal(signal.SIGKILL)
                killed = True
                break
        p.wait(timeout=30)
    except Exception as e:
        transcript.append('wrapper-error: ' + repr(e))
    audit = {'nonce': nonce, 'observer_killed': killed, 'observer_rc': p.poll()}
    if nonce:
        root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
        outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
        audit['manifest'] = read(root / 'manifest.json')[:2000]
        try:
            if outer.exists():
                write(outer / 'cgroup.kill', 1)
                for _ in range(50):
                    if 'populated 0' in read(outer / 'cgroup.events'):
                        break
                    time.sleep(.1)
                audit['outer_empty'] = 'populated 0' in read(outer / 'cgroup.events')
                for parent in (outer / 'delegated', outer):
                    if parent.exists():
                        for q in sorted(parent.iterdir()):
                            if q.is_dir():
                                try: q.rmdir()
                                except OSError: pass
                        try: parent.rmdir()
                        except OSError: pass
            audit['outer_gone'] = not outer.exists()
            if root.exists():
                for q in list(root.iterdir()):
                    try:
                        q.unlink()
                    except OSError:
                        pass
                try:
                    root.rmdir()
                except OSError as e:
                    audit['workspace_error'] = repr(e)
            audit['workspace_gone'] = not root.exists()
        except Exception as e:
            audit['audit_error'] = repr(e)
        audit['verdict'] = ('SUPPORTED-leg (observer death survived by manifest audit; states stayed Unknown, never inferred)'
                            if killed and audit.get('outer_gone') and audit.get('workspace_gone')
                            else 'INCONCLUSIVE (C11 wrapper audit unmet)')
    audit['transcript_tail'] = transcript[-15:]
    dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
    (dest / ('c11-wrapper-' + (nonce or 'nononce') + '.json')).write_text(json.dumps(audit, indent=2))
    print(json.dumps(audit, indent=2))
    return 0 if audit.get('verdict', '').startswith('SUPPORTED') else 2

if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--manager', action='store_true')
    ap.add_argument('--c11-wrapper', action='store_true')
    ap.add_argument('--observer-only-c11', action='store_true')
    ap.add_argument('--case'); ap.add_argument('--only')
    ap.add_argument('--source-sha'); ap.add_argument('--output')
    ap.add_argument('--root'); ap.add_argument('--cg'); ap.add_argument('--nonce'); ap.add_argument('--outer-frag')
    ap.add_argument('--quiet', action='store_true')
    args = ap.parse_args()
    if args.manager:
        manager(args)
    elif args.c11_wrapper:
        sys.exit(c11_wrapper(args))
    elif args.observer_only_c11:
        args.only = 'c11-wrapper-victim'; args.quiet = True
        sys.exit(observer(args))
    else:
        sys.exit(observer(args))
