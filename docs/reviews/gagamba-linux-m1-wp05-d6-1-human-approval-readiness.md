# WP05 D6.1 C1-C4 Human Approval Readiness - GLM Review

Author: GLM (independent adversarial design reviewer). Documents, per contract
decision, the current proposal state at `750914d93edaf5490b42bf0787f20bb0c5089767`,
this review's verdict, what remains before a human holder's decision is
appropriate, exact questions to ask, and consequences of each answer. This
review approves nothing, closes nothing, and does not authorize remediation.
Review-supportable is not verified; work plans and release plans do not change
status. Accepting any contract decision without remediation does not validate
the software.

Status: **HOLDING HUMAN DECISIONS - NOT A DECISION RECORD** (CPG-1: this
review cannot change any of the C1-C4 statuses to Approved)

Date: 2026-10-21

## C1 - Ambiguous intent after final seal-unlink failure

- Current proposal (D6.1): implement exactly what the D6.0 review required for
  the ambiguous path: invoke the bounded runtime stop after `_journalGate`
  release (tail), retain under `_gate`, classify as `AmbiguousPersistence`
  with `MayHaveExecuted`, `NotAttempted`, `DurableCommitted=false`; document
  the capability contradiction window via C2's stop-acceptance consumption;
  pricing-only SLAs for the path.
- Review verdict: DESIGN SUPPORTED WITH REQUIRED AMENDMENTS. The sequence is
  lock-safe and source-consistent, but the D6.1 text states ambiguous-tail
  retention unconditionally while the existing `_gate` rules let a
  concurrently-won stop retain first, in which case the ambiguous caller
  correctly returns a `Duplicate` of the stop-won refusal rather than the
  ambiguous decision (`ReleaseArbiter.cs:120-131` verified).
- Remaining amendments before approval: D6.1-R3 (specify retention-collision
  ordering and extend the opposing-order probe expectations), D6.1-R4
  (provenance of `Wp05ReleaseContractProbes` for the evidence mapping).
- Exact human decision question: "APPROVE C1 ambiguous-release contract as
  D6.1 text amended by D6.1-R3 + D6.1-R4 (human decision, reviews unsupported)?"
- If approved: D6.1 becomes the authoritative C1 documentation contract;
  implementation (WP06+) may cite it as authority, subject to the
  implementation-prerequisite test rows and eventual D2/F2.2/B2.X probe
  evidence. Nothing about the R12 binary changes; no gate closes; release
  remains unqualified.
- If rejected: contract decisions remain Needs review; any release gate stays
  closed; if no D6.2 revision occurs, C1 remains with a text-vs-source
  ordering gap (unresolved state acceptable only because release is
  diagnostics/blocked); remediation does not become authorized.
- Prerequisites: none (text-level decision), but the holder should see that
  D6.1-R3 is a correctness clause, not decoration.
- Unresolved boundaries: WP08 production call sites still zero; FD4 renders
  the wedge permanently inoperable while the owner lives (verified: every
  forward path refuses through a sealed journal); the ambiguity determination
  itself is a D2 runtime qualification item, not a text item.

## C2 - Stop capability consumption

- Current proposal (D6.1): consume inside `_gate` acceptance blocks on every
  acceptance path (normal, clock-unavailable, overflow); duplicate re-entry
  never re-consumes; `ReleaseAlreadyCommitted` covers consume-before-transport;
  CE-03 declared out of model.
- Review verdict: DESIGN SUPPORTED FOR HUMAN DECISION. All insertion points
  verified source-consistent; consumption is store-local, irreversible,
  exactly-once; release-first history is never rewritten; the accepted runtime
  stop persists nothing on failure paths; the consume-before-transport window
  is truthful. This is review support, not technical verification.
- Remaining amendments before approval: none (text-level). Implementation
  prerequisites: inverted-assertion probes (`IsLive == false`) on all three
  paths, failed-durable-stop fixture, second-arbiter fixture.
- Exact human decision question: "APPROVE C2 stop-capability consumption
  contract as written (human decision, reviews unsupported)?"
- If approved: C2 becomes the authoritative stop-side documentation contract
  for capability closure; WP06+ implementation cites it; no gate closes.
- If rejected: C2 stays Needs review; any implementation of stop acceptance
  would lack an authoritative capability-closure contract; release gate stays
  closed.
- Prerequisites: C2 interacts with every other contract; the holder should
  review it together with C1 (its ambiguity closure relies on C2's
  consumption) and C4 (stop-first/refusal classification).
- Unresolved boundaries: multi-instance coordination remains out of model
  (CE-03); the single-arbiter assumption is a declared, synthetic-only
  construction; store-local `Consumed` is terminal at R12 (`Dispose` is the
  only removal path).

