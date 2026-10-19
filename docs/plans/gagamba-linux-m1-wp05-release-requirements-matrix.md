# WP05 Release Requirements and Evidence Matrix

Date: 2026-10-19
Binding: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040`
Scope: bounded WP05 release/stop/intent/status/crash qualification; no release acceptance

Status key:

- **IT** — implemented and supported by implementation-time tests (not production-independent).
- **P** — partially implemented/tested; the named boundary or implementation is incomplete.
- **UT** — implemented but untested at the required boundary.
- **D** — explicitly deferred by the authoritative plan (WP08/WP10/auxiliary), not counted as mandatory WP05.
- **IQ** — independently qualified at the complete production boundary (none).
- **M** — missing implementation in the bounded component scope (none).
- **C** — contradicted by evidence (none).

The Q3 independent harness run and its 3/3 test-only release-path execution are
**not** IQ. No row may be promoted to IQ from the harness qualification.

## Matrix

| ID | Requirement | ADR/plan | Source / symbol | Existing test / evidence | Status | Missing proof / gate |
|---|---|---|---|---|---|---|
| R01 | Durable ReleaseIntent is acknowledged before the release gate/write. | ADR 0004 §4.1, §5, §6; plan §§8.2,14.1 | `Execution/ReleaseArbiter.cs:81-115`; `Recovery/OwnershipTransitions.cs:10-64` | `ReleaseArbiterTests.ValidReleaseWritesExactlyOnceAndPersistsCommit`, `NewArbiterCannotResendOverDurableIntent`; Q3 test-only path | IT | Fresh-process crash after intent; production owner composition (R19) |
| R02 | At most one release winner; at most one physical RELEASE write. | Plan §8.1/8.2; ADR §5/§6 | `ReleaseArbiter.cs:45-183`; `HeldExecution.cs:45-64`; `ReleasePipeWrite.cs:23-60` | `Valid...`; `DuplicateStop...`; Q3 test-only path | P | Concurrent duplicate callers; no second-write witness; gate A |
| R03 | Stop accepted before release forbids all writes and preserves a durable stop. | Plan §§8.2,13; ADR §8 | `ReleaseArbiter.cs:55-61,125-131,226-296` | `StopFirstForbidsReleaseWithoutIntent`, `StopWinsAfterDurableIntentBeforeGate` | IT | Real domain stop side effect in driver; capability invalidation R08 |
| R04 | Release before a later stop preserves Intent/Committed/Delivery; stop cannot rewrite history. | Plan §8.2; ADR §8 | `ReleaseArbiter.cs:176-223,226-296` | `StopAfterReleasePreservesCommittedHistory`; `ReleaseRecoveryClassificationTests.StopAfterCommit...` | IT | Fresh-process stop-after-commit; actual close/stop integration |
| R05 | Expiry before intent and inside the gate forbids release; intent after expiry is not written. | Plan §§8.2,12; ADR §9 | `ReleaseArbiter.cs:64-70,81-115,134-146` | `ExpiredSetupDeadlineRefusesBeforeIntent`, `ExpiryAfterIntentRefusesAtGateWithoutWrite`; Q3 expiry test | IT | Combined expiry/stop race; real clock boundary |
| R06 | Repeated release/stop calls are idempotent and retries cannot reissue RELEASE. | Plan §8.2; ADR §5/§6 | `ReleaseArbiter.cs:48-55,117-131,226-235` | `Valid...` (sequential duplicate), `DuplicateStop...`; `NewArbiterCannotResend...` | P | Concurrent duplicate semantics; real process retry; no-resend witness |
| R07 | Release requires the exact prepared identity and a live single-use capability; forged/stale requests are refused without transport. | Plan §8.2; ADR §4/§5 | `ReleaseArbiter.cs:64-115,149-155,319-334`; `PreparedTransaction.TryConsume` | `ForgedIdentityGateRefusal...` (Q3), `MissingPrerequisites...`, `NewArbiterCannotResend...` | IT | Fresh-process capability absence; no production owner |
| R08 | A winning stop permanently aborts/invalidates the release capability. | Plan §6/§13 (permanent abort) | `ReleaseArbiter.cs:226-296` (no consume); `ReleaseArbiter.cs:55-61` (early refusal) | `StopFirstForbidsReleaseWithoutIntent` (does not assert `IsLive`) | P | Source gap: capability remains live; add explicit test and decision |
| R09 | Journal failure/ambiguity before intent forbids release; ambiguous persistence requests stop. | Plan §§8.2,14.1; ADR §6 | `ReleaseArbiter.cs:110-114`; `OwnershipJournal.cs:358-495` (seal ambiguity at 486-490) | `FailedIntentPersistenceForbidsRelease` (definite fake failure only) | P | Ambiguous-vs-definite fault test; source currently collapses both to NotCommitted without stop |
| R10 | ReleaseCommitted is written only after the gate and one write attempt; delivery result is separate. | ADR §3/§5/§6; plan §8.2 | `ReleaseArbiter.cs:157-223`; `ReleaseDecision.cs:38-44` | `Valid...`; Q3 test-only path | IT | Committed write failure R11; production owner R19 |
| R11 | A failed committed-evidence write after release remains conservative and does not trigger resend. | ADR §6 (step 6) | `ReleaseArbiter.cs:185-223` | none | UT | Fault-inject committed journal write after delivered FD4; verify intent remains and no retry |
| R12 | RELEASE is exactly one 128-byte frame with frozen identity/deadline layout and golden hash. | Plan §8.1; ADR §6 | `CoordinatorProtocol.cs:107-186`; `native/linux-m1/release_protocol.h` | `ReleaseFrameTests` (golden/mutations); `Native/ReleaseProtocolTests` | IT | Production coordinator malformed/extra cases through adapter; no target |
| R13 | FD4 is a single nonblocking write; full/EAGAIN/EPIPE/partial/unknown are classified and never retried. | Plan §8.1/8.2; ADR §6 | `ReleasePipeWrite.cs:23-60`; `HeldExecution.cs:45-64` | `ReleaseArbiterTests.TransportFailures...`; `Native/ReleaseProtocolTests.Nonblocking...`; Q3 path | IT | Real production write witness; no owner crash |
| R14 | FD4/FD5 topology and lifetime isolate release/status from target; held path owns one writer/reader. | Plan §8.1/§9.1; ADR §3 | `BubblewrapAdapter.cs:70-160,209-230`; `HeldExecution.cs:9-78` | Q3 `HeldProductionCoordinator...` (real adapter, pre-exec); WP04 held tests | IT | FD5 status is never consumed in this path; production owner R19 |
| R15 | FD5 status frames validate magic/version/kind/length/sequence/execution/preparation/digest and required order. | Plan §8.1; ADR §4.2 | `CoordinatorProtocol.cs:188-239`; `CoordinatorStatusReader.cs:23-221` | `CoordinatorStatusReaderTests` (11); `Native/ReleaseProtocolTests.Valid...` | IT | Native real FD5 + adapter owner; production status R20 |
| R16 | FD5 rejects malformed/truncated/EOF/replay/duplicate/out-of-order/wrong-identity/status-after-terminal cases. | Plan §8.1/§13; ADR §4.2 | `CoordinatorStatusReader.cs:56-221`; `FdStatusFrameSource.cs:232-305` | `Malformed...`, `OutOfOrderAndDuplicate...`, `ProtocolFailed...`; no actual FD5 EOF | P | Actual FD5 EOF/replay/duplicate-root and coordinator-death native cases |
| R17 | Durable Exec/Root observations are allowed only after Committed, are ordered, single-assignment and never inferred from delivery. | ADR §4.2/§5 | `ReleaseObservationBuffer.cs:9-145`; `OwnershipTransitions.cs:153-241`; `OwnershipValidation.cs:184-374` | `BufferDoesNotPersistBeforeCommitted...`, `BufferRefuses...`, `ObservationsMerge...` | IT | No production composition; crash after observation before persistence |
| R18 | Missing/lost status yields Unknown/MayHaveExecuted; Committed without trusted evidence is not success. | Plan §13; ADR §4.2/§13 | `ReleaseRecoveryClassification.cs:22-114`; `ReleaseObservationBuffer.cs` | `ReleaseRecoveryClassificationTests` (unknown/committed); buffer tests | IT | Fresh process observation loss; no false Committed/run claim |
| R19 | Production owner calls the release gate/transport/observation machinery. | Plan WP08/WP10; G3 | Absent at R12: owner path `OwnerPreparationTransaction.cs:355-395` only HeldReady/Abort | Source search: `ReleaseArbiter`/`ReleaseObservationBuffer`/`CoordinatorStatusReader` have no production caller | D | WP08/WP10; not a bounded WP05 test requirement |
| R20 | Production coordinator emits post-release Exec/Root status and the owner durably projects it. | Plan WP10; G3/D | Production coordinator stops pre-exec (`exec-coordinator.c:176-205`); status only test qualification coordinator | `Native/ReleaseProtocolTests` uses test-only coordinator; no production status | D | WP10 for actual target/status; bounded status test remains R15-R18 |
| R21 | Fresh owner crash before intent recovers NotCommitted/NotStarted and never writes. | Plan §6/§13; ADR §7 | `ReleaseRecoveryClassification.cs:73-78`; owner crash tests are preparation-only | `OwnerDeathQualificationTests` D1-D6 (preparation); no release crash | UT | Fresh-process release driver; crash before intent |
| R22 | Fresh owner crash after durable intent before gate/write recovers MayHaveExecuted/Unknown and never resends. | Plan §6/§13; ADR §7 | `ReleaseRecoveryClassification.cs:36-38,80-83`; `ReleaseArbiter.cs` barriers | `CrashAfterIntent...` throws exception in same process; no fresh process | UT | Real SIGKILL after intent; replacement recovery; no-resend witness |
| R23 | Fresh owner crash after FD4 write before committed recovers conservatively and never resends. | Plan §6/§13; ADR §7 | `ReleaseArbiter.cs:157-223`; `OwnershipJournal.cs` | `CrashAfterWrite...` exception; no fresh process | UT | Real kill at write boundary; witness before/after write |
| R24 | Fresh owner crash after committed/observation preserves durable evidence. | Plan §6/§13; ADR §7 | `ReleaseArbiter.cs:185-223`; `ReleaseObservationBuffer.cs`; classifier | `CrashAfterCommitted...` exception; no fresh process | UT | Real kill after committed/observations; replacement reads journal |
| R25 | No second RELEASE is issued after ambiguous owner loss once durable intent exists. | Plan §§6,8.2,13; ADR §§7,13 | `ReleaseRecoveryClassification.cs`; no recovery release method; owner absent | `NewArbiterCannotResend...` (same process/new capability) | UT | Fresh recovery, independent FD4 attempt/acceptance witness |
| R26 | StopDeadline is exactly StopAcceptedAt + 5,000,000,000 ns, persisted and immutable. | ADR §9.1; plan §14.1 | `OwnershipTransitions.cs:108-150`; `OwnershipValidation.cs:145-181`; `ReleaseArbiter.cs:246-266` | `OwnershipV3TransitionTests.SecondStopCannotRewrite...`; `Wp05V3Independent...StopDeadline...` | IT | No production consumer; see R27 |
| R27 | Recovery/takeover uses the remaining persisted stop allowance and cannot reset it. | ADR §9.1; plan §14.1 | `Reconciler.cs:121-130,231` uses caller `_stopAllowance`; no `StopDeadline` read found | none | P | Confirmed source gap: persisted deadline not consumed; remediation/test decision |
| R28 | Cleanup truth is independent of release/execution truth and cannot manufacture success. | Plan §13/§14.2; ADR §13/§14 | `ReleaseRecoveryClassification.cs:100-113`; `Reconciler.cs` | `CleanupConfirmedDoesNotRewrite...`; `ReconcilerTests` | IT | Integrated release+cleanup crash; actual domain/reap observation in G3-C |
| R29 | Boot/cross-boot records are non-authority; same-boot monotonic deadlines only. | ADR §10; plan §10.2 | `Reconciler.cs:75-96`; `OwnershipValidation`/record | `Wp05V3...`; recovery planner tests | IT | Release-specific cross-boot record; no release authority |
| R30 | The 35 root-only auxiliary rows are accounted for without hiding a mandatory release row. | Harness Q2/Q3; plan WP02-WP04 | `IntegratedBarrierTests`, `DomainAuthorityTests`, `RecoveryHarness`; Q2 537/35 | Q2 skips: IntegratedBarrier 14, DomainAuthority 19, RecoveryHarness 2 | D | Auxiliary only; no mandatory WP05 release row in this set |
| R31 | Actual target execution and production post-release status. | Plan WP10 | Not implemented at R12; production coordinator pre-exec | none | D | WP10; never claim from WP05 |

## Counts (supported by the table)

- Rows: 31.
- Mandatory bounded WP05 rows (R01–R18, R21–R29): **27**.
- Implemented and supported by implementation-time tests (**IT**): **15**.
- Partially implemented/tested (**P**): **6**.
- Implemented but untested at the required boundary (**UT**): **6**.
- Explicitly deferred (**D**): **4** (R19, R20, R30, R31; excluded from the 27).
- Independently qualified at the complete production boundary (**IQ**): **0**.
- Missing implementation in the bounded component scope (**M**): **0**.
- Contradicted by evidence (**C**): **0**.

The counts describe evidence status, not acceptance. The Q3 test-only release path
3/3 is independent execution of an implementation-owned pre-exec composition; it
does not convert R01–R18 to IQ and does not cover R21–R25.

## Gate mapping

- **RQ-A / proposed G3-A** (arbitration): R01–R09, R26, R27.
- **RQ-B / proposed G3-B** (intent/transport): R09–R14, R26.
- **RQ-C / proposed G3-C** (fresh process/no-resend): R21–R25.
- **RQ-D / proposed G3-D** (trusted FD5): R15–R18, R20 boundary.
- **RQ-E / proposed G3-E** (composed disposition): all mandatory rows; WP10
  owner/target scope remains explicit.
