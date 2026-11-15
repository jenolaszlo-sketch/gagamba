# Alternative B Slice E-3 codec R2 independent qualification

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-R2-Q4`, 16 November 2026 (Asia/Manila).

**SUPPORTED WITH QUALIFICATIONS**

The exact R2 implementation satisfies the adopted E-3 contract for the isolated managed v3-pre bootstrap and binary payload codec. All mandatory qualification checks passed. No new in-scope discrepancy was found. Recommend a separate OWNER-02 decision on acceptance of this revision.

## Reviewer independence and baseline

Q4 ran in Codex chat `01a1dffd-bb11-7341-bee8-89ce2f3af4c4`. Codex Desktop's thread history showed one initial Q4 turn and no older page. Local session metadata showed no fork. The separate chat `01a1deab-fe1d-7b81-be00-20df23889d51` contains the R2 implementation, Q3 and OWNER-01 commissions and their results. The Q4 reviewer had no role in those commissions. Q4 built the candidate from the canonical checkout, ran the existing fixture and regression suites, and reviewed the source against the adopted record. Q4 changed only qualification documentation. [Independence evidence](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/independence.md)

| Binding | Commit or blob | Tree or SHA-256 |
|---|---|---|
| Starting canonical `main` and OWNER-01 | `ffdad5d05fefb6d260c884d61c791c6d9be91df5` | `9aef7acf0f865fcc355b7e01c0e9b021eb0ceff0` |
| R2 implementation under qualification | `8be61181e804b2dfafb0797eb54c92d2f234ff86` | `1c9307845858fb25ca0a9473d53d1fd1496018fc` |
| Historical Q3 | `29da0ed60c18ed6364bf9366a64ab5aa3393db62` | `52364b4c62098fc0fc75fdda26bd2bd8f099c760` |
| Adopted frozen record | `f6a71562845d2cfe0aee5495864a509c5caabe7a` | `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |

The sole canonical worktree had a clean tracked checkout and index. The branch ancestry matched the pinned implementation and review records, all 44 historical untracked files matched the prior inventory by path and SHA-256, and all 397 prior Q1/Q2/R2/Q3 checksums and nine additional OWNER-01 checksums matched the committed and working bytes. Every change from R2 to the starting baseline was documentation. The audit binds the frozen record, its adoption decision, the BJ/IE and bootstrap-base rulings, the schema annex and the entry decision. [Preflight and identities](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/preflight.json) · [Adoption](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01.md) · [Entry authorization](../decisions/rc2b-06-b6-alt-b-slice-e-03-entry-owner-01.md)

## Managed results

| Check | Debug | Release |
|---|---:|---:|
| Original Q1 discrepancies resolved | 17/17 | 17/17 |
| Original Q2 bounded `Expectation` refusals | 13/13 | 13/13 |
| Existing Q3 supplemental controls | 56/56 | 56/56 |
| Full Q1 conformance corpus matched on R2 | 2,383 | 2,383 |
| Q2 supplemental corpus matched on R2 | 876 | 876 |
| Original focused / R1 / R2 | 146 / 24 / 15 | 146 / 24 / 15 |
| Combined focused | 185 passed | 185 passed |
| Broader M1 | 1,678 passed, 331 native skips | 1,678 passed, 331 native skips |

Both managed runs had zero failures, runner errors or not-run cases. All 13 Q2 cases returned `Expectation` without decoded data or exceptions. The Q1 replay reproduced the original 17 discrepancies on the predecessor, and the exact Q2 fixtures reproduced five string-decoding and eight numeric-comparison exceptions on R1. The original wire and projection identities matched the committed fixtures. The 56 Q3 controls re-exercised valid neighbouring strings and numbers, precise decimal inequality, escaped characters, extreme exponents and strict wire numeric grammar. No new tests were needed. [Q1 matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/q1-matrix.md) · [Q2 matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/q2-matrix.md) · [Q3 control matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/q3-controls.md) · [Runner counts](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/counts.json)

Frozen §§2–3 and the schema annex govern both bootstrap forms. The decoder checks the byte limit before JSON parsing, decodes property names before ordinal membership and uniqueness checks, and handles the literal `AuthorityHash` token separately from its decoded member name. The replays exercised both complete witnesses, the two B2-only member rules, Boolean false and the two literal null associations, comparison with the separate capability digest, strict 8,191/8,192/8,193-byte controls, and every required member of the closed objects. The R2 delta leaves the R1 projection lookup, ambiguity and lifetime checks in place. It contains deferred string-decoding errors and the exponent-specific numeric error at scalar comparison. Successful scalar comparisons still use typed semantic equality. The decoder initializes the output to null and publishes structural data only after all checks succeed.

