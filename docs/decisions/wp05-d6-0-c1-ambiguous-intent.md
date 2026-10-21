# WP05 D6.0 C1 - R09 Ambiguous ReleaseIntent

Status: **PROPOSED - NOT APPROVED**

Decision owner: human WP05 release/security decision holder, after independent
GLM design review. DeepSeek is the design author; this record does not approve
or implement C1.

## 1. Original requirement

C1 (required-changes blob `73eb09af...`): distinguish a definite pre-Intent
persistence failure from an ambiguous seal-release failure in which both slots
may hold the fully durable Intent content while the write reports failure;
never report a false definite `NotCommitted`; in the ambiguous case request a
bounded stop; define the stop mechanics while the journal is uncertain and
unpersistable; preserve eventual `MayHaveExecuted` recovery and no FD4.

Requirement IDs: R09; blocked gates G3-A11 and G3-B08.

## 2. Current R12 behavior (`SOURCE-CONFIRMED`)

- `ReleaseArbiter.RequestRelease` calls
  `OwnershipJournal.WriteTransition` inside `_journalGate`
  (`Execution/ReleaseArbiter.cs:110-114`).
- Every failure is collapsed to
  `Refused(NotCommitted, "release intent was not durably acknowledged")`.
- `OwnershipJournal.Write` writes complete, fsynced content into both slots
  before removing `gen.seal` (`Recovery/OwnershipJournal.cs:424-489`). A final
  `Unlink(gen.seal)` failure returns `"seal release failed, commit ambiguous"`
  while both slots can be durable (`:486-489`).
- `Resolve` treats any present seal as uncertain (`:773-780`).
  `TryRecoverCommit` requires the real fence and refuses while a live writer
  holds it (`:712-770`).
- `RequestStop` accepts runtime stop, then attempts `WriteMutation`; under a
  seal that mutation fails and `DurableStop=false`, but runtime
  `_stopAccepted` remains (`ReleaseArbiter.cs:226-296`).
- `ReleaseRecoveryClassifier` maps any ReleaseRecord, including Intent, to
  `MayHaveExecuted` (`Recovery/ReleaseRecoveryClassification.cs:28-38`).

The live owner cannot run recovery while it retains the writer lease:
`TryRecoverCommit` takes the same exclusive fence and refuses while the lease
is live. A live lease must be relinquished only by process death/disposal, not
by handing it to a replacement. The only bounded stop channel in the wedge is
runtime acceptance in the live owner; a durable StopRecord cannot be written
while the seal blocks journal mutation. Durable stop evidence is best-effort;
the post-death seal recovery and the absence of resend are the backstop.

## 3. Independent evidence

- `SOURCE-CONFIRMED`: the two failure shapes are distinct in journal source but
  collapsed in `RequestRelease`.
- `PRODUCER-PROBED` / `INDEPENDENTLY REPRODUCED`: D2
  `AmbiguousSealFailureLeavesUncertainIntentAndNoStop`; D4 `D4-6`
  (`Refused(NotCommitted)`, 0 transport, seal error, recovery Intent ->
  `MayHaveExecuted`); D2 `DefiniteIntentFailureIsDistinctFromAmbiguousPersistence`.
- `UNPROVEN`: production owner-level bounded domain stop in the wedge; no
  production owner exists at R12 (WP08/WP10 deferred).

## 4. Candidate contracts

| Option | Contract | Assessment |
|---|---|---|
| C1-A | Add an explicit ambiguous-persistence release decision; `MayHaveExecuted`; no FD4; bounded runtime stop; best-effort durable stop; retained ambiguous result. | Recommended: truthful and explicit; small runtime API change; no schema change. |
| C1-B | Keep `Refused`, add a separate uncertainty classification field. | Viable but two fields must be checked; easier to misread as ordinary refusal. |
| C1-C | Keep `NotCommitted` and document that it may not describe durable truth. | Rejected: violates R09 and makes consumer retries/telemetry unsafe. |

