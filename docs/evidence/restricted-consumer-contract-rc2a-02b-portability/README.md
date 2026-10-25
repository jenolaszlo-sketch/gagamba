# RC2A-02B portability qualification evidence

Date: 2026-10-25 (Asia/Manila).
Status: **IMPLEMENTED / QUALIFIED LOCALLY (WINDOWS + WSL2) / NATIVE LINUX AND
macOS NOT RUN / READY FOR INDEPENDENT REVIEW.**
Independent review has **not** occurred for RC2A-02B; owner acceptance has
**not** been recorded for RC2A-02B. Full RC2A-02 remains **NOT YET ACCEPTED**.

## 1. Scope

RC2A-02B is the portability qualification stage defined by the accepted RC2A-02
specification §3 D (`Q2A-P`): reproducible run definitions, platform honesty
rules, separate per-platform evidence, and pure managed-code portability scope.
Its entry criteria are §9.1 of the same plan.

In scope and executed:

- Release build of `src/Gagamba.Execution` with warnings as errors.
- The Windows public suite and the precise required qualification set
  (RC2A-01 regressions + `Tests.Qualification.`), with a deterministic rerun.
- The same suites on a second Linux-kernel environment available to this
  machine: **WSL2 Ubuntu**, always labeled `wsl2`, never `native Linux`.
- The literal committed-plan filter on Windows only, for provenance.

Out of scope and preserved as open:

- **Q2A-S / RC-D04** — the plan does not assign it to RC2A-02B; it remains an
  open obligation of full RC2A-02 acceptance (RC2A02A-GLM-F04).
- Native Linux and macOS qualification — **not run**, no host available.
- Native confinement, cgroups, sandbox, package, clean-consumer, mixed-graph,
  Hufu (AE4-B01), publication, binary reproducibility.

## 2. Bindings

