# P2 entry assessment — structural versus authority verification

Date: 2026-11-09 (Asia/Manila)

## Categories

**A** pure structural; **B** content binding; **C** provenance/issuer; **D** protected acquisition/publication; **E** live authorization; **F** positive-validation authority construction.

## Check classification (authoritative table)

| Check | Category | P2 | Owner if deferred |
|---|---|---|---|
| CJSON-1 bytes, field sets, versions, types, `ValidId`s, enums, literals, `accepted`=true, pre/post null pair | A | Yes | — |
| Recompute `Hk` from supplied K bytes | A/B | Yes | — |
| R `catalogSha256` = `Hk`; duplicated fields = K | B | Yes | — |
| Recompute `Hr`; Wc `catalogReviewSha256` = `Hr`, `catalogSha256` = `Hk`; duplicated fields = K | B | Yes | — |
| Recompute `Hw`; P `ownerCatalogAcceptanceSha256` = `Hw`, `catalogReviewSha256` = `Hr`, `catalogSha256` = `Hk`; duplicated fields = K | B | Yes | — |
| Cross-record case/phase/topology/host/artifact/qualification equality | B | Yes | — |
| Reviewer identity/independence; human review occurred | C | **No** | D1 governance |
| Owner identity; personal acceptance occurred | C | **No** | D1 governance |
| A's identity/authorization; actual V/W provenance verification | C | **No** | A's root-administrative process |
| P acquired from the protected root with correct modes/no-replace/fsync | D | **No** | E-5/E-6 |
| Publication order; withdrawal/reapproval; freshness | D | **No** | E-6 |
| Live challenge/response; per-invocation authorization | E | **No** | E-6 (with E-4) |
| Capability construction; positive validation authority | F | **No** | E-4/E-6/E-7 + Slice N |

Principles preserved: a self-declared issuer is not proof of authority; a matching hash is not evidence of human acceptance; a syntactically valid protected-approval document is not a protected approval.

## V/W/A separation

- **V:** produces R as an administrative act; R carries no identity field and the contract defines no independence proof — P2 checks structure and `Hk` binding only.
- **W:** produces Wc as an administrative act; Wc carries no identity or signature — P2 checks structure and `Hk`/`Hr` bindings only.
- **A:** verifies actual V/W provenance and provisions P under protected roots — P2 checks supplied P-shaped content and `Hk`/`Hr`/`Hw` bindings only; custody and authenticity stay with A and the protected path.
- P2 may not issue R/Wc/P, authorize catalogs, or convert a consistent evidence chain into an invocation capability.

## Protected P boundary

- **Operation 1 — verify supplied P-shaped bytes:** permissible in P2 as category A/B with a strict non-authority result contract; result names must not imply approval, trust, authorization, eligibility, admission, or execution, and no `VerifyApproval`-style method may imply Operation 2.
- **Operation 2 — verify that P is the authentic protected approval provisioned by A:** requires A's administrative process, protected filesystem custody, and L's protected read/rechecks; deferred to E-5/E-6 and D1 governance.

An owner option exists to split P2-A (R/Wc) and P2-B (P content) if the owner prefers separate evidence cycles; the contracts are the same category, so the assessment does not require the split.

## Trust-anchor and F02 separation

- D1's non-cryptographic administrative model (explicit authority, separately authorized A, protected roots/modes, no-replace, durability, pre-B1 rechecks, per-invocation authorization) is not implemented by P2.
- Threat-model exclusions retained: malicious/colluding root, copied-host rollback, hostile same-UID access where excluded.
- F02 qualification-store authority and the final-admission chain are **separate**; P2 must not reuse F02 records, root authority, or final-admission results as validation-catalog provenance, and must not create a second trust anchor.

## Publication and custody exclusions

R/Wc publication (evidence root, `0444`, no-replace, fsync), P-last ordering, withdrawal serialization, pre-B1 rechecks, and live challenge/response are category D/E and remain with E-5/E-6 (and the A process). R/Wc/P contain no ordering fields, so no serialized record can prove the publication sequence; P2 makes no ordering or custody claim.
