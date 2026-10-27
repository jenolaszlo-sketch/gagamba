# GAGAMBA-RC2A-03-CQ1-Q2 — Focused independent remediation verification

Date: 2026-10-27 (Asia/Manila). Reviewer: Codex (GPT-6 Astra), focused Q2 verification role.

**Overall verdict: SUPPORTED WITH QUALIFICATIONS.** Q1 findings F01–F04 are **CLOSED for the exact R1 target below**. The corrected record supports RC2A-03 component acceptance with the inherited RC2A-02 qualifications. This report supplies a review recommendation; it issues no owner acceptance and authorizes no subsequent stage.

The prior conversation includes R1 preparation. Q2 independently re-derived the corrected assertions from immutable Git objects using a newly authored [read-only verifier](rc2a-03-cq1-q2/verify_record.py), direct source/API/test inspection, existing TRX inspection and raw/normalized inventory comparisons. The producer's verification statements were not used as proof of their own correctness. Unchanged historical results are explicitly reused from Q1, as commissioned, rather than represented as new executions. [Captured Q2 verification results](rc2a-03-cq1-q2/verification.json) record the exact objects and observations.

## A. Git binding and worktree discipline

| Item | Independently verified identity / observation |
|---|---|
| Repository / origin | `jenolaszlo-sketch/gagamba` / `https://github.com/jenolaszlo-sketch/gagamba.git` |
| Accepted RC2A-02 baseline | `9519e7cb612af6ff69c93c0d128553138533d9fd`, tree `7b162d2b0e00d72c27a2389ed505d531758797f1` |
| Original qualification | `c946df0c93dde414c744f46cce054e7cd3f2c26e`, tree `ba7a3ef73a651cf5ace62583873742f855330910`, direct parent = accepted baseline |
| Reviewed R1 commit | `271e34b45fa504f6bb7ef7359c4f2f1a9649e49d` |
| Reviewed R1 tree | `f3f63129be98c5c9f9efba3302da331ae65139ee` |
| Reviewed R1 parent | `c946df0c93dde414c744f46cce054e7cd3f2c26e` |
| Producer branch | `qualification/gagamba-rc2a-03-cq1` |
| Producer worktree | `C:\Users\Laszlos\source\repos\Solo\rc2a-03-cq1` |
| Preserved Q1 commit | `dd7e47d2c5f75af126ce4a081013bf21182d73ee`, tree `c5d1299a2d382585ab240cb35f0352a7adfbfff8` |
| Reused review branch | `review/gagamba-rc2a-03-cq1-q1` — retained name; appended Q2 records distinguish the review rounds |
| Reused review worktree | `C:\Users\Laszlos\source\repos\Solo\rc2a-03-cq1-q1-review` |
| Q2 report commit / tree / parent | Recorded after this report commit in companion `rc2a-03-cq1-q2/git-binding.json`, fields `reviewReportCommit`, `reviewReportTree`, `reviewReportParent`. The final receipt commit identity is supplied in the completion handoff, avoiding circular self-binding. |
| Initial status | Both worktrees clean, including untracked-file status; review HEAD exactly Q1; producer HEAD exactly R1; no existing Q2 report/directory or Q2 remediation-review commit found; approximately 68.80 GiB free on C: |
| Completion status | Post-report-commit clean review status and unchanged clean producer recorded in the companion binding receipt; final clean status checked after the receipt commit and supplied in the handoff |

Q2 reads R1 with `git show <R1>:<path>` and `git diff <original> <R1>` from the existing review worktree. It does not check out R1, switch or modify the producer branch, or create a branch/worktree. The review branch advances from Q1 only by adding the Q2 report and companion evidence. The original Q1 commit and its report, verifier, verification results and receipt remain unchanged. In particular, R1's statement that the review branch was at `dd7e47d` describes its preparation-time checkpoint; the owner's Q2 assignment now expressly permits appending this review on that branch.

