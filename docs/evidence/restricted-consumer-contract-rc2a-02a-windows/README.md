# RC2A-02A Windows qualification evidence

Date: 2026-10-25 (Asia/Manila). Disposition: **IMPLEMENTATION AND WINDOWS
EXECUTION COMPLETE — READY FOR INDEPENDENT REVIEW (QUALIFICATIONS DISCLOSED)**.
This assignment does not perform or claim independent review, package
qualification, native Linux/macOS qualification, RC-2B work or Hufu resumption.

## 1. Scope and baseline

This slice implements the Windows-executable portions of the accepted RC2A-02
qualification plan for families Q2A-V (canonical vectors), Q2A-F (72-field
mutation coverage), Q2A-B (boundaries and review follow-ups) and Q2A-E
(evidence integrity). It changes **test source only**.

| Item | Exact identity |
|---|---|
| Qualification start (owner-acceptance commit) | `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e`, tree `6e94bee4c39801dfa256c158b300b066c0f2f217` |
| Qualification branch | `qualification/gagamba-rc2a-02a-windows` |
| Worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-windows` |
| Test source commit (executed) | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` |
| `src` tree at test source | `b8c167e3d7340a562221591bdc2b6df7adb372d1` (= accepted RC2A-01; **no production change**) |
| `tests` tree at test source | `5c3268286575cb55891fae9288627657f2d30ddf` |
| `native` tree at test source | `85631d2f04512376621d3d09f51d67fd90f45335` (= accepted RC2A-01; unchanged) |
| Qualification commits | `bd553ec` (harness) → `18aa0db` (vectors/A5) → `49b5eb4` (72-field matrix) → `71a28af` (boundaries/exports/follow-ups) |
| Accepted RC2A-01 object | `68b01fbb53eefca9c8ca2f83ff50641521020a2a`, tree `54f659d01937da29bc9a5f82566a8349af264664` |
| Preserved independent RC2A-01 review | `8e41babfc96afa2e617a53b63e50903d54001194`, blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`, byte SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` |
| Frozen API contract | `docs/design/restricted-consumer-contract-rc2a-01-api.md`, blob `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` (now also bound by LF-normalized SHA-256 below) |

The worktree was clean immediately before the runs; `git status --porcelain`
was empty during and after every run. The evidence commit is documentation-only;
its identity is recorded in the completion report to avoid a self-referential
hash. Source identity binding, per-file blob identities and hashes are in
[`source-binding.json`](source-binding.json).

## 2. Environment

| Item | Value |
|---|---|
| OS | Microsoft Windows NT 10.0.26200.0 (Windows 11), AMD64 |
| Machine | `LAPTOP-KBD8D8JG` |
| .NET SDK | 10.0.401 |
| .NET runtime used by tests | Microsoft.NETCore.App 10.0.12 |
| Test framework | xUnit v3 3.2.0, `xunit.runner.visualstudio` 3.1.4, Microsoft.NET.Test.Sdk 18.0.1, `net10.0` |
| Build | Release, warnings as errors (`-warnaserror`): 0 warnings / 0 errors |

Raw capture: [`runs/environment.txt`](runs/environment.txt). No native fixture,
restricted owner, helper, sandbox or workload was started; these are pure
managed-code value/codec tests.

## 3. Claims, probes and outcomes

