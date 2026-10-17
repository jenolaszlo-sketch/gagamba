# ADR 0004: WP05 durable release schema amendment

Date: 2026-10-17 (Asia/Manila)

Status: **Proposed — design tranche only. No production implementation, migration,
coordinator change, release execution, target execution or WP05 qualification is
authorized by this document.** It is input to independent review.

Branch: `qualification/ar7-feasibility`
Baseline after blocker preservation: `c48b4cd193616c7a2777ac664457183002f845a1`
Authoritative architecture: [ADR 0003](0003-ar7-linux-first-restricted-execution.md)
Authoritative delivery plan: [Linux M1 implementation plan](../plans/gagamba-linux-m1-implementation-plan.md)
WP05 blocker evidence: [WP05 plan binding and schema blocker](../evidence/linux-m1/wp05-plan-binding-2026-10-17.md)

## 1. Problem

WP05 requires a durable `ReleaseIntent` carrying the exact preparation identity
and absolute deadlines before entering the release gate (plan lines 147, 209,
342), and recovery must classify release outcomes after owner loss (plan lines
159–165, 315). The accepted ownership journal (`OwnershipSnapshot`, version 2)
represents only the pre-release preparation lifecycle. It has no release intent,
commit, delivery, stop-winner or deadline state, and it explicitly excludes
release credentials.

A second release journal is rejected: it would create two durable authorities for
one execution, would not be covered by the accepted G2 writer fence, and would
contradict the single-snapshot recovery model (plan lines 332, 347). The only
acceptable extension is a versioned field addition to the existing ownership
snapshot, written under the existing `JournalWriterLease`.

**Decision:** propose `OwnershipSnapshot` version 3 with an optional
`ReleaseRecord` and an optional `StopRecord`. Version 2 remains decodable. The
writer emits version 2 while no new field is present and version 3 when a release
or stop transition is first recorded. The release capability, release writer and
all native handles remain outside the journal; the new fields are evidence, never
authority.

## 2. Existing version-2 state

`OwnershipSnapshot` (`src/Gagamba.Execution.Linux.M1/Recovery/OwnershipRecord.cs:69–89`)
contains:

| Field | Meaning |
|---|---|
| `Version` | `2`; the codec rejects every other value. |
| `Sequence` | Monotonic journal sequence; `WriteNext` requires baseline + 1. |
| `ExecutionId`, `BootId` | Execution and boot identity. |
| `CgroupMountId`, `CgroupMountFsType`, `ParentDevice`, `ParentInode`, `DomainName`, `DomainDevice`, `DomainInode`, `LeaseGeneration` | Live domain identity. |
| `Writer` | `RecoveryScope` or `Owner`. |
| `Cleanup` | `NotStarted`, `InProgress`, `Confirmed`, `Failed`, `Unknown`. |
| `Stage` | `None`, `AllocationIntent`, `AllocationRegistered`, `Prepared`. |
| `StagingParentDevice`, `StagingParentInode` | Staging cleanup authority. |
| `Allocations` | Bounded object inventory. |
| `Preparation` | Optional `RecordedPreparation` (execution, issuer, generation, preparation IDs and manifest digest). |
| `CompletedCleanup` | Cleanup component bitmask. |

Version 2 has no release phase, release deadline, stop cause/time or delivery
state. `PreparationStage` ends at `Prepared`; the durable record does not have a
separate `Owned` state. WP04 accepted this pre-release record and its writer
fence ([WP03 evidence](../evidence/linux-m1/wp03.md:141–149)).

The journal is a bounded two-generation snapshot (`gen.a`/`gen.b`, `gen.tmp`,
`gen.seal`) with a SHA-256 checksum. A mutation requires a live
`JournalWriterLease`, the storage `flock`, a baseline sequence and exactly
`baseline + 1` (`OwnershipJournal.cs:358–416`). A failure reopens as the old
acknowledged generation or explicit uncertainty; it never promotes an
unacknowledged candidate. The writer fence is the accepted G2 fence.

## 3. Proposed version-3 schema

Version 3 is the version-2 payload followed by two optional tagged records. The
version byte is inside the checksummed payload.

