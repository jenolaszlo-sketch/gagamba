# RC2A-02A owner-acceptance evidence

- Date: 2026-10-25 (Asia/Manila).
- Slice: independent GLM review preservation (already committed on its own
  branch), owner acceptance of RC2A-02A Windows contract qualification, P3
  finding dispositions, and project-tracking updates (this branch,
  documentation-only).
- Decision: **RC2A-02A ACCEPTED WITH QUALIFICATIONS**.
- Decision branch: `decisions/gagamba-rc2a-02a-owner-acceptance`, worktree
  `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-accept`, created directly
  from the qualification evidence commit
  `c1a01761f8d58136a381ad87269635c6c14ad093` (tree
  `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`). The decision commit and tree are
  recorded in the completion report and external receipt, avoiding
  self-reference.
- Accepted qualification: tested source
  `71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
  `70b932148cbfa59b0e424229879eebd36df2d80c`); production source
  `b8c167e3d7340a562221591bdc2b6df7adb372d1` and native
  `85631d2f04512376621d3d09f51d67fd90f45335` unchanged from accepted RC2A-01;
  tests `5c3268286575cb55891fae9288627657f2d30ddf`.
- Preserved review: branch `review/gagamba-rc2a-02a-glm-q1`, commit
  `7b6344551c2d4b42832df39822eaf4b8a1c01495`, tree
  `6c75b4ddfd233f6e94f079b12c1a253198a4c7e2`, parent
  `c1a01761f8d58136a381ad87269635c6c14ad093`; report blob
  `475af1fb1cd9d849e68fbbeede071a8631d620df`, byte SHA-256
  `a69b4db573a67f8f93e038eb26223a891f5f60c96eadcdcf273f8127b62ca80d`
  (30,224 bytes); verdict **SUPPORTED WITH QUALIFICATIONS**; no confirmed
  production or qualification defect. Unmerged and not copied into this branch.
- Host: Windows 11 / NT 10.0.26200.0, AMD64. **No build, test, pack or native
  workload was run for this record.** The reproduced-result counts cited here are
  the independent review's own recorded execution (its report and preserved
  probe output), not new execution by this acceptance record.
- Scope: documentation, Git-identity and byte-integrity checks only.
- Exclusions: production/test/native edits; RC2A-02B execution; package builds;
  native qualification; Hufu edits; merge/push/tag/publication.

## What this acceptance establishes

The [owner decision](../../decisions/restricted-consumer-contract-rc2a-02a-owner-acceptance.md)
records **RC2A-02A ACCEPTED WITH QUALIFICATIONS**: the Windows managed-code
consumer-contract qualification identified by the independent review is
accepted, with three P3 qualifications (F02 wording, F03 digest-only large
vectors, F04 deferred Q2A-S / RC-D04). The mutation-sweep correction
(Q2A02A-F01) is accepted as a correct plan correction: accepted single-byte
mutations are canonical descriptions of the graphs they parse to; canonical
representation is production-enforced by the decoder's final re-encode check and
is distinct from authority or sandbox enforcement.

| Before this record | Recorded change | Still open |
|---|---|---|
| RC2A-02A executed, independently reviewed (SUPPORTED WITH QUALIFICATIONS), awaiting owner disposition | **RC2A-02A ACCEPTED WITH QUALIFICATIONS**; F02/F03/F04 dispositioned | Q2A-S / RC-D04 qualification; full RC2A-02 acceptance |
| RC2A-02B unspecified in status | **RC2A-02B NOT STARTED; requires separate authorization** | RC2A-02B execution and its independent review |
| F02 wording imprecision noted by review | **ACCEPT, DOCUMENTATION FOLLOW-UP**; clarified in current tracking/decision docs with provenance | no blocking action |
| F03 digest-only large vectors noted by review | **ACCEPT AS DISCLOSED**; materialization reproducible via committed recipes + reproduced suite; standalone byte export absent and recorded | no new requirement; optional future export |
| F04 Q2A-S / RC-D04 deferred | **ACCEPT, DEFERRED QUALIFICATION**; recorded OPEN and assigned to an owner-designated stage (not silently RC2A-02B) | completion or explicit disposition before full RC2A-02 acceptance |

## P3 dispositions (summary)

| Finding | Severity | Disposition |
|---|---|---|
| `RC2A02A-GLM-F02` — filter-class wording implies all five matched native classes skip; two execute and pass; 41 skips come from three; counts accurate | P3 | **ACCEPT, DOCUMENTATION FOLLOW-UP**; wording clarified in current docs, historical evidence and the review untouched |
| `RC2A02A-GLM-F03` — 2 of 23 vectors digest-bound only (no embedded hex) | P3 | **ACCEPT AS DISCLOSED**; reproducibility demonstrated via committed recipes and the reproduced 457-case suite; byte-export limitation recorded |
| `RC2A02A-GLM-F04` — Q2A-S / RC-D04 family deferred | P3 | **ACCEPT, DEFERRED QUALIFICATION**; OPEN, stage assignment by owner, required before full RC2A-02 acceptance |

## Acceptance boundaries

This acceptance does not establish Linux or macOS qualification, native sandbox
confinement, binary reproducibility, package or clean-consumer compatibility,
mixed Hufu dependency-graph compatibility, AE4-B01 closure, Hufu resumption, or
publication. **Full RC2A-02 is not accepted.** Deferred obligations are listed in
the owner decision §8 and the updated implementation plan/work queue.

## Evidence integrity

- `source-binding.json` binds the accepted qualification, the preserved review
  and this decision's parentage to exact Git identities.
- `validation.json` records the checks actually executed for this record
  (documentation and Git/byte integrity only).
- `document-sha256.json` binds the changed documents by LF-normalized SHA-256;
  it excludes itself and is bound by the final commit/tree.
- The independent review and RC2A-02A qualification evidence remain authoritative
  at their own commits and are referenced, not duplicated, here.
