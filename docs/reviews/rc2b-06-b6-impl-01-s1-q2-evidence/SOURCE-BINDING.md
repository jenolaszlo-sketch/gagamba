# SOURCE-BINDING.md — Git identities, ancestry, remote and CI binding

All observations below were independently executed by the Q2 reviewer. `git -C` read-only operations only; no fetch, branch, worktree, push or modification of the canonical repository.

## 1. Commit identities (verified from Git, not from the commission table)

| Role | Commit | Tree | Verified |
|---|---|---|---|
| Original S1 implementation | `1cbe4dfe9c4d19aba8708079b2f617b2ddafcbc0` | `cb178340c21a178914a4233c9e3ecc5a2d9b23ff` | ✔ `git rev-parse 1cbe4df…^{tree}` |
| Remediation implementation | `66896754de2610b1610c665f73d3cc0f88675641` | `62613e8e407eca25c9d0438d041a89d8e9c5b9fa` | ✔ |
| Intermediate remediation (producer-disclosed, superseded) | `635a01212440fe1df680e3231214594598e0fa74` | `a1340ab5d87a867243e3f587a19c6219361efbfc` | ✔ |
| Final remediation receipt | `dce71ac2423c2a49a853bef8703018f0220378cb` | `becec3f768266be158fb57396f00f328408cd30e` | ✔ (HEAD at review start) |

All expected identities match the commission table exactly.

## 2. Ancestry and intermediate sequence

- Canonical branch: `main`. ✔
- `git merge-base --is-ancestor 66896754 dce71ac` → true. ✔
- Complete sequence `1cbe4df..dce71ac` (3 commits, all accounted for):
  1. `635a012` *build(gagamba): remediate S1 production and test variant isolation* (producer-disclosed as intermediate/superseded)
  2. `6689675` *build(gagamba): preserve instrumented variants across solution references* (final implementation)
  3. `dce71ac` *docs(gagamba): bind S1 remediation evidence and prepare independent Q2* (receipt)
- **Receipt changes no runtime input:** `git diff --name-status 66896754..dce71ac` = 8 files, all under `docs/` (PROJECT-STATUS, evidence report/manifests, handoff, work-queue). ✔
- Implementation diff scope (`1cbe4df..6689675`, 22 files): `Directory.Build.props` (8±), `Directory.Build.targets` (+66), 3 production csproj, 2 test csproj, new `eng/VerifyProductionAssembly/` (3 files), new `eng/test-production-build.ps1`, `eng/test-production-regressions.ps1`, docs/evidence. **No `src/**/*.cs` and no `tests/**/*.cs` changed** (only `.cs` in the whole range is the new `eng/VerifyProductionAssembly/Program.cs`). ✔

## 3. Remote binding (observed twice; remote moved during the review window)

| Observation time (review session) | `origin/main` | Meaning |
|---|---|---|
| Review start | `1cbe4dfe9c4d19aba8708079b2f617b2ddafcbc0` | Only original S1 pushed at that moment |
| After Gate F | `401e1ea7f0be06b27bc188c827dd951016b3c62a` | Owner push completed |

`401e1ea7` ancestry (verified via GitHub API, no local fetch): `401e1ea7` → `dce71ac` → `66896754` → `635a012` → `1cbe4df` → `24ac359`. Compare API: `dce71ac…401e1ea7` = ahead/1.

**The final remediation receipt `dce71ac` and implementation `66896754` are in remote `main` ancestry.** Remote main does not point *exactly* at the receipt; it points one commit beyond it:

- `401e1ea7` *fix(gagamba): preserve historical preparation bytes on checkout* — files: `.gitattributes`, `docs/PROJECT-STATUS.md`, `docs/evidence/ci-preparation-checkout-byte-identity-2026-11-02.md`, `docs/handoff.md`, `docs/work-queue.md`, `eng/tests/test_preparation_checkout.py` (API file list). **Docs/harness only; no production, test, or build-boundary source.** Not reviewed by Q2 (post-receipt owner commit).

Classification: the intended revision is available remotely (in ancestry), so this is **not** "REMOTE SOURCE BINDING PENDING"; it is recorded as a **remote-tip delta observation** requiring owner disposition at acceptance (fold `401e1ea7` into scope, or bind acceptance to `dce71ac`).

No push was performed by the reviewer.

## 4. CI binding (GitHub Actions, repo jenolaszlo-sketch/gagamba)

| Run | Head commit | Workflow | Conclusion | Relevant jobs |
|---|---|---|---|---|
| 41311797002 | `1cbe4df` (original S1) | ci | **failure** | Matches Q1-FINDING-001 (Release build failure) |
| 41325323331 | `dce71ac` (receipt) | ci | **failure** | 9/10 jobs green incl. `build (windows-latest)`, `build (ubuntu-latest)`, `unit tests (windows-latest)`, `unit tests (ubuntu-latest)`, pack, fixture self-test, GM-1A/GM-2 macOS; **only `preparation` failed** at step "Verify preparation baseline" (fixture/preparation byte-identity check, not the build boundary) |
| 41327109151 | `401e1ea7` (tip) | ci | **success** | All jobs green |

Determination: CI at the remediation receipt exercises the complete Release solution build on Windows and Ubuntu **successfully** (both build jobs + both unit-test jobs green). The single red job concerns preparation/checkout byte-identity of historical evidence/fixture files and was fixed by the owner's follow-up commit; the tip run is green. CI does not itself exercise the D6.4 fixture variant or the reviewer's contradiction matrix; those remain covered by this independent local review. CI is corroboration, not substitution.

## 5. Local custody

- Review-start canonical HEAD: `dce71ac` (clean tracked tree; 28 pre-existing untracked entries; 74 worktree entries incl. canonical). Review-end canonical HEAD: `401e1ea7` (owner pull during review window; still 0 tracked modifications; 74 worktrees; untracked inventory unchanged). Snapshots: `evidence/canonical-status-final.txt`, `evidence/worktrees-final.txt`.
- Reviewed source materialized via `git archive` (no worktree/branch) to `src-original` (`1cbe4df`) and `src-remediated` (`6689675`) inside the isolated review directory; all builds/tests ran there.
- Q1 evidence preserved: all 16 hashes in the producer's `docs/evidence/rc2b-06-b6-impl-01-s1-rem-01/q1-evidence.sha256` re-verified byte-identical against `C:\Users\Laszlos\source\repos\Solo\b6-impl-01-s1-q1-review` (REPORT, SOURCE-BINDING, BUILD-ISOLATION, BINARY-INSPECTION, HOOK-INVENTORY, REGRESSION-RESULTS, SAFETY-PRESERVATION, T24-QUALIFICATION, DEPENDENCY-CLOSURE, FINDINGS, OWNER-HANDOFF, FINAL-HANDOFF, InspectHooks.cs, probes×2, evidence/MANIFEST.md).
- 74 historical worktrees: verified unchanged (`evidence/worktrees-final.txt`).