## 5. Recommended C1 contract

1. Definite write failure before any Intent content is acknowledged:
   `Refused(NotCommitted)`, no FD4, no false durable claim. Capability remains
   live unless a separate stop wins (C2).
2. Ambiguous seal-release failure after both slots hold Intent:
   - `ReleaseDecisionKind.AmbiguousPersistence` (or an explicit equivalent
     named in the implementation review); `DurableRelease=MayHaveExecuted`;
     `Delivery=NotAttempted`; `DurableCommitted=false`;
     `LiveExecutionExcluded=true`; reason identifies journal ambiguity.
   - No FD4 and no retry of the intent write.
   - Request `RequestStop(StopCauseKind.CallerStop)` after releasing the
     journal gate. C2 consumes/invalidate the capability before any later
     release.
   - Runtime stop acceptance is the bounded channel; with a valid clock the
     runtime deadline is `now + 5_000_000_000 ns`; with clock unavailable or
     overflow it is zero. Durable StopRecord is attempted best-effort and may
     remain absent.
   - The live owner retains its fence until it exits or disposes; it does not
     transfer the lease to another process. Recovery after death acquires a
     fresh lease, resolves the seal, and classifies Intent conservatively.
3. The ambiguous result is retained. Later same-instance calls receive
   `Duplicate` of the ambiguous result; they cannot retry release. This is the
   C4 interaction.
4. Repeated ambiguity/stop persistence failure remains runtime-only; no false
   durable stop, no FD4, no fresh reattempt.
5. Operator-visible state must identify: runtime stop accepted, durable stop
   absent, journal ambiguous, no FD4 attempt, recovery required.
6. Capability state follows C2: not live after stop acceptance. If C1 lands
   before C2, C1 must still consume the capability itself; the recommended
   implementation order is C2 first.
7. Domain stop and cleanup execution remain WP08/WP10; C1 does not invent an
   owner integration or target behavior.

Linearization point: the capability consumption/stop acceptance under `_gate`
after the ambiguous journal result is observed. Durable Intent is already
content-durable; durable stop is necessarily best-effort.

Forbidden: false `NotCommitted`; FD4; second intent write; treating seal
uncertainty as absence; handing the live lease to a replacement; claiming a
durable stop when the write failed.

Production symbols expected: `ReleaseArbiter.RequestRelease`,
`ReleaseDecisionKind`/`ReleaseDecision`, possibly a small stop/uncertainty
field. No `OwnershipSnapshot` v3 change.

## 6. Rejected alternatives

- C1-B: not selected because the recommended explicit decision/uncertainty is
  less likely to be misread by consumers.
- C1-C: rejected as incompatible with R09's explicit no-false-NotCommitted
  requirement.
- Durable stop retry loop while sealed: rejected; it cannot create a durable
  StopRecord and would block the owner indefinitely.
- Releasing the writer lease in-process to let recovery run: rejected; it
  creates an unnecessary live-owner/second-writer ambiguity and is outside the
  single-writer contract. Process death/disposal is the handoff.

## 7. Acceptance tests (post-authorization implementation)

1. Definite pre-Intent write failure: `Refused`, no intent, no FD4.
2. Ambiguous post-content seal failure: explicit ambiguous decision, no FD4.
3. Both slots contain Intent after ambiguity (read slots/recovery evidence).
4. No false definite `NotCommitted`.
5. Bounded runtime stop requested and recorded; durable stop attempted.
6. Forced durable-stop failure during wedge: runtime accepted, durable absent,
   no claim otherwise.
7. Retained ambiguous result: later calls Duplicate; no second write.
8. Owner SIGKILL; fresh writer acquisition.
9. Seal recovery returns Intent; classifier `MayHaveExecuted`.
10. No second RELEASE/FD4 attempt; no second coordinator acceptance.
11. Capability not reusable after stop (C2).
12. Cleanup bounded; no target execution.

