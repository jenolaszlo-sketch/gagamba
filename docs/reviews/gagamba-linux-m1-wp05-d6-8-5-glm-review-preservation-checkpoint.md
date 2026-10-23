# WP05 D6.8.5 GLM Option A C2 Linux Qualification Review Preservation Checkpoint

Status: **PRESERVED - GLM OPTION A: WSL2 C2 RUNTIME QUALIFICATION SUPPORTED;
D5_CR3b INDEPENDENTLY REPRODUCED; H-01 H-02 N-01 TRACKED; NATIVE-DISTRO
QUALIFICATION OUTSTANDING; OWNER INTEGRATION AND G3 OUTSTANDING; RELEASE NOT
QUALIFIED**

Preservation engineer: DeepSeek V4.1. This checkpoint preserves the D6.8.5
WSL2 C2 runtime qualification evidence and GLM's independent Option A review as
immutable sibling histories. No production, test, native, build, schema or
prior evidence content was changed during preservation.

## 1. Repository, base and preservation binding

- Repository: `C:\Users\Laszlos\source\repos\Gagamba`
  (origin `https://github.com/jenolaszlo-sketch/gagamba.git`).
- D6.8.4 preservation base and immediate parent of the D6.8.5 evidence commit:
  `87e9e6f5b165176b27f066ff85025605fdaf31c6` (tree
  `16c0fe915c6b9969664762a74427d77f05fca94f`).
- Preservation worktree:
  `C:\Users\Laszlos\source\repos\Gagamba-preserve-d685`.
- Preservation branch:
  `preservation/wp05-d6-8-5-glm-c2-linux-qualification-review`.
- Preservation commit: created immediately after this document, with immediate
  parent `d4e8bf88ce1661717537aaa3fb392bfe24c04466`. Its full SHA and tree are
  recorded in the preservation report; no self-referential SHA is embedded
  here.
- Producer branch: `qualification/wp05-d6-8-5-c2-linux` = `d4e8bf88...`.
- Review branch: `review/wp05-d6-8-5-glm-c2-linux-qualification` =
  `89464f5f...`.

## 2. Exact Git and evidence identities

| Item | Identity | Verified |
|---|---|---|
| D6.8.4 preservation base | `87e9e6f5b165176b27f066ff85025605fdaf31c6` | yes (commit object; tree `16c0fe915c6b9969664762a74427d77f05fca94f`) |
| D6.8.5 producer commit | `d4e8bf88ce1661717537aaa3fb392bfe24c04466` | yes (commit object) |
| Producer immediate parent | `87e9e6f5b165176b27f066ff85025605fdaf31c6` | yes |
| Producer tree | `e15a4e222ab25ebc55a9de42798630a292525305` | yes |
| Producer evidence document | `docs/evidence/linux-m1/wp05-d6-8-5-c2-linux-qualification.md` | yes |
| Producer evidence blob | `f1cb6a9c6c80ceb7386eaabe7bf235bdb4f6e7ee` | yes (`git ls-tree`) |
| Producer change scope | exactly three files, +728/-2: evidence document +718, `docs/handoff.md` +5/-1, `docs/work-queue.md` +5/-1 | yes (`git diff --numstat`) |
| GLM review commit | `89464f5f214615ba94051e438d9b4979483ae5bf` | yes (commit object) |
| Review immediate parent | `d4e8bf88ce1661717537aaa3fb392bfe24c04466` | yes |
| Review tree | `41bf98212ab764b44ca8528badc9013c8a7979b9` | yes |
| Review document | `docs/reviews/gagamba-linux-m1-wp05-d6-8-5-glm-independent-c2-linux-qualification-review.md` | yes |
| Review-document blob | `f168fce6e1eb83f7d22a24096a3b02530f8eea66` | yes (`git ls-tree`) |
| Review change scope | exactly one file, +506/-0 | yes |
| Production `src/` tree | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` | yes at base, producer, review and preservation |
| Test tree | `e03515f4d1ffb2171ae911a5b1da894b6f553d65` | yes at base, producer, review and preservation |
| Native tree | `85631d2f04512376621d3d09f51d67fd90f45335` | unchanged (docs-only commits) |
| `eng/` tree | `d5cd863fed7637989eb256aee0254be4bfe571f6` | unchanged (docs-only commits) |

Producer baseline evidence identities retained from the D6.8.4 closure:
D6.8.4 test commit `0aa201c3c5689690217ff5ae429de0bcda6943ff`, D6.8.4 evidence
commit `01da5d38a60a6f369c45a87f5bb18e2d8240081c`, GLM closure review
`55f3f7a10b4d6860b8e7e02776bb3da73f801db9`. No prior evidence or review
document was modified.

## 3. Producer and review sibling ancestry

```text
87e9e6f5  D6.8.4 preservation (base)
    |
