# Alternative B P2 - qualified owner manifest authorization evidence

**P2 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW**

Date: 2026-11-06 (Asia/Manila)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-P2-IMPL-01`

Governing owner decision: [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md)

Accepted implementation handoff: [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md)

P1 owner acceptance: [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md)

- **Slice:** Alternative B P2 - normal qualified owner manifest authorization, refusal-only.
- **Baseline source SHA:** `b0a29acb5c40982298a52ef3dc7426361703117c` (tree `68c4b753675106ee0949f4a43e45b211cc2e70e3`).
- **Implementation source SHA:** `56c637a772ab6b82c12c8de06a35673218510f12` (tree `62d9546d29672eb860ec35c40ad3a6a1ed9fc53a`). This evidence document is additive and changes no production source.
- **Branch:** canonical `main`. No push, reset, rebase, clean, prune, branch or worktree operation.
- **Host OS/version:** Microsoft Windows 11 Home Single Language 10.0.26200, build 26200.
- **Architecture:** AMD64.
- **Runtime/SDK:** .NET SDK `10.0.401`; `Microsoft.NETCore.App 10.0.12`.
- **Relevant native prerequisites:** none for the deterministic tests. No protected Linux root store, protected installation, native host or delegated cgroup was provisioned or exercised.
- **Qualification scope:** deterministic owner-side decision, refusal, wiring and effect-ordering checks on Windows; Debug and Release build identity; production Release exclusion of Debug-only authority factories.
- **Explicit exclusions:** no protected final-admission positive; no native Linux qualification; no validation authority or positive validation path; no non-integrated B2/`QualifiedB2Owner` sink audit; no P3, P4, Slice E, Slice N, production activation, publication or push.

## 1. Disposition

**P2 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

P2 adds a refusal gate at the operation-bound qualified manifest acceptance point. A qualified direct owner cannot reach the `Prepared` transition, capability mint or HELD publication without a fresh protected final-admission grant bound to the owner's first retained S2 selection.

P2 does **not** claim complete admission-sink coverage (P4/Q04), validation authorization, native protected-store qualification, production activation or bootstrap completeness. The 331 skipped native cases remain unqualified.

## 2. Required P2 behavior and implementation

| Requirement | Implementation |
|---|---|
| Require the retained normal qualified context before new installed allocation, staging, capability mint or HELD | `OwnerPreparationTransaction.RunTransaction` requires `OwnerAuthorityContext.Qualified` and the actual retained `LoadedInstallation` before `JournalFileStorage`, writer acquisition, allocation or staging. `OwnerManifestAdmissionGate` consumes the same context. |
| Reuse protected `VerifyFinalAdmission()` / `IsFinalAdmissionEligible(...)`; no second trust root | `OwnerManifestAdmissionGate.TryRequire` calls `qualified.RequireLoadedInstallation().VerifyFinalAdmission()` and then `AdmissionAuthority.IsFinalAdmissionEligible(retained, grant)`. |
| Fresh protected check after the manifest is known and before `Prepared`, mint or publisher | The gate runs after `ManifestAuthorization` decoding and identity binding, and before `OwnerPreparedDescription`, `journal.WriteNext(..., Prepared)`, `PreparationPublisher.Publish`/mint and HELD. |
| Refuse missing, malformed, unavailable, stale, replaced, withdrawn or mismatched authority | `VerifyFinalAdmission` fails closed on protected-store/S2 failure; `RequireEligible` refuses a null grant, a binding mismatch or a fresh selection mismatch; `AdmissionAuthority.BindingMatches` now also requires `selector.Accepted`, exact record acceptance and Production build configuration. |
| Bind to the original retained installation identity | The grant is created from `LoadedInstallation.Selection`, which is the first retained selection. `IsFinalAdmissionEligible` binds the protected selector/record to that selection; `ReleaseGenerationGate.RequireStillQualified(grant.Selection, retained)` rejects a newer generation, qualification id or record hash. |
| Reject later controller substitution | The controller's `ManifestAuthorization` identity remains checked against the original Start transaction identity. It is never used as the installation selection or as final-admission evidence. The gate reads only the retained owner context. |
| Preserve the first verified selection; never adopt a newer generation | `RequireEligible` compares and refuses; it never assigns or caches the fresh observation. |
| Preserve trusted error classification | `TryRequire` maps failures through the existing `ReleaseGenerationGate.MapFailure` and reports the classification in the owner abort detail. |
| No Boolean/caller claim/wire echo/test-only mode bypass | The gate accepts only a sealed `OwnerAuthorityContext.Qualified` and a private `FinalAdmissionVerifier.Grant`. There is no Boolean, wire value, profile flag or test-mode branch. The only Debug-only additions are a test-shaped `Grant.ForTest` factory and the existing P1 context factory, both excluded from Release and checked absent by the production verifier. |
| Keep validation non-constructible | `OwnerAuthorityContext.Validation` still has a private constructor and no factory. P2 adds no validation path. |
| Preserve legacy | A request without a qualified direct installation identity continues through the existing legacy context; the P2 gate is not invoked. Legacy cannot enter the qualified gate. |

## 3. Existing authority-granting path and P2 placement

The traced production chain is:

1. **Original controller authentication:** `IntegratedRecoveryHost`/`QualifiedRecoveryConnection.Start` authenticates the original controller and launches the original recovery and owner with the accepted direct topology. `OwnerDirectConnection.Accept` authenticates the controller peer and the original recovery parent, and performs the P1 retained-tuple echo.
2. **Owner preflight:** `OwnerPreparationTransaction.Run` calls `QualifiedOwnerPreflight.Verify` when the Start request carries an installation identity. That returns `OwnerAuthorityContext.Qualified.FromVerified(LoadedInstallation)`, retaining the actual first verified identity, manifest, qualification id and raw record hash.
3. **Effects before manifest authority:** writer acquisition, allocation-intent proof, `WriterReady`, domain creation/registration, staging copy and staged-object description all occur before the final admission check. This is the existing order and is unchanged.
4. **Manifest authorization request:** the direct controller sends `ManifestAuthorization`; `OwnerPreparationTransaction` decodes it with `OwnerManifestAuthorizationCodec.TryDecode` and binds `ExecutionId`, `IssuerId` and `GenerationId` to the original Start/transaction identity.
5. **P2 gate:** with the manifest now known, `OwnerManifestAdmissionGate.TryRequire` independently re-enters the protected verifier through the retained `LoadedInstallation`, requires `IsFinalAdmissionEligible`, and requires the fresh protected S2 result to remain the first retained tuple.
6. **Grant point:** only after the gate passes may the owner build the Prepared description, write `Prepared`, publish `PreparedDescription`, mint the capability, bind the safety monitor and enter HELD.

The P2 gate therefore dominates the new qualified manifest authority, but it does **not** dominate the earlier journal/writer/domain/staging effects. Those effects remain unchanged and are explicitly identified as P4 sink-inventory obligations; P2 makes no complete pre-effect coverage claim.

The non-integrated B2 recovery-to-owner manifest path and `QualifiedB2Owner` are separate owners of installation authority and are not changed or claimed by P2. They remain in the P4 audit scope.

## 4. Production changes

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerManifestAdmissionGate.cs` | New refusal gate. One production entry obtains the protected grant from the retained installation; one shared decision requires eligibility and retained-tuple equality. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerAuthorityContext.cs` | `Qualified.RequireLoadedInstallation()` exposes the actual verified installation and fails closed when a context has no protected installation. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs` | Early qualified-context/loaded-installation refusal before journal/writer/allocation/staging; operation-bound P2 gate after manifest decode and before `Prepared`/mint/HELD. |
| `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs` | Binding predicate additionally requires an accepted selector, exact final acceptance record and Production build configuration. |
| `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs` | Debug-only `Grant.ForTest` for deterministic tests; excluded from Release. |
| `eng/VerifyProductionAssembly/Program.cs` | Requires the new gate type in the production candidate and rejects `Grant::ForTest` / `Qualified::ForTest` in Release. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerManifestAdmissionGateTests.cs` | Fourteen deterministic P2 decision, refusal, isolation and wiring tests. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs` | Minimal IL token scanner for the owner wiring/order assertions. |

