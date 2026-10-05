# GL-1A Linux execution-domain lifecycle probe

Date: 2026-10-05. Host: Ubuntu 26.04.1 LTS, WSL2 kernel
6.6.114.1-microsoft-standard-WSL2, systemd PID 1, cgroup v2 unified.
Spike: `spikes/Gl1aLife/` (shell + python3 only: `fixture.py`,
`run.py`, `summary.py`), entrypoint `eng/gl1a-life.sh` (copies into the
distro filesystem, runs as root, copies evidence back). No .NET in
Ubuntu, no gcc, no provider, no common interface. Raw JSON per run under
ignored `artifacts/` (`gl1a-life-*.json`).

## Capability (pre-phase, not assumed)

cgroup v2 mounted, `cgroup.kill` present, subtree creatable as root
(unprivileged path would be systemd `Delegate=yes`, not used here).
No workload runs as a systemd service: `KillMode=control-group` must not
mask what raw cgroup v2 does, so an outer observer (the orchestrator)
kills each test supervisor itself and observes from outside.

## Matrix result (14/14 match expectations, zero leftovers)

| Leg | Mechanism | Verdict |
|---|---|---|
| L1 | PG/session | descendants inherit group; own group, not orchestrator's |
| L2 | PG/session | root exits (rc 0), descendants remain addressable by PGID |
| L3 | PG/session | `killpg` kills the surviving group, no PID enumeration |
| L4 | PG/session | supervisor killed → workload SURVIVES (no owner-death semantic) |
| L5 | PG/session | `setsid` escapee survives group kill; leaf dies (no containment) |
| L6 | PDEATHSIG | armed root dies on supervisor death |
| L7b | PDEATHSIG | root-only arming: root dies, deeper survive |
| L8 | PDEATHSIG | fully-armed cascade dies; cooperative-only (clears on fork) |
| L9 | cgroup v2 | all descendants inherit membership |
| L10 | cgroup v2 | root exits, descendants remain in cgroup |
| L11 | cgroup v2 | `cgroup.kill` kills the entire tree |
| L12 | cgroup v2 | parent kill recursively kills a nested descendant cgroup |
| L13 | cgroup v2 | supervisor killed → workload SURVIVES (membership ≠ ownership) |
| L14 | composed | pdeathsig watchdog + `cgroup.kill`: workload dead, watchdog fired and exited (constructed ownership, not a kernel guarantee) |

## Conclusion (as specified, now evidenced)

- Process groups provide addressable tree termination but no
  owner-death guarantee (L4). No containment: a descendant can
  `setsid()` out (L5).
- PDEATHSIG provides direct parent-death coupling (L6) but not
  arbitrary descendant ownership (L7b); full-tree cascades need every
  generation armed explicitly and stay cooperative-only (L8).
- cgroup v2 provides recursive kernel-backed workload termination
  (L11, incl. nested L12) but no automatic owner-death action (L13).
- A PDEATHSIG-triggered trusted cgroup reaper composes the semantic
  result (L14), explicitly classified as constructed ownership. Watchdog
  protection belongs to later sandbox/isolation work.

Method notes: readiness files gate every kill (no sleeps); orchestrator
never signals its own group/session (asserted); zombies count as dead
(PID 1 reaps orphans); per-leg cleanup plus an end-of-run cgroup sweep,
with leftovers failing the run (zero in the recorded runs). Two
harness-level races were found and closed: records are read only after
both readiness file AND parsable JSON exist, and cgroup legs explicitly
re-place live members (a child forked before the parent's move lands in
the parent cgroup). Hand-rolled JSON must use lowercase booleans.
