# GAGAMBA-RC2A-03-CQ1-Q1 — Independent component qualification verification

Review date: 2026-10-26 (Asia/Manila). Reviewer: Codex, independent security-oriented record review.

**Overall disposition: REMEDIATION REQUIRED.** The component identities and underlying accepted passive-contract evidence are supported. The qualification record is not ready for sign-off: its C12 assessment binding does not resolve, and C4 states an incorrect digest-verification condition. Two additional bounded provenance/byte-domain discrepancies are recorded below. These are documentation/evidence findings, not demonstrated production defects. No owner acceptance is issued.

## A. Git binding and isolation

| Item | Independently observed identity |
|---|---|
| Repository / origin | `jenolaszlo-sketch/gagamba` / `https://github.com/jenolaszlo-sketch/gagamba.git` |
| Reviewed qualification branch | `qualification/gagamba-rc2a-03-cq1` |
| Reviewed commit | `c946df0c93dde414c744f46cce054e7cd3f2c26e` |
| Reviewed root tree | `ba7a3ef73a651cf5ace62583873742f855330910` |
| Reviewed parent / accepted RC2A-02 baseline | `9519e7cb612af6ff69c93c0d128553138533d9fd` |
| Baseline root tree | `7b162d2b0e00d72c27a2389ed505d531758797f1` |
| Baseline parent | `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` |
| Review branch | `review/gagamba-rc2a-03-cq1-q1` |
| Review worktree | `C:\Users\Laszlos\source\repos\Solo\rc2a-03-cq1-q1-review` |
| Producer worktree (read only) | `C:\Users\Laszlos\source\repos\Solo\rc2a-03-cq1` |
| Review report commit / tree / parent | Exact post-commit identities are recorded in the companion `rc2a-03-cq1-q1/git-binding.json`, committed after this report to avoid circular self-hashing. Its `reviewReportCommit`, `reviewReportTree`, and `reviewReportParent` bind this report and its verification evidence. |
| Worktree status | Producer clean before review; isolated review created clean at the exact target. Only this report and its companion audit files are added. Final clean-state and producer-preservation observations are recorded in the companion receipt and completion message. |

The suggested branch and worktree were both absent before creation (`git branch --list`, `git worktree list --porcelain`, `Test-Path`). Drive C had approximately 71.72 GiB free. `git merge-base --is-ancestor 9519e7c c946df0` returned 0; the exact parent establishes ancestry directly. No uncommitted target changes were present (`git status --porcelain=v1 --untracked-files=all`). No baseline was taken from `main`. Creating the requested worktree required access to shared Git metadata outside Solo; the first sandboxed attempt failed without creating the branch, and the authorized retry succeeded.

Methods: independently authored [read-only verifier](rc2a-03-cq1-q1/verify_record.py), [captured results](rc2a-03-cq1-q1/verification.json), direct Git object inspection, complete target documentation diff, source/API/test inspection, raw TRX parsing, SHA-256 recomputation, and reading preserved reviews and owner decisions at their own commits. Historical run results below are inspected evidence, not new execution. No build, restore, test suite, native probe, pack, publication, implementation, merge or push was performed. Tracking files were not updated because the commission permits only the independent review and forbids altering the producer record.

## B. Independent identity verification

Every checkpoint resolves to an object of type `commit`. For **each of the nine rows below**, the following exact path/object/type triple was independently obtained with `git rev-parse <commit>:<path>` and `git cat-file -t <commit>:<path>`:

