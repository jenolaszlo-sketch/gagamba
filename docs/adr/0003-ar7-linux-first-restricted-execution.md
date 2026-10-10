# AR-7 ADR: Linux-first restricted execution

**Date:** 2026-10-10

**Status:** Architecture approved; implementation authorized within the milestone below. Production qualification remains pending.

**Evidence checkpoint:** `cb511cd18ba25daeda76cf41a400d1136c6ba0f0`

**Review branch:** `qualification/ar7-feasibility`

## Decision

Adopt the surviving rootless Linux composition as Gagamba’s first restricted-execution implementation baseline.

Retain pinned, non-setuid Bubblewrap behind a narrow, internal launcher abstraction. Gagamba owns preparation, the effective authority/resource manifest, release authorization, lifecycle ownership, output enforcement, cleanup and evidence. Bubblewrap supplies the selected namespace/setup mechanism; its successful exit is not proof that Gagamba’s contract was satisfied.

Implement an explicit **prepare → own → release → exec target** boundary. No target-controlled initialization may occur before the release prerequisites hold. The final R2 `/gate` establishes feasibility of observing that boundary; its source and protocol are test instrumentation and are not approved as the production mechanism.

Keep `restricted-offline-closure-v1` as a required bundle. It permits staged read-only runtime and inputs, bounded disposable writable state, and prepared data channels. It denies host-service IPC, ambient credentials and unprepared authority. Its no-IP requirement includes intra-execution loopback. AR-7 has not qualified that complete requirement, and this decision does not weaken it.

Windows native and macOS remain refusal-only for this profile. Windows VM remains a deferred, unimplemented alternative.

This decision supersedes earlier implementation holds only for the work expressly authorized below. It does not reopen accepted feasibility results, alter the frozen AR-6 seal, or qualify a production provider.

## Evidence basis

The controlling disposition is the final independent review at the sealed checkpoint: **AR-7 EVIDENCE READY FOR ARCHITECTURE DECISION**. Earlier contradictory summaries remain historical and do not override that disposition.

| Evidence | Accepted result | Architectural consequence |
|---|---|---|
| P02 | VERDICT SUPPORTED | Select the same-host-UID rootless namespace composition with protected external cgroup controls. Same UID alone is not isolation. |
| P03 | VERDICT SUPPORTED WITH QUALIFICATION | Select private network/mount/IPC views and prepared channels. Retain incomplete transport coverage, the manager-leaf placement exception and the unproved loopback prohibition. |
| P04 | VERDICT SUPPORTED WITH QUALIFICATION | Select cgroup memory/task limits, no swap, finite scratch and inherited FD limits. Native and small self-contained .NET execution are feasible; full storage, runtime and output qualification remains outstanding. |
| P07 | VERDICT SUPPORTED | Make lifecycle-domain ownership, pre-initialization release ordering, whole-domain stop and independent cleanup status central production concepts. |

P07’s accepted R1/R3/R4/R5/R6 results and final frozen-filter R2 pair justify proceeding. They do not establish a production supervisor, release implementation or automatic recovery guarantee.

The decisive source records are the aggregate feasibility report, P02/P03/P04/P07 evidence, and the independent evidence, remediation, replacement-R2, separate-gate and final reviews under `docs/reviews/`, all as committed at the checkpoint above.

No additional AR-7 feasibility execution is required to make this decision.

## Production invariants

These requirements apply regardless of launcher implementation:

1. **No execution before establishment.** Target-controlled loader code, constructors, runtime startup hooks and input-dependent initialization are execution. A barrier inside target `main()` is too late.
2. **No released-but-unowned work.** The entire execution domain remains identifiable and stoppable independently of target parentage, root exit and application cooperation.
3. **No authority widening.** Runtime discovery, compatibility handling and recovery cannot add grants or silently substitute weaker isolation.
4. **Immutable release identity.** The released tool, runtime, inputs, environment, channels, budgets and authority manifest are the exact prepared objects and values accepted for release.
5. **Protected controls.** The target cannot acquire lifecycle controls, release authority, manager memory, recovery records or another execution’s resources.
6. **Finite consumption.** Required budgets apply before release; manager-side capture, staging, export and diagnostic work are also bounded.
7. **Truthful results.** Root result, helper result, execution outcome, output status and cleanup status remain distinct. Unknown information is never reconstructed from success elsewhere.
8. **Fail-closed admission.** Missing or unestablished mandatory restrictions cause refusal before target execution. Restricted requests never fall back to ordinary lifecycle launch.
9. **Stop and reconcile after failure.** Recovery never resumes or automatically replays an execution whose release or result is uncertain.

The first product scope is trusted-caller workstation/tool execution. It does not promise hostile multi-tenant availability or protection from a compromised kernel, administrator or trusted execution owner.

### Classification of the Linux composition

| Element | Classification | Required interpretation |
|---|---|---|
| Pinned Bubblewrap | Current implementation choice | Its identity and approved configuration are mandatory while selected. |
| User namespace | Required production invariant for this Linux baseline | Unprivileged setup with a non-root host identity; no target regain of setup authority. |
| Mount namespace | Required production invariant | Only the declared closure, minimal baseline and bounded writable state are reachable. |
| PID namespace | Required production invariant | Isolated process view and qualified helper/init lifecycle behavior. |
| Network namespace | Required production invariant | Host network separation; insufficient alone to prove no intra-execution IP. |
| IPC namespace | Required production invariant | Private applicable IPC objects; not a substitute for filesystem and descriptor controls. |
| Dropped capabilities | Required production invariant | No usable target capabilities or privilege regain. |
| `no_new_privs` | Required production invariant | Established before target initialization and inherited. |
| Seccomp enforcement | Required production invariant | Installed before target initialization with explicit architecture/ABI coverage. |
| Exact AR-7 frozen filter bytes | Current implementation choice for the initial slice | An evidence-backed starting point, not a permanently complete production policy. |
| Externally owned cgroup lifecycle/resource domain | Required production invariant | Domain-level stop and resource enforcement remain outside target authority. |
| Narrow staged read-only runtime and inputs | Required production invariant | Protected object copies and bindings, not mutable host-tree mounts. |
| Bounded writable scratch | Required production invariant | Includes all temp/cache/shared-memory backing that the profile exposes. |
| Prepared communication channels | Required production invariant | Explicit data-channel allowlist; no inherited ambient authority. |
| External observation and cleanup authority | Required production invariant | Independent of target assertions and target-root survival. |
| UTS isolation, helper process count, directory names and layout | Current implementation choices | Preserve initially where useful; do not expose as portable guarantees. |
| UID/GID 65534, root test observer, positive/sibling leaves, nonce markers, alarms and `/gate` | Qualification-only fixture details | Do not copy these into the production contract. |
| Probe budget values | Qualification/reference configuration | Useful starting values, not universal minima or architectural constants. |

## Linux topology and trusted components

Approve this responsibility structure:

```text
Trusted host / pre-existing cgroup delegation
  └─ Gagamba execution owner, outside the target domain
       ├─ protected preparation and recovery records
       ├─ release arbitration, deadline and bounded output readers
       ├─ stable lifecycle-domain controls
       └─ target cgroup
            └─ pinned Bubblewrap setup/init chain
                 └─ exact prepared target
                      └─ confined descendants
```

The execution owner may contain a manager component, but ownership must not exist solely in a caller’s stack frame or a disposable launch wrapper.

Use an unprivileged execution owner with pre-existing operator-provided delegation. Do not introduce a root daemon, setuid helper or automatic host-security reconfiguration. The production replacement for the probe’s root observer must demonstrate sufficient delegated authority and failure recovery; AR-7’s root observer does not prove that deployment.

