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

The decisions in Sections 5–15 are the implementation baseline, not a menu of alternatives. Names below are expected internal types; ordinary private helpers may be added within the owning component. A changed trust boundary, authority, state transition or wire format requires review before implementation continues.

### 5.1 Values, handles and authority

Implement these immutable value contracts in the two `Gagamba.Execution/Restricted/` files identified in Section 4. Use sealed records, copied immutable arrays, explicit enums and checked integer quantities; never retain caller-owned collections. Serialization is diagnostic, not an admission API.

| Contract | Required fields and meaning |
|---|---|
| `RestrictedManifest` | Schema version 1, implementation ID `linux-m1-native-transaction`, authority, resources, closure, invocation and channels. Canonical binary encoding is versioned, length-prefixed UTF-8 with ordered fields; collection order is explicit. SHA-256 covers that encoding, not a serializer's incidental property order. |
| `AuthorityManifest` | Required namespace set; capability set empty; no_new_privs true; filter identity; read-only object destinations; writable-store inventory; no ambient environment, IPC endpoints or inherited target FDs beyond 0–2. Production no-IP qualification is separately Pending, never encoded as already established. |
| `ResourceManifest` | Exact Section 11 expected budgets with units and mandatory/qualification disposition per constraint; inode/object qualification explicitly PendingQualification. Actual readback results belong to a separate immutable establishment report and never mutate the prepared manifest. Absence, zero, unlimited and pending are distinct. |
| `ClosureIdentity` | Tool, input, coordinator, Bubblewrap and filter identities: SHA-256, length, build/source identity, object role and fixed destination. Helper dynamic dependencies are identified separately if present. No runtime discovery. Native handles are excluded. |
| `InvocationManifest` | Fixed target `/tool`, allowlisted fixture mode and fixed `/input` argument; bounded argument vector and environment; work directory `/tmp`; no shell command or arbitrary path. |
| `ChannelManifest` | stdin EOF; stdout/stderr limits and aggregate limit; no interactive, network, export or user-defined control channel. |
| `PreparationIdentity` | Execution UUID, issuer UUID, process-generation UUID, preparation UUID, manifest digest. This value is an identifier, not a release capability. |
| `ReleaseDisposition` | NotCommitted, Committed, MayHaveExecuted. A durable ReleaseIntent with lost owner observation is MayHaveExecuted even if no byte was actually sent. |
| `ExecDisposition` | NotAttempted, FailedBeforeExec(errno), Confirmed, Unknown. Never infer Confirmed from application stdout or Bubblewrap exit. |
| `RootResult` / `HelperResult` | Separate tagged Unknown, NotStarted, Exited(code), Signalled(signal, coreDumped); optional raw wait status retained. A wrapper's 128+signal convention is not a target signal result. |
| `ExecutionOutcome` | Unsupported, FailedToEstablish, CancelledBeforeRelease, Succeeded, TargetFailed, Stopped, Unknown. Stop cause is a separate first-accepted value plus a bounded set of later causes. |
| `CaptureResult` | Stored and observed-byte counters for each stream and total, overflow/read-failure/truncation flags, EOF/completeness and final-drain disposition. |
| `CleanupReport` | Confirmed, Failed or Unknown, with separate domain-empty, reaping, mount/staging-removal and journal-finalization results. Confirmed requires all required cleanup items. |
| `TransactionObservation` | Identities, states and the distinct fields above, requested/established manifests, evidence version and monotonic transition sequence. Published terminal observation is immutable. |

Use a sealed partial **non-record** `PreparedTransaction` in the M1 implementation with a private constructor, no public factory, no clone/deserialize route and no mutable public fields. `PreparationStore` is an internal nested class of `PreparedTransaction` (declared in the companion `PreparationStore.cs` partial-type file), so it can invoke that private constructor without a public/internal minting factory on the capability. The store owns each capability and indexes the exact object reference by preparation ID. It contains an immutable manifest snapshot, identity, monotonic expiry, `PreparedClosure` safe handles and a private consumption state. Lookup must validate reference equality, issuer, generation, digest, expiry and unused state. A matching GUID/digest in a fabricated object is insufficient. Only `PreparationStore` can mint it; only `ReleaseArbiter` can consume it. Disposal/abort invalidates it permanently. Safe handles remain alive through launch binding and cleanup, not through a caller's temporary scope.

The per-execution owner process is the issuer and holds the sole release-pipe writer. Across its private parent/owner channel, the caller receives `PreparedDescription` (identity + immutable manifest) and can return `Authorize(identity, digest)` once. That message is accepted only on the original inherited channel associated with that live owner generation; it resolves the owner's existing capability and cannot construct another one. There is no named socket, transferable serialized bearer token, persistence of release authority, reconnect/resume API or public provider registration. Channel EOF is caller loss and requests stop. The designated recovery scope has domain controls but never receives a release writer.

`DomainLease` exclusively owns safe handles for the domain directory, `cgroup.kill`, `cgroup.events`, required resource files and parent directory, plus the bound identities in Section 10. Duplicates held by the recovery scope are separately owned safe handles. Disposal alone does not imply termination. Only the owner and recovery scope may invoke domain stop; the target/coordinator receive none of these handles.

`IHeldLauncher` has only `PrepareHeldAsync(PreparedTransaction, DomainLease, LaunchChannels, deadline) -> HeldExecution` and `AbortAsync(HeldExecution)`. `HeldExecution` owns the helper reaper, trusted status reader, immutable establishment report and one internal release transport consumed by the arbiter. It cannot spawn another command. Release arbitration, resources, journals and results stay outside Bubblewrap argument construction. Observation cancellation cancels a wait only; explicit `RequestStop(cause)` affects execution.

Canonical encoding lives in `RestrictedManifest.cs` as an internal codec. Encode fields in the table order; use fixed-width little-endian integers, a one-byte presence/discriminant tag, u32 byte lengths/counts, strict UTF-8 without normalization, raw 32-byte digests and RFC-order UUID bytes. Preserve argument order; sort environment keys, object roles, mount destinations and set-valued names by ordinal UTF-8 bytes, rejecting duplicates. Do not hash native handles, live readbacks, timestamps or result records into the manifest. Pin enum/discriminant numbers and golden vectors in WP01; changing their meaning requires a schema version change. `TransactionState` has Preparing, Prepared, Owned, ReleaseCommitted, Running, Stopping, Completed, Failed, Cancelled and Unknown; `ExecutionOutcome` carries the finer result distinction. Pre-preparation rejection returns a typed refusal without allocating a releasable state.

### 5.2 Exact work-package-one boundary

WP01 creates value contracts, canonical encoding, the capability/store skeleton, a pure transition model and fake dependencies only. The pure model exposes `PublishPrepared`, `MarkOwned`, `TryCommitRelease`, `AcceptStop`, `ObserveExec`, `ObserveRoot`, `ObserveHelper`, `ObserveCaptureTerminal`, `ObserveDomainEmpty` and `PublishTerminal`. Invalid order returns a typed invariant failure without mutating state. All dependency clocks, journal and release transports are fake at this stage. Native object acquisition, a real owner, serialization across processes and any launch are forbidden in WP01. Section 16 specifies every file and the review gate; no architecture choice is left to that agent.

## 6. Execution state machine

The primary state is `Preparing → Prepared → Owned → ReleaseCommitted → Running → Completed`; stop/failure transitions lead through `Stopping` to a terminal Failed/Cancelled/Unknown observation. Root, wrapper, capture and cleanup observations progress independently. `Running` requires trusted exec confirmation; a fast process may supply exec and root confirmation in the same ordered status sequence. `Completed` requires a known root result, domain emptiness and terminal capture, not necessarily successful staging removal. A normal nonzero target exit is TargetFailed, not a restriction violation.

| Transition | Preconditions and required effects |
|---|---|
| Start → Preparing | Recovery scope registered, initial journal durable, setup/overall clocks armed. Allocate only under a pre-registered private parent. |
| Preparing → Prepared | Closure copied, protected and verified; handles and immutable manifests fixed; Prepared journal durable; capability atomically published. No partial preparation is visible. |
| Prepared → Owned | Atomic helper admission succeeded; coordinator reports HELD; independent drains and status/reaper are active; mandatory resource readbacks and namespace/mount/filter establishment accepted; external recovery has acknowledged live controls. Owned record durable. |
| Owned → ReleaseCommitted | Durable ReleaseIntent; final authorization/identity/stop/deadline checks under the single arbiter; capability consumed; one release frame delivery attempted. Section 8 defines the linearization point. |
| ReleaseCommitted → Running | Trusted coordinator establishes the exec condition in Section 8. An unconfirmed exec remains MayHaveExecuted while observed, not guessed Running. |
| Running → Completed | Sole target reaper supplies root status, whole domain becomes empty and capture terminates. Owner still finalizes cleanup separately. |
| Any nonterminal state → Stopping | Accepted explicit stop, caller loss, setup/overall expiry, overflow, reader/status failure, setup failure or recovery takeover. Invalidate capability, close release transport, request cgroup kill if allocated, then bounded drain/reap/cleanup. |
| Observation loss → Unknown | Apply Section 13 field by field. Stop/reconcile; do not replay or use cleanup to manufacture a result. |

The first accepted stop cause is stable. Subsequent causes may add bounded diagnostic flags but cannot authorize release or replace the original result. Concurrent observers share one terminal task; waiter cancellation leaves that task and the execution intact.

### Owner death at every major state

The following is M1's declared process-failure model: a live, unprivileged external recovery scope survives loss of the per-execution owner. It retains domain/preparation handles, a direct-child owner pidfd, a deadline and its own journal access. M1 does not claim automatic recovery after that scope and all handle holders also die.

| Last durable state at owner death | Recovery action and truthful result |
|---|---|
| AllocationIntent / Preparing | No release possible. Kill any allocated domain, remove only registered staging objects and incomplete allocations. Report setup failure and NotStarted if no ReleaseIntent exists. |
| Prepared | Invalidate preparation by losing the issuer generation; never recreate it. Reconcile as above. |
| HelperAdmitted / Held / Owned | Target still barred if ReleaseIntent is absent. Whole-domain stop removes helpers. Release channel EOF is not authorization. |
| ReleaseIntent, including before in-memory commit | MayHaveExecuted / Unknown unless already durable trusted result narrows a field. Kill and reconcile once; no resend. |
| ReleaseCommitted / Running | MayHaveExecuted; preserve any durable trusted root result, otherwise root Unknown. Kill whole domain, close/drain channels and reconcile. |
| RootObserved but descendants or writers remain | Preserve durable root status, mark incomplete execution/capture observations Unknown as appropriate; kill the domain and finalize capture as incomplete. |
| Cleanup in progress | Resume only idempotent identity-checked cleanup. Root/result fields never improve from absence of processes. |
| Terminal durable | Return the identical terminal record; repeated cleanup may append a separate reconciliation report, not rewrite execution history. |

