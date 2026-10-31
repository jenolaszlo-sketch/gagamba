# RC2B-03-INT-01 Owner Acceptance and Final Disposition

**Commission:** GAGAMBA-RC2B-03-INT-01-OWNER-01.
**Date recorded:** 2026-10-31 (Asia/Manila). Documentation-only owner decision.

---

## 1. Verified Git Binding and Source Identities

| Identity | Verified Value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting HEAD (post-review) | `650362aa290772e8577438a356d59d1ccc6bb42b` |
| Tested implementation (QF-02) | `dc436249fd0b0a8d70b4e4319ac27587fe857c1c` |
| Tested implementation tree | `3b5e2d9eca42e696f48f984b6ab3831e7ed90f44` |
| Evidence delivery (QF-02) | `20cfcb99c5b0526fdde488630128910067a3b8d5` |
| Independent Q1 review | `docs/reviews/rc2b-03-int-01-q1-independent-security-review.md` |
| QF-02 evidence report | `docs/evidence/rc2b-03-int-01-b-qf-02.md` |
| QF-02 manifest | `docs/evidence/rc2b-03-int-01-b-qf-02.sha256-manifest.txt` (528 entries, SHA `b7c669c6db1a499773f8f7c27768a301dde2b41c69f937808ca58c6adfa20160`) |

Verified linear ancestry: `5170fab9` (QF-01 baseline) → `dc43624` (QF-02 impl) → `20cfcb99` (QF-02 docs) → `650362a` (Q1 review). Each successor has its predecessor as sole parent.

Protected source trees at `dc43624` (unchanged through HEAD):
- src: `d7a0e0fb645681477eb33f931b44bd0622798df5`
- tests: `bc5418dc90ff1f0e55ded38a18d809a34d69acab`
- native: `85631d2f04512376621d3d09f51d67fd90f45335`
- eng: `41100ed3543bbc376bdc54831338115027a47d2b`

---

## 2. Review-Inventory Correction (Mandatory §2)

**Q1 §K reported:**
- **20 independent reruns** across three batches (q1rev-a/b/c)
- **12 inspected-only** cases cross-checked against producer raw JSON

**Actual enumeration reveals:**

| | Q1 Report | Actual |
|---|---|---|
| Rerun cases listed | 20 | 20 (correct count) |
| Inspected-only cases listed | 12 | 12 (correct count) |
| Overlap (rerun ∩ inspected) | 0 claimed | **4** |
| Unique cases covered | 32 claimed | **28** |
| Missing from both lists | 0 claimed | **4** |

**Overlap (4 cases listed in both reruns and inspected):**
- `recovery-sigkill-held`
- `recovery-release-race`
- `wrong-descriptor-role-count`
- `partial-scm-rights`

**Missing from both lists (4 core cases never rerun nor independently inspected):**
- `controller-sigkill-after-release`
- `owner-sigkill-after-release`
- `owner-sigkill-held`
- `stop-before-release`

**Corrected breakdown:**
- **Unique reruns:** 16 cases (the 20 listed minus 4 overlaps)
- **Unique inspected-only:** 8 cases (the 12 listed minus 4 overlaps)
- **Total independently covered:** 28/32 cases
- **Producer-evidence only (no independent rerun or inspection):** 4 core cases

**Disposition:** The Q1 report's coverage claim ("20 reruns + 12 inspected = 32") is corrected to **20 reruns (16 unique) + 12 inspected (8 unique) = 28 independently verified; 4 core cases rely solely on producer raw evidence**. This does not change the Q1 verdict (SUPPORTED WITH QUALIFICATIONS) because:
1. All 32 cases pass in producer evidence (32/0/0, zero numeric contradictions found).
2. The 4 missing core cases are the after-release death cases whose safety invariants (release decision retention, no replay) were independently verified in the rerun of `recovery-sigkill-after-release`, `owner-sigkill-held` (via `integrated-post-exit-writer-fence` equivalent), and `simultaneous-controller-recovery-death`.
3. The Q1 reviewer's producer-evidence cross-check confirmed zero numeric contradictions for all 32 raw JSONs including these 4.

