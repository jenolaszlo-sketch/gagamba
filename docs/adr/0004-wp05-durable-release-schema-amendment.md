# ADR 0004: WP05 durable release schema amendment

Date: 2026-10-17 (Asia/Manila)

Status: **Accepted revision 2 — independent clarification rereview accepted;
WP05 v3 schema implementation may proceed.** This is a schema-design acceptance
only. No production implementation, migration, coordinator change, release
execution, target execution or WP05 qualification has been performed or is
authorized by this document alone.

Revision binding:

| Item | Revision |
|---|---|
| Original ADR 0004 commit | `c3cb16412cf136691280eb8f3fadedbe7dc451a9` |
| Independent review disposition | `ADR 0004 ACCEPTED WITH REQUIRED CLARIFICATIONS - UPDATE ADR BEFORE IMPLEMENTATION` |
| Preserved review record | [Independent ADR 0004 review](../reviews/gagamba-linux-m1-adr-0004-review.md) at `46a1991b57edb410d53ac7fc0c6b43ba815a6792` |
| Clarification rereview | [Independent clarification rereview](../reviews/gagamba-linux-m1-adr-0004-rereview.md) — accepted |
| Branch | `qualification/ar7-feasibility` |
| Authoritative architecture | [ADR 0003](0003-ar7-linux-first-restricted-execution.md) |
| Authoritative delivery plan | [Linux M1 implementation plan](../plans/gagamba-linux-m1-implementation-plan.md) |
| WP05 blocker evidence | [WP05 plan binding and schema blocker](../evidence/linux-m1/wp05-plan-binding-2026-10-17.md) |

This revision preserves the accepted architecture: `OwnershipSnapshot` version
3, the existing ownership journal and `JournalWriterLease`, no second release or
status journal, `RecordedPreparation` as release identity, durable
`ReleaseIntent` as the conservative crash-safety boundary, the runtime release
winner inside the arbitration gate, no resend after durable intent, no durable
bearer release authority, lazy v2→v3 compatibility, and no wholesale reopening
of WP04 or G2.

## 1. Problem

WP05 requires a durable `ReleaseIntent` carrying the exact preparation identity
and absolute deadlines before entering the release gate (plan lines 147, 209,
342), and recovery must classify release outcomes after owner loss (plan lines
159–165, 315). It also requires durable trusted exec/root observations
(`ExecObserved`, `RootObserved`, or repository-native equivalents) so truthful
target status survives owner death, recovery, domain/staging cleanup and receipt
publication delay (plan lines 343, 437).

The accepted ownership journal (`OwnershipSnapshot`, version 2) represents only
the pre-release preparation lifecycle. It has no release, stop, exec, root or
deadline state, and it explicitly excludes release credentials.

A second release journal or status journal is rejected: it would create two
durable authorities for one execution, would not be covered by the accepted G2
writer fence, and would contradict the single-snapshot recovery model (plan
lines 332, 347). The only acceptable extension is a versioned field addition to
the existing ownership snapshot, written under the existing
`JournalWriterLease`.

**Decision:** propose `OwnershipSnapshot` version 3 with optional
`ReleaseRecord`, `StopRecord`, `ExecObservation` and `RootObservation`. Version 2
remains decodable. The writer emits version 2 while no new fact is present and
version 3 when a WP05 transition first records release, stop or trusted status.
The release capability, release writer, status descriptors and all native
handles remain outside the journal; the new fields are evidence, never
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

Version 2 has no release phase, release deadline, stop cause/time, exec or root
observation or delivery state. `PreparationStage` ends at `Prepared`; the
durable record does not have a separate `Owned` state. WP04 accepted this
pre-release record and its writer fence ([WP03 evidence](../evidence/linux-m1/wp03.md:141–149)).

The journal is a bounded two-generation snapshot (`gen.a`/`gen.b`, `gen.tmp`,
`gen.seal`) with a SHA-256 checksum. A mutation requires a live
`JournalWriterLease`, the storage `flock`, a baseline sequence and exactly
`baseline + 1` (`OwnershipJournal.cs:358–416`). A failure reopens as the old
acknowledged generation or explicit uncertainty; it never promotes an
unacknowledged candidate.

## 3. Proposed version-3 schema

Version 3 is the version-2 payload followed by tagged records. The version byte
is inside the checksummed payload. No field is written at a fixed offset that
can overwrite a v2 field; the new records are an additive tail.

```
OwnershipSnapshotV3 =
    version-2 fields
    ReleaseTag : u8  0 = absent, 1 = Intent, 2 = Committed
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
    ExecTag : u8  0 = NotAttempted, 1 = FailedBeforeExec,
                  2 = Confirmed, 3 = Unknown
    if ExecTag == 1:
        ExecErrno : u32
    if ExecTag == 2:
        ExecBasis : u8  1 = CoordinatorFrame,
                        2 = ExecutableObjectIdentity,
                        3 = ReviewedNormalExitRule
    RootTag : u8  0 = NotObserved, 1 = Observed
    if RootTag == 1:
        RootKind         : u8  0 = Unknown, 1 = NotStarted,
                               2 = Exited, 3 = Signalled
        RootExitCode     : u32
        RootSignal       : u32
        RootCoreDumped   : u8  0 or 1
        RootRawWaitPresent : u8  0 or 1
        if RootRawWaitPresent == 1:
            RootRawWaitStatus : u32
```

`DeliveryResult` is a durable enum; it does not renumber the WP01 runtime
`ReleaseSendResult`:

| Value | Name | Meaning |
|---:|---|---|
| 0 | `NotAttempted` | Intent only; no release write is permitted yet. |
| 1 | `Delivered` | The single nonblocking write returned full 128-byte success. |
| 2 | `WouldBlock` | The single write returned `EAGAIN`. |
| 3 | `BrokenPipe` | The single write returned `EPIPE`. |
| 4 | `Partial` | The single write returned a short count. |
| 5 | `Unknown` | The write outcome was unexpected or could not be classified. |

There is no separate durable `ReleaseDelivered` state: the plan's semantic
`ReleaseDelivered` observation is `ReleaseTag=Committed` +
`DeliveryResult=Delivered`.

