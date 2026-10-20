# WP05 D6.0 Required Design Changes and Approval Handoff (Independent Review Output)

Author: GLM (independent adversarial design reviewer for the WP05 release
decision holder). Final deliverable of the D6.0 independent contract design
review, committed on `review/wp05-d6-0-independent-contract-design` from
exact D6.0 producer tip 23df1c566e7d8f072d924f374bf6891c726c03fe. This
document hands the review results to the human WP05 release/security
decision holder. It does not approve any contract, does not authorize any
code change, remediation tranche or native qualification, and accepts no
gate.

Status: **INDEPENDENT REVIEW HANDOFF - NOTHING APPROVED**

Date: 2026-10-20

Sources of truth for this handoff:

- Main review:
  `docs/reviews/gagamba-linux-m1-wp05-d6-0-independent-contract-design-review.md`
  (Q1; 14 sections; verified R12 baseline; verdicts and disposition Option B).
- Review matrix:
  `docs/reviews/gagamba-linux-m1-wp05-d6-0-contract-review-matrix.md` (Q2;
  per-contract bindings).
- Interaction register:
  `docs/reviews/gagamba-linux-m1-wp05-d6-0-contract-interaction-register.md`
  (Q2; pairwise + I1-I8).
- Producer package under review (audit target, unchanged):
  `analysis/wp05-d6-0-release-contract-decisions` tip 23df1c5... .
- Original requirements: blob
  73eb09af27c5dbc896739ebf48489862ec13e0a9 on published branch
  `review/wp05-release-plan-adversarial-review` (binding note A-PKG-1).

## R1 - Required amendments (blocking for approval-readiness, not for review conclusion)

Each amendment below must be folded into the named D6.0 document as design
text before the corresponding approval question is placed for human
decision. They are precision and completeness requirements raised by this
review; none is a structural objection.

### C1 (owner: producer/human holder, next D6.1 text revision)

- A-C1-1 Stop invocation boundary. The C1 contract must state exactly
  where the bounded runtime stop is invoked in the ambiguous path: after
  the failing request path has released `_journalGate` and before/without
  holding `_gate` across journal I/O (tail-of-call semantics). Rationale
  (verified): `RequestRelease` releases `_journalGate` at 115 and takes
  `_gate` at 118; `RequestStop` takes `_gate` at 230 then `_journalGate`
  at 272; the file invariant "No journal I/O occurs while the arbitration
  gate is held" (ReleaseArbiter.cs:1-5) forbids inverted nesting, and C#
  monitor re-entrancy would hide a same-thread ordering bug, so the
  acceptance plan must include a concurrent opposing-order
  test (release committed-writer racing the ambiguous-path stop) that
  fails loudly on inversion. Acceptance text: no gate-order inversion
  possible; test exists.
- A-C1-2 Exact retained ambiguous vocabulary. The C1 contract must name
  the exact retained decision representation and require exhaustiveness
  verification of every `ReleaseDecisionKind` switch under the new value.
  Rationale (verified): the enum already declares an unused `Stopped`
  value (ReleaseDecision.cs:14; zero repository usages of
  `ReleaseDecisionKind.Stopped`); the producer C4/C1 texts both defer the
  name. Retention types through the kind (`Duplicate` relabels the
  retained decision, 52/122), so the name is semantically load-bearing,
  not aesthetic. Acceptance text: kind named; exhaustiveness verified;
  C4's dependent rows updated to match.
- A-C1-3 Capability invalidation statement in the ambiguous path. The C1
  contract must state the capability disposition between the ambiguous
  failure and stop acceptance: documented as a transient live-while-wedged
  window that C2's stop acceptance closes, or closed directly at the
  ambiguous point. The reviewer assessment prefers relying on C2
  (simpler; the wedged journal blocks every other release path because no
  acknowledged baseline is derivable under a seal, `Resolve` 773-781).
  Acceptance text: window documented and closed by an explicit contract
  statement, whichever the human chooses.

