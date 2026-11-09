# ALT-B Slice E-2 D1 owner decision — validation-catalog approval chain

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-D1`

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push. This is a single-reviewer governance recording using committed repository evidence and locally available frozen inputs; it is **not** independent multi-model qualification, external attestation, or a human signature.

This decision issues the D1 owner decision on the validation-catalog approval chain. The formal disposition is stated exactly once in §16. It adopts a governance model and its authority relationships only. It does not implement the chain, does not issue any concrete catalog approval, does not create a trust anchor, does not extend F02 authority, and does not authorize E-2 implementation, positive validation, or protected execution.

---

## 1. Verified starting baseline

The baseline was verified before any edit.

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `5b2ef145088818c9b9b145d706816a075951c861` |
| Starting tree | `df6d8fea5de20e9efb0eb4bd9ef634d4763e871f` |
| Worktrees | exactly one canonical checkout: `C:/Users/Laszlos/source/repos/Gagamba 5b2ef14 [main]` |
| Tracked / index state | clean; no staged, modified, or deleted tracked path |
| Historical untracked paths | exactly 44 |
| Historical content comparison | 44 of 44 present; per-file SHA-256 match against `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation lines digest | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation map digest | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 18 commits ahead of `origin/main`; no push |
| P0 adoption commit | `5b2ef145088818c9b9b145d706816a075951c861` (the P0 owner-acceptance commit) |
| P0 adopted record identity | blob `db25949d25babd474d00ae5b2f436e15507f22b3`, SHA-256 `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` (unchanged) |
| Network / package / cloud | none used; no fetch, pull, push, or synchronization |
| `src` / `tests` / `eng` change | none for this decision; documentation-only |

Ancestry was verified with `git merge-base --is-ancestor` (all exit 0): P0 acceptance `5b2ef145088818c9b9b145d706816a075951c861`; P0 reconciliation `77101ac18c0e019a9d32712e9fee78b468cc8d02`; E-2 entry owner decision `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`; E-2 entry assessment `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d`; P4 coverage acceptance `2d39380d0c0b61753097c197ce7c1df03cdddcb7`; E-1 acceptance `8bd71425eb31b69290affa2b86c78afce1bd0882`; F-01 closure `1ed1fc706ea76954e4f1d7d43673048ef82072ec`; F-02 closure `e86254e60fb3ded770156519e1d3e15e73bf36fc`; F-03 closure `6d08ae569c03e96872fcc5d330435991952207d6`; Slice E entry `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e`; Alternative B decision `d804d9062bab5d9d540df5c2d64f1b2c31bdd667`; F02 Mechanism B acceptance `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1`; S2/F02 trust-store decision `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6`; native-input freeze `504b6d06ac88591072b11f428ae88cade390c075`. No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was performed.

---

## 2. Authoritative evidence inventory

Committed governance records used for this decision (SHA-256 of exact bytes; full manifest in `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/record-hashes.txt`):

