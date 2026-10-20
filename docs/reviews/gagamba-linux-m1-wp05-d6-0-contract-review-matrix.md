# WP05 D6.0 Contract Review Matrix - Independently Verified Bindings

Author: GLM (independent adversarial design reviewer for the WP05 release
decision holder). Companion to
`docs/reviews/gagamba-linux-m1-wp05-d6-0-independent-contract-design-review.md`
(Q1, commit on `review/wp05-d6-0-independent-contract-design`). This matrix
binds, for each contract, the original requirement, the R12 verification the
reviewer performed, the producer contract essence, the reviewer deltas, and
the verdict. It authorizes nothing and accepts no gate.

Status: **INDEPENDENT REVIEW MATRIX - NO CONTRACT APPROVED**

Date: 2026-10-20

Binding: D6.0 producer tip 23df1c566e7d8f072d924f374bf6891c726c03fe; base
578b2084cfa09be76e73d0fb26e09f8d823f9ae6; R12 src (unchanged across the D6.0
chain); original required-changes blob
73eb09af27c5dbc896739ebf48489862ec13e0a9 (branch
`review/wp05-release-plan-adversarial-review`); D5 preservation verified
published. All source line numbers below were re-read from the R12 tree in
this review; they are not transcriptions of the producer package.

## Original-requirement source rows (C1-C4 of the required-changes document)

- C1 (binds Q06; blocks G3-A11, G3-B08): distinguish definite failures from
  ambiguous seal-release failures at the arbiter boundary; in the ambiguous
  case report a state other than definite NotCommitted and request a bounded
  stop; define the wedge-case stop mechanics: runtime boundary + deadline
  recorded at runtime, durable evidence best-effort, orphan cleanup remains
  the recovery backstop. Acceptance: decision recorded; ambiguity-vs-definite
  fault wrappers (test-only); probe reproduces CE-02's shape; wedge behavior
  observed (no false NotCommitted, no FD4, eventual MayHaveExecuted on
  recovery). Severity: physical release path stays blocked throughout; the
  injury is evidential/contractual.
- C2 (binds Q05; blocks G3-A02, G3-A07): consume/invalidate the capability at
  runtime stop acceptance - or at first post-stop refusal, explicitly chosen;
  test asserting IsLive == false after stop-first; declare the CE-03
  multi-instance window outside the accepted single-arbiter owner model and
  closed by the capability contract, not composition. Acceptance: decision
  recorded; test exists; remediation merged separately thereafter. Severity:
  no runnable single-instance path demonstrates release-after-won-stop;
  journal intent-after-durable-stop guard plus _stopAccepted already block
  the accepted composition; the breach is defense-in-depth.
- C3 (binds Q04; blocks G3-A10 and any R27 pass): Reconciler (or a
  production caller path) must derive the stop allowance from
  StopRecord.StopDeadlineNanoseconds (remaining = deadline - now) or receive
  a caller-supplied value provably computed from the record; tests:
  within-window takeover uses remaining allowance; expired deadline performs
  the bounded urgent stop; no path can reset or extend the deadline.
  Acceptance: decision recorded; signature/test plan fixed; G3-A10 may run
  as diagnostic only. Severity: validator already forbids rewriting the
  deadline; the gap is no consumer converting the persisted bound into the
  actually-used allowance.
- C4 (binds Q07; blocks G3-A05): decide between (a) an in-progress retained
  decision delivered to concurrent losers (or Duplicate of the winner's
  outcome) and (b) a narrowed documented API that permits concurrent losers'
  Refused while the sequential contract stays Duplicate; document CE-01's
  retention asymmetry (outside-gate refusals not retained; gate refusals
  retained) as deliberate contract with retry expectations per failure
  class. Acceptance: decision recorded; deterministic concurrent test
  reproduces CE-04's three-caller outcome (or chosen replacement); sequential
  and concurrent contracts documented separately. Severity: no second-write
  hazard exists.

## C1 matrix row (R09 ambiguous intent)

| Aspect | Binding |
|---|---|
| R12 defect, independently confirmed | `ReleaseArbiter.cs:110-114` collapses every WriteTransition failure to definite `Refused(NotCommitted)`; `OwnershipJournal.cs:424-489` stages both slots fully before the seal-unlink failure at 486-489; sealed state persists for later calls (`Resolve` uncertainty 773-781; `WriteMutation` refuses 636-641; `TryRecoverCommit` refuses while a live writer holds the fence, 716-721). Capability stays live (`PreparedTransaction.cs:52-55`). |
| Producer repro evidence, independently located | D2 `AmbiguousSealFailureLeavesUncertainIntentAndNoStop` (probes file:135), D4_6 fresh-process (D4 supervisor file:194), sealing counter plumbing `FakeJournalStorage.FailAt("unlink", 2)` = post-content phase per original C6. |
| Producer contract essence | Explicit ambiguous decision kind with `MayHaveExecuted`; no FD4; bounded runtime stop accepted at the runtime boundary; durable stop evidence best-effort; result retained; later callers Duplicate it; conservative recovery unchanged. |
| Reviewer assessment | Runtime channel verified real (`_stopAccepted`, 24; stop acceptance journal-independent). Best-effort durable stop in the wedge fails harmlessly (WriteMutation refuses at the Resolve check before creating new seal content). Recovery classifier already maps recovered Intent to `MayHaveExecuted` (`ReleaseRecoveryClassification.cs:36-38`) - vocabulary consistent. Retention map compatible: retention starts after durable intent, which the ambiguous case is past. |
| Reviewer deltas | A-C1-1 stop invocation boundary (no `_gate`/`_journalGate` held; tail-of-call; deadlock test at inverted order). A-C1-2 exact retained vocabulary (unused `ReleaseDecisionKind.Stopped`, value 3, zero usages - choose and verify exhaustiveness). A-C1-3 capability invalidation statement for the ambiguous path (rely on C2 consumption at stop acceptance; document the transient live-while-wedged window; wedged journal blocks every other release path). |
| Verdict | `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS` |
| Gates unchanged before approval | G3-A11, G3-B08 diagnostic-only. |

