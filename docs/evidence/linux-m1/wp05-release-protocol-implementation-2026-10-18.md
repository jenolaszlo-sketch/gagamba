# WP05 Release Protocol and Arbitration Implementation

Date: 2026-10-18 (Asia/Manila)

Remediation slice: WP05 release protocol and release/stop arbitration on the
accepted v3 durable-schema foundation. This is implementation and focused
validation, not independent WP05 qualification or acceptance.

Baseline source SHA: `c4b80db83e52a5125c022c0e217913654ebe3421`
Implementation SHAs: `4bd2f3a051c2727eb5d221b29a5c24ef2c4def73` (managed
arbitration/protocol), `08cda01bff6b2c455fa9e6b4fc46d2db8527ff49` (no-target
native RELEASE coordinator/tests)
Branch: `qualification/ar7-feasibility`
Host: Windows development host; WSL2 Ubuntu for Linux native protocol tests
Architecture: x86_64
Runtime: .NET 10.0.112; gcc 15.2.0 (WSL)
Qualification scope: managed arbitration/release/status tests, Linux no-target
RELEASE coordinator and pipe tests, v3 regression, focused WP04/journal/canonical
checks
Explicit exclusions: real fixture target execution, WP10, full WP05,
independent qualification, full fresh-process process-loss matrix, later
resource/capture work, owner release orchestration, seccomp widening

## 1. Authoritative binding

[ADR 0004](../adr/0004-wp05-durable-release-schema-amendment.md), its independent
final acceptance review, and plan Section 8.1/8.2 are binding. The accepted v3
schema was not changed.

Repository-native plan-binding answers:

| Question | Answer |
|---|---|
| Arbitration owner | `Execution/ReleaseArbiter.cs` |
| Decision type | `Execution/ReleaseDecision.cs` (`ReleaseDecision`/`StopDecision`) |
| RELEASE descriptor | Child FD 4, coordinator reader; owner holds the sole writer |
| Status descriptor | Child FD 5, coordinator writer; owner reader |
| RELEASE frame | 96-byte `GGM1CTL\0` header + generation UUID[16] + overall deadline u64 + reserved[8] |
| Frame size | Exactly **128 bytes** |
| Identity binding | Header execution UUID, preparation UUID, manifest SHA-256; payload generation UUID; sequence 1 |
| Ordering | One owner→coordinator RELEASE, sequence 1, kind 2; no second frame |
| Deadlines | Overall absolute `CLOCK_MONOTONIC` ns in frame; config binds setup and overall |
| No-target coordinator | Test-only source validates the exact frame and exits before any target fork |
| Pre-WP10 statuses | HELD, EXEC_FAILED, EXEC_CONFIRMED, ROOT_WAIT, PROTOCOL_FAILED; no-target build emits no trusted target status |
| Production vs test | Production coordinator stops at the pre-exec boundary; qualification coordinator is a separate source/binary never selected by production |
| Gate state | Stop flag/cause, runtime release winner, consumed capability, retained decision |
| Fake prerequisites | ResourcesReady, CaptureReady, CallerAlive, RecoveryAlive |

## 2. Files changed

Managed implementation:

- `Execution/ReleaseDecision.cs`
- `Execution/ReleaseArbiter.cs`
- `Execution/ReleaseObservationBuffer.cs`
- `Launch/CoordinatorStatusReader.cs`
- `Launch/ReleasePipeWrite.cs`
- `Launch/CoordinatorProtocol.cs`
- `Launch/HeldExecution.cs`
- `Launch/BubblewrapAdapter.cs`
- `Launch/LaunchDescriptorMap.cs`
- `Preparation/PreparedTransaction.cs`
- `Native/AtomicCgroupSpawn.cs`

Native/test implementation:

- `native/linux-m1/release_protocol.h`
- `native/linux-m1/protocol.h`
- `native/linux-m1/exec-coordinator.c`
- `tests/Fixtures/wp05-qualification-coordinator.c`
- `eng/linux-m1/build-native.sh`
- tests: `ReleaseArbiterTests`, `ReleaseFrameTests`,
  `CoordinatorStatusReaderTests`, `Native/ReleaseProtocolTests`

## 3. Release authority and intent ordering

