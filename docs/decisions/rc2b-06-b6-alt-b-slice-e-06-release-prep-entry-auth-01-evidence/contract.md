# Frozen release preparation contract

## Governing representation

F02 case policy §§4–5 and Alternative B compatibility lines 28–36 agree with the [adopted v3-pre record §5](../../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md). The [owner adoption](../rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01.md) makes the latter controlling for its recorded scope. [E-4 owner acceptance](../rc2b-06-b6-alt-b-slice-e-04-owner-01.md) preserves the original identity distinctions and R01 field witness. The [E-6 assessment](../rc2b-06-b6-alt-b-slice-e-06-entry-owner-01-evidence/release-enforcement.md) assigns the pure subset to P1. [Exact input identities](source-identities.json)

Offsets are zero-based and inclusive. Concatenate in ascending offset order, with exactly one domain separator and no padding. Every row is required. SHA-256 consumes all 258 bytes once. The output is exactly 32 raw bytes.

| Byte offsets | Width | Field and encoding | Original source / independent expectation and comparison | Refusal |
|---|---:|---|---|---|
| 0–28 | 29 | ASCII `gagamba-validation-release-v1` | F02 §5 and AB line 34 literal. Caller supplies no domain operand. Compare exact bytes in conformance tests. | Generated deviation fails qualification. |
| 29 | 1 | `00` | Same literal formula. Caller supplies no separator operand. | Generated deviation fails qualification. |
| 30–61 | 32 | `CapabilityHash`, raw | Original sealed capability for this session, independently retained from original L grant/INIT context by C and O in later integration. Compare candidate bytes to the separately supplied original expectation. | Missing/wrong length or unequal bytes refuse. |
| 62–93 | 32 | `CatalogHash` / Hk, raw | SHA-256 of original exact K. Each later actor retains its own validated K association for the exact case. Compare all bytes. | Missing/wrong length or unequal bytes refuse. |
| 94–125 | 32 | `ScriptHash` / Hs, raw | SHA-256 of original CJSON1(S) retained with that K. Independently supplied Hs compares byte-for-byte. | Missing/wrong length or unequal bytes refuse. |
| 126–157 | 32 | `ReleaseActionHash` / A2, raw | F02 §4 action formula on the exact retained CJSON1(S.actions[2]), including index, opcode and timeout, with ASCII `gagamba-validation-action-v1` plus NUL before the element. Both helper operands supply A2 as bytes. Later C and O independently derive from their own original S. Compare all bytes. | Missing/wrong length or unequal bytes refuse. |
| 158–161 | 4 | `RequestedActionIndex`, LE u32 `02 00 00 00` | F02 §§4–5 fixes index 2. Candidate and retained projection must both designate 2 and show release at that position in the complete projected B3 release sequence. | Missing, out of range, wrong action or inconsistent sequence refuses. |
| 162–177 | 16 | `SessionId`, original GUID | Original C session carried in sealed capability, GAVA1 and v3-pre context. Independently supplied retained session equality as Guid. | Missing/empty or unequal refuses. |
| 178–193 | 16 | `ExecutionId`, original GUID | Original C execution bound in the same context and original preparation. Independently supplied retained execution equality as Guid. | Missing/empty or unequal refuses. |
| 194–209 | 16 | `ProtocolLeaseId`, original GUID | Original root protocol lease, E-3 root `Generation` association and GAVA1 lease. Independently supplied retained lease equality as Guid. Store generation, issuer and preparation have separate meanings. | Missing/empty or unequal refuses. |
| 210–225 | 16 | `PreparationId`, original GUID | C's exact retained prepared object and O's own original local preparation/capability identity. Independently supplied same-role preparation equality as Guid. | Missing/empty or unequal refuses. |
| 226–257 | 32 | `PreparationManifestDigest`, raw | Original exact admitted typed preparation manifest digest bound to that preparation. Separate from K, S and fixture manifest M. Compare all bytes against independently supplied original association. | Missing/wrong length or unequal bytes refuse. |

Use ordinary .NET `Guid.TryWriteBytes(Span<byte>)` representation for each 16-byte GUID. The first 4-, 2- and 2-byte parts have their .NET little-endian layout and the final eight bytes keep their order. Use an explicitly little-endian u32 write. Hashes and the SHA-256 result are raw octets. The format has no text-form GUIDs, hex-encoded digest slots, incidental terminators or alternate layout.

## Source and lifetime of supplied associations

`candidate` describes a claimed binding. `expected` is a separate projection populated from independently supplied original expectations using the same shape. Tests must build expected values from independent test-owned inputs, including equal-value controls that use separately allocated arrays. Neither projection may derive its expectation from the other as a qualification witness. The API cannot verify independence, ownership of any live resource or provenance. It applies only the comparisons below.

