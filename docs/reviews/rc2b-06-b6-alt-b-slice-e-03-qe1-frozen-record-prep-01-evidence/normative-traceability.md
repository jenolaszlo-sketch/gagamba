# Source-to-rule and vector traceability

Source IDs and exact bytes are bound in the [source manifest](source-manifest.md)
and its linked predecessor inventory. Vectors are evidence of interpretation
and byte placement.

| Rule ID | Requirement | Source type | Exact source location or calculation | Owner decision | Vectors |
|---|---|---|---|---|---|
| V-01 | Explicit v3 pre, accepted legacy versions remain separate, old parsers refuse | HISTORICAL SOURCE | F1 44–48, AB 42–44/64 | Owner §2 and §3 | J16–17, IE-V10/V12 |
| V-02 | Credential JSON measured in bytes, maximum 8192 and inherited strict shape and number rules | ACCEPTED HISTORICAL DECISION | B2 §2, WIRE §C/D.2 and WI-DELTA §2.1 | Owner §2 | J18, J21–22 |
| V-03 | B2 selector and deadline and integrated selection/location | HISTORICAL SOURCE | F1 46 and inherited B2 §2 / SPEC §6.2.2 | Owner §2 | J01/J02 partial only, FRP-01 |
| V-04 | Base identity fields, digest and generation ties, profile/constants and enclosing base schema | ACCEPTED HISTORICAL DECISION | SPEC §§6.1–6.2.2, WIRE §D.2, B2 §§2–3 | Owner §2 | W03, complete JSON pending FRP-01 |
| BJ-01 | Selected identity uses required Boolean false | NEW OWNER RULING | Normative owner §2 BJ-01 | BJ-01 | J01–05 |
| BJ-02 | Both mandatory literal null members | NEW OWNER RULING | Normative owner §2 BJ-02 | BJ-02 | J01–02, J06–09 |
| BJ-03 | Literal 64 lowercase ASCII hex characters | NEW OWNER RULING | Normative owner §2 BJ-03 | BJ-03 | J01–02, J10–14 |
| BJ-04 | Equality with explicit original capability digest; missing and wrong-width refuse | NEW OWNER RULING | Normative owner §2 BJ-04, F1 24/38, AB 44 | BJ-04 | J01–02, J15/J19–20 |
| W-01 | Inherited envelope axes, role/namespace and context selection | ACCEPTED HISTORICAL DECISION | B2 §3, WIRE §§C–D | Owner §3 | Payload witness context only, IE-V10, W12 |
| W-02 | B2 37 expectation fields and 9/36 exact base, status-only pre delta | HISTORICAL SOURCE | F1 46 and B2 §3. Digests, roles, reserved and offsets in SPEC §§6.2.3/6.2.5 and B2 §3 | Owner §3 | W01–03, W06–09, IE-V11/V12 |
| IE-01 | Integrated absence exactly 76, otherwise one complete pair in pre 9/36 | NEW OWNER RULING | Normative owner §3 IE-01 | IE-01 | IE-V01–04/V09/V10 |
| IE-02 | Two u8 byte counts 1–64, exact consumption | NEW OWNER RULING | Normative owner §3 IE-02 | IE-02 | IE-V02–07/V09 |
| IE-03 | Fixed profile/version UTF-8, exact octet equality | NEW OWNER RULING | Normative owner §3 IE-03, reference profile SPEC §6.2.2 | IE-03 | IE-V02/V03/V08 |
| W-03 | Present total 105, 37 total 246 | MECHANICAL DERIVATION | 76+1+26+1+1, and 219+26+1 respectively from accepted field maps | IE-02/03 for integrated | W03, IE-V02/V03 |
| W-04 | Inherited 10/12 fields, equality, order, codes and refusal | ACCEPTED HISTORICAL DECISION | SPEC §6.2.4, B2 §3, F1 48/58/60 for pre interpretation | Owner §3 | W04–05, W10–14 |
| R-01 | Existing ReleaseRequest digest, exact historical formula | HISTORICAL SOURCE | F2 §5 line 100, AB line 34 and F1 line 54 | Owner §4 | R01–02 field only |
| R-02 | 258-byte preimage, 32-byte result | MECHANICAL DERIVATION | 29+1+4×32+4+4×16+32 bytes, SHA-256 | Owner §4 preserves formula | R01–02 |
| S-01 | Data-only result, no authority or new E-3 release field, milestones separate | ACCEPTED HISTORICAL DECISION | ALT §§3.3–3.5, E2 closure §§5–7, PLAN §3.5, SCOPE §9 | Owner §§4–5 | All witnesses are isolated checks |

The integrated payload size calculations leave the historical outer bounds in
force. FRP-01 is the only preparation blocker identified. No witness settles it.
