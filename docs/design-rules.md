# Durable design rules

These rules are load-bearing. They were each earned by a measured failure
or a platform difference, and the execution-domain work (`GP-2`/`GP-3`,
`GL-2`, `GM-2`, `GR-0`) depends on them. Change one only with new evidence,
not with a convenience.

## 1. Capabilities describe guarantees, not mechanisms

A capability names an outcome the caller can rely on, never a path to it.
There is no `JobObject`, `Cgroup`, `Launchd`, `Pgid`, or `IsSandboxed` on
the contract surface; those are mechanisms and are mechanically banned by
the opacity tests. Windows, Linux, and macOS grant the same capability
vocabulary from completely different primitives.
See `docs/execution-domain.md`, `execution-domain` capability matrix.

## 2. Required capabilities fail closed; preferred capabilities never gate

`ExecutionRequirements.Required` is a floor: any unmet entry rejects the
preparation with the reason named. `Preferred` is advisory and never turns
an acceptable preparation into a refusal. There is no partial accept and
no silent downgrade.
See `ExecutionNegotiator` in `src/Gagamba.Execution/Negotiation.cs`.

## 3. Native and constructed guarantees remain distinct

A guarantee enforced by the OS (`GuaranteeKind.Native`) is not the same as
one Gagamba composes in userspace (`Constructed`), even when both reach
"Full". Constructed owner-death cleanup remains a separate, explicit layer:
Windows has it natively; the Linux and macOS compositions have materially
different bounds (a macOS watchdog does not repair the `setsid` escape).
Never equate the two, and keep composition out of the native providers.
See `docs/execution-domain.md`.

## 4. Callers never select OS primitives directly

A caller states requirements and consumes opaque handles. It does not open
a Job Object, a cgroup, or a launchd job, and it does not name a provider
class. `Gagamba.Runtime.ExecutionRuntime` selects the native provider for
the current OS; an unsupported OS fails closed rather than no-op.
See `docs/conformance.md` (Provider selection).

## 5. Ambient environment is not authority

The caller's process environment does not cross the boundary. Requested
entries are provided; platform-owned execution-domain variables may
additionally exist when the provider documents them. An explicit
environment is not a minimal environment — the caller must declare the
runtime dependencies of what it launches (the GW-2 hosted-runner finding:
PowerShell 5.1 stalled with only `SystemRoot`), and Gagamba never
reconstructs ambient state on the caller's behalf.
See `docs/execution-domain.md` (environment guarantee).

## 6. Successful API calls are not proof of semantics

Security and lifecycle properties get **behavioral probes**, never
inference from an API returning success:

- GL-2: a wrong `POSIX_SPAWN_SETCGROUP` bit made `posix_spawn` return `0`
  while the child was born outside its cgroup; the provider proves live
  placement before the first real launch.
- GM-2: `bootstrap`/`kickstart` returning `0` does not mean the job runs;
  the provider polls `print` for `state = running` and boots out on failure.
- GM-2 (2026-10-07, consumer pressure): `print` reporting
  `state = not running` does not mean the job runs either; substring
  matching read that as running, so Launch "succeeded" on a never-observed
  job and completion polled forever. Negative state parsing must be
  explicit, and a recorded exit code counts as proof of execution.

When an API's success could be a lie, prove the property.
See `docs/conformance.md` (semantic probing).

## 7. Conformance may legitimately expect different behavior per platform

A conformance leg encodes the *expected* result for each platform, and
those results may differ. macOS must demonstrate that a `setsid` escape
**survives** (escape is observable); Linux must demonstrate the escapee is
**still owned and dies** (`resistant`). Forcing one answer onto both would
turn "portable" into a lowest-common-denominator fiction and would hide a
real containment difference. A hosted environment that cannot exercise a
privileged behavior reports **Skipped with the reason** — never a pass.
See `docs/conformance.md` (matrix) and `src/Gagamba.Conformance`.
