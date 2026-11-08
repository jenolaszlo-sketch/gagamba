# Slice E-1 F-02 verification-path parity controls (F-02-PARITY-01)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01` (test-only implementation of the Option B parity controls authorized by `docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f02-arch-decision-01.md`).

- Implementation commit (tests + comment-only cross-references): `4c5a42f0fecff90dcb30088ca0203956e7a80032` (tree `ba021734bc42c43fd1f0c6cc8a52573707a6a972`), on canonical `main`.
- Evidence: [`rc2b-06-b6-alt-b-slice-e-01-f02-parity-01-evidence/`](rc2b-06-b6-alt-b-slice-e-01-f02-parity-01-evidence/README.md).
- Date/environment: 2026-11-08 (Asia/Manila); Windows (win32), .NET 10.0; canonical checkout `C:\Users\Laszlos\source\repos\Gagamba`; one worktree; no push.

**Formal producer disposition: IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION** (see §12; F-02 remains **open**).

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Branch / worktrees | `main`; exactly one canonical worktree |
| Starting HEAD | `071acbec4c376463e37f470aab484bc5765a74db` (matched the commission) |
| Starting tree | `e894c1f2f9d95bcf2a6964e38ce4394c671badde` (matched) |
| Tracked / index | clean at start |
| Historical untracked paths | 44, preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; local `main` was 10 ahead) |

Authorized changes only: the new `InstalledVerificationParityTests.cs`, the two comment-only cross-references, this report, its evidence directory, and the necessary current-state documentation blocks. No reset/clean/merge/rebase/prune/history rewrite, no branch or worktree, no remote synchronization, no push.

## 2. Tests and helpers added

New file: `tests/Gagamba.Execution.Linux.M1.Tests/Installed/InstalledVerificationParityTests.cs` (class `InstalledVerificationParityTests`, 10 `[Fact]`s). No change to `IlCallScanner` (reused as-is); no other test-side helpers added; no new Debug-only seams.

Helpers inside the file: `Unique` (resolves a member and fails loudly if an overload is ever added), `References` (`IlCallScanner.CallOffsets(...) > 0`), `CallersOf` (whole-M1-assembly caller scan), and the `SharedPrefix` table (label → resolved `MemberInfo`).

| Test | Enforces |
|---|---|
| `BothVerificationPathsResolveUniquely` | `VerifyActive`/`VerifyActiveIntegrity` still resolve to exactly one internal static method each. |
| `SharedIntegrityPrefixAppearsInBothPaths` | All 14 shared-prefix calls present in both methods. |
| `SharedIntegrityPrefixOrderIsIdenticalInBothPaths` | First-offset static order of the 14 shared-prefix calls in both methods. |
| `PrepublicationPathNeverEstablishesQualificationAssociation` | `SelectedRecordName`/`AcceptRecord` absent from the prepublication path. |
| `QualifiedPathRetainsRecordSelectionAndAcceptance` | Both present in `VerifyActive`. |
| `BothPathsRequireHostBaselineAndSelectorRecheck` | `CheckHost` and `RecheckSelectors` present in both methods. |
| `ResultTypesRemainDistinct` | `InstalledIntegrityEvidence` constructed only by the prepublication path; `InstalledResult.Fresh`/`Qualified` referenced only by `VerifyActive`. |
| `IntentionalQualifiedAndPrepublicationDifferencesAreEncoded` | The asymmetries are asserted as the contract (not flattened to "identical paths"). |
| `PrepublicationConstructionRemainsProductionUnreachable` | Callers of `VerifyActiveIntegrity` == exactly `PrepublicationIntegrityProjection.Verify`; `Verify` has zero M1 callers; `FromEvidence` remains private. |
| `BothPathsFailClosedOnAnUnprovisionedHost` | Behavioral fail-closed on Windows: `VerifyActive()` returns `InstalledState.Unavailable`; `VerifyActiveIntegrity()` throws `InvalidDataException`; skips on Linux as NATIVE-PENDING. |

