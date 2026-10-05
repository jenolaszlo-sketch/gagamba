#!/usr/bin/env python3
"""GM-1A lifecycle fixture (macOS): same vocabulary as GL-1A, minus Linux.

Records pid/ppid/pgid/sid/role/ready (NO cgroup: macOS has no equivalent;
NO pdeathsig: no public macOS counterpart). Modes: root | tree |
tree-child | leaf | root-exits | escape-session. Readiness files gate
every kill. PPID is recorded but never used as ownership (Darwin
reparents to a system process after creator exit).
"""
import json
import os
import subprocess
import sys
import time

SELF = os.path.abspath(__file__)


def peer_record(role, extra=None):
    rec = {
        "pid": os.getpid(),
        "ppid": os.getppid(),
        "pgid": os.getpgid(0),
        "sid": os.getsid(0),
        "role": role,
        "ready_ts": time.time(),
    }
    if extra:
        rec.update(extra)
    return rec


def publish(directory, role, rec):
    with open(os.path.join(directory, role + ".json"), "w") as f:
        json.dump(rec, f)
    with open(os.path.join(directory, role + ".ready"), "w") as f:
        f.write(str(rec["pid"]))


def spawn(mode, directory, role):
    return subprocess.Popen(
        [sys.executable, SELF, mode, "--dir", directory, "--role", role])


def wait_ready(directory, role, timeout=15.0):
    path = os.path.join(directory, role + ".ready")
    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(path):
            return True
        time.sleep(0.05)
    return False


def idle_forever(stopfile):
    while not os.path.exists(stopfile):
        time.sleep(0.2)


def main(argv):
    mode = argv[0]
    directory = ""
    role = mode
    i = 1
    while i < len(argv):
        if argv[i] == "--dir":
            directory = argv[i + 1]
            i += 2
        elif argv[i] == "--role":
            role = argv[i + 1]
            i += 2
        else:
            i += 1
    if not directory:
        print("fixture: --dir required", file=sys.stderr)
        return 2
    os.makedirs(directory, exist_ok=True)
    stopfile = os.path.join(directory, "stop")

    if mode == "root":
        publish(directory, role, peer_record(role))
        idle_forever(stopfile)
        return 0

    if mode in ("tree", "root-exits"):
        child = spawn("tree-child", directory, role + "-child")
        if not wait_ready(directory, role + "-child"):
            print("fixture: child never ready", file=sys.stderr)
            return 1
        publish(directory, role, peer_record(role, {"child_pid": child.pid}))
        if mode == "root-exits":
            return 0  # exit while descendants remain alive
        idle_forever(stopfile)
        return 0

    if mode == "tree-child":
        leaf = spawn("leaf", directory, role + "-leaf")
        if not wait_ready(directory, role + "-leaf"):
            print("fixture: leaf never ready", file=sys.stderr)
            return 1
        publish(directory, role, peer_record(role, {"leaf_pid": leaf.pid}))
        idle_forever(stopfile)
        return 0

    if mode == "leaf":
        publish(directory, role, peer_record(role))
        idle_forever(stopfile)
        return 0

    if mode == "escape-session":
        leaf = spawn("leaf", directory, role + "-leaf")
        if not wait_ready(directory, role + "-leaf"):
            print("fixture: leaf never ready", file=sys.stderr)
            return 1
        # Adversarial: leave our process group/session (fails if leader).
        escaped = False
        escape_error = ""
        try:
            os.setsid()
            escaped = True
        except OSError as ex:
            escape_error = f"{type(ex).__name__}: {ex}"
        publish(directory, role, peer_record(
            role, {"leaf_pid": leaf.pid, "escaped": escaped,
                   "escape_error": escape_error}))
        idle_forever(stopfile)
        return 0

    print(f"fixture: unknown mode {mode}", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
