# WP05 D6.3 — Terra Independent C3 Outcome Review

**Reviewer:** Terra
**Scope:** D6.2-R1 only; independent review of Luna's documentation-only D6.3 correction.
**Disposition:** **WP05 D6.3 TERRA INDEPENDENT REVIEW COMPLETE - OPTION B: D6.2-R1 PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; HUMAN C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

## 1. Exact Git binding and scope verification

| Item | Verified value |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` |
| Frozen production binding | R12 commit `38f6981b2a60daab2df37c92913a6760c1bef131`; tree `6854675d6b37a49d5302d6104103bdbfbc268040` |
| GLM D6.2 review | `3bce34b6b449a863f784949c0d7be5b0b56d4458` |
| D6.2 preservation / producer parent | `31132513a983bb37cfd609f56541f6ab8d44af84` |
| Luna producer branch and commit | `design/wp05-d6-3-c3-outcome-correction`; `4209581a03562b434badd7a5f9927663559911be` |
| Immediate producer parent | `31132513a983bb37cfd609f56541f6ab8d44af84` (verified) |
| Terra review branch and worktree | `review/wp05-d6-3-terra-independent-c3-outcome`; `C:\Users\Laszlos\source\repos\Gagamba-review-d63-terra` |

The producer commit is locally present and its local producer branch exists; the branch was not assumed absent merely because it is unpublished. The producer diff contains exactly these files:

1. `docs/decisions/wp05-d6-0-c3-stop-deadline.md` (modified)
2. `docs/decisions/gagamba-linux-m1-wp05-d6-3-c3-outcome-correction.md` (added)

`git diff --check` is clean. The diff contains Markdown documentation only: no production, test, native, journal-schema, or durable-record change. This review does not change either Luna document.

## 2. Original GLM finding and reviewed correction

At GLM review `3bce34b...`, section 5.4 (lines 96-111), D6.2-R1 identified an internal contradiction: the unconditional old mapping

| Condition | DomainStop | DomainEmpty |
|---|---|---|
| `Kill=NotAttempted` | `Unknown` | `Unknown` |

discarded trusted fallback evidence of a known-empty domain, while the retained fallback and the `Kill=Failed` row allowed that same independent population evidence to establish emptiness. GLM's preferred narrow correction was conditional `DomainEmpty` based on the actual trusted observation, with no implication that a kill succeeded.

Luna's corrected mapping at `docs/decisions/wp05-d6-0-c3-stop-deadline.md:350-371`, repeated consistently in the D6.3 handoff at `gagamba-linux-m1-wp05-d6-3-c3-outcome-correction.md:24-32`, is:

| Kill dispatch and fallback observation | DomainStop | DomainEmpty | Result |
|---|---|---|---|
| `NotAttempted`, trusted Empty | `Unknown` | `Confirmed` | Correct: empty evidence only; no successful kill or termination inference |
| `NotAttempted`, Populated | `Unknown` | `Unknown` | Correct: no empty inference and no new kill attempt |
| `NotAttempted`, unavailable or inconclusive | `Unknown` | `Unknown` | Correct: no fabricated empty evidence |

This removes the D6.2-R1 evidence-loss contradiction. `Kill=Failed` remains `DomainStop=Failed` with `DomainEmpty=Confirmed` only on a trusted empty observation; `Kill=Unknown` remains excluded from this fallback resolution; and `Kill=Accepted, Population=Empty` remains fully confirmed. The proposed typed `StopOutcome` is explicitly design-only at `wp05-d6-0-c3-stop-deadline.md:289-318`; it is not represented as R12 production behavior.

## 3. Frozen R12 source verification

R12 supports the factual premise for the correction, while not itself implementing the proposed typed model.

- `src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs:102-104` implements `ObservePopulation` separately from dispatch and returns `ReadPopulation` through the retained lease.
- `DomainLease.cs:108-143` implements the bounded stop as `WriteKill` first (`:116-120`), then population reads (`:125-142`). It has no `NotAttempted` dispatch outcome: a disposed lease or write failure returns the old collapsed `StopOutcome.Failure` (`:111-120`). Therefore the new `NotAttempted` rows are defensive proposed-contract cases, not a claim of a currently reachable R12 path.
- `Lifecycle/RestrictedDomain.cs:15-21` makes population an independent typed fact: known populated, known empty, or unknown.
- `Recovery/Reconciler.cs:263-275` performs the retained fallback. A known Empty result sets the existing `CleanupComponents.DomainEmpty` bit, records `Confirmed`, and invokes the existing `TryPersist` path. This proves that fallback emptiness is distinct evidence and can persist an existing progress bit; it proves neither successful kill dispatch nor a new authority.
- `Reconciler.cs:277-285` shows the R12 behavior for known-populated and unknown fallback observations. D6.3 accurately states a proposed more-conservative outcome mapping and does not assert that this is already the R12 typed behavior.
- `Reconciler.cs:423-471` confirms why D6.2's proposed `JournalFinalization` rule is still design work: R12 presently treats `finalizationFailed` and final persistence failure as `Failed`. D6.3 introduces no journal field, record schema, StopRecord, allowance, authority, or persistence mechanism.

## 4. Adversarial case results

| Case | Result | Basis |
|---|---|---|
| A — `NotAttempted`, trusted Empty | Mapping passes: `DomainStop=Unknown`, `DomainEmpty=Confirmed`, aggregate `QuarantinedUnknown`; no kill success, termination, or `CleanupConfirmed` is inferred. | D6.3 mapping `:350-365`; aggregate table `:421-448` |
| B — `NotAttempted`, Populated | Mapping passes: both required unknown results, no new stop/wait interval. | `:350-365` |
| C — `NotAttempted`, unavailable/inconclusive | Mapping passes: both results unknown, no empty evidence, retry, or deadline reset. | `:350-365`, `:437-448` |
| D — `Failed`, trusted Empty | Passes: failure remains authoritative, `DomainEmpty` retains positive evidence, and aggregate remains `Failed`. | `:366`, `:401-415`, `:425-433` |
| E — `Accepted`, Empty | Passes: existing confirmed-cleanup row is unchanged. | `:368`, `:433` |
| F — unknown dispatch/observation | Passes: `Kill=Unknown` remains `Unknown/Unknown`; the fallback is not expanded to it. | `:367` |

## 5. Aggregate, persistence, and authority consistency

The D6.3 diff leaves the D6.2 precedence unchanged: any required component `Failed` yields `Failed`; otherwise an unknown required component or `JournalFinalization=Unknown` yields `QuarantinedUnknown`; only all-confirmed components and finalization yield `CleanupConfirmed` (`wp05-d6-0-c3-stop-deadline.md:399-448`). Thus confirmed `DomainEmpty` cannot override an unknown `DomainStop`, cannot erase a failed kill, and cannot turn persistence uncertainty into cleanup confirmation.

The correction uses only the existing R12 `DomainEmpty` bit and existing persistence path identified above. It introduces no observation or termination authorization, no new durable record/journal schema, no fabricated StopRecord, and no new original stop-deadline allowance. `C3 ZERO-WAIT POLICY - PENDING HUMAN` remains unchanged; neither Policy A nor Policy B is selected.

## 6. R1-T13 through R1-T15 acceptance-plan assessment — required narrow correction

The new rows at `wp05-d6-0-c3-stop-deadline.md:472-474` correctly specify each desired `DomainStop`, `DomainEmpty`, aggregate result, durable-evidence posture, and negative no-successful-dispatch assertion. They are coherent with the mapping and are explicitly documentation-only.

However, they do **not** specify how a test fixture represents `Kill=NotAttempted`, nor how each fallback population result is injected. Their setup cells say only `Existing trusted fallback authority` or `Fallback unavailable/inconclusive`. That is insufficient for these particular defensive proposed-contract cases: the documented R12 bounded stop always calls `WriteKill` before its population read (`DomainLease.cs:108-143`), and so cannot itself produce `NotAttempted`. The generic inventory of `FakeDomainBackend`/`ReadPopulation` at lines 452-456 names available seams but does not bridge that missing dispatch state.

**Smallest required documentation correction:** amend R1-T13 through R1-T15 to name the test-only proposed typed-outcome fixture/seam that supplies `Kill=NotAttempted` without calling `WriteKill`, and state that the existing fake backend's `ReadPopulation` is injected respectively as Empty, Populated, and Unknown/inconclusive. Retain their present component, aggregate, durable-evidence, and no-successful-dispatch assertions. This is a test-plan specification clarification only; it neither selects a policy nor authorizes an implementation or a new production surface.

## 7. Remaining issue, approval readiness, and scope

The fixture/injection omission above is the single remaining issue. It is a concrete, narrow D6.2-R1 acceptance-plan correction; no structural C3 contradiction was found. After it is documented and independently preserved, the mapping correction is ready to support individual human decisions.

This review makes no production changes, no test changes, and executes no tests. It does not qualify a release, accept G3, change H1-H5, or reopen D6.1-R2/R3/R4, C1, C2, or C4. WP08 and WP10 remain deferred. C1-C4 contracts are not approved, C3 policy is not selected, and no remediation is authorized.

## 8. Final disposition and next action

**WP05 D6.3 TERRA INDEPENDENT REVIEW COMPLETE - OPTION B: D6.2-R1 PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; HUMAN C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

Next action: Luna (or the designated documentation producer) should make only the fixture/injection clarification in section 6, followed by a new independent review and preservation. Individual human C1-C4 decisions may follow only after that correction; DeepSeek implementation and later independent qualification remain separately authorized work.
