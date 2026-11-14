# Owner identity and provenance checks

Baseline `6123b6f4de47cb542db3bdab55c0fc49d5cd095c` and tree
`ee97488ed06ae3836e7aeb81f12bf1fede096c9d` identify the reviewed
checkout. Resolve all decision, annex and evidence paths in the
[owner decision](../rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01.md) against that baseline.
Owner verification reviewed and reconciled the committed Q1 evidence.
Q1's source accesses, witness runs and probe source remain in the
[Q1 evidence](../../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/README.md).

| Command or check | Observed before adoption |
|---|---|
| `git rev-parse --show-toplevel HEAD 'HEAD^{tree}' origin/main`, `git branch --show-current`, `git worktree list --porcelain` | Canonical Gagamba path, expected HEAD/tree, one main worktree. |
| `git --no-optional-locks status --porcelain -uno`, `git stash list` | Both empty. |
| `git rev-list --left-right --count HEAD...origin/main` | 9 / 0 from existing tracking state. |
| `Get-ChildItem -LiteralPath .git -Filter '*.lock'` and `Get-CimInstance Win32_Process` filtered to Git/Git LFS processes identifying Gagamba | Zero Git locks and zero identifiable conflicting Git writers. |
| `git -c core.quotePath=false ls-files --others --exclude-standard` against the freeze inventory, then `Get-Item -LiteralPath` and `Get-FileHash -Algorithm SHA256 -LiteralPath` for each entry | Exactly 44 paths. Every length and SHA-256 matched. |
| `git log -9 --format='%H %P'` | Direct parent chain from Q1 through R1, bootstrap order, original preparation, BJ/IE order, provenance, entry assessment and E-2 closure. |
| `git rev-parse HEAD:<path>` for the candidate and named evidence, with the candidate also resolved at R1, the annex at the bootstrap owner commit and the BJ/IE ruling at its originating commit | 21 explicitly bound Git identities matched. All 13 canonical historical path/blob bindings matched the accepted inventory. |
| `git diff --exit-code acd61fd HEAD -- <candidate-and-controlling-paths>` | Candidate, schema decision/evidence, BJ/IE decision and preparation report/evidence match R1. |
| `Get-FileHash -Algorithm SHA256 -LiteralPath <candidate>` and independently `hashlib.sha256(subprocess.check_output(['git','show','HEAD:<candidate>']))` | 21,225 committed bytes identical to the working file and both raw hashes match the adopted SHA-256. Both R1 and Q1 name that same blob and SHA-256. |
| Compare committed raw-source IDs, lengths and hashes between the Q1 table and reconciliation inventory | 12/12 rows match. This is owner inspection of Q1's already executed rehash evidence. |
| Inspect committed Q1 schema and results tables, count their bounded rows, and verify J03/J04, J12, H14 and S8191–S8193 verdicts | 11/99, 110 bootstrap rows (13 pass, 97 refusal), 46 binary and six field-only release cases, with verified key escape, duplicate, value escape and size-boundary results. |

The pre-commit check verifies the baseline and candidate again, accepts
only the decision, this evidence and the three current-state documents
in the staging delta, and runs `git diff --cached --check`. The path
allowlist preserves all previously accepted records, frozen contracts,
production source, tests, project files and tooling. The post-commit
check verifies the one-parent document commit, candidate and Q1
identities, clean index/working tree and the 44 historical files.
