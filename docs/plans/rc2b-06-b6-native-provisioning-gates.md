# Native provisioning and qualification gates

2026-11-04 · GAGAMBA-RC2B-06-B6-NATIVE-INPUT-FREEZE-01

**SPECIFICATION ONLY. No operation below is authorized.** Use [frozen inputs](rc2b-06-b6-native-platform-input-manifest.json), the accepted [native matrix](rc2b-06-b6-native-qualification-matrix.md) and [plan-only runbook](rc2b-06-b6-native-provisioning-runbook.md). Freeze labels never turn source inspection into execution evidence.

## Readiness

Each row uses one controlling readiness label. Verified subfacts and remaining gates are distinguished explicitly.

| Item | Status | Evidence now / stopping condition and exact missing proof |
|---|---|---|
| Image provenance | VERIFIED | Official candidate filename and expected digest authenticated by retained CD-image signature. Full bytes absent; no acquisition/import until explicitly authorized, hashed and archive inspected. |
| Planning owner decision | OWNER-DECISION-REQUIRED | Draft only; no approval act. Separate from independent review and S3 acceptance. |
| Root ancestry/ext4 | PROBE-REQUIRED | Source predicates frozen; observe guest mount filesystem and every trusted ancestor before Gagamba install. Writable `/` refuses; no chmod workaround. |
| glibc ABI | VERIFIED | Actual2.43-2ubuntu2.4 package/ELF exports at GLIBC_2.39 inspected offline. Runtime invocation/permissions remain a separate probe. |
| Bubblewrap identity | VERIFIED | Current0.11.1-1ubuntu0.3 executable matches exact compiled pin. Installed capabilities/ownership/dependencies/namespace operation still required. |
| SDK/compiler/static package transaction | UNRESOLVED | Exact candidates and signed indexes retained; full dependency/alternative transaction, payload set, compiler/linker/static hashes, audited gcc-name resolution and coordinator reproduction not established. No package-install authorization based on this partial candidate list. |
| .NET runtime identity | SOURCE-CONFIRMED |10.0.114 SDK and10.0.14 runtime pin; host/fxr member bytes verified, SDK/shared package metadata signed. Full protected inventories, resolved role config/paths and actual loaded versions absent. |
| Native loader closure/HD-5 | PROBE-REQUIRED | Recorded direct bwrap/host/fxr ELF edges and runtime package dependencies. Need recursive paths/hashes and actual production feature/maps; no invariant change without compatible reviewed config/owner decision. |
| Kernel feature probes | IMPLEMENTATION-REQUIRED | [14 contracts](../evidence/rc2b-06-b6-native-input-freeze-01/kernel-probes.json) define all eight fields. Source/supported release is not a pass. Reviewed runner/interface binding required before approved probes. |
| Delegated cgroup | PROBE-REQUIRED | Below narrow specification; actual parent/UID/GID/unit/grants/controllers/swap/limits/first-action placement/empty readback/outside isolation unproved. |
| Actual candidate identity | PROBE-REQUIRED | New authorized build and independent byte/manifest check required; set/archive digests null. Historical S2 set never reused. |
| F02 acceptance/staging | OWNER-DECISION-REQUIRED | No exact qualification record/evidence/review/owner act. If record-required Q positives depend on prior publication, require approved truthful staged protocol before publishing. |
| Installed-profile driver | IMPLEMENTATION-REQUIRED | Separate Luna workstream and independent review; no code implemented here. |
| Native S2 positive | PROBE-REQUIRED | Real fixed root, actual accepted receipt-selected record and VerifyActive Qualified; no synthetic trust. |
| S3 B2/B3, selectors/loss/FDs | PROBE-REQUIRED | Actual installed C/R/O and new driver; test exact ancestry/credentials/pins and real selection changes. |
| Delegated legacy B2 | PROBE-REQUIRED | Both actual B2Domains tests with operator parent; separate from four documentary T25-A configurations and forbidden qualified delegated v2 topology. |
| Staging/HELD/owner RELEASE | PROBE-REQUIRED | Integrated DirectSession and actual protected staging/journal/cgroup; no B2 handshake HELD claim; physical RELEASE only as individually approved case. |
| C3/loss/stopping | PROBE-REQUIRED | Original absolute durable deadline, one stop, ack/exit/reap/domain readback; no renewal or cleanup inferred from exit. |
| Dedicated4096-byte ancillary truncation | UNRESOLVED | Its own MSG_CTRUNC branch remains unforced;253-rights pressure/other parser coverage is not its proof. Reviewed legal kernel probe or separate evidenced owner coverage decision required. |
| Resources/cleanup/evidence | PROBE-REQUIRED | Measure peaks/space/log/capture bounds; preserve original holder identities, application cleanup verdict and verified export before teardown. Current historical Windows free RAM is not guest capacity proof. |

