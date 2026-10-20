# WP05 D6.0 C3 - R27 Persisted Stop-Deadline Consumption

Status: **PROPOSED - NOT APPROVED**
C3 status: **CONTRACT CHOICE REQUIRED - HUMAN DECISION PENDING**
Human expired-deadline policy choice: **PENDING**

Decision owner: human WP05 release/security decision holder after GLM design
review. This record proposes behavior; it does not authorize code.

## 1. Original requirement

C3 (required-changes blob `73eb09af...`): derive the stop allowance from
`StopRecord.StopDeadlineNanoseconds` (remaining = deadline - now) or prove a
caller-supplied value was computed from the record; test within-window,
expired-deadline and no-reset behavior. R27; gate G3-A10 and any R27 pass.

## 2. Current R12 behavior (`SOURCE-CONFIRMED`)

- `StopRecord` stores `StopAcceptedAtNanoseconds`, cause and
  `StopDeadlineNanoseconds` (`Recovery/OwnershipRecord.cs:119-122`); validator
  treats the record as immutable; transition planner computes
  `acceptedAt + 5_000_000_000 ns` (`OwnershipTransitions.cs:108-150`).
- `Reconciler` stores a constructor-supplied `TimeSpan _stopAllowance`
  (`Recovery/Reconciler.cs:124-129`) and passes it to
  `DomainLease.StopAndAwaitEmpty` (`:231`); it never reads the StopRecord
  deadline.
- `DomainLease.StopAndAwaitEmpty` computes its own `now + timeout` and polls
  until empty/timeout (`Lifecycle/DomainLease.cs:108-140`).
- The record validator prevents rewriting the deadline, but no consumer
  converts it into the actually used bound. D2/D4 diagnostic confirms caller
  allowance is used after persisted expiry. `SOURCE-CONFIRMED`,
  `INDEPENDENTLY REPRODUCED`.

## 3. Candidates

| Option | Contract | Assessment |
|---|---|---|
| C3-A | `Reconciler` derives remaining from durable StopRecord + verified clock. | Recommended: immutable record is already the authority; no schema/API expansion. |
| C3-B | Caller derives remaining and passes it. | Provenance is unenforceable and easy to reset; no current production caller. |
| C3-C | New deadline-bound authority object. | Unnecessary abstraction; existing record carries the bound. |

## 4. Recommended C3 contract

When a StopRecord is present and valid, `Reconciler` must use:

`remaining = max(0, StopDeadlineNanoseconds - clock.NowNanoseconds)`

and pass that bounded duration to the domain stop/empty wait. The caller's
constructor allowance is ignored for this record path.

Cases:

- No StopRecord: retain existing caller-supplied cleanup allowance; this is not
  an accepted stop deadline.
- Valid future deadline: use remaining; no extension/reset.
- Deadline exactly reached/expired: perform a bounded urgent stop with zero
  remaining wait (kill + immediate population observation); if populated,
  return Unknown/quarantine, not a fresh allowance.
- StopDeadline <= 0, invalid accepted time, unavailable clock, or arithmetic
  overflow: do not fabricate a five-second window; classify Unknown/quarantine
  and preserve existing authority/evidence. No unsafe destructive action
  without a verifiable deadline.
- Boot mismatch: existing `RecoveryPlanner` cross-boot refusal; no transfer.
- Owner death/fresh writer: same stored deadline; no reset.
- Repeated reconciliation: same absolute deadline; progress persistence does
  not reset it.
- Domain already empty: no unnecessary WAIT under the D6.1 Option B draft; a kill may be issued before the population observation; record/verify.
- Domain still populated after expiry: bounded zero-wait stop then conservative
  Unknown if not empty.
- Partial cleanup/resume: completed bits persist; remaining deadline unchanged.

The record stays immutable. `StopAndAwaitEmpty` receives a duration derived
from the deadline, not a caller constant. The production symbol likely changes
only in `Reconciler`; no `OwnershipSnapshot` v3 change. If the design chooses
to retain the public constructor, the record-derived path overrides the
caller allowance; the no-record cleanup path remains explicit.

Forbidden: a new five-second window after restart; treating zero/unverifiable
deadline as permission; rewriting StopDeadline; wall-clock sleeps as the sole
timing evidence.

