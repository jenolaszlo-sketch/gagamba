# WP04 Fresh Qualification Rerun — Final Evidence

Date: 2026-10-17 (Asia/Manila)

**Disposition: `WP04 ACCEPTED - HELD SANDBOX QUALIFICATION COMPLETE`.** This is qualification evidence for the held coordinator and pinned Bubblewrap adapter. Independent final acceptance supersedes the author qualification disposition; all qualification results remain unchanged. The exact review body was not included with the supplied task materials and still needs to be archived verbatim. This acceptance does not enable RELEASE, qualify the production restricted-execution profile, or complete WP05/WP10. G2 remains accepted; WP05 release and WP10 target execution remain deferred.

The earlier incomplete attempt remains preserved unchanged in [WP04 fresh qualification attempt](wp04-fresh-qualification-2026-10-17.md). Its 354 passed / 35 skipped / 1 failed result is historical only. The first-run preservation checkpoint, qualification-probe commit, and this successful qualification/status commit remain separate commits.

## Fixed revision and qualification boundary

| Item | Revision / value |
|---|---|
| Repository / branch | `Gagamba` / `qualification/ar7-feasibility` |
| Frozen production source | `bac516eeb640ce4ea415043a86579c20948793c2` |
| Starting evidence revision | `a60f9a68612bf24fad761e406fc81f10d0155a8e` |
| Incomplete-run preservation checkpoint | `2eb021a9f20fd45137ad166d1899f0fe1d35ac9b` |
| Qualification-only probe commit | `3873d2398b31763b4b1200338ea3947bcb1fc1e1` |
| Final qualification source archive | Fresh archive of `3873d2398b31763b4b1200338ea3947bcb1fc1e1`, extracted and built under WSL ext4 at `/tmp/gagamba-wp04-qualification-final-3873d23/source` |
| Production-source diff | Empty: `git diff bac516eeb640ce4ea415043a86579c20948793c2 HEAD -- src` |

The probe commit changes only six test/fixture files: `tests/Fixtures/wp04-qualification-coordinator.c`, `CoordinatorMountVerifierTests.cs`, `CoordinatorProtocolQualificationTests.cs`, `NativeCopyInTests.cs`, `NonRootQualificationTests.cs`, and `OwnerProtocolTests.cs`. It contains the non-root replacement-race correction, real-process WP04 loss/protocol probes, final-cleanup marker assertion, and R3 negative controls. No production source, seccomp policy, protocol, owner/recovery algorithm, descriptor map, or release behavior changed. The checkout was clean at the probe commit before this documentation update.

WP04 was run only through prepare-held, report acceptance, abort and recovery. No RELEASE path, target fork, fixture constructor, or WP10 target execution occurred. The test fixture image was built and bound for the no-initialization oracle; it was not released or executed.

## Environment and process identity

All test commands ran as UID/EUID/GID 1000 (`gaga`) from the operator-provided delegated host. The runner's preflight and the in-suite `NonRootDelegatedPreflightSucceeds` check verified the actual process identity and zero `CapInh`, `CapPrm`, `CapEff`, `CapBnd`, and `CapAmb`; `NoNewPrivs=1`; and membership at `0::/gaga-delegated/host`. No qualification row used root execution.

| Item | Final observation |
|---|---|
| WSL boot ID | `18785e6a-be5e-41e6-8f33-1084ede27841`, unchanged throughout the rerun |
| Qualification host | PID 631, UID/EUID/GID 1000, `/gaga-delegated/host`, all capability sets zero, `NoNewPrivs=1` |
| Operator keepalive | PID 573, `/bin/sleep infinity`, cgroup `/init.scope`; alive at final audit and left running |
| Delegated parent and host leaf | `/sys/fs/cgroup/gaga-delegated` and `/host`, both `gaga:gaga` mode 0755 |
| Delegated controllers | `memory pids` enabled; `cpu` was not enabled in `cgroup.subtree_control` |
| Global cgroup root | `root:root` mode 0555, equal to the captured preflight state |

The persistent keepalive was not moved, stopped, killed, or treated as Gagamba residue.

## Fresh full Linux M1 result

The final qualification restarted from the clean probe-commit archive, rebuilt native and managed artifacts, and ran the full Linux M1 test project as UID/GID 1000:

