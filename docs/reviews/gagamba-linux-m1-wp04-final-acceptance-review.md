# Independent WP04 Final Acceptance Review

## Revision binding

Verified exact full SHAs from Git; branch `qualification/ar7-feasibility`; working tree clean.

| Role | Revision |
|---|---|
| Production implementation (frozen) | `bac516eeb640ce4ea415043a86579c20948793c2` |
| Qualification/probe commit | `3873d2398b31763b4b1200338ea3947bcb1fc1e1` |
| Successful qualification evidence/status | `8afca146cd09841c7a8f58b922cbdacd9398f41e` (HEAD) |
| Incomplete-run preservation checkpoint | `2eb021a9f20fd45137ad166d1899f0fe1d35ac9b` |
| R1 implementation | `6814b0e262509a57ee1761984fd1fc819aefe5f7` |
| R2 implementation | `c03227f64a2397395efbc109e28fb63fcaea9049` |
| R3 implementation | `bac516eeb640ce4ea415043a86579c20948793c2` |
| G2 acceptance checkpoint / accepted implementation | `bf9747d12e0020ba681c82e6980e695d15cfab6e` / `40e8add` (docs HEAD `5b882c1`; review file present) |

- Chain is linear and intact: `… 96fcda7 → fc0852c → bac516e → a60f9a6 → 2eb021a → 3873d23 → 8afca14` (245 commits). The incomplete record was committed at `2eb021a`, its file is byte-unchanged since, and no history rewrite hides it. The successful document links it explicitly as historical.
- `3873d23` touches only six test/fixture files (no docs, no production). `8afca14` touches only four documentation files (evidence index, final evidence, handoff, work-queue).

## Authoritative WP04 boundary

Reviewed strictly as held coordinator + pinned Bubblewrap adapter: atomic owned setup, HELD acceptance, abort, recovery/reconciliation; no managed release, no target initialization, no fixture execution. WP05 release and WP10 execution remain deferred and are not claimed.

## Production freeze

- `git diff --name-status bac516e..HEAD` yields only `docs/**` and `tests/**`. `git diff bac516e..HEAD -- src`, `-- eng`, `-- native` are empty.
- `3873d23`: `tests/Fixtures/wp04-qualification-coordinator.c`, `CoordinatorMountVerifierTests.cs`, `CoordinatorProtocolQualificationTests.cs`, `NativeCopyInTests.cs`, `NonRootQualificationTests.cs`, `OwnerProtocolTests.cs` — no BubblewrapAdapter, PidFdChildReaper, native coordinator, owner/recovery, seccomp, protocol codec, descriptor map, or release code.
- Apparent worktree/archive differences in seven `src/*.cs` and one test file were **CRLF-only**; each file is byte-identical to the `3873d23` git blob (SHA-256 match), and the qualification archive equals those blobs. Production source is unchanged after `bac516e` and the tested archive is exactly `3873d23`.
- The test-only coordinator fixture itself states and demonstrates “no target launch or RELEASE path”; it never forks/execs.

## Environment

- Run as UID/EUID/GID 1000 (`gaga`), cgroup `0::/gaga-delegated/host`, `CapInh/Prm/Eff/Bnd/Amb` all zero, `NoNewPrivs=1`; global cgroup root stayed `root:root 0555`; delegation `gaga:gaga 755`; controllers `memory pids`; no provisioning or widening.
- Recorded Ubuntu 26.04.1, kernel `6.6.114.1-microsoft-standard-WSL2`, x86_64, glibc 2.43, .NET 10.0.112, WSL2 context, boot ID `18785e6a-…`, host PID 631, keepalive PID 573 in `/init.scope`.
- Independently re-checked live (read-only): boot ID identical, PID 631 alive in `/gaga-delegated/host`, PID 573 alive in `/init.scope`, delegated tree contains only `host`, no zombies. Root-only evidence was not substituted for the non-root WP04 rows; `NonRootDelegatedPreflightSucceeds` passed inside the same run.

