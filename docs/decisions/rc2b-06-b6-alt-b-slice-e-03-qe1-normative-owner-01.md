# Alternative B Slice E-3 Q-E1 normative owner decision

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-NORMATIVE-OWNER-01`

Date 2026-11-14 (Asia/Manila)

## 1. Authority, baseline and evidence

This commission settles the two encoding gaps in E3-ENTRY-F01. Slice E owner
§§7 and 9 and the entry review §8 require the complete authoritative layout
record in Git before codec work. This decision supplies owner rules to that
future record. Entry and specification adoption are separate decisions.

| Starting check | Observed |
|---|---|
| Git root | `C:/Users/Laszlos/source/repos/Gagamba`, identified by Git, Gagamba.sln and the accepted decision chain |
| Branch, worktrees | `main`, one canonical worktree |
| HEAD | `2a3dbcef99fdd9073e0c8c5db0f6371b5f7a5120` |
| Tree | `d6a487894b1383d99ac1030d15dde1567c88bdc4` |
| Recorded origin/main | `a30d1807686ee15d39ef7e9103e8b992062bd971`, divergence 4 / 0 |
| Preservation | Clean tracked files and index, zero stashes, 44/44 historical untracked paths matched by SHA-256, zero extras |
| Writer check | No conflicting Gagamba writer observed in process and lock checks |

The starting commit binds the [reconciliation report](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01.md),
blob `a61febbd417ad6628a47bec0cffa48e6bc1c8e0a`, and the evidence tree
`c2205a46c5b5480de36f8dac61c208097dba16b5`. Its
[source inventory](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md)
is blob `8bb7daa7b2b0e635ba2445c78a7ac1fbf395c1ab`. All 12 selected raw
files were rehashed against that committed inventory without normalization.
The [E-3 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01.md)
at `52b932aaebd52f16bd93170b95bb0f8e89241fd2`, blob
`d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12`, leaves E3-ENTRY-F01 open.
The [E-2 closure](rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md)
at `21179680e7ae5d153c7ddeef5d209e5ac450cdd7`, blob
`3388894f821a4a92aca7c2b2d79c832a0f70a450`, remains binding.

| Source used below | Exact identity and authority |
|---|---|
| F1, actor-and-descriptor-binding.md | SHA-256 `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93`. F02 acceptance §2 binds manifest `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`. F1 lines 24, 38, 42–48 and 54 supply context and the proposed wire delta. |
| F2, case-policy-and-action-contract.md | SHA-256 `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7`. Same F02 manifest. §5 lines 98–104 defines release correlation. |
| F3, mechanism-b-session-capability.md | SHA-256 `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010`. Same F02 manifest. Lines 19–23 specify the separate exact capability bytes and their verification context. |
| AB, validation-bootstrap-compatibility.md | SHA-256 `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb`. Alternative B decision §§3.3–3.5 and §4 accept the bounded design and bind AB. See AB lines 21, 32–36, 42–44 and 50–52. |
| SPEC and WIRE, integrated v2 base | [SPEC](../design/rc2b-06-b6-installed-artifact-trust.md) §§6.2.2–6.2.5, blob `7f9f615f75527221e81b7aaa498f4b564acb62b9`. [WIRE](rc2b-06-b6-wire-owner-acceptance.md) §§C and D.2, blob `501487227208d935c3ab1337c9f87055c9e14b8d`. |
| B2, accepted v2 amendment | [Amendment §§2–3](../design/rc2b-06-b6-s3-b2-wire-delta-01.md), blob `95dc3221ae2a5579f966526516073cdd0f392aa9`, committed SHA-256 `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6`, as bound by the [B2 owner decision](rc2b-06-b6-s3-b2-wire-owner-acceptance.md). |
| GOLD, v2 evidence | [Synthetic S3 vectors](../evidence/rc2b-06-b6-impl-01-s3/golden-wire-vectors.json), blob `de506adea195bc198680fdfd8d6c70c71a88c7a1`. Corroboration of inherited fields, with authority from the decisions above. |

The source identities establish exact bytes. Historical proposal captions and
the F02 companion matrix C04/Q04 preserve the separate wire approval gate.
The historical sources define the v3 design delta and inherited shapes.
The field choices expressly marked below are new normative decisions for
v3-pre alone. No source-file or v2 amendment is revised.

## 2. Bootstrap JSON

**Established by source.** The input is the corresponding B2 or integrated
strict UTF-8 JSON bootstrap, at most 8,192 bytes, with numeric bootstrap
version 3 and top-level `AuthorityMode` equal to the string
`validation-pre` and `AuthorityHash` carrying 64 hex characters. The
pre context has both qualification associations null. F1 lines 44–48
inherits the appropriate v2 identity location and other field/number rules.
The B2 selector is exactly `--b2-lifeline 3`, with its inherited
non-integrated/non-delegated shape and positive signed 64-bit deadline.
Integrated selection depends on its authenticated bootstrap. A selector is
a discriminator. The inherited protocol constant `Bootstrap` inside the
identity retains its value 1, independently of top-level `Version`.

Apply the rulings below as a delta to B2 §2 and SPEC §6.2.2 / WIRE §D.2.
Keep the respective existing bootstrap and identity members and inherited
constraints except for the exact v3 overrides and additions here. Use a
separate pre identity object with the inherited identity field layout.
No second identity location is allowed. The selected identity object is
`B2InstallationIdentity` for B2 and `Integrated.InstallationIdentity`
for integrated. The member names are case-sensitive and duplicate or unknown
members refuse. Numeric version uses the integer token `3`, with no
string or floating-point alias. Enforce the 8,192-byte input bound before
parsing. JSON property order and ordinary JSON whitespace carry no identity
or canonical serialization requirement. This decision introduces no
CJSON-1 requirement or dependency on P1/P2.

### BJ-01

**BJ-01 RESOLVED: exact representation adopted.**

**New owner rule.** The selected identity object must contain exactly one
`"Qualified": false` using the JSON Boolean false, with the existing member
name and casing. This is the JSON representation of pre integrity without an
S2 qualification. It is distinct from the binary payload's unsigned status
byte 0. Refuse omission, true, null, numbers including 0, strings, duplicate
members and differently cased aliases. Full integrity verification remains
a caller obligation and leaves this serialized Boolean false. Receipt/phase
change ends eligibility for the pre path. A fresh independently approved post
invocation uses the existing v2 Qualified=true contract (AB lines 50–52).

SPEC §6.2.2 establishes the legacy Boolean type. F1 establishes binary 0
and the distinct pre context. Retaining the Boolean field and selecting false
avoids a second status field or a Boolean/number union. The alternative of a
new numeric status member would change the inherited identity
layout. Version separation prevents any v2 reader from treating false as
a qualified identity. Independent qualification must test both topologies,
all disallowed alternatives, duplicate/case controls and legacy refusal.

### BJ-02

**BJ-02 RESOLVED: exact null-association representation adopted.**

**New owner rule.** Require exactly one `qualificationId` and exactly one
`qualificationRecordSha256` in the same selected identity object. Both
values must be literal JSON `null`. They have no positional ordering rule.
Refuse either member if omitted, duplicated, mis-cased, moved to a different
object or given any non-null value, including an empty string, empty object or
zero digest. No accepted v3-pre bootstrap state permits a non-null association.
The unchanged post path obtains its actual qualification through its
accepted v2 and context contracts.

AB lines 21 and 42–44 and F1 line 46 establish both semantic nulls. F3
line 21 supplies the exact terms in the separate capability format. Their
placement and mandatory presence here are new. Explicit nulls distinguish
pre intent from a truncated or incomplete record. Omitting both or using
a wrapper would leave ambiguity about which association was established.
Qualification must vary the two fields independently and keep the base
identity shape and phase fixed. The codec has no duty to look up a
qualification record.

### BJ-03

**BJ-03 RESOLVED: exact grammar adopted.**

**New owner rule.** Top-level `AuthorityHash` is required exactly once as
a JSON string whose token interior consists of exactly 64 literal ASCII
characters from `0–9` or `a–f`. It is the lowercase hexadecimal encoding
of one 32-byte SHA-256 value. There is no prefix, separator, whitespace,
escape, padding, case-folding or alternate encoding within that string.
Refuse every malformed token, including empty, null, upper or mixed case,
escaped hex characters, Base64 or a byte array. Decode the validated hex
pairs in order to their 32-byte value for BJ-04.

F1 line 46 establishes 64 hex characters. AB line 44 and F3 bind the
capability and SHA-256 context. Adoption of a single literal lowercase
representation follows the strict digest style of B2 §2. Allowing uppercase
or JSON escape aliases would add redundant representations without a
compatibility requirement for new v3. Future qualification tests length,
alphabet, type and lexically distinct spellings, independently of equality.

### BJ-04

**BJ-04 RESOLVED: exact comparison target and equality semantics adopted.**

**New owner rule.** `AuthorityHash` identifies the original pre invocation's
exact retained capability bytes. The comparison input is the 32-byte
capability digest carried by the already authenticated, original
spawn-local GAVA1 INIT and independently matched by the future caller to
SHA-256 of the exact capability bytes it verifies under F1/F3. Use that
pre-established digest as an explicit input to the isolated E-3 codec.
Require exact equality of all 32 decoded bytes. No serialization,
normalization, recomputation from JSON, or derivation of the expected value
from the candidate bootstrap is allowed. The E-3 codec accepts a supplied
expectation solely as data and refuses with no validated result if the
expectation is missing, incorrectly sized or unequal. The separately
authorized runtime caller must establish original invocation, authenticated
INIT, verified capability and matching pre catalog before permitting a
positive decode; the codec has no ability to establish those facts.

F1 lines 24 and 38 distinguish and verify the capability digest and the
catalog digest. AB line 44 explicitly ties pre identity to the capability
hash. Binding the selected capability rather than the catalog alone retains
invocation identity even when the same catalog is reused. Artifact,
catalog, R/Wc/P and release digests are separate identities. E-4 owns GAVA1
and its context handoff; protected acquisition/custody belongs to E-5/E-6
and applicable later qualification, with E-6 accountable for the complete
atomic set. A matching supplied digest is structural content agreement.
It grants no V/W/A authority, custody, live invocation, positive
validation or protected execution. Qualification needs match, one-byte
substitution, missing/wrong-width expectation and shared-catalog/wrong-
capability controls with independently supplied digests.

### Bootstrap ruling examples

This partial B2 JSON fragment illustrates all changed fields. Its compact
form is within the byte bound. The existing required topology, identity
and bootstrap members must be supplied by the future complete record.
The expected digest is independently supplied as the synthetic 32 bytes
represented by the displayed hex. The fragment alone must refuse as a
complete bootstrap. For integrated, move the identity members into
`Integrated.InstallationIdentity`, retaining the top-level version and
authority fields and the inherited integrated shape.

```json
{
  "Version": 3,
  "AuthorityMode": "validation-pre",
  "AuthorityHash": "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
  "B2InstallationIdentity": {
    "Qualified": false,
    "qualificationId": null,
    "qualificationRecordSha256": null
  }
}
```

| Change to the applicable fragment in an otherwise valid complete bootstrap | Expected refusal |
|---|---|
| `"Qualified": 0` | BJ-01 requires Boolean false. |
| Delete `qualificationId`, leaving the other member null | BJ-02 distinguishes omission from the two required literal nulls. |
| `"qualificationRecordSha256": ""` | BJ-02 refuses non-null. |
| `"AuthorityHash": "0xba78"` | BJ-03 refuses prefix and invalid length. |
| `"AuthorityHash": "aa7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"` | BJ-04 refuses the well-formed digest against the displayed expected value. |
| Keep displayed bytes and omit the expected digest | BJ-04 refuses absent independent comparison data. |

## 3. Integrated 9/36 optional profile pair

**Established by source.** SPEC §§6.2.3/6.2.5 defines an optional
length-prefixed UTF-8 pair and a 256-byte message payload cap.
WIRE §C.3 retains the outer 512-byte echo ceiling. F1 lines 44–48 carries
the inherited layout forward with status 0. B2 §3 fixes the separate
B2 9/36 at exactly 76 bytes. The base 76 bytes retain artifact digest
at 0, expected role closure at 32, u16 LE protocols at 64/66/68, role
at 70, status at 71 and four zero reserved bytes at 72–75. The role
is 0 for message 9 and 1 for message 36. Base digests and role are
checked even when no optional pair is present. Existing integrated
`InstallationWire.cs`, blob `35d8c5824590d5cea225a315736519d39b7d5889`,
corroborates the two-u8 and exact profile convention. It supplies
compatibility evidence for the choices below.

### IE-01

**IE-01 RESOLVED: exact presence policy adopted.**

**New owner rule.** For selected integrated v3-pre message 9 and message
36 only, total payload length 76 means absent. Any greater length must
contain one complete ordered ID/version pair at offset 76. Both forms
require status 0. Presence is inferred solely from length, with no flag.
Either form is permitted once the base identity and independent profile
expectation are established. A present empty or partial pair is invalid.
A second pair or any extra bytes refuse. B2 retains its no-tail 76-byte
rule; v1/v2 retain their respective accepted decoders. V3 status 0
does not qualify a v2 input.

Making the pair mandatory would narrow inherited optionality; forbidding
it would discard the compatible extended form. The full identity already binds the profile. Omitting it cannot bypass the inherited profile
expectation or its full identity/context checks. Qualification must prove
both accepted forms, the missing/malformed pair behavior and v2/B2
separation for both message roles.

### IE-02

**IE-02 RESOLVED: exact length-prefix encoding adopted.**

**New owner rule.** Each of the two strings has one unsigned octet byte
count, from 1 through 64. Single octets have no endianness. The ID count
is at 76, the ID starts at 77, the version count is at `77 + idLength`,
and version bytes begin at `78 + idLength`. Counts exclude both prefixes
and the envelope. Total length is exactly `78 + idLength + versionLength`
when present, with generic range 80–206 inside the inherited caps.
IE-03 narrows the currently accepted present length to 105. Check remaining
bytes and bounds before accessing each region or adding its size. Refuse
zero/out-of-range lengths, incomplete prefixes, overflow, declared/actual
mismatch and trailing bytes. Absence has no prefix.

A one-byte count represents the entire historical 1–64 bound and follows
the existing integrated convention. Wider signed or multibyte prefixes
would add a new incompatible interpretation. Qualification must cover
both prefix positions, short input, upper bounds, mismatch and surplus.

### IE-03

**IE-03 RESOLVED: exact accepted-byte grammar adopted.**

**New owner rule.** A present pair carries precisely the UTF-8 bytes of
`ae4-linux-fixed-fixture-v1` and `1`, the exact case-sensitive existing
profile identity in SPEC §6.2.2. These strings consist of ASCII code
points. Compare original octets to the independently expected
profile/version bytes and require that the expectation is the bound
profile. No Unicode normalization, trimming, case conversion, BOM,
termination, reserved tail bytes, unknown profile/version, or extra digest
is allowed. Byte 76 is `1a`, ID occupies 77–102, byte 103 is `01` and byte
104 is `31`. No extra tail data participates in message 10 or 12 digest
semantics. On absence there is no tail comparison. The inherited identity
and role-closure checks still apply.

An open set of UTF-8 profile values would accept identities beyond the
frozen profile. Exact expected octets preserve the bound profile and
avoid Unicode aliases. Qualification must substitute the profile ID and
version separately, verify malformed/variable spellings refuse, and
demonstrate independent digest mismatch refusal with or without the tail.
This makes no change to E-4's GAVA1 grammar or its qualifications.

### Echo payload vectors

Zero-based offsets are as above. All inputs are complete payloads without a
frame header. Spaces between bytes below are display separators. Except as
specified, the expected artifact digest contains 32 copies of `11` and
the recovery/owner closure contains 32 copies of `33`/`22` respectively.
The implicit protocol expectation is `1,1,1` and the independently expected
profile pair is the one defined in IE-03. “Pre” means an already selected integrated v3-pre
context for the structural check. Acceptance is structural only. The former
B2 synthetic vectors were read as evidence. A scratch calculation packed
the inherited fields independently, checked the status-only delta against
GOLD, and checked the 10/12 lengths and success shape. No codec was run.

| Vector | Message | Context | Bytes | Expected result |
|---|---:|---|---:|---|
| IE-V01 | 9 | Pre | 76 | Accept. Length 76, tail absent (IE-01). |
| IE-V02 | 36 | Pre | 105 | Accept. Exact pair at offsets 76–104 (IE-01–03). |
| IE-V03 | 9 | Pre | 105 | Accept. Same pair with role 0 (IE-01–03). |
| IE-V04 | 36 | Pre | 77 | Refuse. Zero length at 76, no empty form (IE-01–02). |
| IE-V05 | 9 | Pre | 105 | Refuse. ID prefix 65 at 76 exceeds bound (IE-02). |
| IE-V06 | 36 | Pre | 104 | Refuse. Byte 104 missing after prefix 1 at 103 (IE-02). |
| IE-V07 | 9 | Pre | 105 | Refuse. Prefix 2 at 103 with one remaining byte (IE-02). |
| IE-V08 | 36 | Pre | 105 | Refuse. Profile version ASCII 2 at 104 (IE-03). |
| IE-V09 | 9 | Pre | 106 | Refuse. Trailing octet at 105 (IE-02). |
| IE-V10 | 36 | Legacy v2 integrated | 105 | Refuse. Status 0 at 71 fails legacy Qualified=1 (F1 and inherited v2). |
| IE-V11 | 9 | Pre | 76 | Refuse. Artifact digest differs at 0 (SPEC §6.2.3). |
| IE-V12 | 36 | Pre | 76 | Refuse. Status 1 at 71 fails the v3 pre requirement (F1). |

```text
IE-V01 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00
IE-V02 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 31
IE-V03 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 31
IE-V04 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 00 00 00 00 00 00
IE-V05 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00 41 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 31
IE-V06 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01
IE-V07 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 02 31
IE-V08 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 32
IE-V09 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 31 00
IE-V10 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 00 00 00 00 00 1a 61 65 34 2d 6c 69 6e 75 78 2d 66 69 78 65 64 2d 66 69 78 74 75 72 65 2d 76 31 01 31
IE-V11 12 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 33 01 00 01 00 01 00 00 00 00 00 00 00
IE-V12 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 11 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 01 00 01 00 01 00 01 01 00 00 00 00
```

IE-V10 intentionally repeats IE-V02 under a different decoder selection.
The B2 v3-pre 9/36 layout remains exactly 76 bytes with status 0 at 71.
The B2 37 retains its status-only delta at 216. Message 10 retains the
36-byte result/reason/reserved/digest shape and existing result codes.
B2-only message 12 retains its 48-byte original session/digest confirmation.
Both confirm matched evidence and neither projects qualification.
Unknown versions and mode/status conflicts refuse without downgrade or
mixed-version fallback. All accepted v1/v2 bytes and transport rules
continue under their existing decisions.

## 4. Release, qualification and downstream scope

F2 §5 and AB lines 32–36 give the same formula and existing
`ReleaseRequest` placement. Preserve its 32-byte result. There is no
separate release field, digest algorithm or recomputation in E-3.
`AuthorityHash` and the echo digests retain their own identities.
GAVA1 header and wire remain with E-4. Protected launcher/custody
remains with E-5. Runtime owner correlation and atomic enablement remain
with E-6. Driver work stays with E-7, with protected native qualification
in separately authorized Slice N. E-2 closure §5 retains protected K/M/F/S
readers and exact case policy in the E-4/E-5/E-6 plus Slice N dependency
set. E-6 owns completeness of plan §3.5. Composite Q-E1 remains open.

This ruling triggers exact contract extraction and future codec-specific
independent qualification, including the cases above. E-2 closure retains
39 qualifications and 23 downstream obligations by immutable reference.
Unrelated qualifications carry forward without requalification. P0/D1/P1/P2
and E-1/F02 remain under their accepted dispositions. The first E-1 consumer
and P0 external durability remain unassigned. RELEASE verifier policy,
Debug negative control, P4 residuals F-R1/O-R1, O-R2, O-R3 and
observation-to-effect, and the E-4 and E-6 P4 re-audits are preserved.
`NATIVE-PENDING` and 331 unqualified skips remain in force.

## 5. Formal owner disposition

**QE1 NORMATIVE CHOICES RESOLVED**

The two gap groups now have exact owner rules for record preparation.
The complete record, independent source-to-record verification and formal
adoption remain pending. E3-ENTRY-F01 stays open until those gates are
met. E-3 entry, codec implementation and all downstream authorizations
remain separate. This commission grants no operational approval, positive
validation or protected execution.

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-FROZEN-RECORD-PREP-01`
under a separate commission. Assemble the source-bound inherited layouts,
these owner choices and the complete version/refusal/digest scope. Verify
traceability and extend the exact vector expectations for the complete
record, then submit its immutable bytes for independent verification and
separate formal owner adoption.