Comment-only cross-references were added above each method naming the parity contract and the intentional asymmetry (5 lines each; see `production-diff.txt` — no code tokens changed).

## 3. Structural parity contract encoded

Shared-prefix call set asserted **present in both** (labels in the asserted order): `EnsureLinuxX64`, `OpenAbsoluteDirectory`, `RequireDirectory`, `OpenDirectory`, `ReadTrustedFile`, `InstalledMetadata.Active`, `InstalledManifest.Parse`, `SameDigest`, `InstalledMetadata.Receipt`, `CreateScan`, `MetadataArtifact`, `Walk`, `EnsureExact`, `InstalledPayload.CheckDependencies`. Plus presence-only: `InstalledPayload.CheckHost` and `RecheckSelectors` in both paths.

Qualified-only (must remain absent from prepublication): `InstalledMetadata.SelectedRecordName`, `InstalledMetadata.AcceptRecord`; and the qualified method must retain both. Prepublication-only: the `InstalledIntegrityEvidence` 5-parameter constructor; neither path may reference the other's result types.

Derivation: architecture decision §3/§9 and the inspected source (`InstalledFileSystem.cs:106–178`, `InstalledFileSystem.Prepublication.cs:13–76`). The suite fails loudly rather than silently matching the wrong member if a listed name becomes ambiguous.

## 4. Ordering and branch-coverage claims

Statically proved (IL metadata-token first-offset order): the 14 shared-prefix calls are emitted in the same relative order in both methods, in the accepted sequence ending `Walk` → `EnsureExact` → `CheckDependencies`. Mutation M-07 (swap `Walk`/`EnsureExact`) confirms this ordering assertion is load-bearing.

Deliberately **not** asserted: relative order or dominance of `CheckHost`/`RecheckSelectors`, because `VerifyActive`'s fresh (null-qualification) branch legitimately executes `RecheckSelectors` before the qualified branch's `CheckHost`; flattening that into a universal order would be a false rule (commission §4). Also not asserted: precedence of the failure-classification catches (typed `InstalledResult` states vs throw), which is an intentional difference.

Statically **not** provable by this suite: branch dominance over every path (a call token can exist in dead/unreachable code), and the internal semantics of any called helper. See §11.

## 5. Behavioral parity controls

One deterministic behavioral case is practical in this environment: `BothPathsFailClosedOnAnUnprovisionedHost`. On Windows, both paths refuse before any protected effect — `VerifyActive()` reports typed `InstalledState.Unavailable` via its `NotSupportedException` mapping, `VerifyActiveIntegrity()` throws `InvalidDataException` via its own mapping — so neither returns a trusted result. On Linux it skips with an explicit `Assert.Skip` naming the protected fixed-root store requirement (native-pending).

Provisioned-store behavioral equivalence (missing active selector, malformed manifest, digest mismatch, missing artifact, invalid role closure/runtime inventory, dependency mismatch, host-evidence mismatch, qualification-record cases) cannot be exercised on Windows without protected native facilities and is not recreated here; it remains a Slice N obligation. The 331 native/environment skips in the full suite remain unqualified and are not passes.

## 6. Intentional differences preserved

- Qualification-record association (`SelectedRecordName`, `AcceptRecord`) and the record-bearing `RecheckSelectors` call remain local to `VerifyActive`; the prepublication method has none and its projection keeps `QualificationId`/`QualificationRecordSha256` null.
- `CheckHost` remains mandatory on the prepublication path and on the qualified admitted branch; the qualified fresh branch's intentional skip is not flattened.
- Result types stay distinct: `InstalledResult` (Fresh/Qualified) vs `InstalledIntegrityEvidence`; the suite asserts cross-absence.
- Failure classification stays as accepted: typed `InstalledResult` states vs throw-based refusal with the four established exception translations.
- F-01 semantic binding (`RoleHash`/runtime-inventory `Require` checks in `FromEvidence`) and the private constructor/`FromEvidence`/Debug-only `ForTest` accessibility are untouched; the reachability test re-asserts them.
- The runtime loaded-assembly verification-to-use check was not touched (separate boundary; Slice N).

