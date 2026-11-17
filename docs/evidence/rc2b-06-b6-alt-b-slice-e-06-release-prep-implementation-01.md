# E-6 release preparation implementation and producer verification

**C. IMPLEMENTATION BLOCKED**

The exact two-file candidate is committed on canonical local `main`. The frozen API requires the internal property `ValidationReleaseProjection.CapabilityHash` in `Gagamba.Execution.Linux.M1.Validation`. The inherited E-2 test `ValidationMemberNamesDoNotImplyAuthority` reflects all public and nonpublic declared members in that namespace and rejects the substring `Capability`. The exact property and the unchanged naming test cannot satisfy both requirements. The conflict needs an owner disposition before producer completion and independent Q1. [Frozen API](../decisions/rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-01-evidence/contract.md) · [Inherited naming test](../../tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationReachabilityTests.cs)

| Identity | Value |
|---|---|
| Date and commission | 18 November 2026 (Asia/Manila), `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-06-RELEASE-PREP-IMPLEMENTATION-01` |
| Renewed F01 baseline commit / tree | `7d298ddd9d83e555a5c16b6c0cab8f36e46b8822` / `27bcf4764d4b0f71b76c4f4741b1f5c54c876d41` |
| Implementation candidate commit / tree | `53ffe7a21408a7e9674cf50b7098a26887c070b5` / `c5e58a742408783545ae667c0a3506c1dfd3af34` |
| Branch / published GitHub main | Canonical local `main` / `4dc0ee0998f0def6cb1cba304a76b3a9f59ac904` |
| Host / toolchain | Windows 11 x64 build 26200, .NET SDK 10.0.401, runtime 10.0.12, Git 2.55.0.windows.3, Python 3.14.8, Windows PowerShell 5.1.26100.9818 for policy and preparation |
| Scope | Managed structural preparation, comparison, regression replay and refusal-only compiled reachability. Native execution, protected resources, live release authority, independent Q1 and owner acceptance remain gated. |

## Renewed preflight

The earlier producer attempt stopped on the unresolved scope of the historical count of 93. The renewed implementation preflight at F01 passed. The assessment inventory contains 93 paths. The separately pinned authorization inventory and independently reconstructed union both contain those 93 plus 35 additional paths, with zero duplicates. All 128 authorization Git blobs, raw SHA-256 values and worktree hashes were checked. The three additive status notes reconstructed to their historically pinned bytes. The authorization contract, vectors and original receipt match the frozen committed bytes. F01 added only its decision and annex. [F01 decision](../decisions/rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-f01.md) · [Renewed preflight](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/preflight.json)

The receipt binds the exact ancestry, live and tracking refs, one canonical worktree, zero stashes, clean tracked files and index, all 1,054 historical manifest entries and the 19 assessment, eight authorization and four F01 entries. It records all 44 historical untracked files by path, length and SHA-256. Both authorized source paths were absent from HEAD, index and disk at entry. SDK default compile evaluation covered both projects in Debug and Release. The audit bound the 18 frozen E-3/E-4/E-5 executable paths and verified the two local F02 copies, Alternative B formula bytes and adopted witness. External ancestry and durability remain open. [Preflight method](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/procedure/preflight.py.txt)

## Candidate scope and observed behavior

- [ValidationReleasePreparation.cs](../../src/Gagamba.Execution.Linux.M1/Validation/ValidationReleasePreparation.cs) defines the internal API, finite template and association checks, deterministic refusal, exact byte encoding and immutable results.
- [ValidationReleasePreparationTests.cs](../../tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationReleasePreparationTests.cs) tests both entry points, independent projections, malformed and contradictory inputs, refusal precedence and null output, repeated and concurrent calls, and defensive ownership.

Both frozen vectors matched the complete 258-byte preimage and the 32 raw digest bytes in each configuration. R01 measured `3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e`. The mixed-endian GUID witness measured `d4573bb166ab7a4f73743297a4b3b2b9f2e19c73ef4fb6c36c94cf2a20fd7d90` in both phases. The R02 expectation produced `DigestMismatch` and null material. The helper is a structural calculator with no production consumer. The existing `ReleaseRequest.ApprovalCorrelationSha256` and its presence semantics retain their accepted implementation. [Candidate identity](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/candidate.json) · [Traceability](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/traceability.md)

