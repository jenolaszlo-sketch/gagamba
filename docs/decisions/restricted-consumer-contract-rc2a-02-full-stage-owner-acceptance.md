# RC2A-02 full-stage owner acceptance — restricted consumer contract

Date: 2026-10-26 (Asia/Manila).
Decision: **RC2A-02 ACCEPTED WITH QUALIFICATIONS.**
Decision authority: Owner.
Decision basis: the accepted RC2A-02A, RC2A-02B and RC2A-02C qualifications
with their owner dispositions, and the independent full-stage
acceptance-gap assessment **RC2A-02-FSA-01** (verdict **READY FOR OWNER
ACCEPTANCE**).
Decision scope: the full RC2A-02 restricted consumer contract stage — the
passive managed-code value/codec implementation qualification defined by the
authoritative RC2A-02 qualification plan — subject to the recorded
qualifications in §6.

This decision closes RC2A-02 within its defined contract. It does not
authorize RC2A-03, RC2A-04, RC-2B, packaging, native qualification, Hufu
work, publication, or any implementation activity (§8, §10).

## 1. Purpose and scope of acceptance

The owner accepts the full RC2A-02 stage: the tests-first qualification of
the accepted RC2A-01 passive value/codec implementation exactly as it stands
— canonical consumer vectors with independent verification, exhaustive
72-field mutation coverage, boundary and misuse assertions, honest
cross-platform run definitions, evidence hygiene, and the passive
preparation-reference public-surface and R1-F04 documentation slice — as
executed across the three authorized substages and verified by the full-stage
assessment.

Acceptance establishes **qualification of the passive value/codec
implementation only**, per the stage contract's own completion note. It
does not complete RC2A-04 packaging/clean-consumer qualification, does not
qualify any native or production capability, does not close AE4-B01, and
does not authorize Hufu resumption or publication.

## 2. Exact Git and evidence bindings

