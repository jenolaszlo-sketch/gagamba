# GW-1B minimal-launch evidence, slice 1 (compact reviewed summary)

Date: 2026-10-03. Raw report: `artifacts/windows-launch-20261003-161059-3c2ab3c0.json`
(ignored raw dir; this file is the retained summary). First sandboxed launches
on this host. Denial/descendant/race/workload evidence and the provider ADR
remain open (later GW-1B slices).

- Entrypoint: `eng/launch-spike.ps1` (Release). Spike: `spikes/Gw1bLaunch`.
  `eng/launch-spike.sh` refuses off-Windows with no evidence.
- Result: 6/6 mandatory Passed, aggregate Passed, cleanup Confirmed.
- Setup/cleanup contract (no elevation, per-user, reversible): disposable
  workspace per leg (marker-gated) + unique identity `GagambaGW1B<16hex><leg>`;
  reaped child, closed handles, `DeleteAppContainerProfile` + registry-Moniker
  existence proof, workspace deleted. No host-wide changes.
- L1-SCHEMA-PIN: mxc `v0.8.0` (`7dac1a95`, signature-verified), 4953 bytes,
  SHA-256 `E1E9AE92…`, root/file-id/version-required confirmed.
  See `spikes/Gw1bLaunch/pinned/PIN.md`. License: MIT (confirm at review).
- L1-SPEC-BUILD: in-repo mini-encoder + independent verifier. Round-trip ok
  (bare/app 40B, grants 80B); wrong-version and corrupted-id negatives rejected.
  Encoder output cross-checked against official `flatc` 25.12.19 reference
  buffers (`verify-file` accepts all three: bare/app/grants).
- L1-BARE-LAUNCH: `app_container=false` is **not a supported shape** — engine
  returns FALSE + `ERROR_NOT_SUPPORTED`, pid 0, no profile materialized.
  Fail-closed rejection before target dispatch (leg asserts the rejection).
- L1-APPCONTAINER-LAUNCH: api=TRUE, exit=7 as requested, engine-job=TRUE,
  43 ms. Profile existed during the run and was deleted after (registry proof).
- L1-FS-GRANT-EFFECT: api=TRUE, exit=0, engine-job=TRUE, 46 ms. Target
  (`cmd /c echo … > effect.txt`) wrote the exact 31 B marker into the granted
  workspace; profile existed then deleted.
- L1-CLEANUP: all legs reaped, handles closed, profiles removed
  (existed→deleted with Moniker proof; bare leg never-materialized),
  workspaces deleted.

## Encoder findings (for the record)

- The first encoder revision emitted vtable-after-table with a flipped
  soffset sign; the engine answered `ERROR_INVALID_DATA` on every leg while
  the self-verifier (same wrong sign) passed — caught only by comparing
  against official `flatc` output. Lesson applied: independent oracle, not
  self-agreement.
- Second finding: buffers whose vector elements reference strings at lower
  addresses (backward uoffsets — legal FlatBuffers) are also rejected with
  `ERROR_INVALID_DATA`. The encoder now emits vectors-before-strings so every
  uoffset points forward, matching canonical `flatc` layout. `flatc` itself is
  used only as a temp-dir oracle, never vendored.

## Open (next GW-1B slices)

- Read-only grant semantics under AppContainer (denied-read proof with
  positive controls), descendant inheritance + tree stop against the engine
  job, launch/cancel/crash races, I/O transport (inheritHandles=FALSE means
  STARTUPINFO redirection needs proof), offline workload fixtures, provider ADR.
- `DeriveAppContainerSidFromAppContainerName` returns failure even for live
  profiles on this build — profile existence is proven via the registry
  Moniker key instead (chain-validated by `profile-probe`).
