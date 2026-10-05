# offline-process-v1: security model and minimum profile

Status: implementation baseline selected during authorized preparation, 2026-10-03. GP-0 documentation is complete; this is a contract to test, not evidence that any backend satisfies it. Backend qualification and independent review remain pending. Public types and method names are not frozen.

This profile refines the [design draft](design.md). Changes to its permissions or guarantees require an explicit decision and renewed affected tests. It is intentionally narrow; unsupported requests must fail before target execution.

## Trust and boundary

Trust the caller/host, Gagamba's compiler and launcher, qualified OS enforcement mechanisms and kernel. Treat the target, loaded project code, interpreters, plugins, configuration and every descendant as potentially malicious. Gagamba confines child execution; it does not confine malicious code already executing inside the trusted host.

Protect ungranted host files, host services and credentials, other sandboxes, and the host's execution authority. No process may acquire broader access through descendants, inherited handles, IPC, privilege changes or namespace manipulation. No unrestricted fallback exists.

Excluded: hostile administrator/kernel, kernel exploits, rollback, exact patch preconditions, snapshot-consistent reads, instantaneous revocation, arbitrary executable allowlisting, and complete denial-of-service protection without the associated OS limits. A policy hash identifies configuration and is not proof of confinement.

The initial qualification environment uses disposable workspaces. The trusted host controls the ancestry, mounts, ownership and initial bindings of grant roots during setup/execution. Unrelated hostile same-user processes changing those bindings are outside this initial profile. Target-controlled changes *within* writable roots and descendant attempts to escape remain inside the threat model and must be tested. This distinction must not be presented as general hostile-namespace protection.

## Input and preparation

- The caller supplies explicit absolute executable, argument vector, working directory, directory grants, runtime manifest, environment, I/O limits and deadline.
- One deeply immutable prepared request is bound to one backend/version, launch request and resource bindings. It is single-use. Reusing or substituting inputs rejects before target dispatch.
- Relative paths, globs, write-only grants, explicit deny rules and conflicting overlaps are unsupported. Duplicate identical grants may be collapsed. Same-mode nested grants may be collapsed; different-mode nesting rejects in v1 rather than inventing precedence.
- Roots must exist and resolve to ordinary directories before preparation. Creation of private scratch occurs during trusted setup and before the final immutable plan is produced. The caller can authorize a scratch allocation constraint in advance; the final resolved path and grant must be reported before target release.
- A trusted runtime manifest lists required executable/libraries/configuration and OS resources. It expands into explicit grants and named platform baseline resources, visible in the prepared plan. Gagamba cannot silently add host-home, credential, config or cache access to make a program work.
- Unavoidable OS resource access must be characterized as part of a versioned platform baseline. If it exceeds this profile's protected-resource boundary, the provider is unsupported. Merely listing an overbroad baseline does not make it acceptable.
- Discovery is advisory. Verify actual prerequisites and setup before target release. Unknown schema/enums, unverifiable bindings or setup failure reject without target dispatch.

## Filesystem permissions

| Permission | Meaning in v1 |
| --- | --- |
| ReadOnly subtree | Read bytes, query metadata, enumerate/traverse the subtree, and map/execute readable code subject to OS compatibility; no write/create/delete/rename |
| ReadWrite subtree | ReadOnly plus ordinary file/directory creation, data modification, truncation, deletion and rename wholly within granted writable space, and ordinary attributes needed for those operations |
| Structural ancestors | Only the explicitly reported traversal/metadata support required to reach granted roots; no undeclared directory enumeration or file-content grant |
| Ungranted host data | No content read/write or enumeration; no guarantee that existence/path metadata is concealed beyond the stated structural baseline |
| Scratch | Private fresh writable directory for this execution, with the same confinement rules; disposition/cleanup is reported |

Read permission is not an executable allowlist. Mutable contents in a writable subtree are allowed; no claim is made that only the initial executable's code runs. Read/write is broad mutation authority, not permission for a specific approved edit.

Do not grant changes to host security descriptors, ownership or privileges as ordinary file writes. If a backend cannot prevent authority-expanding metadata changes, it cannot implement this profile. The target cannot alter policy/helper files or weaken the containment mechanism through any granted path.

Use object-aware enforcement, not string-prefix checks. Initial writable files must not alias ungranted host files through hardlinks; enforce a host-controlled input preparation/check and test the backend's handling. A provider must reject unsupported object/path classes rather than accepting a directory and leaving known aliases unchecked.

Initial policy roots cannot themselves traverse unresolved symlinks/junctions, unsupported reparse points or additional mounts. Backend-specific preparation must enumerate/validate initial special entries where required by its enforcement model. A target may attempt to create or replace links in writable space: resulting access must stay within authorized resource bindings or fail. Prevalidation alone does not satisfy this requirement. Mixed read-only/read-write aliases must never upgrade a read-only object; reject a conflicting binding.

