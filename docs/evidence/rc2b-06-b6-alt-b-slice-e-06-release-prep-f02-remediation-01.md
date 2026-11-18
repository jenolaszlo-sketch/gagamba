# E-6 release preparation F02 remediation and producer verification

**A. F02 REMEDIATED, READY FOR INDEPENDENT Q1 QUALIFICATION**

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-06-RELEASE-PREP-F02-REMEDIATION-01`, 18 November 2026 (Asia/Manila).

| Binding | Verified value |
|---|---|
| Blocked implementation | `53ffe7a21408a7e9674cf50b7098a26887c070b5`, tree `c5e58a742408783545ae667c0a3506c1dfd3af34` |
| F02 authorization and starting local main | `48592c7f18150767caed66ffa7f5c3fa3a9175be`, tree `d38614e53df10a6aadca4dedf33c7dabfd1376ca` |
| Remediated exact candidate | `4202a2f7a99b85cfe1caf70d6a583f58e6551283`, tree `f987f32ced441263167649307003536dd493dd66` |
| Canonical branch / live GitHub main | `main` / `4dc0ee0998f0def6cb1cba304a76b3a9f59ac904` |
| Host / runtime / SDK | Windows 11 x64 build 26200, .NET runtime 10.0.12 and SDK 10.0.401. Windows PowerShell 5.1.26100.9818 for policy and preparation. |
| Qualification scope | Producer managed structural tests and refusal-only compiled isolation. Native qualification, protected execution, independent Q1 and owner acceptance remain due. |

Preflight independently rechecked all 128 governing source identities, 1,054 historical entries, 19 assessment entries, eight original authorization entries, four F01 entries, 188 original producer entries, 12 F02 owner entries and 44 historical untracked paths and hashes. Exact ancestry, trees, one worktree, zero stashes, clean tracked files and index, and live origin were verified. [Preflight](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/preflight.json) · [F02 owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01.md)

## Closure and byte equivalence

The original producer result remains blocked at its recorded revision. The unchanged inherited naming case passed on the F01 baseline, failed on the blocked candidate and now passes against both selected remediated M1 assemblies. Reflection found all seven expected renamed members, zero forbidden matches and zero collisions. The property remains internal get-only `byte[]?` and its constructor argument stays in position 9. The only executable changes are seven uppercase and five lowercase identifier substitutions in the helper and three corresponding test substitutions. Byte comparison against the blocked candidate proves the complete delta. [Original blocked report](rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01.md) · [Exact diff](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/minimal-diff.txt.gz) · [Candidate identity](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/isolation/candidate.json)

Independent reconstruction used the frozen domain, raw digest slots, LE index and mixed-endian GUID rules. Selected artifacts returned the same 258 raw bytes and 32 raw digest bytes as the frozen vectors and blocked producer record. The renamed member fills the exact slot at offsets 30–61. R01 remains `3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e`. The mixed vector remains `d4573bb166ab7a4f73743297a4b3b2b9f2e19c73ef4fb6c36c94cf2a20fd7d90`. R02 returns `DigestMismatch` and null material. Independent expectations, refusal order and statuses, ownership, copying, and the existing approval-correlation field and presence semantics retain their frozen behavior. [Debug member and byte audit](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/compiled-witness-debug.json) · [Release member and byte audit](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/compiled-witness-release.json)

## Results

| Gate | Debug | Release |
|---|---:|---:|
| E-6 / E-5 / E-4 / E-3 focused | 192 / 42 / 11 / 185 passed | 192 / 42 / 11 / 185 passed |
| Broad M1 | 1,923 passed, zero failures | 1,923 passed, zero failures |
| Inherited native skips | 331 unqualified | 331 unqualified |
| E-4 supplemental and omission witness | 1,944 + 1 passed | 1,944 + 1 passed |
| E-5 frozen sensitivity and Q1 replay | 17 controls, 696 supplemental cases, 20 nominal and 20 omissions detected | Same |
| E-6 sensitivity | 34 nominal, 34 deviations detected | Same |
| Direct production verifier | Expected refusal, exit 1 | PASS, exit 0 |

VSTest timed out before executing tests. The authorized xUnit v3 in-process runner supplied the complete managed results. Broad totals contain 1,731 inherited and 192 E-6 passes. Inherited membership and native skip reasons match the accepted ledger. Two E-6 theory display names reflect the rename. The Release focused and discovery paths are bound to Release M1. Broad Release uses the established Debug fixture reference. All 51 build-policy controls, nine preparation regressions and preparation validation passed. Cached restore with advisory checks disabled supplies no advisory coverage. [Results and raw hashes](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/execution-summary.json) · [Exact case membership](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/managed-membership.tsv.gz)

## Exact-revision refusal-only P4 disposition

**P4 neutral within the existing refusal-only scope** for the remediated candidate above. The audit covered 48 Debug and 116 Release first-party build, fixture, selected, discovery and publish assembly paths. Known test calls and ordinary production edges were detected. Both compile-only injected callers were detected. Production callers of the helper, helper-to-effect edges and new E-3/E-4/E-5 production consumers were zero. Source and compiled analysis preserve the existing runtime refusal paths and first-effect ordering. F01's upstream S10/S11 domination, Q04's S09 boundary, S24 and S25 generation gates, S28 test-only isolation, S29 public refusal and all stop and cleanup continuations retain their accepted scope and residuals. All 15 + 32 compiled P4 and arbiter witnesses were reconciled to the broad suite in each configuration. [Inventory and isolation results](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/isolation/summary.json) · [29 dispositions](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/raw/isolation/p4-dispositions.json)

All 39 E-2 qualifications, 23 downstream obligations and 29 P4 dispositions carry forward with their original triggers. Independent Q1 requires a fresh Codex task uninvolved in implementation and producer verification. Owner acceptance follows Q1 separately. [Carry-forward ledger](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/carry-forward.md) · [Reproduction and traceability](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/reproduction.md) · [SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/SHA256SUMS.txt)
