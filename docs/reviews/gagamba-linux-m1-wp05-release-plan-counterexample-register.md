# WP05 Release Plan Review - Counterexample Register

Author: GLM (independent adversarial plan reviewer)
Date: 2026-10-19
Binding: same as `gagamba-linux-m1-wp05-release-plan-independent-adversarial-review.md` (A3 review base, R12 product tree). This register contains no runtime counterexamples: all entries are source-path findings derived at the R12 tree (read-only mirror + reviewed plan commit). None has been executed, and execution is not authorized by this review.

Every entry names the verified mitigation(s) that bound its blast radius, so the required-changes document can target the true residual risk rather than the whole topic.

## Register

### CE-01 - Retention asymmetry: outside-gate refusals are not retained; retry semantics change with the failure point

- Path: `Execution/ReleaseArbiter.cs`
- Interleave: A refused release attempt whose refusal is produced outside the gate (prerequisite/identity/deadline checks and, notably, the durable-intent write failure at `ReleaseArbiter.cs:110-114`) returns a fresh `Refused` decision each time and never installs a retained decision; refusals inside the gate (stop-after-intent, expiry, capability, transport-committed sequence) are retained and subsequent calls receive the original decision relabeled `Duplicate` (`ReleaseArbiter.cs:48-55,125-155`). The test `FailedIntentPersistenceForbidsRelease` (`ReleaseArbiterTests.cs:282-292`) relies on the not-retained behavior (capability stays live and a later attempt is allowed); `ValidReleaseWritesExactlyOnceAndPersistsCommit` (`ReleaseArbiterTests.cs:116-119`) relies on the retained behavior. Both behaviors are internally consistent, but the same public API exhibits two different idempotency contracts depending only on which internal check fires.
- Unsafe effect: none. No second FD4 write and no journal fact change result from either branch (journal-level transition guards verified independently: any retained duplicate returns without writing, and a post-refusal retry that finds a durable intent still cannot plan a second intent).
- Why it matters to the plan: R06 (idempotency) currently reads as one contract; it is really two, split by failure mode. Any G3-A05 evidence that asserts "duplicate/identical decision" across failure modes would be unprovable as written.
- Mitigations verified: journal transition guards; one-shot `HeldExecution`; gate serialization.
- Resolution hook: C4 (define and test the two contracts explicitly); ties to Q07.

### CE-02 - Wedge without a stop signal: ambiguous intent failure yields a false `NotCommitted`, un-persistable evidence, and no stop request

- Path: `Execution/ReleaseArbiter.cs:110-114`; `Recovery/OwnershipJournal.cs:486-490` (ambiguous seal release), `567-573` (uncertain-resolve gate), `716-721` (recovery refused while a writer lease is live).
- Interleave: If the intent commit reaches the post-content phase and the seal unlink fails, `Write` returns failure although both slots durably hold the intent generation; the arbiter reports `Refused(NotCommitted)` without requesting stop (plan line 209 and ADR 0004 §6 step 2 both require a stop request). Follow-up interleavings: (a) a later same-owner `RequestStop` accepts at runtime but its `WriteMutation` cannot persist because `Resolve` still reports the seal; recovery (`TryRecoverCommit`) is refused while the live lease holds the fence; (b) the owner cannot retry the intent either (same uncertain-resolve gate); (c) on owner death the fence is released, recovery recognizes the both-slots-equal intent (`OwnershipJournal.cs:741-745`), and classification becomes `MayHaveExecuted` - conservative and correct.
- Unsafe effect: none at the release-position level. The release is blocked, no FD4 write occurs, and the eventual recovery classification is conservative. The injury is evidential and contractual: a caller that trusts the runtime decision publishes a false "not committed" fact; the durable stop evidence that the contract requires cannot be written during the wedge; and the live owner holds the fence forever, so operator-visible uncertainty persists as long as the process lives.
- Mitigations verified: intent content fully durable in both slots before ambiguity; `Resolve` never promotes sealed content; recovery lands on Intent.
- Why it matters to the plan: G3-A11/G3-B08 as written ("durable intent write fails definitely vs ambiguous seal failure; ambiguous case requests stop") cannot pass against the current component without the ambiguity/stop semantics being defined first; the fixture must also be able to produce this exact failure shape.
- Resolution hook: C1 (R09 remediation decision per Q06) plus C6 (fault-wrapper capability to inject the ambiguous shape).

