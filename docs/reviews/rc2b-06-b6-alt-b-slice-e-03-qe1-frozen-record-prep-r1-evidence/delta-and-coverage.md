# R1 delta inventory and schema audit

The starting [candidate](../../../docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md) is fixed at
commit `0fcd246812ffe1a8d9fa3813caccaf5863773eaf`, blob
`d734d61945192a7c4e2558d949c56c4f75094fdf`. The
[bootstrap-base ruling](../../../docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md)
and its [closed schema](../../../docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md)
are the controlling R1 inputs.

| Candidate section | Classification | R1 action |
|---|---|---|
| Status, §1 authority | CORRECT SUPERSEDED PLACEHOLDER | Record FRP-01 normative resolution and bind the bootstrap owner and schema annex by immutable identity. |
| §2 selection and versions | UPDATE FROM BOOTSTRAP OWNER DECISION | Add the explicit B2/integrated v2-to-v3 presence delta; preserve the envelope paragraph, status and legacy behavior. |
| §3 bootstrap | COMPLETE MISSING CONTENT | Incorporate the full closed schema and operative owner cross-field rules, strict document encoding, complete witnesses and amended refusals. |
| §4 evidence payloads | RETAIN UNCHANGED | Reuse B2 76, integrated 76/105, B2 37, and 10/12 from the original candidate byte for byte. |
| §5 release and scope | RETAIN UNCHANGED except obsolete status | Preserve formula and E-4/E-6 duties. Record preparation complete and Q1/adoption pending. |
| §6 gates | CORRECT SUPERSEDED PLACEHOLDER | Track BB-02 as existing v2 nonconformance. Preserve E3-ENTRY-F01 and independent Q1, adoption and entry gates. |
| Material protocol conflict | No reconciliation identified | The incorporated v3 rules supply the missing contract without changing either prior owner decision. |

## Closed-schema coverage

The normative member-level traceability and null/refusal columns are the
**entire** [schema annex](../../../docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md), blob
`a7248152d09e19d8f3775db397f340949fa9ee57`. Each row is tested for presence
and scalar/child-object type by the named complete positive(s). The annex
separates `H` inherited historical rules, `F` historical F2 semantics, `O`
v3-only owner binding and `BJ` previous owner rules. The table below pairs
all required member rows in that annex with their complete positive and
representative negative controls. It does not imply a negative for every row.

| Object (annex heading) | Rows | Positive | Presence, null and scope | Representative negatives |
|---|---:|---|---|---|
| B2 | 16 | P-B2 | Required once, two designated root nulls and two non-null B2-only members | N03/N06/N11/N13/N21, R1-N01/N02 |
| Integrated | 14 | P-I | Required once, no root nulls, both B2-only names prohibited | N01/N02/N12/N14/N20, R1-N03/N04 |
| IntegratedBody | 8 | P-I | Required non-null, integrated only | N05/N16 |
| RecoveryPin | 2 | Both | Required non-null | Positive only |
| OwnerPin | 2 | Both | Required non-null | Positive only |
| RecoveryHashes | 5 | Both | Exactly the five required non-null role map values | Positive only |
| OwnerHashes | 5 | Both | Exactly the five required non-null role map values | Positive only |
| PreIdentity | 16 | Both | Required once, exactly two literal nulls; Boolean false | N07/N08/N09, R1-N05/N06 |
| Controller | 5 | P-I | Required non-null, integrated only | N05/N16 |
| Runtime | 7 | P-I | Required non-null, integrated only; two uppercase inventory strings by owner rule | N04/N19, R1-N08 |
| Profile | 19 | P-I | Required once, only IntegratedRecovery literal null; no Debug or internal authority fields | N17 |
| Total | 99 | Two full roots, plus P-LIMIT and P-WS | All scalar types resolved, all nested object targets covered | Bounded controls |

Both roots share BJ-03 literal 64-lowercase-ASCII hex and BJ-04 raw capability
comparison. N10/N18 and the original partial-input controls J14/J19/J20
cover grammar, equality and missing/wrong-width comparison evidence. The
latter retain their limited changed-field scope. N15/N22 and R1-N07 record
size, BOM and invalid UTF-8 refusals respectively. R1-N09 isolates byte
counting from JSON schema conformance.

Use the [original rule map](../rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/normative-traceability.md)
for v2 inheritance, IE-01–03, 10/12 and release allocation. Its historical
FRP-01 and partial JSON references describe the original preparation.
R1 replaces that bootstrap gap with the owner annex and complete vectors.
Sections 2–4 of the bootstrap owner decision preserve BJ and IE and bind
strict JSON and all internal field relationships. The fixture script is a
bounded producer check, with further negative cases reserved for independent
Q1. BB-02 stays open and supplies no normative acceptance rule.
