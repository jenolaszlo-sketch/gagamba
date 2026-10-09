# AR-6 — native conformance and exact-source release gate

- Date: 2026-10-09 (Asia/Manila)
- Slice: AR-6
- Baseline SHA: `58ccf129fcfa3b35e6873d3af529459fd8c806a7`
- Tested implementation SHA: `e22e4422c663282883699cd2dc738392e6b7e365`
- Branch/tree: `main`; clean when packages were packed and strict Windows/Linux checks ran. This evidence/handoff documentation commit follows the tested implementation and is itself a new SHA requiring requalification before release.
- Windows host: Microsoft Windows 10.0.26200, X64, .NET SDK 10.0.401/runtime 10.0.12; native Job Object provider.
- Linux host: Ubuntu 26.04.1 under WSL2, kernel 6.6.114.1-microsoft-standard-WSL2, x86-64, glibc 2.43, .NET SDK 10.0.112/runtime 10.0.12; cgroup v2, root UID 0 with writable `/sys/fs/cgroup/gagamba-ar6-tests`, then a unique cgroup delegated to UID 65534 for a second strict native and installed-consumer run.
- Native macOS host: unavailable in this local session; launchd domain, architecture, SDK/runtime and AR-6 behavioral result unmeasured.

**Status:** Implemented and locally tested. The Windows Job Object and stated WSL2 Linux cgroup configuration passed strict native conformance. The same exact-source candidate was restored and exercised by independent Windows and Linux installed-package consumers. A subsequent WSL2 run moved the test process into a unique delegated cgroup, dropped to UID 65534, and passed strict conformance and the same installed-package consumer; its parent was empty and removed after shutting down the test's Roslyn compiler server. Native macOS and its installed consumer are absent, and GitHub has no registered delegated Linux runner for this workflow. No three-host qualification manifest exists, and release qualification is **not established**. No package was published or tag created.

The raw exact-source local reports are [Windows conformance](release-qualification-AR-6/conformance-windows.json), [Windows installed consumer](release-qualification-AR-6/consumer-windows.json), [root Linux conformance](release-qualification-AR-6/conformance-linux.json), [root Linux installed consumer](release-qualification-AR-6/consumer-linux.json), [delegated Linux conformance](release-qualification-AR-6/conformance-linux-delegated.json), and [delegated Linux installed consumer](release-qualification-AR-6/consumer-linux-delegated.json). These are local evidence, not a sealed CI release artifact.

## Scope and baseline-to-fix controls

The baseline conformance runner reused workspace paths and leaned on heartbeat timestamps. Source inspection showed that an early root exit, a stale heartbeat, or a provider falsely reporting terminal state could make lifecycle evidence weak. The prior publish workflow packed and pushed directly on both `v*` tags and manual dispatch, without source-bound native or installed-package evidence. These are source observations; no baseline publication or destructive lifecycle reproduction was attempted.

Each AR-6 behavioral leg now creates a fresh workspace/nonce. Workloads write a workspace proof and, where lifetime matters, hold an OS-visible lock until exit. Root-exit has an explicit proof file plus a live child lock; termination and disposal require bounded lock release and provider terminal evidence. The `setsid` leg checks live leaf/escapee locks rather than frozen mtimes. A deliberately false provider that claims launch, termination and completion without running a workload fails the single-use, unit-termination and completion legs. A source/test run on Windows and Linux exercises the real providers. Unsupported exploratory hosts use visible xUnit skips; qualification mode rejects every missing/skipped mandatory leg and failed evidence write.

`qualify.yml` packs one five-package candidate from a clean SHA, runs strict native conformance and an isolated installed-package consumer on Windows, a delegated non-root Linux cgroup-v2 runner, and macOS, then seals only the same downloaded candidate and three evidence pairs. The seal checks source SHA, clean checkout, version, host identity, native mechanism, mandatory leg set, cleanup, Linux delegation, macOS launchd domain, installed-consumer success, internal dependency lockstep, nuspec repository commit, and each `.nupkg`/`.snupkg` SHA-256. `publish.yml` no longer packs; tag and manual paths both download a successful qualification artifact for their exact SHA, verify it before NuGet login, then push the verified files. Downloaded release artifacts live outside the Git checkout, so full untracked-file cleanliness can be checked.

The Linux CI qualifier requires a self-hosted runner labeled `gagamba-cgroup-v2-delegated`, a writable `/sys/fs/cgroup/gagamba-qualification` parent delegated to a non-root runner account, cgroup v2 `cgroup.kill`, glibc with `posix_spawn` SETCGROUP and close-from actions, and .NET 10. The Windows and macOS jobs use native hosted runners but remain unmeasured for this exact SHA until the workflow actually runs. A skipped or absent runner cannot yield a sealed artifact.

## Exact candidate and results

All five packages were packed from clean `e22e4422c663282883699cd2dc738392e6b7e365`, version `0.1.0-preview.4`. The `Gagamba.Runtime` nuspec repository commit equals that SHA; the gate checks this on all five packages. The local candidate is in a disposable Solo staging directory, outside the Gagamba Git tree. Hashes below identify the exact files used by both installed consumers; no release seal was issued.