- Full project: **389 passed, 35 skipped, 0 failed / 424 total**.
- Fresh `NonRootQualificationTests` WP04 rows, including the actual delegated preflight, passed. No WP04-specific row was skipped.
- Fresh `OwnerDeathQualificationTests` D1–D6 plus D5 replacement: **7/7 passed** as UID/GID 1000.
- The final full-suite TRX is `/tmp/gagamba-wp04-qualification-final-3873d23/results/linux-m1-full-final/linux-m1-full-final.trx`.

### Skip classification

All 35 skips have one of three explicit environment guards:

| Family | Skips | Guard and WP04 relevance |
|---|---:|---|
| `DomainAuthorityTests` | 19 | These tests create `gagamba-r2-*` directly under the hard-coded `/sys/fs/cgroup`. UID 1000 has only the bounded `/gaga-delegated` subtree, so those global-root probes correctly skip; no ownership or ACL was widened. Their accepted G2 descriptor/domain evidence is preserved in [WP03 evidence, section 28](wp03.md) and the [G2-02 R6B rereview](../../reviews/gagamba-linux-m1-g2-02-r6b-rereview.md). Fresh `OwnerProtocolTests` and `DomainHandleTransferTests` also ran non-root (below). These global-root-only probes are not WP04 coordinator cases. |
| `IntegratedBarrierTests` | 14 | The class hard-codes global `/sys/fs/cgroup/gagamba-r4-*` creation and requires `cgroup.kill` plus `openat2`; that is outside the supplied bounded delegation. The current non-root `OwnerDeathQualificationTests` D1–D6 matrix passed 7/7, and the WP04 coordinator-loss barriers below executed against the supplied parent. Thus no required WP04 case is hidden behind this skip family. |
| `RecoveryHarness` | 2 | The mount/boot and missing-delegation checks likewise attempt a global-root cgroup probe. The delegated environment was independently validated by the non-root preflight and WP04 integration rows; these WP03 harness probes are not required WP04 cases. |

The skip reasons were `requires delegated Linux cgroup v2` (21) and `requires delegated Linux cgroup v2 with openat2` (14). No permission was manufactured to reduce the skip count.

## Owner-death D1–D6 and WP04 process-loss inventory

Every kill was a real fresh owner process termination. The owner was a direct child; tests checked the live owner/pidfd and writer exclusion at the barrier before SIGKILL, observed pidfd exit, and recovered through the surviving scope. D1–D6 occur before target launch, so no target marker is applicable at those preparation barriers; each WP04 coordinator case below has its own target-absence oracle.

