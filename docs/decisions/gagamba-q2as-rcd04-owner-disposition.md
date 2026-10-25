# Q2A-S / RC-D04 owner disposition

Date: 2026-10-25 (Asia/Manila).
Status: **OWNER ASSIGNED TO RC2A-02C — OPEN, NOT QUALIFIED.**

This decision assigns the remaining RC2A-02 public-surface/passive-reference
obligation to a narrow, evidence-only qualification stage before RC2A-03
qualification or acceptance. It does not mark the obligation complete, waive
the contract, accept full RC2A-02, or authorize RC2A-03 or later milestones.

## 1. Decision and rationale

The owner rejects permanent acceptance of the limitation. The accepted RC-1
contract requires a validated passive preparation reference with content
identity and no time or control authority. Some focused tests and probes support
the implementation, but the accepted RC2A-02 traceability record explicitly
defers Q2A-S / RC-D04. That leaves a contract assurance gap, not a demonstrated
production defect.

The owner assigns this gap to a planned **RC2A-02C: Q2A-S / RC-D04 focused
qualification**. It is the next qualification action for this obligation and
must complete before full RC2A-02 acceptance and before RC2A-03 qualification
or acceptance can rely on the public reference. The source implementation for
RC2A-03 was already recorded as implemented under RC2A-01; this decision neither
restarts nor commissions that source work.

RC2A-02C is planned, not started. This disposition records scope, acceptance
criteria and sequencing only. It makes no new test or production-code change and
does not claim new execution evidence. The open status remains until the
criteria below are evidenced, independently reviewed and separately dispositioned.

## 2. Authoritative requirement and exact scope

The original source is RC-1 design
[`restricted-consumer-contract-rc1.md`](../design/restricted-consumer-contract-rc1.md)
§RC-D04, commit `d609c29d3048bc0fd91583068cb013fb906fdf90`, file blob
`4b300b01397e7f68d978fadbc36164dda17c8721`. The accepted public API contract is
[`restricted-consumer-contract-rc2a-01-api.md`](../design/restricted-consumer-contract-rc2a-01-api.md)
(blob `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` at the bound RC2A-01 tip).
The [RC2A-02 qualification plan](../plans/restricted-consumer-contract-rc2a-02-qualification.md)
§3 defines Q2A-S as the public-surface/misuse family; §4 maps RC-D04 to
passive-reference rows plus Q2A-S, and R1-F04 to the negative
temporal-surface/documentation check.

For this open RC2A-02 obligation, the exact public value is the sealed
`RestrictedPreparationReference` with five get-only properties:

- `ExecutionId`, `IssuerId`, `GenerationId`, and `PreparationId`: four nonempty
  GUIDs.
- `ManifestSha256`: exactly 32 bytes, copied into immutable storage.
- Equality and hash code compare the four GUID values and digest **contents**;
  equality must not depend on the digest array's backing-object identity.
- The public surface remains passive: no lookup, release, adoption,
  deserialization-to-capability, expiry-renewal, preparation or other control
  member. It has no expiry, deadline, liveness or other temporal field.
- R1-F04 consumer documentation remains explicit that a passive reference does
  not establish timing or current liveness: D4a timing comes from explicit
  enrolled/synthetic data; later D4b timing comes only from an actual
  provider-origin prepared execution.

The public constructor is intentionally permitted and may copy values from
evidence, including test evidence. This stage must not add requirements for
fresh/unique GUIDs, cryptographic attestation of the reference, recomputation of
the manifest digest by the constructor, expiry, provider identity, or a live
operation binding. Those are not properties of this passive RC-2A value.

RC-D04 also describes a distinct RC-2B live-operation half: a
`PreparedRestrictedExecution`, provider/artifact descriptor, retained owner
state, monotonic deadlines and release/admission checks. Those control and
deployment obligations are **outside RC2A-02C** and remain in their existing
RC-2B work. This disposition does not qualify them.

## 3. Why the item remained open

