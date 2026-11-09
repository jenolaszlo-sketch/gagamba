# K/M/F/S/P/R/Wc role map and acyclic graph

Date: 2026-11-09 (Asia/Manila)

This map records the roles as the accepted design uses them. It does **not** expand the labels into new acronyms and does **not** adopt field-level schemas into Gagamba. The exact external contract is accepted by hash through the F02 15-file submission; the repository still lacks the Q-E1 authoritative record.

## Role map

| Symbol | Role | Exact source | In-repository state |
|---|---|---|---|
| **K** | Protected catalog binding the exact case, phase, topology, candidate identity, host/platform identity, qualification association, closure hashes, session bound, and release flag | External `case-policy-and-action-contract.md` §1 | No schema, reader, or store |
| **M** | Protected fixture manifest; B2 has an empty object list, B3 has exactly the four fixed roles | External contract §3 | No schema, reader, or store |
| **F** | Protected fixture policy; B2/B3 variant, host paths, copy/invocation/sandbox/resource policy, release/session bounds | External contract §3 | No schema, reader, or store |
| **S** | Protected finite action script; exactly one of three fixed templates; no arbitrary command or callback | External contract §3 | No schema, reader, or store |
| **P** | Protected catalog-eligibility approval at `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`; eligibility only, not a live invocation | External contract §1 | No schema, root, instance, or owner decision |
| **R** | Detached independent review at `/etc/gagamba/validation-catalog-evidence/<Hr>.json` | External contract §1 | No representation |
| **Wc** | Detached owner acceptance at `/etc/gagamba/validation-catalog-evidence/<Hw>.json` | External contract §1 | No representation |
| **Hk / Hr / Hw / Hp** | SHA-256 of exact canonical K / R / Wc / P bytes | External contract §1; `final-acceptance-hash-contract.md` | No in-repository canonical verifier for this chain |

## Field-level boundary

- K contains no review, owner-decision, acceptance-receipt, or self-hash field.
- R contains `catalogSha256` = Hk, the duplicated identity fields, the three fixture/action hashes, and `conclusion` = `supported-for-bounded-validation`.
- Wc contains `catalogSha256` = Hk, `catalogReviewSha256` = Hr, the duplicated identity fields, and `decision` = `accept-exact-bounded-validation-catalog`.
- P contains `catalogSha256` = Hk, `catalogReviewSha256` = Hr, `ownerCatalogAcceptanceSha256` = Hw, the duplicated identity fields, and `accepted` = true.
- The capability and live challenge/response use `catalogReviewSha256`, `ownerCatalogAcceptanceSha256`, and `catalogApprovalSha256` rather than generic aliases.
- The exact field sets and canonical encoding rules are in the external contract §1 and the final-acceptance hash contract. They are not reproduced here as adopted Gagamba schema.

## Acyclic graph

```text
M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability
```

- M is constructed first; F references hash(M); S references hash(M) and hash(F); K references hash(M), hash(F), and hash(S).
- K does not reference R, Wc, or P.
- R reviews completed K and its transitive bytes; R does not contain Wc or P.
- Wc accepts exact K/R bytes; Wc does not contain P.
- P binds Hk/Hr/Hw and is published last.
- No edge returns from R/Wc/P into K/F/S.
- P grants eligibility, not a live invocation. The fresh challenge/response and one-response latch remain necessary.

## Authority boundaries

- P is not producer-built. A must independently verify V/W provenance; observing candidate files cannot create authority.
- Unprotected review text has no authority.
- R/Wc/P do not reuse `/etc/gagamba/accepted-qualifications`; the F02 trust-store decision is store-specific and does not automatically extend.
- The external catalog-contract labels V (reviewer), W (owner accepting exact bytes), and A (root administrator) are not silently mapped onto Slice E's A/L/C/R/O. A separate owner decision must establish the mapping and authority.

## Provenance status

- The external files are local and hash-match the F02/Alternative B acceptances.
- The historical PENDING captions remain untouched; the later committed acceptances govern the semantics they expressly accept.
- The graph is reconstructible and acyclic.
- The graph is not adopted in-repository as Gagamba policy. That is Q-E2.
