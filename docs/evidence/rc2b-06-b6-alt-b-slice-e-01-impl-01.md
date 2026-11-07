# Slice E-1 implementation evidence — non-forgeable prepublication integrity projection

- Date: 2026-11-07 (Asia/Manila)
- Remediation slice: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-IMPL-01` (E-1)
- Baseline source (starting HEAD): `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e`, tree `183de77fc703bffcd36167541be95e720c9b8d28`
- Final source (implementation commit): `2259fce59b44b6af6448a38685a0635d560d0900`, tree `77610b49ecae5398d633f1db43d4f01604694ee1`
- Source subtree (`src`): `a46e071bdc9a3451cd0e63c66080d36a0e200550`; test subtree (`tests`): `12f86960899f112cc21c90a176e1de982cf637e4`
- Branch: `main` (one canonical worktree)
- Host OS/version: Microsoft Windows NT 10.0.26200.0
- Architecture: AMD64 (win-x64)
- Runtime/SDK: .NET SDK 10.0.401, runtime 10.0.x
- Relevant native prerequisites: a protected, root-owned WSL2/Linux fixed-root installation with the accepted host/runtime/bwrap baseline. **Not provisioned and not exercised.** Native prepublication success is pending (Slice N).
- Qualification scope: source implementation; managed deterministic tests; RELEASE/DEBUG build identity; production-assembly refusal checks. No protected positive, no native lifecycle, no validation authority.
- Explicit exclusions: E-2 through E-7; K/M/F/S roots; v3-pre codec; GAVA1; root authorizer; launcher/driver; privilege drop/custody; R/O/C ordering change; `OwnerAuthorityContext.Validation` factory; positive protected execution; Slice N; public API expansion; production activation; push.

## 1. Claim

E-1 implements the distinct, immutable, internal **prepublication integrity projection** specified by the Alternative B owner decision §3.3: a typed representation of verified integrity evidence, bound to the actual protected active selection and full fixed-root manifest/payload plus the accepted host/platform checks, carrying a **null** qualification ID and record hash, with a single verified production construction route that ordinary callers cannot forge. It grants no validation, admission, execution, manifest, staging, HELD or RELEASE authority and is **not** reachable from any production entry path in this package.

This is **Implemented / Locally tested**. It is **not native-qualified** and does **not** complete Slice E.

## 2. Baseline-to-fix evidence

E-1 is a new, refusal-only boundary; it does not repair a previously failing assertion. Per the standard, a fabricated baseline failure is not invented. The relevant baseline observation is the design/reconciliation gap recorded before this slice.

| Probe / test | Baseline observation | Implementation change | Fixed observation |
|---|---|---|---|
| Distinct prepublication integrity selection | Absent. `InstalledFileSystem.VerifyActive` returns `InstalledResult.Fresh` (Installed/Unqualified) *before* `CheckHost`; `InstallationIdentity.Validate` requires `Qualified == true`; no prepublication type existed (entry review §3, owner decision §3.2). | New `PrepublicationIntegrityProjection` + `InstalledFileSystem.VerifyActiveIntegrity()` (isolated partial). | Typed projection exists; the isolated verifier runs the full active/payload/platform checks without a qualification association. |
| Non-forgeable trusted construction | No controlled construction route. | Private constructor; single verified route `Verify()`; Debug-only `ForTest` excluded from RELEASE. | REFLECT/E-1 tests: only private constructors; caller-accessible factories are `Verify()` (0-arg) and Debug-only `ForTest(InstalledIntegrityEvidence)`. |
| Verification-before-use | `VerifyActive` fresh branch skips host checks. | Prepublication verifier requires `InstalledPayload.CheckHost` before returning evidence. | `Verify()` refuses on an unprovisioned host (see §6). |

No baseline test was made to fail; the slice adds an inert boundary rather than repairing a defect. That is recorded as a limitation of this evidence (there is no before/after defect pair).

## 3. Construction boundary and authority provenance

| Trusted field | Established by (accepted verification) |
|---|---|
| `ArtifactSetSha256` | `InstalledMetadata.Active` over the root-owned `/opt/gagamba/active`, with the generation-directory name and `manifest.json` digest rechecked (`SameDigest`). |
| `SourceCommit`, `SourceTree` | `InstalledManifest.Parse` of the canonical `manifest.json` (`InstallJson.Read(bytes, canonical: true)`). |
| Payload closure | `Walk` + `EnsureExact` + `InstalledPayload.CheckDependencies` over the exact generation set. |
| `RuntimeInventorySha256` | `manifest.platform.runtimeInventorySha256` (validated by `InstalledManifest.Parse`); also enforced functionally by `InstalledPayload.CheckHost`. |
| `OwnerClosureSha256`, `RecoveryClosureSha256` | `InstallationIdentity.RoleHash(manifest, 1/0)` — the accepted role-closure identity projection over the manifest artifacts. |
| Host/platform baseline | `InstalledPayload.CheckHost(manifest)` (WSL2/kernel/glibc/runtime/hostfxr/loader/bwrap digests, loader-preload, cgroup2, direct-peer features). |
| Store stability | `RecheckSelectors` after all reads; no protected store may change across the verification. |

Pure data vs verified integrity vs execution authority are kept distinct: the projection carries **verified integrity only**. It is not an `InstalledResult`, not a `LoadedInstallation`, and not any `OwnerAuthorityContext`.

## 4. Implemented components

- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs`
  - `internal sealed class PrepublicationIntegrityProjection` — immutable (get-only properties, sealed, private constructor); null `QualificationId`/`QualificationRecordSha256`; explicit `PrepublicationIntegrityProvenance`; `internal static Verify()`; private `FromEvidence(...)`; `#if !GAGAMBA_PRODUCTION internal static ForTest(InstalledIntegrityEvidence)`.
  - `internal enum PrepublicationIntegrityProvenance { ProtectedActiveSelection, DeterministicTestOnly }`.
  - `internal sealed record InstalledIntegrityEvidence(...)` — the pure evidence value returned by the verifier.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.Prepublication.cs`
  - `internal static partial class InstalledFileSystem` — `internal static InstalledIntegrityEvidence VerifyActiveIntegrity()`. A deliberate, isolated mirror of `VerifyActive`'s fixed-root read so E-1 cannot alter existing qualified verification; it reuses the same private descriptor-relative read primitives and adds the accepted host checks.
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/PrepublicationIntegrityProjectionTests.cs` — 17 deterministic rows.
- `eng/VerifyProductionAssembly/Program.cs` — added `PrepublicationIntegrityProjection::ForTest` to the RELEASE-forbidden factory list and the projection type to the preserved core types.

