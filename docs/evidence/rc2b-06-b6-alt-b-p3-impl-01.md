# Alternative B P3 - owner pre-RELEASE generation gate evidence

**P3 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW**

Date: 2026-11-06 (Asia/Manila)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-P3-IMPL-01`

Governing owner decision: [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md)

Accepted implementation handoff: [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md)

P1 owner acceptance: [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md)

- **Slice:** Alternative B P3 - owner pre-RELEASE active-generation gate, refusal-only.
- **Baseline source SHA:** `b9459d346ec26827ab807379376974f8fe1d21dd` (tree `808f4bc917d60640e202a5e441e970dd785cb6a4`).
- **Implementation source SHA:** `b7a274f517dc24f4aa5e9e34338bc4c2769f5b9c` (tree `8db23e2efdd74aad3909513d2d2f3ed0828d1d25`). This evidence document and its handoff are additive and change no production source.
- **Branch:** canonical `main`. No push, reset, rebase, clean, prune, branch or worktree operation.
- **Host OS/version:** Microsoft Windows 11 Home Single Language 10.0.26200, build 26200.
- **Architecture:** AMD64 (Intel Core Ultra 5 125H).
- **Runtime/SDK:** .NET SDK `10.0.401`; `Microsoft.NETCore.App 10.0.12`.
- **Relevant native prerequisites:** none for the deterministic tests. No protected Linux root store, protected installation, native host or delegated cgroup was provisioned or exercised.
- **Qualification scope:** deterministic owner-side pre-RELEASE decision, refusal, stop-tail, wiring and effect-ordering checks on Windows through the real release arbiter; Debug and Release build identity; production Release exclusion of the Debug-only observation entry.
- **Explicit exclusions:** no protected fixed-root positive observation; no native Linux owner/controller qualification or generation race; no validation authority or positive validation path; no P4 sink audit; no Slice E, Slice N, production activation, publication or push.

## 1. Disposition

**P3 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

P3 adds an owner-side pre-RELEASE active-generation verification for a retained qualified
operation. After durable release intent and the existing outside-lock after-intent barrier,
the owner re-enters the single shared Q10 decision and requires its retained selection to
still be the active qualified installation (full identity plus qualification id and raw
record hash). The refusal is the last final-gate entry check before capability consumption,
keeps the conservative post-intent result, closes the capability exactly once, never reaches
the durable commit block, and submits the existing stop path as a best-effort durable tail
outside the gates. Every refusal produces zero physical attempts.

P3 does **not** claim a protected fixed-root positive, native generation-race detection,
validation authorization, P4 sink coverage, production activation or bootstrap completeness.
The 331 skipped native cases remain unqualified.

## 2. Required P3 behavior and implementation

The accepted owner-decision section 3.2 order for a new physical attempt maps to the code as
follows.

| Accepted order | Implementation |
|---|---|
| 1. Authenticate the original request and exact capability identity; preserve correlation, deadline and prerequisite checks | `HeldReleasePhase.HandleReleaseRequest` decode/identity checks and `ReleaseArbiter.RequestRelease`'s pre-intent correlation/deadline/prerequisite/binding checks are unchanged. |
| 2. Preserve retained-decision, intent and ambiguity handling; a retained duplicate is observation only | The top `_gate` retained check and the final-gate retained/stop/deadline/ambiguity handling are unchanged; a duplicate returns C4 without verification, consumption or a second attempt. |
| 3. After durable intent and the outside-lock after-intent barrier, verify the fresh owner generation outside the runtime gate, journal gate, safety latches and capability locks | `ReleaseArbiter.RequestRelease` calls `InvokeOwnerGenerationGate()` immediately after `Barrier("after-intent")` and before the final `lock (_gate)`; the call holds no lock. |
| 4. Immediately enter the final runtime gate and recheck retained result, stop, deadlines, prerequisites and capability liveness | The final `lock (_gate)` retains every existing check unchanged; the P3 refusal is the last check before `Barrier("before-consume")`. |
| 5. Consume the locally verified result once, close the capability once, invoke the one writer and persist through the existing path | On success the existing consume/encode/one-`TryWriteOnce` path runs; on refusal `ConsumeCapability()` runs once, the refusal is retained and the durable commit block is not reached. |

| Requirement | Implementation |
|---|---|
| Full identity + qualification id + raw record hash; no null skip for a qualified operation | `OwnerReleaseGenerationGate.Verify` requires `OwnerAuthorityContext.Qualified`, obtains the actual `RequireLoadedInstallation()` and calls the shared `ReleaseGenerationGate.RequireBoundGenerationActive`, which re-reads via the retained `LoadedInstallation.Revalidate()`. |
| No second trust root | The verification reuses `ReleaseGenerationGate` (the same decision shared with admission-time `Revalidate`). P3 adds no alternate issuer, store reader or predicate. |
| Verification is not a caller callback or Boolean | The arbiter stores an optional readonly `Func<OwnerGenerationGateRefusal?>` wired only by `HeldReleasePhase` as a method group for a retained qualified context; there is no settable property, Boolean or wire echo. |
| Refusal prevents a new physical attempt without a retry | Every refusal returns `NotAttempted`; the tests count zero `TryWriteOnce` calls. |
| Refusal preserves the conservative post-intent result | The refusal is `Refused` / `MayHaveExecuted` / `NotAttempted` / `DurableCommitted=false` / `LiveExecutionExcluded=true` / `Binding.Accepted`, reason `release gate refused: owner pre-RELEASE generation verification failed ({error})`. |
| Refusal never reaches the durable commit block | The refusal path returns before `TryPlanReleaseCommitted`; the durable intent record remains `ReleaseTag.Intent`. |
| Refusal submits the existing stop path outside the gates | `SubmitOwnerGateRefusalStopTail` calls `RequestStop(StopCauseKind.SetupFault)` after the gate is released; `HeldReleasePhase.LatchOwnerGateStopTail` latches `OwnerSafetyMonitor.AcceptStop(SetupFault)` so the owner is safe without a new controller request. |
| No capability restore, adoption, deadline renewal, new writer or retry | Tests assert the capability is dead, the retained tuple is unchanged, and a successful verification cannot override a deadline. |
| Preserve the controller Q10 check and the sole writer | `Bootstrap/IntegratedB3Client.cs` and the single `IReleaseWriteChannel.TryWriteOnce` call are unchanged. |
| Legacy unchanged | `Verify(OwnerAuthorityContext.Legacy)` returns null (no installed-generation model); the legacy arbiter path keeps its prior behavior. |
| Validation refused fail-closed and still non-constructible | Any non-legacy, non-qualified context (including a reflection-constructed `Validation`) refuses as `InstallationUnavailable`; `Validation` still has no factory. |
| Debug-only observation excluded from Release | `OwnerReleaseGenerationGate.VerifyObserved` is inside `#if !GAGAMBA_PRODUCTION` and is added to the production verifier's forbidden factory list. |

