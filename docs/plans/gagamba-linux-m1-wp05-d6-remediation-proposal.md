# WP05 D6.0 Remediation Proposal

Status: **PROPOSED - NOT AUTHORIZED**

This is a documentation-only implementation proposal for C1-C4. It does not
authorize production changes, alter C1-C4 decisions, accept a gate or claim
release qualification. Each tranche requires human approval, test-first work,
independent review and new evidence.

## 1. Sequence and boundaries

Recommended independent tranches:

1. **R-C2**: stop acceptance permanently consumes the capability.
2. **R-C1**: ambiguous Intent produces an explicit uncertain result and bounded
   runtime stop, retaining the result; uses R-C2.
3. **R-C4**: explicit concurrent-refusal/retention semantics and API
   documentation, including the retained C1 ambiguous result.
4. **R-C3**: `Reconciler` consumes the persisted stop deadline.
5. Post-remediation D1-D5/fixture/native regression and new evidence.

C2 first closes the authority window used by C1. C3 is independent of C1/C2
semantics but should follow them so the recovery evidence binds one coherent
tree. C4 must be aligned with C1's new retained result.

Each tranche must be separately committed, reviewed and preserved. No tranche
may be merged into an accepted release qualification automatically.

## 2. R-C2 - stop capability invalidation

Production scope: `Execution/ReleaseArbiter.cs` `RequestStop`; use
`PreparedTransaction.TryConsume`; optional `StopDecision` observability.
No schema.

Test-first acceptance:

- stop-first: `IsLive == false`, no release/transport;
- failed durable stop: `IsLive == false`;
- clock-unavailable/overflow: `IsLive == false`;
- second arbiter over same capability cannot release;
- duplicate stop idempotent;
- release-first preserves consumed/Committed state;
- fresh replacement no inherited capability.

Review requirement: independent GLM review of capability lifecycle and
compatibility with C1 stop path.

## 3. R-C1 - ambiguous persistence

Production scope: `ReleaseArbiter.RequestRelease`, `ReleaseDecisionKind`/
`ReleaseDecision`; stop request path. No schema; no new journal.

Test-first acceptance:

- definite pre-intent failure unchanged;
- ambiguous post-content seal failure returns explicit uncertain result;
- no FD4, no false NotCommitted;
- bounded runtime stop requested; durable stop best-effort;
- retained uncertain result; later call Duplicate;
- owner SIGKILL; fresh lease; seal recovery Intent -> MayHaveExecuted;
- no second transport/coordinator acceptance;
- bounded cleanup; no target.

Review requirement: independent review of decision semantics, stop authority,
retention, and unchanged no-resend/single-writer invariants. If the explicit
decision type is a new enum value, verify all switch/consumer exhaustiveness.

## 4. R-C4 - duplicate semantics

Production scope: `ReleaseArbiter.RequestRelease` retained-decision paths and
decision documentation; no schema; no waiting state under the recommended
option.

Test-first acceptance:

- sequential Duplicate; one write;
- deterministic concurrent A/B/C winner Refused/Duplicate shape;
- outside-gate refusal not retained; gate refusal retained;
- different identities refused;
- definitive/ambiguous intent failure classes;
- intent already present;
- stop-winning race; transport failure; writer death/recovery;
- no second Intent/FD4; no resend.

Review requirement: independent review of API contract and retention
asymmetry; reject any proposal that introduces deadlock, unbounded wait or
second physical release.

## 5. R-C3 - stop deadline consumption

Production scope: `Recovery/Reconciler.cs`; existing `StopRecord` only; no
schema. A caller-supplied allowance may be retained only for the no-StopRecord
cleanup path; a durable StopRecord overrides it.

Test-first acceptance:

- within-window uses remaining;
- late takeover uses less, no extension;
- expired uses zero-wait urgent stop, then Unknown if populated;
- zero/invalid/clock-unavailable/overflow classified conservatively, no fresh
  five seconds;
- boot mismatch refused;
- repeated reconciliation no reset;
- partial cleanup no reset;
- StopRecord bytes unchanged;
- identity/cleanup authority preserved.

Use a deterministic monotonic clock; no wall-clock-only oracle. Review R27
policy and destructive-stop boundary independently.

## 6. Regression and requalification

For every tranche:

- run focused unit/concurrency/fault tests on the exact new tree;
- rerun affected D1-D3 probes, D4 supervisor/crash/no-resend, D5 native FD5 and
  formal release-path rows as applicable;
- rebuild native artifacts and record exact hashes; no silent artifact reuse;
- preserve raw logs, source/environment identities, cleanup and a SHA-256
  manifest;
- run a fresh independent review; separate human approval for each contract;
- do not transfer old R12 qualification to the new tree.

## 7. Rollback/rejection criteria

Reject or roll back a tranche if it:

- creates a second physical RELEASE or second writer;
- weakens no-resend or durable intent ordering;
- reintroduces a false definite NotCommitted for ambiguity;
- leaves a capability live after winning stop;
- resets/extends a persisted stop deadline;
- fabricates durable stop/commit/observation evidence;
- requires an unjustified schema change;
- breaks existing statically valid records or recovery compatibility without a
  separately approved migration;
- cannot produce deterministic tests for the selected contract.

## 8. Evidence preservation

Each tranche needs its own source-bound evidence and independent review before
D6 combined acceptance. C1-C4 decisions themselves must be recorded by their
human holders; GLM review supports coherence, not approval. H1-H5 remain
separate obligations. No production post-release FD5/WP10 wiring may ride on
this proposal.