d4e8bf88  D6.8.5 WSL2 C2 runtime evidence (producer)
    |
    +---- 89464f5f  GLM independent Option A review   (sibling A)
    |
    +---- NEW       D6.8.5 preservation              (sibling B, this commit)
```

Both siblings are direct children of `d4e8bf88...`; neither is an ancestor of
the other (`git merge-base --is-ancestor` verified both directions in the
preservation audit). The review was not merged, rebased, amended or
cherry-picked. The preservation commit changes documentation only and is not
based on the review commit.

## 4. GLM Option A disposition (verbatim)

`WP05 D6.8.5 GLM INDEPENDENT C2 LINUX REVIEW COMPLETE - OPTION A: WSL2 C2 RUNTIME QUALIFICATION SUPPORTED; D5_CR3b VERIFIED; NO BLOCKING FINDINGS; NATIVE-DISTRO QUALIFICATION OUTSTANDING; OWNER INTEGRATION AND G3 OUTSTANDING; RELEASE NOT QUALIFIED`

Confirmed against the committed review document blob
`f168fce6e1eb83f7d22a24096a3b02530f8eea66` (Status and Final disposition
sections), not from a producer summary alone. The review records the
non-blocking H-01 and H-02 qualifications as part of the disposition, plus
N-01/N-02 recording notes.

## 5. Source and test tree integrity

- Production `src/` tree `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` and test
  tree `e03515f4d1ffb2171ae911a5b1da894b6f553d65` are identical at the
  D6.8.4 preservation base, the D6.8.5 producer, the GLM review and this
  preservation commit. Both producer and review commits are docs-only.
- The producer evidence commit modifies only the evidence document,
  `docs/handoff.md` and `docs/work-queue.md`; the review commit adds only its
  review document. No production, test, native, `eng/`, schema, fixture, build
  or packaging file changed in either commit.
- GLM hash-verified the four key source/test files from the pinned Linux
  checkout against Git blobs and SHA-256 checkout bytes: `ReleaseArbiter.cs`
  (blob `dca9ea1d...`, SHA-256 `14cc9254...`),
  `Wp05D68C2StopCapabilityTests.cs` (blob `47f70ae7...`, SHA-256
  `279be74d...`), `Wp05ReleaseContractProbes.cs` (blob `698c07e2...`, SHA-256
  `cb83be6a...`), `Wp05D4SupervisorTests.cs` (blob `3c7217cc...`, SHA-256
  `0ac567f8...`) and `Wp05D4OwnerHost.cs` (blob `c7523aac...`, SHA-256
  `4a5b96ba...`). The corrected live-overflow test `C2_T03b` is present and
  was executed, not skipped.
- The producer's `00b-source-identity-post.log` records the same HEAD and
  trees after the runs with `tracked_surface_diff_exit=0` for
  `src tests native eng`; GLM re-verified the checkout as `gaga`
  (`HEAD 87e9e6f5...`, clean tracked tree).

## 6. WSL2 environment provenance

All evidence is managed-VM WSL2 Linux, not a native distribution, native VM,
physical host or bare metal.

| Attribute | Observed |
|---|---|
| Distribution | Ubuntu 26.04.1 LTS (Resolute Raccoon) |
| Kernel | `6.6.114.1-microsoft-standard-WSL2` |
| Architecture | `x86_64`; WSL version 2 |
| Qualification identity | `uid=1000(gaga) gid=1000(gaga) groups=1000(gaga)` |
| CapInh / CapPrm / CapEff / CapAmb | all `0000000000000000` (zero capabilities) |
| CapBnd | `000001ffffffffff` |
| Cgroup | `cgroup2fs`, `nsdelegate`; delegated `/sys/fs/cgroup/gaga-delegated` (`cpu memory pids`; subtree `memory pids`); host leaf `/gaga-delegated/host`; `cgroup.kill` present |
| Placement | process cgroup `0::/gaga-delegated/host`, placed by the pre-existing root-only operator launcher `/root/wp05/run-as-gaga-in-host.sh` (SHA-256 `431c6c0b9c1b0caf4d1fd1ccf1a44b6fe736d06e035bff1fde7d989579b95b6c`), used unmodified; `setpriv --reuid=1000 --regid=1000 --clear-groups` |
| Toolchain | .NET SDK 10.0.113 / runtime 10.0.13; bubblewrap 0.11.1 (`523da3e7...`, non-setuid); GCC 15.2; Python 3.14.4; glibc 2.43; Git 2.53.0 |
| Seccomp artifact | 672 bytes, SHA-256 `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |
| Readiness | producer `SUMMARY pass=56 fail=0`, `READINESS=PASS` (report `751e9f76...`); GLM independent rerun `pass=57 fail=0` |
| Session anchor | pre-existing `sleep infinity` PID 576 in `/init.scope`, outside the host leaf, capability-free, not a test/target process; operator-owned lifecycle |

