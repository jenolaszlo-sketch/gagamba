# Alternative B Slice E-3 implementation entry owner decision

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-ENTRY-OWNER-01`

Date 2026-11-15 (Asia/Manila)

**E-3 ENTRY AUTHORIZED WITH QUALIFICATIONS**

Authorize one subsequent package for an internal, production-unreachable
v3-pre structural decoder and focused managed tests. Choose **BB-02 Option A,
independent strict codec**. The tests may exercise structural success using
independently supplied data. All production entry points continue to refuse
v3-pre. Implementation, independent implementation qualification and owner
acceptance are separate milestones.

## 1. Verified baseline and binding authority

| Item | Verified identity |
|---|---|
| Checkout | `C:/Users/Laszlos/source/repos/Gagamba`, one `main` worktree, clean tracked state/index, no stash, no Git lock or identifiable conflicting writer |
| Entry HEAD and tree | `38e7c212d31211bb817f042bdd2737398726ce90` / `40cfa343093cfad11647a4db62eb3ae0cb38265d` |
| Recorded origin/main | `a30d1807686ee15d39ef7e9103e8b992062bd971`, 10 ahead / 0 behind, tracking data only |
| Historical untracked inventory | 44/44 exact paths, lengths and SHA-256 values, inventory SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1` |
| Frozen-record adoption | Decision blob `d2fea61b7db323319ee59033c8b1b12ded1ffe7d`, evidence tree `0d5d92fce4afdc5bc008d1de48aac688c4c9e7f4` |
| Frozen v3-pre record | `docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md`, blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`, 21,225 bytes, raw SHA-256 `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Normative BJ/IE and bootstrap-base decisions | `f3639dc619dbfee39859afb7da553e2167d9b83d`, `93fe2c9155a21b12fd3486b9da743260897d508e` |
| Complete bootstrap schema and owner witnesses | `a7248152d09e19d8f3775db397f340949fa9ee57`, `19886332f3fb1bcf3149f4e173642a8673fbb746` |
| Frozen-record Q1 | Report `5f8b92e4874378bb365fcec80d794f964aa6f821`, evidence tree `1cb5ac2c2b467cbfb1c0d4ac85682fb979ffe961` |
| Roadmap and prior E-3 entry assessment | Slice E review `0c311fc84bde05d3c9e897ff51c8e44832aea987`, implementation plan `485ad1ac76e836d2bd6eb826131a01294c9bda8c`, entry assessment `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12` |
| E-2 closure and P4 refusal-only acceptance | `3388894f821a4a92aca7c2b2d79c832a0f70a450`, `31ae1b08cb4663a6a9d9de657fd972fccb731cf2` |

The candidate bytes in Git and in the working file were identical. The blob
also resolves to the exact R1 input reviewed by Q1. The adoption binds the
complete schema, BJ-01–04, BB-01/03 and IE-01–03, plus the immutable
accepted historical bases and digest/vector references in adoption §1.
Q1 established both full bootstrap forms and the binary layouts, including
strict refusals and the release-field allocation. The adoption expressly
adjudicated Q1-E01 and separated BB-02. **E3-ENTRY-F01 remains closed by
exact frozen-record owner adoption.** The adopted specification is the
normative oracle for implementation. The Q1 vectors and owner witnesses
are test evidence. The candidate's preparation-state caption remains
historical.

[Adoption](rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-owner-01.md) ·
[Frozen record](../evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md) ·
[Independent specification Q1](../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-q1.md) ·
[Original entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01.md).

## 2. Entry prerequisites

| Prerequisite | Evidence | Status | Required action |
|---|---|---|---|
| Canonical baseline; E-2 predecessor and retained ledger | §1, E-2 closure §§5–10 and original R01 | SATISFIED | Preserve exact revisions and trigger ledger. |
| Adopted byte-level layouts and codec-facing release boundary | Adoption §§1–4, R02/R04/R06 and original entry checks 4–6 | SATISFIED | Use the adopted objects and vectors. Preserve E-4/E-6 ownership. |
| Caller boundary and bounded surfaces | Original R03/R07 and checks 7–9, source inspection and §3 below | SATISFIED | Maintain a supplied-data boundary and leave all existing consumers unchanged. |
| Legacy refusal and BB-02 | R05, bootstrap owner §§2–3 and current legacy source | SATISFIED WITH QUALIFICATIONS | Option A. Verify legacy version refusal and enforce presence-based v3 rules independently. |
| Managed evidence, RELEASE, compiled isolation, P4 | Original R08/R09 and checks 10–16, §§4–5 below | SATISFIED WITH QUALIFICATIONS | Entry plan is established. Exact implementation revision must pass the exit gates. |
| Exact implementation, independent review and acceptance | Slice E owner §9 and roadmap §9 | DEFERRED BY ACCEPTED DECISION | Produce and verify implementation evidence, then obtain separate owner acceptance. |
| E-4 GAVA1, E-5 launcher, E-6 atomic set and E-7 driver | Original R10, E-2 R24/R25/R27 and plan §3.5 | DEFERRED BY ACCEPTED DECISION | Keep the adopted handoff and original identity semantics. Each downstream slice requires separate entry. |
| First E-1 production consumer and P0 external durability | E-2 closure §6 and adoption §3 | DEFERRED BY ACCEPTED DECISION | Obligations remain open and unassigned. This isolated codec creates neither trigger. Independent external Git ancestry remains unverified. |
| Protected/native qualification | E-2 R26, plan §3.6 and adoption §4 | DEFERRED BY ACCEPTED DECISION | Keep `NATIVE-PENDING` and all 331 native skips unqualified. Slice N owns separately authorized native proof. |

