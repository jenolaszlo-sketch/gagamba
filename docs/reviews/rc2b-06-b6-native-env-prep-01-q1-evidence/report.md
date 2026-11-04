# Independent native environment plan qualification

**Disposition: PLAN SUPPORTED WITH QUALIFICATIONS.** Ubuntu 26.04 amd64 WSL2 on a D:-resident ext4 VHDX is a plausible experimental architecture, not a verified host or a guarantee that pinned bytes and kernel permissions can be obtained. The plan explicitly stops at the important unresolved dependencies. No native S3 qualification or installation was performed; latest S3-Q1 remains **INCONCLUSIVE — ENVIRONMENTAL QUALIFICATION ONLY**. Findings, scenario reconciliation and owner decisions are in the companion files.

## 1. Source and document binding

Canonical `main` plan commit `af4300f5dff7e4e49fca98233911a44a15894a43`, tree `2d0ece92e20247d3d61ac84e9215837a713e9921`; implementation baseline `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`, tree `75cbd04d009a84aee2d4da591da228a88f0a0cde`. Origin `https://github.com/jenolaszlo-sketch/gagamba.git`; preflight reports tracked/index clean and 13 changed paths since baseline, all documentation. Live origin was baseline; local plan is not pushed. The coordinator captured preservation controls; `document-binding.json` records the exact Git bytes and copies, avoiding Windows checkout line-ending ambiguity.

| Reviewed committed file | Git blob | SHA-256 of blob bytes |
|---|---|---|
| `docs/plans/rc2b-06-b6-native-env-prep-01.md` | `8a17d0159a420078f5ddb883b4c2ae25066ede48` | `4a046cf8b4113937d6cebbd9bff17011a48c8ba52a41b97fe6843f6fdc3927c8` |
| `docs/plans/rc2b-06-b6-native-qualification-matrix.md` | `c27c3629a3c0fb57246eb9424081cd86a2fdc718` | `0f01a7ff381c56fcea22ef478085017b8997c4a17a4305245f347f0430c9e443` |
| `docs/plans/rc2b-06-b6-native-provisioning-runbook.md` | `ce7feba5d05488374f8b711ffbf05d0cf72c35be` | `79a17e277c6bb17328a7cf72c1330e2bb1c4e546ddb91b426b5346203bcae767` |
| `docs/evidence/rc2b-06-b6-native-env-prep-01.md` | `121138770297f5693ce50613f3c6cd7021019c8c` | `9e7d2f1f9be2b24a99d378597071e4086124f81252040c9a540f80db54e6ab7c` |

## 2. Independent review methodology

Fresh-context independent Terra review checked accepted decisions, source, plan, driver flags and preserved final-nonroot disposition, and compared claims to primary platform documentation. `raw-driver-source.txt` and `raw-contract-source.txt` retain direct search observations; committed copies and repository-preservation files provide bindings. Platform pages cited below are availability/semantic evidence, not measurements of the proposed instance. No builds, tests, WSL startup, installation, privileged probes, production or plan edits were performed. The coordinating agent handled operational preservation and final packaging only, not substantive disposition.

## 3. Ubuntu 26.04 environment feasibility

