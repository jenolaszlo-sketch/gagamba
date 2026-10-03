# GP-1 fixture and evidence contract

Status: GP-1A + GP-1B implemented 2026-10-03 (F1/F2/F3 + protocol + evidence v1 under `tests/Fixtures/`, entrypoint `eng/fixture-selftest.ps1`). F4/F5 grow with the backend spikes. This is private test tooling, not a public Gagamba API.

## First implementation slice

Create a .NET 10 test host and worker under `tests/Fixtures/` with a local verification entrypoint. The host controls temporary directories, unique run IDs, expected sentinels, deadlines, cleanup and evidence. The worker performs only bounded enumerated fixture operations. Do not expose an arbitrary shell-command field.

Start with read, write, exit and bounded output. Then add child/descendant and networking fixtures. Each new worker command needs a passing positive control before use against a sandbox. A native worker is added only when managed code cannot exercise a required primitive.

## Host-worker messages

Use UTF-8 JSON Lines with `protocolVersion: 1`, `runId`, `workerId`, `sequence`, `kind` and a bounded payload. Maximum protocol line is 16 KiB; reject unknown versions/kinds, duplicate/out-of-order sequence, mismatched identity and oversized records. Commands and responses have explicit enumerated fields. Paths must belong to the fixture's host-created sentinel set; the host cannot accept arbitrary worker-proposed cleanup paths.

Control sequence: host prepares fixture -> worker Ready -> host Continue -> operation -> Result -> worker exits. A cancellation test can hold the worker at Ready to establish the race deliberately. Timeouts and malformed messages are failures, not evidence of confinement.

Keep protocol traffic distinguishable from workload bytes. Use a dedicated explicitly inherited pipe if the backend can support it; otherwise use a narrowly scoped worker-only framed stdout protocol before exercising raw-output cases in separate worker invocations. Do not add network access merely for fixture control. A worker can lie, so its messages are advisory: the host independently verifies filesystem effects, listener records and termination.

## Operations and independent checks

| Slice | Worker operations | Host verification |
| --- | --- | --- |
| F1 | Read sentinel, write output, deterministic exit | Exact synthetic bytes/hash; target marker and exit state; out-of-scope sentinel unchanged |
| F2 | Output on stdout/stderr, read stdin, hang | Byte counts, bounded drain, deadline and overflow result; pipe liveness |
| F3 | Spawn child/grandchild, early root exit, wait at barrier | Unique worker IDs and ownership records; child effects; no survivors after stop acknowledgement |
| F4 | TCP/UDP attempts including loopback and IPv6 | Listener binds/readiness and positive control confirmed; target attempt observed; denied listener received no payload |
| F5 | Path/link replacement, native escape primitives, concurrent sandboxes | Test-specific independent object/ownership checks; backend evidence and unchanged denied resources |

For CF-NET, merely observing no packets is insufficient: prove the target executed the attempted operation and the unsandboxed control succeeds. If infrastructure or prerequisites prevent the control, record NotRun with a reason and fail a mandatory qualification gate.

For CF-FAIL, use a distinct target entry marker. Test host failures must not accidentally create it. A missing marker is useful only when the fixture has independently demonstrated that reaching target code reliably creates it.

For lifecycle tests, use explicit barriers and an independent watchdog. PID identity must include a start/creation identity to avoid PID reuse errors. The eventual backend must expose sufficient owned-unit evidence without treating worker-reported PIDs as authoritative. Cleanup targets only known fixture-owned units and checked temporary roots.

## Evidence report version 1

Implement a strict report validator with the collector in GP-1. Unknown versions and incomplete records fail validation. Proposed envelope:

| Field | Required content |
| --- | --- |
| schemaVersion | Integer 1 |
| evidenceKind | FixtureSelfTest, CapabilityProbe, BackendConformance, Workload or PackageQualification |
| runId / startedUtc / endedUtc | Unique ID and ordered UTC timestamps; user-facing summaries may use local time |
| source | Git commit when available, dirty flag and SHA-256 manifest of relevant source/config inputs; null commit with an explicit reason for an unborn repository |
| environment | OS/build/kernel, architecture, filesystem, outer environment, setup/target privilege; unavailable fields explicitly explained |
| backend | Provider/helper identity/version or null for FixtureSelfTest; no fake production provider name |
| profile | offline-process-v1 and requested/prepared hashes where applicable; no invented hashes before policy preparation exists |
| cases | Expected IDs, per-case outcome, control outcome, reason, timing and bounded evidence references |
| cleanup | Confirmed, Failed or Unknown; owned-resource disposition and bounded diagnostics |
| summary | Derived counts, mandatory completeness and aggregate outcome; never trust worker-provided totals |

Case outcomes: Passed, Failed, Unsupported, NotRun. A probe may legitimately report Unsupported, but it does not pass the corresponding backend gate. FixtureSelfTest proves fixture behavior only and cannot satisfy BackendConformance. Mandatory case IDs come from a trusted versioned manifest, not worker input. No duplicate IDs, empty-success reports or missing requested legs.

Use relative artifact references confined to the run directory. Exclude secrets and complete environments from reports. Copy only bounded synthetic fixture output. Reports must survive ordinary failure; a crash with no valid report is incomplete evidence, never success.

## GP-1 completion

F1/F2 plus reliable protocol validation, independent watchdog, exact source/evidence identity and passing controls complete GP-1A. F3 and the corresponding self-tests complete GP-1B (done 2026-10-03: manifest v2, 20/20 harness + 15/15 unit). GW-1 may start after GP-1A for availability and minimal launch; GW-1 containment/lifetime qualification requires GP-1B (now unblocked). F4/F5 grow with the backend spikes and are mandatory before declaring a provider conformant.

No worker, launcher, tests or reports are implemented by this document. Do not manufacture sample Passed reports as handoff evidence.
