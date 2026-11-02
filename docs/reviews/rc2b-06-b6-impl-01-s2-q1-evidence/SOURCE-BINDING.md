# SOURCE-BINDING.md — independent source and evidence binding

**Review:** GAGAMBA-RC2B-06-B6-IMPL-01-S2-Q1 · **Reviewer:** independent (different agent from the S2 author)
**Date:** 2026-11-02 · **Canonical repo:** `C:/Users/Laszlos/source/repos/Gagamba` · **Branch:** `main`

All identities below were resolved locally with `git rev-parse` / `git rev-list` by the reviewer.
No canonical file, ref, branch, worktree, index or evidence byte was modified. Every build/test
ran in a reviewer-owned copy under `gagamba-b6-s2-q1-review/`.

## 1. Repository identity

| Item | Resolved value |
|---|---|
| Work tree root | `C:/Users/Laszlos/source/repos/Gagamba` |
| Branch | `main` |
| `HEAD` | `c9d867d40e91af2342e85917759af0a6b05538a4` |
| `HEAD^{tree}` | `8bee84d0d8750d59cea21053ebeb0fea1fbf596b` |
| Tracked working tree | clean (0 non-untracked porcelain lines) |
| Remote-tracking `origin/main` | `401e1ea7f0be06b27bc188c827dd951016b3c62a` |
| Local commits ahead of `origin/main` | 6 (`git rev-list --count origin/main..main`) |

## 2. Resolved review subjects

| Role | Commit | Tree | Notes |
|---|---|---|---|
| **Reviewed implementation** | `8b897e4ebb8e53385199083a38086a9713f0e07a` | `af1dc67cf6c74979eb24e9395724b86f264f9a3f` | matches producer report |
| **Evidence / candidate preservation** | `c9d867d40e91af2342e85917759af0a6b05538a4` | `8bee84d0d8750d59cea21053ebeb0fea1fbf596b` | matches producer report |
| First S2 implementation | `35e74fda0c2717e70aeaedc686b45c080b50ae15` | `640d8ce764ea22b00da2214b316a422acbc03b5e` | matches producer report |
| Accepted S1 baseline | `df3d6f0fdb5f45e8fd3d8f53eb983b2ad6bba4fb` | `f4a6519d5b3a2c9c984346e955e63ccb787a6b2e` | ancestor of HEAD (verified) |

The candidate manifest (`source.commit` / `source.tree`) records exactly
`8b897e4…` / `af1dc67…`, so the retained candidate was built from the reviewed implementation
revision.

## 3. Commit chain in the reviewed range

`git rev-list --parents` shows a strictly linear chain:

```
c9d867d → 8b897e4 → 35e74fd → df3d6f0 → 7940622 → …
```

```
df3d6f0fdb5f45e8fd3d8f53eb983b2ad6bba4fb..c9d867d40e91af2342e85917759af0a6b05538a4
  35e74fd  Implement B6-S2 installed generation trust and guarded operator tooling
  8b897e4  Check installed host before qualification and exercise transaction refusals
  c9d867d  Preserve source-bound B6-S2 candidate and hand off independent Q1
```

No unreviewed commit is inside the tested candidate: the candidate's `source.tree` is the tree of
`8b897e4`, and `c9d867d` is its child. The six local commits ahead of `origin/main` are the three
S1 acceptance/evidence commits (`5ad9388`, `7940622`, `df3d6f0`) plus the three S2 commits above.
Nothing has been pushed.

## 4. Evidence-only claim (actual Git diff)

`git diff --numstat 8b897e4 c9d867d`:

```
5    0    docs/PROJECT-STATUS.md
116  0    docs/evidence/rc2b-06-b6-impl-01-s2.md
1    0    docs/evidence/rc2b-06-b6-impl-01-s2/candidate-manifest.json
-    -    docs/evidence/rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz
-    -    docs/evidence/rc2b-06-b6-impl-01-s2/verification.zip
5    0    docs/handoff.md
5    0    docs/work-queue.md
```

The preservation commit changed **only documentation and captured evidence**. It changed no
production source, test, build input, `.gitattributes` rule or candidate-producing tool. This is
demonstrated by the diff itself, not by the producer's characterization.

The candidate-producing source itself: `35e74fd` created `src/…/Installed/*`, `eng/InstallTool/*`,
`eng/linux-m1/*`; `8b897e4` modified `eng/InstallTool/Program.cs`, `eng/linux-m1/install_common.py`,
`eng/linux-m1/tests/test_install_common.py`, `eng/test-production-regressions.ps1`,
`src/…/Installed/{InstalledArtifactInspection,InstalledFileSystem,InstalledPayload,InstalledPlatform}.cs`,
and `tests/…/Installation/InstalledMetadataTests.cs`. The candidate was built **at** `8b897e4`
(its manifest names that commit/tree), so no candidate-producing source changed after the candidate
was built.

## 5. Preserved untracked paths

The producer's recovery inventory `recovery/canonical-status-porcelain-v2.txt` lists 28 untracked
top-level entries (`untracked-paths.txt` expands them to 44 individual paths). The current
`git status --porcelain` untracked set is **identical** to that inventory:

```
current count: 28   producer count: 28   Compare-Object differences: none (only quoting)
```

Tracked working tree remains clean. The reviewer created nothing in the canonical checkout.

## 6. Interaction with the accepted S1 baseline

`git merge-base --is-ancestor df3d6f0 c9d867d` → true. The S1 acceptance receipt (`df3d6f0`) is a
direct ancestor of the reviewed implementation. `git diff --name-only df3d6f0..c9d867d` touches
only `.gitattributes`, `eng/`, `docs/`, `src/…/Installed/`, and `tests/…/Installation|Installed/`;
it does **not** touch `src/Gagamba.Runtime/`, `src/Gagamba.Execution/Restricted/`,
`src/…/Bootstrap/`, `src/…/Lifecycle/` or `src/…/Native/`.

## 7. Verdict

**Source binding supports review.** The reviewed implementation, the preserved candidate, and the
preservation commit resolve exactly to the producer-reported SHAs/trees; the preservation commit is
evidence-only; the accepted S1 baseline is an ancestor; all 44 pre-existing untracked paths are
preserved; `main` is six commits ahead of `origin/main` with no push. No source mismatch was found,
so qualification claims may proceed.
