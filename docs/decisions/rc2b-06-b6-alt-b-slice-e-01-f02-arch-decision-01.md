# Slice E-1 F-02 architecture decision — verification-path divergence control

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-ARCH-DECISION-01`

**OPTION B SELECTED — BOUNDED IMPLEMENTATION AUTHORIZED** (explicit parity controls, test-only package)

Date: 2026-11-08 (Asia/Manila). Reviewer and recorder: DeepSeek. Scope: architecture analysis and owner-decision documentation only, on canonical `main`; no push.

This decision selects explicit parity controls over a shared verification core and over undocumented deferral. It authorizes only the bounded `F-02-PARITY-01` package described in §9 (tests and, at most, comment-only cross-references). It does **not** authorize implementing that package, does **not** mark F-02 closed, and does **not** authorize E-2 through E-7, Slice N, or activation.

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `6d08ae569c03e96872fcc5d330435991952207d6` (matches the commission) |
| Starting tree | `a690a4390484e720239c13d9d88c024eff1e0a34` (matches the commission) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized; local `main` nine ahead) |
| Push | none |

Baseline matched exactly. No reset, clean, merge, rebase, prune or extra worktree was used.

## 2. Evidence and source inventory

Authoritative decisions: [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) (§6 F-02), [F-01 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md), [F-03 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md), [F-01 remediation entry](rc2b-06-b6-alt-b-slice-e-01-f01-remediation-entry-01.md), [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) §§3.3/3.5, [E-1 Q1](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md), [F-01 Q2](../reviews/rc2b-06-b6-alt-b-slice-e-01-f01-q2.md), [entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md), [installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md) Gates A–D, [Mechanism B acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md), [P4 acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md).

Source inspected for this decision:

- `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.cs` — `VerifyActive()` at `108–173`; shared primitives `EnsureLinuxX64` `175–181`, `VerifyPayloadCore`/`VerifyPayload`/`VerifyStaged` `75–104`, `OpenAbsoluteDirectory`, `OpenDirectory`, `ReadTrustedFile`, `Walk`, `EnsureExact`, `RecheckSelectors`, `SameDigest`, `CreateScan`, `MetadataArtifact`.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.Prepublication.cs` — `VerifyActiveIntegrity()` at `21–70`.
- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs` — `Verify()` `65–67`, private `FromEvidence` `69–104` (F-01 semantic checks at `95–98`), `ForTest` `100–101`, `InstalledIntegrityEvidence` record.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledPayload.cs` — `CheckHost`/`CheckPlatform` `126–153` (runtime inventory host binding `145–150`).
- `src/Gagamba.Execution.Linux.M1/Installed/InstallationIdentity.cs` — `Project` `25–33`, `RoleHash` `35–45`.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledManifest.cs` — `InstallJson.Hex` `80–85`, `InstalledManifest.Parse` `105–156`.
- `src/Gagamba.Execution.Linux.M1/Installed/LoadedInstallation.cs` — `Verify` `17–25`, `Revalidate` `26–29`, `VerifyFinalAdmission` `30–36`.
- Callers of `VerifyActive()`: `LoadedInstallation.cs:19` and `:28` (qualified authority chain), `FinalAdmissionVerifier.cs:65` (fresh S2 revalidation), `InstalledArtifactInspection.cs:8` (public read-only inspection).
- Deterministic IL-inspection helper: `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs` (`CallOffsets(MethodBase, MemberInfo)`), already used by five deterministic wiring tests.

## 3. Step-by-step verification-path comparison

`A` = `VerifyActive()` (`InstalledFileSystem.cs`), `B` = `VerifyActiveIntegrity()` (`InstalledFileSystem.Prepublication.cs`).

| # | Operation | A | B | Relationship |
|---|---|---|---|---|
| 1 | `EnsureLinuxX64()` | A:112 | B:25 | shared helper |
| 2 | Open `/opt/gagamba` (`trustedAncestors`, `missingUnavailable`) | A:113 | B:26 | same helper |
| 3 | `RequireDirectory(root)` | A:114 | B:27 | same |
| 4 | Open `installations` | A:115 | B:28 | same |
| 5 | Open `qualifications` | A:116 | B:29 | same |
| 6 | Snapshot root/installations/qualifications | A:118–120 | B:31–33 | identical |
| 7 | Read `active` | A:121 | B:34 | same helper |
| 8 | `InstalledMetadata.Active` | A:122 | B:35 | same |
| 9 | Open generation dir | A:124 | B:37 | same |
| 10 | Generation snapshot | A:125 | B:38 | same |
| 11 | Read `manifest.json` | A:126 | B:39 | same helper |
| 12 | `InstalledManifest.Parse` | A:127 | B:40 | same |
| 13 | `SameDigest(manifest.Digest, digest)` | A:128 | B:41 | same |
| 14 | Read `install-receipt.json` | A:129 | B:42 | same helper |
| 15 | `InstalledMetadata.Receipt` | A:130 (retained) | B:43 (discarded `_ =`) | same helper; A retains for association |
| 16 | `CreateScan(trusted)` | A:132 | B:45 | same |
| 17 | Metadata artifacts into scan | A:133–134 | B:46–47 | identical |
| 18 | `Walk` | A:135 | B:48 | same |
| 19 | `EnsureExact` | A:136 | B:49 | same |
| 20 | `InstalledPayload.CheckDependencies` | A:137 | B:50 | same |
| 21 | Qualification record association (`SelectedRecordName` + `AcceptRecord`) | A:150–153 | **absent** | **intentional** (B has null association) |
| 22 | `InstalledPayload.CheckHost` | A:156 (qualified branch only; **fresh branch skips**) | B:55 (unconditional) | **intentional difference** |
| 23 | `RecheckSelectors` | A:144–146 (fresh, `null`) / A:158–160 (qualified, record) | B:58–60 (`null`) | same helper; record parameter differs |
| 24 | Result | A:147 `Fresh` / A:161 `Qualified` | B:62–64 `InstalledIntegrityEvidence` | **intentional** (type/authority) |
| 25 | Failure classification | A:163–172 → typed `InstalledResult` states | B:66–69 → `InvalidDataException` (4 primitive types); other established exceptions propagate | **intentional** |
| 26 | F-01 semantic binding (closures/runtime inventory) | not carried (no projection) | evidence construction B:62–64; enforced in `FromEvidence` `95–98` | prepublication-specific |
| 27 | Runtime loaded-assembly "verification-to-use" | in `LoadedInstallation.VerifyLoaded` (separate; not in either method) | not performed | **deferred / unimplemented** |
| 28 | Final admission / qualification authority | downstream (`FinalAdmissionVerifier`, `LoadedInstallation`) | none | **intentional** (distinct authority domains) |

**Conclusion.** Steps 1–20 are a byte-for-byte read-and-verify prefix per path; steps 21–28 are deliberate authority/type/mapping differences. A silent future change to steps 1–20 in one path would not be detected by existing behavioral tests on an unprovisioned host (both paths refuse at `EnsureLinuxX64` on Windows), which is the divergence risk F-02 records. The two methods are **not** equivalent merely because they use the same helpers: A's fresh branch skips `CheckHost`, A's qualified branch performs association and returns `Qualified`, and B always performs `CheckHost` yet never establishes an association.

## 4. Shared versus authority-specific invariants

May be shared (integrity only, steps 1–20): fixed-root active-selection read; manifest parse + digest binding; exact payload closure; dependency closure; store-stability rechecks; (functionally) host baseline via `CheckHost`.

Must remain separate and type-distinct:

- Qualification-record association (`SelectedRecordName`/`AcceptRecord`) and the qualified `RecheckSelectors` record argument — qualified path only.
- Result types: `InstalledResult` (`Fresh`/`Qualified`) versus `InstalledIntegrityEvidence`/`PrepublicationIntegrityProjection` (null qualification association; no authority).
- Failure classification: typed `InstalledResult` states (A) versus throw-based refusal (B) with the established helper exceptions preserved.
- Authority decisions: final admission (`FinalAdmissionVerifier`), owner manifest admission, release generation — none may be reachable through any shared integrity primitive.
- Construction accessibility: `PrepublicationIntegrityProjection`'s private constructor, private `FromEvidence` and Debug-only `ForTest`; `LoadedInstallation`/`InstalledResult.Qualified` retain their existing gates.
- `CheckHost` remains mandatory on the prepublication path and on the qualified (admitted) path; no shared abstraction may make it optional.

**Sharing integrity verification must never imply sharing qualification or validation authority**, and no generic trusted result may be promotable into either authority domain.

## 5. Options A/B/C analysis

**Option A — shared verification core.** The natural extraction is steps 1–20 into a private helper returning an owned "active closure" (the four `Fd`s/`root`/`installations`/`qualifications`/`generation`, the snapshots needed by `RecheckSelectors`, `digest`, `manifest`, `receipt`), after which each caller performs 21–23 and its own result mapping. Feasibility: possible; the repo already shares `VerifyPayloadCore` for the non-authority candidate/staged reads, so shared read cores are not unprecedented. **Hazards:** (i) `RecheckSelectors` takes raw file-descriptor integers and re-opens directories from them, so extracted `Fd` ownership/disposal order must be preserved exactly or the recheck could use closed descriptors; (ii) `VerifyActive` maps exceptions to typed `InstalledResult` states (`163–172`) — a refactor can change which type/message is caught and therefore the refusal classification; (iii) highest blast radius: `VerifyActive` feeds `LoadedInstallation.Verify`/`Revalidate`, `FinalAdmissionVerifier`, and the owner/release gates; (iv) any change re-opens the qualified admission path. The benefit is maintainability only; F-02 currently has **no security impact** because the prepublication path is production-unreachable and grants nothing.

**Option B — independent paths with explicit parity controls.** Retain both implementations; add deterministic, mechanized divergence detection using the existing `IlCallScanner` (`CallOffsets`) plus focused behavioral tests. The parity contract encodes both the shared-prefix requirement and the intentional differences, so it fails when a shared check is removed from either path and when the prepublication path gains qualification-record association (or the qualified path loses it). Cost: a test-only package; limitation: IL token scanning detects presence/absence of calls, not the internal semantics of a called helper, so it is a change-detector complemented by existing behavioral/native evidence, not a proof of equivalence.

**Option C — deferred consolidation with a gate.** Deferral is *acceptable today* (production-unreachable projection; no consumer), and invalidating a consolidation decision can wait. But the commission's requirement is that deferral carry a **concrete, mechanized** gate rather than an instruction to remember; Option B *is* that mechanized gate, while a documentation-only gate is weaker. Option C alone was therefore not selected, though its timing logic (revisit before the first production consumer or at E-4/E-6 integration) is retained in §9.

## 6. Risk and tradeoff matrix

| Criterion | A (shared core) | B (parity controls) | C (defer only) |
|---|---|---|---|
| Security / trust-boundary risk | Higher: edits the authority-bearing qualified path; risk of changing refusal classification or fd lifetime | Low: test-only; production unchanged | None now; unbounded later |
| Future divergence probability | Lowest (single source) | Reduced by a mechanized detector | Unchanged; relies on memory |
| Blast radius | Qualified admission + owner/release gates | Test project only | None |
| Qualified-path regression risk | Material | None | None |
| Testability | Requires new before/after parity probes on a native-pending path | Deterministic IL + behavioral; native differential deferred | None added |
| Auditability | Good if qualified | Good (explicit contract encoded) | Weak (undocumented) |
| Maintainability | Best long-term | Adequate; intentional differences documented | Poor |
| Complexity cost | New abstraction + ownership/lifetime design | One test surface | None |
| Implementation effort | Moderate–high | Low | None |
| Qualification effort | High (must re-qualify the qualified path) | Low–moderate | None |
| Slice E compatibility | Could help later integration | Neutral; keeps options open | Defers cost |
| P4 preservation | Re-audit required | Neutral | Neutral |

The smallest defensible solution that actually addresses F-02 is **B**: it mechanizes divergence detection without touching the qualified authority path. A remains a valid future consolidation if a production consumer or the E-4/E-6 integration makes convergence necessary; that decision is expressly deferred.

## 7. P4 and RELEASE impact assessment

- **Option A:** `VerifyActive` is a P4-relevant fixed-root verifier feeding S01/S03/S15/S21/S24 and the owner/release gates. A behavioral change could alter refusal classification, the first protected effect ordering relative to admission, or RELEASE refusal. Classification: **P4 RE-AUDIT REQUIRED** before any such change.
- **Option B (selected):** test-only (plus at most comment-only cross-references). No production caller, no sink change, no first-protected-effect or downgrade-path change, no RELEASE behavior change. Classification: **P4-NEUTRAL**.
- **Option C:** no change. **P4-NEUTRAL**, but no new control.

RELEASE: none of the options adds a RELEASE factory or `ForTest` surface. The E-1 projection remains production-unreachable; no positive validation becomes constructible; no protected effect is enabled.

## 8. Selected architectural approach

**Option B — independent verification paths with explicit parity controls.** Retain `VerifyActive()` and `VerifyActiveIntegrity()` as separate implementations; add a deterministic parity/divergence control that encodes the shared-prefix invariants and the intentional differences. Do not refactor the qualified path now. Revisit consolidation (Option A) before the first production consumer of the projection or at the E-4/E-6 integration if a shared core is then justified by a fresh architecture decision and P4 re-audit.

## 9. Exact bounded implementation authorization

**Proposed commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01`.**

