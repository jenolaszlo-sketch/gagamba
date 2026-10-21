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

## D6.2 revision - R1 structured outcomes and R2 clock provenance

Status: **PROPOSED - NOT APPROVED**
Design status: **DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW**
C3 policy status: **C3 ZERO-WAIT POLICY - PENDING HUMAN**
Human contract approval: **PENDING HUMAN**

This section resolves only GLM D6.1-R1 and D6.1-R2. It does not select the
expired-deadline policy, approve C3, authorize implementation or qualify
release. The D6.1 text above remains historical; this section explicitly
supersedes its `MonotonicEpoch.Qpc` convention and its incomplete
Failed/Unknown aggregation description.

### R1 selected mechanism - typed internal stop outcome (R1-A)

The selected mechanism is **R1-A: extend the existing internal `StopOutcome`
record struct** with typed kill-dispatch and population-observation fields.
The existing bounded wait is retained; the Reconciler classifies from typed
fields, never from `Error` text.

Rejected alternative: **R1-B separate kill and observation operations**. It
would move the bounded-wait policy into the Reconciler, duplicate the existing
kill/poll sequence, widen the test surface, and make the already-empty and
zero-wait policy paths harder to keep equivalent. R1-A is smaller, preserves
the single implementation of the bounded loop, and can express Policy A when
the human holder selects it.

Proposed internal shape (design only; not implemented):

```csharp
internal enum KillDispatchOutcome : byte
{
    NotAttempted = 0,
    Failed = 1,
    Accepted = 2,
    Unknown = 3,
}

internal enum StopPopulationObservation : byte
{
    NotObserved = 0,
    Unknown = 1,
    Populated = 2,
    Empty = 3,
}

internal readonly record struct StopOutcome(
    KillDispatchOutcome Kill,
    StopPopulationObservation Population,
    bool TimedOut,
    string Error)
{
    internal bool Stopped =>
        Kill == KillDispatchOutcome.Accepted
        && Population == StopPopulationObservation.Empty;
}
```

`Error` remains diagnostic detail only. No classification depends on its
text. `Stopped` remains a derived convenience and cannot become true from
`TimedOut`, `Failed` or `Unknown`.

Source mapping:

- `DomainLease.StopAndAwaitEmpty` currently collapses kill-write failure and
  population-observation failure into `Stopped=false, TimedOut=false`
  (`Lifecycle/DomainLease.cs:11-18,108-143`). The proposed implementation maps
  `WriteKill` to `Kill`, `DomainPopulationResult` (`Lifecycle/RestrictedDomain.cs:15-21`;
  native read at `Native/CgroupInterop.cs:174-201`) to `Population`, and the
  deadline poll to `TimedOut`.
- The R12 native backend maps a definitive `DomainIoResult` failure to
  `Kill=Failed`. `Kill=Unknown` is reserved for a backend that cannot prove
  dispatch; it is exercised by a test-only/future backend fixture and does not
  add production extensibility now.
- The zero-timeout call already performs kill then one population read
  (`DomainLease.cs:116-142`); with typed fields it can express Policy A if the
  human selects it. Policy A is **not selected** by D6.2.

Production symbols that would eventually change: `Lifecycle/DomainLease.cs`
(`StopOutcome`, `StopAndAwaitEmpty`, factory helpers), and
`Recovery/Reconciler.cs` (component mapping and aggregation). All are internal;
there is no public API or durable-schema change. Tests that construct
`StopOutcome.Success`/`Timeout`/`Failure` or match `Stopped`/`TimedOut` must be
updated at implementation time. No compatibility promise is made for internal
types.

### R1 component outcome mapping

The Reconciler maps the typed stop outcome before aggregation. When the
bounded stop performed no population observation (`Kill=Failed` or
`Kill=NotAttempted`), the existing `ObservePopulation` fallback may
independently establish `DomainEmpty = Confirmed` only from a trusted,
authorized observation of Empty. A Populated, Unknown, unavailable or
inconclusive observation leaves `DomainEmpty = Unknown`. The fallback never
changes `DomainStop` or implies successful kill dispatch, and it must not
upgrade an attempted-but-populated or observation-unknown stop into success.
Existing journal-finalization requirements and aggregate precedence apply
unchanged.

