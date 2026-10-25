# RC2A-02B qualification matrix (planned vs executed)

Date: 2026-10-25. Start commit `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`
(tree `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e`). Authoritative requirements:
accepted RC2A-02 plan §3 D (`Q2A-P`) and §9.1; owner acceptance of RC2A-02A
(`e5fff9e`); RC2A02A-GLM-F04 disposition (Q2A-S deferred and OPEN).

## 1. Plan requirement coverage

| Requirement | Obligation | Executed evidence | Status |
|---|---|---|---|
| §3 P1 | Exact commands, filters, SDK/runtime, results directory and artifact names recorded | `results.json` (commands), `runs/windows/windows-environment.txt`, `runs/wsl2/wsl2-environment.txt`, raw logs/TRX/exit files, `run-wsl2.sh` | PASS |
| §3 P1 | Release build of `src/Gagamba.Execution`, warnings as errors | Windows build 0/0 (`runs/windows/build-execution.log`); WSL2 build 0/0 (`runs/wsl2/wsl2-build-execution.log`) | PASS |
| §3 P1 | Pure tests only; no native fixture/owner/helper/workload started | Precise filter excludes the five Linux-native classes; no native process or fixture started; Windows literal-filter run's only native-class outcomes are pre-existing skips | PASS (scope decision recorded) |
| §3 P2 | Windows primary re-run on the qualification branch | Windows W1–W3 | PASS |
| §3 P2 | Linux executed natively when a host is available; WSL2 labeled `wsl2`, never native | No native Linux host available → not run, recorded; WSL2 executed and labeled `wsl2` | PARTIAL (honest: native Linux not run) |
| §3 P2 | macOS labeled with exact host/architecture; not run recorded with reason | Not run: no macOS host; recorded in `results.json`/README | NOT RUN (recorded) |
| §3 P2 | Unrun platform is neither pass nor failure; no implied success | Stated in README §3/§6 and `results.json` | PASS |
| §3 P3 | Per-platform artifacts and summary; failures not averaged away | `runs/windows/`, `runs/wsl2/`, `results.json`; no failures occurred; WSL2 setup failures preserved separately | PASS |
| §3 P4 | Pure managed-code portability only; no native confinement/cgroup/sandbox claim | Scope statements; no such claim; no such workload started | PASS |
| §3 F1/F2 | Independent review after execution | **NOT YET** — handoff prepared; review is the next gate | PENDING (expected) |
| §9.1.1 | Separate explicit owner authorization | This assignment ("Proceed with RC2A-02B") | SATISFIED |
| §9.1.2 | Dedicated branch/worktree from the accepted tip; identities recorded | Branch `qualification/gagamba-rc2a-02b` from `e5fff9e`; `source-binding.json` | SATISFIED |
| §9.1.3 | Q2A-S / RC-D04 disposition confirmed for this stage | Confirmed NOT assigned to RC2A-02B by the plan; remains OPEN (F04) | SATISFIED (preserved OPEN) |
| §9.1.4 | Host availability established per platform | Windows available; WSL2 available; native Linux and macOS unavailable (recorded) | SATISFIED |
| §9.1.5 | §3 P1 run definitions executed with provenance | Windows and WSL2 runs with exit codes, TRX, logs, hashes, environment captures | PASS |
| §9.1.6 | Separate per-platform evidence with per-platform summary | `runs/windows`, `runs/wsl2`, README §3 | PASS |
| §9.1.7 | §3 P4 scope boundaries maintained | README §6; no native/package/deployment claim | PASS |
| §9.1.8 | Qualified assertions re-executed per declared platform (not inferred) | Same 457 test-name/outcome multiset executed on Windows and WSL2; same for 67 public tests | PASS |
| §9.1.9 | Independent review exit after execution | Pending; handoff at `reviewer-handoff.md` | PENDING (expected) |

## 2. Executed case families per platform

| Family | Cases | Windows native | WSL2 |
|---|---:|---|---|
| Public contract suite (existing 57 + RC2A-02 boundary 10) | 67 | 67 passed, 0 failed, 0 skipped | 67 passed, 0 failed, 0 skipped |
| Q2A-F field mutation matrix (279 cases + 5 matrix checks) | 284 | 284 passed | 284 passed |
| Q2A-V A5 refusal catalog (78 + 1 catalog check) | 79 | 79 passed | 79 passed |
| Q2A-V vector theory + set checks | 27 | 27 passed | 27 passed |
| Q2A-B boundary/sweeps | 20 | 20 passed | 20 passed |
| Q2A-E evidence exports | 2 | 2 passed | 2 passed |
| Q2A-Harness self-tests | 7 | 7 passed | 7 passed |
| RC2A-01 internal regressions (Manifest/Projection/PreparationIdentity) | 38 | 38 passed | 38 passed |
| **Required set total** | **457** | **457 passed, 0 failed, 0 skipped** | **457 passed, 0 failed, 0 skipped** |
| Rerun (deterministic) | 457 | 457/0/0, identical multiset | 457/0/0, identical multiset |
| Literal committed-plan filter (provenance only) | 514 | 473/0/41 (pre-existing Linux-native skips) | not run (selects native classes; scope decision) |

## 3. Cross-platform comparison

- Windows required set vs WSL2 required set: identical test-name/outcome
  multiset (457 entries).
- Windows public vs WSL2 public: identical multiset (67 entries).
- Windows rerun and WSL2 rerun: identical to their first runs.
- No failures on any executed platform; no RC2A-02A/02B case skipped.

## 4. Not executed and why

| Item | Reason |
|---|---|
| Native Linux qualification | No native Linux host accessible (no SSH host/config); WSL2 is not a native host and is labeled separately |
| macOS qualification | No macOS host |
| Literal plan filter on WSL2 | It selects pre-existing Linux-native/root-dependent classes, contrary to §3 P1 (pure tests only) and §3 P4 (managed-code scope); F02 context |
| Q2A-S / RC-D04 | Not assigned to RC2A-02B by the accepted plan; remains OPEN |
| Native confinement, package, clean consumer, mixed graph, Hufu | Outside RC2A-02B scope; unchanged obligations |

## 5. Status

**RC2A-02B IMPLEMENTED / QUALIFIED LOCALLY (WINDOWS + WSL2) / NATIVE LINUX AND
macOS NOT RUN / READY FOR INDEPENDENT REVIEW.** Not independently reviewed; not
owner accepted; full RC2A-02 not accepted.