| Required boundary | Concrete test / deterministic barrier | Durable state and result |
|---|---|---|
| D1 after intent | `OwnerDeathQualificationTests.D1OwnerDeathAfterDurableIntentLeavesNoHiddenAllocation`; `after-intent` | `AllocationIntent`, no domain/staging authority; real owner killed while live and holding the writer; reconciliation is `QuarantinedUnknown`, with no inferred allocation. |
| D2 after domain retention, before registration | `D2OwnerDeathAfterDomainRetentionRefusesUncorroboratedIntentThenCleansByRetainedHandle`; `after-domain-retained` | Journal remains `AllocationIntent`; exact domain lease retained. Reconciler truthfully returns `Refused` because the record has no corroborating registered identity; survivor stops/removes the exact retained domain handle. This is the expected conservative result, not a pass inferred from cleanup. |
| D3 after registration | `D3OwnerDeathAfterRegistrationPreservesExactIdentity`; `after-registered` | `AllocationRegistered` matches retained device/inode identity; real death followed by bounded, repeat-stable `CleanupConfirmed`. |
| D4 after staging accounted | `D4OwnerDeathAfterStagingAccountedKeepsExactDirectoryDebt`; `after-staging-accounted` | Confirmed staging-directory identity and unconfirmed objects remain exact; retained authority cleans and confirms the directory. |
| D5 during staging | `D5OwnerDeathMidStagingCleansAccountedObjectThroughRetainedAuthority`; `mid-staging` | Accounted tool and unconfirmed input are distinguished; recovery removes only known objects and confirms cleanup. |
| D5 replacement control | `D5MidStagingReplacementSurvivesAsUnknownDebt`; after owner death and before recovery, replace the accounted path | Reconciliation is `QuarantinedUnknown`, preserves the replacement, and returns the same status on repeat. |
| D6 after Prepared, before Mint | `D6OwnerDeathAfterPreparedBeforeMintCreatesNoCapability`; `after-prepared-before-mint` | The owner dies after durable `Prepared` and before mint; the surviving scope has no preparation capability to reconstruct. |
| Registered domain before Bubblewrap/coordinator | `NonRootWp04OwnerDeathAfterDomainRegistrationBeforeCoordinatorStartReconciles`; `after-registered` | Durable `AllocationRegistered`, retained empty domain, no staging directory, no coordinator/target. Live direct child is SIGKILLed; pidfd proves exit; repeat reconciliation and identity-checked cleanup confirm removal. |
| Coordinator startup, before HELD | `NonRootWp04RealOwnerLossDuringCoordinatorStartupBeforeHeldReconciles` | The real Bubblewrap-domain coordinator is observed during startup; journal is `Prepared`, owner is live, no target execution. Owner SIGKILL before HELD is observed by pidfd and the surviving scope reconciles. |
| HELD frame emitted, before managed acceptance | `NonRootWp04RealOwnerLossAfterCoordinatorHeldFrameBeforeManagedAcceptanceReconciles`; syscall-entry trace barrier | `strace -yy` binds the complete 96-byte HELD frame write to the owner pipe; the owner is stopped at the matching 96-byte read entry, with no managed acceptance. Journal is `Prepared`, retained recovery/staging authority is live, and the domain is populated. Real owner SIGKILL, pidfd exit and final reconciliation succeed; the independent constructor marker remains absent. |
| HELD accepted | `NonRootWp04HeldSetupReportsHeldWithoutTargetInitializationThenAbortsAndReconciles` | Real held setup reports accepted HELD; `Prepared` remains durable, target marker is absent, abort succeeds, owner exits/reaps, and reconciliation is `CleanupConfirmed` and repeat-stable. An independent trace checks marker absence after acceptance, after abort, and again after final reconciliation. |
| Owner dies while held | `NonRootWp04RealOwnerLossAfterHeldAcceptanceReconciles` | Accepted HELD with live owner, writer exclusion, retained recovery/staging authority and populated domain; real SIGKILL, pidfd-confirmed exit, retained-authority reconciliation and target absence. |
| Owner dies during abort drain | `NonRootWp04RealOwnerLossDuringAbortDrainReconciles`; injected read-entry barrier | Abort is held at its final 4096-byte drain while owner is still live. The domain is already empty (`populated=0`, no cgroup processes), journal remains `Prepared`, and target is absent. Owner is SIGKILLed before `HeldAborted`; pidfd observes exit, `WaitForOwner` reaps the direct child once, retained authority survives, and bounded repeat reconciliation completes with target absent. |

The corresponding fresh `OwnerDeathQualificationTests` cases all passed in the same full Linux M1 run; no historical result was combined with this run. The tests assert retained pidfd CLOEXEC, direct-child identity, live-owner writer exclusion, one direct-child reap, and recovery via retained cgroup/staging handles rather than PID- or pathname-derived authority. `NonRootMissingPidfdRefusesBeforeOwnerTransaction` and `NonRootUnrelatedSiblingUntouched` also passed.

## Coordinator/setup protocol qualification

The integrated coordinator/setup negative suite passed **12/12** as UID/GID 1000. Each case refused HELD/completion, asserted no target process/initialization, observed the owner stopped/reaped, retained `Prepared` state, and completed bounded final reconciliation. The separate trace-controlled run independently checked the constructor marker after final cleanup for every negative row.

| Negative case | Concrete control |
|---|---|
| Incomplete frame | Coordinator mode 1 |
| Malformed frame | Coordinator mode 2 |
| Wrong message type | Mode 3 |
| Wrong execution identity | Mode 4 |
| Wrong generation digest | Mode 5 |
| Wrong sequence | Mode 6 |
| Wrong preparation identity | Mode 7 |
| Setup process exits before HELD | `NonRootWp04CoordinatorExitBeforeHeldRefusesAndReconciles` |
| Invalid mount report | `NonRootWp04InvalidMountReportRefusesAndReconciles` |
| Host namespace claimed as coordinator namespace | `NonRootWp04HostNamespaceClaimRefusesAndReconciles` |
| Corrupt pinned seccomp filter | `NonRootWp04CorruptPinnedFilterRefusesAndReconciles` |
| Missing pinned seccomp filter | `NonRootWp04MissingFilterRefusesBeforeBubblewrapOrTargetInitialization` |

