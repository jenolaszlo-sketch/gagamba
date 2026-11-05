# GAGAMBA-WORKTREE-CONSOLIDATION-01 — consolidation evidence record

Commission: GAGAMBA-WORKTREE-CONSOLIDATION-01 (Mechanism B qualification hold and
single-main consolidation). Executor: GLM Flash in OpenCode, 2026-11-05. Disposition
record: [PROJECT-STATUS.md](../PROJECT-STATUS.md) (owner commits `936cb22`, `d037842`,
`cdf577a`). This record binds the verified evidence; the external recovery package is
authoritative for machine-readable detail.

## 1. Starting conditions (verified ~20:45–20:52 +0800)

- Canonical HEAD `fc710b0e1ed0b4d2f0aba9e477d6bfbf7d5f882e` (tree `3079bf79…`), exactly
  the reported Mechanism B implementation HEAD; all three Mechanism B commits
  (`4aab8776`, `831c980e`, `fc710b0e`) reachable.
- Tracked/staged clean; exactly 44 historical untracked paths; no stash; 74 registered
  worktrees; 99 branches; no index lock.
- Both Q1 review packages read and their SHA-256 manifests byte-verified (20/20 and
  17/17 entries OK): `Solo/gagamba-f02-mechanism-b-impl-01-q1` (INCONCLUSIVE) and
  `Solo/gagamba-f02-mechanism-b-impl-01-q1-cont` (Q1 BLOCKED BY ENVIRONMENT).

## 2. Worktree inventory and classification (twice-verified, identical invariants)

74 registered worktrees (canonical + 73 linked); all paths existed; zero dirty
tracked/staged; zero untracked outside the canonical checkout's 44; no locked,
detached, prunable or stale entries; no rebase/merge/cherry-pick/bisect in progress;
every worktree HEAD equal to its branch tip.

- 44 linked worktrees: tips already ancestors of main (integrated).
- 29 linked worktrees: tips ahead of main — 56 unique commits (union, worktree-tip
  scope). All documentation/review/evidence except one NEM-012 characterization test;
  **zero production (`src/`, `native/`) commits**.

## 3. Preservation before retirement

External recovery package (uniquely named so no other agent can overwrite it):
`C:\Users\Laszlos\source\repos\Solo\gagamba-worktree-consolidation-01-evidence-glm-20261105T130733Z`
(73-entry `SHA256SUMS.txt`). Contains: complete worktree inventories (both runs),
56-commit unique inventory with per-commit branch proofs, all-refs 107-commit inventory,
44-file untracked snapshot with SHA-256, verified `git bundle --all` archives at
`fc710b0e` and `d037842` (mirror-clone recovery proven: 99/99 branches, all unique
commits restored), supersession analysis, and the concurrent-agent incident archive.

## 4. Concurrent-agent incident (documented, resolved)

A second agent (OpenAI Codex runtime) executed the same commission 20:47–20:57,
overwrote the first inventory in the commission-named directory, and produced a
broken first inventory (`mainHead: "f"`, `uniqueCommitCount: 0`, all 74 worktrees
flagged removable). The owner stopped it; the audit found no modification of the
Gagamba repository itself. See `codex-incident/` in the recovery package.

## 5. Ownership transition and removal

The owner committed the Q1 closeout (`936cb22`) and the single-canonical-checkout
policy (`d037842`), then removed all 73 linked worktrees and recorded completion
(`cdf577a`). Executor pre-removal protection (bundles + branch proofs) was verified
before the removal; post-removal verification found: 99 branches intact, all 56
worktip-unique commits resolvable and branch-protected, 44 untracked paths intact,
no stash, single canonical worktree.

## 6. Integration decision: NONE

All-refs scope: 107 unique commits across 42 branch tips; zero production commits;
9 test/qualification-era commits and 10 spike/tooling commits, each superseded by
main's later evolution (main carries larger evolved Wp05 suites; the Oct-18 C#
`qualification/` tooling was replaced by the Python `eng/` tooling; NEM-era
characterization guards belong to a closed era). Per commission rules, unique
commits were not automatically merged and superseded era code was not reintroduced.
All unique history remains on surviving branch refs and in verified bundles.

## 7. Verification at `cdf577a` (executor-run)

- `dotnet build Gagamba.sln -c Debug`: **0 warnings / 0 errors**.
- Focused `FinalAcceptanceTests`: **69 / 69**, 0 failed, 0 skipped.
- Full Windows M1 runner: **1257 passed / 331 skipped / 0 failed (1588 total)** —
  identical to producer and independent-review counts. Native skips are not passes.

## 8. Final state

One canonical checkout on `main` at `cdf577a43fd63d8661d866abbc2916fdfc39b41d`;
99 branches preserved (none deleted); 44 historical untracked paths preserved
in place and snapshotted; no push, no history rewrite, no force operations, no
prune candidates; worktree registry empty except the canonical checkout.

## 9. Follow-ups (unchanged from the Q1 disposition)

Native Mechanism B qualification remains a separate future security milestone:
trusted root-owned installation with cgroup delegation on a disposable host, the
unexecuted native positive/adversarial B2/B3 lifecycle cases, the complete
sink-coverage assessment of the internal `RecoveryConnection.Start` /
`QualifiedRecoveryConnection.Start` initialization route, and Q10's pre-RELEASE
active-generation reread before any affected native RELEASE qualification or
production activation. Q1 is not marked supported; Q10 remains open.
