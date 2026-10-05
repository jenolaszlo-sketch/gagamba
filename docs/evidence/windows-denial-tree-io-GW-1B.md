# GW-1B slice 2 evidence: denial, descendants, races, I/O transport

Date: 2026-10-03. Raw report: `artifacts/windows-launch-20261003-163832-37feedab.json`
(ignored raw dir; this file is the retained summary). Extends slice 1
(`windows-minimal-launch-GW-1B.md`); L1 legs re-ran green in the same report.
10/12 Passed; the 2 failures are characterized blocking gaps, not harness faults.

- Entrypoint: `eng/launch-spike.ps1` (Release). Spike: `spikes/Gw1bLaunch`.
- Controls: every denial/tree leg runs the identical fixture unsandboxed first
  via `ControlRunner` (separate workspaces, scrubbed before the sandbox leg).
  A failed control aborts its leg as incomplete — never as sandbox evidence.

## Proven (with positive controls)

- L2-READONLY-DENIAL: rw=[ws] + ro=[roDir]. Sandboxed `copy` out of ro lands
  (`ok.txt` present); sandboxed write into ro is denied (`try.txt` absent,
  target exit 1). Unsandboxed controls create both files.
- L2-DENIED-READ: ungranted `scope/secret.txt` copied to granted ws lands in
  the control but not in the sandbox (`stolen.txt` absent, exit 1). Sentinel
  hash recorded; scope workspace deleted after.
- L2-TREE-EFFECT: `root.bat` spawns a real `cmd` descendant (`start /b`).
  Root exit 3 observed; `root.txt` + descendant `child.txt` exact; descendant
  write to ungranted scope absent (`sneak.txt`). Descendants inherit
  restrictions.
- L2-CANCEL-RACE: `TerminateProcess` 500 ms after launch; exit 99 confirmed,
  reaped, profile swept, workspace deleted. (Synchronous API: no mid-create
  cancellation exists; post-launch terminate is the cancellation primitive.)
- L1-CLEANUP: 9/9 legs reaped, profiles removed-or-never-materialized
  (registry-Moniker proof), workspaces deleted.

## Blocking gaps (Failed legs — milestone gate, correctly red)

- L2-TREE-STOP: **descendants outlive the root both when the root is killed
  (`KILL-DOES-NOT-STOP-TREE`) and when it exits naturally
  (`EXIT-DOES-NOT-STOP-TREE`)**. Sleeper started (file proof), barrier opened
  2–3 s after root death, `late.txt` still written. The engine does not
  implement "root exit initiates stop of remaining descendants".
  Consequence for the ADR: Gagamba needs its own supervisor — candidate is a
  root-wait plus ToolHelp parent-PID tree sweep with PID+start-time identity
  (no job handle is returned by the API), because the engine job alone does
  not bound lifetime. Kernel-backed ownership remains unproven.
- L2-STDIO-REDIRECT: `STARTF_USESTDHANDLES` with valid handles is rejected
  with `ERROR_INVALID_DATA` in all three configurations tried: single
  non-inheritable file handle, single `HANDLE_FLAG_INHERIT` file handle, and
  all three handles inheritable (stdin = NUL device). The file-effect pattern
  (target writes into a granted dir, host reads) remains the only proven
  output oracle. Anonymous-pipe variant is the remaining experiment.

## Notes

- `timeout.exe` refuses redirected stdin (`ERROR: Input redirection is not
  supported`), so all waits use file-barrier loops; the stdin-less `timeout`
  failure also confirms the sandbox is not needed to explain it (controls).
- `DeriveAppContainerSidFromAppContainerName` fails even for live profiles on
  this build; profile lifecycle is proven via the registry Moniker key
  (chain-validated by `profile-probe`: create→present→delete→gone).

## Open (slice 3)

Pipe-variant stdio attempt; supervisor sweep prototype (ToolHelp) against a
live tree; offline workload fixtures (dotnet/Git/shell with explicit grants);
provider ADR (experimental API + supervisor vs restricted-token fallback).