[Canonical](https://ubuntu.com/download/wsl) lists an amd64 Ubuntu 26.04.1 WSL image; [Microsoft](https://learn.microsoft.com/windows/wsl/use-custom-distro) documents `wsl --import` to a selected installation location. This supports feasibility, not the exact image's provenance or digest. Freeze a signed image and verify its actual archive and import format before authorization. Ubuntu lists [glibc 2.43](https://packages.ubuntu.com/resolute-updates/libc6) and [Bubblewrap 0.11.1](https://packages.ubuntu.com/resolute-updates/bubblewrap); package metadata is not an executable identity. Required bwrap SHA-256 is `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`, mode 0755, root-owned, no caps. Exact package revision and extracted binary hash are unresolved; a different revision/version is not an accepted replacement. Coordinator static binary and filter must also match their source pins. An exact package/toolchain acquisition failure stops this path; no evidence now proves it will fail.

## 4. Kernel/glibc/runtime compatibility

Source `AtomicCgroupSpawn.cs` imports `posix_spawnattr_setcgroup_np`, uses `POSIX_SPAWN_SETCGROUP` and rejects libc below 2.43; this sets a stricter implementation floor than merely knowing an API was introduced. [glibc's implementation and ABI change](https://sourceware.org/pipermail/glibc-cvs/2023q3/083001.html) assigns the symbol `GLIBC_2.39`, stores the cgroup fd in the attribute, and uses `clone3(CLONE_INTO_CGROUP)` only when the flag is set; unsupported placement has no migration fallback. This is primary upstream evidence of semantics, not proof of the selected Ubuntu binary's export. Capture `readelf -Ws` and version information for the exact selected libc, then test actual atomic placement and refusal under the final identity. Exact SDK 10.0.114, shared runtime and hostfxr 10.0.14, compiler 15.2, static libc and linked native packages must be acquired and checked, not inferred from a release number. [Ubuntu dotnet source metadata](https://packages.ubuntu.com/source/resolute-updates/dotnet10) and [.NET release record](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.14/10.0.114.md) indicate candidate versions only.

Source inspection can establish requested flags and failure handling, not actual kernel support or LSM/user permission. Bounded native tests under the real non-root identity with empty CapEff/CapPrm must exercise `openat2` resolution restrictions, pidfd open/signal/wait and `SO_PEERPIDFD`, credential identity (`SO_PASSCRED`/`SCM_CREDENTIALS`), `SCM_RIGHTS`/`MSG_CMSG_CLOEXEC`, namespace and ancestry identity, subreaper adoption/reaping, seccomp allow/deny and user/mount/PID/network/IPC/UTS isolation, plus atomic cgroup placement and negative controls. WSL kernel version/config alone proves none of these. No such probe was executed here.

## 5. Filesystem trust assessment

[Microsoft documents](https://learn.microsoft.com/en-us/windows/wsl/disk-space) WSL2 distributions in ext4 VHDX files. Storing that VHDX on D: differs materially from placing `/opt/gagamba` on `/mnt/d` DrvFS. The plan requires guest ext4, actual mount/ancestor inspection, 0:0 root-owned non-writable ancestors and root-owned protected installation, and checks symlinks, hardlinks, mount boundaries, atomic rename, directory sync and stable bytes. These are sensible prerequisites, not observations of the new instance. Host Windows administrator access remains outside the Linux trust boundary. Existing development WSL `/` 0777 correctly refuses; do not chmod it or weaken the verifier.

## 6. Cgroup delegation assessment

[Kernel cgroup v2](https://docs.kernel.org/admin-guide/cgroup-v2.html) constrains controller enablement and hierarchy; [systemd](https://www.freedesktop.org/software/systemd/man/devel/systemd.resource-control.html) documents `DelegateSubgroup=` from version 254 and explains why the supervisor should occupy a leaf. Proposed P/supervisor and empty P/domains avoid the no-internal-process conflict only if the administrator sets them up correctly. `Delegate=` by itself neither places the supervisor nor proves controller availability or writable target/common-ancestor authority for atomic spawn. Verify actual ControlGroup, permission and membership, memory/swap and PID limit writes/readbacks, atomic child placement, kill/events/procs readback, stop/reap, recovery lifetime and an outside-process isolation control. Operator must review a literal unit, service lifetime and outer limits, including effects on supervisor/recovery as well as workload. No broad cgroup root grant or extra runtime subtree-control grant is proposed. WSL feasibility and least-authority containment remain an explicit P05/N2 gate, not established.

## 7. Native loader closure

The plan binds managed assemblies and deps/runtimeconfig to the candidate, exact `/usr/lib/dotnet/dotnet`, 10.0.14 fxr/shared runtime and clean launch environment. It requires ELF interpreter and recursive `DT_NEEDED` inventory and actual owner/recovery maps, mapped identity and exercise of dynamic loads. ICU is required by the present default path; accepting invariant globalization under HD-5 requires separate compatibility proof and configuration decision, not an ambient environment override. Exercise OpenSSL and zlib when consumed and record actual resolved libraries; absence of a feature must be justified, not inferred from `ldd`. Static inventory alone cannot prove full loaded closure.

## 8. S2 candidate and approval chain

Historical S2 digest `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` is not the S3 candidate. New source/tree-bound Release candidate, test-only exclusion, manifest and exact installed bytes, digest, protected ownership, review and independent approval are required. The F02 distinction is preserved: independent administrator accepts exact digest and qualification record hash under `/etc/gagamba/accepted-qualifications`; runtime uses receipt-selected protected `/opt/gagamba/qualifications`, not the administrative store. Installation produces an unverified candidate, not a native pass. A record requiring installed positives before Qualified launch is circular; runbook §5 explicitly stops for a truthful staged-publication owner decision. No such protocol is proven or authorized here.

## 9. Installed-profile test driver readiness

Production provides `InstalledArtifactInspection` → `InstalledFileSystem.VerifyActive`, `LoadedInstallation` rechecks, internal `Bootstrap/QualifiedLauncher`, and `M1RecoveryScope`'s qualified owner launch; the public production factory remains unavailable. `qualify-s3-completion.py` drives `B6TopologyProbe` via `--b6-topology`; its fixture uses QualifiedLauncher machinery but synthetic identity. Loaded and production-refusal scripts exercise bounded checks, and `qualify-b2.py` invokes actual legacy v1 `/usr/bin/dotnet` processes, including two delegated cases when given `--delegated-parent`. `B6IntegratedProbe` is not a full HELD lifecycle. No existing entry point demonstrates actual protected provider plus installed B2/B3 positive. A separately commissioned and independently reviewed test-only driver must obtain identity through real installed verification and launch exact R/O, observe authentication and lifetimes, and provide integrated DirectSession/staging/HELD/release/stop/journal/C3 cases without altering production authority. It must precede those tests and any record claiming them, not necessarily basic separately authorized import or platform probes. Exact case commands are consequently future gates, not currently reproducible recipes.

`InstalledArtifactInspection.InspectActive()` is public but returns an inspection string, not launch authority. `QualifiedLauncher.InstalledPlan.Prepare(LoadedInstallation, role)` and its `Spawn` are internal; production assembly explicitly grants internal access to the existing `Gagamba.Execution.Linux.M1.B2Controller` fixture assembly. This makes a test-only extension plausible without opening a public factory or changing production authority, but visibility alone does not establish that a new harness can compile, load the exact protected provider bytes and satisfy all identity, loaded-role and integrated profile prerequisites. Those build and runtime constraints remain to be demonstrated by the separately reviewed implementation; a copied/instrumented provider or supplied synthetic identity would not answer them.

## 10. Native qualification matrix reconciliation

The companion matrix maps T01–T25, T20a/b/c and latest Q-E1/E2/E3 to positive controls, mutations, observations and blockers. It retains root-installed S2, loaded closure, B2 original pidfd and association, owner handshake, selector changes, loss and descriptor custody, two legacy delegated cases, integrated B3 HELD, owner-only physical RELEASE, recovery stop, controller loss, original C3 deadline, journal and cleanup. Required evidence includes actual pids/pidfds and frames, no target dispatch on refusal, readback and cleanup classification. Matrix descriptions are not runnable coverage where driver/fixture/commands do not yet exist. The dedicated 4096-byte evidence-reader `MSG_CTRUNC` branch was not directly forced and stays pending; synthetic or different-parser success cannot promote it.

## 11. Stopping and teardown correctness

Plan retains owner-only at-most-one physical RELEASE, recovery stop-only, original unrenewed deadline, no cross-generation adoption or reconstructed disk authority. Stop admission, preserve holders, observe stop/ack, actor exit/reap, journal and cgroup state, record Confirmed/Unknown/Failed before environment disposal and export evidence. Unknown/live/HELD state retains resources absent a distinct owner decision; WSL destruction is not proof of cleanup. Immutable generations remain retained; no automatic GC.

## 12. Local resource practicality

Producer's read-only snapshot reports ~15.52 GiB memory, ~2.17 GiB free and ~507.9 GB free on D: at that instant. These are historical measurements, not current availability or measured build peak. WSL and Docker compete for host memory; set an authorized outer service envelope after measuring build/runtime/helper concurrency, swap, logs and evidence volume. VHDX, SDK/static toolchain and repeated snapshots consume storage; recheck free space and reserve, record peak and teardown cost. No specific capacity is promised. Runtime limits must leave recovery and supervisor able to stop and record evidence.

## 13. Findings

No BLOCKER, MAJOR or MINOR demonstrated. Five observations in `findings.md` cover exact inputs, delegated authority, bootstrap sequencing, driver, closure/truncation/capacity. They remain stop gates rather than assumed successes. A failure of a future gate may require a new decision and could change feasibility.

## 14. Required remediations

No correction to the existing plan is required before considering a **narrow frozen-input preparation** commission. Before the dependent operations: freeze and verify exact platform bytes; review a literal bounded service and probe; independently implement/review the installed driver; settle a truthful staged approval sequence if needed; specify executable case commands and complete evidence. No pin substitution or self-issued qualification.

## 15. Remaining executable/environmental gates

Image digest and provenance, binary/package/toolchain pins and reproducibility; actual protected filesystem and atomic durability; non-root syscall/namespace/seccomp/pidfd/credential behavior; delegated cgroup topology, limits and isolation; measured resources; new artifact candidate and independent F02 approval; actual loaded closure; installed-driver positives and mutations; all native outcomes, cleanup, and explicit `MSG_CTRUNC` coverage remain pending. Each privileged stage, test mutation and teardown needs separate owner authorization.

## 16. Final disposition

**PLAN SUPPORTED WITH QUALIFICATIONS.** No demonstrated mandatory design conflict was found. The proposal is feasible in principle only; exact dependency availability and host behavior may still defeat it. It is not approval to provision, execute, publish or accept S3/B6. Recommend the bounded preparation and separate driver commission in `owner-handoff.md`.