## Verification results

| Per configuration | Debug | Release |
|---|---:|---:|
| New E-6 focused | 192 passed | 192 passed |
| E-5 / E-4 / E-3 focused | 42 / 11 / 185 passed | 42 / 11 / 185 passed |
| Broad managed M1 | 1,922 passed, 1 failed | 1,922 passed, 1 failed |
| Native cases in broad M1 | 331 unqualified skips | 331 unqualified skips |
| E-4 supplemental and catalog omission control | 1,944 passed + 1 witness | 1,944 passed + 1 witness |
| E-5 frozen sensitivity and fault controls | 17 passed | 17 passed |
| E-5 Q1 supplemental replay | 696 passed, 20 nominal and 20 omission detections | 696 passed, 20 nominal and 20 omission detections |
| New E-6 sensitivity | 34 nominal and 34 detected deviations | 34 nominal and 34 detected deviations |
| P4 controls already counted in broad M1 | 15 + 32 passed | 15 + 32 passed |
| Exact M1 verifier in production mode | Expected exit 1 and refusal | Exit 0 and PASS |

Each broad run had 1,730 inherited passes and 192 new passes. The only change to inherited case identity, result and skip-reason membership was the named E-2 test moving from pass to fail. The isolated control at the archived F01 source passed in both configurations. The same test run against the candidate's actual selected Debug and Release M1 binaries failed in both with the `Capability` name filter. Ten VSTest attempts timed out at 15 seconds with zero executed tests. xUnit v3 in-process 3.2.0+9e422989c1 supplied the results above. The focused Release runs used the actual Release core and Release discovery copy. Broad Release retained its authorized Debug fixture dependency. All 51 build-policy controls, nine preparation regressions and preparation validation passed. Cached package use with `NuGetAudit=false` supplies no advisory coverage. [Execution summary](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/execution-summary.json) · [Managed membership](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/managed-membership.tsv.gz) · [Debug blocker](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/candidate-naming-debug.log.gz) · [Release blocker](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/candidate-naming-release.log.gz)

## Compiled isolation and P4

The Debug inventory contains 48 first-party assembly paths including 29 production paths, and the Release inventory contains 116 paths including 55 production paths. The counts include copies at build, selection, fixture and publish locations. The inventories distinguish D6.4's Debug-only instrumented core from the production closure and bind the selected test and release discovery copies. The compiler metadata and source audit found zero production-origin references to the helper, zero helper-to-effect edges and zero new production E-3/E-4/E-5 consumers. Known test calls and ordinary production edges were detected. Each disposable incoming-reference build failed isolation at `ProducerIsolationControl.ForbiddenCaller.Probe` and was scanned without execution. [Inventory summary](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/summary.json) · [Debug inventory](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/debug-inventory.json.gz) · [Release inventory](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/release-inventory.json.gz)

The exact-candidate assessment preserves all 29 P4 dispositions within the existing refusal-only scope. The upstream F01 admission still dominates S10/S11 on the installed route. S09 retains its Q04 scope. S24 retains owner generation before the physical arbiter and S25 retains controller generation before the existing send. S28 remains a test-only path and S29's public runtime factory still refuses. Stop, reap and cleanup continuations retain their original authority and the last-observation-to-effect residual remains. No change to domination or the first effect was observed in the audited paths. The two-file delta and zero incoming edges preserve the accepted production graph. The naming-test failure leaves the overall producer gate blocked. [All 29 Inherited dispositions](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/p4-dispositions.json) · [Source-edge observations](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/raw/isolation/source-edges.json.gz)

All 39 E-2 qualifications, 23 downstream obligations and 29 P4 dispositions carry forward. Independent Q1 remains a fresh-task verification stage after an owner decision resolves the contradiction and the affected producer gates pass. The existing E-6 atomic gates, E-7, Slice N and separate exposure acceptance remain due. [Carry-forward ledger](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/carry-forward.md) · [Reproduction](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/reproduction.md) · [SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01-evidence/SHA256SUMS.txt)
