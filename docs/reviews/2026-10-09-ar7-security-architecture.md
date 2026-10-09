# AR-7 restricted execution: independent architecture review

Date: 2026-10-09, Asia/Manila. Reviewed production source: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`. Frozen branch: `qualification/ar6-native`; annotated review tag: `ar6-review-baseline-06c8ac7`. This report is a **proposal and static review**, not an implementation or native security qualification. It is maintained separately on `review/ar7-architecture`.

Read with the [ordered probes, Sol slices and adversarial matrix](2026-10-09-ar7-probes-and-handoff.md) and [review evidence record](../evidence/restricted-execution-AR-7-review.md). Together these supply deliverables A–K. Original F01 remains open. No production change, provider, service, policy engine, release or security claim is authorized by this review.

## A. Executive recommendation

1. **A useful restricted profile is credible, but its architecture is not ready for implementation.** The missing evidence concerns ordinary native attack paths, not only theoretical kernel exploits. OS documentation establishes candidate mechanisms; it does not establish their composition in Gagamba.
2. **Probe Linux and Windows independently.** Linux has a comparatively direct path through a qualified unprivileged namespace helper plus protected cgroup controls. Windows needs a qualified least-privileged AppContainer configuration and a solution for all writable storage. Neither platform is currently qualified for this profile. A Linux-only first release is preferable to weakening the profile. macOS should refuse the first profile.
3. **Prefer OS enforcement around the existing lifecycle substrate.** On Linux, first compare a pinned, non-setuid Bubblewrap adapter with a small bespoke launcher; prefer the established helper if it meets the complete contract. On Windows, investigate documented AppContainer/LPAC facilities, explicit closure ACLs and Job Objects. Retain a VM-backed worker as the fallback architecture if host IPC or writable-storage authority cannot be bounded. This is a platform-specific candidate family, not a universal provider rewrite.
4. **Do not add a privileged broker now.** The leading Linux candidate needs user namespaces and pre-existing cgroup controller delegation; that may require administrator provisioning, but not a new Gagamba root service. Windows may need administrator-provisioned bounded storage. If non-admin operation cannot meet the profile, refuse, then bring the broker/VM tradeoff back for a decision. A trusted unprivileged setup helper is not a privileged service.
5. **First profile: `restricted-offline-closure-v1` (proposed).** Run a supplied native tool closure over staged inputs, with fresh writable scratch, no IP networking, no host-service IPC, no caller credentials or ambient handles, immutable preparation, protected control resources, mandatory process/memory/time/output/storage bounds and confirmed domain cleanup. Copy selected regular output files out after stop. No host-tree write-through.
6. Intentionally unsupported: online restore, host agents/daemons, GUI/interactive desktop, network shares, device access, arbitrary in-place host edits, arbitrary installed SDK discovery, dynamic authority widening, transparent recovery/resume, unqualified OS/helper versions, and macOS restricted execution. Shells can be explicit tools inside the closure; they acquire no additional authority. Runtime compatibility is separately qualified.
7. **Highest-risk assumption:** all authority reachable through the chosen runtime baseline can be bounded without an accessible host broker or writable control object. Windows AppContainer ambient ACL access and service RPC are the sharpest version of that assumption. Linux namespace/descriptor escape and cgroup reachability must also be proved, including same-host-UID attacks.

The minimum profile is deliberately stricter than trusted lifecycle execution. Existing lifecycle entry points retain their documented behavior. A new required profile must never fall back to them.

## Review basis: what exists and what does not

| Evidence class | Inspected basis | What it establishes |
| --- | --- | --- |
| Current production | [Provider contract](../../src/Gagamba.Execution/Provider.cs), [capabilities](../../src/Gagamba.Execution/Platforms.cs), Windows/Linux/macOS provider implementations, native resource owners, capture and orchestrator | Explicit lifecycle negotiation, invocation copies, owned native domains, execution-owned completion, bounded opt-in Windows/Linux output. No filesystem/network/identity authority manifest or enforcement. |
| Current release evidence | Frozen `06c8ac7` seal from [run 37902043866](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/37902043866), retained archive and manifest described in review evidence | Three native lifecycle and installed-package paths at that exact SHA. No AR-7 qualification. |
| Slice evidence | [AR-1](../evidence/windows-lifecycle-AR-1.md), [AR-2](../evidence/linux-lifecycle-AR-2.md), [AR-3](../evidence/macos-lifecycle-AR-3.md), [AR-4](../evidence/invocation-contract-AR-4.md), [AR-5](../evidence/execution-orchestration-AR-5.md), [AR-6](../evidence/release-qualification-AR-6.md) | Historical repairs and their exact host/source limits. AR-6 document records the earlier `7660f16` candidate; the later frozen source has its separate seal. Neither record is rewritten here. |
| Current tests | Lifecycle regressions, AR-4 snapshots, AR-5 capture/orchestration, conformance scripts/runner/matrix and broken-provider controls | Useful ownership, cancellation, denial-of-launch and non-vacuous lifecycle oracles. They do not test a complete hostile-authority boundary. |
| Review history | [Original review](2026-10-08-independent-review.md), [remediation plan](../review-remediation-sol.md) | Original F01 readiness gap; E10 root migration is measured history, not proof of unprivileged migration denial. |
| Historical experiments | GW-1B BaseContainer/AppContainer and GQ-1; GL-1A and GM-1A; their archived evidence | Inputs to feasibility. Experimental Windows isolation is not the shipped Job Object provider. A successful experimental build does not qualify its schema/API for a production profile. |
| Aspirational contracts | [Security model](../security-model.md), [execution-domain](../execution-domain.md), [design rules](../design-rules.md), fixture/testing plans | Requirements and constraints, not enforcement. Security-model grant-overlap text is internally inconsistent; the new proposal eliminates overlap rather than inheriting an ambiguous rule. |

Static anchors at the frozen revision: Windows `LaunchInner` uses ordinary `CreateProcessW`/extended startup plus suspend/assign/resume; `OwnedJob.Create` configures kill-on-close, not authority restrictions. Linux `LaunchInner` sets `POSIX_SPAWN_SETCGROUP`, installs captured stdout/stderr, then closes descriptors >=3; it does not change identity or namespaces. `LinuxCapturePipes.AddActions` leaves descriptor 0 inherited, so AR-5 captured launch is not yet an explicit stdin-authority boundary. `OwnedCgroup` operates on host paths. macOS `OwnedJob.WritePlist` creates a user-domain job; `LaunchCaptured` refuses. `ExecutionOrchestrator` starts its deadline wait after synchronous preparation/launch and exposes negotiated lifecycle strings. AR-4 copies invocation values at launch; that does not bind an authorized executable object to a preparation.

No new hostile workload was executed in this review. “Candidate,” “must,” and “proposed” below describe requirements or inferences. “Observed” refers only to the cited repository evidence. External references were checked on the review date; moving upstream branches are research references, not dependency pins.

## B. Threat model

The adversary controls the initial executable, project content, scripts, interpreters, loaded libraries, arguments, configuration and every descendant after release. Assume it deliberately invokes native APIs and initially would possess the caller's ordinary OS authority if launched without restrictions. The caller, Gagamba manager, qualified helper, runtime-baseline compiler and OS kernel are trusted. The target cannot be trusted to cooperate, use a VFS, obey a timeout or disclose its own state honestly.

Trusted preparation receives hostile input bytes. It must not execute input hooks, dereference arbitrary input links, or reuse target-controlled configuration as launcher configuration. The host controls source ancestry during copying and the staged closure thereafter. Unrelated malicious same-user host processes actively rewriting trusted manager files are excluded; attacks by the target on those same-user processes and files are **included**. Same UID is therefore not a trust boundary.

| Threat | In scope? | Required mitigation | Platform mechanism candidate | Qualification test |
| --- | --- | --- | --- | --- |
| Read/write caller home, sibling workspaces, absolute paths, SSH/cloud/git/package/browser credential stores | Yes, with synthetic sentinels | Default deny; staged RO closure; private RW scratch; no host-home grant | W: LPAC/ACLs; L: isolated mount tree | Q01–Q03, Q05 |
| `..`, symlink/junction/reparse, hardlink, mount/alias, rename/replacement races | Yes, target and input controlled | Copy regular objects; stable prepared bindings; runtime kernel checks; no live arbitrary mounts | W: protected staging and handle-aware traversal; L: bound closure FDs/mounts, no namespace authority | Q02–Q04 |
| Environment, startup files, PATH/DLL/loader hooks, credential helpers | Yes | Explicit environment and runtime closure; absolute trusted helper; no host search | Both: immutable manifest and protected closure | Q05, Q06 |
| Inherited files, connected sockets, tokens, process/job/cgroup/namespace handles, terminal | Yes | Only prepared data channels; close all setup authority before exec | W: handle list; L: close/allowlist plus helper FD audit | Q06 |
| Internet, LAN, host and private loopback, IPv4/6, DNS, simulated metadata, proxies | Yes | No IP communication, including listeners and inherited transports | W: network isolation with no grants/exemptions; L: net namespace plus socket denial | Q07–Q09 |
| Named pipes, Unix pathname/abstract sockets, SSH agents, Docker/build daemons, D-Bus/Mach services | Yes | No host-service IPC; tightly described inert runtime exceptions only | W: LPAC/object ACLs and broker surface proof; L: no sockets/host mounts, IPC namespace | Q10, Q11 |
| WMI/COM/RPC/service/task/shell brokers creating durable work outside domain | Yes | Deny request path and transferred authority; job membership alone insufficient | W: restricted identity/IPC; L: absent host daemon sockets | Q11, Q12 |
| Direct spawn, breakaway, setsid, double-fork, namespace creation, cgroup migration | Yes | Inherited restriction + kernel ownership + protected controls | W: Job; L: cgroup + PID namespace + seccomp | Q12, Q13 |
| Inspect/inject/signal caller, manager or another execution | Yes | No useful process handles/host PIDs; deny ptrace/process-memory operations; isolate control identity | W: token/object/desktop restrictions; L: PID/user namespace, seccomp, optional Landlock | Q14 |
| Modify launcher, policy, recovery records, cgroup tree, job handles or helper channels | Yes | Target has neither path nor handle authority; authenticated manager-only control | Both: inaccessible control objects | Q15 |
| Process/thread/memory/output/disk/CPU/FD or handle exhaustion | Yes, first-order containment | Mandatory finite budgets; prevent host-disk flood; bound manager work; document residual kernel-resource risk | W: Job + bounded storage; L: cgroup + bounded tmpfs + rlimits | Q16–Q18 |
| Survive cancel, deadline, root exit, caller/manager/helper death | Yes | Owned stop; independently armed death path; conservative cleanup | W: atomic job ownership; L: protected guardian/PID namespace + cgroup | Q19–Q21 |
| Persist through files, scheduled effects, stale profiles, reboot | Yes, for reachable effects | No host writes/brokers; staged artifacts only; journal and reconciliation | Both: protected ownership records, explicit residue status | Q22, Q23 |
| Lie through stdout/exit status or malicious export filenames | Yes | Trusted external oracles; receipt from manager; safe regular-file export | Both | Q04, Q24 |
| Kernel, administrator/root outside execution, hypervisor, physical attacks | No | Trusted platform assumption; patched qualified host | Deployment prerequisite | Record scope; do not simulate as profile pass |
| Timing/cache side channels, total host availability, secure deletion, reversible effects | No | Explicit claim limits; no arbitrary secrets granted | None claimed | Verify documentation/receipt does not claim these |

Output is an authorized disclosure channel. Denying network cannot prevent disclosure of granted input bytes to the trusted stdout recipient. Exported artifacts are untrusted content, never automatically executed or merged.

## C. Proposed profile: restricted-offline-closure-v1

### Contract and first workload

This is one required bundle with inspectable effective restrictions, not independently optional flags. Every provider must meet the same authority ceiling. A missing component produces pre-release `Unsupported` or `FailedToEstablish`; no partial acceptance. More restrictive effective limits must be reported before launch and accepted as part of the immutable request, never silently substituted.

The first workload is a noninteractive native command consuming staged files and producing bounded output/artifacts. A self-contained .NET or NativeAOT closure is a qualification workload, not an assumed entitlement to the user's installed SDK. Ordinary consumers specify the tool/runtime closure, inputs, scratch allocation and budgets. They do not specify Job, cgroup, namespace, token or seccomp flags.

Illustrative intent, **not a proposed public API**:

```text
requiredProfile: restricted-offline-closure-v1
tool: prepared tool closure / bin / analyze
runtime: prepared runtime manifest and digest
arguments: ["/inputs/source", "/workspace/result"]
inputs: copied read-only source tree
workspace: fresh disposable allocation
network: none
hostIpc: none
memoryBudget: 128 MiB (native accounting explicitly reported)
maxConcurrentProcesses: 9 (root plus at most eight others)
wallDeadline: 30 seconds
stdout/stderr: 1 MiB each; overflow stops execution
scratch: 64 MiB data, finite object/metadata bound
export: selected regular files, at most 16 MiB total
```

A runtime that cannot start within these budgets fails; it does not receive larger budgets automatically. Linux task-count enforcement can be stricter than process-count enforcement; see resources below. Names such as `/inputs` denote logical locations, mapped to reported platform paths.

### Identity and filesystem

| Element | Mandatory meaning |
| --- | --- |
| Execution identity | W: unique execution AppContainer/LPAC identity with no elevation, UIAccess, impersonation or ambient application capabilities. L: user-namespace identity with all usable capabilities removed before target release; host UID mapping is disclosed and is not claimed as a separate account. Separate host UID is an alternative deployment, not an automatic requirement or sufficient standalone mechanism. |
| Initial readable authority | Prepared tool, runtime and input copies, plus a minimal versioned OS baseline. No caller home, arbitrary host `/usr`, whole drive-root contents, package caches or host configuration. Baseline resources must enumerate purpose and authority; a broad sensitive grant cannot be excused by naming it “baseline.” |
| Runtime closure | Trusted preparation copies/verifies supplied binaries, native libraries, interpreter modules, explicit configs and helper tools. No online dependency discovery, arbitrary package installation or executing a build to discover dependencies. Runtime diagnostic IPC/profilers/startup hooks disabled where unnecessary. Missing dependency is a compatibility error. |
| Read semantics | Read/enumerate/execute within closure. Read authority is not an executable allowlist. Arbitrary native code may run within the restrictions, including code generated in scratch if the platform supports it. |
| Write semantics | Fresh scratch only, including synthetic HOME/TEMP/cache/profile state. No live RW host input binding. Inputs requiring edits are copied into scratch under the same budget. System-created writable app state must also be bounded and disposable. |
| Object classes | Initial inputs are copied ordinary files/directories. Reject symlinks, junctions/reparse points, special devices, sockets, FIFOs, network shares, alternate streams, unsupported mounts and overlapping logical roots. Copying a multiply linked regular input makes a new object; never hardlink it into staging. Preserve neither setuid/capability metadata nor source ACL authority. |
| Runtime link behavior | Attempts may succeed only if resulting object access remains inside authority. A link to outside must not confer read/write/enumeration. Hardlinks between RO and RW objects must not upgrade RO authority. Namespace/mount manipulation denied. No reliance on lexical prefixes. |
| Preparation integrity | Host owns staging ancestry; target cannot write closure, launcher, manifests or their parents. Resolve opened objects without following uncontrolled links; bind digest and object identity. Protect against mutation after hashing; hash alone is not a lock. No target code runs before final binding and establishment. |
| Export | After confirmed domain death, manager walks owned scratch without following links; copies selected ordinary files into a fresh caller-owned output directory under count/byte/path limits. Reject devices, reparse objects, aliases, traversal and overwrite requests. Do not preserve ownership/security metadata. Do not interpret artifacts as control records. |

Live host mounts and cross-kind nested grants are unnecessary for this first profile. They remain unsupported rather than choosing between conflicting historical “reject nesting” and “inner wins” rules. The original proposal and evidence stay intact; a later accepted contract must clearly supersede only the relevant normative sections.

### Network, IPC and inherited authority

No IPv4 or IPv6 connections, datagrams, listening, name-service egress, multicast, metadata requests, proxy connections or intra-execution IP loopback. Removing proxy variables is supplementary. The profile permits prepared stdin/stdout/stderr pipes and private anonymous descendant pipes/shared memory whose peers are confined. It does not promise arbitrary local IPC compatibility. No host named-pipe, Unix socket, D-Bus, Mach/XPC, COM/RPC or service endpoint may supply undeclared host authority. Necessary OS-internal runtime services require a reviewed, versioned exception whose reachable operations cannot violate this ceiling; broad activation brokers are not acceptable exceptions.

Environment is a finite explicit map. Synthetic HOME/TEMP and runtime variables are part of the prepared report. Ignore neither extra OS variables nor registry/profile lookup paths: inspect their effective authority. No credential values, SSH agent paths, proxy configuration or caller PATH are added. Only prepared stdio and independently audited essential inert handles cross the final target exec. Stdin is closed/EOF in v1; adding finite input bytes later must retain the same bounded data-channel semantics. A TTY, connected socket or manager pipe is not “stdio” merely because it occupies descriptor 0, 1 or 2.

### Lifetime, resources, control and recovery

- Preserve execution-owned terminal state, observation-only cancellation, single-use preparations, release-versus-stop linearization and conservative cleanup. A restricted adapter owns a trusted setup launcher through the established lifecycle layer. The launcher must be confined before it invokes target code. A handshake from target stdout is not establishment evidence.
- All release prerequisites, including an unexpired deadline and an armed owner-death mechanism, must be checked before release. Setup has a separate finite budget. The execution deadline is monotonic and remains active even if the API caller disappears. Root exit may leave descendants running within the still-owned domain until they finish or hit the deadline; this preserves Windows/Linux lifecycle semantics. No “root exited” receipt implies domain emptiness. The older security-model preference to stop children at root exit is not silently applied to legacy APIs.
- Mandatory budgets: finite maximum concurrent processes, aggregate native memory charge, wall time, stdout/stderr, writable storage and manager-side artifact/diagnostic processing. Requested unsupported bounds refuse. Suggested starter defaults are the example above; production maxima await qualification.
- Memory is **not portable RSS**: Windows Job commit and Linux cgroup memory accounting differ. The report must name charged categories, shared/file-backed/kernel exclusions and swap treatment. Linux should disable workload swap and bound memory-accounted tmpfs; Windows must test mapped/file-backed and kernel allocation residuals. A consumer requiring identical physical-memory accounting gets `Unsupported`.
- Process limit means all concurrently alive target processes, including detached children, at most N. Linux `pids.max` counts tasks/threads. A conservative task cap at N, including helper tasks sharing that group, can enforce this ceiling but may make .NET unusable. Adding helper allowance is safe only with a proven separate target limit or non-transferable reservation; a helper exiting must not free extra target process slots. Raising the cap to N+threadAllowance does **not** still prove an N-process ceiling. P04 decides whether first Linux support accepts the conservative limit, needs a different workload budget contract, or requires a different mechanism. No polling process counter may enforce a hard limit.
- Disk/storage is mandatory because deadline/output limits do not stop rapid disk fill. Include every writable place, including Windows AppContainer profile/registry persistence and metadata, Linux tmpfs/inodes/shared memory, and manager logs. An unbounded host temp directory fails this profile. Finite storage backing can bound metadata without an exact portable file-count quota; disclose the difference and cap export counts independently.
- CPU-rate quotas are deferred; a finite deadline bounds duration but not CPU consumption within that duration. Linux optional `cpu.max` and Windows Job CPU-rate policy can be investigated without a universal quota API. Threads and descriptors/handles need small finite stress tests; Linux task/FD limits help. Windows lacks a demonstrated universal per-domain handle/thread ceiling here. This residual kernel-resource DoS risk prevents a hostile multi-tenant availability claim. If it defeats even first-order host usability in bounded probes, select a VM or refuse; do not hide it in a footnote.
- Target cannot name/open/modify controlling cgroups, ancestors/siblings, security descriptors of control objects, Job handles, namespace FDs, launcher binaries/configs, release channels, receipts, cleanup journals or other executions. Managers retain object-bound control rather than trusting target-supplied paths/PIDs. No writable control filesystem is exposed.
- Caller death, manager crash and launcher crash must stop released target work. Windows last-handle kill needs leak/duplication and pre-assignment crash probes. Linux needs a protected, correctly armed owner-death chain and PID-namespace termination semantics; current cgroups alone provide neither. Record constructed guarantees separately from native ones. No finite wall-clock guarantee during kernel/host suspension or failure is promised.
- Cleanup has separate `DomainStopped`, `ControlResourcesReclaimed`, `ScratchReclaimed` and `ExportsCompleted` evidence. Overall success requires the applicable components. A stop request, lost observer, reboot, missing path or expired wait never proves all four. Failed/unknown cleanup retains protected ownership debt and blocks unsafe reuse. No automatic re-execution or resumption after restart.
- A minimal protected recovery record identifies execution nonce, host boot identity, object identities, profile/manifest digest, setup phase and owned resources without secrets. Restart reconciles only these resources. After reboot, prior process lifetime ended but scratch/profile/journal residue still needs inspection; do not reuse PID or path strings as authority. No receipt can be delivered by a dead manager: reconciliation produces a new outcome with uncertainty about the interrupted run. Secure erasure and rollback of exported effects are excluded.

## D. Platform architecture and alternatives

### Windows: LPAC/AppContainer candidate, with two blocking gates

Microsoft documents AppContainer creation for unpackaged applications and distinguishes it from integrity level. Low integrity normally blocks writes upward; it is not a default-deny read policy. A restricted token and ACLs help reduce authority but do not by themselves establish offline operation or deny every host broker. [AppContainer for legacy apps](https://learn.microsoft.com/en-us/windows/win32/secauthz/appcontainer-for-legacy-applications-), [Mandatory Integrity Control](https://learn.microsoft.com/en-us/windows/win32/secauthz/mandatory-integrity-control).

Ordinary AppContainer is not the candidate's final permission boundary: Windows resources can be exposed through `ALL_APPLICATION_PACKAGES`. Microsoft identifies LPAC as removing that ambient access. That documented distinction supports investigating LPAC; it does not prove a supported creation recipe on every Windows build or eliminate `ALL_RESTRICTED_APPLICATION_PACKAGES` and service-specific grants. [Token information reference](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ne-winnt-token_information_class). **P01 must pin the supported API/header/build contract and prove the actual token and access checks.** No inferred native API constant or private token field becomes a production dependency without review.

Candidate establishment sequence:

1. Allocate unique execution identity, protected control records, closure copies and finite writable storage; identify every profile/registry write location. Set ACLs only on Gagamba-owned objects. Never rewrite host-home/system ACLs to make a tool run.
2. Configure the Job's process/memory limits, no breakaway and kill-on-close; prepare only three data handles. Investigate job-at-create via `PROC_THREAD_ATTRIBUTE_JOB_LIST` to close the suspended-unassigned crash interval. Preserve AR-1 admission/cleanup ownership; reject incompatible outer Jobs. The documented startup attributes supply security capabilities, handle lists, mitigations and job assignment, but the combined behavior needs native fault injection. [Startup attributes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute).
3. Create restricted process before release; inspect effective token and Job membership from the trusted side. No network capability or loopback exemption, no full-trust activation. Standard Windows network isolation must be enabled and demonstrated on that host; refusal if unavailable. [Network isolation troubleshooting](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/troubleshooting-uwp-firewall).
4. Use a noninteractive window station/desktop or a proven equivalent; consider Win32k disable and compatible exploit mitigations. These reduce attack surface and UI authority; they are not substitutes for token/IPC isolation. Do not prohibit dynamic code by default and then claim .NET JIT compatibility. Test console and runtime startup without sharing the caller's desktop.
5. Validate closure binding, limits, capture, deadline and cancellation under the same final release gate. Report effective profile before release. Finish with Job emptiness plus separate storage/profile/control cleanup.

**Host IPC gate:** inspect and test object namespace access, named pipes including permissive Everyone/AAP/ARAP ACLs, COM/RPC activation, WMI, SCM, Task Scheduler, shell activation and caller-process handles. A synthetic broker must prove both denied connection and denied delegation of work. A failure cannot be fixed merely by listing the broker as a runtime requirement. Jobs own direct descendants; Microsoft explicitly distinguishes WMI-created processes from that membership. [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects).

**Storage gate:** Job memory is not a disk quota. An LPAC's writable app profile or registry can remain outside a bounded workspace. Until P04 accounts for every writable object and proves a practical bounded store, this profile must refuse on Windows. A pre-provisioned finite volume may help scratch but does not automatically move or bound app profile/registry state. A VM with a fixed guest resource envelope is the alternative, not a polling disk-size watcher. [Job memory accounting](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information).

Privileges/setup: aim for ordinary-user execution with no persistent Gagamba service. Storage provisioning, OS feature installation or special token APIs may require operator setup; establish this experimentally. Refuse elevated target identity. Trust OS enforcement, protected manager/launcher and immutable runtime closure. Known incomplete alternatives: Jobs alone, low-integrity-only, default AppContainer with broad grants, experimental BaseContainer schema without support/qualification, and same-user ACL-only isolation. No safe complete implementation has yet been identified for the proposed Windows storage-plus-IPC contract.

### Linux: unprivileged namespace helper candidate

Prefer probing a pinned non-setuid Bubblewrap executable invoked by absolute trusted path over immediately writing a new namespace/mount launcher. Upstream explicitly places security-policy responsibility on the caller; its default invocation is not a qualified profile. Namespace construction and existing helper maturity can reduce new native code, but Gagamba must own arguments, filters, grants, limits and evidence. [Upstream security model](https://raw.githubusercontent.com/containers/bubblewrap/main/README.md).

Candidate minimum composition:

- User namespace for unprivileged setup; private mount and PID namespaces; private IPC namespace; private network namespace. UTS isolation is optional hygiene; cgroup namespace hides a view but is not migration enforcement. Drop effective/permitted/inheritable/ambient capabilities and prevent regain; set `no_new_privs` before target exec. Do not run the target as host root. Supplementary groups, setuid bits, file capabilities and namespace-root effects require probes. [User namespaces](https://man7.org/linux/man-pages/man7/user_namespaces.7.html), [no_new_privs](https://man7.org/linux/man-pages/man2/PR_SET_NO_NEW_PRIVS.2const.html).
- Empty root populated from protected closure copies, RO input/runtime bindings, bounded private tmpfs scratch/HOME/TEMP and minimal devices. No host `/proc`, `/sys`, `/run`, cgroup mount, user bus, SSH-agent or Docker socket. Mount only a filtered/new PID-namespace proc view if the runtime needs it; otherwise omit. Remove old-root and setup FDs. Bind mounts are not immutable when another writer can change their source: trusted staging is still necessary.
- Architecture-specific seccomp allowlist installed before target code, rejecting unknown syscall ABI and alternate x86 compat entry paths. No socket creation/connection except narrowly proven private mechanisms; prefer anonymous pipes and deny `socket`/`socketpair` initially. Explicitly address `io_uring`, `socketcall`, `ptrace`, process-memory/pidfd access, keyrings, BPF/perf, mount/new-mount APIs, `setns`, `unshare` and clone namespace flags. `clone3` pointer arguments are not inspectable by classic seccomp: reject or use a proved fallback, never inspect pointed-to flags in BPF. Use tested libc/runtime fallback behavior, not hope. Seccomp cannot implement path rules. [Kernel seccomp documentation](https://docs.kernel.org/userspace-api/seccomp_filter.html).
- A network namespace separates network stacks and abstract Unix sockets, but its private loopback may still be usable. Socket denial is necessary for the stronger no-IP profile. IPC namespaces cover SysV IPC and POSIX message queues, not all pathname Unix sockets; filesystem and descriptor controls remain necessary. [Network namespaces](https://man7.org/linux/man-pages/man7/network_namespaces.7.html), [IPC namespaces](https://man7.org/linux/man-pages/man7/ipc_namespaces.7.html).
- Existing atomic cgroup entry owns the trusted helper/setup tree before it can release target work. Require delegated memory/pids controllers, not merely `cgroup.kill`. Manager holds controls outside target namespaces. Read back configured limits. Protect manager memory and control FDs, deny target `ptrace`/`process_vm_*`/`pidfd_getfd`, and prohibit remount/re-entry. Kernel cgroup migration checks depend on write authority at the destination and common ancestor; delegation to the manager's UID alone does not deny that same authority to an unconfined target. [Cgroup v2 delegation and controllers](https://docs.kernel.org/admin-guide/cgroup-v2.html).
- Arm a protected parent-death/PID-namespace chain and bounded deadline independently of the caller. PID-namespace init death provides a useful kernel teardown primitive; a plain `PDEATHSIG` on the root is insufficient for descendants. Test exact helper process topology, death between fork/arming/exec, target attempts to kill or seize the guardian, and descriptor leaks. Empty cgroup plus independent lifetime oracle remains the cleanup criterion. [PID namespaces](https://man7.org/linux/man-pages/man7/pid_namespaces.7.html).

**E10 disposition:** current root WSL migration survived original-domain kill; current delegated qualification does not close hostile control-plane access. P02 must show the target cannot open any parent/sibling `cgroup.procs`, create child/sibling cgroups, change controllers/limits, recover an inherited cgroup FD, mount cgroupfs, reach host `/proc/.../root`, or ask a host daemon to migrate/spawn it. A hidden path alone is insufficient if an alternate namespace, handle or broker makes it reachable. Target denial and manager success must be measured together.

Compare filesystem/identity candidates before selecting additions:

| Candidate | Strength | Missing boundary / requirements | Direction |
| --- | --- | --- | --- |
| Separate host UID + DAC | Protects caller-private files and manager process controls when permissions are correct | Still reads world-readable data, uses networking/IPC, needs identity provisioning, no immutable closure view | Deployment alternative; not sufficient alone |
| `chroot` alone | Changes pathname root | Does not supply full isolation or lifecycle; privileged escape/open-FD concerns | Reject as standalone ([manual](https://man7.org/linux/man-pages/man2/chroot.2.html)) |
| User/mount namespaces + protected copied closure | Explicit visible filesystem, reduced host privilege, old-root removal | Needs path/FD/capability/mount proof; unprivileged userns may be blocked by distro/LSM policy | Leading filesystem candidate |
| Landlock alone | Unprivileged, inherited access restriction; object-based FS mediation | ABI-dependent coverage, ambient descriptors, network/IPC/resource/lifecycle gaps | Do not select alone |
| Landlock layered on candidate | Additional FS and process/IPC restrictions even if a path becomes visible | Extra ABI prerequisite and rules to maintain; metadata/IOCTL coverage must be enumerated | Add only if P02/P06 demonstrates a material gap or simpler proof |
| Read-only host `/usr` + broad home denies | Easy compatibility | Unreviewed installed tools/config/data and mutable external source | Reject for v1 closure |

Landlock's supported rights have evolved: the checked documentation distinguishes FS truncate/refer/device controls, TCP, scoped signals/abstract sockets and newer pathname-socket/UDP rights. Do not infer installed capabilities from a kernel version string, or silently drop unknown required rights. Query the ABI and feature-test the precise ruleset if selected. This review does **not** require the newest documented ABI just because it exists. [Landlock userspace reference](https://docs.kernel.org/userspace-api/landlock.html), [Landlock manual](https://man7.org/linux/man-pages/man7/landlock.7.html).

Privileges/setup: host must permit unprivileged user namespaces and the exact helper, provide a delegated cgroup subtree with mandatory controllers, and meet the existing qualified glibc/atomic-placement prerequisites (currently the glibc 2.43 path). A root test or outer Docker sandbox does not qualify this deployment. A distro that disables these requirements gets pre-release refusal. Do not change sysctls or install a setuid helper as a convenience fallback. A separate host UID/root broker becomes a new architecture decision if these conditions exclude the desired users.

### macOS: explicit first-profile refusal

No safe complete native implementation of this profile has been identified by this review. Existing launchd/PG lifecycle allows setsid escape and lacks AR-5 capture. Same-user private plist permissions protect against other UIDs, not a hostile process sharing that UID.

Public App Sandbox is a real candidate for a **signed/packaged helper product**. Apple documents inherited restrictions for child processes and entitlements for embedded tools. It is incorrect to claim every arbitrary child must be individually re-signed to inherit a parent's sandbox: Apple DTS explains inheritance separately from App Store packaging markers. The unresolved questions are per-execution authority binding, runtime/Mach service baseline, complete descendant ownership, resource budgets and deployable lifecycle. [Apple App Sandbox](https://developer.apple.com/documentation/security/protecting-user-data-with-app-sandbox), [embedded tools](https://developer.apple.com/documentation/xcode/embedding-a-helper-tool-in-a-sandboxed-app), [Apple DTS on inheritance](https://developer.apple.com/forums/thread/679940).

Custom Seatbelt/SBPL through legacy `sandbox-exec`/`sandbox_init` must be treated as a legacy/deprecation-sensitive candidate, not a documented stable per-execution policy contract assumed from third-party use. This review did not establish a current supported custom-profile SDK contract on a native macOS host. P10 must record the native SDK declarations and support status before reopening that route. No private entitlement or undocumented allowance is a production prerequisite without an explicit product decision.

Separate UID/ACLs and launchd do not jointly establish no-network/no-Mach-broker authority or owned escaped descendants. Global firewall changes are not a library-sized per-execution solution. A signed App Sandbox helper plus a separately proven ownership mechanism, or a VM worker with a different deployment/guest-OS contract, could reopen the decision. App Sandbox entitlement restrictions may reduce authority even while launchd cannot prove tree cleanup; neither substitutes for the other. On both Arm64 and x64, require the complete profile matrix before advertising support. For now, preflight/prepare must refuse and prove no target effect; preserve trusted lifecycle support independently.

### Privileged broker contingency: requirements before considering one

A broker would help allocate separate identities, mount bounded storage or control protected cgroups. It also creates a new highly privileged attack surface and does not itself filter target syscalls. It is not recommended until P01–P04 demonstrate that a necessary guarantee cannot be met by operator provisioning/unprivileged helpers.

If reconsidered, require all of the following in a separate reviewed design: OS-authenticated client identity and a narrow local authorization policy (which users may allocate which bounded resources); no trust in caller-supplied SID/UID/profile names; schema-bounded requests; broker-owned staging only; object-bound path resolution; reject arbitrary mount/ACL/service commands; protected binaries/config/journal; bounded message size/concurrency; per-client quota; nonce/session binding and single-use replay protection; private control channel never exposed to target; broker-side final authorization before release; abort-on-disconnect semantics; crash reconciliation and upgrade version negotiation. Gagamba's operational broker access control is distinct from Hufu's decision that a user may execute a tool with business authority. Broker compromise may confer host privilege and belongs explicitly in the trust model. A general privileged “run this command” service is rejected.

## E. Architecture decision matrix

Scores are qualitative estimates, not measurements. **Critical failures veto a candidate; scores are not summed to outvote security.** H = comparatively favorable, M = mixed, L = poor, ? = blocking evidence missing. “Native” below means bespoke setup around OS primitives, not a claim that all composed guarantees are native.

| Criterion / weight | Bespoke direct native | Trusted privileged broker + OS isolation | Established helper/container/VM adapter |
| --- | --- | --- | --- |
| Security correctness / Critical | ? New launcher/token/filter code | ? Adds privileged parser/IPC trust | ? Smaller setup code with helper; VM stronger separation but integration still unproved |
| Fail-closed release / Critical | H if all setup is gated | H if broker gates immutable requests | ? Must prove actual target launch, not helper success |
| Control-plane protection / Critical | ? Same-user boundary needs proof | H potential separate identity; broker IPC risk | H potential private namespaces/guest; host mounts/sockets can defeat it |
| Native evidence / Critical | L No AR-7 native evidence | L None | L None for Gagamba composition; upstream success insufficient |
| Windows practicality / High | M LPAC promising, IPC/storage unresolved | M Provisioning/storage easier; service deployment | M VM OS-edition/feature and automation costs; historical experimental APIs unqualified |
| Linux practicality / High | M Many setup details to own | M Covers locked-down hosts at high trust cost | H Bubblewrap candidate; rootless OCI also possible but broader lifecycle/image surface |
| macOS practicality / High | L Current native profile unresolved | L Broker does not create absent kernel ownership | M VM alternative; native signed helper still needs probes |
| Non-admin use / High | M Host prerequisites required | L Admin install, user execution afterward | M Helper with delegation; VM feature/provisioning varies |
| Operational cost / High | M Small install, high security maintenance | L Service hardening/upgrades/recovery | H helper; L VM image/storage/startup burden |
| Maintainability / High | L–M Bespoke OS ABI/filter surface | L Extra protocol/security lifecycle | H pinned helper; M container/VM stack |
| .NET usability / Medium | ? Closure/JIT/threads/diagnostics | ? Same runtime limits still apply | ? Test actual tool/runtime, not container boot |
| Performance/startup / Medium | Expected favorable; unmeasured | Expected moderate; unmeasured | Helper expected favorable; VM higher; unmeasured |
| Future remote worker / Low | Neutral immutable requests suffice | Neutral | Natural fit for VM/container but not a reason alone |

**Provisional preference:** Linux established unprivileged helper plus native cgroups; Windows direct LPAC plus Jobs only if IPC and all-storage gates pass; macOS refusal. Preserve lifecycle implementations below an explicit restricted path. Do not auto-select another family on failure.

Serious alternatives and reversal evidence:

- **Bespoke Linux launcher:** reverse the helper preference if pinned helper cannot expose trustworthy readiness/root-exit status, close authority FDs, support bounded scratch/export or arm crash cleanup without extensive patching. A smaller auditable helper may then be justified, with its own ABI/packaging evidence.
- **Rootless OCI runtime:** comparable kernel building blocks with a standardized bundle; useful if deployment already provides a maintained runtime and lifecycle/limits integration becomes smaller. Requires no host daemon socket in target, no privileged container, pinned images and same matrix. Container branding proves nothing.
- **VM worker:** favored if Windows accessible brokers, profile writes or kernel-resource limits defeat the ceiling, or if the user prioritizes stronger tenant separation over footprint. A guest can still leak through host shares/clipboard/agents. Windows Sandbox documents networking and host mapping controls, but a `.wsb` launch is not a complete headless execution/receipt API. Disable unnecessary integration and prove guest ownership/export/crash behavior. [Windows Sandbox configuration](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file).
- **Privileged broker:** reconsider only with measured need and accepted deployment cost; no broker-first assumption.
- **Landlock-only or token-only:** reconsider only if the full authority/lifecycle/resource contract can be demonstrated without missing channels. Availability alone is insufficient.

## Material findings

Low-cost improvements worth carrying into the approved design are listed below. They do not require a VFS, scheduler, package manager or universal quota abstraction.

| Improvement | Concrete benefit / bounded cost |
| --- | --- |
| One named required profile | Makes unsafe combinations and silent fallback harder; one versioned contract rather than many independently optional booleans |
| Immutable normalized authority manifest | Gives both caller authorization and provider establishment the same resource ceiling; retain AR-4 copying discipline |
| Prepared runtime/input closure | Reduces dependency ambiguity and TOCTOU surface; copy/hash/validate supplied files rather than discover/install packages |
| Disposable scratch and synthetic HOME/TEMP | Keeps ordinary tool state away from host credentials; useful only with real access and storage enforcement |
| Read-only preflight report | Explains missing helper/controller/token/runtime prerequisites before allocation; actual preparation must still recheck |
| Immutable effective restriction report | Lets consumers distinguish a supported feature from an established guarantee; extend receipts narrowly |
| Separate security qualification manifest | Reuses exact-source evidence discipline while recording helper/filter/baseline dependencies absent from lifecycle-only claims |

These additions address concrete review gaps. Cache reuse, new native abstractions, per-effect mediation and remote transport stay deferred unless evidence demonstrates a need.

Severity is architectural importance, not an assertion of a newly exploited AR-6 defect. Confidence separates source facts from candidate feasibility. All remain open pending their referenced experiments.

### AR7-F01 — Lifecycle acceptance does not establish restricted authority

- Area/severity/confidence: contract; Critical readiness gate; High.
- Existing evidence: `ProcessStartSpec`, `Platforms`, native launch paths and original F01; no authority request fields in production.
- Problem/consequence: a caller could treat six lifecycle guarantees as permission confinement while a native tool retains host access.
- Candidates: new required bundled profile; loose optional flags; replace lifecycle API.
- Direction: bundled profile with immutable effective manifest beside the existing lifecycle path; reject optional-security composition. No lifecycle rewrite.
- Required experiment: P05, Q24; ensure unsupported profile cannot route to ordinary launch.

### AR7-F02 — Windows ambient and broker authority is unbounded by Jobs

- Area/severity/confidence: Windows identity/IPC; Critical; High for scope gap, Medium for proposed LPAC sufficiency.
- Evidence: ordinary `CreateProcessW`, documented WMI exception; Microsoft AAP/LPAC distinction; no production token restriction.
- Problem/consequence: file reads or broker-created work can occur outside the intended ceiling and outlive Job stop.
- Candidates: LPAC with narrow closure; extra restricted SID; broker-isolated identity; VM.
- Direction: P01 must prove actual token/object and broker semantics, including permissive synthetic endpoints; no broad compatibility grants.
- Required experiment: P01/P03; Q01, Q10–Q12, Q14.

### AR7-F03 — Linux controller access can defeat containment

- Area/severity/confidence: Linux control plane; Critical; High source/history, Medium candidate feasibility.
- Evidence: E10 root migration; `OwnedCgroup.Path` host controls; delegated AR-6 tests do not attempt hostile same-UID authority.
- Problem/consequence: reaching writable ancestor/sibling controls permits migration, altered limits, sibling interference or suppression of cleanup.
- Candidates: private mount/PID/user boundary plus closed FDs; Landlock layering; separate UID/broker.
- Direction: establish inaccessible controls through every namespace/FD/process/broker route; do not promote Partial lifecycle escape cell globally.
- Required experiment: P02; Q13–Q15.

### AR7-F04 — Invocation copying is not object/authority binding

- Area/severity/confidence: filesystem/TOCTOU; High; High.
- Evidence: `ProcessStartSpec.TrySnapshot` validates strings and cwd existence; preparation takes lifecycle requirements; AR-4 explicitly excludes replacement resistance.
- Problem/consequence: code/runtime/path identity can change after approval; a live RW host tree can contain aliases to ungranted objects.
- Candidates: copied protected closure; live handle-based grants; hash-only binding.
- Direction: copied closure with stable object binding and no initial special entries; hash-only and path-prefix enforcement rejected. Export must also be object safe.
- Required experiment: P05/P06; Q02–Q06.

### AR7-F05 — No-network and no-host-IPC are different requirements

- Area/severity/confidence: network/local authority; Critical; High.
- Evidence: current providers apply no such controls; Linux capture inherits stdin while closing descriptors >=3; Linux net namespace alone permits a private stack; Apple/Windows runtime brokers need separate evaluation.
- Problem/consequence: DNS/proxy or Unix/named/Mach service paths can disclose data or delegate execution despite an apparently offline process.
- Candidates: address-family denial + private namespaces; token/WFP + object ACLs; VM boundary.
- Direction: test each transport and reachable operation, including inherited sockets; do not use empty env as an IPC control.
- Required experiment: P03; Q07–Q11.

### AR7-F06 — A bounded workspace does not bound all writable state

- Area/severity/confidence: resource availability; High; High gap, Medium platform remedy.
- Evidence: AR-5 limits captured bytes only; Windows `OwnedJob` has no process/memory/storage limits; cgroup setup has no mandatory controllers/limits.
- Problem/consequence: a short-lived target can fill disk, consume memory or spawn tasks before deadline; profile/registry/temp writes can escape scratch budgets.
- Candidates: native aggregate controls plus finite store; separate provisioned volume/identity; VM.
- Direction: storage/process/memory required; distinguish task count, commit/RSS and helper overhead. Polling is telemetry, not a hard quota.
- Required experiment: P04; Q16–Q18.

### AR7-F07 — Crash ownership is not established for the new setup chain

- Area/severity/confidence: lifecycle/recovery; Critical; High existing limits, Medium candidate composition.
- Evidence: Linux owner-death Absent, macOS escape observed; Windows create precedes Job assignment; current handles are in-memory.
- Problem/consequence: a setup/manager crash can orphan work or leave untrusted persistent state even when ordinary termination passes.
- Candidates: atomic Job-at-create; protected helper/namespace guardian; OS service manager; VM owner.
- Direction: prove every launch/death transition; classify constructed behavior; retain cleanup debt and forbid automatic resume/retry.
- Required experiment: P07; Q19–Q23.

### AR7-F08 — macOS symmetry would hide unsupported guarantees

- Area/severity/confidence: platform support; High; High for current refusal, Medium for future alternatives.
- Evidence: AR-6 observed setsid escape; `MacOsExecutionProvider.LaunchCaptured` refuses; no restricted profile.
- Problem/consequence: substituting launchd or a legacy Seatbelt script cannot justify the common profile's ownership, resource and authority claims.
- Candidates: explicit refusal; signed App Sandbox product; VM worker.
- Direction: refusal now; reconsider only against full native matrix and supported deployment contract.
- Required experiment: P10 if user chooses to reopen macOS; Q25 now.

### AR7-F09 — Historical design rules cannot silently define the new profile

- Area/severity/confidence: specification/usability; High; High.
- Evidence: security-model says both reject conflicting nested grants and inner-wins; historical Windows compatibility used RO drive-root; root-exit rule differs from current lifecycle semantics.
- Problem/consequence: an implementer may widen authority to revive historical workloads or change lifecycle behavior unintentionally.
- Candidates: adopt historical model wholesale; versioned narrow closure profile; arbitrary grants.
- Direction: disjoint copied roots, explicit baseline, preserved lifecycle semantics and versioned profile. Record decisions before API design.
- Required experiment: P05/P08; include forbidden broadening cases and complete consumer intent.

### AR7-F10 — Establishment and receipt need stronger semantics than current orchestration

- Area/severity/confidence: pre-release gate/evidence; High; High static observation.
- Evidence: `ExecutionOrchestrator` creates deadline task after synchronous launch; `RunReceipt.EffectiveCapabilities` contains lifecycle negotiation strings; fingerprint key is process-local. Linux close-from currently also closes any proposed helper setup FD >=3, and provider completion observes the directly launched process, not an arbitrary wrapped tool.
- Problem/consequence: expired setup budget can still be followed by target release; a receipt cannot prove bound authority or serve as a cross-restart security seal.
- Candidates: restricted preparation/release transaction with stable manifest digest; retrofit optional booleans; trust launcher exit.
- Direction: narrow additive contract pressure only after probes; trusted establishment evidence and release deadline check, stable non-secret manifest digest distinct from redacted invocation fingerprint. P05/P07 must prove how setup-only channels survive until the trusted helper and close before target exec, and how wrapped root status stays truthful. Do not assume a helper can be substituted with zero source integration. Preserve legacy semantics.
- Required experiment: P05/P07/P09; Q19, Q24. This is a restricted-profile requirement, not a claim of a newly reproduced AR-5 regression.

## F. Gagamba / Hufu boundary contract

```text
caller intent + requested authority
    -> Hufu (or another caller) authorizes immutable normalized intent
    -> Gagamba resolves/binds closure and mandatory profile, without widening
    -> caller checks final authority manifest against authorization
    -> single-use, expiring preparation; final authorize/revoke check
    -> Gagamba establishes all restrictions and releases exact bound target
    -> immutable effective restrictions + execution/cleanup receipt
