# Feasibility review

Reviewed 2026-10-03. Assessment of the supplied proposal, current primary documentation, and the related local projects. No backend execution tests have been run.

## Verdict

The architecture makes sense and a bounded implementation is feasible. The separation between policy selection and OS enforcement, independent library ownership, explicit capabilities, fail-closed compilation, descendant containment, and adversarial tests are sound foundations.

The proposal is not yet implementation-ready. Its largest risks are policy semantics and developer-tool compatibility, not C# interop. A .NET library can control native backends, but a native launcher/helper may be necessary. The API must expose actual guarantees rather than flatten materially different mechanisms into reassuring Boolean flags.

Windows, Linux, and macOS are now target delivery tracks, following the user scope update. Start testing on local Windows; see [test environments](test-environments.md). Their implementations need not use identical primitives, but must implement the same declared policy profile or reject it. No platform is qualified by this review.

## Corrections needed before implementation

| Priority | Proposal sections | Finding | Required change |
| --- | --- | --- | --- |
| Blocking | 6, 18 | Default-deny reads conflict with a read-only host root. Read-only still exposes readable secrets and host data. | Construct a minimal visible filesystem or explicitly authorize every baseline read. Never equate write protection with read isolation. |
| Blocking | 5–7 | Read/write flags leave file versus subtree, traversal, enumeration, execution, create/delete/rename, exclusions, and overlapping rules undefined. | Define a small semantic profile and reject unrepresentable rules. `None` must not silently mean an explicit deny. |
| Blocking | 7, 41 | Canonicalization before launch cannot stop later namespace replacement. Hardlinks and inherited handles also bypass simplistic path reasoning. | Bind resources through the backend's real enforcement mechanism, specify namespace assumptions, and test replacement races. |
| Blocking | 9, 12 | Policy compilation and execution are not bound together. A caller can mutate list contents or change executable/environment after checking. | Freeze inputs and bind the prepared policy, backend identity, launch inputs, and resource bindings to the execution. Revalidate host prerequisites before release. |
| Blocking | 10, 23, 26 | A root PID is not an owned containment unit. Daemons can outlive the root; cancellation can race launch. | Use a kernel-backed owned unit and define normal exit, cancellation, disposal, launcher crash, and cleanup behavior. |
| High | 8 | A hostname allowlist is not a protocol specification or a data-release control. | Define DNS ownership, ports, IPv4/IPv6, TCP/UDP, redirects, proxy bypass, local services, and existing connections. Defer mediation initially. |
| High | 11, 12 | Capability Booleans do not establish that a specific policy works on this machine. | Separate discovery from policy preparation and actual initialization. Report prerequisites, per-limit support, overlap semantics, and unsupported reasons. |
| High | 14–17 | A documented export does not prove API availability, workload compatibility, I/O capture, cleanup, or confinement on a Windows build. | Make all of these spike gates; do not select the production Windows backend yet. |
| High | 25, 42 | Environment filtering alone misses inherited descriptors, config files, IPC, credentials in argv, and secrets in output. | Start from an explicit environment and handle allowlist; deny host control sockets; redact metadata and treat output as sensitive data. |
| High | 26, 27 | Unbounded captured strings and optional wall-clock control permit trivial host resource exhaustion. | Require bounded output and timeout behavior in the first usable runner. Report unsupported OS resource limits honestly. |
| High | 33, 34 | Killing a process is not instantaneous revocation and cannot undo writes or bytes already sent. | Specify stop acknowledgement and an exposure window; avoid claims of atomic revocation. Live changes are deferred. |
| Medium | 43, 44 | A policy hash proves neither enforcement nor file identity. An OS denial is not always observable as a policy violation. | Record versioned requested/prepared policy hashes and execution evidence separately. Only emit violations supported by backend evidence. |

## Filesystem baseline is the central design choice

`dotnet test` needs an executable, runtime libraries, configuration, scratch space, and often package caches and generated build files. A policy that grants only `src` reads and `artifacts` writes may correctly reject the workload. Gagamba must explain required access without automatically widening it.