```
OwnershipSnapshotV3 =
    version-2 fields
    ReleaseTag  : u8  0 = absent, 1 = Intent, 2 = Committed
    if ReleaseTag != 0:
        IntentAtNanoseconds      : u64
        SetupDeadlineNanoseconds : u64
        OverallDeadlineNanoseconds : u64
        if ReleaseTag == 2:
            CommittedAtNanoseconds : u64
            DeliveryResult         : u8
    StopTag : u8  0 = absent, 1 = present
    if StopTag == 1:
        StopAcceptedAtNanoseconds : u64
        StopCause                 : u8
        StopDeadlineNanoseconds   : u64
```

`DeliveryResult` is a new durable enum. It does not renumber the WP01 runtime
`ReleaseSendResult`:

| Value | Name | Meaning |
|---:|---|---|
| 0 | `NotAttempted` | Intent only; no release write is permitted yet. |
| 1 | `Delivered` | The single nonblocking write returned full 128-byte success. |
| 2 | `WouldBlock` | The single write returned `EAGAIN`. |
| 3 | `BrokenPipe` | The single write returned `EPIPE`. |
| 4 | `Partial` | The single write returned a short count. |
| 5 | `Unknown` | The write outcome was unexpected or could not be classified. |

`DeliveryResult=NotAttempted` is valid only with `ReleaseTag=Intent`.
`ReleaseTag=Committed` requires `DeliveryResult` in `Delivered`, `WouldBlock`,
`BrokenPipe`, `Partial` or `Unknown`. There is no separate durable
`ReleaseDelivered` state: the plan's semantic `ReleaseDelivered` observation is
the pair `ReleaseTag=Committed` + `DeliveryResult=Delivered`.

`StopCause` uses the existing `StopCauseKind` values
(`CallerStop`, `CallerLoss`, `SetupExpired`, `SetupFault`, `OutputOverflow`,
`OutputReadFailure`, `RecoveryTakeover`, `TransportFailure`). The first accepted
cause is recorded; later causes are diagnostics and are not added to this
schema.

### 3.1 Field rationale

| Field | Why recovery needs it | Writer | Immutable | In identity | Security-sensitive |
|---|---|---|---|---|---|
| `ReleaseTag=Intent` | Fixes the durable safety boundary. Before an acknowledged intent, release is definitely impossible; after it, recovery must assume release may have won. | Owner, with its live `JournalWriterLease`, after in-memory Owned checks, before the gate. | The intent identity and deadlines are immutable once written. | Uses the existing `Preparation` identity; the tag itself is not identity. | Yes, as evidence. It is not a capability. |
| `ReleaseTag=Committed` | Proves the gate committed and the single write was attempted when the owner survived to record it. Its absence does **not** prove non-commit. | Owner, outside the gate, after the gate's commit/consume/write. | Once written. | No; it refers to the same `Preparation`. | Yes, as evidence. |
| `IntentAtNanoseconds` | Evidence ordering and post-restart timeout context. | Owner. | Yes. | No. | Low. |
| `SetupDeadlineNanoseconds` | The gate's release-eligibility deadline; recovery must know whether release was still permitted. | Owner, copied from the request/preparation context. | Yes. | No (runtime authority, not manifest identity). | Yes, authority-relevant. |
| `OverallDeadlineNanoseconds` | Post-release execution/stop deadline; also carried in the release frame. | Owner. | Yes. | No. | Yes, authority-relevant. |
| `CommittedAtNanoseconds` | Evidence of when the post-gate observation was durable, not the exact gate instant. | Owner. | Yes. | No. | Low. |
| `DeliveryResult` | Narrows whether the single write was accepted; does not prove coordinator consumption or target execution. | Owner, from the one write's return. | Yes. | No. | Yes, result-relevant. |
| `StopTag` | Records that stop won arbitration or was accepted, even when no release exists. | Owner or, after pidfd-proven owner death and fenced takeover, the recovery scope. Never the target. | First acceptance is immutable; later causes are not recorded here. | No. | Yes, arbitration evidence. |
| `StopAcceptedAtNanoseconds` | First-stop time and deadline evidence. | Owner/recovery writer. | Yes. | No. | Low. |
| `StopCause` | Distinguishes cancellation/loss/expiry/setup/output/recovery/transport causes for truthful results. | Owner/recovery writer. | Yes. | No. | Yes, result-relevant. |
| `StopDeadlineNanoseconds` | Bounds teardown after restart without inventing a new allowance. | Owner/recovery writer. | Yes. | No. | Yes, authority-relevant. |

