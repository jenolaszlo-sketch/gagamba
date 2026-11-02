# REGRESSION-RESULTS.md — Independent Debug and Release test runs, skips, assertion preservation

Environment: Windows 11 10.0.26200 x64; .NET SDK 10.0.401; runtime 10.0.12; xUnit v3 3.2.0 / Microsoft.NET.Test.Sdk 18.0.1 / VS runner 3.1.4 (from project files). All runs executed by the reviewer on `src-remediated` with TRX logging; TRX retained under `work\trx-f1`, `work\trx-f2`, `src-remediated\work-artifacts\supplemental\trx`.

## Commands

- `dotnet test tests/<Project>.csproj -c Debug --nologo --logger "trx;LogFileName=<name>.trx" --results-directory <review-work>\trx-f1`
- Release runners: same with `-c Release` (results-directory `trx-f2`).
- Supplemental actual-Release-M1 suite: `powershell -File src-remediated\eng\test-production-regressions.ps1 -EvidenceDirectory <inside-tree>` (reviewer-run; see OBSERVATION Q2-04 about the evidence-directory coupling).

## Results (independently executed)

| Suite | Passed | Failed | Skipped | Discovered | Exit | Evidence |
|---|---:|---:|---:|---:|---|---|
| Gagamba.Execution.Linux.M1.Tests (Debug) | 1,050 | 0 | 269 | 1,319 | 0 | f-m1-debug.log + TRX |
| Gagamba.Execution.Tests (Debug) | 68 | 0 | 0 | 68 | 0 | f-execution-debug.log |
| Gagamba.Conformance.Tests (Debug) | 6 | 0 | 0 | 6 | 0 | f-conformance-debug.log |
| Gagamba.Runtime.Tests (Debug) | 15 | 0 | 0 | 15 | 0 | f-runtime-debug.log |
| Gagamba.Execution.Linux.Tests (Debug) | 26 | 0 | 16 | 42 | 0 | f-linux-debug.log |
| Gagamba.Execution.MacOS.Tests (Debug) | 47 | 0 | 4 | 51 | 0 | f-macos-debug.log |
| Gagamba.Execution.Windows.Tests (Debug) | 30 | 0 | 0 | 30 | 0 | f-windows-debug.log |
| D6_4.Tests (Debug) | 3 | 0 | 0 | 3 | 0 | f-d64-debug.log |
| **Debug total** | **1,245** | **0** | **289** | **1,534** | | |
| M1.Tests (Release runner / Debug M1) | 1,050 | 0 | 269 | 1,319 | 0 | f-m1-release-runner.log |
| D6_4.Tests (Release runner / Debug TestFixture) | 3 | 0 | 0 | 3 | 0 | f-d64-release-runner.log |
| Supplemental actual Release M1 | 1,019 | 0 | 199 | 1,218 | 0 | f-supplemental.log |

Producer-reported counts (1,245/0/289; 1,050/0/269; 3/0/0; 1,019/0/199) are **independently reproduced exactly**.

## Skip classification (from TRX, not categories guessed)

**M1 Debug — 269 NotExecuted, all Linux-native prerequisites:** linux only ×103; requires Linux with openatN ×23; requires delegated Linux cgroup vN with openatN ×21; requires delegated Linux cgroup vN ×21; requires Linux mount namespace semantics ×20; Linux direct socket primitive qualification required ×19; requires Linux ×12; requires Linux SOCK_SEQPACKET ×8; requires Linux openatN ×8; Linux capture pipes required ×7; requires Linux SOCK_SEQPACKET and SCM_RIGHTS ×6; Linux independent lifetime monitor required ×4; requires Linux pread (pinned native filter verifier) ×4; unprivileged Linux socketpair ×3; Linux retained directory handles ×3; requires Linux pidfd and waitid(P_PIDFD) ×3; unprivileged Linux pidfd ×2; Linux listener ×1; Linux pidfds ×1. **Total 269.**

**Linux Debug — 16:** Linux native test ×14; requires delegated Linux cgroup-vN subtree ×2.
**macOS Debug — 4:** Unix helper shim ×3; Unix permissions ×1.
**Supplemental — 199:** same environmental classes (269 − 70 hook-file skips).

No test-condition, no new, and no reclassified skips. Native cases are **skips, not passes**. Nothing was disabled to make the solution green.

## Comparison with the original Q1 inventory

Q1 (independent, at `1cbe4df`): 1,245 passed / 289 skipped across the same eight Debug projects; producer S1's 366/160 and B5-era 1050/269 remain historical separate observations. Current remediated inventory: **identical totals and identical per-project counts**; all material differences from S1-era numbers are the pre-existing environmental/accounting differences already explained in Q1 (FINDING-003 lineage), not introduced by REM-01.

## Supplemental-suite delta accounting (hook-independent assertions against actual Release M1)

Per-class TRX counts of the five explicitly excluded files in the full suite: NonRootQualificationTests 33 (0 pass / 33 skip… net 31 pass + 70 skip across all five), NativeCopyInTests 23, CopyInTests 20, IntegratedBarrierTests 15, BindingTests 10 → **101 cases = 31 passes + 70 environmental skips**; 1,319 − 101 = 1,218 discovered; 1,050 − 31 = 1,019 passed; 269 − 70 = 199 skipped. Zero unexpected added/changed outcomes. The exclusion list is recorded in the suite's own evidence output (`instrumented-only-sources.txt`).

## Safety-behavior coverage preserved

All 61 B5 clock/deadline/authority cases: **61/61 passed** in M1 Debug, **61/61** in the M1 Release runner, **61/61** in the supplemental actual-Release-M1 suite (TRX class-name filter `B5`). HELD, C3 deadline, controller-loss, journal durability, ambiguous outcomes, staging cleanup, protected capture and reconciliation assertions execute within these suites. D6.4's three specialized assertions pass in both runner configurations. No newly disabled test disappeared from the inventory.

## Inherited qualification (explicitly not remediated)

The internal `Reconciler(IDomainClock, TimeSpan)` clock-constructor seam remains in production Release (verified in binary metadata and source). Clock-constructor qualification is **NOT VERIFIED** as closed and is out of this focused review's remediation scope; it is not treated as a pass.
