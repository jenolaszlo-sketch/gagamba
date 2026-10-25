# RC2A-02C documentation and timing review

Reviewed against RC-1 design commit
`d609c29d3048bc0fd91583068cb013fb906fdf90`, the frozen RC2A-01 API contract,
and the RC2A-02 qualification plan bound by the owner disposition.

## Passive reference contract

The [RC-1 §RC-D04 contract](../../design/restricted-consumer-contract-rc1.md#rc-d04--preparation-provider-and-artifact-identity)
defines `RestrictedPreparationReference` as a passive RC-2A value with four
nonempty GUIDs and a manifest digest. Its [frozen public API](../../design/restricted-consumer-contract-rc2a-01-api.md#api-and-failures)
specifies the validating public constructor, exactly 32 digest bytes copied to
`ImmutableArray<byte>`, content equality and hash behavior, and no timing,
control or lookup members. RC-D04 permits copying caller-supplied values from
evidence, including test evidence. It does not require fresh GUIDs, digest
recomputation, attestation or live-operation binding.

The [source XML summary](../../../src/Gagamba.Execution/Restricted/Contracts/RestrictedPreparationReference.cs#L5)
matches that boundary: the value is for passive correlation and carries no
expiry, liveness, establishment or permission to execute. The reflection
inventory and focused assertions confirm the allowed public constructor, five
get-only properties, `IEquatable<RestrictedPreparationReference>`, typed/object
equality and `GetHashCode`. No additional declared public reference member is
present. Inherited public framework methods are listed separately in the
inventory.

## R1-F04 timing semantics

The [RC-1 design's D4a/D4b explanation](../../design/restricted-consumer-contract-rc1.md#rc-d04--preparation-provider-and-artifact-identity)
and the source XML document the same limit:

- D4a timing is supplied as explicit enrolled or synthetic data. It does not
  prove that a native preparation occurred.
- Later D4b timing comes only from an actual provider-origin prepared execution.
- A GUID, digest or constructed passive reference does not establish freshness,
  current liveness, expiry, a deadline or execution authority.

The [RC2A-02 plan's R1-F04 row](../../plans/restricted-consumer-contract-rc2a-02-qualification.md#4-traceability-matrix)
requires the negative temporal-surface and documentation check. The source
comment is the directly shipped consumer-facing statement; the frozen API
contract also states that the reference has no temporal fields. These statements
are internally consistent with RC-D04 and the owner's [scope disposition](../../decisions/gagamba-q2as-rcd04-owner-disposition.md#2-authoritative-requirement-and-exact-scope).

## Qualification limit

This review verifies documentation and the public type's observable API
boundary. It does not qualify D4a enrollment, D4b provider execution, any RC-2B
deadline or release check, or downstream authorization behavior. Those remain
outside RC2A-02C and require their separately assigned evidence.
