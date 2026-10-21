# WP05 D6.3 — C3 Outcome Correction and Luna Review Handoff

Status: **DESIGN TEXT ADDRESSED — AWAITING INDEPENDENT LUNA REVIEW**
Approval: **PROPOSED — NOT APPROVED**

## Binding

- Repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Branch: `design/wp05-d6-3-c3-outcome-correction`; exact base: D6.2 preservation `31132513a983bb37cfd609f56541f6ab8d44af84`.
- D6.2 producer HEAD: `ae09afedddeb703e6bde226bd793b375896c3064`; GLM review Q3: `3bce34b6b449a863f784949c0d7be5b0b56d4458`.
- Frozen R12: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Bind review to the full producer branch HEAD (`git rev-parse HEAD`) before review.

## D6.2-R1 amendment

The finding is recorded in `docs/reviews/gagamba-linux-m1-wp05-d6-2-focused-independent-review.md` at GLM Q3 commit `3bce34b6b449a863f784949c0d7be5b0b56d4458`, section 5.4. It notes that the old `Kill=NotAttempted` row unconditionally set `DomainEmpty=Unknown`, contradicting the retained `ObservePopulation` fallback, which can confirm emptiness.

Old row:

| Condition | `DomainStop` | `DomainEmpty` |
|---|---|---|
| `Kill=NotAttempted` | `Unknown` | `Unknown` |

Corrected rows:

| Condition | `DomainStop` | `DomainEmpty` |
|---|---|---|
| `Kill=NotAttempted`, trusted fallback observes Empty | `Unknown` | `Confirmed` |
| `Kill=NotAttempted`, fallback observes Populated | `Unknown` | `Unknown` |
| `Kill=NotAttempted`, fallback unavailable or inconclusive | `Unknown` | `Unknown` |

Only the population fact changes for trusted, authorized Empty observation. `DomainStop` remains Unknown; no kill dispatch or termination is inferred. Existing aggregate precedence therefore yields `QuarantinedUnknown` while `DomainStop` is Unknown. The `Kill=Failed` row and failure precedence remain unchanged.

## Source and review scope

Frozen R12 `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs:263-275` calls the retained `liveLease.ObservePopulation()` fallback. Known Empty confirms `DomainEmpty`, records the existing progress bit, and uses the existing persistence path. `src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs:102-104` is the separate population-observation method; `DomainLease.cs:108-143` performs the bounded stop sequence with kill dispatch followed by population polling. `Lifecycle/RestrictedDomain.cs:15-21` represents population as known/empty state. No observation authority is added.

R1-T13 through R1-T15 add documentation expectations for Empty, Populated, and unavailable/inconclusive observations; R1-T1 through T12 remain. No tests changed, run or qualified. Existing `JournalFinalization` uncertainty and aggregate precedence remain in force; no new allowance, journal record, or finalization rule is added.

Review only this correction and R1-T13..T15. Confirm independent population and stop outcomes, conservative handling of non-empty/unavailable observations, unchanged `Kill=Failed` and aggregate rules, no new authority, and no completion claim under unknown durable finalization. C3 expired-deadline Policy A/B remains pending human decision. C1-C4 remain unapproved; no production remediation is authorized; release is not qualified. H1-H5, WP08, WP10 and G3 deferrals remain unchanged.
