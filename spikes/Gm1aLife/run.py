#!/usr/bin/env python3
"""GM-1A macOS lifecycle probe: M1-M12 characterization matrix.

No local Mac exists, so this runs on GitHub macOS runners (CI) and was
logic-validated for its portable legs on Ubuntu (see --only). shell +
python3 only. launchd legs need a GUI/user domain with bootstrap rights;
when absent they record honestly instead of hanging. Exit 0 iff every
executed leg matches its expectation (M7/M8/M10 pass on clean observation
either way - the answer is the data).
"""
import json
import os
import plistlib
import signal
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
FIXTURE = os.path.join(HERE, "fixture.py")
SIGKILL = signal.SIGKILL


def read_record(directory, role):
    path = os.path.join(directory, role + ".json")
    if not os.path.exists(path):
        return None
    try:
        with open(path) as f:
            return json.load(f)
    except (OSError, ValueError):
        return None


def wait_record(directory, role, timeout=20.0):
    """Readiness file AND parsable record (closes the publish race)."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(os.path.join(directory, role + ".ready")):
            rec = read_record(directory, role)
            if rec is not None:
                return rec
        time.sleep(0.05)
    return read_record(directory, role)


def proc_zombie(pid):
    try:
        out = subprocess.run(["ps", "-p", str(pid), "-o", "stat="],
                             capture_output=True, text=True,
                             timeout=10).stdout.strip()
        return bool(out.split()) and "Z" in out.split()[0]
    except Exception:
        return False


def alive(pid):
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return not proc_zombie(pid)


def kill_pid(pid, sig=SIGKILL):
    try:
        os.kill(pid, sig)
        return True
    except (ProcessLookupError, PermissionError):
        return False


def kill_group(pgid, sig=SIGKILL):
    # Never signal our own group/session or init.
    assert pgid not in (0, 1, os.getpgrp(), os.getsid(0)), f"refusing killpg({pgid})"
    try:
        os.killpg(pgid, sig)
        return True
    except (ProcessLookupError, PermissionError):
        return False


def wait_all_dead(pids, timeout=15.0):
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


def reap(proc, timeout=15.0):
    try:
        return proc.wait(timeout=timeout)
    except Exception:
        return None


def cleanup_tree(pids, timeout=10.0):
    for p in pids:
        kill_pid(p)
    return wait_all_dead(pids, timeout=timeout)


def spawn_wrapped(directory, child_args):
    """Supervisor wrapper in its own session; the fixture child is root."""
    code = ("import subprocess, sys;"
            f"p=subprocess.Popen([sys.executable, {FIXTURE!r}, *{tuple(child_args)!r}]);"
            "p.wait()")
    return subprocess.Popen([sys.executable, "-c", code], start_new_session=True)


def launchctl(*args, timeout=30):
    try:
        r = subprocess.run(["launchctl", *args], capture_output=True,
                           text=True, timeout=timeout)
        return r.returncode, (r.stdout or "") + (r.stderr or "")
    except FileNotFoundError:
        return 127, "launchctl not found"
    except subprocess.TimeoutExpired:
        return 124, "launchctl timeout"


class Ctx:
    def __init__(self, base):
        self.base = base
        self.domain = ""

    def rundir(self, leg):
        d = os.path.join(self.base, leg)
        os.makedirs(d, exist_ok=True)
        # Stale readiness/record files from a crashed previous run in a
        # REUSED out dir would fake fresh PIDs (phantom verdicts): clear.
        for f in os.listdir(d):
            if f.endswith((".ready", ".json")) or f == "stop":
                try:
                    os.remove(os.path.join(d, f))
                except OSError:
                    pass
        return d


def capability(ctx):
    notes = []
    out = {}
    rc, _ = launchctl("--version")
    out["launchctl_present"] = (rc != 127)
    notes.append(f"launchctl present={out['launchctl_present']}")
    out["domain"] = ""
    if out["launchctl_present"]:
        try:
            uid = os.getuid()
        except AttributeError:
            uid = 501
        for dom in (f"gui/{uid}", f"user/{uid}"):
            rc, bout = launchctl("print", dom)
            if rc == 0:
                out["domain"] = dom
                notes.append(f"domain {dom} printable")
                break
            notes.append(f"domain {dom} print rc={rc}")
    out["posix_groups"] = True
    passed = bool(out["launchctl_present"])
    return passed, notes, out


def tree_pids(directory):
    root = wait_record(directory, "root")
    child = wait_record(directory, "root-child", timeout=5.0)
    leaf = wait_record(directory, "root-child-leaf")
    return root, child, leaf


def leg_pg_tree(ctx):
    d = ctx.rundir("M1")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        same = root["pgid"] == leaf["pgid"]
        ok = same and root["pgid"] != os.getpgrp()
        notes = [f"sup={sup.pid} root-pgid={root['pgid']} leaf-pgid={leaf['pgid']}"]
        kill_group(root["pgid"])
        dead = wait_all_dead([root["pid"], leaf["pid"]], timeout=15.0)
        reap(sup)
        return ok and dead, notes, {"supervisor": sup.pid, "root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_root_exits(ctx):
    d = ctx.rundir("M2")
    sup = subprocess.Popen(
        [sys.executable, FIXTURE, "root-exits", "--dir", d, "--role", "root"],
        start_new_session=True)
    try:
        leaf = wait_record(d, "root-child-leaf")
        if leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["leaf record missing"], {}
        rc = sup.wait(timeout=20)
        ok = rc == 0 and alive(leaf["pid"])
        notes = [f"supervisor exited rc={rc}, leaf alive={alive(leaf['pid'])} pgid={leaf['pgid']}"]
        kill_group(leaf["pgid"])
        wait_all_dead([leaf["pid"]], timeout=15.0)
        return ok, notes, {"leaf": leaf}
    finally:
        try:
            reap(sup)
        except Exception:
            pass


def leg_pg_killpg(ctx):
    d = ctx.rundir("M3")
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
        dead = wait_all_dead(pids, timeout=15.0)
        return dead, [f"killpg({pgid}) all-dead={dead}"], {"pids": pids}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_supervisor_dies(ctx):
    d = ctx.rundir("M4")
    sup = spawn_wrapped(d, ["tree", "--dir", d, "--role", "root"])
    try:
        root, _child, leaf = tree_pids(d)
        if root is None or leaf is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        kill_pid(sup.pid)
        reap(sup)
        # EXPECTED: workload survives (no owner-death semantic).
        survived = wait_all_alive([root["pid"], leaf["pid"]], timeout=5.0)
        notes = [f"supervisor killed; root/leaf alive={survived} (expected: survive)"]
        kill_group(root["pgid"])
        wait_all_dead([root["pid"], leaf["pid"]], timeout=15.0)
        return survived, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_pg_escape(ctx):
    d = ctx.rundir("M5")
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
        pgid = leaf["pgid"]
        kill_pid(sup.pid)
        reap(sup)
        kill_group(pgid)
        esc_alive = wait_all_alive([root["pid"]], timeout=5.0)
        leaf_dead = wait_all_dead([leaf["pid"]], timeout=15.0)
        notes = [f"escaper sid={root['sid']} alive={esc_alive} (expected: survive); "
                 f"leaf dead={leaf_dead}"]
        kill_pid(root["pid"])
        wait_all_dead([root["pid"]], timeout=15.0)
        return esc_alive and leaf_dead, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def write_plist(directory, label, mode_args):
    plist = {
        "Label": label,
        "ProgramArguments": [sys.executable, FIXTURE, *mode_args],
        "WorkingDirectory": directory,
        "StandardOutPath": os.path.join(directory, "launchd-out.log"),
        "StandardErrorPath": os.path.join(directory, "launchd-err.log"),
    }
    path = os.path.join(directory, label + ".plist")
    with open(path, "wb") as f:
        plistlib.dump(plist, f)
    return path


def bootout(domain, label):
    return launchctl("bootout", f"{domain}/{label}")


def leg_launchd_tree(ctx):
    d = ctx.rundir("M6")
    if not ctx.domain:
        return False, ["no launchd domain (capability)"], {}
    label = f"org.gagamba.gm1a.m6.{os.getpid()}"
    plist = write_plist(d, label, ["tree", "--dir", d, "--role", "root"])
    try:
        rc, out = launchctl("bootstrap", ctx.domain, plist)
        if rc != 0:
            return False, [f"bootstrap rc={rc}: {out[:200]}"], {"domain": ctx.domain}
        rc, out = launchctl("kickstart", f"{ctx.domain}/{label}")
        if rc != 0:
            bootout(ctx.domain, label)
            return False, [f"kickstart rc={rc}: {out[:200]}"], {}
        root = wait_record(d, "root")
        leaf = wait_record(d, "root-child-leaf")
        if root is None or leaf is None:
            return False, ["records missing (job may not have started)"], {}
        same = root["pgid"] == leaf["pgid"]
        rc, state = launchctl("print", f"{ctx.domain}/{label}")
        notes = [f"common pgid={same} ({root['pgid']}), sids root={root['sid']} leaf={leaf['sid']}",
                 f"print: {(state[:160]).replace(chr(10), ' ')}"]
        return same, notes, {"root": root, "leaf": leaf, "domain": ctx.domain}
    finally:
        bootout(ctx.domain, label)
        # bootout must clean the tree; verify by PGID if we have records
        try:
            root = read_record(d, "root")
            leaf = read_record(d, "root-child-leaf")
            pids = [r["pid"] for r in (root, leaf) if r]
            wait_all_dead(pids, timeout=15.0)
        except Exception:
            pass


def leg_launchd_root_exits(ctx):
    d = ctx.rundir("M7")
    if not ctx.domain:
        return False, ["no launchd domain (capability)"], {}
    label = f"org.gagamba.gm1a.m7.{os.getpid()}"
    plist = write_plist(d, label, ["root-exits", "--dir", d, "--role", "root"])
    try:
        rc, out = launchctl("bootstrap", ctx.domain, plist)
        if rc != 0:
            return False, [f"bootstrap rc={rc}: {out[:200]}"], {}
        rc, out = launchctl("kickstart", f"{ctx.domain}/{label}")
        if rc != 0:
            bootout(ctx.domain, label)
            return False, [f"kickstart rc={rc}: {out[:200]}"], {}
        leaf = wait_record(d, "root-child-leaf")
        if leaf is None:
            return False, ["leaf record missing"], {}
        # Root exits itself; observe WITHOUT touching: does launchd clean
        # the remaining same-PGID processes? Poll up to 15s.
        cleaned = wait_all_dead([leaf["pid"]], timeout=15.0)
        notes = [f"root exited; launchd cleaned remainder={cleaned} "
                 f"(True = same-PG cleanup observed; False = no cleanup)"]
        return True, notes, {"leaf": leaf, "cleanup_observed": cleaned}
    finally:
        bootout(ctx.domain, label)
        try:
            leaf = read_record(d, "root-child-leaf")
            if leaf:
                kill_group(leaf["pgid"])
                wait_all_dead([leaf["pid"]], timeout=15.0)
        except Exception:
            pass


def leg_launchd_escape(ctx):
    d = ctx.rundir("M8")
    if not ctx.domain:
        return False, ["no launchd domain (capability)"], {}
    label = f"org.gagamba.gm1a.m8.{os.getpid()}"
    plist = write_plist(d, label, ["escape-session", "--dir", d, "--role", "root"])
    try:
        rc, out = launchctl("bootstrap", ctx.domain, plist)
        if rc != 0:
            return False, [f"bootstrap rc={rc}: {out[:200]}"], {}
        rc, out = launchctl("kickstart", f"{ctx.domain}/{label}")
        if rc != 0:
            bootout(ctx.domain, label)
            return False, [f"kickstart rc={rc}: {out[:200]}"], {}
        root = wait_record(d, "root")
        leaf = wait_record(d, "root-leaf")
        if root is None or leaf is None:
            return False, ["records missing"], {}
        if not root.get("escaped"):
            return False, [f"setsid failed: {root.get('escape_error')}"], {"root": root}
        # Root (the job's original process) exits; observe the escaped child.
        # Escapee has a NEW pgid/sid: launchd's same-PG cleanup should miss it.
        survived = wait_all_alive([root["pid"]], timeout=15.0)
        notes = [f"escaped sid={root['sid']} survived-root-exit={survived} "
                 f"(expected: survive = no containment even under launchd)"]
        return True, notes, {"root": root, "leaf": leaf}
    finally:
        bootout(ctx.domain, label)
        try:
            root = read_record(d, "root")
            leaf = read_record(d, "root-leaf")
            for r in (root, leaf):
                if r:
                    kill_pid(r["pid"])
            if leaf:
                wait_all_dead([leaf["pid"]], timeout=15.0)
            if root:
                wait_all_dead([root["pid"]], timeout=15.0)
        except Exception:
            pass


def leg_launchd_stop(ctx):
    d = ctx.rundir("M9")
    if not ctx.domain:
        return False, ["no launchd domain (capability)"], {}
    label = f"org.gagamba.gm1a.m9.{os.getpid()}"
    plist = write_plist(d, label, ["tree", "--dir", d, "--role", "root"])
    try:
        rc, out = launchctl("bootstrap", ctx.domain, plist)
        if rc != 0:
            return False, [f"bootstrap rc={rc}: {out[:200]}"], {}
        rc, out = launchctl("kickstart", f"{ctx.domain}/{label}")
        if rc != 0:
            bootout(ctx.domain, label)
            return False, [f"kickstart rc={rc}: {out[:200]}"], {}
        root = wait_record(d, "root")
        leaf = wait_record(d, "root-child-leaf")
        if root is None or leaf is None:
            return False, ["records missing"], {}
        pids = [root["pid"], leaf["pid"]]
        child = wait_record(d, "root-child", timeout=5.0)
        if child:
            pids.append(child["pid"])
        rc_s, out_s = launchctl("stop", f"{ctx.domain}/{label}")
        stopped_dead = wait_all_dead(pids, timeout=10.0)
        rc_b, out_b = bootout(ctx.domain, label)
        dead = wait_all_dead(pids, timeout=15.0)
        notes = [f"stop rc={rc_s} all-dead={stopped_dead}; bootout rc={rc_b} all-dead={dead}"]
        if not dead:
            kill_group(root["pgid"])
            wait_all_dead(pids, timeout=15.0)
        return dead, notes, {"pids": pids}
    finally:
        bootout(ctx.domain, label)


def leg_launchd_submitter_dies(ctx):
    d = ctx.rundir("M10")
    if not ctx.domain:
        return False, ["no launchd domain (capability)"], {}
    label = f"org.gagamba.gm1a.m10.{os.getpid()}"
    code = ("import subprocess, sys, os;"
            f"d={d!r}; label={label!r}; dom={ctx.domain!r};"
            "plist={'Label': label, 'ProgramArguments': "
            f"[sys.executable, {FIXTURE!r}, 'tree', '--dir', d, '--role', 'root'], "
            "'WorkingDirectory': d};"
            "import plistlib;"
            "open(d + '/' + label + '.plist', 'wb').write(plistlib.dumps(plist));"
            "s0=subprocess.run(['launchctl', 'bootstrap', dom, d + '/' + label + '.plist']);"
            "s1=subprocess.run(['launchctl', 'kickstart', dom + '/' + label]);"
            "open(d + '/submitter.done', 'w').write(f'{s0.returncode} {s1.returncode}');")
    sub = subprocess.Popen([sys.executable, "-c", code])
    try:
        deadline = time.time() + 30
        done = ""
        while time.time() < deadline:
            if os.path.exists(os.path.join(d, "submitter.done")):
                with open(os.path.join(d, "submitter.done")) as f:
                    done = f.read().strip()
                break
            time.sleep(0.1)
        rc = reap(sub, timeout=10.0)
        leaf = wait_record(d, "root-child-leaf", timeout=10.0)
        if not done or leaf is None:
            return False, [f"submitter done=[{done}] leaf-record={leaf is not None}"], {}
        # Submitter is gone (reaped, rc recorded); does the job survive?
        survived = wait_all_alive([leaf["pid"]], timeout=10.0)
        _, state = launchctl("print", f"{ctx.domain}/{label}")
        notes = [f"submitter rc={rc} done=[{done}]; job alive={survived} "
                 f"(expected: survive = launchd owns the job, not the client)"]
        return True, notes, {"leaf": leaf, "submitter_rc": rc}
    finally:
        try:
            kill_pid(sub.pid)
        except Exception:
            pass
        try:
            reap(sub)
        except Exception:
            pass
        bootout(ctx.domain, label)
        try:
            leaf = read_record(d, "root-child-leaf")
            if leaf:
                kill_group(leaf["pgid"])
                wait_all_dead([leaf["pid"]], timeout=15.0)
        except Exception:
            pass


def watchdog_code():
    # Runs in the watchdog child: waits for the workload marker record,
    # then blocks on the pipe; EOF (supervisor dead, write end closed)
    # fires killpg on the workload group. Newlines, not semicolons:
    # compound statements (try/while) need real blocks.
    return "\n".join([
        "import os, sys, signal, time, json",
        "dd = sys.argv[1]; rfd = int(sys.argv[2]); mark = sys.argv[3]",
        "deadline = time.time() + 25; rec = None",
        "while time.time() < deadline:",
        "    try:",
        "        rec = json.load(open(dd + '/' + mark + '.json'))",
        "        break",
        "    except Exception:",
        "        time.sleep(0.1)",
        "pgid = rec['pgid']",
        "data = os.read(rfd, 1024)",
        "open(dd + '/watchdog-fired.json', 'w').write('{\"eof\":' + ('true' if data == b'' else 'false') + '}')",
        "os.killpg(pgid, signal.SIGKILL)",
    ])


def leg_watchdog(ctx):
    d = ctx.rundir("M11")
    # Supervisor (own session) creates a pipe; watchdog child inherits the
    # read end and blocks on it; workload root gets no pipe FDs. Kill
    # supervisor -> write end closes -> watchdog reads EOF -> killpg.
    code = (
        "import subprocess, sys, os; "
        f"d = {d!r}; fix = {FIXTURE!r}; "
        "r, w = os.pipe(); "
        "wd = subprocess.Popen([sys.executable, '-c', " + repr(watchdog_code())
        + ", d, str(r), 'root-child-leaf'], pass_fds=(r,)); "
        "rt = subprocess.Popen([sys.executable, fix, 'tree', '--dir', d, '--role', 'root']); "
        "os.close(r); "
        "rt.wait(); wd.wait()"
    )
    sup = subprocess.Popen([sys.executable, "-c", code], start_new_session=True)
    try:
        root = wait_record(d, "root-child-leaf")
        if root is None:
            kill_pid(sup.pid)
            reap(sup)
            return False, ["records missing"], {}
        pgid = root["pgid"]
        kill_pid(sup.pid)
        reap(sup)
        # Watchdog should observe EOF and killpg the workload.
        child = wait_record(d, "root-child", timeout=5.0)
        pids = [root["pid"]] + ([child["pid"]] if child else [])
        dead = wait_all_dead(pids, timeout=15.0)
        fired_path = os.path.join(d, "watchdog-fired.json")
        fired = read_record(d, "watchdog-fired")
        fired_note = ("present" if fired is not None
                      else ("UNPARSABLE" if os.path.exists(fired_path) else "absent"))
        notes = [f"workload all-dead={dead}, fired-record={fired_note} "
                 f"(constructed owner-death cleanup)"]
        return dead and fired is not None, notes, {"root": root}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


def leg_watchdog_escape(ctx):
    d = ctx.rundir("M12")
    code = (
        "import subprocess, sys, os; "
        f"d = {d!r}; fix = {FIXTURE!r}; "
        "r, w = os.pipe(); "
        "wd = subprocess.Popen([sys.executable, '-c', " + repr(watchdog_code())
        + ", d, str(r), 'root-leaf'], pass_fds=(r,)); "
        "rt = subprocess.Popen([sys.executable, fix, 'escape-session', '--dir', d, '--role', 'root']); "
        "os.close(r); "
        "rt.wait(); wd.wait()"
    )
    sup = subprocess.Popen([sys.executable, "-c", code], start_new_session=True)
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
        kill_pid(sup.pid)
        reap(sup)
        leaf_dead = wait_all_dead([leaf["pid"]], timeout=15.0)
        esc_alive = wait_all_alive([root["pid"]], timeout=5.0)
        fired = read_record(d, "watchdog-fired")
        notes = [f"leaf dead={leaf_dead}, escaper alive={esc_alive}, fired={fired is not None}: "
                 f"constructed cleanup exists, complete ownership does not"]
        kill_pid(root["pid"])
        wait_all_dead([root["pid"]], timeout=15.0)
        return leaf_dead and esc_alive, notes, {"root": root, "leaf": leaf}
    finally:
        try:
            kill_pid(sup.pid)
        except Exception:
            pass
        reap(sup)


LEGS = [
    ("M1", "PG", leg_pg_tree, "common PGID"),
    ("M2", "PG", leg_pg_root_exits, "descendants remain in PG"),
    ("M3", "PG", leg_pg_killpg, "group dies"),
    ("M4", "PG", leg_pg_supervisor_dies, "workload SURVIVES (no owner-death)"),
    ("M5", "PG", leg_pg_escape, "setsid escape survives (no containment)"),
    ("M6", "launchd", leg_launchd_tree, "characterize PGID/session topology"),
    ("M7", "launchd", leg_launchd_root_exits, "observe same-PG cleanup on job death"),
    ("M8", "launchd", leg_launchd_escape, "determine whether escape survives"),
    ("M9", "launchd", leg_launchd_stop, "explicit stop/bootout descendant cleanup"),
    ("M10", "launchd", leg_launchd_submitter_dies, "determine whether job survives client death"),
    ("M11", "composed", leg_watchdog, "watchdog observes death, killpgs workload"),
    ("M12", "composed+escape", leg_watchdog_escape, "escape survives composition (no full ownership)"),
]


def host_info():
    info = {"platform": sys.platform}
    for prog, key in ((["sw_vers", "-productVersion"], "macos"),
                      (["sw_vers", "-buildVersion"], "build"),
                      (["uname", "-m"], "arch")):
        try:
            r = subprocess.run(prog, capture_output=True, text=True, timeout=10)
            info[key] = r.stdout.strip()
        except Exception:
            info[key] = "unknown"
    return info


def main(argv):
    only = set()
    out = None
    i = 0
    while i < len(argv):
        if argv[i] == "--only" and i + 1 < len(argv):
            only = set(argv[i + 1].split(","))
            i += 2
        elif argv[i] == "--out" and i + 1 < len(argv):
            out = os.path.abspath(argv[i + 1])
            i += 2
        else:
            i += 1
    if out is None:
        out = os.path.join(HERE, "out")
    base = os.path.join(out, "runs")
    os.makedirs(base, exist_ok=True)
    ctx = Ctx(base)
    cap_ok, cap_notes, cap = capability(ctx)
    if cap.get("domain"):
        ctx.domain = cap["domain"]
    results = []
    all_ok = cap_ok
    for lid, mech, fn, expect in LEGS:
        if only and lid not in only:
            continue
        try:
            passed, notes, ev = fn(ctx)
        except Exception as ex:  # never lose the run to one leg
            passed, notes, ev = False, [f"leg fault {type(ex).__name__}: {ex}"], {}
        results.append({"id": lid, "mechanism": mech, "expected": expect,
                        "pass": bool(passed), "notes": notes, "evidence": ev})
        all_ok = all_ok and passed
        print(f"gm1a: [{('PASS' if passed else 'FAIL')}] {lid} ({mech})", flush=True)
        for n in notes:
            print(f"      {n}", flush=True)
    report = {"tool": "gm1a-life", "host": host_info(),
              "capability": {"pass": cap_ok, "notes": cap_notes, "detail": cap},
              "legs": results,
              "pass": bool(all_ok)}
    with open(os.path.join(out, "gm1a-life.json"), "w") as f:
        json.dump(report, f, indent=2)
    print(f"gm1a: overall={'PASS' if report['pass'] else 'FAIL'} "
          f"report={os.path.join(out, 'gm1a-life.json')}", flush=True)
    return 0 if report["pass"] else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
