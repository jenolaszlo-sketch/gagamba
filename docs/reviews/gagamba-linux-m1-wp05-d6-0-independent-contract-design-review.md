# WP05 D6.0 Independent Contract Design Review - C1-C4 Release Contract Proposals

Author: GLM (independent adversarial design reviewer for the WP05 release
decision holder). This is an independent review of the D6.0 producer package,
performed against the frozen R12 production source and the independently
published D1-D5 evidence chain. It design-supports or amends proposals; it
does not approve contracts, authorize code changes, authorize remediation,
accept a gate, or claim release qualification.

Status: **REVIEW COMPLETE - DISPOSITION RECORDED IN THIS DOCUMENT**

Date: 2026-10-20

## 1. Scope, authority and disposition options

Reviewed work: the D6.0 producer package authored on
`analysis/wp05-d6-0-release-contract-decisions` (commits 726e081b1b9bac9037f323558a91e62d67a75163,
1e0f126b4617ba5b58f01258278a6dd78c19a63c, 23df1c566e7d8f072d924f374bf6891c726c03fe),
proposing release contracts C1-C4 from the original required-changes document
(blob 73eb09af27c5dbc896739ebf48489862ec13e0a9).

Review authority and exclusions:

- Documentation-only. No production, test, native or schema change was made in
  this review; no contract text in the D6.0 package was edited.
- Per-contract verdicts are independent. A verdict for one contract does not
  imply any other verdict.
- Approval of each contract remains with the human WP05 release decision
  holder. This review is advisory support for that approval, exactly as the
  producer package itself requests.
- No target execution, no production composition and no authorization of the
  remediation tranches follows from this review.

Independent per-contract verdict vocabulary (fixed before analysis):

- `DESIGN SUPPORTED FOR HUMAN DECISION`: the proposal is structurally sound;
  the reviewer recommends placing the exact producer decision question before
  the human without required changes.
- `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS`: the proposal is structurally
  sound but specific design-text amendments are required before the proposal
  is approval-ready; they are listed as named required amendments.
- `CONTRACT CHOICE REQUIRED`: the producer package itself leaves a normative
  policy choice open that the human must make (the reviewer does not answer
  it), and the design text also carries required-amendment items.
- `BLOCKING DESIGN DEFECT`: the proposal structure is unsafe as stated; a
  structural redesign is required before any human approval.

Overall disposition options (mirroring the established D-series review
convention):

- Option A: review complete, package supported, contracts approved.
- Option B: review complete, package analysis independently supported with
  required amendments; no contract approved; human approval pending.
- Option C: review complete, package found defective beyond amendment.
- Option D: review blocked.

This review records: **Option B**.

## 2. Materials audited

Audited in full:

- The nine D6.0 producer documents (decision proposals, C1, C2, C3, C4,
  evidence-and-gate matrix, remediation proposal, deferred integration and
  handoff, executive decision summary).
- The original adversarial review chain (commits 6838f92 "adversarially
  review the release qualification plan", bca1b88 counterexample register,
  e4b0a79 "bind required changes to the plan review findings"), in particular
  the required-changes document C1-C9 rows (blob
  73eb09af27c5dbc896739ebf48489862ec13e0a9 on the published branch
  `review/wp05-release-plan-adversarial-review`).