### C2 (owner: producer/human holder, next D6.1 text revision)

- A-C2-1 Consume point. The C2 contract must state that consumption
  happens inside the `_gate` acceptance blocks on every acceptance path
  (normal 263-267, clock-unavailable 237-243, overflow 246-260), and that
  duplicate-stop re-entry (232-235) does not re-consume: exactly-once at
  first acceptance. Rationale (verified): `TryConsume` is store-local and
  lock-free w.r.t. `_journalGate` (PreparedTransaction 60-69), so the
  placement is deadlock-free; the D2 clock-unavailable/overflow probes
  (98-132) assert `IsLive == true` today and become inverted-assertion
  fixtures. Acceptance text: per-path consume stated; probes planned.
- A-C2-2 Release-first preservation. The C2 contract must state the
  release-first interaction: a failed `TryConsume` with `Consumed` pairs
  with an already-consumed capability; stop acceptance still stands;
  `ReleaseAlreadyCommitted` continues to mean "runtime release path had
  consumed before this stop" - which includes the consume-before-transport
  window (158). Acceptance text: interaction stated; the D2/diagnostic
  coverage extended accordingly.
- A-C2-3 CE-03 declaration. The C2 decision text the human records must
  carry the out-of-model declaration verbatim (multi-instance window
  outside the accepted single-arbiter model, closed by the capability
  contract, not composition), currently only in the evidence matrix row.

### C3 (owner: producer/human holder, next D6.1 text revision)

- A-C3-1 Clock identity and epoch corroboration. The C3 contract must
  REQUIRE an epoch-corroborated monotonic clock for the remaining-time
  derivation: the durable StopRecord timestamps are in the QPC-based
  transaction-clock epoch (`OwnerClock`, OwnerPreparationTransaction.cs:
  21-32); `IDomainClock` has two divergent production implementations -
  `OwnerDomainClock` (34-39; `Stopwatch.StartNew()`, construction-moment
  epoch, INCOMPATIBLE) and `LaunchDomainClock` (BubblewrapAdapter.cs:
  517-521; QPC, COMPATIBLE). An unconstrained composition passes a wrong
  clock silently and miscomputes every remaining. The contract must state
  the failure rule when epoch identity cannot be established (fail to the
  conservative path - quarantine/Unknown - never a fabricated window) and
  define the corroboration test. A duration-vs-deadline
  `StopAndAwaitEmpty` overload remains optional; the epoch is what
  matters. Acceptance text: clock identity stated; failure rule stated;
  corroboration test exists.
- A-C3-2 Outcome mapping. The C3 contract's "attempted but not empty ->
  Unknown/quarantine" must be implemented as a Reconciler aggregation
  amendment (unconfirmed stop is currently a Failed component mapping to
  ReconcileOutcome.Failed, 234-235 with 429-436; Unknown/quarantine only
  via Unknown components, 437-442, or missing persistence, 450-459).
  Acceptance text: aggregation rule stated and tested, distinct from
  operation failure.
- A-C3-3 No-record restart window. The C3 contract must document that the
  no-StopRecord path after a runtime-accepted-but-unpersistable stop
  (clock-unavailable/overflow/durable-failure) yields a cleanup-only
  caller allowance across a restart, never a fresh stop deadline, and
  that C2 consumption guarantees no release authority returns (scenario
  I6). Acceptance text: documented; test asserts the no-record path does
  not manufacture a durable stop bound.
- A-C3-4 Domain-already-empty row. The C3 acceptance row "Domain already
  empty: no unnecessary stop" conflicts with today's unconditional kill
  whenever the DomainStop bit is unset (217-248 kill before any
  observation). Either add the explicit pre-observation amendment to the
  design text or rewrite the row to "no unnecessary WAIT" and record the
  kill; both are acceptable, silence is not.

### C4 (owner: producer/human holder, next D6.1 text revision)

