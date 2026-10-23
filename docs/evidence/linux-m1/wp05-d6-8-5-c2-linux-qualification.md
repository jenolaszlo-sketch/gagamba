# WP05 D6.8.5 C2 Linux Runtime Qualification

Date: 2026-10-23
Producer: DeepSeek V4.1
Scope: WSL2 Linux runtime qualification of the human-approved, implemented and
independently reviewed C2 stop-capability invalidation behavior

Status: **WP05 D6.8.5 C2 WSL2 LINUX RUNTIME EVIDENCE COMPLETE - ALL REQUIRED WSL2 C2 LEGS PASSED; NATIVE-DISTRO QUALIFICATION OUTSTANDING; INDEPENDENT GLM REVIEW PENDING; RELEASE NOT QUALIFIED**

This document records producer execution evidence. It is not independent
qualification and does not accept G3, Owner integration or WP05 release. The
required C2 legs executed under WSL2 Linux with the exact preserved source and
test content. Native Linux distribution qualification remains outstanding. Two
test-harness issues were encountered and are disclosed in section 14; neither
was a C2 authority contradiction, and neither involved a production or test
source change.

## 1. Git binding and exact source identity

Repository: `C:\Users\Laszlos\source\repos\Gagamba`
Origin: `https://github.com/jenolaszlo-sketch/gagamba.git`

