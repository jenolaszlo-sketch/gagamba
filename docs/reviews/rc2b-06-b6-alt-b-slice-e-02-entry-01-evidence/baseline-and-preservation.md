# Baseline and preservation — E-2 entry assessment

Date: 2026-11-09 (Asia/Manila)

Baseline recorded before any assessment edit.

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| HEAD | `e86254e60fb3ded770156519e1d3e15e73bf36fc` |
| Tree | `69a18e8c3a1e199802f4cfc1906de2a30e3a7928` |
| Worktrees | `C:/Users/Laszlos/source/repos/Gagamba e86254e [main]` — exactly one |
| Tracked/index state | clean |
| Historical untracked paths | 44 |
| Historical untracked comparison | 44/44 paths present; per-file SHA-256 match; `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` |
| Local `main` vs `origin/main` | 14 commits ahead |
| Push | none |

## Commands and results

```text
git rev-parse HEAD
e86254e60fb3ded770156519e1d3e15e73bf36fc

git rev-parse 'HEAD^{tree}'
69a18e8c3a1e199802f4cfc1906de2a30e3a7928

git branch --show-current
main

git worktree list
C:/Users/Laszlos/source/repos/Gagamba  e86254e [main]

git status --porcelain -uall | Measure-Object -Line
44

git rev-parse origin/main
53850aa158cf26f5a40fb9bd51c606a111188ef3

git rev-list --count origin/main..main
14
```

## 44-path preservation comparison

The historical inventory is `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`.

The current untracked path set was obtained with:

```powershell
git ls-files --others --exclude-standard -z
```

Each current path was hashed with SHA-256. The comparison checked:

- every current path exists in the historical inventory;
- every historical path still exists;
- every per-file SHA-256 equals the historical value.

Result:

```text
current_untracked_count=44
historical_count=44
diff_count=0
```

## Inventory digests

Two content-inventory digests were computed over the sorted 44 paths:

1. **Sorted path/hash lines** — each line is `<path>\t<sha256>`, lines joined with LF and terminated with LF, encoded UTF-8, then SHA-256.

   ```text
   de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1
   ```

2. **Compact JSON path/hash map** — sorted paths mapped to SHA-256, compact JSON, UTF-8, then SHA-256.

   ```text
   b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327
   ```

These digests are content inventories. They are not hashes of the historical porcelain-status list.

## Preservation statement

The 44 historical untracked paths are a hard preservation constraint. They were not cleaned, deleted, rewritten, or staged. The new assessment report and evidence files are additive documentation paths and are not part of the historical 44.

No branch, linked worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.
