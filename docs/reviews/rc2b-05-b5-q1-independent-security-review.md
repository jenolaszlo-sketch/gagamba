# RC2B-05 / B5 — Independent Sealed-Clock, Durable-Deadline and Reconciliation Security Review

**Status:** Independent review complete

**Scope:** B5 implementation at commit `0bf045d0ec3e4cb560edb2bc3b4e7065efaa7b9c`, accepted C3 contract, Policy A, typed reconciliation and regression preservation.

**Verdict:** SUPPORTED WITH QUALIFICATIONS

---

## A. Git and Source Binding

| Object | SHA |
|---|---|
| Authorized starting HEAD | `e7818b5a876c220cb4f803b6cc1f9c55027d3c66` ✓ Verified |
| B5 implementation | `0bf045d0ec3e4cb560edb2bc3b4e7065efaa7b9c` ✓ Verified |
| B5 implementation tree | `80d138f81c82b61243e23c0865eb6a1ca516cc54` ✓ Verified |
| B5 evidence commit | `9cba151ee82bdba301251f0c12fc1550c9e6d5d6` ✓ Verified |
| Evidence tree | `155da2cfd0d185eba0b6187b3e69f390cd6c348e` ✓ Verified |

**Ancestry verified:** `0bf045d` descends from `e7818b5` with a clean tracked tree. The 27 changed files in the implementation commit match the evidence document. No untracked modifications were introduced during implementation.

**Evidence integrity:** All 108 raw evidence manifest entries are present and hash-verified against `docs/evidence/rc2b-05-b5-impl-01-sha256-manifest.txt` (SHA-256 `476809507ab458c59670d439f437d1d9606528eab8f3db8f14bba466638c0dd2`). The `native-summary.json` binds to the exact source commit and tree.

---

## B. Raw Evidence Integrity

- **Windows M1 assembly:** 1,050 passed / 0 failed / 269 skipped (log: `windows-solution-full.log`)
- **B5 focused deterministic tests:** 61/61 passed (logs: `b5-focused-1.log`, `b5-focused-2.log`, `b5-focused-3.log`, `after-fix-focused.log`, `postcommit-b5-focused.log`)
- **D6.4 variant:** 3/3 passed (log: `postcommit-d64.log`)
- **Native `NonRootQualificationTests` (WSL2):** 33/33 passed including two new B5 deadline cases (log: `wsl-class-NonRootQualificationTests.log`)
- **INT-01 full matrix:** 32/32 passed, cleanup classification 20 Confirmed / 12 Unknown (logs: `wsl-int01-full.log`, `wsl-int01-full2.log`, `committed/int01-full.log`, `committed/nonroot-integrated.log`)
- **Owner death qualification:** 7/7 (log: `wsl-class-OwnerDeathQualificationTests.log`)
- **Release path qualification:** 3/3 (log: `wsl-release-path-class.log`)
- **Termination observer:** 3/3 (log: `wsl-class-Wp05TerminationObserverTests.log`)
- **Sigpipe disposition:** 1/1 (log: `wsl-class-Wp05SigpipeDispositionTests.log`)
- **Recovery harness:** 3/3, 2 environment skips (log: `wsl-class-RecoveryHarness.log`)
- **`eng/tests` classifier suite:** 35/35 (log: `eng-tests.log`, `postcommit-eng-tests.log`)

**Test categories are not conflated.** The 61 deterministic tests exercise in-memory fakes; the 33 native WSL2 tests exercise real cgroups, processes, and journal. The 32 INT-01 cases are a separate fault/acknowledgement matrix. The producer correctly reports them as distinct inventories.

---

## C. Clock Provenance and Conversion Assessment (Review Gate A)

### C.1 Sealed Production Clock

**Implementation:** `src/Gagamba.Execution.Linux.M1/Lifecycle/MonotonicClock.cs`

