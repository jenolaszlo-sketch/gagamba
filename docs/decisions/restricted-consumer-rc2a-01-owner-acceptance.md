# RC2A-B: Restricted consumer contract RC2A-01 owner acceptance

Date: 2026-10-25 (Asia/Manila).
Decision: **RC2A-01 ACCEPTED WITH FOLLOW-UP**.
Scope: implementation acceptance and decision/follow-up preservation only.
RC2A-02 execution, package qualification, RC-2B, native qualification, Hufu
resumption and publication are **NOT AUTHORIZED BY THIS RECORD**.

## 1. Accepted implementation and exact source binding

The project owner explicitly accepts the RC2A-01 passive restricted consumer
contract implementation and its evidence at the exact identities below. This
accepts the implementation only; **it does not complete qualification**
(RC2A-02 remains a separately commissioned stage) and does not change the
accepted RC-1 owner decision.

| Item | Exact identity |
|---|---|
| Accepted branch | `implementation/gagamba-rc2a-01` |
| Accepted HEAD (final evidence commit) | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` |
| Accepted root tree | `54f659d01937da29bc9a5f82566a8349af264664` |
| Clarified implementation commit | `781e9debb8df852d1d4d8ca98d92591e1609e193` |
| Original implementation commit | `e93d642e1a36af25c0e145d328bebadd1bba4838` |
| Source tree at accepted commit | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| Tests tree at accepted commit | `ceaa7aca8121a1acef5300e074a1f6bc73c0a781` |
| Unchanged native tree | `85631d2f04512376621d3d09f51d67fd90f45335` |
| Frozen API contract blob at accepted commit | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` |
| Governing design (RC-1) | `d609c29d3048bc0fd91583068cb013fb906fdf90` |
| RC-1 independent review | `feb9d7584c41712195d4b90754ed7a342ba74bb7` |
| RC1-A owner acceptance | `9a705335701f0c1923bd9867a2612b8a93afa8cb` |
| Required direct parent of this decision commit | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` |

The canonical repository state was re-verified before recording: the branch
resolves at the stated HEAD, whose root tree equals the stated tree, in the
canonical Gagamba repository; the accepted worktree was clean before and after.
No accepted commit is rewritten, amended, rebased or merged.

## 2. Independent review preservation (Commit A)

The independent GLM implementation review is preserved **verbatim and
unmodified** as a separate immutable artifact on its own branch; it is
referenced here by exact identity and is not merged, cherry-picked, copied into
this branch or attributed to this decision author.

| Item | Exact identity |
|---|---|
| Review branch | `review/gagamba-rc2a-01-glm-independent` |
| Review commit | `8e41babfc96afa2e617a53b63e50903d54001194` |
| Review root tree | `fd30ef218b2ff5d1422d2e3abddd59c50e11aead` |
| Review document path | `docs/reviews/restricted-consumer-rc2a-01-glm-independent.md` |
| Review document Git blob | `d329fcc49d289696bf1c4163f8f346c56115ba8c` |
| Review document byte SHA-256 (exact blob bytes) | `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` |
| Review document byte length | 16,888 bytes |
| Review commit parent | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` (the accepted commit) |
| Provenance disclosure | The review text was produced by GLM in the review conversation and supplied by the owner as a local file after the review session; the custodian preserved the exact supplied bytes (source and preserved SHA-256 verified identical) and added a `-text` `.gitattributes` entry for this one path, following the existing repository convention, so the blob preserves CRLF bytes without line-ending normalization. The review document itself contains no owner commentary. |

Read the original document with:

```text
git show 8e41babfc96afa2e617a53b63e50903d54001194:docs/reviews/restricted-consumer-rc2a-01-glm-independent.md
```

The review's verified content: independent review heading and Git binding
(section A), scope inspected (B), contract compliance matrix C1–C15 (C),
findings R2A-F01–R2A-F04 plus coverage observations (D), independent
reproduction of the 95 passing tests, build, golden digest, probes and hash
chain (E), deferred obligation reconciliation (F), and final recommendation
**ACCEPT WITH FOLLOW-UP** (G). The review personally reproduced, at
`68b01fb` on Windows 11: Release build 0 warnings/0 errors; Execution assembly
57/0/0; M1 deterministic filter 38/0/0 (95 total, matching producer claims);
golden 873-byte digest `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`;
adversarial probes with no counterexample; blob and artifact-hash chains
verified. No confirmed contract violation and no blocking finding was reported.

## 3. Owner authority and decision provenance

The owner supplied the assignment **Gagamba RC2A-01: Preserve Independent
Review, Record Owner Acceptance and Commit RC2A-02 Plan** in this conversation.
Its express owner decision is:

> **RC2A-01 ACCEPTED WITH FOLLOW-UP.** This acceptance is limited to the
> RC2A-01 implementation and its independently reviewed evidence. It does not
> authorize package qualification, RC2A-02 execution, RC-2B implementation or
> Hufu resumption.

The same instruction supplied the four finding dispositions recorded in
section 4 and authorized committing the review preservation (Commit A), this
decision (Commit B) and the RC2A-02 preparation. It expressly did not authorize
starting RC2A-02. The readable owner instruction and its byte binding are in
[the decision evidence](../evidence/restricted-consumer-rc2a-01-owner-acceptance/README.md).
This is a conversation-based owner decision, not a digital signature, external
approval ticket or independent authentication of a human identity. The review is
independent implementation evidence; the owner's instruction is the acceptance
authority. Neither is runtime qualification.

## 4. Finding dispositions (R2A-F01–R2A-F04)