| Record | SHA-256 | D1 role |
|---|---|---|
| [E-2 entry owner decision](rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md) | `ee0b2b81682e266488f59e0abcb870664ba25eaa0048764f885b44ae72793c79` | D1 authorization; Q-E2 completion criteria; trust-anchor restrictions |
| [E-2 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md) | `f4212a8a1302695dae53a763682c0dc79de3a47af02e29b14abffbec96e1b664` | Accepted `RECONCILIATION REQUIRED`; role map and graph |
| [E-2 entry evidence package](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/README.md) | `3090058f7dc0e872e4c721b5f21cd9f6ab955b87c5dab4dbc0bb978c6e17273c` | Eight-file evidence set including the role map |
| [P0 report](../reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md) | `513d395d9cb6f6591e62a20732a33c2f4c78018865f0407cf28c2b3b962d2f85` | Reconciliation of the frozen inputs and chain |
| [P0 adopted record](../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md) | `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` | Owner-adopted E-2-scoped provenance record |
| [P0 owner acceptance](rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md) | `049d9195bd2cc9b2b02515b771c4d15263a49042dc56d467d4d33a81c5ba50f5` | Q-E1 satisfied for the E-2 subset; limits preserved |
| [F02 Mechanism B owner acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e` | Accepts the exact 15-file submission with Q01–Q10 |
| [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361` | Validation context, early direct authentication, stopping |
| [Alternative B implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | `9b3d9774e6a1c244ccfa89e8f16a54c8678d22bdef0e0272ce1c528b8dd4f42c` | §3.5 atomic Slice E dependency set |
| [F02 trust-store owner decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | `e318125f91e20350824aeb70042b35a37c33d54fcde8350a4d353dccfaeb0adb` | Store-specific administrative model (not extended) |
| [Installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md) | `f727ea026f9ef4eafe2bfb21af87ad9bee493149645e9e6576a3f85c694ca5f6` | Integrity versus authorization; gates A–D |
| [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) | `27de68690cca7027d46b0692e52e5da71641e38e13c4037fd31937d08f823b93` | E-1 accepted with qualifications |
| [F-01 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) | `30205d32409c097624da25cfd9ed6608f1588d79f3af96231125da06e6ad194b` | Role-hash bindings; fail-closed obligation |
| [F-02 parity owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | `00f39ae2a1fc1675fa195c3af0ba040fdd05eed956e497f5e0c6fa5285a56107` | Q1-F01–Q06; future-integration gate |
| [F-03 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) | `44c85e0062c42ddec6e98a74268d71f358a9435f506089aa416e6045d47fb944` | PowerShell 5.1 scope; retained limitations |
| [P4 coverage owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | `3bdccfdf3e24d4a00fd9f2e600cee13e3eab94014def03a349023eb2bf3e4b6e` | Refusal-only P4 coverage; F-R1/O-R1, O-R2, O-R3 |
| [S2 qualification-record review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md) | `68a4830c16924fdfefa31e0df01c9f42ae336a106df1571048c676b7d1b2dc03` | `ValidId` grammar and association behavior |

External frozen inputs, locally present under `C:\Users\Laszlos\source\repos\Solo\` and hash-verified against the committed acceptances (all 15 F02 files match the acceptance-bound recheck manifest `5940b98b…`; the three Alternative B inputs match `896532d7…`, `3c4307f6…`, `9a1a195b…`). The files carrying D1 authority semantics:

| Frozen input | SHA-256 | D1 relevance |
|---|---|---|
| `case-policy-and-action-contract.md` | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | Normative K/M/F/S/R/Wc/P schemas, V/W/A chain steps 1–7 |
| `final-acceptance-hash-contract.md` | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` | CJSON-1 and administrative acceptance model |
| `mechanism-b-session-capability.md` | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | A live issuance, L verification, eligibility-only P |
| `trusted-actors-and-boundaries.md` | `dfc2188b811de50d96f8e1db117c10c15c9efc1603f486607bb2fd38538b201c` | Principal table for V, W, A and trust premises |
| `acyclic-authority-sequence.md` | `ed8dca1edb26b0ccde009b60f3f16148a67fed323fd47e9989b605397364d200` | Transition table; catalog subgraph line |
| `mechanism-a-consume-before-launch.md` | `8cbcdd5493fe4b86b680912d1fdf7ece5fb9bd662117798402d4c90f667d73d6` | Unselected durable-permit alternative |
| `minimum-implementation-contract.md` | `c4e73095cd1f05ee7410cc97bd26d8a8c94c09a0e9e14a47eb1c1840426234b5` | Privilege-drop and call-graph obligations |
| `negative-test-specifications.md` | `0900b7fe4049c941695b9dd9c421105b25f3b879bcdf2cad30239ea3aea76791` | Future refusal procedures (specifications only) |
| `owner-decision-proposal.md` | `f3b175f755e4319a428a452103f2d20e30f2f37c764685cc971b1c5f30545bc3` | Historical M01–M09 proposal (not authority) |
| `publication-withdrawal-race-matrix.md` | `1f5d9d651ccd2e88d5b406e0d1fe3a1e26d64c666ee8dded27099c4da98bc963` | Concurrency context (not adopted by D1) |
| `actor-and-descriptor-binding.md` | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | Installed-actor context |
| `remediation-notes.md` | `8618e4980ab641ae815a3f05ffc781208607ea70470784901641703ab6788ff4` | Historical remediation context |
| `validation-bootstrap-compatibility.md` | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | A/L/C/R/O/D notation; exact contract table |
| `validation-bootstrap-assessment.md` | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | Acceptance chronology for the frozen captions |
| `implementation-and-test-handoff.md` | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | Implementation/test boundary context |

Evidence classes: **direct** (verified first-hand here), **inherited accepted** (established by a committed owner acceptance, including the F02 design acceptance of the 15-file submission), **locally reverified** (recomputed against the committed identity), **qualified but sufficient** (bounded residual disclosed), **unsupported or contradictory** (none found for the relationships D1 adopts). Historical `PENDING` captions in the external drafts are labels of a superseded draft state; the later committed decisions govern the semantics they expressly accept, and D1 does not rewrite the frozen bytes.

---

## 3. P0 adopted-provenance reference

D1 relies on the owner-adopted P0 record for the exact E-2-scoped representations and identities:

- **Record:** `docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md`
- **Git blob:** `db25949d25babd474d00ae5b2f436e15507f22b3` (unchanged since adoption)
- **SHA-256:** `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6`
- **Adoption:** P0 owner acceptance at commit `5b2ef145088818c9b9b145d706816a075951c861`; scope limited to CJSON-1 and K/M/F/S/R/Wc/P; Q-E1 satisfied for the E-2 subset only.

P0 established provenance only. It did not approve a validation catalog, did not adopt a trust anchor, did not extend F02 authority, did not grant invocation or runtime authority, and did not authorize E-2 entry or implementation. D1 uses the record's exact field sets, bounds, cross-field bindings, roots, publication order, and acyclic graph as the accepted representation of the artifacts the approval chain approves. D1 does not alter, broaden, or re-adopt the record, and does not silently substitute another revision.

---

## 4. Exact K/M/F/S contract reconstruction

K, M, F, and S are used exactly as the accepted design uses them. No acronym expansion or new field meaning is introduced. The field-level definitions are the P0 record §§7–12 (extracted from `case-policy-and-action-contract.md` §§1/3/4), whose source bytes are accepted through the F02 owner acceptance §2.

| Property | K — protected catalog | M — fixture manifest | F — fixture policy | S — action script |
|---|---|---|---|---|
| Accepted definition | Catalog binding case, phase, topology, candidate/host identity, qualification association, closure hashes, session bound, release flag | Fixture manifest: B2 empty list; B3 exactly four fixed roles with exact bytes/metadata | Fixture policy: B2/B3 variant, host paths, copy/invocation/sandbox/resource policy, release/session bounds | Finite action script: exactly one of three fixed templates; no arbitrary command |
| Artifact identity | `Hk = SHA256(CJSON1(K))`; `schemaVersion`=1; exact key set (P0 record §7.1) | `Hm = SHA256(CJSON1(M))` | `Hf = SHA256(CJSON1(F))` | `Hs = SHA256(CJSON1(S))`; per-action `Ai` |
| Provenance | P0 record §7; accepted source `667e6118…` §1 | P0 record §8; source §3 | P0 record §9; source §3 | P0 record §10; source §4 |
| Issuer / producer | The catalog's producer/administrator constructs the candidate; construction is not approval | Producer constructs; no self-approval | Producer constructs; no self-approval | Producer constructs; no code evaluation |
| Exact binding | References `Hm`, `Hf`, `Hs`; contains no R/Wc/P reference and no self/approval hash | F references `Hm`; S and K reference `Hm` | S and K reference `Hf` | K references `Hs`; S references `Hm`, `Hf` |
| Approval role | **None in K** — K is the object reviewed and approved, not an approver | None | None | None |
| Consumer | V reviews it; L/C/R/O verify it; P binding selects it | F/S/K and the fixture-source checks | L/C/R/O and owner preparation | C dispatches only the exact mapped action |
| Failure behavior | Unknown/duplicate/missing key, non-canonical bytes, hash mismatch, cross-field mismatch refuse | B2 latent objects refuse; role/digest/length/mode mismatch refuse | B2 non-null objects refuse; B3 deviations refuse | Unknown action or release in a false case refuses the whole case before effects |
| Prohibited promotion | K is not P and not approval; no Hr/Hw/Hp inside K | M is not approval | F is not approval; `PendingQualification` tags are not relabeled | S is not code and does not approve anything |

**Which artifacts participate in the catalog approval chain.** M, F, S, and K are the *approved substrate*: the subject matter of the review and the transitive content bound by `Hk`. None of them performs an approval step, and none of them can approve itself. V, W, and A produce the detached approval records R, Wc, and P. The `M -> F -> S -> K` sequence is a construction and binding order, not an authority chain. The acyclic graph proves consistency and non-circularity only; it does not establish approval authority by itself, which is why D1 adopts the authority relationships expressly in §§5, 7, and 12.

---

## 5. V/W/A role mapping

D1 explicitly establishes the mapping that prior decisions deferred (E-2 entry decision §11 item 9; P0 record §11.6; role-map §Authority boundaries).

**Notation.** The Alternative B/Slice E notation is `A/L/C/R/O/D`: **A** = independent root administrator/authorizer; **L** = fixed launcher and continuing non-root lifetime guard; **C** = original controller; **R** = original recovery; **O** = original owner; **D** = non-root test conductor (`validation-bootstrap-compatibility.md` line 15). The catalog-contract notation is **V** = independent reviewer; **W** = owner accepting exact bytes; **A** = root administrator (`case-policy-and-action-contract.md` §1; `trusted-actors-and-boundaries.md` lines 24–26).

**Mapping table (source-grounded).**

| # | Property | V — independent reviewer | W — owner acceptance | A — root administrator |
|---|---|---|---|---|
| 1 | Exact normative meaning | Examines exact candidate/evidence and concludes gate completeness; must be independent of the producer for the approval in question (`trusted-actors` line 24) | Accepts specified evidence/scope and issues detached owner receipt Wc; acceptance is administrative, not a native observation (`trusted-actors` line 25; case-policy step 4) | Verifies actual V/W provenance, provisions protected approvals, and explicitly authorizes each live validation invocation (`trusted-actors` line 26; case-policy step 5) |
| 2 | Nature | Administrative review authority producing evidence | Administrative owner-acceptance act producing evidence | Administrative root authority producing protected approval |
| 3 | Creation/issuance authority | Performs review under the owner-adopted process; no self-appointment; independence is a requirement, not a registered identity | The Gagamba owner (human) acting explicitly; Wc is the owner's administrative act | A separately authorized host-root administrator under the accepted trusted-root model |
| 4 | Required inputs | Completed K and its transitive exact bytes (M/F/S), raw evidence, exact identities | The exact K and R bytes for the same `Hk`/`Hr` | Exact K/R/Wc bytes with verified V/W provenance |
| 5 | Identity binding | Not cryptographically attested; role attribution and administrative provenance (F02 acceptance Q07; proposal M03 "No signing infrastructure needed") | Same; Wc binds exact hashes, not a key | Root-controlled protected files represent the administrator's decision (trust-store decision §"What the administrator approves") |
| 6 | Required approvals | None; V's conclusion alone neither publishes nor launches (`trusted-actors` line 24) | None beyond the review it accepts; Wc contains no P hash | A must independently verify actual V/W provenance; a producer hash or observation of candidate files is insufficient (case-policy step 5) |
| 7 | Permitted consumers | W and A read R as review evidence | A reads Wc; L opens it only through protected P | L consumes protected P; the owner/administrator may inspect the chain |
| 8 | Prohibited consumers | R is not executable authority; no runtime reads unprotected review text (case-policy step 6) | Wc is mechanism-neutral eligibility, not invocation or durable-permit authority (case-policy step 4) | P is eligibility only; A's live response is required per invocation; no standing approval; no `--approved-file` (mechanism-b line 15) |
| 9 | Failure behavior | A conclusion without complete evidence does not close the gate; missing/conflicting review refuses | Accepting bytes that do not match K/R refuses; Wc for another catalog refuses | Missing/unsafe/mismatched provenance refuses; failure of independent verification means no P |
| 10 | Relationship to K/M/F/S | Reviews completed K and its transitive bytes only | Accepts exact K/R bytes only; no edge back into K/F/S | Binds `Hk`/`Hr`/`Hw`; publishes P last |
| 11 | Relationship to catalog approval | Produces the review leg (R) of the approval chain | Produces the acceptance leg (Wc) | Produces the protected approval leg (P) and consumes R/Wc while doing so |
| 12 | Relationship to future positive validation | R is evidence; not a capability | Wc is eligibility; not a capability | P is eligibility; A's live challenge/response is the per-invocation authorization that follows eligibility |
| 13 | Authority status | Accepted design semantics (F02 15-file acceptance §2) applied under this D1 adoption; reviewer independence is procedural | Accepted design semantics; owner authority is inherent to the human owner and bounded by this decision | Accepted design semantics; D1 explicitly adopts A's catalog-domain role within the trusted-root model |
| 14 | Adoptable within D1 | Yes, with the independence qualification (§13) | Yes, with the scope limitation (§13) | Yes, for catalog approval; no new cryptographic anchor; live issuance mechanics remain downstream (§10, §18) |

**The explicit mapping ruling.** (a) External **A** and Slice E **A** are the same administrative role — the independent root administrator/authorizer — and D1 maps them onto each other explicitly for catalog approval; A remains an administrative authority, never an installed-process actor. (b) External **W** is the human owner acting administratively; it is **not** Slice E **O** (the installed owner process), and Wc is not owner-process RELEASE authority. (c) External **V** is an independent reviewer authority; it has no installed-process counterpart, and it is not L, C, R, O, or D. (d) L/C/R/O/D remain installed actors that consume the approval chain; none of them may approve, and L may not treat unprotected review text or candidate files as authority.

**Classification: `MAPPING ACCEPTABLE WITH QUALIFICATIONS`.** Every V/W/A relationship essential to catalog approval is now defined from accepted sources: who reviews (V, independent of the producer), who accepts (W, the owner), who provisions and authorizes (A, separately authorized root), what each produces (R, Wc, P), and what none of them may do (no producer-built P, no standing approval, no invocation authority). The qualifications are §13 items: reviewer independence is procedural and not cryptographically enforced; A is trusted root; reviewer/administrator designation and the act of owner acceptance are administrative process that future authorization must execute explicitly. These do not undermine the identity or legitimacy of the approving authority, because approval authority originates from the owner and the accepted trusted-root model, not from the reviewers' identities.

---

## 6. Approval-chain dependency graph and matrix

The accepted dependency graph (P0 record §11.5; case-policy §1; acyclic sequence line 26) is:

```text
M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability
```

- M/F/S/K form the approved substrate; R/Wc/P are the detached approval records; the live challenge/response and capability are the *invocation* stage, not approval.
- K contains no R/Wc/P reference; no edge returns into K/F/S; there is no hash fixed point.
- P binds `Hk`, `Hr`, `Hw`; P contains no self-hash; publication order is R, Wc, then P.

Approval-chain matrix:

| Approval step | Input artifact | Governing schema | Approver | Authority source | Required evidence | Verification rule | Scope | Rejection conditions | Downstream consumer | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| V review | Completed K + transitive M/F/S bytes | CJSON-1; contract §1 step 3 | V (independent reviewer) | Owner-adopted chain (§5, §7); F02 design acceptance | Detached R with `conclusion`=`supported-for-bounded-validation` and all duplicated values equal to K | Exact bytes; every field equals K; no owner hash embedded | Bounded validation suitability of that exact catalog | Mismatch, missing evidence, incomplete review | W (acceptance), A (verification) | Adoptable with qualifications |
| W acceptance | Exact K + R bytes | CJSON-1; contract §1 step 4 | W (owner) | Owner's administrative authority, bounded by this decision | Detached Wc with `decision`=`accept-exact-bounded-validation-catalog`; `catalogReviewSha256`=Hr | Exact bytes; every duplicated value equals K; no P hash | Mechanism-neutral catalog eligibility | Wrong target/scope/schema; missing review | A (verification) | Adoptable with qualifications |
| A protected approval | Exact K/R/Wc bytes | CJSON-1; contract §1 step 5 | A (separately authorized root administrator) | Trusted-root model + this explicit owner adoption | Protected P at `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` with `accepted`=true; R/Wc at `/etc/gagamba/validation-catalog-evidence/` | A independently verifies actual V/W provenance, computes Hk/Hr/Hw, compares every duplicated value; root:root 0444 no-replace + fsync | Eligibility for that exact catalog identity on that host/scope | Missing/false/mismatched provenance; substitute approval roots | L (protected verification) | Adoptable with qualifications |
| Protected publication | P and R/Wc files | Contract §1 step 5 | A | Same | Complete protected files | R and Wc first, P last under authority/install locks; no-replace; directory fsync; conflicting bytes refuse | Protected store state | Partial writes, conflicts, unsafe ancestors | L, C, R, O | Adoptable with qualifications |
| L protected verification | P, R, Wc, K/M/F/S, code/host inputs | Contract §1 step 6 | L (fixed launcher) | Accepted design | Protected P opened by selected digest; R/Wc hashed against P; strict parse and cross-compare | Every binding equal; unprotected review text has no authority; substitute roots forbidden | Live-invocation eligibility for the exact selected catalog | Any mismatch or unavailability refuses before effects | Live challenge | Implementation prerequisite |
| Live challenge / response | Current live invocation + protected chain | Contract §1 step 7; mechanism-b | A (one response) | Trusted root + per-invocation explicit authorization | Challenge SHA-256 displayed; exact CJSON-1 response with the three approver hashes and `action`=`authorize` | L validates root peer, exact outstanding challenge/deadline; one response; replay refused | One original L/C invocation only | Absence, expiry, withdrawal, replay, wrong pins | B1 issuance | Downstream (atomic Slice E) |
| Capability | Sealed capability after B1/B2 | mechanism-b | L issues; C consumes | Accepted design | Root:root sealed memfd; `Pending->Issued`; `Offered->Consumed` | Seals, hashes, original pins, latches | One execution/session/lease/case | Duplicates, expiry, burn rules | Original C/R/O | Downstream; implementation prerequisite |

**Stage separation.** The decision distinguishes and does not merge: catalog construction (producer) → structural validation (CJSON-1 strict decode) → integrity verification (hash/size/metadata) → provenance verification (V/W/A evidence) → owner approval (W) → catalog eligibility (P) → positive validation authority (downstream, not established) → invocation authorization (live challenge/response, downstream) → protected execution (separately gated). A favorable result at any stage does not establish a later one; the chain is adopted as a whole precisely because each stage has a distinct authority source.

---

## 7. Trust-anchor provenance analysis

**Determination.** Validation-catalog approval authority originates from the human owner's administrative authority, exercised through the accepted trusted-root administrative model, and is adopted explicitly by this owner decision. It does **not** originate from a cryptographic root, key, certificate, or signing service; none is created, and none is required by the accepted design. The F02 acceptance records that input/model identities are role attribution and prior-chat evidence, not cryptographic attestation (Q07), and the owner-decision proposal states that no signing infrastructure is needed within the trusted-root model (M03). The protected root files "represent the administrator's decision within B6's trusted-root model" (F02 trust-store decision).

| Claimed authority | Exact accepted source | D1 determination |
|---|---|---|
| Owner administrative authority | F02 acceptance §§1–3; trust-store decision; Alternative B decision §1; this decision | Accepted; inherent to the owner; bounded by the adopted scope |
| A (root administrator) over catalog approvals | `case-policy-and-action-contract.md` §1 step 5; `trusted-actors-and-boundaries.md` line 26; acyclic sequence | Accepted design semantics; **explicitly adopted** for the catalog domain by this decision |
| Protected roots as authority representation | Contract §1 step 5 (root:root 0444, no-replace, fsync); P0 record §12; mechanism-b line 23 | Adopted custody mechanism, not a new cryptographic anchor |
| V reviewer authority | `trusted-actors-and-boundaries.md` line 24; contract §1 step 3 | Accepted with a procedural independence requirement (§13) |
| W acceptance authority | `trusted-actors-and-boundaries.md` line 25; contract §1 step 4 | Accepted; administrative act; scope-bounded |
| F02 qualification-store authority | Trust-store decision; F02 acceptance §4 | **Does not extend** (§9); separate |
| Final-admission chain | F02 acceptance §4; acyclic sequence steps 11–14 | Separate chain; not reused |

**Protection mechanism actually adopted (non-cryptographic).** Root:root `0444` single-link protected metadata; root-controlled, non-group/other-writable ancestors; exact hash-derived leaf names with no search or `latest` selection; descriptor-relative no-follow acquisition with exact uid/gid/mode/link/size checks; no-replace publication with file and directory fsync; independent A verification of actual V/W provenance; L re-verification of P/R/Wc under the authority lock immediately before B1; one-response live latch; and fail-closed refusal on any mismatch.

**Delegation.** No delegation of approval authority is specified by the accepted materials. D1 therefore adopts a **no-delegation default**: only the owner (W) may accept, only a separately authorized root administrator (A) may provision P or authorize a live invocation, and only an independent reviewer (V) may produce R; any broader delegation, surrogate approval root, or substitute issuer would be a new authority relationship requiring its own owner decision (§13). Root/user/producer-supplied substitute approval roots are forbidden by the accepted contract.

**Replacement, supersession, revocation.** R/Wc/P are content-addressed; changed bytes create different objects. A different approval byte set for the same `Hk` is not silently adopted mid-attempt; reapproval requires independent explicit administrative action and a new invocation; existing sessions retain their original immutable inputs (contract §1 step 5 and final paragraph). Withdrawal of P is serialized with B1/A-consumption: removal first refuses new issuance; issuance first preserves that one session (contract §1 step 5). Live withdrawal uses the boot-scoped `/run` marker and refuses progression without retroactive revocation of an already issued session (mechanism-b §Withdrawal).

**Conflicting authorities, unknown issuers, cross-host.** Conflicting bytes refuse; unknown issuers have no protected representation and therefore no authority; approval is host/scope-specific with no cross-host reuse and no standing approval (entry decision §12; P0 record §9).

**Explicit distinction.** No existing accepted trust store covers validation catalogs. The F02 qualification store covers only `/etc/gagamba/accepted-qualifications/<qualificationId>.json`. D1 does not satisfy Q-E2 by implication from F02 or from hash agreement; it adopts the catalog authority explicitly, under the owner's administrative authority and the accepted trusted-root model, with the provenance, scope, custody, and failure semantics recorded here. This satisfies the entry decision's requirement that a new authority relationship be established explicitly rather than inferred.

**Trusted-root bounds (retained).** Malicious or colluding root, copied-host rollback, hostile same-UID memory access, and non-atomic root mutation remain outside the claimed guarantee; the observation-to-effect residual remains accepted (Alternative B §2; P4 acceptance). These are inherited premises and limitations, not defects introduced by D1.

---

## 8. Catalog identity and schema bindings

| Property | Adopted rule | Source |
|---|---|---|
| Catalog identity | K is identified by `Hk = SHA256(CJSON1(K))` over exact canonical bytes; leaf name derives only from `Hk` | P0 record §7; contract §1 |
| Governing frozen schema | CJSON-1 canonical encoding; `schemaVersion`=1; exact key set; 65,536-byte maximum; fail-closed parsing | P0 record §6–§7; hash contract §CJSON-1 |
| Serialization | Exact-byte canonical reserialization must equal input; duplicate/unknown/missing keys refuse; ASCII-only, sorted keys | P0 record §6 |
| K/M/F/S bindings | K references `Hm`, `Hf`, `Hs`; duplicated values must equal K and never override it | P0 record §§7–12 |
| R/Wc/P relationships | R binds `Hk`; Wc binds `Hk`+`Hr`; P binds `Hk`+`Hr`+`Hw`; P has no self-hash; no edge returns into K/F/S | P0 record §11; contract §1 |
| Approval target | The exact K bytes for `Hk`, with R/Wc/P bound to them; approval never targets a name, case label, or directory entry | Contract §1 steps 3–5 |
| Version identity | `schemaVersion`=1 plus the exact canonical rule set; a byte or rule change creates a different object and requires reapproval | P0 record §15 |
| Digest requirements | SHA-256, 64 lower-case hex; hashes over exact canonical bytes; `Ai`/`Hs` rules preserved | P0 record §§6, 10; hash contract |
| Unknown-field behavior | Unknown, duplicate, missing, or extra fields refuse before use; no extension fields | P0 record §6; contract §1 negatives |
| Substitution/rollback | Conflicting bytes for the same identity refuse; changed K after Wc with P retained → `Hk` mismatch; replay of an old Hp against a new/withdrawn approval refuses | Contract §1 negatives; P0 record §15 |
| Cross-version compatibility | None is promised; a catalog approved under one schema identity is not approved under another. Compatibility or replacement would require an exact accepted rule, which does not exist today | P0 record §15; this decision |
| Approval reuse | P is reusable **eligibility data** only for the exact catalog identity; every live invocation still requires a fresh independently authorized response; no standing approval; no cross-host reuse | Mechanism-b line 19; contract §1 step 7 |

A post-phase catalog additionally binds the actual selected S2 qualification ID and raw record hash and is constructed after that selection; it does not feed backward into the first F02 record (acyclic sequence line 26). A pre-phase catalog binds both qualifications null. No compatibility or downgrade path may move a catalog between phases, schemas, hosts, or qualification associations without a new approval.

---

## 9. F02 authority determination

**Classification: `SEPARATE AUTHORITIES`.**

F02 qualification-store authority and validation-catalog approval authority are distinct authority domains. The F02 trust-store decision approves a root-controlled administrative decision for one exact qualification record under `/etc/gagamba/accepted-qualifications/<qualificationId>.json`; it does not adopt the catalog review/acceptance/provisioning chain, the `/etc/gagamba/accepted-validation-catalogs` or `/etc/gagamba/validation-catalog-evidence` roots, the V/W/A roles, the R/Wc/P representations, or the catalog withdrawal/reapproval semantics (E-2 entry decision §6.1). F02 authority is store-specific and is not extended by analogy, by shared identity format, or by shared administrative primitives (entry decision §§11.8, 12.5; F02 acceptance §4; P0 record §§1.9, 14.4 item 6; role map line 47).

Consequences:

- The catalog chain is adopted under its own authority basis (§7) and its own protected roots; it does not reuse F02's approval files, does not add fields to F02 records, and does not alter any F02 rejection or trust-store guarantee.
- Catalog approval is not qualification-record association; P is not a qualification record; `qualificationId`/`qualificationRecordSha256` inside K bind the catalog to an S2 selection but do not confer S2 or final-admission authority.
- The separate final-admission chain (`/etc/gagamba/accepted-final-admissions`, `/opt/gagamba/final-admissions`, selectors) is not reused or modified.

No evidence was found that any accepted source explicitly relates F02 authority to validation-catalog approval; the relationship is determined to be separate, and D1 adopts the catalog authority independently.

---

## 10. Approval versus operational authority separation

The adopted model keeps one hard boundary: **approval is eligibility; execution is separately gated.**

Preserved boundaries:

1. **P is eligibility, not invocation.** P alone cannot issue or reissue a capability; the fresh live challenge, the one-response latch, and original process pins remain mandatory (P0 record §11.4; mechanism-b).
2. **Approved catalogs do not construct positive validation authority.** No `OwnerAuthorityContext.Validation`, no capability, no Boolean/selector/final-admission bypass is created by approval.
3. **E-1 integrity evidence is not catalog approval.** E-1 remains integrity-only and production-unreachable (§14).
4. **A valid approval chain is not a protected execution permit.** The live invocation remains subject to the atomic Slice E dependency set (plan §3.5).
5. **No producer-built P.** A must independently verify actual V/W provenance; candidate files, timestamps, and self-consistent hash sets cannot create approval.
6. **No standing approval.** Every live invocation requires explicit per-invocation authorization; there is no `--approved-file`, stored-response, or unattended path.
7. **No cross-host reuse.** Approval is host/scope-specific.
8. **R and Wc remain detached; K is not P; S is not code.**

The transition from approved eligibility to execution authority is owned by later milestones: the live issuance path (fixed launcher/custody, E-5; atomic enablement and R/O/C ordering, E-6; driver integration, E-7; GAVA1 direct context, E-4) and, for real protected behavior, Slice N native qualification. D1 does not implement, enable, or authorize that transition, and creates no generic trusted result that could be promoted into protected execution authority.

Stage ladder applied by this decision: integrity → provenance → approval → eligibility → invocation → protected execution. Each arrow requires its own accepted authority; this decision adopts the approval and eligibility stages only.

---

## 11. Fail-closed rejection requirements

The adopted chain is fail-closed at the invariant level: any missing, malformed, mismatched, unauthorized, or unavailable evidence refuses positive progression before effects. Exact error codes, exception types, and log formats are **not** specified here; encoding is deferred to implementation design, and the future implementation must add independent negative controls. Existing refusal-policy sources: `case-policy-and-action-contract.md` §1 steps 3–7 and substitution/cycle negatives; `mechanism-b-session-capability.md` §Replay and failure proof; `trusted-actors-and-boundaries.md` ("Missing mechanisms refuse"); `negative-test-specifications.md` ("Missing… is Pending/Failed, never Pass"); F-01/F-02 obligations; entry decision stop conditions.

| # | Failure class | Accepted contractual source | Required decision | Expected observable rejection (invariant) | Responsible future package | Required independent negative control | Currently specified? |
|---|---|---|---|---|---|---|---|
| 1 | Missing catalog | Contract §1 step 6; P0 record §12.2 | Refuse | No protected object found → no progression, no effects | E-2 implementation (protected readers) | Absent/removed K refuses | Invariant; encoding deferred |
| 2 | Unknown catalog identity | P0 record §12.2 (no search/`latest`); contract step 6 | Refuse | No name search, no fallback selection | E-2 implementation | Unknown `Hk`/path refuses without enumeration | Invariant; encoding deferred |
| 3 | Catalog/schema mismatch | Contract §1 negatives; P0 record §6 | Refuse | Schema/field/hash mismatch before use | E-2 implementation | Altered K bytes refuse | Invariant; encoding deferred |
| 4 | Unapproved catalog | Contract step 6; mechanism-b line 7 ("merely installing a catalog does not authorize") | Refuse | P absent or not matching → no live progression | E-2 implementation + live path | K without P refuses | Invariant; encoding deferred |
| 5 | Unknown approval issuer | Contract step 5 ("No tool may generate P"); step 6 (substitute roots forbidden) | Refuse | Unprotected/unverified provenance → no P consumed | E-2 implementation | Producer-built P/candidate refuses | Invariant; encoding deferred |
| 6 | Invalid trust anchor | Entry decision §12; F02 acceptance Q02; contract step 6 | Refuse | Unsafe/missing protected roots refuse; no weaker fallback | E-2 implementation | Substituted approval root refuses | Invariant; encoding deferred |
| 7 | Approval for another catalog | Contract steps 5–6; P binds `Hk` | Refuse | Cross-field mismatch → no progression | E-2 implementation | P/R/Wc for a different K refuses | Invariant; encoding deferred |
| 8 | Approval for another schema | P0 record §15; this decision §8 | Refuse | No cross-schema/cross-version reuse | E-2 implementation | Approval under another schema identity refuses | Invariant; encoding deferred |
| 9 | Incomplete approval chain | Contract §1 step 27 negatives; P0 record §11.4 | Refuse | Missing R/Wc/P or missing binding → refusal | E-2 implementation | Producer-built K/R/Wc without protected P refuses | Invariant; encoding deferred |
| 10 | Unauthorized delegation | This decision §7 (no-delegation default); contract step 6 | Refuse | Only owner/W and separately authorized A act; no surrogates | Owner process + E-2 implementation | Substitute/undelegated issuer refuses | Policy adopted; encoding deferred |
| 11 | Conflicting approvals | Contract step 5 (mid-attempt rule); P0 record §12.2 | Refuse | Conflicting bytes refuse; no silent adoption | E-2 implementation | Conflicting P/record bytes refuse | Invariant; encoding deferred |
| 12 | Stale approval | Contract steps 5, 7; mechanism-b withdrawal/expiry | Refuse | Withdrawn/expired/replaced → no new issuance/recheck fails | E-2 implementation + live path | Withdrawn or expired approval refuses | Invariant; encoding deferred |
| 13 | Malformed approval evidence | Contract §3 CJSON-1; hash contract | Refuse | Duplicate/unknown/non-canonical/overlong refuse | E-2 implementation | Malformed R/Wc/P refuses | Invariant; encoding deferred |
| 14 | Missing K/M/F/S binding | Contract step 6; P0 record §12.3 | Refuse | Missing/digest-mismatched bound object → refusal | E-2 implementation | Tampered M/F/S refuses | Invariant; encoding deferred |
| 15 | Substituted R/Wc evidence | Contract steps 5–6 | Refuse | L hashes named R/Wc against P → mismatch refuses | E-2 implementation + live path | Swapped R/Wc refuses | Invariant; encoding deferred |
| 16 | Unsupported F02 reuse | Entry decision §6.1; F02 acceptance §4; §9 above | Refuse | Catalog approval never derived from qualification-store files | E-2 implementation | F02 record presented as catalog approval refuses | Policy adopted |
| 17 | Unknown V/W/A role/mapping | This decision §5; entry decision §11.9 | Refuse | Roles must match the adopted mapping; no silent analogies | E-2 implementation + governance | Ad-hoc approver identity refuses | Mapping adopted; encoding deferred |
| 18 | Verifier unavailable | `trusted-actors` line 33; proposal M09; negative-test spec | Refuse | Missing mechanism/host state → refuse, never fallback | E-2 implementation + native path | Unavailable verifier/protected input refuses | Invariant; encoding deferred |
| 19 | E-1 integrity verification failure | F-01 acceptance §9; F-02 gate; §14 below | Refuse (fail closed) | No fallback/downgrade/partial authorization/continuation | Future E-1 consumer (none today) | Semantic/structural integrity failure refuses | Closed obligation; no consumer |

No failure class was found whose refusal policy itself is missing; all are covered by accepted invariants. Items 10, 16, and 17 add D1-adopted policy (no delegation; separate F02; explicit mapping). Where the accepted contracts establish a mandatory refusal but not its encoding, the refusal invariant is binding and the encoding is an implementation-design deliverable that must arrive with independent negative controls before the first protected effect.

---

## 12. Adopted or rejected authority relationships — owner decision matrix

Exactly the following relationships are adopted; nothing else is adopted by implication.

| # | Item | Accepted normative basis | Current evidence | Owner ruling | Binding qualification | Unresolved prerequisite | Future implementation owner |
|---|---|---|---|---|---|---|---|
| 1 | V/W/A mapping | Contract §1; `trusted-actors` lines 24–26; Alternative B notation line 15 | Roles, evidence states, and constraints fully defined | **ADOPTED WITH QUALIFICATIONS** (mapping table §5): V reviewer, W owner acceptance, A root administrator; external A = Slice E A explicitly; W ≠ O; V has no installed counterpart | Reviewer independence is procedural; A is trusted root | Reviewer/administrator designation process | Governance (owner) + E-2 implementation |
| 2 | Approval-chain authority | Entry decision §8; F02 design acceptance §2; §7 above | Authority originates from the owner under the trusted-root model | **ADOPTED**: owner-adopted catalog approval authority for the E-2 scope | Non-cryptographic; administrative | None for adoption | Governance; later provisioning |
| 3 | Trust-anchor legitimacy | Entry decision §12; F02 Q02/Q03/Q07; §7 above | No cryptographic anchor; protected root administrative model | **ADOPTED**: no new cryptographic anchor; catalog-domain authority explicitly adopted; no implication from F02 | Malicious root/rollback outside model | Protected-root provisioning (later) | Owner + A + E-2 implementation |
| 4 | Catalog identity | P0 record §7; contract §1 | `Hk` over CJSON-1 K; exact fields | **ADOPTED** | No search/fallback naming | Reader implementation | E-2 implementation |
| 5 | Catalog-to-schema binding | P0 record §§6–8, 15 | Exact schema version, canonical rules, cross-field equalities | **ADOPTED**: approval binds exact bytes and schema identity; no cross-schema reuse | Compatibility rules absent → refuse | None | E-2 implementation |
| 6 | K/M/F/S relationships | P0 record §§7–12; entry decision §11 | M→F→S→K; K contains no approval hash | **PRESERVED/ADOPTED** | No role expansion | Reader/verifier work | E-2 implementation |
| 7 | R/Wc/P boundaries | P0 record §11; contract §1 | Detached records; P eligibility only | **PRESERVED/ADOPTED** | No producer-built P; no collapse | Protected store provisioning | E-2 implementation + live path |
| 8 | F02 authority relationship | Entry decision §6.1; F02 acceptance §4; trust-store decision | F02 is store-specific; no cross-authorization | **`SEPARATE AUTHORITIES`** | No extension by analogy; no shared authority | None | Governance; preserved |
| 9 | Approval delegation | §7 above; contract step 6 | No delegation specified | **NOT ADOPTED (default: no delegation)** | Any delegation requires its own owner decision | None for E-2 | Governance |
| 10 | Rejection / fail-closed | §11 table; contract; mechanism-b | Invariants defined; encodings deferred | **ADOPTED at invariant level** | Encoding + negative controls required before effects | Implementation design | E-2 implementation + independent qualification |
| 11 | Catalog approval scope | Contract §1; entry decision §12 | Host/scope/phase-specific; bounded | **ADOPTED**: eligibility for the exact catalog identity and validation purpose only | No standing approval; no cross-host reuse | None | E-2 implementation |
| 12 | Operational-capability separation | Entry decision rules 5–8; P0 record §11 | P ≠ invocation; no generic trusted result | **ADOPTED**: hard boundary preserved | Live response mandatory; no promotion | Atomic Slice E dependencies | E-5/E-6/E-7 + Slice N (separate) |
| 13 | Downstream implementation prerequisites | Plan §3.5; entry decision §15; §10 above | Slice E is atomic; no implementation exists | **DEFERRED to the fresh entry and implementation decisions** | No partial exposure; E-4/E-6 P4 re-audits | Fresh E-2 entry decision; separate implementation commission | E-2 implementation; E-4/E-6; Slice N |

No relationship beyond this matrix is adopted. In particular, D1 does not adopt: any concrete catalog, any approval instance, any live capability, any E-1 consumer, any runtime read of a catalog, any positive validation path, any durable permit (Mechanism A remains unselected), any F02 extension, or any change to the final-admission chain.

---

## 13. Binding qualifications

Each retained item is classified and specified.

1. **Reviewer independence is procedural, not cryptographically attested** — `ACCEPTED LIMITATION`.
   - Property: identity/legitimacy of the V review.
   - Limitation: there is no key, certificate, or registry that proves V's independence; it rests on administrative process and on A/W verifying actual provenance.
   - Non-blocking because V's conclusion alone is not authority; W and A independently accept and verify.
   - Resolution milestone: governance (reviewer designation for each approval) and E-2 implementation evidence handling.
   - Evidence for closure: per-instance administrative record showing V independent of the producer, plus W/A verification.
   - Blocks production? No, provided the administrative process is executed.
   - Owner re-approval? Not required for the model; the owner performs the act itself.

2. **A is trusted root; malicious/colluding root, copied-host rollback, and hostile same-UID access are outside the model** — `ACCEPTED LIMITATION`.
   - Property: trust-anchor bounds.
   - Limitation: inherited from the accepted trusted-root premises (F02 Q02/Q07; Alternative B §5).
   - Non-blocking: it is the accepted security model, not a defect.
   - Resolution milestone: none (permanent non-claim); Slice N can measure behavior, not root honesty.
   - Evidence: preserved non-claims in this decision and P4 acceptance.
   - Blocks production? It bounds claims, not the chain.
   - Owner re-approval? Not required unless the trust model itself is changed.

3. **Protected catalog roots are unprovisioned and the chain is unimplemented; the live path is atomic** — `IMPLEMENTATION PREREQUISITE`.
   - Property: custody and enforcement.
   - Limitation: no `/etc/gagamba/accepted-validation-catalogs` or `/etc/gagamba/validation-catalog-evidence` store exists; no reader, verifier, launcher, or capability exists.
   - Non-blocking for adoption: D1 adopts authority, not implementation; the entry decision requires exactly this separation.
   - Resolution milestone: the fresh E-2 entry assessment and a separately authorized bounded implementation package; Slice E remains atomic (plan §3.5).
   - Evidence for closure: implemented readers/verifiers with negative controls and independent qualification under the fresh entry scope.
   - Blocks production? Yes until implemented and qualified.
   - Owner re-approval? Yes: a fresh E-2 entry owner decision plus implementation authorization.

4. **Refusal encodings and error semantics are unspecified** — `IMPLEMENTATION PREREQUISITE`.
   - Property: fail-closed behavior encoding.
   - Limitation: accepted contracts mandate refusal invariants but not codes/exceptions/log formats.
   - Non-blocking: invariants are defined (§11); encoding is an implementation detail that must preserve the invariants.
   - Resolution milestone: E-2 implementation design + independent negative controls.
   - Evidence for closure: negative-test results mapping each invariant to an observable refusal.
   - Blocks production? Yes until demonstrated.
   - Owner re-approval? Not for encoding, provided invariants are preserved.

5. **Reviewer/administrator designation and owner-acceptance execution process** — `IMPLEMENTATION PREREQUISITE`.
   - Property: who performs V/W/A acts per instance.
   - Limitation: the model defines roles and constraints, not named persons or tooling.
   - Non-blocking: these are administrative executions under the adopted model.
   - Resolution milestone: process adoption before any real approval; E-2 implementation supplies evidence handling.
   - Evidence for closure: executed administrative records.
   - Blocks production? Yes until performed for any real catalog.
   - Owner re-approval? No; the owner performs W and designates A under the adopted scope.

6. **Delegation of approval authority is not adopted** — `ACCEPTED LIMITATION`.
   - Property: scope of who may approve.
   - Limitation: no sub-delegation or surrogate issuer exists.
   - Non-blocking: the default is safe and matches the accepted contract (substitute roots forbidden).
   - Resolution milestone: none required for E-2; any future delegation is a new owner decision.
   - Evidence: this decision §7.
   - Blocks production? No.
   - Owner re-approval? Yes if delegation is ever desired.

7. **Mechanism A durable permits are unselected and out of E-2 scope** — `DOWNSTREAM DEFERRED`.
   - Property: durable issuance alternative.
   - Limitation: only Mechanism B (live one-shot) is selected (F02 acceptance Q01); the durable path would require its own owner-approved durable-grant contract and protected permit.
   - Non-blocking: not required for catalog approval.
   - Resolution milestone: a separate owner decision if durable grants are ever wanted.
   - Evidence: mechanism-a and case-policy step 5; this decision.
   - Blocks production? No.
   - Owner re-approval? Yes for any durable-grant adoption.

8. **Native behavior and protected-store operation remain unqualified** — `INDEPENDENT QUALIFICATION OBLIGATION`.
   - Property: real host behavior of protected reads, roots, launcher, and stop paths.
   - Limitation: `NATIVE-PENDING`; 331 skips are not passes.
   - Non-blocking for adoption.
   - Resolution milestone: Slice N on a separately authorized disposable host after implementation.
   - Evidence for closure: exact candidate/host/kernel/runtime bindings and mandatory native cases.
   - Blocks production? Yes.
   - Owner re-approval? Slice N has its own authorization.

9. **P0 extraction completeness** — `ACCEPTED LIMITATION` (inherited).
   - Property: field-by-field completeness of the adopted representations.
   - Limitation: the P0 record's extraction is source-bound, section-mapped, and spot-checked, but not fully re-derived field-by-field (P0 acceptance §11 item 4).
   - Non-blocking: approval binds exact bytes and hashes; a discovered extraction error would be a correction, not an authority failure.
   - Resolution milestone: any fresh entry/implementation review may re-derive; corrections are additive per P0 record §15.
   - Evidence: P0 acceptance.
   - Blocks production? No, subject to correction if found.
   - Owner re-approval? Only if the adopted record is corrected or replaced.

10. **Single-reviewer governance recording** — `ACCEPTED LIMITATION`.
    - Property: review independence of this decision itself.
    - Limitation: not multi-model or human-signed.
    - Non-blocking: the decision grounds every authority claim in committed accepted sources.
    - Resolution milestone: none required; independent qualification remains required for implementation.
    - Evidence: this record.
    - Blocks production? No.
    - Owner re-approval? No.

11. **E-4/E-6 P4 re-audits and downstream provenance** — `DOWNSTREAM DEFERRED`.
    - Property: later milestone obligations.
    - Limitation: GAVA1, v3-pre, and release correlation remain within composite Q-E1; E-4/E-6 require fresh P4 re-audits.
    - Non-blocking for the approval chain.
    - Resolution milestone: those milestones' own decisions.
    - Evidence: entry decision §15; plan §3.5.
    - Blocks production? Yes for those milestones.
    - Owner re-approval? Yes, as already required.

No qualification undermines the identity or legitimacy of the approving authority: authority is grounded in the owner's administrative act and the accepted trusted-root model, with the exact scope, custody, and failure semantics recorded in §§7–12.

---

## 14. E-1 integration exclusions

E-1 remains accepted with qualifications and production-unreachable. D1 cites E-1 only as a future integrity-evidence dependency and introduces no consumer.

- **F-01:** owner role-hash binding (`OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1)`), recovery role-hash binding (`RoleHash(manifest, 0)`), runtime-inventory binding (`platform.runtimeInventorySha256`), and the binding future-consumer obligation that every integrity-verification exception or failure mode must fail closed with no fallback, downgrade, partial authorization, or continuation into a protected effect.
- **F-02:** Q1-F01 through Q1-F06 preserved (call-preserving bypass limitation, branch-dominance limitation, call-argument/helper coverage limitation, ordering-versus-dominance, commit-bound artifact identities, non-exhaustive mutation evidence), the first-consumer security review required, Option A consolidation a mandatory reassessment at the accepted gate, and targeted E-4/E-6 parity and authority reviews.
- **F-03:** Windows PowerShell 5.1 qualification, PowerShell 7 unexecuted and unqualified, existing negative-control nonzero-exit semantics, and scoped `$ErrorActionPreference` restoration; verification tooling is not modified.

No E-1 consumer, verification-path refactor, or authority promotion is authorized by this decision.

---

## 15. P4 and RELEASE preservation

This D1 owner decision is documentation-only and classified **P4-NEUTRAL**. The commit contains this decision and the three current-state documentation updates; no executable production or test change, no protected-sink caller change, no authority-gate change, and no RELEASE change.

Preserved: protected-sink caller restrictions; F01/F02 authority gates; S10/S11 domination; S28 isolation; the first protected effect; qualified-to-legacy downgrade refusal; F-R1/O-R1; O-R2; O-R3; the observation-to-effect residual; `NATIVE-PENDING`; all 331 unqualified native skips (not passes); and RELEASE test-hook isolation. No native positive qualification is claimed. E-4 and E-6 retain their separately required fresh P4 re-audits before any protected effect.

---

## 16. Formal D1 owner disposition

**D1 APPROVAL CHAIN ADOPTED WITH QUALIFICATIONS — Q-E2 SATISFIED FOR E-2 SCOPE.**

The validation-catalog approval-chain authority is established by accepted evidence and is explicitly adopted by this owner decision for the E-2 scope, with the bounded qualifications in §13. No material authority, trust-anchor, or binding gap was found that undermines the adopted model. This disposition does not approve any concrete catalog, does not create any approval instance or protected store, does not authorize implementation, and does not grant execution authority.

---

## 17. Exact Q-E2 status

**Q-E2 is satisfied for the E-2 scope**, subject to the §13 qualifications, because this decision establishes each completion criterion required by the E-2 entry owner decision §8.5:

1. **Who may approve each artifact, and under what authority:** V reviews, W accepts, A provisions P under the owner's administrative authority and the accepted trusted-root model (§§5, 7, 12).
2. **Exact accepted representations and cross-field bindings:** K/M/F/S/R/Wc/P with `Hk`/`Hr`/`Hw`/`Hp` and all equalities (§§4, 6, 8; P0 record).
3. **Protected roots and custody requirements:** exact roots, root:root `0444`/`0755`, no-replace + fsync, descriptor-relative no-follow reads, independent acquisition (§§7–8).
4. **Publication, failure, withdrawal, and reapproval semantics:** R/Wc first, P last; fail-closed table (§11); withdrawal serialization and no mid-attempt substitution (§§7–8).
5. **Live-binding separation:** P is eligibility only; fresh challenge/response and one-response latch mandatory (§10).
6. **Non-extension of F02 and non-reuse of the final-admission chain:** `SEPARATE AUTHORITIES` (§9).
7. **Threat model and non-claims:** trusted-root bounds, non-cryptographic administrative provenance, retained residuals (§7, §13).
8. **Binding scope of adoption:** exact scope and exclusions (§12, §§17–19).

Scope of this satisfaction: the approval-chain governance for the validation-catalog domain within the E-2 scope. It does not approve a catalog, does not implement the chain, does not provision roots, does not resolve GAVA1/v3-pre/release correlation, and does not make E-2 entry or implementation automatic.

---

## 18. Remaining Q-E1 downstream exclusions

D1 does not resolve or close the remaining composite Q-E1 items: the GAVA1 header and exact wire layout, the v3 validation-pre layouts/status values/echo fields/bootstrap bounds, and the release-correlation formula for E-3/E-4. D1 does not extend the P0 adoption scope. The fresh E-2 entry assessment must expressly determine whether any of these items is a dependency of the proposed first E-2 package, as required by the E-2 entry owner decision §10; absent that express determination, they remain outside E-2 and open.

The live invocation mechanics that follow eligibility (fixed launcher, custody, GAVA1 direct context, atomic enablement, driver) belong to the atomic Slice E dependency set and remain untouched: E-3 through E-7 and Slice N stay unauthorized, and no partial exposure is permitted (plan §3.5).

---

## 19. Fresh E-2 entry prerequisites

D1 is not the fresh entry decision. Even with Q-E2 satisfied, E-2 entry and implementation remain unauthorized. The required sequence is:

```text
D1 owner decision (this document)
  -> fresh E-2 entry assessment
  -> fresh E-2 entry owner decision
  -> separately commissioned bounded implementation
```

The fresh entry assessment must confirm:

1. Q-E1 remains satisfied for the E-2 subset (P0 record identity unchanged).
2. Q-E2 has an explicit owner adoption (this decision) with its qualifications.
3. Exact adopted artifact identities remain unchanged.
4. K/M/F/S and V/W/A mappings remain consistent with this decision.
5. Trust-anchor authority is grounded (no new anchor; adopted model).
6. F02 authority remains correctly scoped (separate).
7. Approval-chain fail-closed requirements are defined (invariants) and encodings will arrive with the implementation.
8. E-1 qualifications remain intact.
9. No unauthorized E-1 consumer exists.
10. P4 remains refusal-only.
11. RELEASE and native limitations remain explicit.
12. The first implementation work package is independently qualifiable, including its negative controls and its atomic dependencies.

Only that later owner decision may authorize a bounded E-2 implementation package.

---

## 20. Recommended next commission

With Q-E2 satisfied for the E-2 scope, the recommended next (not automatically started) commission is:

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-ENTRY-REASSESSMENT-01`** — a fresh architectural entry assessment using the P0 owner-adopted provenance record, this D1 approval-chain decision, the existing E-1 qualifications, the accepted P4 refusal-only constraints, and the established E-2 scope and downstream exclusions. It must determine whether a bounded first E-2 implementation package can be proposed for separate owner authorization, and it must not begin implementation.

No further action is taken here. This decision stops after its own commit; the fresh entry assessment, E-2 implementation, catalog implementation or approval, positive validation, E-3 through E-7, Slice N, activation, synchronization, and push remain unauthorized.


