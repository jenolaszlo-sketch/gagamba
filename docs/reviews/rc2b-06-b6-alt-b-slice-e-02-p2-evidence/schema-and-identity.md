# E-2-P2 implemented schema, identity, and binding evidence

Date: 2026-11-09 (Asia/Manila)

Binding owner decision: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-OWNER-01`
(`docs/decisions/rc2b-06-b6-alt-b-slice-e-02-p2-entry-owner-01.md`, blob `893b48a2…`).

Normative sources: adopted P0 frozen-schema provenance record §11.1–§11.6
(`docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md`,
blob `db25949d…`), D1 decision §§5–7/9 (blob `83fe8d26…`), P2 owner decision
§§6–9, §11–§16.

Implemented artifacts (new files only; all existing files unchanged):

| Artifact | Blob |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceVerifier.cs` | `2b482a70…` |
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceContracts.cs` | `652486d4…` |

## 1. R structural verification (owner decision §6)

Exactly 13 fields, verified against the supplied K after P1 K verification:

| Check | Accepted rule | First-refusal diagnostic |
|---|---|---|
| CJSON-1 exact bytes | P0 §6; codec `CanonicalJson.Parse` | codec diagnostics (see negative matrix) |
| Field set (ascending, exact) | P0 §11.1 | `R field set` |
| `schemaVersion` = 1 | P0 §11.1 | `schemaVersion integer` |
| `conclusion` = `supported-for-bounded-validation` | P0 §11.1 | `R conclusion` |
| `caseId` S2 `ValidId` | P0 §11.1, §16-P0-G3 | `R caseId` |
| `phase` ∈ {pre, post}; `topology` ∈ {b2-lifeline, b3-integrated} | P0 §7.1 | `R phase` / `R topology` |
| hashes lower-case 64-hex | P0 §6 | `actionScriptSha256 hex` … `hostId hex` |
| qualification pair (null for pre; exact K pair for post) | P0 §11.1 | `R qualificationId disagreement` / `R qualificationRecordSha256 disagreement` |
| `catalogSha256` = `Hk` | P0 §11.1; owner §9 | `R catalogSha256 does not bind K` |
| ten duplicated K values | P0 §11.1/§7.2 | `R <field> disagreement` for each |

No reviewer identity, timestamp, or owner hash exists in the frozen field set;
injecting any is a field-set rejection. Structural success makes no claim that V
exists, is independent, performed a review, or issued R (owner §6).

## 2. Wc structural verification (owner decision §7)

Exactly 11 fields; after re-verifying K and R, and recomputing `Hr` from the
supplied R bytes:

| Check | Accepted rule | First-refusal diagnostic |
|---|---|---|
| Field set (ascending, exact) | P0 §11.2 | `Wc field set` |
| `schemaVersion` = 1 | P0 §11.2 | `schemaVersion integer` |
| `decision` = `accept-exact-bounded-validation-catalog` | P0 §11.2 | `Wc decision` |
| `catalogSha256` = `Hk` | P0 §11.2 | `Wc catalogSha256 does not bind K` |
| `catalogReviewSha256` = `Hr` (recomputed) | P0 §11.2; owner §9 | `Wc catalogReviewSha256 does not bind R` |
| eight duplicated K values | P0 §11.2 | `Wc <field> disagreement` |

No owner identity, signature, timestamp, or P hash exists; the `decision`
literal is frozen content, not evidence of human acceptance (owner §7).

## 3. P-content structural verification (owner decision §8, §12)

Exactly 12 fields, supplied-content verification only; after re-verifying K and
R and re-verifying Wc (including its recomputed `Hw`):

| Check | Accepted rule | First-refusal diagnostic |
|---|---|---|
| Field set (ascending, exact) | P0 §11.3 | `P field set` |
| `schemaVersion` = 1 | P0 §11.3 | `schemaVersion integer` |
| `accepted` = true (boolean) | P0 §11.3 | `P accepted is not true` / `accepted boolean` |
| `catalogSha256` = `Hk` | P0 §11.3 | `P catalogSha256 does not bind K` |
| `catalogReviewSha256` = `Hr` (recomputed) | P0 §11.3 | `P catalogReviewSha256 does not bind R` |
| `ownerCatalogAcceptanceSha256` = `Hw` (recomputed) | P0 §11.3 | `P ownerCatalogAcceptanceSha256 does not bind Wc` |
| eight duplicated K values | P0 §11.3 | `P <field> disagreement` |

No self-hash, administrator identity, signature, nonce, or timestamp exists.
No protected-root access, administrator verification, custody check, issuance,
publication, durability, withdrawal, pre-B1 authorization, challenge/response,
or invocation permit is implemented (owner §§8, 12, 24).

## 4. Hk/Hr/Hw/Hp identities (owner decision §9)

- Reused P1 `FrozenContractVerifier.TryVerifyCatalog` first; `Hk =
  SHA256(exact received K bytes)` recomputed from the same bytes passed to K
  verification.
- `Hr`/`Hw`/`Hp` are `SHA256` over the exact received canonical R/Wc/P bytes via
  the accepted `CanonicalJson.Hash` (no reserialization; no trust in asserted
  identity metadata).
- P has no self-hash field: `Hp` is only ever computed from verified P bytes or
  compared against an externally supplied expected identity; it is never demanded
  as an intrinsic P field.
- The graph `M → F → S → K → R → Wc → P` gains no new edges; verification flows
  strictly in the accepted dependency direction.

## 5. Vanguard re-validation and check ordering

Each downstream verifier independently re-validates the preceding records
(`K → R → Wc → P`), so a failure in a preceding record is named by that record's
own rule (verified in
`ReceiptRefusesBindingAndSubstitutionViolations` and the chain negatives). The
binding checks for a record run before its duplicated-value checks, so a
substituted or mismatched reference is always reported as a binding failure,
while a hash-consistent semantic edit is reported as the intended disagreement.

## 6. Non-authoritative result contract (owner decision §16)

Result types (all `internal sealed record`, non-authoritative by name and
documentation): `ReviewRecordContract`, `OwnerReceiptRecordContract`,
`AdminRecordContentContract`, `DetachedEvidenceBindingsContract` (carrying the
P1-verified `CatalogContract`, the three content contracts, and `Hk/Hr/Hw/Hp`).

- Permitted claims: frozen-schema conformance; content identities computed;
  references match; duplicated values consistent; the supplied K/R/Wc/P chain is
  structurally and binding-consistent.
- Forbidden claims remain forbidden: genuine review, owner acceptance,
  administrator approval, protected acquisition/custody, catalog approval or
  eligibility, invocation authorization, positive validation, protected
  execution. No name contains `Approved`/`Authorized`/`Trusted`/`Eligible`/
  `ExecutionPermit`/`ValidationAuthority`/`VerifyApproval` (enforced by
  `DetachedEvidenceReachabilityTests.DetachedEvidenceMemberNamesDoNotImplyAuthority`).
- No promotion method, factory, capability, public API, or approval token exists.
- Design note: the required `accepted=true` literal is verified as a
  precondition, and it is deliberately **not** projected as a result property,
  so no P2 value can be misread as an approval flag (asserted in
  `DetachedEvidenceTests.UntrustedChainIsStructurallyValidButCarriesNoAuthorityProjection`).
  `conclusion`/`decision` are projected only as frozen content strings.

## 7. P1 read-only reuse (owner decision §13)

Used: `CanonicalJson.Parse`, `CanonicalJson.Hash`, `CanonicalJson.IsLowerHex`,
`FrozenSchemaRules.IsValidId`, `FrozenContractVerifier.TryVerifyCatalog`, and
reads of the returned `CatalogContract` values. No P1 file was modified; no
other P1 interface is called (enforced by
`DetachedEvidenceReachabilityTests.DetachedEvidenceCallsOnlyItselfConsumesTheCatalogContractAndUsesTheAuthorizedP1Surface`).
No new canonicalization, hash implementation, or K-contract change was added.
