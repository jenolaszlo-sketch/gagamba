# Verified field extraction and release-correlation allocation

Source IDs resolve to full immutable identities in
[Sources and raw-byte verification](sources-and-verification.md).
This matrix is assessment evidence. All rows await the applicable owner
decision. **NOT DEFINED BY VERIFIED SOURCE** marks missing exact rules.
Offsets are zero-based payload offsets, exclusive of any frame header.

## Bootstrap and selection

| ID | Requirement | Source and location | Extraction and owner status |
|---|---|---|---|
| V3-01 | Explicit bootstrap version 3, pre mode only. Existing version 1 stays legacy and unqualified. Actual post qualification remains v2. Old parsers refuse unknown versions/modes. Mixed old/new validation has no fallback. The bootstrap version is independent of the inherited binary header version and of identity protocol fields. | F1 lines 44–48, AB lines 42–44, 64; WIRE §§C.1–C.2; ALT §3.5 | Direct explicit delta and inherited separation. |
| V3-02 | Retain the corresponding accepted strict UTF-8 JSON bootstrap fields and size/number rules, with maximum 8192 bytes. Add top-level `AuthorityMode` with literal string `validation-pre` and `AuthorityHash` with 64 hex characters. Integrated uses its existing `Integrated.InstallationIdentity` location, B2 the inherited `B2InstallationIdentity`. | F1 line 46; AB line 42; SPEC §6.2.2, WIRE §§C.1, D.2, B2 §2 | Direct and inherited. Exact v3 member ordering, JSON whitespace/canonical byte order, additional `AuthorityHash` case/content comparator and v3 JSON identity status mapping are **NOT DEFINED BY VERIFIED SOURCE**. No CJSON-1 requirement was recovered for this JSON. |
| V3-03 | B2 inherits the non-integrated and non-delegated shape and positive canonical signed i64 `B2DeadlineNs`. B2 pre uses exact selector `--b2-lifeline 3`. Integrated selection comes solely from authenticated bootstrap and keeps the existing integrated identity location, with B2-specific fields excluded according to its inherited rule. | F1 line 46; B2 §2; B2-O Wire and readiness contract | Direct v3 selector and inherited topology. The command selector alone conveys no authority. |
| V3-04 | In the pre context both qualification associations are null. Distinct installed-integrity and pre-session semantics, with complete host/loaded-role checks in the future caller. A pre result cannot satisfy Qualified/general admission. Later positive bootstrap decode requires authenticated INIT and matching pre catalog before it can progress. | F1 lines 46–48; AB lines 21, 42–44, 50–52; F3 line 21; ALT §§3.1–3.4 | Direct semantic requirement. Exact JSON null versus absent representation and placement for the pre bootstrap identity are **NOT DEFINED BY VERIFIED SOURCE**. The F3 capability keys cannot be copied into the bootstrap as a presumed schema. |

## Binary expectation and echo

| ID | Record or field | Offset and width | Exact representation or dependency | Evidence |
|---|---|---|---|---|
| V3-05a | B2 37 original session | 0 / 16 | Existing .NET GUID byte order, original nonempty session | B2 §§2–3 and F1 line 46 |
| V3-05b | B2 37 installation and role evidence | 16, 48, 80, 112, 144, 176 / 32 each | Raw SHA-256 in order: artifact set, owner closure, recovery closure, coordinator, seccomp filter, runtime inventory. Generation equals artifact set without another representation. | B2 §3 and F1 line 46 |
| V3-05c | B2 37 protocol fields | 208, 210, 212, 214 / 2 each | Unsigned LE u16 manifest schema, owner protocol, coordinator protocol, bootstrap constant. Retain corresponding v2 constants (1 in the accepted identity profile). | B2 §§2–3 and F1 line 46. SPEC §§6.1–6.2 supplies the base constants. |
| V3-05d | B2 37 status | 216 / 1 | Exactly unsigned byte 0 in the selected v3 pre layout. Legacy Qualified shape uses 1. No alias or other accepted pre value. | F1 lines 46–48 and B2 §3 |
| V3-05e | B2 37 profile ID/version | 217 / 1 byte ID length, then ID at 218, then 1 byte version length and version bytes | Each printable ASCII 1..64 bytes, exact expected values. Total 219 + both lengths (221..347). No trailing bytes. | B2 §3, inherited under F1 line 46 |
| V3-06a | 9/36 artifact and role closure | 0 and 32 / 32 each | Raw digests. Compare independently established expectations for artifact set and expected sender's role closure. No separate generation slot. | SPEC §§6.2.3, 6.2.5, B2 §3, F1 line 46 |
| V3-06b | 9/36 protocol fields and role | 64, 66, 68 / 2 each; 70 / 1 | Unsigned LE u16 owner, coordinator, bootstrap constants. Expected role 0 for 9, 1 for 36. | Same sources as V3-06a |
| V3-06c | 9/36 status/reserved | 71 / 1; 72 / 4 | Unsigned status 0 only in pre v3. Reserved bytes exactly zero. | Same sources as V3-06a |
| V3-06d | 9/36 tail and lengths | from 76 | B2 has no tail and exactly 76 payload bytes. Integrated retains the base layout with optional length-prefixed UTF-8 profile ID/version up to 64 bytes each and the original 256-byte payload bound under the 512-byte wire ceiling. Exact new integrated v3 tail-prefix encoding/presence policy requires an owner ruling. **NOT DEFINED BY VERIFIED SOURCE** for that v3 decision. | B2 §3 limits B2 only, SPEC §§6.2.3, 6.2.5, WIRE §C.3, F1 line 46 |
| V3-07 | Inherited recovery 10 confirmation | 0 / 1; 1 / 1; 2 / 2; 4 / 32 | Total 36. Success 1 with reason 0. Failure 0 with inherited reasons 1 digest, 2 closure, 3 protocol, 4 generation, 5 qualified. Reserved zero and exact expected artifact digest. Source defines no new pre reason or remapping of historical code 5. | SPEC §6.2.4, B2 §3, F1 line 48 |
| V3-08 | Inherited B2 12 confirmation | 0 / 16; 16 / 32 | Total 48. Exact original session in existing GUID order and original expected artifact digest. Positive only; no new status field, no integrated 12. | B2 §3, B2-O, F1 lines 48, 58, 60 |