## 3. Traced owner release authority chain and P3 placement

1. **Owner preflight retention:** `QualifiedOwnerPreflight.Verify` returns `OwnerAuthorityContext.Qualified.FromVerified(LoadedInstallation)`, retaining the first verified full identity, manifest, qualification id and raw record hash (P1).
2. **HELD phase:** `HeldReleasePhase` is constructed with that retained `_authority`. When it is `Qualified`, the phase wires `VerifyOwnerReleaseGeneration` into the arbiter; any other non-legacy context is refused in the constructor by construction.
3. **RELEASE receiver:** `HeldReleasePhase.HandleReleaseRequest` authenticates the exact capability identity and calls `ReleaseArbiter.RequestRelease`.
4. **Durable intent:** `ReleaseArbiter.RequestRelease` writes `ReleaseTag.Intent` under `_journalGate`, then raises `Barrier("after-intent")`.
5. **P3 verification:** immediately after that barrier and **outside every lock**, `InvokeOwnerGenerationGate()` runs `OwnerReleaseGenerationGate.Verify(_authority)` → the shared Q10 decision. Any fault is wrapped fail-closed as `InstallationUnavailable`.
6. **Final gate:** the arbiter enters the existing `lock (_gate)` and rechecks retained result, stop, deadline, prerequisites and capability liveness unchanged. The P3 refusal is the last check before `Barrier("before-consume")`; on refusal it retains the conservative decision and returns without consumption of the writer.
7. **Stop tail:** after the lock, `SubmitOwnerGateRefusalStopTail` runs `RequestStop(SetupFault)` (durable under `_journalGate`), which the existing `OwnerSafetyMonitor`/arbiter stop path also drives natively; `HeldReleasePhase` latches the monitor from the decision.
8. **Success path:** unchanged — `TryConsume`, `EncodeRelease`, exactly one `TryWriteOnce`, delivery mapping and `WriteMutation` `/` `TryPlanReleaseCommitted` persistence.

