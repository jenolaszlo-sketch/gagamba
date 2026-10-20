# WP05 D6.1 Focused Independent Review Handoff

Status: **PROPOSED - NOT APPROVED**

To: independent GLM design reviewer.
From: D6.1 design author.
Purpose: focused re-review of the amended C1-C4 proposed contracts, not a
repeat of D1-D5 qualification.

## 1. Binding and revisions

- Frozen R12: `38f6981...`, tree `6854675d...`.
- D5 preservation: `578b2084...`.
- D6.0 design HEAD: `23df1c566e7d8f072d924f374bf6891c726c03fe`.
- D6.0 review Q3: `2a235c391673258023d3b3058ad7bbb31cc20406`.
- D6.0 preservation/base: `947aebe7df2eefd116cfba8a4b3e1baee2067e0d`.
- D6.1 branch: `design/wp05-d6-1-contract-amendments`.
- D6.1 exact design HEAD: filled by the final D6.1 commit (not self-approved).

The D6.1 branch modifies only design documents. The D6.0 originals remain
recoverable at `23df1c5...`; GLM Q1-Q3 remain unchanged on their branch.

## 2. Amendment closure to re-check

- A-C1-1: tail-of-call lock-safe stop sequence and opposing-order probe.
- A-C1-2: `AmbiguousPersistence` vocabulary, retained result, consumer
  exhaustiveness.
- A-C1-3: C2 reliance, transient wedge window, death/no-resend behavior.
- A-C2-1: consume inside `_gate` on all three acceptance paths; no re-consume.
- A-C2-2: release-first/Consumed/`ReleaseAlreadyCommitted` semantics.
- A-C2-3: exact CE-03 out-of-model declaration.
- A-C3-1: QPC/transaction-clock epoch corroboration; replacement/constraint
  of `OwnerDomainClock`; incompatible-clock rejection.
- A-C3-2: attempted-but-unconfirmed stop -> Unknown/quarantine; operation
  failure remains Failed; aggregation amendment.
- A-C3-3: no-StopRecord cleanup-only allowance; no manufactured deadline.
- A-C3-4: Option B kill-first/no-unnecessary-WAIT resolution.
- C3 policy: Policy A recommended, human choice pending, not selected.
- A-C4-1: exact reason-string taxonomy and semantic gate vs lock boundary.
- A-PKG-1: publisher branch/commit/blob and non-ancestry.
- A-PKG-2: zero production callers, component-only scope.
- A-PKG-3: composed ambiguous-wedge/clock-unavailable-stop case.
- A-PKG-4: unchanged satisfied declaration.

## 3. Source symbols requiring re-check

`ReleaseArbiter.RequestRelease/RequestStop`, `ReleaseDecisionKind` and
`ReleaseDecision`, `PreparedTransaction.TryConsume/IsLive`,
`OwnershipJournal.Write/WriteTransition/WriteMutation/TryRecoverCommit/Resolve`,
`OwnershipTransitions.TryPlanReleaseIntent/TryPlanStop`,
`ReleaseRecoveryClassifier`, `Reconciler` aggregation and
`DomainLease.StopAndAwaitEmpty`, `OwnerClock`, `OwnerDomainClock`,
`LaunchDomainClock`, and refusal reason strings at the cited R12 lines.

## 4. Questions for focused review

1. Does the C1 ambiguous sequence preserve the no-journal-I/O-under-`_gate`
   invariant and avoid lock inversion/deadlock?
2. Is `AmbiguousPersistence` consistently used by C1, C4, matrix and
   remediation, with every consumer accounted for?
3. Does C2 consumption-on-stop close the window without corrupting release-first
   history or treating `ReleaseAlreadyCommitted` as delivery proof?
4. Does C3 now reject the incompatible `Stopwatch.StartNew()` epoch and define
   a source-corroborated epoch correctly?
5. Does C3 distinguish operation failure, attempted-but-unconfirmed stop,
   population unknown, empty, persistence failure and identity refusal without
   collapsing them?
6. Is the kill-first already-empty resolution coherent with the existing
   Reconciler sequence?
7. Is the expired-deadline policy still explicitly open and not smuggled into
   the contract?
8. Are the C4 reason strings and retention taxonomy exact and consistent with
   the C1 ambiguous result?
9. Do all six interactions remain coherent with no new durable authority?
10. Did D6.1 accidentally claim implementation, approval, target execution or
    gate acceptance?

## 5. Unchanged boundaries

- No production/test/native/artifact/harness change.
- No new qualification run.
- C1-C4 not approved; C3 policy pending; H1-H5 deferred; WP08/WP10/full G3
  deferred.
- Component-level only; no integrated owner lifecycle claim.
- Release remains not qualified.

## 6. Requested review output

A focused amendment verdict per A-ID, any new contradiction, whether the
package is approval-ready, and the exact remaining human questions. Do not
approve contracts on the human holder's behalf.