| Path | Exact object ID at every checkpoint | Type |
|---|---|---|
| `src` | `b8c167e3d7340a562221591bdc2b6df7adb372d1` | tree |
| `native` | `85631d2f04512376621d3d09f51d67fd90f45335` | tree |
| `docs/design/restricted-consumer-contract-rc2a-01-api.md` | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` | blob |

| Checkpoint (full commit) | Root tree | Three identities and types |
|---|---|---|
| `68b01fbb53eefca9c8ca2f83ff50641521020a2a` | `54f659d01937da29bc9a5f82566a8349af264664` | MATCH / tree, tree, blob |
| `71a28afe2623516fb4176e6a4ce7988a3d70df06` | `70b932148cbfa59b0e424229879eebd36df2d80c` | MATCH / tree, tree, blob |
| `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | MATCH / tree, tree, blob |
| `1cd22b9b109ddba4673b11487bceea62d2e92117` | `2f2bae947dee368ae073dd3fe7877380ca8854ff` | MATCH / tree, tree, blob |
| `c4837b0b434fc75ebc3e08d278bdc09c3965a181` | `d47eb6173ecf7b4de442f0f9c165f3ef9e24c88d` | MATCH / tree, tree, blob |
| `62b75f5866d0c339816712baeb04baa386b06f85` | `66f996e1cf973e48487a073b07f1e78d9139c911` | MATCH / tree, tree, blob |
| `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` | `b3efa338fe5625f8f86fa49fb8db1f6fd62e1bda` | MATCH / tree, tree, blob |
| `058a150130c5f112c7e9b8189bba8842cf0cec52` | `b2211a26c838f98f994602e5c56ea8bb95b2c1d8` | MATCH / tree, tree, blob |
| `9519e7cb612af6ff69c93c0d128553138533d9fd` | `7b162d2b0e00d72c27a2389ed505d531758797f1` | MATCH / tree, tree, blob |

The same three identities/types also match at the reviewed `c946df0` tip. All ten RC-1 design blobs are `4b300b01397e7f68d978fadbc36164dda17c8721`. Full parents and per-checkpoint object results are in `verification.json:checkpoints`; all 20 `source-binding.json` inherited commit references resolve as commits with recorded full root-tree/parent results in `chain`.

The assessment `058a150` and baseline `9519e7c` are siblings sharing parent `a36cb684`; the assessment is **not** an ancestor of the target. It is a separately preserved assessment, not an accepted implementation commit. Identity equality across these checkpoints is valid without implying a linear ancestry through the assessment. Accepted baseline ancestry is independently established.

No production, native, frozen-API, or RC-1 contract drift was found. Between `9519e7c` and `c946df0`, tests and all design/decision/review files are unchanged. Across RC2A-01 to the accepted baseline, tests add 18 files and strengthen `RestrictedContractTests.cs`; production is constant. This supports continuity of the accepted compatibility evidence, not binary or package compatibility.

## C. Traceability assessment — all 13 claims

Evidence shorthand below is commit-specific: **A** = `c1a01761f8d58136a381ad87269635c6c14ad093:docs/evidence/restricted-consumer-contract-rc2a-02a-windows/`; **B** = corrected accepted `1cd22b9b109ddba4673b11487bceea62d2e92117:docs/evidence/restricted-consumer-contract-rc2a-02b-portability/`; **C** = `62b75f5866d0c339816712baeb04baa386b06f85:docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/`. These packages' root trees match the cited identities (A `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`, B `2f2bae947dee368ae073dd3fe7877380ca8854ff`, C `66f996e1cf973e48487a073b07f1e78d9139c911`).

For C1–C7, review `7b634455` was read at `docs/reviews/restricted-consumer-contract-rc2a-02a-glm-q1.md` and acceptance `e5fff9e8` at `docs/decisions/restricted-consumer-contract-rc2a-02a-owner-acceptance.md`. For C8, Q1 `5bc396c2` and Q2 `60de1f2d` reports and acceptance `dc029519` were read. For C9–C11, report `f8b692e:docs/reviews/rc2a-02c-q2-independent-verification.md` and acceptance `a36cb684` were read. Their verdicts and owner dispositions are represented accurately except for the specifically noted provenance/byte-domain claims. These are local Git records of owner decisions, not independent authentication of the humans who issued them.

