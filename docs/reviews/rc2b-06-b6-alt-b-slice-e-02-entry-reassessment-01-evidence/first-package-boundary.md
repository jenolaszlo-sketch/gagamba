# E-2 entry reassessment — proposed first package boundary

Date: 2026-11-09 (Asia/Manila)

**Status: PROPOSED ONLY — not authorized.** The E-2 entry owner decision (`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-OWNER-02`) was not performed by this commission. This file records the recommended boundary for the smallest independently qualifiable first package.

## Identity

| Item | Proposal |
|---|---|
| Package identifier | **E-2-P1 — frozen-schema contract core** |
| Purpose | Implement CJSON-1 canonical encoding and deterministic structural/binding verification for K/M/F/S as internal, non-authoritative data representations; fail closed on every inconsistency; remain production-unreachable and incapable of constructing approval, capability, or validation authority |
| Normative prerequisites | P0 owner acceptance (Q-E1, E-2 subset); D1 decision (Q-E2 governance); accepted external schema text bound by the F02 acceptance |
| Allowed inputs | Byte sequences supplied by tests or by later separately authorized callers; no filesystem, environment, network, or host probing |
| Outputs and authority limit | Immutable internal decoded objects plus validated/refused result; **non-authoritative** — no approval, eligibility, capability, permit, or positive validation; later packages must re-verify at their own boundary |
| Proposed surfaces | Internal types in `src/Gagamba.Execution.Linux.M1` under a new `Validation/` namespace, `internal` visibility, no public API; deterministic tests in the existing M1 test project; no `eng/` verification-tooling change in this package |
| RELEASE impact | None intended; must be demonstrated by reachability evidence (no production caller, `internal`, no factory, no test seam in RELEASE) |
| P4 classification | **P4-NEUTRAL (design)**, conditional on the implementation's no-caller/RELEASE-isolation evidence |
| Native dependency | None for this package; no protected behavior is claimed |

## Included verification content

1. **CJSON-1 codec.** ASCII-only UTF-8; no BOM/whitespace/trailing newline; exactly one object; exact key sets; ascending key order; string escape rules; unsigned decimal integers; booleans; `null` only where allowed; exact parse→serialize round trip; 65,536-byte limit; SHA-256 lower-case hex; duplicate/unknown/missing keys refuse.
2. **K structure.** Exact key set (`schemaVersion`=1, `caseId` S2 `ValidId`, `phase`/`topology` enums, identity fields, qualification null-pair for pre or exact pair for post, three fixture/action hashes, `maximumSessionNs` bounds, `allowRelease` boolean); **no** review/owner/receipt/self-hash field.
3. **M structure.** B2 exactly empty `objects`; B3 exactly four roles sorted by ASCII with exact mode/destination/length rules and the aggregate bound.
4. **F structure.** Exact top-level key set; B2 null/hard-false rules; B3 fixed `copy`/`invocation`/`sandbox`/`resources` constants; `PendingQualification` preserved.
5. **S structure.** Exact keys; exactly one of the three fixed templates; contiguous indexes; timeout bounds; no extension fields.
6. **Cross-artifact bindings.** `F.fixtureManifestSha256 == Hm`; `S.fixtureManifestSha256 == Hm`; `S.fixturePolicySha256 == Hf`; `K.fixtureManifestSha256 == Hm`; `K.fixturePolicySha256 == Hf`; `K.actionScriptSha256 == Hs`; duplicated case/phase/topology/host/qualification/release/session values equal across K/F/S; release presence equals both `allowRelease` values.
7. **Deterministic refusal.** Wrong version, wrong schema, unknown/duplicate fields, malformed bytes, hash mismatch, broken cross-artifact relationships, and non-canonical reserialization refuse with no fallback.

## Explicit exclusions (must not appear in E-2-P1)

- Protected-root access or any `/opt/gagamba` or `/etc/gagamba` interaction.
- R/Wc/P detached-evidence verification, approval state, or publication/withdrawal logic.
- V/W/A actors; any approval or reviewer verification.
- Live challenge/response, capability construction, launcher, custody, privilege drop.
- GAVA1 / v3-pre / release correlation.
- E-1 production consumer or `OwnerAuthorityContext.Validation`.
- Any public API, authority factory, or positive-validation path.
- Any `src`, `tests`, or `eng` change outside the authorized surfaces.

## Selection-rule check (reassessment §9)

| # | Criterion | Result |
|---|---|---|
| 1 | Accepted input contracts complete | Yes — P0 record §§6–10; external text bound by F02 acceptance |
| 2 | Outputs clearly bounded | Yes — non-authoritative internal results |
| 3 | Implementation surfaces identifiable | Yes — internal namespace + existing test project |
| 4 | Independently testable | Yes — deterministic codec and binding checks |
| 5 | Failure behavior specifiable without inventing policy | Yes — refusal invariants; encodings designed with the package |
| 6 | RELEASE exposure controlled | Yes — internal, no caller; must be demonstrated |
| 7 | P4 implications known | Yes — P4-NEUTRAL design; no sink/authority/lifecycle change |
| 8 | No dependency on unimplemented roots/custody | Yes — none touched |
| 9 | Cannot create positive validation authority | Yes — no approval/eligibility/capability path |
| 10 | Later packages consume only via separately authorized contracts | Yes — stated output limitation |

## Qualification sketch

- Deterministic portable tests for each structural and binding invariant.
- Independently designed negative controls for every refusal invariant (wrong version, unknown/duplicate fields, malformed encodings, broken `Hm/Hf/Hs` bindings, K/F/S duplication disagreements, B2/B3 variant violations).
- Reviewer-authored mutations (not only producer tests).
- RELEASE reachability evidence: no production caller, no public API, no authority factory, no RELEASE test seam.
- No filesystem or native tests in this package; if any appears, the package is out of scope and requires a P4 re-audit.
