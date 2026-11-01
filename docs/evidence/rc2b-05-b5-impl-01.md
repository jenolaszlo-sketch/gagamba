# RC2B-05 / B5 — Sealed C3 Deadline and Recovery Reconciliation

**Milestone:** `RC2B-05 / B5`
**Authorization:** GAGAMBA-RC2B-05-B5-IMPL-01 (experimental B5 implementation only)
**Disposition:** `IMPLEMENTED WITH QUALIFICATIONS — REVIEW REQUIRED`
**Date:** 2026-11-01

This is producer evidence. It is **not** independent review and **not** owner
acceptance. B5 makes the durable stop deadline authoritative in the existing
Linux M1 integrated recovery architecture. It does not authorize production
activation, B6, B7, Hufu integration, VM/HCS work, package publication, or a
claim of native Linux-host/macOS qualification.

Raw evidence is retained outside the tracked tree under
`artifacts/rc2b-05-b5-impl-01/` (git-ignored) with
[`SHA256SUMS.txt`](rc2b-05-b5-impl-01-sha256-manifest.txt), 108 files,
manifest SHA-256 `476809507ab458c59670d439f437d1d9606528eab8f3db8f14bba466638c0dd2`.
The compact case summary is committed at
[`native-summary.json`](rc2b-05-b5-impl-01/native-summary.json).

---

## A. Exact source binding

| Item | Value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba` / `main` (canonical checkout) |
| Starting commit | `e7818b5a876c220cb4f803b6cc1f9c55027d3c66` |
| Starting tree | `d035ae70ba1b3f95dd5800dd16815c8485b2e112` |
| Implementation commit | `0bf045d0ec3e4cb560edb2bc3b4e7065efaa7b9c` |
| Implementation tree | `80d138f81c82b61243e23c0865eb6a1ca516cc54` |
| Implementation subject | `feat(gagamba): implement sealed C3 deadline reconciliation (B5)` |
| Windows | Windows 11 x64 build 26200; .NET SDK 10.0.401; runtime 10.0.12 |
| WSL2 | Ubuntu 26.04.1; kernel `6.6.114.1-microsoft-standard-WSL2`; SDK 10.0.401; runtime 10.0.14 |
| WSL2 user | `gaga` uid 1000, `CapInh/CapPrm/CapEff/CapAmb = 0` after `setpriv --inh-caps=-all` |
| WSL2 delegation | transient user unit, `Delegate=memory pids`, `DelegateSubgroup=host` |

`main` was at the authorized starting commit with a clean tracked tree. Many
historical linked worktrees exist for earlier accepted stages; no new branch or
worktree was created for B5, and the canonical checkout was the only
implementation tree. Untracked material was inventoried before editing and is
unchanged by this work (test `TestResults/` directories and historical `spikes/`
artifacts remain untracked).

### Changed files (implementation commit)

Production:

- `src/Gagamba.Execution.Linux.M1/Lifecycle/MonotonicClock.cs` (new)
- `src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/RestrictedDomain.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs`
- `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs`
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedRecoveryHost.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/OwnerSafetyMonitor.cs`
- `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs`
- `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs`
- `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`
- `src/Gagamba.Execution/Restricted/RestrictedManifest.cs`

Tests/qualification:

- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3ClockTrustTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3DeadlineTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3TypedOutcomeTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3JournalTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3AuthorityTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3HistoricalFixtureTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05B5C3FailingBeforeTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryTestSupport.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/NonRootQualificationTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/IntegratedBarrierTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/OwnerDeathQualificationTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`
- `tests/Gagamba.Execution.Linux.M1.D6_4.Tests/Gagamba.Execution.Linux.M1.D6_4.Tests.csproj` (new)
- `tests/Gagamba.Execution.Linux.M1.D6_4.Tests/Wp05B5C3NotAttemptedFallbackTests.cs` (new)

No durable journal schema, public restricted-execution API, release authority
or recovery privilege was added or widened.

---

## B. Contract traceability

| Requirement | Implementation | Evidence |
|---|---|---|
| C3-01 sealed monotonic clock | `MonotonicClock.cs`: `MonotonicEpoch`, `MonotonicClockBasis`, `MonotonicTime`, sealed `StopwatchDomainClock`; `Reconciler` production ctor takes the sealed concrete type | `Wp05B5C3ClockTrustTests` (T1) |
| C3-01 negative clock cases | `TryResolveStopBudget` checks epoch, `Stopwatch.Frequency`, positive timestamp, checked arithmetic; refusal before destructive action | T1: unknown epoch, frequency mismatch, construction-relative clock, unavailable clock, nonpositive timestamp, invalid provenance, overflow |
| C3-02 authoritative deadline | `remaining = max(0, deadline - now)` in `TryResolveStopBudget`; caller allowance ignored whenever `record.Stop` is valid | T2 + failing-before regression |
| C3-03 Policy A | zero-budget `StopAndAwaitEmpty(long, clock)` performs kill then one immediate observation, no `Delay` | T3; native `NonRootExpiredDurableStopDeadlineAppliesPolicyAWithoutRenewedWait` |
| C3-04 typed outcomes | `KillDispatchOutcome`, `StopPopulationObservation`, typed `StopOutcome`; `Stopped` derived | T4; R1-T1–T5; D6_4 R1-T13–T15 |
| C3-05 component/aggregate mapping | `Reconciler` typed mapping and `JournalFinalization` precedence | T4/T5/T7 truth-table tests |
| C3-06 no valid StopRecord | caller allowance retained only for no-record cleanup; no synthesis, no release resend; conservative refusal on invalid record | T6 authority tests |
| C3-07 durable/lifecycle correctness | immutable `StopRecord`; same absolute deadline across repeats/new processes/partial cleanup; cross-boot refusal; identity-checked cleanup | T2/T6/T7 + native repeat |
| R1-T13–T15 fixture isolation | D6.4 fixed marker and Reconciler seam under `GAGAMBA_WP05_D6_4_TEST_FIXTURE`; dedicated same-project variant project | `Wp05B5C3NotAttemptedFallbackTests` 3/3 |
| B4 cleanup preservation | unchanged `eng/int01_cleanup.py`; 32-case matrix reclassified | 20 Confirmed / 12 Unknown; both named cases Unknown |
| Compatibility | C1/C2/C4/INT-01/B4 regressions | full Windows solution + WSL2 components |

---

## C. Clock qualification

### Basis and conversion

`StopwatchDomainClock` is `internal sealed`, has a private constructor and one
static trusted instance, and exposes:

- `Basis = (MonotonicEpoch.Qpc, Stopwatch.Frequency)`;
- `NowNanoseconds = MonotonicTime.NowNanoseconds()`, using the exact accepted
  `OwnerClock` conversion
  `(t / F * 1_000_000_000) + (t % F * 1_000_000_000 / F)`.

The same helper now backs `OwnerClock`, `LaunchDomainClock`,
`OwnerSafetyMonitor.Now`, `M1RecoveryScope`, `B3Qualification` and the sealed
domain clock, so there is one conversion and one epoch. The construction-relative
`OwnerDomainClock` (`Stopwatch.StartNew()` elapsed ticks) was dead and was
removed; it is never used for deadline arithmetic.

### Trust boundary

- Production composition (`B3Qualification`, `IntegratedRecoveryHost`) uses
  `Reconciler(StopwatchDomainClock, TimeSpan)`.
- The open `IDomainClock` constructor remains only as the deterministic test
  seam. A test clock cannot acquire production trust from an enum label because
  the production constructor does not accept the interface.
- Before any subtraction the Reconciler checks the declared epoch, the exact
  `Stopwatch.Frequency`, a positive current timestamp and checked arithmetic.
- On refusal it returns `QuarantinedUnknown`, `DestructiveActionTaken = false`,
  and performs no kill/removal. No fresh deadline is fabricated.

### Deterministic results

`Wp05B5C3ClockTrustTests` passed 14/14 (theory cases included):

- sealed production type, no public constructor, shared conversion values;
- production reading lies between two `MonotonicTime.NowNanoseconds()` readings
  (therefore not rebased to object construction);
- compatible Qpc basis consumes the persisted deadline (40 × 25 ms for the
  remaining 1 s);
- unknown epoch, construction-relative clock, frequency mismatch, unavailable
  clock, zero/negative timestamp, invalid provenance, overflow and cross-boot
  are all refused before destructive action.

No cross-platform clock compatibility is claimed. WSL2 is not an independent
native Linux host.

---

## D. Deadline qualification

For a valid `StopRecord`:

```text
remaining = max(0, StopDeadlineNanoseconds - trustedNowNanoseconds)
```

with checked arithmetic and persisted-provenance validation
(`accepted > 0`, `deadline = accepted + 5_000_000_000`, defined cause). The
record is never rewritten. The caller's `TimeSpan` is used only when no valid
StopRecord exists.

### Failing-before (preserved)

`Wp05B5C3FailingBeforeTests.CallerAllowanceCannotOverrideExpiredPersistedStopDeadline`
was run against `e7818b5`. A stop accepted at 1 s (deadline 6 s) was recovered
at 100 s with a populated domain. The pre-B5 reconciler performed **200**
25 ms polls (a fresh five seconds) and returned `Failed`; the test expected
`0` polls and `QuarantinedUnknown`. Log: `artifacts/rc2b-05-b5-impl-01/failing-before.log`.
The former D2 probe
`Wp05ReleaseContractProbes.ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired`
was replaced (as the C3 record requires) by
`ReconcilerConsumesPersistedStopDeadlineAndAppliesPolicyAWhenExpired`.

### Passing-after

| Case | Result |
|---|---|
| Future deadline, 1 s remaining, caller 5 s | exactly 40 polls (1 s), not 200 |
| Future deadline, 100 ms remaining | exactly 4 polls |
| Caller allowance one hour | still 20 polls (500 ms remaining) |
| Exact expiry | 0 polls, 1 kill, 1 observation |
| Expired by 360 s | 0 polls, 1 kill, 1 observation |
| Repeated reconciliation at deadline−2 s then deadline−1 s | 80 then 40 polls; absolute deadline unchanged |
| Partial cleanup (stop+empty already durable) | no kill, no poll; StopRecord unchanged |
| New same-boot native process (WSL2) | original deadline retained across two reconciliations |

`ValidStopRecordIsNeverRewritten` asserts the durable record is byte-for-byte
the same `StopRecord` after reconciliation.

---

## E. Typed outcomes

`KillDispatchOutcome ∈ {NotAttempted, Failed, Accepted, Unknown}` and
`StopPopulationObservation ∈ {NotObserved, Unknown, Populated, Empty}`.
`StopOutcome.Stopped` is true only for `Accepted + Empty`. Diagnostic `Error`
text is never parsed.

| Condition | DomainStop | DomainEmpty | Test result |
|---|---|---|---|
| Kill accepted, empty observed | Confirmed | Confirmed | T4 `AcceptedKillAndEmptyObservation...`; R1-T5; exact bits 0x0B, Cleanup Confirmed |
| Kill accepted, still populated | Unknown | Unknown | T4, R1-T2; QuarantinedUnknown |
| Kill accepted, observation unavailable | Unknown | Unknown | T4, R1-T4; exactly one read |
| Kill definitively failed | Failed | Unknown (or Confirmed from an independent empty observation) | T4 both variants; aggregate Failed |
| Kill dispatch unknown | Unknown | Unknown | T4, R1-T3; aggregate QuarantinedUnknown |
| Kill not attempted, trusted fallback empty | Unknown | Confirmed | D6_4 R1-T13; 0 kill writes, 1 fallback read, durable `0x08 → 0x0A` |
| Kill not attempted, fallback populated | Unknown | Unknown | D6_4 R1-T14; 0 kill writes, 1 read, durable `0x08` |
| Kill not attempted, fallback unavailable | Unknown | Unknown | D6_4 R1-T15; 0 kill writes, 1 read, durable `0x08` |

Aggregate precedence: any required component `Failed` → `Failed`; otherwise any
required component or `JournalFinalization` `Unknown` → `QuarantinedUnknown`;
otherwise `CleanupConfirmed`. A persistence failure alone never becomes an
operation failure; a component failure is retained even when finalization is
also uncertain.

---

## F. Journal behavior

`JournalFinalization` is a classification row, not a new durable field.

| Scenario | JournalFinalization | Aggregate | Result |
|---|---|---|---|
| All operations confirmed, final write succeeds | Confirmed | CleanupConfirmed | T5/R1-T5 |
| Stop-progress write fails persistently | Unknown | QuarantinedUnknown (not Failed) | T5/R1-T6 |
| Final write fails after confirmed empty | Unknown | QuarantinedUnknown (not Failed/Confirmed) | T5/R1-T7 |
| Earlier progress write fails, later final write succeeds | Unknown | QuarantinedUnknown; durable verdict remains not Confirmed | T5 `EarlierProgressFailure...` |
| Staging operation fails + final write fails | Unknown | Failed (operation failure retained) | T5/R1-T11 |
| No writer lease | Unknown | QuarantinedUnknown; never CleanupConfirmed | T5 |

No fabricated `CompletedCleanup` bits are added for unobserved work. A
successfully persisted `Unknown` verdict is not treated as Confirmed cleanup.

---

## G. Native qualification (WSL2)

The bounded WSL2 run used the accepted integrated recovery topology and a
profile reconstructed from the preserved B4 fixture assets plus the B5
Owner/Recovery closures. Preflight was **12/12**.

| Check | Result |
|---|---|
| B5 deterministic tests on Linux | **61 passed / 0 failed / 0 skipped** |
| D6.4 R1-T13–T15 variant | **3/3** |
| `NonRootQualificationTests` (real cgroups/processes/journal, includes two new B5 deadline cases) | **33/33** |
| Full INT-01 matrix | **32 passed / 0 failed / 32 requested** |
| Cleanup classification | **20 Confirmed / 12 Unknown**; `missing-domain-ack` and `missing-staging-ack` remain Unknown |
| `OwnerDeathQualificationTests` | 7/7 |
| `B3RestrictedLifecycleTests` | 8/8, 2 skips (B2 installation closure absent) |
| `Wp05ReleasePathIndependentQualificationTests` | 3/3 |
| `Wp05TerminationObserverTests` | 3/3 |
| `Wp05SigpipeDispositionTests` | 1/1 |
| `RecoveryHarness` | 3/3, 2 environment skips |
| `DomainAuthorityTests` | 2/2, 19 skips (class probes `/sys/fs/cgroup` root) |
| `IntegratedBarrierTests` | 1/1, 14 skips (same root-probe limitation) |
| `DirectAuthenticationTests` | 1/1, 20 skips (explicit `GAGAMBA_INT01_NATIVE_PRIMITIVES` profile and controller artifact not enabled) |

The two new native B5 cases (`NonRootExpiredDurableStopDeadlineAppliesPolicyAWithoutRenewedWait`,
`NonRootSameBootRecoveryKeepsTheOriginalDeadlineBeforeExpiry`) plant a real
`StopRecord` in the real journal for a retained delegated-cgroup domain, kill the
real owner, and reconcile with the sealed clock. The expired case completes in
under two seconds (no fresh five-second wait), keeps the same absolute deadline
across a repeat, and does not report `Failed`; the before-expiry case keeps the
same deadline and does not fail.

The full matrix includes `integrated-original-deadline`,
`integrated-owner-live-takeover-refusal`,
`integrated-post-exit-writer-fence`,
`simultaneous-controller-recovery-death`,
`actual-recovery-release-impersonation`, the `missing-*` acknowledgement cases,
and the remaining accepted fault cases. All 32 passed. The profile and closure
hashes are in `artifacts/rc2b-05-b5-impl-01/binding.json` and
`committed/profile-binding.json`.

### Explicitly not executed / not qualified

- The original accepted B4 private profile was not present in the canonical
  checkout; a new profile was reconstructed from the preserved B4 fixture assets
  and rebuilt B5 closures. This is native topology evidence, not a replay of the
  historical B4 profile.
- `IntegratedBarrierTests` and `DomainAuthorityTests` still skip most cases
  because their `Usable()` probe creates a child directly under
  `/sys/fs/cgroup` rather than the delegated parent. This is a pre-existing
  harness limitation; those skips are **not** passes.
- `B3RestrictedLifecycleTests` native install-closure cases are skipped because
  `artifacts/b2/install` is absent.
- `DirectAuthenticationTests` cases requiring the explicit INT-01 primitive
  profile/controller artifact are skipped.
- The B4-specific confined matrix was not re-run; B4 behavior is covered by the
  INT-01 full matrix, the native component classes and the deterministic
  regressions.
- The earlier full regular-phase WSL2 suite was also run
  (`1149 passed / 0 failed / 5 skipped`); the unit reached its
  `RuntimeMaxSec` after the test result because a child process lingered, so the
  result is reported separately from the process-global class runs.

---

## H. Regression preservation

| Regression scope | Result |
|---|---|
| Windows solution build | 0 warnings / 0 errors |
| Windows M1 assembly | 1050 passed / 0 failed / 269 skipped |
| Execution.Tests | 68/68 |
| Runtime.Tests | 15/15 |
| Conformance.Tests | 6/6 |
| Execution.Linux.Tests | 26/26, 16 platform skips |
| Execution.MacOS.Tests | 47/47, 4 platform skips |
| Execution.Windows.Tests | 30/30 |
| Fixture.SelfTest | 15/15 |
| Gw1bLaunch.Tests | 2/2 |
| B5 focused (Windows) | 61/61 |
| D6.4 variant (Windows) | 3/3 |
| `eng/tests` classifier suite | 35/35 |
| INT-01 cleanup interpretation | 20 Confirmed / 12 Unknown, zero flips |
| C1/C2/C4 behavior | unchanged deterministic suites |
| Option A / at-most-one RELEASE / recovery without release authority | unchanged and asserted |

No acceptance expectation was weakened. The only intentional replacement is the
former D2 probe that asserted the C3 violation; it now asserts the corrected
behaviour.

### Production purity of the D6.4 variant

- Production `Gagamba.Execution.Linux.M1.dll` contains no
  `NotAttemptedStopTestFixture`, no `GAGAMBA_WP05_D6_4_TEST_FIXTURE` symbol, and
  no `D6_4.Tests` friend grant.
- The variant assembly contains the fixed marker and grants internals only to
  `Gagamba.Execution.Linux.M1.D6_4.Tests`.
- `Gagamba.Execution.dll` carries the two additional compile-time friend names
  required by the accepted design; it contains no fixture symbol and no
  runtime fixture code.
- Production and variant output/intermediate paths are disjoint; the variant is
  non-packable and excluded from the solution/Owner graph.
- The variant is **not** binary-identical evidence for production.

---

## I. Security boundary

Not demonstrated or claimed:

- production activation, restricted execution on any platform, or a public API
  change;
- B6 installed-artifact attestation, B7 comprehensive lifecycle qualification;
- Hufu integration, VM/HCS, package/NuGet release or deployment;
- native Linux distribution or macOS execution; WSL2 is not an independent
  native Linux host;
- cross-platform clock comparability merely because two systems expose
  `Stopwatch`;
- binary reproducibility or production-binary identity for the D6.4 variant;
- independent security review (this document is producer evidence only).

Carried findings are unchanged and not silently closed: **B4Q1-F02** filesystem
identity check/remove race, **B4Q1-F03** broader drain-thread fault handling,
**B4Q1-F04** experimental store-failure-hook isolation, **Q1R-08** historical
D5_O03 flake, **R1-F02** global native stop/runtime enforcement. B5 does not
touch the check/remove path, the drain thread, the store-failure hook or the
release transport. No B5 dependency on them was found; they remain bounded
residuals for separate remediation.

Stable security properties retained: C1 ambiguity, C2 irreversible stop-first
capability consumption, C4 duplicate/concurrent release, R1-F03 Option A, at
most one physical RELEASE, recovery without release authority, existing
journal format/identity validation, B4 capture custody/limits, independent
native stopping, and `RestrictedExecutionRuntime.Create()` remaining
production-unavailable.

---

## J. Independent reviewer handoff

### Reproduce

```powershell
# Windows
dotnet build Gagamba.sln -c Release
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build `
  --filter "FullyQualifiedName~Wp05B5C3"
dotnet test tests/Gagamba.Execution.Linux.M1.D6_4.Tests/Gagamba.Execution.Linux.M1.D6_4.Tests.csproj -c Release
dotnet test Gagamba.sln -c Release --no-build
python -m unittest discover -s eng/tests
```

