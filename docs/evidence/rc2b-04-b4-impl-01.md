# RC2B-04-B4-IMPL-01: protected capture and independent native stop

Date: 2026-10-31 (Asia/Manila).
Remediation slice: **GAGAMBA-RC2B-04-B4-IMPL-01**, explicitly authorized by the owner in this chat.
Baseline source SHA: `4a181dc41d865bd46c6fca68a64fc7ea3a7fb26b`.
Final source SHA: `4bb4943df69af5f95b65300d90852c04d08a1926`.
Final source tree: `6c9535013e98fb83f72d4b628a03c59b0e99a1c7`.
Branch: canonical `main`; repository `jenolaszlo-sketch/gagamba`.
Host OS/version: Windows 11 x64 build 26200; actual Linux mechanisms exercised in **WSL2 Ubuntu 26.04.1**, not a native Linux host.
Architecture: x86_64 / AMD64.
Runtime/SDK: Windows .NET 10.0.12 / SDK 10.0.401; WSL2 .NET 10.0.13 / SDK 10.0.113.
Native prerequisites: kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, systemd 259.5, cgroup v2, existing unprivileged user-unit memory/pids delegation, pidfds, SO_PEERPIDFD, SCM_RIGHTS, original subreaper/reaper/fences and exact experimental runtime/native pins.
Qualification scope: B4 capture custody, independent stop during setup/blocked observation/backpressure, applicable partial cleanup, deterministic native-pipe faults, original INT-01 matrix and affected regressions.
Explicit exclusions: B5-B7 implementation/acceptance, sealed C3 reconciliation, B6 deployment attestation, native Linux-host/macOS qualification, restricted Windows backend, VM/HCS, Hufu, production activation, packages/publication, binary reproducibility and independent B4 review.

**Disposition: B4 IMPLEMENTED; BOUNDED WSL2 QUALIFICATION PASSED; READY FOR INDEPENDENT B4 SECURITY REVIEW.**

This is producer evidence, not B4 owner acceptance. INT-01 remains accepted with its qualifications. R1-F02 remains globally OPEN, with its B4 obligations addressed only within this experimental profile. C1-C4, Option A, first-retainer behavior, immutable original deadlines, uncertainty and no-resend semantics remain in force. B5-B7, Hufu, VM/HCS and production remain gated.

## 1. Source and evidence binding

The implementation commit is the sole child of the authorized baseline. Its 24 files contain implementation, regressions and qualification runners. The evidence/status commit is documentation-only and follows it; resolve that child with `git log -1 --format=fuller -- docs/evidence/rc2b-04-b4-impl-01.md`. Delivery uses a normal push of canonical main, with no amendment, force push, reset, linked worktree or second Git checkout.

Subtrees: src `ec924874cfd7847733c88245ee1c491031bdab54`; tests `3c87e332b3cf952677001719e0c6c3d40c11f113`; eng `e94c064ba96ca9ba93c7c1bc6f85c8737a6075a4`; native `85631d2f04512376621d3d09f51d67fd90f45335` (unchanged).

Qualification ran against the working implementation before its commit. After committing, **306/306 tracked source/test/native/eng files** in the commit archive were byte-compared with the private WSL build source: zero differences. The private source/build directories are build inputs, not Git checkouts or worktrees. `tested-source.tar`, `implementation.patch`, `source-bindings.json`, `source-audit.json` and managed/native artifact hashes retain the exact binding. Common managed DLL pins agree across the original controller, owner and recovery closures.

Raw evidence is retained locally under `artifacts/rc2b-04-b4-impl-01/`, including TRX, source and binary archives, private profiles, case journals, failed iterations, preflights, reproduction scripts and cleanup observations. The committed [raw manifest](rc2b-04-b4-impl-01.sha256-manifest.txt) contains **281 files**, SHA-256 `b02f2f73660c8e8aa241fb59bed88542fb6bd3db1bd4c716db102c1d59800397`; paths are relative to that raw directory. The later delivery receipt is deliberately separate from this manifest. No output bytes are asserted to have been disclosed externally.

Historical preservation was rechecked against the unchanged QF-01 and QF-02 manifests: **686/686 files match** (158 + 528), and every original unrelated untracked inventory entry remains. Historical INT-01 reports, raw cases and owner decision were not rewritten.

