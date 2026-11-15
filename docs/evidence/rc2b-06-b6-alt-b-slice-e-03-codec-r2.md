# Alternative B Slice E-3 codec R2 remediation

15 November 2026 (Asia/Manila), canonical Gagamba `main`.

**E-3 CODEC R2 IMPLEMENTED — READY FOR INDEPENDENT Q3**

The two blocking Q2 projection comparison defects now produce the existing `Expectation` refusal with no decoded data. This is producer verification. The independent R1 Q2 disposition remains **REMEDIATION REQUIRED** for its reviewed R1 revision. [Original Q2 report](../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2.md) · [Before-and-after matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/results.md)

## Source and scope

The exact baseline was `4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469` with tree `0975cc5fbd13c6342c219333029e82e3d02c94aa`. R1 was `4a97b79e0060b001a7c1d30ae93bc66cedc52418` and the original implementation was `34eef4c8e7d814e15cb1bfce27df032e75766f19`. The tested tree before documentation was `013cea761b6086a7c66f63646962f8657898ebe9`. The final revision is the commit introducing this report. Retrieve its SHA and tree with `git log -1 --diff-filter=A --format="%H %T" -- docs/evidence/rc2b-06-b6-alt-b-slice-e-03-codec-r2.md`. [Exact tested blobs](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/source-binding.json)

The production delta is confined to `ValidationPreCodec.cs`. The new `ValidationPreCodecR2Tests.cs` contains 13 exact synthetic projection mutations and two positive and boundary cases. All other changes are this evidence and current status entries. The adopted frozen record remains blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`. The Q1, R1 and Q2 evidence remains byte-identical in Git.

## Cause and correction

Baseline Debug and Release rebuilt from the exact source reproduced all 13 cases before any production edits. Five string cases reached deferred JSON unescaping and threw `InvalidOperationException`. Eight numeric cases reached exponent normalization and threw `ArgumentOutOfRangeException` with parameter `exponent`. Every stack traced to `JsonElement.DeepEquals` during projected leaf comparison.

The codec handles those two failures only at that scalar comparison. The string guard applies to expected JSON strings. The numeric guard applies to expected JSON numbers and the specific exponent failure. Both return `Expectation`. Other exceptions keep their existing handling. Valid inputs retain `DeepEquals` semantics, and well-formed unequal values still return `Comparison`. Wire validation and its exact lexical grammar remain intact. [Exact wire/projection hashes and stack traces](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/cases.json)

## Results per configuration

| Check | Debug | Release |
|---|---|---|
| Original focused | 146 passed | 146 passed |
| R1 focused | 24 passed | 24 passed |
| R2 tests on original R1 DLL | 2 passed, 13 expected failures | 2 passed, 13 expected failures |
| R2 tests on corrected DLL | 15 passed | 15 passed |
| Combined focused | 185 passed | 185 passed |
| Broad M1 | 1,678 passed, 331 native skips | 1,678 passed, 331 native skips |
| Unchanged Q1 replay on R2 | 2,383 matched, including all 17 Q1 cases | Same |
| Supplemental Q2 replay on R2 | 876 matched, including all 13 Q2 cases | Same |
| Production-mode verifier | Expected policy refusal, exit 1 | PASS, exit 0 |

The broad suite gained 15 passing cases and retained the 331 explicitly unqualified native skips. Corrected managed runs report zero failures or errors. Each focused configuration loaded its matching core DLL. The broad Release test project retains its existing Debug dependency and tests that inspect production isolation load the actual Release DLL through their established discovery layout. The 51 unchanged production-build controls and the final preparation validator passed. [Counts](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/test-counts.json) · [Execution exits](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/q2-exits.txt) · [Build controls](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/production-controls.log) · [Preparation](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/preparation-final.log)

## Reproduction and limits

From PowerShell set `$repo` to canonical Gagamba and `$run` to a fresh external artifact directory. Build the baseline before editing using `dotnet build <M1.csproj> -c <Debug-or-Release> -m:1 --artifacts-path "$run/before/build"`. Run the committed Q2 findings probe against each DLL with `dotnet run --project <Q2-findings-project> -c <configuration> --artifacts-path "$run/before/probe" -p:Candidate=<core-output-directory> -- <Q2-findings-inputs>`. The baseline capture reports the 13 exceptions as data. After the correction, build M1 Tests, M1, Owner, Recovery and InstallTool with the same build form under `$run/candidate`. Set `UseArtifactsOutput=true` and `ArtifactsPath` per build tier so the nested verifier output remains external. Run the evidence runner from the recorded sibling-workspace layout. [Verification runner](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/Verify-R2.ps1) · [Build commands](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/build-Debug-Gagamba.Execution.Linux.M1.log) · [Artifact identity ledger](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/r2-bindings.txt)

The build used cached dependencies and `NuGetAudit=false` after the previously reproduced advisory endpoint failure. The xUnit v3 in-process runner was selected because VSTest timed out in Q2 on this host. The exact assemblies, filters, exit codes and XML are included. The production-build controls run with `pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory <external-directory>`. Preparation runs with `pwsh -NoProfile -File eng/verify-preparation.ps1`.

The unchanged compiled scanner inspected 36 DLL paths in both configurations including M1, Owner, Recovery, InstallTool and actual dependency copies. Both test assemblies provided positive controls. It reported zero production codec references. P4 stays neutral because the compiled shipping graph and source caller inventory show no new route. S10/S11 parent paths, first-effect dominance and S28 test-only status are preserved. F-R1/O-R1 and O-R2 retain their re-audit triggers, and O-R3 and the observation-to-effect residual remain open. The scan covers statically resolvable references and the inspected shipping graph. [Compiled scan](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/compiled-scan.jsonl) · [P4 caller inventory](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/raw/p4-callers.txt)

E-1 and E-2, their retained qualifications and downstream obligations, E3-ENTRY-F01 closure, composite Q-E1, P0 durability, BB-02 and NATIVE-PENDING retain their established status. E-4 retains GAVA1 and cross-format handoff. E-6 retains runtime release correlation and atomic enforcement. No protected execution or operational approval follows from these results. Independent Q3 remains a separate commission. [Evidence manifest over committed Git bytes](rc2b-06-b6-alt-b-slice-e-03-codec-r2-evidence/SHA256SUMS.txt)
