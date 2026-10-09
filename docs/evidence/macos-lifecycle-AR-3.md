# AR-3 macOS lifecycle and helper evidence

Date: 2026-10-09 (Asia/Manila)  
Slice: AR-3  
Baseline SHA: `1e6273b58de7bf92858a0701c62abe55a3239a9d`  
Tested source SHA: `4735ac55a77e5121e04a8ba0a23112daeca1ad8e`  
Branch: `main`; source checkpoint working tree: clean after commit. This evidence and handoff update follow as a documentation commit.  
Windows host: Windows 10.0.26200, x64, .NET SDK 10.0.401 and runtime 10.0.12.  
Unix helper-test host: Ubuntu 26.04.1 LTS under WSL2, Linux 6.6.114.1-microsoft-standard-WSL2, x86-64, .NET SDK 10.0.112 and runtime 10.0.12.  
Native macOS prerequisites: **unavailable on these hosts**. `/bin/launchctl`, a logged-in GUI domain and native launchd were not exercised at the tested revision. Historical GM-2 macOS CI evidence predates this repair and cannot qualify it.

Status: **AR-3 IMPLEMENTED — NATIVE QUALIFICATION PENDING**. Locally tested means parser, fake launchd and Unix helper-process tests passed. Native-qualified: no. Package-qualified: no. Release-qualified: no. The macOS capability remains Partial same-process-group lifecycle behavior; `setsid` escape remains known and uncontained. Hostile-workload filesystem, network, IPC and identity restrictions remain outside this slice.

## Baseline to fix

| Finding | Baseline observation | Repair | Fixed observation |
| --- | --- | --- | --- |
| F07 helper path/deadline | Source inspection at baseline: ambient `PATH` resolved `launchctl`; stdout was read synchronously to EOF before stderr and before the timeout; displayed output was truncated after collection. No new same-test baseline execution was run. | Absolute `/bin/launchctl`; concurrent bounded stdout/stderr retention with continued draining; one start/read/exit deadline; kill-and-reap on timeout; explicit transport failure. | Unix shim tests passed for simultaneous streams, output beyond 2,000 characters with a structural state at the end, oversized output, timed-out open pipes with child PID gone, and missing executable. A delayed `Process.Start` is explicitly reported `ReapUnconfirmed`; that rare branch has no injected regression. |
| F06 observation/cleanup | Baseline source treated generic nonzero `print` or missing/unknown state as terminal; failed bootout followed by failed print could be treated as gone. No new same-test baseline execution was run. | Explicit Running, Terminal, ConfirmedNotFound and Unknown observations; only a recognizable absent-service response establishes not-found; missing/malformed/unknown state, timeout and permission failure retain uncertainty. Bootout cleanup requires confirmed absence and keeps metadata for retry. | Parser/fake tests passed malformed/unknown/permission cases, signal exit and recognized absence; failed bootout plus failed print retained the plist and failed, then a successful retry removed it. |
| F03/F05 ownership | Baseline source inspection showed preparation/launch/disposal registration gaps and independent waiter polling. No new same-test baseline execution was run. | Gate-owned preparation and admitted launch; disposal joins admitted work. One execution-owned observation task holds the stable result; cancelled waiters do not cancel it. | Cancellation, two waiters, late retry, terminate during observation, dispose during observation and disposal winning before kickstart passed with fake launchd. |
| Private job files | Luna final review found predictable shared-temp directory creation could expose plist arguments/environment and output to other local users under ordinary Unix umask. | Atomic private temporary subdirectory per job. | Unix permission assertion passed: directory mode 0700. Faults still retain the private directory for cleanup retry. |

The baseline column is static source evidence, not a fabricated failing test run. The earlier GM-2 native run documents old behavior only. Native macOS regressions for instant exit and same-process-group descendants remain pending on this exact source revision.

## Checks at tested source SHA

| Check | Result |
| --- | --- |
| Windows `dotnet test tests/Gagamba.Execution.MacOS.Tests/Gagamba.Execution.MacOS.Tests.csproj --no-restore` | 40 passed, 0 failed, 4 skipped (Unix helper/permission shims). Existing native-provider tests also return early on non-macOS and are not counted as native evidence. |
| Ubuntu WSL disposable copy, same test project | 44 passed, 0 failed, 0 skipped. This validates parser, fake launchd and actual Unix helper process I/O/deadline behavior, not launchd. |
| Windows `dotnet build Gagamba.sln --no-restore -v:q` | Passed, 0 warnings, 0 errors before the private-directory review fix; focused project rebuild after that fix passed. |
| Native macOS launchd provider tests / conformance / installed package | Not run; qualification pending. |

The WSL tree was copied from the candidate checkout to `/tmp/gagamba-ar3-20261009` without `.git`, `bin` or `obj`. SourceLink warnings in that disposable copy reflect absent Git metadata. After the final WSL test, no `gagamba-ar3-helper-*` directory remained. All AR-3 private job directories created by the tests were removed; `/tmp/gagamba-macos-tests` is a pre-existing legacy provider-test directory and is excluded from that claim. Windows fake tests left no owned job directory. The timeout shim wrote its PID before flooding both pipes; the regression checked that PID was absent after transport returned `Timeout`. A failed bootout/print test deliberately kept its plist until retry established absence, then checked removal. No launchd service was installed by these local tests.

## Remaining qualification and review

Native macOS qualification must run this source revision with actual `bootstrap`, `kickstart`, root exit/instant exit, same-PG descendants, explicit terminate, cancellation, disposal races, cleanup and `setsid` escape. Use finite workloads and independent bounded cleanup. Until then F03/F05/F06/F07 are implemented and locally tested but not native-qualified on macOS; native compatibility or race behavior may require further repair. The launchd `print` format and immediate post-bootout absence assumption require native verification. AR-5 separately owns output-file budgets and retention; AR-3 only makes their directory private.

Luna's initial read-only inspection identified the baseline helper, observation and lifecycle defects. Luna's final read-only review found private-directory permissions and an incomplete helper-timeout oracle; both were repaired and rerun on WSL. It found no other high-confidence lifecycle/parser defect. Luna review is source scrutiny, not native evidence.
