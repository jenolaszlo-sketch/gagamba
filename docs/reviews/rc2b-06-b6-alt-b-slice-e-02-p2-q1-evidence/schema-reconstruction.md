# P2-Q1 — independent R/Wc/P normative reconstruction and verification matrix

Normative sources used (read in full where cited):

- Adopted P0 frozen-schema provenance record
  (`docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md`,
  blob `db25949d…`, accepted by `…-P0-OWNER-ACCEPTANCE-01`): §6 CJSON-1,
  §7 K, §8 M, §9 F, §10 S, §11.1–11.6 R/Wc/P and the acyclic graph.
- P2 entry owner decision
  (`docs/decisions/rc2b-06-b6-alt-b-slice-e-02-p2-entry-owner-01.md`): §§6–16.
- D1 approval-chain decision (§§5–7, 9) and the E-2 entry authorization.
- P2 producer report and evidence (challenged, not relied on, below).

The reviewer reconstructed each schema directly from the P0 extraction before
reading the implementation behavior; every producer claim was then re-checked
against the reconstruction and against reviewer-executed probes.

## 1. CJSON-1 (P0 §6) — required rules and reviewer vectors

| P0 §6 rule | Probe evidence (reviewer-owned) |
|---|---|
| UTF-8 ASCII only; no BOM/whitespace/trailing newline | MAL-02 (trailing `\n` → `CJSON-1 ASCII printable bytes`), MAL-11 (0xE9 byte) |
| Exactly one JSON object | MAL-09 (concatenated `{}` → `invalid CJSON-1`) |
| Keys strictly ascending ASCII, recursively; no duplicates/unknown/missing keys | MAL-03 (duplicate key), MAL-04 (swapped keys), AUTH-05a–h (ascending-position injections), MAL-16–18 (missing/extension → `R field set`) |
| Strings U+0020–U+007E; `\"` and `\\` escapes only; `/` literal; no `\u` | MAL-05 (`\u002d` → `noncanonical CJSON-1 representation`), MAL-08 (`\q` → `invalid CJSON-1`), MAL-07 (empty string), AUTH vectors |
| Unsigned decimal integers; no leading zero/exponent/fraction/sign; 0..2^63-1 | MAL-06 (`1.0`), MAL-15 (`"1"`), SEM-23 (Wc `schemaVersion` 2) |
| Booleans literal; null only where allowed | MAL-20 (`"true"` → `accepted boolean`), MAL-21 (`false` → `P accepted is not true`) |
| Round trip: parse + canonical serialization must equal input bytes | MAL-01 (trailing space), MAL-05, plus every accepted fixture (POS-01..03) |
| Empty strings allowed only where explicitly permitted (none here) | MAL-07; no schema field permits empty |
| Size limit 65,536 bytes per metadata file | MAL-12 (oversize), MAL-13 (empty) |
| SHA-256 over exact canonical bytes; 64 lower-case hex | POS-05, MAL-14 (upper-case hostId) |
| No reserialization accepted | POS-05 codec hash equals platform hash over the exact received bytes; every hash reference recomputed from received bytes |

Reviewer R/Wc/P size reality check: constructed records are 708/534/595 bytes
(B2/pre) and 777/603/664 bytes (B3/post) — far below the 65,536-byte limit and
at depth 2. A depth-overflow probe (MAL-19) is refused as `invalid CJSON-1`. The
depth-8 codec constraint remains the accepted P1-Q1-F01 implementation bound;
no R/Wc/P record approaches it, and it is not treated as a CJSON-1 rule.

## 2. R — detached review content (P0 §11.1)

Exact field set (13, ascending): `actionScriptSha256`, `artifactSetSha256`,
`caseId`, `catalogSha256`, `conclusion`, `fixtureManifestSha256`,
`fixturePolicySha256`, `hostId`, `phase`, `qualificationId`,
`qualificationRecordSha256`, `schemaVersion`, `topology`.

| Requirement | Reviewer verification |
|---|---|
| `conclusion` = `supported-for-bounded-validation` exactly | SEM-09 (`R conclusion`), POS-01 |
| `catalogSha256` = `Hk` recomputed from received K bytes | SUB-01, SUB-07; POS-04 |
| Ten values equal to K: caseId, phase, topology, hostId, artifactSetSha256, qualificationId, qualificationRecordSha256, fixtureManifestSha256, fixturePolicySha256, actionScriptSha256 | SEM-01..08, SEM-10, SEM-11 (each hash-consistent, exact named disagreement) |
| Qualification pair follows K's pre/post rule (null pre; exact pair post) | POS-07 (P1 refuses non-null pair on pre K), SEM-10, SEM-11 |
| `schemaVersion` = integer 1 | MAL-15 |
| caseId S2 ValidId; phase/topology enums; hashes 64 lower-case hex | MAL-14, producer category confirmed by code reading (`ValidId`, `Choice`, `Hex`) |
| No reviewer identity/signature/timestamp | AUTH-05a/b field-set refusals |
| `Hr = SHA256(CJSON1(R))` over exact received bytes; no self-hash field | POS-05; SUB-03/05 (Wc/P reference recomputation) |