1. **Purpose.** Mechanize F-02 divergence detection between `VerifyActive` and `VerifyActiveIntegrity` without changing production behavior.
2. **Authorized surfaces.** A new test file under `tests/Gagamba.Execution.Linux.M1.Tests/Installed/` (for example `VerifyActiveParityTests.cs`), reusing `IlCallScanner`. Optionally, comment-only cross-references in `InstalledFileSystem.cs`/`InstalledFileSystem.Prepublication.cs` naming the parity contract — **no logic change**. No other production or test source.
3. **Required parity invariants (the encoded contract).** Both methods must reference the shared-prefix calls: `EnsureLinuxX64`, `OpenAbsoluteDirectory`, `RequireDirectory`, `OpenDirectory`, `ReadTrustedFile`, `InstalledMetadata.Active`, `InstalledManifest.Parse`, `SameDigest`, `InstalledMetadata.Receipt`, `CreateScan`, `MetadataArtifact`, `Walk`, `EnsureExact`, `InstalledPayload.CheckDependencies`, `RecheckSelectors`, and `InstalledPayload.CheckHost`. `VerifyActiveIntegrity` must **not** reference `InstalledMetadata.SelectedRecordName` or `InstalledMetadata.AcceptRecord`. `VerifyActive` must reference `AcceptRecord`. The prepublication method must remain the only caller of `VerifyActiveIntegrity`, and `VerifyActiveIntegrity` must remain called only by `PrepublicationIntegrityProjection.Verify` (extend the existing reachability assertions rather than weakening them).
4. **Required invariants.** F-01 semantic checks, null qualification association, private constructor/`FromEvidence`, Debug-only `ForTest`, RELEASE separation, production-unreachability, and P4 refusal guarantees are preserved unchanged.
5. **Existing prerequisites.** F-01 closed with qualifications; F-03 closed with qualifications; E-1 accepted with qualifications.
6. **Explicitly prohibited.** Any change to `VerifyActive`/`VerifyActiveIntegrity` logic; any shared verification helper; any production refactor; any new authority/factory/public API; any Slice N or E-2 through E-7 work; any change to `InstalledFileSystem.cs` behavior; any RELEASE-policy change.
7. **Positive regression tests.** Assert the required call sets are present in both methods and that the existing E-1 tests and qualified-path tests continue to pass.
8. **Negative/adversarial tests.** Demonstrate the parity control is load-bearing: with an **external mutated M1 copy** (canonical source untouched), (a) remove `Walk` (or `EnsureExact`) from `VerifyActiveIntegrity` → the parity test fails; (b) add `AcceptRecord` to `VerifyActiveIntegrity` → fails; (c) remove `CheckHost` from `VerifyActive`'s qualified branch → fails. Record the mutations and results.
9. **RELEASE checks.** Confirm the RELEASE production-assembly verifier still PASSes and the Debug negative control still rejects; no new `ForTest` or public surface.
10. **P4 preservation.** The package is test-only (and comment-only at most) → confirm **P4-NEUTRAL** by diff and by the reachability assertions; no re-audit is expected, but any production touch would trigger one.
11. **Independent qualification.** A separate `F-02-PARITY-Q1` must construct its **own** mutations of either path, confirm the parity control detects them, verify the intentional differences are encoded (not over-constrained), and confirm production is unchanged.
12. **Stop conditions.** If a deterministic, non-brittle parity control cannot be built from IL tokens plus existing behavioral tests, stop and report; do not substitute a documentation-only gate or refactor production to make testing easier.