## 2. Baseline-to-fix observations

| Probe | Baseline observation | Change | Fixed observation |
| --- | --- | --- | --- |
| `DisposalAcceptsDurableStopBeforeClosingMonitor` | Stop latch remains false on disposal; first invariant fails | Dispose accepts stop before closing; dispatch remains available, first cause retained, monitor remains active while durable worker settles | Native regression passes: latch set, capability consumed, StopRecord present, no physical release |
| Confined overflow with owner observation blocked | Populated domain remains populated for the two-second observation; no owner native-stop receipt (`baseline-overflow2`) | Two independent drains start before helper admission; overflow latches fault and calls native safety stop | Same controlled target is stopped while owner remains blocked and alive; bounded prefix and overflow preserved |
| Retained-directory replacement | Original positive passes; replacement directory is incorrectly removed and reported Cleaned; symlink yields Failed instead of identity refusal (1 pass / 2 failures) | Reopen the name under retained parent without following links and compare it to the retained directory identity before empty-only removal | All three controls pass: original removed; replacement/symlink and original object preserved with Mismatch |
| Blocked-observer stop witness (Q1R-01) | Historical populated=0 sample followed owner exit | Sample kernel population/inode while owner is still paused; only then allow observer continuation | Original INT-01 case and four new B4 cases observe populated=0 before owner exit |

The disposal regression's secondary assertion was corrected during development to preserve the accepted C2 vocabulary: a consumed capability can produce **Refused**, and is never required to produce Stopped. Baseline failure was the earlier missing stop latch; no C2 semantic change was made to satisfy the test.

Retained unsuccessful iterations: initial baseline launch refused an insufficiently protected newly built baseline recovery directory; private-directory protection was corrected. Native B4 first returned 10/12, then 12/13: a protected-descriptor observation from another process was unavailable, a journal read was transient, and the first status-pipe witness assumed all nominal byte capacity was usable. These were qualification defects, not passes. Final evidence uses owner-local ioctl sampling, bounded journal-read retry and the actual atomic-frame/page occupancy. Initial analyzer/build failures and the earlier logs remain in raw evidence.

## 3. Resulting ownership and security invariants

`ProtectedCapture` is private owner memory: fixed per-stream capacity <=65,536 bytes, total retained quota <=131,072 bytes, one 4096-byte read buffer per stream, and a synchronized shared reservation. Both dedicated native drains start before Bubblewrap admission. Helper/setup output is charged to the same budgets; a synchronized quiet-drain barrier preserves the pre-HELD constructor-marker check. Stream excess, aggregate excess, native read error or custody failure inhibits release and invokes independent native safety stop without waiting for observers, status/root observation, journal settlement or the other drain. Draining may discard bounded chunks after overflow; it cannot grow storage.

Stop remains available before preparation, during setup, after Intent and with an observer/status channel blocked. The owner's duplicate domain lease and recovery's retained lease remain independent. Dispose now accepts/dispatches stop before closing, while retaining the first cause. Recovery no longer blocks its safety loop in a one-second `WaitForOwner`; it retries nonblocking reaping. Stop acceptance, durable StopRecord, native kill acceptance, empty observation, reaping, fencing and finalization remain separate facts. No native receipt is substituted for C2 runtime acceptance or for cleanup completion.

Final drain bounds only shorten: original overall deadline, first stop bound (five-second stop allowance plus at most one-second drain), or one second after domain-empty observation/finite finalization. Repetition cannot extend them. Expiry closes the readers and records incomplete/expired capture; EOF is not domain emptiness. Owner loss does not reconstruct output. Known exec/root facts survive capture failure; incomplete/overflowed capture or unknown/populated domain cannot produce Succeeded.

The new `ReadCaptureAsync(stream, offset, count)` returns immutable copied bytes to the trusted host only. It validates stream, offset and the 16-KiB per-call limit, accepts no path/descriptor and permits only **one pending read**. A cancelled wait does not cancel its underlying ownership or free the slot prematurely. A blocked read cannot queue unlimited work or block stop submission. Latest observation remains a bounded local immutable snapshot with explicit gap/loss; no reliable event delivery is invented.

