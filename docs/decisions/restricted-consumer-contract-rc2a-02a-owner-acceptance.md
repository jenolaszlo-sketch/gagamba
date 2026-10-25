# RC2A-02A owner acceptance — Windows contract qualification

Date: 2026-10-25 (Asia/Manila).
Decision: **RC2A-02A ACCEPTED WITH QUALIFICATIONS**.
Scope: acceptance of the RC2A-02A Windows qualification implementation, executed
tests, canonical vectors, mutation coverage, boundary assertions and supporting
evidence identified by the independent GLM review.
This record does **not** accept full RC2A-02, does **not** authorize RC2A-02B,
and does **not** establish any native, package, cross-platform, Hufu or
publication readiness. No production, test or native source changed.

## 1. Decision

The project owner accepts the RC2A-02A Windows managed-code qualification with
the three P3 qualifications recorded in section 6. The acceptance is based on
the independent GLM review `RC2A-02A-GLM-Q1`, which was preserved unchanged on
its own branch and whose verdict is **SUPPORTED WITH QUALIFICATIONS** with no
confirmed production or qualification defect.

Status immediately after this record:

- **RC2A-02A: ACCEPTED WITH QUALIFICATIONS.**
- **RC2A-02B: NOT STARTED, REQUIRES SEPARATE AUTHORIZATION.**
- **Full RC2A-02: NOT YET ACCEPTED.**
- **Q2A-S / RC-D04: OPEN** (deferred qualification; see finding F04).

## 2. Accepted qualification and evidence binding

| Item | Exact identity |
|---|---|
| Qualification branch | `qualification/gagamba-rc2a-02a-windows` |
| Owner-acceptance baseline | `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e`, tree `6e94bee4c39801dfa256c158b300b066c0f2f217` |
| Tested source commit | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` |
| Committed qualification evidence | `c1a01761f8d58136a381ad87269635c6c14ad093`, tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19` |
| Evidence commit relationship | Documentation-only; direct parent is the tested source commit `71a28afe2623516fb4176e6a4ce7988a3d70df06` |
| Production source subtree | `b8c167e3d7340a562221591bdc2b6df7adb372d1` (= accepted RC2A-01 identity; unchanged) |
| Tests subtree | `5c3268286575cb55891fae9288627657f2d30ddf` |
| Native subtree | `85631d2f04512376621d3d09f51d67fd90f45335` (= accepted RC2A-01 identity; unchanged) |
| Accepted RC2A-01 object | `68b01fbb53eefca9c8ca2f83ff50641521020a2a`, tree `54f659d01937da29bc9a5f82566a8349af264664` |
| Required direct parent of this decision commit | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| Decision branch | `decisions/gagamba-rc2a-02a-owner-acceptance` |
| Decision worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-accept` |

The canonical repository state was re-verified before recording: the
qualification evidence commit resolves at the stated tree, its direct parent is
the tested source commit, the evidence diff is documentation-only, and
`src`/`tests`/`native` subtree identities are unchanged from the accepted
RC2A-01 implementation. This decision commit's own identity is recorded in the
completion report and external receipt, avoiding self-reference.

## 3. Independent review preservation

The independent GLM review is preserved **verbatim and unmodified** on its own
branch. It is referenced here by exact Git identity; it is **not merged,
cherry-picked, copied into this branch, amended or rewritten**, and this record
does not alter its wording or findings.

| Item | Exact identity |
|---|---|
| Review branch | `review/gagamba-rc2a-02a-glm-q1` |
| Review worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-glm-q1-review` |
| Review commit | `7b6344551c2d4b42832df39822eaf4b8a1c01495` |
| Review root tree | `6c75b4ddfd233f6e94f079b12c1a253198a4c7e2` |
| Review commit parent | `c1a01761f8d58136a381ad87269635c6c14ad093` (the qualification evidence commit) |
| Review report path | `docs/reviews/restricted-consumer-contract-rc2a-02a-glm-q1.md` |
| Review report Git blob | `475af1fb1cd9d849e68fbbeede071a8631d620df` |
| Review report byte SHA-256 (exact blob bytes) | `a69b4db573a67f8f93e038eb26223a891f5f60c96eadcdcf273f8127b62ca80d` |
| Review report byte length | 30,224 bytes |
| Supporting evidence directory | `docs/reviews/restricted-consumer-contract-rc2a-02a-glm-q1-evidence/` (tree `94d9211d9b11ca9c9ac9e94a5e699c54718f00e2`) |
| `probe-output.txt` | blob `41e1b7c0db3e8e706207aec7d5fce365d920fce5`, byte SHA-256 `164c40bb1bf61bf6da19468205395a9cf702fc98f1b82da1744f002f9d1e29e3` (21,632 bytes) |
| `reproduction-record.md` | blob `9995e066d03f0a1dae61e92ce7dd31d2b0dddef6`, byte SHA-256 `b16d596b593bfd010fefb170e50033ac0a1a97be4c5016b9ef80be22e6651bbf` (4,251 bytes) |
| Review-only probe source | `review/GlmQ1Probes/` on the review branch (csproj blob `cf44926cd213720d1582df0eb008f9ea5f305cee`, parser blob `aa661c3fc5fdd3ab75350bb22dbe32902ae0b45a`, program blob `d74e601e951e2f743e1ffe116827cfb943b66e1d`); not part of the solution, not required for this acceptance |
| Verdict | **SUPPORTED WITH QUALIFICATIONS** (review §M) |
| Confirmed production defects | none (review finding RC2A02A-GLM-F05) |
| Confirmed qualification defects | none; three P3 qualifications (F02–F04) |

