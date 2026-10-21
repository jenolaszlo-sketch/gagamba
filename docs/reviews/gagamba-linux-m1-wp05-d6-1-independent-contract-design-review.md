# WP05 D6.1 Independent Contract Design Review - C1-C4 Release Contract Proposals

Author: GLM (independent adversarial architecture/correctness reviewer for the
WP05 release decision holder). This review independently verifies the ten-commit
D6.1 documentation-only revision of the C1-C4 contract proposals against the
frozen R12 production source. It verifies documentation completeness separately
from technical correctness; it does not approve any contract, does not authorize
any production remediation, accepts no gate, and does not qualify release.

Status: **INDEPENDENT REVIEW COMPLETE - NOTHING APPROVED**

Date: 2026-10-21

## 1. Git and source binding

- Canonical repository/worktree: `C:\Users\Laszlos\source\repos\Gagamba`
  (untouched; untracked local artifacts only, tracked tree clean).
- Review worktree (isolated, documentation-only):
  `C:\Users\Laszlos\source\repos\Gagamba-review-d61-glm`.
- Review branch: `review/wp05-d6-1-independent-contract-design-2`, created
  exactly at the D6.1 final HEAD.
- Producer final HEAD under review:
  `750914d93edaf5490b42bf0787f20bb0c5089767` (verified to resolve locally).
- Labeled design revision: `bdd711d716fe2bcf0cd06183efc4d36ab030ece4`
  (verified below; NOT the tree tip - the review target is the full tree at
  `750914d` as instructed).
- D6.0 base: `947aebe7df2eefd116cfba8a4b3e1baee2067e0d` (verified: the first
  D6.1 commit's parent is exactly this object).
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen WP05 harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D6.0 producer: `23df1c566e7d8f072d924f374bf6891c726c03fe`; D6.0 independent
  review Q3: `2a235c391673258023d3b3058ad7bbb31cc20406` (all four review
  documents re-read in full from that revision).
- Fixture-branch test assets referenced for provenance: counterexample probes
  `dc4c372` (tests only; verified `src/` is byte-identical to R12, so D2 probe
  evidence binds to R12 production behavior).

### Ten-commit D6.1 chain, independently resolved

Full parent chain read with `git log --format='%h %P %s'`; every abbreviated SHA
resolves and parents match the reported sequence:

1. `d662e0d` - parent `947aebe7...` - C1/C2 decision-record revisions (2 files).
2. `d1424c7` - C3/C4 decision-record revisions (2 files).
3. `2793453` - package docs (proposals, summary, evidence/gate matrix,
   remediation proposal, handoff, work-queue; 6 files).
4. `a345e31` - D6.1 reconciliation, focused-review handoff, human approval
   questions (3 files).
5. `ec973cc` - proposals doc: D6.1 revision section + interactions (1 file).
6. `a6e092b` - focused-review handoff design-commit binding (1 file).
7. `e63a6d4` - C3 status line "CONTRACT CHOICE REQUIRED - HUMAN DECISION
   PENDING" (1 file).
8. `29b58e2` - C3 policy wording alignment: already-empty row rewritten to
   Option B wording; expired-deadline test row 3 rewritten to "policy-pending;
   Policy A recommends zero-wait kill + one observation, not selected" (2
   hunks, 1 file).
9. `bdd711d` - C3 exact epoch corroboration mechanism paragraph added
   (`MonotonicEpoch` marker with one production value `Qpc`; narrow `Epoch`
   member on `IDomainClock`; Reconciler accepts only `Qpc`; test clocks must
   explicitly declare the accepted epoch; no new durable field) (1 file).
10. `750914d` - focused-review handoff design-commit pointer corrected from
    `ec973cc` to `bdd711d` (1 hunk, 1 file).

Findings on commits 8-10: only `bdd711d` introduces a new semantic commitment
(the exact epoch-corroboration mechanism; analyzed in section 6). `e63a6d4`
and `29b58e2` are consistency/policy-discipline changes that prevent the
expired-deadline policy from being smuggled as selected (good). `750914d`
changes a binding pointer only. No new contradictions were introduced by the
final three commits beyond the issues recorded in section 10.

## 2. Complete D6.1 change inventory (independently verified)

`git diff --numstat 947aebe7..750914d`: exactly 13 files, all `.md`,
772 insertions, 2 deletions:

- `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md` (+97)
- `docs/decisions/wp05-d6-0-c2-stop-capability.md` (+73)
- `docs/decisions/wp05-d6-0-c3-stop-deadline.md` (+142/-2)
- `docs/decisions/wp05-d6-0-c4-duplicate-semantics.md` (+56)
- `docs/decisions/gagamba-linux-m1-wp05-d6-0-decision-summary.md` (+21)
- `docs/decisions/gagamba-linux-m1-wp05-d6-0-evidence-and-gate-matrix.md` (+33)
- `docs/decisions/gagamba-linux-m1-wp05-d6-0-release-contract-decision-proposals.md` (+62)
- `docs/decisions/gagamba-linux-m1-wp05-d6-1-amendment-reconciliation.md` (+45)
- `docs/decisions/gagamba-linux-m1-wp05-d6-1-focused-review-handoff.md` (+93)
- `docs/decisions/gagamba-linux-m1-wp05-d6-1-human-approval-questions.md` (+101)
- `docs/handoff.md` (+4)
- `docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md` (+41)
- `docs/work-queue.md` (+4)