Capture messages are restricted to the authenticated original controller channel, forbidden to recovery, and carry no descriptors or release authority. OwnerDirectConnection authentication/association rules are unchanged; direct authentication and role-refusal regressions pass. Target-written fake status is retained as data while the trusted root exit remains 7. No capture bytes determine trusted exec/root state. Public factories still refuse production restricted execution.

After proven owner exit/reaping and writer fencing, partial cleanup may remove an exact surviving retained domain or an exact **empty** retained staging directory. It never adopts a journal-only name, recursively deletes unknown contents, invents accounting or promotes journal finalization. Identity mismatch preserves the replacement. No reconciler/clock/seal implementation or new recovery actor was added.

## 4. Native environment and stop witnesses

Every native matrix invocation passed **12/12 preflight checks**. Test launch is `wsl.exe -d Ubuntu -u gaga`, UID/GID 1000, CapInh/CapPrm/CapEff/CapAmb zero, NoNewPrivs 1. Each unique transient **user** service uses `Delegate=memory pids`, `DelegateSubgroup=host`, `ExitType=cgroup`, `KillMode=control-group`, `RuntimeMaxSec=300`, `TimeoutStopSec=10`, `--collect --wait --pipe`. Only that service's empty delegated parent is enabled for memory/pids; actors run in its `/host`, targets in its distinct transaction domain. No root/sudo, global delegation, ACL widening or privileged fallback was used.

Boot: `b87db9d9-f88e-4bb2-8b65-d85879947625`. Private build root: `/home/gaga/gagamba-b4-20261031`. Authoritative units: `gagamba-b4-native-final.service` and `gagamba-b4-int01-final.service` under `/sys/fs/cgroup/user.slice/user-1000.slice/user@1000.service/app.slice/`. Exact profiles, runtime/native pins, controller/owner/recovery DLL hashes and runner hashes are in their `identity.json`, `profile.json` and `artifact-identities.json`. Native source/filter pins are unchanged.

| Fault / witness | Observed fault/barrier-to-empty upper bound | Owner at kernel empty sample |
| --- | --- | --- |
| `controller-loss-with-blocked-observer` | 8.738 ms | Alive; populated=0 |
| `full-status-buffer-stop` | 11.143 ms | Alive; populated=0 |
| `overflow-with-blocked-observer` | 180.266 ms | Alive; populated=0 |
| `recovery-loss-with-blocked-observer` | 6.721 ms | Alive; populated=0 |

All four have a successful owner native-kill receipt and independent kernel populated=0 observation before observer continuation. The overflow measurement includes the finite target's pre-output pause; these are run observations, not universal latency guarantees. Full-status case: 64,512 pending bytes, nominal capacity 65,536, page size 4096, atomic frame size 96; the pipe's page slots are full even though 1024 nominal bytes are unusable by those atomic writes. The owning process samples its retained status FD with FIONREAD/F_GETPIPE_SZ; no protected `/proc/<owner>/fd` access is bypassed.

Native read failure and final-drain expiry are actual Linux **pipe-component** regressions (wrong-access retained descriptor / writer kept open), not claims of a confined target manufacturing those trusted-owner faults. Injected custody failure also runs through the real confined owner and stops its native domain. Independent aggregate-only excess is exercised deterministically with a smaller test quota; in the fixed confined profile the aggregate quota equals the sum of the two stream quotas. B7 full fault qualification remains separate.

## 5. Required B4 cases and regressions

Authoritative B4 matrix: **13 passed / 0 failed / 0 skipped**.

| Exact case | Result | Gagamba cleanup before watchdog |
| --- | --- | --- |
| `capture-store-failure` | Passed | Confirmed |
| `controller-loss-with-blocked-observer` | Passed | Confirmed |
| `copied-custody-positive` | Passed | Confirmed |
| `full-status-buffer-stop` | Passed | Confirmed |
| `overflow-with-blocked-observer` | Passed | Confirmed |
| `recovery-loss-with-blocked-observer` | Passed | Unknown |
| `root-exit-descendant-writer` | Passed | Confirmed |
| `setup-constructor-marker` | Passed | Confirmed |
| `setup-output-overflow` | Passed | Confirmed |
| `simultaneous-overflow` | Passed | Confirmed |
| `stderr-overflow` | Passed | Confirmed |
| `stdout-overflow` | Passed | Confirmed |
| `target-status-is-data` | Passed | Confirmed |

