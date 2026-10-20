# WP05 D5 Execution Plan - Native FD5 and D4 Corrections - 2026-10-20

Status: D5.0 plan only. Production R12 remains unchanged. No release gate is
accepted by this plan.

## 1. Binding

- Start: preservation `5c58b065c37a71c8f392a617f725822ffb64c171`.
- Product: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d...`.
- Harness: Q3 `36a124d7...`; not reopened.
- Review: Q5 `310484a5e82d4a81b0909e07281511cb7bc4c315`.
- Required-work blob `1768ec2a...`.
- D5 branch: `implementation/wp05-d5-native-status-qualification`.

## 2. Correction register

| CR | Finding | D5 action | Mandatory | Owner/evidence |
|---|---|---|---|---|
| CR-1 | ExitCode 137 is not a universal SIGKILL oracle | Bind D5 to the reviewed control-based standard: direct child, audited self-exit set, fence-release corroboration, reviewer wait4/.NET controls; record exact scope and residual public-surface limit. Do not claim raw kernel status from ExitCode. | Oracle/readiness | `termination-oracle.txt`; no production change |
| CR-2 | Supervisor wording says it never acquires a lease, but it transiently probes the fence | Correct class/design wording: no release-purpose lease; transient probe after death only. | Yes (docs) | D4 host/test comments |
| CR-3 | No fresh-process CE-04 second-owner race | Add two genuine owner processes over one real journal: owner A holds writer and releases after barrier; owner B is alive, is refused the fence, then after A death acquires and attempts release with its own test capability; exactly one release, no second transport. Document that R12's single-writer fence makes simultaneous two-writer `RequestRelease` unrepresentable without production change. | Yes | D5 cross-process test |
| CR-3b | No dedicated stop-refused/re-inheritance case | Add fresh owner with durable stop, then release refusal, death, fresh lease, then replacement retry refusal. Compare with failed-durable-stop diagnostic. Document R12 has no accepted=false stop; the observable refusal is release-after-stop / durable stop state. | Yes | D5 cross-process test + result |
| CR-4 | `Environment.ProcessId > 0` surrogate | Document as test-only caller-liveness surrogate; not WP08/WP10 authority. | Yes (docs) | D4 design / D5 evidence |
| CR-5 | `go.txt` gate has no writer identity binding | Document fixture-only limitation; no production authority claim. | Yes (docs) | D4 design / D5 evidence |
| CR-6 | D4-7 did not assert persisted deadline policy | Assert `deadline == acceptedAt + StopConfirmationAllowanceNanoseconds` from owner result and durable StopRecord; no extension; overflow outside this case. | Yes | D5 assertion |
| CR-7 | Concurrent witness append risk | Inspect/test append behavior; keep bounded O_APPEND line records, state Linux atomicity limits, no unbounded lock. Add negative/parse checks if feasible. | Applicable hardening | D5 witness note/test |
| CR-8 | xUnit v3 within-class parallelization | Document supported policy: `ProcessGlobalReaping` runs in a dedicated host; facts may fan out, each owns independent resources; no frozen-runner change. Add test class to dedicated category. | Applicable hardening | `docs/testing-and-ci.md` |
| CR-9 | PID prefix only | Compare full `pid:starttime` from `/proc/<pid>/stat` against `owner.ready`; add assertion. | Yes | D5 supervisor test |
| CR-10 | Informational only | No implementation change. Record no action. | Informational | D5 report |
| CR-11 | Legacy snapshot mirror version | Change test-only `MirrorRegistered` to `CurrentVersion` (3); ensure D4/D5 still pass; if v3 transition changes behavior, record. | Yes | D5 host/test |

No CR item authorizes production remediation. C1-C4 remain unresolved.

## 3. Native FD5 matrix

Test-only qualification coordinator:
`tests/Fixtures/wp05-qualification-coordinator.c` extended only for D5
negative status modes; production `exec-coordinator.c` untouched.

New D5 artifact (distinct from historical
`e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`):
`artifacts/linux-m1/wp05-d5/wp05-qualification-coordinator`, with its own
SHA-256.

Positive: S01 ExecConfirmed; S02 ExecFailed; S03 ExecConfirmed+RootWait;
S04 RootWait-unconfirmed; S05 ExecFailed+RootWait; S06 ProtocolFailed.

Negative: N01 wrong execution, N02 wrong preparation, N03 wrong digest,
N04 wrong sequence, N05 replayed sequence, N06 duplicate exec, N07 root after
terminal, N08 contradictory transition, N09 magic, N10 version, N11 short
header, N12 truncated payload, N13 length below header, N14 length above max,
N15 invalid kind, N16 invalid payload length, N17 premature EOF, N18 deadline,
N19 trailing bytes. Release-validation exits 20/21/22 are separate formal
cases, not FD5 status kinds.

Every negative frame is written by the native coordinator through a real FD5
pipe. The test uses a recording reader over the actual pipe fd; raw bytes and
the coordinator's `/proc/<pid>/fd/5` pipe identity are preserved. Synthetic
queue tests remain component regressions only.

## 4. Descriptor topology

- Test owner creates release and status pipes with inheritable handles.
- Coordinator receives release fd and status fd, duplicates status to FD5.
- Test owner holds the status read end; native coordinator holds FD5 write end.
- Coordinator closes/exit produces EOF; no extra writer is inherited by a
  replacement or helper.
- D5 records `/proc/<coordinator>/fd/5` and the owner read descriptor target;
  both must resolve to the same pipe inode.
- Negative cases include short header/payload, EOF, no status, wrong fd/extra
  writer where safely test-owned, and deadline expiry.

## 5. Durable observation ordering

Real native frames feed the production `CoordinatorStatusReader` and
`ReleaseObservationBuffer` over a real journal (or a test-only failure wrapper
around it). Required: no durable observation before durable Committed; no
fabricated Exec/Root after invalid frames; contradictions do not overwrite;
journal write failure preserves prior truth; fresh owner death before
persistence is HELD unless a faithful seam exists.

## 6. Formal release-path conversion

Run the three exact methods in
`Wp05ReleasePathIndependentQualificationTests` with real generated native
fixture, WP04 coordinator, filter and delegated cgroup. Give each a formal
matrix row, raw log, artifacts, expected/forbidden effects, journal state and
cleanup. No skip is a pass. These are bounded pre-exec facts, not target
execution or production post-release FD5.

## 7. Execution order

1. CR-11/CR-2/CR-4/CR-5/CR-6/CR-9 fixture corrections and D4 regression.
2. CR-3/CR-3b cross-process tests.
3. CR-7/CR-8 witness/parallelism documentation and any safe hardening.
4. Native D5 coordinator modes and D5 artifact build.
5. Native FD5 positive/negative and descriptor tests.
6. Durable observation ordering tests.
7. Formal three release-path evidence runs.
8. D4/D1-D3/protocol regressions, evidence manifest, cleanup, handoff.

## 8. Stop conditions

Stop the affected case if production must change, a target would execute,
synthetic bytes are offered as native FD5, a malformed status becomes durable,
pre-Committed persistence occurs, a second release is attempted, descriptor
leakage makes EOF ambiguous, or native provenance is uncertain. Preserve the
failure and do not weaken the oracle.