Descriptor count and role controls apply to the separate owner/bootstrap SCM_RIGHTS transfer boundary, not coordinator status frames (which carry no FDs). Fresh non-root `OwnerProtocolTests` passed **21/21** and `DomainHandleTransferTests` **10/10**, including missing/extra descriptors, descriptor-bearing controls, CLOEXEC, duplicate/wrong role and role-order rejection. The global-cgroup `DomainAuthorityTests` were the explicitly classified skip family above; accepted production-path G2-02 evidence includes the real-descriptor `51 bytes + 0x55` `MSG_TRUNC` refusal, and no WP04-specific descriptor property depends on a skipped test.

The WP04 path does not expose RELEASE. None of these negatives reached target fork or constructor execution. The accepted synthetic HELD positive control also aborted, reconciled, and retained an absent target marker. The dedicated final TRX files are under `/tmp/gagamba-wp04-qualification-final-3873d23/results/`: `wp04-preaccept-read96-20s-yyd-injected`, `wp04-abort-drain-read4096-3s-injected`, `wp04-held-abort-final-cleanup-traced`, and `wp04-protocol-setup-negative-final-marker`. Their independent trace logs are under the sibling `traces/` directory.

## Target marker and additional controls

The independent fixture marker was `gagamba-m1-fixture: ctor-marker`.

- Normal HELD→abort: marker absent after HELD acceptance, after abort, and after final cleanup/reconciliation, each checked at its own point. The final point is an independent trace-log read, not an inference from earlier observations.
- Owner-loss before acceptance and during abort: target process and trace marker absent before and after recovery.
- All 12 protocol/setup negatives: no target initialization before recovery and independent marker absent after final cleanup.
- Wrapper/coordinator output streams were drained to EOF; no target RELEASE or fork was observed.

R3 mount verifier controls passed **20/20**, including malformed `size=bad` and `size=`, duplicate mount IDs, and prohibited `shared:` / `master:` propagation metadata. No control unexpectedly accepted invalid state.

The original `NativeCopyInTests.ReplacementRaceDeterministicallyRefused` failure was classified as **B: test-harness privilege assumption**, not a product defect. The old hook attempted `File.WriteAllBytes` on a mode-0500 staged object: UID 1000 correctly could not write it, while root could. The security proposition concerns path identity replacement, which does not require mutating that object. The committed probe now pre-creates a replacement and atomically renames it over the staged name through the writable parent. It proves replacement occurred, `(device,inode)` changed, the production copier refuses with `ObjectReplaced` for the identity mismatch, and the replacement survives cleanup. The same atomic rename passed the positive control. Fresh results: negative **1/1**, positive **1/1**.

## Focused regressions and canonical vector

| Regression | Fresh result |
|---|---:|
| R1 `PidFdChildReaperTests` | 3/3 |
| R2 `SeccompFilterPinTests` | 4/4 |
| R2 seccomp policy (`native/linux-m1/test_seccomp_filter_policy.py -v`) | 7/7 |
| R3 `CoordinatorMountVerifierTests` | 20/20 |
| `OwnerProtocolTests` | 21/21 |
| `DomainHandleTransferTests` | 10/10 |
| Non-root replacement-race negative / atomic-rename positive control | 1/1 each |
| `ManifestTests.CanonicalGoldenBytes` | 1/1 |

The canonical manifest remains **873 bytes**, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.

## Native artifact binding

Artifacts were built from the clean final archive under WSL ext4. `eng/linux-m1/build-native.sh` passed; the .NET test project restored/built with zero errors (eight SourceLink warnings are from the `.git`-free archive). These exact final build artifacts were hashed:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| Bubblewrap 0.11.1 (`/usr/bin/bwrap`) | 80,424 | `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` |
| Production `exec-coordinator` | 878,792 | `4c0c25af23109f8e17287a09d6929e83a8eb8d87413dd489c0343970ca6151fa` |
| Pinned seccomp filter | 672 | `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` |
| Static fixture image (built/bound, never released or executed) | 735,432 | `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305` |
| Managed owner helper DLL | 7,168 | `b0a07154bd5bad7b8d2e1865c27cccf99f3c2b2d92eedd0038563dca4549cc30` |
| Managed Linux M1 adapter assembly | 358,400 | `6722ca0a643a444f6bca3aed13f19be3ffbf124d7462840e60bf7e44c5ab6ea9` |
| Managed execution contracts assembly | 126,464 | `afb5ca7ee62a56715c6d6883c0a9ea9f42104971201309654f0ea1b23bcf42d3` |
| Final test assembly | 349,184 | `e87900941d4d22036e38058bb407fa993ca7111c9a06843e7791d0c136d7816f` |
| `exec-coordinator.c` source | — | `d6b8265ca363dc0b466c1fc7173c99920e38060773d1cd7e33dc95d893fe38c5` |

