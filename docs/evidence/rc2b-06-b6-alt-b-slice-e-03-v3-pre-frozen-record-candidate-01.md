# Alternative B Slice E-3 v3-pre frozen record candidate

**PROPOSED FROZEN RECORD — NOT INDEPENDENTLY QUALIFIED OR OWNER ADOPTED**

Preparation status: partial candidate with a blocking inherited-bootstrap
schema gap. This document is an assembly of settled rules for review.
The complete bootstrap acceptance oracle is pending resolution of FRP-01.

## 1. Authority and scope

The [normative owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md)
at commit `f12a84943ea30153ccf96dfef21866a78e3fb934`, blob
`f3639dc619dbfee39859afb7da553e2167d9b83d`, settles BJ-01–04 and IE-01–03
for v3-pre. The [source manifest](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/source-manifest.md) binds raw historical
inputs and the accepted v2 bases. Inherited rules follow the corresponding
accepted B2 or integrated v2 contract, with only the v3 delta and the explicit
owner rules below. The record and its vectors are derived evidence.
[Normative traceability](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/normative-traceability.md) identifies the
authority for each rule and the associated vectors.

This scope is a data-only, internal E-3 codec contract. Authenticated INIT,
verified original capability and matching pre catalog are future caller
preconditions to positive bootstrap decode. Selecting the pre layout does not
establish them. E3-ENTRY-F01 remains open. Implementation requires later
independent verification, formal adoption and a separate entry decision.

## 2. Version and topology

| Format | Selection and representation | Refusal and compatibility |
|---|---|---|
| Recovery bootstrap v3-pre, B2 | Case-sensitive top-level numeric token `"Version":3`, string `"AuthorityMode":"validation-pre"`; B2 owner selector exactly `--b2-lifeline 3` | Refuse v1/v2, unknown version/mode, alternate version spelling, mismatched topology, mixed validation versions or missing context. No fallback. |
| Recovery bootstrap v3-pre, integrated | Same top-level version/mode. Selection comes solely from authenticated bootstrap | Same refusal. Keep integrated identity location. Do not accept a B2 identity location in its place. |
| Inherited v1/v2 | v1 remains bounded unqualified legacy; v2 remains qualified and is used for postvalidation with separate context | Old decoders refuse the v3 version and pre status. Accepted v1/v2 encodings and gates are unchanged. A phase or receipt change never upgrades an existing pre session. |
| Evidence payloads 9, 36, 37 | Selected v3-pre context, inherited channel, direction and role. Payloads have no new version member. Status 0 as specified below | Refuse status 1 or any other byte in pre. A matching payload does not select its own version or authorize mixed-version use. 37 is B2 only. |
| Confirmations 10/12 | Inherited payloads within independently selected context; 12 is B2 only | Legacy context is not inferred from matching bytes. No new confirmation status or version field. |

The inherited M1OP version is u16 LE 1, separate from bootstrap JSON version
and identity protocol constants. B2 §3 binds the 48-byte envelope with magic
u32 LE at 0, version at 4, type u8 at 6, zero u8 at 7, execution and lease
GUIDs at 8 and 24, u32 LE sequence and payload length at 40 and 44,
respectively. RecoveryWire applies its inherited byte-0 XOR `20` hex.
B2 does not use GID1. Integrated keeps its accepted transport, with 9/10 on
recovery control and owner 36 in the existing integrated direct namespace.
The payload vectors below exclude envelopes. This record assigns no framing,
socket, descriptor handling or GAVA1 implementation to the isolated codec.

## 3. Bootstrap JSON

The input is the strict UTF-8 JSON byte payload on the credential channel.
Measure actual payload bytes before parsing, exclusive of transport metadata.
8,192 bytes satisfies the inclusive size gate and still requires valid
complete content. Greater lengths, empty input and truncated JSON refuse.
Version and inherited canonical integer rules apply to numbers. Member names
are case-sensitive. Duplicate and unknown members refuse. Member order and
ordinary JSON whitespace convey no identity. There is no general CJSON-1
or canonical reserialization requirement. BJ-03 imposes its own literal
string-token rule independently of ordinary JSON string decoding.

