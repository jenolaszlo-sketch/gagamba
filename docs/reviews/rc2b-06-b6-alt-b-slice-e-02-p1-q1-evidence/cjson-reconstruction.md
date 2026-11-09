# P1-Q1 — independent CJSON-1 reconstruction and limit analysis

Date: 2026-11-09 (Asia/Manila)

Normative source: adopted P0 record §6, extracted from `final-acceptance-hash-contract.md` §"Canonical encoding CJSON-1" (`49d566e1…`); the F02 codec (`FinalAcceptanceCodec`) is a comparison reference, not the authority.

| Accepted rule (P0 §6) | Precise meaning | Implementation (`CanonicalJson`) | Independent control | Classification |
|---|---|---|---|---|
| 1. UTF-8, ASCII only | Every byte 0x20–0x7E | byte scan before parse | non-ascii, raw-control, BOM reject | conforms |
| 2. No BOM/whitespace/trailing newline | No insignificant bytes anywhere | byte scan + round-trip equality | leading/trailing space reject as noncanonical; newline/BOM as non-printable | conforms |
| 3. Exactly one root object | Root `{...}`, no extra content | parse + root-kind check | root-array reject `CJSON-1 root object`; `{} {}` reject | conforms |
| 4. No duplicate/unknown/missing keys | Field sets are schema rules; duplicates are encoding errors | strictly ascending keys at codec; `RequireFields` at schema | duplicate reject; K extension reject `K field set` | conforms |
| 5. Ascending keys, recursively | Ordinal ascending | recursive walk + round trip | key-order-wrong reject | conforms |
| 6. Printable strings; `\"`/`\\` only; `/` literal; no `\u` | Escape set is exactly quotes/backslash | string walk + round trip | backslash/quote escapes accept; `\u0041`, `\/` reject | conforms |
| 7. Canonical unsigned integers | No sign/leading zero/exponent/fraction; 0..2^63-1 | `TryGetInt64` + raw-text equality | 0/max accept; `01`, `-1`, `1.5`, `1e2`, overflow reject | conforms |
| 8. Booleans literal; `null` only where allowed | Codec-level value; schema-level position | codec permits `null`; K/F enforce positions | null accept structurally; K pre/post and F B2 rules enforced | conforms (schema-scoped) |
| 9. Arrays retain order | No sorting of arrays | order-preserving walk + exact sequences | argv/namespaces/store order checks | conforms |
| 10. Parse→serialize byte equality | No normalization | `Utf8JsonWriter` round-trip check | 6 noncanonical byte forms reject | conforms |
| 11. Empty strings only where permitted | No E-2 field permits one | codec rejects empty strings | `{"a":""}` reject `CJSON-1 string` | conforms for the scoped artifacts; matches F02 |
| 12. ≤ 65,536 bytes | Hard limit | length check | 65,536 accept; 65,537 reject `CJSON-1 size` | conforms (explicit rule) |
| 13. SHA-256 over exact bytes; 64 lower-case hex | No normalized substitute | hash after equality proof; lower-hex helper | known vectors + F02 hash parity | conforms |
| 14. No self-approval field | K has no approval hash | exact K field set excludes them | authority-field injection rejects | conforms |

## Size and depth limits — classification

- **Size 65,536 bytes** is an explicit frozen-contract rule (P0 §6 rule 10). Independent boundary probes: exactly 65,536 bytes accepted; 65,537 rejected. No qualification.
- **Depth 8** is **not** stated in P0 §6. Exact measured boundary: root object plus 7 nested containers accepted; 8+ nested containers rejected as `invalid CJSON-1` (the same `MaxDepth = 8` behavior as the accepted F02 codec). The deepest accepted E-2 shape is far below this bound (F: root → `sandbox` → `writableStores` array → store object → values = depth 4; M/S/K ≤ 3). Classification: **bounded implementation constraint** (resource safety, F02-consistent); it rejects only canonical inputs deeper than any accepted artifact shape. Retained as a qualification; no remediation required. If a future schema needed deeper nesting, the bound would require reconciliation before use.

No divergence from the accepted CJSON-1 rules was found. No accepted input shape is rejected by the implementation, and no noncanonical input is accepted.
