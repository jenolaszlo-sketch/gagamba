# RC2B-03-INT-01-ENV-01: bounded WSL2 native qualification

Date: 2026-10-31 (Asia/Manila).
Remediation slice: GAGAMBA-RC2B-03-INT-01-ENV-01.
Baseline source: `026f2e8afe74b658cd6603efbcfc78e80277a16b`.
Final tested source: `a80b622168ba4e1a5250f06ea81f36b0995cc374`;
tree `6ff9c3b70520aa72b9cbed383240ad0acb546922`.
Branch: canonical `main`, `jenolaszlo-sketch/gagamba`.
Host: Windows/WSL2 Ubuntu 26.04.1, x86_64;
kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, systemd 259.5.
SDK/runtime: .NET 10.0.113 / 10.0.13.
Native prerequisites: unified cgroup v2, delegated memory/pids controllers,
systemd user manager, unprivileged UID/GID 1000, zero checked capabilities,
pinned Bubblewrap 0.11.1, SO_PEERPIDFD, pidfds and user namespaces.
Qualification scope: one fixed experimental integrated B3 positive execution,
followed by the eight existing core actual-process cases in disposable user units.
Explicit exclusions: 24 additional integration cases, full cleanup qualification,
race/populated-workload qualification, independent security review, owner acceptance,
native Linux-host/macOS qualification, production attestation, B4-B7, Hufu, HCS and VMs.

**Result: environment established; first genuinely integrated confined execution
reproduced; core runner 8 passed / 0 failed / 24 additional cases pending.**
Four core records retain Cleanup Unknown. A runner pass is scoped to its actual
assertions and is not complete security qualification. **F-01 remains MAJOR/OPEN.**

## A. Authority and exact source custody

The owner's ENV-01 request authorizes only uniquely named transient unprivileged
user units and their test-owned delegated descendants. It supersedes the prior
environment-only stop for that bounded scope. No shared preexisting cgroup was
adopted, no permission/ownership changes were made to existing cgroups, and no
process was migrated across delegation boundaries. No sudo, privileged fallback,
persistent daemon, host-global configuration, architecture selection or contract
amendment was performed. Initial WSL discovery accidentally used Ubuntu's default
root login for read-only identity/systemd inspection; all subsequent discovery,
probes, builds and Gagamba execution explicitly used `-u gaga`. No target or
cgroup mutation ran as root. The default login was not changed.

HEAD and origin/main initially matched the expected baseline. Tracked source was
clean; existing untracked work is inventoried in `git-before.txt` and preserved.
The only implementation-delivery changes are the internal controller fixture
and native runner. Production `src` and `native` are unchanged:

| Tree | Final tested identity |
|---|---|
| src | `2748fb78af6a2658ae15b34db5903f2e120b6755` |
| tests | `d1c8cf2f426d7994afef9818f48add8f95a01c89` |
| native | `85631d2f04512376621d3d09f51d67fd90f45335` |
| eng | `041987066ee06729cd623aa0844333e00cbce1e4` |

The subsequent evidence commit changes this report and the three current status
pointers only. No worktree, new branch, reset, history rewrite or force push was
used. Commits remain local; no push was requested or performed. Accepted B1/B2/B3,
RC-1, C1-C4 and Option A guarantees/qualifications remain in force. Historical
[implementation evidence](rc2b-03-int-01-b-impl-01.md), [owner architecture
decision](../decisions/rc2b-03-int-01-d01-owner-decision.md) and reviews are unchanged.

## B. Read-only discovery and isolated delegation

Ordinary WSL gaga commands start in `/init.scope`, with no permission to migrate
through root `cgroup.procs`. The running user manager owns
`/user.slice/user-1000.slice/user@1000.service`; its shared subtree was inspected,
not selected as a test parent. Memory and pids controllers were available.
Installed systemd manuals establish that `DelegateSubgroup=host` places ExecStart
directly inside the subgroup, avoiding manual migration, and `ExitType=cgroup`
keeps the service alive while any member remains. The harness, rather than the
controller-under-test, is the service's supervising main process. Controller
SIGKILL therefore does not trigger service teardown of independent recovery.

