# E-2-P1 — CJSON-1 codec contract and F02 parity findings

Date: 2026-11-09 (Asia/Manila)

## Implemented codec (`CanonicalJson`)

The codec implements the accepted CJSON-1 rules from the adopted P0 record §6 (extracted from `final-acceptance-hash-contract.md`): ASCII-only bytes (0x20–0x7E), no BOM/whitespace/trailing newline, exactly one root object, strictly ascending ASCII keys with no duplicates, strings restricted to U+0020–U+007E with only `\"` and `\\` escapes, unsigned canonical decimals (no sign/exponent/fraction/leading zero, 0..2^63-1), booleans, parse→canonical-serialize byte equality, 65,536-byte limit, SHA-256 over exact bytes as 64 lower-case hex, and no self-approval field.

## Codec-level decisions (documented for independent review)

| Decision | Ruling | Basis |
|---|---|---|
| Depth bound | Serializer/parser depth limited to 8 | Consistent with the accepted F02 codec (`FinalAcceptanceCodec`, `MaxDepth = 8`) and the bounded-parsing requirement; the accepted schemas nest far below this |
| `null` handling | Codec permits `null` as a value kind; schemas decide positions | E-2 requires `null` in B2 policy fields and pre-phase catalog qualification fields; F02's schema forbids all nulls (schema-scoped, see parity below) |
| Empty strings | Rejected at the codec boundary | Consistent with the accepted F02 codec and with the E-2 schemas, none of which declares an empty-string permission |
| Unknown fields | Codec is schema-free and accepts structurally canonical extra fields; `RequireFields` in the K/M/F/S validators rejects every extension | Mirrors the accepted separation where field sets are schema-level rules |
| Hashing | `Hm/Hf/Hs/Hk` are SHA-256 over the exact received bytes **after** the parse→serialize equality proof | P0 record §6 rule 8/11; no normalized substitute is hashed |
| `Ai` per-action hash | `SHA256(ASCII("gagamba-validation-action-v1") ∥ 0x00 ∥ CJSON1(action))` | P0 record §10.3 |

## F02 parity assessment (`FinalAcceptanceCodec`)

`FinalAcceptanceCodec` exposes no canonical serializer, so parity was measured as acceptance/rejection behavior plus exact round-trip identity of accepted canonical bytes.

| Case | New codec | F02 codec | Finding |
|---|---|---|---|
| Canonical 24-field record and 7-field selector | Accept; re-encode equals input | Accept | Compatible |
| 13 noncanonical categories (trailing space/newline, BOM, non-ASCII, duplicate key, multiple objects, `\u` escape, `\/` escape, negative/float/exponent schema values, leading zero, empty string) | Reject | Reject | Compatible |
| `null` values | Accept structurally; schemas restrict positions | Reject (its schema forbids nulls) | **Schema-scoped divergence**, documented and required by E-2 |
| Unknown field | Accept at codec; K/M/F/S schemas reject | Reject (codec+schema) | **Schema-scoped divergence**, documented; extensions refuse in every E-2 schema |
| Empty strings | Reject | Reject | Compatible |
| Depth > 8 | Reject (`invalid CJSON-1`) | Reject | Compatible |
| `Hash` / `IsLowerHex` primitives | Equal outputs on sampled inputs | — | Compatible |

**Reuse ruling compliance.** `FinalAcceptanceCodec` and all other authority-bearing files are unchanged. The new core is standalone with parity tests and a divergence-control obligation recorded in the implementation report §6: any future change to either canonical implementation requires a parity reassessment before it is relied upon.