No change was made to `ReleaseArbiter`, `HeldReleasePhase` behavior, `HeldExecution` transport, the journal schema, the C3 deadline, the release writer, recovery stop authority, reconciliation or public production activation.

## 5. Baseline-to-fix observations

P2 is an additive refusal gate; there was no prior failing runtime test to reproduce on the baseline because the baseline had no gate at all. Baseline observations below are source/IL inspection, not fabricated test failures.

| Probe / test | Baseline observation | Implementation change | Fixed observation |
|---|---|---|---|
| `DirectIntegratedStartManifestGateIsWiredBeforePreparedAndMint` | Baseline `b0a29ac` has no `OwnerManifestAdmissionGate` type and no call in `RunTransaction`. | Add the gate after manifest decode, before `Prepared`/mint; require the loaded installation before journal construction. | The test finds exactly one gate call, after the manifest decode and before `Publisher.Publish`; the loaded-installation requirement precedes `JournalFileStorage`. |
| `ManifestGateHasExactlyOneProductionCallSite` | Baseline has zero gate call sites. | Add exactly one owner manifest call site. | Assembly-wide IL scan reports only `OwnerPreparationTransaction.RunTransaction`. |
| `WithdrawnAcceptanceBeforeManifestGrantRefuses` | Baseline `AdmissionAuthority.BindingMatches` did not require `selector.Accepted` or exact record acceptance/build fields. | Require accepted selector, `final-native-accepted` and `Production` in the pure binding predicate. | A withdrawn selector fails parsing; a hand-shaped withdrawn grant is ineligible and the gate refuses. |
| Release production verifier | Baseline verifier did not check `Grant::ForTest` or `Qualified::ForTest`. | Add the two Debug-only authority-factory checks plus the gate core type. | Release verifier PASS with zero forbidden factories; running the same production mode against the Debug assembly reports both factories. |

