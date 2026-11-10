# ALT-B Slice E-2 P2 entry owner decision — detached R/Wc/P structural verification

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-OWNER-01`

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push. This is a single-reviewer governance authorization based on the committed entry assessment and accepted decisions; it is **not** a producer implementation or an independent qualification commission.

Evaluated assessment: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-ASSESSMENT-01`, disposition `ENTRY READY WITH QUALIFICATIONS`, recommending **E-2-P2: detached R/Wc/P structural verification**. The formal owner disposition is stated exactly once in §25. This decision authorizes a future bounded P2 implementation; it does not implement anything and does not authorize independent qualification.

---

## 1. Verified repository baseline

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `051c6824de86b37e8c582721c43f8b622f3e4402` |
| Starting tree | `38828bea8919ab5ce3c193c0010359370436491d` |
| Worktrees | exactly one canonical checkout |
| Tracked / index state | clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 25 commits ahead; no push |
| Network / package / cloud | none used |

Ancestry verified (`git merge-base --is-ancestor`, all exit 0): P2 assessment `051c682`; P1 acceptance `28275ba`; Q1 `ab5f7a7`; P1 `06006a7`; entry-owner-02 `2dab45a`; reassessment `ad52aae`; D1 `bdc7fa2`; P0 acceptance `5b2ef14`; E-2 entry decision `4263c8b`; entry assessment `a35ce10`; P4 `2d39380`; E-1 `8bd7142`; F-01 `1ed1fc7`; F-02 `e86254e`; F-03 `6d08ae5`. No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was used.

## 2. Binding evidence inventory

| Class | Evidence | Identity |
|---|---|---|
| P2 assessment | [assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01.md) | blob `745dbc8c…` |
| P2 evidence (6 files) | `…-p2-entry-assessment-01-evidence/`: README `3f180612…`; baseline `e215d89b…`; P1-reuse/controls `06d0206b…`; readiness/downstream `2493070e…`; R/Wc/P schemas `6403cad6…`; structural-vs-authority `f8d7b4c3…` | committed |
| P1 owner acceptance | [P1 owner acceptance](rc2b-06-b6-alt-b-slice-e-02-p1-owner-acceptance-01.md) | blob `f360932a…` |
| E-2 entry authorization | [entry owner decision 02](rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md) | blob `f5ab96c6…` |
| D1 approval governance | [D1 decision](rc2b-06-b6-alt-b-slice-e-02-d1.md) | blob `83fe8d26…` |
| P0 adopted provenance | [frozen-schema provenance record](../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md) | blob `db25949d…` |
| P1 implementation | `src/Gagamba.Execution.Linux.M1/Validation/` four files | verifier blob `fdff96b9…` |
| P1-Q1 | [Q1 report](../reviews/rc2b-06-b6-alt-b-slice-e-02-p1-q1.md) | blob `7ee0b781…` |
| Supporting acceptances | P0 owner acceptance `5b2ef14`; E-2 entry assessment; E-1 `8bd7142`; F-01 `1ed1fc7`; F-02 `e86254e`; F-03 `6d08ae5`; P4 `2d39380`; Alternative B `d804d90`; F02 acceptance `4aab877`; trust store `142cc23` | committed |

External schema authority: `case-policy-and-action-contract.md` `667e6118…` and `mechanism-b-session-capability.md` `f5d28dbe…` as extracted in the adopted P0 record §11.

## 3. P2 assessment acceptance and reconciliation

The owner accepts the assessment as the entry basis, having verified its milestone state, schema reconstruction, identity matrix, category separation, and readiness conclusions against the committed sources. The assessment's fifteen recommended owner-decision points reconcile as follows:

| # | Assessment recommendation | Owner resolution |
|---|---|---|
| 1 | Accept the assessment | Accepted (§3) |
| 2 | Frozen R/Wc/P contracts complete | Confirmed complete for content (§§6–8) |
| 3 | P2 may reuse P1 | Authorized read-only (§13) |
| 4 | P structural checking included | Included as supplied-content verification only (§8, §12) |
| 5 | Subdivision required? | Not required; one bounded package authorized (§14) |
| 6 | Exact surfaces | Defined (§15) |
| 7 | Trusted acquisition excluded | Confirmed excluded (§12, §24) |
| 8 | Non-authoritative output contract | Adopted (§16) |
| 9 | Positive/negative controls | Required (§§17–18) |
| 10 | Independent qualification | Mandatory P2-Q1 (§19) |
| 11 | RELEASE/P4 conditions | Adopted conditionally (§22) |
| 12 | E-1 restrictions | No E-1 consumer (§21) |
| 13 | Retained qualifications | Retained (§23) |
| 14 | Forbidden authority transitions | Enumerated (§§11–12, §16, §24) |
| 15 | First P2 package authorization | Granted with qualifications (§25) |

## 4. P1 accepted-state verification

P1 remains accepted with qualifications at the exact Q1-reviewed revision: production blobs `276581c4…`, `868ddbee…`, `3344069c…`, `fdff96b9…`; six test blobs recorded in the P1 owner acceptance; acceptance blob `f360932a…` unchanged. No further P1 acceptance is needed, and this decision does not reopen P1.

## 5. P1-Q1 qualification applicability

- **Q1-F01 (depth 8):** R/Wc/P are single-level objects (depth ≤ 2); compatible. Depth 8 remains an implementation constraint, not a CJSON-1 rule; P2 must not reinterpret it.
- **Q1-F02 (empty strings):** no R/Wc/P field permits an empty string; the accepted codec behavior applies unchanged.
- **Q1-F03 (null/unknown fields):** R/Wc/P enforce exact field sets and the K pre/post qualification-pair rule; CJSON-1 and F02 semantics are not weakened.
- **Q1-F04 (rebuild non-identity):** retained; P2 builds will not be claimed bit-identical.
- **Q1-F05 (direct-call-only reachability):** P2 requires its own fresh Debug and Release evidence; P1 results do not transfer.
- **Q1-F06 (runtime-only exclusions):** issuer authenticity, protected acquisition, host checks, freshness, and execution remain outside P2.
- **Q1-F07 (path observation):** confirmed inapplicable — R/Wc/P contain no path fields.

No qualification blocks P2; none is reopened.

## 6. R frozen-schema ruling

Confirmed from the adopted P0 record §11.1: exactly 13 fields (`actionScriptSha256`, `artifactSetSha256`, `caseId`, `catalogSha256`, `conclusion` = `supported-for-bounded-validation`, `fixtureManifestSha256`, `fixturePolicySha256`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` = 1, `topology`); `catalogSha256` = `Hk`; ten duplicated K values; no reviewer identity; no timestamp; no owner hash; `Hr = SHA256(CJSON1(R))`.

**Authorized:** R structural and content-binding verification (CJSON-1 bytes, field set, literals, types, identifiers, identity computation, relationship to supplied K, duplicated-field consistency). **Prohibited:** any conclusion that V exists, is independent, performed a review, is authorized, or that R originated from an authorized reviewer. A structurally valid R record is not proof of a real independent review.

## 7. Wc frozen-schema ruling

Confirmed from P0 §11.2: exactly 11 fields (`artifactSetSha256`, `caseId`, `catalogReviewSha256` = `Hr`, `catalogSha256` = `Hk`, `decision` = `accept-exact-bounded-validation-catalog`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` = 1, `topology`); eight duplicated values; no owner identity, signature, timestamp, or P hash; `Hw = SHA256(CJSON1(Wc))`.

**Authorized:** Wc structural and binding verification against supplied K and R. **Prohibited:** any conclusion that W personally accepted anything, that W was an authorized human owner, that the decision was actually issued, or that the catalog is operationally approved. The `decision` literal is frozen content, not evidence of human authority.

## 8. P frozen-schema ruling

