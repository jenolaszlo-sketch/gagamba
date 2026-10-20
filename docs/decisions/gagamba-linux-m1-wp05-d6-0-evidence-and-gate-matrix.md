# WP05 D6.0 Evidence and Gate Matrix

Status: **PROPOSED - NOT APPROVED**

Binding: R12 `38f6981...`, tree `6854675d...`; D5 preservation
`578b208...`; original required changes blob `73eb09af...`.
This matrix is analysis only. No gate is accepted.

| Decision | Original finding | R12 source symbols | D2/D4/D5 evidence | Recommended contract | Missing evidence / future change | Gate blocked |
|---|---|---|---|---|---|---|
| C1 R09 | Ambiguous seal failure can leave both slots holding Intent while arbiter reports definite `NotCommitted` and requests no stop. | `ReleaseArbiter.RequestRelease` `:110-114`; `OwnershipJournal.Write` `:424-489`; `RequestStop` `:226-296`; `ReleaseRecoveryClassifier` `:28-38` | D2 ambiguous/definite probes; D4-6 fresh-process; independently reproduced | Explicit ambiguous-persistence decision; `MayHaveExecuted`; no FD4; bounded runtime stop; best-effort durable stop; retained result; recovery Intent | Decision approval; production API/arbiter change; post-change wedge/stop tests; owner domain-stop integration WP08/WP10 | G3-A11, G3-B08 |
| C2 R08 | Stop-first does not invalidate the capability; failed durable stop + second arbiter can release. | `ReleaseArbiter.RequestStop` `:226-296`; `_stopAccepted` `:24,55-61`; `PreparedTransaction.IsLive/TryConsume`; `OwnershipTransitions.TryPlanStop` `:108-150` | D2 stop-first/failed-stop; D5-CR3/CR3b diagnostics; independently reproduced | Consume/invalidate at runtime stop acceptance for normal, clock-unavailable and overflow paths; irreversible; no schema change | Decision approval; production `RequestStop` change; tests asserting `IsLive == false`; post-change fresh-lease/stop suite | G3-A02, G3-A07 |
| C3 R27 | Persisted stop deadline is immutable but `Reconciler` uses caller allowance and never consumes the deadline. | `Reconciler._stopAllowance` `:124-129`; `StopAndAwaitEmpty` call `:231`; `StopRecord` `OwnershipRecord.cs:119-122`; allowance `:202`; planner `OwnershipTransitions.cs:108-150` | D2 R27 probe; D4/D5 diagnostics; independent review agrees source gap | `Reconciler` derives remaining = deadline - verified monotonic now; expired => zero-wait urgent stop; zero/unverifiable => quarantine/Unknown; no reset | Decision approval; `Reconciler` change and deterministic clock tests; no schema change | G3-A10, R27 |
| C4 R02/R06 | Sequential duplicates get Duplicate; concurrent loser may get Refused while winner releases; retention asymmetry. | `ReleaseArbiter.RequestRelease` `:48-61,81-115,176-223`; `HeldExecution.TryWriteRelease`; `OwnershipTransitions.TryPlanReleaseIntent` | D2 concurrent probe; D4-9; D5-CR3 second-owner/fence; independently reproduced | Explicit concurrent-refusal semantics; sequential Duplicate; document outside-gate vs gate retention; no waiting; one physical write | Decision approval; possible decision-comment/retention changes; tests for all failure classes; no schema change | G3-A05 |

## Source-to-evidence sufficiency

- C1: source confirmed; D2/D4 reproduced; no production owner domain-stop
  path. Decision and post-change tests required.
- C2: source confirmed; D2/D4 reproduced; no normal single-owner release after
  stop was demonstrated. Multi-instance risk is bounded; capability contract
  is the proposed closure.
- C3: source confirmed; D2/D4 diagnostic; no production caller currently
  consumes the deadline. Decision and deterministic clock tests required.
- C4: source confirmed; D2/D4/D5 reproduced the API/retention shape; no
  second-write hazard observed. Decision and updated contract tests required.

## Gate state

No G3-A/B/C/D/E, G2a/G2b/G3 or WP05 release gate is accepted. D4/D5 fixture
passes do not convert these gates to PASS. All C1-C4 items remain unresolved
until human approval and separately authorized remediation.

## Qualification transfer

R12 evidence remains valid for R12 only. New production commits would require
new source binding and reruns:
- D1-D3 contract diagnostics;
- D4 fresh-process supervisor/crash/no-resend, with new source/tests;
- D5 native FD5 and release-path rows, with new production tree where affected;
- affected native/protocol regressions and focused suites;
- new independent review and evidence preservation.

Native FD5 semantics are not changed by C1-C4 proposals, but the tests must be
rerun and rebind if `ReleaseArbiter`/`Reconciler` production changes alter the
tree or fixtures.

## Handoff H1-H5

H1 D5-O05: component `ReleaseObservationBuffer` has zero production callers;
no production status wiring or target-status persistence claim. Future owner:
WP08/WP10 per authoritative plan.
H2: extra inherited FD5 writer/wrong-FD/helper-writer experiments remain
unclaimed; smallest safe future test-only package is a second-writer/EOF case
with documented ownership.
H3: raw wait4/pidfd termination witness remains optional; bounded control-based
standard adopted; do not reopen frozen harness.
H4: C1-C4 this package.
H5: D5 plan commit-order documentation note; no historical rewrite.

WP08/WP10, production post-release status and full G3 remain deferred.