- R12 production source files, symbol-verified line by line:
  - `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` (335 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs` (843 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipTransitions.cs` (270 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipRecord.cs` (689 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/ReleaseRecoveryClassification.cs` (114 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs` (475 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipValidation.cs` (stop-record rules, lines 37-175, 379-380, 466-565)
  - `src/Gagamba.Execution.Linux.M1/Preparation/PreparedTransaction.cs` (77 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs` (lines 1-209: StopOutcome, IDomainClock, StopAndAwaitEmpty)
  - `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs` (lines 1-80: both production clock implementations)
  - `src/Gagamba.Execution.Linux.M1/Launch/HeldExecution.cs` (lines 1-90)
  - `src/Gagamba.Execution.Linux.M1/Execution/ReleaseDecision.cs` (54 lines, full)
  - `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs` (lines 1-140: ITransactionClock definition)
  - `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs` (lines 510-521: LaunchDomainClock)
- Test evidence seams cited by the producer package:
  - `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` (lines 1-330: all five cited D2 probes plus stop probes)
  - `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs` (names D4_6, D4_9 verified)
- The published D4/D5 independent review layers (preservation checkpoints in
  the audited tree plus this reviewer's own D5 reruns preserved at
  578b2084cfa09be76e73d0fb26e09f8d823f9ae6).

Also verified by repository queries: symbol-level absence of production
composition (see 4.9) and the reachability structure of the binding blobs
(see 3.3).

## 3. Binding verification of the D6.0 producer package

### 3.1 Commit-chain binding

Resolved exactly, in the canonical repository:

- Base: 578b2084cfa09be76e73d0fb26e09f8d823f9ae6 ("docs(wp05): preserve
  independently reproduced D5 native status review") - the preservation
  commit of the D5 independent review, whose publication to origin was
  independently verified in the D5 review task (ls-remote, fresh bare clone,
  fsck clean, ancestry held).
- Commit A (726e081b...): "docs(wp05): reconstruct D6 release contract gaps".
- Commit B (1e0f126b...): "docs(wp05): propose C1-C4 release contracts".
- Commit C (23df1c566e7d8f072d924f374bf6891c726c03fe): "docs(wp05): prepare
  D6 contract review and remediation plan"; branch tip of
  `analysis/wp05-d6-0-release-contract-decisions`.
- Ancestry: 578b2084 -> 726e081 -> 1e0f126 -> 23df1c5, linear, no null
  commits, verified by `git log 578b2084..23df1c5`.

### 3.2 Documentation-only proof

Diff 578b2084..23df1c5 touches exactly nine new files, 1260 insertions, 0
deletions, all documentation: none under src/, native/, artifacts/, tests/
or eng/. Every document carries a PROPOSED/NOT APPROVED or NOT AUTHORIZED
status. The package therefore satisfies the standing D-series constraint
that proposals are records, never authorizations.

### 3.3 Binding precision of the original requirements

The evidence-and-gate matrix binds "original required changes blob
73eb09af...". Verified precisely:

- blob 73eb09af27c5dbc896739ebf48489862ec13e0a9 is the required-changes
  document, created at commit e4b0a79 (branch tip of the published
  `review/wp05-release-plan-adversarial-review`, local and origin).
- e4b0a79 is NOT an ancestor of the D6.0 tip 23df1c5 (and not of a4fca00):
  the D5 lineage replaced the three adversarial-review work files with the
  preservation checkpoint
  `docs/reviews/gagamba-linux-m1-wp05-release-plan-adversarial-preservation-checkpoint.md`
  (blob 9aa7f8c9...), which records the original blob identities
  (adversarial review f1a062e1..., counterexample register 0bbafa6b...,
  required changes 73eb09af...) rather than embedding their full text.
- Consequence: the C1-C4 requirement source is authentic and published, but
  a reader of the D6.0 package alone must fetch the adversarial-review
  branch to see the full original text. This is acceptable because the blob
  id is published on origin and the preservation checkpoint binds it, but it
  is recorded here as a binding precision note (package-level amendment
  A-PKG-1 in the required-changes document).

### 3.4 R12 source binding

The producer package binds R12 38f6981... / tree 6854675d... . Verified: the
audited src tree in the D6.0 worktree is unchanged from R12 - no commit in
578b2084..23df1c5 touches src or tests (3.2), and the producer-declared
previous production baseline 58cb1ba31c102e0e9d5488721e8c438adb55c614 is a
verified ancestor of the audited D5 tip a4fca00 (ancestry check performed in
this review, not taken from the package). All src-side line references in
this review therefore bind the same R12 source the producer package binds.

## 4. Independently confirmed R12 source baseline

The facts below were reaffirmed by direct symbol reading in this review,
not taken from the producer package. Line numbers are R12 (verified in the
audited tree, unchanged from R12).

### 4.1 Release request path (`ReleaseArbiter.RequestRelease`, lines 45-224)

- Entry retention block under `_gate` (48-62): retained decision relabeled
  `Duplicate` (50-53); `_stopAccepted` pre-intent refusal retained (55-61).
- Pre-durable-intent checks (64-79): clock, request deadlines, prerequisites,
  `IsLive`, identity equal, manifest digest; failure returns non-retained
  `Refused(NotCommitted)`.
- Journal block under `_journalGate` (81-115): writer liveness (83-87), latest
  snapshot read (89-94, non-retained refusal on uncertain), preparation
  matching (96-100), `TryPlanReleaseIntent` (102-108, non-retained refusal on
  refusal reasons including the already-existing-intent reason) and the
  collapsed write-failure refusal (110-114): any `WriteTransition` failure is
  reported as the definite `Refused(NotCommitted, "release intent was not
  durably acknowledged")`, with the capability left live. This is the
  factual core of C1.
- Post-intent gate under `_gate` (117-182): retained re-entry (120-123),
  stop-won refusal with `ConsumeCapability()` (125-131), gate recheck
  refusal with `ConsumeCapability()` (134-146), `TryConsume` refusal
  (148-155), runtime commit flag set before transport (158), exactly one
  transport attempt with exception mapping to `Unknown` (162-169), delivery
  mapping and live-exclusion derivation (172-175), retained `Released`
  decision with `MayHaveExecuted`, `DurableCommitted: false` (176-182).
- Committed-evidence write outside `_gate` (185-223): WriteMutation under
  `_journalGate` (194-211); the retained decision is amended with the
  durable flag (215-223).

### 4.2 Stop request path (`ReleaseArbiter.RequestStop`, lines 226-297)

- Three acceptance paths, none touching the capability: retained duplicate
  (232-235), clock-unavailable (237-243: accepted, acceptedAt 0, deadline 0,
  no durable stop), overflow (246-260: accepted, acceptedAt now, deadline 0,
  no durable stop), normal (263-267: `_stopAccepted = true`, deadline =
  now + StopConfirmationAllowanceNanoseconds).
- Durable attempt outside `_gate` (270-289): `WriteMutation(TryPlanStop)`;
  failure appends reason text, `_stopAccepted` and the retained stop stand
  regardless of durability.
- `_stopAccepted` (line 24) is the runtime winner channel. No `TryConsume`
  occurs anywhere in `RequestStop`. This is the factual core of C2.

### 4.3 Journal two-slot seal protocol (`OwnershipJournal`)

- `Write` (358-495): lease/fence checks (361-369), sequence check (371-387),
  re-resolve under the fence (399-416; a present seal fails the write as
  lineage-uncertain BEFORE any new seal content), scratch unlink
  (418-420), exclusive seal creation (424-428), seal fsync file+dir
  (430-440), both slots staged fully and fsynced in order SlotB then SlotA
  (446-477), then seal unlink. Unlink failure returns failure with
  "seal release failed, commit ambiguous" (486-489) after both slots may
  already hold the new generation. This is the factual core of CE-02/C1.
- `FailSealed` (504-519): pre-ack knowledge cases deliberately restore the
  acknowledged baseline by unsealing only when the baseline still decodes
  from a slot; otherwise (genesis, unreadable slots, higher generation
  only) the seal stays and readers report uncertainty.
- `WriteTransition` (550-614) and `WriteMutation` (620-675): exact-baseline
  checks against `Resolve`, validator application, single-sequence advance.
  On a present seal, both fail closed (568-573, 636-641) without touching
  content.
- `TryRecoverCommit` (712-771): takes the exclusive fence (refusing while a
  live writer holds it), requires a seal, unlinks temp, classification:
  both slots decodable -> acknowledge lower sequence; none -> genesis
  acknowledge nothing; lone/torn slot -> refuse, seal stays ("operator
  resolution required", 747-748).
- `Resolve` (773-819): present seal -> `Uncertain` regardless of content
  (773-781); unsealed both-equal -> acknowledged; unsealed unequal ->
  older acknowledged, newer treated as uncommitted candidate (801-816);
  otherwise Uncertain (818).

### 4.4 Transition planners and validator (`OwnershipTransitions`, `OwnershipValidation`)

- `TryPlanReleaseIntent` (10-64): refuses when Release exists ("release
  intent already exists", 21-25), when durable Stop exists ("release intent
  is forbidden after a durable stop", 27-31), after observations, and
  without a Prepared snapshot.
- `TryPlanReleaseCommitted` (66-106): requires prior durable Intent; does
  NOT refuse on the presence of a StopRecord (stop-then-committed and
  committed-then-stop are both durable-legal, one after the other).
- `TryPlanStop` (108-151): refuses when Stop exists ("the first accepted
  stop is immutable", 118-122), requires acceptedAt > 0, computes deadline
  = acceptedAt + StopConfirmationAllowanceNanoseconds (5,000,000,000,
  `OwnershipRecord.cs:202`) with overflow refusal.
- Validator (`OwnershipValidation.cs`): a durable StopRecord must have
  positive accepted/deadline values (148) and the deadline must equal
  acceptedAt + 5,000,000,000 exactly (165-175); the record is immutable in
  transitions (466, 504, 565). The producer's claim that the deadline
  cannot be rewritten is structurally true.

### 4.5 Capability (`PreparedTransaction`)

- `IsLive` (52-55) is a pure store lookup; runtime stop does not change it.
  `TryConsume` (60-69) is store-owned single-use; the store invalidates
  permanently on consume or dispose. After a `RequestStop` acceptance the
  capability remains live - verified by the D2 probes
  `StopFirstLeavesCapabilityLiveOnR12` (183-195) and
  `StopPersistenceFailureAllowsSecondArbiterReleaseOnR12` (215-233).

### 4.6 Recovery classification (`ReleaseRecoveryClassification`)

- A durable snapshot with Release present (Intent or Committed) classifies
  `MayHaveExecuted` (36-38); Release absent classifies `NotCommitted`
  unless the stop cause implies cancellation. Intent-only (not Committed)
  yields aggregate `ExecutionOutcome.Unknown` (80-83). Recovery is already
  conservative about a durable Intent.

### 4.7 Reconciler and stop semantics (`Reconciler`, `DomainLease`)

- `Reconciler` holds a constructor-supplied `TimeSpan _stopAllowance`
  (124-130) and passes it to `StopAndAwaitEmpty` (231); the durable
  StopRecord deadline is never read by `Reconciler`. Recovery planning
  refuses cross-boot (RecoveryPlanner.Plan 80-83) and quarantines when no
  live authority survives (85-88).
- `StopAndAwaitEmpty` (`DomainLease.cs:108-143`) writes the kill FIRST
  (116), then polls population every 25 ms (123-142) against
  `clock.Now + timeout`; a zero timeout therefore still performs the kill
  and then exactly one population observation. Scheduling granularity is
  the clock's Delay; failure/timeout are distinct outcomes.
- `Reconciler` maps an unconfirmed stop to `Failed` (234-235) and its
  aggregation yields `ReconcileOutcome.Failed` (429-436), not
  Unknown/quarantine, in the current code.

### 4.8 Cited probe inventory verified

All five D2 probes exist at the producer-cited names in
`Wp05ReleaseContractProbes.cs`: AmbiguousSealFailureLeavesUncertainIntentAndNoStop (135),
DefiniteIntentFailureIsDistinctFromAmbiguousPersistence (166),
StopPersistenceFailureAllowsSecondArbiterReleaseOnR12 (215),
ConcurrentDuplicateReleaseProducesOnePhysicalWrite (236),
ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired (268), plus
StopWithUnavailableClockIsLiveAcceptedWithoutDurableStop (98),
StopDeadlineOverflowIsLiveAcceptedWithoutDurableStop (118),
StopFirstLeavesCapabilityLiveOnR12 (183),
DurableStopBlocksReleaseIntentFromSecondArbiter (198). The D4 supervisor
names D4_6 and D4_9 exist. FakeJournalStorage.FailAt provides the exact
fail-point plumbing (unlink counter) that the original C6 requirement
demanded; the ambiguous probe uses the second unlink, i.e. the
post-content seal release.

### 4.9 Component composition status (scope fact)

`ReleaseArbiter` and `Reconciler` appear only in their own definition files
inside src/; no production file constructs either. Both are
component-level, exercised by test hosts only (D2 probes, D4 supervisor
and owner-host test code). The owner runtime's live abort path
(`OwnerPreparationTransaction.cs:388-402`) stops the domain through the
protocol channel and does not route through `ReleaseArbiter`/`Reconciler`.
Consequences, recorded as package-level amendment A-PKG-2 in the
required-changes document:

- The C1/C2/C4 "production scope" statements name files whose runtime
  behaviors will pre-date any production composition. The contracts change
  currently-uncomposed components; first production composition (the WP08
  boundary) must be built against the revised contracts.
- No R12 production behavior changes by adopting C1-C4 until composition
  exists. This strengthens the safety argument for adopting the contracts
  early, and it also means gate claims from these contracts must remain
  component-level until composition.

## 5. C1 - ambiguous release-intent persistence (R09)

### 5.1 Original requirement

Required-changes C1 (blob 73eb09af...): distinguish definite failures from
ambiguous seal-release failures at the arbiter boundary; in the ambiguous
case report a state other than definite NotCommitted and request a bounded
stop; define the wedge-case stop mechanics (runtime boundary + runtime
deadline; durable evidence best-effort; orphan cleanup as the recovery
backstop). Binds Q06; blocks G3-A11 and G3-B08.

### 5.2 Verified defect analysis

Confirmed independently: `ReleaseArbiter.cs:110-114` collapses every
`WriteTransition` failure - including the post-content cases in which both
journal slots may already durably hold the Intent (`OwnershipJournal.cs:424-489`
staged both slots before the seal-unlink failure at 486-489) - into
definite `Refused(NotCommitted)`, leaves the capability live, and requests
no stop. The wedge persists: with a present seal, `ReadLatest` returns
uncertain on every later call, `WriteMutation` fails closed for a durable
stop (636-641), and a live writer blocks `TryRecoverCommit` (716-721). The
D2 ambiguous probe reproduces the shape (135-163), the D4_6 probe reproduces
it across a fresh process, and recovery classification yields
`MayHaveExecuted` once the sealed Intent is recovered. All of this matches
the producer package's account.

### 5.3 Proposal analysis

The proposed C1 contract - explicit ambiguous decision with
`MayHaveExecuted`, no FD4, bounded runtime stop with best-effort durable
stop, retained ambiguous result, conservative recovery - was independently
assessed against the source:

- The runtime channel is real: `_stopAccepted` is journal-independent and
  the retained decision slots are separate per decision type, so a request
  that reached the ambiguous point can set stop state without further
  journal I/O. `RequestStop`'s durable attempt fails fast and harmlessly on
  a wedged journal (`WriteMutation` refuses at the `Resolve` check,
  636-641, before creating any new seal content), so the proposed
  best-effort durable stop inside the ambiguous path adds no new wedge
  risk. Verified.
- A runtime stop accepted while the journal is wedged does leave the
  journal stuck (seal present until process death or operator recovery).
  The producer package states the orphan-cleanup backstop accordingly, and
  the D5-layer recovery mechanics agree. The physical release path stays
  blocked throughout (no acknowledged baseline is derivable from a sealed
  journal; every release path requires `WriteTransition` success), so the
  injury remains evidential, as the original required-changes severity note
  records.
- `MayHaveExecuted` disposition already exists and is the same vocabulary
  the recovery classifier uses for a recovered durable Intent (36-38),
  making the runtime and recovery vocabularies consistent.
- Retention semantics ("later callers receive Duplicate of the ambiguous
  result and cannot retry") are compatible with the R12 retention map:
  retention happens after durable intent, which the ambiguous case is past.

### 5.4 Required amendments

- A-C1-1 (stop invocation boundary): the proposal does not state where
  `RequestStop` is invoked from inside the ambiguous path. In R12 shape,
  `RequestRelease` releases `_journalGate` at 115 and takes `_gate` at
  118; `RequestStop` takes `_gate` at 230 and `_journalGate` at 272. If the
  ambiguous-path stop were invoked while either lock is held on the same
  thread, the lock nesting inverts the established order (the file header
  invariant "no journal I/O occurs while the arbitration gate is held",
  lines 1-5, and the two-phase structure of both entry points). The design
  text must fix the invocation point: after all locks of the failing
  request path are released (tail-of-call semantics), with the stop state
  mutation under `_gate` kept minimal. C# monitor re-entrancy would mask a
  same-thread bug, so the acceptance tests must place a concurrent
  transport/committed writer at the opposing order and assert no
  deadlock/serialization inversion.
- A-C1-2 (exact decision vocabulary): `ReleaseDecisionKind` already contains
  an unused `Stopped` value (3) and `ReleaseDecisionKind.Stopped` has zero
  usages in the repository. The design text must name the exact retained
  ambiguous decision (new kind or pickup of `Stopped`) and require
  exhaustiveness verification for all `ReleaseDecisionKind` switches
  before approval. The producer package notes this ("C1 may add the
  explicit ambiguous kind"); this review promotes it from note to required
  amendment because retention (`Duplicate of the ambiguous result`) is
  typed through the kind.
- A-C1-3 (capability invalidation in the ambiguous path): between the
  ambiguous failure and the stop request the capability remains live while
  the journal is permanently wedged; a second arbiter in the same process
  could replay the same wedge. C1 must state, in contract terms, that the
  ambiguous path relies on the C2-derived stop acceptance to consume the
  capability, and must either (a) accept the transient live-while-wedged
  window as closed by C2 at stop acceptance, or (b) consume the capability
  directly at the ambiguous point. The reviewer's assessment: (a) is
  sufficient and simpler because the wedged journal blocks every other
  release path anyway (no acknowledged baseline is reachable); the window
  must nevertheless be documented exactly as (a) proposes.

### 5.5 Verdict

**C1 verdict: `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS`** (A-C1-1,
A-C1-2, A-C1-3). The proposal is the correct R09 resolution and direction;
the required amendments are design-text precision items, not structural
objections.

## 6. C2 - stop-first capability invalidation (R08)

### 6.1 Original requirement

Required-changes C2: consume/invalidate the capability at runtime stop
acceptance (or at first post-stop refusal, explicitly chosen); assert
`IsLive == false` after stop-first; declare the CE-03 multi-instance window
outside the accepted single-arbiter model, closed by the capability
contract. Binds Q05; blocks G3-A02, G3-A07.

### 6.2 Verified defect analysis

Confirmed independently: none of the three acceptance paths in
`RequestStop` touches the capability (226-297 contains no `_capability`
reference), while `IsLive` is a pure store lookup (`PreparedTransaction.cs:
52-55`); the D2 probes verify both the durable-stop case (183-195) and the
failed-durable-stop case (215-233; a second arbiter over the same live
capability then releases with a full transport attempt and a durable
Committed record). The proposed consume-at-acceptance choice is the
stronger of the two original options and is consistent with the store's
 single-use semantics; the first-post-stop-refusal alternative is
unnecessarily weaker because R12 has two refusal sites after stop
acceptance (entry 55-61, gate 125-131) and only the gate site already
consumes.

### 6.3 Proposal analysis

Assessed against the source:

- All three acceptance paths (normal, clock-unavailable, overflow) must
  consume; the D2 probes for the latter two paths exist
  (StopWithUnavailableClock..., StopDeadlineOverflow..., 98-132) and assert
  `IsLive` is live today; they become direct regression fixtures for the
  inverted assertion. Verified feasible with the existing store API.
- Stop-after-release-first: `TryConsume` refuses with
  `InvariantViolation.Consumed`; the retained release record stands; the
  stop's `ReleaseAlreadyCommitted` field is sourced from
  `_releaseRuntimeCommitted` (only set after the gate consumed the
  capability, 158). Verified semantics: consumption by stop can erase
  nothing; the release's own consume already happened. Timing nuance
  verified: `_releaseRuntimeCommitted` is set before the transport write,
  so a stop arriving between consume and transport reports
  `ReleaseAlreadyCommitted == true` while the transport result may be
  Unknown - the conservative reading is correct.
- Design text ordering: consumption must occur under `_gate` at acceptance
  (inside the existing acceptance blocks) so that no new lock nesting is
  introduced; `TryConsume` is store-local and takes no journal lock
  (verified: `PreparedTransaction` touches only `_store`), so placing it
  inside `_gate` cannot deadlock against `_journalGate` users.
- CE-03 declaration: the original requirement makes the two-instance
  window declaration part of the decision record; the producer package
  carries it in the evidence-and-gate matrix ("Multi-instance risk is
  bounded; capability contract is the proposed closure"). It must also
  appear verbatim in the C2 decision text itself when the human records it
  (amendment A-C2-3).

### 6.4 Required amendments

- A-C2-1 (consume point): the design text must state that consumption
  happens for every acceptance path (normal, clock-unavailable, overflow)
  inside the `_gate` acceptance blocks, and duplicate-stop re-entry
  (232-235) must NOT re-consume (the flag path only; production code
  attempts would be idempotent but the contract should say the consume is
  exactly once at first acceptance and duplicate stops are warrant-less
  replays).
- A-C2-2 (release-first preservation): the design text must state the
  interaction: when `TryConsume` fails because the release already
  consumed, stop acceptance still stands (Accepted = true) and the retained
  release is not rewritten; `ReleaseAlreadyCommitted` continues to mean
  "runtime release path had consumed before this stop" (which includes the
  pre-transport window).
- A-C2-3 (CE-03 wording): copy the out-of-model declaration into the C2
  decision document itself.

### 6.5 Verdict

**C2 verdict: `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS`** (A-C2-1,
A-C2-2, A-C2-3). Structurally correct closure of the R08 window with no
schema change and no consumer visible-negative impact; the amendments are
precision items.

## 7. C3 - persisted stop-deadline consumption (R27)

### 7.1 Original requirement

Required-changes C3: `Reconciler` (or a production caller path) must derive
the stop allowance from `StopRecord.StopDeadlineNanoseconds`
(remaining = deadline - now) or receive a caller-supplied value provably
computed from the record; tests for within-window, expired-deadline and
no-reset behavior; G3-A10 diagnostic-only meanwhile. Binds Q04; blocks
G3-A10 and any R27 pass.

### 7.2 Verified defect analysis

Confirmed independently: `Reconciler` stores and forwards the caller
allowance (124-130, 231) and never reads the durable deadline. The D2 R27
probe (268-285) verifies the caller allowance is consumed after the
persisted deadline has expired (the fake clock advances by >= 5 s with the
durable deadline in the past). The validator makes the durable deadline
rewrite-proof (148, 165-175). The defect is real and bound to the right
symbols.

### 7.3 Proposal analysis

The C3-A recommendation (derive remaining from the durable record;
rejected C3-B provenance and C3-C authority object) is independently
supported by the source: the record is already immutable and validated; a
derived remaining time is the only unforgeable path to a reset-proof
allowance. One epoch-identity risk and two classification subtleties were
found beyond the producer analysis:

1. Clock identity (the producer package's most significant omission):
   `StopRecord` timestamps are produced by the transaction clock
   (`ITransactionClock`; production implementation `OwnerClock`,
   `OwnerPreparationTransaction.cs:21-32`, based on
   `Stopwatch.GetTimestamp()` = QPC/monotonic-with-boot-anchored epoch).
   The candidate analog clock for the recovery side, `IDomainClock`, has
   TWO divergent production implementations in R12:
   - `OwnerDomainClock` (`OwnerPreparationTransaction.cs:34-39`) is based
     on `Stopwatch.StartNew().ElapsedTicks` - its epoch is the construction
     moment, NOT the transaction-clock epoch. Feeding it into the C3
     formula `remaining = StopDeadline - now` would compute a
     near-zero-or-negative value at any recovery shortly after process
     start, wrongly driving every recovery to the expired/urgent path (or
     to quarantine under amendment A-C3-1's failure rule) - a silently
     wrong bound.
   - `LaunchDomainClock` (`BubblewrapAdapter.cs:517-521`) and
     `M1RecoveryScope`'s internal timing helpers (644, 868-870) are QPC-
     based, epoch-compatible with `OwnerClock`.
   The design text must therefore REQUIRE an epoch-corroborated clock for
   the C3 derivation (same QPC basis as the durable StopAcceptedAt/StopDeadline
   timestamps), state the failure rule when epoch identity cannot be
   established (fail to the conservative path, never to a fabricated
   window), and define the corroboration test. A duration-vs-deadline
   `StopAndAwaitEmpty` overload is optional; what matters is the epoch.
2. Outcome classification: the proposal binds "expired deadline ->
   zero-wait urgent stop; if populated -> return Unknown/quarantine, not a
   fresh allowance". The current `Reconciler` does not have that outcome
   path: an unconfirmed stop is recorded as a `Failed` component (234-235)
   and the aggregation returns `ReconcileOutcome.Failed` (429-436), while
   Unknown/quarantine arises only from Unknown components (437-442) or
   missing persistence (450-459). Adopting C3's stated behavior therefore
   requires an explicit Reconciler aggregation amendment (stop attempted at
   zero remaining but domain still populated -> explicit Unknown debt),
   not merely a different timeout source. Recorded as A-C3-2.
3. Structural reachability notes:
   - A durable `StopRecord` with `StopDeadlineNanoseconds <= 0` cannot
     validate (148), so the C3 zero/invalid-deadline case is defensive for
     records that the codec/validator already refuse; the reachable
     zero-deadline reality is exclusively the RUNTIME clock-unavailable /
     overflow stop, which by design persists no StopRecord at all
     (238-260). Recovery after such a stop therefore lands on the
     no-record path, where the caller allowance applies per the proposal.
     This is coherent, but the design text should say explicitly that the
     no-record path after a runtime-stop-accepted epoch is a bounded
     CLEANUP allowance, not a stop-confirmation window (I6 in the
     interaction register). Recorded as A-C3-3.
   - The DomainStop component currently performs the kill unconditionally
     whenever the completed-bit is unset (217-248), including when the
     domain may already be empty; the proposal's "Domain already empty:
     no unnecessary stop" acceptance row (row 11) is not today's behavior
     and would require an explicit pre-observation amendment, or the test
     row must be rewritten to "no unnecessary WAIT" only (the kill is the
     bounded destructive part; a kill of an already-empty domain is not a
     fresh-window problem but should be stated). Recorded as A-C3-4.
4. Zero-wait policy question (open in the producer package, section 7
   last bullet): whether an expired deadline authorizes kill + single
   observation as the "bounded urgent stop". The mechanism exists and
   matches the D2 probe's semantics; the policy choice itself belongs to
   the human decision holder and the reviewer does not answer it. This is
   the reason the C3 verdict is not unconditional support.

### 7.4 Required amendments / choices

- A-C3-1 (clock identity + epoch corroboration): as above; blocking for
  test design until stated.
- A-C3-2 (outcome mapping): binding Unknown/quarantine on
  attempted-but-not-empty requires the Reconciler aggregation amendment.
- A-C3-3 (no-record restart window): document the no-StopRecord cleanup
  allowance as cleanup-only, not stop confirmation (I6).
- A-C3-4 (row 11 rewrite or pre-observation amendment): reconcile the
  "domain already empty" acceptance row with the current unconditional
  kill.
- Human choice (open): zero-wait kill+observe acceptability on expiry.

### 7.5 Verdict

**C3 verdict: `CONTRACT CHOICE REQUIRED`** with required amendments
A-C3-1..A-C3-4. The derivation contract is the only structurally sound
R27 resolution (C3-B/C3-C rejections concur), but the package leaves the
expired-deadline destructive-action policy open and omits the clock-epoch
requirement that the formula's correctness silently depends on.

## 8. C4 - duplicate and concurrent release semantics (R02/R06)

### 8.1 Original requirement

Required-changes C4: choose between (a) an in-progress retained decision
delivered to concurrent losers and (b) a documented concurrent-refusal
contract with sequential Duplicate unchanged; document CE-01's retention
asymmetry as deliberate contract; retain retry expectations per failure
class. Binds Q07; blocks G3-A05.

### 8.2 Verified defect analysis

Confirmed independently: the three-caller shape (winner Released,
concurrent loser `Refused(NotCommitted)`, later caller Duplicate, exactly
one transport attempt) is today's behavior and is reproduced by the D2
concurrent probe (236-265) and the D4_9 owner-host probe. The concurrent
loser's refusal arises at the intent-planning refusal (102-108, reason
"release intent already exists"), is pre-gate, and is therefore
non-retained; the retention asymmetry (outside-gate refusals not retained,
gate refusals retained) follows from the code map (entry 64-79 and
journalBlock 81-115 non-retained; 125-223 retained). No second physical
write is reachable in any concurrent shape (journal baseline/transition
guards plus the one-shot transport channel). All verified.

### 8.3 Proposal analysis

C4-B (explicit concurrent-refusal contract documenting existing behavior)
is independently supported: it exactly matches observed R12 behavior, adds
no wait/notification/crash surface (correctly rejecting C4-A's
linearize-via-wait design for an M1 bounded tranche), and the
retention-asymmetry documentation turns an undocumented accident into a
declared contract. The candidate evaluation is fair; C4-C's extra state is
correctly rejected as unneeded. The ten-case contract in the proposal is
consistent with the verified code paths, including the explicit
declarations that `Refused` is not durable classification and `Duplicate`
is not delivery proof (both match the R12 semantics: pre-gate refusals are
not persisted anywhere; the retained Duplicate relabels whatever the
original retained decision was, including a MayHaveExecuted or an
ambiguous future C1 result).

### 8.4 Precision amendments

- A-C4-1 (bind the refusal classes to source symbols): the contract text
  should bind, in the decision record, the exact R12 reason strings per
  class so tests can assert against them: "release intent already exists"
  (intent-planning structural refusal, non-retained, corresponds to the
  producer's case 7), the pre-gate classes (64-79, 83-100), and the
  retained gate classes (125-154). Also clarify that "no acknowledged
  snapshot"/writer-not-liveness checks sit inside `_journalGate` (81-115) -
  physically inside a lock but semantically outside the gate (before
  durable intent) - so the outside/gate taxonomy is not confused with lock
  placement. All producer case rows are consistent with the source; this
  amendment is documentation discipline, not semantic change.
- The producer's open question (optional `ConcurrentRefusal` kind versus
  documented metadata) is genuinely optional; the reviewer concurs with
  keeping `Refused` with metadata as the minimal form, provided the
  concurrent vs structural refusal distinction is discoverable in the
  reason/metadata per A-C4-1.
- The explicit-C1-ambient dependency (case 7/8 of the contract and
  acceptance test 6) is properly deferred to the C1 decision item.

### 8.5 Verdict

**C4 verdict: `DESIGN SUPPORTED FOR HUMAN DECISION`** (with A-C4-1 as a
documentation-discipline item that does not block the approval question).
The proposal represents observed R12 behavior truthfully and is the right
choice among the original alternatives.

## 9. Package-level adversarial review

Findings beyond the per-contract analysis:

- A-PKG-1 (binding precision): see 3.3. The original requirements blob is
  reachable only from the adversarial-review branch, while the D6.0 base
  carries the preservation checkpoint that binds it by id. Acceptable, but
  the D6.0 documents should cite the branch name alongside the blob id.
- A-PKG-2 ("production scope" phrasing): `ReleaseArbiter` and `Reconciler`
  have zero production call sites in R12 (4.9). Statements like
  "Production scope: ReleaseArbiter.RequestRelease" are accurate as
  file-scoped statements but should be tempered with the composition
  boundary (first production composition arrives with the WP08 owner
  orchestration; until then all adoption evidence is component-level and
  gates stay diagnostic).
- A-PKG-3 (absence of a dedicated CE-02 stop-deadline probe note): the
  package's C1 acceptance list contains the ambiguous-intent probe and
  stop probes, but the interplay "ambiguous wedge THEN salvaged
  StopDecision with deadline 0" (the clock-unavailable acceptance inside a
  wedged journal) has no dedicated named acceptance row; D2 has both
  halves (ambiguous wedge 135-163; clock-unavailable stop 98-115). The
  composer test list should include the composed case to avoid a
  silent-under-test seam. Non-blocking.
- A-PKG-4 (evidence-and-gate matrix selfhood): the matrix correctly
  declines to accept any gate and correctly marks diagnostics-only for
  G3-A minus items; verified against required-changes C8 (sub-gates
  cannot be tagged accepted while C1-C4 are unresolved) and the D5 review
  handoff H1-H5.

## 10. Pairwise interaction assessment (summary)

Full scenario register: `gagamba-linux-m1-wp05-d6-0-contract-interaction-register.md`.
All six pairs were assessed against verified lock/serialization structures.

- C1xC2 - coherent by construction: C1's bounded runtime stop is exactly
  the C2 acceptance point; the wedge makes C2's durable attempt fail
  harmlessly (636-641) without new seal content. Amend with A-C1-1 so the
  stop invocation cannot invert lock order.
- C1xC3 - coherent: C1's best-effort durable stop, when it succeeds before
  crash, produces a validated StopRecord that C3's reconciler can consume;
  when it fails, no record exists and the no-record path applies (I6/A-C3-3).
- C1xC4 - coherent: C4's case 6/7 retention (Duplicate of the ambiguous
  result) requires A-C1-2's exact vocabulary to be stated first; C4
  defers correctly.
- C2xC3 - coherent: stop-at-expiry consumes the capability (release
  authority never returns) while the reconciler performs bounded cleanup;
  the residual is I6's no-record window (documented cleanup allowance).
- C2xC4 - coherent: C4's case 8 (stop-winning race) inherits C2's
  consumption; the release-then-stop order preserves the release while
  stop acceptance stands; verified against the gate/journal race map.
- C3xC4 - independent components: the reconciler consumes no release
  decisions; the only coupling is that C4's documented retention map must
  not be read as recovery classification (durable facts remain the
  classifier's domain). No conflict.

No requires-a-redesign interaction was found. No deadlock risk is
introduced by any pair, given the established lock phases (verified:
`_gate` is never held across journal I/O in either entry point; both C1
and C2 amendments reinforce this).

## 11. Scenario register I1-I8 (summary)

- I1 ambiguous intent then stop: covered by the D2 ambiguous probe's stop
  half today; C1+A-C1-3 formalizes it. No new mechanism needed.
- I2 stop then ambiguous retry: durable-stop case blocked at journal level
  (27-31); failed-durable-stop case blocked by C2 consumption. Closed by
  the proposals.
- I3 release wins then stop: stop accepted, release context preserved
  (A-C2-2); durable order between Committed and StopRecord is legal in
  either arrangement (66-106 vs 108-151) and classification stays
  conservative (100-103). Verified.
- I4 concurrent duplicate during ambiguous persistence: all concurrent
  shapes fail closed on the wedged journal before the gate; no FD4 in any
  branch (verified pre-gate structure). C4 documents the loser class.
- I5 stop near persisted deadline: C3 derives remaining; within-window
  takeover uses less; no reset (validator-imposed). Requires the A-C3-1
  clock identity to hold.
- I6 runtime stop with no durable deadline (clock-unavailable/overflow or
  failed durable stop), then crash/recovery: no StopRecord exists; C2 has
  consumed the capability; recovery has no stop bound and grants the
  cleanup-only caller allowance (A-C3-3 documentation item). No release
  authority returns. Residual policy risk is explicitly cleanup-window
  length, not release safety.
- I7 repeated recovery unbounded window: same absolute deadline; C3 rows
  4/5/10 forbid reset; verified that nothing in Reconciler alters the
  record (records are immutable under the validator; TryPersist mutates
  only CompletedCleanup/Cleanup fields, 184-201).
- I8 duplicate after partial FD4 delivery: the runtime exception map (162-
  169) yields Unknown delivery, retained as Released/MayHaveExecuted with
  DurableCommitted update best-effort; later callers get Duplicate of
  that; recovery classification stays MayHaveExecuted for the durable
  Committed record. Verified; no resend path exists.

## 12. Remediation proposal assessment

Assessed: `docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md`.

- Sequence C2 -> C1 -> C4 -> C3 is accepted as a sensible dependency-
  coherent order (C2 closes the authority window C1 uses; C4 aligns with
  C1's vocabulary; C3 is independent of C1/C2 semantics and placed last for
  evidence coherence against a single coherent tree per tranche). The
  reviewer additionally notes C3 could be pulled forward without
  interaction risk (no C1-C4 dependency either direction) - informational,
  not a required change.
- Per-tranche boundaries (separate commits, separate review, separate
  evidence, no automatic gate transfer) are correct and match the
  D-series conventions; requalification requirements (fresh tree binding,
  reruns of affected D1-D5 rows, fresh independent review, SHA-256
  manifests, no silent artifact reuse) are adequately strict and
  consistent with the R12 evidence regime.
- Rollback criteria (second physical release, weakened no-resend, false
  definite NotCommitted, live capability after stop, deadline reset,
  fabricated evidence, unjustified schema, broken record compatibility)
  are complete against the contract semantics reviewed here.
- Tranches must include the required amendments from this review as
  updated design text BEFORE first code: A-C1-1..3, A-C2-1..3, A-C3-1..4,
  A-C4-1 (see the required-changes document for the exact list).

## 13. H1-H5, gate matrix concurrence, protocol deviations

- H1-H5 preservation verified: the D6.0 deferred-integration document
  carries H1-H5 unchanged from the D5 review handoff (D5-O05 zero-caller
  status, descriptor exclusions unclaimed, optional wait4/pidfd witness,
  C1-C4 unresolved, D5 ordering note). No item is silently resolved.
- Gate mapping verified against the original required-changes rows:
  C1 -> G3-A11, G3-B08; C2 -> G3-A02, G3-A07; C3 -> G3-A10 (and the R27
  pass rule); C4 -> G3-A05. The evidence-and-gate matrix agrees, marks all
  as blocked/diagnostic-only, and correctly declares that no D4/D5 fixture
  pass converts them.
- The retained decision retention semantics used in the matrix's C4 row
  ("possible decision-comment/retention changes") is compatible with the
  recommended minimal form.
- Protocol deviations found: none of substance. The producer package, the
  required-changes source and this review's verdicts align on authority
  boundaries; the only shared-vocabulary correction is A-PKG-1/A-PKG-2
  binding precision.

## 14. Disposition

| Item | Verdict | Required amendments |
|---|---|---|
| C1 R09 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C1-1, A-C1-2, A-C1-3 |
| C2 R08 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C2-1, A-C2-2, A-C2-3 |
| C3 R27 | CONTRACT CHOICE REQUIRED | A-C3-1, A-C3-2, A-C3-3, A-C3-4 + human choice (expired-deadline policy) |
| C4 R02/R06 | DESIGN SUPPORTED FOR HUMAN DECISION | A-C4-1 (documentation discipline) |
| Package | Supported with precision amendments | A-PKG-1, A-PKG-2 (non-blocking) |

**Overall disposition: Option B.** This review independently confirms
the D6.0 package's defect-to-contract mapping against the R12 source, finds
no structural defect in any proposed contract, requires the named design
precision amendments before any contract is approval-ready, defers the two
human decisions (C3 expired-deadline destructive-action policy; each
contract's approval), rejects no proposal, and accepts no gate. The
remediation proposal is eligible for human authorization only after the
amendments are folded into the respective contract texts. Release
qualification remains unclaimed; D5's H1-H5 boundaries and the standing
"green tests are not completion" rule apply unchanged.

## Review-commit provenance

This review is committed on
`review/wp05-d6-0-independent-contract-design` from exact base
23df1c566e7d8f072d924f374bf6891c726c03fe, in three commits (main review;
matrix + interaction register; required changes + handoff), documentation-
only, local-only per review protocol.
