# GAGAMBA-RC2B-06-B6-ALT-B-P4-03-OWNER-ACCEPTANCE-01

**P4-03 SOURCE IMPLEMENTATION ACCEPTED WITH EXPLICIT QUALIFICATIONS**

Date: 2026-11-06 (Asia/Manila). Executor: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This owner approval is limited to the independently reviewed **S02/O1 qualified B3 constructor-provider authority boundary**. Supporting disposition: **P4-03-Q1 SUPPORTED WITH QUALIFICATIONS**. This is not acceptance of P4 as a whole, broader Q04/S09 sink coverage, positive validation bootstrap (Slice E), protected native qualification (Slice N) or production activation.

## 1. Source identity and completed independent evidence

| Item | Exact identity |
|---|---|
| Canonical repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main`, one worktree |
| Accepted implementation | `ead317ee96c6c177ca1fc1568192c6bf6ea28a90` |
| Implementation tree | `1b65806e3089ec30d5d1bd6942a104267b2c03d7` |
| Implementation source subtree | `e558d9e9034008dcc7d4cd0f0bf43fe838764bc5` |
| Implementation test subtree | `fd96cbbf08ec48fe7f054e9c48966ae80be8b163` |
| Baseline | `5d47624bd6661f0065f3a3cca11b72ac52bd9a3c`, tree `dbb810dafc7a371b0c8756d73f1f70123e95e7f2`; baseline `src` `24db6ae4…` = accepted P4-02 Q1 source |
| Evidence/documentation HEAD at acceptance | `08d60bb7d78a61b5dabedb1f388a8e26ff6371fc`, tree `cd0a9d88d3b8b9021052c9a73a679ff3a9f50d78` |
| Independent Q1 package | `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-03-q1` |
| Q1 manifest | All **48** self-excluded SHA-256 entries verified, zero mismatches; manifest SHA-256 `c46a5dc52be4e3b7a2c13c4b9abaa3f61397fb4da6706ef739b4fa8536c1b790` |
| Q1 `REPORT.md` SHA-256 | `510395ba7ff1d1c6006f3c64e2bcecdd36d2bd52e4b2dcffb2d61c9c6d9ec694` |
| Q1 `findings.md` SHA-256 | `3ecdfe2767c5a099eeccd07d32211a6e54e0e06cf657bd39c1f8522f9eedfacc` |
| Q1 `owner-recommendation.md` SHA-256 | `9e3519e0e47af90136008c49773a3be8ec4647db51c9c198cd6484bb89197227` |
| Q1 constructor/caller + effect-order evidence | `constructor-caller-audit.md`, `authority-effect-order-evidence.md`, `focused-probes.md`, `remaining-p4-coverage.md` |

At this decision's preflight, HEAD/tree matched exactly, tracked files/index were clean, one canonical worktree remained, and all 44 historical untracked paths were preserved. No prior or duplicate P4-03 acceptance was found. The external producer package `Solo/gagamba-alt-b-p4-03-impl-01` was byte-verified earlier (268/268). No concurrent modifying agent was identified; no global exclusion lock is claimed.

The independent Q1 review re-read the committed implementation, re-verified the source-file hashes against the producer package, and independently rebuilt and ran the focused tests, the recovery topology regressions, the authority/lifecycle filter, the full Windows M1 suite, the isolated D6.4 suite, Debug/Release builds and the production verifier. It additionally authored its own reflection probe against the actual committed binaries and the preserved P4-02 baseline binaries. Q1 execution used Codex with a fresh review session; this is independent review, not cross-model certification or native proof.

## 2. Accepted properties

The owner accepts the following, solely for the independently reviewed S02/O1 boundary at `ead317ee…`:

1. Both B3 constructor entry points enforce the qualified authority contract. `B3PreparationOwner(profile, provider)` delegates a null context to the guarded constructor, and `B3PreparationOwner(profile, provider, providerInstallation)` runs the guard before every constructor effect.
2. Qualified construction requires the actual retained provider selection. The sole production factory caller passes the retained `LoadedInstallation.Verify("provider")`; no production caller uses the two-argument overload.
3. Provider-role verification and the original installation identity, qualification ID and raw record hash remain bound. `B3ProviderAdmission.Require` re-enters the literal provider-role verifier with the original identity and requires the fresh observation to remain the exact original tuple; a later observation is never adopted.
4. Fresh protected final admission is checked before qualified B3 setup and journal-directory creation. `retained.VerifyFinalAdmission()` returns a fresh private grant validated by the shared `AdmissionAuthority.IsFinalAdmissionEligible` decision.
5. Missing or mismatched authority refuses without creating the journal directory. Absent, unqualified, unavailable, incomplete, fabricated, withdrawn and mismatched contexts refuse; the independent probe observed `JournalCreated = false` on every qualified refusal in Debug and Release.
6. The P4-02 F02 topology refusal remains intact: qualified/non-integrated construction throws before any effect, even with a supplied installed context.
7. Legacy and stopping/reconciliation paths retain accepted behavior. The two-argument legacy route, the legacy integrated classification and the stop/reap/reconcile paths are unchanged; `StopCore`, `DisposeCore` and `Reconcile` never call the new guard.
8. No positive validation or new RELEASE authority is introduced. `B3ProviderAdmission` adds no issuer, grant factory, injected verifier, validation exception, manifest authority or RELEASE writer; P1 retained authority, P2 manifest admission, P3 owner generation gate and controller Q10 are byte-identical to the accepted baseline.

## 3. Preserved qualifications

The review's four non-blocking qualifications are recorded verbatim in scope:

- **Q1-F01 (qualification) — refusal-reason typing under the narrow constructor-recheck race.** In the microsecond window between the factory's protected read and the constructor's re-read, an `InvalidDataException` from `B3ProviderAdmission.Require` is currently mapped by the factory's outer catch to `RestrictedRefusalReason.InstallationUnavailable`, whereas the equal factory pre-check returns `UnqualifiedInstallation`. Both refuse with zero effects and no authority granted. Minimal future remediation: route the constructor's `InvalidDataException` through `ReleaseGenerationGate.MapFailure`, or return a typed refusal from `Require`.
- **Q1-F02 (qualification) — rebuilt binaries are not bit-reproducible.** An independent rebuild does not reproduce the producer's binary SHA/MVIDs; source identity is independently verified (`e558d9e9…`, `fd96cbbf…`). Producer binary identity is therefore not independently reproducible.
- **Q1-F03 (boundary note) — reflection-fabricated `LoadedInstallation` is outside the accepted threat-model contract.** An uninitialized reflection-created `LoadedInstallation` would raise `NullReferenceException` rather than `InvalidDataException`; `LoadedInstallation`'s private constructor plus `Verify` always set a non-null `Selection`, so the shape is not production-reachable.
- **Q1-F04 (inherited limitations) — Slice E validation and Slice N protected native qualification remain pending.** Protected installed-positive/withdrawal/mismatch, process/fd custody, native RELEASE, stop/reap and partial cleanup remain unqualified; all 331 native skips are not passes; the accepted observation-to-effect residual remains.

All existing P1–P3 and P4-02 qualifications are retained without change. The P1, P2 and P3 owner decisions and the P4-02 owner acceptance are not rewritten, superseded or expanded by this acceptance.

## 4. Evidence counts and mandatory qualifications

| Q1 check | Result and scope |
|---|---|
| New P4-03 constructor tests | 19 passed, 0 failed, 0 skipped |
| P4-02 recovery/topology regressions | 14 passed, 0 failed, 0 skipped |
| Authority/authentication/P1/P2/P3/stopping/C3/reconciliation/capture/installation filter | 352 passed, 0 failed, **58 native skipped / NotExecuted** |
| Full Windows M1 (independently rerun) | **1,368 passed, 0 failed, 331 native skipped (1,699)** |
| Isolated D6.4 | 3 passed, 0 failed |
| Debug/Release builds | 0 warnings, 0 errors |
| Release production verifier / Debug negative control | Release PASS (exit 0); Debug FAIL (exit 1), listing only pre-existing Debug-only authority factories and no P4-03 surface |
| Independent constructor/effect probe | committed Debug and Release 6/6 rows pass; preserved P4-02 baseline fails only the qualified/integrated omission row (`JournalCreated = true`) |

All 331 native skips and the 58 filter skips are **not passes**. Source-level ordering, IL assertions and deterministic tests do not establish protected native lifecycle behavior, process custody, native stopping/reaping or partial cleanup. Protected installed-positive qualification remains **Slice N, pending**; actual native cleanup remains Unknown/pending.

## 5. Next milestone recommendation

P4 is **not** wholly accepted. Remaining obligations from the original 29-row [inventory](../evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md):

- **Covered earlier:** S01/S03 factory/client provider gates; S07–S09 F01 recovery-initialization gate; S15–S17 P1; S18–S23 P2; S24–S25 P3/Q10.
- **P4-03 (this acceptance):** S02 both constructor entries.
- **Refusal-only routes not locally guarded by design:** S10 `M1RecoveryScope.StartOwnerPreparation` and S11 owner spawn rely on F01 domination on the installed integrated route; other callers are legacy/B2/test.
- **Test-only:** S28 (`PreparationOwnershipCoordinator`, no production caller); S29 production closure and unavailable public factory unchanged.
- **Separate milestones:** Slice E atomic validation bootstrap (T16–T18/T20); Slice N protected native qualification.

Recommended next milestone: a **final P4 coverage reconciliation** that determines whether all refusal-only coverage requirements are met or a specific additional implementation slice remains necessary. This acceptance does **not** authorize that reconciliation or any additional P4 implementation.

## 6. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Production, tests, historical evidence and previously accepted owner decisions remain unchanged. The external acceptance receipt is under `Solo/gagamba-alt-b-p4-03-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No new branch/worktree, reset, clean, restore, rebase, prune, push, privileged provisioning, production activation, additional P4 implementation, Slice E or Slice N. Preserve all 44 historical untracked paths. Stop after this bounded P4-03 acceptance and the recommendation above.
