# WP05 D6.0 Release Contract Decision Proposals - 2026-10-20

Status: **PROPOSED - NOT APPROVED**

This is a read-only contract analysis. It does not modify production, tests,
native code, schema, artifacts or the frozen harness. It does not resolve
C1-C4, authorize remediation or accept any gate.

Disposition: `WP05 D6.0 C1-C4 CONTRACT DECISION PROPOSALS COMPLETE - INDEPENDENT
DESIGN REVIEW REQUIRED; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT
QUALIFIED`.

## 1. Git binding

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`.
- D6.0 worktree: `C:\Users\Laszlos\source\repos\gagamba-d6`.
- Branch: `analysis/wp05-d6-0-release-contract-decisions`.
- Base: `578b2084cfa09be76e73d0fb26e09f8d823f9ae6` (D5 preservation).
- Product: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Previous production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Frozen harness: Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D4 preservation: `5c58b06...`; D5 implementation `a4fca00...`; D5 review
  `87cced6...`; D5 preservation `578b208...`.

The decision records and matrix are documentation-only. No R12 production
symbol is changed by this package.

## 2. Source and review inventory

Read and reconciled:

- Original V3 review `e4b0a79...`: independent review, counterexample register
  and required changes C1-C9.
- A1-A3 gap audit, requirements matrix, execution plan and decision queue
  (Q04-Q07).
- D1-D3 contract probes and independent evidence.
- D4 real-process crash/witness/stop evidence and independent review.
- D5 native FD5/cross-process evidence and independent review/H1-H5.
- R12 source: `ReleaseArbiter`, `ReleaseDecision`, `PreparedTransaction`,
  `OwnershipJournal`, `OwnershipTransitions`, `ReleaseRecoveryClassifier`,
  `Reconciler`, `StopRecord`, `DomainLease`, `ReleaseObservationBuffer`,
  `CoordinatorStatusReader`, `HeldExecution`, `ReleasePipeWrite` and owner
  preparation symbols.

Evidence labels used below: `SOURCE-CONFIRMED`, `PRODUCER-PROBED`,
`INDEPENDENTLY REPRODUCED`, `CONTRACT INFERENCE`, `PROPOSED DECISION`,
`UNPROVEN`, `DEFERRED INTEGRATION`.

## 3. Current R12 state-machine summary

### 3.1 Release

`ReleaseArbiter.RequestRelease` (`Execution/ReleaseArbiter.cs:45-223`):

1. Entry under `_gate`: retained decision -> `Duplicate`; `_stopAccepted` ->
   retained `Refused(NotCommitted)` (`:48-61`).
2. Prechecks include clock, deadlines, prerequisites, capability liveness and
   exact identity (`:64-78`).
3. Under `_journalGate`, the current Prepared snapshot is resolved and
   `TryPlanReleaseIntent` is applied; `WriteTransition` must acknowledge
   (`:81-115`). Any WriteTransition failure is collapsed to
   `Refused(NotCommitted)` with no stop request (`:110-114`).
4. `after-intent`, then gate re-check (`:117-146`); capability `TryConsume`
   (`:148-155`); one nonblocking transport write (`:157-175`); retained
   `Released(MayHaveExecuted, delivered outcome)` (`:176-183`).
5. After the gate, `WriteMutation` records Committed; failure leaves Intent and
   `DurableCommitted=false` (`:185-223`).

There is one journal writer fence. `OwnershipJournal.Write` requires both
`lease.IsLive` and `IsEffectiveWriter` (`Recovery/OwnershipJournal.cs:358-370`);
`WriteTransition` and `WriteMutation` add baseline/lineage checks
(`:550-674`). Intent content is fully written to both slots before the seal is
removed (`:424-489`). A reportable failure at the final seal unlink therefore
leaves ambiguity, not a definite absence of Intent (`:486-489`).

### 3.2 Stop

`RequestStop` (`ReleaseArbiter.cs:226-296`):

1. A retained stop is `Duplicate` (`:232-235`).
2. Clock unavailable is accepted at runtime with deadline 0 and no durable
   stop (`:238-245`); deadline overflow is accepted with deadline 0
   (`:247-261`).
3. Normal acceptance sets `_stopAccepted`, records runtime acceptance/deadline,
   then attempts durable `WriteMutation` (`:263-295`).
4. `_stopAccepted` never consumes the capability. Durable StopRecord is the
   journal-level guard (`OwnershipTransitions.cs:27-31`); a failed durable stop
   is not represented durably.

Stop and release share `_gate` only at the runtime decision points; journal
mutation has its own `_journalGate`. Runtime stop acceptance and durable
StopRecord are distinct facts.

### 3.3 Recovery

`OwnershipJournal.TryRecoverCommit` takes the real fence, refuses while a live
writer holds it, and only removes a seal when the sealed slots are decodable
(`OwnershipJournal.cs:712-770`). `ReleaseRecoveryClassifier` maps any non-null
ReleaseRecord to `MayHaveExecuted` (`ReleaseRecoveryClassification.cs:28-38`),
not to a definite delivery. `Reconciler` accepts a caller-supplied
`TimeSpan _stopAllowance` (`Reconciler.cs:124-129`) and passes it directly to
`DomainLease.StopAndAwaitEmpty` (`:231`); it does not read
`StopRecord.StopDeadlineNanoseconds`.

## 4. C1 - R09 ambiguous ReleaseIntent

### 4.1 Finding

R12 collapses a failed `WriteTransition` to definite
`Refused(NotCommitted)` (`ReleaseArbiter.cs:110-114`). The journal can have both
slots durably holding Intent while the seal unlink fails (`OwnershipJournal.cs:
486-489`), so the live decision is not a truthful durable classification.
The same failure path requests no stop. Recovery after writer death can
promote Intent and classify `MayHaveExecuted`.

Evidence: `SOURCE-CONFIRMED`; D2 probe
`AmbiguousSealFailureLeavesUncertainIntentAndNoStop`; D4 `D4-6` fresh-process
recovery; `INDEPENDENTLY REPRODUCED`.

### 4.2 Alternatives

- **C1-A (recommended): explicit uncertain-persistence decision plus bounded
  runtime stop.** Add an explicit release decision/uncertainty value for
  ambiguous journal persistence; never report definite `NotCommitted`; request
  bounded runtime stop; attempt durable stop best-effort; keep/no FD4.
- **C1-B: preserve `Refused` but add an explicit uncertainty classification
  field.** Lower API churn than a new kind, but two fields must be checked;
  easier for consumers to miss.
- **C1-C: retain current semantics with documentation.** Rejected: it would
  state that `NotCommitted` does not mean not committed, directly violating
  R09 and making downstream retries/telemetry unsafe. No code saving justifies
  it.

### 4.3 Recommended C1 contract

- Definite pre-Intent storage failure: `Refused(NotCommitted)`, no FD4,
  capability remains live unless separately stopped.
- Ambiguous post-content seal failure: explicit
  `AmbiguousPersistence` decision/uncertainty; no FD4; `DurableRelease=
  MayHaveExecuted`; capability is invalidated by the C2 stop path; retained
  decision is the ambiguous result, so later calls receive `Duplicate` of it,
  not a new release attempt.
- Runtime stop: request `RequestStop(CallerStop)` after releasing
  `_journalGate`; acceptance is runtime only if durable StopRecord cannot be
  written. Accepted deadline is `now + 5_000_000_000` when the clock is valid;
  zero if unavailable/overflow. No false durable claim.
- The live writer remains the effective fence holder until process exit; it
  must not hand its lease to another process. Recovery after death takes the
  fence afresh and applies seal recovery. The owner/domain stop execution and
  cleanup remain WP08/WP10 integration; D6.0 proposes no new production owner.
- If durable stop also fails in the wedge: runtime stop is accepted, durable
  stop is absent, and recovery sees the sealed Intent; conservative
  `MayHaveExecuted`, no resend. Operator-visible state records the failed
  durable stop.
- Repeated release after ambiguity: retained/duplicate ambiguous result; no
  second intent or FD4.
- The production change is expected in `ReleaseDecisionKind`/`ReleaseDecision`
  and `ReleaseArbiter.RequestRelease`; no OwnershipSnapshot v3 change.
- C1 requires C2's capability invalidation for permanent stop-first closure.
  C1 and C4 share retained-decision semantics.

## 5. C2 - R08 stop-first capability invalidation

### 5.1 Finding

`_stopAccepted` blocks release only inside one arbiter (`ReleaseArbiter.cs:55-61,
125-131`), and a durable StopRecord blocks intent at the journal layer
(`OwnershipTransitions.cs:27-31`). If stop persistence fails or the clock is
unavailable, a second arbiter over the same live capability can plan a release.
D2/D4 reproduce the bounded component/fresh-lease asymmetry; no normal
single-owner exploit was demonstrated. `SOURCE-CONFIRMED`, `INDEPENDENTLY
REPRODUCED` for the diagnostic.

### 5.2 Alternatives

- **C2-A (recommended): consume/invalidate the capability at runtime stop
  acceptance, before/independent of journal persistence.** The capability
  store is the single-use authority; this closes the multi-instance window.
- **C2-B: invalidate only on first post-stop release refusal.** Leaves a
  window before the refusal and makes closure dependent on a later caller.
  Rejected.
- **C2-C: rely only on single-arbiter composition.** Does not satisfy R08's
  permanent-abort requirement; rejected as the primary contract (it remains a
  composition assumption, not a defense).

### 5.3 Recommended C2 contract

- At every runtime stop acceptance path (normal, clock-unavailable, overflow),
  call the capability's one-shot consume/invalidate under `_gate` before
  returning. Journal persistence success is not a precondition.
- `IsLive` becomes false after a winning stop; irreversible and idempotent.
  If release already consumed the capability, stop remains accepted but does
  not reactivate it.
- Stop-first release refusal has no FD4. Stop after capability consumption
  preserves the already-won release semantics; stop after FD4/Committed does
  not rewrite history.
- Failed durable StopRecord, clock failure and overflow never reopen release
  through the same capability. A fresh replacement has no capability; it uses
  the durable journal only.
- Duplicate stop remains idempotent. Multiple arbiter instances sharing the
  same capability cannot both release because the store consume is single-use.
- Production symbols expected: `ReleaseArbiter.RequestStop`,
  `PreparedTransaction.TryConsume` caller path, possibly `StopDecision`
  observability; no schema change.
- C2 must land before C1 so C1's ambiguous stop path uses permanent capability
  invalidation.

## 6. C3 - R27 persisted stop-deadline consumption

### 6.1 Finding

`StopRecord` persists accepted time and absolute deadline (`OwnershipRecord.cs:
119-122`); allowance is exactly 5,000,000,000 ns (`:202`). `Reconciler` uses a
caller-supplied `TimeSpan` (`Reconciler.cs:124-129,231`) and never reads the
persisted deadline. The validator prevents rewriting it, but no production
consumer enforces it. `SOURCE-CONFIRMED`; D2/D4 diagnostic.

### 6.2 Alternatives

- **C3-A (recommended): `Reconciler` derives remaining =
  `StopDeadlineNanoseconds - clock.NowNanoseconds` from the durable record.**
  Caller-supplied allowance is not used when a StopRecord exists.
- **C3-B: caller computes remaining with proven provenance.** Requires a
  validated caller/API and is easier to misuse; no current production caller
  exists.
- **C3-C: new deadline-bound authority object.** Unnecessary; the immutable
  StopRecord already carries the bound.

### 6.3 Recommended C3 contract

- StopRecord present, valid future deadline: remaining window; no reset.
- Deadline equal/reached/expired: perform a bounded urgent stop with zero
  remaining delay (kill plus one immediate population observation); if still
  populated, return Unknown/quarantine, not a new five-second window.
- StopDeadline <= 0, invalid accepted time, unavailable clock or arithmetic
  overflow: no fresh allowance; quarantine/Unknown, preserve retained
  authority, no fabricated destructive authority.
- No StopRecord: retain existing caller allowance for cleanup only; this path
  is not an accepted stop deadline.
- Boot mismatch: existing cross-boot refusal; no authority transfer.
- Repeated recovery uses the same absolute deadline; partial cleanup may
  persist progress but must not reset or extend the deadline.
- Production symbol: `Reconciler`; no schema change. Tests use a controllable
  monotonic clock, not wall-clock sleeps.
- R27/C3 remains unresolved until this decision is approved and implemented.

## 7. C4 - R02/R06 duplicate semantics

### 7.1 Finding

Sequential duplicate calls receive `Duplicate` of the retained winner. A
concurrent caller that loses the Intent race can receive
`Refused(NotCommitted)` while the winner later releases; a third caller then
receives `Duplicate`. Outside-gate refusals are not retained; gate refusals
are (`ReleaseArbiter.cs:48-61,117-131,176-223`). The physical write remains
at-most-once. `SOURCE-CONFIRMED`, `INDEPENDENTLY REPRODUCED` (D4-9; D5-CR3).

### 7.2 Alternatives

- **C4-B (recommended): explicit concurrent-refusal semantics.** A concurrent
  loser may receive `Refused(NotCommitted)` while the winner is in progress;
  after the winner retains a decision, later calls receive `Duplicate` of it.
  Document the retention asymmetry and retry classes. No new waiting state.
- **C4-A: linearizable winner result for all callers.** Requires an
  in-progress decision/wait/notification and introduces caller blocking,
  deadlock and writer-death semantics. Rejected for this bounded API; it is a
  larger future contract.
- **C4-C: new in-progress/completed classification.** A new enum could be
  clearer, but it is not necessary if C4-B's two contracts are explicit; it
  increases API surface. Rejected unless a future consumer needs it.

### 7.3 Recommended C4 contract

- Sequential calls after a retained release decision: `Duplicate` of the
  original decision.
- Concurrent callers during the winner's Intent-to-retained interval:
  `Refused(NotCommitted)` with no second Intent/transport; this is explicitly
  a concurrent-retry outcome, not proof that the winner failed.
- Outside-gate refusals are not retained and may be retried after the
  underlying condition changes. Gate-level refusals (stop, expiry after
  intent, capability, transport) are retained and subsequent calls receive
  `Duplicate` of that refusal.
- A refusal does not imply no prior release occurred; `Duplicate` does not
  prove durable Committed. Retryability, idempotency and at-most-once physical
  RELEASE are distinct.
- C1's ambiguous decision follows the same retention rule: retained Ambiguous;
  later calls receive Duplicate of Ambiguous, no retry.
- No schema change; production symbols `ReleaseArbiter.RequestRelease` and its
  decision documentation/retention paths.

## 8. Combined interaction analysis

| Pair | Decision | Required interaction result |
|---|---|---|
| C1 x C2 | Ambiguous Intent + stop | Stop consumes capability even when durable StopRecord is blocked; no live second authority; no FD4; recovery Intent -> MayHaveExecuted. |
| C1 x C3 | Ambiguous journal, no durable stop | No durable deadline exists; C3 must not grant five seconds. Runtime stop deadline is evidence only; recovery quarantines/conservatively classifies until seal resolution. |
| C1 x C4 | Ambiguous retained result | Retain Ambiguous; concurrent/later callers receive the defined concurrent refusal or Duplicate of Ambiguous, not a fresh release attempt. |
| C2 x C3 | Capability invalidation and deadline | Invalidation happens at runtime acceptance, independent of deadline validity; C3 never reactivates a consumed capability. |
| C2 x C4 | Stop wins during competing release | Stop consumes; release caller receives MayHaveExecuted/refused; later duplicates cannot release. |
| C3 x C4 | Retry/recovery and deadline | Recovery/retry never resets the stored deadline; duplicate handling cannot bypass the remaining window. |

No pair requires a journal schema change. The shared ordering principle is:
runtime winner first under `_gate`, durable evidence best-effort and separately
classified; recovery never promotes runtime state to durable authority.

## 9. Compatibility and evidence consequences

- C1/C2/C4 are runtime/API semantics; no OwnershipSnapshot v3 field addition is
  required. C3 uses existing StopRecord fields and no schema change.
- All R12 product evidence remains historical evidence for the R12 tree only.
  New production commits invalidate automatic transfer of release qualification
  to the new tree.
- D1-D5 fixtures remain reusable test assets, but must be rerun against the new
  production tree after each remediation. Native FD5 evidence is unaffected by
  C1-C4 semantics but must remain separately bound.
- Existing test expectations that assert `NotCommitted` for ambiguous failure,
  `IsLive == true` after stop, caller-reset allowance, or Refused/duplicate
  behavior must be updated only under separately authorized remediation.
- C1/C2/C3/C4 changes require independent review, human authorization and
  post-change requalification; green old fixtures do not qualify new code.

## 10. Recommended remediation order

1. **C2** capability invalidation at stop acceptance (small, closes the
   single-use authority window; prerequisite for C1 stop behavior).
2. **C1** ambiguous-release decision and bounded runtime stop (depends on C2).
3. **C4** explicit concurrent-refusal/retention contract and API comments
   (coordinate with C1's retained Ambiguous result).
4. **C3** persisted-deadline consumption in `Reconciler` (independent after
   C2/C1 semantics are fixed).
5. Re-run D1-D5 fixtures, native FD5, and all affected regressions; preserve
   new evidence bound to the new production tree.
6. Independent D6 remediation review and human acceptance decisions.

Each tranche must be test-first, separately reviewed, separately evidenced and
separately authorized. No tranche may mark G3-A/B/D/E or WP05 release accepted.

## 11. Gate mapping

| Decision | Requirement | Gates blocked |
|---|---|---|
| C1 | R09 | G3-A11, G3-B08 |
| C2 | R08 | G3-A02, G3-A07 |
| C3 | R27 | G3-A10 and R27 acceptance |
| C4 | R02/R06 | G3-A05 |

These are proposed pre-G3 diagnostic sub-gates; authoritative G2a/G2b/G3
remain unchanged. No gate is accepted by this document.

## 12. Remaining questions and approval questions

- C1: Should ambiguous persistence be a new decision kind or a dedicated
  uncertainty field? Recommendation: new explicit kind; approve/reject.
- C1: Is best-effort runtime stop without durable StopRecord acceptable when
  the journal is wedged, with recovery/operator subsequently authoritative?
- C2: Should capability invalidation be at runtime stop acceptance for all
  stop paths (recommended) or at first post-stop refusal?
- C3: For an expired/zero/unverifiable deadline, is quarantine/Unknown after a
  zero-wait urgent stop acceptable, or should the contract require a different
  bounded urgent action?
- C4: Accept the explicit concurrent-refusal contract and documented
  retention asymmetry, or require a future linearizable API?
- Human authorization is required for each C item and each remediation
  tranche. GLM review does not approve production changes.

## 13. H1-H5 handoff

See `gagamba-linux-m1-wp05-d6-0-deferred-integration-and-handoff.md`.
H1 D5-O05 remains a production integration gap; H2 destructive descriptor
experiments are unclaimed; H3 raw termination witness is optional; H4 is this
C1-C4 package; H5 preserves the D5 plan ordering note. WP08/WP10, production
post-release status and full G3 remain deferred.

## 14. Status

**PROPOSED - NOT APPROVED.** No production remediation is authorized. No
C1-C4 item is resolved. No G3/WP05 release gate is accepted.

## D6.1 revision - approval-ready proposed design

Status: **PROPOSED - NOT APPROVED**. C3 expired-deadline policy:
**PENDING HUMAN**. This revision folds GLM Q3 amendments into the proposal; it
does not implement or approve them.

- **C1:** use `ReleaseDecisionKind.AmbiguousPersistence`, a typed
  journal-ambiguity result, retained under `_gate` after `_journalGate` is
  released, then invoke `RequestStop` as a tail action with no gate held across
  journal I/O. C2 is an implementation prerequisite. Concurrent opposing-order
  test required.
- **C2:** consume at first runtime stop acceptance inside `_gate` on normal,
  clock-unavailable and overflow paths; no re-consume on duplicate; preserve
  release-first/Consumed history; CE-03 declaration verbatim.
- **C3:** QPC/transaction-clock epoch corroboration; reject the incompatible
  `Stopwatch.StartNew()` epoch; expired => policy-gated kill+one observation;
  invalid/unavailable => quarantine/Unknown; no-record path is cleanup-only;
  already-empty Option B (kill-first, no unnecessary WAIT); Reconciler
  aggregation must distinguish attempted/unconfirmed from operation failure.
  No schema change. C3 remains `CONTRACT CHOICE REQUIRED - HUMAN DECISION
  PENDING`.
- **C4:** retain the explicit concurrent-refusal contract and exact R12 reason
  taxonomy; C1 ambiguous result retained; no waiting state or new
  `ConcurrentRefusal` kind.
- **A-PKG-1:** original C1-C9 blob `73eb09af...` is reachable through
  `review/wp05-release-plan-adversarial-review` at `e4b0a798...` and is not an
  ancestor of D6.0.
- **A-PKG-2:** `ReleaseArbiter` and `Reconciler` are uncomposed components with
  zero production call sites at R12; changes affect component code, not an
  integrated owner runtime. WP08/10 composition remains deferred.
- **A-PKG-3:** add `AmbiguousWedgeThenClockUnavailableStop` as a composed
  acceptance case.
- **A-PKG-4:** already satisfied; no change.

The four decision records, evidence/gate matrix and remediation proposal carry
the corresponding detailed revisions. No contract is approved; no production
remediation is authorized; H1-H5 and full G3 remain deferred.
