#!/usr/bin/env python3
"""GM-2 environment-exactness probe: does a launchd plist EnvironmentVariables
entry produce EXACTLY the specified environment, or does launchd add/inherit?

Parent sets GAGAMBA_AMBIENT=must-not-appear. Plist grants only
GAGAMBA_GRANTED=yes. Target dumps its complete environment to a file.
Verdict: granted present? ambient absent? what else did launchd supply?
Also records kickstart-vs-running semantics for the provider readiness design.
"""
import json
import os
import plistlib
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))


def launchctl(*args, timeout=30):
    p = subprocess.run(["launchctl", *args], capture_output=True, text=True,
                       timeout=timeout)
    return p.returncode, (p.stdout + p.stderr)[:2000]


def wait_file(path, timeout=20.0):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(path):
            return True
        time.sleep(0.1)
    return False


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
    os.makedirs(outdir, exist_ok=True)
    d = os.path.join(outdir, "gm2env")
    os.makedirs(d, exist_ok=True)
    domain = "gui/501"
    label = f"org.gagamba.gm2env.{os.getpid()}"

    os.environ["GAGAMBA_AMBIENT"] = "must-not-appear"

    target = os.path.join(HERE, "envtarget.py")
    plist = {
        "Label": label,
        "ProgramArguments": [sys.executable, target, "--dir", d],
        "WorkingDirectory": d,
        "EnvironmentVariables": {"GAGAMBA_GRANTED": "yes"},
        "StandardOutPath": os.path.join(d, "launchd-out.log"),
        "StandardErrorPath": os.path.join(d, "launchd-err.log"),
    }
    plist_path = os.path.join(d, label + ".plist")
    with open(plist_path, "wb") as f:
        plistlib.dump(plist, f)

    result = {"label": label}
    try:
        rc, out = launchctl("bootstrap", domain, plist_path)
        result["bootstrap_rc"] = rc
        if rc != 0:
            result["verdict"] = f"BOOTSTRAP-FAILED: {out[:300]}"
            print(json.dumps(result, indent=2))
            return 1
        rc, out = launchctl("kickstart", f"{domain}/{label}")
        result["kickstart_rc"] = rc
        if rc != 0:
            result["verdict"] = f"KICKSTART-FAILED: {out[:300]}"
            print(json.dumps(result, indent=2))
            return 1
        # Immediately ask launchd: is it already running?
        rc, pout = launchctl("print", f"{domain}/{label}")
        result["print_right_after_kickstart_rc"] = rc
        for line in pout.splitlines():
            s = line.strip()
            if s.startswith("state =") or s.startswith("pid ="):
                result.setdefault("print_state_lines", []).append(s)
        ready = wait_file(os.path.join(d, "env-ready.txt"), timeout=20.0)
        result["target_ready"] = ready
        rc, pout = launchctl("print", f"{domain}/{label}")
        for line in pout.splitlines():
            s = line.strip()
            if s.startswith("state =") or s.startswith("pid ="):
                result.setdefault("print_running_lines", []).append(s)
        dump = os.path.join(d, "env-dump.txt")
        if os.path.exists(dump):
            with open(dump) as f:
                env = dict(line.rstrip("\n").split("=", 1)
                           for line in f if "=" in line)
            result["target_env_sorted"] = sorted(env.items())
            result["granted_present"] = env.get("GAGAMBA_GRANTED") == "yes"
            result["ambient_absent"] = "GAGAMBA_AMBIENT" not in env
            result["env_count"] = len(env)
            if result["granted_present"] and result["ambient_absent"]:
                result["verdict"] = "EXACT-DIRECT"
            else:
                result["verdict"] = "POLLUTED-NEEDS-TRAMPOLINE"
        else:
            result["verdict"] = "NO-DUMP"
            result["granted_present"] = False
            result["ambient_absent"] = False
    finally:
        launchctl("bootout", f"{domain}/{label}")
        time.sleep(0.5)
        rc, pout = launchctl("print", f"{domain}/{label}")
        result["print_after_bootout_rc"] = rc
        try:
            os.remove(plist_path)
        except OSError:
            pass
    print(json.dumps(result, indent=2))
    return 0 if result.get("verdict") in ("EXACT-DIRECT",
                                          "POLLUTED-NEEDS-TRAMPOLINE") else 1


if __name__ == "__main__":
    sys.exit(main())
