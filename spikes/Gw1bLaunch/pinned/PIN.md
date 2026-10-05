# Schema pin: SandboxSpec FlatBuffer contract

Pinned for GW-1B minimal-launch work. Review status: content-pinned, not yet
reviewed for production use. No generated bindings are vendored; the spike
builds buffers with an in-repo mini-encoder against this exact file plus an
independent verifier (round-trip gate before any API call).

- Source: `microsoft/mxc`, tag `v0.8.0`
  (commit `7dac1a952f0c9ad13f0a4cb089c4e0e8b3e0013a`, GitHub-verified signature,
  2026-08-22).
- URL: `https://raw.githubusercontent.com/microsoft/mxc/v0.8.0/external/windows-sdk/BaseContainerSpecification.fbs`
- Fetched: 2026-10-03. File: `BaseContainerSpecification.fbs`, 4953 bytes.
- SHA-256: `E1E9AE9217638EE9F702EC1742C560952E3AAD30AFD41CD12F5F6737C3290059`
- License: MIT (mxc repository license; confirm the LICENSE file at review).
- Verified in this file: `namespace SandboxTechSpecLayout`, `root_type SandboxSpec`,
  `file_identifier "SBOX"`, `version:string (required)`.
- Cross-check: Microsoft Learn `createprocessinsandbox` (2026-06-01) requires
  file id `"SBOX"` and `version == "0.1.0"`, and documents the same field names
  (`app_container`, `integrity`, `disallow_win32k_system_calls`,
  `ui_restrictions`, `capabilities`, `fs_read_write`, `fs_read_only`,
  `network_policy.proxy`).

## Slot map (declaration order = vtable slots; derived from the pinned file)

| Slot | Field | Type | Default |
| --- | --- | --- | --- |
| 0 | version | string (required) | — |
| 1 | app_container | bool | false |
| 2 | integrity_level | uint32 (deprecated, slot preserved) | 0 |
| 3 | disallow_win32k_system_calls | bool | false |
| 4 | ui_restrictions | uint64 | 0 |
| 5 | least_privilege | bool | false |
| 6 | capabilities | string | — |
| 7 | fs_read_write | [string] | — |
| 8 | fs_read_only | [string] | — |
| 9 | network_policy | table NetworkPolicy | — |
| 10 | integrity | enum IntegrityLevel : byte | system_default (0) |
| 11 | fs_deny | [string] | — |

Omitted fields encode as absent (default). The deprecated slot 2 is never written.
`flatc --conform` against a future schema is GW-1B follow-up, not done here.
