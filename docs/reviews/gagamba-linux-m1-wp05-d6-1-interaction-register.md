# WP05 D6.1 Contract Interaction and Counterexample Register

Author: GLM (independent adversarial design reviewer). Six pairwise C1-C4
interactions independently re-evaluated against verified R12 source structure
(`38f6981b...`, tree `6854675d...`) and the D6.1 amended contracts at
`750914d93edaf5490b42bf0787f20bb0c5089767`. Each pair gets at least one
adversarial state transition. Counterexample status: GENUINE (unresolved by
D6.1 text as written), RULED OUT (structurally impossible in source), or
UNRESOLVED-BY-TEXT (safe in source, unspecified in contract text; resolves via
the named review amendment). No contract approved; no remediation authorized.

Status: **INDEPENDENT INTERACTION AUDIT - NOTHING APPROVED**

Date: 2026-10-21

R12 structure used (all verified this review):
`ReleaseArbiter.cs` entry `_gate` 48-62 (retained relabel 50-52; stop-entry
refusal 55-61); prechecks 64-79; `_journalGate` 81-115; after-intent `_gate`
118-183 (retained/stop rules 120-131; gate checks 134-146; consume 148-155;
`_releaseRuntimeCommitted` 158; transport 159-171; retained release 176-183);
committed persistence `_journalGate` 185-223; final retention `_gate` 215-223;
`RequestStop` `_gate` 226-268 (duplicate 232-235; clock 237-244; overflow
246-261; normal accept 263-267), journal persistence after gate release
270-289, after-stop retention 291-296. `OwnershipJournal.cs` staging 424-490
(seal 424-428; unlink-failure ambiguity 486-490), `FailSealed` 504-519,
`WriteMutation` resolve-refusal 635-641, `TryRecoverCommit` 712-754, `Resolve`
773-819. `Reconciler.cs` planning 149-157, DomainStop kill-first 216-248,
aggregation 424-473. `DomainLease.cs` StopOutcome 11-18, kill 116-120, poll
123-142. Transitions plan refusals (`OwnershipTransitions.cs:21-31,108-151`).

## Pair 1: C1 x C2 - ambiguous persistence and stop consumption

**Adversarial trace (initial state: clean owner; event order: wedged intent ->
stop):**

1. Caller A enters `_journalGate`; `WriteTransition` writes both slots with
   Intent; final seal unlink fails (`OwnershipJournal.cs:486-490`); A leaves
   `_journalGate` at 115 with a typed ambiguous outcome.
2. Concurrent operator stop call B enters `_gate` first (`:230`): sets
   `_stopAccepted`, retains decision; under the amended contract B consumes
   the capability inside `_gate` (C2 first acceptance); B's durable attempt
   after gate release fails harmlessly at the resolve-refusal
   (`OwnershipJournal.cs:635-641`) - no new seal content.
3. A acquires `_gate` for retention (the D6.1 point) - collision case (see
   C1 x C4 / D6.1-R3).
4. Stop log: A invokes the tail `RequestStop`: duplicate re-entry path
   (`:232-235`) returns the retained stop without a second capability
   transition.

- Runtime winner: stop (B) - consumed exactly once; A's tail stop is
  idempotent.
- Journal evidence: both slots hold Intent; seal present; no StopRecord unless
  a later clock-valid acceptance attempt writes one (sealed journal blocks it
  while the owner lives).
- Capability state: permanently consumed at B's acceptance (or A's, if A wins
  `_gate` first); duplicate consume never re-transitions.
- Returned classification: A receives retained stop refusal or Duplicate of
  it (per D6.1-R3 rule); B receives its StopDecision.
- Recovery consequence: owner death; fresh writer takes fence; seal recovery
  yields Intent -> classifier `MayHaveExecuted` (`ReleaseRecoveryClassification`);
  no resend; replacement has no capability.
- Forbidden physical action: FD4 (journal seals out every forward path;
  verified), second intent, lock inversion (none possible: no nested
  gate/journal phase anywhere).
- Test needed: `AmbiguousReleaseStopLockOrderProbe` with the stop winning
  before A's retention (D6.1-R3 expectations) and after.
- Verdict: supported; collision ordering was UNRESOLVED-BY-TEXT in D6.1,
  resolved by required amendment D6.1-R3.