## 6. Deterministic tests and exact results

Commands were run from `C:\Users\Laszlos\source\repos\Gagamba` on the host above.

| Check | Result |
|---|---|
| Debug build of M1 tests | **0 warnings, 0 errors**; instrumented verifier PASS (4/4 hook types, 3/3 hook properties, 13/13 core types) |
| `OwnerManifestAdmissionGateTests` | **14 passed / 0 skipped / 0 failed** |
| P1 `OwnerAuthorityContextTests` | **10 passed / 0 skipped / 0 failed** |
| Focused regression filter (`OwnerAuthorityContextTests`, `OwnerManifestAdmissionGateTests`, `FinalAcceptanceTests`, `ReleaseGenerationGateTests`, `InstallationWireTests`, `RecoveryB2Tests`, `DirectAuthenticationTests`, `ReleaseArbiterTests`) | **179 passed / 20 skipped / 0 failed / 199 total** |
| Full Windows M1 Debug runner | **1290 passed / 331 skipped / 0 failed / 1621 total** |
| Release `Gagamba.Execution.Linux.M1` build | **0 warnings, 0 errors**; production verifier PASS; 0/4 hook types, 0/3 hook properties, 13/13 core types |
| Release `Gagamba.Execution.Linux.M1.Owner` build | **0 warnings, 0 errors**; production verifier PASS |
| Release `Gagamba.Execution.Linux.M1.Recovery` build | **0 warnings, 0 errors**; production verifier PASS |
| Negative production-verifier control on the Debug assembly | FAIL as expected, including **`Grant::ForTest, Qualified::ForTest`** |

The reference full-run baseline was **1,276 passed / 331 skipped / 0 failed (1,607 total)**. P2 adds exactly 14 deterministic tests: **1,290 / 331 / 0 (1,621)**. The 331 native skips were not executed and are not passes.

### 6.1 Executed commands

```text
dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerManifestAdmissionGateTests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerAuthorityContextTests|FullyQualifiedName~OwnerManifestAdmissionGateTests|FullyQualifiedName~FinalAcceptanceTests|FullyQualifiedName~ReleaseGenerationGateTests|FullyQualifiedName~InstallationWireTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~ReleaseArbiterTests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build
dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj -c Release
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll"
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll"
```

The last command is the Debug negative control; it fails as expected and names `Grant::ForTest, Qualified::ForTest`.

### 6.1 Test-to-claim mapping

