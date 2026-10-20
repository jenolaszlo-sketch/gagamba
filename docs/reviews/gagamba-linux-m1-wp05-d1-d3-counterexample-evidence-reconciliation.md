# WP05 D1-D3 Counterexample Evidence Reconciliation - 2026-10-20

Status: independent second-step review document. It reconciles each recorded
counterexample on the R12 release path with the diagnostic observations that
the reviewed D1-D3 slice actually reproduces, and states precisely what is not
yet established. It does not decide any originating review item and does not
accept any gate.

Disposition: `WP05 D1-D3 DIAGNOSTIC EVIDENCE INDEPENDENTLY RECONCILIATED -
REPRODUCE-OR-UNPROVEN ASSIGNED PER COUNTEREXAMPLE; C1-C4 REMAIN UNRESOLVED`.

## 1. Binding

- Slice base `24e7498f1940c8e985d229a48b208fe4a0065b26`; D1
  `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c`; D2
  `dc4c372b180abef1b27038d94f49af90226fa548`; D3
  `338a98af269e6b5839f3ccc2e0881f016153b01e`.
- Counterexample register blob
  `0bbafa6b9725cce8a20858e1d64a8deec6134f15`
  (`docs/reviews/gagamba-linux-m1-wp05-release-plan-counterexample-register.md`
  on the preservation branch).
- Probe source reviewed in
  `gagamba-linux-m1-wp05-d1-d3-diagnostic-fixture-implementation-review.md`
  in the same commit. Reproduction status is per that review's independent
  rerun (all 13 probes passed on reviewer-executed run).

## 2. CE-01 (concurrent duplicate release, R02/R06)

Original finding: concurrent duplicate release invocation does not retain the
loser's refusal; three callers can be created; released-with-`MayHaveExecuted`
and `Duplicate` can consume the same intent in one write window vs another;
the loser of the race does not reach retention.

What D2 reproduces (probe `ConcurrentDuplicateReleaseProducesOnePhysicalWrite`,
deterministic through the `after-intent` `TestBarrier`):

- Two intent-writing windows: winner parked inside the arbiter with the
  durable intent present; second caller invokes concurrently.
- Loser: `Refused` with durable release `NotCommitted`; no retention by the
  first arbiter instance.
- Winner (`later` caller after resume): sees retained activation plus intent.
- Independent fact updates: transport attempts = exactly 1; journal
  `Release` = `Committed`; loser's persisted decision absent.
- Reviewer conclusions: reproducible at the component seam; the three-caller
  data triage maps exactly to the R02/R06 question (which single ordering is
  mandated after the intent-write?); the probe does not insert a widget into
  any crane decision: what R02/R06 MUST be remains an originating-review
  decision, not test-reversal.

What remains unresolved: the R02/R06 decision itself and any production
implementation change it requires; the fresh-process (real cross-process)
ordering question; retention semantics on refusals by design.

## 3. CE-02 (ambiguous post-content seal-failure wedge, R09)

Original finding: when the seal-release `unlink` fails after the seal was
created, the write is a definite failure before it is an acknowledged re-fix
(`fences-only` semantics); the loser is a real-data wedge where the journal
is unreadable and releases cannot be recorded; recovery classification
starts as `MayHaveExecuted` shape and lacks an explicit R09 bridge.

What D2 reproduces (probe `AmbiguousSealFailureLeavesUncertainIntentAndNoStop`
with `FailAt("unlink", 2)`):

- The runtime decision is `Refused(NotCommitted)`; the transport is never
  tried (0 attempts); capability stays live.
- Journal read fails with a "seal"-bearing error after the failed
  acknowledgement.
- The stop requested by this release frame's caller shows externally:
  `stop.Accepted` is true, `stop.DurableStop` is false (i.e. the stop cannot
  become durable while the wedge persists).
- Recovery path itself (`TryRecoverCommit` after `ReleaseWriter`) returns the
  intent and classifies it `MayHaveExecuted` with exec `NotAttempted`.