## 3. Wc — detached owner receipt content (P0 §11.2)

Exact field set (11, ascending): `artifactSetSha256`, `caseId`,
`catalogReviewSha256`, `catalogSha256`, `decision`, `hostId`, `phase`,
`qualificationId`, `qualificationRecordSha256`, `schemaVersion`, `topology`.

| Requirement | Reviewer verification |
|---|---|
| `decision` = `accept-exact-bounded-validation-catalog` exactly | SEM-16 (`Wc decision`) |
| `catalogSha256` = `Hk` | SUB-02 |
| `catalogReviewSha256` = `Hr` recomputed from received R bytes | SUB-03; POS-04 |
| Duplicated K values: caseId, phase, topology, hostId, artifactSetSha256, qualificationId, qualificationRecordSha256 (plus `catalogSha256` as the K identity reference) | SEM-12..15, SEM-21 |
| Qualification pair equal to K | SEM-15, SEM-21 |
| `schemaVersion` = 1 | SEM-23 |
| No owner identity/signature/timestamp; no P hash | AUTH-05c/d field-set refusals; field set contains neither |
| `Hw = SHA256(CJSON1(Wc))` over exact bytes | POS-05; SUB-06 (P binding recomputation) |

## 4. P — supplied P-shaped content (P0 §11.3)

Exact field set (12, ascending): `accepted`, `artifactSetSha256`, `caseId`,
`catalogReviewSha256`, `catalogSha256`, `hostId`,
`ownerCatalogAcceptanceSha256`, `phase`, `qualificationId`,
`qualificationRecordSha256`, `schemaVersion`, `topology`.

| Requirement | Reviewer verification |
|---|---|
| `accepted` = boolean `true` as a content precondition | MAL-20, MAL-21; AUTH-02 |
| `catalogSha256` = `Hk` | SUB-04, SUB-08 |
| `catalogReviewSha256` = `Hr` | SUB-05 |
| `ownerCatalogAcceptanceSha256` = `Hw` | SUB-06 |
| Duplicated K values (as Wc) | SEM-17..20, SEM-22 |
| Qualification pair equal to K | SEM-10/11 class; code-verified for P |
| No self-hash; `Hp = SHA256(CJSON1(P))`; external expected-Hp comparison only | SUB-10, SUB-11 (no verifier entry accepts a caller expected-digest/authority string) |
| No administrator identity/signature/nonce/timestamp | AUTH-05e–h field-set refusals |
| `accepted=true` is never projected as an authority flag | AUTH-02 (no `Accepted` property; exact 11-property content surface) |

## 5. Counting semantics for "duplicated values" (clarification, not a defect)

The owner decision and producer report label Wc/P as having "eight duplicated
values" and R "ten". Enumerated precisely against P0 §11:

- **R:** `Hk` reference + 10 equal-to-K fields = 11 checked references.
- **Wc:** `Hk` + `Hr` + 7 equal-to-K fields; "eight" counts
  `catalogSha256` (the K identity) with the seven equal-to-K values.
- **P:** `Hk` + `Hr` + `Hw` + 7 equal-to-K fields; "eight" uses the same
  counting convention for the K-duplicated set.

The reviewer verified the implementation checks the complete enumerated set for
every record (code inspection plus hash-consistent probes per category). The
different count words are a labeling convention in producer/owner documents,
not a missing check.

## 6. Cross-record semantic consistency

- Chain order `K → R → Wc → P` is re-verified at each level (vanguard
  re-validation); a failure in a preceding record is named by that record's own
  rule before any downstream rule (SUB-09; SEM probes).
- No new edge is added: `Hk`/`Hr`/`Hw`/`Hp` are recomputed from the exact
  received canonical bytes; every reference field is compared to the recomputed
  value, never to a value asserted by the input itself.
- No edge returns into K/F/S; P has no self-hash; no hash fixed point exists in
  the verified graph.
- Reviewers found no accepted normative rule tying `phase` to `topology`
  (e.g., pre↔b2-lifeline) inside the E-2 subset, and the implementation does not
  invent one; cross-record equality (R/Wc/P vs K) is the accepted rule and is
  enforced.

Result: no normative schema defect, no canonicalization defect, no incorrect
content identity, and no broken binding was demonstrated.
