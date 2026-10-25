# RC2A-02C requirement-to-assertion matrix

Status at matrix commit: **PLANNED — NO RC2A-02C BUILD, TEST OR PROBE HAS RUN.**

This matrix was authored before qualification execution. It covers the public
RC-2A passive `RestrictedPreparationReference` only. The separate RC-2B live
operation, provider/artifact identity, deadlines, retained-state authorization,
release gates, native confinement, packaging and Hufu behavior are excluded.

## Bound baseline and sources

| Item | Identity |
|---|---|
| Owner authorization / current baseline | `91612b9a42827ffdd76891819670623fa43daeed` (`decisions/gagamba-q2as-rcd04-disposition`), parent `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| RC2A-02A acceptance | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` |
| RC2A-02B acceptance | `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| RC-1 source | `d609c29d3048bc0fd91583068cb013fb906fdf90`, `docs/design/restricted-consumer-contract-rc1.md` blob `4b300b01397e7f68d978fadbc36164dda17c8721`, §RC-D04 |
| Frozen public API | `docs/design/restricted-consumer-contract-rc2a-01-api.md`, blob `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` at its accepted binding |
| Qualification mapping | `docs/plans/restricted-consumer-contract-rc2a-02-qualification.md`, §3 Q2A-S, §4 RC-D04/R1-F04 rows, §12 owner assignment |
| Initial source tree | `src` tree `b8c167e3d7340a562221591bdc2b6df7adb372d1` at `91612b9a42827ffdd76891819670623fa43daeed` |
| Initial test tree | `tests` tree `5c3268286575cb55891fae9288627657f2d30ddf` at `91612b9a42827ffdd76891819670623fa43daeed` |
| Initial native tree | `native` tree `85631d2f04512376621d3d09f51d67fd90f45335` at `91612b9a42827ffdd76891819670623fa43daeed` |

The source clause defines four nonempty GUIDs and one 32-byte manifest digest;
the validating public constructor may copy values from evidence; equality is by
field contents; and this public value has no lookup, release, adoption,
deserialization-to-capability or expiry-renewal method. It carries no time or
control authority. RC-D04's RC-2B live-operation requirements are not rows in
this qualification.

## Planned assertions

| ID | Contract requirement and source | API / element | Assertion and expected result | Planned evidence | Status |
|---|---|---|---|---|---|
| Q2A-S-01 | Exactly four nonempty GUID identities and one exactly 32-byte digest; RC-1 §RC-D04 and frozen API §API and failures | Five public get-only properties on `RestrictedPreparationReference` | Reflection records exactly `ExecutionId:Guid`, `IssuerId:Guid`, `GenerationId:Guid`, `PreparationId:Guid`, `ManifestSha256:ImmutableArray<byte>`; no additional public property; a valid 32-byte value constructs | `public-surface-inventory.json`; focused TRX | NOT RUN — PLANNED |
| Q2A-S-02 | Empty GUIDs and digest lengths other than 32 refuse; frozen API requires `ArgumentException` naming the argument | Public constructor | Exercise each empty GUID independently; exercise digest lengths 0, 31 and 33; exercise null byte-array conversion to the empty `ReadOnlySpan<byte>` case; each refuses with the expected argument name; 32 bytes succeeds | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-03 | Constructor may copy values from evidence; digest storage is independent of mutable caller input; RC-1 §RC-D04 | Constructor and `ManifestSha256` | Construct from a mutable byte array, mutate the original array, and read the original 32 bytes unchanged | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-04 | Five-field passive value uses immutable digest content; frozen API specifies `ImmutableArray<byte>` | `ManifestSha256` getter | Reflection confirms the immutable property type; repeated reads return the same content; mutate a separately materialized `ToArray()` result and confirm the reference remains unchanged | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-05 | Equality compares field contents, not collection identity; RC-1 §RC-D04 | `IEquatable<T>`, `Equals` overloads | Independently construct equal values with separately allocated digest inputs; typed and object equality return true | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-06 | Equal values have consistent hash codes; RC-1 §RC-D04 | `GetHashCode` | Equal independently constructed values have equal hash codes | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-07 | Equality covers each GUID and digest content | Five value fields | Change each GUID separately and one digest byte separately; every changed value compares unequal | focused TRX and log | NOT RUN — PLANNED |
| Q2A-S-08 | Only the approved passive public surface is exposed; RC-1 §RC-D04 and frozen API | Exported type and declared members | Inventory the exported contract types and the reference's declared constructors, properties, methods, operators, events, interfaces, nested types and fields. Confirm the reference is sealed; allow only the documented constructor, five get-only properties, `IEquatable<T>`, `Equals` and `GetHashCode`; record inherited `Object` members separately; no unapproved declared public member exists | `public-surface-inventory.json`; focused TRX | NOT RUN — PLANNED |
| Q2A-S-09 | No temporal authority, deadline, expiry or validity decision; RC-1 §RC-D04 and R1-F04 | Exact property/member inventory and XML/API documentation | Confirm no temporal property or method exists; document review confirms reference creation does not imply freshness, liveness, expiry or temporal validity | `public-surface-inventory.json`; `documentation-review.md` | NOT RUN — PLANNED |
| Q2A-S-10 | No provider execution, artifact release, retained-state access or control operation; RC-1 §RC-D04 | Exact declared member inventory | Confirm the reference exposes no provider, execution, release, retained-state or other control member. Interpret as a passive-type API boundary only; make no claim about downstream runtime enforcement | `public-surface-inventory.json`; focused TRX; report limitation | NOT RUN — PLANNED |
| Q2A-S-11 | Constructor may accept values copied from evidence and does not prove live-operation provenance; RC-1 §RC-D04 | Public validating constructor | Successfully construct from caller-supplied nonempty IDs and a 32-byte digest; record that construction adds no provenance or authorization assertion and does not establish live execution authority | focused TRX; `documentation-review.md` | NOT RUN — PLANNED |
| Q2A-S-12 | Constructor need not recompute/attest the manifest digest; RC-1 §RC-D04 and owner disposition §2 | Public validating constructor | Confirm expected behavior is length validation and copying only; no test requires digest recomputation, attestation, GUID freshness/uniqueness, provider binding or live-operation binding | source/API review; `documentation-review.md` | NOT RUN — PLANNED |
| Q2A-S-13 | R1-F04 timing semantics remain outside the passive reference; RC-1 §RC-D04, owner disposition §2, qualification plan §4 | XML/API and consumer timing documentation | Confirm D4a timing comes from explicit enrolled/synthetic data; later D4b timing comes only from an actual provider-origin prepared execution; no timing or liveness is inferred from this reference | `documentation-review.md` with exact linked passages | NOT RUN — PLANNED |

## Execution plan fixed before qualification

The focused Windows managed-code run will execute only the directly mapped
passive-reference assertions in `Gagamba.Execution.Tests`. Any test-only
assertions needed for the listed gaps will be committed before that run. A
reflection inventory will enumerate the real built public surface and preserve
the declared members separately from inherited framework members. The exact
commands, source/test identities, host and SDK, outcomes, TRX/log/exit files,
and hashes will be recorded in this evidence directory.

The run can establish only the passive public value contract and its API
boundary. It does not establish that another component's live preparation,
provider, release or native security behavior is correct. A confirmed contract
failure will be preserved and qualification will stop before production edits.
