# GM-2 macOS provider evidence (2026-10-05)

`src/Gagamba.Execution.MacOS` implements `IExecutionProvider` over launchd
jobs + process groups, using only the GM-1A-proven mechanics: unique launchd
job per execution, `bootstrap` then `kickstart`, readiness via `print`,
termination and cleanup via `bootout` (never `stop`), no KeepAlive, no
`AbandonProcessGroup`, WorkingDirectory from the spec, opaque handle mapping
privately to job/label/plist, no PID enumeration, no watchdog composition,
and no claim beyond Partial escape resistance. GP-3 unchanged.

## Environment exactness first (the contract pressure point)

The question: launchd supplies an execution environment of its own, so is a
plist `EnvironmentVariables` entry still `ProcessStartSpec.Environment`
exclusive, or does it quietly weaken GP-3?

Probe (`spikes/Gm2Env`, manual workflow `gm2-env-probe`, run 37308159249 on
`macos-latest`): parent sets `GAGAMBA_AMBIENT=must-not-appear`; the plist
grants only `GAGAMBA_GRANTED=yes`; the target dumps its complete
environment. Verdict **EXACT-DIRECT**:

```
"granted_present": true, "ambient_absent": true, "env_count": 13
delivered: GAGAMBA_GRANTED, HOME, LC_CTYPE, LOGNAME, OSLogRateLimit,
           PATH, SHELL, SSH_AUTH_SOCK, TMPDIR, USER, XPC_FLAGS,
           XPC_SERVICE_NAME, __CF_USER_TEXT_ENCODING
```

No trampoline needed. But the delivered set is **not byte-identical to the
spec**: launchd injects OS session variables (HOME, PATH, TMPDIR, XPC_*,
`__CF_USER_TEXT_ENCODING`, …). These are supplied by launchd, **not
inherited from the launching process** — the ambient value never appears.
So the property GP-3 actually requires ("environment is a granted resource,
never inherited") holds directly; direct plist configuration is used and
GP-3 is untouched. The delta is documented, not hidden: on macOS the
domain owner contributes session variables, and exact set equality with the
spec is therefore not claimed. A trampoline (provider-private exec with
exactly the spec env) remains the fallback if a future requirement demands
byte-exact environment; it is deliberately not built now.

## Launch invariant

`Launch` cannot return success until launchd owns the job **and** the target
demonstrably runs under it: `bootstrap` installs, `kickstart` starts,
`print` must report `state = running` within a bounded private poll. If
bootstrap succeeds but kickstart/readiness fails, the provider **boots out
immediately** and returns a classified failure. Readiness never reads the
pid line (that would leak identity); only `state` is inspected.

## Acceptance tests (production semantics from GW-2/GL-2)

`tests/Gagamba.Execution.MacOS.Tests` runs on `macos-latest` (job
`macos-provider-tests`), heartbeats only, no PIDs:

1. root launch → bootout terminates;
2. 3-level tree (root→child→grandchild) all frozen after bootout;
3. root exits, surviving same-PG child still cleaned via the handle;
4. provider disposal cleans a live tree;
5. failed launch (target never reaches running) fails closed and leaves no
   loaded `org.gagamba.exec.*` job;
6. explicit environment present;
7. ambient environment absent;
8. working directory honored (realpath compare);
9. foreign/stale/single-use preparations and handles fail closed;
10. public surface opacity scan (no Pid/JobHandle/Cgroup/Pgid/Label/Plist…);
11. `setsid` escapee is **observed alive**, not reported killed — the
    same-PG leaf dies with the domain while the session escapee survives
    (M8 truth: escape is escape, not containment).

Plus 8 pure unit tests (arg splitter, env validation incl. Unix
case-sensitivity, `print` state parsing) that run on every CI OS.

## Cross-platform result

One `IExecutionProvider` now spans three materially different lifecycle
models: Windows Job Objects (kernel-owned, Full), Linux cgroup v2 (native
recursive kill, escape Partial), macOS launchd + PG (PG-scoped, escape
Absent). The SPI absorbed all three with no signature change; the only
contract-adjacent tension — environment exactness — was resolved by
measurement, and GP-3 stands.