### CE-03 - Multi-instance window: a runtime-accepted stop with failed/incomplete durable evidence does not bind a second arbiter instance; capability invalidation is the intended closure

- Path: `Execution/ReleaseArbiter.cs:270-289` (durable stop write may fail with the decision still accepted), `238-243` and `254-261` (clock-unavailable/overflow stops record `StopDeadlineNanoseconds = 0`), `226-298` (no capability consume on stop), `55-61` (post-stop release refusal leaves capability live).
- Interleave: Owner process accepts a stop at runtime; the durable stop write fails (or the clock path leaves no durable evidence at all); a second `ReleaseArbiter` instance constructed in the same process around the same journal and capability sees no durable stop (journal truth) and a fresh runtime gate (no `_stopAccepted`), so `RequestRelease` may plan an intent, pass the gate, consume the copy of the capability, and physically write FD4 - after a "won stop" that never became a durable fact.
- Unsafe effect: bounded. In the accepted single-arbiter owner model this path is unreachable (there is no second instance); the in-instance path is blocked by `_stopAccepted` (`55-61`) and, once a durable stop does exist, by the journal transition guard (`OwnershipTransitions.cs:27-31`). The plan itself (lines 150/310/326) requires the capability to be permanently aborted on a winning stop precisely to remove this window; the capability being live is the missing defense-in-depth, and its absence is a genuine contract breach of those plan lines even though no runnable single-instance path demonstrates a release-after-won-stop.
- Mitigations verified: `_stopAccepted` runtime gate; durable-stop journal guard; single-arbiter owner convention (owner composition has no second arbiter at R12).
- Why it matters to the plan: R08/G3-A02/G3-A07 evidence must assert `IsLive == false` after stop-first (no existing test does), and the multi-instance window must be declared out-of-model by remediation decision rather than silently assumed away.
- Resolution hook: C2 (R08 decision per Q05) plus C5 (clock/overflow stop cases in the test matrix).

### CE-04 - Concurrent duplicate loser returns `Refused` (with "intent already exists") while the winner returns `Released`, and a third caller later returns `Duplicate`

- Path: `Execution/ReleaseArbiter.cs:45-62,81-115,117-183`; `Recovery/OwnershipTransitions.cs:21-25`; `Launch/HeldExecution.cs:45-50`.
- Interleave: Caller A passes the entry-slot duplicate check (no retained decision yet) and enters the intent phase; caller B does the same; A wins the `_journalGate` race and writes the durable intent, then completes the gate and the FD4 write and installs the retained decision; B loses the race, `TryPlanReleaseIntent` fails against the now-intent baseline ("release intent already exists"), and B returns `Refused(NotCommitted)`. A third caller after B receives `Duplicate(Released...)` from the retained decision. One logical release produces three different decision kinds for three callers.
- Unsafe effect: none. Exactly one intent, one winner, one FD4 write (verified guards: transition baseline/sequence fencing; gate serialization; `Interlocked` one-shot transport). This is a semantics/API-contract gap against plan line 215 ("Duplicate calls return the original release decision") for the concurrent case only; the sequential case is proven by `ValidReleaseWritesExactlyOnceAndPersistsCommit`.
- Mitigations verified: as above; no divergent winner and no second write are possible under the current guards.
- Why it matters to the plan: G3-A05 must add the deterministic concurrent interleaving (currently absent from every test file) and its expected loser behavior; "duplicate/identical decision" as written is not a well-defined assertion for concurrent callers.
- Resolution hook: C3/C4 (Q07 decision: install an in-progress retained decision or narrow the documented concurrency semantics).

## Disposition summary

- No registered entry creates a demonstrated release-authority violation at the R12 single-arbiter composition.
- CE-02/CE-03 are the entries that require production-symbol decisions (R09/R08) before related gate evidence can be accepted; CE-01/CE-04 require an API-contract decision plus deterministic round-tests (R02/R06) before G3-A05 can be accepted.
- The register supports the plan-review disposition `PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES` in the main review document; it does not alter the WP05/WP08/WP10 boundary or the frozen harness disposition.