There is exactly one production `ReleaseArbiter` construction site (`HeldReleasePhase` line 85); the audit found no other writer or construction path.

## 4. Production changes

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/OwnerGenerationGateRefusal.cs` | New: typed refusal payload (`Reason`, `Error`) produced only by the owner verification. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerReleaseGenerationGate.cs` | New: production `Verify(OwnerAuthorityContext)` entry reusing the shared `ReleaseGenerationGate`; `Legacy` verifies nothing; every other context refuses fail-closed. Debug-only `VerifyObserved` binds the same decision to synthetic observations. |
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | Optional readonly construction dependency; outside-lock verification after intent; refusal as the last final-gate entry check; refusal retained without reaching the durable commit block; stop tail outside the gates with limitation recording. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs` | Constructor guard (qualified or legacy only); conditional method-group wiring; `LatchOwnerGateStopTail` on the recognized refusal in both production branches. |
| `eng/VerifyProductionAssembly/Program.cs` | Adds `OwnerReleaseGenerationGate::VerifyObserved` to the forbidden Debug-only authority factories. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerReleaseGenerationGateTests.cs` | New: 11 deterministic owner verification decision/refusal/isolation tests. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/OwnerReleaseGenerationGateArbiterTests.cs` | New: 16 deterministic arbiter, stop/deadline concurrency, wire and IL wiring tests. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs` | Scanner target widened from `MethodBase` to `MemberInfo` so field tokens (`_ownerGenerationGate`) can be pinned. |

No change was made to `IntegratedB3Client`, `Launch/HeldExecution.cs` transport, the journal schema, the C3 deadline, the sole writer, recovery stop authority, reconciliation or public production activation. `IlCallScanner` is test-only.

## 5. Baseline-to-implementation observations

P3 is an additive refusal gate; the baseline had no owner pre-RELEASE generation verification at all, so there was no prior failing runtime test to reproduce. Baseline observations below are source/IL inspection of the baseline SHA, not fabricated failures.

| Probe / test | Baseline observation | Implementation change | Fixed observation |
|---|---|---|---|
| `RequestReleaseVerifiesAfterIntentAndBeforeConsumptionAndWrite` | Baseline `RequestRelease` has no verification call between the intent write and consumption. | Add `InvokeOwnerGenerationGate()` after `Barrier("after-intent")`. | IL offsets: `WriteTransition` (intent) < verification < `PreparedTransaction.TryConsume` < `IReleaseWriteChannel.TryWriteOnce`. |
| `QualifiedVerifiedObservationStillReleasesExactlyOnce` | Baseline would release without any owner generation read. | Add the verification dependency for qualified contexts. | A verified observation still releases exactly once; the counting transport records exactly one attempt. |
| `UnqualifiedRefusalHasZeroPhysicalAttemptsAndPostIntentShape` | Baseline had no refusal shape for this case. | Retain the conservative post-intent refusal. | `Refused`/`MayHaveExecuted`/`NotAttempted`/`false`/`true`/`Accepted`; journal `ReleaseTag.Intent`; capability dead; attempts = 0. |
| `ArbiterStoresVerificationAsAReadonlyConstructionDependency` | Baseline constructor had six parameters and no verification field. | Add an optional trailing seventh parameter stored in a readonly field. | Field is `IsInitOnly`; parameter 7 is optional with a null default; exactly one `stfld`. |
| `SharedVerificationIsReferencedOnlyFromTheOwnerPhase` | Baseline had no `Verify` to reference. | Add exactly one owner call site. | Assembly-wide IL scan reports only `HeldReleasePhase.VerifyOwnerReleaseGeneration`. |
| Production Release verifier | Baseline verifier did not check `OwnerReleaseGenerationGate::VerifyObserved`. | Add the Debug-only observer to the forbidden factory list. | Release verifier PASS with zero forbidden factories; the Debug negative control names `Grant::ForTest, OwnerReleaseGenerationGate::VerifyObserved, Qualified::ForTest`. |

