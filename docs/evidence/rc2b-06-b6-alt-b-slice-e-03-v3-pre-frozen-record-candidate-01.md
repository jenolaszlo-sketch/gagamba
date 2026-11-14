# Alternative B Slice E-3 v3-pre frozen record candidate

**PROPOSED FROZEN RECORD — NOT INDEPENDENTLY QUALIFIED OR OWNER ADOPTED**

Preparation status: complete R1 candidate for separate independent Q1.
FRP-01 is owner-resolved for the normative v3-pre contract. The complete
record remains proposed.

## 1. Authority and scope

The [normative owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md)
at commit `f12a84943ea30153ccf96dfef21866a78e3fb934`, blob
`f3639dc619dbfee39859afb7da553e2167d9b83d`, settles BJ-01–04 and IE-01–03
for v3-pre. The [source manifest](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/source-manifest.md) binds raw historical
inputs and the accepted v2 bases. Inherited rules follow the corresponding
accepted B2 or integrated v2 contract, with only the v3 delta and the explicit
owner rules below. The record and its vectors are derived evidence.

The [bootstrap-base owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md) at commit
`0fcd246812ffe1a8d9fa3813caccaf5863773eaf`, blob
`93fe2c9155a21b12fd3486b9da743260897d508e`, binds the
[complete normative schema annex](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md), blob
`a7248152d09e19d8f3775db397f340949fa9ee57` in evidence tree
`a78ecb5040f0525d2047641b75ad48dd59cd23d6`. Incorporate the exact
annex bytes as normative, including all scalar domains, 99 member rows
across 11 closed objects, required presence, null typing, prohibitions
and historical versus v3-only authority distinctions. Use decision §4 for
cross-field and serialization requirements. The [R1 coverage matrix](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-r1-evidence/delta-and-coverage.md)
maps the exact annex rows to full witnesses and refusal cases.

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

| Bootstrap field group | Qualified B2 v2 | Integrated v2 | B2 v3-pre | Integrated v3-pre | Authority |
|---|---|---|---|---|---|
| Version and authority members | Version 2; v2 qualified | Version 2; v2 qualified | Exact integer 3; required AuthorityMode and AuthorityHash | Same v3 fields | F1 line 46, BJ-03/04, bootstrap owner §2 |
| Root B2-only members | Both required | Both prohibited | Both required and non-null | Both prohibited, including at literal null | B2 §2, BB-01 |
| Identity and topology | B2 identity, Integrated/DelegatedParent null | Integrated.InstallationIdentity | B2 pre identity, Integrated/DelegatedParent literal null | Integrated pre identity and required delegated parent | SPEC §6.2.2, B2 §2, bootstrap owner §§2/4 |
| Qualification and remainder | S2 Qualified; v2 rules continue | S2 Qualified; v2 rules continue | Boolean false, both required literal null associations, closed B2 member set | Same BJ rules at the integrated location, with complete closed integrated member set | BJ-01/02 and exact schema annex |

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

The normative annex gives each root and nested object as a closed member
inventory. Every listed member is required exactly once. Only declared
literal nulls are accepted. Omission, null and an empty object have
different structural meanings. The B2 root requires non-null
`B2InstallationIdentity` and `B2DeadlineNs`; integrated forbids either
name at any value. B2 selects its pre identity at
`B2InstallationIdentity`, integrated at `Integrated.InstallationIdentity`.
Top-level version is the integer token 3, with exact string mode
`validation-pre`. Follow the annex for each scalar token, string domain
and all nested bindings. The v3 schema defines no compatibility aliases or defaults.

### Encoding and refusal

Accept one strict UTF-8 JSON object, with no BOM. Refuse malformed UTF-8,
invalid escapes, unpaired surrogate escapes, comments, trailing commas,
non-whitespace trailing content and multiple top-level values. ASCII
space, tab, CR and LF are permitted outside tokens; property order and a
final newline have no semantic effect. Compare decoded member names
ordinally and case-sensitively. Refuse duplicate decoded names recursively,
including escaped aliases, and refuse unknown members. Ordinary escapes
are allowed only where the decoded value fits the exact member type,
except BJ-03's literal AuthorityHash rule. Integer grammar and bounds
come from the annex. No CJSON-1 or general canonical serialization applies.

Count the full credential-channel JSON payload, including whitespace and
the actual UTF-8 bytes of multibyte characters, before decoding. The
allowed size is 1–8,192 bytes inclusive, excluding metadata or framing.
There is no terminator. The 8,192-byte endpoint still requires complete
valid content. Reject above-limit input, truncation, wrong member types
or values, and any violated relationship without a structural result.
The contract does not prescribe a diagnostic or failure order among
multiple invalid fields.

### Prior BJ rulings

| Ruling | Requirement |
|---|---|
| BJ-01 | Required `Qualified` is JSON Boolean false in the selected identity. Binary pre status remains unsigned byte 0 under its separate layout. |
| BJ-02 | Required `qualificationId` and `qualificationRecordSha256` are both literal JSON null in that same identity. Omission, relocation and all non-null values refuse. Neither is a B2-only root field. |
| BJ-03 | Required top-level `AuthorityHash` has exactly 64 literal lowercase ASCII hexadecimal characters, no alternate spelling, escaping, prefix or normalization. Decode to 32 SHA-256 bytes. |
| BJ-04 | Compare those bytes to the separately supplied SHA-256 of the original retained capability. Refuse missing, wrong-width or mismatched expectation. No recovery of the expected digest from the candidate. Future caller authenticates INIT and verifies the original bytes and pre catalog. The result expresses structural agreement only. |

### Cross-field checks and caller duties