The Q1 report at `dd7e47d:docs/reviews/rc2a-03-cq1-q1-independent-verification.md` retains blob `090cc279b4ec508d16cec6ef7a4ac095fe52b4b1` and SHA-256 `be23011288e6e947d3b9a28e1940618c437e21e380c0f48886062818f260f5a9`. Its receipt's three report/support object IDs, byte lengths and hashes were independently checked. Q1's REMEDIATION REQUIRED verdict remains valid historical evidence for `c946df0`.

No build, restore, test execution, native probe or package operation occurred. Git and Python were used only for inspection and review-evidence authoring; the exact host/tool versions are in `verification.json:environment`. No target process, execution fixture or native domain was created. Tracking documents are not updated by Q2 because this commission permits review additions and forbids changing the qualification record.

## B. Finding verification

Locations below are in the reviewed R1 commit. Package paths are relative to `docs/evidence/restricted-consumer-contract-rc2a-03-cq1/` unless fully specified.

| Finding | Verdict | Corrected assertion and independently checked evidence |
|---|---|---|
| F01 — C12 binding | **CLOSED** | Matrix §1 C12, handoff “Exact material to review”, README §9.1 and implementation-plan §1 RC2A-02 row resolve to FSA-01 at `058a150130c5f112c7e9b8189bba8842cf0cec52:docs/assessments/rc2a-02-full-stage-acceptance-gap-assessment.md`, blob `5b4f559d71e8bb6b871a28f73be694b742887bdd`, tree `b2211a26c838f98f994602e5c56ea8bb95b2c1d8`. §4 contains E1–E8; E2/E6 explicitly retain qualifications. |
| F02 — C4 digest condition | **CLOSED** | Matrix §1 C4 and README §9.2 require length 32 and content equality; nonmatching 32-byte values fail, null manifest throws before the length check, and equality grants no provenance/authority. Frozen API §API and failures, unchanged codec and accepted B20–B23 sources/results agree. |
| F03 — C7/Q3 provenance | **CLOSED** | Matrix §1 C7 / §2 Q3 and README §9.3 cite originating RC2A-01 review `8e41babfc96afa2e617a53b63e50903d54001194` §D and owner acceptance `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e` §4. The inherited RC1-A citation error is disclosed; historical decisions/assessment remain untouched. |
| F04 — C10 byte domain | **CLOSED** | Matrix §1 C10 and README §9.4 correctly distinguish differing committed bytes, identical LF-normalized bytes and equal parsed JSON. The frozen API blob is separately identified. Fresh comparisons of all three historical Git blobs reproduce the stated sizes, line endings and hashes. |

### F01: assessment versus accepted decision

Git object inspection confirms FSA-01's E1–E8 table is §4, whereas §5 is the requirement-source matrix. The cited §4 anchor resolves to the actual heading. The separately committed owner decision is `9519e7cb612af6ff69c93c0d128553138533d9fd:docs/decisions/restricted-consumer-contract-rc2a-02-full-stage-owner-acceptance.md`: §5 accepts E1–E8, §6 carries qualifications, and §8 accepts the union of substage evidence without requiring a consolidated run. These sections support the corrected C12 claim within its E2/E6 limits.

Assessment and acceptance share parent `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6`; the assessment is not an ancestor of R1. R1 accurately preserves the distinction between a separately committed readiness assessment and an owner acceptance. The roadmap assessment still resolves separately at `d417367499979d38a3c9a498275d8422bf69f3d0:docs/assessments/rc2a-roadmap-01-restricted-execution-dependency-review.md`. No replacement assessment or retroactive decision was invented.

### F02: actual contract and accepted tests

At R1, `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs:56` (`VerifySha256`) checks null, checks `expectedSha256.Length != 32`, then calls `CryptographicOperations.FixedTimeEquals(ComputeSha256(manifest).AsSpan(), expectedSha256)`. `ComputeSha256` hashes the complete canonical encoding. The frozen API states valid-length matching/nonmatching inputs return true/false; all-zero is an ordinary present value.

