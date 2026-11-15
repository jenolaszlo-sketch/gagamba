# Managed conformance matrix

The adopted v3-pre record, incorporated bootstrap schema and owner entry decision supply the expected behavior. Measured results refer to both freshly built R2 configurations.

| Contract area | Executed controls and source check | Outcome |
|---|---|---|
| Frozen §§2–3 and schema annex | Q1 independently built both complete forms and enumerated all 11 closed schemas and 99 member definitions, covering 46 B2 and 83 integrated occurrences. Required members, wrong kinds, nested closure, scalar domains, topology and version selection were replayed. | Supported. |
| Frozen §3 strict wire grammar | Q1 tested strict UTF-8, invalid JSON, escaped names, BOM, surrogates, exact 8,191/8,192/8,193-byte boundaries, strict scalar representations, whitespace and ordering. Q2 repeated duplicate cases with literal identical keys and escaped aliases at all applicable object instances. | Supported. |
| BJ-01/02 and BB-01 | R1/R2 source leaves these checks unchanged. Replay tests Boolean false, literal null associations, both B2-only fields, and integrated refusal even with literal nulls. | Supported. |
| BJ-03/04 and Q1-E01 | Accept valid escaped AuthorityHash member name, refuse decoded duplicate keys including alternate escapes, and refuse escapes in the digest value. Independent expected digest octet changes and matching controls re-executed. | Supported. Structural agreement confers no authority. |
| Frozen §3 stated projection relationships | Q1 covers every required projection member and relationship. Q2 independently covered six integrated policy claims, ambiguous duplicate sessions, invalid and disposed projections, matching projections and type boundaries. R2 preserves R1 uniqueness and comparison rules. | Supported. |
| Frozen §3 typed semantic comparison, entry §3 bounded refusal | Thirteen committed Q2 cases replayed on both revisions. Q3 adds near-equal decimal paths, alternate same-value number forms, exponents at the supported parser edges, both sides escaped, distinct strings and invalid surrogates. | Supported. No structural data published on refusal. |
| Frozen §4 binary 9 and 36 | Q1 exact roles, lengths, echoes, status-zero and reserved octets. Independent integrated 76 and 105 byte variants and profile length/payload controls retained. | Supported. |
| Frozen §4 B2 37 and confirmations 10/12 | Q1 offsets, scope, prior transition, input errors, peer refusal, digest and original GUID expectations. Q2 checks ownership for all six digest fields across mutable input, returned memory, aliasing and repeat use. | Supported. |
| BB-02 Option A, legacy and downgrade | Original passing legacy controls and pre-v3 refusals replayed both ways. Legacy integrated v2 can still exhibit the known null-member discrepancy. R2 touched no legacy code. | Supported within Option A. BB-02 remains open. |
| Frozen §§4–5 excluded operations | Existing ReleaseRequest opcode refusal, confirmation-only structural result and zero compiled production references. Source inspection found no production authority or transport expansion. | Supported. E-4/E-6 retain their obligations. |

Supplemental cases carry their normative clause in the ledger. They are finite managed controls, run from outside repository test projects. [Q3 Debug](raw/q3-supplement-debug.jsonl) · [Q3 Release](raw/q3-supplement-release.jsonl) · [Q2 Debug](raw/q2-supplement-debug.jsonl) · [Q2 Release](raw/q2-supplement-release.jsonl)