| Role | Branch | Commit | Tree | Parent |
|---|---|---|---|---|
| RC2A-01 accepted object | `decisions/gagamba-rc2a-01-owner-acceptance` | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` | `54f659d01937da29bc9a5f82566a8349af264664` | — |
| RC2A-02A qualification evidence | `qualification/gagamba-rc2a-02a-windows` | `c1a01761f8d58136a381ad87269635c6c14ad093` | `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19` | `71a28afe2623516fb4176e6a4ce7988a3d70df06` |
| RC2A-02A owner acceptance | `decisions/gagamba-rc2a-02a-owner-acceptance` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02A independent review (Q1) | `review/gagamba-rc2a-02a-glm-q1` | `7b6344551c2d4b42832df39822eaf4b8a1c01495` | `6c75b4ddfd233f6e94f079b12c1a253198a4c7e2` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B qualification | `qualification/gagamba-rc2a-02b` | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | `5b4292d0fefc576664df61692dfad5cef6362879` |
| RC2A-02B R1 evidence correction | `remediation/gagamba-rc2a-02b-r1` | `1cd22b9b109ddba4673b11487bceea62d2e92117` | `2f2bae947dee368ae073dd3fe7877380ca8854ff` | `12d914d319b1df3eedafc5439bc547dacb578651` |
| RC2A-02B Q1 / Q2 reviews | `review/gagamba-rc2a-02b-q1` / `review/gagamba-rc2a-02b-q2` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` / `60de1f2d34a7df337a426501cb3954fdd3216a6b` | preserved unmerged | — |
| RC2A-02B owner acceptance | `decisions/gagamba-rc2a-02b-owner-acceptance` | `dc02951902f11031f8b0d225d2afa2cd537296a3` | `119470918c3b3a3c3c1af150e554026b230a2677` | `1cd22b9b109ddba4673b11487bceea62d2e92117` |
| Q2A-S / RC-D04 stage assignment | `decisions/gagamba-q2as-rcd04-disposition` | `91612b9a42827ffdd76891819670623fa43daeed` | `5f9ec8a9d01627ee3ed0a245ca54642ae1413ea4` | `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| RC2A-02C qualification evidence | `qualification/gagamba-rc2a-02c-q2as-rcd04` | `62b75f5866d0c339816712baeb04baa386b06f85` | `66f996e1cf973e48487a073b07f1e78d9139c911` | `c4837b0b434fc75ebc3e08d278bdc09c3965a181` |
| RC2A-02C independent review (+ re-verification) | `review/gagamba-rc2a-02c-q2` | `9534581c34d55c1219e90ddcd290900583098388` / `f8b692e04941462ea592d9653960191cafb937cc` | report `docs/reviews/rc2a-02c-q2-independent-verification.md` blob `e85924573a02bfebf1e2cb3aa97bc15ba151d0e2` | `62b75f5866d0c339816712baeb04baa386b06f85` |
| RC2A-02C owner acceptance | `decisions/gagamba-rc2a-02c-owner-acceptance` | `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` | `b3efa338fe5625f8f86fa49fb8db1f6fd62e1bda` | `62b75f5866d0c339816712baeb04baa386b06f85` |
| Full-stage assessment (RC2A-02-FSA-01) | `assessment/gagamba-rc2a-02-full-stage` | `058a150130c5f112c7e9b8189bba8842cf0cec52` | `b2211a26c838f98f994602e5c56ea8bb95b2c1d8` | `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` |
| This decision branch | `decisions/gagamba-rc2a-02-full-stage-acceptance` | recorded in the completion report (self-reference avoided) | — | `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` |

The full-stage assessment is read at its stated commit `058a150…` (report
blob `5b4f559d71e8bb6b871a28f73be694b742887bdd`); it is **not** merged,
cherry-picked, or copied into this decision branch. All review branches are
preserved unchanged on their own refs. At every verified stage commit the
production `src` tree is `b8c167e3d7340a562221591bdc2b6df7adb372d1`, the
`native` tree is `85631d2f04512376621d3d09f51d67fd90f45335`, and the frozen
API contract blob is `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` — none
changed during RC2A-02.

## 3. Authoritative requirement sources

- RC-1 design at `d609c29d3048bc0fd91583068cb013fb906fdf90` (blob
  `4b300b01397e7f68d978fadbc36164dda17c8721`): §RC-D02 field table,
  §RC-D03 canonical grammar, §RC-D04 preparation identity, R1-F01/F04/F05.
- RC1-A owner acceptance (`9a705335701f0c1923bd9867a2612b8a93afa8cb`):
  R2A-F01/F02/F04 carried into RC2A-02; R2A-F03 accepted as disclosed.
- The authoritative RC2A-02 stage contract:
  [qualification plan](../plans/restricted-consumer-contract-rc2a-02-qualification.md)
  — entry criteria §2, case families §3, traceability matrix §4, **exit
  criteria §5**, exclusions/stop conditions §6, and the authorized plan
  adaptation in §8 (canonical-rigidity correction, owner-accepted as
  Q2A02A-F01).
- Implementation-plan work-order tranches (RC-T01..T23) for scope
  classification of the T-rows.
- The frozen public API contract
  (`docs/design/restricted-consumer-contract-rc2a-01-api.md`, blob
  `2d6b9d12…`).

## 4. Substage disposition inventory

| Substage | Authorized scope | Qualification result | Independent review | Owner disposition | Status |
|---|---|---|---|---|---|
| RC2A-02A | Q2A-V/F/B/E on Windows | public 67/0/0; required 457/0/0 twice; 72/72 fields; 279 mutation cases; 222,615-input flip sweep; 23 vectors; all boundaries | Q1 SUPPORTED WITH QUALIFICATIONS; 103/103 probes; no confirmed defect | `e5fff9e8…` ACCEPTED WITH QUALIFICATIONS | **ACCEPTED WITH QUALIFICATIONS** |
| RC2A-02B | Q2A-P Windows + WSL2 | 67/0/0 and 457/0/0 on each platform; identical required reruns; matching cross-platform multisets; native Linux/macOS recorded not run | Q1 (F-01/F-02) → R1 correction → Q2 VERIFIED WITH QUALIFICATIONS | `dc029519…` ACCEPTED WITH QUALIFICATIONS | **ACCEPTED WITH QUALIFICATIONS** |
| RC2A-02C | Q2A-S + RC-D04/R1-F04 passive-reference slice on Windows | 13/13 matrix rows; focused build 0/0 and 3/3 tests; public-surface inventory; hash-bound evidence | Q2 review SUPPORTED WITH QUALIFICATIONS, 13/13; re-verification reproduced build/tests and recomputed all manifests | `a36cb68…` ACCEPTED WITH QUALIFICATIONS | **ACCEPTED WITH QUALIFICATIONS** |

The full-stage assessment reconciled all three dispositions against the
stage contract, independently recomputed the evidence manifests (02A 29/29,
02B 43/43, 02C 40/40 package digests; 02C 16/16 document bindings and
14/14 links), verified the complete ancestry chain, and found no blocking
defect, missing evidence, or unresolved P0–P2 finding.

## 5. Exit-criteria acceptance table (stage contract §5)

| # | Criterion | Final acceptance | Qualification carried |
|---|---|---|---|
| E1 | Every Q2A case executed on the primary platform against the exact accepted source; passes; no unjustified skips | **ACCEPTED** | 41 broad-filter skips disclosed as pre-existing out-of-scope Linux-native classes (F02 clarification) |
| E2 | Vector set byte-for-byte reproducible and independently verified | **ACCEPTED WITH QUALIFICATION (F03)** | two large vectors digest-bound; materialization reproduced twice via committed recipes; no standalone byte export required by the plan |
| E3 | 72-field mutation matrix with required classes; exact refusal code/offset | **ACCEPTED** | — |
| E4 | Both byte boundaries, argv/env cases, null-manifest and wrong-length digest (incl. combined), truncation sweep, flip sweep | **ACCEPTED** | flip-sweep interpretation per the owner-accepted §8 canonical-rigidity correction |
| E5 | Defensive re-encode branch assessment recorded | **ACCEPTED** | — |
| E6 | Windows completed; additional platforms completed or recorded not run with reasons; no claim beyond what ran | **ACCEPTED WITH QUALIFICATION (not-run platforms)** | WSL2 evidence is real and separate but labeled `wsl2`, never native Linux; native Linux and macOS NOT RUN (no hosts), as the criterion and §3 P2 expressly permit |
| E7 | Evidence hygiene artifacts (E1–E5) committed | **ACCEPTED** | 02B evidence defects corrected in R1 and re-verified by Q2 |
| E8 | Independent reviewer re-executed and accepted the evidence; disposition preserved | **ACCEPTED** | Q1 (02A), Q1+Q2 (02B), Q2 (02C), all preserved unmerged |

No mandatory criterion lacks evidence; no mandatory implementation obligation
is incomplete; no blocking contract ambiguity remains. The E2 and E6
qualifications are compatible with the authoritative stage contract: E2's
limitation was expressly accepted (RC2A-02A acceptance §6, F03 — the plan
does not require embedded payloads), and E6's not-run recording is expressly
permitted by exit criterion 6 and §3 P2.

## 6. Carried-forward qualifications

1. **E2 / F03 — vector materialization (RC2A-02A acceptance §6).** Two of
   the 23 vectors (`Q2A-V-19-max-argenv-16384`,
   `Q2A-V-20-max-manifest-65536`) are bound by committed SHA-256 rather than
   embedded hex payloads. Materialization is reproducible from the committed
   deterministic recipes and was reproduced twice inside the qualification
   suite by the independent review. **ACCEPT AS DISCLOSED** — no standalone
   byte export is created by this decision, and no stronger reproducibility
   claim (binary, package, or byte-level audit of those two payloads) is
   made.
2. **E6 — native platform execution (RC2A-02B acceptance §5).** Windows
   native qualification evidence is complete and was re-run in RC2A-02B.
   WSL2 Ubuntu execution evidence exists, is real and separately preserved,
   and is always labeled `wsl2`; **it does not constitute native Linux
   qualification**. Native Linux: **NOT RUN** (no native host). Native
   macOS: **NOT RUN** (no host). Neither status is a pass or a failure, per
   §3 P2, and no cross-platform success is implied beyond the executed
   environments.
3. **Passive preparation reference (RC2A-02C acceptance §5).**
   `RestrictedPreparationReference` remains a passive managed-code
   correlation value. Its accepted behavior does not establish provider
   provenance, live authorization, temporal or deadline guarantees, release
   authority, retained-state authority, native confinement, packaging
   correctness, Hufu integration, or end-to-end execution correctness. The
   RC-2B live-operation halves of RC-D04 remain outside RC2A-02.
4. **R2A-F03 standing rule (RC1-A acceptance §4).** Test-outcome
   reproducibility is the qualified claim; local DLL byte hashes are
   environment-bound observations. No binary-reproducibility,
   package-identity, or deployment-qualification claim is accepted in either
   direction.
5. **RC2A-02B Q1 authorization-provenance/audit-trail note (RC2A-02B
   acceptance §4).** The separate-authorization condition for RC2A-02B was
   satisfied by an owner assignment recorded in the qualification matrix
   whose instruction text is not independently authenticated from Git.
   **PRESERVED AS DISCLOSED.**
6. **Q2C-Q2-F01 (RC2A-02C acceptance §4).** The hash-bound
   `validation.md` understated the package checksum count as 39/39; the
   committed `SHA256SUMS` contains 40 entries, all independently verified.
   **ACCEPTED AS CORRECTED BY ERRATUM** — the erratum in the RC2A-02C
   acceptance §4 remains the authoritative correction; the immutable
   evidence snapshot and both integrity manifests are unchanged, and no
   historical evidence is retrospectively edited.
7. **Q2A02A-F01 (RC2A-02A acceptance §5).** The plan's original §3 B6
   single-byte-mutation prediction was disproved and corrected (canonical
   rigidity + immutable-region containment). Accepted as a correct plan
   correction; not a production defect and not a security bypass.

## 7. Nonblocking historical findings

All findings across the stage are dispositioned and closed; none is blocking.

| Finding | Disposition |
|---|---|
| Q2A02A-F01 | accepted plan correction (above) |
| RC2A02A-GLM-F02 (P3, filter wording) | ACCEPT + documentation follow-up; clarification recorded; historical wording retained as provenance |
| RC2A02A-GLM-F03 (P3) | ACCEPT AS DISCLOSED (carried as qualification 1) |
| RC2A02A-GLM-F04 (P3, Q2A-S deferral) | ACCEPT, DEFERRED QUALIFICATION → **closed** by RC2A-02C |
| RC2A-02B Q1 F-01 (P2, stale artifact digests) | corrected in R1; verified RESOLVED by Q2 |
| RC2A-02B Q1 F-02 (P3, broad-filter wording) | corrected in R1; verified RESOLVED by Q2 |
| 02B Q1 audit-trail note | PRESERVED AS DISCLOSED (carried as qualification 5) |
| Q2C-Q2-F01 (P3, checksum count) | ACCEPTED AS CORRECTED BY ERRATUM (carried as qualification 6) |

No P0 or P1 finding exists in the RC2A-02 record; none was omitted or
silently waived by this decision.

## 8. Cross-stage acceptance statement

Separately qualified substages are sufficient for this stage's original
contract because:

- The authoritative stage contract (qualification plan §5) requires each
  executed matrix case to run on the primary platform against the exact
  accepted source — satisfied by the union of the three executed, bound
  matrices — and does not require a single consolidated cross-substage run.
- **A consolidated cross-substage run was not performed and is not a
  mandatory RC2A-02 exit criterion.** It may remain a future optional
  qualification activity; its absence is not characterized as a defect or
  missing acceptance evidence, and no end-to-end security guarantee is
  inferred from this boundary.
- The existing accepted qualification matrices jointly cover every mandatory
  stage requirement (§4 traceability rows; assessment §5).
- The frozen API blob, the production `src`/`native` trees, and the RC-1
  design blob are identical across all accepted evidence; the full-stage
  assessment found no cross-stage compatibility defect and verified the
  complete disposition ancestry.

## 9. Explicit exclusions and non-authorizations

This acceptance reconciles the stage contract's exclusions (plan §1, §5
completion note, §6) and preserves them. The following remain **unqualified,
open, or unauthorized** by RC2A-02 acceptance:

| Item | Status after this decision |
|---|---|
| RC2A-03 component qualification | separate pending gate; **not authorized** |
| RC2A-04 packaging, clean-consumer, distribution (RC-T12/T17) | separate pending stage; **not authorized** |
| RC-2B live operations (R1-F02, R1-F03) | OPEN; assigned to RC-2B by contract; **not closed, not implemented** |
| Provider-origin preparation, execution deadlines and timing guarantees beyond the accepted contract, retained-state and release behavior | RC-2B obligations; **not established** by the passive-reference acceptance |
| AE4-B01 (Hufu consumer dependency) | OPEN; unchanged by this decision |
| Hufu integration, Hufu resumption, additional authority effects | dormant; **not authorized** |
| Native Linux / native macOS qualification | **NOT PERFORMED** for this stage; WSL2 evidence never constitutes native Linux |
| End-to-end restricted execution | **not qualified** by RC2A-02 |
| Public package release or publication | **not authorized** |
| Native sandbox/confinement qualification | separate gates; **not established** |
| Any security claim beyond the accepted evidence | none is made; canonical-representation validation remains distinct from authority or sandbox enforcement |

Distinguishing note: the *implementation* of the passive value/codec was
already accepted under RC2A-01 (a separate, earlier milestone). RC2A-02
acceptance establishes that implementation's **qualification**; it does not
re-open, alter, or claim credit for any other milestone's completed work, and
it reopens no unrelated work.

## 10. Evidence preservation and next-stage authorization boundary

**Evidence preservation.** This decision modifies no file under `docs/evidence/`,
`src/`, `tests/`, or `native/`; no qualification evidence package, checksum
manifest, review report, or previously accepted owner decision is rewritten.
All corrections referenced by this decision (R1, the F02 clarification, the
F01 erratum) exist in their own commits with original wording preserved as
provenance. The evidence identities in §2 were verified to resolve at their
stated commits and trees.

**Next-stage authorization boundary.** This decision closes RC2A-02 only.
RC2A-03, RC2A-04, RC-2B, native qualification, packaging, publication, Hufu
work, and any other subsequent engineering require separate, explicit owner
authorization. No such authorization is granted or implied here.
