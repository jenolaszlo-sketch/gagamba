# B6 disposable WSL2 provisioning runbook

**PLAN ONLY — PRIVILEGED EXECUTION NOT AUTHORIZED**

2026-11-04 (Asia/Manila) · GAGAMBA-RC2B-06-B6-NATIVE-ENV-PREP-01

**NOT AUTHORIZED FOR EXECUTION.** Every command and operation below is a future procedure. None was executed by this commission. Independent review, then a separately scoped owner commission, must authorize the named actions. This document does not authorize root, package installation, environment creation, namespace/cgroup probes, publication, activation or teardown.

Source baseline: `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`, tree `75cbd04d009a84aee2d4da591da228a88f0a0cde`, canonical `main`. Read the [environment plan](rc2b-06-b6-native-env-prep-01.md), [test matrix](rc2b-06-b6-native-qualification-matrix.md), accepted HD-1–HD-5 and F02 decisions first. Production runtime factory remains unavailable. This is a qualification procedure for the accepted experimental scope, not production activation.

## 1. Freeze the execution inputs before approval

The later commission must replace every bracketed value below with reviewed literal values and enumerate permissions per step. A placeholder is an unresolved input and causes a stop, never a license to choose a convenient value at execution time.

| Input | Proposed scope / mandatory record |
|---|---|
| Environment identity | `Gagamba-B6-Native-01`; intended directory `D:\GagambaQualification\B6-Native-01`; operator proves no pre-existing registration/path collision or unrelated data |
| Rootfs | Ubuntu26.04 amd64, exact source URL, signed release provenance and SHA-256; inspect archive entries, ownership/modes, links and root entry before import; no existing-distro export with unrelated projects/credentials |
| Source | Exact approved commit/tree and Git archive hash; canonical main source freeze. If using later docs delivery commit, review its scope and record its new tree. No branch/worktree creation or modification of historical registrations |
| Packages | Signed repository snapshot, exact .deb names/versions/architectures/hashes and transitive dependencies; SDK10.0.114, runtime/fxr10.0.14, GCC15.2.0/static libc, Python/Git/binutils/coreutils and actual native dependency closure including ICU>=70 for the current default production path. ICU omission requires a separately reviewed HD-5-compatible configuration; current launch/build cannot select it merely through ambient environment. Exact bwrap executable must equal compiled pin. Package revision unresolved here; no unrestricted apt update/upgrade |
| Runtime identity | Dedicated non-root `gagambaqual`, literal UID/GID chosen without collision; no sudo group, root or effective/permitted capabilities |
| Root actor | Named administrator in this disposable distro only; no blanket sudo rule and no root build/test runner |
| Delegation | One exact named systemd service `gagamba-b6-native.service`, its resolved ControlGroup and boot/mount/inode identity; approved outer memory/task envelope and lifecycle; runtime parent is its `/domains` child |
| Candidate | Source/toolchain/build recipe and output manifest, artifact-set digest, archive SHA-256 and reviewer acceptance; new S3 bytes, not historical S2 digest |
| Verifier bootstrap | Exact root-side installer scripts and published InstallTool closure hashes; independently reviewed source and binary provenance; root-owned protected runtime and ancestors |
| Qualification | Exact ID, record bytes/SHA-256, artifact digest, profile/version/RID, host, source/tree, evidence digest, independent review revision/scope/limitations and truth of required gates |
| Native tests | Exact reviewer driver revision/hash, case IDs, fixture artifacts, finite workloads, timing/watchdog, root mutation targets, evidence export and authorized cleanup |

Do not loosen root predicates, pins, globalization policy, loader closure, live-holder retention or authority in response to a failed gate. Unknown package/hash feasibility remains a N1 input gate. Missing installed-profile driver remains a N5 test gate. If evidence needed by the proposed qualification record requires prior Qualified execution, resolve the staged-publication protocol with the owner before approving that record; never invent completed tests to unlock them.

## 2. Privileged / host-changing action inventory

All actions require separate explicit owner authorization in addition to the actor's technical ability. Root authority is confined to the new distro; Windows user control of WSL registration is also a host-changing action. Each target is exact after input freeze.

