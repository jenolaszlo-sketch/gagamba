# AR-2 Linux lifecycle, descriptors and cleanup evidence

Date: 2026-10-09 (Asia/Manila)
Remediation slice: AR-2
Baseline source SHA: `57f3b050ff1f60778685916a6c1d4dd57e8427cc`
Final source SHA: `9550ff768cf09889de4a37a2811a187a0281c44d`
Branch: `main`
Host OS/version: Ubuntu 26.04.1 LTS under WSL2 on the local Windows host
Architecture: x86-64
Runtime/SDK version: .NET runtime 10.0.12; SDK 10.0.112
Relevant native prerequisites: kernel `6.6.114.1-microsoft-standard-WSL2`; glibc `2.43-2ubuntu2.4`; cgroup v2 (`cgroup2fs`); GNU `posix_spawnattr_setcgroup_np` and `posix_spawn_file_actions_addclosefrom_np`; `CLONE_INTO_CGROUP` behaviorally verified at first Prepare
Privilege/delegation: root UID 0 in `/init.scope` with writable cgroup root, plus a separate UID 65534 run moved into a uniquely created, owned delegated parent; the parent directory, `cgroup.procs`, and `cgroup.subtree_control` were owned by UID 65534 only for that run
Qualification scope: native lifecycle and descriptor behavior on this exact WSL2/x86-64/glibc-2.43/cgroup-v2 configuration, including root and delegated unprivileged execution
Explicit exclusions: other kernels, libc versions, architectures and delegation topologies; installed NuGet package and release qualification; hostile-workload filesystem/network/IPC/identity isolation; resistance to a privileged target migrating to a sibling cgroup

The source commit was clean after the source/test commit. The native test tree was copied byte-for-byte from that checkout to `/tmp/gagamba-ar2-57f3b05-20261009` without `.git`, `bin`, or `obj`; generated SourceLink warnings reflect the missing `.git` in this disposable copy. This documentation commit follows the tested source commit and does not change tested code.

## Baseline → repair → observation

| Probe / test | Baseline failure | Implementation change | Fixed observation |
| --- | --- | --- | --- |
| `DisposalWinsBeforeAnAdmittedLaunchCanSpawn` on baseline SHA | Native root-WSL run: `LaunchResult.Started` after disposal won, expected `Failed`. | Admission remains owned between preparation consumption and spawn; spawn and execution registration occur under the provider gate, with a disposal check at that transition. | Same regression passed on final SHA; no post-disposal target marker. |
| `E8_CancelAfterRootReapPreservesExitCode` on baseline SHA | Native root-WSL run: retry returned `CompletionResult.Failed` after the first observer reaped root exit 17 and was cancelled. | One execution state owns `waitpid`, root status and terminal task; caller cancellation only cancels that caller's wait. `EINTR` retries and unexpected `ECHILD` is a failure. | Same regression passed: retry returned `NaturalExit(17)` after the child delayed domain emptiness. Concurrent and late waiters also returned the same exit 23. |
| Archived E9 native probe on review SHA `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88` | Inheritable FD 40 wrote `inherited` to the sentinel. `git diff --quiet` confirmed Linux provider source was unchanged between the review SHA and AR-2 baseline SHA. The first new file-FD baseline test accidentally overwrote its own evidence with a parent write and passed; that invalid observation is not used. | `posix_spawn_file_actions_addclosefrom_np(3)` runs in the child after optional chdir. Trusted host-provided stdin/stdout/stderr FDs 0–2 remain inherited; every FD >=3 is closed before target code. | Corrected file test and independent pipe/socket test passed. Target-run marker and exit prove the child ran; no sentinel write and no pipe/socket bytes arrived; parent successfully reused its descriptors afterward. The corrected regression was not rerun against the unchanged baseline, so the archived E9 probe is the measured baseline. |
| F09 `OwnedCgroup.Dispose` inspection and cleanup tests | Baseline set `_disposed` before its own `Kill`, making that call return `already removed`; normal provider disposal separately called `Kill`, so this did **not** prove all normal disposal skipped termination. Baseline swallowed kill, read and remove faults. | Kill result is checked; a sole cleanup path polls `cgroup.events` for confirmed `populated 0` with a bound, removes descendants bottom-up, retains failed groups, and exposes failure for retry. Missing/unreadable events are errors unless this owner already confirmed removal. | Denied kill, events read, and removal regressions reported failure; clearing the injected denial permitted cleanup. Disposal-retry regression required the waiter to resolve `Terminated`, `/proc/<root>` to disappear after owned reaping, and the cgroup to be removed before retry returned. |
| F10 prerequisite and placement inspection | Baseline only probed placement behavior and did not preflight required GNU symbols; first-Prepare cache fields were unsynchronized. No same-test baseline injection was run. | Prepare requires Linux x86-64, glibc 2.43 and all required symbols, then synchronizes one live placement self-test. `EOPNOTSUPP`, `ENOSYS`, and `EACCES` classify refusal; no parent-side migration fallback exists. | Missing-symbol and injected `EOPNOTSUPP` tests rejected twice with stable reasons and no target; 12 concurrent first Preparations ran one probe. Native root and delegated tests accepted after live born-inside observation. |
| Archived E10 migration probe, repeated as `E10_RootWslSiblingMigrationIsAnObservedContainmentLimit` | Root-privileged target migrated to a sibling cgroup inside a unique outer parent and wrote after Terminate. | No claim of resistance to privileged cgroup migration was added. The regression preserves this boundary. | The target again survived termination of its original execution cgroup; the test passed because it asserts the documented limitation. The unique outer parent then killed the survivor and was removed. |