UNC/network shares, device paths, alternate data streams and special device files are unsupported initial caller grants. A runtime baseline may explicitly expose minimal OS facilities such as a null device, but cannot turn that into general device access. Alternate spelling, case behavior and short names must not create additional access. New ordinary files inside writable roots are allowed; non-existing grant roots are not.

## Grant preparation (multiple grants, normalization, conflicts)

Multiple explicit grants are allowed; no root is a special singleton.
Before launch, preparation must resolve/canonicalize each grant (fully
qualified, separator-normalized, no trailing separator except roots,
compared case-insensitively on Windows) and collapse exact duplicates
within a kind. The same path in both kinds is a conflicting binding and
rejects in preparation. Nested bindings are permitted with inner-wins
semantics (a narrower inner grant never widens its outer grant); the
provider verifies the effect and reports, never silently widens. An
`ERROR_INVALID_DATA` grant rejection is retried on fresh inputs before
being reported — measured verdicts are tuple-dependent (stable per path
string, mechanism unknown, falsified as a length cap) — and persistent
rejection is reported as unbindable, never dropped or broadened.

## Network and IPC

No IPv4/IPv6 network communication is authorized, including remote destinations, host or sandbox loopback, DNS, outbound connections, inbound listeners and inherited connected sockets. V1 promises no private loopback allowance. A backend that can only isolate external networking while leaving usable loopback must add enforcement or reject this profile.

No host control services, SSH agents, Docker sockets, build daemons, named pipes or Unix/Mach endpoints may supply undeclared authority. Standard I/O uses explicitly prepared pipes/handles, not a general network exception. Required platform runtime IPC must be enumerated in the baseline and demonstrated not to expose arbitrary host execution or protected resources. Intra-tree private IPC may be supported only within the owned boundary.

Ignoring proxy variables must not create connectivity. Test ordinary TCP/UDP and controlled listeners, not only privileged raw sockets. Network denial does not prevent disclosure through the explicitly authorized stdout/stderr channel; the caller is responsible for who receives it.

## Environment, handles and launch

Start from an explicit environment map. Required variables such as temporary-directory/runtime locations are included in the prepared report, not inherited from the host. Reject duplicate ambiguous environment keys using platform semantics. Do not log values by default.

Inherit only the prepared standard I/O channels and verified essential OS resources. No ambient tokens, file descriptors, terminal handles, credentials or external session access. Resolve helpers from host-controlled locations; target-writable PATH and working directory must not select the launcher.

No implicit shell. A shell may be the explicitly requested executable, in which case its commands and descendants remain confined. Bind executable and resource identity against replacement under the stated namespace assumptions; do not claim immutable executable contents in target-writable space.

All containment is installed before target instructions execute. A trusted setup helper may run outside that boundary but cannot execute target-controlled hooks, scripts or arbitrary configuration during setup.

## Lifetime and I/O

One execution owns a process tree, with a kernel-enforced or equivalently qualified ownership mechanism. Enumerating/killing PIDs, process groups alone, and checking only the root PID are insufficient. Descendants must stay restricted even if the parent exits.

Normal root exit initiates stop of remaining descendants. Cancellation, deadline, output overflow and disposal also stop the owned tree. No target release after a stop request has won the launch race. Stop is idempotent, uses its own cleanup lifetime and acknowledges success only after every owned process has terminated. Launcher crash must not leave runnable survivors.

Require finite positive wall-clock and output budgets. Initial fixture defaults: 30 seconds execution time, 1 MiB each stdout/stderr and 64 KiB stdin, with an independent 5-second stop budget. These are test defaults, not frozen public API maxima. Count raw bytes before decoding; overflow triggers stop and an OutputLimit result. Drain both streams concurrently and report truncation. No output spooling beyond declared limits.

If stop cannot be confirmed within its deadline, return TerminationFailed with cleanup evidence and keep the containment state explicitly unresolved. Never report success because the host stopped waiting. The caller/test watchdog must act on that failure. No claim that stopping reverses previous file or output effects.

Private scratch deletion follows confirmed process termination and validates ownership and path bounds; cleanup failure is reported separately. Do not recursively delete a target-controlled path merely because its string matches an expected prefix. No automatic retry of target execution.

## Resource and evidence limits

CPU, memory, process count and disk/scratch quotas are separate capabilities. Requested unsupported limits reject. Mandatory time/output bounds do not imply protection from every fork/memory/disk denial of service; stress fixtures need disposable outer resource limits.

Distinguish policy invalid/unsupported, backend unavailable, setup failure, launch failure, exit, cancellation, timeout, output limit, termination failure and cleanup failure. Exit code may be absent. Only a trustworthy native denial event supports a PolicyViolation classification; generic exit failure does not.

Required qualification: all applicable CF-* cases in [testing and CI](testing-and-ci.md), expanded per the exact profile above, using real providers and reliable positive controls. Windows, Linux, macOS ARM64 and macOS x64 have separate results. No backend currently qualifies.