- A-C4-1 Documentation discipline (non-blocking for the approval
  question). Bind, in the decision record, the exact R12 reason strings
  per class ("release intent already exists" for the concurrent/structural
  intent refusal; the pre-gate class strings from 64-79 and 81-100; the
  retained gate classes from 125-154) and state that the
  snapshot-liveness/identity checks live inside `_journalGate` (81-115)
  while being semantically outside the release gate (before durable
  intent), so the outside/gate taxonomy cannot be confused with lock
  placement. Optional concurrency-kind question remains as the producer
  left it (reviewer concurs with the minimal Refused+metadata form
  pending A-C4-1's discoverability requirement).

### Package-level (owner: producer, next D6.1 package revision; non-blocking)

- A-PKG-1 Binding precision: cite the publisher branch
  (`review/wp05-release-plan-adversarial-review`) next to the original
  blob id 73eb09af... in the D6.0 documents, since the blob is not an
  ancestor of the D6.0 base and reaches readers only through that branch
  plus the preservation checkpoint (blob 9aa7f8c9...).
- A-PKG-2 Composition boundary: rephrase "Production scope" statements to
  make explicit that `ReleaseArbiter` and `Reconciler` are currently
  uncomposed components (zero production call sites, verified by symbol
  search); changed semantics pre-date any production composition; the
  first composition (WP08 boundary) builds against the revised contracts;
  all adoption evidence stays component-level until composition exists.
- A-PKG-3 Composed acceptance row: add a named acceptance case for
  "ambiguous wedge then salvaged clock-unavailable stop decision" (both
  halves exist as D2 probes, 135-163 and 98-115; the composed shape has
  no named row today).

## R2 - Clarification requests (non-blocking, answer in the next text revision)

- C3 zero-wait policy (the contract choice): the package asks whether an
  expired deadline authorizes kill + one observation as the bounded
  urgent stop. The mechanism is mechanically supported by
  StopAndAwaitEmpty (kill at 116; zero timeout yields a single population
  poll, 122-142) and matches the D2 R27 probe's KillWrites-plus-bound
  expectations; the POLICY decision belongs to the human holder (the
  reviewer does not answer; C3 verdict is therefore CONTRACT CHOICE
  REQUIRED).
- C4 optional `ConcurrentRefusal` kind: genuinely optional; reviewer
  concurs with keeping `Refused` plus discoverable reason/metadata
  (A-C4-1) unless a future consumer requires a distinct kind.
- C3 duration-vs-deadline `StopAndAwaitEmpty` overload: optional;
  epoch corroboration is the substantive requirement (A-C3-1).
- C1 stop-authority source in the wedge (already defined by the package as
  runtime boundary + runtime-recorded deadline + best-effort durable):
  reviewer verified the mechanics; no further clarification needed unless
  the human chooses a different bound source.

## R3 - Nonblocking observations (recorded for completeness)

1. The D6.0 package's R12 source citations were re-verified line by line
   in this review; all cited ranges check out within +/-1 line slop, and
   the five named D2 probes plus D4_6/D4_9 exist at the cited names. No
   fabricated citation found.
2. `TransitionValidator` deadline locking (OwnershipValidation 148,
   165-175) supports the C3 no-reset claim structurally; the C3-B
   provenance rejection is sound for the same reason.
3. The remediation proposal's tranche order (C2 -> C1 -> C4 -> C3) is
   dependency-coherent; the reviewer notes C3 could be scheduled earlier
   without interaction risk (no C1-C4 dependency in either direction)
   - informational, not required.
4. Rollback criteria in the remediation proposal (second physical write,
   no-resend weakening, false definite NotCommitted, live capability
   after stop, deadline reset, fabricated evidence, unjustified schema,
   record-compatibility break) are complete against the verified
   contract semantics.
5. The producer evidence matrix's gate mapping (C1->G3-A11+G3-B08,
   C2->G3-A02+G3-A07, C3->G3-A10/R27, C4->G3-A05, all diagnostic-only)
   matches the original required-changes rows and C8's sub-gate rule.

## R4 - Implementation and requalification prerequisites (if the human approves contracts and tranches)

