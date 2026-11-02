# SAFETY-PRESERVATION.md — B1–B5 invariants and source-change assessment

## 1. Complete REM-01 diff analysis (`1cbe4df..dce71ac`)

Changed files (22 in implementation, 8 more docs-only in receipt):

| File | Nature | Production source? | Test source? |
|---|---|---|---|
| `Directory.Build.props` | Scopes `GAGAMBA_PRODUCTION` to M1/Owner/Recovery (Release only) | build config | — |
| `Directory.Build.targets` (new) | Early + late symbol invariants, post-copy binary gates, publish gates | build config | — |
| 3 production csproj (M1, Owner, Recovery) | `AdditionalProperties="Configuration=$(Configuration)"` on ProjectReferences; M1 keeps existing carrier-property removal | build config | — |
| `Gagamba.Execution.Linux.M1.Tests.csproj` | Explicit Debug references for M1/Execution/Owner | — | build config |
| `Gagamba.Execution.Linux.M1.D6_4.Tests.csproj` | `Configuration=Debug` added to fixture reference and restore | — | build config |
| `eng/VerifyProductionAssembly/*` (new) | Standalone metadata/IL verifier | — | tooling |
| `eng/test-production-build.ps1`, `eng/test-production-regressions.ps1` (new) | Probe/supplemental-suite scripts | — | tooling |
| docs/evidence/status/queue/handoff (12 files) | Documentation | — | — |

**`git diff --name-only 1cbe4df..dce71ac -- '*.cs'`** → only `eng/VerifyProductionAssembly/Program.cs` (new tool). **No file under `src/` other than the three csproj, and no file under `tests/` other than the two csproj, changed.** Zero runtime source edits; zero test-assertion edits; zero skips added; zero projects removed from the solution; zero packaging-behavior changes (props packaging block byte-identical; only the symbol PropertyGroup changed).

The changes are genuinely restricted to: build configuration, MSBuild verification, test-project compilation configuration, artifact isolation, and bounded evidence/verification tooling — exactly the authorized scope.

## 2. B1–B5 invariant preservation

- **Owner-only RELEASE authority; at-most-once physical RELEASE; independent stopping; retained recovery; HELD; deadline enforcement; journal and uncertainty handling; controller-loss safety; staging/capture cleanup:** no runtime source changed, therefore behavior is preserved by construction; independently re-evidenced by the 61 B5 cases passing in all three execution contexts (Debug, Release runner, supplemental actual-Release M1 — TRX-verified), the D6.4 3/3 assertions in both runner configurations, and the full 1,245/0/289 Debug inventory with zero unexplained differences.
- **`RestrictedExecutionRuntime.Create()` remains production-unavailable:** `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs:9` — `Create() => new UnavailableRestrictedExecutionProvider();` unchanged (file has zero diff in the range).
- **Production call sites unchanged:** the sealed `StopwatchDomainClock` composition and the internal `Reconciler(IDomainClock,…)` test-only seam are byte-identical to S1 (source-inspected; binary metadata confirms presence).

## 3. Scope exclusions verified (no S2+ or unauthorized implementation)

Verified from the complete diff: no root-owned generations, no installation qualification records, no canonical installed manifests, no `RecoveryConfigV2`, no wire messages 9/10/36, no public descriptor v2, no B6 production admission, no B7/Hufu/VM/HCS work, no package publishing. The only new executable code is the standalone verifier and two probe scripts — outside production execution paths (the verifier is invoked by build gates and is never a runtime dependency of M1/Owner/Recovery).

## 4. Producer claims independently confirmed

- "No existing runtime `.cs` file or existing test assertion source was edited" — **confirmed** (diff).
- "Instrumented variants retain the necessary qualification behavior" — **confirmed** (d4/d5/d7 + 101 hook-dependent cases).
- "Production binaries exclude all seven qualification-only surfaces" — **confirmed** (independent probe; BINARY-INSPECTION.md).
- "51/51 build matrix" — **corroborated in material claims** for every mandatory class (not line-by-line re-executed; qualified in BUILD-ISOLATION.md §4).
- "0 errors / 0 warnings solution Release build" — **0 errors confirmed; warnings differ environmentally** (58 SourceLink warnings in the reviewer's git-metadata-free copy; zero in the canonical checkout context the producer used).

## 5. Historical evidence preservation

- Original S1 evidence (`docs/evidence/rc2b-06-b6-impl-01-s1-production-build-separation.md`) unchanged in the range.
- Q1 review directory: 16/16 recorded hashes re-verified byte-identical; no file touched.
- All 74 worktrees preserved; canonical tracked tree clean before and after.

**Gate H outcome: Preserved.** The remediation introduces no unauthorized change in authority or implementation scope, and no B1–B5 safety property is weakened within the tested scope.
