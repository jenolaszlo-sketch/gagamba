# WP05 D6.8.5 GLM Independent C2 Linux Qualification Review

Date: 2026-10-23
Reviewer: GLM (independent)
Producer: DeepSeek V4.1
Scope: Independent verification of D6.8.5 C2 Linux (WSL2) runtime qualification
evidence and qualification integrity
Production changes: none. Test changes: none. Publication: none.

Disposition: **WP05 D6.8.5 GLM INDEPENDENT C2 LINUX REVIEW COMPLETE - OPTION A:
WSL2 C2 RUNTIME QUALIFICATION SUPPORTED; D5_CR3b VERIFIED; NO BLOCKING
FINDINGS; NATIVE-DISTRO QUALIFICATION OUTSTANDING; OWNER INTEGRATION AND G3
OUTSTANDING; RELEASE NOT QUALIFIED**

Non-blocking H-01/H-02 qualifications are recorded in sections 8 and 9 and are
part of this disposition.

## 1. Git and evidence identities

Repository: `C:\Users\Laszlos\source\repos\Gagamba`
Origin: `https://github.com/jenolaszlo-sketch/gagamba.git`

| Item | Identity | Verified |
|---|---|---|
| D6.8.4 preservation base | `87e9e6f5b165176b27f066ff85025605fdaf31c6` | yes (commit exists; tree `16c0fe915c6b9969664762a74427d77f05fca94f`) |
| D6.8.4 GLM closure review | `55f3f7a10b4d6860b8e7e02776bb3da73f801db9` | yes |
| Producer evidence commit | `d4e8bf88ce1661717537aaa3fb392bfe24c04466` | yes |
| Immediate parent of evidence commit | `87e9e6f5b165176b27f066ff85025605fdaf31c6` | yes (`git log` parent field) |
| Evidence tree | `e15a4e222ab25ebc55a9de42798630a292525305` | yes |
| Evidence document blob | `f1cb6a9c6c80ceb7386eaabe7bf235bdb4f6e7ee` | yes (`git ls-tree`) |
| Producer branch tip | `qualification/wp05-d6-8-5-c2-linux` = `d4e8bf8...` | yes (local; not on origin, not required) |
| Evidence commit diff vs base | evidence document + `docs/handoff.md` + `docs/work-queue.md` only | yes (`git diff --stat`) |

Review binding:

| Item | Identity |
|---|---|
| Review worktree | `C:\Users\Laszlos\source\repos\Gagamba-review-d685-glm` |
| Review branch | `review/wp05-d6-8-5-glm-c2-linux-qualification` |
| Review base (immediate parent of review commit) | `d4e8bf88ce1661717537aaa3fb392bfe24c04466` |

The producer branch is local and unpublished; per the review contract this is
acceptable and no origin presence was required.

## 2. Review scope

This review verifies the D6.8.5 C2 Linux qualification evidence: exact source
and test identity, authentic Linux execution, environment and delegated-cgroup
prerequisites, required C2 coverage, the D5_CR3b real-process case, runner
accounting, the two disclosed harness issues, cleanup, and the separation
between WSL2 runtime evidence and native-distro qualification.

Out of scope and not reopened: C2 contract design, C1/C3/C4, Owner
integration, G3, WP05 release acceptance, prior F-01/F-02 findings (closed at
`55f3f7a1...`; no contradictory observation was found). No production or test
file was modified by this review. No privileged infrastructure was created;
the pre-existing operator launcher `/root/wp05/run-as-gaga-in-host.sh` (SHA-256
`431c6c0b9c1b0caf4d1fd1ccf1a44b6fe736d06e035bff1fde7d989579b95b6c`, matching
the producer document) was used unmodified for reproduction placement.

## 3. Source/test tree verification