The in-test `SkipUnlessQualified()` guard (root, nonzero `CapEff` or missing
artifacts) passed, independently confirming the executed C2 process was
non-root with `CapEff=0`. No new privileged service, setuid helper or cgroup
delegation was created by producer or reviewer.

## 7. Managed and real-process C2 results

Producer results (Grade 2: hash-verified records, not re-executed by GLM):

- C2-focused suite (`Wp05D68C2StopCapabilityTests`): 13 discovered, 13
  executed, 13 passed, 0 failed, 0 skipped, including the corrected
  `C2_T03b_LiveCapabilityOverflowStopConsumesCapability`.
- Historical contract probes (`Wp05ReleaseContractProbes`): 13/13 passed,
  0 skipped; probes not weakened or modified.
- Corrected supported isolation runner: 593 total, 558 passed, 35 skipped,
  0 failed, exit 0. The 35 skips are the root-only cgroup-probe classes
  (`IntegratedBarrierTests` 14, `DomainAuthorityTests` 19, `RecoveryHarness`
  2); none is a C2-mandatory leg and no failure was reclassified as a skip.
- Adjacent D4 supervisor class: 17/17 passed, including `D5_CR3b`; reported
  separately from the isolated `D5_CR3b` invocation.
- Isolated `D5_CR3b`: 1 discovered, 1 executed, 1 passed.

GLM independently executed (Grade 1, live WSL2 session 2026-10-23T03:05Z,
`gaga` uid 1000, zero capabilities, `/gaga-delegated/host`, timeout-wrapped):
readiness 57/0; C2-focused 13/13 (TRX `4e9c1a6f...`); contract probes 13/13
(TRX `aef13ae9...`); isolated `D5_CR3b` 1/1 (TRX `6cd62ba4...`, 300 s
watchdog). Rerun binaries were hash-identical to the producer's (tests DLL
`a1f9d7e8...`, M1 DLL `ebc73e64...`, Release owner `6a7960b2...`, D4 fixture
`881daf61...`).

Producer-only executions verified through retained artifacts are not conflated
with independently rerun tests. Grade 3 (accepted from hash-verified producer
records without re-execution) covers the 510-test regular phase's internal
content, `NonRootQualificationTests` and `OwnerDeathQualificationTests` case
bodies, the adjacent D4 class re-execution count, and the hang reproduction.

## 8. Independent reproduction grades

| Grade | Meaning | D6.8.5 content |
|---|---|---|
| Grade 1 | GLM executed live in the WSL2 qualification topology | readiness 57/0; C2-focused 13/13; contract probes 13/13; isolated `D5_CR3b` 1/1 with fresh case artifacts |
| Grade 2 | Producer records hash-verified by GLM | corrected supported-profile 593 (558/35/0); first-attempt 29 failures; adjacent D4 17/17; hang log; environment/readiness reports; D5 retained case artifacts; 24/24 manifest hashes plus D5 case artifacts |
| Grade 3 | Claims accepted without re-execution | regular-phase internal content; NonRoot/OwnerDeath case bodies; adjacent D4 class re-execution; hang reproduction (not repeated) |

