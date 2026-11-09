# E-2-P1 — implemented negative-control matrix

Date: 2026-11-09 (Asia/Manila)

All controls are deterministic tests under `tests/Gagamba.Execution.Linux.M1.Tests/Validation/`. Failure messages are asserted exactly where the diagnostic is part of the deterministic contract; the remainder assert rejection plus the expected invariant owner. Byte-level controls construct raw bytes independently (never through the encoder).

| # | Control | Requirement | Expected refusal | Test |
|---|---|---|---|---|
| 1 | Missing CJSON-1 field | §6 rule 2; schema field sets | `M field set` / `K field set` | CanonicalJsonTests; FrozenContractNegativeTests |
| 2 | Wrong field type | P0 §§7–10 | `<key> string/integer/boolean/object/array` | FrozenContractNegativeTests |
| 3 | Invalid identifier | S2 `ValidId` | `K caseId` / `F toolEnrollmentId` | FrozenContractNegativeTests |
| 4 | Unknown schema version | §7/§8/§9/§10 `schemaVersion`=1 | `schemaVersion integer` | FrozenContractNegativeTests |
| 5 | Unsupported enum | §7–§10 | `F policy kind` / `K phase` / `M topology` | FrozenContractNegativeTests |
| 6 | Malformed CJSON-1 | §6 rules 1–8 | `invalid CJSON-1` / `noncanonical CJSON-1 representation` | CanonicalJsonTests; parity tests |
| 7 | Noncanonical bytes (space, BOM, escapes, numbers) | §6 rules 4–5, 8 | rejection with deterministic message | CanonicalJsonTests (20 categories) |
| 8 | Duplicate key | §6 rule 2 + ascending order | `CJSON-1 keys are not strictly ascending` | CanonicalJsonTests; FrozenContractNegativeTests |
| 9 | Prohibited extension | Exact field sets | `K field set` / `M object field set` / `S action field set` | FrozenContractNegativeTests |
| 10 | Truncated input | §6 rules 2, 8 | `invalid CJSON-1` | FrozenContractNegativeTests |
| 11 | Incorrect Hm | B1 | `F/S/K fixtureManifestSha256 does not bind M` | FrozenContractNegativeTests |
| 12 | Incorrect Hf | B2 | `S/K fixturePolicySha256 does not bind F` | FrozenContractNegativeTests |
| 13 | Incorrect Hs | B3 | `K actionScriptSha256 does not bind S` | FrozenContractNegativeTests |
| 14 | Substituted artifact content | B1–B3 | binding failure (cross-chain substitution) | FrozenContractNegativeTests |
| 15 | Conflicting duplicated values | §7.2 | `case id / phase / topology / host id / allow release / session limit disagreement` | FrozenContractNegativeTests (rebuilt hashes) |
| 16 | M B2 latent objects | §8.2 | `M B2 objects` | FrozenContractNegativeTests |
| 17 | M B3 role/mode/destination/aggregate violations | §8.1–8.2 | `M object role order/mode/destination`, `M aggregate length` | FrozenContractNegativeTests |
| 18 | F B2 null/release violations | §9.2 | `F B2 topology/release/sandbox/toolEnrollmentId` | FrozenContractNegativeTests |
| 19 | F B3 host-path violations | §9.3 | `F hostPaths field set`, `... mode`, `... uid` | FrozenContractNegativeTests |
| 20 | F B3 fixed-object violations | §9.4 | `F copy hooks`, `F invocation argv length`, `F sandbox ...`, resource constants | FrozenContractNegativeTests |
| 21 | S template/index/op/timeout violations | §10.1–10.2 | `S action template`, `index integer`, `S action op`, `timeoutNs integer` | FrozenContractNegativeTests |
| 22 | K pre/post qualification rules | §7.1 | `K pre qualificationId`, `qualificationId string` | FrozenContractNegativeTests |
| 23 | Release template vs allowRelease | §10.2 | `release template disagreement` | FrozenContractNegativeTests |
| 24 | Action timeout beyond session | §10.1 | `S action timeout exceeds the F maximum session` | FrozenContractNegativeTests |
| 25 | Unauthorized authority field in K | §7.2 | `K field set` (`approved`, `catalogReviewSha256` insertions) | FrozenContractNegativeTests |
| 26 | Structural success is not approval | Owner decision §10 | No authority-shaped names or promotion path; negative tests for authority-field injection | ValidationReachabilityTests; FrozenContractNegativeTests |
| 27 | Oversize input | §6 rule 10 | `CJSON-1 size` | CanonicalJsonTests |
| 28 | Exponent depth | bounded parsing | rejection | CanonicalJsonTests |
| 29 | F02 parity controls | §6 shared rules | both codecs reject 13 noncanonical categories | Cjson1F02ParityTests |

No negative condition was invented that the normative contract permits; rules that are intentionally schema-scoped (null positions, unknown fields, F02 divergence) are tested as such and documented in `cjson1-contract.md`.
