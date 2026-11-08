# Slice E-1 F-02 parity-control owner acceptance — verification-path divergence control closure

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-OWNER-ACCEPTANCE-01`

**F-02 CLOSED WITH QUALIFICATIONS**

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This decision formally closes the E-1 F-02 divergence finding **within the accepted Option B bounded parity-control mechanism** of [architecture decision 01](rc2b-06-b6-alt-b-slice-e-01-f02-arch-decision-01.md). It accepts only the implemented test-only parity controls and their independent qualification. It does **not** certify behavioral equivalence, branch dominance, or protected native parity between `VerifyActive()` and `VerifyActiveIntegrity()`; it does not integrate the projection into any production path; and it does not authorize E-2 through E-7, Slice N, or activation.

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `ac764a5d5fcaadbeb6c23e5b4082841d4e01df4b` (matches the commission) |
| Starting tree | `bea88b6451c482c9367e4bc01063254c571a30cf` (matches the commission) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded during Q1; not synchronized; local `main` 13 ahead) |
| Push | none |

Revision chain verified directly: implementation commit `4c5a42f0fecff90dcb30088ca0203956e7a80032` (tree `ba021734bc42c43fd1f0c6cc8a52573707a6a972`); implementation handoff `d221b24be3d19c775c5118e419b2e88769fea2ce` (tree `d58f5f12f159572e41d9e44814fdf9527effc4b0`); Q1 review committed at the current HEAD `ac764a5`. `git log 4c5a42f..HEAD -- src tests eng` and `git diff d221b24 HEAD -- src tests eng` are empty: no production, test, or tooling source change exists after the independently reviewed implementation. No reset, clean, merge, rebase, prune, extra worktree, or remote synchronization was used.

## 2. Authoritative evidence inventory

Reconciled against the actual records, not only the producer/reviewer conclusions:

- [F-02 architecture decision](rc2b-06-b6-alt-b-slice-e-01-f02-arch-decision-01.md) §§8–12 — Options A/B/C, Option B selection, bounded `F-02-PARITY-01` authorization, P4-neutral classification, deferred Option A trigger.
- [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) §6 F-02 — "when either verification path changes, a parity analysis is required; before production integration, a fresh consolidation-versus-parity assessment" (the original obligation this closure discharges).
- [F-01 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) and [F-03 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) — preserved qualifications and future-consumer obligations.
- [Original E-1 Q1 finding](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md) F-02 (Observation; LOW; non-blocking) — duplication/divergence risk.
- [F-02-PARITY-01 implementation report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f02-parity-01.md) and its [evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-f02-parity-01-evidence/README.md).
- [F-02-PARITY-Q1 report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1.md) (**SUPPORTED WITH QUALIFICATIONS**) and its [Q1 evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1-evidence/README.md), including `reconstruction-calls.txt`, `mutation-matrix.txt`, `comment-strip-and-bypass.txt`, `reachability-and-identity.txt`, `production-diff-and-identity.txt`, `regressions.txt`, `il-probe.cs`, and `commands.txt`.
- [P4 refusal-only coverage owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) — F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, the 331 unqualified native skips.
- [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) §§3.3/3.5 — prepublication validation context, original installed-integrity selection, phase-change refusal, stopping/reconciliation independence.
- Implemented source: `tests/Gagamba.Execution.Linux.M1.Tests/Installed/InstalledVerificationParityTests.cs`; `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.cs`; `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.Prepublication.cs`; `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs`.

Independent checks performed directly for this record:

- Exact baseline commit/tree/branch, single worktree, clean tracked/index state, and 44 preserved untracked paths (expanded enumeration).
- The parity test file is byte-identical at `4c5a42f`, `d221b24`, and `ac764a5` (blob `1b782ae115f4d81a0dcc6a22cb50857cf6670252`); the two production files match the reviewed blobs (`7f1eed85…`, `ec30fccc…`).
- `git show 4c5a42f` is a pure addition: two comment-only source hunks (5 lines each, no code tokens) plus the 227-line test file; zero deletions. The only commit touching `src`/`tests` in `071acbe..d221b24` is `4c5a42f`.
- Producer and Q1 regression evidence files read directly: parity 10/0/0; E-1/F-01 class 23/0/0; focused installed/authority/release 248/0/32; full Windows M1 1,401/0/331; Release verifier PASS / Debug-as-production FAIL.
- Source inspection of both verification methods, the comment-only blocks, and the `PrepublicationIntegrityProjection` construction boundary (`Verify()` → `VerifyActiveIntegrity()`; private constructor; private `FromEvidence`; Debug-only `ForTest`).
- IL reconstruction reconciliation of the shared-prefix and ordering claims (see §5, including the owner observation on the reported prefix range).
- No tests were rerun for this decision: the committed producer and independent Q1 evidence address every acceptance question, and no material contradiction required a new run. No external model, agent, network, or tool installation was used.

## 3. Implemented scope and revision

The authorized package added **ten parity tests** plus comment-only cross-references at `4c5a42f`:

- `InstalledVerificationParityTests.cs` — 10 deterministic `[Fact]`s: unique method resolution; shared-prefix presence (14 labels); shared-prefix first-offset order; qualified-only association absence/presence (`SelectedRecordName`, `AcceptRecord`); mandatory `CheckHost`/`RecheckSelectors` presence in both paths; result-type separation (`InstalledIntegrityEvidence` vs `InstalledResult.Fresh`/`Qualified`); encoded intentional asymmetries; E-1 production-unreachability (caller set of `VerifyActiveIntegrity` == exactly `PrepublicationIntegrityProjection.Verify`; `Verify` has zero M1 callers; `FromEvidence` private); behavioral fail-closed refusal on an unprovisioned host (host-conditional; skips on Linux).
- Comment-only cross-references above both verification methods naming the parity contract and the intentional asymmetry.

Owner confirmation of the boundary conditions required for closure:

- The test implementation is unchanged since its accepted producer revision (blob identity above).
- **No production executable logic was modified** — comment-only diff; Q1 independently proved that stripping the two comment blocks yields byte-identical method bodies (`62145748…`/1030 and `78C97923…`/739).
- No shared verification core, no refactor, no authority factory, no public API, no new `ForTest` seam; the Release assembly exports exactly one pre-existing type.
- No existing security test was weakened (pure addition; no test deletions or edits).
- No positive validation path was enabled; the E-1 chain remains dead code in Release.

The owner decision does **not** reclassify this test-only implementation as production verification refactoring.

## 4. Independent Q1 assessment

The independent Q1 disposition **SUPPORTED WITH QUALIFICATIONS** is accepted as the qualification basis for this closure. Q1 was performed by a separate reviewer with a reviewer-owned IL probe and reviewer-authored mutations in external scratch; the canonical checkout was never mutated. Q1's material claims were re-read against its committed evidence for this record:

- Structural reconstruction: identical resolved call sequence in the shared prefix with correct mandated targets, correct qualified/prepublication tail differences, and correct straight-line ordering (reconciled in §5).
- Intentional differences: independently verified as encoded, not flattened; no false positives on the accepted implementation (Q0: 33/33).
- Mutations: 17 reviewer runs; categories preserved distinctly in §6.
- Reachability: caller scans on Debug **and Release**; `VerifyActiveIntegrity` has exactly one caller (`PrepublicationIntegrityProjection.Verify`, itself uncalled); RELEASE verifier PASS exit 0; Debug-as-production FAIL exit 1 naming `PrepublicationIntegrityProjection::ForTest`.
- Regressions reproduced in kind: parity 10/0/0; E-1/F-01 class 23/0/0; focused installed/authority/release 248/0/32; full Windows M1 1,401/0/331, 0 build warnings/errors.
- P4: **P4-NEUTRAL** (test-only executable additions; comment-only production changes).
- Reviewer explicitly did **not** establish branch dominance; no dominance or equivalence claim is accepted here.

Binary identities are commit-bound (SourceLink embeds the revision): the reviewed build identities at `d221b24` (Debug `ac25e3b5…`/`311222F5…`, Release `2167863b…`/`4A2F83C9…`) differ from the producer's pre-commit captures while method IL is identical. Source blob hashes are the anchors (Q1-F05, §8).

## 5. Structural and ordering guarantees

What this closure accepts as established, at the accepted static level:

1. **Mandated shared-prefix calls are present in both methods** and resolve to the intended targets: `EnsureLinuxX64`, `OpenAbsoluteDirectory`, `RequireDirectory`, `OpenDirectory`, `ReadTrustedFile`, `InstalledMetadata.Active`, `InstalledManifest.Parse`, `SameDigest`, `InstalledMetadata.Receipt`, `CreateScan`, `MetadataArtifact`, `Walk`, `EnsureExact`, `InstalledPayload.CheckDependencies`; plus presence-only `InstalledPayload.CheckHost` and `RecheckSelectors`.
2. **First-offset order of the shared prefix is identical in both methods** in the accepted sequence ending `Walk` → `EnsureExact` → `CheckDependencies`. The order control was independently proven load-bearing (R-10b: pure reorder with all calls present fails).
3. **Qualified-only and prepublication-only behaviors are distinct and encoded**: association tokens absent from the prepublication path and required in the qualified path; result-type cross-absence; the intentional asymmetries asserted rather than flattened.
4. **E-1 production-unreachability** is asserted in-assembly and independently confirmed on Debug and Release.

Owner observation on the reported prefix range: the committed reconstruction lists decimal first offsets; the resolved call sequence and first offsets are identical in both methods from `0x002` (`EnsureLinuxX64`) through `0x151` (`InstalledMetadata.Receipt`). From the first `CreateScan` reference onward the offsets differ by exactly one byte (`0x15B` in `VerifyActive` vs `0x15A` in `VerifyActiveIntegrity`), consistent with the intentional retained-versus-discarded receipt local; the fourteen shared calls remain present and ordered through `CheckDependencies` (`0x1D0` / `0x1CF`). The Q1 report's shorthand "`0x002–0x15B`" bounds the byte-identical/shared region ending at that first `CreateScan` reference; the exact raw-byte boundary is not independently reproducible from the committed evidence, and this owner decision binds to the call-level reconstruction facts above — which are the facts the parity controls assert. No shared-prefix check is absent, skipped, or replaced in either method at this revision.

What is explicitly **not** established (and not required by the selected Option B contract): branch dominance over every path, argument correctness, helper-internal semantics, and provisioned-store behavioral parity (Slice N, `NATIVE-PENDING`).

## 6. Mutation detection and non-detection

Categories are kept separate; detection scores are not combined across them.

**Independent Q1 matrix (17 runs plus the Q0 positive control; external scratch only):**

| Category | Runs | Detail |
|---|---|---|
| Detected by the intended parity assertion | R-01, R-02, R-03 (prefix presence + order), R-04, R-05, R-06 (host/recheck), R-07 (qualified acceptance + asymmetry), R-08 (association + asymmetry), R-09 (record selection), R-11 (`VerifyActive` Walk), and R-10b (order-only, all calls present) | 11 runs; each failed the exact expected assertion message |
| Detected by a different existing test | CP-04 (guarded F-01 semantic `Require` block) — 5 failures in the existing E-1/F-01 projection class | 1 run; **not** parity-suite detection |
| Compiler rejection | none among the final runs | — |
| Invalid / inconclusive | R-10 (swap `SameDigest`/`Receipt`) — did not compile (`CS0841`), superseded by R-10b | 1 run |
| Not detected | CP-01, CP-02, CP-03 (call-preserving conditional bypasses) | 3 runs; §9 |
| False positives | none (Q0: 33/33 on the accepted implementation; scratch method IL hashes equal to canonical) | — |

**Producer matrix (12 runs, external scratch only):** M-01–M-07, M-10, M-11 detected by the intended assertions; M-08 (check moved after `return`) build-rejected by repository policy (`CS0162` under `TreatWarningsAsErrors`) — compiler rejection, not detection; M-09 (call-preserving conditional bypass) not detected and recorded as an inherent limitation. The producer matrix is corroborating producer evidence; the independent matrix is the qualification basis.

Required architecture-decision §9.8 negative controls all reproduce: remove `Walk`/`EnsureExact` from the prepublication path → fail (R-01/R-02); inject `AcceptRecord` into the prepublication path → fail (R-08); remove `CheckHost` from the qualified branch → fail (R-05).

## 7. Intentional trust-boundary differences preserved

The closure preserves the accepted distinctions; none was flattened for symmetry:

- Qualification-record association (`SelectedRecordName`, `AcceptRecord`) remains qualified-path-only and absent from prepublication.
- Qualified outcomes remain represented by `InstalledResult` (`Fresh`/`Qualified`); the prepublication path returns `InstalledIntegrityEvidence` and constructs `PrepublicationIntegrityProjection`.
- `CheckHost` remains mandatory on the prepublication path and on the qualified admitted branch; the qualified fresh-branch skip remains intentional.
- Null qualification association (`QualificationId`/`QualificationRecordSha256` == `null`) remains required on every projection path in E-1.
- The projection constructor remains private; `FromEvidence` remains private; `ForTest` remains unavailable in RELEASE.
- No generic verified result can be promoted into validation or qualification authority; the runtime loaded-assembly verification-to-use check remains a separate, unimplemented obligation (`NATIVE-PENDING`, Slice N).
- Failure classification stays as accepted: typed `InstalledResult` states vs throw-based refusal with the established exception translations.

## 8. Retained Q1 qualifications (Q1-F01 through Q1-F06)

All independent Q1 findings are retained as binding qualifications of this closure:

- **Q1-F01 (LOW) — call-preserving bypasses.** The structural parity suite does not detect every mutation that preserves a call token while changing its effective execution condition (CP-01/02/03, M-09 class). This is an accepted limitation of the selected Option B mechanism. The requirement for future behavioral and authority-specific qualification is retained.
- **Q1-F02 (LOW) — arguments and helper internals.** The suite does not comprehensively verify security-relevant call arguments, helper-internal semantics, or every behavioral difference under identical call targets. Future helper changes affecting shared invariants require corresponding regression review.
- **Q1-F03 (LOW) — name-specific negative checks.** Forbidden-call assertions depend on expected member names and targets. If authority operations are renamed, replaced, or redistributed, those checks must be revisited; the suite does not detect every newly invented authority operation.
- **Q1-F04 (INFO) — ordering versus dominance.** First-offset IL ordering does not prove branch dominance. Required call ordering is checked at the accepted static level only; complete control-flow enforcement is not established; future changes introducing conditional execution require additional review or tests.
- **Q1-F05 (INFO) — commit-bound artifact identities.** Production changes were comment-only and method IL is unchanged, but whole-assembly identities change with source/commit metadata. The reviewed build identities are tied to their source revision; prior assembly identity or qualification evidence must not be reused as though it applied to a different committed build. Rebuild/reapproval obligations for any later artifact publication are preserved.
- **Q1-F06 (INFO) — mutation evidence provenance.** The independent mutation matrix includes distinct detection, non-detection, and invalid-mutation categories (§6). The evidence and its interpretation are preserved; mutation coverage is not exhaustive.

## 9. M-09 acceptance boundary

The reviewer's classification of the call-preserving bypass limitation as **ACCEPTABLE WITH QUALIFICATIONS** is reconciled with the binding architecture decision as follows:

- The decision selected deterministic **structural parity controls**, **ordering guards**, **explicit intentional-difference assertions**, and **mutation-sensitive tests**, and it explicitly excluded a general IL control-flow analysis framework; native differential parity was deferred to Slice N. The implemented package fulfils exactly that contract (all §9.2–§9.10 deliverables present; all §9.8 negative controls reproduced).
- The undetected class requires deliberately authoring a guard inside the protected method — an edit that is itself the review subject — and no test-only package can robustly detect it without branch-dominance analysis, which the decision excludes.

**F-02 closure accepts the bounded divergence-control mechanism. It does not certify that all security checks dominate all execution paths or that both verification methods are behaviorally equivalent.** A future reachable production consumer must not treat this owner acceptance as a replacement for its own authority, failure-path, and protected native qualification.

## 10. P4 and RELEASE preservation

Independently supported classification confirmed: **P4-NEUTRAL.**

- Test-only executable additions; comment-only production changes; no protected-sink caller added; no F01/F02 authority-gate change; no S10/S11 domination change; no S28 reachability change; no first-protected-effect change; no qualified-to-legacy downgrade; no RELEASE refusal-policy change.
- E-1 remains production-unreachable: `VerifyActiveIntegrity` is called only by `PrepublicationIntegrityProjection.Verify`, which has zero callers in Debug and Release; `FromEvidence` is private; `ForTest` is excluded from RELEASE; no validation-authority factory or positive validation route is constructible through F-02 acceptance.
- RELEASE verifier: PASS exit 0 (0/4 hook types, 0/3 hook properties, 14/14 core types); Debug-as-production: expected FAIL exit 1 naming `PrepublicationIntegrityProjection::ForTest` among forbidden factories.
- Preserved: F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, and all **331 unqualified native skips — which are not passes and are not protected native qualification**.

## 11. Mandatory future production-integration gate

This is a **binding condition of F-02 closure**. Before introducing the first production consumer of the E-1 prepublication integrity projection, a fresh security review is required covering at least:

1. `VerifyActive()` versus `VerifyActiveIntegrity()` current state.
2. Shared integrity invariants.
3. Conditional bypass and branch-dominance risks (the Q1-F01 class).
4. Call arguments and helper semantics (Q1-F02).
5. F-01 semantic integrity relationships.
6. Fail-closed treatment of every integrity-verification exception and failure mode (the F-01 consumer-integration obligation).
7. Qualified versus prepublication authority separation.
8. Runtime loaded-assembly verification-to-use.
9. Publication and artifact identity (rebuild/reapproval; Q1-F05).
10. P4 effects and applicable protected native qualification.

A targeted parity and authority review is also triggered when E-4 or E-6 changes the relevant verification or authority paths. Option A (shared-core consolidation) must be reconsidered when the first production consumer is designed or when the E-4/E-6 integration requires stronger guarantees — this is a **mandatory reassessment, not automatic authorization to consolidate**. If future evidence demonstrates that structural parity is insufficient for safe production integration, bounded remediation or architectural reconciliation is required before activation. F-02 is **not** reopened merely because its already documented limitations exist.

## 12. F-01 and F-03 preservation; E-1 finding status

- **F-01 — CLOSED WITH QUALIFICATIONS.** Preserved: owner role-hash binding (`OwnerClosureSha256 == RoleHash(manifest,1)`), recovery role-hash binding (`RoleHash(manifest,0)`), runtime-inventory binding (`platform.runtimeInventorySha256`), fail-closed integrity verification, and the required exception handling before the first production consumer. Do not reopen without new evidence.
- **F-03 — CLOSED WITH QUALIFICATIONS.** Preserved: Windows PowerShell 5.1 qualification (51/51 probes, exit 0); PowerShell 7 unexecuted; existing negative-control nonzero-exit semantics; guaranteed restoration of the scoped `$ErrorActionPreference`. Do not modify verification tooling under this decision.
- **F-02 — CLOSED WITH QUALIFICATIONS (this decision).**

All three E-1 findings now have formal owner dispositions: **F-01 closed with qualifications; F-02 closed with qualifications; F-03 closed with qualifications.** This does **not** remove future E-1 integration requirements: the §11 gate, the F-01 future-consumer fail-closed obligation, and protected native qualification all remain binding.

## 13. Slice E and Slice N boundaries

- E-2 through E-7 remain separately unauthorized: frozen-schema provenance reconciliation, the validation-catalog approval-chain owner decision, GAVA1, v3-pre, the root authorizer/launcher and custody requirements, atomic enablement and the R/O/C ordering correction, and the E-7 driver.
- Slice N protected native qualification remains `NATIVE-PENDING`: provisioned-store differential parity for both verification paths, runtime loaded-role verification-to-use, native stop/reap/reconcile, and all 331 skipped native cases. Skipped cases are not passes and no protected native positive is established by F-02 acceptance.
- Positive validation remains non-constructible; no public API, authority factory, or activation is added or implied.

## 14. Formal owner disposition

**F-02 CLOSED WITH QUALIFICATIONS.**

The bounded Option B implementation satisfies its accepted contract — deterministic structural parity controls, ordering guards, explicit intentional-difference assertions, and mutation-sensitive tests within a test-only scope — independent Q1 supports it (**SUPPORTED WITH QUALIFICATIONS** with the M-09 class judged **ACCEPTABLE WITH QUALIFICATIONS**), production and RELEASE behavior are unchanged, P4 is neutral, and the limitations (Q1-F01 through Q1-F06) and the mandatory future production-integration gate (§11) remain explicitly governed. No required F-02 invariant failed; no blocking contradiction exists.

**Exact closure boundary:** closure discharges the E-1 F-02 obligation to select and implement a mechanized divergence-control mechanism before production integration and to record the consolidation-versus-parity decision. It establishes only the §5 static structural/ordering guarantees for the reviewed revision. It does not certify behavioral equivalence, branch dominance, argument correctness, helper internals, or native parity; and it grants no production integration of the projection.

## 15. Recommended next commission

A separate architectural entry commission for **`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-01`** to evaluate the prerequisites, authority boundaries, and authorized work-package sequence for Slice E-2 (frozen-schema provenance; validation-catalog approval; K/M/F/S authority relationships; trust-anchor and approval-chain requirements; interaction with accepted E-1 integrity evidence; fail-closed validation prerequisites; P4 preservation and later re-audit obligations). **This recommendation does not authorize E-2 entry, implementation, or authority construction;** E-2 requires its own separate owner entry decision and must not start automatically. The §11 gate and the Option A reassessment trigger remain binding regardless of sequencing.

## 16. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Production, tests, tooling, historical evidence, and previously accepted owner decisions remain unchanged. No production change, no test change, no parity-control remediation, no shared-core extraction, no verifier or harness modification, no F-01/F-03 reopening, no E-2 through E-7 work, no Slice N work, no activation, no public API or authority expansion, no external model dependency, no network synchronization, no branch or extra worktree, no reset/clean/merge/rebase/prune/history rewrite, and no push. All 44 historical untracked paths preserved. **Stop after committing this owner decision; do not begin remediation or any subsequent milestone automatically.**
