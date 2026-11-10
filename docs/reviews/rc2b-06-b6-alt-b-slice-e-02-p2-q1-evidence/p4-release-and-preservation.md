# P2-Q1 — RELEASE verification, P4 classification, and preservation

## 1. Independent build + RELEASE verification

| Check | Command | Result |
|---|---|---|
| Debug M1 build | `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Debug` | succeeded; 0 warnings / 0 errors; instrumented mode PASS; MVID `bd8a820d…`; SHA-256 `E3089FB9…` |
| Test project build | `dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug` | succeeded; 0 warnings / 0 errors |
| Release M1 build | `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release` | succeeded; 0 warnings / 0 errors; production mode PASS (0/4 hooks, 0/3 properties, 14/14 core); MVID `1994ef0f…`; SHA-256 `BC8E33E8…` |
| RELEASE production verifier | `dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll` | **RESULT: PASS**, exit 0; 0/4 hook types, 0/3 hook properties, 14/14 core types |
| Debug-as-production control | same verifier against `bin/Debug/...` | **RESULT: FAIL**, exit 1; unchanged forbidden lists (qualification hooks, `Grant::ForTest`, `OwnerReleaseGenerationGate::VerifyObserved`, `PrepublicationIntegrityProjection::ForTest`, `Qualified::ForTest`, metadata/user-string/IL-token references, configuration `Debug`) |

The verifier project and policy were not modified (`eng/**` unchanged:
last commit `52037fa`, no diff from the P1 revision to HEAD). No verifier rule
was weakened; no new `ForTest` seam; no new public authority API; no new
protected acquisition or approval path. Rebuilds are not bit-reproducible
against the producer's recorded hashes (retained Q1-F04-lineage qualification);
the source blobs are the identity anchor.

## 2. P4 classification: **P4-NEUTRAL**

Basis (all independently checked at `9b80969`):

| P4-relevant surface | Finding |
|---|---|
| New production files | exactly the two authorized internal files under `Validation/`; no existing protected-sink caller changed (`git diff 06006a7..HEAD` over `Installed`, `Bootstrap`, `Launch`, `Lifecycle`, `Restricted`, `Execution`, `Native`, `Preparation`, `Recovery` is empty) |
| F01/F02 enforcement | unchanged (no diff in the authority-bearing directories; `FinalAcceptance.cs` untouched since `831c980`) |
| S10/S11 domination | unchanged; no new production caller of `M1RecoveryScope.StartOwnerPreparation`/`TrySpawnOwner` or any other audited generic method (the P2 verifier has zero production callers) |
| S28 isolation | unchanged; no new caller of `PreparationOwnershipCoordinator` |
| First protected effect | unchanged; P2 performs no protected effect and has no production path |
| Qualified-to-legacy downgrade refusal | unchanged; no admission/authority source touched |
| Production authority path consuming P2 | none (REACH scans, signature scan, type-token and string probes) |
| E-1 production consumer | none (no E-1 type referenced from the P2 core; no E-1 file changed) |
| RELEASE isolation | unchanged: production verifier PASS, Debug-as-production FAIL, no exported Validation type |

Retained: F-R1/O-R1, O-R2, O-R3, the accepted observation-to-effect residual,
`NATIVE-PENDING`, and all 331 native skips (unchanged in the full suite).
Classification follows the P2 entry owner decision §22 conditional
"P4-NEUTRAL": the condition (post-implementation evidence with no
production-reachable approval or protected-execution path) is satisfied for
this revision. It does not extend to any future consumer or downstream
milestone (E-3–E-7, Slice N).

## 3. E-1 and F02 separation (restated evidence)

- No P2 executable reference to E-1 types, `Installed`, `Lifecycle`, `Launch`,
  `Restricted`, `Bootstrap`, `Preparation`, `Recovery`, or `Native`
  (`P1R-SURFACE`, `REACH-*-01/02/03`).
- `FinalAcceptanceCodec` unchanged; no P2 reference to it or the qualification
  store; F02 authority remains store-specific and is not reused.
- No new manifest/RELEASE authority; `OwnerAuthorityContext.Validation` remains
  without a factory; no positive validation; no protected execution.

## 4. Preservation

- Exactly one canonical worktree; branch `main`; clean tracked/index state.
- All 44 historical untracked paths present with matching SHA-256
  (`diff_count=0`), and the NUL-delimited untracked-set comparison is
  identical.
- No fetch, pull, push, reset, clean, merge, rebase, prune, stash, branch, or
  additional worktree was used at any point in this commission.
- The 331 native skips are unqualified and are not passes; no native positive
  qualification is claimed.
