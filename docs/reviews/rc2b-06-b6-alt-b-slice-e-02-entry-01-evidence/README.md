# Slice E-2 entry assessment — evidence index

This directory supports [the Slice E-2 entry assessment](../rc2b-06-b6-alt-b-slice-e-02-entry-01.md).

- **Commission:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-01`
- **Date:** 2026-11-09 (Asia/Manila)
- **Baseline HEAD:** `e86254e60fb3ded770156519e1d3e15e73bf36fc`
- **Baseline tree:** `69a18e8c3a1e199802f4cfc1906de2a30e3a7928`
- **Formal disposition:** `RECONCILIATION REQUIRED`

## What this evidence establishes

- The exact baseline and the preservation of the 44 historical untracked paths and their bytes.
- SHA-256 identities for the committed records used by the assessment.
- SHA-256 identities and acceptance bindings for the locally available external frozen inputs.
- The K/M/F/S/P/R/Wc role map and the acyclic catalog approval graph, as reconstructed from the accepted external contract.
- The current source and authority state, including E-1 production-unreachability.
- The in-repository provenance gap for Q-E1 and the missing owner decision for Q-E2.
- The P4 and protected-native boundaries that E-2 must preserve.

## Files

| File | Contents |
|---|---|
| `baseline-and-preservation.md` | Baseline table, 44-path preservation comparison, inventory digest method |
| `record-hashes.txt` | SHA-256 and size for each committed record used by the assessment |
| `external-input-hashes.txt` | SHA-256, size, and acceptance binding for each local external frozen input |
| `frozen-schema-role-map.md` | K/M/F/S/P/R/Wc roles, exact sources, and the acyclic graph |
| `source-and-authority-state.txt` | Source searches, call graph, parser version, and tracked-source diff |
| `in-repo-provenance-gap.txt` | Exact evidence that no in-repository K/M/F/S/R/Wc/P schema or approval decision exists |
| `p4-and-native-boundary.md` | P4 classifications and protected-native qualifications preserved by E-2 |

## Method

All checks were read-only local operations:

- `git rev-parse`, `git status`, `git worktree list`, `git log`, `git diff`, `git grep`, and `git ls-files`.
- PowerShell `Get-FileHash -Algorithm SHA256` for file identities.
- Manual inspection of the committed records and the locally available external inputs.

The external inputs are under `C:\Users\Laszlos\source\repos\Solo\`. They are not part of the Gagamba repository. They were inspected only to confirm that their bytes match hashes recorded in committed Gagamba decisions. This evidence does not treat them as in-repository authority.

## Reproduction outline

From `C:\Users\Laszlos\source\repos\Gagamba`:

```powershell
git rev-parse HEAD
git rev-parse 'HEAD^{tree}'
git branch --show-current
git worktree list
git status --porcelain -uall
git rev-parse origin/main
git rev-list --count origin/main..main
```

For the record hashes:

```powershell
$files = Get-Content docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/record-hashes.txt |
    Where-Object { $_ -and -not $_.StartsWith('#') }
foreach ($line in $files) {
    $path = ($line -split "`t")[2]
    $hash = (Get-FileHash $path -Algorithm SHA256).Hash.ToLower()
    "$hash`t$path"
}
```

For the external inputs, use the paths and hashes in `external-input-hashes.txt`.

## Limitations

- No build, test suite, native probe, protected acquisition, provisioning, or execution was performed.
- No external input was authenticated beyond SHA-256 comparison to a hash recorded by a committed decision.
- No native protected positive, privilege drop, SCM/pidfd custody, GAVA1, v3-pre, or Slice N behavior is qualified.
- No behavioral equivalence or branch dominance is claimed for the two verification paths.
- The assessment does not create a schema, catalog, approval, trust anchor, capability, or authority.
- The historical PENDING captions in external documents remain untouched. Later committed decisions govern the semantics they expressly accept.

## Preservation

The 44 historical untracked paths were verified present with matching per-file SHA-256 values; `diff_count=0`. They were not cleaned, deleted, rewritten, or staged. The assessment and evidence files are additive documentation paths.

No branch, extra worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.
