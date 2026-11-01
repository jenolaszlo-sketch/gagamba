# RC2B-06 B6-SPEC-R1-Q2 — Bounded Correction Receipt

**Commission:** GAGAMBA-RC2B-06-B6-OWNER-01 (owner-directed bounded corrections).

**Status:** documentation-only. No implementation, test execution, native run,
installer, package build, host change or production activation is claimed or performed.
No source, test, native, engineering or workflow path is changed.

**Source reviewed by the independent Q2 reviewer:**
`77ee4414df37b9f88979bde02742756538ba45af` (design/plan) with evidence/pointers at
`5b3466719a50df072848ecc5f832adbaeb06b3bf`.

**Independent reviewer disposition (preserved):**
**`B6-SPEC-R1-Q2 SUPPORTED WITH QUALIFICATIONS`** — see the canonical index
[../reviews/rc2b-06-b6-spec-r1-q2-independent-review.md](../reviews/rc2b-06-b6-spec-r1-q2-independent-review.md)
and the preserved reviewer findings at
[../reviews/rc2b-06-b6-spec-r1-q2-review-evidence/FINDINGS.md](../reviews/rc2b-06-b6-spec-r1-q2-review-evidence/FINDINGS.md).

---

## 1. Nature and limits of these corrections

- These are **owner-applied bounded documentation corrections**, not independently
  requalified fixes. A correction is marked **addressed by the owner**; it is **not**
  described as independently verified unless an independent check exists.
- The independent reviewer's findings, rationale, verdict and evidence bytes are
  preserved unchanged. Nothing in this receipt is attributed to the independent reviewer.
- No correction changes a security-relevant contract, message ordering or authority.
  Each is wording, numbering, grammar or provenance precision. If a correction had
  changed substantive trust semantics, ordering or authority, a focused independent
  delta review would have been required before implementation authorization; **none of
  these corrections did**, so no delta review is triggered. This determination is the
  owner's and is open to independent re-check.
- The original Q1 review (`08ebcb5`), the R1 remediation evidence and the BIND-01
  receipt, and all independent review documents are **not modified** by this receipt.

---

## 2. Finding disposition summary

| Finding | Severity (reviewer) | Owner disposition | Location amended |
|---|---|---|---|
| Q2-F01 | Minor | Bounded documentation correction — addressed by owner | design §6.3 prop. 8; plan T13 |
| Q2-F02 | Minor | Bounded documentation correction — addressed by owner | plan: duplicate `B6-T20` removed |
| Q2-F03 | Minor | Bounded documentation correction — addressed by owner | design §2 and §6 subsection order/numbering |
| Q2-F04 | Significant qualification | Separate owner decision recorded as **HD-5**; label corrected | design §2.7; design §10.3; owner acceptance |
| Q2-F05 | Minor | Correct evidence description — addressed by owner | plan T25-A evidence line |
| Q2-F06 | Minor | Explicit grammar + trusted-acceptance wording — addressed by owner | design §4.5; plan T20c subcase 8 |
| Q2-F07 | Minor | Contextual attribution corrected (actual checkpoint labeled) — see §3 | this receipt (historical evidence untouched) |
| Q2-F08 | Minor | Snapshot attribution corrected (actual checkpoint labeled) — see §3 | this receipt (historical evidence untouched) |
| Q2-F09 | Minor | Reconcile wire/identity descriptions — addressed by owner | design §6.2.3 |

No Blocking finding existed; none is created. No finding is left unaddressed.

---

## 3. Q2-F07 / Q2-F08 — stale checkpoint attribution

The Q2 findings Q2-F07 and Q2-F08 concern two rows in the **historical** R1 producer
evidence `docs/evidence/rc2b-06-b6-spec-r1-remediation.md` §1. The reviewer's own
smallest remediation for both was "label the actual checkpoint" and explicitly noted
that **no producer evidence edit is required or performed**. To avoid rewriting a
historical evidence snapshot (commission instructions forbid modifying historical
evidence documents), the actual checkpoints are labeled **here** instead:

- **Q2-F07 (`docs` tree `f958119d425a0cc4fd6613dae8383710b9bffab8`).** That value is
  the **pre-B6 baseline** (`2cd8c1d`) `docs` tree, recorded in a table whose other rows
  describe baseline identities. The **post-remediation** `docs` trees are:
  - `5e05f37e3a34169d877ce72071e96af97d16fab8` at the R1 remediation `77ee441`; and
  - `17f0b3047fcca90cc9ca1de72f5b56529350e1cc` at the receipt `5b34667`.
  The row is a pre-remediation baseline value; the authoritative commit/tree identities
  remain exact. No source-binding ambiguity results.
- **Q2-F08 (`origin/main` "equal; no ahead/behind").** That statement describes the
  **authoring baseline** (`08ebcb5`), where local `main` equaled `origin/main`
  (`2cd8c1d`). After the R1 remediation and BIND-01 receipt commits, local `main` is
  `5b34667` and is **ahead 4, behind 0**; `origin/main` remains `2cd8c1d`. Nothing was
  pushed. The row is a historical snapshot, not a current-state claim.

Neither observation contradicts a mandatory trust guarantee; both are historical
precision matters. The historical R1 evidence file is preserved unchanged.

---

## 4. Q2-F01 — residual FD wording

The corrected §5.2 step 6 wording ("copied from verified source paths with claimed
digests under a verified staging parent") is now used consistently.

- **Design §6.3 property #8** previously read "coordinator/filter staged from verified
  fds". It now reads: "coordinator/filter staged from verified source paths with claimed
  digests under a verified staging parent (no retained source-root fd); Bubblewrap
  executes the pinned coordinator through `/proc/self/fd/11`."
- **Plan T13** previously read "coordinator/filter staged from verified fds". It now
  reads: "Bubblewrap fexecve through `/proc/self/fd/11`; coordinator/filter staged from
  verified source paths with claimed digests under a verified staging parent, with no
  retained source-root fd".

The independently supported `/proc/self/fd/11` Bubblewrap execution semantics
(Q1-F12) are retained. No trust claim is changed; the path-based source handling,
claimed-digest staging boundary and its root-only residual are now described
consistently. **Owner disposition: addressed (not independently requalified).**

---

## 5. Q2-F02 — duplicate T20 section

The qualification plan contained the section
`### B6-T20 — Installation verification unavailable` twice (near the original lines
399 and 533). The later duplicate (between T20c and T21) has been removed. The distinct
`B6-T20a`, `B6-T20b` and `B6-T20c` assertions are unchanged, and the plan now lists
`B6-T20` exactly once (traceability §9 already listed it once). **Owner disposition:
addressed.**

---

## 6. Q2-F03 — section numbering artifacts

Document order is now monotonic with unique subsection numbers; no accepted semantics
changed:

- **Design §2** — the native `.NET` runtime dependency closure (§2.7) has been moved to
  its numeric position after §2.6, giving order `2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7`.
  The identifiers §2.5 (test-only assets), §2.6 (Gate A result) and §2.7 (native
  closure) are preserved unchanged, so existing cross-references remain valid.
- **Design §6** — "Recovery after controller death" (already numbered §6.4) now
  physically precedes "Deployment generations" (already numbered §6.5), and the final
  "Gate D result" heading has been renumbered from a **second** §6.5 to **§6.6**. The
  order is now `6.1, 6.2, 6.3, 6.4, 6.5, 6.6`.

  Cross-reference note: historical documents (including the R1 evidence and the Q1
  review) that cite "§6.5" refer to **Deployment generations**, which retains the
  number §6.5. Only the previously duplicated "Gate D result" number changed (to §6.6).
  **Owner disposition: addressed.**

---

## 7. Q2-F04 — globalization decision label

The design no longer attributes the globalization-mode choice to "HD-3 scope".

- **Design §2.7** now names the globalization mode as its own owner decision
  (**HD-5**), separate from the native-host platform-scope decision (HD-3), and records
  that invariant globalization is conditional on demonstrated compatibility and does not
  remove the OpenSSL/zlib/native-runtime obligations or alter the untrusted target.
- **Design §10.3** now enumerates **HD-5** alongside HD-1–HD-4.

The choice itself is an owner decision recorded in the B6 owner-acceptance decision
([../decisions/rc2b-06-b6-spec-owner-acceptance.md](../decisions/rc2b-06-b6-spec-owner-acceptance.md)).
This is a cross-reference/label correction; it creates no unsafe default. **Owner
disposition: addressed.**

---

## 8. Q2-F05 — T25-A evidence claim

T25-A previously asserted that "Exact test names, skip reasons, and source lines" are
recorded in the remediation evidence artifact, but that artifact records configurations
and the source range `B3RestrictedLifecycleTests.cs:628–800`, not exact test method
names. The evidence line now states that the artifact records **configurations and
source lines, not exact test method names**, that no exact test-method identity is
claimed unless independently established, and that the accounting is documentary only.
T25-B remains the mandatory installed-closure qualification. **Owner disposition:
addressed.**

---

## 9. Q2-F06 — qualification identifier grammar and trusted acceptance

### 9.1 Identifier grammar (design §4.5)

A strict canonical `qualificationId` is now defined:

- Permitted ASCII characters `A-Z`, `a-z`, `0-9`, `.`, `_`, `-`; length 1–128.
- Case-sensitive; canonical form is the exact ASCII bytes with no folding, normalization
  or alias.
- Rejected: characters outside the set (including `/` and `\`, NUL/control, whitespace,
  non-ASCII); `.`/`..` as the whole value; leading/trailing `.`; empty or over-length.
- Safe path construction: validate the identifier first, then open exactly one path
  `/opt/gagamba/qualifications/<qualificationId>.json` with the descriptor-relative
  `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS` policy; a malformed
  identifier is a refusal, not a search.
- Exactly one association; no enumeration or latest-file-wins; absent/extra/ambiguous/
  malformed/unauthenticated record → `Unqualified`.
- The receipt binds the exact `qualificationRecordSha256`; verification step 3 now
  validates the grammar before opening the record.

### 9.2 Trusted acceptance wording (design §4.5; plan T20c subcase 8)

The acceptance basis is now stated as the **root-owned (uid 0) 0444 record** selected by
the receipt-bound `qualificationId` + `qualificationRecordSha256`, not the record's
`acceptedAtUtc` field content. A trusted owner/operator action must reference an accepted
qualification decision and the exact artifact-set identity; a timestamp, receipt or
producer-generated report cannot approve itself; `qualify-install.sh` is a future
implementation proposal, not an implemented or independently qualified authority. If the
record cannot be authenticated against the trusted installation policy, verification
returns `Unqualified`. **Owner disposition: addressed (not independently requalified).**

---

## 10. Q2-F09 — identity struct vs echo payload projection

Design §6.2.3 now states the explicit projection from the §6.1 `InstallationIdentity`
struct to the echo payload: which fields are carried (`ArtifactSetSha256`, the
role-specific closure at offset 32 selected by `Role`, `OwnerProtocol`,
`CoordinatorProtocol`, `Bootstrap`, `Qualified`, optional `ProfileId`/`ProfileVersion`)
and which are not carried separately (`ManifestSchema`, `CoordinatorSha256`,
`SeccompFilterSha256`, `RuntimeInventorySha256`). The projection adds no field, changes
no message id, ordering, channel or authority, and preserves the owner/recovery
authority separation and exact accepted ordering. No new release-bearing field is
invented. **Owner disposition: addressed.**

---

## 11. Preserved and unchanged

- **Production/test/native/tooling/workflow:** `src/`, `tests/`, `native/`, `eng/` and
  `.github/` are unchanged by this correction commit.
- **Historical documents:** the Q1 review, the R1 remediation evidence, the BIND-01
  receipt, the original specification evidence and all independent review documents are
  unchanged. The preserved Q2 reviewer bytes (canonical repository copies and external
  custody) are unchanged.
- **Architecture:** root of trust, exact-artifact-set identity, receipt/qualification
  association, verification-to-use, authority separation, refusal behavior and the
  B6-T01–T25 / T20a/b/c / T25-A/B plan shape are unchanged in substance.

## 12. Disposition

**Q2-F01–F09 addressed by bounded owner corrections.** No correction changes
security-relevant trust semantics, message ordering or authority, so a focused
independent delta review is not triggered. These corrections are **not** independent
requalifications and are recorded as owner-applied.