### 3.1 Release, stop and observation field rationale

| Field | Why recovery needs it | Writer | Immutable | In identity | Security-sensitive |
|---|---|---|---|---|---|
| `ReleaseTag=Intent` | Fixes the durable safety boundary. Before an acknowledged intent, release is definitely impossible; after it, recovery must assume release may have won. | Owner, with its live `JournalWriterLease`, after in-memory Owned checks, before the gate. | The intent identity and deadlines are immutable once written. | Uses the existing `Preparation` identity; the tag itself is not identity. | Yes, as evidence. It is not a capability. |
| `ReleaseTag=Committed` | Proves the gate committed and the single write was attempted when the owner survived to record it. Its absence does **not** prove non-commit. | Owner, outside the gate, after the gate's commit/consume/write. | Once written. | No; it refers to the same `Preparation`. | Yes, as evidence. |
| `IntentAtNanoseconds` | Audit ordering and restart context. | Owner. | Yes. | No. | Low. |
| `SetupDeadlineNanoseconds` | The gate's release-eligibility deadline; recovery must know whether release was still permitted. | Owner, copied from the request/preparation context. | Yes. | No (runtime authority, not manifest identity). | Yes, authority-relevant. |
| `OverallDeadlineNanoseconds` | Post-release execution/stop deadline; also carried in the release frame. | Owner. | Yes. | No. | Yes, authority-relevant. |
| `CommittedAtNanoseconds` | Audit time of the post-gate observation, not the exact gate instant. | Owner. | Yes. | No. | Low. |
| `DeliveryResult` | Narrows whether the single write was accepted; does not prove coordinator consumption or target execution. | Owner, from the one write's return. | Yes. | No. | Yes, result-relevant. |
| `StopTag` / `StopAcceptedAtNanoseconds` / `StopCause` / `StopDeadlineNanoseconds` | Records the first accepted stop, its stable cause and the absolute teardown bound. | Owner, or the recovery scope after pidfd-proven owner death and fenced takeover. Never the target. | First acceptance is immutable; later causes are not recorded here. | No. | Yes, arbitration and teardown evidence. |
| `ExecTag` / `ExecErrno` / `ExecBasis` | Records the trusted exec classification and its evidence basis so recovery does not infer execution from delivery or cleanup. | Owner only, after validating a trusted coordinator status frame, live executable-object identity or the reviewed normal-exit rule. | First non-zero value is terminal and immutable. Duplicate identical is idempotent; contradiction fails closed. | Bound by the snapshot's `Preparation` and the validated status frame identity; no duplicate identity field. | Yes, execution evidence. |
| `RootTag` / `RootKind` / exit/signal/core/raw wait | Records the trusted target root result, or an explicitly unknown root with optional child-diagnostic raw wait. | Owner only, after validating trusted coordinator/reaper status. | First observation is terminal and immutable. `Unknown` is not later upgraded; contradictions fail closed. | Bound by the same `Preparation`; no duplicate identity field. | Yes, result evidence. |

No release nonce, release token, decision UUID, manifest copy, generation copy,
execution copy, second boot ID or final outcome is added. The release identity
is the existing `RecordedPreparation` (`ExecutionId`, `IssuerId`,
`GenerationId`, `PreparationId`, `ManifestDigest`) in the same snapshot. A
release or observation record must never exist when `Preparation` is absent or
`Stage` is not `Prepared`. A durable record is not a bearer capability.

### 3.2 Stable durable enum encodings

All values are explicit on-disk constants. They are never reordered or reused.
Reserved values are rejected by the decoder; unknown values fail closed. Code
must not rely on compiler enum ordinal defaults.

| Durable enum | Values | Reserved |
|---|---|---|
| `ReleaseTag` | `0=absent`, `1=Intent`, `2=Committed` | `3–255` |
| `DeliveryResult` | `0=NotAttempted`, `1=Delivered`, `2=WouldBlock`, `3=BrokenPipe`, `4=Partial`, `5=Unknown` | `6–255` |
| `StopCause` | Existing `StopCauseKind`: `0=CallerStop`, `1=CallerLoss`, `2=SetupExpired`, `3=SetupFault`, `4=OutputOverflow`, `5=OutputReadFailure`, `6=RecoveryTakeover`, `7=TransportFailure` | `8–255` |
| `ExecTag` | `0=NotAttempted`, `1=FailedBeforeExec`, `2=Confirmed`, `3=Unknown` | `4–255` |
| `ExecBasis` | `0=None`, `1=CoordinatorFrame`, `2=ExecutableObjectIdentity`, `3=ReviewedNormalExitRule` | `4–255` |
| `RootTag` | `0=NotObserved`, `1=Observed` | `2–255` |
| `RootKind` | `0=Unknown`, `1=NotStarted`, `2=Exited`, `3=Signalled` | `4–255` |
| `RootCoreDumped` | `0,1` | `2–255` |
| `RootRawWaitPresent` | `0,1` | `2–255` |

The existing `ExecDisposition` values (`NotAttempted=0`, `FailedBeforeExec=1`,
`Confirmed=2`, `Unknown=3`), `RootResultKind` values (`Unknown=0`,
`NotStarted=1`, `Exited=2`, `Signalled=3`) and `StopCauseKind` values are reused
exactly. New durable enums are explicit and separate from the WP01 runtime
`ReleaseSendResult`.

## 4. Durable state machine

### 4.1 Release and stop

The durable release lifecycle is layered beside the unchanged preparation stage;
it does not replace `Stage=Prepared`.

| State | Durable shape | Meaning |
|---|---|---|
| R0 | `Release=null`, `Stop=null` | No release was requested. Target remains held. Release is definitely impossible. |
| R1 | `Release=Intent`, `Stop=null` | Release was admitted for arbitration. The gate has not yet been observed durably. The single write may or may not have happened. |
| R2 | `Release=Committed`, `DeliveryResult=<value>` | The owner recorded, after the gate, that the gate committed and the single write was attempted with this result. |
| S0 | `Stop` present, `Release=null` | Stop was accepted before any release intent. Release is definitely impossible. |
| S1 | `Stop` present, `Release=Intent` | A stop coexists with an earlier or concurrent intent. Recovery must not claim release was impossible. |
| S2 | `Stop` present, `Release=Committed` | Release won before a later stop, or the stop was accepted while release was already committed. Release history is preserved; stop terminates/reconciles. |

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

