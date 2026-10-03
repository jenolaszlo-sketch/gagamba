# Gagamba and Penghou.Hufu.Sandbox

## Status

Proposed architecture.

## Purpose

Create a reusable .NET sandbox execution library named **Gagamba** and integrate it with Hufu through a separate **`Penghou.Hufu.Sandbox`** package.

Gagamba is not part of Penghou and must be independently useful.

Gagamba owns:

> Running a process inside an enforceable execution boundary.

Hufu owns:

> Deciding what authority an execution should have.

`Penghou.Hufu.Sandbox` owns:

> Translating Hufu's effective authority into a Gagamba sandbox policy.

The intended architecture is:

```text
Fuwen / application
        |
        v
      Hufu
  authority resolution
        |
        v
EffectiveAuthority
        |
        v
Penghou.Hufu.Sandbox
        |
        v
 Gagamba SandboxPolicy
        |
        v
      Gagamba
   /      |       \
Windows  Linux   OpenShell
        |
        v
restricted process tree
```

Neither Gagamba nor its platform backends may depend on Hufu.

---

# 1. Architectural Principles

## 1.1 Gagamba is not an authorization framework

Gagamba must not contain concepts such as:

- Cedar
- Biscuit
- grants
- delegation
- authority provenance
- workflow
- activity
- plan revision
- human approval
- Hufu revocation records

It receives a concrete execution policy and attempts to enforce it.

Example:

```text
read:
    /workspace/src/**

write:
    /workspace/out/**

network:
    none

command:
    dotnet test
```

Gagamba does not know why that policy was selected.

---

## 1.2 Hufu is not a sandbox

Hufu must not directly implement:

- AppContainer
- Windows restricted tokens
- Windows ACL manipulation
- Landlock
- seccomp
- bubblewrap
- Seatbelt
- Docker isolation
- OpenShell policy files
- operating-system process containment

Hufu calculates effective authority.

Gagamba enforces the portion of that authority that can be represented by the selected sandbox backend.

---

## 1.3 No silent weakening

This is a security invariant.

If Hufu asks for:

```text
network:
    api.nuget.org only
```

and a backend can only provide:

```text
network:
    all or nothing
```

Gagamba must not silently choose:

```text
network:
    all
```

It must report that the requested policy cannot be enforced.

The caller can then:

- select another backend
- deny execution
- request human approval
- recreate the sandbox differently
- explicitly accept a weaker mode

Strict enforcement should be the default.

---

# 2. Research Basis

The backend model needs capability discovery because current sandbox implementations differ materially.

On Linux, current Codex uses bubblewrap as its default filesystem sandbox, with `no_new_privs` and seccomp applied in-process. Its filesystem becomes read-only by default, with explicit writable roots overlaid, and it can isolate networking with a network namespace. Landlock remains a legacy path rather than the primary implementation.

Anthropic's sandbox runtime similarly uses bubblewrap on Linux and Seatbelt on macOS, and applies restrictions to subprocesses spawned by the sandboxed command.

Microsoft now exposes experimental Windows 11 APIs named `Experimental_CreateProcessInSandbox` and `Experimental_CreateProcessAsUserInSandbox`. They use a compiled FlatBuffer sandbox specification and can apply AppContainer filesystem/network isolation, integrity restrictions, Win32k restrictions and Job Object UI limits. Microsoft explicitly marks the APIs experimental.

The Windows API is promising but must not be assumed to solve arbitrary developer workloads. OpenAI's own Windows sandbox work found plain AppContainer unsuitable for open-ended coding-agent workloads involving shells, Git, Python, package managers and arbitrary developer tools. Codex instead built a more complex model involving dedicated sandbox users, restricted tokens, ACLs and Windows Firewall.

OpenShell provides another useful backend model. Its filesystem, Landlock and process policies are fixed when the sandbox starts, while network policy can be updated while the sandbox runs. It also provides formal boundary checking for supported policy domains.

