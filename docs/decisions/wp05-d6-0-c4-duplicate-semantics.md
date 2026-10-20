# WP05 D6.0 C4 - R02/R06 Duplicate and Concurrent-Release Semantics

Status: **PROPOSED - NOT APPROVED**

Decision owner: human WP05 release/security decision holder after GLM design
review. This record proposes API semantics; it does not authorize code.

## 1. Original requirement

C4 (required-changes blob `73eb09af...`): decide between a retained
in-progress/winner outcome for concurrent duplicates and an explicitly
documented concurrent-refusal contract; preserve the sequential Duplicate
semantics; document outside-gate versus gate refusal retention and retry
expectations. Requirement R02/R06; gate G3-A05.

## 2. Current R12 behavior (`SOURCE-CONFIRMED`)

- Entry duplicate check returns retained decision relabeled `Duplicate`
  (`Execution/ReleaseArbiter.cs:48-55`).
- A concurrent caller that passes the entry check can lose the Intent race and
  receive `Refused(NotCommitted)` (`:81-115`); the winner later releases and
  retains its decision (`:176-223`); a later caller receives `Duplicate`.
- Outside-gate refusals (`:64-61` entry stop, prechecks, intent write/planning)
  are not retained; gate refusals (stop-after-intent, expiry, capability,
  transport committed) are retained.
- `HeldExecution.TryWriteRelease` is one-shot (`Launch/HeldExecution.cs:45-64`);
  journal Intent planning refuses a second intent
  (`OwnershipTransitions.cs:21-25`).
- D2 `ConcurrentDuplicateReleaseProducesOnePhysicalWrite`, D4-9, D5-CR3
  reproduce the three-caller/concurrent shape with one FD4.

## 3. Candidates

| Option | Contract | Assessment |
|---|---|---|
| C4-A | Linearizable: concurrent losers wait/receive winner result. | Strong API consistency but adds wait/notification/crash semantics and deadlock/latency risk; too large for C4. |
| C4-B | Explicit concurrent refusal: loser Refused while winner in progress; later Duplicate. | Recommended: matches observed behavior, no wait, preserves one-write safety; requires precise documentation. |
| C4-C | New in-progress/completed classification. | Could improve clarity but adds API state solely for aesthetics; reject unless a future consumer requires it. |

## 4. Recommended C4 contract

1. Sequential duplicate after the winner has retained a decision: `Duplicate`
   of the original decision.
2. Concurrent caller during the winner's Intent-to-retained interval:
   `Refused(NotCommitted)` with no second Intent/FD4/transport. This is a
   concurrent-retry result, not proof the winner failed.
3. A later caller after retention receives `Duplicate` of the retained winner
   or refusal. No new release attempt.
4. Outside-gate refusal (prerequisite/identity/deadline/capability/no
   acknowledged snapshot/writer not live) is not retained; the caller may
   retry after the condition changes. Intent-planning refusal due an existing
   Intent is a structural refusal; it must not create a second intent.
5. Gate-level refusal (stop-after-intent, gate expiry/prerequisite/capability,
   capability consume failure, transport/committed path refusal) is retained;
   later calls receive `Duplicate` of that refusal.
6. `Duplicate` does not prove durable Committed; `Refused` does not prove no
   prior release occurred. Retryability, idempotency and at-most-once physical
   RELEASE are distinct. The physical invariant remains one write.
7. Ambiguous C1 result is retained as the ambiguous decision; later callers
   receive Duplicate of it and cannot retry.
8. Stop-winning concurrent release: stop consumes capability (C2); the release
   caller gets the gate refusal/MayHaveExecuted outcome; no FD4; later calls
   follow retention.
9. Failed prerequisites and definitive Intent persistence failure remain
   non-retained refusals; their retry rules are documented separately.
10. No new waiting/blocking state and no schema change. Production symbols
    expected: `ReleaseArbiter.RequestRelease` retention/decision paths and
    `ReleaseDecision` documentation; no new public state if the recommended
    minimal form is selected.

The contract explicitly declares CE-01's retention asymmetry deliberate:
outside-gate refusals are retryable; inside-gate refusals are retained.
`Refused` is not a durable classification; `Duplicate` is not delivery proof.

## 5. Rejected alternatives

- C4-A: adds blocking/synchronization and writer-death/waiter-release
  semantics; unjustified for bounded WP05.
- C4-C: extra state without a concrete consumer; rejected.
- Retaining every outside-gate refusal: would change retry semantics and
  require a new decision state; not recommended.
- Treating Refused as proof of no release: rejected; no-resend is at-most-once,
  not exactly-once.

## 6. Acceptance tests

1. Sequential duplicate returns Duplicate; one FD4.
2. Deterministic concurrent A/B/C: winner Released, concurrent loser Refused,
   later caller Duplicate; one Intent, one physical write.
