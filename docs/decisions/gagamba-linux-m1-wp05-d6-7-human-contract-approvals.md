# Gagamba Linux M1 WP05 D6.7 — Human Contract Approvals

## 1. Purpose and exact Git binding

This is the authoritative record of the human contract decisions made on October 21, 2026, for WP05 D6 C1–C4 and the separate C3 expired-deadline policy choice. It records decisions and their limits; it does not authorize implementation or qualify a release.

- Canonical repository: C:\Users\Laszlos\source\repos\Gagamba
- Origin: https://github.com/jenolaszlo-sketch/gagamba.git
- D6.7 worktree: C:\Users\Laszlos\source\repos\Gagamba-d6-7
- Branch: decisions/wp05-d6-7-human-contract-approvals
- Required base and immediate parent of the D6.7 commit: 8fac756c3063d360de7c793fe959fc34845a8859
- Base branch: preservation/wp05-d6-6-glm-independent-symbol-delivery-review
- Base parent: D6.6 producer commit 4bcdac8ac0941f6ec70fb401db7f204f38a5770e

The frozen source and harness identities remain R12 production commit 38f6981b2a60daab2df37c92913a6760c1bef131 with tree 6854675d6b37a49d5302d6104103bdbfbc268040, and WP05 harness Q3 36a124d7dff98ecfaf956340767fe1768ad5b17f with tree f764722997130f06c8a62c45a4230805aa538ea5. They are references only and remain unchanged.

| Decision | D6.7 status |
|---|---|
| C1 — Ambiguous ReleaseIntent (R09) | APPROVED |
| C2 — Stop-first capability invalidation (R08) | APPROVED |
| C3 — Durable stop deadline and recovery (R27) | APPROVED |
| C4 — Duplicate/concurrent release semantics (R02/R06) | APPROVED |
| C3 expired-deadline policy | APPROVED — Policy A selected for Linux M1; Policy B deferred |

The authoritative status as of D6.7 is **HUMAN CONTRACT APPROVED - IMPLEMENTATION NOT AUTHORIZED**. It supersedes the earlier proposed/pending status for these decisions from this point forward; it does not rewrite the earlier record of what was pending at the time.

## 2. Decision authority and provenance

The human decision holder's conversation statements, reported in this task, are the approval authority recorded here. The statements are reproduced as supplied:

- C2: “I approve C2 as specified. Let's review C1 next.”
- C1: “yes c1 is good , including the D6.2 first-retainer and stop-ordering rules”
- C4: “yes, C4 as specified, including the D6.2 precision note”
- C3 contract: “yes, C3”

For the separate policy choice, the supplied decision history says the human accepted the recommendation to use Policy A for Linux M1 and defer Policy B configurability. No separate verbatim policy sentence was supplied, so this record does not invent one.

This is conversation-based human decision evidence. It is not a digital signature, cryptographic attestation, external approval ticket, or independently authenticated identity record. The prior GLM/Terra reviews are independent design evidence; they did not approve any contract for the human.

## 3. Evidence and independent-review references

The approved contracts are the D6.0 C1–C4 proposals as amended by their D6.1–D6.2 design records, including the D6.2 human-approval addendum and precision-amendment reconciliation. Relevant documents include:

- docs/decisions/wp05-d6-0-c1-ambiguous-intent.md
- docs/decisions/wp05-d6-0-c2-stop-capability.md
- docs/decisions/wp05-d6-0-c3-stop-deadline.md
- docs/decisions/wp05-d6-0-c4-duplicate-semantics.md
- docs/decisions/gagamba-linux-m1-wp05-d6-2-human-approval-addendum.md
- docs/decisions/gagamba-linux-m1-wp05-d6-2-precision-amendment-reconciliation.md
- docs/decisions/gagamba-linux-m1-wp05-d6-3-c3-outcome-correction.md
- docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md
- docs/decisions/gagamba-linux-m1-wp05-d6-5-fixture-isolation-equivalence.md
- docs/decisions/gagamba-linux-m1-wp05-d6-6-fixture-symbol-delivery.md

Earlier independent-review preservation checkpoints are also part of the design record:

- docs/reviews/gagamba-linux-m1-wp05-d6-1-independent-review-preservation-checkpoint.md
- docs/reviews/gagamba-linux-m1-wp05-d6-2-independent-review-preservation-checkpoint.md
- docs/reviews/gagamba-linux-m1-wp05-d6-3-terra-review-preservation-checkpoint.md
- docs/reviews/gagamba-linux-m1-wp05-d6-4-glm-review-preservation-checkpoint.md
- docs/reviews/gagamba-linux-m1-wp05-d6-5-glm-review-preservation-checkpoint.md