| Item | Identity |
|---|---|
| Qualification base / D6.8.4 preservation | `87e9e6f5b165176b27f066ff85025605fdaf31c6` |
| Base tree | `16c0fe915c6b9969664762a74427d77f05fca94f` |
| Qualification branch | `qualification/wp05-d6-8-5-c2-linux` |
| Windows worktree | `C:\Users\Laszlos\source\repos\Gagamba-d6-8-5-c2` |
| Linux-native checkout | `/home/gaga/qualification/gagamba-d6-8-5-c2` |
| Linux bundle | `/home/gaga/qualification/bundles/wp05-d6-8-5-c2.bundle` |
| Bundle SHA-256 | `ca518c19fd00eb96b959f66bf420f76e2698af2d027f9b7b41125d37d7d08e5a` |
| Production `src/` tree | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` |
| Test tree | `e03515f4d1ffb2171ae911a5b1da894b6f553d65` |
| Native tree | `85631d2f04512376621d3d09f51d67fd90f45335` |
| `eng/` tree | `d5cd863fed7637989eb256aee0254be4bfe571f6` |
| Worktree status | clean tracked tree; no `safe.directory` override |

The Linux checkout was created by a fresh `git clone` from the bundle as
`gaga:gaga`; it is not the Windows-mounted repository and does not contain
copied Windows `.git` ownership metadata.

### 1.1 D6.8.4 producer and review identities

| Item | Identity |
|---|---|
| D6.8.4 test commit | `0aa201c3c5689690217ff5ae429de0bcda6943ff` |
| D6.8.4 test commit tree | `f05c4065f2db9dd15e1fed531d53293fdb9c2f81` |
| D6.8.4 evidence commit | `01da5d38a60a6f369c45a87f5bb18e2d8240081c` |
| D6.8.4 producer tree | `19d1241eb556f4452b38230e594d5abdb0a0f73a` |
| GLM closure review commit | `55f3f7a10b4d6860b8e7e02776bb3da73f801db9` |
| GLM review tree | `7a9d6ae7e6254b81af4300f8ab6a78d9efc804e6` |
| GLM review document blob | `22f6998cca7a3dee5453f2bc932f7ca89116c4cf` |
| D6.8.4 preservation | `87e9e6f5b165176b27f066ff85025605fdaf31c6` |
| Preservation checkpoint | `docs/reviews/gagamba-linux-m1-wp05-d6-8-4-glm-review-preservation-checkpoint.md` |

The previously misreported review-tree identity `e46ae4ac...` was not used.
The authoritative review tree read from `55f3f7a1...` is
`7a9d6ae7...`. No prior evidence was rewritten.

### 1.2 Test and production file identities

| File | Git blob at base | SHA-256 of Linux checkout bytes |
|---|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | `dca9ea1d6feea9fdb752de7d3e90f80431b85c58` | `14cc92540558e8c676a042d68c8e80d996231bb8c2814c9ff1d2aa729b82354e` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs` | `47f70ae76d0287cbaf618b529ada5330e514cb1e` | `279be74df05fb002c07922b6d47b95116e42d700f82c317288287bb8b3c9d74f` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` | `698c07e21add0164938b99e7f22502ce7e822d10` | `cb83be6ae5511e86368e74bc5055f9a7823199a40c4e40ad227ca920e719edad` |
| `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs` | `3c7217cce600025b60849edc36654b83aedb028b` | `0ac567f8597977cdb7fd01f551ff7d2cf1a756b43c19e334aafad06062c1049d` |
| `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4OwnerHost.cs` | `c7523aac98402bfc5627447a971e1ec88bf35860` | `4a5b96baf8c8d99e43231d4ef8c9a1226d2e2c07452a562a8d0e6d2a1db7a031` |
| `eng/linux-m1-qualified-tests.sh` | `ed246b2a2e659085f5416d03f9fd26ffdfd73fa8` | not modified |
| `eng/linux-m1-independent-readiness.sh` | `93c4dec426372c2cd89be05f3e617ed20f3b24eb` | not modified |

The source and test trees were verified before execution and again after all
qualification runs. The post-run verification recorded the same HEAD and trees,
a clean tracked tree, and `tracked_surface_diff_exit=0` for
`src tests native eng` (`00b-source-identity-post.log`).

## 2. Linux environment manifest

### 2.1 Platform

| Item | Observed |
|---|---|
| Lane | **LINUX WSL2 RUNTIME EVIDENCE**; not native-distro and not bare metal |
| Distribution | Ubuntu 26.04.1 LTS (Resolute Raccoon) |
| Kernel | `6.6.114.1-microsoft-standard-WSL2` |
| Architecture | `x86_64` |
| WSL version | 2 |
| Cgroup filesystem | `cgroup2fs` at `/sys/fs/cgroup`, `nsdelegate` |
| Root cgroup permissions | `/sys/fs/cgroup` mode `555`, `root:root` |
| Qualification identity | `uid=1000(gaga) gid=1000(gaga) groups=1000(gaga)` |
| CapInh / CapPrm / CapEff / CapAmb | all `0000000000000000` |
| CapBnd | `000001ffffffffff` |
| NoNewPrivs | `0` |
| Seccomp | `0` on the qualification process; the seccomp artifact is separately pinned |

### 2.2 Delegated cgroup provenance

The delegated tree existed before this task and was not recreated or relaxed by
this qualification.

| Item | Observed |
|---|---|
| Delegated parent | `/sys/fs/cgroup/gaga-delegated`, mode `755`, `gaga:gaga` |
| Parent controllers | `cpu memory pids` |
| Parent subtree control | `memory pids` |
| Host leaf | `/sys/fs/cgroup/gaga-delegated/host`, mode `755`, `gaga:gaga` |
| Host `cgroup.procs` | writable by `gaga`; empty after runs |
| Host `cgroup.events` | `populated 0`, `frozen 0` after runs |
| `cgroup.kill` | present |
| Test placement | `0::/gaga-delegated/host` |
| Current cgroup of test processes | `/sys/fs/cgroup/gaga-delegated/host` |

The qualification test processes were not root. Placement was performed by the
existing operator-controlled root-only launcher
`/root/wp05/run-as-gaga-in-host.sh` (mode `700`, `root:root`, SHA-256
`431c6c0b9c1b0caf4d1fd1ccf1a44b6fe736d06e035bff1fde7d989579b95b6c`). The
launcher writes its own PID to the host `cgroup.procs`, then execs
`setpriv --reuid=1000 --regid=1000 --clear-groups` with
`GAGAMBA_DELEGATED_CGROUP=/sys/fs/cgroup/gaga-delegated`. Root is never the
qualification process. The operator root context was also used only to copy the
bundle and test scripts into gaga-owned paths and to invoke the existing
launcher. No new privileged service, setuid helper, system-wide policy or
cgroup delegation was created.

### 2.3 Toolchain and pinned mechanisms

| Item | Observed |
|---|---|
| .NET SDK | `10.0.113` (`/usr/lib/dotnet/sdk/10.0.113`) |
| .NET runtime | `10.0.13` (`Microsoft.NETCore.App 10.0.13`) |
| RID | `ubuntu.26.04-x64` |
| Bubblewrap | `bubblewrap 0.11.1`, `/usr/bin/bwrap`, mode `755`, non-setuid |
| Bubblewrap SHA-256 | `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` |
| GCC | `gcc (Ubuntu 15.2.0-16ubuntu1) 15.2.0` |
| Python | `3.14.4` |
| glibc | `2.43` |
| Git | `2.53.0` |
| strace | `6.19` |

The pre-existing session anchor is `sleep infinity` PID `576`, user `gaga`,
parent `568`, in `/init.scope`. It is outside the qualification host leaf, has
no capabilities and is not a test or target process. It keeps the WSL session
and delegated tree alive. It was present before this task; owner: prepared WSL
review session; cleanup: kill PID 576 or reboot, which also removes the
delegated tree.

## 3. Readiness preflight

Exact command, run as `gaga` in the delegated host:

```bash
bash eng/linux-m1-independent-readiness.sh \
  --source-rev 87e9e6f5b165176b27f066ff85025605fdaf31c6 \
  --report /home/gaga/qualification/d685/reports/wp05-d685-readiness.txt
```

Result: `SUMMARY pass=56 fail=0`, `READINESS=PASS`, exit `0`.
`REVIEWER_ACCESS=UNVERIFIED` and `INDEPENDENT_ACCEPTANCE=NOT_CLAIMED` were
emitted by the script. The source comparison used the exact D6.8.4 preservation
SHA, not R12; `source.head`, `source.rev`, `source.revision`,
`source.ancestor` and `source.production_surfaces` all referenced
`87e9e6f5...`.

The preflight's optional `artifacts/linux-m1/wp04/seccomp-filter.bin` check was
`INFO not present; generated pin is reproducible` because native artifacts had
not yet been built. The generator check reproduced the 672-byte filter
`2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`. The
artifact was built afterwards and independently hash-verified (section 4). This
is a readiness result, not qualification.

## 4. Build and artifact identities

Phase A command, from the clean Linux checkout:

```bash
dotnet --info
dotnet build Gagamba.sln -c Release --nologo
```

The solution build succeeded with `0 Error(s)` and `48 Warning(s)`. All 48
warnings were environment SourceLink/repository-remote warnings caused by the
local bundle clone's remote URL, for example:

- `The URL of repository remote 'origin' is invalid: '/home/gaga/qualification/bundles/wp05-d6-8-5-c2.bundle'`
- `Source control information is not available - the generated source link is empty`

These are recorded as environment warnings, not production code warnings. The
build produced the expected assemblies with no compiler errors.

Native fixtures were built with the existing documented entrypoint:

```bash
bash eng/linux-m1/build-native.sh
```

| Artifact | SHA-256 |
|---|---|
| `tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture` | `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305` |
| `artifacts/linux-m1/wp04/exec-coordinator` | `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b` |
| `artifacts/linux-m1/wp04/seccomp-filter.bin` | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` (672 bytes) |
| `artifacts/linux-m1/wp05/wp05-qualification-coordinator` | `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3` |
| Release M1 owner DLL used by the supported profile | `6a7960b291fe36093c66911abcd92748fe2dcffbfb3fb453495574a37cc1a26d` |
| D4 test owner fixture DLL | `881daf61416fd09c90169c0a6ed4d43ebb4455f6f7e75b637e80c2526a447415` |