The trusted computing base includes the host kernel and relevant filesystem implementation, Bubblewrap and its executable dependencies, Gagamba’s native adapter, release coordinator, preparation/export code, output readers and recovery machinery. A staged runtime is identified and constrained content; it is not trusted to enforce its own isolation.

The launcher abstraction should express only the needs established here: prepare a held execution, report trusted establishment, commit or abort release, expose truthful status and cooperate with owned teardown. Keep cgroup ownership and public semantics outside helper-specific argument construction. Do not build a general launcher plugin framework.

A small trusted exec coordinator may be introduced if necessary to realize the release boundary. It must have its own reviewed production protocol and source binding. This is not authorization to replace Bubblewrap’s namespace/mount implementation or copy the R2 gate unchanged.

## Release and lifecycle model

Use the following conceptual states:

```text
Preparing → Prepared → Owned → ReleaseCommitted → Running → Completed
     \          \         \             \           \
      └──────────┴─────────┴──────────────┴───────────→ Stopping
                                                        ↓
                                              Failed / Cancelled

Observation loss → Unknown execution outcome + reconciliation
Cleanup proceeds with separate Confirmed / Failed / Unknown status.
```

An execution that exits immediately may reach a terminal result without a separately observable `Running` interval.

### State meanings

| State | Meaning |
|---|---|
| Preparing | Allocate and incrementally record owned resources; stage and validate content. No target code runs. |
| Prepared | Invocation and authority/resource manifests are fixed; staging is protected; preparation is single-use and issuer-bound. |
| Owned | Sandbox setup is complete; limits and channels are ready; helpers are domain-owned; stop/deadline machinery and recovery responsibility are established; target remains held before initialization. |
| ReleaseCommitted | The single-use preparation has been consumed by the release arbiter after final authorization, stop and expiry checks. Target execution is permitted but not yet necessarily observed. |
| Running | Trusted launcher/status evidence establishes target exec. Target stdout alone is insufficient. |
| Completed | Root result is known, domain is empty and capture has reached a terminal state. Cleanup and export outcomes remain separately reported. |
| Failed / Cancelled | Setup or execution has failed or stop has won. Required teardown follows; these labels do not themselves prove cleanup. |
| Unknown | Observation cannot establish the interrupted execution result. Later cleanup does not convert this to success. |

### Atomicity and recovery

- **Preparation publication must be atomic:** consumers see either a complete immutable preparation or no releasable preparation.
- **Allocation must be recoverable:** register each resource before it can become orphaned or permit target effects. Persist protected ownership information incrementally.
- **Helper admission must be atomic or held:** place setup processes in the owned domain at creation, or prove they cannot release target code before confirmed placement. Post-exec migration is unacceptable.
- **Release versus stop must be linearizable:** one trusted arbiter consumes the preparation and decides the winner. A stop accepted first permanently forbids release. A stop accepted afterward terminates the domain.
- **Deadline checking belongs at the release boundary:** use a monotonic, setup-inclusive deadline and a bounded setup phase. Do not equate a pre-Bubblewrap check with a pre-target check.
- **Release delivery must be single-use and fail-closed:** EOF, invalid identity, stale preparation, channel failure or owner loss cannot authorize execution.
- **Release and exec need not be one durable transaction:** crashes between them must remain recoverable as “may have executed.” Kill/reconcile that domain; never resend authorization or replay the target to resolve uncertainty.
- **Terminal publication must be stable:** concurrent and later observers see the same execution result. Cancelling an observation does not cancel execution.

Control channels must be separate from application stdio and unavailable after target exec. A target must not be able to retain a descriptor that impersonates a live owner or keeps release authority usable.

### Ownership and failure

Before release, retain:

- A unique execution identity, preparation issuer/generation and host boot identity.
- Stable cgroup control handles and a protected domain identity.
- Protected staging/scratch identities and an incremental recovery record.
- Bound tool/runtime/input and manifest identities.
- An exclusive release capability.
- Prepared output readers, status/reaping resources, and armed deadline/owner-loss handling.
- A designated external recovery authority capable of reconciling the domain if the launch manager fails.