| Claim | Row-level verdict | Independent assessment and evidence |
|---|---|---|
| C1 | SUPPORTED | A `traceability-matrix.md`, `exports/mutation-coverage-matrix.json`, frozen API and `RestrictedManifestView.cs` agree on the field/view mapping. Independently counted 72 unique field IDs, 279 unique cases, no field without cases; inspected composite-property and exact-refusal assertions. C inventory has 12 view types among 18 exports. A raw public 67 and precise 457 results pass; Q1 §E records 72/72 verification and 103 probes. All evidence/acceptance bindings resolve. |
| C2 | SUPPORTED WITH QUALIFICATIONS | A vector/case sources, A5 catalog, reader, and Q1 §§C–F support the 12-code vocabulary, canonical identity and exact refusals. Rehashed all 21 embedded hex payloads; all match. Two of 23 remain digest/recipe-bound (F03), not independently materialized in this review. The 222,615 sweep's 114,378 accepted mutations represent valid alternate canonical data, not authority invariance; Q2A02A-F01 remains essential. Existing TRX includes the sweep and 873-prefix tests; this review did not execute them anew. |
| C3 | SUPPORTED | A `boundary-results.json`, `Q2A_B_BoundaryTests.cs` B01–04, bounded Encode and reader checks substantiate inclusive 65,536, symmetric 65,537 refusal/no partial return, and pre-parse refusal at 1,000,000. Existing TRX names/outcomes match the asserted scope; Q1 §G and acceptance preserve it. |
| C4 | UNSUPPORTED | As written, “32 true” does not state the required digest-content match and is false for nonmatching 32-byte values. A public test B22 explicitly asserts matching true and changed/all-zero false; frozen API and `RestrictedManifestCodec.VerifySha256` require equality after checking length. Other C4 assertions (wrong lengths, exact ordinal Supports, null ordering including B23) are supported. See F02. |
| C5 | SUPPORTED WITH QUALIFICATIONS | A `reencode-branch-assessment.md`, `ManifestReader.cs:15–29`, boundary B18/B19 and Q1 §§F/H/K support pass-through exercise on accepted decodes and no observed independently triggerable failure. This is source reasoning plus the accepted corpus, expressly not a formal proof over arbitrary future input; no rejection-path fault injection was performed. |
| C6 | SUPPORTED | A `document-sha256.json` binds the exact frozen API blob, 6,338 LF bytes, SHA-256 `df5390c890f2dc59e9fee33fd08c4b173ebf31de32b6cf2bf506991061cae521`. Independently verified 20/20 A document digests and 18/18 source-file blob/size/digest bindings. Nine checkpoints plus target match; R2A-F02 follow-up is legitimately satisfied. |
| C7 | SUPPORTED WITH QUALIFICATIONS | A E4, Q1 §L, and actual RC2A-01 acceptance `49ebbfec` §4 preserve test-outcome versus DLL-byte reproducibility. No binary/package/deployment claim follows. Qualification Q3's attribution to RC1-A is wrong (F03), although the rule and the later accepted dispositions are real. |
| C8 | SUPPORTED WITH QUALIFICATIONS | B raw Windows/WSL2 TRX each has public 67 and precise 457 passes; both 457 reruns and cross-platform name/outcome multisets match independently. 43/43 manifest and 9/9 document digests pass. Q1's Windows reproduction is supported by its committed 67/457/457 TRX; Q1 did not rerun WSL2 and Q2 parsed evidence only. Original `dd16baec` must be read with accepted R1 corrections, not its stale nested artifact hashes. WSL2 remains WSL2, native Linux/macOS NOT RUN, audit-trail note disclosed. |
| C9 | SUPPORTED WITH QUALIFICATIONS | C 13-row matrix, three final test results, constructor/equality test bodies and actual source support four nonempty GUIDs, 32-byte copied digest, named errors, immutable storage and all-field equality/hash behavior. Independently read both Q2 successful 3-test TRX and the preserved zero-result timeout. Windows passive value only; equal-value hashes are not a cross-process deterministic-hash guarantee; F01 erratum remains applicable. |
| C10 | SUPPORTED WITH QUALIFICATIONS | The frozen API, source, exact reference assertions and all three JSON inventories agree: 18 exported types; sealed reference; one constructor; five get-only properties; equality/hash; no temporal/provider/release/lookup/control members; inherited Object members separately recorded. Literal committed-byte identity is false, although JSON and LF-normalized bytes are identical (F04). Absence of members proves this surface boundary only. |
| C11 | SUPPORTED WITH QUALIFICATIONS | C `documentation-review.md`, RC-1 §RC-D04 and `RestrictedPreparationReference.cs:5–7` preserve D4a enrolled/synthetic versus D4b actual provider-origin timing. Q2 Q2A-S-13 and acceptance §5 concur. No timing/freshness/liveness or downstream Hufu/provider behavior is qualified. Future package docs/Hufu delta review must carry R1-F04. |
| C12 | INSUFFICIENT EVIDENCE | The exact commit:path cited in the row is absent; it also points to §5 where the actual report's E1–E8 table is §4. Thus the row's stated evidence binding cannot be verified (F01). Independently locating the actual FSA-01 report and reading acceptance `9519e7c` §§5–8 supports the underlying stage disposition, including E2/E6 qualifications and no consolidated run requirement. This is a recoverable citation defect, not a claim that the historical acceptance or evidence is missing. |
| C13 | SUPPORTED WITH QUALIFICATIONS | Independently reproduced all nine object comparisons and extended to the actual target, including types, paths, root trees and RC-1 design identity. This supports no drift, not any additional runtime capability. The producer correctly labels C13 PREPARED/review pending and owner acceptance pending; this review supplies independent verification only. Owner acceptance remains unissued and the overall record still requires correction. |

