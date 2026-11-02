# Gate C case index — exact commands and observed results (reviewer-executed)

All cases ran against reviewer-materialized `src-remediated` (implementation `66896754`). Logs under `evidence\`; some refusal logs are byte-identical except `Time Elapsed` because the early refusal message is constant (it does not echo the offending constants). The batch run outputs (recorded at execution time) are transcribed below.

## Batch 1 (run-gateC-batch1.ps1; direct M1, `-c Release --nologo -v:m`)

| Case | Extra property | EXIT | Diagnostic | Log |
|---|---|---|---|---|
| c02-empty | `-p:DefineConstants=` | 1 | GAGAMBA002 | c02-empty.log |
| c03-trace | `-p:DefineConstants=TRACE` | 1 | GAGAMBA002 | c03-trace.log |
| c04-unrelated | `-p:DefineConstants=REVIEWER_UNRELATED_CONST` | 1 | GAGAMBA002 | c04-unrelated.log |
| c18-prefixjunk | `-p:DefineConstants=XGAGAMBA_PRODUCTION` | 1 | GAGAMBA002 | c18-prefixjunk.log |
| c05-fixtureonly | `-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE` | 1 | GAGAMBA002 | c05-fixtureonly.log |
| c06-contradictory | `-p:DefineConstants=GAGAMBA_PRODUCTION;GAGAMBA_WP05_D6_4_TEST_FIXTURE` (unescaped) | 1 | MSB1006 (MSBuild CLI property parsing; superseded by c06b) | c06-contradictory.log |

## Batch 2 (run-gateC-batch2.ps1)

| Case | Configuration + property | EXIT | Diagnostic | Log |
|---|---|---|---|---|
| c06b-contradictory-esc | Release `-p:DefineConstants=GAGAMBA_PRODUCTION%3BGAGAMBA_WP05_D6_4_TEST_FIXTURE` | 1 | GAGAMBA004 | c06b-contradictory-esc.log |
| c07a-debug-forced-prod | Debug `-p:DefineConstants=GAGAMBA_PRODUCTION` | 1 | GAGAMBA003 | c07a-debug-forced-prod.log |
| c07b-test-forced-prod | Test `-p:DefineConstants=GAGAMBA_PRODUCTION` | 1 | GAGAMBA003 | c07b-test-forced-prod.log |
| c19-unknown-config | `-c Unclassified` | 1 | GAGAMBA001 | c19-unknown-config.log |
| c20a-release-fixture-carrier | Release `-p:GagambaWp05D64FixtureEnabled=true` | 1 | GAGAMBA004 | c20a-release-fixture-carrier.log |
| c20b-debug-fixture-no-identity | Debug `-p:GagambaWp05D64FixtureEnabled=true` | 1 | GAGAMBA007 | c20b-debug-fixture-no-identity.log |
| c22-release-explicit-valid | Release `-p:DefineConstants=GAGAMBA_PRODUCTION` | 0 | (valid positive control) | c22-release-explicit-valid.log |

## Batch 3 (run-gateC-batch3.ps1)

| Case | Target + args | EXIT | Diagnostic / gate | Log |
|---|---|---|---|---|
| c09a-owner-contradictory | Owner `-c Release -p:DefineConstants=` | 1 | GAGAMBA002 | c09a-owner-contradictory.log |
| c09b-recovery-contradictory | Recovery `-c Release -p:DefineConstants=` | 1 | GAGAMBA002 | c09b-recovery-contradictory.log |
| b3-owner-release-direct | Owner `-c Release --artifacts-path work\b3-owner-artifacts` | 0 | closure gate 0/4+0/3 PASS | b3-owner-release-direct.log |
| b4-recovery-release-direct | Recovery `-c Release --artifacts-path work\b4-recovery-artifacts` | 0 | closure gate 0/4+0/3 PASS | b4-recovery-release-direct.log |
| c10-solution-contradictory | `Gagamba.sln -c Release -p:DefineConstants=` | 1 | GAGAMBA002 (+ instrumented gate PASS for Debug closure) | c10-solution-contradictory.log |

## Batch 4 (run-gateC-batch4.ps1; in-place tree; BIN = bin\Release M1.dll first-16 SHA)

| Case | Command | EXIT | Diagnostic | BIN before → after |
|---|---|---|---|---|
| c11-pre | (hash check) | — | — | `4D8A31BD6920DCDE` |
| c11-contradictory-after-valid | `-c Release -p:DefineConstants=TRACE` | 1 | GAGAMBA002 | `4D8A31BD` → `4D8A31BD` (untouched) |
| c13-no-restore-contradictory | `-c Release -p:DefineConstants= --no-restore` | 1 | GAGAMBA002 | `4D8A31BD` → `4D8A31BD` |
| c14a-no-build-valid | `-c Release --no-build` | 1 | MSB1001 (CLI-level; fails closed) | `4D8A31BD` → `4D8A31BD` |
| c14b | corrupt bin with Debug bytes (`07DC76D1…`) then `--no-build` | 1 | MSB1001 | `07DC76D1` → `07DC76D1` (unsafe bytes never validated) |
| c12-valid-rebuild-over-corrupt | `-c Release` | 0 | gate PASS 0/4+0/3 | `07DC76D1` → `0FAFB4F7AA4CDCDC` (repaired) |
| c12c-incremental-valid-over-corrupt | corrupt again, `-c Release` | 0 | gate PASS | `07DC76D1` → `0FAFB4F7AA4CDCDC` (repaired) |

## Batch 5 (run-gateC-batch5.ps1)

| Case | Command | EXIT | Diagnostic / gate | Output |
|---|---|---|---|---|
| c15-publish-valid | `dotnet publish M1 -c Release -o work\c15-publish` | 0 | gates PASS | published M1 `0FAFB4F7…` |
| c16-publish-contradictory | `dotnet publish -c Release -p:DefineConstants= -o …` | 1 | GAGAMBA002 | nothing published |
| c17-publish-nobuild-valid | `dotnet publish -c Release --no-build -o …` | 0 | input gate PASS | published M1 `0FAFB4F7…` |
| c17b-publish-nobuild-corrupt | corrupt bin (`07DC76D1…`), publish `--no-build` | 1 | instrumented/production mismatch (4/4+3/3 found) | `work\c17b-publish` NOT created |
| c17c-publish-nobuild-contradictory | publish `--no-build -p:DefineConstants=` | 1 | GAGAMBA002 | nothing published |
| c14c-msbuild-nobuild-valid | `dotnet msbuild -t:Build -p:Configuration=Release -p:NoBuild=true` | 1 | NETSDK1085 (SDK rejects Build+NoBuild) | bin unchanged |
| c12d-valid-repair | `-c Release` | 0 | gate PASS | bin `0FAFB4F7…` |

## Batch 6 (run-gateC-batch6.ps1)

| Case | Command | EXIT | Diagnostic | Log |
|---|---|---|---|---|
| c-g5-malformed-carrier | Debug `-p:GagambaWp05D64FixtureEnabled=banana` | 1 | GAGAMBA005 | c-g5-malformed-carrier.log |
| c-g6-fixture-symbol-no-carrier | Debug `-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE` | 1 | GAGAMBA006 | c-g6-fixture-symbol-no-carrier.log |
| c-g7-release-fixture-assemblyname | Release `-p:DefineConstants=GAGAMBA_PRODUCTION -p:AssemblyName=Gagamba.Execution.Linux.M1.TestFixture` | 1 | GAGAMBA004 | c-g7-release-fixture-assemblyname.log |
