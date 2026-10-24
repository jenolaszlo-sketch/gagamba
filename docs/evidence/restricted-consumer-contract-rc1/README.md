# RC-1 contract design evidence

- Date: 2026-10-24 (Asia/Manila).
- Slice: restricted consumer contract design and implementation planning only.
- Baseline source: `629a08d2ee82a9ac084d87ed73ad8d584661ea28`.
- Final production source: unchanged from baseline; final design commit/tree in
  the external completion receipt and `git rev-parse HEAD HEAD^{tree}`.
- Branch: `design/gagamba-rc1-restricted-consumer-contract`.
- Host: Windows authoring workstation; exact OS/architecture and Python runtime
  are in `source-manifest.json`. .NET SDK was not queried; no build/test was run.
- Native prerequisites: not used; no native Linux environment commissioned.
- Qualification scope: source/document inspection and static document validation.
- Exclusions: production/test/project/package changes; native workloads; package
  building/publication; Hufu changes; C1 independent acceptance; C3/C4 implementation;
  G3/G4/G5 or production offline qualification; push/merge/tag.

## Claim and evidence boundary

| Baseline observation | Documentation change | Established here | Not established |
|---|---|---|---|
| Restricted values/encoder internal; binary decoder absent; public SPI lifecycle-only | RC-D01–D03 passive package/API/reader design | A source-grounded proposed contract and work order | Implemented decoder, supported package or closed AE4-B01 |
| Owner/capability/arbiter/recovery machinery internal; consumer process lifetime missing | RC-D04–D09 facade, authority and deployment obligations | Reviewable proposed responsibility split | Native lifetime, stop, confinement or capture behavior |
| C1 producer evidence, C2 reviewed WSL2 component, approved C3 Policy A | Preserve original status and gate dependencies | Fresh local/remote inventory and truthful status | New independent C review or native qualification |

No baseline runtime failure/fixed runtime observation is claimed. No native
cleanup was required because no target/owner/helper workload was started. Existing
worktrees and unrelated files were left in place; all changes are in this isolated
worktree's `docs/` directory.

## Evidence files and byte domains

- `source-manifest.json`: 46 Gagamba/Hufu source/document bindings, exact revision,
  Git blob, byte length and SHA-256 of **Git blob bytes** (not Windows CRLF text),
  plus src/tests/native trees, host and observation time.
- `local-refs.txt`, `worktrees.txt`, `remote-heads.txt`: actual Git inventories
  captured after creating this branch, including this RC-1 worktree. The remote
  inventory was also checked before selecting the parent. No branch fetch/merge
  was needed; no later restricted implementation was advertised in the inventory.
  The terminal blank separator in `worktrees.txt` was removed for Git whitespace
  hygiene; no inventory entry was changed.
- `commission.md`: readable supplied assignment, LF/no BOM/trailing whitespace
  removed. Original attachment byte length/hash/path are recorded separately;
  this is a normalized readable copy, not a claim of byte-identical preservation.
- `input-and-vector-check.json`: original assignment hash and Python SHA-256 check
  of the existing 873-byte literal golden vector. It does not execute the encoder
  or constitute a codec test run.
- `validation.json`: static check names/results and changed paths. Validates
  document structure/link/scope/evidence consistency, not architecture correctness.
- `document-sha256.json`: SHA-256 of final LF-normalized document/evidence bytes
  for every changed file except this hash manifest itself. Its own Git blob is
  bound by the final commit/tree. Commit hashes are reported externally to avoid
  circular self-hashing.

Authoring/check scripts are retained outside the Gagamba worktree at
`C:\Users\Laszlos\source\repos\Solo\artifacts\gagamba-rc1-tools` so this commit
contains documentation only. The independent reviewer can recompute every Git
blob hash without those scripts.

## Inspection and validation commands

Commands actually used during source inspection include `git status --short
--branch`, `git worktree list --porcelain`, `git for-each-ref`, `git ls-remote
--heads origin`, `git rev-parse`, `git show <revision>:<path>`, `rg --files`,
focused `rg -n`, and `Get-Content` of the bound documents/source. These are read-only
observations. Initial speculative searches for `ManifestCodecTests.cs` and a
`Lifecycle` test folder found no such paths; corrected discovery located
`Contracts/ManifestTests.cs` and `Recovery/PreparationOwnershipTests.cs`. Those
search misses were not code/test failures. Some large inspection outputs were
truncated; subsequent targeted reads supplied the relied-on sections.

Worktree creation: `git worktree add -b design/gagamba-rc1-restricted-consumer-contract
C:/Users/Laszlos/source/repos/Solo/gagamba-rc1-contract
629a08d2ee82a9ac084d87ed73ad8d584661ea28`.

Source binding: `C:\Python314\python.exe
C:\Users\Laszlos\source\repos\Solo\artifacts\gagamba-rc1-tools\collect_sources.py`.
Observed result: 46 source bindings; literal golden length 873 bytes; SHA-256
matches `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.

Final static checks: `C:\Python314\python.exe
C:\Users\Laszlos\source\repos\Solo\artifacts\gagamba-rc1-tools\validate_docs.py`,
`git diff --check`, staged diff/path review, unchanged tree comparison, and final
`git status --porcelain`. Exact outcomes are in `validation.json` and the external
completion receipt. No `dotnet test`, `dotnet build`, `dotnet pack` or native
executable command is part of RC-1 validation.

The first staged `git diff --cached --check` reported a new blank line at EOF in
`worktrees.txt`, inherited from the captured porcelain output. Only that terminal
blank separator was removed. The combined diff against the parent and final
staged diff were rechecked; this was a document-format finding, not a runtime test.

Independent review and acceptance remain pending; see the
[GLM handoff](../../reviews/restricted-consumer-contract-rc1-handoff.md).
