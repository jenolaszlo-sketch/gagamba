# WP05 D6.4 GLM Independent Review Preservation Checkpoint

Date: 2026-10-21 (Asia/Manila)
Status: **PRESERVED FOR DOCUMENTATION CORRECTION — OPTION B; R1/R2 OPEN; NO CONTRACT APPROVED**

## 1. Repository and source identity

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Remote: `https://github.com/jenolaszlo-sketch/gagamba.git`
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`; source tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen WP05 harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D6.3 preservation base: `preservation/wp05-d6-3-terra-independent-c3-review` at `004a12d62bd90bb11217e4e57fa8326f7afafecf` — the direct parent of the D6.4 producer commit.
- Terra D6.3 review: `cda2f22221c4db12ced3a4317362e80167270a63`; document `docs/reviews/gagamba-linux-m1-wp05-d6-3-terra-independent-c3-review.md`, blob `3bb7fd2322670d9f9977004661ab2a72760798d4`; section 6 is the testability finding that motivated D6.4.

## 2. Producer, review, and preservation bindings

| Role | Branch | Commit | Parent |
|---|---|---|---|
| Luna D6.4 producer | `design/wp05-d6-4-r1-fixture-clarification` | `08938e96c69cfd573a284589c986f53f041c5803` | `004a12d62bd90bb11217e4e57fa8326f7afafecf` |
| GLM independent review | `review/wp05-d6-4-glm-independent-fixture` | `8b95c52abdebf8fd9a0235dd5a69082456e1984f` | `08938e96c69cfd573a284589c986f53f041c5803` |
| Preservation | `preservation/wp05-d6-4-glm-independent-fixture-review` | This checkpoint commit (`git rev-parse HEAD` on this branch) | `08938e96c69cfd573a284589c986f53f041c5803` |

GLM's exact review file is `docs/reviews/gagamba-linux-m1-wp05-d6-4-glm-independent-fixture-review.md`; blob `632c311da26e511519c5fbb44cb86efe9b0bb6e1`. The review commit adds only that file. The preservation commit is a direct child of Luna's D6.4 producer commit and is **not** based on GLM's review.

Required graph, verified by commit parents and ancestry checks:

```text
004a12d6 D6.3 preservation
  └─ 08938e96 Luna D6.4 producer
       ├─ 8b95c52a GLM review
       └─ [this preservation commit]