- Release authority remains the owner-local single-use `PreparedTransaction`
  capability. `PreparedTransaction.TryConsume` now delegates to the existing
  `PreparationStore.TryConsume`; no serialized or recovery-reconstructed
  authority was added.
- `ReleaseArbiter.RequestRelease` validates the exact capability/identity,
  deadlines, fake prerequisites and live snapshot, then durably writes
  `ReleaseIntent` through `OwnershipTransitions.TryPlanReleaseIntent` and the
  existing `OwnershipJournal.WriteTransition` before entering the gate.
- If intent persistence fails, there is no gate entry, no capability
  consumption and no RELEASE write.
- The gate rechecks monotonic expiry, stop flag, prerequisites, liveness,
  identity and capability state. It consumes the capability, changes runtime
  state to ReleaseCommitted, and performs exactly one nonblocking 128-byte
  write before leaving the gate.
- No ownership-journal I/O occurs inside the gate. The committed delivery
  observation is persisted afterward with `WriteMutation` from the latest
  acknowledged snapshot.

## 4. Single-use capability and transport

- The capability is consumed inside the gate through the store's
  reference-identity/issuer/generation/digest/expiry checks.
- `HeldExecution.TryWriteRelease` permits one attempt; a second call returns
  `AlreadyAttempted` without writing.
- `ReleasePipeWrite.WriteOnce` maps a full 128-byte write to `Delivered`,
  `EAGAIN` to `WouldBlock`, `EPIPE` to `BrokenPipe`, a short count to `Partial`,
  and unexpected results to `Unknown`; SIGPIPE is ignored and handled as
  `EPIPE`. No retry exists.
- The control writer is `O_CLOEXEC` and is set nonblocking before Bubblewrap
  admission. FD 4 remains the coordinator-only reader; FD 5 remains the trusted
  status writer. Neither is transferred to the target.
- The accepted `PIPE_BUF` invariant is retained: a 128-byte write is within
  `PIPE_BUF`; `Partial` is a defensive anomaly, never retried or normalized to
  success.

## 5. RELEASE frame and golden vector

Frame layout is exact and descriptor-free:

```text
0..8    "GGM1CTL\0"
8..10   version = 1
10..12  kind = 2 (RELEASE)
12..16  total length = 128
16..24  sequence = 1
24..40  execution UUID (RFC bytes)
40..56  preparation UUID (RFC bytes)
56..88  manifest SHA-256
88..96  zero reserved
96..112 generation UUID (RFC bytes)
112..120 overall deadline (u64, CLOCK_MONOTONIC ns)
120..128 zero reserved
```

Golden vector length: **128 bytes**. SHA-256:
`90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B`.

