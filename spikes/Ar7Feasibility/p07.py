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
import select
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

def revalidate_pid(pid, outer_frag, leaf_frag=None, cmd_frag=None):
    """Fresh single-PID identity read: cgroup membership + starttime +
    cmdline. Returns the record or None if absent/unreadable. Never acts."""
    try:
        cg = pathlib.Path('/proc/%d/cgroup' % pid).read_text()
        cmd = pathlib.Path('/proc/%d/cmdline' % pid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
    except OSError:
        return None
    if outer_frag not in cg:
        return None
    if leaf_frag is not None and leaf_frag not in cg:
        return None
    if cmd_frag is not None and cmd_frag not in cmd:
        return None
    st = starttime_of(pid)
    if st is None:
        return None
    return {'pid': pid, 'starttime': st, 'cmdline': cmd[:160]}

def signal_validated(pid, expected, signo, outer_frag, leaf_frag=None, cmd_frag=None):
    """R6 safeguard: re-read identity immediately before acting. Signal ONLY
    on exact pid+starttime match (+ optional leaf/cmd re-check). On absence
    or any mismatch, refuse and record — never touch the numeric PID."""
    fresh = revalidate_pid(pid, outer_frag, leaf_frag, cmd_frag)
    if fresh is None:
        return {'action': 'refused-absent', 'pid': pid}
    if fresh['starttime'] != expected.get('starttime'):
        return {'action': 'refused-mismatch', 'pid': pid,
                'expected_starttime': expected.get('starttime'),
                'observed_starttime': fresh['starttime']}
    os.kill(pid, signo)
    return {'action': 'signaled', 'pid': pid, 'signo': signo,
            'starttime': fresh['starttime']}

def scan_leaf_targets(outer_frag, leaf_frag='delegated/target'):
    """All /target-cmdline processes in the fixture leaf with innermost NSpid
    and state. Root has innermost ns-pid 2; descendants 3+."""
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
        if outer_frag not in cg or leaf_frag not in cg:
            continue
        if '/target' not in cmd:
            continue
        nspid, state = None, None
        for line in st.splitlines():
            if line.startswith('NSpid:'):
                nspid = line.split()[-1]
            if line.startswith('State:'):
                state = line.split(':', 1)[1].strip()
        found.append({'pid': pid, 'ns_pid': nspid, 'state': state,
                      'starttime': starttime_of(pid), 'cmdline': cmd[:120]})
    return sorted(found, key=lambda d: d['pid'])

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
    rec = {'ready_seen': b'READY' in ready, 'announced_identity': ident}
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
    # Kill bwrap-outer at the earliest instant (own unreaped child: parenthood
    # is kernel-tracked, no stale-PID risk, no validation needed). Two honest
    # branches: pre-READY (setup aborted, the designed case) or post-READY
    # (outer death with a live but UNRELEASED namespace — no GO was ever
    # written, so the commit barrier never passed; drain via our own pipe).
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'sleeper', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '8'])
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    os.kill(p.pid, signal.SIGKILL)
    evt({'case': 'c5', 'fault': 'SIGKILL bwrap-outer immediately post-spawn'})
    try:
        p.stdin.close()
    except OSError:
        pass
    try:
        stdout, stderr = p.communicate(timeout=20)
        drained = True
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        drained = False
    out = stdout.decode(errors='replace')
    ready = 'READY' in out
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec = {'fault_landed_pre_ready': not ready, 'drained_via_owned_pipe': drained,
           'domain_empty': empty, 'empty_after_s': dt,
           'stdout_tail': out[-1500:], 'wrapper_rc': p.returncode}
    if not drained or not empty:
        rec['leg'] = 'INCONCLUSIVE (C5 drain unmet; needs review)'
    elif not ready:
        rec['leg'] = 'SUPPORTED-leg (setup aborted pre-READY; no target; domain empty)'
    else:
        rec['leg'] = ('SUPPORTED-leg (outer death post-setup; target never passed the commit barrier '
                      '(no GO); drained via owned pipe; no unowned survivor; domain empty)')
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
    sig = signal_validated(nspid, others[0], signal.SIGKILL, outer_frag, 'target', 'bwrap')
    rec['ns_init_signal'] = sig
    evt({'case': 'c6', 'fault': 'SIGKILL ns-init host-PID %d' % nspid, 'guard': sig['action']})
    if sig['action'] != 'signaled':
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (ns-init identity guard refused)'
        return rec
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
    # Corrected expectation (see evidence): the design anticipated a
    # non-empty domain after root exit, but the PID-namespace init-death
    # cascade (design Section 2, item 2) kills the sleeping child the instant
    # the root exits and ns-init reaps it. Instant-empty is the mechanism
    # proving itself — strictly stronger than the designed expectation: no
    # survivor is constructible in this topology.
    rec, _ = spawn_worker(ctx, 'sleeper', 8, outer_frag, timeout=25)
    cand = rec.get('candidate', {})
    rec['root_rc'] = cand.get('rc')
    empty, dt = wait_empty(ctx.cg / 'target', 15.0)
    rec['domain_empty_after_root_exit'] = empty; rec['empty_after_s'] = dt
    rec['cascade_emptied_immediately'] = empty and dt < 1.0
    rec['leg'] = ('SUPPORTED-leg (root rc 0 known; init-death cascade emptied domain immediately; '
                  'no survivor; design non-empty expectation refuted by the mechanism)'
                  if cand.get('rc') == 0 and not cand.get('timeout') and rec['cascade_emptied_immediately']
                  else 'INCONCLUSIVE (C7 expectation unmet)')
    return rec

def do_c8(ctx, outer_frag):
    # Corrected expectation (see evidence): a 5 s pipe hold past root exit is
    # unconstructible — the init-death cascade kills the holder with the
    # namespace, so the pipe closes at root exit. Verdict rests on output
    # completeness + preserved exit code + empty domain; elapsed time is
    # recorded as cascade evidence, not as a hold requirement.
    rec, _ = spawn_worker(ctx, 'pipeholder', 5, outer_frag, timeout=25)
    cand = rec.get('candidate', {})
    rec['holds_pipe'] = 'holds_pipe' in cand.get('stdout', '')
    rec['output_complete'] = 'complete' in cand.get('stdout', '')
    rec['exit_preserved'] = cand.get('rc') == 0
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty
    rec['leg'] = ('SUPPORTED-leg (output complete; exit code preserved; cascade closed pipe at root exit; '
                  'domain empty)' if rec['holds_pipe'] and rec['output_complete'] and rec['exit_preserved'] and empty
                  else 'INCONCLUSIVE (C8 expectation unmet)')
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
    sig = signal_validated(validated['pid'], validated, signal.SIGKILL, outer_frag, 'target', '/target')
    rec['target_signal'] = sig
    evt({'case': 'c10', 'fault': 'SIGKILL resolved target-root host-PID %d' % validated['pid'],
         'guard': sig['action']})
    if sig['action'] != 'signaled':
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (target identity guard refused)'
        return rec
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

def is_in_ns_desc(t):
    try:
        v = int(t.get('ns_pid') or 'x')
        return 3 <= v < 100000
    except ValueError:
        return False

