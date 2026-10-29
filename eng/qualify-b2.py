#!/usr/bin/env python3
"""Bounded B2 process qualification. Run as a normal Linux user; no cgroup provisioning.
Explicit build directories only. No production installation/attestation is implied.
"""
import argparse, array, base64, ctypes, hashlib, json, os, pathlib, select
import signal, socket, struct, subprocess, sys, time, unittest, uuid

parser = argparse.ArgumentParser()
parser.add_argument('--installation', type=pathlib.Path, required=True)
parser.add_argument('--delegated-parent', type=pathlib.Path)
parser.add_argument('--artifacts', type=pathlib.Path, required=True)
options = parser.parse_args()
INSTALL = options.installation.resolve()
ARTIFACTS = options.artifacts.resolve()
if sys.platform != 'linux' or os.getuid() == 0:
    raise SystemExit('Requires unprivileged Linux; Windows cannot qualify B2')
ARTIFACTS.mkdir(parents=True, exist_ok=True)
# This dedicated test supervisor adopts ONLY its fixtures and explicitly reaps each known PID.
if ctypes.CDLL(None).prctl(36, 1, 0, 0, 0) != 0:
    raise SystemExit('Test supervisor subreaper unavailable')
RESULTS = []

def pin(folder, entry):
    directory = INSTALL / folder
    names = [entry+'.dll', entry+'.deps.json', entry+'.runtimeconfig.json',
             'Gagamba.Execution.Linux.M1.dll', 'Gagamba.Execution.dll']
    return {'Directory': str(directory), 'Sha256': {name: hashlib.sha256((directory/name).read_bytes()).hexdigest() for name in names}}

RECOVERY = pin('recovery', 'Gagamba.Execution.Linux.M1.Recovery')
OWNER = pin('owner', 'Gagamba.Execution.Linux.M1.Owner')
CONTROLLER = pin('controller', 'Gagamba.Execution.Linux.M1.B2Controller')

def config(directory):
    return dict(Version=1, Execution=str(uuid.uuid4()), Generation=str(uuid.uuid4()), Session=str(uuid.uuid4()),
                ControllerPid=os.getpid(), ControllerUid=os.getuid(), Secret=base64.b64encode(os.urandom(32)).decode(),
                Recovery=RECOVERY, Owner=OWNER, EvidenceDirectory=str(directory))

def wait_for(predicate, seconds=8):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        value = predicate()
        if value: return value
        time.sleep(.02)
    raise AssertionError('Bounded fixture observation timed out')

def read_json(path):
    try: return json.loads(path.read_text())
    except FileNotFoundError: return None

def alive(fd): return not select.select([fd], [], [], 0)[0]

def terminate(fd):
    if alive(fd): signal.pidfd_send_signal(fd, signal.SIGKILL)
    wait_for(lambda: not alive(fd), 5)