| Association | Origin and lifetime | Helper check and absence/conflict behavior | Runtime duty retained |
|---|---|---|---|
| Session, execution and root lease | Original C values sealed for the same invocation in the grant and carried in original context. Persist through B2 and relevant B3 progression under original lifetime. | All three independently supplied on each side. Nonempty `Guid` values, compare by matching role. Missing/empty or unequal refuses. | Original peers, protected local context, retention, scope and validity of the IDs. Values from a later echo, new process or store generation cannot replace originals. |
| Preparation and manifest | C retains the original prepared object, O retains its own original admitted preparation/capability and manifest, for that preparation's lifetime. | Independently supply one preparation GUID and one 32-byte original admitted manifest digest per side. Validate as in the byte table, then compare both. | Check exact prepared object, original execution/preparation/generation/manifest, live HELD and owner-local premises. |
| Case | Original case ID selected under accepted K and matching F/S bindings. Retained for the original invocation alongside Hk/Hs/A2. | Independent `CaseId` each side. S2 ValidId: length 1–128 ASCII characters from `[A-Za-z0-9._-]`, first/last cannot be dot. Compare ordinal, case-sensitive. Missing/invalid/unequal refuses. This field has no preimage slot. | Verify K/M/F/S provenance and complete cross-record association. A syntactically valid case ID plus matching supplied hashes cannot establish that these records exist or belong together. |
| Projected script and policy flags | Original S v1 action indices/opcodes and separately retained K/F `allowRelease` facts. Original topology selected under that case. | Independently supply schema version, topology, full index/op projection, both Boolean values and requested action index. Validate each side before comparing. Absence, template/policy conflict and unsupported version or scalar values refuse. | Enforce actual schemas/byte canonicalization, exact S element shape, timeout bounds and independently derive Hk/Hs/A2 with original retained bytes. No runtime clock or manifest/policy verification belongs here. |
| Phase and generation | Original pre or post phase is fixed for original invocation. Pre uses original integrity selection and null qualification pair. Post uses original actual S2 Qualified ID/raw record hash. | Independently supply phase `Pre` or `Post` per side and match. Missing, undefined or contradictory phase refuses. Phase and generation add zero preimage bytes. No store generation, receipt or qualification pair enters this API. | Verify genuine v3-pre versus qualified v2 with validation sideband, phase-specific original selection and fresh final generation check. Stop on phase/receipt/selection change and require a fresh approved invocation where applicable. |

`CaseId`, schema, topology, phase, step slots and booleans are local comparison context. Their encodings are never appended to the preimage. Hk/Hs and A2 come in as raw digests. The helper does not parse K/M/F/S, canonicalize JSON, infer action hashes from step slots, check timeoutNs, resolve origins, or admit a runtime phase. An agreeing change to both projections may pass a structural check. `Compare` detects such a change to a hashed component only if the independent digest expectation remains anchored to the original. Neither method can authenticate allegedly independent inputs.

## Finite structural projection

Case grammar follows the [P0 accepted S2 ValidId reference](../rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md) and current `FrozenSchemaRules.IsValidId` shape. F02 §4 supplies the three existing ordered sequences:

| Topology | Indexed steps in order | `CatalogAllowsRelease` and `FixturePolicyAllowsRelease` | Release-preparation result |
|---|---|---|---|
| B2Lifeline | 0 ConnectB2, 1 Inspect, 2 Stop, 3 Cleanup | Both false | `NoReleaseTemplate` |
| B3Integrated | 0 PrepareB3, 1 Inspect, 2 Stop, 3 Cleanup | Both false | `NoReleaseTemplate` |
| B3Integrated | 0 PrepareB3, 1 Inspect, 2 Release, 3 Inspect, 4 Stop, 5 Cleanup | Both true | Continue only for `RequestedActionIndex == 2` |

The helper checks this enumerated index/op projection only. Topology, length, each position, pair of policy facts and requested release position have separate checks. Recognized non-release projections are refusal witnesses. Excess, missing, reordered or duplicate slots and wrong policy facts refuse. Pre and Post both support the same B3 release projection. No new topology/phase rule is introduced.

## Managed API and ownership

All declarations are internal to namespace `Gagamba.Execution.Linux.M1.Validation` in the one authorized source file. Type names, member names, types and visibility below are contractual. Constructors and properties for data carriers are internal and get-only. The helper has no public surface. The following signatures specify the entry points for the next commission.

```csharp
internal static ValidationReleasePreparationStatus Prepare(
    ValidationReleaseProjection? candidate,
    ValidationReleaseProjection? expected,
    out ValidationReleaseMaterial? material);

internal static ValidationReleasePreparationStatus Compare(
    ValidationReleaseProjection? candidate,
    ValidationReleaseProjection? expected,
    byte[]? expectedCorrelationSha256,
    out ValidationReleaseMaterial? material);
```