def leaf_pids(cg, outer_frag, exclude=()):
    """Every fixture-domain process across the four leaves (for stray scans).
    Callers exclude themselves. Includes starttime + state for liveness."""
    found = []
    for leaf in ('manager', 'target', 'sibling', 'positive'):
        lp = cg / leaf
        if not lp.exists():
            continue
        for pid in read(lp / 'cgroup.procs').split():
            if not pid.isdecimal() or int(pid) in exclude:
                continue
            try:
                member = pathlib.Path('/proc/%s/cgroup' % pid).read_text()
                cmd = pathlib.Path('/proc/%s/cmdline' % pid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
                st = pathlib.Path('/proc/%s/status' % pid).read_text()
            except OSError:
                continue
            if outer_frag not in member:
                continue
            state = None
            for line in st.splitlines():
                if line.startswith('State:'):
                    state = line.split(':', 1)[1].strip()
            found.append({'pid': int(pid), 'leaf': leaf, 'cmdline': cmd[:120],
                          'starttime': starttime_of(int(pid)), 'state': state})
    return sorted(found, key=lambda d: d['pid'])

def spawn_release(ctx, mode, N, outer_frag):
    """Shared release leg for the live-stop cases: spawn + READY + unique
    NSpid resolution + GO. Returns (Popen, record). Caller owns fault/wait."""
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
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    rec = {'ready_seen': b'READY' in ready, 'announced_identity': ident,
           'ns_resolution': resolved, 'bwrap_host_pid': p.pid}
    if len(resolved) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (live-stop root unresolvable)'
        return None, rec
    try:
        p.stdin.write(b'G'); p.stdin.flush()
    except OSError:
        pass
    rec['state'] = 'RELEASED-OWNED'
    return p, rec

def do_positives_r(ctx, outer_frag):
    rec = {'legs': []}
    for mode, N, want_rc, markers in [('trickle', 6, 0, ['trickle_done', 'complete']),
                                      ('holdchild', 2, 0, ['root_holding', 'complete'])]:
        pos = positive_run(ctx, mode, N)
        ok = pos['rc'] == want_rc and not pos['timeout'] and all(m in pos['stdout'] for m in markers)
        rec['legs'].append({'mode': mode, 'rc': pos['rc'], 'gate': ok,
                            'stdout_tail': pos['stdout'][-1500:], 'timeout': pos['timeout']})
        evt({'case': 'positives-r', 'mode': mode, 'gate': ok})
    rec['leg'] = 'SUPPORTED-leg' if all(l['gate'] for l in rec['legs']) else 'INCONCLUSIVE (positive gate failed)'
    return rec

def do_r1worker(ctx, outer_frag):
    # R1b releaser: owned release of a long-lived root; SPAWNED (with NSpid
    # validation) goes to manager stdout, which the provisioner redirects to
    # a capture file — so it survives the provisioner's death.
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'hold', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    # NOTE (m1): the worker preamble (identity/input lines) precedes RELEASED;
    # the marker is awaited by draining, never by assuming the first line.
    marker = False; ident = {}
    for _ in range(10):
        line = p.stdout.readline()
        if not line:
            break
        if b'RELEASED' in line:
            marker = True
            break
        if line.startswith(b'identity '):
            for kv in line.decode(errors='replace').split()[1:]:
                k, _, v = kv.partition('=')
                ident[k] = v
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    validated = resolved[0] if len(resolved) == 1 else None
    evt({'r1': 'SPAWNED', 'marker': marker, 'validation': validated})
    try:
        stdout, stderr = p.communicate(timeout=25)
        rec = {'marker_seen': marker, 'ns_resolution': resolved, 'external_validation': validated,
               'hold_rc': p.returncode}
    except subprocess.TimeoutExpired:
        p.kill(); p.communicate()
        rec = {'marker_seen': marker, 'ns_resolution': resolved, 'external_validation': validated,
               'hold_rc': 'timeout-killed-by-manager'}
    rec['leg'] = ('RELEASED-OWNED (caller-independent; see runner audit)'
                  if marker and validated else 'INCONCLUSIVE (R1 release unvalidated)')
    return rec

def do_r2deadline(ctx, outer_frag):
    # Genuine pre-target setup deadline: STOP-freeze our own unreaped child at
    # the earliest instant, prove phase (outer present, no /target process,
    # zero target bytes), then decide. Up to 3 attempts; every attempt fully
    # recorded. No GO is ever written on any path.
    rec = {'attempts': []}
    for attempt in (1, 2, 3):
        argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'root-hold', str(ctx.cg),
                              str(ctx.root), ctx.nonce, '/input', '30'])
        p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                             close_fds=True, pass_fds=(ctx.seccomp_fd,),
                             preexec_fn=lambda: ctx.enter_target(None))
        os.kill(p.pid, signal.SIGSTOP)  # own unreaped child: parenthood kernel-tracked
        time.sleep(0.05)
        pre = scan_leaf_targets(outer_frag)
        target_present = any(t['cmdline'].startswith('/target ') or t['cmdline'].startswith('/target\x00')
                             or t['cmdline'].split(' ')[0] == '/target' for t in pre)
        os.kill(p.pid, signal.SIGKILL)  # deadline decision at the setup barrier
        try:
            p.stdin.close()
        except OSError:
            pass
        try:
            stdout, stderr = p.communicate(timeout=20)
            drained = True
        except subprocess.TimeoutExpired:
            p.kill(); stdout, stderr = p.communicate()
            drained = False
        out = stdout.decode(errors='replace')
        att = {'attempt': attempt, 'outer_host_pid': p.pid, 'outer_frozen_first': True,
               'target_process_seen_pre_decision': target_present,
               'target_bytes_in_pipe': b'READY' in stdout or b'identity' in stdout,
               'drained_via_owned_pipe': drained, 'wrapper_rc': p.returncode}
        rec['attempts'].append(att)
        evt({'case': 'r2deadline', 'attempt': attempt, 'pre_target': not target_present})
        if not target_present and not att['target_bytes_in_pipe']:
            empty, dt = wait_empty(ctx.cg / 'target', 10.0)
            att['domain_empty'] = empty; att['empty_after_s'] = dt
            if drained and empty:
                rec['leg'] = ('SUPPORTED-leg (deadline won during setup on attempt %d; '
                              'target never executed; no release; domain empty)' % attempt)
                return rec
            rec['leg'] = 'INCONCLUSIVE (R2 drain unmet)'
            return rec
        empty, dt = wait_empty(ctx.cg / 'target', 10.0)
        att['domain_empty'] = empty; att['branch'] = 'post-exec race lost; drained; not counted as R2'
    rec['leg'] = 'INCONCLUSIVE (could not land a pre-target deadline in 3 attempts)'
    return rec

def do_r3a(ctx, outer_frag):
    # R3 part 1: release root + descendant, prove the descendant alive twice
    # over a 1 s gap, then EXIT with the workload running (no wait, no reap).
    # The observer issues the whole-domain stop afterwards.
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'holdchild', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '30'])
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
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    rec = {'ready_seen': b'READY' in ready, 'announced_identity': ident, 'ns_resolution': resolved}
    if len(resolved) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (R3 root unresolvable)'
        return rec
    try:
        p.stdin.write(b'G'); p.stdin.flush()
    except OSError:
        pass
    time.sleep(1.0)
    snap1 = scan_leaf_targets(outer_frag)
    time.sleep(1.0)
    snap2 = scan_leaf_targets(outer_frag)
    d1 = [t for t in snap1 if is_in_ns_desc(t)]
    d2 = [t for t in snap2 if is_in_ns_desc(t)]
    rec['descendants_t0'] = d1; rec['descendants_t1'] = d2
    alive = [t for t in d2 if t['state'] and t['state'][0] in 'RSD'
             and any(o['pid'] == t['pid'] and o['starttime'] == t['starttime'] for o in d1)]
    rec['descendant_alive_twice'] = alive
    rec['alive_claim'] = len(alive) >= 1
    rec['bwrap_host_pid'] = p.pid
    rec['leg'] = ('RELEASED-OWNED-DESCENDANT-ALIVE (workload left running; domain stop pending; '
                  'see observer_stop)' if rec['alive_claim']
                  else 'INCONCLUSIVE (no live descendant provable)')
    return rec

def do_r3b(ctx, outer_frag):
    # R3 part 2 (runs after the observer's target-leaf kill): verify empty +
    # no strays anywhere in the domain.
    me = os.getpid()
    empty, dt = wait_empty(ctx.cg / 'target', 5.0)
    strays = leaf_pids(ctx.cg, outer_frag, exclude=(me,))
    rec = {'domain_empty': empty, 'empty_after_s': dt, 'strays': strays}
    if not empty:
        rec['leg'] = 'BLOCKED (target domain survived the whole-domain stop)'
    elif strays:
        rec['leg'] = 'INCONCLUSIVE (stray fixture processes outside expectation)'
    else:
        rec['leg'] = 'SUPPORTED-leg (post-stop verification: empty, no strays)'
    return rec

def do_r4a(ctx, outer_frag):
    # R4 part 1: release the trickle spawner, prove spawning actively in
    # progress (streamed count >= 8 + leaf scan), then EXIT mid-spawn.
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'trickle', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '30'])
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
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    rec = {'ready_seen': b'READY' in ready, 'announced_identity': ident, 'ns_resolution': resolved}
    if len(resolved) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (R4 root unresolvable)'
        return rec
    try:
        p.stdin.write(b'G'); p.stdin.flush()
    except OSError:
        pass
    spawned = 0
    for _ in range(300):
        line = p.stdout.readline()
        if not line:
            break
        if line.startswith(b'spawned '):
            try:
                spawned = int(line.split()[1])
            except ValueError:
                pass
            if spawned >= 8:
                break
    snap = scan_leaf_targets(outer_frag)
    desc = [t for t in snap if is_in_ns_desc(t)]
    rec['streamed_spawned_count'] = spawned
    rec['leaf_descendants'] = desc
    rec['spawning_claim'] = spawned >= 8 and len(desc) >= 8
    rec['bwrap_host_pid'] = p.pid
    rec['leg'] = ('SPAWNING-IN-PROGRESS (left running mid-spawn; domain stop pending; see observer_stop)'
                  if rec['spawning_claim'] else 'INCONCLUSIVE (spawn progress unprovable)')
    return rec

def do_r4b(ctx, outer_frag):
    return do_r3b(ctx, outer_frag)

def do_r5phase(ctx, outer_frag):
    # R5 releaser: hold victim; after the manager itself observes target
    # execution (RELEASED line), it persists a nonce phase file. The wrapper
    # kills the observer only on (phase file + populated leaf).
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'hold', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,), preexec_fn=lambda: ctx.enter_target(None))
    # NOTE (m1): the worker preamble (identity/input lines) precedes RELEASED;
    # the marker is awaited by draining, never by assuming the first line.
    marker = False
    for _ in range(10):
        line = p.stdout.readline()
        if not line:
            break
        if b'RELEASED' in line:
            marker = True
            break
    phase = ctx.root / 'phase-r5'
    if marker:
        phase.write_text('RELEASED-EXECUTING monotonic=%r nonce=%s' % (time.monotonic(), ctx.nonce))
        evt({'case': 'r5phase', 'phase': 'RELEASED-EXECUTING'})
    try:
        stdout, stderr = p.communicate(timeout=35)
        rec = {'marker_seen': marker, 'phase_file': phase.exists(), 'hold_rc': p.returncode}
    except subprocess.TimeoutExpired:
        p.kill(); p.communicate()
        rec = {'marker_seen': marker, 'phase_file': phase.exists(), 'hold_rc': 'timeout'}
    rec['leg'] = ('RELEASED-EXECUTING (proven phase persisted; see wrapper audit)'
                  if marker and p.returncode == 0 else 'INCONCLUSIVE (R5 phase unproven)')
    return rec

def do_r6stale(ctx, outer_frag):
    # R6 guard demonstration. No host games: a reaped child (absent), self
    # with a wrong starttime (mismatch), self with the true starttime + SIG0
    # (match passes, harmless).
    rec = {'checks': []}
    c = os.fork()
    if c == 0:
        os._exit(0)
    st = starttime_of(c)
    os.waitpid(c, 0)
    ra = signal_validated(c, {'starttime': st}, signal.SIGKILL, outer_frag, 'manager', 'p07.py')
    rec['checks'].append({'name': 'absent-pid', 'result': ra})
    me = os.getpid()
    rb = signal_validated(me, {'starttime': '0'}, signal.SIGKILL, outer_frag)
    rec['checks'].append({'name': 'mismatched-starttime', 'result': rb})
    true_st = starttime_of(me)
    rc = signal_validated(me, {'starttime': true_st}, 0, outer_frag)
    rec['checks'].append({'name': 'matching-identity-sig0', 'result': rc})
    ok = (ra.get('action') == 'refused-absent' and rb.get('action') == 'refused-mismatch'
          and rc.get('action') == 'signaled')
    rec['leg'] = ('SUPPORTED-leg (guard refuses absent + mismatched, passes matching)'
                  if ok else 'BLOCKED (identity guard misbehaved)')
    return rec