Reusable fixtures: D2 ambiguous seal probe, D4 fresh-process supervisor and
D4-6, D5 no-target process/status harness; tests must be rerun against the new
production tree and separately evidenced.

## 8. Compatibility, risks and open questions

- No schema change; runtime enum/decision compatibility matters. Internal
  consumers and tests must be updated for the new decision kind.
- New risk: callers might treat `AmbiguousPersistence` as a definite failure;
  documentation and decision tests must forbid that.
- The domain stop is not implemented by the arbiter; WP08/WP10 must consume the
  runtime stop and perform bounded domain cleanup.
- Open: exact name/field shape of the explicit decision; whether a distinct
  durable-uncertainty field is needed in addition to the decision kind.
- Open: human policy on whether a runtime-only bounded stop is acceptable when
  no durable StopRecord can be written. Recommendation: yes, with truthful
  uncertainty and recovery/operator backstop.

Review status: independent GLM design review required. Approval: **NOT
APPROVED**.

## D6.1 revision - A-C1-1..A-C1-3

Status: **PROPOSED - NOT APPROVED** (this revision supersedes the earlier
D6.0 draft text; the D6.0 version remains recoverable at `23df1c5...`).

### Exact ambiguity classification and vocabulary

A definite failure and an ambiguous seal-release failure must be distinct at
the arbiter boundary. The proposed minimal internal change is a typed
journal-write outcome: `JournalIoResult`/the transition result carries an
explicit ambiguity flag set only for the post-content `gen.seal` unlink
failure (`OwnershipJournal.cs:486-489`); it is not set for a definite write,
fsync, rename, or pre-content failure. String matching on the existing error
text is not the contract.

The selected proposed representation is
`ReleaseDecisionKind.AmbiguousPersistence` (a new explicit kind), with:

- `ReleaseDisposition.MayHaveExecuted`
- `DeliveryResult.NotAttempted`
- `DurableCommitted = false`
- no FD4 write
- retained in `_retainedRelease`
- reason identifying journal ambiguity, not definite failure.

`ReleaseDecisionKind.Stopped` is not reused: it denotes a stop result, not a
journal persistence result. A separate uncertainty field remains a fallback
only if implementation review demonstrates a concrete compatibility reason;
the proposed default is the explicit kind.

Consumer inventory to update/exhaustively verify: `ReleaseDecisionKind` and
`ReleaseDecision.Kind` in `Execution/ReleaseDecision.cs`;
`ReleaseArbiter` retained/return paths; `ReleaseArbiterTests`;
`Wp05ReleaseContractProbes`; `Wp05ForgedAbortOracle*` and probe tests; and
`Wp05ReleasePathIndependentQualificationTests`. No serializer or persisted
journal field consumes this runtime kind at R12. Every switch/pattern match,
log/projection and test expectation must be reviewed; adding an enum member is
not assumed behavior-neutral.

### Lock-safe sequence (A-C1-1)

The revised proposed sequence is:

1. `RequestRelease` has already completed its prechecks without `_gate`
   (prechecks are outside the runtime gate). It enters `_journalGate` for the
   intent attempt (`ReleaseArbiter.cs:81-115`).
2. `WriteTransition` returns a typed ambiguous outcome. The release path leaves
   the `_journalGate` block; it must not hold `_journalGate` while acquiring
   `_gate`, and must not hold `_gate` across journal I/O.
3. The release path acquires `_gate`, retains
   `ReleaseDecisionKind.AmbiguousPersistence`, and releases `_gate`. This
   retention is the visibility point for concurrent callers: a later caller
   sees the retained ambiguous result and receives `Duplicate`; it cannot
   start a new release.
4. After `_gate` is released, the same call invokes the bounded stop as a
   **tail action**. `RequestStop` may then take `_gate` for runtime acceptance
   (C2 consumption), release it, and take `_journalGate` for best-effort
   durable stop persistence. There is no nested `_gate` -> `_journalGate`
   call originating inside the release journal block.
