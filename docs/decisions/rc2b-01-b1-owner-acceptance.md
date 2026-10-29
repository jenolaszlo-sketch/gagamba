# RC2B-01 B1 owner acceptance and R1-F03 decision

Commission: GAGAMBA-RC2B-01-B1-OWNER-01 (owner acceptance, R1-F03 decision, next milestone).
Date recorded: 2026-10-29. This is a documentation-only acceptance and decision record.
It changes no implementation, test, package, release, native, or qualification state.

## 1. Disposition

The owner makes two explicit decisions:

1. **RC2B-01 B1 is ACCEPTED WITH QUALIFICATIONS**, limited to its implemented public
   control contract, safe-refusal behavior and deterministic local semantics.
2. **R1-F03 Option A is APPROVED** as the required future pre-retention
   correlation-mismatch behavior, as a semantic owner contract only — not as implemented
   behavior.

No further B1 implementation remediation is required for this accepted scope. This
acceptance does **not** authorize live restricted execution, native work, B2/B3
implementation, Hufu resumption, packaging or any subsequent milestone.

## 2. Binding identities (verified)

| Identity | Value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, directly on `main` |
| Expected and verified starting `main` | `beb3fe4ee53d1c2d9a6cf97af83ca390d6137fef` (= `origin/main`) |
| Accepted RC2A-04 baseline | `c4a326f8b9063e7a21b285fc0b46cbf6f77214e9` |
| B1 implementation commit | `764045fed4a01c4c1160865bd9da53fa3af2b954` (parent `c4a326f8`) |
| B1 implementation root tree | `646484a2ca5017d2439d7460dda36fc1e1d2c23f` |
| B1 delivery commit | `bdd124db5ed6638746d4810b293cf497e61f86e0` (parent `764045f`, tree `60bf5cfbb863d43e2002a8a1af760eb6218c88a3`) |
| Independent B1 review commit | `beb3fe4ee53d1c2d9a6cf97af83ca390d6137fef` (parent `bdd124d`) |
| Review verdict | **SUPPORTED WITH QUALIFICATIONS** |
| Review document | [docs/reviews/rc2b-01-b1-independent-review.md](../reviews/rc2b-01-b1-independent-review.md) (blob `c90010463f7187c283976d8a6d9d668f6ce46c7a`) |
| Producer handoff | [docs/evidence/rc2b-01-b1.md](../evidence/rc2b-01-b1.md) (blob `609cab5f62df8d54e4d27e539091f08a59584153`) |
| CI | https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40793941938 |

Ancestry is linear and verified: `c4a326f` → `764045f` (tree `646484a`) → `bdd124d`
→ `beb3fe4`; every listed commit is an ancestor of the verified starting `main`. No
history was reset, rebased, amended, cherry-picked, merged or force-pushed. No branch or
worktree was created for this record. Existing untracked files were preserved.

## 3. Accepted scope

The owner accepts the RC2B-01 B1 restricted-execution facade **WITH QUALIFICATIONS** at
the exact implementation, delivery and review identities in §2, covering:

- The separate restricted-execution public facade (`IRestrictedExecutionProvider` in
  `Gagamba.Execution.Restricted.Control`), distinct from any ordinary provider.
- Public capability-construction restrictions (no public operation/prepared
  constructors, clone, factory, setters or JSON reconstruction).
- Safe refusal of unavailable installations and unsupported profiles before any
  domain, staging, owner, source-handle or native allocation.
- Bounded profile and preparation inputs.
- Typed results that preserve uncertainty and original evidence.
- Local stop-versus-preparation-publication ordering.
- Cancellation and lost-acknowledgement behavior.
- Retained C4 `Duplicate` semantics and independent binding status.
- Original deadline preservation.
- Disposal behavior without fabricated cleanup success.
- The 46 new deterministic B1 tests.
- The independent review's **SUPPORTED WITH QUALIFICATIONS** verdict.

The producer reported **630 distinct locally passing tests** and a warning-free Release
build; CI completed successfully with **10 jobs passed and one conditional launch job
skipped**. The independent reviewer separately reproduced the focused B1 tests (45 control
+ 1 runtime) and verified contract behavior. These results support B1's **contract-only**
scope.