| Typed stop outcome / fallback condition | `DomainStop` | `DomainEmpty` | Rule |
|---|---|---|---|
| `Kill=NotAttempted`, trusted fallback observes Empty | `Unknown` | `Confirmed` | Confirms emptiness only; no successful kill dispatch or termination is inferred |
| `Kill=NotAttempted`, fallback observes Populated | `Unknown` | `Unknown` | No empty inference or new kill attempt |
| `Kill=NotAttempted`, fallback unavailable or inconclusive | `Unknown` | `Unknown` | No empty assumption |
| `Kill=Failed` | `Failed` | `Confirmed` if a diagnostic observation is empty, otherwise `Unknown` | Definitive kill operation failure; aggregate remains `Failed` even if the domain is later observed empty |
| `Kill=Unknown` | `Unknown` | `Unknown` | Dispatch not proven; no trusted resolution adopted in D6.2 |
| `Kill=Accepted`, `Population=Empty` | `Confirmed` | `Confirmed` | Successful bounded wait established emptiness |
| `Kill=Accepted`, `Population=Populated`, `TimedOut=true` | `Unknown` | `Unknown` | Kill attempted but emptiness unconfirmed; no second wait/upgrade |
| `Kill=Accepted`, `Population=Unknown` | `Unknown` | `Unknown` | Population observation unavailable |
| `Kill=Accepted`, `Population=Populated`, `TimedOut=false` | `Unknown` | `Unknown` | Not produced by the current loop; defense-in-depth |

Staging and domain-removal mapping is unchanged: a definitive cleaner/removal
operation failure is `Failed`; a replacement/mismatch refusal or missing
authority is `Unknown`; confirmed removal is `Confirmed`. A component-level
`Failed` is never converted to `Unknown` merely because another component or
journal finalization is uncertain.

### R1 JournalFinalization aggregation

`JournalFinalization` is a classification component, not a new durable field.
It is `Confirmed` when the final persistence attempt succeeds (or the required
progress is already present under the writer lease). It is `Unknown` when no
writer lease is supplied, a component progress persistence attempt fails, or
the final persistence attempt fails. It is **not** `Failed` for a
persistence-only uncertainty after otherwise confirmed cleanup.

The current `Reconciler` sets `finalizationFailed` inside the component
progress paths and again at the final write, and the aggregate maps that flag
to `Failed` (`Recovery/Reconciler.cs:232-236,429-436,450-459,465-471`). The
proposed change is:

1. Record component progress persistence failures as
   `journalFinalizationUnknown` with detail, not as the component's own
   operation failure.
2. Compute a `JournalFinalization` outcome (`Confirmed` or `Unknown`) before
   the aggregate decision; append its `ComponentOutcome` beside the existing
   component rows.
3. Use the following precedence:

```text
if any required component is Failed:
    aggregate = Failed          // preserve the known negative operation evidence
else if any required component is Unknown
     or JournalFinalization is Unknown:
    aggregate = QuarantinedUnknown
else:
    aggregate = CleanupConfirmed
```

4. On a mixed result, retain both details: the aggregate `Failed` detail names
   the component operation failure; the `JournalFinalization` row separately
   records the persistence uncertainty. A persistence-only failure with all
   components confirmed is `QuarantinedUnknown`, never a fabricated
   `CleanupConfirmed` and never `Failed` by itself.

This is the required `JournalFinalization` acceptance row. It directly
addresses `Reconciler.cs:465-471`; it is not implied by changing only the
DomainStop mapping at `:232-236`.

### R1 complete aggregate truth table

| DomainStop | DomainEmpty | Staging | DomainRemoval | JournalFinalization | Aggregate |
|---|---|---|---|---|---|
| `Failed` | any | any | any | any | `Failed` |
| any | any | `Failed` | any | any | `Failed` |
| any | any | any | `Failed` | any | `Failed` |
| `Unknown` | any | any | any | any | `QuarantinedUnknown` |
| any | `Unknown` | any | any | any | `QuarantinedUnknown` |
| any | any | `Unknown` | any | any | `QuarantinedUnknown` |
| any | any | any | `Unknown` | any | `QuarantinedUnknown` |
| `Confirmed` | `Confirmed` | `Confirmed` | `Confirmed` | `Unknown` | `QuarantinedUnknown` |
| `Confirmed` | `Confirmed` | `Confirmed` | `Confirmed` | `Confirmed` | `CleanupConfirmed` |
| Refused by cross-boot/identity | — | — | — | — | `Refused`, no destructive action |
| No live authority (planner) | — | — | — | — | `QuarantinedUnknown`, no destructive action |

Additional rules:

- A known operation failure plus a persistence failure is `Failed`.
- An otherwise successful cleanup whose only failure is durable finalization
  is `QuarantinedUnknown`.
