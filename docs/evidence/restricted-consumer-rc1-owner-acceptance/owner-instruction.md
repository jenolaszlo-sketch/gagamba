# Gagamba RC1-A: Owner Acceptance and Decision Record

**Assigned model:** Astra
**Task:** Record project-owner acceptance of the RC-1 restricted consumer contract
**Scope:** Documentation and decision preservation only
**Implementation:** Not authorized in this task

## 1. Owner decision

I accept the Gagamba RC-1 restricted consumer contract design with the qualifications identified by GLM.

Design commit:

`d609c29d3048bc0fd91583068cb013fb906fdf90`

Independent GLM review:

`feb9d7584c41712195d4b90754ed7a342ba74bb7`

Review disposition:

**RC-1 ACCEPTED WITH QUALIFICATIONS**

This is my explicit owner approval of the contract direction described below.

Record this as RC1-A without reopening already settled C1–C4 decisions.

## 2. Accepted architectural decisions

### Public contract

Approve immutable restricted-execution manifest views and a strict decoder in the existing `Gagamba.Execution` package.

Requirements:

- Preserve the existing `RestrictedManifest` semantics.
- Keep `ManifestCodec` as the sole authoritative production encoder.
- Do not introduce another manifest format or competing codec.
- Preserve exact canonical binary encoding.
- Maintain deep immutability.
- Reject invalid, malformed, noncanonical or unsupported representations.
- Expose passive inspection and identity verification, not execution authority.

RC-2A must not expose native handles, live release capabilities or restricted-process control operations.

### First consumer mode

Explicitly approve:

`Mode = work`

This is the selected mode for the first Hufu AE-4 consumer contract.

Do not infer support for additional modes or generalize this selection into an unrestricted execution interface.

Preserve the mode's existing source-defined semantics.

### Canonical encoding limit

Explicitly approve a maximum canonical encoded manifest size of:

**65,536 bytes**

This limit applies to both decoding and encoding.

For encoding:

- Reject a manifest whose complete canonical encoding would exceed 65,536 bytes.
- Do not return a truncated or partially successful encoded manifest.
- Do not alter the existing canonical grammar to accommodate oversized inputs.
- Preserve existing validation and refusal semantics.

Incorporate this clarification into RC2A-01 and its implementation acceptance tests.

### Restricted execution separation

Preserve the distinction between:

- RC-2A: Passive public values and canonical codec.
- RC-2B: Preparation, release, stop, capture and recovery facade.

RC-2A must not claim that Gagamba's restricted provider is available, integrated or production-qualified.

An immutable manifest, digest, GUID or preparation reference does not grant execution authority.

An ordinary lifecycle provider cannot satisfy a confinement-required profile.

Gagamba remains independent of Hufu.

## 3. Accept GLM findings and qualifications

Preserve all seven findings with their original identifiers.

| Finding | Owner disposition |
|---|---|
| R1-F01 | **Accepted correction:** 65,536-byte encode-side refusal is mandatory in RC-2A |
| R1-F02 | Deferred to RC-2B: pre-mint queued-stop semantics |
| R1-F03 | Deferred to RC-2B: pre-retention correlation mismatch and retry ambiguity |
| R1-F04 | Accepted qualification: passive reference carries no temporal fields |
| R1-F05 | **Accepted test additions:** wrong-length SHA-256 digest, `Supports` boundary conditions and oversized encode-side input |
| R1-F06 | Preserve the Hufu evidence-location observation; do not treat it as an RC-2A blocker |
| R1-F07 | Verified-accurate disclosures; no corrective action |

Do not silently close deferred findings.

Do not implement RC-2B behavior inside RC-2A merely to address a later-stage concern.

## 4. Implementation readiness

Record these distinct statuses:

| Milestone | Owner decision |
|---|---|
| RC-1 design | Accepted with GLM qualifications |
| RC1-A owner acceptance | Approved by this instruction; record pending commit |
| RC2A-01 contract finalization | Eligible for separate implementation commission |
| RC2A-02 tests-first implementation | Eligible after required RC2A-01 conditions are satisfied |
| RC-2B runtime facade | Not authorized; prerequisite gates remain open |
| Hufu AE4-D4a | Held pending accepted, version-pinned RC-2A and consumer dependency review |
| Hufu AE4-D4b | Held pending RC-2B and its other prerequisites |
| Production restricted execution | Not qualified |
| Public package publication | Not authorized |

Owner acceptance is an architectural decision. It does not establish successful implementation or security qualification.

## 5. Record the decision in Gagamba

Inspect the current repository conventions and applicable `AGENTS.md`.

Create a dedicated documentation-only branch based on the accepted RC-1 design, preserving the independent GLM review commit separately.

Suggested branch:

`decisions/gagamba-rc1-owner-acceptance`

Create an owner decision record under the existing `docs/decisions/` convention.

Suggested document:

`docs/decisions/restricted-consumer-rc1-owner-acceptance.md`

Include:

1. Exact RC-1 design commit.
2. Exact GLM review commit.
3. Independent review disposition.
4. Explicit owner acceptance.
5. Approved public contract and Mode=`work`.
6. Accepted 65,536-byte encoding rule.
7. R1-F01–R1-F07 dispositions.
8. RC-2A acceptance criteria and implementation prerequisites.
9. RC-2B deferred responsibilities.
10. Hufu AE4-B01 resumption conditions.
11. Security non-claims.
12. Source binding and decision provenance.

Update the RC-1 implementation plan/progress tracker to reference the owner decision.

Update the relevant roadmap or handoff only as needed.

Do not copy the independent review into the design branch as though Astra authored it. Reference the exact review commit.

Do not rewrite the original GLM findings.

## 6. Validation

Verify:

- All seven GLM findings have recorded dispositions.
- The public contract remains passive.
- Mode=`work` is explicit.
- The 65,536-byte limit covers encoding and decoding.
- R1-F05 additions are carried into RC-2A.
- RC-2A and RC-2B remain separate.
- Hufu AE4-B01 remains open.
- No unsupported security claim was introduced.
- The changes are documentation only.

Run documentation link checks and `git diff --check`.

Commit the decision record and progress-tracker changes.

Do not modify production code, tests, package metadata or project references.

Do not build packages, run native tests, push, merge or publish.

## 7. Final report

Provide:

**A. Git binding:** Branch, parent, decision commit and tree.

**B. Owner decision:** Exact accepted contract.

**C. GLM reconciliation:** R1-F01–R1-F07 dispositions.

**D. Contract requirements:** Mode, canonical encoding, public values, API restrictions.

**E. Stage readiness:** RC2A-01, RC2A-02 and RC-2B.

**F. Hufu status:** AE4-B01 remains open, with exact conditions for reconsideration.

**G. Validation:** Executed checks and documentation-only diff.

**H. Next action:** Smallest eligible RC-2A implementation commission.

**I. Outstanding limitations:** Security and qualification gates that remain open.

### Completion rule

Commit the owner acceptance and progress-tracker update, leave the worktree clean, and stop.

**Do not begin RC-2A implementation in this task.**

The next assignment will authorize RC2A-01 and tests-first RC2A-02 under the accepted contract, followed by independent GLM implementation review.
