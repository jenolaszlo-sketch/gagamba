# Gagamba Linux M1 — WP05 Release Qualification Gap Audit

Date: 2026-10-19
Status: documentation-only audit and plan input; no release qualification executed

## Disposition

**`WP05 RELEASE PROTOCOL COMPONENTS IMPLEMENTED; PRODUCTION OWNER/POST-RELEASE STATUS DEFERRED; QUALIFICATION NOT ESTABLISHED`**

**WP05 RELEASE IS NOT ACCEPTED.** The independently qualified harness remains a
frozen dependency; this audit does not reopen it and does not qualify the release
protocol.

## 1. Binding and scope

- Canonical root: `C:\Users\Laszlos\source\repos\Gagamba`
- Planning branch: `planning/wp05-release-protocol-qualification`
- Branch base: preservation HEAD `86eee0d9362fc2785c4d7997fec377b7a33c7767`
- Canonical preservation branch: `preservation/wp05-harness-independent-acceptance`
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R11: `c48e358451c8b0dbe7aace281f64a2eced399ed7`
- R12: `38f6981b2a60daab2df37c92913a6760c1bef131`
- R12 tree: `6854675d6b37a49d5302d6104103bdbfbc268040`
- Frozen harness review reference: `review/wp05-opencode-harness-final-qualification`
  at Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f`
- Harness disposition: **`WP05 HARNESS QUALIFIED (bounded)`**, as preserved at
  `86eee0d`. This audit does not reopen it.

Authoritative inputs read for this audit: `docs/plans/gagamba-linux-m1-implementation-plan.md`
(especially Sections 5, 6, 8, 13, 14 and WP05/G2/G3), `docs/adr/0003-ar7-linux-first-restricted-execution.md`,
`docs/adr/0004-wp05-durable-release-schema-amendment.md` (accepted revision 2),
`docs/evidence/linux-m1/wp05-release-protocol-implementation-2026-10-18.md`,
`docs/reviews/gagamba-linux-m1-wp05-v3-schema-final-acceptance-review.md`,
the Q3 independent review record, the preservation checkpoint, WP04/G2 acceptance
records, and current work queue/handoff.

### 1.1 Authoritative boundary and reconciliation

The implementation plan assigns WP05 to the release components and bounded
pre-exec behavior, WP08 to owner orchestration, WP09 to reconciliation/integration
readiness, and WP10 to the production owner release wiring and actual target.
WP05 explicitly must not require WP10 behavior. The current production owner has
no release call; that is consistent with the plan, not by itself a WP05 defect.

The requested `G3-A`–`G3-E` labels are **planning sub-gate labels** for this
proposal, not amendments to the authoritative plan. The authoritative review
gates are:

- G2a (WP05): actual pinned helper transport and held coordinator; target cannot
  initialize; EOF/invalid control fails closed.
- G2b (WP06): release-vs-stop proof, single-write behavior, crash-intent rule,
  child exec/error logic, truthful target-vs-wrapper status.
- G3 (WP10): composition of WP05–WP09 with actual resources/readbacks, capture,
  caller/owner loss, setup deadlines, journal ordering and reconciliation; only
  accepted G3 permits integrated target release.

The proposed `G3-A`–`G3-E` labels must therefore be treated as **pre-G3 WP05
release sub-gates**. They do not replace G2a/G2b/G3, and the full G3 gate remains
blocked by WP08/WP09/WP10. This distinction is load-bearing.

The request mentions “actual WP05 release protocol”; the authoritative plan says
the live integrated path still cannot release and WP10 enables target execution.
This audit follows the plan: a test-only release composition is in scope for
WP05 qualification; production owner wiring and actual target execution remain
WP10 and are not demanded here.

## 2. Production call graph (R12)

### 2.1 Owner path

The production owner path prepares, reports HeldReady, waits for AbortHeld and
aborts. It has no release call:

```text
Owner/Program.cs
  -> OwnerPreparationTransaction.Run(...)
     -> M1RecoveryScope / writer handoff
     -> BubblewrapAdapter.PrepareHeldAsync(...)              // Held setup
        -> HeldExecution (FD4 writer, FD5 status reader)
     -> send OwnerProtocolMessageType.HeldReady
     -> receive OwnerProtocolMessageType.AbortHeld
     -> BubblewrapAdapter.AbortAsync(...)
        -> domain StopAndAwaitEmpty + wrapper reap + drains
