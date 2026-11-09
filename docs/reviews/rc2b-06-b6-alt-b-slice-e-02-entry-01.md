# ALT-B Slice E-2 entry assessment — frozen-schema provenance and validation-catalog approval

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-01`

Date: 2026-11-09 (Asia/Manila). Reviewer: DeepSeek. Role: independent documentation and source inspection; no implementation, no authority construction, no owner entry decision.

**Formal entry disposition: RECONCILIATION REQUIRED.**

The E-1 prerequisite is conditionally satisfied: E-1 is accepted with qualifications, F-01/F-02/F-03 are formally closed with qualifications, and the P4 refusal-only coverage remains accepted. The K/M/F/S roles and the acyclic catalog approval graph are reconstructible from the accepted records and the locally available frozen contract. The frozen external inputs are locally present and their SHA-256 values match the hashes recorded by the committed acceptances. No contradiction or security-invariant failure was found.

E-2 is nevertheless not ready for an entry decision. **Q-E1** (an authoritative in-repository frozen-schema provenance record) and **Q-E2** (an owner decision for the validation-catalog approval chain) are express E-2 entry prerequisites and both remain unsatisfied in-repository. Proceeding now would either invent schema or approval authority or treat external unpublished bytes as if they were adopted Gagamba policy. The required next step is reconciliation, not implementation and not an owner authorization of E-2.

This assessment authorizes nothing. It does not implement E-2, construct validation authority, adopt a schema, approve a catalog, wire an E-1 consumer, or begin E-3 through E-7 or Slice N.

---

## 1. Commission, scope, and decision boundary

This is a documentation-only entry assessment for **E-2: protected K/M/F/S readers and case policy, refusal-only**. It answers one question: may E-2 entry proceed on the current committed evidence, or must specific prerequisites be reconciled first?

The review scope is deliberately narrow:

- It inspects committed records and source at the exact baseline in §2.
- It verifies that the locally available external frozen inputs match the hashes recorded by committed acceptances.
- It reconstructs the K/M/F/S role model and the catalog approval graph without expanding, renaming, or inventing schema semantics.
- It classifies the E-1 integrity evidence against the F-02 future-consumer gate and the F-01 fail-closed obligation.
- It records the E-2 entry prerequisites and the smallest defensible reconciliation packages.

Out of scope, and not performed or authorized:

- E-2 implementation, protected K/M/F/S readers, case-policy execution, or protected-store provisioning.
- Any schema, catalog, fixture, policy, action, approval, trust-anchor, capability, or authority construction.
- Any production consumer of the E-1 projection, any `OwnerAuthorityContext.Validation` factory, or any positive validation path.
- E-3 through E-7, Slice N, driver work, GAVA1, v3 validation-pre, ordering changes, or activation.
- Public API expansion, package installation, network access, fetch/pull/push, external models, or cloud verification.
- An owner entry decision. This record is an assessment for a later owner decision, not that decision.

The permitted dispositions for this commission are `ENTRY SUPPORTABLE`, `ENTRY SUPPORTABLE WITH QUALIFICATIONS`, `RECONCILIATION REQUIRED`, and `ENTRY BLOCKED`. Exactly one is issued in §18.

---

## 2. Verified baseline and preservation

The baseline was verified before any edit.

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| Starting HEAD | `e86254e60fb3ded770156519e1d3e15e73bf36fc` |
| Starting tree | `69a18e8c3a1e199802f4cfc1906de2a30e3a7928` |
| Worktrees | exactly one canonical checkout |
| Tracked / index state | clean; no staged or modified tracked path |
| Historical untracked paths | exactly 44 |
| Historical untracked content comparison | 44 of 44 paths present; per-file SHA-256 values match `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation inventory digest — sorted path/hash lines | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation inventory digest — compact JSON path/hash map | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 14 commits ahead of `origin/main`; no push |
| Network / package / cloud | none used |

The 44 historical untracked paths are a hard preservation constraint. They were not cleaned, deleted, rewritten, or staged. The new assessment and evidence files are additive documentation paths; they are not modifications of the historical untracked set.

No branch, linked worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.

---

## 3. Governing records and precedence

The assessment uses the following committed records as authority for their stated decisions. Hashes are recorded in `rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/record-hashes.txt`.