The archived E9/E10 source and raw observations are in `docs/reviews/2026-10-08-evidence/linux-probe/Program.cs` and `linux-probe-results.txt`. The two first-run failing AR-2 regressions above were run in root WSL before source edits: 1 passed (invalid E9 oracle), 2 failed. The corrected E9 oracle is explicitly distinguished from that invalid first run.

## Exact local checks on final source SHA

| Check | Result |
| --- | --- |
| Solution build, copied WSL source tree | Passed; 0 errors, 42 SourceLink warnings because `.git` was omitted from the disposable copy. |
| Linux provider tests, root WSL | 38 passed, 0 failed, 0 skipped. Includes 14 new AR-2 lifecycle regressions and existing nested-cgroup/delayed-exit tests. |
| AR-2 regressions, UID 65534 in delegated parent | 13 passed, 0 failed, 0 skipped. E10 root-only migration leg was excluded by filter, not counted as a skip. |
| Execution contract tests | 14 passed, 0 failed, 0 skipped. |
| Runtime tests | 5 passed, 0 failed, 0 skipped. |
| Conformance tests, root WSL | 5 passed, 0 failed, 0 skipped. The generated report said `usable: true` and all 11 legs `Passed`, including prepare, termination, root exit, disposal, completion and setsid. |
| Native Windows/macOS, installed package, release gate | Not run for AR-2; unsupported as AR-2 qualification evidence. |

Commands: `dotnet test tests/Gagamba.Execution.Linux.Tests/Gagamba.Execution.Linux.Tests.csproj --no-restore`; `dotnet build Gagamba.sln -v:q`; and `dotnet test` with `--no-build --no-restore` for `Gagamba.Execution.Tests`, `Gagamba.Runtime.Tests`, and `Gagamba.Conformance.Tests`. The unprivileged run used `dotnet vstest` on the built Linux test DLL with filter `FullyQualifiedName~LifecycleRegressions&FullyQualifiedName!~E10_RootWslSiblingMigrationIsAnObservedContainmentLimit`, after moving only that runner into the delegated parent and dropping to UID 65534. The final root and delegated runs were repeated after the source commit.

## Ownership and cleanup observations

