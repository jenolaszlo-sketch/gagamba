# RC2B-05-B5 Owner Acceptance and Qualification Preservation

**Commission:** GAGAMBA-RC2B-05-B5-OWNER-01.

**Date recorded:** 2026-11-01 (Asia/Manila). Documentation and decision recording
only. No implementation, remediation, publication, B6/B7 or Hufu work is
authorized by this document.

---

## 1. Verified Git Binding and Source Identities

| Identity | Verified value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting baseline | `e7818b5a876c220cb4f803b6cc1f9c55027d3c66` (tree `d035ae70ba1b3f95dd5800dd16815c8485b2e112`) |
| B5 implementation | `0bf045d0ec3e4cb560edb2bc3b4e7065efaa7b9c` (tree `80d138f81c82b61243e23c0865eb6a1ca516cc54`) |
| Producer evidence commit | `9cba151ee82bdba301251f0c12fc1550c9e6d5d6` (tree `155da2cfd0d185eba0b6187b3e69f390cd6c348e`) |
| Independent B5-Q1 review commit | `d5852f46bb3db08b8f37386851bf22bd7a99b48f` (tree `61fbd419007cd1c4567a63c1a9c636b95f7448ee`) |
| Independent review document | [rc2b-05-b5-q1-independent-security-review.md](../reviews/rc2b-05-b5-q1-independent-security-review.md), blob `62a802b277c6b46aca0553a05ab7b1c092991fce` |
| Producer evidence report | [rc2b-05-b5-impl-01.md](../evidence/rc2b-05-b5-impl-01.md) |
| Raw evidence manifest | [rc2b-05-b5-impl-01-sha256-manifest.txt](../evidence/rc2b-05-b5-impl-01-sha256-manifest.txt), 108 entries, SHA-256 `476809507ab458c59670d439f437d1d9606528eab8f3db8f14bba466638c0dd2` |
| Compact case summary | [native-summary.json](../evidence/rc2b-05-b5-impl-01/native-summary.json) |