No unmet or inconclusive prerequisite blocks this bounded package. E-2
remains closed with 39 retained qualifications and 23 obligations. P0/D1/P1/P2
acceptances and P1/P2 structural-versus-authority limits carry forward.
Protected K/M/F/S readers and exact case policy retain their E-4/E-5/E-6
and Slice N gates. E-6 remains accountable for the full plan §3.5 set.

## 3. Exact implementation authorization

Only the following new paths may be created in the subsequent implementation
package. Use the existing M1 and M1 Tests project globs and friend access.

| Path | Authorized purpose |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodec.cs` | Pure managed bootstrap decoder, closed schema and comparison logic. Private parsing helpers may stay here. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodecData.cs` | Internal structural values and supplied expectations, including topology, relevant message context and refusal result types where needed. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationPreCodecBinary.cs` | Payload-only 9/36/37 and 10/12 validation. Private layout helpers may stay here. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecBootstrapTests.cs` | Contract, bootstrap, malformed-input and legacy refusal tests. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecBinaryTests.cs` | Binary layout, version, confirmation and refusal tests. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecVectors.cs` | Test-owned fixture bytes and independent expectations, with exact source/vector identities. |
| `docs/evidence/rc2b-06-b6-alt-b-slice-e-03-codec-implementation-01.md` and same-stem evidence directory | Producer evidence and any necessary bounded reproducibility material for the exact implementation. Temporary probes and compiled artifacts stay outside the repository. |

The only authorized existing-file updates at implementation handoff are
the current-state sections of `docs/PROJECT-STATUS.md`, `docs/handoff.md`
and `docs/work-queue.md`. Existing source, tests, projects, package and
build settings, solution, verifier and all adopted contracts remain protected.
Keep the change within the allowlist. Use standard-library managed byte,
JSON and binary primitives. Existing v1/v2 code and helpers may serve as
read-only references or direct legacy test subjects. The codec depends
on no repository parser, serializer, trusted model, fixture hook or
native module. [Source and dependency evidence](rc2b-06-b6-alt-b-slice-e-03-entry-owner-01-evidence/surfaces-and-gates.md).

Accept raw bootstrap or message payload bytes, explicit selected pre
topology/context, applicable message/expected role and independently
supplied original comparison data. Bootstrap expectations include the
separate 32-byte original-capability digest and the exact adopted
bootstrap projections. Binary expectations carry the relevant original
digests, profile, GUID and confirmation prerequisite as ordinary data.
Use distinct internal structural values, kept independent of the existing
qualified and trusted types. A failed check yields a bounded refusal with
no validated payload and no sensitive bytes in diagnostics. Local categories
may distinguish version/context, size/encoding/schema, and equality or
confirmation failures. They create no new wire codes and have no prescribed
precedence among simultaneous failures. Expose no public API, new interface
or abstract service. Implementation may choose internal signatures and
equivalent algorithms within this contract.

Scope is decode, validate and compare of the adopted bootstrap and
payloads. No shipping serializer, frame transport, descriptor handling,
handshake state machine, dispatch registration or owner selector activation
is authorized. Test-owned fixture builders may independently pack bytes
where needed. Context comparators use the supplied original data.
Authentication, phase establishment, retention and verification of the
original capability and pre catalog, transport, proof of predecessor
messages and trusted custody are future caller responsibilities.
Successful structural data confers no authority.

**BB-02 remains open and unremediated.** Integrated v2 can emit and accept
both B2-only root names at null. Option A uses decoded member identities
with a closed v3 grammar and required presence or prohibition, with no
legacy parse or fallback. It needs no change to the legacy call chain,
qualified model, protocol types, test projects or package graph.
B2 v3 requires both B2-only members non-null. Integrated v3 refuses each
name at every value including null. Legacy compatibility remains within
the accepted prior scope. Exposure, remediation or reuse of affected
behavior requires a separate decision and applicable regression proof.

## 4. Managed qualification and release boundary