Both methods belong to internal static `ValidationReleasePreparation`. Internal sealed `ValidationReleaseProjection` takes, in the order shown, the values named in this table. There are no implicit or default argument values. Optionality in the managed types permits explicit bounded refusal.

| Internal property | Exact managed type |
|---|---|
| `SchemaVersion` | `uint?` |
| `Phase` | `ValidationReleasePhase?` |
| `Topology` | `ValidationReleaseTopology?` |
| `CaseId` | `string?` |
| `CatalogAllowsRelease` | `bool?` |
| `FixturePolicyAllowsRelease` | `bool?` |
| `Steps` | `ValidationReleaseStep[]?` |
| `RequestedActionIndex` | `uint?` |
| `CapabilityHash` | `byte[]?` |
| `CatalogHash` | `byte[]?` |
| `ScriptHash` | `byte[]?` |
| `ReleaseActionHash` | `byte[]?` |
| `SessionId` | `Guid?` |
| `ExecutionId` | `Guid?` |
| `ProtocolLeaseId` | `Guid?` |
| `PreparationId` | `Guid?` |
| `PreparationManifestDigest` | `byte[]?` |

`ValidationReleaseStep` is an internal readonly struct with internal constructor `(uint index, ValidationReleaseOpcode opcode)` and internal get-only `Index` and `Opcode`. The internal enums have byte underlying type:

- `ValidationReleasePhase`: `Pre = 1`, `Post = 2`.
- `ValidationReleaseTopology`: `B2Lifeline = 1`, `B3Integrated = 2`.
- `ValidationReleaseOpcode`: `ConnectB2 = 1`, `PrepareB3 = 2`, `Inspect = 3`, `Release = 4`, `Stop = 5`, `Cleanup = 6`.
- `ValidationReleasePreparationStatus`: `MissingInput = 0`, `UnsupportedVersion = 1`, `InvalidScalar = 2`, `InvalidCaseId = 3`, `InvalidLength = 4`, `InvalidTemplate = 5`, `NoReleaseTemplate = 6`, `CaseMismatch = 7`, `ProjectionMismatch = 8`, `MissingExpectedDigest = 9`, `DigestMismatch = 10`, `StructuralMatch = 11`.

These values are local API discriminants, with no wire, journal or protocol refusal encoding. Internal sealed `ValidationReleaseMaterial` exposes only internal get-only `ImmutableArray<byte> Preimage` and `ImmutableArray<byte> CorrelationSha256`. Its internal constructor takes `(ImmutableArray<byte> preimage, ImmutableArray<byte> correlationSha256)`. Its sole authorized call site is the helper's checked material publication path, with freshly owned immutable bytes of the exact widths. The result type has no factory or conversion members. The result conveys structural data only.

`Prepare` requires both complete projections, calculates the preimage and digest after agreement and returns the material. `Compare` additionally requires the complete third operand `expectedCorrelationSha256` from the independent expectation source. A null or wrong-length expectation in `Compare` always refuses. There is no optional flag that bypasses digest comparison on that entry point. Both return `StructuralMatch` only with a complete result for their stated operation. All refusals return a status and null material, including a mismatch after scratch calculation.

Input carrier arrays are caller-owned mutable references. Validate lengths before copying or indexing. Each invocation takes local bounded copies of both step arrays, all ten digest arrays and, for `Compare`, the 32-byte digest expectation. Compare, serialize and hash only those copies and the immutable scalars. Never retain the projection or any caller buffer on return. Result arrays have exclusively owned backing storage exposed only through normal immutable APIs. Sequential reuse and between-call mutations are supported. Distinct calls against stable arrays may run concurrently. Callers must keep their arrays stable until the call returns and use normal managed ownership APIs. Simultaneous writes to caller memory, unsafe aliasing and resource exhaustion are outside the deterministic malformed-input domain. The API accepts no resource-backed or disposable context.

Permitted dependencies are .NET managed value/string/array/span/immutable collection primitives, `BinaryPrimitives` for LE encoding, `Guid`, and `SHA256` with optional `CryptographicOperations.FixedTimeEquals` for equal-width raw digests. Equality is exact in all cases, with no trust gained from timing behavior. Re-express only the accepted small ASCII case-ID predicate and fixed projected sequences locally. No existing E-1–E-5 callable verifier, codec, model, IO/clock, environment, native, assembly-loading or production-dispatch dependency is authorized. Both authorized files compile through the existing SDK item globs. Existing M1 Tests friend access is declared in `Execution/TransactionStateModel.cs`. The Owner and Recovery assemblies also have existing friend access, which is one reason visibility alone cannot prove isolation.

## Deterministic refusal and evaluation order