- `StopwatchDomainClock` is `internal sealed` with a private constructor and a single static `Instance` property
- `Basis` returns `(MonotonicEpoch.Qpc, Stopwatch.Frequency)` — fixed at type initialization
- `NowNanoseconds` delegates to `MonotonicTime.NowNanoseconds()` which uses the exact accepted `OwnerClock` conversion:
  ```csharp
  (timestamp / frequency * 1_000_000_000) + (timestamp % frequency * 1_000_000_000 / frequency)
  ```
  with `checked` arithmetic and `frequency <= 0` guard.

**Production composition:** `B3Qualification.Reconcile()` and `IntegratedRecoveryHost` cleanup task both construct `Reconciler(StopwatchDomainClock.Instance, TimeSpan)`. The open `IDomainClock` constructor is retained **only** as a deterministic test seam and is never reachable from production composition.

**Clock validation before destructive action:** `Reconciler.TryResolveStopBudget()` validates:
1. Declared epoch is `MonotonicEpoch.Qpc` (not `Unknown`)
2. Exact `Stopwatch.Frequency` match
3. Current timestamp > 0
4. Checked arithmetic on `deadline - now`

On any refusal: returns `QuarantinedUnknown`, `DestructiveActionTaken = false`, no kill/removal attempted.

### C.2 Cross-Process Epoch Compatibility

The implementation **does not claim** cross-platform or cross-boot epoch compatibility. The `MonotonicEpoch.Qpc` documentation states:

> "On Linux this is the .NET monotonic source (CLOCK_MONOTONIC-backed), not a literal Windows QPC device; cross-process comparability holds only for the same supported basis and a compatible boot context."

The reconciliation planner explicitly refuses cross-boot (`RecoveryPlanKind.RefuseCrossBoot`) when `record.BootId != currentBootId`. The clock basis check in `TryResolveStopBudget` adds a second gate: even within the same boot, a frequency mismatch or unknown epoch refuses.

**Assessment:** The sealed class + singleton + fixed basis + pre-subtraction validation is **correctly implemented**. The producer honestly documents the limitation: WSL2 is not an independent native Linux host; cross-platform clock comparability is not claimed. This is a **qualification**, not a defect.

### C.3 Test Seam Isolation

The `IDomainClock` test seam:
- Cannot be passed from production composition (production ctor takes `StopwatchDomainClock` concrete type)
- The `FakeDomainClock` and `RelativeTestClock` in tests declare `MonotonicEpoch.Unknown` or wrong frequency and are correctly refused
- The `GAGAMBA_WP05_D6_4_TEST_FIXTURE` conditional compilation guards the third Reconciler constructor and `NotAttemptedStopTestFixture`

**Finding:** No production path can inject a foreign clock. The test seam is isolated.

---

## D. Durable Deadline and Policy A Correctness (Review Gates B, C)

### D.1 Authoritative Deadline Arithmetic

**Implementation:** `Reconciler.TryResolveStopBudget()` (lines 165–253)

```csharp
checked { remaining = stop.StopDeadlineNanoseconds - now; }
budgetNanoseconds = remaining > 0 ? remaining : 0;
```

**Validation before use:**
- `StopAcceptedAtNanoseconds > 0`
- `StopDeadlineNanoseconds > 0`
- `StopDeadlineNanoseconds >= StopAcceptedAtNanoseconds`
- `declaredAllowance == StopConfirmationAllowanceNanoseconds (5_000_000_000)`
- Clock basis compatibility (epoch + frequency)
- Clock availability and positive timestamp
- Checked arithmetic on all subtractions

**Caller allowance ignored** when a valid `StopRecord` exists (line 161–163 comment and code at 173–183).

### D.2 Failing-Before Regression Preserved

**Pre-B5 behavior (recorded in `failing-before.log`):**
- Stop accepted at 1s, deadline 6s
- Recovery at 100s with populated domain
- Pre-B5 reconciler performed **200 polls (5 seconds fresh)** and returned `Failed`

