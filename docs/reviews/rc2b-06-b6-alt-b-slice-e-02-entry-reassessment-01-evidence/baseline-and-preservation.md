# E-2 entry reassessment — baseline and preservation

Date: 2026-11-09 (Asia/Manila)

## Baseline

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| HEAD | `bdc7fa2cc84f4a43269270405e5ff9fae7273c28` |
| Tree | `2bf9d9710c46a7a67c97f52d65faa5be63ca7ae2` |
| Worktrees | exactly one: `C:/Users/Laszlos/source/repos/Gagamba bdc7fa2 [main]` |
| Tracked/index state | clean; no staged, modified, or deleted tracked path |
| Historical untracked paths | 44 |
| Historical untracked content comparison | 44 of 44 present; per-file SHA-256 match against `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation inventory digest — sorted path/hash lines | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation inventory digest — compact JSON path/hash map | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 19 commits ahead of `origin/main`; no push |
| Network / package / cloud | none used |

Ancestry verified with `git merge-base --is-ancestor` (all exit 0): P0 acceptance `5b2ef14`; P0 reconciliation `77101ac`; E-2 entry owner decision `4263c8b`; E-2 entry assessment `a35ce10`; P4 coverage acceptance `2d39380`; E-1 acceptance `8bd7142`; F-01 closure `1ed1fc7`; F-02 closure `e86254e`; F-03 closure `6d08ae5`; Slice E entry `c7b7621`; Alternative B decision `d804d90`; F02 Mechanism B acceptance `4aab877`; S2/F02 trust-store decision `142cc23`; native-input freeze `504b6d0`.

## Authority identities under reassessment

| Object | Git blob | SHA-256 |
|---|---|---|
| P0 adopted provenance record | `db25949d25babd474d00ae5b2f436e15507f22b3` | `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` |
| P0 owner acceptance decision | `8dd20d11202bb4ed7fb0dd3cdf447eaa3c5e54b7` | `049d9195bd2cc9b2b02515b771c4d15263a49042dc56d467d4d33a81c5ba50f5` |
| D1 owner decision | `83fe8d2649675e00b76457885fd0169b4ae77498` | `438194de12d063f2212038879b71f6f4f5c2f68526f32a19649ea49f8f47916b` |

## Source state

| Check | Result |
|---|---|
| `git diff --stat 4c5a42f0fecff90dcb30088ca0203956e7a80032 HEAD -- src tests eng` | empty (exit 0) |
| `accepted-validation-catalogs` / `validation-catalog` / `GAVA` / `validation-pre` / catalog hash symbols in `src`, `tests`, `eng` | no matches |
| E-2 reader, catalog store, approval verifier, launcher, capability | none exist |

No design-level acceptance of the catalog approval chain has been converted into executable code. E-1 remains the only relevant production-unreachable surface.

## Preservation

The 44 historical untracked paths were not cleaned, deleted, rewritten, staged, or committed by any of the P0, P0-acceptance, D1, or reassessment documentation commits. The reassessment report, its evidence directory, and the current-state updates are additive documentation paths.
