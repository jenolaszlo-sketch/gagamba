# Alternative B P3 - independent review handoff

Date: 2026-11-06 (Asia/Manila)

Review commission: independent P3 review, separately commissioned

Implementation commission: `GAGAMBA-RC2B-06-B6-ALT-B-P3-IMPL-01`

- **Implementation source:** `b7a274f517dc24f4aa5e9e34338bc4c2769f5b9c`, tree `8db23e2efdd74aad3909513d2d2f3ed0828d1d25`.
- **Baseline:** `b9459d346ec26827ab807379376974f8fe1d21dd`, tree `808f4bc917d60640e202a5e441e970dd785cb6a4`.
- **Evidence:** [P3 implementation evidence](../evidence/rc2b-06-b6-alt-b-p3-impl-01.md).
- **Governing decision:** [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md).
- **Implementation handoff:** [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md).
- **P1 acceptance:** [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md).

## 1. Review scope

Review P3 only: owner-side pre-RELEASE active-generation verification for a retained qualified
operation, refusal-only, on the integrated direct owner path.

Review the production decision logic, the actual owner wiring and the final after-intent path,
not only the pure equality helper:

- `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerReleaseGenerationGate.cs`
- `src/Gagamba.Execution.Linux.M1/Execution/OwnerGenerationGateRefusal.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerAuthorityContext.cs`
- `src/Gagamba.Execution.Linux.M1/Installed/ReleaseGenerationGate.cs`
- `eng/VerifyProductionAssembly/Program.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Execution/OwnerReleaseGenerationGateArbiterTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerReleaseGenerationGateTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs`

## 2. What was executed

On Windows 11 10.0.26200 AMD64 with SDK `10.0.401` and runtime `10.0.12`:

| Check | Result |
|---|---|
| Debug build of M1 tests | 0 warnings, 0 errors; instrumented verifier PASS |
| P3 focused (`~OwnerReleaseGenerationGate`) | 27 passed / 0 skipped / 0 failed |
| `~ReleaseGenerationGateTests` (Q10 + P3) | 20 passed / 0 skipped / 0 failed |
| P2 focused regression filter, with P3 | 196 passed / 20 skipped / 0 failed |
| P2 focused regression filter, excluding P3 | 185 passed / 20 skipped / 0 failed |
| Full Windows M1 Debug runner | 1,322 passed / 331 skipped / 0 failed |
| Release M1, Owner and Recovery builds | 0 warnings, 0 errors; production verifier PASS |
| Production verifier on the Release M1 DLL | PASS |
| Production-verifier negative control on Debug | FAIL as expected; detected `Grant::ForTest, OwnerReleaseGenerationGate::VerifyObserved, Qualified::ForTest` |

The 331 native skips were not executed and are not passes. No protected root store, native host,
validation session, generation switch or target execution was used.

## 3. Explicit non-claims

The review must not treat P3 as:

- a protected fixed-root positive or a native generation-race detection claim;
- admission-sink coverage (P4/Q04) or bootstrap compatibility;
- validation authorization, a positive validation path or Slice E;
- protected-root/native qualification or production activation;
- a claim that root mutation is atomically excluded from the write interval. The accepted post-scan observation-to-effect residual stands.

## 4. Required review questions

1. **Gate dominance.** Is every new physical RELEASE attempt on the qualified owner path dominated by the fresh after-intent active-generation verification?
2. **Outside-lock placement.** Does the verification run outside `_gate`, `_journalGate`, safety latches and capability locks, with no test barrier, journal I/O, protocol wait or unrelated I/O between the successful observation and final gate entry?
3. **Full comparison.** Is the comparison the full retained identity plus qualification id and raw record hash, with no null skip and no adoption of the observed selection?
4. **Refusal shape and capability.** On refusal, is the conservative post-intent shape retained with zero physical attempts, the capability closed once, no deadline renewal, no retry and no new writer?
5. **Stop authority.** Does the refusal latch the existing stop path under the original context and C3 authority so the owner becomes safe without a controller request?
6. **Winner preservation.** Do the existing retained-result, stop, deadline, prerequisite and liveness rechecks still precede the new check, and does a duplicate return C4 without a second attempt?
7. **Legacy and controller preservation.** Are the legacy path and the controller-side Q10 check (`IntegratedB3Client`) preserved unchanged, with no new journal schema, writer or public API?
8. **Production exclusion.** Is the verification wired only from the sealed owner context (no settable caller callback, Boolean or wire echo), and is the Debug-only observation entry excluded from Release?
9. **Native honesty and residual.** Are the fixed-root positive and generation-race rows explicitly native-pending, and is the post-scan observation-to-effect residual reported rather than promised away?

## 5. Suggested independent checks

- Inspect `ReleaseArbiter.RequestRelease` and confirm `InvokeOwnerGenerationGate()` runs immediately after `Barrier("after-intent")` and before the final `lock (_gate)`, and that the P3 refusal is the last check before `Barrier("before-consume")` and returns without reaching the `TryPlanReleaseCommitted` block.
- Confirm the P3 refusal path retains `Refused`/`MayHaveExecuted`/`NotAttempted`/`DurableCommitted=false`/`LiveExecutionExcluded=true`/`Binding.Accepted` with reason `release gate refused: owner pre-RELEASE generation verification failed ({error})`, and that `SubmitOwnerGateRefusalStopTail` calls `RequestStop(StopCauseKind.SetupFault)` outside every lock.
- Confirm `OwnerReleaseGenerationGate.Verify` reuses `ReleaseGenerationGate.RequireBoundGenerationActive`/`LoadedInstallation.Revalidate` (no second trust root), returns null for `Legacy`, refuses every other context fail-closed, and never adopts the observation.
- Confirm `HeldReleasePhase` wires the verification only for a retained `Qualified` context as a method group, guards the constructor to qualified-or-legacy, and latches `_safety?.AcceptStop(SetupFault)` only on the recognized refusal tags in both production branches.
- Re-run the P3 focused and full Windows M1 suites, or verify the recorded logs and hashes; confirm the P2 focused filter is 196/20 with P3 and 185/20 without it.
- Re-run the production verifier against the Release and Debug assemblies to reproduce the factory-exclusion control.
- Confirm `Bootstrap/IntegratedB3Client.cs`, `Launch/HeldExecution.cs`, the journal schema, the C3 deadline and the sole `IReleaseWriteChannel.TryWriteOnce` call are unchanged.
- Recheck the 44 historical untracked paths and the one canonical worktree after the evidence commit.

## 6. Requested disposition

Return one of:

- **P3 SUPPORTED WITH QUALIFICATIONS**
- **P3 REMEDIATION REQUIRED**
- **P3 DESIGN DECISION REQUIRED**
- **P3 BLOCKED BY ENVIRONMENT**
- **P3 INCOMPLETE**

Do not declare native qualification, validation authorization or production activation.
