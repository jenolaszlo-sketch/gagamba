# Alternative B Slice E-3 Q-E1 frozen record owner adoption

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-FROZEN-RECORD-OWNER-01`

Date 2026-11-15 (Asia/Manila)

**QE1 FROZEN RECORD OWNER ADOPTED WITH QUALIFICATIONS**

**E3-ENTRY-F01 CLOSED BY EXACT FROZEN-RECORD OWNER ADOPTION**

This decision adopts the exact committed v3-pre record for the internal E-3
codec scope. Q1-E01 is accepted as a non-blocking qualification. The
external historical Git ancestry limitation remains in force, and BB-02
remains open as a separate implementation nonconformance. Composite Q-E1
remains open. E-3 implementation entry requires a separate owner decision.

## 1. Baseline and exact binding

| Item | Verified identity |
|---|---|
| Canonical repository | `C:/Users/Laszlos/source/repos/Gagamba`, one `main` worktree |
| Starting HEAD / tree | `6123b6f4de47cb542db3bdab55c0fc49d5cd095c` / `ee97488ed06ae3836e7aeb81f12bf1fede096c9d` |
| Starting Git state | Tracked files and index clean, no stashes, no Git lock or identifiable conflicting Git writer observed |
| Historical untracked paths | 44/44 lengths and full raw SHA-256 matched. Inventory SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1`. Zero extra paths. |
| Recorded origin/main | `a30d1807686ee15d39ef7e9103e8b992062bd971`, 9 ahead / 0 behind at entry |
| Adopted path | `docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md` |
| Adopted bytes | Blob `f6a71562845d2cfe0aee5495864a509c5caabe7a` at `acd61fd0af622f9e1d988845658c26a6835ddd62`, 21,225 bytes, raw SHA-256 `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Q1 disposition | QE1 FROZEN RECORD Q1 SUPPORTED WITH QUALIFICATIONS. Report blob `5f8b92e4874378bb365fcec80d794f964aa6f821`, evidence tree `1cb5ac2c2b467cbfb1c0d4ac85682fb979ffe961` at starting HEAD. |
| R1 preparation | Report blob `8b0d8c8c5103b850209b6e1b2d76808b7a8c8007`, evidence tree `d52d9ba53dac7e3aca13a7a01f32914f3685f0d1` at `acd61fd0af622f9e1d988845658c26a6835ddd62` |
| Bootstrap owner ruling and evidence | Commit `0fcd246812ffe1a8d9fa3813caccaf5863773eaf`, decision blob `93fe2c9155a21b12fd3486b9da743260897d508e`, evidence tree `a78ecb5040f0525d2047641b75ad48dd59cd23d6` |
| Closed normative schema | Blob `a7248152d09e19d8f3775db397f340949fa9ee57` within that tree. Owner witness blob `19886332f3fb1bcf3149f4e173642a8673fbb746`, original scratch probe blob `90e97bf480e336f7d17717f4484507161942aca7`. |
| BJ/IE owner ruling | Commit `f12a84943ea30153ccf96dfef21866a78e3fb934`, blob `f3639dc619dbfee39859afb7da553e2167d9b83d` |
| Historical provenance | Reconciliation commit `2a3dbcef99fdd9073e0c8c5db0f6371b5f7a5120`, report blob `a61febbd417ad6628a47bec0cffa48e6bc1c8e0a`, evidence tree `c2205a46c5b5480de36f8dac61c208097dba16b5`, inventory blob `8bb7daa7b2b0e635ba2445c78a7ac1fbf395c1ab`. Supplementary source manifest blob `1c217485c8e8406bbfffad61fd39d7ec5b6dd549`. |
| Accepted v2 base | SPEC `7f9f615f75527221e81b7aaa498f4b564acb62b9`, WIRE `501487227208d935c3ab1337c9f87055c9e14b8d`, wire delta `889f173151c0c6be97033e9e03dcd84c1697a677`, B2 amendment `95dc3221ae2a5579f966526516073cdd0f392aa9` and acceptance `76b70f6832733c7cf58ffaa56afbd3df1a9f014a`. |
| Evidence fixtures | Owner P-B2/P-I/P-LIMIT/P-WS and N01–N22 in the bound witness blob, the R1 additions in the bound R1 tree, GOLD `de506adea195bc198680fdfd8d6c70c71a88c7a1`, inherited/IE and release vectors `c0935d0be9c7619b386f7c5be0edd0f49e778cc9`, and independent positive/refusal sets in the bound Q1 tree. |

The authoritative normative content is the adopted candidate and its
incorporated rules from the exact bootstrap owner decision and schema
annex, BJ/IE owner decision and accepted historical definitions at the
identities above and in the bound inventory. The owner rulings govern
the v3 deltas, and the accepted v2 rules govern inherited fields. The
provenance and vector artifacts establish traceability and evidence.
They supply no additional normative rules. Later document revisions
require an explicit disposition. The candidate's proposed-status wording
records its preparation state. This decision records adoption of those
bytes without editing them.

[Adopted record](../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md) ·
[Q1](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1.md) ·
[Q1 evidence](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1-evidence/README.md) ·
[R1](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-r1.md) ·
[Bootstrap owner](./rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md) ·
[BJ/IE owner](./rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md) ·
[Historical inventory](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md).

## 2. Sufficiency of Q1 and provenance

The owner verified the candidate blob at both R1 and Q1, calculated its
raw SHA-256 over Git bytes, and matched the working copy byte for byte.
Both reports identify that same record. The Q1 evidence tree includes
a reviewer-transcribed schema, independently packed binary layouts,
complete new synthetic bootstrap forms, targeted refusal cases and the
reproducible standard-library probe. Its conclusions come from the
governing sources, with implementation inspection confined to
corroboration or nonconformance. The owner inspected the method and
outcomes and reconciled the recorded source evidence without rerunning Q1.

The 12 reported original raw-byte lengths and hashes match the accepted
inventory, and 13 canonical path/blob identities were rechecked in
Gagamba. Q1 independently bound the six relevant package manifest
memberships and the Alternative B and F02 acceptance records. Repository
content identity and the adopted source-to-rule chain suffice for this
internal frozen record. The historical artifacts retain the limitation
that independent verification of their external Git ancestry is
unavailable. P0's separate external-package durability obligation
remains open. Adoption establishes no new historical custody.

| Contract area | Owner assessment of the independent evidence |
|---|---|
| Closed bootstrap | The 11-object, 99-member matrix accounts for both roots and the selected identity, role pin and closure maps, integrated body, controller, runtime and profile. Each row is required once, with exact type and nullability. The incorporated scalar definitions and §4 of the bootstrap owner decision settle lexical domains, internal equality and independently supplied comparison operands. |
| B2/integrated and version | BB-01 requires non-null B2InstallationIdentity and B2DeadlineNs in B2 and forbids both in integrated, including at null. B2 requires literal null Integrated and DelegatedParent. BB-03 leaves bounded unqualified v1 and qualified v2 under their accepted contracts. Exact JSON version 3, validation-pre and the selected topology are required, with the exact B2 owner selector and integrated authenticated selection. Wrong/mixed versions and modes, status mismatch and downgrade refuse. |
| BJ-01/02 | The selected identity requires JSON Boolean false and both literal null qualification associations. Omission, substitution or relocation refuses. The JSON Boolean is distinct from binary u8 status. |
| BJ-03/04 | The top-level key uses decoded name identity. Its value token has exactly 64 literal lowercase ASCII hex characters. Compare the decoded 32 bytes to the independent expected original-capability SHA-256. Missing, wrong-width and mismatching expectation refuse. Q1 H01–H14 and C01–C07 distinguish lexical, width, equality and provenance boundaries. |
| UTF-8, bounds and witnesses | One closed strict UTF-8 object, no BOM, duplicate decoded member, unknown member or invalid syntax. The entire raw credential JSON payload is bounded to 1–8,192 octets. S8191 and S8192 pass, S8193 refuses. U8192/U8193 distinguish an invalid non-ASCII path from over-limit byte counting. Owner witnesses reproduce 2,693/4,519 bytes and their recorded hashes. Independent forms measure 2,686/4,515 bytes with different valid synthetic values and member order. The specification permits ordinary JSON whitespace, order and in-domain escaping. The lengths and digests of synthetic examples need not coincide. |
| Binary and IE-01–03 | Independent packing covers exact 76-byte B2 9/36, plus integrated 76-byte base or exact 105-byte form. The added integrated pair has u8 sizes 26/1 at 76/103, with the exact UTF-8 bytes for `ae4-linux-fixed-fixture-v1` and `1`. Presence follows length. The separately bound profile, digest, role, protocol constants and reserved bytes remain checked. Only offsets 71 for 9/36 and 216 for B2 37 change from qualified status 1 to pre status 0 in the compared inherited forms. |
| Confirmations | Recovery 10 keeps 36 bytes, raw expected artifact digest at 4, success (1,0) and the inherited refusal reasons. B2 recovery 12 keeps 48 bytes, original .NET GUID then exact artifact digest, after verified 36 and before Ready. Both reproduce historical bytes and retain their context and order. Invalid lengths, digest/session mismatch and out-of-context forms refuse. |
| Release correlation | F2 §5 and AB line 34 define the same preimage, 258 bytes. The ASCII domain and zero, original raw capability/catalog/script/action hashes, LE u32 action index 2, four original .NET GUIDs in defined order and raw preparation manifest digest produce one SHA-256 value. Its 32 octets fill the existing ReleaseRequest.ApprovalCorrelationSha256 field. E-3 introduces no field, serialization, computation or comparison for this digest. E-4 owns the cross-format handoff and E-6 owns runtime recomputation and enforcement. |

The recorded 110 bootstrap outcomes comprise 13 structural passes and 97
refusals. The 46 binary outcomes comprise 10 structurally progressing
forms, one well-formed refusal and 35 other refusals. Six field-only
release controls distinguish agreement from wrong GUID order, wrong
action, missing approval and digest substitution or omission. The
combination of static traceability, valid examples and focused negatives
addresses the actual entry prerequisite. These are synthetic checks on
the specification. Transport ordering and framing were assessed
statically. Product, runtime and native conformance require later proof.

Two independent implementers can determine the same member sets, types,
presence/null rules, version selection, digest operands, exact binary
forms, inherited confirmations and refusal conditions. Variations permitted
in JSON serialization and in the presence of the integrated profile pair
are explicitly defined. Error diagnostics and the ordering of independent
failures carry no protocol semantics. The owner found no unresolved
material design choice or blocking Q1 defect.

## 3. Qualification dispositions

**Q1-E01 ACCEPTED AS A NON-BLOCKING QUALIFICATION.** BB §4 accepts
ordinary JSON escape spellings of the member name where they decode
ordinally to `AuthorityHash`. Compare decoded names and refuse duplicates
recursively. BJ-03 requires the digest value itself to consist of exactly
64 literal lowercase ASCII hex characters. Escapes and normalization of
that digest value refuse. The earlier scratch pattern searched for the
literal key bytes. Q1 J12 covers a valid escaped key, J03/J04 verify
duplicate refusal at two object depths, and H14 rejects an escaped
character in the digest value. Candidate §§1 and 3 already incorporate
the governing distinction. No revision or new grammar is required.
Future codec qualification must retain these three behaviors.

**BB-02 REMAINS OPEN AND UNREMEDIATED.** The integration path identified
in the bootstrap-base decision can emit both B2-only root members as
null in v2, and the decoder can accept them at its presence gate. The
observed behavior creates no compatibility exception. The frozen record
prohibits the two names in integrated v3-pre at every value and preserves
the accepted integrated v2 prohibition. E-3's isolated contract has no
dependency on the nonconforming path. Separate authorization is required
for remediation. Any reuse or exposure of affected behavior requires
explicit assessment and applicable independent regression qualification.

| Qualification or retained obligation | Evidence and owner disposition | Future trigger |
|---|---|---|
| Q1-E01 | Bounded scratch-probe limitation from the Q1 findings and controls above. Non-blocking for adoption. | Future implementation qualification on decoded member names, duplicate refusal and the literal value token. |
| External historical ancestry | Reconciliation and Q1 source evidence establish accepted raw-byte identities and canonical Git bindings. Independent external ancestry is unverified. Retain the limitation. Non-blocking for this internal record. | A later claim requiring independent external history needs new source-history evidence. |
| P0 durability | E-2 closure §6 retains separate, unassigned external-package preservation or independent source-history verification. Non-blocking for the isolated E-3 scope. | Preservation handoff, source loss/replacement risk or repeat external verification requires a separately accountable gate. |
| BB-02 | Bootstrap owner §3 and Q1 static evidence. Preserve the open implementation nonconformance outside normative adoption. Non-blocking for the isolated scope. | Reuse, exposure or remediation of the affected legacy path. |
| E2-R24–R27 and E-2 F-01 | Source-to-rule completeness is established for E-3. E-2 closure §§5–7 retain protected readers, exact policy, native qualification and the remaining composite Q-E1 allocations. Non-blocking for exact adoption. | E-4/E-5/E-6 with Slice N and the complete plan §3.5 dependency set. |
| First E-1 production consumer | E-2 closure §6 leaves it open and unassigned. Current implementation remains production-unreachable for the accepted isolated surfaces. Non-blocking for exact adoption. | Separate owner resolution before any first production caller. |
| RELEASE, P4 and native limits | E-2 closure §7 and Q1 preserve prior scoped evidence. The current owner action supplies structural specification acceptance. Non-blocking for adoption. | Exact codec revision at its qualification gate, E-4/E-6 P4 re-audits and Slice N protected native qualification. |

## 4. E3-ENTRY-F01 closure and remaining entry decision

The [original E-3 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01.md)
at `52b932aaebd52f16bd93170b95bb0f8e89241fd2`, blob
`d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12`, requires the authoritative
layout in Git and reconciliation of the codec-facing release boundary.
Its evidence rows R02/R04/R06 and the Slice E owner §§7 and 9 define the
blocker. The accepted provenance reconciliation binds the historical
inputs. The two prior owner rulings settle the exact v3 changes and the
complete inherited bootstrap. R1 incorporates them, Q1 independently
qualifies the exact candidate, and this decision formally adopts it.
Candidate §5 and the verified F2/AB sources settle the release
representation and the E-3 exclusion. Those facts satisfy both parts of
the finding. The historical entry assessment remains the record of its
original disposition. This decision records the subsequent closure and
leaves the remaining entry criteria to their separate decision.
[Entry requirements and blockers](../reviews/rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01-evidence/requirements-and-blockers.md).

**COMPOSITE Q-E1 OPEN.** E-3's normative portion is adopted. E-4 still
requires the authoritative GAVA1 header and wire reconciliation, authenticated
INIT/context and cross-format release handoff evidence. Preserve the original
identities and 32-byte ReleaseRequest representation across the E-3/E-4
boundary. E-6 must independently recompute and enforce runtime release
correlation under its atomic gate, including all plan §3.5 dependencies.
Protected K/M/F/S acquisition and readers and exact runtime case policy
retain their E-4/E-5/E-6 and Slice N allocation. The accepted E-2 scope,
including P0/D1/P1/P2, remains closed with 39 retained qualifications
and 23 downstream obligations under trigger-based governance.
[E-2 closure](rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md) ·
[Slice E owner](rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md).

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-ENTRY-OWNER-01`.
That entry decision must verify the exact adopted identities and review
the remaining entry checks, exact internal files and data-only interfaces,
implementation reuse and BB-02 exposure, managed positive and negative
tests including Q1-E01, legacy refusal without downgrade, valid and invalid
size boundaries, BJ-04 supplied comparison, exact binary variants and
independent qualification of the implementation revision. Require a
compiled production reachability check and an internal, production-unreachable
codec. Require RELEASE production verification to pass and Debug-as-production
to fail. Assess the exact revision's P4 implications and preserve
F-R1/O-R1, O-R2, O-R3, S10/S11 dominance, S28 isolation, first protected-effect
constraints, the observation-to-effect residual and mandatory E-4/E-6
re-audits. Those are implementation qualification requirements.
`NATIVE-PENDING` and all 331 unqualified native skips remain in force.

The adopted codec boundary is structural bytes and explicit expectations.
Validated shape or equality supplies no authenticated INIT, genuine
V/W/A, verified capability custody, live invocation, positive validation,
final admission or protected execution. Future callers own those
preconditions, including retaining the original capability and verifying
its digest and the pre catalog. This commission grants only normative
adoption and closure of E3-ENTRY-F01. E-3 entry, codec work, BB-02
remediation, E-4 through E-7, Slice N and operational authorization remain
separately gated.

[Owner identity checks](./rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01-evidence/identity-and-provenance.md).