All thirteen files were read in full. Producer-reported facts verified
independently:

- Documentation-only: verified (no `src/`, `tests/`, `native/`, `eng/`,
  CI/artifact, or fixture paths in the diff).
- No production source changes: verified (R12 tree unchanged; additionally the
  R12 `src/` subtree is byte-identical to the fixture-branch probe tree
  `dc4c372`, so the D2 probe evidence binds to R12 behavior).
- No test/native/ci/artifact changes: verified by path audit.
- Clean worktree: verified for tracked state.
- Branch not published: verified (`origin/` has no
  `design/wp05-d6-1-contract-amendments`; publication would require separate
  authorization).

Neutral record: a branch named `review/wp05-d6-1-independent-contract-design`
pre-exists with three review-shaped commits (`adc913e`, `18e87fb`, `8aded09`)
directly atop `750914d`. This review did not read those documents as input; all
verdicts below are from fresh R12/source verification. This review runs on the
new distinct branch `-2`.

## 3. Independent amendment verification

Method: each amendment was verified against (a) the D6.0 review's original
finding text (Q1 review sections 5.4/6.4/7.4/8.4 + Q3 handoff R1), (b) the
D6.1 document revisions, and (c) the R12 source. Detailed row-level statements
are in the amendment matrix; the substance is restated here per contract.

### C1 - ambiguous release persistence

**A-C1-1 (lock-safe tail-of-call runtime stop): PARTIALLY ADDRESSED.**

Verified implementable against the actual lock order in R12:

- Prechecks run without `_gate` (`ReleaseArbiter.cs:64-79`); the intent attempt
  is a single `_journalGate` phase (`:81-115`), released at 115; the runtime
  winner phases (`:118-183`, `:215-223`) acquire only `_gate`; `RequestStop`
  takes `_gate` at 230 and `_journalGate` at 272 as separate phases; the file
  invariant "No journal I/O occurs while the arbitration gate is held"
  (`ReleaseArbiter.cs:1-5`) holds in R12 and in the proposed sequence.
- The proposed sequence (journal attempt -> typed ambiguous result -> release
  `_journalGate` -> retain `AmbiguousPersistence` under `_gate` -> release
  `_gate` -> tail `RequestStop` -> possibly `_gate` + `_journalGate`) introduces
  no nested `_gate`/`_journalGate` composition anywhere, so no opposing-order
  interleaving of a ReleaseIntent writer, committed-evidence writer, ambiguous
  path stop, or concurrent release caller can invert the order or deadlock.
  Verified across all concurrent compositions of the five `_gate`/`_journalGate`
  phases in both entry points.
- The record also correctly declines to use C# monitor re-entrancy as proof and
  requires the opposing-order probe (`AmbiguousReleaseStopLockOrderProbe`).

The incomplete part (new required amendment D6.1-R3 in section 10): the
documents state the ambiguous path "retains `ReleaseDecisionKind.AmbiguousPersistence`"
under `_gate` as if retention were unconditional, but the R12 after-intent
`_gate` block starts with two prior rules - an existing retained decision
(120-123) and `_stopAccepted` (125-131). A stop can therefore win the
`_gate`/retention race between the `_journalGate` release and the ambiguous
path's retention acquisition, in which case the existing stop-won refusal
(consume + `Refused(MayHaveExecuted, "release gate refused: stop won during
durable intent")`) is what gets retained first, and the ambiguous caller
receives `Duplicate` of it, not the ambiguous decision. The D6.1 text does not
state this allowed outcome, so the acceptance plan's retained-ambiguity
assertion would fail in that scheduling. The window is journal-safe (a sealed
journal cannot authorize a second intent and the capability is consumed by the
winning rule), but the contract must name the collision resolution explicitly
before the human question finalizes.

Retention timing and the stop-winning linearization point are otherwise
precise: retention under `_gate` (no journal I/O) precedes the tail stop; the
tail stop's own consume at first acceptance under `_gate` is the C2
linearization; a stop win before retention consumes the capability exactly as
the existing gate block does.

**A-C1-2 (exact ambiguous decision kind, exhaustively accounted): VERIFIED
ADDRESSED** with one provenance defect (D6.1-R4).

