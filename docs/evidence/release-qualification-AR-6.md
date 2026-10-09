# AR-6 — native conformance and exact-source release qualification

- Date: 2026-10-09 (Asia/Manila)
- Slice: AR-6; review findings F14 and F15
- Review baseline: `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88`; AR-6 implementation baseline: `58ccf129fcfa3b35e6873d3af529459fd8c806a7`
- Qualified source SHA: `7660f16dbb5b4b165e6a5279b131bf53c5bf6a7b`, branch `qualification/ar6-native`, clean at pack and seal
- Hosted run: [qualification 37901286740](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/37901286740), all candidate/native/seal jobs passed
- Version: `0.1.0-preview.4`; no tag or package publication

**Status: AR-6 native and package qualification complete for the exact qualified SHA.** The hosted run packed one five-package candidate, passed strict conformance and independent installed-package consumers on Windows, delegated non-root Linux, and macOS, then sealed and verified that candidate. A later source or documentation commit has a different SHA and must pass the same gate before its own package can be released. AR-7 is untouched.

The durable raw records are [sealed manifest](release-qualification-AR-6/manifest-hosted-7660f16.json), [Windows conformance](release-qualification-AR-6/conformance-windows-hosted-7660f16.json), [Windows consumer](release-qualification-AR-6/consumer-windows-hosted-7660f16.json), [Linux conformance](release-qualification-AR-6/conformance-linux-hosted-7660f16.json), [Linux consumer](release-qualification-AR-6/consumer-linux-hosted-7660f16.json), [macOS conformance](release-qualification-AR-6/conformance-macos-hosted-7660f16.json), and [macOS consumer](release-qualification-AR-6/consumer-macos-hosted-7660f16.json). The downloadable CI artifact also contains the exact package bytes. The same downloaded seal and all ten package files were independently accepted by `python -B eng/qualification_gate.py verify` against the clean local checkout at the qualified SHA.

## Native matrix and cleanup

| Host and mechanism | Strict conformance | Installed consumer | Cleanup |
| --- | --- | --- | --- |
| Microsoft Windows 10.0.26100 X64, .NET 10.0.12, native Job Object | All mandatory legs Passed; setsid escape Skipped as non-applicable | Passed from the packed NuGet candidate | Confirmed: job terminal and workload lifetime locks released |
| Ubuntu 26.04.1 LTS X64, .NET 10.0.12, cgroup v2 delegated to a non-root UID | All 11 legs Passed, including setsid escape | Passed from the same candidate | Confirmed: cgroup-owned workloads stopped and child cgroups removed; delegated wrapper checked the parent empty and removed it after build-server shutdown |
| macOS 26.6.2 Arm64, .NET 10.0.12, launchd `gui/501` process group | All 11 legs Passed, including observed setsid escape | Passed from the same candidate | Confirmed: launchd terminal/bootout and same-process-group workload lifetime locks released |

Each native report has a unique run ID, `usable=true`, `cleanup=Confirmed`, zero skipped or unsupported mandatory legs, and the qualified source SHA. The Linux report confirms cgroup v2 and delegation. The macOS report identifies its GUI launchd domain. Independent consumers restored from the candidate feed, selected the native provider, exercised deadline/lifecycle cleanup, and report the same package hashes. macOS capture remains explicitly unavailable before dispatch; the macOS installed consumer exercises the legacy lifecycle path, not AR-5 capture.

## Exact candidate

The gate checked version, package IDs and dependency lockstep, nuspec repository commit, source cleanliness, and SHA-256 for every `.nupkg` and `.snupkg`. These hashes are from the sealed manifest at the qualified SHA.