The native identities match the previously pinned values. Build outputs are in
ignored directories and are not tracked changes.

## 5. Supported Linux M1 profile

The supported runner was used, not an unrestricted parallel test invocation:

```bash
CONFIGURATION=Release bash eng/linux-m1-qualified-tests.sh
```

### 5.1 First attempt and environment correction

The first attempt executed the regular parallel phase successfully, then failed
in the `NonRootQualificationTests` dedicated test host:

| Phase | Total | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|---:|
| Regular parallel phase | 510 | 510 | 0 | 0 | 8 s |
| `Recovery.NonRootQualificationTests` | 31 | 2 | 29 | 0 | 27 s |
| Later classes | not run | - | - | - | - |

Runner exit status: `1`.

The failures reported:

```text
System.InvalidOperationException : owner executable missing from the test build
   at ...NonRootQualificationTests.IntegratedFixture.Request(...)
      NonRootQualificationTests.cs:line 433
```

The cause is the test helper `RecoveryHarness.LocateOwnerDll()`: its default
fallback is hardcoded to
`src/Gagamba.Execution.Linux.M1.Owner/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.Owner.dll`
(`RecoveryHarness.cs:85`). Phase A built Release only. The helper supports the
documented override `GAGAMBA_OWNER_DLL` (`RecoveryHarness.cs:67-72`). This is a
test-harness configuration default, not a C2 authority behavior. The first
failure is preserved in `04-supported-profile.log`; it was not discarded.

### 5.2 Corrected supported-profile run

The supported runner was re-executed with the test's own documented override
pointing at the Release-built owner DLL; no test or production source was
changed:

```bash
GAGAMBA_OWNER_DLL=/home/gaga/qualification/gagamba-d6-8-5-c2/src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/Gagamba.Execution.Linux.M1.Owner.dll \
CONFIGURATION=Release bash eng/linux-m1-qualified-tests.sh
```

| Phase | Total | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|---:|
| Regular parallel phase | 510 | 510 | 0 | 0 | 6 s |
| `Recovery.NonRootQualificationTests` | 31 | 31 | 0 | 0 | 7 s |
| `Recovery.OwnerDeathQualificationTests` | 7 | 7 | 0 | 0 | 1 s |
| `Recovery.IntegratedBarrierTests` | 15 | 1 | 0 | 14 | 52 ms |
| `Recovery.DomainAuthorityTests` | 21 | 2 | 0 | 19 | 52 ms |
| `Recovery.RecoveryHarness` | 5 | 3 | 0 | 2 | 46 ms |
| `Recovery.Wp05TerminationObserverTests` | 3 | 3 | 0 | 0 | 46 ms |
| `Launch.Wp05SigpipeDispositionTests` | 1 | 1 | 0 | 0 | 104 ms |
| **Total** | **593** | **558** | **0** | **35** | - |

Runner exit status: `0`.

The 35 skips are the known root-only cgroup-probe classes:

- `IntegratedBarrierTests` (14): its `Usable()` probe tries to create
  `/sys/fs/cgroup/gagamba-r4-probe-*`; the non-root test process cannot create
  it because `/sys/fs/cgroup` is mode `555`, `root:root`. The test emits
  `requires delegated Linux cgroup v2 with openat2`; the permission failure on
  the root cgroup probe occurs before the `openat2` check.
- `DomainAuthorityTests` (19): same `/sys/fs/cgroup/gagamba-r2-probe-*`
  permission limit; test reason `requires delegated Linux cgroup v2`.
- `RecoveryHarness` (2): `Usable()` creates a probe directly under
  `/sys/fs/cgroup`; test reason `requires delegated Linux cgroup v2`.

These classes are not C2-mandatory legs. No C2 test was skipped. The
`NonRootQualificationTests` class, which is the non-root delegated topology
leg, passed 31/31 after the owner override.

## 6. C2 focused regression

Exact filter:

```text
FullyQualifiedName~Wp05D68C2StopCapabilityTests
```

Discovery returned 13 tests. Execution returned 13 passed, 0 failed, 0 skipped.
The corrected `C2_T03b` live-capability overflow test was discovered and
executed.