def do_r3live(ctx, outer_frag):
    # Streaming R3 releaser: release root + descendant, prove the descendant
    # alive twice, announce ALIVE, then BLOCK on stdin for the runner's
    # PROCEED (the runner kills the domain while this manager lives).
    p, rec = spawn_release(ctx, 'holdchild', 30, outer_frag)
    if p is None:
        return rec
    time.sleep(1.0)
    snap1 = scan_leaf_targets(outer_frag)
    time.sleep(1.0)
    snap2 = scan_leaf_targets(outer_frag)
    d1 = [t for t in snap1 if is_in_ns_desc(t)]
    d2 = [t for t in snap2 if is_in_ns_desc(t)]
    alive = [t for t in d2 if t['state'] and t['state'][0] in 'RSD'
             and any(o['pid'] == t['pid'] and o['starttime'] == t['starttime'] for o in d1)]
    rec['descendants_alive_twice'] = alive
    if not alive:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (no live descendant provable)'
        return rec
    evt({'live': 'ALIVE', 'descendant': alive[0], 'root': rec['ns_resolution'][0]})
    try:
        proceed = sys.stdin.read(1)
    except OSError as e:
        rec['proceed_error'] = repr(e); proceed = ''
    rec['proceed'] = proceed
    try:
        stdout, stderr = p.communicate(timeout=20)
        rec['candidate'] = dict(rc=p.returncode, stdout_tail=stdout.decode(errors='replace')[-1500:],
                                timeout=False)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, timeout=True)
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    if empty and not rec['candidate'].get('timeout'):
        rec['leg'] = 'SUPPORTED-leg (live descendant stopped by runner domain kill; see runner audit)'
    elif not empty:
        rec['leg'] = 'BLOCKED (target domain survived the runner domain stop)'
    else:
        rec['leg'] = 'INCONCLUSIVE (R3 live completion unmet)'
    return rec

def do_r4live(ctx, outer_frag):
    # Streaming R4 releaser: release the trickle spawner, prove active
    # spawning, announce, then BLOCK for PROCEED mid-spawn.
    p, rec = spawn_release(ctx, 'trickle', 30, outer_frag)
    if p is None:
        return rec
    spawned = 0
    for _ in range(300):
        line = p.stdout.readline()
        if not line:
            break
        if line.startswith(b'spawned '):
            try:
                spawned = int(line.split()[1])
            except ValueError:
                pass
            if spawned >= 8:
                break
    snap = scan_leaf_targets(outer_frag)
    desc = [t for t in snap if is_in_ns_desc(t) and t['state'] and t['state'][0] in 'RSD']
    rec['streamed_spawned_count'] = spawned
    rec['leaf_live_descendants'] = desc
    if spawned < 8 or len(desc) < 8:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (spawn progress unprovable)'
        return rec
    evt({'live': 'SPAWNING', 'spawned': spawned, 'sample_descendant': desc[0]})
    try:
        proceed = sys.stdin.read(1)
    except OSError as e:
        rec['proceed_error'] = repr(e); proceed = ''
    rec['proceed'] = proceed
    try:
        stdout, stderr = p.communicate(timeout=25)
        rec['candidate'] = dict(rc=p.returncode, stdout_tail=stdout.decode(errors='replace')[-1500:],
                                timeout=False)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, timeout=True)
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    if empty and not rec['candidate'].get('timeout'):
        rec['leg'] = 'SUPPORTED-leg (mid-spawn domain stopped by runner kill; see runner audit)'
    elif not empty:
        rec['leg'] = 'BLOCKED (target domain survived the runner mid-spawn stop)'
    else:
        rec['leg'] = 'INCONCLUSIVE (R4 live completion unmet)'
    return rec

def spawn_gate(ctx, outer_frag):
    """Release the R2 pre-target gate (no GO: the gate's own stdin channel is
    the authorization path). Returns (Popen, record) after PRETARGET is seen
    and the announced ns-PID is uniquely resolved. No auth is ever written
    here — the runner authorizes release via the manager's stdin decision."""
    argv = (ctx.common + ['--seccomp', str(ctx.secc()), '--', '/target', 'gate', str(ctx.cg),
                          str(ctx.root), ctx.nonce, '/input', '0'])
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,),
                         preexec_fn=lambda: ctx.enter_target(None))
    pretarget = False
    ready = b''
    for _ in range(20):
        chunk = p.stdout.readline()
        if not chunk:
            break
        ready += chunk
        if b'PRETARGET_GATE_REACHED' in chunk:
            pretarget = True
            break
    ident = {}
    for line in ready.decode(errors='replace').splitlines():
        if line.startswith('identity '):
            for kv in line.split()[1:]:
                k, _, v = kv.partition('=')
                ident[k] = v
    resolved = resolve_ns_root(outer_frag) if ident.get('pid', '').isdecimal() else []
    rec = {'gate_reached': pretarget, 'announced_identity': ident,
           'ns_resolution': resolved, 'bwrap_host_pid': p.pid,
           'bwrap_argv_tail': argv[-8:]}
    if not pretarget or len(resolved) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (gate unreached or unresolvable)'
        return None, rec
    rec['state'] = 'GATE-HELD'
    return p, rec

def do_r2gate(ctx, outer_frag, positive):
    # R2 replacement releaser. Blocks for the runner's single-byte decision:
    # 'A' = write the exact nonce authorization (positive only);
    # anything else ('K') = never authorize (deadline).
    p, rec = spawn_gate(ctx, outer_frag)
    if p is None:
        return rec
    evt({'r2gate': 'GATE-READY', 'positive': positive,
         'gate': rec['ns_resolution'][0]})
    try:
        decision = sys.stdin.read(1)
    except OSError as e:
        rec['decision_error'] = repr(e); decision = ''
    rec['decision'] = decision
    if positive and decision == 'A':
        try:
            p.stdin.write((ctx.nonce + '\n').encode()); p.stdin.flush()
            rec['auth_sent'] = True
            evt({'r2gate': 'AUTH_SENT'})
        except OSError as e:
            rec['auth_sent'] = False; rec['auth_error'] = repr(e)
    else:
        rec['auth_sent'] = False
    try:
        stdout, stderr = p.communicate(timeout=25)
        out = stdout.decode(errors='replace')
        rec['candidate'] = dict(rc=p.returncode, stdout_tail=out[-3000:], timeout=False)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, timeout=True)
    out = rec['candidate'].get('stdout_tail', '')
    rec['auth_received_echo'] = 'AUTH_RECEIVED' in out
    rec['target_markers'] = [m for m in ('GATE_AUTH_OK', 'spinwork_sum', 'complete')
                             if m in out]
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    if positive:
        if (rec['auth_sent'] and rec['auth_received_echo'] and 'GATE_AUTH_OK' in out
                and 'spinwork_sum' in out and 'complete' in out and p.returncode == 0 and empty):
            rec['leg'] = ('SUPPORTED-leg (gate released the real target on exact-nonce auth; '
                          'useful bounded work; empty)')
        else:
            rec['leg'] = 'INCONCLUSIVE (R2 positive control failed)'
    else:
        if (not rec['auth_sent'] and not rec['auth_received_echo'] and 'GATE_AUTH_OK' not in out
                and empty and not rec['candidate'].get('timeout')):
            rec['leg'] = ('SUPPORTED-leg (barrier held, no auth, no target exec; '
                          'see runner audit)')
        elif not empty:
            rec['leg'] = 'BLOCKED (domain survived the R2 deadline stop)'
        else:
            rec['leg'] = 'INCONCLUSIVE (R2 deadline completion unmet)'
    return rec

def do_r2gate_pos(ctx, outer_frag):
    return do_r2gate(ctx, outer_frag, True)

def do_r2gate_dead(ctx, outer_frag):
    return do_r2gate(ctx, outer_frag, False)

def spawn_sgate(ctx, outer_frag):
    """Release the SEPARATE pre-target gate: Bubblewrap launches /gate
    directly (a distinct static binary sharing no initialization code with
    /target). The documented fixture-preparation delta vs COMMON is exactly
    one extra --ro-bind of the gate binary; namespaces/caps/seccomp/cgroups
    are unchanged. No auth is ever written here."""
    argv = (ctx.common + ['--ro-bind', str(ctx.root / 'gate'), '/gate', '--seccomp', str(ctx.secc()), '--',
                          '/gate', ctx.nonce, str(ctx.cg), str(ctx.root), '/input'])
    p = subprocess.Popen(argv, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                         close_fds=True, pass_fds=(ctx.seccomp_fd,),
                         preexec_fn=lambda: ctx.enter_target(None))
    pretarget = False
    ready = b''
    # Bounded: a silent-but-live Bubblewrap (or a wedged setup) must surface
    # as a stalled spawn with evidence, never as a hang.
    t0 = time.monotonic()
    stalled = False
    while time.monotonic() - t0 < 30.0:
        r, _, _ = select.select([p.stdout], [], [], 2.0)
        if not r:
            if p.poll() is not None:
                break
            continue
        chunk = p.stdout.readline()
        if not chunk:
            break
        ready += chunk
        if len(ready) > 65536:
            break
        if b'PRETARGET_GATE_REACHED' in chunk:
            pretarget = True
            break
    else:
        stalled = True
    text = ready.decode(errors='replace')
    if stalled:
        p.kill()
        try:
            p.wait(timeout=5)
        except subprocess.TimeoutExpired:
            pass
        return None, {'gate_reached': False, 'stalled_spawn': True,
                      'partial_output': text[:2000], 'bwrap_host_pid': p.pid,
                      'leg': 'INCONCLUSIVE (gate spawn stalled with live silent setup)'}
    # Structural corroboration (not proof): a separate gate emits no worker
    # preamble — identity/input lines must be absent pre-marker.
    preamble_free = ('identity ' not in text and 'input rc=' not in text)
    resolved = resolve_ns_root(outer_frag, 'delegated/target', '2', '/gate')
    rec = {'gate_reached': pretarget, 'preamble_free': preamble_free,
           'pre_marker_output': text[:2000],
           'ns_resolution': resolved, 'bwrap_host_pid': p.pid,
           'bwrap_argv_tail': argv[-11:]}
    if not pretarget or len(resolved) != 1:
        p.kill(); p.wait()
        rec['leg'] = 'INCONCLUSIVE (separate gate unreached or unresolvable)'
        return None, rec
    rec['state'] = 'GATE-HELD'
    return p, rec

