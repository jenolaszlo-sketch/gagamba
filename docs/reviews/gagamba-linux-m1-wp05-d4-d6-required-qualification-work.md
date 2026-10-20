# WP05 D4-D6 Required Qualification Work - 2026-10-20

Status: independent reviewer statement of the minimum remaining work for a
future slice, derived from the reviewed D1-D3 slice against the binding prior
plan review and the bounds this reviewer verified. This document does not
authorize production changes, does not implement any of this work, and does
not accept any release gate.

Disposition: `WP05 D1-D3 DIAGNOSTIC EVIDENCE ACCEPTED AS A BOUNDED FIXTURE
FOUNDATION - D4-D6 REQUIRED BEFORE ANY RELEASE GATE - C1-C4 UNRESOLVED`.

## 1. Fixed inputs

- R12 product `38f6981b2a60daab2df37c92913a6760c1bef131` / tree
  `6854675d6b37a49d5302d6104103bdbfbc268040` with the current branch lineage
  `24e7498… -> D1-D3` (test-only, no production diff) is unchanged. Any D4-D6
  work that needs different production code must be separately reviewed and
  may not silently extend this lineage.
- Review binding `e4b0a798e944ddfb144901c63b0c444e7980533b` (blobs
  `f1a062e1…`, `73eb09af…`, `0bbafa6b…`) is binding.
- Frozen harness Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f` remains closed
  and not reopened.
- The reviewed evidence
  `docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-2026-10-20/`
  and this reviewer's
  `docs/reviews/gagamba-linux-m1-wp05-d1d3-review-artifacts/` are the
  diagnostic baseline; no higher claim may be rewritten out of them.

## 2. Required work items, D4/D5 scope

### D4: Release-owner fixtures at full seam coverage

Test-only. Must reuse the exact R12 semantics verified by this review's
fixture implementation audit and the D1-D3 probe set:

1. Release-owner SIGKILL-at-release-barrier fixture: real journal fence, real
   FD4 transport witness, kill at every boundary of the release decision
   (pre-intent, post-intent pre-seal, post-seal, post-transport, or the stop
   generation itself as documented). Not a supervisor mock.
2. Transport witness ledger (a test-only shim implementing the same interface
   as the real FD4 transport whose `DeliveryKind` classifications are
   asserted side-effect-free), so that the "exactly-once transport" question
   becomes observable after a kill.
3. Fresh-process variants for CE-01/CE-03/CE-04 windows, spawning a second
   owner process against the same journal (as with the C7 lease fixture) so
   the two-instance synthetic is no longer the only reproduction.
4. Stop-refused/signal machinery per lease retained across intermediate
   leases must not be re-inherited (extend the D1 pattern with the same
   remaining fenced state).

### D5: C9 native status negatives

Test-only against the real qualification coordinator:

1. Real FD5 status frames spanning coordinator `exec_failed`,
   `exec_confirmed`, `protocol_failed`, `exec_confirmed_root`,
   `root_unconfirmed` and the 20/21/22 exit-code mapping, including the
   invalid identity/wrong-sequence negative cases, against the held-execution
   descriptor topology (not the synthetic queue source).
2. The three currently `Unavailable` `Wp05ReleasePathIndependentQualificationTests`
   require the generated Linux native fixture and WP04 coordinator artifacts
   (checkout gap recorded in the producer and reviewer runs) before being
   counted as running.

## 3. Required work items, D6 scope

1. C1-C4 originating-review decisions recorded in the same review lineage
   with evidence (the D1-D3 fixture/reconciliation records may be cited but
   may not substitute for the holders' decisions).
2. If a decision creates any production source change, it needs its own
   evidence slice, separate from D4/D5, with real run re-qualification and
   formally preserved artifacts. A change may not piggy-back on the D4/D5
   acceptance claim.
3. G3-A/B/D/E diagnostic labels may be exercised only with evidence that
   still binds the pre-existing unresolved clause "C1-C4 decisions are not
   resolved"; the fixture work alone cannot tag them accepted.
4. Full release acceptance requires all of WP05's remaining unchanged rules:
   no target executed, no production post-release status through FD5, C7's
   release-owner crash/no-resend via the D4 fixtures, and review already
   retained for D4/D5 qualification. No acceptance compile may be run in a
   non-tooling context.

## 4. Non-scope riders

- No production code changes in D4-D6 as currently authorized by the binding
  plan review.
- No modifying `docs/plans` linearization text (already C8-set) unless a
  later binding review requires it.
- No minting "unavailable" test-centred claims of qualification; the three
  Unavailable facts must be converted by generating their required artifacts
  (see D5 point 2) or explicitly re-scheduled as non-qualifying.
- No verification: this document is a statement of required work only, and
  does not conduct any additional runtime runs beyond those recorded in the
  fixture-implementation and reconciliation reviews in this commit.

## 5. Evidence and preservation bar for the later slice

Per the repository's evidence standard, D4-D6 artifacts must bind:
exact source branch/commit and the unchanged production diff, fixture/source
SHA-256, environment identity (boot, cgroup, uid), per-suite counts with
explicit unavailable/ skipped accounting, cleanup evidence, and explicit
out-of-scope exclusions. Reaching them via re-run of the D1-D3 probe suite is
insufficient; the supervisor, witness ledger and native status negatives are
new work and need their own runs, OR an explicit `Held` record when artifacts
or the environment become unavailable, and that record must be preserved
verbatim. A skipped or unavailable case must never be counted as passed.