These differences must be visible through Gagamba rather than hidden.

---

# 3. Repository and Package Structure

Gagamba should be a standalone repository.

Suggested structure:

```text
gagamba/

src/
    Gagamba.Abstractions/
    Gagamba/
    Gagamba.Windows/
    Gagamba.Linux/
    Gagamba.MacOS/
    Gagamba.OpenShell/

tests/
    Gagamba.Tests/
    Gagamba.ConformanceTests/
    Gagamba.Windows.Tests/
    Gagamba.Linux.Tests/
```

Initial implementations do not need to include every backend.

Recommended first targets:

```text
Gagamba.Abstractions
Gagamba
Gagamba.Windows
Gagamba.Linux
```

OpenShell and macOS can follow.

Inside Hufu:

```text
Penghou.Hufu.Sandbox
```

Dependencies:

```text
Penghou.Hufu.Sandbox
    -> Penghou.Hufu
    -> Gagamba.Abstractions
```

There must be no dependency in the opposite direction.

---

# 4. Core Gagamba Abstraction

Gagamba should model a sandbox as an execution environment rather than merely a wrapped `Process.Start`.

Suggested API:

```csharp
public interface ISandboxProvider
{
    ValueTask<SandboxCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ISandbox> CreateAsync(
        SandboxCreateRequest request,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface ISandbox : IAsyncDisposable
{
    string Id { get; }

    SandboxCapabilities Capabilities { get; }

    ValueTask<SandboxExecutionResult> ExecuteAsync(
        SandboxExecutionRequest request,
        CancellationToken cancellationToken = default);
}
```

This supports both:

- one process per sandbox
- multiple commands inside the same sandbox

without forcing all platform implementations to behave like containers.

A native implementation may internally make every `ExecuteAsync` a separate process sandbox.

OpenShell may maintain a persistent sandbox environment.

---

# 5. Sandbox Policy

The policy must describe desired effects, not platform-specific mechanisms.

Example:

```csharp
public sealed record SandboxPolicy
{
    public FileSystemSandboxPolicy FileSystem { get; init; }
        = FileSystemSandboxPolicy.DenyAll;

    public NetworkSandboxPolicy Network { get; init; }
        = NetworkSandboxPolicy.DenyAll;

    public ProcessSandboxPolicy Process { get; init; }
        = new();

    public SandboxResourceLimits? Resources { get; init; }
}
```

---

# 6. Filesystem Policy

Suggested model:

```csharp
public sealed record FileSystemSandboxPolicy
{
    public IReadOnlyList<FileSystemRule> Rules { get; init; } = [];
}
```

```csharp
public sealed record FileSystemRule(
    string Path,
    FileSystemAccess Access);
```

```csharp
[Flags]
public enum FileSystemAccess
{
    None  = 0,
    Read  = 1,
    Write = 2
}
```

Example:

```csharp
Rules =
[
    new("/workspace/src", FileSystemAccess.Read),
    new("/workspace/artifacts",
        FileSystemAccess.Read | FileSystemAccess.Write)
];
```

Default:

```text
not explicitly granted -> inaccessible
```

where supported by the backend.

---

# 7. Path Resolution

Path handling is security-sensitive.

Before policy compilation, paths must be:

1. normalized
2. made absolute
3. canonicalized
4. checked for traversal
5. checked for symlink/reparse-point behavior

The policy compiler must not rely only on string prefix checks.

Examples requiring explicit handling:

```text
../secret
/workspace/src/../../secret

symlink:
workspace/src/link -> /etc

Windows junction:
workspace/src/link -> C:\Sensitive
```

Backend conformance tests must cover these cases.

Gagamba should expose canonical paths in diagnostics.

---

# 8. Network Policy

Do not pretend all platforms support hostname allowlists.

Suggested model:

