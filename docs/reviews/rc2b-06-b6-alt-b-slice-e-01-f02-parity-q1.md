# Slice E-1 F-02 parity controls — independent qualification (Q1)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-Q1` (independent security and verification-parity qualification of `…-F02-PARITY-01`). Reviewer: DeepSeek. Date: 2026-11-09 (Asia/Manila). Scope: independent qualification and reviewer evidence only — no production changes, no remediation, no owner acceptance, no push.

**Formal Q1 disposition: SUPPORTED WITH QUALIFICATIONS** (see §14). **F-02 remains open**; only a later owner decision may close it.

Evidence: [`rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1-evidence/`](rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1-evidence/README.md). All mutations and disposable assemblies were built in external scratch; the canonical checkout was never mutated.

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Branch / worktrees | `main`; exactly one canonical worktree |
| HEAD | `d221b24be3d19c775c5118e419b2e88769fea2ce` (matched) |
| Tree | `d58f5f12f159572e41d9e44814fdf9527effc4b0` (matched) |
| Implementation commit / tree | `4c5a42f0fecff90dcb30088ca0203956e7a80032` / `ba021734bc42c43fd1f0c6cc8a52573707a6a972` (matched) |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (unchanged; local `main` 12 ahead) |

No reset/clean/merge/rebase/prune/history rewrite, additional branch/worktree or remote synchronization.

## 2. Exact implementation revision reviewed

- Parity controls: `tests/Gagamba.Execution.Linux.M1.Tests/Installed/InstalledVerificationParityTests.cs` (blob `1b782ae115f4d81a0dcc6a22cb50857cf6670252` — identical at the implementation commit and at final HEAD).
- Comment-only cross-references: `InstalledFileSystem.cs` (blob `7f1eed85…`), `InstalledFileSystem.Prepublication.cs` (blob `ec30fccc…`).
- Verification methods reviewed: `VerifyActive` (`InstalledFileSystem.cs:113–178`) and `VerifyActiveIntegrity` (`InstalledFileSystem.Prepublication.cs:21–70` at this revision).
- Binding contract: architecture decision §§8–10 (`docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f02-arch-decision-01.md`), E-1 owner acceptance §6 (F-02), E-1 Q1 F-02 finding, Alt-B decision §§3.3/3.5.
- Only commit touching `src` or `tests` in `071acbe..d221b24` is `4c5a42f` (verified).

## 3. Independent structural parity reconstruction

The reviewer's own IL probe (not the producer's suite) decoded every call/member reference of both methods in IL order (full listings in `reconstruction-calls.txt`).

**Prefix identity verified at the byte level.** Offsets `0x002–0x15B` of both methods contain the identical reference sequence: `EnsureLinuxX64` → `OpenAbsoluteDirectory` → (`Fd.Snapshot`) → `RequireDirectory` → `OpenDirectory` (installations) → `OpenDirectory` (qualifications) → `Fd.Snapshot` ×3 → `ReadTrustedFile` (active) → `InstalledMetadata.Active` → `OpenDirectory` (generation) → `ReadTrustedFile` (manifest.json) → `InstalledManifest.Parse` → `get_Digest` → `SameDigest` → `ReadTrustedFile` (install-receipt.json) → `InstalledMetadata.Receipt` → `CreateScan` → `MetadataArtifact` ×2 → `Walk` → `EnsureExact` → `InstalledPayload.CheckDependencies`.

**Then the paths intentionally diverge:**