```

`004a12d6` is an ancestor of Luna's commit; Luna's commit is the immediate parent of both GLM's review and this preservation checkpoint. The review and preservation are separate sibling histories; neither sibling tip is an ancestor of the other. GLM's review is referenced only through the exact commit and blob identities above; its content is not copied into this history beyond the faithful preservation summary required by the checkpoint format.

## 3. Scope, diff, and worktree safety

- Luna's diff from the D6.3 preservation contains exactly `docs/decisions/wp05-d6-0-c3-stop-deadline.md` (modified) and `docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md` (added). It is documentation-only: no source, test, native, CI, package, or durable-schema change.
- GLM's diff from Luna contains exactly the independent review document named above. Its worktree was clean before preservation; both the producer and review branches were absent from the remote at pre-publication inspection (`git ls-remote` returned no matching refs).
- The preservation diff is restricted to this checkpoint, `docs/handoff.md`, and `docs/work-queue.md`. No source, test, native, package, schema, CI, harness, or evidence artifact is changed. Luna's two D6.4 design documents and GLM's review document are not modified.
- Luna's producer worktree `C:\Users\Laszlos\source\repos\Gagamba-d6-4` was clean at `08938e96`. GLM's reviewer worktree `C:\Users\Laszlos\source\repos\Gagamba-review-d64-glm` was clean at `8b95c52a`.
- The canonical checkout remained on `implementation/wp05-release-qualification-fixtures` at `338a98a` with its pre-existing unrelated untracked files (trailing `-` and redirection artifacts, `NEM-INDEPENDENT-REVIEW.md`, `spikes/*` scripts and test-output directories, and `tests/*/TestResults/` directories). They were preserved untouched. No worktree was reset, rebased, amended, pruned, or deleted; the three pre-existing prunable `C:\Windows\Temp\opencode\gagamba-conformance*` registrations were left alone.
- No conflicting `preservation/wp05-d6-4*` branch or `Gagamba-preserve-d64` worktree existed before creation.

## 4. GLM's exact independent disposition

GLM's committed review reports:

**WP05 D6.4 GLM INDEPENDENT REVIEW COMPLETE - OPTION B: FIXTURE TESTABILITY PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

The committed disposition matches the reported disposition verbatim. D6.4 is not independently accepted in full: the proposed test strategy is largely sound, and two required documentation corrections (R1 and R2, sections 8-9 below) remain open. GLM assigned no separate finding identifiers beyond R1/R2.

## 5. Verified injection-boundary findings

GLM verified, against frozen R12 source (`Recovery/Reconciler.cs`, `Lifecycle/DomainLease.cs`):

- The synthetic result enters the Reconciler at the single stop-result boundary (`Reconciler.cs:224-231`) only after `RecoveryPlanner.Plan()` (`:75-97`: matching boot, live `AuthorityAlive` lease, identity corroboration) and `liveLease.VerifyIdentity()` (`:224-228`) succeed, and only while the `DomainStop` progress bit is unconfirmed.
- The fixture supplies exactly `Kill=NotAttempted`, `Population=NotObserved` (plus `TimedOut=false` and a diagnostic error string); it is a fixed marker accepting no delegate, payload, or arbitrary result.
- The real bounded stop (`StopAndAwaitEmpty`, `DomainLease.cs:108-143`) is bypassed only in the synthetic test path; therefore `WriteKill` (`DomainLease.cs:116`) is not invoked (`WriteKillCalls=0`; the only other dispatch path, `RequestStop`, is never called by `Reconcile`).
- The genuine population-observation fallback remains exercised: with `DomainStop=Unknown`, the `DomainEmpty` block reaches the real `liveLease.ObservePopulation()` (`Reconciler.cs:265`), a single non-looping call against `FakeDomainBackend.ReadPopulation()`; `ReadPopulationCalls=1` is measurable.
- The design does not replace Reconciler classification with a precomputed result: typed-outcome classification, the `DomainEmpty` fallback classification, aggregate precedence, journal finalization, and durable-progress persistence all run in shared Reconciler code.
- No production caller can select the synthetic path: the fixture type, field, overload, and branch exist only under the test-build symbol in the variant assembly (subject to open correction R1 on friend metadata).

## 6. Verified R1-T13–T15 outcomes

Deterministic and reproducible under the proposed fixture, per GLM's committed review (still proposed acceptance tests — not implemented, not executed):

| Test | Fallback observation | DomainStop | DomainEmpty | Aggregate |
|---|---|---|---|---|
| R1-T13 | Trusted Empty (`Populated=false, ReadFault=false`) | Unknown | Confirmed | QuarantinedUnknown |
| R1-T14 | Populated (`Populated=true, ReadFault=false`) | Unknown | Unknown | QuarantinedUnknown |
| R1-T15 | Unknown/inconclusive (`ReadFault=true`; lease stays live and matching) | Unknown | Unknown | QuarantinedUnknown |

JournalFinalization is `Confirmed` in all three cases via the healthy seeded writer; the R12-vs-proposed divergence (R12 classifies a known-populated fallback as `Failed`, `Reconciler.cs:277-280`; the accepted D6.3 mapping classifies it `Unknown` under `NotAttempted`) is explicitly labeled future typed-classifier behavior, not R12 behavior.

## 7. Durable-progress, zero-kill, and zero-removal evidence

- Durable progress: R1-T13 adds only the `DomainEmpty` bit (`CompletedCleanup` `0x08` → `0x0A`), with `DomainStop` clear; R1-T14/T15 add no new domain progress bit (`0x08` preserved). No successful kill is inferred; `DomainStop` remains Unknown; aggregate cleanup is not confirmed (`QuarantinedUnknown`). Persistence uses the existing v3 `WriteMutation` path; no new journal field or record; no deadline reset (the fixture path never touches the clock; `FakeDomainClock.Delays=0`); no error-string classification.
- Zero-kill and negative assertions independently assessed: zero `WriteKill` calls (retaining existing `KillWrites=0`), exactly one fallback population read, no fabricated stop success, no fabricated durable `DomainStop` confirmation, no false `CleanupConfirmed`, no deadline reset, no new journal field or record, no unauthorized cleanup.
- Zero-removal assertion — basis preserved exactly as GLM established it: the expectation is supported by the fixture's **seeded `DomainRemoval` progress bit**, which routes the `DomainRemoval` block to the existing skip path ("previously verified", `Reconciler.cs:391-395`). It is **not** supported by any assumption that `DomainStop=Unknown` automatically suppresses domain removal. Without that seeded bit, the current reconciliation flow would set `destructive = true` and reach `liveLease.RemoveIfIdentityMatches()` (`Reconciler.cs:398-399`), which dispatches the backend `RemoveChild` after an identity re-check (`DomainLease.cs:176-196`). The proposed `RemoveChildCalls` counter makes the claim measurable rather than assumed.

## 8. R1 — test-variant friend assembly declarations (OPEN — REQUIRED DOCUMENTATION CORRECTION)

GLM found that the proposed assembly-isolation invariant is not fully achievable under the documented build configuration. The D6.4 design says the test-variant M1 assembly grants internals only to its dedicated test assembly and excludes Owner, but frozen R12 contains unconditional shared-source friend grants in `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:11-12` (`Gagamba.Execution.Linux.M1.Tests` and `Gagamba.Execution.Linux.M1.Owner`). Compiling that source set into a separate variant assembly cannot omit the Owner grant without explicitly conditionalizing the attributes — as written, the stated invariant is unreachable by the documented mechanism. GLM also found that the proposed `D6_4.Tests` project compiles `RecoveryTestSupport.cs`, which consumes internals from the separate `Gagamba.Execution` assembly, so the required friend grant from `Gagamba.Execution` to the D6.4 test assembly was not specified.

**Required D6.5 documentation correction (Luna):** document (1) the exact conditional compilation of the M1 friend attributes; (2) production-build friend grants; (3) test-variant friend grants; (4) explicit exclusion of Owner from the test variant; (5) the additional `Gagamba.Execution` friend requirement for the dedicated test assembly; (6) exact assembly names and compilation-symbol boundaries; (7) the practical limits of the phrase "compile-time-only isolation" (the friend metadata persists in the production `Gagamba.Execution` assembly; residual friend-name exposure is consistent with the existing `M1.Tests` precedent). Distinguish legitimate test access from production authority; the solution must not add a general production-accessible synthetic stop-outcome mechanism.

## 9. R2 — production/test source equivalence (OPEN — REQUIRED DOCUMENTATION CORRECTION)

GLM found that the D6.4 design implies, but does not require, that the test variant compiles the same production M1 source set. The proposed production-purity check is useful but incomplete: it verifies exclusion of fixture code from the normal production graph without establishing source-graph equivalence between the production and test builds. A test variant using different substantive reconciliation source could pass tests without supporting the intended production-contract claim.

**Required D6.5 documentation correction (Luna):** document (1) how the production and test variant compile the identical production M1 source inventory by construction; (2) which fixture-specific files, symbols, and assembly metadata are additional; (3) how the build check verifies source-graph identity (both directions: production purity and variant source-graph identity); (4) how conditional-compilation differences are enumerated; (5) how unintended substitution or omission of production Reconciler source is detected; (6) how the normal production build is proven free of fixture code; (7) the exact limitations of testing a separate assembly variant. The design must explicitly state: **a test-built variant is not binary-identical production evidence.** Behavioral conclusions must be bounded accordingly; no byte identity or released-binary qualification may be claimed from these tests.

## 10. Contract readiness

| Contract | Status |
|---|---|
| C1 | Previously supported for individual human decision; approval pending |
| C2 | Previously supported for individual human decision; approval pending |
| C3 | Fixture testability partially addressed; R1/R2 documentation corrections required; policy pending |
| C4 | Previously supported for individual human decision; approval pending |

All remain `PROPOSED - NOT APPROVED`. C3 remains `C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor Policy B is selected. The human C3 contract decision remains held pending resolution of R1 and R2 and the separate policy choice. Unchanged and not reopened by D6.4 or this preservation: `Kill=NotAttempted` mapping; `Kill=Failed` classification; R1-T1–T12 expectations; aggregate Failed/Unknown/Confirmed precedence; JournalFinalization uncertainty; trusted-clock design; expired-deadline contract; C1 ambiguous-release design; C2 stop-capability design; C4 duplicate/concurrent semantics. GLM reported no new contradiction in these areas.