| ID / actor | Purpose and exact target | Preconditions and required evidence | Retry/refusal / rollback limits |
|---|---|---|---|
| P01 Windows operator | Create only `Gagamba-B6-Native-01` under its dedicated D: directory from approved image | Independent plan review and create/import permission; rootfs hash/provenance/archive inspection; no collision; capture command, registration and resulting VHD location | No import over existing name/path; no automatic unregister on failure; retain new failed environment for diagnosis |
| P02 distro administrator | Initialize only dedicated account and `/etc/wsl.conf` of new distro | Enumerated identity/config permission; record UID/GID/groups, config bytes/hash, proc capabilities and mount inventory | Compare before retry; refuse unexpected existing config/user. Restart only this named distro after application quiescence; never global `wsl --shutdown` |
| P03 distro administrator | Install only frozen package/dependency list into new image | Signed source snapshot and exact package hashes/versions; expected protected paths; verify actual root/ancestors before installing Gagamba | No global upgrade/pin substitution; partial package transaction recorded, image retained/recreated only with authorization; package rollback is not assumed safe |
| P04 distro administrator | Publish root-owned installer scripts/InstallTool under `/usr/local/libexec/gagamba-install`, wrappers under `/usr/local/sbin` | Independent tool closure review and trusted runtime; dirs0755/scripts0555/data0444 proposed; source-owned Python and wrapper paths verified; hashes/modes/caps/link counts recorded | No unreviewed root shell script; refuse aliases/untrusted ancestors/extras; stop if bootstrap mismatch. Tool removal later only when publication/install quiescent |
| P05 systemd administrator | Create/start only `gagamba-b6-native.service`; delegate memory+pids below its exact ControlGroup | Reviewed literal unit, non-root user, outer limits, long-lived supervisor/recovery lifetime, no credentials/host mounts; record generated unit and cgroup identities/permissions | No delegation at `/sys/fs/cgroup`, no broad chown, no rights above P. Refuse unavailable controllers/swap or common-ancestor authority; stop only after observed application cleanup, not on C death |
| P06 distro administrator | Install exact `candidate/` to `/opt/gagamba/installations/<digest>` and atomically select active | Candidate independently accepted; expected digest literal; source tool/runtime trusted; full protected ancestry; record before/after tree, receipt, active, staged verifier output and sync boundaries | Installer no-replace/lock governs retry; existing changed generation refuses; failure may retain generation. Do not delete old/active/live/uncertain generations or roll selector back while authority is uncertain |
| P07 independent approval administrator | Write `/etc/gagamba/accepted-qualifications/<id>.json` only | Independently approved exact record/source/artifact/evidence scope and passed prerequisite gates; exact four-field bytes; 0:0 0444 single-link, trusted ancestors | Never producer-created acceptance. Collision or changed approval refuses; serialize withdrawal with publication. Withdrawal blocks later publication only, not installed revocation |
| P08 distro administrator | `qualify-install.sh --qualification <exact-record-path>` publishes selected record under `/opt/gagamba/qualifications/<id>.json` and binds receipt | P07 exists and matches raw hashes; staged/platform/association checks succeed under lock; snapshot selectors and raw record/receipt bytes | Existing ID must be identical; differently bound receipt refuses. Publication interruption may retain unselected record; retry rechecks approval. No opportunistic reassociation |
| P09 test administrator | One individually enumerated root mutation at a time: test generation metadata/artifact, active selector, package/runtime test copy, or publication checkpoint | Approved exact case, expected original/changed hashes, separate disposable snapshot/generation, qualified positive, non-root independent observers, stop/watchdog and restore protocol | Do not mutate global current development WSL or use unspecified `sudo as needed`. Refuse unexpected holder state or source drift. Restore only after required stop/reap evidence; changing back bytes does not undo an admitted operation |
| P10 operator / administrator | Stop exact test service, terminate/unregister exact disposable distro and remove only approved eligible disposable resources | Stop new admissions; preserve holders/generations; application cleanup verdict already recorded; independently empty cgroups/reaped actors; verified exported evidence; explicit teardown permission | Unknown/live/HELD state retains environment unless owner separately accepts destructive loss of obligations. Destruction never upgrades application's cleanup verdict. No broad recursive delete, no worktree/source/evidence deletion |