The production stop primitive is whole-domain termination, followed by verified domain emptiness and required reaping. Numeric PIDs are diagnostic data, not lifecycle authority. If individual process operations are necessary, use stable kernel references or an equivalently qualified mechanism; PID/starttime rechecking alone is not an atomic solution.

Root exit does not imply descendant completion. Descendants may remain owned until they exit or a stop/deadline ends the domain. The helper may terminate them earlier under its disclosed topology. Gagamba promises neither descendant survival nor automatic elimination solely because the root exited.

Record helper and target results separately. Do not infer a target signal merely from Bubblewrap’s `128 + signal` exit convention where that is ambiguous.

Caller loss must trigger the execution owner’s stop policy. Manager loss must leave domain controls available externally. Loss of the execution owner itself requires a qualified death-containment/reconciliation path; cgroups alone are not owner-death enforcement. If that path is unavailable, the production profile refuses.

Cleanup has component results for domain stop, process/status resources, scratch/staging, controls and exports:

- **Confirmed:** applicable owned resources were verifiably reclaimed.
- **Failed:** a known cleanup operation failed or identified residue remains.
- **Unknown:** observation or identity is insufficient.

Failed or unknown cleanup retains protected debt and prevents unsafe resource reuse. Reconciliation may update cleanup evidence without recovering an unknown root result.

## Resource model

The first production profile requires explicit finite budgets. Numerical values are manifest data, subject to qualification and host admission. Missing values or unsupported guarantees refuse; compatibility never silently increases them.

| Resource | Approved production semantics |
|---|---|
| Memory | Aggregate target-domain cgroup memory ceiling, including in-domain helper overhead. Report native accounting; do not call it portable RSS or complete host-memory accounting. |
| Swap | Zero workload swap, established through the effective domain hierarchy and verified before release. |
| Tasks/threads | Hard Linux task ceiling through `pids.max`, including helper tasks. Expose this as tasks, not a claimed equivalent process count. |
| FDs | Finite hard `RLIMIT_NOFILE` before target exec. State that it is per-process, combined with task and memory limits; do not claim an independent cgroup FD quota. |
| Scratch bytes | Kernel-enforced finite backing for every writable store. Account for open-unlinked files, sparse/mapped writes and aliases. No unbounded host temp fallback. |
| Inodes/metadata | Explicit finite inode/object ceilings on writable filesystems, with documented metadata accounting and residual limits. Mandatory before production qualification. |
| stdout/stderr | Independent byte limits, bounded reader memory and whole-domain stop on overflow. |
| `/dev/shm` | Private, explicitly bounded and included in the writable-state manifest and aggregate resource accounting. No implicit default-size host mount. |
| Temp/cache | Synthetic HOME/TEMP/cache locations inside declared bounded storage. Persistent shared writable caches are excluded. |
| Core dumps | Core creation disabled, runtime dump facilities disabled where applicable, and host core-sink behavior qualified. `RLIMIT_CORE=0` alone does not discharge this gate. |
| Time/CPU | Finite setup and overall monotonic deadlines. Exact CPU-rate guarantees remain unsupported unless separately implemented and qualified. |
| Manager work | Bounded staging bytes/object counts, export bytes/counts, output buffers, diagnostic records and admitted concurrency. |

A request for an exact process-only limit cannot be silently translated into a more permissive task budget. A conservative task cap may satisfy a process ceiling only when its stricter effect is explicit and accepted.

Use P04’s 128 MiB memory, 48 tasks, FD ceiling 64 and 8 MiB scratch as the first native implementation’s reference configuration. These are engineering starting values, not a minimum .NET budget or mandatory production defaults. Any `/dev/shm` and additional writable mount allocation must be explicitly included rather than assumed covered by the scratch number.