## 11. Evidence and qualification boundaries

No D6.4 production implementation occurred; no tests were executed; no native code was changed; no new source-bound runtime qualification exists. Frozen R12 remains `38f6981b2a60daab2df37c92913a6760c1bef131` / tree `6854675d6b37a49d5302d6104103bdbfbc268040`; frozen harness Q3 remains `36a124d7dff98ecfaf956340767fe1768ad5b17f`. No G3 gate is accepted; no release is qualified; no tags, packages, or default-branch changes are created. Unchanged deferrals: H1 production status integration; H2 destructive FD5 negative cases; H3 optional raw SIGKILL/wait4/pidfd witness; H4 C1–C4 human approvals; H5 historical ordering note; WP08 owner orchestration; WP10 status/target integration; full G3 acceptance; remaining portability qualification. None of these is closed by documentation preservation.

## 12. Next task — D6.5

**WP05 D6.5 — Test-Variant Isolation and Source-Equivalence Precision** (documentation-only, Luna). Correct GLM's R1 and R2 exactly as specified in sections 8-9: conditional friend attributes, the missing `Gagamba.Execution` test friend, production/test assembly access boundaries, shared production-source inventory, variant source-graph verification, production-purity checks, and the binary-identity limitation — followed by a focused independent follow-up review. Do not implement the fixture, change production code, or select C3 policy during D6.5.