## C2 matrix row (R08 stop-first capability invalidation)

| Aspect | Binding |
|---|---|
| R12 defect, independently confirmed | `RequestStop` (226-297) never references the capability; `IsLive` is a pure store lookup; probes `StopFirstLeavesCapabilityLiveOnR12` (183) and `StopPersistenceFailureAllowsSecondArbiterReleaseOnR12` (215) demonstrate the live capability with and without durable stop evidence; the latter proves the second-arbiter release with a full transport attempt and durable Committed record. |
| Producer repro evidence, independently located | D2 stop-first/failed-stop probes (above); D5-CR3/CR3b two-owner/fence diagnostics were independently reproduced in the D5 review layer (preservation 578b2084, published). |
| Producer contract essence | Consume/invalidate the capability at every runtime stop acceptance (normal, clock-unavailable, overflow); irreversible; store-owned single-use semantics; no schema change; CE-03 window declared outside the single-arbiter model. |
| Reviewer assessment | The chosen alternative is the stronger of the two original options. Feasible with the existing store API; consumption is store-local, lock-free w.r.t. the journal gate, so placing it inside the `_gate` acceptance blocks adds no nesting risk. Stop-after-release-first: `TryConsume` refuses (`Consumed`), retained release stands, `ReleaseAlreadyCommitted` is sourced from `_releaseRuntimeCommitted` (set at 158, before transport) - conservative semantics verified. The two post-acceptance refusal sites in R12 (entry 55-61, gate 125-131) plus the journal-level guard (Transitions 27-31 for the durable case) mean the runtime contract is defense-in-depth, exactly as the original severity note records. |
| Reviewer deltas | A-C2-1 consume point per acceptance path; duplicate-stop re-entry must not re-consume. A-C2-2 release-first preservation semantics; `ReleaseAlreadyCommitted` includes the pre-transport window. A-C2-3 CE-03 declaration copied verbatim into the C2 decision document. |
| Verdict | `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS` |
| Gates unchanged before approval | G3-A02, G3-A07 diagnostic-only. |

## C3 matrix row (R27 persisted stop-deadline consumption)

| Aspect | Binding |
|---|---|
| R12 defect, independently confirmed | `Reconciler` stores/forwards the caller allowance (124-130, 231) and never reads the durable deadline; the D2 R27 probe (268-285) shows the caller allowance consumed after the persisted deadline expired; validator makes rewrite structurally impossible (deadline must equal acceptedAt + 5,000,000,000 exactly, OwnershipValidation 165-175); StopRecord record shape and allowance constant at OwnershipRecord 119-122, 202. |
| Producer repro evidence, independently located | D2 R27 probe above (component-level; Reconciler currently has zero production callers - scope fact 4.9 of Q1). |
| Producer contract essence | Reconciler derives `remaining = max(0, StopDeadline - now)` from the durable record with a verified clock; expired -> bounded urgent stop with zero remaining wait; zero/unverifiable -> Unknown/quarantine; no fresh five seconds; record immutable; C3-B provenance and C3-C authority object rejected (reviewer concurs with both rejections). |
| Reviewer assessment | The derivation contract is the only structurally sound R27 resolution. NOTE the reviewer-found omissions: (1) clock-epoch identity - durable StopRecord timestamps come from the QPC-based transaction clock (`OwnerClock`, OwnerPreparationTransaction.cs:21-32), while `IDomainClock` has two divergent production forms: `OwnerDomainClock` (34-39, `Stopwatch.StartNew()` construction-moment epoch - incompatible) and `LaunchDomainClock` (BubblewrapAdapter 517-521, QPC - compatible); an unconstrained composition silently miscomputes remaining. (2) outcome mapping - the stated Unknown/quarantine after an attempted-but-not-empty urgent stop does not exist in the current Reconciler aggregation (unconfirmed stop -> Failed -> ReconcileOutcome.Failed, 234-235 and 429-436; Unknown/quarantine only via Unknown components 437-442 or missing persistence 450-459). (3) structural reachability - durable StopDeadline <= 0 cannot validate (148), so the zero/invalid case is defensive; the reachable zero-deadline reality is the runtime clock-unavailable/overflow stop which persists no StopRecord (238-260), landing recovery on the no-record caller-allowance path. (4) the "domain already empty: no unnecessary stop" acceptance row conflicts with today's unconditional kill whenever the DomainStop bit is unset (217-248). |
| Reviewer deltas | A-C3-1 epoch-corroborated clock requirement + failure rule + corroboration test. A-C3-2 aggregation amendment for Unknown/quarantine mapping. A-C3-3 no-record restart window documented as cleanup-only allowance (I6). A-C3-4 rewrite or amend the domain-already-empty row. Human choice: acceptability of zero-wait kill + single observation at expiry (mechanically supported by StopAndAwaitEmpty: kill executes first, then one poll with zero timeout). |
| Verdict | `CONTRACT CHOICE REQUIRED` |
| Gates unchanged before approval | G3-A10 and any R27 pass, diagnostic-only per the original row. |