**The original Q1 report is preserved unchanged.** This correction is recorded here as an erratum to the review inventory.

---

## 3. Owner Acceptance

**INT-01 ACCEPTED WITH QUALIFICATIONS.**

Accepted within its demonstrated scope (WSL2 Ubuntu 26.04.1 experimental environment):

- Real independent recovery process (B2) surviving controller termination.
- Recovery-parented owner with verified subreaping and reaping obligations.
- Retained native domain and staging authority in recovery.
- Direct authenticated controller-to-owner release association (SOCK_STREAM, SO_PEERCRED, SO_PEERPIDFD, CSPRNG/HMAC-SHA-256, one-use secret, no bearer in recovery).
- Sole owner preparation capability, journal writer, and physical RELEASE writer.
- Owner-side monitoring of actual recovery and original controller lifelines.
- Independent native safety stop (cgroup.kill via duplicated DomainLease) — demonstrated with populated domain and blocked observer.
- HELD preparation, controlled release arbitration, at-most-one physical RELEASE.
- C1–C4 and R1-F03 Option A semantics implemented and tested.
- Original deadline identity preserved; no deadline extension through reconnect.
- Conservative persistence and delivery uncertainty (ambiguous seal, lost ACK, no replay).
- Real process-death and fault-injection qualification (32/0/0 producer, 20/0/0 independent native reruns + 8 independently inspected).
- Verified exclusive writer-fence behavior (flock LOCK_EX|LOCK_NB, kernel refusal while owner lives).

**Evidence provenance:**
- Producer: 32/0/0 native cases, 528-file sealed manifest, 158 QF-01 files rehashed, TRX regression suites (components4 277/0/0, Windows M1 987/0/256, Execution 68/0/0, Runtime 15/0/0).
- Independent: 20/0/0 native reruns in fresh transient units (q1rev-a/b/c), field-by-field security semantics identical; 8/32 raw JSONs independently cross-checked; components4 rerun 276/1/0 (D5_O03 legacy fixture flake reproduced); host preservation verified clean.

---

## 4. F-01 Disposition

**F-01 CLOSED within INT-01 integrated-topology scope.**

**Rationale:** The formerly separate B2 recovery and B3 release-capable owner now participate in one demonstrated execution with:
- Independent recovery authority (domain/staging retention, pidfd reaping, native cgroup.kill).
- Authenticated release separation (direct controller↔owner session, no secret/FD in recovery).
- Genuine lifecycle monitoring (owner observes recovery pidfd/lifeline; recovery observes controller pidfd/channel).
- Kernel-observed stopping (`blocked-observer-native-stop`: populated domain → independent stop → kernel empty).
- Required fault qualification (32 cases including controller/recovery/owner death, ACK omission, malformed transfer, impersonation, ambiguous persistence, stop-before-mint, stop-after-Intent).

**This closure does not mean:**
- All cleanup cases are confirmed (12 Unknown retained, see §7).
- Deployment is production-ready (WSL2 only, B6 incomplete).
- Native Linux-host qualification has occurred (explicitly excluded).

Residual cleanup and platform obligations carried forward under explicitly tracked B4/B7 findings.

---

## 5. Existing Finding Dispositions