## Full-suite assessment

Retained TRX parsed directly: **424 result rows — 389 Passed, 35 NotExecuted, 0 Failed**; counters `total=424 executed=389 passed=389 failed=0 error=0`. Exact executed test total: **389**. The 35 `RunInfo` warnings are skip reports, not passes.

## Skip classification

| Family | Skips | Reason | WP04 impact |
|---|---:|---|---|
| `DomainAuthorityTests` | 19 | global `/sys/fs/cgroup/gagamba-r2-*` probes outside the bounded delegation | none; global-root-only, accepted G2 evidence preserved |
| `IntegratedBarrierTests` | 14 | global `/sys/fs/cgroup/gagamba-r4-*` + `openat2` probes outside delegation | none; equivalent WP04 matrix ran non-root |
| `RecoveryHarness` | 2 | global-root mount/boot and missing-delegation probes | none |

Guards pre-date WP04 and live in files untouched by WP04 commits. No WP04-specific row was skipped — every WP04/owner-death/protocol row appears as `Passed`.

## Replacement-race qualification correction

The earlier `NativeCopyInTests.ReplacementRaceDeterministicallyRefused` failure was a UID-1000 harness assumption: `File.WriteAllBytes` on a mode-0500 staged object (root could, `gaga` cannot), reproduced as `InvariantFailure`. The committed probe now pre-creates the replacement, sets 0500, and atomically renames it over the staged name through the writable parent. It asserts the hook succeeded, `(device,inode)` changed, replacement content is at the staged path, production refuses with `ObjectReplaced` + “identity does not match”, and cleanup reports `IdentityMismatch` while preserving the replacement. A separate positive control proves the rename replaces a 0500 entry and changes identity. This models the intended path-replacement threat (stronger than the old write), not a weakened test; production `SafeCopyIn` was untouched. Focused negative 1/1, positive 1/1.

## Atomic prepare-held

The normal HELD test exercises the production coordinator and asserts: direct-child owner, journal stages through intent/registered/`Prepared`, retained empty-domain lease with `memory.max=134217728`, `memory.swap.max=0`, `pids.max=48`, retained staging, live owner/pidfd, competitor journal-writer refused, coordinator inside the domain, HELD accepted, `Completed` false, target and marker absent. Ownership precedes sandbox work; no unowned interval is exposed; recovery authority is durable.

## HELD acceptance

HELD is accepted only after coordinator identity, execution/preparation/generation/sequence, descriptor-free status framing, uid/gid/caps=0, `NoNewPrivs=1`, seccomp mode 2, dumpable=0, rlimits, object identity, cgroup containment, coordinator liveness, private namespace inodes (all six differ), mount identity/topology, exact scratch capacities (7168k / 1024k), security mount options, and exact inventory pass. Acceptance does not release or complete the target.

## Target-not-initialized oracle

The independent fixture marker is `gagamba-m1-fixture: ctor-marker`. Verified absence:
1. after HELD acceptance (adapter-captured streams + target absence in domain);
2. after abort (adapter capture);
3. after final cleanup/reconciliation (independent `strace` trace-log read in the dedicated traced TRX; zero marker occurrences across all traces and the full TRX).
The 12 protocol/setup negatives additionally ran under the final-marker trace and passed 12/12 with no marker; owner-loss pre-accept and abort-drain runs also carry independent marker absence. The pre-HELD startup loss relies on structural impossibility of target release plus pre-kill target absence (noted below).

## Release boundary

No managed release API/path exists or was exercised; no release command was sent; no fixture target initialized or executed; fixture image only built/bound as the oracle identity. `exec-coordinator.c` explicitly implements “release-disabled, HELD” and no target launch. WP05 and WP10 remain deferred.

## Normal abort

HELD accepted → abort accepted, owner exited/reaped, cleanup requested, marker absent; domain `populated 0` and empty; staging removed; reconciliation twice `CleanupConfirmed` (destructive on first, stable on repeat); no staging/cgroup residue; target and (traced) final marker absent.

