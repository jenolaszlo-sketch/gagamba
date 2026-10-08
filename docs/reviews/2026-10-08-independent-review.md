# Gagamba independent security and architecture review

> Archive note (2026-10-08): this is the original review, with source/evidence links made repository-relative. Findings and observed results are unchanged. Line references refer to the recorded review revision. Original runs used a copied source snapshot; archived probe projects now resolve current repository source for reuse. See the [evidence README](2026-10-08-evidence/README.md) before reproducing. The implementation follow-up is the [Sol remediation plan](../review-remediation-sol.md).

Review date: 8 October 2026, Asia/Manila. Specification: the supplied Gagamba Astra Review Specification, sections 1–31.

Source: `C:\Users\Laszlos\source\repos\Gagamba`, clean revision `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88`, version `0.1.0-preview.4`. This is a review of the checked-out source, not a certification of published NuGet binaries. Production source was copied into the attached evidence workspace for builds and probes; production files were not edited. Source hashes, executable probes and test results accompany this report.

The review covers all production contract/provider/runtime source, the conformance runner, provider and contract tests, CI/release configuration, and current architecture/security documentation. Earlier spike and fixture evidence was inspected selectively to distinguish historical experiments from shipped enforcement. Hufu and other sibling implementations were not independently audited. No native macOS run or remote CI dispatch was performed.

## A. Executive assessment

**Verdict: NOT YET A RELIABLE SECURITY BOUNDARY.** The architecture is a useful process-lifecycle substrate and does not need wholesale replacement. It is not currently sufficient to run malicious agent-generated workloads with restricted access to the host. Its production providers do not enforce filesystem grants, network denial, reduced identity, or credential isolation. The experimental Windows sandbox is separate from the shipped Job Object provider. The older offline-process-v1 design is an unqualified design target, not the behavior of the current packages.

The best decisions are the capability vocabulary, explicit required-versus-preferred negotiation, separation of native and constructed guarantees, provider-owned execution domains, and honest acknowledgement of weaker Linux/macOS lifecycle properties. Keeping policy decisions outside Gagamba is correct. The Linux atomic-placement self-test and the documented macOS escape experiment show a valuable willingness to measure OS semantics.

There are also concrete implementation defects in the smaller lifecycle contract. A deterministic Windows probe launched a process **after provider disposal completed**. The Windows asynchronous completion method blocked its caller for the root's entire lifetime. Disposing during a completion wait did not settle that wait. On Linux, cancelling a wait after reaping the root discarded its exit status, and a later wait failed. A child also inherited a deliberately opened host file descriptor. These are observed results, not architectural preferences.

macOS has additional high-risk failure handling: failed observations can be interpreted as a missing job; text is truncated before state parsing; helper output is read before the timeout is enforced; and cleanup failure is discarded. These conclusions follow from source and pure parser probes; native macOS fault-injection remains necessary.

The existing Windows provider suite passed 14/14, the contract suite passed 14/14, and Windows conformance passed its five xUnit assertions. Those results coexist with the defects above. The tests need deterministic lifecycle races and stronger evidence that a fresh workload actually ran and died. Linux/macOS skips must also be separated from qualification success, and package publication must require evidence for the exact revision being published.

Next: repair lifecycle state ownership and completion first; make cleanup uncertainty explicit; close Linux descriptor inheritance; correct the security contract and capability bounds. Before accepting hostile workloads, qualify a concrete least-authority profile that denies ungranted filesystem/network/IPC access. Preserve the current lifecycle layer underneath it. Do not add a policy engine, distributed scheduler, VFS, or containers merely to fill an architectural diagram.

## B. Security boundary assessment

### Actual threat model and guarantees

The current implementation trusts the host caller, OS, executable lookup, working-directory ancestry, and the target's host identity. It is most suitable for cooperative or buggy subprocesses whose lifecycle must be managed. Some deliberate process-tree escapes are resisted; arbitrary hostile host access is not constrained. `Prepare` negotiates lifecycle capabilities and allocates an OS domain. It does not authorize or bind an immutable invocation, because the invocation is supplied later to `Launch`.

The [production SPI (line 13)](../../src/Gagamba.Execution/Provider.cs) contains executable, one argument string, working directory and environment. It contains no filesystem/network/identity policy or limits. Windows launches with ordinary `CreateProcessW`; Linux sets a cgroup spawn attribute without changing credentials/namespaces; macOS launches a job in a user domain. A working directory is a starting location, not a filesystem boundary.

| Property | Classification | What is actually established |
| --- | --- | --- |
| Required lifecycle capability negotiation | Enforced | Required entries reject when the static provider row cannot satisfy them; preferred entries do not gate. This does not establish host readiness by itself. |
| Single launch per preparation | Enforced | Production launch consumes the issuing provider's dictionary entry. |
| Foreign launch/termination handles | Enforced | Provider identity plus issuance lookup; `Discard` is an exception, F12. |
| Direct descendant membership | OS-dependent / partially enforced | Windows job inheritance; Linux cgroup inheritance; macOS process-group relationship only. |
| Atomic entry before target execution | OS-dependent | Windows suspend/assign/resume; Linux clone-into-cgroup path; macOS launchd ownership before kickstart. Concurrent disposal is broken, F03. |
| Owner death cleanup | OS-dependent | Windows last-job-handle closure; unsupported in native Linux/macOS providers. Historical watchdog experiments are not runtime implementations. |
| Arbitrary process escape prevention | Partially enforced | Normal Windows breakaway disabled; broker-mediated work not covered. Linux migration possible with cgroup write authority; macOS setsid escape documented. |
| Filesystem read/write confinement | Unsupported | `..`, absolute paths, symlinks, junctions, hardlinks and mounts are not access-filtered by production providers. Host ACLs still apply. |
| Executable identity and replacement resistance | Caller responsibility | No content binding, immutable closure, handle-based executable binding, or launch-time identity verification. |
| Network denial/selective egress | Unsupported | No provider-added controls for internet, LAN, loopback, DNS, metadata addresses or proxies. Host restrictions may independently deny access. |
| Host IPC isolation | Unsupported | No named-pipe, Unix-socket, SSH-agent, service/broker or desktop isolation policy. |
| Host environment inheritance | Enforced with platform bounds | Windows/Linux construct child environment; macOS supplies requested entries plus launchd session entries. This is not secret isolation. |
| Descriptor/handle inheritance | OS-dependent | Windows uses `bInheritHandles=false`; Linux leaks non-CLOEXEC descriptors, E9. macOS target is launched by launchd; its IPC/session authority remains a separate concern. |
| Credential confidentiality | Unsupported | Host-readable home/config/credential files remain reachable; loaded scripts and tools can read them. No actual credentials were accessed in this audit. |
| Deadline, CPU, memory, process/thread or disk quotas | Unsupported | No production controls. A cancellation token cancels a wait, not execution. |
| Bounded output | Unsupported | No portable capture API; macOS redirects to unbounded temporary files. |
| Escape detection | Detected in tests, not generally detected at runtime | Heartbeat probes characterize chosen escapes. No runtime escape monitor or complete descendant receipt exists. |
| Completion and cleanup proof | Partially enforced | Windows/Linux attempt root-plus-domain completion; concurrency, observation and cleanup defects weaken the evidence. |
| Crash recovery/restart reconciliation | Unsupported in current runtime | In-memory opaque tokens cannot be reattached after owner restart. Linux/macOS jobs can outlive that owner. |