1. The selected topology is exactly one of the two schema roots. Version 3
   and `validation-pre` are exact selectors. A v1/v2 parser may not derive
   positive pre semantics from those bytes. No fallback after pre refusal.
   The numeric top-level version and the identity `Bootstrap` constant 1
   have different meanings. JSON Boolean false does not change during
   integrity verification or convert to a status grant.
2. Compare the root execution GUID, lease GUID in root `Generation`,
   original session GUID, controller PID/UID and private association secret
   to the explicitly supplied original data. The caller obtains that data
   independently. Require the values to match. The codec does not generate,
   repin or adopt identities. The identity's string `Generation` equals
   `ArtifactSetSha256` and is separate from the root lease GUID.
3. Each executable pin directory is
   `/opt/gagamba/installations/<selected identity Generation>/recovery`
   or `/opt/gagamba/installations/<selected identity Generation>/owner`
   by its role. The closure map is exact for that role and must agree with
   the separately supplied verified manifest projection. Check identity
   digest and role-closure claims against that same original projection.
   The accepted B2 §2 role preimage, filesystem checks and protected
   source acquisition remain separate from this codec. No reading or
   recomputing file hashes to establish authority here.
4. For integrated only, `Controller.Pid` and `Controller.Uid` equal the
   corresponding root fields; the GID and namespace claims must agree with
   the supplied original controller expectation. `AcceptedAtNs <
   ExpiresAtNs` and their positive difference is at most 120000000000.
   Compare integrated issuer, store generation, times and profile to the
   provided original context projection. The caller owns temporal checks
   against real monotonic time, earlier session limits, expiry and
   verified process provenance. B2 `B2DeadlineNs` matches the supplied
   original deadline and obeys the existing B2 limits at the caller.
5. Integrated `HostPath` is exactly `/usr/lib/dotnet/dotnet`,
   `RuntimeDirectory` is `/usr/lib/dotnet/shared/Microsoft.NETCore.App/`
   plus `RuntimeVersion`, and `HostFxrDirectory` is
   `/usr/lib/dotnet/host/fxr/` plus that version. All runtime claims match
   the independently verified installed-runtime projection. The decoded
   `RuntimeInventorySha256` bytes equal those of the selected
   identity's runtime inventory. No filesystem or loaded-process
   qualification follows from equality.
6. Integrated `DelegatedParent` equals `Profile.DelegatedParentPath`.
   `Profile.OwnerDllPath` is `Owner.Directory` plus
   `/Gagamba.Execution.Linux.M1.Owner.dll`; its digest equals the matching
   owner map member. The profile coordinator digest equals the identity
   coordinator digest. The F2 profile claims must match the separately
   supplied expected projection for the fixed fixture and verified policy.
   This includes the exact `SourceRootPath` under
   `/opt/gagamba/validation-fixtures/<Hm>`, exact source basenames,
   tool/input digests and enrollment IDs, delegated/staging/journal paths,
   budget, object count and lead time. Staging parent and journal directory
   must end in `/va-` followed by the 32 lowercase hex characters of the
   original execution GUID with hyphens removed. Their respective base
   paths come from the supplied verified policy projection. Do not derive
   those bases from JSON. The caller establishes the applicable fixture
   and case policy, positive lease/expiry bounds, ownership and custody
   at the later atomic and native gates. The codec checks claims only.
7. For both forms, `EvidenceDirectory` must match its supplied expected
   path. BJ-03/04 supplies the sole top-level `AuthorityHash` source.
   Decode its exact literal value and compare all 32 bytes to the
   independently supplied expected SHA-256 of the original capability.
   Absence, wrong width or mismatch refuses. An authenticated INIT and
   independently verified exact retained capability and pre catalog are
   prerequisites owned by the later caller. No reconstruction of the
   expected digest from the payload, fallback to other digest identities
   or authorization inference is permitted.

Rules 2–6 use explicit data-only inputs. Apart from the stated grammar and
internal equality checks, matching a caller projection is an exact
semantic member comparison using the declared types. Missing required
comparison data refuses. This assigns no protected I/O, runtime policy,
live time measurement or trust decisions to E-3. Implementations and
independent reviewers can build payloads and expected data independently
from the closed schema. The complete structural witnesses and successive refusal vectors are bound below.

### Bootstrap verification fixtures

Use the [owner witness annex](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/witnesses.md), blob
`19886332f3fb1bcf3149f4e173642a8673fbb746`, for the exact synthetic
P-B2 (2,693 bytes) and P-I (4,519 bytes) payloads and their full digests,
the P-LIMIT and P-WS controls, and N01–N22. The two full witnesses are
valid serializations under separately supplied structural expectations.
The [R1 additions](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-r1-evidence/bootstrap-checks.md) bind remaining
targeted mutations and measured identities. Fixture bytes establish no
real authenticated context. The normative authority is the owner decision
and schema annex, with the vectors serving as checkable evidence.

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
| Bootstrap, binary delta, echo and confirmations | Consolidate exact codec-facing shapes and refusals | R1 candidate complete; independent verification and adoption pending |
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

## 6. Compatibility and remaining gates

FRP-01 is resolved for the normative v3-pre contract. BB-02 remains an
unremediated integrated v2 implementation nonconformance. The serializer
can emit both prohibited B2 members as null and the decoder can accept
those null values. The proposed isolated E-3 codec does not depend on or
authorize reuse of that behavior. Legacy remediation and any future reuse
or exposure of affected paths require separate authorization and applicable
independent regression qualification. No v2 null exception is introduced.

E3-ENTRY-F01 remains open. Independently qualify the exact proposed record
and reviewer-owned witnesses, then submit the candidate Git blob and raw
SHA-256, owner decisions, schema and evidence for separate formal adoption.
Only that decision may determine closure of E3-ENTRY-F01. E-3 entry and
codec implementation remain separately gated.