| Tree | At `87e9e6f5` (base) | At `d4e8bf8` (evidence) | Required | Verified |
|---|---|---|---|---|
| `src/` | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` | same | `84cfabb8...` | yes |
| `tests/` | `e03515f4d1ffb2171ae911a5b1da894b6f553d65` | same | prefix `e03515f4` resolved to this full SHA | yes |
| `native/` | `85631d2f04512376621d3d09f51d67fd90f45335` | same | - | yes (via producer logs; tree unchanged in commit diff) |
| `eng/` | `d5cd863fed7637989eb256aee0254be4bfe571f6` | same | - | yes |

Both trees are unchanged between the D6.8.4 preservation base and the D6.8.5
evidence commit; the evidence commit touches only docs.

File-level binding (Git blob -> checkout bytes):

| File | Git blob at `d4e8bf8` | SHA-256 of Linux checkout bytes (producer `09-hashes.log`) | SHA-256 of this review worktree's bytes | Match |
|---|---|---|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | `dca9ea1d6feea9fdb752de7d3e90f80431b85c58` | `14cc9254...` | `14cc92540558e8c676a042d68c8e80d996231bb8c2814c9ff1d2aa729b82354e` | yes |
| `tests/.../Wp05D68C2StopCapabilityTests.cs` | `47f70ae76d0287cbaf618b529ada5330e514cb1e` | `279be74d...` | `279be74df05fb002c07922b6d47b95116e42d700f82c317288287bb8b3c9d74f` | yes |
| `tests/.../Wp05ReleaseContractProbes.cs` | `698c07e21add0164938b99e7f22502ce7e822d10` | `cb83be6a...` | `cb83be6ae5511e86368e74bc5055f9a7823199a40c4e40ad227ca920e719edad` | yes |
| `tests/.../Wp05D4SupervisorTests.cs` | `3c7217cce600025b60849edc36654b83aedb028b` | `0ac567f8...` | `0ac567f8597977cdb7fd01f551ff7d2cf1a756b43c19e334aafad06062c1049d` | yes |
| `tests/.../Wp05D4OwnerHost.cs` | `c7523aac98402bfc5627447a971e1ec88bf35860` | `4a5b96ba...` | `4a5b96baf8c8d99e43231d4ef8c9a1226d2e2c07452a562a8d0e6d2a1db7a031` | yes |
| `eng/linux-m1-qualified-tests.sh` | `ed246b2a2e659085f5416d03f9fd26ffdfd73fa8` | - | - | yes (`git ls-tree`) |
| `eng/linux-m1-independent-readiness.sh` | `93c4dec426372c2cd89be05f3e617ed20f3b24eb` | - | - | yes (`git ls-tree`) |

The Linux checkout at `/home/gaga/qualification/gagamba-d6-8-5-c2` (gaga:gaga,
clean) reads HEAD `87e9e6f5...` with the same tree SHAs today (re-verified
during this review as `gaga` via `runuser`); the WSL-mounted Windows main
repository is on an unrelated branch, and the identity scripts' `pwd=` line
(`/mnt/c/...`) is only the invocation directory - all git identity lines use
`git -C /home/gaga/qualification/gagamba-d6-8-5-c2`, confirmed by reading the
captured scripts. Attribution to the Linux checkout is correct.

The corrected D6.8.4 test version was used: `Wp05D68C2StopCapabilityTests.cs`
blob `47f70ae7...` is identical at the D6.8.4 preservation base and at the
evidence commit; `C2_T03b_LiveCapabilityOverflowStopConsumesCapability`
(blob line 179 in `00a-source-identity-initial.log`) explicitly asserts a live
capability (`IsLive`, `ExpiresAtNs > overflowNow`) before the overflow stop and
consumption without durable stop (review worktree lines 179-202).

## 4. Linux environment and readiness

Verified from the captured `01-environment.log` (hash-matched) and re-verified
live during this review's reproduction:

| Attribute | Producer (captured 2026-10-22/23) | GLM live re-check (2026-10-23T03:05Z) |
|---|---|---|
| Distribution | Ubuntu 26.04.1 LTS (Resolute Raccoon) | same (`/etc/os-release`) |
| Kernel | `6.6.114.1-microsoft-standard-WSL2` | same (`/proc/version`) |
| Architecture | x86_64 | same |
| Test identity | `uid=1000(gaga)`, groups `1000(gaga)` | same (via existing root-only launcher) |
| CapInh/Prm/Eff/Amb | all `0000000000000000`; CapBnd `000001ffffffffff` | same in the launched process |
| Cgroup v2 | `cgroup2fs`, `nsdelegate`, delegated `/gaga-delegated` (`cpu memory pids`; subtree `memory pids`), host leaf `/gaga-delegated/host`, `cgroup.kill` present | same |
| .NET SDK / runtime | 10.0.113 / 10.0.13 | same |
| Bubblewrap | 0.11.1, `/usr/bin/bwrap`, `755`, non-setuid, SHA-256 `523da3e7...` | PASS (readiness rerun) |
| GCC / Python / glibc | 15.2 / 3.14.4 / 2.43 | same |
| Seccomp filter pin | 672 bytes, `2aa87901...` | PASS (`filter.artifact.pin`) |

The qualification process (not merely its parent shell) was non-root with zero
capabilities: the D5 run scripts record `id`, `/proc/self/cgroup`
(`0::/gaga-delegated/host`) and the four capability fields from inside the
executed session; additionally the D4 test's own `SkipUnlessQualified()`
guard (review worktree `Wp05D4SupervisorTests.cs:475-504`) skips on root,
nonzero `CapEff`, or missing artifacts - a passing execution is itself proof
the in-test process was non-root with `CapEff=0`. Host-leaf placement was
performed by the pre-existing root-only launcher, which writes its own PID to
the host `cgroup.procs` and execs `setpriv --reuid=1000 --regid=1000
--clear-groups` with `GAGAMBA_DELEGATED_CGROUP` set; verified by reading the
launcher (hash matches the producer document). No new privileged
infrastructure was provisioned by producer or reviewer.

Readiness preflight: producer command
`bash eng/linux-m1-independent-readiness.sh --source-rev 87e9e6f5b165176b27f066ff85025605fdaf31c6`
yielded `SUMMARY pass=56 fail=0`, `READINESS=PASS` (hash-verified report:
`751e9f76...`); `REVIEWER_ACCESS=UNVERIFIED` and
`INDEPENDENT_ACCEPTANCE=NOT_CLAIMED` were honestly emitted; no FAIL line and
no suppressed preflight failure exists in the report; the seccomp artifact
check was `INFO not present; generated pin is reproducible` (later built and
pinned). GLM independently re-ran the same preflight in the current session
against the same checkout: `SUMMARY pass=57 fail=0`, `READINESS=PASS` (57
because the built artifact now adds the `filter.artifact.pin` PASS). The 56
preflight checks are readiness, not executed C2 tests.

## 5. Managed C2 test results

All 24 producer-referenced logs/TRX/reports in the document's section-18
manifest were hash-verified against on-disk content in
`/home/gaga/qualification/d685/` (24/24 OK), plus the D5 case artifacts
(result, witness, owner.ready, barrier, replacement, both journal generations)
- all match, including `gen.a` == `gen.b` (`27540774...`).

C2-focused (`FullyQualifiedName~Wp05D68C2StopCapabilityTests`): TRX counters
independently parsed - 13 discovered, 13 executed, 13 passed, 0 failed, 0
skipped; the twelve expected identities including `C2_T03b` are present in the
TRX and in the execution log. Contract probes
(`FullyQualifiedName~Wp05ReleaseContractProbes`): 13 discovered, 13 executed,
13 passed, 0 skipped; all 13 identities verified in the TRX.

Required-case mapping (all executed, none skipped): normal first stop
(C2_T01), clock-unavailable stop (C2_T02), expired-capability overflow
(C2_T03), live-capability overflow (C2_T03b), failed durable persistence
(C2_T04), second-arbiter refusal (C2_T07), duplicate-stop idempotency
(C2_T05), release-first history preservation (C2_T09), post-Intent
conservative classification (C2_T08), concurrent stop/release (C2_T12),
replacement-context authority (C2_T11), plus C2_T06/C2_T10. No C2-mandatory
case was skipped, blocked or not run.

## 6. D5_CR3b real-process findings

Test `Gagamba.Execution.Linux.M1.Tests.D4.Wp05D4SupervisorTests.D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`,
run in dedicated single-test hosts (producer runs 07b/07c/07d each discovered
exactly one test; TRX `d5-cr3b-evidence2.trx` counters: 1 executed, 1 passed).

Producer's retained case
`gagamba-d4-D5-CR3b-d6aa0d443a0a4845a99318fbae28841e` (all hashes verified)
shows the required sequence:

1. Real owner process starts: `owner.ready` `owner_pid=67255`,
   `owner_start=67255:27026622`; witness `SupervisorStartedOwner`,
   `OwnerStarted`, `JournalWriterAcquired`.
2. Journal authority held by owner (`JournalWriterAcquired`, baseline 0).
3. Runtime stop accepted: `result.txt` `stop_accepted=True`.
4. StopRecord persistence succeeds: `stop_durable=True`,
   `stop_deadline=270271417591791`, journal sequence 4.
5. `capability_live=False`.
6. Release refused: `release_kind=Refused`, `release_durable=NotCommitted`,
   `release_delivery=NotAttempted`.
7. Owner terminated: witness `SupervisorSigkill` -> `OwnerTerminated
   exit=137`.
8. Durable StopRecord recovered: replacement reports `recovered=true`,
   `sequence=4`, `stop=true`, identical `stop_deadline`.
9. Fresh lease/replacement created: replacement `owner_pid=67269`,
   `writer=acquired`, baseline 4.
10. Replacement RELEASE refused: `release_attempted=true`,
    `replacement_decision=Refused`.
11. `replacement_transport_attempts=0`.
12. No RELEASE witness markers: counts for `PreWriteWitnessAcknowledged`,
    `WriteReturned`, `CoordinatorExitObserved`, `TestTransportAttempt`,
    `ReleaseCommitted` are all zero in the retained witness; only
    lifecycle/recovery events exist (11 events, counts verified).
13. Test-owned cgroup removed: test `AssertCgroupRemoved()` (source lines
    749-760) passed; post-run state shows only
    `/sys/fs/cgroup/gaga-delegated/host`, `populated 0`, no `d4-*` child.

Exit code 137: the test's `KillOwner()` (source lines 678-692) performs
`_owner.Kill(entireProcessTree: false)` (SIGKILL) and asserts
`ExitCode == 137` before recording `OwnerTerminated`. The exit code is the
test's intended termination mechanism at the intended point (after stop,
durable stop and refusal, before replacement), asserted by the test itself;
it is neither a cleanup failure nor a test failure.

Adjacent D4 supervisor regression: TRX `d4-adjacent.trx` independently parsed
- 17 discovered, 17 executed, 17 passed, 0 skipped, including
`D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`, `D5_CR3_...`,
`D4_7`, `D4_8`, `D4_13`. D5_CR3b is included within the 17 and was additionally
executed in isolated single-test hosts; the producer accounts these as separate
invocations (its section-11 table lists "D5_CR3b isolated 1" and "Adjacent D4
class 17"). Accounting note (non-blocking): the producer executed D5_CR3b
successfully three times in isolated hosts (retry 324 ms, first evidence run
268 ms in `07c`/`d5-cr3b-evidence.trx`, retained evidence run 336 ms in
`07d`/`d5-cr3b-evidence2.trx`), plus once inside the 17-test class run. The
document's section-8 run table lists only the hang, the retry and the retained
evidence run; the 07c execution and its TRX are on disk and hash-recorded in
`09-hashes.log` but are not itemized in the document's section-18 manifest.
No contradiction; all executions passed; the omission is a documentation gap
only.

## 7. Supported runner accounting

The supported isolation runner was used:
`CONFIGURATION=Release bash eng/linux-m1-qualified-tests.sh` (blob-verified).
Corrected run (`04b-supported-profile-owner-override.log`, hash-verified) -
per-phase summary lines independently reconciled:

| Phase | Total | Passed | Failed | Skipped | Verified in log |
|---|---:|---:|---:|---:|---|
| Regular parallel phase | 510 | 510 | 0 | 0 | yes |
| `Recovery.NonRootQualificationTests` | 31 | 31 | 0 | 0 | yes |
| `Recovery.OwnerDeathQualificationTests` | 7 | 7 | 0 | 0 | yes |
| `Recovery.IntegratedBarrierTests` | 15 | 1 | 0 | 14 | yes |
| `Recovery.DomainAuthorityTests` | 21 | 2 | 0 | 19 | yes |
| `Recovery.RecoveryHarness` | 5 | 3 | 0 | 2 | yes |
| `Recovery.Wp05TerminationObserverTests` | 3 | 3 | 0 | 0 | yes |
| `Launch.Wp05SigpipeDispositionTests` | 1 | 1 | 0 | 0 | yes |
| **Total** | **593** | **558** | **0** | **35** | reconciled |

Process-global classes ran in dedicated hosts in separate phases; the D4/D5
focused runs used separate invocations. The 35 skips are the root-only
cgroup-probe classes (`IntegratedBarrierTests` 14, `DomainAuthorityTests` 19,
`RecoveryHarness` 2) whose `Usable()` probes create directories directly under
root-owned mode-`555` `/sys/fs/cgroup`; their skip reasons
(`requires delegated Linux cgroup v2`) are recorded in the log. They are not
C2-mandatory legs; no C2 requirement is hidden by a skip; no failure was
reclassified as a skip (the first-attempt's 29 real failures are preserved in
`04-supported-profile.log` and were corrected via configuration, not
suppression). The 35 skips remain a visible limitation of wider Linux/WP05
qualification; the full Linux M1 suite is not universally qualified.

## 8. H-01 owner DLL provenance (harness/environment; non-blocking)

Observed behavior: first supported-profile run passed the 510-test regular
phase, then `NonRootQualificationTests` failed 29/31 with
`System.InvalidOperationException : owner executable missing from the test
build` (`NonRootQualificationTests.cs:433`). Preserved in
`04-supported-profile.log` (hash-verified); runner exit 1.

Evidence-based assessment:

1. Default path resolution: `RecoveryHarness.LocateOwnerDll()`
   (review worktree `RecoveryHarness.cs:65-88`) checks `GAGAMBA_OWNER_DLL`
   first, then falls back to
   `src/Gagamba.Execution.Linux.M1.Owner/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.Owner.dll`.
2. Why unsuitable: Phase A built Release only; no Debug owner existed.
3. Exact override used (from `04b` log header):
   `GAGAMBA_OWNER_DLL=/home/gaga/qualification/gagamba-d6-8-5-c2/src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/Gagamba.Execution.Linux.M1.Owner.dll`.
4. Identity: SHA-256 `6a7960b291fe36093c66911abcd92748fe2dcffbfb3fb453495574a37cc1a26d`
   (final-check `final_build_artifacts`, hash-verified log).
5. Built from expected source: the DLL is the Phase A `dotnet build
   Gagamba.sln -c Release` output from the pinned checkout (`03-build.log`,
   tree-verified before and after runs), not an external binary.
6. Not stale: produced in-session by the pinned build; hash recorded before
   (`09-hashes.log` via final check) and after all runs.
7. No security behavior change: the override only supplies the owner binary
   path to the test helper; the owner still executed as a real non-root
   zero-capability process under the same delegated topology; no assertion,
   guard or required check was weakened or bypassed.
8. Reproducibility of the documented procedure: NOT reproducible as written -
   `eng/linux-m1-qualified-tests.sh` does not mention `GAGAMBA_OWNER_DLL`
   (grep-verified), so a Release-only checkout + documented runner command
   fails without an override that is documented only in the helper source.

Consequence: harness/environment configuration defect; did not affect C2
authority behavior. Classification: non-blocking (the tested owner DLL is
bound to the expected source; provenance is adequate). Smallest correction:
have the qualified runner export `GAGAMBA_OWNER_DLL` to the Release owner path
(or make `LocateOwnerDll()` probe `Release` before `Debug`), and note the
override in the runner's usage text.

## 9. H-02 initial D5_CR3b hang (test harness/infrastructure; non-blocking)

Observed behavior: first isolated D5_CR3b invocation (`07`/`07a`,
timestamp 2026-10-23T01:30:02Z, hash-verified) discovered exactly one test,
logged `Starting:`, then produced no output and no test result; the producer
reports termination by the 900-second tool timeout. The run script
(`run-d5-cr3b.sh`) had no internal timeout wrapper.

Evidence-based findings:

1. Invocation/timestamp: 01:30:02Z; log preserved, not overwritten.
2. Stop point: after discovery and xUnit `Starting:`, i.e. inside the test's
   `EnsureOwnerBuilt()` (no case artifacts, owner process or child cgroup had
   been created at that point - `D4Case` construction precedes `StartOwner`,
   and the owner/cgroup only appear after `EnsureOwnerBuilt()` returns).
3. Involvement of build output/handles/waits: the D4 owner fixture DLL was
   completed during the hung attempt (timestamp 09:30 local = 01:30Z), so the
   `dotnet build` child finished while the test host did not progress. Source
   review confirms `EnsureOwnerBuilt()` calls
   `build.StandardOutput.ReadToEnd()` and `build.StandardError.ReadToEnd()`
   before `build.WaitForExit()` (review worktree
   `Wp05D4SupervisorTests.cs:539-541`) - the classic concurrent-pipe pattern
   that can block indefinitely when a pipe handle remains open (e.g. an
   MSBuild server node holding the inherited pipe).
4. Termination of the hung execution: producer reports the agent tool timeout
   (900 s). The logs contain no `RUN_EXIT` line, consistent with an externally
   terminated invocation. Residual `MSBuild.dll /nodeReuse:true` nodes from
   the fixture build (PIDs 68300, 68301) were later terminated after
   `dotnet build-server shutdown` and are recorded as a cleanup event in the
   producer's section 13.
5. Left-behind resources: no test-owned process and no test-owned cgroup -
   the hang occurred before owner start and cgroup creation; post-run state
   logs and GLM's live inspection show no surviving test workload. The two
   MSBuild nodes were build infrastructure, terminated and recorded.
6. What changed before the successful rerun: the fixture DLL now existed, so
   `EnsureOwnerBuilt()` short-circuited at its existence check (source line
   525, `if (File.Exists(dll)) return dll;`) and never entered the hanging
   build path (retry log `07b` explicitly records the prebuilt DLL and its
   hash `881daf61...`).
7. Did the successful rerun avoid the original hanging path: yes - it used the
   already-built fixture. Producer also ran an explicit, reproducible rebuild
   of the same project entrypoint (`10-d4-owner-build.log`, `timeout 180`
   wrapped, exit 0) producing the byte-identical DLL `881daf61...`, proving
   the build path itself completes.
8. Reproducibility of the hang: not reproduced and not re-attempted by this
   review (a hang-prone invocation is not repeated without necessity; the
   producer's own later evidence runs were timeout-wrapped). The root cause is
   recorded as: mechanism consistent with a redirected-pipe/MSBuild-server
   deadlock in `EnsureOwnerBuilt()`, exact trigger unresolved.
9. Material effect on qualification reproducibility: it is a first-run-only
   harness hazard on fresh checkouts (fixture absent). It does not affect the
   executed C2 result: the qualification unit (the test) passed in the
   producer's runs and independently in GLM's rerun; the fixture provenance is
   established by the explicit rebuild; the hung attempt produced no test
   result that could contaminate the record. It is NOT a C2 production defect
   - the hang is entirely inside a test-side build helper, before any
   production code path executes.

Consequence: test-harness/infrastructure issue; root cause not fully
established and explicitly recorded as unresolved in detail. Classification:
non-blocking for the WSL2 C2 qualification claim. Smallest correction: wrap
the build in `WaitForExit(timeout)`-based asynchronous reads (or pass
`/nodeReuse:false` / `/nologo` with `UseShellExecute=false` and read both
pipes concurrently), and have focused-run scripts timeout-wrap `dotnet test`
as the producer's evidence scripts already do.

## 10. Cleanup verification

Verified from the original execution evidence (hash-matched logs):

- `state-check.log` (01:58:28Z) and `11-final-check.log` (01:56:11Z):
  host `cgroup.procs` empty, `cgroup.events` `populated 0, frozen 0`, only
  child cgroup `/sys/fs/cgroup/gaga-delegated/host`, `gaga_processes: none`
  except the anchor.
- D5_CR3b `AssertCgroupRemoved()` passed; all 17 adjacent D4 cases asserted
  cgroup removal; no `d4-*` child cgroup remains.
- No `testhost`, `dotnet`, `bwrap`, fixture, coordinator or owner process
  remains. The two MSBuild node leftovers from the fixture build were
  terminated and recorded.
- Pre-existing session anchor: PID 576, PPID 568, user gaga,
  `sleep infinity`, in `/init.scope` - outside the qualification host leaf,
  capability-free, not a test or target process, present before the task
  (verified live during this review: same PID/PPID/args). Its documented
  lifecycle (kill PID 576 or reboot; delegated tree removed with it) is the
  operator's, not this review's.
- GLM live inspection after independent reproduction: host leaf empty,
  `populated 0`, only `/sys/fs/cgroup/gaga-delegated/host/` child, no test
  processes, anchor unchanged. (GLM's own rerun log's instantaneous post-run
  snapshot showed three transient `dotnet`/testhost PIDs of the rerun itself
  still draining; the live re-check minutes later shows a clean state. No
  surviving workload.)

One recorded anomaly: `state-check.log`'s `git_status_tracked` section failed
with a transient git `dubious ownership` error. It is preserved as-is. The
authoritative post-run source checks succeeded (`00b` at 01:55:54Z,
`tracked_surface_diff_exit=0`; final-check source identity at 01:56:11Z), and
GLM re-verified today as `gaga` that the checkout is at HEAD `87e9e6f5...`
with a clean tracked tree. Non-blocking.

## 11. Independent reproduction

Evidence grades used in this review, kept explicit:

- Grade 1 (GLM-executed in the live WSL2 session, 2026-10-23T03:05-03:07Z, as
  `gaga` uid 1000, zero capabilities, in `/gaga-delegated/host`, via the
  pre-existing launcher, all invocations timeout-wrapped):
  - Readiness preflight rerun: `SUMMARY pass=57 fail=0`, `READINESS=PASS`,
    source bound to `87e9e6f5...`, clean tracked tree.
  - C2-focused suite rerun: 13 discovered, 13 passed, 0 failed, 0 skipped,
    exit 0 (TRX `4e9c1a6f...` under `/home/gaga/qualification/d685-review-glm/`).
  - Contract probes rerun: 13 passed, exit 0 (TRX `aef13ae9...`).
  - D5_CR3b isolated rerun: 1 discovered, 1 executed, 1 passed, exit 0, 300 s
    watchdog (TRX `6cd62ba4...`). GLM's fresh case
    (`gagamba-d4-D5-CR3b-b5ee4c6ec87d42fcb66158a3d7d4e684`) independently
    reproduced every required observation: `stop_accepted=True`,
    `stop_durable=True`, `capability_live=False`, `release_kind=Refused`,
    SIGKILL -> `exit=137`, recovered durable stop with identical deadline,
    fresh-lease replacement `release_attempted=true`,
    `replacement_decision=Refused`, `replacement_transport_attempts=0`, zero
    forbidden release markers, cgroup removed. The rerun binaries were
    hash-verified identical to the producer's (tests DLL `a1f9d7e8...`, M1 DLL
    `ebc73e64...`, Release owner `6a7960b2...`, D4 fixture `881daf61...`).
- Grade 2 (producer records hash-verified by GLM): supported-profile corrected
  run (593 results), first-attempt failure (29), d4-adjacent 17/17, hang log,
  environment/readiness reports, D5 retained case artifacts, hash manifests.
- Grade 3 (claims accepted from producer records without re-execution): the
  510-test regular phase's internal content, `NonRootQualificationTests` and
  `OwnerDeathQualificationTests` case bodies, adjacent D4 class re-execution,
  and hang reproduction.

The adjacent D4 class was not re-executed by GLM (the mandatory C2 legs were
prioritized; the class's 17/17 result is hash-verified producer evidence).
The hanging invocation was not repeated, per the review contract.

## 12. C2 qualification verdict

**Q1: Does the evidence support C2 behavior under WSL2? YES.**

- Source and test identity: `src/` `84cfabb8...` and `tests/` `e03515f4...`
  unchanged from the D6.8.4 preservation base through the evidence commit;
  checkout bytes match Git content; corrected T03b present.
- Managed C2 tests: 13/13 C2-focused and 13/13 contract probes passed, TRX
  counters independently parsed, identities verified.
- D5_CR3b: producer's retained artifacts and GLM's independent rerun both show
  a real owner stop consuming the capability, durable StopRecord, refusal of
  the original release, SIGKILL termination (asserted 137), recovery of the
  durable stop across a fresh lease, refusal of replacement RELEASE with zero
  transport attempts and zero release witness markers, and cgroup removal.
- No physical RELEASE: no release markers in any retained or rerun witness;
  `replacement_transport_attempts=0` in both.
- Cleanup: clean in producer records and live re-check.
- Harness provenance: owner DLL and D4 fixture bound to the pinned source by
  hash and explicit rebuild; H-01/H-02 are harness issues that do not alter
  test assertions or security behavior; the failed first attempt is preserved,
  not suppressed.

**Q2: Does it qualify a native Linux distribution? NO.** All evidence is from
Ubuntu 26.04.1 on WSL2 (managed-VM kernel
`6.6.114.1-microsoft-standard-WSL2`). This is genuine Linux execution
evidence for the WSL2 lane only; it does not establish qualification on Ubuntu
or any other distribution running natively on a VM, physical host or other
native environment. The native-distro gate remains outstanding and was not
claimed by the producer. Even a fully supported Q1 does not establish WP05
release acceptance: G3, Owner (WP08) and execution (WP10) integration, and
C1/C3/C4 implementation remain outstanding; no target was executed; the 35
root-only supported-profile skips and the D4/D5 process-level scope remain
limitations of wider Linux/WP05 qualification.

## 13. Remaining Linux/native-distro and WP05 boundaries

1. Native Linux distribution qualification: OUTSTANDING (separate lane).
2. Wider Linux M1: 35 supported-profile skips (root-only cgroup-probe classes)
   remain; the full suite is not universally qualified under non-root.
3. C1 (ambiguous ReleaseIntent), C3 (durable stop deadline/recovery), C4
   (duplicate/concurrent release semantics): unimplemented.
4. WP08 Owner integration, WP10 execution integration: outstanding.
5. G3: not accepted. WP05 release: NOT QUALIFIED.
6. H-02 root cause unresolved in detail (pipe-deadlock hypothesis; fixture
   build path follow-up recommended, no fix implemented in this review).

## 14. Blocking and non-blocking findings

Blocking: none.

Non-blocking:

| ID | Finding | Classification | Smallest correction |
|---|---|---|---|
| H-01 | Supported-runner procedure fails on Release-only checkouts; `GAGAMBA_OWNER_DLL` override documented only in helper source (`RecoveryHarness.cs:65-88`) | harness/environment; non-blocking (owner provenance verified) | Runner exports the Release owner path or `LocateOwnerDll()` probes Release before Debug; document the override |
| H-02 | First isolated D5_CR3b hung in `EnsureOwnerBuilt()` (`ReadToEnd` before `WaitForExit`, `Wp05D4SupervisorTests.cs:539-541`); exact trigger unresolved; first-run-only hazard on fresh checkouts | test harness/infrastructure; non-blocking (no test result contaminated; fixture rebuilt byte-identically) | Concurrent/timeout-bounded pipe reads or `/nodeReuse:false`; timeout-wrap focused `dotnet test` invocations |
| N-01 | D5_CR3b executed successfully four times (3 isolated + 1 in-class); the 07c isolated execution (268 ms, `d5-cr3b-evidence.trx`) and its log are not itemized in the document's run table or section-18 manifest | documentation gap; non-blocking | Add 07c/`d5-cr3b-evidence.trx` to the manifest with one clarifying line |
| N-02 | `state-check.log` git section failed transiently (`dubious ownership`), preserved as-is; authoritative post-run checks succeeded | recording anomaly; non-blocking | No action required; optionally run state checks with explicit `HOME`/user context |

## 15. Final disposition

**WP05 D6.8.5 GLM INDEPENDENT C2 LINUX REVIEW COMPLETE - OPTION A: WSL2 C2
RUNTIME QUALIFICATION SUPPORTED; D5_CR3b VERIFIED; NO BLOCKING FINDINGS;
NATIVE-DISTRO QUALIFICATION OUTSTANDING; OWNER INTEGRATION AND G3 OUTSTANDING;
RELEASE NOT QUALIFIED**

with the non-blocking H-01 and H-02 qualifications recorded above.

GLM review evidence retained under `/home/gaga/qualification/d685-review-glm/`
(readiness 57/0; C2-focused 13/13; contract probes 13/13; isolated D5_CR3b
1/1 with fresh case artifacts; TRX/log hashes in section 11). Raw producer
evidence under `/home/gaga/qualification/d685/` was verified by hash and
content inspection; the producer worktree, canonical checkout and prior
qualification worktrees were not modified. This review makes no change to
production or test files and claims neither G3 nor WP05 release acceptance.
