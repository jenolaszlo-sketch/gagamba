# WP04 fresh qualification attempt — incomplete

Date: 2026-10-17 (Asia/Manila)

Disposition: **WP04 QUALIFICATION INCOMPLETE — NOT QUALIFIED.** This record preserves a fresh run against the fixed R3 source state. No product defect was established, and no source or test file in the repository was changed. The full Linux M1 run had one non-root test-fixture failure, and required WP04 process-loss/protocol rows remain without fresh deterministic coverage. Do not use this run as WP04 acceptance evidence.

## Fixed revision and boundary

- Repository: `Gagamba`
- Branch: `qualification/ar7-feasibility`
- Starting evidence HEAD: `a60f9a68612bf24fad761e406fc81f10d0155a8e` (clean)
- Last production source/test commit: `bac516eeb640ce4ea415043a86579c20948793c2`
- R1 implementation: `6814b0e262509a57ee1761984fd1fc819aefe5f7`
- R2 implementation: `c03227f64a2397395efbc109e28fb63fcaea9049`
- R2 evidence: `96fcda7facec75860a22c0bf0c6533115dd7e630`
- R1/R2 evidence checkpoint: `fc0852c`
- R3 implementation: `bac516eeb640ce4ea415043a86579c20948793c2`
- R3 evidence/status: `a60f9a68612bf24fad761e406fc81f10d0155a8e`

WP04 remains the held coordinator and pinned Bubblewrap adapter. The tested path prepares trusted setup, accepts HELD, and aborts/reconciles. It does not release the managed target, run fixture initialization, or perform WP10 target execution. WP05 release and WP10 remain deferred. G2 remains accepted; this record does not mark WP04 qualified or accepted.

## Environment and external delegation

All product qualification and regression test processes were launched through the existing UID-1000 delegated-host command bridge. No test row used root execution.

| Item | Observed value |
|---|---|
| Distribution | Ubuntu 26.04.1 LTS |
| WSL | WSL 2.7.3.0; WSLg 1.0.73 |
| Kernel / architecture | Linux 6.6.114.1-microsoft-standard-WSL2; x86_64 |
| glibc / .NET SDK | glibc 2.43; .NET SDK 10.0.112 |
| WSL boot ID | `18785e6a-be5e-41e6-8f33-1084ede27841` (unchanged from the R3 environment) |
| Qualification host | PID 631; UID/EUID 1000; GID 1000; `/gaga-delegated/host` |
| Host capabilities | CapInh/Prm/Eff/Bnd/Amb all zero; `NoNewPrivs=1` |
| Keepalive | PID 573, `/bin/sleep infinity`, cgroup `/init.scope`; alive at final audit |
| Delegation | `/sys/fs/cgroup/gaga-delegated` and `/host`, owner `gaga:gaga`, mode 0755 |
| Controllers | `memory pids` enabled in `cgroup.subtree_control`; `cpu` was not enabled there |
| Global cgroup root | `root:root`, mode 0555 before and after; no widening |

The fresh `NonRootDelegatedPreflightSucceeds` test passed 1/1 and verified the real test process in the delegated host leaf with zero permitted/effective/ambient capabilities. The actual held setup also completed from this context, proving the configured `clone3(CLONE_INTO_CGROUP)` path worked for the exercised scenario.

## Native artifact binding

The fixed-revision archive was built under WSL ext4 in a temporary directory. `eng/linux-m1/build-native.sh` passed and reports that it built the fixture/coordinator without executing either. The .NET project restored and built with 0 errors; eight SourceLink warnings occurred because the pristine Git archive has no `.git` directory. These hashes bind the fresh native build:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `/usr/bin/bwrap` (Bubblewrap 0.11.1) | 80,424 | `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` |
| `exec-coordinator` | 878,792 | `4c0c25af23109f8e17287a09d6929e83a8eb8d87413dd489c0343970ca6151fa` |
| generated seccomp filter | 672 | `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` |
| static fixture image (not released or executed) | 735,432 | `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305` |
| managed owner helper DLL | 7,168 | `f9b0a0d8e974624f51ed2a0eeb462db3e60e305e009e01e0ba0c2f1310cd6311` |
| `exec-coordinator.c` source | — | `d6b8265ca363dc0b466c1fc7173c99920e38060773d1cd7e33dc95d893fe38c5` |

