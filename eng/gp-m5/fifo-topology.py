#!/usr/bin/env python3
"""TEST-ONLY native launchd/FIFO mechanism measurement; not provider qualification."""
import json
import os
from pathlib import Path
import platform
import plistlib
import select
import shutil
import subprocess
import sys
import tempfile
import time
import uuid

output = Path(sys.argv[1]).resolve()
output.parent.mkdir(parents=True, exist_ok=True)
record = {'schema': 'gp-m5-fifo-mechanism/v1', 'sourceSha': os.environ.get('GITHUB_SHA'),
          'host': platform.platform(), 'architecture': platform.machine(),
          'uid': os.getuid(), 'providerQualification': False, 'helpers': [], 'passed': False}
scratch = None
fds = []
loaded = False
removed = False
label = 'org.gagamba.gpm5.mechanism.' + uuid.uuid4().hex
domain = f'gui/{os.getuid()}'
service = domain + '/' + label


def helper(*args):
    p = subprocess.run(['/bin/launchctl', *args], stdin=subprocess.DEVNULL,
                       stdout=subprocess.DEVNULL if args == ('print', domain) else subprocess.PIPE,
                       stderr=subprocess.PIPE, timeout=10)
    stdout = p.stdout or b''
    result = {'args': list(args), 'exitCode': p.returncode,
              'stdout': stdout[:32768].decode(errors='replace'),
              'stderr': p.stderr[:8192].decode(errors='replace')}
    record['helpers'].append(result)
    assert len(stdout) <= 32768 and len(p.stderr) <= 8192, 'helper output exceeded test bound'
    return result


def absent(result):
    return result['exitCode'] != 0 and any(
        s.strip().startswith('Could not find service ') and ' in domain' in s
        for s in (result['stdout'] + '\n' + result['stderr']).splitlines())


try:
    assert platform.system() == 'Darwin' and platform.machine() == 'arm64'
    assert helper('print', domain)['exitCode'] == 0, 'GUI domain unavailable'
    scratch = Path(tempfile.mkdtemp(prefix='gagamba-gpm5-fifo-'))
    assert scratch.stat().st_mode & 0o777 == 0o700
    record['scratch'] = str(scratch)
    record['service'] = service
    nonce = uuid.uuid4().hex
    expected = [b'out-' + nonce.encode(), b'err-' + nonce.encode()]
    paths = [scratch / 'stdout.fifo', scratch / 'stderr.fifo']
    readers, keepers = [], []
    for path in paths:
        os.mkfifo(path, 0o600)
        reader = os.open(path, os.O_RDONLY | os.O_NONBLOCK | os.O_CLOEXEC)
        fds.append(reader)
        keeper = os.open(path, os.O_WRONLY | os.O_NONBLOCK | os.O_CLOEXEC)
        fds.append(keeper)
        readers.append(reader)
        keepers.append(keeper)
    marker = scratch / 'target-entry'
    script = 'printf %s "$1" > "$2"; printf %s "$3"; printf %s "$4" >&2; exit 7'
    plist = scratch / 'job.plist'
    with plist.open('xb') as stream:
        plistlib.dump({'Label': label,
            'ProgramArguments': ['/bin/sh', '-c', script, 'gpm5', nonce, str(marker),
                                 expected[0].decode(), expected[1].decode()],
            'WorkingDirectory': str(scratch), 'StandardOutPath': str(paths[0]),
            'StandardErrorPath': str(paths[1])}, stream)
    assert helper('bootstrap', domain, str(plist))['exitCode'] == 0
    loaded = True
    record['noDispatchBeforeKickstart'] = not marker.exists()
    assert record['noDispatchBeforeKickstart']
    assert helper('kickstart', service)['exitCode'] == 0
    retained = {fd: bytearray() for fd in readers}
    terminal = False
    deadline = time.monotonic() + 15
    while time.monotonic() < deadline:
        for fd in select.select(readers, [], [], 0.05)[0]:
            data = os.read(fd, 4096)
            retained[fd].extend(data)
            assert len(retained[fd]) <= 4096, 'unexpected output beyond fixed mechanism budget'
        state = helper('print', service)
        lines = [line.strip() for line in state['stdout'].splitlines()]
        if any(s in ('state = not running', 'state = exited') for s in lines):
            assert any(s in ('last exit code = 7', 'last exit status = 7') for s in lines)
            terminal = True
            break
    assert terminal, 'no recognized finite target exit'
    helper('bootout', service)
    removed = absent(helper('print', service))
    assert removed, 'bootout absence not confirmed'
    for fd in keepers:
        os.close(fd)
        fds.remove(fd)
    eof = set()
    deadline = time.monotonic() + 3
    while len(eof) != len(readers) and time.monotonic() < deadline:
        for fd in select.select([r for r in readers if r not in eof], [], [], 0.05)[0]:
            data = os.read(fd, 4096)
            if not data:
                eof.add(fd)
            else:
                retained[fd].extend(data)
                assert len(retained[fd]) <= 4096
    record['eofAfterTerminalAndKeeperClose'] = len(eof) == 2
    record['stdoutExact'] = bytes(retained[readers[0]]) == expected[0]
    record['stderrExact'] = bytes(retained[readers[1]]) == expected[1]
    record['targetNonceObserved'] = marker.read_text() == nonce
    record['passed'] = all(record[k] for k in (
        'eofAfterTerminalAndKeeperClose', 'stdoutExact', 'stderrExact', 'targetNonceObserved'))
except Exception as exc:
    record['error'] = f'{type(exc).__name__}: {exc}'
finally:
    if loaded and not removed:
        try:
            helper('bootout', service)
            removed = absent(helper('print', service))
        except Exception as exc:
            record['cleanupError'] = f'{type(exc).__name__}: {exc}'
    descriptor_errors = []
    for fd in fds:
        try:
            os.close(fd)
        except OSError as exc:
            descriptor_errors.append(str(exc))
    record['parentDescriptorsClosed'] = not descriptor_errors
    record['descriptorErrors'] = descriptor_errors
    record['serviceAbsenceConfirmed'] = not loaded or removed
    if scratch and (not loaded or removed):
        try:
            shutil.rmtree(scratch)
            record['scratchRemoved'] = not scratch.exists()
        except OSError as exc:
            record['scratchCleanupError'] = str(exc)
    record['passed'] &= record['serviceAbsenceConfirmed'] and record.get('scratchRemoved', False) and record['parentDescriptorsClosed']
    output.write_text(json.dumps(record, indent=2) + '\n')
    print(json.dumps({'passed': record['passed'], 'evidence': str(output)}))
raise SystemExit(0 if record['passed'] else 1)
