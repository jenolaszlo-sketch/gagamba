# Alternative B P2 - independent review handoff

Date: 2026-11-06 (Asia/Manila)

Review commission: independent P2 review, separately commissioned

Implementation commission: `GAGAMBA-RC2B-06-B6-ALT-B-P2-IMPL-01`

- **Implementation source:** `56c637a772ab6b82c12c8de06a35673218510f12`, tree `62d9546d29672eb860ec35c40ad3a6a1ed9fc53a`.
- **Baseline:** `b0a29acb5c40982298a52ef3dc7426361703117c`, tree `68c4b753675106ee0949f4a43e45b211cc2e70e3`.
- **Evidence:** [P2 implementation evidence](../evidence/rc2b-06-b6-alt-b-p2-impl-01.md).
- **Governing decision:** [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md).
- **Implementation handoff:** [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md).
- **P1 acceptance:** [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md).

## 1. Review scope

Review P2 only: owner-side manifest authorization for new qualified authority, refusal-only, on the integrated direct owner path.

Review the production decision logic and the actual owner wiring, not only the pure helper:

- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerManifestAdmissionGate.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerAuthorityContext.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs`
- `eng/VerifyProductionAssembly/Program.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerManifestAdmissionGateTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs`

## 2. What was executed

On Windows 11 10.0.26200 AMD64 with SDK `10.0.401` and runtime `10.0.12`:

| Check | Result |
|---|---|
| Debug build of M1 tests | 0 warnings, 0 errors; instrumented verifier PASS |
| `OwnerManifestAdmissionGateTests` | 14 passed / 0 skipped / 0 failed |
| P1 `OwnerAuthorityContextTests` | 10 passed / 0 skipped / 0 failed |
| Focused regression filter | 179 passed / 20 skipped / 0 failed |
| Full Windows M1 Debug runner | 1290 passed / 331 skipped / 0 failed |
| Release M1, Owner and Recovery builds | 0 warnings, 0 errors; production verifier PASS |
| Production-verifier negative control on Debug | FAIL as expected; detected `Grant::ForTest, Qualified::ForTest` |

The 331 native skips were not executed and are not passes. No protected root store, native host, validation session or target execution was used.

## 3. Explicit non-claims

The review must not treat P2 as:

- complete admission-sink coverage (P4/Q04) or bootstrap compatibility;
- owner pre-RELEASE generation verification (P3);
- validation authorization, a positive validation path or Slice E;
- protected-root/native qualification or production activation;
- a claim that journal, writer, allocation/domain or staging effects are gated. Those effects precede the manifest gate and remain P4 obligations.

## 4. Required review questions

1. **Gate dominance.** Is every P2-protected owner manifest grant dominated by the new final-admission gate?
2. **Trusted evidence.** Does the gate independently obtain trusted evidence?
3. **Direct Start bypass.** Can a direct Start caller bypass authorization despite successful process authentication?
4. **Tuple substitution.** Can caller-provided identity or a later echo replace the retained P1 tuple?
5. **Withdrawal versus stopping.** Does admission withdrawal refuse new grants without disabling stopping?
6. **Boundary preservation.** Are legacy and validation boundaries preserved?
7. **P4 obligations.** Are effects that still precede the gate accurately identified as P4 obligations?
8. **Native honesty.** Are positive native cases still pending rather than falsely qualified?

## 5. Suggested independent checks

- Inspect `OwnerPreparationTransaction.RunTransaction` and confirm the P2 gate follows `OwnerManifestAuthorizationCodec.TryDecode` and precedes `OwnerPreparedDescription`, the `Prepared` journal write, `PreparationPublisher.Publish` and HELD.
- Confirm the gate reads only `OwnerAuthorityContext.Qualified.Selection` / `RequireLoadedInstallation` and a private `FinalAdmissionVerifier.Grant`, never the controller's `ManifestAuthorization` identity as admission evidence.
- Confirm `AdmissionAuthority.BindingMatches` requires the accepted selector, exact acceptance record and Production build configuration, and that `ReleaseGenerationGate.RequireStillQualified` rejects newer identity, qualification id or record hash.
- Confirm `OwnerAuthorityContext.Validation` still has no factory, the gate has no Boolean/legacy overload, and `Grant.ForTest` / `Qualified.ForTest` are absent from Release.
- Re-run the production verifier against the Release and Debug assemblies to reproduce the authority-factory exclusion control.
- Re-run the focused and full Windows M1 suites, or verify the recorded logs and hashes.
- Check that `HeldReleasePhase`, the release arbiter, reconciliation, recovery and stop paths do not call the manifest gate and do not require renewed admission.
- Recheck the 44 historical untracked paths and the one canonical worktree after the evidence commit.

## 6. Requested disposition

Return one of:

- **P2 SUPPORTED WITH QUALIFICATIONS**
- **P2 REMEDIATION REQUIRED**
- **P2 DESIGN DECISION REQUIRED**
- **P2 BLOCKED BY ENVIRONMENT**
- **P2 INCOMPLETE**

Do not declare full sink coverage, native qualification or production activation.