The initial, unmodified test assembly was 310,272 bytes with SHA-256 `4805dff5af225adc63b8bf6eefa49997a9635ce66e8ffbf754fcda46bb44f88d`. Temporary qualification-only report/mount/owner-death probes were later added to the ext4 archive only; the probe-bearing assembly hash was `4847143a746732de6286558fc1cdd40be4fa502897286842ee27c231a6ef4962`. Neither assembly nor temporary probe source was copied into the repository checkout.

The pinned filter length/hash matched the R2 binding. The canonical vector test passed; the expected vector remains 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.

## Fresh results

| Suite or case | Result | Notes |
|---|---:|---|
| `NonRootDelegatedPreflightSucceeds` | 1/1 | Fresh UID-1000 delegated preflight |
| `NonRootWp04HeldSetupReportsHeldWithoutTargetInitializationThenAbortsAndReconciles` | 1/1 | Real prepare→HELD→abort→reconcile path; target marker absent at HELD and after abort; stable `CleanupConfirmed` reconciliation and staging/cgroup removal |
| `NonRootWp04MissingFilterRefusesBeforeBubblewrapOrTargetInitialization` | 1/1 | Setup refusal; no child cgroup or staging allocation |
| `NonRootQualificationTests` | 14/14 | Includes fresh D1–D6 owner-preparation cases and the D5 replacement control; D6 executed |
| `OwnerDeathQualificationTests` | 7/7 | Real owner process, pidfd liveness/exit, takeover and D1–D6 plus D5 replacement |
| `OwnerProtocolTests` | 21/21 | SCM_RIGHTS/owner protocol regressions |
| `PidFdChildReaperTests` | 3/3 | Fresh R1 reaper regression |
| `SeccompFilterPinTests` | 4/4 | Fresh pin/hash negative controls |
| `test_seccomp_filter_policy.py -v` | 7/7 | Generator and policy controls |
| `CoordinatorMountVerifierTests` | 15/15 | Existing R3 controls |
| Temporary `Wp04FreshQualificationProbeTests` | 3/3 | Malformed size, duplicate mount ID, shared propagation marker; fail-closed |
| `ManifestTests.CanonicalGoldenBytes` | 1/1 | Canonical vector unchanged |
| Full Linux M1 test project | **354 passed, 35 skipped, 1 failed / 390 total** | The failed test is described below; skips are not passes |
| `DomainAuthorityTests` | 2 passed, 19 skipped / 21 | The skipped tests attempt global cgroup-root setup beyond the delegated subtree |
| `IntegratedBarrierTests` | 1 passed, 14 skipped / 15 | The skipped tests attempt global cgroup-root probe creation; no broadening was performed |

The 35 full-suite skips comprise 19 `DomainAuthorityTests`, 14 `IntegratedBarrierTests`, and two `RecoveryHarness` global-cgroup capability tests. Their guards require cgroup creation at `/sys/fs/cgroup`, which UID 1000 cannot do under the approved bounded delegation. Prior native-root evidence for equivalent unchanged G2 domain/owner-preparation paths is recorded in [WP03 evidence](wp03.md) (root-native domain/descriptor and integrated barrier suites). That earlier evidence does not cover WP04 coordinator-loss boundaries and is not counted as a fresh pass here.

## HELD and mount evidence

A temporary read-only test probe captured the actual accepted HELD report from one fresh run. The report identified coordinator namespace PID 2, cgroup `0::/gaga-delegated/d-489481a8e96342018d66e9138419db9b`, `uid/euid/gid/egid=0` inside the sandbox, `CapEff/CapPrm/CapAmb=0`, `NoNewPrivs=1`, and seccomp mode 2. Its namespace-inode vector was `4026532295,4026532228,4026532231,4026532232,4026532230,4026532229`; the coordinator mount namespace inode was `4026532228`, distinct from the qualification host's `mnt:[4026532219]`.

The captured HELD mountinfo showed:

| Mount | ID / parent | Device | Root → mountpoint | Options | Superblock ceiling |
|---|---|---|---|---|---|
| sandbox root | 450 / 278 | 0:88 | `/newroot` → `/` | `ro,nosuid,nodev,relatime` | — |
| `/dev/shm` | 474 / 450 | 0:90 | `/` → `/dev/shm` | `rw,nosuid,nodev,relatime` | `size=1024k` = 1,048,576 bytes |
| `/tmp` | 504 / 450 | 0:91 | `/` → `/tmp` | `rw,nosuid,nodev,relatime` | `size=7168k` = 7,340,032 bytes |

