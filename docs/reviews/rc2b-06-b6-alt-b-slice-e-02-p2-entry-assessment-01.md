# ALT-B Slice E-2 P2 entry assessment — detached R/Wc/P structural verification

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-ASSESSMENT-01`

Date: 2026-11-09 (Asia/Manila). Reviewer: DeepSeek. Scope: documentation-only architectural entry assessment on canonical `main`; no push. This is a single-reviewer governance assessment, not independent multi-model qualification and not an owner decision.

Assessed package: **E-2-P2 — detached R/Wc/P structural verification** (proposed; not authorized). Preceding milestone state: **E-2-P1 ACCEPTED WITH QUALIFICATIONS**.

Formal assessment disposition: stated once in §20. This assessment does not authorize P2 implementation; it prepares the separate owner entry decision.

---

## 1. Baseline and preservation evidence

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| HEAD / tree | `28275ba516ba9326bd7a850357fa3143749ecd03` / `3ae03486d3373caab9cfd9191204761dcb955cc6` |
| Worktrees | exactly one canonical checkout; tracked/index clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 24 commits ahead; no push |
| P0 / D1 / entry-owner-02 / P1 acceptance | blobs `db25949d…` / `83fe8d26…` / `f5ab96c6…` / `f360932a…` |
| P1 implementation / Q1 report | verifier blob `fdff96b9…` / Q1 blob `7ee0b781…` |

Ancestry verified (`git merge-base --is-ancestor`, all exit 0) for P1 acceptance, Q1, P1, entry-owner-02, reassessment, D1, P0 acceptance, E-2 entry decision, P4, E-1, and F-01/F-02/F-03. No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was used. Detail: [baseline-and-preservation.md](rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01-evidence/baseline-and-preservation.md).

## 2. Accepted milestone-state reconstruction

Confirmed from committed decisions, not from current-state captions alone:

- **P0:** P0 ACCEPTED WITH QUALIFICATIONS — Q-E1 satisfied for the E-2 subset (CJSON-1 and K/M/F/S/R/Wc/P provenance); composite Q-E1 remains open for GAVA1 headers, v3-pre layouts, and release correlation. Source: [P0 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md); adopted record `db25949d…`.
- **D1:** D1 APPROVAL CHAIN ADOPTED WITH QUALIFICATIONS — Q-E2 satisfied for the E-2 scope; V/W/A case, R/Wc/P, roots/custody, publication, live-binding separation, F02 separation, no delegation. Source: [D1 decision](../decisions/rc2b-06-b6-alt-b-slice-e-02-d1.md).
- **E-2 entry:** E-2 ENTRY AUTHORIZED WITH QUALIFICATIONS — E-2-P1 ONLY. Source: [entry owner decision 02](../decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md).
- **P1:** P1 ACCEPTED WITH QUALIFICATIONS at the exact Q1-reviewed revision. Source: [P1 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-02-p1-owner-acceptance-01.md).
- **P2:** not authorized. No R/Wc/P verification implementation exists or has been accepted; the repository contains no R/Wc/P reader, store, or evidence verifier (source scan and P0/P1 reports).

## 3. P1 identity and reusable-contract analysis

Accepted P1 implementation blobs: `CanonicalJson.cs` `276581c4…`; `FrozenSchemaRules.cs` `868ddbee…`; `FrozenContracts.cs` `3344069c…`; `FrozenContractVerifier.cs` `fdff96b9…` (internal, production-unreachable, P4-neutral at the accepted revision). P2 may reuse, without modifying P1:

| P1 component | P2 reuse |
|---|---|
| `CanonicalJson.TryParse` / `Encode` / `Hash` / `IsLowerHex` | CJSON-1 validation, canonical serialization, exact-byte SHA-256, hex checks for R/Wc/P |
| `FrozenSchemaRules.IsValidId` | `caseId` and post-phase `qualificationId` grammar in R/Wc/P |
| `FrozenSchemaRules.IsCanonicalUuid` / `IsAbsolutePosixPath` | not required by R/Wc/P (no bootId/path fields) |
| `FrozenContractVerifier.TryVerifyCatalog` | obtain a verified K contract and recompute `Hk` from supplied K bytes before binding R/Wc/P |
| P1 result patterns (`Try*` → `false` + failure + null; internal records) | consistent non-authoritative R/Wc/P contract and binding results |

Constraints: P1 files remain **read-only**; P2 adds new internal files under the same authorized directories; no new public API, no authority factory, no project-file change. P1's divergence-control obligation (parity reassessment on any change to either canonical implementation) carries forward; P2 introduces no new canonicalization logic. Detail: [p1-reuse-and-negative-controls.md](rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01-evidence/p1-reuse-and-negative-controls.md).

## 4. P1-Q1 qualification applicability

| Q1 finding | Effect on P2 |
|---|---|
| Q1-F01 depth-8 constraint | No impact: R, Wc, and P are single-level objects (depth ≤ 2); no accepted R/Wc/P shape approaches the bound |
| Q1-F02 empty-string interpretation | Compatible: no R/Wc/P field permits an empty string (hashes, `ValidId`s, enums, fixed literals, integers, booleans) |
| Q1-F03 null/unknown-field behavior | Compatible: R/Wc/P follow the K null rules for the pre/post qualification pair, and exact field sets reject extensions |
| Q1-F04 non-bit-identical rebuild | Carries forward: P2 builds will not be bit-identical across rebuilds; source blobs remain the identity anchor |
| Q1-F05 direct-call-only reachability | Applies directly: P2 must produce its own Debug and Release direct-call absence evidence; a first production consumer needs separate authorization |
| Q1-F06 deferred runtime-only checks | Defines P2's exclusions: issuer authenticity, protected acquisition, freshness, live invocation, custody |
| Q1-F07 `/` path observation | Not applicable: no path fields exist in R/Wc/P |

No P2 requirement exceeds an accepted P1 limitation; no reconciliation or owner amendment is required for P1 reuse. R/Wc/P values are representable in accepted CJSON-1 (lower-case hex, `ValidId`, enums, integers, booleans, `null` only in the qualification pair for the pre phase).

## 5. R/Wc/P schema reconstruction

Exact fields from the adopted P0 record §11 (extracted from `case-policy-and-action-contract.md` §1; `667e6118…`), in canonical ascending order:

**R — detached independent catalog review (13 fields).** `actionScriptSha256`, `artifactSetSha256`, `caseId`, `catalogSha256`, `conclusion` (= `supported-for-bounded-validation`), `fixtureManifestSha256`, `fixturePolicySha256`, `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`. `catalogSha256` = `Hk`; all duplicated identity/fixture/action values equal K; qualification pair follows K's pre/post null rule. `Hr = SHA256(CJSON1(R))`. R contains no owner-acceptance hash and **no reviewer identity field**.

**Wc — detached owner catalog acceptance (11 fields).** `artifactSetSha256`, `caseId`, `catalogReviewSha256` (= `Hr`), `catalogSha256` (= `Hk`), `decision` (= `accept-exact-bounded-validation-catalog`), `hostId`, `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`. Duplicated values equal K. `Hw = SHA256(CJSON1(Wc))`. Wc contains no P hash and **no owner identity field, signature, or timestamp**.

**P — protected catalog-eligibility approval content (12 fields).** `accepted` (= `true`), `artifactSetSha256`, `caseId`, `catalogReviewSha256` (= `Hr`), `catalogSha256` (= `Hk`), `hostId`, `ownerCatalogAcceptanceSha256` (= `Hw`), `phase`, `qualificationId`, `qualificationRecordSha256`, `schemaVersion` (= 1), `topology`. Duplicated values equal K. P contains **no self hash**, **no administrator identity field, signature, nonce, or timestamp**. `Hp = SHA256(CJSON1(P))`.

Structural limits: every R/Wc/P string field is a hash, a `ValidId`, an enum, or a fixed literal; the only non-string values are `schemaVersion`, `accepted`, and the pre-phase `null` qualification pair. Values are the accepted `ValidId` grammar and 64/40-lower-hex conventions already implemented by P1.

## 6. Hk/Hr/Hw/Hp identity and binding matrix

| # | Property | Hk (K) | Hr (R) | Hw (Wc) | Hp (P) |
|---|---|---|---|---|---|
| 1 | Artifact | Protected catalog K | Detached review R | Detached owner acceptance Wc | Protected approval content P |
| 2 | Algorithm | SHA-256 | SHA-256 | SHA-256 | SHA-256 |
| 3 | Input bytes | Exact canonical `CJSON1(K)` bytes | Exact canonical `CJSON1(R)` bytes | Exact canonical `CJSON1(Wc)` bytes | Exact canonical `CJSON1(P)` bytes |
| 4 | Canonicalization | CJSON-1; parse→serialize equality required | Same | Same | Same |
| 5 | Included fields | All 22 K fields | All 13 R fields | All 11 Wc fields | All 12 P fields |
| 6 | Excluded fields | Review/owner/receipt/self-hash fields | Owner/P references | P reference | Self hash |
| 7 | Self-reference | K contains no self hash | R contains no self hash | Wc contains no self hash | P contains no self hash |
| 8 | Required references | `fixtureManifestSha256`, `fixturePolicySha256`, `actionScriptSha256` (to M/F/S) | `catalogSha256`=`Hk` + 10 duplicated K values | `catalogSha256`=`Hk`, `catalogReviewSha256`=`Hr` + 8 duplicated K values | `catalogSha256`=`Hk`, `catalogReviewSha256`=`Hr`, `ownerCatalogAcceptanceSha256`=`Hw` + 8 duplicated K values |
| 9 | Format | 64 lower-case hex | Same | Same | Same |
| 10 | Failure behavior | Refuse on noncanonical bytes, field-set/type/bounds mismatch, broken M/F/S bindings | Refuse on noncanonical bytes, field-set/type mismatch, `Hk` mismatch, duplicated-value mismatch | Refuse on `Hk`/`Hr` mismatch, duplicated-value mismatch | Refuse on `Hk`/`Hr`/`Hw` mismatch, `accepted` false, duplicated-value mismatch |
| 11 | P1 already supplies | Full structural verification + identity recomputation | CJSON-1/hash/`ValidId`/result patterns only | Same | Same |
| 12 | P2 may check without authority | Yes (already accepted) | Yes: structure + bindings to supplied K | Yes: structure + bindings to supplied K and R | Yes: structure + bindings to supplied K, R, Wc |

**Binding graph (accepted, unchanged):** `M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability`. K contains no reference to R/Wc/P; R contains no owner hash; Wc contains no P hash; P binds `Hk`/`Hr`/`Hw`; no edge returns into K/F/S; no hash fixed point. **P2 may verify exactly these edges over supplied bytes and must not introduce new edges** (for example, it must not require R to embed a reviewer identity, or P to embed an administrator identity, because the accepted contracts define no such fields). Detail: [rwcp-schema-and-bindings.md](rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01-evidence/rwcp-schema-and-bindings.md).

## 7. V/W/A authority separation

| Chain step (D1 §5–§7) | Required evidence nature | Category (see §8) | P2 role |
|---|---|---|---|
| V independently reviews K and issues R | Human/administrative review; role attribution (F02 Q07), not cryptographic | C | None: P2 can check only R's structure and its `Hk` binding |
| W accepts exact K/R bytes and issues Wc | Owner administrative act; no identity field in Wc | C | None: P2 cannot prove personal acceptance |
| A verifies actual V/W provenance and provisions P under protected roots | Root-administrative process; custody, modes, no-replace, fsync | C + D | None: P2 checks supplied P-shaped bytes only |
| R/Wc/P publication order (R, Wc, then P last) | Filesystem events | D | None: a serialized record cannot prove publication order |
| L protected verification and pre-B1 rechecks | Trusted filesystem acquisition + rechecks | D | None: excluded |
| A authorizes a live invocation (fresh challenge/response, one-response latch) | Live process + administrative session | E | None: excluded |
| Capability construction after B1/B2 | Atomic Slice E mechanisms | E + F | None: excluded |

No operational V/W/A role, issuance, actor, or approval-state transition is part of P2. P2 may not issue R/Wc/P, may not authorize a catalog, and may not turn a structurally consistent evidence chain into an invocation capability.

## 8. Structural versus authority verification matrix

Categories: **A** pure structural; **B** content binding; **C** provenance/issuer; **D** protected acquisition/publication; **E** live authorization; **F** positive-validation authority construction.

| Proposed check | Category | P2 permissible? | Owner if deferred |
|---|---|---|---|
| CJSON-1 canonical bytes for R/Wc/P | A | Yes | — |
| Exact field sets, versions, types, `ValidId`s, enums, fixed literals, `accepted`=true, pre/post null pair | A | Yes | — |
| Recompute `Hk` from supplied K bytes (via P1 `TryVerifyCatalog`) | A/B | Yes | — |
| R `catalogSha256` == `Hk`; R duplicated fields == K | B | Yes | — |
| Recompute `Hr`; Wc `catalogReviewSha256` == `Hr`, `catalogSha256` == `Hk`, duplicated fields == K | B | Yes | — |
| Recompute `Hw`; P `ownerCatalogAcceptanceSha256` == `Hw`, `catalogReviewSha256` == `Hr`, `catalogSha256` == `Hk`, duplicated fields == K | B | Yes | — |
| Cross-record consistency (K/R/Wc/P all describe the same case/phase/topology/host/artifact/qualification) | B | Yes | — |
| Reviewer is a real independent person; W is the human owner; A is authorized | C | **No** | D1 governance; protected administrative process |
| V/W provenance actually verified by A | C | **No** | A's root-administrative process |
| P came from the protected root, correct ownership/modes/no-replace/fsync | D | **No** | E-5/E-6 protected path |
| Publication order enforced; withdrawal/reapproval semantics | D | **No** | E-6 |
| Freshness/expiry of an approval | D/E | **No** | E-6 with B1/A-consumption rules |
| Live challenge/response; per-invocation authorization | E | **No** | E-6 (with E-4) |
| Capability construction; positive validation authority | F | **No** | E-4/E-6/E-7 + Slice N |

The preferred P2 boundary is **A + B only**. A self-declared issuer is not proof of authority; a matching hash is not evidence of human acceptance; a syntactically valid protected-approval document is not a protected approval. Detail: [structural-vs-authority.md](rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01-evidence/structural-vs-authority.md).

## 9. Protected P boundary assessment

Two operations must be distinguished:

1. **Verify supplied P-shaped bytes** — establishes only that the bytes satisfy the accepted P content schema and their required content relationships (`Hk`/`Hr`/`Hw`, `accepted`=true, duplicated values equal K).
2. **Verify that P is the authentic protected approval provisioned by A** — requires accepted administrative, storage, custody, and verification mechanisms (A's root process, `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`, root:root `0444`, no-replace + fsync, independent provenance verification, and L's protected read/rechecks).

Operation 1 can be safely included in P2 **only** under strict result-contract restrictions: the result must be named and documented as content/binding verification (for example `AdminRecordContentContract` or an equivalent explicit non-authority name), must state in its contract that it proves no custody, authenticity, approval, or eligibility, and no method may be named `VerifyApproval`, `VerifyEligibility`, `IsAuthorized`, or similar. Operation 2 is deferred to the protected path (E-5/E-6) and D1 governance. If the owner prefers maximal caution, a justified subdivision is available — P2-A (R/Wc structural and binding checks) and P2-B (P content structural verification) — but no contractual dependency requires it, since all three checks are the same A/B category; the assessment recommends the single bounded package with the naming restriction, and records the subdivision as an owner option rather than a requirement.

---

## 10. Trust-anchor and F02 separation

D1 adopted a non-cryptographic administrative trusted-root model; P2 does not implement any part of it. Preserved and excluded from P2: explicit administrative authority; separately authorized A; protected root ownership; required filesystem modes; no-replace publication; durability; independent protected verification; pre-B1 rechecks; per-invocation authorization. Threat-model exclusions retained: malicious or colluding root, copied-host rollback, hostile same-UID access where excluded.

**F02 remains separate.** F02 qualification-store authority (`/etc/gagamba/accepted-qualifications/...`) and the final-admission chain are not reusable as validation-catalog approval evidence; P2 must not import F02 root authority by analogy, must not consume F02 records or final-admission results as R/Wc/P provenance, and must not create a second trust anchor or delegation system. If a proposed check needed trusted actor designation, its accepted authority source would have to exist; none does for P2's scope, so such checks are deferred (category C/D), not invented.

## 11. Protected publication and custody exclusions

| Requirement | Accepted source | Owner | P2 |
|---|---|---|---|
| R/Wc publication at `/etc/gagamba/validation-catalog-evidence/`, root:root `0444`, no-replace, fsync | P0 §11.4; case-policy §1 step 5 | E-5/E-6 protected path | Excluded |
| P publication last under authority/install locks | P0 §11.4 item 2 | E-6 | Excluded |
| Withdrawal/replacement serialized with B1/A-consumption | case-policy §1 step 19 | E-6 | Excluded |
| Pre-B1 protected approval rechecks | case-policy §1 step 7 | E-6 (launcher) | Excluded |
| Live challenge/response and one-response latch | P0 §11.4 items 4–6 | E-6 (with E-4) | Excluded |
| fsync durability, no-replace atomicity | P0 §12.2 | E-5/E-6 | Excluded |

A serialized timestamp or declared publication order does not independently prove that the accepted publication sequence occurred; R/Wc/P contain no ordering fields at all, and P2 must make no ordering or custody claim. Operational requirements stay with their accepted later Slice E owners; nothing is reallocated.

## 12. Proposed implementation surfaces

Candidate surfaces (based on P1; **not yet authorized**): new files only under `src/Gagamba.Execution.Linux.M1/Validation/` with tests under `tests/Gagamba.Execution.Linux.M1.Tests/Validation/`.

| Surface | Responsibility |
|---|---|
| New `DetachedEvidenceContracts.cs` (name illustrative; final naming subject to the no-authority-words rule) | Internal non-authoritative contract values for R, Wc, P content, plus a supplied-evidence binding result |
| New `DetachedEvidenceVerifier.cs` | `TryVerifyReviewRecord`, `TryVerifyOwnerRecord`, `TryVerifyAdminRecordContent`, and `TryVerifyEvidenceBindings(k, r, wc, p, …)` using P1 components |
| Reused P1 files | Read-only: `CanonicalJson`, `FrozenSchemaRules`, `FrozenContractVerifier.TryVerifyCatalog` |
| New tests | Positive hand-written chains, raw-byte negatives, adversarial/authority-boundary controls, and Debug/Release direct-call absence assertions (mirroring P1's reachability pattern) |
| Unchanged | All existing production/test files, `FinalAcceptanceCodec`, `eng/**`, project/solution files, package references |

No project-file change is expected (SDK default globs, as verified for P1). No public API, authority factory, or production caller. If P2 could not proceed without modifying accepted authority-sensitive code, an owner amendment would be required; the assessment finds no such need. Detail: [p1-reuse-and-negative-controls.md](rc2b-06-b6-alt-b-slice-e-02-p2-entry-assessment-01-evidence/p1-reuse-and-negative-controls.md).

## 13. Negative-control strategy

Candidate independent-qualification matrix (controls apply only where the accepted contract defines the field or relationship; no rejection rule is invented to complete the matrix):

| Case | Accepted invariant | Expected refusal | Owner | Raw bytes | Trusted acquisition | Native |
|---|---|---|---|---|---|---|
| Malformed / noncanonical CJSON-1 in R/Wc/P | P0 §6 | `invalid CJSON-1` / `noncanonical` | P2 | Yes | No | No |
| Wrong schema / version | P0 §11.1–11.3 | field set / `schemaVersion integer` | P2 | Yes | No | No |
| Missing required field; wrong type; extension | P0 §11 | exact field-set rule | P2 | Yes | No | No |
| Invalid identity encoding (`caseId`, hashes, enums, literals, `accepted`) | P0 §11; `ValidId` | type/`ValidId`/hex/literal rules | P2 | Yes | No | No |
| Duplicate keys; truncated; oversized | P0 §6 | codec rules | P2 | Yes | No | No |
| Incorrect `Hk` in R/Wc/P | P0 §11.1–11.3 | binding mismatch | P2 | Yes | No | No |
| Incorrect `Hr` in Wc/P | P0 §11.2–11.3 | binding mismatch | P2 | Yes | No | No |
| Incorrect `Hw` in P | P0 §11.3 | binding mismatch | P2 | Yes | No | No |
| Substituted K content (bindings recomputed for the new K) | P0 §11.5 | duplicated-field equality to K | P2 | Yes | No | No |
| Substituted R/Wc/P with stale references | P0 §11 | binding mismatch | P2 | Yes | No | No |
| Conflicting but individually valid evidence (different case/phase/topology/host/artifact/qualification) | P0 §11 | cross-record equality | P2 | Yes | No | No |
| Wrong evidence target (R/Wc/P for a different K) | P0 §11 | `Hk` binding | P2 | Yes | No | No |
| Unauthorized authority field inserted into R/Wc/P | P0 §11 exact sets | field-set refusal | P2 | Yes | No | No |
| Self-declared issuer (extension field) | P0 §11 | field-set refusal | P2 | Yes | No | No |
| Structurally valid but untrusted P-shaped input | accepted design; no custody in P2 | **no structural refusal** — result states content-only success with no authority claim | P2 result semantics + D1/E-5/E-6 | Yes | No | No |
| Promotion attempt (treat binding success as approval/eligibility) | P1/P2 non-authoritative contract | no promotion API exists; test asserts absence | P2 | Yes | No | No |
| Real issuer authenticity, protected acquisition, publication order, freshness, live invocation | D1/E-5/E-6 | deferred: not checkable from supplied bytes | Downstream | — | Yes | Yes (native for real custody) |

Structural refusals are separated from authority refusals: P2 only refuses structural/binding violations; authority conditions produce no affirmative claim, never a false rejection ritual and never a success-shaped authority result.

## 14. Self-consistent adversarial-case analysis

| Case | P2 behavior (required) |
|---|---|
| R structurally valid and correctly bound to K, no independently established V authority | Accept structurally; result states content-consistency only, with no reviewer-independence, review-performed, or approval claim |
| Wc structurally valid and bound to K/R, no independently established W acceptance | Accept structurally; result states no human-acceptance, owner-identity, or approval claim |
| P structurally valid and consistently bound, supplied from an untrusted location | Accept structurally; result states no custody, authenticity, eligibility, or invocation claim; protected acquisition remains E-5/E-6 |
| Records referencing valid but different catalog versions | Refuse on the `Hk`/duplicated-value binding that differs |
| Hash-consistent evidence violating an accepted field or role constraint | Refuse on the violated structural rule (for example a literal or field-set rule) |
| Syntactically valid forged administrative identity field | Unknown/extension field → field-set refusal (no such accepted field exists) |

The parser is not required to prove an issuer is genuine when it has no trusted source; equally, a structurally successful result must never be described as approval. Adversarial-case expectations are incorporated into the negative-control strategy above and into the proposed P2 qualification criteria.

## 15. E-1 consumer classification

**NO E-1 PRODUCTION CONSUMER** (proposed). P2 needs no E-1 integrity projection, no loaded-installation verification, no final-admission consumer, and no protected execution caller; it consumes supplied byte arrays and P1 internal functions only. F-01 fail-closed future-consumer requirements, F-02 Q1-F01–Q1-F06 and the mandatory first-consumer parity/authority review, and F-03's Windows PowerShell 5.1 scope and tooling constraints remain preserved. If a future package required E-1 integration, that boundary would require separate authorization, not incidental inclusion.

## 16. RELEASE and P4 classification

**Proposed classification: P4-NEUTRAL, CONDITIONAL.** Rationale: P2 adds only new internal, unprivileged, non-authoritative structural/binding verification with no production caller, no protected-sink interaction, no admission or lifecycle change, no authority construction, and no RELEASE behavior change; the P1 precedent (independently verified direct-call absence and unchanged RELEASE verifier) supplies the pattern. The condition is implementation evidence: Debug and Release direct-call absence, no public API, no authority factory, RELEASE verifier PASS with the expected Debug-as-control FAIL, and no E-1 consumer. If any production-reachable authority path or protected-sink interaction appeared, the classification would become P4 RE-AUDIT REQUIRED or P4-BLOCKED and implementation would stop. Preserved: F-R1/O-R1, O-R2, O-R3, observation-to-effect residual, `NATIVE-PENDING`, all 331 native skips, and RELEASE test-hook isolation. Structural R/Wc/P tests provide no protected native qualification.

## 17. Downstream ownership map

| Excluded requirement | Accepted owner |
|---|---|
| v3-pre codec/layouts; composite Q-E1 items | E-3 (composite Q-E1 remains open) |
| Positive validation authority, GAVA1 direct context | E-4 (mandatory P4 re-audit) |
| Fixed launcher, privileged acquisition, custody interface | E-5 |
| Protected publication, withdrawal, ordering, authority/install locks, atomic enablement | E-6 (mandatory P4 re-audit) |
| Driver integration | E-7 |
| Protected native qualification, real protected positives | Slice N |
| V/W/A authenticity and administrative authority | D1 governance + protected administrative process |
| R/Wc/P issuance | V, W, and A respectively — never P2 |

No downstream responsibility is consumed or reassigned by the proposed P2.

## 18. Entry-readiness matrix

| Area | Conclusion | Classification of any residual |
|---|---|---|
| P1 acceptance | Verified | — |
| Frozen R schema | Complete (P0 §11.1) | — |
| Frozen Wc schema | Complete (P0 §11.2) | — |
| Frozen P content schema | Complete (P0 §11.3) | — |
| Hk/Hr/Hw/Hp bindings | Complete (P0 §11.5) | — |
| CJSON-1 compatibility | Supported (single-level objects; no depth/empty-string/null conflicts) | Accepted non-blocking |
| P1 reuse | Safe (internal calls; no modification) | — |
| Structural versus authority split | Enforceable (A+B vs C/D/E/F) | — |
| Trusted acquisition | Excluded from P2 | Future production integration requirement (E-5/E-6) |
| Actor authenticity | Excluded from P2 | Downstream authority requirement (D1 governance) |
| Protected P semantics | Clearly limited (content-only; naming restriction) | Downstream authority requirement (E-5/E-6; D1) |
| Negative-control feasibility | Demonstrated (fixture design above) | — |
| Output authority | Non-authoritative (proposed contract) | — |
| Production reachability | Classified (expected direct-call absence; evidence required) | Independent qualification prerequisite |
| E-1 consumer | None proposed | — |
| RELEASE impact | Bounded; verifier PASS expected | Independent qualification prerequisite |
| P4 impact | P4-NEUTRAL, CONDITIONAL | Independent qualification prerequisite |
| Native dependency | None for structural verification | Accepted non-blocking; Slice N downstream |
| Downstream responsibility | Preserved | — |
| First implementation package | Defined (§19); owner decision required | Entry governance step |

No entry-blocking or implementation-blocking item was found. The residuals are either independent-qualification prerequisites (evidence to be produced by the implementation and its Q1) or downstream obligations owned by other milestones.

## 19. Recommended bounded package

If the owner accepts this assessment, the recommended commission is:

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2`** — detached R/Wc/P structural and content-binding verification.

1. **Objective:** implement internal, unprivileged, production-unreachable structural verification of supplied R/Wc/P-shaped bytes and their accepted content bindings to supplied K bytes.
2. **Binding contracts:** P0 record §§6, 11.1–11.6; D1 decision §§5–7, 9; entry-owner-02 surfaces; P1 owner acceptance (reuse boundaries).
3. **Surfaces:** new files under `src/Gagamba.Execution.Linux.M1/Validation/` and `tests/Gagamba.Execution.Linux.M1.Tests/Validation/`; P1 files read-only; no project changes.
4. **Inputs:** supplied byte arrays for K, R, Wc, and P (no filesystem reads).
5. **Outputs:** internal non-authoritative contract values and binding results; no authority, no promotion.
6. **Structural checks:** exact field sets/versions/types, `ValidId`s, enums, fixed literals, `accepted`=true, pre/post null pair, CJSON-1 bytes.
7. **Binding checks:** recompute `Hk`/`Hr`/`Hw`/`Hp`; R/Wc/P references equal recomputed values; duplicated fields equal K; cross-record consistency.
8. **Prohibited claims:** reviewer/owner/administrator identity or act, provenance, custody, publication order, freshness, eligibility, invocation, positive validation.
9. **P1 reuse:** `CanonicalJson`, `FrozenSchemaRules`, `FrozenContractVerifier.TryVerifyCatalog`; no new canonicalization; divergence-control obligation retained.
10. **Positive fixtures:** hand-written canonical K/R/Wc/P chains (B2/pre and B3/post variants), digests computed outside the P2 encoder.
11. **Negative fixtures:** the §13 matrix, including raw-byte malformed forms and rehashed self-consistent semantic negatives.
12. **Independent qualification:** separate `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-Q1` with reviewer-owned vectors/chains and adversarial authority-boundary controls; producer tests alone are insufficient.
13. **Reachability:** Debug and Release direct-call absence evidence plus positive control, mirroring P1; a first consumer requires separate authorization.
14. **RELEASE:** unchanged verifier PASS and expected Debug-as-production FAIL.
15. **P4:** conditional P4-NEUTRAL with evidence; stop if any production-reachable authority path appears.
16. **Deferred responsibilities:** as §17.
17. **Stop conditions:** ambiguity in an accepted R/Wc/P field or binding; need to modify P1/F02/`eng`; need for protected acquisition or issuer designation; any public API or authority factory; inability to demonstrate reachability or RELEASE preservation.
18. **Required owner acceptance:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-OWNER-ACCEPTANCE-01` after P2-Q1.
19. **Excluded downstream:** E-3 through E-7, Slice N, catalog approval, protected roots, live invocation, positive validation, protected execution.

Subdivision option (not required): the owner may authorize only **P2-A** (R/Wc structural and binding checks) first and defer **P2-B** (P content structural verification), if the owner judges the P-content boundary should receive a separate evidence cycle. The contractual analysis shows both are the same A/B category, so the assessment's primary recommendation is the single bounded package.

## 20. Formal assessment disposition

**ENTRY READY WITH QUALIFICATIONS.**

The accepted frozen R/Wc/P contracts, the D1 approval governance, and the accepted P1 core are sufficient to authorize a bounded, internal, unprivileged, production-unreachable structural and content-binding verification package. The qualifications are explicit and non-blocking for entry: protected acquisition, issuer authenticity, publication order, freshness, live invocation, and positive validation authority remain outside P2 and owned by E-5/E-6, D1 governance, E-4/E-7, and Slice N; P2 evidence prerequisites include its own direct-call absence and RELEASE/P4 verification; and the P-content result contract must carry strict non-authority naming and documentation.

This assessment does not authorize P2 implementation and does not issue the owner entry decision.

## 21. Recommended owner decision

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-OWNER-01`** — the owner must separately decide: (1) whether to accept this assessment; (2) whether the frozen R/Wc/P contracts are complete; (3) whether P2 may reuse P1; (4) whether P structural checking is included; (5) whether P2 must be subdivided (P2-A/P2-B); (6) the exact permitted source and test surfaces; (7) whether trusted acquisition is excluded; (8) the non-authoritative output contract and naming restrictions; (9) required positive and negative controls; (10) required independent qualification; (11) RELEASE and P4 conditions; (12) E-1 consumer restrictions; (13) retained P0/D1/P1 qualifications; (14) exact forbidden authority transitions; (15) whether exactly one first P2 implementation package may be authorized.

No implementation authorization arises automatically from this favorable assessment. E-2-P2, E-3 through E-7, Slice N, positive validation, and protected execution remain unauthorized. This assessment stops here: it does not execute the P2 owner decision, implement P2, provision protected roots, issue approvals, or activate anything.