3. Different identities refused before shared release.
4. Failed prerequisites refusal not retained; retry after condition.
5. Definitive Intent failure refused; no second write.
6. Ambiguous C1 result retained; Duplicate of ambiguous; no retry.
7. Intent-already-present refusal; no second intent.
8. Winner blocked before consume; deterministic concurrent result.
9. Winner after consume/write/committed; retention remains consistent.
10. Writer death recovery: no resend; replacement sees durable facts.
11. Stop-winning race: capability consumed; no FD4; release decision
    conservative.
12. Transport failure: one attempt, conservative result, no retry.
13. Duplicate after recovery does not reissue.
14. Retention asymmetry: outside-gate vs gate refusal documented and tested.

Use D2 `after-intent` barrier and D4/D5 process/lease fixtures; no sleeps as
sole concurrency control.

## 7. Compatibility, risks and open questions

- No schema change. `ReleaseDecisionKind` behavior/retention changes require
  tests and caller documentation.
- Risk: consumers may assume every Refused is definitely NotCommitted or every
  Duplicate is Committed; tests/docs must forbid both.
- Open: whether to add a small `ConcurrentRefusal` decision kind or keep
  `Refused` + reason/retention metadata. Recommendation: keep `Refused` and
  document; C1 may add the explicit ambiguous kind.
- Open: exact name of the retained decision for C1; C4 accepts an explicit
  ambiguous retained value.
- GLM review required; approval: **NOT APPROVED**.

## D6.1 revision - A-C4-1

Status: **PROPOSED - NOT APPROVED**
Verdict context: **DESIGN SUPPORTED FOR HUMAN DECISION - APPROVAL PENDING**

### Exact refusal taxonomy (A-C4-1)

The following R12 reason strings are the proposed discoverable taxonomy. They
must be retained as the implementation's class identifiers/metadata, subject to
normal source review; no new `ConcurrentRefusal` kind is proposed.

| Class | R12 reason (exact where quoted) | Semantic boundary |
|---|---|---|
| Stop already won before intent | `release request refused: stop already won before durable intent` | runtime gate entry, retained |
| Initial precondition/identity/deadline/prerequisite/capability | `release request refused before durable intent: identity, deadline, prerequisite or capability check failed` | pre-intent, non-retained |
| Writer lease unavailable | `release request refused: writer lease is no longer live` | pre-intent journal serialization, non-retained |
| No acknowledged snapshot | `release request refused: no acknowledged preparation snapshot ({latest.Error})` | pre-intent journal serialization, non-retained |
| Preparation identity mismatch | `release intent refused: acknowledged preparation identity does not match the capability` | pre-intent journal validation, non-retained |
| Existing Intent / transition plan refusal | `release intent refused: {intentError}` (structural includes `release intent already exists`; also `release intent is forbidden after a durable stop`) | semantic pre-gate, structurally no second Intent |
| Definite journal write failure | `release intent was not durably acknowledged` | semantic pre-gate, non-retained |
| C1 ambiguity | new `AmbiguousPersistence` result with a reason identifying seal ambiguity | semantic pre-gate persistence uncertainty; retained ambiguous |
| Stop-winning gate refusal | `release gate refused: stop won during durable intent` | runtime gate, retained |
| Post-intent expiry/prerequisite/liveness/capability | `release gate refused: expiry, prerequisite, liveness or capability check failed` | runtime gate, retained |
| Capability consumption failure | `release gate refused: capability consumption rejected ({violation})` | runtime gate, retained |
| Retained prior decision | `Duplicate` relabel; no new reason | retention path |

Some journal validation/snapshot-liveness checks occur inside `_journalGate`
(`ReleaseArbiter.cs:81-115`), but they are semantically **before** the runtime
release-winning gate. Lock placement does not define the outside/gate taxonomy.
A concurrent `Refused(NotCommitted)` is not evidence that another caller did
not release. Existing Intent is structural and can never permit a second
Intent/FD4.

The optional `ConcurrentRefusal` kind is not introduced; `Refused` plus the
reason/metadata is the recommended minimal form. The C1 ambiguous result is the
explicit `ReleaseDecisionKind.AmbiguousPersistence` defined in the C1 record;
C4 must use that vocabulary for retention and Duplicate relabeling.

### Tests to add under later authorization

- One deterministic test per reason class above (retained vs non-retained).
- Winner A / concurrent B / later C with exact decisions and one Intent/write.
- Sequential Duplicate and retention asymmetry.
- Failed prerequisites, definite failure, ambiguous retained, existing Intent.
- Stop-winning race and transport-failure retention.
- Writer death/recovery/no resend.
- Duplicate of AmbiguousPersistence; no retry.
- Exact reason/metadata assertions so the taxonomy cannot silently drift.

No waiting API, no additional synchronization, no schema change.

### Human question

Approve documented concurrent-refusal semantics, sequential Duplicate and the
retention asymmetry? **PENDING HUMAN.**
