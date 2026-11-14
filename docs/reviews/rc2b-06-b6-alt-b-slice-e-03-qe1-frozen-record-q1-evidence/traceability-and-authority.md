# Source-to-rule completeness and authority

Source aliases resolve in [source verification](source-verification.md).
BJ is the [normative owner decision](../../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-normative-owner-01.md)
and BB is the [bootstrap owner decision](../../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md).
Authority proceeds from accepted historical requirements and adoptions to
BJ/IE then BB. Mechanical derivations and the proposed candidate follow
those authorities. Implementation observations and vectors corroborate
specific facts. They define no additional grammar.

| Rule | Controlling source | Candidate | Q1 evidence | Classification |
|---|---|---|---|---|
| V01 Exact version and topology selection | F1 44–48, AB 42–44/50–52, BB §§2–4 | §§2–3 | V01–V10, E17/E18/E26, K08/K12 | Inherited delta and explicit BB constraints |
| S01 Complete closed object graph, presence, decoded names | BB §4 and schema annex | §§1, 3 | 99-row matrix, M01–M24, J03/J04/J15–J18 | New closed owner-approved schema |
| S02 Types and scalar domains | BB annex scalar table, with B2 §2, SPEC §6.2.2, F1/F2 and BJ inputs | §3 | P-B/P-I, D01–D13 | Historical content plus explicit owner lexical choices |
| S03 Matching original identity, evidence and manifest data | BB §4 items 2–3, 7, B2 §2, F1/F2 | §3 items 2–3, 7 | P-B/P-I, X01/X07/X10, V11 | Explicit operand supplied independently. Manifest projection and role preimage retained. |
| S04 Integrated controller, times and runtime | BB §4 items 4–5, SPEC §6.2.2 | §3 items 4–5 | X02–X04/X09 | Exact projection plus specified internal comparisons |
| S05 Integrated profile | F2 §3 and line 94, BB §4 item 6 and annex | §3 item 6 | X05/X06/X08/X11, D06/D12/D13 | Caller supplies verified policy/profile inputs and original execution. No inference from peer path. |
| J01 Strict UTF-8 JSON and size | BB §4 | §3 | J01–J18, S8191–S8193, U8192/U8193 | Whole payload octets. Order and ordinary whitespace vary. |
| J02 Selected identity false and literal nulls | BJ-01/02, BB §2 | §§2–3 | M09–M20 | Exact distinct types and object containment |
| J03 Literal digest spelling | BJ-03, BB §4 | §3 | H01–H14, J12 | Literal lowercase hex value, decoded member key |
| J04 Digest comparison and supplied context | BJ-04, F1 24/38, F3 19–23, AB 44 | §§1, 3 | C01–C07 | Exact original capability expectation. Positive structural equality carries no authentication. |
| B01 B2 9/36 and 37 | B2 §3, B2-O, F1 46 | §§2, 4 | E01/E02, E07–E27 where applicable, F01–F05; GOLD comparisons | Exact binary layouts and status delta |
| B02 Integrated 9/36 and profile option | SPEC §§6.2.3/6.2.5, WIRE §C.3, IE-01–03 | §4 | E03–E27 and IE-V01–12 comparison | Presence by length, two u8s and fixed original UTF-8 pair |
| B03 10/12 confirmation | SPEC §6.2.4, B2 §3 and owner acceptance, F1 48, BJ/IE §3 closing text | §4 | K01–K14 and GOLD | Raw original digest and B2 session. Inherited reasons and order. |
| R01 Release-correlation formula and destination | F2 §5 line 100, AB line 34, normative owner §4 | §5 | R01–R06 below and exact original preparation preimage comparison | Existing 32-byte field. No E-3 release encode, recomputation or comparison. |
| A01 Structural non-authority, no fallback or promotion | F1 42–48, AB 42–52, BJ-01/04, E3 §§2/8 | §§1–6 | C06, E17/E18, K06/K12 and static analysis below | Authenticated caller, full integrity and phase duties persist |
| A02 Runtime execution and gates | SCOPE §§7/9, E2 §§4/5, PLAN §3.5, ROAD §8 | §§1, 5–6 | Static inspection | E-4 context and wire, E-5 custody, E-6 atomic recomputation, E-7 driver, Slice N native gates |