| Package | `.nupkg` SHA-256 | `.snupkg` SHA-256 |
| --- | --- | --- |
| `Gagamba.Execution` | `b2af19fd0cf8ed4419bb5c4798ab9efa4ec72f0653e7922dfb82c32e9bb43e0b` | `7dd9bb69de226cb4f2bc6453c429ba817625dd1e82c7c2bde56125b1846e5961` |
| `Gagamba.Execution.Windows` | `8f97f53f66ae56f72e9a65773b5ed69c03deacb74461789d4e14b0abd542cb1d` | `7a2e52e8ec03f73c629307d562d5ef8fcc68cc01d8146121e6faa84e9b90bf04` |
| `Gagamba.Execution.Linux` | `2c66689de80b82f63b7b856550417f5f33592df017812910127956de329b8869` | `0932e3067d887bc492aa70ee18d3f33bbab61506adf40e0c692be190bb6724d9` |
| `Gagamba.Execution.MacOS` | `99b57c6c42f9f4eb67e6ead91b9522d0aa78ddc3d9e52e3656a069de91942389` | `7f0b30e5dc3c8a8ba9d024d66365157ab45e8a7915c2f4b722152d5a6fae69f1` |
| `Gagamba.Runtime` | `7c3604acf9078f9c237ad81c72cf7dfe315a21f701fd378ae58156f8098c431a` | `afda649042c36178962f46bb3d0d42a7e299b5ada9e1404cf47a2b2a362259e1` |

## Baseline-to-fix controls and review

| Baseline failure or uncertainty | Correction and measured result |
| --- | --- |
| F14: conformance reused paths and weak heartbeat-only oracles; a broken provider could appear green or unavailable hosts could skip. | Fresh per-leg workspaces, explicit source binding, OS-visible lifetime locks, root-exit barrier, strict mandatory-leg handling, and a broken-provider negative control. Windows/Linux/macOS strict native runs passed. |
| F15: tag/manual publication packed directly without an exact-source native/package gate. | `qualify.yml` packs once, tests the same candidate on three hosts, and seals only matching evidence. `publish.yml` downloads a successful artifact for its exact SHA and verifies it before NuGet login or push. The hosted seal and independent local verification passed. |
| Native macOS assumptions were unqualified. | Hosted launchd exposed the `(never exited)` running marker, `/var` versus `/private/var` temp aliases, .NET macOS `EWOULDBLOCK` lock-open behavior, and a root-exit timing race in the oracle. Targeted parser/oracle fixes kept unknown terminal states and unrelated I/O failures fail-closed. All 11 macOS legs then passed. |
| Hosted Linux identity was rejected despite valid native evidence. | The gate accepts the observed `Ubuntu 26.04.1 LTS` OS description alongside `Linux`, while still rejecting a Windows host claiming Linux evidence. The final hosted seal passed. |

The release gate's 19 Python tests passed, including stale SHA, dirty tree, missing/extra/tampered packages, skipped or missing legs, unsupported delegation, unknown cleanup, failed/mismatched consumer, absent artifact, both publish paths, Ubuntu acceptance and wrong-OS rejection. The macOS parser/provider suite passed 47 portable tests on Windows with four native-only skips; the hosted strict conformance and installed consumer supply native launchd evidence. Luna's read-only AR-6 review found no remaining material release bypass after the clean-source check was extended to untracked files. No new public SPI was needed for AR-6.

Historical local reports from the earlier implementation SHA remain alongside the hosted records: [Windows](release-qualification-AR-6/conformance-windows.json), [root WSL2 Linux](release-qualification-AR-6/conformance-linux.json), and [delegated WSL2 Linux](release-qualification-AR-6/conformance-linux-delegated.json), with corresponding `consumer-*.json` reports. Those earlier bytes are not the sealed hosted candidate.

## Scope and release boundary

The qualification establishes lifecycle and package behavior on the stated native hosts and mechanisms. macOS process-group escape was observed, not contained; AR-5 macOS bounded capture still refuses before dispatch. Privileged sibling migration, filesystem, network, IPC, credentials, host identity and broker/service restrictions are outside this lifecycle-only claim. AR-7 restricted-execution design and implementation require a separate security review. A tag or manual publication of any SHA other than the qualified one must first produce its own passing three-host seal; no package has been published here.