5. The stop's failure or inability to persist never changes the retained
   ambiguous result and never authorizes FD4. If the stop invocation cannot
   complete, the result remains ambiguous and the owner/recovery boundary is
   responsible for bounded cleanup.

The exact visibility ordering is: durable journal attempt -> ambiguous typed
result -> release of journal serialization -> runtime retention under `_gate`
-> release of `_gate` -> stop tail action. C# monitor re-entrancy is not used
as proof; the opposing-order concurrent test is required.

### Capability state (A-C1-3)

C1 relies on C2 as an implementation prerequisite. Between the ambiguous
journal result and stop acceptance, the capability can still be live, but the
sealed journal refuses every further release transition/acknowledged baseline,
so no FD4 can be authorized. C2 consumes the capability at runtime stop
acceptance. If stop persistence fails, the capability is still consumed; if
the owner dies before stop completes, the replacement has no capability and
must recover from the sealed journal. No RELEASE is resent. This transient
window is documented, not claimed corrected in R12.

### Tests to add under later authorization

- `AmbiguousReleaseStopLockOrderProbe`: barriers at the journal/`_gate`
  transition and stop acceptance; opposing ordering; detect inversion,
  deadlock, journal I/O under `_gate`, second FD4, wrong retention.
- `AmbiguousWedgeThenClockUnavailableStop`: compose ambiguous wedge with a
  clock-unavailable stop; no FD4, no false definite NotCommitted, runtime stop,
  C2 consumption, no fabricated StopRecord, retained ambiguity, owner death,
  conservative recovery, no resend.
- Definite/ambiguous distinction; both-slot Intent; retained Duplicate;
  stop-failure; fresh writer/recovery; no second Intent/FD4.

### Open human question

Approve the explicit `AmbiguousPersistence` decision, retained ambiguity,
lock-safe tail stop and C2-based capability closure? **PENDING HUMAN.**

## D6.2 revision - R3 retention collision and R4 fixture provenance

Status: **PROPOSED - NOT APPROVED**
Design status: **DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW**
Human contract approval: **PENDING HUMAN**

This section resolves only GLM D6.1-R3 and D6.1-R4. It does not redesign C1,
change C2/C4 semantics, select the C3 policy, approve any contract, authorize
remediation or qualify release. It explicitly supersedes the D6.1 statement
that the ambiguous path unconditionally retains `AmbiguousPersistence`.

### R3 first-retainer ordering

The D6.1 sequence remains lock-safe: the journal attempt completes under
`_journalGate`; the ambiguous result is returned; `_journalGate` is released;
retention, if it occurs, is under `_gate`; the bounded stop is a tail action
after `_gate` is released. No journal I/O occurs under `_gate`, and no
`_gate`/`_journalGate` nesting is introduced (`ReleaseArbiter.cs:1-5,81-131,
226-296`).

Retention is **conditional**. The R12 after-intent `_gate` block checks the
existing retained release first (`ReleaseArbiter.cs:120-123`), then
`_stopAccepted` (`:125-131`), before any gate checks. Between the
`_journalGate` release at `:115` and the ambiguous caller's `_gate`
acquisition, a concurrent stop can win `_gate`, or another release caller can
retain a stop-won refusal first. The ambiguous caller must obey those existing
first-retainer rules.

#### Case R3-A - ambiguity retention wins first

1. The journal operation returns an ambiguous persistence result.
2. The release caller obtains `_gate`; no retained release exists and no stop
   has been accepted.
3. The caller retains `AmbiguousPersistence` with `MayHaveExecuted`,
   `DeliveryResult.NotAttempted`, `DurableCommitted=false` and no FD4.
4. The caller releases `_gate`, then invokes the bounded stop as a tail action.
5. If the tail stop encounters an already accepted stop, it returns the
   retained stop duplicate; otherwise its first acceptance is the C2
   consumption point. No lock inversion is introduced.
6. Later release callers receive `Duplicate` of the retained ambiguous result.

