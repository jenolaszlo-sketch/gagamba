# GP-1B lifecycle evidence (compact reviewed summary)

Date: 2026-10-03. Raw report: `artifacts/fixture-selftest-20261003-155847-f635d6e3.json`
(ignored raw dir; this file is the retained summary). Fixture behavior only;
no sandbox backend is qualified. Supersedes the GP-1A summary for case counts
(manifest v2 = v1 16 + 4 F3).

- Entrypoint: `eng/fixture-selftest.ps1` (Release). Also `eng/fixture-selftest.sh`.
- Unit: `dotnet test Gagamba.Fixture.SelfTest` — 15/15 Passed (validators, workspace
  bounds, PID-reuse sweep guard, manifest v2 coverage).
- Harness: 20/20 mandatory Passed, aggregate Passed, cleanup Confirmed.
- New F3 IDs: F3-CHILD-GRANDCHILD (depths 2/1/0 chained via claimed ppids, leaf
  effect, exit 0, no survivors), F3-EARLY-EXIT (root exit 0 first, child marker
  lands after, stop clean), F3-BARRIER (file signal releases with exit 0;
  cancel-during-wait kills, no survivors), F3-ORPHAN-STOP (reparented 30 s sleeper
  confirmed alive after root death, killed via node record, 30 s effect suppressed).
- Ownership: `nodes/<pid>.json` records (pid + StartTime creation identity);
  sweep matches start times within 3 s so reused PIDs are never killed as ours
  (unit-pinned). Stop = tree-kill root + record sweep within 5 s budget.
- Source: commit `208c2b1` (GP-1A) dirty with GP-1B working tree, SHA-256 manifest
  over `tests/Fixtures/**/*.cs|csproj`, `eng/fixture-selftest.*`, `docs/fixture-protocol.md`.
- Environment: Windows 10.0.26200 win-x64 NTFS, host-direct, standard-user, .NET 10.0.12.
- Limitation: unsandboxed host kill + record sweep only. Kernel-backed owned-unit
  guarantees (surviving launcher crash, backend containment) arrive with GW-1A/GL-1A/GM-1A.

Next: GW-1A Windows availability/minimal-launch probe (export/schema, contained
process, captured I/O). F4/F5 grow with the backend spikes.
