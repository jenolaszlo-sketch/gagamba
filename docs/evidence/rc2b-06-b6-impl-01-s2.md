# B6-S2 installed-artifact trust — producer evidence and Q1 handoff

**2026-11-02 · GAGAMBA-RC2B-06-B6-IMPL-01-S2 · canonical main.**
**S2 IMPLEMENTED WITH RESTRICTIONS — READY FOR INDEPENDENT Q1.**

| Source binding | Git commit | Git tree |
|---|---|---|
| Starting S1 acceptance receipt | `df3d6f0fdb5f45e8fd3d8f53eb983b2ad6bba4fb` | `f4a6519d5b3a2c9c984346e955e63ccb787a6b2e` |
| Recovery record and first S2 implementation | `35e74fda0c2717e70aeaedc686b45c080b50ae15` | `640d8ce764ea22b00da2214b316a422acbc03b5e` |
| Tested implementation and source of the retained candidate | `8b897e4ebb8e53385199083a38086a9713f0e07a` | `af1dc67cf6c74979eb24e9395724b86f264f9a3f` |

Windows x64 build 26200, .NET SDK 10.0.401. Nonprivileged native evidence used Ubuntu 26.04.1 under WSL2 x86-64, kernel `6.6.114.1-microsoft-standard-WSL2`, uid/gid 1000, .NET SDK 10.0.114, runtime 10.0.14 and GCC 15.2.0. Cgroup delegation, target execution and privileged installation were outside this run. No accepted qualification or production admission was issued.

**Host restriction.** Direct non-root inspection found `/` owned by `0:0` with mode `0777`. Both the fixed-root verifier and the platform check refused the group/other-writable ancestor. This blocks trusted installation qualification on the observed host. User-owned ext4 fixtures exercised file acquisition and hashing. The permission check took precedence over a host-inventory mismatch, so that mismatch was not counted as a separate native result. See `verification/host-ancestor.log` and `wsl/cli-mutation-probes.txt` in the evidence archive.

## Recovery and change boundary

Git identified `C:/Users/Laszlos/source/repos/Gagamba` as the primary checkout. The inspected remote `main` was `401e1ea7f0be06b27bc188c827dd951016b3c62a`. Three local S1 evidence/decision commits preceded S2. The owner decision at `79406229` accepted the S1 implementation reviewed at `66896754`; `df3d6f0` recorded its provenance. The initial source, all 74 registered worktrees, 44 unrelated untracked paths, refs, reflogs, stashes, unreachable commit inventory, and local handoffs yielded **S2 NOT STARTED**. The other 73 worktrees were clean. Historical checkouts and untracked work were preserved. Only named commission paths were staged. [Recovery matrix and checkout inventory](rc2b-06-b6-impl-01-s2-recovery.md)

| Checkpoint | Before | Result and source |
|---|---|---|
| R — recovery | S1 and an S2 planning handoff | Source and preservation inventories under `recovery/` in the archive |
| A — canonical metadata | Execution manifest and passive selection fields | `InstalledManifest.cs`, `InstalledMetadata.cs` and golden-byte vector. Strict codec, fixed production closure, digest, receipt and record contracts |
| B — trust association | No installed selector | `InstalledFileSystem.VerifyActive` reads the fixed anchor. `InstalledMetadata` selects one record. Portable T20c results below |
| C — filesystem | Existing preparation openat2 primitives | `InstalledFileSystem.cs` and `InstalledPlatform.cs` validate actual descriptors and exact sets. Non-root native positive and adversarial tests passed. Privileged positive pending |
| D — lifecycle | Native build script also produced fixtures | Separate Release builder, guarded install/qualify/remove tools and read-only InstallTool. Nonprivileged publication and interruption tests passed |
| E — regression | Accepted S1 safety baseline | Production binary matrix, Debug, Release runner, actual-Release and D6.4 results below |
| F — preservation | No S2 implementation evidence | Actual candidate archive, canonical manifest, logs and Q1 handoff bound to the tested source |

The installed verifier lives in `src/Gagamba.Execution.Linux.M1/Installed/`. The operator CLI in `eng/InstallTool/` calls the same read-only implementation. Only fixed-root inspection can return installed qualification. Candidate inspection and byte comparison return no trusted selection or release authority. The public descriptor, runtime factory, journal v3, release arbiter, owner/recovery protocols and B1–B5 lifecycle sources have no changes in the implementation diff. The existing supplemental test harness gained the golden resource required by the new tests.

## Exact candidate

