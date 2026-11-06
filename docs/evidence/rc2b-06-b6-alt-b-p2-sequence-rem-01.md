# Alternative B P2 sequencing remediation (early / late final admission) evidence

**P2 SEQUENCING REMEDIATED — READY FOR INDEPENDENT Q2** (bounded early/late admission sequencing only)

Date: 2026-11-06 (Asia/Manila)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-P2-SEQUENCE-REM-01`

Governed by: [P2 owner sequencing clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md) (additive to the [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md))

- **Baseline commit:** `ec9758a69c8d1946b76d7d5e8e5578102c41bd63`, tree `bff333b79f270d2adf55021f81a3a9d8a2ac6837`.
- **Clarification commit:** `67182bf` (docs/decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md).
- **Implementation commit:** `7cc5d65a62de5338a0b3c1460af793140fb8db43`, tree `a31d33aabe14bcf58693b874c5dab635c12fb7d3`.
- **Branch:** canonical `main`, one worktree at `C:\Users\Laszlos\source\repos\Gagamba`. No push, reset, rebase, clean, prune, branch or worktree operation.
- **Host OS/version:** Microsoft Windows 11 Home Single Language 10.0.26200, build 26200.
- **Architecture:** AMD64.
- **Runtime/SDK:** .NET SDK `10.0.401`; `Microsoft.NETCore.App 10.0.12`.
- **Relevant native prerequisites:** none for the deterministic tests. No protected Linux root store, protected installation, native host or delegated cgroup was provisioned or exercised.
- **Qualification scope:** deterministic owner-side decision, refusal, wiring and effect-boundary ordering checks on Windows; Debug and Release build identity; production Release exclusion of Debug-only authority factories.
- **Explicit exclusions:** no protected final-admission positive; no native Linux qualification; no P3, P4, Slice E, Slice N, production activation, publication or push; the non-integrated B2/`QualifiedB2Owner` route remains outside this remediation.

## 1. Disposition

**P2 SEQUENCING REMEDIATED — READY FOR INDEPENDENT Q2.**

The qualified integrated owner path now performs the shared protected final-admission decision at **two independent boundaries**: an early check after the trusted first installation verification and before the first protected initialization effect, and the retained operation-bound late recheck before manifest authorization reaches `Prepared`, capability mint or HELD. The early check refuses without journal, writer, domain, allocation or staging effects when evidence is missing, unavailable, malformed, withdrawn, stale or mismatched. The late check is unchanged in purpose and is not replaced by the early one.

P2 still does **not** claim complete admission-sink coverage (P4/Q04), validation authorization, native protected-store qualification, production activation or bootstrap completeness. The 331 skipped native cases remain unqualified. The non-integrated B2/`QualifiedB2Owner` route remains a P4 obligation.

## 2. Owner sequencing clarification

Recorded additively in [the clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md): qualified integrated execution requires valid final admission before its first protected initialization effect, and a fresh final-admission revalidation before its operation-bound manifest grant. The early check protects initiation; the late check protects manifest authorization after acceptance withdrawal or generation replacement. A late refusal may occur after earlier effects that were initiated under valid admission; those are handled by the existing stopping and cleanup mechanisms. The clarification is not a waiver of pre-effect authorization and does not rewrite the original owner decision.

## 3. Before-fix observation (source/IL/text-scan, not a fabricated runtime failure)

At the baseline `ec9758a` there was no early gate at all. The baseline had exactly **one** `OwnerManifestAdmissionGate.TryRequire` call and it ran after staging. The independent P2-Q1 review recorded this as finding F1 (`findings.md` SHA-256 `306011dc827668864dcfebd069c005dfbb4d044e214816ae55a5db23914d20b7`) using an independently authored text-order probe. The producer reproduces the same baseline ordering markers:

```text
84: new JournalFileStorage()
102: TryAcquireWriter(
196: TryCreateDomain(
360: SafeCopyIn.Prepare(
379: OwnerManifestAuthorizationCodec.TryDecode(
409: OwnerManifestAdmissionGate.TryRequire(     <- baseline: only gate, after staging
421: new OwnerPreparedDescription(
463: PreparationPublisher.Publish(
493: PrepareHeldAsync(
PASS: ordered markers and one transaction call; text scan is not a control-flow proof
```

This is additive refusal-gate evidence: there was no prior failing runtime test to reproduce, because the baseline lacked the early gate entirely. It is source ordering, not a runtime failure.

## 4. Exact source remediation

### 4.1 Changes

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs` | Added the early qualified final-admission check (return `48`) after `RequireLoadedInstallation()` and before `new JournalFileStorage()`; clarified the late check as an independent operation-bound recheck. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerManifestAdmissionGate.cs` | Documentation only: the same single decision now serves both boundaries; no behavioral change. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerManifestAdmissionGateTests.cs` | Updated the wiring test to two boundaries; added an instrumented effect-boundary ordering test and four early/late decision tests; strengthened the anti-bypass test; renamed it to reflect two call sites in one owning method. |

No change was made to `AdmissionAuthority`, `FinalAdmissionVerifier`, `ReleaseGenerationGate`, `LoadedInstallation`, `OwnerAuthorityContext`, `HeldReleasePhase`, `ReleaseArbiter`, `HeldExecution`, the journal schema, the C3 deadline, the release writer, recovery stop authority, reconciliation, `eng/VerifyProductionAssembly/Program.cs` or public production activation. No new grant, factory, Boolean, wire echo, validation path or temporary bypass was added.

### 4.2 Early check (exact behavior)

The early check reuses `OwnerManifestAdmissionGate.TryRequire`, which calls the retained installation's protected `LoadedInstallation.VerifyFinalAdmission()` and then the shared `AdmissionAuthority.IsFinalAdmissionEligible` / `ReleaseGenerationGate.RequireStillQualified` decision. It validates against the original retained installation identity, qualification ID and record hash. On refusal it sends the owner abort and returns before `JournalFileStorage`, `OwnershipJournal`, `TryAcquireWriter`, `ScopedAllocationContext.TryCreate`, `TryCreateDomain` or `SafeCopyIn.Prepare`. It accepts only the sealed `OwnerAuthorityContext.Qualified` and the private `FinalAdmissionVerifier.Grant`; it does not consult the controller's `ManifestAuthorization`, a caller Boolean or a wire echo.

### 4.3 Fixed source ordering (probe output)

```text
71: RequireLoadedInstallation()
92: OwnerManifestAdmissionGate.TryRequire(     <- early, before any effect
104: new JournalFileStorage()
121: new OwnershipJournal(
122: TryAcquireWriter(
216: TryCreateDomain(
380: SafeCopyIn.Prepare(
399: OwnerManifestAuthorizationCodec.TryDecode(
443: new OwnerPreparedDescription(
485: PreparationPublisher.Publish(
515: PrepareHeldAsync(
PASS: ordered markers and 2 transaction gate calls; text scan is not a control-flow proof
```

(The second `OwnerManifestAdmissionGate.TryRequire` occurrence is the late check after manifest decode; the marker list names the call once. The exact line anchors are also machine-checked by the IL effect-boundary test in §6.)

## 5. Corrected source-backed effect-order table (qualified integrated route)

Line anchors are at implementation commit `7cc5d65`, tree `a31d33a`. "P2 corrected" is the integrated direct route in `OwnerPreparationTransaction.RunTransaction`; "Remaining" distinguishes P4 and other open obligations.

| # | Transition / effect | Source anchor (`OwnerPreparationTransaction.cs`) | Class | P2 corrected | Remaining |
|---|---|---|---|---|---|
| 1 | First verified selection | `OwnerPreparationTransaction.Run` → `QualifiedOwnerPreflight.Verify` returns `Qualified.FromVerified` | Retention | Unchanged | — |
| 2 | Retained authority requirement | `RunTransaction` `RequireLoadedInstallation()` L71 | Refusal (no effect) | Unchanged | — |
| 3 | **Early final-admission check** | `RunTransaction` `OwnerManifestAdmissionGate.TryRequire` L92 (return 48) | Refusal (no effect) | **Added** | — |
| 4 | Journal storage construction | `new JournalFileStorage()` L104 | Protected initialization | Preceded by early check | P4 inventory of any other sinks |
| 5 | Ownership journal construction | `new OwnershipJournal(...)` L121 | Protected initialization | Preceded by early check | P4 |
| 6 | Exclusive writer acquisition / publication | `TryAcquireWriter` L122; allocation-intent proof L139; `WriterReady` L145 | Protected initialization | Preceded by early check | P4 |
| 7 | Direct authentication + retained-tuple echo | `OwnerDirectConnection.Accept` L151 | Authentication | After writer/`WriterReady` (P1 ordering) | — |
| 8 | Allocation context / domain / registered state | `ScopedAllocationContext.TryCreate` L207; `CgroupInterop.TryCreateDomain` L216 | Protected initialization | Preceded by early check | P4 |
| 9 | Staging directory/files and `StagedObjects` | `SafeCopyIn.Prepare` L380; `StagedObjects` L397 | Protected initialization | Preceded by early check | P4 |
| 10 | Manifest authorization decode/binding | `OwnerManifestAuthorizationCodec.TryDecode` L399; identity binding L406 | Manifest authority input | After effects | — |
| 11 | **Late operation-bound final-admission recheck** | `OwnerManifestAdmissionGate.TryRequire` L431 (return 47) | Refusal before manifest grant | Retained, independent | — |
| 12 | Prepared + publisher/capability mint + HELD | `new OwnerPreparedDescription` L443; `WriteNext(...,Prepared)` L453; `PreparationPublisher.Publish` L485; HELD L501/L531 | Manifest authority / RELEASE prerequisite | Reached only after late check | P3 pre-RELEASE generation gate |

The non-integrated B2 recovery-to-owner path and `QualifiedB2Owner` are separate owners of installation authority and are **not** covered by this remediation; they remain P4 audit scope.

## 6. Tests and raw results

Commands run from `C:\Users\Laszlos\source\repos\Gagamba` on the host above.

| Check | Result |
|---|---|
| Debug build of M1 tests | **0 warnings, 0 errors**; instrumented verifier PASS (4/4 hook types, 3/3 hook properties, 13/13 core types) |
| `OwnerAuthorityContextTests` + `OwnerManifestAdmissionGateTests` | **29 passed / 0 skipped / 0 failed** |
| Focused regression filter (P1/P2 + FinalAcceptance + ReleaseGenerationGate + InstallationWire + RecoveryB2 + DirectAuthentication + ReleaseArbiter) | **185 passed / 20 skipped / 0 failed / 205 total** |
| Full Windows M1 Debug runner | **1295 passed / 331 skipped / 0 failed / 1626 total** |
| Release `Gagamba.Execution.Linux.M1` build | **0 warnings, 0 errors**; production verifier PASS; 0/4 hook types, 0/3 hook properties, 13/13 core types |
| Release `Gagamba.Execution.Linux.M1.Owner` build | **0 warnings, 0 errors**; production verifier PASS |
| Release `Gagamba.Execution.Linux.M1.Recovery` build | **0 warnings, 0 errors**; production verifier PASS |
| Negative production-verifier control on the Debug assembly | FAIL as expected, including **`Grant::ForTest, Qualified::ForTest`** |

Reference baseline full run: **1290 passed / 331 skipped / 0 failed (1621 total)**. This remediation adds exactly 5 deterministic tests: **1295 / 331 / 0 (1626)**. The `OwnerManifestAdmissionGateTests` class grew from 14 to 19 and the P1+P2 two-class subset from 24 to 29 (the prior producer focused filter reported 179 passed / 20 skipped / 199 total; P2-Q1 documented a one-test direct-vstest selection difference). The 331 native skips were not executed and are **not** passes.

### 6.1 Executed commands

```text
dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerAuthorityContextTests|FullyQualifiedName~OwnerManifestAdmissionGateTests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerAuthorityContextTests|FullyQualifiedName~OwnerManifestAdmissionGateTests|FullyQualifiedName~FinalAcceptanceTests|FullyQualifiedName~ReleaseGenerationGateTests|FullyQualifiedName~InstallationWireTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~ReleaseArbiterTests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build
dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj -c Release
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll"
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll"
```

The last command is the Debug negative control; it fails as expected and names `Grant::ForTest, Qualified::ForTest`.

### 6.2 Test-to-claim mapping (required focused tests)

| Required focus | Test(s) |
|---|---|
| Missing final admission before initialization: refuse, no protected effects | `RetainedQualifiedContextWithoutProtectedAdmissionRefuses`; `InitialFinalAdmissionRefusesWithoutAProtectedInstallation`; `InitialFinalAdmissionPrecedesEveryProtectedInitializationEffect` |
| Invalid final admission before initialization: refuse, no protected effects | `UnavailableProtectedAuthorityRefuses`; `WithdrawnAcceptanceBeforeManifestGrantRefuses`; `InitialFinalAdmissionPrecedesEveryProtectedInitializationEffect` |
| Correct controller authentication without admission: refuse | `ValidProcessAuthenticationAloneCannotGrantManifestAuthority`; `InitialFinalAdmissionRefusesWithoutAProtectedInstallation` |
| Wrong retained installation identity: refuse | `WrongInstallationIdentityRefuses`; `WrongQualificationIdRefuses`; `WrongQualificationRecordHashRefuses` |
| Initial admission valid, then withdrawn: late refusal, no manifest grant | `EarlyAdmissionAcceptedThenWithdrawalRefusesAtTheOperationBoundRecheck`; `WithdrawnAcceptanceBeforeManifestGrantRefuses` |
| Initial generation valid, then replaced: late refusal, no manifest grant | `EarlyGenerationAcceptedThenReplacementRefusesAtTheOperationBoundRecheck` |
| Early check passes but late check fails: no manifest grant / no new RELEASE authority | `EarlyAdmissionAcceptedThenChangedQualificationIdRefusesAtTheOperationBoundRecheck`; late-gate-before-publish ordering in `DirectIntegratedStartManifestGateIsWiredBeforePreparedAndMint` |
| Later echo cannot replace original identity | `LaterEchoCannotSubstituteRetainedAuthority`; P1 `OwnerAuthorityContextTests` |
| Existing legacy path unchanged | `LegacyContextCannotSatisfyInstalledManifestAdmission` |
| Existing stopping/reconciliation unaffected | `StoppingReconciliationAndRecoveryDoNotRequireRenewedManifestAdmission` |
| No production validation-context construction | `ValidationContextRemainsNonConstructibleAndCannotSatisfyTheGate` |
| All earlier P2 admission tests continue to pass | the 14 original P2 tests remain; one wiring test was updated to the two-boundary contract and the anti-bypass test was renamed/strengthened |

"Absence of the protected effects" is established structurally by `InitialFinalAdmissionPrecedesEveryProtectedInitializationEffect`, which instrument-reads the production IL and asserts the early gate call offset precedes every protected initialization call (`JournalFileStorage`, `OwnershipJournal`, `TryAcquireWriter`, `ScopedAllocationContext.TryCreate`, `CgroupInterop.TryCreateDomain`, `SafeCopyIn.Prepare`). It does not rely on the returned refusal value. A negative control exists by construction: removing the early call would make the two-call assertion and all effect-ordering assertions fail.

### 6.3 Source and binary identity

| File | SHA-256 |
|---|---|
| `Lifecycle/OwnerPreparationTransaction.cs` | `779975cea145f2fd31eb11a66e7e3abdb0811de401e69751ac35d4675b63d27d` |
| `Lifecycle/OwnerManifestAdmissionGate.cs` | `4199c535c863ded323f0527698ff142578c925cfda0097b90d1158841a29464d` |
| `Lifecycle/OwnerManifestAdmissionGateTests.cs` | `6603be9edd80b762e38aab1bb0b6dea0b6af6a8deca7b281394658d1adb675e7` |

Release M1 DLL SHA-256: `9de94fb9b92c9fa75b45e0582cf2001431e8e24e44deae179a71db42241687e6` (MVID `f821bd00-61f4-418b-b1fb-90e6940e412a`).

Debug M1 DLL SHA-256: `d3874ee3912bebb96acb23a9068aa1a0886dc626f26a39e8abacbbcd3be4a3f9`.

Debug test DLL SHA-256: `707ca978330772c89475030cd313b315493fa9536c6c273f32a9d17478164bf8`.

## 7. Remaining limitations

- **P2-Q2-F01 (scope):** this remediation covers the integrated direct owner route only. The non-integrated B2 recovery-to-owner manifest path and `QualifiedB2Owner` remain P4 sink-inventory obligations.
- **P2-Q2-F02 (native):** protected final-admission positive/negative reads, stable protected bytes and an actual installed owner remain pending. The 331 skips are not passes.
- **P2-Q2-F03 (cleanup):** the "early pass, late fail" safe-cleanup path is source-supported and the late gate precedes any manifest grant, but an actual partial-initialization withdrawal-to-reaping chain was not executed in a native environment. Source inspection is not a cleanup result.
- **P2-Q2-F04 (validation):** validation remains non-constructible; no positive validation exception, alternative issuer or Slice E behavior is added.
- **P1 observations unchanged:** the later direct echo remains after writer acquisition, allocation-intent proof and `WriterReady`; this remediation does not move it.
- **Independent binaries:** the deterministic tests were built and run in place; no independent rebuild is claimed.

## 8. Independent P2-Q2 handoff

- [P2-Q2 handoff](../reviews/rc2b-06-b6-alt-b-p2-q2-handoff.md) lists the exact review scope, commands and questions.
- The independent review must confirm: the early check is genuinely before every protected initialization effect; the late check is genuinely independent and fresh; both use the retained tuple and the private protected grant only; legacy, validation, stopping/reconciliation and sole-writer behavior are unchanged; and no P4/P3/Slice E/native/production claim is made.

## 9. Preservation

- Canonical `main`, one worktree at `C:\Users\Laszlos\source\repos\Gagamba`.
- The 44 historical untracked paths remain unchanged. `git status --porcelain -uall` reported 44 untracked paths before and after (44 + the 3 tracked files during the working change).
- No reset, rebase, stash, clean, prune, branch, worktree, force operation or push occurred.
- Prior owner decisions, evidence, reviews and reviewer bytes are unchanged.

## 10. Final disposition

**P2 SEQUENCING REMEDIATED — READY FOR INDEPENDENT Q2.**

P3, P4, Slice E, Slice N, protected provisioning, native qualification and production activation remain unimplemented and unauthorized. The 331 native skips remain unqualified. No push.