Read the original review with:

```text
git show 7b6344551c2d4b42832df39822eaf4b8a1c01495:docs/reviews/restricted-consumer-contract-rc2a-02a-glm-q1.md
```

## 4. Acceptance basis

The decision is supported by the independently reproduced results recorded in
the review; this record executed no new qualification tests and claims none:

- 72/72 manifest fields covered; 279 deterministic field cases.
- 222,615 mutation inputs independently reproduced: 114,378 accepted, 108,237
  refused.
- 103/103 independent GLM probe checks passed.
- 21/21 committed hexadecimal vector digests verified (the two large vectors are
  digest-bound; see F03).
- Four independently implemented encoding/parsing paths agreed on the qualifying
  golden and minimal vectors, as described in the review (production codec,
  qualification wire template, preserved `ReferenceEncoder`, and the review's
  own independent parser/writer).
- 14 refusal categories reproduced with contract-derived codes and offsets.
- Windows public tests: 67 passed, 0 failed, 0 skipped.
- Windows required qualification set: 457 passed, 0 failed, 0 skipped, repeated
  twice with identical test-name/outcome multisets.
- Committed-plan filter: 473 passed, 0 failed, 41 skipped (the 41 are
  pre-existing Linux-native classes; none is an RC2A-02A case).
- Evidence hashes and Git source bindings independently verified (29/29
  `SHA256SUMS`, 18/18 source-binding file identities, 20/20 document digests,
  preserved RC2A-01 review blob).
- No confirmed production defects; no confirmed qualification defects.

## 5. Canonical mutation interpretation (Q2A02A-F01)

The owner accepts the independent review's conclusion that **Q2A02A-F01 is a
correct qualification-plan correction**, not a production defect:

- The plan's literal Section 3 B6 claim ("no mutation outside the two raw 32-byte
  digest fields is accepted") was disproved by execution and corrected by an
  append-only plan section; the correction is preserved.
- The byte-157 mutation to `0x3B` changes field segmentation and yields a
  **different but canonical manifest** (the re-segmented read-only destination
  absorbs store-section bytes and the writable-store count re-reads as zero).
- This is valid canonical representation behavior, **not** evidence of authority
  enforcement and **not** a security bypass or production defect.
- The decoder's final canonical re-encoding check remains **production-enforced**:
  every accepted mutation re-encodes byte-for-byte, and no accepted input is
  noncanonical.
- Canonical representation validation, manifest validity, authority
  authorization and sandbox execution enforcement are separate responsibilities.
  This acceptance establishes only the first two within the accepted passive
  contract.

The mutation results are not described as proof that all semantic
transformations are invalid; acceptance of a canonical mutation means only that
the bytes are a canonical description of the graph they parse to.

## 6. P3 finding dispositions

### RC2A02A-GLM-F02 — Native-filter wording (P3, TEST EXPECTATION ERROR — wording)

Source: review §K, `docs/reviews/restricted-consumer-contract-rc2a-02a-glm-q1.md`;
origin: `docs/evidence/restricted-consumer-contract-rc2a-02a-windows/findings.md`
Q2A02A-F02 (historical evidence; not altered).

- **Disposition: ACCEPT, DOCUMENTATION FOLLOW-UP.**
- Original wording (preserved): the finding states that the committed internal
  filter "also matches five pre-existing Linux-native classes outside RC2A-02A"
  and that "all 41 skips are those unrelated native classes".
- Clarification (this record): on Windows, two of the five filter-matched classes
  execute and pass — `CoordinatorProtocolQualificationTests` (11 tests) and
  `Wp05V3IndependentQualificationTests` (5 tests); the 41 pre-existing
  Linux-native skips come from the other three classes
  (`NonRootQualificationTests` 31, `OwnerDeathQualificationTests` 7,
  `Wp05ReleasePathIndependentQualificationTests` 3). The recorded test counts
  (473 passed, 0 failed, 41 skipped, 514 total) are accurate and are not
  rewritten.
- Follow-up: the corrected wording is recorded in this decision and in the
  current handoff/work-queue entries. Historical raw evidence and the
  independent review are not edited. Provenance of the difference is retained
  here (original wording quoted; clarification stated).

### RC2A02A-GLM-F03 — Digest-only large vectors (P3, EVIDENCE LIMITATION)

Source: review §K and §C.

- **Disposition: ACCEPT AS DISCLOSED.**
- Two of the 23 vectors — `Q2A-V-19-max-argenv-16384` and
  `Q2A-V-20-max-manifest-65536` — are bound by committed SHA-256 rather than
  embedded complete hexadecimal payloads, because of size
  (`exports/vector-manifest.json` marks them `hexOmittedForSize`).