Recovery authority loss while owner lives requests stop via its lifetime pipe. Caller loss also requests stop. If the owner is stalled rather than dead, recovery may stop the domain at its own deadline but cannot concurrently become the journal writer; it must first establish owner termination via its retained pidfd/direct-child wait. Simultaneous loss of owner and recovery scope is an explicit production gap; restart with records alone returns Unknown and quarantines unresolved resources. Bubblewrap death containment is defense in depth and is not credited as the sole recovery mechanism.

## 7. Preparation and immutable binding

1. Receive only an internal allowlisted fixture request and bounded regular-file input. Snapshot scalar values and copy collections immediately. Reject an unknown mode, runtime, path mapping, extra channel, export request or production profile request.
2. Register allocation intent before creating a private staging child. Use an already opened trusted parent directory and relative, single-component names; owner permissions 0700, no target bind of the parent. Record the child's identity before later allocation. The recovery scope retains the parent/child handles before the owner can publish preparation.
3. Open input/tool using `openat2` with `RESOLVE_BENEATH | RESOLVE_NO_SYMLINKS | RESOLVE_NO_MAGICLINKS` relative to approved roots and `O_NOFOLLOW | O_CLOEXEC`. Require ordinary files, bounded length and supported filesystem behavior. Refuse missing syscall support; no path-walk fallback. Copy from opened descriptors to new exclusive files, never hard-link/reflink or expose the caller's source. Hash destination bytes. The resulting copied snapshot is authoritative; concurrent source mutation cannot change the published copy. A claimed source digest, if supplied, must match the copy.
4. Enforce staging totals while copying: tool ≤4 MiB, input ≤256 KiB, all staged content including coordinator/filter ≤16 MiB and ≤128 objects; stop before the first excess allocation/write. Bound arguments/environment/config to 16 KiB in total. Reject sparse/oversized objects instead of allocating based on attacker-controlled lengths. All names are fixed roles, not paths supplied by the input.
5. Close every writable copy descriptor, set tool/coordinator mode 0500 and input/filter 0400, and retain read-only object descriptors plus directory identity. Do not rely on modes or hashes alone: staged parent is inaccessible to the target, host writes are restricted to the trusted owner/recovery scope, and mounts bind those exact descriptors. There is no cache or later replacement/reopen by pathname. Compromise of the trusted host owner is outside the ADR's threat model.
6. Validate fixture/coordinator build identity and helper/filter pins. Hash the exact opened Bubblewrap executable and prohibit setuid/setgid/file capabilities. Pin any dynamic helper dependencies in a protected installation inventory and refuse writable/unidentified dependencies. Spawn the helper with a fixed clean environment, with no LD_PRELOAD/LD_LIBRARY_PATH or caller loader variables; clearing the later sandbox environment alone is insufficient. The trusted installation is held unchanged for the transaction; an observed replacement invalidates preparation. Unknown dependencies refuse. The static target has no dynamic-loader closure. Record source and artifact hashes separately.
7. Canonicalize and hash the completed manifest, bind prepared configuration to it, persist Prepared, then insert the private capability atomically into `PreparationStore`. Failed or cancelled preparation publishes no capability. Partial allocations remain journaled for cleanup.
8. Before release, compare held mounted-object identities with the retained copied-object identities and manifest; verify no writer remains and all expected mounts are read-only. Changing the source name, replacing a staging name or presenting a different preparation cannot switch the bound FD. Replacement of a protected staging entry is also an integrity failure: refuse, even if the already-open original is still safe. Re-authorization requires a new preparation, never mutation of the current one.

Use sealed `memfd` objects for the immutable coordinator configuration and seccomp bytes (`F_SEAL_WRITE | F_SEAL_GROW | F_SEAL_SHRINK | F_SEAL_SEAL`); verify seals and rewind before helper admission. These do not make the copied executable/input immutable by themselves. Copied-object protection depends on inaccessible staging plus retained descriptors and verified read-only mount binding. Native evidence must exercise replacement between copy, publication, bind and release. Section 19 retains full hostile-filesystem qualification as a production gap.

## 8. Pre-initialization release mechanism

### 8.1 Selected coordinator and wire contract

Build a new small static `exec-coordinator.c`, separate from the AR-7 R2 gate. Bubblewrap execs this trusted binary inside the completed sandbox. It parses only immutable owner configuration, sets and reads back inherited rlimits, sets `PR_SET_DUMPABLE=0`, closes unneeded descriptors and emits HELD. It does not open `/input`, load target code, run target constructors, fork the target or invoke its hooks before a valid release. Do not use a barrier in target main, SIGSTOP after exec, an inherited environment switch or Bubblewrap `--block-fd` for release.

Wire version 1 uses a 96-byte header: magic `GGM1CTL\0` (8 bytes), version u16, kind u16, total-length u32, sequence u64, execution UUID bytes[16], preparation UUID bytes[16], manifest SHA-256 bytes[32], reserved zero bytes[8]. Integers are little endian; UUIDs are canonical RFC byte order. Each frame is at most 4096 bytes. Header lengths, identities, sequences, reserved bytes and kind-specific payload lengths must match before action. Configuration uses the canonical manifest plus a bounded fixed launch configuration in the sealed memfd; total ≤16 KiB. Protocol constants/layout live in `protocol.h` and mirrored managed codec tests, with golden byte vectors.

The sole owner→coordinator RELEASE frame is exactly 128 bytes: header plus generation UUID bytes[16], overall deadline in CLOCK_MONOTONIC nanoseconds u64 and reserved zero bytes[8]. There is exactly one release attempt. The anonymous pipe is created with CLOEXEC, only its owner writer is nonblocking, and its atomic-write capacity must be ≥128 bytes. There is no retry queue or second release. Invalid/partial frame, premature EOF, identity mismatch, expiry, unexpected extra control input or poll/read error causes coordinator failure without target fork. After accepting RELEASE the coordinator closes its release reader permanently. It also checks the absolute overall deadline locally before fork/exec; this supplements the owner's authoritative final check.

Use independent sequence counters starting at 1 for each direction; RELEASE is the sole owner control kind. Kind IDs are HELD=1, RELEASE=2, EXEC_FAILED=3, EXEC_CONFIRMED=4, ROOT_WAIT=5 and PROTOCOL_FAILED=6. EXEC_FAILED and ROOT_WAIT payloads are one little-endian u32 (errno/raw wait); EXEC_CONFIRMED has no payload; PROTOCOL_FAILED is one u32 closed reason code. HELD is a bounded versioned encoding of the Section 11 establishment report; refuse if it cannot fit one 4096-byte frame. The immutable configuration binds issuer/generation and both absolute deadlines in addition to the manifest digest.

Coordinator→owner frames are HELD (bounded establishment payload), EXEC_FAILED (errno), EXEC_CONFIRMED, ROOT_WAIT (raw wait status), and PROTOCOL_FAILED (bounded reason enum). The valid status sequence is HELD, then either EXEC_FAILED followed by ROOT_WAIT, or optional EXEC_CONFIRMED followed by ROOT_WAIT; PROTOCOL_FAILED terminates the sequence at any stage. ROOT_WAIT without confirmation cannot establish a target result except through the reviewed normal-exit confirmation rule, in which case EXEC_CONFIRMED must precede it. The owner validates exact order and permits one root result. EOF before the expected terminal status is observation loss. No status comes from stdout/stderr. No caller-controlled strings are interpolated into the protocol. A full status pipe or failed write causes bounded coordinator failure; it must never block indefinitely while keeping the target alive.

After RELEASE, the coordinator forks one child. That child closes the coordinator status writer and every setup/control FD, retaining only 0–2 and a CLOEXEC exec-error pipe writer, applies the final fixed invocation and calls `execve("/tool", fixed_argv, fixed_env)`. On failure it writes a fixed errno record then exits with a reserved coordinator failure code; the parent distinguishes that record from target status. The parent stays non-dumpable, is the sole reaper of this direct child and reports its raw wait status. It never sends target-controlled bytes as trusted status. The fixed protocol and filter must prevent target acquisition of coordinator controls via ptrace, proc-fd access, process_vm operations or inherited descriptors; a failing negative test blocks integration.

**Exec confirmation rule:** CLOEXEC error-pipe EOF alone is insufficient because the child might have died before exec. For a live child the coordinator can confirm `/proc/<direct-child>/exe` against its retained `/tool` identity while the direct child is unreaped, using an opened object and fstat comparison; this PID is a non-reused direct-child observation during sole-reaper ownership, not kill/recovery authority. If this cannot be observed, defer confirmation. At reaping, a normal child exit plus complete error-pipe EOF and no exec-error record confirms exec only because the reviewed child path has no normal-exit path except a successfully reported exec failure. Any inability to deliver that failure record must terminate the child by signal, never by a normal exit. A signalled child without prior executable-object confirmation remains exec Unknown. A ROOT_WAIT without exec confirmation is retained as child diagnostic status and cannot become a known target root result. This intentionally sacrifices precision in ambiguous races. There is no fabricated early Running state.

Coordinator exit closes the namespace's remaining wrapper path; Bubblewrap's default PID-namespace init remains in use (no `--as-pid-1`). Descendant lifetime after root exit is observed, not promised: wrappers may eliminate descendants, or the owner keeps the domain owned until it empties or the overall deadline requests stop. Neither branch substitutes for cgroup emptiness.

### 8.2 Release versus stop linearization

`ReleaseArbiter` owns one short synchronous gate shared by release and all accepted stop requests. Never await journal I/O or native setup while holding it.

1. While still held, validate all Owned prerequisites and authorization against the live capability. Write and durably flush ReleaseIntent outside the gate. Stop is allowed to win during that write. A failed flush requests stop and forbids release.
2. Enter the gate; reread the monotonic clock, setup/overall expiry, stop flag, caller/recovery liveness flags, immutable identity, prerequisite completion and capability state. Revalidate after the flush, not from an earlier cached boolean. Expiry is evaluated at this point even if the timer callback is delayed.
3. If any check fails, consume/abort the capability without sending release. Otherwise change Owned→ReleaseCommitted and mark the capability consumed **inside this gate**. That state change is the linearization point. Attempt one nonblocking atomic RELEASE write before leaving the gate. Full delivery is success; EAGAIN, EPIPE, partial or unexpected return causes stop and a conservative delivery-failure disposition. No retry. Ignore SIGPIPE as a process-termination mechanism; handle its error explicitly.
4. A stop accepted in the gate before that state change forbids every future release. A stop accepted after it closes the release writer and requests whole-domain stop, even if the child has not yet forked or execed. It does not revoke already-permitted effects or change history to CancelledBeforeRelease.
5. Persist ReleaseDelivered/exec/root observations as they arrive. Those records improve knowledge but are not prerequisites that could delay or reopen the single release write. A crash after durable ReleaseIntent is MayHaveExecuted until independent trusted evidence proves more.

