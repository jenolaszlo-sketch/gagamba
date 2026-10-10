# Gagamba Linux M1 implementation plan

Date: 2026-10-10 (Asia/Manila).

Status: planning artifact; implementation and native qualification have not begun under this plan.

Authoritative architecture: [ADR 0003: Linux-first restricted execution](../adr/0003-ar7-linux-first-restricted-execution.md), committed at `4d09452172caf232c209ae5c6d1031ef3f6feaa8`.

Sealed feasibility evidence: `cb511cd18ba25daeda76cf41a400d1136c6ba0f0`. The approved ADR was saved but uncommitted when this planning task began; its existing documentation changes were committed separately before this plan was written. This plan neither changes that architecture nor qualifies its implementation.

Read with [contributor guidance](../../AGENTS.md), [handoff](../handoff.md), [work queue](../work-queue.md), and [remediation evidence standard](../remediation-evidence-standard.md). Where historical queue entries conflict with the ADR, the ADR's bounded implementation authorization governs. Preserve the AR-6 baseline `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, its tag and evidence.

## 1. Purpose and milestone outcome

Build one internal Linux prepared-execution transaction: a pinned Bubblewrap composition executes one copied static native fixture over one protected copied input, with a fixed resource manifest, bounded stdout/stderr, externally owned cgroup lifecycle, and a production-designed pre-initialization release boundary.

M1 ends with a small, independently reviewable implementation and source-bound evidence for its stated internal transaction. It does not end with an advertised production `restricted-offline-closure-v1` provider. A complete transaction must perform useful work and must refuse before target initialization when any M1-mandatory establishment item is missing. It must also stop and reconcile owned resources after injected failures without replaying uncertain execution.

Use an internal `linux-m1-native-transaction` implementation identifier, separate from the production profile claim. The harness may run only the source-bound finite fixture under the declared test envelope. It is not an arbitrary-executable bypass around profile refusal. No public runtime selection path enables this identifier. The production profile continues refusing until all mandatory production gates pass.

Only planning/documentation is performed by the task creating this file. All code, builds, native tests and fault injections described below are future implementation work. Accepted P02/P03/P04/P07 feasibility is not reopened.

## 2. Architectural constraints inherited from the AR-7 ADR

The following are load-bearing, with no opt-out flag in the internal transaction:

| ID | Constraint | M1 consequence |
|---|---|---|
| I01 | Target initialization follows release | The loader, constructors, input preamble and target hooks cannot run in the held phase. |
| I02 | One release/stop linearization point | One external arbiter consumes the preparation and serializes release with accepted stop requests. |
| I03 | No released-but-unowned execution | Register resources and establish domain ownership before setup can authorize target execution. |
| I04 | Domain authority, not numeric PID authority | Terminate through stable cgroup control handles; PIDs are observations or direct-child reaping identifiers only. |
| I05 | Immutable, single-use preparation | Bind copied bytes, invocation, environment, channels, limits and issuer identity; reject mutation, replay and foreign preparations. |
| I06 | Ambiguous release means may-have-executed | Stop/reconcile without retrying authorization or executing again to discover a result. |
| I07 | Output enforcement outside target | Two independent drains, finite storage, immediate stop signaling on overflow/read failure, finite final drain. |
| I08 | Missing restrictions refuse | Required readback/establishment failure never becomes a warning followed by release. |
| I09 | Result and cleanup are independent | Unknown root/exec results remain Unknown even after cleanup is Confirmed. |
| I10 | Narrow Bubblewrap abstraction | Preserve helper independence without a general plugin or OS-policy framework. |
| I11 | No new privilege | Use existing delegation and an unprivileged owner/coordinator; no daemon installation, setuid, file capabilities, sysctl or host ACL repair. |
| I12 | Standalone Gagamba | No Hufu, Penghou or workflow dependency and no policy evaluation inside the mechanism. |
| I13 | All exposed writes accounted | Explicit storage inventory; unknown writable stores block integrated release. Unqualified inode enforcement stays an explicit production gap. |
| I14 | Evidence-bound mechanism | Pin helper/filter/coordinator/fixture bytes and record actual establishment, not just configured intent. |

Retain user/mount/PID/network/IPC namespaces, capability drop, no_new_privs, installed seccomp, protected cgroup controls and narrow read-only copies. Retain UTS isolation initially as configuration. Do not promote UID 65534, a root observer, probe leaf names, exact helper process counts or the R2 `/gate` protocol into production requirements.

The full profile's no-IP requirement includes private loopback. M1 must neither weaken that requirement nor claim the frozen filter already proves it. The frozen filter and helper are the initial baseline; any strengthening is a new reviewed configuration with new evidence, not a rewrite of sealed AR-7 results.

## 3. Non-goals

- Hufu authority/grant/revocation integration or workflow integration.
- Arbitrary runtime discovery, interpreters, installed SDKs, .NET target runtime qualification or online restore.
- Shared writable caches, reusable closure caches, host-tree write-through or an export API.
- Public API/package stabilization, production capability advertisement, publication or release qualification.
- Windows, macOS or VM implementation, additional native Windows candidates or a privileged service.
- Replacing Bubblewrap's namespace/mount engine with a bespoke launcher.
- Exact portable RSS, process-only/thread-independent quotas, CPU-rate guarantees or hostile multi-tenant availability.
- Full P05/P06/P08/P09 qualification and exhaustive owner-death/network/IPC coverage.

The trusted owner may use the repository's .NET 10 host tooling. That does not qualify a .NET workload inside the sandbox. The target remains one static native fixture, with bounded modes compiled into the same source-bound binary.

## 4. Proposed code/project structure

The existing code at the architecture commit is a lifecycle substrate, not a restricted provider:

| Existing component | Reuse pressure and constraint |
|---|---|
| `src/Gagamba.Execution/Provider.cs` | Preserve the existing SPI, issuer checks and observation-only cancellation semantics. Value snapshots do not bind executable/input objects. |
| `src/Gagamba.Execution/OutputCapture.cs` | Reuse the bounded two-stream design where suitable. Overflow currently signals a task; M1 must connect it to its owner. Reader failure must be exposed promptly, not discovered only after provider completion. |
| `src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs` | Its atomic cgroup spawn path is relevant. Its private launch path closes FDs >=3, so it cannot simply transport release/status/filter FDs. It currently observes the direct launched child, which would be Bubblewrap. |
| `src/Gagamba.Execution.Linux/OwnedCgroup.cs` | Reference existing kill/population/retry behavior, but M1 needs held native identities, resource readback and guarded recovery. Path-based adoption/removal is not the new security contract. |
| `src/Gagamba.Execution.Linux/LinuxExecutionState.cs` | Preserve the sole reaper and execution-owned completion principles. Add separate target/helper observations; do not relabel its direct-child status as the wrapped target's status. |
| `src/Gagamba.Execution.Linux/LinuxCapturePipes.cs` | Useful pipe ownership patterns; M1 explicitly fixes stdin to EOF and audits every setup descriptor. |
| `src/Gagamba.Runtime/ExecutionOrchestrator.cs` | Reference result separation, but its timer is installed after synchronous launch. M1 needs a deadline active through preparation/held setup; do not route the transaction through this public orchestrator. |
| Existing Linux/core/runtime test projects | Add focused regressions and retain legacy behavior. Current Linux tests use .NET 10 and xUnit v3. |

