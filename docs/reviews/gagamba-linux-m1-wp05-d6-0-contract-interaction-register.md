# WP05 D6.0 Contract Interaction Register - Pairwise and Scenario Audit

Author: GLM (independent adversarial design reviewer for the WP05 release
decision holder). Companion to the D6.0 independent contract design review
(Q1) and review matrix (Q2), committed on
`review/wp05-d6-0-independent-contract-design`. This register records the
independent interaction audit: all six pairwise contract interactions and
the eight named scenarios (I1-I8). It authorizes nothing; it approves
nothing.

Status: **INDEPENDENT INTERACTION AUDIT - NO CONTRACT APPROVED**

Date: 2026-10-20

Method: every entry cites verified R12 source structure (line numbers
re-read in this review from the unchanged R12 tree) and proposes the
expected coherent behavior IF the corresponding D6.0 contracts are adopted
with the amendments from Q1/Q2. Entries marked "amendment-gated" depend on
specific required amendments being folded into the design text first.

## Part A - pairwise interactions

### C1 x C2 - ambiguous persistence and stop capability invalidation

- Engaged mechanisms: C1's bounded runtime stop on the ambiguous path; C2's
  capability consumption at every stop acceptance.
- Verified source structure: the ambiguous path ends inside `_journalGate`
  (81-115); stop acceptance takes `_gate` (230) and `_journalGate` (272)
  separately; the file invariant reads "No journal I/O occurs while the
  arbitration gate is held" (ReleaseArbiter.cs:1-5).
- Interaction result: coherent by construction. The wedge makes C2's
  durable stop attempt fail harmlessly (`WriteMutation` refuses at the
  Resolve check, 636-641, before creating any new seal content), so the
  stop is accepted at runtime with a capability consumed and no journal
  progress. The two-instance wedge replay attempt is refused because a
  second instance needs an acknowledged baseline for any forward path.
- Amendment dependence: A-C1-1 (stop invocation tail-of-call, gates
  released) is REQUIRED to prevent inverted lock nesting; A-C1-3 states
  the capability window closure as relying on C2's consumption.
- Residual: journal remains sealed until process death/operator recovery
  (unchanged R12 mechanics; D2/D4-6 verified recoverability when both
  slots decode; torn-slot variants quarantine by TryRecoverCommit's
  operator-resolution rule, 747-748).

### C1 x C3 - ambiguous persistence and persisted stop deadline

- Engaged mechanisms: C1's best-effort durable stop; C3's reconciler
  derivation from a durable StopRecord.
- Verified structure: a StopRecord is legal alongside Release Intent
  (Transitions: TryPlanStop 108-151 does not refuse on Release presence;
  intent-after-stop is refused at 27-31, so the order matters); the
  reconciler consumes the record only if it validates (deadline must equal
  acceptedAt + 5 s exactly, OwnershipValidation 165-175).