## 5. Rejected alternatives

- C3-B: provenance cannot be structurally enforced; rejected.
- C3-C: new authority object adds no safety; rejected.
- Ignoring zero/invalid deadlines and using caller allowance: rejected; it is
  exactly the reset risk R27 forbids.

## 6. Acceptance tests

1. Within-window takeover uses remaining allowance.
2. Late takeover uses less time; no extension.
3. Expired-deadline behavior is policy-pending; Policy A recommends zero-wait kill + one observation, not selected.
4. Restart does not reset allowance.
5. Repeated reconciliation does not accumulate time.
6. Zero/invalid deadline is Unknown/quarantine, no fresh 5s.
7. Clock unavailable/overflow fails closed.
8. Boot mismatch refused.
9. StopRecord unchanged before/after.
10. Partial cleanup resume does not reset deadline.
11. Domain-empty path does not wait unnecessarily.
12. Domain-populated-after-expiry outcome is explicit.
13. Identity/cleanup authority remains intact.
14. Timing evidence uses a controllable monotonic clock.

D2 `ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired` is a
baseline; it must be replaced/expanded under authorized remediation and rerun
against the new tree.

## 7. Compatibility, risks and open questions

- No schema change; `Reconciler` behavior and tests change. Existing callers
  passing five seconds remain compatible for no-stop cleanup but cannot
  override a persisted deadline.
- Risk: zero-wait urgent stop may leave more Unknown/quarantine outcomes; that
  is the intended fail-closed behavior.
- Open policy question: for an expired deadline, is a zero-wait kill+one
  observation acceptable as "bounded urgent action" (recommended), or should a
  different bounded interval be chosen? It must never be a fresh 5s.
- Open: whether `StopAndAwaitEmpty` needs a duration-vs-deadline overload; no
  schema change required.
- GLM review required; approval: **NOT APPROVED**.

## D6.1 revision - A-C3-1..A-C3-4

Status: **PROPOSED - NOT APPROVED**
Human expired-deadline policy choice: **PENDING**

### Clock identity and epoch (A-C3-1)

Durable StopRecord timestamps are created by `OwnerClock` using
`Stopwatch.GetTimestamp()` converted to nanoseconds
(`OwnerPreparationTransaction.cs:21-32`); this is the QPC-based transaction
epoch. `LaunchDomainClock` uses the same QPC conversion
(`BubblewrapAdapter.cs:517-521`). `OwnerDomainClock` uses
`Stopwatch.StartNew()` elapsed time (`OwnerPreparationTransaction.cs:34-39`),
whose epoch is the construction instant and is **not numerically comparable**
to the QPC transaction timestamps. Two monotonic clocks are not interchangeable
when their epochs differ.

The proposed contract requires one corroborated production monotonic epoch for
all durable deadline arithmetic. The recommended minimal resolution is:

- replace `OwnerDomainClock` with a QPC-based implementation using the same
  conversion as `OwnerClock`/`LaunchDomainClock`; and
- make the Reconciler clock dependency carry a clock-epoch/corroboration
  identity that must match the transaction clock. A test-only clock must
  declare the accepted epoch; an incompatible clock is rejected rather than
  silently used.

Proposed exact corroboration mechanism (subject to implementation review):
introduce an internal `MonotonicEpoch` marker with one production value, `Qpc`;
add a narrow `Epoch` member to the `IDomainClock` dependency used by
`Reconciler`; have `LaunchDomainClock` and the replacement owner domain clock
return `Qpc`; require `Reconciler` to accept only `Qpc` and route any other or
unknown epoch to quarantine/Unknown. Test clocks must explicitly declare the
accepted epoch; a `Stopwatch.StartNew()` relative clock cannot declare `Qpc`.
No new durable field is required: boot identity is verified separately by the
existing recovery planner, and the epoch marker is a runtime contract. A
duration-vs-deadline `StopAndAwaitEmpty` overload is optional; the epoch is the
substantive issue.

When a StopRecord exists and the clock is corroborated:

`remaining = max(0, StopDeadlineNanoseconds - clock.NowNanoseconds)`