#### Case R3-B - stop acceptance wins first

1. The journal becomes ambiguous; the failing release caller releases
   `_journalGate`.
2. A competing stop obtains `_gate` first, is accepted, and C2 consumes the
   capability at first stop acceptance.
3. The original release caller later enters `_gate` for retention and sees
   `_stopAccepted`.
4. The existing stop-won rule takes precedence. The stop already consumed the
   capability at its first acceptance; the release caller's existing
   consume attempt is a no-op. The caller retains the stop-won release refusal;
   it does **not** retain a second, separately manufactured
   `AmbiguousPersistence` result.
5. The caller returns that retained stop-won refusal. A later release caller
   receives `Duplicate` of it. The stop's own `_retainedStop` is a separate
   slot and does not populate `_retainedRelease`; a prior `_retainedRelease`
   would have been caught by the entry/after-intent duplicate rule before the
   journal phase (R3-C).
6. Its tail stop is idempotent. The journal ambiguity remains a real durable
   evidence fact.

The retained stop-won refusal is:

| Field | Value |
|---|---|
| `ReleaseDecisionKind` | `Refused` |
| `DurableRelease` | `MayHaveExecuted` |
| `Delivery` | `DeliveryResult.NotAttempted` |
| `DurableCommitted` | `false` |
| `LiveExecutionExcluded` | `true` |
| Reason | exact source string `release gate refused: stop won during durable intent` |
| Retention | retained in `_retainedRelease`; later calls `Duplicate` |

It is a runtime result, not a durable `NotCommitted` finding. It must not be
read as proof that no durable Intent exists. A sealed journal may still
recover to `MayHaveExecuted`; recovery remains conservative and no RELEASE is
resent.

#### Case R3-C - another decision is already retained

If `_retainedRelease` already exists when the ambiguous caller reaches the
after-intent retention point, it returns `Duplicate` of that retained decision
and does not overwrite it. This is a defensive first-retainer rule, not a claim
that the state is normally reachable after the journal phase. In the reviewed
single-arbiter model, a prior `_retainedRelease` from an earlier release call
is caught at the entry check before any second intent; a prior
`AmbiguousPersistence` cannot be present for the same first ambiguous call; and
the stop's `_retainedStop` is a separate slot that does not populate
`_retainedRelease`. D6.2 does not manufacture a prior stop-won refusal or use
an unreachable state as a test premise. It does not add a second
retained-decision slot or a new durable record.

#### R3 ordering summary

| Ordering | First retainer | Ambiguous caller result | Later release callers | Tail stop | Capability | Durable journal |
|---|---|---|---|---|---|---|
| Ambiguity first | Ambiguous caller | `AmbiguousPersistence` | `Duplicate` of ambiguous | Duplicate/idempotent if stop already accepted | C2 consume at first stop acceptance | Sealed/Intent ambiguity preserved; recovery may be `MayHaveExecuted` |
| Stop first | Stop acceptance | Stop-won `Refused` retained by the ambiguous caller (or `Duplicate` if a prior retained decision exists) | `Duplicate` of stop-won refusal | Idempotent | Consumed by stop; second consume has no effect | Sealed/Intent ambiguity preserved; stop-won refusal does not assert absent Intent |
| Existing retained | Prior `_retainedRelease` at entry | Entry `Duplicate`; defensive after-intent rule only | `Duplicate` of prior | Idempotent | Unchanged | Unchanged; no second Intent/FD4 |

### R3 lock and physical invariants

- Journal I/O remains outside `_gate`; retention and first-retainer checks are
  under `_gate`; the tail stop follows `_gate` release.
- No blocking wait is added between release and stop callers merely to force
  identical results; no monitor reentrancy is used as proof.
- First-retainer rules are preserved. If a future contract requires changing
  them, that is a new explicit human decision outside D6.2.
- The journal ambiguity remains real regardless of which runtime result wins.
  No definite NotCommitted statement may erase it.
