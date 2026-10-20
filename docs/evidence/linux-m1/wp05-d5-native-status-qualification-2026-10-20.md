# WP05 D5 Native FD5 and Cross-Process Qualification - 2026-10-20

Status: test-only D5 implementation and producer evidence. Independent GLM/Beam
review is required. No production remediation, target execution, C1-C4
resolution, D6 or release acceptance is claimed.

Disposition: `WP05 D5 NATIVE FD5 AND CROSS-PROCESS QUALIFICATION FIXTURES
IMPLEMENTED - INDEPENDENT REVIEW REQUIRED; C1-C4 UNRESOLVED; RELEASE NOT
QUALIFIED`.

## A. Repository binding

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`; isolated
  worktree `C:\Users\Laszlos\source\repos\gagamba-d5`.
- Branch: `implementation/wp05-d5-native-status-qualification`.
- Start: preservation `5c58b065c37a71c8f392a617f725822ffb64c171`.
- Product: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Commits:
  - D5.0 `c3cc27e` execution plan/CR inventory.
  - D5.1 `ed49ed5` fixture corrections and cross-process probes.
  - D5.2 `dbef38a` native FD5 status probes.
  - D5.3 `fa028c2` native persistence/cleanup.
  - D5.1b `30a8eb9` strengthened two-owner contention.\n  - D5.1c `a990982` cross-owner fence refusal; evidence source HEAD.
- Production `src`/production `native` diff against R12: **empty**.
- Frozen Q3 harness and its test sources are unchanged.

## B. Correction register

| CR | Action | Evidence | Remaining limitation |
|---|---|---|---|
| CR-1 | Documented the reviewed control-based termination standard. D5 does not claim raw `WIFSIGNALED` from public `Process.ExitCode`; it relies on direct-child SIGKILL, audited bounded exit set, fence release and the independent Q5 wait4/.NET controls. | `termination-oracle.txt` | Public ExitCode alone cannot distinguish voluntary exit 137; no native wait4 witness was added in D5. |
| CR-2 | Corrected D4 host/test/design wording: supervisor holds no release-purpose writer; it transiently probes the fence after death. | source diff / D4 design | None |
| CR-3 | Added D5-CR3: owner A holds the writer and releases after barrier; a second owner process with its own test-only capability calls `RequestRelease` through a test-composed no-writer lease and is refused; after A releases, a fresh retry is refused. Exactly one FD4 attempt and one durable Committed result. | `compact/...D5-CR3...` | R12's single writer fence means two simultaneous acquired-writer `RequestRelease` calls are not representable without production change; the second owner's lease is test-composed, not acquired authority. |
| CR-3b | Added D5-CR3b: owner accepts durable stop, release is refused, owner dies; fresh replacement acquires its own lease and retry is refused by durable StopRecord; no process-local stop state is inherited and no FD4 occurs. | `compact/...D5-CR3b...` | R12 has no `StopDecision.Accepted=false` path; the observable refused generation is release-after-stop/durable-stop. Failed-durable-stop remains the D4-8 diagnostic. |
| CR-4 | Documented `Environment.ProcessId > 0` as a test-only caller-liveness surrogate. | D4 design correction | Not a WP08/WP10 owner-liveness authority check. |
| CR-5 | Documented `go.txt` as fixture-only with no writer identity binding. | D4 design correction | Not suitable for production-context authority. |
| CR-6 | D5-D4-7 asserts persisted deadline equals accepted time + `StopConfirmationAllowanceNanoseconds` in the owner result and durable StopRecord. | D4 supervisor log/result | R27/C3 remains unresolved; no production change. |
| CR-7 | D4 witness append now uses one bounded O_APPEND write per event with `FileOptions.WriteThrough`; no unbounded lock added. | source diff | Missing events remain unknown; no claim of universal atomicity beyond the supported Linux profile. |
| CR-8 | Documented in `docs/testing-and-ci.md`: `ProcessGlobalReaping` classes run in a dedicated host, but xUnit v3 may fan out facts; each fact owns independent resources. Frozen runner unchanged. | docs diff | No forced class serialization; D4/D5 are designed for fan-out. |
| CR-9 | D5-D4-1 compares full `pid:starttime` from `/proc/<pid>/stat` with `owner.ready`, not only PID. | D4 supervisor log | Start-time identity still depends on `/proc` and the tested Linux profile. |
| CR-10 | Informational only; no implementation change. | This table | None |
| CR-11 | `OwnerSession.MirrorRegistered` uses `OwnershipSnapshotCodec.CurrentVersion` (3), matching the frozen v3 claim; D4/D5 tests pass. | source diff / D4 log | None |

## C. Cross-process completion

### CE-04 / CR-3
- Owner A: real R12 `ReleaseArbiter`/`HeldExecution`, holds real journal fence, parks before release.
- Owner B: independent process with its own test-composed capability and a
  test-composed stale no-writer lease; calls `RequestRelease` while A holds the
  writer and is refused with zero transport attempts.
- A releases after the supervisor barrier: one real FD4 write, coordinator exit 0,
  durable Committed.
- Replacement/retry process acquires the journal only after A exits and its own
  release attempt over the committed journal is refused with zero transport.
- Both process identities and results are preserved in the case artifacts.
- The test-composed no-writer lease is explicitly not a production authority
  bypass; R12 cannot have two acquired writers for one journal.

### CR-3b / stop machinery
- Original owner accepts a durable stop and then a release request is refused;
  capability remains live in that dead process.
- Owner is SIGKILLed; the durable journal contains Stop and no Release.
- A fresh replacement acquires its own writer lease, recovers the StopRecord,
  and a new test-only capability release attempt is refused without transport.
- No runtime `_stopAccepted` state crosses processes; the refusal is derived
  from the durable StopRecord.
- Failed-durable-stop remains a separate diagnostic (D4-8): no durable stop,
  replacement diagnostic Lane-A release only.

## D. Native FD5

- Test-only coordinator source:
  `tests/Fixtures/wp05-qualification-coordinator.c`
  SHA-256 `16da6f3fd672a793549aea802bad70afed092247351fa3179d9fa39ff0b5fea3`.
- D5 binary distinct from historical `e20903cc...`:
  `artifacts/linux-m1/wp05-d5/wp05-qualification-coordinator`
  SHA-256 `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3`.
- The production coordinator, protocol definitions and production native sources
  are unchanged.

Positive native cases (real FD5 bytes, coordinator exit 0): ExecConfirmed,
ExecFailed, ExecConfirmed+RootWait, RootWait-without-confirm, ExecFailed+RootWait,
ProtocolFailed. Reader results and raw frames are preserved under
`native-status-run1/` and `native-status-run2/`.

Negative native cases use coordinator modes for wrong execution/preparation/digest,
wrong/replayed sequence, duplicate exec, root-after-terminal, contradictory
transition, wrong magic/version, short header, truncated payload, invalid length
bounds, invalid kind/payload length, premature EOF and trailing bytes. Each
reader rejection is recorded without durable observation.

Descriptor topology:
- Test owner creates an inheritable status pipe and coordinator status handle.
- Coordinator duplicates the status handle to FD5.
- `/proc/<coordinator>/fd/5` and the owner read descriptor both resolve to the
  same `pipe:[inode]` (captured in `descriptor-topology.txt`).
- `d5_hold_exec` holds the coordinator alive for descriptor inspection.
- EOF/short/truncated/no-status cases produce no fabricated observation.

Release validation exits 20 (incomplete), 21 (extra input) and 22 (invalid
frame) are executed as separate native cases; none is treated as accepted
release or FD5 status.

## E. Durable observation

- D5-O01: native ExecConfirmed+Root frames buffer before durable Committed; a
  pre-Mark flush writes nothing; after `MarkDurableCommitted` and real journal
  flush, Exec/Root are durable.
- D5-O02: without durable Committed, no durable Exec/Root is written.
- D5-O03: journal write failure after marking does not persist Exec and preserves
  prior journal truth.
- D5-O04: contradictory native status is rejected and an existing valid Exec
  observation is not overwritten.
- D5-O05 (owner dies after native status but before persistence) is **HELD**:
  no faithful fresh-process owner/FD5 seam exists in R12 test-only topology;
  no same-process reconstruction was substituted.

## F. Formal release-path conversion

Each method was run separately at exact D5 source `a990982` on the supported
profile with the generated native fixture, WP04 coordinator and frozen filter.

| Row | Exact method | Requirement mapping | Result | Raw evidence |
|---|---|---|---|---|
| D5-RP-1 | `HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary` | WP05 release gate, one FD4 delivery, pre-exec coordinator boundary (R01/R10/R12/R13) | **PASS - NATIVE RELEASE-PATH OBSERVED** | `release-path-1.log` |
| D5-RP-2 | `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport` | Identity refusal before intent/transport (R01/R02/R12) | **PASS - NATIVE RELEASE-PATH OBSERVED** | `release-path-2.log` |
| D5-RP-3 | `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent` | Overall expiry -> PROTOCOL_FAILED 30; gate refuses expired intent (R05/R12/R15) | **PASS - NATIVE RELEASE-PATH OBSERVED** | `release-path-3.log` |

All three: no target execution, no production post-release FD5, no full release
acceptance. A green incidental D4 filter is not the formal conversion; these
separate rows and raw logs are the D5 conversion.

## G. Execution

Environment: Ubuntu WSL2, kernel `6.6.114.1-microsoft-standard-WSL2`, boot
`69d053fb-0fd9-4209-aa5b-be128424ed34`, uid 1000 `gaga`, delegated
`/gaga-delegated/host`, capability sets zero, .NET `10.0.112`, GCC `15.2.0`,
Bubblewrap `0.11.1`.

| Group | Result |
|---|---|
| D4 supervisor + D5 CR-3/CR-3b | **17/17**, twice |
| D5 native status | **32/32**, twice |
| Focused D1-D3/protocol/release regressions | **66/0/0** |
| Formal release-path rows | **1/1 each**, three separate runs |
| Native build | 0 errors |

Cleanup: delegated host cgroup empty; no owner, qualification-coordinator or
exec-coordinator process remains; no `/tmp/gagamba-d5-*` residue; no target
executed.

## H. Evidence and manifest

Report: this file.
Artifacts: `docs/evidence/linux-m1/wp05-d5-native-status-qualification-artifacts/`.
Includes run logs, native-status raw frame directories, descriptor topology,
D4/D5 compact case records, source/environment identity, cleanup and manifest.

The `SHA256SUMS` covers 256 preserved artifact entries and was verified
against committed Git blob bytes by `git archive` plus `sha256sum -c`:
**256 entries, 0 mismatches**. The report SHA-256 in `report-sha256.txt` also
matches. Historical D4 evidence and the historical qualification-coordinator
identity are not rewritten.

## I. Remaining work

1. Independent GLM/Beam D5 review, including the test-composed no-writer lease
   limitation for CR-3 and the control-based CR-1 standard.
2. D5-O05 fresh-process status/persistence seam (HELD).
3. Additional destructive descriptor negatives (inherited extra writer/wrong FD)
   are not claimed; see exclusions.
4. D6 C1-C4 decisions and separate production remediation if required.
5. WP08/WP10 owner and target integration.
6. No G3/WP05 release acceptance.

## J. Frozen claims

- R12 production, native production sources, protocol and seccomp are unchanged.
- Frozen harness Q3 and D1-D4 evidence are unchanged.
- No target was executed.
- C1-C4 remain unresolved.
- No D6, G3 or WP05 release acceptance is claimed.