| Regression check | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Windows M1 | 989 | 0 | 0 |
| Execution | 68 | 0 | 0 |
| Runtime | 15 | 0 | 0 |
| WSL components | 272 | 0 | 0 |
| Direct authentication | 21 | 0 | 0 |
| D4_11 | 1 | 0 | 0 |
| D5_O03 | 1 | 0 | 0 |
| Release identity | 1 | 0 | 0 |
| Capture + disposal | 9 | 0 | 0 |

Solution Release build: **0 warnings / 0 errors**. Windows tests are managed regressions, not restricted Windows-native qualification. WSL component run initially had three existing native-helper-dependent skips; after building the source-bound existing helpers, each was executed in its own test-host process (three 1/0/0 rows above). All 275 selected component cases therefore executed successfully across those records; the original three skips remain disclosed, not rewritten. Authentication ran in a separate test host. D5_O03 passing this run does not close the inherited Q1R-08 legacy flake.

Commands and exact filters are retained in each `command.json`. Principal commands:

```text
dotnet build Gagamba.sln -c Release --no-restore --nologo
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --no-restore
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-build --no-restore
dotnet test tests/Gagamba.Runtime.Tests/Gagamba.Runtime.Tests.csproj -c Release --no-build --no-restore
python3 eng/b4-native-matrix.py --native /home/gaga/gagamba-b4-20261031/native --report <new-report>
python3 eng/int01-native-matrix.py --full-matrix --report <new-report>
```

The Python commands consume `GAGAMBA_INT01_PROFILE`, `GAGAMBA_DELEGATED_CGROUP` and `GAGAMBA_B2_CONTROLLER_DLL` in the declared unit. They do not provision/elevate. Raw `scripts/build.sh`, `prepare.py` and `launch.py` reproduce the private artifact build, exact pin generation and bounded service invocation on this established host; build commands disable SourceLink queries for the archive-based private build. GCC uses static test binaries with `-Wall -Wextra -Werror`; test coordinator variants are never production enrollment.

## 6. Complete accepted INT-01 regression inventory

**32 passed / 0 failed / 0 skipped**, with **21 Confirmed / 11 Unknown** cleanup. Every former required case is named below; no Unknown is counted as cleanup success.

| Exact case | Result | Gagamba cleanup before watchdog |
| --- | --- | --- |
| `actual-recovery-release-impersonation` | Passed | Confirmed |
| `blocked-observer-native-stop` | Passed | Confirmed |
| `controller-before-preparation` | Passed | Unknown |
| `controller-during-preparation` | Passed | Confirmed |
| `controller-release-race` | Passed | Confirmed |
| `controller-sigkill-after-release` | Passed | Confirmed |
| `controller-sigkill-held` | Passed | Confirmed |
| `integrated-ambiguous-intent` | Passed | Unknown |
| `integrated-lost-release-ack` | Passed | Confirmed |
| `integrated-original-deadline` | Passed | Confirmed |
| `integrated-owner-live-takeover-refusal` | Passed | Confirmed |
| `integrated-post-exit-writer-fence` | Passed | Confirmed |
| `integrated-stop-after-intent-before-write` | Passed | Confirmed |
| `integrated-stop-before-mint` | Passed | Confirmed |
| `lost-retention-ack` | Passed | Confirmed |
| `missing-domain-ack` | Passed | Unknown |
| `missing-staging-ack` | Passed | Confirmed |
| `owner-after-intent` | Passed | Confirmed |
| `owner-ambiguous-delivery` | Passed | Confirmed |
| `owner-before-retention` | Passed | Unknown |
| `owner-mid-staging` | Passed | Confirmed |
| `owner-sigkill-after-release` | Passed | Confirmed |
| `owner-sigkill-held` | Passed | Confirmed |
| `partial-scm-rights` | Passed | Unknown |
| `positive-one-release` | Passed | Confirmed |
| `recovery-release-race` | Passed | Unknown |
| `recovery-retention-death` | Passed | Unknown |
| `recovery-sigkill-after-release` | Passed | Unknown |
| `recovery-sigkill-held` | Passed | Unknown |
| `simultaneous-controller-recovery-death` | Passed | Unknown |
| `stop-before-release` | Passed | Confirmed |
| `wrong-descriptor-role-count` | Passed | Unknown |

## 7. Investigation of all twelve historical Cleanup Unknown obligations