Set material to null before validation. Use the first failing row in this order. Check the two projection objects first, then run rows 2–7 on the candidate and then on the expected projection. Within a row any failure returns that row's status. For an invalid opcode or index slot, report the template row. Copy only bounded arrays, as soon as their bounds are established, before reading their contents. No refusal produces any usable bytes, partial result, logging or external state change.

| Order | Condition | Status | Source and rationale |
|---:|---|---|---|
| 1 | Either projection object is null | `MissingInput` | P1 requires independent operands. |
| 2 | Any required property null, including a missing typed scalar, step array or hashed operand | `MissingInput` | P1 completeness and F02 original association. No defaults. |
| 3 | `SchemaVersion` differs from 1 | `UnsupportedVersion` | F02 §4 S v1. `Prepare` and `Compare` accept no raw GAVA/bootstrap version. |
| 4 | Phase/topology outside defined values or an original GUID equals `Guid.Empty` | `InvalidScalar` | F02/AB phase context and E-3/E-4 original identities. Typed scalars have no raw text or fallback interpretation. |
| 5 | Non-null case ID fails S2 ValidId | `InvalidCaseId` | P0-Q3 and accepted case binding. No trimming or case folding. |
| 6 | Any hashed input length differs from 32, or step array length is neither 4 nor 6 | `InvalidLength` | Adopted §5 digest widths and finite F02 §§4–5 templates. No slicing or padding. |
| 7a | Unknown op, sequence or explicit indices do not match topology's exact template, or either policy flag disagrees with the matched template | `InvalidTemplate` | F02 §4 mandatory contiguous indices, exact ops and both allowRelease facts. |
| 7b | Complete and consistent B2 or B3 no-release template | `NoReleaseTemplate` | F02 §5 and AB physical-release gate. Reject irrespective of the requested index value. |
| 7c | Complete release template with `RequestedActionIndex != 2` | `InvalidScalar` | F02 §5 exact index, including unsigned boundary values. |
| 8 | Individually valid case IDs differ ordinally | `CaseMismatch` | Independent exact original K/F/S case association. |
| 9 | Other projected fields disagree, including phase, ordered steps, either policy flag, index, any GUID or any raw digest | `ProjectionMismatch` | P1/RE01–RE03 require independent original association and exact comparison. |
| 10 | `Compare` supplied null digest expectation | `MissingExpectedDigest` | P1 bounded compare; adopted §5 approval-correlation presence for validation. |
| 11 | `Compare` supplied a non-null digest of any length other than 32 | `InvalidLength` | Adopted §5 exact raw digest width. Check before copying. |
| 12 | `Compare` supplied exact-width expected digest differs from the digest calculated by the helper | `DigestMismatch` | Adopted §5 exact digest and R02 witness. Material remains null. |
| 13 | All required checks for the called method hold | `StructuralMatch` | Publish immutable exact 258/32 material only here. |

Width checks impose no unstated digest content rule, including a prohibition on all-zero byte strings. Original GUIDs have named roles and nonempty value requirements. Presence of each role and equality to that role's independent expectation are the checks. The implementation cannot use equal GUIDs in different slots as a substitute for a missing expectation. There is no input for a caller-supplied preimage or template byte length. The fixed output length is an implementation invariant. An internally generated wrong length is a programming defect and fails qualification. Expected malformed managed inputs take the bounded refusal path without exceptions. Unexpected implementation or platform failures propagate without publication or partial success. No blanket catch, clamping, normalization, retry behavior, stateful latches or diagnostics carrying operand bytes.

## Existing request representation and runtime boundary

The existing `ReleaseRequest.ApprovalCorrelationSha256` carries the 32 raw digest bytes. `IntegratedB3Client.Release` passes `!correlation.IsDefault` to `OwnerHeldWireCodec.RequestFromIdentity(..., correlationPresent)`. The adapter produces a non-null byte array or null in `OwnerReleaseRequestWire.ApprovalCorrelationSha256`. This is the existing effective `HasApproval` indication for validation. There is no separate JSON Boolean, new GAVA opcode or alternate digest field. The ordinary codec's optional-null behavior is preserved. P1 adds no wire adapter, decoder, sender, or call from `HeldReleasePhase` or `ReleaseArbiter`.

Future O validates original authenticated C and local original preparation, acquires actor-local protected policy and independently recomputes using its original values. It verifies the existing approval indication and exact request digest even when a sender supplies mutually agreeing data. Only that later separately qualified atomic path may enforce HELD, original deadlines, unused local release slot, phase-specific generation at the final effect boundary, sole O writer and at most one physical attempt. R/C ordering, stop service and generation remain under the accepted plan. The new helper does not acquire a writer or perform any of those effects. [Plan §§3.5–3.6](../../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) · [Release enforcement and wire reconciliation](../rc2b-06-b6-alt-b-slice-e-06-entry-owner-01-evidence/release-enforcement.md)
