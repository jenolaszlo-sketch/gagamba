# E-2 entry reassessment — original Q-E1/Q-E2 reconciliation

Date: 2026-11-09 (Asia/Manila)

Sources: original entry assessment (`docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md` §14/§15/§18); E-2 entry owner decision (`docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md` §§5–8); P0 owner acceptance (`docs/decisions/rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md`); D1 decision (`docs/decisions/rc2b-06-b6-alt-b-slice-e-02-d1.md`).

## Q-E1 — frozen-schema provenance

| # | Item | Record |
|---|---|---|
| 1 | Original finding | No authoritative in-repository frozen-schema provenance record; external inputs accepted by hash only |
| 2 | Original blocking reason | E-2 schema-dependent work could not cite adopted, reproducible representations |
| 3 | Required evidence | Committed provenance record with exact identities, acceptance chain, normative extraction, limitations, conflict rule, reproducible commands |
| 4 | Resolving decision | P0 reconciliation (`77101ac`) + P0 owner acceptance (`5b2ef14`) |
| 5 | Exact accepted scope | CJSON-1 and K/M/F/S/R/Wc/P provenance for the E-2 subset only |
| 6 | Retained qualification | External durability/no external Git history; historical captions; `ValidId` reference; extraction spot-checked not fully re-derived; single-reviewer recording |
| 7 | Remaining dependency | GAVA1 header, v3-pre layouts, release correlation remain open within composite Q-E1 |
| 8 | Satisfied? | **Q-E1: SATISFIED FOR E-2 SCOPE WITH QUALIFICATIONS** |
| 9 | Affects first package? | No qualification blocks a structural contract-verification package; the extraction-completeness residual is a correction path, not a blocker |

## Q-E2 — validation-catalog approval chain

| # | Item | Record |
|---|---|---|
| 1 | Original finding | No owner decision adopting the validation-catalog approval chain; F02 authority does not extend |
| 2 | Original blocking reason | Catalog approval authority, V/W/A mapping, and trust basis were unresolved |
| 3 | Required evidence | Formal owner decision establishing who approves each artifact and under what authority, exact representations/bindings, roots/custody, failure/withdrawal/reapproval semantics, live-binding separation, F02 non-extension, threat model, binding scope |
| 4 | Resolving decision | D1 (`bdc7fa2`): D1 APPROVAL CHAIN ADOPTED WITH QUALIFICATIONS — Q-E2 SATISFIED FOR E-2 SCOPE |
| 5 | Exact accepted scope | Catalog approval governance: V review → W acceptance → A protected approval → publication → L verification → live challenge/response (downstream); F02 separate; no delegation; eligibility only |
| 6 | Retained qualification | V independence procedural; A trusted root; roots unprovisioned/chain unimplemented; refusal encodings deferred; designation process administrative; Mechanism A unselected; native pending |
| 7 | Remaining dependency | Protected roots, readers, custody, live issuance, and native behavior are unimplemented (atomic Slice E / Slice N) |
| 8 | Satisfied? | **Q-E2: SATISFIED FOR E-2 SCOPE WITH QUALIFICATIONS** |
| 9 | Affects first package? | No; the first package excludes protected roots, approvals, and live issuance by design |

## P0 qualification blocking assessment

| P0 qualification | Blocks first structural package? | Reason |
|---|---|---|
| External-package durability / no external Git history | No | The adopted record is committed; the first package verifies content structure, not external publication |
| Historical `PENDING` captions | No | Superseded within their expressed scopes; frozen bytes are untouched |
| S2 `ValidId` grammar reference | No | Resolved in-repository reference; usable as-is |
| Extraction spot-checked, not fully re-derived | No | Any discovered error is an additive correction; structural checks bind hashes, not field-by-field trust |
| Single-reviewer recording | No | Implementation still requires independent qualification |

## D1 qualification blocking assessment

| D1 qualification | Blocks first structural package? | Reason |
|---|---|---|
| V independence procedural | No | First package performs no approval or reviewer verification |
| A trusted root; malicious root/rollback excluded | No | First package has no root interaction |
| Roots unprovisioned / chain unimplemented | No | Explicitly excluded from first package |
| Refusal encodings deferred | No (prerequisite for implementation, not entry) | Encodings arrive with the package and its negative controls |
| Designation/administration process | No | Not required for structural verification |
| Mechanism A durable permits unselected | No | Different issuance mechanism; downstream |
| Native behavior `NATIVE-PENDING` | No | First package is portable and structural; native qualification remains a later obligation for any protected path |

No contradiction was found between P0/D1 and any binding earlier contract. D1 explicitly separates F02 and the final-admission chain; P0 explicitly limits its scope. Both decisions preserve E-1/F-01/F-02/F-03/P4 qualifications.
