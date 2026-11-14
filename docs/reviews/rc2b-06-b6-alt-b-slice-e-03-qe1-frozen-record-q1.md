# Alternative B Slice E-3 Q-E1 frozen record independent qualification

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-FROZEN-RECORD-Q1`

Date 2026-11-15 (Asia/Manila)

**QE1 FROZEN RECORD Q1 SUPPORTED WITH QUALIFICATIONS**

The exact R1 candidate supports separate owner adoption. It specifies
both v3-pre forms and the binary delta without another material protocol
choice. Q1 found a non-blocking limitation in the earlier evidence probe's
treatment of escaped member names. Independent controls now cover the
required distinction. BB-02 remains a separate integrated v2 implementation
nonconformance.

## 1. Immutable baseline

| Item | Verified value |
|---|---|
| Git identity | `C:/Users/Laszlos/source/repos/Gagamba`, `main`, sole canonical worktree |
| Starting HEAD | `acd61fd0af622f9e1d988845658c26a6835ddd62` |
| Starting tree | `704f73e6d127303e74a63c01997235a084b0b8a6` |
| Recorded `origin/main` | `a30d1807686ee15d39ef7e9103e8b992062bd971`, 8 ahead / 0 behind, without synchronization |
| Worktree state | Tracked files and index clean, zero stashes, no conflicting Git writer or lock observed |
| Historical untracked | 44 present, 44 SHA-256/length matches, zero extras, freeze manifest SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1` |
| Candidate | Blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`, raw SHA-256 `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e`, 21,225 bytes |
| R1 report and evidence | Blob `8b0d8c8c5103b850209b6e1b2d76808b7a8c8007`, tree `d52d9ba53dac7e3aca13a7a01f32914f3685f0d1` |
| Bootstrap owner | Commit `0fcd246812ffe1a8d9fa3813caccaf5863773eaf`, blob `93fe2c9155a21b12fd3486b9da743260897d508e`, evidence tree `a78ecb5040f0525d2047641b75ad48dd59cd23d6` |
| Normative annex and witnesses | Schema blob `a7248152d09e19d8f3775db397f340949fa9ee57`, witness blob `19886332f3fb1bcf3149f4e173642a8673fbb746`, original probe blob `90e97bf480e336f7d17717f4484507161942aca7` |
| Original preparation | Commit `fcf61cce5cdf56b1f96d86bee3e79fad522a9e19`, report blob `566201901c26e9236a9a38636209e96961210fe2`, evidence tree `bdb8a82842afc034f5f846478920701a81a2f032` |
| Normative BJ/IE owner | Commit `f12a84943ea30153ccf96dfef21866a78e3fb934`, blob `f3639dc619dbfee39859afb7da553e2167d9b83d` |
| Provenance reconciliation | Commit `2a3dbcef99fdd9073e0c8c5db0f6371b5f7a5120`, report blob `a61febbd417ad6628a47bec0cffa48e6bc1c8e0a`, evidence tree `c2205a46c5b5480de36f8dac61c208097dba16b5` |
| E-3 entry assessment | Commit `52b932aaebd52f16bd93170b95bb0f8e89241fd2`, report blob `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12` |
| E-2 closure | Commit `21179680e7ae5d153c7ddeef5d209e5ac450cdd7`, blob `3388894f821a4a92aca7c2b2d79c832a0f70a450` |

The listed commits form the verified direct parent chain from the starting
baseline through formal E-2 closure. Repository identity, refs, lock and
writer checks, worktree status and all historical untracked bytes were
verified before writes. Candidate SHA-256 was independently checked both
against the file and its committed bytes. [Candidate](../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md) ·
[R1 report](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-r1.md).

## 2. Method and provenance

The reviewer consulted exact raw accepted historical artifacts, the
immutable original v2 decisions and v3-pre owner decisions, the entry
and E-2 closure constraints, and the prior reports and evidence for
comparison. The reviewer independently transcribed the owner schema,
constructed new synthetic contexts and witnesses, and packed binary
payloads from the historical layouts. The scratch oracle calculated
lengths, hashes, offsets and expected results with the Python standard
library. The previous validation scripts supplied none of the oracle's
rules or expected outcomes. Prior fixtures were compared separately at
their stated identities. [Source verification](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/source-verification.md) ·
[Reproduction](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/reproduction.md).

All 12 named raw sources matched independently, including the F02 and
Alternative B bindings and six package manifest memberships. All 13
canonical source blobs matched the committed provenance inventory,
including SPEC, WIRE, B2 and GOLD. The B2 amendment raw Git SHA-256 matches
its owner acceptance. Runtime behavior does not set the normative rules.
Historical external Git ancestry remains unverified.

## 3. Bootstrap contract

The independent inventory matched 11 closed objects and all 99 required
member rows. Candidate §§1 and 3 incorporate the exact accessible owner
annex by commit/blob, and preserve owner §4's cross-field checks. No
member, type, nullability, presence or version mismatch was found.
BB-01 requires B2's two non-null root members and prohibits both in
integrated v2 and v3-pre, including at null. B2's separate literal nulls
for Integrated and DelegatedParent remain mandatory. Both v3 topologies
retain exact numeric 3, validation-pre, and one selected pre identity.
B2 owner selection is exactly `--b2-lifeline 3`. Integrated uses
authenticated bootstrap selection. BB-03 preserves the legacy decoders
and their bounded contexts, with v1 unqualified and v2 qualified. Mixed
validation versions, unknown modes/versions and downgrade refuse.
[Member matrix](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/schema-reconstruction.md).

BJ-01/02 preserve Boolean false and the two required literal nulls inside
that identity. BJ-03 validates exactly 64 literal lowercase ASCII hex
characters in the AuthorityHash value token. BJ-04 compares all decoded
32 bytes against a separately supplied original-capability digest and
refuses missing, wrong-width or unequal comparison material. Later callers
authenticate INIT and retain and verify original capability bytes and pre
catalog. Matching untrusted supplied data remains a structural pass only.

The probe confirmed one strict UTF-8 object, invalid UTF-8/BOM/escapes,
unpaired surrogate and extra top-level refusal, recursive decoded-name
duplicates, unknown members, and exact scalar rules. Valid escaped keys
compare by decoded name. JSON whitespace and property order may vary.
The byte bound covers the whole received credential JSON payload including
whitespace. Structural controls passed at 8,191/8,192 bytes and refused
at 8,193. A multibyte path mutation distinguished byte counting from
code-point counting and was independently invalid under the ASCII path
rule. Both committed complete witnesses reproduced their exact 2,693
and 4,519 bytes and hashes. Reviewer positives are 2,686 and 4,515 bytes.
All complete SHA-256 identities and 110 outcomes are recorded, with 13
passes (four owner reproductions) and 97 refusals.
[Bootstrap witnesses and results](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/bootstrap-independent-vectors.md).

## 4. Binary and release contract

An independent offset map accounts for all 76 octets of B2 and integrated
base 9/36. The integrated present form is 105 bytes. IE-01 binds tail
presence to length, IE-02 binds prefixes at 76/103 to u8 widths 26/1,
and IE-03 binds original bytes to `ae4-linux-fixed-fixture-v1` /
`1` under the independent profile expectation. Both integrated roles and
both lengths were exercised. B2 refuses a tail. Wrong length, malformed
counts, profile, digest, role, status, reserved or extra bytes refuse.
The v2 GOLD comparison independently established status-only changes
at 71 for 9/36 and 216 for 37. JSON false and the binary zero byte
retain their distinct types.

Message 10 retains 36 bytes and the original artifact digest at offset 4,
success (1,0) and inherited refusal (0,1–5). B2-only recovery 12 retains 48
bytes with original .NET session GUID followed by the digest at 16. No
positive progression follows a malformed, out-of-context or mismatched
confirmation or a well-formed refusal. 10/12 matched historical bytes
exactly. For 46 binary controls, 10 permitted structural progression,
one recognized a well-formed refusal and 35 rejected an invariant.
Predecessor state and selected version were supplied data. Sequence,
channel and framing constraints were checked statically.
[Binary results](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/binary-independent-vectors.md).

F2 §5 and AB line 34 agree on the exact 258-byte preimage and SHA-256.
The 32-byte result fills existing ReleaseRequest.ApprovalCorrelationSha256
with the existing approval indication true. The independent calculation
reproduced the prior preimage and added controls for .NET order, action,
approval flag and digest substitution or omission. E-3 has no release
serialization, digest computation or comparison responsibility. E-4
retains GAVA1 header, wire and authenticated handoff. E-6 retains runtime
recomputation and atomic enforcement against its own original verified
context. [Traceability and release controls](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/traceability-and-authority.md).

## 5. Implementability, findings and next gate

Two implementers can use the candidate and its immutable owner references
to choose the same names, scalar/child types, required nulls and omissions,
version and refusal conditions, comparison operands, authority limits and
binary fields. Bootstrap admits only the explicitly described variation in
representations. Echo tail presence has two exact accepted forms in the
integrated context. Neither gives room to invent a profile or comparison.
The record's successful results supply structural agreement only and
remain separate from V/W/A, original capability provenance, protected
custody, live invocation, final admission, positive validation or
protected execution. No codec, runtime or native qualification was run.

Q1-E01 qualifies the earlier scratch evidence for escaped AuthorityHash
keys. The candidate and owner decisions are consistent and the new J12
and H14 controls remove that evidence gap for adoption. BB-02 remains
separate and unremediated. Reuse or exposure of the affected legacy path
requires separate authorization and regression qualification. No
candidate-content mismatch or missing material normative decision was
found. [Findings and carried qualifications](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/findings.md).

Recommend separate
`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-FROZEN-RECORD-OWNER-01` to
verify and disposition the exact candidate and this report, accept or
reject the qualification and disposition E3-ENTRY-F01 separately. This
candidate remains proposed. E3-ENTRY-F01 and composite Q-E1 remain open.
E-3 entry and implementation, E-4 through E-7 and Slice N remain
unauthorized. The E-2 closure, 39 retained qualifications, 23 downstream
obligations, F-01 protected readers and exact case policy, P0 durability
and unassigned first E-1 production consumer retain their accepted
allocations. RELEASE and expected Debug refusal, P4 residuals and
E-4/E-6 re-audits, `NATIVE-PENDING` and all 331 unqualified native skips
remain in force. No operational approval, positive validation or
protected execution follows.