### 4.2 Trusted exec/root observations

Observations live in the same ownership snapshot. A second status journal is
forbidden.

**Exec observation model.**

| `ExecTag` | Meaning | Required fields | First allowed | Update rule |
|---|---|---|---|---|
| `0=NotAttempted` | No trusted exec observation; initial state. | None. | Before release. | The first non-zero value is terminal. |
| `1=FailedBeforeExec` | Trusted coordinator `EXEC_FAILED`; target did not exec. | `ExecErrno > 0`. | After `ReleaseTag=Committed`; validated `EXEC_FAILED` status. | Terminal; no change. |
| `2=Confirmed` | Trusted exec confirmation. | `ExecBasis` in `1–3`. | After `ReleaseTag=Committed`; validated `EXEC_CONFIRMED`, live executable-object identity, or reviewed normal-exit rule. | Terminal; no change. |
| `3=Unknown` | Trusted status was lost/signalled/ambiguous before exec could be confirmed. | None. | After `ReleaseTag=Committed`; terminal status-loss decision. | Terminal; no change. |

`ExecBasis` values:

| Value | Basis | Plan source |
|---:|---|---|
| `0=None` | Not used with `Confirmed`. | — |
| `1=CoordinatorFrame` | Trusted `EXEC_CONFIRMED` coordinator status. | Plan line 197/8.1 |
| `2=ExecutableObjectIdentity` | Live `/proc/<direct-child>/exe` fstat identity match against the retained `/tool` object while the direct child is unreaped. | Plan line 201 |
| `3=ReviewedNormalExitRule` | Reaping-time normal child exit with complete error-pipe EOF and no exec-error record; only the reviewed child path may take this route. | Plan line 201 |

`ExecBasis=ReviewedNormalExitRule` requires a recorded `RootKind=Exited` for the
same reaped child. `ExecBasis=CoordinatorFrame` or
`ExecBasis=ExecutableObjectIdentity` may be recorded before a root observation.

**Root observation model.**

| `RootTag` / `RootKind` | Meaning | Required fields | Consistency |
|---|---|---|---|
| `0` / — | No root observation. | None. | The result layer derives `Unknown`/`NotStarted` from release/exec facts. |
| `1` / `0=Unknown` | Root observation is explicitly unknown; optional raw child wait is diagnostic only. | `RootRawWaitPresent`; optional `RootRawWaitStatus`. | Requires `ExecTag` in `2,3` (`Confirmed` or `Unknown`). The raw wait must never be treated as a target exit/signal. |
| `1` / `1=NotStarted` | Target root never started. | Optional diagnostic raw wait. | Requires `ExecTag=1` (`FailedBeforeExec`). |
| `1` / `2=Exited` | Trusted target exited with `RootExitCode`. | `RootExitCode`; `RootSignal=0`; `RootCoreDumped=0`; optional raw wait. | Requires `ExecTag=2` (`Confirmed`). If a raw wait is present, it must decode as a normal exit with the same code. |
| `1` / `3=Signalled` | Trusted target was signalled with `RootSignal`; core flag retained. | `RootSignal>0`; `RootExitCode=0`; `RootCoreDumped` in `0,1`; optional raw wait. | Requires `ExecTag=2` (`Confirmed`). If a raw wait is present, it must decode as the same signal and core flag. |

A `ROOT_WAIT` without exec confirmation is recorded only as
`RootKind=Unknown` with a diagnostic raw wait; it cannot become an
`Exited`/`Signalled`/`NotStarted` target result. `ReleaseTag=Committed` with
`DeliveryResult=Delivered` never creates an exec observation. Only the trusted
status/reaping evidence listed above does.

**Observation authority.** Durable observations are evidence, not authority.
They must not permit release, reconstruct a consumed capability, authorize
retry, authorize target execution, or override writer fencing. Recovery reads
them; it never writes or invents them.

**Trusted source, identity and order.** The owner reads coordinator status from
the trusted status pipe (FD 5) and the target/reaper relationship. It validates
the frame magic, version, kind, total length, sequence, execution UUID,
preparation UUID, manifest digest and the exact status order from plan §8.1
(HELD; then `EXEC_FAILED → ROOT_WAIT`, or optional `EXEC_CONFIRMED → ROOT_WAIT`;
`PROTOCOL_FAILED` terminates). It rejects stdout/stderr, Bubblewrap JSON,
caller-supplied bytes and any status for a different identity. The durable
observation is bound to the same snapshot `Preparation`; no duplicate identity
field is added. The journal sequence orders durable facts.

**Observation timing and owner loss.** The sequence is: coordinator produces a
trusted status; owner observes and validates it; owner persists it under the
live lease; owner may die before terminal receipt publication. Recovery reads
only the persisted observation. A crash after observing but before persisting
loses the observation; recovery must not infer it from the status pipe, stdout,
stderr, raw wait bytes or cleanup. A crash after persisting preserves it. A crash
while a later observation is pending keeps the earlier observation and leaves
the later fact unknown. Only durable evidence may narrow recovery truth.

**Monotonicity.** `ExecTag` and `RootTag` are single-assignment terminal facts
after their first non-zero/observed value. Identical duplicate writes are
idempotent; a contradictory value fails closed and is never persisted. A root
`Unknown` is not later upgraded; the owner records it only when it has decided
the trusted status is terminally lost. A later stop or cleanup preserves the
observation.

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
attempted. It does **not** mean the write was accepted (`DeliveryResult` decides
that), the coordinator consumed the frame, the target executed, or the execution
result is known. Durable exec/root observations are separate facts and never
inferred from release delivery.

If a reviewer requires a durable commit record written *before* the release
write, that requires either journal I/O inside the gate (forbidden by plan line
207) or a second durable decision step outside the gate that can diverge from
the stop/expiry checks. That would be an architecture change, not this schema
amendment. This design keeps the accepted ordering.