| Item | Observed SHA-256 |
|---|---|
| 6,536-byte canonical manifest and artifact-set identity | `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` |
| Candidate payload archive, collected without privilege | `0f4533b77daf2ae3c9d3a4487bc59d6be5be422393049f1fed5a3c0e54f1486a` |
| M1 assembly, same bytes in provider, owner and recovery | `3bc7e5dfd6dce54ac6954f3f88534495aaa662f35b930ef1e1349d091f542154` |
| Static production coordinator, 878,792 bytes | `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b` |
| Generated seccomp filter, 672 bytes | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |
| Independent portable golden manifest vector | `8f9527205a66941dba2da98ee473886315a418a201a3f326fd60193e539054b0` |

The builder archives a single committed revision, publishes the two trusted Release entry points, compares their shared DLLs and copies those bytes into the provider directory. A standalone provider publish produced different M1 bytes during development. Selecting the S1-checked common DLLs preserves byte equality in all three installed locations. Each actual published `.deps.json` has exactly three managed runtime assets in the selected Linux x64 target. The development build graph contains SourceLink → System.IO.Hashing. The published graph omits both and the candidate verifier rejects extra runtime assets. The runtimeconfigs pin Microsoft.NETCore.App 10.0.14 with roll-forward disabled. Git-archive builds can emit SourceLink warnings about absent Git metadata. Commit/tree and the build recipe are recorded separately from the hashes of the produced bytes.

The saved archive was read back independently. Its 14 artifact paths, lengths, modes and SHA-256 values matched the manifest. The archive contains the candidate manifest and payload only. It carries no receipt or operator decision. [Canonical manifest](rc2b-06-b6-impl-01-s2/candidate-manifest.json) · [Retained candidate bytes](rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz)

## Verification and refusals

| Gate or mutation | Observation |
|---|---|
| V01–03, T07, T21 | Golden bytes independently hashed. Source metadata changes the identity. Reordered, duplicate, unknown or noncanonical fields, invalid hashes, roles, types, paths, lengths and self-digest refuse |
| V04–05, T02, T03, T12 | Live fd checks accepted the intact 14-file candidate. Deleted, extra, changed, sparse, FIFO, symlink, symlink ancestor, hardlink and incorrect-mode fixtures refused. Portable tests exercised uid/gid, special modes and directory write rules. Installing a file capability was not attempted |
| Changed fd during read | An unprivileged ptrace probe changed mtime/ctime on the opened M1 DLL at a read syscall. Contents and mode stayed the same. The verifier refused the changed metadata. A second probe substituted a byte-identical generation directory during enumeration and the verifier refused it |
| V06, applicable portion | Actual published managed dependency sets compared to installed manifests. Rehashed extra managed asset, native asset group and wrong framework runtimeconfig each refused. Native loader and full system dependency closure remain outstanding |
| V07–09, T20/T20c/T21 | Active pointer requires lowercase digest and one LF. Directory, manifest and receipt must agree. Native snapshots recheck root, generation, manifest, receipt, pointer and any selected record. Absent or contradictory trust cannot yield a qualified identity |
| V12, T24-G scope | Actual Debug M1 copies substituted across components with newly computed hashes refused on build configuration. A compiled WP05 test coordinator with an updated manifest refused on its binary identity. Neither substituted payload was launched |
| T14 | Eleven transaction tests covered bounded copy, changed source during copy, no-replace publication, interruption before publication, interruption after generation rename, and a concurrent reader on opposite sides of active-pointer publication. All passed in user-owned fixtures |
| T17 and HD-4 | Active, HELD, live owner/recovery, retained stopping authority, unknown and incomplete cleanup conditions refuse removal. Production removal always lacks sufficient holder proof and retains the generation. No cross-generation adoption or automatic collection |
| V10, V11, V13 and later integration | Complete descriptor selection, runtime refusal routing, wire exchange, loaded-assembly checks, process lifetime and pre-RELEASE enforcement require later stages. S2 never returned an operation or invoked a target |

**T20c.** The portable positive control compares a matching production record from a test-owned model of the trusted namespace. The same parser, filename selector and field comparator run in production. Production gets record bytes only from the validated root-controlled file descriptor. The nine cases below are deterministic evidence. No native root-owned acceptance record was created.

| Receipt or selected record | Observed result |
|---|---|
| 1. Fresh receipt | Both association fields null with `unverified-candidate`. Verified installation can report Installed/Unqualified |
| 2. Referenced record missing | No association |
| 3. Wrong actual record SHA-256 | Refused |
| 4. Two records present | Only the exact receipt-named file was evaluated |
| 5. Old selected scope, valid newer record available | Refused, no alternate selection |
| 6. Different artifact-set digest | Refused |
| 7. Different RID or profile | Refused |
| 8. Producer-only record outside trusted namespace | No association |
| 9. Test build or contradictory metadata | Refused |

## Operator boundary

The root-only writer serializes cooperating writers, verifies expected and copied bytes, applies final modes, syncs the staged tree and publishes the generation with `RENAME_NOREPLACE`. A complete temp file, fsync and rename publish `active` last. First installation writes an unqualified receipt. Qualification checks the current staged payload and platform, the named record, and a separate operator decision. The record is durable before the receipt rename. An interruption can leave a retained generation or an unselected record. Conflicting bytes under an existing identifier refuse.