**B5 behavior (test `CallerAllowanceCannotOverrideExpiredPersistedStopDeadline`):**
- Same inputs
- **0 polls**, 1 kill, 1 observation
- Returns `QuarantinedUnknown` (not `Failed`)
- Clock `NowNanoseconds` unchanged (no time advanced)

The former D2 probe asserting the C3 violation was replaced by `ReconcilerConsumesPersistedStopDeadlineAndAppliesPolicyAWhenExpired`.

### D.3 Policy A: Expired and Exact-Expiry Cases

| Case | Remaining | Polls | Kill Writes | Population Reads | Outcome |
|---|---|---|---|---|---|
| Future, 1s remaining | 1,000,000,000 | 40 (25ms each) | 1 | 1 (per poll) | `QuarantinedUnknown` |
| Future, 100ms remaining | 100,000,000 | 4 | 1 | 1 | `QuarantinedUnknown` |
| Caller allowance 1h, 500ms remaining | 500,000,000 | 20 | 1 | 1 | `QuarantinedUnknown` |
| Exact expiry (`now == deadline`) | 0 | **0** | 1 | 1 | `QuarantinedUnknown` |
| Expired by 360s | 0 (max) | **0** | 1 | 1 | `QuarantinedUnknown` |

**Verification:**
- `ExactExpiryAppliesPolicyAWithoutRenewedWait`: 0 delays, 1 kill, 1 read
- `ExpiredDeadlineAppliesPolicyAWithoutRenewedWait`: 0 delays, 1 kill, 1 read
- `PolicyADoesNotSkipTheFinalPopulationObservation`: 1 read even when `killAccepted=false`
- `PolicyADefinitiveKillFailureRemainsFailed`: `KillDispatchOutcome.Failed` → `ComponentState.Failed`, aggregate `Failed`
- `PolicyAObservationUnavailableIsUncertainNotFailed`: `ReadFault=true` → `Unknown`, not `Failed`

**No hidden sleep/retry:** The zero-budget path in `DomainLease.StopAndAwaitEmpty(long, clock)` performs exactly one `ReadPopulation()` after the kill and returns immediately — no `Delay` call.

### D.4 Repeated Reconciliation and Partial Cleanup

- `RepeatedReconciliationNeverExtendsTheAbsoluteDeadline`: Two reconciliations at `deadline-2s` then `deadline-1s` yield 80 then 40 polls; `StopRecord` byte-for-byte unchanged
- `PartialCleanupDoesNotResetTheDeadline`: When `DomainStop|DomainEmpty` already durable, no kill, no poll, `StopRecord` unchanged

### D.5 Cross-Process Deadline Retention

The native test `NonRootSameBootRecoveryKeepsTheOriginalDeadlineBeforeExpiry` plants a real `StopRecord` in the real journal, kills the real owner, and reconciles twice — the original deadline is retained across two reconciliations in a new same-boot native process.

---

## E. No-Record Authority Assessment (Review Gate D)

### E.1 Separation of Paths

`TryResolveStopBudget` has two distinct branches:
1. **Valid StopRecord** (lines 183–252): authoritative deadline, caller allowance ignored
2. **No StopRecord** (lines 173–182): caller allowance used **only** after negative check (`< TimeSpan.Zero` refuses)

### E.2 Authority Provenance

The no-record allowance comes from the Reconciler's construction parameter (`_noRecordCleanupAllowance`). In production:
- `B3PreparationOwner.Reconcile()` passes `TimeSpan.FromSeconds(5)`
- `IntegratedRecoveryHost` cleanup task passes `TimeSpan.Zero`

**No synthesis of deadline** from an absent/invalid record. The validation in `TryResolveStopBudget` (lines 185–221) refuses on any invalid provenance before destructive action.

### E.3 Release Authority Not Restored

- The no-record path never grants RELEASE capability
- `OwnershipTransitions.TryPlanStop` forbids release intent after a durable stop (line 27–30)
- Recovery scope never receives preparation capability or RELEASE writer
- `RestrictedExecutionRuntime.Create()` remains production-unavailable