- Cross-boot and live-lease identity mismatch are `Refused` before destructive
  action (`Recovery/Reconciler.cs:75-97,224-228`). Restart-only/no surviving
  live authority is `QuarantinedUnknown` per the existing planner, not
  `Refused`. A missing staging cleaner is `Unknown` debt, not `Refused`.
- `Unknown` is a distinct evidence classification, not a generic error bucket.
- No fabricated completion bits: when finalization is `Unknown`, the durable
  cleanup state is not reported as `Confirmed`.

### R1 required deterministic tests

Use the existing test seams: `FakeDomainBackend` (`WriteKill`,
`ReadPopulation`, `StatDomain`, `LookupChild`, `RemoveChild`),
`FakeDomainClock`, and `FakeJournalStorage` (`FailAt`/`Fault`). Assert typed
outcomes and aggregate states; use `Error` only as diagnostic text. Do not use
sleeps or error-string parsing as the oracle.

| Test | Fault/setup | Typed evidence | Required component/aggregate assertion |
|---|---|---|---|
| R1-T1 kill-write failure | `KillFault` | `Kill=Failed`, `Population=NotObserved` | DomainStop `Failed`; aggregate `Failed`; no `CleanupConfirmed`; no fabricated bits |
| R1-T2 kill accepted, remains populated | `KillAccepted=true`, `Populated=true` | `Kill=Accepted`, `Population=Populated`, `TimedOut=true` | DomainStop `Unknown`, DomainEmpty `Unknown`; aggregate `QuarantinedUnknown` |
| R1-T3 kill dispatch unknown | Test-only backend returning unknown dispatch | `Kill=Unknown` | DomainStop `Unknown`; aggregate `QuarantinedUnknown`; no success inference |
| R1-T4 population unavailable | `ReadFault` | `Kill=Accepted`, `Population=Unknown` | DomainStop `Unknown`, DomainEmpty `Unknown`; aggregate `QuarantinedUnknown` |
| R1-T5 confirmed empty | `KillAccepted=true`, `Populated=false` | `Kill=Accepted`, `Population=Empty` | Both domain components `Confirmed`; finalization `Confirmed`; `CleanupConfirmed`; exact durable bits |
| R1-T6 stop progress persistence failure | `FakeJournalStorage` fault on stop progress write | Domain components confirmed; `JournalFinalization=Unknown` | Aggregate `QuarantinedUnknown`, not `Failed`; no fabricated confirmed record |
| R1-T7 final persistence failure after confirmed empty | Fault on final write | All components confirmed; `JournalFinalization=Unknown` | Aggregate `QuarantinedUnknown`, not `Failed`, not `CleanupConfirmed` |
| R1-T8 genuine reconciliation operation failure | Staging cleaner or removal returns operation failure | Component `Failed` | Aggregate `Failed`; negative evidence preserved |
| R1-T9 invalid cleanup authority | Cross-boot or live-lease identity mismatch; separate no-live-authority case; missing staging cleaner | Planner refusal / unknown staging authority | Cross-boot/identity mismatch `Refused`; restart-only no-live-authority `QuarantinedUnknown`; missing cleaner `Unknown` debt; no destructive action where authority is absent |
| R1-T10 identity mismatch | `StatDomain`/lookup mismatch | `VerifyIdentity` refusal | `Refused` before stop; no destructive action |
| R1-T11 mixed component results | e.g. Staging `Failed` + finalization `Unknown`; Staging `Unknown` + finalization `Unknown` | Mixed | `Failed` when any component Failed; otherwise `QuarantinedUnknown`; both details retained |
| R1-T12 repeated reconciliation after uncertain finalization | First run finalization fault; repeat with same record | Same components, finalization retried | No deadline/cleanup reset; no fabricated bits; idempotent `QuarantinedUnknown` or confirmed only after real successful finalization |
| R1-T13 kill not attempted, trusted fallback empty | Dedicated D6.4 test-build fixture; matching live lease and identity; v3 journal baseline has `Cleanup=InProgress` and `DomainRemoval` only (no allocations; `DomainStop`/`DomainEmpty` clear); healthy writer seeded before reconciliation; fake backend `Populated=false`, `ReadFault=false` | Synthetic `Kill=NotAttempted`, stop population `NotObserved`; real fallback reads `DomainPopulationResult.Emptied` | `DomainStop=Unknown`, `DomainEmpty=Confirmed`; aggregate `QuarantinedUnknown`; finalization `Confirmed`; durable `CompletedCleanup` adds only `DomainEmpty` (`DomainRemoval` preserved, `DomainStop` clear); `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed` |
| R1-T14 kill not attempted, fallback populated | Same matched v3 `Cleanup=InProgress` baseline, live lease and healthy writer; fake backend `Populated=true`, `ReadFault=false` | Synthetic `Kill=NotAttempted`, stop population `NotObserved`; real fallback reads `DomainPopulationResult.Populated` | `DomainStop=Unknown`, `DomainEmpty=Unknown`; aggregate `QuarantinedUnknown`; finalization `Confirmed`; durable `CompletedCleanup` remains `DomainRemoval` only; `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed` |
| R1-T15 kill not attempted, fallback unavailable or inconclusive | Same matched v3 `Cleanup=InProgress` baseline, live lease and healthy writer; fake backend `ReadFault=true` (live lease remains present and matching) | Synthetic `Kill=NotAttempted`, stop population `NotObserved`; real fallback reads `DomainPopulationResult.Unknown` | `DomainStop=Unknown`, `DomainEmpty=Unknown`; aggregate `QuarantinedUnknown`; finalization `Confirmed`; durable `CompletedCleanup` remains `DomainRemoval` only; `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed` |