## 10. Required regression and independent qualification evidence

Minimum evidence for the package: the new parity tests pass on the committed revision; the three (or more) external mutations each cause a specific parity-test failure; existing M1 suites pass (E-1 focused, installed/authority, full Windows M1 with its unchanged 331 native skips); RELEASE verifier PASS with the Debug negative control; source diff confined to the authorized test file (and optional comments); the E-1 projection remains production-unreachable and cannot construct validation authority. This evidence is deterministic/graph-based; native differential parity remains a Slice N obligation and must be named as such.

## 11. Remaining F-01 / F-03 qualifications

- **F-01 (closed with qualifications).** The three semantic relationships remain enforced (owner/recovery `RoleHash`, runtime inventory); the binding future-consumer requirement (every integrity-verification failure mode must fail closed before a production consumer is introduced) is preserved.
- **F-03 (closed with qualifications).** Windows PowerShell 5.1 harness behavior is qualified; PowerShell 7 is unexecuted; the existing nonzero-exit negative-control semantics and the scoped `$ErrorActionPreference` restoration are preserved; no verifier-policy change.
- **P4/Slice N.** P4 refusal-only acceptance, F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and all 331 unqualified native skips are preserved. No protected native positive execution is established here.

## 12. Formal architectural disposition

**OPTION B SELECTED — BOUNDED IMPLEMENTATION AUTHORIZED.** Explicit parity controls provide mechanized divergence detection with far lower production-code risk than extracting a shared core, and they are stronger than a documentation-only deferral gate. Option A (shared core) is deferred to a future architecture decision tied to the first production consumer or the E-4/E-6 integration, where it would require a P4 re-audit. Option C's timing logic is retained, but its enforcement is the Option B control.

Authorization applies **only** to the `F-02-PARITY-01` package in §9; it is not authorization to execute it within this commission, and F-02 is **not** closed.

## 13. Recommended next commission

`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01` (test-only parity/divergence controls), followed by an independent `F-02-PARITY-Q1` and a later owner acceptance. Revisit shared-core consolidation at the first production consumer or the E-4/E-6 integration. The Slice E-2 frozen-schema and validation-catalog approval work requires separate owner authorization. None starts automatically.
