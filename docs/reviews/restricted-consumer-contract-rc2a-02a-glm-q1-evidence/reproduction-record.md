# GLM Q1 review — compact reproduction record

Review ID: `RC2A-02A-GLM-Q1` · Worktree: `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-glm-q1-review` · Branch: `review/gagamba-rc2a-02a-glm-q1` · Base: `c1a01761f8d58136a381ad87269635c6c14ad093`

## Environment

- Microsoft Windows NT 10.0.26200.0, AMD64, machine `LAPTOP-KBD8D8JG`
- .NET SDK 10.0.401; Microsoft.NETCore.App 10.0.12
- xunit.v3 3.2.0, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 18.0.1, net10.0

## Executed commands and outcomes

| # | Command (from review worktree) | Outcome |
|---|---|---|
| 1 | `dotnet restore src/Gagamba.Execution/Gagamba.Execution.csproj` (clean checkout needs restore; evidence runs used `--no-restore` against an already-restored tree) | restored |
| 2 | `dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror` | 0 warnings / 0 errors |
| 3 | `dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --blame-hang-timeout 60s` | Passed 67, Failed 0, Skipped 0 |
| 4 | `dotnet restore tests/Gagamba.Execution.Tests/...; dotnet restore tests/Gagamba.Execution.Linux.M1.Tests/...` | restored |
| 5 | `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.' --logger 'trx;LogFileName=review-required-set.trx' --results-directory artifacts/rc2a-02/review --blame-hang-timeout 60s` | Passed 457, Failed 0, Skipped 0 |
| 6 | Same as (5) with `--no-build` and `review-required-set-rerun.trx` | Passed 457, Failed 0, Skipped 0; R3/R4 name+outcome multisets identical |
| 7 | Committed-plan internal filter (`...FullyQualifiedName~Qualification`) with `review-internal.trx` | Passed 473, Skipped 41, Total 514; skips = `NonRootQualificationTests` 31 + `OwnerDeathQualificationTests` 7 + `Wp05ReleasePathIndependentQualificationTests` 3 |
| 8 | `dotnet run -c Release --project review/GlmQ1Probes/GlmQ1Probes.csproj` | 103/103 checks pass, exit 0 |

## This review's TRX hashes (artifacts/rc2a-02/review/, regenerable; not committed)

- `review-required-set.trx` — `2b4a0790be1a60c6844e586b9512ec010d98f6e4c902a9f89a31e3cf63fac085`
- `review-required-set-rerun.trx` — `7d1e9295737931a5beb3748bcf78595958222c76e97d7159e5ca68bc65582bab`
- `review-internal.trx` — `e94fdeaa79f26023b8c4451f7ba4c262143d24549dbfe3e8b902c1f72dae4679`

## Evidence hash verification (all recomputed by this review)

- `SHA256SUMS` (29 entries): 29/29 match
- `source-binding.json` file inventory (18 test files): 18/18 Git blob + byte SHA-256 match
- `document-sha256.json` (20 docs, LF-normalized domain): 20/20 match
- Preserved RC2A-01 review blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`: 16,888 bytes, SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` — match
- `exports/vector-manifest.json`: 21/21 committed hex literals re-hashed with BCL SHA-256 — all digests match (2 large vectors digest-bound only)
- Golden literal: 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` — recomputed; review-local writer also reproduces the bytes from the documented graph values

## Adversarial probe summary (review/GlmQ1Probes, REVIEW-ONLY)

- Independent schema-1 parser + independent byte writer written from the committed
  API contract text; no production or qualification-harness code shared.
- AR-01..AR-10 counterexample probes: all invariants held (see report §J).
- AR-11: complete independent 222,615-mutation single-byte sweep — 114,378
  accepted / 108,237 refused; histogram identical on all nine codes; 576 survivor
  positions with per-position value counts matching the committed profile; zero
  `InvalidValue@0`; all accepted mutations re-encode byte-exactly; no survivor
  inside the review-derived immutable regions.
- Probe-authoring history: 7 initial check failures were review-side errors
  (hand-computed offsets, inconsistent byte-edits, one hex typo), corrected in
  the committed probes; production followed the documented rule in every case.
