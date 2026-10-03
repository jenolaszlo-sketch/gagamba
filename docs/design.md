# Standalone design draft

Status: working architecture, not a frozen API. Updated 2026-10-03. The [security model](security-model.md) now refines this draft as the offline-process-v1 implementation baseline; use its precise permissions and lifetime semantics.

## Purpose and scope

Gagamba runs a process and its descendants within a caller-selected, enforceable execution boundary. It validates and compiles that policy; the caller decides which policy is authorized. No authorization engine, approvals, grants, workflow IDs, or product-specific integrations belong in the library.

The target platforms are Windows, Linux, and macOS. Begin with local Windows experiments, then Linux in WSL2 and macOS runner probes. Backend selection is explicit and produces diagnostics. Automatic backend selection, if added later, may only choose a backend satisfying the complete policy.

## Threat model to qualify

Assume the target executable, repository files, build scripts, package hooks, environment inputs, and all descendants may be malicious. Trust the host application, Gagamba compiler/launcher, selected enforcement components, and OS kernel. Protect host user data and control services outside the granted boundary, and prevent descendants from acquiring broader rights.

Host administrators/kernel compromise are outside this profile. Same-host attacker modification of resource bindings is a distinct assumption to test or exclude explicitly. In-process plugins in the trusted host are not confined by a sandbox used only for child commands. This is not hostile multi-tenant virtualization or a guarantee against every denial-of-service attack.

The boundary includes filesystem, network, process lifetime, handle inheritance, and accessible IPC. Merely hiding files while allowing access to a host build daemon, Docker socket, SSH agent, or privileged named pipe defeats the intended model.

## Initial policy profile

| Domain | Candidate minimum |
| --- | --- |
| Filesystem | Explicit absolute directory subtree grants, immutable requested policy, inaccessible ungranted host data, private scratch |
| Runtime | Caller-selected runtime/dependency profile expanded into grants; no hidden access expansion |
| Network | Denied networking including host and sandbox loopback, as specified by offline-process-v1 |
| Process | Explicit executable and argument vector, no implicit shell, unrestricted descendants only within the same boundary |
| Environment | Explicit allowlist; no wholesale host inheritance; define required OS variables |
| Handles/IPC | Only explicitly prepared I/O handles/channels; no ambient host service endpoints |
| Lifetime | Single execution tree; no persistent session or background survivors |
| Operational limits | Bounded stdout/stderr, wall-clock deadline, termination acknowledgement |
| Optional OS limits | CPU, memory, process count individually supported or rejected when requested |

Endpoint mediation, unrestricted-network mode, globs, fine-grained executable allowlists, nested deny overrides, live policy changes, persistent sessions, secrets brokerage, and remote backends are deferred. This narrows implementation scope, not requested security constraints: unsupported requests are rejected.

Read-only access is not denial of read. An empty rule set grants no host data access; execution may fail until required runtime paths are explicitly included. A backend with unavoidable ambient access must report the baseline and cannot claim this profile until that access is within the declared contract.

Directory access semantics must be documented before coding: metadata, enumeration, traversal, execution, file creation, deletion, renaming, links, alternate streams, and attributes. Initially reject write-only rules or overlapping modes if the selected profile cannot represent their exact meaning. No implicit conversion of write-only to read/write.

## Preparation and launch contract

Conceptual flow; method/type names are provisional:

```text
Caller policy + runtime profile + launch request
  -> freeze and validate all inputs
  -> discover backend and host prerequisites
  -> compile specific policy
  -> prepared immutable plan + diagnostics + effective grants
  -> create isolated resources and containment unit
  -> verify setup and bind exact executable/environment/working directory
  -> release target execution
  -> observe execution, stop all descendants, drain bounded output
  -> acknowledge termination and cleanup
```

Discovery is advisory. Preparation may fail for a specific path, overlap, OS version, filesystem, or privilege configuration even when a broad capability is reported. Initialization can fail after preparation; it must fail before unconfined target code runs.