```csharp
public sealed record NetworkSandboxPolicy
{
    public NetworkAccessMode Mode { get; init; }
        = NetworkAccessMode.None;

    public IReadOnlyList<NetworkEndpointRule> Endpoints { get; init; }
        = [];
}
```

```csharp
public enum NetworkAccessMode
{
    None,
    Unrestricted,
    Mediated
}
```

`Mediated` means network traffic must pass through an enforcement mechanism capable of applying endpoint-level restrictions.

Example:

```text
Mediated

allow:
    api.nuget.org:443
    github.com:443
```

A backend supporting only:

```text
network on
network off
```

must reject this policy.

It must not convert it to unrestricted network access.

---

# 9. Process Policy

Gagamba must distinguish between:

1. the process Gagamba launches
2. descendant processes launched by that process

The entry point can be authorized exactly:

```csharp
public sealed record SandboxExecutionRequest
{
    public required string FileName { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public string? WorkingDirectory { get; init; }

    public IReadOnlyDictionary<string, string?> Environment { get; init; }
        = new Dictionary<string, string?>();

    public bool CaptureOutput { get; init; } = true;
}
```

Do not claim that Gagamba can universally enforce:

```text
only dotnet may ever execute
```

because descendant executable restrictions differ significantly between platforms.

Instead expose backend capabilities describing what is actually enforceable.

---

# 10. Descendant Processes

A fundamental invariant is:

> Descendant processes must not escape the sandbox merely because the root process launched them.

This is one of the most important conformance tests.

Example:

```text
Gagamba
   |
   v
bash
   |
   v
python
   |
   v
git
```

`python` and `git` must remain subject to the effective sandbox boundary.

Both current Codex and Claude Code designs explicitly rely on restrictions propagating through the process tree.

---

# 11. Backend Capability Discovery

Introduce:

```csharp
public sealed record SandboxCapabilities
{
    public required string Backend { get; init; }

    public bool FileSystemIsolation { get; init; }

    public bool ReadOnlyPaths { get; init; }

    public bool ReadWritePaths { get; init; }

    public bool NestedPathOverrides { get; init; }

    public bool NetworkIsolation { get; init; }

    public bool NetworkEndpointFiltering { get; init; }

    public bool DynamicNetworkPolicy { get; init; }

    public bool DynamicFileSystemPolicy { get; init; }

    public bool ProcessTreeContainment { get; init; }

    public bool ResourceLimits { get; init; }

    public bool SupportsPersistentSandbox { get; init; }
}
```

The precise structure may be richer than Booleans, but capability discovery is mandatory.

---

# 12. Policy Compilation

Before executing anything, the backend must compile the neutral policy into an enforceable backend representation.

Conceptually:

```text
SandboxPolicy
      |
      v
Backend policy compiler
      |
      +--> fully enforceable
      |
      +--> unsupported
      |
      +--> invalid
```

Suggested result:

```csharp
public sealed record SandboxPolicyCompilationResult
{
    public bool IsEnforceable { get; init; }

    public IReadOnlyList<SandboxPolicyDiagnostic> Diagnostics { get; init; }
        = [];
}
```

Execution must not begin when strict policy compilation fails.

---

# 13. Policy Diagnostics

Diagnostics should be machine-readable.

Example:

```text
GAGAMBA_NET_002

Requested:
network endpoint allowlist

Backend:
Windows.AppContainer

Problem:
backend can provide network isolation but not direct
per-host enforcement without mediated proxying
```

This becomes important to Hufu's future:

> Why was execution denied?

Hufu can report both authority reasoning and enforcement limitations separately.

---

# 14. Windows Backend

Initial backend:

```text
Gagamba.Windows
```

The first spike should investigate:

```text
Experimental_CreateProcessInSandbox
```

rather than immediately recreating Codex's custom Windows architecture.

The Microsoft API currently supports:

- AppContainer isolation
- recursive read-only filesystem roots
- recursive read/write filesystem roots
- integrity levels
- AppContainer capabilities
- network proxy configuration
- Win32k syscall disable
- Job Object UI restrictions

