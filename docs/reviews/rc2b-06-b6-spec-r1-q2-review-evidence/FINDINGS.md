# B6-SPEC-R1-Q2 — Findings

Severity: **Blocking** · **Significant qualification** · **Minor evidence/documentation**.
No finding was classified Blocking. One is Significant qualification; the rest are
Minor. Each finding states the exact source location, claim challenged, controlling
authority, documentary contradiction, expected vs actual contract, consequence, the
smallest required remediation, and whether it blocks Q2 acceptance.

Reviewed source: design/plan @ `77ee441`; evidence/pointers @ `5b34667`; Q1 review @ `08ebcb5`.

---

## Q2-F04 — Globalization-mode decision mislabelled as HD-3 (Significant qualification)

- **Source location.** `docs/design/rc2b-06-b6-installed-artifact-trust.md` @ `77ee441`, §2.7, lines ~277–280; cross-reference in §10.3.
- **Claim challenged.** That the invariant-vs-non-invariant globalization choice is governed by an existing owner decision (HD-3).
- **Controlling authority.** Design §10.3 (HD definitions); R1 remediation §7 (HD scope).
- **Contradiction.** §2.7 says "If the owner decides otherwise (HD-3 scope), the non-invariant row applies." HD-3 as defined is the **native-host/platform** decision (WSL2-only vs native Linux), not a globalization-mode decision. The globalization mode is not enumerated among HD-1–HD-4.
- **Expected contract.** The globalization-mode choice should be an explicitly enumerated owner decision (or an explicitly recorded design default) separate from HD-3.
- **Actual contract.** The choice is presented as a recommendation tied to "HD-3 scope."
- **Consequence.** An owner approving HD-3 (WSL2-only) could believe the globalization mode is thereby settled. It is not. However, **no unsafe default is created**: the recommended default (invariant globalization) eliminates ICU, and the alternative requires the ICU version be recorded as a platform-baseline limitation with refusal behavior. The native dependency is classified and verified in both modes.
- **Smallest remediation.** Reword §2.7 to name the globalization mode as its own owner decision (e.g. "HD-5 (globalization mode)") or as a required recorded design default, and remove the "HD-3 scope" attribution.
- **Blocks Q2 acceptance?** No. Does not contradict a mandatory trust guarantee and creates no unsafe default.

---

## Q2-F01 — Residual "staged from verified fds" overstatement (Minor)

- **Source location.** Design §6.3 property #8 (line 1132) and plan T13 (line 294) @ `77ee441`.
- **Claim challenged.** That coordinator/filter staging is fd-anchored at the source.
- **Controlling authority.** Q1 finding B6-Q1-F03; design §5.2 step 6 (F03 remedy).
- **Contradiction.** §5.2 step 6 was corrected to "claimed-digest path copies under a verified staging parent," but §6.3 property #8 and plan T13 retain "coordinator/filter staged from verified fds" — the exact phrase Q1 flagged as an overstatement of the current API.
- **Expected contract.** Consistent use of the corrected (claimed-digest path) wording.
- **Actual contract.** Two locations retain the old phrase.
- **Consequence.** Cosmetic overstatement; the underlying content is still claimed-digest bound, so no trust boundary is crossed. Reduces precision.
- **Smallest remediation.** Replace the two residual phrases with the corrected wording.
- **Blocks Q2 acceptance?** No.

---

## Q2-F02 — Duplicate `B6-T20` section in the qualification plan (Minor)

- **Source location.** `docs/plans/rc2b-06-b6-qualification.md` @ `77ee441`, lines 399 and 533 (both headed `### B6-T20 — Installation verification unavailable`).
- **Claim challenged.** Plan precision/uniqueness of test identifiers.
- **Controlling authority.** Plan §9 traceability lists B6-T20 once.
- **Contradiction.** The same section appears twice; the traceability table does not.
- **Expected contract.** Each test appears exactly once.
- **Actual contract.** T20 duplicated.
- **Consequence.** A reader could mis-scope or double-count T20. No obligation is changed.
- **Smallest remediation.** Delete the duplicate section (lines ~533–546).
- **Blocks Q2 acceptance?** No.