**Counterexample attempt (capability reuse race):** a second arbiter instance
over the same capability between wedge and stop acceptance - RULED OUT: any
forward path needs `ReadLatest().Ok` (sealed -> Uncertain, 677-690) or
`WriteTransition` success (refused at 567-573), so no transport is reachable;
the capability-consume window is documented (A-C1-3), not exploitable.

## Pair 2: C1 x C3 - ambiguous wedge and deadline/reset

**Adversarial trace A (clock valid):** wedge; tail stop accepted with runtime
deadline `now + 5s`; durable StopRecord attempt fails at the sealed journal
(harmless, 270-289, `write.Ok=false`); owner dies. Recovery: journal has
Intent, no StopRecord -> C3 no-record rule (cleanup-only allowance; not a
deadline; no fabricated interval). Fabricated-deadline counterexample attempt:
RULED OUT (nothing writes a StopRecord out of the wedge; validator forbids
deadline <= 0 and non-conformant deadlines, `OwnershipValidation.cs:145-175`).

**Adversarial trace B (clock unavailable):** wedge; tail stop accepted with
deadline 0 and no durable attempt at all (`:237-244` early return inside
`_gate`); capability consumed (C2); recovery identical no-record path.
Unbounded-stop counterexample attempt: RULED OUT - stop bounds remain the
runtime advisory deadline (absent in this case) and C3's bounded cleanup
allowance; neither can manufacture a five-second window; repeated
reconciliation reuses the same absolute deadline when a record exists.

- Runtime winner: stop. Journal: Intent-only. Capability: consumed.
- Classification: `AmbiguousPersistence` retained (D6.1-R3 collision rule
  aside); recovery classifier `MayHaveExecuted` after death recovery.
- Forbidden: fresh 5s; fabricated StopRecord; retry of the intent write.
- Tests: `AmbiguousWedgeThenClockUnavailableStop` (composed),
  no-record restart probe.
- Verdict: supported; constraints verified end-to-end in source.

## Pair 3: C1 x C4 - retained ambiguity vs duplicate/retry

