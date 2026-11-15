# Alternative B Slice E-3 codec R1 independent Q2

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-R1-Q2`, 15 November 2026 (Asia/Manila)

**REMEDIATION REQUIRED. The exact R1 revision is unsuitable for formal owner acceptance.**

All 17 original Q1 counterexamples produced the required refusal in both builds. The supplemental qualification found two blocking projection comparison defects. A malformed string can escape as `InvalidOperationException`, and a numeric exponent outside the comparer’s supported range can escape as `ArgumentOutOfRangeException`. The probes reproduced 13 observations across those two defects in each configuration. The failures concern the internal managed comparison boundary. They produced no structural result and no production caller was found. [Findings, exact fixtures, and scope](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/findings.md)

## Revision, authority and scope

| Binding | Commit or Git identity | Tree or raw SHA-256 |
|---|---|---|
| Original implementation | `34eef4c8e7d814e15cb1bfce27df032e75766f19` | `5b9bfa884b4cec86aab293b80ada1b7f73b58d11` |
| Starting origin/main | `e8099e37c1243e81f173bf0248307cf7d2fe3875` | `a51ed41ee2cba8205a930749f63244c5491898c5` |
| Q1 evidence import | `7713ec58cc7c292862c8c80d6b57dcacd3420b44` | `e7f0854d5365aef6533d6ba731eebadc8638bac0` |
| Reviewed R1 candidate | `4a97b79e0060b001a7c1d30ae93bc66cedc52418` | `dfd9519b7799155a0fd6f99a8edad27618c38a6a` |
| Adopted frozen v3-pre record | Blob `f6a71562845d2cfe0aee5495864a509c5caabe7a` | `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Owner adoption and incorporated schema | `d2fea61b7db323319ee59033c8b1b12ded1ffe7d`, `a7248152d09e19d8f3775db397f340949fa9ee57` | See full authority ledger |
| BJ/IE and bootstrap-base owner texts | `f3639dc619dbfee39859afb7da553e2167d9b83d`, `93fe2c9155a21b12fd3486b9da743260897d508e` | See full authority ledger |
| Entry and P4 owner decisions | `028a7c1858b5e7b506deff9d2d8d263afe3344ac`, `31ae1b08cb4663a6a9d9de657fd972fccb731cf2` | See full authority ledger |

The sole canonical worktree began at the expected R1 commit with a clean tracked state, zero stashes and a two-ahead, zero-behind tracking relation. The 44 historical untracked files matched the R1 preflight SHA-256 ledger. All 156 Q1 checksums matched canonical files and Git blobs. The Q1 report retains **REMEDIATION REQUIRED** for the original revision. A read-only Git remote identity check completed before the final local-only scope clarification. Qualification inputs and test execution were local.

The complete R1 delta was limited to two internal codec files, one focused test file and documentation/evidence. The frozen contract, legacy serializer/decoder, host dispatch, transports, projects, dependencies, production gates and accepted evidence were unchanged. The change in comparison precedence for multiple simultaneous invalid inputs yields no successful structural result, and the adopted record does not assign an ordering to those diagnostics. The two comparison defects below block conformance. They do not constitute scope expansion. [Authority and Git ledger](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/identities.json) · [Complete implementation diff](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/scope-code-diff.json)

## Q1 closure and independent coverage

The unchanged, checksum-verified Q1 probe ran against fresh original and corrected Debug and Release DLLs. Each original replay reproduced all 17 recorded counterexamples at their original wire hashes or exact binary bytes. Both corrected runs matched all 2,383 assertions. The six integrated policy claims now require separately supplied matching values. Conflicting decoded sessions and disposed projection documents refuse. The original artifact digest remains bound after the caller mutates the input array. Each of the four Q1 implementation findings is corrected for its documented counterexamples. F03’s broader bounded-refusal objective remains unsatisfied for malformed live projection values. The separate Q1 documentation-placement observation remains an owner disposition item. [17-case matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/closure-matrix.md)

The Q1 replay re-exercised both bootstrap forms, all 11 schemas and 99 definitions, the 8,192-byte limit, pre-integrity rules, B2-only presence and integrated prohibition, `AuthorityHash` literal digest and escaped-name controls, binary 9/36/37 and inherited 10/12, the integrated optional profile tail, legacy and downgrade refusal and the `ReleaseRequest` exclusion. These checks derive expected outcomes from the adopted record. Producer tests corroborate the behavior.

The supplemental Q2 probe independently exercised 876 assertions per configuration. Among its results:

- 387 duplicate wire occurrences and 306 duplicate consumed-projection occurrences refused, including identical values and alternate escaped names in both orders. Conflicting sessions and topology substitution refused. Legitimate single escaped names still passed.
- The six integrated policy values passed their matching controls and refused typed contradictions, missing entries, invalid kinds and mixed claims. Valid numeric representations comparing to the same value remained accepted in the caller projection. The same alternate representations remained invalid as wire encodings.
- Disposed, undefined and wrong-kind projection roots refused. An owned clone of a valid projection survived disposal of its original document. Some malformed same-kind projected strings and numbers reached the exception paths below.
- Binary checks retained all six original digests across input mutations, writes through exposed views, candidate-buffer aliasing, shared inputs across multiple expectations, disposal of an input memory owner after construction, and repeated calls. The bootstrap digest span is used synchronously, and a later call compares the bytes supplied for that call.

| Finding | Severity | Candidate result | Smallest justified correction |
|---|---|---|---|
| CODEC-Q2-F01 | Medium, blocking | Five malformed string projection cases throw `InvalidOperationException`. Two already threw in the original implementation. The other three reached a newly enforced policy comparison in R1. | Handle malformed projected strings at the scalar comparison boundary. Preserve decoded ordinal equality and valid escaping. |
| CODEC-Q2-F02 | Medium, blocking | Eight malformed or unusable numeric projection cases throw `ArgumentOutOfRangeException` for extreme exponents. Six already threw in the original implementation. R1 adds the comparison for the remaining two policy fields. | Refuse numbers the comparer cannot represent and preserve exact semantic equality for valid numeric forms. Keep the correction local to scalar comparison. |

A usable supplied projection is a prerequisite for structural success. Both defects violate entry §3’s bounded refusal and frozen §3’s exact typed agreement. No spontaneous failure or producer-test discrepancy was used as evidence. The separate targeted probe captured the original and R1 stack traces for the identical inputs. [Full findings register](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/findings.md) · [Supplemental Debug ledger](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/supplement-debug.jsonl) · [Supplemental Release ledger](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/supplement-release.jsonl)

## Managed and production gates

| Measured group | Debug | Release |
|---|---|---|
| Fresh corrected builds | 5 passed | 5 passed |
| Original focused | 146 passed | 146 passed |
| R1 regressions | 24 passed | 24 passed |
| Combined focused | 170 passed | 170 passed |
| Broad M1 | 1,663 passed, 331 native skips | 1,663 passed, 331 native skips |
| Original Q1 replay on R1 | 2,383 matched, 0 discrepancies | 2,383 matched, 0 discrepancies |
| Supplemental Q2 | 863 matched, 13 exceptions | 863 matched, 13 exceptions |
| Production-mode verifier on actual M1 | Expected policy refusal, exit 1 | PASS, exit 0 |

The managed suites above had zero failed or unexecuted cases. The aborted VSTest run produced no test counts. The successful runs used the xUnit v3 in-process runner and verified DLL identities. Focused runs used the corresponding actual core DLL. The unfiltered Release test assembly used the existing Debug M1 fixture dependency, with the two inherited reachability tests loading the actual corrected Release M1. The 51 existing production-build controls and nine preparation regressions passed. The final preparation validator also passed. An advisory-enabled restore reproduced `NU1900`. The measured builds used cached dependencies and disabled advisory lookup. [Execution summary](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/summary.json) · [Reproduction and exits](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/reproduction.md)

The unchanged metadata procedure inspected 36 physical DLLs, including M1, Owner, Recovery, InstallTool and their actual dependency copies in both configurations. It found zero production codec references or activation paths and detected two known bootstrap and four known binary calling methods in each test DLL. The applicable M1 SHA-256 values are `8064e8af080ab6f0c7f31432e2cde8fd640cd9fc884cabca3dd1c2bdc6343dc6` for Debug and `ab7f57a3003f10abd6516425f5edd55b180c69372bc747e4a15fbdc577abaaf2` for Release. Static metadata coverage is limited to the inspected shipping graph and statically resolvable references. [Scan and identity bindings](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/compiled-scan.jsonl)

P4 remains neutral. The original and R1 source call inventories have the same S10 and S11 parent paths and retain the installed recovery gate before initial effects. S28 has its sole test caller. F-R1/O-R1 and O-R2 triggers, O-R3’s refusal-reason issue and the observation-to-effect residual remain in force. R1 creates no new authority, protected effect or activation path. E-1/E-2, the 39 E-2 qualifications and 23 obligations, E3-ENTRY-F01 closure, open composite Q-E1, open BB-02, open P0 durability, `NATIVE-PENDING` and 331 unqualified native skips retain their established status. E-4 retains GAVA1 and cross-format handoff. E-6 retains runtime release correlation and atomic enforcement. [Exact P4 caller inventory](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/raw/p4-callers.txt)

Recommend a separately authorized, bounded E-3 codec R2 correction for these two scalar comparison paths, followed by independent requalification. This Q2 commit contains the qualification report and evidence. [Git-blob SHA-256 evidence manifest](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/SHA256SUMS.txt)