**Inode decision:** an independently configured inode bound is not required before writing implementation code, but it is mandatory before production qualification of this profile. AR-7 did not configure or demonstrate one. A 20,000-file harness cap and memcg accounting are not substitutes. If the selected helper cannot establish the required storage bounds without a material topology change, stop and review that change.

## Output enforcement

Place output-limit enforcement in Gagamba’s execution owner/broker layer, outside the target domain and below Hufu or workflow integration.

The launcher prepares descriptors and closes authority handles. It does not become the output policy engine. A caller-side convenience reader is insufficient if its disappearance disables enforcement.

Required behavior:

- **stdout overflow:** retain at most the declared stdout prefix, record stdout overflow and request whole-domain termination.
- **stderr overflow:** apply the same rule independently to stderr.
- **Concurrent output:** drain both streams concurrently with bounded buffers. Never collect everything and truncate afterward.
- **Blocked downstream consumer:** do not let consumer backpressure disable capture or deadline handling. Bound queues; stop with an explicit output-consumer failure if delivery cannot remain bounded.
- **Blocked target writer:** continue deadline/stop enforcement independently of pipe progress.
- **Descendants retaining descriptors:** root exit does not complete capture. Domain termination closes contained writers; use a finite final-drain period and report incomplete capture if EOF cannot be established.
- **Reader failure:** stop the domain and report failure/uncertainty rather than continue unbounded or unobserved execution.

Byte limits bound accepted capture, not the exact number of bytes a target can attempt to write before termination. Kernel pipe buffers and bounded reader chunks may contain additional bytes. Record retained bytes, observed bytes or lower bounds, overflow and truncation distinctly.

Existing AR-5 capture code may be reused after review, but its earlier qualification does not qualify the new topology. P04 evidence-prefix truncation is not enforcement.

## Filesystem and runtime closure

Represent prepared resources as a versioned manifest bound to protected objects:

| Resource | Representation and handling |
|---|---|
| Runtime/tool closure | Complete file inventory, content digests, modes, runtime revision and declared native dependencies; copied into protected staging and mounted read-only. |
| Inputs | Fresh ordinary-file/directory copies with content identity and logical destinations. No hardlinking source objects into staging. |
| Scratch | Fresh execution-owned stores with byte and object ceilings, permitted logical locations and cleanup identities. |
| Exportable outputs | Explicit selectors, count/byte/path limits and destination policy. Export ordinary files only after confirmed domain stop. |
| Immutable pre-release state | Bound invocation, environment, closure objects, channel definitions and manifests. Hashing is supplemented by protection against subsequent mutation. |
| Runtime temp/cache | Explicit mappings into bounded disposable stores; no ambient HOME, SDK discovery, agent sockets or caller cache reuse. |
| Writable-state inventory | Every writable filesystem, pseudo-filesystem surface, shared-memory class, output path and manager-created artifact classified by authority, accounting and cleanup. |

Read-only bind mounts of mutable external sources are insufficient. Lexical path prefixes are insufficient. Export and cleanup must use object-safe traversal, reject unsafe links/special objects, and never expand their scope from target-supplied paths.

### Disposition of deferred probes

| Work | Decision |
|---|---|
| P05 immutable preparation/release | Required implementation prerequisite for integrating a releasable restricted-provider path. Its narrow protocol implementation and validation are part of the first milestone. Full production qualification remains mandatory. |
| P06 filesystem closure/export | Protected copy-in and object binding are implementation prerequisites. Complete hostile-object, replacement, writable-state and export qualification is mandatory before production. Export remains unavailable until its own gate passes. |
| P08 runtime baseline | Required before advertising each runtime closure. Begin with a static native fixture; qualify one complete self-contained .NET closure later. |
| P09 security manifest/package gate | Mandatory before production release, including installed-package consumers and negative evidence controls. |

None of P05/P06/P08/P09 is optional hardening. Closure caching, additional runtime families and performance optimizations are optional later work.