| Claim | Probe | Outcome |
|---|---|---|
| C1. The preserved 873-byte golden vector is unchanged and independently digest-bound | Literal copy of the committed RC2A-01 hex; rebuild with the independent wire template; BCL SHA-256 | **SUPPORTED** — 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`, byte-identical rebuild |
| C2. Canonical vectors are independently generated and decode to the committed graph | 23 vectors: committed literal hex = raw-template rebuild = independent `ReferenceEncoder` output; public decode compared property-by-property against committed template values; SHA-256 recomputed by BCL | **SUPPORTED** — 23/23 vectors; all digests and graphs match; production `Encode(Decode(x)) == x` |
| C3. The decoder refuses malformed/noncanonical input with the documented code and exact offset | 78-entry A5 catalog with construction-derived offsets, canonical positive counterparts | **SUPPORTED** — 79/79 catalog tests pass; every code/offset matched |
| C4. Every one of the 72 manifest fields is mutation-mapped, not incidentally covered | Committed field-map order; per-field kind and applicability table; 279 cases; matrix self-tests rejecting fabricated class coverage | **SUPPORTED** — 72/72 fields; 279/279 cases pass; non-applicable classes explicitly recorded |
| C5. Valid mutations are accepted, change bytes/digests, and remain visible in the decoded view | 162 valid-row cases through real validating constructors; independent reference-encoder equality; graph compare; domain checks | **SUPPORTED** — 162/162 |
| C6. The 65,536-byte ceiling is symmetric and inclusive; oversize is refused before parsing | 65,535/65,536/65,537/1,000,000; legacy encoder positive control; bounded encode/projection/decode | **SUPPORTED** — 65,536 accepted; 65,537/1,000,000 refused `InputTooLarge@0`; bounded encode and projection throw `ArgumentException("manifest")` |
| C7. The 16,384-byte combined argv/environment ceiling is measured in UTF-8 bytes | 16,383/16,384/16,385; multibyte shape; direct-wire 16,385 | **SUPPORTED** — inclusive acceptance at 16,384; 16,385 refuses construction and (on the wire) `InvalidValue` at the invocation end |
| C8. Empty argv item, zero-length argv list and empty environment value are distinct | Constructor and wire cases; zero-count bytes; empty value vector | **SUPPORTED** — empty item refuses `InvalidValue`; empty list is valid and encodes a zero count; empty value is a valid present value |
| C9. Digest and identifier behavior matches the clarified contract | Lengths 0/31/32/33/64/65,536; matching/changed/all-zero; exact ordinal `Supports`; null ID/manifest | **SUPPORTED** — including the explicit combined `VerifySha256(null, wrong-length)` `ArgumentNullException` (R2A-F04) |
| C10. No single-byte mutation can make the decoder accept a non-self-consistent image | Full 873 × 255 = 222,615 mutation sweep; every acceptance re-encoded byte-exactly; zero `InvalidValue@0` | **SUPPORTED** — 114,378 acceptances all canonical; 108,237 refusals with in-range offsets; see finding Q2A02A-F01 for the corrected plan characterization |
| C11. Every proper golden prefix refuses without a partial view | 873-prefix sweep with error presence, offset range, `Truncated` end-of-input rule | **SUPPORTED** |
| C12. R2A-F01's re-encode branch is assessed and not artificially forced | Source-level control-flow analysis plus the full sweep/corpus probe | **SUPPORTED (assessment)** — unreachable failure path under the accepted model; exercised as a pass-through on every accepted decode; no production hook |
| C13. Evidence is bound to the exact committed source and machine results | Commit/tree/subtree identities, per-file blob hashes, raw TRX/log hashes, deterministic rerun, environment capture | **SUPPORTED** — see [`source-binding.json`](source-binding.json), [`test-results.json`](test-results.json), `SHA256SUMS` |
| C14. The preserved RC2A-01 implementation still satisfies its accepted contract | Existing 38 internal + 57 public regressions executed alongside | **SUPPORTED** — 95/95 pass |

## 4. Exact commands and results

All commands ran from the qualification worktree at test source
`71a28afe2623516fb4176e6a4ce7988a3d70df06`, with:

```text
GAGAMBA_RC2A02A_SOURCE_COMMIT=71a28afe2623516fb4176e6a4ce7988a3d70df06
GAGAMBA_RC2A02A_EXPORT_DIR=<worktree>\artifacts\rc2a-02\windows\exports
```

```powershell
dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=windows-public.trx' --results-directory artifacts/rc2a-02/windows/run-final --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Qualification' --logger 'trx;LogFileName=windows-internal.trx' --results-directory artifacts/rc2a-02/windows/run-final --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.' --logger 'trx;LogFileName=windows-required-set.trx' --results-directory artifacts/rc2a-02/windows/run-final --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.' --logger 'trx;LogFileName=windows-required-set-rerun.trx' --results-directory artifacts/rc2a-02/windows/run-final --blame-hang-timeout 60s
```

| Run | Role | Passed | Failed | Skipped | Total | Exit |
|---|---|---:|---:|---:|---:|---:|
| Build | Release Execution, warnings as errors | — | 0 | — | — | 0 |
| R1 | Exact committed-plan public run | 67 | 0 | 0 | 67 | 0 |
| R2 | Exact committed-plan internal run | 473 | 0 | 41 | 514 | 0 |
| R3 | Precise required set (RC2A-01 regressions + `Tests.Qualification.`) | 457 | 0 | 0 | 457 | 0 |
| R4 | Deterministic rerun of R3 | 457 | 0 | 0 | 457 | 0 |

- R1 = 57 existing public regressions + 10 new public boundary cases.
- R3 = 38 existing internal contract regressions + 419 new qualification cases.
- R2's 41 skips are pre-existing Linux-native classes selected by the committed
  `~Qualification` substring; none is an RC2A-02A case (finding Q2A02A-F02).
- R3 and R4 have an identical test-name/outcome multiset; matrix and vector
  export bytes are identical across reruns (sweep export differs only in its
  recorded `durationMs` observation).
- Per-test names, outcomes, durations, counters, TRX/log paths and SHA-256
  hashes are in [`test-results.json`](test-results.json). Raw TRX/log/exit
  files are under [`runs/`](runs/).

## 5. Vector evidence (Q2A-V)

- **Golden**: 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`, preserved literal; verified by an independent raw template rebuild and by BCL SHA-256.
- **23 vectors** with committed literal hex and SHA-256: minimal manifest; empty/nonempty namespace lists; empty argv list; empty environment; empty environment value; absent/exact-zero/unlimited budgets; inode budget present/absent (and both in one manifest); `NoNewPrivs=false`; `RuntimeDumpsDisabled` true/false; core pattern present/absent (including NUL and supplementary plane); supplementary-plane and non-ASCII UTF-8; exactly 16,384 combined argv/environment bytes; exactly 65,536 complete bytes; duplicate argv; empty closure.
- Each vector is cross-checked three ways: committed literal bytes = independent raw-template build = independent `ReferenceEncoder` output. Production decode is compared property-by-property against committed template values; the production re-encode is compared byte-for-byte. Digests are recomputed with `System.Security.Cryptography.SHA256`, never with the production digest API.
- Machine-readable provenance: [`exports/vector-manifest.json`](exports/vector-manifest.json).
- **A5 refusal catalog**: 78 named entries covering truncated prefixes, trailing data, 65,537/1,000,000-byte input, empty input, inflated counts/lengths (including a reduced declared length), malformed UTF-8 (overlong/surrogate/out-of-range), unknown tags, unsorted sets, adjacent duplicates, empty non-allowed strings, all forced contradictions, unsupported schema (0/2/65,535) and implementation variants. Every entry asserts the exact code and construction-derived offset and has a canonical positive counterpart.