```

Source anchors:

- `src/Gagamba.Execution.Linux.M1.Owner/Program.cs`: bootstrap validation and
  `OwnerPreparationTransaction.Run`.
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs:355-395`:
  `new BubblewrapAdapter()`, `PrepareHeldAsync`, `HeldReady`, `AbortHeld`,
  `AbortAsync`. No `ReleaseArbiter`, `ReleasePipeWrite`, or release decision.
- `src/Gagamba.Execution.Linux.M1/Launch/IHeldLauncher.cs:7-24`: the interface
  exposes only `PrepareHeldAsync` and `AbortAsync`; no release method.
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs:70-110`: FD map,
  including FD4 `ControlReader`, FD5 `CoordinatorStatusWriter`; lines 156-160
  create `HeldExecution`; lines 209-230 abort/drain/reap.
- `src/Gagamba.Execution.Linux.M1/Launch/HeldExecution.cs:9-78`: owns the sole
  writer and status reader; `TryWriteRelease` permits one attempt.

The owner path has no release transport invocation. This is the plan's WP08/WP10
boundary and means an integrated owner crash-after-release cannot be exercised
by the current production owner.

### 2.2 Test-only release path

The only actual release call graph is test-owned:

```text
Wp05ReleasePathIndependentQualificationTests
  -> ReleaseArbiter.RequestRelease
     -> OwnershipJournal.WriteTransition (ReleaseIntent; FD-independent)
     -> internal gate: prerequisite/stop/expiry/identity checks
     -> PreparedTransaction.TryConsume
     -> HeldExecution.TryWriteRelease
        -> ReleasePipeWrite.WriteOnce -> FD4 (nonblocking, one 128-byte frame)
     -> OwnershipJournal.WriteMutation (ReleaseCommitted/delivery result)
```

Source anchors:

- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs:44-79,97-127,181-198`:
  constructs `ReleaseArbiter` with a real journal, real prepared capability and
  real `HeldExecution`; the three tests exercise release, forged-identity refusal
  and expiry.
- `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs:45-224`:
  `RequestRelease`; lines 81-115 durable intent; 117-183 gate/consume/write;
  185-223 committed snapshot; 226-297 `RequestStop`.
- `src/Gagamba.Execution.Linux.M1/Launch/ReleasePipeWrite.cs:23-60`: one
  nonblocking write and errno mapping.
- `src/Gagamba.Execution.Linux.M1/Launch/CoordinatorProtocol.cs:110-186`:
  128-byte release frame encode/decode.
- `src/Gagamba.Execution.Linux.M1/Launch/HeldExecution.cs:45-64`: one-shot
  transport (`AlreadyAttempted` on a second call).

There is no production caller for `ReleaseArbiter`. Repository search at R12
finds it only in `Execution/ReleaseArbiterTests.cs` and
`Recovery/Wp05ReleasePathIndependentQualificationTests.cs`.

### 2.3 Native coordinator and FD5

- Production coordinator: `native/linux-m1/exec-coordinator.c:176-205`. It reads
  exactly 128 bytes from FD4, rejects extra input, validates the frame through
  `native/linux-m1/release_protocol.h:31-51`, closes FD4, and returns 0 at the
  pre-exec boundary. It never forks/execs a target and emits no EXEC/ROOT status.
- FD5 is the trusted coordinator status writer in
  `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs:96-110`; the owner
  side is a `SafeFileHandle` in `HeldExecution.cs:36`.
- `CoordinatorStatusReader` and `ReleaseObservationBuffer` are implemented at
  `src/Gagamba.Execution.Linux.M1/Launch/CoordinatorStatusReader.cs:23-306` and
  `src/Gagamba.Execution.Linux.M1/Execution/ReleaseObservationBuffer.cs:9-145`.
  Neither has a production caller.
- The only native post-release status generator is the test-only
  `tests/Fixtures/wp05-qualification-coordinator.c` (modes `exec_failed`,
  `exec_confirmed`, `protocol_failed`, `root_unconfirmed`,
  `exec_confirmed_root`, `exec_failed_root`). `Native/ReleaseProtocolTests.cs`
  compiles and runs it. This binary is never selected by production.
- The qualification coordinator is not a held coordinator: it has no HELD report.
  `ReleaseProtocolTests` therefore exercises status codec/reader behavior over
  standalone pipes, not an owner/adapter-held execution.

### 2.4 Durable path and recovery