The RC2A-02A authorization and executed traceability matrix marked RC-D04,
Q2A-S and R1-F04 **DEFERRED** outside its authorized family. The RC2A-02A owner
decision accepted that deferral as an evidence limitation and explicitly left it
open for full RC2A-02. RC2A-02B then qualified the declared Windows/WSL2
portability set; its scope did not silently add Q2A-S. Q1 and Q2 preserved that
boundary, and Q2 did not disposition this separate obligation.

This was a scope/traceability deferral, not a recorded test failure or finding
that the implementation violates RC-D04. Existing tests were not promoted
retroactively into Q2A-S qualification because the required rows were deferred
in the accepted matrix.

## 4. Evidence and its limits

Supporting evidence already exists:

- [RC2A-01 focused evidence](../evidence/restricted-consumer-contract-rc2a-01/clarification-2026-10-25.md)
  records 95 passing cases, including preparation-reference checks and a
  public-surface check; these were implementation tests, not the separately
  authorized RC2A-02 qualification.
- [`RestrictedContractTests.PassiveReferenceCopiesAndComparesContents`](../../tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs)
  checks
  valid content equality/hash behavior, digest copying, changed identifiers,
  empty GUIDs and digest lengths. `PublicSurfaceIsSealedReadOnlyAndPassive`
  checks sealed/read-only types, the five property names and absence of several
  control-like method names.
- Both committed [Windows](../evidence/restricted-consumer-contract-rc2a-02b-portability/runs/windows/windows-public.trx)
  and [WSL2](../evidence/restricted-consumer-contract-rc2a-02b-portability/runs/wsl2/wsl2-public.trx)
  public TRX files record those two public tests as passed. The public suite
  totals are 67/0/0 on each platform. The existing `PreparationIdentityTests`
  regressions also pass in the required suites.
- The RC2A-02A independent review’s AR-09 probe exercised reference validation
  and equality without finding a defect.
- The accepted API/source documentation defines the five fields, validation,
  content equality and non-temporal/passive boundary.

The [RC2A-02A traceability matrix](../evidence/restricted-consumer-contract-rc2a-02a-windows/traceability-matrix.md)
explicitly marks RC-D04 and R1-F04 deferred. Its linked 72-field
[mutation-coverage export](../evidence/restricted-consumer-contract-rc2a-02a-windows/exports/mutation-coverage-matrix.json)
contains only manifest field roots and no `RestrictedPreparationReference` or
five-field identity rows. The generic Q2A-F2 reference-row pointer therefore
does not itself supply attributable five-field coverage.

These observations support the current implementation, but none removes the
explicit **DEFERRED** rows from the accepted RC2A-02A traceability matrix or
supplies the focused RC2A-02 qualification record and disposition. The current
surface test checks a blacklist of method-name fragments (including
`Release`, `Prepare` and `Lookup`), not the full forbidden categories in RC-D04
such as adoption, deserialization-to-capability and expiry renewal. Source
inspection shows none are currently exposed; the qualified test evidence should
protect that complete boundary. Broad-suite pass counts are not a substitute
for requirement-to-assertion mapping and review of the negative public-surface
boundary.

## 5. Security and functionality assessment

This is a **security-contract assurance gate**, not a known vulnerability or a
demonstrated functionality failure. The security property is that a public
identity/digest correlation value cannot be mistaken for proof of a live,
unexpired preparation or for execution authority. The source contract assigns
those checks to a separate live RC-2B operation; the passive value alone has no
lookup or control path.

The current unqualified status does not invalidate the bounded RC2A-02A or
RC2A-02B acceptances. It does block full RC2A-02 acceptance and any RC2A-03
qualification/acceptance that relies on the passive reference. Because the
RC2A-03 work-order row specifically includes public views and the passive
reference, the residual should be resolved before that component is signed off,
not deferred to a later package or Hufu-consumer stage.

## 6. RC2A-02C acceptance criteria

The future qualification record must:

1. Bind the exact accepted source and test trees, current owner baseline, RC-1
   §RC-D04, the frozen RC2A-01 API contract, and the RC2A-02 plan rows. Commit
   the case-to-assertion matrix before any stage test execution.