## 5. Why ordinary callers cannot forge trusted state

- The constructor is **private**. Only `FromEvidence` (private) builds a projection.
- In RELEASE, the only caller-accessible factory is `Verify()`, which re-enters the protected verifier; there is no factory accepting a digest, identity, manifest, `InstalledResult`, Boolean, claimed verification result, or injectable verifier.
- The Debug-only `ForTest(InstalledIntegrityEvidence)` seam is compile-time excluded from RELEASE (verified in §9) and permanently tags its result `DeterministicTestOnly`, so synthetic evidence is distinguishable from protected provenance.
- `FromEvidence` performs structural defense-in-depth (well-formed hex, digest == manifest digest, defined provenance) and rejects contradictory evidence.
- Test REFLECT assertions: only private constructors; caller-accessible factories limited to `Verify()`/`ForTest(InstalledIntegrityEvidence)`; no `bool`/`InstalledResult` parameter; no member returns or accepts an authority type; the type is internal, not public API; **no other type's signature references the projection** in the assembly.

The reflective boundary is out of the accepted threat model (consistent with P4-03-Q1 F03); the boundary claimed is against ordinary callers and untrusted evidence.

## 6. Refusal behavior

`Verify()` returns evidence only after the full accepted verification. It throws `InvalidDataException` on any missing/inconsistent/untrusted evidence. On a non-Linux or otherwise unprovisioned host (`EnsureLinuxX64` → `NotSupportedException`, or an absent `/opt/gagamba` root), it refines to `InvalidDataException`, so it fails closed. Confirmed by `VerifyRefusesOnANonLinuxHost` on the Windows host.

A phase, receipt or active-selection change refuses via `SameDigest`/`RecheckSelectors`; it never adopts or promotes the observed state.

## 7. Positive construction capabilities and limitations

- Deterministic positive: `ForTest` produces a fully populated projection from synthetic evidence (identity preserved, null qualification, `DeterministicTestOnly`). This is a Debug/test-only behavior, not protected provenance.
- Protected positive: **not established.** Succeeding `Verify()` requires a provisioned protected install and is `NATIVE-PENDING` (Slice N).
- The runtime loaded-assembly ("verification-to-use") role check from Gate C is **not** performed here: it belongs to the authenticated validation session/actor that does not exist until later packages. The projection binds the manifest-derived role-closure identity instead. This is a named limitation, not a hidden gap.

## 8. Production reachability and P4 impact assessment

- **No production consumer.** A working-tree audit finds `PrepublicationIntegrityProjection` / `VerifyActiveIntegrity` / `InstalledIntegrityEvidence` referenced only in the two new source files, the new test file, and the verifier tool's string lists. No runtime entry path constructs validation authority or consumes the projection.
- **P4-neutral (provisional).** The implementation diff under `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/` and `Installed/InstalledFileSystem.cs` is **empty**; the existing qualified verification is byte-identical. No new production caller of a protected sink; no change to F01/F02, S10/S11 domination, S28, downgrade paths, first protected effects, or RELEASE refusal. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 native skips are preserved.
- Confirming P4-neutrality for the exact revision is an independent-qualification obligation (owner decision Q-E5); this record claims only that the diff touches no P4 sink.

## 9. Final local checks