One initial unit request used invalid comma-separated `Delegate=memory,pids` and
was rejected before creation. The corrected `Delegate=memory pids` unit started
in the right host child, but inherited nonzero CapInh `0000000800000000` from the
user manager. That probe refused immediately. Explicitly dropping inherited and
ambient capabilities with unprivileged `setpriv` resolved this; no runner check
was weakened. All attempts are retained separately.

Unit suffix `02` passed all 12 delegation/lifetime probe checks: UID 1000;
CapInh/CapPrm/CapEff/CapAmb zero; exact `/host` membership; empty delegated parent;
memory/pids enabled; test-created child's cgroup.procs, cgroup.kill, memory.max
and pids.max writable by UID 1000. The probe installed finite limits, placed its
own forked child only within this test-owned subtree, wrote cgroup.kill, reaped
SIGKILL via waitpid, observed `populated 0`, and removed the empty child. Separate
finite controller/recovery stand-ins demonstrated harness/recovery survival of
controller SIGKILL; both were reaped. Actual Gagamba controller-death cases later
corroborated independent recovery survival and reaping.

## C. Verified test-only host launch

For final core unit `09`, the exact delegated parent was:

```text
/sys/fs/cgroup/user.slice/user-1000.slice/user@1000.service/app.slice/gagamba-int01-env01-20261031-09.service
```

The harness and its controller/recovery/owner children began in that parent's
`host` child. The owner allocated only its own sibling `int-<execution>` domain.
Systemd retained management of the unit root; the test used its delegated subtree
and never changed shared ancestor controls. All units used unique names, collection,
a 120-second lifetime cap, ten-second stop timeout and control-group teardown.

Exact final invocation from unprivileged gaga (WSL entry is `wsl.exe -d Ubuntu
-u gaga -- ...`):

```bash
systemd-run --user --unit=gagamba-int01-env01-20261031-09 \
  --collect --wait --pipe --property='Delegate=memory pids' \
  --property=DelegateSubgroup=host --property=ExitType=cgroup \
  --property=KillMode=control-group --property=RuntimeMaxSec=120 \
  --property=TimeoutStopSec=10 /usr/bin/setpriv \
  --inh-caps=-all --ambient-caps=-all --no-new-privs /usr/bin/python3 \
  /mnt/c/Users/Laszlos/source/repos/Solo/artifacts/int01-env01/launch.py \
  positive-diagnostic-core-matrix all
```

The retained `launch.py` derives and validates its unique test parent from actual
membership, enables memory/pids only there, writes the profile's exact parent,
sets the three required environment variables, executes preflight, inspects
every Passed field, and only then invokes the runner. `all` passes no `--case`,
thereby exercising the unchanged default full core matrix. Positive-only runs
pass `--case positive-one-release`. The independent harness survives controller
loss; systemd provides bounded outer recovery after the harness ends. This is
test-only orchestration, not a production cgroup manager or installed service.

## D. Source-bound artifacts and profile pins

The prior source-bound archive was recovered into private mode-0700 Linux storage
under `/home/gaga/gagamba-int01-env01-20261031`. All 181 retained managed-file
hashes and four native-file hashes matched their previous manifests: **185/185**.
Archive SHA-256: `089ecbc1354ca806169d515499ce17db9dc0f3f2fc982148dcd509eff670aae8`.
The unchanged production trees match the previous tested implementation. The
fixed controller was built from a canonical baseline Git archive plus the exact
committed fixture change, with SourceLink queries disabled for the source export.
Only its controller DLL replaced the controller assembly in copied retained
closures; owner, recovery and production dependency binaries remained unchanged.
No binary reproducibility claim is made for that exported-source build.

