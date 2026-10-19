# Independent Adversarial Review of the WP05 Release Qualification Plan

Author: GLM (independent adversarial plan reviewer; not an implementation author of the reviewed documents)
Date: 2026-10-19
Status: documentation-only review; no implementation, qualification execution, or production change authorized or performed by this review

## 0. Overall disposition

**`PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES - TEST-ONLY FIXTURE WORK AND DIAGNOSTIC PROBES AUTHORIZED; GATE ACCEPTANCE BINDING DECISIONS REQUIRED BEFORE ANY TAGGED GATE (G3-A/B/D/E) IS MARKED ACCEPTED. NO RELEASE QUALIFICATION IS ESTABLISHED OR CLAIMED.`**

The DeepSeek planning slice (A1 gap audit, A2 requirements matrix, A3 execution plan, decision queue) is boundary-correct, source-accurate on all four load-bearing defect claims (P1/P2/P3/P4), and safely conservative in its exclusion structure. It is acceptable for use as the working plan for the bounded WP05 release qualification slice, under the required changes in `docs/reviews/gagamba-linux-m1-wp05-release-plan-required-changes.md` (C1-C9). The three documents need no structural rewrite; the required changes are binding decision requirements, fixture-spec clarifications, and test additions. No claim in the plan may be promoted from "planned" to "observed" on this review's authority.

This review is bound to exact revisions; every source claim below was verified directly at the product revision, not taken from the plan documents.

## 1. Binding and scope

- Branch under review: `planning/wp05-release-protocol-qualification`
- Review base (A3): `2c66eac1da24d0bb9ce706b618abfffdc2d5a437` (verified tip of the planning branch; review performed against the checked-out planning branch)
- A2: `0542b1ac62406504433f65fa7bb48b5f0787891a` (requirements matrix)
- A1: `ffe3c37edc84f48b0157d7b4a1b84dd24a0d739b` (gap audit)
- Decision queue: included in the A3 commit (`docs/plans/gagamba-linux-m1-wp05-release-decision-queue.md`, Q01-Q12)
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R11: `c48e358451c8b0dbe7aace281f64a2eced399ed7`
- R12: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`
- Canonical preservation root: `86eee0d9362fc2785c4d7997fec377b7a33c7767`
- Frozen harness reference: `review/wp05-opencode-harness-final-qualification` at Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f`, disposition `WP05 HARNESS QUALIFIED (bounded)` - not reopened here.

### 1.1 Verified git facts (first-hand)

- R12 `38f6981b...` is an ancestor of A3 `2c66eac1...` (git merge-base check). The plan's "exact R12 binding" is therefore self-consistent: the same tree is reviewable and testable.
- `86eee0d` (preservation root) is an ancestor of A3; the diff `86eee0d`..`A3` touches documentation only (4 new plan/review documents plus handoff/work-queue edits); no production, test, fixture, runner, or native file changed. DeepSeek's "documenting only, nothing implemented" claim is confirmed at commit granularity.
- The preservation commit `169e4e89...` records the frozen harness disposition
  verbatim and binds Q0-Q3; this review treats that boundary as closed.

### 1.2 Review scope and method