Use a caller-selected runtime profile that expands into visible, reviewable grants before preparation. A runtime profile is not an implicit exemption. Shared writable caches create cross-execution poisoning risks; begin with private scratch and prepared read-only dependencies. Disable build servers or contain them per sandbox so an existing host daemon cannot execute work outside the boundary.

For the first profile, use exact documented subtree grants and reject conflicting overlaps. Do not promise fine-grained ACL portability. Decide whether read includes metadata/enumeration/traversal and whether write includes deletion/rename. Broader directory mutation rights are materially different from permission to apply one patch.

## Backend feasibility

**Linux:** bubblewrap is a plausible starting mechanism, provided we construct a minimal mount view and a complete policy. Its documentation explicitly makes the caller responsible for the sandbox security model and warns about exposed IPC sockets. This is useful prior art, not an already-complete Gagamba implementation. [Bubblewrap documentation](https://github.com/containers/bubblewrap)

**Windows:** the proposed experimental API is real. Microsoft documents dynamic loading, FlatBuffer input, Windows 11 requirements, and unsupported inherited handles. That last restriction makes stdout/stderr/stdin transport an early feasibility question. The spike must also establish schema acquisition, launch fencing, filesystem behavior, and reliable process-tree lifetime. [Microsoft API documentation](https://learn.microsoft.com/en-us/windows/win32/secauthz/createprocessinsandbox)

OpenAI's Windows engineering account supports the proposal's compatibility concern and describes a dedicated-user/restricted-token/ACL/firewall design. It demonstrates how much operational work can be hidden behind a process-launch abstraction. It does not establish that Gagamba needs to copy that design. [Windows sandbox engineering](https://openai.com/index/building-codex-windows-sandbox/)

Microsoft MXC is additional build-versus-reuse research worth tracking. Its current README explicitly warns that profiles are not yet security boundaries because some generated policies are overly permissive. It must not become a trusted production dependency merely because it wraps similar APIs. [Microsoft MXC](https://github.com/microsoft/mxc)

**macOS:** now in scope; investigate Seatbelt and validate on GitHub macOS runners before claiming support. **OpenShell:** keep deferred. OpenShell documents static filesystem/process controls and dynamic network control. Its policy prover models supported policy properties, not actual runtime enforcement. Neither feature removes the need for Gagamba conformance tests. [OpenShell controls](https://docs.nvidia.com/openshell/latest/security/best-practices), [prover scope](https://github.com/NVIDIA/OpenShell/blob/main/crates/openshell-prover/README.md)

## Original Hufu/Luban question — deferred assessment

Gagamba could supply an ordinary-command execution path for a future Hufu host. It is not a drop-in replacement for Luban's semantic operation admission, target-state checks, release checks, or mutation evidence. For example, granting write access to a directory permits changes beyond a single approved patch; granting read access plus an outbound endpoint permits sending read content there.

The inspected Hufu source currently defines `ReadFile`, `ListDirectory`, `ReadMetadata`, `PatchFile`, `Release`, and `WriteFile`; its evaluator answers concrete requests against a snapshot. It does not expose the proposed `EffectiveAuthority` contract or process/network actions. General contextual authorization rules cannot automatically be reduced to filesystem/network grants. Any future adapter needs an explicitly supported projection profile and rejection for constraints that the execution boundary cannot enforce.

Local evidence inspected: `Penghou.Hufu/src/Penghou.Hufu/AuthorityModel.cs`, `AuthorityRequestAuthorizer.cs`, `AuthorityOperationStart.cs`, Hufu ADRs 0004 and 0011, and both projects' AMLE documentation/READMEs. This records observed working-tree content, not a pinned release audit. Existing atomic start evidence does not imply an atomic transaction with OS launch.

Per the user's subsequent direction, no integration contracts or sibling changes are part of current Gagamba work. Whether to replace any Luban usage remains undecided.

## Recommended next step

Run matched Windows, Linux, and macOS feasibility spikes before freezing the final abstractions, beginning on the local Windows host. A successful spike demonstrates both an ordinary offline developer workload and adversarial denial under the same policy. A successful command alone is insufficient. Then select the minimum shared profile and implement it as an experimental library.
