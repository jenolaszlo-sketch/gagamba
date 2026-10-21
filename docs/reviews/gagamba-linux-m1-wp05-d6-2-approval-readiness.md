# WP05 D6.2 Independent Human Approval Readiness (GLM)

Status: **PROPOSED - NOT APPROVED**
Disposition: **WP05 D6.2 FOCUSED INDEPENDENT REVIEW COMPLETE - OPTION B: DESIGN SUPPORTED WITH ONE REQUIRED DOCUMENTATION-ONLY CORRECTION (D6.2-R1); C2/C4 SUPPORT UNCHANGED; C3 POLICY AND C1-C4 APPROVAL PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

Decision owner: human WP05 release/security decision holder. GLM review is advisory; it cannot approve C1-C4, select the C3 expired-deadline policy, authorize implementation or qualify release. This document records, from the completed focused review, exactly what is ready for the human decisions and what still must change first.

## 1. Review identity and what this closes

| Item | Value |
|---|---|
| Producer (under review) | `design/wp05-d6-2-contract-precision-amendments` @ `ae09afedddeb703e6bde226bd793b375896c3064` (chain `d39dab6...` -> `d089f938...` -> `ab911d1...` -> `ae09afed...`, docs-only) |
| R12 production (binding) | `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040` |
| D6.1 review (requirements source) | tip `77f559a7dfc1702b33812d5c3d5c42ac925f0343` (not merged into D6.2 - verified) |
| Fixture binding (verified) | `implementation/wp05-release-qualification-fixtures`, test commit `dc4c372b180abef1b27038d94f49af90226fa548`, evidence tip `338a98af269e6b5839f3ccc2e0881f016153b01e`, blob `ccde61763664a4ed56e6d1941c20d61cc0cf2b5f`, SHA-256 `cd5cd00c4c4ef213239047a85ed8f26129e45bb5cad0ddd2c3fe646a407da2b4`, `src` tree `406f52da43b62920128151b2f5706c32d45b6db7` == R12 `src`, `native` no diff, 13/13 method names |
| Review Q1 | `01f4d8c61de635ffa947502b7a54513fc1c5e6f4` - "record independent D6.2 focused contract design review"; document blob `414a3390e4165862de1c8e37822a2b9f7e336f80`; parent = `ae09afed...` |
| Review Q2 | `6c02fe0d130342944f5d0371948606df3b1296ec` - "record independent D6.2 amendment verification matrix"; document blob `adf79f7ff8eae65286e610472784c2f6e3c83573` |
| Review Q3 | this document |

The focused review answers the four primary questions (GLM-R1..R4 in the D6.2 handoff) with these verdicts: R1 `PARTIALLY ADDRESSED` (one required documentation-only correction, D6.2-R1); R2 `VERIFIED ADDRESSED`; R3 `VERIFIED ADDRESSED`; R4 `VERIFIED ADDRESSED`. All six cross-contract pairs re-verify CONSISTENT with no new contradiction. Nothing was approved, no policy selected, no test run, no gate accepted, release not qualified.

## 2. Per-contract readiness for the human decisions

| Contract | Review verdict chain | Ready for human decision? | Blocking item (exactly one or none) |
|---|---|---|---|
| C1 ambiguous ReleaseIntent retention collision | D6.1 `DESIGN SUPPORTED WITH REQUIRED AMENDMENTS` -> D6.2 R3 `VERIFIED ADDRESSED`, R4 `VERIFIED ADDRESSED` | **READY (wording-level)** once the D6.2-R1 correction lands; the non-blocking R3 wording clause (Q1 §7.3-1) may be folded into the same pass | None of substance (the Q1 §7.3-1 wording clause is optional, recommended) |
| C2 stop capability consumption | D6.1 `DESIGN SUPPORTED FOR HUMAN DECISION`, unchanged by D6.2 (cross-reference verified consistent) | **READY** | None |
| C3 deadline contract | D6.1 `CONTRACT CHOICE REQUIRED` -> D6.2 R1 `PARTIALLY ADDRESSED`, R2 `VERIFIED ADDRESSED` | **NOT READY** | **D6.2-R1 correction required first** (mapping row 1 vs retained fallback rule); then the separate policy decision |
| C4 duplicate/concurrent semantics | D6.1 `DESIGN SUPPORTED FOR HUMAN DECISION`, unchanged by D6.2 (precision note verified consistent, exact strings byte-match R12) | **READY** | None |

