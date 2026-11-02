# GAGAMBA-RC2B-06-B6-IMPL-01-S1-Q2 — Independent Remediation Requalification (REPORT.md)

Review date: 2026-11-02 (Windows 11 Home Single Language 10.0.26200, x64; .NET SDK 10.0.401, MSBuild 18.9.11.42413, runtime 10.0.12).
Reviewer: independent Q2 agent (GLM-5.3-Flash via OpenCode). Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba` (read-only during review; no worktree/branch/push).

## Verdict

**S1 SUPPORTED WITH QUALIFICATIONS**

Both Q1 major findings are **independently closed** by the reviewed remediation `66896754de2610b1610c665f73d3cc0f88675641` (tree `62613e8e407eca25c9d0438d041a89d8e9c5b9fa`), with the final documentation receipt `dce71ac2423c2a49a853bef8703018f0220378cb` (tree `becec3f768266be158fb57396f00f328408cd30e`) introducing no runtime change. Bounded, non-blocking qualifications remain (remote tip delta, environmental skips, inherited clock qualification, future-stage S2 obligations). No material qualification defect was established.

**Recommendation: READY FOR S1 OWNER ACCEPTANCE** (with the source-binding qualifications in §7 below; acceptance binds to the receipt `dce71ac` unless the owner explicitly folds the post-receipt commit `401e1ea7` into the accepted scope).

## 1. What was independently established

| Gate | Determination | Key independent evidence |
|---|---|---|
| A. Original failure reproduction | **Reproduced (both findings)** | `evidence/a1-solution-release-before.log` (25 errors, all in M1.Tests), `evidence/a2-before-build.log` + `a2-before-inspection.log` (override succeeded; Release-labeled binary with 4/4 hook types + 3/3 hook properties, SHA `19F77AB4…`) |
| B. Full Release solution | **Supported** | `evidence/b1-solution-release-after.log`: 0 errors; every solution project compiled incl. M1.Tests Release; post-copy gates 0/4+0/3 (production) and 4/4+3/3 (instrumented) all PASS |
| C. Property override refusal | **Supported** | 14+ contradiction probes refused with exact GAGAMBA001–008 diagnostics; `--no-restore` refused; `--no-build` fails closed (MSB1001/NETSDK1085); publish family refuses unsafe input and publishes nothing |
| D. Production/test binaries | **Supported** | Reviewer-owned `probes/Q2Inspect`: M1 Release 0/4+0/3 hooks, no raw marker bytes, 12/12 safety types; Owner/Recovery closures clean; Debug/Test/TestFixture 4/4+3/3; unsafe baseline rejected |
| E. Build isolation | **Supported (with documented cross-root hash variance)** | Full E matrix green; stale production binary in Debug closure repaired+verified; parallel isolated builds produce correct variants |
| F. Regression preservation | **Supported** | Debug inventory 1,245/0/289 (8 projects, exact Q1 parity); M1 Release runner 1,050/0/269; D6.4 3/0/0 (Debug+Release); supplemental actual-Release-M1 1,019/0/199; all 289 skips environmental Linux/Unix prerequisites; all 61 B5 cases pass in all three contexts |
| G. T24 | **Seven obligations dispositioned** (see T24-QUALIFICATION.md) | A/B/D/F VERIFIED; C VERIFIED WITH QUALIFICATIONS; E VERIFIED WITH QUALIFICATIONS; G VERIFIED WITH QUALIFICATIONS |
| H. B1–B5 and scope | **Preserved** | Zero `.cs` runtime/test changes in `1cbe4df..dce71ac`; `RestrictedExecutionRuntime.Create()` still returns `UnavailableRestrictedExecutionProvider`; no S2 scope implementation |
| I. Source/CI identity | **Exact (local) / Remote recorded with delta** | Local identities verified; remote main observed moving during review: `1cbe4df` → `401e1ea7` (contains full remediation chain; one owner docs/harness commit beyond receipt); CI: S1 run failure (41311797002), receipt run 9/10 green (only `preparation` fixture-byte job red), tip run green (41327109151) |
| J. Review evidence | **Complete** | All logs retained under `evidence/`; probes under `probes/`; manifest below |

## 2. Failing-before → passing-after (FINDING-001)

- **Before (original S1 `1cbe4df`, reviewer-materialized source copy):** `dotnet build Gagamba.sln -c Release` → exit 1, 25 errors / 0 fatal warnings, exclusively in `tests\Gagamba.Execution.Linux.M1.Tests`: CS0246 ×11 (`CopyTestHooks`), CS7036 ×2, CS1729 ×7, CS1744 ×4, CS1503 ×1 — matching Q1-FINDING-001 exactly.
- **After (remediated `66896754`):** same command → exit 0, 0 errors; all 24 solution projects produced outputs including `Gagamba.Execution.Linux.M1.Tests -> bin\Release\…M1.Tests.dll`; the Release test closure carries the **Debug instrumented** M1 (byte-identical to `src\Gagamba.Execution.Linux.M1\bin\Debug\…M1.dll`, SHA `8DA0A01C…`).
- **Test preservation:** `git diff 1cbe4df..dce71ac -- '*.cs'` shows **no** runtime or test source change (only the new `eng/VerifyProductionAssembly/Program.cs` tool). TRX inventories reproduce Q1's totals exactly (1,245/0/289 over 1,534 discovered). The supplemental 1,019/0/199 suite accounts for exactly 101 cases (31 passes + 70 environmental skips) from the five explicitly listed hook-dependent files (NonRoot 33, NativeCopyIn 23, CopyIn 20, IntegratedBarrier 15, Binding 10), verified from TRX per-class counts. No assertion was weakened, moved out of coverage, or given a new skip.

## 3. Bypass → refusal (FINDING-002)

- **Before:** `-p:DefineConstants=` Release build of M1 succeeded (exit 0). MSBuild logged `The "DefineConstants" property is a global property, and cannot be modified` (the props append no-ops), the M1 Csc invocation received no `GAGAMBA_PRODUCTION` (only the Execution reference — whose reference build strips the global — did), and the produced Release-labeled binary contained all seven surfaces (4/4 types, 3/3 properties, hook backing fields/accessors, fault strings, IL references). Q1-FINDING-002 reproduced at MSBuild-semantics level.
- **After:** every mutation refused before compilation and (early target) before reference resolution: empty/TRACE-only/unrelated/prefix-junk (`XGAGAMBA_PRODUCTION`)/fixture-only constants → GAGAMBA002; Debug/Test + forced production → GAGAMBA003; Release + fixture carrier/symbol/identity → GAGAMBA004; malformed carrier → GAGAMBA005; fixture symbol without carrier → GAGAMBA006; carrier without fixture identity → GAGAMBA007; unknown configuration → GAGAMBA001. Contradictory builds leave prior valid output bytes untouched (hash-verified). Corrupted/stale outputs are repaired or refused at the next valid build and at publish input/final gates (nothing published on failure). `--no-build` cannot bypass (CLI/SDK-level failure). Direct M1/owner/recovery, solution-level (`-m`), and incremental paths all re-verify actual binary bytes via the post-copy metadata/IL gate.

## 4. Production/test separation (independent, reviewer-owned probe)

Seven accepted surfaces: 4 hook types (`Int01QualificationFault`, `Int01Qualification`, `CopyTestHooks`, `BarrierDeathException`) + 3 hook properties (`OwnerSafetyMonitor::QualificationNativeStop`, `BubblewrapAdapter::QualificationBeforeCaptureStore`, `JournalFileStorage::QualificationBeforeRename`).

| Binary | Config | Hook types | Hook props | Raw markers | 12 safety types | Result |
|---|---|---|---|---|---|---|
| M1 (canonical Release) | Release | 0/4 | 0/3 | none | 12/12 | PASS |
| Owner Release closure | Release | 0/4 | 0/3 | none | n/a (2-type assembly) | PASS |
| Recovery Release closure | Release | 0/4 | 0/3 | none | n/a | PASS |
| M1 Debug | Debug | 4/4 | 3/3 | 6 hook markers | 12/12 | PASS |
| M1.Tests Release closure copy | Debug | 4/4 | 3/3 | 6 hook markers | 12/12 | PASS |
| M1 Test-config (e9) | Test | 4/4 | 3/3 | present | 12/12 | PASS |
| D6.4 TestFixture | Debug | 4/4 | 3/3 | present | 12/12 | PASS |
| Unsafe baseline (original-bypass artifact) | Release | 4/4 | 3/3 | present | 12/12 | **REJECTED (correct)** |

"Source conditionally excluded" and "binary independently verified absent" coincide for all seven surfaces on every production configuration exercised, through direct, solution, owner, recovery, publish and parallel entry points.

## 5. Artifact identities (SHA-256, reviewer environment)

| Artifact | SHA-256 |
|---|---|
| M1 Release (production) | `0FAFB4F7AA4CDCDCF1B7D040CF45ADB99F583BF7A1D1D54FBBDD9ABF993E2CF4` |
| Owner Release | `B1D48349B931E480DFB24FEFBF873280E3985C83356B45B2BD08B76385995E45` |
| Recovery Release | `C0794CEE6A04917CC15BF459FC15643B6C2B4D7BF01588EC4B04455F094436C2` |
| Execution Release | `648652F7C7750A403CFEEFB6188C533E32BD0DC27DF5D2230C0889A0998ED7FA` |
| M1 Debug (instrumented) | `8DA0A01CE0620EA0D75FC74B6D6758B99CB93CDB8D490B6C26755A8F8BB27067` |
| Owner Debug | `7D3CDE0995AA871770DEB5AE1F8ADCB8FDF8C7DD1D31030E5AD863565F22E109` |
| D6.4 TestFixture | `ECBD3385584AD53C99B36DD7104FC6EC35A1A78CEF546EA2426D1724C922DAB9` |
| Unsafe baseline (negative control) | `19F77AB4B169878D70DA0EC21ED171C00630696B3BE68C0DBBB84C177257FF6B` |

Within-root identity is stable across incremental builds; across isolated output roots hashes differ (SourceLink/PDB path inputs) — consistent with the declared boundary and Q1 FINDING-005. These do not equal the producer's hashes (different environment/roots; equality is not claimed by either side).

## 6. Regression summary (independently executed)

| Suite | Passed | Failed | Skipped | Discovered |
|---|---:|---:|---:|---:|
| M1 Debug | 1,050 | 0 | 269 | 1,319 |
| Execution Debug | 68 | 0 | 0 | 68 |
| Conformance Debug | 6 | 0 | 0 | 6 |
| Runtime Debug | 15 | 0 | 0 | 15 |
| Linux Debug | 26 | 0 | 16 | 42 |
| macOS Debug | 47 | 0 | 4 | 51 |
| Windows Debug | 30 | 0 | 0 | 30 |
| D6.4 Debug | 3 | 0 | 0 | 3 |
| **Debug total** | **1,245** | **0** | **289** | **1,534** |
| M1 Release runner | 1,050 | 0 | 269 | 1,319 |
| D6.4 Release runner | 3 | 0 | 0 | 3 |
| Supplemental actual Release M1 | 1,019 | 0 | 199 | 1,218 |

All 289 skips are environmental Linux/Unix-native prerequisites on the Windows host (full classification in REGRESSION-RESULTS.md). Native cases remain skips, not passes. Producer's counts independently reproduced without reuse of producer expectations.

## 7. Qualifications carried by this verdict (non-blocking)

1. **Remote tip delta:** remote `main` = `401e1ea7` ("preserve historical preparation bytes on checkout"), one commit beyond the receipt, containing only `.gitattributes`, docs and `eng/tests/test_preparation_checkout.py` (verified via API). Unreviewed by Q2; no runtime change; owner should fold it into acceptance scope or bind acceptance to `dce71ac`.
2. **CI `preparation` job failure at the receipt** (fixture/preparation byte-identity verification step; not the build boundary): fixed by `401e1ea7`; tip run fully green. All other receipt jobs — including build (windows+ubuntu) and unit tests — succeeded.
3. **Environmental skips:** 289 Debug skips + 199 supplemental skips are Linux/Unix-native prerequisites; native qualification remains NOT ATTEMPTED (inherited).
4. **Inherited clock qualification:** the internal `Reconciler(IDomainClock, TimeSpan)` constructor remains in Release metadata (verified) with production call sites on the sealed `StopwatchDomainClock` overload; unchanged from S1 and explicitly outside this review's remediation scope.
5. **Inherited Q1 qualifications preserved:** System.IO.Hashing via SourceLink (S2), native dependency closure, .NET host/runtime, ICU/OpenSSL/zlib, native Linux/WSL2 scope, D6.4 explicit restore.
6. **Supplemental script coupling (new OBSERVATION):** `eng/test-production-regressions.ps1` requires the evidence directory to be inside the repository tree (generated project inherits `Directory.Build.props` for `ImplicitUsings`); with an outside evidence dir it fails with CS0246. Documented; does not affect accepted entry points.
7. **Cross-root hash variance** and same-root overlapping builds: declared boundary respected; no cross-root byte-reproducibility claim is made by the reviewed implementation.

## 8. Findings summary

No BLOCKER, no MAJOR, no MINOR. Five OBSERVATIONS recorded (see FINDINGS.md). Historical Q1 findings retain their dispositions; nothing was silently closed.

## 9. Custody

- Canonical repository unchanged by the reviewer: 0 tracked modifications before and after; 74 worktrees preserved; pre-existing untracked inventory unchanged (28 entries; two junk-named entries present before review and preserved).
- Canonical HEAD was observed moving during the review window from `dce71ac` to `401e1ea7` (owner pull/push activity); the reviewed implementation and receipt remain in ancestry and all local probes used reviewer-materialized source copies at `C:\Users\Laszlos\source\repos\Solo\b6-impl-01-s1-q2-review\src-original|src-remediated`.
- All compilation, outputs and test results live in the isolated review directory. Q1 evidence re-verified byte-identical (16/16 hashes from the producer's `q1-evidence.sha256`).
- Self-excluded SHA-256 manifest: `evidence/MANIFEST.md`.