## D. Security and platform assessment

The accepted evidence supports passive canonical encoding/decoding, strict refusal and no partial views, immutable view/reference behavior, the frozen public API, the bounded mutation/compatibility corpus, and deterministic test outcomes under the executed profiles. Read-only code inspection agrees: `RestrictedManifestCodec` explicitly denies authority; `ManifestReader` checks grammar, invariants and byte-exact re-encoding; `RestrictedPreparationReference` validates and copies caller data without attestation or control operations.

**Successful decoding grants no authority and proves no execution establishment or isolation.** No packet statement was found granting live authority, issuing/verifying execution authority, proving producer provenance by decoding, establishing native confinement, or qualifying stop/correlation/recovery/lifecycle behavior. C4's digest wording must nevertheless be corrected: digest length is not digest verification, and digest equality itself is not provenance or authorization.

Excluded: public restricted-execution facade/RC-2B; provider-origin preparation, deadlines, release and retained-state behavior; native sandbox/isolation; package/clean-consumer/mixed-dependency compatibility and publication; Hufu integration/AE4-B01 and end-to-end execution. Constant native Git identity is preservation evidence, not native qualification. The continued exclusions in README §4, matrix X1–X5 and the full-stage decision are effective.

Windows is primary: recorded Windows NT build 26200/AMD64, SDK 10.0.401, runtime 10.0.12 for A/B. The WSL2 record explicitly reports `environment=wsl2`, Ubuntu 26.04.1, kernel `6.6.114.1-microsoft-standard-WSL2`, SDK 10.0.113/runtime 10.0.13. It supports the A/B managed assertion set only, not C's later Windows-only focused surface qualification. Native Linux and macOS remain **NOT RUN** for this consumer qualification. Earlier unrelated native evidence is not imported as consumer qualification.

The broad Windows filter's 514 outcomes consist of 473 passes and 41 NotExecuted; the precise authoritative set is 457 passes/zero skips. The extra 16 passes are the two managed classes; 41 skips are the three previously disclosed Linux-native classes. Test assembly names containing `Linux.M1` do not establish native execution. Failed SourceLink setup and zero-result VSTest attempts remain historical failures/environment observations, not passing evidence. No new native probe was executed.

## E. Inherited qualifications and historical dispositions

Read against RC-1 review `feb9d758` §G, RC1-A `9a705335` §§3–10, RC2A-01 review `8e41bab` §§D–G and acceptance `49ebbfec` §4, the A/B/C reviews and decisions, and full-stage `9519e7c` §§5–9:

| Qualification / obligation | Preservation assessment |
|---|---|
| Q2C-Q2-F01 (39/39 versus 40/40) | Preserved in CQ1 README §5 and Q1; actual C manifest independently matches 40/40. Acceptance `a36cb684` §4 explicitly corrects by erratum, superseding the review's suggested evidence-file correction. The historical `validation.md` remains unchanged. No unresolved bad digest is implied. |
| RC2A02A-GLM-F03 large vectors | Preserved in README §5/Q2/C2; 21 payloads embedded, two digest/recipe-bound. Acceptance `e5fff9e8` §6 accepts this limitation; no standalone byte-export claim is established. |
| R2A-F03 | Restriction preserved everywhere relevant; origin misattributed in Q3 (F03). Actual disposition is `49ebbfec` §4, subsequently carried by A/full-stage decisions. No binary, package, deployment or cross-process hash reproducibility claim. |
| 02B Q1 authorization audit trail | README §5/Q4 preserves the lack of independently authenticated separate instruction text in Git. Owner acceptance `dc029519` §4 accepts it as disclosed. This review cannot authenticate that source; it is not silently repaired by a green test run. |
| Passive-reference / R1-F04 boundary | Preserved in README §4–5/Q5/C9–C11, source XML and C acceptance §5. Future consumer package documentation and Hufu timing-source review remain obligations. Caller-supplied values, including all-zero digest content, confer no authority or provenance. |
| Native Linux/macOS and WSL2 | Q6 and README §4 preserve the explicit not-run exclusions and WSL2 distinction. Windows C surface coverage is not broadened to WSL2. |
| Q2A02A-F01 | Q7/C2 and README §5 retain the accepted canonical-rigidity/immutable-region correction. It did not show every non-digest mutation invalid and was not an authority-bypass finding. Original failed sweep remains preserved. |
| A F02 / broad-filter disclosure | Q8 and current tracking retain 16 additional passes versus 41 skips; precise filter authoritative. A acceptance §6 provides the historical wording clarification. B F-02 separately corrected by R1 and verified by Q2. Neither is an active implementation defect. |
| B F-01 stale nested hashes | Corrected by R1, accepted via `dc029519`; original qualification remains historical. C8 expressly includes R1/Q2, rather than silently marking the original snapshot sufficient. |
| A F04 Q2A-S deferral | Historically open, then assigned by `91612b9`, covered by C, accepted by `a36cb684`, reconciled by full-stage `9519e7c`. Closing the scoped deferral is supported; RC-2B live halves remain open. |
| R2A-F01/F02/F04 and R1-F01/F05 | Legitimate scoped follow-ups: re-encode assessment, API rebinding, combined null/wrong-length and argv distinctions, symmetric size ceiling and digest/Supports assertions. Current CQ1 does not reopen or independently grant acceptance to these dispositions. |
| Full-stage E2/E6; no consolidated run | Q9/README inherited scope preserves the large-vector and native-host limits. Full-stage §8 expressly says no consolidated cross-substage run occurred or was mandatory. CQ1 is not a new combined execution. |
| R1-F02/F03, R1-F06, R1-F07 and downstream gates | Stop-latch/correlation semantics remain RC-2B OPEN; R1-F06 evidence-location recheck remains HUFU-R; R1-F07's accurate historical capability disclosures do not qualify C1–C4/native/runtime/package gates. Existing plan rows preserve these, and the CQ1 diff advances none. AE4-B01 remains open. Mode `work` remains descriptive, not authorization of arbitrary commands or a broader execution profile. |

The restrictions are substantively carried forward. Q3's attribution defect and the newly identified C10 byte-domain qualification require explicit treatment; neither authorizes editing the historical decisions/reviews. Historical “not accepted/not started” text is read in chronological context with subsequent scoped decisions, not as a current reversal of those decisions.

## F. Documentation and integrity audit

The complete `9519e7c..c946df0` diff has exactly nine paths: five added files in `docs/evidence/restricted-consumer-contract-rc2a-03-cq1/` and four modified trackers (`docs/handoff.md`, `docs/work-queue.md`, `docs/plans/restricted-consumer-contract-implementation.md`, `docs/plans/restricted-consumer-contract-rc2a-02-qualification.md`). Raw modes/object IDs are recorded in `verification.json:rawDiff`. There are no implementation, native, test, project/package, design, historical review or decision changes, renames, or mode changes.

The stale implementation-plan RC2A-02 row is correctly changed to FULL STAGE ACCEPTED WITH QUALIFICATIONS. RC2A-03 remains CQ1 prepared/independent review pending/owner sign-off pending. RC2A-04 remains unauthorized; no unrelated native, facade, Hufu or package milestone advances. The refreshed RC2A-02 row introduces the same incorrect FSA-01 path as C12 (F01). The qualification-plan addition is §15; prior acceptance/plan-correction text remains intact.

Whitespace-sensitive `git diff --check 9519e7c c946df0` returns 2 solely for `docs/evidence/restricted-consumer-contract-rc2a-03-cq1/SHA256SUMS:5: new blank line at EOF`. This is nonblocking whitespace, not a fifth payload or integrity failure. The implementation-plan heading also replaces an em dash with a hyphen; no semantic change follows. No unexpected whitespace-sensitive implementation change exists.

