# GL-2 Linux provider evidence (2026-10-05, Ubuntu 26.04 WSL2 as root)

`src/Gagamba.Execution.Linux` implements `IExecutionProvider` over cgroup v2:
`posix_spawn` + `SETCGROUP` (child born inside via `CLONE_INTO_CGROUP`),
`cgroup.kill` terminate/dispose, exclusive env, single-use preparations,
idempotent terminate, opaque handles. No watchdog composition, no PID use.

## From-memory constants are untrusted: two silent failures in one session

1. `O_DIRECTORY` remembered as `0x4000` is actually `O_DIRECT` on x86-64;
   the real value is `0x10000` (proven against `os.O_DIRECTORY`). With the
   wrong value every cgroup fd open failed `EINVAL` (err=22).
2. `POSIX_SPAWN_SETCGROUP` remembered as `0x40` is actually
   `POSIX_SPAWN_USEVFORK`; the placement bit on glibc 2.43/x86-64 is
   **`0x100`** (found by brute-forcing flag bits against behavior: only
   `0x100` puts the child in `cgroup.procs`).

Both failures were SILENT: spawn returned 0 while the child landed in
`/init.scope`. Proof of silence: `setflags(0x40)` + `setcgroup(badfd=9999)`
still returned spawn rc=0. GL-1A never used this primitive (it used
`Popen` + parent-side `cgroup.procs` writes), so no earlier result covered it.

## Atomic placement proven at the syscall layer

With bit `0x100`, strace shows the mechanism working, vfork included:

```
clone3({flags=CLONE_VM|CLONE_VFORK|CLONE_CLEAR_SIGHAND|CLONE_INTO_CGROUP,
        exit_signal=SIGCHLD, ..., cgroup=4}, 88) = 9954
```

Child cgroup reads `0::/gagamba-gl2-tests/repro-verify`; `cgroup.procs`
contains the newborn pid with no parent-side move. `START_SUSPENDED`
(`0x10`) is ignored by this glibc (child state `R`, never `T`), so the
provider does NOT rely on it. Workdir uses
`posix_spawn_file_actions_addchdir_np` (verified: child `pwd` correct).

## Fail-closed guard against the next wrong constant

The SETCGROUP bit is a glibc-version detail that fails silently, so the
provider re-proves it on every new machine: first `Prepare` spawns a
sacrificial `/bin/sleep` into a probe cgroup and requires its pid to be
BORN in `cgroup.procs`. If not, every `Prepare` rejects with the reason
(`SETCGROUP bit ignored on this glibc`). No downgrade to parent-side
`cgroup.procs` moves (racy), ever.

## Suite: 19/19 as root

`tests/Gagamba.Execution.Linux.Tests` (heartbeat files, no PIDs):
placement-born-inside, root terminate, 3-level tree kill, root-exit
orphan kill via handle, terminate idempotence, dispose kills live tree,
unknown/bogus preparation fail-closed, strict-agent EscapeResistant
rejection, exclusive env (marker present, ambient absent), single-use +
foreign-handle fail-closed, nested-cgroup recursive kill, setsid escapee
stays in domain and dies, public-surface opacity scan, 8 pure splitter/env
unit tests (run on every CI OS; OS tests gate off without delegation).
Solution-wide Release build: 0 warnings, 0 errors, 14 projects.