### 2.1 C3 expired-deadline policy (still a separate human decision)

The D6.2 addendum's Policy A / Policy B question is untouched by the D6.2 amendments: neither D6.2 nor this review selects a policy. Both remain technically coherent against the verified R12 mechanism (`kill + one immediate population observation` is exactly what `StopAndAwaitEmpty` with a non-positive wait executes, and the D6.2 typed mapping supports either). The decision order stays: **D6.2-R1 correction (documentation-only), then the policy answer, then the C3 contract approval.** `C3 ZERO-WAIT POLICY - PENDING HUMAN` remains the standing status string.

### 2.2 What the required D6.2-R1 correction is

One documentation-only amendment in C3 "R1 component outcome mapping": the row `Kill=NotAttempted -> DomainEmpty Unknown` contradicts the retained diagnostic-fallback rule in the same section (the fallback can observe empty, and the `Kill=Failed` row already resolves `DomainEmpty` conditionally). Preferred fix: for `Kill=NotAttempted`, resolve `DomainEmpty` identically to the `Kill=Failed` row (`Confirmed` if the diagnostic observation is empty, otherwise `Unknown`). One sentence; the aggregate truth table and tests are unchanged for all other rows. It requires no production or test change and no re-review of the other three amendments.

## 3. Non-blocking recommendations carried to future slices (do not gate the decisions)

1. C3 implementation slice: disclose the internals-visible assembly set (`Execution/TransactionStateModel.cs:11-12` grants internals to `"Gagamba.Execution.Linux.M1.Tests"` and `"Gagamba.Execution.Linux.M1.Owner"`) when restructuring the Reconciler clock constructor; define the test-only seam as a reviewer-visible invariant.
2. C1: mark in R3-B case 4 that "consumed at first stop acceptance" is C2-contract (composed) semantics; at R12 the stop consumes nothing and the release caller's `:127` consume is the first consume (documented by the fixture probe `StopFirstLeavesCapabilityLiveOnR12`).
3. C3 implementation slice: state whether the `DomainEmpty` durable bit persists when `DomainStop` is `Failed` and only the diagnostic fallback proves emptiness; note the `DomainLease.cs:122` floating-point wait conversion and the `TimeSpan` 100 ns resolution when deriving the wait from `remaining` (urgency-neutral, but deserves one line in the implementation review).

## 4. Approvals, authorizations and gates that remain where they were

- C1-C4 contracts: **PROPOSED - NOT APPROVED**.
- C3 expired-deadline policy: **C3 ZERO-WAIT POLICY - PENDING HUMAN**.
- Implementation authorization: **PENDING HUMAN**, per tranche, proposal order C2 -> C1 -> C4 -> C3, C3 movable only by separate authorization; no blanket authorization.
- Production remediation: **NOT AUTHORIZED**. No production, test, native, engineering or fixture change was made by D6.2 or by this review.
- Release qualification: **NOT ESTABLISHED**. No new test ran, no fixture rerun occurred, no gate was accepted, R12 evidence remains valid for R12 only, and any future production change requires new source binding and reruns.
- H1-H5 dispositions, WP08/WP10 and full G3: deferred, unchanged (carried from the verified D6.0 matrix state; not re-verified by the focused review).
- Component-only boundary unchanged: `ReleaseArbiter`/`Reconciler` have no production call sites (D6.1-verified); this review did not re-verify the absent-call-site census beyond the verified structures it inspected.

## 5. Next bounded actions

1. D6.2 producer: land the D6.2-R1 documentation-only correction (section 2.2) as a small docs commit on the D6.2 branch, additionally folding in (optionally) the two wording recommendations. Record exact SHAs and blob IDs for the corrected C3 section.
2. Human decision holder: answer the four contract questions in `docs/decisions/gagamba-linux-m1-wp05-d6-2-human-approval-addendum.md` and the C3 policy question; the GLM verdicts above are the current best advisory input, not answers.
3. If any contract is approved and a tranche authorized later: tests-first implementation, independent review, new source/evidence binding, per-tranche rollback criteria; no R12 qualification transfer; preserved evidence stays intact.

Nothing in this document changes approval status, authorization status, gate state or release qualification. It establishes that, after one documentation-only correction, C1, C2 and C4 are submittable for the standing human decisions, and C3 requires the correction plus its policy answer first.