| Test | Outcome |
|---|---|
| `C2_T01_NormalStopFirstConsumesCapabilityAndBlocksRelease` | PASSED |
| `C2_T02_ClockUnavailableStopConsumesCapabilityWithoutDurableStop` | PASSED |
| `C2_T03_ExpiredCapabilityOverflowStopAcceptedWithoutDurableStop` | PASSED |
| `C2_T03b_LiveCapabilityOverflowStopConsumesCapability` | PASSED |
| `C2_T04_StopRecordPersistenceFailureStillConsumesCapability` | PASSED |
| `C2_T05_DuplicateStopIsIdempotentWithoutSecondConsumeOrDeadline` | PASSED |
| `C2_T06_ReleaseAfterStopWritesNoIntentAndNoPhysicalRelease` | PASSED |
| `C2_T07_SecondArbiterAfterFailedDurableStopCannotRelease` | PASSED |
| `C2_T08_StopAfterDurableIntentStaysConservativeMayHaveExecuted` | PASSED |
| `C2_T09_ReleaseBeforeStopLeavesReleaseEvidenceUnchanged` | PASSED |
| `C2_T10_StopAfterConsumedReleaseCannotReactivateCapability` | PASSED |
| `C2_T11_ReplacementContextCannotInheritConsumedCapability` | PASSED |
| `C2_T12_CompetingStopAndReleaseHaveOneWinnerAndNoPhysicalRelease` | PASSED |

Evidence: `05-c2-focused.log` and `testresults/c2-focused.trx`
(SHA-256 `f16a787c5d7db1c5bf5dbb8f8b3243b52118ea8de451c2808514ce265aa4541c`).

## 7. Historical contract probes

Exact filter:

```text
FullyQualifiedName~Wp05ReleaseContractProbes
```

Discovery returned 13 tests. Execution returned 13 passed, 0 failed, 0 skipped.
The probes were not weakened or modified.

| Probe | Outcome |
|---|---|
| `StopWithUnavailableClockConsumesCapabilityC2` | PASSED |
| `ExpiredCapabilityOverflowStopAcceptedC2` | PASSED |
| `AmbiguousSealFailureLeavesUncertainIntentAndNoStop` | PASSED |
| `DefiniteIntentFailureIsDistinctFromAmbiguousPersistence` | PASSED |
| `StopFirstConsumesCapabilityC2` | PASSED |
| `DurableStopBlocksReleaseIntentFromSecondArbiter` | PASSED |
| `StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2` | PASSED |
| `ConcurrentDuplicateReleaseProducesOnePhysicalWrite` | PASSED |
| `ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired` | PASSED |
| `StatusBufferDoesNotPersistBeforeDurableCommitted` | PASSED |
| `StatusBeforeCommittedPersistenceFailureDoesNotCreateObservation` | PASSED |
| `StatusReaderRejectsWrongIdentityAndSequence` | PASSED |
| `StatusReaderRejectsEofAndMalformedFrames` | PASSED |

Evidence: `06-contract-probes.log` and `testresults/contract-probes.trx`
(SHA-256 `1fb139752b816d2590498ae4e02ec3e0281eb1de32b022c0d4bccb46cdc5626f`).

## 8. `D5_CR3b` real-process supervisor regression

Test: `Gagamba.Execution.Linux.M1.Tests.D4.Wp05D4SupervisorTests.D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`.
It is tagged `ProcessGlobalReaping` and was run in its own test host, outside
the regular parallel phase and not concurrently with another process-global
class.

Exact filter:

```text
FullyQualifiedName~D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease
```

Discovery returned exactly one test. The first isolated execution did not
produce a test result: it was terminated by the 900-second tool timeout. The
test's `EnsureOwnerBuilt()` had built the D4 owner fixture (DLL timestamp
`09:30`), but it calls `build.StandardOutput.ReadToEnd()` and
`build.StandardError.ReadToEnd()` before `build.WaitForExit()`
(`Wp05D4SupervisorTests.cs:539-541`); this can block on an inherited MSBuild
server pipe. The hang is preserved in `07a-d5-cr3b-hang.log`. This is a
test-harness build-helper issue, not a C2 result.

The test's own build step had completed the fixture during the hung attempt
(DLL timestamp `09:30`). The isolated test was rerun with that existing fixture;
`EnsureOwnerBuilt()` then returned without starting a build. A separate explicit
build of the same project entrypoint was also run as a reproducible check:

```bash
dotnet build tests/Fixtures/Wp05D4Owner/Wp05D4Owner.csproj -c Release --nologo
```

Result: exit `0`, 0 errors, 10 environment SourceLink warnings; DLL SHA-256
`881daf61416fd09c90169c0a6ed4d43ebb4455f6f7e75b637e80c2526a447415`, identical
to the fixture used by the passing run.

| Run | Discovered | Executed | Passed | Failed | Skipped | Result |
|---|---:|---:|---:|---:|---:|---|
| First isolated attempt | 1 | 0 | 0 | 0 | 0 | hung in owner-build helper; no test result |
| Retry with the completed fixture | 1 | 1 | 1 | 0 | 0 | PASSED, 324 ms |
| Evidence run with `D4_EVIDENCE` | 1 | 1 | 1 | 0 | 0 | PASSED, 336 ms |

The evidence run used:

```bash
D4_EVIDENCE=/home/gaga/qualification/d685/d5-evidence \
GAGAMBA_DELEGATED_CGROUP=/sys/fs/cgroup/gaga-delegated \
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --no-build --nologo \
  --filter 'FullyQualifiedName~D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease'
```

### 8.1 Required observations

The retained case root is
`/home/gaga/qualification/d685/d5-evidence/gagamba-d4-D5-CR3b-d6aa0d443a0a4845a99318fbae28841e`.

