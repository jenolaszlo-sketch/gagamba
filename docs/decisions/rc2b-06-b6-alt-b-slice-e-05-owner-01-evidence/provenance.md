# Authorization and qualification provenance

| Milestone | Commit | Tree or identity |
|---|---|---|
| Published E-4 acceptance | `1116c66d7858e6000f0001a0abe39319e4e694be` | `86768fbd5da028fc271fda295a7da5c389aab8bc` |
| Earlier blocked E-5 entry | `b309a9ec1a9a0f160ea0c2dd513001c3561a36d9` | Direct child of published E-4 |
| Frozen E-5 design | `1ff4d6e6d1f756646072790478921d9a64f15654` | `0dd0da1ba6e395ac3e7339b07ad4809e9a20b62d` |
| Bounded entry reassessment | `02ba7ad3c81e72de885349c2bd54b0b7ab16bc12` | `dc2b1303a9529cf0ed8b03ff2dd5912ca0e01f60` |
| Six-file authorization | `7a30d0a05a7c898c4e90cc2ca01280db166ed6f2` | `35eff6247f52d85e565adae6c226ba918ef8dcbc` |
| Qualified implementation | `28895d55de98eea4bc402373b653f7216e092843` | `e314f2e8a1259e2263c8494bb0c8ca9f7eb27016` |
| Producer evidence | `0a77422c1349b99e518292d46314606b383527e4` | `02353c07b031199c01d8750119a8514521052b86` |
| Independent Q1 and owner starting revision | `b6631367d39531b2c5af8a1917760d7c807f8e73` | `b62300eb6b3093ecd7857f8bb6c82eda529c2a15` |

Each row is the direct child of the preceding row. The producer commit added its report, manifest and 40 annex members. Q1 added its report, manifest and 188 annex members. Neither changed implementation or committed tests. At owner preflight the canonical `main` tracked checkout and index were clean, with one worktree and no stashes. Fetch and push origin identify `https://github.com/jenolaszlo-sketch/gagamba.git`, whose live `main` matched the E-4 milestone. [Owner integrity and execution audit](acceptance-audit.json)

| Frozen artifact | Git blob | SHA-256 |
|---|---|---|
| Contract | `121443efb9e145e2413c5a2c40f59e45a55eaa55` | `8d782d55544a1fcc65a7adbe035e21ce7d7cd10d31658274b1620527b8b02a54` |
| Requirements and verification rows | `63ebfb7afed2dfb2883fae237ed64472c2adad66` | `ca9c64bbf85238859fbce8d07e260ab0cbd7940738d7a928d0f2da32ab3587f0` |
| Q1 evidence manifest | Bound at Q1 revision above | `3e57695efbf302bf15bf50dd32f502f3b3832a7d37d93fde28e06e91a756731b` |

The owner reverified all 814 historical, 41 producer and 189 Q1 manifest entries at their pinned revisions, starting HEAD and working bytes, together with the exact paths, lengths and raw hashes of all 44 historical untracked files. The six candidate files and both frozen artifacts match the candidate, Q1 and current checkout. [Executable acceptance evidence checker](verify-acceptance.py)

## Exact additive scope

| Role | Path |
|---|---|
| Production | `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModelData.cs` |
| Production | `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModel.cs` |
| Test | `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelTests.cs` |
| Test | `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelLifecycleTests.cs` |
| Test | `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelCompositionTests.cs` |
| Test | `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelVectors.cs` |

The authorizing decision and frozen contract permit this exact set. Owner review accepts the byte-identical qualified implementation with the recorded managed and downstream bounds. Existing project, CI, configuration, build-policy and frozen evidence files retain their identities. [Authorization bindings](../rc2b-06-b6-alt-b-slice-e-05-entry-auth-01-evidence/bindings-and-allowlist.md) · [Q1 candidate inventory](../../reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence/preflight.json)

Owner review reused the verified Q1 evidence and parsed its original executed results, refusal receipts, artifact bindings and compiled graphs. The acceptance checker records its observations without another qualification run. Before publication, the complete outgoing range is checked for scope, sensitive material and fast-forward ancestry. Publication, owner commit/tree and CI evidence are recorded externally after this commit.