**Assessment:** The no-record path is correctly separated, conservatively refuses on invalid record, and does not recreate release authority.

---

## F. Typed Reconciliation Outcome Assessment (Review Gate E)

### F.1 Outcome Types

| Type | Values |
|---|---|
| `KillDispatchOutcome` | `NotAttempted=0`, `Failed=1`, `Accepted=2`, `Unknown=3` |
| `StopPopulationObservation` | `NotObserved=0`, `Unknown=1`, `Populated=2`, `Empty=3` |
| `StopOutcome.Stopped` | `true` iff `Kill == Accepted && Population == Empty` |

Diagnostic `Error` text is never parsed for classification.

### F.2 Mapping Verification (Wp05B5C3TypedOutcomeTests + D6.4 variant)

| Native Observation | DomainStop | DomainEmpty | Aggregate |
|---|---|---|---|
| Kill accepted, empty observed | Confirmed | Confirmed | CleanupConfirmed ✓ |
| Kill accepted, still populated | Unknown | Unknown | QuarantinedUnknown ✓ |
| Kill accepted, observation unavailable | Unknown | Unknown | QuarantinedUnknown ✓ (1 read) |
| Kill definitively failed | Failed | Unknown* | Failed ✓ |
| Kill dispatch unknown | Unknown | Unknown | QuarantinedUnknown ✓ |
| Kill not attempted, trusted fallback empty | Unknown | Confirmed | QuarantinedUnknown ✓ (R1-T13) |
| Kill not attempted, fallback populated | Unknown | Unknown | QuarantinedUnknown ✓ (R1-T14) |
| Kill not attempted, fallback unavailable | Unknown | Unknown | QuarantinedUnknown ✓ (R1-T15) |

*Independent empty observation via fallback may confirm `DomainEmpty` but aggregate stays `Failed` (test `DefinitiveKillFailureWithIndependentEmptyObservationStillFailed`).

**No accidental promotion:** `Stopped` is never established merely because kernel accepted a kill request. `Refused` (reconciliation planner) remains distinct from `Failed` and `Unknown`.

---

## G. Journal Progress and Finalization Assessment (Review Gate F)

### G.1 Invariant Verification

| Scenario | JournalFinalization | Aggregate | Test |
|---|---|---|---|
| All confirmed, final write succeeds | Confirmed | CleanupConfirmed | T5, R1-T5 |
| Stop-progress write fails persistently | Unknown | QuarantinedUnknown (not Failed) | `ComponentProgressPersistenceFailureIsUnknownNotFailed` |
| Final write fails after confirmed empty | Unknown | QuarantinedUnknown (not Failed/Confirmed) | `FinalPersistenceFailureIsUnknownNotFailed` |
| Earlier progress fails, later final succeeds | Unknown | QuarantinedUnknown; durable verdict not Confirmed | `EarlierProgressFailureKeepsFinalVerdictUnknownEvenIfFinalWriteSucceeds` |
| Staging fails + final write fails | Unknown | **Failed** (operation failure retained) | `MixedOperationAndPersistenceFailureStaysFailed` |
| No writer lease | Unknown | QuarantinedUnknown; never CleanupConfirmed | `MissingWriterLeaseCannotProduceCleanupConfirmed` |

### G.2 Key Properties Confirmed

1. **Persistence-only uncertainty ≠ operation failure** — progress write failure yields `Unknown`, not `Failed`
2. **Persistence-only uncertainty ≠ Confirmed cleanup** — final write failure yields `Unknown`, not `Confirmed`
3. **Genuine component failure stays Failed** despite concurrent persistence uncertainty
4. **Successfully persisted Unknown ≠ Confirmed cleanup** — verdict remains `Unknown` until all required components are `Confirmed` and no persistence uncertainty
5. **Earlier progress failure not erased by later success** without accepted reconciliation evidence — `EarlierProgressFailureKeepsFinalVerdictUnknownEvenIfFinalWriteSucceeds` confirms durable verdict remains not `Confirmed`
6. **Aggregate precedence consistent**: any required `Failed` → `Failed`; else any required `Unknown` or `JournalFinalization=Unknown` → `QuarantinedUnknown`; else `CleanupConfirmed`

