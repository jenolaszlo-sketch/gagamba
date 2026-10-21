# WP05 D6.2 Focused Independent Review Handoff

Status: **PROPOSED - NOT APPROVED**
Design status: **DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW**

To: independent GLM design reviewer.
From: D6.2 design author.
Purpose: one narrowly focused review of the four D6.1-R1 through R4
corrections. This is not a repeat of the D6.0 or D6.1 full review, not a
production review, not a qualification run and not a contract approval.

## 1. Binding

- R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D6.1 producer final HEAD: `750914d93edaf5490b42bf0787f20bb0c5089767`.
- D6.1 independent review Q3: `77f559a7dfc1702b33812d5c3d5c42ac925f0343`.
- D6.1 preservation base: `d39dab60152a6f83724760b3122f61d6cf928be3`.
- D6.2 branch: `design/wp05-d6-2-contract-precision-amendments`.
- D6.2 review target: the branch HEAD at review time (the D6.2 documentation
  commit(s) after `d39dab60152a6f83724760b3122f61d6cf928be3`). The final
  completion report records the full commit SHAs.
- The D6.1 producer, independent review and preservation histories remain
  unchanged. The D6.2 branch does not merge or cherry-pick the review branch.

## 2. Exact revised sections

- C1: `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md`, **D6.2 revision -
  R3 retention collision and R4 fixture provenance**.
- C3: `docs/decisions/wp05-d6-0-c3-stop-deadline.md`, **D6.2 revision - R1
  structured outcomes and R2 clock provenance**.
- C2: `docs/decisions/wp05-d6-0-c2-stop-capability.md`, **D6.2 cross-reference
  - R3 stop ordering** (semantics unchanged).
- C4: `docs/decisions/wp05-d6-0-c4-duplicate-semantics.md`, **D6.2 precision
  note - R3 and retry futility** (semantics unchanged).
- Evidence/gate matrix: `docs/decisions/gagamba-linux-m1-wp05-d6-0-evidence-and-gate-matrix.md`,
  **D6.2 amendment reconciliation additions**.
- Remediation plan: `docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md`,
  **D6.2 revision - precision prerequisites**.
- Reconciliation register:
  `docs/decisions/gagamba-linux-m1-wp05-d6-2-precision-amendment-reconciliation.md`.
- Human addendum:
  `docs/decisions/gagamba-linux-m1-wp05-d6-2-human-approval-addendum.md`
  (not a primary review question).

## 3. Exactly four primary review questions

### GLM-R1

Does the C3 D6.2-R1 design identify an implementable typed mechanism that
distinguishes backend/kill failure, attempted-but-unconfirmed emptiness,
population-observation uncertainty, confirmed empty cleanup and genuine
operation failure, including the explicit `JournalFinalization` aggregation
row, without error-string classification or blanket Failed-to-Unknown mapping?

Review evidence:
`wp05-d6-0-c3-stop-deadline.md` §D6.2-R1; R12
`Lifecycle/DomainLease.cs:11-18,98-143`,
`Lifecycle/RestrictedDomain.cs:15-21`,
`Recovery/Reconciler.cs:217-287,423-471`.

### GLM-R2

Does the C3 D6.2-R2 clock contract structurally bind epoch identity and
conversion to a trusted `Stopwatch.GetTimestamp()` production implementation
with the `OwnerClock` conversion and `Stopwatch.Frequency`, reject incompatible
epoch/frequency before subtraction, and include behavior-based compatibility
and negative tests without relying on an enum label alone?

Review evidence:
`wp05-d6-0-c3-stop-deadline.md` §D6.2-R2; R12
`Lifecycle/OwnerPreparationTransaction.cs:21-39`,
`Launch/BubblewrapAdapter.cs:510-521`,
`Recovery/OwnershipValidation.cs:145-175`,
`Recovery/Reconciler.cs:75-97`.

### GLM-R3

Does the C1 D6.2-R3 ambiguous-retention collision contract precisely define
ambiguity-first, stop-first and already-retained outcomes, the returned
`ReleaseDecisionKind` and stop-won classification, capability consumption,
durable-journal separation, no second FD4/no resend and lock safety, without
changing the existing first-retainer rules?

Review evidence:
`wp05-d6-0-c1-ambiguous-intent.md` §D6.2-R3; R12
`Execution/ReleaseArbiter.cs:48-62,81-131,148-183,215-223,226-296`,
`Preparation/PreparedTransaction.cs:60-69`,
`Recovery/OwnershipJournal.cs:424-490`; C2/C4 D6.2 cross-references.

### GLM-R4

Are the test-only fixture sources correctly bound to their actual branch,
commit, file, blob, digest and evidence manifest, and correctly distinguished
from R12 production, with no false ancestry or source attribution?

Review evidence:
`wp05-d6-0-c1-ambiguous-intent.md` §D6.2-R4; fixture branch
`implementation/wp05-release-qualification-fixtures`, test commit
`dc4c372b180abef1b27038d94f49af90226fa548`, evidence tip
`338a98af269e6b5839f3ccc2e0881f016153b01e`; R12
`38f6981b2a60daab2df37c92913a6760c1bef131`.

## 4. What not to re-review

Do not repeat the entire D6.0/D6.1 review or re-qualify D1-D5. The D6.1
verdicts for C2 and C4 remain independent support for human decisions; D6.2
does not reopen their substance. C3 expired-deadline Policy A/B remains a
human decision and is not selected by D6.2. The reviewer should only
re-evaluate R1-R4 and any directly affected cross-contract claim.

## 5. Unchanged boundaries

- No production, test, native, engineering or fixture change.
- No new test run, qualification, target execution or gate acceptance.
- No contract approved; C1-C4 remain `PROPOSED - NOT APPROVED`.
- C3 remains `CONTRACT CHOICE REQUIRED - HUMAN DECISION PENDING`;
  `C3 ZERO-WAIT POLICY - PENDING HUMAN`.
- H1-H5, WP08/WP10 and full G3 remain deferred.
- Release remains not qualified.
