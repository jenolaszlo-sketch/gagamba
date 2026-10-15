# GP-M5 native baseline and mechanism evidence

Date: 2026-10-15. Slice: GP-M5.0–GP-M5.3. Baseline public GP source:
`06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`. Observed source including only
new tests/analysis: `cc7b76c66e45acf5a10cae3eb5eb219c704fe963`.
Final provider implementation: not yet changed. Branch:
`codex/gp-m5-macos-capture`. Host: native macOS 15.7.9 ARM64, uid 501,
launchd `gui/501`, absolute `/bin/launchctl`. SDK 10.0.401; observed CLR
10.0.13. Gagamba producer .NET 8: not applicable (net10-only projects).
Scope: actual provider refusal, ordinary positive control and test-only FIFO
mechanism. Excludes capture provider implementation qualification, hostile
isolation, owner-death cleanup, setsid escape control and Marang semantics.

[Native run 38764971575](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/38764971575)
passed both pre-implementation probes. Source start/end records are clean.
Original JSON, host inventory and source records are preserved byte-for-byte in
[the baseline archive](baseline-native.zip), with per-file lengths/hashes in
its capture manifest. This green **baseline** run does not qualify capture.
Archive SHA256: `7cbdebcf57506c4e52cc66820b2cfd8de0d31bb0be5cf8055ad0d2dea3f95ce8`.

## Observed refusal anchor

Command: `dotnet run --project tests/Gagamba.CaptureProbe/Gagamba.CaptureProbe.csproj -c Release -- --mode baseline --output artifacts/gp-m5/baseline.json`.

The real native provider accepted the three requested lifecycle requirements,
then refused bounded stdout/stderr capture with the historical no-dispatch
reason. The captured target's fresh nonce marker remained absent immediately,
after the same preparation ran a separate ordinary positive-control command,
and after provider disposal. The ordinary marker contained the correct nonce;
terminal status was NaturalExit(0). Disposal succeeded, workspace removal and
both marker removals were confirmed. This verifies pre-dispatch refusal and
unconsumed preparation using the public provider boundary independently of Marang.

## Observed topology, not yet a production path

Command: `python3 eng/gp-m5/fifo-topology.py artifacts/gp-m5/fifo-mechanism.json`.

Two 0600 FIFOs in a unique 0700 directory had nonblocking close-on-exec readers
and keeper writers opened before launchd bootstrap. Bootstrap did not write
the target marker. Kickstart released one finite nonce-bearing shell command.
Both streams matched exact expected bytes; launchd recorded exit 7. Bootout
and recognized service absence succeeded. Closing keeper writers after terminal
cleanup produced EOF on both readers within the bound. All parent descriptors
closed and the unique directory was removed. No ordinary output files existed.

This establishes that the candidate launchd/FIFO topology works for this one
finite native mechanism case. Provider implementation must still prove pressure,
overflow, races, stop/disposal, resource closure, lifecycle/descendant parity and
installed-package behavior. Native observations support proceeding with the
minimal provider implementation described in the recorded contract. They do
not justify removing any pre-dispatch validation or claiming broader guarantees.
