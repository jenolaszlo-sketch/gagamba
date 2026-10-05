#!/usr/bin/env python3
"""GL-1A lifecycle probe: capability + L1-L14 characterization matrix.

The orchestrator is the outer observer. A supervisor WRAPPER (inline
python) sits between orchestrator and workload root in every
supervisor-death leg: the orchestrator kills the wrapper, never the root
directly. Readiness files gate every kill (no sleeps). Exit 0 iff every
leg matches its expected discriminator (L4/L7/L13 expect SURVIVAL - that
is the finding, not a failure).
"""
import json
import os
import signal
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
FIXTURE = os.path.join(HERE, "fixture.py")
SIGKILL = signal.SIGKILL
SIGUSR1 = signal.SIGUSR1


def read_record(directory, role):
    path = os.path.join(directory, role + ".json")
    if not os.path.exists(path):
        return None
    try:
        with open(path) as f:
            return json.load(f)
    except (OSError, ValueError):
        return None


def wait_record(directory, role, timeout=15.0):
    """Readiness file AND parsable record (closes the publish race)."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(os.path.join(directory, role + ".ready")):
            rec = read_record(directory, role)
            if rec is not None:
                return rec
        time.sleep(0.05)
    return read_record(directory, role)


def proc_state(pid):
    """None=gone, else single-letter state (Z counts as dead: reaped soon)."""
    try:
        with open(f"/proc/{pid}/stat") as f:
            parts = f.read().rsplit(")", 1)
            return parts[1].split()[0]
    except (FileNotFoundError, ProcessLookupError, IndexError):
        return None


def alive(pid):
    st = proc_state(pid)
    return st is not None and st != "Z"


def kill_pid(pid, sig=SIGKILL):
    try:
        os.kill(pid, sig)
        return True
    except (ProcessLookupError, PermissionError):
        return False


def kill_group(pgid, sig=SIGKILL):
    # Never signal our own group/session or init: a wrong pgid here would
    # suicide the orchestrator or spray the host. Fresh sessions per
    # supervisor make collisions impossible; assert anyway.
    assert pgid not in (0, 1, os.getpgrp(), os.getsid(0)), f"refusing killpg({pgid})"
    try:
        os.killpg(pgid, sig)
        return True
    except (ProcessLookupError, PermissionError):
        return False


def wait_all_dead(pids, timeout=10.0):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if not any(alive(p) for p in pids):
            return True
        time.sleep(0.1)
    return not any(alive(p) for p in pids)


def wait_all_alive(pids, timeout=10.0):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if all(alive(p) for p in pids):
            return True
        time.sleep(0.1)
    return all(alive(p) for p in pids)


def reap(proc, timeout=10.0):
    try:
        return proc.wait(timeout=timeout)
    except Exception:
        return None


def spawn_wrapped(directory, child_args):
    """Supervisor wrapper in its own session: spawns the fixture child and
    waits. The orchestrator kills the WRAPPER; the fixture child is root.
    The wrapper never calls setsid itself, so no EPERM hazard."""
    code = ("import subprocess, sys;"
            f"p=subprocess.Popen([sys.executable, {FIXTURE!r}, *{tuple(child_args)!r}]);"
            "p.wait()")
    return subprocess.Popen([sys.executable, "-c", code], start_new_session=True)


class Ctx:
    def __init__(self, base):
        self.base = base
        self.cg_root = "/sys/fs/cgroup/gagamba-gl1a"

    def rundir(self, leg):
        d = os.path.join(self.base, leg)
        os.makedirs(d, exist_ok=True)
        return d

    def cg(self, *parts):
        return os.path.join(self.cg_root, *parts)

    def cg_create(self, *parts):
        p = self.cg(*parts)
        os.makedirs(p, exist_ok=True)
        return p

    def cg_write_procs(self, path, pid):
        with open(os.path.join(path, "cgroup.procs"), "w") as f:
            f.write(str(pid))

    def cg_kill(self, path):
        with open(os.path.join(path, "cgroup.kill"), "w") as f:
            f.write("1")

    def cg_members(self, path):
        out = []
        for root, _dirs, _files in os.walk(path):
            try:
                with open(os.path.join(root, "cgroup.procs")) as f:
                    out.extend(int(x) for x in f.read().split())
            except (OSError, ValueError):
                pass
        return out

    def cg_remove(self, *parts):
        p = self.cg(*parts)
        try:
            os.rmdir(p)
            return True
        except OSError:
            return False

    def cg_sweep(self):
        """Kill everything under our test root, then remove what is empty."""
        try:
            members = self.cg_members(self.cg_root)
        except OSError:
            return []
        for p in members:
            kill_pid(p)
        wait_all_dead(members, timeout=10.0)
        for root, dirs, _files in os.walk(self.cg_root, topdown=False):
            for d in dirs:
                try:
                    os.rmdir(os.path.join(root, d))
                except OSError:
                    pass
        try:
            os.rmdir(self.cg_root)
        except OSError:
            pass
        return [p for p in members if alive(p)]


def capability(ctx):
    """Do not assume writable cgroups just because v2 is mounted."""
    notes = []
    out = {}
    try:
        with open("/proc/self/cgroup") as f:
            out["self_cgroup"] = f.read().strip().replace("\n", ";")
    except OSError as ex:
        out["self_cgroup"] = f"unreadable:{ex}"
    out["cgroup_v2_mounted"] = os.path.exists("/sys/fs/cgroup/cgroup.controllers")
    probe = None
    try:
        probe = ctx.cg_create("probe-cap")
        out["cgroupKillPresent"] = os.path.exists(os.path.join(probe, "cgroup.kill"))
        out["delegatedSubtreeAvailable"] = True
        out["delegationMechanism"] = "root-owned (probe runs as root; unprivileged path would be systemd Delegate=yes)"
        notes.append(f"probe cgroup ok; kill present={out['cgroupKillPresent']}")
    except OSError as ex:
        out["cgroupKillPresent"] = False
        out["delegatedSubtreeAvailable"] = False
        out["delegationMechanism"] = f"unavailable: {ex}"
        notes.append(f"probe cgroup FAILED: {ex}")
    finally:
        try:
            os.rmdir(probe)
        except OSError:
            pass
    out["cgroupVersion"] = 2 if out["cgroup_v2_mounted"] else 0
    passed = bool(out["cgroup_v2_mounted"] and out["cgroupKillPresent"]
                  and out["delegatedSubtreeAvailable"])
    return passed, notes, out


def tree_pids(directory):
    root = wait_record(directory, "root")
    child = wait_record(directory, "root-child", timeout=5.0)
    leaf = wait_record(directory, "root-child-leaf")
    return root, child, leaf


def leg_pg_tree(ctx):
    d = ctx.rundir("L1")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        same = root["pgid"] == leaf["pgid"]
        ok = same and root["pgid"] != os.getpgrp()
        notes = [f"sup={sup.pid} root-pgid={root['pgid']} leaf-pgid={leaf['pgid']} "
                 f"(own group, not orchestrator's)"]
        kill_group(root["pgid"])
        dead = wait_all_dead([root["pid"], leaf["pid"]], timeout=10.0)
        reap(sup)
        return ok and dead, notes, {"supervisor": sup.pid, "root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_root_exits(ctx):
    d = ctx.rundir("L2")
    sup = subprocess.Popen(
        [sys.executable, FIXTURE, "root-exits", "--dir", d, "--role", "root"],
        start_new_session=True)
    try:
        leaf = wait_record(d, "root-child-leaf")
        if leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["leaf record missing"], {}
        rc = sup.wait(timeout=15)
        ok = rc == 0 and alive(leaf["pid"])
        notes = [f"supervisor exited rc={rc}, leaf alive={alive(leaf['pid'])} pgid={leaf['pgid']}"]
        kill_group(leaf["pgid"])
        wait_all_dead([leaf["pid"]], timeout=10.0)
        return ok, notes, {"leaf": leaf}
    finally:
        try:
            reap(sup)
        except Exception:
            pass


def leg_pg_killpg(ctx):
    d = ctx.rundir("L3")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        root, child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        pids = [root["pid"]] + ([child["pid"]] if child else []) + [leaf["pid"]]
        pgid = root["pgid"]
        kill_pid(sup.pid)
        reap(sup)
        kill_group(pgid)
        dead = wait_all_dead(pids, timeout=10.0)
        return dead, [f"killpg({pgid}) all-dead={dead}"], {"pids": pids}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_supervisor_dies(ctx):
    d = ctx.rundir("L4")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        kill_pid(sup.pid)
        reap(sup)
        # EXPECTED: workload survives (no owner-death semantic). That is the
        # finding; survival here is a pass.
        survived = wait_all_alive([root["pid"], leaf["pid"]], timeout=5.0)
        notes = [f"supervisor killed; root/leaf alive={survived} (expected: survive)"]
        kill_group(root["pgid"])
        wait_all_dead([root["pid"], leaf["pid"]], timeout=10.0)
        return survived, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_escape(ctx):
    d = ctx.rundir("L5")
    # Escort (own session, never setsid itself) spawns the escaper, which is
    # an ordinary group member and CAN setsid.
    sup = spawn_wrapped(d, ["escape-session", "--dir", d, "--role", "root"])
    try:
        root = wait_record(d, "root")
        leaf = wait_record(d, "root-leaf")
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        if not root.get("escaped"):
            kill_pid(sup.pid)
            reap(sup)
            return False, [f"setsid failed: {root.get('escape_error')}"], {"root": root}
        pgid = leaf["pgid"]  # escort's group: leaf still in it
        kill_pid(sup.pid)
        reap(sup)
        kill_group(pgid)
        esc_alive = wait_all_alive([root["pid"]], timeout=5.0)
        leaf_dead = wait_all_dead([leaf["pid"]], timeout=10.0)
        notes = [f"escaper sid={root['sid']} alive={esc_alive} (expected: survive); "
                 f"leaf dead={leaf_dead}"]
        kill_pid(root["pid"])
        wait_all_dead([root["pid"]], timeout=10.0)
        return esc_alive and leaf_dead, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pd_root_dies(ctx):
    d = ctx.rundir("L6")
    sup = spawn_wrapped(d, ["root", "--dir", d, "--role", "root",
                            "--pdeathsig", str(int(SIGKILL))])
    try:
        root = wait_record(d, "root")
        if root is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["root record missing (not armed?)"], {}
        kill_pid(sup.pid)
        reap(sup)
        dead = wait_all_dead([root["pid"]], timeout=10.0)
        return dead, [f"supervisor killed; armed root dead={dead} (expected: die)"], {"root": root}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pd_solo_arm(ctx):
    d = ctx.rundir("L7b")
    # Root-only arming: root arms on the WRAPPER supervisor and does NOT
    # propagate (solo-arm); deeper generations stay unarmed. Kill wrapper
    # -> root dies, deeper survive. The true L7 discriminator.
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root",
                            "--pdeathsig", str(int(SIGKILL)), "--solo-arm"])
    try:
        root, child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        kill_pid(sup.pid)
        reap(sup)
        root_dead = wait_all_dead([root["pid"]], timeout=10.0)
        deeper = ([child["pid"]] if child else []) + [leaf["pid"]]
        deeper_alive = wait_all_alive(deeper, timeout=5.0)
        notes = [f"root dead={root_dead} (expected); deeper alive={deeper_alive} (expected: survive)"]
        cleanup_tree(deeper)
        return root_dead and deeper_alive, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pd_cascade(ctx):
    d = ctx.rundir("L8")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root",
                            "--pdeathsig", str(int(SIGKILL))])
    try:
        root, child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        # Fixture propagates the flag down its own spawns: every generation
        # is armed. Cooperative only: arbitrary executables would not do
        # this (setting clears on fork).
        kill_pid(sup.pid)
        reap(sup)
        pids = [root["pid"]] + ([child["pid"]] if child else []) + [leaf["pid"]]
        all_dead = wait_all_dead(pids, timeout=15.0)
        notes = [f"cascade all-dead={all_dead} (cooperative-only: every generation armed explicitly)"]
        cleanup_tree(pids)
        return all_dead, notes, {"root": root, "child": child}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_cg_tree(ctx):
    d = ctx.rundir("L9")
    cg = ctx.cg_create("gl1a-L9", "workload")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        ctx.cg_write_procs(cg, sup.pid)
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        members = ctx.cg_members(cg)
        ok = root["pid"] in members and leaf["pid"] in members
        notes = [f"workload members={sorted(members)}"]
        kill_pid(sup.pid)
        reap(sup)
        cleanup_tree([root["pid"], leaf["pid"]])
        return ok, notes, {"root": root, "leaf": leaf, "members": sorted(members)}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)
        ctx.cg_remove("gl1a-L9", "workload")
        ctx.cg_remove("gl1a-L9")


def leg_cg_root_exits(ctx):
    d = ctx.rundir("L10")
    cg = ctx.cg_create("gl1a-L10", "workload")
    sup = subprocess.Popen(
        [sys.executable, FIXTURE, "root-exits", "--dir", d, "--role", "root"])
    try:
        ctx.cg_write_procs(cg, sup.pid)
        leaf = wait_record(d, "root-child-leaf")
        if leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["leaf record missing"], {}
        rc = sup.wait(timeout=15)
        members = ctx.cg_members(cg)
        ok = rc == 0 and alive(leaf["pid"]) and leaf["pid"] in members
        notes = [f"supervisor rc={rc}, leaf alive+in-cgroup={ok}"]
        ctx.cg_kill(cg)
        wait_all_dead([leaf["pid"]], timeout=10.0)
        return ok, notes, {"leaf": leaf}
    finally:
        try:
            reap(sup)
        except Exception:
            pass
        ctx.cg_remove("gl1a-L10", "workload")
        ctx.cg_remove("gl1a-L10")


def leg_cg_kill(ctx):
    d = ctx.rundir("L11")
    cg = ctx.cg_create("gl1a-L11", "workload")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        ctx.cg_write_procs(cg, sup.pid)
        root, child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        pids = [root["pid"]] + ([child["pid"]] if child else []) + [leaf["pid"]]
        ctx.cg_kill(cg)
        dead = wait_all_dead(pids, timeout=10.0)
        notes = [f"cgroup.kill all-dead={dead}"]
        kill_pid(sup.pid)
        reap(sup)
        return dead, notes, {"pids": pids}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)
        ctx.cg_remove("gl1a-L11", "workload")
        ctx.cg_remove("gl1a-L11")


def leg_cg_nested(ctx):
    d = ctx.rundir("L12")
    cg = ctx.cg_create("gl1a-L12", "workload")
    sup = subprocess.Popen(
        [sys.executable, FIXTURE, "tree-subgroup", "--dir", d, "--role", "root",
         "--cgroup-base", cg])
    try:
        ctx.cg_write_procs(cg, sup.pid)
        root = wait_record(d, "root")
        if root is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["root record missing"], {}
        leaf = wait_record(d, "root-leaf")
        if not root.get("moved"):
            if leaf:
                cleanup_tree([root["pid"], leaf["pid"]])
            kill_pid(sup.pid)
            reap(sup)
            return False, [f"subgroup move failed: {root.get('move_error')}"], {"root": root}
        ctx.cg_kill(cg)
        dead = wait_all_dead([root["pid"], leaf["pid"]], timeout=10.0)
        notes = [f"parent cgroup.kill recursive all-dead={dead} (subgroup={root.get('subgroup')})"]
        kill_pid(sup.pid)
        reap(sup)
        return dead, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            reap(sup)
        except Exception:
            pass
        ctx.cg_remove("gl1a-L12", "workload", "sub")
        ctx.cg_remove("gl1a-L12", "workload")
        ctx.cg_remove("gl1a-L12")


def leg_cg_supervisor_dies(ctx):
    d = ctx.rundir("L13")
    cg = ctx.cg_create("gl1a-L13", "workload")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        ctx.cg_write_procs(cg, sup.pid)
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        kill_pid(sup.pid)
        reap(sup)
        # EXPECTED: workload survives (cgroup membership is not ownership).
        survived = wait_all_alive([root["pid"], leaf["pid"]], timeout=5.0)
        members = ctx.cg_members(cg)
        notes = [f"supervisor killed; workload alive={survived} (expected: survive), "
                 f"still in cgroup={[p for p in (root['pid'], leaf['pid']) if p in members]}"]
        ctx.cg_kill(cg)
        wait_all_dead([root["pid"], leaf["pid"]], timeout=10.0)
        return survived, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)
        ctx.cg_remove("gl1a-L13", "workload")
        ctx.cg_remove("gl1a-L13")


def leg_watchdog(ctx):
    d = ctx.rundir("L14")
    # Watchdog lives OUTSIDE the workload cgroup (else cgroup.kill would
    # take it too): supervisor + watchdog stay in the parent test cgroup;
    # only the workload tree is moved into workload/.
    parent = ctx.cg_create("gl1a-L14")
    cg = ctx.cg_create("gl1a-L14", "workload")
    killfile = os.path.join(cg, "cgroup.kill")
    sup = subprocess.Popen(
        [sys.executable, "-c",
         "import subprocess, sys, os;"
         f"d={d!r};"
         "w=subprocess.Popen([sys.executable, os.environ['GL1A_SRC'] + '/fixture.py',"
         " 'watchdog', '--dir', d, '--role', 'watchdog',"
         f" '--pdeathsig', str({int(SIGUSR1)}), '--on-pdeath', {killfile!r}]);"
         "r=subprocess.Popen([sys.executable, os.environ['GL1A_SRC'] + '/fixture.py',"
         " 'tree', '--dir', d, '--role', 'root']);"
         "w.wait(); r.wait()"],
        env={**os.environ, "GL1A_SRC": HERE})
    try:
        ctx.cg_write_procs(parent, sup.pid)
        if not (wait_record(d, "watchdog") and wait_record(d, "root-child-leaf")):
            kill_pid(sup.pid)
            reap(sup)
            return False, ["watchdog/root not ready"], {}
        root = wait_record(d, "root-child-leaf")
        child = wait_record(d, "root-child", timeout=5.0)
        if root is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        pids = [root["pid"]] + ([child["pid"]] if child else [])
        for p in pids:
            ctx.cg_write_procs(cg, p)
        wdog = wait_record(d, "watchdog")
        kill_pid(sup.pid)
        reap(sup)
        # Watchdog fires on supervisor death and cgroup.kills the rest, then
        # exits normally itself: expect workload dead, fired record present,
        # watchdog gone (it returns after firing). The fired record proves
        # it survived the kill it caused (else no record).
        dead = wait_all_dead(pids, timeout=15.0)
        wgone = wait_all_dead([wdog["pid"]], timeout=15.0)
        fired = wait_record(d, "watchdog-fired", timeout=5.0)
        notes = [f"workload all-dead={dead}, watchdog gone={wgone}, "
                 f"fired-record={fired is not None} (constructed ownership, not a kernel guarantee)"]
        if alive(wdog["pid"]):
            kill_pid(wdog["pid"])
            wait_all_dead([wdog["pid"]], timeout=10.0)
        return dead and wgone and fired is not None, notes, \
            {"root": root, "watchdog": wdog}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)
        ctx.cg_remove("gl1a-L14", "workload")
        ctx.cg_remove("gl1a-L14")


def cleanup_tree(pids, timeout=10.0):
    for p in pids:
        kill_pid(p)
    return wait_all_dead(pids, timeout=timeout)


def reap_pid(pid, timeout=10.0):
    """Non-children cannot be reaped (only observed). Kept for symmetry."""
    return wait_all_dead([pid], timeout=timeout)


LEGS = [
    ("L1", "PG/session", leg_pg_tree, "descendants inherit group"),
    ("L2", "PG/session", leg_pg_root_exits, "descendants remain addressable by PGID"),
    ("L3", "PG/session", leg_pg_killpg, "surviving group dies without PID enumeration"),
    ("L4", "PG/session", leg_pg_supervisor_dies, "workload SURVIVES (no owner-death semantic)"),
    ("L5", "PG/session", leg_pg_escape, "setsid escapee survives group kill (no containment)"),
    ("L6", "PDEATHSIG", leg_pd_root_dies, "armed root dies on supervisor death"),
    ("L7b", "PDEATHSIG", leg_pd_solo_arm, "root-only arming: root dies, deeper survive"),
    ("L8", "PDEATHSIG", leg_pd_cascade, "explicitly-armed cascade; cooperative-only"),
    ("L9", "cgroup v2", leg_cg_tree, "all descendants inherit membership"),
    ("L10", "cgroup v2", leg_cg_root_exits, "descendants remain in cgroup"),
    ("L11", "cgroup v2", leg_cg_kill, "entire tree dies via cgroup.kill"),
    ("L12", "cgroup v2", leg_cg_nested, "parent kill recursively kills nested cgroup"),
    ("L13", "cgroup v2", leg_cg_supervisor_dies, "workload SURVIVES (membership is not ownership)"),
    ("L14", "composed", leg_watchdog, "pdeathsig watchdog + cgroup.kill (constructed ownership)"),
]


def host_info():
    info = {}
    try:
        with open("/proc/version") as f:
            info["kernel"] = f.read().strip()
    except OSError:
        info["kernel"] = "unknown"
    try:
        with open("/etc/os-release") as f:
            for line in f:
                if line.startswith("PRETTY_NAME="):
                    info["release"] = line.strip().split("=", 1)[1].strip('"')
    except OSError:
        info["release"] = "unknown"
    return info


def main(argv):
    out = os.path.abspath(argv[0] if argv else os.path.join(HERE, "out"))
    base = os.path.join(out, "runs")
    os.makedirs(base, exist_ok=True)
    ctx = Ctx(base)
    try:
        os.makedirs(ctx.cg_root, exist_ok=True)
    except OSError as ex:
        print(f"gl1a: cannot create {ctx.cg_root}: {ex}")
        return 2
    cap_ok, cap_notes, cap = capability(ctx)
    results = []
    all_ok = cap_ok
    for lid, mech, fn, expect in LEGS:
        try:
            passed, notes, ev = fn(ctx)
        except Exception as ex:  # never lose the run to one leg
            passed, notes, ev = False, [f"leg fault {type(ex).__name__}: {ex}"], {}
        results.append({"id": lid, "mechanism": mech, "expected": expect,
                        "pass": bool(passed), "notes": notes, "evidence": ev})
        all_ok = all_ok and passed
        print(f"gl1a: [{('PASS' if passed else 'FAIL')}] {lid} ({mech})", flush=True)
        for n in notes:
            print(f"      {n}", flush=True)
    # Top-level safety net: kill everything still under our test root, so no
    # leg can leak past a fault path. Then report leftovers honestly.
    still = ctx.cg_sweep()
    report = {"tool": "gl1a-life", "host": host_info(),
              "capability": {"pass": cap_ok, "notes": cap_notes, "detail": cap},
              "legs": results,
              "leftoverPids": still,
              "pass": bool(all_ok and not still)}
    with open(os.path.join(out, "gl1a-life.json"), "w") as f:
        json.dump(report, f, indent=2)
    print(f"gl1a: overall={'PASS' if report['pass'] else 'FAIL'} "
          f"leftover={still} report={os.path.join(out, 'gl1a-life.json')}", flush=True)
    try:
        os.rmdir(ctx.cg_root)
    except OSError:
        pass
    return 0 if report["pass"] else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
