# RC2A-B owner-acceptance evidence

- Date: 2026-10-25 (Asia/Manila).
- Slice: independent-review preservation (Commit A, separate review branch),
  owner acceptance of RC2A-01, RC2A-02 qualification planning and project
  tracking updates (Commit B, this branch).
- Accepted implementation: `68b01fbb53eefca9c8ca2f83ff50641521020a2a`, tree
  `54f659d01937da29bc9a5f82566a8349af264664` on
  `implementation/gagamba-rc2a-01` (untouched).
- Preserved review: commit `8e41babfc96afa2e617a53b63e50903d54001194`, tree
  `fd30ef218b2ff5d1422d2e3abddd59c50e11aead`, blob
  `d329fcc49d289696bf1c4163f8f346c56115ba8c`, exact byte SHA-256
  `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0`
  (16,888 bytes), parent = the accepted commit, on
  `review/gagamba-rc2a-01-glm-independent` (unmerged).
- Decision branch: `decisions/gagamba-rc2a-01-owner-acceptance` (worktree
  `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-01-accept`), direct child of
  the accepted commit. Final decision commit and root tree are recorded in the
  external completion receipt and user report, avoiding self-reference.
- Host: Windows 11 / NT 10.0.26200.0, AMD64. No build, test, pack or native
  workload was run for this record; the cited 95-test reproduction is the
  preserved independent review's own recorded evidence.
- Qualification scope: documentation, Git identity and byte-integrity checks.
- Exclusions: implementation, production/test/project/package edits, package
  builds, native tests, Hufu edits, RC2A-02 execution, runtime qualification,
  push/merge/tag/publication.

## Decision evidence and scope

The [owner record](../../decisions/restricted-consumer-rc2a-01-owner-acceptance.md)
records **RC2A-01 ACCEPTED WITH FOLLOW-UP**: acceptance of the implementation
and its independently reviewed evidence only. Qualification is not complete;
RC2A-02 is planned by
[its specification](../../plans/restricted-consumer-contract-rc2a-02-qualification.md)
and not started. The review's findings are dispositioned by the owner without
rewriting the review; the review branch is referenced by exact identity and
never merged.

| Before RC2A-B | Recorded change | Established | Still pending |
|---|---|---|---|
| RC2A-01 implemented, review required | Verbatim review preservation + explicit owner acceptance at exact identities | Accepted implementation with follow-up | RC2A-02 qualification, RC2A-04 packaging |
| R2A-F01/F02/F04 follow-ups open | Dispositions recorded; folded into the RC2A-02 specification | Required follow-through is planned and traceable | Execution under separate authorization |
| R2A-F03 DLL byte non-identity | ACCEPT AS DISCLOSED | No binary-reproducibility claim in either direction | None commissioned |
| R1-F02/F03, AE4-B01, Hufu, publication | Unchanged | All remain open/unauthorized | Their own gates |

## Files and byte domains

- `owner-instruction.md`: readable verbatim copy of the owner's assignment;
  conversation-based approval authority, not a cryptographic signature.
- `source-binding.json`: exact branch/commit/tree/blob identities for the
  accepted implementation and the preserved review, review source-file path and
  SHA-256, decision-branch parent, unchanged src/tests/native trees, host and
  the owner-instruction copy binding.
- `validation.json`: the checks actually executed for this record, changed-path
  inventory and explicit non-execution statements. No invented validation runs.
- `document-sha256.json`: final LF-normalized byte hashes of every changed
  document/evidence file except this hash manifest itself, which is bound by
  the final commit/tree.

The independent review's full text is not duplicated in this evidence
directory; it is preserved once, on the review branch, at the identities above.

## Review provenance disclosure

The review document was produced by GLM in the independent review conversation.
After the session, the owner supplied the review text as a local file
(`C:\Users\Laszlos\source\repos\Gagamba\docs\reviews\restricted-consumer-rc2a-01-glm-independent.md`,
SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0`).
The custodian preserved those exact bytes — source and preserved-copy SHA-256
verified identical — on the review branch, adding a `-text` `.gitattributes`
entry for that one path (the repository's existing convention for byte-exact
preservation) so the blob stores the supplied CRLF bytes unmodified. The
canonical checkout's copy was left untouched; no file outside the two dedicated
branches was modified. The review document contains no owner commentary.
