# GW-1B slice 3 evidence: pipes, supervisor sweep, workload closure

Date: 2026-10-03. Raw report: `artifacts/windows-launch-20261003-171635-cc2d5d93.json`
(ignored raw dir; this file is the retained summary). L1/L2 legs re-ran in the
same report (tree-stop and file-stdio gaps stand as recorded in slice 2).
12/15 Passed; 3 Failed legs are open gates, not harness faults.

- Entrypoint: `eng/launch-spike.ps1` (Release). Spike: `spikes/Gw1bLaunch`.
- Provider decision: [ADR 0001](../adr/0001-windows-provider.md) (prototyping
  scope, not production qualification).

## Proven

- L3-PIPE-STDIO: anonymous pipes accepted where file handles (3 configs) were
  rejected — `echo hello-pipe` captured exactly, exit 0, engine-job. The
  transport rule is now: pipes yes, files no. Rationale unknown (recorded,
  not explained away).
- L3-SUPERVISOR-SWEEP: ToolHelp parent-PID walk finds the sandboxed descendant
  across the boundary (found=1; a `conhost.exe` fellow traveler is correctly
  skipped by the cmd-only guard), terminates it (exit 99), root survives the
  sweep and is terminated afterwards, barrier opened, no `late.txt`. The ADR
  mechanism works end to end, control and sandbox alike. Reproduced 2/2 runs.
  (First attempt failed on two spike bugs, both fixed and recorded: ANSI
  ToolHelp import garbling names/sizes, and a missing SYNCHRONIZE right making
  waits fail with access-denied.)
- L1-CLEANUP: 12/12 legs clean.

## Open gates

- L3-WORKLOAD-DOTNET: `whoami.exe` runs with workspace-only grants (exit 0,
  identity shape confirmed, names never recorded) — so the base image covers
  System32 and non-cmd executables work. But `dotnet --info` launches
  (api=True) and exits 1 with zero output on stdout AND stderr in EVERY grant
  configuration (ws-only, +dotnetDir, +full closure). Grants are exonerated;
  this is runtime compatibility (registry access without a `registryRead`
  capability, Low-IL runtime friction, or env), not path closure. Next:
  `registryRead`-capability variant, then env tuning. dotnet build/test
  workloads wait on this answer.
- L2-TREE-STOP: engine never stops descendants (kill and natural exit both
  measured). Compensated by the proven supervisor, not closed.
- L2-STDIO-REDIRECT: file handles rejected in all configs. Pipes are the
  answer; file-effect oracles stay as backup.
- 0xB7 (`ERROR_ALREADY_EXISTS`) appeared twice for identical inputs across
  runs, then vanished — transient engine state, not a deterministic rule.
  Fresh-identity-per-variant practice stays regardless.
- Profile lifecycle timing varies (existed-then-deleted vs never-materialized
  with no residue either way); likely eager engine removal racing our sweep.
  The residue check — not the timing — is the gate, and it passes every run.