Proposed files are names for coherent implementation components, not instructions to create empty scaffolding:

| Proposed location | Responsibility |
|---|---|
| `src/Gagamba.Execution/Restricted/RestrictedManifest.cs` | Internal OS-neutral immutable authority/resource/closure value model. |
| `src/Gagamba.Execution/Restricted/RestrictedTransaction.cs` | Internal preparation identity, state, outcome and cleanup vocabulary. No native handles in semantic records. |
| `src/Gagamba.Execution.Linux.M1/` | New non-packable internal implementation project targeting the existing .NET baseline; no public provider registration. |
| `.../Preparation/PreparedClosure.cs`, `SafeCopyIn.cs` | Copy-in, inventory, protected object binding and bounded staging. |
| `.../Lifecycle/RestrictedDomain.cs`, `DomainLease.cs` | Stable cgroup controls, placement, limit readback, empty observation and recovery identity. |
| `.../Launch/IHeldLauncher.cs`, `BubblewrapAdapter.cs`, `LaunchDescriptorMap.cs` | Private helper abstraction, deterministic argument vector, setup FD protocol and installation checks. |
| `.../Execution/ReleaseArbiter.cs`, `RestrictedExecutionOwner.cs` | Single-use release/stop ordering, monotonic deadlines, result aggregation. |
| `.../Output/RestrictedOutputCapture.cs` | Two drains and owner stop notification; reuse or adapt existing code only where its lifecycle fits. |
| `.../Recovery/OwnershipJournal.cs`, `Reconciler.cs` | Bounded durable records and identity-safe stop/remove without replay. |
| `.../Evidence/TransactionReceipt.cs` | Bounded semantic receipt and separate protected recovery evidence. |
| `src/Gagamba.Execution.Linux.M1.Owner/` | Non-packable per-execution unprivileged owner executable for process-failure coverage; no listener/service installation. |
| `native/linux-m1/exec-coordinator.c`, `protocol.h` | Small trusted static held-exec/status coordinator; does not construct namespaces or own host cgroup controls. |
| `tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c` | One finite static untrusted fixture, including constructor, output, child and fault modes. |
| `tests/Gagamba.Execution.Linux.M1.Tests/` | Non-packable portable and delegated-native tests; bounded external supervisor/recovery harness. |
| `eng/linux-m1/build-native.sh`, `run-m1.sh` | Future explicit local build/run entry points; no host provisioning or publication. |
| `docs/evidence/gagamba-linux-m1.md`, `docs/evidence/linux-m1/` | Future durable evidence and compact selected records. Large logs/binaries go under ignored `artifacts/linux-m1/`. |

Add narrowly named friend assemblies only where internal contracts cross projects. Keep native handles inside the Linux implementation. Avoid a shared lifecycle base-class refactor. If a private legacy primitive must be extracted, preserve behavior and run the affected existing regressions in that package. Explicitly set new projects non-packable; no version change or published dependency is needed. Do not ship the fixture or unrestricted positive controls in a production launch path.

## 5. Core internal contracts

These are semantic contracts; final signatures may vary without changing them:

| Contract | Required content / operations |
|---|---|
| `M1RequestSnapshot` | Fixed fixture identity, ordered argv, one input descriptor, explicit environment, logical cwd, complete fixed resource values, monotonic setup/overall deadlines. Reject arbitrary target identities. |
| `AuthorityManifest` | Schema and baseline revisions, closure/input digests, logical read/write map, channel classes, no-host-IPC/no-IP intent and establishment/qualification states. No policy grants or tokens. |
| `ResourceManifest` | Units, scope and finite values for memory/swap/tasks/FDs/storage/output/time/staging; explicit unsupported or pending dimensions. |
| `PreparedTransaction` | Opaque random ID, issuer instance/generation, immutable manifest digest, expiry, private bound-object owner and atomic state. It cannot be reconstructed from serialized caller fields. |
| `DomainLease` | Private stable handles, owned leaf identity, delegation/boot context; `KillDomain`, `ReadPopulation`, `ReadLimits`, guarded reclaim. Never accepts a caller PID to stop. |
| `IHeldLauncher` | `StartHeld(prepared, domain, descriptorMap)` yields either a held trusted setup instance or a classified establishment failure; no target release in this call. |
| `HeldExecution` | Single owner, installation report, private release/status channels, helper observation, abort operation; invalid after release or abort. |
| `ReleaseArbiter` | `TryRelease(exactPreparation)`, `RequestStop(cause)`, deadline event, owner-loss event; one ordering authority. No mutable request values passed at release. |
| `ExecutionObservation` | Exec disposition (`NotReleased`, `MayHaveExecuted`, `ExecObserved`), target root result (`Exit`, `Signal`, `Unknown`), wrapper result, domain state and output state. |
| `CleanupReport` | DomainStopped, ProcessesReaped where observable, StorageReclaimed, ControlsReclaimed; each Confirmed/Failed/Unknown and finite reasons. Export is explicitly not applicable in M1. |
| `OwnershipRecord` | Version, execution/issuer/boot IDs, owned resource identities, phase/sequence, release intent, digests and outstanding cleanup components. No reusable release secret. |

Store copied immutable collections rather than exposing mutable arrays through records. A digest identifies content and is not authorization or proof of installation. An establishment result distinguishes Requested, Established, Unsupported and FailedToEstablish. Production qualification remains a separate dimension; an internal receipt cannot mark the full profile qualified.

Allocate bounded operation budgets for preparation and evidence parsing. Malformed or oversized input, environment, command data, protocol frames and recovery records are classified failures before release. Defaults are frozen into the M1 manifest; no automatic budget increase.

## 6. Execution state machine

```text
Preparing → Prepared → Owned → ReleaseCommitted → Running → Completed
     \          \         \             \           \
      └──────────┴─────────┴──────────────┴───────────→ Stopping
                                                       ↓
                                             Failed / Cancelled

Observation loss: execution outcome Unknown; reconcile separately.
Cleanup state: Confirmed / Failed / Unknown, independent of the above.
```

| Transition | Preconditions and authoritative action | Crash disposition |
|---|---|---|
| Enter Preparing | External owner registered, setup deadline running, journal intent reserved before allocation | Reconcile partial allocations; no target exists. |
| Preparing → Prepared | Protected copies and manifest complete; all later mutation paths closed | Unpublished/partial preparation cannot release; reclaim by recorded identities. |
| Prepared → Owned | Held helper domain-owned; required namespace/filter/limits/channel checks passed; output readers and stop path armed | Abort held setup; no target initialization. |
| Owned → ReleaseCommitted | Release arbiter checks issuer, unused state, exact digest, establishment, stop latch, owner lease and deadline; consumes once | Durable release-intent record means may-have-executed if precise observation is lost. |
| ReleaseCommitted → Running | Trusted exec observation, distinct from target stdout | If exec cannot be established, remain MayHaveExecuted and stop/reconcile. |
| Running → Completed | Root result known, domain empty and output terminal | Publish stable execution result; cleanup may independently fail. |
| Any nonterminal → Stopping | Accepted stop/expiry/owner-loss/failure; close unreleased channel and use domain kill when allocated | Retain cleanup debt; never repeat release. |

Helper OS processes may exist before the semantic Owned state, but they are already in the lifecycle domain and cannot exec the target. Owned means all release prerequisites are satisfied, not the first moment lifecycle responsibility appears.