def do_r2sgate(ctx, outer_frag, positive):
    # Separate-gate R2 releaser. Same decision protocol as the in-worker
    # gate ('A' authorizes with the exact nonce; anything else never does).
    p, rec = spawn_sgate(ctx, outer_frag)
    if p is None:
        return rec
    evt({'r2gate': 'GATE-READY', 'positive': positive,
         'gate': rec['ns_resolution'][0]})
    try:
        decision = sys.stdin.read(1)
    except OSError as e:
        rec['decision_error'] = repr(e); decision = ''
    rec['decision'] = decision
    if positive and decision == 'A':
        try:
            p.stdin.write((ctx.nonce + '\n').encode()); p.stdin.flush()
            rec['auth_sent'] = True
            evt({'r2sgate': 'AUTH_SENT'})
        except OSError as e:
            rec['auth_sent'] = False; rec['auth_error'] = repr(e)
    else:
        rec['auth_sent'] = False
    try:
        stdout, stderr = p.communicate(timeout=25)
        out = stdout.decode(errors='replace')
        rec['candidate'] = dict(rc=p.returncode, stdout_tail=out[-3000:], timeout=False)
    except subprocess.TimeoutExpired:
        p.kill(); stdout, stderr = p.communicate()
        rec['candidate'] = dict(rc=p.returncode, timeout=True)
    out = rec['candidate'].get('stdout_tail', '')
    rec['auth_received_echo'] = 'AUTH_RECEIVED' in out
    rec['gate_seccomp_ok'] = 'gate seccomp=2 filters=1' in out
    rec['target_seccomp_ok'] = 'target seccomp=2 filters=1' in out
    rec['target_markers'] = [m for m in ('GATE_AUTH_OK', 'spinwork_sum', 'complete')
                             if m in out]
    empty, dt = wait_empty(ctx.cg / 'target', 10.0)
    rec['domain_empty'] = empty; rec['empty_after_s'] = dt
    if positive:
        if (rec['auth_sent'] and rec['auth_received_echo'] and 'GATE_AUTH_OK' in out
                and 'spinwork_sum' in out and 'complete' in out and p.returncode == 0 and empty
                and rec['gate_seccomp_ok'] and rec['target_seccomp_ok']):
            rec['leg'] = ('SUPPORTED-leg (separate gate released the real target on exact-nonce auth; '
                          'useful bounded work under the installed frozen filter; empty)')
        else:
            rec['leg'] = 'INCONCLUSIVE (separate-gate positive control failed)'
    else:
        if (not rec['auth_sent'] and not rec['auth_received_echo'] and 'GATE_AUTH_OK' not in out
                and empty and not rec['candidate'].get('timeout') and rec['gate_seccomp_ok']):
            rec['leg'] = ('SUPPORTED-leg (barrier held under the installed frozen filter, no auth, '
                          'no target exec; see runner audit)')
        elif not empty:
            rec['leg'] = 'BLOCKED (domain survived the separate-gate deadline stop)'
        else:
            rec['leg'] = 'INCONCLUSIVE (separate-gate deadline completion unmet)'
    return rec

def do_r2sgate_pos(ctx, outer_frag):
    return do_r2sgate(ctx, outer_frag, True)

def do_r2sgate_dead(ctx, outer_frag):
    return do_r2sgate(ctx, outer_frag, False)

CASES = {
    'positives': do_positives,
    'c1': do_c1, 'c2': do_c2, 'c3': do_c3, 'c4': do_c4, 'c5': do_c5,
    'c6': do_c6, 'c7': do_c7, 'c8': do_c8, 'c9': do_c9, 'c10': do_c10, 'c11': do_c11,
    'cancel_det': do_cancel_det, 'cancel_race': do_cancel_race,
    'bat_daemon': battery_case('daemonizer', 6, 0, ['complete']),
    'bat_fork': battery_case('forker', 40, 0, ['complete']),
    'bat_reexec': battery_case('reexec', 0, 0, ['generation second']),
    'bat_interfere': battery_case('interfere', 0, 0, ['complete']),
    'positives-r': do_positives_r,
    'r1worker': do_r1worker, 'r2deadline': do_r2deadline,
    'r3a': do_r3a, 'r3b': do_r3b, 'r4a': do_r4a, 'r4b': do_r4b,
    'r5phase': do_r5phase, 'r6stale': do_r6stale,
    'r3live': do_r3live, 'r4live': do_r4live,
    'r2gate_pos': do_r2gate_pos, 'r2gate_dead': do_r2gate_dead,
    'r2sgate_pos': do_r2sgate_pos, 'r2sgate_dead': do_r2sgate_dead,
}
SUITE = ['positives', 'c1', 'c2', 'c3', 'c4', 'c5', 'c6', 'c7', 'c8', 'c9', 'c10',
         'cancel_det', 'cancel_race', 'bat_daemon', 'bat_fork', 'bat_reexec', 'bat_interfere', 'c11']

def observer(a):
    assert os.getuid() == 0
    nonce = uuid.uuid4().hex; root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
    outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce); cg = outer / 'delegated'
    repo = pathlib.Path(__file__).resolve().parent.parent.parent
    only = a.only.split(',') if a.only else SUITE
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
                elif case in ('r3a', 'r4a'):
                    # R3/R4: the manager exited leaving a proven-live workload.
                    # The observer now issues the whole-domain stop itself on
                    # the TARGET leaf (manager lives outside it) and verifies.
                    rec = (crec['result'] or {}).get('record', {}) or {}
                    claim = rec.get('alive_claim') if case == 'r3a' else rec.get('spawning_claim')
                    audit = {'manager_rc': crec.get('manager_rc'), 'claim': bool(claim)}
                    pre = leaf_pids(cg, outer_frag)
                    audit['populated_before_stop'] = [(e['pid'], e['leaf']) for e in pre
                                                      if e['leaf'] == 'target']
                    if crec.get('manager_rc') == 0 and claim and audit['populated_before_stop']:
                        if case == 'r4a':
                            time.sleep(0.3)
                            pre2 = leaf_pids(cg, outer_frag)
                            n1 = len([e for e in pre if e['leaf'] == 'target'])
                            n2 = len([e for e in pre2 if e['leaf'] == 'target'])
                            audit['growth_check'] = {'before': n1, 'after': n2}
                        write(cg / 'target/cgroup.kill', 1)
                        audit['stop_issued'] = 'target/cgroup.kill by root observer'
                        empty, dt = wait_empty(cg / 'target', 10.0)
                        audit['domain_empty'] = empty; audit['empty_after_s'] = dt
                        audit['strays_after'] = leaf_pids(cg, outer_frag)
                        if empty and not audit['strays_after']:
                            audit['root_status'] = 'known per manager record; domain observation governs'
                            crec['verdict'] = 'SUPPORTED-leg (live domain stopped via owned cgroup.kill; empty, no strays)'
                        elif not empty:
                            crec['verdict'] = 'BLOCKED (target domain survived the whole-domain stop)'
                            halted = True
                        else:
                            crec['verdict'] = 'INCONCLUSIVE (strays after domain stop)'
                    else:
                        crec['verdict'] = 'INCONCLUSIVE (R3/R4 stop preconditions unmet)'
                    crec['observer_stop'] = audit
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
    child, SIGKILL it shortly after the release is independently observed
    (target leaf populated — read directly from cgroupfs, never relayed
    through the victim), then audit via the nonce manifest. No guardian, no
    service: a bounded test driver."""
    assert os.getuid() == 0
    cmd = [sys.executable, __file__, '--observer-only-c11', '--source-sha', a.source_sha or '', '--output', a.output]
    p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1, close_fds=True)
    nonce = None; killed = False; transcript = []
    try:
        line = p.stdout.readline()
        transcript.append(line[:500])
        if line.startswith('NONCE '):
            nonce = line.split()[1].strip()
        else:
            raise RuntimeError('no NONCE first line: ' + line[:200])
        target_procs = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce) / 'delegated' / 'target' / 'cgroup.procs'
        t0 = time.monotonic()
        released = False
        while time.monotonic() - t0 < 60.0:
            try:
                if target_procs.read_text().strip():
                    released = True
                    break
            except OSError:
                pass
            time.sleep(0.05)
        transcript.append('release_observed=%r' % released)
        if released:
            time.sleep(1.0)
            p.send_signal(signal.SIGKILL)
            killed = True
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

def manifest_append(manifest, obj):
    with open(manifest, 'a') as f:
        f.write(json.dumps(obj) + '\n')

def provisioner(a):
    """R1 dedicated outer provisioning/caller (root). Persists a nonce-bound
    manifest incrementally OUTSIDE the candidate lifecycle domain, then
    terminates itself at the intended barrier. r1a: mid-provisioning (before
    any manager spawn). r1b: after the manager's release is observed."""
    assert os.getuid() == 0
    variant = a.variant
    root = pathlib.Path(a.root); outer = pathlib.Path(a.outer); cg = outer / 'delegated'
    nonce = a.nonce
    manifest = pathlib.Path(str(root) + '.manifest-' + variant + '.json')
    root.mkdir(mode=0o755, exist_ok=True)
    manifest_append(manifest, {'step': 0, 'action': 'provisioner-start', 'nonce': nonce, 'variant': variant})
    (root / 'input').write_text(nonce); os.chmod(root / 'input', 0o444)
    manifest_append(manifest, {'step': 1, 'action': 'workspace+input', 'paths': [str(root), str(root / 'input')]})
    outer.mkdir(exist_ok=True); write(outer / 'memory.max', 268435456); write(outer / 'pids.max', 64)
    write(outer / 'memory.swap.max', 0); write(outer / 'cgroup.subtree_control', '+memory +pids')
    cg.mkdir(exist_ok=True); write(cg / 'memory.max', 268435456); write(cg / 'pids.max', 64)
    write(cg / 'cgroup.subtree_control', '+memory +pids'); leaf_own(cg)
    for name in ('manager', 'target', 'positive'):
        (cg / name).mkdir(exist_ok=True); leaf_own(cg / name)
    write(cg / 'target/memory.max', 134217728); write(cg / 'target/pids.max', 48)
    manifest_append(manifest, {'step': 2, 'action': 'cgroup-tree',
                               'paths': [str(outer), str(cg), str(cg / 'manager'), str(cg / 'target'),
                                         str(cg / 'positive')]})
    if variant == 'r1a':
        manifest_append(manifest, {'step': 3, 'action': 'barrier-pre-manager-spawn',
                                   'fault': 'SIGKILL self mid-provisioning'})
        os.kill(os.getpid(), signal.SIGKILL)
        return
    # r1b: complete provisioning, spawn the manager, die once its release
    # is observed in the capture file.
    data = {'compile': run(['/usr/bin/gcc', '-static', '-O2', '-Wall', '-Wextra', '-o',
                            str(root / 'worker'), str(pathlib.Path(__file__).with_suffix('.c'))])}
    if data['compile']['rc']:
        manifest_append(manifest, {'step': 3, 'action': 'compile-failed', 'fault': 'SIGKILL self'})
        os.kill(os.getpid(), signal.SIGKILL)
        return
    os.chown(root, UID, UID)
    capture = root / 'r1b-capture.txt'
    capture.touch(); os.chown(capture, UID, UID)
    manifest_append(manifest, {'step': 3, 'action': 'worker+capture',
                               'paths': [str(root / 'worker'), str(capture)],
                               'worker_sha256': sha(root / 'worker')})
    def drop_r1():
        write(cg / 'manager/cgroup.procs', os.getpid()); os.setgroups([]); os.setgid(UID); os.setuid(UID)
        bounds(fsize=False)
    cmd = [sys.executable, __file__, '--manager', '--case', 'r1worker', '--root', str(root),
           '--cg', str(cg), '--nonce', nonce, '--outer-frag', str(outer).removeprefix('/sys/fs/cgroup')]
    with open(capture, 'w') as cap:
        m = subprocess.Popen(cmd, stdout=cap, stderr=subprocess.STDOUT, preexec_fn=drop_r1, close_fds=True)
        manifest_append(manifest, {'step': 4, 'action': 'manager-spawned', 'manager_pid': m.pid})
        t0 = time.monotonic(); seen = False
        while time.monotonic() - t0 < 60.0:
            try:
                if 'SPAWNED' in capture.read_text():
                    seen = True
                    break
            except OSError:
                pass
            if m.poll() is not None:
                break
            time.sleep(0.05)
        manifest_append(manifest, {'step': 5, 'action': 'barrier-release-observed' if seen else 'barrier-manager-gone',
                                   'fault': 'SIGKILL self post-manager-spawn'})
        os.kill(os.getpid(), signal.SIGKILL)