| Required observation | Observed evidence |
|---|---|
| 1. Real owner process starts | `owner.ready`: `owner_pid=67255`, `owner_start=67255:27026622`; witness `OwnerStarted`; supervisor observed `barrier after-stop` |
| 2. Stop is accepted | `result.txt`: `stop_accepted=True` |
| 3. Stop evidence is durable | `result.txt`: `stop_durable=True`, `stop_deadline=270271417591791`; journal sequence `4` |
| 4. Owner reports `capability_live == False` | `result.txt`: `capability_live=False` |
| 5. Release is refused | `result.txt`: `release_kind=Refused`, `release_durable=NotCommitted`, `release_delivery=NotAttempted` |
| 6. Original owner is terminated | witness: `SupervisorSigkill` followed by `OwnerTerminated ... result=exit=137`; test asserts exit `137` |
| 7. Durable `StopRecord` remains recoverable | test asserts journal `Stop` non-null and `Release` null; replacement result reports `recovered=true`, `sequence=4`, `stop=true`, and the exact persisted `stop_deadline` |
| 8. Fresh lease/replacement refuses RELEASE | replacement result: `recovered=true`, `writer=acquired`, `release_attempted=true`, `replacement_decision=Refused` |
| 9. Replacement transport attempts remain zero | replacement result: `replacement_transport_attempts=0` |
| 10. No release witness indicates a physical RELEASE | witness contains no `PreWriteWitnessAcknowledged`, `WriteReturned`, `CoordinatorExitObserved`, `TestTransportAttempt`, `RELEASE` or `ReleaseCommitted` markers; only lifecycle/recovery events |
| 11. Test-owned cgroup cleanup is confirmed | test `AssertCgroupRemoved()` passed; final inspection shows only `/sys/fs/cgroup/gaga-delegated/host`, host `populated 0`, no `d4-*` child and no test processes |

Witness event counts from the evidence run:

```text
1 JournalWriterAcquired
1 OwnerPrepared
2 OwnerStarted
1 OwnerTerminated
1 RecoveryFinished
1 RecoveryProcessExited
1 RecoveryStarted
1 SupervisorBarrierObserved
1 SupervisorSigkill
1 SupervisorStartedOwner
```

No physical-release event appears in that list. The replacement result
explicitly reports `replacement_transport_attempts=0` and
`replacement_decision=Refused`.

Evidence: `07d-d5-cr3b-evidence.log`, `07e-d5-cr3b-artifacts.log`,
`testresults/d5-cr3b-evidence2.trx`
(SHA-256 `dbe8d31af11a64f6c28061b51a34443820e08c2352fbf6706d5f448008795d40`),
and the retained case artifacts (`result.txt`, `witness.txt`, `owner.ready`,
replacement result, journal).

## 9. Adjacent D4 supervisor regression

The remaining `Wp05D4SupervisorTests` class was run in its own test host, with
`D4_EVIDENCE` retaining the case roots:

```bash
GAGAMBA_DELEGATED_CGROUP=/sys/fs/cgroup/gaga-delegated \
D4_EVIDENCE=/home/gaga/qualification/d685/d4-evidence \
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --no-build --nologo \
  --filter 'FullyQualifiedName~Gagamba.Execution.Linux.M1.Tests.D4.Wp05D4SupervisorTests'
```

Discovery returned 17 tests; all 17 executed and passed, with 0 failed and 0
skipped. This includes the C2-adjacent cases `D4_7_DurableStopRecoversWithoutRelease`,
`D4_8_FailedDurableStopAllowsOnlyDiagnosticReplacementRelease`,
`D4_13_StopPersistenceGateDoesNotFabricateStopOrRelease`,
`D5_CR3_TwoIndependentOwnersHaveOneReleaseAndOneRefusal`, and
`D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`, plus the crash-window,
journal-fence, duplicate and owner-death cases:

| Case | Outcome |
|---|---|
| `D4_1_RealHeldReleaseHasOneFd4ObservationAndCleanup` | PASSED |
| `D4_2_CrashAfterDurableIntentRecoversAndDoesNotResend` | PASSED |
| `D4_2b_ReplacementRetryOverDurableIntentIsRefusedWithoutTransport` | PASSED |
| `D4_3_CrashAfterFd4BeforeCommittedKeepsIntentAndObservedWriteOnly` | PASSED |
| `D4_4_CrashAfterCommittedPreservesCommitWithoutSecondWrite` | PASSED |
| `D4_5_CrashAfterConsumeBeforeWriteHasNoFd4Witness` | PASSED |
| `D4_6_AmbiguousSealFailureRecoversIntentAndDoesNotResend` | PASSED |
| `D4_7_DurableStopRecoversWithoutRelease` | PASSED |
| `D4_8_FailedDurableStopAllowsOnlyDiagnosticReplacementRelease` | PASSED |
| `D4_9_OwnerConcurrentDuplicateHasOneFd4Attempt` | PASSED |
| `D4_10_PreIntentStorageGateLeavesNoIntentOrFd4` | PASSED |
| `D4_11_DuringIntentStorageGatePreservesActualJournalState` | PASSED |
| `D4_12_ReplacementCannotAcquireWhileOwnerIsAlive` | PASSED |
| `D4_13_StopPersistenceGateDoesNotFabricateStopOrRelease` | PASSED |
| `D4_14_TwoRecoveryContendersHaveOneEffectiveWriterAndNoRelease` | PASSED |
| `D5_CR3_TwoIndependentOwnersHaveOneReleaseAndOneRefusal` | PASSED |
| `D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease` | PASSED |