The macOS evidence explicitly records `SSH_AUTH_SOCK` among launchd-supplied variables. Its absence from the caller's environment does not establish absence of agent access. See [GM-2 environment evidence (line 16)](../../docs/evidence/macos-provider-GM-2.md). This is a concrete reason to distinguish “no caller environment inheritance” from “only explicitly granted authority.”

### Boundary trace

| Stage | Trusted data / validation | Authority and partial-failure behavior |
| --- | --- | --- |
| Request | Caller constructs requirements and invocation | No policy engine; correct separation from Hufu. Public read-only collection interfaces do not make their backing data immutable. |
| Configuration | Static capability rows, Linux parent, macOS domain | Linux defaults often lack delegation; macOS defaults to GUI user domain. Static `Describe` is not a readiness report. |
| Preparation | Negotiation, job/cgroup/directory allocation | Linux runs a placement probe. Resources are retained by provider dictionaries; concurrent disposal can miss new allocations. |
| Launch | Consumes preparation; validates executable nonempty and environment shape | Invocation is not bound to preparation. Relative paths/raw argument strings remain platform-dependent. |
| OS dispatch | Job assignment, cgroup placement or launchd bootstrap/kickstart | Target retains host/user-domain authority. Windows has a suspended interval before assignment; host crash in that interval requires a dedicated experiment. |
| Execution | No per-effect mediation | Child can use ordinary host-permitted filesystem/network/IPC resources, shell hooks, interpreters and credential helpers. |
| Descendants | OS membership inheritance | Direct membership is stronger on Windows/Linux; brokers, Linux migration and macOS new sessions are separate escape surfaces. |
| Cancellation/timeout | Explicit `Terminate`; wait token only cancels observer | There is no execution deadline, graceful phase or launch cancellation token. Concurrent termination/completion lacks one shared terminal state. |
| Cleanup | Close job, write cgroup.kill, or bootout; dispose temporary storage | Failures are mostly swallowed. There is no portable cleanup receipt or retryable ownership after some failure paths. |
| Results | Root exit, terminated, or failed with strings | No bounded output, duration, stop cause, limits hit, invocation fingerprint or structured cleanup evidence. |
| Disposal/restart | Dictionaries cleared and native resources released | In-flight launch can repopulate a disposed provider. Restart cannot use the old opaque token to reconcile surviving Linux/macOS work. |

### Escape scope

Gagamba should prevent loss of an already-owned execution through its own lifecycle races, leaked descriptors, or false stop evidence. Direct-child inheritance, double-fork/setsid behavior, and spawn-during-stop must be tested against each advertised capability. Linux migration outside an execution cgroup requires denying the target authority over the controlling hierarchy; cgroups alone cannot do that when the target shares its manager's credentials.

Brokered processes, services, scheduled work, process injection and credential-mediated effects require stronger identity/filesystem/IPC restrictions. They should not be described as prevented by a job or cgroup. A hostile administrator/kernel remains outside the intended threat model. Machine reboot terminates running processes, but no current API reports reboot recovery, residual metadata or macOS plaintext log/plist cleanup.

## C. Findings

Severity follows the supplied S0–S3 definitions. No S0 is assigned: the production design does explicitly acknowledge a lifecycle substrate and weaker platform behavior. The serious error would be deploying it as the stronger offline-process-v1 boundary without qualification. Confidence distinguishes observed code behavior from OS scenarios still requiring experiments.

### F01 — The intended hostile-workload boundary is not implemented by the production packages

- **Severity:** S1 — High. **Confidence:** High. **Area:** Security contract / intended-role readiness.
- **Evidence:** [ProcessStartSpec (line 13)](../../src/Gagamba.Execution/Provider.cs); [Windows dispatch (line 185)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs); [Linux dispatch (line 207)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs); [macOS plist (line 39)](../../src/Gagamba.Execution.MacOS/OwnedJob.cs); [README direction (line 9)](../../README.md).
- **Problem:** The shipped providers constrain process lifetime, while the prominent project description and historical baseline discuss explicit filesystem grants and denied network. No corresponding production enforcement exists. The security-model document does mark itself unqualified, so this is a readiness/contract gap, not evidence of a silently disabled implemented policy.
- **Impact:** A compromised tool can access host-permitted files, credentials and services, and communicate wherever the host allows. An Hufu approval cannot restrict a native child's later effects without a lower enforcement boundary.
- **Recommendation:** Make the shipped scope unambiguous in package README and current docs. Keep lifecycle execution available for trusted tools. Add only a concrete demanded hostile-workload profile, with enforced filesystem/network/IPC/identity restrictions and explicit refusal on unsupported hosts. Do not infer that profile from “all six lifecycle capabilities.”
- **Suggested validation/test:** Synthetic denied-file and local-listener tests with positive controls, including credential-like files, symlink/junction replacement, inherited connected handles and host control sockets. Current lifecycle providers should explicitly report those restrictions unsupported.

### F02 — Windows `EscapeResistant=Full` has a broader wording than its mechanism proves

- **Severity:** S1 — High. **Confidence:** Medium for a host-specific broker escape; High for the contract mismatch. **Area:** Capability honesty.
- **Evidence:** [Windows capability cell (line 39)](../../src/Gagamba.Execution/Platforms.cs) says “no Win32 path leaves the job”; [capability definition (line 20)](../../src/Gagamba.Execution/Capabilities.cs). Microsoft explicitly documents that children created through `Win32_Process.Create` are not associated with the job. [Microsoft Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects).
- **Problem:** Disabling breakaway establishes a direct creation/membership property. It does not prevent a same-identity target from asking an accessible broker to create work outside the job. The current target has no IPC/token restriction to establish that stronger precondition.
- **Impact:** A caller can negotiate Full/native and still launch work that survives job termination through host services. No WMI escape was executed in this audit; broker availability and access are host-specific.
- **Recommendation:** Define this capability precisely as direct descendant membership under named identity/IPC assumptions, or lower its advertised level until a stronger execution profile closes broker access. Update evidence strings and strict-agent examples accordingly. Preserve Windows' real advantage over process groups without claiming complete escape prevention.
- **Suggested validation/test:** Disposable finite broker-created child with a fresh marker; terminate the source job and observe survival independently. Also test `CREATE_BREAKAWAY_FROM_JOB`, nested jobs and attempts to retain job/process handles. Never leave scheduled services/tasks behind.

### F03 — Preparation/launch can race past disposal and leave an unreachable live domain

