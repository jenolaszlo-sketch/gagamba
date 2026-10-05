# FlatBuffers reference vectors (test oracles, not inputs)

Official `flatc` 25.12.19 output for fixed shapes, used by L1-SPEC-BUILD to
assert byte-exact encoder compatibility. Regenerating:

flatc --binary -o testdata <pinned BaseContainerSpecification.fbs> <json>

with inputs: `{ version: "0.1.0" }` (bare), `{ version, app_container: true }`
(app), `{ version, app_container: true, fs_read_write: ["C:\\tmp\\ws"] }`
(grants-fixed), `{ version, app_container: true, fs_read_only:
["C:\\temp\\a", "C:\\temp\\b"] }` (ro2: multi-element vector order),
`{ version, app_container: true, capabilities: "registryRead", fs_read_only:
["C:\\Windows"] }` (caps). Schema pin: `../pinned/PIN.md` (mxc v0.8.0).
flatc itself is a temp-dir oracle only and is never vendored; these 36–108
byte outputs are.