- Interaction result: coherent. If C1's durable stop lands before a crash,
  recovery reconciles with the record's deadline (C3); if it does not land
  (wedged or clock-unavailable), no record exists and the no-record path
  applies (see I6/A-C3-3). Either branch yields no release authority
  (C1's own consumption statement plus C2).
- Amendment dependence: A-C3-1 (clock epoch) applies to the reconciler
  side only; C1's runtime deadline is advisor-quality ("evidence
  pending") and always valid-flagged as accepted-live.
- Residual: none beyond the C3 items.

### C1 x C4 - ambiguous persistence and duplicate semantics

- Engaged mechanisms: C1's retained ambiguous decision; C4's retention map
  (sequential Duplicate of whatever is retained; concurrent loser class).
- Verified structure: retention slot `_retainedRelease` (26) is typed by
  `ReleaseDecisionKind`; `Duplicate` relabels the retained decision
  (52, 122); C4's case 7 (intent-planning structural refusal) arises
  before C1's ambiguous point and is non-retained.
- Interaction result: coherent. Once C1 retains an ambiguous decision,
  later callers relabel it `Duplicate` - C4 already declares this
  dependency (C4 case 6, acceptance test 6) and defers the vocabulary to
  C1.
- Amendment dependence: A-C1-2 (exact ambiguous vocabulary) must precede
  C4's final contract text; the exhaustiveness sweep over
  `ReleaseDecisionKind` switches becomes part of the combined acceptance.
- Residual: none.

### C2 x C3 - capability invalidation and persisted deadline

- Engaged mechanisms: C2's consumption at stop acceptance; C3's bounded
  cleanup stop at reconciliation.
- Verified structure: stop acceptance (230-296) consumes (proposed) the
  capability; the reconciler's cleanup stop never touches capabilities
  (its stop is domain-kill + population observation, DomainLease 108-143);
  recovery authority is the live lease, not the capability (RecoveryPlanner
  75-97).
- Interaction result: coherent with a clean division: C2 closes RELEASE
  authority; C3 governs CLEANUP stop boundedness. The expiry path
  (remaining = 0) performs the kill first, then a single population
  observation (mechanism verified at 116 then 122-142).
- Amendment dependence: A-C3-1/A-C3-2 as above; A-C3-3 for the no-record
  branch (C2 consumption ensures no release authority returns even then).
- Residual: I6 (below).

### C2 x C4 - capability invalidation and duplicate semantics

- Engaged mechanisms: C2's stop-winning consumption; C4's stop-winning
  race case (case 8) and retention map.
- Verified structure: stop accepted -> `_stopAccepted = true` (24); two
  refusal sites for in-flight release (entry 55-61 pre-intent; gate
  125-131 post-intent, the latter already consuming). After C2, the
  capability is consumed at acceptance, so every subsequent release path
  fails at the entry `IsLive` check (72) or at consume (149) with the
  conservative `MayHaveExecuted` retention.
- Interaction result: coherent. The release-first ordering preserves the
  release (A-C2-2); the stop-first ordering never yields FD4. Both orders
  converge on C4's documented retention classes.
- Residual: none beyond A-C2 items.

### C3 x C4 - persisted deadline and duplicate semantics

- Engaged mechanisms: no shared runtime symbols; the reconciler is not a
  release decision consumer.
- Interaction result: independent by construction. The only coupling is
  documentation-level: C4's retention vocabulary is a live-owner concept
  and must not be confused with durable recovery classification
  (`ReleaseRecoveryClassification` is the separate conservative
  projection). No conflict found.
- Residual: none.

Pairwise conclusion: no pair requires structural redesign; three pairs
carry amendment dependencies that are already tracked (A-C1-1/2/3,
A-C2-2, A-C3-1..4). No new deadlock vectors: `_gate` is never held across
journal I/O in either entry point, and both amendment sets explicitly
preserve that invariant.

## Part B - scenario register (I1-I8)

### I1 - ambiguous-intent persistence, then stop

- Scenario: release request wedges at the seal-unlink failure; runtime
  then requests stop.
- Verified today: D2 `AmbiguousSealFailureLeavesUncertainIntentAndNoStop`
  (135-163): wedge refusal retained-none, capability live, later stop
  accepted (durable attempt fails harmlessly), recovery after process
  death yields MayHaveExecuted for the recovered Intent.
- Under C1+C2 (amendment-gated): the ambiguous decision is retained
  (A-C1-2), the stop consumes the capability (C2), the retained Duplicate
  path serves later callers. Coherent.
- Residual: wedged journal persists until death/recovery - by design
  (unchanged R12 mechanics); documented by C1 and H-layer recovery rows.

### I2 - stop first, then ambiguous retry (second instance or fresh caller)

- Scenario: durable stop lands, wedged-state or failed-durable-stop retry
  attempts release.
- Verified today: durable-stop case blocked at the journal level
  (Transitions 27-31; D2 `DurableStopBlocksReleaseIntentFromSecondArbiter`
  198-212); failed-durable-stop case is exactly the C2 defect probe
  (215-233) - releasable today.
- Under C2: the failed-durable-stop case closes by capability consumption
  at stop acceptance; any later release attempt fails `IsLive` (72) or
  consume (149). Coherent; both arms of the original CE window closed.

### I3 - release wins the gate, then stop

- Scenario: release consumed the capability and reached transport/committed
  persistence when stop acceptance interleaves.
- Verified structure: `ReadLatest`/WriteMutation serialization under
  `_journalGate` (194-211 vs 272-289); committed-then-stop and stop-then-
  committed are both durable-legal (Transitions 66-106, 108-151); stop's
  `ReleaseAlreadyCommitted` flag sources `_releaseRuntimeCommitted` (158),
  i.e., consume-before-transport counts as runtime-committed even if the
  transport result was Unknown.
- Under C2 (amendment-gated per A-C2-2): stop acceptance stands, the
  retained release is untouched, `TryConsume` refuses with `Consumed` -
  no history rewrite possible. Classifier outcome remains conservative
  (Stopped only after confirmed exec/root; Unknown otherwise,
  Classification 100-103). Coherent.

### I4 - concurrent duplicate during ambiguous persistence

- Scenario: winner wedges inside the journal block; concurrent duplicate
  fires from its own arbiter instance.
- Verified structure: the duplicate passes entry checks (no shared
  retained state across instances), then fails at its own WriteTransition
  (uncertain baseline, 568-573); no transport is reachable from any
  pre-gate path; no FD4 in any branch.
- Under C1+C4: same-wedge refusals are non-retained for the loser, the
  winner's ambiguous decision is retained; process-death recovery decides
  the durable truth. Coherent; no change to the physical one-write
  invariant.

### I5 - stop accepted near the persisted deadline, then recovery

- Scenario: a validated StopRecord exists with a near-expiry deadline;
  reconciliation runs shortly before/after expiry.
- Under C3 (amendment-gated): remaining = max(0, deadline - now) with an
  epoch-corroborated clock (A-C3-1); within-window uses less time; expiry
  triggers the zero-wait urgent path (kill + single observation), whose
  acceptability is the human policy choice; populated-after-attempt maps
  to Unknown debt only after A-C3-2's aggregation amendment.
- Residual: without A-C3-1 the formula is silently wrong
  (`OwnerDomainClock` epoch mismatch); without A-C3-2 the outcome mapping
  says Failed, not the contract's Unknown/quarantine.

### I6 - runtime stop with no durable deadline evidence, then crash/recovery

- Scenario: clock-unavailable or overflow acceptance (238-260) or durable
  attempt failure; stop is real at runtime but no StopRecord persists.
- Verified today: no StopRecord written on those paths (probes 98-132);
  capability live (fixed by C2); recovery after crash has no stop record,
  so the reconciler applies the no-record caller allowance for cleanup.
- Under C3 (amendment-gated per A-C3-3): documented as a cleanup-only
  allowance - not a stop-confirmation window - because no durable stop
  exists to confirm; C2 guarantees no release authority returns. The
  residual policy risk is bounded to cleanup-window length, not release
  safety. The reviewer requires this to be stated in the contract text so
  the no-record path is never read as "fresh stop deadline".

### I7 - repeated recovery across process generations

- Scenario: multiple replacements reconcile repeatedly against the same
  StopRecord.
- Verified structure: the record is immutable (validator 466, 504, 565);
  Reconciler persistence mutates only CompletedCleanup/Cleanup (184-201);
  nothing rewrites the deadline.
- Under C3: same absolute deadline every time; no accumulation (rows 4/5/10
  of the C3 acceptance list). Coherent; deterministic-clock acceptance rows
  required by the remediation proposal section 5 apply.

### I8 - duplicate after partial FD4 delivery

- Scenario: transport throws mid-write (Unknown) or other non-delivery
  result; retention and recovery classification must stay conservative and
  no resend may occur.
- Verified structure: exception map to Unknown (162-169); retained
  Released/MayHaveExecuted with LiveExecutionExcluded for WouldBlock/
  BrokenPipe/Partial (172-182); committed write is attempted after the
  runtime decision, delivery recorded (194-211); one-shot channel prevents
  second attempts (HeldExecution 47-50); recovery classifies a durable
  Committed record conservatively (MayHaveExecuted for the release
  disposition; aggregate Unknown until exec/root corroborate).
- Under C1-C4 unchanged: later callers receive Duplicate of the retained
  decision; no resend path exists anywhere invoked. Coherent.

## Summary table

| Interaction / scenario | Pairs engaged | Coherence under amended contracts | Blocking finding |
|---|---|---|---|
| C1 x C2 | C1, C2 | Coherent | A-C1-1, A-C1-3 must be folded first |
| C1 x C3 | C1, C3 | Coherent | A-C3-1 applies to reconciler path only |
| C1 x C4 | C1, C4 | Coherent | A-C1-2 must precede final C4 text |
| C2 x C3 | C2, C3 | Coherent | A-C3-1..3 |
| C2 x C4 | C2, C4 | Coherent | A-C2-2 |
| C3 x C4 | C3, C4 | Independent | none |
| I1 | C1, C2 | Closed under amended contracts | probes exist today |
| I2 | C2 | Closed by C2 | - |
| I3 | C2 | Closed; conservative orderings verified | - |
| I4 | C1, C4 | Closed; no FD4 in any branch | - |
| I5 | C3 | Coherent, amendment-gated | A-C3-1, A-C3-2 |
| I6 | C2, C3 | Coherent, documented cleanup allowance | A-C3-3 |
| I7 | C3 | No accumulation verified | - |
| I8 | C4 | No resend path; conservative retention | - |

No blocking interaction defect was found; no additional required amendment
beyond the ones tracked in Q1/Q2 emerged from the pairwise or scenario
audit.