| Artifact / inventory | SHA-256 |
|---|---|
| Final controller DLL | `203db78890a3d12205604aab4b3577ab57164fe98b3e863523e00f7b468e648b` |
| Owner DLL | `23e67b90087127c5e4a78fdee5a0952bfcfdaa865c68e0e6d0aeffdb8ba390a1` |
| Recovery DLL | `a77cc7c7596c810f7775261242ce67db1753a7458302c162635ebbce5485bfec` |
| M1 DLL in all actor closures | `cd168a4facd8e2bec4ddb5ddf3349959474251e7048a46ed3ff07c8d50131854` |
| Execution DLL | `a44d69054437c86d6de902f1f57ad3583b6e13d486f679f8556b5969772c4b4e` |
| Fixed native fixture | `f07bd71128093a98c4dc2ead3e484c9f150990f26b192dc2bbd23c01aa37ad9a` |
| B3 coordinator | `ec3e31d3ffa8483aa31cbecef682b656d9fcd16c7a194d17bea9719071d3691e` |
| Seccomp, 672 bytes | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |
| Bubblewrap /usr/bin/bwrap | `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` |
| /usr/lib/dotnet/dotnet | `bebf5e63396735bdc14caae72d50eaf2d145f9166d9d932b514ec2f9f8b94e2d` |
| Runtime 10.0.13 inventory | `e80d1e4ef3bcfaf6fbd4b4fb08a1a8b1b01ac58875f4a5fd4f1658f7b850205c` |
| Hostfxr 10.0.13 inventory | `e7c12bb5c381652c37b39a4cc3b2dd7a0e112e35377dd677b5d2df66c0ba5a72` |

Complete five-file owner/recovery closure pins, controller file inventory, source
input hashes and private paths are in `artifact-identities.json`, per-run profiles
and `controller-diagnostic-sha256.json`. No missing hash was substituted. Runtime
inventories follow ExperimentalRuntimePin's sorted relative-name/NUL/content-hash
construction. The experimental caller-populated profile is not B6 attestation.

Final core profile SHA-256:
`8d045983c0ac91ad180f45ce6f05deb4c80b07d9a895ec427ac59a4eaefa94d3`.
Final runner byte SHA-256:
`cd944f1404c17f49995d2ca7c84ffb5a1692115695660564c2739618213e9bc6`.
`GAGAMBA_INT01_PROFILE` was
`/home/gaga/gagamba-int01-env01-20261031/positive-diagnostic-core-matrix-profile.json`;
`GAGAMBA_B2_CONTROLLER_DLL` was
`/home/gaga/gagamba-int01-env01-20261031/positive-diagnostic-core-matrix-controller/Gagamba.Execution.Linux.M1.B2Controller.dll`.
`GAGAMBA_DELEGATED_CGROUP` was the exact unit parent in section C.

## E. Preflight and baseline-to-fix observations

Standalone unit `03` executed `--preflight-only`: **12/12 checks passed**. Each
subsequent unit repeated preflight and inspected every check before execution:
unprivileged UID, four zero capability sets, declared delegation, parent existence,
memory/pids controls, exact host relationship, operator write authority, profile
file and controller file. These are prerequisite checks; actual closure/kernel
admission is separately enforced by the real implementation. Full raw JSON,
stdout/stderr and launch identity are retained per run.

| Probe | Baseline observation | Change | Verified observation |
|---|---|---|---|
| Delegated launch | Correct host; nonzero inherited capability; refused | Drop inheritable/ambient caps with setpriv | Zero capability checks pass, no elevation |
| First positive, unit 04 | Controller exits with generic refusal; runner records lost response | Add secret-free exception type/stack diagnostics in fixture | InvalidOperationException in System.Text.Json exposed |
| Diagnostic positives, units 05/06 | Serializer rejects ReadOnlySpan<byte> metadata in passive reference constructor, even before usable result output | Fixture-local write-only converter emitting reference fields and digest bytes | Unit 07 positive passes; final unit 08 positive and unit 09 full core matrix pass |

The public passive reference contract was not changed; the evidence converter
refuses reads and grants no authority. No production defect repair, authentication
bypass, assertion weakening or production source change was needed. The runner's
default eight-case selection/assertions remain; additions are explicit `--case`
selection and preservation of already observed evidence, including receipts,
held journal, process status/cgroup/FD inventories and duplicate-release results.
All failed attempts remain in the raw corpus rather than being overwritten.

## F. Genuine positive execution and proof limits