### G.3 B4Q1-F01 Preservation

The cleanup classification hardening (20 Confirmed / 12 Unknown) remains effective after B5. The two explicitly preserved Unknown cases (`missing-domain-ack`, `missing-staging-ack`) remain Unknown. No regression in INT-01 cleanup interpretation.

---

## H. C1/C2/C4/INT-01/B4 Preservation (Review Gate G)

| Property | Status | Evidence |
|---|---|---|
| Owner-exclusive physical RELEASE | Preserved | `B3PreparationOwner.ReleaseCore`, `IntegratedRecoveryHost` — only owner holds capability/RELEASE writer |
| At-most-one physical RELEASE attempt | Preserved | `ReleaseArbiter` logic unchanged; `PhysicalReleaseAttempts` tracked |
| Recovery without release authority | Preserved | Scope never receives capability/store/RELEASE writer |
| C1 retained ambiguity / first-retainer ordering | Preserved | No changes to owner protocol or retention logic |
| C2 irreversible stop-first capability consumption | Preserved | `PreparedTransaction.TryConsume` in `OwnerSafetyMonitor.AcceptStop`; `PublishCapability` returns false if stop latched |
| C4 duplicate/concurrent release semantics | Preserved | `ReleaseDecisionKind.Duplicate` handling unchanged |
| R1-F03 Option A | Preserved | Policy A implemented exactly as specified |
| Original preparation/setup/overall deadlines | Preserved | `OwnerPreparationTransaction` deadlines unchanged |
| Independent native stopping | Preserved | `OwnerSafetyMonitor` native stop path unchanged; `DomainLease.StopAndAwaitEmpty` unchanged |
| Protected B4 capture and stop behavior | Preserved | `ProtectedCapture`, `BoundDrain`, `SafetyStopRetained` unchanged |
| No journal schema change | Preserved | `OwnershipSnapshotCodec` version 3 unchanged |
| No new public restricted-execution activation | Preserved | `RestrictedExecutionRuntime.Create()` unavailable; `B3Qualification` remains internal |

### H.1 D6.4 Test Fixture Isolation

- `GAGAMBA_WP05_D6_4_TEST_FIXTURE` symbol only defined in `Gagamba.Execution.Linux.M1.D6_4.Tests` project
- Production `Gagamba.Execution.Linux.M1.dll` contains no fixture code, no symbol, no friend grant to D6.4
- Variant assembly grants internals only to `Gagamba.Execution.Linux.M1.D6_4.Tests`
- `Gagamba.Execution.dll` carries two additional compile-time friend names per accepted design; no fixture symbol, no runtime fixture code
- Production and variant output/intermediate paths are disjoint; variant is non-packable, excluded from solution/Owner graph

**Assessment:** Testability changes do not widen production trust boundary. The D6.4 variant is explicitly not binary-identical evidence for production.

---

## I. Native and Deterministic Qualification Inventory

### I.1 Deterministic (In-Memory Fake) Tests — 61 cases
- Clock trust: 14 cases (T1)
- Deadline arithmetic: 10 cases (T2/T3)
- Typed outcomes: 11 cases (T4)
- Journal behavior: 6 cases (T5)
- Authority/no-record: 7 cases (T6)
- Failing-before: 1 case
- Historical fixtures: 13 cases

### I.2 D6.4 Variant (Same-Project) — 3 cases
- R1-T13: NotAttempted + trusted empty fallback → DomainEmpty Confirmed
- R1-T14: NotAttempted + populated fallback → Unknown
- R1-T15: NotAttempted + unavailable fallback → Unknown