## 6. Transport ordering, delivery and partial delivery

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

**Delivered terminology.** `DeliveryResult=Delivered` means only that the
owner's full 128-byte pipe write returned successfully. It does not mean the
coordinator consumed `RELEASE`, exec succeeded, the root was observed, the target
initialized, or the target executed. Those facts come only from the trusted
observations in §4.2.

**Partial delivery.** For the qualified Linux transport the release frame is
128 bytes, `128 <= PIPE_BUF`, and a nonblocking pipe write of at most `PIPE_BUF`
is expected to be full-length or fail with `EAGAIN`. `Partial` is therefore an
impossible-under-contract anomaly. It remains as a defensive representation
only: it is never retried; it produces conservative `Unknown`/`MayHaveExecuted`;
and encountering it is qualification-worthy evidence of an invariant violation.
The coordinator requires a complete 128-byte frame and fails without target fork
on a partial/invalid frame.

## 7. Crash semantics

| Crash boundary | Latest durable state | May release have been written? | Recovery action | Result |
|---|---|---|---|---|
| Before intent | R0 | No. Release was never admitted. | Stop domain, identity-checked cleanup. | `ReleaseDisposition.NotCommitted`; root `NotStarted`; `FailedToEstablish` or `CancelledBeforeRelease` by cause. |
| After intent, before gate | R1 | Unknown. The gate may not have run, but the durable record cannot prove it. | Kill whole domain, no release, no resend, cleanup. | `MayHaveExecuted`; exec/root `Unknown` unless durable trusted observations. |
| Inside gate before commit | R1 | Unknown. Durable state is identical to the previous row. | Same. | Same. |
| After in-memory commit, before write | R1 | Unknown. Physical no-write is not durably provable. | Same. | Same. |
| After write, before committed record | R1 | Unknown; delivery also unknown. | Same; no resend. | `MayHaveExecuted`; delivery unknown; exec/root per any durable observations. |
| After committed record | R2 + delivery result | Yes; release won. Delivery is known only at the write-result level. | Kill whole domain, no resend, cleanup. | `MayHaveExecuted`; delivery per record; exec/root per durable observations. |
| Stop before intent | S0 | No. | Stop/cleanup. | `NotCommitted`; `CancelledBeforeRelease`/`FailedToEstablish`; root `NotStarted`. |
| Stop after intent, before commit | S1 | Unknown. | Kill/cleanup; no resend. | `MayHaveExecuted`; stop may still terminate/reconcile. |
| Stop after commit | S2 | Yes. | Stop/cleanup; no history rewrite. | Release `MayHaveExecuted`; outcome `Stopped` or `Unknown` by evidence. |
| Exec/root observed but not persisted | Prior durable state | May or may not have been written; the observation is lost. | Do not infer from the status channel; kill/cleanup. | Conservative prior classification; no observation is manufactured. |
| Exec/root persisted | R2 plus observation | Yes if committed. | Use the observation; kill/cleanup as needed. | Exec/root per observation; release remains `MayHaveExecuted` after owner loss. |
| Malformed/unsupported record | Unknown | Cannot establish. | Quarantine/Unknown; no destructive adoption without live authority. | `Unknown`; never success or confirmed-not-executed. |

Physical possibility and durable proof are distinct. A crash after a durable
intent but before any write may physically leave the target unexecuted, but the
durable contract cannot prove no write occurred. The accepted plan chooses
possible false-positive `MayHaveExecuted` over duplicate release. Recovery never
sends a release to resolve the uncertainty.

## 8. Stop/release serialization

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

**Serialization cases.**

A. `ReleaseIntent` is durable, then a stop is accepted: the stop write starts
from the latest acknowledged snapshot, preserves `Release=Intent`, adds the
`StopRecord`, and advances exactly one sequence. The release history remains.
Recovery is conservative. If the stop won runtime arbitration before release
commit, the arbiter does not write a later `Committed`. If release won first,
the committed record is written with the later stop preserved; release history
is not rewritten.

B. A stop is durable first: the release arbiter checks the stop flag and the
latest snapshot under the gate and refuses to write an intent. If a stale or
competing writer attempts an intent, it lacks the live lease or its baseline is
behind; the journal refuses with a sequence/baseline failure. No release-intent
write occurs after a winning stop.

C. One writer prepares a release update from a stale state while another
durable fact advances: the stale write holds the wrong baseline sequence. The
journal's `Write` re-resolves under the fence and refuses
("lineage changed under the writer fence" or the expected-sequence check). It
does not erase the newer fact. Only the single live lease holder can write, and
its baseline advances on each acknowledged snapshot.

**Committed/stop serialization.** If release wins runtime arbitration, the
committed snapshot is written after the transport attempt. Any durable stop
record before or after that write is preserved. A later stop cannot erase intent,
commitment or delivery result. A stop that won before runtime release commit
prevents release; no committed record is written (the durable state is
stop-only or intent+stop). The journal representation preserves truth because
every write is a full monotonic snapshot; the writer copies the existing release,
stop and observation facts and changes only the authorized transition.

## 9. Deadlines and timestamps

### 9.1 Deadlines

| Deadline | Representation | Time base | Written by/when | Restart behavior |
|---|---|---|---|---|
| `SetupDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner, with `ReleaseIntent` | If intent exists, immutable evidence. Expiry does not undo intent/commit. If no intent, no release decision exists. |
| `OverallDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner, with `ReleaseIntent`; also in the 128-byte release frame | After release, governs execution/stop. Recovery stops/cleans at or after expiry; expiry never authorizes execution. |
| `StopDeadlineNanoseconds` | u64 absolute nanoseconds | `CLOCK_MONOTONIC`, same boot | Owner/recovery writer when the first stop is accepted | Recovery uses the remaining allowance for bounded stop; if expired, it proceeds with a bounded urgent stop/cleanup. |

`StopDeadline` is computed exactly at the first accepted stop:

```
StopAcceptedAtNanoseconds = CLOCK_MONOTONIC sample at first in-gate stop acceptance
StopDeadlineNanoseconds   = StopAcceptedAtNanoseconds + 5_000_000_000  (5 seconds)
```

This is the plan's five-second stop confirmation allowance (plan line 284). The
bounded final drain is separate: at most one second after domain-empty or
stop-allowance exhaustion, whichever comes first (plan line 284). The final-drain
deadline is runtime-derived from the fixed one-second allowance and the observed
domain-empty time or `StopDeadline`; it is not the `StopDeadline` and is not
persisted as a second deadline. Recovery must not extend the five-second stop
allowance or the one-second drain. If recovery begins after `StopDeadline`, it
performs a bounded urgent stop and the fixed one-second final drain; it never
waits for more execution time.

All deadlines are positive and `SetupDeadline < OverallDeadline`;
`StopDeadline >= StopAcceptedAt`. A process-relative stopwatch value is never
persisted. `CLOCK_MONOTONIC` is boot-relative; a reboot changes the boot ID and
resets the clock. Cross-boot records remain non-authority and are
quarantined/refused by the existing recovery predicate. A wall-clock change or
WSL host clock change does not affect the monotonic basis. If the clock is
unavailable/zero at a write or recovery decision, fail closed. Expiry never
authorizes release or retry.

### 9.2 Timestamp semantics

| Timestamp | Sampling meaning |
|---|---|
| `IntentAtNanoseconds` | Sampled immediately before the intent snapshot write; it is the sample time, not the fsync completion time. |
| `CommittedAtNanoseconds` | Sampled after the gate and its single write attempt, immediately before the committed snapshot write. |
| `StopAcceptedAtNanoseconds` | Sampled at the first in-gate stop acceptance, before the stop snapshot write. |
| Observation timestamp | No separate observation timestamp is added. The containing snapshot sequence and the owner's audit trace provide the durable order/time. |

Timestamps are audit/diagnostic facts and do not determine release/stop winner
ordering. Winner ordering comes from the arbitration gate and the durable state
semantics, never from comparing `IntentAt` with `StopAcceptedAt` or any other
timestamp. A stop accepted before runtime release commit prevents release even
if its persisted record is written after the intent record. Timestamps are
sampled before the record containing them is durably persisted; a clock failure
refuses the write or fails closed.

## 10. Boot and deadline binding

Version 3 continues to inherit the existing `OwnershipSnapshot.BootId`. No
second boot identifier is added. Monotonic deadlines are meaningful only when
the persisted `BootId` matches the current boot. Cross-boot recovery cannot
exercise release authority and fails safe: the existing recovery predicate
refuses destructive adoption and quarantines the record. A cross-boot record may
be read as evidence but cannot be used to release, retry, or infer a
same-boot deadline. No wall-clock conversion is introduced.

## 11. Compatibility and migration

| Question | Decision |
|---|---|
| Can v3 code read v2? | Yes. A v2 snapshot decodes as `Release=null`, `Stop=null`, `ExecTag=NotAttempted`, `RootTag=NotObserved`; all existing fields retain their v2 meaning. |
| Can a v2 Prepared execution be upgraded safely? | Yes, lazily, on the first v3 write, by the live issuer that still holds the capability. v2 has no release state and the WP04 production path had no release operation, so null release/observation defaults are truthful. A restarted process has no capability and cannot upgrade into release. |
| Is the upgrade lazy, explicit or prohibited? | Lazy at the writer boundary: the first write that needs release, stop or trusted status emits v3; otherwise the writer emits v2. No bulk migration tool. |
| Can older v2 binaries read v3? | No. The v2 decoder must refuse version 3 and fail closed. Mixed-version operation is unsupported. |
| Must newer unsupported versions fail closed? | Yes. The v3 decoder refuses every version other than 2 or 3; no fallback, no best-effort decode. |
| Are persisted v2 records migrated? | Only on their next acknowledged write. Existing v2 records may remain v2 indefinitely if no WP05 transition occurs. |
| Can any v2 state not be migrated safely? | No. v2 has no release, stop or trusted-observation state to lose. A v2 `Prepared` record becomes v3 with null release/stop and `ExecTag=0`/`RootTag=0`; allocations, preparation identity and cleanup progress are copied unchanged. |

The version-selection rule is exact: `Encode` emits version 2 when release,
stop and both observations are absent and all pre-release fields are
representable; otherwise it emits version 3. A writer must never clear a durable
fact to force a version-2 write. A v3 record with all new records absent is
decodable but is not produced by the writer.

Migration policy: no bulk migration is implemented or required by this ADR. The
first schema implementation tranche may add the v2→v3 lazy transition; that is
schema implementation, not migration of live records, and it must preserve the
v2 decode and the exact v2 field meaning.

## 12. Writer fencing, serialization and no lost facts

All new transitions use the existing `JournalWriterLease`. No new fence, lock or
writer type is introduced. A stale or competing writer cannot append release
intent, release commit, stop, exec/root observation or cleanup state because
`OwnershipJournal.Write` requires a live lease, the effective fence and the exact
next sequence.

Every update is a **full monotonic snapshot transition**, never an independent
last-writer-wins field write:

1. Acquire/retain the valid `JournalWriterLease`.
2. Read the latest acknowledged snapshot under the lease, or use the snapshot
   already held under that same live lease; do not use an unverified stale copy.
3. Construct the next snapshot from the latest snapshot, preserving every
   existing monotonic fact and changing only the authorized transition.
4. Use `sequence = baseline + 1`.
5. `Write` re-resolves the durable lineage under the fence and refuses if the
   baseline changed, the sequence is wrong, the lease is dead/foreign, or the
   snapshot is malformed.
6. On refusal, no fact is written; the caller reports the conflict and does not
   retry with a stale snapshot.

**No-lost-fact rule.** Once durable, none of these facts may disappear:

- release intent;
- release commitment;
- delivery result;
- accepted stop;
- exec observation;
- root observation.

