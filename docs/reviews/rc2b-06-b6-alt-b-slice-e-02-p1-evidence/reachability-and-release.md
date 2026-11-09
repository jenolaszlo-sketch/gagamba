# E-2-P1 — production-reachability and RELEASE findings

Date: 2026-11-09 (Asia/Manila)

## Direct-call inspection (Debug assembly)

Test: `ValidationReachabilityTests.NoProductionCallerReachesTheValidationCore`.

| Check | Result |
|---|---|
| E-2-P1 types and members used as scan targets | all types and members of `Gagamba.Execution.Linux.M1.Validation` |
| Methods with bodies scanned outside the namespace | 4,519 method bodies (Debug M1 type-name entries: 511) |
| Direct call/ldftn tokens targeting the E-2-P1 core | **0 offenders** |
| Positive control (`TryVerifyChain` → `VerifyManifest`) | detected (scanner validity proven) |

## Compiled RELEASE assembly inspection

Test: `ValidationReachabilityTests.CompiledReleaseAssemblyHasNoProductionCallerAndNoPublicExport` (loads the source Release DLL in a collectible `AssemblyLoadContext`; asserts skip when the Release artifact is absent).

| Check | Result |
|---|---|
| Source Release assembly | `src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll` |
| Release identity | MVID `19a06c60-a117-4502-a1ef-ae61ac0b83ca`; SHA-256 `321F71315E79A190C07AA83BAD2480993F109428DC4DE374814AB1342FBD8006`; type-name entries 488; method bodies decoded 4,366 |
| Direct call tokens targeting the E-2-P1 core | **0 offenders** |
| Exported public types in the namespace | **none** (`GetExportedTypes` contains no `...Validation` type) |

## Surface and dependency checks

| Check | Result |
|---|---|
| Public API | E-2-P1 types are internal; no exported namespace type in Debug or Release |
| Authority-shaped names | no `Approved`/`Authorized`/`Permit`/`Authority`/`Trusted`/`Capability` tokens in type or member names |
| Forbidden dependencies | no `System.IO.File/Directory/FileStream/Path`, `System.Environment`, `System.Diagnostics.Process`, `System.Net.*`, or interop references in E-2-P1 IL |
| Filesystem/process/network behavior | none; the core is pure over byte arrays |

## RELEASE verifier

| Probe | Result |
|---|---|
| `dotnet run --project eng/VerifyProductionAssembly -c Release -- --production <Release M1>` | **RESULT: PASS**, exit 0; 0/4 hook types, 0/3 hook properties, 14/14 core types |
| Same verifier against the Debug assembly (expected negative control) | **RESULT: FAIL**, exit 1, naming the accepted forbidden hooks/factories (including `PrepublicationIntegrityProjection::ForTest`) |

No verifier policy was modified; `eng/` is unchanged.

## P4 assessment supported by this evidence

The implementation adds only new internal files; no existing production file changed, no protected-sink caller changed, no admission/lifecycle path changed, no authority factory or RELEASE seam exists, and the RELEASE verifier passes. This supports the owner decision's **conditional P4-NEUTRAL** classification at this revision; independent P1-Q1 must confirm.

## Limitations

- The scans prove absence of inspected **direct-call tokens** from other M1 types; they do not constitute a general transitive-reachability proof.
- Debug and Release are compiled from the same sources; the Release scan is the stronger artifact but the two were not run as a differential native test.
- No protected-filesystem or native behavior is exercised; `NATIVE-PENDING` and the 331 native skips remain.