| Location/member | Type, presence and constraints | Authority |
|---|---|---|
| Top-level `Version` | Required integer token `3` | F1, BJ owner §2 |
| Top-level `AuthorityMode` | Required JSON string with exact case-sensitive value `validation-pre` | F1, BJ owner §2 |
| Top-level `AuthorityHash` | Required once. JSON string with exactly 64 literal ASCII `0–9a–f` characters in the token interior. No escaping, whitespace, prefix, separator, padding, uppercase or alternate representation | BJ-03, F1 |
| B2 `Integrated`, `DelegatedParent` | Literal null in the B2 base shape | B2 §2, inherited by owner §2 |
| B2 `B2InstallationIdentity` | Required selected pre identity object | B2 §2, owner §2 |
| B2 `B2DeadlineNs` | Positive canonical signed i64 JSON integer, original absolute monotonic ns. A stale deadline cannot progress at the caller | B2 §2 |
| Integrated `Integrated.InstallationIdentity` | Required selected pre identity object; no B2 identity location | SPEC §6.2.2, WIRE §D.2, owner §2 |
| Integrated `B2InstallationIdentity`, `B2DeadlineNs` | Accepted B2 §2 excludes both from integrated. FRP-01 records the difference between that exclusion and current serializer output | B2 §2, inherited by owner §2 |
| Remaining inherited objects and members | Preserve corresponding accepted v2 base. Complete accepted root and nested wire schema needs the source binding or decision in FRP-01 | F1 line 46, owner §2 |

### Selected identity object

Each listed member is required once at the selected location. Preserve
strict identity shape, match the original expected identity and refuse
duplicates, unknowns, wrong type, wrong placement or mismatches.

| Members | Exact rule |
|---|---|
| `ArtifactSetSha256`, `OwnerClosureSha256`, `RecoveryClosureSha256`, `CoordinatorSha256`, `SeccompFilterSha256`, `RuntimeInventorySha256`, `Generation` | Inherited lowercase 64-hex digest strings for original set, corresponding loaded role closures, coordinator, filter, manifest runtime inventory and generation. `Generation` equals `ArtifactSetSha256`. Refuse malformed, empty or all-zero identity and mismatch under inherited rules. |
| `ManifestSchema`, `OwnerProtocol`, `CoordinatorProtocol`, `Bootstrap` | Canonical JSON integers, each 1 under SPEC §§6.1–6.2. `Bootstrap` here is the inherited identity constant. |
| `ProfileId`, `ProfileVersion` | Exact case-sensitive JSON strings `ae4-linux-fixed-fixture-v1` and `1`, matching the original expectation. |
| `Qualified` | JSON Boolean false only (BJ-01). Omission, null, numeric zero, string false, true and differently cased aliases refuse. The separate binary field is a u8 zero. |
| `qualificationId`, `qualificationRecordSha256` | Both literal JSON null, within this same object (BJ-02). Reject omission, relocation, differently cased names and any non-null value, including empty string/object or zero digest. |

BJ-04 supplies a separately provided 32-byte comparison value. Decode the
literal AuthorityHash hex into 32 bytes and require equality over all bytes.
Missing, wrong-width or mismatched expectation refuses without a validated
result. The codec performs no digest recomputation or expectation derivation
from the bootstrap. A future authorized caller authenticates the original
spawn-local INIT, independently verifies SHA-256 over the original retained
capability bytes against its capability digest, and establishes the matching
pre catalog. A catalog, artifact, release or detached evidence digest cannot
stand in for the capability digest. [Changed-field and boundary vectors](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/bootstrap-vectors.md)

## 4. Evidence payloads

Offsets below count from zero within the payload. Hashes are raw digest
bytes, and comparisons use separately supplied original expectations.
Refuse invalid lengths and any wrong identity, role, constant, status
or reserved value. No undocumented padding is accepted.

| Offset | Width | 9 Recovery InstallationEchoRequest and 36 OwnerInstallationEcho |
|---:|---:|---|
| 0 | 32 | Original expected artifact-set SHA-256, also binding installation generation |
| 32 | 32 | Sender-role closure SHA-256 (recovery for 9, owner for 36) |
| 64 | 2 | Owner protocol, u16 LE = 1 |
| 66 | 2 | Coordinator protocol, u16 LE = 1 |
| 68 | 2 | Bootstrap protocol, u16 LE = 1 |
| 70 | 1 | Role, u8 = 0 for 9 or 1 for 36 |
| 71 | 1 | Pre status, u8 = 0 |
| 72 | 4 | Reserved zero bytes |

B2 is exactly 76 bytes with no tail. Integrated permits exactly the same
76-byte base or exactly 105 bytes with the one following pair, for 9 and
36 only in selected integrated v3-pre (IE-01–03). Presence is determined by
length alone. The independent profile expectation applies to both forms.
On absence, there are no tail bytes to compare.

| Offset | Width | Integrated present profile pair |
|---:|---:|---|
| 76 | 1 | Unsigned byte count `1a` hex (26) |
| 77 | 26 | Exact UTF-8 `ae4-linux-fixed-fixture-v1` |
| 103 | 1 | Unsigned byte count `01` hex (1) |
| 104 | 1 | Exact UTF-8 `1` (hex `31`) |

Each prefix counts its own string bytes, excluding prefixes and envelope.
Generic prefix bounds are 1–64. Check remaining space before indexing or
adding lengths. The fixed pair determines both prefix values. Refuse empty,
partial, duplicate or trailing data, incorrect prefix bounds, alternate
profile values or equivalent text with different octets. Compare original
octets to the expected profile/version. No normalization, alias or added
digest. 76 + 1 + 26 + 1 + 1 = 105, within the historical 256-byte integrated
payload limit and 512-byte outer echo ceiling.

