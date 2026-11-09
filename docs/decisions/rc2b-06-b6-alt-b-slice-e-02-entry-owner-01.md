# ALT-B Slice E-2 entry owner decision — bounded reconciliation authorization

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-OWNER-01`

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push. This is a single-reviewer governance recording using committed evidence and locally available inputs. It is **not** independent multi-model qualification.

## 1. Verified starting baseline

The baseline was verified before any edit.

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| Starting HEAD | `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d` |
| Starting tree | `deedbff8cfa9ab8572ffc43874fedadab0a79df2` |
| Worktrees | exactly one canonical checkout: `C:/Users/Laszlos/source/repos/Gagamba a35ce10 [main]` |
| Tracked / index state | clean; both unstaged and staged diffs empty |
| Historical untracked paths | exactly 44 |
| Historical untracked content comparison | 44 of 44 paths present; per-file SHA-256 values match `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation inventory digest — sorted path/hash lines | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation inventory digest — compact JSON path/hash map | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 15 commits ahead of `origin/main`; no push |
| Entry-assessment commit | `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d`, tree `deedbff8cfa9ab8572ffc43874fedadab0a79df2`, message `docs: assess Slice E-2 entry as reconciliation required`; it is the current HEAD |
| Entry-assessment ancestry | `a35ce10` is an ancestor of HEAD (it is HEAD); its parent is the E-1 F-02 closure `e86254e60fb3ded770156519e1d3e15e73bf36fc` |
| E-1 decision ancestry | E-1 implementation `2259fce59b44b6af6448a38685a0635d560d0900`, E-1 independent qualification `db832452474c378d2c1ed8f515325c23f9ee7aab`, and F-01 remediation `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f` are ancestors of HEAD |
| Network / package / cloud | none used |
| Reconciliation / implementation | none performed |

Digest reproduction: sort the 44 `untracked-before.json` entries by `path`. The lines digest uses `path + TAB + lowercase SHA-256 + LF` for each entry, including the trailing LF, encoded as UTF-8. The map digest uses a compact JSON object with keys in the same sorted order and lowercase-hash values, encoded as UTF-8. Both digests were recomputed from the committed inventory at this baseline.

The expected baseline matches the commission exactly. The 44 historical untracked paths were preserved by content, not merely by path count. They were not cleaned, deleted, rewritten, staged, or committed. No branch, linked worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.

---

## 2. Evidence inventory

### 2.1 Committed records evaluated

The following committed records were read and their identities verified against the E-2 entry evidence package. The full inventory and reproduction commands are in `../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/record-hashes.txt`.

