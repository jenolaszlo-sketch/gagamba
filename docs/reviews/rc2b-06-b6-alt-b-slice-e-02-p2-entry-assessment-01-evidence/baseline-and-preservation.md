# P2 entry assessment — baseline and preservation

Date: 2026-11-09 (Asia/Manila)

## Baseline

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| HEAD / tree | `28275ba516ba9326bd7a850357fa3143749ecd03` / `3ae03486d3373caab9cfd9191204761dcb955cc6` |
| Worktrees | exactly one |
| Tracked / index | clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 24 commits ahead; no push |

Ancestry verified (`git merge-base --is-ancestor`, exit 0) for: P1 acceptance `28275ba`; Q1 `ab5f7a7`; P1 `06006a7`; entry-owner-02 `2dab45a`; reassessment `ad52aae`; D1 `bdc7fa2`; P0 acceptance `5b2ef14`; E-2 entry decision `4263c8b`; P4 `2d39380`; E-1 `8bd7142`; F-01 `1ed1fc7`; F-02 `e86254e`; F-03 `6d08ae5`.

## Decision and implementation identities (unchanged)

| Object | Git blob |
|---|---|
| P0 adopted provenance record | `db25949d25babd474d00ae5b2f436e15507f22b3` |
| D1 approval-chain decision | `83fe8d2649675e00b76457885fd0169b4ae77498` |
| E-2 entry owner decision 02 | `f5ab96c6fedc480b427beeb9e1941a64e83a2b43` |
| P1 owner acceptance | `f360932adc4909b4f26e32b6005a592618dd0912` |
| P1 verifier file blob | `fdff96b93176058f025bb814603c0678d78b6fe6` |
| P1-Q1 report | `7ee0b781dbfd74aef873e5649c83d5db28795016` |

## Source state

The repository contains no R/Wc/P reader, verifier, issuance tool, or protected store; the only accepted Slice E-2 implementation is the P1 contract core (`src/Gagamba.Execution.Linux.M1/Validation/`, internal and production-unreachable). No production, test, tooling, project, or frozen artifact was modified by this assessment.
