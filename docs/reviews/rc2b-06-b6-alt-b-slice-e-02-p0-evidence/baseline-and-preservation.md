# P0 baseline and preservation

Date: 2026-11-09 (Asia/Manila)

## Baseline

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| HEAD | `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe` |
| Tree | `934bf6567e1ee10bff59d3669bf0ca2a95c6d6d3` |
| Worktrees | exactly one: `C:/Users/Laszlos/source/repos/Gagamba 4263c8b [main]` |
| Tracked/index state before P0 edits | clean; no staged or modified tracked path |
| Historical untracked paths | 44 |
| Historical untracked content comparison | 44 of 44 present; per-file SHA-256 match against `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation inventory digest — sorted path/hash lines | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation inventory digest — compact JSON path/hash map | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 16 commits ahead of `origin/main` |
| Network / package / cloud | none used |
| P0 source changes | none in `src`, `tests`, or `eng` |

## Preservation method

The preservation inventory is `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`. For each of its 44 entries:

1. Confirm the path exists.
2. Compute SHA-256 over the exact bytes.
3. Compare with the recorded lower-case hash.
4. Record `diff_count`.

The lines digest is computed over sorted `path + TAB + lowercase SHA-256 + LF` lines, including the trailing LF, encoded as UTF-8. The map digest is computed over a compact JSON object with keys in the same sorted order and lowercase-hash values, encoded as UTF-8.

## New P0 paths

The P0 report, this evidence package, and the proposed provenance record are additive documentation paths. They are not members of the historical untracked set and do not modify its contents.

## Operations not performed

No branch, linked worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.
