#!/usr/bin/env python3
"""GL-1A lifecycle fixture: one Python program, many process-tree shapes.

Modes: root | tree | tree-child | leaf | root-exits | tree-subgroup
     | escape-session | watchdog
Every process records pid/ppid/pgid/sid/cgroup/role/ready (JSON) and
touches <dir>/<role>.ready when settled. Orchestrator drives lifecycle
(stop files, signals); readiness files gate every kill (no sleeps).
"""
import ctypes
import json
import os
import signal
import subprocess
import sys
import time

SELF = os.path.abspath(__file__)
libc = ctypes.CDLL("libc.so.6", use_errno=True)
PR_SET_PDEATHSIG = 1
SIGKILL_NUM = 9
SIGUSR1_NUM = 10


def peer_record(role, extra=None):
    try:
        with open("/proc/self/cgroup") as f:
            cgroup = f.read().strip().replace("\n", ";")
    except OSError as ex:
        cgroup = f"unreadable:{ex}"
    rec = {
        "pid": os.getpid(),
        "ppid": os.getppid(),
        "pgid": os.getpgid(0),
        "sid": os.getsid(0),
        "cgroup": cgroup,
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


def arm_pdeathsig(signum):
    """Install parent-death signal on OUR parent. Must run before ready."""
    if libc.prctl(PR_SET_PDEATHSIG, signum, 0, 0, 0) != 0:
        err = ctypes.get_errno()
        raise OSError(err, f"prctl(PR_SET_PDEATHSIG) failed: {os.strerror(err)}")
    # Racy by nature (parent may die before we arm); the orchestrator gates
    # kills on our readiness file, which we publish only after arming.


def spawn(mode, directory, role, extra_args=()):
    return subprocess.Popen(
        [sys.executable, SELF, mode, "--dir", directory, "--role", role, *extra_args]
    )


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
    pdeath = 0
    solo_arm = False
    cgroup_base = ""
    on_pdeath = ""
    i = 1
    while i < len(argv):
        if argv[i] == "--dir":
            directory = argv[i + 1]
            i += 2
        elif argv[i] == "--role":
            role = argv[i + 1]
            i += 2
        elif argv[i] == "--pdeathsig":
            pdeath = int(argv[i + 1])
            i += 2
        elif argv[i] == "--solo-arm":
            # Arm self only; do NOT propagate to spawned children (L7b:
            # root-only arming; default propagation is L8's cascade).
            solo_arm = True
            i += 1
        elif argv[i] == "--cgroup-base":
            cgroup_base = argv[i + 1]
            i += 2
        elif argv[i] == "--on-pdeath":
            on_pdeath = argv[i + 1]
            i += 2
        else:
            i += 1
    if not directory:
        print("fixture: --dir required", file=sys.stderr)
        return 2
    os.makedirs(directory, exist_ok=True)
    stopfile = os.path.join(directory, "stop")

    if pdeath:
        arm_pdeathsig(pdeath)

    if mode == "root":
        publish(directory, role, peer_record(role))
        idle_forever(stopfile)
        return 0

    if mode in ("tree", "root-exits"):
        inherit = () if solo_arm else (("--pdeathsig", str(pdeath)) if pdeath else ())
        child = spawn("tree-child", directory, role + "-child", inherit)
        if not wait_ready(directory, role + "-child"):
            print("fixture: child never ready", file=sys.stderr)
            return 1
        publish(directory, role, peer_record(role, {"child_pid": child.pid}))
        if mode == "root-exits":
            return 0  # exit while descendants remain alive
        idle_forever(stopfile)
        return 0

    if mode == "tree-child":
        inherit = () if solo_arm else (("--pdeathsig", str(pdeath)) if pdeath else ())
        leaf = spawn("leaf", directory, role + "-leaf", inherit)
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

    if mode == "tree-subgroup":
        # Create a nested cgroup, move self into it, then spawn the leaf
        # (which inherits the subgroup): demonstrates recursive membership.
        subgroup = os.path.join(cgroup_base, "sub")
        moved = False
        move_error = ""
        try:
            os.makedirs(subgroup, exist_ok=True)
            with open(os.path.join(subgroup, "cgroup.procs"), "w") as f:
                f.write(str(os.getpid()))
            moved = True
        except OSError as ex:
            move_error = f"{type(ex).__name__}: {ex}"
        leaf = spawn("leaf", directory, role + "-leaf",
                     ("--pdeathsig", str(pdeath)) if pdeath else ())
        if not wait_ready(directory, role + "-leaf"):
            print("fixture: leaf never ready", file=sys.stderr)
            return 1
        publish(directory, role, peer_record(
            role, {"leaf_pid": leaf.pid, "subgroup": subgroup,
                   "moved": moved, "move_error": move_error}))
        idle_forever(stopfile)
        return 0

    if mode == "escape-session":
        leaf = spawn("leaf", directory, role + "-leaf",
                     ("--pdeathsig", str(pdeath)) if pdeath else ())
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

    if mode == "watchdog":
        # Trusted reaper (L14): on parent death (SIGUSR1), write the
        # workload cgroup.kill file. Sleeps until then or stop file.
        fired = []
        watched = on_pdeath

        def handler(signum, frame):
            try:
                with open(watched, "w") as f:
                    f.write("1")
                fired.append(True)
            except OSError:
                pass

        signal.signal(SIGUSR1_NUM, handler)
        publish(directory, role, peer_record(role, {"watching": watched}))
        while not os.path.exists(stopfile) and not fired:
            time.sleep(0.2)
        publish(directory, role + "-fired",
                peer_record(role + "-fired", {"fired": bool(fired)}))
        return 0

    print(f"fixture: unknown mode {mode}", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