- `ReleaseDecisionKind.AmbiguousPersistence` as a new explicit kind is
  consistent with the existing vocabulary: `ReleaseDecision`(Kind,
  `DurableRelease`, `Delivery`, `DurableCommitted`, `LiveExecutionExcluded`,
  Reason) and `DeliveryResult` (OwnershipRecord.cs:55-64) both support the
  proposed shape `MayHaveExecuted` + `DeliveryResult.NotAttempted` +
  `DurableCommitted=false` (`ReleaseDecision.cs:38-44`); `DeliveryResult`
  exists at OwnershipRecord.cs:55-64. The result does not imply a transport
  attempt: `NotAttempted` is the explicit value (used by every `Refused`
  helper path, `ReleaseArbiter.cs:301-303`), and the uncertainty is expressed
  about the durable release-intent record (both slots may hold Intent while the
  seal-unlink failed: `OwnershipJournal.cs:424-490`, ambiguity override at
  422-428, seal unlink failure at 486-490) - not about FD4 delivery.
- `Stopped` (value 3, `ReleaseDecision.cs:14`) has zero usages at R12 today;
  the record's non-reuse rationale ("a stop result, not a journal persistence
  result") stands.
- Exhaustiveness: at R12 the kind is consumed only by
  `ReleaseArbiter.cs:52/122/177/302` and R12 tests (`ReleaseArbiterTests.cs`,
  `Wp05AbortOracleFinalReviewerProbeTests.cs`, `Wp05ForgedAbortOracle.cs:60`
  (`Kind != Refused` check - reviewed in the sweep), `Wp05ForgedAbortOracleTests.cs`,
  `Wp05ForgedAbortOracleHostileProbeTests.cs`, `Wp05ReleasePathIndependentQualificationTests.cs`);
  there is no production `switch` over the kind and no serializer or persisted
  journal field consumes it (verified by `git grep` over the R12 tree). Adding
  an enum member is behavior-safe at compile-time; the record correctly requires
  reviewing every switch/pattern/log projection and test expectation.
- Defect: the named consumer inventory includes
  `Wp05ReleaseContractProbes`, which does not exist at the reviewed R12 tree;
  it exists only at `dc4c372` on `implementation/wp05-release-qualification-fixtures`
  (test-only; `src/` identical to R12; it asserts `Refused`/`NotCommitted` on
  exactly the wedge and failed-stop shapes the amendments invert). The
  inventory must state that provenance (D6.1-R4).
- The typed-ambiguity-flag proposal is implementable:
  `JournalIoResult` is a readonly record struct (`OwnershipJournal.cs:68`); an
  `Ambiguous` property set only for the post-content seal-unlink failure is a
  narrow, reviewable change, and the caller-side collapse at
  `ReleaseArbiter.cs:110-114` is the exact insertion point. Residual note: a
  torn/undecodable baseline inside `FailSealed` (`OwnershipJournal.cs:504-516`)
  can leave the seal present while returning a definite-shaped error; at R12
  this is unreachable for the arbiter's non-genesis intent path without a
  filesystem integrity violation, and Resolve/TryRecoverCommit still refuse
  correctly - acceptable as an observation, not a required change.

**A-C1-3 (capability disposition consistent with C2): VERIFIED ADDRESSED.**

The record states and correctly documents: transient live-while-wedged window
until stop acceptance; C2 consumption at stop acceptance; on stop-persistence
failure the capability stays consumed; on owner death the replacement gets no
capability and must recover from the sealed journal; no RELEASE resend. Source
corroboration: a sealed journal refuses every further forward path -
`WriteTransition` refuses at the uncertain-resolve check
(`OwnershipJournal.cs:567-573`), `ReadLatest` returns Uncertain (677-690), so
no second intent is possible (`TryPlanReleaseIntent` cannot be reached), and
`TryRecoverCommit` refuses while the live writer holds the fence (712-754).
`PreparedTransaction.TryConsume` is store-local (`PreparedTransaction.cs:60-69`),
so no lock hazard is added.

Required outcome for C1: **DESIGN SUPPORTED WITH REQUIRED AMENDMENTS**
(D6.1-R3, D6.1-R4).

### C2 - capability lifecycle

**A-C2-1 (consume on every required path): VERIFIED ADDRESSED.**

- Proposed insertion points verified against R12 lines: normal accept
  (`:263-267`), clock-unavailable (`:237-244`), deadline overflow (`:246-261`)
  - all inside the current `_gate` acceptance block, before returning the
  StopDecision; duplicate re-entry (`:232-235`) returns the retained stop
  without touching the consume path. Exactly-once at first acceptance is
  implementable exactly as stated.