The full no-IP requirement also remains mandatory. The frozen filter permits some io_uring socket paths; P03’s host-endpoint failures do not settle private loopback. A production filter revision, including disabling unneeded io_uring paths, is a legitimate implementation direction but requires explicit review and compatibility/denial qualification. No such revision inherits the frozen filter’s evidence automatically.

## Hufu integration boundary

**Hufu owns:** authority decisions, grants, delegation, policy evaluation, approvals, revocation and the relationship between a principal and an authorized execution.

**Gagamba owns:** concrete isolation, immutable preparation, lifecycle ownership, resource enforcement, prepared channels, safe export, cleanup and execution evidence.

The contract is:

1. Hufu or another trusted caller authorizes a normalized execution intent and bounded preparation access.
2. Gagamba stages and binds the concrete objects, then returns an immutable effective authority/resource manifest and an expiring, single-use preparation.
3. The caller authorizes that exact effective manifest. A changed closure, budget or grant requires a new decision.
4. Gagamba’s release arbiter validates the preparation, authorization binding, cancellation/revocation state and deadline, then releases once.
5. Gagamba returns trusted execution and cleanup evidence.
6. Revocation requests termination of the owned domain and awaits its result. It does not undo disclosed output or exported effects.

Gagamba validates contract structure and enforceability; it does not decide whether a principal deserves a grant. Hufu receives semantic guarantees and refusal reasons without needing cgroup paths, namespace flags or seccomp details.

Use an OS-neutral execution contract in Gagamba’s execution-abstraction layer initially, with no Hufu or workflow dependency. Keep it internal while the first milestone establishes the required shape. `Penghou.Workflow.Abstractions` may later carry a neutral intent/reference or adapter mapping, but should not own the enforcement manifest merely because one consumer uses workflows. Shared extraction requires demonstrated consumer pressure and a separate API review.

Manifests identify authority; they are not themselves bearer grants or proof that enforcement occurred.

## Platform disposition

| Platform | Decision |
|---|---|
| Linux | Approved first implementation baseline. Production support is limited to explicitly qualified host/helper/runtime configurations. |
| Windows native | Refusal-only for the first restricted implementation. P01 eliminated the original LPAC + Job composition. P01B remains inconclusive without candidate construction; no P01C is authorized. Defer further native work. |
| Windows VM | Deferred fallback. Begin qualification only after a concrete Windows requirement and a separately approved VM deployment/ownership/storage/channel design justify the work. Linux progress need not wait for it. |
| macOS | Refusal-only. Earlier lifecycle experiments and launchd qualification establish no restricted-execution qualification. |

Unsupported hosts must refuse before target initialization. No platform may satisfy the required profile by falling back to its ordinary lifecycle provider.

## Bubblewrap dependency and supply chain

Bubblewrap is both **part of the trusted computing base** and **an implementation dependency behind the launcher abstraction**. It is not disposable scaffolding.

Start with the evidence-bound Bubblewrap 0.11.1 artifact:

```text
SHA-256:
523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6
```

This pin identifies the tested artifact; it is not a claim about current upstream suitability or provenance completeness.

Production requirements:

- Record approved upstream source/tag or commit, distribution/build provenance, build configuration and executable dependencies.
- Verify the exact approved binary hash before use through a protected, replacement-resistant binding.
- Use a trusted absolute path; never search caller-controlled `PATH`.
- Prohibit setuid, setgid and file-capability privilege elevation for this baseline.
- Refuse unknown or altered artifacts, missing delegation/controllers, unsupported ABI or unestablished namespace/filter prerequisites.
- Never repair prerequisites by silently installing a privileged helper or changing host security settings.
- Bind helper, filter, adapter and runtime revisions into qualification evidence.
- Treat upgrades as new candidates: review changes, rerun affected security/lifecycle/resource cases, validate installed-package behavior and obtain an updated independent disposition.

A hash authenticates bytes against an approved record; it does not independently establish trustworthy provenance.

## Production evidence model

Promote these concepts into bounded, target-inaccessible production records:

- Execution identity and preparation/release identity.
- Provider/source/package identity and helper/filter revision.
- Complete tool/runtime/input identity.
- Opaque lifecycle-domain identity; private native recovery handles remain internal.
- Requested and established authority/resource manifests.
- Release disposition, stop cause and refusal reason.
- Root result and helper/wrapper result as separate fields.
- Output bounds, overflow, truncation and completeness.
- Domain-stop and cleanup component states, with identified debt.

Do not promote every probe mechanism into telemetry. Positive-control listeners, attack matrices, full process snapshots, test markers, fault-injection scripts and exhaustive diagnostic transcripts remain qualification artifacts.

Production evidence must distinguish `Unsupported`, `FailedToEstablish`, `CancelledBeforeRelease`, execution failure and observation loss. A target assertion or ordinary nonzero exit is not automatically a policy violation.

## Deferred alternatives

| Alternative | Evidence needed to revisit |
|---|---|
| Bespoke Linux launcher | Bubblewrap cannot provide the required pre-init boundary, truthful root status, FD closure or bounded storage without substantial patching; a smaller reviewed replacement demonstrably reduces complexity. |
| Landlock | P05/P06 or later qualification identifies a concrete authority gap or substantially simpler proof; exact required ABI/rights and compatibility are demonstrated. |
| Separate host UID | Measured same-UID control exposure or an accepted deployment requirement justifies provisioning and cleanup costs. A separate UID is additional separation, not a complete sandbox. |
| Privileged daemon/helper | A mandatory guarantee cannot be achieved with operator provisioning and unprivileged execution; a separately approved narrow protocol, authentication, quotas, recovery and upgrade model exists. |
| VM | Required platform coverage or a stronger isolation/availability threat model justifies guest management; host integration, storage, output, lifecycle and export are independently qualified. |
| Stronger LSM integration | A supported deployment can require identified LSM policy, and evidence demonstrates a necessary guarantee or material reduction in attack surface. Unknown AR-7 LSM state supplies no such evidence. |

These alternatives are not automatic fallbacks.

## Implementation authorization

The following work may begin now, bounded by the first milestone:

1. Define internal immutable preparation, manifest, refusal, result and cleanup types.
2. Define the narrow launcher abstraction and Bubblewrap adapter.
3. Implement protected copy-in for one static native tool and ordinary-file inputs.
4. Implement domain allocation, incremental recovery records, pre-release establishment, single-use release arbitration and abort.
5. Implement bounded stdout/stderr readers tied to whole-domain stop.
6. Implement explicit resource installation/readback and writable-state inventory.
7. Implement safe teardown and reconciliation without replay.
8. Add focused P05/P06 and production-topology validation with synthetic fixtures and independent review.
9. Record exact source/helper/filter/configuration evidence and unresolved qualifications.

This authorizes implementation of the selected architecture, not completion of all later work without review. It does not authorize Hufu/workflow changes, a new privileged service, a replacement namespace launcher, Windows/macOS enforcement, VM implementation, public package publication or modification of frozen AR-6 evidence.

## Remaining production qualification gates

Before calling the provider production restricted execution, require:

- Accepted P05 immutable preparation/release and replay/foreign-preparation rejection.
- Accepted P06 complete filesystem, writable-state, object-replacement, export and cleanup qualification.
- Explicit inode/object limits and metadata accounting.
- Streaming output enforcement, overflow, backpressure and retained-descriptor tests.
- Production owner-death, deadline, cancellation and recovery qualification across every state transition.
- Stable domain identity and stale-object safety, beyond PID/starttime checks.
- Complete profile-relevant network/IPC coverage, including intra-execution loopback, inherited transports and applicable alternate syscall paths.
- Resource charging, swap, helper overhead and core-sink qualification for supported deployments.
- Complete P08 identity and usability qualification for every advertised runtime, including the complete published .NET closure.
- P09 exact-source/package/helper/filter/profile binding, negative gate controls and installed-package consumers.
- Explicit non-root delegation, filesystem, kernel/libc/architecture and LSM support conditions for each advertised host configuration.
- Native refusal evidence for unsupported platforms and prerequisites.
- Relevant lifecycle regressions and final independent security review.

