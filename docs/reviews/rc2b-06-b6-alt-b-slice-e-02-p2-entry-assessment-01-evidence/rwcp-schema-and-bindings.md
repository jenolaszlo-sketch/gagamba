# P2 entry assessment — R/Wc/P schemas and identities

Date: 2026-11-09 (Asia/Manila)

Normative source: adopted P0 record §11.1–§11.6 (extracted from `case-policy-and-action-contract.md` `667e6118…` §1 and `mechanism-b-session-capability.md` `f5d28dbe…`), adopted by the P0 owner acceptance; D1 decision §§5–7.

## R — detached independent catalog review (13 fields, canonical ascending order)

`actionScriptSha256`, `artifactSetSha256`, `caseId`, `catalogSha256` (= `Hk`), `conclusion` (= `supported-for-bounded-validation`), `fixtureManifestSha256`, `fixturePolicySha256`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`.

- Duplicated identity/fixture/action values equal K; qualification pair follows K's pre/post null rule.
- Contains no owner-acceptance hash, **no reviewer identity field**, no signature, no timestamp.
- `Hr = SHA256(CJSON1(R))`.

## Wc — detached owner catalog acceptance (11 fields)

`artifactSetSha256`, `caseId`, `catalogReviewSha256` (= `Hr`), `catalogSha256` (= `Hk`), `decision` (= `accept-exact-bounded-validation-catalog`), `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`.

- Duplicated values equal K; no fixture/action hashes; no P hash.
- **No owner identity field, signature, or timestamp.**
- `Hw = SHA256(CJSON1(Wc))`.
- Wc is mechanism-neutral eligibility; not invocation or durable-permit authorization.

## P — protected catalog-eligibility approval content (12 fields)

`accepted` (= `true`), `artifactSetSha256`, `caseId`, `catalogReviewSha256` (= `Hr`), `catalogSha256` (= `Hk`), `hostId`, `ownerCatalogAcceptanceSha256` (= `Hw`), `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`.

- Duplicated values equal K; P binds `Hk`/`Hr`/`Hw`; **no self hash**.
- **No administrator identity field, signature, nonce, or timestamp.**
- `Hp = SHA256(CJSON1(P))`.
- Authenticity is a property of protected custody and A's administrative process, not of the bytes: A writes `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` root:root `0444` after independently verifying actual V/W provenance; no tool may generate P merely by observing K/R/Wc candidates.

## Identity matrix (summary)

| Identity | Artifact | Input bytes | References bound | Self hash | Format |
|---|---|---|---|---|---|
| `Hk` | K | exact `CJSON1(K)` | `Hm`, `Hf`, `Hs` (to M/F/S) | none | 64 lower-case hex |
| `Hr` | R | exact `CJSON1(R)` | `Hk` + 10 duplicated K values | none | 64 lower-case hex |
| `Hw` | Wc | exact `CJSON1(Wc)` | `Hk`, `Hr` + 8 duplicated K values | none | 64 lower-case hex |
| `Hp` | P | exact `CJSON1(P)` | `Hk`, `Hr`, `Hw` + 8 duplicated K values | none (explicit) | 64 lower-case hex |

## Binding graph (accepted; unchanged by P2)

```text
M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability
```

K contains no reference to R/Wc/P; R contains no owner hash; Wc contains no P hash; P binds `Hk`/`Hr`/`Hw`; no edge returns into K/F/S; no hash fixed point. P2 verifies these edges over supplied bytes and adds none (no reviewer/administrator identity edges exist in the accepted contracts).

## Structural compatibility with P1

- All R/Wc/P shapes are single-level objects (depth ≤ 2) — unaffected by the accepted depth-8 constraint (P1-Q1-F01).
- No R/Wc/P string field permits an empty string (P1-Q1-F02 compatible).
- `null` appears only in the qualification pair for pre-phase records, matching K's rule (P1-Q1-F03 compatible); unknown fields are rejected by exact field sets.
- Every value is representable in accepted CJSON-1: lower-case hex, `ValidId`, enums, fixed literals, booleans, and integers.