```bash
# WSL2 (exact bound command)
cd /home/gaga/gagamba-b5-impl-01
systemd-run --user --unit=gagamba-b5-committed \
  --property="Delegate=memory pids" --property=DelegateSubgroup=host \
  --property=ExitType=cgroup --property=KillMode=control-group \
  --property=TimeoutStopSec=10 --property=RuntimeMaxSec=1800 \
  --collect --wait --pipe bash /mnt/c/.../run-committed-evidence.sh
```

### Artifact hashes

- External evidence manifest: `artifacts/rc2b-05-b5-impl-01/SHA256SUMS.txt`,
  108 files, SHA-256 `e54b4ef04029f1ffbcd4e034f53fad8dc725e925b4de89ee7eb58be5ecb9c9f4`.
- Owner closure `Gagamba.Execution.Linux.M1.Owner.dll`:
  `28cdf2c1b62f7bb64ed06f31f838a4c4e02d7e7907d356884b7d72cf66ed5e7b`.
- Recovery closure `Gagamba.Execution.Linux.M1.Recovery.dll`: see
  `committed/profile-binding.json`.
- WSL runtime: `10.0.14`, inventories bound in `profile-binding.json`.
- Failing-before log: `failing-before.log`.

### Adversarial questions for the reviewer

1. Is the clock genuinely sealed (concrete type + private construction + fixed
   basis), or can any production path pass a foreign `IDomainClock` with a
   `Qpc` label?
2. Can any recovery/retry path reset or extend the original persisted deadline,
   including new process, repeated reconciliation, partial cleanup or owner
   death?
3. Does Policy A genuinely perform one immediate kill+observation with no
   additional wait, and does it still perform the final observation when the
   deadline is exactly reached?
4. Can invalid clock evidence (unknown epoch, wrong frequency, unavailable,
   nonpositive, overflow, construction-relative) reach destructive cleanup?
5. Are definitive operation failures distinguished from observation uncertainty
   in every typed combination?
6. Can a persistence failure produce `CleanupConfirmed` or hide a known
   operation failure?
7. Can recovery recreate release authority, resend RELEASE, or create a second
   physical RELEASE?
8. Can absence of a StopRecord synthesize a deadline or cleanup authority?
9. Did B4 capture/native-stop behavior, INT-01 acceptance or the 20/12 cleanup
   interpretation regress?
10. Can the test-only D6.4 seam or its friend grant be reached from production
    composition or packaged output?

**Do not treat this producer document as independent review or owner
acceptance.** The implementation agent did not perform the independent review.