The qualification host mountinfo identified host `/dev/shm` as device 0:63 (mount 117) and host `/tmp` as device 0:72 (mount 138). The accepted HELD devices 0:90 and 0:91 are distinct from one another and from those host devices. The fresh verifier suite and the three extra fail-closed probes passed. The temporary probe did not obtain an independent `statfs` sample from the coordinator after launch; therefore only the actual coordinator mountinfo capacities and the verifier's semantic byte comparisons are reported as fresh measurements. The prior R3 `statfs` observation remains remediation history, not a substitute for this run.

## Target and lifecycle boundary

The normal fresh HELD test accepted HELD and then aborted. The adapter's constructor-marker check passed before HELD acceptance; the abort result also reported the marker absent. The path exposed no release command or release token: the WP04 owner protocol uses `HeldReady` and `AbortHeld`/`HeldAborted`; `Completed` reports preparation and does not release the target. No fixture target initialization or target execution was observed.

A separate temporary qualification-only probe used the accepted HELD return as a deterministic barrier, verified the owner alive and writer-excluding before the kill, killed the actual owner, observed pidfd exit, reaped once, then reconciled through retained authority twice. It passed 1/1 with `CleanupConfirmed` and no staging/cgroup residue. This adds fresh evidence for owner death **after HELD acceptance**. The probe's post-cleanup assertion reused the pre-HELD marker result; it was not an independent post-cleanup marker read.

The required WP04 process-loss matrix is incomplete:

| Boundary | Fresh evidence |
|---|---|
| Domain retained before Bubblewrap start | G2 owner-preparation D2 coverage passed; it does not start the WP04 coordinator |
| Bubblewrap/coordinator startup before HELD | NotRun; no current deterministic WP04 hook exercised |
| HELD reported before managed acceptance | NotRun; no current deterministic WP04 hook exercised |
| After HELD acceptance | Temporary real-owner-death probe passed 1/1 |
| During abort/reconciliation | Normal abort/reconcile passed; a separate process-loss/fault-injection row was NotRun |

Fresh malformed-size, duplicate-mount-ID and shared-propagation probes all refused as required. The pinned-filter, missing-filter, mount verifier and owner protocol suites passed. However, the fixed repository has no fresh integrated negative suite for malformed coordinator HELD framing, invalid execution/generation/sequence/type combinations, or WP04 descriptor role/order failures. Global-root `DomainAuthorityTests` skips are not presented as passes; prior root evidence is only bound for equivalent unchanged G2 descriptor/domain behavior. The target marker was not independently re-read after final cleanup. These omissions block the qualification success criteria.

## Full-suite failure and classification

`NativeCopyInTests.ReplacementRaceDeterministicallyRefused` failed under the required UID-1000 environment: expected `ObjectReplaced`, actual `InvariantFailure` (354 pass, 35 skip, 1 fail). The test hook calls `File.WriteAllBytes` on the just-staged object after `SafeCopyIn` sets its final mode to 0500. As UID 1000 without capabilities, that write cannot modify the existing file; the hook exception is caught by the outer `SafeCopyIn` invariant-failure path. This is a non-root test-fixture privilege assumption, not evidence of a WP04 product defect. The failing result was not rerun or masked, and no test/source change was made to hide it.

## Cleanup, deferred items, and final status

Final audit on the same boot found: no child cgroups below the delegated parent, no staging/test temp residue, no delegated-tree zombies, no testhost or coordinator process, the delegated host still in its host leaf, and the keepalive still alive in `/init.scope`. Three MSBuild/Roslyn servers created by the qualification build were shut down; only the persistent host remained after audit. Global cgroup-root mode stayed `root:root 0555`.

The deferred generator mismatch remains untouched: wrong audit architecture emits `SECCOMP_RET_KILL_THREAD`; the historical artifact used `SECCOMP_RET_KILL_PROCESS`. No WP05 release, WP10 target execution, or production source change occurred.

**Final status: `WP04 QUALIFICATION INCOMPLETE - NON-ROOT REGRESSION FAILURE AND REQUIRED WP04 CASES UNVERIFIED`.** No product defect was established. WP04 remains unqualified and unaccepted; G2 remains accepted; WP05 and WP10 remain deferred. The full-success status `WP04 QUALIFIED - INDEPENDENT FINAL ACCEPTANCE REVIEW REQUIRED` is not warranted by this run.