- **Severity:** S1 — High. **Confidence:** High, demonstrated on Windows. **Area:** Lifecycle concurrency / resource ownership.
- **Evidence:** Windows [Launch (line 58)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), [DisposeAsync (line 158)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), and [post-resume registration (line 240)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs). Linux and macOS use the same consume → launch outside lock → register pattern. Probe E2.
- **Problem:** `_disposed` is checked outside the gate. Launch removes the preparation before native launch and registers the running record afterward. Disposal between those points sees neither resource and returns; launch then inserts into the disposed instance. Prepare has the corresponding check/allocate/register race.
- **Impact:** Shutdown or revocation can finish before a new target starts. Public `Terminate` then throws because the provider is disposed, and repeated disposal is a no-op. E2 returned `Started` with one execution record after disposal had completed; the test explicitly closed its native resources.
- **Recommendation:** Add an internal lifecycle state machine that owns in-flight allocations. Disposal must prevent release or join and terminate every in-flight launch before acknowledging completion. Recheck state under the same synchronization that commits ownership; protect the interval before target release, not only dictionary insertion.
- **Suggested validation/test:** Promote E2 to a deterministic regression, then repeat prepare/launch/dispose and revocation-at-release interleavings on all providers. Assert no target effect after disposal wins and no retained OS domain.

### F04 — Windows asynchronous completion blocks the calling thread until root exit

- **Severity:** S1 — High. **Confidence:** High, demonstrated. **Area:** Async correctness / cancellation usability.
- **Evidence:** [Windows WaitForCompletionAsync (line 116)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), particularly the `WaitForSingleObject(..., 200)` loop before the first await. Probe E1.
- **Problem:** For a live root, the async method executes synchronous waits repeatedly before returning its ValueTask. The cancellation token is checked, but the caller cannot proceed to establish subsequent cancellation or `WhenAny` logic on that thread.
- **Impact:** UI or workflow threads block, cancellation sequences can deadlock, and many activities can consume worker threads. E1 measured **1607 ms inside the call** for a 1500 ms child. Existing tests terminate first or wait after root exit and miss this case.
- **Recommendation:** Use an actual asynchronous wait over a safely owned process handle, or a bounded asynchronous polling loop that yields immediately. Treat WAIT_FAILED and failed exit-code queries as classified observation errors.
- **Suggested validation/test:** A long-lived finite root must return a pending ValueTask promptly; schedule termination from the caller only after that return. Add UI/single-thread synchronization-context and concurrent-wait coverage.

### F05 — Completion state is owned by individual waiters, so cancellation/disposal races lose results or hang

- **Severity:** S1 — High. **Confidence:** High; E3 and E8 demonstrate two consequences. **Area:** Completion, cancellation and native lifetime.
- **Evidence:** [Linux wait/reap (line 132)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs), [Linux Terminate (line 107)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs), [Windows wait (line 116)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), [Windows job query/disposal (line 60)](../../src/Gagamba.Execution.Windows/OwnedJob.cs).
- **Problem:** Linux stores reaped status only in the wait's local variables. Cancelling after root exit but before descendants finish irreversibly loses the status. A retry receives ECHILD and returns Failed. Concurrent Terminate also reaps independently and marks termination afterward. Windows disposal closes native handles while a waiter still uses them; WAIT_FAILED is ignored and a closed-job query can never produce active=0. Multiple waiters can race removal and closure of the same handles.
- **Impact:** E8 cancelled only observation, as the contract permits, then obtained Failed instead of exit 17 on a second wait. E3 settled only when its external token expired after disposal. Other interleavings can misclassify termination, double-close a raw handle or continue polling invalid resources.
- **Recommendation:** Let each execution own one terminal task and persistent root status. Observers await that task with observer-local cancellation. Serialize termination intent, reaping, domain-empty observation and reclamation; use SafeHandle/reference-safe native ownership. Define multiple-waiter and dispose-during-wait behavior explicitly.
- **Suggested validation/test:** Root exits 17 while child remains → cancel wait → wait again; two simultaneous waits; terminate during root reaping; dispose while root runs and while only descendants remain. All outcomes must be bounded and stable, with exactly one resource release.

### F06 — macOS converts observation errors into terminal state and can discard cleanup ownership

- **Severity:** S1 — High. **Confidence:** High for source/parser behavior; native fault outcomes need macOS validation. **Area:** Stop evidence / failure semantics.
- **Evidence:** [Launchd.Run truncation (line 52)](../../src/Gagamba.Execution.MacOS/Launchd.cs), [ParseState (line 100)](../../src/Gagamba.Execution.MacOS/Launchd.cs), [Bootout (line 74)](../../src/Gagamba.Execution.MacOS/OwnedJob.cs), [completion cleanup (line 119)](../../src/Gagamba.Execution.MacOS/MacOsExecutionProvider.cs). Probe E5.
- **Problem:** Any nonzero `print` result becomes “not running”; failed bootout followed by any failed print becomes success. That includes timeout/tool-start/permission failures, not just a specifically absent job. Output is truncated to 2000 characters before parsing. Missing state defaults to false. Completion removes its record and suppresses disposal errors regardless of whether absence was proved.
- **Impact:** Stop can report success without evidence the domain stopped, or a temporary observation failure can cause completion to boot out a still-live workload and consume its handle. Long real output may lose status fields; E5 proves the parser failure using synthetic input, not a claim about every launchd output layout.
- **Recommendation:** Represent Running, Terminal, NotFound and Unknown/ObservationFailed separately. Parse complete bounded structural input and truncate only diagnostic excerpts. Require a recognized absence result before idempotent success. Retain ownership and report cleanup failure until termination is established. Match state values exactly, including unknown states and signal exits.
- **Suggested validation/test:** Inject timeout, permission error, unavailable helper, >2000-character output, unknown state, malformed status and failed bootout. A live job must never become confirmed gone merely because observation failed; exercise same-PG cleanup completion on native macOS.

### F07 — The macOS helper timeout is ineffective while its pipes are being drained

- **Severity:** S1 — High. **Confidence:** High from control flow; native deadlock frequency unmeasured. **Area:** Helper execution / availability.
- **Evidence:** [Launchd.Run (line 28)](../../src/Gagamba.Execution.MacOS/Launchd.cs).
- **Problem:** Stdout and stderr are read synchronously and sequentially to EOF before `WaitForExit(30000)` is called. A hung helper never reaches the timeout; enough stderr while stdout remains open can deadlock. Truncating the accumulated string afterward does not bound allocation. The helper is also resolved as bare `launchctl` through host executable discovery.
- **Impact:** Prepare, Launch, Terminate, completion or disposal can block indefinitely. PATH manipulation in the trusted host's launch environment can select an unintended helper; this is a host-input precondition, not an escape caused solely by a target environment entry.
- **Recommendation:** Resolve `/bin/launchctl` explicitly. Drain both streams concurrently under one deadline spanning start, read and exit; bound retained output while continuing safe draining; kill/reap the helper on timeout. Make helper invocations injectable internally for tests, without exposing native primitives publicly.
- **Suggested validation/test:** Controlled shim that fills stderr, holds stdout open, emits excessive output or never exits. Verify the total operation deadline, bounded retained bytes and helper cleanup.

### F08 — Linux target inherits unrelated host file descriptors