A writer using a stale snapshot must never overwrite a newer fact. The existing
journal sequence, baseline re-resolution and exclusive `flock` detect stale
writes. A stale baseline fails with a sequence/lineage error; a competing writer
cannot hold the lease; a dead owner's lease is taken over only after
pidfd-proven exit. The writer copies all existing facts into the new snapshot.
The decoder rejects self-inconsistent records and unknown enums; cross-snapshot
regression is prevented by the full-snapshot writer discipline and focused
tests. The journal is not append-only; a deliberately faulty trusted writer
could clear a fact, which is a product defect outside the stale-writer model.

**Intent/stop and observation merge.** A stop written after intent preserves the
intent and any observations. A committed write after a stop preserves the stop.
An exec/root observation written after release/stop preserves release, stop,
deadlines, identity and the other observation. A later cleanup write preserves
all of them. Once an observation is durable, a later stop/cleanup cannot erase
it. The writer uses the latest snapshot and `record with`-style copying; it never
constructs an empty snapshot.

### 12.1 Validation invariants

- A v3 transition is allowed only from a valid acknowledged snapshot read under
  the live writer lease; a stale baseline, wrong sequence, dead/foreign lease or
  conflicting lineage fails closed.
- Release requires `Stage=Prepared` and a non-null `Preparation`; the in-memory
  Owned proof is the writer precondition.
- `ReleaseTag=Committed` requires the intent fields in the same snapshot and a
  prior acknowledged intent transition by the same owner generation.
- `DeliveryResult` is present only with `ReleaseTag=Committed`;
  `NotAttempted` is valid only with `Intent`.
- Release/observation transitions cannot change the preparation identity,
  manifest digest, generation or execution identity.
- Deadlines are immutable once intent exists; `SetupDeadline < OverallDeadline`
  and `StopDeadline >= StopAcceptedAt`.
- Release facts (intent, commitment, delivery) and stop facts never disappear.
- Exec/root observations never regress; `ExecTag>0`/`RootTag=1` require
  `ReleaseTag=Committed`; `RootTag=1` requires `ExecTag>0`.
- `ExecTag=FailedBeforeExec` requires `ExecErrno>0` and no `ExecBasis`;
  `ExecTag=Confirmed` requires an `ExecBasis` in `1–3` and no errno;
  `ExecTag=NotAttempted/Unknown` carries neither.
- `RootKind=Exited`/`Signalled` require `ExecTag=Confirmed`; `RootKind=NotStarted`
  requires `ExecTag=FailedBeforeExec`; `RootKind=Unknown` may coexist only with
  `ExecTag=Confirmed` or `ExecTag=Unknown`.
- A present root raw wait must be kind-consistent when the root kind is
  `Exited`/`Signalled`; with `Unknown`/`NotStarted` it is diagnostic only.
- `ExecBasis=ReviewedNormalExitRule` requires a recorded `RootKind=Exited`.
- A stop/release winner cannot reverse; a stop with no release forbids a later
  release intent; a later stop cannot erase release/observation facts.
- Unsupported versions, unknown/reserved enum values, invalid presence tags and
  contradictory combinations fail closed during decode/recovery.

## 13. Truthful recovery matrix

Only semantically valid combinations are listed. `ReleaseDisposition` is the
durable release classification after recovery; a live owner may expose
`Committed` where recovery must remain conservative. Trusted observations
narrow the execution/root/outcome fields, not the release disposition: after
owner loss a durable `Intent`/`Committed` still exposes
`ReleaseDisposition.MayHaveExecuted` even when `ExecTag=FailedBeforeExec`
establishes that the target never executed and root is `NotStarted`. This
field-separated truth is required by the plan.

Valid durable combinations require `ReleaseTag=Committed` before any exec or
root observation. `ReleaseTag=Intent` cannot coexist with `ExecTag>0` or
`RootTag=1`: trusted status can only follow a committed release, and if the
owner observes status before writing the committed record it persists the
committed record and the observation in the same snapshot. A requested
"intent + exec observed," "intent + trusted exec-not-observed" (for example
`ExecTag=Unknown`) or "intent + root observed" state is therefore not a valid
durable state; the valid equivalent is "committed + observation."

| Release | Stop | Exec | Root | Delivery | Cleanup | Execution classification | Root classification | Resend? | Recovery action / cleanup effect |
|---|---|---|---|---|---|---|---|---|---|
| none | none | none | none | n/a | any | `NotCommitted`; definitely not executed | `NotStarted` | No (no intent) | Stop domain; identity-checked cleanup; cleanup independent |
| none | accepted | none | none | n/a | any | `NotCommitted`; definitely not executed | `NotStarted` | No | Stop/cleanup; release history absent |
| Intent | none | none | none | n/a | any | `MayHaveExecuted` | `Unknown` | No | Kill whole domain; no resend; cleanup |
| Intent | accepted | none | none | n/a | any | `MayHaveExecuted` | `Unknown` | No | Stop/kill; no resend; preserve intent+stop |
| Committed | any | `FailedBeforeExec` | none/`NotStarted` | per record | any | `Exec FailedBeforeExec`; `FailedToEstablish` when the trusted protocol is complete, otherwise `Unknown` | `NotStarted` | No | Stop/cleanup; preserve exec fact |
| Committed | any | `Confirmed` | none | per record | any | `Exec Confirmed`; aggregate `Unknown` until root/domain/capture complete | `Unknown` | No | Stop/cleanup; preserve exec fact |
| Committed | any | `Confirmed` | `Exited(0)` | per record | any | `Succeeded` only if domain empty, capture complete and no accepted stop; otherwise `Unknown`/`Stopped` | `Exited(0)` | No | Stop/cleanup; root preserved independently |
| Committed | any | `Confirmed` | `Exited(nonzero)` | per record | any | `TargetFailed` unless an accepted stop/observation loss requires `Stopped`/`Unknown` | `Exited(code)` | No | Stop/cleanup; root preserved |
| Committed | any | `Confirmed` | `Signalled` | per record | any | `Stopped` if an accepted stop caused teardown, otherwise `TargetFailed`; `Unknown` if observations are incomplete | `Signalled(signal, core)` | No | Stop/cleanup; root preserved |
| Committed | any | `Unknown` | `Unknown` (raw wait optional) | per record | any | `MayHaveExecuted`/`Unknown`; raw wait is diagnostic only | `Unknown`; raw wait not a target result | No | Stop/cleanup; no inference from raw wait |
| Committed | any | none | none | `Delivered` | any | `MayHaveExecuted`; exec/root remain `Unknown` | `Unknown` | No | Kill/cleanup; no resend; delivery is pipe acceptance only |
| Committed | any | none | none | `WouldBlock`/`BrokenPipe`/`Partial`/`Unknown` | any | `MayHaveExecuted` after owner loss; live owner may report `NotStarted` | `Unknown` | No | Kill/cleanup; no resend; `Partial` is an invariant-violation signal |
| any of the above | any | any | any | any | `Confirmed` | Execution/root classification unchanged | Unchanged | No after intent | No additional destructive action; cleanup `Confirmed` may coexist with `MayHaveExecuted`/`Unknown` |
| malformed/unsupported | — | — | — | — | — | `Unknown` | `Unknown` | No | Quarantine; no destructive adoption without live authority |