| Record | What it establishes here |
|---|---|
| [Slice E entry review](rc2b-06-b6-alt-b-slice-e-entry-01.md) | Slice E architecture, K/M/F/S role labels, P4 interaction, and the original E-2 reconciliation prerequisites |
| [Slice E entry owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) | Bounded E-1 authorization; Q-E1 through Q-E5; E-2 is planning-only until frozen provenance and catalog-approval decisions exist |
| [E-1 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) | E-1 accepted with qualifications at the exact implementation and qualification revisions |
| [F-01 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) | F-01 closed with qualifications; future-consumer fail-closed obligation |
| [F-02 parity owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | F-02 closed with qualifications; mandatory ten-point future production-integration gate |
| [F-03 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) | F-03 closed with qualifications; PowerShell 5.1 scope only |
| [P4 coverage owner acceptance](../decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | Refusal-only P4 coverage, preserved residuals, `NATIVE-PENDING`, 331 unqualified native skips |
| [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | Accepted prepublication selection, phase separation, corrected observation-to-effect residual, validation-only ordering semantics |
| [Alternative B implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | Atomic Slice E dependency set and the §3.5 rule that positive validation cannot be partially exposed |
| [Mechanism B owner acceptance](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | Exact 15-file frozen submission, its recheck manifest hash, and Q01–Q10 |
| [F02 trust-store owner decision](../decisions/rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | Administrative approval model for `/etc/gagamba/accepted-qualifications/<qualificationId>.json` only |
| [Installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md) | Gates A–D and the integrity-versus-authorization separation |
| [Remediation evidence standard](../remediation-evidence-standard.md) | Durable evidence expectations for security-sensitive work; used as a model, not as a claim that this documentation assessment is a remediation |

Precedence used:

1. A later owner decision governs the semantics it expressly accepts, including the historical PENDING captions it supersedes.
2. A closure decision preserves every qualification it does not expressly remove. Later work may not silently widen an earlier closure.
3. External accepted-by-hash inputs prove byte identity to the accepted object. They do not become in-repository authority merely because they are locally present.
4. An earlier review's recommendation is not an authorization. Only an owner decision can open a package.

---

## 4. E-1 prerequisite status and preserved closures

The E-1 prerequisite is conditionally satisfied. The accepted E-1 boundary is narrow and remains binding.

| Prerequisite | Status | Exact boundary retained |
|---|---|---|
| E-1 non-forgeable prepublication integrity projection | **ACCEPTED WITH QUALIFICATIONS** | Accepted implementation `2259fce59b44b6af6448a38685a0635d560d0900` (tree `77610b49ecae5398d633f1db43d4f01604694ee1`); independent qualification `db832452474c378d2c1ed8f515325c23f9ee7aab` (tree `ccd5c1f0d68a8b21416cf683fbd8eca20d2df813`). Refusal-only, production-unreachable. |
| F-01 semantic integrity | **CLOSED WITH QUALIFICATIONS** | Reviewed implementation `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f` (tree `7edfedd6badebda029ce1e3371445d973c6fd82b`). Owner/recovery role hashes and runtime inventory are bound. The future-consumer fail-closed obligation remains. |
| F-02 verification-path divergence | **CLOSED WITH QUALIFICATIONS** | Implementation `4c5a42f0fecff90dcb30088ca0203956e7a80032` (tree `ba021734bc42c43fd1f0c6cc8a52573707a6a972`); independent qualification `d221b24be3d19c775c5118e419b2e88769fea2ce`. Ten structural controls accepted; behavioral equivalence and branch dominance are not certified. |
| F-03 verification-harness capture | **CLOSED WITH QUALIFICATIONS** | Script `52037fa5738609b1469089835380a0cc94e047f1` (tree `ce921c0c0013f7baceac6c898f758ca1587f5ec4`), Windows PowerShell 5.1 only. PowerShell 7 is unexecuted. |
| P4 refusal-only coverage | **ACCEPTED WITH QUALIFICATIONS** | 21 COVERED, 5 AUTHORIZED CONTINUATION, 3 LEGACY / TEST-ONLY; zero SLICE E DEPENDENCY, zero GAP, zero INCONCLUSIVE. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, and 331 native skips preserved. |

The E-1 acceptance is not a positive validation capability. Its own text says it must not imply that validation authority is constructible, that a trusted validation catalog exists, that K/M/F/S approval is implemented, that GAVA1 or v3-pre exists, that atomic R/O/C validation ordering exists, or that E-2 through E-7 are authorized.

Preserved E-1 limitations relevant to E-2:

- The runtime loaded-assembly "verification-to-use" check is not performed by E-1 and remains a deferred obligation.
- Non-forgeability is claimed against ordinary callers and untrusted evidence, not arbitrary privileged code inside the trusted process; reflection is outside the accepted threat model.
- E-1 is production-unreachable: `VerifyActiveIntegrity` is called only by `PrepublicationIntegrityProjection.Verify`, which has no production caller; `FromEvidence` is private; `ForTest` is excluded from RELEASE.
- E-1 integrity evidence is not qualification, catalog approval, case authorization, capability issuance, or execution authority.

---

## 5. K/M/F/S role map without invented semantics

The accepted design uses **K**, **M**, **F**, and **S** as role labels. They are not expanded into new acronyms or new field meanings here. The exact field-level schemas live in the frozen external contract; this assessment records their roles and the in-repository gap.

| Symbol | Role as used by the accepted design | Exact schema location | In-repository state |
|---|---|---|---|
| **K** | Protected catalog binding the exact case, phase, topology, candidate identity, host/platform identity, qualification association, closure hashes, session bound, and release flag. | External `case-policy-and-action-contract.md` §1; accepted through the F02 15-file manifest. | No in-repository K schema or K reader. |
| **M** | Protected fixture manifest. For B2 the object list is empty; for B3 it is exactly the four fixed roles and their exact bytes/metadata. | External contract §3. | No in-repository M schema or M reader. |
| **F** | Protected fixture policy. It carries the B2/B3 variant, exact host paths, copy/invocation/sandbox/resource policy, and the release/session bounds. | External contract §3. | No in-repository F schema or F reader. |
| **S** | Protected finite action script. It selects exactly one of three fixed templates and permits no arbitrary command, expression, loop, retry, or callback. | External contract §3. | No in-repository S schema or S reader. |
| **P** | Protected catalog-eligibility approval written by the root administrator at `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`. It grants eligibility, not a live invocation. | External contract §1. | No in-repository P schema, root, or approval decision. |
| **R** | Detached independent catalog review at `/etc/gagamba/validation-catalog-evidence/<Hr>.json`. | External contract §1. | No in-repository R representation. |
| **Wc** | Detached owner catalog acceptance at `/etc/gagamba/validation-catalog-evidence/<Hw>.json`. | External contract §1. | No in-repository Wc representation. |
| **Hk / Hr / Hw / Hp** | SHA-256 over the exact canonical bytes of K / R / Wc / P, respectively. | External contract §1; canonical encoding in `final-acceptance-hash-contract.md`. | No in-repository canonical schema or verifier for this chain. |

Role boundaries that must not be collapsed:

- K is not P. K contains no review, owner-decision, acceptance-receipt, or self-hash field.
- P is eligibility, not a live capability. A fresh live challenge/response and one-response latch remain necessary.
- R and Wc are detached. They do not become fields inside K, and no edge returns from R/Wc/P into K/F/S.
- The presence of a hash does not establish human provenance. Provenance is administrative and must be independently verified by the root administrator.
- The labels V (catalog reviewer), W (owner accepting the exact catalog bytes), and A (root administrator provisioning P) in the external catalog contract are not silently mapped onto Slice E's A/L/C/R/O roles. A separate owner decision must establish that mapping and authority.

The exact external schemas are not reproduced in this report as adopted authority. Reproducing selected fields for review is not a substitute for the Q-E1 reconciliation record.

---

## 6. Acyclic catalog approval graph

The frozen contract specifies this exact dependency direction:

```text
M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability
```

The graph is acyclic:

- M is constructed first; F references hash(M); S references hash(M) and hash(F); K references hash(M), hash(F), and hash(S).
- K contains no reference to R, Wc, or P.
- R reviews completed K and its transitive bytes; R contains no owner acceptance hash.
- Wc accepts the exact K and R bytes; Wc contains no P hash.
- P binds Hk, Hr, and Hw and is published last under the authority/install locks.
- The live challenge binds Hr, Hw, and Hp; the live response and then the one-use capability follow.
- No edge returns from P to K, from Wc to K, or from R to K.

The graph is reconstructible from the external contract §1. It is internally coherent and contains no hash fixed point: K is hashed before R/Wc/P exist, and R/Wc/P are detached records rather than fields that must be hashed back into K.

The graph is **not** yet an in-repository Gagamba policy. No committed decision adopts the V/W/A roles, the R/Wc representations, the `/etc/gagamba/accepted-validation-catalogs` store, or the publication/withdrawal semantics for this chain. That is Q-E2, not a defect in the graph.

---

## 7. Frozen-schema provenance and external-input verification

The external frozen inputs are locally present under `Solo/`. Their SHA-256 values match the hashes recorded by the committed acceptances.

### 7.1 Inputs accepted through the F02 15-file submission

The F02 owner acceptance binds the frozen recheck manifest `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` and the independent recheck `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6`. The relevant exact files verify as:

| External input | Size | SHA-256 | Acceptance binding |
|---|---:|---|---|
| `case-policy-and-action-contract.md` | 30,362 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | Listed in the F02 recheck manifest and the package `SHA256SUMS.txt` |
| `actor-and-descriptor-binding.md` | 18,757 | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | Listed in the F02 recheck manifest |
| `mechanism-b-session-capability.md` | 14,325 | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | Listed in the F02 recheck manifest |
| `final-acceptance-hash-contract.md` | 10,989 | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` | Listed in the F02 recheck manifest |
| `independent-recheck.md` | 4,470 | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | Bound directly by F02 §2 |
| `recheck-input-SHA256SUMS.txt` | 1,463 | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | Bound directly by F02 §2 |

The F02 recheck manifest contains the exact hashes above for the four schema-bearing files. The outer package `SHA256SUMS.txt` also lists the same hashes and the recheck manifest. This establishes byte identity to the object the owner accepted. It does not make the external bytes an in-repository schema.

### 7.2 Inputs accepted through the Alternative B decision

Alternative B §4 records these exact hashes, and the local files verify as:

| External input | Size | SHA-256 | Acceptance binding |
|---|---:|---|---|
| `validation-bootstrap-compatibility.md` | 15,291 | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | Alternative B §4 key reviewed inputs |
| `validation-bootstrap-assessment.md` | 3,501 | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | Alternative B §4 key reviewed inputs |
| `implementation-and-test-handoff.md` | 18,650 | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | Alternative B §4 key reviewed inputs |

These documents supply the accepted validation contract context, the K/M/F/S + A/L/C/R/O/D notation, GAVA1, v3 validation-pre, protected roots, release correlation, and the implementation/test handoff. Their historical PENDING captions remain untouched; the later canonical decisions establish the accepted semantics. They remain external inputs, not in-repository authority.

### 7.3 What the hashes prove and do not prove

Proved:

- The local external bytes are byte-identical to the objects whose hashes are recorded in committed acceptances.
- The F02 15-file manifest and the Alternative B key-input list are internally consistent with the local files.
- The schema-bearing text used to reconstruct K/M/F/S/R/Wc/P is the same text accepted through those records.

Not proved:

- That an in-repository file adopts those bytes as Gagamba's normative schema.
- That the external package is a published or durable artifact independent of the local `Solo/` tree.
- That a reviewer can reconstruct every field from the repository alone.
- That any catalog, approval, root, or capability exists or is qualified.

The material provenance gap is therefore **repository authority**, not byte identity. The exact bytes are available locally and hash-match; the repository still lacks an authoritative frozen-schema record.

---

## 8. Validation-catalog approval chain and the F02 boundary

The external catalog contract specifies a complete administrative chain:

1. **V** independently reviews completed K and its transitive exact bytes and issues detached review R.
2. **W** accepts those exact bytes and issues detached owner receipt Wc.
3. **A** independently verifies the actual V/W provenance, recomputes Hk/Hr/Hw, and writes protected approval P at `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`.
4. R and Wc are published first at `/etc/gagamba/validation-catalog-evidence/<Hr>.json` and `<Hw>.json`, with protected metadata and no-replace/file-and-directory fsync.
5. P is published last under the authority/install locks.
6. L later opens P by selected catalog digest, hashes it to Hp, opens the named R/Wc, hashes them against P, strictly parses and cross-compares every binding, and verifies K/M/F/S and code/host inputs.
7. The live challenge/response binds Hr, Hw, and Hp; P alone cannot issue or reissue a capability.
8. Withdrawal/replacement is serialized with issuance/consumption; a different approval byte set for the same Hk is not silently adopted mid-attempt.

The exact P, R, and Wc schemas and the exact Hk/Hr/Hw/Hp bindings are in the external contract §1. The important boundary for E-2 is:

- P is an **eligibility** approval, not authorization for a live invocation.
- P is not a producer-built file. A must independently verify administrative provenance; merely observing K/R/Wc candidates cannot generate P.
- P, R, and Wc are protected administrative records. Unprotected review text has no authority.
- The chain does not reuse the final-admission chain or the F02 qualification store.

The F02 trust-store decision is **not** a decision for this chain. It establishes an administrative approval model for `/etc/gagamba/accepted-qualifications/<qualificationId>.json`. Q-E2 expressly says F02's authority does not automatically extend to validation catalogs. E-2 may reuse the accepted administrative *primitive* only if a later owner decision adopts the exact catalog representation, roots, roles, publication rules, and withdrawal semantics.

The in-repository position is:

- No committed owner decision establishes who V, W, or A are for validation catalogs.
- No committed decision adopts the R/Wc/P representations.
- No committed decision adopts the `/etc/gagamba/accepted-validation-catalogs` store or the `/etc/gagamba/validation-catalog-evidence` store.
- No committed decision adopts the approval publication, no-replace, fsync, lock-order, withdrawal, or reapproval rules for this chain.
- No in-repository schema file contains the exact catalog-chain field names. The only in-repository mentions of `accepted-validation-catalogs` are the gap statements in the E-1 entry decision and the Slice E entry review.

This is a specified-but-unadopted chain, not an underspecified one. The reconciliation is an owner adoption decision, not a new cryptographic design.

---

## 9. Current source and authority state

The source state confirms that E-2 is not implemented and that E-1 remains unreachable.

### 9.1 E-1 projection and call graph

- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs` defines the internal sealed projection with a private constructor, get-only properties, and null `QualificationId` / `QualificationRecordSha256`.
- `Verify()` is the only RELEASE construction route; it calls `InstalledFileSystem.VerifyActiveIntegrity()` and then private `FromEvidence`.
- `VerifyActiveIntegrity()` is defined in `InstalledFileSystem.Prepublication.cs` and calls `InstalledPayload.CheckHost`.
- `ForTest` is Debug-only and is named as a forbidden type in the RELEASE production verifier.
- `PrepublicationIntegrityProjection.Verify` has no production reference in `src/` or `eng/`.
- `git diff --stat 4c5a42f0fecff90dcb30088ca0203956e7a80032 HEAD -- src tests eng` is empty. The reviewed F-02 implementation remains the current source state.

### 9.2 Authority and parser state

- `OwnerAuthorityContext.Validation` is an `internal sealed class` with a private constructor and no factory. Its own comment says the atomic validation-bootstrap slice must add the only factory together with the complete issuer/context/policy checks.
- `RecoveryProtocol.RecoveryConfiguration.Validate` admits only `Version` 1 or 2. There is no v3 validation-pre parser.
- There is no GAVA1 exchange, no validation catalog store, no K/M/F/S reader, no `ValidatedPreSession` type, and no validation-catalog approval implementation in `src/` or `eng/`.
- Test references to `OwnerAuthorityContext.Validation` are type-level checks; they do not construct validation authority.
- The E-1 type `InstalledIntegrityEvidence` is an internal E-1 evidence record, not the frozen v3 `InstalledIntegrity` type. Its existence must not be mistaken for v3-pre implementation.

### 9.3 What this means for E-2

E-2 would be the first package that reads protected K/M/F/S and applies an approved case policy. Even a refusal-only E-2 could add:

- new protected-store acquisition paths;
- new parsing and canonicalization surfaces;
- new refusal classifications;
- new production callers or sinks if any reader is wired into a lifecycle path;
- new authority claims if P/R/Wc are treated as approval.

The current source has none of those. That is the safe starting condition, not evidence that the future package is safe. E-2 entry must therefore be gated by the schema and approval prerequisites, and its eventual implementation must be independently reviewed against the exact source revision, not against this assessment.

---

## 10. E-1 integrity evidence versus validation authority — the F-02 gate

E-1 produces **verified prepublication integrity evidence**. It does not produce validation authority.

The distinction is binding:

| Layer | What it establishes | What it does not establish |
|---|---|---|
| E-1 projection | Fixed-root active selection, manifest/payload closure, platform/host checks, loaded-role integrity evidence, null qualification association | Qualification, catalog approval, case policy, capability, session authorization, execution, publication |
| K/M/F/S + P | Exact approved case data and catalog eligibility for a bounded validation invocation | A live invocation, a capability, a process launch, or a RELEASE |
| GAVA1 + live challenge/response | Authenticated context and one-use capability for the original session | General admission, postpublication authority, or production activation |
| S2 Qualified + final admission | Installed qualification and final-admission authority for the normal path | Validation authority or a positive validation result |

The F-02 closure is a **binding condition**. Before the first production consumer of the E-1 projection, a fresh security review is required covering at least:

1. `VerifyActive()` versus `VerifyActiveIntegrity()` current state.
2. Shared integrity invariants.
3. Conditional bypass and branch-dominance risks (the Q1-F01 class).
4. Call arguments and helper semantics (Q1-F02).
5. F-01 semantic integrity relationships.
6. Fail-closed treatment of every integrity-verification exception and failure mode (the F-01 consumer-integration obligation).
7. Qualified versus prepublication authority separation.
8. Runtime loaded-assembly verification-to-use.
9. Publication and artifact identity; rebuild and reapproval (Q1-F05).
10. P4 effects and applicable protected native qualification.

The gate is triggered by the first production consumer, not by the existence of the projection type. E-2 must not treat E-1 evidence as a substitute for K/M/F/S approval or for the later live authorization. E-2 must not construct a validation context, mint a capability, or convert the projection into a `LoadedInstallation`, `Qualified` result, or general-admission grant.

The F-01 future-consumer obligation is also preserved: before any production consumer is authorized, every integrity-verification failure mode must fail closed with no fallback, downgrade, partial authorization, or continuation.

F-03 is a verification-harness closure, not a production-verifier qualification. Its PowerShell 5.1 evidence does not qualify PowerShell 7, native custody, or a protected positive.

Classification for this assessment:

- E-1 evidence is **supporting integrity evidence**.
- E-1 evidence is **not** catalog approval.
- E-1 evidence is **not** validation authority.
- E-1 evidence is **not** a qualification record.
- E-1 evidence is **not** a positive validation result.

---

## 11. E-2 security invariants and refusal obligations

Any future E-2 implementation must preserve the following invariants. This assessment records them; it does not implement them.

1. **No partial positive exposure.** Positive validation is atomic under plan §3.5. E-2 may be a refusal-only preparatory package, but it may not make any positive validation path reachable.
2. **No invented authority.** A missing K/M/F/S/P/R/Wc input, an unavailable protected store, an absent approval, or an unsupported schema must refuse. There is no fallback, default, `latest`, search-by-name, user path, environment override, or producer-supplied substitute.
3. **Protected acquisition.** K/M/F/S are opened descriptor-relatively through trusted directory chains, pinned, checked for exact uid/gid/mode/link/size, read into private bounded memory, hashed, strictly decoded and re-encoded, and reacquired/checked unchanged. Metadata descriptors are CLOEXEC and closed after the bounded read.
4. **Exact binding.** K binds the exact case, phase, topology, candidate/source/profile/host identity, qualification association, closure hashes, session bound, and release flag. Duplicated values in M/F/S must equal K, never override it.
5. **No catalog-as-code.** S is a finite template, not a command language. No script text is evaluated; no arbitrary argv, expression, substitution, loop, retry, callback, or extension field is admitted.
6. **Approval is administrative.** P is written by the root administrator after independent verification of V/W provenance. A producer-built K/R/Wc set without protected P refuses. P is not a self-hash and does not grant a live invocation.
7. **Live authorization remains separate.** P grants eligibility only. The fresh challenge, exact response, one-response latch, and one-use capability remain mandatory. P alone cannot issue or reissue.
8. **Phase separation.** A prepublication session may not upgrade to postpublication authority. A phase, receipt, or active-selection change refuses. A new postpublication invocation requires independent approval.
9. **No downgrade.** Rejected validation or integrity evidence must not fall back to legacy, qualified, or general-admission execution authority.
10. **Preserved stopping and cleanup.** Existing stop service, reaping, retry/reconciliation, original deadlines, sole owner RELEASE writer, and at-most-one physical attempt remain unchanged. Missing or uncertain cleanup evidence remains `Unknown`, never a fabricated success.
11. **P4 preservation.** Any new production caller of a protected sink, any E-1 consumer, or any L/C/R/O wiring invalidates the corresponding domination claim and requires a fresh P4 audit before the first protected effect.
12. **Evidence honesty.** Synthetic fixtures, mocks, upstream CI, and skipped native cases do not qualify a protected backend. Missing or skipped required cases block qualification.

The observation-to-effect residual is preserved, not waived: a root change after the last relevant stable observation may escape detection, even before the verifier returns. No atomic root-mutation/effect lock or retroactive cancellation is promised.

---

## 12. P4 classification and protected-native boundary

The E-2 entry assessment itself is documentation-only. Its P4 classification is **P4-NEUTRAL**.

A future E-2 implementation is only **provisionally neutral** if it adds no production caller, no protected sink, no E-1 consumer, and no lifecycle wiring. It must be re-audited at its exact revision. Any of the following requires **P4 RE-AUDIT REQUIRED** before the first protected effect:

- a production reader of K/M/F/S;
- a production caller of `VerifyActiveIntegrity` or `PrepublicationIntegrityProjection.Verify`;
- any L/C/R/O validation wiring;
- any new `OwnerAuthorityContext.Validation` construction;
- any change to the first protected effect or the caller graph.

Preserved P4 qualifications:

- **F-R1 / O-R1:** S10/S11 domination relies on the audited caller graph and upstream F01 domination. Any new production caller of the generic callees requires a fresh sink/authority audit.
- **O-R2:** S28 remains test-only. `internal` visibility is not authority.
- **O-R3:** refusal-reason typing under the constructor-recheck race remains open.
- 5 AUTHORIZED CONTINUATION and 3 LEGACY / TEST-ONLY rows remain as accepted; they are not reclassified as unconditional COVERED.
- `NATIVE-PENDING` rows remain pending.
- All **331 unqualified native skips** remain not-passes. They are not coverage, not qualification, and not a protected native positive.
- Slice N remains a separate authorized milestone.

E-2 cannot claim native qualification from deterministic tests, synthetic fixtures, mocked providers, or outer sandbox denial. Protected-root readability, actual loaded closures, privilege drop, SCM/pidfd custody, GAVA1, native RELEASE, stop/reap, and cleanup remain Slice N obligations.

---

## 13. E-2 package boundary and the atomic Slice E dependency

E-2 is part of the Slice E atomic dependency set. It is not independently enableable.

The implementation plan §3.5 requires all of the following to be complete together before any exposed installed validation path exists:

- independent live root authorizer A, fixed reviewed launcher L, and protected catalog/approval chain;
- permanent privilege drop, original process custody, pidfds, and SCM credentials;
- protected K/M/F/S acquisition, exact case policy, and finite fixture;
- GAVA1 authenticated direct-context exchange;
- prepublication full integrity verifier and v3 pre bootstrap parser;
- original session, execution, lease, preparation, and correlation identities;
- all pre-effect sink checks;
- owner policy and release-correlation recomputation;
- owner-side phase-specific final pre-write generation gate;
- stop service at every barrier and original deadline handling;
- exact driver dispatch, production closure checks, and test-instrumentation exclusion;
- atomic recovery/owner/controller ordering for the validation-only early direct authentication sequence.

E-2's narrow package content, per the Slice E entry review §8, is protected K/M/F/S readers plus case policy, refusal-only. It does not include GAVA1, v3-pre, the launcher, custody, the validation context factory, the ordering amendment, the driver, or positive enablement.

E-2 entry prerequisites:

1. E-1 accepted with qualifications, with F-01/F-02/F-03 closures preserved.
2. Q-E1 frozen-schema provenance reconciliation.
3. Q-E2 validation-catalog approval-chain owner decision.
4. Confirmation that the E-2 revision is P4-neutral if it adds no caller/sink; otherwise a P4 re-audit.
5. A later owner entry decision that accepts the reconciliation and authorizes the bounded package.

This assessment satisfies only the evidentiary review portion of item 1/2/3 analysis. It does not satisfy Q-E1 or Q-E2, and it does not issue the owner entry decision.

---

## 14. Q-E1 — frozen-schema provenance reconciliation

**Status: UNSATISFIED IN-REPOSITORY.**

The E-1 entry owner decision states Q-E1 exactly:

> Authoritative K/M/F/S schemas, GAVA1 header, v3 validation-pre layouts and release correlation remain supported by external accepted packages and historical hashes, not a complete in-repository frozen-schema record. E-1 must not guess missing fields. Commission a separately reviewable provenance reconciliation that binds exact bytes, originating acceptances and limitations and commits the authoritative record before schema-dependent packages.

The external inputs are locally present and hash-match. That is necessary but not sufficient. The repository still lacks a committed record that:

- identifies each frozen input and its exact SHA-256;
- identifies the originating acceptance for each input, including the F02 15-file manifest and the Alternative B key-input list;
- states which semantics are normative and which historical captions are superseded;
- records limitations, non-claims, and unresolved fields;
- states whether the exact bytes are copied into the repository or remain external;
- defines the review and owner-acceptance process for the record itself;
- gives a reviewer a repository-only path to reconstruct the accepted K/M/F/S/R/Wc/P contract for E-2.

A hash list alone is not a schema record. A local external file is not an in-repository authority. A review that cites external bytes without a committed provenance record repeats the gap.

Minimum P0 reconciliation content:

1. **Revision and scope.** Exact source revision, date, reviewer, and the E-2 subset covered. The full Q-E1 set includes GAVA1, v3-pre, and release correlation; P0 may stage the K/M/F/S/R/Wc/P subset first, but it must state what remains for E-3/E-4.
2. **Exact inventory.** File name, size, SHA-256, and originating acceptance for every frozen input. The inventory must include the F02 recheck manifest hash and the Alternative B key-input hashes.
3. **Normative extraction or exact copies.** Either commit the authoritative schema text (if permitted) or commit a precise, reviewable extraction that identifies every field, type, bound, canonical encoding, and cross-field equality needed by E-2. If exact copies cannot be committed, record why and the consequence for schema-dependent work.
4. **Provenance chain.** Identify the owner decisions that accept each input, the historical PENDING captions, and the later decision that supersedes them. Do not rewrite historical bytes.
5. **Limitations.** Record what the external acceptance does not establish: no catalog instance, no protected store, no native qualification, no positive validation, no production activation.
6. **Conflict and change rule.** State how a later external revision is detected, who may accept it, and why an unreviewed hash change refuses.
7. **Verification commands.** Provide reproducible hash commands and expected output.
8. **Independent review and owner acceptance.** The record must be separately reviewable and accepted by the owner before schema-dependent E-2 work.

P0 is documentation-only. It must not add production/test code, a catalog reader, a trust anchor, a protected store, or an E-1 consumer.

---

## 15. Q-E2 — validation-catalog approval-chain decision

**Status: UNSATISFIED IN-REPOSITORY.**

The E-1 entry owner decision states Q-E2 exactly:

> `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` has no accepted owner decision defining its approval chain. F02 covers `/etc/gagamba/accepted-qualifications/<qualificationId>.json`; its authority does not automatically extend to validation catalogs. A later decision must establish who approves, the detached review/acceptance chain and exact accepted representation. No new approval schema is selected here.

The external contract supplies a complete proposed chain. The repository does not adopt it. The owner decision must establish at least:

1. **Role authority.** Who may act as V (catalog reviewer), W (owner accepting exact catalog bytes), and A (root administrator provisioning P). Whether these are the same or different human/administrative roles as the existing F02/qualification chain.
2. **Exact representations.** The exact R, Wc, and P schemas, their canonical encoding, their field sets, and their cross-field equality rules. No generic `ownerDecisionSha256` or `independentReviewSha256` aliases.
3. **Protected roots and custody.** `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` and `/etc/gagamba/validation-catalog-evidence/<Hr|Hw>.json`, with exact metadata, no-replace publication, file and directory fsync, and lock order.
4. **Publication order and failure semantics.** R/Wc first, P last; no-replace; crash/retry behavior; quarantine behavior; no producer-built P; no silent replacement of a different approval byte set for the same Hk.
5. **Live binding.** The fresh challenge/response fields, one-response latch, capability binding to Hr/Hw/Hp, and the rule that P alone cannot issue or reissue.
6. **Withdrawal and reapproval.** Removal-first refusal for new issuance, preservation of an already-issued session, and the requirement for independent explicit administrative action for a new invocation.
7. **Separation from F02.** An express statement that the F02 qualification-store decision is not extended by analogy and that the final-admission chain is not reused.
8. **Threat model and non-claims.** Trusted-root bounds, malicious-root exclusion, no standing approval, no unattended authority, no cross-host reuse.
9. **Owner acceptance.** A committed owner decision, not a review recommendation and not a producer-generated approval.

D1 must not implement the chain. It is an owner decision that adopts the exact administrative semantics; the implementation and independent qualification follow under a separate commission.

The fact that the external chain is complete is why this is reconciliation rather than an architectural block. The missing item is adoption, not design.

---

## 16. Risk register and explicit non-claims

| Risk | Consequence if not reconciled | Control |
|---|---|---|
| Treating hash acceptance as in-repository schema authority | E-2 implementation would cite unpublished external bytes as if they were adopted policy; reviewers could not reconstruct the schema from the repository | Q-E1 / P0 |
| Extending F02 by analogy | The qualification store's authority would silently become catalog authority without the required decision | Q-E2 / D1 |
| Using E-1 as validation authority | Integrity evidence would be promoted into qualification, catalog approval, or capability | §10 gate; E-1 acceptance boundary |
| Inventing missing fields or approval semantics | E-2 would create authority not accepted by the owner | No schema invention; refusal on unsupported input |
| Partial E-2 enablement | A refusal-only package could accidentally expose a positive path | Atomic plan §3.5; no consumer; P4 re-audit |
| P4 sink drift | New reader/wiring could invalidate S10/S11 domination or first-effect claims | F-R1/O-R1; re-audit requirement |
| Native skips treated as passes | Deterministic/synthetic evidence would be overstated as protected qualification | `NATIVE-PENDING`; 331 skips not passes |
| External package drift or unavailability | A later reviewer could not reconstruct the accepted bytes | P0 hash inventory and conflict rule |
| Stale or withdrawn approval | A previously eligible catalog could be treated as live authority | D1 withdrawal/reapproval and live challenge rules |
| Observation-to-effect residual | A root change before or after the last observation may escape detection | Preserve the residual; no stronger claim |

Explicit non-claims for this assessment:

- No build, test suite, native probe, protected acquisition, provisioning, or execution was performed.
- No external frozen byte was independently authenticated beyond SHA-256 comparison to committed hashes.
- No conclusion is drawn about native protected positive behavior, privilege drop, SCM/pidfd custody, GAVA1, v3-pre, or Slice N.
- No behavioral equivalence, branch dominance, or protected native parity is claimed for `VerifyActive` versus `VerifyActiveIntegrity`.
- No PowerShell 7 qualification is claimed.
- No positive validation, production activation, publication, or deployment readiness is claimed.
- No public API stability or implementation authorization is claimed.
- No schema, catalog, approval, trust anchor, or authority is created.

---

## 17. Proposed reconciliation packages E-2-P0 and E-2-D1

Two documentation-only packages are the smallest defensible next steps. They are separate because schema provenance and approval authority are different owner decisions.

### 17.1 E-2-P0 — frozen-schema provenance reconciliation

**Purpose:** create the authoritative in-repository record required by Q-E1 for the E-2 K/M/F/S/R/Wc/P subset, while preserving the broader GAVA1/v3-pre/release-correlation items for their own later reconciliation.

**Permitted scope:**

- a new `docs/evidence/` or `docs/reviews/` provenance record with exact hash inventory, acceptance chain, normative extraction or exact copies, limitations, conflict rule, and verification commands;
- additive references from the current-state docs;
- no production/test/verifier/harness source;
- no schema reader, catalog store, trust anchor, or authority construction.

**Exit criteria:**

- a reviewer can reconstruct the E-2 K/M/F/S/R/Wc/P contract from the repository without relying on an unpublished external file;
- every hash matches the committed acceptance;
- the record states what remains external and why;
- an independent review and owner acceptance are recorded;
- no schema behavior is implemented.

### 17.2 E-2-D1 — validation-catalog approval-chain owner decision

**Purpose:** adopt the exact V/W/A, R/Wc/P, roots, custody, publication, live-binding, and withdrawal semantics for `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`.

**Permitted scope:**

- a new owner decision under `docs/decisions/`;
- exact references to the P0 provenance record;
- no implementation, no catalog instance, no protected store, no approval record, no capability.

**Exit criteria:**

- the owner expressly adopts or declines the external chain;
- the F02 non-extension is recorded;
- the exact representations and roots are identified;
- the decision states its threat model and non-claims;
- no producer or implementation is authorized to create P.

### 17.3 Sequencing

```text
E-2 entry assessment (this record)
  -> E-2-P0 provenance reconciliation
  -> E-2-D1 catalog approval-chain owner decision
  -> a fresh E-2 entry owner decision
  -> bounded E-2 implementation under a separate commission
```

P0 should precede D1 because D1's exact representations need the provenance record's exact byte inventory and normative extraction. D1 may identify issues that send P0 back for correction; that loop is expected and must be resolved before the E-2 entry decision.

Neither package authorizes E-2 implementation. Neither package may be absorbed into E-2 implementation as an incidental dependency.

---

## 18. Formal entry disposition

**RECONCILIATION REQUIRED.**

The E-1 prerequisite is conditionally satisfied, the K/M/F/S roles and acyclic graph are reconstructible, the external frozen inputs are locally present and hash-match the committed acceptances, and no contradiction or security-invariant failure was found. Those are real positives and they distinguish this finding from a categorical block.

They are not enough for E-2 entry. Q-E1 and Q-E2 are express E-2 entry prerequisites. Q-E1 is unsatisfied because the repository has no authoritative frozen-schema record. Q-E2 is unsatisfied because no owner decision adopts the validation-catalog approval chain. E-2 is schema-dependent and catalog-approval-dependent. Proceeding now would either invent authority or rely on external unpublished bytes as adopted policy.

This disposition is not a conditional entry authorization: Q-E1 and Q-E2 are unmet gating prerequisites, not residual limits that can be carried into implementation. The correct next step is P0 and D1, followed by a fresh entry decision.

The disposition is also not a finding that reconciliation is impossible. The external chain is complete, the bytes are available locally, the hashes match, and the graph is acyclic. The missing items are repository authority and owner adoption. Those are reviewable and bounded.

Nothing in this disposition authorizes E-2 implementation, E-3 through E-7, Slice N, positive validation, an E-1 consumer, a validation-catalog store, a schema, a trust anchor, or production activation.

---

## 19. Recommended next owner commission and stop

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-OWNER-01`.

That commission should be limited to:

1. Accepting or rejecting this assessment and its `RECONCILIATION REQUIRED` disposition.
2. Authorizing **E-2-P0** as a documentation-only frozen-schema provenance reconciliation.
3. Authorizing **E-2-D1** as a separate documentation-only validation-catalog approval-chain owner decision.
4. Recording the sequencing, stop conditions, preservation requirements, and the rule that neither package authorizes E-2 implementation.
5. Requiring a fresh E-2 entry decision after P0 and D1 are accepted.

It should not authorize E-2 implementation, schema-dependent readers, protected stores, a validation context, an E-1 consumer, GAVA1, v3-pre, the launcher, custody, ordering changes, the driver, Slice N, positive validation, or production activation.

This assessment changed documentation only: this report and its evidence directory, plus additive current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md`, and `docs/work-queue.md`. Historical records remain unchanged. The 44 historical untracked paths and their bytes are preserved. One canonical worktree remains on `main`. No branch, extra worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.

**Stop after committing this entry assessment.** Do not begin the owner entry decision or E-2 implementation.