def run_r1(a):
    """R1 independent surviving observer (root): provision nothing itself,
    await the provisioner's death, then audit release/domain/population and
    enforce whole-domain cleanup on a dedicated R1 domain."""
    assert os.getuid() == 0
    variant = a.variant
    nonce = uuid.uuid4().hex
    root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
    outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
    outer_frag = str(outer).removeprefix('/sys/fs/cgroup')
    manifest = pathlib.Path(str(root) + '.manifest-' + variant + '.json')
    data = {'leg': 'r1' + variant, 'source_sha': a.source_sha, 'nonce': nonce,
            'date_utc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
            'host': {'kernel': os.uname().release, 'arch': os.uname().machine},
            'source_hashes': {p.name: sha(p) for p in list(pathlib.Path(__file__).parent.glob('p07.*'))},
            'paths': {'root': str(root), 'outer': str(outer)}, 'observer_uid': 0}
    try:
        if sha('/usr/bin/bwrap') != PIN or os.stat('/usr/bin/bwrap').st_mode & 0o6000:
            raise RuntimeError('helper pin/nonsetuid prerequisite changed')
        cmd = [sys.executable, __file__, '--provisioner', '--variant', variant, '--root', str(root),
               '--outer', str(outer), '--nonce', nonce]
        p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, close_fds=True)
        try:
            out, _ = p.communicate(timeout=120)
            data['provisioner_rc'] = p.returncode
            data['provisioner_signo'] = -p.returncode if p.returncode < 0 else 0
            data['provisioner_output'] = out.decode(errors='replace')[-2000:]
        except subprocess.TimeoutExpired:
            p.kill(); p.communicate()
            data['provisioner_watchdog'] = True
        steps = []
        try:
            with open(manifest) as f:
                for line in f:
                    line = line.strip()
                    if line:
                        steps.append(json.loads(line))
        except OSError as e:
            data['manifest_error'] = repr(e)
        data['manifest_steps'] = steps
        if variant == 'r1a':
            listed = [path for s in steps for path in s.get('paths', [])]
            data['manifest_paths'] = listed
            data['paths_exist'] = {q: pathlib.Path(q).exists() for q in listed}
            procs = leaf_pids(outer / 'delegated', outer_frag) if (outer / 'delegated').exists() else []
            data['domain_processes'] = procs
            data['target_released'] = False
            for q in listed:
                qp = pathlib.Path(q)
                try:
                    if qp.is_file() or qp.is_symlink():
                        qp.unlink()
                except OSError as e:
                    data.setdefault('removal_errors', []).append(repr(e))
            for q in sorted([q for q in listed if pathlib.Path(q).is_dir()], reverse=True):
                try:
                    pathlib.Path(q).rmdir()
                except OSError as e:
                    data.setdefault('removal_errors', []).append(repr(e))
            data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                       'manifest': not manifest.exists()}
            for q in (root, manifest):
                try:
                    if q.is_file() or q.is_symlink():
                        q.unlink()
                    elif q.is_dir():
                        q.rmdir()
                except OSError as e:
                    data.setdefault('removal_errors', []).append(repr(e))
            data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                       'manifest': not manifest.exists()}
            ok = (data['provisioner_signo'] == signal.SIGKILL and not procs
                  and all(data['cleanup_absence'].values()) and not data.get('removal_errors'))
            data['verdict'] = ('SUPPORTED-leg (caller died mid-provisioning; manifest-driven audit: '
                               'nothing released, no domain population, all listed state removed)'
                               if ok else 'INCONCLUSIVE (R1a audit unmet)')
        else:
            cap = root / 'r1b-capture.txt'
            try:
                data['capture'] = cap.read_text()[-4000:]
            except OSError as e:
                data['capture_error'] = repr(e)
            spawned = 'SPAWNED' in data.get('capture', '')
            mgr = [e for e in leaf_pids(outer / 'delegated', outer_frag) if e['leaf'] == 'manager']
            tgt = [e for e in leaf_pids(outer / 'delegated', outer_frag) if e['leaf'] == 'target']
            data['manager_leaf'] = mgr; data['target_leaf_pre_kill'] = [(e['pid'], e['cmdline'][:60]) for e in tgt]
            data['ownership_without_caller'] = spawned and len(mgr) > 0 and len(tgt) > 0
            if outer.exists():
                write(outer / 'cgroup.kill', 1)
                for _ in range(50):
                    if 'populated 0' in read(outer / 'cgroup.events'):
                        break
                    time.sleep(.1)
                data['outer_empty'] = 'populated 0' in read(outer / 'cgroup.events')
                known = {'manager', 'target', 'positive', 'sibling'}
                for parent in (outer / 'delegated', outer):
                    if parent.exists():
                        for q in sorted(parent.iterdir()):
                            if q.is_dir() and q.name in known:
                                try:
                                    q.rmdir()
                                except OSError as e:
                                    data.setdefault('removal_errors', []).append(q.name + ': ' + repr(e))
                        try:
                            parent.rmdir()
                        except OSError:
                            pass
            data['outer_gone'] = not outer.exists()
            try:
                data['capture_final'] = cap.read_text()[-2000:]
            except OSError:
                pass
            if root.exists():
                for q in list(root.iterdir()):
                    try:
                        if q.is_file() or q.is_symlink():
                            q.unlink()
                    except OSError:
                        pass
                for q in list(root.iterdir()):
                    try:
                        if q.is_dir():
                            q.rmdir()
                    except OSError as e:
                        data.setdefault('removal_errors', []).append(repr(e))
                try:
                    root.rmdir()
                except OSError as e:
                    data.setdefault('removal_errors', []).append(repr(e))
            if manifest.exists():
                try:
                    manifest.unlink()
                except OSError as e:
                    data.setdefault('removal_errors', []).append(repr(e))
            data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                       'manifest': not manifest.exists()}
            ok = (data['provisioner_signo'] == signal.SIGKILL and data['ownership_without_caller']
                  and data.get('outer_empty') and all(data['cleanup_absence'].values()))
            data['verdict'] = ('SUPPORTED-leg (caller died post-spawn; release + domain population '
                               'survived without the caller; observer-enforced domain kill emptied; all removed)'
                               if ok else 'INCONCLUSIVE (R1b audit unmet)')
    except Exception as e:
        data['setup_error'] = repr(e)
    dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
    (dest / ('r1-' + variant + '-' + nonce + '.json')).write_text(json.dumps(data, indent=2))
    print(json.dumps(data, indent=2))
    return 0 if data.get('verdict', '').startswith('SUPPORTED') else 2

