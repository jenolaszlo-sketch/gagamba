# RC1-A: Restricted consumer contract owner acceptance

Date: 2026-10-25 (Asia/Manila).
Decision: **OWNER APPROVED — RC-1 ACCEPTED WITH QUALIFICATIONS**.
Scope: architectural acceptance and decision preservation only.
Implementation: **NOT AUTHORIZED BY THIS RECORD**.

## 1. Accepted contract and exact source binding

The project owner explicitly accepts the Gagamba RC-1 restricted consumer
contract at `d609c29d3048bc0fd91583068cb013fb906fdf90`, with the qualifications
in GLM's independent review at `feb9d7584c41712195d4b90754ed7a342ba74bb7`.
GLM's exact disposition is **RC-1 ACCEPTED WITH QUALIFICATIONS**.

| Item | Exact identity |
|---|---|
| Accepted RC-1 design commit | `d609c29d3048bc0fd91583068cb013fb906fdf90` |
| Design root tree | `d09ee7d079e44ecb645e7173bc99e11979ffee39` |
| Independent GLM review commit | `feb9d7584c41712195d4b90754ed7a342ba74bb7` |
| Review root tree | `639e5345c8e47abc625fe6228d147cf7ad0bc094` |
| Review document Git blob | `c2617370806f7ba0ac7bb4006fc92d3a880df2dc` |
| Decision branch | `decisions/gagamba-rc1-owner-acceptance` |
| Decision worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc1-owner-acceptance` |
| Required direct parent of this decision commit | `d609c29d3048bc0fd91583068cb013fb906fdf90` |
| Unchanged src tree | `9dd0f055eaf48d48bda8683dee3f8dc534a06e5d` |
| Unchanged tests tree | `ebcb976a164b212090fbe0958fe4b0c0b243f794` |
| Unchanged native tree | `85631d2f04512376621d3d09f51d67fd90f45335` |

The review commit is a separate direct child of the design commit. It remains
on `review/gagamba-rc1-glm-independent-contract`; it is not merged, cherry-picked,
copied or attributed to this decision author. Read its original document with:

```text
git show feb9d7584c41712195d4b90754ed7a342ba74bb7:docs/reviews/restricted-consumer-contract-rc1-glm-review.md
```

The verified local review checkout is
[gagamba-rc1-glm-review](C:/Users/Laszlos/source/repos/Solo/gagamba-rc1-glm-review/docs/reviews/restricted-consumer-contract-rc1-glm-review.md).
Use the exact commit above if that checkout later moves. The accepted
[RC-1 design](../design/restricted-consumer-contract-rc1.md) remains unchanged;
this record supplies the subsequent owner decision and mandatory clarification.
The [implementation plan](../plans/restricted-consumer-contract-implementation.md)
is updated prospectively. Prior evidence and pending-status statements remain
historical records of their original dates.

## 2. Owner authority and decision provenance

The user supplied the assignment titled **Gagamba RC1-A: Owner Acceptance and
Decision Record** in this conversation. Its express owner decision is:

> I accept the Gagamba RC-1 restricted consumer contract design with the qualifications identified by GLM.

The same instruction explicitly approves the public contract, Mode=`work`, the
65,536-byte encode/decode limit, and the seven finding dispositions below. It
authorizes recording and committing this decision, not implementing RC-2A.
No further approval was inferred or requested.

The [readable owner instruction](../evidence/restricted-consumer-rc1-owner-acceptance/owner-instruction.md)
and [source/provenance binding](../evidence/restricted-consumer-rc1-owner-acceptance/source-binding.json)
preserve the original attachment location, byte hash, normalized-copy rules and
exact design/review identities. This is a conversation-based owner decision,
not a digital signature, external approval ticket or independent authentication
of a human identity. GLM's review is independent design evidence; the user's
instruction is the owner approval authority. Neither is runtime qualification.

## 3. Approved public contract

Accept immutable restricted-execution manifest views, passive preparation
references, identity verification and a strict decoder in the existing net10.0
`Gagamba.Execution` package, as defined in the accepted RC-1 design.

- Preserve `RestrictedManifest` field meanings, deep immutability and exact
  canonical binary encoding. Keep `ManifestCodec` the sole authoritative
  production encoder; add no competing manifest format or codec.
- Reject invalid, malformed, noncanonical and unsupported representations.
  Preserve strict version/implementation, tag, ordering, duplicate, UTF-8 and
  length validation and the accepted round-trip invariants.
- Public RC-2A values and codec expose passive inspection, not execution
  authority. No native handle, live preparation/release capability, release
  frame, native control, provider registration or runtime-facade operation is
  part of RC-2A. No consumer reflection, friend access or copied encoder.
- A manifest, SHA-256 digest, GUID or passive preparation reference cannot
  authorize, renew, reconstruct or adopt an execution. Gagamba remains
  independent of Hufu policy and persistence.
- Ordinary lifecycle providers cannot satisfy a confinement-required profile.
  Preserve refusal before target initialization and no lifecycle fallback.

The reference's five fields remain four GUIDs and a manifest digest. It has
**no temporal fields** and establishes no expiry, deadline or current liveness.
Carry R1-F04 in consumer-facing package documentation: D4a timing comes from
explicit enrolled/synthetic binding data, and later D4b timing only from the
actual provider-origin prepared execution. Neither source is invented by
decoding or constructing a passive reference.

## 4. Explicit first-consumer mode approval

**Mode = `work` is approved** for the first Hufu AE-4 consumer contract.
Preserve the source-defined mapping reviewed by GLM: the fixed native fixture's
default invocation uses argv exactly `[/tool]`; the descriptive mode adds no
argument. Its default branch prints fixed useful-work text, its constructor
prints the constructor marker, and it does not read the staged input.

The existing `--fail` and `--ctor-marker` branches are not additional approved
consumer modes. This decision does not authorize arbitrary commands, general
tool selection, native execution or expansion of the closed first profile.

## 5. Mandatory canonical encoding limit (R1-F01)

**65,536 bytes is the maximum complete canonical encoding on both the public
decode and public encode boundaries.** This is an inclusive ceiling, not a
truncation target. The stricter owner/profile configuration limits remain distinct.

Decode refuses input above the ceiling as already specified. Encode must also
refuse any manifest/view whose complete canonical encoding would exceed 65,536
bytes. It must return no truncated buffer, partial success or alternate grammar.
Existing canonical grammar, valid in-bound bytes, validation and refusal
semantics are preserved. The common encoder remains authoritative.

This instruction settles the encode-side requirement. RC2A-01 must still name
the exact public encoding failure mechanism/type in its frozen API contract and
bind it to tests **before RC2A-02 begins**. Choosing that concrete mechanism is
contract finalization under a separate commission, not permission to start code
here or to reinterpret the approved limit. Accepted encoding must remain within
the domain its decoder accepts.

Required boundary evidence in RC-2A: a structurally valid exact-65,536-byte
encoding succeeds and round-trips; a structurally valid 65,537-byte encoding is
refused on both relevant paths. Exercise the public encode boundary through a
Gagamba-internal deterministic view/projection seam if necessary; do not add a
public oversized-view factory or native capability to manufacture the test.
If view construction rejects an oversized value first, prove that path as well
as the shared encode guard, with no public route around the invariant.

## 6. GLM finding reconciliation

These are **owner dispositions of the original findings**, not rewritten GLM
findings or new independent verification. The authoritative wording and
classification remain in section G of the exact review commit. No deferred
finding is closed by this acceptance record.

| Finding | Owner disposition | Required follow-through and current state |
|---|---|---|
| R1-F01 | **Accepted correction** | Encode-side refusal above 65,536 bytes is mandatory, now recorded in section 5 and RC2A-01. Concrete failure contract, implementation and tests remain pending; no implementation closure claimed |
| R1-F02 | **Deferred to RC-2B — OPEN** | B1/B3 must specify the observable pre-mint queued-stop state and its interaction with minting; carry RC-T18. Do not implement that latch or facade in RC-2A |
| R1-F03 | **Deferred to RC-2B — OPEN** | B1 must choose whether pre-retention correlation mismatch is retained/consuming and whether a later matching call may proceed; carry RC-T23. GLM's recommendation is preserved in its report, not silently selected by this record |
| R1-F04 | **Accepted qualification** | Passive reference has no temporal fields. Preserve the consumer timing-source limitation in package docs and Hufu delta review; no RC-2A authority or timing field added |
| R1-F05 | **Accepted test additions** | Carry wrong-length SHA-256 inputs, `Supports` boundary arguments and oversized encode-side input into the required RC-2A subset. Tests are specified, not implemented or passed |
| R1-F06 | **Preserved evidence-location observation; not an RC-2A blocker** | Hufu AE-4 evidence is in the dedicated Solo worktree/branch at `37dcf33362e8e745800a530a478fd406795c3e58`, not canonical Hufu main. Recheck that binding at HUFU-R |
| R1-F07 | **Verified-accurate disclosures; no corrective action** | Preserve GLM's source verification and its scope. Existing C1 review/C3/C4/runtime/package qualifications are not closed or re-certified by RC1-A |

For R1-F05, explicitly include 31- and 33-byte expected digests (and empty input),
a matching valid 32-byte digest and a nonmatching valid 32-byte digest. An
all-zero 32-byte digest is a present value, not an absence or wildcard; verification
against the known nonzero golden digest must not succeed. This does not add the
later RC-2B approval-correlation API to RC-2A. `Supports` tests cover the exact
supported schema/implementation positive control, unsupported schema values,
unknown/empty/whitespace/case-variant implementation IDs, and null under the
accepted programming-error convention. RC2A-01 freezes the exact failure behavior
for assertions; tests must not choose it accidentally during implementation.

## 7. Stage readiness and smallest next commission

| Milestone | Status after this decision is committed |
|---|---|
| RC-1 design | **ACCEPTED WITH GLM QUALIFICATIONS** at the exact design/review commits above |
| RC1-A owner acceptance | **APPROVED AND RECORDED** by this instruction and decision commit |
| RC2A-01 contract finalization | **ELIGIBLE FOR SEPARATE IMPLEMENTATION COMMISSION; NOT STARTED** |
| RC2A-02 tests-first implementation | **ELIGIBLE AFTER RC2A-01 CONDITIONS; NOT STARTED** and requires that separate commission |
| RC-2B runtime facade | **NOT AUTHORIZED**; R1-F02/R1-F03 and prerequisite gates remain open |
| Hufu AE4-D4a | **HELD** pending independently accepted, exact-version RC-2A and consumer dependency/delta review |
| Hufu AE4-D4b | **HELD** pending RC-2B and its other prerequisites |
| Production restricted execution | **NOT QUALIFIED** |
| Public package publication | **NOT AUTHORIZED** |

The smallest next assignment is RC2A-01 plus tests-first RC2A-02 under this
accepted contract. It must revalidate the exact parent/schema/source, freeze the
passive API and named encode/digest/Supports refusal behavior, include R1-F01 and
R1-F05 in the required test subset, retain R1-F04 in package docs, and preserve
all RC-2B/native exclusions. If the source or contract changed, review that delta
before proceeding. RC2A-02 starts only after those conditions are satisfied and
the separate assignment authorizes it. Later RC-2A code/package work follows its
explicitly commissioned scope and independent GLM implementation review.

The existing [RC-2A work order](../plans/restricted-consumer-contract-implementation.md)
continues to require canonical vector stability, exhaustive field mapping,
strict bounded parsing, negative misuse tests, immutability, a unique local
package, clean-consumer qualification and evidence of the Hufu dependency graph.
These are future acceptance criteria; no build, test, package or implementation
result is established by this record.

## 8. Deferred RC-2B responsibilities and existing decisions

Preparation, release, stop, capture, observation, protected output custody,
recovery and their process/deployment interfaces remain RC-2B. Its future
commission must resolve R1-F02/R1-F03 explicitly, preserve live capability
association, independently surviving recovery/lifeline behavior, original
deadlines, no replay and independent result/cleanup uncertainty.

Do not reopen or alter approved C1–C4 or **C3 Policy A**. D6.7
`01063c164391e0676ae1f6087d2c0865f7692511` remains the decision authority for
those contracts. Relevant accepted implementations, real owner/resource/capture/
recovery support, applicable G3/G4/G5 and exact consumer/native qualification are
separate gates. This record does not fresh-qualify or advance those workstreams.

## 9. Hufu AE4-B01 remains OPEN

The inspected consumer evidence is at
`C:\Users\Laszlos\source\repos\Solo\hufu-ae4-implementation`, branch
`implementation/ae4-integration-plan`, commit
`37dcf33362e8e745800a530a478fd406795c3e58`. Its AE-4 design, stage register,
amendments and entry report are the relevant evidence; canonical main's absence
of those files is not a missing-source blocker. This is R1-F06's recorded
observation, not a request to move, merge or modify Hufu.

Sol may reconsider the contract portion of AE4-B01 only after RC-2A has been
implemented and independently accepted at an exact source/API/package version,
with package/artifact hashes, canonical vectors, public-value/codec tests,
clean-consumer evidence and resolved required findings. Sol must review Hufu's
existing Runtime dependency graph and semantic deltas, including Mode=`work`,
passive-reference timing limits and qualification versus establishment. Any
remaining Hufu design/owner gates and explicit D4a resumption authorization still
apply. The owner acceptance alone is not a resume instruction.

D4a remains a deterministic no-launch slice. D4b requires independently accepted
RC-2B and its relevant C-contract, owner, capture, recovery, deployment and native
gates plus separate consumer authorization. No Hufu file is changed in RC1-A.

## 10. Security non-claims and completion evidence

Owner acceptance establishes an architectural decision, not successful
implementation, public API availability, package compatibility, OS confinement,
native controller-loss behavior, output disclosure, cleanup or production
qualification. Mandatory pending restrictions stay pending. Unknown execution,
stop, capture and cleanup remain separate facts; a digest or empty domain cannot
erase uncertainty or confer authority.

This commit contains documentation only. The original RC-1 design and GLM review
are preserved unchanged. No source, tests, project references or package metadata
are changed; no package build, native test, push, merge, tag or publication occurs.
Checks and hashes are in the
[decision evidence](../evidence/restricted-consumer-rc1-owner-acceptance/README.md).
The final decision commit/root tree are recorded in the external completion
receipt and user report, avoiding a self-referential commit hash. Verify them
with `git rev-parse HEAD HEAD^{tree} HEAD^` in this worktree.

**Stop after the owner-decision commit. Do not begin RC-2A in this task.**
