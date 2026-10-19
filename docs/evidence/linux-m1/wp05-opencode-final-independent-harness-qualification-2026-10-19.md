# WP05 OpenCode final independent harness qualification (2026-10-19)

Status: independent qualification of the WP05 harness at exact R12. This
document is the reviewer's Q0-Q3 record. It does not qualify the WP05 release
implementation itself, does not execute real WP10 target workloads, and does
not modify production behavior.

## Reviewer and context

- Reviewer/model: OpenCode (opencode/glm-5.3-flash-free), independent security
  and correctness reviewer; not the implementation author.
- Execution context: Windows host LAPTOP-KBD8D8JG, OpenCode shell (PowerShell
  5.1) driving `wsl.exe -d Ubuntu`; all Linux observations are first-hand from
  this context.
- Prepared checkout (frozen baseline): `/home/gaga/qualification/gagamba-r12`
  on branch `review/wp05-opencode-linux-qualification`, owner gaga:gaga.
- Reviewer worktree: `/home/gaga/qualification/harness-review` on branch
  `review/wp05-opencode-harness-final-qualification`, created from exact R12
  `38f6981b2a60daab2df37c92913a6760c1bef131` (tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`).
- Production baseline for diff: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.

## Q0 — Independent Linux readiness

### Source identity (first-hand, as gaga)

- HEAD: `38f6981b2a60daab2df37c92913a6760c1bef131` — exact R12.
- Tree: `6854675d6b37a49d5302d6104103bdbfbc268040` — exact R12 tree.
- Tracked state: clean (`git status --porcelain=v1` empty).
- R11 `c48e358451c8b0dbe7aace281f64a2eced399ed7` ancestor of HEAD: exit 0.
- Production baseline `58cb1ba31c102e0e9d5488721e8c438adb55c614` ancestor of
  HEAD: exit 0.
- `git fsck --connectivity-only --no-dangling`: clean, exit 0.
- Normal git operation as `gaga`: yes (Linux-native storage; no
  safe.directory workaround needed or present).
- Infrastructure `infrastructure/wp05-independent-linux-readiness-2`:
  P1 `d9955dd0820938d245241c6b24469b33785fcf46`,
  P2 `f8397bb5e2ab91bdd3449d3f72441307c437e7c6`,
  P3 `5df22242b652b3b82c7a2697aef4a3eb1187f7d8`,
  P3b `d3480b5d7e48e93d4f4de855d8da9b245e236764` all resolve and build on the
  earlier readiness chain (`7909522` I3 → `5c04d32` I3b, both present).
  **P3c `0f29bb54921d11306f14fad1288851efd349c26a` is NOT resolvable in the
  Linux checkout, the Windows checkout, or the bundle** (bundle tip is P3b).
  Recorded as an unresolved producer claim; no readiness artifact used by this
  review depended on P3c content.
- Preserved hostile probe:
  `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ForgedAbortOracleHostileProbeTests.cs`
  at R12 is Git blob `d427c19f7e0aaf635a24bc877b4eb511acbb89a2`, 1810 bytes —
  matches the required identity byte-exact (worktree copy re-hashes identical).
- Readiness tool blob: `/home/gaga/qualification/tools/linux-m1-independent-readiness.sh`
  hashes to `93c4dec426372c2cd89be05f3e617ed20f3b24eb` — byte-exact.
- Portable bundle `wp05-opencode-preparation.bundle`: size 2,121,754 bytes,
  SHA-256 `b1bd41d69a86f3758168fc2e3b91b429f3185465d3a25dc20861f0dcd651fee5`,
  eight refs; verified but not restored (checkout already verified).

### Operator launcher boundary (first-hand audit)

- `/root/wp05/run-as-gaga-in-host.sh`: root:root, mode 700 (only root can
  invoke). Behavior: requires uid 0; requires host leaf
  `/sys/fs/cgroup/gaga-delegated/host` to exist; writes its own PID into
  `$host/cgroup.procs`; then `exec setpriv --reuid=1000 --regid=1000
  --clear-groups env HOME=/home/gaga TMPDIR=/tmp USER=gaga LOGNAME=gaga
  GAGAMBA_DELEGATED_CGROUP=/sys/fs/cgroup/gaga-delegated
  DOTNET_CLI_HOME=/home/gaga DOTNET_CLI_TELEMETRY_OPTOUT=1 "$@"`.
- Assessment: privileges are dropped before any requested command runs; no
  production target can run as root through it; no persistent process or host
  configuration change; no arbitrary-root surface beyond the intended
  place-and-drop. `/root/wp05/provision-session-cgroup.sh` (root:root 700)
  audited read-only: refuses an existing delegation, enables only
  `+memory +pids`, chowns parent and host leaf to gaga:gaga 755, and verifies
  writability via `setpriv` as gaga. No unexpected host changes found.

### Linux environment (measured this boot)

- Boot ID: `69d053fb-0fd9-4209-aa5b-be128424ed34` (new boot; not copied from
  any earlier record).
- Distribution: Ubuntu 26.04.1 LTS; kernel `6.6.114.1-microsoft-standard-WSL2`;
  x86_64.
- Qualification identity: uid=1000(gaga) gid=1000(gaga); CapInh=CapPrm=
  CapEff=CapAmb=0; NoNewPrivs=0; CapBnd `000001ffffffffff`.
- cgroup v2 unified (`cgroup2fs`); root controllers
  `cpuset cpu io memory hugetlb pids rdma`.
- Delegation: `/sys/fs/cgroup/gaga-delegated` 755 gaga:gaga; controllers
  offered `cpu memory pids`; `cgroup.subtree_control` = `memory pids`;
  `cgroup.kill` present. Host leaf `/sys/fs/cgroup/gaga-delegated/host` 755
  gaga:gaga, controllers `memory pids`, writable by gaga.
- Session anchor: PID 576, `sleep infinity`, `0::/init.scope`, alive — the
  documented session anchor outside the host leaf; left running (not
  terminated) per instructions.
- Bubblewrap: `/usr/bin/bwrap`, `bubblewrap 0.11.1`, SHA-256
  `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`, mode 755
  non-setuid.
- Toolchain: .NET SDK 10.0.112; gcc 15.2.0; Python 3.14.4; glibc 2.43; git
  present.
- Seccomp artifact (staged, untracked/ignored local file):
  `artifacts/linux-m1/wp04/seccomp-filter.bin`, 672 bytes, SHA-256
  `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`.

### Readiness preflight (reviewer execution, not the producer's)

- Command: `wsl.exe -d Ubuntu -u root -- bash /root/wp05/run-as-gaga-in-host.sh
  /bin/bash /home/gaga/qualification/tools/run-preflight-once.sh
  reviewer-readiness`
- Observed process placement: `CURRENT_CGROUP=/gaga-delegated/host`;
  `HOST_PID=2068`; identity uid=1000(gaga) — the launcher placed the process
  in the delegated host leaf and dropped privileges before qualification code.
- Result: `READINESS=PASS`, `SUMMARY pass=57 fail=0`, exit code 0. All 57
  checks passed first-hand, including `cgroup.placement`, the delegated
  controller family, probe cgroup create/write/remove, toolchain, pinned
  bwrap, workspace write, exact source revision/ancestor/production-surface
  checks, and seccomp artifact pin (`filter.artifact.pin` PASS).
- The producer's 57/57 claim is therefore reconciled against my own run of the
  same script blob (`93c4dec4…`).

### Cleanup after readiness

- Probe cgroup `/sys/fs/cgroup/gaga-delegated/gagamba-readiness-2078` removed
  (script-reported PASS; directory listing verified — only `host` remains).
- Script temp workspace `/tmp/gagamba-wp05-readiness.eX86KV` removed by the
  EXIT trap (verified absent).
- `run-readiness-in-r12.sh` temp copy
  `$repo/eng/.wp05-readiness-$$.sh` removed (repo `git status` clean).
- Host leaf `cgroup.procs` empty after the run; no disposable process remains.
- Session anchor PID 576 intentionally left running (session lifecycle).
- No host configuration was modified by the reviewer.

### Q0 disposition

`WP05 INDEPENDENT LINUX READINESS ACCEPTED - R12 HARNESS QUALIFICATION MAY PROCEED`

Residual note (non-blocking, preserved): infrastructure commit P3c
`0f29bb54921d11306f14fad1288851efd349c26a` is not resolvable from any
repository accessible to this reviewer; the bundle tip is P3b. Nothing in the
qualification path used P3c content.

The sections below are appended by later Q1-Q3 commits.