## 6. Field mutation evidence (Q2A-F)

- 72/72 committed field-map rows, in order, each with a declared field kind and
  an explicit applicable/non-applicable class table.
- 279 cases: valid 51, boundary 70, presence 41, tag 15, forced 12, order 6,
  duplicate 6, length 19, utf8 18, cross 16, empty 14, supported-id 2, misuse 1,
  trace 8. Row classes: valid 162, malformed 68, noncanonical 40, misuse 1,
  trace 8.
- Valid/boundary/presence cases go through the real validating constructors and
  assert independent reference-encoder equality, canonical byte/digest change
  (where required), decode success, complete graph projection and an explicit
  domain observation. Refusal cases assert exact code and offset and a canonical
  counterpart.
- Composite trace cases bind each public view type's shipped property inventory
  to its committed subfield list, and the matrix self-tests reject fabricated
  class coverage and orphan fields.
- Machine-readable matrix: [`exports/mutation-coverage-matrix.json`](exports/mutation-coverage-matrix.json).

## 7. Boundary and sweep evidence (Q2A-B)

- Manifest size 65,535/65,536 accepted with full graph and re-encode; 65,537
  refused by the bounded writer, the projection guard and the decoder
  (`InputTooLarge@0`); 1,000,000 refused pre-parse; 65,537 positive control from
  the legacy unbounded internal encoder only.
- Combined argv/environment: 16,383 and 16,384 accepted (including a multibyte
  shape proving bytes-not-characters); 16,385 refused at construction and
  directly on the wire (`InvalidValue` at the invocation end).
- Empty argv item (`InvalidValue`/`ArgumentException`) versus zero-length argv
  list (valid, zero count) explicitly distinguished; empty environment value is
  valid.
- Digest: lengths 0/31/33/64/65,536 false; 32 matching true; changed/all-zero
  false; combined null + wrong length throws `ArgumentNullException` (R2A-F04).