No release nonce, release token, decision UUID, manifest copy, generation copy,
execution copy or final outcome is added. The release identity is the existing
`RecordedPreparation` (`ExecutionId`, `IssuerId`, `GenerationId`,
`PreparationId`, `ManifestDigest`) in the same snapshot. The release record must
never exist when `Preparation` is absent or `Stage` is not `Prepared`. A release
record is not a bearer capability and cannot be used by recovery to send a
release.

## 4. Durable state machine

The durable lifecycle is layered beside the unchanged preparation stage; it does
not replace `Stage=Prepared`.

| State | Durable shape | Meaning |
|---|---|---|
| R0 | `Release=null`, `Stop=null` | No release was requested. Target remains held. Release is definitely impossible. |
| R1 | `Release=Intent`, `Stop=null` | Release was admitted for arbitration. The gate has not yet been observed durably. The single write may or may not have happened. |
| R2 | `Release=Committed`, `DeliveryResult=<value>` | The owner recorded, after the gate, that the gate committed and the single write was attempted with this result. |
| S0 | `Stop` present, `Release=null` | Stop was accepted before any release intent. Release is definitely impossible. |
| S1 | `Stop` present, `Release=Intent` | A stop coexists with an earlier or concurrent intent. Recovery must not claim release was impossible. |
| S2 | `Stop` present, `Release=Committed` | Release won before a later stop, or the stop was accepted while release was already committed. Release history is preserved; stop terminates/reconciles. |

Transitions:

| Transition | Precondition | Writer and durable write | Successors | Forbidden |
|---|---|---|---|---|
| R0 → R1 | In-memory `Owned`; live capability; exact issuer/generation/preparation/digest match; authorization accepted; not expired; no accepted stop; live writer lease. | Owner writes a v3 intent snapshot outside the gate. | R1; or S0/S1 if a stop wins during/after the write. | Intent without `Prepared`/`Preparation`; intent after a durable stop that already won; intent after expiry. |
| R1 → R2 | Enter the single arbiter gate; recheck clock, setup/overall expiry, stop, caller/recovery liveness, identity, prerequisites and capability; consume capability; change `Owned → ReleaseCommitted`; attempt exactly one nonblocking 128-byte write. | Owner writes the v3 committed snapshot **after leaving the gate**, recording `DeliveryResult`. | R2; or S1 if the gate refused because stop won; no release write then. | A committed record before the gate/write; a second write; a committed record without an intent; a committed record with `NotAttempted`. |
| R0/R1/R2 → S0/S1/S2 | Stop accepted under the single arbiter; first cause establishes a stable stop cause; stop deadline computed. | Owner writes the stop snapshot under its live lease; after owner death, recovery may write it only after pidfd-proven exit and fenced takeover. | Stopping/cleanup; terminal. | Stop record written by a stale/competing writer; release after a durable stop that already won; rewriting R1/R2 to R0. |
| R2 → terminal | No new release state. | Cleanup snapshots preserve `Release` and `Stop` unchanged while `Cleanup`/`CompletedCleanup` progress. | Terminal. | Clearing `Release`; inferring terminal success from cleanup. |

The durable record cannot distinguish an R1 that was refused by the gate from an
R1 whose gate committed but crashed before R2. That is intentional: the accepted
plan's owner-loss rule treats any durable intent as `MayHaveExecuted`. R1 must
therefore never be reported as `ReleaseDisposition.NotCommitted` after owner
loss.

## 5. Release linearization point

Two different linearization points must not be conflated:

1. **Runtime arbitration linearization:** the in-gate `Owned → ReleaseCommitted`
   state change plus capability consumption. A stop accepted in the gate before
   this change forbids release forever. A stop accepted after it may terminate
   and reconcile the domain but cannot rewrite the release history. This remains
   exactly the plan's arbiter rule (plan lines 207–215).