Acceptance is **not** expanded to any unimplemented runtime, provider or native
capability.

## 4. R1-F03: Option A approved as a semantic contract

The owner explicitly approves:

> **Option A:** A mismatching pre-retention approval correlation refuses only that
> request, without consuming the operation or replacing its previously admitted
> correlation.

The following semantics are mandatory for the later release-admission implementation.

### 4.1 Before retention

When the operation has an established approval correlation and a caller presents a
different correlation:

- Refuse the mismatching request.
- Report a separate binding-mismatch diagnostic.
- Do not retain a new release decision because of that mismatch.
- Do not replace the original correlation.
- Do not consume an otherwise unused release capability merely because of the mismatch.
- Do not extend any original deadline.
- Do not generate a physical RELEASE attempt.

A later request using the original matching correlation may proceed only if all original
admission, capability, deadline, liveness and C1–C4 gates still permit it. This is
permission to **evaluate** the later request, **not** permission to replay an ambiguous
execution.

### 4.2 After retention

If an execution decision already exists:

- Preserve the original decision and correlation.
- Preserve C4 `Duplicate` semantics.
- Report correlation mismatch separately.
- Never overwrite the retained decision.
- Never create another physical RELEASE attempt.
- Never treat an ambiguous acknowledgement as permission to retry.

### 4.3 Concurrency

Concurrent matching and mismatching requests retain the original C1–C4 first-retainer and
single-attempt rules. No caller serialization, waiting for the winning caller, automatic
replay or new durable journal field is introduced merely to implement correlation
admission.

### 4.4 Status distinction

- **R1-F03 semantic owner decision: APPROVED, Option A.**
- **R1-F03 live implementation and native qualification: NOT COMPLETED.**

The B1 implementation deliberately leaves pre-retention correlation admission unwired and
returns `LiveReleaseNotImplemented`; that behavior is not modified by this commission. The
owner decision removes the semantic ambiguity; it does **not** establish implementation
correctness or operational authority.

## 5. R1-F02 status

- **Local contract behavior: IMPLEMENTED AND INDEPENDENTLY REVIEWED.** The in-memory stop
  gate prevents publication of a releasable prepared capability when stop wins before
  minting. Locally queued stop remains distinct from owner-runtime stop acceptance.
- **Native/runtime enforcement: OPEN.** Outstanding obligations include actual C2
  capability consumption; stop persistence and uncertainty handling; stop during durable
  release intent; no RELEASE write when stop wins the accepted ordering; process and
  controller loss; owner and recovery-scope survival; native release-gate enforcement; and
  the relevant C1–C4 native and recovery qualification.

R1-F02 is **not** globally CLOSED.

## 6. RC2B-00 review findings reconciliation

The independent B1 review discusses F01–F03 from an earlier ordinary-provider/RC2A review
(hostile-workload boundary, Windows `EscapeResistant` wording, preparation/launch race
past disposal). These are **different** from the separate RC2B-00 planning-review findings
below and are **not** rewritten or conflated by this record. The accepted B1 independent
review is preserved unchanged.

The RC2B-00 planning review was performed against:

- Planning commit `cda4691156a7477b29f641e4ba958ab718da6086` (branch
  `planning/gagamba-rc2b-scope-01`).
- Local-only independent review `b1ea5d3a84fcc11deac0e14f94c3f67b90c91e0b` (branch
  `review/gagamba-rc2b-scope-q1`), report path
  `docs/reviews/rc2b-scope-01-independent-review.md`, verdict **REMEDIATION REQUIRED**.

Both commits exist as local Git objects in the canonical checkout but are **not reachable
from `origin/main`** (verified with `git merge-base --is-ancestor`); the planning packet
directory and the review document are absent from `main`. They are recorded here by identity
only and are not restored, merged or accepted.

- **RC2B-00-Q1-F01 (Hufu prerequisite fidelity, MAJOR):** the planning review reports that
  Hufu H1 requires OD-01 and the pure binding portions of OD-02/03/04 accepted before D4a,
  and OD-05 experimental-consumer intent before the applicable Hufu-facing facade contract
  acceptance. These gates remain unresolved unless independently proven satisfied. **B1
  acceptance is limited to Gagamba's safe-refusal contract.** It is not Hufu-facing contract
  acceptance, D4a eligibility, installed-provider qualification, or authorization to resume
  Hufu integration. The Hufu H1 gate list is **not** claimed satisfied or owner-confirmed by
  this B1 decision. B1 is also distinct from the GAG-API-style facade contract acceptance
  named in H1's stage register, so this narrowly scoped Gagamba acceptance does not contradict
  the H1 wording; no Hufu-owner approval is fabricated.
