# E-2-P2 production reachability, E-1/F02 separation, RELEASE, and P4 neutrality

Date: 2026-11-09 (Asia/Manila)

Test source: `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceReachabilityTests.cs`
(blob `f65e60c4…`). All checks below executed in the P2 filter run
(`executed/full-suite.txt`, `executed/release-verifier.txt`,
`executed/debug-as-production-verifier.txt`).

## 1. Direct-call reachability (owner §20)

- **Debug scan** — `NoProductionCallerReachesTheDetachedEvidenceCore`: every
  method/constructor token target declared by the five P2 types is collected,
  and every type outside `Gagamba.Execution.Linux.M1.Validation` is scanned for
  IL `InlineMethod` tokens referencing them; zero offenders. The accepted P1
  namespace-wide scan (`ValidationReachabilityTests.NoProductionCallerReachesTheValidationCore`)
  additionally covers the new P2 types because it targets every member in the
  Validation namespace, and it passed in the same run.
- **Positive control** — `CallerScannerDetectsKnownIntraCoreCalls` proves the
  scanner shape detects a known direct call (`TryVerifyDetachedEvidenceChain` →
  private `VerifyReviewRecord`).
- **Compiled RELEASE scan** —
  `CompiledReleaseAssemblyHasNoProductionCallerAndNoPublicExportForDetachedEvidence`
  loads the compiled source-Release M1 assembly, asserts all five P2 types are
  present and internal, scans every non-Validation type for tokens referencing
  P2 members, and asserts no Validation type is exported; zero offenders. The
  P1 compiled-Release scan passed in the same run. The full suite reported
  exactly the preserved 331 native skips, so neither Release-scan test was
  skipped due to a missing artifact.
- **Production signatures** — `ProductionSignaturesDoNotConsumeDetachedEvidenceResults`:
  no method, constructor, property, or field of any type outside the Validation
  namespace uses a P2 result type (parameters, returns, member types). A
  final-admission, protected-sink, or invocation consumer would require such a
  signature or a direct call; neither exists.
- **Scope limitation (P1-Q1-F05 retained)** — direct-call absence in the
  inspected Debug and compiled-Release method bodies is the supported static
  evidence scope; this is not an exhaustive transitive or reflection-aware
  reachability proof, and no stronger claim is made.

## 2. Surface and dependency inspection (owner §§14–16, §20)

- `DetachedEvidenceCoreExportsNoPublicApi`: no P2 type is public or nested
  public; the Validation namespace contributes no exported type.
- `DetachedEvidenceMemberNamesDoNotImplyAuthority`: no type or member name
  contains `Approved`, `Authorized`, `Permit`, `Authority`, `Trusted`,
  `Capability`, `Eligible`, `ValidationAuthority`, `VerifyApproval`.
- `DetachedEvidenceVerifierIsAStaticInternalClassWithoutInstanceState`: static
  internal class; no public methods; no instance constructors; all declared
  methods static.
- `EveryTryVerifyEntryRequiresCatalogAndPrecedingRecordBytes`: exactly four
  `TryVerify*` entries, each requiring at least two `byte[]` inputs; there is no
  single-`byte[]`-only verification entry.
- `DetachedEvidenceCoreReferencesOnlyValidationTypesAndTheBaseClassLibrary`:
  every resolved IL reference and every signature type in the P2 core is either
  a `System.*` type or a `Gagamba.Execution.Linux.M1.Validation` type — no
  `Installed`, `Launch`, `Lifecycle`, `Restricted`, `Execution`, `Bootstrap`,
  `Native`, `Preparation`, or `Recovery` dependency (no protected root, custody,
  final-admission, E-1, or invocation path).
- `DetachedEvidenceCallsOnlyItselfConsumesTheCatalogContractAndUsesTheAuthorizedP1Surface`:
  P2 call targets are limited to P2 itself, reads of the P1-verified
  `CatalogContract` result, and the authorized P1 surface
  (`CanonicalJson.Parse/Hash/IsLowerHex`, `FrozenSchemaRules.IsValidId`,
  `FrozenContractVerifier.TryVerifyCatalog`). No other P1 interface is called.

## 3. E-1 exclusion (owner §21)

No E-1 production consumer is introduced: the P2 core has no reference of any
kind to E-1 types (dependency scan §2), no production signature consumes P2
results (§1), and no existing file was modified (report §"diff inspection"), so
E-1's accepted-with-qualifications state (F-01 bindings, F-02 Q1-F01–Q1-F06 and
first-consumer obligations, F-03 PowerShell 5.1 qualification) is untouched.

## 4. F02 authority separation (owner §13, D1)

`FinalAcceptanceCodec` is unchanged (blob-verified in the report), no P2 code
references it or any `Installed` type, and no executable approval dependency on
the F02 qualification store was introduced. The P1 CJSON-1 compatibility that
F02 already qualified is reused read-only; the divergence-control obligation is
preserved and not modified.

## 5. RELEASE verification (owner §22)

| Check | Command | Result |
|---|---|---|
| Release M1 build | `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release --nologo` | succeeded, 0 warnings / 0 errors; production mode; 0/4 hook types, 0/3 hook properties, 14/14 core types; `RESULT: PASS`; MVID `973e4468…`; SHA-256 `F234CFDA…` |
| RELEASE verifier | `dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll` | **RESULT: PASS**, exit 0; 0/4 hook types, 0/3 hook properties, 14/14 core types |
| Debug-as-production negative control | same with `bin/Debug/...` | **RESULT: FAIL**, exit 1; qualification hooks, debug-only factories (`Grant::ForTest`, `OwnerReleaseGenerationGate::VerifyObserved`, `PrepublicationIntegrityProjection::ForTest`, `Qualified::ForTest`), metadata/user-string/IL-token references, configuration `Debug` |

Exact outputs are preserved in `executed/release-verifier.txt` and
`executed/debug-as-production-verifier.txt`. `eng/**` is unchanged; no verifier
rule was weakened; no new `ForTest` seam was introduced.

## 6. P4 neutrality (owner §22; CONDITIONAL)

The implementation adds only two new internal files under the authorized
Validation directory and four new test files under the authorized test
directory; no protected-sink caller set, F01/F02 enforcement, S10/S11
domination, S28 isolation, first protected effect, qualified-to-legacy
downgrade refusal, root authorization, E-1 reachability, or RELEASE isolation
file was modified. No new authority-bearing path is reachable from production
(§1–§2). Preserved: F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual,
`NATIVE-PENDING`, and all 331 native skips (the full suite skipped count is
unchanged at 331; native skips are not passes and no native positive
qualification is claimed). P4 neutrality remains conditional on independent
confirmation in P2-Q1.