Cleanup success never overwrites execution history. A `Cleanup=Confirmed` row
with release intent/commitment and no exec/root evidence remains
`MayHaveExecuted`/`Unknown`.

## 14. Cleanup/result independence and evidence preservation

The release, stop and observation records are separate from `Cleanup` and
`CompletedCleanup`. Cleanup progress uses full snapshot updates that preserve
`Release`, `Stop`, `ExecTag` and `RootTag` unchanged. A `Cleanup=Confirmed`
record can therefore coexist with release intent/commitment and an execution
result of `MayHaveExecuted`/`Unknown`.

Domain removal, staging removal, owner exit and coordinator exit do not erase the
journal record. Final journal retirement is allowed only after the terminal
receipt has copied the release disposition, stop cause, delivery result, exec
observation and root observation; terminal receipt retention is WP09 work. WP05
qualification must retain the journal snapshots/traces needed to reconstruct the
classification even after cleanup. Cleanup success never proves the target did
not execute.

## 15. Security analysis

| Threat | Assessment |
|---|---|
| Replayable release authority | The durable record is evidence, not a capability. Recovery never sends release from it. There is no nonce/token/frame stored. The owner-local capability is consumed and dies with the owner. |
| Mutable release identity | The identity is the existing `RecordedPreparation`; the release/observation records do not duplicate it. Decode refuses release/observation without a preparation; writer invariants preserve it. Focused tests cover mutation attempts. |
| Authority persistence beyond need | Only identity, phase, timestamps, deadlines, delivery and trusted observations are persisted. No release FD, writer handle, status reader, bearer token or reusable authorization is stored. |
| Stale-writer release | The existing G2 fence is mandatory; a stale/competing writer cannot append release/stop/observation transitions. Recovery takeover is pidfd-proven and fenced; recovery never writes release or observations. |
| Release after stop | The arbiter gate enforces stop-first. A durable stop with no intent forbids release. If an intent exists, recovery is conservative and never resends; no path authorizes release after a durable stop. |
| Duplicate delivery | The capability is single-use; the gate attempts exactly one write; recovery never releases. A durable intent/commit is sufficient to forbid resend. |
| Rollback to pre-release state | Phase and observation monotonicity plus fact preservation are writer invariants; cleanup preserves new fields; focused tests cover. The journal is not append-only, so a faulty trusted writer could overwrite evidence; that is outside the host-owner threat model and would be a product defect. |
| Forged observations | Observations are trusted only from validated coordinator/reaper status and are persisted only by the owner under the fence. Recovery cannot create them. Checksum detects corruption, not a malicious trusted writer; host-owner trust applies. |
| Execution inferred from delivery | Forbidden. `Delivered` is only the full pipe write; exec/root require the trusted observations in §4.2. |

## 16. G2 impact

| Accepted G2 property | Impact |
|---|---|
| Descriptor ownership | Unaffected; no FD or descriptor-map change. |
| Native protocol topology | Unaffected by this schema. The WP05 `RELEASE`/status protocol is a separate future implementation change. |
| Canonical 873-byte manifest vector | Unaffected; the manifest codec and golden vector do not change. |
| Capability locality | Unaffected; release authority remains owner-local; recovery cannot release from the record; observations are evidence only. |
| Non-root pidfd topology | Unaffected. |
| Journal v3 encode/decode and journal honesty | **Focused renewed evidence required**: v2/v3 round-trip, version/enum refusal, checksum, malformed/contradictory records, truthful uncertainty. |
| Writer fencing | **Focused renewed evidence required**: release/stop/observation transitions require the live lease; stale baseline, dead/foreign lease and competing writer refused. |
| Stale-baseline refusal | **Focused renewed evidence required**: a stale snapshot cannot erase a newer release/stop/observation fact; sequence/lineage failure is fail-closed. |
| Recovery classification | **Focused renewed evidence required**: the §13 matrix, exec/root observation survival and truthful `MayHaveExecuted`/`Unknown` handling. |
| Cleanup/result independence | **Focused renewed evidence required**: cleanup `Confirmed` preserves release/stop/observations and does not overwrite execution history. |
| Identity validation | **Focused renewed evidence required**: release/observation identity binding, immutable preparation identity and malformed-record refusal. |
| Exact-identity cleanup | **Focused renewed evidence required** for preserving the new facts through cleanup; the cleanup primitive itself is unchanged. |

G2 is not reopened wholesale. There is no counterexample to an accepted G2
property; the focused renewed evidence belongs to the WP05 schema/recovery
implementation and its review.

## 17. WP04 v3 boundary

WP04 production behavior is unchanged:

- The coordinator, HELD/abort path, descriptor map, mount verification and
  seccomp policy are not touched.
- Ordinary WP04 HELD/abort continues to write only version 2. The existing
  `AbortAsync` path does not write a `StopRecord`; the v3 `StopRecord` is written
  by the WP05 release/stop arbiter path when that path is implemented.
- Pre-release snapshots with no release, stop or observation facts remain
  version 2 under the lazy version-selection rule.