Those GLM/Terra records remain attributable to their original review stages and dates. Read with the later D6.4–D6.6 design records, they document design support and closure of the specified design findings only; none is a human approval or runtime qualification.
D6.6 independent review evidence is GLM commit 3c3d336c0a47ccb18627a5571298df8d0d1f0683, review document blob 0dbe1ab4cab9b5efc09cf15983f36fc61c11c3c2, and the preserved evidence-custody record at docs/reviews/gagamba-linux-m1-wp05-d6-6-glm-review-preservation-checkpoint.md. The recorded GLM disposition was: “WP05 D6.6 GLM INDEPENDENT REVIEW COMPLETE - OPTION A: D6.5-R2a VERIFIED ADDRESSED IN DESIGN; D6.4-R1 AND D6.4-R2 DESIGN FINDINGS CLOSED; C1-C4 READY FOR INDIVIDUAL HUMAN CONTRACT DECISIONS; C3 POLICY SELECTION PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED”.

That disposition describes the state before these human decisions. D6.4–D6.6 fixture-design findings are closed in design as reviewed; that does not establish implementation completion, runtime qualification, or release acceptance. D6.3–D6.6 accepted fixture/testability specifications, including R1-T13–T15, remain future specifications and are not represented as executed tests.

## 4. C2 approval — Stop-first capability invalidation

The human approved C2 as specified.

Consume the existing single-use release capability irreversibly and idempotently at the first runtime stop acceptance. This applies to normal stop, unavailable-clock, and deadline-overflow acceptance paths, independently of whether a durable StopRecord can be persisted. A duplicate stop does not consume it again. A failed durable stop write cannot restore release authority.

If release wins first, preserve its existing release, delivery, and durability evidence. A fresh owner does not inherit or recreate the consumed capability. The multi-instance diagnostic is addressed by these capability semantics; this decision does not claim that the diagnostic's topology is supported production composition. No new durable schema is approved.

C2 is the implementation prerequisite for C1's permanent authority closure.

## 5. C1 approval — Ambiguous ReleaseIntent

The human approved C1, including the D6.2 first-retainer and stop-ordering rules.

The contract introduces the explicit ReleaseDecisionKind.AmbiguousPersistence result and distinguishes a definite pre-Intent persistence failure from an ambiguous post-content seal failure. Ambiguity is represented conservatively as MayHaveExecuted. It is not classified as definite NotCommitted. No second Intent or FD4 RELEASE is permitted, and journal ambiguity remains preserved independently of the runtime result.

After the ambiguous journal operation releases the journal lock, retain the applicable release decision under the runtime gate. Release that gate before requesting the bounded runtime stop as a tail action. Use C2 capability consumption on first stop acceptance. Attempt durable stop persistence on a best-effort basis without inventing durable evidence. This contract does not claim that the production Owner already performs the required bounded domain stop.

D6.2 first-retainer ordering is:

1. **Ambiguity wins retention:** retain AmbiguousPersistence, then request the tail stop.
2. **Stop wins first:** retain the stop-won Refused(MayHaveExecuted) result without erasing journal ambiguity.
3. **A release decision is already retained:** return Duplicate without overwriting it.

Preserve the D6.2-R3 conditional-retention correction and D6.2-R4 test-fixture provenance correction. A stop-won refusal is a runtime result, not evidence that durable Intent is absent.

## 6. C4 approval — Duplicate and concurrent release semantics

The human approved C4 as specified, including the D6.2 precision note.

Sequential calls after a decision is retained receive Duplicate. A concurrent loser may receive Refused while another caller wins. Preserve the existing pre-gate versus gate-level retention asymmetry; semantic refusal classification is not determined solely by lock ownership. Preserve the stop-won refusal and sequential Duplicate clarification, along with C1's retained ambiguity semantics.

Do not add a ConcurrentRefusal decision kind or a waiting/notification protocol. At most one physical RELEASE is permitted. Refused does not prove that another caller did not release, and Duplicate does not prove durable Committed. No new journal schema or synchronization architecture is approved.

## 7. C3 Policy A selection

Policy A is selected for Linux M1:

**When a valid durable stop deadline is expired, permit the existing identity-checked kill and one immediate population observation, without renewing or extending the waiting allowance.**

If emptiness is unconfirmed, preserve the conservative Unknown/quarantine outcome. Policy A does not recreate release authority and does not authorize an additional waiting allowance.

Policy B remains deferred as a possible future configurable capability. It requires separately bounded authority, design review, implementation authorization, and qualification. This selection does not authorize configurable expired-deadline behavior in M1.

## 8. C3 contract approval — Durable stop deadline and recovery

The human approved C3 with the previously selected Policy A and the complete D6.2 amendments.

- A persisted absolute StopRecord deadline is authoritative. Recovery computes remaining time from that original deadline. Restart, reconciliation, or partial cleanup cannot renew it.
- Use a sealed, corroborated production monotonic clock with verified epoch and frequency. Reject incompatible, unavailable, or invalid clock evidence conservatively, and preserve cross-boot refusal.
- Apply Policy A on a valid expired deadline.
- Distinguish Failed, Unknown, Confirmed, and Refused correctly under the D6.2 typed stop outcome contract; preserve the separate JournalFinalization classification.
- Known operation failures remain Failed. Persistence-only uncertainty does not become a fabricated operation failure or successful finalization.
- Report CleanupConfirmed only when every required component and durable finalization are confirmed.
- With no valid StopRecord, do not fabricate a deadline, a new waiting window, or release authority. A separate cleanup allowance requires independent authorization.
- Preserve the D6.3–D6.6 accepted fixture and testability specifications, including R1-T13–T15.

These are approved contract requirements, not claims that the production clock, recovery path, Policy A behavior, or operating-system behavior has been implemented or qualified.

## 9. Contract dependencies and invariant preservation

The recommended implementation dependency order is C2 → C1 → C4 → C3. C2 closes capability authority at stop acceptance and is a prerequisite for C1. C4 preserves C1 ambiguity and first-retainer behavior. C3's contract includes the selected Policy A. This sequence is a recommendation only.

The decisions preserve no-resend behavior, irreversible stop capability consumption, at-most-once physical RELEASE, the immutable absolute stop deadline, conservative treatment of uncertainty, and C1's conditional first-retainer ordering. No durable schema or new synchronization architecture is approved.

## 10. Implementation authorization boundary

D6.7 records human contract approval only. It authorizes no production remediation, test execution, integration work, release qualification, or publication. No source change, test run, G3 acceptance, or OS-level qualification is implied.

The production Owner integration remains deferred. WP08 and WP10 remain deferred. Full G3 acceptance is not established. H1–H5 remain unchanged. Portability qualification remains deferred. The R12 production code and frozen WP05 harness remain unchanged.

## 11. Deferred future configurability

Policy B is not approved for M1 implementation. Any future configurable expired-deadline policy must be proposed as a separately bounded capability with its own authority limits and upper bound, receive design review, receive explicit implementation authorization, and pass the applicable qualification. Policy A cannot be reinterpreted as permission to add that configurability.

## 12. Required implementation and qualification gates

Each implementation tranche requires its own explicit authorization. C2 is the proposed first tranche and must define its exact production source scope and deterministic tests before production implementation. At minimum, its test/evidence plan must cover:

- stop-first and release-first scenarios;
- normal stop, clock-unavailable, and deadline-overflow paths;
- durable-stop persistence failure;
- single-use capability behavior across arbiter instances;
- duplicate-stop idempotency; and
- fresh-owner recovery with no authority resurrection.

The C2 tranche must also specify independent adversarial code review, source- and commit-bound evidence, and failure and rollback criteria. The implementation and qualification evidence must be bound to the exact authorized source and commit. Later C1, C4, and C3 tranches likewise require separate authorization, review, and source-bound qualification. Contract approval alone closes no release gate.

## 13. Final human-decision disposition

**HUMAN CONTRACT APPROVED - IMPLEMENTATION NOT AUTHORIZED**

**WP05 D6.7 HUMAN CONTRACT DECISIONS RECORDED - C1 C2 C3 C4 APPROVED; C3 POLICY A SELECTED; POLICY B DEFERRED; IMPLEMENTATION AUTHORIZATION PENDING; RELEASE NOT QUALIFIED**

On completion of the documentation commit: **WP05 D6.7 DOCUMENTATION COMMITTED - INDEPENDENT PRESERVATION AND IMPLEMENTATION AUTHORIZATION OUTSTANDING**.