through a FlatBuffer specification passed to `processmodel.dll`.

Interop should use dynamic loading because Microsoft provides no public header and explicitly recommends resolving the API from `processmodel.dll`.

Conceptually:

```text
Gagamba SandboxPolicy
       |
       v
WindowsSandboxPolicyCompiler
       |
       v
SandboxSpec FlatBuffer
       |
       v
Experimental_CreateProcessInSandbox
```

---

# 15. Windows Compatibility Spike

Do not assume that the Microsoft API is sufficient.

OpenAI found standard AppContainer too restrictive for arbitrary coding-agent development workflows and instead implemented a dedicated-user plus restricted-token sandbox.

Therefore the Windows milestone must test real workloads:

```text
dotnet build
dotnet test
git status
git diff
git worktree
cargo build
npm install
python
PowerShell
cmd.exe
MSBuild
NuGet restore
```

Record:

- compatibility failures
- filesystem requirements
- registry requirements
- child process behavior
- package manager behavior
- network behavior
- performance

Only after this spike should the production Windows mechanism be selected.

---

# 16. Alternative Windows Backend

If Microsoft's experimental API proves incompatible with normal developer tooling, investigate a second implementation based on current Windows security primitives.

OpenAI's current Codex implementation is useful prior art:

```text
dedicated sandbox user
       +
restricted token
       +
filesystem ACLs
       +
Windows Firewall
       +
sandbox command runner
```

OpenAI moved to separate online/offline sandbox users so Windows Firewall rules could apply to the actual principal running the descendant process tree.

Do not copy this architecture blindly.

Treat it as evidence of the problems that need to be solved.

Possible future backend names:

```text
WindowsAppContainerSandboxProvider

WindowsRestrictedTokenSandboxProvider
```

Both can implement the same Gagamba abstractions.

---

# 17. Windows Experimental API Isolation

The experimental API must remain behind a capability/version check.

Minimum behavior:

```text
API unavailable
    -> backend unavailable

unsupported SandboxSpec version
    -> backend unavailable

requested feature unsupported
    -> compilation failure
```

Never silently fall back to unrestricted `Process.Start`.

Microsoft currently marks the API experimental and currently requires Windows 11.

---

# 18. Linux Backend

Initial Linux strategy:

```text
bubblewrap
+
no_new_privs
+
seccomp
```

This follows current practical precedent from Codex.

Conceptually:

```text
Gagamba Policy
       |
       v
bubblewrap command builder
       |
       +-- read-only root
       +-- writable mounts
       +-- PID namespace
       +-- network namespace if required
       |
       v
seccomp / no_new_privs
       |
       v
target process
```

Bubblewrap is a sandbox construction toolkit, not a complete security policy by itself, so Gagamba owns correct argument generation and the associated security model.

---

# 19. Linux Landlock

Landlock can remain an additional or fallback mechanism.

The Linux kernel describes Landlock as an unprivileged, stackable access-control mechanism designed to restrict ambient process rights and safely sandbox applications.

However, do not build the first Linux backend around Landlock alone.

Current Codex moved its primary filesystem isolation to bubblewrap and retains Landlock as a legacy path, partly because namespace-based isolation supports cases Landlock does not model cleanly.

Possible later use:

```text
bubblewrap
+
Landlock
+
seccomp
```

as layered defense where useful.

---

# 20. macOS Backend

Proposed package:

```text
Gagamba.MacOS
```

Initial research implementation may use Seatbelt in the same general model currently used by coding-agent runtimes.

Anthropic confirms its sandbox runtime uses macOS Seatbelt, while Linux uses bubblewrap.

Because Seatbelt's public/support status is less attractive for a general-purpose library, macOS should not block Gagamba 1.0.

Treat it initially as:

```text
experimental backend
```

until API stability and distribution implications are understood.

---

# 21. OpenShell Backend

Optional package:

```text
Gagamba.OpenShell
```

OpenShell should be treated as another backend, not as Gagamba's architecture.

It provides:

- managed sandboxes
- filesystem policy
- process identity policy
- Landlock configuration
- network policy
- network middleware
- dynamic network updates
- formal boundary checking

OpenShell is deny-by-default for network access and applies filesystem/process restrictions at sandbox startup.

Its prover can verify that a candidate policy stays within a configured boundary for supported policy domains.

Gagamba may expose those additional abilities through backend capabilities.

---

# 22. Dynamic Policy

Do not promise universal live policy updates.

Represent them as backend capabilities.

Example:

```text
OpenShell:

filesystem change
    -> sandbox recreation required

process identity change
    -> sandbox recreation required

network change
    -> live update possible
```

This is the documented OpenShell behavior today.

Native operating-system backends may be even more static.

---

# 23. Sandbox Lifetime

Gagamba should distinguish:

```text
Sandbox
```

from:

```text
Execution
```

Example:

```text
sandbox S1
    |
    +-- execution E1
    +-- execution E2
    +-- execution E3
```

But providers may advertise:

```text
SupportsPersistentSandbox = false
```

and implement:

```text
sandbox == one process tree
```

This keeps the API compatible with native process sandboxes and OpenShell/container-style environments.

---

# 24. Sandbox Recreation

Provide explicit recreation semantics.

For example:

```csharp
public enum SandboxPolicyMutationSupport
{
    None,
    NetworkOnly,
    Partial,
    Full
}
```

If a policy changes in a way the current sandbox cannot enforce dynamically:

```text
terminate sandbox
      |
      v
create sandbox with new policy
```

Do not modify only the pieces that happen to be mutable while leaving stale authority elsewhere.

---

# 25. Environment and Secrets

Environment variables are part of the sandbox attack surface.

`SandboxExecutionRequest` must allow the caller to specify:

```text
inherit environment?
explicit environment
remove environment entries
```

Default behavior should avoid blindly copying the complete host environment when Gagamba is used for untrusted execution.

Potential secrets include:

```text
API keys
Git credentials
cloud credentials
package registry credentials
SSH configuration
```

Secret brokerage is not a Gagamba 1.0 responsibility.

OpenShell-style external credential injection can be explored later.

---

# 26. Output and Cancellation

Gagamba must support:

```text
stdout capture
stderr capture
exit code
timeout
cancellation
forced termination
process-tree termination
```

Example result:

```csharp
public sealed record SandboxExecutionResult
{
    public required int ExitCode { get; init; }

    public string? StandardOutput { get; init; }

    public string? StandardError { get; init; }

    public required SandboxTerminationReason TerminationReason { get; init; }
}
```

Cancellation must terminate the sandboxed process tree, not only the immediate parent process.

---

# 27. Resource Limits

Resource limits are useful but should not block the initial release.

Future neutral model:

```text
maximum memory
maximum CPU time
maximum wall-clock time
maximum process count
maximum output size
```

Backends advertise which limits they can enforce.

Unsupported limits in strict mode cause policy compilation failure.

---

# 28. Gagamba Security Invariants

### Invariant 1

Unsupported restrictions never silently become broader permissions.

### Invariant 2

Child processes remain within the sandbox boundary.

### Invariant 3

Cancellation terminates the complete owned process tree.

### Invariant 4

Path policy does not depend on unsafe string-prefix comparisons.

### Invariant 5

Backend failure does not fall back to unrestricted execution.

### Invariant 6

Gagamba does not make authorization decisions.

### Invariant 7

Gagamba does not depend on Hufu.

---

# 29. Penghou.Hufu.Sandbox

`Penghou.Hufu.Sandbox` is the integration layer.

Its primary responsibility is:

```text
Hufu EffectiveAuthority
        |
        v
Gagamba SandboxPolicy
```

It should depend on:

```text
Penghou.Hufu
Gagamba.Abstractions
```