A rapidly exiting target can have exec and terminal evidence arrive together without an observable Running interval. Failed/Cancelled expresses the run disposition, not proof of death. Publish a stable root/exec outcome separately from subsequent versioned cleanup reconciliation. Cancelling a waiter detaches that observer only; explicit execution cancellation goes through the arbiter.

## 7. Preparation and immutable binding

M1 accepts one trusted-build static ELF fixture and one ordinary input file, copied into a fresh protected staging root. No directory-tree import or general file grant language is needed. Reject symlinks, special objects and unsupported filesystems; copy regular hardlinked source bytes into a new inode rather than preserving the link. Strip source privilege/security metadata. Set fixed destination modes and logical names.

Use trusted root directory handles and component-relative, no-follow resolution with checked object classes. Add the exact supported native resolution primitives through the Linux adapter and qualify their use; lexical prefix checks are not sufficient. Source handles stabilize names but do not freeze bytes against an existing writer: copy first, hash the resulting protected destination, and make the destination identity the authorized input. A source concurrently modified during copy may yield a rejected copy or the actual copied snapshot; it must never falsely retain an expected digest. The fixture must match its expected build digest or refuse.

Staging is not under target scratch. The target cannot access its host parent, alter its mount sources or recover staging FDs. Protect staging before publication; drop writable preparation descriptors and use read-only object bindings for helper setup. Hashes alone do not protect against later writes. Exclude hostile unrelated host processes, as the ADR threat model does; include every write attempt by the target.

`PreparedTransaction` binds fixture bytes, input bytes, environment, argv, cwd, helper/filter/coordinator identity, mount plan, channel plan and resource manifest. A final caller or harness acceptance references this exact digest. Publication and consumption are atomic under the owner. Mutation after publication requires discard and a new preparation, never updating an existing token.

Proposed fixed M1 admission configuration: one execution, fixture <=4 MiB, input <=256 KiB, staging <=16 MiB and <=128 objects including implementation-owned records. These are implementation test limits, not qualified runtime budgets. Count bytes while copying and abort before exceeding admission limits. The execution deadline begins before preparation. Test under-budget acceptance and deterministic over-budget refusal.

Register allocation intent before creating an object, then persist the confirmed identity. A crash between these operations leaves a narrowly scoped incomplete intent requiring guarded reconciliation. Names are generated by the owner under a retained trusted root; never adopt arbitrary paths from a record or scan unrelated directories by prefix.

## 8. Pre-initialization release mechanism

Select a small, separately built, static trusted exec coordinator launched as Bubblewrap's initial command. Bubblewrap remains the namespace/mount engine. The coordinator is independently pinned, has no target initialization code, performs no input reads, loads no target-controlled configuration and uses a dedicated bounded control protocol. Do not reuse the R2 `/gate` source, nonce-on-stdin protocol or target markers as the implementation.

Proposed production protocol to review before native coding:

1. The external owner creates anonymous release/status channels and the output pipes. Stdin for the eventual target is a prepared EOF source. A descriptor map identifies which trusted process may possess each endpoint and when it must close.
2. Bubblewrap is born inside the target cgroup. It establishes the approved namespaces, capability ceiling, no_new_privs, mounts and explicitly installed seccomp filter, then starts only the trusted coordinator.
3. The coordinator receives only a bounded immutable setup description bound to the execution and manifest digest. It checks its expected setup, closes setup-only authority, establishes its own protected control/status handling, and reports `Held` through the trusted channel. The external owner corroborates establishment. Target-controlled stdout cannot satisfy this step.
4. The coordinator blocks before any target fork/exec. Its control parser accepts at most one valid release frame for this instance. EOF, timeout, abort, malformed/truncated/oversized data, wrong version/identity/digest and duplicate authorization fail closed. Channels are capability-bearing descriptors; an execution ID in a frame is binding, not a security secret or an authentication substitute.
5. The external arbiter writes and flushes a bounded ReleaseIntent record, then rechecks expiry, stop and establishment at its sole commit point. A persisted intent can conservatively mean may-have-executed after a crash even if no frame was sent. This is safer than promising an atomic disk-write-plus-exec operation.
6. The arbiter consumes the preparation and commits release once; delivery is bounded and coordinated with stop. It never awaits target output while holding the ordering lock. Cancellation arriving after commit is a post-release stop, even if exec is not yet observed. A frame-send failure is ambiguous and causes domain stop, not retry.
7. After authorization, the coordinator starts the target using the bound executable and immutable argv/environment. Every release/status/setup FD is closed or close-on-exec in the target. The target gets only the permitted data descriptors; confinement inherits across fork/exec.
8. To distinguish target result from wrapper result, the coordinator remains a minimal trusted direct-parent status reporter, waits/reaps its own target and reports raw wait status over a channel the target never inherits. It carries no host cgroup control and no post-release grant/release capability. Then it exits. Domain emptiness is still established externally.

Step 8 requires a concrete protection review: prevent the target from reopening coordinator FDs through proc/process APIs, altering the reporter, or forging messages. Use minimal descriptor ownership, explicit non-dumpable coordinator state and the approved process-access restrictions; validate compatibility with the frozen filter. Target destruction of the reporter must yield Unknown, never a forged successful root result. If this cannot be established without a broader launcher/security redesign, stop at review gate G2; do not implement an unreviewed guardian.

Exec observation also needs a source-reviewed chain. A close-on-exec error pipe can carry a positive exec failure; EOF alone may also mean pre-exec child death and cannot prove successful exec. Correlate trusted coordinator state, error reporting and owned-child status. Classify unresolved cases MayHaveExecuted. A root exit value equal to a wrapper signal convention is not enough to infer the target signal. A trusted setup coordinator is allowed by the ADR; it is not an additional confinement boundary.

The coordinator must use a bounded wait/abort protocol and never make release depend on cooperative target code. Native helpers are small enough for a source/descriptor/state review. If the pinned Bubblewrap source already provides equivalent supported primitives, the implementation review may use them instead, documenting equivalent semantics and removing redundant coordinator work. That substitution must satisfy the same pre-init, identity, status and descriptor requirements.

## 9. Bubblewrap adapter

Pin Bubblewrap 0.11.1, SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`, and initial AR-7 filter SHA-256 `30bfaf6b62030de8b105e1ac8de43f2a9884b2bd5de20e98aef81dce426c1aad`. Record the actual source/build provenance available; a matching hash does not complete the supply-chain gate. Require non-setuid/non-setgid mode and no file-capability elevation.

Before constructing argv, inspect the pinned helper source/options for descriptor preservation, seccomp installation, held coordination and status semantics. Do not infer an option's behavior from its name or pass an FD without an explicit installation option. This is implementation integration work, not a rerun of AR-7 feasibility.

Construct a vector, never shell text. Use a protected absolute helper identity and replacement-resistant execution binding. Clear ambient environment, set an explicit minimal map, disable nested-userns regain as in the approved baseline, drop all target capabilities, use private proc/dev views and expose no host cgroupfs, home, bus, agent or daemon paths. No broad `/usr`, `/run`, `/sys` or host library-tree grant. The static fixture requires no discovered loader closure.

Maintain a descriptor ledger covering owner, spawn child, Bubblewrap setup, coordinator and target. The filter FD is consumed/closed after installation; it is not target data. Dedicated release and status channels are never aliases for stdout/stderr. Preserve only explicitly required setup FDs through helper launch, then close them before target exec. Do not weaken the legacy close-from behavior globally.

Return an `EstablishmentReport` only after checking domain membership, actual filter/NNP/capability state, namespace separation, mount plan and resource readbacks. Target self-reports may corroborate but cannot establish a required item. Missing interfaces produce Unsupported/FailedToEstablish, never a permissive fallback. A helper exiting zero with no trusted target status yields a wrapper observation only.

## 10. Lifecycle ownership and cgroup model

Use the following M1 topology; all runtime roles are unprivileged:

```text
external test/recovery controller (outside target domain)
  └─ per-execution Gagamba owner (separate control context)
       └─ target cgroup: Bubblewrap → trusted coordinator → target → descendants