## C4 matrix row (R02/R06 duplicate and concurrent semantics)

| Aspect | Binding |
|---|---|
| R12 behavior, independently confirmed | Sequential Duplicate via retained relabel (50-53); concurrent loser refused at intent-planning ("release intent already exists", Transitions 21-25; arbiter journalBlock 102-108) - non-retained; winner retains (176-182) and persists committed (194-211); later caller Duplicate of the retained decision; retention asymmetry exactly as producer C4 states (outside-gate 64-79 and 81-100 non-retained; gate 125-154 retained); `HeldExecution.TryWriteRelease` one-shot (Interlocked.Exchange, 47-50); no second physical write in any concurrent shape (journal guards + one-shot channel). Probes: `ConcurrentDuplicateReleaseProducesOnePhysicalWrite` (236), D4_9 owner-host concurrent probe (D4 supervisor:284), D5-CR3 two-owner/fence row (independently reproduced under 578b2084). |
| Producer contract essence | C4-B: explicit concurrent-refusal contract documenting observed behavior; later callers receive Duplicate; retention asymmetry declared deliberate; no waiting state; one physical write; `Refused` is not durable classification; `Duplicate` is not delivery proof. |
| Reviewer assessment | Matches observed R12 behavior exactly; the C4-A rejection (wait/notification/crash surface) and C4-C rejection (state without consumer) are correct; the ten-case contract maps one-to-one onto verified code paths; the explicit deferral of the C1-dependent rows is proper. |
| Reviewer deltas | A-C4-1 (documentation discipline, non-blocking): bind the exact R12 reason strings per class in the decision record and clarify that the snapshot-liveness/identity checks sit inside `_journalGate` (81-115) but semantically outside the gate (before durable intent) so the outside/gate taxonomy is not confused with lock placement. The optional-ConcurrentRefusal-kind open question concurs with the producer's own minimal recommendation (keep Refused + metadata). |
| Verdict | `DESIGN SUPPORTED FOR HUMAN DECISION` |
| Gates unchanged before approval | G3-A05 diagnostic-only. |

## Package-level matrix rows

| Item | Binding | Disposition |
|---|---|---|
| A-PKG-1 | Original requirements blob 73eb09af... is not an ancestor of the D6.0 base 578b2084; it is reachable on the published branch `review/wp05-release-plan-adversarial-review`; the D5 lineage carries the preservation checkpoint (blob 9aa7f8c9...) that binds the three original blob identities without embedding the full text. | Non-blocking binding-precision note; cite the branch name next to the blob id in the D6.0 documents. |
| A-PKG-2 | `ReleaseArbiter` and `Reconciler` have zero production call sites in R12 (verified by symbol search: each appears only in its own definition file). "Production scope" statements in the remediation proposal name currently-uncomposed components; first production composition arrives at the WP08 boundary. | Non-blocking phrasing amendment; adoption evidence remains component-level until composition; gates stay diagnostic. |
| A-PKG-3 | No dedicated acceptance row for the composed case "ambiguous wedge THEN salvaged StopDecision (clock-unavailable shape)" although both halves exist as D2 probes (135-163; 98-115). | Non-blocking; add a composed acceptance row in the C1/R-C1 test list. |
| A-PKG-4 | Evidence-and-gate matrix self-declaration (no gate accepted; diagnostics only) verified against required-changes C8 and the D5 handoff H1-H5. | Concurs. |

## Verdict summary

| Contract | Original gate binding | Reviewer verdict | Required amendments |
|---|---|---|---|
| C1 R09 | G3-A11, G3-B08 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C1-1, A-C1-2, A-C1-3 |
| C2 R08 | G3-A02, G3-A07 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C2-1, A-C2-2, A-C2-3 |
| C3 R27 | G3-A10 (+R27 rule) | CONTRACT CHOICE REQUIRED | A-C3-1..A-C3-4 + human choice on zero-wait policy |
| C4 R02/R06 | G3-A05 | DESIGN SUPPORTED FOR HUMAN DECISION | A-C4-1 (non-blocking discipline) |
| Package | - | Supported (Option B overall) | A-PKG-1..A-PKG-3 (non-blocking) |
