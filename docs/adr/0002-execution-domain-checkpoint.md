# ADR 0002: Execution-domain architectural checkpoint

Status: accepted (architectural checkpoint; `IExecutionProvider` frozen).
Date: 2026-10-05.
Milestone commit: `c5d6f3f`. Tag: `arch-execution-domain-v1`.
Amendment 1 commit: `cbabf56`. Tag: `arch-execution-domain-amendment-1`. Package: `0.1.0-preview.2`.
Durable rules: `docs/design-rules.md`. Conformance: `docs/conformance.md`.

## Context

The execution-domain work reached a coherent milestone:

- **GP-2** extracted platform capabilities (guarantees, not mechanisms)
  from GL-1A/GM-1A/GQ-1 evidence: `Absent`/`Partial`/`Full` ×
  `Native`/`Constructed`/`None`, required/preferred negotiation, fail-closed.
- **GP-3 (`IExecutionProvider`)** defined the SPI: Describe / Prepare /
  Launch / Terminate / DisposeAsync, opaque handles, issuance validation.
- Three genuinely different implementations exercised it with **zero
  signature changes**:
  - Windows — Job Objects (`Gagamba.Execution.Windows`), evidence GW-2/GQ-1.
  - Linux — cgroup v2 with atomic `CLONE_INTO_CGROUP` placement
    (`Gagamba.Execution.Linux`), evidence GL-2.
  - macOS — launchd ownership + process groups with a visible escape
    limitation (`Gagamba.Execution.MacOS`), evidence GM-2.
- **GR-0** added one conformance matrix + runner over the frozen SPI
  (`Gagamba.Conformance`, run per OS in CI) and a requirement-driven runtime
  (`Gagamba.Runtime.ExecutionRuntime`): `requirements → capability
  negotiation → native provider → evidenced guarantee`, with no OS
  primitive or provider class in caller code.

Conformance preserves platform differences instead of hiding them: macOS
must demonstrate that a `setsid` escape survives, Linux must demonstrate
the escapee is still owned and dies, and a hosted environment that cannot
exercise a privileged behavior reports Skipped with the reason.

## Decision

1. **Freeze GP-3.** No extension to `IExecutionProvider` unless a
   downstream consumer proves a missing concept with evidence. New platform
   differences are expressed through already-frozen mechanisms (capability
   grants, classified refusal), never by widening the SPI.
2. **Adopt the durable design rules** in `docs/design-rules.md` as
   load-bearing.
3. **Add no features speculatively.** Explicitly out of scope until a real
   consumer demands them: quotas, watchdogs/owner-death composition, output
   capture, richer isolation, resource policy, and VFS integration.
4. **Keep constructed guarantees out of native providers** and out of the
   runtime; composition is a later, explicit layer.
5. **Seek the next pressure from a downstream integration**, not from
   another round of interface design.

## Out of scope (until demanded)

Owner-death composition (Linux/macOS watchdog), quotas and resource policy,
stdout/stderr capture, richer sandboxing, and VFS/overlay integration.
Windows owner-death cleanup already exists natively and is unaffected.

## Amendment 1: `Discard(PreparedExecution)`

The first downstream consumer (`Penghou.Hufu.Sandbox`, the Hufu → Gagamba
integration, milestone HG-1) proved a missing lifecycle state: a provider may
allocate domain resources at `Prepare`, and a launch that is revoked or
cancelled before it runs leaves that preparation unreclaimed until provider
disposal. GP-3 gained `IExecutionProvider.Discard` — provider-owned,
single-use, safe before launch, idempotent, foreign-preparation fail-closed.
The providers now allocate the domain at `Prepare` and reclaim it at `Launch`
or `Discard`; `launch → terminate → dispose` is unchanged. This is the
narrowest justified amendment to a frozen SPI, and it is recorded as such.
`arch-execution-domain-v1` (the pre-amendment checkpoint) plus Amendment 1 is
the frozen execution-authority milestone.

## Next pressure

HG-1 is complete: an authorized workflow-less activity launches through
`ExecutionRuntime`, authority is revalidated at launch, revocation terminates
the domain, and every confirmed model survived contact without distorting
either system. The next pressure is **workflow-side (HZ-1: workflow-authorized
execution)**: a durable workflow activity consumes `Penghou.Hufu.Sandbox` so
identity/authority/revision survive orchestration, retries, cancellation and
recovery. No new sandbox capability is added for it.

## Consequences

- The public SPI and the capability vocabulary are stable; consumers may
  depend on them.
- Conformance must keep measuring behavioral properties on every claimed
  OS build; semantic probing is permanent doctrine.
- The package is a defensible execution substrate, not merely a process
  launcher: capability-negotiated, fail-closed, and behaviorally verified.