Binary fields compare to the exact expected values and reject incompatible
layout lengths, incorrect roles/status, malformed reserved fields and
digest/context disagreement. Digests compare as raw bytes and the inherited
B2 profile compares exact bytes. There is no normalization rule to substitute
a new identity. Receipt/phase changes cannot upgrade a pre session. Codec
failure supplies no trusted result. Stop, actual peer authentication, deadlines,
latch handling and runtime progress belong to future authorized integration.
The sources provide no codec exception type, diagnostic string or new
pre-phase refusal code. General-purpose canonical JSON, padding beyond the
specified reserved bytes, and new fields remain out of the extraction.

## Composite Q-E1

| Obligation | Normative source | Assigned milestone | Reconciliation result and remaining work |
|---|---|---|---|
| v3-pre layouts | F1 lines 42–48, AB lines 40–44, SCOPE §§7, 9 | E-3 | Verified delta and base layout mapping above. Resolve JSON encoding/binding and integrated echo tail before adoption. |
| Codec-side release correlation | F1 §Driver and release confinement, last paragraph at line 54; F2 §5 lines 98–104; AB lines 32–36 | E-3 compatibility evidence and E-3/E-4 allocation | Existing ReleaseRequest carries the digest. No v3-pre layout contains a release-correlation field. No E-3 algorithm, serializer or comparison required by the verified contract. Do not conflate `AuthorityHash` or echo digests with the release digest. |
| GAVA1 header | F1 §GAVA sideband and AB line 27; E2 §5 R27 | E-4 | Retain source pointers and separate accepted header prerequisite. Full header extraction and adoption outside this commission. |
| GAVA1 wire | F1 lines 17–30 and E2 §5 R27 | E-4 | Authenticated INIT and matching pre catalog are future caller conditions. No new RELEASE opcode. Full wire reconciliation remains open. |
| E-3/E-4 cross-format release binding | E2 §5 R27 and AB lines 23, 32–36; F2 §5 | E-3/E-4 handoff | Preserve original identities and hashed content meanings across the handoff. No runtime proof claimed. E-4 retains its own exact direct-context evidence gate. |
| Runtime owner correlation | PLAN §3.5, ROAD §8, F2 §5, ALT §3.2 | E-6 atomic set | Owner compares the existing ReleaseRequest approval correlation and requires HasApproval as part of the independently verified action, context and preparation. No runtime work authorized here. |

The formula's exact source order is the ASCII domain
`gagamba-validation-release-v1`, one zero byte, four raw 32-byte digests
(capability, Hk, Hs, A2), unsigned LE u32 with value 2, four 16-byte .NET
`Guid.TryWriteBytes` values (session, execution, lease, preparation),
and the 32-byte preparation manifest digest. SHA-256 of those 258
concatenated bytes gives the existing 32-byte field. F2 line 100 and AB
line 34 are identical. F2 §4 binds A2 to the exact finite action template.
There is no new release identifier, opcode, digest grammar or transfer of
runtime correlation ownership in this record. Correlation conveys no
standalone authority.

## Extraction controls

Read the F1 raw source separately from the AB restatement and locate every
delegated rule in the adopted v2 sources. For the B2 binary carry-forward,
a separate mechanical read of the committed GOLD payloads (without invoking
the installed parser) confirmed 9/36 length 76, LE protocol fields and legacy
status offset 71, 37 status offset 216 and the two length-prefix boundaries
(the recorded vector is 246 bytes), 10 length 36 and success pair (1,0), and
12 length 48. Sum of fixed widths independently gives 76 and
219 + profile lengths for 37. This checks v2 field placement and confirms
where the F1 zero-status delta applies. Actual v3 bytes still need
owner-adopted test oracles and future independent qualification. The
integrated tail and JSON decisions were deliberately left unresolved.