- Prepared cgroups are registered under the provider gate. A consumed preparation remains in a launch-admission set until launch either transfers the group to an execution state or confirms cleanup. Disposal blocks new admission and joins admitted launches.
- One `LinuxExecutionState` owns each root PID, `waitpid` call stream, exit status, termination intent, cgroup population observation, removal and terminal result. `Terminate` only requests `cgroup.kill`; it never reaps independently. Multiple/cancelled waiters observe the same task. Issued handles retain a weak-keyed terminal result, pruned periodically.
- Disposal requests stop and joins each active state. If kill is denied, it reports failure without hanging, retains the group and state, and a later disposal retries stop, joins the reaper and confirms removal. A failed observation remains a `CompletionResult.Failed`; it is not interpreted as an empty domain.
- The native tests used GUID workspaces/cgroups, finite child workloads, explicit marker or root-reap barriers, independent watchdogs and outer test-owned cgroup cleanup. The state confirms root reaping by successful `waitpid`; `cgroup.events populated 0` confirms subtree emptiness before bottom-up removal. The disposal-retry test independently checked the root's `/proc` entry and waiter completion. Positive parent-FD writes and child-run markers rule out a vacuous descriptor pass.
- The root-only E10 test's escaped sibling was killed by the unique outer test parent, independent of provider cleanup. The delegated parent showed `populated 0`, no child directories, and was removed. A final `/sys/fs/cgroup/gagamba-ar2-*` search found no AR-2 test cgroups; a `/tmp/gagamba-ar2-*` search found only the intentional source mirror. The older shared GL-2 test parent (`/sys/fs/cgroup/gagamba-gl2-tests`) remains with `populated 0` and pre-existing empty directories, which this slice did not remove or count as AR-2-owned. No helper service was installed.

Cleanup status: **Confirmed for the final AR-2 unique root and delegated test cgroups/workspaces**. The legacy shared GL-2 fixture's older empty directories remain outside that claim. Fault-injection legs deliberately observed and retained a failed inner cleanup before retry; they did not count that intermediate state as success. Abrupt provider-process death and a permanently denied kernel cleanup remain unqualified.

## Finding disposition and boundary

| Finding | Status at AR-2 | Basis |
| --- | --- | --- |
| F03 Linux launch/dispose ownership | Fixed for the qualified Linux configuration | Gate-owned admission and disposal-race regression; no target marker after disposal won. |
| F05 Linux completion/reaping | Fixed for the qualified Linux configuration | E8, concurrent/late waiters, dispose-during-wait, single `waitpid` owner. |
| F08 inherited descriptors | Fixed for the qualified Linux configuration | Child close-from action and file/pipe/socket native probes; stdio 0–2 intentionally inherited. |
| F09 cgroup cleanup | Fixed for tested faults and qualified configuration | Bounded population/removal, surfaced denial and retry, no false empty from unreadable events. Permanent denial remains an explicit failure. |
| F10 native prerequisites/placement | Partially addressed | GNU symbol/ABI gate and one synchronized live self-test proven on glibc 2.43/x86-64 WSL2; other native profiles unqualified. |
| E10 privileged sibling migration | Still open as containment limit | Root WSL target survived original-domain Terminate after migration. Escape resistance remains Partial. |
| F01 hostile-workload security boundary | Still open | Lifecycle/FD ownership does not isolate filesystem, network, IPC, credentials, identity or privileged cgroup writes. |

Status: **Implemented and native-qualified for the stated Ubuntu WSL2 glibc-2.43 host configuration, including a delegated UID 65534 run.** This is not package-qualified or release-qualified and does not extend to general Linux deployment. The `posix_spawn` opaque buffers and flag are qualified behaviorally on this glibc profile; other libc versions are rejected before target dispatch. A delegated unprivileged run on a different non-WSL Linux kernel remains a useful future qualification, not an observed result here.

Luna independently inspected the baseline and final implementation. Its final review found that disposal retry did not join a still-running reaper after denied kill, and that the probe's `kill` import lacked errno capture; both findings were accepted, repaired and covered by the final runs. Its evidence-artifact reminder is satisfied by this document. No model review is treated as a substitute for native measurements.