- Reproducibility verification: materialization **is** reproducible from the
  committed evidence. The committed deterministic recipes plus the independent
  wire template rebuild all 23 vectors inside the qualification test suite, and
  that suite (which asserts byte equality against the committed vector digests,
  including the two large vectors) was reproduced twice by the independent
  review with 457/457/0 outcomes. The review additionally reproduced the
  minimal vector byte-exactly with its own writer. What is absent is a
  standalone committed byte export for the two large vectors, not the ability
  to reconstruct them.
- Limitation recorded: byte-level audit of those two vectors relies on the
  committed generator/recipe and reproduced execution, not on an embedded
  payload. No new requirement to embed oversized payloads is created; the
  accepted qualification plan does not require it.
- No production change.

### RC2A02A-GLM-F04 — Deferred Q2A-S / RC-D04 family (P3, EVIDENCE LIMITATION)

Source: review §K and §L; scope source:
`docs/plans/restricted-consumer-contract-rc2a-02-qualification.md` §3 case
families (`Q2A-S-*` public surface/misuse) and §4 rows for RC-D04 ("Five
fields, validation, content equality, no temporal/control members" →
`Q2A-F2 (reference rows), Q2A-S`) and R1-F04 ("No temporal fields;
documentation preserved" → `Q2A-S negative surface check`).

- **Disposition: ACCEPT, DEFERRED QUALIFICATION.**
- Q2A-S / RC-D04 remains **UNQUALIFIED** in RC2A-02A. The executed traceability
  matrix discloses the deferral as outside the RC2A-02A authorized families.
- Scope verified from the committed specification: Q2A-S is the public
  surface/misuse family; RC-D04 covers the five-field passive reference
  (validation, content equality, no temporal/control members); R1-F04 covers
  the negative temporal-surface check. The plan's dedicated execution sections
  (A vectors, B mutations, C boundaries, D portability — `Q2A-P`, E evidence,
  F review) do **not** assign Q2A-S to RC2A-02B; RC2A-02B is defined as
  portability. Q2A-S is therefore recorded as an **OPEN obligation of full
  RC2A-02 acceptance**, to be assigned to a qualification stage explicitly
  designated by the owner. It is **not** silently assumed to belong to
  RC2A-02B.
- It must be completed, or receive a separate explicit owner disposition,
  before full RC2A-02 acceptance.
- Current coverage status: the RC2A-01 passive-reference regressions
  (PreparationIdentityTests, 10 cases) and the implementation-id misuse case
  pass, and the independent review's AR-09 probe exercised reference
  validation/equality with no defect found. These observations do not complete
  Q2A-S and are not a substitute for it.

## 7. Qualification acceptance boundaries

This acceptance does **not** establish:

- Linux qualification.
- macOS qualification.
- Native sandbox confinement.
- Binary reproducibility.
- Package compatibility.
- Clean-consumer compatibility.
- Mixed Hufu dependency-graph compatibility.
- Hufu AE4-B01 closure.
- Hufu resumption.
- Package publication.
- Full RC2A-02 acceptance.

These distinctions are maintained in the updated roadmap, qualification plan,
work queue and handoff. The three P3 items do not block RC2A-02A acceptance;
the deferred Q2A-S / RC-D04 requirement remains visible as an open obligation.

## 8. Deferred obligations (unchanged unless an existing decision says otherwise)

- Q2A-S / RC-D04 qualification — **OPEN** (F04; stage to be designated).
- RC2A-02B portability qualification — **NOT STARTED, REQUIRES SEPARATE
  AUTHORIZATION**.
- RC2A-03/04 component, package, clean-consumer and mixed-graph gates —
  pending.
- R1-F02 and R1-F03 — OPEN, assigned to RC-2B.
- R1-F06 — HUFU-R recheck.
- AE4-B01 — OPEN.
- Hufu resumption — not authorized.
- Package publication — not authorized.
- Native sandbox qualification — not performed.
- C1-R, C3-I, C4-I, M1-G3, M1-G4G5, RC2B-01 through RC2B-03, RC2B-R and
  HUFU-D4B — retain their existing recorded status in the implementation plan.

No closure evidence is invented for any obligation.

## 9. Evidence

Owner-acceptance evidence (this branch, documentation-only):
[`docs/evidence/restricted-consumer-contract-rc2a-02a-owner-acceptance/`](../evidence/restricted-consumer-contract-rc2a-02a-owner-acceptance/README.md),
including the source binding, owner instruction, validation record and
LF-normalized document digest manifest. The RC2A-02A qualification evidence
remains authoritative at commit `c1a01761`; this record references it rather than
duplicating it.

## 10. Next action

**RC2A-02B may be commissioned only by separate owner authorization. Full
RC2A-02 remains unaccepted, and Q2A-S / RC-D04 remains open.** The RC2A-02
qualification plan now carries the RC2A-02B entry-condition checklist for that
separate authorization. No RC2A-02B work, merge, push or publication is
authorized by this record.