- **Severity:** S1 — High. **Confidence:** High, demonstrated in WSL. **Area:** Authority transfer / IPC / secrets.
- **Evidence:** [Linux spawn file actions (line 207)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs) add only chdir; there is no descriptor allowlist or close-from action. Probe E9.
- **Problem:** Setting CLOEXEC on the cgroup descriptor protects that one descriptor, not other non-CLOEXEC descriptors already open in the host. E9 opened a disposable sentinel with an inheritable native descriptor; the target wrote through fd40 and exited successfully.
- **Impact:** Embedding applications can unintentionally grant open files, IPC channels, connected sockets or other authority despite the explicit child environment. Many managed descriptors may already be CLOEXEC; that does not cover arbitrary native libraries or explicitly inheritable handles.
- **Recommendation:** Construct a child descriptor allowlist, including deliberate stdin/stdout/stderr semantics, and atomically close everything else through suitable spawn actions or a narrowly scoped launcher. Do not “fix” this by changing unrelated host descriptors globally, which races other users.
- **Suggested validation/test:** Open synthetic file, pipe and local connected socket with CLOEXEC off; require them absent in the child and present in the parent. Repeat concurrently with native host descriptor creation and explicit allowed I/O.

### F09 — Linux cleanup suppresses failures, and its owned-cgroup disposal never issues its advertised kill

- **Severity:** S2 — Medium. **Confidence:** High for ordering; host-specific cleanup consequences need native fault injection. **Area:** Resource reclamation / evidence.
- **Evidence:** [OwnedCgroup.Dispose (line 81)](../../src/Gagamba.Execution.Linux/OwnedCgroup.cs), [Kill guard (line 32)](../../src/Gagamba.Execution.Linux/OwnedCgroup.cs), [IsEmpty (line 67)](../../src/Gagamba.Execution.Linux/OwnedCgroup.cs), [provider disposal (line 177)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs). Probe E6.
- **Problem:** Dispose sets `_disposed=true` before calling Kill, which immediately refuses a disposed cgroup. The provider's normal live-disposal path separately calls Kill first, so this is **not** a claim that every ordinary Linux provider disposal omits termination. However, the owned resource's fallback is broken, kill return values are ignored, descendant emptiness is not awaited before removal, and removal failures become permanent silence. `IsEmpty` treats `File.Exists=false` as proof of removal, although access failures can also produce false. [Microsoft File.Exists contract](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.exists).
- **Impact:** Partial failures can leave directories or workloads without reportable cleanup evidence or a retry path. E6 demonstrates the ordering defect with a fake directory only; it does not establish native cgroup survivor behavior.
- **Recommendation:** Kill before marking final disposal, observe subtree emptiness within a bounded budget, remove bottom-up, and retain/report failures. Open/read the expected events file with classified errors; absence and inaccessible are different states.
- **Suggested validation/test:** Native busy descendants, nested groups, delayed termination, denied kill/read/remove, vanished parent and repeated disposal. Assert both no owned processes and exact owned-path removal, or a retained explicit cleanup failure.

### F10 — Missing Linux native prerequisites can throw instead of returning classified refusal

- **Severity:** S2 — Medium. **Confidence:** High from source; unsupported-host execution not performed. **Area:** Platform support / preparation failure.
- **Evidence:** [Prepare placement check (line 35)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs), [VerifyAtomicPlacement (line 276)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs), [native imports/constants (line 15)](../../src/Gagamba.Execution.Linux/NativeMethods.cs).
- **Problem:** GNU-specific imports, especially `posix_spawnattr_setcgroup_np`, are invoked without availability classification. An absent entry point can escape Prepare as an exception after delegation succeeds. `_placementChecked` is set before the check completes and is unsynchronized, so concurrent prepares can see incomplete failure state. Raw 1024-byte buffers and architecture-specific constants also need an explicit qualified-platform contract.
- **Impact:** An unsupported deployment can fail differently from the advertised `PrepareResult.Rejected` path; later calls may return an empty/unhelpful reason. This remains execution refusal, not evidence of an unrestricted launch fallback.
- **Recommendation:** Probe required symbols and supported architecture before allocating/spawning; classify platform absence. Publish required kernel/libc/delegation conditions. Make the placement check a once-only shared result with synchronized publication. Use a small native ABI shim if broader architectures actually become required.
- **Suggested validation/test:** Older glibc, musl, missing clone3 support, undelegated cgroups, concurrent first Prepare calls and controlled native-call failures. No target runs; refusal is stable and diagnostic.

### F11 — Invocation representation and mutable inputs undermine reproducibility and safe authorization binding

- **Severity:** S2 — Medium. **Confidence:** High. **Area:** API / paths / immutability.
- **Evidence:** [ProcessStartSpec (line 13)](../../src/Gagamba.Execution/Provider.cs), [Unix argument parser (line 72)](../../src/Gagamba.Execution.Linux/NativeMethods.cs), its duplicate in [Launchd (line 127)](../../src/Gagamba.Execution.MacOS/Launchd.cs), [Windows launch/environment enumeration (line 185)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), [mutable static profiles (line 27)](../../src/Gagamba.Execution/Platforms.cs). Probe E7.
- **Problem:** A single argument string has different Windows/Unix interpretations. Unix parsing silently accepts unmatched quotes and removes backslashes in cases a caller may intend to preserve. Relative executables and empty cwd select ambient/platform-dependent locations. Read-only dictionary/list interfaces wrap mutable backing objects; Windows/Linux validate environment entries and enumerate again to encode. Static capability dictionaries can be downcast and changed for every caller.
- **Impact:** The launched invocation can differ from a caller's intended or authorized values, and a mutable shared profile can contaminate later negotiation. These are trusted-host integration hazards, not a claim that an out-of-process target can mutate managed dictionaries. Windows' null application-name lookup also relies on parent search behavior. [Microsoft CreateProcessW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw).
- **Recommendation:** Add an argument-vector form with defined Windows serialization; retain raw command-line form only as explicit compatibility behavior. Snapshot validated inputs once, freeze capability maps, validate NUL/path shapes, and require absolute executable/cwd in a security-sensitive profile. Bind the normalized invocation at the Hufu-to-execution seam before authorization; a domain preparation alone is not that binding. Linux should use ordinal environment-name comparison, like macOS, rather than rejecting valid `Path`/`PATH` pairs.
- **Suggested validation/test:** Argument round trips including empty args, quotes, backslashes, Unicode and malformed quoting; mutation during validation/encoding; executable replacement in disposable trees; same-name executable in parent cwd; distinct Linux case-sensitive environment entries.

### F12 — Discard accepts preparations from another instance of the same provider

- **Severity:** S2 — Medium. **Confidence:** High, demonstrated. **Area:** Issuance contract / cleanup.
- **Evidence:** [Windows Discard (line 43)](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs), [Linux Discard (line 69)](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs), [macOS Discard (line 56)](../../src/Gagamba.Execution.MacOS/MacOsExecutionProvider.cs); [SPI promise (line 111)](../../src/Gagamba.Execution/Provider.cs). Probe E4.
- **Problem:** The provider string is checked, then removal of an unknown preparation is treated as idempotent success. A never-issued ID and an already-discarded issued ID are indistinguishable.
- **Impact:** Routing cleanup to the wrong runtime returns Discarded while the original runtime retains its job/cgroup/directory. This is not a foreign launch bypass; Launch does perform issuance lookup.
- **Recommendation:** Give opaque preparations instance identity and an owned terminal state, or an equivalent bounded issuance design, so true idempotence remains distinguishable from foreign input. Do not add an unbounded tombstone dictionary to a long-lived service.
- **Suggested validation/test:** Two instances of each provider; discard A's token on B; forged same-provider token; repeat on A; verify correct classification and resource count.

