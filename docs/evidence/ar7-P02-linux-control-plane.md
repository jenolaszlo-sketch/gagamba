# AR-7 P02 — Linux same-UID control-plane isolation

Date: 2026-10-10 (Asia/Manila). Slice: defensive feasibility only. **Conclusion: SUPPORTED BY THIS PROBE**, limited to this exact host, composition and tested operations.

## Current source and host identity

- Production baseline `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; frozen branch/tag verified unchanged. AR-6 seal does not qualify probes.
- Continuation start `4d70b7969c47766b778727478c240abb661cbc13`; branch `qualification/ar7-feasibility`.
- Harness commit `2b22b89c34d8e6a95a2ad1f331b771aa40d74831`: setup aborted before target release (control address variable shadowing), cleaned completely; NOT RUN as an isolation result.
- Corrected first executed source `c3a67eb0da3096ddbc277232f3ed7d7c29560a05`; bounded follow-up `caadbee51d9e909b9aebfed5f6bce7581bcdab3d`. All committed and clean before execution.
- Ubuntu 26.04.1 LTS, explicit WSL Ubuntu, x86_64, kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, GCC 15.2.0, Python 3.14.4.
- Manager/control/candidate same host UID/GID **65534/65534**, empty supplementary groups, effective/permitted capabilities zero. Candidate namespace identity 0/0 maps to that non-root host identity.
- Root only provisions/observes/cleans disposable resources. Existing account, no new user/daemon/setuid/sysctl/security changes. Root execution is not qualification.
- cgroup v2 RW/`nsdelegate`, actual memory/pids delegation. User namespaces actually start; maximum 30743. Yama 1; owned manager/control opt into ptrace for fixture controls only. LSM **Unknown**, inventory path absent; not inferred disabled.
- Bubblewrap **0.11.1**, mode0755 non-setuid/non-setgid, SHA256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`.
- Follow-up native source hash `7b126d233d5a39485b6e6f4e7d84ac75d4a61705103c658de4e814e0db26672c`; script `4bfff0b5b7bd06915b524f279950fb68604bb785978490ddf5cfea0f36a57e5b`; static worker `6bee9eb0c3fb6dcb52430df5ddba1fe95c23588847cea10cfbff2915deafb2b0`.

## Question and composition

Can this pinned rootless helper run a useful tiny closure while preventing same-UID access to the manager's enforcement resources?

Exact argv and filter binding: [follow-up raw observations](ar7-feasibility/P02/f614e368bab845f68bdcccc453a656d6.json). Mandatory user/mount/PID/net/IPC/UTS namespaces, disable/assert nested userns, cap drop ALL, new session, die-with-parent and clear environment. Read-only static `/target` and `/input`; private proc/dev, 8 MiB tmpfs scratch; empty `/sys`/`/run`; no host cgroupfs. Only prepared anonymous stdio; setup filter FD consumed/closed. No Landlock/separate host UID.

x86_64 cBPF checks architecture and rejects compatibility/x32. Blocks socket/socketpair, ptrace/process-memory/pidfd, namespace and mount authority calls; clone NEW* including time; clone3 returns intentional ENOSYS for libc fallback. Filter SHA256 `30bfaf6b62030de8b105e1ac8de43f2a9884b2bd5de20e98aef81dce426c1aad`.

Independent root-owned outer cgroup: 256 MiB, swap zero, 64 tasks. Delegated inner: 128 MiB/48 tasks with manager/target/sibling leaves. All migration destinations are inside outer. Observer watchdog30s; native alarm12s; CPU12s, FD64, file-size1MiB, core0, fixed finite children/bounded output. These safety bounds do not qualify P04.

## Controls, restricted operations and external observations