The test-only protocol coordinator is compiled per case from committed `wp04-qualification-coordinator.c` (8,604 bytes, SHA-256 `6f24751578f504e86fcf1bc63372deae00ccad52ab6ee46be5d4050349e03d6e`) with `/usr/bin/cc 15.2.0`, `-static -O2 -std=c11 -DWP04_MODE=<mode>` (and the current host mount-namespace inode for mode 10). Its outputs are temporary and removed by the test. Each of the 12 modes was rebuilt twice after the run with the same source, compiler, flags, mode, and (for mode 10) host mount-namespace inode; byte lengths and SHA-256 values matched between the two builds. The temporary test binaries were deleted by the tests. The table below records reproducible bindings, not direct hashes captured from the ephemeral run outputs, and does not replace the production artifact hashes above.

| Test mode | Bytes | Reproducible SHA-256 |
|---:|---:|---|
| 0 | 816,960 | `ea5f60f35109fcc10243ac62f48aaf5a2189d80deda28c1e7f4f56a4b25b8f72` |
| 1 | 817,056 | `e114ed869b79baf683195695415e3283acef4116d5146535598305832ee6d519` |
| 2 | 817,056 | `3ca38c2c2e275cc673d208319b8636a325de298ca76f7c4eeca2b6ffc4dacf14` |
| 3 | 817,056 | `01c8d6cc2fd0f6b669a4045a1a70197a1b508a1c2b295b28e1dd07e7b13bdf34` |
| 4 | 817,056 | `6bb183fa62f2b3b5eba51da27f5411d7041e76a950947787a5759b1cbd1c5913` |
| 5 | 817,248 | `d3d0eb54080810923127c765f0a765af6d1f01aa5211e54946407ac00699e9a7` |
| 6 | 817,248 | `7519d743a82dbb611974d94dc097b7847b0016b04c9df6c578a68e4d69ec9a6f` |
| 7 | 817,248 | `4628de264e77fdecb82273bd88ad1d8e2b0aa55757dad6fee548aa30c08826b0` |
| 8 | 816,920 | `7d58a555737a9fbc8797a3a9c08d4efc7ab7a74402bb020ea8624bff75bf4da9` |
| 9 | 821,968 | `e00825ef7cf5f150b637f47b7795766a6dd239bd2fc9ec6061a63029a40bd9a6` |
| 10 (host mnt inode `4026532219`) | 821,968 | `19a4dd5f91213fad7fa6e924e9f87f92c9b7a5a5f891d71d3c99415eecf35b7c` |
| 11 | 821,968 | `a46fb6b494fe743db1ec8dc4a495c995f48c9047a04d134fba32645b98379261` |

## Cleanup and host boundary

The final task-scoped audit confirmed: WSL boot unchanged; qualification host PID 631 alive in its delegated host leaf with zero capabilities; keepalive PID 573 alive outside the delegated subtree; delegated ownership/modes/controllers unchanged; global cgroup root unchanged; no child cgroups below the delegation other than the persistent `host` leaf; no delegated zombies, wrapper, coordinator, Bubblewrap, or testhost processes; and no new staging/test temporary directories created by the final run. Idle MSBuild/Roslyn servers from the build were gracefully shut down before the final audit.

Six `/tmp/gagamba-r5-*` directories were already present and recorded by the audit before the final archive run; the final audit found the same six paths and no new ones. They belong to prior R5 test runs and were left untouched. The final qualification workspace `/tmp/gagamba-wp04-qualification-final-3873d23` holds the source archive, build, TRX, trace and evidence artifacts and is not a staging directory.

## Final status

**`WP04 ACCEPTED - HELD SANDBOX QUALIFICATION COMPLETE`**. The exact independent review text was not included with the supplied task materials and remains to be archived verbatim. G2 remains accepted. WP05 maintenance/release work and WP10 target execution remain deferred; no target code was released or executed. Production source remains identical to `bac516eeb640ce4ea415043a86579c20948793c2`.
