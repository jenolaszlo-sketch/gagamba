# FINDING-002-REVIEW.md — DefineConstants override, fail-closed semantics, unsafe/stale artifact probes

Q1-FINDING-002 (MAJOR): `-p:DefineConstants=""` bypassed the production compilation symbol, producing a Release-labeled binary containing qualification hooks.

## 1. Failing-before reproduction (independent, original S1 `1cbe4df`)

- Command: `dotnet build src-original\src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj -c Release -p:DefineConstants= --artifacts-path work\a2-before-artifacts --nologo -v:d` → **exit 0** (`evidence/a2-before-build.log`).
- **Effective compiler constants verified, not assumed:** the detailed log records
  - `The "DefineConstants" property is a global property, and cannot be modified.` (the props-file append no-ops against a CLI global), and
  - the M1 project's Csc response file with `/define:NET;NET10_0;NETCOREAPP;…` — **no `GAGAMBA_PRODUCTION`, no TRACE/RELEASE** (the empty global suppresses the SDK defaults), while the Execution reference build (`GlobalPropertiesToRemove="…DefineConstants…"`) did compile with `/define:GAGAMBA_PRODUCTION;TRACE;RELEASE;…`.
- Produced artifact: `work\a2-before-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll`, AssemblyConfiguration **Release**, SHA-256 `19F77AB4B169878D70DA0EC21ED171C00630696B3BE68C0DBBB84C177257FF6B`, MVID `a4de5d53-c550-4064-9cb0-607230b41a83`.
- **Reviewer-owned inspection (`probes/Q2Inspect`, `evidence/d6-unsafe-baseline.log` + producer-tool run `evidence/a2-before-inspection.log`):** 4/4 hook types, 3/3 hook properties, hook backing fields and accessors, fault-case user strings, and IL token references to hook members all present. 294 types / 3,597 methods (vs 275 / 3,450 for the true production build).
- **Agreement with Q1-FINDING-002: exact** — the bypass mechanism, the surviving hooks and the Release label reproduce independently. The unsafe artifact is retained as a negative control (never eligible for acceptance; rejected by both verifiers).

## 2. Remediated mechanism (source-inspected)

`Directory.Build.targets` (reviewed revision):
- `RejectMissingGagambaProductionSymbol` — `BeforeTargets="PrepareForBuild"`, Release-only for the three trusted projects → GAGAMBA002 **before reference resolution** (early refusal prevents differently-scoped dependency instances racing shared outputs).
- `ValidateGagambaCompilationIdentity` — `BeforeTargets="CoreCompile;ComputeFilesToPublish"`: GAGAMBA001 (unknown configuration), GAGAMBA002 (Release without effective symbol, regex `(^|[;,\s])GAGAMBA_PRODUCTION($|[;,\s])` over `$(DefineConstants)` — the exact string Csc receives per Microsoft.CSharp.Core.targets), GAGAMBA003 (non-Release with production symbol), GAGAMBA004–008 (D6.4 fixture constraints). Runs even when incremental compilation is skipped.
- `VerifyGagambaProductionBinary` — `AfterTargets="CopyFilesToOutputDirectory"` (Release): executes the independent PE metadata/IL verifier against `$(TargetDir)Gagamba.Execution.Linux.M1.dll` — actual bytes, not properties.
- `VerifyGagambaInstrumentedBinary` (Debug/Test) and publish gates (`BeforeTargets=ComputeFilesToPublish` + `AfterTargets=Publish`).

Observed effective Release constants for trusted projects: `;GAGAMBA_PRODUCTION;TRACE;RELEASE;NET;NET10_0;…` (validation messages in `evidence/b1-solution-release-after.log`); Debug: `TRACE;DEBUG;…` (no production symbol).

## 3. Mandatory negative-case matrix (all independently executed)