For each approved tranche (per
`docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md`, with this
review's amendments folded in first):

1. Test-first update of the contract text (R1/R2 items), then the tranche
   implementation on a fresh tree, committed separately.
2. Focused unit/concurrency/fault tests deterministic at the barrier
   seams (`TestBarrier` names stay the observable instrument; no
   sleeps-as-oracle additions).
3. Rerun of affected D1-D3 contract diagnostics (the five D2 probes plus
   stop probes, with truth-inverting assertions for C2's `IsLive`
   fixtures), affected D4 supervisor/crash/no-resend rows (D4_6, D4_9 at
   minimum), and affected D5 native/release-path rows where the production
   tree identity changes.
4. Native artifact rebuild with exact hash binding; no silent artifact
   reuse; SHA-256 manifest; source/environment identity recorded.
5. Fresh independent review per tranche; explicit human approval per
    contract; no automatic transfer into any accepted qualification; the
    existing R12 evidence remains valid for R12 only.
6. Component-level-only claim until the WP08 composition boundary exists
   (A-PKG-2); gates remain diagnostic until the human approves contracts
   AND the qualification plan is re-run on the new tree.

## R5 - Human approval matrix (all PENDING - recorded here for the decision holder)

| Decision question | Raised by | Current state | Owner |
|---|---|---|---|
| Approve C1 explicit ambiguous decision with bounded runtime stop and retained result (with A-C1-1..3 folded into text) | C1 D6.0 doc approval question | PENDING HUMAN | WP05 release decision holder |
| Approve C2 irreversible capability invalidation on stop acceptance across all three paths incl. failed/clock-unavailable (with A-C2-1..3) | C2 D6.0 doc | PENDING HUMAN | WP05 release decision holder |
| Choose C3 zero-wait expired-deadline policy (kill + observation vs other bounded interval), then approve C3 derivation contract with A-C3-1..4 | C3 D6.0 doc open question | PENDING HUMAN (choice + approval) | WP05 release decision holder |
| Approve C4 concurrent-refusal semantics + retention asymmetry documentation (A-C4-1 discipline) | C4 D6.0 doc | PENDING HUMAN | WP05 release decision holder |
| Approve remediation tranches (order C2 -> C1 -> C4 -> C3, or reviewer-noted C3-earlier option) with per-tranche requalification | remediation proposal | PENDING HUMAN | WP05 decision holder |
| Approve D6.1 package revision to fold A-PKG items | this handoff | PENDING HUMAN | producer |

No approval exists in this repository or in this review. Nothing in this
handoff constitutes approval or authorization; it is input to the human
decision process.

## R6 - Preservation statement and deferred items

- H1 (D5-O05 zero-production-caller status integration), H2 (descriptor
  destructive exclusions unclaimed), H3 (optional wait4/pidfd witness,
  bounded control-based standard adopted), H4 (C1-C4 unresolved until
  human approval), H5 (D5 plan-ordering annotation note): all remain
  unchanged and unresolved; the D6.0 deferred-integration document
  preserves them accurately and this review adds no resolution.
- WP08 owner orchestration, WP10 actual target/status integration, full
  G3 acceptance, portability expansions: deferred, unchanged, not
  affected by this review.
- The review branch `review/wp05-d6-0-independent-contract-design` is
  documentation-only and remains local per review protocol; publication
  requires separate authorization (mirroring the D5 practice).
- After the human decisions and the D6.1 revision, the next independent
  review step (if any) starts from the revised package; this review's
  verdicts apply to the audited tip 23df1c566e7d8f072d924f374bf6891c726c03fe
  exactly and do not bind future revisions.

## Final disposition (restated from Q1)

**Option B**: the D6.0 package's defect-to-contract mapping is
independently confirmed against the R12 source; no structural defect was
found in any proposal; the named amendments are required before any
approval question is final; two human decisions are outstanding (C3
policy choice; per-contract approvals); no gate is accepted; release
qualification remains unclaimed and unqualified.