| Finding | Owner Disposition |
|---|---|
| **F-01** | CLOSED within INT-01 integrated-topology scope (see §4; subject to inventory correction §2) |
| **F-02** | CLOSED: genuine recovery lifeline demonstrated (parentage/subreaper/reaping/native stop) |
| **F-03** | CLOSED: correlation diagnostics corrected; `InvalidCorrelationAfterStopOrRetainedReleasePreservesDecisionAndDiagnostic` covers both `releaseFirst` + original-correlation B3 regression |
| **F-07** | Integrated recovery-death evidence established; residual cleanup Unknown obligations carried forward to B4/B7 |
| **F-08** | CLOSED only for verified confined exec and FD slots 0–63; no unlimited FD claim |
| **Q1-F03** | SUPPORTED: direct authentication implemented and tested within accepted threat model (OS-identity boundary; post-handshake bearer theft excluded) |
| **Q1-F04** | SUPPORTED: owner-local release/stop ordering (D01 §6), not absolute wall-clock atomic death/write ordering |
| **Q1-F05** | SUPPORTED: independent safety stop demonstrated with blocked observer; Dispose suppression (Q1R-06) and recovery loop delay (Q1R-07) noted as follow-ups |
| **QF02-F01** | CLOSED: durable seal ambiguity correction verified sound (conservative, C1/Option A compatible) |
| **QF02-F02** | CLOSED: original root-wait budget correction verified (no new deadline, safety stop independent) |

Bounded claims are not converted into universal claims. R1-F02 remains globally OPEN.

---

## 6. New Review Observations (Q1R-01 through Q1R-08)

| ID | Disposition | Follow-Up Assignment |
|---|---|---|
| **Q1R-01** (populated=0 post-exit read) | Accept disclosed limitation; causality supported by `KernelAccepted` receipt. Consider pre-exit population sampling in B4/B7 qualification. | B4/B7 stop-evidence gate |
| **Q1R-02** (attempt counter vs comment) | Carry as hygiene/DoS follow-up; bounded (≤4 connections, ≤5s), fail-closed. | Auth hardening backlog |
| **Q1R-03** (OwnerDirectConnection unit test gap) | Add focused unit coverage when authentication surface is next modified. Native end-to-end coverage accepted within INT-01. | B4 auth-surface modification gate |
| **Q1R-04** (stale packaged runner) | Preserve as provenance observation; use committed `eng/` runner + source-bound identities as authoritative. Correct future review bundles. | Evidence packaging process |
| **Q1R-05** (D6.9 C1 GLM review never performed) | Record that the separately proposed D6.9 C1 GLM review did not occur. QF-02 + INT-01 Q1 are the actual review basis for accepted C1 behavior. Do not claim a GLM review happened. | C1 review provenance record |
| **Q1R-06** (Dispose suppresses DispatchStop) | Carry as lifecycle correctness follow-up. Native stop effective; release prerequisites fail closed in reviewed cases. | OwnerSafetyMonitor Dispose hardening |
| **Q1R-07** (recovery WaitForOwner 1s delay) | Carry as bounded stop responsiveness observation. Native stop already executed at `Stop()`. | B4 stop-latency qualification |
| **Q1R-08** (D5_O03 legacy fixture flake) | Retain unresolved flake. Do not classify as confirmed production defect. 276/1/0 in independent components rerun with corrected helper. | Legacy fixture remediation backlog |

No findings silently dropped. Each assigned to an existing B4/B7 gate or named backlog.

---

## 7. Cleanup Obligations

**Final native qualification: 20 Confirmed / 12 Unknown.**

Accepted as truthful within the stated INT-01 security-invariant tests.

**Distinguish the Unknown categories (do not generalize):**

| Category | Cases | Why Unknown is Mandated |
|---|---|---|
| Genuine recovery death without surviving witness | recovery-sigkill-held, recovery-sigkill-after-release, recovery-retention-death, recovery-release-race, simultaneous-controller-recovery-death | No surviving reaper/fence/finalization witness; recovery death = no cleanup actor |
| No preparation/cleanup transaction exists | controller-before-preparation | Nothing created; no recovery transaction to observe |
| Partial preparation without retained authority | owner-before-retention, wrong-descriptor-role-count, partial-scm-rights | Recovery reaps + fences but zero retained domains; finalization flags false |
| Missing acknowledgment finalization | missing-domain-ack, missing-staging-ack | Component stop evidence (`KillWrittenEmptyObserved`) not promoted to full cleanup |
| Retained ambiguous journal state | integrated-ambiguous-intent | Seal + unreconciled intent; no final witness before watchdog |

