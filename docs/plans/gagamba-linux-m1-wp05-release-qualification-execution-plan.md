# Gagamba Linux M1 — WP05 Release Qualification Execution Plan

Date: 2026-10-19
Status: planning only; no implementation or qualification authorized here
Binding: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`
Harness dependency: `review/wp05-opencode-harness-final-qualification` at Q3
`36a124d7dff98ecfaf956340767fe1768ad5b17f` (bounded acceptance frozen)
Reference: `docs/plans/gagamba-linux-m1-wp05-release-requirements-matrix.md`

## 1. Purpose and boundary

This plan qualifies the WP05 release/stop/intent/transport/trusted-status
components at exact R12 under the supported non-root delegated Linux topology.
It does **not** qualify the production owner release wiring, actual target
execution, or production post-release status. Those remain WP08/WP10 and the
authoritative G3 gate.

The proposed labels `G3-A`–`G3-E` are planning sub-gate labels used for
reviewability. They are not an amendment to the implementation plan. The
authoritative gates remain G2a (WP05 held transport), G2b (WP06 release/stop),
and G3 (WP09/WP10 composition). A full G3 claim is not available at R12.

No production or test code may be changed under this plan until an independent
plan review accepts the required test-only seams and any proposed remediation.

## 2. Prerequisites

1. Independent adversarial review of this plan, the matrix, and the audit by
   GLM or Beam (not the implementation author).
2. Exact R12 source and tree; no later infrastructure/review-only commit may be
   substituted for the product under test.
3. Re-run the qualified readiness preflight in the reviewer's own context:
   non-root gaga, zero effective capabilities, delegated cgroup placement,
   pinned Bubblewrap/filter/toolchain, exact R12 source, writable workspace.
   The prior session anchor/PID is not assumed to survive a reboot.
4. Frozen harness remains accepted; do not modify the harness runner, probes,
   oracle, observer or isolation profile without a separate decision.
5. Test-only fixtures are non-packable and cannot be selected by production.
6. Any proposed production remediation (R27/R08/R09/R02 findings) is separately
   authorized and reviewed; this plan does not implement it.

## 3. Gate overview

| Proposed label | Scope | Can start now? | Blocking issue |
|---|---|---|---|
| G3-A | Release/stop arbitration and stop capability/deadline semantics | After independent plan review; test-only driver required | R27 source gap; R08/R09 decisions |
| G3-B | Durable intent, FD4 delivery and committed/error behavior | After plan review; existing test-only release path plus witness | R11/R13 missing witness/fault tests |
| G3-C | Fresh-process crash and no-resend | Blocked | No fresh-process release owner fixture; R21–R25 untested |
| G3-D | Trusted FD5 status and durable projection | After plan review; test-only native status fixture | Production coordinator has no post-release status; WP10 |
| G3-E | Combined independent disposition | Blocked by A–D and plan review | No production owner/target; explicit exclusions |

The authoritative full G3 remains later and is not represented by these labels.

## 4. Test-only seams and fixtures

### 4.1 Required new test-only fixtures

| Fixture | Purpose | Production impact |
|---|---|---|
| `wp05-release-owner` test-only child process | Runs real `ReleaseArbiter`, journal, prepared capability and `HeldExecution` in a separate process so it can be SIGKILLed at named boundaries | None; test-only executable/project, not production-selected |
| `wp05-release-supervisor` test-only observer | Owns the qualification coordinator/witness pipes; records process identity, kill signal, FD4 attempt ledger and coordinator acceptance; survives owner death | None; external observer, no authority |
| `wp05-release-transport-witness` test-only wrapper | Writes a bounded attempt record to the supervisor before each actual `ReleasePipeWrite` call; wraps `HeldExecution` only in tests | None; must not change production transport semantics |
| `wp05-status-bridge` test-only driver | Feeds qualification-coordinator FD5 frames into `CoordinatorStatusReader` + `ReleaseObservationBuffer` after durable commit | None; production status remains WP10 |
| Fault wrappers | Journal storage failure/ambiguity; committed-write failure; transport result injection; barrier hooks | Test-only where possible; no production fault flag |

The existing `ReleaseArbiter.TestBarrier` (`ReleaseArbiter.cs:23,299`), real
`OwnershipJournal`, `BubblewrapAdapter`, production coordinator and the
`wp05-qualification-coordinator.c` are reusable. Do not build a second harness.

### 4.2 Required production decisions before G3-C/G3-E

The audit found source-level gaps at R27 (stop deadline not consumed), R08
(stop-first capability not invalidated) and R09 (ambiguous intent collapsed to
NotCommitted without stop). These require a decision and, if confirmed as
product defects, a separately authorized production remediation. They are not
implemented by this plan. G3-A may run a diagnostic probe before remediation; it
must not be marked accepted while the invariant remains unmet.

## 5. G3-A — Release/stop arbitration

All cases use the test-only release driver with a real prepared capability,
real `OwnershipJournal`, real `HeldExecution`/production coordinator and a
real domain. No target fork/exec. Deterministic barriers are internal test hooks,
not sleeps.

| ID | Requirement | Initial state | Stimulus/interleaving | Expected durable/side effect | Forbidden | Independent observation | Fresh? | Fixture/change |
|---|---|---|---|---|---|---|---|---|
| G3-A01 | R01/R10 | Prepared, no release/stop | Valid release request | Intent then Committed; one FD4 write; capability consumed | Second write; intent after expiry; target init | Journal sequence; FD4 attempt ledger; coordinator exit 0 | No | Existing Q3 test composition + witness |
| G3-A02 | R03/R08 | Prepared, no release | Stop first; then release request | StopRecord; no intent/write; capability invalidated | Intent; FD4 write; live capability | Journal; capability state; FD4 ledger | No | Driver + capability probe; R08 decision |
| G3-A03 | R03/R01 | Prepared | Release reaches after-intent barrier; stop wins | Intent + Stop; no FD4; MayHaveExecuted | FD4 write; Committed; lost Stop | Journal; FD4 ledger; stop deadline | No | Existing barrier |
| G3-A04 | R05 | Prepared | Clock crosses setup/overall deadline after intent, before gate | Intent remains; no write; MayHaveExecuted | Committed; FD4 | Journal; fake/real clock trace | No | Existing barrier/clock |
| G3-A05 | R02/R06 | Prepared | Two release calls: sequential and concurrent barrier | One FD4 attempt; duplicate/identical decision; no second | Second FD4; divergent winner | FD4 attempt ledger; retained decision | No | Concurrency harness; R02 decision |
| G3-A06 | R04 | Prepared | Release wins; later authorized stop | Committed + Stop; history preserved | Rewrite to NotCommitted; second write | Journal; FD4 ledger; stop result | No | Existing test + real domain stop driver |
| G3-A07 | R03 | StopRecord present | Later release request | Refused; no intent/write; capability invalidated/aborted | Intent; FD4; live capability | Journal; capability state | No | R08 must be resolved |
| G3-A08 | R02/R03 | Prepared | Barrier-controlled concurrent release and stop threads | Exactly one winner; loser has no side effect | Two winners; write after stop; stale decision wins | Journal; barrier trace; FD4 ledger | No | New concurrency test |
| G3-A09 | R04/R28 | Committed / cleanup in progress | Release attempt during cleanup | No second write/Commited rewrite; cleanup independent | False release success; rewritten cleanup | Journal; cleanup outcomes | No | Test-only driver; production cleanup deferred |
| G3-A10 | R27 | StopRecord with absolute deadline | Fresh same-boot recovery after/inside deadline | Remaining allowance used; no reset; bounded urgent stop after expiry | New now+5s; unbounded wait | Journal deadline; recovery clock; domain empty | Yes for takeover | Production remediation or explicit limitation |
| G3-A11 | R09 | Prepared | Durable intent write fails definitely vs ambiguous seal failure | No release; ambiguous case requests stop; conservative state | FD4 write; NotCommitted when intent may exist | Journal state/uncertainty; stop result | No | Fault storage; production decision |

Stop condition for G3-A: any second FD4 delivery, release after a winning stop,
false NotCommitted after ambiguous intent, live capability after stop-first, or
reset stop deadline stops the gate.

## 6. G3-B — Durable intent, transport and committed evidence

Use the real test-only release path plus the test-only transport witness. The
production coordinator is used for pre-exec delivery; the qualification
coordinator may be used for status only in G3-D.

| ID | Requirement | Initial state | Stimulus | Expected durable/side effect | Forbidden | Independent observation | Fresh? | Fixture/change |
|---|---|---|---|---|---|---|---|---|
| G3-B01 | R01/R13 | Prepared | Valid release with witness before FD4 | Intent ack before attempt record before FD4; Committed after | FD4 before intent; second write | Journal sequence; witness order; coordinator acceptance | No | Test-only witness |
| G3-B02 | R13/R10 | Prepared | Full 128-byte write, coordinator accepts | Delivered + Committed; coordinator exits 0 | Target initialization; extra frame | FD4 ledger; coordinator receipt; process exit | No | Existing path |
| G3-B03 | R13 | Prepared | Pipe full -> EAGAIN | WouldBlock; no retry; stop requested | Retry; success claim | FD4 ledger; journal; stop | No | Transport wrapper |
| G3-B04 | R13 | Prepared | Reader closed -> EPIPE | BrokenPipe; no retry; conservative | Retry; success | FD4 ledger; journal | No | Transport wrapper |
| G3-B05 | R13 | Prepared | Short/unknown write (defensive) | Partial/Unknown; conservative; stop | Retry/normalize success | FD4 ledger; journal | No | Fault transport |
| G3-B06 | R11 | Prepared | FD4 delivered; committed journal write fails | Intent remains durable; no resend; live decision conservative | Rewrite intent; retry release | Journal; FD4 ledger; supervisor | No | Fault journal |
| G3-B07 | R12 | Prepared | Truncated/extra/expired/identity-wrong release frame | Coordinator refuses before target; no acceptance | Target init; acceptance | Process exit; stdout; status; FD4 | No | Production coordinator + test-only qualification coordinator |
| G3-B08 | R09 | Prepared | Ambiguous intent persistence | No FD4; stop requested; uncertain intent preserved | FD4; false NotCommitted | Journal uncertainty; stop; witness | No | Fault storage; R09 decision |

Stop condition: physical write before acknowledged intent, second write, or a
Delivered claim without the one write both stop G3-B.

## 7. G3-C — Fresh-process crash and no-resend

### 7.1 Orchestration

For each case:

1. Supervisor starts a fresh `wp05-release-owner` process with a unique test-only
   execution/preparation identity, real journal, real domain and real capability.
2. Owner prepares held, then reaches the named barrier; supervisor records
   owner/coordinator PIDs, cgroup, journal sequence and FD4 attempt ledger.
3. Supervisor sends SIGKILL at the boundary. A barrier that only throws an
   exception is invalid for this gate.
4. Supervisor confirms owner death by pidfd/direct-child wait; the retained
   recovery scope/lease is used by a fresh replacement process.
5. Replacement runs `Reconciler`/classification only; it receives no prepared
   capability and no release writer.
6. Supervisor proves no second FD4 attempt (witness) and no second coordinator
   acceptance (receipt ledger), then performs bounded identity-checked cleanup.

### 7.2 Crash matrix

| ID | Boundary | Latest durable state | Expected disposition | Forbidden | Fresh? | Evidence |
|---|---|---|---|---|---|---|
| G3-C01 | Before intent | R0 | NotCommitted/NotStarted; no FD4 | Intent; write | Yes | Journal; attempt ledger; process death |
| G3-C02 | After intent before gate/write | R1 | MayHaveExecuted/Unknown; no resend | Committed; second attempt | Yes | Intent snapshot; witness; supervisor |
| G3-C03 | After gate before write | R1 | MayHaveExecuted/Unknown; no resend | Physical write claim; second attempt | Yes | Barrier; journal; witness |
| G3-C04 | After write before committed | R1 | MayHaveExecuted; delivery unproven; no resend | Committed claim; second attempt | Yes | Attempt ledger; coordinator receipt; journal |
| G3-C05 | After committed | R2 + delivery | Preserve release/delivery; stop/cleanup | Rewrite facts; second write | Yes | Committed snapshot; recovery |
| G3-C06 | After exec/root persisted | R2 + observations | Preserve observations; cleanup separate | Infer execution from cleanup; second write | Yes | Exec/root snapshots; classifier; cleanup |
| G3-C07 | Concurrent recovery contenders | R1/R2 | One fenced writer; no release from recovery; loser refuses | Two writers; second release | Yes | Lease evidence; attempt/acceptance ledgers |
| G3-C08 | Coordinator dies before status | R1/R2 | Exec/root Unknown; no resend; cleanup | Success/NotStarted inference | Yes | Status EOF; journal; process identity |

No-resend observation: the test-only witness is written **before** the actual
FD4 write and is owned by the supervisor, so it survives owner death. A second
attempt would produce a second witness record even if the pipe is closed. The
coordinator receipt ledger independently records accepted frames. A test that
only checks the final journal state is insufficient.

Stop condition: any witness record after the original crash, any second
coordinator acceptance, any fabricated observation, or any capability/writer
available to the replacement stops G3-C.

## 8. G3-D — Trusted FD5 status

The production coordinator emits HELD and PROTOCOL_FAILED only; it has no
post-release EXEC/ROOT branch. G3-D therefore uses the existing qualification
coordinator (`tests/Fixtures/wp05-qualification-coordinator.c`) with test-only
status modes and a test-only status bridge. This qualifies the status/reader/
durable projection boundary, not production target status. Production status is
WP10 and remains excluded.

| ID | Requirement | Stimulus | Expected durable/side effect | Forbidden | Independent observation | Fresh? |
|---|---|---|---|---|---|---|
| G3-D01 | R15/R17 | ExecConfirmed then RootWait(exit 0) | Committed + Exec Confirmed + Root Exited; buffer flush after commit | Root before exec; observation before commit | FD5 frames; journal; order trace | No |
| G3-D02 | R15/R17 | ExecFailed(errno) then RootWait | FailedBeforeExec + Root NotStarted | Confirmed/Exited inference | FD5; journal | No |
| G3-D03 | R15/R17 | RootWait without confirmation | Exec Unknown + Root Unknown (diagnostic raw wait) | Known root; success | FD5; journal | No |
| G3-D04 | R16 | EOF/truncated/malformed/extra frame | No observation; Unknown; stop/cleanup | Fabricated observation; Committed success | FD5 bytes; reader error; journal | No |
| G3-D05 | R15/R16 | Wrong execution/preparation/digest/sequence; duplicate/out-of-order; status after terminal | Rejection; no durable observation | Accepted frame; second root | Reader trace; journal | No |
| G3-D06 | R18 | Status observed, owner dies before persistence | Prior durable state only; later fact Unknown | Inferred durable observation | Process death; journal | Yes |
| G3-D07 | R18 | Committed without trusted evidence | Release MayHaveExecuted; exec/root Unknown | Success; NotStarted | Classifier; journal | No |
| G3-D08 | R20 | Production coordinator post-release status | Not available at R12 | Claiming production target status | Source/coordinator audit | No |

Status reader / buffer must preserve exact frame identity and plan order; no
stdout, stderr, Bubblewrap JSON, caller bytes, or synthetic target strings may
become durable observations.

## 9. G3-E — Combined independent disposition

After A–D, the independent reviewer composes the evidence and records:

- exact R12, test/fixture revision, environment, boot/kernel, cgroup, capabilities;
- all mandatory matrix rows and their results;
- every failed attempt and skip;
- FD4 witness/coordinator acceptance and FD5 traces;
- journal snapshots and cleanup;
- explicit pre-exec/WP10 exclusions;
- frozen harness remains valid; no production changes outside approved remediation.

A possible bounded disposition after successful independent review is
`WP05 RELEASE PROTOCOL QUALIFIED (bounded pre-exec) - PRODUCTION OWNER/TARGET NOT ACCEPTED`.
That string is not an authorization or a current claim. Full WP05 release
acceptance still requires WP10 and full G3.

## 10. Required implementation changes

- **No production changes are authorized by this plan.**
- **Test-only work required:** release owner process, supervisor/witness,
  transport witness, status bridge, fault wrappers, and test-only matrix drivers.
- **Production remediation candidates (separate decision):** stop-deadline
  consumption (R27), stop-first capability invalidation (R08), ambiguous intent
  handling (R09), concurrent duplicate retention (R02). Do not implement until
  independently confirmed and authorized.
- **Deferred:** WP06/WP07 resources/capture; WP08 owner orchestration; WP10 owner
  release wiring, actual target execution and production post-release status.

## 11. Reviewer responsibilities

- **DeepSeek (implementation author):** source inventory, test-only fixture and
  remediation implementation only after authorization; never self-accept.
- **GLM/Beam independent reviewer:** adversarial plan review, native probes,
  hostile mutations, fresh-process kill qualification, evidence audit, final
  independent disposition.
- **Preservation:** import review history, verify manifests, commit canonical
  status, freeze accepted boundaries; no implementation.

## 12. Evidence structure

For each gate preserve:

- exact source/test revision and environment identity;
- journal snapshots (sequence, Release/Stop/Exec/Root, delivery, cleanup);
- FD4 attempt ledger and coordinator acceptance ledger;
- FD5 raw frames and reader/ordering trace;
- owner/coordinator/replacement PIDs, kill signal and pidfd-exit evidence;
- cgroup/domain state, reaping and cleanup outcomes;
- every failure, unavailable case and skip classification;
- exit codes and bounded SHA-256 artifact manifest.

Raw logs in ignored `artifacts/linux-m1/wp05-release/`; reviewed compact records
under `docs/evidence/linux-m1/`. A green test is not sufficient if the required
side effect or negative observation is absent.

## 13. Stop conditions

Stop the affected gate if:

- intent cannot be proven durable before FD4;
- a second FD4 attempt/delivery is observed after intent;
- recovery obtains release authority or resends;
- stop deadline is reset or not consumed;
- capability remains live after stop-first;
- ambiguous intent is reported as definitely not committed;
- status identity/order is not enforced or durable observations are fabricated;
- a required fresh-process case uses exception simulation instead of SIGKILL;
- the test-only driver cannot be kept outside production selection;
- actual target execution is needed to pass a WP05 row (defer to WP10);
- any mandatory row is skipped or its log is unavailable.

## 14. Expected sequence

1. A1 audit commit; A2 matrix commit; A3 plan/decision commit.
2. Independent plan review by GLM/Beam; decide R02/R08/R09/R27 and test-only
   fixture authorization.
3. Test-only fixture commits (release owner/supervisor/status bridge), no
   production changes.
4. Diagnostic G3-A/G3-B/G3-D runs; if source gaps confirmed, separately
   authorized production remediation and retest.
5. G3-C fresh-process crash/no-resend run.
6. Independent G3-E review and evidence preservation.
7. Full G3/WP10 remains later; WP05 release is not accepted by this plan.

## 15. Acceptance criteria for a later bounded disposition

All mandatory rows R01–R18 and R21–R29 have independent evidence, the frozen
harness remains valid, the Q3 harness acceptance is not reopened, no production
change is unapproved, and the explicit WP10/owner/target exclusions are preserved.
The independent reviewer must reject any missing crash boundary, no-resend
witness, status negative case or stop-deadline proof.

**No release qualification is executed or accepted by this plan.**
