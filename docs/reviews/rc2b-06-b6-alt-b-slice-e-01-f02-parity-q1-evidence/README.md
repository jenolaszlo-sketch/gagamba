# F-02-PARITY-Q1 evidence index

Independent qualification of `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01`.
Report: [`../rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1.md`](../rc2b-06-b6-alt-b-slice-e-01-f02-parity-q1.md)

Environment: Windows (win32), .NET 10.0, canonical checkout `C:\Users\Laszlos\source\repos\Gagamba`
on `main` at `d221b24be3d19c775c5118e419b2e88769fea2ce`; one worktree; no push.
All mutations and disposable assemblies were built in external scratch
(`C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-q1`, deleted after extraction);
the canonical checkout was never mutated.

| File | Content |
|---|---|
| `baseline.txt` | Verified starting HEAD/tree/branch/worktree/untracked/remote state. |
| `il-probe.cs` | Reviewer-owned IL probe (calls / ilhash / callers / exports / info modes). |
| `reconstruction-calls.txt` | Independent IL call reconstruction of both verification methods (Debug). |
| `reachability-and-identity.txt` | Caller scans (Debug + Release), exported types, assembly identities, method IL hashes. |
| `mutation-matrix.txt` | Reviewer mutation matrix Q0, R-01..R-11, R-10b, CP-01..CP-04 with summaries/failures/assertions. |
| `comment-strip-and-bypass.txt` | Comment-only IL equivalence proof and call-preserving bypass findings. |
| `production-diff-and-identity.txt` | Comment-only diff proof, blob/file identities, commit-binding identity analysis. |
| `regressions.txt` | Independent regression counts, builds, verifier results. |
| `run-q1-mutations.ps1`, `run-q1-part2.ps1` | Reviewer mutation harness (ordered runs and part 2). |
| `commands.txt` | Exact commands for every evidence item. |

## Detection categories (do not merge)

- **Detected by the intended parity assertion**: R-01..R-09, R-10b, R-11 (each failed the exact
  expected assertion; see `mutation-matrix.txt`).
- **Detected by a different existing test**: CP-04 (guarded F-01 semantic `Require` block caught by
  the E-1/F-01 projection class, not by the parity suite).
- **Rejected by compilation**: none of the final runs (R-10 was invalid/inconclusive).
- **Invalid or inconclusive**: R-10 (reviewer swap did not compile, `CS0841`; superseded by R-10b).
- **Not detected**: CP-01, CP-02, CP-03 (call-preserving conditional bypasses; branch dominance is
  outside the accepted Option B guarantee).
- **False positives**: none observed (legitimate accepted implementation passes all controls).

## Status

F-02 remains **open**. This qualification supports the bounded structural/ordering control role
with explicit qualifications (M-09 class, branch dominance, native differential parity); only a
later owner decision may close the finding.
