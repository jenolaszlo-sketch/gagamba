# ALT-B Slice E-2 P0 owner acceptance — frozen-schema provenance

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P0-OWNER-ACCEPTANCE-01`

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push. This is a single-reviewer governance recording using committed repository evidence and locally available frozen inputs; it is **not** independent multi-model qualification, external attestation, or a human signature.

This decision issues the owner acceptance for the E-2-P0 frozen-schema provenance reconciliation (`PROVENANCE RECONCILED — READY FOR OWNER ACCEPTANCE`, P0 report §17). The formal owner disposition is stated exactly once in §11. This decision does not start D1, does not adopt the validation-catalog approval chain, and does not authorize E-2 entry or implementation.

---

## 1. Verified repository baseline

The baseline was verified before any edit. General state:

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `77101ac18c0e019a9d32712e9fee78b468cc8d02` |
| Starting tree | `98c238035485d82d5df04281c4a6a4480d644232` |
| Worktrees | exactly one canonical checkout: `C:/Users/Laszlos/source/repos/Gagamba 77101ac [main]` |
| Tracked / index state | clean; no staged, modified, or deleted tracked path |
| Historical untracked paths | exactly 44 |
| Historical content comparison | 44 of 44 present; per-file SHA-256 match against `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`; `diff_count=0` |
| Preservation lines digest | `de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1` |
| Preservation map digest | `b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 17 commits ahead of `origin/main`; no push |
| P0 commit | `77101ac18c0e019a9d32712e9fee78b468cc8d02`, direct child of `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`; changed exactly 13 documentation files (2,045 insertions) |
| Network / package / cloud | none used; no fetch, pull, push, or synchronization |
| `src` / `tests` / `eng` change | none for this acceptance; the P0 commit changed documentation only |

Ancestry was verified with `git merge-base --is-ancestor` (all exit 0): E-2 entry owner decision `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`; E-2 entry assessment `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d`; P4 coverage acceptance `2d39380d0c0b61753097c197ce7c1df03cdddcb7`; E-1 acceptance `8bd71425eb31b69290affa2b86c78afce1bd0882`; F-01 closure `1ed1fc706ea76954e4f1d7d43673048ef82072ec`; F-02 closure `e86254e60fb3ded770156519e1d3e15e73bf36fc`; F-03 closure `6d08ae569c03e96872fcc5d330435991952207d6`; Slice E entry `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e`; Alternative B decision `d804d9062bab5d9d540df5c2d64f1b2c31bdd667`; F02 Mechanism B acceptance `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1`; S2/F02 trust-store decision `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6`; native-input freeze `504b6d06ac88591072b11f428ae88cade390c075`. No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was performed.

---

## 2. Authoritative evidence inventory

The principal records reconciled for this decision are committed at or before the P0 acceptance baseline and are ancestral to it. Full hashes, sizes, and paths for 26 entries are in `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/record-hashes.txt`.