Frozen §4 governs exact roles, digest ownership, byte layout and supplied context. Source inspection and the replay cover the 76-byte B2 and 76/105-byte integrated 9/36 forms, B2-only 246-byte 37, 36-byte 10 and B2-only 48-byte 12. The tests cover wrong role, fixed fields, profile, nonzero pre status, wrong original digest or session, missing confirmation predecessor, malformed length and well-formed peer refusal. The R1 digest snapshots remain in force and the managed controls confirm that modifying the original arrays or returned memory cannot replace a bound digest. Legacy refusal and matching legacy positives remain covered. The codec implements structural comparison under supplied context. Frozen §5 assigns release correlation to downstream stages. [Adopted frozen record](../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md) · [Run procedure and evidence](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/reproduction.md)

## Integration controls

All 51 production-build controls and nine preparation regressions passed. Preparation validation passed. The production verifier reported 14/14 required core types and passed on Release with exit 0. The Debug-as-production control reported the expected configuration and qualification-hook diagnostics with exit 1. The metadata scan covered 36 physical assembly paths, detected both known managed test caller sets and found zero inbound production references to the codec and zero outbound repository-code calls from it. The Owner, Recovery and InstallTool copies matched the corresponding inspected core. [Build policy results](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/production-controls.json) · [Release](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/verifier-release.log) · [Debug](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/verifier-debug.log) · [Metadata scan](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/compiled-scan.jsonl)

P4 is neutral for exact R2. The reverse-call inventory matched the original implementation and starting `main`. The installed S10/S11 chain still enters through `RequireIntegrated` before its first protected effect, and S28 retains its sole test construction site. The source delta and compiled metadata established no new production caller, authority promotion or control-flow change to a protected effect. F-R1/O-R1 and O-R2 retain their future-caller review triggers. O-R3 and the observation-to-effect residual remain open. [P4 inventory](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/p4-inventory.txt) · [P4 coverage decision](../decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md)

## Historical reconciliation and remaining qualifications

Q1 and Q2 identified real defects on their original revisions. R1 fixed Q1's incomplete policy comparisons, ambiguously supplied sessions, disposed projection handling and mutable digest binding. R2 bounded the two remaining scalar comparison failure classes exposed by Q2. Q3 supplied corroborating technical evidence from the same task that implemented R2. OWNER-01 correctly withheld acceptance under the independent-review requirement, and Q4 supplies the separate qualification for unchanged R2. [Original implementation](rc2b-06-b6-alt-b-slice-e-03-codec-implementation-01.md) · [Q1](rc2b-06-b6-alt-b-slice-e-03-codec-q1.md) · [R1](../evidence/rc2b-06-b6-alt-b-slice-e-03-codec-r1.md) · [Q2](rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2.md) · [R2](../evidence/rc2b-06-b6-alt-b-slice-e-03-codec-r2.md) · [Historical Q3](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3.md) · [OWNER-01 decision](../decisions/rc2b-06-b6-alt-b-slice-e-03-owner-01.md) · [Owner acceptance matrix](../decisions/rc2b-06-b6-alt-b-slice-e-03-owner-01-evidence/acceptance-matrix.md)

`NATIVE-PENDING` and all 331 native skips remain unqualified. BB-02, composite Q-E1, the first E-1 production consumer gate and P0 external evidence durability and independent ancestry obligations remain open. E-2 retains all 39 qualifications and 23 obligations. E3-ENTRY-F01 retains its adoption closure. P4 remains refusal-only, with separate E-4 and E-6 re-audits. E-4 retains GAVA1 and cross-format handoff, and E-6 retains runtime release correlation and atomic enforcement. The verified scope is internal managed structure and statically resolvable references in the inspected binaries. Runtime/protected qualification, custody and activation remain separate. [Inherited qualifications](../decisions/rc2b-06-b6-alt-b-slice-e-03-owner-01-evidence/inherited-qualifications.md) · [Downstream obligations](../decisions/rc2b-06-b6-alt-b-slice-e-03-owner-01-evidence/downstream-obligations.md) · [Additional qualifications](../decisions/rc2b-06-b6-alt-b-slice-e-03-owner-01-evidence/carry-forward.md)

VSTest 18.7.0 timed out connecting to its testhost before cases ran. The measured results use xUnit v3's in-process runner on .NET 10.0.12. The broad Release test assembly uses the repository's Debug fixture dependency and its reachability checks inspect the actual Release core. The focused Release runs bind the Release codec itself. Cached restores used `NuGetAudit=false`, so advisory status is outside this evidence. [VSTest diagnostic](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/vstest-debug.log) · [Artifact bindings](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/test-bindings.json)

Owner acceptance remains a separate decision. Submit the exact R2 commit and its Q4 evidence for OWNER-02. [SHA-256 evidence manifest](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/SHA256SUMS.txt)