| Record | SHA-256 | Role in this decision |
|---|---|---|
| [E-2 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md) | `f4212a8a1302695dae53a763682c0dc79de3a47af02e29b14abffbec96e1b664` | The assessed record; disposition `RECONCILIATION REQUIRED` |
| [E-2 entry evidence index](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/README.md) | `3090058f7dc0e872e4c721b5f21cd9f6ab955b87c5dab4dbc0bb978c6e17273c` | Evidence package index, method, and limitations |
| [Slice E entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md) | `497dcb6924d692d4f3601c33baa3320de7036e057293d4bc4836c29e52fe99de` | Slice E architecture, K/M/F/S labels, E-2 package, original reconciliation prerequisites |
| [E-1 entry owner decision](rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) | `573f248a41b6d0215d294b74152ab17c64478fc6ac484884dff0f001b46b84fb` | Q-E1 through Q-E5; E-2 planning-only sequencing |
| [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) | `27de68690cca7027d46b0692e52e5da71641e38e13c4037fd31937d08f823b93` | E-1 accepted with qualifications |
| [F-01 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) | `30205d32409c097624da25cfd9ed6608f1588d79f3af96231125da06e6ad194b` | F-01 closed with qualifications; future-consumer fail-closed obligation |
| [F-02 parity owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | `00f39ae2a1fc1675fa195c3af0ba040fdd05eed956e497f5e0c6fa5285a56107` | F-02 closed with qualifications; Q1-F01–Q1-F06 and future-consumer gate |
| [F-03 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) | `44c85e0062c42ddec6e98a74268d71f358a9435f506089aa416e6045d47fb944` | F-03 closed with qualifications; PowerShell 5.1 scope only |
| [P4 coverage owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | `3bdccfdf3e24d4a00fd9f2e600cee13e3eab94014def03a349023eb2bf3e4b6e` | Refusal-only P4 coverage and preserved residuals |
| [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361` | Accepted prepublication selection, phase separation, validation-only ordering |
| [Alternative B implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | `9b3d9774e6a1c244ccfa89e8f16a54c8678d22bdef0e0272ce1c528b8dd4f42c` | Atomic Slice E dependency set (§3.5) |
| [Mechanism B owner acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e` | Frozen 15-file submission, recheck manifest, Q01–Q10 |
| [F02 trust-store owner decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | `e318125f91e20350824aeb70042b35a37c33d54fcde8350a4d353dccfaeb0adb` | Administrative approval for `/etc/gagamba/accepted-qualifications` only |
| [Installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md) | `f727ea026f9ef4eafe2bfb21af87ad9bee493149645e9e6576a3f85c694ca5f6` | Gates A–D; integrity versus authorization |
| [Remediation evidence standard](../remediation-evidence-standard.md) | `0478e4878b7c95d6441dcc24f6fde17f2f86baf74aab41a3298df892913b9854` | Durable evidence expectations; used as a model, not as a claim that this governance record is an implemented remediation |

### 2.2 Locally available external frozen inputs

The following local external inputs are **not** part of the Gagamba repository. They were checked only to confirm that their bytes match hashes recorded by committed acceptances. Full sizes, paths, and reproduction notes are in `../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/external-input-hashes.txt`.

| External input | SHA-256 | Committed acceptance binding |
|---|---|---|
| `case-policy-and-action-contract.md` | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | F02 15-file recheck manifest |
| `actor-and-descriptor-binding.md` | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | F02 15-file recheck manifest |
| `mechanism-b-session-capability.md` | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | F02 15-file recheck manifest |
| `final-acceptance-hash-contract.md` | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` | F02 15-file recheck manifest |
| `independent-recheck.md` | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | F02 owner acceptance §2 |
| `recheck-input-SHA256SUMS.txt` | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | F02 owner acceptance §2 |
| `validation-bootstrap-compatibility.md` | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | Alternative B owner decision §4 |
| `validation-bootstrap-assessment.md` | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | Alternative B owner decision §4 |
| `implementation-and-test-handoff.md` | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | Alternative B owner decision §4 |

The outer F02 package `SHA256SUMS.txt` (`a9c5dc71eb0c7b7ff9e443ea8107d55f99540cc119ee343fc18ca105b5ce4e6b`) is recorded for context only; the acceptance-bound object is `recheck-input-SHA256SUMS.txt`. All local hashes still match the committed values at this baseline.

### 2.3 Classification of the evidence

| Class | What it is here | What it is not |
|---|---|---|
| Normative accepted contracts | Alternative B decision; F02 mechanism acceptance; F02 trust-store decision for the qualification store; E-1/F-01/F-02/F-03/P4 acceptances | Not a validation-catalog approval-chain adoption; not an E-2 implementation authorization |
| Historical proposals and reviews | Slice E entry review; E-2 entry assessment | Not owner authority; recommendations do not authorize work |
| Artifact-integrity evidence | SHA-256 comparisons and source/call-graph inspection | Not provenance by itself; not approval |
| Artifact-provenance evidence | Committed owner acceptances that bind exact hashes and external package identities | Does not make external bytes in-repository schema |
| Existing owner authority | E-1 entry decision; E-1/F-01/F-02/F-03/P4 acceptances; Alternative B and F02 decisions within their stated scopes | Does not extend to validation catalogs or E-2 |
| Unresolved owner decisions | Q-E1 frozen-schema provenance; Q-E2 validation-catalog approval chain | Not resolved by this decision |
| New authorizations | E-2-P0 and E-2-D1, each separately gated and documentation-only | No E-2 entry or implementation authorization |

---

## 3. Entry-assessment evaluation

### 3.1 Material conclusions confirmed

| Assessment conclusion | Verification result | Basis |
|---|---|---|
| E-1 is accepted with qualifications | **CONFIRMED** | E-1 owner acceptance: implementation `2259fce`, qualification `db832452`; refusal-only, production-unreachable |
| F-01 is closed with qualifications | **CONFIRMED** | F-01 owner acceptance; three semantic bindings and future-consumer fail-closed obligation preserved |
| F-02 is closed with qualifications | **CONFIRMED** | F-02 parity owner acceptance; Q1-F01–Q1-F06 and ten-point future gate preserved |
| F-03 is closed with qualifications | **CONFIRMED** | F-03 owner acceptance; Windows PowerShell 5.1 only; PowerShell 7 unexecuted |
| P4 refusal-only acceptance remains in force | **CONFIRMED** | P4 owner acceptance: 21 COVERED, 5 AUTHORIZED CONTINUATION, 3 LEGACY / TEST-ONLY; residuals preserved |
| K/M/F/S roles are reconstructible | **CONFIRMED** | Accepted external contract through the F02 15-file manifest; role map in the E-2 evidence package; no new expansion invented |
| Approval graph is acyclic | **CONFIRMED** | `M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability`; no edge returns into K/F/S |
| Local external frozen inputs match committed acceptance hashes | **CONFIRMED** | All nine checked external inputs match the F02/Alternative B acceptance records; no mismatch |
| Q-E1 remains unresolved in-repository | **CONFIRMED** | No authoritative in-repository frozen-schema provenance record; exact catalog-chain field names absent from `docs/` |
| Q-E2 remains unresolved in-repository | **CONFIRMED** | No owner decision adopts the validation-catalog approval chain; only gap statements mention the root |
| External approval chain appears coherent | **CONFIRMED AS A PROPOSED CHAIN** | Complete and acyclic in the external contract; not adopted in-repository |
| E-2 entry is not yet supportable | **CONFIRMED** | Q-E1 and Q-E2 are express gating prerequisites; E-2 is schema- and approval-dependent |

### 3.2 Integrity, provenance, approval, and authorization kept separate

| Property | Meaning | What exists at this baseline | What does not exist |
|---|---|---|---|
| **Integrity** | The available bytes match an expected cryptographic digest | Local external inputs match committed hashes; source continuity from the reviewed F-02 revision; 44 historical paths hash-match | An in-repository frozen-schema record |
| **Provenance** | The authoritative source and accepted identity of those bytes are established | Committed F02 and Alternative B acceptances bind the hashes; the chain from acceptance to bytes is traceable | A published or durable external package independent of the local `Solo/` tree; a repository-only reconstruction of every field |
| **Approval** | An authorized owner has adopted the artifact or approval chain for a specified purpose | E-1/F-01/F-02/F-03/P4 accepted within their scopes; F02 trust store approved for `accepted-qualifications` only | Owner adoption of the validation-catalog approval chain; any extension of F02 by analogy |
| **Authorization** | The approved material may influence a specific later operation | E-1 refusal-only implementation within its accepted boundary; no production consumer | E-2 implementation authority; catalog eligibility; validation authority; execution authority |

Hash agreement is **not** provenance, approval, or authorization. The assessment correctly preserved that distinction, and this decision does too.

### 3.3 No material assessment error found

The entry assessment's disposition follows its evidence. Its stated positives are real: the E-1 prerequisite is conditionally satisfied, the frozen inputs are locally present and hash-match, and the approval graph is coherent. Its stated gaps are also real: Q-E1 and Q-E2 are unmet in-repository, and the missing items are repository authority and owner adoption rather than a cryptographic or architectural contradiction.

No conclusion in the assessment was found to overstate the evidence or to promote integrity evidence into validation authority. The assessment did not adopt a schema, approve a catalog, create a trust anchor, or authorize E-2.

---

## 4. Formal assessment acceptance

**The E-2 entry assessment and its `RECONCILIATION REQUIRED` disposition are ACCEPTED.**

This acceptance is of the assessment as a governance record. It is not an E-2 entry authorization, not an implementation authorization, and not a finding that Q-E1 or Q-E2 are resolved.

Limits of this acceptance:

- It is a single-reviewer documentation evaluation based on committed records and locally available inputs.
- No build, test suite, native probe, protected acquisition, provisioning, or execution was performed or rerun.
- External bytes were not authenticated beyond SHA-256 comparison to committed acceptance hashes.
- This is not independent multi-model qualification, external attestation, or human signature.
- The historical PENDING captions in external documents remain untouched; the later committed decisions govern the semantics they expressly accept.

These limits do not undermine the assessment's narrow conclusion: E-2 entry requires reconciliation before it can be decided.

---

## 5. Q-E1 disposition — frozen-schema provenance

**Q-E1 remains OPEN and UNRESOLVED IN-REPOSITORY.**

The E-1 entry owner decision requires a separately reviewable provenance reconciliation that binds exact bytes, originating acceptances and limitations, and commits the authoritative record before schema-dependent packages. The repository still lacks that record.

Authorizing **E-2-P0** does not resolve Q-E1. It authorizes a bounded attempt to reconcile existing accepted evidence. Q-E1 may be recommended for closure only after:

1. an independently traceable provenance record exists in the repository;
2. the record is separately reviewable and its hashes are reproducible;
3. the relevant owner accepts the record within its precise authority scope; and
4. a separate P0 completion and owner-acceptance decision establishes whether Q-E1 is actually satisfied.

A Markdown document alone does not resolve Q-E1. A hash list alone is not a schema record. A local external file is not an in-repository authority.

Q-E1 is composite. P0 is authorized first for the **E-2 subset**: K/M/F/S/R/Wc/P and their exact canonical encoding, field sets, bounds, cross-field equalities, and acceptance bindings. P0 must explicitly record what remains within Q-E1 for later packages, including the GAVA1 header, v3 validation-pre layouts, and release correlation for E-3/E-4. A P0 owner acceptance may close Q-E1 **only for the precise E-2 subset it accepts**; broader Q-E1 items remain open for their own reconciliation.

When authoritative evidence permits, the P0 record must identify:

1. each frozen-schema artifact;
2. exact artifact identity;
3. accepted version or revision;
4. cryptographic digest and algorithm;
5. original authoritative source;
6. relevant committed acceptance;
7. relationship to the accepted K/M/F/S contracts;
8. evidence chain establishing provenance;
9. permitted uses;
10. prohibited uses;
11. replacement or supersession restrictions;
12. unresolved approval or authority questions.

Where authoritative evidence does not permit an item to be established, P0 must record the gap explicitly and must not invent a value.

If existing evidence is insufficient to establish provenance, P0 must report **RECONCILIATION STILL REQUIRED**, not manufacture an authoritative record.

---

## 6. Q-E2 disposition — validation-catalog approval chain

**Q-E2 remains OPEN and UNRESOLVED IN-REPOSITORY.**

The E-1 entry owner decision states that `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` has no accepted owner decision defining its approval chain, and that F02's authority over `/etc/gagamba/accepted-qualifications/<qualificationId>.json` does not automatically extend to validation catalogs.

Authorizing **E-2-D1** does not adopt the external chain. It authorizes a bounded owner-decision activity to determine whether the chain is adoptable, and on what authority.

D1 must evaluate:

1. accepted K/M/F/S meanings;
2. direction of each approval dependency;
3. authority to approve each artifact;
4. catalog identity;
5. catalog-to-schema binding;
6. trust-anchor origin;
7. delegation restrictions, if applicable;
8. approval provenance;
9. version and substitution controls;
10. replacement, rollback, or supersession rules;
11. invalid or conflicting approval evidence;
12. fail-closed refusal conditions;
13. downstream consumers and their restrictions;
14. relationship to E-3 and E-4;
15. whether any existing F02 authority is relevant.

### 6.1 Critical F02 distinction

F02 qualification-store authority and validation-catalog approval authority are **not interchangeable**.

The F02 trust-store decision approves a root-controlled administrative decision for one exact qualification record under `/etc/gagamba/accepted-qualifications`. It does not adopt the catalog review/acceptance/provisioning chain, the `/etc/gagamba/accepted-validation-catalogs` root, the `/etc/gagamba/validation-catalog-evidence` root, the V/W/A roles, the R/Wc/P representations, or the catalog withdrawal/reapproval semantics.

D1 must explicitly determine whether a permitted authority relationship exists. If existing specifications do not authorize that relationship, D1 must not infer one from the implementation, from internal coherence, or from the fact that both chains use administrative records.

The owner may adopt an existing chain only after establishing its legitimate authority, scope, and provenance. D1 may issue an explicit adoption, rejection, qualification, or reconciliation-required decision. Q-E2 may be recommended for closure only when a formal owner decision establishes an accepted validation-catalog approval chain with clear scope and binding authority. If required approval evidence remains missing, D1 must record an unresolved prerequisite. A chain is not accepted solely because it is internally coherent or acyclic.

---

## 7. E-2-P0 authorization and limits

### 7.1 Exact scope

**E-2-P0** is authorized as a documentation-only frozen-schema provenance reconciliation for the E-2 subset of Q-E1. Its mission is to establish an auditable repository provenance record for the externally accepted frozen-schema inputs, using only existing accepted artifacts and committed approval evidence.

P0 may:

- inspect existing accepted artifacts and committed acceptance records;
- verify locally available frozen-input identities;
- reconcile their existing acceptance records;
- create an authoritative repository provenance record only to the extent supported by actual owner authority and accepted source evidence;
- record unresolved provenance gaps explicitly;
- update current-state documentation;
- commit reproducible documentation and evidence.

P0 must not:

- change frozen-schema contents;
- invent a schema version;
- redefine canonical serialization;
- introduce new signatures or trust anchors;
- treat a hash match as owner approval;
- grant validation-catalog authority;
- create runtime authority;
- introduce production code;
- activate positive validation.

### 7.2 Required prerequisites

1. This owner decision is committed and in force.
2. The E-2 entry assessment and its evidence package remain committed at their reviewed revision.
3. E-1 remains accepted with qualifications; F-01/F-02/F-03 remain closed with qualifications; P4 refusal-only coverage remains accepted.
4. The 44 historical untracked paths remain preserved by content.
5. No new external authority artifact is required. Locally available accepted artifacts may be used; unavailable items must be recorded as gaps.
6. P0 is separately commissioned; it does not start automatically from this decision.

### 7.3 Permitted deliverables

- A new provenance record under `docs/evidence/` or `docs/reviews/`, with exact hash inventory, acceptance chain, normative extraction or exact copies where permitted, limitations, conflict/change rule, and reproducible verification commands.
- An explicit statement of the E-2 subset covered and the Q-E1 items left for E-3/E-4.
- An independent-review handoff and a proposed owner-acceptance package.
- Additive current-state references only.
- No production/test/verifier/harness source, schema reader, catalog store, trust anchor, or authority construction.

### 7.4 Authority limitations

P0 creates provenance documentation, not approval authority. It cannot convert hash agreement into owner approval. It cannot adopt the validation-catalog chain, create a catalog instance, provision a protected store, define a trust anchor, or authorize any runtime effect. It cannot resolve Q-E2. It cannot make E-2 entry-ready.

### 7.5 Completion criteria

Q-E1 may be recommended for closure **for the accepted E-2 subset** only when:

1. an independently traceable provenance record exists in the repository;
2. a reviewer can reconstruct the E-2 K/M/F/S/R/Wc/P contract from the repository without relying on an unpublished external file;
3. every hash matches the committed acceptance;
4. the record states what remains external and why;
5. unresolved provenance gaps are recorded explicitly;
6. no schema behavior is implemented; and
7. a separate P0 owner-acceptance decision accepts the record within its precise scope.

### 7.6 Stop conditions

P0 must stop or report **RECONCILIATION STILL REQUIRED** if:

- a frozen artifact's provenance cannot be established;
- an expected digest does not match;
- referenced acceptance evidence is missing;
- the record would depend on an unknown authority;
- the catalog would be approved against the wrong schema;
- existing F02 authority is being reused without an explicit accepted contract;
- a proposed trust anchor lacks provenance;
- K/M/F/S relationships conflict with accepted specifications;
- the activity begins introducing runtime authority;
- E-1 evidence is promoted into positive validation authority;
- E-2 implementation is attempted before fresh entry approval; or
- protected execution behavior would change without P4 authorization.

A reconciliation blocker must remain visible. It must not be concealed by inventing a convenient engineering interpretation.

### 7.7 Required next decision

A separate **P0 owner acceptance** decision. P0 completion does not automatically start D1.

---

## 8. E-2-D1 authorization and limits

### 8.1 Exact scope

**E-2-D1** is authorized as a documentation-only owner-decision activity to evaluate, and then accept, qualify, reject, or require reconciliation of, the validation-catalog approval-chain model.

D1 may:

- examine the accepted external approval chain;
- reconstruct the authority relationships;
- reconcile applicable owner decisions;
- determine whether the chain is adoptable;
- issue an explicit owner adoption, rejection, or qualified reconciliation decision;
- document binding future verification requirements.

D1 must not:

- invent a root authorizer;
- introduce a trust anchor without accepted authority;
- expand F02 authority by implication;
- create validation-catalog implementation;
- create a positive validation authority factory;
- grant execution authorization;
- implement an E-1 production consumer;
- implement E-3 or E-4;
- activate any protected execution path.

D1 is an explicit owner-adoption decision, not a runtime implementation task.

### 8.2 Required prerequisites

1. This owner decision is committed and in force.
2. P0 has ordinarily completed and received its separate owner acceptance, so D1's exact representations can rely on correctly identified frozen-schema artifacts and accepted provenance.
3. If D1 evaluates independent contractual questions before P0 completion, it must not make final adoption depend on unresolved schema provenance.
4. E-1/F-01/F-02/F-03/P4 qualifications remain preserved.
5. D1 is separately commissioned; it does not start automatically from this decision or from P0 completion.

### 8.3 Permitted deliverables

- A new owner decision under `docs/decisions/`.
- Exact references to the P0 provenance record and the accepted frozen-input identities.
- An explicit determination on the V/W/A roles, R/Wc/P representations, protected roots, custody, publication order, failure semantics, live binding, withdrawal/reapproval, F02 separation, threat model, and non-claims.
- Binding future verification requirements for any later implementation.
- Additive current-state references only.
- No implementation, catalog instance, protected store, approval record, capability, trust anchor, or authority factory.

### 8.4 Authority limitations

D1 cannot create approval authority by describing it. It cannot treat the external chain's coherence as adoption. It cannot map V/W/A onto existing Slice E roles without an explicit accepted decision. It cannot extend the F02 qualification-store decision by analogy. It cannot authorize a producer to create P, a runtime to read an unapproved catalog, or any positive validation path.

### 8.5 Completion criteria

Q-E2 may be recommended for closure only when a formal owner decision establishes:

1. who may approve each artifact and under what authority;
2. the exact accepted representations and cross-field bindings;
3. the protected roots and custody requirements;
4. publication, failure, withdrawal, and reapproval semantics;
5. the live-binding separation that keeps P as eligibility rather than invocation;
6. the express non-extension of F02 and non-reuse of the final-admission chain;
7. the threat model and non-claims; and
8. the binding scope of any adoption.

If required approval evidence remains missing, D1 must record an unresolved prerequisite.

### 8.6 Stop conditions

D1 must stop or require additional owner resolution if:

- the approval chain depends on an unknown authority;
- a catalog would be approved against the wrong schema;
- existing F02 authority is being reused without an explicit accepted contract;
- a proposed trust anchor lacks provenance;
- K/M/F/S relationships conflict with accepted specifications;
- the activity begins introducing runtime authority;
- E-1 evidence is promoted into positive validation authority;
- E-2 implementation is attempted before fresh entry approval; or
- protected execution behavior would change without P4 authorization.

### 8.7 Required next decision

The D1 owner decision itself, followed by a fresh E-2 entry assessment and owner decision. D1 adoption does not automatically authorize E-2 implementation.

---

## 9. Required sequencing

The binding sequence is:

```text
E-2 entry assessment (accepted)
  -> E-2-P0 frozen-schema provenance reconciliation
  -> P0 owner acceptance
  -> E-2-D1 validation-catalog approval-chain owner decision
  -> D1 owner decision (adoption / qualification / rejection / reconciliation required)
  -> fresh E-2 entry assessment
  -> fresh E-2 entry owner decision
  -> bounded E-2 implementation under a separate commission
```

Rules:

1. P0 and D1 are separately commissioned and independently accepted within their respective scopes.
2. P0 should ordinarily precede D1 because D1 must rely on correctly identified frozen-schema artifacts and accepted provenance.
3. D1 may evaluate independent contractual questions before P0 is complete, but final adoption must not rely on unresolved schema provenance.
4. Neither reconciliation activity may automatically start the next one.
5. P0 evidence must not be promoted into catalog approval.
6. D1 approval must not be promoted into runtime authorization.
7. E-1 integrity evidence must not be promoted into validation authority.
8. Catalog approval must not be promoted into protected native execution.
9. No activity in this sequence authorizes E-2 implementation.

---

## 10. Fresh E-2 entry decision gate

After P0 and D1 receive their required dispositions, a fresh E-2 entry assessment and owner decision must verify:

1. Q-E1 has authoritative accepted provenance for the E-2 subset.
2. Q-E2 has explicit approval-chain owner adoption, or a recorded owner decision that the chain is not adoptable and E-2 must stop.
3. The accepted K/M/F/S relationship remains consistent with the accepted design.
4. Relevant frozen-input identities still match their committed acceptances.
5. No new conflicting authority evidence exists.
6. E-1 qualifications remain preserved.
7. No unauthorized E-1 production consumer has appeared.
8. P4 refusal-only boundaries remain intact.
9. The proposed first implementation package is independently qualifiable.
10. All applicable RELEASE and native constraints remain explicit.

If the E-2-scoped P0 acceptance is relied upon, the fresh decision must expressly state that the remaining Q-E1 items (GAVA1 header, v3 validation-pre layouts, and release correlation) are not dependencies of the proposed E-2 package. Otherwise full Q-E1 provenance is required before E-2 entry.

Only that later owner decision may authorize a bounded E-2 implementation package. Completing P0 and D1 does not automatically authorize E-2 implementation.

---

## 11. K/M/F/S authority-preservation conditions

1. **Role labels are not expansions.** K, M, F, and S are used as the accepted design uses them. This decision does not invent acronym expansions or new field meanings.
2. **K is not P.** K is the protected catalog. P is the protected catalog-eligibility approval. K contains no review, owner-decision, acceptance-receipt, or self-hash field.
3. **P is eligibility, not invocation.** P does not grant a live capability. The fresh live challenge/response and one-response latch remain necessary.
4. **R and Wc are detached.** They do not become fields inside K, and no edge returns from R/Wc/P into K/F/S.
5. **Exact binding is preserved.** M is constructed first; F references hash(M); S references hash(M) and hash(F); K references hash(M), hash(F), and hash(S). Duplicated values in dependent data must equal K, never override it.
6. **No catalog-as-code.** S is a finite template, not a command language. No script text is evaluated; no arbitrary command, expression, substitution, loop, retry, callback, or extension field is admitted.
7. **No producer-built P.** A must independently verify V/W provenance. Merely observing K/R/Wc candidate files cannot create approval.
8. **F02 is store-specific.** The F02 qualification-store decision does not extend by analogy to validation catalogs.
9. **V/W/A mapping is unresolved until D1.** The external catalog-contract labels V (reviewer), W (owner accepting exact bytes), and A (root administrator) are not silently mapped onto Slice E's A/L/C/R/O roles.
10. **No schema adoption by this decision.** The exact external schemas remain external until P0 produces a supported provenance record and the owner accepts it. Reproducing selected fields for review is not adoption.
11. **Hash agreement is not approval.** The presence of a matching digest does not establish human provenance, owner acceptance, or runtime authority.

---

## 12. Trust-anchor restrictions

1. **No new trust anchor.** This decision creates none.
2. **Hash acceptance is not trust-anchor adoption.** The F02 and Alternative B acceptances bind exact external bytes; they do not turn those bytes into a repository trust anchor.
3. **P0 may not introduce signatures or trust anchors.** It may only reconcile existing provenance and accepted evidence.
4. **D1 may not invent a root authorizer.** It must establish whether an authority relationship already exists and is accepted.
5. **F02 authority must not be expanded by implication.** A shared administrative primitive is not shared authority.
6. **Provenance is administrative.** A must independently verify the actual review/acceptance chain. A producer report, timestamp, candidate file, or self-consistent hash set is insufficient.
7. **Trusted-root premises are preserved.** The trusted host root, kernel, filesystem, and fixed protected selection remain premises. Malicious root, copied-host rollback, and hostile same-UID memory isolation remain outside the claimed guarantee.
8. **No standing approval or unattended authority.** Any future catalog approval is bounded, host/scope-specific, and subject to the accepted live-binding and withdrawal rules.
9. **No cross-host reuse.** A catalog approval must not be treated as portable authority across hosts or installations.
10. **A new trust anchor requires its own owner decision.** It must identify exact provenance, scope, custody, and failure semantics.

---

## 13. E-1 integration obligations

1. **E-1 remains accepted with qualifications and production-unreachable.** `VerifyActiveIntegrity` is called only by `PrepublicationIntegrityProjection.Verify`, which has no production caller; `FromEvidence` is private; `ForTest` is excluded from RELEASE.
2. **E-1 evidence is integrity evidence only.** It is not qualification, catalog approval, case authorization, capability issuance, execution authority, or a positive validation result.
3. **F-01 remains closed with qualifications.** Preserve the three semantic bindings:
   - `OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1)`
   - `RecoveryClosureSha256 == InstallationIdentity.RoleHash(manifest, 0)`
   - `RuntimeInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256")`
4. **F-01 future-consumer obligation remains binding.** Before any production consumer of the E-1 projection is authorized, every integrity-verification exception and failure mode must fail closed with no fallback, downgrade, partial authorization, or continuation into a protected effect. A consumer must not catch a subset of exception types in a way that permits continuation.
5. **F-02 remains closed with qualifications.** Preserve Q1-F01 (call-preserving bypasses), Q1-F02 (arguments/helper internals), Q1-F03 (name-specific negative checks), Q1-F04 (ordering versus dominance), Q1-F05 (commit-bound artifact identities; rebuild/reapproval), and Q1-F06 (non-exhaustive mutation evidence).
6. **F-02 future-consumer gate remains binding.** Before the first production consumer of the E-1 projection, a fresh security review must cover both verification paths, shared invariants, conditional bypass/branch dominance, call arguments/helper semantics, F-01 relationships, fail-closed exception handling, authority separation, runtime loaded-assembly verification-to-use, publication/artifact identity, and P4/native obligations.
7. **Option A consolidation is not authorized.** It must be reconsidered at the accepted integration gate; closure of F-02 is not automatic authorization.
8. **F-03 remains closed with qualifications.** Windows PowerShell 5.1 is the qualified harness scope. PowerShell 7 is unexecuted and unqualified. Existing nonzero-exit semantics and scoped `$ErrorActionPreference` restoration are preserved. No verification-tooling modification is authorized.
9. **No E-1 production consumer is authorized by P0 or D1.** Neither activity may wire the projection, construct `OwnerAuthorityContext.Validation`, mint a capability, or promote E-1 evidence into catalog or validation authority.
10. **Runtime loaded-assembly verification-to-use remains deferred.** E-1 does not perform it.

---

## 14. P4 and RELEASE preservation

### 14.1 P4 classification

This decision is **P4-NEUTRAL**. It creates no executable production change, no production caller, no protected sink, no E-1 consumer, and no lifecycle wiring.

P0 and D1 are documentation-only. They may establish provenance and approval authority documentation; they may not activate execution authority. Any later executable integration must receive its own P4 impact classification before the first protected effect.

### 14.2 Preserved P4 qualifications

- **Protected-sink caller restrictions:** preserved; no new caller or sink is authorized.
- **F01/F02 enforcement:** preserved; no production integration or behavioral-equivalence claim is added.
- **S10/S11 domination:** preserved; any new production caller of the generic callees requires a fresh sink/authority audit (F-R1/O-R1).
- **S28 isolation:** preserved; S28 remains test-only, and `internal` visibility is not authority (O-R2).
- **First protected effect:** preserved; no documentation reconciliation may move or add it.
- **Qualified-to-legacy downgrade refusal:** preserved; no fallback, downgrade, or partial authorization is authorized.
- **F-R1 / O-R1:** S10/S11 domination relies on the audited caller graph and upstream F01 domination. Any new production caller of the generic callees requires a fresh sink/authority audit.
- **O-R2:** S28 remains test-only. `internal` visibility is not authority.
- **O-R3:** refusal-reason typing under the constructor-recheck race remains open.
- **Observation-to-effect residual:** a root change after the last relevant stable observation may escape detection, even before the verifier returns. No atomic root-mutation/effect lock or retroactive cancellation is promised.
- **`NATIVE-PENDING`:** applicable covered rows remain pending.
- **331 unqualified native skips:** not passes, not coverage, not protected native positives.
- **5 AUTHORIZED CONTINUATION and 3 LEGACY / TEST-ONLY rows:** preserved as accepted; not reclassified as unconditional COVERED.
- **Slice N:** a separate authorized milestone.

No protected native execution qualification is implied. P0 and D1 cannot claim native qualification from deterministic tests, synthetic fixtures, mocked providers, or outer sandbox denial.

### 14.3 RELEASE preservation

No production, test, verifier, or harness source change is authorized. No public API expansion, `OwnerAuthorityContext.Validation` factory, test seam in RELEASE, or E-1 consumer is authorized. The RELEASE/Debug separation and existing production refusal behavior remain as accepted.

---

## 15. Downstream Slice E exclusions

This decision authorizes none of the following:

- **E-2 implementation.** E-2 entry and implementation remain unauthorized.
- **E-3 v3-pre codec/parser.**
- **E-4 GAVA1 direct-context exchange.**
- **E-5 fixed launcher/custody interface.**
- **E-6 atomic enablement and R/O/C ordering correction.**
- **E-7 driver integration.**
- **Slice N protected native qualification.**
- Root-authorizer implementation.
- Launcher or custody changes.
- Privilege drop, pidfd/SCM custody, or protected execution activation.
- Positive validation.
- Validation-catalog implementation or protected-store provisioning.
- Any trust-anchor construction.
- Any authority factory.
- Any E-1 production consumer.
- Any public API expansion.
- Any production/test/verifier/harness change.

The accepted roadmap labels remain: E-2 frozen-schema provenance and validation-catalog approval; E-3 v3-pre codec; E-4 GAVA1; E-5 fixed launcher/custody interface; E-6 atomic enablement and R/O/C ordering correction; E-7 driver integration; Slice N protected native qualification. The labels document planning order only. They do not authorize implementation.

E-4 and E-6 retain their fresh P4 re-audit requirements. E-6 remains unreachable if any atomic dependency in plan §3.5 is unfinished.

---

## 16. Formal owner disposition

**ENTRY RECONCILIATION AUTHORIZED WITH QUALIFICATIONS.**

The E-2 entry assessment and its `RECONCILIATION REQUIRED` disposition are accepted. The two bounded reconciliation activities are separately authorized for future execution:

- **E-2-P0:** documentation-only frozen-schema provenance reconciliation for the E-2 subset.
- **E-2-D1:** documentation-only validation-catalog approval-chain owner decision.

The qualifications are binding:

1. **Q-E1 and Q-E2 remain unresolved.** Authorizing P0 and D1 does not resolve either prerequisite.
2. **P0 and D1 do not start automatically.** Each requires its own commission and must be independently accepted within its scope.
3. **P0 precedes D1 in the ordinary sequence.** D1 final adoption cannot rely on unresolved schema provenance.
4. **P0 completion requires a separate P0 owner acceptance.** A provenance record alone does not close Q-E1.
5. **D1 must produce an explicit owner decision.** Internal coherence or acyclicity is not adoption.
6. **F02 authority is not extended.** The qualification-store decision remains store-specific.
7. **No schema, catalog, approval, trust anchor, or runtime authority is created by this decision.**
8. **No E-1 integrity evidence is promoted into validation authority.**
9. **E-2 entry and implementation remain unauthorized.** A fresh E-2 entry assessment and owner decision are required after P0 and D1 receive their dispositions.
10. **E-3 through E-7 and Slice N remain unauthorized.**
11. **P4 and RELEASE preservation remain binding.** This decision is P4-neutral and makes no executable change.
12. **This is a single-reviewer governance recording, not independent multi-model qualification.**

Nothing in this disposition authorizes E-2 implementation, a schema-dependent reader, a protected store, a validation context, an E-1 consumer, a catalog approval, a capability, positive validation, or production activation.

---

## 17. Recommended next commission

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P0`.

Purpose: establish the accepted frozen-schema provenance record for the E-2 subset using existing authoritative artifacts and committed approval evidence. P0 must be documentation-only, evidence-backed, and independently reviewable. It must not invent schema provenance or catalog approval authority. It must record unresolved gaps explicitly and must report **RECONCILIATION STILL REQUIRED** if provenance cannot be established.

Upon successful P0 completion and separate owner acceptance, proceed to a separately commissioned D1 owner-adoption decision. Do not begin P0 automatically from this decision.

This decision changed documentation only: this owner decision and additive current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md`, and `docs/work-queue.md`. Historical records remain unchanged. The E-2 entry report and evidence package are preserved. The 44 historical untracked paths and their bytes are preserved. One canonical worktree remains on `main`. No branch, extra worktree, reset, clean, merge, rebase, prune, stash, force operation, synchronization, or push was performed.

**Stop after committing this E-2 entry owner decision. Do not begin P0, D1, E-2 implementation, or any later Slice E milestone automatically.**

---

## Appendix A — Reconciliation authorization matrix

| Activity | Decision required | Status after this decision |
|---|---|---|
| E-2-P0 provenance reconciliation | Authorize / reject / qualify | **AUTHORIZED WITH QUALIFICATIONS** — documentation-only, E-2 subset, separate commission required |
| P0 owner acceptance | Required separately | **NOT ISSUED** — required before D1 final adoption and before any Q-E1 closure recommendation |
| E-2-D1 approval-chain decision | Authorize / reject / qualify | **AUTHORIZED WITH QUALIFICATIONS** — documentation-only, owner-decision activity, separate commission required |
| D1 approval-chain adoption | Required separately | **NOT ISSUED** — D1 itself must issue an explicit adoption, qualification, rejection, or reconciliation-required decision |
| Fresh E-2 entry assessment | Required after reconciliation | **NOT ISSUED** — required after P0 and D1 dispositions |
| Fresh E-2 entry owner decision | Required before implementation | **NOT ISSUED** — only this later decision may authorize a bounded E-2 implementation |
| E-2 implementation | Not authorized | **NOT AUTHORIZED** |
| E-3 through E-7 | Not authorized | **NOT AUTHORIZED** |
| Slice N | Not authorized | **NOT AUTHORIZED** |

### A.1 P0 authorization fields

| Field | Binding content |
|---|---|
| Exact scope | Documentation-only provenance reconciliation for the E-2 K/M/F/S/R/Wc/P subset of Q-E1; explicit statement of remaining GAVA1/v3-pre/release-correlation items |
| Required prerequisites | This decision; committed E-2 assessment/evidence; preserved E-1/F-01/F-02/F-03/P4 qualifications; 44-path preservation; separate P0 commission |
| Permitted deliverables | Provenance record with exact inventory, acceptance chain, normative extraction or permitted exact copies, limitations, conflict/change rule, reproducible commands, review handoff, additive current-state references |
| Authority limitations | No schema change/version invention/serialization redefinition/signatures/trust anchors/hash-as-approval/catalog authority/runtime authority/production code/positive validation |
| Completion criteria | Independently traceable record; repository-only reconstruction for E-2 subset; hashes match; external remainder stated; gaps explicit; no behavior implemented; separate owner acceptance |
| Stop conditions | Unestablished provenance; digest mismatch; missing acceptance; unknown authority; wrong-schema approval; F02 reuse without contract; trust anchor without provenance; K/M/F/S conflict; runtime authority; E-1 promotion; premature E-2 implementation; P4 change without authorization |
| Required next decision | Separate P0 owner acceptance |

### A.2 D1 authorization fields

| Field | Binding content |
|---|---|
| Exact scope | Documentation-only owner decision on the validation-catalog approval chain: adopt, qualify, reject, or require reconciliation |
| Required prerequisites | This decision; ordinarily P0 completion and P0 owner acceptance; preserved E-1/F-01/F-02/F-03/P4 qualifications; separate D1 commission |
| Permitted deliverables | Owner decision under `docs/decisions/`; authority reconstruction; exact representations/roots/custody/publication/live-binding/withdrawal semantics; F02 non-extension; threat model; non-claims; binding future verification requirements |
| Authority limitations | No root-authorizer invention; no trust anchor without accepted authority; no F02 expansion; no catalog implementation; no validation factory; no execution authorization; no E-1 consumer; no E-3/E-4; no activation |
| Completion criteria | Formal owner decision establishing accepted chain with clear scope and binding authority, or recording an unresolved prerequisite |
| Stop conditions | Unknown authority; missing approval evidence; wrong-schema approval; F02 reuse without explicit contract; trust anchor without provenance; K/M/F/S conflict; runtime authority; E-1 promotion; premature E-2 implementation; P4 change without authorization |
| Required next decision | The D1 owner decision, then a fresh E-2 entry assessment and owner decision |

---

## Appendix B — Binding stop conditions for future reconciliation

Future P0, P1, D1, or fresh-entry work must stop or require additional owner resolution if:

1. A frozen artifact's provenance cannot be established.
2. An expected digest does not match.
3. Referenced acceptance evidence is missing.
4. An approval chain depends on an unknown authority.
5. A catalog is approved against the wrong schema.
6. Existing F02 authority is being reused without an explicit accepted contract.
7. A proposed trust anchor lacks provenance.
8. K/M/F/S relationships conflict with accepted specifications.
9. A reconciliation activity begins introducing runtime authority.
10. E-1 evidence is promoted into positive validation authority.
11. E-2 implementation is attempted before fresh entry approval.
12. Protected execution behavior would change without P4 authorization.

A reconciliation blocker must remain visible in the committed record. It must not be concealed by inventing a convenient engineering interpretation, by treating hash agreement as approval, or by describing a proposed chain as adopted.