### F13 — Output and basic workload resource bounds are missing; macOS accumulates hidden logs

- **Severity:** S2 — Medium. **Confidence:** High. **Area:** Resource isolation / usability.
- **Evidence:** [macOS stdout/stderr files (line 59)](../../src/Gagamba.Execution.MacOS/OwnedJob.cs), [public start/result model (line 13)](../../src/Gagamba.Execution/Provider.cs), [Windows job setup (line 24)](../../src/Gagamba.Execution.Windows/OwnedJob.cs).
- **Problem:** The contract has no deadline, output budget or process/memory controls. macOS writes arbitrary output to private-workdir files, exposes none of it as a result, and deletes it during normal completion. Its plist also contains the explicit environment in plaintext; crash cleanup is not implemented. Windows/Linux have no portable capture model.
- **Impact:** A buggy generated command can fill storage or create excessive descendants; a legitimate failing tool is hard to diagnose. Successful wait cancellation leaves that command running by design. Temp naming alone is not a verified permission boundary.
- **Recommendation:** First add an execution orchestration helper for deadline → terminate → observe completion, and bounded output sinks with overflow behavior. For hostile workloads, prioritize process count and memory limits where native mechanisms exist. Make log retention/redaction explicit and use restrictive temp permissions. Keep larger CPU/disk/thread/network quota projects demand-driven.
- **Suggested validation/test:** Bounded synthetic stdout/stderr flood with an outer watchdog; disk/log budget; canceled wait versus execution deadline; host crash with synthetic secret strings in plist/logs; finite spawn burst under a configured process limit.

### F14 — Conformance can reuse stale heartbeat evidence and does not prove the claimed lifecycle transitions

- **Severity:** S2 — Medium. **Confidence:** High from test control flow. **Area:** Test oracle integrity.
- **Evidence:** [ordered shared workspace (line 17)](../../src/Gagamba.Conformance/ConformanceRunner.cs), [UnitTermination / RootExit (line 174)](../../src/Gagamba.Conformance/ConformanceRunner.cs), [Fresh and FrozenAsync (line 387)](../../src/Gagamba.Conformance/ConformanceRunner.cs), [exitroot fixture (line 17)](../../src/Gagamba.Conformance/ConformanceScripts.cs), [fake provider (line 11)](../../tests/Gagamba.Execution.Tests/ProviderTests.cs).
- **Problem:** Sequential legs reuse `root`/`child` filenames. Fresh means mtime less than eight seconds old; the prior leg's dead child can still satisfy the next leg. RootExit then immediately terminates without independently proving root exit. The shell fixture sleeps for one second before exiting, so a fresh heartbeat can come from a root still alive. Frozen timestamps prove no writes, not process death. The fake provider also fails to consume successful launch preparations and has different terminate/completion behavior from production.
- **Impact:** A green conformance row can measure an earlier execution or the wrong lifecycle phase. The fake contract tests cannot be treated as provider conformance. The actual implementations may still behave correctly on a tested happy path; this finding concerns what the oracle establishes.
- **Recommendation:** Use a unique per-leg execution nonce/workspace, require heartbeat advancement after the launch boundary, prove root exit separately, and verify domain-empty/handle evidence plus an independent bounded survivor check. Run reusable SPI scenarios against each real provider, with a small fake only for orchestration logic.
- **Suggested validation/test:** Intentionally broken provider that never starts the second workload, kills root but leaves a blocked non-writing child, or delays root exit. The relevant conformance leg must fail. Add a pure mutation test showing the old fake's reusable preparation is rejected by the shared scenario.

### F15 — Skipped platform behavior and publication are not tied to exact-revision qualification

- **Severity:** S2 — Medium. **Confidence:** High from repository configuration. **Area:** CI / release evidence.
- **Evidence:** [Linux test early return (line 136)](../../tests/Gagamba.Execution.Linux.Tests/ProviderTests.cs), [macOS test early return (line 164)](../../tests/Gagamba.Execution.MacOS.Tests/ProviderTests.cs), [conformance accepts skipped behavior (line 110)](../../tests/Gagamba.Conformance.Tests/ConformanceTests.cs), [publish workflow (line 13)](../../.github/workflows/publish.yml).
- **Problem:** Provider tests return normally when their OS/prerequisites are absent, so xUnit can count them as passes. Conformance records skips but its xUnit assertions pass when the host is unusable. CI does not provision a qualified Linux cgroup/glibc environment. Publishing on `v*` or manual dispatch packs and pushes without an exact-revision test/qualification dependency in the checked-in workflow.
- **Impact:** A green test or publication run need not establish native Linux behavior, and an unqualified revision can reach the package feed. External branch protection or NuGet policy was not inspected; this is the absence of a repository-defined gate, not a claim about every organization control.
- **Recommendation:** Separate exploratory portability builds from mandatory qualified-host jobs. Emit real skipped/unsupported counts and fail qualification when required legs are absent. Require exact-commit Windows/Linux/macOS evidence and source-independent package-consumer tests before publishing the corresponding supported claims.
- **Suggested validation/test:** Run qualification with delegation or launchd disabled and verify it fails; delete one mandatory evidence leg and verify failure; attempt a tag release without matching qualification receipt and verify publication is blocked.

## D. Missed opportunities

These are improvements, separate from the defects above. They do not justify a larger platform.

| Rank | Opportunity | Value | Implementation cost | Architectural leverage |
| --- | --- | --- | --- | --- |
| 1 | Internal per-execution terminal task and state record | Very high: fixes cancellation, reaping, disposal and multiwaiter races together | Medium | High; common lifecycle semantics, provider-specific mechanisms remain separate |
| 2 | Structured execution/cleanup receipt | High: distinguish requested stop from observed domain death and retained cleanup debt | Small–medium | High for Hufu, operators and eventual remote workers |
| 3 | Immutable argument-vector invocation and effective capability snapshot | High: reproducibility, authorization binding and clear platform differences | Medium, additive migration | High without introducing policy evaluation |
| 4 | Small runner helper with deadline and bounded output | High for real agent workloads; fewer caller lifecycle mistakes | Medium | High; orchestration can initially sit above frozen SPI |
| 5 | Preflight readiness report separated from allocating Prepare | Medium–high: explain libc/delegation/domain prerequisites before dispatch | Small–medium | Medium; no new backend abstraction necessary |
| 6 | Explicit reusable execution profiles | Medium: trusted-tool lifecycle profile versus qualified restricted profile | Small once enforcement exists | High; prevents callers from interpreting a capability list as a complete sandbox |
| 7 | Optional disposable workspace utility | Medium for repeatable cleanup and artifact collection | Small–medium | Medium; it must not be labelled filesystem isolation |

