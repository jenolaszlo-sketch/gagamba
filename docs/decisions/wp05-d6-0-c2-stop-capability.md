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

## D6.1 revision - A-C2-1..A-C2-3

Status: **PROPOSED - NOT APPROVED** (supersedes the earlier D6.0 draft text;
the original remains recoverable at `23df1c5...`).

### Exact consumption point (A-C2-1)

The first runtime stop acceptance is the linearization point. Inside the
`_gate` acceptance block, before returning the StopDecision:

- normal path (`ReleaseArbiter.cs:263-267`): consume the capability;
- monotonic-clock-unavailable path (`:238-243`): consume the capability;
- deadline-overflow path (`:247-261`): consume the capability.

Consumption is through the issuing store's single-use
`PreparedTransaction.TryConsume`; it is independent of `_journalGate` and of
whether the subsequent StopRecord write succeeds. Duplicate-stop re-entry
(`:232-235`) returns the retained stop without a second consumption. The
contract is exactly-once at first acceptance, irreversible and observable via
`IsLive == false`.

Durable stop persistence failure, clock unavailability and overflow never
restore the capability. A second arbiter sharing the same store cannot release
with that capability. The accepted single-arbiter owner model remains the
production composition assumption; the capability is the defense that closes
the reconstructed multi-instance window.

### Release-first behavior (A-C2-2)

If release has already consumed the capability, a stop arriving afterward:

- receives `TryConsume == false` with `Consumed`/`Aborted` (depending on store
  state), but stop acceptance still stands;
- does not reactivate, clone or replace the capability;
- does not rewrite release, delivery or Committed history;
- preserves the earlier retained release decision.

`_releaseRuntimeCommitted` is set after capability consumption and before the
transport write (`ReleaseArbiter.cs:157-175`), so the proposed contract defines
`ReleaseAlreadyCommitted` as "the runtime release path won and consumed before
this stop", including the consume-before-transport interval. It is **not**
proof that RELEASE reached the coordinator or that Committed was persisted.

### CE-03 declaration (A-C2-3)

The required declaration is preserved verbatim in substance:

> The CE-03 multi-instance window is outside the accepted single-arbiter
> owner model and is closed by the capability contract, not by composition.

The two-arbiter construction is a deliberately synthetic diagnostic. It does
not represent a supported production owner topology. Shared capability-store
single-use semantics and the real journal writer fence remain distinct
authority layers; neither permits a second simultaneously valid release
writer.

### Tests to add under later authorization

- Normal, clock-unavailable and overflow stop: `IsLive == false`.
- Failed durable stop: capability remains consumed.
- Duplicate stop: no second consume.
- Release-first before transport/after transport/after Committed: stop stands,
  history unchanged, flag not treated as delivery proof.
- Two arbiter instances sharing capability: no release/transport.
- Fresh replacement: no inherited capability; durable StopRecord remains
  authoritative where present.

### Open human question

Approve irreversible capability consumption at first stop acceptance,
including clock-unavailable/overflow and failed durable stop? **PENDING
HUMAN.**

## D6.2 cross-reference - R3 stop ordering

Status: **PROPOSED - NOT APPROVED**
C2 contract status: **DESIGN SUPPORTED FOR HUMAN DECISION - NOT APPROVED**

C2 semantics are unchanged. D6.2-R3 only defines what the ambiguous release
caller may return when a stop wins the runtime gate before ambiguous retention.
In that case the stop's first acceptance remains the C2 consumption point;
the ambiguous caller's own report is conditional (`Refused` stop-won or
`Duplicate` of a prior retained result), and its tail stop is idempotent. No
new consumption path, no new waiting protocol and no schema change are
introduced. The reader should treat this section as a cross-reference to C1
D6.2-R3, not a reopening of C2.
