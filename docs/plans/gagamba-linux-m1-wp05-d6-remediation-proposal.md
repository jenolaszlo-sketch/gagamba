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

## D6.1 revision - amendment prerequisites and requalification

Status: **PROPOSED - NOT AUTHORIZED**

The staged proposal remains C2 -> C1 -> C4 -> C3; C3 may be scheduled earlier
because its durable deadline logic is largely independent, but every tranche
still requires its own approved contract revision, tests-first change,
independent review and human authorization.

### Updated prerequisites

- C2 must define consumption at first stop acceptance inside `_gate` on all
  three paths and preserve release-first/Consumed history.
- C1 depends on C2; use `AmbiguousPersistence`, lock-safe tail stop, retained
  ambiguity and no journal I/O under `_gate`.
- C4 uses the exact C1 ambiguous vocabulary and R12 reason taxonomy.
- C3 must establish QPC/transaction-clock epoch corroboration, reject the
  incompatible `OwnerDomainClock` epoch, correct Reconciler Failed/Unknown
  aggregation, and resolve the already-empty row with Option B (kill-first,
  no unnecessary WAIT) while keeping the zero-wait policy PENDING HUMAN.
- No schema change is proposed. `ReleaseArbiter` and `Reconciler` are
  uncomposed components at R12; no integrated owner runtime is claimed.

### Future tranche gates

For each tranche: approved contract revision first; deterministic unit,
concurrency, fault and recovery tests; exact source identity; native/protocol
regressions; independent GLM review; human acceptance; new source-bound
evidence; rollback on any second write, no-resend weakening, false definite
NotCommitted, live capability after stop, deadline reset, fabricated evidence,
unjustified schema change or compatibility break.

Future reruns must include affected D1-D3 probes, D2 clock/ambiguity/duplicate
cases (with truth-inverting C2 assertions where the old contract asserted live
capability), D4 supervisor/crash/no-resend including D4_6/D4_9, D5 CR-3/CR-3b,
D5 native FD5, formal release-path rows and native/protocol regressions. Old
R12 evidence is historical only; no automatic qualification transfer.

C3 policy and all C1-C4 approvals remain pending human decisions. H1-H5,
WP08/WP10, production status and full G3 remain outside this proposal.

## D6.2 revision - precision prerequisites

Status: **PROPOSED - NOT AUTHORIZED**

D6.2 adds four precision prerequisites. It does not authorize any tranche,
select the C3 policy, approve a contract or accept a gate. The staged order
C2 -> C1 -> C4 -> C3 remains a proposal; C3 may be scheduled earlier only by a
separate human authorization.

### R1 - structured reconciliation outcomes (C3)

Before C3 implementation, the design requires the typed internal
`StopOutcome` mechanism in C3 §D6.2-R1 (`KillDispatchOutcome`,
`StopPopulationObservation`, `TimedOut`), a complete component-to-aggregate
truth table, and an explicit `JournalFinalization` row. The implementation
must not classify by error strings, must not blanket-map failures to Unknown,
and must preserve a component `Failed` when finalization is also uncertain.
Required tests are R1-T1..T12 in C3 §D6.2-R1 and the evidence matrix. No
production `DomainLease`/`Reconciler` change is authorized yet.

### R2 - trusted clock provenance (C3)

Before C3 implementation, the design requires the sealed internal production
domain clock backed by `Stopwatch.GetTimestamp()` and the exact `OwnerClock`
conversion, a constant `MonotonicClockBasis(MonotonicEpoch.Qpc,
Stopwatch.Frequency)`, and Reconciler rejection of incompatible epoch or
frequency before subtraction. Required tests are R2-A..F in C3 §D6.2-R2. The
construction-relative `OwnerDomainClock` remains incompatible and dead code at
R12; no active production failure is claimed. No clock implementation is
authorized yet. The C3 expired-deadline policy remains
`C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor B is selected.

### R3 - retention collision (C1, with C2/C4 cross-reference)

Before C1 implementation, the design requires the first-retainer ordering in
C1 §D6.2-R3: stop-first acceptance, ambiguity-first retention, and an already
retained decision. The stop-won refusal is retained as
`Refused(MayHaveExecuted, NotAttempted, DurableCommitted=false)` and never
asserts absent durable Intent; later callers receive `Duplicate`. The journal
remains the separate durable evidence. Required tests are R3-T1..T8. C2
consumption and C4 retention semantics are unchanged; no new waiting protocol
or second retained-decision slot is introduced.

### R4 - fixture provenance (C1/evidence)

Before any requalification that cites the diagnostic fixture, the inventory
must use the exact fixture branch/commit/file/blob/digest and distinguish it
from R12 production. The fixture branch
`implementation/wp05-release-qualification-fixtures` at test commit
`dc4c372b180abef1b27038d94f49af90226fa548` is test-only; its `src` tree is
identical to R12 and its `native` diff is empty. It is not copied into R12,
merged or rerun by D6.2.

### D6.2 prerequisite outcome

Each tranche still requires its own approved contract revision, tests-first
implementation, independent review, human authorization and new source-bound
evidence. No tranche may inherit R12 qualification. C3 policy and all C1-C4
approvals remain pending; no gate is accepted.