| Item | Exact identity |
|---|---|
| Start commit (accepted baseline) | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`, tree `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` |
| Accepted qualification evidence | `c1a01761f8d58136a381ad87269635c6c14ad093`, tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19` |
| Tested implementation | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` |
| Independent RC2A-02A review | `7b6344551c2d4b42832df39822eaf4b8a1c01495` (preserved, unmerged) |
| `src` / `tests` / `native` at baseline | `b8c167e3d7340a562221591bdc2b6df7adb372d1` / `5c3268286575cb55891fae9288627657f2d30ddf` / `85631d2f04512376621d3d09f51d67fd90f45335` |
| Qualification branch / worktree | `qualification/gagamba-rc2a-02b` / `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02b` |
| Windows executed source | worktree HEAD `e5fff9e…`, clean at execution start |
| WSL2 executed source | local clone checked out at `e5fff9e…` (clean); origin URL set to the canonical GitHub URL for SourceLink only |
| Evidence commit | documentation-only commit on the qualification branch; identity recorded in the completion report |

The start commit is the owner-acceptance tip, a direct child of the accepted
qualification evidence commit (the plan's §9.1 rule permits a later accepted tip
designated by the owner; the current assignment designates this baseline).
`src`, `tests` and `native` are identical between `e5fff9e` and `c1a01761`; the
qualification therefore executes the same tested implementation and test tree.

## 3. Results

| Platform | Build | Public suite | Required set | Rerun | Notes |
|---|---|---:|---:|---:|---|
| Windows 11 native, AMD64 | 0 warnings / 0 errors | 67 / 0 / 0 | 457 / 0 / 0 | 457 / 0 / 0 | broad filter (provenance only): 473 / 0 / 41 |
| WSL2 Ubuntu 26.04.1 (labeled `wsl2`) | 0 warnings / 0 errors | 67 / 0 / 0 | 457 / 0 / 0 | 457 / 0 / 0 | broad plan filter not run (selects native classes) |
| Native Linux | not run | — | — | — | no host available |
| macOS | not run | — | — | — | no host available |

Cross-platform comparison: the Windows and WSL2 public suites have identical
test-name/outcome multisets (67/67), and the required sets have identical
multisets (457/457). Reruns on each platform are also identical. The 41 Windows
skips are the same three pre-existing Linux-native classes recorded in
RC2A02A-F02 (`NonRootQualificationTests` 31, `OwnerDeathQualificationTests` 7,
`Wp05ReleasePathIndependentQualificationTests` 3); none is an RC2A-02A/02B case.
Beyond the 457 tests selected by the precise filter, the broad filter selects
five classes: `CoordinatorProtocolQualificationTests` (11 passed) and
`Wp05V3IndependentQualificationTests` (5 passed) are managed-code classes;
`NonRootQualificationTests` (31 skipped), `OwnerDeathQualificationTests`
(7 skipped), and `Wp05ReleasePathIndependentQualificationTests` (3 skipped)
are Linux-native classes. The Windows broad-filter total is **473 passed / 0
failed / 41 skipped** (514 total). This run is provenance evidence only.

Machine-readable detail, exact commands and artifact hashes:
[`results.json`](results.json). Raw logs, TRX files, exit captures and
environment captures: [`runs/windows/`](runs/windows/) and
[`runs/wsl2/`](runs/wsl2/).

## 4. Execution-scope decisions (explicit)

1. **Precise filter for qualification evidence on every platform.** The literal
   broad filter `FullyQualifiedName~Qualification` adds five classes beyond the
   precise selection. Two managed-code classes execute and pass on Windows
   (16 tests total); three Linux-native classes identified by Q2A02A-F02
   contribute 41 skips there. On Linux, those native/root-dependent classes
   would execute, which §3 P1 (pure tests only) and §3 P4 (pure managed-code
   scope) exclude. The precise `Tests.Qualification.` filter remains the
   qualification selection mechanism on each platform. The broad filter is run
   on Windows for provenance only and is not a substitute for the precise set.
2. **WSL2 is not native Linux.** All WSL2 results are labeled `wsl2`; no native
   Linux qualification is claimed. No WSL2 result substitutes for a native Linux
   run.
3. **Start baseline.** `e5fff9e` (owner-acceptance tip) is used as the accepted
   baseline; the plan permits a later accepted tip. The tested implementation and
   test tree are unchanged from `71a28afe`/`c1a01761`.
4. **Environment setup, not qualification.** Two WSL2 build attempts failed
   before the successful run and are preserved under `runs/wsl2/`
   (`setup-attempt1-archive-sourcelink.*`, `setup-attempt2-local-origin.*`).
   Both are SourceLink metadata failures (an archive without Git metadata; a
   local-path origin URL), resolved by executing from a clone checked out at the
   accepted commit with the canonical repository URL configured. They are **not**
   counted as qualification runs and do not indicate product defects.
5. **Documentation-text correction.** The qualifying plan's appended §9/§10 text
   (written by shell appends in earlier stages) contained invalid single-byte
   punctuation bytes for `§` and `—`. This update repairs them to valid UTF-8 and
   fixes one identifier typo (`R2A-A01` → `RC2A-02A`). No requirement, result or
   acceptance statement changes; historical evidence and the independent review
   are untouched.

## 5. Status vocabulary (not interchangeable)

| Status | RC2A-02B state |
|---|---|
| Implemented | Yes — run definitions executed and evidence committed; no source change was required |
| Tested | Yes — Windows native and WSL2 executed the public and required sets |
| Supported by evidence | Yes — for the executed platforms and managed-code scope |
| Qualified | Locally only (Windows + WSL2); **not** qualified across native Linux/macOS |
| Independently reviewed | **No** — this is the next gate |
| Owner accepted | **No** for RC2A-02B (RC2A-02A remains the accepted qualification stage) |

## 6. Limitations and non-claims

- Native Linux and macOS were not run; per plan §3 P2 this is neither a pass nor
  a failure, and no cross-platform success is implied beyond the executed
  environments.
- WSL2 shares the Windows kernel host; it is a Linux userland under WSL2, not a
  native Linux host or a substitute for one.
- SDK/runtime patch levels differ between platforms (Windows SDK 10.0.401 /
  runtime 10.0.12; WSL2 SDK 10.0.113 / runtime 10.0.13). Test outcomes and test
  names are identical; no byte-identical build or binary-reproducibility claim is
  made (R2A-F03 disclosure retained).
- These are pure managed-code value/codec tests. No native fixture, restricted
  owner, helper, sandbox, workload, package build, clean consumer or Hufu graph
  was exercised. Native confinement is not qualified.
- Q2A-S / RC-D04 remains OPEN (not part of this stage).
- Full RC2A-02 is NOT YET ACCEPTED; RC2A-03/04 and all other gates are unchanged.

## 7. Evidence integrity and reproduction

- `source-binding.json` binds the start/target commits, subtrees and executed
  source for each platform.
- `document-sha256.json` binds the changed documents by LF-normalized SHA-256.
- `SHA256SUMS` covers every artifact in this evidence directory except itself.
- `run-wsl2.sh` is the exact script executed for the WSL2 runs.
- Reproduction (Windows): from a clean worktree at the start commit, run the
  restore/build/test commands in `results.json` in order.
- Reproduction (WSL2): clone the canonical repository, check out the start
  commit, set `origin` to a valid repository URL, then run `run-wsl2.sh`
  (paths in the script are the ones used for this evidence).