## 7. Mutation-control results

All mutations were applied **only** to a disposable external scratch copy (`…\Temp\opencode\f02-parity-mutation`; canonical source untouched, scratch hash-compared to canonical before the final run). Each mutation rebuilt the scratch M1 and ran the real parity suite. Full detail: `mutation-controls.txt` + `mutation-controls-assertions.txt`; harness: `mutation-harness.ps1`.

| # | Mutation (scratch only) | Observed | Expected |
|---|---|---|---|
| M-00 | none (positive control) | **10/10 pass** | 10/10 pass |
| M-01 | remove `Walk` from prepublication | 2 fail: prefix presence + order | detected |
| M-02 | remove `CheckHost` from prepublication | 1 fail: host/recheck test | detected |
| M-03 | inject `AcceptRecord` into prepublication | 2 fail: association + asymmetry | detected |
| M-04 | remove `AcceptRecord` from qualified path | 2 fail: qualified acceptance + asymmetry | detected |
| M-05 | remove `CheckHost` from qualified branch | 1 fail: host/recheck test | detected |
| M-06 | remove `RecheckSelectors` from prepublication | 1 fail: host/recheck test | detected |
| M-07 | swap `Walk`/`EnsureExact` order (prepublication) | 1 fail: shared-prefix order | detected |
| M-08 | move `CheckHost` after `return` | **build-rejected**: `CS0162` as error (`TreatWarningsAsErrors`) | rejected or documented |
| M-09 | conditional runtime-false bypass of `CheckHost` (call token kept) | **10/10 pass — not detected** | documented limitation |
| M-10 | remove `SelectedRecordName` from qualified path | 1 fail: record selection | detected |
| M-11 | remove `CheckDependencies` from prepublication | 2 fail: prefix presence + order | detected |

Every detected mutation failed with the intended assertion (e.g. M-01: *"VerifyActiveIntegrity no longer references Walk"*; M-07: *"shared-prefix static order diverged at EnsureExact"*; M-05: *"qualified verification must retain CheckHost"*). No test was weakened to obtain these results.

## 8. RELEASE and production-reachability results

- `eng/VerifyProductionAssembly --production` on the Release M1: **RESULT: PASS**, exit 0 — 0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden Debug-only factories (`release-verifier.txt`).
- Same verifier on the Debug M1 as production: **RESULT: FAIL**, exit 1, with `PrepublicationIntegrityProjection::ForTest` named among the forbidden factories (`debug-negative-control.txt`) — the negative control remains intact.
- In-assembly reachability (Debug M1, asserted by the parity suite): `VerifyActiveIntegrity` is called only by `PrepublicationIntegrityProjection.Verify`; `Verify` has no M1 caller; `FromEvidence` is private. E-1 therefore remains production-unreachable and cannot construct positive validation authority; the projection cannot be promoted into `InstalledResult`/`LoadedInstallation` authority.
- No public API was added; the production diff is comment-only; no new `ForTest`/factory/Boolean seam exists. (Note: RELEASE absence of the Debug-only `ForTest` seam is established by `eng/VerifyProductionAssembly`, not by this test project, which references the Debug M1 assembly by design.)

## 9. P4 neutrality

Confirmed by diff and by tests: neither verification method's executable logic changed (production diff is 10 added comment lines, `production-diff.txt`); no protected sink gained a caller; F01/F02 authority gates and the qualified admission chain (`LoadedInstallation`, `FinalAdmissionVerifier`, release gates) are untouched; S10/S11 domination, S28 reachability, first protected effects and the qualified-to-legacy downgrade refusal are unchanged; RELEASE refusal behavior is unchanged. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 native skips are preserved. Classification: **P4-NEUTRAL**.

