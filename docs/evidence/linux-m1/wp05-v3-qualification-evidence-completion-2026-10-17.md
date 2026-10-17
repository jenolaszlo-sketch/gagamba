# WP05 v3 Durable Schema — Qualification Evidence Completion

Date: 2026-10-17 (Asia/Manila)

Remediation slice: WP05 v3 durable ownership schema foundation (commits
`3ffb843`, `959bac1`, `21a04a1`, `e004bc4`). This document completes the
objective qualification evidence begun by independent reviewer Luna, which was
interrupted by execution-quota exhaustion. It is a completion of
qualification evidence, **not** independent final acceptance.

Baseline source SHA: `84a6aac92647b1c09bd729fe6171d62b34063047`
Final source SHA: `c075841bcccc31e186719c9e81261285332fe8ac`
Branch: `qualification/ar7-feasibility`
Host OS/version: Microsoft Windows NT 10.0.26200.0 (Windows 11 25H2),
host name `LAPTOP-KBD8D8JG`
Architecture: x86_64
Runtime/SDK: .NET SDK 10.0.401; runtime `Microsoft.NETCore.App 10.0.12`
Relevant native prerequisites: none exercised on this Windows host. Native
Linux journal fence (`flock`), owner protocol (`SOCK_SEQPACKET`/`SCM_RIGHTS`),
pidfd, openat2, delegated cgroup v2 and mount-namespace behavior remain
exercised only on the WSL2 Linux environment; those results are carried from
Luna's independent runs and are identified as such.
Qualification scope: portable schema/codec/transition/journal/recovery tests and
the independent hostile probes on Windows; plus source-level freeze and invariant
verification.
Explicit exclusions: release transport, coordinator `RELEASE`, native
post-release behavior, owner release orchestration, target execution, WP05 full
qualification, WP10, seccomp policy changes (freeze check only, not requalification).

## 0. Repository binding and retained artifacts

- `git rev-parse HEAD` = `c075841bcccc31e186719c9e81261285332fe8ac`, matching the
  production revision under qualification.
