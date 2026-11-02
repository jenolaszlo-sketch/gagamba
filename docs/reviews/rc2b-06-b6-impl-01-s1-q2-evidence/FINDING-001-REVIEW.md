# FINDING-001-REVIEW.md — Full Release solution failure, remediation, test preservation

Q1-FINDING-001 (MAJOR): full-solution Release build failed because test projects referenced qualification-only types excluded by `GAGAMBA_PRODUCTION` in Release assemblies.

## 1. Failing-before reproduction (independent)

- Source: reviewer-materialized copy of `1cbe4dfe9c4d19aba8708079b2f617b2ddafcbc0` (`src-original/`, via `git archive`; no worktree/branch).
- Environment: .NET SDK 10.0.401; MSBuild 18.9.11.42413; runtime 10.0.12; Windows 11 10.0.26200 x64.
- Command: `dotnet build src-original\Gagamba.sln -c Release --nologo`
- Log: `evidence/a1-solution-release-before.log` — **exit 1, "Build FAILED.", 25 errors / 52 warnings.**

Unique errors (each logged twice by MSBuild; summary reports 25):

| Count | Code | Project / file |
|---|---|---|
| 11 | CS0246 `CopyTestHooks` not found | M1.Tests Preparation\BindingTests.cs (×5), CopyInTests.cs (×3), NativeCopyInTests.cs (×3) |
| 2 | CS7036 missing `Hooks` parameter of `CopyPlan..ctor` | IntegratedBarrierTests.cs(134), NonRootQualificationTests.cs(430) |
| 7 | CS1729 `Fixture` ctor 3-arg mismatch | IntegratedBarrierTests.cs (7 sites) |
| 4 | CS1744 named `pidFdProvider` positional conflict | NonRootQualificationTests.cs (4 sites) |
| 1 | CS1503 `FailingPidFdProvider` → `byte` conversion | NonRootQualificationTests.cs(1226) |

All 25 errors are in `Gagamba.Execution.Linux.M1.Tests`; no other project failed. Effective reference configuration at failure: `Directory.Build.props` (original, line 22) appended `GAGAMBA_PRODUCTION` for **every** project in Release, so the M1 project referenced by M1.Tests compiled without the qualification-only types the test source requires.

**Agreement with Q1-FINDING-001: exact** (same project, same error classes and counts, same root cause).

## 2. Passing-after verification (independent)

- Source: reviewer-materialized copy of `66896754de2610b1610c665f73d3cc0f88675641` (`src-remediated/`).
- Clean in-place build (fresh bin/obj): `dotnet build src-remediated\Gagamba.sln -c Release --nologo` → **exit 0, 0 errors**, `evidence/b1-solution-release-after.log`. (58 warnings, all `Microsoft.SourceLink.Common.targets` "Source control information is not available" — environmental: the materialized copy has no `.git`; the canonical checkout's own builds do not show these. Not a build-boundary defect.)
- Repeated as parallel `-m` with fresh `--artifacts-path` (`evidence/e5-solution-release-parallel-fresh.log`): exit 0.
- Every solution project produced output, including the previously failing project:
  `Gagamba.Execution.Linux.M1.Tests -> …\bin\Release\net10.0\Gagamba.Execution.Linux.M1.Tests.dll`.
- Post-copy verification gates inside the same build: production closures 3× `0/4 hook types and 0/3 hook properties` PASS; instrumented closures 2× `4/4 + 3/3` PASS.
- Success is **not** explained by project removal (no project removed; producer diff touches no `.sln`) nor by test suppression (see §4).

## 3. Project-reference variant resolution

- `M1.Tests` references M1, Execution and Owner with `AdditionalProperties="Configuration=Debug"`; within the Release solution build, M1 and Owner additionally compiled in Debug (validation messages for `Gagamba.Execution.Linux.M1, Debug` and `Owner, Debug` in `b1` log) and the test output directory carries the Debug M1:
  - `src\Gagamba.Execution.Linux.M1\bin\Debug\…M1.dll` SHA-256 `8DA0A01CE0620EA0…`
  - `tests\Gagamba.Execution.Linux.M1.Tests\bin\Release\…M1.dll` SHA-256 `8DA0A01CE0620EA0…` — **byte-identical** (reviewer probe `d5`, same MVID `1ce2cd83-30bd-4b2c-95af-3309a6a8a17a`).
- Owner/recovery propagate `Configuration=$(Configuration)` on every dependency edge (`AdditionalProperties`); D6.4.Tests pins `Configuration=Debug` plus fixture identity and isolated `d6-4-variant` roots.
- The solution did not substitute a production binary into an instrumented closure nor vice versa (hash comparisons in BINARY-INSPECTION.md §3).

## 4. Test-preservation challenge

- **Source identity:** `git diff --name-only 1cbe4df..dce71ac -- '*.cs'` → only `eng/VerifyProductionAssembly/Program.cs` (new tool). No test source file changed; no assertion edited; no test removed; no skip added. `git diff --name-only … -- src/ tests/ native/` → 5 csproj files only.
- **Discovery parity:** current Debug TRX inventory reproduces Q1's published totals exactly — 1,534 discovered, 1,245 passed, 0 failed, 289 skipped across the same eight projects (REGRESSION-RESULTS.md). M1.Tests: 1,319 discovered in both Debug and Release runner.
- **Supplemental-suite accounting (adversarial check for hidden suppression):** the additional actual-Release-M1 compilation explicitly omits five hook-dependent files. Independent TRX per-class counts: NonRootQualificationTests 33, NativeCopyInTests 23, CopyInTests 20, IntegratedBarrierTests 15, BindingTests 10 → **101 cases = 31 passed + 70 environmental skipped**, exactly the delta between the full suite (1,319 = 1,050+269) and the supplemental suite (1,218 = 1,019+199). Every omitted assertion still executes or reports its original environmental skip in the full suites. `eng/test-production-regressions.ps1` records the exclusion list in its evidence output.
- **Skip parity:** all 289 Debug skips are pre-existing environmental classes (Linux/openat2/cgroup/pidfd/socket/mount/lifetime/Unix shim), disjoint and counted from TRX; zero new skips; nothing was reclassified as passed.

## 5. Gate B required-outcome checklist

1. Full-solution Release succeeds — ✔ (in-place and parallel-fresh roots).
2. Previously failing test projects compile — ✔ (M1.Tests Release output produced).
3. Qualification-only types absent from production assemblies — ✔ (independent probe, BINARY-INSPECTION.md).
4. Test projects use instrumented configuration where needed — ✔ (explicit Debug references; Debug binaries verified present and hook-complete).
5. Project references resolve intended variants — ✔ (byte-identical closure copies).
6. Tests not silently excluded — ✔ (101-case delta fully accounted; no discovery loss).
7. No weakened assertion — ✔ (zero test source changes; counts and outcomes match Q1).
8. Existing fixture configurations supported — ✔ (D6.4 Debug 3/3 and Release runner 3/3; d6-4-variant isolation intact).
9. Outputs not shared incorrectly between variants — ✔ (separate bin/obj per configuration; closure hash checks).
10. No new packaging/publishing behavior — ✔ (props diff touches only the symbol scoping; targets add verification only).

**Gate B outcome: Q1-FINDING-001 CLOSED.** The full solution builds and the original test intent is preserved with no suppression.