class Fixture:
    def __init__(self, name, withhold=False, delegated=None):
        self.directory = ARTIFACTS / (name + '-' + uuid.uuid4().hex)
        self.directory.mkdir(mode=0o700)
        self.pids = {}; self.fds = {}; self.records = {}
        if withhold: (self.directory/'withhold-ack').touch()
        self.controller = subprocess.Popen(['/usr/bin/dotnet', str(INSTALL/'controller/Gagamba.Execution.Linux.M1.B2Controller.dll'), '--controller'],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        self.track('controller', self.controller.pid)
        try:
            cfg = config(self.directory)
            if delegated is not None: cfg['DelegatedParent'] = str(delegated)
            self.controller.stdin.write(json.dumps(cfg)+'\n'); self.controller.stdin.flush()
            wait_for(lambda: (self.directory/'controller-ready.json').exists() or self.controller.poll() is not None)
            if self.controller.poll() is not None:
                raise AssertionError('Controller bootstrap refused: ' + self.controller.stderr.read())
            ready = wait_for(lambda: read_json(self.directory/'ready.json'))
            self.track('recovery', ready['RecoveryPid']); self.track('owner', ready['OwnerPid'])
            self.ready = ready
        except:
            self.close(); raise
    def track(self, role, pid):
        self.pids[role] = pid; self.fds[role] = os.pidfd_open(pid)
    def command(self, value):
        self.controller.stdin.write(value+'\n'); self.controller.stdin.flush()
    def kill(self, role): terminate(self.fds[role])
    def json(self, name): return read_json(self.directory/name)
    def wait(self, name): return wait_for(lambda: self.json(name))
    def close(self):
        # Send no PID-only kill. Retained fixture pidfds are the only signal authority.
        if 'controller' in self.fds: terminate(self.fds['controller'])
        if getattr(self, 'ready', {}).get('RetainedDomains', 0):
            wait_for(lambda: (s := self.json('stop.json')) and s['DomainRemoved'])
        for role in ['recovery', 'owner']:
            if role in self.fds: terminate(self.fds[role])
        self.controller.wait(timeout=5)
        for role in ['recovery', 'owner']:
            if role in self.pids:
                try: os.waitpid(self.pids[role], 0)
                except ChildProcessError: pass  # Already reaped by its original owner.
        for fd in self.fds.values(): os.close(fd)
        for stream in [self.controller.stdin, self.controller.stdout, self.controller.stderr]: stream.close()
        leaked = [pid for pid in self.pids.values() if pathlib.Path('/proc', str(pid)).exists()]
        if leaked: raise AssertionError('Fixture process cleanup incomplete: ' + str(leaked))
        RESULTS.append({'case': self.directory.name, 'pids': self.pids, 'cleanup': 'all known fixture processes reaped',
                        'observations': {p.name: json.loads(p.read_text()) for p in self.directory.glob('*.json')}})
    def __enter__(self): return self
    def __exit__(self, *unused): self.close()

class B2Processes(unittest.TestCase):
    def test_controller_sigkill_recovery_survives_and_owner_stops(self):
        with Fixture('controller-loss') as f:
            self.assertEqual(f.ready['RecoveryPid'], f.ready['SessionId'])
            self.assertTrue(f.ready['Subreaper']); self.assertFalse(f.ready['OwnerExited'])
            self.assertFalse(f.ready['ReleaseAuthority'])
            self.assertEqual(int(pathlib.Path('/proc', str(f.pids['owner']), 'stat').read_text().split()[3]), f.pids['recovery'])
            f.kill('controller'); f.wait('owner-stop.json')
            stopped = wait_for(lambda: (s if (s := f.json('stop.json')) and s['ControllerLost'] and s['OwnerExited'] else None))
            self.assertTrue(alive(f.fds['recovery'])); self.assertEqual(stopped['StopCount'], 1)
            deadline = stopped['StopDeadline']; time.sleep(.15)
            self.assertEqual(f.wait('stop.json')['StopDeadline'], deadline)
            self.assertEqual(f.wait('owner-stop.json')['NativeStop'], 'Unproven')
    def test_owner_sigkill_pidfd_exit_recovery_survives(self):
        with Fixture('owner-loss') as f:
            self.assertFalse(f.ready['OwnerExited']); f.kill('owner')
            exited = f.wait('owner-exit.json')
            self.assertTrue(exited['OwnerExited']); self.assertTrue(alive(f.fds['recovery']))
            self.assertEqual(exited['StopCount'], 1); self.assertFalse(exited['ReleaseAuthority'])
            self.assertTrue(alive(f.fds['controller'])); self.assertFalse(exited['ControllerLost'])
            self.assertIn(exited['Cause'], ['OwnerExited', 'OwnerTransportLost'])
    def test_recovery_sigkill_surviving_owner_detects_lifeline(self):
        with Fixture('recovery-loss') as f:
            self.assertTrue(alive(f.fds['owner'])); f.kill('recovery')
            observed = f.wait('owner-stop.json')
            self.assertEqual(observed['Cause'], 'RecoveryLifelineLost')
            self.assertEqual(observed['StopCount'], 1); self.assertFalse(observed['ReleaseAuthority'])
    def test_control_eof_while_controller_alive(self):
        with Fixture('transport-close') as f:
            f.command('close'); stopped = f.wait('stop.json'); f.wait('owner-stop.json')
            self.assertTrue(alive(f.fds['controller'])); self.assertTrue(alive(f.fds['recovery']))
            self.assertTrue(stopped['ControllerLost']); self.assertEqual(stopped['StopCount'], 1)
    def test_lost_acknowledgement_stays_unknown(self):
        with Fixture('lost-ack', withhold=True) as f:
            f.command('stop'); f.wait('owner-stop.json')
            stopped = wait_for(lambda: (s if (s := f.json('stop.json')) and s['OwnerExited'] else None))
            self.assertFalse(stopped['OwnerAcknowledged']); self.assertEqual(stopped['NativeStop'], 'Unproven')
    def test_duplicate_stops_do_not_renew_deadline(self):
        with Fixture('duplicate-stop') as f:
            f.command('stop'); first = f.wait('stop.json')
            f.command('stop'); f.command('stop'); time.sleep(.1)
            last = f.wait('stop.json'); self.assertEqual(last['StopCount'], 1)
            self.assertEqual(first['StopDeadline'], last['StopDeadline'])
    def test_reconnect_refuses_and_cannot_gain_release(self):
        with Fixture('reconnect') as f:
            f.command('reconnect'); stopped = f.wait('stop.json')
            self.assertEqual(stopped['Cause'], 'ControllerAssociationRefused'); self.assertFalse(stopped['ReleaseAuthority'])
    def test_malformed_transport_requests_stop(self):
        with Fixture('malformed') as f:
            f.command('malformed'); stopped = f.wait('stop.json')
            self.assertEqual(stopped['Cause'], 'InvalidControllerFrame'); f.wait('owner-stop.json')
    def test_observation_backpressure_cannot_block_safety(self):
        with Fixture('backpressure') as f:
            f.command('flood'); stopped = f.wait('stop.json'); f.wait('owner-stop.json')
            self.assertEqual(stopped['StopCount'], 1); self.assertTrue(alive(f.fds['recovery']))
    def test_observer_cancellation_does_not_stop_operation(self):
        with Fixture('cancel-observer') as f:
            f.command('cancel-observer')
            wait_for(lambda: (f.directory/'observer-cancelled').exists())
            self.assertFalse((f.directory/'stop.json').exists())
            self.assertTrue(alive(f.fds['owner'])); self.assertTrue(alive(f.fds['recovery']))
    def test_duplicate_association_cannot_restore_generation(self):
        with Fixture('duplicate-association') as f:
            f.command('reassociate'); stopped = f.wait('stop.json')
            self.assertEqual(stopped['Cause'], 'ControllerAssociationRefused'); self.assertFalse(stopped['ReleaseAuthority'])
    def test_replayed_sequence_does_not_renew_deadline(self):
        with Fixture('replayed-sequence') as f:
            f.command('replay-stop'); first = f.wait('stop.json'); time.sleep(.1)
            last = f.wait('stop.json'); self.assertEqual(last['StopCount'], 1)
            self.assertEqual(first['StopDeadline'], last['StopDeadline'])
    def test_delayed_owner_ack_does_not_renew_deadline(self):
        with Fixture('delayed-ack') as f:
            signal.pidfd_send_signal(f.fds['owner'], signal.SIGSTOP)
            f.command('stop'); first = f.wait('stop.json'); self.assertFalse(first['OwnerAcknowledged'])
            time.sleep(.15); signal.pidfd_send_signal(f.fds['owner'], signal.SIGCONT)
            f.wait('owner-stop.json'); last = wait_for(lambda: (s if (s := f.json('stop.json')) and s['OwnerExited'] else None))
            self.assertEqual(first['StopDeadline'], last['StopDeadline']); self.assertEqual(last['StopCount'], 1)

    def raw_refusal(self, name, mutate=None, bad_auth=False, roles=None, frame_change=None, wrong_peer=False):
        directory = ARTIFACTS/(name+'-'+uuid.uuid4().hex); directory.mkdir(mode=0o700)
        c, child_c = socket.socketpair(socket.AF_UNIX, socket.SOCK_SEQPACKET)
        s, child_s = socket.socketpair(socket.AF_UNIX, socket.SOCK_SEQPACKET)
        if wrong_peer:
            carrier, sender = socket.socketpair(socket.AF_UNIX, socket.SOCK_SEQPACKET)
            creator = os.fork()
            if creator == 0:
                carrier.close()
                a, b = socket.socketpair(socket.AF_UNIX, socket.SOCK_SEQPACKET)
                sender.sendmsg([b'roles'], [(socket.SOL_SOCKET, socket.SCM_RIGHTS, array.array('i', [a.fileno(), b.fileno()]))])
                os._exit(0)
            sender.close()
            _, ancillary, _, _ = carrier.recvmsg(16, socket.CMSG_SPACE(8))
            received = array.array('i'); received.frombytes(ancillary[0][2])
            os.waitpid(creator, 0); carrier.close(); c.close(); child_c.close()
            c, child_c = (socket.socket(fileno=fd) for fd in received)
        cfg = config(directory)
        if mutate: mutate(cfg)
        env = os.environ.copy(); env['GAGAMBA_B2_CONTROL_FD'] = str(child_c.fileno()); env['GAGAMBA_B2_CREDENTIAL_FD'] = str(child_s.fileno())
        if roles: roles(env, child_c.fileno(), child_s.fileno())
        child = subprocess.Popen(['/usr/bin/dotnet', str(INSTALL/'recovery/Gagamba.Execution.Linux.M1.Recovery.dll')],
            env=env, pass_fds=(child_c.fileno(), child_s.fileno()), stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        child_c.close(); child_s.close()
        try:
            try: s.send(json.dumps(cfg).encode())
            except OSError: pass
            payload = uuid.UUID(cfg['Session']).bytes_le + (bytes(32) if bad_auth else base64.b64decode(cfg['Secret']))
            frame = struct.pack('<IHBB', 0x4D314F50 ^ 0x20, 1, 1, 0) + uuid.UUID(cfg['Execution']).bytes_le + uuid.UUID(cfg['Generation']).bytes_le + struct.pack('<II', 1, len(payload)) + payload
            if frame_change: frame = frame_change(frame)
            try: c.send(frame)
            except OSError: pass
            out, err = child.communicate(timeout=8)
            self.assertEqual(child.returncode, 3); self.assertFalse((directory/'ready.json').exists())
            self.assertNotIn(cfg['Secret'].encode(), out+err)
            RESULTS.append({'case': name, 'pids': {'recovery': child.pid}, 'refused': True, 'cleanup': 'reaped'})
        finally:
            if child.poll() is None: child.kill(); child.wait(timeout=5)
            c.close(); s.close()
    def test_invalid_os_peer_identity_refuses(self): self.raw_refusal('wrong-os-peer', wrong_peer=True)
    def test_duplicate_inherited_descriptor_roles_refuse(self):
        self.raw_refusal('duplicate-roles', roles=lambda env, c, s: env.update(GAGAMBA_B2_CREDENTIAL_FD=str(c)))
    def test_missing_independent_channel_refuses(self):
        self.raw_refusal('missing-role', roles=lambda env, c, s: env.pop('GAGAMBA_B2_CREDENTIAL_FD'))
    def test_oversized_native_record_refuses(self): self.raw_refusal('oversized-record', frame_change=lambda f: f + bytes(70000))
    def test_wrong_native_protocol_version_refuses(self):
        self.raw_refusal('wrong-wire-version', frame_change=lambda f: f[:4] + b'\x02\x00' + f[6:])
    def test_wrong_session_secret_refuses_before_owner_creation(self): self.raw_refusal('bad-secret', bad_auth=True)
    def test_wrong_controller_identity_refuses(self): self.raw_refusal('bad-controller', lambda c: c.update(ControllerPid=os.getpid()+100000))
    def test_changed_executable_pin_refuses(self):
        def change(c):
            c['Owner'] = json.loads(json.dumps(c['Owner'])); c['Owner']['Sha256']['Gagamba.Execution.Linux.M1.Owner.dll'] = '0'*64
        self.raw_refusal('bad-installation', change)
    def test_invalid_delegation_refuses_without_provisioning(self): self.raw_refusal('bad-delegation', lambda c: c.update(DelegatedParent=str(ARTIFACTS)))

class B2Domains(unittest.TestCase):
    def test_controller_loss_retains_real_domain_and_writes_kill(self):
        with Fixture('domain-controller-loss', delegated=options.delegated_parent) as f:
            self.assertEqual(f.ready['RetainedDomains'], 1)
            targets = [os.readlink(p) for p in pathlib.Path('/proc', str(f.pids['recovery']), 'fd').iterdir()]
            self.assertTrue(any('/b2-' in p and 'cgroup.kill' in p for p in targets))
            self.assertFalse(any('/b2-' in p for p in pathlib.Path('/proc', str(f.pids['recovery']), 'cgroup').read_text().splitlines()))
            self.assertFalse(any('/b2-' in p for p in pathlib.Path('/proc', str(f.pids['owner']), 'cgroup').read_text().splitlines()))
            f.kill('controller')
            last = wait_for(lambda: (s if (s := f.json('stop.json')) and s['DomainRemoved'] else None))
            self.assertEqual(last['NativeStop'], 'KillWrittenEmptyObserved')
            self.assertTrue(last['ControllerLost']); self.assertTrue(last['OwnerExited'])
            self.assertFalse(last['OwnerAcknowledged']); self.assertTrue(alive(f.fds['recovery']))
    def test_owner_exit_retained_domain_stop_and_identity_checked_removal(self):
        with Fixture('domain-owner-loss', delegated=options.delegated_parent) as f:
            f.kill('owner')
            last = wait_for(lambda: (s if (s := f.json('stop.json')) and s['DomainRemoved'] else None))
            self.assertEqual(last['NativeStop'], 'KillWrittenEmptyObserved')
            self.assertTrue(last['OwnerExited']); self.assertFalse(last['ReleaseAuthority'])
            self.assertTrue(alive(f.fds['controller'])); self.assertFalse(last['ControllerLost'])
            self.assertIn(last['Cause'], ['OwnerExited', 'OwnerTransportLost'])

suite = unittest.defaultTestLoader.loadTestsFromTestCase(B2Processes)
if options.delegated_parent:
    suite.addTests(unittest.defaultTestLoader.loadTestsFromTestCase(B2Domains))
result = unittest.TextTestRunner(verbosity=2).run(suite)
report = {'environment': {'kernel': os.uname().release, 'uid': os.getuid(), 'platform': 'wsl2' if 'microsoft' in os.uname().release else 'linux'},
          'delegated_parent': str(options.delegated_parent) if options.delegated_parent else 'NOT RUN: no operator delegation supplied',
          'installation': {'recovery': RECOVERY, 'owner': OWNER, 'controller': CONTROLLER},
          'runtime_executables': {path: hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest() for path in ['/usr/bin/dotnet', sys.executable]},
          'tests': {'passed': result.testsRun-len(result.errors)-len(result.failures), 'failed': len(result.errors)+len(result.failures), 'skipped': len(result.skipped)}, 'runs': RESULTS}
(ARTIFACTS/'results.json').write_text(json.dumps(report, indent=2))
raise SystemExit(0 if result.wasSuccessful() else 1)