These are **owner dispositions of the independent review findings**. The
authoritative finding wording and classification remain in the exact review
commit above; no finding is rewritten or closed beyond its stated disposition.

| Finding | Review substance | Owner disposition | Required follow-through |
|---|---|---|---|
| R2A-F01 | The defensive decode-path re-encode equality branch (`ManifestReader.cs:24-25`) has no direct triggering test | **ACCEPT, FOLLOW-UP** | Carry its reachability assessment into RC2A-02 ([qualification spec §3, B5](restricted-consumer-contract-rc2a-02-qualification.md)). Do not require an artificial test if the branch is demonstrably unreachable under the accepted input model |
| R2A-F02 | The clarified API document is missing from the updated post-clarification document-digest inventory | **ACCEPT, FOLLOW-UP** | Rebind `docs/design/restricted-consumer-contract-rc2a-01-api.md` in the RC2A-02 evidence set (qualification spec §3, E1). Until then it remains bound by its Git blob `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` at the accepted commit |
| R2A-F03 | Locally rebuilt DLL bytes were not byte-identical despite matching test outcomes | **ACCEPT AS DISCLOSED** | No binary reproducibility or deployment qualification claim is accepted; the producer's own non-qualification scoping of local DLL hashes stands. No immediate remediation is required. Test-reproducibility versus binary-reproducibility is kept distinct in RC2A-02 evidence rules (§3, E4) |
| R2A-F04 | Combined null-manifest plus wrong-length-digest behavior is deterministic and documented but lacks an explicit assertion | **ACCEPT, FOLLOW-UP** | Include the explicit combined case in RC2A-02 (qualification spec §3, B4). Also carry the review's empty argv-item and zero-length argv-list coverage observations (§3, B3) |

Carried into RC2A-02 by these dispositions: the re-encode branch assessment,
the API-document rebinding, the test-versus-binary reproducibility distinction,
the combined null+wrong-length assertion, and the argv coverage observations.
No disposition reopens RC-1, changes the accepted public contract, or closes
any R1 finding.

## 5. Acceptance basis and its limits

The acceptance rests on: the accepted RC-1 owner baseline; the RC2A-01
implementation and owner clarification at `781e9de`; the independent review with
its successful 95-test reproduction and no confirmed contract violation; and no
blocking review finding. It does **not** rest on, and does not establish:
RC2A-02 qualification; exhaustive per-field mutation coverage; exported
consumer vectors; Linux/macOS runs; package, clean-consumer or mixed-graph
qualification; any native, sandbox, production or release capability; or Hufu
resumption. R2A-F03 is accepted as disclosed: local DLL byte identity is not a
qualification claim in either direction.

## 6. Stage readiness after this decision

| Milestone | Status |
|---|---|
| RC-1 design and RC1-A | **ACCEPTED (unchanged)** |
| RC2A-01 implementation and evidence | **ACCEPTED WITH FOLLOW-UP** at the exact commit/tree above |
| RC2A-R independent implementation review | **COMPLETE — ACCEPT WITH FOLLOW-UP** at `8e41babfc96afa2e617a53b63e50903d54001194` |
| R2A-F01/F02/F04 follow-ups | **CARRIED INTO RC2A-02** (spec updated by this commit) |
| R2A-F03 | **ACCEPTED AS DISCLOSED**; no remediation commissioned |
| RC2A-02 tests-first qualification | **PLANNED, NOT STARTED** — [specification](restricted-consumer-contract-rc2a-02-qualification.md) committed; execution requires separate owner authorization |
| RC2A-03/RC2A-04 (component, package/clean consumer/mixed graph) | **PENDING**, not started |
| RC-2B (R1-F02/F03) | **OPEN, DEFERRED — NOT AUTHORIZED** |
| HUFU-R / AE4-B01 | **OPEN — Hufu resumption NOT AUTHORIZED** |
| Production restricted execution; publication | **NOT QUALIFIED / NOT AUTHORIZED** |

## 7. Explicit exclusions

This record does not: reopen RC-1; change the accepted RC2A-01 public contract
or any accepted commit; implement RC-2B obligations R1-F02/F03; close AE4-B01;
resume Hufu; authorize package builds, publication, native sandbox
qualification, or RC2A-02 execution; or treat RC2A-02 completion (when it
happens) as automatic package qualification. A production defect discovered
during future qualification must be reported with a separate bounded
remediation proposal, never silently absorbed into qualification scope.

## 8. Security non-claims and completion evidence

Owner acceptance establishes an implementation-acceptance decision at an exact
Git identity. It does not establish runtime confinement, native controller-loss
behavior, output disclosure, cleanup, deployment, package compatibility or
production qualification. A digest, decoded view or passive reference cannot
authorize, renew, reconstruct or adopt an execution; Gagamba remains independent
of Hufu policy and persistence.

This Commit B contains documentation only: the decision record, the RC2A-02
qualification specification, plan/queue/handoff status updates and this
evidence directory. No production source, test source, project file or package
metadata changed; src/tests/native trees remain as bound in section 1; no
build, test, pack, push, merge, tag or publication occurred in this assignment
(the 95-test reproduction cited in section 2 is the independent review's
recorded evidence, not a rerun by this custodian). Any validation newly
performed for this record is identified in
[validation.json](../evidence/restricted-consumer-rc2a-01-owner-acceptance/validation.json).
The final decision commit/root tree are recorded in the external completion
receipt and user report, avoiding a self-referential commit hash. Verify them
with `git rev-parse HEAD HEAD^{tree} HEAD^` in this worktree.

**Stop after this decision commit. RC2A-02 execution requires separate owner
authorization.**