def run_live(a, kind):
    """R3/R4 streaming runner (root): own domain; manager stays alive across
    the stop. Marker (ALIVE/SPAWNING) -> FRESH pre-kill liveness proof ->
    target-leaf kill -> PROCEED -> RESULT -> teardown. The kill lands on a
    domain whose members were just proven live — no cascade ambiguity."""
    assert os.getuid() == 0
    case = 'r3live' if kind == 'r3' else 'r4live'
    marker_key = 'ALIVE' if kind == 'r3' else 'SPAWNING'
    nonce = uuid.uuid4().hex
    root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
    outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
    cg = outer / 'delegated'
    outer_frag = str(outer).removeprefix('/sys/fs/cgroup')
    data = {'leg': 'live-' + kind, 'source_sha': a.source_sha, 'nonce': nonce,
            'date_utc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
            'host': {'kernel': os.uname().release, 'arch': os.uname().machine},
            'helper_sha256': sha('/usr/bin/bwrap'),
            'helper_mode': oct(os.stat('/usr/bin/bwrap').st_mode & 0o7777),
            'source_hashes': {p.name: sha(p) for p in list(pathlib.Path(__file__).parent.glob('p07.*'))},
            'paths': {'root': str(root), 'outer': str(outer)}, 'observer_uid': 0}
    if data['helper_sha256'] != PIN or os.stat('/usr/bin/bwrap').st_mode & 0o6000:
        data['verdict'] = 'INCONCLUSIVE (helper pin changed)'
    else:
        m = None
        try:
            root.mkdir(mode=0o755)
            comp = run(['/usr/bin/gcc', '-static', '-O2', '-Wall', '-Wextra', '-o',
                        str(root / 'worker'), str(pathlib.Path(__file__).with_suffix('.c'))])
            data['compile'] = comp
            if comp['rc']:
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
            data['filter_note'] = 'frozen P02/P03/P04 bytes; manager builds its own memfd (manager-owned)'

            def drop():
                write(cg / 'manager/cgroup.procs', os.getpid()); os.setgroups([]); os.setgid(UID); os.setuid(UID)
                bounds(fsize=False)
            cmd = [sys.executable, __file__, '--manager', '--case', case, '--root', str(root),
                   '--cg', str(cg), '--nonce', nonce, '--outer-frag', outer_frag]
            m = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                 stderr=subprocess.PIPE, text=True, bufsize=1,
                                 preexec_fn=drop, close_fds=True)
            data['manager_pid'] = m.pid
            marker = None; deadline = time.monotonic() + 90.0
            # Bounded streaming read: a bare readline() can block past the
            # deadline when an orphaned fixture process holds the pipe open,
            # so every wait is select-bounded and manager death breaks the
            # loop even without EOF.
            while time.monotonic() < deadline:
                r, _, _ = select.select([m.stdout], [], [], 2.0)
                if not r:
                    if m.poll() is not None:
                        data['stream_note'] = 'manager dead with no further output; breaking read loop'
                        break
                    continue
                line = m.stdout.readline()
                if not line:
                    break
                if line.startswith('EVT '):
                    try:
                        obj = json.loads(line[4:])
                    except ValueError:
                        continue
                    if obj.get('live') == marker_key:
                        marker = obj
                        break
            data['marker'] = marker
            if marker is None:
                m.kill(); m.wait()
                data['verdict'] = 'INCONCLUSIVE (live marker never arrived)'
            else:
                # FRESH pre-kill liveness: re-read every claimed member NOW.
                proof = {'manager_alive': False, 'members': []}
                try:
                    mst = pathlib.Path('/proc/%d/status' % m.pid).read_text()
                    proof['manager_alive'] = 'State:' in mst
                except OSError:
                    pass
                if kind == 'r3':
                    want = [marker['descendant']['pid']]
                    for pid in want:
                        r = revalidate_pid(pid, outer_frag, 'target', '/target')
                        live = (r is not None and r['starttime'] == marker['descendant']['starttime'])
                        if r is not None:
                            try:
                                state = [l for l in pathlib.Path('/proc/%d/status' % pid).read_text().splitlines()
                                         if l.startswith('State:')][0].split(':', 1)[1].strip()
                            except (OSError, IndexError):
                                state = None
                        else:
                            state = None
                        proof['members'].append({'pid': pid, 'live': bool(live and state and state[0] in 'RSD'),
                                                 'state': state})
                    proof['root_present'] = len(resolve_ns_root(outer_frag)) == 1
                    fresh = proof['manager_alive'] and all(x['live'] for x in proof['members']) and proof['root_present']
                else:
                    s1 = leaf_pids(cg, outer_frag)
                    time.sleep(0.3)
                    s2 = leaf_pids(cg, outer_frag)
                    n1 = len([e for e in s1 if e['leaf'] == 'target'])
                    n2 = len([e for e in s2 if e['leaf'] == 'target'])
                    live2 = [e for e in s2 if e['leaf'] == 'target' and e['state'] and e['state'][0] in 'RSD']
                    proof['growth'] = {'before': n1, 'after': n2}
                    proof['live_target_members'] = [(e['pid'], e['state']) for e in live2]
                    fresh = proof['manager_alive'] and n2 >= n1 and n2 >= 8 and len(live2) >= 8
                data['fresh_proof'] = proof
                if not fresh:
                    m.kill(); m.wait()
                    data['verdict'] = 'INCONCLUSIVE (pre-kill liveness unprovable)'
                else:
                    write(cg / 'target/cgroup.kill', 1)
                    data['stop_issued'] = 'target/cgroup.kill by root runner on proven-live domain'
                    empty, dt = wait_empty(cg / 'target', 10.0)
                    data['domain_empty'] = empty; data['empty_after_s'] = dt
                    mgr_leaf = read(cg / 'manager/cgroup.procs')
                    data['manager_survived_stop'] = str(m.pid) in mgr_leaf.split()
                    strays = [e for e in leaf_pids(cg, outer_frag, exclude=(m.pid,))]
                    data['strays_after'] = strays
                    try:
                        m.stdin.write('P'); m.stdin.flush()
                    except OSError as e:
                        data['proceed_error'] = repr(e)
                    try:
                        out, err = m.communicate(timeout=60)
                        data['manager_rc'] = m.returncode
                        res = [json.loads(l[7:]) for l in out.splitlines() if l.startswith('RESULT ')]
                        data['manager_result'] = res[0] if res else None
                    except subprocess.TimeoutExpired:
                        m.kill(); m.communicate()
                        data['manager_watchdog'] = True
                    ok = (empty and not strays and data['manager_survived_stop']
                          and (data.get('manager_result') or {}).get('record', {}).get('domain_empty'))
                    data['verdict'] = ('SUPPORTED-leg (proven-live domain stopped via owned kill; '
                                       'manager survived; empty; no strays)' if ok
                                       else 'INCONCLUSIVE (live-stop completion unmet: empty=%r strays=%r)' % (empty, strays))
        except Exception as e:
            data['setup_error'] = repr(e)
        finally:
            try:
                if outer.exists():
                    try:
                        write(outer / 'cgroup.kill', 1)
                    except OSError:
                        pass
                    if m is not None:
                        try:
                            m.wait(timeout=5)
                        except Exception:
                            pass
                    for _ in range(30):
                        if 'populated 0' in read(outer / 'cgroup.events'):
                            break
                        time.sleep(.1)
                    data['cleanup_events'] = read(outer / 'cgroup.events')
                    # The manager creates the sibling leaf itself (P02 pattern);
                    # it must be in the known set or the parent rmdir fails.
                    known = {'manager', 'target', 'positive', 'sibling'}
                    for parent in (cg, outer):
                        if parent.exists():
                            for name in sorted(known):
                                q = parent / name
                                if q.exists():
                                    try:
                                        q.rmdir()
                                    except OSError as e:
                                        data.setdefault('cleanup_errors', []).append(name + ': ' + repr(e))
                    for q in (cg, outer):
                        if q.exists():
                            try:
                                q.rmdir()
                            except OSError as e:
                                data['cleanup_error'] = repr(e)
            except Exception as e:
                data['cleanup_error'] = repr(e)
            if not outer.exists() and root.exists():
                for name in ('input', 'worker', 'phase-r5'):
                    q = root / name
                    if q.exists() or q.is_symlink():
                        try:
                            q.unlink()
                        except OSError:
                            pass
                try:
                    root.rmdir()
                except OSError as e:
                    data['cleanup_error'] = repr(e)
            data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                       'manager': m is None or m.poll() is not None}
            data['cleanup_confirmed'] = all(data['cleanup_absence'].values()) and not data.get('cleanup_error')
    dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
    (dest / ('live-' + kind + '-' + nonce + '.json')).write_text(json.dumps(data, indent=2))
    if not a.quiet:
        print(json.dumps(data, indent=2))
    return 0 if data.get('verdict', '').startswith('SUPPORTED') and data.get('cleanup_confirmed') else 2