| Command | Result |
|---|---|
| `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Debug` | Build succeeded; 0 warnings / 0 errors |
| `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release` | Build succeeded; 0 warnings / 0 errors |
| `dotnet test ... --filter FullyQualifiedName~PrepublicationIntegrityProjectionTests` (Debug) | Passed: 17, Failed: 0, Skipped: 0 |
| `dotnet test ... --filter "FullyQualifiedName~Installed|~Authority|~ReleaseGenerationGate|~FinalAcceptance"` (Debug) | Passed: 232, Failed: 0, Skipped: 32 |
| `dotnet test ...` full M1 suite (Debug) | **Passed: 1385, Failed: 0, Skipped: 331** (1716) |
| `eng/VerifyProductionAssembly --production` (Release M1) | **RESULT: PASS** — 0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories |
| `eng/VerifyProductionAssembly --production` (Debug M1, negative control) | RESULT: FAIL (exit 1) — includes `debug-only authority factories remain: ... PrepublicationIntegrityProjection::ForTest ...` |
| `eng/test-production-build.ps1` | Positive Release/Debug inspection probes passed; stopped at the `missing-symbol` negative-control step (see limitation below) |

Build identities: Debug M1 SHA-256 `2AE1D393F88C05DBA1342EF4DC805926AB8B6233E9D0556AC53CC5753F4CF2C8` (MVID `8152d5aa-7c82-4dbe-8d2f-f6ed6835fe41`); Release M1 SHA-256 `78D053CCA229D317A7DA8AD07FD3652BA1A6685A3C090E18E080048A404CED05` (MVID `ec5381da-e67c-4a5f-8c18-4a24f0d064a7`).

The full-suite count equals the accepted reference 1368 passed plus the 17 new E-1 rows; the 331 skips are unchanged and are **not** passes.

## 10. Call-site / conversion audit

- No method outside the projection type returns or accepts `PrepublicationIntegrityProjection` (deterministic REFLECT test + working-tree grep).
- No conversion from a projection to `InstalledResult`, `LoadedInstallation`, `OwnerAuthorityContext.*`, `FinalAdmissionVerifier.Grant` or `ProductionActivationAuthorization` exists (REFLECT test).
- `OwnerAuthorityContext.Validation` remains factory-less; `Qualified.ForTest` remains Debug-only; `InstallationIdentity.Validate` is unchanged.

## 11. Cleanup evidence

E-1 acquires no protected resource and launches no process: the verifier performs read-only descriptor-relative reads only. The deterministic tests create no protected store. Cleanup status: **not applicable / no test-owned resource** for the executed rows. Native partial-resource cleanup remains Slice N and is not claimed.

## 12. Findings addressed

- **E-1 construction boundary** — Implemented / Locally tested.
- **Entry review Q-E5 (P4 re-audit)** — Evidence improved: the E-1 diff is P4-neutral at the sink level; independent confirmation for the exact revision is deferred to qualification.
- **Entry review Q-E1/Q-E2/Q-E3/Q-E4** — Still open (frozen-schema provenance, validation-catalog approval, atomic enablement, ordering inversion). E-1 does not touch them.
- **Runtime loaded-role verification** — Deferred to the authenticated-session packages.

## 13. Qualification boundary (what this evidence does NOT establish)

- It does not establish a protected/native positive; `Verify()` success is `NATIVE-PENDING`.
- It does not establish any validation authority, `OwnerAuthorityContext.Validation` construction, or positive validation path.
- It does not implement K/M/F/S, GAVA1, v3-pre, the root authorizer, the launcher/driver, custody/privilege drop, or the R/O/C ordering change.
- It does not prove filesystem confinement, native lifecycle, cleanup, or Release qualification of the wider product.
- Reflective construction is outside the accepted threat model.
- `eng/test-production-build.ps1` did not complete end to end: every positive RELEASE/Debug inspection probe passed, then the run stopped at the `missing-symbol` negative-control step because the script's PowerShell 5.1 logging round-trip did not match its own diagnostic string. The probe's behavior is correct (exit 1, emits `GAGAMBA002`), reproduced manually; `eng/test-production-build.ps1` and `Directory.Build.targets` are unchanged by this slice. `pwsh` is unavailable in this environment. This is recorded as an environment limitation, not an E-1 failure.

## 14. Source binding

- Baseline `c7b7621…` (tree `183de77f…`) is the exact starting revision; no non-`docs` change existed between the P4 reconciliation source and this baseline (per the entry owner decision §2).
- The implementation commit `2259fce…` (tree `77610b49…`) is the exact source this evidence proves. Documentation is committed separately, so the doc commit does not alter the proven revision.
- Working tree at evidence time: tracked/index clean except the documentation being added; 44 historical untracked paths preserved.

## 15. Independent reviewer instructions

A separately commissioned reviewer (`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-Q1`) must construct original adversarial cases and examine the construction boundary independently, not merely reproduce these tests. Required checks are listed in the companion handoff at `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-impl-01.md` and mirror the owner decision §8.2.
