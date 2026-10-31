# INT-01-B-QF-02: complete integrated native fault qualification

Date: 2026-10-31 (Asia/Manila). Slice: GAGAMBA-RC2B-03-INT-01-B-QF-02.
Baseline source SHA: `5170fab91cf6ebd0bc3c067d8cd29216251ef18c`.
Final tested source SHA: `dc436249fd0b0a8d70b4e4319ac27587fe857c1c`.
Final tested source tree: `3b5e2d9eca42e696f48f984b6ab3831e7ed90f44`.
Branch: canonical `main`, `jenolaszlo-sketch/gagamba`.
Host: Windows 11 x64 build 26200; native sandbox runs inside WSL2 Ubuntu
26.04.1 x86_64, kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43,
systemd 259.5, cgroup v2. UID/GID 1000 (`gaga`); effective, permitted,
inheritable and ambient capabilities zero; NoNewPrivs 1.
Runtime/SDK: WSL .NET 10.0.13 / 10.0.113; Windows 10.0.12 / 10.0.401.
Native prerequisites: exact pinned bubblewrap/coordinator/fixture/seccomp closure,
pidfds, SO_PEERPIDFD, subreaper/waitpid, SCM_RIGHTS, delegated memory/pids,
protected runtime/source/staging, owner journal and independent recovery.
Scope: all eight original core cases, all 24 original additional cases,
affected native/component regressions, Windows managed regressions, and retained
failing-before reproductions for defects found during qualification.
Exclusions: independent review, owner acceptance, B4-B7, VM/HCS/Hufu integration,
production activation, native Linux-host/macOS qualification, binary reproducibility,
package compatibility, B6 attestation and a full C3 sealed-launch claim.

**Disposition: READY FOR INDEPENDENT INT-01 SECURITY REVIEW.**
Required native cases: **32 passed / 0 failed / 0 skipped; 0 outstanding**
(core 8/8; additional 24/24). This is producer evidence, not independent approval.
**F-01 remains MAJOR/OPEN** pending independent review and owner acceptance.
Historical B1/B2/B3 acceptances, RC-1, C1-C4, R1-F03 Option A and production
restrictions remain in force. No later implementation stage starts here.

## 1. Exact baseline, source binding and delivery

Verified each successor has its predecessor as sole parent:

| Commit | Tree | Scope |
|---|---|---|
| `988ef8f31b9e56ed0bb0ac46846cf06d9244a14d` | `d09fc4d61b12031349e10486cbbab8eda77c0124` | ENV-01 evidence |
| `51f615534b468fa3977afc867e5603abdf09bdae` | `9f9b6adb604a4d63a44c048f05db8d701a362379` | QF-01 Python qualification changes only |
| `5170fab91cf6ebd0bc3c067d8cd29216251ef18c` | `439e5a0f83df3c95a5ea110527b914866e303415` | QF-01 evidence/status only |
| `dc436249fd0b0a8d70b4e4319ac27587fe857c1c` | `3b5e2d9eca42e696f48f984b6ab3831e7ed90f44` | QF-02 implementation/tests, 17 files |

Initial live remote main was 988ef8f; normal push delivered the two old local
commits through 5170fab and live verification confirmed it. Remote immediately
before QF-02 delivery remained the full 5170fab identity above. The final delivery
uses an ordinary push of the implementation and its separate documentation-only
child. The evidence commit is the commit introducing this report directly after
dc436249; resolve with `git log -1 --format=fuller -- <this report path>`.
Its full identity and live remote receipt are returned with delivery and retained
in the separate local `delivery-receipt.json`; no self-referential commit SHA is
embedded in this file. No reset, rebase, amendment, forced update, linked worktree
or history rewrite was used. The unrelated untracked inventory is unchanged.

All **158** QF-01 raw files were rehashed against their unchanged manifest:
`248f50498a3e881ae8280193b04bc9faf7bc8195989e8feb30e9f22f2fa15a47`.
The ENV-01/QF-01 reports and historical case records are unchanged. Baseline
commit scopes, exact trees, ancestry, remote and preserved untracked names are
recorded in `baseline-reverification.json`.