Final standalone positive unit `08` passed, followed by the full matrix's positive
under unit `09`. The latter used controller PID 626, recovery PID 636 and owner
PID 645. The harness retained actual pidfds and read owner `/proc/645/status`:
PPid was **636**, independently corroborating actual recovery parentage. The
recovery snapshot reported Subreaper true, RetainedDomains 1, no stop/loss and no
release authority. Its actual `/proc/<recovery>/fd` inventory retained the exact
domain directory, cgroup.kill/events, memory.max/swap.max, pids.max and staging
directory authorities; this is not merely a successful IPC claim.

Successful preparation traversed the source-bound, fail-closed direct
SO_PEERPIDFD/credential/challenge-confirmation session and both original retention
barriers. No authentication bypass or mock was present. This session claim is
supported by successful admission through those exact pinned code paths, not an
independent cryptographic packet capture. Secrets were not recorded.

The raw establishment frame's SHA-256 was
`711a80228c21c6bdd79a3db60020dc0f88ac834232ce57bb898696eb0e907e46`.
Offline inspection verified receipt digest, frame size, HELD version 1 and flags
`0x000f` (trusted setup/no-init/release-disabled/HELD). Coordinator kernel report:
UID/EUID/GID/EGID 0 inside its private user namespace, PID 2, NoNewPrivs 1,
Seccomp 2, Dumpable 0, CapEff/CapPrm/CapAmb 0, six nonzero private namespace
inodes, bounded mounts and exact test `int-<execution>` cgroup. Outer actors
remain unprivileged UID 1000. This is trusted fixed-coordinator evidence, not
target self-attestation. Before release the durable journal was Prepared, with
Release/Exec/Root absent, corroborating the coordinator's pre-fork HELD barrier;
the pinned coordinator forks/execs the target only after consuming RELEASE.

The release reply was Released, committed and delivered. PhysicalReleaseAttempts
was **1**. Native coordinator ExecConfirmed was observed with root raw wait status
0 / exit 0 and Succeeded outcome. Repeated release returned **Duplicate**, retained
the original decision, and a second observation still reported **1** physical
attempt. Final positive cleanup separately confirmed domain empty, reaping,
staging removal and journal finalization; archived independent recovery final
evidence includes owner exit/reaping and writer fencing. Capture remains absent,
GapOrLoss remains true, and unknown axes are not promoted to complete observation.

## G. Eight existing core cases

| Case | Runner | Cleanup in case record | Actual scope / independent facts |
|---|---|---|---|
| positive-one-release | Passed | Confirmed | HELD, actual parent, one RELEASE, exec/exit 0, Duplicate; four cleanup axes |
| stop-before-release | Passed | Confirmed | Stop accepted before release; later release not Released |
| controller-sigkill-held | Passed | Confirmed | Recovery survives original controller; owner exit/reaping, fence and final cleanup |
| recovery-sigkill-held | Passed | Unknown | Actual recovery killed; owner exits; later release not Released; no independent recovery reaping/fence proof |
| owner-sigkill-held | Passed | Unknown | Actual owner exit; recovery reaps/acquires fence; snapshot also has cleanup flags true, but runner retains Unknown |
| controller-sigkill-after-release | Passed | Confirmed | Committed execution preserved; surviving recovery independently reaps/fences/cleans |
| recovery-sigkill-after-release | Passed | Unknown | Actual recovery killed; owner exits; original release observation retained; recovery cleanup proof unavailable |
| owner-sigkill-after-release | Passed | Unknown | Owner reaped/fence acquired; captured snapshot has cleanup flags false; no successful-cleanup inference |

The eight saved HELD receipts were inspected independently offline: valid hashes,
flags 15, NNP/seccomp/dumpable/capability facts, exact parentage and no pre-release
journal Release/Exec/Root. No tests skipped. Native stop is distinct from emptiness:
some final NativeStop strings are Unknown after domain removal even when separate
cleanup flags confirm emptiness. Owner-after-release records
KillWrittenEmptyObserved but its early cleanup snapshot is incomplete. Neither
fact is relabeled. The harness's final watchdog may terminate remaining recovery
after the runner's owner-reaped/fence assertion; it does not prove later cleanup.
Recovery-death runs have no surviving independent reaper/fence witness.