Independent SHA-256 over **Git blob bytes and the review checkout's raw bytes** matches all four CQ1 manifest entries:

| Payload | Recomputed SHA-256 (both domains match the manifest) |
|---|---|
| `README.md` | `e46e1cd4149b81049e05a7cb4a19917c81ccb2c44e8f280d8360e87e33d0f176` |
| `reviewer-handoff.md` | `d88918326e7caa19da7cc4f257053edfac779a4968b8e54bdc44ac083a0cc497` |
| `source-binding.json` | `61ca34b03d026b12a53cd266d9c0bc35797792f25b6f320ff319747704bc08ab` |
| `traceability-matrix.md` | `4005739b857335a7785e05d58d037a6eee03ef7a9385f8f8e1492d058574149d` |

Additional independent checks: A 29/29, accepted B 43/43, C 40/40 package digests; A 20/20, B 9/9, C 16/16 document bindings at their **historical commits**, not today's changed trackers; A 18/18 test-source blob/size/hash bindings; 21/21 embedded vector digests; all 10 Markdown relative file links in the CQ1 Markdown and added tracker text resolve. Code-formatted cross-branch references require separate inspection: the roadmap path resolves at `d417367`, but the FSA-01 path at `058a150` does not (F01). A valid manifest authenticates the bytes containing the incorrect citation; it cannot make the citation true.

`source-binding.json` baseline commit/tree/parent, component object paths/IDs and all 20 inherited commit references agree with Git. Its PASS claim was independently recomputed, not trusted. The packet avoids self-hashing its containing commit and checksum file; no circular evidence was used to establish behavior. C13's producer assertion alone was insufficient until this review reproduced it. Historical reviews are separately preserved; this review inspected actual records and available raw outcomes rather than taking the CQ1 handoff's claimed results as proof. Exact arbitrary source-author identity and historical external authorization text are outside what Git objects can authenticate; the inherited 02B qualification covers the latter limitation.

## G. Findings

### RC2A03-CQ1-Q1-F01 — P2 / medium / blocking: broken full-stage assessment binding

**Evidence:** target `traceability-matrix.md:28` (C12), `reviewer-handoff.md:25–27`, and `docs/plans/restricted-consumer-contract-implementation.md:156` name `058a150130c5f112c7e9b8189bba8842cf0cec52:docs/assessments/rc2a-roadmap-01-restricted-execution-dependency-review.md`. `git cat-file -e` fails for that exact reference. `git ls-tree` locates the actual `docs/assessments/rc2a-02-full-stage-acceptance-gap-assessment.md`, blob `5b4f559d71e8bb6b871a28f73be694b742887bdd`, at the same commit/tree. The E1–E8 table is §4, not C12's §5 (which maps requirement sources). The roadmap path belongs to `d417367`, a different assessment.

**Impact:** a central acceptance-evidence citation cannot be independently resolved as recorded. Recovering a different path during review does not make the producer's exact binding correct. The underlying historical stage disposition is independently supported; this is a material traceability defect in a qualification record whose purpose is durable exact binding.

**Required disposition:** a separately authorized documentation correction must bind all three references to the actual FSA-01 path/commit/tree and correct C12's section pointer, then regenerate affected CQ1 payload digests and undergo focused verification. Preserve original reviewed evidence and historical decisions. No production change or new test campaign is indicated. Not fixed here.

### RC2A03-CQ1-Q1-F02 — P2 / medium / blocking: C4 omits digest equality

**Evidence:** target matrix `traceability-matrix.md:20` states digest length rules “0/31/33/64/65,536 false; 32 true”. Frozen API §API and failures says valid-length matching/nonmatching returns true/false. `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs` checks `expectedSha256.Length != 32`, then compares actual and expected digest using `FixedTimeEquals`. The accepted `tests/Gagamba.Execution.Tests/Restricted/Rc2a02PublicBoundaryTests.cs:39–47` asserts matching true, changed 32-byte false, all-zero 32-byte false against the golden; its B22 result is Passed in A's public TRX.

**Impact:** the C4 summary as written equates correct digest length with successful verification. Its “present-not-wildcard” phrase does not supply the missing content-equality condition. This is an incorrect security-relevant API claim, not a production bypass: implementation and accepted tests enforce the correct rule.