Known no-delivery failures can retain live-owner NotStarted evidence; after owner loss the conservative durable ReleaseIntent rule wins. Duplicate calls return the original release decision without another write. The recovery process never calls release. A target deadline check immediately before exec is defense in depth, not a substitute for the gate.

## 9. Bubblewrap adapter

The adapter pins Bubblewrap 0.11.1 SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` from ADR 0003. The filter is the exact `filter_bytes()` sequence in `spikes/Ar7Feasibility/p07.py` at sealed commit `cb511cd18ba25daeda76cf41a400d1136c6ba0f0`; extract that data into the new internal artifact and record/verify its SHA-256 without importing or running the probe harness. Both are verified from their actual opened objects. The [pinned upstream implementation](https://github.com/containers/bubblewrap/blob/v0.11.1/bubblewrap.c) supports `--ro-bind-fd`, consumes setup/filter descriptors, and leaves application descriptors for its command while its PID-namespace init closes unrelated FDs. Its `--block-fd` read does not require a successfully read authorization byte. Consequently M1 uses explicit inherited descriptors and the coordinator protocol; it must not invent a `--preserve-fds` option. These source observations require implementation-time native validation, not an AR-7 rerun.

### 9.1 Fixed descriptor map

Use an adapter-specific atomic-cgroup `posix_spawn` path based on the existing glibc primitive. Refuse an unsupported libc/kernel combination; do not migrate an already executing process into the cgroup. Reserve destination FDs below 12; first move all source descriptors to unique temporary numbers ≥64 so dup ordering cannot overwrite a source. Add file actions for all destinations and closefrom(12) after those dups. Parent copies of child endpoints close immediately after successful spawn; unwind all copies on failure. FD_CLOEXEC and ownership of each endpoint are checked, not assumed.

| Child FD | Role in Bubblewrap launch | Coordinator / target fate |
|---|---|---|
| 0 | EOF-only read pipe, no writer retained anywhere | Coordinator and target stdin; never ambient terminal. |
| 1 | stdout pipe writer | Target and required scaffolding share it; external reader owns capture. |
| 2 | stderr pipe writer | Same; helper diagnostics consume the same declared bound. |
| 3 | Sealed immutable coordinator configuration | Read/validate before HELD; close before target fork. |
| 4 | RELEASE pipe reader | Coordinator only; close on release/abort; never target. |
| 5 | Trusted coordinator status writer | Coordinator parent only; explicitly close in target child. |
| 6 | Sealed seccomp program | Pass `--seccomp 6`; Bubblewrap consumes it. |
| 7 | Copied static tool read-only FD | `--ro-bind-fd 7 /tool`; consumed by setup. |
| 8 | Copied input read-only FD | `--ro-bind-fd 8 /input`; consumed by setup. |
| 9 | Copied coordinator read-only FD | `--ro-bind-fd 9 /coordinator`; consumed by setup. |
| 10 | Bubblewrap JSON status writer | `--json-status-fd 10`; independent bounded helper-status reader. Never interpreted as target status. |
| 11 | Verified Bubblewrap executable FD | Spawn executable `/proc/self/fd/11`, binding launch against path replacement. Close any inherited copy in coordinator setup. |

No domain, staging-directory, journal, caller, recovery, owner release-writer or output-reader FD is in this map. The coordinator audits and closes every FD ≥6 that survived helper setup before HELD; retains an opened `/tool` identity only for trusted exec observation and closes it in the child. Its later error pipe must be CLOEXEC. Capture and control pipes are independent. Duplicate writer leaks anywhere, including harness/recovery, fail the FD-leak tests. The recovery scope does not retain an output writer merely to aid diagnostics.

The adapter builds an argv array without a shell: unshare user/mount/PID/network/IPC/UTS namespaces, drop all capabilities, establish no_new_privs and seccomp, clear environment, set fixed work directory, create only the tree in Section 11, bind the three objects, and run `/coordinator`. Use `--die-with-parent` as defense in depth, keep the normal PID-namespace init, and do not use `--block-fd`. Kernel cgroup placement, lifecycle and release stay outside argument construction. Current host UID mapping is an installation detail; UID 65534 is not a contract.

Exact argument order is emitted by `BubblewrapArguments.cs` and verified against a single checked-in golden vector for the Section 11 layout, including nonrecursive root remount-readonly after submount creation. If a pinned helper cannot create that exact topology or transport the fixed descriptors, refuse and stop that work package for review. Do not add a generic builder, privileged mount helper or alternate launcher.

### 9.2 Separate observations

The owner's sole `waitpid`/pidfd-associated reaper observes its direct child Bubblewrap and populates HelperResult. Bubblewrap JSON status supplies helper setup/namespace diagnostics only. Coordinator status supplies trusted establishment and target observations. EOF on one channel cannot complete another. A root result followed by wrapper failure keeps the root result and records the wrapper failure separately; wrapper success without trusted root status leaves root Unknown. Bound helper-status storage to 16 KiB total and 4096 bytes per record; overflow or malformed status requests stop. No unbounded diagnostic log is enabled in the execution path.

## 10. Lifecycle ownership and cgroup model

### 10.1 Selected M1 owner topology

A dedicated M1 harness host process contains the internal `M1RecoveryScope` and outlives the separate per-execution owner and request client. Before creating either child, it sets and verifies PR_SET_CHILD_SUBREAPER; no unrelated application children run in this host. This lets it reap adopted execution descendants after owner death under the [Linux child-subreaper semantics](https://www.man7.org/linux/man-pages/man2/PR_SET_CHILD_SUBREAPER.2const.html). The ordinary live owner remains the sole wrapper reaper; after owner death, the scope becomes the sole reaper for adopted children. It receives a duplicate wrapper pidfd before Owned for observation/reaping, never as a substitute for whole-domain stop. Death before that transfer is handled by domain stop plus reaping adopted children of this dedicated host; no journal PID is used as authority. A request-client lifetime pipe is distinct from the recovery-scope lifetime pipe. This allows caller loss to be tested without killing recovery. No installed service is introduced. Before starting an owner, the scope obtains existing operator delegation, durably records `AllocationIntent` under the journal fence, and retains the delegated parent and recovery envelope. It releases that writer before spawning the per-execution owner, immediately retains the owner pidfd, and sends the owner a bounded request plus duplicated delegated-parent and staging-parent descriptors. The owner acquires the exclusive transaction writer and proves the exact acknowledged intent, then creates the fresh random domain and performs preparation. It transfers an independently owned recovery handle set for that exact domain to the scope; the scope validates, retains and acknowledges it before the owner advances to registration. The scope remains the external subreaper/recovery actor and may take the journal writer only after pidfd-proven owner exit. The owner alone owns the primary lease and issuer-local `PreparationStore` capability. This is the approved G2-03 owner-owned topology clarification; no WP04 release path is implemented here. The scope retains a monotonic watchdog for the direct-child owner. Missing pidfd/delegation/subreaper support or bootstrap handle transfer refuses before helper admission.

The owner remains outside the target leaf. Bubblewrap, its setup/init processes, the coordinator, target and descendants are admitted to that one leaf at creation via the existing atomic cgroup spawn mechanism. The owner/recovery are never in the target memory/tasks budget. The target cannot see delegated controls or create/migrate to another cgroup. No nested target-created subgroups are allowed. There is one M1 transaction at a time per scope; no scheduling framework.

Before release the scope must acknowledge it holds the current domain handles and preparation cleanup handles. Anonymous inherited local pipes carry bounded bootstrap/acknowledgement and liveness, not a network service. Where handles must cross after owner creation, use one inherited Unix socketpair solely for `SCM_RIGHTS` transfer, with a fixed role/count allowlist and CLOEXEC-on-receive. It is never bound to a filesystem address or inherited by Bubblewrap. Bootstrap transfer includes recovery evidence identity, never release capability. A missing/invalid acknowledgement blocks Owned. The scope applies a setup-inclusive independent deadline and kills via its duplicate cgroup handle if the owner is lost or stops responding; external test-harness rescue beyond this scope is recorded as a failed recovery case, not a passing mechanism.

### 10.2 Stable domain identity

Identity is `{execution UUID, host boot ID, cgroup2 mount identity, delegated-parent identity, freshly created child name, child dev/inode, lease-generation UUID}` plus the **live handles** opened at creation. Mount identity comes from the actual cgroup2 mount (`statx` mount ID and filesystem type), not a supplied string. Parent and child handles use no-follow, beneath-only relative resolution. Pre-open `cgroup.kill`, `cgroup.events`, `memory.max`, `memory.swap.max`, `pids.max` and required readback files. Match every opened control's directory membership before acknowledging the lease.

Domain termination writes `1` through the retained `cgroup.kill` FD and polls `cgroup.events` for populated=0 through its retained FD, with bounded retry/deadline. Never issue kill to a PID from the journal, enumerate PIDs as stop authority, or re-resolve a path in order to kill a replacement domain. Owner termination/reaping may use its retained pidfd/direct-child relationship; that does not change target-domain authority.

Before directory removal, compare the entry under the retained parent to the retained child identity; mismatch means refuse removal and report Unknown. If a control write/read loses its object or returns ambiguous state, do not reopen by name and continue on a potentially new object. Mark cleanup Failed/Unknown and preserve the record. A fresh process with only boot ID, dev/inode and a name has **no recovered authority**: those values can be reused. M1 restart-only reconciliation reports Unknown/quarantine and requires operator handling. Same-boot recovery is supported only while the registered external scope retains the original handles. Cross-boot records cannot authorize any action on current objects.

## 11. Resource installation and verification

These are exact **internal fixture-envelope** values, not public defaults or claims about minimum usable budgets:

| Resource | M1 installation and release readback |
|---|---|
| Memory | `memory.max=134217728` (128 MiB); read same value before helper spawn and again before release. |
| Swap | `memory.swap.max=0`; require file/controller and exact readback. |
| Tasks | `pids.max=48`; read back exactly. Includes helper/coordinator/target threads and processes, not an independent thread/process quota. |
| File descriptors | Coordinator installs RLIMIT_NOFILE soft=hard=64 and reports getrlimit; target inherits it. Not a domain-wide FD quota. |
| Core dumps | RLIMIT_CORE soft=hard=0 for owner and coordinator, read back; fixture has no runtime dump facility. Record host core_pattern. A piped/external core sink without an established disabled configuration refuses the M1 fault run; never alter host configuration automatically. Ordinary-file core suppression must be checked in finite native fault cases. |
| Writable filesystem bytes | `/tmp` private tmpfs 7 MiB (7340032), `/dev/shm` private tmpfs 1 MiB (1048576), aggregate 8 MiB. Both explicitly sized; no third writable file store. |
| Inodes/metadata | PendingQualification; no independent production inode/object-limit claim. M1 uses only finite fixture modes and bounded test work. Memory and test caps are not substitutes for the later required bound. |
| stdout/stderr | 65536 bytes each and 131072 combined; enforcement and retained storage per Section 12. |
| Stdin | EOF, no retained writer. |
| Setup / total duration | Setup 10 s, overall 30 s from request acceptance, measured with CLOCK_MONOTONIC; both include preparation and helper setup. |
| Stop / final drain | Stop confirmation allowance 5 s after first stop; final pipe drain at most 1 s after domain-empty or stop allowance exhaustion, whichever comes first. These do not authorize extra target execution. |
| Owner allocation/logs | Staging 16 MiB/128 objects; config 16 KiB; coordinator/helper status 16 KiB each; journal 64 KiB live snapshot plus 64 KiB replacement; receipt 32 KiB; fixed capture/drain buffers. Refuse overlarge records. |

The root tree is built by Bubblewrap, then nonrecursively remounted read-only so its two child tmpfs mounts remain writable. `/tool`, `/input`, `/coordinator` are read-only FD binds. `/proc` is a new PID-namespace proc mount with read-only mount policy; failure to establish that policy refuses. `/dev` is a read-only empty directory except its private `/dev/shm` child: **no `--dev`, device nodes, devpts or inherited terminal** are needed by the static M1 fixture. `/run`, `/sys` and `/home` are empty read-only directories. No host home, runtime tree, service socket, cgroup filesystem, host `/tmp` or ambient resolver configuration is mounted. Set only `HOME=/tmp`, `TMPDIR=/tmp`, `LANG=C`; no PATH search. Invocations use absolute destinations.

Record the complete mount inventory from trusted coordinator setup and corroborate it with the launched namespace identity and fixed helper configuration. For each expected mount compare mount ID, parent relation, filesystem type, read/write/noexec policy where applicable, backing identity and observed tmpfs capacity. Reject unexpected writable mounts, aliases or unsupported readback. Read tmpfs capacity with statfs and parse the established size option; require the expected page-aligned ceilings, allowing no implicit default mount size. Anonymous memory, SysV/POSIX shared-memory usage within the private IPC namespace, pipe buffers, and pseudo-filesystem effects remain accounted by the memory/tasks limits and explicit authority inventory; they are not falsely reported as regular-file scratch or as independently metadata-qualified. The coordinator needs executable `/tool`/`/coordinator`; scratch is nodev/nosuid. M1 does not require or claim scratch noexec: the pinned helper tmpfs operation does not establish that flag. Record the actual executable-mount policy in the manifest; do not invent an unsupported option or add a mount implementation to obtain it. An inability to establish the selected required flags stops the package rather than silently dropping them.

`ResourceInstaller` performs cgroup writes/readbacks before spawning any helper. `EstablishmentVerifier` combines that external readback with HELD's trusted in-namespace namespace IDs, mount inventory, CapEff/CapPrm/CapAmb=0, no_new_privs=1, seccomp mode, rlimits and object identities. Installed seccomp mode plus the pinned program/config binding is establishment evidence, not proof of complete syscall-policy coverage. HELD is accepted only from the new source-bound coordinator before target code exists. Final release rechecks cgroup values, immutable prerequisite identity and both deadlines. A configured value without a required readback is FailedToEstablish. Inode qualification alone is an explicit allowed Pending for the internal fixture, never an exception to a production request.

Deadlines are armed at request acceptance in both owner and recovery scope, **before copying or synchronous native setup**. An async timeout around a blocking method is insufficient. Every copy loop, handshake and status wait observes the absolute deadline; a bounded worker may continue unwinding but cannot later publish/release. The independent recovery watchdog stops the domain if the owner is blocked. After ReleaseCommitted, the setup deadline ceases to be a runtime limit; release must have committed before it expires; overall remains active until the domain is empty. One timestamp origin is transferred in CLOCK_MONOTONIC nanoseconds; no wall-clock expiry or comparison of unconverted .NET Stopwatch ticks to native nanoseconds.

## 12. Bounded stdout/stderr enforcement

The owner starts both asynchronous drains before admitting Bubblewrap and keeps them alive independently of callers and observers. Use a fixed ≤4096-byte buffer per stream and retained storage capped at the declared stream/aggregate budgets. Serialize aggregate reservation with checked/saturating counters; retain only the permitted prefix. At the first byte beyond any limit, set overflow/truncation and immediately invoke `ReleaseArbiter.RequestStop(OutputOverflow)` without awaiting observers, log writes, root exit or the other reader. A reader error similarly invokes `RequestStop(OutputReadFailure)` immediately. There is no target-supplied overflow signal.

Output that arrives during helper/held setup is charged to the same budgets and can prevent release. An absent subscriber never blocks enforcement. Slow consumers can read a bounded snapshot after completion; no unbounded queue, per-line string accumulation or observer callback in the drain loop. After overflow the drains may discard into fixed buffers until stop/final-drain deadline to avoid a blocked writer preventing teardown. All pipe endpoints have one documented owner, and the target can inherit only its writer endpoints.

Root exit does not complete capture or domain lifetime. Descendants can retain writers; continue under the overall deadline and stop policy. At final-drain expiry close readers, mark capture incomplete and preserve any root result. EOF is stream completion, not proof of domain emptiness. If the owner dies, M1's recovery scope stops the domain but does not reconstruct lost output; capture is incomplete/Unknown unless an already durable terminal capture record exists. A successful target exit plus output overflow is not overall Succeeded.

## 13. Failure, cancellation and Unknown-result semantics

Apply this decision table without collapsing fields:

| Evidence/condition | Outcome, root and release disposition |
|---|---|
| Unsupported platform/ABI/helper/delegation/prerequisite before release | Unsupported or FailedToEstablish with structured reason; root NotStarted and NotCommitted if release was provably impossible. |
| Caller stop, caller loss or expiry accepted before release linearization | CancelledBeforeRelease for cancellation/loss, FailedToEstablish for setup expiry; root NotStarted, capability permanently aborted. |
| Trusted EXEC_FAILED errno, complete protocol, no confirmed target exec | FailedToEstablish, Exec FailedBeforeExec, target root NotStarted; child/wrapper exit remains separate. |
| Confirmed exec and trusted normal root exit; domain empty and complete capture | Succeeded for exit 0 or TargetFailed for other exits, subject to separately reported stop/enforcement failures. |
| Confirmed exec and trusted signalled root result | Known Signalled root; Stopped if an accepted stop caused teardown, otherwise TargetFailed with actual evidence. Do not invent causality from signal alone. |
| Stop accepted after commit | Stopped, or Unknown where root/execution observation was lost; release remains Committed/MayHaveExecuted. Preserve known root status. |
| Owner dies after durable ReleaseIntent and before a durable trusted result | MayHaveExecuted; exec/root/execution fields Unknown as appropriate, irrespective of later domain emptiness. |
| RELEASE delivery uncertain or status channel lost/malformed/out of order after commit | Unknown execution/result fields not otherwise proven; request stop. A failed send is never automatically replayable. |
| Child is signalled before trusted exec confirmation, with empty error pipe | Exec Unknown and target root Unknown; retain child raw wait only as diagnostic evidence. |
| Wrapper exits 0, coordinator vanishes, no trusted target status | Helper Exited(0); target root Unknown. |
| Known target result followed by lost helper/domain/capture observations | Preserve root; mark only missing observations Unknown; aggregate execution cannot claim successful completion. |
| Output read error/final-drain cutoff with known target result | Preserve root; capture failed/incomplete, outcome not Succeeded. Lack of complete output alone does not erase known exec/root. |
| Kill returns success without observed populated=0, stale domain identity, lost control handle or reconciliation from records alone | Cleanup Unknown (or Failed for a definite removal error); root stays whatever was known. |
| Domain empty and all cleanup complete, but root status lost | Cleanup Confirmed; root and execution remain Unknown. |

`Unknown` means insufficient trusted observation, not any nonzero exit or any cleanup failure. It is required by interrupted release, unresolved exec race, missing/corrupt trusted result, owner loss without durable result, or loss of the relevant lifetime observation. A known root result may coexist with execution/capture/cleanup Unknown. Known root plus a definite staging deletion error gives cleanup Failed, not root Unknown. Never map Unknown to exit code 0, -1 or 137. Store result variants explicitly.

External cancellation is an explicit stop request; cancellation of a caller's `ObserveAsync` await is observation-only. Caller-channel EOF is a separate lifecycle signal and does stop. If a request is cancelled during a blocking preparation step, the eventual worker result is discarded and its allocations reconciled; it cannot publish a late capability. No retry of a whole transaction whose release may have happened; a new explicitly requested transaction is a distinct decision and identity.

## 14. Cleanup and reconciliation

### 14.1 Journal ordering and durability

Use one bounded versioned snapshot per execution, written in the protected recovery directory through exclusive temporary create, complete write, file fsync, atomic rename and directory fsync. Include a sequence number and checksum; retain the last complete version until replacement durability succeeds. Required durability failures prevent release. The snapshot holds allocated-object inventory and monotonic facts, not executable instructions or release credentials. Configuration/manifest digests and bounded trusted result frames are included; output payloads and secrets are excluded. No unbounded append-only transcript.

| Durable record / acknowledgement | Must precede |
|---|---|
| AllocationIntent with execution ID, boot/parent identity, fixed random child names and cleanup intent | Creating staging/domain under those retained parents. If death occurs between intent and registration, only the surviving allocator/recovery scope can resolve the named child under its controlled parent; there is no arbitrary-path sweep. |
| AllocationRegistered with actual object identities and retained recovery handles acknowledged | Any process admission or preparation publication using them. A creation-to-registration crash is reconciled under the exclusive pre-registered parent and never grants release authority. |
| OwnerAccepted and recovery-scope lease acknowledgement | Transfer of exclusive normal journal writing from scope to owner; owner preparation/launch. |
| Prepared snapshot | Publishing the preparation capability/description. |
| HelperAdmissionIntent | Atomic spawn; an admitted process with missing post-spawn record is still contained by the already registered cgroup. |
| HelperAdmitted / Held establishment / Owned | ReleaseIntent; include actual readbacks and scope liveness acknowledgement. |
| ReleaseIntent with preparation/manifest identity and absolute deadlines | Entering the final release gate or attempting the release byte. |
| ReleaseDelivered / ExecObserved / RootObserved / CaptureTerminal | Terminal publication that claims those observations; fsync results before treating them as recoverable. Owner memory can expose provisional observations explicitly, never durable ones. |
| StopRequested / CleanupProgress | Record before destructive cleanup when possible. Journal failure never prevents an urgent kill through an existing live lease; report that persistence failed. |
| Terminal with result + cleanup status | Stable terminal publication to observers. If final persistence fails, retain known live results but mark durable recovery evidence unavailable; recovery must not pretend it read a terminal result. |

Parent directories are private, registered, and bounded; the scope never allows concurrent allocation into the same execution parent. The file system must support the stated durability operations or admission refuses. Checksums detect torn/malformed records, not authority forgery. Protect records by host ownership and access policy. One writer exists at a time: the scope writes allocation/bootstrap, then owner; recovery takeover requires stable proof the owner exited and exclusive journal lock acquisition. If ownership cannot be fenced, the scope may kill using its live domain lease but cannot concurrently rewrite the owner's result record.

### 14.2 Cleanup order

Invalidate capability and close release first. Request whole-domain stop when required. Observe empty through retained events handle; reap the direct wrapper once and consume available trusted status. Bound final drain, dispose readers/status endpoints, and record incomplete observations. Only after domain emptiness and necessary reaping release namespace-associated bindings and remove owned staging/scratch resources by retained-parent-relative identity-checked operations. Do not recursively follow symlinks or traverse arbitrary paths. Private tmpfs state dies with the namespace; its disappearance must be corroborated by namespace/domain lifetime, not claimed because a mount command once succeeded. Remove the matching empty cgroup directory last, then finalize the journal/receipt. A cleanup error does not prevent attempts at other independently safe cleanup items.

Reconciliation is idempotent and never resumes target work: acquire fenced ownership, validate boot and original live lease, read latest complete snapshot, kill the registered domain, observe emptiness/reap where authority exists, and clean only verified allocations. Repeated calls return the same result facts plus new cleanup observations. Identity mismatch, damaged journal or absent original handles means quarantine/Unknown rather than adopting an apparently matching directory. Preserve unresolved records for operator action; do not delete them to produce a clean run. After a terminal Confirmed cleanup retain the bounded semantic receipt and retire private recovery state according to the test run's retention policy.

## 15. Production evidence model

The following is a production-designed schema exercised internally by M1; its existence is not production qualification.

| Evidence channel | Contents and authority | Retention/exposure |
|---|---|---|
| Semantic transaction receipt | Execution/preparation IDs, source/package/helper/filter/coordinator/closure hashes, requested and established manifests, qualification flags, release/exec disposition, first stop cause, separate root/helper/domain/capture/cleanup observations and structured refusal/failure reasons. Derived only from trusted establishment/lifecycle observers. | Bounded ≤32 KiB; immutable terminal record. Opaque domain ID only; no native handles, raw private paths or release credentials. |
| Protected recovery snapshot | Boot/mount/parent/child identity, lease generation, allocation inventory, durable state/sequence, deadline and bounded trusted result observations. | Private ≤64 KiB snapshot, one bounded replacement; usable only with surviving authority under Section 10. Not a public result or serialized capability. |
| Qualification record | Build command/toolchain, exact source commit, binary/filter hashes, host/kernel/libc/WSL/delegation/LSM facts or explicit unknowns, test names, failure barriers, timestamps, external observations, negative controls and cleanup. | Durable reviewed repository evidence with selected compact records; large logs/binaries in ignored artifacts. Never consumed as runtime authorization. |

Application stdout, constructor markers, nonce files, traced syscalls, harness PID lists, root observers, helper process counts and probe leaf names are qualification diagnostics. They may corroborate a test, but cannot mint a preparation, establish runtime release success or replace trusted target status/domain emptiness. A source-bound HELD/status frame from the new trusted coordinator is semantic evidence; a similar string printed by the fixture is not. Keep configured, observed, inferred and unqualified facts distinct. Do not record secrets, raw input contents or unlimited output in receipts/journals. Native tests must say Executed/Passed/Failed/Skipped/Unavailable explicitly; a skip cannot satisfy a native completion gate.

## 16. Ordered implementation work packages

Execute WP01–WP12 in order. Each package is one bounded change and evidence checkpoint; no parallel implementation is required. All paths are repository-relative. Names below are the exact expected entry files; colocated private helpers are allowed only for the same responsibility and must be listed in the package evidence. Do not create empty scaffolding for later packages. Shared evidence locations are `docs/evidence/gagamba-linux-m1.md`, `docs/evidence/linux-m1/wpNN.md` and ignored `artifacts/linux-m1/wpNN/`; every package records its exact commit, commands, result summary and limitations. Independent review means a reviewer other than the implementation author, recorded against that exact revision. If unavailable, stop at that gate; self-review does not satisfy it.

Native target release is disabled in WP01–WP09. WP04 may execute trusted Bubblewrap/coordinator setup only. WP05 tests the actual RELEASE codec against a coordinator test build whose post-release branch records acceptance and exits before any target fork; such a binary is qualification-only and cannot pass integrated artifact pinning. WP10 enables the fixed fixture only after G3. There is no development flag that lets arbitrary commands bypass prerequisites.

### WP01 — Internal contracts, capability identity and state model

- **Objective:** Encode the exact vocabulary and legal transitions before native mechanisms exist.
- **Expected projects/files/components:** `src/Gagamba.Execution/Restricted/RestrictedManifest.cs`, `RestrictedTransaction.cs`; new non-packable `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`; `Preparation/PreparedTransaction.cs`, `PreparationStore.cs`; `Execution/TransactionStateModel.cs`; `tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj`, `Contracts/ManifestTests.cs`, `PreparationIdentityTests.cs`, `StateModelTests.cs`. Add only necessary project references, solution entries and specifically named InternalsVisibleTo entries. Match the existing .NET 10/xUnit v3 build conventions.
- **Dependencies:** Accepted Sections 1–4, ADR 0003 and Sections 5–6 of this plan. No native availability required.
- **Contracts/invariants introduced:** Every Section 5 field/enum and canonical digest; I02/I05/I06/I09; single-use reference identity; stable terminal observation; separate root/helper/cleanup; no native handles in semantic records.
- **Implementation boundaries:** Implement immutable snapshots, canonical encoding, store skeleton and pure state transitions with injected fake clock/journal/transport. No filesystem copy, P/Invoke, process launch, real release transport or provider registration.
- **Unit tests:** Mutation of original arrays cannot alter a digest; canonical golden bytes; missing vs unlimited vs Pending; foreign issuer/generation, fabricated identifier, duplicate consume, aborted capability and expiry; complete transition table; known-root/unknown-cleanup combinations; observer cancellation never stops the fake execution.
- **Native/integration/fault tests:** None required or claimed. Build the new projects and affected existing contract tests; fault injection is purely fake time/transition ordering. Record native as NotApplicable for WP01, not Passed.
- **Evidence retained:** Type/field mapping to Section 5; canonical test vectors; transition test results; API/project-reference diff; exact source revision and build commands in wp01 evidence.
- **Must not change:** Sections 1–4, existing public SPI/result semantics, package versions, public provider registry, legacy orchestrator, AR-7 files or evidence.
- **Completion/stop condition:** All contract tests/builds pass, no runtime/native path exists, and a fresh reviewer can trace each value/state to this plan. Stop for any need to expose a public type or choose an undefined authority/state semantics.
- **Independent review before next package:** **Yes — G1.** Review contracts, state/Unknown semantics, immutable identity and scope before WP02.

### WP02 — Protected copy-in and atomic preparation publication

- **Objective:** Produce exactly one bounded, immutable, FD-bound tool/input preparation.
- **Expected projects/files/components:** M1 `Preparation/PreparedClosure.cs`, `SafeCopyIn.cs`, `PreparedTransaction.cs`, `PreparationStore.cs`, `Native/PreparationInterop.cs`; tests `Preparation/CopyInTests.cs`, `BindingTests.cs`, `PublicationTests.cs`; initial `tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c` with finite useful-work/constructor-marker modes; `eng/linux-m1/build-native.sh` for reproducible static fixture build only.
- **Dependencies:** WP01/G1; approved parent-directory/deadline/journal interfaces using fakes until WP03. Real native tests use bounded temporary parents owned by the harness.
- **Contracts/invariants introduced:** I01/I05/I08/I13/I14; Section 7 copy/open rules, destination hashing, handle ownership, staging budgets, immutable manifest and atomic publication.
- **Implementation boundaries:** Copy and bind objects; validate allowlist and pins; publish only after fake/real journal acknowledgement. Do not launch the fixture, cache closures, add exports, discover runtimes or implement general file-tree copying.
- **Unit tests:** Copy budgets and length overflow; canonical snapshots; copy/read/digest failures at every boundary; cancellation before publication; handle disposal and no late publication; claimed digest mismatch; immutable request channels/environment.
- **Native/integration/fault tests:** Symlink/magiclink rejection; hard-linked source produces an independent copy; pathname replacement at open/copy/publish; source truncation/growth; unsupported openat2; partially written copy cleanup; final writable-FD leak inspection. No target execution.
- **Evidence retained:** Fixture source/build/hash identity, actual copied-object identities, each replacement test's source/copy/bound digest, allocation/cleanup record, portable/native outcome matrix.
- **Must not change:** Public preparation types, production profile refusal, Hufu/workflow references, sealed probes or arbitrary input-tree/export support.
- **Completion/stop condition:** Publication is all-or-nothing, every tested replacement either keeps exact copied bytes or refuses, all partial allocations are accounted. Stop if required protection needs a shared cache, privileged mount or unsupported filesystem fallback.
- **Independent review before next package:** **No separate gate.** WP02's binding diff and evidence must pass G2 before WP04.

### WP03 — Delegated domain lease, persistent allocation and external recovery scope

- **Objective:** Establish recoverable ownership before any helper exists.
- **Expected projects/files/components:** M1 `Lifecycle/RestrictedDomain.cs`, `DomainLease.cs`, `M1RecoveryScope.cs`; `Recovery/OwnershipJournal.cs`, `OwnershipRecord.cs`; `Native/CgroupInterop.cs`, `OwnerBootstrap.cs`, `RecoverySubreaper.cs`; new non-packable `src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj`, `Program.cs`; tests `Lifecycle/DomainLeaseTests.cs`, `Recovery/JournalTests.cs`, `Recovery/OwnerBootstrapTests.cs` and `Native/RecoveryHarness.cs`.
- **Dependencies:** WP01–WP02; real existing delegation/pidfd/cgroup2 support for native cases. No automatic host provisioning.
- **Contracts/invariants introduced:** I03/I04/I11; Section 10 live-handle identity, bootstrap descriptor allowlist, outside-domain owner, retained recovery duplicates, dedicated-host subreaping, one journal writer, Section 14 pre-allocation durability.
- **Implementation boundaries:** Allocate/hand off/stop empty domains and recover preparation allocations; implement bounded snapshot durability. The owner executable accepts inherited private bootstrap only. No Bubblewrap or target launch yet; no path-only adoption or general service.
- **Unit tests:** Identity mismatch and duplicate lease lifetime; control/readback errors; journal bounds, checksum/sequence, torn writes, rename/fsync failure; invalid bootstrap FD count/roles; writer-transfer failure.
- **Native/integration/fault tests:** Missing delegation refuses; retained handle versus removed/replaced name; owner crash before/after allocation registration and acknowledgement; orphaned trusted bootstrap child is adopted/reaped by the dedicated scope; recovery scope survives and cleans originals; unrelated sibling stays unchanged; cross-boot/handle-loss record refuses destructive recovery. Inject kill/read errors on empty domains safely.
- **Evidence retained:** Host boot/mount/delegation facts, privilege inventory, FD ownership diagram, ordered journal snapshots at barriers, unrelated-sibling identity before/after, cleanup evidence and explicit restart-only Unknown case.
- **Must not change:** Host controllers/ACLs/sysctls, install privileges/services, existing `OwnedCgroup.Adopt` semantics, target PID-based stop, production registration or AR-7 qualification claims.
- **Completion/stop condition:** Every allocated object is recoverable under the declared surviving-scope model and stale records never touch replacements. Stop if recovery needs a privileged daemon, numeric PID authority or resurrection of a preparation.
- **Independent review before next package:** **Yes — G2.** Review WP02–WP03 plus planned coordinator/FD protocol against Sections 7–10 before trusted helper admission in WP04.

### WP04 — Held coordinator and pinned Bubblewrap adapter

- **Objective:** Start trusted sandbox setup atomically inside its domain and prove that the target has not initialized.
- **Expected projects/files/components:** `native/linux-m1/protocol.h`, `exec-coordinator.c`; M1 `Launch/IHeldLauncher.cs`, `HeldExecution.cs`, `BubblewrapAdapter.cs`, `BubblewrapArguments.cs`, `LaunchDescriptorMap.cs`, `CoordinatorProtocol.cs`, `HelperInstallation.cs`; `Native/AtomicCgroupSpawn.cs`; extend `eng/linux-m1/build-native.sh`; tests `Launch/ArgumentTests.cs`, `ProtocolTests.cs`, `DescriptorTests.cs`, `Native/HeldLaunchTests.cs`.
- **Dependencies:** WP03/G2, pinned helper/filter, source-bound static coordinator, delegated native host. Scope/journal admission and output placeholder drains must already be bounded; no release capability is exposed.
- **Contracts/invariants introduced:** I01/I03/I10/I14; exact Section 9 FD map and helper pinning; HELD/EOF protocol; default namespace init; separate helper status/reaper; no target fork before RELEASE.
- **Implementation boundaries:** Implement only prepare-held and abort on live path. Coordinator may contain its reviewed post-release source branch but managed runtime cannot invoke it yet. Reuse/extract a private existing atomic-spawn primitive only if behavior-preserving; otherwise keep an M1-local adapter. No new mount engine.
- **Unit tests:** Golden argv and FD actions; sealed config/filter; protocol malformed length/kind/sequence/identity; unknown helper/hash/privilege rejection; distinct wrapper/coordinator status types; map collision/closure failure.
- **Native/integration/fault tests:** Start HELD and inspect namespace/mount/control inventory; absence of constructor marker; close release writer, partial/invalid frames and setup failure without target init; helper substitution by rename; missing filter; owner death while held, with wrapper adoption/reaping by the scope; descriptor audit proves no lifecycle/recovery handles reached sandbox. Abort and verify domain empty/reaped.
- **Evidence retained:** Exact helper/filter/coordinator/source hashes, held report, FD map at each process boundary, externally observed no-init marker control and expected refusal, wrapper status, domain-empty and cleanup records.
- **Must not change:** R2 gate code/protocol, existing legacy launch defaults, helper privilege, production registration, public abstractions, or allow a `--block-fd` substitute.
- **Completion/stop condition:** Held setup is observable, all failure/EOF paths prevent target fork, and abort cleans the domain. Stop for descriptor leakage, pre-release target activity, helper/filter incompatibility requiring a policy/topology change or any post-exec cgroup migration.
- **Independent review before next package:** **Yes — G2a.** Native protocol, helper source assumptions, no-init boundary and FD lifetime review before WP05.

### WP05 — Single-use release and truthful target-status protocol

- **Objective:** Implement the exact release/stop winner and exec/root observation rules without enabling integrated target release.
- **Expected projects/files/components:** M1 `Execution/ReleaseArbiter.cs`, `Execution/ReleaseDecision.cs`, `Launch/CoordinatorProtocol.cs`, `Launch/CoordinatorStatusReader.cs`; `native/linux-m1/exec-coordinator.c`, `protocol.h`; tests `Execution/ReleaseRaceTests.cs`, `Launch/TargetStatusTests.cs`, `Native/ReleaseProtocolTests.cs`; explicit qualification-only coordinator build target in `eng/linux-m1/build-native.sh`.
- **Dependencies:** WP04/G2a; durable journal, private capability, bounded transport and monotonic clock contracts. Resources/capture are fake prerequisite proofs until WP06–WP09.
- **Contracts/invariants introduced:** I02/I05/I06/I09; gate linearization after durable intent; one atomic nonblocking 128-byte write; no retries; source-bound exec-error/confirmation rules; separate direct-child/helper/target status.
- **Implementation boundaries:** Live integrated path still cannot release. Use fake transport/model tests and a separately hashed protocol-test coordinator that acknowledges then exits without forking target. Never compile a bypass into a production-selected artifact. Implement the post-release native branch and exercise its parsing/decision logic through a native unit harness with synthetic child observations. Do not execute the untrusted fixture or exercise a real post-release target exec until WP10.
- **Unit tests:** Stop-before/after commit, expiry inside gate, journal flush race, duplicate/foreign/replayed capability, zero/partial/EAGAIN/EPIPE transport, observation loss, false EOF, malformed status, wrapper exit 0 with missing target status, normal/signalled child before/after exec confirmation.
- **Native/integration/fault tests:** Barrier-controlled owner death before/after ReleaseIntent, commit and frame delivery using no-target protocol build; pipe capacity/write behavior; protocol-test process cannot execute fixture. Native unit status tests feed failed-exec, pre-exec-signal and immediate-normal-exit observations into the status logic without executing target code. Real exec/error-pipe/reaping confirmation cases are mandatory in WP10; model results cannot qualify them.
- **Evidence retained:** Linearization traces with monotonic sequence/time, protocol vectors, every race's winner/number-of-writes, distinct production/test build hashes, source review of child normal-exit paths and non-dumpable status owner.
- **Must not change:** Overall runtime refusal to release before G3, public cancellation semantics, unknown-to-success conversion, helper namespace engine or any retry/resume behavior.
- **Completion/stop condition:** Exactly one winner and at most one release write in every schedule; ambiguous delivery/result stays Unknown. Stop for a need to hold the gate over blocking I/O, resend release, infer exec from pipe EOF alone or expose coordinator controls to target.
- **Independent review before next package:** **Yes — G2b.** Review arbiter, native exec/status path and all race evidence before WP06.

### WP06 — Resource installation, readback and writable-state inventory

- **Objective:** Establish and verify the exact Section 11 envelope before release can be enabled.
- **Expected projects/files/components:** M1 `Lifecycle/ResourceInstaller.cs`, `Launch/EstablishmentVerifier.cs`, `Launch/MountInventory.cs`, `Launch/BubblewrapArguments.cs`; coordinator HELD/rlimit reporting; tests `Resources/ResourceReadbackTests.cs`, `Resources/InventoryTests.cs`, `Native/HeldResourceTests.cs`.
- **Dependencies:** WP05/G2b; domain/held setup and manifest contracts. No integrated target release.
- **Contracts/invariants introduced:** I08/I13; exact memory/swap/tasks/rlimits, split 7+1 MiB tmpfs, root read-only, complete writable inventory, installed-versus-requested evidence, explicit inode PendingQualification.
- **Implementation boundaries:** Implement current fixture envelope only. Do not create inode-quota machinery or claim object-limit qualification. Trusted held coordinator may perform bounded setup/readback probes without loading target input/code.
- **Unit tests:** Missing controller/file; parse and unit errors; writes that appear successful but read back wrong; extra writable mount, alias, default shm, privilege/filter mismatch, unsupported flags; pending inode cannot be interpreted as Passed.
- **Native/integration/fault tests:** Actual cgroup/rlimit/mount/namespace/capability/filter readbacks and core-sink preflight; force each required write/read failure; verify aggregate scratch backing and root/dev/run/sys read-only. Bounded trusted setup fault helpers can exhaust/test a limit under scope supervision; target behavior tests wait for WP10.
- **Evidence retained:** Requested/observed table with raw bounded readbacks, mount identities/capacities, helper argument vector and explicit list of pending policy/inode qualifications.
- **Must not change:** Budget values without a reviewed plan amendment, no-IP requirement, inode pending status, host configuration, arbitrary runtimes or full-profile admission.
- **Completion/stop condition:** Every M1-mandatory constraint has a verified installation route and mismatches block Owned; only named production gaps remain pending. Stop for unaccounted writable state, unenforceable byte ceiling or material topology change.
- **Independent review before next package:** **No separate gate.** Required input to G3.

### WP07 — Bounded capture connected to whole-domain stop

- **Objective:** Make output bounds enforceable outside target and caller lifetime.
- **Expected projects/files/components:** M1 `Output/RestrictedOutputCapture.cs`, `Output/CaptureCounters.cs`; tests `Output/BoundedCaptureTests.cs`, `Output/StopNotificationTests.cs`, `Native/PipeCaptureTests.cs`. Touch existing `Gagamba.Execution/OutputCapture.cs` only for a separately justified private, behavior-preserving reuse, with its existing regressions.
- **Dependencies:** WP05 arbiter and WP06 envelope; domain stop transport and fake observer/stream dependencies.
- **Contracts/invariants introduced:** I07/I09; stream/aggregate reservation, fixed buffers, prompt first-excess/read-fault stop, caller-independent drains and finite final drain.
- **Implementation boundaries:** Two pipes and bounded snapshots only; no streaming API expansion, export, line parser, unbounded diagnostics or generalized event bus. Trusted pipe-writer test processes are allowed; integrated target still held.
- **Unit tests:** Exact limit and first extra byte, concurrent stream races, huge reads, count overflow, zero-byte EOF, read exception, slow/no observer, cancellation of waiter, truncation and final-drain expiry.
- **Native/integration/fault tests:** Dual pipe flood and pipe backpressure; duplicate/retained writers; reader failure; stdout before HELD prevents release; recovery receives stop despite owner observer disconnection. Domain fixture writers must all be terminated, not just the root writer.
- **Evidence retained:** Peak retained/buffer allocation bounds, counters/stop sequence, pipe FD inventory, first-excess-to-stop ordering, root/capture/cleanup independence and cleanup of all writers.
- **Must not change:** Existing caller observer cancellation semantics, output budgets, introduce caller-dependent enforcement or make launcher the policy engine.
- **Completion/stop condition:** Every overflow/read fault promptly requests domain stop and storage remains bounded with absent observers. Stop if any path awaits root exit/subscriber before enforcing a limit or retains an unbounded queue.
- **Independent review before next package:** **No separate gate.** Required input to G3.

### WP08 — Owner orchestration, setup-inclusive deadlines and caller loss

- **Objective:** Wire preparation/held setup/capture/stop into one execution-owned lifetime, with release still inhibited pending G3.
- **Expected projects/files/components:** M1 `Execution/RestrictedExecutionOwner.cs`, `Execution/MonotonicDeadline.cs`, `Execution/StopCoordinator.cs`; owner `Program.cs`; `Lifecycle/M1RecoveryScope.cs`; tests `Execution/OwnerStateTests.cs`, `DeadlineTests.cs`, `CancellationTests.cs`, `Native/OwnerLossTests.cs`.
- **Dependencies:** WP01–WP07; private owner bootstrap, actual capture and readback.
- **Contracts/invariants introduced:** Setup-inclusive 10/30 s clocks before work, final gate recheck, caller/recovery liveness separation, stop coalescing, sole reapers and independent observation task.
- **Implementation boundaries:** Wire only fixed request flow. No public orchestrator replacement, scheduler, retry policy, external service or integrated fixture release.
- **Unit tests:** Expiry while copying, blocked setup, delayed timer callback, clock unit conversion, stop during journal I/O, owner/caller/recovery EOF, repeated stop, watcher cancellation, one stable terminal task.
- **Native/integration/fault tests:** Stall copy/setup/HELD with external watchdog active; kill caller but retain scope; kill owner in each pre-release state; drop recovery lifetime channel; prove independent scope stops blocked owner's domain and fences journal takeover.
- **Evidence retained:** Monotonic start/deadline/stop traces, actual process-role tree, recovery-vs-test-rescue classification, states reached and domain-empty/reaper outcomes.
- **Must not change:** Setup-inclusive origin, observation-only wait cancellation, public runtime orchestrator, full-profile refusal, one-transaction scope or owner privilege.
- **Completion/stop condition:** All setup work is bounded, late workers cannot publish/release, and declared owner/caller failures trigger the proper stop. Stop if cancellation depends on caller stack unwinding or timeout merely abandons live work.
- **Independent review before next package:** **No separate gate.** Required input to G3.

### WP09 — Reconciliation, semantic receipts and integration readiness

- **Objective:** Close the transaction on every failure without replay or unsafe cleanup, and make its result auditable.
- **Expected projects/files/components:** M1 `Recovery/Reconciler.cs`, `OwnershipJournal.cs`, `OwnershipRecord.cs`; `Evidence/TransactionReceipt.cs`, `ReceiptWriter.cs`; `Execution/RestrictedExecutionOwner.cs`; tests `Recovery/ReconciliationTests.cs`, `CrashBoundaryTests.cs`, `Evidence/ReceiptTests.cs`, `Native/RecoveryIdentityTests.cs`.
- **Dependencies:** WP01–WP08; actual domain/capture/owner mechanisms and journal ordering.
- **Contracts/invariants introduced:** I04/I06/I09; Section 14 idempotence and fencing; field-wise Unknown; semantic receipt distinct from private recovery and qualification diagnostics.
- **Implementation boundaries:** Finalize existing cleanup and bounded evidence. No reboot recovery authority, path-only adoption, output reconstruction, exports or repair of historical AR-7 uncertainty.
- **Unit tests:** Every Section 13 result row; corruption/oversize/torn terminal snapshot; repeated reconciliation; identity mismatch; known root with failed cleanup; Unknown with confirmed cleanup; journal persistence failure cannot suppress urgent kill.
- **Native/integration/fault tests:** Crash at allocation, prepared, held, intent, stop and cleanup barriers with protocol-test coordinator; stale/replaced parent/child entries; retained-handle recovery versus restart-only records; interrupted cleanup resumes safely; unrelated sibling untouched. Full released-target crash coverage follows WP11.
- **Evidence retained:** Schema samples for success/refusal/Unknown/cleanup failure, crash-state matrix, same-object verification, unresolved-resource records and bounded artifact inventory.
- **Must not change:** Release inhibition before G3, Unknown history, private recovery exposure, no public registration, no unsolicited operator cleanup or host reconfiguration.
- **Completion/stop condition:** All prerequisites for a live fixed-fixture transaction are implemented and negative tests pass; complete G3 checklist. Stop on any unresolved authority, output, storage, release or recovery correctness defect.
- **Independent review before next package:** **Yes — G3.** Independent review of WP05–WP09 as composed, including the actual production-selected coordinator and protocol-test exclusion. Only accepted G3 permits WP10 target release.

### WP10 — One complete internal static-native transaction

- **Objective:** Execute the exact prepared useful-work fixture through the real owned release path and retain truthful evidence.
- **Expected projects/files/components:** Extend `tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c` with bounded hash/echo, constructor marker, exit, bounded child/retained-writer, output and resource-stress modes; tests `Native/TransactionTests.cs`, `Native/FixtureEnvelope.cs`; `eng/linux-m1/run-m1.sh`; minimal owner integration wiring in `RestrictedExecutionOwner.cs`; complete `docs/evidence/gagamba-linux-m1.md` run index.
- **Dependencies:** WP09/G3, exact reviewed native artifacts and supported delegated host. All prior native gates executed; skips do not authorize launch.
- **Contracts/invariants introduced:** Composition of I01–I14 for one internal transaction; useful work bound to exact input/tool; actual separate target/helper/domain/capture results.
- **Implementation boundaries:** Enable only the allowlisted source-bound fixture. Keep internal entry points and production profile refusal. No installed-package API, arbitrary command selector, export, new runtime or platform.
- **Unit tests:** Integrated request allowlist, actual prerequisite aggregation, fixed budgets/channels, incompatible artifact/test-build rejection and public production-request refusal.
- **Native/integration/fault tests:** Useful output matches copied input; normal zero/nonzero exit, failed exec, pre-exec signal and immediate exit with actual error-pipe/reaping observations; constructor cannot run while held; release after accepted stop fails; root exit with descendant/writer; output excess; memory/tasks/FD/scratch/shm limits and bounded ordinary-core suppression checks; failure readback prevents marker; wrapper-vs-target exit mismatch. Exercise final readback-to-release mutation barrier.
- **Evidence retained:** Exact commit/host/artifact pins, prepared/established manifest and semantic receipt, durable release chronology, negative/positive control pair, root/helper raw status, output bounds and final empty/cleanup evidence.
- **Must not change:** Freeze points, full-profile refusal, no export/API/provider registration, prior sealed evidence or production-gap statuses without independent qualification.
- **Completion/stop condition:** One useful transaction and all listed bounded negative cases pass with confirmed cleanup, or accurately failed cleanup is treated as a failed test requiring repair. Stop immediately on pre-init violation, released-but-unowned work, wrong copied bytes or target access to controls.
- **Independent review before next package:** **No separate gate.** WP10 evidence is mandatory input to G4 after fault coverage.

### WP11 — Focused P05/P06 races and whole-lifecycle fault matrix

- **Objective:** Attack the new transaction's boundaries sufficiently for a bounded M1 review, without claiming full production qualification.
- **Expected projects/files/components:** Tests `Native/ReleaseFaultMatrixTests.cs`, `Native/ReplacementTests.cs`, `Native/OwnerDeathMatrixTests.cs`, `Native/OutputFaultTests.cs`, `Native/ControlIsolationTests.cs`; bounded barrier support in the existing harness; fixes confined to implicated M1 components; `docs/evidence/linux-m1/wp11.md` and selected compact case records.
- **Dependencies:** WP10 useful transaction with exact source binding. Any implementation fix invalidates affected earlier evidence until rerun on the fixed revision.
- **Contracts/invariants introduced:** No new authority; evidence for the existing release, replacement, owner-loss, output and reconciliation guarantees. Qualification-only barriers never grant production release.
- **Implementation boundaries:** Run Section 17 matrix on the selected host/envelope. No broad AR-7 feasibility rerun, new inode implementation, network-policy expansion, export corpus or runtime qualification project.
- **Unit tests:** Regression for every discovered state/codec/identity/capture bug; pairwise ordering coverage and explicit expected Unknown fields at each crash boundary.
- **Native/integration/fault tests:** Owner/caller death before and after every major transition; kill coordinator/wrapper/root separately; stop-vs-release races; partial/closed/status channels; tool/input/helper replacement; retained output writers; missing readbacks; source/config mutation; target ptrace/proc-fd/control acquisition attempts; recovery loses original handles; unrelated sibling remains intact.
- **Evidence retained:** Per-case injection barrier, permitted effects, actual trusted observations, release winner, root/helper distinctions, cleanup status, rescue required/not required, exact host/commit/artifact identities and unexecuted cases. Repeated stress results supplement deterministic cases, not replace them.
- **Must not change:** No-IP/inode/P08/P09 pending claims, production refusal, AR-7 historical records, use external rescue as a pass, or treat observed PID reuse checks as atomic authority.
- **Completion/stop condition:** Every M1-mandatory matrix cell executes and passes; unresolved safety defect stops progress. Unavailable host operations stay Unavailable and block the corresponding M1 claim, not silently skip to completion. Production-only cells remain explicitly pending.
- **Independent review before next package:** **Yes — G4.** Review source-bound fault evidence and remaining-gap ledger before final handoff.

### WP12 — Independent M1 review and bounded handoff

- **Objective:** Decide whether this internal milestone is complete and identify the next separately authorized slice.
- **Expected projects/files/components:** `docs/reviews/gagamba-linux-m1-review.md`, `docs/evidence/gagamba-linux-m1.md`, `docs/handoff.md`, `docs/work-queue.md`; any necessary correction to this plan is explicitly identified, not silently rewritten. No implementation changes in the review-only package; findings return to the owning earlier package.
- **Dependencies:** WP11/G4 and exact reviewed implementation/evidence revision.
- **Contracts/invariants introduced:** No new runtime contracts. Review acceptance is scoped to the internal fixture and declared surviving-recovery-scope model.
- **Implementation boundaries:** Independent source/evidence review and documentation only. Reviewer checks all Section 18 gates and traces every claim to code/native evidence. A further runtime/API/production task requires a new bounded authorization.
- **Unit tests:** No new tests for documentation. Verify recorded required test results correspond to the reviewed revision; a changed implementation requires affected tests to be rerun in its package.
- **Native/integration/fault tests:** No automatic rerun of sealed feasibility. Independently reproduce selected decisive M1 positive/negative/race/recovery cases if needed; record actual execution versus source-only review.
- **Evidence retained:** Reviewer identity/date, exact reviewed commits, blocking/nonblocking findings, resolved findings with retest links, acceptance/refusal and explicit production-gap ledger.
- **Must not change:** Production provider registration, packages/API, Hufu/workflow/platform scope, AR-7 verdicts, inode pending status or declare general Linux security/production readiness.
- **Completion/stop condition:** G5 explicitly accepts the internal milestone with no unresolved M1 blocker, or records a stop and sends concrete findings back to their packages. Do not continue into a new feature merely because the plan's last package is reached.
- **Independent review before next package:** **Yes — G5 is this package.** No next implementation package is authorized by this plan.

## 17. Test and fault-injection strategy

Use deterministic barriers controlled by the trusted test harness at allocation intent/created/registered, copy completed, Prepared persisted/published, helper admitted, HELD, readback accepted, Owned persisted, ReleaseIntent durable, immediately before/after arbiter commit, frame delivered, child fork/before exec, exec observed, root observed, domain empty, capture terminal, removal and terminal persistence. Production code may expose internal dependency interfaces for faults; test hooks must not be enabled through target environment or an ordinary runtime option. Every native test has a finite watchdog outside the owner and recovery scope. If that outer watchdog rescues a run, mechanism recovery failed.

| Test family | Required assertion and negative control |
|---|---|
| Pre-init | A fixture constructor writes a bounded marker only after valid release; missing marker in a known-working held/refused case. Pair with identical allowed transaction showing marker after trusted release chronology. A marker alone never proves semantic exec. |
| Preparation | Copy/mutate/rename/hardlink/symlink at every binding barrier; prepared exact object remains or release refuses. Foreign/replayed capabilities and wrong generation cannot write RELEASE. |
| Linearization | Stop first yields zero writes/zero init; release first permits effects but post-stop kills whole domain; durable-intent crash produces MayHaveExecuted without retry. Clock advances while journal flush/gate is delayed. |
| FD/control isolation | Audit owner/helper/coordinator/target FD sets; target cannot read release/status/config/recovery/domain controls or impersonate owner via retained writer. Test proc-fd/ptrace access to trusted coordinator and inherited socket paths. |
| Status | Distinguish exec errno, pre-exec death, immediate exit, signalled target, lost coordinator, wrapper success/failure, malformed/duplicate frames and helper exit 128+signal. Assert exact per-field Unknown cases. |
| Resource/readback | One missing/wrong required limit per case prevents release. Actual bounded resource stress after G3 confirms observed ceilings within stated semantics; enumerate mounts and open-unlinked/sparse/mapped scratch cases within finite fixture limits. Inode production qualification remains Pending. |
| Output | Exact/over limit independently and simultaneously; no/slow observer; huge writes; reader fault; root exit with retained writer; final-drain timeout. Counter/storage bound and whole-domain stop are both asserted. |
| Ownership/recovery | Kill each role separately at each state; retained handles survive owner death; all-holders-lost record refuses destructive adoption; unrelated cgroup/staging sibling survives identity mismatch. Scope rescue and outer-harness rescue are reported separately. |
| Cleanup | Kill/control/event/read/remove/fsync failures; timeout and partial teardown; repeated reconciliation; known root with cleanup failure and unknown root with confirmed cleanup. No target replay. |
| Scope/regression | No production profile registration; non-Linux refused; arbitrary command/runtime/export request refused; existing lifecycle/output tests unchanged where touched. |

Portable tests run on ordinary .NET hosts using fakes. Native tests require the explicit delegated Linux environment and pinned artifacts, with preflight recorded as Supported/Unsupported/Unavailable. A missing prerequisite is a truthful refusal case but cannot stand in for a successful delegated transaction. Keep each probe finite in tasks, bytes, files and duration. The native fixture's stress limits are test safeguards, not production inode enforcement. Do not use privileged observers to satisfy an unprivileged mechanism claim. Record LSM state if observable, otherwise Unknown. No claims are inherited from old AR-7 hosts merely because the new command resembles a probe.

## 18. Review gates

| Gate | Required before | Acceptance criteria |
|---|---|---|
| G1 | WP02 | Internal value/capability shapes and pure states match ADR; immutable canonical identity; no public API/registration; WP01 has executable tests and no architectural placeholders. |
| G2 | WP04 | Copy/FD binding, incremental durability, live cgroup identity, unprivileged recovery topology and the specified native protocol/FD map are coherent; no record-only destructive authority. |
| G2a | WP05 | Actual pinned helper transport and held coordinator reviewed; target code cannot initialize; EOF/invalid control fails closed; admission atomic; descriptor exposure bounded. |
| G2b | WP06 | Release-vs-stop proof, single-write behavior, crash-intent rule, child exec/error logic and truthful target-vs-wrapper status accepted. |
| G3 | WP10 | Real resources/readbacks, bounded capture stop, caller/owner loss, setup-inclusive deadlines, journal ordering and reconciliation are composed; no missing mandatory prerequisite; test-only binaries/hooks cannot be selected by integrated path. |
| G4 | WP12 | Useful transaction plus deterministic source-bound fault matrix; each claimed behavior actually executed; unknowns/nonclaims preserved; no outer-rescue case labelled success. |
| G5 | Any later separately authorized milestone | Independent M1 review accepts this precise internal scope, records production gaps and proposes the next bounded decision. No implicit production readiness. |

The coordinator, FD layout, atomic admission, recovery topology and writable-store layout are selected in this document. Reviews verify those choices; they are not invitations for an implementation agent to choose a different architecture. If source/native evidence contradicts a choice, record the concrete contradiction and stop the affected package for a narrow plan amendment. Preserve accepted Sections 1–4 unless the contradiction actually concerns them. Do not solve a blocked package by weakening a required invariant, silently widening authority or installing privilege.

## 19. Remaining production-qualification gaps

M1 can be complete while the following remain pending. They must be present in the final review and must continue to prevent advertising or accepting `restricted-offline-closure-v1`:

- **Inode/object ceilings and metadata accounting:** mandatory before production qualification; explicitly PendingQualification in M1. Neither 128 staging objects, finite fixture loops, memcg nor byte-sized tmpfs proves a general independent writable inode ceiling. Do not expand M1 to implement this unless a concrete ADR contradiction is found; the ADR expressly permits implementation to start first.
- **Complete P05/P06:** M1 provides a narrow immutable preparation/release and protected-copy implementation with focused races. Full hostile-object/filesystem/storage/alias corpus, generalized closures and export are not qualified. Export remains unavailable, with no M1 API.
- **Owner death and recovery deployment:** the surviving unprivileged recovery scope is real M1 machinery, but scope/owner simultaneous loss, all-handle loss, reboot/service restart, durable long-lived deployment and exhaustive death containment remain unqualified. Records alone never regain authority.
- **Complete no-IP/IPC/syscall policy:** the frozen filter is a source-bound starting point, not evidence of complete loopback prohibition or all alternate ABI/transport paths. Namespace isolation and the tested finite fixture do not qualify the production offline bundle. Any later policy strengthening needs new reviewed evidence.
- **Runtime P08:** one static native fixture only; no arbitrary native program or self-contained .NET closure qualification, runtime discovery or installed SDK.
- **Package/security-manifest P09:** no public API/provider registration, installed-package consumer qualification, supply-chain release gate, publication or blanket support matrix. Dynamic Bubblewrap dependencies remain part of the TCB even if locally pinned.
- **Core sinks and charging:** setting RLIMIT_CORE=0 is not exhaustive core-sink qualification. The selected M1 host preflight and finite fault cases do not qualify every host collector, runtime dump facility, helper-overhead charge or per-case swap behavior. Preserve the ADR's explicit residual accounting/core-dump limitations.
- **Host breadth:** conclusions apply only to actually tested kernel/libc/filesystem/delegation/WSL arrangement. LSM unknowns, scheduler/resource residuals, same-UID host-owner trust and hostile multi-tenant availability remain explicitly bounded.
- **Platforms/integration:** Windows/macOS remain refusal-only; VM, Hufu/Penghou/workflow and a privileged service are outside this plan. AR-7 is not reopened, and its chronology, PID-reuse and historical cleanup qualifications remain intact.

No skipped M1-mandatory native test is a production gap that can be waved through: it blocks that M1 work package. Conversely, the production-only gaps above must not be turned into an unbounded expansion of this first milestone.

## 20. Implementation-agent handoff

Start with **WP01 only**. Read `AGENTS.md`, the current handoff/queue, ADR 0003, this complete plan and the remediation evidence standard. Inspect repository status and preserve unrelated work. Confirm actual .NET/xUnit/project conventions from the existing projects; add the two named contract files, non-packable M1/test projects, capability/store skeleton and pure model/tests listed in WP01. Use the Section 5 fields, enum values, canonical encoding rules and Section 6 transitions; do not invent native mechanics or defer those contracts to a later agent. Build/run portable checks and retain the exact-revision evidence. Stop for G1 independent review before WP02.

For each later package, verify its dependency gate, implement only its listed boundary, run required portable/native/fault tests, record exact artifact/source/host identity and stop condition, and obtain the specified review. Review findings return to the owning package and invalidate affected evidence until retested. A native prerequisite unavailable on the current host must be recorded and blocks native completion; do not provision privilege or call a skip a pass. Do not merge a partially reviewed live-release path into public registration.

The implementation handoff is complete only after WP12/G5 accepts the internal transaction and updates the gap ledger. Until then, report the last completed package, next gate and precise blockers. After that acceptance, stop for the next bounded authorization; do not proceed automatically to exports, .NET targets, production registration or Hufu/workflow work.

### Planning-artifact consistency check

This revision preserves Sections 1–4 verbatim and resolves Sections 5–20 against ADR 0003: internal single-use preparation and release authority; no target initialization before trusted release; externally retained stable domain controls; incremental durable recovery; field-wise Unknown; bounded external output; read-back resources; protected copied objects; separate wrapper/target status; no new privilege or registration. Section 16 assigns each mechanism, test, evidence requirement and gate to an ordered package. WP01 is executable from its named files/contracts without selecting a launcher, recovery or release architecture. Inode/object production qualification remains pending as the ADR requires. These are planning decisions and future test obligations, not claims that code or native tests have already run.
