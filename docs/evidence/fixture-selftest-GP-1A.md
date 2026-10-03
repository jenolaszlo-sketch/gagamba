# GP-1A FixtureSelfTest evidence (compact reviewed summary)

Date: 2026-10-03. Raw report: `artifacts/fixture-selftest-20261003-154656-666045be.json`
(ignored raw dir; this file is the retained summary). This proves fixture behavior
only and does not qualify any sandbox backend.

- Entrypoint: `eng/fixture-selftest.ps1` (Release). Also `eng/fixture-selftest.sh` for Linux/macOS.
- Unit: `dotnet test Gagamba.Fixture.SelfTest` — 13/13 Passed (protocol + evidence validators, workspace bounds).
- Harness: `Gagamba.Fixture.Harness` — 16/16 mandatory Passed, aggregate Passed, cleanup Confirmed.
- Manifest v1 IDs: F1-READ-SENTINEL, F1-WRITE-OUTPUT, F1-EXIT-CODE, F1-SCOPE-ISOLATION,
  F2-STDOUT-CAPTURE, F2-STDERR-CAPTURE, F2-STDIN-BOUND, F2-HANG-STOP, F2-OUTPUT-LIMIT,
  P-READY-CONTINUE-SEQUENCE, P-REJECT-UNKNOWN-VERSION, P-REJECT-UNKNOWN-KIND,
  P-REJECT-BAD-SEQUENCE, P-REJECT-OVERSIZE, P-REJECT-MISMATCH-IDENTITY, EVIDENCE-VALID.
- Bounds demonstrated: 16 KiB protocol lines, 64 KiB stdin (70k refused pre-spawn),
  1 MiB stdout/stderr caps (2 MiB probe yields OutputLimit), 3 s hang watchdog kill,
  workspace escape (`../escape.bin`) rejected with Error + non-zero exit.
- Source: commit `8eee0fd11f5750a7359379d44207945d3630aa01`, dirty true (uncommitted GP-1A files),
  manifest SHA-256 over `tests/Fixtures/**/*.cs|csproj`, `eng/fixture-selftest.*`, `docs/fixture-protocol.md`.
- Environment: Windows 10.0.26200 (NT 10.0.26200.0), X64/X64, win-x64, NTFS (C:\),
  host-direct, standard-user, .NET runtime 10.0.12 / SDK 10.0.401.
- Cleanup: Confirmed — 5 disposable fixture roots deleted, marker-gated, no survivors.
- Validator: strict v1 accepts this report (0 errors); rejects bad envelope (23 errors on synthetic bad).

Next: GP-1B (F3 child/grandchild, barrier, ownership) and GW-1A availability/minimal-launch probe.
Backend conformance, workload, and package qualification remain open.