- Method: targeted source re-derivation of every load-bearing claim (Part-level verification of the audit's assertions against the R12 tree, checked out clean at a read-only mirror), plus adversarial interleave analysis of `ReleaseArbiter.RequestRelease`/`RequestStop`/`OwnershipJournal.Write` under the `_gate`/`_journalGate` protocol.
- Read first-hand: ADR 0004 (accepted rev 2), ADR 0003 (stop-condition list binding "release after a won stop" as a milestone stop condition, line ~409), implementation plan sections 6/8/13 and WP05-WP10 boundaries, the A1/A2/A3 documents and decision queue, `Execution/ReleaseArbiter.cs` (full), `Recovery/OwnershipJournal.cs` (write/seal/resolve/TryRecoverCommit regions), `Recovery/OwnershipTransitions.cs` (full), `Recovery/Reconciler.cs` (RecoveryPlanner + Reconciler stop path), `Recovery/OwnershipRecord.cs` (record shapes), `Recovery/OwnershipValidation.cs` (stop validation), `Launch/HeldExecution.cs` (full), `Launch/ReleasePipeWrite.cs` (full), `Execution/ReleaseObservationBuffer.cs` (ordering/commit gating region), `native/linux-m1/exec-coordinator.c` (main loop), `tests/Fixtures/wp05-qualification-coordinator.c` (full), `tests/Gagamba.Execution.Linux.M1.Tests/Execution/ReleaseArbiterTests.cs` (all 16 tests), `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs` (all 3 facts), plus the preservation checkpoint.
- Out of scope: running any qualification (test-only or diagnostic probes); modifying any file outside this review's branch; re-opening the frozen harness; demanding WP08/WP09/WP10 behavior at WP05.

## 2. Verdict per reviewed document/topic

| # | Topic | Verdict | Basis (first-hand) |
|---|---|---|---|
| 2.1 | A1 audit: binding/branch/tree claims | **Confirmed** | Git checks (1.1); document set matches the working tree |
| 2.2 | A1 §2 call graph: owner has no release call; release call graph is test-owned | **Confirmed** | `OwnerPreparationTransaction.cs:355-395` (HeldReady/AbortHeld only); `IHeldLauncher.cs:7-24`; only callers of `ReleaseArbiter` are the two test files |
| 2.3 | A1 §2.3: production coordinator stops at pre-exec; FD5 status exists only in the test qualification coordinator | **Confirmed** | `exec-coordinator.c:204-205` exits 0 after exactly-128-byte validated RELEASE (extra bytes rejected `fail(41)`; invalid/expired `fail(42)`); no EXEC/ROOT emission; status modes only in `wp05-qualification-coordinator.c` |
| 2.4 | A1 §3: FD4 write is the first irreversible effect; intent/committed ordering | **Confirmed** | `ReleaseArbiter.cs:118-223` (intent outside gate, gate, consume, one write, committed mutation) |
| 2.5 | P1 - R27 deadline not consumed | **Confirmed, sharpened** | `StopRecord.StopDeadlineNanoseconds` has zero consumers in the whole component set; only the codec writes it (`OwnershipRecord.cs:122,321`) and the validator enforces acceptedAt+5s exactly (`OwnershipValidation.cs:145-181`); `Reconciler` uses caller `_stopAllowance` (`Reconciler.cs:124,129,231`); no production caller computes remaining allowance. This is the confirmed gap; Q04's verdict: correct. Sharpening: ADR 0004's no-reset holds formally (the validator forbids rewriting the deadline), but nothing enforces the no-extension in effect, because the allowance the reconciler uses is caller-supplied. Q04's verdict: correct. |
| 2.6 | P2 - R08 stop-first does not invalidate the capability | **Confirmed, bounded** | `RequestStop` never touches the capability (`ReleaseArbiter.cs:226-298`); post-stop `RequestRelease` returns at `55-61` without consuming; no test asserts `IsLive == false` after stop-first. Bounding: two structural blocks prevent release after a winning stop in the accepted single-arbiter model - `_stopAccepted` (runtime) and `TryPlanReleaseIntent` refusing "intent after a durable stop" (`OwnershipTransitions.cs:27-31`, journal enforced by `OwnershipTransitionValidator`). The residual unblocked window is a second arbiter instance sharing journal+capability when the first stop was accepted at runtime but its durable stop persist FAILED or the clock was unavailable (see CE-03). Then a "fresh-instance" release can plan an intent. This is a defense-in-depth breach of plan lines 150/310/326, not a demonstrated release-after-won-stop. Q05 verdict: correct. |
| 2.7 | P3 - R09 ambiguous intent collapsed to NotCommitted | **Confirmed, sharpened** | `OwnershipJournal.Write` returns failure at `486-490` only AFTER both slots durably hold the fully fsynced new generation (`446-477`); `ReleaseArbiter.cs:110-114` collapses every `WriteTransition` failure to `NotCommitted` without a stop request; the test `FailedIntentPersistenceForbidsRelease` (`ReleaseArbiterTests.cs:282-292`) injects only a definite failure and (correctly for that case, wrongly for the ambiguous case) asserts `NotCommitted`. Sharpening (CE-02): after an ambiguous seal-release failure the journal is wedged for the live owner; a later same-owner stop `WriteMutation` cannot persist (resolve gate, `OwnershipJournal.cs:567-573`) and `TryRecoverCommit` is refused while the writer lease is live (`OwnershipJournal.cs:716-721`), so the mandatory "requests stop" (plan line 209; ADR 0004 §6 step 2) has no obvious persistence channel in this window; runtime `_stopAccepted` still governs release, and on owner death the both-slots-equal recovery recognizes the intent (`OwnershipJournal.cs:741-745`), keeping classification MayHaveExecuted. Q06 verdict: correct and incomplete (stop-request mechanics under a wedged journal must be defined - C1). |
| 2.8 | P4 - R02/R06 concurrent duplicate may receive Refused instead of Duplicate | **Confirmed** | The retained release decision is installed only after the gate phase (`ReleaseArbiter.cs:176-223`), while the entry-slot duplicate check reads it at `48-62` before the intent phase; a caller that passes the entry check while the first caller is at the intent phase can lose the `_journalGate` intent race to the first caller and return `Refused` while a durable intent exists and the winner completes the release. No second-write hazard: journal refuses a second intent ("release intent already exists", `OwnershipTransitions.cs:21-25`), the gate serializes the winner, and `HeldExecution.TryWriteRelease` is `Interlocked`-guarded one-shot (`HeldExecution.cs:45-50` with `AlreadyAttempted`). Also verified (CE-01, registered): outside-gate refusals (`77,85,92,98,106,112`) are never retained, so refusals at different points have different retry semantics; sequential duplicate at the gate returns the original decision relabeled `Duplicate` (`48-55`). Q07 verdict: correct. |
| 2.9 | A2 matrix: counts (31 rows; 27 mandatory; 15 IT, 6 P, 6 UT, 4 D; 0 IQ/M/C) | **Confirmed as self-consistent** | Row statuses match the verified source/test reality; the "Q3 3/3 is not IQ" exclusion is correct; the P set {R02, R08, R09, R16, R27, R30} matches my verification (R16's quoted gap - no actual FD5 EOF/replay native case - is consistent with the qualification-coordinator-only status source); the UT set {R11, R21-R25} matches the in-process-exception crash-barrier reality (`ReleaseArbiterTests.cs:237-280`, no fresh-process release test). |
| 2.10 | A2 gate mapping and A3 sub-gates G3-A..G3-E | **Confirmed as planning labels** | A3 §1 and A1 §1.1 correctly subordinate G3-A..G3-E to G2a/G2b/G3; ADR 0003 grants them no authority; use of RQ-A..RQ-E (Q01) is fine. |
| 2.11 | A3 §4.1 fixture set (release owner process, supervisor, transport witness, status bridge, fault wrappers) | **Sound, with clarifications (C5, C6, C9)** | Reuses verified seams (`TestBarrier` `ReleaseArbiter.cs:23,299`; real journal; real adapter/execution; qualification coordinator). The witness-before-write design (Q08) is sound: it survives owner death and detects a second attempt even on a closed pipe. Required clarifications: fault wrappers must explicitly support the *ambiguous* seal-release injection (seal unlink failure) distinct from definite failure; "retained recovery scope/lease" wording must not imply lease inheritance across processes; status bridge must preserve frame identity/order. |
| 2.12 | A3 §7 G3-C fresh-process design | **Sound and necessary** | Verified need: no fresh-process release test exists; the existing "crash" tests throw in-process exceptions (`ReleaseArbiterTests.cs:237-280`) and `NewArbiterCannotResendOverDurableIntent` mints a new capability in-process (`331-344`). The orchestration (SIGKILL at named barriers, pidfd death confirmation, replacement with no capability/writer, witness ledgers) matches the ADR/plan requirements. The fixture must be killable at the *named internal barriers*, not at arbitrary sleep points (already stated in the plan; keep as acceptance criterion). |
| 2.13 | G3-D status scope (qualification-coordinator-only, production status WP10) | **Confirmed** | Matches verified production coordinator boundary and the status source reality; D08 correctly refuses production status claims. |
| 2.14 | Q02/Q03 (test-only owner/driver/coordinator) | **Correct** | Consistent with verified owner path (no release call) and coordinator pre-exec boundary; WP10 exclusion preserved. |
| 2.15 | Q10 (35 root-only auxiliary rows contain no mandatory release row) | **Accepted** | Matches the harness Q2/Q3 evidence domains (IntegratedBarrierTests 14, DomainAuthorityTests 19, RecoveryHarness 2); no release-row reclassification without a rejected-probe decision. |
| 2.16 | Q12 (acceptor role split; DeepSeek not self-accepting) | **Correct** | Consistent with repository review/preservation conventions; this review's authorization does not self-accept any future gate result. |
| 2.17 | A3 §9 disposition candidate string and §13 stop conditions | **Correct** | "bounded pre-exec" qualification string is not an authorization or claim; the stop-condition list covers every verified defect and the CE-register items. |
| 2.18 | Missing from plan: clock-unavailable stop / stop persist-failure semantics (CE-03) | **Added by this review** | `RequestStop` accepts stop with no durable evidence when the monotonic clock is unavailable (`ReleaseArbiter.cs:238-243`) or the deadline overflows (`254-261`), recording `StopDeadlineNanoseconds = 0`; no existing test covers it; G3-A02/A03 should include these as negative/decision cases (C5). Not a release-authority hazard in the single-arbiter model, but it (a) breaks the deadline-exact record contract at the component seam and (b) widens the CE-03 multi-instance window. |

No contradictory evidence against the frozen harness boundary was found anywhere in the planning slice or the product tree.

## 3. Adversarial checks beyond the plan's own claims

These were performed this review independent of DeepSeek's write-up:

1. **Gate/journal interleave audit of `RequestRelease` under stop** - the intent phase runs outside `_gate` by design ("never await journal I/O while holding gate"); a stop accepted in that window re-seizes the gate, sets `_stopAccepted` + durable stop first, and the release caller re-enters the stop check before any FD4 write (`125-132`), consuming the capability on refusal. Result: the manufacture of "release after stop" requires breaking both the runtime flag and the journal transition validator; neither break was found in the tree. The plan's G3-A03 test is well-chosen for this window.
2. **Journal-level gate: a second intent is impossible.** `WriteTransition` validates against the resolved acknowledged baseline and the transition set; intent-after-stop and intent-after-intent refusals are enforced at the journal layer independent of the arbiter (verified planners at `OwnershipTransitions.cs:21-37`). This is the load-bearing reliability invariant bounding P2/P4 severity.
3. **Seal/recovery correctness for ambiguous outcomes** - `Resolve` promotes nothing while a seal exists (uncertainty is chosen over highest visible sequence); `TryRecoverCommit` acknowledges only (a) both slots decodable (lower generation) or (b) both absent (genesis); lone/uninterpretable slots remain refused with the seal intact (operator resolution). The intent content is fully written to both slots before the ambiguous unlinking, so a crash inside that window recovers to Intent (MayHaveExecuted), never NotCommitted. No lost-fact violation found in the seal protocol itself.
4. **One-shot transport re-check** - `HeldExecution` marks the attempt before the write; `WouldBlock/BrokenPipe/Partial/Unknown` cannot be retried at the component (`AlreadyAttempted`); mapping in `ReleasePipeWrite.cs:37-59` matches the plan's conservative classification table (`Delivered` only on full 128-byte write; `Partial`/zero-byte/errno-default → `Unknown`).
5. **Committed-write failure behavior (R11)** - verified the arbiter does not resend on committed-write failure and appends the reason while returning the runtime decision; the delivery already happened and conservative classification rules govern later reads. This is currently untested (UT row R11 stands).

## 4. Refusals (what this review refuses to accept)

1. Any promotion of G3-A/B/D/E to "accepted" while R02/R08/R09/R27 keep unresolved remediation decisions; the plan itself requires it and this review binds it.
2. Any evidence composition that reuses the 3/3 Q3 test-path runs as row-level IQ evidence; the A2 exclusion is correct and stays binding.
3. Any implementation of R08/R09/R27 remediation and the G3-C fresh-process fixture inside this planning slice without separate, reviewed authorization (plan §10 constraint preserved).
4. Use of `TestBarrier`-in-process exception tests in place of the required SIGKILL fresh-process cases for G3-C (plan §13 stop condition; matches the verified in-process reality of `ReleaseArbiterTests` crash tests).
5. Self-acceptance of the future bounded disposition by the implementation author (Q12 binding).

## 5. Required changes

The numbered required changes C1-C9, each bound to a matrix row/topic and to a decision from the queue, are in `docs/reviews/gagamba-linux-m1-wp05-release-plan-required-changes.md`. Counterexample-level detail (CE-01..CE-04) with exact interleavings is in `docs/reviews/gagamba-linux-m1-wp05-release-plan-counterexample-register.md`. Severities there are bounded by the verified structural mitigations in section 3 above.

## 6. What the next slice may start on under this disposition

1. Fixture-author commits for the test-only fixture set per the C5/C6/C9 clarifications only (no production/test-code changes beyond test-only trees).
2. Diagnostic G3-A/G3-B/G3-D probe preparations (no acceptance tagging), including the concurrent-duplicate deterministic test as a probe-grade test (C4), and the goal-not-a-gate R27 candidate probe (per Q04: remediation or explicit limitation decision required before any R27 pass).
3. No execution of any gate as "passed" until the C1-C4 decisions are recorded as resolved by their required decision-holders, per the plan's own §4.2 and §14.

The plan documents remain the working plan; this review grants no implementation authorization beyond the test-only fixture work it explicitly requires, and does not alter the WP05/WP08/WP10 boundaries or the full-G3/WP10 closure path.

**Final string for this review document, bound above: `PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES`.**