**Adversarial trace:** after A's wedge and BEFORE retention (window between
leaving `_journalGate` and the `_gate` retention), concurrent caller C2'
completes its own sequence: entry `_gate` fine (nothing retained yet),
prechecks pass (capability still live pre-stop), `_journalGate`: writer live,
`ReadLatest` -> Uncertain -> `Refused(NotCommitted, "release request refused:
no acknowledged preparation snapshot (commit seal present...)")
(`ReleaseArbiter.cs:92-94`), non-retained; C2' may retry (liveness drain).
After A retains ambiguity, C2's next call hits the entry relabel
(`:50-52`) -> `Duplicate` of the ambiguous decision.

- Genuine counterexample attempt (retry converts ambiguity into a physical
  second write): RULED OUT - `TryPlanReleaseIntent` is unreachable through a
  sealed journal, and the retained `_retainedRelease` short-circuits every
  later same-instance call.
- Cross- NotCommitted concern: C2''s pre-retention `Refused(NotCommitted)`
  reports a definite-shaped refusal while the durable Intent content may be
  complete - structurally safe (no intent/FD4 possible; recovery resolves) but
  the retry expectation should note futility for a live owner (optional C4
  refinement D6.1-R4 tail).
- Stop-won collision variant: bounded by D6.1-R3 (see Unresolved-BY-TEXT).
- Verdict: supported with D6.1-R3/R4 precision; no physical-action violation
  is reachable.

## Pair 4: C2 x C3 - capability closure vs clock/persistence failure

**Adversarial trace:** operator stop accepted on the clock-unavailable path
(`:237-244`): consume inside `_gate` (C2) precedes any persistence attempt;
stop persists nothing. Later StopRecord write attempts (other components /
recovery host) can only produce a validator-valid record (acceptedAt > 0,
deadline = acceptedAt + 5s); a failed durable stop never reopens the capability
(store `Consumed` is terminal). A second entertains release: entry precheck
`_capability.IsLive` false -> pre-gate refusal; or, for a different capability
minted later by the store (foreign instance), `IdentityEqual` fails. Recovery
never touches capabilities (`Reconciler` has no PreparedTransaction reference;
RecoveryPlanner authority is the live lease, `Reconciler.cs:75-97`).

- Counterexample attempt (expired/invalid deadline -> authority transfer):
  RULED OUT - `RecoveryPlanner` refusals (cross-boot, quarantine, mismatch)
  and the C2 consumption are independent.
- Forbidden: clock-dependent consume timing (isolated by `_gate` placement),
  deadline reset (validator), reactivation (none exists; `Dispose` is the only
  store removal and is not reachable from stop).
- Tests: three-path `IsLive == false` inverted assertions; failed-durable-stop
  fixture; second-arbiter fixture.
- Verdict: supported.

## Pair 5: C2 x C4 - stop-first vs release-first retained decisions

**Adversarial trace (stop-first):** release caller X is past prechecks and
holds the Intent write either before or after stop acceptance:
1. Durable stop exists first: X's `TryPlanReleaseIntent` refuses structurally
   ("release intent is forbidden after a durable stop").
2. Stop accepted while X is between intent and gate: X reaches after-intent
   `_gate` (`:125-131`) -> consume (already consumed by C2 -> ignored result,
   `ConsumeCapability` at `:127/:305-308` tolerates false) -> retained
   `Refused(MayHaveExecuted, "release gate refused: stop won during durable
   intent")`; capability consumed once; no FD4.
3. Release-first: X consumed at 149, `_releaseRuntimeCommitted=true` at 158,
   transport attempted; stop arrives: StopDecision carries
   `ReleaseAlreadyCommitted=true` (read under `_gate`), consume attempt fails
   with `Consumed`, acceptance stands, X's retained release is untouched;
   Committed persistence (185-223) proceeds independently; no history
   rewrite anywhere in `RequestStop`.

- Winner ordering is deterministic under the locks (both mutations under
  `_gate`).
- Counterexample attempt (stop erases or reinterprets a release): RULED OUT.
- Retained decisions stay distinct per decision type (`_retainedRelease` /
  `_retainedStop` are separate slots, `:26-27`).
- Tests: stop-winning race; transport/committed/retention case per taxonomy.
- Verdict: supported; A-C2-2's flag semantics verified source-true.

## Pair 6: C3 x C4 - repeated reconciliation and deadline reset

**Adversarial trace:** repeated reconciler generations over the same record
(including after duplicates in the live owner): Reconciler writes only
`CompletedCleanup`/`Cleanup` bits (`Reconciler.cs:184-210`); the StopRecord is
validator-immutable (`OwnershipValidation.cs:148,165-175`; planner refuses a
second stop `OwnershipTransitions.cs:118-122`). Duplicate release handling in
the live owner never touches the journal StopRecord.

- Counterexample attempt (duplicate/recovery grants a fresh window): RULED
  OUT - the consumer-side allowance is derived per-call as
  `max(0, deadline - now)`; no writer path exists for the deadline.
- Interaction is otherwise independent (no shared runtime symbols; C4
  vocabulary is live-owner only; durable classification is the separate
  conservative projection).
- Tests: repeated-takeover no-accumulation; record-bytes unchanged.
- Verdict: supported.

## Register summary

| Pair | Adversarial transition used | Counterexample status | Test obligation | Dependency |
|---|---|---|---|---|
| C1 x C2 | wedged intent + concurrently-won stop; duplicate tail stop | ordering at retention UNRESOLVED-BY-TEXT -> D6.1-R3; reuse race RULED OUT | lock-order probe + retention expectations | A-C1-1, C2 |
| C1 x C3 | wedge with valid/invalid clock; death; recovery | fabricated deadline RULED OUT | composed wedge/clock case; no-record test | A-C3-1..3 |
| C1 x C4 | pre-retention refuser; post-retention duplicate; cross-instance retry | retry-to-second-write RULED OUT; NotCommitted-shape refusal documented as observation | retention + concurrent probes | A-C1-2, D6.1-R4 |
| C2 x C3 | clock-unavailable stop + failed durable stop + second arbiter | authority-transfer RULED OUT | three-path inverted IsLive fixtures | A-C2-1..2 |
| C2 x C4 | stop-first and release-first winner traces | history rewrite RULED OUT | stop-winning race; taxonomy assertions | A-C2-2, A-C4-1 |
| C3 x C4 | repeated reconciliation; duplicates before/after | window reset RULED OUT | record-immutability probes | A-C3-2 |

No pair requires structural redesign. One new required precision amendment
emerged from the adversarial audit (D6.1-R3, recorded in the main review); the
optional retry-futility note is folded into D6.1-R4's tail. No new durable
record is introduced by any amendment to patch a source-level inconsistency.