def full_leaf_snapshot(cg, outer_frag):
    """Complete pre-decision process-state proof: every fixture-domain
    process with pid/ppid/state/starttime/cmdline/cgroup. Preserved in full,
    never reduced to a Boolean."""
    found = []
    for leaf in ('manager', 'target', 'sibling', 'positive'):
        lp = cg / leaf
        if not lp.exists():
            continue
        for pid in read(lp / 'cgroup.procs').split():
            if not pid.isdecimal():
                continue
            ipid = int(pid)
            try:
                member = pathlib.Path('/proc/%d/cgroup' % ipid).read_text()
                cmd = pathlib.Path('/proc/%d/cmdline' % ipid).read_bytes().replace(b'\0', b' ').decode(errors='replace')
                st = pathlib.Path('/proc/%d/status' % ipid).read_text()
            except OSError:
                continue
            if outer_frag not in member:
                continue
            ppid, state = None, None
            for line in st.splitlines():
                if line.startswith('PPid:'):
                    ppid = line.split()[1] if len(line.split()) > 1 else None
                if line.startswith('State:'):
                    state = line.split(':', 1)[1].strip()
            seccomp = None
            for line in st.splitlines():
                if line.startswith('Seccomp:') and not line.startswith('Seccomp_filters:'):
                    try:
                        seccomp = int(line.split(':', 1)[1].strip())
                    except ValueError:
                        pass
            seccomp_filters = None
            for line in st.splitlines():
                if line.startswith('Seccomp_filters:'):
                    try:
                        seccomp_filters = int(line.split(':', 1)[1].strip())
                    except ValueError:
                        pass
            found.append({'pid': ipid, 'ppid': ppid, 'leaf': leaf, 'state': state,
                          'starttime': starttime_of(ipid), 'cmdline': cmd[:200],
                          'cgroup': member.strip()[:160],
                          'seccomp': seccomp, 'seccomp_filters': seccomp_filters})
    return sorted(found, key=lambda d: d['pid'])

def run_r2(a, kind, pos_case='r2gate_pos', dead_case='r2gate_dead'):
    """R2 replacement runner (root): positive proves the gate releases the
    real target; deadline proves the barrier held with no auth and no target
    exec, then stops the owned domain. Manager stays alive across the stop."""
    assert os.getuid() == 0
    case = pos_case if kind == 'r2pos' else dead_case
    separate = 'sgate' in case
    nonce = uuid.uuid4().hex
    root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
    outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
    cg = outer / 'delegated'
    outer_frag = str(outer).removeprefix('/sys/fs/cgroup')
    data = {'leg': 'r2-' + kind, 'source_sha': a.source_sha, 'nonce': nonce,
            'date_utc': time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()),
            'host': {'kernel': os.uname().release, 'arch': os.uname().machine},
            'helper_sha256': sha('/usr/bin/bwrap'),
            'helper_mode': oct(os.stat('/usr/bin/bwrap').st_mode & 0o7777),
            'source_hashes': {p.name: sha(p) for p in [pathlib.Path(__file__).parent / n for n in ('p07.py', 'p07.c', 'p07gate.c')]},
            'paths': {'root': str(root), 'outer': str(outer)}, 'observer_uid': 0}
    if data['helper_sha256'] != PIN or os.stat('/usr/bin/bwrap').st_mode & 0o6000:
        data['verdict'] = 'INCONCLUSIVE (helper pin changed)'
    else:
        m = None
        # Global watchdog: bound the entire runner even if a child holds a
        # pipe open past every local deadline. Firing raises into the try
        # below, whose finally still runs full teardown and writes a record.
        class Watchdog(Exception):
            pass
        def _wd(signum, frame):
            import traceback
            raise Watchdog('global 240s watchdog fired; runner stack: '
                           + ''.join(traceback.format_stack(frame))[-3000:])
        signal.signal(signal.SIGALRM, _wd)
        signal.alarm(240)
        try:
            root.mkdir(mode=0o755)
            comp = run(['/usr/bin/gcc', '-static', '-O2', '-Wall', '-Wextra', '-o',
                        str(root / 'worker'), str(pathlib.Path(__file__).with_suffix('.c'))])
            data['compile'] = comp
            if comp['rc']:
                raise RuntimeError('compile failed before release')
            data['worker_sha256'] = sha(root / 'worker')
            if separate:
                gsrc = str(pathlib.Path(__file__).parent / 'p07gate.c')
                gcomp = run(['/usr/bin/gcc', '-static', '-O2', '-Wall', '-Wextra', '-o',
                             str(root / 'gate'), gsrc])
                data['gate_compile'] = gcomp
                data['gate_source'] = gsrc
                if gcomp['rc']:
                    raise RuntimeError('gate compile failed before release')
                data['gate_sha256'] = sha(root / 'gate')
                os.chmod(root / 'gate', 0o755)
            else:
                data['gate_sha256'] = 'not-requested (in-worker gate case)'
            (root / 'input').write_text(nonce); os.chmod(root / 'input', 0o444)
            outer.mkdir(); write(outer / 'memory.max', 268435456); write(outer / 'pids.max', 64)
            write(outer / 'memory.swap.max', 0); write(outer / 'cgroup.subtree_control', '+memory +pids')
            cg.mkdir(); write(cg / 'memory.max', 268435456); write(cg / 'pids.max', 64)
            write(cg / 'cgroup.subtree_control', '+memory +pids'); leaf_own(cg)
            for name in ('manager', 'target', 'positive'):
                (cg / name).mkdir(); leaf_own(cg / name)
            write(cg / 'target/memory.max', 134217728); write(cg / 'target/pids.max', 48)
            os.chown(root, UID, UID)
            data['filter_note'] = 'frozen P02/P03/P04 bytes; manager builds its own memfd (manager-owned)'

            def drop():
                write(cg / 'manager/cgroup.procs', os.getpid()); os.setgroups([]); os.setgid(UID); os.setuid(UID)
                bounds(fsize=False)
            cmd = [sys.executable, __file__, '--manager', '--case', case, '--root', str(root),
                   '--cg', str(cg), '--nonce', nonce, '--outer-frag', outer_frag]
            m = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                 stderr=subprocess.PIPE, text=True, bufsize=1,
                                 preexec_fn=drop, close_fds=True)
            data['manager_pid'] = m.pid
            pre_lines = []
            marker = None; deadline = time.monotonic() + 90.0
            # Bounded streaming read: a bare readline() can block past every
            # deadline when a live process holds the pipe without emitting a
            # newline, so each wait is select-bounded and a dead manager
            # breaks the loop even without EOF.
            while time.monotonic() < deadline:
                r, _, _ = select.select([m.stdout], [], [], 2.0)
                if not r:
                    if m.poll() is not None:
                        data['stream_note'] = 'manager dead with no further output; breaking read loop'
                        break
                    continue
                line = m.stdout.readline()
                if not line:
                    break
                pre_lines.append(line[:300])
                if line.startswith('EVT '):
                    try:
                        obj = json.loads(line[4:])
                    except ValueError:
                        continue
                    if obj.get('r2gate') == 'GATE-READY':
                        marker = obj
                        break
            data['pre_decision_output'] = pre_lines
            data['marker'] = marker
            if marker is None:
                # Drain whatever the manager left (nonblocking: a live silent
                # fixture process may still hold the pipe) so a missed marker
                # still preserves the manager's RESULT for diagnosis.
                try:
                    os.set_blocking(m.stdout.fileno(), False)
                    try:
                        rest = m.stdout.read() or ''
                    except Exception:
                        rest = ''
                    if rest:
                        data['manager_output_tail'] = rest[-3000:]
                        res = [json.loads(l[7:]) for l in rest.splitlines() if l.startswith('RESULT ')]
                        if res:
                            data['manager_result'] = res[0]
                except Exception as e:
                    data['drain_error'] = repr(e)
                m.kill(); m.wait()
                data['verdict'] = 'INCONCLUSIVE (gate marker never arrived)'
            else:
                snap = full_leaf_snapshot(cg, outer_frag)
                data['pre_decision_snapshot'] = snap
                tgt = [e for e in snap if e['leaf'] == 'target']
                bwrap = [e for e in tgt if e['cmdline'].startswith('/usr/bin/bwrap')]
                if separate:
                    # Separate gate: its cmdline starts with /gate (never
                    # /target); ANY non-bwrap /target process is a failure.
                    gate = [e for e in tgt if e not in bwrap and e['cmdline'].startswith('/gate ')]
                    others = [e for e in tgt if e not in bwrap and e not in gate
                              and '/target' in e['cmdline']]
                    gate_ok = all(e['cmdline'].startswith('/gate ') for e in gate)
                else:
                    gate = [e for e in tgt if e not in bwrap and '/target gate ' in e['cmdline']]
                    others = [e for e in tgt if e not in bwrap and e not in gate]
                    gate_ok = all('/target gate ' in e['cmdline'] for e in gate)
                data['phase_proof'] = {
                    'target_leaf_members': len(tgt),
                    'gate_processes': [(e['pid'], e['state'], e['starttime']) for e in gate],
                    'bwrap_processes': [(e['pid'], e['state']) for e in bwrap],
                    'non_gate_target_processes': others,
                    'gate_cmdline_still_gate': gate_ok,
                    'gate_seccomp_observed': [(e['pid'], e['seccomp'], e['seccomp_filters']) for e in gate],
                    'auth_received_echo_in_output': any('AUTH_RECEIVED' in l for l in pre_lines),
                    'target_markers_in_output': [l for l in pre_lines
                                                 if 'GATE_AUTH_OK' in l or l.startswith('generation ')],
                }
                phase_ok = (len(gate) >= 1 and not others
                            and data['phase_proof']['gate_cmdline_still_gate']
                            and all(e['seccomp'] == 2 and e['seccomp_filters'] == 1 for e in gate)
                            and not data['phase_proof']['auth_received_echo_in_output']
                            and not data['phase_proof']['target_markers_in_output'])
                data['phase_ok'] = phase_ok
                if not phase_ok:
                    m.kill(); m.wait()
                    data['verdict'] = 'INCONCLUSIVE (pre-decision phase unproven)'
                elif kind == 'r2pos':
                    try:
                        m.stdin.write('A'); m.stdin.flush()
                    except OSError as e:
                        data['authorize_error'] = repr(e)
                    data['release_authorization'] = 'sent exact nonce after proven gate (positive control)'
                    try:
                        out, err = m.communicate(timeout=60)
                        data['manager_rc'] = m.returncode
                        res = [json.loads(l[7:]) for l in out.splitlines() if l.startswith('RESULT ')]
                        data['manager_result'] = res[0] if res else None
                        data['full_transcript_tail'] = out[-3000:]
                    except subprocess.TimeoutExpired:
                        m.kill(); m.communicate()
                        data['manager_watchdog'] = True
                    rec = (data.get('manager_result') or {}).get('record', {}) or {}
                    ok = (rec.get('auth_sent') and rec.get('auth_received_echo')
                          and 'GATE_AUTH_OK' in str(rec.get('target_markers'))
                          and 'spinwork_sum' in str(rec.get('target_markers'))
                          and 'complete' in str(rec.get('target_markers'))
                          and data.get('manager_rc') == 0 and rec.get('domain_empty')
                          and (not separate or (rec.get('gate_seccomp_ok')
                                                and rec.get('target_seccomp_ok'))))
                    data['verdict'] = ('SUPPORTED-leg (gate released the real target on exact-nonce auth; '
                                       'target initialized post-release; useful bounded work; empty)'
                                       if ok else 'INCONCLUSIVE (R2 positive control failed)')
                else:
                    data['deadline_decision'] = ('deadline wins at proven PRETARGET barrier; '
                                                 'release authorization never sent')
                    data['release_authorization'] = 'never sent (manager blocked; gate attests no AUTH_RECEIVED)'
                    write(cg / 'target/cgroup.kill', 1)
                    data['stop_issued'] = 'target/cgroup.kill by root runner on proven-held gate domain'
                    empty, dt = wait_empty(cg / 'target', 10.0)
                    data['domain_empty'] = empty; data['empty_after_s'] = dt
                    mgr_leaf = read(cg / 'manager/cgroup.procs')
                    data['manager_survived_stop'] = str(m.pid) in mgr_leaf.split()
                    strays = [e for e in leaf_pids(cg, outer_frag, exclude=(m.pid,))]
                    data['strays_after'] = strays
                    try:
                        m.stdin.write('K'); m.stdin.flush()
                    except OSError as e:
                        data['proceed_error'] = repr(e)
                    try:
                        out, err = m.communicate(timeout=60)
                        data['manager_rc'] = m.returncode
                        res = [json.loads(l[7:]) for l in out.splitlines() if l.startswith('RESULT ')]
                        data['manager_result'] = res[0] if res else None
                    except subprocess.TimeoutExpired:
                        m.kill(); m.communicate()
                        data['manager_watchdog'] = True
                    rec = (data.get('manager_result') or {}).get('record', {}) or {}
                    ok = (empty and not strays and data['manager_survived_stop']
                          and not rec.get('auth_sent') and not rec.get('auth_received_echo')
                          and 'GATE_AUTH_OK' not in str(rec.get('target_markers', []))
                          and (not separate or rec.get('gate_seccomp_ok')))
                    data['verdict'] = ('SUPPORTED-leg (barrier held; no auth; no target exec; '
                                       'owned domain stopped; empty; no strays)' if ok
                                       else 'INCONCLUSIVE (R2 deadline completion unmet: empty=%r strays=%r auth=%r)' % (
                                           empty, strays, rec.get('auth_sent')))
        except Exception as e:
            data['setup_error'] = repr(e)
        finally:
            try:
                if outer.exists():
                    try:
                        write(outer / 'cgroup.kill', 1)
                    except OSError:
                        pass
                    if m is not None:
                        try:
                            m.wait(timeout=5)
                        except Exception:
                            pass
                    for _ in range(30):
                        if 'populated 0' in read(outer / 'cgroup.events'):
                            break
                        time.sleep(.1)
                    data['cleanup_events'] = read(outer / 'cgroup.events')
                    known = {'manager', 'target', 'positive', 'sibling'}
                    for parent in (cg, outer):
                        if parent.exists():
                            for name in sorted(known):
                                q = parent / name
                                if q.exists():
                                    try:
                                        q.rmdir()
                                    except OSError as e:
                                        data.setdefault('cleanup_errors', []).append(name + ': ' + repr(e))
                    for q in (cg, outer):
                        if q.exists():
                            try:
                                q.rmdir()
                            except OSError as e:
                                data['cleanup_error'] = repr(e)
            except Exception as e:
                data['cleanup_error'] = repr(e)
            if not outer.exists() and root.exists():
                for name in ('input', 'worker', 'gate', 'phase-r5'):
                    q = root / name
                    if q.exists() or q.is_symlink():
                        try:
                            q.unlink()
                        except OSError:
                            pass
                try:
                    root.rmdir()
                except OSError as e:
                    data['cleanup_error'] = repr(e)
            data['cleanup_absence'] = {'outer': not outer.exists(), 'workspace': not root.exists(),
                                       'manager': m is None or m.poll() is not None}
            data['cleanup_confirmed'] = all(data['cleanup_absence'].values()) and not data.get('cleanup_error')
    dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
    (dest / ('r2-' + kind + '-' + nonce + '.json')).write_text(json.dumps(data, indent=2))
    if not a.quiet:
        print(json.dumps(data, indent=2))
    return 0 if data.get('verdict', '').startswith('SUPPORTED') and data.get('cleanup_confirmed') else 2