A future authorized administrator must independently provision a reviewed Release publish of `eng/InstallTool`, its runtimeconfig, deps and dependencies, and the reviewed `install_common.py` under `/usr/local/libexec/gagamba-install/`. The fixed host is `/usr/lib/dotnet/dotnet`. The script, verifier and ancestors must be protected from non-root writes. A later operator decision at `/etc/gagamba/accepted-qualifications/<id>.json` must be root:root 0444, with exactly `qualificationId`, `artifactSetSha256`, `qualificationRecordSha256` and boolean `accepted`. The separately reviewed qualification record contains the evidence digest and review commit. This commission did not provision the tool, install a generation, change the real pointer or write either kind of accepted record.

## Regression, commands and evidence

| Executed suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| S1 production build/binary probes at the tested revision | 51 | 0 | 0 |
| Windows M1 Debug | 1,113 | 0 | 270 |
| Windows M1 Release runner | 1,113 | 0 | 270 |
| Windows supplemental actual Release M1 | 1,082 | 0 | 200 |
| Windows six other baseline Debug projects combined | 192 | 0 | 20 |
| D6.4 Debug / Release | 3 / 3 | 0 / 0 | 0 / 0 |
| Fixture SelfTest Debug | 15 | 0 | 0 |
| WSL2 portable S2 subset | 63 | 0 | 0 |
| WSL2 native fd test containing multiple mutations | 1 | 0 | 0 |
| WSL2 nonprivileged transaction suite | 11 | 0 | 0 |

The 63 portable S2 cases are included in the Windows M1 counts. Each of the three M1 regression contexts passed all 61 B5 C3 cases. The platform skips include 269 historical Linux-only M1 cases, the new opt-in native case on Windows, 16 Linux cases and four macOS cases in their respective suites. The supplemental suite skips 199 historical cases and the new native case. Restores used `NuGetAudit=false` for this run because the audit endpoint was unavailable to the sandbox. Early attempts exposed an omitted golden resource and an assumption that the supplemental runner loaded an instrumented assembly. The corrected official runner passed on the tested revision.

Representative reproduction commands follow. Full case names, exact invocations, return codes, binary inventory and individual logs are retained. The raw WSL orchestration initially exited 1, first on a read-only mutation copy and then on the invalid expectation that this host would pass the platform control. The saved candidate was checked again in isolated copies. The completed supplemental probe recorded exit 0 with the host refusal explicitly classified.

```text
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug
./eng/test-production-build.ps1 -EvidenceDirectory <isolated-output>
./eng/test-production-regressions.ps1 -EvidenceDirectory <isolated-source-output>
bash eng/linux-m1/build-install-candidate.sh <canonical-repo> <empty-ext4-output>
dotnet <reviewed-Release-InstallTool.dll> check-payload <output>/candidate
GAGAMBA_S2_TEST_CANDIDATE=<output>/candidate dotnet test <M1-tests.csproj> --filter FullyQualifiedName~InstalledFileSystemNativeTests
/usr/bin/python3 -IB eng/linux-m1/tests/test_install_common.py
```

The verification ZIP has SHA-256 `f4a09b1fd71bb846a7f422bd0963444e33b4b90ed7d3be4210b0071aacc759eb`. Its internal index covers 148 evidence files. The two WSL TRX files are explicitly labeled reruns against the saved source and candidate archives. Original build and test logs are preserved alongside them. The source path inventory is under `verification/`, production probes under `windows/s1/`, regression TRX and commands under `windows/`, and candidate, native, mutation and race transcripts under `wsl/`. [Verification logs and reproduction evidence](rc2b-06-b6-impl-01-s2/verification.zip)

## Independent S2-Q1 commission

Use the retained bytes as the primary review subject. Recompute the external manifest digest and every tar member hash and verify that only the specified 14 artifacts occur. Independently try duplicate JSON keys, source/policy mutations, relabeled test binaries, alternative record selection, unstable fd and directory substitutions, and interruption after each publish step. Check both refusal paths and positive controls. Inspect root-owned ancestor enforcement and the independent approved-decision path before considering a future disposable host privilege gate.

HD-1 privilege remains separately authorized per run. HD-2 keeps public provider selection passive. HD-3 limits experimental native evidence to WSL2 x86-64. HD-4 retains generations. HD-5's invariant-globalization choice awaits actual production compatibility work. This run leaves full ICU/OpenSSL/zlib/loader closure, pinned launches, loaded-assembly checks, identity echoes, privileged positive controls and installed pre-RELEASE lifecycle qualification outstanding. Independent Q1 and owner acceptance are pending.