Evidence: `08-d4-adjacent.log` and `testresults/d4-adjacent.trx`
(SHA-256 `3e9baadbaa2c5660b0869b9f9f416a07156f18985fb7d42fc31155711761e4c8`).
The retained `d4-evidence` directory contains 17 case roots (51 MB). Each case's
cgroup cleanup assertion passed; final inspection found only the host leaf.

## 10. Qualification matrix

| Case | Required evidence | Result | Actual evidence |
|---|---|---|---|
| L-C2-01 | Normal accepted stop consumes capability | **PASS** | `C2_T01` passed; `D5_CR3b` stop accepted and `capability_live=False` |
| L-C2-02 | Clock-unavailable stop consumes capability | **PASS** | `C2_T02` passed |
| L-C2-03 | Live-capability overflow stop consumes capability | **PASS** | `C2_T03b` passed |
| L-C2-04 | Failed durable stop cannot preserve reusable release authority | **PASS** | `C2_T04`, `C2_T07` and probe `StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2` passed |
| L-C2-05 | Second arbiter cannot produce a RELEASE after accepted stop | **PASS** | `C2_T07` and probe `DurableStopBlocksReleaseIntentFromSecondArbiter` passed; `D5_CR3b` fresh-lease replacement refused |
| L-C2-06 | Duplicate stop remains idempotent | **PASS** | `C2_T05` passed |
| L-C2-07 | Release-first durable history remains unchanged | **PASS** | `C2_T09` passed |
| L-C2-08 | Stop after Intent remains conservatively classified | **PASS** | `C2_T08` passed |
| L-C2-09 | Real owner reports capability invalidated after stop | **PASS** | `D5_CR3b` result `stop_accepted=True`, `stop_durable=True`, `capability_live=False` |
| L-C2-10 | Fresh-lease release is refused | **PASS** | `D5_CR3b` replacement result `replacement_decision=Refused` |
| L-C2-11 | Zero physical RELEASE after stop-first | **PASS** | `D5_CR3b` witness has no release markers; replacement `replacement_transport_attempts=0`; `C2_T06`/`C2_T12` passed |
| L-C2-12 | No surviving test-owned processes or cgroups | **PASS** | host `populated 0`, only `host` child, no test processes; `d4-*` cgroup removed; final state check |

No mandatory C2 case is `SKIPPED`, `BLOCKED` or `NOT RUN`.

## 11. Expected-versus-executed accounting

| Execution | Expected / discovered | Executed | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|---:|
| Supported profile regular phase | 510 | 510 | 510 | 0 | 0 |
| Supported profile `NonRootQualificationTests` | 31 | 31 | 31 | 0 | 0 |
| Supported profile `OwnerDeathQualificationTests` | 7 | 7 | 7 | 0 | 0 |
| Supported profile `IntegratedBarrierTests` | 15 | 15 | 1 | 0 | 14 |
| Supported profile `DomainAuthorityTests` | 21 | 21 | 2 | 0 | 19 |
| Supported profile `RecoveryHarness` | 5 | 5 | 3 | 0 | 2 |
| Supported profile `Wp05TerminationObserverTests` | 3 | 3 | 3 | 0 | 0 |
| Supported profile `Wp05SigpipeDispositionTests` | 1 | 1 | 1 | 0 | 0 |
| C2 focused | 13 | 13 | 13 | 0 | 0 |
| Contract probes | 13 | 13 | 13 | 0 | 0 |
| `D5_CR3b` isolated | 1 | 1 | 1 | 0 | 0 |
| Adjacent D4 class | 17 | 17 | 17 | 0 | 0 |

The focused filters were verified with `--list-tests`; none was empty. The
supported-profile total is 593 executed tests: 558 passed, 35 skipped, 0
failed. The first supported-profile attempt executed 541 tests (510 regular +
31 non-root), with 512 passed and 29 failed; it is reported separately and is
not counted as the corrected run.

## 12. Skips and limitations

1. **WSL2 only.** This is managed-VM WSL2 Linux runtime evidence, not
   native-distribution or bare-metal qualification. The native Linux
   distribution gate remains outstanding.
2. **35 supported-profile skips.** The skipped tests are the root-only
   cgroup-probe classes listed in section 5.2. They are outside the C2 claim;
   no C2-mandatory test was skipped. The limiting condition is the root-owned
   `/sys/fs/cgroup` mode `555` needed by their direct probe paths, not absence
   of the delegated non-root topology.
3. **Test-harness Debug-path default.** The supported profile required the
   test's documented `GAGAMBA_OWNER_DLL` override because
   `RecoveryHarness.LocateOwnerDll()` defaults to a `bin/Debug` owner while the
   qualification build is Release. The first attempt's 29 failures are
   preserved. The override points at the Release-built production owner; no
   test or production source was changed.
4. **D4 owner-build helper hang.** `EnsureOwnerBuilt()` reads redirected
   stdout/stderr to end before waiting for the build process. It can block on an
   inherited MSBuild server pipe. The fixture was made available from the
   completed build and independently rebuilt with the same project entrypoint;
   the test then passed. This is a test-harness issue, not a C2 result.
5. **Build warnings.** The solution build produced 48 environment SourceLink
   warnings from the local bundle remote. There were 0 errors and no production
   compile failure.
6. **No C1/C3/C4 or Owner integration.** Those contracts remain unimplemented
   and out of scope. No target was executed. No G3 or WP05 release acceptance is
   claimed.
