# Alternative B Slice E-3 Q-E1 bootstrap base owner decision

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-BOOTSTRAP-BASE-OWNER-01`

Date 2026-11-15 (Asia/Manila)

## 1. Baseline and authority

| Starting identity | Verified value |
|---|---|
| Canonical checkout | `C:/Users/Laszlos/source/repos/Gagamba`, sole worktree on `main` |
| HEAD / tree | `fcf61cce5cdf56b1f96d86bee3e79fad522a9e19` / `f71afd4aedeb9dbaed0507e94e57919d91734c2c` |
| Local tracking reference | `a30d1807686ee15d39ef7e9103e8b992062bd971`, 6 ahead and 0 behind |
| Preservation | Clean tracked files and index, no stashes or Git lock observed, all 44 historical untracked paths SHA-256-identical |

The [partial candidate](../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md)
is blob `d734d61945192a7c4e2558d949c56c4f75094fdf`, raw SHA-256
`c7e12dcfaf9df04177d2a7e50f805deb9bf06737f81836ce4946d56e801a28bc`.
Its [preparation report](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01.md)
at starting HEAD is blob `566201901c26e9236a9a38636209e96961210fe2`, evidence tree
`bdb8a82842afc034f5f846478920701a81a2f032`. FRP-01-A is the integrated
member-presence discrepancy. FRP-01-B is the incomplete inherited schema.

The [normative owner ruling](../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md)
is at `f12a84943ea30153ccf96dfef21866a78e3fb934`, blob
`f3639dc619dbfee39859afb7da553e2167d9b83d`. It follows the reconciliation
`2a3dbcef99fdd9073e0c8c5db0f6371b5f7a5120` (report
`a61febbd417ad6628a47bec0cffa48e6bc1c8e0a`, evidence tree
`c2205a46c5b5480de36f8dac61c208097dba16b5`), entry assessment
`52b932aaebd52f16bd93170b95bb0f8e89241fd2` and E-2 closure
`21179680e7ae5d153c7ddeef5d209e5ac450cdd7`. These were checked as a direct
parent chain. All 12 raw source files matched the current
[committed inventory](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md)
(blob `8bb7daa7b2b0e635ba2445c78a7ac1fbf395c1ab`).

| Source and verified identity | Version / authority | Relevant rule and effect |
|---|---|---|
| [Original B2 acceptance](rc2b-02-b2-owner-acceptance.md), blob `3056254758f56d8a1cb99dd9b03f08e6e05fa689`, and [B2 evidence](../evidence/rc2b-02-b2.md) | Accepted legacy v1 implementation envelope | Bounds the credential-channel JSON and private association. The accepted scope supplies the transport and semantic base, with the v3 inventory explicitly selected here. |
| [SPEC](../design/rc2b-06-b6-installed-artifact-trust.md) §§6.1–6.2.6, blob `7f9f615f75527221e81b7aaa498f4b564acb62b9`, and [WIRE](rc2b-06-b6-wire-owner-acceptance.md) §§C, D.2, H, blob `501487227208d935c3ab1337c9f87055c9e14b8d` | Accepted integrated v2 identity and corrected wire delta | Names the integrated identity and inherited member semantics. The illustrative surrounding JSON omits fields and does not set a complete root and nested grammar. |
| [B2 amendment](../design/rc2b-06-b6-s3-b2-wire-delta-01.md) §2, blob `95dc3221ae2a5579f966526516073cdd0f392aa9`, committed SHA-256 `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6`, adopted by [B2 owner](rc2b-06-b6-s3-b2-wire-owner-acceptance.md), blob `76b70f6832733c7cf58ffaa56afbd3df1a9f014a` | Accepted additional B2 v2 fields and integrated prohibition | Requires `B2InstallationIdentity` and `B2DeadlineNs` in B2. Forbids both in integrated. Strict shape and presence control over current code. |
| F1 actor/descriptor, SHA-256 `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93`, lines 42–48, AB compatibility SHA-256 `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb`, lines 21, 42–44, 50–52 | v3-pre delta over corresponding v2 shapes. [F02 acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) §2 and [Alternative B design](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) §§3.3–3.5, 4 | Explicit pre mode/version plus inherited shape. Keeps runtime authentication separate. Complete wire choices follow owner authority. |
| F2 case/policy, SHA-256 `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7`, §3 and lines 94, 98–104; F3 capability SHA-256 `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010`, lines 19–23 | Accepted original data meanings and capability binding | Profile claims remain tied to independently verified F/M and installed-integrity data. Capability and release have different encodings and roles. No new release member. |
| Normative owner §§2–3 | Binding BJ/IE delta | Prior owner selected false, mandatory nulls, exact capability digest and integrated echo rules. They take precedence for these new v3 fields. |
| Current production source, identities in §3 | Implementation evidence | Supplies an inventory and reveals permissive null handling. This decision adopts explicit v3 rules where sources delegate, with source code used only as compatibility evidence. |

## 2. Version inheritance and BB-01

| Context | Version and identity | Top-level B2-only fields | Other differences |
|---|---|---|---|
| Legacy v1 | Existing unqualified bootstrap | Existing legacy behavior is unchanged | No pre mode, no authority fields, no v3 interpretation |
| Qualified B2 v2 | JSON 2; selected B2 identity is S2 Qualified | Both required, typed as identity object and positive canonical i64 deadline | `Integrated` and `DelegatedParent` null. No BJ additions. |
| Integrated v2 | JSON 2; `Integrated.InstallationIdentity` is S2 Qualified | Both prohibited under B2 §2 | Existing integrated layout and postvalidation context remain governed by v2 and separate authority. |
| B2 v3-pre | JSON token 3; B2 identity with BJ fields | Both required, typed as pre identity and positive canonical i64 deadline | Two authority fields added. `Integrated` and `DelegatedParent` literal null. B2 owner selector exactly `--b2-lifeline 3`. |
| Integrated v3-pre | JSON token 3; integrated identity with BJ fields | Both prohibited by inheritance | Two authority fields added. `Integrated` is a complete object and delegated parent is required. Runtime selection comes from authenticated bootstrap context. |

**BB-01 RESOLVED.** The exact names are `B2InstallationIdentity` and
`B2DeadlineNs`, at the root. In B2 v3-pre, missing, null or wrong-type forms
refuse. In integrated v3-pre, presence at any value, including literal null,
refuses. Required nested `qualificationId` and
`qualificationRecordSha256` from BJ-02 are distinct fields. Each remains
required exactly once with literal null in the selected identity object.
FRP-01-A is normatively resolved through B2 §2, F1 line 46 and this ruling.

## 3. BB-02 and BB-03

**BB-02 — Normatively nonconformant on the observed integrated v2 path.**
`IntegratedB3Client.Prepare` (blob `94d3427cd5956bfbb00fd9d2c05a8ffdf3908cfb`)
chooses v2 for an installed integrated invocation and omits the B2 arguments
in the CLR constructor. `QualifiedRecoveryConnection.Start` lines 15–28
and 74 (blob `2f6f779d6b5414c22790a7fb8a892b253e044963`) retains the null
identity, sets the deadline null in integrated topology and uses default
record serialization, emitting both JSON members. `RecoveryProtocol.cs`
lines 129–177 (blob `f19017dccce5b83d42bb9276c6e8839d13564061`) recognizes
both names as CLR properties and tests their deserialized values. Omission
and explicit null can both pass that integrated presence gate, subject to
the rest of validation. A non-null B2 identity or deadline cannot pass it.
The current decoder accepts versions 1/2 and contains no E-3 v3-pre path.
This observation was made by source inspection.

The focused `InstallationWireTests` round trip covers **B2**, including
required non-null B2 values and duplicate/version negatives. Existing
integrated runtime tests and call-site coverage do not establish a
contract-level expectation that these particular null members be emitted or
accepted. No test was run. The current serializer/decoder are outside the
bounded isolated E-3 codec surface. Any legacy correction and its
compatibility evidence need separate authorization. The code discrepancy
remains open.

**BB-03 — Preserve the accepted version boundaries.** Accepted B2 values,
legacy wire vector identities and the accepted integrated prohibition remain
in force. No v3 null exception, version-agnostic fallback or downgrade is
created. A raw integrated v2 payload containing the two nulls can reflect
deployed serializer behavior without satisfying the accepted schema. This
decision adds no v2 remediation, migration requirement or permission to
modify historical v2 bytes. The E-3 oracle uses exact wire-member presence.

## 4. Complete v3-pre base member and byte contract

The [normative schema and completeness matrix](rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md)
is incorporated in this decision. It is exhaustive for both roots, selected
pre identity, both executable pins and closure maps, integrated body,
controller, runtime and fixture profile. Every listed member is required once.
The only nulls are enumerated. No array, optional member, default,
unlisted field, or recursive launch object is permitted. The selected
fixture projection requires literal null `IntegratedRecovery` as an explicit
recursion cut. Debug profile metadata and the internal
`QualifiedInstallation` property are outside the closed v3 projection.

The new owner bindings close historical omissions without changing v2.
They select the identifiable production field set, explicit member presence,
numeric and textual domains, and exclude Debug fields. The two runtime
inventory fields retain uppercase hex to match the installed runtime pin
representation. The S2 identity digests remain lowercase. This is a
field-specific wire choice for v3, with raw digest agreement where required.
It does not change BJ-03, allow mixed-case aliases, or impose a new
whole-document canonicalization. The bounds are determined by the
established profile and the new scalar table, independent of CLR defaults.

### Encoding and refusal

Accept exactly one strict UTF-8 JSON object with no BOM; reject invalid
UTF-8, invalid escapes or unpaired surrogate escapes, comments, trailing
commas, trailing non-whitespace and additional top-level values. RFC JSON
whitespace (ASCII space, tab, CR, LF) is permitted outside tokens before,
within and after the object. Property order and a final newline have no
semantic effect. Ordinary JSON escape syntax is allowed only when the
decoded member name or decoded string satisfies the exact schema. Names
compare ordinally case-sensitive, and repeated decoded names refuse
recursively, including escaped aliases of an earlier name. BJ-03
separately rejects all escapes in the AuthorityHash token interior.
These exact document-level rules are owner bindings where the historical
source is silent. There is no CJSON-1 dependency. No reserialization or
normalization is used to establish byte identity.

Bound the entire JSON payload on the private credential channel to
`1..8192` received octets before decoding. This includes all whitespace
and the UTF-8 bytes of any multibyte characters. It excludes socket framing
and ancillary metadata. There is no terminator. Exactly 8192 bytes passes
the size check and still requires schema and comparison success. Refuse
truncation, unsupported shape, missing, duplicate, null or unknown members
outside the exact rules, wrong kinds, numeric overflow and each violated
relationship below. Refusal produces no structural result. The decision
specifies neither implementation exceptions nor validation order amongst
multiple malformed fields.

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
from the closed schema. [Complete positive and targeted negative witnesses](rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/witnesses.md)
exercise both forms, including the boundary and the BB-01 distinction. The [mechanical check](rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/mechanical-checks.md)
matched 99 required member rows across 11 closed inventories, 4 structural
positives and 22 targeted refusals.

## 5. Disposition and effect

**QE1 BOOTSTRAP BASE SCHEMA RESOLVED**

FRP-01-B is owner-resolved by the complete v3-only schema, with no remaining
normative bootstrap choice in this scope. FRP-01-A's normative interpretation
is settled. The integrated v2 serializer/decoder nonconformance remains a
separate implementation observation. BJ-01–04 and IE-01–03, exact 76-byte
B2 and 76/105-byte integrated echo, binary status-zero, 10/12 digest
confirmation, and the existing 32-byte `ReleaseRequest` boundary remain
binding. The prior binary and release checks were not rerun.

The partial candidate retains its original bytes. E3-ENTRY-F01 remains open
until complete record preparation, independent frozen-record Q1 and formal
owner adoption. E-3 entry and implementation require separate authorization.
Composite Q-E1 remains open. GAVA1 header/wire remains E-4, runtime owner
correlation remains E-6, and E-2 closure preserves protected readers and
exact case policy in the accepted atomic dependency set. Preserve E-2's
39 qualifications and 23 downstream obligations by its immutable closure
and assessment references, unassigned first E-1 consumer and P0 external
durability, RELEASE verifier and Debug negative control, P4 residuals
and E-4/E-6 re-audits, `NATIVE-PENDING` and all 331 unqualified skips.
This decision confers no operational approval, positive validation or
protected execution.

The proposed next commission is
`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-FROZEN-RECORD-PREP-R1`.
Incorporate this exact schema into the partial candidate, preserve the
existing binary evidence, and carry complete bootstrap vectors into a final
completeness audit. Recommend fresh independent Q1 only when ready.