Final subtrees: src `d7a0e0fb645681477eb33f931b44bd0622798df5`,
tests `bc5418dc90ff1f0e55ded38a18d809a34d69acab`,
eng `41100ed3543bbc376bdc54831338115027a47d2b`,
native unchanged `85631d2f04512376621d3d09f51d67fd90f45335`.
`tested-source.tar`, `implementation.patch`, `source-bindings.json` and the
retained binary closure bind execution to the implementation commit. Every
changed compiled C# file was byte-compared with the private Linux build source;
the corrected C qualification fixture was also compared. Final runner hashes
are recorded per invocation. These are source/artifact bindings, not a claim of
reproducible binaries across independent builds.

## 2. Native environment and artifact identities

Every native invocation uses a unique transient **user** service:
`Delegate=memory pids`, `DelegateSubgroup=host`, `ExitType=cgroup`,
`KillMode=control-group`, `RuntimeMaxSec=300` (shorter bounds for smaller probes),
`TimeoutStopSec=10`, and `--collect --wait --pipe`. Launch is explicitly
`wsl.exe -d Ubuntu -u gaga`, followed by
`setpriv --inh-caps=-all --ambient-caps=-all --no-new-privs`.
The launcher checks UID 1000, its own unique unit parent, exact `/host`
membership and empty parent before enabling memory/pids on that parent only.
Every executed qualification invocation passes and inspects **12/12 preflight
checks**. Protected runtime/actor/native pins remain enforced by real setup.
No root, sudo, global host configuration, shared-unit or shared-cgroup mutation
or privileged fallback was used. Collection outside a test unit is read-only;
its `/init.scope` membership is not represented as test launch membership.

Final required run parent:
`/sys/fs/cgroup/user.slice/user-1000.slice/user@1000.service/app.slice/gagamba-int01-qf02-review32.service`.
Harness/controller/recovery/owner membership is that parent's `/host`; native
target membership is the separate transaction domain. Private build/artifact root:
`/home/gaga/gagamba-int01-qf02-20261031` (not a Git worktree).
Final profile SHA-256:
`81e9ddf4f45d7548edfa09095858de9dd7735eae29f077f2e47547d6a5cc979c`.
Runner hashes: native matrix
`b8dbfca432d47bcdb7a25782e8f361ee53d38fa21c69e1ac06f009ec99408d0a`;
fault matrix `3ee29c82fdec1e7cba38c0d6a91973853e518ee4509e7c8aee1f744dc3150dd0`.

| Artifact | SHA-256 |
|---|---|
| Controller DLL | `4475eb550afea64a25187d41abc036845ecaf269cc697a300f5b4aa435dc6f69` |
| Owner DLL | `f4a90cb28ab7185503c62a380f157f619e4ef09c39491310f14cfc5b8856929f` |
| Recovery DLL | `8abeb066e89b9af783e4442495307df976de01c3c34720f13d58e76ac8047998` |
| Common M1 DLL | `9c0e9fef1de2ab82dbb0b1bef8c3eff0c5aa34512fe5949ff82c6e57317d730b` |
| dotnet host | `bebf5e63396735bdc14caae72d50eaf2d145f9166d9d932b514ec2f9f8b94e2d` |
| Runtime inventory | `e80d1e4ef3bcfaf6fbd4b4fb08a1a8b1b01ac58875f4a5fd4f1658f7b850205c` |
| Hostfxr inventory | `e7c12bb5c381652c37b39a4cc3b2dd7a0e112e35377dd677b5d2df66c0ba5a72` |
| Bubblewrap | `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` |
| Confined default fixture | `f07bd71128093a98c4dc2ead3e484c9f150990f26b192dc2bbd23c01aa37ad9a` |
| Confined coordinator | `ec3e31d3ffa8483aa31cbecef682b656d9fcd16c7a194d17bea9719071d3691e` |
| Seccomp filter (672 bytes) | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |

Common M1 hashes agree in controller, owner and recovery directories. Complete
deps/runtimeconfig and build closure identities are in `artifact-identities.json`;
the profile also retains exact immutable operation/deadline and native pins.

## 3. Original inventory and individual final results

Baseline classification is **12 Implemented and passed, 0 Implemented and
failed, 0 Blocked, 20 Not implemented**. The four QF-01 additions are exactly
`simultaneous-controller-recovery-death`, `integrated-original-deadline`,
`integrated-owner-live-takeover-refusal`, `integrated-post-exit-writer-fence`.
The original `PENDING_CASES` tuple remains the authoritative 24-name inventory;
its name is historical, not the final implementation status. All 32 are now
Implemented and passed. No requested fault is counted without reaching it.

The table is from **`review32/default/<exact-case>.json`**, the final complete
run after fixes. C/R/O are actual controller/recovery/owner PIDs in this run;
the pre-preparation case has no owner/recovery. Physical counts are explicit
observed counters; **unobserved does not mean zero**. Stage receipts and
continuation markers prove the twenty new injection points, with kernel status,
pidfds, domain inode/population/FD facts, original identity and deadlines retained.

| Exact case / baseline group | Reached stage and applied fault | C/R/O PIDs | Observed invariant/effect | Physical attempts | Cleanup | Result |
|---|---|---|---|---|---|---|
| `positive-one-release` (core) | Original HELD; release twice | 435/446/455 | One write; exec confirmed; root exit 0; repeat Duplicate | 1 | Confirmed | Pass |
| `stop-before-release` (core) | HELD; accept stop then release | 468/478/487 | Later request is not Released | unobserved | Confirmed | Pass |
| `controller-sigkill-held` (core) | HELD; actual controller SIGKILL | 498/508/517 | Recovery survives; owner exit, recovery reaping and fence | unobserved | Confirmed | Pass |
| `recovery-sigkill-held` (core) | HELD; actual recovery SIGKILL | 527/537/546 | Owner exits; later release is not Released | unobserved | Unknown | Pass |
| `owner-sigkill-held` (core) | HELD; actual owner SIGKILL | 557/567/577 | Recovery separately observes exit, reaps and fences | unobserved | Confirmed | Pass |
| `controller-sigkill-after-release` (core) | Observed release/target; controller SIGKILL | 589/599/608 | Original release decision retained; recovery completes | 1 | Confirmed | Pass |
| `recovery-sigkill-after-release` (core) | Observed release/target; recovery SIGKILL | 626/646/663 | Original release decision retained; owner exits | 1 | Unknown | Pass |
| `owner-sigkill-after-release` (core) | Observed release/target; owner SIGKILL | 696/713/731 | Original release decision retained; recovery reaps/fences | 1 | Confirmed | Pass |
| `controller-before-preparation` (QF-02 new) | controller-ready; controller SIGKILL | 758/-/- | No prepare requested, journal or new domain | unobserved | Unknown | Pass |
| `controller-during-preparation` (QF-02 new) | owner-before-allocation; controller SIGKILL | 764/773/782 | No capability published; owner exits; recovery final | unobserved | Confirmed | Pass |
| `controller-release-race` (QF-02 new) | release-after-intent; controller SIGKILL | 788/798/814 | Durable Intent; owner rejects release at final gate | 0 | Confirmed | Pass |
| `recovery-retention-death` (QF-02 new) | owner-domain-retained; recovery SIGKILL | 825/834/843 | No capability published; owner exits | unobserved | Unknown | Pass |
| `recovery-release-race` (QF-02 new) | release-after-intent; recovery SIGKILL | 849/859/868 | Durable Intent; owner rejects release at final gate | 0 | Unknown | Pass |
| `owner-before-retention` (QF-02 new) | owner-before-domain-transfer; owner SIGKILL | 879/888/897 | No capability; recovery reaps/fences; no retained domain | unobserved | Unknown | Pass |
| `owner-mid-staging` (QF-02 new) | owner-first-object-accounted; owner SIGKILL | 913/922/931 | No capability; recovery reaps/fences and cleans staging | unobserved | Confirmed | Pass |
| `owner-after-intent` (QF-02 new) | release-after-intent; owner SIGKILL | 938/948/957 | Durable Intent before death; recovery reaps/fences; no replay | 0 | Confirmed | Pass |
| `owner-ambiguous-delivery` (QF-02 new) | release-after-write; owner self-SIGKILL | 971/981/990 | One physical attempt; reply not Released; uncertainty retained | 1 | Confirmed | Pass |
| `missing-domain-ack` (QF-02 new) | recovery-before-domain-ack; omit ACK | 1002/1011/1020 | Actual omission receipt; preparation refused; no capability | unobserved | Unknown | Pass |
| `missing-staging-ack` (QF-02 new) | recovery-before-staging-ack; omit ACK | 1029/1038/1047 | Actual omission receipt; preparation refused; no capability | unobserved | Unknown | Pass |
| `wrong-descriptor-role-count` (QF-02 new) | owner-domain-transfer; malformed roles | 1056/1065/1074 | Receiver rejects 6 roles / 7 real SCM_RIGHTS FDs | unobserved | Unknown | Pass |
| `partial-scm-rights` (QF-02 new) | owner-domain-transfer; send 6 of 7 FDs | 1083/1092/1101 | Receiver rejects actual descriptor count 6/7 | unobserved | Unknown | Pass |
| `lost-retention-ack` (QF-02 new) | recovery-before-prepared-ack; omit ACK | 1110/1119/1128 | Retained authority but preparation refused; no capability | unobserved | Confirmed | Pass |
| `blocked-observer-native-stop` (QF-02 new) | owner-before-observation; controller SIGKILL | 1137/1147/1156 | Populated domain; independent stop accepted; kernel empty | 1 | Confirmed | Pass |
| `simultaneous-controller-recovery-death` (QF-01 added) | HELD; back-to-back exact pidfd SIGKILL | 1168/1178/1187 | Owner exits; kernel population checked where domain exists | unobserved | Unknown | Pass |
| `integrated-stop-before-mint` (QF-02 new) | owner-before-mint; queue stop, await native ACK | 1197/1206/1215 | Accepted native stop; preparation refuses; no capability | unobserved | Confirmed | Pass |
| `integrated-stop-after-intent-before-write` (QF-02 new) | release-after-intent; queue stop, await native ACK | 1223/1233/1242 | Durable Intent; accepted stop prevents physical write | 0 | Confirmed | Pass |
| `integrated-ambiguous-intent` (QF-02 new) | owner-intent-storage; filesystem rename conflict | 1254/1264/1273 | Retained seal; AmbiguousPersistence; repeat Duplicate | 0 | Unknown | Pass |
| `integrated-lost-release-ack` (QF-02 new) | owner-before-release-reply; close direct channel | 1283/1293/1302 | Original decision Released, one attempt; later request not Released | 1 | Confirmed | Pass |
| `integrated-original-deadline` (QF-01 added) | Original setup cutoff; later release | 1315/1325/1334 | Acknowledged journal has no Release; deadline not renewed | unobserved | Confirmed | Pass |
| `integrated-owner-live-takeover-refusal` (QF-01 added) | HELD; separate-open flock contender | 1348/1358/1367 | Kernel refuses exclusive fence; live recovery WriterFence false | unobserved | Confirmed | Pass |
| `integrated-post-exit-writer-fence` (QF-01 added) | HELD; actual owner SIGKILL | 1379/1389/1398 | Owner pidfd exit, recovery reaping, exclusive fence separately observed | unobserved | Confirmed | Pass |
| `actual-recovery-release-impersonation` (QF-02 new) | recovery-before-rendezvous; actual recovery connects | 1408/1418/1427 | Kernel peer identity rejected; original controller then releases once | 1 | Confirmed | Pass |

