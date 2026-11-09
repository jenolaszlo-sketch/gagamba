# ALT-B Slice E-2 frozen-schema provenance record — E-2 subset (P0)

**Status: PROPOSED — PENDING P0 OWNER ACCEPTANCE. This record does not adopt a schema, approve a catalog, create a trust anchor, or authorize runtime behavior.**

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P0`

Date: 2026-11-09 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only provenance reconciliation on canonical `main`; no push.

Baseline: HEAD `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`, tree `934bf6567e1ee10bff59d3669bf0ca2a95c6d6d3`, branch `main`, exactly one canonical worktree.

This record is the proposed repository provenance record for the **E-2 subset** of Q-E1: canonical encoding CJSON-1 and the K/M/F/S/R/Wc/P representations, their exact field sets, bounds, cross-field bindings, protected roots, custody rules, permitted and prohibited uses, and the acyclic approval graph. It reconstructs those representations from the exact external bytes accepted by committed owner decisions, without converting those bytes into new repository authority.

This is a single-reviewer documentation reconciliation. It is **not** independent multi-model qualification, external attestation, a human signature, an owner acceptance, a catalog approval, a trust anchor, a runtime grant, or a native qualification.

---

## 1. Status and non-authority declaration

1. This record is **proposed**. It becomes an accepted P0 provenance record only if a separate **P0 owner acceptance** decision accepts its exact bytes and precise scope.
2. **Q-E1 remains formally OPEN.** P0 reconciliation is not P0 owner acceptance. A P0 owner acceptance may close Q-E1 only for the precise E-2 subset it expressly accepts; the GAVA1 header, v3 validation-pre layouts, and release correlation remain open for E-3/E-4 reconciliation.
3. **Q-E2 remains formally OPEN.** This record does not adopt the validation-catalog approval chain, the V/W/A roles, the `/etc/gagamba/accepted-validation-catalogs` root, the `/etc/gagamba/validation-catalog-evidence` root, the R/Wc/P authority model, or any catalog withdrawal/reapproval semantics.
4. **D1 has not started.** The owner decision that authorized P0 and D1 does not authorize D1 automatically; D1 requires its own commission and its own owner decision.
5. **E-2 entry and E-2 implementation remain unauthorized.** P0 completion does not authorize a fresh entry assessment, E-2 implementation, E-3 through E-7, Slice N, positive validation, or any protected execution path.
6. This record creates **no** schema version, schema field, canonical serialization rule, signature, trust anchor, approval record, protected store, catalog instance, capability, runtime authority, or production code.
7. A matching SHA-256 value proves byte identity to the object whose hash was accepted. It does **not** prove human provenance, owner approval, catalog approval, trust-anchor adoption, or authorization.
8. The historical `PENDING` captions in the external documents are preserved unchanged. The later committed owner decisions govern the semantics they expressly accept; this record does not rewrite historical bytes.
9. F02 authority over `/etc/gagamba/accepted-qualifications/<qualificationId>.json` remains **store-specific** and is not extended by this record or by analogy to validation catalogs.
10. No E-1 integrity evidence is promoted into validation authority by this record.

---

## 2. Scope: E-2 subset and Q-E1 remainder

### 2.1 Covered by this record

- CJSON-1 canonical encoding, exact-byte reserialization, strict field sets, integer/string/array rules, size limit, and hash conventions.
- **K**: protected catalog schema, exact keys, bounds, construction order, and hash `Hk`.
- **M**: protected fixture manifest schema, B2/B3 variants, and hash `Hm`.
- **F**: protected fixture policy schema, B2/B3 variants, constants, and hash `Hf`.
- **S**: protected finite action script schema, three exact templates, per-action hash `Ai`, and script hash `Hs`.
- **R**: detached independent catalog review schema and hash `Hr`.
- **Wc**: detached owner catalog acceptance schema and hash `Hw`.
- **P**: protected catalog-eligibility approval schema and hash `Hp`.
- The acyclic dependency graph `M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability`.
- Protected roots, custody/acquisition rules, permitted/prohibited uses, replacement/supersession restrictions, and unresolved questions for the E-2 subset.

### 2.2 Explicitly outside this record

- The GAVA1 header and its exact wire layout.
- The v3 validation-pre layouts, status values, echo fields, and bootstrap bounds.
- The release-correlation formula as an E-3/E-4 implementation dependency, except where an exact cross-reference is needed to preserve the K/S binding.
- The F02 final-acceptance record, selector, and final-admission stores, which belong to the separate final-admission chain.
- The validation-catalog approval **authority** model: who may review V, who may accept W, who may provision P as A, and how that authority maps to Slice E roles.
- Any catalog instance, approval record, protected store provisioning, native behavior, or positive validation.
- Any E-2 implementation package or public API.

### 2.3 P0 completion criteria

This record supports a P0 owner-acceptance decision only if the owner can conclude that:

1. an independently traceable provenance record exists in the repository;
2. a reviewer can reconstruct the E-2 K/M/F/S/R/Wc/P contract from the repository without relying on an unpublished external file;
3. every hash matches the committed acceptance;
4. the record states what remains external and why;
5. unresolved provenance gaps are recorded explicitly;
6. no schema behavior is implemented; and
7. a separate P0 owner-acceptance decision accepts the record within its precise scope.

### 2.4 P0 stop conditions

P0 did not trigger a stop condition. Each condition was checked against the evidence:

| Stop condition | P0 result |
|---|---|
| A frozen artifact's provenance cannot be established | Not triggered. The exact bytes are locally present and hash-match the committed acceptance chain. |
| An expected digest does not match | Not triggered. All checked hashes match; `diff_count=0`. |
| Referenced acceptance evidence is missing | Not triggered. The F02 acceptance, recheck, manifest, Alternative B decision, and E-1/F-01/F-02/F-03/P4 records are committed and ancestral to the P0 baseline. |
| The record would depend on an unknown authority | Not for the E-2 provenance subset. The catalog approval authority is expressly deferred to D1 and is not asserted. |
| The catalog would be approved against the wrong schema | Not triggered. No catalog is approved; the schema bindings are reconstructed and preserved. |
| Existing F02 authority is reused without an explicit accepted contract | Not triggered. F02 remains store-specific and is not extended. |
| A proposed trust anchor lacks provenance | Not triggered. No trust anchor is proposed. |
| K/M/F/S relationships conflict with accepted specifications | Not found. The extracted relationships match the accepted-by-hash contract. |
| The activity begins introducing runtime authority | Not triggered. This is documentation-only. |
| E-1 evidence is promoted into positive validation authority | Not triggered. E-1 remains integrity-only and production-unreachable. |
| E-2 implementation is attempted before fresh entry approval | Not triggered. No implementation is attempted. |
| Protected execution behavior would change without P4 authorization | Not triggered. No executable change exists. |

---

## 3. Source hierarchy and precedence

The following precedence is used throughout this record:

1. A later owner decision governs the semantics it expressly accepts, including any historical `PENDING` caption it supersedes.
2. A closure decision preserves every qualification it does not expressly remove. Later work may not silently widen an earlier closure.
3. An external input accepted by hash proves byte identity to the accepted object. It does not become in-repository authority merely because it is locally present.
4. An earlier review, recommendation, producer report, or hash list is not an authorization.
5. Only an owner decision can adopt an approval chain, create a trust anchor, or authorize runtime authority. P0 is not such a decision.
6. No field, version, signature, trust anchor, approval, or authority may be invented to fill a gap.

### 3.1 Committed records used

| Record | SHA-256 | Role |
|---|---|---|
| [E-2 entry owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md) | `ee0b2b81682e266488f59e0abcb870664ba25eaa0048764f885b44ae72793c79` | Authorizes P0/D1 with binding limits; leaves Q-E1/Q-E2 open |
| [E-2 entry assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md) | `f4212a8a1302695dae53a763682c0dc79de3a47af02e29b14abffbec96e1b664` | Accepted `RECONCILIATION REQUIRED` assessment |
| [E-2 entry evidence index](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/README.md) | `3090058f7dc0e872e4c721b5f21cd9f6ab955b87c5dab4dbc0bb978c6e17273c` | Prior evidence inventory and role map |
| [Slice E entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md) | `497dcb6924d692d4f3601c33baa3320de7036e057293d4bc4836c29e52fe99de` | Slice E architecture and E-2 package boundary |
| [E-1 entry owner decision](../decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) | `573f248a41b6d0215d294b74152ab17c64478fc6ac484884dff0f001b46b84fb` | Q-E1 through Q-E5 and E-2 sequencing |
| [E-1 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) | `27de68690cca7027d46b0692e52e5da71641e38e13c4037fd31937d08f823b93` | E-1 accepted with qualifications |
| [F-01 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md) | `30205d32409c097624da25cfd9ed6608f1588d79f3af96231125da06e6ad194b` | F-01 closed with qualifications |
| [F-02 parity owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | `00f39ae2a1fc1675fa195c3af0ba040fdd05eed956e497f5e0c6fa5285a56107` | F-02 closed with qualifications |
| [F-03 owner acceptance](../decisions/rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md) | `44c85e0062c42ddec6e98a74268d71f358a9435f506089aa416e6045d47fb944` | F-03 closed with qualifications |
| [P4 coverage owner acceptance](../decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | `3bdccfdf3e24d4a00fd9f2e600cee13e3eab94014def03a349023eb2bf3e4b6e` | Refusal-only P4 coverage preserved |
| [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361` | Accepted validation context and ordering |
| [Alternative B implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | `9b3d9774e6a1c244ccfa89e8f16a54c8678d22bdef0e0272ce1c528b8dd4f42c` | Atomic Slice E dependency set |
| [F02 Mechanism B owner acceptance](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e` | Accepts the exact 15-file frozen submission with Q01–Q10 |
| [F02 trust-store owner decision](../decisions/rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | `e318125f91e20350824aeb70042b35a37c33d54fcde8350a4d353dccfaeb0adb` | Store-specific qualification authority only |
| [Installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md) | `f727ea026f9ef4eafe2bfb21af87ad9bee493149645e9e6576a3f85c694ca5f6` | Integrity versus authorization; gates A–D |
| [Remediation evidence standard](../remediation-evidence-standard.md) | `0478e4878b7c95d6441dcc24f6fde17f2f86baf74aab41a3298df892913b9854` | Evidence-standard model; not an implementation claim |

### 3.2 External source hierarchy

- **Normative schema text:** `case-policy-and-action-contract.md` and `final-acceptance-hash-contract.md`, both in the F02 15-file frozen submission.
- **Capability and live-binding semantics:** `mechanism-b-session-capability.md`, in the same submission.
- **Actor and descriptor semantics:** `actor-and-descriptor-binding.md` and `trusted-actors-and-boundaries.md`, in the same submission.
- **Validation contract context:** `validation-bootstrap-compatibility.md`, `validation-bootstrap-assessment.md`, and `implementation-and-test-handoff.md`, bound by the Alternative B owner decision.
- **Acceptance identity:** the F02 owner acceptance at commit `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` and the Alternative B owner decision at commit `d804d9062bab5d9d540df5c2d64f1b2c31bdd667`.
- **Original external source location:** `C:/Users/Laszlos/source/repos/Solo/gagamba-f02-authority-mechanism-decision-01`, with owner-package support at `C:/Users/Laszlos/source/repos/Solo/gagamba-f02-authority-mechanism-owner-01`, and validation-context support under `C:/Users/Laszlos/source/repos/Solo/gagamba-integrated-start-authority-owner-decision-01` and `...-q1`.
- **Durability limitation:** the external design has no independently verified Git commit. Its exact revisions are its content manifests. It remains local external material, not a published repository artifact.

---

## 4. Frozen artifact inventory and exact identities

All values below were recomputed with `Get-FileHash -Algorithm SHA256` at the P0 baseline. Sizes are bytes. The F02 manifest entry and the local file agree for all 15 entries.

### 4.1 F02 15-file frozen submission

| File | Size | SHA-256 | E-2 role |
|---|---:|---|---|
| `actor-and-descriptor-binding.md` | 18,757 | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | Actor/descriptor authority and catalog chain references |
| `acyclic-authority-sequence.md` | 8,004 | `ed8dca1edb26b0ccde009b60f3f16148a67fed323fd47e9989b605397364d200` | Acyclic authority context |
| `authority-mechanism-comparison.md` | 5,760 | `14dc6bd42432dc6c434c5ac10473dd1e568114ba85e11c6e95356b4534526179` | Mechanism comparison; not adopted by P0 |
| `case-policy-and-action-contract.md` | 30,362 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | **Normative K/M/F/S/R/Wc/P schemas, roots, graph** |
| `final-acceptance-hash-contract.md` | 10,989 | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` | **Normative CJSON-1 and identity types** |
| `mechanism-a-consume-before-launch.md` | 9,876 | `8cbcdd5493fe4b86b680912d1fdf7ece5fb9bd662117798402d4c90f667d73d6` | Alternative mechanism; not selected |
| `mechanism-b-session-capability.md` | 14,325 | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | **Capability fields and P/eligibility semantics** |
| `minimum-implementation-contract.md` | 14,152 | `c4e73095cd1f05ee7410cc97bd26d8a8c94c09a0e9e14a47eb1c1840426234b5` | Implementation boundary context |
| `negative-test-specifications.md` | 21,157 | `0900b7fe4049c941695b9dd9c421105b25f3b879bcdf2cad30239ea3aea76791` | Test specifications; not executed by P0 |
| `owner-decision-proposal.md` | 8,004 | `f3b175f755e4319a428a452103f2d20e30f2f37c764685cc971b1c5f30545bc3` | Historical proposal; not authority |
| `prior-input-provenance.json` | 2,955 | `f22a9b3025a24b529b97adc47465c16b6b3c27b9852dd45497322d17f10c42e7` | Historical input provenance |
| `publication-withdrawal-race-matrix.md` | 14,140 | `1f5d9d651ccd2e88d5b406e0d1fe3a1e26d64c666ee8dded27099c4da98bc963` | Publication/withdrawal context; not adopted by P0 |
| `remediation-notes.md` | 8,720 | `8618e4980ab641ae815a3f05ffc781208607ea70470784901641703ab6788ff4` | Remediation context |
| `source-provenance.json` | 83,824 | `6b9312dc3baab780fad51a3bb4baaf22bfaec52156ef2b8d1b47356d6414b99c` | Source provenance record; not a Git attestation |
| `trusted-actors-and-boundaries.md` | 9,402 | `dfc2188b811de50d96f8e1db117c10c15c9efc1603f486607bb2fd38538b201c` | Root/actor boundary context |

### 4.2 Acceptance and support objects

| Object | Size | SHA-256 | Binding |
|---|---:|---|---|
| `recheck-input-SHA256SUMS.txt` | 1,463 | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | Bound by F02 owner acceptance §2 |
| `independent-recheck.md` | 4,470 | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | Bound by F02 owner acceptance §2 |
| F02 outer `SHA256SUMS.txt` | 5,321 | `a9c5dc71eb0c7b7ff9e443ea8107d55f99540cc119ee343fc18ca105b5ce4e6b` | Context only; not the acceptance-bound manifest |
| `final-design-binding.md` | 8,598 | `5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d` | Lists the exact 15 files; states no independently verified Git commit |
| `accepted-contract-and-qualification-matrix.md` | 12,042 | `d8ba4a64c48a6fbac2c9ca4245fcfb3147cca63e0c0ee6a3bcadf080d581d808` | F02 owner-package support |
| `negative-test-reconciliation.md` | 19,502 | `9deeb26b63918cdc92b782e498ab3a6e870a0ae127f61ad474f3533f72b037ad` | F02 owner-package support |
| `implementation-authorization-boundary.md` | 8,759 | `9eb472d640335a1af259416b4afd365399977361f50e28a10b0065af6937376b` | F02 owner-package support |
| Historical initial `independent-review.md` | 7,914 | `f09bc803728514cb46f2a13de7209839355cde3cc876d353f5171c747f8d38bb` | Historical incomplete review; not replaced |

### 4.3 Alternative B validation-context inputs

| File | Size | SHA-256 | Binding |
|---|---:|---|---|
| `validation-bootstrap-compatibility.md` | 15,291 | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | Alternative B owner decision §4 |
| `validation-bootstrap-assessment.md` | 3,501 | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | Alternative B owner decision §4 |
| `implementation-and-test-handoff.md` | 18,650 | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | Alternative B owner decision §4 |

### 4.4 Acceptance-chain commits

All of the following commits are ancestors of the P0 baseline HEAD `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`:

| Commit | Date | Subject |
|---|---|---|
| `504b6d06ac88591072b11f428ae88cade390c075` | 2026-11-04 | Freeze B6 native platform inputs and preserve plan Q1 |
| `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` | 2026-11-05 | Accept B6-F02 Mechanism B and authorize bounded implementation |
| `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` | 2026-11-06 | Record Alternative B integrated Start authority decision |
| `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6` | 2026-11-03 | Accept the bounded F02 publication policy and B6-S2 with qualifications |
| `c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e` | 2026-11-07 | Accept Slice E entry and authorize bounded E-1 |
| `8bd71425eb31b69290affa2b86c78afce1bd0882` | 2026-11-07 | Accept Slice E-1 with qualifications |
| `1ed1fc706ea76954e4f1d7d43673048ef82072ec` | 2026-11-07 | Close Slice E-1 F-01 with qualifications |
| `6d08ae569c03e96872fcc5d330435991952207d6` | 2026-11-08 | Close Slice E-1 F-03 with qualifications |
| `e86254e60fb3ded770156519e1d3e15e73bf36fc` | 2026-11-09 | Close Slice E-1 F-02 with qualifications |
| `2d39380d0c0b61753097c197ce7c1df03cdddcb7` | 2026-11-07 | Accept refusal-only P4 coverage with qualifications |
| `a35ce108d3f65c1a560a4b01ef91fa3d9ade900d` | 2026-11-09 | Assess Slice E-2 entry as reconciliation required |
| `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe` | 2026-11-09 | Authorize Slice E-2 entry reconciliation with qualifications |

No mismatch, missing file, or extra unbound schema-bearing input was found in the E-2 subset.

---

## 5. Acceptance chain and provenance reconstruction

### 5.1 F02 frozen design chain

1. The external design package is at `C:/Users/Laszlos/source/repos/Solo/gagamba-f02-authority-mechanism-decision-01`.
2. `final-design-binding.md` identifies the exact 15-file submission and states that the external design has **no independently verified Git commit**; its exact revisions are its content manifests.
3. The submission's frozen recheck manifest is `recheck-input-SHA256SUMS.txt`, SHA-256 `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`.
4. Every one of the 15 local files matches its manifest entry. The independent recheck `independent-recheck.md` has SHA-256 `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` and binds to that manifest. Its disposition is `SUPPORTED WITH QUALIFICATIONS`.
5. The F02 owner acceptance at commit `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` (document SHA-256 `5f72da6f7ef7ede6379f587521226038137e7de602e53b2b94669f366cd75b3e`) accepts the exact 15-file submission with Q01–Q10 and expressly binds the manifest and recheck hashes.
6. The acceptance is design-level. It does not activate production, create an approval record, publish a final record, provision a protected store, implement full session capability, implement the installed-profile driver, or authorize native qualification.

### 5.2 Alternative B validation-context chain

1. The Alternative B owner decision at commit `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` (document SHA-256 `3465b3503ad75daa40fead701fad36d165d642568bc60c98f67c3870462b6361`) accepts owner-side manifest authorization and the validation-only ordering context.
2. Section 4 of that decision binds the three validation-context inputs by SHA-256: `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb`, `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced`, and `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054`.
3. All three local files match those values. Their historical `PENDING` captions remain unchanged; the later canonical decision governs the semantics it expressly accepts.
4. This chain supplies validation context. It does not adopt the catalog approval authority or the V/W/A mapping.

### 5.3 E-1 prerequisite chain

The P0 baseline preserves the accepted closures and qualifications:

- E-1 accepted at implementation `2259fce59b44b6af6448a38685a0635d560d0900`, qualification `db832452474c378d2c1ed8f515325c23f9ee7aab`.
- F-01 closed at reviewed implementation `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f`.
- F-02 closed at implementation `4c5a42f0fecff90dcb30088ca0203956e7a80032`, qualification `d221b24be3d19c775c5118e419b2e88769fea2ce`.
- F-03 closed at script `52037fa5738609b1469089835380a0cc94e047f1`.
- P4 refusal-only coverage accepted with 21 COVERED, 5 AUTHORIZED CONTINUATION, and 3 LEGACY / TEST-ONLY rows; zero SLICE E DEPENDENCY, zero GAP, zero INCONCLUSIVE.

These are preserved. P0 does not reopen, widen, or promote them.

### 5.4 What the chain proves

Proved:

- The local external bytes are byte-identical to the objects whose hashes the committed acceptances bind.
- The F02 15-file manifest and Alternative B key-input list are internally consistent with the local files.
- The schema-bearing text used for the E-2 extraction is the same text accepted through those records.
- The committed acceptance records are ancestral to the P0 baseline.

Not proved:

- That an in-repository file adopts those bytes as product behavior.
- That the external package is a published or durable artifact independent of the local `Solo/` tree.
- That a reviewer can reconstruct every field from the repository without this P0 extraction.
- That any catalog, approval, root, capability, or protected store exists or is qualified.
- That hash agreement is owner approval or runtime authority.

---

## 6. CJSON-1 canonical encoding

Normative extraction from `final-acceptance-hash-contract.md` §“Canonical encoding CJSON-1”. No rule is added or relaxed.

1. **Encoding:** UTF-8 containing ASCII only; no BOM, whitespace, or trailing newline.
2. **Document shape:** exactly one JSON object. No duplicate, unknown, or missing keys.
3. **Key order:** keys sorted in ascending ASCII order, recursively.
4. **Strings:** permit U+0020 through U+007E. Encode `"` as `\"` and backslash as `\\`; every other permitted character is literal, including `/`. No `\u` or alternative escapes.
5. **Integers:** unsigned decimal; no leading zero except `0`; no exponent, fraction, or sign; range 0..2^63-1 unless a narrower bound applies.
6. **Booleans:** literal `true` or `false`. `null` only where explicitly allowed.
7. **Arrays:** retain the defined order.
8. **Round trip:** reject any representation unless parsing followed by canonical serialization exactly equals the input bytes.
9. **Empty strings:** allowed only where explicitly permitted; none in the final metadata.
10. **Size limit:** each metadata file is at most 65,536 bytes. A final evidence index may be a separately hashed file, not arbitrary inline data.
11. **Hashing:** SHA-256 over the exact canonical bytes.
12. **Hex conventions:** all SHA-256 values are 64 lowercase hex; Git source commit/tree and review commit are full 40 lowercase hex; nonzero UUIDs are 32 lowercase hex.
13. **Self-approval:** a hash is never a self-approval field.
14. **Existing record encoding:** the existing S2/F02 record encoding remains unchanged; its hash is over the original raw record bytes, not reserialization. The final schema refers to that exact raw hash.

For the E-2 subset, K, M, F, S, R, Wc, and P are all canonical metadata objects subject to these rules.

---

## 7. K — protected catalog schema

Normative extraction from `case-policy-and-action-contract.md` §1.

### 7.1 Exact keys

| Key | Exact type / requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `caseId` | S2 ValidId |
| `phase` | `pre` or `post` |
| `topology` | `b2-lifeline` or `b3-integrated` |
| `artifactSetSha256` | SHA-256; exact artifact-set identity |
| `sourceCommit` | full 40 lowercase hex |
| `sourceTree` | full 40 lowercase hex |
| `profileId` | nonempty ASCII, at most 128; exact accepted profile |
| `profileVersion` | nonempty ASCII, at most 128; exact accepted version |
| `rid` | nonempty ASCII, at most 128; exact accepted RID |
| `scope` | nonempty ASCII, at most 128; exact accepted scope |
| `hostId` | 64 lowercase hex; protected local host identity |
| `platformInventorySha256` | exact manifest platform inventory hash |
| `qualificationId` | `null` for pre; exact selected S2 ValidId for post |
| `qualificationRecordSha256` | `null` for pre; exact raw receipt-selected record hash for post |
| `controllerClosureSha256` | SHA-256 |
| `driverClosureSha256` | SHA-256 |
| `fixtureManifestSha256` | SHA-256 of M (`Hm`) |
| `fixturePolicySha256` | SHA-256 of F (`Hf`) |
| `actionScriptSha256` | SHA-256 of S (`Hs`) |
| `maximumSessionNs` | integer 1..600000000000 |
| `allowRelease` | Boolean |

### 7.2 Prohibitions and bindings

- K contains **no** review, owner-decision, acceptance-receipt, or self-hash field.
- K contains **no** `catalogReviewSha256`, `ownerCatalogAcceptanceSha256`, `catalogApprovalSha256`, `Hr`, `Hw`, or `Hp`.
- K contains no argv, environment, or resource fields; those meanings are in F.
- Duplicated phase, topology, case, host, qualification, release, and session-limit values in dependent data must equal K and must never override it.
- `Hk = SHA256(CJSON1(K))`.

### 7.3 Construction order

1. Construct M and F. F references `Hm`. Construct S referencing `Hm`, `Hf`, and the fixed case/phase/topology. None references a catalog or approval.
2. Construct K with `Hm`, `Hf`, `Hs`, and exact candidate/host/selection and code-closure identities. Compute `Hk`.
3. K contains no reference to R, Wc, or P. There is no hash fixed point.

---

## 8. M — protected fixture manifest schema

Normative extraction from `case-policy-and-action-contract.md` §3.

### 8.1 Exact keys and B2/B3 variants

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `caseId` | equal to K/F case |
| `topology` | equal to K/F topology |
| `objects` | B2: exactly empty array. B3: exactly four entries sorted by ASCII `role`: `coordinator`, `filter`, `input`, `tool` |

Each B3 object has exactly:

| Key | Exact requirement |
|---|---|
| `role` | one of `coordinator`, `filter`, `input`, `tool` |
| `path` | same literal single component as `role` |
| `length` | integer 1..16777216 |
| `sha256` | SHA-256 of the exact object |
| `mode` | `0555` for coordinator/tool; `0444` for input/filter |
| `destination` | `/coordinator`, `fd:6`, `/input`, `/tool` respectively |

### 8.2 Bounds and bindings

- Aggregate `length` over all objects is at most 16,777,216.
- Coordinator and filter digest and length must also equal the same installed generation's corresponding verified roles and the fixed filter constant. A catalog cannot select an alternate security coordinator or filter.
- M has no catalog, policy, script, or approval hash.
- B2 M cannot contain latent target objects.
- `Hm = SHA256(CJSON1(M))`.
- F references `Hm`; S references `Hm`; K references `Hm`.

---

## 9. F — protected fixture policy schema

Normative extraction from `case-policy-and-action-contract.md` §3.

### 9.1 Exact top-level keys

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `policyKind` | `b2-lifeline-v1` or `b3-static-fixture-v1` |
| `caseId` | equal to K |
| `phase` | equal to K |
| `topology` | equal to K |
| `hostId` | equal to K |
| `bootId` | same canonical boot format as the capability; must equal current boot |
| `fixtureManifestSha256` | equal to `Hm` |
| `allowRelease` | equal to K |
| `maximumSessionNs` | equal to K |
| `uid` | approved nonzero signed-31-bit integer |
| `gid` | approved nonzero signed-31-bit integer |
| `hostPaths` | B2: `null`; B3: exact object below |
| `toolEnrollmentId` | B2: `null`; B3: nonempty S2 ValidId |
| `inputEnrollmentId` | B2: `null`; B3: nonempty S2 ValidId |
| `copy` | B2: `null`; B3: exact object below |
| `invocation` | B2: `null`; B3: exact object below |
| `sandbox` | B2: `null`; B3: exact object below |
| `resources` | B2: `null`; B3: exact object below |

No platform-dependent omitted fields or permissive defaults exist.

### 9.2 B2 policy

- `policyKind` = `b2-lifeline-v1`.
- `topology` = `b2-lifeline`.
- `allowRelease` = `false`.
- `hostPaths`, `toolEnrollmentId`, `inputEnrollmentId`, `copy`, `invocation`, `sandbox`, and `resources` are all JSON `null`.
- No delegated parent, staging, journal, target, writer, or grants may be allocated.
- Existing B2 30/35-second lifetimes and original stop bounds apply irrespective of `maximumSessionNs`.

### 9.3 B3 host paths

`hostPaths` has exact keys `delegatedParent`, `stagingParent`, and `journalParent`. Each value has exactly `path`, `device`, `inode`, `uid`, `gid`, and `mode`.

- `path` is an absolute ASCII POSIX path of at most 1024 bytes, with no empty, `.` or `..` component and no trailing slash.
- `device` and `inode` are positive CJSON integers.
- `uid` and `gid` equal F.
- `mode` is exactly `0700` for staging/journal and `0755` for the delegated cgroup parent.
- Pin the existing directory by no-follow component traversal. Each ancestor must be host-root or approved-UID controlled and not writable by other identities, with no symlinks. Leaf stat must match every field.
- `delegatedParent` must be cgroup v2 with actual reviewed delegation/controller/kill behavior. The field grants no root privilege.
- Compare pinned directory identities again before child allocation.
- Journal and staging children are generated by trusted code from the original execution GUID: journal `va-<executionN>`, staging existing CopyPlan child `prep-001` under fresh `va-<executionN>`, domain existing integrated `int-<executionN>`.
- Exclusive child creation refuses collisions; no adoption or reset.
- HostPath identities are enrollment observations, not evidence that cleanup succeeded.

### 9.4 B3 fixed objects

**`copy`** exact object: `aggregateBytes`=16777216, `maximumObjects`=4, `maximumObjectBytes`=16777216, `hooks`=false. No copy callback executes catalog content.

**`invocation`** exact object: `toolPath`=`/tool`, `mode`=`work`, `inputPath`=`/input`, `argv` exactly `["--in","/input"]`, `environment` exactly ordered `[{"name":"HOME","value":"/tmp"},{"name":"LANG","value":"C"}]`, `workDirectory`=`/tmp`, `stdinEof`=true. The fixed existing `InvocationManifest` mapping, not a shell command assembled from these strings, defines invocation. C/O require exact constants, including no extra environment or argument. Driver/controller launch args are a separate compiled fixed template, not this target argv.

**`sandbox`** exact object:

- `namespaces` exactly `["ipc","mount","net","pid","user","uts"]`.
- `capabilities` = `[]`.
- `noNewPrivs` = `true`.
- `readOnlyDestinations` exactly `["/coordinator","/input","/tool"]`.
- `writableStores` exactly two objects sorted by path: `{"path":"/dev/shm","bytes":1048576,"hasInodeBudget":false,"inodeBudget":0}` then `{"path":"/tmp","bytes":7340032,"hasInodeBudget":false,"inodeBudget":0}`.
- `noAmbientEnvironment` = `true`.
- `noIpcEndpoints` = `true`.
- `noExtraInheritedFds` = `true`.
- `toolExecutable` = `true`.
- `scratchNoexec` = `false`.
- `networkQualification` = `PendingQualification`.

Filter hash and length come exclusively from verified M and the installed filter role. No extra mounts, endpoints, networking promise, writable host source, or unlisted descriptor is granted. These fields reproduce existing B3 `AuthorityManifest` semantics; validation does not relabel pending network/inode qualification as passed.

**`resources`** exact flat object:

| Field(s) | Exact value(s) / units |
|---|---|
| `memoryBytes`, `swapBytes`, `tasks`, `fileDescriptors` | 134217728 bytes, 0 bytes, 48 tasks, 64 fds |
| `tmpBytes`, `shmBytes`, `stdoutBytes`, `stderrBytes`, `captureAggregateBytes` | 7340032, 1048576, 65536, 65536, 131072 bytes |
| `setupTimeoutNs`, `overallTimeoutNs`, `stopAllowanceNs`, `finalDrainNs` | 10000000000, 30000000000, 5000000000, 1000000000 ns |
| `expiresLeadNs` | positive <=120000000000 and <=`maximumSessionNs`; actual operation end additionally capped by original session end, never extends an existing deadline |
| `inodeQualification` | string `PendingQualification` |
| `coreDisabled`, `runtimeDumpsDisabled`, `corePatternObserved` | `true`, `false`, `null`, preserving existing target `CoreDumpPolicy` interpretation |
| `stagingBytes`, `stagingObjects`, `configBytes`, `coordinatorStatusBytes`, `helperStatusBytes`, `journalBytes`, `receiptBytes` | 16777216, 128, 16384, 16384, 16384, 65536, 32768; bytes except `stagingObjects` |

Every numeric resource maps to existing `ResourceBudget(Exact, value, unit, Mandatory)`; there is no zero-as-unlimited interpretation. This policy version only admits the current fixed B3 invocation/security profile. A different target argument, fault command, mount, release mode, or resource policy refuses this schema and needs a separately reviewed version, not an escape hatch.

`Hf = SHA256(CJSON1(F))`. F references `Hm`; S and K reference `Hf`.

---

## 10. S — finite action script schema

Normative extraction from `case-policy-and-action-contract.md` §4.

### 10.1 Exact keys

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `caseId` | equal to K/F |
| `phase` | equal to K/F |
| `topology` | equal to K/F |
| `fixtureManifestSha256` | equal to `Hm` |
| `fixturePolicySha256` | equal to `Hf` |
| `actions` | exactly one of the three templates below |

Each action element has exactly `index` (contiguous 0..n-1), `op` (literal below), and `timeoutNs` (positive <=600000000000 and <=F.`maximumSessionNs`). There are no arguments, expressions, substitutions, labels, jumps, loops, parallel actions, retries, callbacks, conditions, or extension fields. Maximum seven entries.

### 10.2 Exact templates

- B2/no release: `connect-b2, inspect, stop, cleanup` (4 entries).
- B3/no release: `prepare-b3, inspect, stop, cleanup` (4 entries).
- B3/release: `prepare-b3, inspect, release, inspect, stop, cleanup` (6 entries).

F.`allowRelease` and K.`allowRelease` must equal whether the selected template contains release. B2 is always false. Every actor checks the full template before effects; an unknown action or a release in a false case refuses the whole case before B1/preparation, not just when reached.

### 10.3 Hashes and deadlines

- `Ai = SHA256(ASCII("gagamba-validation-action-v1") || 0x00 || CJSON1(S.actions[i]))`.
- `Hs = SHA256(CJSON1(S))`.
- D's `actionSha256` is lowercase hex(`Ai`).
- C recomputes `Ai` from its privately retained S, checks session/case, exact next index, and expected hash, and never dispatches from a D-supplied op or payload.
- At runtime each action gets one deadline `min(original sessionEnd, now + timeoutNs, every already-applicable earlier S3/operation/C3 deadline)` with checked arithmetic. It never resets an existing clock.
- Timeout or an unknown required gate terminates positive progression and invokes retained stop/cleanup.
- Alternative JSON bytes cannot become equivalent inputs because CJSON-1 rejects them before hashing or dispatch.

---

## 11. R/Wc/P and the acyclic approval graph

Normative extraction from `case-policy-and-action-contract.md` §1 and `mechanism-b-session-capability.md`.

### 11.1 R — detached independent catalog review

Exact schema:

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `catalogSha256` | `Hk` |
| `caseId` | equal to K |
| `phase` | equal to K |
| `topology` | equal to K |
| `hostId` | equal to K |
| `artifactSetSha256` | equal to K |
| `qualificationId` | equal to K |
| `qualificationRecordSha256` | equal to K |
| `fixtureManifestSha256` | equal to K |
| `fixturePolicySha256` | equal to K |
| `actionScriptSha256` | equal to K |
| `conclusion` | exactly `supported-for-bounded-validation` |

R contains no owner acceptance hash. `Hr = SHA256(CJSON1(R))`.

### 11.2 Wc — detached owner catalog acceptance

Exact schema:

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `catalogSha256` | `Hk` |
| `catalogReviewSha256` | `Hr` |
| `caseId` | equal to K |
| `phase` | equal to K |
| `topology` | equal to K |
| `hostId` | equal to K |
| `artifactSetSha256` | equal to K |
| `qualificationId` | equal to K |
| `qualificationRecordSha256` | equal to K |
| `decision` | exactly `accept-exact-bounded-validation-catalog` |

`Hw = SHA256(CJSON1(Wc))`.

Wc is mechanism-neutral catalog eligibility. It is **not** authorization to issue either a live invocation or a durable permit. Mechanism B still requires a fresh explicit independent live approval; Mechanism A requires a separate owner-approved durable-grant contract and independently authorized exact persistent permit.

### 11.3 P — protected catalog-eligibility approval

Exact schema:

| Key | Exact requirement |
|---|---|
| `schemaVersion` | integer `1` |
| `catalogSha256` | `Hk` |
| `catalogReviewSha256` | `Hr` |
| `ownerCatalogAcceptanceSha256` | `Hw` |
| `caseId` | equal to K |
| `phase` | equal to K |
| `topology` | equal to K |
| `hostId` | equal to K |
| `artifactSetSha256` | equal to K |
| `qualificationId` | equal to K |
| `qualificationRecordSha256` | equal to K |
| `accepted` | `true` |

P does not contain its own hash. `Hp = SHA256(CJSON1(P))`.

A independently verifies actual V/W provenance through the accepted root-administrative decision process, computes `Hk`/`Hr`/`Hw` from the supplied exact bytes, compares every duplicated value, and writes P at `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`. No tool may generate P merely by observing K/R/Wc candidate files; actual independent administrative acceptance is mandatory. A producer report, timestamp, candidate file, or self-consistent hash set is insufficient.

### 11.4 Publication order

1. Publish R at `/etc/gagamba/validation-catalog-evidence/<Hr>.json` and Wc at `/etc/gagamba/validation-catalog-evidence/<Hw>.json`; root:root `0444`, no-replace, file and directory fsync.
2. Publish P last under the authority/install locks using the same protected publication primitives.
3. Merely installing a catalog does not authorize an invocation. P is eligibility only.
4. A live challenge additionally includes `catalogReviewSha256`=`Hr`, `ownerCatalogAcceptanceSha256`=`Hw`, and `catalogApprovalSha256`=`Hp`.
5. The live response has exactly `schemaVersion`=1, `challengeSha256`, the same three hashes, and `action`=`authorize`. It has no generic `ownerDecisionSha256`/`independentReviewSha256` aliases.
6. A fresh challenge, original pins, and the one-response latch remain mandatory. P alone cannot issue or reissue a B capability.

### 11.5 Acyclic graph

```text
M -> F -> S -> K -> R -> Wc -> P -> live challenge -> live response -> capability
```

- M is constructed first; F references hash(M); S references hash(M) and hash(F); K references hash(M), hash(F), and hash(S).
- K contains no reference to R, Wc, or P.
- R reviews completed K and its transitive bytes; R contains no owner acceptance hash.
- Wc accepts the exact K and R bytes; Wc contains no P hash.
- P binds `Hk`, `Hr`, and `Hw` and is published last.
- No edge returns from P to K, from Wc to K, or from R to K.
- The graph contains no hash fixed point.

### 11.6 Detached-record rules

- R and Wc do not become fields inside K.
- K is not P.
- P is eligibility, not invocation.
- No producer-built P.
- No edge returns into K/F/S.
- The V/W/A labels remain unresolved until D1. They are not silently mapped onto Slice E's A/L/C/R/O roles.

---

## 12. Protected roots, acquisition, and custody

### 12.1 Exact roots

| Artifact | Protected root |
|---|---|
| K | `/opt/gagamba/validation-catalogs/<Hk>.json` |
| M | `/opt/gagamba/validation-fixture-manifests/<Hm>.json` |
| F | `/opt/gagamba/validation-policies/<Hf>.json` |
| S | `/opt/gagamba/validation-actions/<Hs>.json` |
| Fixture source | `/opt/gagamba/validation-fixtures/<Hm>/` |
| R | `/etc/gagamba/validation-catalog-evidence/<Hr>.json` |
| Wc | `/etc/gagamba/validation-catalog-evidence/<Hw>.json` |
| P | `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` |
| F02 qualification (not extended) | `/etc/gagamba/accepted-qualifications/<qualificationId>.json` |
| F02 final approval (separate chain) | `/etc/gagamba/accepted-final-admissions/<artifactSetSha256>.json` |
| F02 final data (separate chain) | `/opt/gagamba/final-admissions/<finalRecordSha256>.json` |
| F02 final selector (separate chain) | `/opt/gagamba/final-admission-selectors/<artifactSetSha256>.json` |
| Host identity | `/etc/gagamba/host-id` |
| Live invocation state | `/run/gagamba/validation-live/<nonce>/` |

### 12.2 Metadata custody

- Immutable single-link regular metadata is root:root `0444`; leaf directories are root:root `0755`; all ancestors are root-controlled and not writable by group or other.
- Exact leaf names derive only from validated hashes. No lookup by case name, `latest`, search, user-provided path, environment, or driver packet exists.
- Root metadata publication uses complete temp/write/fsync/no-replace/directory-fsync before P can refer to it. Identical existing bytes may be reused; conflicting bytes refuse.
- Open trusted directory chains descriptor-relatively; pin the regular leaf; check exact uid/gid/mode/link/size; read through the pinned object into private bounded memory; hash; strictly decode and reencode; reacquire and check metadata unchanged.
- Retain immutable decoded objects plus raw bytes and hashes in one session-scoped private context.
- Metadata fds are `CLOEXEC` and closed after the bounded verified read. No extra inherited authority fds are introduced.
- No code evaluates script text.

### 12.3 Independent acquisition and timing

- Each L/C/R/O independently acquires K using the digest in its authenticated input: L uses protected P plus the requested `Hk`; C uses its authenticated root `OFFER`; R/O use the inherited sealed capability plus original-parent `INIT` and, before owner effects, original-C `DIRECT-CONTEXT`.
- Then each independently opens M/F/S using only the verified K hashes and performs the same checks.
- L completes all reads and semantic validation before B1.
- C completes before B2 and before constructing B3 owner/operation objects.
- R completes before INIT ACK/spawning O.
- O completes before INIT ACK and delays all preparation/staging/authority effects until direct C context and ordinary installation authentication complete.
- A failed or blocked read refuses positive progression with independent stop monitors serviced.
- The memfd/catalog hash is necessary authority binding, not a substitute for those reads.

### 12.4 Fixture source

- Fixture source is `/opt/gagamba/validation-fixtures/<Hm>/` with exact M-listed files only.
- The directory is root:root `0755`; files are root:root and use the mode from M; each is single-link regular; no extra entries, symlink, or special file is allowed.
- L/C/R verify the exact inventory and pinned hashes before their positive gates.
- O opens and pins each source itself before copying, copies from that same verified readable descriptor (not reopening a driver path), hashes during copy, verifies exact length/hash and source metadata before and after, and rechecks the staged object hash, read-only destination, and role against M before capability mint and before RELEASE.
- These source/staging checks are the required adaptation of the existing CopyPlan/VerifyStaged path; caller paths cannot be injected.
- On error, existing cleanup/Unknown discipline is retained.
- Active-session metadata is immutable; reopening current approvals is not required to stop.

---

## 13. Permitted uses

The following uses are permitted **only within their stated limits**:

1. **Repository review and reconstruction.** A reviewer may read this record to reconstruct the E-2 K/M/F/S/R/Wc/P contract without relying on an unpublished external file.
2. **Hash and acceptance verification.** A reviewer may recompute the recorded SHA-256 values and compare them with the committed acceptances.
3. **P0 owner-acceptance evidence.** This record may be submitted for the separate P0 owner-acceptance decision.
4. **D1 reference, if separately commissioned.** D1 may use the record to identify exact representations, subject to D1's own authority and its express prohibition on relying on unresolved schema provenance for final adoption.
5. **Future E-2 design and implementation planning.** The record may inform a fresh E-2 entry assessment and a later bounded implementation commission **only after** the required owner decisions. It does not itself authorize either.
6. **Preservation of the accepted separation.** Reviewers may use the record to confirm that integrity, provenance, approval, and authorization remain distinct.

No permitted use includes creating runtime authority, a catalog instance, a protected store, an approval record, a capability, a trust anchor, or a production consumer.

---

## 14. Prohibited uses

The following are prohibited:

1. **No schema adoption by this record.** It is proposed, not accepted. It does not create a schema version, field, canonical rule, or in-repository authority.
2. **No production or test code.** No schema reader, catalog store, verifier, harness, parser, driver, launcher, capability, authority factory, or E-1 consumer may be created from this record.
3. **No catalog or approval instance.** No K, M, F, S, R, Wc, or P instance may be created, provisioned, or treated as approved.
4. **No trust anchor or signature.** None is created or implied.
5. **No hash-as-approval.** A matching digest does not establish human provenance, owner acceptance, catalog approval, or authorization.
6. **No F02 expansion.** F02 qualification-store authority remains limited to `/etc/gagamba/accepted-qualifications/<qualificationId>.json` and does not extend to validation catalogs or the final-admission chain.
7. **No V/W/A mapping.** The external V/W/A labels must not be silently mapped onto Slice E's A/L/C/R/O roles. D1 must establish any mapping and authority explicitly.
8. **No producer-built P.** A producer, publisher, timestamp, candidate file, or self-consistent hash set cannot create approval.
9. **No K-as-P or P-as-invocation.** K is not approval; P is eligibility, not a live capability or durable permit.
10. **No detached-record collapse.** R and Wc must not become fields inside K, and no edge may return from R/Wc/P into K/F/S.
11. **No positive validation or protected execution.** No validation context, capability, catalog approval, or protected native execution may be activated.
12. **No E-1 promotion.** E-1 integrity evidence must not be promoted into positive validation authority.
13. **No cross-host reuse or standing approval.** A future catalog approval must be host/scope-specific and bounded; no cross-host portability or unattended standing authority is created.
14. **No history rewrite.** Accepted bytes, historical records, and the 44 preserved untracked paths must not be rewritten, cleaned, or silently replaced.

---

## 15. Replacement, supersession, and conflict rule

1. **Content-addressed identity.** Every artifact is identified by the SHA-256 of its exact canonical bytes. Any byte change creates a different object and a different digest. A previous digest must not be reused for changed bytes.
2. **No silent substitution.** No K, M, F, S, R, Wc, or P representation may be replaced by a different byte sequence under the same identity. Conflicting bytes refuse.
3. **No mid-attempt reapproval.** A different approval byte set for the same `Hk` is not silently adopted mid-attempt. Reapproval requires independent explicit administrative action and a new invocation. Existing sessions hold their original immutable inputs.
4. **Withdrawal/replacement ordering.** P withdrawal/replacement is serialized with B1/A-consumption: removal first refuses new issuance; issuance first preserves that one session. No tool reconstructs approval from data or selects an orphan by search.
5. **Record supersession.** This P0 record may be superseded only by a later owner-accepted provenance record or an owner decision that expressly identifies the superseded bytes and the replacement scope. Historical records remain preserved.
6. **Correction rule.** A correction must be additive and must identify the exact prior bytes and the reason. It may not edit an accepted historical record in place or imply retroactive approval.
7. **Scope preservation.** A later acceptance of this record closes Q-E1 only for the E-2 subset it expressly accepts. The GAVA1 header, v3 validation-pre layouts, and release correlation remain open for their own reconciliation.

---

## 16. Unresolved gaps and authority questions

| ID | Gap or question | Status |
|---|---|---|
| P0-G1 | The external design package is local under `Solo/` and has no independently verified Git commit. Its identity is content-manifest-based. | **QUALIFICATION** — exact bytes and hashes are verified; publication/durability is not established. |
| P0-G2 | Historical external documents carry `PENDING` captions. | **QUALIFICATION** — later committed decisions govern the semantics they expressly accept; captions are preserved unchanged. |
| P0-G3 | S2 ValidId grammar is referenced by K/F/qualification fields. | **RESOLVED REFERENCE** — the in-repository `QUALIFICATION-RECORD-REVIEW.md` §1 supplies the exact grammar: 1–128 `[A-Za-z0-9._-]`, no leading/trailing dot, reject `.`/`..`, separators, whitespace, non-ASCII; strict ASCII, case-sensitive. |
| P0-G4 | Who may review V, accept W, and provision P as A; whether the external chain is adoptable; how V/W/A maps to Slice E roles; catalog withdrawal/reapproval authority. | **D1-DEFERRED** — Q-E2 remains open. P0 does not adopt or infer an answer. |
| P0-G5 | GAVA1 header, v3 validation-pre layouts, and release correlation for E-3/E-4. | **DOWNSTREAM-DEFERRED** — not E-2 dependencies; remain within composite Q-E1 for later reconciliation. |
| P0-G6 | P0 owner acceptance has not been issued. | **OPEN** — required before Q-E1 closure for the E-2 subset and before D1 final adoption. |
| P0-G7 | No native qualification, protected-store provisioning, catalog instance, or positive validation exists. | **PRESERVED LIMIT** — P0 makes no native or runtime claim. |
| P0-G8 | No independent multi-model review of this P0 record has occurred. | **LIMIT** — this is a single-reviewer governance reconciliation. |

No unresolved gap was converted into an invented field, schema version, trust anchor, approval, or authority.

---

## 17. Reproduction and review handoff

### 17.1 Exact identity and verification

The P0 evidence package at `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/` records the baseline, preservation inventory, all artifact hashes, the acceptance mapping, the source/authority state, and reproducible commands. The companion P0 report is `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md`.

Minimum verification:

1. Confirm `HEAD` and tree against §4.4 and the evidence baseline.
2. Recompute all 15 F02 manifest entries and confirm the manifest hash `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`.
3. Recompute `independent-recheck.md` and confirm `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6`.
4. Recompute the three Alternative B inputs and confirm the hashes in §4.3.
5. Recompute the committed record hashes in §3.1 and confirm ancestry with `git merge-base --is-ancestor`.
6. Confirm the source diff from `4c5a42f0fecff90dcb30088ca0203956e7a80032` to the P0 baseline for `src`, `tests`, and `eng` is empty.
7. Confirm the 44 historical untracked paths remain byte-identical to `docs/evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json`.

### 17.2 Review handoff

An independent reviewer should verify:

- the exact baseline and preservation inventory;
- every digest in §4;
- the acceptance chain and ancestry in §5;
- the field sets, bounds, cross-field bindings, roots, and graph in §§6–12 against the accepted external bytes;
- that no schema/catalog/runtime behavior was implemented;
- that Q-E1/Q-E2 remain open, D1 has not started, and E-2 entry/implementation remain unauthorized;
- that the record does not treat hash agreement as approval.

The reviewer's conclusion is not a substitute for the separate P0 owner acceptance.

### 17.3 Disposition

This record is **P0-reconciled and proposed for owner acceptance**. It does not itself close Q-E1 or Q-E2, does not adopt the validation-catalog chain, and does not authorize E-2 implementation.

Required next decision: a separate **P0 owner acceptance** decision.