It should not contain platform-specific sandbox code.

---

# 30. Hufu Integration Flow

Target flow:

```text
Zhinu / caller
      |
      v
Hufu authorization
      |
      +-- Biscuit validation
      +-- revocation
      +-- Cedar evaluation
      +-- delegation rules
      +-- workflow context
      |
      v
EffectiveAuthority
      |
      v
Penghou.Hufu.Sandbox
      |
      +-- inspect backend capabilities
      +-- project enforceable authority
      +-- reject unmappable constraints
      |
      v
SandboxPolicy
      |
      v
Gagamba
      |
      v
OS enforcement
```

Cedar and Biscuit never need to know Gagamba exists.

Gagamba never needs to know Cedar or Biscuit exist.

---

# 31. Effective Authority Projection

Hufu should expose a neutral resolved authority model.

Example:

```text
filesystem.read:
    ./src/**

filesystem.write:
    ./artifacts/**

network.connect:
    api.nuget.org:443

process.execute:
    dotnet
```

`Penghou.Hufu.Sandbox` converts the enforceable subset into a Gagamba policy.

Example:

```text
Hufu EffectiveAuthority

filesystem.read ./src/**
filesystem.write ./artifacts/**
network.connect api.nuget.org:443

            |
            v

Gagamba SandboxPolicy

filesystem:
    ./src             read
    ./artifacts       read/write

network:
    mediated
    api.nuget.org:443
```

---

# 32. Projection Must Be Conservative

Suppose Hufu calculates:

```text
network:
    api.nuget.org only
```

but the selected Gagamba backend reports:

```text
NetworkIsolation = true
NetworkEndpointFiltering = false
```

`Penghou.Hufu.Sandbox` must not generate:

```text
Network = Unrestricted
```

Instead:

```text
projection failed:
authority cannot be enforced by selected backend
```

This distinction is essential.

Hufu decided that broader access is not authorized.

An implementation limitation cannot broaden that authority.

---

# 33. Authority Epoch

Introduce the concept of an **authority epoch** in the integration layer.

Example:

```text
AuthorityEpoch 17

read ./src
write ./artifacts
```

If Hufu changes the effective authority:

```text
AuthorityEpoch 18

read ./src
write denied
```

the integration determines whether the current sandbox can enforce the change.

If not:

```text
terminate sandbox for epoch 17
create sandbox for epoch 18
```

This gives revocation a precise relationship with sandbox lifetime.

---

# 34. Revocation

Hufu remains the source of truth for revocation.

Gagamba does not query the revocation store.

If authority is revoked while a sandbox is executing:

### Backend supports safe live narrowing

Apply the narrower policy.

### Backend cannot safely narrow

Terminate the sandbox.

Recreate only if execution is subsequently authorized again.

The integration must never leave a known-overprivileged sandbox running merely because recreation is inconvenient.

---

# 35. Sandbox Per Activity

For high-assurance execution, Zhinu may eventually choose:

```text
activity
   |
   v
Hufu EffectiveAuthority
   |
   v
new Gagamba sandbox
   |
   v
activity executes
   |
   v
sandbox destroyed
```

This naturally provides activity-scoped least privilege.

However, Gagamba itself must not depend on Zhinu.

Persistent workflow sandboxes remain possible where performance matters.

---

# 36. Relation to Penghou.Workflow.Abstractions

Workflow authorization and sandbox enforcement remain separate.

```text
Penghou.Workflow.Abstractions
    |
    v
IExecutionAuthorizer
    |
    v
Hufu

then

EffectiveAuthority
    |
    v
Hufu.Sandbox
    |
    v
Gagamba
```

The workflow engine asks whether execution may happen.

Gagamba enforces the resulting runtime boundary.

---

# 37. Relation to Penghou.IO.Abstractions

Keep the IO abstractions.

They remain useful for:

- VFS
- testing
- recording
- WhatIf
- deterministic simulations
- controlled in-process components