## Release boundary

Independent extraction found identical formula text in F2 and AB.
The domain is 29 ASCII bytes followed by one zero. Four raw digests
add 128 bytes, LE u32 action index 2 adds four, four .NET GUIDs
add 64, and the preparation manifest digest adds 32. Total is
258. SHA-256 yields 32 raw bytes. Q1 reconstructed the original
preparation preimage and R01 exactly, then used new, nonuniform GUIDs
to exercise the inherited mixed-endian .NET order rather than just
uniform bytes. Inputs are retained original capability, catalog, script,
release action, session, execution, lease, preparation and its manifest.
The field is existing `ReleaseRequest.ApprovalCorrelationSha256`.
Current `OwnerHeldWire.cs` corroborates that its non-null logical value
is 32 bytes in the existing ReleaseRequest record. Its existing outer JSON
serialization is outside the v3 pre bootstrap codec. The sender's approval
indication must be true and the E-6 owner independently verifies the
original binding, allowed action, preconditions and exact correlation.

| ID | Mutation / context | Expected | Observed |
|---|---|---|---|
| R01 | Exact reviewer digest, flag true | field agreement | field agreement |
| R02 | GUIDs use network byte order | refuse | refuse |
| R03 | Action index 1 instead of 2 | refuse | refuse |
| R04 | Digest matches but approval flag is false | refuse | refuse |
| R05 | Use the capability SHA-256 itself | refuse | refuse |
| R06 | Omit correlation | refuse | refuse |

Reviewer preimage (258 bytes) and digest (32 bytes)

~~~text
676167616d62612d76616c69646174696f6e2d72656c656173652d7631000a734b62f7fd4fc45277b77cfdf572ca6cccc622b851aa66100c9f4ceb6219b4b7f4ce706438df82c6b855c373a03933cab567b6e79d8c42f514fe613d67b09a4c888021942a3c0ec8f7d2eae84c786eb027beef646a7e726227611c9210eefbb51da5a0468fcef2a339e96696a2ff46f1791f18e0fa4d58c9196f36c3ab703902000000d1c3b2a1f6e51847a92a3204567890abd2c3b2a1f6e51847a92a3204567890abd3c3b2a1f6e51847a92a3204567890abd4c3b2a1f6e51847a92a3204567890ab70b7925748f8932ad6a0118ea4653e0cae86d4b27cc44c09bf4af5a53c8c8188
df6b4e7b682d11afde4db1d639eb5a5193683967aa7cd26ea53631777969e277
~~~


## Non-authority and implementability

Numeric bootstrap version, fixed strings, fields and type-specific lexical
domains, exact binary offsets, and designated supplied expectations can be
implemented by two independent readers without an encoding choice.
Permitted JSON property order, ordinary whitespace and in-domain escape
spellings preserve decoded content, with the explicit BJ-03 value-token
exception. Binary positives use fixed fields and, only for the integrated
echo and within its exact contract, an optional pair. An implementation
need not reproduce literal witness JSON order. An integrated echo sender
may choose either accepted length, and receivers must accept both with
the requisite profile context. Refusal is deterministic. Exceptions and
diagnostic ordering are outside the wire contract.

`Qualified:false` selects pre integrity data, and status byte 0 belongs
to the binary echo and 37 formats. Neither changes phase. AuthorityHash
matches original capability bytes through an expected digest. Echoes and
confirmations bind the original digest/context and carry no separate grant.
Release correlation binds an action and preparation at a later gate.
All these results, including successful parse, express at most structural
agreement. None supplies genuine V review, W owner acceptance, A
administrative approval, trusted capability provenance, protected P custody,
live invocation authorization, positive validation, final admission or
protected execution. Under F1 and E-3 entry assessment §2, structural data
cannot satisfy a Qualified or general admission API or construct an
operational `LoadedInstallation`/live validation context.

Authenticated original INIT, retained exact capability bytes and matched
catalog, installed/loaded-role integrity, peer and lifetime checks, and
the originally bound comparison projections are later caller obligations.
E-4 owns GAVA1 grammar and authenticated handoff, E-6 recomputes and
enforces release against its own retained inputs, and composite Q-E1
stays open. Stop/cleanup, protected readers, runtime policy and custody
remain governed by their established gates.
