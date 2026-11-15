# Required Changes for the WP05 Release Qualification Plan (Reviewer Bindings)

Author: GLM (independent adversarial plan reviewer)
Date: 2026-10-19
Disposition source: `docs/reviews/gagamba-linux-m1-wp05-release-plan-independent-adversarial-review.md` (`PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES`). This document converts the review's findings into numbered, binding changes. It does not modify the plan documents in place; the fixture author applies these as decisions to the queue plus targeted plan/test clarifications in the next slice.

Each change lists: what must change, the binding rows/decision, the owner, and the minimal condition under which the change is satisfied for gate-acceptance purposes. None of the changes authorizes production code changes by itself (plan §10 remains binding).

## Production-symbol decisions (block gate-acceptance tagging)

### C1 - R09 ambiguous-intent contract and stop mechanics under a wedged journal (binds Q06; blocks G3-A11, G3-B08)

- Required: record the R09 remediation decision: (a) distinguish definite failures from ambiguous seal-release failures at the arbiter boundary; (b) in the ambiguous case, report a state other than definite `NotCommitted` and request a bounded stop; (c) define the stop-request mechanics for the wedge case documented in CE-02 - runtime stop acceptance is the channel available while the journal is uncertain and un-persistable (the durable `WriteMutation` cannot proceed, and recovery is refused while the live lease holds the fence), so the decision must state where the bounded stop's authority and bound come from (runtime boundary + deadline recorded at runtime; durable evidence best-effort; orphan cleanup remains the recovery backstop).
- Acceptance condition: decision recorded; ambiguity-vs-definite fault wrappers exist (test-only); a probe reproduces CE-02's shape; the wedge behavior (no false NotCommitted, no FD4, eventual MayHaveExecuted on recovery) is observed.
- Severity note from verified mitigations: the physical release path stays blocked throughout; the injury is evidential/contractual.

### C2 - R08 stop-first capability invalidation and the multi-instance window (binds Q05; blocks G3-A02, G3-A07)

- Required: record the R08 decision: consume/invalidate the capability at runtime stop acceptance (or at first post-stop refusal, explicitly chosen), add the test asserting `IsLive == false` after stop-first, and declare by decision that the CE-03 multi-instance window (requires two arbiter instances sharing one journal+capability plus a stop with failed or clock-unavailable durable evidence) is outside the accepted single-arbiter owner model and is closed by the capability contract, not by composition.
- Acceptance condition: decision recorded; test exists; remediation merged separately thereafter if the decision requires code changes.
- Severity note: no runnable single-instance path demonstrates release-after-won-stop; the journal intent-after-durable-stop guard (`OwnershipTransitions.cs:27-31`) plus `_stopAccepted` already block the accepted composition; the requirement breach is defense-in-depth.

### C3 - R27 persisted stop-deadline consumption (binds Q04; blocks G3-A10 and any R27 pass)

- Required: record the R27 remediation decision: `Reconciler` (or a production caller path) must derive the stop allowance from `StopRecord.StopDeadlineNanoseconds` (remaining = deadline - now) or receive a caller-supplied value that is provably computed from the record; add tests: within-window takeover uses the remaining allowance; expired deadline performs the bounded urgent stop; no path can reset or extend the deadline (the record stays immutable per the validator).
- Acceptance condition: decision recorded; signature/test plan fixed; G3-A10 may run as a diagnostic probe only, explicitly pending remediation.
- Severity note: validator already forbids rewriting the deadline; the gap is that no consumer converts the persisted bound into the actually-used allowance.

### C4 - R02/R06 duplicate semantics and the two idempotency contracts (binds Q07; blocks G3-A05)

- Required: decide between (a) installing an in-progress retained decision so concurrent losers receive the winner's outcome (or Duplicate of it), or (b) narrowing the documented API to permit concurrent losers' `Refused` while the sequential contract stays Duplicate; document CE-01's retention asymmetry (outside-gate refusals are not retained; gate refusals are) as deliberate contract, with the retry expectation after each failure class.
- Acceptance condition: decision recorded; a deterministic concurrent test reproduces CE-04's three-caller outcome (or the chosen replacement semantics); sequential and concurrent contracts documented separately in the plan text.
- Severity note: no second-write hazard exists (journal baseline/transition guards + one-shot transport verified).

## Fixture/test clarifications (authorize the next slice; do not block diagnostics)

### C5 - Extend the G3-A matrix with stop-evidence edge cases (R03/R26 support)

- Add: stop accepted with unavailable monotonic clock (`ReleaseArbiter.cs:238-243` records deadline 0) and stop accepted with deadline overflow (`254-261`); assert: decision accepted at runtime, no false durable claim, release still refused, and the deadline-zero record's downstream implications documented for recovery callers.
- Acceptance condition: matrix rows/decision entries exist; covered by selection fixtures (fault wrapper around `_clock`).

### C6 - Fault wrappers must be able to produce the exact ambiguity shape (R09 support)

- Specify: seal-release (unlink) failure injection distinct from definite storage failure, per CE-02, including a plumbing point that fails only in the post-content phase so the both-slots-durable precondition holds.
- Acceptance condition: wrapper spec added to §4.1; used by G3-A11/G3-B08.

### C7 - G3-C orchestration wording: no cross-process lease inheritance

- Replace "the retained recovery scope/lease is used by a fresh replacement process" with lease acquisition semantics matching the verified protocol: the dead owner's fence is released by process death; the replacement must `TryAcquireWriter` afresh and seed its baseline from `Resolve`; no capability/writer is supplied to the replacement.
- Acceptance condition: wording corrected; orchestration demonstrates fresh acquisition.

### C8 - Linearize the review dependency in §2 prerequisites

- State: A1/A2/A3 may proceed to fixture-authorization work only with this review's disposition recorded; A3's planned labels G3-A..G3-E are pre-G3 sub-gates and cannot be tagged accepted while C1-C4 are unresolved (plan §4.2 already implies this; make it explicit in §14 expected sequence).

### C9 - Status-bridge spec bound (R15-R18 support)

- Specify: the status bridge must preserve exact frame identity (magic/version/kind/length/sequence/execution/preparation/digest) and plan order, must not synthesize observations from stdout/stderr/wrapper artifacts, and must gate persistence on `MarkDurableCommitted` (verified behavior at `ReleaseObservationBuffer.cs:18-24,71-80`); G3-D status-observation evidence must show the reader/buffer rejecting wrong-identity/replayed frames in the held execution's actual descriptor topology.
- Acceptance condition: bridge spec text matches these bounds; G3-D negative cases run against the qualification coordinator, never the production coordinator (which emits no post-release status).

## Binding summary

- C1-C4 are production-symbol decisions; until each is recorded as resolved, the corresponding gates may run diagnostics but may not be tagged accepted.
- C5-C9 are fixture/test clarifications that the fixture-author slice may implement immediately under the plan's test-only constraint.
- This document authorizes no production change and establishes no release qualification; the WP05/WP08/WP10 boundaries, the full G2b/G3 closure path, and the frozen harness disposition are unchanged.
