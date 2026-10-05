# Execution-domain contract (GP-2)

Status: accepted vocabulary + negotiation rules, extracted from
GW-1B/GQ-1 (Windows), GL-1A (Linux), GM-1A (macOS) evidence. No provider
implementation; the only code is `src/Gagamba.Execution` (types) plus
contract tests. No backend fields, no OS concepts in the public shape.

## Vocabulary

Six capabilities, one per evidenced question:

| Capability | Question it answers |
| --- | --- |
| `UnitTermination` | Terminate the workload as a unit without PID enumeration? |
| `SurvivesRootExit` | Descendants stay owned after the original root exits? |
| `OwnerDeathCleanup` | Workload dies if the Gagamba owner/supervisor disappears? |
| `RecursiveMembership` | Descendants automatically included in the owned domain? |
| `EscapeResistant` | A descendant cannot leave via normal process APIs? |
| `KernelOwnedLifecycle` | The OS itself enforces the full lifecycle relationship? |

Levels: `Absent < Partial < Full`. Partial always names its bound (e.g.
PG-scoped). Kinds: `Native` (intrinsic to the OS primitive),
`Constructed` (composed by Gagamba, e.g. watchdog + kill — weaker by
construction, never equated), `None` (for Absent only).

## Per-platform matrix (weakest demonstrated behavior)

| Capability | Windows Job | Linux cgroup v2 | macOS launchd/PG |
|---|---|---|---|
| UnitTermination | Full/Native | Full/Native | Partial/Native, PG-scoped |
| SurvivesRootExit | Full/Native | Full/Native | Full/Native |
| OwnerDeathCleanup | Full/Native | Absent (compose: Partial/Constructed) | Absent (compose: Partial/Constructed) |
| RecursiveMembership | Full/Native | Full/Native | Partial/Native, no nesting |
| EscapeResistant | Full/Native | Partial/Native, escape path untested | Absent |
| KernelOwnedLifecycle | Full/Native | Absent | Absent |

Composed owner-death differs by platform and is recorded as such: Linux
pairs a strong recursive primitive underneath, macOS only PG kill while
escapees survive. Same verdict shape, different strength, never merged.

## Negotiation

An activity declares requirements: capability + minimum level +
whether constructed guarantees count, each with a reason. Preferred
capabilities ride along as advisory notes.

- Accept iff every requirement is satisfied (Full satisfies a Partial
  minimum; constructed counts only when allowed).
- Reject otherwise, naming every unmet requirement with platform,
  floor, and actual grant. No silent downgrade, no best-effort
  substitution.
- Empty requirements accept (nothing demanded).

Example: unit-termination + survive-root-exit + escape-resistant at
Full/native accepts on Windows, rejects on Linux (escape Partial) and
macOS (escape Absent, unit Partial). Owner-death at Partial+constructed
accepts on Linux and macOS on visibly different grounds.

## Hard rules

- **No boolean called `IsSandboxed`** (mechanically enforced by
  contract tests): a workload can be well-owned without a security
  boundary and vice versa; one bit would lie about both. The rule bans
  misleading sandbox booleans, not the word (`SandboxPolicy` stays legal).
- Matrix cells cite evidence; a cell without evidence is Absent.
- Partial grants name their bound in the evidence string.
- Constructed grants name their composition in the evidence string.

## Provider SPI (`IExecutionProvider`)

Negotiation → preparation → launch → lifecycle control, then async
disposal. An activity declares `ExecutionRequirements`
(required + preferred); `Prepare` returns an opaque `PreparedExecution`
or classified refusal. `Launch` takes the preparation plus a
`ProcessStartSpec` (executable, arguments, working directory,
environment — itself a granted resource, never inherited) and returns an
opaque `ExecutionHandle`. `Terminate` uses the domain primitive, never
PID lists. Handles expose no PIDs, native handles, cgroup paths, PGIDs,
or job objects (mechanically enforced). Providers throw
`ObjectDisposedException` once disposed; root exit never invalidates a
handle while descendants remain. Only preparations a provider issued
itself are honored.

Environment guarantee, stated honestly per platform: the spec's variables
are always present and the launcher's ambient environment is never
inherited (Windows explicit block; Linux `envp`; macOS plist
`EnvironmentVariables` — proven by GM-2). Exact set equality with the spec
is Native on Windows/Linux; on macOS launchd additionally injects OS
session variables (HOME, PATH, TMPDIR, XPC_*, …) that are supplied by the
domain owner, not inherited. That delta is a platform property; the
contract requires presence of the granted set and absence of ambient
inheritance, not byte-identical environments.
