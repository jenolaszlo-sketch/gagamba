# WP05 D6.0 C3 - R27 Persisted Stop-Deadline Consumption

Status: **PROPOSED - NOT APPROVED**

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
- Domain already empty: no unnecessary stop; record/verify.
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
3. Expired deadline triggers zero-wait urgent stop.
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
