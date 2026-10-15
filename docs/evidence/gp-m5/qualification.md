# GP-M5 bounded macOS capture cross-platform evidence

Date: 2026-10-16
Remediation slice: GP-M5 — bounded macOS launchd capture and reader-task join correction
Baseline source SHA: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56` (public GP-M5 baseline); historical runtime producer: `9fe127a` (Marang baseline)
Final source SHA: `0c1f05e199588622a5987f95a9149374f5bd858b`
Branch: `codex/gp-m5-macos-capture`
Host OS/version: macOS 15.7.9 (24G830, hosted ARM64); Windows 10.0.26200; Ubuntu 26.04.1 LTS under WSL2
Architecture: ARM64 macOS; x64 Windows and Linux
Runtime/SDK: macOS .NET 10.0.13 and SDK 10.0.401; Windows .NET 10.0.12; Linux .NET 10.0.12 and SDK 10.0.401; target framework `net10.0`
Relevant native prerequisites: macOS `gui/501`, absolute `/bin/launchctl`, private FIFO paths; Linux WSL2 kernel `6.6.114.1-microsoft-standard-WSL2`, cgroup v2 with `cpu memory pids` controllers and a root-created/delegated subtree. Supplemental same-host [libc inventory](linux-libc.txt) records Ubuntu GLIBC 2.43-2ubuntu2.4. Linux qualification and package consumer ran as UID/GID 1000 (`gaga`) in the delegated cgroup. Windows uses the local native Job Object provider.
Qualification scope: exact-source macOS capture and lifecycle tests, exact-source Windows and delegated WSL2 Linux regressions, strict conformance on all three recorded hosts, isolated installed-package consumers on macOS/Windows/Linux, and canonical five-package hash parity.
Explicit exclusions: general hostile-workload security, filesystem/network/IPC/credential/identity confinement, same-user resistance, macOS `setsid` containment, owner-death cleanup, Linux qualification outside this recorded WSL2 delegated configuration, unmeasured libc configurations, root WSL sibling migration as a Gagamba guarantee, Hufu/Marang semantic qualification, and release qualification.

**Status: GP-M5 complete for the stated bounded capture profile at implementation source `0c1f05e`.** Native capture, lifecycle, cross-platform regressions, exact-package consumers and independent review passed. Reviews [04](reviews/04-final-independent-review.md) and [05](reviews/05-source-provenance-addendum.md) record the final disposition and source-identity method. The evidence is committed separately; the named implementation and package hashes remain the qualified inputs. No package was published. This evidence does not qualify a later implementation revision or general sandbox security.

## Source identity

The native macOS workflow recorded empty source-start/source-end status files at source `0c1f05e199588622a5987f95a9149374f5bd858b`. The Linux workspace had no `.git` metadata, so its identity must not be described as a native clean-Git-checkout claim. The preserved source export was a Git archive of the exact commit. `source-content-verification.json` records SHA-256 `b969a00faa873778aadc92403199922db6a8810ccfe3f4ecf26a1eab8f7af1b3`, compares 217 regular files and 0 symlinks with 0 mismatches, and passes. The verification script and JSON are included in the cross-platform archive. Both duplicate 1,863,680-byte final-source tar files are excluded from the zip; the internal manifest records their matching hash. The earlier candidate source tar is also excluded and separately hashed.

## Exact checks by host

Counts below are kept separate by suite and by host; the focused AR-5 selection is a separate run and is not added into the ordinary Linux provider-suite count.

| Host / check | Passed | Failed | Skipped | Result |
| --- | ---: | ---: | ---: | --- |
| macOS ARM64 native capture probe | 14 | 0 | 0 | All 14 cases recorded cleanup confirmation |
| macOS ARM64 installed-package capture consumer | 14 | 0 | 0 | All 14 cases recorded cleanup confirmation; provider disposal succeeded |
| macOS ARM64 provider tests | 55 | 0 | 0 | TRX preserved |
| macOS ARM64 shared execution contracts | 20 | 0 | 0 | TRX preserved |
| macOS ARM64 runtime orchestration | 14 | 0 | 0 | TRX preserved |
| macOS ARM64 strict conformance | 6 | 0 | 0 | TRX preserved |
| Windows x64 provider tests | 30 | 0 | 0 | TRX preserved |
| Windows x64 shared execution contracts | 20 | 0 | 0 | TRX preserved |
| Windows x64 runtime orchestration | 14 | 0 | 0 | TRX preserved |
| Windows x64 conformance tests | 6 | 0 | 0 | TRX preserved; conformance JSON records zero skipped/unsupported mandatory legs |
| Linux x64 provider tests, delegated WSL2 invocation | 39 | 0 | 3 | 42 total; the two AR-5 tests and E10 root-WSL sibling migration test were skipped in this ordinary invocation |
| Linux x64 focused AR-5 capture tests, delegated cgroup rerun | 2 | 0 | 0 | Separate focused TRX; both cases passed |
| Linux x64 shared execution contracts | 20 | 0 | 0 | TRX preserved |
| Linux x64 runtime orchestration | 14 | 0 | 0 | TRX preserved |
| Linux x64 strict conformance | 6 | 0 | 0 | JSON reports 0 skipped/unsupported mandatory legs |
| Linux installed-package lifecycle consumer | Passed | — | — | Independent package consumer; cgroup emptied and parent removed |
| Windows canonical-package lifecycle consumer | Passed | — | — | Independent consumer on Windows; all five package hashes match canonical set |
| Native macOS installed-package lifecycle consumer | Passed | — | — | Independent consumer on macOS; exact package manifest preserved |

The initial focused Linux AR-5 wrapper attempt is retained and reported as a setup failure: it ran in `/init.scope`, outside the required delegated cgroup, so both probes returned `PrepareFailure`. The subsequent bounded rerun created `/sys/fs/cgroup/gagamba-ar5-tests`, moved the test actor into that delegated cgroup, and passed both cases. Its teardown recorded `populated 0` and `parent_removed=true`. The ordinary Linux provider selection remains 39/0/3; those separate focused results do not rewrite its three skips.

The Linux conformance record binds to exact source `0c1f05e`, reports cgroup v2, delegated true, usable true, cleanup confirmed and all mandatory legs passed, including its `setsid` leg for this exact WSL2 host/configuration. The `E10_RootWslSiblingMigrationIsAnObservedContainmentLimit` case was skipped because this was the delegated UID 1000 run, not a root WSL migration qualification. No E10 migration guarantee is claimed.

Cleanup evidence includes final `cgroup.events` with `populated 0`, empty `cgroup.procs`, and successful parent removal for the delegated Linux provider selection, installed consumer, and focused AR-5 rerun. The earlier focused wrapper failure occurred before workload dispatch because preparation refused outside the required cgroup; its failure logs and cleanup inventory remain preserved. Per-case macOS output cleanup and provider disposal are documented above and in the native report.

## Canonical package identity parity

The macOS run produced the canonical package set `0.1.0-preview.4-gp-m5.1`. The independent macOS capture consumer, Windows lifecycle consumer and Linux lifecycle consumer each report the same five package hashes and the same repository source commit. The two non-macOS consumers exercise lifecycle/package installation; macOS alone ran the 14-case installed capture consumer.

| Package | SHA-256 on macOS, Windows and Linux consumer records |
| --- | --- |
| Gagamba.Execution | `8a93d8fbcfb43d695ea8d65db3603b98e2e9c00750a662829c6374c0a8be4c0c` |
| Gagamba.Execution.Windows | `1d57fd0bbf4ae6830be49fe6c4c4339a26a6349fbd5308d39e167c0d7decef00` |
| Gagamba.Execution.Linux | `7fa53482b389a12630d649ba43cafb09b6a2f61d7a45fd6b77df63a5379df1cc` |
| Gagamba.Execution.MacOS | `912d8033d66feb9fe45c50fa0e2aa862ee77790b8d62f8dc86d65b2138be7ac8` |
| Gagamba.Runtime | `b141f8775185864f491110c0f22684a58a7bbd02d0481ae74776a0961b954311` |

## Review findings and boundary

Review `04-final-independent-review.md` reports PASS for the bounded macOS capture slice at the exact source SHA. It records the R6 reader-task join correction as fixed and tested, native/package outcomes, and the same-process-group boundary. Review 05 clarifies the verified Linux source-export identity. No result here establishes hostile-workload security or package publication readiness.

- GP-M5 bounded macOS capture — **Native-qualified and package-qualified** for the exact ARM64 macOS host/source/package set recorded in the prior sections.
- Windows and Linux — **Locally/native tested in the stated host configurations**; test and consumer results above are configuration-specific and do not generalize to all Windows or Linux deployments.
- E10 root WSL sibling migration — **Skipped / not claimed**; it is not a Gagamba guarantee.
- Hostile-workload security boundary — **Still open / excluded**; lifecycle and bounded I/O do not establish filesystem, network, IPC, credential or identity confinement.

These results do not establish compatibility with other OS versions, architectures, runtimes, libc implementations, or package SHAs. Linux is measured only on this delegated WSL2 cgroup-v2 host. macOS same-user and `setsid` escape resistance, owner-death recovery, resource limits beyond tested output-byte bounds, and Marang/Hufu behavior remain outside this evidence.

## Preserved raw evidence

[`cross-platform.zip`](cross-platform.zip) contains the exact-source Linux artifacts and source identity verifier, prior-candidate Linux artifacts under a distinct history prefix, and exact-source Windows artifacts. Logs, TRX files, conformance and consumer JSON, scripts, cgroup inventories, source revision markers, source-content verification records and the initial failed focused wrapper attempt are preserved. The large source tar snapshots are excluded to avoid duplicate payload; their hashes and sizes are recorded in the internal manifest. The macOS raw bundle remains in [`qualified-native.zip`](qualified-native.zip), and first-candidate history remains in [`first-candidate-native.zip`](first-candidate-native.zip).

Cross-platform evidence archive SHA-256: `867239bcfc690c8eb4968a5892291bc32c0c3522890e3bed57c19c1c115ada14`.