- **RC2B-00-Q1-F02 (deadline inversion, MINOR):** recorded that the B1 contract refuses both
  `setup == overall` and `setup > overall`. This does not establish native deadline
  enforcement.
- **RC2B-00-Q1-F03 (stop-wins transition, MINOR):** recorded that the required transition is
  that stop wins after local latch and before physical release, so no RELEASE write occurs and
  release authority remains consumed. B1 provides local inhibition, not evidence of the
  corresponding native release-gate behavior.
- **Other findings (Q1-F04/F05/F06 and the HUFU-R / AE4-B01 rows):** the earlier planning
  review's remaining qualifications are preserved, including the AE4-B01 recorded-stop
  closure-bullet evidence requirements and the HUFU-R reassessment prerequisite. The old
  planning packet is **not** merged or accepted as part of this commission.

## 7. Explicit exclusions

Acceptance does not authorize or establish: live restricted execution; successful production
preparation; actual execution release; native confinement; owner-runtime C1–C4
qualification; independent controller-loss recovery; native release/stop race qualification;
installed provider descriptors; native binary or deployment manifests; Hufu D4a or D4b
eligibility; Hufu Sandbox integration; public NuGet publication; or any new release tag.

`UnavailableRestrictedExecutionProvider` remains the default production behavior. No
production code or security gate is changed.

## 8. Next engineering milestone — RC2B-02 (proposed, NOT STARTED)

The next implementation candidate is identified as:

**RC2B-02: B2 recovery bootstrap and controller/owner lifetime infrastructure, followed by
B3 real owner preparation and release composition.**

B2 is recorded as the **next proposed implementation slice**, not started here. Its intended
engineering objective is to establish a genuine recovery/owner process topology that can
survive controller loss and retain the necessary stop/control obligations. The eventual B3
integration must use that real topology and the approved R1-F03 Option A semantics.

### 8.1 B2 prerequisites and constraints

A future B2 commission must explicitly bind:

- Accepted RC-1 topology and C1–C4 contracts.
- Real bootstrap and recovery process design.
- Parent, controller, scope and owner lifetime relationships.
- Private authenticated communication.
- Process-death and lifeline-loss behavior.
- Stop obligations independent of blocked observers or controller calls.
- Actual OS capability and native prerequisites.
- Host/platform qualification boundaries.
- No privileged or test-only fallback.

The existing **in-process `M1RecoveryScope` must not be described as an already-qualified,
independently surviving recovery process.** Fake owner readiness, synthetic deployment
identities and historical fixture evidence must not be used as production qualification. B3
remains separately gated by real owner/deadline/release semantics and the relevant native
qualifications.

### 8.2 Authorization boundary

Recording B2 as next does **not** authorize implementation in this commission. B2, B3, Hufu
work and native execution tests must not start now. The owner will commission the next
implementation separately.

## 9. Protected scope

This record changes documentation only. No modifications were made to `src/`, `native/`,
`tests/`, `.github/`, existing independent review reports, previous owner decisions,
historical evidence, package manifests, or Hufu repositories. Protected tree identities at
the verified starting `main` are unchanged by this commission: `native`
`85631d2f04512376621d3d09f51d67fd90f45335`; `src` and `tests` retain the B1 implementation
trees `ab1044288340fc5e68681b63630ada0441308bc5` and
`7afdd22bafdf76899d6f647dec21b1f6afef97ec`. No build, native workload, package publication,
release tag or additional independent review was performed.

## 10. Next owner action

The B1 accepted scope is closed. RC2B-02 (B2) is proposed and awaits separate owner
commissioning; no subsequent stage starts automatically. Hufu OD-01–OD-05, AE4-B01,
HUFU-R, native C1–C4, deployment and recovery gates retain their independent, open or
blocked dispositions.
