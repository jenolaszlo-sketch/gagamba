# RC2A-02B owner acceptance — Windows and WSL2 portability

Date: 2026-10-25 (Asia/Manila).
Decision: **RC2A-02B ACCEPTED WITH QUALIFICATIONS.**

This decision accepts the bounded Windows 11 native and WSL2 Ubuntu managed-code
value/codec portability qualification. It does not accept full RC2A-02, qualify
native Linux or macOS, establish deployment or sandbox behavior, close Q2A-S /
RC-D04, or authorize later milestones.

## 1. Owner decision

The owner accepts RC2A-02B based on the committed qualification evidence, the
Q1 independent review, the R1 evidence remediation, and Q2 independent
verification. Q2’s verdict is **VERIFIED WITH QUALIFICATIONS**. Its independent
checks found both Q1 findings resolved, evidence integrity intact, and no new
material defect. This is the owner’s acceptance decision; it is not a new
independent review or qualification run.

## 2. Accepted scope and results

- Windows 11 native managed-code value/codec qualification.
- WSL2 Ubuntu managed-code value/codec qualification. WSL2 is not native Linux.
- Public suite on each platform: **67 passed / 0 failed / 0 skipped**.
- Precise required suite on each platform: **457 passed / 0 failed / 0 skipped**;
  each required-suite rerun produced an identical result.
- Windows and WSL2 public and required test-name/outcome multisets match.
- Evidence hashes and source/tree provenance were independently validated by Q2.
- The precise `Tests.Qualification.` filter remains the authoritative
  qualification selection. The Windows broad-filter result (473 passed / 0
  failed / 41 skipped) is provenance evidence only.

No production, test, native, or raw qualification artifact change was required.
R1 changed evidence metadata and documentation to correct the two findings;
Q2 independently confirmed that raw runs and outcomes were preserved.

## 3. Baselines and Git identities

| Role | Branch | Commit | Tree | Parent |
|---|---|---|---|---|
| RC2A-02A owner-accepted start | `decisions/gagamba-rc2a-02a-owner-acceptance` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B qualification | `qualification/gagamba-rc2a-02b` | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | `5b4292d0fefc576664df61692dfad5cef6362879` |
| Q1 independent review, preserved separately | `review/gagamba-rc2a-02b-q1` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | `8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| R1 correction | `remediation/gagamba-rc2a-02b-r1` | `12d914d319b1df3eedafc5439bc547dacb578651` | `35cc347f8e436bd1fec9997cb50ede81e561767c` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| R1 final evidence tip | `remediation/gagamba-rc2a-02b-r1` | `1cd22b9b109ddba4673b11487bceea62d2e92117` | `2f2bae947dee368ae073dd3fe7877380ca8854ff` | `12d914d319b1df3eedafc5439bc547dacb578651` |
| Q2 independent verification, preserved separately | `review/gagamba-rc2a-02b-q2` | `60de1f2d34a7df337a426501cb3954fdd3216a6b` | `c556b0ad7bad69e5b7ca0815f9a1a17d36e51407` | `1cd22b9b109ddba4673b11487bceea62d2e92117` |

The owner-decision branch is
`decisions/gagamba-rc2a-02b-owner-acceptance`, created in an isolated worktree
from the exact R1 final tip `1cd22b9b109ddba4673b11487bceea62d2e92117`. The
decision commit is a documentation-only direct child of that tip; its identity
is reported in the completion record to avoid self-reference.

The qualification evidence binds execution to `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`
(tree `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e`). Its source, test and native
subtrees remain `b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`5c3268286575cb55891fae9288627657f2d30ddf`, and
`85631d2f04512376621d3d09f51d67fd90f45335`. Q1 and Q2 remain unchanged on
their own branches and are neither merged nor copied into this decision branch.

## 4. Independent review and finding dispositions

Q1 at `5bc396c2e5619906ae202afe0a4244dfd6024d9e` identified two material
evidence defects. R1 corrected them, and Q2 at
`60de1f2d34a7df337a426501cb3954fdd3216a6b` independently verified the results.

| Q1 finding | R1 correction | Q2 disposition |
|---|---|---|
| R1-01: 14 stale Windows artifact hashes in `results.json` | Recomputed the 14 hashes from committed artifact bytes; checked remaining references and dependent manifests | **RESOLVED** — independently verified all 29 artifact references, 43 `SHA256SUMS` entries, and nine document bindings |
| R1-02: README inaccurately said all five broad-filter-only classes were Linux-native skips | Corrected README and related evidence wording using raw Windows TRX class outcomes | **RESOLVED** — beyond the precise qualification set, the broad filter adds 2 managed-code classes / 16 passes and 3 Linux-native classes / 41 skips; aggregate 473/0/41. The precise filter remains authoritative |

The Q1 authorization-provenance/audit-trail note remains **PRESERVED AS
DISCLOSED**. Q1 and Q2 reports are immutable independent-review records and
their findings have not been rewritten.

## 5. Qualifications and exclusions

- Native Linux: **NOT RUN**. macOS: **NOT RUN**. Under the qualification plan,
  host unavailability is disclosed as not run; provisioning those hosts is not
  a prerequisite to this bounded acceptance.
- WSL2 does not constitute native Linux qualification.
- Native confinement and sandbox behavior are not established.
- Package and clean-consumer compatibility are not established.
- Mixed dependency graph compatibility and binary reproducibility are not
  established.
- Hufu integration is not established; AE4-B01 remains open and Hufu
  resumption is not authorized.
- **Q2A-S / RC-D04 remains OPEN.** The plan requires completion or a separate
  explicit owner disposition before full RC2A-02 acceptance; it is not silently
  assigned to RC2A-02B.
- RC2A-03 and RC2A-04 remain separate pending stages. RC-2B R1-F02/F03,
  R1-F06 HUFU-R recheck, AE4-B01, native sandbox qualification and publication
  retain their previously recorded status and authorization boundaries.

These are qualifications and deferred obligations, not defects in the
remediation verified by Q2.

## 6. Full-stage boundary and next action

**Full RC2A-02 is NOT ACCEPTED.** The remaining explicit full-stage obligation
is Q2A-S / RC-D04: complete the qualification at an owner-designated stage or
record a separate explicit owner disposition, then assess full RC2A-02 against
the plan’s exit criteria. RC2A-03/04 and their package, consumer, and later
integration gates remain separate and pending. No later milestone is authorized
by this decision.

The recommended next action is to retain this decision as the bounded RC2A-02B
owner acceptance and assign or separately disposition Q2A-S / RC-D04 before
considering full RC2A-02 acceptance. No additional broad review or rerun of the
completed qualification suites is indicated by Q2.