At accepted evidence commit `c1a01761f8d58136a381ad87269635c6c14ad093`, `tests/Gagamba.Execution.Tests/Restricted/Rc2a02PublicBoundaryTests.cs` contains B21's five wrong-length cases, B22's matching/changed/all-zero assertions, and B20/B23's null-first cases. B22 changes a byte without changing length and asserts false; it also asserts false for the all-zero digest against the golden. The test and codec blobs are unchanged at R1. Existing `docs/evidence/restricted-consumer-contract-rc2a-02a-windows/runs/windows-public.trx` contains all eight corresponding Passed results. This verifies the accepted evidence, without executing tests.

R1's C4 avoids the former implication that every 32-byte value verifies. Length alone establishes no content integrity; matching digest content establishes neither producer provenance nor authorization. All-zero is not a wildcard and is not universally forbidden as a value. The unchanged encoding-boundary and exception contract remains applicable.

### F03: decision origin and preserved historical error

The originating review is `8e41babfc96afa2e617a53b63e50903d54001194:docs/reviews/restricted-consumer-rc2a-01-glm-independent.md`, blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`; §D R2A-F03 records different rebuilt DLL hashes despite reproduced test outcomes. The originating decision is `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e:docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md`, blob `15e008524e4d1c239074b0f204d7c9a0f692fa0c`; §4 says ACCEPT AS DISCLOSED and carries the test-versus-binary distinction into RC2A-02 E4.

The 02A evidence at `c1a01761` retains the R2A-F03 disclosure, its owner decision `e5fff9e8` excludes binary/package qualification, and full-stage acceptance `9519e7c` §6 item 4 carries the restriction. That item's “RC1-A acceptance §4” attribution is historically wrong. FSA-01 §3 also contains the inherited mislabeling. RC1-A `9a705335701f0c1923bd9867a2612b8a93afa8cb:docs/decisions/restricted-consumer-rc1-owner-acceptance.md` contains no R2A-F03 disposition; §4 approves Mode `work`. README §9.3 now discloses these errors explicitly while preserving the historical artifacts. This closes the qualification-record attribution defect without claiming to amend those owner records. No DLL, package, distribution or deployment reproducibility claim is established.

### F04: independently reproduced byte domains

Exact inputs:

1. `62b75f5866d0c339816712baeb04baa386b06f85:docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/runs/windows/final/public-surface-inventory.json`.
2. `f8b692e04941462ea592d9653960191cafb937cc:docs/reviews/rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json`.
3. `f8b692e04941462ea592d9653960191cafb937cc:docs/reviews/rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json`.

| Input | Raw bytes | CRLF pairs | Raw SHA-256 |
|---|---:|---:|---|
| Qualification | 34,221 | 904 | `4b53a523634c304336cb3faf01a1133d038bb74d1de32eecf6e1023936f06a0b` |
| Review | 33,317 | 0 | `9e35d1ea33083537f6bab295f764423a69bd4f9d913d055ff42e9f7d3bf774d1` |
| Rerun | 33,317 | 0 | `9e35d1ea33083537f6bab295f764423a69bd4f9d913d055ff42e9f7d3bf774d1` |

The qualification blob differs from both review blobs; the latter two are raw-equal. Replacing CRLF with LF produces three equal 33,317-byte sequences with the review SHA-256 above. Parsed JSON objects are equal and contain 18 exported types. Equality applies to recorded surface content, not wider runtime or binary compatibility. No historical artifact is rewritten to manufacture raw equality.

Separately, frozen API `docs/design/restricted-consumer-contract-rc2a-01-api.md` remains Git blob `2d6b9d12cab02f092c6b6e0b690138cf6726f12b`, 6,338 committed LF bytes, SHA-256 `df5390c890f2dc59e9fee33fd08c4b173ebf31de32b6cf2bf506991061cae521`. Generated/checked-out document representations are not conflated with that Git identity. The exact computed SHA-256 is also recorded in the companion verification JSON.

## C. Regression, traceability and integrity checks

The complete R1 diff contains only eight modified files, no additions/deletions, renames or mode changes: the CQ1 package's `README.md`, `reviewer-handoff.md`, `traceability-matrix.md`, `SHA256SUMS`; `docs/handoff.md`; `docs/work-queue.md`; `docs/plans/restricted-consumer-contract-implementation.md`; and `docs/plans/restricted-consumer-contract-rc2a-02-qualification.md`. The tracker changes correct F01 and label R1/Q2 status without granting acceptance. `git diff --check c946df0 271e34b` passes.

| Protected object | Identical at accepted baseline, original producer and R1 |
|---|---|
| `src` | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| `native` | `85631d2f04512376621d3d09f51d67fd90f45335` |
| `tests` | `9c906b349b0bf0cffdee331fb5700f9b7828c199` |
| Frozen API | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` |