```text
ReleaseArbiter
  -> OwnershipTransitions.TryPlanReleaseIntent / TryPlanReleaseCommitted / TryPlanStop
     -> OwnershipJournal.WriteTransition / WriteMutation
        -> OwnershipSnapshot v3 (ReleaseRecord/StopRecord/Exec/Root)
ReleaseRecoveryClassifier.Classify(snapshot, domainEmpty, captureComplete)
Reconciler.Reconcile(snapshot, boot, liveLease, cleaner, journal, writer)
  -> RecoveryPlanner.Plan(boot/current lease)
  -> DomainLease.StopAndAwaitEmpty(_stopAllowance)
```

Source anchors: `Recovery/OwnershipRecord.cs:43-193`,
`Recovery/OwnershipTransitions.cs:10-269`,
`Recovery/OwnershipJournal.cs:358-495,550-690`,
`Recovery/ReleaseRecoveryClassification.cs:22-114`,
`Recovery/Reconciler.cs:55-180,212-470`.

`ReleaseObservationBuffer` is not called by `ReleaseArbiter` or the owner.
`ReleaseRecoveryClassifier` is pure and accepts `domainEmpty`/`captureComplete`
booleans; it does not itself observe those facts.

## 3. Durable transition inventory

The accepted ADR 0004 shapes are preserved. The audit-relevant transitions are:

| Step | Initiator | Durable state before | Effect | First irreversible effect | Actual R12 implementation |
|---|---|---|---|---|---|
| Release request precheck | `ReleaseArbiter.RequestRelease` | Prepared / no release | Validate identity, deadlines, prerequisites, capability | none | Implemented; tests |
| Release intent | `ReleaseArbiter` -> `TryPlanReleaseIntent` -> `WriteTransition` | Prepared / R0 | R1 Intent with intent/setup/overall times | intent fsync/acknowledgement | Implemented; test-only composition |
| Gate recheck/consume | `ReleaseArbiter` in `_gate` | R1 | stop/expiry/identity checks; capability consumed; runtime `ReleaseCommitted` | capability consumption | Implemented; tests |
| FD4 write | `ReleaseArbiter` -> `HeldExecution` -> `ReleasePipeWrite` | R1/live gate | exactly one nonblocking 128-byte write | physical RELEASE delivery | Implemented; test-only path |
| ReleaseCommitted | `ReleaseArbiter` -> `TryPlanReleaseCommitted` -> `WriteMutation` | R1 | R2 + delivery result | evidence only; not authority | Implemented; tests |
| Stop | `ReleaseArbiter.RequestStop` -> `TryPlanStop` -> `WriteMutation` | R0/R1/R2 | S0/S1/S2; first cause/deadline | stop record | Implemented at record/decision level; no domain/transport side effect in arbiter |
| Exec/root observation | `ReleaseObservationBuffer.TryFlush` -> `TryPlanExec/Root` | R2 | durable exec/root or refusal | observation evidence | Implemented; not composed by owner |
| Recovery classification | `ReleaseRecoveryClassifier.Classify` | any valid durable snapshot | release/exec/root/cleanup classification | none | Implemented; pure tests |
| Recovery cleanup | `Reconciler.Reconcile` | any valid same-boot record + live lease | domain stop/empty/removal; cleanup facts | domain stop | Implemented; not release-specific |

The first irreversible release side effect is the FD4 write, not the durable
intent or committed record. Durable intent is the conservative safety boundary;
committed evidence is written after the write. `DeliveryResult=Delivered` proves
only a full pipe write, not coordinator consumption, exec or target execution.

## 4. Authority and arbitration model

- Runtime release winner: `ReleaseArbiter._gate` plus
  `_releaseRuntimeCommitted` and `PreparedTransaction.TryConsume`
  (`ReleaseArbiter.cs:45-183`). The first stop accepted in the gate sets
  `_stopAccepted`; release after an accepted stop is refused.
- Durable safety boundary: acknowledged `ReleaseIntent` (`ReleaseArbiter.cs:81-115`,
  `OwnershipTransitions.cs:10-64`; ADR 0004 §4.1/§5). After owner loss, any
  intent/committed record yields `ReleaseDisposition.MayHaveExecuted`
  (`ReleaseRecoveryClassification.cs:36-38`), never `NotCommitted`.
- Stop: `ReleaseArbiter.RequestStop` sets the live stop flag and writes the first
  `StopRecord` (`ReleaseArbiter.cs:226-296`). The five-second deadline is
  calculated and persisted by `TryPlanStop`/validator
  (`OwnershipTransitions.cs:108-150`, `OwnershipValidation.cs:145-181`).