Native limits are a practical opportunity after lifecycle repair: process count/memory controls fit existing Job Object/cgroup ownership. CPU budgets are platform-specific and less urgent. General disk quotas, per-thread controls, network accounting and cross-platform resource emulation are more expensive; expose unsupported results rather than approximating them silently. A Windows creation-time job-assignment attribute is worth evaluating to close the pre-assignment host-crash interval, but should follow a focused experiment rather than assumption.

## E. Cross-platform capability matrix

“Advertised” values below are the current source rows, not blanket security approval. Native providers only: the historical Linux PDEATHSIG/watchdog and macOS watchdog compositions are not selected by ExecutionRuntime.

| Capability | Windows | Linux | macOS | Semantic differences and confidence/evidence |
| --- | --- | --- | --- | --- |
| Launch mechanism | Suspended CreateProcess → assign Job → resume | posix_spawn + SETCGROUP | launchd bootstrap → kickstart → print | Source reviewed; Windows/Linux native probes run here; macOS historical evidence only |
| UnitTermination | Advertised Full/native | Advertised Full/native | Partial/native, PG-scoped | Job/cgroup kill members; bootout cannot capture setsid escapees |
| SurvivesRootExit | Full/native; descendants may remain | Full/native; descendants may remain | Full/native label, but launchd normally cleans same-PG remainder | “Owned after root exit” is not “child continues running”; macOS test oracle needs repair |
| OwnerDeathCleanup | Full/native after successful ownership and last handle closure | Absent | Absent | Windows spike J6 evidence; Linux/macOS intentionally outlive caller; launch race F03 applies |
| RecursiveMembership | Full/native for direct descendant inheritance | Full/native by cgroup inheritance | Partial/native | Brokered work/migration are distinct from ordinary inheritance |
| EscapeResistant | Full advertised; wording overbroad, F02 | Partial; setsid retained, migration not prevented | Absent; setsid escapes | E10 measured Linux root-host migration; macOS documented M8 test |
| KernelOwnedLifecycle | Full advertised | Absent | Absent | Kernel-backed kill is not automatic owner-death cleanup |
| Filesystem confinement | Unsupported | Unsupported | Unsupported | Same host/user-domain authority; cwd does not confine |
| Network / IPC confinement | Unsupported | Unsupported | Unsupported | No production allow/deny enforcement or local-service isolation |
| Environment | Explicit block, no caller ambient block | Explicit envp; wrong case-insensitive duplicate rule | Requested entries plus launchd session variables | Session `SSH_AUTH_SOCK` observed historically; do not equate with no credentials |
| Descriptor inheritance | General inheritance disabled | Non-CLOEXEC host descriptors retained | Target created by launchd; not direct host spawn | E9 proves Linux exposure; macOS inherited/session channels need native audit |
| Root/domain completion | Root handle + active process count | waitpid + cgroup.events | print state/exit status + bootout | F04/F05/F06 break some waits/evidence; macOS no independent subtree-empty proof |
| Wait cancellation | Wait only; currently can block caller | Wait only; can lose reaped status | Wait only; helper reads may ignore deadline | Execution cancellation always needs explicit Terminate |
| Output | No portable capture | No portable capture | Unbounded private files, removed at cleanup | Different observable behavior behind the same SPI |
| Resource limits | No configured limits beyond ownership | No configured CPU/memory/pids limits | No configured limits | Native opportunities exist; none currently requested/enforced |
| Provider prerequisites | Job creation/assignment permitted | Writable cgroup v2, compatible glibc/kernel | Accessible launchd user domain | Missing Linux symbols can throw; GUI-domain default is not universally available |
| Cleanup on ordinary disposal | Close job/process handles | Kill, reap root, remove dirs best effort | Bootout and delete temp dir best effort | No structured failure receipt; concurrency and error defects apply |
| Restart/recovery | Job normally dies; tokens cannot reattach | Work may survive; no reconciliation API | Job/temp metadata may survive; no reconciliation API | Historical spike recovery does not establish production restart semantics |