- No second Intent, no second FD4, C2 consumption remains irreversible, and
  runtime stop remains idempotent.
- `Duplicate` of a stop-won refusal does not establish absence of durable
  Intent; recovery reads the journal conservatively and may classify
  `MayHaveExecuted`.
- No new durable journal schema or second durable record is introduced.

### R3 required deterministic opposing-order tests

Extend `AmbiguousReleaseStopLockOrderProbe` with deterministic barriers, not
sleeps. The barrier names below are proposed test-only names; the exact
`TestBarrier` strings are an implementation detail.

| Test | Barrier placement | Expected winner / returned kind | Durable / capability state | FD4 / journal | Recovery / no-resend | Negative assertion |
|---|---|---|---|---|---|---|
| R3-T1 ambiguity-first | after `_journalGate` release, before ambiguous `_gate` retention | Ambiguous caller; `AmbiguousPersistence`; later release `Duplicate` | Journal sealed/Intent; C2 consumed at tail stop acceptance | 0 FD4; no second Intent | Recovery `MayHaveExecuted`; no resend | No lock inversion; no journal I/O under `_gate` |
| R3-T2 stop-first | after `_journalGate` release, before ambiguous retention; hold stop at `_gate` | Stop; release `Refused` stop-won (or `Duplicate` if a prior retained decision exists); exact reason | Journal sealed/Intent; capability consumed by stop | 0 FD4; no second Intent | Recovery `MayHaveExecuted`; no resend | Release must not retain `AmbiguousPersistence` ahead of stop-won rule |
| R3-T3 existing retained | prior release decision retained at entry; a separate defensive assertion covers the after-intent guard without claiming it is reachable | Prior retained result; ambiguous caller `Duplicate` at entry | Journal unchanged; capability state unchanged | 0 FD4; no overwrite | Recovery conservative; no resend | Prior decision not overwritten; defensive after-intent branch does not invent a reachable stop-won prior result |
| R3-T4 concurrent committed writer opposing order | barrier between committed `_journalGate` write (`ReleaseArbiter.cs:185-223`) and ambiguous retention; test-only opposing composition | Committed writer completes its own durable evidence; ambiguous caller retention/refusal remains safe | No shared second Intent; committed evidence remains separate | 0 FD4 for ambiguous path; one release write max | Recovery classification remains separate | No `_gate`/`_journalGate` inversion, deadlock or shared-record corruption |
| R3-T5 clock-unavailable stop collision | stop uses `now <= 0` path (`:237-244`) before ambiguous retention | Stop accepted; release `Refused`/`Duplicate`; exact stop-won reason | No durable StopRecord; capability consumed; journal ambiguity | 0 FD4; no fabricated deadline | Recovery `MayHaveExecuted`; no resend | No fresh five-second allowance |
| R3-T6 durable-stop persistence failure | stop accepted, then journal write failure after gate release (`:270-289`) | Stop remains accepted; release `Refused`/`Duplicate` | Runtime stop only; capability consumed; journal ambiguity | 0 FD4; no durable StopRecord | Recovery `MayHaveExecuted`; no resend | Stop failure does not rewrite retained release result |
| R3-T7 repeated release after winner retained | after retention, additional release callers | `Duplicate` of retained ambiguous or stop-won result | No new intent; capability state unchanged | 0 additional FD4 | No resend | No new retained decision |
| R3-T8 owner death after ambiguous persistence | SIGKILL after ambiguous retention and/or after stop collision | Runtime winner retained as above; replacement gets no capability | Journal sealed/Intent; fresh writer recovers | 0 FD4; no second write | Seal recovery -> `MayHaveExecuted`; no resend | No capability resurrection; no second Intent |

Common assertions for every row: bounded test watchdog; no deadlock; no
`_gate` held across journal I/O; no second physical RELEASE; no second Intent;
the durable journal is classified separately from the runtime result; C2
capability consumption is exactly once at first stop acceptance; duplicate stop
does not consume again.

No tests are implemented by D6.2.