| Aspect | `VerifyActive` (offsets) | `VerifyActiveIntegrity` (offsets) |
|---|---|---|
| Qualification branch | `get_QualificationId` `0x1D8`, `get_QualificationRecordSha256` `0x1E1`; fresh branch `RecheckSelectors` `0x227` → `InstalledResult.Fresh` `0x22F`; qualified branch `SelectedRecordName` `0x23D`, `ReadTrustedFile` `0x26A`, `AcceptRecord` `0x277`, `CheckHost` `0x27F`, `RecheckSelectors` `0x2AA` → `InstalledResult.Qualified` `0x2B6` | none; `CheckHost` `0x1D7`, `RecheckSelectors` `0x209`, then `RoleHash` ×2, `InstallJson.Hex`, `InstalledIntegrityEvidence::.ctor` `0x236` |
| Host check | qualified branch only (fresh branch skips by design) | unconditional |
| Result/failure | `InstalledResult` Fresh/Qualified; typed `Unavailable`/`Unqualified` mappings | `InstalledIntegrityEvidence`; four `InvalidDataException` translations |
| Record association | `SelectedRecordName` + `AcceptRecord` | absent |

**Required shared operations are mandatory and in the accepted order**; the reconstruction found no weaker or skipped check in the prefix. The producer's 14-label shared-prefix table matches the actual resolved method targets (the probe resolved tokens, so same-named members on different types would have been visible).

## 4. Test-by-test contract assessment

All ten tests were inspected line-by-line; behavior of `IlCallScanner.CallOffsets` and `Unique()` was reviewed directly.

| Test | Claim enforced | Evidence | Can it pass for an incorrect implementation? |
|---|---|---|---|
| `BothVerificationPathsResolveUniquely` | Exactly one `VerifyActive`/`VerifyActiveIntegrity`, both internal static | Reflection `Single()` | An added overload fails loudly (`Single` throws) — conservative. Cannot detect a same-name method on a different type, but the type is pinned by `typeof(InstalledFileSystem)`. |
| `SharedIntegrityPrefixAppearsInBothPaths` | All 14 prefix calls referenced by both methods | IL token presence | Yes for call-preserving/argument changes (CP-01..03; Q1-F01/F02) — bounded, see §6. Detects removal/replacement. |
| `SharedIntegrityPrefixOrderIsIdenticalInBothPaths` | Accepted first-offset order in each method | IL offsets | Order-only; passes if a call is duplicated earlier (Q1-F04). Independently proven load-bearing by R-10b. |
| `PrepublicationPathNeverEstablishesQualificationAssociation` | No `SelectedRecordName`/`AcceptRecord` tokens in prepublication | IL token absence | Name-specific (Q1-F03); passing is the safe direction. |
| `QualifiedPathRetainsRecordSelectionAndAcceptance` | Both tokens present in `VerifyActive` | IL token presence | Same presence limitation; detects removal (R-07/R-09). |
| `BothPathsRequireHostBaselineAndSelectorRecheck` | `CheckHost` + `RecheckSelectors` tokens in both | IL token presence | Same presence limitation; detects removal (R-04/R-05/R-06). Does not flatten branch-specific fresh/admitted behavior. |
| `ResultTypesRemainDistinct` | Evidence ctor only in prepublication; Fresh/Qualified only in `VerifyActive` | IL token presence/absence | Detects cross-contamination both directions; cannot detect type misuse that keeps references. |
| `IntentionalQualifiedAndPrepublicationDifferencesAreEncoded` | Asymmetries asserted as the contract | IL token presence/absence | Redundant by design; prevents future flattening to "identical". |
| `PrepublicationConstructionRemainsProductionUnreachable` | Caller set of `VerifyActiveIntegrity` == exactly `{PrepublicationIntegrityProjection.Verify}`; `Verify` has zero callers; `FromEvidence` private | Whole-assembly token scan + reflection | Detects any added caller; exempts reflection/delegate dispatch (not present; RELEASE verifier provides the stronger claim). |
| `BothPathsFailClosedOnAnUnprovisionedHost` | Typed `Unavailable` vs `InvalidDataException`; neither trusted | Direct invocation (Windows) | Genuine behavioral check but host-conditional: skips on Linux. Does not test provisioned-host behavior (native-pending). |

