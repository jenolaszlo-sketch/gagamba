# WP05 D6.0 C2 - R08 Stop-First Capability Invalidation

Status: **PROPOSED - NOT APPROVED**

Decision owner: human WP05 release/security decision holder after GLM design
review. This record proposes behavior; it does not approve or implement it.

## 1. Original requirement

C2 (required-changes blob `73eb09af...`): a winning stop must permanently
abort/invalidate the release capability; add a test asserting `IsLive == false`
after stop-first; declare the CE-03 multi-instance window outside the accepted
single-arbiter owner model and close it by the capability contract, not only by
composition. Requirement R08; gates G3-A02/G3-A07.

## 2. Current R12 behavior (`SOURCE-CONFIRMED`)

- `ReleaseArbiter.RequestStop` sets `_stopAccepted` and records a runtime
  StopDecision; it does not call `PreparedTransaction.TryConsume` and does not
  invalidate the capability (`Execution/ReleaseArbiter.cs:226-296`).
- Same-instance release is refused by `_stopAccepted` (`:55-61,125-131`).
- A durable StopRecord blocks Intent at the journal layer
  (`Recovery/OwnershipTransitions.cs:27-31`).
- Clock-unavailable/overflow stop paths are accepted with deadline 0 and no
  durable stop (`ReleaseArbiter.cs:238-261`).
- If durable stop persistence fails, a second arbiter over the same live
  capability can plan release; D2 probe
  `StopPersistenceFailureAllowsSecondArbiterReleaseOnR12` reproduces this at
  the component seam. `PreparedTransaction.IsLive` remains true after stop
  (`Preparation/PreparedTransaction.cs:52-58`).
- No runnable normal single-owner path demonstrates release-after-won-stop;
  the gap is bounded defense-in-depth (original review and D4/D5 evidence).

## 3. Evidence

`SOURCE-CONFIRMED`; D2 stop-first/failed-stop/durable-stop probes;
`INDEPENDENTLY REPRODUCED` by independent D1-D3 review. D4/D5 add real
fresh-process stop/lease cases but do not alter the R12 capability behavior.
`UNPROVEN`: a production second-owner composition; no such composition exists
at R12 (WP08/WP10).

## 4. Candidate contracts

| Option | Contract | Assessment |
|---|---|---|
| C2-A | Consume/invalidate capability at runtime stop acceptance, independent of journal persistence. | Recommended: uses existing single-use store; closes multi-instance window; no schema. |
| C2-B | Invalidate on first post-stop release refusal. | Leaves a window before the refusal and depends on a future caller; rejected. |
| C2-C | Single-arbiter composition only. | Does not meet R08 permanent-abort; retained only as an implementation assumption, not the primary contract. |

## 5. Recommended C2 contract

1. At every stop runtime-acceptance path (normal, clock unavailable, deadline
   overflow), before returning the StopDecision, consume/invalidate the
   capability through its issuing store under `_gate`.
2. The invalidation is irreversible and idempotent; `IsLive` becomes false
   after a winning stop even if durable StopRecord persistence fails.
3. Stop-first: subsequent release refused, no FD4, `IsLive == false`.
4. Stop concurrent with release after durable Intent: release caller observes
   stop and is refused conservatively (`MayHaveExecuted` per existing gate
   path); capability cannot be reused; no FD4.
5. Stop after capability consumption: stop is accepted but cannot reactivate a
   consumed capability; release already won remains governed by its existing
   evidence.
6. Stop after FD4/Committed: stop does not rewrite release/delivery history.
7. Stop with clock unavailable/overflow: capability still invalidated; no
   durable StopRecord is claimed; runtime deadline is 0.
8. Failed durable stop: capability closure does not depend on persistence;
   StopRecord absence remains a recovery/cleanup fact.
9. Duplicate stop remains idempotent; duplicate release after stop returns the
   retained refusal, not a new attempt.
10. Multiple arbiter instances sharing the same capability cannot both
    release because the store's single-use consume is shared.
11. Fresh owner after crash receives no capability; journal StopRecord (if
    durable) remains authoritative, but runtime invalidation is not inherited.

The linearization point is the runtime stop acceptance and capability consume
under `_gate`. Stop precedence is a runtime winner; durable StopRecord remains
separate evidence. The accepted single-arbiter owner model is still presumed
for production composition, but the capability contract no longer depends on
that presumption for this window.

Forbidden: release after stop with the same capability; reactivating a
consumed capability; treating failed durable stop as permission to reuse
authority; a second writer/capability.

Production symbols expected: `ReleaseArbiter.RequestStop`; use of
`PreparedTransaction.TryConsume`; optional `StopDecision` observability field.
No schema change.

## 6. Rejected alternatives

- C2-B: insufficient closure window.
- C2-C: does not satisfy permanent abort.
- New separate stop token/journal: unnecessary; the existing single-use
  capability store already expresses permanent invalidation.

## 7. Acceptance tests

1. Stop-first: `IsLive == false`, release refused, no transport.
2. Stop with failed StopRecord persistence: `IsLive == false`.
3. Stop with clock unavailable/overflow: `IsLive == false`.
4. Second arbiter sharing capability: no release, no transport.
5. Release-first: stop does not rewrite Committed/delivery; capability already
   consumed.
6. Duplicate stop remains idempotent.
7. Stop at after-intent barrier: no FD4, release result conservative.
8. Fresh replacement after SIGKILL: no capability inherited; no release.
9. Durable StopRecord remains authoritative where present.
10. Exactly one successful release possible across all cases.

Tests are required against the new production tree; D2/D4 fixtures are
reusable but do not qualify the changed code.

## 8. Compatibility, risks and open questions

- Runtime behavior only; no journal schema or serialization change.
- Tests that assert `IsLive == true` after stop must change under authorized
  remediation.
- Possible risk: consuming capability on stop after a release has already
  consumed it must not turn a stop into a contradictory release result; the
  existing `TryConsume` result is observed, not treated as release authority.
- Open: whether `StopDecision` needs an explicit `CapabilityInvalidated` field
  for consumers; recommendation is optional, with tests asserting `IsLive`.
- Review: GLM design review required; approval: **NOT APPROVED**.