- Truncation sweep: all 873 proper prefixes refuse without a partial value;
  `Truncated` offsets equal end-of-input; no `InvalidValue` observed.
- Single-byte sweep: 222,615 mutations; 114,378 accepted, 108,237 refused;
  576 survivor byte positions; every accepted mutation re-encoded byte-exactly;
  zero `InvalidValue@0`; refusal code histogram and survivor profile preserved
  in [`exports/flip-sweep-survivors.json`](exports/flip-sweep-survivors.json).
  The plan's literal "no mutation outside digest fields is accepted" row was
  disproved and corrected; see finding Q2A02A-F01.
- Machine-readable boundary rows: [`boundary-results.json`](boundary-results.json).

## 8. Evidence integrity (Q2A-E)

- [`source-binding.json`](source-binding.json): baseline/test-source commits and
  trees, src/tests/native subtree identities, commit list, per-file Git blob and
  SHA-256 identities for all 18 added test files, production-change=false basis.
- [`test-results.json`](test-results.json): environment, build command/exit/log
  hash, four runs with exact command lines, exit codes, counters, timings, full
  per-test outcomes, TRX/log hashes, export hashes, rerun comparison, failed
  attempt record and the reproducibility/qualification distinctions.
- [`document-sha256.json`](document-sha256.json): LF-normalized SHA-256 binding
  for the clarified API contract (R2A-F02), owner acceptance, RC2A-02 plan
  (including its appended correction), the RC2A-01 field map and evidence,
  implementation plan, queue/handoff, this evidence report set, and the
  preserved independent review identity (bound by exact Git blob bytes; not
  checked out in this worktree).
- [`boundary-results.json`](boundary-results.json), [`family-results.json`](family-results.json),
  [`exports/`](exports/): machine-readable manifests and observed profiles.
- `SHA256SUMS`: hashes of every committed evidence artifact (excluding itself).

## 9. Failed attempts, corrections and limitations

Preserved in [`runs/failed-attempts/`](runs/failed-attempts/) and the findings
register; these are **not** counted as passes:

1. First single-byte sweep run failed its initial eligibility classification at
   byte 157 (`runs/failed-attempts/b-first-sweep-failure.trx`). Analysis showed a
   legitimate canonical re-segmentation; the test now asserts canonical rigidity
   plus immutable-region containment, and the plan row was corrected
   (Q2A02A-F01).
2. Bootstrap emitter failure on vector V-19 (two-step invocation replacement
   temporarily exceeded the 16 KiB cap); the committed recipe constructs the
   invocation in one validated call.
3. A5 reduced-mode-length case authoring error (a consistent shortened mode is
   valid and was accepted); corrected to an inconsistent declared length. No
   production behaviour or expected refusal was changed.

Known limitations and non-claims:

- **Windows only.** No native Linux or macOS run; RC2A-02B is not authorized and
  no cross-platform claim is made. WSL2 was not used.
- Managed-code value/codec tests only. No native fixture, sandbox, owner,
  helper, workload, package build, clean consumer or Hufu graph was exercised.
- R2's 41 collateral Linux-native skips are disclosed; the required set R3/R4 is
  zero-skipped.
- No binary reproducibility, package identity or deployment qualification is
  claimed in either direction (R2A-F03 disclosure retained).
- The single-byte sweep's survivor containment is compared against a documented
  immutable-region map; the complete survivor profile is evidence, not an
  exact-equality expectation on the full 576-position set.
- The defensive re-encode branch assessment is source-level plus empirical over
  the committed corpus; it is not a formal proof and no artificial hook was
  added.

## 10. Cleanup and final state

- Only test source and documentation were added; `src` and `native` trees remain
  identical to the accepted RC2A-01 identities.
- No test process, `testhost`, `vstest` or workload remained after the runs; only
  standard MSBuild node-reuse workers (ordinary build infrastructure) were
  present.
- No production fix, API change, package build/pack/publish, merge, push, tag or
  Hufu action occurred in this assignment.
- The worktree contained only the untracked evidence directory before the final
  evidence commit.

## 11. Review status

Independent review has **not** occurred. This evidence set is the handoff for
the independent GLM review specified by RC2A-02 §3.F; see
[`reviewer-handoff.md`](reviewer-handoff.md). RC2A-02A is not complete until
that review accepts the evidence and the owner dispositions any findings.
