# RC2A-02B independent review handoff

Date: 2026-10-25 (Asia/Manila). Handoff author: RC2A-02B implementation agent.
**Independent review has not occurred.** This document prepares the independent
review required before any RC2A-02B acceptance decision; it does not claim or
pre-judge that review.

## Exact identities

| Item | Identity |
|---|---|
| Repository metadata | `C:\Users\Laszlos\source\repos\Gagamba` (canonical `.git`; review from a clean checkout) |
| Qualification branch | `qualification/gagamba-rc2a-02b` |
| Qualification worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02b` |
| Start commit (accepted baseline) | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`, tree `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` |
| Accepted qualification evidence | `c1a01761f8d58136a381ad87269635c6c14ad093`, tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19` |
| Tested implementation | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` |
| Preserved RC2A-02A review | `7b6344551c2d4b42832df39822eaf4b8a1c01495` on `review/gagamba-rc2a-02a-glm-q1` (unmerged) |
| `src` / `tests` / `native` | `b8c167e3d7340a562221591bdc2b6df7adb372d1` / `5c3268286575cb55891fae9288627657f2d30ddf` / `85631d2f04512376621d3d09f51d67fd90f45335` |
| This evidence commit | documentation-only commit on the qualification branch; identity is in the completion report |

## What to review

1. **Re-execute** the declared commands from a clean checkout of the evidence
   commit on Windows (primary platform), using the precise filter:
   - build: `dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release -warnaserror`
   - public: `dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release`
   - required set: `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.'`
   - repeat the required set for determinism.
   Expected: build 0/0; public 67/0/0; required 457/0/0 twice with identical
   name/outcome multisets.
2. **Undertake a second-platform run** if a host is available (native Linux or
   macOS). This qualification ran WSL2 Ubuntu (labeled `wsl2`) and reproduced
   the same 67/67 and 457/457 multisets. WSL2 is **not** native Linux; native
   Linux and macOS were **not run** (no hosts). A reviewer with such a host
   should run the same precise command set natively and record it separately.
3. **Verify evidence integrity**: recompute `SHA256SUMS`; recompute the
   LF-normalized document hashes in `document-sha256.json`; confirm the raw TRX
   outcome counts in `runs/windows/` and `runs/wsl2/` match `results.json`
   (67/0/0; 457/0/0; 457/0/0; 473/0/41 on Windows only).
4. **Verify source binding**: both executed sources are `e5fff9e`; the Windows
   worktree was clean, and the WSL2 clone was checked out at that commit and
   clean at run time (the clone's `origin` URL is set to the canonical GitHub
   URL solely for SourceLink; no fetch occurred). Confirm `src`/`tests`/`native`
   subtree identities equal the accepted RC2A-01/RC2A-02A identities and that no
   source or test file changed in the evidence commit.
5. **Review the two scope decisions** recorded in the README §4: the precise
   filter is used for qualification on every platform while the literal plan
   filter runs only on Windows (Q2A02A-F02 context); and `e5fff9e` (owner
   acceptance tip) is used as the accepted baseline, permitted by plan §9.1.
6. **Review the preserved setup failures** under `runs/wsl2/`
   (`setup-attempt1-archive-sourcelink.*`, `setup-attempt2-local-origin.*`).
   Both are SourceLink metadata/environment issues, resolved before the
   qualification runs; neither indicates a product defect.
7. **Confirm the open obligations** were not absorbed: Q2A-S / RC-D04 remains
   OPEN (not part of RC2A-02B); native Linux and macOS remain NOT RUN; native
   confinement, package/clean-consumer/mixed-graph, AE4-B01 and Hufu remain
   unqualified. Full RC2A-02 remains NOT YET ACCEPTED.

## Producer claims to verify

| Claim | Producer evidence |
|---|---|
| Windows build 0 warnings / 0 errors | `runs/windows/build-execution.log` |
| WSL2 build 0 warnings / 0 errors | `runs/wsl2/wsl2-build-execution.log` |
| Windows public 67/0/0 | `runs/windows/windows-public.trx` |
| Windows required set 457/0/0, rerun 457/0/0 | `runs/windows/windows-required-set*.trx` |
| WSL2 public 67/0/0 | `runs/wsl2/wsl2-public.trx` |
| WSL2 required set 457/0/0, rerun 457/0/0 | `runs/wsl2/wsl2-required-set*.trx` |
| Cross-platform test-name/outcome multisets identical | TRX files; comparison reproducible by parsing `testName`/`outcome` |
| Windows literal plan filter 473/0/41 | `runs/windows/windows-plan-filter.trx` |
| No production/test/native source change | `src`/`tests`/`native` identities; `source-binding.json` |
| Only documentation and evidence added | commit name-status inventory; `source-binding.json` |

## Explicit non-claims

Independent review; native Linux or macOS qualification; WSL2-as-native;
native sandbox confinement; package publication; clean-consumer or mixed Hufu
graph qualification; binary reproducibility; RC2A-03/04; AE4-B01 closure; Hufu
resumption; Q2A-S/RC-D04 completion; any owner acceptance of RC2A-02B or full
RC2A-02.