**Required disposition:** explicitly state “matching 32-byte digest true; nonmatching 32-byte digest false; wrong lengths false for a non-null manifest; null manifest throws before length checking.” Keep the all-zero present-value and non-authority boundary. Correct the qualification row and regenerate its digest under separate authorization; no code/test modification is required. Not fixed here.

### RC2A03-CQ1-Q1-F03 — P3 / low / nonblocking alone: R2A-F03 source decision misattributed

**Evidence:** target matrix `traceability-matrix.md:37` Q3 cites RC1-A `9a705335701f0c1923bd9867a2612b8a93afa8cb` as the R2A-F03 source decision. That RC1-A record has no R2A-F03 disposition; §4 approves Mode `work`. The actual R2A-F03 review is `8e41bab`, and acceptance is `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e:docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md` §4. The full-stage historical decision itself also repeats an inaccurate RC1-A origin, so this is inherited citation debt rather than a newly invented restriction.

**Impact:** the rule is correctly preserved and supported by later accepted records, but the named original decision does not support the attributed finding. A qualification provenance audit must distinguish RC1-A from RC2A-01 acceptance.

**Required disposition:** correct the new qualification attribution to the actual review/decision, and explicitly disclose any carried historical citation error without rewriting the immutable prior acceptance. Carry the no-binary/package/deployment restriction unchanged. Not fixed here.

### RC2A03-CQ1-Q1-F04 — P3 / low / nonblocking alone: inventory byte-domain claim needs qualification

**Evidence:** target C10 `traceability-matrix.md:26` calls the inventories “byte-identical across qualification, review, and rerun inventories.” Direct committed-byte comparison gives:

| Object | Bytes / newline domain | SHA-256 |
|---|---|---|
| `62b75f58:docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/runs/windows/final/public-surface-inventory.json` | 34,221; 904 CRLF pairs | `4b53a523634c304336cb3faf01a1133d038bb74d1de32eecf6e1023936f06a0b` |
| `f8b692e:docs/reviews/rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json` | 33,317; LF | `9e35d1ea33083537f6bab295f764423a69bd4f9d913d055ff42e9f7d3bf774d1` |
| `f8b692e:docs/reviews/rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json` | 33,317; LF | `9e35d1ea33083537f6bab295f764423a69bd4f9d913d055ff42e9f7d3bf774d1` |

After CRLF-to-LF normalization, all three are byte-identical; parsed JSON is also equal. This does not refute the prior review's observation about Windows-generated/checkout bytes at run time, but the new Git-bound qualification does not specify that byte domain.

**Impact:** literal unqualified committed-byte identity is not established; public API content equality is established. No changed member or corrupted inventory was found.

**Required disposition:** qualify C10 as equal parsed content and equal LF-normalized bytes, or explicitly bind a demonstrated checkout-byte domain. Preserve both original committed artifacts and their valid hashes; do not rewrite historical evidence to manufacture byte equality. Not fixed here.

## H. Overall verdict

**REMEDIATION REQUIRED.** All 13 rows were audited. Nine-checkpoint identities plus the target and package hashes pass. The accepted passive behavior, security boundary, platform limits and substantive inherited restrictions are supported, but F01 and F02 prevent sign-off on this exact qualification record. F03/F04 must remain explicit bounded qualifications until corrected or separately dispositioned. No claim of owner acceptance follows from this review.

## I. Owner decision recommendation and completion

Do not accept RC2A-03 on the exact `c946df0` record. Commission only a bounded documentation/evidence correction for the findings, preserving this review and the original packet, then obtain focused independent verification. The existing accepted implementation evidence does not require a new build/native/test campaign for these record defects. Following successful verification, any owner acceptance must still carry all applicable inherited qualifications; an unqualified sign-off would remain inappropriate.

The review report and independently authored audit evidence are committed solely on the isolated review branch. The companion binding receipt records the report commit/tree/parent and clean producer/review checks after that commit. The final receipt commit's own identity and final clean status are supplied in the completion message, avoiding a self-referential Git hash. No producer file, prior decision, test or implementation is remediated. Stop here: no owner acceptance, RC2A-04, RC-2B, Hufu, merge, push or release is authorized or performed.
