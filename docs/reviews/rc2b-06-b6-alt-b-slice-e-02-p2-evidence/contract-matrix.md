# E-2-P2 pre-implementation contract matrix

Date: 2026-11-09 (Asia/Manila)

Normative sources: adopted P0 record §11.1–§11.6 (blob `db25949d…`), extracted from the F02-accepted `case-policy-and-action-contract.md` (`667e6118…`) §1 and `mechanism-b-session-capability.md` (`f5d28dbe…`); D1 approval-chain decision (`83fe8d26…`) §§5–7; P2 entry owner decision (`893b48a2…`) §§6–14.

This matrix separates **R/Wc/P structural verification** (P2) from **approval/provenance/issuance authority** (excluded). No field, version, signature, timestamp, or authority metadata is invented.

## R — detached review content (13 fields)

| Attribute | Requirement | Source |
|---|---|---|
| Schema identifier/version | `schemaVersion` = integer 1 | P0 §11.1 |
| Exact field set (ascending) | `actionScriptSha256`, `artifactSetSha256`, `caseId`, `catalogSha256`, `conclusion`, `fixtureManifestSha256`, `fixturePolicySha256`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion`, `topology` | P0 §11.1 |
| Types | hashes 64 lower-case hex; `caseId` S2 `ValidId`; `phase`/`topology` enums; `schemaVersion` 1 | P0 §11.1, §7.1 |
| Required literal | `conclusion` = `supported-for-bounded-validation` | P0 §11.1 |
| Identifier grammar | S2 `ValidId` (1–128 `[A-Za-z0-9._-]`, no leading/trailing dot, reject `.`/`..`/separators/whitespace/non-ASCII) | P0 §16 P0-G3; QUALIFICATION-RECORD-REVIEW §1 |
| Nullable fields | `qualificationId`/`qualificationRecordSha256` equal K (null for pre; exact pair for post) | P0 §11.1 |
| Empty strings | none permitted (all strings are hashes/ids/enums/literals) | P0 §6 rule 9 |
| Enumerations | `phase` pre/post; `topology` b2-lifeline/b3-integrated | P0 §7.1 |
| Content identity | `Hr = SHA256(CJSON1(R))` over exact received canonical bytes | P0 §11.1 |
| Required hash reference | `catalogSha256` = `Hk` | P0 §11.1 |
| Duplicated values (10) | `caseId`, `phase`, `topology`, `hostId`, `artifactSetSha256`, `qualificationId`, `qualificationRecordSha256`, `fixtureManifestSha256`, `fixturePolicySha256`, `actionScriptSha256` — all equal K | P0 §11.1, §7.2 |
| Cross-record consistency | same K anchors as Wc/P; no R↔Wc direct edge beyond `Hr` | P0 §11.5 |
| Rejection conditions | noncanonical bytes; wrong field set/type/literal/version/enum/id; `Hk` mismatch; duplicated-value mismatch; extensions | P0 §6, §11.1 |
| P1 reuse | `CanonicalJson`, `FrozenSchemaRules.IsValidId`, `FrozenContractVerifier.TryVerifyCatalog` | P2 entry owner decision §13 |
| Non-authoritative semantics | no reviewer existence/independence/review-execution/issuance claim | P2 entry owner decision §6 |

## Wc — detached owner receipt content (11 fields)

| Attribute | Requirement | Source |
|---|---|---|
| Schema/version | `schemaVersion` = 1 | P0 §11.2 |
| Exact field set (ascending) | `artifactSetSha256`, `caseId`, `catalogReviewSha256`, `catalogSha256`, `decision`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion`, `topology` | P0 §11.2 |
| Types | as R | P0 §11.2 |
| Required literal | `decision` = `accept-exact-bounded-validation-catalog` | P0 §11.2 |
| Nullable fields | qualification pair equal K | P0 §11.2 |
| Empty strings | none permitted | P0 §6 rule 9 |
| Content identity | `Hw = SHA256(CJSON1(Wc))` over exact bytes | P0 §11.2 |
| Required references | `catalogSha256` = `Hk`; `catalogReviewSha256` = `Hr` | P0 §11.2 |
| Duplicated values (8) | case/phase/topology/host/artifact/qualification pair equal K | P0 §11.2 |
| Rejection conditions | structural violations; `Hk`/`Hr` mismatch; duplicated-value mismatch; extensions | P0 §11.2 |
| P1/P2 reuse | same as R; `Hr` recomputed from verified R bytes | P2 entry owner decision §13 |
| Non-authoritative semantics | no owner identity, signature, acceptance, or issuance claim | P2 entry owner decision §7 |

## P — supplied P-shaped content (12 fields)

| Attribute | Requirement | Source |
|---|---|---|
| Schema/version | `schemaVersion` = 1 | P0 §11.3 |
| Exact field set (ascending) | `accepted`, `artifactSetSha256`, `caseId`, `catalogReviewSha256`, `catalogSha256`, `hostId`, `ownerCatalogAcceptanceSha256`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion`, `topology` | P0 §11.3 |
| Types | `accepted` boolean true; hashes 64 hex; ids/enums as above | P0 §11.3 |
| Required literal | `accepted` = `true` (content requirement, never standalone approval) | P0 §11.3; P2 owner decision §8 |
| Nullable fields | qualification pair equal K | P0 §11.3 |
| Empty strings | none permitted | P0 §6 rule 9 |
| Content identity | `Hp = SHA256(CJSON1(P))`; **no self-hash field** | P0 §11.3 |
| Required references | `catalogSha256` = `Hk`; `catalogReviewSha256` = `Hr`; `ownerCatalogAcceptanceSha256` = `Hw` | P0 §11.3 |
| Duplicated values (8) | case/phase/topology/host/artifact/qualification pair equal K | P0 §11.3 |
| Rejection conditions | structural violations; `Hk`/`Hr`/`Hw` mismatch; `accepted` not true; duplicated mismatch; extensions | P0 §11.3 |
| Reuse | same P1 interfaces; `Hk`/`Hr`/`Hw` recomputed from verified bytes | P2 entry owner decision §13 |
| Non-authoritative semantics | no administrator identity, custody, provision, approval, or eligibility claim | P2 entry owner decision §8, §12 |

## Excluded (never implemented)

V/W/A actors and issuance; trusted acquisition; protected roots; administrator verification; publication/order/durability; withdrawal; pre-B1 rechecks; live challenge/response; invocation authorization; positive validation; protected execution. A structurally valid chain remains operationally untrusted content.