### I.3 Native WSL2 Tests — 33 cases (`NonRootQualificationTests`)
Includes two new B5-specific cases:
1. `NonRootExpiredDurableStopDeadlineAppliesPolicyAWithoutRenewedWait` — real `StopRecord`, real journal, real owner kill, sealed clock reconciliation; completes in <2s, no fresh 5s wait
2. `NonRootSameBootRecoveryKeepsTheOriginalDeadlineBeforeExpiry` — same deadline retained across two reconciliations

**WSL2 is explicitly not claimed as independent native Linux host.** The environment is unprivileged (`CapInh/CapPrm/CapEff/CapAmb = 0`), transient user unit with `Delegate=memory pids`, `DelegateSubgroup=host`.

### I.4 INT-01 Full Matrix — 32 cases
Reconstructed from preserved B4 fixture assets and rebuilt B5 closures. **Not a replay of the historical B4 profile.** Cleanup classification: 20 Confirmed / 12 Unknown. The two preserved Unknown cases (`missing-domain-ack`, `missing-staging-ack`) remain Unknown.

### I.5 Skipped / Not Qualified (Explicitly Acknowledged)
- `IntegratedBarrierTests` / `DomainAuthorityTests`: most cases skip due to `/sys/fs/cgroup` root probe limitation (pre-existing, not passes)
- `B3RestrictedLifecycleTests`: native install-closure cases skipped (`artifacts/b2/install` absent)
- `DirectAuthenticationTests`: explicit profile/controller artifact not enabled
- B4-specific confined matrix not re-run (covered by INT-01 full matrix + deterministic regressions)
- Cross-platform (macOS, native Linux distribution) not qualified

---

## J. Independent Reproduction Assessment

The producer provides exact reproduction commands:

**Windows:**
```powershell
dotnet build Gagamba.sln -c Release
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/... -c Release --no-build --filter "FullyQualifiedName~Wp05B5C3"
dotnet test tests/Gagamba.Execution.Linux.M1.D6_4.Tests/... -c Release
dotnet test Gagamba.sln -c Release --no-build
python -m unittest discover -s eng/tests
```

**WSL2 (bound command):**
```bash
cd /home/gaga/gagamba-b5-impl-01
systemd-run --user --unit=gagamba-b5-committed \
  --property="Delegate=memory pids" --property=DelegateSubgroup=host \
  --property=ExitType=cgroup --property=KillMode=control-group \
  --property=TimeoutStopSec=10 --property=RuntimeMaxSec=1800 \
  --collect --wait --pipe bash /mnt/c/.../run-committed-evidence.sh
```

**Artifact hashes** bound in `committed/profile-binding.json` and `binding.json`.

**Reviewer note:** Independent native reproduction was not executed in this review (environment not available). The assessment is **inspection-only** for native cases. The deterministic test inventory and failing-before log are independently verifiable from the committed evidence.

---

## K. Findings and Limitations

### K.1 Findings (No Material Contract Violations)

| ID | Severity | Contract | Location | Summary |
|---|---|---|---|---|
| B5-F01 | Qualification | C3 clock | `MonotonicClock.cs:20-23` | Cross-process epoch compatibility explicitly limited to "same supported basis and compatible boot context"; not a defect, documented honestly |
| B5-F02 | Qualification | C3 deadline | `Reconciler.cs:165-253` | Clock basis check uses exact `Stopwatch.Frequency` match; a frequency change across boots (theoretically possible on some platforms) would refuse — conservative, correct |
| B5-F03 | Qualification | C3 Policy A | `DomainLease.cs:214-239` | Zero-budget path performs kill + exactly one immediate `ReadPopulation()`; no `Delay` — correctly implements Policy A |
| B5-F04 | Qualification | C3 typed | `Reconciler.cs:386-469` | All typed combinations map per specification; `Stopped` requires `Accepted + Empty`; `Refused` distinct from `Failed`/`Unknown` |
| B5-F05 | Qualification | C3 journal | `Reconciler.cs:288-333, 603-680` | Persistence uncertainty never fabricates `Failed` or `Confirmed`; aggregate precedence consistent |
| B5-F06 | Qualification | C1/C2/C4 | `B3PreparationOwner.cs`, `IntegratedRecoveryHost.cs` | No regression in release authority, capability consumption, or duplicate release semantics |
| B5-F07 | Qualification | INT-01 | `native-summary.json` | 20 Confirmed / 12 Unknown preserved; `missing-domain-ack` and `missing-staging-ack` remain Unknown |