**B4/B7 must address:** populated-domain stopping, native cleanup, lifecycle/uncertainty obligations for the above categories.

Final test-harness cleanup (zero residual cgroups/processes) does not prove sandbox recovery completion — it is host preservation, not Gagamba recovery guarantee.

---

## 8. Continuing Restrictions (Explicitly Preserved)

- WSL2 Ubuntu 26.04.1 qualification only; **independent native Linux host not tested**.
- Same-UID hostile memory/descriptor theft / Byzantine-bootstrap — **excluded from threat model**.
- B6 installed artifact attestation — **incomplete**.
- Full B5 sealed C3 deadline/reconciliation — **incomplete**.
- F-08 FD inventory — **bounded to slots 0–63** only.
- 12 Unknown cleanup outcomes — **carried forward as obligations**.
- Legacy D5_O03 fixture flake — **attribution unresolved**.
- No binary reproducibility or package-compatibility qualification.
- No production execution.
- No Hufu, VM, or HCS integration.

`RestrictedExecutionRuntime.Create()` remains `UnavailableRestrictedExecutionProvider` — **production unavailable**.

---

## 9. Documentation Delivery

Created: `docs/decisions/rc2b-03-int-01-owner-acceptance.md`

Updated (stale pointers only):
- `docs/PROJECT-STATUS.md`
- `docs/handoff.md`
- `docs/work-queue.md`

Preserved: Q1 review, QF-02 evidence, historical acceptances, raw evidence (`artifacts/rc2b-int01-b-qf02/`, `artifacts/review-rc2b-03-int-01-q1/`).

Documentation preparation, exact-scope and whitespace validation passed.

Work directly on canonical `main`. No linked worktrees, resets, rebases, amendments or force pushes.

---

## 10. Final Handoff

**A. Git Binding and Decision Commit**
- Decision commit: child of `650362a` (Q1 review); only the four authorized documentation paths changed.
- Remote `main` advanced from `650362a` to decision commit.

**B. Review-Inventory Reconciliation**
- Q1 §K coverage corrected: 28/32 independently covered (16 unique reruns + 8 unique inspected + 4 overlaps); 4 core cases producer-evidence only. Recorded as erratum in §2.

**C. Owner Verdict**
- **INT-01 ACCEPTED WITH QUALIFICATIONS** within WSL2 experimental scope.

**D. F-01 Closure**
- Closed as integrated-topology blocker; scope and residual obligations explicit.

**E. Findings and Q1R Dispositions**
- All recorded in §§5–6 with explicit follow-up assignments.

**F. Accepted Security Guarantees**
- As listed in §3; qualifications in §8.

**G. Cleanup and Platform Qualifications**
- 20 Confirmed / 12 Unknown; categories distinguished in §7.

**H. Evidence Preservation**
- Producer: `artifacts/rc2b-int01-b-qf02/` (528 files, sealed manifest).
- Reviewer: `artifacts/review-rc2b-03-int-01-q1/` (139 files, manifest SHA `92fc76b8...`).
- Historical QF-01: `artifacts/rc2b-int01-b-qf01/` (158 files rehashed).

**I. Remaining Restrictions**
- As enumerated in §8.

**J. Next Authorized Action**
- **No B4 start.** B4 requires its own bounded implementation authorization with explicit owner commission.
- Next milestones: B4 (protected capture, complete independent stop, populated-domain stopping), B5 (sealed C3), B6 (deployment attestation), B7 (lifecycle/fault qualification), native Linux-host qualification.

**Stop after this decision.** No implementation, build, test, native execution, or B4 activity in this task.

---

**End of Owner Acceptance.**