Namespace creation, pidfd signaling, seccomp and atomic-placement probes are unprivileged **execution actions**, also separately scoped and bounded; read-only preparation does not authorize them.

## 3. N1 future environment procedure

After input freeze and P01 authorization, the Windows import command syntax is:

```powershell
wsl --import Gagamba-B6-Native-01 D:\GagambaQualification\B6-Native-01 <reviewed-rootfs.tar> --version 2
```

Do not execute with placeholders. Import syntax is documented by [Microsoft](https://learn.microsoft.com/en-us/windows/wsl/use-custom-distro). Observe registration and exact resulting storage; no claim that import preserves satisfactory `/` mode without inspection.

First administrator session in **that distro only** reads root filesystem ownership/mode/mounts before any Gagamba install. `/` and every trusted ancestor must be 0:0, non-group/other-writable; installation directories exact0755. Writable root rejects the image. Do not chmod `/` as remediation. Inspect actual architecture, kernel, release, package origins, ext4 root and remaining host integration mounts. Stop if outside HD-3 or image provenance differs.

Approved new-distro configuration should explicitly contain the following per-distro settings, reviewed for that image rather than merged blindly:

```ini
[boot]
systemd=true
[automount]
enabled=false
mountFsTab=false
[interop]
enabled=false
appendWindowsPath=false
[user]
default=gagambaqual
```

Do not touch existing Ubuntu's config or user-wide `.wslconfig`. Disabling integration reduces normal host exposure, with [documented WSL security limitations](https://wsl.dev/technical-documentation/security/); it is not independent machine isolation. Restart only the named new distribution when needed and authorized. The proposed account creation/package installation must be expanded into literal reviewed administrator commands once numeric IDs and package inputs are frozen. Unknown inputs currently block execution; their exact targets and intended effects are specified in P02/P03 rather than hidden in an unrestricted setup script.

## 4. N2 bounded platform and delegation qualification

Retain read-only inventory commands and outputs from plan sections3–6. Do not invoke bwrap/unshare merely to discover support during preparation. In N2, use one finite test child and explicit positive/negative cases for openat2 resolution, Unix credentials/pidfds, FD passing/CLOEXEC, subreaper/reap, namespaces, seccomp and actual atomic cgroup placement. Record kernel support, libc export availability, and user's permissions separately. Capture failures truthfully; no global userns/LSM/mount/kernel repair.

Delegation unit must run a non-root supervisor under an administrator-set envelope. `Delegate=memory pids` exposes controllers; it is not sufficient to assume child enablement. Resolve actual ControlGroup `P`, require supported `DelegateSubgroup=supervisor` (systemd254+) or a separately reviewed operator first-action placement mechanism, verify the supervisor is in `P/supervisor`, enable memory+pids on empty `P` and `P/domains`, hand `P/domains` to Gagamba. Operator sets outer resource limits and preconfigures controller enablement. Missing actual supervisor leaf placement blocks P05/N2; a normal User/Delegate unit alone is insufficient. No additional cgroup.threads/subtree_control runtime write grant is requested. Atomic placement requires cgroup.procs write permission at the target and common ancestor even though Gagamba does not directly write that file; verify these checks as the non-root spawning actor and retain all authority inside P. Runtime user needs child `cgroup.kill` write, events/procs read and exact memory/swap/pids limit write/readback; original domain descriptors retain identity. Verify the kernel's no-internal-process rule, common-ancestor migration boundary and lack of authority over a separate synthetic outside process. No recursive ownership change to unrelated cgroups.

N2 evidence includes successful limit readbacks `134217728/0/48`, process first-action membership proving atomic placement, stop/kill event and empty/reap readbacks with original identities. A failed or unsupported mandatory probe stops N3. Select actual outer memory/task limits only after measured serial peak plus watchdog headroom; no arbitrary total budget is an established minimum. Keep the service alive after C loss for R/O stopping; do not use service teardown as the test's stop path.

## 5. N3 build, bootstrap and exact installation

All following builds run as non-root under future authorization, from exact frozen committed source on Linux-native storage. No mutable `/mnt/c` source participates in trusted runtime use. The current builder requires a real canonical main `.git` directory; it refuses linked-worktree `.git` files. Record any separately authorized source transfer/import with hashes and full source identity; no new branch/worktree is needed for this preparation.

Source-backed candidate invocation:

```bash
bash eng/linux-m1/build-install-candidate.sh <canonical-gagamba-repo> <empty-output-directory>
```

The script builds Owner and Recovery with `-c Release -r linux-x64 --self-contained false -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false -p:RuntimeFrameworkVersion=10.0.14 -p:RollForward=Disable`; it checks actual managed closure and equality of shared DLLs, then statically builds the pinned coordinator and filter. Python gate enforces SDK10.0.114/GCC15.2.0/glibc>=2.43 and bwrap0.11.1. The output is `<out>/candidate` plus `<out>/artifact-set.sha256`. No digest is predeclared here.

Review **actual** S1 binary gate output and per-file bytes, manifest canonicalization and source binding independently. Verify runtime/hostfxr paths selected by the builder equal installed code's fixed paths; reject an otherwise discoverable alternate fxr. Record all package and compiler/static-library inputs; coordinator different bytes are a stop, not a rehash opportunity. The new candidate's qualification is independent of historical S2 archive/hash.

Build a separate non-packable bootstrap verifier closure from `eng/InstallTool/InstallTool.csproj`, using the same approved SDK/runtime pin and production references. Future publish recipe (not executed):

```bash
/usr/lib/dotnet/dotnet publish eng/InstallTool/InstallTool.csproj -c Release -r linux-x64 --self-contained false -p:UseAppHost=false -p:RuntimeFrameworkVersion=10.0.14 -p:RollForward=Disable -o <empty-installtool-output>
```

Independently inventory all its output, exact deps/runtimeconfig and M1/Execution production DLLs before P04. Current Python `_check_verifier_tree` requires `InstallTool.dll`, `.deps.json`, `.runtimeconfig.json`, M1 DLL and Execution DLL; it also inspects all tree entries for root ownership/nonwritability/regular single-link files. Presence of those five names is insufficient proof of correct verifier bytes. Scripts `install-candidate.sh`, `qualify-install.sh` call `/usr/bin/python3 -I /usr/local/libexec/gagamba-install/install_common.py`; `verify-install.sh` invokes `/usr/bin/env -i /usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll verify-active` and accepts **no arguments**. Root-side `_tool` uses a cleared fixed environment, cwd `/`, closes fds, 60-second timeout. Verify Python/tool/wrapper protected ancestry as well as payload.

With P06 authorization the installed wrapper's exact flags are:

```bash
/usr/local/sbin/install-candidate.sh --candidate <out>/candidate --expected-digest <reviewed-64-hex> --generation <same-reviewed-64-hex>
```

The administrator invokes that command through the individually approved root mechanism; this document provides no sudo grant. `install_common.py install` requires uid0, WSL2x64, matching expected digest/generation, safe payload and trusted verifier. It locks `.install.lock`, copies to0700 temporary generation, installs file modes and metadata, calls staged verification, syncs and no-replace publishes the generation, then atomically writes/syncs active last. Data/metadata0444/coordinator0555/dirs0755, root:root. A fresh receipt contains null qualification fields and `unverified-candidate`; it is **not Qualified**.

Use the trusted tool subcommands for prepublication evidence:

```bash
/usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll check-payload <candidate-directory>
/usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll check-staged /opt/gagamba/installations/<digest>
/usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll check-platform /opt/gagamba/installations/<digest>/manifest.json
/usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll check-association <manifest> <proposed-receipt> <proposed-record>
```

Use a clean approved environment and record runtime path/version. `check-payload` is untrusted bytes-only; `check-staged` checks protected metadata; `check-platform` checks fixed platform; `check-association` validates metadata relationships only. None creates a Qualified identity or proves independent approval/native feature coverage. `verify-install.sh` correctly exits nonzero for unbound install; preserve that negative before approval.

## 6. N4 F02 approval, publication and installed verification

Independent qualification authority reviews actual candidate/platform/prepublication evidence and approves only a truthful scope matching `InstalledMetadata` schema. The proposed record must bind exact source/tree, candidate digest, profile/version/RID, host, evidence and independent review revision/limitations. Inspect current record codec to generate bytes; there is no general `qualify` flag that manufactures evidence. Stop if required production-qualified positives cannot be truthfully established before the chosen record's claimed gates; get an explicit staged-publication owner decision rather than forging self-approval.

The installed record codec requires exactly `qualificationId`, `artifactSetSha256`, `profileId`, `profileVersion`, `rid`, `scope`, `host`, `sourceCommit`, `sourceTree`, `evidenceSha256`, `reviewCommit`, `acceptedAtUtc`, `limitations`, `buildConfiguration`. Fixed values are profile `ae4-linux-fixed-fixture-v1`, version `1`, RID `linux-x64`, scope **`WSL2-x86_64`**, buildConfiguration **`Production`**. Source/tree/reviewCommit are full40 hex; evidence/artifact hashes full64 hex. UTC time follows the codec's exact Z format; limitations is at most32 nonempty printable-ASCII strings, each<=512 characters. No `Qualified` self-assertion field is accepted. Qualification ID is1–128 ASCII letters/digits/dot/underscore/hyphen, with no leading or trailing dot. These syntax checks do not independently prove approval or tests; validate complete current codec and raw bytes before publication.

Only after independent approval, P07 provisions the fixed administrative record with exactly:

```json
{"qualificationId":"<approved-id>","artifactSetSha256":"<reviewed-digest>","qualificationRecordSha256":"<sha256-of-exact-record-bytes>","accepted":true}
```

The ID must match current grammar in `install_common.py`/`InstalledMetadata`; validate before constructing any filename. Root-protected store and file metadata, stable byte read, no duplicates/extras and literal boolean true are mandatory. This is the administrator's independently sourced decision, not the builder's output.

P08 exact future invocation:

```bash
/usr/local/sbin/qualify-install.sh --qualification <exact-reviewed-record-path>
```

Under the installation lock the tool reads the active digest and proposed record, validates fixed-store approval against both hashes and ID, then runs staged/platform/association checks. It publishes record with no-replace, syncs its directory, and selects it by receipt rename last. An existing ID must contain identical bytes; a previously differently associated receipt refuses. An interrupted transaction may leave an unselected record. Retry is permitted only with the same reviewed inputs after independent state inspection, and always rechecks approval. No record enumeration/fallback.

Independent non-root check:

```bash
/usr/local/sbin/verify-install.sh
```

Expect actual `Qualified <digest...>` output and exit0 only after protected exact-set, receipt-selected record, host checks and selector rechecks succeed. Preserve full output and all inventories. `VerifyActive` success proves its own bounded checks, not all native kernels, loader/dlopen features, staging, HELD or cleanup. F02 runtime verification never consults administrative approval; withdrawal affects later publication only. No revocation, automatic deletion or activation is introduced.

## 7. N5 exact native test boundary and observations

Run the [matrix](rc2b-06-b6-native-qualification-matrix.md) as the non-root user with empty CapEff/CapPrm. Each root mutation needs P09 case-specific approval; put adversarial changes in separate disposable generations/snapshots. A known-good installed positive must precede each mutation on the same actual platform.

Existing producer commands require flags; do not invoke bare filenames as though they were complete run recipes:

```bash
python3 eng/qualify-s3-completion.py --fixture <B2Controller.dll> --artifacts <private-results> --production-owner <actual-production-owner.dll>
python3 eng/qualify-s3-loaded.py --fixture <B2Controller.dll> --artifacts <private-results>
python3 eng/qualify-s3-production-refusal.py --recovery <actual-production-recovery.dll> --owner <actual-production-owner.dll> --artifacts <private-results>
python3 eng/qualify-b2.py --installation <legacy-test-install-with-controller-owner-recovery> --delegated-parent <operator-parent> --artifacts <private-results>
```

The first three are **bounded component/PN/refusal evidence**, not installed positive qualification. In particular the v2-refusal driver must not be expected to remain a refusal test in a genuinely qualified environment without reviewing its identity/control preconditions. The fourth uses legacy v1 semantics and `/usr/bin/dotnet`, not Qualified launch. Its `--delegated-parent` enables both `B2Domains.test_controller_loss_retains_real_domain_and_writes_kill` and `B2Domains.test_owner_exit_retained_domain_stop_and_identity_checked_removal`, in addition to non-delegated regressions. No supported single-test selector is assumed. Preserve results.json and per-fixture outputs, domain-empty/remove readbacks, stop acknowledgement separately from exit, and adopted-child reaping.

A **new independently reviewed installed-profile driver** is required to load the actual protected provider, launch exact installed R/O through QualifiedLauncher and exercise S2+loaded verification without synthetic identity injection. Integrated B3 additionally needs authenticated DirectSession, real staging/HELD, journal/capture and durable C3. No existing B6IntegratedProbe supplies that full positive. Driver development/authorization belongs to the later test commission, never a bypass in production or a public factory activation. Public descriptor-v2/complete pre-RELEASE/S4 outcomes remain explicit blockers when current code lacks the commissioned surface.

B2 trace must preserve actual 9/10→23/24→38→11→authenticated evidence37/36→12→Ready and C's final live-pin/snapshot/S2 checks. Integrated trace preserves 9/10 before Rendezvous and owner36 before StagedObjects; B2-only messages are forbidden there. No qualified delegated-parent B2. Original-owner verification and actual sender credentials remain separate proofs. Same execution/lease captured36 sent by deliberately modified synthetic O is not a production bypass; exact installed session/loaded verification must be tested under the accepted trusted-launcher premise. The dedicated evidence-buffer MSG_CTRUNC limit is reported, not promoted through other parser coverage.

Observe original PID/pidfd/ancestry, each actor's lifetime, descriptor custody/CLOEXEC/installed and queued copy closure, C loss, stop25, ack26, exit, wait/reaping, cgroup membership and populated=0, journal updates/capture, first durable C3 absolute deadline and cleanup classification. Stop preempts success and never renews C3. Ptrace/syscall-injected evidence is labeled and paired with separate untraced timing controls; no latency guarantee from a single sample. Count physical owner RELEASE attempts only where that accepted test authorizes it; recovery never receives a writer and cannot initiate a second attempt.

## 8. Retention, teardown and next decision

1. Supervisor stops new test admission. Enumerate original generation/lease/session and every holder; preserve pinned stopping/recovery authority.
2. Wait for accepted stop, original deadline, actor exits and reaping; read protected journal/capture and retained cgroup identities/events/procs. Missing C or exit0 does not prove cleanup.
3. Record application's Cleanup Confirmed/Unknown/Failed **before** service/distro termination. Unknown/live/HELD holder keeps its generation and resources retained; do not remove current `active` to manufacture an empty state.
4. Export evidence/candidate/source/toolchain inventories and original reports with SHA-256; verify exported bytes outside the disposable disk. Record capture truncation, pending cases and any retained resources.
5. With explicit P10 authorization, stop only the now-empty named service, inspect no surviving unit processes/domains, and terminate only `Gagamba-B6-Native-01`. Verify registration/storage identity again before any unregister. `wsl --unregister Gagamba-B6-Native-01` is destructive and must appear as a separately authorized literal action in the subsequent commission; do not execute it based on this runbook.
6. Do not run `remove-generation.sh` as an automated GC/rollback: current `remove_generation` deliberately refuses all current holder states, then refuses even independently empty removal absent approved cleanup evidence. A future supported generation-removal contract needs its own review. Entire-environment disposal after safe closure is a distinct operator lifecycle, not a Gagamba cleanup pass.

N6 independent review reassesses exact environment/artifacts and every result/limit; N7 owner accepts, revises or rejects bounded native claims. Provisioning and formal qualification remain unexecuted. The next approval request is for a concrete frozen-input N1/N2 commission with the P01–P05 subset and bounded probes, not an unrestricted N1–N7 task.