2. Verify constructor behavior for each of the four empty GUID cases and digest
   lengths 0, 31 and 33 refusing; exactly 32 bytes is accepted. Verify the
   constructor copies digest bytes and stores immutable content.
3. Verify content equality and equal hash codes for separately allocated but
   equal inputs, and inequality when any one GUID or digest content changes.
4. Verify the exact approved public property/member surface, including that the
   five properties are get-only and that no time, liveness, lookup, release,
   adoption, deserialization-to-capability, expiry-renewal or other control
   member is exposed. Preserve the permitted validating public constructor and
   ordinary value-equality methods.
5. Verify the R1-F04 documentation statement and its D4a/D4b timing sources.
   Do not infer temporal validity from a GUID, digest or constructed reference.
6. Run only the focused managed-code assertions on the primary Windows
   environment unless a separately declared platform is justified. Capture the
   exact command, exit code, TRX/log, source/test identity, environment and
   artifact hashes. Existing Windows/WSL2 TRX results may be cited as supporting
   history, not relabeled as the RC2A-02C run.
7. Preserve a failure as evidence and stop if implementation behavior
   contradicts the accepted contract. Any production remediation requires its
   own tests-first authorization and review. No package, native sandbox,
   provider/deadline, RC-2B, RC2A-04 or Hufu work is included.
8. Obtain focused independent review of the committed matrix, exact public
   surface, run evidence and exclusions before owner disposition.

Existing passing tests may be reused if the precommitted matrix demonstrates
that each accepted assertion is directly covered. Add test-only assertions only
for a proven coverage gap; do not invent stronger semantics or modify
production/native code. Passing the focused suite alone does not complete the
stage until evidence binding, independent review and owner disposition are
recorded.

## 7. Baselines and stage assignment

| Role | Branch / record | Commit | Tree / parent |
|---|---|---|---|
| RC2A-02A owner acceptance | `decisions/gagamba-rc2a-02a-owner-acceptance` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | tree `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e`; parent `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B owner acceptance / RC2A-02C planning baseline | `decisions/gagamba-rc2a-02b-owner-acceptance` | `dc02951902f11031f8b0d225d2afa2cd537296a3` | tree `119470918c3b3a3c3c1af150e554026b230a2677`; parent `1cd22b9b109ddba4673b11487bceea62d2e92117` |
| RC2A-02B Q1, preserved separately | `review/gagamba-rc2a-02b-q1` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | parent `dd16baec48663d40cd29504d98e57e30dd0855df` |
| RC2A-02B Q2, preserved separately | `review/gagamba-rc2a-02b-q2` | `60de1f2d34a7df337a426501cb3954fdd3216a6b` | parent `1cd22b9b109ddba4673b11487bceea62d2e92117` |
| Original RC-1 source | `design/gagamba-rc1-restricted-consumer-contract` | `d609c29d3048bc0fd91583068cb013fb906fdf90` | design file blob `4b300b01397e7f68d978fadbc36164dda17c8721` |

The disposition worktree is
`C:\Users\Laszlos\source\repos\Solo\q2as-disposition`, branch
`decisions/gagamba-q2as-rcd04-disposition`, created clean at the exact RC2A-02B
owner tip `dc02951902f11031f8b0d225d2afa2cd537296a3`. Q1 and Q2 remain on their
separate unchanged review refs and are not ancestors of this branch. The
RC2A-02A owner branch remains at `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`.
Source, test and native subtree identities at the accepted baseline are
`b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`5c3268286575cb55891fae9288627657f2d30ddf`, and
`85631d2f04512376621d3d09f51d67fd90f45335`.

The Q2A-S / RC-D04 status is **OPEN / NOT QUALIFIED; assigned to planned
RC2A-02C; execution not started**. This assignment resolves who owns the next
stage, not whether its evidence passes. Full RC2A-02 remains **NOT ACCEPTED**
until RC2A-02C is complete and independently reviewed and the owner separately
records full-stage acceptance against the plan’s exit criteria. RC2A-03 and
later milestones remain unstarted by this decision.