The Linux kernel documents both inherited membership and migration through cgroup control files. The E10 outcome is consistent with that model; denial of migration depends on restricting target authority over the hierarchy. [Linux cgroup v2 documentation](https://www.kernel.org/doc/html/latest/admin-guide/cgroup-v2.html).

## F. Test-gap matrix

| Claim / property | Existing evidence | Missing evidence | Recommended test |
| --- | --- | --- | --- |
| Required capability floors | Pure negotiation tests; 14 contract tests passed here | Mutable/invalid input behavior, actual host readiness | Immutable maps, invalid enums, concurrent first Prepare, unsupported-symbol host |
| No dispatch after disposal/revocation | Single-use happy paths | In-flight ownership | E2 as deterministic regression on all platforms |
| Async completion usable by callers | Exit-code and pre-terminated tests | Live-root prompt return | E1 plus single-thread scheduling |
| Wait cancellation preserves execution/result | Token plumbing | Reaped root + surviving descendants | E8; cancel/retry repeatedly without losing exit 17 |
| Safe completion/terminate/dispose concurrency | Sequential termination and disposal tests | Multiple waiters, invalid handle/query, linearized termination | E3 and barrier-controlled interleavings; assert one terminal result and one close |
| Windows owner-death cleanup | Historical spike J6; direct tree tests | Crash at create/assign/resume/register boundaries; retained job handle | Child with finite self-timeout; crash owner at each barrier; independent observer |
| Windows escape resistance | No breakaway flag; structural conformance attestation | WMI/brokers, parent process attributes, job handle duplication | Finite broker child plus controlled breakaway/nesting probes |
| Linux recursive kill/setsid | Historical native suite; current provider source | Unprivileged delegation, hostile migration, spawn during kill | E10 plus delegated separate-identity setup; bounded spawning with external cleanup |
| Linux no unrelated descriptors | Environment-only tests | Files, pipes and connected sockets | E9 extended to a descriptor allowlist and concurrent native opens |
| macOS completion means stopped | Historical launchd tests; instant-exit regression | Unknown/error/truncated states, signal exits, failed bootout | Inject helper responses; then native bounded jobs and independent liveness checks |
| macOS helper is bounded | Timeout constant | Pipe deadlock, hung CLI, oversized output | Helper shim exercises deadline before EOF and both streams concurrently |
| Cleanup is complete | Windows lock-file checks; Unix frozen heartbeats | Non-writing survivors, permission failures, directory debt | Query actual owned-domain emptiness plus independent finite survivor oracle |
| Root-exit survival semantics | Shared conformance heartbeats | Fresh per-execution identity and proof root actually exited | Unique nonce, root-exit barrier and child acknowledgement before stop |
| Environment is a constructed grant | Explicit/ambient markers | launchd global/session environment, HOME/config hooks, mutable input | Synthetic session variable and agent socket; safe shell startup fixtures |
| Filesystem/network denial | Separate Windows launch spike, unqualified design | Production denial implementation on each claimed platform | Positive/negative sentinel and local listener tests; report unsupported today |
| Resource bounds | None in production | Process/memory/output/deadline behavior | Small finite budget tests with independent outer limits; do not fork-bomb host |
| Native portability | Windows local tests; historical WSL/macOS evidence | Current native macOS rerun, old libc/musl, qualified Linux CI | Required host matrix with explicit support limits and real skips |
| Crash recovery | Historical platform spikes; consumer claims in docs | Production token/resource reconciliation after owner restart | Crash/reopen consumer using isolated worker; classify orphan risk without inventing successful recovery |
| Package release qualification | Build/pack workflow | Exact-source qualification and installed-package behavior | Restore locally packed packages into isolated consumer and bind results to release SHA |

Existing test categories: **unit** (negotiation, parsers, fake provider, evidence validation); **integration/cross-process** (real launches, tree lifetimes, environment); **cross-platform** (shared conformance expectations); **adversarial/security regressions** (setsid, invalid inputs, single use, instant macOS exit); **recovery/destructive probes** (historical owner-death and cleanup spikes). Current production **race/concurrency** and **failure-injection** coverage is the main deficit. Historical filesystem/network denial probes are not production-provider security regressions.

Observed during this review: 14/14 Windows provider tests, 14/14 contract tests, five/five Windows conformance xUnit assertions. Ten focused audit probes ran: E1–E4 Windows behavior, E5–E7 pure code behavior, E8–E10 Linux native behavior. They are bounded demonstrations, not exhaustive qualification. macOS requires native follow-up.

## G. API and usability assessment

The core interface is small and discoverable for lifecycle experts. Required capabilities and refusal values are preferable to a misleading `IsSandboxed` flag. Default empty environment is a sound starting point: keep it constructed/allowlisted, offer documented explicit runtime profiles, and do not return to ambient inheritance merely for convenience. Empty requirements may remain legal for trusted lifecycle usage, but examples must explain that acceptance means only the requested guarantees.

The README currently ends its example after Prepare. A new user still needs to know how to handle start failure, terminate on deadline, await domain completion, reclaim an unused preparation and dispose the runtime. Provide one complete small example, including the critical distinction that wait-token cancellation leaves execution running. After F04/F05 are fixed, a deadline helper can implement that pattern once.

“Opaque” handles avoid OS coupling, but their public records are identifiers, not unforgeable authorization objects. Issuance dictionaries enforce ownership for launch/termination. Hufu must not treat possession of a PreparedExecution as proof that a particular executable/argument/environment combination was authorized. That binding belongs at the trusted invocation seam. Snapshot input once and use the same snapshot for authorization and dispatch.

Current result distinctions are useful but incomplete:

| Question | Current answer | Needed clarification/improvement |
| --- | --- | --- |
| Did the tool fail? | NaturalExit with nonzero root code | Reasonable; caller decides tool success. Linux signal death is encoded as 128+signal, losing signal-vs-exit distinction. |
| Did negotiation/setup fail? | Rejected with string list, sometimes exceptions | Stable stage/reason codes; platform absence should classify consistently. |
| Did Hufu deny policy? | Outside Gagamba | Correct boundary; preserve policy denial separately at integration layer. |
| Was execution cancelled or timed out? | Explicit Terminate, later Terminated | No stop-cause metadata; wait cancellation is not execution cancellation. |
| Did sandbox enforcement fail? | Mixed strings/exceptions | Separate guarantee-not-established from invocation failure and observation uncertainty. |
| Was cleanup complete? | Mostly no answer | Receipt: confirmed, failed or unknown; retain affected ownership for retry. |
| What was actually enforced? | Static Describe plus string `Met` | Immutable per-execution effective report; preferred notes are currently discarded by providers. |
| What happened to output? | Not portable; macOS temp logs discarded | Bounded configurable capture with explicit retention/overflow semantics. |

Do not introduce a large telemetry system. A correlation ID, normalized invocation fingerprint, platform/provider version, negotiated grants, start/end times, stop cause and cleanup status would answer most operator questions. Keep environment values, arguments and output redacted by default. Native identifiers may remain private diagnostic evidence; public opacity does not require throwing away internal proof.

The contract says every method throws after disposal, but production `Describe` continues returning static data whereas the fake test insists it throws. Pick and document one behavior; this is a low-risk consistency fix. Public composed-owner-death examples should also state plainly that the current runtime does not activate those historical compositions.

## H. Architecture improvements

| Current design | Proposed incremental change | Reason | Migration impact |
| --- | --- | --- | --- |
| Three providers duplicate dictionaries and lifecycle transitions | Introduce a small internal execution-state owner or shared state-transition helpers after defining invariants | Concrete repeated bugs justify shared structure; native launch/kill/observe stays provider-specific | Internal first; existing SPI can remain |
| Each wait reaps/polls/closes resources | One per-execution terminal task; observer-local cancellation | Preserve exit status and synchronize terminate/dispose/multiple waiters | No new public method required; define result reuse semantics |
| Raw IntPtr and distributed closure | SafeHandle on Windows, explicit initialized/native-resource scopes elsewhere | Prevent double-close/use-after-close and exception leaks | Internal |
| State inferred from generic helper text/failure | Small launchctl adapter returning classified observations and bounded diagnostics | Keep parsing and transport failures distinct from workload state | Internal; injectable test seam |
| Raw argument string and shallow read-only records | Immutable argument vector, normalized invocation snapshot and frozen maps | Real authorization/reproducibility pressure already exists | Additive overload/record; document compatibility behavior |
| Best-effort cleanup hidden by DisposeAsync | Explicit terminal/cleanup evidence retained per execution | Hufu/workflows need to know whether stopping actually constrained effects | Internal status first; narrowly justified additive receipt later |
| Runtime chooses native lifecycle provider | Preserve selection; add a clearly named restricted profile only when qualified | Separate trusted lifecycle utility from hostile execution boundary | Keep current callers valid; new profile fails closed |

Do not collapse the providers into a fictional universal `IProcessJob`; the platform differences are real. Do consolidate identical argument normalization and invariant checks where their semantics truly match. Avoid an inheritance-heavy provider base class that makes exception/ownership transitions harder to audit.

### Hufu integration

Gagamba remains independently usable: production source has no Hufu/Penghou dependency. Preserve that. Hufu should decide whether a normalized invocation and requested resource authority are allowed, then require an appropriate enforcement profile. Gagamba should establish the OS boundary and return effective restrictions plus completion/cleanup evidence. Revocation must terminate the correct owned execution and await bounded stop evidence; observer cancellation alone is insufficient.

The current capability vocabulary describes lifecycle, so it cannot compare filesystem/network authority requested by Hufu with effective restrictions. Add those capabilities only alongside real enforcement, with explicit unsupported results. A check in an adapter cannot prevent a native process from directly reading a host file. Documentation describing downstream end-to-end tests is supporting context, not independent proof of Hufu revocation/recovery correctness in this review.

### Filesystem architecture

A disposable workspace would improve artifact collection and cleanup for cooperative tools. It would not stop absolute paths, links, mounts, HOME discovery or host services. A managed filesystem broker/VFS helps only when every relevant effect goes through it; arbitrary native executables can bypass a voluntary broker. For the concrete hostile-native-workload use case, qualify an OS-enforced access boundary and protect its launcher/configuration/control files from the target. No overlay, snapshot engine or VFS framework is justified solely by the current lifecycle API.

### Future remote execution

The request/result concept and opaque handles do not prevent a future remote worker. Preserve that option cheaply: separate serializable immutable invocation/profile identifiers from local live handles, use logical workspace/artifact identities at the outer adapter, and return execution/cleanup receipts with correlation IDs. Absolute executable paths are appropriate inside a local worker but should not become global activity identity. Treat native PIDs and provider domains as local diagnostics. Do not serialize a live PreparedExecution token and imply it survives process restart, host migration or a remote retry. No transport, cloud backend or distributed ownership protocol is needed now.

## I. Prioritized roadmap

### Fix before relying on Gagamba as a security boundary

1. **F01/F02:** State the actual lifecycle-only boundary and narrow escape claims. Reject hostile-workload profiles until real filesystem/network/IPC/identity enforcement is qualified. Preserve the existing library for trusted-tool lifecycle use.
2. **F03/F04/F05:** Repair atomic lifecycle ownership, prompt asynchronous wait return, persistent completion state and disposal/termination synchronization. These defects also block dependable trusted-workload orchestration.
3. **F06/F07/F08:** Make macOS observation/termination fail conservatively, bound helper execution, and prevent unintended Linux descriptor transfer.
4. Establish positive cleanup evidence for security-sensitive callers. Do not report “stopped” when only a request was issued or an observer failed.

### Harden next

1. Fix Linux owned-cgroup cleanup and platform prerequisite classification (F09/F10), including one synchronized placement check.
2. Snapshot invocation inputs, freeze capability maps, fix foreign-instance discard and environment case semantics (F11/F12).
3. Correct test oracles and require qualified-host release evidence (F14/F15). Keep exploratory skips visible without accepting them as qualification.
4. Add bounded process/memory controls when the hostile profile requires them; test native mechanisms independently of any outer test sandbox.
5. Run finite owner-crash/startup, broker-escape, cgroup-migration and macOS observation-fault experiments. Choose stronger mechanisms from the resulting evidence.

### Improve developer experience

1. Provide a complete launch/deadline/terminate/wait/discard example and a small orchestration helper.
2. Return structured failure stage, stop cause and cleanup status; preserve preferred-capability notes and a per-execution effective snapshot.
3. Add bounded output with explicit overflow and retention behavior; stop discarding the only diagnostics on macOS.
4. Publish current support prerequisites and separate current execution-domain docs from historical offline-process-v1/spike plans. Remove stale statements about absent providers/workflows and obsolete “next” milestones.

### Future opportunities

Preserve portable invocation/receipt shapes, optional disposable workspaces, and a possible restricted profile. Reconsider constructed owner-death cleanup only for a real consumer and state its weaker guarantees. Preserve remote-worker flexibility without building distribution. Do not add Hufu policy evaluation, Kubernetes, a general VFS, speculative quota emulation or a wholesale provider rewrite.

## Final verdict and requested top-five lists

**NOT YET A RELIABLE SECURITY BOUNDARY.** The lifecycle architecture is fundamentally useful. Its current host authority is too broad for hostile agent execution, and observed lifecycle defects prevent treating even its narrower contract as fully dependable under concurrency and cancellation. The next work is bounded and incremental; architectural replacement is not warranted.

### Top 5 things Gagamba gets right

1. Explicit capability negotiation instead of one “sandboxed” boolean.
2. Honest native-versus-constructed and platform-specific distinctions.
3. Kernel-backed Windows/Linux domain mechanisms and fail-closed launch intent.
4. Constructed environments and separation from higher-level policy engines.
5. Behavioral platform experiments with recorded limitations, including Linux placement and macOS setsid escape.

### Top 5 things that should change

1. Align the advertised security boundary with the actual lifecycle-only implementation.
2. Make launch, completion, cancellation and disposal one coherent resource-ownership state machine.
3. Prevent unrequested descriptor/authority transfer and protect execution-control resources from targets.
4. Treat observation/cleanup failure as uncertainty or failure, never proof of stopped work.
5. Replace weak/stale test oracles and permissive qualification paths with exact-revision behavioral evidence.

### Top 5 experiments/tests that would increase confidence most

1. Deterministic launch-versus-dispose/revocation tests across all three providers, including owner crash at each startup boundary.
2. Cancellation/retry and concurrent wait/terminate/dispose tests that preserve root status and prove one terminal cleanup.
3. A platform escape matrix: Windows broker/breakaway, Linux migration under real delegation, macOS setsid, each with independent finite cleanup.
4. Fault-injected macOS helper transport/parsing/bootout plus native verification that unknown state never becomes confirmed termination.
5. Positive-control denial tests for the proposed restricted profile: host file/credential sentinel, loopback/LAN-like test endpoints, local IPC and inherited descriptors, with bounded resource/output tests.

## Evidence appendix and reproduction

The [evidence manifest](2026-10-08-evidence/evidence.json) records the revision, environments, measured outcomes and limitations. [Source hashes](2026-10-08-evidence/source-sha256.txt) identify the copied input files. The copy intentionally has no Git metadata; builds emitted SourceLink warnings about the missing repository. This affects source-link packaging metadata, not the tested code behavior.

Windows host: x64, SDK 10.0.401, runtime 10.0.12. Native runs were performed outside the Codex process sandbox so its service/process restrictions would not masquerade as Gagamba enforcement. Linux: existing Ubuntu WSL2, kernel 6.6.114.1-microsoft-standard-WSL2, glibc 2.43-2ubuntu2.4, SDK 10.0.112, UID 0. Linux findings are qualified to that environment; unprivileged delegation remains a required test.

From the evidence directory, the Windows probe is `dotnet run --project probe/Probe.csproj -c Release`. It uses copied production source, finite 1.5-second child processes and explicit cleanup for the intentionally reproduced disposal race. The Linux probe is `dotnet run --project linux-probe/LinuxProbe.csproj -c Release` on a compatible Linux host with permission to create its unique cgroup parent. It creates only test-owned resources, keeps the migration test inside that parent, and kills/removes the parent in finally. It is a probe, not production code or a supported deployment recipe.

Probe sources: [Windows and pure-code probes](2026-10-08-evidence/probe/Program.cs), [Linux native probes](2026-10-08-evidence/linux-probe/Program.cs), [Linux captured output](2026-10-08-evidence/linux-probe-results.txt).

Baseline results: [contract TRX](2026-10-08-evidence/test-results/contract.trx), [Windows provider TRX](2026-10-08-evidence/test-results/windows.trx), [Windows conformance TRX](2026-10-08-evidence/test-results/conformance.trx). These suites were rebuilt and run from the source copy. No failures were repaired or hidden to obtain the baseline results.

No host ACL, firewall, identity, scheduled task, service or runtime installation was changed. No real credentials, destructive floods or unrestricted escape processes were used. The finite Linux migration probe was contained by its separate audit parent, whose removal was confirmed. Historical evidence cited in this report is labelled as historical; it was not silently promoted to a new independent measurement.