### B2 owner expectation 37

| Offset | Width | Field |
|---:|---:|---|
| 0 | 16 | Nonempty original session GUID in existing .NET byte order |
| 16, 48, 80, 112, 144, 176 | 32 each | Original artifact set, owner closure, recovery closure, coordinator, seccomp filter, runtime inventory SHA-256, in that order |
| 208, 210, 212, 214 | 2 each | LE u16 manifest schema, owner protocol, coordinator protocol and bootstrap constants, each 1 |
| 216 | 1 | Pre status u8 = 0 |
| 217, 218, 244, 245 | 1, 26, 1, 1 | ID count `1a`, exact printable ASCII profile ID, version count `01`, ASCII `1` |

Inherited variable-field layout is ID count at 217, ID at 218, version
count at 218 + ID length and version at 219 + ID length. Each length
1–64; total = 219 + both lengths, 246 for the bound pair. Generation
equals the artifact set with no second slot. Require exact expected fields
and no trailing bytes. 37 is confined to the accepted B2 owner evidence
context. The v3 delta on the corresponding legacy binary form changes
only status 216.

### Confirmations

| Message | Exact fields | Interpretation |
|---|---|---|
| Recovery 10, C→R | 36 bytes. Success u8 at 0, reason u8 at 1, zero bytes at 2–3, raw expected artifact digest at 4–35. Success pair (1,0); refusal pair (0,1..5) with inherited reasons digest, closure, protocol, generation, qualified | Success requires the original expected digest and selected context after 9. A valid refusal encoding cannot progress. Reject malformed pairs, wrong digest or reserved bytes. No v3 remapping of reason 5. |
| B2 Recovery 12, C→R | 48 bytes. Original session GUID at 0–15 in existing .NET order, original expected raw artifact digest at 16–47 | Positive only after verified B2 owner 36 and before Ready under B2. Both fields must match. Integrated has no 12. |

10/12 confirm matching digest/context and convey no S2 qualification.
No additional digest, profile-tail hash, retry rule or wire result code
is introduced. Transport ordering/lifetime and stop remain caller duties.
[Exact binary vectors](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/binary-vectors.md)

## 5. Release correlation and scope exclusions

F2 §5 line 100 and AB line 34 give the same historical formula. Concatenate
ASCII `gagamba-validation-release-v1`, a zero byte, raw
`capabilityHash[32]`, `Hk[32]`, `Hs[32]`, `A2[32]`, LE u32 action index 2,
session/execution/lease/preparation GUIDs in existing .NET byte order
(16 bytes each), and raw `preparationManifestDigest[32]`. The 258-byte input
is hashed with SHA-256. The resulting 32 raw bytes belong to the existing
`ReleaseRequest` approval-correlation field. Runtime validation requires
the existing approval indication and exact digest. This is a release-stage
contract. E-3 introduces no release field, digest computation or comparison.
[Release field witnesses](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/binary-vectors.md#release-field-only)

| Requirement | E-3 responsibility | Future responsibility and status |
|---|---|---|
| Bootstrap, binary delta, echo and confirmations | Consolidate exact codec-facing shapes and refusals | FRP-01 blocks complete oracle; verification and adoption pending |
| Release representation and E-3/E-4 correlation | Keep the existing ReleaseRequest digest separate and preserve original identity meanings | E-4 owns GAVA1 header/wire, authenticated context and cross-format handoff evidence, without a new RELEASE opcode |
| Runtime owner release recomputation | No codec implementation duty | E-6 recomputes and enforces against original verified context/preparation at its atomic gate |

Parsing, equality and confirmation yield structural content agreement only.
They confer no qualification, V/W/A approval, custody, live permit,
admission, positive validation or protected execution. There is no mandatory
P1/P2 or CJSON-1 reuse. E-4 GAVA1, E-5 launcher/custody, E-6 policy,
operational correlation and atomic enablement, E-7 driver and Slice N are
separate. E-2 F-01 leaves protected readers and exact runtime case policy
with the atomic set and Slice N. R/Wc/P issuance, protected acquisition,
publication, withdrawal and operational approval remain outside this record.
Composite Q-E1 stays open.

## 6. Outstanding owner and independent review gates

FRP-01 requires an exact accepted inherited bootstrap schema for both
topologies, including allowed member sets, required presence/absence and
nested encoding constraints. See [consistency audit](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/consistency-and-open-issues.md).
The settled BJ and IE deltas are preserved. Complete bootstrap acceptance
vectors must await that decision. Then independently verify the exact record
and reviewer-owned vectors before submitting the record's Git blob and raw
SHA-256, source manifest, normative decision, evidence and qualifications
for formal owner adoption. Only that adoption can determine the status
of E3-ENTRY-F01.