After-release faults occur after synchronous release/outcome observation; the
finite target has already exited in these samples. They do not prove populated
workload death races, post-write observation interruption or every stop path.
These fixture/coverage limitations are not production defects established by
this run. They remain qualification gaps for follow-up, not relaxed assertions.

## H. Twenty-four additional cases remain pending

Exactly the runner's PENDING_CASES remain **NOT RUN / NOT QUALIFIED**:

```text
controller-before-preparation
controller-during-preparation
controller-release-race
recovery-retention-death
recovery-release-race
owner-before-retention
owner-mid-staging
owner-after-intent
owner-ambiguous-delivery
missing-domain-ack
missing-staging-ack
wrong-descriptor-role-count
partial-scm-rights
lost-retention-ack
blocked-observer-native-stop
simultaneous-controller-recovery-death
integrated-stop-before-mint
integrated-stop-after-intent-before-write
integrated-ambiguous-intent
integrated-lost-release-ack
integrated-original-deadline
integrated-owner-live-takeover-refusal
integrated-post-exit-writer-fence
actual-recovery-release-impersonation
```

F-01: evidence improved, **MAJOR/OPEN**. F-02 and Q1-F03/F04/F05 now have bounded
genuine integrated core evidence, not full finding closure. F-03/F-07 historical
dispositions remain; F-08 has confined exec evidence but this run does not claim a
complete independent target-FD inventory or all abnormal exec cases. Full native
fault coverage, independent review and owner acceptance remain required. No
absolute wall-clock peer-death/physical-write ordering guarantee is claimed.

## I. Raw evidence, checks and host preservation

Ignored raw root: `artifacts/rc2b-int01-env01/` in canonical checkout. Retained:
all launch attempts, raw preflights, all case JSON, profiles, runtime/closure pins,
source export, build logs, before-discovery, final environment/cleanup inventory,
offline inspected summary, exact probe/prepare/launch/build/collection scripts,
and `linux-raw-evidence.tar.gz` containing per-case journal/stderr/recovery files
and final controller closures. `sha256-manifest.txt` inventories the raw corpus;
its identity is returned in the final handoff. Raw evidence is retained locally,
not an off-device backup. No secret/session challenge/MAC was logged.

Final read-only process/cgroup inspection found **zero remaining experimental
processes and zero test-unit cgroups**. Systemd listed no matching loaded units.
Probe descendants were explicitly emptied/reaped/removed; final outer unit
collection removed the test-owned trees. Raw workspaces and binaries remain
intentionally retained. Outer removal does not convert four Unknown per-case
cleanup records into confirmed recovery cleanup or journal finalization.
Shared user-manager controller state remains cpu/memory/pids; no shared cgroup
ownership/permission changes, root migration, persistent unit or daemon occurred.

| Check | Result |
|---|---|
| Retained managed/native archive hashes | 185 verified, 0 mismatches |
| Delegation/lifetime probe | 12 passed after explicit capability drop; earlier failures preserved |
| Standalone and per-unit native preflight | 12/12 each, no failed prerequisite permitted to launch |
| Final internal controller Release build | 0 warnings, 0 errors |
| Final standalone positive | 1 passed, 0 failed |
| Existing core default matrix | 8 passed, 0 failed, 0 skipped; 24 additional pending |
| Offline core evidence inspection | 8 receipt digests/HELD/kernel-parent/journal checks inspected |
| Python --case CLI / default | Help verified; single and default-all selection exercised natively |
| Windows/full managed suites | Not rerun; production source unchanged; no new claim |
| Documentation preparation and whitespace | Passed before evidence commit; exact receipt retained locally |

## J. Smallest next action and stop

The previous environment blocker is resolved for this exact disposable WSL2
launcher. Preserve the bounded native evidence for independent inspection, then
separately commission completion of the 24 pending fixtures and stronger fault
lifecycle/cleanup observations (including the four Unknown records) before full
integrated security review and owner acceptance. No additional environment
privilege or architecture decision is currently demonstrated necessary.

This bounded ENV-01 attempt stops here. No F-01 closure, ready-for-acceptance,
production activation, independent Linux-host qualification or next-stage
authorization follows from eight core runner passes. B4 remains NOT STARTED;
B4-B7, Hufu, HCS and VM exploration were not begun.