- Durable writes are serialized by `_journalGate`; the journal writer lease and
  full-snapshot `WriteTransition`/`WriteMutation` enforce baseline/sequence
  fencing (`ReleaseArbiter.cs:81-115,194-211,272-289`; `OwnershipJournal.cs:358-495,550-690`).
- `ReleaseArbiter` does not stop the domain or close the release writer itself;
  those effects belong to an owner/driver. `HeldExecution.CloseAbortControl` and
  `BubblewrapAdapter.AbortAsync` are separate (`HeldExecution.cs:65-78`,
  `BubblewrapAdapter.cs:209-230`).
- No production owner release integration exists; the test-only release path
  constructs the arbiter around a live `HeldExecution`.

## 5. Existing test inventory

| Area | File/class | Tests | Reality | Coverage / limits |
|---|---|---|---|---|
| Release arbitration | `Execution/ReleaseArbiterTests.cs` | 16 | fake journal/transport; fake clock | intent, gate, stop, expiry, transport mapping, exception barriers, duplicate/retry model; no real process or FD5 |
| RELEASE frame | `Launch/ReleaseFrameTests.cs` | 6 facts + theory cases | codec | exact 128 bytes, golden SHA, identity/deadline/reserved mutations |
| Status reader/order | `Launch/CoordinatorStatusReaderTests.cs` | 11 | synthetic queue; fake journal buffer | valid exec/root, failed exec, unknown root, malformed/out-of-order/duplicate, buffer pre/post commit |
| Coordinator HELD protocol | `Launch/CoordinatorProtocolQualificationTests.cs` | 5 facts + theory | synthetic frames | frame header/HELD payload rejection; not release/status |
| Native release protocol | `Native/ReleaseProtocolTests.cs` | 4 | real test-only coordinator + pipes | valid release + synthetic exec/root; malformed/extra/expired; one-shot transport; no owner/adapter/HELD |
| Recovery classification | `Recovery/ReleaseRecoveryClassificationTests.cs` | 11 | pure snapshot | conservative release, exec/root combinations, stop, cleanup independence, timestamp non-ordering |
| V3 transitions | `Recovery/OwnershipV3TransitionTests.cs` | 18 | fake journal/storage | intent, stop, commit, observations, stale baseline, immutability, stop deadline |
| V3 codec/independent probes | `Recovery/OwnershipV3CodecTests.cs`, `Wp05V3IndependentQualificationTests.cs` | 9 + 5 | codec/fake journal | enum/round-trip/refusal, v2→v3, stop deadline overflow, hostile bytes |
| Real test-only release path | `Recovery/Wp05ReleasePathIndependentQualificationTests.cs` | 3 | real journal, real capability, real `BubblewrapAdapter`/`HeldExecution`, production coordinator | release through real FD4 to pre-exec; forged identity; expiry; no post-release status, owner process or fresh crash |
| Preparation owner death | `Recovery/OwnerDeathQualificationTests.cs` | 7 | real process death, preparation barriers | D1–D6 before/through Prepared/Mint; no release/stop/post-release crash |
| Process-global isolation | `Recovery/IntegratedBarrierTests.cs`, `NonRootQualificationTests.cs`, others | suite | real delegated Linux | supporting lifecycle; not release rows |

Evidence anchors: `docs/evidence/linux-m1/wp05-release-protocol-implementation-2026-10-18.md`
records 16/16 arbitration, 17/17 frame cases, 11/11 status, 4/4 native protocol,
98/98 focused and explicitly says it is not independent WP05 qualification. The
Q3 independent review executed the full supported profile twice (537 passed,
35 root-only skips, zero failed) and the test-only release path 3/3; it did not
qualify full WP05 release. Q1 hostile probes target the harness oracle, not the
release protocol.

## 6. Mandatory gaps and candidate defects

Confirmed source-level discrepancies relative to the accepted plan/ADR (no
runtime counterexample was executed; these are code-path findings):

1. **Durable stop deadline is not consumed by recovery.** `StopRecord` calculates
   and persists the five-second deadline (`OwnershipTransitions.cs:108-150`,
   `OwnershipValidation.cs:145-181`), but `Reconciler` uses a caller-supplied
   `_stopAllowance` (`Reconciler.cs:121-130,231`) and does not read
   `record.Stop.StopDeadlineNanoseconds`. No production path is found that
   converts the persisted absolute deadline into remaining recovery allowance.
   This conflicts with ADR 0004 §9.1 and the no-reset requirement unless a caller
   supplies the remaining time correctly; that caller is absent. **Status:
   confirmed source-level gap; mandatory test/remediation decision.**