## 4. Authority, HELD, physical effects and race limits

Actual `/proc/<owner>/status` establishes recovery parentage. Recovery records
its verified subreaper status and retained domain/staging FDs; per-process
status/cgroup/FD inventories and retained pidfd targets are preserved. Final
records expose **ReleaseAuthority false** in recovery. Recovery owns no direct
controller bearer, preparation capability, journal writer while owner lives,
or physical release writer. Owner remains the sole release writer.

Across the required run, **21 real HELD receipts** were independently decoded:
frame SHA-256, declared length, version 1, flags 15, namespace-root UID/GID
values, NoNewPrivs 1, seccomp 2, dumpable 0 and zero recorded capabilities all
match. Eleven preparation faults intentionally never reach a usable HELD
capability. Invalid/missing retention fails before capability publication.
Malformed transfer assertions inspect the actual receiver rejection, not a
sender intention. Partial SCM_RIGHTS delivers six of seven expected descriptors;
it does not claim a forced kernel MSG_CTRUNC event.

The positive target really execs and returns root wait status 0. Its physical
attempt counter is 1, repeat is Duplicate, and repeat observation stays 1.
The release-boundary barriers observe durable Intent before controller/recovery/
owner death or stop. Accepted loss/stop at that gate prevents the later write;
owner death after Intent has a separately recorded pre-write count 0. Native
stop acknowledgement is sent before telemetry. The blocked-observer case proves
the domain is populated first, then independent owner stop is kernel accepted
and domain population becomes empty while observation remains blocked.