```

The external test controller supplies pre-existing delegated resources and retains emergency cleanup authority. It must not mask missing production behavior: separate tests measure owner-triggered stop from controller fallback, and any fallback needed to finish a claimed owner-death leg makes that leg failed/pending. A root safety observer, if used by a future native test environment, remains fixture infrastructure, not an implementation dependency.

Create one fresh leaf with stable directory/control FDs. Configure finite limits before admitting setup helpers; retain a private mapping to execution/boot/object identity. Use the existing atomic placement mechanism only after checking its stated libc/kernel requirements and native behavior on the test host. No unowned launch followed by migration. A held alternative is acceptable only with a reviewed structural no-target-exec guarantee and complete recovery ownership.

The owner remains outside the target domain. Its output buffers and management allocations use separate bounded admission; a target-domain kill must not kill the readers or erase recovery records. The target cannot mount or recover parent/sibling controls. Fork, setsid, reparenting and root exit do not change ownership.

Primary stop is write to the owned `cgroup.kill` control, then verify `populated 0` and reap owned direct children. Do not enumerate numeric PIDs to implement termination. Direct-child wait operations are status observation under an exclusive reaper; they are not authority to kill an arbitrary process later.

For M1, the owner monitors a dedicated caller-liveness channel outside the target. Caller disconnect latches stop. Manager and owner failure are separate fault cases. The ADR requires a qualified death path; M1 exercises the chosen helper parent-death/init chain and independently observes domain emptiness. It does not claim every production crash/restart/deployment case is solved. If owner loss can leave running work without the proposed automatic containment path, retain Unknown, clean through the independent controller and stop M1 acceptance for that case.

After losing the owner, a surviving external recovery controller may reconcile recorded objects. Production selection of a durable recovery host remains a qualification/deployment gate; this plan does not install a daemon to make it disappear. Preserve the distinction between surviving domain ownership, automatic timely stop and later cleanup.

## 11. Resource installation and verification

The integrated reference configuration is fixed, not caller-tunable in M1:

| Dimension | Initial M1 setting | Verification / refusal |
|---|---|---|
| Target-domain memory | 134217728 bytes, including helpers | Effective `memory.max` readback; finite touched-allocation test and event deltas. |
| Swap | 0 bytes | Effective `memory.swap.max=0`; unavailable control refuses. Do not invent unmeasured usage. |
| Tasks | 48 Linux tasks including helper threads/processes | `pids.max` readback; finite creation test and event deltas, never label as 48 processes. |
| FDs | Hard and soft `RLIMIT_NOFILE=64` for target | Trusted establishment/readback and finite EMFILE test. Setup overhead must fit or report incompatibility. |
| Scratch | 8388608-byte aggregate writable backing budget | Prefer one private bounded tmpfs with logical tmp/cache/shm subdirectories or explicitly partition the same total; confirm actual mount and accounting behavior. |
| stdout / stderr | 65536 bytes each; combined retained cap 131072 bytes | Two owner-side drains; per-stream and total overflow triggers; exact byte accounting. |
| stdin | Closed/EOF data source | No inherited terminal, socket, release pipe or caller stdin. |
| Time | 10-second setup budget within a 30-second overall deadline | Monotonic clock active before preparation; boundary-injected expiry tests. |
| Final stop / drain | 5-second healthy-host stop observation; 1-second final drain after domain stop | Expiry yields Failed/Unknown or DrainIncomplete, not fabricated completion. |
| Staging | 16 MiB, <=128 objects; fixture/input limits in section 7 | Bounded copy and record processing outside target domain. |
| Dumps | `RLIMIT_CORE=0`, no requested runtime dump facilities | Establish/read back; retain host sink exclusion as pending. |

These chosen output/time/staging numbers are M1 test configuration, not measurements or new public promises. Record actual helper task overhead; do not add hidden task allowance. Stricter inherited limits must be exposed and accepted in a new fixed configuration or refused, not silently substituted.

Reserve schema fields for inode/object ceilings, but record independent inode/metadata enforcement as PendingQualification. A pending production dimension cannot be reported Established by the production profile. M1's finite fixture file count and independent envelope bound test risk, not production inode enforcement. Do not implement a privileged tmpfs quota workaround. The ADR still makes explicit inode bounds mandatory before production qualification.

Inventory `/`, private `/dev`, `/proc`, `/tmp`, `/dev/shm`, `/run`, `/sys`, temporary mount staging, pipes, anonymous/SysV shared memory and manager-owned artifacts. Prefer read-only or absent unused writable paths. If a setup-created tmpfs remains writable, include its explicit byte/accounting budget or refuse. An 8 MiB `/tmp` mount does not bound all other writable mounts. Validate the actual configured view, not merely HOME/TMPDIR values. Inability to express bounded storage with the selected helper is a stop/review condition.

Resource installation is before Owned; package order does not postpone limits until after release. Earlier held-launch tests retain an outer finite safety envelope and cannot enable target release until WP06-WP09 are integrated. No RLIMIT_FSIZE-based substitute is required: P04 showed compatibility pressure, and M1 uses bounded backing stores. Exact CPU-rate and exhaustive kernel-resource accounting remain outside this milestone.

## 12. Bounded stdout/stderr enforcement

The owner allocates two readers before release and starts concurrent byte drains independent of any user observer. Use bounded buffers and prefix retention. Decode for display only after byte enforcement. No `ReadToEnd`, `communicate`-equivalent full collection or unbounded diagnostic queue. No spill to an unbounded host file.

On the first byte beyond either limit or the combined limit, atomically latch the stream/cause and request whole-domain stop. Continue only bounded-buffer draining/discard while stop is in progress. Stop deadline, reader progress and result observation are independent tasks. Counters use overflow-safe arithmetic; saturation is recorded as a lower bound rather than wrapping.

On reader fault, immediately notify the owner and stop; do not wait for root completion to discover that a broken reader can deadlock a writer. Keep stderr drainage active when stdout fails and vice versa while safely possible. Returned prefixes never exceed their limits. Total bytes observed are not guaranteed to equal total bytes the target attempted to emit.

M1 has no streaming delivery API. Its caller receives a finite result after capture. Tests can attach a stalled diagnostic subscriber to prove observer backpressure cannot stall the drains; implementation logging must be bounded and nonblocking or cause an explicit management failure. Closing a reader intentionally is capture failure/incompleteness, not clean EOF.

A root may exit while descendants retain stdout/stderr. Observe the domain independently. Wait for genuine EOF within the overall lifecycle and final-drain bounds; stop the entire domain when required. If a writer remains after purported empty-domain cleanup, record an invariant failure/Unknown and investigate descriptor escape rather than infer success from the root status.

## 13. Failure, cancellation and Unknown-result semantics

| Event | Execution disposition | Required action |
|---|---|---|
| Invalid request, missing helper/control, unsupported host | Refused before release | Reclaim any preparation; never call legacy launch. |
| Preparation or held setup fails | FailedToEstablish; NotReleased | Abort control channel, kill allocated setup domain if necessary, reconcile. |
| Stop wins before release | CancelledBeforeRelease; NotReleased | Consume/retire preparation, no release frame, teardown. |
| Release wins then stop | Stop requested after commit; target may have executed | Whole-domain kill; preserve any valid root/exec observations. |
| Crash after release intent/commit but before exec observation | Unknown with MayHaveExecuted | Never resend/replay; independent reconciliation. |
| Helper exits without trustworthy target result | Wrapper result known; root Unknown | Stop remaining domain and report uncertainty. |
| Normal target exit 0 or nonzero | Known root exit, subject to domain/capture completion | Preserve exact exit; nonzero is not automatically a policy violation. |
| Target killed / resource event | Record raw root status if independently known, plus limit event/cause | Do not synthesize a signal solely from wrapper code 128+N. |
| Output overflow/read fault/deadline | Known stop trigger, root possibly Unknown | Domain stop with bounded capture/cleanup; do not overwrite a previously known root result. |
| Observer wait cancelled | No execution state change | Detach observer only. |
| Cleanup observation unavailable | Cleanup Unknown, execution result unchanged | Retain debt and refuse reuse. |

Use an injected monotonic clock for portable ordering tests and native barrier events for integration ordering. Wall-clock timestamps are audit metadata, not release ordering authority. Persist one terminal execution observation; later cleanup reconciliation creates a linked cleanup update. Preserve the first accepted stop cause and bounded additional observations without racing to invent a definitive causal result.

## 14. Cleanup and reconciliation

One owner coordinates teardown; concurrent stop/dispose/wait paths join it. Cleanup is idempotent for verified owned objects and retryable after a known removal failure. It must not consume the only control handle before proving the domain empty.

Order: forbid further release; close unused release writers; terminate the domain when required; verify emptiness; reap direct trusted children; finalize bounded capture; close held namespace/mount references; reclaim scratch and staging through retained trusted roots; remove verified empty cgroup leaves; close controls; persist component results and only then retire cleanup debt. Keep the record needed for uncertain components.

Use descriptor-relative no-follow traversal with object-class checks for scratch deletion. Reject or quarantine unexpected mounts/aliases. Directory contents are hostile even after processes stop. No generic recursive deletion over target-provided absolute paths, and no reuse of `OwnedCgroup.Adopt(string)` as proof that a recovery record owns a path.

Reconciliation validates record version/size, execution identity, host boot identity, configured trusted root, and object identities before any stop/delete. Reopened path equality or inode number alone is not proof after possible replacement/reuse. M1 can use controller-retained handles for strong same-boot recovery. If all handles are lost and identity cannot be re-established safely, preserve Unknown and require an independently reviewed recovery action; never kill/delete the object currently occupying the old name.

The journal uses atomic record replacement and the required file/directory durability ordering. A failed flush blocks release. Test a torn/truncated record, crash before/after object registration, failed unlink/removal, permission denial and replaced resources. Reboot changes process-lifetime assumptions but does not prove storage cleanup. Cross-reboot recovery is explicitly a later production qualification gate.

Unknown root results never become success after a sweep. Retained failed/unknown debt blocks the single M1 execution slot until reconciled or explicitly quarantined under the reviewed recovery policy.

## 15. Production evidence model

Implement the bounded production concepts in the ADR, but keep M1 receipts marked internal/unqualified. Record execution/preparation IDs; package/source identity when available; helper/filter/coordinator/fixture/input digests; authority/resource manifest digests; host/delegation/boot context; establishment outcomes; release disposition; stop trigger; root and wrapper results; output counters/prefix status; domain observation; cleanup components and refusal reasons.

Separate three artifacts:

- A small semantic receipt safe for the trusted caller, with redacted environment/arguments and no native control handles or reusable authority secrets.
- A protected operational ownership journal containing the identities necessary for cleanup.
- Qualification evidence with source-bound fixture events, independent oracles, exact test counts and diagnostic observations. Target output is untrusted payload, never a receipt.

Future evidence header fields follow the remediation standard: baseline/final SHA, clean/dirty state, branch, native host/kernel/libc/architecture/UID/delegation, toolchain versions, pins, exact scope and exclusions. Record tests as Passed/Failed/Skipped/Unsupported; portable green and emergency cleanup are not native enforcement passes. Preserve source-before-run identity and commit raw records needed to substantiate decisive transitions; large logs remain in ignored artifacts with durable hashes and retention notes.

Each work package appends an entry to the future `docs/evidence/gagamba-linux-m1.md`, with supporting small records under `docs/evidence/linux-m1/wpNN/` when native claims begin. Do not create fictional evidence or success placeholders now. Link baseline source facts rather than inventing a failing baseline when a capability did not previously exist. Original F01 remains open.

## 16. Ordered implementation work packages

The package order matches the requested sequence. Two dependencies run ahead of full implementation: WP01 defines minimal recovery/abort contracts because allocation needs them immediately, and WP03 supplies an emergency owned teardown path before WP04 can launch trusted setup. WP06-WP09 remain mandatory prerequisites for integrated target release. This avoids a runnable partial sandbox while preserving small reviewable commits.

### WP01 — Internal contracts and state model

- **Objective:** freeze M1 semantics and an executable transition model without exposing a public restricted provider.
- **Expected files/components:** internal `RestrictedManifest.cs`, `RestrictedTransaction.cs`; M1 project and tests only when populated with working model code; minimal ownership-journal interface.
- **Contracts/invariants:** I01-I14 vocabulary; immutable issuer-bound preparation; one release/stop arbiter; independent execution/output/cleanup state; no replay.
- **Dependencies:** ADR and this plan; inspect existing provider/capture contracts before choosing reuse.
- **Unit tests:** mutation through caller collections; foreign/expired/replayed preparations; competing release/stop/discard calls; cancelled waiters; stable terminal observation; unknown plus confirmed cleanup remains unknown.
- **Integration/fault tests:** no target launch yet; public runtime cannot select M1 and unsupported-platform construction refuses. Native enforcement explicitly not exercised.
- **Retained evidence:** API/state decision table, passing portable test counts, legacy API surface comparison and exact source.
- **Stop condition:** unresolved semantics, any public SPI change without justified review, or hidden route from M1 to legacy launch. Review G1 before native implementation.

### WP02 — Protected preparation and copy-in

- **Objective:** produce the exact protected fixture/input snapshot accepted for release.
- **Expected files/components:** `PreparedClosure`, `SafeCopyIn`, narrow native object-resolution helpers; fixture source/build identity definition; bounded preparation tests.
- **Contracts/invariants:** I05/I13; destination identity defines the snapshot, no source hardlink reuse, no target execution during preparation, allocation intents recoverable.
- **Dependencies:** WP01; supported local Linux filesystem resolution and staging-root ownership contract.
- **Unit tests:** canonical manifest bytes/digest, object/byte admission counts, environment/argv copies, malformed paths and budget validation.
- **Integration/fault tests:** regular-file positive; source mutation/replacement, symlink/FIFO/device refusal, hardlinked source copied to independent inode; injected interruption at create/copy/seal/publish and partial-resource reclamation.
- **Retained evidence:** source/destination identity and digest observations, copy limits, barrier ordering, cleanup components; no general directory-import claim.
- **Stop condition:** mutable published destination, changed fixture bytes accepted, path escape or incomplete resource registration. No held launch until copy-in binding passes focused review.

### WP03 — Lifecycle-domain ownership

- **Objective:** allocate an externally stoppable, identity-bound domain and establish atomic setup admission.
- **Expected files/components:** `RestrictedDomain`, `DomainLease`, bounded journal implementation, owner-host skeleton with working allocate/abort behavior; atomic-spawn adapter.
- **Contracts/invariants:** I03/I04/I11; controls never enter target descriptor set; kill/empty/reap separate from removal; recorded identities before target effects.
- **Dependencies:** WP01-WP02 and real non-root cgroup v2 delegation. Missing delegation is Unsupported, not root fallback.
- **Unit tests:** controller/readback parser failures; lease retirement/idempotency; ownership record bounds; no numeric-PID termination API.
- **Integration/fault tests:** trusted finite setup child born in correct leaf; caller/controller separation; allocation/spawn/registration failure; kill denial, population-read failure and wrong-object recovery refusal. Keep workload synthetic and finite.
- **Retained evidence:** native membership/readback, exact UID/delegation and libc path, independent empty-domain evidence, failure cleanup records.
- **Stop condition:** post-exec migration needed, root-only enforcement dependency, unregistered child, path-only adoption or unproven cleanup. G2 topology review before WP04.

### WP04 — Bubblewrap held launch

- **Objective:** launch only the trusted held coordinator under the pinned composition with no target initialization.
- **Expected files/components:** `IHeldLauncher`, `BubblewrapAdapter`, `LaunchDescriptorMap`, native coordinator/parser and native build script.
- **Contracts/invariants:** I01/I03/I10/I14; actual filter installation; protected immutable setup; helper status distinct from target status.
- **Dependencies:** WP02-WP03; G2 approval of pinned-helper source/options, descriptor ledger, coordinator status protection and abort behavior.
- **Unit tests:** argv determinism and injection resistance; exact helper/filter pin rejection; bounded protocol framing, partial/duplicate/wrong-instance frames; descriptor map closure.
- **Integration/fault tests:** Held reached with external namespace/domain/filter checks; target constructor has not run by structural launch path; absent/mismatched helper, omitted filter installation and owner EOF all refuse/abort. This package has no enabled target-release path.
- **Retained evidence:** reviewed protocol, helper/coordinator sources and hashes, actual argv/FD roles, held-phase kernel observations and clean abort.
- **Stop condition:** target code enters held setup, undeclared inherited FD, filter merely passed but uninstalled, target-forgeable control, or need to replace the namespace engine. Do not copy the AR-7 gate to bypass review.

### WP05 — Release arbiter and pre-init boundary

- **Objective:** implement the one-way authorization transaction and truthful exec/root status protocol.
- **Expected files/components:** `ReleaseArbiter`, coordinator release/fork/exec/wait reporting, execution observation aggregator and deterministic test barriers.
- **Contracts/invariants:** I01/I02/I05/I06/I09; flushed release intent, final recheck, single commit, no retry on ambiguous delivery, non-forgeable root status.
- **Dependencies:** WP01-WP04 and G2 protocol approval. Production-path target release remains disabled until WP06-WP09 pass; test the arbiter first with a fake held endpoint.
- **Unit tests:** all release/stop interleavings; expiry during intent flush; consumption after failed delivery; duplicate/late authorization; root status separate from helper; EOF alone never asserts successful exec.
- **Integration/fault tests:** coordinator protocol with trusted synthetic endpoints; crash before commit/after intent/after commit/at exec-report delivery. Actual fixture release cases are queued for WP10 after all enforcement prerequisites exist.
- **Retained evidence:** linearization trace, write-ahead record rules, source-reviewed only-exec path, status-forgery/FD analysis and pending native cases.
- **Stop condition:** release after a stop accepted first, async commit path bypassing recheck, reauthorization/replay to resolve ambiguity, or inability to distinguish trusted target observation from wrapper output. G3 release review is mandatory.

### WP06 — Resource installation and readback

- **Objective:** enforce and verify the fixed M1 budgets before Owned.
- **Expected files/components:** resource installer/readback in `RestrictedDomain` and adapter, writable-mount inventory, rlimit installation in trusted setup and manifest validation.
- **Contracts/invariants:** I08/I13; explicit units/task accounting; swap zero; all configured writable backing finite; pending inode qualification never reported solved.
- **Dependencies:** WP03-WP05, pinned helper's reviewed storage options, reference configuration in section 11.
- **Unit tests:** malformed/missing/stricter-than-requested readbacks; unavailable controllers; aggregate storage accounting; hard-limit installation failure; helper overhead and no automatic uplift.
- **Integration/fault tests:** held setup limit readbacks; deliberate omitted/incorrect limits prevent Owned; inspect all writable mounts. Finite native memory/task/FD/scratch exhaustion cases execute only in WP10 after the complete path exists.
- **Retained evidence:** requested-versus-established table, mount inventory, exact limits, known inode/core/accounting gaps and refusal records.
- **Stop condition:** unbounded writable location, missing control accepted, hidden extra storage or budget increase. No privileged provisioning or broader mounts to make it pass.

### WP07 — Bounded stdout/stderr

- **Objective:** install continuous bounded capture and prompt owner notification before any release.
- **Expected files/components:** `RestrictedOutputCapture`, restricted pipe owner/descriptor mapping; narrowly reviewed shared capture changes only if needed.
- **Contracts/invariants:** I07; independent concurrent drains; bounded prefix and queue memory; overflow/read-failure events independent of lifecycle completion.
- **Dependencies:** WP01/WP03/WP05-WP06; capture readers ready is a mandatory Owned condition.
- **Unit tests:** exact-limit and limit+1 per stream, simultaneous floods, total cap, binary data, integer saturation, reader fault before root completion, abort/final-drain races, blocked subscriber.
- **Integration/fault tests:** owned finite pipe-writer helper for reader wiring; target flood/descriptor-retention scenarios scheduled in WP10. Verify no ambient stdin or extra writer in owner/helper paths.
- **Retained evidence:** maximum retained allocations and bytes, overflow trigger/cause, bounded stop/drain behavior and legacy AR-5 regressions if shared code changes.
- **Stop condition:** full-stream buffering, sequential stream drain, unbounded spill/logging, reader failure noticed only at completion, or overflow detached from domain stop.

### WP08 — Cancellation, deadline and failure handling

- **Objective:** connect all stop sources to the arbiter and preserve truthful execution state during observation loss.
- **Expected files/components:** `RestrictedExecutionOwner`, caller lease, monotonic deadline controller, bounded result aggregator and per-execution owner executable.
- **Contracts/invariants:** I02/I03/I06/I09; setup-inclusive timer; stop request acceptance separate from confirmed emptiness; external enforcement outlives an observer.
- **Dependencies:** WP01-WP07; early abort/cleanup path from WP03; no integrated release until WP09 complete.
- **Unit tests:** setup timeout, final release expiry, cancellation-before/after commit, caller lease closure, helper/reader/journal failure, multiple waiters and stopped observer.
- **Integration/fault tests:** held-owner/caller termination and independent retained domain controls; owner-host process fault boundaries. Post-release native cases execute in WP10; emergency controller actions are separately counted.
- **Retained evidence:** stop-cause and phase matrix, independent authority ownership, failed/unknown paths and timing measured only on the declared healthy host.
- **Stop condition:** loss of caller/manager loses all stop authority, deadline exists only after launch, or cleanup is inferred from cancellation/timeout. Do not create a new privileged watchdog service.

### WP09 — Cleanup and reconciliation

- **Objective:** make every partial allocation and released transaction safely terminable/reclaimable, with durable uncertainty.
- **Expected files/components:** `OwnershipJournal`, `Reconciler`, object-safe scratch/staging cleanup and component reports; fault seams scoped to tests.
- **Contracts/invariants:** I04/I06/I09/I13; idempotent owned cleanup; no name/PID-only action; immutable execution result with separate cleanup updates.
- **Dependencies:** WP02-WP08; trusted root/identity and journal durability review.
- **Unit tests:** journal version/size/torn records, phase monotonicity, partial component results, retry state, boot mismatch, stale IDs, execution Unknown surviving successful cleanup.
- **Integration/fault tests:** crash after each recorded allocation; controller-held handle reconciliation after owner death; removal denial, swapped path/cgroup, hostile scratch links and unknown mount refusal; no unrelated fixture touched.
- **Retained evidence:** exact residual identities, component transitions, independent absence/emptiness checks and conservative unknown outcomes.
- **Stop condition:** deletion outside owned objects, replay/resume during recovery, stale identity action or debt discarded on error. G3 confirms cleanup readiness before integrated release.

### WP10 — Integrated native transaction

- **Objective:** run one complete useful static-native transaction through the implementation with all M1 prerequisites enabled.
- **Expected files/components:** populated fixture modes, `run-m1.sh`, end-to-end tests and receipt writer; connect WP01-WP09 without public runtime registration.
- **Contracts/invariants:** all I01-I14; no fixture shortcut through a legacy launch; full M1 establishment prior to release.
- **Dependencies:** WP01-WP09 and G3 sign-off. One execution at a time in a finite independent test envelope.
- **Unit tests:** receipt aggregation and never-launching/broken-adapter controls; state tests remain green.
- **Integration/fault tests:** useful input digest and scratch work, exact exit 0/17, constructor-before-main boundary; deterministic stop-wins/release-wins; missing restriction refusal; bounded memory/task/FD/storage exhaustion; output floods/retained writers; target/helper/owner failures and live-descendant domain stop.
- **Retained evidence:** exact clean source/build identities, limits/establishment, trusted phase ordering, root/helper results, output counters, cleanup components and independent oracle records for every claimed case.
- **Stop condition:** unsafe boundary result, emergency fallback needed for a claimed automatic stop, misleading status or cleanup uncertainty. Preserve and resolve residue before further runs; no compatibility widening.

### WP11 — Focused P05/P06-style validation

- **Objective:** challenge this narrow implementation's immutable preparation, single-use release and object safety without claiming full P05/P06 completion.
- **Expected files/components:** adversarial tests in the M1 project, finite replacement/link fixtures and durable evidence additions.
- **Contracts/invariants:** I01/I02/I05/I06/I13; exact prepared snapshot runs once; cleanup remains in its owned scope.
- **Dependencies:** WP10 useful control accepted and confirmed cleanup.
- **Unit tests:** manifest/token mutations, wrong issuer/digest/profile, bounded parser and canonicalization negative cases; avoid implementation-mirroring assertions.
- **Integration/fault tests:** swap fixture/input/parent paths at named preparation/release barriers; attempt re-release and foreign preparation; corrupt/truncate journal; malicious scratch links/aliases; invalid helper/filter pin; initialized target must never precede commit. No export tests imply an export API exists.
- **Retained evidence:** claim-to-positive/negative-control matrix, exact unexecuted variants, source binding and cleanup proof; retained failures superseded explicitly, never overwritten.
- **Stop condition:** useful-work control absent, missing native prerequisite counted as pass, changed bytes accepted or any claim exceeding the narrow tested object set. G4 scope review before final independent review.

### WP12 — Independent review and handoff stop

- **Objective:** obtain a read-only implementation/evidence review and decide the M1 disposition.
- **Expected files/components:** `docs/reviews/<date>-gagamba-linux-m1-review.md`, final M1 evidence and bounded handoff/queue documentation. No automatic next feature.
- **Contracts/invariants:** architecture is preserved; every M1 claim maps to code and native evidence; all production gaps remain explicit.
- **Dependencies:** WP10-WP11 evidence at an exact source revision, relevant regressions complete and no unexplained cleanup residue.
- **Unit tests:** none added merely for this documentation step; inspect recorded coverage and meaningful negative controls.
- **Integration/fault tests:** reviewer executes none by default; targeted evidence corrections require explicit scope and new source binding. No blanket rerun of accepted AR-7.
- **Retained evidence:** reviewer scope, material findings, accepted/rejected findings with rationale, required repairs and precise final disposition.
- **Stop condition:** stop after review and any expressly scoped repairs. If native qualification is unavailable, record `IMPLEMENTED — NATIVE QUALIFICATION PENDING`; do not mark M1 native-complete. Do not add exports, .NET, Hufu/workflow or public support without the next reviewed slice.

## 17. Test and fault-injection strategy

Portable tests establish state/contract behavior only. Delegated Linux tests establish the actual implementation mechanisms on the measured host. Independent control processes observe effects outside the target; a target marker corroborates an action but never serves as installation or cleanup proof.

The future native runner must validate its own environment, require explicit paths for the approved helper/delegation/staging root, and refuse unmet prerequisites without installing software or changing host security configuration. Do not automatically call old AR-7 drivers. Build the new source-bound finite fixture and coordinator in the implementation phase, commit their source before evidence runs, and record exact binary hashes/toolchain.

| Test family | Positive control | Required negative/fault evidence |
|---|---|---|
| Preparation identity | Exact copied fixture reads expected copied bytes | Source/destination replacement, foreign/replayed preparation, expired/different manifest never releases altered target. |
| Held phase | A later valid commit produces useful work | External held snapshot plus reviewed sole exec path; constructor/input access cannot start before commit. Silence alone is insufficient. |
| Ordering | Release-first case executes under ownership | Stop-first and expired-setup cases send no release; ambiguous commit crash stops without retry. |
| Status | Deliberate root exit 17 and separate wrapper status | Exec failure, coordinator death, lost status and wrapper-only exit never synthesize root success. |
| Resources | Finite under-budget workload succeeds | Above-budget operations fail/stop with matching readbacks/events where applicable; task/FD/storage units are truthful. |
| Capture | Exact finite bytes on both streams | Both-stream flood, blocked reader/subscriber and retained descriptors preserve bounded memory and trigger correct stop/incomplete status. |
| Domain lifetime | Finite descendant demonstrably alive before stop | Fresh live-descendant and mid-spawn stop, caller/manager/owner loss, no numeric-PID sweep; actual domain emptiness independently observed. |
| Recovery | Valid retained ownership record reconciles once | Torn/wrong-boot/replaced-object records cannot delete or kill an unrelated controlled sentinel; Unknown stays Unknown. |
| Refusal | Valid internal fixture path can run | Wrong helper/filter, omitted installation, unsupported controllers and never-launching adapters fail the appropriate controls without legacy fallback. |

Use named barriers at allocation intent/creation/identity persistence, copy completion/publication, held helper readiness, limit readback, release-intent durability, release commit/delivery, target exec/report, root exit, domain-empty observation and each cleanup component. Pause trusted test orchestration at those barriers; do not put the enforcement boundary in the target fixture. The static fixture should include constructor-side effects before main to challenge a late barrier, but the authoritative proof remains the source-bound launch structure plus external observations.

Every native case has a fresh execution identity, bounded workload/output, an independent finite watchdog and cleanup controller. Kill only owned test processes using stable child handles or their test domains. Verify unrestricted positives inside the independent safety envelope are permitted, so outer denial cannot masquerade as enforcement. Do not access real credentials, services or metadata endpoints. Full transport campaign belongs to later production qualification.

Run appropriate existing core/provider/AR-5 Linux/runtime regressions when shared code is affected; do not broaden legacy behavior as part of this milestone. No skips become passes. The M1 evidence table lists proposed tests that remain unexecuted separately from measured outcomes, including both release/stop orderings and crash-after-commit uncertainty.

## 18. Review gates

| Gate | When | Required review / exit |
|---|---|---|
| G1 — contract | After WP01 | Immutable preparation, state/result vocabulary, single linearization authority, no public fallback and no policy dependencies. |
| G2 — native topology/protocol | Before WP04 target-capable code | Pinned helper option/source verification, descriptor ledger, coordinator only-exec path, protected root-status reporting, delegated ownership and crash authority. A material launcher/service redesign returns to architecture review. |
| G3 — integrated release admission | After WP05-WP09, before WP10 | All M1 limits/readbacks installed, two readers active, deadline/stop/recovery armed, staging immutable, missing-item refusal and safe cleanup. No target release merely because helper launch works. |
| G4 — evidence scope | After WP11 | Useful positives, actual phase proof, exact source/build identities, conservative result/cleanup and unclaimed production gaps. |
| G5 — independent milestone decision | WP12 | Reviewer accepts the narrow native transaction or records blockers/pending qualification; authorizes no broader product automatically. |

These gates are engineering reviews within the ADR's existing implementation authorization, not repeated requests for permission for ordinary edits. New privileged components, authority widening, a substantial namespace launcher replacement or profile weakening require a new architecture decision. A detected boundary failure halts affected execution work and triggers safe cleanup and review.

Architecture ambiguities that require focused implementation decisions are bounded rather than silently assumed solved:

1. The ADR fixes pre-init release semantics, but not the coordinator/status protocol. Section 8 proposes one; G2 must verify it against the pinned helper and process-access model. If a protected truthful reporter is infeasible without a broader topology, stop that package.
2. The ADR requires external recovery authority without selecting a persistent deployment host. M1 uses an unprivileged per-execution owner and external test/recovery controller; production recovery integration stays pending, and emergency controller cleanup does not count as automatic owner-death enforcement.
3. The precise byte-bounded writable mount layout depends on supported pinned-helper behavior. WP06 must establish a complete finite inventory; independent inode/metadata enforcement remains future qualification. Do not silently accept default writable tmpfs mounts.

These points do not prevent precise initial contract/copy-in/domain work. They are explicit stop gates before the dependent integrated release, not permission to invent missing guarantees.

## 19. Remaining production-qualification gaps

M1 does not close the following:

| Gap | Required future disposition |
|---|---|
| Full P05 | Complete production preparation/release authorization, mutation/replay/expiry and failure coverage beyond one internal fixed fixture. |
| Full P06 | Complete supported filesystem/object/alias/mount matrix, writable-state qualification and safe export API. |
| Inode/metadata enforcement | Independently configured and demonstrated finite bounds before production, as required by ADR 0003. Fixture file-count caps/memcg observations are not substitutes. |
| Complete network/IPC and loopback | Full applicable direct/alternate syscall and inherited-channel matrix; private IP loopback prohibition remains unproved by AR-7. |
| Complete owner-death | Qualified production recovery host, all crash windows, no leaked keepalive authority, restart/reboot identity handling and declared healthy-host stop behavior. |
| P08 runtime | Complete useful runtime closure and supported-host compatibility evidence. A static fixture establishes no general runtime entitlement. |
| P09 | Exact package/source/helper/filter/profile security manifest, negative gates and independently installed consumers. |
| .NET closure | Complete published files/native dependencies, identity bindings, writable state, budget and diagnostic/core-sink qualification. |
| Hufu/workflow | Later adapter and authority/revocation contract integration; no dependencies added in M1. |
| Unsupported platforms | Windows native/macOS refusal remains; Windows VM and further native designs require their own decisions/qualification. |
| Supply chain / hosts | Complete provenance and upgrade qualification; explicit kernel/libc/architecture/filesystem/delegation/LSM support conditions. |

Preserve every sealed non-claim: no general Linux security or production readiness; tested WSL2/pinned-helper/UID-delegation scope and unknown LSM state; incomplete syscall/IPC coverage; no independent inode bound or streaming-output enforcement demonstrated by AR-7; no measured minimum .NET budget; no exhaustive host core-sink exclusion; incomplete published-.NET closure; P03 manager-leaf placement exception; P04 charging/configuration qualifications; no general C7/C8 descendant-elimination guarantee; R5 chronology and R6 PID-reuse qualifications; Unknown results remain Unknown; unavailable `39d1` and superseded `a7d8` cleanup uncertainty are not rehabilitated. P01 does not prove native Windows impossible; P01B remains inconclusive; no P01C, Windows VM or macOS qualification. P05/P06/P08/P09 were unexecuted at the seal. R2 `/gate` is not a production release mechanism.

New M1 measurements may establish narrow implementation-specific facts without retroactively upgrading AR-7. Original F01 stays open until production qualification and final independent security review.

## 20. Implementation-agent handoff

Start in the Gagamba repository with this plan and the committed ADR above. Inspect current Git status and preserve unrelated changes. Do not rewrite the sealed evidence or frozen AR-6 refs. The planning commit contains documentation only; no component or test named here should be assumed to exist until verified.

Begin WP01, then WP02 and WP03. Implement populated internal components in small ordered commits and record exact source identity. Keep the public lifecycle SPI and ordinary runtime behavior intact. Use the proposed non-packable internal project/owner arrangement, with no Hufu/workflow dependency and no public restricted-provider registration.

Before WP04, resolve G2's coordinator/descriptor/status and delegated-ownership design using the pinned helper. Do not copy the test gate or mistake target stdout for readiness. Preserve the sole release/stop linearization point and never replay an ambiguous release. WP06-WP09 are prerequisites for integrated native target release, even though their implementation follows the contract/held-launch packages.

Run only the new bounded M1 validation required by implemented work, with synthetic resources, meaningful controls, an independent cleanup controller and per-case retained evidence. Do not rerun accepted AR-7 feasibility or expand into .NET, exports, network qualification campaigns or alternative platforms. Unsupported hosts produce explicit pending/refusal evidence.

At WP12, request an independent read-only review of exact implementation and retained evidence. Record findings and any separately scoped repairs. Stop when the narrow M1 disposition is recorded; the next slice requires that review's concrete handoff. No package publication, public API stabilization, merge into a release line, privileged service or additional architecture is authorized by this plan.

Plan completion means this document is committed and ready for that handoff. It is not implementation completion, native qualification or permission to claim production restricted execution.