**D6.4 test-only fixture specification (proposed; not R12 behavior).** The exact fixture, build isolation, baseline seeding, durable-bit assertions, and negative controls are defined in `docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md`. In each case the test keeps the normal recovery planner and `VerifyIdentity` checks, substitutes only the typed stop result at the Reconciler stop-result boundary, then exercises the real `liveLease.ObservePopulation()` fallback against `FakeDomainBackend.ReadPopulation()`. The only synthetic stop input is `KillDispatchOutcome.NotAttempted` plus `StopPopulationObservation.NotObserved`; the fallback population result is injected separately. This specification targets a future isolated test-build variant, not the production assembly. R1-T1 through R1-T12 and the accepted C3 outcome mapping are unchanged.

No tests are implemented by D6.2 or D6.3; D6.4 specifies only the future deterministic fixture.

### R2 trusted clock construction and epoch

Selected mechanism: a **sealed internal production domain clock** with a
constant basis, built directly on `Stopwatch.GetTimestamp()` and the exact
`OwnerClock` conversion. The Reconciler receives a narrow clock basis and
rejects any unsupported epoch or frequency before deadline arithmetic.

Proposed internal shape (design only; not implemented):

```csharp
internal enum MonotonicEpoch : byte
{
    Unknown = 0,
    Qpc = 1, // Stopwatch.GetTimestamp() basis with OwnerClock conversion
}

internal readonly record struct MonotonicClockBasis(
    MonotonicEpoch Epoch,
    long Frequency);

internal interface IDomainClock
{
    MonotonicClockBasis Basis { get; }
    long NowNanoseconds { get; }
    void Delay(TimeSpan duration);
}
```

The sealed production implementation, proposed name `StopwatchDomainClock`:

- obtains `NowNanoseconds` directly from `Stopwatch.GetTimestamp()`;
- uses the shared conversion
  `(t / Stopwatch.Frequency * 1_000_000_000L) +
   (t % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency)`,
  identical to `OwnerClock` (`Lifecycle/OwnerPreparationTransaction.cs:21-32`);
- returns `Basis => new(MonotonicEpoch.Qpc, Stopwatch.Frequency)`;
- has no constructor parameter or factory for an epoch label;
- does not rebase to object-construction time;
- is the only production clock used for durable StopRecord deadline arithmetic.

Structural production constraint: the Reconciler production constructor accepts
the sealed production clock type (or a sealed trusted-clock capability minted
by it), not an arbitrary `IDomainClock`. The existing internal
`IDomainClock` constructor is retained only as an explicitly test-only seam for
deterministic fixtures; production composition cannot pass a foreign clock
without a source change and review. The `Basis` check remains defense-in-depth
for test clocks. A false `Qpc` label on a test clock is test-code deception,
not a production path; a production adapter cannot launder a construction-
relative clock through the open interface because the production constructor
does not accept that interface.

To avoid duplicated subtly different conversions, the proposed implementation
extracts one internal helper (or equivalent single formula) used by
`OwnerClock`, `LaunchDomainClock` (`Launch/BubblewrapAdapter.cs:510-521`) and
the sealed production domain clock. `LaunchDomainClock` already uses the same
primitive/conversion and is compatible. `OwnerDomainClock`
(`Lifecycle/OwnerPreparationTransaction.cs:34-39`) uses construction-relative
`Stopwatch.StartNew()` values, is incompatible, and has no identified
production caller in R12; it is dead code, not an active production failure.
It must not be used for deadline arithmetic.

Definition of `MonotonicEpoch.Qpc`:

> `Qpc` names the epoch of `Stopwatch.GetTimestamp()` with the exact nanosecond
> conversion and `Stopwatch.Frequency` basis used by `OwnerClock`. On Windows
> that primitive is associated with QPC; on Linux it is the .NET Stopwatch
> monotonic source (CLOCK_MONOTONIC-backed), not a literal Windows QPC device.
> Cross-process comparability is valid only for the same supported timestamp
> basis and a compatible boot context. No raw cross-platform QPC comparability
> or operating-system qualification follows from the name.

The Reconciler checks `Basis.Epoch == MonotonicEpoch.Qpc` and
`Basis.Frequency == Stopwatch.Frequency` before any subtraction. If the clock
is unavailable, the epoch is incompatible/unknown, the frequency differs,
`now <= 0`, or the checked derivation overflows, it fails closed to
`QuarantinedUnknown`, performs no fabricated deadline arithmetic, and never
creates a fresh five-second allowance. A test-only or future incompatible
clock is rejected structurally by its declared basis; a false `Qpc` label on a
test clock is test-code deception, not a production path, and is covered by
the sealed production implementation plus the negative fixture/source
invariant.

Production symbols that would eventually change: `Lifecycle/DomainLease.cs`
(`IDomainClock` basis), `Lifecycle/OwnerPreparationTransaction.cs`
(`OwnerClock`/`OwnerDomainClock` conversion ownership),
`Launch/BubblewrapAdapter.cs` (`LaunchDomainClock`), `Recovery/Reconciler.cs`
(production constructor and deadline arithmetic), plus the new sealed clock and
shared conversion helper. All are internal; there is no public API or durable
schema change. Test clocks that implement `IDomainClock` must declare an
explicit basis. No compatibility promise is made for internal types.

Durable deadline derivation when a StopRecord exists and the basis is trusted:

```text
remaining = max(0, StopDeadlineNanoseconds - corroborated now)
```

with checked/range-validated units. The same absolute durable deadline is
used across repeated reconciliation, a new Reconciler, a new same-boot
process, and partial cleanup resume. No caller can extend or replace the
StopRecord; `OwnershipValidation.cs:145-175` already requires
`acceptedAt > 0`, `deadline > 0` and `deadline == acceptedAt + 5_000_000_000`.
Boot identity refusal remains in `RecoveryPlanner.Plan`
(`Reconciler.cs:75-97`).

If the human selects Policy A, an expired deadline may use
`StopAndAwaitEmpty(TimeSpan.Zero, clock)` (identity-checked kill + one
immediate population observation) without a renewed wait. If Policy B is
selected, a separately specified bounded interval applies. D6.2 selects
neither; `C3 ZERO-WAIT POLICY - PENDING HUMAN` remains.

### R2 required deterministic tests

| Test | Setup | Required assertion |
|---|---|---|
| R2-A compatible production epoch | StopRecord written through the `OwnerClock` conversion; consume with the sealed clock | Correct `remaining`; same absolute deadline; no rebase |
| R2-B construction-relative epoch | Fixture modeled on `Stopwatch.StartNew()` honestly declares its incompatible epoch | Rejected before subtraction; no fabricated remaining; `QuarantinedUnknown` |
| R2-C false `Qpc` metadata | Source-level constructor/type invariant and focused negative fixture | Enum label alone is insufficient; sealed production clock has no injected epoch; Reconciler production constructor accepts the sealed type; a false test-clock label uses the test-only seam and is not a production path |
| R2-D cross-boot | Current boot differs from the record | Existing `RefuseCrossBoot` remains effective |
| R2-E invalid conversion | Frequency mismatch, overflow, nonpositive timestamp, unavailable clock, incompatible epoch | All fail closed; no fabricated remaining; no fresh window |
| R2-F repeated reconciliation | New Reconciler, new same-boot process, repeated/partial cleanup | Original durable deadline never moves forward; no reset |

Frequency/conversion mismatch is checked through the declared `Basis` and the
sealed production clock's constant `Stopwatch.Frequency`; no production
runtime extensibility is added merely to inject a mismatch. No clocks or tests
are implemented by D6.2.

### C3 D6.2 status

- R1 design text: **ADDRESSED - AWAITING INDEPENDENT REVIEW**.
- R2 design text: **ADDRESSED - AWAITING INDEPENDENT REVIEW**.
- C3 expired-deadline policy: **C3 ZERO-WAIT POLICY - PENDING HUMAN**.
- C3 contract: **PROPOSED - NOT APPROVED; HUMAN DECISION PENDING**.
- No production clock, Reconciler or `DomainLease` change is authorized.