A missing mandatory case remains pending or unsupported. It is not a passing restriction.

## First implementation milestone

**Milestone: one internal Linux prepared-execution transaction.**

Implement one noninteractive static native fixture using pinned Bubblewrap, a protected copied input, a fixed explicit resource manifest, bounded stdout/stderr, externally owned cgroup lifecycle and a production-designed pre-initialization release boundary.

Use one execution at a time. Keep stdin closed/EOF. Provide no Hufu integration, workflow integration, arbitrary runtime discovery, reusable shared cache or export API.

The slice must demonstrate:

- Useful execution of exactly the prepared bytes.
- Structural prevention of target initialization before release.
- Refusal on altered bindings, missing restrictions, expired preparations and a stop that wins before release.
- Truthful handling of a release that wins before cancellation.
- Whole-domain stop on timeout, output overflow and owner/manager failures within the declared model.
- Separate root/helper results and conservative cleanup after injected failures.
- Recovery of owned resources without replay or unrelated-object action.

The initial internal slice may retain explicitly unqualified profile properties while implementing and reviewing them. It must not advertise or accept production `restricted-offline-closure-v1` requests until all mandatory gates pass.

## Stop condition

End the milestone when that single native transaction is implemented, its focused evidence is recorded, and an independent reviewer has assessed:

- Preparation/object binding and the pre-init boundary.
- Release/stop linearization.
- Delegated ownership and owner-loss behavior.
- Root/helper/domain result separation.
- Output enforcement and resource installation.
- Cleanup and recovery identity safety.
- The remaining gap between the implementation and the full profile.

Stop earlier for authority widening, released-but-unowned execution, release after a won stop, unbounded writable/output state, unsafe cleanup, or a need for privileged service or substantial launcher replacement.

Proceed to exports, .NET runtime qualification, public API stabilization or Hufu/workflow integration only after this review establishes the next bounded slice. No further feasibility rerun is required merely to revalidate already accepted AR-7 evidence.

## Explicit non-claims

This decision preserves all sealed limitations:

- AR-7 establishes no general Linux-security or production-readiness claim.
- Evidence is scoped to the tested WSL2 host, pinned helper, UID/delegation arrangement and exercised operations; LSM state remains unknown.
- No independently configured inode/metadata ceiling was demonstrated.
- Syscall/IPC coverage is incomplete; intra-execution loopback prohibition was not established.
- Bounded streaming-output enforcement was not demonstrated.
- Approximately 27 MiB .NET startup was an observation, not a measured minimum budget.
- Exhaustive host core-dump/core-sink exclusion was not established.
- Complete published-.NET closure binding and writable-mount qualification remain incomplete.
- P03 ran in the manager leaf; its target-leaf placement/emptiness claims remain withdrawn.
- P04 configuration and accounting qualifications remain, including cumulative memory counters and absent per-case swap-use measurements.
- C7/C8 provide no general root-exit descendant-elimination guarantee.
- R5 retains its chronology qualification: source binding is verified, but its committed wrapper record lacks an independent release timestamp.
- R6 demonstrates fresh identity checks, not atomic immunity to every PID-reuse race.
- Unknown execution results remain unknown despite later confirmed cleanup.
- Superseded attempts remain superseded; unavailable `39d1` and cleanup uncertainty for `a7d8` are not repaired by the final R2 pair.
- P01 does not prove Windows-native execution impossible. P01B remains inconclusive as a composition; no P01C exists.
- Windows VM and macOS restricted execution remain unqualified.
- P05/P06/P08/P09 were unexecuted at the sealed checkpoint.
- The final R2 `/gate` is test instrumentation, not itself a production release mechanism.

AR-7 ARCHITECTURE APPROVED - IMPLEMENTATION MAY PROCEED