2. **Durable safety linearization:** the acknowledged `ReleaseIntent`. Before
   that record, release is definitely impossible. After it, recovery must
   conservatively assume release may have committed and/or been written. The
   durable `ReleaseCommitted` record does not move this boundary; it only proves
   the gate committed when the record exists.

`ReleaseCommitted` means: the single arbiter consumed the preparation and won
release arbitration; the capability is consumed; one release write was
attempted. It does **not** mean:

- the write was accepted (`DeliveryResult` decides that);
- the coordinator consumed the frame;
- the target executed;
- the execution result is known.

`ReleaseDelivered` is not a durable state. `ReleaseTag=Committed` +
`DeliveryResult=Delivered` means only that the one nonblocking write returned
full success. It does not prove coordinator consumption or target execution.

If a reviewer requires a durable commit record written *before* the release
write, that requires either journal I/O inside the gate (forbidden by plan line
207) or a second durable decision step outside the gate that can diverge from
the stop/expiry checks. That would be an architecture change, not this schema
amendment. This design keeps the accepted ordering.

## 6. Transport ordering

The compliant order is:

1. Validate Owned prerequisites and authorization against the live capability.
2. Write and durably flush `ReleaseIntent` **outside** the gate. Stop may win
   during this write. A failed/ambiguous flush forbids release and requests stop.
3. Enter the single gate. Re-read the clock, setup/overall expiry, stop flag,
   caller/recovery liveness, exact identity, prerequisite completion and
   capability state.
4. If any check fails: consume/abort the capability without sending release; do
   not write a committed record; write/retain the stop record if a stop won.
5. Otherwise: change `Owned → ReleaseCommitted`, consume the capability, and
   attempt exactly one nonblocking 128-byte write **inside the gate** before
   leaving it. `Delivered` means the write returned full success; `EAGAIN`,
   `EPIPE`, a short count or an unexpected return requests stop and is recorded
   as a conservative delivery failure. There is no retry.
6. Outside the gate, write the committed snapshot with the delivery result. This
   write is evidence, not a prerequisite for the release that already happened.
   If it fails, release remains committed and the latest durable record is the
   intent; recovery stays conservative and must not resend.

The capability is consumed in memory at step 5. Consumption is not durable.
After owner death, the durable intent/committed record and the absence of a live
capability are authoritative: recovery never reconstructs or reuses the
capability. There is one source of truth for release authority (the live
owner-local capability) and one source of truth for durable release evidence
(the journal snapshot). They cannot disagree in a way that authorizes a second
release.

While the owner is live, a duplicate release request resolves against the
retained decision and performs no second write. After owner loss, recovery has
no capability and never serves a release request; a new execution must start a
new preparation with a new identity. The durable record is authoritative for
classification, never for re-authorization.

## 7. Crash semantics

| Crash boundary | Latest durable state | May release have been written? | Recovery action | Result |
|---|---|---|---|---|
| Before intent | R0 | No. Release was never admitted. | Stop domain, identity-checked cleanup. | `ReleaseDisposition.NotCommitted`; root `NotStarted`; `FailedToEstablish` or `CancelledBeforeRelease` by cause. |
| After intent, before gate | R1 | Unknown. The gate may not have run, but the durable record cannot prove it. | Kill whole domain, no release, no resend, cleanup. | `MayHaveExecuted`; exec/root `Unknown` unless durable trusted evidence. |
| Inside gate before commit | R1 | Unknown. Durable state is identical to the previous row. | Same. | Same. |
| After in-memory commit, before write | R1 | Unknown. Physical no-write is not durably provable. | Same. | Same. |
| After write, before committed record | R1 | Unknown; delivery also unknown. | Same; no resend. | `MayHaveExecuted`; delivery unknown. |
| After committed record | R2 + delivery result | Yes; release won. Delivery is known only at the write-result level. | Kill whole domain, no resend, cleanup. | `MayHaveExecuted`; delivery per record; exec/root `Unknown` absent trusted status. |
| Stop before intent | S0 | No. | Stop/cleanup. | `NotCommitted`; `CancelledBeforeRelease`/`FailedToEstablish`; root `NotStarted`. |
| Stop after intent, before commit | S1 | Unknown. | Kill/cleanup; no resend. | `MayHaveExecuted`; stop may still terminate/reconcile. |
| Stop after commit | S2 | Yes. | Stop/cleanup; no history rewrite. | Release `MayHaveExecuted`; outcome `Stopped` or `Unknown` by evidence. |
| Malformed/unsupported record | Unknown | Cannot establish. | Quarantine/Unknown; no destructive adoption without live authority. | `Unknown`; never success or confirmed-not-executed. |

