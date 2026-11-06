# Alternative B P2 sequencing remediation — independent P2-Q2 handoff

Date: 2026-11-06 (Asia/Manila)

Review commission: independent P2-Q2 review, separately commissioned

Remediation commission: `GAGAMBA-RC2B-06-B6-ALT-B-P2-SEQUENCE-REM-01`

- **Baseline:** `ec9758a69c8d1946b76d7d5e8e5578102c41bd63`, tree `bff333b79f270d2adf55021f81a3a9d8a2ac6837`.
- **Clarification commit:** `67182bf` ([P2 owner sequencing clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md)).
- **Implementation commit:** `7cc5d65a62de5338a0b3c1460af793140fb8db43`, tree `a31d33aabe14bcf58693b874c5dab635c12fb7d3`.
- **Evidence:** [P2 sequencing remediation evidence](../evidence/rc2b-06-b6-alt-b-p2-sequence-rem-01.md).
- **Governing decision:** [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) and its additive [P2 sequencing clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md).
- **Implementation handoff:** [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md).
- **P1 acceptance:** [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md).
- **Prior review:** independent P2-Q1 (`Solo/gagamba-alt-b-p2-q1`), disposition **P2-Q1 DESIGN DECISION REQUIRED** (`REPORT.md` SHA-256 `a389894c9555ac42353db4cbe6564e0155c200a7d02040f5e9a25e69c61428f7`).
- **Prior P2 evidence:** [P2 implementation evidence](../evidence/rc2b-06-b6-alt-b-p2-impl-01.md).

## 1. Review scope

Review the bounded sequencing remediation of P2 only: the qualified integrated direct owner path, refusal-only, with an early protected final-admission check before the first protected initialization effect and the retained late operation-bound recheck before manifest authorization.

Review the production decision logic and the actual owner wiring, not only the pure helper:

- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerManifestAdmissionGate.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerAuthorityContext.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/LoadedInstallation.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/ReleaseGenerationGate.cs`
- `eng/VerifyProductionAssembly/Program.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerManifestAdmissionGateTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerAuthorityContextTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs`
- the clarification decision and this evidence.

## 2. What was executed

On Windows 11 10.0.26200 AMD64 with SDK `10.0.401` and runtime `10.0.12`:

| Check | Result |
|---|---|
| Debug build of M1 tests | 0 warnings, 0 errors; instrumented verifier PASS |
| P1 + P2 focused (`OwnerAuthorityContextTests` + `OwnerManifestAdmissionGateTests`) | 29 passed / 0 skipped / 0 failed |
| Focused regression filter | 185 passed / 20 skipped / 0 failed |
| Full Windows M1 Debug runner | 1295 passed / 331 skipped / 0 failed (1626 total) |
| Release M1, Owner and Recovery builds | 0 warnings, 0 errors; production verifier PASS |
| Production-verifier negative control on Debug | FAIL as expected; detected `Grant::ForTest, Qualified::ForTest` |

The 331 native skips were not executed and are not passes. No protected root store, native host, validation session or target execution was used.

## 3. Explicit non-claims

The review must not treat this remediation as:

- complete admission-sink coverage (P4/Q04) or bootstrap compatibility, including the non-integrated B2 recovery-to-owner path and `QualifiedB2Owner`;
- owner pre-RELEASE generation verification (P3);
- validation authorization, a positive validation path or Slice E;
- protected-root/native qualification or production activation;
- a demonstrated native partial-initialization cleanup result;
- a rewrite or weakening of the original owner decision (the clarification is additive).

## 4. Required review questions

1. **Early dominance.** Is the early final-admission check genuinely after the trusted first installation verification and before the first protected initialization effect (journal, writer, allocation, domain, staging)?
2. **Early refusal without effects.** Does an early refusal perform no journal/writer/domain/allocation/staging effect, not merely return a refusal value?
3. **Trusted inputs only.** Does the early check use the retained `LoadedInstallation` and private protected grant only, never a caller Boolean, wire echo or controller manifest?
4. **Late independence.** Is the late operation-bound recheck independent and fresh, still before `Prepared`/mint/HELD, and still rejecting withdrawal or generation replacement that occurred after the early check?
5. **First-tuple binding.** Do both checks bind the original retained identity, qualification ID and record hash and never adopt a newer generation?
6. **Boundary preservation.** Are legacy, validation non-constructibility, stopping, reconciliation, C3 and sole-writer behavior preserved?
7. **P4 obligations.** Are the journal/writer/domain/allocation/staging effects and the non-integrated B2 path accurately identified as remaining P4 obligations?
8. **Native honesty.** Are positive native cases and the withdrawal-to-cleanup chain still pending rather than falsely qualified?

## 5. Suggested independent checks

- Inspect `OwnerPreparationTransaction.RunTransaction` and confirm the early `OwnerManifestAdmissionGate.TryRequire` follows `RequireLoadedInstallation()` and precedes `new JournalFileStorage()`; confirm the late recheck follows `OwnerManifestAuthorizationCodec.TryDecode` and precedes `OwnerPreparedDescription`, the `Prepared` journal write and `PreparationPublisher.Publish`.
- Confirm both call sites read only `OwnerAuthorityContext.Qualified` and a private `FinalAdmissionVerifier.Grant`, never the controller's `ManifestAuthorization` identity as admission evidence.
- Reproduce the IL effect-boundary ordering test (`InitialFinalAdmissionPrecedesEveryProtectedInitializationEffect`) and verify a negative control (removing the early call) fails.
- Confirm `OwnerAuthorityContext.Validation` still has no factory, the gate has no Boolean/legacy overload, and `Grant.ForTest` / `Qualified.ForTest` are absent from Release.
- Re-run the production verifier against the Release and Debug assemblies to reproduce the authority-factory exclusion control.
- Re-run the focused and full Windows M1 suites, or verify the recorded logs and hashes.
- Check that `HeldReleasePhase`, the release arbiter, reconciliation, recovery and stop paths do not call the manifest gate and do not require renewed admission.
- Recheck the 44 historical untracked paths and the one canonical worktree after the evidence commit.

## 6. Requested disposition

Return one of:

- **P2-Q2 SUPPORTED WITH QUALIFICATIONS**
- **P2-Q2 REMEDIATION REQUIRED**
- **P2-Q2 DESIGN DECISION REQUIRED**
- **P2-Q2 BLOCKED BY ENVIRONMENT**
- **P2-Q2 INCOMPLETE**

Do not declare full sink coverage, native qualification or production activation. Do not record P2 owner acceptance.