An accepted release winner retains its original decision after subsequent loss.
The old after-release core cases signal after target observation, so they do not
alone prove stopping of a still-running target; the blocked-observer case supplies
that live-domain evidence. Dual death uses back-to-back exact pidfd signals,
not atomic simultaneous wall-clock death. No absolute ordering between observed
process death and physical RELEASE is claimed. C1-C4 and Option A remain.

After an owner self-kill immediately following one physical write, delivery and
exec stay uncertain; no physical RELEASE replay occurs. Dropping the direct
reply after the original Released decision retains one attempt and never admits
another Released result. The filesystem conflict retains a durable seal and
returns AmbiguousPersistence with zero attempts; repeat is Duplicate.
The original-deadline case waits the original setup cutoff and polls an
acknowledged journal read afterward; its observation window creates no new
execution or release deadline.

The impersonation probe runs in the **actual recovery process** and connects to
the owner listener before controller rendezvous publication. Kernel peer PID is
RecoveryPid, differs from expected ControllerPid, and is rejected. The original
controller subsequently completes the genuine authenticated session and releases
once. This proves the OS-identity boundary, not a stolen post-handshake bearer
attack. Direct authentication/challenge/sequence regressions run separately.

Supplementary `confined-fd2` reuses that real integrated topology with separately
pinned static FD-probe bytes
`6d07769d369bcf964b0c47344e4dd862d0fe0ffe4f36381e7d589bd4fd465980`
from unchanged `tests/Fixtures/b3-exec-target-probe.c`. Kernel fcntl inventory
of **slots 0..63** produces exactly `TARGET_FDS=0,1,2` plus newline (17 bytes),
SHA-256 `00763f9daaf94900dc683f9a8c6f51ae6bd59092228827705b1a496cb69230a4`,
with EOF, no overflow/read failure, exec confirmed, native root raw wait 0,
wrapper exit 0, empty domain and one physical attempt. It sleeps two seconds
after output, also exercising root observation. This is actual confined exec/FD
evidence for F-08; it does not enumerate arbitrary slots above 63.

## 5. Defects, smallest corrections and preserved failures

| Finding | Failing-before evidence | Authoritative correction | Fixed observation |
|---|---|---|---|
| QF02-F01: C1 uncertainty lost after retained seal | `before-c1-reproduced`: real filesystem rename failure after content fsync; gen.seal remains, but public result Refused (1) | `OwnershipJournal.FailSealed` preserves whether the failed transition is ReleaseIntent and returns AmbiguousFailure when its seal remains | Required ambiguous-Intent case returns AmbiguousPersistence (5), zero writes, repeat Duplicate (3); cleanup Unknown |
| QF02-F02: root wait truncated despite remaining original budget | `confined-fd1` and `before-fd-reproduced`: exec confirmed and FD digest valid, wrapper exit 0, but root remains Unknown | `HeldReleasePhase.ObserveExecution` uses existing statusDeadline after ExecConfirmed instead of a fresh one-second cap | Same two-second target has actual RootWait/raw status 0, confined-fd2 passes; no new overall deadline |
| QF02-F04: qualification helper status-FD alias | `alias-before`: release source 6 / status source 4; dup2 to release FD4 destroys status source; child exit 23 and no frame | C test fixture duplicates status with F_DUPFD_CLOEXEC at least 6 before remapping release FD4 | `alias-after2`: child 0; actual 96-byte EXEC_CONFIRMED kind 4 sequence 2, exact identities/digest, actual release/reaping |

