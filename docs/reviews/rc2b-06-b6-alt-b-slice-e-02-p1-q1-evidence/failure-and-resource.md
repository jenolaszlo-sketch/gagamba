# P1-Q1 — failure handling, robustness, and resource bounds

Date: 2026-11-09 (Asia/Manila)

## Source audit observations

- `CanonicalJson.Parse` validates size before parsing, byte-prints before parsing, wraps `JsonException` into `InvalidDataException("invalid CJSON-1")`, and only then checks root kind, canonical walk, and byte-equality round trip. No path returns a success after partial validation.
- `Try*` wrappers catch exactly `InvalidDataException`; every schema accessor runs after an exact field-set check, so `GetProperty` cannot miss, and every nested object/array is kind-checked before member access. No unhandled path was found in the audited methods.
- Inputs are never mutated (probe compared before/after byte arrays).
- No static mutable state exists in the four production files (only `static readonly` string arrays); repeated calls are deterministic.
- `checked(aggregate + length)` is bounded by four elements ≤ 2^24 each, so overflow is unreachable under the verified per-field bounds.
- `Encode` validates the value tree and serializes without repair; it is used only by tests and by the internal per-action hash over already-validated elements. It is not part of any input-acceptance path.

## Reviewer robustness probes (all PASS)

| Probe | Method | Result |
|---|---|---|
| 400 deterministic single-byte mutations across M/F/S/K | `TryVerifyChain` | never threw; every rejection carried a message; 0 silent acceptances |
| Mutated M/F/S identities refused | recomputed bindings | 0 accepted (m=0, f=0, s=0) |
| Mutated K bytes remaining schema-valid | content addressing | 4 accepted as **new** valid K objects (different `Hk`); logged, expected |
| 11 truncations of K (5-byte steps) | `TryVerifyChain` | never threw |
| 65,537-byte input | `TryVerifyChain` | rejected `CJSON-1 size` |
| Deep nesting 8–9 arrays | `TryParse` | rejected `invalid CJSON-1` |
| Oversize / max-size boundary | `TryParse` | 65,536 accepted, 65,537 rejected |

## Resource bounds

- Size: hard 65,536-byte cap enforced before parsing.
- Depth: 8-container cap (root + 7 nested), enforced by the parser option and the walker.
- Allocation is bounded by input size and depth; no recursion beyond the depth cap; no I/O, process, environment, or network calls (verified by IL dependency scan in `reachability-release-p4.md`).

## Exception policy

The accepted P1 contract requires deterministic `false` + failure for invalid input through the `Try*` surface; the reviewer found no case where invalid input produced `true`, and no exception escaped the `Try*` wrappers across the mutation corpus. Direct `Parse`/`Encode` calls throw `InvalidDataException` by design (documented XML comments), which is consistent with the owner decision (no universal exception-suppression policy was mandated).