def r5_wrapper(a):
    """R5 observer loss at a proven phase (root). The manager persists a
    nonce phase file only after IT observes target execution; the wrapper
    kills the observer solely on (phase file + populated leaf). Binds exact
    source SHA and harness hashes, unlike the original C11 record."""
    assert os.getuid() == 0
    here = pathlib.Path(__file__).parent
    binding = {'source_sha': a.source_sha or '',
               'p07_py_sha256': sha(here / 'p07.py'), 'p07_c_sha256': sha(here / 'p07.c')}
    cmd = [sys.executable, __file__, '--observer-only-r5', '--source-sha', a.source_sha or '',
           '--output', a.output]
    p = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1, close_fds=True)
    nonce = None; killed = False; transcript = []
    try:
        line = p.stdout.readline()
        transcript.append(line[:500])
        if line.startswith('NONCE '):
            nonce = line.split()[1].strip()
        else:
            raise RuntimeError('no NONCE first line: ' + line[:200])
        root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
        outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
        phase = root / 'phase-r5'
        target_procs = outer / 'delegated' / 'target' / 'cgroup.procs'
        t0 = time.monotonic(); proven = False
        while time.monotonic() - t0 < 90.0:
            try:
                if phase.exists() and target_procs.read_text().strip():
                    proven = True
                    break
            except OSError:
                pass
            time.sleep(0.05)
        transcript.append('proven_phase=%r' % proven)
        try:
            transcript.append('phase_content=' + phase.read_text()[:200])
        except OSError as e:
            transcript.append('phase_read_error=' + repr(e))
        if proven:
            time.sleep(1.0)
            p.send_signal(signal.SIGKILL)
            killed = True
        p.wait(timeout=30)
    except Exception as e:
        transcript.append('wrapper-error: ' + repr(e))
    audit = {'leg': 'r5', 'binding': binding, 'nonce': nonce, 'observer_killed': killed,
             'observer_rc': p.poll()}
    if nonce:
        root = pathlib.Path('/tmp') / ('gagamba-ar7-P07-' + nonce)
        outer = pathlib.Path('/sys/fs/cgroup') / ('gagamba-ar7-P07-' + nonce)
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
                                try:
                                    q.rmdir()
                                except OSError:
                                    pass
                        try:
                            parent.rmdir()
                        except OSError:
                            pass
            audit['outer_gone'] = not outer.exists()
            if root.exists():
                for q in list(root.iterdir()):
                    try:
                        if q.is_file() or q.is_symlink():
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
        # Temporal split, explicit: execution result Unknown after observer
        # loss (never recovered); domain cleanup confirmed independently
        # afterwards. Cleanup confirmation implies nothing about results.
        audit['execution_result'] = 'UNKNOWN (observer loss; never recovered or inferred)'
        audit['verdict'] = ('SUPPORTED-leg (proven-phase observer loss; result Unknown; '
                            'independent domain cleanup confirmed afterwards)'
                            if killed and audit.get('outer_gone') and audit.get('workspace_gone')
                            else 'INCONCLUSIVE (R5 wrapper audit unmet)')
    audit['transcript_tail'] = transcript[-15:]
    dest = pathlib.Path(a.output); dest.mkdir(parents=True, exist_ok=True)
    (dest / ('r5-wrapper-' + (nonce or 'nononce') + '.json')).write_text(json.dumps(audit, indent=2))
    print(json.dumps(audit, indent=2))
    return 0 if audit.get('verdict', '').startswith('SUPPORTED') else 2

if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--manager', action='store_true')
    ap.add_argument('--c11-wrapper', action='store_true')
    ap.add_argument('--observer-only-c11', action='store_true')
    ap.add_argument('--provisioner', action='store_true')
    ap.add_argument('--r1', action='store_true')
    ap.add_argument('--live-runner', action='store_true')
    ap.add_argument('--r2-runner', action='store_true')
    ap.add_argument('--pos-case', default='r2gate_pos')
    ap.add_argument('--dead-case', default='r2gate_dead')
    ap.add_argument('--kind')
    ap.add_argument('--r5-wrapper', action='store_true')
    ap.add_argument('--observer-only-r5', action='store_true')
    ap.add_argument('--variant'); ap.add_argument('--outer')
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
    elif args.provisioner:
        provisioner(args)
    elif args.r1:
        sys.exit(run_r1(args))
    elif args.live_runner:
        sys.exit(run_live(args, args.kind))
    elif args.r2_runner:
        sys.exit(run_r2(args, args.kind, args.pos_case, args.dead_case))
    elif args.r5_wrapper:
        sys.exit(r5_wrapper(args))
    elif args.observer_only_r5:
        args.only = 'r5phase'; args.quiet = True
        sys.exit(observer(args))
    else:
        sys.exit(observer(args))