One observation for reviewers: the instrumented M1 identity changed versus the pre-comment build (`C14875A…` → `5AC95F2…`) because Roslyn's deterministic compilation hash covers source text, so comment-only edits change the MVID/assembly hash even though the emitted method IL is unaffected; the comment-only diff is the load-bearing evidence.

## 10. Test counts and build results

| Check | Result |
|---|---|
| Focused parity suite (Debug) | **10 passed / 0 failed / 0 skipped** |
| Full Windows M1 suite (Debug, post-change) | **1,401 passed / 0 failed / 331 skipped / 1,732 total** (= accepted 1,391 + 10 new controls) |
| E-1/F-01 class `PrepublicationIntegrityProjectionTests` | **23 passed / 0 failed / 0 skipped** |
| Focused installed/authority/release/final-acceptance | **248 passed / 0 failed / 32 skipped** |
| Release build (`Gagamba.Execution.Linux.M1`) | 0 warnings / 0 errors |
| Production verifier (Release / Debug-as-production) | PASS exit 0 / FAIL exit 1 |

The 331 skips are native/missing-environment cases and are **not** passes; they are not re-qualified by this commission. The 51-probe PowerShell 5.1 harness was not rerun (F-03 evidence stands; PowerShell 7 remains unexecuted).

## 11. Explicit coverage limitations

1. **Presence/order ≠ reachability.** The static control detects call-token presence and straight-line first-offset order; it cannot prove branch dominance or that a present call is reachable. M-09 demonstrates this concretely (a call-preserving conditional bypass passes all 10 tests). M-08 shows the repository build policy (`TreatWarningsAsErrors`) independently rejects at least the dead-code variant of that class.
2. **Helper internals are out of scope.** A semantic weakening inside `Walk`, `EnsureExact`, `ReadTrustedFile`, `CheckHost`, etc. is invisible to this suite; those helpers are covered by their own tests and, for protected success, by Slice N native qualification.
3. **Fail-safe direction.** The static control argues about the presence of security checks, not their correctness; a change that *adds* a check passes. Over-constraining is limited to intentional-difference assertions.
4. **Behavioral parity is thin by necessity.** Only the unprovisioned-host refusal is behaviorally compared; full provisioned-store differential testing and the runtime loaded-role verification-to-use check remain `NATIVE-PENDING` Slice N obligations, as accepted in the architecture decision.
5. **Mutation controls are producer-authored.** They demonstrate the suite is load-bearing for the listed mutations but do not replace independent qualification with original mutations (including the call-preserving class).
6. **RELEASE seam absence** is proven by the canonical verifier, not by the Debug-referencing test project.

## 12. Recommended independent qualification scope

`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-Q1` should, without rerunning the producer suite as the primary method:

1. Inspect the exact structural and ordering claims against source and the IL scanner.
2. Construct original external mutations of either path (including removals not in M-01..M-11, branch-condition changes, and at least one call-preserving bypass) and confirm which the suite detects; judge whether the M-09 class is acceptable under the accepted Option B scope or requires a follow-on control.
3. Confirm intentional differences do not cause false positives (no flattening of the fresh-branch `CheckHost` skip or of qualified-only association).
4. Verify no production-code change (diff/hashes) and that the comment-only edits carry no logic.
5. Confirm E-1 stays production-unreachable, P4 neutrality and RELEASE separation hold, and the verifier results reproduce.
6. Identify any unsupported dominance or native-behavior claims; carry the native-pending obligations forward. Only then can an owner acceptance consider F-02.

## Formal producer disposition

**IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION.** The authorized Option B parity controls are implemented, all required positive/negative/intentional-difference controls executed, and the required evidence is committed with this report. Limitations in §11 are inherent to the accepted Option B scope and are named, not hidden. This is a producer disposition only: it is not independent qualification or owner acceptance, and **F-02 remains open**.

No production refactor, shared verification helper, E-2–E-7 work, Slice N work, activation, or push occurred. Recommended next: independent `F-02-PARITY-Q1`, then an owner decision; consolidation (Option A) remains deferred to the first production consumer or the E-4/E-6 integration per the architecture decision.