### 5.1 Accepted post-scan residual (Terra F1, owner-decision section 2)

A generation switch that is observable at the verifier's last relevant stable observation
refuses the physical RELEASE. A switch that occurs **after** that observation may escape, even
if it happens before the scan returns or before the write. No lock spans the protected read and
the physical write, so P3 does not claim perfect concurrent-switch detection or retroactive
cancellation of an in-flight write. This is the accepted trusted-root observation-to-effect
residual, unchanged in strength; the owner echo and the immutable-generation model remain the
detection layers and the stop path keeps its original authority. The controller-side Q10 check
remains additional and unchanged.

## 6. Deterministic tests and exact results

Commands were run from `C:\Users\Laszlos\source\repos\Gagamba` on the host above.

| Check | Result |
|---|---|
| Debug build of M1 tests | **0 warnings, 0 errors**; instrumented verifier PASS (4/4 hook types, 3/3 hook properties, 13/13 core types) |
| P3 focused (`~OwnerReleaseGenerationGate`) | **27 passed / 0 skipped / 0 failed** (11 decision + 16 arbiter/wiring) |
| `~ReleaseGenerationGateTests` (Q10 + P3) | **20 passed / 0 skipped / 0 failed** (9 Q10 + 11 P3) |
| P2 focused regression filter (8 classes), with P3 | **196 passed / 20 skipped / 0 failed / 216 total** |
| P2 focused regression filter (8 classes), excluding P3 | **185 passed / 20 skipped / 0 failed / 205 total** (pre-P3 baseline preserved) |
| Full Windows M1 Debug runner | **1,322 passed / 331 skipped / 0 failed / 1,653 total** |
| Release `Gagamba.Execution.Linux.M1` build | **0 warnings, 0 errors**; production verifier PASS (0/4 hook types, 0/3 hook properties, 13/13 core types) |
| Release `Gagamba.Execution.Linux.M1.Owner` build | **0 warnings, 0 errors**; production verifier PASS |
| Release `Gagamba.Execution.Linux.M1.Recovery` build | **0 warnings, 0 errors**; production verifier PASS |
| Production verifier on the Release M1 DLL | PASS |
| Negative production-verifier control on the Debug M1 DLL | FAIL as expected, naming **`Grant::ForTest, OwnerReleaseGenerationGate::VerifyObserved, Qualified::ForTest`** |

The pre-P3 full-run baseline was **1,295 passed / 331 skipped / 0 failed (1,626 total)**. P3 adds
exactly 27 deterministic tests: **1,322 / 331 / 0 (1,653)**. Eleven of those tests match the
existing `~ReleaseGenerationGateTests` term, so the P2 focused filter rose from the preserved
185/20 to 196/20. The 331 native skips were not executed and are not passes.

### 6.1 Executed commands

```text
dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Debug
dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerReleaseGenerationGate"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OwnerAuthorityContextTests|FullyQualifiedName~OwnerManifestAdmissionGateTests|FullyQualifiedName~FinalAcceptanceTests|FullyQualifiedName~ReleaseGenerationGateTests|FullyQualifiedName~InstallationWireTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~ReleaseArbiterTests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName!~OwnerReleaseGenerationGate&(FullyQualifiedName~OwnerAuthorityContextTests|FullyQualifiedName~OwnerManifestAdmissionGateTests|FullyQualifiedName~FinalAcceptanceTests|FullyQualifiedName~ReleaseGenerationGateTests|FullyQualifiedName~InstallationWireTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~ReleaseArbiterTests)"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build
dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj -c Release
dotnet build src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj -c Release
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll"
dotnet run --project eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj -c Release --no-launch-profile -- --production "src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll"
```

The last command is the Debug negative control; it fails as expected and names the three
Debug-only authority factories.

### 6.2 Test-to-claim mapping