The existing canonical manifest vector remains 873 bytes,
`82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.

## 6. Coordinator RELEASE reader and one-shot behavior

`release_protocol.h` provides the shared strict parser used by the production
coordinator and the qualification coordinator. It validates length, magic,
version, kind, total length, sequence, execution/preparation/digest identity,
generation, overall deadline, both reserved regions and local expiry.

The production coordinator reads exactly 128 bytes after HELD, rejects
incomplete/truncated, invalid and extra-frame input, closes FD 4 permanently
after acceptance, and stops at the WP05 pre-exec boundary. Target fork/exec
remains WP10. The integrated managed path still has no release call.

The qualification-only coordinator:

- source: `tests/Fixtures/wp05-qualification-coordinator.c` (SHA-256
  `4fb0559241a925b822989a9a29e068b6c12200d1967c97108619091ce85328b1`);
- built binary from `build-native.sh`: 739,520 bytes / SHA-256
  `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`;
- validates the same frame parser, records acceptance on stdout, optionally
  emits bounded synthetic status frames, and never forks/executes a target.

The production coordinator artifact rebuilt at this revision is 878,792
bytes with SHA-256
`5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`
(source `bcbf5526d625bbf2768be52b538093d3c5d1299a0407eb988b686040930099ea`).
It is not the WP04 release-disabled artifact; WP04 HELD/abort behavior remains
the accepted path and the WP05 addition is the post-HELD RELEASE branch.

## 7. Status protocol and observation ordering

- `CoordinatorStatusReader` validates the trusted status kinds
  EXEC_FAILED/EXEC_CONFIRMED/ROOT_WAIT/PROTOCOL_FAILED, fixed payload sizes,
  sequence, identity, reserved bytes and plan order. It is the only accepted
  target/reaper observation source; stdout/stderr/Bubblewrap JSON are excluded.
- EXEC_FAILED maps to `FailedBeforeExec(errno)`; EXEC_CONFIRMED maps to
  `Confirmed(CoordinatorFrame)`; ROOT_WAIT after EXEC_CONFIRMED decodes
  Exited/Signalled; ROOT_WAIT after EXEC_FAILED maps root NotStarted; an
  unconfirmed ROOT_WAIT maps Exec Unknown + diagnostic Root Unknown.
- `ReleaseObservationBuffer` holds at most one exec and one root observation.
  It does not persist before durable ReleaseCommitted, flushes in plan order
  after commitment, rejects root-before-exec and contradictory observations,
  and treats identical duplicates idempotently.
- Delivery/observation compatibility is enforced by the accepted v3 validator:
  definite failed delivery (`WouldBlock`/`BrokenPipe`/`Partial`) cannot coexist
  with Confirmed/FailedBeforeExec/known root; `Unknown` may coexist with later
  validated status.

## 8. Stop/release matrix and crash windows

- Stop accepted first: release is refused before durable intent; no capability
  consumption, no write, StopRecord persisted.
- Stop accepted after durable intent but before the release gate: the gate
  refuses, consumes the capability, preserves durable Intent + Stop; recovery
  remains conservative.
- Release wins first: later stop is accepted/durable but cannot erase Intent,
  Committed or Delivery; both facts persist.
- Duplicate release returns the retained decision without another write;
  duplicate stop returns the retained stop without another write.
- Deterministic implementation barriers cover: after intent, after capability
  consumption, after the one write, after durable Committed, stop-after-intent,
  release-then-stop, expiry after intent, missing/stale prerequisites and failed
  intent persistence.
- After durable intent, recovery never reconstructs/resends release; the v3
  classifier remains MayHaveExecuted/Unknown until trusted observations narrow
  the result. Cleanup remains separate.

## 9. Seccomp and canonical vector impact

The pinned seccomp artifact is unchanged: 672 bytes, SHA-256
`4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`.
The production coordinator release branch uses already-present
read/poll/close/clock_gettime/memcmp operations and requires no filter
widening. The canonical manifest vector is unchanged; a separate RELEASE
vector was added.

## 10. Tests

| Check | Result |
|---|---|
| Windows full Linux-M1 | **366 passed, 160 skipped, 0 failed / 526** |
| Non-root Linux full assembly (UID 1000) | **453 passed, 73 skipped, 0 failed / 526** |
| Release arbitration tests | 16/16 |
| RELEASE frame tests | 17/17 |
| Status reader/ordering tests | 11/11 |
| Linux native release protocol tests | 4/4 |
| v3/protocol focused selection | 98/98 |
| Focused WP04/journal/canonical | 65/65 |
| Canonical manifest | 1/1; 873 bytes / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` |
| RELEASE golden vector | 1/1; 128 bytes / `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B` |
| Full Release build | 0 warnings, 0 errors |
| Seccomp freeze | 672 bytes / `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` |

The Windows skips are existing Linux-native/environment families. The Linux
non-root run skipped 73 delegated-cgroup/native families; no new
release-protocol or v3 test was skipped. The WP04 native HELD/abort test with the
new coordinator was not available in the post-reboot WSL delegation (atomic
Bubblewrap spawn returned EACCES before HELD); this is an environment
prerequisite gap, not a product or WP04 counterexample. Portable WP04
owner-protocol/journal regressions and the accepted WP04 evidence remain; the
native HELD/abort rerun is required in independent qualification.

## 11. Limitations and deferred work

This tranche does not independently qualify or accept WP05. It does not prove
real release-pipe behavior through the production coordinator end-to-end,
fresh-process kill behavior at every boundary, integrated owner orchestration,
resource/capture work, actual target execution, full WP05 or WP10. The deferred
wrong-audit-architecture seccomp-generator decision remains deferred. A full
independent qualification must rerun native WP04 HELD/abort with the new
coordinator, exercise the production coordinator release path end-to-end, and
perform the fresh-process process-loss matrix.

**WP05 RELEASE PROTOCOL IMPLEMENTED - INDEPENDENT QUALIFICATION REQUIRED**
