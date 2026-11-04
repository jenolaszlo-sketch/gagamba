# Luna source contract — B6 native input freeze

Scope: source/documentation analysis only for `GAGAMBA-RC2B-06-B6-NATIVE-INPUT-FREEZE-01`. Canonical source examined at expected `main` commit `af4300f5dff7e4e49fca98233911a44a15894a43`, tree `2d0ece92e20247d3d61ac84e9215837a713e9921`. This note does not assert fresh host probes, candidate build, native qualification, or approval.

## Source-derived candidate contract

The S2 artifact at `docs/evidence/rc2b-06-b6-impl-01-s2/candidate-manifest.json` is historical and predates S3; its digest `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` must not be reused. Build the candidate only from the commission-approved S3 source snapshot, expected `af4300f5dff7e4e49fca98233911a44a15894a43` / tree `2d0ece92e20247d3d61ac84e9215837a713e9921`, after source identity is rechecked. The prior S3 runtime implementation commit is `5b2fe73567a9c33df50fc7c662436a67814a86f4`; S3 completion/evidence is verification-only. The exact final archive and manifest hashes are presently **not established** and must be derived from actual output; no deterministic digest should be invented.

Required build inputs/constraints from `docs/plans/rc2b-06-b6-native-provisioning-runbook.md:97` and `eng/linux-m1/build-install-candidate.py`:

- Ubuntu amd64 / `linux-x64`; production `Release`; .NET SDK `10.0.114`; GCC `15.2.0`; Python and Git/binutils/coreutils versions plus signed package snapshot and hashes must be frozen before execution.
- Owner and Recovery publish with `-c Release -r linux-x64 --self-contained false -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false -p:RuntimeFrameworkVersion=10.0.14 -p:RollForward=Disable`. No invariant-globalization property is currently selected by the builder. Record exact invocation and output provenance; any altered property set requires the HD-5 decision/proof path, not ambient environment overrides.
- Native coordinator must be statically linked from frozen compiler inputs; seccomp filter is the production 672-byte object. Builder gate is glibc `>=2.43`, Bubblewrap version `0.11.1`; actual output and dependencies must pass source verifier and independently recomputed manifest/archive checks.
- Manifest platform pins: RID `linux-x64`, x86_64, kernel value captured from actual `uname -r` at build time, glibc minimum `2.43`, runtime `10.0.14`, `requiredFeatures` exactly `cgroup2`, `openat2`, `pidfd`, `SO_PEERPIDFD`, `subreaper`, `userns`, `seccomp`. `eng/linux-m1/build-install-candidate.py:195-217` records build-host `uname -r`; `src/Gagamba.Execution.Linux.M1/Installed/InstalledPayload.cs:138` requires current release equality with manifest `kernelMin`. Historical `6.6.114.1-microsoft-standard-WSL2` is an observed baseline, not a permanent minimum; a kernel update requires source/profile/candidate rebind and requalification. Runtime image/dependency closure requires actual map observations; do not equate managed `.deps.json` entries with native libraries.
- Runtime host pin in the historical profile was `/usr/lib/dotnet/dotnet`; runtime/fxr inventory and hostfxr are platform inputs, not candidate artifacts. Candidate verifier forbids unexpected managed runtime assets; owner/recovery runtimeconfigs pin `Microsoft.NETCore.App` `10.0.14`, roll-forward disabled. Inspect actual native loader closure: ELF interpreter `/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2`; glibc; OpenSSL 3; zlib; ICU >=70 for current default non-invariant path; all actually observed `dlopen`/mapped libraries. No reliance on `/usr/lib` compatibility symlink copying into installation.
- Bubblewrap must be `/usr/bin/bwrap`, version `0.11.1`, exact pinned SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`, root:root `0755`, no file capabilities. Package metadata version alone is insufficient. Ubuntu package revision in the historical profile was `0.11.1-1ubuntu0.3`; this does not prove current bytes. Any hash mismatch is a hard execution gate and requires exact package selection or separately reviewed/owner-approved pin amendment.

### Exact 14 candidate payload paths

The historical canonical manifest enumerates the structure below. New S3 output must be freshly built and rehashed; these paths are the frozen role/layout contract, not reusable historical hashes:

1. `native/exec-coordinator` (native coordinator)
2. `native/seccomp-filter.bin` (policy filter)
3. `owner/Gagamba.Execution.dll` (owner contract)
4. `owner/Gagamba.Execution.Linux.M1.Owner.deps.json`
5. `owner/Gagamba.Execution.Linux.M1.Owner.dll` (owner entry)
6. `owner/Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json`
7. `owner/Gagamba.Execution.Linux.M1.dll`
8. `recovery/Gagamba.Execution.dll` (must equal owner/provider contract bytes)
9. `recovery/Gagamba.Execution.Linux.M1.Recovery.deps.json`
10. `recovery/Gagamba.Execution.Linux.M1.Recovery.dll` (recovery entry)
11. `recovery/Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json`
12. `recovery/Gagamba.Execution.Linux.M1.dll`
13. `provider/Gagamba.Execution.dll` (must equal owner/recovery contract bytes)
14. `provider/Gagamba.Execution.Linux.M1.dll` (must equal owner/recovery M1 bytes)

The exact manifest must bind each file's role, kind, relative path, size, mode, UID/GID, link count, RID, production build identity, requiredness, consumer, survival role and SHA-256; reject extras/missing/duplicate paths. Candidate contains no installer receipt or owner decision. Historical profile pins useful for schema comparison (not S3 expected values): seccomp SHA `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`, length 672; protocol manifest schema 1, implementation `linux-m1-native-transaction`, owner/coordinator/bootstrap protocol 1, coordinator header 96 bytes, release frame 128 bytes, release sequence 1.

## Kernel/cgroup probe specification (future implementation required)

No command is claimed as a runnable harness for the absent installed-profile driver. The following is an exact proposed read-only/controlled probe interface and evidence schema; `IMPLEMENTATION-REQUIRED` before qualification. Every probe runs as the dedicated non-root runtime identity with empty effective/permitted capabilities unless the row explicitly identifies a one-time operator preparation. Kernel version/header presence is not a positive result. Capture command/argv or syscall recipe, exit/errno, raw bounded output, identity, boot ID, namespaces, kernel, mount and cgroup inode identity, evidence hash, negative control and cleanup.

Required feature checks (source is `docs/plans/rc2b-06-b6-native-env-prep-01.md:120-135`, `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs:27,131,463-492`, `NativeMethods.cs:20,55`, `CgroupInterop.cs:56-201,355`):

| Feature / source predicate | Positive observation / negative control | privilege |
|---|---|---|
| `openat2` constrained descriptor walk (`open_how` resolve flags as used by verifier) | Open expected trusted fd-relative paths; reject symlink/escape fixture with expected errno; preserve fd/inode identity | non-root |
| `pidfd_open` + required pidfd operations | Open live child pidfd, validate expected process identity/liveness, observe exit; invalid/dead/wrong pid refusal | non-root |
| `SO_PEERPIDFD` | Socket peer pidfd obtained and authenticated against separately retained original pidfd/peer credentials; wrong peer refusal | non-root |
| `SO_PASSCRED` / `SCM_CREDENTIALS` | Actual kernel credentials match peer on each record; absent/mismatched credentials refused | non-root |
| `SCM_RIGHTS` + `MSG_CMSG_CLOEXEC` | Transfer only expected descriptors; received copies CLOEXEC; wrong count/type/dead/foreign fds rejected and all delivered copies closed | non-root |
| Ancestry and namespace identity | Compare `/proc` ancestry, pid/user/mount namespaces and retained process identity as production does; substituted peer/namespace refused | non-root |
| `PR_SET_CHILD_SUBREAPER` behavior | Orphan a controlled fixture child, demonstrate designated supervisor adopts and reaps exact child; no unrelated process adopted; bounded cleanup | non-root |
| Seccomp and required namespaces | Execute production filter / user+mount+pid namespace path; allowed fixture succeeds, denied syscall/unsupported namespace fails closed before target dispatch; no broad fallback | non-root |
| Atomic cgroup spawn | With delegated positive control, use production `posix_spawnattr_setcgroup_np` path and prove first observable membership is target domain; sibling/outside case and missing grants fail before dispatch | non-root; operator only creates prior delegation |

Probe record must additionally freeze (1) `uname` kernel release/arch; (2) `statfs` cgroup2 mount identity; (3) `cgroup.controllers` and `cgroup.subtree_control` (`memory`,`pids`); (4) service ControlGroup `P`, boot/mount/device/inode identity and supervisor leaf placement; (5) delegated UID/GID and exact modes/ACLs; (6) child `memory.max=134217728`, `memory.swap.max=0`, `pids.max=48` with successful exact readback; (7) child `cgroup.kill` write-only authority, `cgroup.events`/`cgroup.procs` read, target/common-ancestor `cgroup.procs` write permission needed by atomic kernel placement; (8) stop and cleanup evidence: original descriptors remain valid, kernel kill accepted, `populated 0`, processes reaped, no outside process affected. This eight-field envelope and every feature row are **probe-required**, not met by plan/source inspection.

Delegation contract: one named systemd service, actual resolved `P`, supervisor first placed in `P/supervisor` (systemd `DelegateSubgroup=supervisor` only if supported or separately reviewed operator placement), empty interior `P/domains`, pre-enabled memory+pids, runtime creates only children. Do not grant runtime writes to `cgroup.threads` or `cgroup.subtree_control`; do not recursively chown parent; no unrelated cgroups. Operator retains parent limits. Runtime envelope must outlive controller long enough to observe stop/reap. `Delegate=memory pids` alone is insufficient.

## Trusted installation predicates and F02 chain

Source contract: `eng/linux-m1/install_common.py:510-591`, `src/Gagamba.Execution.Linux.M1/Installed/InstalledMetadata.cs`, `InstalledManifest.cs:124`, plus env plan §§9–10 and runbook §§5–7.

- Trusted ancestry is checked from `/` through `/opt`, `/opt/gagamba`, `installations`, selected generation, active selector/manifest/receipt/qualification record and every native/managed artifact. Must be root-controlled, no writable ancestor, symlink, hardlink substitution, unexpected owner/mode/link count or special file; verify fd-relative with `O_NOFOLLOW`/stable metadata. A fresh distro must demonstrate suitable `/` ancestry. Existing development WSL `/` `0:0 0777` is an intentional negative control; do not chmod it. D: may contain the WSL VHDX only; `/mnt/d` is not a trusted install path.
- Generation is immutable root-owned content under `/opt/gagamba/installations/<artifact digest>`; active selector chooses generation atomically; receipt binds exact candidate manifest/artifact set and installer identity. Qualification record under `/opt/gagamba/qualifications/<id>.json` is receipt-selected runtime authority. It is distinct from `/etc/gagamba/accepted-qualifications/<id>.json`, the independent administrative approval input. Never merge stores.
- F02 approval is exact four-key JSON: `qualificationId`, `artifactSetSha256`, `qualificationRecordSha256`, `accepted` (JSON boolean `true`), root:root `0444`, single-link, trusted ancestors. Hash exact raw record bytes. Independent administrator establishes provenance and acceptance; producer/candidate cannot self-approve. Approval withdrawal blocks later publication/retry after serialization, but does not revoke already installed runtime record. Publish only after required gates and truthful scope; avoid circularly claiming Q positives needed to create the record that enables those positives. O03 remains unresolved for required staging/publication sequencing.

## HD-5 and evidence freeze status

HD-5 is approved only as a **conditional design policy** (`docs/decisions/rc2b-06-b6-spec-owner-acceptance.md:205-226`): prefer invariant globalization for trusted managed components only after exact compatibility tests prove no regression to B1–B5 or protocol/error semantics. Test actual production config and all three trusted roles; if incompatible, stop and request bounded alternative baseline decision. This policy does not require invariant mode for untrusted children and does not eliminate native closure duties. Current builder does not enable invariant and verifier disallows ambient property overrides; default requires ICU >=70. No S3 compatibility proof or configuration decision is evidenced here. Freeze as **current default non-invariant + ICU closure required**; an invariant candidate is not authorized by this note.

Freezeable now: source revision/tree and production-vs-test boundary; 14 payload roles/layout; build property set and runtime/RID requirements; source constants and limits; trusted filesystem and F02 predicates; candidate digest derivation method; explicit probe interface and stop gates; Terra's disposition. Actual image/package bytes, built S3 candidate hashes, observed native maps/dependencies, feature results, delegated authority, root install/receipt and F02 record remain unavailable/unverified and block execution or acceptance.

## Terra independent findings preserved (O01–O05)

Original disposition **PLAN SUPPORTED WITH QUALIFICATIONS; no demonstrated BLOCKER/MAJOR/MINOR**. Preserve these as five execution gates, not defects or passes:

- **O01 exact bytes:** no authenticated image digest, frozen packages/static inputs, Bubblewrap binary match, glibc export, or exact hostfxr measured. Freeze signed source snapshot and exact hashes; mismatch stops; no silent substitution.
- **O02 delegation:** supervisor leaf, controller enablement, limits, atomic spawn permissions and bounded non-root authority unexecuted. Stop on failure; no fallback or verifier relaxation.
- **O03 qualification bootstrap:** F02 accepted record precedes publication but Qualified tests can depend on it. No staged record/design resolution exists. Separate `/etc` approval and `/opt` runtime stores; no self-approval.
- **O04 driver:** no runnable installed-v2 B2/B3 HELD scenario command exists. Existing PN/legacy probes are not substitutes. Separately commission/review source-bound driver; no identity bypass.
- **O05 closure/truncation/capacity:** actual Owner/Recovery loaded native closure not proven; ICU default and HD-5 proof unresolved; dedicated 4096-byte evidence receiver `MSG_CTRUNC` not forced; build peaks/storage/log sizing unmeasured.

## Future Luna driver commission handoff (separate authorization required)

Production entry points to anchor against expected source: `OwnerPreparationTransaction.Run` delegates to private `RunTransaction` (`src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs:29-49`); production integrated branch creates real domain and later invokes `IHeldLauncher.PrepareHeldAsync` / `HeldReleasePhase` (`:153-162,235,419-449`). This is an internal implementation surface, not a public test API. `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs:414-468` is a reusable native delegated positive harness: it actually creates a domain, installs exact limits and calls `PrepareHeldAsync`. Reuse its mechanics only after reviewing exact source/revision; do not call it installed-profile qualification as-is. `CgroupInterop.cs:56-103` exact target values are 134217728, 0, 48; `:117-201` supports live cgroup-procs/populated observation.

Relevant entry symbols and IVT/build boundary: Owner entry `src/Gagamba.Execution.Linux.M1.Owner/Program.cs:75` invokes `OwnerPreparationTransaction.Run`; that internal method (`Lifecycle/OwnerPreparationTransaction.cs:27-49`) constructs the real request and private `RunTransaction`; integrated execution installs limits at `:235` and reaches `IHeldLauncher.PrepareHeldAsync` / `HeldReleasePhase` at `:419-449`. Recovery entry is `src/Gagamba.Execution.Linux.M1.Recovery/Program.cs:2` → `Bootstrap/RecoveryHost.Run(args)` (`RecoveryHost.cs:12+`); qualified B2 symbols are `QualifiedB2Owner.Run` and `QualifiedB2Recovery.Run`, integrated qualified path `IntegratedRecoveryHost.Run`. M1 `Execution/TransactionStateModel.cs:12-16` grants IVT to `Gagamba.Execution.Linux.M1.Tests`, `Gagamba.Execution.Linux.M1.Owner`, and conditionally `Gagamba.Execution.Linux.M1.D6_4.Tests` when `GagambaWp05D64FixtureEnabled=true`; `Bootstrap/Visibility.cs:1-2` unconditionally grants IVT to `Gagamba.Execution.Linux.M1.Recovery` and `Gagamba.Execution.Linux.M1.B2Controller`. A plausible future harness carrier is a separately built test-only assembly named `Gagamba.Execution.Linux.M1.B2Controller`, referencing the actual production-installed `Gagamba.Execution.Linux.M1.dll`; this uses the existing friend boundary while keeping the carrier outside the 14-file production candidate. Review assembly naming/build inputs and installed DLL identity; do not force a Debug/fixture variant of the installed production M1. Never add an unqualified route to production Release entrypoints. Keep test helpers out of 14 production artifacts. The fixture projects source selftest is not installed-driver evidence.

Required driver scenarios and observable results:

1. **B2 non-integrated qualified handshake:** positive C/R/O process topology, expected S2 identity supplied only by real argument-free installed `VerifyActive()`, direct echo and final Ready gating; verify all process credentials, original pidfds, message IDs/sequences, no result before final C acceptance. Delegated-parent B2 refusal occurs before allocation; B2 alone is not HELD qualification.
2. **B3 integrated transaction / HELD:** actual trusted install, real controller→Recovery→Owner process tree, bootstrap/Associate 9/10 before rendezvous 8; Owner 23/24, 38/11, controller/owner echoes and confirmation12; real shared domain and live original domain descriptors; journal registration and retained recovery authority; staged objects authorization/acknowledgement, `WriterReady`, then HELD/RELEASE. Reuse exact `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs` semantics and `tests/Fixtures/B2Controller/B6IntegratedProbe.cs` only for handshake mechanism boundaries; latter explicitly never emits WriterReady, creates a domain, reaches HELD, or supplies authority.
3. **Process/FD custody:** record C/R/O PIDs/PPIDs, namespace IDs, peer credentials and original pidfd identity; before/after FD inventory per actor, transferred fd type/CLOEXEC, retained originals; socket sequence/rights counts and causal barrier; process exit vs wait/reap distinct. Observe cgroup membership on first spawn, exact limits, `cgroup.events populated=1→0`, cgroup.procs empty, kill acceptance and descendant reaping.
4. **C3 stop/HELD:** preserve absolute deadline, primary and recovery domain descriptors, independent OwnerSafetyMonitor behavior; kill controller while owner worker blocked and while held; prove exactly one native stop, kernel acceptance, owner acknowledgement where expected, Owner exit and Recovery reaping; report `OwnerAcknowledged`, `NativeStop` and final cleanup facts distinctly. Missing ACK is not a pass from exit code alone; leave Unknown/HELD on unresolved population/reap evidence. Include bounded before-deadline and at/after-deadline cases.
5. **Mutation controls:** absent/wrong install, writable ancestor, selector race, changed candidate/receipt/qualification, wrong record scope, bad peer identity, wrong protocol/sequence/FD cardinality, delegation loss/controller unavailable/limit readback mismatch; each must refuse before its specified side-effect point, with valid neighboring positive control and complete cleanup evidence.

No final command is asserted: a new source-bound driver and exact per-case commands are **IMPLEMENTATION-REQUIRED** and then need independent review before authorized execution. The runner must export structured per-process/FD/cgroup observations and bind driver source hash, fixture hashes, S3 candidate/manifest digest, installed generation/receipt/record hashes, profile ID/version, host and evidence digest. It cannot itself create the F02 approval or production-qualified result.

## Source anchors

- `Gagamba/AGENTS.md`; `docs/handoff.md`; `docs/work-queue.md` (read first; unavailable native qualification remains Pending).
- `docs/plans/rc2b-06-b6-native-env-prep-01.md` §§3–10; `docs/plans/rc2b-06-b6-native-qualification-matrix.md` §§1–5; `docs/plans/rc2b-06-b6-native-provisioning-runbook.md` §§1, 4–7.
- `docs/evidence/rc2b-06-b6-impl-01-s2.md` §§2–4 (historical candidate explicitly not new S3 bytes); `docs/evidence/rc2b-06-b6-impl-01-s3.md` “Completion mapping”, “Actual B2 and integrated chronologies”, “Loaded artifacts and native failure observations”.
- `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs`; `NativeMethods.cs`; `Native/CgroupInterop.cs`; `Installed/InstalledManifest.cs`; `Launch/HelperInstallation.cs`; `Lifecycle/OwnerPreparationTransaction.cs`; `Lifecycle/HeldReleasePhase.cs`.
- `eng/linux-m1/build-install-candidate.py`, `eng/linux-m1/install_common.py`; `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs`; `tests/Fixtures/B2Controller/B6IntegratedProbe.cs`.




