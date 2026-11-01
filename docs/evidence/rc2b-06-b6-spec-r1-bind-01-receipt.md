# RC2B-06 B6-SPEC-R1-BIND-01 — Source-Binding Provenance Receipt

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-R1-BIND-01 (source-binding correction only).

**Status:** Documentation-only. No technical design, qualification plan, source,
test, native, tooling or workflow change. No implementation, test execution,
native run, package build, installation or production activation is claimed.

This receipt exists because the B6-SPEC-R1 remediation content was complete but
uncommitted, and the R1 pointer documents had mislabelled the Q1 review commit as
the "remediation commit". A commit cannot cite its own identity, so the R1
remediation content was committed first and its exact SHA/tree are recorded here.

---

## 1. Exact Git identity

| Role | Commit SHA | Tree SHA |
|---|---|---|
| Original specification | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` |
| Q1 independent review / R1 parent baseline | `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` | `630b803d57e27a0044686595e108868072aa20ea` |
| B6-SPEC-R1 remediation commit | `77ee4414df37b9f88979bde02742756538ba45af` | `62736d355e2650b5f40cdc3d904ed49466cbf025` |
| B6-SPEC-R1-BIND-01 provenance receipt (this commit) | not embedded (a commit cannot cite its own SHA); it is the child of the R1 remediation commit | — |

- The original specification and the Q1 review are **not** the remediation.
  `08ebcb5…` is the Q1 independent review commit and the R1 starting baseline.
- The R1 remediation commit `77ee441…` is a distinct commit whose sole parent is
  `08ebcb5…`. It introduces the six authorized B6-SPEC-R1 documents.
- This provenance receipt is a separate, later, documentation-only commit whose
  sole parent is the R1 remediation commit. It changes no technical content.

## 2. R1 remediation commit inventory

Commit `77ee4414df37b9f88979bde02742756538ba45af` (tree
`62736d355e2650b5f40cdc3d904ed49466cbf025`), parent
`08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df`, contains exactly:

- `docs/design/rc2b-06-b6-installed-artifact-trust.md` (amended)
- `docs/plans/rc2b-06-b6-qualification.md` (amended)
- `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` (new)
- `docs/PROJECT-STATUS.md` (pointer)
- `docs/handoff.md` (pointer)
- `docs/work-queue.md` (pointer)

No production, native, test, tooling or workflow path changed.

## 3. Five-finding remediation status (preserved)

| Finding | Status |
|---|---|
| B6-Q1-F01 | Addressed (T25 split into T25-A/T25-B) |
| B6-Q1-F02 | Addressed (production/test separation; T24) |
| B6-Q1-F04 | Addressed (native dependency closure; T20a) |
| B6-Q1-F07 | Addressed (installation-identity handshake wire spec; T20b) |
| B6-Q1-F13 | Addressed (qualification-record association; T20c) |

Bounded findings F03, F05, F06, F08–F11 and F14 corrected; F12 preserved.
HD-1–HD-4 remain open owner decisions. The remediation content is unchanged by
this receipt.

## 4. Starting identity for the focused Q2 review

- Review baseline (tip after this receipt): the B6-SPEC-R1-BIND-01 provenance
  receipt commit (child of the R1 remediation commit).
- R1 remediation commit: `77ee4414df37b9f88979bde02742756538ba45af` (tree
  `62736d355e2650b5f40cdc3d904ed49466cbf025`).
- Parent baseline: `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` (tree
  `630b803d57e27a0044686595e108868072aa20ea`).
- Original specification: `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` (tree
  `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7`).

**DISPOSITION: B6-SPEC-R1 SOURCE-BOUND — READY FOR Q2**