But they are no longer the primary sandbox boundary.

Defense in depth can look like:

```text
Agent
  |
  v
Gagamba sandbox
  |
  v
Hufu-aware IO abstraction
  |
  v
filesystem
```

A bug in the IO broker should still meet the sandbox boundary.

---

# 38. Luban

Luban is not required for Gagamba.

Do not require agents to use a proprietary DSL to obtain sandbox protection.

Normal commands should work:

```text
dotnet test
git diff
python build.py
cargo test
```

If Luban returns later, its role should be:

```text
structured effects
WhatIf
replay
provenance
deterministic operation descriptions
```

not OS containment.

---

# 39. WhatIf

Gagamba and VFS solve different problems.

```text
Gagamba:
what can this process actually access?

VFS / WhatIf:
what would this operation change?
```

They may later compose:

```text
Hufu
  |
  v
Gagamba sandbox
  |
  v
agent
  |
  v
VFS
  |
  v
recorded effects
```

No WhatIf functionality is required for Gagamba 1.0.

---

# 40. Conformance Test Suite

Every backend must run the same security-oriented conformance suite where the platform supports the tested feature.

Required tests include:

### Filesystem

```text
allowed read succeeds
denied read fails
allowed write succeeds
denied write fails
parent traversal fails
symlink escape fails
junction/reparse-point escape fails where applicable
```

### Process tree

```text
root process restricted
child process restricted
grandchild process restricted
```

### Network

```text
network denied means raw socket cannot bypass it
allowed endpoint works where filtering is supported
denied endpoint fails where filtering is supported
```

### Termination

```text
cancellation kills root
cancellation kills descendants
timeout kills process tree
```

### Failure

```text
backend initialization failure does not execute command
policy compilation failure does not execute command
unsupported rule does not execute command
```

---

# 41. Security Testing

Tests must attempt adversarial bypasses, not just happy-path permission checks.

Examples:

```text
symlink replacement after validation
junction replacement
relative traversal
alternate path spelling
case differences on Windows
short-name paths on Windows where relevant
child process escape
raw sockets
environment proxy bypass
shell indirection
interpreter indirection
```

TOCTOU behavior should be documented per backend.

Do not claim stronger guarantees than the operating-system mechanism actually provides.

---

# 42. Observability

Every execution should emit structured metadata such as:

```text
sandbox ID
backend
backend version
policy hash
execution ID
command
working directory
start time
end time
exit code
termination reason
policy compilation diagnostics
```

Do not log:

```text
secrets
raw credentials
sensitive environment values
```

`Penghou.Hufu.Sandbox` should additionally correlate:

```text
Hufu decision ID
authority epoch
workflow ID if present
activity ID if present
```

without moving those concepts into Gagamba.

---

# 43. Policy Hashing

Generate a stable hash of the effective sandbox policy.

Example:

```text
SHA-256(canonical SandboxPolicy)
```

This provides a useful provenance link:

```text
Hufu Decision #9281
    |
    v
SandboxPolicy hash ABC123
    |
    v
Gagamba execution #5532
```

This fits naturally with Hufu's existing provenance goals.

---

# 44. Failure Model

Distinguish at least:

```text
PolicyInvalid
PolicyUnsupported
BackendUnavailable
SandboxCreationFailed
ExecutionFailed
PolicyViolation
Cancelled
TimedOut
```

Do not flatten these into generic process failures.

Hufu needs to distinguish:

> Authorization denied the action.

from:

> Authorization allowed it, but the selected platform cannot safely enforce that authority.

Those are different diagnoses.

---

# 45. Initial Roadmap

## Phase 1 - Gagamba.Abstractions

Implement:

```text
SandboxPolicy
SandboxCapabilities
SandboxCreateRequest
SandboxExecutionRequest
SandboxExecutionResult

ISandboxProvider
ISandbox
```

Build backend conformance infrastructure.

