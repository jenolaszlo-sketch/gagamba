# P2-Q1 — verified baseline and reviewed revision

Date: 2026-11-11 (Asia/Manila).

## Baseline (verified before any write)

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| HEAD | `9b80969ce766b6d8939775710aa77d080c3ff674` |
| Tree | `fc77bc1d673f787adae2d332141f1dd5f854e883` |
| Worktrees | exactly one (`git worktree list --porcelain`) |
| Tracked / index | clean (0 non-`??` porcelain lines) |
| Historical untracked paths | 44 of 44 present; per-file SHA-256 match against `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0`; NUL-delimited exact-set comparison identical |
| Recorded `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; **no fetch**) |
| Local `main` position | 27 commits ahead of the recorded remote-tracking reference; no push |
| Stashes | none |

Ancestry verified with `git merge-base --is-ancestor` (all exit 0): P2
implementation `9b80969`; P2 entry owner decision `dfa570f`; P2 entry
assessment `051c682`; P1 owner acceptance `28275ba`; P1-Q1 `ab5f7a7`; P1
implementation `06006a7`; E-2 entry owner-02 `2dab45a`; entry reassessment
`ad52aae`; D1 `bdc7fa2`; P0 owner acceptance `5b2ef14`; E-2 entry
reconciliation `4263c8b`; P4 refusal-only acceptance `2d39380`; E-1 acceptance
`8bd7142`.

Raw values: [executed/identity-checks.txt](executed/identity-checks.txt) and
[executed/untracked-check.txt](executed/untracked-check.txt).

## Reviewed implementation identity

| Object | Git blob |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceVerifier.cs` | `2b482a7068b3eae4603a93d79454e9c69ce7b6d3` |
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceContracts.cs` | `652486d4ab26fec6a975e876e59a473b66bc331c` |

Both match the identities reported by the producer. The reviewed P2 commit
`9b80969` adds exactly 2 production files, 4 test files, 1 report, 17 evidence
files and modifies only the three current-state documents; no existing
production, test, tooling, project, solution, package, or frozen artifact file
changed (verified with `git show --name-status`).

## Previously accepted artifacts (read-only during review)

| Object | Verified state |
|---|---|
| P1 `CanonicalJson.cs` | blob `276581c492e155d7b24090db8967eb4179730df0` (= P1 accepted) |
| P1 `FrozenSchemaRules.cs` | blob `868ddbeeaafdcb070a53c2c6f6c083a1fd4e50f6` (= P1 accepted) |
| P1 `FrozenContracts.cs` | blob `3344069cd3f7b766cb4a43e6e91b18caa7d03b4f` (= P1 accepted) |
| P1 `FrozenContractVerifier.cs` | blob `fdff96b93176058f025bb814603c0678d78b6fe6` (= P1 accepted) |
| `Installed/FinalAcceptance.cs` (FinalAcceptanceCodec) | blob `4a3097cb48dbbc87809ffef3d53e88f21d97a14a`; last changed at `831c980` (F02); untouched by P1/Q1/P2 |
| `eng/**` | last changed at `52037fa` (F-03); no diff from the P1 revision to HEAD |
| `src/Gagamba.Execution.Linux.M1/{Installed,Bootstrap,Launch,Lifecycle,Restricted,Execution,Native,Preparation,Recovery}` | no diff from `06006a7` to HEAD |
| Project/solution files | no change inside the range (the only `*.csproj` difference is the previous Q1's evidence probe under `docs/reviews/...-p1-q1-evidence/probe/`, which is not part of `Gagamba.sln`) |

## Build identities (reviewer rebuilds)

| Artifact | Reviewer rebuild |
|---|---|
| Debug M1 | 0 warnings / 0 errors; instrumented mode PASS; MVID `bd8a820d-d914-42b9-b455-edaad2e42a5e`; SHA-256 `E3089FB9D6CFF30AA8D07A5C8B64A88BA925485D7814AC4D53FBF957878153CC` |
| Release M1 | 0 warnings / 0 errors; production mode PASS; MVID `1994ef0f-9e7a-44fb-b2eb-407213fbc2b7`; SHA-256 `BC8E33E8E897D14167E0D447B42DED73350BF7D3E83D19D2A0D8DAA37FE320EA` |

Rebuild hashes differ from the producer's recorded binaries; this is the
accepted non-bit-reproducibility qualification (P1-Q1-F04 lineage; P4-03
Q1-F02). Source blobs, not rebuilt binaries, are the identity anchor; both
configurations pass the unchanged production-assembly verifier / expected
negative control.