## 13. Publication and fresh-clone verification

Before publication, `git ls-remote` found no D6.4 producer, GLM review, or preservation ref on origin; the prior published refs were left untouched. No remote publication is claimed by this checkpoint at creation time.

| Ref to publish by ordinary non-force push | Expected commit |
|---|---|
| `design/wp05-d6-4-r1-fixture-clarification` | `08938e96c69cfd573a284589c986f53f041c5803` |
| `review/wp05-d6-4-glm-independent-fixture` | `8b95c52abdebf8fd9a0235dd5a69082456e1984f` |
| `preservation/wp05-d6-4-glm-independent-fixture-review` | This checkpoint commit, direct child of Luna's D6.4 commit |

After publication, a fresh clone must verify the three exact refs, parent/sibling ancestry, producer/review/preservation file scopes, GLM review blob `632c311da26e511519c5fbb44cb86efe9b0bb6e1`, verbatim Option B text, both R1 and R2 explicitly open, verified injection/acceptance findings distinguishable from open isolation findings, unchanged R1-T13–T15 expectations, no implementation claim, unapproved C1–C4, pending C3 Policy A/B, unchanged R12 tree and harness commit, intact prior D6.3 published refs, clean preservation checkout, and `git fsck --full` exit status/warnings. The final preservation report records the publication result; no force update, tag, package, default-branch change, or unrelated ref is permitted.

**Preservation is evidence custody only. It does not implement R1 or R2, modify the proposed fixture design, approve C1-C4, choose C3 policy, authorize production remediation or test implementation, or qualify a release.**