2. **Stop-first does not invalidate the capability in `ReleaseArbiter`.**
   `RequestStop` sets `_stopAccepted` and writes the stop record but never calls
   `TryConsume`/invalidation (`ReleaseArbiter.cs:226-296`). A subsequent
   `RequestRelease` returns early at lines 55-61 without consuming; the
   capability can remain live. The plan requires the capability to be
   permanently aborted on a winning stop (plan lines 150, 310, 326). **Status:
   confirmed component-level discrepancy; no test currently asserts
   `capability.IsLive == false` after stop-first.**
3. **Ambiguous intent persistence is collapsed to a non-committed decision.**
   `OwnershipJournal.Write` returns failure for an ambiguous seal release
   (`OwnershipJournal.cs:486-490`), but `ReleaseArbiter.RequestRelease` treats
   every `WriteTransition` failure as “intent was not durably acknowledged”
   and returns without requesting stop (`ReleaseArbiter.cs:110-114`). The plan
   says a failed/ambiguous flush forbids release and requests stop (plan line
   209). The component forbids release but does not request/perform stop.
   **Status: confirmed code-path discrepancy; ambiguous-vs-definite failure is
   untested.**
4. **Concurrent duplicate release semantics are unproven.** The retained
   decision is installed only after the gate (`ReleaseArbiter.cs:176-223`); the
   initial duplicate check reads it before the gate (`45-62`). A second
   concurrent caller can receive a `Refused` decision rather than the original
   `Duplicate` result while the first is in the gate. The plan says duplicate
   calls return the original decision without another write (plan line 215).
   No concurrent test exists. **Status: candidate semantic gap; no second-write
   safety violation observed in source (the journal refuses a second intent).**

Missing qualification evidence, not necessarily missing code:

- No production owner release call, no production post-release coordinator
  status, and no actual target; explicitly WP08/WP10.
- No fresh-process release crash matrix; existing “crash” tests throw a test
  exception in the same process (`ReleaseArbiterTests.cs:237-280`) and
  `NewArbiterCannotResend...` mints a new capability in the same process
  (`ReleaseArbiterTests.cs:324-344`).
- No independent no-resend write-attempt/acceptance witness across process death.
- `ReleaseObservationBuffer`/`CoordinatorStatusReader` are not composed with
  `ReleaseArbiter`; no end-to-end trusted status-to-durable observation test.
- No test for committed-snapshot write failure after a successful write
  (`ReleaseArbiter.cs:185-223`).
- No test for stop during committed-write/cleanup, concurrent release/stop
  threads, or release during cleanup.
- No actual FD5 EOF/replay/duplicate-root/wrong-wrapper native case; status
  tests are synthetic/qualification-coordinator only.
- No real target execution/status; WP10.
- The 35 root-only rows are WP02/WP03/WP04 auxiliary gates, not WP05 release
  rows; no release test is silently hidden in them. They do not authorize a
  release claim and do not block the bounded WP05 release plan.

## 7. Deferred scope

- WP06 resource installation/readback and WP07 capture are later packages; the
  current `ReleasePrerequisites` are fake booleans.
- WP08 owner orchestration and WP09 integration/reconciliation are not
  implemented. WP10 production owner release wiring, actual target execution,
  and production coordinator post-release status are deferred. This audit does
  not demand them for the bounded WP05 release protocol, and does not claim them.
- Full authoritative G3 remains a WP10 composition gate after WP08/WP09; the
  proposed `G3-A`–`G3-E` labels are planning sub-gates only.
- Cross-host/cross-kernel and Windows release lanes are not qualified.

## 8. Final gap classification

- Release/stop components exist and have substantial implementation-time tests.
- The real test-only release path is independently exercised at the pre-exec
  boundary 3/3, but this is not complete production release qualification.
- The mandatory gaps are durability/no-resend/status integration, stop-deadline
  enforcement, stop-first capability invalidation, ambiguous-intent handling,
  and concurrent duplicate semantics.
- Production owner/status/target work is deferred by the authoritative plan, not
  silently missing.
- **No WP05 release acceptance is supported by the current evidence.**

The requirements matrix counts and classifications are in
`docs/plans/gagamba-linux-m1-wp05-release-requirements-matrix.md`; the executable
gates are in `docs/plans/gagamba-linux-m1-wp05-release-qualification-execution-plan.md`.