| Record | SHA-256 | Role |
|---|---|---|
| [E-2 entry owner decision](rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md) | `ee0b2b81682e266488f59e0abcb870664ba25eaa0048764f885b44ae72793c79` | Authorizes P0/D1; Q-E1 completion criteria and stop conditions |
| [E-2 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md) | `f4212a8a1302695dae53a763682c0dc79de3a47af02e29b14abffbec96e1b664` | Accepted `RECONCILIATION REQUIRED`; minimum P0 content |
| [P0 report](../reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md) | `513d395d9cb6f6591e62a20732a33c2f4c78018865f0407cf28c2b3b962d2f85` | Producer reconciliation and single disposition |
| [Proposed provenance record](../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md) | `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` | E-2-scoped record submitted for adoption |
| [P0 evidence README](../reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/README.md) | `efd7ea81a24eece6a8790f137cd2b5d32b0de8a24f72ef3be0170cff704b2756` | Evidence index and status |
| [F02 Mechanism B owner acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e` | Accepts the exact 15-file submission with Q01–Q10 |
| [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361` | Binds the three validation-context inputs (§4) |
| [F02 trust-store owner decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | `e318125f91e20350824aeb70042b35a37c33d54fcde8350a4d353dccfaeb0adb` | Store-specific qualification authority; `ValidId` association path |
| [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) | `27de68690cca7027d46b0692e52e5da71641e38e13c4037fd31937d08f823b93` | E-1 accepted with qualifications |
| [F-01 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) | `30205d32409c097624da25cfd9ed6608f1588d79f3af96231125da06e6ad194b` | Role-hash bindings; future-consumer fail-closed obligation |
| [F-02 parity owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | `00f39ae2a1fc1675fa195c3af0ba040fdd05eed956e497f5e0c6fa5285a56107` | Q1-F01–Q1-F06; future production-integration gate |
| [F-03 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) | `44c85e0062c42ddec6e98a74268d71f358a9435f506089aa416e6045d47fb944` | Windows PowerShell 5.1 scope; retained Q1-R1/R2/R3 |
| [P4 coverage owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | `3bdccfdf3e24d4a00fd9f2e600cee13e3eab94014def03a349023eb2bf3e4b6e` | Refusal-only P4 coverage; F-R1/O-R1, O-R2, O-R3 |
| [S2 qualification-record review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md) | `68a4830c16924fdfefa31e0df01c9f42ae336a106df1571048c676b7d1b2dc03` | In-repository `ValidId` grammar reference |

External frozen inputs verified locally for this decision (under `C:\Users\Laszlos\source\repos\Solo\`): the F02 recheck manifest `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`; the focused recheck `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6`; all 15 manifest-listed files (zero mismatches; including `case-policy-and-action-contract.md` `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7`, `final-acceptance-hash-contract.md` `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652`, `mechanism-b-session-capability.md` `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010`); the three Alternative B inputs `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb`, `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced`, `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054`; and the six acceptance support objects recorded in the evidence package (zero mismatches).

Evidence classes used in this decision: **direct** (verified first-hand here), **inherited accepted** (established by a committed owner acceptance), **locally reverified** (recomputed here against the committed identity), **qualified but sufficient** (bounded residual disclosed and accepted), and **unsupported or contradictory** (none found).

---

## 3. Committed P0 record integrity

The P0 producer reported: 26 record hashes verified before commit; zero mismatches; nine committed P0 deliverable blobs verified after commit; zero blob mismatches; `record-hashes.txt` intentionally not self-hashed; all relative Markdown links resolved; all 44 historical untracked paths preserved. Independent reconciliation of the committed artifacts:

1. **Manifest and method.** `record-hashes.txt` contains 26 SHA-256 entries — 17 committed repository records and 9 P0 deliverables — in the format `sha256 <TAB> size <TAB> repo-relative-path`, measured with `Get-FileHash -Algorithm SHA256` (lower-case hex, size in bytes). The self-exclusion is explicit and intentional: “The record-hashes file itself is intentionally not self-hashed” (line 42).
2. **Working-tree recomputation.** All 17 committed records and all 9 P0 deliverables recompute to the manifest values from the current checkout (0 mismatches), which reproduces the producer's pre-commit 26-hash claim.
3. **Committed blob recomputation.** All 9 P0 deliverable blobs match the manifest at the committed-blob level. 16 of 17 record blobs also match at the blob level. The single difference is `docs/remediation-evidence-standard.md`: its manifest value is the checked-out byte sequence (10,959 bytes, CRLF) while the committed blob is the normalized LF sequence (10,446 bytes); the two are content-identical modulo CRLF (`i/lf w/crlf` under `core.autocrlf=true`). The manifest measures working-tree bytes as documented and does not claim Git-blob identity for that governance record; no content contradiction exists, and no E-2 schema-bearing artifact is affected (the E-2-relevant records are LF and match at both levels).
4. **No mutable reference treated as an immutable anchor.** The record's anchors are the acceptance-bound F02 manifest/recheck, the three Alternative B input hashes, the committed owner decisions, and the committed P0 evidence; the external package itself is consistently disclosed as external, local-only, and content-manifest-identified.
5. **Link resolution.** 47 relative Markdown links across the P0 report, the proposed record, and the evidence README were checked; 0 broken.
6. **Single producer disposition.** `PROVENANCE RECONCILED — READY FOR OWNER ACCEPTANCE.` appears exactly once (report line 660, §17, with a U+2014 em dash); the adjacent explanatory sentence is not a second disposition.
7. **Report/record agreement.** The artifact tables in the report and the record contain zero cross-document hash conflicts; both bind the same F02 manifest/recheck, the same three Alternative B inputs, and the same committed acceptance chain.
8. **Targeted spot-checks against the accepted bytes.** Distinctive literals in the accepted `case-policy-and-action-contract.md` were confirmed in place: the graph `M -> F -> S -> K -> R -> Wc -> P`, `supported-for-bounded-validation`, `accept-exact-bounded-validation-catalog`, `catalogReviewSha256`/`ownerCatalogAcceptanceSha256`/`catalogApprovalSha256`, `b2-lifeline-v1`/`b3-static-fixture-v1`, `gagamba-validation-action-v1`, `/etc/gagamba/accepted-validation-catalogs`, `/opt/gagamba/validation-catalogs`, and the K prohibition paragraph “no review, owner-decision, acceptance-receipt or self-hash fields” with its exact key list. `final-acceptance-hash-contract.md` contains the CJSON-1 section, the 65,536-byte limit, lower-case hex conventions, and the self-approval rule; `mechanism-b-session-capability.md` contains the three approval hashes and the `Pending -> Issued` / `Offered -> Consumed` latches. Full field-by-field re-derivation from the external text was **not** performed in this bounded owner-decision task; the extraction is recognized as qualified but sufficient (§4).
9. **Preservation.** The 44 historical untracked paths and their bytes are unchanged (digests in §1).

Classification of the counted items: the 17 “committed repository records” are governance documents; the 9 “P0 deliverables” are the report, the proposed record, and seven evidence files; the external-input hashes are the 15 F02 files plus manifest/recheck plus 3 Alternative B inputs plus 6 support objects; the schema artifacts are the external normative texts from which the record extracts CJSON-1 and the K/M/F/S/R/Wc/P representations. **The 26-hash count does not represent 26 independent frozen schemas.** The producer's integrity claims are reproducible and accurate as stated, and no contradiction was found.

---

## 4. Artifact-by-artifact provenance assessment

Assessment of each E-2-scoped representation against the accepted external bytes and the committed acceptance chain. “Identity” is the in-repo extraction controlled by the proposed record; the underlying frozen text remains external and is identified by hash.

| Artifact | Identity and normative source | Digest and current bytes | Origin acceptance | Scope and dependencies | Restrictions and residual gaps | Evidence class |
|---|---|---|---|---|---|---|
| CJSON-1 | Canonical encoding rules; `final-acceptance-hash-contract.md` §“Canonical encoding CJSON-1” | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652`; local bytes match; rules spot-checked | F02 Mechanism B owner acceptance §2 (explicit input row) and the accepted 15-file submission | Applies to K, M, F, S, R, Wc, P; upstream of every E-2 identity | No new rules added; no signature or trust anchor; residual P0-Q1/P0-Q2 only | Direct + locally reverified |
| K — protected catalog | Exact keys, bounds, construction order, `Hk`; `case-policy-and-action-contract.md` §1 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7`; local bytes match; prohibition paragraph and key list spot-checked | F02 owner acceptance §2 (manifest member) | Depends on `Hm`, `Hf`, `Hs`; M → F → S → K direction; K contains no R/Wc/P reference | No review/owner/receipt/self-hash field; no catalog instance; approval authority deferred to D1; V/W/A unresolved | Qualified but sufficient (inherited accepted + section-mapped + spot-checked) |
| M — fixture manifest | Exact keys, B2/B3 variants, `Hm`; `case-policy-and-action-contract.md` §3 | Same source hash; local bytes match | F02 owner acceptance §2 (manifest member) | F, S, K reference `Hm`; coordinator/filter bindings to installed roles | B2 empty objects; B3 exactly four roles and 16 MiB bounds; no latent B2 objects | Qualified but sufficient |
| F — fixture policy | Exact top-level keys, fixed B2/B3 objects, `Hf`; `case-policy-and-action-contract.md` §3 | Same source hash; local bytes match | F02 owner acceptance §2 (manifest member) | References `Hm`; S and K reference `Hf`; host/UID/GID/boot bindings | Fixed constants; `PendingQualification` for network/inode retained; no permissive defaults | Qualified but sufficient |
| S — action script | Exact keys, three templates, `Ai`/`Hs`; `case-policy-and-action-contract.md` §4 | Same source hash; local bytes match | F02 owner acceptance §2 (manifest member) | References `Hm`, `Hf`; K references `Hs` | S is data, not code; finite ops with no arguments/jumps/loops; checks are pre-effect | Qualified but sufficient |
| R — detached review | Exact keys, `Hr`; `case-policy-and-action-contract.md` §1 | Same source hash; local bytes match | F02 owner acceptance §2 (manifest member) | Reviews completed K and transitive bytes; no owner acceptance hash | Detached from K; no edge back into K/F/S; reviewer authority deferred to D1 | Qualified but sufficient |
| Wc — detached owner acceptance | Exact keys, `Hw`; `case-policy-and-action-contract.md` §1 | Same source hash; local bytes match | F02 owner acceptance §2 (manifest member) | Accepts exact K + R bytes; no P hash; mechanism-neutral eligibility | Not live-invocation authorization; W authority mapping deferred to D1 | Qualified but sufficient |
| P — protected eligibility approval | Exact keys (binds `Hk`, `Hr`, `Hw`), no self-hash, `Hp`; `case-policy-and-action-contract.md` §1 + `mechanism-b-session-capability.md` | Both source hashes verified locally | F02 owner acceptance §2 (manifest members) | Written by A after independent verification; published last; live challenge remains mandatory | Eligibility only, not invocation; no producer-built P; root/store provisioning and A authority deferred to D1 | Qualified but sufficient |

Protected roots, custody, and the acyclic graph: the record reproduces the exact roots (`/opt/gagamba/validation-catalogs`, `validation-fixture-manifests`, `validation-policies`, `validation-actions`, `validation-fixtures`; `/etc/gagamba/validation-catalog-evidence`, `accepted-validation-catalogs`; `/run/gagamba/validation-live`), the root:root `0444`/`0755` custody rules, descriptor-relative no-follow acquisition with exact metadata checks, independent L/C/R/O acquisition timing, fixture-source rules, and the acyclic `M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability` graph with no edge returning into K/F/S and no hash fixed point. These are consistent with the accepted-by-hash texts and with the committed acceptance chain; no protected store, catalog, approval, or capability exists.

Conclusion: every essential E-2-scoped provenance link — exact bytes, accepted identity, originating acceptance, normative status, scope, dependencies, permitted use, restrictions, and residual gaps — is supported for the eight representations above and for the shared encoding/roots/custody/graph material. No unsupported or contradictory evidence was found. The bounded residual is the external-package durability qualification (§6), the historical caption qualification (§7), the resolved grammar reference (§8), and the not-yet-re-derived extraction fields disclosed in §3 item 8.

---

## 5. Exact adopted provenance-record identity

The owner hereby designates the following exact committed revision as the authoritative E-2-scoped repository provenance reference, to the extent and only to the extent of §4 and §9:

| Item | Value |
|---|---|
| Repository path | `docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md` |
| SHA-256 (exact bytes) | `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` |
| Size | 54,446 bytes |
| Git blob identity | `db25949d25babd474d00ae5b2f436e15507f22b3` |
| Recording revision | committed at `77101ac18c0e019a9d32712e9fee78b468cc8d02` (P0 commit), unchanged since |
| Companion report | `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md` = `513d395d9cb6f6591e62a20732a33c2f4c78018865f0407cf28c2b3b962d2f85` (blob `f08e485f011ca06104a8ac9e2a57cd76901f8dfb`) |
| Evidence manifest | `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/record-hashes.txt` (blob `19f8d47eaf67ed9d0eae2f06ab4db3f08d56f75d`) |

Acceptance of this record as an owner-adoptable artifact is confirmed against each required property:

- **E-2 restriction.** The record covers only CJSON-1 and K/M/F/S/R/Wc/P with their roots, custody, uses, supersession, and graph, and explicitly leaves GAVA1, v3-pre layouts, release correlation, the catalog-approval authority model, the F02 final-admission chain, and native qualification outside (§2.1–2.2).
- **Exact accepted identities.** The inventory identifies the F02 manifest/recheck, all 15 submission files, the support objects, and the three Alternative B inputs by SHA-256 and size, with the originating acceptance (§4.1–4.3).
- **Reproducible source-to-acceptance chain.** Each identity is traceable through `record-hashes.txt`, `digest-verification.txt`, `acceptance-mapping.md`, `schema-extraction-map.md`, and §5/§17 of the record, and is ancestral to the P0 baseline.
- **Normative versus historical status.** The record separates current normative semantics from historical `PENDING` captions and preserved reviews (§1.8, §3, §16).
- **K/M/F/S relationships.** `M -> F -> S -> K` construction, the detached R/Wc/P records, and the acyclic graph are preserved (§§7–11).
- **R/Wc/P boundaries.** R and Wc carry no owner/P-edge; P binds `Hk`/`Hr`/`Hw`, has no self-hash, and is eligibility only (§11).
- **No reinterpretation of approval authority.** The record states no producer-built P, no V/W/A mapping, and defers catalog-approval authority to D1 (§11.3–11.6, §16).
- **No runtime validation or invocation authority.** Permitted uses exclude producers, stores, approvals, capabilities, and activation; prohibited uses are enumerated (§§13–14).
- **Open D1 questions preserved.** V/W/A roles, catalog roots/custody, withdrawal/reapproval, and F02 separation remain D1-deferred (§16 P0-G4).
- **Residual limitations visible.** The external durability, caption, grammar, and review-limitation gaps are recorded (§16 P0-G1–G8).
- **Reproducibility.** §17 and the evidence package provide the exact commands and expected outputs.

This adoption is narrowly bounded. It does not retroactively create an earlier source approval, does not replace the original frozen artifact, does not adopt the validation-catalog approval chain, and does not authorize runtime behavior of any kind. The record's own “PROPOSED — PENDING P0 OWNER ACCEPTANCE” status line is historical bytes and is not rewritten; this decision supplies the missing acceptance and the current-state references record it.

---

## 6. External-package durability qualification (P0-Q1)

The P0 report retains a qualification on local external-package durability and the absence of an independently verified Git commit for the external package. Established facts:

1. The external artifact bytes are available locally under `C:\Users\Laszlos\source\repos\Solo\` (F02 decision package, F02 owner-support package, Alternative B decision and Q1 packages all present).
2. All checked local bytes match the committed acceptance identities: 15/15 manifest entries, manifest and recheck, three Alternative B inputs, and six support objects, with zero mismatches.
3. Existing authoritative acceptance records identify their origins: the F02 owner acceptance §2 accepts the exact 15-file submission and binds the manifest and recheck hashes; the Alternative B owner decision §4 binds the three validation-context inputs; `final-design-binding.md` (`5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d`) states that the design has **no independently verified Git commit** and that its exact revisions are its content manifests; the F02 acceptance Q07 confirms that input identities are recorded role attribution and prior-chat evidence, not cryptographic attestation.
4. The lack of an external Git commit does not prevent establishing the required E-2-scoped provenance: the accepted identity is the content manifest, which the owner expressly accepted, and the exact bytes are locally reverified against it. No stronger source-history claim is made or needed for the schema extraction's stated scope.
5. The repository-local P0 record provides durable traceability for its limited purpose: the exact hashes, the acceptance chain, and the full normative extraction are committed on canonical `main`, and a reviewer can reconstruct the E-2 contract from the repository without an unpublished external file and can re-verify the external bytes whenever local copies remain.
6. Original external-package preservation remains a separate future obligation. Nothing in this acceptance protects the `Solo/` copies from loss, and no independent source-history verification exists.
7. A future reproduction of the raw external bytes would require the local external state; if those copies were lost, byte-level re-verification of the external inputs would be impossible, although the committed extraction and its provenance identities would remain. This residual is exactly why the durability obligation is retained.

Artifact integrity is not independent source-history verification, and this decision does not claim upstream Git history for the external package. **Classification: NON-BLOCKING WITH RETAINED DURABILITY OBLIGATION.** The E-2-scoped source and identity are established by the existing accepted provenance; long-term external-package preservation or independent source-history verification remains outstanding and is carried as a binding residual.

---

## 7. Historical PENDING-caption qualification (P0-Q2)

The P0 report retains historical `PENDING` captions in the external frozen documents. Determinations:

- **Which artifacts contain them.** Within the E-2-scoped inputs, `PENDING` captions appear in 13 of the 15 F02 submission files: `actor-and-descriptor-binding.md`, `acyclic-authority-sequence.md`, `authority-mechanism-comparison.md`, `case-policy-and-action-contract.md`, `final-acceptance-hash-contract.md`, `mechanism-a-consume-before-launch.md`, `mechanism-b-session-capability.md`, `minimum-implementation-contract.md`, `negative-test-specifications.md`, `owner-decision-proposal.md`, `publication-withdrawal-race-matrix.md`, `remediation-notes.md`, and `trusted-actors-and-boundaries.md`. The two JSON files (`prior-input-provenance.json`, `source-provenance.json`) contain none. Among the three Alternative B inputs, `validation-bootstrap-compatibility.md` and `implementation-and-test-handoff.md` carry “PROPOSED - HUMAN OWNER APPROVAL PENDING”, and `validation-bootstrap-assessment.md` explains that the F02 documents are accepted through the later canonical decision despite their historical captions.
- **Frozen historical content.** The captions are part of the exact bytes bound by the committed acceptance hashes. Changing or deleting them would alter the accepted identities and destroy frozen evidence; they must not be rewritten.
- **Meaning at the time.** They stated that the design drafts awaited an owner decision and, for remediation items, a focused independent recheck. They were accurate pre-decision labels, not semantic claims about the eventual acceptance.
- **Current normative authority.** The [F02 Mechanism B owner acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) (commit `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1`, 2026-11-05) accepts the exact 15-file submission with Q01–Q10 and states that its acceptance does not rewrite the historical `PENDING` draft. The [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) (commit `d804d9062bab5d9d540df5c2d64f1b2c31bdd667`, 2026-11-06) accepts the validation context and binds the three inputs in §4.
- **Conflict.** There is no conflict between the captions and the later decisions: the later decisions govern the semantics they expressly accept, prospectively; the captions remain historical labels for their earlier state.
- **Effect on E-2-scoped provenance.** None. Provenance rests on exact bytes and the acceptance bindings, not on the captions; the record and report treat them as historical.
- **Genuinely pending items remain pending.** The design acceptances did not qualify native behavior, provision stores, publish records, or authorize implementation; `NATIVE-PENDING`, the 331 unqualified native skips, and the unstarted D1/E-2 implementation all remain as recorded and are not converted into accepted status.

**The historical label is distinguished from current normative authority: the captions are preserved evidence of a superseded draft state; the committed owner decisions are the current normative record within their expressed scopes. Classification: NON-BLOCKING; the qualification is retained and the captions are not to be modified.**

---

## 8. S2 ValidId grammar qualification (P0-Q3)

Determinations on the in-repository `ValidId` grammar reference:

1. **Exact normative source.** `docs/reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md` §1 item 2 (committed; SHA-256 `68a4830c16924fdfefa31e0df01c9f42ae336a106df1571048c676b7d1b2dc03`).
2. **Prior acceptance.** The grammar governs the receipt/record association and identifier validation of the accepted B6 S2/F02 trust-store behavior; the [F02 trust-store owner decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md) cites that qualification-record review and its deterministic check records five invalid IDs (traversal, absolute path, leading dot, trailing dot, overlength) refusing before lookup.
3. **Applicable artifact types.** K `caseId` and post-`qualificationId`; F `toolEnrollmentId` and `inputEnrollmentId`; qualification identifiers in the S2/F02 path.
4. **Accepted syntax and interpretation.** 1–128 characters from `[A-Za-z0-9._-]`; no empty value; no leading or trailing dot; reject `.`, `..`, path separators, whitespace, and non-ASCII; strict ASCII and case-sensitive (`char.IsAsciiLetterOrDigit`).
5. **P0 citation accuracy.** The record cites the grammar accurately in its K and F key tables and in P0-G3, with no paraphrase that changes the character set or the rejection rules.
6. **No competing definition.** P0 introduces no alternative grammar; the report, record, and evidence package use “S2 ValidId” consistently.
7. **Necessity for E-2 identity.** The grammar is required to fully reconstruct K and F identity fields; without it those fields would be under-specified.
8. **Ambiguity affecting Q-E1.** None. The reference is in-repository, committed, and unambiguous; the earlier external dependency concern is resolved.

**Classification: RESOLVED REFERENCE — NON-BLOCKING.** The existing accepted in-repository reference supports the P0 provenance claim. This decision does not invent, expand, or relax the identifier syntax; the presence of the parser in accepted S2 code is corroborating, not the authority.

---

## 9. K/M/F/S/R/Wc/P preservation

The following accepted properties are preserved and are the exact scope of the adoption in §5:

- The binding direction is `M -> F -> S -> K`; F references `Hm`, S references `Hm`/`Hf`, K references `Hm`/`Hf`/`Hs`; there is no hash fixed point.
- K is not P: K contains no review, owner-acceptance, approval, or self-hash field and no reference to R, Wc, or P.
- P concerns eligibility, not invocation: P binds `Hk`/`Hr`/`Hw`, is published last, contains no self-hash, and cannot issue or reissue a capability; a fresh live challenge and one-response latch remain mandatory.
- R and Wc remain detached: they do not become fields inside K, and no edge returns from R/Wc/P into K/F/S.
- S is not code: it is a finite, canonically encoded action manifest with no arguments, expressions, substitutions, labels, jumps, loops, retries, or callbacks; no actor evaluates script text.
- No producer-built P: A alone may write P after independently verifying actual V/W provenance; a producer, timestamp, candidate file, or self-consistent hash set is insufficient.
- No cross-host reuse or standing approval: content-addressed identity, exact host binding, and bounded scope are retained; approvals are not portable and do not survive as unattended authority.
- F02 authority remains store-specific: `/etc/gagamba/accepted-qualifications/<qualificationId>.json` is not extended by analogy to validation catalogs or the final-admission chain.
- V/W/A mapping remains unresolved pending D1: the external labels are not silently mapped onto Slice E actors.
- Protected roots and custody are preserved as recorded: exact hash-derived leaf names, root:root `0444`/`0755`, descriptor-relative no-follow acquisition, independent L/C/R/O acquisition, and immutable active-session metadata.

---

## 10. Provenance versus operational authority separation

What this acceptance establishes is **provenance only**: for the E-2 subset, the frozen artifact bytes, their accepted identities, the authoritative sources, the accepted normative record, and the documented permitted uses are reconciled in an auditable repository chain.

It grants **no operational authority**. Specifically, it does not create: a schema instance, a catalog, an approval record, a trust anchor or signature, a protected store or its provisioning, a capability, a validation session, a runtime grant, a positive-validation path, an E-1 production consumer, or any protected execution effect. This decision is documentation-only and cannot convert hash agreement, design consensus, or acyclicity into authorization. Any operational use remains gated by the separate decisions identified in §§12–13 and §17.

## 11. Formal P0 owner disposition

**P0 ACCEPTED WITH QUALIFICATIONS — Q-E1 SATISFIED FOR E-2 SUBSET ONLY.**

The required E-2-scoped provenance relationships are supported by the committed evidence and the locally reverified frozen inputs, with the bounded residual evidence qualifications below retained. No material evidence conflict was found. The owner adopts the record identified in §5 as the authoritative E-2-scoped repository provenance reference.

Binding qualifications retained by this disposition:

1. **P0-Q1 — external durability.** Non-blocking with the retained durability obligation (§6): the external package remains local-only with no independently verified Git commit, and its long-term preservation is an outstanding obligation.
2. **P0-Q2 — historical captions.** Non-blocking (§7): the external `PENDING` captions are preserved historical bytes superseded within their expressed scopes by the later committed decisions; they must not be rewritten, and genuinely pending items remain pending.
3. **P0-Q3 — ValidId grammar.** Resolved in-repository reference, non-blocking (§8).
4. **Extraction completeness.** The E-2 contract is source-bound, section-mapped, spot-checked, and cross-agreeing between the report and the record; a full field-by-field re-derivation from the external bytes was not performed in this bounded owner-acceptance task and remains available to later reviewers.
5. **Single-reviewer recording.** This is a governance recording, not independent multi-model qualification or external attestation.
6. **No disposition in this decision closes Q-E2, starts D1, or authorizes E-2 entry or implementation.**

This decision records the acceptance revision as the single documentation commit that adds this file and updates the three current-state documents; its exact commit identity is reported in the P0 acceptance handoff (it cannot be self-referenced before commit). No other formal disposition is issued.

---

## 12. Exact Q-E1 scope and status

**Q-E1 SATISFIED FOR E-2 SUBSET WITH QUALIFICATIONS.** Composite Q-E1 remains partially open: the E-2-scoped prerequisite is satisfied by §5 and §11, while the GAVA1 header and exact wire layout, the v3 validation-pre layouts, status values, echo fields and bootstrap bounds, and the release-correlation formula remain **open within Q-E1** for E-3/E-4 reconciliation. This decision does not close, weaken, or silently advance any E-3/E-4 provenance prerequisite, and it does not make those items dependencies of E-2 on the basis of this acceptance.

Scope of the satisfaction:

- **Artifacts:** CJSON-1 and K/M/F/S/R/Wc/P representations, bounds, cross-field bindings, protected roots, custody, permitted/prohibited uses, supersession rules, and the acyclic graph.
- **Accepted provenance record:** `docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md` at SHA-256 `11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6` (Git blob `db25949d25babd474d00ae5b2f436e15507f22b3`).
- **Retained qualifications:** §6, §7, §8, and §11 items 4–5.
- **Downstream exclusions:** §16.

---

## 13. Q-E2/D1 status and restrictions

**Q-E2 remains OPEN.** This acceptance does not adopt the validation-catalog approval chain and does not decide who may review V, accept W, or provision P as A. It does not map V/W/A onto Slice E roles, extend F02 qualification-store authority, create a root authorizer or trust anchor, approve a validation catalog, grant validation or invocation capability, authorize an E-1 production consumer, or construct any positive validation authority.

**D1 has not started.** D1 requires its own separate commission and its own owner decision. P0 acceptance does not predetermine D1's conclusion; D1 must independently decide whether the existing approval chain can be adopted under the accepted authority model. If separately commissioned and completed, D1 still does not automatically authorize E-2 implementation.

---

## 14. E-1 qualifications

- **E-1: ACCEPTED WITH QUALIFICATIONS.** The refusal-only prepublication integrity projection remains accepted at implementation `2259fce59b44b6af6448a38685a0635d560d0900` / qualification `db832452474c378d2c1ed8f515325c23f9ee7aab`, production-unreachable (no production caller of `Verify()`), with no validation-authority factory and no positive validation. This decision adds no E-1 consumer.
- **F-01: CLOSED WITH QUALIFICATIONS.** Preserved: owner role-hash binding (`OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1)`), recovery role-hash binding (`RoleHash(manifest, 0)`), runtime-inventory binding (`platform.runtimeInventorySha256`), and the binding future-consumer obligation that every integrity-verification exception or failure mode must fail closed before any production consumer is authorized.
- **F-02: CLOSED WITH QUALIFICATIONS.** Preserved: Q1-F01 through Q1-F06 as binding qualifications, and the mandatory future production-integration gate (both verification paths, shared invariants, branch dominance, call-preserving bypasses, helper semantics and arguments, F-01 integrity relationships, fail-closed behavior, authority separation, loaded-assembly verification-to-use, artifact identity/publication, and P4/native obligations). Option A shared-core consolidation remains a mandatory reassessment at that gate, not an approved implementation.
- **F-03: CLOSED WITH QUALIFICATIONS.** Preserved: Windows PowerShell 5.1 qualification (51/51 probes, exit 0), PowerShell 7 unexecuted and unclaimed, accepted nonzero-exit semantics, and guaranteed scoped `$ErrorActionPreference` restoration. Verification tooling must not be modified by this decision or in its name.

All three closures and the E-1 acceptance are preserved without reopening, widening, or promotion.

---

## 15. P4 and RELEASE preservation

This owner decision is documentation-only and is classified **P4-NEUTRAL**. The commit contains exactly this decision and the three current-state documentation updates; it contains no executable production or test change, no protected-sink caller change, no authority-gate change, and no RELEASE change. Verified at commit time with the staged-path and diff checks.

Preserved in full: protected-sink caller restrictions; F01/F02 enforcement; S10/S11 domination; S28 isolation; the first-protected-effect boundary; qualified-to-legacy downgrade refusal; F-R1/O-R1; O-R2; O-R3; the accepted observation-to-effect residual; `NATIVE-PENDING`; all 331 unqualified native skips (not passes); and RELEASE test-hook isolation. No positive native qualification is claimed, no protected execution is activated, and no E-1 production consumer is introduced.

---

## 16. Downstream E-3/E-4 exclusions

This acceptance explicitly excludes, and must not be read to advance: GAVA1 headers and authority construction; v3-pre layouts; release correlation as an E-3/E-4 implementation dependency; the E-3 validation-pre codec; E-4 GAVA1; the E-5 fixed launcher and custody interface; E-6 atomic enablement and the R/O/C ordering correction; E-7 driver integration; and Slice N protected native qualification. Shared terminology or dependency on related artifacts does not extend the accepted provenance scope. E-2 entry itself remains unauthorized until the full required sequence in §17 completes.

---

## 17. Recommended next commission

With Q-E1 satisfied for the E-2 subset, the recommended next (not automatically started) commission is:

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-D1`** — the explicit owner decision on validation-catalog approval-chain adoption.

Required subjects for D1 (per the E-2 entry owner decision §8 and the preserved qualifications): the K/M/F/S authority relationships; catalog-to-schema identity binding; the V/W/A roles and their accepted definitions; trust-anchor provenance; approval-chain legitimacy; delegation and scope constraints; F02 authority separation; fail-closed rejection rules; the prohibition on implicit promotion from provenance into operational authority; and the downstream E-3/E-4 restrictions. D1 must not assume that successful P0 provenance adoption proves catalog approval authority, and D1 must not begin automatically from this decision.

Required sequence thereafter remains: **D1 owner decision → fresh E-2 entry assessment → fresh E-2 entry owner decision → separately authorized implementation.** A D1 approval decision would not automatically grant implementation authorization.

No further action is taken here. This decision stops after its own commit; D1, E-2 implementation, all later Slice E work, activation, synchronization, and push remain unauthorized.