### K.2 Limitations (Deferred / Out of Scope)

| ID | Description | Impact |
|---|---|---|
| B5-L01 | WSL2 not independent native Linux host | Native cgroup/process qualification limited to WSL2 topology; production Linux distribution not qualified |
| B5-L02 | INT-01 profile reconstructed, not replayed | The 32-case matrix uses rebuilt closures and a new profile; historical B4 profile identity not re-verified |
| B5-L03 | `IntegratedBarrierTests` / `DomainAuthorityTests` largely skipped | Harness limitation (probes `/sys/fs/cgroup` root); these skips are not passes |
| B5-L04 | `B3RestrictedLifecycleTests` install closure skipped | `artifacts/b2/install` absent; native installation path not re-qualified |
| B5-L05 | `DirectAuthenticationTests` require explicit profile | Not enabled in this qualification run |
| B5-L06 | Carried findings unchanged | B4Q1-F02 (fs identity race), B4Q1-F03 (drain thread), B4Q1-F04 (store-failure hook), Q1R-08 (D5_O03 flake), R1-F02 (global native stop) remain bounded residuals |
| B5-L07 | No independent native reproduction executed | Review relies on producer evidence + inspection; native cases marked inspection-only |

---

## L. Final Verdict and Recommended Owner Action

### Verdict: SUPPORTED WITH QUALIFICATIONS

**Rationale:** The B5 implementation correctly implements the accepted C3 durable stop deadline and recovery contract:
- Sealed monotonic clock with pre-action validation ✓
- Authoritative durable deadline (`max(0, deadline - now)`) with caller allowance ignored ✓
- Policy A: zero renewed wait, exactly one kill + one immediate observation on expiry ✓
- Typed outcomes correctly mapped; `Stopped` requires `Accepted + Empty` ✓
- Journal progress/finalization invariants preserved; uncertainty never fabricates success/failure ✓
- No-record path separated; no release authority recreated ✓
- C1/C2/C4/INT-01/B4 architecture preserved ✓
- Failing-before regression (200 fresh polls → 0 polls) permanently fixed ✓

**Qualifications (bounded, documented):**
1. Cross-process epoch compatibility limited to same basis + compatible boot context (honestly documented, not a defect)
2. WSL2 native qualification only; independent Linux/macOS host not qualified
3. INT-01 profile reconstructed from preserved assets, not historical replay
4. Several native test classes skip due to pre-existing harness limitations
5. Carried findings (B4Q1-F02/F03/F04, Q1R-08, R1-F02) remain open for separate remediation

### Recommended Owner Action

1. **Accept B5 as implemented** with the stated qualifications
2. **Do not authorize** production activation, B6/B7, Hufu integration, package publication, or native Linux/macOS qualification based on this review
3. **Track qualifications B5-L01 through B5-L07** in the work queue for future qualification slices
4. **Preserve all producer evidence, original decisions, and historical material** (already committed)
5. **Proceed to independent review of next slice** per the authorized roadmap

---

**Review completed:** 2026-11-01  
**Reviewer:** Independent security review agent  
**Evidence location:** `docs/reviews/rc2b-05-b5-q1-independent-security-review.md` (this document)  
**Producer evidence preserved:** `docs/evidence/rc2b-05-b5-impl-01.md`, `docs/evidence/rc2b-05-b5-impl-01-sha256-manifest.txt`, `docs/evidence/rc2b-05-b5-impl-01/native-summary.json`, `artifacts/rc2b-05-b5-impl-01/`