- `git status --porcelain` shows exactly one untracked file:
  `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05V3IndependentQualificationTests.cs`
  (Luna's independent qualification probes). The tree is otherwise clean at
  `c075841`; no production or test source was modified during this work.
- The full-suite test run included the untracked probe file (it compiles into the
  test assembly) and its five tests passed, so the probes were exercised against
  the exact `c075841` tree.

## 1. Skip classification (all 156 skipped rows)

The Windows full Linux-M1 run reports 156 `NotExecuted` rows. Every one is a
pre-existing Linux-native test unavailable on this Windows host; none is a new
WP05-v3 test and none hides a required v3 property.

| Family (test class) | Count | Reason |
|---|---|---|
| `NonRootQualificationTests` | 31 | `linux only` |
| `NativeCopyInTests` | 23 | `requires Linux with openat2 support` |
| `CoordinatorMountVerifierTests` | 20 | `requires Linux mount namespace semantics` |
| `DomainAuthorityTests` | 19 + 2 | `requires delegated Linux cgroup v2` (19), `requires Linux` (2) |
| `IntegratedBarrierTests` | 14 | `requires delegated Linux cgroup v2 with openat2` |
| `NativeStagingCleanupTests` | 8 | `requires Linux openat2` |
| `OwnerProtocolTests` | 8 + 6 | `requires Linux SOCK_SEQPACKET` (8), `requires Linux SOCK_SEQPACKET and SCM_RIGHTS` (6) |
| `OwnerDeathQualificationTests` | 7 | `requires delegated Linux cgroup v2 with openat2` |
| `JournalFencingTests` | 6 | `requires Linux` |
| `SeccompFilterPinTests` | 4 | `requires Linux pread for the pinned native filter verifier` |
| `PidFdChildReaperTests` | 3 | `requires Linux pidfd and waitid(P_PIDFD)` |
| `RecoveryHarness` | 3 + 2 | `linux only` (3), `requires delegated Linux cgroup v2` (2) |

Pre-existing status: all 156 are inherited from the WP03/G2/WP04 test inventory;
no WP05-v3 test (`OwnershipV3CodecTests`, `OwnershipV3TransitionTests`,
`ReleaseRecoveryClassificationTests`, `Wp05V3IndependentQualificationTests`) is
skipped.

Required-v3 intersection: the only skipped family that touches a required v3
property is `JournalFencingTests` (writer fencing). It is covered on Linux by
Luna's independent run (6/6 passed) and is therefore not hidden behind a skip for
the Linux environment; on Windows it requires a real `flock` fence and is
correctly reported as not-executed rather than passed. The portable fencing
coverage (`JournalTests`, 62/62) and the pure stale-baseline/foreign-lease
transition tests all execute on Windows and pass.

Focused fresh coverage: no required v3 property remains hidden behind a skip. The
portable v3 codec/transition/classification/probe tests all run green on Windows,
and the Linux-native fence/owner-protocol properties are covered by Luna's
recorded Linux results.

## 2. Canonical manifest vector

Independently recomputed from the tracked golden byte constant (not from the
running test's assertion):

- decoded length: **873 bytes**;
- SHA-256: `82161029848B1A821241AF112CA806BE8029D3CF2E7B95C6B23CE3E35850D5F3`
  (case-insensitive match to `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`).

`ManifestTests.CanonicalGoldenBytes` also re-encodes the sample manifest and
recomputes the digest, passing in the fresh full-suite run. No difference. The
manifest codec is unchanged by the WP05 tranche (`git diff` touches no
`Contracts/` source).

## 3. Native / protocol / seccomp freeze

`git diff --name-only 84a6aac c075841` is confined to four documentation files,
eight `src/Gagamba.Execution.Linux.M1/…` files (all under `Recovery/` or the two
`Lifecycle/OwnerPreparation{Specification,Transaction}.cs` legacy-version
constants), and four test files (plus the untracked probe). There are **no
changes** to:

- native coordinator (`native/linux-m1/exec-coordinator.c`);
- Bubblewrap adapter (`src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs`);
- owner protocol (`src/Gagamba.Execution.Linux.M1/Native/OwnerProtocol.cs`);
- coordinator protocol (`src/Gagamba.Execution.Linux.M1/Launch/CoordinatorProtocol.cs`);
- descriptor map / FD-role transfer (`src/Gagamba.Execution.Linux.M1/Native/*`);
- release FD behavior; or
- seccomp policy/artifact (`native/linux-m1/seccomp-filter.hex`,
  `native/linux-m1/seccomp_filter_policy.py`).

Seccomp artifact recomputation (freeze check, not requalification): the tracked
hex source `native/linux-m1/seccomp-filter.hex` (1344 hex chars) decodes to
**672 bytes** with SHA-256
`4EE63C48010D74118F74DDEC44B48ACC16CABC3E7E5693E83D941F82BBB3A2BC`, matching the
required `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`. The
present `artifacts/linux-m1/wp04/seccomp-filter.bin` (672 bytes) hashes identically.

## 4. Full solution build

Fresh `dotnet build Gagamba.sln -c Release`:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Matches the expected implementation report of 0 warnings / 0 errors.

## 5. V2 golden record

The representative v2 record golden SHA-256
`5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE` is
recomputed from first principles by two independent sources, both of which
compute `SHA256(Encode(v2Snapshot))` rather than reading a stored file, and both
of which pass in the fresh run:

- implementation codec test `OwnershipV3CodecTests.Version2RecordRemainsLegacyAndAccepted`;
- independent Luna probe
  `Wp05V3IndependentQualificationTests.RecomputesV2GoldenAndChecksLazyUpgradePreservesIdentity`.

Verified additionally:

- **v2 meaning unchanged**: a v2 record decodes with `Release=null`,
  `Stop=null`, `Exec=null`, `Root=null`, and `Stage`, `ExecutionId`, `BootId`,
  `DomainInode` retain their exact v2 values (`Version2RecordRemainsLegacyAndAccepted`).
- **no v3 facts inferred from v2**: the decoder leaves all v3 tail records absent
  for version 2 (`OwnershipRecord.TryDecode` reads no tail for v2).
- **WP04 pre-release path remains v2**: `OwnerPreparationSpecificationCodec.BuildIntent`
  and `OwnerPreparationTransaction` now use the explicit `LegacyVersion` constant
  (`git diff` shows only `CurrentVersion` → `LegacyVersion` in those two call
  sites); `OwnershipV3TransitionTests.Wp04LegacyIntentStillEncodesVersion2`
  proves the encoded first byte is version 2.

## 6. Independent hostile probes (Luna)

Reviewed `Wp05V3IndependentQualificationTests.cs` (5 facts, all passed). The
probes are qualification-only, not implementation, and their comparison logic is
structural (byte arrays compared through xunit's element-wise overloads), so the
earlier reference-equality defect is not present. Coverage of the required
hostile cases:

| Required hostile case | Probe |
|---|---|
| malformed / hostile bytes | `RechecksummedHostileV3BytesFailClosed` (truncation, corruption, trailing byte, re-checksummed mutations of version, delivery, stop-cause, exec-tag, basis, root-tag, root-kind, raw-wait) |
| stale baseline | `StaleSnapshotsCannotOverwriteAnyAdvancedDurableFact` (per-fact stale refusal across intent/commit/stop/exec/root/cleanup) |
| disappearing durable fact | same probe (mutations clearing `Release`/`Stop`/`Exec`/`Root` refused and fact-preserved) |
| contradictory observation | `RechecksummedHostileV3BytesFailClosed` (WouldBlock + Confirmed refused); `UnknownRootAndCleanupRemainIndependentInRecoveryProjection` (Partial delivery + Unknown exec/root) |
| invalid delivery/observation combination | `RechecksummedHostileV3BytesFailClosed` (`WouldBlock` delivery byte mutation refused) |
| v2→v3 identity preservation | `RecomputesV2GoldenAndChecksLazyUpgradePreservesIdentity` (execution/boot/lease/stage/preparation/allocations preserved across lazy upgrade) |

No probe was modified; no concrete defect was found in any probe.

## 7. Stale-baseline and no-lost-fact

Production (`OwnershipJournal.WriteTransition`/`WriteMutation`, `OwnershipValidation.OwnershipTransitionValidator`):

- every write re-resolves the acknowledged lineage under the live lease and
  refuses a stale baseline (`stale baseline: expected snapshot is not the acknowledged lineage`);
- `IdentityEqual` freezes execution/boot/domain/staging/lease/preparation/
  allocation identity;
- `ValidateReleaseTransition` refuses `next.Release is null` (`release facts must not disappear`);
- stop is immutable (`the first accepted stop is immutable`);
- exec/root are single-assignment;
- `CompletedCleanup` bitmask and `Cleanup` confirmed/failed/unknown are monotonic
  (cannot regress);
- exactly one release/stop/observation fact may change per transition, separate
  from cleanup progress.

This guarantees no stale update can erase `ReleaseIntent`, `ReleaseCommitted`,
`DeliveryResult`, `StopRecord`, `ExecObservation`, `RootObservation`, or cleanup
progress; every durable update is a full monotonic snapshot transition from the
exact latest acknowledged baseline. Covered by `StaleBaselineCannotErase*`,
`MutationCannotClearDurableFacts`, `CleanupMutationPreservesAllDurableFacts`,
`ChangedLineageAndForeignLeaseRefused`, `WrongSequenceAndNoFactTransitionRefused`,
and Luna's `StaleSnapshotsCannotOverwriteAnyAdvancedDurableFact` (all pass).

## 8. Delivery / observation invariant (production validation, not merely tests)

`OwnershipValidation.TryValidateExec`/`TryValidateRoot` enforce, via
`IsTrustedStatusDelivery` (`Delivered` or `Unknown`), that:

- `ExecTag=Confirmed` requires delivery ∈ {`Delivered`,`Unknown`};
- `ExecTag=FailedBeforeExec` requires delivery ∈ {`Delivered`,`Unknown`};
- known root (`Exited`/`Signalled`/`NotStarted`) requires the matching confirmed/
  failed-before-exec exec and delivery ∈ {`Delivered`,`Unknown`}.

Therefore `WouldBlock`, `BrokenPipe` and `Partial` cannot coexist with
`Confirmed`, `FailedBeforeExec`, or a known root observation, while `Unknown`
delivery may coexist with subsequently validated status (ADR 0004 §4.2/§13). This
is the production validator, not a test-only guard. Covered by
`OwnershipV3CodecTests.FailedDeliveryWithConfirmedObservationFailsClosed` and the
`ValidObservationStatesRoundTrip` case with `DeliveryResult.Unknown` (all pass).

## 9. StopDeadline takeover

`OwnershipTransitions.TryPlanStop` refuses when `current.Stop is not null`
(`the first accepted stop is immutable`), and `OwnershipTransitionValidator`
refuses any transition where `previous.Stop is not null` and the stop changes. A
same-boot persisted `StopDeadline = StopAcceptedAt + 5_000_000_000 ns` therefore
remains authoritative through recovery takeover; a second/different stop is
refused. If recovery creates the first `StopRecord` (`Stop is null`), the exact
five-second allowance from ADR 0004 §9.1 is applied. Covered by
`SecondStopCannotRewriteFirstStopDeadline` and Luna's
`StopDeadlineOverflowAndImmutableDeadlineAreRefused` (both pass; the latter also
proves the overflow path fails closed and a takeover cannot rewrite the deadline).

## 10. Explicit recovery state

`Committed + Exec Confirmed + Root Unknown` is a valid durable state and
`ReleaseRecoveryClassifier.Classify` returns `Release=MayHaveExecuted`,
`Exec=Confirmed`, `Root=Unknown`, `Outcome=Unknown` — never `NotStarted`, success,
or failure (root is not `Exited`/`Signalled`, so aggregate outcome stays
`Unknown`). Covered by `ReleaseRecoveryClassificationTests.ConfirmedWithUnknownRootStaysUnknown`
and Luna's `UnknownRootAndCleanupRemainIndependentInRecoveryProjection` (both pass).

## 11. WP04 regression

- Ordinary WP04 HELD/abort remains version 2 (`LegacyVersion` in `BuildIntent`/`BuildRegistered`;
  `Wp04LegacyIntentStillEncodesVersion2` passes).
- No release/stop/observation state is introduced into the legacy path; the only
  shared-path change is the reconciler cleanup writer's use of the latest-snapshot
  `WriteMutation`, which preserves existing facts and does not alter cleanup
  decisions or HELD/abort behavior.
- Native, Bubblewrap and seccomp behavior are unchanged (section 3).
- Focused WP04 owner-protocol (21/21) and journal-fence (6/6) regressions passed
  on WSL in Luna's run (27/27). On this Windows host those families report the
  Linux-native skips classified in section 1; the portable `OwnerProtocolTests`
  (7) and all `JournalTests` (62), `ReconcilerTests` (18), and
  `ManifestTests` (19) pass.

## 12. G2 focused evidence (ADR 0004 §16, focused renewal only)

| Focused G2 property | Evidence |
|---|---|
| v3 codec honesty | `OwnershipV3CodecTests` (20/20): v2/v3 round-trip, stable enum values, version/enum refusal, checksum/malformed/contradictory refusal, trailing-byte/truncation. Luna `RechecksummedHostileV3BytesFailClosed`. |
| writer fencing | `OwnershipV3TransitionTests.ChangedLineageAndForeignLeaseRefused`, `WrongSequenceAndNoFactTransitionRefused`; `JournalTests` (62/62); Luna Linux `JournalFencingTests` (6/6). |
| stale-baseline refusal | `StaleBaselineCannotErase*`; Luna `StaleSnapshotsCannotOverwriteAnyAdvancedDurableFact`. |
| recovery classification | `ReleaseRecoveryClassificationTests` (11/11); Luna `UnknownRootAndCleanupRemainIndependentInRecoveryProjection`. |
| cleanup/result independence | `CleanupMutationPreservesAllDurableFacts`, `MutationCannotClearDurableFacts`, `CleanupConfirmedDoesNotRewriteReleaseOrExecution`. |
| identity validation | `ReleaseIdentityAndDeadlinesCannotMutate`, `OwnershipValidation.IdentityEqual`, invalid-combination refusal. |

G2 is not reopened wholesale; no G2 counterexample was produced.

## 13. Test totals

Windows full Linux-M1 project, fresh run against `c075841` (with the five
untracked Luna probes compiled in):

```text
Total tests: 478
     Passed: 322
    Skipped: 156
     Failed: 0
```

Per-class (Windows):

| Class | Passed | Skipped | Failed |
|---|---|---|---|
| `OwnershipV3CodecTests` | 20 | 0 | 0 |
| `OwnershipV3TransitionTests` | 18 | 0 | 0 |
| `ReleaseRecoveryClassificationTests` | 11 | 0 | 0 |
| `Wp05V3IndependentQualificationTests` (Luna) | 5 | 0 | 0 |
| `JournalTests` | 62 | 0 | 0 |
| `ReconcilerTests` | 18 | 0 | 0 |
| `OwnerProtocolTests` | 7 | 14 | 0 |
| `JournalFencingTests` | 0 | 6 | 0 |
| `ManifestTests` | 19 | 0 | 0 |

Luna's Linux non-root results (carried, not re-run here): v3 codec/transition
38 passed; recovery classification 11 passed; journal fencing 6 passed; owner
protocol 21 passed; full Windows Linux-M1 322/156/0 with the five independent
probes green.

## 14. Limitations

- This document completes the qualification evidence; it is **not** independent
  final acceptance, and it does not claim DeepSeek's tranche is accepted.
- Windows results are portable-suite results; native Linux fence/owner-protocol/
  pidfd/delegated-cgroup behavior is carried from Luna's Linux/WSL runs, not
  re-executed on this host.
- No release transport, coordinator `RELEASE`, owner release orchestration,
  target execution, WP05 full qualification, WP10, or seccomp policy change was
  performed or qualified.
- The seccomp/native/protocol check is a freeze verification, not a seccomp
  requalification.

## 15. Disposition

All remaining qualification evidence passes; no product defect, ADR counterexample,
G2 counterexample, or WP04 counterexample was exposed. Luna's probes were not
modified, and no probe defect was found.

**WP05 V3 QUALIFICATION EVIDENCE COMPLETE - INDEPENDENT ACCEPTANCE PENDING**