---

## Q2-F03 — Section-numbering artifacts in the amended design (Minor)

- **Source location.** Design §2 and §6 @ `77ee441`.
- **Claim challenged.** Internal document reference integrity.
- **Contradiction.** §2 order is 2.1, 2.2, 2.3, 2.4, **2.7**, 2.5, 2.6. §6 order is 6.1, 6.2, **6.3**, **6.5** (Deployment generations), **6.4** (Recovery after controller death), **6.5** (Gate D result) — two §6.5 headings and a §6.4 after a §6.5.
- **Expected contract.** Monotonic, unique subsection numbering.
- **Actual contract.** Out-of-order and duplicated numbering introduced by the R1 insertions.
- **Consequence.** Cross-references to §2.5/§2.6/§6.4/§6.5 become ambiguous to readers. No contractual change.
- **Smallest remediation.** Renumber §2.5–§2.7 and §6.3–§6.6 in document order.
- **Blocks Q2 acceptance?** No.

---

## Q2-F05 — T25-A evidence-claim mismatch (Minor)

- **Source location.** Plan T25-A (line ~655) and R1 remediation evidence §4.1 @ `5b34667`.
- **Claim challenged.** That the exact previously-skipped test names are recorded.
- **Controlling authority.** Q1 finding B6-Q1-F05 (enumerate the exact migrated test names).
- **Contradiction.** T25-A says "Exact test names, skip reasons, and source lines recorded in the remediation evidence artifact," but the remediation evidence records only four configurations and the line range `B3RestrictedLifecycleTests.cs:628–800` — not the exact test method names.
- **Expected contract.** Either the exact names are recorded, or the claim is narrowed.
- **Actual contract.** An assertion of recorded exact names that the artifact does not contain.
- **Consequence.** Minor documentary inaccuracy; the accounting split (T25-A vs T25-B) is nonetheless unambiguous.
- **Smallest remediation.** Narrow the T25-A sentence to "configurations and source lines recorded," or add the exact test names to the evidence artifact (without altering its historical bytes).
- **Blocks Q2 acceptance?** No.

---

## Q2-F06 — Identifier/acceptance precision in qualification-record selection (Minor)

- **Source location.** Design §4.5 @ `77ee441`; plan T20c subcase 8.
- **Claim challenged.** Deterministic, fail-closed selection and acceptance semantics.
- **Contradiction.** (a) The design opens `/opt/gagamba/qualifications/<qualificationId>.json` but states no bounded identifier grammar for `qualificationId` (no separators/`..`/empty). (b) T20c subcase 8 requires the record to have "`acceptedAtUtc` from trusted owner action," but the actual basis is root ownership of the file, not the field content.
- **Expected contract.** A bounded `qualificationId` grammar and an acceptance basis expressed as file ownership.
- **Actual contract.** Neither is stated explicitly.
- **Consequence.** Outside the declared trust boundary (receipt/record are root-owned; a root actor is out of model), so not security-critical. A format constraint and ownership-based wording are defense-in-depth and precision improvements.
- **Smallest remediation.** Add a bounded-identifier rule for `qualificationId` (e.g. `[A-Za-z0-9._-]{1,128}`, no separators) and restate acceptance as "root-owned 0444 record" rather than "field from trusted source."
- **Blocks Q2 acceptance?** No.

---

## Q2-F07 — Stale `docs`-tree baseline observation in R1 evidence (Minor, non-blocking)

