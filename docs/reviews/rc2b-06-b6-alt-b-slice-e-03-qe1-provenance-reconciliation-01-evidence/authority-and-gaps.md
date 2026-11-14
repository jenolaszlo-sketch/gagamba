# Authority, discrepancies and owner decisions

Source IDs and locations resolve through [the manifest](sources-and-verification.md).
Historical captions are original evidence. Approval chronology and exact scope
control interpretation.

| Topic | Classification | Finding and precedence |
|---|---|---|
| F02 draft/recheck versus owner acceptance | Editorial and governance chronology | F02 §2 adopts the exact design submission through F-M. AR line 3 records that the historical captions predate owner acceptance. F1 line 48, FR line 21 and FMX C04/Q04 separate v3 wire adoption. ALT §§3.3–3.5 approves the narrower integrity/phase/ordering design and binds AB by hash. SCOPE and E2 explicitly retain E-3 Q-E1 adoption. No later summary supplies the missing field-level adoption. |
| Alternative B compatibility vs actor source | Equivalent reference | AB lines 42–44 delegates the wire delta and exact inherited constraints to F1. F1 locates the referenced corresponding v2 forms. Use the accepted B2 amendment for B2 and the original adopted wire contract with WI-DELTA for integrated. |
| Source captions versus current implementation | No discrepancy | A planned prepublication mode and currently refusing old parsers coexist. Acceptance of design has no effect on compiled v1/v2 dispatch or production authorization. The current `InstallationWire.cs` blob `35d8c5824590d5cea225a315736519d39b7d5889` is a comparison point and cannot settle a v3 gap. |
| v3 JSON identity, status/null and AuthorityHash | Requires owner interpretation | F1 line 46 specifies a 64-hex field, exact pre status in binary, inherited JSON fields and null qualification association. SPEC §6.2.2 defines the legacy JSON Boolean `Qualified` and gives an illustrative rather than exhaustive full bootstrap object. F3 line 21 defines separate capability keys. No verified source fixes how the new v3 bootstrap JSON represents the integrity status, null association and complete pre identity shape, or normatively selects the accepted digest/case rules for `AuthorityHash`. |
| Integrated 9/36 optional profile tail | Requires owner interpretation | SPEC §§6.2.3/6.2.5 describes optional length-prefixed UTF-8 values and a payload of at most 256 bytes. WIRE §C.3 preserves an outer 512-byte ceiling with tighter per-message validation. B2 §3 narrows only B2 to 76 bytes, with integrated v2 outside that amendment. Current source accepts fixed profile values with two u8 lengths for integrated echoes. No accepted v3 decision resolves the full prefix representation/presence policy for exact conformance. The two ceilings can coexist as inner and outer bounds. |
| Message 10/12 semantics | Equivalent representation | F1 line 48 preserves matching verified digest/context. SPEC §6.2.4 and B2 §3 bind confirmation shape and legacy result codes. No evidence argues for adding a pre qualification result or inventing a v3 reason mapping. |
| Release correlation across sources | No discrepancy | F2 line 100 and AB line 34 have the same formula bytes. Both put the result in existing ReleaseRequest. F1 line 54 confirms this and excludes a GAVA release payload. Entry R06 asked for the codec-facing assignment. Exact source inspection narrows that assignment to compliance with the separate format and preserved identity meanings. |
| P0/P1/P2 and E-2 owner | No discrepancy | P0 §12 and D1 §18 leave E-3/E-4 open. E2 §§5–7 retains E-2's bounded adopted contracts and downstream duties. CJSON-1 for K or the F3 capability establishes no required reuse for the v3 bootstrap. |
| Formatting of B2 source | Equivalent representation for reviewed clauses | Git committed bytes match the digest bound in B2-O. Local CRLF checkout differs in raw hash. The commit byte identity governs, and the external-file matches used unmodified bytes. |

## Required owner decisions

1. Bind an exact v3 JSON object schema for each topology against its named
   v2 base. Decide the field name, type and encoding for pre integrity status
   and representation of null qualification association. Bind accepted
   `AuthorityHash` grammar, semantic referent and exact comparison to
   supplied context, including how missing or mismatched data refuses.
   Keep trust acquisition in caller scope. If the rule is already specified
   elsewhere, bind the controlling bytes and exact section.
2. Establish the exact integrated v3 9/36 tail policy: allowed presence,
   length prefix width, canonical accepted bytes and boundaries. Either
   bind a controlling inherited integrated encoding or adopt an explicit
   v3-only resolution. Preserve the distinct B2 76-byte layout.

These are exactness gates for a production-unreachable codec and its Q1
oracle. They require no P1/P2, E-1, F02 or verifier change. The verified
extraction is attached to the reconciliation report. It has no proposed
frozen-specification identity or owner-adoption status. After the separate
ruling and exact record verification, a later owner can decide adoption
and the status of E3-ENTRY-F01. E-3 entry and implementation remain separate.