- `TryConsume` is store-local (`PreparedTransaction.cs:60-69`;
  `PreparationStore.TryConsume` takes only the store's own lock), so placing it
  inside `_gate` cannot deadlock against `_journalGate` users - verified.
- Required paths audited against the proposal: normal accepted stop (consume),
  clock-unavailable accepted stop (consume; note R12's early-return at 243
  attempts no durable persistence - the proposal matches), deadline overflow
  (consume, early return as R12), duplicate stop (no second consume - retained
  path), stop after release consumed (consume attempt fails with
  `Consumed`/`Aborted`/`Foreign`; acceptance stands - matches
  `PreparationStore.TryConsume` violation values), stop after runtime commit
  or durable commit (`ReleaseAlreadyCommitted` sourced from
  `_releaseRuntimeCommitted` read inside `_gate` - consistent; no update of
  release history anywhere in RequestStop), failed durable StopRecord
  persistence (consume already happened under `_gate`; the persistence attempt
  occurs only after `_gate` release at 270-289 - so the ordering proposal is
  implementable and the capability cannot be restored by failure).
- The uniform linearization point (first runtime acceptance under `_gate`)
  is genuinely uniform across the three initial acceptance paths, and the
  duplicate path is strictly non-transitioning. A duplicate stop cannot become
  a second state transition: `_retainedStop` returns early at 232-235.

**A-C2-2 (release-first semantics remain valid): VERIFIED ADDRESSED.**

`_releaseRuntimeCommitted = true` is set after the gate consume and before the
transport write (`ReleaseArbiter.cs:157-175`, set at 158); the proposed
definition of `ReleaseAlreadyCommitted` ("runtime release path won and consumed
before this stop", explicitly NOT proof that RELEASE reached the coordinator or
that Committed was persisted) is exactly truthful for that source. A failed
later `TryConsume` (stop arriving after release-first) pairs with the
already-consumed state (`InvariantViolation.Consumed`); nothing in the proposed
text rewrites the retained release decision, delivery or Committed history.

**A-C2-3 (CE-03 out-of-model declaration): VERIFIED ADDRESSED.** The exact
declaration ("multi-instance window outside the accepted single-arbiter owner
model, closed by the capability contract, not composition") appears in the C2
record and reconciliation register; the two-arbiter construction is framed as a
deliberately synthetic diagnostic; no multi-instance production qualification
is claimed, and single-instance assumptions are not used to excuse the defect
(the capability store is independently the closure).

Required outcome for C2: **DESIGN SUPPORTED FOR HUMAN DECISION** (all three
amendments independently verified; testable via the existing store surface and
the inverted-assertion D2 fixtures).

### C3 - deadline and recovery semantics

**A-C3-1 (clock origin/epoch corroboration): PARTIALLY ADDRESSED.**

Source facts verified: `OwnerClock` derives durable transaction timestamps from
`Stopwatch.GetTimestamp()` with the exact conversion
`(t/Frequency)*1e9 + (t%Frequency)*1e9/Frequency`
(`OwnerPreparationTransaction.cs:21-32`); `LaunchDomainClock` uses the same
primitive and conversion via `MonotonicNowNanoseconds`
(`BubblewrapAdapter.cs:510-521`) and is epoch-compatible;
`OwnerDomainClock` (`OwnerPreparationTransaction.cs:34-39`) wraps
`Stopwatch.StartNew()` whose epoch is the construction instant - counted-value
differences from the transaction clock are essentially arbitrary - and it is
incompatible. (Also verified: `OwnerDomainClock` is dead code at R12 - declared
and never constructed - so the replacement is composition-facing for WP08.)

The proposed mechanism (a `MonotonicEpoch` marker with one production value
`Qpc`, a narrow `Epoch` member on `IDomainClock`, Reconciler accepting only
`Qpc`, test clocks explicitly declaring their epoch, incompatible epoch routed
to quarantine/Unknown, no new durable field, boot identity separately verified
by `RecoveryPlanner.Plan` refusal at `Reconciler.cs:75-97`) has the right
shape and rejects incompatible clocks before arithmetic, fails closed on
clock unavailability/overflow/invalid values and cross-boot (unchanged
RecoveryPlanner refusal; CrossBoot text preserved). Two required precision
amendments follow (D6.1-R2 in section 10):

1. **The enum does not prove the origin.** A `Stopwatch.StartNew()`-relative
   clock can be written to claim `Epoch = Qpc`; the D6.1 sentence "a
   `Stopwatch.StartNew()` relative clock cannot declare `Qpc`" is a review
   convention, not a structural guarantee. The smallest verifiable solution
   (recommended): the replacement production clock is a sealed internal class
   whose `Epoch` member is a constant in the class that itself implements the
   `Stopwatch.GetTimestamp()` conversion identical to `OwnerClock`
   (`OwnerPreparationTransaction.cs:23-31`) - no delegating factory, no injected
   epoch; and the corroboration test must assert behavior (an explicitly
   non-Qpc fixture clock is refused; a Qpc fixture works against StopRecords
   written through OwnerClock's conversion), not merely enum equality.
   Test-only clocks carrying an explicit epoch parameter remain a convention -
   acceptable because production code review plus the incompatible-clock
   fixture plus the rejection test make a false Qpc claim an intentional
   source change, which is what the contract must catch.
2. **Naming scope.** `MonotonicEpoch.Qpc` is Windows terminology. The contract
   must define it as "the `Stopwatch.GetTimestamp()` epoch with the exact
   nanosecond conversion of `OwnerClock`" and record that on Linux this is
   .NET's Stopwatch (monotonic, boot-anchored) - cross-process comparability
   derives from both parties using the same primitive with the same constant
   `Stopwatch.Frequency`, not from the QPC name. Do NOT accept literal
   cross-platform QPC comparability by assumption; require this explicit
   definitional scoping in the C3 text (a rename is not required; the scoping
   sentence is).

Overflow/invalid handling and no-fabrication rules are stated correctly
(derived-remaining arithmetic under `checked` semantics; watch-overflow and
`now <= 0` fail closed; never a fresh five-second window; no fabricated
interval on boot mismatch).

**A-C3-2 (reconciler outcome aggregation): PARTIALLY ADDRESSED** (see below
and D6.1-R1).

**A-C3-3 (no-record recovery does not manufacture a durable deadline):
VERIFIED ADDRESSED.** The proposal matches the source reality: a runtime-only
stop (clock-unavailable/overflow) persists nothing (RequestStop returns inside
`_gate` at 243/260 with no journal attempt); a failed durable attempt leaves no
StopRecord; a durable StopRecord cannot encode deadline <= 0
(`OwnershipValidation.cs:145-175` requires `acceptedAt > 0`,
`deadline > 0`, and `deadline == acceptedAt + 5_000_000_000` exactly). The
contract's cleanup-only allowance (explicitly not a stop deadline,
continuation or reset; C2 has consumed release authority; runtime-only state
is not described as durable; absence of a StopRecord grants no release
authority; no RELEASE resend; cleanup identity/authority still required via
`RecoveryPlanner.Plan` + `VerifyIdentity`) is coherent and testable
(`RuntimeAcceptedStopWithoutDurableRecordAfterRestart`). The composed
`AmbiguousWedgeThenClockUnavailableStop` case's recovery half specifies
conservative recovery consistently with the runtime half.

**A-C3-4 (already-empty behavior matches the selected proposed design):
VERIFIED ADDRESSED.** Option B (kill-first preserved; no additional
pre-observation phase; no unnecessary WAIT after emptiness is established)
matches today's source: with the `DomainStop` bit unset the Reconciler kills
before querying population (`Reconciler.cs:217-248`), and
`StopAndAwaitEmpty` with a zero-derived duration performs kill then a single
population read with the wait decision made before any sleep
(`DomainLease.cs:108-143`) - so "no unnecessary WAIT" is a true description,
and no D6.1 row contradicts another D6.1 contract row on this. Option B is a
proposed design resolution, correctly not presented as implemented behavior
(the acceptance row is explicitly rewritten, not claimed remediated).
Required outcome: verified.

**C3 remaining engineering issues - D6.1-R1 (required amendment):** the
A-C3-2 outcome vocabulary (Failed = genuine kill/backend operation failure or
failure-classified invariant; Unknown/quarantine = attempted-but-unconfirmed
stop, population observation unavailable, unconfirmed persistence; Refused =
absent/mismatched authority or identity; Confirmed = identity-verified empty
with persistence met) is correct and forbids a blanket Failed-to-Unknown
conversion. But the amendment does not specify the exact mechanism by which
the Reconciler distinguishes the cases it must distinguish: `StopOutcome`
(`DomainLease.cs:11-18`) collapses "kill write failed" and "population
observation unavailable" into `(Stopped=false, TimedOut=false)` with only
the Error string differing, and `StopAndAwaitEmpty` maps both to a plain
Failure (`:116-129`) - while the amendment itself bans error-string
classification. Until the mechanism is named (extend the stop outcome
record, or implement the zero-wait urgent stop as direct backend kill +
explicit `ObservePopulation` so outcomes map natively), the acceptance tests
in the matrix ("kill failure, delivered-but-populated, population
unknown/empty") cannot be written precisely, and the six-way
Failed/Unknown/Confirmed discrimination cannot be implemented distinctly.
Also, today's final-persistence-failure row maps the aggregate to
`ReconcileOutcome.Failed` (`Reconciler.cs:465-471`), which contradicts the
declared "persistence uncertainty after otherwise confirmed empty cleanup is
Unknown/quarantine" rule - the acceptance matrix must name that
JournalFinalization row change explicitly alongside the DomainStop mapping
change at 232-236 and the `finalizationFailed` aggregation at 429-436.

Expired-deadline policy feasibility review (policy NOT selected, verdict must
preserve the pending status): Policy A (kill + one immediate population
observation, no renewed wait interval) is mechanically supported exactly as
claimed - `StopAndAwaitEmpty` with timeout 0 issues the kill
(`DomainLease.cs:116-120`), derives deadline = now + 0, then performs one
population read; emptiness -> Success (no wait), populated ->
`StopOutcome.Timeout` before any sleep, unavailable observation ->
`StopOutcome.Failure` - exceptions and unavailable observations remain
distinguishable in shape; no fresh five-second interval is reachable; valid
cleanup authority and identity are still enforced upstream
(`RecoveryPlanner.Plan` refusal classes; `VerifyIdentity` at 224-228). The
kill path crosses the already-stated D6.1-R1 discrimination need (Timeout vs
Failure). Compared against Policy B (a separately specified bounded cleanup
interval with its own authority/bound), Policy A is the minimal mechanism
change and is technically supportable; the choice remains the human holder's.
The review preserves `C3 ZERO-WAIT POLICY - PENDING HUMAN`.

Required outcome for C3: **CONTRACT CHOICE REQUIRED** (policy pending) with
required engineering amendments D6.1-R1 and D6.1-R2. Findings separated per
dimension in section 6.

### C4 - duplicate and concurrent-release semantics

**A-C4-1 (refusal taxonomy and retention source-accurate): VERIFIED
ADDRESSED.**

Every listed reason string was checked verbatim against the R12 tree:

| Class | R12 verification |
|---|---|
| "release request refused: stop already won before durable intent" | `ReleaseArbiter.cs:59` exact; retained at 55-61 |
| "release request refused before durable intent: identity, deadline, prerequisite or capability check failed" | `:78` exact; pre-gate non-retained 64-79 |
| "release request refused: writer lease is no longer live" | `:86` exact |
| "release request refused: no acknowledged preparation snapshot ({latest.Error})" | `:93` exact shape |
| "release intent refused: acknowledged preparation identity does not match the capability" | `:99` exact |
| "release intent refused: {intentError}" | `:107`; structural `intentError` values exact: "release intent already exists" (`OwnershipTransitions.cs:23`) and "release intent is forbidden after a durable stop" (`OwnershipTransitions.cs:29`) |
| "release intent was not durably acknowledged" | `:113` exact |
| stop-winning gate refusal "release gate refused: stop won during durable intent" | `:130` exact; retained; |
| "release gate refused: expiry, prerequisite, liveness or capability check failed" | `:144` exact; retained |
| "release gate refused: capability consumption rejected ({violation})" | `:153` exact; retained |
| retained `Duplicate` relabel | `:52`/`:122` (`with { Kind = ReleaseDecisionKind.Duplicate }`) |

The semantic-outside/gate-vs-lock-placement distinction is stated (checks
inside `_journalGate` at 81-115 are semantically pre-gate), which prevents the
mislabeled taxonomy. No invented, normalized or paraphrased string is present.
No new `ConcurrentRefusal` kind and no waiting protocol is introduced -
consistent with the D6.0 concurrence.

**New ambiguous decision compatibility (Verified):** sequential Duplicate of a
retained `AmbiguousPersistence` rides the existing relabel (`with { Kind =
Duplicate }` keeps the retained disposition/delivery/durable fields, so
ambiguity is preserved, not flattened to NotCommitted); retention slot is
kind-agnostic; the enumerated consumers (no production switch; test files
listed) are correct; the one inventory provenance defect is D6.1-R4.

Required outcome for C4: **DESIGN SUPPORTED FOR HUMAN DECISION** - no separate
`ConcurrentRefusal`, no waiting protocol; no concrete consumer contradiction
was found.

## 4. C1 findings (consolidated)

- The proposed vocabulary is consistent with `ReleaseDecision`,
  `ReleaseDisposition`, `DeliveryResult` and the recovery classification
  (`ReleaseRecoveryClassifier` maps any present ReleaseRecord to
  `MayHaveExecuted`, `ReleaseRecoveryClassification.cs:~36-40`), and the
  result does not imply an FD4 attempt (`DeliveryResult.NotAttempted`).
- Lock-order implementability verified as in section 3 (A-C1-1); the
  remaining gap is the stop-won ordering rule (D6.1-R3).
- Concurrent-caller behavior during the wedge: a same-instance caller in the
  wedge window receives `Refused(NotCommitted, "...no acknowledged preparation
  snapshot (...commit seal present...)")` before retention and `Duplicate` of
  the ambiguous result after retention; both are structurally safe (no second
  intent; no FD4). Not a forbidden effect; it must be covered by the
  retention test's expectations (fold into D6.1-R3's test row).
- Retention timing and stop-winning linearization are otherwise correctly
  specified.
- C1 classification: Ready for human decision only after the D6.1-R3 (and
  D6.1-R4 documentation-precision) amendments are folded.

## 5. C2 findings (consolidated)

- Implementable, consistent with single-use capability semantics, consistent
  with C1, testable with existing D2 barriers (inverted `IsLive` fixtures on
  the three stop-acceptance paths plus the second-arbiter probe), and ready
  for individual human approval.
- No contradiction with release-first; `ReleaseAlreadyCommitted` is not
  treated as delivery proof anywhere in the revised text.

## 6. C3 findings (consolidated, per dimension)

1. **Epoch corroboration:** source facts correct; mechanism directionally
   correct; needs the construction-discipline and definitional-scoping
   amendments (D6.1-R2). An implementation must not be able to claim `Qpc`
   while sourcing construction-relative values unnoticed.
2. **Deadline derivation:** `remaining = max(0, deadline - corroborated now)`
   with checked arithmetic, fail-closed on unavailable/overflow/invalid clock
   and boot mismatch, repeated reconciliation reusing the same absolute
   deadline, partial cleanup never resetting - all consistent with the
   immutable validated record (`StopRecord` deadline == acceptedAt + 5s,
   `OwnershipValidation.cs:145-175`). No path resets the deadline; caller
   allowance survives only for the no-record cleanup-only path and must not
   be misrepresented as the original durable stop allowance (stated).
3. **Expired-deadline policy feasibility:** Policy A is technically
   supportable and coherent with `StopAndAwaitEmpty` (verified kill + single
   observation at zero timeout; exceptions/unavailable observations
   distinguishable in shape; no unintended fresh wait; cleanup authority and
   identity still required). Policy B remains the stated alternative. The
   review does NOT select; `C3 ZERO-WAIT POLICY - PENDING HUMAN` preserved.
4. **Reconciler aggregation:** correct intent; requires the explicit
   discrimination mechanism and the named finalization row change (D6.1-R1).
   A test plan must prove component outcomes AND aggregate outcomes for the
   six cases (kill failure; kill-attempted-populated; kill-uncertain;
   observation unavailable; positively-empty; stop-evidence persistence
   failure; plus the unaffected Refused/Confirmed classes).
5. **No-record restart:** verified (section 3, A-C3-3).
6. **Already-empty behavior:** verified Option B consistency (section 3,
   A-C3-4).
7. **Tests and implementation prerequisites:** an explicitly incompatible
   clock fixture (a relative-epoch clock that honestly declares its epoch)
   and actual aggregation-outcome tests are required; found present in the
   test list except for the mechanism-blocked aggregation precision
   (D6.1-R1). Deterministic controllable-clock fixtures exist at R12
   (`BarrierClock`, `QualificationClock`, `RealClock`) and the D2 R27 probe
   fixture pattern is reusable; no sleeps-as-oracle addition is proposed.

## 7. C4 findings (consolidated)

- Retained contract matches R12 exactly (winner/Refused/Duplicate;
  non-retained pre-gate vs retained gate; one physical RELEASE; one-shot
  channel `HeldExecution.TryWriteRelease` via `Interlocked.Exchange`,
  `HeldExecution.cs:45-60`).
- Documentation consistently distinguishes runtime gate, journal lock, durable
  Intent boundary, retained decision, retryable pre-gate refusal, and
  nonretryable structural refusal; and correctly prevents
  lock-placement-based misclassification.
- `AmbiguousPersistence` compatibility verified across sequential Duplicate,
  retention, logging projections (none exist to amend beyond the enumerated
  consumers), and exhaustiveness.
- Ready for human approval without a new enum value or waiting protocol.

## 8. Cross-contract findings

Six adversarial interactions were re-run independently (full traces in the
interaction register):

| Pair | Verdict |
|---|---|
| C1 x C2 | Supported. The wedge consumes the capability at first stop acceptance; the journal seals out any second intent; duplicate stops are non-transitioning. One residual specified via D6.1-R3 (stop-won ordering at retention). |
| C1 x C3 | Supported. No fabricated deadline: the wedge either yields a durable StopRecord (then deadline arithmetic, not a new window) or yields no record (cleanup-only allowance). No unbounded stop: objective bounds are the runtime deadline (advisory) and C3's bounded cleanup. |
| C1 x C4 | Supported. Retained ambiguity cannot be converted by any caller's retry into a physical second write; pre-retention refusals are structural (sealed journal). |
| C2 x C3 | Supported. Capability closure is independent of clock validity and persistence; C3 never reactivates release authority (RecoveryPlanner refuses; no capability path from recovery). |
| C2 x C4 | Supported. Stop-first yields a retained `MayHaveExecuted` gate refusal with consumed capability and no FD4; release-first preserves the release and marks `ReleaseAlreadyCommitted` without claiming delivery. |
| C3 x C4 | Supported. Duplicate/reconcile cannot reset the deadline (validator-immutability + Reconciler writes only cleanup bits). |

One new interaction-level rule was required and is captured as D6.1-R3 (C1
retention versus concurrently-won stop ordering); no additional runtime
coordination rule beyond it was found to be missing. No new durable record is
introduced anywhere to patch a source-level inconsistency.

## 9. Evidence and qualification boundaries

- D6.1 adds no implementation, no approval, no qualification claim; the
  handoff/queue entries say "DESIGN TEXT ADDRESSED, not production
  remediated", "no new tests or target run, no gate accepted" - verified
  accurate against the diff.
- The D2 probe evidence (counterexample probes at `dc4c372`) binds to R12:
  the fixture branch's `src/` subtree is byte-identical to R12 `38f6981`;
  D4/D5 rows remain frozen-harness/Q3-bound historical evidence. None of this
  qualifies any future changed production code; the evidence/gate matrix and
  remediation proposal correctly re-run everything post-remediation with new
  tree binding.
- `ReleaseArbiter` and `Reconciler` have zero production call sites at R12
  (verified by construction-site grep; all `new ReleaseArbiter`/`new
  Reconciler` occurrences are in `tests/`). Correct component-only boundary;
  WP08 owner orchestration, WP10 status/target integration, D5-O05 (H1) and
  full G3 qualification remain deferred and are not claimed.
- H1-H5 remain preserved and open: H1 (D5-O05 zero-caller status integration),
  H2 (destructive descriptor/wrong-FD/helper-writer cases unclaimed), H3
  (optional raw SIGKILL/wait4/pidfd witness), H4 (C1-C4 open until human
  approval), H5 (D5 document/commit ordering note). The D6.1 amendments
  contain no closure of any H item.
- C1-C4 implementation status at D6.1: none (no such code exists on any
  branch reviewed); the review forbids treating the documents as code. Verified.

## 10. New risks or contradictions (required changes from this review)

- **D6.1-R1 (C3, required):** Name the exact discrimination mechanism for the
  aggregated stop outcomes (Failed vs Unknown vs Confirmed, and the
  population-unavailable case), because `StopOutcome` cannot express it
  natively; name the JournalFinalization aggregate change (Failed -> Unknown
  on persistence uncertainty after confirmed-empty cleanup) explicitly in the
  acceptance rows; forbid error-string classification
  (`Reconciler.cs:232-236`, `:429-436`, `:450-459`, `:465-471`;
  `DomainLease.cs:11-18,108-143`).
- **D6.1-R2 (C3, required):** Bind `MonotonicEpoch.Qpc` to the concrete
  primitive (sealed production clock implementing OwnerClock's exact
  `Stopwatch.GetTimestamp()` conversion; constant epoch in the implementing
  class; behavior-based corroboration test with an explicitly non-Qpc fixture)
  and add the definitional scoping sentence (the marker names the
  Stopwatch.GetTimestamp() epoch; on Windows this is QPC and on Linux it is
  .NET's CLOCK_MONOTONIC-backed Stopwatch; cross-process comparability follows
  the shared primitive and constant Frequency, not the name; no
  cross-platform qualification follows from the label).
- **D6.1-R3 (C1, required):** Specify the retention-collision rule: between
  `_journalGate` release and the ambiguous retention acquisition, an
  already-retained decision or an already-accepted stop wins the standard
  first-retainer rules (`ReleaseArbiter.cs:120-131`), the ambiguous caller may
  receive `Duplicate` of that retained result, and the tail stop remains
  idempotent; extend the opposing-order probe to assert these expectations
  explicitly.
- **D6.1-R4 (documentation precision, required):** In the C1 consumer
  inventory, mark `Wp05ReleaseContractProbes` as a test-only fixture-branch
  asset (`implementation/wp05-release-qualification-fixtures` at `dc4c372`,
  `src/` identical to R12), so the inventory binding is exact. Optional
  refinement: state in the C4 taxonomy that the sealed-journal
  "no acknowledged preparation snapshot" refusal is retry-futile for a live
  owner (never authorizes a second intent), distinct from a
  pre-history missing-snapshot refusal.

None of these is structural: no contract must be redesigned; each is a precise
text amendment bound to verified source lines.

## 11. Contract-level verdicts

| Contract | Verdict (this review) |
|---|---|
| C1 | `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS` (D6.1-R3 lock-order/retention precision; D6.1-R4 inventory binding) |
| C2 | `DESIGN SUPPORTED FOR HUMAN DECISION` |
| C3 | `CONTRACT CHOICE REQUIRED` (expired-deadline policy PENDING HUMAN) with required engineering amendments D6.1-R1, D6.1-R2 |
| C4 | `DESIGN SUPPORTED FOR HUMAN DECISION` |

## 12. Overall disposition

**`WP05 D6.1 INDEPENDENT DESIGN REVIEW COMPLETE - OPTION B: DESIGN SUPPORTED
WITH SPECIFIC REQUIRED AMENDMENTS; HUMAN CONTRACT DECISIONS HELD; NO PRODUCTION
REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED`**

The D6.1 package genuinely resolves the D6.0 review findings in substance
(vocabulary, consume placements, release-first, CE-03, no-record rule,
already-empty row, taxonomy strings, package bindings, component boundaries),
with three technically required precision amendments (D6.1-R1, D6.1-R2,
D6.1-R3) and one binding-precision amendment (D6.1-R4). The C3
expired-deadline policy remains pending; no policy is selected by this review;
no contract is approved; no gate is accepted; release remains unqualified.
Recommended next action: one narrowly scoped documentation revision folding
D6.1-R1..R4, followed by a focused re-review of those rows only, while C2/C4
remain approval-ready in the interim.