```

Preparation may stage data under an already-authorized allocation/read constraint. It may not execute target code. If resolving runtime dependencies or concrete object bindings changes authority, return the final manifest for caller authorization; do not call it already authorized. Gagamba can offer deterministic normalization as a utility without deciding who is entitled to a grant. No Hufu/Cedar/Biscuit dependency, token parser or business policy engine enters Gagamba.

Proposed manifest fields: schema/profile revision; opaque correlation; tool/runtime/input digests and binding identities; logical-to-platform resource map; RO/RW operations; baseline resource revision; network/IPC modes; execution identity class; constructed environment descriptors (values redacted in ordinary receipts); allowed inherited-channel classes; limits with accounting units; lifetime/death/cleanup semantics; helper/provider versions and required host features. Live handles and privileged native IDs remain private. A digest identifies content, not authorization or proof of enforcement.

Results distinguish **Requested**, **Established**, **Unsupported**, and **FailedToEstablish** per required restriction, plus an overall refusal/acceptance. No executable is released with an unknown or missing required item. “Supported by discovery” is advisory; “Established” needs per-launch checks before release. Static assurance references identify the qualified mechanism/version; per-run evidence records actual setup. A successful tool exit does not attest its own confinement.

Receipts attach the immutable effective manifest (or immutable redacted view plus digest), source/package/helper identities, execution result, native limit-hit observations where known, output status, cause and separate cleanup components. Protect persisted evidence from the target. Unknown denials must remain generic execution failure, not fabricated `PolicyViolation`. Existing AR-5 process-local HMAC remains useful for redacted comparison but is not the authority manifest digest or a portable attestation.

Hufu revocation requests stop of the correct owned execution and awaits bounded evidence. It does not cancel only an observer. Native file grants/handles already obtained cannot generally be withdrawn by editing a policy record. First profile revocation is terminate, confirm, then optionally authorize a new preparation; no live grant widening/narrowing API. Returned bytes and previously exported effects remain. An Hufu decision cannot compensate for Gagamba returning `Unsupported`.

## J. Claims after successful future qualification

Allowed wording, with placeholders filled from the exact security manifest:

> Gagamba [version/source/package hashes], using [provider/helper/OS configuration], is qualified for restricted-offline-closure-v1 revision [R]. On the qualified hosts it executes potentially hostile native code with access limited to the prepared runtime/input closure, disclosed OS baseline, bounded disposable scratch and prepared data channels. It denies the tested ungranted filesystem, IP network and host-service IPC authority, protects its execution controls, enforces the reported native resource bounds, and reports domain-stop and resource-cleanup evidence. Unsupported requirements fail before target release. These claims assume the trusted caller, preparation resources and OS described in the profile.

Also state platform refusals, exact runtime closures tested, constructed death-cleanup bounds, memory/task accounting and excluded resource/side-channel risks beside the claim. No claim is allowed today beyond AR-6's lifecycle/package scope.

Forbidden wording: “secure against arbitrary local attackers”; “all Windows/Linux/macOS configurations”; “no possible escape”; “same-user isolation because files are 0700”; “Jobs/cgroups provide filesystem/network isolation”; “all descendants stopped” based only on root exit; “all disk/memory/CPU use bounded” without matching accounting; “revocation reverses effects”; “reboot proves cleanup”; “a previous source seal qualifies this commit”; “all .NET/SDK/Git workloads supported”; “Hufu authorization establishes OS enforcement.”

## K. Decisions requiring user approval before production implementation

These are review recommendations, **not decisions silently made by writing the plan**. Probe-only work can gather evidence before presenting final architecture approval. Existing permission to implement AR-5 Windows/Linux with macOS refusal does not authorize a new privileged AR-7 service or weaker profile.

| Decision | Why / options | Security consequence | Complexity consequence | Recommended choice / evidence |
| --- | --- | --- | --- | --- |
| D01 First profile scope | Copied closure + scratch vs live host edits | Copies reduce alias/TOCTOU and persistence authority | Staging/export cost; fewer compatibility promises | Copied closure; AR7-F04/F09 and P05/P06 |
| D02 Platform order | Simultaneous three-platform vs independently gated subset | Avoids weaker semantics disguised as portability | Allows Linux first; Windows may remain held; macOS refusal | Independent gates; AR-6 platform differences, P01–P04/P10 |
| D03 Linux deployment | Unprivileged helper + delegation vs new root broker vs VM | No new privileged service on preferred path; distro support narrower | Pin/package helper; operator controller provisioning | Helper candidate if P02/P07 pass; no automatic sysctl/setuid fallback |
| D04 Windows deployment | LPAC + provisioned storage vs service/VM | IPC and writable profile limits determine viability | Provisioning or VM footprint may be material | Decide after P01/P04; no unbounded-disk compromise |
| D05 Supported API policy | Supported documented contract vs private/experimental platform APIs | Unsupported ABI could silently weaken setup | Supported subset may be smaller | Documented contracts; experimental route needs separate explicit acceptance and evidence |
| D06 Resource semantics | Native accounting with explicit limits vs identical portable accounting | Avoids false memory/process equivalence | Conservative Linux task cap may limit .NET | Native-accounting disclosure; P04/P08 must show useful workload; revise contract openly if necessary |
| D07 Runtime compatibility | Curated closure vs arbitrary host-installed tools | Broad host grants expose authority | Curated manifests need maintenance | Native fixture then one explicit .NET closure; no implicit SDK/cache/home grants |
| D08 Crash/recovery | Terminate/reconcile vs resumable executions | No replay or stale-handle authority | Small protected journal, not distributed scheduler | Terminate/reconcile; P07 and Q22/Q23 |
| D09 Product availability promise | Single trusted caller workstation vs hostile multi-tenant service | First-order quotas do not cover all kernel-resource DoS | Stronger service promise may force VM isolation | Workstation/tool execution first; P04 finite stress decides adequacy |
| D10 Broker contingency | Approve service only if needed vs refuse unsupported hosts | Root broker compromise materially expands trust | Authentication, install, upgrade, recovery and auditing | No broker now; revisit only measured unmet guarantee |

## Final review summary

Recommended profile: `restricted-offline-closure-v1`, with copied runtime/inputs, bounded disposable scratch, no network or host IPC and mandatory pre-release establishment. Windows candidate: LPAC/AppContainer + protected ACL closure + Job + bounded storage, conditional on P01/P04. Linux candidate: pinned unprivileged namespace helper + explicit seccomp + protected cgroup controllers, conditional on P02/P04/P07. macOS: explicit refusal. A privileged broker is not justified yet. Hufu authorizes; Gagamba establishes and reports enforceable authority.

Top five assumptions requiring proof: (1) Windows runtime/IPC baseline has no ambient authority escape; (2) Linux target cannot regain manager/cgroup/namespace authority; (3) closure/export bindings survive link and replacement attacks; (4) native resource accounting and all writable storage are actually bounded; (5) the setup/death chain cannot release or leave orphaned work across crash windows.

Top five feasibility experiments: P01 Windows LPAC and broker boundary; P02 Linux controller/namespace separation; P03 no-network and local IPC transports; P04 resource/storage/accounting viability; P07 crash and release transitions. P05/P06 then bind preparation and hostile filesystem objects before implementation choices are approved.

Top five implementation risks: (1) compatibility fixes widening baseline grants; (2) helper/process status being mistaken for target readiness/completion; (3) path/handle and ACL ownership errors during staging/export/cleanup; (4) seccomp/ABI and runtime-thread compatibility gaps; (5) stale or partial evidence being promoted into a portable security claim.

Proposed sequence: AR-7A evidence-only feasibility → AR-7B approved profile/contract → AR-7C protected preparation → AR-7D one complete platform enforcement path → AR-7E crash/recovery → AR-7F adversarial native/package qualification → AR-7G independent security review. Platform paths qualify separately; no partial path is publicly advertised. See the companion handoff for scope, tests, artifacts and stop conditions for every slice.

After qualification, claim only the exact profile on the measured source/packages/hosts, with disclosed native accounting and exclusions. Do not claim arbitrary local-attacker safety, universal platform support, complete DoS prevention or reversible effects. The review and AR-6 freeze do not close original F01.

AR-7 REQUIRES FEASIBILITY PROBES BEFORE ARCHITECTURE DECISION