| Package | `.nupkg` SHA-256 | `.snupkg` SHA-256 |
| --- | --- | --- |
| `Gagamba.Execution` | `8cf588aa65c393b6e638798833df8e3f5d4b5045da7d097efac8ea8cd6c8d427` | `115d3c9237a88798030139013d96adcb028287669afbce3bcde4770a37a24f51` |
| `Gagamba.Execution.Linux` | `be3c3a10e89a243ec22aa5f791eb282f811f4514ecf6fed8db5a9b488e4e653b` | `3d5503b742d12de2c73ace3acc03d9dc4a7cc482f90016d974d3d7e621064823` |
| `Gagamba.Execution.MacOS` | `9018226cb5693b9efdc3bac1b080f07f83e846461dac1417f1a4604a0b7125ea` | `777a6aaeb6d6f8996b0b6366054a4e954e1530efacdd7951cb038d98e4118a15` |
| `Gagamba.Execution.Windows` | `899ab251b196e88f235fcd52bfe5c0d811350451c309e5c716f98f870b8ee8f9` | `7f018c105684716fd547d763c8e28d8dc49246891049b4fc31d473deb277c1a6` |
| `Gagamba.Runtime` | `8f6486cdf42d630ed16dc7ded089d93fec44d305fcdefc0007c4d7fbd2af1b9c` | `b57960a35d91b891fb9ce8f9af28b6058571a07a87073c033b044a889de3dd97` |

| Check at tested implementation SHA | Result |
| --- | --- |
| Windows `dotnet build Gagamba.sln -c Release --no-restore` | Passed, zero warnings/errors |
| Windows strict conformance with exact SHA | 6 passed, 0 skipped; `cleanup=Confirmed`, all mandatory legs Passed, `setsid-escape` explicitly Skipped as non-applicable |
| WSL2 Linux strict conformance with exact SHA | 6 passed, 0 skipped; `cleanup=Confirmed`, 11/11 legs Passed, including setsid escape; evidence marks `delegated=false` because UID 0 |
| WSL2 Linux delegated strict conformance with exact SHA | 6 passed, 0 skipped at UID 65534 inside a unique delegated cgroup; `cleanup=Confirmed`, 11/11 legs Passed, `delegated=true` |
| Independent Windows installed consumer | Local-only NuGet restore from the candidate feed, runtime selected `windows-job`, finite deadline stopped the native domain, bounded capture completed and cleanup Confirmed |
| Independent Linux installed consumer | Same candidate bytes, local-only NuGet restore, runtime selected `linux-cgroup-v2`, finite deadline/capture/cleanup passed |
| Independent delegated Linux installed consumer | Same candidate bytes restored and executed as UID 65534 within the delegated parent; deadline/capture/cleanup passed |
| Gate negatives | 17 Python tests passed: stale SHA, dirty claim, wrong version, missing host/leg, skipped leg, unsupported Linux delegation, unknown cleanup, failed/mismatched consumer, missing/extra/tampered packages/symbols, absent manifest and manual/tag workflow gate ordering |
| Actual seal with local Windows/Linux evidence | Rejected `macos: evidence artifact absent`; no manifest emitted |
| Workflow syntax | Both YAML files parsed on the WSL host |
| Native macOS and three-host release artifact | Not run / absent; therefore not qualified |

The root WSL2 run's AR-6 cgroup parent contained zero child directories after strict conformance and the installed consumer. In the delegated run, the provider's child cgroups were removed, but the outer parent initially stayed populated by the test's Roslyn `VBCSCompiler` process after `dotnet run`; that is an independent compiler helper, not a Gagamba workload. Running `dotnet build-server shutdown` under the same UID/home stopped it. The final repeated delegated run recorded zero child directories, `populated 0`, and successful `rmdir` of the unique parent; the test command exited 0. Windows provider completion and released workload locks are the bounded cleanup proof for its tested legs. The disposable independent-consumer projects used only the candidate NuGet feed and blocked parent `Directory.Build.props`/targets imports; no project reference or repository source was compiled by those consumers. The Linux conformance build in a disposable copy had SourceLink warnings because that copy has no `.git`; it was not used to pack the candidate.

## Qualification matrix, review and limits

| Platform | Local evidence at `e22e442` | Release requirement | Status |
| --- | --- | --- | --- |
| Windows | Windows 10.0.26200 X64 native Job Object conformance and installed consumer Passed | Strict clean-SHA native evidence plus identical candidate hashes | Locally native/package tested; CI qualification not run |
| Linux | WSL2 kernel 6.6.114.1, glibc 2.43, root and delegated UID-65534 cgroup-v2 native conformance and installed consumer Passed | Registered unprivileged delegated cgroup-v2 CI runner and same candidate | Locally qualified for stated WSL2 topology; release CI runner absent |
| macOS | No native host; fake/parser tests in AR-3/AR-5 do not qualify launchd | GUI launchd domain, native conformance, independent consumer | Pending |

Luna's read-only AR-6 inspection found the prior conformance suite could return green on unavailable hosts and relied on weak lifetime observation; the changes above address those points. Its release inspection found direct tag/manual publishing with no exact-source candidate; both paths now share the gate. The final read-only review found no material release bypass or false-positive lifetime-lock gap. It identified an untracked-file omission in the clean-source check; Sol moved downloaded artifacts outside the checkout and changed the gate to inspect tracked **and** untracked files. No style-only changes were taken.

AR-6 improves evidence and the F14/F15 release boundary; it does not retroactively qualify AR-3 macOS, AR-5 macOS capture, a different Linux topology, or the documentation commit's new SHA. Publication is blocked until a successful three-host qualification produces a sealed artifact for the exact revision being published. The present code enforces a release gate, but no release-qualified package exists. Filesystem, network, IPC, credentials, host identity and brokers/services remain outside this lifecycle-only boundary; the documented macOS process-group escape is observed, not prevented. AR-7 restricted-execution design and implementation remain untouched.