| Test | P3 claim |
|---|---|
| `UnchangedObservationVerifies` | An unchanged full retained tuple verifies (not an unconditional refusal). |
| `ReplacedGenerationRefusesAsUnqualified` | A replaced active generation refuses as a completed mismatch; never adopts the newer selection. |
| `MissingActiveSelectorRefuses` | A missing active selector refuses. |
| `FreshAndUnqualifiedSelectionsRefuse` | Installed-but-unaccepted and refused records cannot carry a release. |
| `QualificationRecordHashChangeRefuses` | A changed raw record hash refuses. |
| `QualificationIdChangeRefuses` | A changed qualification id refuses. |
| `RefusalNeverAdoptsTheObservedSelection` | The retained tuple is unchanged after a refusal. |
| `ProductionEntryRefusesWithoutAVerifiedInstallation` | The production entry refuses fail-closed without an actual verified installation. |
| `LegacyContextHasNoInstalledGenerationModel` | Legacy verifies nothing (controller Q10 parity). |
| `ValidationContextRemainsUnreachableAndRefusesFailClosed` | Validation remains non-constructible and any instance refuses as unavailable. |
| `GateExposesNoBooleanOrCallerCallbackSurface` | No Boolean, delegate parameter or Boolean property exists on the gate. |
| `QualifiedVerifiedObservationStillReleasesExactlyOnce` | Verified qualified operations still release with exactly one attempt. |
| `UnqualifiedRefusalHasZeroPhysicalAttemptsAndPostIntentShape` | Unqualified refusal: zero attempts and the exact conservative post-intent shape. |
| `UnavailableRefusalAlsoRefusesWithZeroPhysicalAttempts` | Unavailable refusal also refuses with zero attempts. |
| `RefusalSubmitsSetupFaultStopTailOutsideTheGate` | The refusal submits a durable `SetupFault` stop tail and keeps the intent. |
| `DuplicateAfterRefusalReturnsC4WithoutSecondAttemptOrStop` | A duplicate returns C4 without verification, a second attempt or a new stop write. |
| `StopDuringBlockedVerificationWinsWithoutPhysicalAttempt` | Stop wins while the verifier is blocked; the stop-won wording and cause are preserved. |
| `DeadlineExpiryDuringVerificationCannotBeOverriddenBySuccess` | A successful observation never overrides an expired deadline. |
| `VerificationFaultRefusesFailClosedAndStillStops` | A verifier fault refuses fail-closed and still stops. |
| `NewArbiterOverDurableIntentCannotVerifyOrResend` | A new arbiter cannot verify or resend over a durable intent. |
| `LegacyNoGateKeepsTheExistingReleasePath` | Legacy keeps the exact existing release path and never verifies. |
| `RefusedDecisionRoundTripsOnTheOwnerWireForBothReasons` | Both P3 refusal reasons survive the owner wire codec with decision equality. |
| `ArbiterStoresVerificationAsAReadonlyConstructionDependency` | The verification is construction-time only (readonly field, optional null-default parameter, one `stfld`). |
| `RequestReleaseVerifiesAfterIntentAndBeforeConsumptionAndWrite` | IL ordering: intent before verification, verification before consumption and the single write. |
| `OwnerPhasePinsTheSingleSharedVerificationDecision` | The phase wires a method group and forwards to exactly one shared Q10 decision. |
| `SharedVerificationIsReferencedOnlyFromTheOwnerPhase` | Anti-bypass: the shared decision is referenced only from the owner phase. |
| `DebugObserverHasNoProductionCaller` | The Debug-only observation entry has no production caller in the M1 assembly. |

### 6.3 Negative control (exit criterion 8.4)

The wiring tests fail if the owner wiring is removed or bypassed: `RequestReleaseVerifiesAfterIntentAndBeforeConsumptionAndWrite` requires the verification call between intent and consumption; `SharedVerificationIsReferencedOnlyFromTheOwnerPhase` requires exactly one owner call site; and `ArbiterStoresVerificationAsAReadonlyConstructionDependency` requires the readonly optional dependency. The production verifier adds a Release/Debug factory-exclusion control.

## 7. T01-T23 traceability

The governing grouping is the implementation handoff section 5, and row 62 requires the
deterministic tests to exercise the real arbiter and the final after-intent path. P3 addresses
the release-authority rows:

| Cases | P3 disposition |
|---|---|
| T10 (direct RELEASE omitting controller Q10) | **P3 deterministic owner final-read refusal implemented and tested** through the real arbiter with a counting channel (attempts = 0). The native active-switch (A to B) positive remains pending. |
| T11 (switch during owner intent) | **P3 deterministic refusal and conservative post-intent shape implemented and tested**; durable intent present, attempts = 0. The native controlled switch remains pending. |
| T12 (change generation while HELD) | Controller Q10 in `IntegratedB3Client` unchanged; the direct-request variant is independently refused by the owner gate. Native variants remain pending. |
| T13 (concurrent RELEASE) | **P3 deterministic**: duplicate returns C4 without a second attempt or stop; stop wins while the verifier is blocked; at most one physical attempt. Native concurrency remains pending. |
| T14 (deadline expiry and blocked verifier) | **P3 deterministic** with a controlled clock: expiry during verification refuses even when verification would succeed; stop during a blocked verification wins; a late/new arbiter cannot revive authority. Native watchdog rows remain pending. |
| T15 (stop after final withdrawal) | The P3 refusal continues the original stop/cleanup authority without renewed admission; already-admitted stop semantics are unchanged and covered by the existing arbiter stop tests. Native kill/reaper observations remain pending. |
| T19 (legacy v1 compatibility) | **P3 deterministic**: legacy verifies nothing and the existing release path is unchanged; a false installed claim cannot route through legacy. Native legacy positives were not rerun. |
| T21 (retained identity and no second trust root) | **P3 deterministic**: the retained tuple is never replaced, the single shared Q10 decision is reused, and no caller/Boolean/wire trust source exists. Protected fixed-root reads remain pending. |
| T22 (post-scan root residual) | Reported as the accepted residual (section 5.1), not as a promised atomicity and not as a native Q10 shortcut. |
| T01-T09, T16-T18, T20, T23 | **P2/Slice E/P4 pending.** P3 adds no admission, validation, bootstrap or administrative behavior. |

## 8. Lifecycle and cleanup

- No test created a process, cgroup, domain, staging directory, protected file or native host. The deterministic tests use in-memory manifests, synthetic observations, a fake clock, a fake journal storage and a counting transport.
- No production writer, RELEASE attempt or target execution was created; every refusal test asserts zero physical attempts.
- Cleanup status: **not applicable / no test-owned OS resources**. No cleanup claim is converted from an observation that did not occur.
- The C3 deadline, Policy A stop, C1/C2/C4 and the owner-only writer are unchanged; the P3 stop tail uses the existing stop path under the original authority.
- Already admitted operations retain stopping, reaping and reconciliation without renewed admission or a new active-generation read.

## 9. Preservation

- Canonical `main`, one worktree at `C:\Users\Laszlos\source\repos\Gagamba`.
- The 44 historical untracked paths (`git ls-files --others --exclude-standard`) were present before the implementation commit and remain at 44 after it. P3 adds four tracked source/test paths.
- No reset, rebase, stash, clean, prune, branch, worktree, force operation or push occurred.
- Prior owner decisions, evidence, reviews and reviewer bytes are unchanged.

## 10. Source and binary identity

| File | SHA-256 |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/OwnerGenerationGateRefusal.cs` | `FD97795294C4D133CA0C88383A7EFDBE40736FB488CE148F7E5CF63D6A0AE061` |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerReleaseGenerationGate.cs` | `B17CE18156F4233A99E2351E9FB5166FDAFE06232BAC0D66F4AB1AC71A73C827` |
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | `9272CA451B10C1A56ED488745CD7717B9379B3F07EE859AD6ECDBA6C015F417A` |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs` | `B30A802BE3EFE3F06381EA3A4E086F9E149556D269C2D92DB1F41B3758780AEB` |
| `eng/VerifyProductionAssembly/Program.cs` | `6CB286EA376B8A8F00CFE68BC0F41D42AC7C841BAA772DEF3A024BFC30DAE1BD` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerReleaseGenerationGateTests.cs` | `14C95AC73D9B0FF6D591A0F06A99C5F510F5437B8A6550382F55B467ACAF4A51` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/OwnerReleaseGenerationGateArbiterTests.cs` | `45A1C1CDA5C55D95DA687E7D01E63BC532843A4155CDCECF88BCED9B29A38E5B` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/IlCallScanner.cs` | `946ABC08A5CA570B57BACF1217771FCC38D8E07E65C376FD5534BD17897C9BEF` |

