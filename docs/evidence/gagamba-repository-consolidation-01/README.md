# Gagamba repository consolidation and CI remediation

Commission `GAGAMBA-REPOSITORY-CONSOLIDATION-AND-CI-REMEDIATION-01`

Date 2026-11-15 (Asia/Manila)

## Baseline and CI correction

Canonical `main` started at `34eef4c8e7d814e15cb1bfce27df032e75766f19`,
tree `5b9bfa884b4cec86aab293b80ada1b7f73b58d11`, with one worktree,
zero stashes, clean tracked files and index, and 44 exact historical
untracked paths. Live GitHub HEAD and `main` matched the starting commit.
The branch-ref fetch excluded tags and pruning. The inventory verified
all 73 live heads against the fetched refs.

GitHub CI run 43694899733 failed its preparation link check on the
R1 bootstrap Python example. The CI tool interpreted part of the code
line as an inline link with target `raw,form,e`. Commit
`18c95f6cdf082f87bd6113141c7f967abfc82042`, tree
`9f23c66209f54079d97db3056bdded8d2da09472`, changes only the preparation
link extractor and its tests. The scanner passes prose sections to the
existing extraction and reference checks. It excludes backtick and
tilde fenced blocks, using the opening character and length, legal
info strings and line boundaries. It handles LF and CRLF. The exact
R1 evidence, legacy map, original proposal and historical identities
remain untouched. Source hashes, occurrence accounting and the
existing path and line reference checks still apply.

## Historical preservation

The [branch-by-branch manifest](branches.md) records all 99 local and
73 remote-tracking branch refs. It lists each tip and disposition.
The [machine-readable inventory](branch-inventory.json) carries each
divergent tip's merge base, unique commits and paths, changed-file modes
and blobs, and comparison with the starting main tree. Path/blob
equality is corroborating evidence of representation. It supplies
no inference that a different implementation was accepted.

- Local ref classification is one canonical main, 56 contained
  historical branches and 42 divergent branches.
- Remote classification is one canonical main, 46 contained
  historical branches and 26 divergent branches.
- The 56 local and 46 remote contained branches are candidates for
  separate deletion approval after rechecking their tips. Divergent
  branches with copied documentation still hold unique commits.
  Preserve each divergent tip before any later retirement.

This commit adds 66 historical files from 16 remote tips at their
original paths using the exact Git blobs. The [import manifest](import-manifest.json)
binds each path, originating ref and commit, SHA-256 and length.
The complete OpenCode SHA-256 list verified 17 included payloads.
No import overwrites a tracked or an untracked file. Attribute scopes
preserve raw archive bytes and pre-existing historical whitespace.
The newly committed status introductions preserve existing status text.
No old project status, source, executable review probes, solution
configuration or workflow was merged. This is preservation of prior
observations. The consolidation grants no new implementation or
qualification disposition.

| Branch | Preservation disposition |
|---|---|
| `codex/gp-m5-macos-capture` | Retain the original 5-commit native capture candidate and evidence together. The tip changes macOS/core code, tests, scripts and a CI workflow. A future owner can decide whether to archive the complete source-bound package or separately evaluate selected implementation against current main. Preserve the reviewed candidate and its dependency/qualification boundaries. |
| `review/gagamba-rc2a-01-glm-independent` | The verbatim review remains accessible at its remote tip, and its original destination is one of the 44 protected historical untracked paths. Preserve both and keep the old attributes change off main. |
| `review/gagamba-rc2a-02a-glm-q1` | Retain the report, reproduction evidence and independent probe project together on the original ref. Transplanting the report alone would leave its probe reference unresolved. Preserve provenance as a review-only package. |
| `review/wp05-opencode-harness-final-qualification` | Import the report and its exact evidence package. The historical reviewer test in the active test tree remains on the original ref. The preserved review concerns its R12 harness snapshot. |
| Other mixed, conflicting or local-only tips | Keep exact refs and the inventory. The inventory distinguishes bytes already on main, independent historical evidence, candidate source/test changes and infrastructure. Local-only packages remain accessible locally. Future protection for retirement is a separately approved lossless archive of each exact tip, with source trees and original evidence manifests. |

## Verification

- Exact CI preparation command passed on the fixed baseline. The
  preparation regression suite passed 9 tests, including rejects for
  genuine broken links, invalid line citations and escaping paths, and
  historical source hash and occurrence checks.
- Debug and Release solutions built with zero warnings or errors. The
  consolidation introduces no build inputs outside the two preparation
  files, and the new attributes apply to imported evidence paths only.
- The existing M1 regression run reported 1,637 passed, zero failed
  and 333 skipped. Two managed Release reachability tests subsequently
  passed when supplied their expected Release path under the external
  build directory. The 331 inherited native skips remain unqualified.
  The existing Execution domain run passed 68/68 with no failures or skips.
- The production Release verifier returned 0 and PASS. The Debug input
  returned 1 for its Debug configuration and qualification surfaces.
  The committed E-3 metadata probe found zero inbound production
  references in the Debug and Release M1, Owner, Recovery and InstallTool
  assemblies. The managed test positive control found 14 caller methods.
- E-3 source and test files, adopted record and implementation evidence,
  and accepted P4/RELEASE, BB-02, composite Q-E1 and `NATIVE-PENDING`
  boundaries remain in force. No protected native execution was started.

The final publication and exact GitHub CI result are recorded in the
operator handoff. E-3 independent Q1 and owner acceptance remain pending.
