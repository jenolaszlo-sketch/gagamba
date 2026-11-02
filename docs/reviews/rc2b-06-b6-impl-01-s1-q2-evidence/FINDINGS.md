# FINDINGS.md — Q2 findings, severities, and smallest required actions

## Q2-OBS-01 — Remote main tip advanced one docs/harness commit beyond the reviewed receipt (during review window)
- **Severity:** OBSERVATION
- **Source reference:** remote `main` = `401e1ea7f0be06b27bc188c827dd951016b3c62a`; parent = `dce71ac` (receipt). Files: `.gitattributes`, `docs/PROJECT-STATUS.md`, `docs/evidence/ci-preparation-checkout-byte-identity-2026-11-02.md`, `docs/handoff.md`, `docs/work-queue.md`, `eng/tests/test_preparation_checkout.py`.
- **Expected:** remote main to point exactly at the reviewed final remediation receipt.
- **Actual:** remote main points one commit beyond it; the receipt and implementation are in ancestry. The commit is docs/harness-only (verified via GitHub API file list; no production/test/build-boundary source).
- **Consequence:** acceptance would implicitly include an owner-authored, Q2-unreviewed (albeit inert) commit.
- **Smallest required correction:** owner decision at acceptance — bind S1 acceptance to `dce71ac` or explicitly fold `401e1ea7` into the accepted scope (recommended, given it is docs/harness-only and CI-green).
- **Effect on owner acceptance:** none material; a scoping decision only.

## Q2-OBS-02 — CI `preparation` job failure at the remediation receipt
- **Severity:** OBSERVATION
- **Source reference:** run 41325323331 (head `dce71ac`), job `preparation`, step "Verify preparation baseline" = failure; all other 9 jobs (incl. build windows/ubuntu, unit tests windows/ubuntu, pack) success. Follow-up run 41327109151 (head `401e1ea7`) fully green.
- **Expected:** fully green CI at the receipt.
- **Actual:** 9/10 green; the red job concerns preparation/checkout byte-identity of historical fixture/evidence bytes (fixed by Q2-OBS-01's commit).
- **Consequence:** none for the S1 build boundary (the Release solution and unit-test jobs passed at the receipt).
- **Smallest required correction:** none required for S1; the owner's fix is already merged at tip.
- **Effect on owner acceptance:** none material.

## Q2-OBS-03 — Cross-output-root binary hash variance
- **Severity:** OBSERVATION (declared boundary; not a defect)
- **Source reference:** identical sources produce different M1 Release SHA-256 across isolated `--artifacts-path` roots (`22804E14…`, `E6335C11…`, `7AFA96DB…`) while in-place/repeated builds are stable (`0FAFB4F7…`).
- **Expected/actual:** path-derived PE inputs (SourceLink/PDB references) vary with output roots; the reviewed implementation claims within-root stability only.
- **Consequence:** binary identity comparison must be root-consistent.
- **Smallest required correction:** none; S2 installed-trust work must not assume cross-root byte equality.
- **Effect on owner acceptance:** none.

## Q2-OBS-04 — `eng/test-production-regressions.ps1` requires the evidence directory inside the repository tree
- **Severity:** OBSERVATION (tooling usability; producer-reported counts were still independently reproduced)
- **Source reference:** the generated `ProductionRegression.csproj` inherits `Directory.Build.props` (for `ImplicitUsings`) only when created under the repo; an outside evidence directory yields CS0246 (`Guid`/`Task`/`TimeSpan`) and suite failure.
- **Independent reproduction:** reviewer's first supplemental run with an outside evidence dir failed exactly as described; rerun with the evidence dir inside the materialized tree passed 1,019/0/199.
- **Smallest required correction (future):** generate the project with explicit `<ImplicitUsings>enable</ImplicitUsings>` (or set it in the generated csproj) so the script is location-independent.
- **Effect on owner acceptance:** none (bounded tooling; the accepted entry points are unaffected).

## Q2-OBS-05 — `dotnet build --no-build` fails at the CLI layer in .NET 10 (informational)
- **Severity:** OBSERVATION
- **Source reference:** `evidence/c14a-no-build-valid.log` (MSB1001 Unknown switch `--no-build`), `evidence/c14c-msbuild-nobuild-valid.log` (NETSDK1085).
- **Consequence:** both no-build avenues fail closed; no verification gap. Recorded so the closed behavior is not misread as a Gagamba gate.
- **Smallest required correction:** none.

## Carried historical dispositions (unchanged, not silently closed)

| Finding | Disposition |
|---|---|
| Q1-FINDING-001 (MAJOR) | **CLOSED by REM-01; independently verified by Q2** (FINDING-001-REVIEW.md) |
| Q1-FINDING-002 (MAJOR) | **CLOSED by REM-01; independently verified by Q2** (FINDING-002-REVIEW.md) |
| Q1-FINDING-003 (MINOR, reporting) | historical; current independent inventory matches Q1's own 1,245/289 |
| Q1-FINDING-004 (MINOR, System.IO.Hashing) | open for S2; preserved (present via SourceLink; no dependency redesign attempted) |
| Q1-FINDING-005 (hash mismatch across environments) | OBSERVATION; reproduced as Q2-OBS-03 semantics |
| Q1-FINDING-006 (D6.4 explicit restore) | OBSERVATION; retained (GAGAMBA008 now explicitly guards the isolated-roots contract) |
| Inherited clock-constructor qualification | OPEN qualification; preserved, not remediated, not treated as passed |
| Native/WSL2/ICU/OpenSSL/zlib/host-runtime qualifications | NOT ATTEMPTED here; carried forward unchanged |

## Summary

| Severity | Count | Blocking |
|---|---|---|
| BLOCKER | 0 | — |
| MAJOR | 0 | — |
| MINOR | 0 | — |
| OBSERVATION | 5 (new) | No |

No new material qualification defect was established. The findings standard's bar for another remediation cycle is not met.
