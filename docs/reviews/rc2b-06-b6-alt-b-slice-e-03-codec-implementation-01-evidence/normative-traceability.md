# Implementation surfaces and normative traceability

| File (relative to the authorized Bootstrap directory) | Purpose | Owner authorization | Dependencies |
|---|---|---|---|
| src: ValidationPreCodec.cs | Bootstrap bounds, schema and comparisons | Entry §3 | .NET JSON, UTF-8 and scalar primitives, internal data |
| src: ValidationPreCodecData.cs | Pre-only context, supplied projections and structural result | Entry §3 | .NET data types |
| src: ValidationPreCodecBinary.cs | Payload-only 9/36/37 and 10/12 validation | Entry §3 | .NET spans and internal data |
| tests: ValidationPreCodecBootstrapTests.cs | Complete, boundary and malformed bootstrap tests | Entry §3 | xUnit, fixtures, strict codec and read-only legacy tests |
| tests: ValidationPreCodecBinaryTests.cs | Exact binary and comparison tests | Entry §3 | xUnit, fixtures, strict codec and read-only legacy tests |
| tests: ValidationPreCodecVectors.cs | Committed owner/Q1 synthetic bytes and independent expectations | Entry §3 | .NET byte and JSON data primitives |

The exact code-and-test index tree qualified before the documentation
was staged is `62f3448400459215d98c52e1ac2d24dbb531184a`. Its parent
baseline before source additions is `1c7fb672055cc3322eb1633d37e5e84d764cb66d`.
The final commit adds the documented results and three status blocks.
The six checked source/test blobs are exact in the final commit.

| File | Qualified Git blob |
|---|---|
| ValidationPreCodec.cs | `e9ac5bfefdc59d3fda2e70c82bf902ccd7f6d361` |
| ValidationPreCodecBinary.cs | `814c233d6290c111d751c73dbe65e1ec7dc0cc23` |
| ValidationPreCodecData.cs | `dbae1d7ed30ec84f1b6b4a78a803b4b43bc1aace` |
| ValidationPreCodecBinaryTests.cs | `558c702fcb26a1ae4349c950dec4b3e1bc1b0d7e` |
| ValidationPreCodecBootstrapTests.cs | `995e67f0b734a982399d011f4c6d0a46d68fc629` |
| ValidationPreCodecVectors.cs | `4880945d6c6177351e929fde15b41babd1529298` |

| Adopted contract | Implementation and executed evidence |
|---|---|
| §§2–3 and BB §4. Eleven closed objects, 99 distinct required member rows. | `Schemas`, `ValidObject`, `ValidScalar`. Object inventory counts are B2 16, Integrated 14, IntegratedBody 8, RecoveryPin/OwnerPin 2 each, RecoveryHashes/OwnerHashes 5 each, PreIdentity 16, Controller 5, Runtime 7 and Profile 19. The shared inventory is tested under both roots. `EveryWitnessMemberIsRequiredAndHasASpecificType` independently omits and replaces each of the 46 B2 and 83 integrated field occurrences (258 refusals inside two xUnit cases). Complete witnesses reproduce 2,693/4,519 bytes and the owner SHA-256s. |
| Strict UTF-8 JSON, scalar domains and inclusive bound | Strict transcoding and one JSON document within 1–8,192 octets, with per-scalar decoded text or exact integer grammar. Managed controls cover BOM, invalid UTF-8 and surrogates, syntax, alternate numeric and invalid types, POSIX path domain and legal escaping, order and ordinary whitespace. Otherwise valid boundary payloads pass at 8,191/8,192 and refuse at 8,193. |
| BB-01/03 and BJ-01/02 | The two roots enforce separate inventories. B2 requires both non-null B2-only members. Integrated refuses either at any value. The selected identity separately requires Boolean false and both literal nulls. Controls cover missing, incorrect-kind and wrong-value cases. Exact context and version are required. |
| BJ-03/04 and Q1-E01 | Decoded ordinal keys from the JSON document are checked for duplicates at every nesting level. The validated AuthorityHash value uses its original JSON token for the 64 lowercase literal hex digits. Tests separately accept the escaped name, refuse a duplicate alternate spelling at root and nested depth, and refuse an escaped digest value. The 32 decoded bytes compare to a separately supplied digest. Controls cover invalid width, omission, mismatch, alternate lexical forms, and an all-zero digest with an equal separately supplied test value. Equality carries no provenance. |
| BB §4 relationships and expectations | `RelationshipsMatch` checks generation and role pin paths, controller and time relationship, runtime path and inventory identity, fixed fixture root, original execution directory suffix, owner DLL and coordinator bindings. `ProjectionMatches` walks the schema for supplied context data. It compares typed leaf values. Protocol constants, required nulls and the separately supplied capability digest have their own checks. The caller supplies original source and policy projections. Missing and mismatched data refuse. |
| §4, IE-01–03 and B2 inherited layout | `ValidatePayload` selects message and topology from supplied context. It checks 76-byte B2 and 76/105-byte integrated forms, exact u8 profile pair, LE constants, role, digest and reserved bytes, status at 71, and the B2-only 246-byte 37 with status at 216. Accepted/failing committed vectors and independent single-field substitutions cover layouts. |
| 10/12 and mixed-version refusal | Message 10 uses the 36-byte response, existing pairs and expected artifact digest. A well-formed refusal has a distinct non-success result. B2 12 uses 48 bytes and the original session GUID and digest. Confirmation uses an explicit supplied predecessor fact. Tests cover wrong state, context, session/digest and truncation/trailing bytes. Legacy managed decoder/echo/37 controls refuse representative v3 while accepted qualified positive controls continue. |
| §5 ReleaseRequest | The existing 258-byte preimage/32-byte field remain at the adopted boundary. An input presented as the existing ReleaseRequest message type refuses as outside this codec. No code computes, parses or compares release correlation. E-4 keeps GAVA1 and the cross-format handoff. E-6 keeps runtime recomputation and atomic enforcement. |

Fixtures are exact literals from owner witness blob
`19886332f3fb1bcf3149f4e173642a8673fbb746` and frozen binary evidence
`c0935d0be9c7619b386f7c5be0edd0f49e778cc9`. Their original synthetic
expectations are kept separate from altered or tested bytes. Q1 controls
are carried forward as executable regression cases. The codec supplies
no expected outputs. See the [independent frozen-record Q1 evidence](../rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/README.md).

The internal bootstrap result owns a JSON object snapshot of validated
structural fields. It has no installation, approval or execution API.
Binary success is content equality under supplied payload context.
Host identity, authenticated INIT, capability retention, catalog,
predecessor evidence and lifetime remain caller duties. No shipping
serializer or frame transport was introduced.