No OS implementation yet.

---

## Phase 2 - Windows Research Backend

Implement a spike using:

```text
Experimental_CreateProcessInSandbox
```

Test real developer workloads.

Do not call the backend production-ready until compatibility and escape testing have been completed.

Outcome should explicitly decide:

```text
A. use Microsoft sandbox API

B. supplement it

C. implement restricted-token backend

D. support multiple Windows providers
```

---

## Phase 3 - Linux Backend

Implement:

```text
bubblewrap
+
no_new_privs
+
seccomp
```

Use current Codex/bubblewrap designs as prior art rather than inventing the isolation model from scratch.

---

## Phase 4 - Penghou.Hufu.Sandbox

Implement:

```text
EffectiveAuthority
      ->
SandboxPolicy
```

including:

- capability matching
- conservative projection
- authority epochs
- revocation handling
- diagnostics
- decision correlation

---

## Phase 5 - OpenShell Backend

Explore OpenShell for:

- stronger managed environments
- long-lived autonomous agents
- remote execution
- policy verification
- dynamic network control

---

## Phase 6 - macOS

Add Seatbelt-based backend if its support and distribution model are acceptable.

---

# 46. Non-Goals for 1.0

Do not attempt to solve all of these immediately:

- container orchestration
- MicroVM management
- generic secret brokerage
- arbitrary network protocol inspection
- VFS
- WhatIf
- workflow orchestration
- Cedar integration
- Biscuit integration
- DSL execution
- distributed sandbox fleets
- complete executable allowlisting
- malware analysis
- kernel exploit protection

Gagamba is an execution sandbox library, not a complete hostile-code virtualization platform.

---

# 47. Acceptance Criteria for Gagamba 1.0

1. Gagamba is usable without Penghou or Hufu.
2. Gagamba exposes a backend-neutral sandbox policy.
3. Backends expose their enforcement capabilities.
4. Unsupported restrictions fail closed by default.
5. A backend failure never falls back to unrestricted execution.
6. Descendant processes remain inside the effective boundary.
7. Filesystem permissions are enforced by the underlying OS sandbox mechanism.
8. Network denial cannot be bypassed merely by ignoring proxy environment variables.
9. Cancellation terminates the owned process tree.
10. Policy compilation produces useful diagnostics.
11. Backend conformance tests cover escape attempts.
12. At least one Windows or Linux production-capable backend is available.

---

# 48. Acceptance Criteria for Penghou.Hufu.Sandbox

1. Hufu core has no Gagamba dependency.
2. Gagamba has no Hufu dependency.
3. `Penghou.Hufu.Sandbox` references both.
4. EffectiveAuthority can be conservatively projected into SandboxPolicy.
5. Unsupported authority constraints never become broader sandbox permissions.
6. Hufu decision IDs correlate with sandbox executions.
7. Authority reductions terminate or safely narrow stale sandboxes.
8. Authority expansion requires a new Hufu decision.
9. Cedar remains behind Hufu.
10. Biscuit remains behind Hufu.
11. No sandbox backend interprets Cedar or Biscuit directly.

---

# 49. Final Responsibility Model

```text
Fuwen
"What does the workflow intend to do?"
          |
          v
Hufu
"What authority does this execution have?"
   |                  |
   v                  v
Cedar              Biscuit
policy             delegation
   \                  /
    \                /
     v              v
      EffectiveAuthority
              |
              v
    Penghou.Hufu.Sandbox
"How can this authority be represented
 by the selected sandbox backend?"
              |
              v
           Gagamba
"Create and manage the execution boundary."
              |
      +-------+-------+
      |       |       |
      v       v       v
   Windows  Linux  OpenShell
              |
              v
        Operating system
"Actually enforce the boundary."
```

The key architectural rule is:

> **Hufu decides. Gagamba confines. The operating system enforces.**

`Penghou.Hufu.Sandbox` is the intentionally small bridge between those responsibilities.