- **Source location.** `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` §1 @ `5b34667` (and unchanged by the receipt).
- **Claim challenged.** That the recorded `docs` tree describes the reviewed tree.
- **Contradiction.** The row records `f958119d425a0cc4fd6613dae8383710b9bffab8`, which is the **pre-B6 baseline** (`2cd8c1d`) `docs` tree. The final committed `docs` trees are `5e05f37e…` (`77ee441`) and `17f0b304…` (`5b34667`).
- **Expected contract.** The row should state it is the baseline value, or record the final tree.
- **Actual contract.** A baseline value in a table where other rows also read as baseline ("unchanged from baseline"); the annotation "(modified by this remediation)" provides context.
- **Consequence.** A reader could momentarily misread the value as the post-remediation tree. The authoritative commit/tree identities (`77ee441`/`62736d35…`, `5b34667`/`87bb8f73…`) are exact and independently verified, so no source-binding ambiguity results.
- **Classification.** Harmless historical/baseline observation with sufficient context. The authoritative identities remain unambiguous.
- **Smallest remediation (optional).** Label the row explicitly "baseline (pre-remediation)"; no producer evidence edit is required or performed by Q2.
- **Blocks Q2 acceptance?** No.

---

## Q2-F08 — Stale `origin/main` observation in R1 evidence (Minor, non-blocking)

- **Source location.** `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` §1 @ `5b34667`.
- **Claim challenged.** That `origin/main` is "equal; no ahead/behind."
- **Contradiction.** The statement was true at the authoring baseline (`08ebcb5`). After the R1 and receipt commits, local `main` is `5b34667` and is ahead four, behind zero.
- **Expected contract.** The row describes the pre-remediation remote relationship.
- **Actual contract.** A historical snapshot that no longer describes the final local/remote relationship.
- **Consequence.** None for the security contract; the authoritative identities and the "not pushed" property are unambiguous.
- **Classification.** Harmless historical observation with sufficient context.
- **Smallest remediation (optional).** Date-qualify the row; no producer evidence edit required.
- **Blocks Q2 acceptance?** No.

---

## Q2-F09 — InstallationIdentity struct vs echo payload field-set variance (Minor)

- **Source location.** Design §6.1 (`InstallationIdentity`) vs §6.2.3 (`InstallationEchoRequest` payload) @ `77ee441`.
- **Claim challenged.** Internal consistency of the wire specification.
- **Contradiction.** The §6.1 struct includes `ManifestSchema` and both `OwnerClosureSha256` and `RecoveryClosureSha256`; the §6.2.3 echo payload omits `ManifestSchema` and carries a single role-closure field (offset 32) reused as "the sender's role closure." Functionally resolved by the `Role` bit and by the controller comparing each field against its expected value, but the field sets are not identical.
- **Expected contract.** The identity struct and the wire echo should enumerate the same fields (or state the projection explicitly).
- **Actual contract.** Two field sets that overlap but are not identical.
- **Consequence.** Minor implementer ambiguity; no trust-boundary effect, and T20b exercises the handshake.
- **Smallest remediation.** State the projection from `InstallationIdentity` to the echo payload (which fields are carried and which are direction-specific).
- **Blocks Q2 acceptance?** No.

---

## Severity summary

| ID | Severity | Short name | Blocks Q2? |
|---|---|---|---|
| Q2-F04 | **Significant qualification** | Globalization mode mislabelled as HD-3 | No |
| Q2-F01 | Minor | Residual F03 "verified fds" wording | No |
| Q2-F02 | Minor | Duplicate T20 section in plan | No |
| Q2-F03 | Minor | Design section-numbering artifacts | No |
| Q2-F05 | Minor | T25-A evidence-claim mismatch | No |
| Q2-F06 | Minor | `qualificationId` grammar / acceptance wording | No |
| Q2-F07 | Minor | Stale `docs`-tree baseline observation | No |
| Q2-F08 | Minor | Stale `origin/main` observation | No |
| Q2-F09 | Minor | InstallationIdentity vs echo field-set variance | No |

**No Blocking finding.** No mandatory trust guarantee is contradicted. Q2-F04 is a
bounded owner-decision/dependency disclosure. Q2-F01–F03, F05, F09 are wording,
formatting or precision issues. Q2-F06–F08 are non-blocking precision/observation
issues, two of which are the known residual baseline-table entries the Q2 mission
asked to classify.
