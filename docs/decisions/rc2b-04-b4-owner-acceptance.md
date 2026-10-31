# RC2B-04-B4 Owner Acceptance and Cleanup Erratum

**Commission:** GAGAMBA-RC2B-04-B4-OWNER-01.
**Date recorded:** 2026-10-31 (Asia/Manila). Documentation-only owner decision. No implementation, no B5 work, no additional native experiments.

---

## 1. Verified Git Binding and Source Identities

| Identity | Verified Value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting HEAD (post-review) | `b95a82305245ef9f30571879b01972f58750b802` |
| Authorized B4 baseline | `4a181dc41d865bd46c6fca68a64fc7ea3a7fb26b` |
| B4 implementation | `4bb4943df69af5f95b65300d90852c04d08a1926` |
| B4 implementation tree | `6c9535013e98fb83f72d4b628a03c59b0e99a1c7` |
| Producer evidence delivery | `ca7200937fe12f0cbccccd72ee157528630e9670` |
| Independent B4-Q1 review | `b95a82305245ef9f30571879b01972f58750b802` ([review document](../reviews/rc2b-04-b4-q1-independent-security-review.md)) |
| Producer evidence report | `docs/evidence/rc2b-04-b4-impl-01.md` |
| Raw evidence manifest | `docs/evidence/rc2b-04-b4-impl-01.sha256-manifest.txt` (281 files, SHA-256 `b02f2f73660c8e8aa241fb59bed88542fb6bd3db1bd4c716db102c1d59800397`) |