Confirmed from P0 §11.3: exactly 12 fields (`accepted` = true, `artifactSetSha256`, `caseId`, `catalogReviewSha256` = `Hr`, `catalogSha256` = `Hk`, `hostId`, `ownerCatalogAcceptanceSha256` = `Hw`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` = 1, `topology`); eight duplicated values; no self hash; no administrator identity, signature, nonce, or timestamp; `Hp = SHA256(CJSON1(P))`.

**Authorized (Operation 1 only):** verification of supplied P-shaped bytes — CJSON-1 conformance, exact field set, literal checks, identity calculation, `Hk`/`Hr`/`Hw` relationships, duplicated-value consistency. **Prohibited (Operation 2):** any conclusion that P is an authentic protected approval provisioned by A, was acquired from trusted custody, or constitutes operational approval. `accepted=true` is a content requirement and must never be treated as standalone operational approval.

## 9. Hk/Hr/Hw/Hp identity and binding ruling

All four identities are SHA-256 over the exact accepted canonical bytes of their artifact, with no self-reference: `Hk` over K (references `Hm`/`Hf`/`Hs`), `Hr` over R (binds `Hk` + ten duplicated K values), `Hw` over Wc (binds `Hk`/`Hr` + eight duplicated values), `Hp` over P (binds `Hk`/`Hr`/`Hw` + eight duplicated values). Graph `M → F → S → K → R → Wc → P → downstream live mechanisms` unchanged; no new edges; no edge returns into K/F/S; P has no self-hash field.

The future implementation **must**: (1) use exact accepted canonical bytes; (2) recompute identities independently of input assertions; (3) verify every mandatory referenced identity; (4) verify all accepted duplicated values; (5) reject missing or inconsistent bindings; (6) reject unsupported identities or versions; (7) reject incorrect field sets; (8) reject content substitutions violating accepted relationships; (9) preserve exact frozen record boundaries; (10) add no approval or issuer authority to digest verification. `Hp` is the identity of accepted P content, not evidence of administrative approval.

## 10. V/W/A authority-separation ruling

Preserved: V is a procedural independent reviewer; W is the human owner (not installed owner process O); A is a separately authorized root administrator; V independence is procedural; P is eligibility content only; no producer-built P; no delegation; no standing approval; no cross-host reuse; unselected Mechanism A durable permits remain unselected. P2 must not implement any V/W/A actor, issue R/Wc/P, designate reviewers/owners/administrators, or declare any supplied chain genuinely approved. The administrative roles exist in accepted governance, not as authority proved by record contents.

## 11. Structural versus operational verification ruling

Adopted as a binding implementation boundary: P2 may implement categories **A** (structural) and **B** (content binding) only. Categories **C** (provenance/issuer authority), **D** (protected acquisition/publication), **E** (live invocation authorization), and **F** (positive validation authority) are excluded. A category A/B result must not imply success in C–F, and no result naming may suggest otherwise. Content-chain verification is not operational approval verification.

## 12. Protected P-content boundary

Operation 1 (supplied P-shaped bytes) is authorized under the non-authority naming rules of §16. Operation 2 (authentic protected approval provisioned by A) is excluded and remains with the protected path (E-5/E-6) and D1 governance. No method may be named `VerifyApproval`, `VerifyEligibility`, `IsAuthorized`, `IsEligible`, or similar; result types and documentation must state that no custody, authenticity, approval, or eligibility is established.

## 13. P1 reuse authorization

**P1 REUSE AUTHORIZED WITH QUALIFICATIONS — EXISTING IMPLEMENTATION READ-ONLY.** Permitted calls: `CanonicalJson` (`TryParse`, `Encode`, `Hash`, `IsLowerHex`), `FrozenSchemaRules.IsValidId`, and `FrozenContractVerifier.TryVerifyCatalog`. Conditions: no modification of accepted P1 source files; no modification of `FinalAcceptanceCodec`; no new canonicalization algorithm; no new public API; no authority factory; no change to accepted K verification semantics; no frozen-schema modification; no project or package change; no new production caller; F02 parity/divergence controls preserved. If an essential interface cannot be used safely without modifying P1, the implementation must stop and seek a separate owner amendment. No broad P1 refactor is authorized.

---

## 14. Exact P2 implementation scope and authorization matrix

The authorized scope is: **an internal, unprivileged, production-unreachable verifier of supplied K, R, Wc, and P bytes that establishes only structural and content-binding consistency under the accepted frozen schemas.** No operational approval, issuance, custody, or invocation property is included.

Authorization matrix:

| Area | Ruling |
|---|---|
| P2 entry | **AUTHORIZED WITH QUALIFICATIONS — P2 ONLY** (§25) |
| R structural verification | Authorized exactly per §6 |
| Wc structural verification | Authorized exactly per §7 |
| P content structural verification | Authorized Operation 1 only (§8, §12) |
| Hk/Hr/Hw/Hp | Exact accepted bindings; recompute and verify per §9 |
| P1 reuse | Authorized read-only per §13 |
| P1 modification | Prohibited without a separate owner amendment |
| CJSON-1 behavior | Existing accepted rules only; no new canonicalization |
| V/W/A actors | Excluded (§10) |
| R/Wc/P issuance | Excluded |
| Trusted acquisition | Excluded |
| Administrative approval | Excluded |
| Positive validation | Excluded |
| Invocation authority | Excluded |
| Result semantics | Non-authoritative per §16 |
| Source/test surfaces | Precisely bounded per §15 |
| Positive fixtures | Required per §17 |
| Negative controls | Required per §18 |
| Production reachability | Evidence required per §20 |
| E-1 consumer | Prohibited per §21 |
| RELEASE | Must remain qualified per §22 |
| P4 | Conditional neutrality per §22 |
| Independent P2-Q1 | Mandatory per §19 |
| P2 owner acceptance | Mandatory per §19 |
| E-3 through E-7 | Not authorized (§24) |
| Slice N | Not authorized (§24) |

Detail for the authorized items: **normative basis** — P0 record §§6, 11.1–11.6; D1 decision §§5–7, 9; entry-owner-02; this decision. **Permitted implementation** — supplied-byte structural/binding verification only, with internal results. **Prohibited behavior** — everything in §§10–12 and §24. **Required test/evidence** — §§17–18, §20, §22. **Qualification condition** — P2-Q1 and separate owner acceptance. **Stop condition** — any need to modify accepted code, any protected/authority mechanism, any ambiguity in an accepted field or binding (stop and seek an owner amendment rather than inventing a rule).

## 15. Permitted source and test surfaces

Permitted additive surfaces (repository-confirmed by the assessment; P1 uses the same layout):

- **Production:** new internal files only under `src/Gagamba.Execution.Linux.M1/Validation/` — internal R structural verification, internal Wc structural verification, internal P-content structural verification, internal chain-binding verification, internal non-authoritative result records, and deterministic failure handling.
- **Tests:** new files only under `tests/Gagamba.Execution.Linux.M1.Tests/Validation/` — positive fixtures, adversarial negatives, authority-boundary controls, and test-side reachability checks.

Prohibited: modification of any existing file (P1, `FinalAcceptanceCodec`, E-1, authority-bearing source, existing tests, `eng/**`, project/solution files, package references); new projects; new public APIs; protected-root functionality; operational approval issuance. If repository conventions make these surfaces insufficient (for example an unexpected project-file requirement), the implementation must stop and request an amendment before proceeding.

## 16. Non-authoritative output contract

Permitted conclusions: R/Wc/P content structurally conforms; `Hk/Hr/Hw/Hp` identities are consistent; supplied K/R/Wc/P relationships are internally consistent. Forbidden conclusions: independent review occurred; human-owner acceptance occurred; administrative approval occurred; P was provisioned by A or retrieved from trusted custody; the catalog is eligible for live invocation; a live invocation is authorized; positive validation is permitted; protected execution may begin. Forbidden naming includes `Approved`, `Authorized`, `Trusted`, `Eligible`, `ExecutionPermit`, `ValidationAuthority`, and `VerifyApproval`. No promotion mechanism, public authority-bearing API, or factory may be created; successful content verification cannot establish an authorization transition.

## 17. Required positive fixtures

At least one complete hand-constructed canonical chain `M → F → S → K → R → Wc → P` with both major variant families where practical (B2/pre and B3/post), using accepted P1 functionality for the K verification where appropriate but **not** generating expected values solely through the P2 code under test. Required: hand-constructed canonical R/Wc/P bytes; independently computed `Hk/Hr/Hw/Hp` digests; exact expected binding values and verification results; reproducible construction instructions; multiple K variants to exercise qualification states. All fixtures are test-only, labeled as fixtures, and are **not** genuine issued review, owner-acceptance, or administrative-approval records.

## 18. Required negative controls

- **Canonical/structural failures:** malformed CJSON-1; noncanonical bytes; duplicate keys; incorrect key order; unsupported schema/version; missing field; wrong type; invalid identifier; invalid required literal; prohibited extension; truncated; oversized.
- **Identity/binding failures:** incorrect `Hk`/`Hr`/`Hw`; `Hp` identity recalculation; R/Wc/P referencing substituted or mismatched K/R/Wc; duplicated-value mismatch; conflicting otherwise valid records; invalid chain relationships where structurally defined; semantically invalid but correctly rehashed records.
- **Authority-boundary controls:** extra reviewer identity/signature where prohibited; extra owner or administrator authority metadata; valid P-shaped bytes from an untrusted source; forged issuer claims outside the frozen schema; `accepted=true` without trusted provenance; attempted promotion of structural results into operational approval; attempted use as an invocation permit.

Two outcomes must be distinguished: **structural failure** (reject when an accepted frozen invariant is violated) and **authority not established** (structurally accept valid bytes while making no authority claim). No parser rejection may be invented for missing issuer authenticity, which is explicitly outside P2. Every test references an accepted invariant or an explicit authority boundary.

## 19. Independent P2-Q1 requirement

A separate independent qualification commission, **`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-Q1`**, is mandatory. The reviewer must: reconstruct R/Wc/P schemas independently; verify exact canonical-byte requirements; construct original positive fixtures; independently compute `Hk/Hr/Hw/Hp`; challenge field sets and literals; challenge duplicated K values; challenge all content bindings; construct substituted and correctly rehashed chains; compare results with accepted P1 K verification; challenge malformed/noncanonical input; test structurally valid but untrusted P-shaped bytes; verify no operational V/W/A actor, no issuance mechanism, no protected-root or custody integration, and no protected acquisition; verify non-authoritative output semantics; inspect production reachability; verify no E-1 consumer; run RELEASE production verification; verify conditional P4 neutrality; and preserve native limitations. Producer-authored passing tests are insufficient. A separate formal P2 owner acceptance (`…-P2-OWNER-ACCEPTANCE-01`) must follow successful Q1.

## 20. Production reachability requirements

P2 must remain production-unreachable; internal visibility alone is insufficient. Required future evidence: Debug direct-call inspection; RELEASE direct-call inspection; scanner positive control; no production authority consumer; no final-admission consumer; no protected-sink caller; no validation-authority factory; no E-1 production consumer; no exported public Validation type; no protected acquisition or invocation dependency. The Q1-F05 limitation is preserved: **direct-call absence is not a complete reflection-aware or transitive reachability proof**; no stronger claim may be made than the executed evidence supports. If a production consumer becomes necessary, implementation stops and requires separate authorization.

## 21. E-1 consumer exclusion

**NO E-1 PRODUCTION CONSUMER** is confirmed for the authorized design. Preserved: F-01 owner/recovery role-hash bindings, runtime-inventory binding, and fail-closed integrity-exception handling; F-02 Q1-F01–Q1-F06, the first-consumer parity/branch-dominance/verification-to-use/authority review, and the required Option A reassessment; F-03 Windows PowerShell 5.1 qualification, PowerShell 7 unexecuted, existing failure-exit semantics, and scoped preference restoration. P2 does not authorize an E-1 consumer and must not be described as E-1 production integration.

## 22. RELEASE and P4 conditions

P2's entry classification is **P4-NEUTRAL, CONDITIONAL**. Required post-implementation evidence: only approved new internal files; no existing protected-sink change; no new authority path; no public API expansion; no positive-validation factory; no E-1 consumer; no protected-root integration; no protected-execution caller; RELEASE production verifier PASS with the expected Debug-as-production FAIL; unchanged verifier policy; no RELEASE test-hook leakage. Preserved: F-R1/O-R1, O-R2, O-R3, observation-to-effect residual, `NATIVE-PENDING`, all 331 native skips. If implementation creates a production-reachable approval or protected-execution path, it stops and requires a separate P4 assessment and owner authorization; P4 neutrality may not be inferred from unchanged public signatures.

## 23. Retained P0/D1/P1 qualifications

- **P0:** external-package durability limitation; no independently verified external Git history; historical `PENDING` captions; accepted S2 grammar reference; spot-checked rather than exhaustive provenance extraction; single-reviewer governance limitation.
- **D1:** procedural V independence; separate W and A authorities; trusted-root threat-model limits; unprovisioned protected roots; deferred refusal encodings; no delegation; no standing approval; no cross-host reuse; F02 authority separation; unselected Mechanism A durable permits; pending native qualification. Adoption of D1 is not qualification of operational administrative authority.
- **P1:** all seven Q1 findings (Q1-F01 through Q1-F07) as ruled in §§5 and the P1 owner acceptance; P1 remains bound to its exact accepted revision.

## 24. Downstream exclusions

Not authorized: protected P acquisition; protected-root provisioning; trusted-administrator verification; publication ordering and durability; R/Wc/P issuance; live invocation; positive validation authority. The adopted roadmap is retained unchanged: E-3 v3-pre codec; E-4 GAVA1 (mandatory P4 re-audit); E-5 fixed launcher and custody; E-6 atomic enablement and R/O/C ordering (mandatory P4 re-audit); E-7 driver integration; Slice N protected native qualification. Where an operational responsibility spans milestones, the exact accepted ownership is retained rather than reassigned. Composite Q-E1 is not closed, and E-2 remains open beyond P1/P2.

## 25. Formal owner disposition

**P2 ENTRY AUTHORIZED WITH QUALIFICATIONS — P2 ONLY.**

The accepted evidence supports exactly one bounded P2 implementation package for supplied-content R/Wc/P structural and content-binding verification, subject to the mandatory P2-Q1 qualification and separate owner acceptance and to every authority exclusion in this decision. The authorization permits a future separately commissioned implementation only; it does not begin implementation, does not authorize independent qualification, and does not create any operational approval, eligibility, invocation, or execution authority.

## 26. Recommended next commission

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2`** — implement the accepted, internal, unprivileged, production-unreachable structural and content-binding verifier for supplied K/R/Wc/P bytes. The future implementation commission must: start from this decision's commit; modify only the §15 surfaces; reuse P1 without changing it; implement the exact R/Wc/P frozen schemas; verify `Hk/Hr/Hw/Hp` identities and accepted bindings; use hand-constructed positive fixtures; include adversarial raw-byte and hash-consistent negatives; keep outputs explicitly non-authoritative; avoid R/Wc/P issuance and trusted acquisition; demonstrate production unreachability; preserve E-1 and RELEASE boundaries; demonstrate conditional P4 neutrality; produce reproducible evidence; stop before independent P2-Q1; and require separate formal P2 owner acceptance.

This recommended commission is not started automatically. E-2-P2 implementation, P2-Q1, E-3 through E-7, Slice N, catalog approval, protected roots, live invocation, positive validation, and protected execution remain unauthorized. This decision stops here: no implementation, no qualification execution, no approval issuance, no synchronization, and no push.