## Owner death while HELD

Real SIGKILL of the live owner after HELD; pre-kill checks prove live pidfd, live process, retained recovery/staging authority, populated domain, writer exclusion; pidfd proves exit; the scope reaps and reconciles through retained authority; domain removed; target absence asserted; R1 reaping semantics preserved.

## Process-loss matrix

Fresh 7/7 D1–D6 + D5 replacement (`after-intent`, `after-domain-retained`, `after-registered`, `after-staging-accounted`, `mid-staging`, replacement-survives, `after-prepared-before-mint`), plus WP04 boundaries: registered domain before coordinator start, coordinator startup before HELD, complete HELD frame buffered before managed acceptance (`strace` syscall-entry barrier), accepted HELD, owner loss during abort drain (`strace` read-entry barrier). Barriers are deterministic (journal barriers, `/proc/<pid>/syscall` polling, `strace` injection), not sleeps; each row records pre-kill durable state, exact loss point, recovery outcome, cleanup, and target result. No row was inherited; all ran in the final TRX (focused barrier runs in dedicated TRXs from the same workspace).

## Protocol/setup negatives

12/12 end-to-end: incomplete frame, malformed frame, wrong type, wrong execution, wrong generation, wrong sequence, wrong preparation identity, setup exit before HELD, invalid mount report, host-namespace claim, corrupt pinned filter, missing filter. Each refused HELD/completion, kept the target uninitialized, stopped/reaped the owner, retained `Prepared`, reconciled boundedly, and (trace run) read the final marker absent. Mode 11 (valid synthetic HELD) is the accepted positive control, then aborts and reconciles — proving the negatives fail for the intended reasons, not merely parser rejection.

## Descriptor boundary

`OwnerProtocolTests` 21/21 (including the added wrong-execution decode assertion) and `DomainHandleTransferTests` 10/10 fresh non-root: exact roles/order, duplicate/wrong-role, missing/extra descriptors, truncation, CLOEXEC, wrong identity/lease/version, reserved bytes. Coordinator status frames carry no descriptors. `close_range` policy 7/7. The descriptor model and `close_range(6, UINT_MAX, 0)` arrangement are unchanged by the freeze.

## R1 integration

Fresh `PidFdChildReaperTests` 3/3: pidfd readiness vs reaping, exactly-once status consumption, zero/nonzero statuses. Production reaper code unchanged; no competing reaper or zombie observed in the final audit.

## R2 integration

Pinned filter remains 672 bytes / `4ee63c48…` (source pin and artifact recomputed); coordinator reaches HELD under seccomp in the production HELD test; policy controls 7/7; no bypass or widening after R2. The deferred wrong-architecture `KILL_THREAD` vs historical `KILL_PROCESS` generator decision is test-tooling-only and does not invalidate the qualified x86_64 environment or the pinned artifact.

## R3 integration

Final verifier suite **20/20**, including the previously requested controls — malformed `size=bad`, empty `size=`, duplicate mount IDs, prohibited `shared:` / `master:` propagation — plus exact capacity, identity, topology, host-device separation, namespace identity, and options. Actual HELD values 7168k = 7,340,032 and 1024k = 1,048,576 with distinct anonymous devices vs host 0:63/0:72. Production verifier unchanged by the probe commit.

## Canonical vector