### R4 fixture source provenance correction

D6.1 cited `Wp05ReleaseContractProbes` in the consumer inventory as though it
were part of frozen R12. It is not. The corrected binding is:

| Item | Verified value |
|---|---|
| R12 production | `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`; the fixture file is absent |
| Fixture branch | `implementation/wp05-release-qualification-fixtures` |
| Fixture test commit | `dc4c372b180abef1b27038d94f49af90226fa548` (parent `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c`), tree `1f1c8669affd63ae2fc48d20429241e505751000` |
| Fixture evidence tip | `338a98af269e6b5839f3ccc2e0881f016153b01e` |
| Test file | `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` |
| Git blob | `ccde61763664a4ed56e6d1941c20d61cc0cf2b5f` |
| SHA-256 of test file | `cd5cd00c4c4ef213239047a85ed8f26129e45bb5cad0ddd2c3fe646a407da2b4` |
| Fixture `src` tree | `406f52da43b62920128151b2f5706c32d45b6db7`, identical to R12 `src` |
| Fixture `native` | no diff from R12 |
| Evidence manifest | `docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-artifacts/SHA256SUMS` (blob `e203b5c3954d5c4e27b2cc17478208f9f174999c`) and `source-head.txt` (blob `e35e47640dd95394ec6bf4159dbad5f46ed67c37`) |
| Evidence document | `docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-2026-10-20.md` (blob `fd7dc44eb7815f5747ac860bf2e6eb6749bb17bc`) |
| Diagnostics | `contract-probes.log` blob `03beaf8863099d96cdfb8ce82297398cbf9d6083`; 13/13 passed in the recorded fixture profile |

Relevant test methods at that fixture commit (13 facts):

`StopWithUnavailableClockIsLiveAcceptedWithoutDurableStop`,
`StopDeadlineOverflowIsLiveAcceptedWithoutDurableStop`,
`AmbiguousSealFailureLeavesUncertainIntentAndNoStop`,
`DefiniteIntentFailureIsDistinctFromAmbiguousPersistence`,
`StopFirstLeavesCapabilityLiveOnR12`,
`DurableStopBlocksReleaseIntentFromSecondArbiter`,
`StopPersistenceFailureAllowsSecondArbiterReleaseOnR12`,
`ConcurrentDuplicateReleaseProducesOnePhysicalWrite`,
`ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired`,
`StatusBufferDoesNotPersistBeforeDurableCommitted`,
`StatusBeforeCommittedPersistenceFailureDoesNotCreateObservation`,
`StatusReaderRejectsWrongIdentityAndSequence`,
`StatusReaderRejectsEofAndMalformedFrames`.

The fixture is test-only diagnostic evidence, not an R12 production symbol.
It is not copied into R12 history, not merged, and not rerun by D6.2. The
consumer inventory is now split into:

- **R12 production consumers:** `ReleaseDecisionKind`/`ReleaseDecision.Kind` in
  `Execution/ReleaseDecision.cs`; `ReleaseArbiter` retained/return paths
  (`Execution/ReleaseArbiter.cs:50-52,120-131,177,301-303`); R12 tests
  `ReleaseArbiterTests`, `Wp05ForgedAbortOracle*`,
  `Wp05ReleasePathIndependentQualificationTests`; no production switch or
  serializer consumes the runtime kind at R12.
- **Test-only fixture asset:** `Wp05ReleaseContractProbes` at the fixture
  commit above, used to reproduce the wedge/failed-stop diagnostics against
  R12 behavior. Its evidence is evidence of R12 behavior only; it does not
  qualify future changed production.

### C1 D6.2 status

- R3 design text: **ADDRESSED - AWAITING INDEPENDENT REVIEW**.
- R4 design text: **ADDRESSED - AWAITING INDEPENDENT REVIEW**.
- C1 contract: **PROPOSED - NOT APPROVED; HUMAN DECISION PENDING**.
- No production, test or fixture change is authorized.