| Historical case(s) | Investigation / applicable B4 action | New final classification |
| --- | --- | --- |
| `recovery-sigkill-held`, `recovery-sigkill-after-release`, `recovery-retention-death`, `recovery-release-race`, `simultaneous-controller-recovery-death` | Original surviving owner safety stops/refuses release; new populated/blocked recovery-loss probe corroborates native stop. No surviving recovery reaper/fence/finalization authority is created. | All five remain Unknown |
| `controller-before-preparation` | No preparation requested, journal or new domain; no cleanup transaction exists. Do not fabricate a successful recovery receipt. | Unknown |
| `owner-before-retention` | Owner dies before domain transfer; recovery has no retained child lease. Parent/name/intent cannot be adopted as child authority. | Unknown |
| `wrong-descriptor-role-count`, `partial-scm-rights` | Invalid/incomplete transfers are refused and received rights closed; no lease is salvaged from an invalid authority message. | Both Unknown |
| `missing-domain-ack` | Complete validated lease survives despite missing ACK. B4 fallback stops, independently observes empty and removes that exact live domain after reaping/fencing. AllocationIntent lacks full corroborated accounting; journal finalization is not promoted. | Unknown, with physical domain cleanup improved |
| `missing-staging-ack` | Surviving recovery authority and the actual cleanup/fencing/finalization witnesses complete this run. | Confirmed, all required final flags true |
| `integrated-ambiguous-intent` | Seal/rename conflict still prevents a trustworthy complete cleanup/finalization witness. Native stop remains independent; no record-only adoption or sealed-C3 repair. | Unknown; B5/B7 obligation retained |

This is investigation and bounded improvement, not a requirement to force all cases to Confirmed. The old 20/12 records remain unchanged. The new B4 recovery-loss case is independently Unknown as required.

## 8. Findings, cleanup and final boundaries

| Finding / obligation | Disposition |
| --- | --- |
| Q1R-01 | **Evidence improved / addressed in this profile**: population sampled before owner exit and before observer continuation, with native acceptance and identity/inode facts |
| Q1R-06 | **Fixed in bounded implementation**: disposal accepts and dispatches the first stop while native monitoring survives durable settlement; regression fails on baseline and passes on fix |
| Q1R-07 | **Fixed / bounded evidence improved**: zero-wait reaping in independent recovery loop; immediate native stop remains separate; measured populated-domain witnesses above |
| R1-F02 | **Partially addressed; globally OPEN**: queued stop/pre-mint/Intent ordering, first cause, capture faults and native stopping are qualified here; broader lifecycle/platform/deployment obligations are not closed |
| Twelve INT-01 Unknown obligations | **Investigated**: one now Confirmed, eleven truthfully Unknown; partial live-resource cleanup improved without journal promotion |
| Retained-directory replacement | **Fixed for qualified Linux filesystem path**: no-follow named identity comparison and original/directory/symlink controls |

C1-C4 journal/arbitration/reconciler semantics, original direct-peer authentication and native policy source are preserved. No independent reviewer was used in this implementation task; no independent approval is claimed. The accepted trusted-controller/same-UID-host model remains unchanged; isolation from malicious same-UID host code or a hostile administrator/kernel is not claimed. Other INT-01 qualifications/backlog, including Q1R-02/Q1R-03/Q1R-05/Q1R-08 provenance and scope limits, remain as dispositioned by the accepted owner decision.

Recovery final records establish root/descendant-domain emptiness, owner reaping, writer fencing, staging removal and journal finalization only where those flags are actually confirmed. Handles close with their owning actors; native pipe tests explicitly verify expired readers close. Final host-preservation audit found **zero remaining B4 service cgroups and zero live qualification actors**. Test-owned source/binaries/case workspaces are intentionally retained for review. Systemd/watchdog host cleanup is never promoted into Gagamba cleanup, and does not settle any of the eleven Unknown transactions.

**Final disposition: READY FOR INDEPENDENT B4 SECURITY REVIEW — IMPLEMENTED AND LOCALLY QUALIFIED IN THE DECLARED UNPRIVILEGED WSL2 PROFILE.** Native Linux-host/macOS, package/deployment qualification and independent review remain unproved. B4 is not owner-accepted by this producer report; B5-B7, Hufu, VM/HCS and production remain unauthorized.
