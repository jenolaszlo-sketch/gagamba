# Baseline and preservation record

Verified before any assessment write, on the canonical checkout
`C:\Users\Laszlos\source\repos\Gagamba`.

## Repository identity

| Item | Verified value |
|---|---|
| Repository root | `C:/Users/Laszlos/source/repos/Gagamba` |
| Git directory | `.git` |
| Branch | `main` |
| HEAD | `a30d1807686ee15d39ef7e9103e8b992062bd971` |
| Tree | `420fcb2c9273c39c5f88243d7a5d9bd8bdcd0ec7` |
| Remote URL | `https://github.com/jenolaszlo-sketch/gagamba.git` |
| Worktrees | exactly one: `C:/Users/Laszlos/source/repos/Gagamba a30d180 [main]` |
| Stashes | 0 |
| Tracked/index changes | 0 staged, 0 unstaged, 0 deleted |
| Recorded `origin/main` | `a30d1807686ee15d39ef7e9103e8b992062bd971` |
| Ahead/behind vs recorded tracking ref | `0 0` |

No fetch, pull, push, merge, rebase, reset, clean, prune, stash, branch, or
additional worktree was used.

## Historical untracked preservation

Manifest: `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`

| Check | Result |
|---|---|
| Manifest entries | 44 |
| Manifest paths present as files | 44 / 44 |
| Per-file SHA-256 matches | 44 / 44 |
| SHA-256 mismatches | 0 |
| Historical paths missing | 0 |
| Manifest SHA-256 | `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1` |

At baseline the live untracked set contained exactly the 44 manifest paths.
After the assessment writes, the only additional untracked paths are the
authorized assessment report and this evidence directory; the 44 historical
paths remain hash-identical.

## Owner-decision and package ancestry

`git merge-base --is-ancestor <commit> HEAD` returned exit 0 for all:

| Record | Commit |
|---|---|
| P0 provenance | `77101ac18c0e019a9` |
| P0 acceptance | `5b2ef145088818c9b9b145d706816a075951c861` |
| D1 adoption | `bdc7fa2cc84f4a43269270405e5ff9fae7273c28` |
| E-2 entry owner-01 | `2dab45aa4f4be6b0f713a35b4d50bdfa6fc925be` |
| E-2 entry reassessment | `ad52aae811a4de8e9f47661e531e16c455c3274b` |
| P1 implementation | `06006a7035ca3a7615189b867584b1b3955eeb7a` |
| P1-Q1 | `ab5f7a728a3f04fb6e00dadf28c6a9a7293569bf` |
| P1 acceptance | `28275ba516ba9326bd7a850357fa3143749ecd03` |
| P2 entry assessment | `051c6824de86b37e8c582721c43f8b622f3e4402` |
| P2 entry owner | `dfa570fcfa4b55176d6cc63ad1d8c8930d4a58eb` |
| P2 implementation | `9b80969ce766b6d8939775710aa77d080c3ff674` |
| P2-Q1 | `285c016` |
| P2-Q1 corrections | `661800d`, `a79dc60` |
| P2 acceptance | `24c4cf4` |
| P2 acceptance wording correction | `a30d180` |
| E-1 acceptance | `8bd71425eb31b69290affa2b86c78afce1bd0882` |
| P4 coverage acceptance | `dfe820170dc45efe57f8e6c74b73ffff6cf57a91` |

## Baseline deviations

None. No material baseline discrepancy was found, so no stop condition was
triggered.