| Area | Required implementation evidence |
|---|---|
| Complete bootstrap | Run complete B2 and integrated positives with all 11 closed objects and 99 required rows accounted for by the traceability matrix. Cover exact nullability, scalar and nested types, original comparisons and internal cross-field relationships. Demonstrate strict UTF-8, no BOM/trailing data, missing and unknown fields in root and nested objects, canonical numeric grammar, type and null failures, broken cross-field equality, legal whitespace/order/escapes and limits 8,191/8,192 accepted and 8,193 refused for otherwise valid padded fixtures. Exercise byte counts before decoding. |
| BB-01 and Q1-E01 | Byte-level tests for each B2-only field missing or null in B2 and present in integrated, including literal null. Verify an escaped name decoding to `AuthorityHash` is accepted, alternate raw spellings of the same decoded member refuse as duplicates recursively, and an escaped hex character inside the value refuses. Use separate byte vectors for each. |
| BJ-01–04 | Confirm Boolean false and both nested literal nulls. Cover omissions, wrong topology, type/alternate status and non-null associations. Verify 64 literal lowercase ASCII hex characters and compare 32 bytes with an independent expected digest. Refuse alternate spelling, wrong type or width, missing and mismatched expectations. Matching supplied data proves content agreement only. |
| Exact binary forms | 9/36 B2 exact 76, integrated exact 76 or 105 for both roles, IE-01–03 exact pair and one-octet lengths, B2-only 37 at 246 bytes for the bound pair. Verify original digest/role, LE constants, reserved bytes, status 0 at 71/216 and expected profile. Refuse altered offsets/fields, unsupported profiles/status, wrong prefix/length, truncated payloads, substitutions, excess data and wrong context. |
| Confirmations | 10 at 36 bytes with exact original artifact digest and inherited success/reason rules. A correctly shaped refusal permits no positive result. B2-only 12 at 48 with .NET GUID and exact digest. Exercise wrong digest/session, truncation, wrong context, missing predecessor satisfaction and the accepted success relationship. |
| Legacy and downgrade | Verify that current managed legacy bootstrap and applicable binary validation entry points still refuse representative adopted v3 bytes, with accepted v1/v2 controls where applicable. The new path refuses legacy, unknown or mixed contexts, wrong selectors and phase, and follows no fallback. Ensure failed decoding returns no usable value or protected effect. |
| Oracle and execution | Bind accepted witness and Q1 vectors, exact bytes where fixed and valid alternative JSON encodings where allowed. Test-owned input builders and expected results must be independent of the implementation. Use the existing managed project and targeted legacy regressions and record commands, discovered tests, results, skips, artifact hashes and traceability. No required managed case may be skipped. |

The established 258-byte release preimage yields a 32-byte result in
the existing `ReleaseRequest.ApprovalCorrelationSha256` field. Adopted §5
assigns E-3 no release-field parser, serializer, digest computation or
comparison. In E-3 preserve the distinct capability, installation and
release identity meanings. **Composite Q-E1 stays open.** E-4 owns GAVA1
header/wire and the authenticated context and cross-format handoff.
E-5 retains protected launcher/custody. E-6 owns runtime owner recomputation,
enforcement and atomic dependencies. E-7 retains the driver and Slice N
retains protected qualification.

## 5. Exit, independent qualification and acceptance

Producer readiness requires the authorized code and tests in an exact
commit with a clean tracked checkout, traceability and managed results
for §4, verified compiled isolation and RELEASE/Debug controls described
in the evidence annex. Required checks that fail or remain unverified
block the qualification handoff. The scanner must exercise a known
positive and report its coverage and limits. Verify Debug and RELEASE
artifacts, including host and project integration. The existing RELEASE
verification must pass on Release and fail with the expected qualification
diagnostic on Debug supplied as production. No new artifacts were built
or qualified by this entry decision.

The P4 trigger is introduction of a new structural refusal, version and
comparison surface. Require exact-revision proof it has no production call,
status promotion, changed control flow to a first protected effect, or
downgrade. Preserve F-R1/O-R1, S10/S11 upstream domination, S28 test-only under
O-R2, O-R3 refusal-reason typing, first-effect checks and the accepted
observation-to-effect residual. A new caller, effect, or authority conversion
invalidates the affected existing proof and requires separate authorization
and targeted P4 review. Mandatory E-4 and E-6 re-audits remain at those gates.
Entry is a design disposition. Confirm P4 implications on the actual
implementation revision. [P4 acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md).

After producer completion, request a separate independent implementation
Q1. It must bind the adopted spec and exact commit, examine every changed
path and the dependency boundary, generate independent positive/negative
vectors, exercise Q1-E01, BB-01/02 isolation, binary and confirmation rules,
legacy refusal and no authority path, and reproduce the compiled isolation,
RELEASE/Debug and applicable P4 evidence. Include a scanner positive and a
disposable control showing detection of a removed required check, without
altering the canonical checkout. Record findings, limits and a disposition.
A subsequent owner acceptance must bind exact source/evidence and Q1,
adjudicate findings and verify every completion gate before accepting the
codec for this refusal-only handoff. Producer completion, independent
qualification and acceptance confer no production activation.

If implementation finds a missing wire rule, conflict, refusal ambiguity
with security impact, new production path, native dependency, BB-02
dependency or change outside the allowlist, record the exact finding and
seek separate authorization before expanding scope. Resolve routine
internal choices normally when they preserve the adopted behavior and scope.

Recommended next commission
`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-IMPLEMENTATION-01`.
The granted scope contains no authenticated INIT, V/W/A, R/Wc/P issuance,
protected provisioning, publication/withdrawal, custody, live invocation,
release approval, positive validation, final admission, native integration,
or protected execution. A future production consumer requires a separate
owner decision. No implementation or independent implementation Q1 begins
in this commission.