The failing-before source is an **instrumented candidate snapshot restoring only
the two prior production behaviors**, not an assertion that unmodified 5170fab
ran the new fixtures. Exact source manifest/archive, before artifact manifest/
archive, profiles, runner and failures are retained. The snapshot preceded the
later auxiliary C helper correction; its artifacts are separately identified.
Before-build attempts missing the owner project failed setup, are preserved,
and are not used as native defect proof. Only the `*-reproduced` runs above are.

The old C qualification helper SHA-256 is
`16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3`;
corrected binary `a2196f140b5e73b24c3f4cee9608ed0758f6a47054c00cc7cb594d7fef0a8dbb`,
source `09eb10081309115710bd3b1599adf2b9a79e3b863dd3bef768ea39d71f3a5093`.
This is a legacy qualification fixture, not the confined production coordinator.

QF02-F03 fixes a harness race: final32b had 31 passes/1 failure because a journal
snapshot was read during seal replacement. Polling `journal-status` now requires
an acknowledged valid snapshot within a bounded observation window; it does not
weaken the no-Release assertion or extend the operation deadline. QF02-F05 fixes
another evidence race: malformed-transfer rejection is recorded by the receiver
before it kills the owner, rather than depending on post-send owner telemetry.
The first `alias-after` assertion incorrectly expected 100 rather than the real
protocol's 96 bytes; its failure is retained and the assertion checks the exact
actual schema. No authentication check or production release assertion was relaxed.

Qualification-only hooks require an explicit trusted experimental profile,
validate whitelisted cases and a private evidence directory, remain bounded by
the original setup/overall cutoff, and are inert without that profile. Passive
proc/FD telemetry is bounded. They do not mint release authority, impersonate
an authenticated controller or add an authentication bypass. The defects above
were corrected on their authoritative paths; no architecture redesign occurred.

## 6. Cleanup, exit, reaping and journal ownership

Final required cases: **20 Cleanup Confirmed / 12 Cleanup Unknown**. Confirmation
requires independent final recovery flags for owner reaping, exclusive writer
fence, domain empty, staging removed and journal finalized, or the original
positive disposal contract. A readable owner pidfd establishes exit only.
The owner-live contender opens the real ownership.lock separately and the kernel
refuses flock while owner lives; recovery WriterFence remains false. This is a
kernel-fence contender test, not a forged recovery takeover command. Post-exit
cases separately retain pidfd exit, actual recovery reaping and fence acquisition.

| Unknown group | What is established / what is missing |
|---|---|
| recovery-sigkill-held; recovery-sigkill-after-release; recovery-retention-death; recovery-release-race; simultaneous-controller-recovery-death | Actual recovery dies and owner exits; release invariant is checked. No surviving independent recovery reaper/fence/finalization witness. Unknown is required by the evidence contract, not an inferred successful cleanup. |
| controller-before-preparation | No preparation, journal or new domain. No recovery cleanup transaction exists; no manufactured Confirmed receipt. |
| owner-before-retention; wrong-descriptor-role-count; partial-scm-rights | Recovery reaps owner and acquires fence, but retains zero domains; domain/staging/finalization flags are not established. Partially allocated resources are not claimed recovered by native evidence. |
| missing-domain-ack | Recovery retained one domain and records KillWrittenEmptyObserved and reaping/fence, but final DomainEmpty/StagingRemoved/JournalFinalized flags remain false; component stop evidence is not promoted to full cleanup. |
| missing-staging-ack | Recovery records DomainEmpty, DomainRemoved, OwnerReaped, WriterFence and JournalFinalized, but StagingRemoved is false; full cleanup remains Unknown. |
| integrated-ambiguous-intent | Retained seal and unreconciled intent; no final cleanup witness before watchdog. Controller/owner/recovery watchdog signals stay separately recorded. |