| # | Case | Command/property | Exit | Diagnostic | Evidence |
|---|---|---|---|---|---|
| 1 | Release, valid defaults | `-c Release` (fresh roots + in-place) | 0 | — | b1, e3, c15 |
| 2 | Release, empty DefineConstants | `-p:DefineConstants=` | 1 | GAGAMBA002 | c02 |
| 3 | Release, value omitting symbol | `-p:DefineConstants=TRACE` | 1 | GAGAMBA002 | c03 |
| 4 | Release, only unrelated constants | `-p:DefineConstants=REVIEWER_UNRELATED_CONST` | 1 | GAGAMBA002 | c04 |
| 5 | Release, qualification-only constants | `-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE` | 1 | GAGAMBA002 | c05 |
| 6 | Release, contradictory production+fixture | `-p:DefineConstants=GAGAMBA_PRODUCTION%3BGAGAMBA_WP05_D6_4_TEST_FIXTURE` | 1 | GAGAMBA004 | c06b |
| 7 | Debug/Test with forced production | `-c Debug` / `-c Test` `-p:DefineConstants=GAGAMBA_PRODUCTION` | 1/1 | GAGAMBA003 | c07a/c07b |
| 8 | Direct M1 Release build | fresh artifacts root | 0 | gate PASS 0/4+0/3 | e3 |
| 9 | Owner/Recovery Release w/ transitive M1 | valid: 0 with closure gate; contradictory: GAGAMBA002 | 0/1/1 | — | b3, b4, c09a, c09b |
| 10 | Full-solution Release with override | `dotnet build Gagamba.sln -c Release -p:DefineConstants=` | 1 | GAGAMBA002 | c10 |
| 11 | Valid Release then contradictory | contradiction after valid build | 1 | GAGAMBA002; prior bytes unchanged (hash `4D8A31BD…` preserved) | c11 |
| 12 | Contradictory/stale with retained outputs | corrupt bin\Release with Debug bytes → valid build | 0 | gate PASS; **repaired** to `0FAFB4F7…` (fresh + incremental) | c12, c12c, c12d |
| 13 | No-restore / no-build paths | `--no-restore` + contradiction → GAGAMBA002; `dotnet build --no-build` → MSB1001; `dotnet msbuild -p:NoBuild=true -t:Build` → NETSDK1085 | 1/1/1 | refused | c13, c14a, c14b, c14c |
| 14 | Publish targets (trusted M1 closure) | valid publish (build and `--no-build`) pass with input+final verification; contradictory with/without build refuse; **corrupt-input no-build publish refuses and publishes nothing** | 0/1/1/1 | GAGAMBA002 / instrumented-gate fail | c15, c16, c17, c17b, c17c |

Reviewer-authored mutations not copied from the producer's matrix: **c18** `-p:DefineConstants=XGAGAMBA_PRODUCTION` (regex boundary guard; refused with GAGAMBA002 — a prefix-junk pseudo-symbol must not enable production compilation), **c19** `-c Unclassified` (GAGAMBA001), **c20a/b** fixture-carrier contradictions via `GagambaWp05D64FixtureEnabled` (GAGAMBA004/007), **c-g5/g6/g7** malformed carrier value (GAGAMBA005), fixture symbol without carrier (GAGAMBA006), fixture AssemblyName in Release (GAGAMBA004), plus the `%3B`-escaped combined-symbols case (c06b) and the MSBuild-native NoBuild probe (c14c).

## 4. Fail-closed: bytes on disk after failure

- Contradictory builds refuse **before compilation and reference resolution**; post-refusal hash checks show the previously valid Release binary untouched (`c11`, `c13`: `4D8A31BD…` before/after).
- A corrupted output (Debug bytes at the Release path) is refused at publish input (`c17b`: exit 1, `work\c17b-publish` **not created**) and repaired by the next valid Release build (fresh and incremental: `c12`, `c12c`, `c12d` → `0FAFB4F7…` with a PASS gate). A failed build never produces an eligible candidate; every *successful* trusted Release output passed the actual-bytes gate in the same invocation.
- `--no-build` cannot reach an accepted state: CLI `MSB1001` (unknown switch) and MSBuild-native `NETSDK1085` (NoBuild=true with Build target) both fail closed.
- No scenario was found where an unsafe binary sits at a trusted Release output path behind a green build command, within the supported entry-point set.

## 5. Threat-model boundary (accepted build-input scope)

The mechanism protects the declared repository build entry points and configurations against accidental or CLI-level contradictory properties. It does **not** resist: modification of `Directory.Build.targets`, the verifier, the compiler or SDK; arbitrary custom imports/targets; or disabling repository imports. No root-owned installation attestation, no forged-record rejection (S2+), no general provenance claim. This matches the reviewed implementation's stated boundary; no stronger claim is made here.

**Gate C outcome: Q1-FINDING-002 CLOSED.** Contradictory configurations demonstrably refuse with the intended diagnostics, and rejected/stale outputs cannot pass the accepted production-artifact boundary: refusal precedes emission, successful outputs are byte-verified in-invocation, corrupted outputs are repaired or refused, and publish never accepts unsafe input.
