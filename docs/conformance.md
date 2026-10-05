# Execution provider conformance

One capability-negotiated execution-domain SPI (`IExecutionProvider`,
`Gagamba.Execution`, **frozen**) is implemented three times and validated
against the same shared matrix:

- Windows — `Gagamba.Execution.Windows` (Job Objects), evidence
  [GW-2 slice4](evidence/windows-slice4-GW-1B.md), [GQ-1](evidence/windows-job-ownership-GQ-1.md).
- Linux — `Gagamba.Execution.Linux` (cgroup v2), evidence
  [GL-2](evidence/linux-provider-GL-2.md).
- macOS — `Gagamba.Execution.MacOS` (launchd + process groups), evidence
  [GM-2](evidence/macos-provider-GM-2.md).

`Gagamba.Conformance` holds the **expected matrix** and one runner that
measures a provider against it; `tests/Gagamba.Conformance.Tests` runs the
runner for the current OS. A provider that drifts from the matrix fails
loudly instead of silently redefining its guarantee.

## Matrix

| Leg | Windows | Linux | macOS |
|---|---|---|---|
| Capability matrix | PASS | PASS | PASS |
| Opaque handles | PASS | PASS | PASS |
| Prepare (negotiation) | PASS | PASS | PASS |
| Single-use preparation | PASS | PASS | PASS |
| Working directory | PASS | PASS | PASS |
| No ambient inheritance | PASS | PASS | PASS |
| Unit termination | PASS | PASS | PASS |
| Root-exit survivor termination | PASS | PASS | PASS |
| Dispose cleanup | PASS | PASS | PASS |
| `setsid` escape | resistant | resistant* | observed |

Capability levels behind the behavioral rows:

- `UnitTermination`: **Full** (Windows/Linux), **Partial** (macOS — PG
  scoped, no subtree primitive).
- `SurvivesRootExit`: **Full** on all three.
- `EscapeResistant`: **Full** (Windows), **Partial** (Linux), **Absent**
  (macOS).
- `OwnerDeathCleanup`: native **Full** on Windows only; composed later on
  Linux/macOS as a separate layer, never equated with native.

\* Linux is honestly **Partial**: `setsid`/session escape stays inside the
cgroup (so the escapee is still owned and dies), but broader
`cgroup.procs`-migration resistance is untested and not claimed.

The `setsid` escape row is the one intentional cross-platform difference.
The correct macOS result is **observed**, not killed: the same-process-group
workload dies with the job, while a process that leaves the group survives.
That is a platform fact (GM-1A M8), not a defect, and conformance asserts
it rather than papering over it.

## Provider selection and requirement-driven launch

`Gagamba.Runtime.ExecutionRuntime` closes the loop the caller actually
wants:

```text
requirements -> capability negotiation -> native provider -> evidenced guarantee
```

The caller asks for guarantees, not classes:

```csharp
await using var runtime = ExecutionRuntime.Create(); // picks the OS provider
var prep = runtime.Prepare(new ExecutionRequirements(new[]
{
    ExecutionRequirement.Require(ExecutionCapability.UnitTermination),
    ExecutionRequirement.Require(ExecutionCapability.SurvivesRootExit),
}));
// Accepted -> launch/terminate through the same runtime;
// Rejected -> reasons name exactly which guarantee could not be met.
```

Callers (Hufu/Fuwen, or anything else) never reference a Job Object, a
cgroup, launchd, or a provider class. On an unsupported OS the runtime
still exists and every `Prepare` is an explicit refusal, never a silent
no-op. Constructed owner-death cleanup stays out of the runtime and the
native providers: its Linux and macOS bounds differ materially (a macOS
watchdog does not repair the `setsid` escape), so it is a later, explicit
composition.

## Semantic probing (permanent doctrine)

Two probes exist because API success did not imply the guarantee:

- GL-2 **placement self-test**: a wrong `POSIX_SPAWN_SETCGROUP` bit made
  `posix_spawn` return success while the child was born *outside* its
  cgroup. The provider now proves placement with a live probe before the
  first real launch (see [GL-2](evidence/linux-provider-GL-2.md)).
- GM-2 **behavioral readiness**: `bootstrap`/`kickstart` returning 0 does
  not mean the job runs; the provider polls `print` for `state = running`
  and boots out on failure (see [GM-2](evidence/macos-provider-GM-2.md)).

For this library the rule is: **prove the security/lifecycle property,
never infer it from API availability.**