These are bounded security-invariant passes with disclosed cleanup gaps, not
universal cleanup success. Missing registered authority during failed preparation
does not qualify cleanup of those partial resources. Review must retain these
limits. Historical ENV-01 four Unknown records remain immutable. New owner-death
cases preserve the QF-01 fix awaiting independent recovery-final evidence and
confirm cleanup; recovery-death cases still cannot do so. No harness/watchdog or
systemd removal substitutes for those witnesses.

Read-only final host collection separately finds **zero remaining test cgroups,
zero experimental processes and zero loaded matching units**. Test-owned transient
units are collected. **157 case workspaces** and build artifacts remain intentionally
retained as evidence. Native stop request, kernel acceptance, kernel population,
owner exit, reaping, writer fence, staging/finalization, watchdog actions and final
host preservation are separate facts in the raw records.

## 7. Final regressions and exact execution

Counts below are separate and may overlap; they are not added into a grand total.

| Run | Passed / failed / skipped | Scope |
|---|---|---|
| review32/default, final required native matrix | **32/0/0** (8 core + 24 additional) | All originally required cases, actual processes |
| confined-fd2 | **1/0/0** | Supplementary integrated target/FD/root-wait probe |
| components4 WSL selection | **277/0/0** | Direct authentication, safety monitor, release admission/arbitration, owner protocol, journals/reconciliation, bootstrap, B2 recovery/disposal, restricted B1/B2/B3/C1-C4/Option A |
| Windows M1 delivery | **987/0/256** | Managed regression; platform/prerequisite skips remain disclosed in TRX; not Windows native qualification |
| Windows Execution | **68/0/0** | Managed regression |
| Windows Runtime | **15/0/0** | Managed regression |
| alias-before / alias-after2 | **1/0/0 each expected-outcome probe** | Before child fails as expected; corrected child emits actual frame |

Earlier components1 was **274/0/3**: the three legacy native prerequisites were
missing, not passed. Source-bound private legacy helpers were built, supplied via
GAGAMBA_REPOSITORY_ROOT and real owner DLL, and components2 passed **277/0/0**.
Components3 was **276/1/0** (D5_O03 status EOF). The deterministic FD alias above
was then reproduced/corrected and components4 passed **277/0/0**. The original
EOF's FD numbers were not captured, so that specific failure is not conclusively
attributed to aliasing. All attempts remain. final32a was 32/0 before the root
observation fix; final32b was 31/1 due journal sampling; delivery32 and review32
each pass 32/0 after corrections. These reruns are overlapping evidence.
Builds completed with zero warnings/errors. Earlier build/setup, stage-telemetry,
collection Git-ownership and assertion failures remain in the corpus. No global
Git safe.directory setting was changed to work around collection.

Exact outer form (unique test-owned service name each time):

```text
wsl.exe -d Ubuntu -u gaga -- systemd-run --user --unit=gagamba-int01-qf02-review32 --collect --wait --pipe --property="Delegate=memory pids" --property=DelegateSubgroup=host --property=ExitType=cgroup --property=KillMode=control-group --property=RuntimeMaxSec=300 --property=TimeoutStopSec=10 /usr/bin/setpriv --inh-caps=-all --ambient-caps=-all --no-new-privs /usr/bin/python3 /mnt/c/Users/Laszlos/source/repos/Solo/artifacts/int01-qf02/launch.py review32 full
```

The retained launcher performs checked preflight and then invokes:

```text
python3 eng/int01-native-matrix.py --profile <exact protected profile> --report <raw-root>/review32/default --full-matrix
```

