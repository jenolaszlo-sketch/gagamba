# P1-Q1 — baseline and reviewed revision

Date: 2026-11-09 (Asia/Manila)

## Baseline

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| HEAD | `06006a7035ca3a7615189b867584b1b3955eeb7a` |
| Tree | `89330a5cf779ec35bb208e6bf9ec67bf0c12e87a` |
| Worktrees | exactly one |
| Tracked / index | clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 22 commits ahead; no push |

Ancestry verified (`git merge-base --is-ancestor`, exit 0): P1 `06006a7`; entry-owner-02 `2dab45a`; reassessment `ad52aae`; D1 `bdc7fa2`; P0 acceptance `5b2ef14`; E-2 entry decision `4263c8b`; P4 `2d39380`; E-1 `8bd7142`; F-01 `1ed1fc7`; F-02 `e86254e`; F-03 `6d08ae5`.

## Reviewed revision identities

| Object | Git blob | Working-file SHA-256 |
|---|---|---|
| P0 adopted record | `db25949d25babd474d00ae5b2f436e15507f22b3` | `11bca1f6…` |
| D1 decision | `83fe8d2649675e00b76457885fd0169b4ae77498` | unchanged |
| Entry owner decision 02 | `f5ab96c6fedc480b427beeb9e1941a64e83a2b43` | unchanged |
| P1 report | `8dfe6b8cb9917dbc7018ee2cea4e5584879f8dcd` | — |
| `CanonicalJson.cs` blob | `276581c492e155d7b24090db8967eb4179730df0` | `fa1ec845…` |
| `FrozenSchemaRules.cs` blob | `868ddbeeaafdcb070a53c2c6f6c083a1fd4e50f6` | `435fff7e…` |
| `FrozenContracts.cs` blob | `3344069cd3f7b766cb4a43e6e91b18caa7d03b4f` | `8dae7db2…` |
| `FrozenContractVerifier.cs` blob | `fdff96b93176058f025bb814603c0678d78b6fe6` | `3f357b8d…` |

## Build identities (reviewer rebuilds)

| Artifact | Producer | Reviewer rebuild |
|---|---|---|
| Debug M1 MVID | `a50bd575-e181-4a75-99af-700bc9b97a1b` | rebuilt; same verifier behaviour |
| Debug M1 SHA-256 | `F63DDE2F…` | differs (non-bit-reproducible rebuild) |
| Release M1 MVID / SHA-256 | `19a06c60…` / `321F7131…` | `ec05b2be…` / `5B649DAE…` |
| Release verifier | PASS | PASS (reviewer rebuild) |
| Debug-as-production | FAIL (expected) | FAIL (expected) |

Rebuilt-binary differences are the accepted non-bit-reproducibility qualification (P4 coverage acceptance §5 evidence-provenance limits; P4-03 Q1-F02). Source blobs are the identity anchor; both builds pass the unchanged verifier.