| Test | P2 claim |
|---|---|
| `ExactRetainedTupleWithProtectedGrantIsAccepted` | The gate is not an unconditional refusal; an exact protected-verification-shaped grant for the retained tuple passes the shared decision. |
| `RetainedQualifiedContextWithoutProtectedAdmissionRefuses` | Missing final admission refuses. |
| `ValidProcessAuthenticationAloneCannotGrantManifestAuthority` | A retained/authenticated context without the actual protected installation refuses; process authentication alone is insufficient. |
| `WrongInstallationIdentityRefuses` | Wrong installation identity refuses. |
| `WrongQualificationIdRefuses` | Wrong qualification id refuses. |
| `WrongQualificationRecordHashRefuses` | Wrong raw qualification-record hash refuses. |
| `UnavailableProtectedAuthorityRefuses` | Unavailable/malformed protected authority refuses; malformed selector bytes refuse. |
| `WithdrawnAcceptanceBeforeManifestGrantRefuses` | Withdrawn acceptance cannot yield a grant or an eligible binding. |
| `LaterEchoCannotSubstituteRetainedAuthority` | A later echo cannot replace the first tuple, and a grant for the replacement fails against the retained selection. |
| `DirectIntegratedStartManifestGateIsWiredBeforePreparedAndMint` | Direct integrated Start cannot reach `Prepared`/mint without the gate; the gate follows manifest decode and precedes publication. |
| `ManifestGateHasExactlyOneProductionCallSite` | The gate has exactly one production call site, in `RunTransaction`; removing it fails the test. |
| `ValidationContextRemainsNonConstructibleAndCannotSatisfyTheGate` | Validation remains unreachable and cannot satisfy or bypass the gate; no Boolean selector exists. |
| `LegacyContextCannotSatisfyInstalledManifestAdmission` | Legacy remains a separate accepted path and cannot satisfy installed admission. |
| `StoppingReconciliationAndRecoveryDoNotRequireRenewedManifestAdmission` | Stopping, release arbitration, reconciliation and recovery do not consume the manifest gate; stopped/admitted operations do not need renewed admission. |

## 7. T01-T23 traceability

The governing grouping is the implementation handoff §5.

| Cases | P2 disposition |
|---|---|
| T01-T05, T21 | **P2 deterministic owner manifest refusal implemented and tested** for the integrated direct owner path. Protected-store/native positive and negative rows remain pending. |
| T06-T09 | Existing actor/descriptor/session controls are unchanged; focused deterministic suites pass and native cases remain skipped pending native qualification. P2 adds no alternate actor path. |
| T10-T12, T22 | **P3 pending.** Controller Q10, the owner release arbiter, sole writer and post-scan residual are unchanged. P2 adds no release authority. |
| T13-T15 | Existing arbiter/stop/deadline behavior is preserved; no new writer, deadline or admission read. P3 adds the owner pre-write generation scan. |
| T16-T18, T20 | **Slice E pending.** Validation remains non-constructible; no validation issuance or positive path exists. |
| T19 | Legacy context remains separate and cannot satisfy installed admission; native legacy positives were not rerun. |
| T23 | **P4/E pending.** Administrative lock/marker ordering and the non-integrated B2 `/ QualifiedB2Owner` sinks are untouched and explicitly outside P2 coverage. |

## 8. Lifecycle and cleanup

- No test created a process, cgroup, domain, staging directory, journal or protected file. The deterministic tests operate on in-memory manifests/bindings and IL metadata.
- No production writer, RELEASE attempt or target execution was created.
- Cleanup status: **not applicable / no test-owned OS resources**. No cleanup claim is converted from an observation that did not occur.
- Original actor/session/execution/generation bindings, the C3 deadline, Policy A stop, C1/C2/C4 and the owner-only writer are unchanged.
- Already admitted operations retain stopping/reaping/reconciliation without renewed final admission; the gate is not on those paths.
- Qualified-to-legacy downgrade, a new RELEASE writer, renewed C3, production activation and uncertain-to-confirmed cleanup are not introduced.

## 9. Preservation

- Canonical `main`, one worktree at `C:\Users\Laszlos\source\repos\Gagamba`.
- The 44 historical untracked paths were present before the implementation commit and remain unchanged. P2 adds three tracked source/test paths and this evidence/handoff pair.
- No reset, rebase, stash, clean, prune, branch, worktree, force operation or push occurred.
- Prior owner decisions, evidence, reviews and reviewer bytes are unchanged.