The adjacent D4 class was not re-executed by GLM; its 17/17 result is
hash-verified producer evidence. The hang-prone invocation was not repeated.

## 9. D5_CR3b journal, witness and cleanup evidence

Producer retained case:
`/home/gaga/qualification/d685/d5-evidence/gagamba-d4-D5-CR3b-d6aa0d443a0a4845a99318fbae28841e`.

- Real owner `owner_pid=67255`, `owner_start=67255:27026622`; witness
  `SupervisorStartedOwner`, `OwnerStarted`, `JournalWriterAcquired`.
- `stop_accepted=True`; `stop_durable=True`; journal sequence 4;
  `stop_deadline=270271417591791`; `capability_live=False`.
- Release refused: `release_kind=Refused`, `release_durable=NotCommitted`,
  `release_delivery=NotAttempted`.
- Owner terminated by asserted SIGKILL (`SupervisorSigkill` ->
  `OwnerTerminated exit=137`; the test asserts exit 137).
- Durable `StopRecord` recovered after owner death (`recovered=true`,
  `sequence=4`, `stop=true`, identical deadline); fresh lease/replacement
  `owner_pid=67269`, `writer=acquired`, baseline 4.
- Replacement RELEASE refused (`release_attempted=true`,
  `replacement_decision=Refused`) with
  `replacement_transport_attempts=0`.
- Witness contains only lifecycle/recovery events (11 events:
  `JournalWriterAcquired` 1, `OwnerPrepared` 1, `OwnerStarted` 2,
  `OwnerTerminated` 1, `RecoveryFinished` 1, `RecoveryProcessExited` 1,
  `RecoveryStarted` 1, `SupervisorBarrierObserved` 1, `SupervisorSigkill` 1,
  `SupervisorStartedOwner` 1); zero `PreWriteWitnessAcknowledged`,
  `WriteReturned`, `CoordinatorExitObserved`, `TestTransportAttempt` or
  `ReleaseCommitted` markers.
- Journal `gen.a` and `gen.b` are identical
  (`275407744bc6918063fbea750b2b4e181c3fd5b4fc420352ea7e34fe7f85fbe0`).
- Retained artifact hashes: `result.txt` `6b016ab6...`, `witness.txt`
  `99fc4bd7...`, `owner.ready` `80d83518...`, `barrier.txt` `19ea9791...`,
  replacement result `5b71d632...`.

GLM's fresh independent case
`gagamba-d4-D5-CR3b-b5ee4c6ec87d42fcb66158a3d7d4e684` reproduced every
required observation: durable stop, capability invalidation, refused original
release, asserted SIGKILL 137, recovery with identical deadline, fresh-lease
refusal with zero transport attempts, zero forbidden release markers and
cgroup removal.

Cleanup: test `AssertCgroupRemoved()` passed; host leaf `cgroup.procs` empty
and `cgroup.events` `populated 0, frozen 0`; only
`/sys/fs/cgroup/gaga-delegated/host` remains; no `d4-*` child cgroup; no
`testhost`, `dotnet`, `bwrap`, fixture, coordinator or owner process remains.
Two `MSBuild.dll /nodeReuse:true` build nodes from the fixture build were
terminated after `dotnet build-server shutdown` (recorded, not hidden). The
pre-existing anchor PID 576 is outside the host leaf and not test-owned. The
`state-check.log` transient `dubious ownership` recording anomaly (GLM N-02)
is preserved as-is; authoritative post-run source checks succeeded.

## 10. H-01 - Owner DLL default path

Status: **NON-BLOCKING; RUNNER CORRECTION OUTSTANDING.**

- Observation: the first supported-profile run passed the 510-test regular
  phase, then `NonRootQualificationTests` failed 29/31 with
  `System.InvalidOperationException : owner executable missing from the test
  build` (`NonRootQualificationTests.cs:433`); runner exit 1. Preserved in
  `04-supported-profile.log` (SHA-256 `2132156d...`).
- Cause: `RecoveryHarness.LocateOwnerDll()` defaults to the `bin/Debug`
  owner path while the qualification build is Release; the documented
  `GAGAMBA_OWNER_DLL` override is not exported by the runner and is documented
  only in helper source.