Verified linear ancestry (each successor's sole parent is its predecessor):

`4a181dc4` (authorized baseline) → `4bb4943d` (B4 implementation; tree `6c953501`) → `ca720093` (producer evidence delivery) → `b95a8230` (independent B4-Q1 review; starting `main` HEAD of this decision).

Documentation scope and historical evidence preservation, verified at this decision:

- `ca72009` modified exactly `docs/PROJECT-STATUS.md`, `docs/handoff.md`, `docs/work-queue.md` (current pointers) and added `docs/evidence/rc2b-04-b4-impl-01.md` with its 281-entry manifest. No historical decision, review or evidence document was modified.
- `b95a823` added only the independent B4-Q1 review document.
- B4-Q1 independently rehashed the QF-01 (158) and QF-02 (528) manifests: 686/686 match; the 281-file B4 manifest reproduces 281 OK / 0 mismatch / 0 missing; the 306 tracked source/test/native/eng files at `4bb4943` byte-compare with the private build source at zero differences.
- All original unrelated tracked and untracked material remains in place.

Applicable standing decisions read and preserved unchanged: the RC-1 acceptance (`restricted-consumer-rc1-owner-acceptance.md`), the C1–C4 human contract approvals with C3 Policy A (`gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md`), the R1-F03 Option A approval in the B1 owner acceptance (`rc2b-01-b1-owner-acceptance.md`), the INT-01 owner acceptance (`rc2b-03-int-01-owner-acceptance.md`) and the B1/B2/B3 acceptance chain.

---

## 2. Mandatory B4Q1-F01 Erratum (Cleanup Classification Correction)

**B4Q1-F01: ACCEPTED REPORTING ERRATUM, REMEDIATION DEFERRED FOR THE QUALIFICATION HARNESS.**

The original B4 producer report states: **21 Confirmed / 11 Unknown.**
The independently verified interpretation is: **20 Confirmed / 12 Unknown.**
The affected case is `missing-staging-ack`.

- Its physical cleanup is genuinely improved: the retained staging directory is removed, and the required process and fencing observations are available.
- The durable journal nonetheless contains `OwnershipCleanupState.Unknown = 4`.
- The successful persistence of an Unknown verdict is not equivalent to a durable Confirmed cleanup verdict.
- The five-flag runner classification (Confirmed iff all five `RecoveryFinal` flags are true) therefore overstates the transaction's cleanup status. B4-Q1 independently reproduced the case with the identical five flags and the identical `Cleanup=4` journal verdict.

Preservation: the original producer report ([B4 evidence](../evidence/rc2b-04-b4-impl-01.md)) and its raw manifest are preserved unchanged, including the original 21/11 tally in the historical producer text. This owner decision and the current project-status documentation record the correction.

No source remediation is required for B4 acceptance. **Required follow-up:** the qualification harness's cleanup classification must be corrected before any future stage uses its aggregate Confirmed count as authoritative. The corrected 20/12 classification must remain visible in subsequent qualification planning.

Neither `missing-staging-ack` nor `missing-domain-ack` is promoted to Confirmed merely because physical resource removal improved. Both carry the annotation "physical cleanup improved, durable verdict Unknown".

---

## 3. Owner Acceptance

**B4 ACCEPTED WITH QUALIFICATIONS.**

The owner accepts the demonstrated protected-capture and independent-native-stop implementation within its qualified WSL2 experimental profile:

- Owner-private protected stdout/stderr capture.
- Fixed per-stream and aggregate retained-byte limits.
- Synchronized quota accounting across concurrent drains.
- Bounded native draining and final-drain behavior.
- Independent stopping on the qualified capture faults.
- Capture-reading API limited to the authenticated controller.
- Checked offsets/counts and 16-KiB maximum reads.
- Single in-flight capture-reading operation.
- Immutable copied capture results.
- No recovery capture-reading or release authority.
- Independent native stopping while observation/status handling is blocked.
- Populated-domain empty observation before owner exit.
- Corrected `OwnerSafetyMonitor` disposal and stop dispatch.
- Nonblocking recovery reaping.
- Identity-checked retained-resource cleanup within demonstrated constraints.
- Preservation of C1–C4, Option A and original deadline semantics.

Evidence basis (as independently verified by B4-Q1):

- Producer B4 native matrix: 13/0/0.
- Independent B4 native reruns: 8/0/0.
- The remaining five B4 native cases were inspected with corresponding mechanism coverage, not independently rerun end-to-end (zero rerun/inspected overlap).
- Producer INT-01 matrix: 32/0/0.
- Focused independent `missing-staging-ack` reproduction.
- Independent capture, cleanup and disposal component tests: 12/0/0.
- Verified 281-file B4 evidence manifest.
- Verified source, artifact and historical evidence identities.

This acceptance is not a production or cross-platform qualification.

---

## 4. Review Finding Dispositions (B4Q1-F01 through F04)

| Finding | Severity | Owner Disposition |
|---|---|---|
| **B4Q1-F01** | MEDIUM | **Accept the corrected reporting interpretation (erratum recorded in §2).** Carry the qualification-harness classification correction as a required follow-up before future cleanup-qualification reporting. No B4 source remediation. |
| **B4Q1-F02** | LOW | **Accept the residual check-then-remove race for empty directory substitution within the existing threat model.** Do not claim atomic or race-free name-based removal. Carry a bounded filesystem-identity hardening requirement into subsequent lifecycle/security qualification. |
| **B4Q1-F03** | LOW | **Accept the bounded robustness limitation** involving unexpected drain-thread exceptions outside the handled exception types. Do not claim that every possible drain-thread failure produces an immediate stop witness. Require broader fault handling and corresponding evidence before production eligibility. |
| **B4Q1-F04** | INFORMATIONAL | **Accept the explicitly gated experimental store-failure hook.** Require production installation/qualification to exclude or appropriately isolate test-only fault injection. |

---

## 5. Inherited Findings and Obligations

| Item | Recorded disposition |
|---|---|
| Q1R-01 (post-exit population read) | Addressed within the B4 experimental profile: population sampled while the owner is alive and before observer continuation, with identity/inode facts and native receipts. |
| Q1R-06 (Dispose suppressed stop dispatch) | Fixed; independently reviewed against failing-before evidence. |
| Q1R-07 (recovery one-second blocking wait) | Fixed (nonblocking reaping); independently reviewed. |
| Q1R-08 (legacy D5_O03 flake) | Remains unresolved; a passing run does not close it. |
| R1-F02 (stop/publication enforcement) | **GLOBALLY OPEN**, partially satisfied by B4 evidence. Not closed globally by this acceptance. |
| INT-01 | Remains ACCEPTED WITH QUALIFICATIONS. |
| F-01 | Remains CLOSED within its accepted integrated-topology scope. |

The original findings, historical owner decisions and their exact qualification boundaries are preserved unchanged.

---

## 6. Continued Restrictions

B4 acceptance does not authorize or establish:

- Native Linux-host or macOS qualification.
- Isolation from malicious same-UID host processes.
- Full B5 sealed-C3 durability and deadline reconciliation.
- B6 installed-artifact/deployment attestation.
- B7 comprehensive lifecycle and fault qualification.
- Full R1-F02 closure.
- General-purpose or production restricted-execution activation.
- Hufu integration.
- VM/HCS work.

`RestrictedExecutionRuntime.Create()` remains unavailable for production restricted execution.

The corrected 20/12 cleanup classification must remain visible in subsequent qualification planning.

---

## 7. Documentation Delivery

Created: `docs/decisions/rc2b-04-b4-owner-acceptance.md` (this document).

Updated current pointers only:

- `docs/PROJECT-STATUS.md`
- `docs/handoff.md`
- `docs/work-queue.md`

Preserved unchanged:

- B4 producer evidence and raw manifest.
- Independent B4-Q1 review.
- INT-01 acceptance and evidence.
- All historical reviews and decisions.
- Unrelated tracked and untracked material.

Documentation and Git-preservation checks: exact-scope diff validation (exactly the four authorized paths), whitespace validation, ancestry and preservation verification as recorded in §1. Work on canonical `main`; no linked worktrees, forced pushes, rebases, resets or evidence rewrites. The decision commit is pushed normally and the live remote identity verified afterwards.

---

## 8. Final Handoff

**A. Git identities and owner-decision commit.**
Verified identities: baseline `4a181dc41d865bd46c6fca68a64fc7ea3a7fb26b`; implementation `4bb4943df69af5f95b65300d90852c04d08a1926` (tree `6c9535013e98fb83f72d4b628a03c59b0e99a1c7`); producer evidence `ca7200937fe12f0cbccccd72ee157528630e9670`; independent review `b95a82305245ef9f30571879b01972f58750b802`. The owner-decision commit is the normal child of `b95a8230` on canonical `main`, changing exactly the four authorized documentation paths; its exact SHA is bound in the current pointer documents and verified against the live remote after push.

**B. B4Q1-F01 cleanup classification correction.**
Corrected tally **20 Confirmed / 12 Unknown** (from 21/11); `missing-staging-ack` reclassified with the physical-improvement annotation. Qualification-harness classification correction required before future stages use its aggregate Confirmed count as authoritative.

**C. Owner verdict.**
**B4 ACCEPTED WITH QUALIFICATIONS** within the qualified WSL2 experimental profile.

**D. Accepted B4 capabilities.**
As enumerated in §3.

**E. B4Q1-F01 through F04 dispositions.**
As recorded in §4.

**F. INT-01 and R1-F02 status.**
INT-01 remains ACCEPTED WITH QUALIFICATIONS. R1-F02 remains GLOBALLY OPEN, partially satisfied by B4 evidence.

**G. Remaining B5/B6/B7 obligations.**
B5: sealed-C3 durability and deadline reconciliation. B6: installed-artifact/deployment attestation. B7: comprehensive lifecycle and fault qualification. Additional carried follow-ups: the harness cleanup-classification correction (B4Q1-F01), filesystem-identity hardening (B4Q1-F02), broader drain-fault handling with evidence (B4Q1-F03), production exclusion/isolation of the store-failure hook (B4Q1-F04), and the unresolved Q1R-08 D5_O03 flake.

**H. Documentation/evidence preservation results.**
Producer evidence, raw manifest, the independent review, INT-01 acceptance and evidence, and all historical reviews and decisions are unchanged; unrelated tracked and untracked material is untouched; preservation identities re-verified as recorded in §1.

**I. Continuing production restrictions.**
As enumerated in §6, including that `RestrictedExecutionRuntime.Create()` remains production-unavailable.

**J. Smallest next authorized action.**
None is authorized by this acceptance beyond the recorded documentation. B4 acceptance does not automatically authorize B5 implementation; B5 requires a separate bounded commission. Before any future stage that consumes cleanup-qualification reporting, the qualification harness's cleanup classification must first be corrected (B4Q1-F01 follow-up), under its own owner authorization.

**Stop after this acceptance decision.**
