# WP05 D6.1 Human Approval Questions

Status: **PROPOSED - NOT APPROVED**

All answers are **PENDING HUMAN**. The human WP05 release/security decision
holder must answer each item separately. GLM review is advisory; it does not
approve.

## C1 - Ambiguous ReleaseIntent

**Question:** Approve the explicit `AmbiguousPersistence` decision, retained
uncertainty, lock-safe tail-of-call runtime stop and C2-based capability
closure?

- Recommended: **Yes**, with A-C1-1..3 design text.
- Alternatives: separate uncertainty field; retain false-NotCommitted semantics
  (rejected).
- Consequences: truthful ambiguity, no FD4, runtime stop best-effort, recovery
  `MayHaveExecuted`, no resend. C2 is a prerequisite.
- Open prerequisites: A-C1 tests; independent re-review; component-only claim.
- Status: **PENDING HUMAN**

## C2 - Stop-first capability invalidation

**Question:** Approve irreversible single-use capability consumption when stop
first wins, including clock-unavailable and overflow paths, independent of
StopRecord persistence?

- Recommended: **Yes**.
- Alternatives: invalidate only at first post-stop refusal (rejected);
  single-arbiter-only documentation (rejected as primary defense).
- Consequences: `IsLive == false` after stop; failed durable stop cannot reopen
  release; release-first history preserved; CE-03 window closed by capability.
- Open prerequisites: A-C2 tests; independent re-review; human acceptance.
- Status: **PENDING HUMAN**

## C3 policy - Expired deadline

**Question:** Does an expired durable stop deadline authorize the existing
kill-plus-one-immediate-observation behavior as the accepted urgent-stop
contract?

- Recommended: **Policy A** (kill + one immediate population observation; no
  fresh waiting allowance), subject to human decision.
- Alternative: Policy B, a separately specified bounded cleanup action with
  independent authority and bound.
- Consequences: Policy A is mechanically supported and minimal; Policy B needs
  a new explicit bound/authority. Neither may reset the stored deadline.
- Status: **PENDING HUMAN**; no policy selected.

## C3 contract - Deadline derivation

**Question:** Subject to the policy choice, approve epoch-corroborated
deadline consumption, fail-closed clock uncertainty and corrected Reconciler
classification?

- Recommended: **Yes**, with A-C3-1..4 design text.
- Requirements: QPC/transaction-clock epoch; reject `Stopwatch.StartNew()`
  relative epoch; remaining = max(0, deadline - now); invalid/unverifiable =>
  quarantine/Unknown; no-record path is cleanup-only; no schema change.
- Consequences: no restart reset; unconfirmed stop maps conservatively, not
  automatically Failed; component change only until WP08 composition.
- Status: **PENDING HUMAN** (contract + policy)

## C4 - Concurrent duplicate semantics

**Question:** Approve the documented concurrent-refusal contract, sequential
Duplicate semantics and retention asymmetry?

- Recommended: **Yes** (C4-B), with A-C4-1 discipline.
- Alternatives: linearizable wait/notification API (rejected); new
  in-progress kind (unnecessary).
- Consequences: concurrent loser may receive `Refused(NotCommitted)`; later
  callers receive Duplicate; no waiting state; one physical write; Refused
  does not prove no prior release, Duplicate does not prove Committed.
- Status: **PENDING HUMAN**

## Implementation authorization

**Question:** Should any specific D6 remediation tranche be authorized after
contract approval and focused independent review?

- Recommended: **No blanket authorization**; approve one tranche at a time,
  test-first, with its own source/evidence binding and review.
- Order: C2 -> C1 -> C4 -> C3 (C3 may be earlier; independent).
- Every tranche requires its own human approval, independent GLM review,
  new qualification evidence and rollback criteria.
- Status: **PENDING HUMAN**

## Summary

| Item | Status |
|---|---|
| C1 contract | PENDING HUMAN |
| C2 contract | PENDING HUMAN |
| C3 expired-deadline policy | PENDING HUMAN |
| C3 contract | PENDING HUMAN |
| C4 contract | PENDING HUMAN |
| Implementation authorization | PENDING HUMAN |

No answer in this document is an approval.