## Kernel capability/probe contract

No probe executable is claimed to exist. The machine-readable14-contract list states source, minimum documented platform condition, exact proposed syscall/interface recipe, runtime identity, positive, negative, qualified-result predicate and root-required flag. It covers all six required namespaces and common time-namespace identity, openat2, pidfd open/signal/poll/waitid, peer-pidfd/per-record credentials, rights/CLOEXEC/truncation, subreaper, seccomp and atomic delegated placement. An absent runner is an implementation prerequisite, not a fabricated command. A future test commission must bind its exact reviewed executable hash/argv and fixture inputs to each ID before execution.

Documented upstream floors are advisory necessary conditions only: [openat2 Linux5.6](https://man7.org/linux/man-pages/man2/openat2.2.html), [pidfd_open Linux5.3](https://man7.org/linux/man-pages/man2/pidfd_open.2.html), [CLONE_INTO_CGROUP Linux5.7](https://man7.org/linux/man-pages/man2/clone.2.html). [Unix socket semantics](https://man7.org/linux/man-pages/man7/unix.7.html), [subreaper](https://man7.org/linux/man-pages/man2/PR_SET_CHILD_SUBREAPER.2const.html) and [seccomp](https://man7.org/linux/man-pages/man2/seccomp.2.html) require actual correct configuration and permission. Where this commission has not established a minimum release for a specific subfeature, the JSON preserves null plus the required capability/configuration condition; it does not invent a numeric floor. Actual selected kernel string must be recorded and match the new manifest exactly.

Parent C/R/O runtime identity is dedicated uid!=0, recorded UID/GID, empty CapEff/CapPrm and trusted/common process namespace context. Controlled bwrap/user-namespace setup may require namespace-scoped setup capabilities as its accepted implementation uses; record namespace relationships and final capability drops, never grant host root/capabilities to satisfy a probe. All positive/negative fixture processes are finite and owned, externally bounded, reaped, and independently observed. Refusal must occur before target dispatch on unavailable/mismatched features, with no weaker fallback. No namespaces or child workloads were created here.

## Narrow delegated cgroup specification

Only a separately named administrator in the new distro may provision one reviewed systemd service `gagamba-b6-native.service`. Require unified cgroup2 and exact current boot/mount/device/inode identity. Literal unit, UID/GID and measured outer memory/task envelope remain approval inputs, not values to choose during a privileged run.

Resolve its actual ControlGroup as **P**, not a guessed path. Start supervisor in **P/supervisor** using verified supported `DelegateSubgroup=supervisor` (systemd254+) or an independently reviewed operator placement mechanism. `Delegate=memory pids` alone is insufficient. Pre-enable memory+pids on empty P and **P/domains**; no live process in interior nodes. The first process placement belongs to the delegater. The service and supervisor survive controller death until original R/O stopping/reaping and evidence complete.

Runtime authority is limited to creation/removal of its execution child directories under P/domains; safe retained directory handles; read controllers/subtree_control/events/procs and identity; read/write own child memory.max/memory.swap.max/pids.max; write own child cgroup.kill; and target/common-ancestor cgroup.procs write permission required by the kernel's atomic placement checks. Operator configures controllers; no extra runtime cgroup.threads or controller-reconfiguration grant, recursive chown, authority above P, or unrelated process grant is proposed. Record any broader structural permissions systemd actually supplies and prove containment; do not describe those defaults as narrower than they are. Exact modes/ACLs and identities must be reviewed, not presumed from root ownership.

Before helper spawn, enforce and read back **memory.max134217728**, **memory.swap.max0**, **pids.max48**. These are target-domain limits, not adequate managed-supervisor/whole-guest budgets. Require retained cgroup.kill write-only access and events/limits readback as CgroupInterop expects. Use actual `posix_spawnattr_setcgroup_np`/flag/clone3 path and independently prove membership from child's first action. No post-spawn migration equivalent. Negative controls: wrong/non-cgroup fd, disabled/missing controller, absent swap file, inadequate target/common-ancestor access, altered domain identity, foreign synthetic outside process unaffected. Preserve original stopping descriptors/boot/mount/domain IDs, process membership, populated=0, exit and reap evidence. Missing proof is Unknown.

## Candidate, independent acceptance and publication

| Stage | Frozen inputs now | Inputs that must remain null/pending |
|---|---|---|
| Candidate identity | Exact source/tree,14 paths, build/RID/config/native pin contract, manifest grammar | Actual reviewed file bytes/lengths/hashes, runtime inventories and kernel, canonical manifest/set/archive SHA |
| Protected installed identity | `/opt/gagamba`, root ancestry and mode/type predicates, atomic install/active-last transaction | Actual generation/receipt/active/ownership/readback and trusted positive |
| Independent acceptance | Named future independent authority requirement and owner approval boundary | Actual evidence digest, reviewer revision, owner acceptance reference and accepted scope |
| F02 publication authorization | Fixed `/etc/gagamba/accepted-qualifications/<id>.json` four keys | Exact approved ID, artifact set, raw qualification-record hash and literal accepted=true act |
| Runtime verification | Argument-free VerifyActive fixed root; receipt-selected `/opt/gagamba/qualifications/<id>.json` | Actual protected record/receipt/active and successful host/selection checks |

Approved record schema requires exactly: qualificationId, artifactSetSha256, profileId, profileVersion, rid, scope, host, sourceCommit, sourceTree, evidenceSha256, reviewCommit, acceptedAtUtc, limitations, buildConfiguration. Fixed constants: `ae4-linux-fixed-fixture-v1` /`1` /`linux-x64` /`WSL2-x86_64` /`Production`. IDs are1–128 ASCII letters/digits/dot/underscore/hyphen with no leading/trailing dot. Source/tree/review full40hex, artifact/evidence/raw-record hashes64hex, codec-approved UTC-Z date, bounded printable limitations. Syntax/binding does not establish truth or approval. No final accepted record/administrative decision is produced here.

F02 admin record has exactly qualificationId/artifactSetSha256/qualificationRecordSha256/accepted, with accepted JSON true, root:root0444/single-link/trusted ancestry. Independent approval comes before root publication; publisher cannot produce its own acceptance. Withdrawal is serialized against publication and blocks subsequent attempts only, not runtime revocation. Do not invent general revocation, search/latest fallback, generation adoption or RELEASE authority.

## Transition and stopping gates

Input-freeze Q1 by Terra High and formal owner planning disposition precede any provisioning authorization. Finish exact package/source delivery transaction and remaining input investigation before authorizing the operations that consume them. Image acquisition/import, scoped root package/config/cgroup actions, non-root native probes, candidate build/install, independent record acceptance/publication, installed-driver tests and teardown each need their own literal target/preconditions/evidence/refusal/retention boundary. The prior P01–P10 runbook is an action inventory, not permission.

Stop new admissions through the test supervisor, preserve original generations/holders and allow original stopping deadline/reaping. Record application Confirmed/Unknown/Failed from protected journal/capture and cgroup/process readback before exporting and independently verifying evidence. Retain live/HELD/uncertain generations and environment unless a distinct owner decision explicitly accepts destructive loss of unresolved obligations. Environment destruction is never application cleanup success; current remove-generation tool's conservative refusal stays intact. No automatic generation GC, controller-relaunch adoption, disk reconstruction, renewed C3 deadline or second RELEASE attempt is allowed.