Use `--case <exact name>` to reproduce one case; default still runs the original
eight. `eng/int01-fault-matrix.py` implements the twenty added stage injections.
`components4/command.json` preserves the full dotnet command and exact filter:
DirectAuthenticationTests, OwnerSafetyMonitorTests, ReleaseArbiterTests,
ReleaseAdmissionTests, OwnerProtocolTests, Journal, ReconcilerTests,
OwnerBootstrapTests, RecoveryB2Tests, RecoveryConnectionDisposalTests,
RestrictedControl (each FullyQualifiedName~ and OR joined).
Native primitive opt-in, exact owner DLL and private repository root are supplied
by the retained launcher. Windows commands use `dotnet test <project> -c Release
--no-restore --logger trx;LogFileName=<suite>.trx --results-directory <raw-dir>`
for the M1, Execution and Runtime test projects. Exact logs/TRX preserve skips,
test names and build output. Documentation preparation and whitespace/scope/hash
checks are performed before delivery; no further native rerun is needed for
documentation-only changes.

## 8. Existing findings and review boundary

| Existing identity | Actual implementation/evidence status; review disposition |
|---|---|
| F-01 | **MAJOR/OPEN**. Complete producer matrix now supplied; independent review and owner acceptance still required. |
| F-02 | Real recovery lifeline, owner parentage/subreaper/retained native authority and controller-death survival/reaping/fence are evidenced by actual integrated cases. No closure by source inspection. |
| F-03 | Existing correlation diagnostics preserved; native component regression includes InvalidCorrelationAfterStopOrRetainedReleasePreservesDecisionAndDiagnostic for both releaseFirst values and original-correlation B3 regression. |
| F-07 | Actual integrated recovery killed while HELD, after release, during retention and at release Intent. Owner exit/no later release observed; full recovery-death cleanup still Unknown. |
| F-08 | Actual confined exec/root wait plus separately pinned FD-probe digest for slots 0..63. Scope above is narrower than an unlimited FD inventory; no source-only closure. |
| Q1-F03 | Real recovery peer rejected by expected kernel PID gate, genuine direct controller still succeeds; direct authentication suite passes. Post-handshake stolen bearer not tested. |
| Q1-F04 | Original deadlines, native loss/stop before final write, retained release winner, ambiguous intent/delivery and no physical replay covered. No wall-clock atomic death/write claim. |
| Q1-F05 | Actual populated domain stopped with observer blocked; independent native stop accepted and kernel empty. Dedicated monitor regressions also pass. |

This report proposes evidence for independent assessment; it does not self-close
the review findings or supersede an owner decision. M-01 remains CLOSED and
R1-F02 globally OPEN. Alternative B/B1/B2/B3 and accepted qualifications remain;
no B4, B5-B7, Hufu, VM/HCS or production activity occurred.

## 9. Raw evidence package and handoff

Local ignored raw root: `C:/Users/Laszlos/source/repos/Gagamba/artifacts/rc2b-int01-b-qf02/`.
The binary/raw corpus is retained locally, **not uploaded by the Git push**.
Its manifest is also committed separately as
[rc2b-03-int-01-b-qf-02.sha256-manifest.txt](rc2b-03-int-01-b-qf-02.sha256-manifest.txt).
Manifest entries: **528 files**; SHA-256 of exact UTF-8 manifest bytes:
`b7c669c6db1a499773f8f7c27768a301dde2b41c69f937808ca58c6adfa20160`.
Paths are relative to that raw root. Every entry is verified before the evidence
commit. The manifest excludes itself and the post-commit `delivery-receipt.json`
(the latter binds the final documentation commit and live remote without a hash
cycle). Case JSON, original failed attempts, preflights, raw stdout/stderr/exit
statuses, TRX, profiles, all journal/staging/actor evidence, 157 case workspaces,
before/final source and artifact archives, helper probes, hashes and collector/
launcher scripts are retained. Historical manifests remain sealed separately.

Next action: independent INT-01 security review of the exact implementation and
this package, then an owner disposition. No acceptance or later stage is implied.
All originally required case names are implemented and executed; **unfinished
cases: none**. QF-02 delivery stops at this review boundary.
