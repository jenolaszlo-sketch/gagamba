# BINARY-INSPECTION.md — Independent metadata/IL inventory and SHA-256 identities

## 1. Reviewer-owned probe

`probes/Q2Inspect/` (reviewer-authored, ~200 lines C#, `System.Reflection.Metadata` only — independent of the producer's `eng/VerifyProductionAssembly`). It enumerates TypeDefinitions (incl. nested), Properties, Methods, Fields; reads AssemblyConfigurationAttribute; decodes no producer expectations — the seven accepted surfaces and twelve core safety types were independently taken from the reviewed source (`SafeCopyIn.cs`, `Int01QualificationFault.cs`, the three property declarations). A **raw byte scan** over the whole PE (UTF-8 metadata heap + UTF-16 user strings) provides a second, independent channel for hook/test-clock/test-fixture markers.

The producer's verifier (`eng/VerifyProductionAssembly`, committed at `6689675`) was built and run in parallel for corroboration; its inventories were validated against source and never used as the sole oracle.

The accepted seven (verified at source level, conditionally compiled behind `GAGAMBA_PRODUCTION` in 12 M1 source files):

- Types: `Gagamba.Execution.Linux.M1.Bootstrap.Int01QualificationFault`, `…Bootstrap.Int01Qualification`, `…Preparation.CopyTestHooks`, `…Preparation.BarrierDeathException`
- Properties: `…Bootstrap.OwnerSafetyMonitor::QualificationNativeStop`, `…Launch.BubblewrapAdapter::QualificationBeforeCaptureStore`, `…Native.JournalFileStorage::QualificationBeforeRename`

## 2. Independent results (full logs in evidence\d1–d7)

| Probe | Binary | Configuration | MVID | Hook types | Hook props | Raw markers | Safety types | Result |
|---|---|---|---|---|---|---|---|---|
| d1 | M1 Release (production) | Release | `65fa969d-8458-4024-89cf-494cf3366c1d` | **0/4** | **0/3** | **none** | 12/12 | PASS |
| d2 | Owner Release closure | Release | `e27af7e4-5a67-45a5-a56d-dcb3e06e902f` | 0/4 | 0/3 | none | (2-type assembly; M1 types correctly in referenced M1) | PASS |
| d3 | Recovery Release closure | Release | `9d304d1e-18f6-4020-9de3-37504268b8b6` | 0/4 | 0/3 | none | (as d2) | PASS |
| d4 | M1 Debug (instrumented) | Debug | `1ce2cd83-30bd-4b2c-95af-3309a6a8a17a` | 4/4 | 3/3 | 6 hook markers | 12/12 | PASS |
| d5 | M1.Tests\bin\Release closure copy | Debug | same MVID as d4 | 4/4 | 3/3 | 6 hook markers | 12/12 | PASS |
| d6 | Unsafe baseline (Q1-bypass artifact) | Release | `a4de5d53-c550-4064-9cb0-607230b41a83` | 4/4 | 3/3 | 6 hook markers | 12/12 | **REJECT (correct)** |
| d7 | D6.4 TestFixture | Debug | `c04602d2-9e9d-432f-b28e-56851fe34588` | 4/4 | 3/3 | markers + TestFixture | 12/12 | PASS |

d1 inventory: 275 types (+79 nested distinct), 3,450 methods, 1,632 fields, 871 properties. d4/d5: 295 types, 3,600 methods — the +20/+150 delta is the hook surface, consistent with the conditional-compilation boundary.

Producer-tool corroboration (same binaries): identical PASS/FAIL dispositions; its stricter checks (backing fields, accessors, IL token references, fault strings, forbidden metadata names) add no contradiction.

**Coverage of required assertions:** type definitions ✔ (d1–d7), methods ✔ (metadata + raw scan; the producer tool additionally decoded every method body and IL token reference — 3,326 bodies on production — with matching results), fields ✔ (backing fields absent in production; producer tool corroborates), properties ✔, qualification delegates ✔ (no hook-type/property surfaces remain), compiler-generated closures ✔ (no `<Qualification…>k__BackingField`/closure names in production), executable fault branches ✔ (fault-case strings absent from production user strings), reflection-accessible members ✔ (properties/accessors enumerated; no activation switch present in the remediation diff).

**Distinction:** the seven surfaces are *source-conditionally excluded* (`#if !GAGAMBA_PRODUCTION` guards in 12 files) and *binary-independently-verified absent* in every production configuration built through supported entry points (direct, solution, owner/recovery closures, publish outputs, parallel roots).

## 3. Variant identity (SHA-256, reviewer environment)

See `evidence/artifact-hashes.txt`. Key identities:

| Artifact | SHA-256 |
|---|---|
| M1 Release | `0FAFB4F7AA4CDCDCF1B7D040CF45ADB99F583BF7A1D1D54FBBDD9ABF993E2CF4` |
| Owner Release | `B1D48349B931E480DFB24FEFBF873280E3985C83356B45B2BD08B76385995E45` |
| Recovery Release | `C0794CEE6A04917CC15BF459FC15643B6C2B4D7BF01588EC4B04455F094436C2` |
| Execution Release | `648652F7C7750A403CFEEFB6188C533E32BD0DC27DF5D2230C0889A0998ED7FA` |
| M1 Debug | `8DA0A01CE0620EA0D75FC74B6D6758B99CB93CDB8D490B6C26755A8F8BB27067` |
| M1 Test config | `A651F219966FD3E76EED22B78C7104ADFFD041351FC64CB5F1E77AB27E006AF0` |
| Owner Debug | `7D3CDE0995AA871770DEB5AE1F8ADCB8FDF8C7DD1D31030E5AD863565F22E109` |
| D6.4 TestFixture | `ECBD3385584AD53C99B36DD7104FC6EC35A1A78CEF546EA2426D1724C922DAB9` |
| Unsafe baseline | `19F77AB4B169878D70DA0EC21ED171C00630696B3BE68C0DBBB84C177257FF6B` |

- Production/test identities are fully distinguishable (no collisions).
- **Differences from the producer's recorded hashes are expected:** different build roots and absence of git metadata in the materialized copy (SourceLink warnings; path-derived PE inputs). Neither side claims cross-environment byte-reproducibility (Q1 FINDING-005 lineage).
- Cross-root variance among reviewer's own fresh roots documented in BUILD-ISOLATION.md §3; within-root stability confirmed.
- Published M1 (`work\c15-publish`) is byte-identical to the in-place Release build (copy, not recompile).
- Closure contamination: Owner/Recovery Release output directories contain 4 DLLs each, **zero** matching Tests/TestFixture/B2Controller/VerifyProductionAssembly patterns.

## 4. Instrumented positive control

The Debug/Test/TestFixture variants retain all seven surfaces with correct configuration labels; representative hook execution is exercised by the unchanged copy/binding, barrier and recovery tests (101 hook-dependent cases executed in the full suites; 61 B5 cases pass against both instrumented and actual-Release M1).

## 5. Inherited clock qualification (verified, unchanged)

`Reconciler.cs:141` retains `internal Reconciler(IDomainClock clock, TimeSpan stopAllowance)` as an explicitly documented test-only seam; `IDomainClock`, `StopwatchDomainClock`, `Reconciler` and the legitimate `FixtureConstructorMarker` safety sentinel are present in the Release binary's metadata (raw scan), exactly as disclosed by the producer. Production call sites compose the sealed `StopwatchDomainClock`. This inherited qualification is **not** treated as remediated.

## Gate D outcome

**Supported.** The remediation preserves the independently qualified production/test separation; the unsafe baseline is rejected by the reviewer's own oracle.