7. **Retained artifacts.** The D4 evidence directory is 51 MB and the D5
   evidence directory is 3.2 MB. They are test-owned files under
   `/home/gaga/qualification/d685/`, not running processes. The session anchor
   PID 576 remains, documented in section 2.3.

## 13. Cleanup evidence

Final state check (`state-check.log`, `11-final-check.log`):

```text
host_procs: (empty)
host_events: populated 0, frozen 0
child_cgroups: /sys/fs/cgroup/gaga-delegated/host only
gaga_processes: none
anchor: 576 568 gaga Ss sleep infinity
```

- The `D5_CR3b` test's `AssertCgroupRemoved()` passed; no `d4-*` child cgroup
  remains.
- All 17 adjacent D4 cases asserted cgroup removal; no test-owned child cgroup
  remains.
- No `testhost`, `dotnet`, `bwrap`, fixture, coordinator or owner process
  remains.
- The owner fixture build caused two `MSBuild.dll /nodeReuse:true` nodes to
  remain in the host leaf. They were terminated after the build (`kill 68300
  68301` following `dotnet build-server shutdown`); the final state shows an
  empty host leaf and no MSBuild process. This is recorded as a cleanup event,
  not hidden.
- The pre-existing anchor (`sleep infinity` PID 576) is outside the host leaf
  and is not a test process. It is intentionally retained for the WSL session.
  Cleanup after review: kill PID 576 or reboot; this also removes the delegated
  tree.

## 14. Observed failures and disposition

### F-D685-01: supported-profile owner path (test harness / environment)

- Observation: first supported-profile run, `NonRootQualificationTests` 29
  failed / 2 passed; `System.InvalidOperationException : owner executable
  missing from the test build` at `NonRootQualificationTests.cs:433`.
- Cause: `RecoveryHarness.LocateOwnerDll()` defaults to
  `bin/Debug/net10.0`, while Phase A built Release. The test's documented
  `GAGAMBA_OWNER_DLL` override was not set.
- Classification: test-harness/environment configuration defect.
- Minimum counterexample: clean Release checkout + exact supported runner
  without the override -> 29 failures.
- Disposition: rerun with the test's documented `GAGAMBA_OWNER_DLL` pointing at
  the Release owner DLL; 31/31 passed. No test assertion was changed or
  weakened. The first failure is preserved in `04-supported-profile.log`.

### F-D685-02: `D5_CR3b` owner-build helper hang (test harness / infrastructure)

- Observation: first isolated `D5_CR3b` run discovered exactly one test but
  produced no result for more than 900 seconds after "Starting".
- Cause: `EnsureOwnerBuilt()` built the fixture, then blocked reading redirected
  stdout/stderr before `WaitForExit()`; an inherited MSBuild server pipe can
  keep the read open.
- Classification: test-harness/infrastructure hang, not a C2 authority result.
- Minimum counterexample: fresh checkout without the D4 owner DLL + isolated
  `D5_CR3b` filter -> hang in the owner-build helper.
- Disposition: build the fixture with its existing project entrypoint; rerun the
  isolated test. Result: 1 discovered, 1 executed, 1 passed (324 ms; evidence
  run 336 ms). The hang log and the successful logs are separate and both
  preserved. No test code was changed.

### F-D685-03: build warnings (environment, not a failure)

- Observation: 48 SourceLink/repository-remote warnings because the Linux
  checkout's `origin` is a local bundle path.
- Classification: environment build metadata; no production compile error.
- Disposition: recorded; not a C2 qualification failure.

No executed test demonstrated a contradiction of the C2 single-use
release-capability invariant. No production or test source was changed.

## 15. Environment-specific qualification result

WSL2 Linux: the required C2 legs executed and passed. The D5_CR3b real-process
leg, the C2 focused suite, the contract probes, and the adjacent D4 supervisor
class all passed; no C2-mandatory test was skipped. The supported runner's
corrected run exited `0` with 558 passed / 35 root-only skips / 0 failed.

Native Linux distribution: **OUTSTANDING**. No native-distro environment was
provisioned or claimed. This document does not close the native Linux gate and
does not claim full WP05 release qualification.

## 16. Independent review handoff

Producer: DeepSeek V4.1. Independent reviewer: GLM. This document is producer
evidence only; it does not claim GLM review or acceptance.

The review should verify:

- the exact Git binding and that `src/` and `tests/` still match
  `87e9e6f5...` / `84cfabb8...` / `e03515f4...`;
- actual WSL2 Linux execution logs and TRX files;
- the non-root, zero-effective-capability delegated profile and host placement;
- the 35 root-only skips are not C2-mandatory skips;
- the `D5_CR3b` isolated host, its artifact witness counts and cgroup removal;
- the corrected `C2_T03b` was discovered and executed;
- the first supported-profile failure and the `GAGAMBA_OWNER_DLL` correction;
- the D4 owner-build hang and the completed-fixture/reproducible-rebuild path;
- cleanup state, including the pre-existing anchor and the retained evidence
  directories;
- the separation of WSL2 runtime evidence from native-distro qualification.

Raw logs, TRX files and retained case artifacts are under
`/home/gaga/qualification/d685/` in the prepared WSL session; the Linux checkout
is `/home/gaga/qualification/gagamba-d6-8-5-c2`. The evidence commit is the tip
of `qualification/wp05-d6-8-5-c2-linux` after this document; its immediate
parent is `87e9e6f5b165176b27f066ff85025605fdaf31c6`.

## 17. Remaining WP05 release boundaries