`ManifestTests.CanonicalGoldenBytes` 1/1; 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`; codec/protocol production code unchanged.

## Native artifact binding

Recomputed from the qualification workspace: `bwrap` 80,424 / `523da3e7…`; **exec-coordinator 878,792 / `4c0c25af…`** (closing the R3 gap — executed artifact, not just source revision; `exec-coordinator.c` `d6b8265c…`); filter 672 / `4ee63c48…`; fixture 735,432 / `ee25a876…`; adapter 358,400 / `6722ca0a…`; contracts (`Gagamba.Execution.dll`) 126,464 / `afb5ca7e…`; test assembly 349,184 / `e8790094…`; owner helper 7,168 / `b0a07154…3dca4549cc30` (workspace JSON and recomputation agree). The test coordinator source `6f2475…` and all 12 modes are bound as reproducible builds; durable doc records the same values except one malformed owner-helper hash line — see errata below.

## Cleanup assessment

Final audit plus live re-check: boot unchanged; host 631 alive in its host leaf; keepalive 573 alive outside the delegation; ownership/modes/controllers unchanged; global root unchanged; no child cgroups beyond `host`; no zombies, wrapper, coordinator, Bubblewrap, or testhost; no new staging/test temp dirs. The six `/tmp/gagamba-r5-*` directories pre-date the final run (mtimes 16:17/16:58 vs run 18:39–18:46), contain R5-era artifacts, were already recorded by the prior audit, and were not touched or depended on by WP04. Leaving them untouched is correct; attribution is unambiguous and does not compromise cleanup. The retained `/tmp/gagamba-wp04-qualification-final-3873d23` workspace is qualification evidence, not staging.

## G2 preservation

Production freeze plus additive, test-only probes; no descriptor, framing, recovery, journal, fencing, capability-locality, exact-identity cleanup, reconciliation, identity-validation, or pidfd-topology behavior changed. Fresh owner-protocol/domain-handle/reaper/recovery rows pass. No concrete counterexample to any accepted G2 property was found.

## Evidence honesty

The incomplete record is preserved unchanged with its truthful 354 passed / 35 skipped / 1 failed, missing process-loss/protocol rows, and missing final marker read. The successful document separately records the fixed-run facts, the classified skips, the resolved deficiencies, and the deferred items; it claims `QUALIFIED – independent review required`, not acceptance. Verified violations: the 354/35/1 arithmetic, the 389/35/0 TRX totals, the +34-test delta from the probe commit, and the fresh (not inherited) TRX timestamps (run 18:40; probe commit 18:39). One **non-blocking documentation erratum**: the durable evidence line 144 prints a 62-character owner-helper hash (`…9f3c2d92eedd0038…`); the correct 64-character value (`…9f3c2b2d92eedd0038…`) is in the retained raw artifact record and was independently recomputed. It is a transcription typo in a non-load-bearing row; the load-bearing `exec-coordinator` binding is correct and verified.

## Remaining limitations

- WP05 release protocol and WP10 target execution remain deferred.
- Deferred wrong-audit-architecture seccomp atom decision (test-tooling only; does not invalidate the qualified environment).
- Global-root-only `DomainAuthorityTests` / `IntegratedBarrierTests` / `RecoveryHarness` cases remain environment-skipped; they are not WP04 cases, and equivalent accepted G2 evidence is preserved.
- Raw TRX/trace/JSON evidence resides in the retained `/tmp` qualification workspace rather than the repository; the durable document binds the counts/hashes and was independently cross-checked live.
- The pre-HELD owner-loss row’s post-cleanup marker result is structural (target release impossible before HELD + pre-kill target absence) rather than a traced post-cleanup read; the other required boundaries carry independent trace reads.
- Owner-helper hash erratum noted above (docs-only correction recommended; not a proof gap).

## Final disposition

The package establishes, against one fixed production revision (`bac516e` content, tested via the `3873d23` test-only archive), atomic owned setup, trusted Bubblewrap/coordinator startup, pinned seccomp, exact protocol validation, descriptor authority separation, private namespace/mount topology, observable HELD, independent target non-initialization, unreachable release, normal abort/cleanup, owner-death recovery, required process-loss behavior, fail-closed protocol/setup negatives, R1/R2/R3 composition, unchanged canonical vector, truthful cleanup, and no WP04-required case hidden by skip.

WP04 ACCEPTED - HELD SANDBOX QUALIFICATION COMPLETE