- A v3 snapshot is produced only when a WP05 release, stop or trusted-observation
  transition is first written.
- If a later implementation routes WP04 abort through the WP05 arbiter and makes
  it emit v3, that is an explicit WP05-scoped behavior change requiring focused
  WP04 regression evidence before acceptance. This ADR does not authorize that
  routing.
- v2 decoding is preserved and focused regressions must prove existing WP04
  journal behavior. No WP04 requalification is required unless a focused
  regression exposes a concrete counterexample.

## 18. Protocol impact

This schema amendment changes no owner protocol, coordinator protocol,
descriptor map or canonical G2 vector. The future WP05 implementation will add
the release frame, post-release status handling and the no-target qualification
coordinator; those are separate wire changes with their own review and
qualification. If the implementation needs a new coordinator syscall, that is a
separate seccomp classification and review, not part of this schema.

## 19. Future schema test plan (not implemented here)

| Future test | Required property |
|---|---|
| v2 decode/round-trip | Existing v2 records retain exact field meaning; release/stop/observations absent. |
| v2 → v3 lazy upgrade | A Prepared v2 record becomes v3 with null release/stop, `ExecTag=0`, `RootTag=0`; identity/allocations unchanged. |
| v3 release/stop round-trip | All release/stop fields encode/decode with exact values. |
| v3 observation round-trip | Every valid `ExecTag`/`ExecBasis`/`RootTag`/`RootKind` combination encodes/decodes. |
| Version/enum refusal | Version 4+, unknown enum values, reserved values and invalid presence tags fail closed. |
| Invalid transition refusal | Intent without Prepared/Preparation; committed without intent; delivery before commit; committed with `NotAttempted`; observation without committed release; contradictory observations; deadline inversion. |
| Identity immutability | Release/observation transitions cannot change preparation/manifest/generation identity. |
| Deadline immutability | Intent/committed deadlines cannot change across snapshots. |
| Duplicate release commitment | A second logical commit is refused/idempotent per the arbiter; no second write. |
| Release/stop winner | Stop-first has no intent; intent-first remains conservative; later stop never rewrites history. |
| Intent/stop serialization | A: intent then stop preserves both. B: stop-first refuses a later intent. C: stale baseline cannot erase a newer fact. |
| Committed/stop serialization | Later stop preserves intent/commit/delivery; stop-before-commit prevents release. |
| Observation merge | Exec/root writes preserve release/stop/deadlines; later stop/cleanup preserves observations. |
| Crash-state classification | R0/S0/R1/R2 plus observation timing matrix matches the recovery matrix. |
| Writer fencing | Stale/competing writers cannot append release/stop/observation state; wrong sequence/baseline fails. |
| Cleanup/result independence | Cleanup Confirmed coexists with release `MayHaveExecuted`; cleanup preserves all facts. |
| Malformed recovery | Unsupported/corrupt/contradictory records quarantine/Unknown; no destructive adoption. |

## 20. Future process-loss plan (not executed here)

| Barrier | Durable observation | Expected classification |
|---|---|---|
| 1. Before `ReleaseIntent` | R0/S0 | Release definitely impossible; `NotCommitted`; root `NotStarted`. |
| 2. After `ReleaseIntent`, before the gate | R1 | `MayHaveExecuted`; no resend. |
| 3. Inside the gate before/after the in-memory commit | R1 (durable points are indistinguishable) | Same as 2; the in-memory distinction is not durably observable. |
| 4. After the single release write, before the committed record | R1 | `MayHaveExecuted`; delivery unknown; no resend. |
| 5. After the committed record | R2 + delivery result | `MayHaveExecuted`; delivery per record; no resend. |
| 6. After exec observation, before persistence | Prior durable state | Observation lost; recovery must not infer it from the status pipe. |
| 7. After exec persistence, before root | R2 + exec observation | Exec classification from durable evidence; root `Unknown` unless a later durable root exists. |
| 8. After root persistence | R2 + exec + root | Root classification from durable evidence; release remains `MayHaveExecuted` after owner loss. |
| 9. Abort/release race | S0, S1 or S2 | Stop-first no release; release-first history preserved; crash unresolved -> conservative. |

There is no production delivery acknowledgement distinct from the owner's single
write result, so a separate "before delivery acknowledgement" barrier collapses
into barrier 4/5. A test-only coordinator acknowledgement is qualification
evidence, not durable release truth.

## 21. First implementation tranche boundary

After this ADR is independently rereviewed, a first schema implementation
tranche is allowed to implement only:

- the version-3 data model;
- stable durable enum encodings from §3.2;
- the v3 encoder/decoder and v2 compatibility;
- invariant validation from §4.2 and §12;
- monotonic full-snapshot update helpers;
- durable release, stop, exec and root records;
- focused journal/writer-fence/decoder tests, including stale-baseline refusal,
  v2 compatibility, invalid transitions and cleanup preservation.

It must not automatically include:

- release transport or the release arbiter's native gate;
- the coordinator `RELEASE` protocol or post-release branch;
- owner release wiring;
- target execution;
- WP05 qualification or G2b.

Those follow only after the durable representation is independently qualified.

## 22. Explicitly deferred

- Production implementation of `ReleaseArbiter`, `ReleaseDecision`,
  `CoordinatorProtocol`/`CoordinatorStatusReader`, owner wiring, release frame
  and coordinator post-release branch.
- Schema migration implementation (beyond the lazy v2→v3 transition), codec
  changes and all tests in §19/§20.
- WP05 qualification, G2b review, WP06–WP09 and WP10 target execution.
- Terminal receipt persistence and journal retirement (WP09).
- Any seccomp change or the deferred wrong-audit-architecture generator decision.
- Acceptance of this revision or amendment of the authoritative plan. A later
  implementation tranche must not begin until this revision is independently
  rereviewed and the plan records the accepted schema.

## 23. Disposition

ADR 0004 revision 2 incorporates the supplied independent-review clarifications
and was accepted by the independent clarification rereview. WP05 v3 schema
implementation may proceed under the bounded first-tranche scope in §21.
Implementation has not begun.