- C1 (ambiguous ReleaseIntent), C3 (durable stop deadline/recovery) and C4
  (duplicate/concurrent release semantics) remain unimplemented. Their human
  contracts are approved, but implementation requires separate authorization.
- WP08 production Owner integration and WP10 execution integration remain
  outstanding.
- G3 is not accepted. WP05 release is **NOT QUALIFIED**.
- No target was executed; no package was published; no push or merge was
  performed.
- The native Linux distribution qualification is a separate outstanding lane.
  WSL2 evidence must not be silently substituted for it.

## 18. Material evidence references

| Artifact | SHA-256 |
|---|---|
| `wp05-d6-8-5-c2.bundle` | `ca518c19fd00eb96b959f66bf420f76e2698af2d027f9b7b41125d37d7d08e5a` |
| `00a-source-identity-initial.log` | `31f33dd374d9e547f80d5598937ea4c7e5ef51f133d550f0dfcd0df5ee5b3367` |
| `00b-source-identity-post.log` | `8d66f6e05899a8b01235a1cefa34cf802d23a880993eb6701dfd4d32613f111c` |
| `01-environment.log` | `cbf194bcb14ecf16473f87a304c2757ac721bd48807b5eaecfac4ffc21a1daa0` |
| `02-readiness.log` | `cb50fb5b439bb89499c1a7d59b56fb06c4ae259c0eb14805e02c5327b52c4e8c` |
| `reports/wp05-d685-readiness.txt` | `751e9f76ef4f84ba1d090bea0515f07af1be317c81ed2c38422f577d3a58cc79` |
| `03-build.log` | `2075fbc9a5a8342bb43fef61a4ffdb3fccdcdbe896d76ad461a9164984add4a3` |
| `04-supported-profile.log` (first attempt) | `2132156d85c380a6506c4f1b0769538d774dfaf7915f18aa3208f15ae1683169` |
| `04b-supported-profile-owner-override.log` | `d12eeef696d85bbe4508c3c98ec3ec42206c56d168d45b9fb5789ce65d18fa27` |
| `05-c2-focused.log` | `39100ac7b9403861b6e6344f8c5bb428497ad5d8585362bb8bad2cfc953e9b05` |
| `06-contract-probes.log` | `cffbccdbcc2f884d0925c41f75c502de5e132ca3ad1077c3c788b58a7c00bd96` |
| `07a-d5-cr3b-hang.log` | `6d63f301289126b28feca37c7ab8ded4e8f8b48ee7f863a916cdace5a082f8aa` |
| `07b-d5-cr3b-retry.log` | `cdf108b85e219f0715ec67c80f45d3fb819ee573eef83962e4a08416961756f8` |
| `07d-d5-cr3b-evidence.log` | `0a2e0ec071608618fb9641a18c4d59be6a76f24c7cbd08ee762194a7529743db` |
| `07e-d5-cr3b-artifacts.log` | `7702f424b0e374325a856484ebd0101fa7ce296ccd533acf5518347414375e0c` |
| `08-d4-adjacent.log` | `a5fbcac040c2e280e68a54644b3f679db092d3a1b50f88819ff7d925ce301bb1` |
| `09-hashes.log` | `4282aff0806341b7d8189feb3ce755b3bae6dd56bb44aea727fa1fe0ee22ed5d` |
| `10-d4-owner-build.log` | `bdd14d8f044f10775dbcea292c96ee8f77fdb78acb757edde6da99baf8960b20` |
| `11-final-check.log` | `911e7939f9f7ae1139e93f8f82bf7d27c6a369e3c04f62b6d300a0f88c8aa2ff` |
| `state-check.log` | `655541cf3b6a145bcebc70241a01a9c4abb4135c939ed91e677006b1881aa152` |
| `testresults/c2-focused.trx` | `f16a787c5d7db1c5bf5dbb8f8b3243b52118ea8de451c2808514ce265aa4541c` |
| `testresults/contract-probes.trx` | `1fb139752b816d2590498ae4e02ec3e0281eb1de32b022c0d4bccb46cdc5626f` |
| `testresults/d5-cr3b-retry.trx` | `9b78af0f49945b1c962d379d9c1a86cd35ea6ff8c0aa5e12f689dc06e103f0b7` |
| `testresults/d5-cr3b-evidence2.trx` | `dbe8d31af11a64f6c28061b51a34443820e08c2352fbf6706d5f448008795d40` |
| `testresults/d4-adjacent.trx` | `3e9baadbaa2c5660b0869b9f9f416a07156f18985fb7d42fc31155711761e4c8` |
| `D5 result.txt` | `6b016ab69aebe479fa339846647f067f6a4e28a2b5f38057d189864238ffe838` |
| `D5 witness.txt` | `99fc4bd7fb6fd8e2b69fc298d3493f61cefe9a3e6df961341ea1877e898b8b41` |
| `D5 owner.ready` | `80d835185ac517a6d3ec71b78e1ae7c681c1f10f7fde4afc9c1a87e1b68d4d0a` |
| `D5 barrier.txt` | `19ea9791cef879dd0501973e01c6154b10971356d0615e91a58f867cc3ae72c8` |
| `D5 replacement result` | `5b71d63287f07a5ea312219ebe318464e55e187aac404d8a87d51ef491daad22` |
| `D5 journal gen.a` and `gen.b` | `275407744bc6918063fbea750b2b4e181c3fd5b4fc420352ea7e34fe7f85fbe0` (both) |

Reproduction commands are recorded in sections 3, 4, 5.2, 6, 7, 8 and 9.
