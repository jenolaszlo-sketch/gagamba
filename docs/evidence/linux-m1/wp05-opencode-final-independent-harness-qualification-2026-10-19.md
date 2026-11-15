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

## Q1 — Reviewer-authored hostile probes of the harness observation/oracle (commit `bf453ea`)

### New review-only test file

- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05OpencodeReviewerProbeTests.cs` —
  six untagged reviewer probes; committed on the review branch only. Committed
  content SHA-256: `df21e395cfd6f0536bc8abd520dc66826c07ab602e136931a95f2e3fadfb73ae`.
  Untagged on purpose: the probes wait on specific pids/pidfds only, never on
  process-global wait state, so they run in the regular parallel phase and were
  qualified there (Q2) rather than in an isolated lane.

- Probe semantics (test-only harness behavior):
  1. `UnrelatedChildIsNeverAttributedToTheObservedPidfd` — pidfd scoping: an
     unrelated live child is never observed as the wrapped child; it survives,
     takes its own SIGKILL, and is reaped separately.
  2. `PeekedStatusIsNotConsumedAndReapedPidfdObservationFailsClosed` — after the
     WNOWAIT peek the raw waitpid still reaps (status not consumed), and a
     second observation over a consumed pidfd returns Unknown with an error
     (ECHILD fail-closed), never a fabricated valid record.
  3. `ObserverStartedAfterZombieStillObservesExitStatus` — a late observer over a
     retained zombie still observes Exited/137.
  4. `ClosedPidfdObservationFailsClosedWithoutHang` — a closed/stale fd number
     yields Unknown + an error immediately (poll EBADF fail-closed).
  5. `BareWrapperExitCodeIsAlwaysRejected` — the preserved five-argument
     compatibility overload rejects 0/133/137 unconditionally: a bare exit
     status can never validate an abort.
  6. `OracleAcceptsOnlyStructuredExit133OrSigkill9` — positive controls (Exited/133
     and Killed/SIGKILL both accept, so the harness can construct valid
     evidence), then mutations all reject: exit 137, non-SIGKILL signal 15,
     Unknown kind, dumped kind, identity mismatch, un-stopped stop, injected
     journal stop fact, consumed capability, non-refusal decision.

- Execution: five focused runs as uid 1000, CapEff 0, host-leaf placement
  (`0::/gaga-delegated/host`); six of six passed in every run (durations
  51-55 ms; the fifth instrumented run is preserved in
  `q1-reviewer-probe-run.log`). Residue audits after every run: zero zombies, no
  leftover probe children, no coordinator/bwrap/testhost residue, no child
  cgroups below the delegated parent, session anchor PID 576 intact.

### Staged-artifact reproducibility (first-hand)

`eng/linux-m1/build-native.sh` executed in the reviewer worktree (compile only;
fixtures and coordinators never executed). All four native artifacts reproduce
the producer's recorded hashes byte-exactly:

- `tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture`
  `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`
- `artifacts/linux-m1/wp04/exec-coordinator`
  `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`
- `artifacts/linux-m1/wp04/seccomp-filter.bin`
  `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`
- `artifacts/linux-m1/wp05/wp05-qualification-coordinator`
  `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`

The full identity record is in `q1-reviewer-probe-identity.txt`.

### Evidence-integrity reconciliation (first-hand, carried into this commit)

All three disappearance-manifest audits were recomputed as reviewer artifacts
committed in `bf453ea` (`q2-evidence-integrity-reconciliation.txt`,
`q2-crlf-cause-reconstruction.txt`, `q2-crlf-exhaustive.txt`):

- R9 era 23-entry manifest: 20 committed entries match, exactly 3 mismatch.
  CRLF causes conclusively reproduced:
  - `observer-observation.txt` and `source-head.txt`: committed LF bytes plus
    one CR before the final newline (hit positions 251 and 175 respectively);
  - `sigpipe-observation.txt`: all-lines CRLF.
- R11/R12 remediation manifest: 28 of 28 committed entries match.
- Qualification-harness-remediation manifest: 18 of 18 committed entries match.
- `source-head.txt` inside that manifest records R7 (`6e534c8...`), the
  historical revision at its creation — no relabeling.

## Q2 — Full supported qualification profile, two fresh runs (commit `21e53ad`)

Command per run: `bash eng/linux-m1-qualified-tests.sh` (Release configuration;
the supported paid profile; the bare parallel assembly was explicitly not used
as an acceptance profile), as uid 1000 with CapEff 0, placed at
`0::/gaga-delegated/host`, boot `69d053fb-0fd9-4209-aa5b-be128424ed34`.
Identical results in both runs, runner exit 0:

| phase | run 1 | run 2 |
| --- | --- | --- |
| regular parallel (process-global excluded) | 489 passed / 0 skipped | 489 passed / 0 skipped |
| NonRootQualificationTests (WP04, real delegation) | 31 passed | 31 passed |
| OwnerDeathQualificationTests | 7 passed | 7 passed |
| IntegratedBarrierTests | 1 pass / 14 skip | 1 pass / 14 skip |
| DomainAuthorityTests | 2 pass / 19 skip | 2 pass / 19 skip |
| RecoveryHarness | 3 pass / 2 skip | 3 pass / 2 skip |
| Wp05TerminationObserverTests | 3 passed | 3 passed |
| Wp05SigpipeDispositionTests | 1 passed | 1 passed |

Skip classification (35 root-only environmental gates, identical in both runs):

- `IntegratedBarrierTests`, `DomainAuthorityTests`, `RecoveryHarness` gate on
  child-cgroup creation authority at the bare `/sys/fs/cgroup` root. First-hand
  proof (`q2-root-only-gate-probe.txt`): as gaga, `/sys/fs/cgroup` is
  `root:root 555`, `mkdir` fails EPERM, while the delegated parent and host leaf
  are `gaga:gaga 755`. `OwnerDeathQualificationTests`, which gates on
  delegated-cgroup capability rather than bare-root ownership, passes 7/7 in
  the same topology.
- Producer parity: the producer's own R11 supported-profile run reports the
  same 14/19/2 root-only skips and phase-2 48 passed / 35 skipped
  (`wp05-identity-evidence-integrity-remediation-2026-10-19.md`, lines 98-103).
- Row arithmetic: producer R11 phase 1 was 483 rows; the reviewer worktree adds
  six untagged probes -> 483 + 6 = 489, matching both runs above with 0 skips in
  the parallel phase (the probes ran; they did not skip).
- These 35 lanes are WP02-era/WP03-era/WP04 auxiliary classes, not WP05
  release-path rows. Every WP05-scoped lane (release-path 3, observer 3,
  sigpipe 1, WP04 31, plus the forged-abort/oracle and V3 rows inside the 489)
  ran and passed.

Post-run audits (both runs, `q2-residue-audit.txt`): no zombies, no probe
children, no coordinator/bwrap/testhost residue, no child cgroups below the
delegated parent, no `/tmp/gagamba*` residue from these runs, session anchor
PID 576 alive.

## Q3 — Final review facts (this commit)

- Focused release-path class rerun with per-row console preserved:
  `q3-release-path-focused.log` (3/3 passed; real wrapped coordinator, bwrap,
  staged frozen artifacts).
- Production surface diff (`q3-production-diff.txt`): baseline `58cb1ba` to R12
  over `src/`, `native/`, `artifacts/` — EMPTY (first-hand). Reviewer branch
  relative to R12 over the production surface — EMPTY (first-hand). R12 tree
  hash `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen native host artifacts: `exec-coordinator` `5e55e92b...`, filter
  `2aa87901...` (672 B), bwrap 0.11.1 `523da3e7...` mode 755, all re-verified
  this session.