Compilation returns an opaque prepared plan or a typed rejection, not just a Boolean. The plan records schema version, backend/version, expanded policy, resource-binding assumptions, and hashes. Lists and maps are deeply frozen; caller mutation must have no effect. A prepared plan must not be usable with a different launch request or expired/changed setup assumptions.

Use explicit executable resolution; account for mutable binaries, interpreters, shebangs and shell quoting. An allowed entrypoint does not attest the behavior of code it loads. Do not add unrestricted `Process.Start` as a fallback.

## Lifecycle and stop

```text
Prepared -> Creating -> Ready -> Running -> Stopping -> Terminated -> Disposed
                     \-> Failed (no target dispatch)
```

Stop may race every launch state. No target may be released after stop has won. Normal root exit still requires draining or terminating remaining descendants before completion. Disposal while running stops the owned unit. Stopping is idempotent and uses an internal cleanup lifetime independent of the already-cancelled caller token.

Containment must survive the launcher crashing. Windows Job Objects and Linux namespace/supervisor or cgroup mechanisms are candidates, not yet proven choices. Enumerating a process tree and killing PIDs is insufficient proof of ownership or containment.

A future public execution handle may expose completion and explicit stop. `IAsyncDisposable` alone is not a sufficient specification for stop acknowledgement and cleanup failure. Keep the initial implementation single-use; persistent sandbox state sharing needs a separate contract later.

Policy is immutable throughout this first profile. Narrowing requires stopping and creating a new sandbox. Stop cannot retract data read, writes completed, or network effects already emitted. Stop failure is a first-class error with evidence, never a successful completion.

## Network and path requirements

Do not infer network confinement from proxy environment variables. Test ordinary direct TCP and UDP, IPv6, DNS, loopback/host services, and inherited connections; raw sockets alone are a weak test because they commonly require privileges already absent.

For later mediation, specify protocol and port matching, hostname normalization, trusted DNS resolution/rebinding behavior, proxy identity, direct-IP access, redirects, listener policy, tunnel bypass, existing connection termination, and allowed endpoint exfiltration implications.

Paths are backend-specific resource bindings, not portable string prefixes. Address non-existing targets, symlinks/junctions, hardlinks, reparse points, mount boundaries, case-sensitive Windows directories, UNC/device paths, short names, alternate streams, and namespace replacement. Reject unsupported path classes. An immutable mount view can still contain mutable file contents; do not imply snapshot consistency or patch preconditions.

## Results and evidence

Distinguish policy invalid/unsupported, backend unavailable, setup failure, launch failure, normal exit, cancellation, timeout, output limit, and termination/cleanup failure. Exit code is optional when no target started or the OS reports another termination form; preserve native details separately.

Only report a policy violation when observed by a trustworthy backend signal. Permission denied inside a program and a nonzero exit are not universal proof. Stream or capture both outputs concurrently with explicit truncation/termination rules and no deadlock on full pipes.

Record sandbox/execution IDs, backend/version, requested/prepared policy hashes, timestamps, termination state, and bounded diagnostics. Command arguments, paths, output, and environment values may contain secrets. Default telemetry must not serialize arbitrary argv or output. Policy hashes identify configuration, not evidence that enforcement worked.

## macOS research boundary

macOS is a target platform. Seatbelt through a small launcher or sandbox-exec is a research candidate; API support, distribution, .NET runtime compatibility, path rules, network denial, and descendant lifetime need independent qualification. Start with a GitHub-hosted runner capability probe. This is process sandboxing, not nested virtualization. Keep it experimental until the supported profile and maintenance strategy are demonstrated. See [test environments](test-environments.md).

## Packaging after spikes

Likely initial projects are `Gagamba.Abstractions`, `Gagamba`, `Gagamba.Windows`, `Gagamba.Linux`, `Gagamba.MacOS`, and a shared conformance harness. Add projects only with useful implementation. No sibling repository references. Decide supported .NET TFMs, native helper packaging, licensing, signing, dependency updates, and supported OS/kernel matrices before release.

All three platform tracks must pass the declared minimum profile before claiming the intended three-platform baseline. Platform-specific experimental previews may precede that milestone with accurately limited claims.
