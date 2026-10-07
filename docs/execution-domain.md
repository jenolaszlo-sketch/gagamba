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

Environment guarantee (portable, three parts): **caller ambient state is
not implicitly inherited; requested environment entries are provided;
platform-owned execution-domain variables may additionally exist when the
provider documents them.** This is what all three providers guarantee.
Windows and Linux can additionally advertise exact-set equality (they pass
exactly the declared block/`envp`); macOS cannot, because launchd injects
OS session variables (`HOME`, `PATH`, `TMPDIR`, `XPC_*`, …) that belong to
the domain and are not inherited. The portable contract deliberately stops
at the three-part guarantee; byte-exactness is a stronger, separate
property a consumer may require later. An explicit environment is not a
minimal environment: the caller declares the runtime dependencies of what
it launches (the GW-2 runner finding), and Gagamba never reconstructs
ambient state on its behalf.

## Frozen

The `IExecutionProvider` SPI (GP-3) is **frozen**. Three materially
different implementations (Windows Job Objects, Linux cgroup v2, macOS
launchd) exercised it without any signature change. It will not be extended
unless a downstream consumer proves something is missing; new platform
differences are expressed through already-frozen mechanisms (capability
grants, classified refusal), never by widening the SPI.

**Amendment 1 — `Discard(PreparedExecution)`** (the first, and so far only,
exception, justified by a real downstream consumer). The Hufu →
Gagamba integration (`Penghou.Hufu.Sandbox`) exposed a lifecycle hole: a
provider may allocate domain resources at `Prepare`, and an
authorization-revoked or cancelled launch leaves that preparation
unreclaimed until provider disposal. `Discard` is provider-owned,
single-use, safe before launch, idempotent, releases resources allocated by
`Prepare`, and fails closed for a foreign preparation. Providers now
allocate the domain at `Prepare` and reclaim it either at `Launch` or at
`Discard`. This is not speculative growth; it is the missing state the
consumer proved. `launch → terminate → dispose` is unchanged.

**Amendment 2 — `WaitForCompletionAsync(ExecutionHandle, CancellationToken)`**
(second exception, same doctrine: the workflow consumer HZ-1 proved a missing
state). The SPI could launch, discard and terminate but never observe a domain
reaching its terminal state, so a durable workflow could not await a process
nor distinguish natural exit from termination. Completion means the root
invocation terminated **and** the provider-controlled domain is empty — not
merely that the root PID exited, because a domain deliberately outlives its
root. The result is `NaturalExit(RootExitCode)` (any code, including non-zero,
is a natural exit), `Terminated` (no portable exit code), or `Failed`
(foreign/stale handle). The token cancels the wait only and never terminates
the execution; callers terminate explicitly via `Terminate`. Completion
consumes the handle's provider resources (like `Discard`), so it cannot leak.
Per-provider terminal state: Windows waits for the root exit code and the Job
reaching zero active processes; Linux reaps the root and requires the
execution cgroup to become unpopulated; macOS observes launchd job termination
and its recorded exit status (an escaped `setsid()` descendant is outside the
native domain and does not block completion).

**Provider correctness fix, `0.1.0-preview.4` (not an amendment).** The SPI
above is unchanged and remains frozen; preview.4 is the current provider
implementation. It fixes one proven defect, exposed by the first real
consumer (`Penghou.Hufu.SandboxRunner`, 2026-10-07) rather than by
unit/conformance tests: launchd reports instantly-exited on-demand jobs as
`state = not running`, and substring matching misread that as running, so
Launch "succeeded" on a never-observed job and completion polled forever.
State comparison is now negation-aware (`running` without the `not running`
negation), and a recorded exit code counts as proof of execution at Launch,
so instant commands complete deterministically instead of racing the first
readiness poll. This was a bug fix, not Amendment 3: no signature changed,
no capability was added, and no new infrastructure was justified.

**Consumer-1 checkpoint (durable conclusions).** Real consumer #1 is
continuously exercised on Windows, macOS, and capability-qualified Linux; it
exposed the defect above and required zero architectural expansion:

- Consumer tests belong in CI. Unit/conformance tests had not exposed the
  instant-exit launchd race; only the assembled consumer did.
- Provider acceptance is a semantic claim. Once `Prepare` accepts,
  unexpected execution failure is a test failure, never something to skip;
  only classified pre-acceptance refusal (unsatisfiable negotiation,
  unpreparable domain) may skip, with its exact reason.
- Negative state parsing must be explicit. `not running` matching
  `running` joins the earlier glibc constant failures as evidence for the
  semantic-probing doctrine (rule 6): successful API calls are not proof.
