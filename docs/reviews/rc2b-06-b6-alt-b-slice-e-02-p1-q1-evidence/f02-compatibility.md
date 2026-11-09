# P1-Q1 — F02 parity matrix

Date: 2026-11-09 (Asia/Manila)

Reference: accepted F02 `FinalAcceptanceCodec` (`src/Gagamba.Execution.Linux.M1/Installed/FinalAcceptance.cs`, unchanged) and the shared CJSON-1 rules. Both codecs were invoked independently by reflection from the reviewer probe.

| Case | New codec | F02 codec | Classification |
|---|---|---|---|
| Canonical 24-field record | accept, re-encode equals input | accept | overlap consistent |
| Canonical 7-field selector | accept | accept | overlap consistent |
| Trailing space | reject (`noncanonical`) | reject | overlap consistent |
| BOM | reject | reject | overlap consistent |
| Duplicate `acceptance` field | reject | reject | overlap consistent |
| `\u0031` escape | reject | reject | overlap consistent |
| `1.0` schema version | reject | reject | overlap consistent |
| Empty string value | reject | reject | overlap consistent |
| Depth > 8 | reject | reject | overlap consistent |
| `Hash` outputs | equal | equal | overlap consistent |
| `IsLowerHex` outputs | equal | equal | overlap consistent |
| `null` value in a schema position | accept structurally; K/F schemas restrict positions | reject (F02 schema forbids all nulls) | **SCHEMA-SCOPED DIFFERENCE** — confirmed by probe: E-2 B2 policies and pre-phase catalogs require null fields; the underlying canonical representation is consistent |
| Unknown field | accept at codec; K/M/F/S `RequireFields` reject extensions | reject (codec+schema) | **SCHEMA-SCOPED DIFFERENCE** — confirmed: the same canonical bytes are accepted structurally and refused by the E-2 schema layer |

## Divergence-control assessment

- The producer parity suite (`Cjson1F02ParityTests`) asserts both-accept on canonical fixtures, both-reject on 13 noncanonical categories, hash/hex parity, and the two documented schema-scoped differences.
- Reviewer reproduction confirms those assertions; no **NORMATIVE CJSON-1 DIVERGENCE** was found.
- Future divergence in overlapping behavior would be caught by the parity tests only when the test suite runs; the divergence-control obligation (parity reassessment on any change to either canonical implementation) is recorded in the P1 report §6 and remains binding.
- No modification of `FinalAcceptanceCodec` occurred; no shared-core refactor was attempted (the owner decision reserved that for a separate amendment).