- Exact override used in the corrected run:
  `GAGAMBA_OWNER_DLL=/home/gaga/qualification/gagamba-d6-8-5-c2/src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/Gagamba.Execution.Linux.M1.Owner.dll`
  (from the hash-verified `04b-supported-profile-owner-override.log`).
- Owner binary identity: SHA-256
  `6a7960b291fe36093c66911abcd92748fe2dcffbfb3fb453495574a37cc1a26d`,
  produced in-session by the Phase A `dotnet build Gagamba.sln -c Release`
  from the pinned checkout, not an external or stale binary; hash recorded
  before and after the runs.
- GLM verified the owner artifact is correctly bound to the expected source
  and that the override changed only the owner binary path, not any assertion,
  guard, required check or security behavior; the owner still ran as a real
  non-root zero-capability process in the same delegated topology.
- Reproducibility limitation: **NOT reproducible as written** - a Release-only
  checkout plus the documented runner command fails without the override
  because `eng/linux-m1-qualified-tests.sh` does not mention
  `GAGAMBA_OWNER_DLL` (grep-verified). Corrections are outstanding: export the
  Release owner path from the runner or probe Release before Debug in
  `LocateOwnerDll()`, and document the override in the runner usage.

## 11. H-02 - Initial fixture-build hang

Status: **NON-BLOCKING; ROOT CAUSE UNRESOLVED.**

- Initial isolated `D5_CR3b` execution (2026-10-23T01:30:02Z) discovered
  exactly one test, logged xUnit `Starting:`, then produced no output and no
  test result; terminated by the 900-second tool timeout. The run script had
  no internal timeout wrapper. Log preserved as
  `07a-d5-cr3b-hang.log` (SHA-256 `6d63f301...`), not overwritten.
- Stop point: inside `EnsureOwnerBuilt()` before owner start and cgroup
  creation. Source review identified the sequential
  `build.StandardOutput.ReadToEnd()` and `build.StandardError.ReadToEnd()`
  before `build.WaitForExit()` (`Wp05D4SupervisorTests.cs:539-541`), a
  redirected-pipe pattern that can block while an MSBuild server node holds an
  inherited pipe. The exact trigger remains unresolved; the hang was not
  reproduced or re-attempted by GLM.
- The original 29-failure runner attempt (`04-supported-profile.log`,
  SHA-256 `2132156d...`) is preserved separately; it is an H-01 manifestation,
  not a C2 result, and was corrected by configuration rather than suppression.
- Recovery actions: the owner fixture DLL completed during the hung attempt;
  the rerun with the existing fixture short-circuited the build path
  (`07b-d5-cr3b-retry.log`, 324 ms); an explicit timeout-wrapped rebuild of
  the same project entrypoint (`10-d4-owner-build.log`, exit 0) produced the
  byte-identical DLL `881daf61416fd09c90169c0a6ed4d43ebb4455f6f7e75b637e80c2526a447415`.
  Evidence runs `07c-d5-cr3b-evidence.log` (268 ms) and
  `07d-d5-cr3b-evidence.log` (336 ms) both passed 1/1.
- Final successful execution evidence: `d5-cr3b-evidence2.trx`
  (SHA-256 `dbe8d31a...`), 1 executed, 1 passed; retained case artifacts and
  witness verified.
- Cleanup evidence: residual MSBuild nodes (PIDs 68300, 68301) were terminated
  after `dotnet build-server shutdown`; final host leaf empty, `populated 0`,
  no test process or child cgroup (section 9).
- Outstanding reproducibility concern: this is a first-run-only harness hazard
  on fresh checkouts when the fixture is absent. Later passing runs do not
  remediate it and are not evidence of a root-cause fix. Corrections are
  outstanding: concurrent or timeout-bounded pipe reads, `/nodeReuse:false`,
  and timeout-wrapping focused `dotnet test` invocations.

## 12. N-01 - Manifest omission

Status: **OPEN DOCUMENTATION ISSUE; NON-BLOCKING.**

The producer document's section-18 manifest does not itemize the `07c`
isolated `D5_CR3b` execution (268 ms) or its TRX, although GLM confirmed both
exist on disk and are hash-recorded in `09-hashes.log`. All four successful
isolated/evidence executions passed. This is a documentation gap only; the
producer evidence was **not** modified by this preservation.