- Reviewer conclusions: the wedge shape is real on R12 and matches the R09
  observation; the injection point selection is proven by the journal's
  failure/classification output, not by a magic counter in the test seam.

What remains unresolved: the R09 contract decision (whether the refactored
shape is acceptable), any source change for either resolution, and the
release-acceptance semantics for monitoring a wedged-journal station.

## 4. CE-03 (stop-handling semantics vs multi-instance arbiter, R07/R08)

Original finding: stop handling is decoupled from capability-reporting on R12;
a crashed arbiter leaves no retained activation for abandoned support records
and refused releases; the stop deadline is the only state-of-record; the same
inventory of uncertain states can surface compliant results by entering R18
arbiters under the same capability (capability invalidation is the only
counterfactual security loss).

What D2 reproduces (probes
`StopFirstLeavesCapabilityLiveOnR12`,
`DurableStopBlocksReleaseIntentFromSecondArbiter`,
`StopPersistenceFailureAllowsSecondArbiterReleaseOnR12`):

- First arbiter, normal stop: durable stop; capability stays live through
  both queries (`IsLive`).
- Second arbiter over a durable stop: refused, no release intent, stop
  record visible.
- First arbiter's stop-durable-write fails: no Stop record; second arbiter
  releases exactly once (attempt 1, Committed).
- The clock-unavailable and overflow stop frames (`Stop…WithoutDurableStop`
  probes) record no durable stop and still skip release attempts.
- Reviewer conclusions: the second-arbiter window is real on R12 at the
  component level; the "durable stop blocks, non-durable stop does not"
  asymmetry is the R07/R08 lesson surfaced by the probes.

What remains unresolved: any R07/R08 decision; the fresh-process variant of
the same window (a real owner rewritten after a stop write failure) — current
reproduction is synthetic two-instance in one process; stop-web hook
integration with the actual supervisor.

## 5. Additional seams the reviewed slice staked out

- C3/R27: `Reconciler` consumes its caller's allowance even after the
  persisted stop deadline has long expired, measured at 5 seconds real
  advance, with the kill performed and outcomes not CleanupConfirmed.
- C5/W3-W31: time-model branch facts (clock-unavailable stop, overflow
  deadline) observed via runtime acceptance fields only (0,0) while the
  release is refused independently of that state.
- C9: the durable-commit gate, failed flush non-persistence, and synthetic
  identity/sequence/EOF/malformed frames are all observable; the real FD5
  descriptor topology and coordinator negative frames are not.

## 6. Reviewer judgment on reproduced-vs-unresolved

| Counterexample | Reproduced component-level | Fresh process | Decided |
|---|---|---|---|
| CE-01 | yes | no | no |
| CE-02 | yes | recovery-path only | no |
| CE-03 | yes | no | no |
| CE-04 | yes (TestBarrier shape) | no | no |

None of the four counterexamples is `resolved` by this slice alone; CE-02's
recovery classification is the only one where source code actually
exercised the recovery entry, and even there the release-acceptance decision
stays open. What D2 does establish — and what was previously not itself an
explicit independent assertion — is the exact decision surface R12 exposes
for each window, verified under non-determinism-safe seams and reproduced
independently by this reviewer on the supported Linux environment.

## 7. Forward implications for WP05

- The D4/D5 release-owner supervisor, transport witness and coordinator
  receipt ledger should be specified on top of, not instead of, these probe
  seams: the same decision surfaces are what a compliant owner must observe.
- Any fresh-process variant of CE-01/CE-03/CE-04 will have to reuse the C7
  lease fixture pattern (real journal, real fence, SIGKILL, `/proc`-bounded
  assertions) instead of in-process arbiter instances.
- Reconciliation done means the four counterexamples now each carry at least
  one reproducible observed reference; acceptance cannot be inferred from the
  reproduction and none of the reproduction pushes any originating review item
  toward a value.
