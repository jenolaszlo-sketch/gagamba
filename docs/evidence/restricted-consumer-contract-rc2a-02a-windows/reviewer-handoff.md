# RC2A-02A independent review handoff (GLM)

Date: 2026-10-25 (Asia/Manila). Handoff author: implementation agent.
**Independent review has not occurred.** This document prepares the independent
GLM review required by RC2A-02 plan §3.F; it does not claim or pre-judge that
review.

## Exact identities

| Item | Identity |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` (canonical metadata; review from a clean checkout of the branch below) |
| Qualification branch | `qualification/gagamba-rc2a-02a-windows` |
| Worktree used | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-windows` |
| Qualification start | `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e`, tree `6e94bee4c39801dfa256c158b300b066c0f2f217` |
| Test source commit (executed) | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` |
| Source/test commit chain | `bd553ec` → `18aa0db` → `49b5eb4` → `71a28af` |
| Evidence commit | the documentation-only commit on the same branch after `71a28af`; identity is in the completion report (avoids self-reference) |
| Baseline accepted implementation | `68b01fbb53eefca9c8ca2f83ff50641521020a2a`, tree `54f659d01937da29bc9a5f82566a8349af264664` |
| Preserved RC2A-01 review | commit `8e41babfc96afa2e617a53b63e50903d54001194`, blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`, SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` |
| Accepted plan | `docs/plans/restricted-consumer-contract-rc2a-02-qualification.md` at the start commit, plus the appended RC2A-02A correction section |

Production source is unchanged: `src` tree `b8c167e3d7340a562221591bdc2b6df7adb372d1`
and `native` tree `85631d2f04512376621d3d09f51d67fd90f45335` equal the accepted
RC2A-01 identities at both the start and test-source commits.

## What to review

1. **Re-execute** the declared commands from a clean checkout of the evidence
   commit (or `71a28af` for the source-only subset), on Windows:
   - build: `dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror`
   - public: `dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --blame-hang-timeout 60s`
   - exact-plan internal command (expect 473/0/41: 41 pre-existing Linux-native
     skips selected by the committed `~Qualification` substring)
   - precise required set: replace the last filter segment with
     `FullyQualifiedName~Tests.Qualification.` (expect 457/0/0).
   Record exact commands, SDK/runtime and outcomes; producer counts are in
   [`test-results.json`](test-results.json).
2. **Recompute vectors and digests** independently of the committed data:
   - decode the 23 committed hex literals, recompute SHA-256 with any tool, and
     compare to `exports/vector-manifest.json`;
   - confirm the preserved golden is 873 bytes with SHA-256
     `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`;
   - confirm the expected bytes are reproducible from the committed
     construction recipes and the independent `ReferenceEncoder`, not from the
     production encoder.
3. **Verify the 72-field matrix** against
   `docs/evidence/restricted-consumer-contract-rc2a-01/field-map.md`: 72/72
   fields, no fabricated class coverage, non-applicable classes have reasons,
   composite view property inventories match.
4. **Verify refusal codes/offsets against the decoder source**, focusing on the
   construction-derived offsets (not decoder observations): truncated prefixes
   end at input length, local refusals at offending field starts, cross-field
   invariants at the documented reader positions, unsupported schema/
   implementation at 0/2.
5. **Review the sweep finding and correction**: the plan's B6 literal claim was
   disproved (114,378 accepted single-byte mutations; canonical re-segmentation
   example byte 157 → `0x3B`); confirm the corrected assertion (canonical
   rigidity + immutable-region containment) and the appended plan correction.
6. **Review the R2A-F01 assessment** against the source control flow and the
   empirical corpus; confirm no artificial production hook exists and the
   branch's failure path was not observed.
7. **Review evidence hygiene**: source binding, document inventory (including
   the R2A-F02 API-document rebinding), failed attempts, limitations, the
   test-versus-binary reproducibility distinction (R2A-F03), and `SHA256SUMS`.

## Producer claims to verify

| Claim | Producer evidence |
|---|---|
| 419 new qualification cases + 38 RC2A-01 internal regressions pass with zero skips (457/0/0) and reproduce identically on rerun | R3/R4 TRX, `test-results.json` |
| 67 public cases pass, including 10 new boundary/digest/misuse cases | R1 TRX |
| Release build 0 warnings / 0 errors | `runs/build-execution.log` |
| 23 canonical vectors, all three representations byte-identical; all digests independently recomputed | `exports/vector-manifest.json`, R3 rows |
| 72/72 fields and 279 mutation cases with explicit class applicability | `exports/mutation-coverage-matrix.json`, R3 rows |
| 873-prefix truncation sweep and 222,615-mutation single-byte sweep; 114,378 accepted all canonically self-consistent; zero `InvalidValue@0` | R3 rows, `exports/flip-sweep-survivors.json` |
| No production source change | `src`/`native` tree identities; `source-binding.json` |

## Findings and qualifications carried to review

- **Q2A02A-F01** (TEST EXPECTATION ERROR): plan B6's literal survivor claim is
  wrong; corrected characterization committed; no production defect.
- **Q2A02A-F02** (RECOMMENDATION): exact plan filter also selects 41
  out-of-scope Linux-native skips on Windows; precise required-set run is
  zero-skipped.
- **Q2A02A-F03** (TEST EXPECTATION ERROR, authoring): three test-authoring
  corrections preserved (V-19 construction, A5 reduced-length defect, sweep
  classification); no product impact.
- **Q2A02A-F04** (ENVIRONMENTAL LIMITATION): Windows managed-code only; no
  native/package/cross-platform claim.
- R2A-F01/F02/F04 addressed; R2A-F03 disclosure retained.
- No confirmed production defect was found.

## Explicit non-claims

Independent review; RC2A-02B portability; native Linux/macOS qualification;
package publication; clean-consumer or mixed Hufu graph qualification; RC-2B
R1-F02/F03; AE4-B01 closure; Hufu resumption; production/native restricted
execution; binary reproducibility; any change to the accepted RC2A-01 API.