Supplementary capture of the existing raw artifacts (read-only, original
paths, no invented identity):

| Artifact | Exact existing path | SHA-256 |
|---|---|---|
| Isolated execution log | `/home/gaga/qualification/d685/logs/07c-d5-cr3b-evidence.log` | `7acd4add60809f43d061774bded5ebcbbf5a745c33eff3adb9aebc2aedeaffd9` |
| Execution TRX | `/home/gaga/qualification/d685/testresults/d5-cr3b-evidence.trx` | `dd5a47adae93b3f8d124fcd92f62cbb48ea1e355894b225f6802a40c4af0c9bc` |

The `07c` log records `timestamp_utc=2026-10-23T01:54:27Z`,
`uid=1000(gaga)`, `0::/gaga-delegated/host`, all four capability fields
`0000000000000000`, `RUN_EXIT=0`, and
`Passed ... D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease [268 ms]`,
`Total tests: 1`, `Passed: 1`.

N-01 is not independently closed merely by this capture: the original
producer document and manifest remain unchanged, and a formal producer
documentation correction (adding `07c`/`d5-cr3b-evidence.trx` with a
clarifying line) remains outstanding.

## 13. Native-distro qualification boundary

WSL2 runtime qualification is supported by the independent review; native
Linux distribution qualification is **OUTSTANDING** and was not claimed by
the producer or the reviewer. All evidence comes from Ubuntu 26.04.1 on the
managed WSL2 kernel `6.6.114.1-microsoft-standard-WSL2`. It does not establish
qualification on Ubuntu or any other distribution running natively on a VM,
physical host, container host or bare metal. WSL2 evidence must not be
substituted for the native-distro gate.

The 35 supported-profile skips remain a visible limitation of wider Linux/WP05
qualification under non-root; the full Linux M1 suite is not universally
qualified. No C2-mandatory case was skipped.

## 14. Deferred Owner integration, execution integration and G3

- WP08 production Owner integration and WP10 execution integration remain
  outstanding.
- G3 is not accepted. WP05 release acceptance is **NOT QUALIFIED**.
- C1 (ambiguous `ReleaseIntent`), C3 (durable stop deadline/recovery) and C4
  (duplicate/concurrent release semantics) remain unimplemented; their human
  contracts are approved, but implementation requires separate authorization.
- No target was executed, no package was published, no tag was created and no
  merge to `main` was performed. This preservation changes documentation only.

## 15. Recommended next work

1. **C1 - Ambiguous ReleaseIntent (R09)** as the next proposed implementation
   tranche, subject to separate explicit authorization. No C1 work is started,
   authorized or claimed by this checkpoint.
2. **Native Linux distribution qualification** as a separate outstanding lane,
   with its own environment and authorization; WSL2 results must not be
   substituted for it.
3. **Harness reliability follow-ups, separately tracked and non-blocking:**
   H-01 runner owner-path export/documentation correction; H-02
   concurrent/timeout-bounded pipe reads and timeout-wrapped focused runs;
   preserve the unresolved root-cause status until a fix is independently
   demonstrated.
4. **N-01 producer documentation correction** remains open; only the producer
   evidence owner can amend the original manifest, and no independent closure
   is claimed here.
5. Do not open a broad C2 remediation program: no C2 runtime contradiction or
   blocking finding was observed.

## 16. Preservation scope and integrity assertions

This preservation commit is based directly on
`d4e8bf88ce1661717537aaa3fb392bfe24c04466` and changes exactly three
documentation files:

- `docs/reviews/gagamba-linux-m1-wp05-d6-8-5-glm-review-preservation-checkpoint.md` (new),
- `docs/handoff.md` (status update),
- `docs/work-queue.md` (status update).

The producer evidence commit `d4e8bf88...` and the GLM review commit
`89464f5f...` are unchanged; their blobs (`f1cb6a9c...` and `f168fce6...`)
remain the originals. No native file, schema, test assertion, production
source or build configuration changed. `git diff --check` succeeds and the
preservation worktree is clean after the commit. The review and preservation
are siblings; neither is an ancestor of the other.
