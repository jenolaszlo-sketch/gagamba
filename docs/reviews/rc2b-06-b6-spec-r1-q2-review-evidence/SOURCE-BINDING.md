# B6-SPEC-R1-Q2 — Source Binding

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-R1-Q2 (independent specification requalification).
**Mode:** Read-only. No fetch, reset, clean, checkout, rebase, amend, merge or push.
**Reviewer workspace:** `C:\Users\Laszlos\source\repos\Solo\b6-spec-r1-q2-review` (outside the canonical repo).

## 1. Repository identity

| Item | Verified value |
|---|---|
| Repository | `jenolaszlo-sketch/gagamba` (`https://github.com/jenolaszlo-sketch/gagamba.git`) |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| HEAD at review | `5b3466719a50df072848ecc5f832adbaeb06b3bf` |
| `origin/main` at review | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` |
| Ahead/behind (`origin/main...main`) | `0  4` (local ahead four, behind zero) |
| Worktrees | 74 pre-existing; none created or modified by Q2 |

The local/remote relationship matches the producer handoff (local `main` = `5b34667`, `origin/main` = `2cd8c1d`, ahead four, behind zero). Nothing was pushed.

## 2. Mandatory commit and tree identities

Independently recomputed with `git cat-file -t`, `git rev-parse <c>^{tree}` and `git log -1 --format=%P`.

| Role | Commit SHA | Reported tree | Actual tree | Match |
|---|---|---|---|---|
| Original B6 specification | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` | YES |
| Q1 review / R1 parent | `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` | `630b803d57e27a0044686595e108868072aa20ea` | `630b803d57e27a0044686595e108868072aa20ea` | YES |
| R1 substantive remediation | `77ee4414df37b9f88979bde02742756538ba45af` | `62736d355e2650b5f40cdc3d904ed49466cbf025` | `62736d355e2650b5f40cdc3d904ed49466cbf025` | YES |
| R1 provenance receipt | `5b3466719a50df072848ecc5f832adbaeb06b3bf` | `87bb8f73520c37cbffe936c4f8c336398e9112d7` | `87bb8f73520c37cbffe936c4f8c336398e9112d7` | YES |

All four objects are present as `commit` objects. Every reported tree identity is reproduced exactly.

## 3. Linear ancestry

Each commit has exactly one parent, forming the required chain:

```
cc47f4d  parent 2cd8c1d   "specify B6 installed-artifact trust and qualification"
  └─ 08ebcb5  parent cc47f4d   "add independent B6 specification review"
       └─ 77ee441  parent 08ebcb5   "commit B6 specification remediation (B6-SPEC-R1)"
            └─ 5b34667  parent 77ee441   "record B6-SPEC-R1 provenance receipt (B6-SPEC-R1-BIND-01)"
```

`git merge-base --is-ancestor` returns exit 0 for `cc47f4d→08ebcb5`, `08ebcb5→77ee441` and `77ee441→5b34667`. The ancestry is linear and matches `cc47f4d → 08ebcb5 → 77ee441 → 5b34667`.

Required distinctions established:

- `08ebcb5` added exactly one file, `docs/reviews/rc2b-06-b6-spec-q1-independent-review.md` — it **is** the Q1 review and the R1 parent baseline. It is **not** the R1 remediation.
- `77ee441` has `08ebcb5` as its sole parent and introduces the six R1 documents.
- `5b34667` has `77ee441` as its sole parent and is the provenance receipt that follows (does not replace) the remediation.
- The remediation is a committed object, not a working-tree state. The BIND-01 receipt exists precisely because the R1 content was initially complete-but-uncommitted.

## 4. Changed-file inventories and deltas

### 4.1 R1 substantive remediation (`08ebcb5 → 77ee441`)