## C3 - Stop deadline binding and repeated reconciliation, plus C3 zero-wait policy (two entries)

### C3(a) - Expired-deadline policy choice

- Current proposals (D6.1, aligned by `e63a6d4`/`29b58e2`): Policy A =
  identity-checked kill + one immediate population observation, no additional
  waiting allowance, Unknown/quarantine when emptiness is unconfirmed;
  Policy B = a separately specified bounded cleanup/observation interval with
  its own authority and upper bound. Reviewer explicitly does not answer.
- Review verdict: CONTRACT CHOICE REQUIRED. Both policies are mechanically
  supportable in the reviewed text; Policy A was found technically
  supportable against R12 mechanics (kill is immediate at `Reconciler.cs:231`;
  `StopAndAwaitEmpty` with zero timeout performs the kill plus one zero-delay
  poll, verified in `DomainLease.cs:108-143`) but is NOT selected here; status
  stays `C3 ZERO-WAIT POLICY - PENDING HUMAN`.
- Remaining amendments before approval: D6.1-R1 (name the
  Failed/Unknown/Confirmed discrimination mechanism instead of exception-text
  classification; specify the final persistence-failure row change
  `:465-471`) and D6.1-R2 (bind the Qpc epoch mechanism to the primitive: a
  sealed production domain clock with the OwnerClock conversion, a
   behavior-based corroboration test, and an explicitly non-Qpc fixture must
   reject; plus a definitional scoping sentence: "Qpc" names the
   `Stopwatch.GetTimestamp()` epoch primitive and its constant
   `Stopwatch.Frequency`, comparable across processes and OSes, with Linux
   backed by CLOCK_MONOTONIC).
- Exact human decision question(s): "Does an expired durable stop deadline
  authorize the identity-checked kill-plus-one-immediate-observation behavior
  as the accepted urgent-stop contract (Policy A), with the alternative being
  a separately specified bounded cleanup/observation interval (Policy B)?" and
  "APPROVE C3 deadline/reconciliation contract as amended by D6.1-R1 + D6.1-R2
  (human decision, reviews unsupported)?" The first answer gates the second.
- If Policy A selected and contract approved: expired-deadline domains get the
  identity-checked kill with no additional waiting allowance and one immediate
  population observation; unconfirmed emptiness -> Unknown/quarantine
  (via D6.1-R1's mechanism: kill-write failure -> component Failed;
  observation-unavailable or emptiness-unconfirmed -> Unknown; persisted
  confirmations -> per-record Confirmed). Neither policy resets the stored
  deadline; neither authorizes release. No auto-remediation; no gate closes.
- If Policy B selected: a separately specified bounded cleanup/observation
  interval applies with its own authority and upper bound; deadline
  immutability and the no-new-five-seconds constraint stand; everything else
  in the mapping is unchanged. If the contract itself is rejected regardless:
  C3 stays Needs review; deadline/consumption behavior has no authoritative
  contract; the release gate stays closed.
- Prerequisites: the holder must decide the policy BEFORE the contract text
  approval, because the expired-deadline outcome row and its test wording
  depend on the choice; D6.1-R1/R2 must be folded in regardless of policy
  choice.
- Unresolved boundaries: zero-timeout semantics still require D2 runtime
  qualification (kill vs observation timing); the `LaunchDomainClock`
  corroboration test is an implementation prerequisite; epoch-corroborated
  clock rejection is verified structurally possible (boot refusal
  `Reconciler.cs:75-97`) but not yet exercised.

### C3(b) - Deadline binding and reconciliation contract (independent of the policy choice)

- Current proposal (D6.1): epoch-corroborated monotonic clock (MonotonicEpoch,
  Qpc only); remaining allowance derived from the immutable durable record
  (`remaining = max(0, StopDeadlineNanoseconds - corroborated now)`) instead of
  the caller-supplied allowance, with no deadline reset or extension by any
  caller or repeated reconciliation; bounded cleanup-only allowance when no
  StopRecord exists (never fabricated as a durable deadline); quarantine or
  Unknown classification on zero/invalid/unverifiable deadlines; immutable
   record and validator gates accept only acceptedAt > 0 and deadline =
   acceptedAt + 5_000_000_000; timing evidence uses a controllable monotonic
   clock (wall-clock sleeps as the sole timing basis and the deadline-as-
   permission reading are rejected); duplicates and repeated
  reconciliations reuse the same absolute deadline and ignore duplicate
  records.
- Review verdict: DESIGN SUPPORTED WITH REQUIRED AMENDMENTS (D6.1-R1, D6.1-R2,
  per A-C3-1/A-C3-2), alongside the separate policy question above. The
  D6.1 mapping's outcome vocabulary (Failed / Unknown-or-quarantine / Refused /
  Confirmed, with persistence uncertainty after otherwise-confirmed empty
  cleanup classified Unknown/quarantine) is source-truthful; D6.1-R1 adds the
  discrimination mechanism that makes each row mechanically decidable.