Verified linear ancestry (each successor's sole parent is its predecessor):

`e7818b5a` (authorized baseline) → `0bf045d0` (B5 implementation; 27 changed
files) → `9cba151e` (producer evidence delivery) → `d5852f4` (review document
only, 1 file added) → this owner-decision commit.

Preservation checks performed at this decision:

- `9cba151e` changed exactly five paths: the B5 evidence report, its 108-entry
  manifest, `native-summary.json`, `docs/handoff.md` and `docs/work-queue.md`.
  No production or test source changed in the evidence commit.
- `d5852f46` added only the independent review document; no producer evidence,
  historical decision or pointer document changed in that commit.
- All original unrelated tracked and untracked material remains in place. The
  pre-existing historical worktree count remains **74**; no new branch or
  worktree was created.
- `artifacts/rc2b-05-b5-impl-01/` (git-ignored raw evidence, including
  `SHA256SUMS.txt` and the committed-profile logs) is present and unchanged.

---

## 2. Independent Review Verification

### 2.1 Actual verdict and findings

The complete B5-Q1 report states:

> **Verdict: SUPPORTED WITH QUALIFICATIONS**

Its final section recommends that the owner "Accept B5 as implemented with the
stated qualifications" and explicitly recommends **against** production
activation, B6/B7, Hufu integration, package publication, or any native
Linux/macOS qualification based on that review.

The report records **no material contract violation**. Findings B5-F01 through
B5-F07 are all classified as *Qualification*, and limitations B5-L01 through
B5-L07 are disclosed deferrals:

| Finding | Subject | Owner reading |
|---|---|---|
| B5-F01 | Cross-process epoch limited to same basis + compatible boot context | Correctly documented limitation, not a defect |
| B5-F02 | Exact `Stopwatch.Frequency` basis match required | Conservative refusal, correct |
| B5-F03 | Zero-budget Policy A kill + one immediate observation | Correct implementation |
| B5-F04 | Typed kill/population mapping | Correct; `Stopped` requires `Accepted + Empty` |
| B5-F05 | Journal persistence-uncertainty precedence | Correct; never fabricates `Failed` or `Confirmed` |
| B5-F06 | C1/C2/C4 release-authority preservation | No regression found |
| B5-F07 | INT-01 cleanup interpretation | 20 Confirmed / 12 Unknown preserved |

No undisclosed acceptance-blocking finding was found in the full report.

### 2.2 Independent execution inventory (strictly as reported)

The review is explicit about what it did independently:

- **Independently performed:** verification of the five source/tree identities
  (§A); recomputation and verification of all 108 manifest entries against the
  committed manifest (§A); inspection of the exact source paths, typed outcome
  tables, aggregate precedence and preservation surfaces (§C–§H).
- **Not independently executed:** native reproduction. The report states:
  "Independent native reproduction was not executed in this review
  (environment not available). The assessment is **inspection-only** for native
  cases." B5-L07 records the same limitation.
- **Deterministic inventory:** the report says the deterministic test inventory
  and failing-before log are "independently verifiable from the committed
  evidence"; it does not claim a reviewer test rerun or bind a reviewer run log.

Accordingly, **no test execution result was independently reproduced**. All
61/61, 3/3, 33/33, 32/32 and 1,050/269 counts are producer-executed results that
the reviewer inspected. The owner did not rerun tests for this decision either;
the owner checks in §3 are artifact-integrity and log inspections, not reruns.
Nothing inspected-only is recorded here as independently reproduced.

### 2.3 Review-report precision clarifications (owner-observed, non-blocking)

The report's §B log citations are imprecise in three places, and the producer
report contains one stale identity line. None changes the verdict or an
accepted count, because each headline number is corroborated by another bound
artifact in the same 108-file manifest:

1. §B cites `wsl-class-NonRootQualificationTests.log` for **33/33**; that log
   records **31/31**. The 33/33 result is recorded in `wsl-nonroot-b5.log`,
   `wsl-committed-all.log` and `committed/nonroot-integrated.log`.
2. §B cites `windows-solution-full.log` for Windows M1 **1,050 / 269**; that log
   records **1,050 / 267** (1,317 total, versus 1,319 in the 269-skip run). The
   bound 1,050 / 269 result is recorded in `postcommit-solution-tests.log` and
   `windows-m1-full-3.log`; the passed count is identical in both runs.
3. §B lists `wsl-int01-full.log` among the 32/32 evidence; that log records an
   early **0 passed / 32 failed** attempt. The passing runs are
   `wsl-int01-full2.log` and `committed/int01-full.log` (32/32).
4. The producer report §J gives the external evidence manifest SHA-256 as
   `e54b4ef04029f1ffbcd4e034f53fad8dc725e925b4de89ee7eb58be5ecb9c9f4`.
   The actual `artifacts/rc2b-05-b5-impl-01/SHA256SUMS.txt`, the committed
   manifest and producer §A all agree on
   `476809507ab458c59670d439f437d1d9606528eab8f3db8f14bba466638c0dd2`.
   The §J value is stale. Producer evidence is preserved unchanged; the
   discrepancy is recorded here.

The evidence set also preserves earlier negative/intermediate runs that are not
counted as passes: `windows-m1-full.log` (1,049 passed / 1 failed, the
intentionally replaced pre-B5 D2 probe that asserted the C3 violation),
`wsl-regular-phase.log` (3 failures, superseded by the isolated release-path
class run at 3/3), and `wsl-int01-full.log` (the early failed INT-01 attempt).
Their presence is expected for preserved raw evidence and does not weaken the
final, bound run results.

---

## 3. Raw Evidence Integrity (Owner Checks)

The owner re-verified the raw evidence rather than accepting the handoff summary:

- All **108/108** manifest entries are present under
  `artifacts/rc2b-05-b5-impl-01/` with **0 missing and 0 hash mismatches**.
- Manifest file identity: SHA-256
  `476809507ab458c59670d439f437d1d9606528eab8f3db8f14bba466638c0dd2`.
- `native-summary.json` binds `source_commit` `0bf045d0…` and `source_tree`
  `80d138f8…`; it reports 32 requested / 32 passed and the exact 20 Confirmed /
  12 Unknown case lists; `missing-domain-ack` and `missing-staging-ack` remain
  Unknown.
- `committed/cleanup-tally.log` reports `cases=33 confirmed=20 unknown=12
  other=1`, where the extra row is the `preflight` entry; the 32 matrix cases
  are the classified inventory.
- `failing-before.log` records the expected pre-fix failure: expected 0 polls,
  actual 200, one failed test.
- Final bound run logs corroborate the preserved inventory: `postcommit-b5-focused.log`
  61/61, `postcommit-d64.log` 3/3, `wsl-committed-all.log` 61/61 + 3/3 + 33/33,
  `committed/int01-full.log` 32/32, `postcommit-solution-tests.log` Windows M1
  1,050 / 0 failed / 269 skipped, `postcommit-eng-tests.log` 35/35.
- The D6.4 variant remains non-packable and excluded from production; no
  production fixture symbol or friend grant is claimed.

## 4. Owner Decision

**B5 ACCEPTED WITH QUALIFICATIONS.**

The owner accepts the completed B5 implementation within the demonstrated
experimental WSL2/Windows scope, on the basis of the source-bound producer
evidence and the independent B5-Q1 review whose actual verdict supports
acceptance with qualifications.

Accepted capabilities:

1. Sealed and corroborated production monotonic-clock composition.
2. The immutable durable `StopRecord` deadline as the authoritative recovery
   bound.
3. No deadline renewal through restart, repeat reconciliation or partial
   cleanup.
4. Policy A's identity-checked kill and one immediate population observation at
   expiry, including exact-expiry and already-expired cases.
5. Conservative refusal of invalid, incompatible or unavailable clock evidence
   before any destructive action.
6. Separate no-`StopRecord` behavior without fabricated deadlines or restored
   RELEASE authority.
7. Typed native kill and population outcomes, preserving `Failed`, `Refused`,
   `Unknown` and `Confirmed` distinctions.
8. Correct component and aggregate reconciliation precedence, including
   persistence-only uncertainty that is neither operation failure nor confirmed
   cleanup.
9. Independent journal-finalization evidence and conservative persistence
   uncertainty.
10. Preservation of C1, C2, C4, R1-F03 Option A, INT-01 and B4 guarantees.

---

## 5. Qualification Limits Retained

This acceptance does **not** establish and explicitly retains:

- Compatible monotonic basis and boot context are required; cross-platform clock
  equivalence is **not** established.
- Native qualification was performed under **WSL2 Ubuntu**, not a separate
  native Linux host.
- The INT-01 profile was reconstructed from preserved assets and rebuilt B5
  closures; it is **not** a byte-identical historical replay.
- Pre-existing native class skips remain unqualified (root-probe
  `IntegratedBarrierTests` / `DomainAuthorityTests`, absent B2 install closure in
  `B3RestrictedLifecycleTests`, and the explicit INT-01 primitive profile in
  `DirectAuthenticationTests`). Skips are not passes.
- B4Q1-F02/F03/F04, Q1R-08 and R1-F02 retain their existing dispositions.
- No production restricted-execution activation, installation attestation,
  package release, Hufu integration, B6 or B7 work is approved.
- No binary reproducibility or production-binary identity is claimed for the
  D6.4 test variant.

The INT-01 cleanup classification remains **20 Confirmed / 12 Unknown**, with
`missing-domain-ack` and `missing-staging-ack` remaining Unknown.

---

## 6. Evidence Record (Producer and Independent Reviewer Separately)

### 6.1 Producer evidence (inspected, not independently reproduced)

| Result | Value | Binding |
|---|---|---|
| B5 deterministic suite | 61/61 | `postcommit-b5-focused.log` |
| D6.4 R1-T13–T15 variant | 3/3 | `postcommit-d64.log` |
| Native non-root qualification (WSL2) | 33/33, including two real `StopRecord` deadline cases | `wsl-nonroot-b5.log`, `wsl-committed-all.log`, `committed/nonroot-integrated.log` |
| Full INT-01 matrix | 32/32 | `wsl-int01-full2.log`, `committed/int01-full.log` |
| Windows M1 assembly | 1,050 passed / 0 failed / 269 skipped | `postcommit-solution-tests.log` |
| B5 evidence manifest | 108 files, verified identity | manifest file and `SHA256SUMS.txt` |
| Failing-before | 200 fresh polls → 0; `QuarantinedUnknown` | `failing-before.log` |
| Cleanup interpretation | 20 Confirmed / 12 Unknown | `native-summary.json`, `committed/cleanup-tally.log` |

### 6.2 Independent reviewer evidence

| Item | Independent status |
|---|---|
| Source/tree/ancestry bindings | Independently verified |
| 108-entry evidence manifest | Independently recomputed; all entries matched |
| Native test executions | **Not executed**; inspection-only (B5-L07) |
| Deterministic test executions | No reviewer rerun claimed; inventory verified from committed evidence |
| Reviewer findings | B5-F01–F07 qualification-level; no material contract violation |
| Reviewer limitations | B5-L01–L07 disclosed |

No missing or skipped execution has been converted into success in this
decision.

---

## 7. Preserved Dispositions

- **INT-01:** remains ACCEPTED WITH QUALIFICATIONS; cleanup remains 20
  Confirmed / 12 Unknown.
- **B4:** remains ACCEPTED WITH QUALIFICATIONS; B4Q1-F01 erratum and the
  corrected 20/12 tally remain in force.
- **B4Q1-F02** (filesystem identity check/remove race), **B4Q1-F03** (drain-thread
  fault handling), **B4Q1-F04** (experimental store-failure hook), **Q1R-08**
  (legacy D5_O03 flake) and **R1-F02** (global native stop/publication
  enforcement) retain their existing dispositions and are not closed by B5.
- **C1, C2, C4 and R1-F03 Option A** remain the accepted contracts; C3 is now
  accepted as implemented and qualified within the limits above.
- Historical producer evidence, the independent review, raw artifacts and all
  earlier owner decisions are preserved byte-for-byte.

---

## 8. Explicit Non-Authorizations

This decision does not authorize:

- Production restricted-execution activation or any public API change.
- Installation attestation, deployment attestation, packaging or publishing.
- B6 or B7 work.
- Hufu integration, VM/HCS work or sibling-repository changes.
- Native Linux-distribution or macOS qualification; cross-platform clock
  equivalence.
- Any repair or closure of B4Q1-F02–F04, Q1R-08 or R1-F02.

`RestrictedExecutionRuntime.Create()` remains production-unavailable.

---

## 9. Documentation Delivery and Preservation Checks

Created: `docs/decisions/rc2b-05-b5-owner-acceptance.md` (this document).

Updated current pointers only:

- `docs/PROJECT-STATUS.md`
- `docs/handoff.md`
- `docs/work-queue.md`

The owner-decision commit changes exactly these four documentation paths, is a
normal child of review commit `d5852f46`, and is committed locally on canonical
`main`. **Nothing was pushed.** At this decision, `origin/main` remains at the
authorized baseline `e7818b5a`; the B5 implementation, producer evidence,
review and owner decision exist only in the local canonical checkout.

No historical decision, review, evidence document, manifest or raw artifact was
modified; the 74 pre-existing worktrees are unchanged; no branch or worktree was
created.

---

## 10. Final Handoff

- **Independent review commit:** `d5852f46bb3db08b8f37386851bf22bd7a99b48f`
  (verdict **SUPPORTED WITH QUALIFICATIONS**).
- **Owner-decision commit:** the normal documentation-only child of
  `d5852f46` on canonical `main`, changing exactly the four authorized
  documentation paths; its exact SHA is reported in the commission handoff.
- **Owner verdict:** **B5 ACCEPTED WITH QUALIFICATIONS** within the demonstrated
  Windows/WSL2 experimental scope.
- **Qualification boundary:** same-basis compatible-boot monotonic clock only;
  WSL2 is not a native Linux host; INT-01 profile reconstructed, not replayed;
  pre-existing native skips unqualified; carried findings unchanged; no
  production, package, B6/B7 or Hufu authorization.
- **Smallest recommended next milestone:** B6 installed-artifact/deployment
  attestation under a separate bounded commission, with B5-L01 (independent
  native Linux-host qualification) tracked as the highest-priority B5
  qualification follow-up. Neither is authorized or started by this decision.

**Stop after this owner decision.**