**Resolution and determinism.** `Unique()` fails on ambiguity rather than silently selecting; the tests are deterministic reflection/IL reads against the Debug M1 assembly (the test project pins Debug by design). They were reproduced identically in the reviewer's independent scratch build (33/33 pass; scratch method IL hashes equal to canonical). A future compiler that stopped emitting a listed call for equivalent code would fail the control by design (review trigger); no such case exists at this revision. No test in the suite is flaky or timing-dependent.

## 5. Original reviewer mutation matrix

Seventeen reviewer-authored runs (not the producer's script): one positive control, eleven removal/reorder mutations, one superseding reorder, four call-preserving bypasses. Every run also executed the E-1/F-01 projection class (33 tests total per run). Full detail: `mutation-matrix.txt`.

| # | Mutation (scratch only; canonical untouched) | Category | Evidence |
|---|---|---|---|
| Q0 | none (positive control) | pass 33/33 | scratch IL hashes == canonical; no false positives |
| R-01 | remove `Walk` (prepublication) | **detected by intended assertion** | prefix presence + order: "no longer references Walk" |
| R-02 | remove `EnsureExact` (prepublication) | **detected** | "no longer references EnsureExact" |
| R-03 | remove `CheckDependencies` (prepublication) | **detected** | "no longer references InstalledPayload.CheckDependencies" |
| R-04 | remove `CheckHost` (prepublication) | **detected** | "prepublication verification must retain CheckHost" |
| R-05 | remove `CheckHost` (qualified branch) | **detected** | "qualified verification must retain CheckHost" |
| R-06 | remove `RecheckSelectors` (prepublication) | **detected** | "prepublication verification must retain RecheckSelectors" |
| R-07 | remove `AcceptRecord` (qualified) | **detected** | qualified acceptance + asymmetry tests |
| R-08 | inject `AcceptRecord` (prepublication) | **detected** | "must not accept a qualification record" |
| R-09 | remove `SelectedRecordName` (qualified) | **detected** | "must retain record selection" |
| R-10 | swap `SameDigest`/`Receipt` (prepublication) | **invalid/inconclusive** | reviewer swap did not compile (`CS0841`); superseded by R-10b |
| R-10b | swap `EnsureExact`/`CheckDependencies` (prepublication) | **detected by order-only control** | "shared-prefix static order diverged at InstalledPayload.CheckDependencies" (all calls present) |
| R-11 | remove `Walk` (qualified) | **detected** | "VerifyActive no longer references Walk" |
| CP-01 | prepublication `CheckHost` behind env-var guard | **not detected** | 33/33 pass (§6) |
| CP-02 | prepublication `RecheckSelectors` wrapped in swallowing try/catch | **not detected** | 33/33 pass (§6) |
| CP-03 | qualified `AcceptRecord` behind env-var guard | **not detected** | 33/33 pass (§6) |
| CP-04 | F-01 semantic `Require` block behind runtime-false guard | **detected by a different existing test** | 5 E-1/F-01 class failures (§6) |

Categories are kept separate: nine detections are by the intended parity assertion, one is order-only, one is by the existing E-1/F-01 class, three are not detected, one was invalid/inconclusive, zero false positives, zero final compile rejections (the producer's M-08 after-`return` variant was independently reasoned about but the reviewer used higher-value bypasses; the build-policy behavior is consistent with `TreatWarningsAsErrors`).

## 6. Call-preserving bypass findings (including M-09)

Three independent call-preserving bypasses compile, preserve every asserted IL token, and pass all 33 tests (details in `comment-strip-and-bypass.txt`):

1. **CP-01** — prepublication `CheckHost` behind `Environment.GetEnvironmentVariable("GAGAMBA_Q1_SKIP_HOST") is null`. On a provisioned host the host baseline becomes environment-bypassable.
2. **CP-02** — prepublication `RecheckSelectors` wrapped in `try { … } catch (Exception) { }`. The store-change recheck becomes non-enforcing (silent TOCTOU acceptance).
3. **CP-03** — qualified-path `AcceptRecord` behind `GAGAMBA_Q1_SKIP_RECORD`. A receipt-bound record acceptance can be skipped while `InstalledResult.Qualified` is still returned.

**CP-04** (F-01 semantic `Require` block in `FromEvidence` behind `Environment.ProcessId < 0`) compiles and is **detected by the existing E-1/F-01 class** (5 failures): where direct behavioral tests exist, guarding the check is caught. This is complementary evidence, not parity-suite detection.

### M-09 disposition: ACCEPTABLE WITH QUALIFICATIONS

The producer's M-09 (and my CP-01/02/03) are the same class: token-preserving conditional bypass. This is acceptable within the approved F-02 contract because:

1. The binding instrument bounds the mechanism explicitly. Architecture decision §5 (Option B): IL token scanning "detects presence/absence of calls, not the internal semantics of a called helper, so it is a change-detector complemented by existing behavioral/native evidence, not a proof of equivalence"; §10 defers native differential parity to Slice N; §12 forbids substituting a general-purpose IL analysis framework.
2. All required negative controls passed. §9.8 required (a) remove `Walk`/`EnsureExact` → fail, (b) add `AcceptRecord` to prepublication → fail, (c) remove `CheckHost` from the qualified branch → fail. Independently reproduced (R-01/R-02, R-08, R-05), plus five more removal/reorder detections.
3. The finding's own purpose is met. E-1 Q1 F-02 (LOW) was "future edits to `VerifyActive` will not propagate"; the owner acceptance requires "when either verification path changes, a parity analysis is required". A mechanized, committed, loud-failing control now enforces shared-prefix presence/order and encodes every intentional difference.
4. The undetected class requires deliberately authoring a guard inside the protected method — an edit that is itself the review subject and that no test-only package can robustly detect without branch-dominance analysis (explicitly excluded).
5. No required invariant failed: this is not REMEDIATION REQUIRED; the capability question is answered by §5/§10 of the decision, so it is not RECONCILIATION REQUIRED.

The retained qualification: the parity suite detects mechanical divergence (presence/order/association/type separation), **not** branch dominance, argument correctness, or helper internals; native behavioral parity remains `NATIVE-PENDING`.

## 7. Intentional-difference verification

No false positives were observed (Q0 33/33 on the accepted implementation). Independently verified that the controls do **not** demand identical paths:

- Record association is asserted as qualified-only (presence in `VerifyActive`, absence in prepublication) — R-08/R-09 confirm both directions.
- `CheckHost`/`RecheckSelectors` are presence-checked, not ordered against each other, so the accepted fresh-branch ordering (`RecheckSelectors` first offset `0x227` before qualified `CheckHost` `0x27F`) causes no failure.
- Result types are asserted cross-absent, matching `InstalledResult` vs `InstalledIntegrityEvidence`.
- The E-1 class independently asserts null qualification association, private constructors, no authority promotion, and no caller-fed factory; all pass unchanged.
- Typed failure states vs throw are not constrained by any parity assertion.
- F-01 semantic bindings remain enforced (their tests pass; CP-04 shows they are load-bearing).
- Runtime loaded-assembly verification-to-use is outside both methods and untouched.

## 8. Ordering and branch-dominance assessment

What is **established**: in the unchanged source, both methods reference the accepted calls in the accepted static order; first-offset ordering corresponds to straight-line emission order in the shared prefix (offsets confirm a single linear region through `CheckDependencies`); R-10b proves the order control fails on a pure reorder with all calls present.

What is **not** established: any control-flow dominance. First-offset existence does not prove reachability on every successful path (CP-01..03); a duplicated earlier call could satisfy ordering while a later effective check is mispositioned; calls inside helpers (`Walk`, `EnsureExact`, `ReadTrustedFile`, `CheckHost`, `RecheckSelectors`, …) receive no coverage of their internals; no behavioral evidence of dominance exists on this host because the protected store cannot be provisioned. **Conclusion: the parity suite is not proof of universal fail-closed behavior and is not reported as such.**

## 9. Comment-only source/hash analysis

- The two production files contain only added `//` comment lines (filter-verified: no non-comment `[+-]` line), and the changed-file set is exactly the two comment edits plus the new test file (`production-diff-and-identity.txt`).
- Public and internal signatures are unchanged by construction (no code tokens touched). The Release assembly exports exactly one type (`InstalledArtifactInspection`, pre-existing); no public API was added.
- **Direct IL equivalence proof**: a reviewer scratch build with the two 5-line comment blocks removed produced byte-identical method bodies for both methods (`62145748…`/1030 and `78C97923…`/739, same as the committed build). The comment-only edits therefore change no executable IL.
- **Identity consequence**: the deterministic build embeds the commit revision (SourceLink), so whole-DLL hashes/MVIDs differ across revisions even with identical method IL. The producer recorded identities while HEAD was `071acbe` (Debug `9d312b37`/`5AC95F27…`, Release `d9b60c61`/`71F224D7…`); a rebuild at final HEAD `d221b24` yields Debug `ac25e3b5`/`311222F5…`, Release `2167863b`/`4A2F83C9…`, all with `INFO +d221b24…`. **Classification: expected identity consequence with no blocking effect** — source blob hashes are the correct anchors, and no installed-artifact or qualification evidence was produced, recycled, or invalidated by the package (no installed store exists in this checkout). A future artifact rebuild/re-approval is only required when such artifacts are actually created, which remains unauthorized.

## 10. RELEASE and production-reachability verification

Independent checks on the freshly rebuilt Release M1 (not only Debug reflection tests):

- Reviewer caller scan on the **Release** assembly: `VerifyActiveIntegrity` has exactly one caller, `PrepublicationIntegrityProjection::Verify`; `Verify` has zero callers (`reachability-and-identity.txt`). Identical result on Debug.
- `eng/VerifyProductionAssembly --production` on Release M1: **RESULT: PASS**, exit 0 — 0/4 hook types, 0/3 hook properties, 14/14 core types.
- Same verifier on Debug M1 as production: **RESULT: FAIL**, exit 1, naming `PrepublicationIntegrityProjection::ForTest` among the forbidden Debug-only factories (expected rejection preserved).
- `FromEvidence` remains private (reflection assertion in the parity suite; also unchanged by the comment-only diff).
- No new validation-authority factory, no new public API (one pre-existing exported type), no qualified-to-prepublication promotion path, and no positive validation execution route is constructible; the projection cannot derive from any caller-supplied digest/result/Boolean.
- Existing qualified admission is unchanged (no diff in `LoadedInstallation`, `FinalAdmissionVerifier`, release gates; full suite green).

## 11. P4 neutrality

The complete non-documentation change is: two comment-only production edits plus one new test file. Diff-verified outcomes:

- Test-only executable additions; comment-only production changes; no production logic/semantics change.
- No new protected-sink caller (caller scans of both configurations unchanged; no `Verify` consumer).
- F01/F02 gates, S10/S11 domination, S28 isolation, first protected effects, qualified-to-legacy downgrade refusal and RELEASE refusal behavior: unchanged.
- Preserved: F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, the 331 unqualified native skips.

**Classification: P4-NEUTRAL (confirmed).** One reviewer note: the commit-bound binary identity changes described in §9 require no P4 re-audit because no executable behavior changed.

## 12. Executed regression counts

| Check | Result |
|---|---|
| Focused parity suite | 10 / 0 / 0 |
| E-1/F-01 projection class | 23 / 0 / 0 |
| Focused installed/authority/release | 248 / 0 / 32 |
| Full Windows M1 suite | 1,401 / 0 / 331 (total 1,732); 0 build warning/error lines |
| Release build (M1) | 0 warnings / 0 errors |
| Production verifier (Release / Debug-as-production) | PASS exit 0 / FAIL exit 1 |

Passed/failed/skipped per the producer baseline and independently reproduced in kind. The 331 skips are native/missing-environment cases, **not** native qualification; PowerShell 7 remains unexecuted. Exact binary identities differ from the producer's recorded values because they are commit-bound (§9).

## 13. Findings and qualifications

| ID | Severity | Type | Summary | Impact on F-02 qualification | Remediation / retained obligation |
|---|---|---|---|---|---|
| **Q1-F01** | LOW | Structural coverage limitation | Call-preserving conditional bypasses (CP-01/02/03, M-09 class) escape a token-presence suite | Retained qualification; does not fail the authorized contract (§5/§6) | None required under Option B; branch-dominance/native behavioral parity remains a Slice N obligation and a future-Option-A trigger |
| **Q1-F02** | LOW | Structural coverage limitation | Call arguments and helper-internal semantics are not checked; a same-token call with wrong inputs would pass | Bounded; complements, not replaces, helper tests | Retained; helper behavior is covered by their own suites and native qualification |
| **Q1-F03** | LOW | Structural coverage limitation | Negative association checks are name-specific; a differently-named future authority method entering prepublication would evade the scanner | Bounded; new authority mechanisms already require review/authorization | Retained: any new authority method is out of contract and reviewable |
| **Q1-F04** | INFO | Ordering limitation | First-offset order only; no branch dominance; duplicated earlier calls could satisfy ordering | Explicitly outside the accepted guarantee | Retained; no dominance claims are made |
| **Q1-F05** | INFO | Evidence/identity note | Producer binary identities were captured pre-final-commit and are commit-bound; rebuild at `d221b24` differs while method IL is identical | No blocking effect; source blobs are the anchors | Retained: future evidence should record the commit plus rebuild identity |
| **Q1-F06** | INFO | Mutation-matrix note | Reviewer R-10 was invalid (`CS0841`, superseded by R-10b); no final compile rejections in the reviewer matrix; producer M-08 build rejection is build-policy, not parity detection | None | Recorded for accuracy |

No demonstrated parity-control defect, no P4/authority regression, no unauthorized execution path, and no failed required invariant was found. No false positives were observed.

## 14. Formal Q1 disposition

**SUPPORTED WITH QUALIFICATIONS.**

The controls satisfy the authorized F-02 contract: every mandated shared-prefix operation is present and ordered in both paths (independently reconstructed and mutation-verified), qualified-only and prepublication-only behaviors remain distinct and encoded, the required negative controls all fail the intended assertions, E-1 remains production-unreachable in Debug and Release, RELEASE restrictions are unchanged, and P4 is neutral.

The qualifications are explicit and bounded: (1) the M-09/CP-01..03 class of call-preserving conditional bypasses is accepted as outside the Option B guarantee per architecture-decision §5/§10 and reviewer §6 analysis — the suite is a mechanical divergence/regression control, not a branch-dominance prover; (2) Q1-F02/F03/F04 structural limits are retained; (3) native provisioned-store differential parity and the runtime loaded-role check remain `NATIVE-PENDING` Slice N obligations; (4) binary identities are commit-bound (Q1-F05). None of these constitute a failed required invariant, and none are remediable within the authorized test-only scope without expanding into control-flow analysis that the decision explicitly excludes.

This review does **not** close F-02 and does **not** constitute owner acceptance.

## 15. Recommended owner decision and next commission

1. **Owner acceptance decision** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-OWNER-ACCEPTANCE-01`, considering this Q1 with the retained qualifications (Q1-F01..F06), the preserved F-01/F-03 qualifications, and the unchanged P4/Slice N obligations. Only that decision may close F-02.
2. Before the first production consumer of the projection (or at the E-4/E-6 integration), revisit the architecture decision's Option A consolidation trigger with a fresh P4 re-audit, per the decision's §8/§12.
3. Keep the retained obligations visible: F-01 future-consumer fail-closed requirement; F-03 PowerShell 7 unexecuted; native differential parity and loaded-role verification in Slice N; 331 skips are not native positives.
4. No E-2 through E-7 work, validation-catalog approval, positive validation, activation, or push is authorized by this review.
