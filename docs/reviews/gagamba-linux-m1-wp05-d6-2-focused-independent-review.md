# WP05 D6.2 Focused Independent Contract Design Review (GLM)

Status: **PROPOSED - NOT APPROVED**
Review disposition: **D6.2 FOCUSED INDEPENDENT REVIEW COMPLETE - OPTION B: DESIGN SUPPORTED WITH ONE REQUIRED DOCUMENTATION-ONLY CORRECTION (D6.2-R1); C2/C4 SUPPORT UNCHANGED; C3 POLICY AND C1-C4 APPROVAL PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**
Scope: independent reviewer verification of the four D6.2 precision amendments (D6.1-R1 through R4). Nothing in this document is an approval, an authorization, a qualification or a gate acceptance.

Decision owner: human WP05 release/security decision holder. This review is advisory input; it cannot approve the C1-C4 contracts, select the C3 expired-deadline policy, authorize production remediation or close any gate.

## 1. Identity and binding

All bindings verified during this review:

- R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f` (pre-commit checked in this repository).
- D6.1 independent review tip: `77f559a7dfc1702b33812d5c3d5c42ac925f0343` on `review/wp05-d6-1-independent-contract-design-2` (blob identities re-validated in the D6.1 preservation, unchanged here).
- D6.1 preservation base and D6.2 diff base: `d39dab60152a6f83724760b3122f61d6cf928be3`.
- D6.2 producer branch: `design/wp05-d6-2-contract-precision-amendments`, review target HEAD `ae09afedddeb703e6bde226bd793b375896c3064`; commit chain `d39dab60152a6f83724760b3122f61d6cf928be3` -> `d089f9382b89fc53ef4cbf1db28d1a40bf93baf9` -> `ab911d1deeb89b965f751dea8a9c1e82117bbcdf` -> `ae09afedddeb703e6bde226bd793b375896c3064` (verified by `git rev-list` ancestry; the first D6.2 commit's parent is the D6.1 preservation base).
- Fixture branch: `implementation/wp05-release-qualification-fixtures`; test commit `dc4c372b180abef1b27038d94f49af90226fa548`; evidence tip `338a98af269e6b5839f3ccc2e0881f016153b01e` (identical to the branch tip, see section 8).
- Phase-gate fact: the D6.1 review branch is **not** an ancestor of the D6.2 branch (`git merge-base --is-ancestor` refuted), and R12 is an ancestor of the D6.2 branch. No review content was merged or cherry-picked into D6.2.

## 2. Scope rules

Reviewed: only the four D6.2 correction sections and their directly affected cross-contract claims.

- C1: `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md`, "D6.2 revision - R3 retention collision and R4 fixture provenance".
- C3: `docs/decisions/wp05-d6-0-c3-stop-deadline.md`, "D6.2 revision - R1 structured outcomes and R2 clock provenance".
- C2: `docs/decisions/wp05-d6-0-c2-stop-capability.md`, "D6.2 cross-reference - R3 stop ordering".
- C4: `docs/decisions/wp05-d6-0-c4-duplicate-semantics.md`, "D6.2 precision note - R3 and retry futility".
- Reconciliation register: `docs/decisions/gagamba-linux-m1-wp05-d6-2-precision-amendment-reconciliation.md`.
- Supporting: the D6.2 rows of the evidence/gate matrix, the D6.2 pointer sections in the decision summary, proposals, D6.1 register and human-questions sheet, the remediation-proposal prerequisites section, the focused-review handoff and the human approval addendum.

Not re-reviewed (per the D6.2 handoff): the full D6.0/D6.1 review, D1-D5 qualification, H1-H5 dispositions, WP08/WP10, full G3, and the C2/C4 substance verdicts from D6.1 (`DESIGN SUPPORTED FOR HUMAN DECISION`), which D6.2 does not reopen. The C3 expired-deadline policy selection remains a human decision; D6.2 selects neither Policy A nor B and this review does not either.

## 3. Documentation-only diff verification

`git diff --numstat d39dab6... ae09afed...` (15 files, all under `docs/`):

| File | + | - |
|---|---|---|
| `docs/decisions/gagamba-linux-m1-wp05-d6-0-decision-summary.md` | 34 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-0-evidence-and-gate-matrix.md` | 63 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-0-release-contract-decision-proposals.md` | 13 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-1-amendment-reconciliation.md` | 7 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-1-human-approval-questions.md` | 5 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-2-focused-review-handoff.md` | 121 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-2-human-approval-addendum.md` | 139 | 0 |
| `docs/decisions/gagamba-linux-m1-wp05-d6-2-precision-amendment-reconciliation.md` | 265 | 0 |
| `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md` | 201 | 0 |
| `docs/decisions/wp05-d6-0-c2-stop-capability.md` | 14 | 0 |
| `docs/decisions/wp05-d6-0-c3-stop-deadline.md` | 340 | 0 |
| `docs/decisions/wp05-d6-0-c4-duplicate-semantics.md` | 22 | 0 |
| `docs/handoff.md` | 5 | 1 (heading replacement) |
| `docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md` | 60 | 0 |
| `docs/work-queue.md` | 5 | 1 (heading replacement) |

No production, test, native, engineering or fixture file is touched at any point of the D6.2 chain. No fixture file is copied or merged. The `handoff.md`/`work-queue.md` hunks are heading-plus-insertion edits; the prior D5/D6.1 paragraphs are retained verbatim as "Prior action/Prior priority" entries. STATUS/authorization posture is consistent in every touched document (documents claim `DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW`, not verification).

## 4. Verification toolchain record

- Review worktree: `C:\Users\Laszlos\source\repos\Gagamba-review-d62-glm` on branch `review/wp05-d6-2-focused-contract-precision`, created at the exact review target HEAD `ae09afed...` and kept clean between review steps.
- R12 sources were re-read fresh from the exact R12 commit via `git show` extraction to a disposable temp directory, not from a stale checkout.
- R4 digests were computed on raw blob bytes with `git cat-file blob` redirected through `cmd` and hashed with `certutil`. A first attempt through a PowerShell 5.1 `>` redirect produced a different digest because that operator re-encodes native stdout as UTF-16 text; the byte-faithful re-hash matched the D6.2 claim exactly. The method and this pitfall are recorded so a reproducer does not repeat the text-redirect mistake.
- Line citations were taken from the exact-commit extracts and cross-checked against `git grep -n` hits on the same commit for strings such as `seal release failed, commit ambiguous` (OwnershipJournal.cs:489), `commit seal present` (OwnershipJournal.cs:780), `OwnerDomainClock` (declaration-only hit, see section 6), and the fixture-absence check in section 8.

## 5. R1 - structured reconciliation outcome discrimination

### 5.1 What the D6.1 finding required

D6.1-R1 required an implementable mechanism that can distinguish, without error-string classification or blanket Failed-to-Unknown mapping: a genuine backend/kill failure; an attempted-but-unconfirmed stop; a population-observation uncertainty; a confirmed empty cleanup; and a genuine reconciliation operation failure. It also required an explicit `JournalFinalization` aggregation rule, because `Reconciler` currently maps a persistence-only failure of the final write to aggregate `Failed`.

### 5.2 R12 source verification

The D6.2 source-evidence claims were verified line-anchored against R12:

- `Lifecycle/DomainLease.cs:11-18` - `StopOutcome(bool Stopped, bool TimedOut, string Error)` with the three factories. `StopOutcome.Failure(string)` (`:17`) yields `new(false, false, error)`; this is the exact collapse point the D6.1 finding named: a population-read failure (`:126-129` returns `StopOutcome.Failure(population.Error)`) is indistinguishable from a kill write failure (`:116-120` returns `StopOutcome.Failure(kill.Error)`).
- `Lifecycle/DomainLease.cs:108-143` - `StopAndAwaitEmpty` attempts the kill first (`:116`), returns `Failure` on `!kill.Ok` (`:117-120`), derives `deadline = clock.NowNanoseconds + (long)(timeout.TotalSeconds * 1_000_000_000d)` (`:122`), then polls `ReadPopulation` with a 25 ms `clock.Delay`; exits are `Success` (empty, `:131-134`), `Failure` (observation not `Known`, `:126-129`) and `Timeout` (deadline reached, `:136-139`). `StopOutcome` cannot currently express "kill dispatched but emptiness unconfirmed" as anything other than `Timeout`, and cannot express "observation unavailable" at all (both collapse to `Stopped=false`). The D6.2 characterization of the collapse is accurate.
- `Lifecycle/RestrictedDomain.cs:15-22` - `DomainPopulationResult(bool Known, bool Empty, string Error)` with `Populated`/`Emptied`/`Unknown` factories. This is the typed base the D6.2 `StopPopulationObservation` maps onto; the register's citation `:15-21` is correct within one line.
- `Native/CgroupInterop.cs:174-201` - `ReadPopulation()` parses the `populated 0/1` line and returns `DomainPopulationResult.Unknown("cgroup.events lacks a populated line")` at `:201`; the cited range is exact.
- `Recovery/Reconciler.cs:215-248` - the stop component: verify identity (`:224-228`, refusal return), `StopAndAwaitEmpty(_stopAllowance, _clock)` (`:231` - the caller-supplied constructor allowance, confirming the C3 section 2 premise), and the blanket mapping `!stop.Stopped -> ComponentState.Failed` (`:232-236`). At R12 a timed-out-but-populated stop is classified identically to a kill write failure, which is exactly the imprecision D6.2's typed table eliminates.
- `Recovery/Reconciler.cs:250-287` - the empty component, including the retained `ObservePopulation` fallback when the bounded stop did not establish emptiness (`:263-287`): a known-populated fallback currently yields `DomainEmpty = Failed` (`:277-281`); an unknown fallback yields `Unknown` (`:282-286`); a known-empty fallback yields `Confirmed` plus bit persistence (`:265-275`).
- `Recovery/Reconciler.cs:423-471` - the aggregation and finalization block, verified exactly as D6.2 describes:
  - `:429-436`: first clause `if (required.Any(s => s == ComponentState.Failed) || finalizationFailed)` -> `Failed`. Note that `finalizationFailed` participates in this clause, so at R12 a component-progress persistence failure is rolled into aggregate `Failed` even when no required component failed (the detail fallback at `:433-435` handles that sub-case).
  - `:437-442`: aggregate `QuarantinedUnknown` is reached only via a required component with state `Unknown`.
  - `:450-459`: no writer lease supplied -> `JournalFinalization = Unknown` row and, if the computed outcome was `CleanupConfirmed`, an override to `QuarantinedUnknown`.
  - `:460-471`: the final `TryPersist` failure sets `finalizationState = ComponentState.Failed` **and unconditionally overrides** `outcome = ReconcileOutcome.Failed` (`:469-470`) - including the case where every required component is `Confirmed`. This is the exact defect row D6.1-R1 targeted; the register's citation `465-471` is exact.
  - D6.2's statement that the explicit `JournalFinalization` amendment "is not implied by changing only the DomainStop mapping at `:232-236`" is verified: the `:469-470` override would survive a stop-mapping-only change.
- Test seam inventory: `FakeDomainBackend`, `FakeDomainClock`, `FakeJournalStorage` all exist in R12 in `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryTestSupport.cs`, so the R1-T1..T12 test specification is buildable on the existing seams without new infrastructure.
- Staging/removal/authority rows of the D6.2 table agree with the verified R12 facts: staging `Failed` on definitive cleaner failure, `Unknown` on mismatch/missing cleaner (`:366-375`, `:310-314`); removal `Failed` on definitive failure, `Unknown` on refusal (`:399-421`); planner `RefuseCrossBoot`/`RefuseMismatch` -> `Refused` and `QuarantineUnknown` -> `QuarantinedUnknown` with no destructive action (`Reconciler.cs:75-97, 149-157`).

### 5.3 Verdict on R1

**`PARTIALLY ADDRESSED`.** The D6.2 mechanism is implementable on R12's actual seams, the typed decomposition (`KillDispatchOutcome`/`StopPopulationObservation`/`TimedOut`) genuinely resolves the five-way discrimination D6.1-R1 demanded, the aggregate precedence (`Failed` over `QuarantinedUnknown` over `CleanupConfirmed`) preserves negative operation evidence, and the explicit `JournalFinalization` row directly corrects the verified `:469-470` override. The R1-T1..T12 table is deterministic and sound against the named seams.

One internal contradiction blocks a `VERIFIED ADDRESSED` verdict and is fixed by a documentation-only amendment (finding **D6.2-R1**, section 5.4). The remaining items below are observations, not required amendments.

### 5.4 Required correction - D6.2-R1 (documentation-only)

Contradiction between the D6.2 mapping table and the retained fallback rule, in C3 "R1 component outcome mapping":

- Row 1 states, unconditionally: `Kill=NotAttempted -> DomainStop Unknown, DomainEmpty Unknown` ("if reached, classify conservatively").
- The immediately preceding paragraph states the fallback `ObservePopulation` is "retained only for diagnostics when the bounded stop performed no observation (`Kill=Failed`/`NotAttempted`)".
- Row 2 (`Kill=Failed`) resolves `DomainEmpty` conditionally: "`Confirmed` if a diagnostic observation is empty, otherwise `Unknown`".

As written, the two rules disagree for `Kill=NotAttempted` plus a fallback observation of empty. Row 1 forces `DomainEmpty = Unknown` even though the store-local evidence proves emptiness, while row 2 would have confirmed the identical observation. This is the same evidence-loss shape that D6.1-R1 criticized in the finalization mapping, and it also sits oddly with the R12 fallback behavior (`Reconciler.cs:265-275` confirms and persists `DomainEmpty` from a fallback observation).

Required amendment text (one row, documentation-only): either

1. align row 1 with row 2 - for `Kill=NotAttempted`, `DomainEmpty` is `Confirmed` if the diagnostic observation is empty, otherwise `Unknown` (preferred: `DomainEmpty` is a statement about emptiness, observable independently of kill dispatch success, and preserves real evidence); or
2. state explicitly that for `Kill=NotAttempted` the fallback observation is diagnostic-only and never confirms `DomainEmpty`, and delete the `NotAttempted` case from the fallback's operational sentence.

Either resolution must be stated in one sentence, and the aggregate truth table is then unchanged for all other rows.

### 5.5 R1 observations (non-blocking)

1. `Kill=NotAttempted` is not producible by the proposed bounded stop itself: `StopAndAwaitEmpty` always attempts the kill first (`:116`), and a disposed lease yields `Failure`, not `NotAttempted`. Row 1 is therefore a defensive row for future/planned paths; keeping it is fine once it is consistent per D6.2-R1.
2. The D6.2 text does not state whether the `DomainEmpty` durable bit is persisted when `DomainStop` is `Failed` and only the diagnostic fallback established emptiness. The `Kill=Failed` row ("aggregate remains `Failed` even if the domain is later observed empty") is explicitly adopted, so persistence of the true `DomainEmpty` bit on that degraded path should be stated at implementation-authorization time (it affects the next-run component skip decisions).
3. `Reconciler.cs:122` converts the wait duration through a floating-point multiply (`(long)(timeout.TotalSeconds * 1_000_000_000d)`) on top of `TimeSpan`'s 100 ns resolution. For a derived `remaining` below ~100 ns this can collapse the wait to a single poll immediately after the checked deadline condition - behaviorally already equivalent to the urgent one-observation shape. Not a defect of the D6.2 contract; it reinforces the D6.1-era optional deadline-vs-duration overload question and should be handled in implementation review, not by D6.2.
4. The mixed-result detail rule ("the aggregate `Failed` detail names the component operation failure; the `JournalFinalization` row separately records the persistence uncertainty") matches the required precedence and is implementable with the existing `ComponentOutcome` list.

### 5.6 R1 test-obligation verification

The R1-T1..T12 names in C3, the reconciliation register and the matrix additions are mutually consistent, mention only the named existing seams, and forbid sleep/error-text oracles. Row T9's split dispositions (cross-boot/live-lease mismatch `Refused`; restart-only no-live-authority `QuarantinedUnknown`; missing cleaner `Unknown` debt) are consistent with the verified `RecoveryPlanner.Plan` branches and the staging component rules. No new infrastructure is implied beyond what D6.2 already lists.

## 6. R2 - structurally corroborated monotonic clock provenance

### 6.1 R12 source verification

- `Lifecycle/OwnerPreparationTransaction.cs:21-32` - `OwnerClock : ITransactionClock` with `Stopwatch.GetTimestamp()` and the exact integer conversion `(t / Stopwatch.Frequency * 1_000_000_000L) + (t % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency)`. The D6.2 formula is byte-identical to this conversion.
- `Lifecycle/OwnerPreparationTransaction.cs:34-39` - `OwnerDomainClock : IDomainClock` uses `Stopwatch.StartNew()` (`_watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency)`) - construction-relative epoch **and** a different integer conversion. D6.2's "incompatible" claim is verified and, if anything, understates the second mismatch (a truncated divisor multiplier). `git grep OwnerDomainClock` on R12 returns exactly one hit: its declaration. Dead-code claim verified; "no active production failure is claimed" is the correct posture.
- `Launch/BubblewrapAdapter.cs:510-521` - the private `MonotonicNowNanoseconds()` helper implements the same conversion as `OwnerClock`, and `LaunchDomainClock : IDomainClock` uses it. Citation range exact.
- `Recovery/OwnershipValidation.cs:145-175` - `TryValidateStop` requires `StopAcceptedAtNanoseconds > 0`, `StopDeadlineNanoseconds > 0`, a defined cause, and `deadline == acceptedAt + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds` with a `checked` overflow guard. Citation exact; "the record validator prevents rewriting the deadline" posture confirmed.
- `Recovery/OwnershipTransitions.cs:108-151` - `TryPlanStop` refuses a second stop ("the first accepted stop is immutable" at `:117`) and derives the deadline with the same checked five-second allowance. Citation range exact.
- `Recovery/Reconciler.cs:75-97` - cross-boot and identity refusals before any deadline arithmetic. Citation exact.

### 6.2 Verdict on R2

**`VERIFIED ADDRESSED`** with one recording recommendation (non-blocking, see 6.4).

The proposed mechanism binds the D6.1-R2 requirement correctly and no longer relies on an enum label:

1. A sealed internal production clock (`StopwatchDomainClock`) with a constant `MonotonicClockBasis(MonotonicEpoch.Qpc, Stopwatch.Frequency)`, built directly on `Stopwatch.GetTimestamp()` with the exact `OwnerClock` conversion, with no injected epoch and no construction-time rebase. This structurally supersedes the D6.1-era `MonotonicEpoch.Qpc` label convention.
2. The `Qpc` name definition is precise and honest: it names the `Stopwatch.GetTimestamp()` epoch with the exact conversion and frequency; on Linux it is the .NET CLOCK_MONOTONIC-backed Stopwatch source, not a literal Windows QPC device; comparability is only within the same basis and boot context; no OS qualification is claimed.
3. The Reconciler rejects epoch/frequency incompatibility and invalid/overflowing/nonpositive/unavailable inputs before any subtraction and fails closed to `QuarantinedUnknown` without fabricating a window. Cross-boot refusal remains pre-arithmetic via `RecoveryPlanner.Plan`.
4. The `OwnerDomainClock` disposition is correct and additionally verified by the single-grep-hit dead-code check.
5. The R2-A..F test set is behavior-based (compatible epoch, honestly-declared incompatible relative epoch rejected before subtraction, false-label source invariant, cross-boot, invalid conversions, no-reset across repeated reconciliation/new process/partial cleanup), and does not add production extensibility to inject a mismatch.
6. The structural split (production constructor accepts the sealed clock type or a minted trusted capability; the existing internal `IDomainClock` constructor retained only as an explicitly test-only seam; `Basis` check as defense-in-depth; a false `Qpc` label on a test clock declared "test-code deception, not a production path") is a reasonable, honestly-scoped boundary for a single-assembly codebase, and the claim "production composition cannot pass a foreign clock without a source change and review" remains true - writing clock-passing code is a source change wherever it happens.

### 6.3 No contradiction found

The D6.2 text does not overstate R12: it explicitly says `OwnerDomainClock` is dead code, does not claim any current production failure, does not authorize any clock change, and keeps `C3 ZERO-WAIT POLICY - PENDING HUMAN` untouched (Policy A/B remain unselected).

### 6.4 R2 observation (non-blocking)

The production constructor remains `internal`, and this assembly has standing `InternalsVisibleTo` grants, including to the sibling owner executable (`Execution/TransactionStateModel.cs:11-12`: `"Gagamba.Execution.Linux.M1.Tests"` and `"Gagamba.Execution.Linux.M1.Owner"`). The sentence "production composition cannot pass a foreign clock without a source change and review" is true as written (a foreign clock requires writing source code in whatever assembly supplies it), but the record should, at C3 implementation-authorization time, name the internals-visible assembly set and state that the owner assembly can reach the test-only seam through its `InternalsVisibleTo` grant, so that the reviewer-visible boundary is the review requirement rather than the compiler. The `Basis` check remains the runtime backstop against an honestly-formed incompatible clock; only a deliberately deceptive test-faring clock can defeat it, and the record already classifies that as review-code deception rather than a structural guarantee. This observation does not diminish the verdict; it is a disclosure requirement to attach to the future C3 implementation slice.

## 7. R3 - ambiguous retention versus concurrent stop ordering

### 7.1 R12 source verification

All ordering facts were read from the extracted `Execution/ReleaseArbiter.cs` at the exact R12 commit:

- `:45-62` - entry: retained-release `Duplicate` relabel first (`:50-52`), then the pre-intent stop-won refusal `Refused(ReleaseDisposition.NotCommitted, "release request refused: stop already won before durable intent")` (`:55-60`). Both verified strings match the C4 taxonomy and the D6.2 claims.
- `:64-79` - prechecks outside `_gate` (deadtine/prerequisites/liveness/identity), bare `Refused(NotCommitted)` without retention (`:77-78`).
- `:81-115` - the journal intent phase under `_journalGate`: `!_writer.IsLive` (`:83-86`), `ReadLatest` failure/no snapshot (`:89-93`, the confirmed `release request refused: no acknowledged preparation snapshot ({latest.Error})` shape; the sealed-journal `ReadLatest` error `commit seal present: durability ambiguous, explicit recovery required` exists at `OwnershipJournal.cs:780`, matching the C4 D6.2 note's interpolation), preparation identity mismatch (`:96-100`), `TryPlanReleaseIntent` failure (`:102-108`), and `WriteTransition` failure -> `Refused(NotCommitted, "release intent was not durably acknowledged")` (`:110-114`). Every journal-phase failure returns bare, unretained - the retention points are the after-intent gate and the release path, exactly where D6.2 places them.
- `:117-132` - **the critical after-intent block, matches the D6.2 R3-B contract exactly**:
  - `:120-123`: existing `_retainedRelease` first -> return Duplicate.
  - `:125-131`: `if (_stopAccepted) { ConsumeCapability(); _retainedRelease = Refused(ReleaseDisposition.MayHaveExecuted, "release gate refused: stop won during durable intent"); return _retainedRelease; }`. The refusal is **retained in `_retainedRelease`**, so every later release caller receives `Duplicate` of it via `:50-52` or `:120-123`. The D6.2 claim that the stop-won refusal is retained (rather than returned bare) is verified, and its field table matches the verified decision record: `Refused`, `MayHaveExecuted`, `DeliveryResult.NotAttempted`, `DurableCommitted=false`, `LiveExecutionExcluded=true` (helper at `:301-303`), reason string byte-exact at `:130`.
- `:134-146` - gate-expiry/prerequisite/liveness refusal, also retained with `MayHaveExecuted` after `ConsumeCapability()`.
- `:148-155` - consumption-failure refusal, also retained with `MayHaveExecuted`.
- `:157-183` - release-wins path: `_releaseRuntimeCommitted = true`, one transport write with caught IOException/InvalidOperationException/OverflowException mapped to `Unknown`, `liveExcluded` computed from WouldBlock/BrokenPipe/Partial, retained `Released` decision with `DurableCommitted: false` pre-persistence.
- `:185-223` - committed-evidence persistence outside the first `_gate` block (its own `_journalGate`), then the final retention `_retainedRelease = _retainedRelease with { DurableCommitted = durableCommitted, ... }` under `_gate` (`:215-223`). No journal I/O occurs inside any `_gate` block, confirming the arbiter header invariant (`:1-5`).
- `:226-297` - `RequestStop`: retained-stop duplicate first (`:232-235`); the clock-unavailable path (`:237-244`) and the overflow path (`:246-261`) both accept live, set `_stopAccepted = true`, retain a `StopDecision` with no durable timestamp/deadline, and return; the normal path (`:263-267`) likewise. The durable-stop mutation attempt is a separate, later `_journalGate` block (`:270-289`) with `DurableStop = write.Ok` folded into the final retained stop under `_gate` (`:292-296`). `RequestStop` never writes `_retainedRelease`.
- `:23-27` - `_retainedRelease` and `_retainedStop` are separate fields, as the D6.2 cross-contract claims; the stop slot never populates the release slot.
- `Preparation/PreparedTransaction.cs:60-69` - `TryConsume` is store-mediated ("The release arbiter is the only production caller; recovery cannot reconstruct a consumed capability"), matching D6.2's store-local consumption attribution, and `ConsumeCapability()` (`:305-308`) discards the violation anyway.

### 7.2 Verdict on R3

**`VERIFIED ADDRESSED`.** The D6.1-R3 requirement - replace the unconditional "ambiguous retention" statement with an explicit first-retainer collision rule - is met, precisely:

1. The three orderings are specified: ambiguity-first (`AmbiguousPersistence` retained by the ambiguous caller), stop-first (the stop's acceptance is authoritative; the ambiguous caller retains the existing stop-won refusal rather than manufacturing a second ambient result), and already-retained (`Duplicate` of the prior decision at entry and, defensively, in the after-intent block).
2. The stop-first runtime result is fully R12-conformant: retained `Refused(MayHaveExecuted, NotAttempted, DurableCommitted=false, LiveExecutionExcluded=true)` with the byte-exact reason `release gate refused: stop won during durable intent`, later callers on `Duplicate` - none of this is asserted beyond what the source shows.
3. The durable separation is correct and, if anything, conservative: a caller that reaches the after-intent `_gate` has already written an acknowledged ReleaseIntent (any journal-phase failure returns at `:110-114`), so the stop-won refusal never proves the absence of durable Intent, and the sealed-journal `MayHaveExecuted` recovery classification stands. "The journal ambiguity remains real regardless of which runtime result wins" is the correct framing, and "definite NotCommitted may not erase it" matches the R12 refusal shapes.
4. No second Intent, no second FD4, no new waiting protocol, no reentrancy assumption, no lock inversion, no journal I/O under `_gate`: all verified against the actual lock and I/O structure above. The R3-T1..T8 barrier table is compatible with the existing `TestBarrier` hook (`:299`) without new production surface.
5. The ordering summary table's three rows (first retainer, later-caller results, capability/effects, durable classification) match the verified code paths row by row, including "C2 consumption at first stop acceptance" as the stated C2-contract linearization point, and the row-level note that a prior retained decision at entry short-circuits before any journal phase.

### 7.3 R3 observations (non-blocking)

1. The R3-B case text mixes R12-grounded statements with post-C2 composition semantics in one paragraph ("The stop already consumed the capability at its first acceptance; the release caller's existing consume attempt is a no-op"). At R12 the stop does **not** consume anything - `RequestStop` only sets `_stopAccepted` - and the release caller's `ConsumeCapability()` at `:127` would be the first (and consuming) call. Under the composed C2+R3 contract the wording is correct, because C2 (a separate, already-reviewed proposal, not authorized) moves consumption to first stop acceptance. The R12/D2 fixture name `StopFirstLeavesCapabilityLiveOnR12` documents exactly this composition gap. Recommendation: add one clause naming "under the C2 contract" in that sentence so a reader cannot take "no-op" as a description of R12; no ordering rule changes either way.
2. R3-C's honesty markers ("defensive first-retainer rule, not a claim that the state is normally reachable after the journal phase"; no manufactured prior stop-won result; no unreachable state as a test premise) are correct and are preserved without weakening the D6.1 first-retainer rules.

## 8. R4 - diagnostic test-fixture source provenance

### 8.1 Identity verification table (all checks executed during this review)

| Claim (D6.2 C1 table) | Independent check | Result |
|---|---|---|
| Fixture branch `implementation/wp05-release-qualification-fixtures` | `git rev-parse` of the branch name resolves | PASS |
| Branch tip equals evidence tip `338a98af269e6b5839f3ccc2e0881f016153b01e` | `rev-parse` branch tip and raw SHA compared | PASS (identical) |
| Test commit `dc4c372b180abef1b27038d94f49af90226fa548` | resolves on the fixture branch | PASS |
| Parent `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c` | `git rev-parse dc4c372^` | PASS |
| Commit tree `1f1c8669affd63ae2fc48d20429241e505751000` | `git rev-parse 'dc4c372^{tree}'` | PASS |
| R12 tree `6854675d6b37a49d5302d6104103bdbfbc268040` | rechecked at R12 | PASS |
| Test file path `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` | path listed in the fixture commit's tree; **absent** from the R12 tree (`git cat-file -e` rc=128) | PASS (test-only attribution correct) |
| Blob `ccde61763664a4ed56e6d1941c20d61cc0cf2b5f` | `git rev-parse dc4c372:<path>` | PASS |
| SHA-256 `cd5cd00c4c4ef213239047a85ed8f26129e45bb5cad0ddd2c3fe646a407da2b4` | `git cat-file blob` over raw bytes (cmd redirect) + `certutil -hashfile SHA256` | PASS |
| Fixture `src` tree `406f52da43b62920128151b2f5706c32d45b6db7` identical to R12 `src` | `git rev-parse dc4c372:src` == `git rev-parse 38f6981b:src` (both `406f52da...`), plus `git diff --exit-code 38f6981b dc4c372 -- src native` rc=0 | PASS |
| Fixture `native` no diff from R12 | included in the same `-- src native --exit-code` check | PASS |
| Evidence manifest blobs: `SHA256SUMS` `e203b5c3954d5c4e27b2cc17478208f9f174999c`, `source-head.txt` `e35e47640dd95394ec6bf4159dbad5f46ed67c37` | `git rev-parse <tip>:<path>` | PASS |
| Evidence document blob `fd7dc44eb7815f5747ac860bf2e6eb6749bb17bc`; diagnostics log blob `03beaf8863099d96cdfb8ce82297398cbf9d6083` | same method at the evidence tip | PASS |
| 13 test method names | full method-name extraction from the blob (`:98, :118, :135, :166, :183, :198, :215, :236, :268, :288, :308, :324, :342`) - all 13 D6.2 names found, no extras, exact ordering match | PASS |
| No fixture copied or merged into D6.2 | D6.2 diff list (section 3) contains no test/fixture file | PASS |

Additional structural check that D6.2 does not claim and correctly avoids: the fixture commit is not a descendant of the D6.2 lineage's R12 tip (the R12-to-dc4c372 diff shows its own docs/eng/tests/harness divergence), so the "identical `src`/`native` trees" statement is a tree-identity claim, not an ancestry claim - and that is exactly how the record phrases it ("`src` identical to R12"; "no false ancestry").

### 8.2 Verdict on R4

**`VERIFIED ADDRESSED`.** The consumer inventory split (R12 production symbols vs test-only fixture asset), the exact branch/commit/file/blob/digest binding, the tree-identity framing, the evidence-manifest binding, and the no-copy/no-merge rule all verify against the actual Git data. The inventory's production-side list (`ReleaseDecisionKind`/`ReleaseDecision` in `Execution/ReleaseDecision.cs`; the arbiter retained/return paths; the three named R12 test families; no production switch/serializer consuming the runtime kind) matches the verified `ReleaseDecision.cs` vocabulary and arbiter structure read for R3. The fixture's declared role ("evidence of R12 behavior only; does not qualify future changed production") is consistent with its identity.

## 9. Cross-contract re-verification

The six interactions, re-checked only where D6.2 could have changed them:

| Pair | D6.2 claim re-checked | Verified basis | Verdict |
|---|---|---|---|
| C1 (x) C2 | Stop-first consumption at first acceptance is the C2 linearization point; the ambiguous caller's report is conditional; tail stop idempotent | C2 D6.1 consumption section (unchanged), C1 D6.2-R3; arbiter `:125-131, :232-235`; `PreparedTransaction.cs:60-69` | CONSISTENT (one wording recommendation, 7.3-1) |
| C1 (x) C3 | Ambiguity/stop outcome does not fabricate a StopRecord or a fresh window | C3 D6.2-R1/R2 fail-closed rules; no fabricated-deadline statement; arbiter/Reconciler separation | CONSISTENT |
| C1 (x) C4 | Stop-won refuse retained; sequential `Duplicate`; one physical FD4; no retry-to-second-write; taxonomy rows exact | C4 taxonomy table vs arbiter `:55-60, :92-93, :128-131, :301-303`; sealed-journal ReadLatest error at `OwnershipJournal.cs:780` | CONSISTENT |
| C2 (x) C3 | Clock/finalization uncertainty cannot revive release authority; consumption independent | C3 fail-closed -> `QuarantinedUnknown`; release authority is the capability/journal path, untouched by clock rejection | CONSISTENT |
| C2 (x) C4 | Stop-first and release-first histories preserved; no new `ConcurrentRefusal` or waiting protocol | C2 D6.2 cross-reference; C4 D6.2 note; both explicitly non-reopening | CONSISTENT |
| C3 (x) C4 | Duplicate/reconciliation does not reset the immutable deadline | Validator/transition immutability (`OwnershipValidation.cs:145-175`, `OwnershipTransitions.cs:117`); R1-T12/R2-F no-reset rows | CONSISTENT |

No new contradiction was introduced by D6.2 in any pair.

## 10. Findings and disposition summary

| ID | Object | Verdict | Disposition |
|---|---|---|---|
| D6.2-R1 | C3 D6.2-R1 component mapping table vs retained fallback rule | PARTIALLY ADDRESSED | Required documentation-only row/paragraph reconciliation (section 5.4); no semantic redesign; aggregate truth table unchanged |
| D6.2-R2 | C3 D6.2-R2 | VERIFIED ADDRESSED | Recording recommendation on the internals-visible assembly set at C3 implementation authorization (section 6.4); non-blocking |
| D6.2-R3 | C1 D6.2-R3 | VERIFIED ADDRESSED | Wording recommendation to mark "consumed at first stop acceptance" as C2-contract semantics in R3-B case 4 (section 7.3-1); non-blocking |
| D6.2-R4 | C1 D6.2-R4 | VERIFIED ADDRESSED | None; full binding verification table in section 8.1 |

Non-blocking observations carried for the future implementation slices: R1 item (2) persistence of the `DomainEmpty` bit on the degraded kill-failed path and the floating-point wait conversion (5.5-2/3); R2 item 6.4; R3 item 7.3-1. None alters the dispositions above.

## 11. Review completeness and integrity self-check

1. Review target is the exact producer HEAD `ae09afedddeb703e6bde226bd793b375896c3064`, verified by `git rev-parse HEAD` in the review worktree at the point of review.
2. The review worktree stayed clean between steps; no producer file was amended, rebased or touched.
3. All R12 citations were read from fresh `git show` extraction at the exact R12 commit; no cached source was trusted for a binding claim.
4. The four primary questions (GLM-R1..R4 in the handoff) were each answered with a distinct verdict in section 10.
5. The handoff's "what not to re-review" list was respected; no gate acceptance, qualification, or policy selection appears anywhere in this document.
6. The C3 expired-deadline policy remains explicitly unselected (`C3 ZERO-WAIT POLICY - PENDING HUMAN`).
7. C2/C4 substance was not reopened; only their D6.2 cross-references were verified as consistent.
8. Every required correction is documentation-only; no production, test, native, engineering or fixture change is required or proposed by this review.
9. The R4 verification used byte-faithful hashing; the PowerShell redirect pitfall is recorded so the result is reproducible.
10. Cross-contract re-verification conclusions are anchored to verified lines rather than restatements of D6.2 prose.
11. The first review commit of this branch will have `ae09afed...` as its immediate parent; no review content is mixed with producer content in one commit; each commit adds exactly one document per its phase.
12. The D6.2 producer's own summaries (reconciliation register, handoff, addendum, matrix) were read in full; no wording was found to claim approval, authorization, qualification or a passed gate anywhere in the D6.2 diff.

## 12. Next bounded action

For the D6.2 design author: produce the D6.2-R1 documentation-only correction (one row/paragraph reconciliation in C3, plus optionally folding in the two non-blocking wording recommendations D6.2-R2/D6.2-R3), then the human holder can complete the C1-C4 approval questions and the C3 policy selection in the addendum sheets. Nothing else stands between this review and the human decisions; no production work is authorized by this review in any case.
