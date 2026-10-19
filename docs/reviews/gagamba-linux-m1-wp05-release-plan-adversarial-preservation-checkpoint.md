# WP05 Release-Plan Adversarial Review — Preservation Checkpoint

Date: 2026-10-20
Status: documentation and Git preservation only. No implementation, remediation, qualification execution, or production change was performed by this checkpoint, and none is authorized by the review it preserves.

## A. Repository binding

- Canonical root: `C:\Users\Laszlos\source\repos\Gagamba`
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R11: `c48e358451c8b0dbe7aace281f64a2eced399ed7`
- R12: `38f6981b2a60daab2df37c92913a6760c1bef131` (tree `6854675d6b37a49d5302d6104103bdbfbc268040` verified via `git rev-parse ^{tree}`)
- Planning branch: `planning/wp05-release-protocol-qualification` — A1 `ffe3c37edc84f48b0157d7b4a1b84dd24a0d739b`, A2 `0542b1ac62406504433f65fa7bb48b5f0787891a`, A3 `2c66eac1da24d0bb9ce706b618abfffdc2d5a437` (branch tip; the exact planning revision that received independent review)
- Independent review branch: `review/wp05-release-plan-adversarial-review` — V1 `6838f922bc2a2ca32f11c8fe06274762871ca69d`, V2 `bca1b884b189bcb72a665c28674bc35d9f40c552`, V3 `e4b0a798e944ddfb144901c63b0c444e7980533b`. Ancestry verified: V2 descends from V1, V3 descends from V2, A3 descends into V1 (A3 is the parent of V1), R12 descends into A3; all reachable objects via `git fsck --connectivity-only` (two unrelated pre-existing dangling objects, `a2d0e647…`/`d2b34a88…`, are not part of the review ancestry).
- Review document blob identities (V3 tree): adversarial review `f1a062e1e12895426dcfb8b7b0b13a2e6d933918`, counterexample register `0bbafa6b9725cce8a20858e1d64a8deec6134f15`, required changes `73eb09af27c5dbc896739ebf48489862ec13e0a9`. A3→V3 diff is documentation-only: exactly these three files, 212 insertions, no production/test/runner/native change, A1–A3 documents byte-unchanged.
- Canonical review reference: `refs/heads/review/wp05-release-plan-adversarial-review` points to exact V3; no tag conflicts (`refs/tags` holds only unrelated historical tags). The review branch was not rewritten.
- This preservation branch: `preservation/wp05-release-plan-adversarial-review`, based on exact A3 `2c66eac1…` (the planning revision that already contains A1–A3); it deliberately does not contain the review documents, which are preserved as separate review history under the review branch.
- Frozen harness (not reopened): `review/wp05-opencode-harness-final-qualification` at Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f`, preservation root `86eee0d9362fc2785c4d7997fec377b7a33c7767`, disposition `WP05 HARNESS QUALIFIED (bounded)`.

## B. Review disposition

**`PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES`**

The full binding string in section 0 of the review adds: test-only fixture work and diagnostic probes are authorized; gate-acceptance-binding decisions are required before any tagged gate (G3-A/B/D/E) is marked accepted; no release qualification is established or claimed. This means the A1–A3 analysis is sufficiently grounded to guide the next step with blocking contract and fixture decisions identified. It does NOT mean the execution plan is unconditionally approved, C1–C9 are satisfied, production remediation is authorized, WP05 release qualification passed, any G3 release gate is accepted, or WP08/WP10 work completed.

The review documents are authoritative; this checkpoint references them rather than duplicating them:

1. [Independent adversarial review](#) — `docs/reviews/gagamba-linux-m1-wp05-release-plan-independent-adversarial-review.md` (review branch, blob `f1a062e1…`)
2. [Counterexample register](#) — `docs/reviews/gagamba-linux-m1-wp05-release-plan-counterexample-register.md` (review branch, blob `0bbafa6b…`)
3. [Required changes](#) — `docs/reviews/gagamba-linux-m1-wp05-release-plan-required-changes.md` (review branch, blob `73eb09af…`)

## C. Verified source findings (reviewer classifications preserved)

All four findings were first-hand source re-derivations at R12. No runtime reproducer was executed by the review; none is claimed. Classification vocabulary: source-confirmed / source-derived / hypothesis-requiring-probe / qualification-design limitation / executed counterexample.

- **P1 — R27 stop-deadline consumption (source-confirmed gap).** `StopRecord.StopDeadlineNanoseconds` is validated (exactly acceptedAt + 5,000,000,000 ns) and persisted, but has zero consumers; `Reconciler` stops the domain using a caller-supplied `_stopAllowance` (`Reconciler.cs:124,129,231`); `OwnershipValidation.cs:145-181` enforces immutability. No production caller converts the persisted deadline into a remaining allowance. ADR 0004 §9.1's no-reset holds formally at the validator; the missing consumer is a production-contract gap requiring an explicit remediation-or-accepted-limitation decision and probe before R27/G3-A10 acceptance. This is a source-confirmed missing consumer, not an executed deadline-reset counterexample.
- **P2 — R08 stop-first capability lifetime (source-confirmed, bounded).** `RequestStop` never consumes the capability; post-stop `RequestRelease` refuses without consuming (`ReleaseArbiter.cs:55-61,226-296`); no test asserts `IsLive == false` after stop-first. Verified protections that bound the risk: runtime `_stopAccepted` blocks release in the same arbiter; `TryPlanReleaseIntent` rejects release intent after a durable stop (`OwnershipTransitions.cs:27-31`); the accepted owner model is single-arbiter. The residual concern (CE-03) is a second arbiter instance when stop persistence fails or the clock is unavailable. Stop-first capability reuse is NOT an unconditional vulnerability; it is a defense-in-depth contract breach of plan lines 150/310/326 requiring a decision before R08 acceptance. Existing valid protections must not be removed by the fix.
- **P3 — R09 ambiguous ReleaseIntent persistence (source-confirmed, load-bearing).** Exact failure ordering: the intent generation is fully written and fsynced to both journal slots before the seal unlink; if the unlink fails, `Write` reports failure while the intent content is durably present in both slots (`OwnershipJournal.cs:486-490`); the arbiter collapses every intent-write failure to `Refused(NotCommitted)` without requesting stop (`ReleaseArbiter.cs:110-114`); a later same-owner stop cannot persist while the seal is unresolved and recovery is refused while the live lease holds the fence (`OwnershipJournal.cs:567-573,716-721`); on owner death, recovery acknowledges the both-slots-equal Intent (`741-745`) and classification is conservative `MayHaveExecuted`. The durable truth (intent may exist) and the returned error (commit ambiguous / not durably acknowledged) differ. No second physical RELEASE was observed or is claimed; the physical release path stays blocked throughout. Evidential/contractual production-contract concern requiring an explicit decision and qualification.
- **P4 — R02–R06 concurrent duplicate semantics (source-derived).** One logical release can produce three decision kinds: winner `Released`, concurrent loser `Refused` (reason "release intent already exists", `OwnershipTransitions.cs:21-25`), later caller `Duplicate`. Existing journal fencing (baseline/sequence/transition validation) and one-shot transport (`HeldExecution.cs:45-50`, `Interlocked` before write) protect against a second intent and a second physical write. This is a decision/API-consistency finding, NOT a demonstrated double-release defect; a contract decision (Q07) is required.

## D. Counterexample register (exact evidence classification)

All four register entries are source-path findings derived from R12 code without executing a reproducer. Classification for all: source-derived counterexample (experiment required); implementation status: Not implemented; acceptance status: Not accepted.

| ID | Source | Finding (reviewer-supported) | Classification | Risk (bounded) | Required evidence |
|---|---|---|---|---|---|
| CE-01 | `ReleaseArbiter.cs` outside-gate refusal sites (77/85/92/98/106/112) vs gate refusals (125-155) | Outside-gate refusals are never retained; gate refusals are retained and relabeled Duplicate — the same API has two idempotency contracts split by failure mode | Source-derived; experiment required | API-consistency only; no second write in either branch | Deterministic tests distinguishing both contracts; G3-A05 must not assert one uniform "duplicate/identical decision" |
| CE-02 | `OwnershipJournal.cs:486-490`; `ReleaseArbiter.cs:110-114`; `567-573,716-721` | Ambiguous seal-release failure yields false `NotCommitted`, no stop request, and a wedged journal: later stop cannot persist and recovery is refused while the live lease holds the fence; owner death recovers to Intent | Source-derived; experiment required | Evidential/contractual; release stays blocked; classification conservative | Fault wrapper producing exact ambiguity shape; probe reproducing wedge + recovery-to-Intent |
| CE-03 | `ReleaseArbiter.cs:270-289` (durable-stop write may fail), `238-243,254-261` (clock-unavailable/overflow stop records deadline 0), `226-298` (no consume) | A runtime-accepted stop with failed/incomplete durable evidence does not bind a second arbiter instance sharing journal+capability; intent, gate and FD4 write remain reachable | Source-derived; experiment required | Bounded: unreachable in the accepted single-arbiter model; the contract breach is the missing capability invalidation | Decision (C2) + test asserting `IsLive == false` after stop-first; multi-instance window declared out-of-model |
| CE-04 | `ReleaseArbiter.cs:45-62,81-115,117-183`; `OwnershipTransitions.cs:21-25`; `HeldExecution.cs:45-50` | Concurrent duplicate loser returns `Refused`("intent already exists") while winner returns `Released`; a third later caller returns `Duplicate`; one logical release, three decision kinds | Source-derived; experiment required | Decision/API consistency only; exactly one intent, one winner, one FD4 write | Deterministic concurrent interleaving test (absent from every test file at R12) |

No entry may be upgraded to an executed counterexample, and no unexecuted release path may be labeled independently kernel-qualified.

## E. Required changes (C1–C9) — exact cross-references from the required-changes document

Unresolved-until-resolved rule preserved prominently: **no release qualification gate may be marked accepted while C1–C4 are unresolved.** Plan-analysis acceptance does not complete any decision and does not automatically authorize any production remediation; each of C1–C4 must be individually resolved as: no production change required (evidence-supported), counterexample-first experiment required, source-confirmed production correction (separately authorized), or explicitly deferred with accepted boundary and rationale.

- **C1** — R09 ambiguous-intent contract incl. stop mechanics under a wedged journal (binds Q06; blocks G3-A11, G3-B08). Owner: next engineering stage. Action: recorded decision, then ambiguity-vs-definite wrappers and a CE-02-shaped probe. Production decision: UNRESOLVED. Test-only authorized in principle: yes.
- **C2** — R08 stop-first capability invalidation and the multi-instance window (binds Q05; blocks G3-A02, G3-A07). Owner: next engineering stage. Action: recorded decision, `IsLive == false` assertion, out-of-model declaration. Production decision: UNRESOLVED. Test-only authorized in principle: yes.
- **C3** — R27 persisted stop-deadline consumption (binds Q04; blocks G3-A10 and any R27 pass). Owner: next engineering stage. Action: recorded decision (remaining-allowance derivation from the record or a provably record-bound caller path) plus within-window/expired/no-reset tests. Production decision: UNRESOLVED. Test-only authorized in principle: yes (diagnostic probe only).
- **C4** — R02/R06 duplicate semantics and the two idempotency contracts (binds Q07; blocks G3-A05). Owner: next engineering stage. Action: policy decision (in-progress retained decision or narrowed documented concurrency semantics), CE-01 documented as deliberate contract, deterministic concurrent test. Production decision: UNRESOLVED. Test-only authorized in principle: yes.
- **C5** — G3-A matrix extension: stop-evidence edge cases (clock-unavailable stop, deadline-overflow stop; deadline-0 record implications). Fixture/clarification; pending; test-only.
- **C6** — Fault wrappers must produce the exact ambiguity shape (post-content seal-unlink injection distinct from definite storage failure). Fixture/clarification; pending; test-only.
- **C7** — G3-C orchestration wording: no cross-process lease inheritance; replacement must `TryAcquireWriter` afresh with baseline from `Resolve`, and receive no capability/writer. Fixture/clarification; pending; test-only.
- **C8** — Review dependency linearization in plan §2/§14: fixture authorization proceeds only with this review's disposition recorded; G3-A..G3-E cannot be tagged accepted while C1–C4 are unresolved (explicit in §14). Process clarification; pending.
- **C9** — Status-bridge spec bound: exact frame identity/order preserved, no synthesized observations from stdout/stderr/wrapper artifacts, persistence gated on `MarkDurableCommitted`, G3-D negative cases against the qualification coordinator only (the production coordinator emits no post-release status). Fixture/clarification; pending; test-only.

Fixture-scope caution (review wording preserved): a pre-write attempt witness is not proof of delivered FD4 bytes; a test coordinator is not proof of WP10 target execution. Only elements actually required by C5–C9 are authorized; originally proposed fixtures were not approved unchanged (e.g., C7 wording correction, C6 exact injection shape).

## F. Accepted boundaries

- Frozen and unchanged: `WP05 HARNESS QUALIFIED (bounded)` (§1.A binding); the Q0–Q3 harness review records, corrected seccomp, RELEASE frame golden vector, G2 manifest and OwnershipSnapshot v3 remain frozen.
- NOT qualified / NOT accepted: `WP05 RELEASE ACCEPTED` (does not exist and is not claimed by the review, this checkpoint, or the work-queue/handoff updates); no conceivable WP05 release acceptance is established by plan-analysis acceptance.
- Not done: no WP10 execution; no production or test modifications anywhere in this preservation; no release/crash/trusted-status qualification run by the review or this preservation.
- Not authorized: any production remediation (R27/R08/R09/R02) — authority rests with separately reviewed remediation slices after C1–C4 decisions.

## G. Next authorized workflow

1. Decision reconciliation: resolve C1–C4 (each per §12's four allowed outcomes), as the next engineering stage's own responsibility — not pre-filled by this preservation.
2. Bounded test-only implementation: C5–C9 fixture/test clarifications and, when decisions permit, diagnostic probes (G3-A/G3-B/G3-D as diagnostics, never tagged accepted).
3. Independent native release qualification: G3-C fresh-process crash/no-resend slice strictly after the C1–C4 decisions and fixture set exist.
4. Counterexample-driven production remediation, only if separately approved by an independent review of its own.
5. Full WP05 release acceptance, WP08 owner orchestration and WP10 real target execution remain later work under the authoritative plan.

Smallest next DeepSeek task boundary (from the review, not pre-approved): record the C1–C4 decision outcomes (supported by counterexample-first probes where chosen) and implement the C5–C9 test-only fixture clarifications — no production change, no gate accepted, no remediation in the same slice.

## H. Preservation verification

- Review ref `review/wp05-release-plan-adversarial-review` = exact V3 `e4b0a798e944ddfb144901c63b0c444e7980533b` (verified before and after this preservation commit).
- V1–V3 chain complete and reachable; A3 ancestor of V3; R12 ancestor of A3 (merge-base checks); connectivity via fsck clean for the review ancestry.
- V1–V3 diff is documentation-only (212 insertions across exactly the three review files); A1–A3 documents unchanged; no review document modified by this preservation.
- C1–C9 headings and CE-01–CE-04 headings verified present in the committed V3 blobs (marker check).
- This checkpoint, work-queue and handoff updates form one documentation-only commit on `preservation/wp05-release-plan-adversarial-review`; no unrelated file staged.
- Untracked working-tree experiments (spikes/, TestResults/, stray TRX query file) existed before and after preservation and were not modified, staged or deleted.
- No test run is attributed to the review or to this preservation; neither executed release qualification.
- Remote backup: none performed; the canonical local repository holds all referenced refs. No remote publication was executed, so no remote-durability claim is made.