- Artifact manifest `SHA256SUMS` in this directory covers every committed
  evidence artifact at this commit (excludes itself).
- Cleanup: host leaf returns to empty after each launch; the delegated tree
  survives only while the session anchor lives; no qualification cgroups remain.

## Acceptance decision (bounded)

**WP05 HARNESS QUALIFIED (bounded) — independent OpenCode reviewer position,
2026-10-19.**

The WP05 Linux M1 test harness as exercised at exact R12 in this delegated
non-root operator topology is qualified:

1. Readiness: 57/0 first-hand (Q0) with the audited root-only launcher boundary,
   zero capabilities, host-leaf placement, and the exact documented session
   topology.
2. Evidence integrity: the R9-era manifest anomaly is conclusively explained
   (CRLF), reproduced at byte level, recomputed, and the committed manifest
   contents are correct; both later manifests reconcile 28/28 and 18/18.
3. Harness behavior: six reviewer probes demonstrate that the test-only
   observation/oracle machinery is pidfd-scoped, WNOWAIT-non-consuming,
   fail-closed on consumed/stale handles, and that the accepted abort contract
   rejects every forged or degraded input class (including the bare-exit path
   and the 137-status path).
4. Qualification runs: the full supported profile twice with identical results
   (489-row parallel phase + seven process-global phases; phase-2 48/35);
   every WP05-scoped lane passes; residue-free after each run.
5. Reproducibility: native artifact staging reproduces producer hashes
   byte-for-byte; the production surface is unchanged through the reviewer
   branch.

Scope and exclusions (explicit, per the remediation-evidence standard):

- This qualifies the test harness and lane infrastructure, NOT WP05 release
  acceptance: no WP10 target workload ran; no production behavior changed; the
  Windows M1 lane was not rerun (the R11 Windows lane remains historical
  producer evidence: 399 passed / 0 failed / 167 skipped).
- The 35 root-only skips are environmental gates on WP02/WP03/WP04 auxiliary
  lanes and are NOT WP05 release-path rows and cannot qualify in this topology;
  they are recorded as pending for any future root-session qualification
  instead of claimed as passed.
- P3c `0f29bb54921d11306f14fad1288851efd349c26a` remains an unresolved producer
  claim (nowhere resolvable); no readiness or qualification fact used its
  content.
- Seccomp policy was not requalified; its frozen identity was verified
  byte-exact (672 B, `2aa87901...`) and used as-is.
- Single-host caveat: all observations come from this workstation (WSL Ubuntu
  guest on this boot of LAPTOP-KBD8D8JG); no cross-host independent
  qualification was available. Recorded as pending future scope, never claimed
  as passed.

## Reviewer commit chain on `review/wp05-opencode-harness-final-qualification`

| commit | slice |
| --- | --- |
| `1e64875ce30f10c5c834188c330b6aae1fbac0ae` | Q0 readiness record |
| `bf453ea` | Q1 hostile probes + evidence-integrity reconciliation artifacts |
| `21e53ad` | Q2 2x full supported-profile runs + skip classification |
| (this commit) | Q3 final review facts, manifest, acceptance decision |