Physical possibility and durable proof are distinct. A crash after a durable
intent but before any write may physically leave the target unexecuted, but the
durable contract cannot prove no write occurred. The accepted plan chooses
possible false-positive `MayHaveExecuted` over duplicate release. Recovery never
sends a release to resolve the uncertainty.

## 8. Stop/release arbitration

- The single arbiter gate is the only place where release and stop accept a
  winner. The stop linearization point is the in-gate acceptance that sets the
  stable first stop cause. The release linearization point is the in-gate
  `Owned → ReleaseCommitted` change. The first of those two in-gate events wins;
  release after an accepted stop is forbidden.
- R0 (no release, no stop) and a durable stop record with `Release=null` prove
  no release was requested; these are the durable shapes that support
  "definitely not released" after owner loss.
- `Release=Intent/Committed` plus a stop record never proves release was
  refused. Recovery conservatively assumes release may have won. This is
  required because the stop may have been accepted during the intent write or
  after the gate's release decision.
- A stop accepted after commit may stop the domain but cannot rewrite the
  release record to `null` or change the result to `CancelledBeforeRelease`.
- Recovery never decides the winner from timing inference or a timestamp
  comparison. It uses the durable presence of release and stop records and the
  plan's conservative rule.

## 9. Deadline semantics

| Deadline | Representation | Time base | Written by/when | Restart behavior |
|---|---|---|---|---|
| `SetupDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner, with `ReleaseIntent` | If intent exists, it is immutable evidence. Expiry does not undo intent/commit. If no intent, no release decision exists. |
| `OverallDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner, with `ReleaseIntent`; also in the 128-byte release frame | After release, governs execution/stop. Recovery stops/cleans at or after expiry; expiry never authorizes execution. |
| `StopDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner/recovery writer when the first stop is accepted | Recovery uses the remaining allowance for bounded stop; if expired, it proceeds with a bounded urgent stop/cleanup. |

Rules:

- All values are positive and `SetupDeadline < OverallDeadline`;
  `StopDeadline >= StopAcceptedAt`.
- A process-relative stopwatch value is never persisted. `CLOCK_MONOTONIC` is
  boot-relative; a reboot changes the boot ID and resets the clock. Cross-boot
  records remain non-authority and are quarantined/refused by the existing
  recovery predicate.
- A wall-clock change or WSL host clock change does not affect the monotonic
  basis. If the clock is unavailable/zero at a write or recovery decision, fail
  closed.
- Recovery never uses a deadline to extend execution or to resend release.

## 10. Compatibility and migration

| Question | Decision |
|---|---|
| Can v3 code read v2? | Yes. A v2 snapshot decodes as `Release=null`, `Stop=null`; all existing fields retain their v2 meaning. |
| Can a v2 Prepared execution be upgraded safely? | Yes, lazily, on the first v3 write, by the live issuer that still holds the capability. v2 has no release state and the WP04 production path had no release operation, so `Release=null` is truthful. A restarted process has no capability and cannot upgrade into release. |
| Is the upgrade lazy, explicit or prohibited? | Lazy at the writer boundary: the first write that needs `Release`/`Stop` emits v3; otherwise the writer emits v2. No bulk migration tool. |
| Can older v2 binaries read v3? | No. The v2 decoder must refuse version 3 and fail closed. Mixed-version operation is unsupported. |
| Must newer unsupported versions fail closed? | Yes. The v3 decoder refuses every version other than 2 or 3; no fallback, no best-effort decode. |
| Are persisted v2 records migrated? | Only on their next acknowledged write. Existing v2 records may remain v2 indefinitely if no WP05 transition occurs. |
| Can any v2 state not be migrated safely? | No. v2 has no release intent/commit/delivery state to lose. A v2 `Prepared` record becomes v3 with `Release=null`, `Stop=null`; allocations, preparation identity and cleanup progress are copied unchanged. |

The version-selection rule is exact: `Encode` emits version 2 when both
`Release` and `Stop` are absent and all pre-release fields are representable;
otherwise it emits version 3. A writer must never clear a non-null release
record to force a version-2 write. A v3 record with both new records absent is
decodable but is not produced by the writer.

## 11. Writer fencing and journal invariants

All new transitions use the existing `JournalWriterLease`. No new fence, lock or
writer type is introduced. A stale or competing writer cannot append release
intent, release commit, delivery state or stop winner because
`OwnershipJournal.Write` requires a live lease, the effective fence and the
exact next sequence. The recovery scope may write stop/cleanup state only after
pidfd-proven owner exit and fenced takeover; it never writes release state.

Decode/recovery validation:

- `Release` requires `Stage=Prepared` and a non-null `Preparation` record.
- The durable record does not add a separate `Owned` stage. The in-memory Owned
  proof is the writer precondition, and the intent is the first durable
  post-HELD transition. Recovery treats the intent as the trusted writer's
  assertion that Owned was established, not as an independently verified HELD
  report. This avoids changing WP04's prepared-stage behavior.
- `ReleaseTag=Intent` requires `DeliveryResult=NotAttempted`.
- `ReleaseTag=Committed` requires `CommittedAt >= IntentAt` and a concrete
  `DeliveryResult` other than `NotAttempted`.
- Release transitions cannot change the `Preparation` identity or the manifest
  digest recorded in it.
- The deadlines are immutable across intent and committed snapshots.
- A later snapshot must never move `Committed → Intent`, `Intent → absent`, or
  `Committed → absent`.
- A durable stop with `Release=null` forbids any later release intent; a writer
  that attempts it is refused. A stop with `Release!=null` is a later stop and
  does not erase release history.
- `StopDeadline >= StopAcceptedAt`; `StopCause` is a known enum value.
- A malformed, unsupported, oversized or checksum-invalid snapshot fails closed;
  recovery does not destructively adopt from an uncertain record.
- The existing 64 KiB snapshot bound, 128-allocation bound and sequence rules
  remain unchanged.

The journal is not an append-only tamper-evident log; its two slots hold
acknowledged generations, not history. Immutable-identity and no-rollback rules
are therefore writer invariants plus focused tests. The existing checksum
detects corruption, not forgery; host ownership and the trusted-writer model
apply. Recovery never derives release authority from the record.

## 12. Truthful recovery matrix

| Durable state | Release definitely impossible? | Delivery known? | Execution excludable? | Recovery action | Result |
|---|---|---|---|---|---|
| R0 `Prepared`, no intent, no stop | Yes (release was never admitted) | N/A | Yes, target remains held | Stop domain; identity-checked cleanup | `NotCommitted`; root `NotStarted`; `FailedToEstablish`/`CancelledBeforeRelease` by cause |
| S0 stop before intent | Yes | N/A | Yes | Stop/cleanup | `NotCommitted`; `CancelledBeforeRelease`/`FailedToEstablish`; root `NotStarted` |
| R1 intent only | No | Unknown | No | Kill whole domain; no resend; cleanup | `MayHaveExecuted`; exec/root `Unknown` |
| R1 + later stop | No | Unknown | No | Kill/cleanup; no resend; preserve history | `MayHaveExecuted`; stop may terminate; exec/root `Unknown` |
| R2 committed + `Delivered` | No | Write accepted into pipe; coordinator/target unproven | No | Kill/cleanup; no resend | `MayHaveExecuted`; exec/root `Unknown` absent trusted status |
| R2 committed + `WouldBlock`/`BrokenPipe`/`Partial`/`Unknown` | No (commit happened) | Known write failure/uncertainty | No under the durable owner-loss rule | Kill/cleanup; no resend | `MayHaveExecuted`; live owner may have `NotStarted`; recovery stays conservative |
| R2 + later stop | No | Per delivery result | No | Stop/cleanup; do not rewrite history | Release `MayHaveExecuted`; outcome `Stopped` or `Unknown` |
| Cleanup confirmed with R1/R2 | No | Per record | No | No further destructive action; retain receipt | Execution `MayHaveExecuted`/`Unknown`; cleanup `Confirmed` independently |
| Malformed/unsupported | Cannot establish | Unknown | No | Quarantine/Unknown; no destructive adoption without live authority | `Unknown` |

`ReleaseDisposition.Committed` remains a live-owner decision. After owner loss,
the durable commitment record does not by itself upgrade the exposed result:
the plan's conservative rule exposes `MayHaveExecuted` because delivery and
execution may remain uncertain. The record narrows evidence, not safety.

## 13. Cleanup/result independence and evidence preservation

The release/stop records are separate from `Cleanup` and `CompletedCleanup`.
Cleanup progress uses `record with` updates; it must preserve `Release` and
`Stop` unchanged. A `Cleanup=Confirmed` record can therefore coexist with
`Release=Intent/Committed` and an execution result of `MayHaveExecuted/Unknown`.

Domain removal, staging removal, owner exit and coordinator exit do not erase the
journal record. Final journal retirement is allowed only after the terminal
receipt has copied the release disposition, stop cause and delivery result;
terminal receipt retention is WP09 work. WP05 qualification must retain the
journal snapshots/traces needed to reconstruct the release classification even
after cleanup. Cleanup success never proves the target did not execute.

## 14. Security analysis

| Threat | Assessment |
|---|---|
| Replayable release authority | The durable record is evidence, not a capability. Recovery never sends release from it. There is no nonce/token/frame stored. The owner-local capability is consumed and dies with the owner. |
| Mutable release identity | The identity is the existing `RecordedPreparation`; the release record does not duplicate it. Decode refuses release without a preparation; writer invariants preserve it. Focused tests cover mutation attempts. |
| Authority persistence beyond need | Only identity, phase, timestamps, deadlines and delivery result are persisted. No release FD, writer handle, bearer token or reusable authorization is stored. |
| Stale-writer release | The existing G2 fence is mandatory; a stale/competing writer cannot append release/stop transitions. Recovery takeover is pidfd-proven and fenced; recovery never writes release. |
| Release after stop | The arbiter gate enforces stop-first. A durable stop with no intent forbids release. If an intent exists, recovery is conservative and never resends; no path authorizes release after a durable stop. |
| Duplicate delivery | The capability is single-use; the gate attempts exactly one write; recovery never releases. A durable intent/commit is sufficient to forbid resend. |
| Rollback to pre-release state | Phase monotonicity and preservation are writer invariants; cleanup preserves new fields; focused tests cover. The journal is not append-only, so a faulty trusted writer could overwrite evidence; that is outside the host-owner threat model and would be a product defect. |
| Journal forgery | The checksum detects corruption, not a malicious trusted writer. Records are never authority; host ownership and the existing trust model apply. |

## 15. G2 impact

| Accepted G2 property | Impact |
|---|---|
| Descriptor ownership | Unaffected; no FD or descriptor-map change. |
| Protocol framing / canonical 873-byte manifest vector | Unaffected by this schema. The WP05 RELEASE/status protocol is a separate future implementation change and does not alter the canonical manifest vector. |
| Recovery topology | Unaffected; same owner/recovery scope, same writer lease. |
| Journal honesty | Requires focused renewed evidence for v2/v3 decoding, version refusal, checksum and truthful uncertainty. |
| Writer fencing | Requires focused regression that release/stop transitions require the live lease and that stale/competing writers are refused. Fence implementation unchanged. |
| Capability locality | Unaffected; the release capability remains owner-local and is not representable in the journal. Focused test that recovery cannot release. |
| Exact-identity cleanup | Requires focused regression that cleanup preserves release/stop evidence. |
| Truthful reconciliation | Requires focused regression for the new recovery matrix. |
| Identity validation | Requires focused regression for release identity matching and malformed-record refusal. |
| Non-root pidfd topology | Unaffected. |

G2 is not reopened wholesale. The focused renewed evidence belongs to the WP05
schema/recovery implementation and its review; unrelated native topology does
not need requalification without a concrete dependency.

## 16. WP04 impact

WP04 production behavior is unchanged:

- The coordinator, HELD/abort path, descriptor map, mount verification and
  seccomp policy are not touched.
- Pre-release snapshots with no release/stop data remain version 2 under the
  lazy version-selection rule, preserving the accepted WP04 durable shape.
- A v3 snapshot is produced only when a WP05 release intent or stop record is
  first written. WP04 HELD/abort mechanics remain semantically identical.
- v2 decoding is preserved and focused regressions must prove existing WP04
  journal behavior. No WP04 requalification is required unless a focused
  regression exposes a concrete counterexample.

## 17. Protocol impact

This schema amendment changes no owner protocol, coordinator protocol,
descriptor map or canonical G2 vector. The future WP05 implementation will add
the release frame and post-release status handling; those are separate wire
changes with their own review and qualification. If the implementation needs a
new coordinator syscall, that is a separate seccomp classification and review,
not part of this schema.

## 18. Future schema test plan (not implemented here)

| Future test | Required property |
|---|---|
| v2 decode/round-trip | Existing v2 records retain exact field meaning; `Release`/`Stop` absent. |
| v2 → v3 lazy upgrade | A Prepared v2 record becomes v3 with null release/stop; identity/allocations unchanged. |
| v3 intent/committed/stop round-trip | All new fields encode/decode with exact values. |
| Version refusal | Version 4+ and malformed version fail closed. |
| Invalid transition refusal | Intent without Prepared/Preparation; committed without intent; delivery before commit; committed with `NotAttempted`; stop deadline inversion; deadline inversion. |
| Identity immutability | Release transitions cannot change preparation/manifest/generation identity. |
| Deadline immutability | Intent/completed deadlines cannot change across snapshots. |
| Duplicate release commitment | A second logical commit is refused/idempotent per the arbiter; no second write. |
| Release/stop winner | Stop-first has no intent; intent-first remains conservative; later stop never rewrites history. |
| Crash-state classification | R0/S0/R1/R2 matrix matches `MayHaveExecuted`/`NotCommitted`/`Unknown` rules. |
| Writer fencing | Stale/competing writers cannot append release or stop state. |
| Cleanup/result independence | Cleanup Confirmed coexists with release `MayHaveExecuted`; cleanup preserves release/stop. |
| Malformed recovery | Unsupported/corrupt records quarantine/Unknown; no destructive adoption. |

## 19. Future process-loss plan (not executed here)

| Barrier | Durable observation | Expected classification |
|---|---|---|
| 1. Before `ReleaseIntent` | R0/S0 | Release definitely impossible; `NotCommitted`; root `NotStarted`. |
| 2. After `ReleaseIntent`, before the gate | R1 | `MayHaveExecuted`; no resend. |
| 3. Inside the gate before/after the in-memory commit | R1 (durable points are indistinguishable) | Same as 2; the in-memory distinction is not durably observable. |
| 4. After the single release write, before the committed record | R1 | `MayHaveExecuted`; delivery unknown; no resend. |
| 5. After the committed record | R2 + delivery result | `MayHaveExecuted`; delivery per record; no resend. |
| 6. Abort/release race | S0, S1 or S2 | Stop-first no release; release-first history preserved; crash unresolved -> conservative. |

There is no production delivery acknowledgement distinct from the owner's single
write result, so a separate "before delivery acknowledgement" barrier collapses
into barrier 4/5. A test-only coordinator acknowledgement is qualification
evidence, not durable release truth.

## 20. Explicitly deferred

- Production implementation of `ReleaseArbiter`, `ReleaseDecision`,
  `CoordinatorProtocol`/`CoordinatorStatusReader`, owner wiring, release frame
  and coordinator post-release branch.
- Schema migration implementation, codec changes and all tests in section 18/19.
- WP05 qualification, G2b review, WP06–WP09 and WP10 target execution.
- Terminal receipt persistence and journal retirement (WP09).
- Any seccomp change or the deferred wrong-audit-architecture generator decision.
- Acceptance of this ADR or amendment of the authoritative plan. A later
  implementation tranche must not begin until this proposal is independently
  reviewed and the plan records the accepted schema.

## 21. Disposition

**WP05 SCHEMA AMENDMENT READY FOR INDEPENDENT REVIEW**