with checked/range-validated units and same-boot identity. The resulting
duration is passed to the existing `StopAndAwaitEmpty(TimeSpan, IDomainClock)`.
When epoch identity cannot be established, clock unavailable, arithmetic
invalid/overflow, or boot identity mismatches: do not calculate a fabricated
window; fail to the conservative Unknown/quarantine path and preserve retained
authority/evidence. Never reset the five-second bound.

### Outcome mapping (A-C3-2)

The proposed Reconciler semantics distinguish:

- **Failed**: kill/backend operation genuinely failed, or an invariant/identity
  operation failed where the contract classifies it as failure.
- **Unknown/quarantine**: kill may have been delivered but domain emptiness is
  unconfirmed, or population observation is unavailable, or persistence
  leaves cleanup state unconfirmed.
- **Refused**: absent/mismatched authority or identity.
- **Confirmed**: identity-verified domain empty and all required cleanup
  evidence/persistence conditions are met.

The current aggregation maps any `ComponentState.Failed` to
`ReconcileOutcome.Failed` (`Reconciler.cs:429-436`); Unknown is reached only
via Unknown components (`:437-442`) or missing persistence (`:450-459`). The
proposed amendment is to represent an attempted-but-unconfirmed stop as an
Unknown component state (or an explicitly equivalent conservative outcome),
not as operation Failed, while keeping true kill/provider failures as Failed.
Persistence uncertainty after otherwise confirmed empty cleanup is also
Unknown/quarantine, not a fabricated success or an operation failure. A
confirmed domain stop with unrelated cleanup debt remains component-wise debt,
not aggregate CleanupConfirmed.

### No StopRecord after restart (A-C3-3)

A runtime stop can be accepted without a valid durable StopRecord when the
clock is unavailable, deadline arithmetic overflows, durable persistence fails,
or the journal is wedged. In that case:

- the caller may possess an independently authorized cleanup allowance;
- that allowance is **not** a StopRecord deadline, continuation, or reset;
- C2 has already consumed the release capability;
- process-local invalidation is not inherited; a fresh owner gets no capability;
- recovery must not recreate release authority or invent a stop deadline;
- absence of StopRecord does not authorize release;
- cleanup remains identity/authority checked; no RELEASE resend.

Planned named test:
`RuntimeAcceptedStopWithoutDurableRecordAfterRestart` (exact name may be
refined). It must assert no manufactured persisted deadline, no release
authority recreation, separate cleanup allowance, no resend and conservative
recovery.

### Domain already empty (A-C3-4)

Recommended: **Option B - preserve current kill-first behavior** and rewrite
the acceptance row to "Domain already empty: no unnecessary WAIT." The current
Reconciler issues the kill before querying population when DomainStop is unset
(`Reconciler.cs:217-248`). Option B is the smaller change, preserves the
existing cleanup safety sequence, and does not add a new pre-observation phase.
The contract must record that a kill operation can still be issued even when
the domain is already empty. Option A (pre-observation) is a larger production
behavior change and is not recommended for this contract.

### Expired-deadline human policy (not selected)

- **Policy A (recommended technical option):** an expired durable deadline
  authorizes the existing identity-checked kill plus one immediate population
  observation; no additional waiting allowance; if emptiness is unconfirmed,
  report Unknown/quarantine.
- **Policy B:** a separately specified bounded cleanup action/observation
  interval with its own authority, upper bound and explicit non-representation
  as the expired window.

Both preserve deadline immutability and avoid a new five seconds. The human
decision question is:

**Does an expired durable stop deadline authorize the existing
kill-plus-one-observation behavior as the accepted urgent-stop contract?**

No policy is selected or approved here.

### Tests to add under later authorization

Compatible/incompatible epoch; same/cross-boot; unavailable/overflow; exact
deadline/expired; old `Stopwatch.StartNew()` clock rejected; repeated
takeover; no reset; partial cleanup; no-record restart; already-empty kill
and no-unnecessary-wait; kill failure; delivered-but-populated; population
unknown/empty; persistence failure; identity refusal; component and aggregate
outcomes; no schema change.

### Human question

Subject to the policy choice, approve corroborated epoch deadline consumption,
fail-closed clock uncertainty and corrected Reconciler classification?
**PENDING HUMAN.**