| Property | Same-UID positive control | Restricted result / observer |
| --- | --- | --- |
| Useful work | Same executable reads synthetic input | Input32 bytes, scratch32 bytes, prepared output, finite child, exit0 |
| Cgroup migration | Manager creates sibling/moves finite child; ordinary worker moves self and child | Paths ENOENT; external samples keep target and child in target domain |
| Limits/controllers | Ordinary creates cgroup, writes memory/pids/subtree_control and inspects controls | Paths ENOENT, all limits/controllers unchanged; no candidate cgroup |
| Manager proc/FD | Status and exact retained control FD open | Host PID absent from private proc, no recovery |
| Process operations | ptrace attach/detach, process_vm_readv marker, pidfd_open/getfd succeed | Filter EPERM, no effect or recovered control FD |
| Signal | SIGUSR1 succeeds, independent child emits `signal-observed` | ESRCH for invisible host PID; no child event |
| Namespace calls | Helper actually constructs namespaces; setns(-1) outside filter EBADF | unshare USER/NS, all legacy clone NEW* flags plus combined variant EPERM; clone3 filter ENOSYS; setns(-1) EPERM |
| Namespace references/mounts | Owned manager namespace opens; actual ordinary setns EPERM | Manager reference absent; valid init user/pid/net/ipc/mount references open but setns filtered EPERM; cgroup/proc mount EPERM; init-root cgroup path absent |
| Descriptors | Positive recovers a fixture child pipe with pidfd_getfd | Self only stdio/transient scanner; init stdio+eventfd4; eventfd recovery EACCES. Reopening granted output channels works, adds no authority |
| Unix IPC | Both pathname/abstract observers authenticate exact nonce and same-UID peer | socket EPERM, no candidate request |

Actual kernel setns authorization comparison is **INCONCLUSIVE**: positive setns also failed. Invalid-FD control tests filter ingress only. Namespace/mount controls establish helper construction, not unrestricted kernel permission for every variant. ENOENT means mount/PID visibility exclusion, not kernel cgroup ACL denial. No claim that same-UID ownership alone protects delegation.

Root observer, outside fixture domain, samples only fixture PIDs: hostUID65534, private target/child PIDs2/3, cap0, NNP1, seccomp2/filter1. Owned controls unchanged and target domain populated0 after return. Synthetic endpoint observers log nonce/PID/UID/timestamp. Positive gate checked before candidate release.

## Cleanup

All execution stopped/reaped; outer populated0 before known-path removal. No recursive target-provided deletion, unknown descendants or residues. Aborted nonce `495ac440a3a441ed83992b9c25e8fb44`, first `90146f20d9244ef493a718742860b050`, follow-up `f614e368bab845f68bdcccc453a656d6` have separate exact JSON records under `ar7-feasibility/P02`. Each records complete cleanup; [subsequent independent check](ar7-feasibility/P02/independent-cleanup.json) verifies all workspace/cgroup paths and Unix endpoint nonces absent. No watchdog or shared safety failure.

## Luna review, consequence and non-claims

Luna's read-only pre-launch review found missing positive gates, missing NEWTIME, recursive cleanup and overbroad ENOENT interpretation. Accepted/corrected before release. Post-run review supported a narrow result but requested init-root/FD and remaining clone variants; accepted and run in follow-up, with an actual signal-effect oracle. Follow-up review found no material cleanup blocker and supported the same narrow conclusion. Actual setns remains unqualified. io_uring transport variants belong in P03; fspick/openat2 and complete filesystem closure remain unqualified/deferred. No complete syscall-denylist claim.

This exact candidate survives P02 for P03/P04/P07. No general Linux security claim, complete profile qualification, .NET/resource/storage/crash qualification, production launcher selection, or other distro/kernel/LSM coverage. No production implementation, candidate repair, persistent privilege, release or publication.

## Historical 2026-10-09 stop snapshot (superseded scheduling only)

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P02 harness exists and there is **no tested P02 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P02.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P02 native run.
- P02 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P02.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P02 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Can a genuinely non-root, same-host-UID namespace/helper composition prevent target access to manager cgroups, namespaces and resources?

Planned candidate: Linux rootless pinned non-setuid Bubblewrap or small-launcher composition. No Linux experiment, UID switch, delegation, cgroup, namespace, seccomp filter or synthetic daemon was created. Current Linux prerequisites and helper hashes were not inventoried in this assignment. Prior AR-2/AR-6 host evidence is lifecycle-only and is not P02 evidence.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P02; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P02.

## Cleanup

No P02 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P02 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P02 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

Linux same-UID control-plane feasibility remains Pending. Landlock, separate UID, disposable privileged provisioning, VM and Bubblewrap versus bespoke launcher remain decisions for Astra; none was implemented.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P02 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
