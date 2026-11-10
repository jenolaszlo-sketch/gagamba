# P2-Q1 — executed commands, results, and exit codes

All commands executed 2026-11-11 (Asia/Manila) from
`C:\Users\Laszlos\source\repos\Gagamba` at HEAD
`9b80969ce766b6d8939775710aa77d080c3ff674`. Raw outputs are in `executed/`.

## Builds (reviewer-executed)

| # | Command | Result | Exit |
|---|---|---|---|
| 1 | `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Debug --nologo` | succeeded; 0 warnings / 0 errors; instrumented PASS; MVID `bd8a820d-d914-42b9-b455-edaad2e42a5e`; SHA-256 `E3089FB9…` | 0 |
| 2 | `dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --nologo` | succeeded; 0 warnings / 0 errors | 0 |
| 3 | `dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release --nologo` | succeeded; 0 warnings / 0 errors; production mode PASS (0/4 hooks, 0/3 properties, 14/14 core); MVID `1994ef0f-9e7a-44fb-b2eb-407213fbc2b7`; SHA-256 `BC8E33E8…` | 0 |

Raw: `executed/build-debug-m1.txt`, `executed/build-test-debug.txt`,
`executed/build-release-m1.txt`.

## Reviewer-owned probe (external scratch)

Command (source committed at `probe/`):

    dotnet run --project <scratch>/probe/Probe.csproj -c Release -- \
      src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll \
      src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll \
      <scratch>/reviewer-fixtures

Result: **SUMMARY|total=97|failed=0**, exit 0; 0 `|FAIL|` lines. Raw:
`executed/probe-run.txt`.

Probe matrix composition (all reviewer-owned; no producer fixture reused):

| Category | IDs | Count |
|---|---|---|
| Positive chains (B2/pre, B3/post) + entries + digest identities + field-order cross-check + P1 K pair rule | POS-01..07 (a–c) | 9 |
| Malformed / noncanonical CJSON-1 and schema bytes | MAL-01..21 | 21 |
| Hash-consistent semantic-invalidity controls (hashes correctly recomputed and propagated) | SEM-01..23 | 23 |
| Identity/binding/substitution/cross-case/stale-reference/external-Hp | SUB-01..11 | 11 |
| Authority-surface and untrusted-chain controls | AUTH-01..06 (05a–h) | 13 |
| Determinism, non-mutation, fail-closed, P1 null parity | ROB-01..06 | 6 |
| IL reachability + scanner positive control (Debug and Release) | REACH-01, DBG-01..06, REL-01..06 | 13 |
| Compiled P1 reuse surface | P1R-01 + surface dump | 1 |

## Test suites (reviewer-executed)

| # | Command | Result | Exit |
|---|---|---|---|
| 4 | `dotnet test tests/.../Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --nologo --filter "FullyQualifiedName~Validation.DetachedEvidence"` | Failed 0, **Passed 33**, Skipped 0, Total 33 (compiled-Release scan executed, not skipped) | 0 |
| 5 | `... --filter "FullyQualifiedName~Tests.Validation"` | Failed 0, **Passed 120**, Skipped 0, Total 120 | 0 |
| 6 | `...` (full Windows M1, no filter) | Failed 0, **Passed 1517**, **Skipped 331**, Total 1848 | 0 |

The 331 native skips are unqualified and are **not** passes. Raw:
`executed/test-p2-filter.txt`, `executed/test-validation-filter.txt`,
`executed/test-full-suite.txt`. Producer-reported counts (33 / 120 / 1517-0-331)
are independently reproduced exactly.

## RELEASE verification (reviewer-executed; unchanged verifier)

| # | Command | Result | Exit |
|---|---|---|---|
| 7 | `dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll` | **RESULT: PASS**; 0/4 hooks, 0/3 properties, 14/14 core | 0 |
| 8 | same against `bin/Debug/net10.0/...` | **RESULT: FAIL** (expected); unchanged forbidden lists | 1 |

Raw: `executed/release-verifier.txt`, `executed/debug-as-production-verifier.txt`.

## Producer evidence cross-checks (reviewer-executed)

- Producer fixtures (6 JSON files) hash to the values in
  `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p2-evidence/fixtures/digests.txt`
  and match the sizes (694/520/581/759/585/646) — 6/6.
- Producer-recorded Hk/Hr/Hw/Hp, blob identities, ancestry and test counts
  reproduced (see `baseline-and-revision.md` and the raw logs above).
- `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`
  SHA-256 `7d6abbd9…` matches its recorded `SHA256SUMS.txt` entry; all 44
  paths match.

## Identity checks

`executed/identity-checks.txt` and `executed/untracked-check.txt` record the
baseline, ancestry, blob and untracked-path verification commands and results.

## Reproduction

See `README.md` (package root) for the exact command sequence; the probe source
is committed at `probe/Program.cs` + `probe/Probe.csproj` (run from external
scratch; it takes the two assembly paths and an output directory as
arguments).