| Status | Path | +/− |
|---|---|---|
| M | `docs/PROJECT-STATUS.md` | +6 / −5 |
| M | `docs/design/rc2b-06-b6-installed-artifact-trust.md` | +442 / −25 |
| A | `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` | +487 / −0 |
| M | `docs/handoff.md` | +5 / −4 |
| M | `docs/plans/rc2b-06-b6-qualification.md` | +250 / −33 |
| M | `docs/work-queue.md` | +4 / −3 |

Totals: **6 files, +1194 / −70.** Matches the reported `+1194/−70` and the six-file list exactly.

### 4.2 Provenance receipt (`77ee441 → 5b34667`)

| Status | Path | +/− |
|---|---|---|
| M | `docs/PROJECT-STATUS.md` | +1 / −1 |
| A | `docs/evidence/rc2b-06-b6-spec-r1-bind-01-receipt.md` | +72 / −0 |
| M | `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` | +6 / −5 |
| M | `docs/handoff.md` | +1 / −1 |
| M | `docs/work-queue.md` | +1 / −1 |

Totals: **5 files, +81 / −8.** Matches the reported `+81/−8` and the five-file list exactly.

**Critical check:** the receipt does **not** touch `docs/design/rc2b-06-b6-installed-artifact-trust.md` or `docs/plans/rc2b-06-b6-qualification.md`. Neither B6 design nor qualification plan was altered by the receipt. The receipt's edits are limited to (a) the new receipt file, (b) SHA/tree references in the R1 evidence file, and (c) pointer wording in `PROJECT-STATUS.md`, `handoff.md`, `work-queue.md`.

## 5. Non-documentation scope

Between the pre-B6 baseline `2cd8c1d` and the tip `5b34667`, **no** path under `src`, `tests`, `native`, `eng` or `.github` changed (`git diff --name-only 2cd8c1d 5b34667 -- src tests native eng .github` is empty). Subtree identities are identical at `2cd8c1d`, `77ee441` and `5b34667`:

| Subtree | Tree SHA (all three commits) |
|---|---|
| `src` | `ff5bb98dc4a3566dd4c8e8b58738979d6e9c6997` |
| `tests` | `fb2424c92854bbc24e9adec1dd2e35ef97b0a716` |
| `native` | `85631d2f04512376621d3d09f51d67fd90f45335` |
| `eng` | `9fbc9034cb3e7a611251e8f31356ffd3c0aa2b61` |
| `.github` | `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` |

Only `docs/` differs across the two commits. Both changesets are documentation/evidence-only.

## 6. Exact source extraction and preservation

Pinned versions were extracted with `git cat-file blob <commit>:<path>` into `source-pinned/<tag>/…`. Each extracted file was re-hashed with `git hash-object` and matched against `git rev-parse <commit>:<path>`. All 18 extracted files matched their committed blob identities (see `evidence/extracted-blob-hashes.txt`). Producer bytes were not modified.

## 7. Provenance classification

- The R1 pointer documents at `77ee441` correctly avoided embedding a commit's own SHA by deferring the R1 SHA to the follow-up receipt. The receipt then embedded `77ee441` in the pointer docs and the R1 evidence file. No document claims a commit contains its own final SHA.
- `08ebcb5` is identified everywhere as the Q1 review / R1 parent baseline, never as the remediation.
- `77ee441` is identified as the substantive remediation; `5b34667` as the provenance receipt.

## 8. Source-binding conclusion

**Source binding is correct and unambiguous.** All mandatory objects exist, all reported trees and deltas reproduce exactly, ancestry is linear and matches the required chain, and no non-documentation path changed. The previously required BIND-01 correction (not mislabelling `08ebcb5` as the remediation) is itself correctly recorded.

The two residual baseline-table observations in the R1 evidence file are classified in `CONSISTENCY-AUDIT.md` §4 and `FINDINGS.md`; they are non-blocking because the authoritative commit/tree identities are exact and independently verified.

**No `B6-SPEC-R1-Q2 BLOCKED — SOURCE BINDING` condition applies.**