## 10. Source and binary identity

| File | SHA-256 |
|---|---|
| `OwnerManifestAdmissionGate.cs` | `b48bafc4ee47cbfe80df17a27f2519ba08239cec1b2168af9ebde07d92abc652` |
| `OwnerAuthorityContext.cs` | `669eb111be68e22d93246b758e3d50f0c4ab3dfeac0f8f4154745d2caf03266a` |
| `OwnerPreparationTransaction.cs` | `b119792038aeef003a947b8f2f56ed75b88ce03446be2a70f253185f1d51ead8` |
| `AdmissionAuthority.cs` | `52b80e8abd6a6699fcb8bc4678c9bcd8423cdd9f2449802a81fce0e56eeff355` |
| `FinalAdmissionVerifier.cs` | `e07455f3363318247c09cbd1b34b4e7f680f8d9ebf53dc5f2293d2ebd7f5a98b` |
| `eng/VerifyProductionAssembly/Program.cs` | `00635d750699ebbd0ad27e48670decd39a15f4d7e9a01e0495c54a26281ff088` |
| `OwnerManifestAdmissionGateTests.cs` | `daebe0298b7a530df78c28b4ff30950bf1204b42800dcde39fe626a4e7076024` |
| `IlCallScanner.cs` | `1ae758677fb0c9b94ab8928e35fb2997bc6f7d823e3631229363d85cdfac223a` |

Release M1 DLL SHA-256: `c8dfa6cf75496c306951e5c9ea27d910537aecdaa0257969f07262e5c379c538` (MVID `b8aa138f-7442-4932-82b6-91e9009414c4`).

Debug M1 DLL SHA-256: `5226b9ea605272a1d51fb0e6f161139ab983279432f833432bb2e9c0053f5749`.

Debug test DLL SHA-256: `2ff35db82d983fadaa6731cb1a366f2a17f6365eca2702c8d1b7e7047281154b`.

## 11. Findings and qualifications

- **P2-F01 (scope):** P2 gates the integrated direct owner manifest authority only. The non-integrated B2 recovery-to-owner manifest path and `QualifiedB2Owner` remain P4 sink-inventory obligations. Not a P2 defect; explicit non-claim.
- **P2-F02 (ordering):** journal, writer, allocation/domain and staging effects precede the final-admission gate. P2 does not move them or claim complete pre-effect coverage. P4 must inventory them.
- **P2-F03 (native):** protected final-admission positive/negative reads, stable protected bytes and an actual installed owner remain pending. The 331 skips are not passes.
- **P2-F04 (validation):** validation remains non-constructible; no positive validation exception or Slice E behavior is added.
- **P1 observations unchanged:** the P1 later echo remains after writer acquisition, allocation-intent proof and `WriterReady`; P2 does not move it or restate its ordering.

## 12. Qualification boundary

This evidence establishes deterministic owner-wiring refusal and build-identity properties on the documented Windows host only. It does **not** establish:

- a protected final-admission positive or protected-store stability on a real root-owned installation;
- native Linux/owner/recovery/controller qualification, generation races or descriptor/custody behavior;
- P3 owner pre-RELEASE generation verification;
- P4 complete pre-effect sink coverage or non-integrated B2/`QualifiedB2Owner` gating;
- Slice E validation authorization, validation-session capability, GAVA1 or v3 prepublication parsing;
- production activation, publication, packaging or public factory enablement.

## 13. Independent review questions

1. Is every P2-protected owner manifest grant dominated by the new final-admission gate?
2. Does the gate independently obtain trusted evidence?
3. Can a direct Start caller bypass authorization despite successful process authentication?
4. Can caller-provided identity or a later echo replace the retained P1 tuple?
5. Does admission withdrawal refuse new grants without disabling stopping?
6. Are legacy and validation boundaries preserved?
7. Are effects that still precede the gate accurately identified as P4 obligations?
8. Are positive native cases still pending rather than falsely qualified?

## 14. Final disposition

**P2 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

P2-P4 beyond this slice, Slice E, Slice N, protected provisioning, native qualification and production activation remain unimplemented and unauthorized. The 331 native skips remain unqualified.
