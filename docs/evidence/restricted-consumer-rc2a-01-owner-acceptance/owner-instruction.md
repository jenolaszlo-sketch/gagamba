# RC2A-01 owner acceptance instruction

Source: project-owner assignment delivered in the coordination conversation on
2026-10-25 (Asia/Manila), titled **Gagamba RC2A-01 Owner Acceptance and
RC2A-02 Preparation**. This is a conversation-based owner instruction, not a
digital signature or externally authenticated approval. It is the approval
authority for the acceptance recorded in
[the RC2A-01 owner acceptance record](../../decisions/restricted-consumer-rc2a-01-owner-acceptance.md);
it is not runtime qualification or independent review.

The instruction is preserved verbatim below. Its exact byte hash is recorded in
[source-binding.json](source-binding.json).

---

# Gagamba RC2A-01 Owner Acceptance and RC2A-02 Preparation

You are acting as the implementation coordinator and evidence custodian.

RC2A-01 has completed independent GLM review with the recommendation ACCEPT WITH FOLLOW-UP.

Your task is to record the owner's acceptance of RC2A-01 and prepare the detailed, executable plan for RC2A-02.

This is a documentation and planning assignment. Do not implement RC2A-02 tests or production changes yet.

## 1. Owner decision

Record:

**RC2A-01 ACCEPTED WITH FOLLOW-UP.**

Acceptance applies only to the implementation and evidence at:

- Branch: `implementation/gagamba-rc2a-01`
- HEAD: `68b01fbb53eefca9c8ca2f83ff50641521020a2a`
- Tree: `54f659d01937da29bc9a5f82566a8349af264664`

Record the independent review findings R2A-F01 through R2A-F04 and preserve their dispositions.

Distinguish acceptance of implementation from completion of qualification.

Verify the canonical repository state before making changes. Do not rewrite or amend accepted commits.

## 2. RC2A-02 plan

Design a tests-first qualification stage covering:

### A. Independent consumer vectors
- Export canonical vectors with reproducible generation procedures.
- Verify them with an independent implementation.
- Preserve byte-for-byte reproducibility.
- Include malformed and noncanonical rejection vectors.

### B. Exhaustive field mutation coverage
- Cover all 72 manifest fields.
- Test schema and implementation identifiers.
- Exercise presence, ordering, lengths and value boundaries.
- Check refusal codes and offsets where specified.
- Distinguish valid mutations from malformed or noncanonical encodings.

### C. Boundary testing
- Include 65,536/65,537-byte manifest boundaries.
- Include 16,384/16,385-byte argv/environment boundaries.
- Add explicit empty argv-item and zero-length argv-list cases.
- Add null-manifest plus wrong-length digest assertions.
- Assess whether the defensive re-encode equality branch can be exercised meaningfully.

### D. Cross-platform portability
Define reproducible Linux and macOS qualification runs in addition to Windows.

Preserve platform-specific evidence separately.

Do not imply cross-platform success until actual native execution is demonstrated.

### E. Evidence hygiene
- Rebind the clarified API document in the current SHA-256 inventory.
- Record exact source commit and tree identities.
- Capture independent runner and test-result provenance.
- Distinguish test reproducibility from binary reproducibility.
- Preserve failed attempts and qualification limitations.

### F. Independent review
Define exit criteria requiring independent verification of the completed qualification evidence.

## 3. Explicit exclusions

Do not:

- Reopen RC-1.
- Change the accepted RC2A-01 public contract.
- Implement RC-2B obligations R1-F02/F03.
- Close AE4-B01.
- Resume Hufu.
- Publish packages.
- Claim native sandbox qualification.
- Treat RC2A-02 completion as automatic package qualification.

If qualification discovers a production defect, report it and propose a separate bounded remediation rather than silently expanding scope.

## 4. Deliverables

1. Committed owner-acceptance record.
2. Updated implementation roadmap and work queue.
3. Detailed RC2A-02 specification.
4. Traceability matrix linking requirements, tests and evidence.
5. Explicit entry and exit criteria.
6. Git-bound handoff report.

Use the existing repository documentation conventions.

Do not begin qualification execution without a separate authorization.

Commit documentation changes on a dedicated branch or worktree, leaving the accepted implementation branch intact.

Report the final Git identities and all remaining open obligations.

---

Custodian notes (not part of the instruction):

1. The instruction's canonical-repository-state verification was performed and
   passed: branch `implementation/gagamba-rc2a-01` resolves at the stated HEAD,
   whose root tree equals the stated tree, in the canonical Gagamba repository;
   the accepted worktree was clean before and after.
2. The instruction directed that the independent GLM review of RC2A-01 be
   preserved verbatim with its Git blob and SHA-256 digest bound, findings
   R2A-F01–R2A-F04 recorded with their dispositions in the acceptance record,
   and owner decisions kept separate from the unchanged review document.
3. At custodian start the review document was not present in any repository,
   branch, worktree or filesystem location accessible to the custodian; the
   owner selected verbatim preservation of the review text, which is preserved
   on `review/gagamba-rc2a-01-glm-independent` with its provenance disclosed in
   the review document header and the acceptance record.