The complete design, decision and review trees also match across those three commits; no assessment path changes. All three inherited qualification-package trees are identical between original producer and R1. `source-binding.json` is byte-identical; all 20 inherited commit bindings resolve. This supports reuse of Q1's nine-checkpoint-plus-original-target identity verification, historical 02A 29/29, accepted 02B-R1 43/43 and 02C 40/40 manifest results, and historical probe/TRX assessments. Those old campaigns were not repeated.

All **13 unique explicit commit:path references** and **14 Markdown file/section links** in the three package Markdown files and R1-added tracker text resolve using Git objects. Cross-branch URL references were checked against the corresponding local commit/path and heading; publication/network availability is not assumed. README §§4–5 are byte-identical to the original producer. Of the 13 capability rows, only C4/C7/C10/C12 change; of the nine inherited-qualification rows, only Q3 changes; all five exclusions are unchanged.

| Claim | Q2 assessment | Basis and continuing limitation |
|---|---|---|
| C1 | SUPPORTED | Q1 field/view mapping and accepted results reused; exact row and supporting package unchanged. |
| C2 | SUPPORTED WITH QUALIFICATIONS | Q1 canonical/refusal evidence reused; two large vectors remain digest/recipe-bound; canonical-rigidity correction retained. |
| C3 | SUPPORTED | Q1 inclusive-size/refusal evidence reused; row and source/tests unchanged. |
| C4 | SUPPORTED | Corrected digest condition independently verified above; Q1's supported ordinal/null assertions remain applicable. |
| C5 | SUPPORTED WITH QUALIFICATIONS | Q1 re-encode source reasoning and accepted corpus reused; no formal proof over arbitrary future input or new fault injection. |
| C6 | SUPPORTED | Q1 historical API inventory verification reused; exact API blob/hash freshly verified. |
| C7 | SUPPORTED WITH QUALIFICATIONS | Correct originating decision verified; test-result versus binary-byte limitation remains. |
| C8 | SUPPORTED WITH QUALIFICATIONS | Q1 historical portability evidence reused with accepted B R1/Q2 corrections and authorization audit-trail disclosure; WSL2 is not native Linux. |
| C9 | SUPPORTED WITH QUALIFICATIONS | Q1 passive-reference evidence reused; Windows scope and F01 erratum remain; equality/hash does not guarantee cross-process hash determinism. |
| C10 | SUPPORTED WITH QUALIFICATIONS | Surface content and corrected byte domains freshly compared; passive API surface only, no downstream enforcement guarantee. |
| C11 | SUPPORTED WITH QUALIFICATIONS | Q1 R1-F04 documentation assessment reused; D4a enrolled/synthetic and D4b actual provider-origin timing-source limitations retained. |
| C12 | SUPPORTED WITH QUALIFICATIONS | Corrected assessment and separate accepted decision verified; E2/E6 and no-mandatory-consolidated-run boundary retained. |
| C13 | SUPPORTED WITH QUALIFICATIONS | Q1 checkpoint results reused and exact R1 protected identities independently extended; this proves preservation and grants no runtime capability or owner acceptance. |

All four current payload hashes recompute correctly over R1 Git blobs and the producer's read-only checkout bytes:

| Payload | Recomputed SHA-256 |
|---|---|
| `README.md` | `cf76b78e58310546cac309271a53bfe898a8972635f970f2dad6c2a01d34ece5` |
| `reviewer-handoff.md` | `0b0215f975d32997e54a8ba54bb2cca26c378ea11662ba5d39617f78da9439eb` |
| `source-binding.json` | `61ca34b03d026b12a53cd266d9c0bc35797792f25b6f320ff319747704bc08ab` |
| `traceability-matrix.md` | `4b173e3ea1d9ea80915661b26c1356986f4ecfe78393c4f2f3772a6004fba9a8` |

The manifest contains exactly those four payloads, excludes itself, and does not embed its containing commit. No circular integrity dependency was used.

## D. Restrictions to carry into any owner acceptance

1. **Passive security boundary.** Successful decoding grants no authority and proves no execution establishment or isolation. Neither digest equality nor a caller-supplied passive reference establishes provider provenance, live authorization, temporal/deadline/freshness/liveness guarantees, release/retained-state authority, native confinement or end-to-end execution. R1-F04 and the D4a/D4b timing-source distinction remain obligations for future consumer documentation/Hufu review.
2. **Platform scope.** Windows is primary; the 02A/B managed assertion set has separately recorded WSL2 evidence. The later 02C focused surface qualification is Windows-only. WSL2 is never native Linux; native Linux/macOS remain **NOT RUN**. No new platform or runtime qualification follows from constant source/native Git identities.
3. **Large vectors and reproducibility.** Two vectors remain digest/recipe-bound without standalone byte exports. Test-outcome reproducibility remains distinct from DLL-byte reproducibility; no binary, package, distribution or deployment reproducibility/compatibility guarantee is added. The corrected RC2A-01 origin must be used while the historical RC1-A citation error remains disclosed.
4. **Evidence dispositions.** Preserve Q2C-Q2-F01's 39/39-to-40/40 acceptance erratum without altering historical `validation.md`; the 02B authorization-provenance/audit-trail limitation; Q2A02A-F01's accepted canonical-rigidity correction; and precise-filter authority versus the broad-filter 16 extra managed passes/41 native skips. Read original B evidence with its accepted R1/Q2 corrections. Historical failed/zero-result attempts do not become passes. Git records do not independently authenticate the original human authorization.
5. **Bounded proof.** The re-encode reachability conclusion is source reasoning plus the accepted corpus. Surface inventory equality does not prove downstream enforcement or raw equality across differing representations. Full-stage E2/E6 qualifications remain; no consolidated cross-substage run occurred or was mandatory. No new combined execution is claimed.
6. **Remaining gates.** RC-2B R1-F02/F03 remain open; R1-F06 HUFU-R recheck and AE4-B01 remain open; native, facade, package/clean-consumer/mixed-graph, Hufu and publication gates remain unqualified or unauthorized. Mode `work` remains descriptive. RC2A-03 awaits owner acceptance; RC2A-04 is unauthorized.

## E. Overall verdict

**SUPPORTED WITH QUALIFICATIONS.** F01–F04 are closed for R1 `271e34b45fa504f6bb7ef7359c4f2f1a9649e49d`. No new in-scope blocking discrepancy was found. All 13 claims remain supported within their stated and inherited qualifications. The independent review does not erase Q1's findings against the original qualification commit.

## F. Owner recommendation and completion

RC2A-03 is **eligible for acceptance with qualifications** on the exact R1 commit/tree in §A. The next owner action is a separate explicit acceptance decision carrying §D's restrictions and referencing both preserved Q1 and this Q2 report. Unqualified acceptance would overstate the evidence. Further remediation of F01–F04 is not indicated by this verification.

Only this Q2 report and its companion verifier, captured results and binding receipt are added to the reused review branch. Q1 report/evidence and every pre-existing tracked review-worktree file are preserved. The companion receipt records the report commit/tree and the producer-preservation checks after that commit. The final clean-state check and receipt-commit identity are supplied in the handoff. Stop after Q2: no owner acceptance, RC2A-04, implementation, merge, rebase, push, worktree cleanup or consolidation is performed.