Release M1 DLL SHA-256: `0F2F92B8DC39FCD21D242CC6709A9D6C19C4703C4A598B67753AED6368F8E85D` (MVID `9d542cac-933d-47ff-ac43-1c6ae387f5d2`).

Debug M1 DLL SHA-256: `E1AF512D5CF580F49EE1265A7F2BAB4BF295DA7DC8FEE2F15F0174DCDA3389E4` (MVID `e4cd566c-9707-47e1-bb16-fff3213c2b8b`).

Debug test DLL SHA-256: `809128C3FD9A1AA00E534488DDBB5FF5E20F1AADA7061F0D6FF33AB3E64573CF`.

## 11. Findings and qualifications

- **P3-F01 (native):** the trusted fixed-root positive observation, the controlled A-to-B generation switch and the real owner/controller generation race are **native-pending**. The 331 skips are not passes.
- **P3-F02 (residual):** the post-scan observation-to-effect residual (section 5.1) is accepted and unchanged in strength. P3 does not promise perfect concurrent-switch detection or retroactive write cancellation.
- **P3-F03 (scope):** P3 gates the integrated direct owner release path only. The controller Q10 check and the sole writer are preserved; P3 adds no new writer, journal schema, deadline, correlation reset or protocol message.
- **P3-F04 (validation):** validation remains non-constructible; no positive validation path or Slice E behavior is added.
- **P3-F05 (stop tail):** the tail stop is best effort and durable under the journal gate; a tail limitation is recorded on the retained refusal rather than retried, and the refusal already excludes live execution. A verification fault is wrapped fail-closed as `InstallationUnavailable`.
- **P3-F06 (verifier):** the Debug-only `VerifyObserved` entry relies on conditional compilation and is additionally listed as a forbidden Release factory so the control fails if the conditional is removed.

## 12. Qualification boundary

This evidence establishes deterministic owner-side pre-RELEASE decision, refusal, stop-tail,
wiring and effect-ordering properties through the real release arbiter, plus build-identity and
production-exclusion properties, on the documented Windows host only. It does **not** establish:

- a protected fixed-root positive observation or protected-store stability on a real root-owned installation;
- native Linux owner/controller/recovery qualification, actual generation races or descriptor/custody behavior;
- validation authorization, validation-session capability, GAVA1 or v3 prepublication parsing;
- P4 complete pre-effect sink coverage or non-integrated B2/`QualifiedB2Owner` gating;
- production activation, publication, packaging or public factory enablement.

## 13. Independent review questions

1. **Gate dominance.** Is every new physical RELEASE attempt on the qualified owner path dominated by the fresh after-intent active-generation verification?
2. **Outside-lock placement.** Does the verification run outside `_gate`, `_journalGate`, safety latches and capability locks, with no test barrier, journal I/O, protocol wait or unrelated I/O between the successful observation and final gate entry?
3. **Full comparison.** Is the comparison the full retained identity plus qualification id and raw record hash, with no null skip and no adoption of the observed selection?
4. **Refusal shape and capability.** On refusal, is the conservative post-intent shape retained with zero physical attempts, the capability closed once, no deadline renewal, no retry and no new writer?
5. **Stop authority.** Does the refusal latch the existing stop path under the original context and C3 authority so the owner becomes safe without a controller request?
6. **Winner preservation.** Do the existing retained-result, stop, deadline, prerequisite and liveness rechecks still precede the new check, and does a duplicate return C4 without a second attempt?
7. **Legacy and controller preservation.** Are the legacy path and the controller-side Q10 check (`IntegratedB3Client`) preserved unchanged, with no new journal schema, writer or public API?
8. **Production exclusion.** Is the verification wired only from the sealed owner context (no settable caller callback, Boolean or wire echo), and is the Debug-only observation entry excluded from Release?
9. **Native honesty and residual.** Are the fixed-root positive and generation-race rows explicitly native-pending, and is the post-scan observation-to-effect residual reported rather than promised away?

## 14. Final disposition

**P3 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

P4, Slice E, Slice N, protected provisioning, native qualification and production activation
remain unimplemented and unauthorized. The 331 native skips remain unqualified.