- Exact human decision question: see C3(a) second question.
- Consequences: as in C3(a) contract-answer rows.
- Prerequisites: fold D6.1-R1/R2 before approval; C2 consumption guarantees
  (A-C2-1) are load-bearing for the derivation chain.
- Unresolved boundaries: WP08 boundary (component-only, zero production call
  sites) applies to the entire C3 machinery; B2 recovery paths rely on the
  conservative `MayHaveExecuted` projection; final persistence failure indeed
  shifts the aggregate to Failed at R12 (`Reconciler.cs:465-471`) - the
  D6.1-R1 amendment names this row so the approved text matches source truth.

## C4 - Duplicate release and retry semantics

- Current proposal (D6.1): exact R12 reason strings bound per class (verified
  verbatim across `ReleaseArbiter.cs:59,78,86,93,99,107,113,130,144,153`,
  `OwnershipTransitions.cs:23,29`, relabels `:52/:122`); lock-placement vs
  semantic-taxonomy distinction stated; Refused + discoverable metadata
  retained; `ReleaseAlreadyCommitted` includes the consume-before-transport
  window; semantic taxonomy retained across planned insignificant changes;
  no new ConcurrentRefusal kind; duplicate handling (single snapshot, no
  feedback loop).
- Review verdict: DESIGN SUPPORTED FOR HUMAN DECISION. The taxonomy is
  source-exact; retention vs retry routing matches source behavior; the
  optional retry-futility refinement for the pre-retention `Refused(
  NotCommitted, "release request refused: no acknowledged preparation
  snapshot (commit seal present...)")` shape noted for a live owner is
  folded into D6.1-R4's tail as optional, not required.
- Remaining amendments before approval: none required (D6.1-R4 optional tail
  only). Implementation prerequisites: per-reason-class assertion tests.
- Exact human decision question: "APPROVE C4 duplicate-release taxonomy
  contract as written (human decision, reviews unsupported)?"
- If approved: C4 becomes the authoritative duplicate/retry documentation
  contract; consumers may rely on the exact strings for observability mapping
  only (they are not a dispatch contract: no production consumer switches on
  `ReleaseDecisionKind` at R12 - verified).
- If rejected: C4 stays Needs review; observability assertions in later
  phases would lack an authoritative reference; release gate stays closed.
- Prerequisites: review C1 together (the `AmbiguousPersistence` vocabulary
  C4 inherits is defined there); review C4's taxonomy with C2's stop-first
  row (Pair 5 in the interaction register).
- Unresolved boundaries: WP08 boundary; exact-string tests are an
  implementation prerequisite; string stability across future refactors is a
  maintenance discipline, not a capability contract guarantee.

## Cross-cutting readiness notes (all contracts)

- Nothing here changes R12 behavior; all approval consequences are
  documentation-authority consequences only; the release gate is
  diagnostics/blocked until human approval AND later real evidence (D2
  probes, D9.7 containment, B2.X zero-influence, D22/D24); no remediation is
  authorized by this review.
- Review support is limited to: verifiable correctness reasoning about R12
  structure, factual accuracy of the D6.1 text, and traceability of every
  amendment row. It does not include runtime observation, execution of any
  test suite, or acceptance of any gate evidence.
- The D6.2 revision (if the holder proceeds) should fold D6.1-R1..R4 into the
  four decision records in one narrowly scoped documentation change; no C1-C4
  status may change from review work itself (CPG-1 remains binding).

## Consolidated decision matrix

| Contract | Review verdict | Required text amendments | Policy gate | Decision question count |
|---|---|---|---|---|
| C1 | Design supported with required amendments (D6.1-R3, D6.1-R4) | D6.1-R3, D6.1-R4 | None - approval only | 1 |
| C2 | Design supported for human decision | None | None | 1 |
| C3(a) policy | Contract choice required | D6.1-R1, D6.1-R2 (reflected in C3(b)) | YES - A vs B holder choice, reviewer absent | 1 (policy) |
| C3(b) contract | Design supported with required amendments (D6.1-R1, D6.1-R2) | D6.1-R1, D6.1-R2 | Gated by C3(a) | 1 (shared with C3(a)) |
| C4 | Design supported for human decision | None (D6.1-R4 tail optional) | None | 1 |

Total human decision questions: 4 (C1 approve, C2 approve, C3 policy select,
C3 contract approve). None may be answered by this review. No contract is
approved by this document; statuses remain Grievance/Needs review/pending
human until the holder answers.
