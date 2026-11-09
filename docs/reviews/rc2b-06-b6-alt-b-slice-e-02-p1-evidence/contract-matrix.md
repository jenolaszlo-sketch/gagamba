# E-2-P1 pre-implementation contract matrix

Date: 2026-11-09 (Asia/Manila)

Normative sources: adopted P0 record (`docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md`) §§6–10, itself extracted from the F02-accepted external documents `final-acceptance-hash-contract.md` (`49d566e1…`) and `case-policy-and-action-contract.md` (`667e6118…`); S2 `ValidId` from `docs/reviews/rc2b-06-b6-iml-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md` §1 (committed path: `docs/reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md`).

This matrix separates: **CJSON-1 syntax/canonicalization**; **K/M/F/S frozen-schema validation**; **cross-artifact identity verification**; and **approval/authority verification (prohibited in P1)**.

## 1. CJSON-1 syntax and canonicalization

| # | Requirement | Normative source | Implementation responsibility | Test |
|---|---|---|---|---|
| C1 | UTF-8, ASCII-only; every byte 0x20–0x7E | P0 §6 rule 1 | `CanonicalJson` byte scan | accept/reject cases |
| C2 | No BOM, whitespace, trailing newline | P0 §6 rule 1 | byte scan + round trip | raw-byte negatives |
| C3 | Exactly one JSON object at root | P0 §6 rule 2 | root kind check | `[]`, `1`, `"x"` refuse |
| C4 | No duplicate/unknown/missing keys | P0 §6 rule 2 | strict ascending keys (codec) + exact field sets (schema) | duplicate-key raw bytes; unknown-field schema tests |
| C5 | Keys sorted ascending ASCII, recursively | P0 §6 rule 3 | recursive walk + round trip | reordered-key fixture |
| C6 | Strings U+0020–U+007E; `\"`/`\\` only escapes, no `\u`, `/` literal | P0 §6 rule 4 | string walk + round trip | escape negatives |
| C7 | Unsigned canonical decimal integers; no leading zero/exponent/fraction/sign; 0..2^63-1 | P0 §6 rule 5 | raw-text equality + `TryGetInt64` | numeric negatives |
| C8 | Booleans literal; `null` only where allowed | P0 §6 rule 6 | codec permits `null`; schema restricts positions | null-position tests |
| C9 | Arrays retain defined order | P0 §6 rule 7 | order-preserving walk; exact sequences in schema | template/order tests |
| C10 | Parse→serialize equals input bytes | P0 §6 rule 8 | `Utf8JsonWriter` round trip | noncanonical negatives |
| C11 | Empty strings only where explicitly allowed; none in E-2 metadata | P0 §6 rule 9 | codec refuses empty strings (matches accepted F02 codec; no E-2 field permits one) | empty-string negatives |
| C12 | ≤ 65,536 bytes per file | P0 §6 rule 10 | length check | oversize negative |
| C13 | SHA-256 over exact canonical bytes; 64 lower-case hex | P0 §6 rule 11–12 | `CanonicalJson.Hash`, `IsLowerHex` | digest vector + parity |
| C14 | No self-approval hash field | P0 §6 rule 13 | K exact field set excludes approval/self hashes | K approval-field negatives |
| C15 | Depth-bounded parsing | accepted F02 codec behavior (`FinalAcceptanceCodec`, depth 8); bounded-parsing requirement | `JsonDocumentOptions.MaxDepth = 8` + walker depth check | deep-nesting negative |

## 2. K/M/F/S frozen-schema validation

| Artifact | Requirement (P0 section) | Implementation |
|---|---|---|
| M | exact keys `caseId,objects,schemaVersion,topology`; B2 empty objects; B3 exactly four role entries with exact `path`/`mode`/`destination`/length bounds and aggregate bound (§8) | `FrozenContractVerifier.VerifyManifest` |
| F | exact 19 keys; `policyKind`↔`topology`; B2 hard-false/null rules; B3 exact `hostPaths`/enrollment IDs/`copy`/`invocation`/`sandbox`/`resources`; `bootId` canonical UUID; uid/gid nonzero ≤2^31-1 (§9) | `VerifyPolicy` |
| S | exact 7 keys; exactly one of three templates per topology; contiguous index; timeouts 1..600000000000; no extensions (§10) | `VerifyActionScript` |
| K | exact 22 keys; pre null-pair / post ValidId+hash qualification; bounds; no approval/self-hash fields (§7) | `VerifyCatalog` |
| `ValidId` | 1–128 `[A-Za-z0-9._-]`; no leading/trailing dot; reject `.`/`..`, separators, whitespace, non-ASCII; strict ASCII case-sensitive | `FrozenSchemaRules.IsValidId` |
| Hex | 64 lower-case hex fields; 40 for commit/tree | `CanonicalJson.IsLowerHex` |

## 3. Cross-artifact identity verification

| # | Relationship | Source | Implementation |
|---|---|---|---|
| B1 | `Hm = SHA256(CJSON1(M))`; F/S/K `fixtureManifestSha256 == Hm` | P0 §§7.2, 8.2, 9.4, 10 | chain verifier |
| B2 | `Hf = SHA256(CJSON1(F))`; S/K `fixturePolicySha256 == Hf` | same | chain verifier |
| B3 | `Hs = SHA256(CJSON1(S))`; K `actionScriptSha256 == Hs` | same | chain verifier |
| B4 | case/phase/topology duplicated across K/F/S/M equal K | P0 §7.2 | chain verifier |
| B5 | hostId F==K; allowRelease F==K==template release presence; maximumSessionNs F==K; S timeouts ≤ F maximum | P0 §§7–10 | chain verifier |
| B6 | Per-action `Ai = SHA256("gagamba-validation-action-v1" ∥ 0x00 ∥ CJSON1(action))` | P0 §10.3 | `ActionScriptContract.ActionHashes` |

Construction order M→F→S→K is a content dependency, not approval authority.

## 4. Approval and authority verification — PROHIBITED in P1

R/Wc/P records, V/W/A actors, protected roots, publication/withdrawal, live challenge/response, capability, delegation, standing approval, cross-host reuse, E-1 consumption, and any authority factory are **not implemented**. P1 result types are non-authoritative contract values with no promotion path.
