# Slice E-1 review handoff — non-forgeable prepublication integrity projection

Date: 2026-11-07 (Asia/Manila). Producer disposition: **IMPLEMENTED WITH RESTRICTIONS — QUALIFICATION REQUIRED**.

Review implementation/tests `2259fce59b44b6af6448a38685a0635d560d0900`, tree `77610b49ecae5398d633f1db43d4f01604694ee1`, against baseline `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e`, tree `183de77fc703bffcd36167541be95e720c9b8d28`. Documentation is committed separately; the doc commit does not alter the proven revision. This handoff starts no independent review, owner acceptance, or later package.

Read the durable evidence first: [E-1 implementation evidence](../evidence/rc2b-06-b6-alt-b-slice-e-01-impl-01.md).

## Exact change and claim

Only three source/test files are new and one tracked verifier file changed:

- New `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs`: the immutable internal projection (private constructor, null qualification association, explicit provenance), its single verified production route `Verify()`, a Debug-only `ForTest` seam, and the `InstalledIntegrityEvidence` value.
- New `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.Prepublication.cs`: `VerifyActiveIntegrity()` — an isolated mirror of `VerifyActive`'s fixed-root reads plus the accepted `CheckHost` baseline, returning evidence only.
- New `tests/Gagamba.Execution.Linux.M1.Tests/Installed/PrepublicationIntegrityProjectionTests.cs`: 17 deterministic rows.
- `eng/VerifyProductionAssembly/Program.cs`: forbids `PrepublicationIntegrityProjection::ForTest` in RELEASE and adds the projection to the preserved core types.

**Claim:** E-1 provides one internal, non-forgeable, refusal-only prepublication integrity boundary, production-unreachable and incapable of validation/admission/execution authority. It is not Slice E completion and does not enable positive validation.

## Producer execution and reproduction limits

All counts are producer-executed on Windows 10.0.26200 (SDK 10.0.401), on the exact committed source:

- E-1 tests 17/17; focused authority/installed/release filter 232 passed / 32 skipped / 0 failed; full M1 suite **1385 passed / 331 skipped / 0 failed** (1716) = accepted 1368 + 17 new; the 331 skips are **not** passes.
- Debug/Release M1 builds 0 warnings / 0 errors.
- RELEASE production-assembly verifier PASS (0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories); `--production` on the Debug assembly FAILS and names `PrepublicationIntegrityProjection::ForTest` (negative control).
- `eng/test-production-build.ps1` passed all positive Release/Debug inspection probes, then stopped at its `missing-symbol` negative-control step due to a PowerShell 5.1 log-encoding/string-match artifact; the probe behavior is correct (exit 1, `GAGAMBA002`) and reproduced manually. `pwsh` is unavailable. Script and build targets are unchanged by this slice.
- No native run, no provisioning, no protected positive. `Verify()` success is `NATIVE-PENDING`.
- The runtime loaded-assembly ("verification-to-use") role check is deferred; the projection binds the manifest-derived role-closure identity.

## Required independent challenges

1. **Construction boundary.** Independently establish that the projection has exactly one RELEASE construction route, that it re-enters the accepted verification, and that no caller-supplied digest/identity/manifest/`InstalledResult`/Boolean/claimed result/injectable verifier can produce trusted state. Visibility alone is insufficient; look for indirect or reflective construction.
2. **Non-forgeability under mutation.** Author adversarial cases beyond the producer's (e.g., inconsistent evidence through the Debug seam, spliced manifests, wrong role closures, changed `active`/generation) and confirm fail-closed behavior with no trusted partial result.
3. **Promotion refusal.** Confirm a projection cannot be promoted to `OwnerAuthorityContext.Validation`/`Qualified`, `LoadedInstallation`, `InstalledResult`, a final-admission grant, or activation authority, and that `OwnerAuthorityContext.Validation` remains factory-less.
4. **Production reachability.** Audit the full caller graph (not just type signatures): prove no production entry path constructs or consumes the projection, and that E-1 added no sink caller.
5. **P4 neutrality for the exact revision.** Verify the diff leaves F01/F02, S10/S11 domination, S28, downgrade paths, first protected effects, and RELEASE refusal unchanged, and that F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 skips are preserved.
6. **RELEASE separation.** Reproduce the RELEASE verifier PASS and the Debug negative control, and confirm the Debug-only seam and any test provenance cannot leak into RELEASE.
7. **Schema honesty.** Confirm E-1 assumes or implements no K/M/F/S, GAVA1, v3-pre or approval-chain schema, and no `OwnerAuthorityContext.Validation` factory.
8. **Inert/limitation correctness.** Assess whether the deferred runtime loaded-role check and the native-pending positive are honestly represented, and whether the producer's `IMPLEMENTED WITH RESTRICTIONS` label is correct.

## Reproduction

Build the M1 project (Debug/Release) and the tests; run the E-1 filter and the full M1 suite; build `eng/VerifyProductionAssembly` and run `--production` against `src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll` (expect PASS) and against the Debug DLL (expect FAIL naming the E-1 seam). Exact commands and results are in the evidence record §9.

## Native and repository limits

Native protected positive/refusal requires a separately authorized disposable host and remains Slice N. Preserve one canonical main worktree, all 44 historical untracked paths, prior evidence and accepted owner decisions. Do not create branches/worktrees, reset/clean/rebase/prune, push, provision, activate, or begin Q1/E-2+ automatically. A separate owner acceptance is required after independent qualification; neither implementation completion nor a passing suite accepts E-1 or opens E-2.
