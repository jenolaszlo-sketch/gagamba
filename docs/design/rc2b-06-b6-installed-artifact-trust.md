# RC2B-06 B6 — Installed-Artifact Trust, Deployment Attestation and Qualification

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-01.

**Status:** Design specification only. Not implementation. Not production activation.
Not publication. No source, test, native, packaging or workflow change is authorized
by this document.

**Baseline:** canonical `main` at `2cd8c1d716ed2e3e61156819730cd3d39e81844b`
(tree `e9ed9989ee35c43557e0a93fdec4d313af4f740e`), verified before writing.

**Scope:** local, source-bound, installed-artifact and deployment qualification of
Gagamba's experimental Linux restricted-execution implementation (B3/B4/B5
composition). Initial positive qualification is scoped to the existing
Windows/WSL2 x86-64 experimental profile. Native Linux-host qualification is a
separate requirement (B5-L01 / B7).

**Non-authorizations:** this design does not authorize B6 implementation, a
production restricted provider, `RestrictedExecutionRuntime.Create()` activation,
NuGet or public package publication, signing infrastructure, remote attestation,
TPM/secure boot, containers/VMs, Hufu integration, automatic updates/rollback,
B7, macOS/native-Linux-host qualification, or any change to C1–C4, R1-F03 Option A,
B1–B5 accepted semantics.

**Amendment history:** the R1 remediation for the independent Q1 review (commit
`77ee4414df37b9f88979bde02742756538ba45af`) added §2.7 (native runtime dependency
closure), §6.2 (wire-specified installation-identity handshake) and the
qualification-record association. The bounded Q2 corrections (recorded in
`docs/evidence/rc2b-06-b6-spec-r1-q2-correction-receipt.md`) fix residual FD
wording, section numbering, the `qualificationId` grammar, trusted-acceptance
wording, the identity-echo projection and the globalization-decision label. These
corrections do not change any accepted architectural semantics, message ordering or
authority separation, and no implementation is authorized by this document.

---

## 1. Purpose and boundaries

B5 established sealed C3 monotonic-clock and durable-deadline reconciliation
inside the qualified WSL2/Windows scope. The accepted implementation does not
establish that a locally installed sandbox consists of the exact authorized and
qualified bytes. B3 additionally retained:

- **B3-F04:** the experimental profile is caller-populated rather than an
  independently qualified installation.
- **B3-F06:** the provider descriptor truthfully reports `Unavailable` because no
  production restricted provider is installed.
- **B5-L04:** the B2 installation closure (`artifacts/b2/install`) was absent, so
  relevant native installation/lifecycle cases skipped.

B6 answers one question:

> How can a trusted Gagamba host establish that the exact implementation it is
> about to execute, and that its surviving recovery actors will continue to use,
> is an authorized and qualified installation rather than an arbitrary collection
> of compatible-looking files?

B6 keeps six identities distinct and never lets one imply another:

| Identity | Question | Kind |
|---|---|---|
| Artifact identity | Which logical component is this? | role + build identity (descriptive) |
| Artifact integrity | Are these the exact bytes? | SHA-256 per file + artifact-set digest |
| Installation provenance/trust | Who authorized this set on this host? | root-owned install receipt + qualification record |
| Deployment compatibility | Does it fit this host/profile? | RID, kernel, glibc, runtime, bwrap, features |
| Runtime qualification | Was this exact set exercised? | source-bound qualification evidence |
| Execution/release authority | May this operation release? | owner-local capability + RELEASE writer only |

This document is deliberately **not** a general software-supply-chain platform.
It defines one local installation candidate, one verifier, one descriptor and one
test plan. It adds no package manager, signing service, daemon, updater, remote
attestation or new release authority.

---

## 2. Gate A — Actual deployment closure inventory

### 2.1 Method

The inventory below is derived from the current source execution paths, not from
design sketches or historical fixture filenames. The relevant paths are:

- `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs` — the only
  trusted internal composition that binds B1 control to the real owner
  transaction; `B3FixtureProfile` is the current caller-populated pin set.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs` — original
  controller direct session; never obtains capability/store/RELEASE writer.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedRecoveryHost.cs` — actual
  recovery-parented owner, stop, reaping and journal cleanup.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryHost.cs`,
  `RecoveryConnection.cs`, `RecoveryProtocol.cs` — recovery process, bootstrap
  configuration, `PinnedExecutable` closure and wire.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/ExperimentalRuntimePin.cs` — .NET
  host/runtime/hostfxr pins and private-directory checks.
- `src/Gagamba.Execution.Linux.M1/Launch/HelperInstallation.cs` — pinned
  Bubblewrap open/hash/identity.
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs`,
  `BubblewrapArguments.cs` — fixed sandbox plan, seccomp filter and coordinator
  launch.
- `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs` — atomic
  `posix_spawn` with `posix_spawnattr_setcgroup_np`, fd descriptor map and
  execution of `/proc/self/fd/11` (the opened Bubblewrap object).
- `src/Gagamba.Execution.Linux.M1/Preparation/SafeCopyIn.cs`,
  `Preparation/Native/PreparationInterop.cs` — descriptor-relative acquisition,
  digest, read-only reopen, dense accounting and identity binding.
- `src/Gagamba.Execution.Linux.M1/Native/OwnerBootstrap.cs`,
  `OwnerProtocol.cs`, `Lifecycle/OwnerPreparationTransaction.cs` — owner
  bootstrap roles, bounded wire, transaction order and authority allocation.
- `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`,
  `Lifecycle/OwnerHeldWire.cs`, `Launch/HeldExecution.cs` — one physical RELEASE
  and held authority.
- `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs`,
  `Lifecycle/MonotonicClock.cs` — sealed C3 clock and reconciliation.
- `src/Gagamba.Execution/Restricted/Control/RestrictedProvider.cs` — public
  passive descriptor and refusal vocabulary.
- `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs` — production selection
  remains `Unavailable`.
- `native/linux-m1/exec-coordinator.c`, `protocol.h`, `release_protocol.h`,
  `seccomp_filter_policy.py`, `seccomp-filter.hex`.
- `eng/linux-m1/build-native.sh`, `build-d5-status.sh` — current native build
  commands (test/qualification only; no installer).
- `tests/Fixtures/*` — test-only coordinators, targets, probes and D6.4 variant.

### 2.2 Inventory table

`Req` = required for the qualified experimental profile. `Survives` = must
survive controller death. `Target` = can the target influence selection? `Refusal`
= required behavior when absent, changed or unqualified.

| # | Logical role | Actual identity / source | Provenance | Req | Class | Version / compatibility | Resolution | Mutable after install? | Consumer | Survives | Target | Integrity / provenance evidence | Refusal |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A1 | Controller provider assembly | `Gagamba.Execution.Linux.M1.dll` | `src/Gagamba.Execution.Linux.M1` build | yes | managed | assembly 0.1.0.x; protocol constants below | host loads assembly; verifier hashes `Assembly.Location` against manifest provider role | no (root-owned 0444) | trusted host | n/a (controller) | no | SHA-256 + artifact-set digest + root receipt | `UnqualifiedInstallation` before operation |
| A2 | Public contract assembly | `Gagamba.Execution.dll` | `src/Gagamba.Execution` build | yes | managed | 0.1.0.x | same | no | controller, owner, recovery | yes | no | SHA-256; must be identical in owner and recovery closures | refusal |
| A3 | Runtime facade (only if host composes through it) | `Gagamba.Runtime.dll` | `src/Gagamba.Runtime` build | conditional | managed | 0.1.0.x | host load | no | controller host | n/a | no | SHA-256 | refusal |
| A4 | Recovery entry | `Gagamba.Execution.Linux.M1.Recovery.dll` | `src/Gagamba.Execution.Linux.M1.Recovery` | yes | managed | net10.0; owner protocol v1 | pinned absolute path; `dotnet <dll>` | no | recovery process | yes (own lifetime) | no | SHA-256 + role closure digest | refusal before association |
| A5 | Recovery dependency manifest | `...Recovery.deps.json` | same build | yes | config | exact runtime asset set | read by host; verifier enumerates runtime assets | no | recovery host | yes | no | SHA-256; runtime asset set must equal manifest set | refusal on extra/missing asset |
| A6 | Recovery runtime config | `...Recovery.runtimeconfig.json` | same build | yes | config | `Microsoft.NETCore.App` 10.0.0; roll-forward fixed | hostfxr | no | recovery host | yes | no | SHA-256; framework name/version checked | refusal |
| A7 | Recovery managed dependency | `Gagamba.Execution.Linux.M1.dll` | same build | yes | managed | exact build | deps resolution | no | recovery | yes | no | SHA-256; must equal owner/controller copy | refusal |
| A8 | Recovery managed dependency | `Gagamba.Execution.dll` | same build | yes | managed | exact build | deps resolution | no | recovery | yes | no | SHA-256 | refusal |
| A9 | Owner entry | `Gagamba.Execution.Linux.M1.Owner.dll` | `src/Gagamba.Execution.Linux.M1.Owner` | yes | managed | net10.0; owner protocol v1 | pinned absolute path; `dotnet <dll>` | no | owner process | until owner exit | no | SHA-256 + role closure digest | refusal before spawn |
| A10 | Owner dependency manifest | `...Owner.deps.json` | same build | yes | config | exact runtime asset set | host | no | owner host | owner lifetime | no | SHA-256; exact assets | refusal |
| A11 | Owner runtime config | `...Owner.runtimeconfig.json` | same build | yes | config | 10.0.0 + fixed roll-forward | hostfxr | no | owner host | owner lifetime | no | SHA-256 | refusal |
| A12 | Owner managed dependency | `Gagamba.Execution.Linux.M1.dll` | same build | yes | managed | exact build | deps | no | owner | owner lifetime | no | SHA-256 | refusal |
| A13 | Owner managed dependency | `Gagamba.Execution.dll` | same build | yes | managed | exact build | deps | no | owner | owner lifetime | no | SHA-256 | refusal |
| A14 | Coordinator | static ELF from `native/linux-m1/exec-coordinator.c`, built with the exact reviewed gcc command | native source build | yes | native | protocol `GGM1_VERSION=1`, header 96, config header 202, report version 1, release frame 128, release sequence 1 | installed `native/exec-coordinator`; staged by `SafeCopyIn` as role `coordinator`; bound at `/coordinator` | no | sandbox trusted setup; HELD/release gate | no (per-execution) | no (read-only bind, fd identity) | SHA-256 + static-ELF/arch + source/build identity | refusal before staging |
| A15 | Seccomp policy | 672-byte BPF from `native/linux-m1/seccomp_filter_policy.py --binary`; digest `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` | generator + `--check` | yes | policy | x86-64 `AUDIT_ARCH_X86_64`; generator version 4 | installed `native/seccomp-filter.bin`; sealed memfd fd 6 | no | coordinator/bwrap | no | no | SHA-256 + exact length 672 + generator check | refusal before spawn |
| A16 | Sandbox plan | `BubblewrapArguments.Build()` fixed argv; `CoordinatorMountVerifier` expected mount tree; `FixedResources` | compiled source | yes | code-derived policy | v1 | compiled; no file | n/a | bwrap/coordinator/owner | no | no | source commit + compiled constants digest | refusal on mismatch |
| A17 | Bubblewrap launcher | `/usr/bin/bwrap`, 0.11.1, SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` | OS package (`bubblewrap` 0.11.1) | yes | native platform | 0.11.1; x86-64; root:root 0755; no file caps | `HelperInstallation.TryOpenPinnedBubblewrap`; opened fd; executed via `/proc/self/fd/11` | root package update only (refused) | bwrap | no | no | SHA-256, uid/gid/mode, no caps, ELF arch, `ldd` baseline | refusal before spawn |
| A18 | .NET host | `/usr/lib/dotnet/dotnet` (WSL2 example), exact SHA-256 | OS/runtime install | yes | runtime platform | 10.0.x exact | `ExperimentalRuntimePin.HostPath`; no PATH | root update only (refused) | controller/recovery/owner host | yes | no | SHA-256 + root-owned + immutable | refusal |
| A19 | .NET shared runtime | `/usr/lib/dotnet/shared/Microsoft.NETCore.App/<exact>` | OS/runtime install | yes | runtime platform | exact version; inventory digest | `RuntimeDirectory`; `Environment.Version` + directory check | root update only (refused) | all managed actors | yes | no | recursive inventory SHA-256 + root-owned | refusal |
| A20 | .NET hostfxr | `/usr/lib/dotnet/host/fxr/<exact>` | OS/runtime install | yes | runtime platform | exact version; inventory digest | `HostFxrDirectory` | root update only | all managed actors | yes | no | recursive inventory SHA-256 | refusal |
| A21 | ELF loader / libc | distro `ld-linux-x86-64.so.2`, libc, libm, libpthread, libdl, librt | OS baseline | yes | platform baseline | glibc >= 2.43 (atomic spawn); exact package version recorded | loader default search; env cleared | root update only | bwrap and managed host native runtime | yes | no | package version + file SHA-256 + `/etc/ld.so.preload` empty | refusal |
| A22 | Bubblewrap shared libraries | libcap, libseccomp, libselinux (as linked), libc | OS baseline | yes | platform baseline | package versions + file hashes | dynamic loader | root update only | bwrap | no | no | `ldd` inventory hashes + package identity | refusal |
| A23 | Kernel / cgroup2 features | Linux kernel; cgroup2; user/pid/net/ipc/uts/mnt namespaces; `openat2`; `pidfd_*`; `SO_PEERPIDFD`; `PR_SET_CHILD_SUBREAPER`; `posix_spawnattr_setcgroup_np` | host OS | yes | platform baseline | kernel >= recorded; glibc >= 2.43; unprivileged uid, empty CapEff | feature probes at admission | host update only | all | yes | no | kernel/glibc/feature evidence | `PrerequisitesUnavailable` |
| A24 | Delegated cgroup parent | operator-supplied delegated cgroup2 subtree | host operator | yes | runtime state | `memory.max`, `memory.swap.max`, `pids.max` readable | profile path; `TryOpenDelegatedParent` | operator | recovery/owner | yes | no | cgroup path/identity + limits | refusal |
| A25 | Journal directory | user-owned 0700 directory | runtime | yes | runtime state | journal schema v3 unchanged | `OwnershipJournal` | runtime writes | owner/recovery | yes | no | ownership/mode; durable record | refusal/Unknown |
| A26 | Staging parent | user-owned 0700 directory | runtime | yes | runtime state | none | `SafeCopyIn` | runtime | owner/recovery | yes | no | ownership/mode | refusal |
| A27 | Evidence directory | user-owned 0700 directory | runtime | optional diagnostic | runtime state | bounded JSON facts | `RecoveryEvidence` | runtime | recovery | yes | no | bounded facts; no secrets | refusal if required by test |
| A28 | Target tool | enrolled per execution; `tool` object | trusted host enrollment | per execution | untrusted input | profile-specific | `SourceRootPath` + digest | per execution | sandbox target | no | no (enrolled, read-only) | SHA-256 claimed digest at staging | refusal on mismatch |
| A29 | Target input | enrolled per execution; `input` object | trusted host enrollment | per execution | untrusted input | profile-specific | `SourceRootPath` + digest | per execution | sandbox target | no | no (read-only) | SHA-256 | refusal |
| A30 | Capture/readback component | `ProtectedCapture`, `RestrictedCaptureRead` path | `Gagamba.Execution.Linux.M1` build | yes | managed code | exact build | compiled | no | controller/owner | owner lifetime | no | required type/method presence at admission + runtime evidence | refusal; no incomplete enrollment |
| A31 | Qualification/evidence files | `qualification.json`, review/evidence digests | accepted B6 record | yes for `Qualified` | metadata | v1 | root-owned record | root only | verifier/descriptor | n/a | no | root-owned + digest | `Unqualified` |

### 2.3 Current actual artifact resolution (source-backed)

- **Controller:** the integrated controller is the `IntegratedB3Client` inside the
  trusted host process. It calls `RecoveryConnection.Start(config)` where
  `config.Recovery` and `config.Owner` are `PinnedExecutable` values built by the
  caller profile. `RecoveryConnection.Start` verifies the recovery entry path and
  hashes the five-file closure before spawn. It then uses
  `ExperimentalRuntimePin.StartInfo(recovery)` only when
  `config.Integrated is not null`; otherwise it uses `new ProcessStartInfo("/usr/bin/dotnet")`
  and **inherits the ambient environment** (`RecoveryConnection.cs:52–55`). The
  integrated profile is the only one with an explicit runtime pin.

  **Load-path correction (remediation for B6-Q1-F10)**: The design previously
  stated "host loads from install root." The practical binding is hashing the
  already-loaded `Assembly.Location` against the manifest's provider role, as
  `B3Qualification` already does for owner/recovery pins (`B3Qualification.cs:158–165`).
  The load path is immaterial; the verification is the digest comparison. The
  wording is corrected to "host loads assembly; verifier hashes
  `Assembly.Location` against manifest provider role."
- **Recovery:** `RecoveryHost.RunCore` decodes a bounded JSON
  `RecoveryConfiguration`, validates it (`Version == 1`), receives control and
  credential fds, verifies peer/uid/parent and `config.Recovery.Verify(...) ==
  Assembly.GetEntryAssembly().Location`, then detaches lifetime, initializes the
  subreaper/scope and, when `config.Integrated is not null`, runs
  `IntegratedRecoveryHost.Run`.
- **Owner spawn:** `M1RecoveryScope.TrySpawnOwner` uses
  `FileName = "dotnet"` and only replaces it with `runtime.StartInfo(ownerDll)`
  when `runtime is not null` (`M1RecoveryScope.cs:194–205`). The integrated path
  uses the pinned host; the legacy path uses PATH.
- **Managed closure:** `PinnedExecutable` requires exactly five names:
  `<entry>.dll`, `<entry>.deps.json`, `<entry>.runtimeconfig.json`,
  `Gagamba.Execution.Linux.M1.dll`, `Gagamba.Execution.dll`
  (`RecoveryProtocol.cs:78–100`). It hashes them by path. It does **not**
  enumerate `deps.json` runtime assets, the native apphost, PDBs, extra files,
  symlinks, link count, ownership, mode or file capabilities.
- **Runtime:** `ExperimentalRuntimePin.VerifyCurrent` requires Linux,
  unprivileged uid with empty `CapEff`, exact `Environment.Version`, exact
  runtime directory, root-owned/immutable host/runtime/hostfxr, hashed host and
  recursive inventory digests. `StartInfo` clears the environment and sets
  `PATH=/usr/bin:/bin`, `DOTNET_ROOT`, `DOTNET_MULTILEVEL_LOOKUP=0`,
  `DOTNET_EnableDiagnostics=0`, `DOTNET_ROLL_FORWARD=LatestPatch`
  (`ExperimentalRuntimePin.cs:94–107`). The ownership helper requires root for
  the host/runtime/hostfxr objects (`rootOnly: true`) but accepts a current-uid
  owner for private data directories and for ancestors
  (`ExperimentalRuntimePin.cs:79–93`); the installed closure therefore needs a
  stricter root-only predicate.
- **Coordinator/filter/tool/input:** `B3Qualification.TryCreate` checks
  `File.Exists`/`VerifySha256` by path for the owner DLL and three source
  artifacts, plus the compile-time filter length/digest. `B3PreparationOwner.BuildPlan`
  then stages them through `SafeCopyIn` with claimed digests. `SafeCopyIn` opens
  sources with `O_RDONLY|O_NOFOLLOW|O_CLOEXEC` and
  `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`, checks regular
  file, size stability, digest, read-only reopen, device/inode, mode, link count
  and dense accounting. It is the strongest existing mechanism; the remaining
  weakness is the owner DLL, which is path-verified and then path-spawned.
- **Bubblewrap:** `HelperInstallation` opens `/usr/bin/bwrap`, requires root:root
  regular 0755, no file capability xattr, exact SHA-256, identity and CLOEXEC.
  `AtomicCgroupSpawn` then executes `/proc/self/fd/11` with
  `posix_spawnattr_setcgroup_np`; the pathname is not used after open.
- **Seccomp:** the filter is a 672-byte x86-64 BPF program; `BubblewrapAdapter`
  re-reads the staged filter, checks length and digest against the compile-time
  `FilterSha256`, seals it into a memfd and passes fd 6. It is x86-64 only
  (`AUDIT_ARCH_X86_64` in `seccomp_filter_policy.py`).
- **Descriptor:** `RestrictedProviderDescriptor` is passive, has no public
  constructor, and today returns nulls and `Unavailable`. `RestrictedProfileSelection`
  already carries `ProfileId`, `ProfileVersion`, `RuntimeIdentifier`,
  `QualificationId` and `ArtifactSetSha256`.
- **Production selection:** `RestrictedExecutionRuntime.Create()` returns
  `UnavailableRestrictedExecutionProvider`; `CreateOperation` refuses the
  experimental profiles with `InstallationUnavailable`. `Gagamba.Execution.Linux.M1`
  is not packable; Owner/Recovery are `IsPackable=false`.

### 2.4 System-dependency boundary

Application-controlled artifacts are A1–A3 (controller provider closure), A4–A13
(owner/recovery closures), A14 (coordinator), A15 (seccomp policy), A16
(code-derived sandbox/profile policy), A30 (capture/readback code) and the
installation trust metadata A31. These must be in the artifact-set manifest.

Accepted **platform baseline** (not application artifacts, but version-constrained
and evidence-recorded):

- A17 Bubblewrap 0.11.1 at `/usr/bin/bwrap`; root-owned, mode 0755, no file caps.
- A18–A20 the exact .NET host, hostfxr and shared framework.
- A21–A22 the ELF loader, glibc and Bubblewrap's shared libraries.
- A23 kernel, cgroup2, namespaces and required syscalls/features.
- A24 the operator-delegated cgroup. A25–A27 are runtime state, not install
  artifacts.

The platform baseline is trusted because the target cannot modify root-owned
system files and the trusted host/operator controls them. It is **not**
"uncontrolled": the installation records exact versions and hashes where
practical, and admission refuses an incompatible baseline. A system library that
cannot be identified or constrained is not silently trusted; it is a bounded
qualification limitation.

### 2.5 Test-only assets that must never enter the production closure

Verified in the current tree:

- `tests/Fixtures/b3-release-coordinator.c` — B3 fixture coordinator.
- `tests/Fixtures/wp04-qualification-coordinator.c` — WP04 qualification variant.
- `tests/Fixtures/wp05-qualification-coordinator.c` — D5/WP05 variant.
- `tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c` and its built `out/fixture`.
- `tests/Fixtures/b3-exec-target-probe.c`, `b3-exec-oracle-probe.c`,
  `b3-exec-confirm.h`, `b4-capture-target.c`, `wp05-sigpipe-probe.c`.
- `tests/Fixtures/B2Controller/` (`Gagamba.Execution.Linux.M1.B2Controller`),
  `Wp05D4Owner`, `Wp05LeaseFixture`, `Gagamba.Fixture.*`.
- `tests/Gagamba.Execution.Linux.M1.D6_4.Tests/` and the
  `Gagamba.Execution.Linux.M1.TestFixture` assembly variant (build symbol
  `GAGAMBA_WP05_D6_4_TEST_FIXTURE`, isolated `bin/d6-4-variant`/`obj/d6-4-variant`).
- `Int01QualificationFault` / `QualificationFault` injection hooks
  (`Bootstrap/Int01QualificationFault.cs`, `IntegratedRecoveryHost.cs`,
  `OwnerPreparationTransaction.cs`).
- `OwnerSafetyMonitor.QualificationNativeStop` and
  `ProtectedCapture.QualificationBeforeCaptureStore` test seams.
- `FakeDomainClock` / `RelativeTestClock` and the open `IDomainClock`
  constructor (production composition uses sealed `StopwatchDomainClock.Instance`).
- `artifacts/linux-m1/b3/b3-release-coordinator`,
  `artifacts/linux-m1/wp04/exec-coordinator` as **test-built** copies,
  `artifacts/linux-m1/wp05/wp05-qualification-coordinator`.
- `artifacts/b2/install` (absent today) is a test-install closure, not a
  production installation.

**Production/test separation (remediation for B6-Q1-F02)**: The above items from
`Int01QualificationFault` through `FakeDomainClock` are currently **compiled into
the production `Gagamba.Execution.Linux.M1.dll`**. A file/assembly hash deny list
cannot exclude individual types or members from an otherwise approved DLL. The
design requires a genuine production build closure in which test-only
fault-injection capabilities are **absent or demonstrably unreachable** under an
explicitly justified security boundary.

Three alternatives are evaluated:

**A. Compile-time exclusion (recommended)**
- Define a `Production` build configuration (or `DefineConstants` condition)
  that excludes qualification-only code and symbols from the production assembly.
- The following are conditionally compiled out of the `Production` configuration:
  - `Int01QualificationFault` / `Int01Qualification` types and all references
  - `OwnerSafetyMonitor.QualificationNativeStop` field and its wiring
  - `BubblewrapAdapter.QualificationBeforeCaptureStore` field and its wiring
  - `SafeCopyIn.CopyTestHooks` / `BarrierDeathException` types and all references
  - `JournalFileStorage.QualificationBeforeRename` field and its wiring
  - `FakeDomainClock` / `RelativeTestClock` / open `IDomainClock` constructor
- The `Debug` and `Test` configurations retain these for deterministic and native
  qualification. The `Release` configuration used for the candidate build is
  aliased to `Production`.
- The verifier inspects the production assembly (via `Assembly.GetTypes()`,
  `Module.GetMethods()`, reflection over `internal` members) and asserts that
  none of the excluded type/member names exist. This is an executable test, not a
  textual symbol claim.

**B. Separate test assembly or adapter**
- Move all qualification-only behavior into a separately built, non-deployable
  assembly (e.g., `Gagamba.Execution.Linux.M1.TestSeams.dll`) referenced only by
  test projects and the D6.4 variant.
- The production M1 assembly has no reference to it and contains no
  fault-injection surface.
- This requires more source restructuring but provides the strongest assembly-level
  separation.

**C. Narrow runtime guard (only if A/B are infeasible)**
- Retain the hooks in the production assembly but gate every entry point behind a
  `RuntimeConfiguration` check that is **provably false** in the production
  configuration (e.g., a `AppContext` switch set only by the test runner, or a
  `DOTNET_GAGAMBA_QUALIFICATION_MODE` environment variable that the cleared
  environment guarantees is absent).
- The verifier must prove: (1) the guard is on every reachable path, (2) the
  production environment cannot satisfy it, (3) no public/internal configuration
  can enable it. This is harder to prove than A/B and is not recommended.

**Recommendation: Alternative A (compile-time exclusion)** based on actual source
coupling. The fault hooks are isolated to specific types and call sites; they do
not permeate core logic. The `Production` configuration excludes exactly the
listed types/members. The D6.4 variant and qualification tests continue to build
against the `Test` configuration.

**Required proof obligations (executable, not packaging assertions):**

1. Production assemblies contain no prohibited test-only implementation
   (type/member inspection test).
2. Production execution cannot activate synthetic faults through public or
   internal configuration (environment/configuration enumeration test).
3. The installation manifest excludes qualification-only binaries (manifest
   role/kind grammar test).
4. Test variants remain available for deterministic and native qualification
   (D6.4 and qualification test builds succeed under `Test` configuration).
5. Production and test variants cannot be confused by artifact identity or
   provider selection (distinct assembly names/strong names/hashes; verifier deny
   list).
6. Accepted B1–B5 behavior remains correct in the actual production-configuration
   candidate (regression suite runs against `Production` build).
7. Native qualification evidence identifies whether it used a production or
   instrumented test build (qualification record includes `buildConfiguration:
   "Production"` or `"Test"`).

**Implementation boundary (for IMPL-01):** The exact source changes are:
- Add `DefineConstants` condition `GAGAMBA_PRODUCTION` to the M1 csproj for the
  `Release` configuration.
- Wrap each listed type/member in `#if !GAGAMBA_PRODUCTION ... #endif`.
- Update `Directory.Build.props` to define `GAGAMBA_PRODUCTION` for `Release`
  and not for `Debug`/`Test`.
- Add a deterministic verifier test (`InstalledArtifactVerifierTests`)
  that loads the production assembly and asserts the excluded types/members are
  absent.

This design correction does **not** implement the separation; it defines the
exact source changes and testing consequences for the later implementation
commission. If removing test hooks makes existing qualification impossible, a
bounded alternative (separate test adapter assembly) preserves testability
without including hooks in the qualified production closure.

The production install manifest must have a closed role/kind enum and a path
grammar that cannot name these files. The verifier must additionally reject
known test-only artifact hashes and assembly names (`*.Tests`, `*.TestFixture`,
`B2Controller`, `*.Fixture.*`), and must reject any file whose name or metadata
matches the test-only deny list. A test artifact must never become production by
relabeling its descriptor; the manifest/receipt/qualification digest chain is the
control, not the filename.

### 2.6 Gate A result

A complete source-backed inventory exists for the qualified experimental profile.
The application-controlled closure (A1–A16, A30, A31) is fully specified. The
platform baseline (A17–A24) is version-constrained and evidence-bound. However:

1. **Native .NET runtime `dlopen` dependencies** (ICU for globalization, OpenSSL
   `libssl`/`libcrypto`, zlib) are not inventoried in the current table. They are
   loaded by the .NET runtime itself, not by Gagamba code. The design must
   explicitly classify each as either an application-controlled artifact (if
   deployed and pinned by B6) or a qualified platform-baseline component with
   minimum version, resolution source, and refusal behavior. See **B6-Q1-F04**.

2. **Build-only dependency ambiguity**: The existing Release output for
   Owner/Recovery lists `Microsoft.SourceLink.GitHub` → `System.IO.Hashing` in
   `deps.json` and contains `System.IO.Hashing.dll`. This was observed in build
   output, not a fresh publish. The candidate publish must be inspected; the
   design requires either excluding build-only assets from the runtime `deps.json`
   or manifest-pinning every listed runtime asset. See **B6-Q1-F09**.

3. The current implementation has **no install candidate, no installer, no root-owned
   manifest/receipt and no qualified descriptor**; those are B6 implementation
   obligations, not unresolved design ambiguity.

Gate A is satisfied as an inventory for application-controlled artifacts; the
native runtime `dlopen` baseline and the fresh publish dependency inspection are
explicit remediation items before B6-S1 can be claimed.

### 2.7 Native .NET runtime dependency closure (remediation for B6-Q1-F04)

The .NET runtime loads native libraries via `dlopen` that are not part of the
managed `deps.json` asset graph. For the qualified profile, each must be
explicitly classified:

| Native dependency | Load mechanism | Triggering feature | File/library family | Expected resolution source | Deployment controls bytes? | Minimum platform requirement | Mandatory/conditional/excluded | Independent observation | Refusal on unsupported/substituted version |
|---|---|---|---|---|---|---|---|---|---|
| ICU (libicuuc, libicui18n, libicudata) | `dlopen` by `System.Globalization` / `GlobalizationNative` | Non-invariant globalization; culture-aware formatting, collation, casing | ICU 70+ (distro package) | OS package (`libicu70` or distro equivalent) | No (platform baseline) | glibc ≥ 2.43; ICU ≥ 70 | Conditional — only if `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0` (default) | `ldd` on `GlobalizationNative.so`; runtime `Assembly.Load` of culture data; `dotnet --info` globalization mode | Refuse admission if invariant mode is declared but ICU loads, or if ICU version < minimum |
| OpenSSL (libssl, libcrypto) | `dlopen` by `System.Net.Security` / `SslStream` / `HttpClient` | TLS/SSL, HTTPS, certificate validation | OpenSSL 3.0+ (distro package) | OS package (`libssl3`, `libcrypto3`) | No (platform baseline) | OpenSSL 3.0.x compatible | Mandatory for any TLS use | `ldd` on `System.Net.Security.Native.so`; `openssl version`; TLS handshake probe | Refuse if OpenSSL < 3.0 or ABI-incompatible; record exact version in platform baseline |
| zlib (libz) | `dlopen` by `System.IO.Compression` / `DeflateStream` / `GZipStream` | Compression streams | zlib 1.2.11+ (distro package) | OS package (`zlib1g` or equivalent) | No (platform baseline) | zlib ≥ 1.2.11 | Mandatory | `ldd` on `System.IO.Compression.Native.so`; compression round-trip probe | Refuse if zlib missing or version < minimum |
| Native runtime loader (hostfxr, `libhostfxr.so`, `libcoreclr.so`, `libclrjit.so`, `libmscordaccore.so`, `libmscordbi.so`, `libSOS.so`) | `dlopen` by `hostfxr` / runtime startup | All managed execution | .NET 10.0.x shared framework | OS/runtime install (`/usr/lib/dotnet/shared/Microsoft.NETCore.App/<version>/`) | Yes (pinned by B6 as A19/A20) | Exact version from `runtimeconfig.json` | Mandatory | Recursive inventory SHA-256 (A19); `dotnet --list-runtimes`; `ldd` on each `.so` | Refuse on any version/inventory mismatch; exact pin required |

**Globalization mode decision**: The design must choose one:

- **Invariant globalization** (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`): ICU is not
  loaded. The manifest records this mode; admission verifies the environment
  variable is set and that no ICU libraries are loaded by the runtime (observed via
  `ldd`/`/proc/<pid>/maps` on a test execution). This is a bounded platform
  limitation: culture-aware operations throw or use invariant behavior.

- **Non-invariant globalization** (default): ICU is a qualified platform-baseline
  dependency. The manifest records the minimum ICU version; admission checks the
  distro package version and `ldd` evidence. A root-owned ICU update changes the
  loader input without changing the Gagamba manifest; this is accepted as a
  platform-baseline residual and recorded in the qualification limitations.

The design recommends **invariant globalization** for the initial B6 qualification
to eliminate the ICU conditional dependency and simplify the closure. The
globalization mode is its own owner decision (**HD-5**), separate from the
native-host platform-scope decision (**HD-3**). If the owner selects non-invariant
globalization (**HD-5**), the non-invariant row applies and the qualification
record must list the ICU version as a platform-baseline limitation. Invariant
globalization is conditional on demonstrated compatibility and does not alter the
untrusted target's requirements; selecting it does not remove the OpenSSL, zlib or
native-runtime closure obligations.

The same reasoning applies to OpenSSL and zlib: they are mandatory platform-baseline
components with recorded minimum versions. The manifest's `platform` section is
extended with `icuVersion`, `opensslVersion`, `zlibVersion` (or `globalizationMode:
"invariant"`) and admission verifies them.

**Verification plan** (supplements Gate C):

1. **Static inspection**: `ldd` on `GlobalizationNative.so`, `System.Net.Security.Native.so`,
   `System.IO.Compression.Native.so`, and each runtime `.so` in the pinned shared
   framework directory. Record every `NEEDED` entry and its resolved path.
2. **Runtime observation**: Launch a minimal test process under the pinned runtime
   with the cleared environment; capture `/proc/<pid>/maps` and `ldd -v` output.
   Verify no unexpected libraries are loaded.
3. **Controlled substitution tests**: In a test generation, replace the pinned
   runtime directory with a version having a different OpenSSL/ICU/zlib dependency;
   verify admission refuses (`RuntimeDependencyMissing` or `UnqualifiedInstallation`).
4. **Missing-library tests**: Temporarily remove a platform-baseline library (e.g.,
   `libicuuc.so`) in the disposable instance; verify the runtime fails to start
   and the verifier records the missing dependency.
5. **Roll-forward check**: Verify `DOTNET_ROLL_FORWARD=LatestPatch` with the pinned
   runtime directory does not resolve to a different patch with different native
   dependencies.

No single `ldd` or one trivial code path is claimed exhaustive. The qualification
plan (T01, T10, T19) requires both static and runtime evidence across the
supported feature surface.

---

## 3. Threat and trust model

### 3.1 Preserved boundary

- The trusted host, the bootstrap authority, the accepted OS enforcement
  mechanisms and the kernel are trusted.
- The target and every descendant are untrusted, including target-controlled
  files, paths, environment, configuration and process input.
- Target-controlled paths/environment must not select the installation, the
  coordinator, the tool, the runtime or any helper.
- Unsupported or unverified installations fail closed.
- Malicious same-UID host processes and a compromised trusted bootstrap remain
  outside the accepted isolation model (B2/RC-1/security-model).
- No claim is made against root/admin compromise, kernel exploit, rollback,
  exact patch preconditions, snapshot-consistent reads, or instantaneous
  revocation.

### 3.2 Trusted actors

| Actor | Must be trusted to | Not trusted to |
|---|---|---|
| Host administrator / installer (uid 0) | authorize and install the exact artifact set; own the trust anchor | be resisted by this model |
| Trusted controller host | load the provider, enroll inputs, authorize release, hold the one release capability | be confined by Gagamba |
| Recovery process | retain stop/cleanup authority, never release | release |
| Owner process | hold the live capability, arbiter and sole RELEASE writer | survive controller policy decisions it was not given |
| Kernel / OS enforcement | namespaces, cgroup, seccomp, pidfd, fexecve, file ownership | be perfect |

The target is never trusted to report its own confinement, artifact identity or
cleanup. Recovery/owner self-attestation is **detection**, not the root of trust;
the root of trust is the root-owned installation plus the host's authorization.

### 3.3 Same-UID exposure

The design does not rely on same-UID isolation. It avoids *unnecessary* same-UID
exposure by requiring the qualified install root and all ancestors to be
root-owned and not group/other-writable. A user-owned install may exist only as
`Installed/Unqualified` for test/development and can never be selected for
restricted admission. This is stricter than the current
`ExperimentalRuntimePin.Protected` behavior, which accepts a current-uid owner.

---

## 4. Gate B — Installation identity and attestation contract

### 4.1 Required identities

| # | Identity | Representation | Trust role |
|---|---|---|---|
| 1 | Source commit/tree | `source.commit`, `source.tree` in manifest; qualification record | descriptive only; bound by evidence, not by bytes |
| 2 | Build inputs/toolchain | `source.toolchain` (SDK, gcc, Python, distro, kernel, commands, flags) | descriptive; reproducibility not claimed |
| 3 | Exact generated bytes | per-artifact SHA-256 + length; artifact-set digest | integrity; becomes trusted only through the anchor |
| 4 | Role and expected path | closed `role`, relative `path`, `kind`, `mode`, `uid/gid`, `nlink` | identity/placement; manifest comparison |
| 5 | Package/candidate identity | artifact-set digest = SHA-256 of canonical manifest | installation generation ID; selection binding |
| 6 | Protocol/ABI | `manifestSchema`, `implementationId`, `ownerProtocol`, `coordinatorProtocol`, `bootstrap`, header/frame/sequence sizes | compatibility; compared to compiled constants |
| 7 | Platform/RID/arch | `linux-x64`, ELF machine, kernel/glibc minimum | compatibility; host probe |
| 8 | Runtime dependencies | exact .NET host/version/inventory; hostfxr inventory; bwrap version/hash; loader/glibc/libraries | platform baseline; admission check |
| 9 | Security configuration | profile ID/version, seccomp filter hash/length, fixed argv/authority/resources digests | policy identity; compiled/derived, not mutable |
| 10 | Qualification evidence | qualification ID, scope, host, evidence digest, review commit, limitations | authorization decision; never inferred from bytes alone |

Hashes are cryptographic identities. Commit/tree/toolchain/qualification text are
descriptive metadata. The **authorization decision** is the root-owned install
receipt plus the accepted qualification record; neither is supplied by the
artifact directory itself.

### 4.2 Integrity is not authorization

A SHA-256 manifest establishes agreement between bytes and a digest only if the
digest has an independently trustworthy source. A manifest distributed beside
its binaries in a user-writable directory does not establish a trusted
installation.

The design specifies:

- **Who may approve:** the trusted host administrator (uid 0) performing the
  install, and the owner/accepted qualification record for `Qualified` state.
- **Where expected identities originate:** the reviewed, source-bound
  qualification record (artifact-set digest + scope) and the root-owned install
  receipt. For the first B6 qualification, the candidate digest is produced by
  the build, installed as `Installed` (not `Qualified`), exercised by B6-T01–T25,
  and only then recorded as `Qualified` by a root-owned record bound to the same
  digest.
- **How the verifier obtains them:** reads `/opt/gagamba/active`,
  `/opt/gagamba/installations/<digest>/manifest.json`,
  `install-receipt.json` and `/opt/gagamba/qualifications/<id>.json` through
  descriptor-relative, no-symlink opens; it never trusts a path supplied by the
  target or by the artifact directory.
- **Protection:** root ownership, mode 0444/0555, no group/other write, no
  symlinks/hardlinks/setuid/file caps, all ancestors root-owned; the active
  pointer is updated atomically (temp+fsync+rename).
- **Deployment-profile tie:** the manifest names the exact profile ID/version,
  RID, protocol versions, seccomp filter and fixed policy digests; the
  qualification record names the same profile/RID; selection must match all.
- **Unavailable/uncorroborated anchor:** `Unavailable` or `Unqualified`, never
  `Qualified`; no fallback to caller-populated pins, PATH, another runtime or a
  test fixture.

### 4.3 Qualified artifact versus rebuilt artifact

B5 qualification was source-bound but does not prove binary reproducibility. B6
prohibits treating a fresh build from the same commit as byte-identical to a
previously qualified binary.

B6 process:

1. Build the candidate from the exact source commit/tree with the recorded
   toolchain.
2. Compute the canonical manifest and artifact-set digest from the actual bytes.
3. Install the candidate as a new immutable generation (root); record
   `Installed` with the digest. **For a first-time install there is no
   independent expected digest; root computes it from the candidate bytes. This
   is an integrity check only. The receipt records
   `expectedDigestSource="unverified-candidate"` (remediation for B6-Q1-F14).
   The state is `Installed/Unqualified`, not `Qualified`.**
4. Verify the installed closure (same digest, root ownership, modes, exact file
   set, platform baseline).
5. Run the B6 qualification matrix against the **installed** bytes.
6. Record the accepted qualification (root-owned) with the same digest, scope,
   host, evidence digest and limitations. The receipt is updated with
   `expectedDigestSource="qualification-record"`, `qualificationId`, and
   `qualificationRecordSha256`.
7. Descriptor becomes `Qualified` only when install digest == qualification
   digest == selection digest and every mandatory prerequisite is verified.

If installation modifies binaries or security-relevant bytes, the result is a
different artifact set and must be re-qualified. The installer may set
ownership/mode (declared in the manifest) and must not alter content; a post-copy
digest mismatch refuses installation. A rebuild with identical bytes is
acceptable only if the recomputed manifest digest matches; "same source commit"
is not sufficient evidence.

### 4.4 Trust-anchor alternatives

| Criterion | A. Root-owned immutable local install (recommended) | B. Signed artifact manifest | C. Caller-populated pins (current) |
|---|---|---|---|
| Trust-anchor strength | root-owned receipt + qualification record; root is trusted | signature key; verifier and pinned key must be trusted | none: caller asserts hashes |
| Target substitution risk | target cannot write root-owned tree | target cannot forge signature, but can replace a user-owned verifier/key | caller can point at anything |
| Verification-to-use | strong: root-owned paths + fd hashing + post-start echo | strong if verifier anchors key; otherwise same path race | weak: path/hash then path-spawn |
| WSL2 practicality | needs `sudo` in the disposable instance for install | no root for install, but key management and verifier bootstrap | already works, but insecure |
| Linux portability | `/opt/gagamba` standard; root-owned ancestors | key distribution/rotation; not local-first | ad hoc paths |
| Dependency management | one manifest, one verifier | manifest + signature + key | scattered pins |
| Recovery continuity | generation identity in handshake; no cross-generation adoption | same | no generation identity |
| Operational complexity | low: one install script + one verifier | medium: signing/key/verification | low but unqualified |
| Testability | deterministic + native negative tests | deterministic signature tests + native | already tested but inadequate |
| Evidence | manifest/receipt/qualification digests | signature/key evidence | caller JSON |
| Maintenance | re-pin/re-qualify on update | key rotation + re-sign | unbounded |

**Recommendation: A.** It is the smallest mechanism that closes B3-F04/F06 without
a signing service or package manager, and it matches the accepted trusted-host
model. B is a future option if multi-host distribution requires it; it is not a
hidden prerequisite. C is rejected for production installation trust; it remains
only as an unqualified test/development profile and must be refused by the
installed provider.

### 4.5 Canonical manifest and receipt

The manifest is deterministic UTF-8 JSON with a fixed field order, no
insignificant whitespace, sorted artifact entries (role, then path) and strict
types. It contains no self-digest field. The artifact-set digest is SHA-256 over
the canonical manifest bytes. Proposed shape:

```
{
  "schemaVersion": 1,
  "profileId": "<bounded id>",
  "profileVersion": "1",
  "source": { "commit": "<40 hex>", "tree": "<40 hex>",
              "toolchain": { "dotnet": "...", "gcc": "...", "python": "...",
                             "distro": "...", "kernel": "..." },
              "buildCommandDigest": "<64 hex>" },
  "platform": { "rid": "linux-x64", "architecture": "x86_64",
                "kernelMin": "...", "glibcMin": "2.43",
                "dotnetRuntime": "<exact>", "dotnetHostSha256": "<64 hex>",
                "runtimeInventorySha256": "<64 hex>",
                "hostFxrInventorySha256": "<64 hex>",
                "bubblewrap": { "path": "/usr/bin/bwrap", "version": "0.11.1",
                                "sha256": "<64 hex>", "package": "..." },
                "loaderSha256": "<64 hex>",
                "requiredFeatures": ["cgroup2","openat2","pidfd","SO_PEERPIDFD",
                                     "subreaper","userns","seccomp"] },
  "protocol": { "manifestSchema": 1, "implementationId": "linux-m1-native-transaction",
                "ownerProtocol": 1, "coordinatorProtocol": 1, "bootstrap": 1,
                "coordinatorHeaderBytes": 96, "releaseFrameBytes": 128,
                "releaseSequence": 1 },
  "policy": { "profileId": "ae4-linux-fixed-fixture-v1",
              "seccompFilterSha256": "<64 hex>", "seccompFilterLength": 672,
              "bubblewrapArgsSha256": "<64 hex>",
              "authoritySha256": "<64 hex>", "resourcesSha256": "<64 hex>" },
  "artifacts": [
    { "role": "owner-entry", "kind": "managed", "path": "owner/Owner.dll",
      "sha256": "<64 hex>", "length": 12345, "mode": "0444",
      "uid": 0, "gid": 0, "nlink": 1, "rid": "linux-x64",
      "buildIdentity": "...", "required": true,
      "consumer": "owner", "survivesController": true }
  ]
}
```

`install-receipt.json` (root-owned, 0444):

```
{ "schemaVersion": 1, "artifactSetSha256": "<64 hex>",
  "manifestFileSha256": "<64 hex>", "installedByUid": 0,
  "installedAtUtc": "...", "candidatePath": "...", "installerVersion": "...",
  "expectedDigestSource": "qualification-record|unverified-candidate|<explicit root input>",
  "qualificationId": "<qualification-id-or-null>",
  "qualificationRecordSha256": "<64-hex-or-null>" }
```

- `expectedDigestSource`: `"qualification-record"` for a qualified installation
  (digest from the accepted qualification record), `"unverified-candidate"` for a
  first-time install where root computes the digest from the candidate bytes
  (remediation for **B6-Q1-F14**), or an explicit root-supplied value.
- `qualificationId` and `qualificationRecordSha256`: **Required for `Qualified`
  state** (remediation for **B6-Q1-F13**). The verifier selects the qualification
  record by matching `qualificationId` (not directory search) and verifies its
  SHA-256 equals `qualificationRecordSha256`. If either is null/empty or the
  record hash mismatches, the state is `Unqualified`.

`active` (root-owned, 0444) contains only `<artifactSetSha256>\n`.
`qualifications/<qualificationId>.json` (root-owned, 0444) contains the accepted record:
`qualificationId`, `artifactSetSha256`, `profileId/version`, `rid`, `scope`,
`host`, `sourceCommit/tree`, `evidenceSha256`, `reviewCommit`, `acceptedAtUtc`,
`limitations[]`, `buildConfiguration` (`"Production"` or `"Test"`).

The verifier recomputes the manifest digest from the installed bytes and compares
it to the directory name, `active`, the receipt and (for `Qualified`) the
qualification record. A file in the generation dir that is not in the manifest,
or a manifest entry with no file, refuses.

**Qualification record association (remediation for B6-Q1-F13)**: The receipt's
`qualificationId` and `qualificationRecordSha256` provide an explicit, deterministic
binding.

**`qualificationId` grammar (Q2-F06).** `qualificationId` is a bounded canonical
identifier, not a free-form string:

- **Permitted characters (ASCII):** `A-Z`, `a-z`, `0-9`, `.`, `_`, `-`.
- **Length:** 1–128 characters inclusive; an empty value is rejected.
- **Case:** case-sensitive, with no case folding, Unicode normalization or alias.
  The canonical form is the exact ASCII bytes; two identifiers differing only in
  case are distinct.
- **Rejected:** any character outside the permitted set (including the path
  separators `/` and `\`, NUL and control characters, whitespace and any non-ASCII
  character); the traversal sequences `.` and `..` as the entire value; a leading
  or trailing `.`; and any empty or over-length value.
- **Safe path construction:** the verifier validates the identifier against this
  grammar before touching the filesystem, then opens exactly one record path
  `/opt/gagamba/qualifications/<qualificationId>.json` with the same
  descriptor-relative `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`
  policy used for installed artifacts. Because the identifier is a single validated
  path component, there is no path escape and no directory enumeration; a malformed
  identifier is a refusal, not a search.
- **Exactly one association:** the grammatically valid identifier selects exactly
  one record path, which must hash-match the receipt's
  `qualificationRecordSha256`. There is no enumeration, wildcard or
  latest-file-wins fallback. An absent, extra, ambiguous, malformed or
  unauthenticated record yields `Unqualified`.

The verifier:

1. Reads `active` → gets `artifactSetSha256`.
2. Reads the generation's `install-receipt.json` → gets `qualificationId` and
   `qualificationRecordSha256`.
3. Validates `qualificationId` against the grammar above, then opens the single
   `/opt/gagamba/qualifications/<qualificationId>.json` (no directory
   enumeration, no latest-file-wins).
4. Verifies the record's `artifactSetSha256` matches `active`.
5. Verifies the record's SHA-256 equals `qualificationRecordSha256`.
6. Verifies `profileId/version`, `rid`, `scope` match the manifest.
7. Verifies `buildConfiguration` is `"Production"` for a qualified production
   installation.
8. If any check fails → `Unqualified`. If `qualificationId` is null/empty →
   `Unqualified` (fresh install is `Installed/Unqualified`, not `Qualified`).

**Required states and deterministic behavior:**

| State | Behavior |
|---|---|
| Fresh installation, no qualification record | `Installed/Unqualified`; receipt has `expectedDigestSource="unverified-candidate"`, `qualificationId=null` |
| Exactly one valid associated accepted record | `Qualified` if all digest/profile/rid/scope/buildConfiguration checks pass |
| Missing referenced record (`qualificationId` not found) | `Unqualified`; receipt points to non-existent record |
| Record hash mismatch (`qualificationRecordSha256` ≠ actual record SHA-256) | `Unqualified`; record tampered or replaced without receipt update |
| Multiple candidate records (not possible with explicit `qualificationId`) | N/A — selection is by `qualificationId`, not search |
| Stale/superseded qualification (different `qualificationId`) | `Unqualified`; receipt points to specific record; old records remain but are not selected |
| Record for another digest | `Unqualified`; record `artifactSetSha256` ≠ `active` |
| Record for another RID, protocol or profile | `Unqualified`; manifest/profile/RID mismatch |
| Unaccepted producer-only evidence | `Unqualified`; acceptance requires a root-owned (uid 0) 0444 record selected by the receipt-bound `qualificationId` + `qualificationRecordSha256`; a producer-generated report or a bare `acceptedAtUtc` field cannot approve itself |
| Conflicting qualification metadata | `Unqualified`; any field mismatch refuses |

A self-generated test report cannot automatically approve itself. Acceptance is a
separate trusted owner/operator action performed by root: it must reference an
accepted qualification decision and the exact artifact-set identity
(`artifactSetSha256`), and it writes the root-owned 0444 record selected by
`qualificationId`. Root ownership and mode of the record are the verifier's
acceptance basis; a timestamp, a receipt or a producer-generated report cannot
approve itself. `qualify-install.sh` is a future implementation proposal for that
action, not an implemented or independently qualified authority. If the record
cannot be authenticated against the trusted installation policy — that is, it is
not root-owned 0444, or its identifier is malformed, or its
`qualificationRecordSha256` does not match the receipt, or its
digest/profile/RID/scope/build do not match the manifest — verification returns
`Unqualified`.

### 4.6 Installed-provider descriptor

The descriptor is a passive, typed, versioned value. B6 proposes **descriptor
version 2**, additive to version 1; version 1 continues to mean `Unavailable`.

**Minimal public surface (remediation for B6-Q1-F08 / HD-2)**: The review
confirmed that the proposed v2 surface is larger than B6 needs. B6 requires only
admission/refusal reporting fields; per-file `Artifacts`, `Verification` and
`Platform` inventories duplicate internal manifest data and should remain
internal until a real consumer exists (B7). The minimal public v2 fields are:

Proposed additions/changes (all read-only properties; no public factory and no
authority):

- `InstallationState`: `Unavailable` / `Unqualified` / `Qualified` (existing enum).
- `RefusalReason`: existing `RestrictedRefusalReason` plus a bounded
  `InstallationDiagnostic` (`ArtifactIntegrityMismatch`,
  `InstallationGenerationChanged`, `ProtocolIncompatible`,
  `RuntimeDependencyMissing`, `UntrustedMetadata`, `PartialInstallation`).
- `InstallationId` = artifact-set digest hex (same value as
  `ArtifactSetSha256`, retained for clarity).
- `ProfileId`, `ProfileVersion`, `BootstrapVersion`.
- `RuntimeIdentifier`, `Architecture`.
- `Protocol`: `ManifestSchemaVersion`, `OwnerProtocolVersion`,
  `CoordinatorProtocolVersion`, `CoordinatorHeaderBytes`,
  `ReleaseFrameBytes`, `ReleaseSequence`.
- `QualificationId`, `QualificationScope`, `QualificationEvidenceSha256`,
  `QualificationLimitations` (bounded list).
- `HostPrerequisites`: verified booleans for cgroup2, openat2, pidfd,
  `SO_PEERPIDFD`, subreaper, userns, seccomp, unprivileged-empty-caps.
- `CaptureReadbackAvailable` (A30 component presence).

**Internal verifier diagnostics** (not public): `Artifacts` (full role inventory),
`Verification` (`ManifestSha256`, `ReceiptSha256`, `VerifiedGeneration`),
`Platform` (full `DotnetRuntimeVersion`, `BubblewrapVersion`, `KernelMin`,
`GlibcMin`, `icuVersion`/`globalizationMode`, `opensslVersion`, `zlibVersion`).
These remain in the internal `InstalledRestrictedProviderDescriptor` used by the
verifier and tests; they are not exposed in the public `RestrictedProviderDescriptor`.

Semantics:

- `Qualified` requires every mandatory field non-null, install digest ==
  qualification digest == selection digest, protocol constants equal compiled
  values, RID/arch match, and all prerequisites true.
- Contradictory fields (e.g., `Qualified` with empty artifacts, duplicate
  roles/paths, all-zero digest, length <= 0, unsorted list, mode/uid mismatch)
  refuse; never "best effort".
- Stale fields (qualification commit/tree not matching install source, scope not
  covering this RID/profile, generation not active) refuse as `Unqualified`.
- Missing fields or a missing/unreadable anchor yield `Unavailable` or
  `Unqualified`; never optimistic `Qualified`.
- A descriptor alone never grants `RELEASE`, creates a preparation capability,
  enrolls sources or changes provider selection. `CreateOperation` re-verifies.
  `Describe()` is a report, not an admission token.

### 4.7 Gate B result

A coherent root of trust (root-owned install receipt + accepted qualification
record), exact artifact-set binding and conservative descriptor semantics are
defined. The design does not assume cryptographic signatures and does not accept
self-reported hashes.

---

## 5. Gate C — Verification-to-execution integrity

### 5.1 The gap

Verification produces a digest; execution resolves a path, starts a host and
loads managed/native dependencies. A hash check followed by loading an
independently resolved file is not automatically sufficient.

### 5.2 Selected mechanism

The selected mechanism is **root-owned immutable generation directories +
descriptor-relative fd verification + cleared loader environment + post-start
identity echo**, with a documented root-only residual race. It does not claim an
atomic or race-free guarantee beyond what the OS operations establish.

1. **Immutable generation directory.** `/opt/gagamba/installations/<digest>` and
   every ancestor to `/` are root-owned, not group/other-writable, and contain no
   symlinks. Files are 0444 (data) or 0555 (executables), `nlink == 1`, no
   setuid/setgid, no file capabilities. The target cannot write them; a non-root
   trusted process cannot replace them.

   **Ancestor predicate correction (remediation for B6-Q1-F06)**: The existing
   `ExperimentalRuntimePin.AncestorsProtected` accepts a root-owned ancestor with
   group/other write when the sticky bit is set (`owner==0 && (mode&0x200)`), i.e.
   `/tmp`-like directories. The installed closure **must drop that exception**.
   The install verifier implements a stricter predicate: every ancestor directory
   to `/` must be root-owned (`uid==0`) and **not** group-writable (`mode&020==0`)
   and **not** other-writable (`mode&002==0`). The sticky-bit exception is
   removed. This is enforced at admission (V04) and verified in T01/T04.
2. **Descriptor-relative verification.** Open the generation directory by
   `openat2(RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS,
   O_DIRECTORY|O_NOFOLLOW)`; open each artifact relative to it with
   `O_RDONLY|O_NOFOLLOW|O_CLOEXEC` and the same resolve policy. Hash from the fd,
   not by path. `fstat` before and after hashing; require identical
   device/inode/mode/uid/gid/size/mtime/ctime and `nlink == 1`. This reuses and
   extends `PreparationInterop.RequiredResolve` and `SafeCopyIn`'s proven
   acquisition pattern.
3. **Exact set.** Recursively enumerate the generation directory; every regular
   file must appear exactly once in the manifest, and every manifest entry must
   exist. Reject extra files, directories not in the layout, symlinks, hardlinks,
   devices, FIFOs, sockets and sparse/abnormal objects. Bound the count and
   total bytes.
4. **Active generation.** Read `/opt/gagamba/active` (root-owned 0444, 65 bytes)
   through a no-symlink open; parse exactly 64 hex + newline. The generation
   directory name, manifest digest, receipt and (for `Qualified`) qualification
   record must agree. A partial or concurrent install is never named by `active`.
5. **Platform baseline.** Verify `/usr/bin/bwrap` (root-owned 0755, no caps,
   exact SHA-256/version), the exact .NET host/runtime/hostfxr inventory, the
   loader/glibc version, `/etc/ld.so.preload` empty/absent, cgroup2 and the
   required kernel features. Refuse on any mismatch.
6. **Spawn without ambient resolution.**
   - The recovery/owner processes are started from the pinned absolute `dotnet`
     host path with a cleared environment and the fixed five variables. The
     legacy `"/usr/bin/dotnet"` and `FileName = "dotnet"` paths are refused for
     the qualified profile.
   - `PATH` is never used. `DOTNET_ROOT`, `DOTNET_MULTILEVEL_LOOKUP=0`,
     `DOTNET_EnableDiagnostics=0`, fixed roll-forward; `DOTNET_ADDITIONAL_DEPS`,
     `DOTNET_SHARED_STORE`, `DOTNET_STARTUP_HOOKS`, `COREHOST_TRACE*`, `LD_*`,
     `DYLD_*` and all other ambient variables are absent.
   - Bubblewrap is opened and hashed by fd and executed via
     `/proc/self/fd/11`; the pathname is not used after open.
   - Coordinator and seccomp filter are copied from verified source **paths with
     claimed digests** by `SafeCopyIn`; the coordinator is bound read-only at
     `/coordinator`, the filter is a sealed memfd at fd 6.
     **Correction (remediation for B6-Q1-F03)**: The design previously claimed
     "copied from verified source fds." The current `CopyPlan` carries a
     `SourceRootPath` string; `SafeCopyIn` opens the source root by path with
     `O_NOFOLLOW` and `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`,
     checks regular file, size stability, digest, read-only reopen, device/inode/
     mode/link count and dense accounting. The **content is claimed-digest bound**,
     but the source root is reopened by path, not retained as an fd. The
     staging-parent fd (`StagingParentAuthority`) is retained, not the source
     root. A root actor could replace the source root path after verification;
     non-root cannot. The wording is corrected to "claimed-digest path copies
     under a verified staging parent." The implementation may add a
     `SourceRootAuthority` fd to `CopyPlan` in IMPL-01; the design does not
     require it for the trust claim.
7. **Post-start identity echo.** After load but before any native authority, the
   owner and recovery compute a hash of their own role closure (entry, deps,
   runtimeconfig, `Gagamba.Execution.Linux.M1.dll`, `Gagamba.Execution.dll`),
   hash every loaded assembly with a non-empty `Location` and verify it is either
   in their role closure or under the pinned shared-runtime directory with the
   pinned inventory, and compare the installation identity/protocol versions with
   the controller's. They send a bounded `InstallationEcho` (32-byte digest +
   u16 versions + role bit) before `WriterReady`/`StartAuthority` (owner) or
   `Ready` (recovery). A mismatch aborts before domain creation, staging or
   release. This is detection, not the trust anchor.
8. **Before release.** The controller re-reads `active` and requires the
   operation's generation to still be active. If it changed, the controller does
   not send `ReleaseRequest`; it submits the existing stop path and records the
   result. A deployment change does not grant release.

### 5.3 Required case analysis

| Case | Threat | Mechanism / result | Residual |
|---|---|---|---|
| Symlink substitution | link to another file | no-symlink resolve; `lstat` regular; `nlink==1`; root-owned dir | root actor |
| Path traversal / absolute path | escape generation | manifest grammar (relative, no `.`/`..`, single components); `RESOLVE_BENEATH` | none within model |
| Parent-directory replacement | swap dir | open dir fd first, verify each relative open; root-owned ancestors | root actor |
| In-place replacement / truncation | modify during hash | hash from fd; before/after `fstat` identity+size+mtime+ctime; `SafeCopyIn` size stable + claimed digest | root actor |
| Atomic rename after verify | swap name | root-owned dir prevents non-root rename; entry fd retained; post-start echo | root actor; documented |
| Executable substitution between verify/spawn | swap owner/recovery | root-owned path + post-start role-closure hash; bwrap fexecve fd | root actor |
| Managed assembly substitution during load | resolved other copy | cleared env; exact deps assets; no shared store/additional deps; loaded-assembly `Location` check; post-start closure hash | host runtime itself is trusted baseline |
| Native library substitution | `LD_PRELOAD`/lib path | env cleared; no `LD_*`; root-owned libs; loader/preload check; bwrap fd | root actor |
| Environment loader redirection | `DOTNET_*`, `LD_*`, `PATH` | environment cleared; fixed variables; no PATH; legacy path refused | none |
| Roll-forward/resolution change | OS patch / multiple runtimes | exact runtime directory + `Environment.Version` + inventory digest; fixed roll-forward; refuse mismatch | OS patch requires re-pin/requalify |
| Alternate config/dependency files | `.runtimeconfig.dev.json`, extra deps | exact file set; no extra JSON; deps runtime assets == manifest | none |
| Target-writable paths | target writes install | install not bind-mounted; root-owned; target sees only read-only staged objects | none |
| Partial/concurrent install | mixed generation | immutable generation + atomic active pointer + exact set; incomplete never active | none |
| Artifact changed after verification, before release | root update | controller re-reads active before RELEASE; owner echo; no cross-generation release | root actor; documented |
| Artifact changed during execution | root update | no kill on manifest mismatch alone; existing stop/lifeline authority; recovery retains identity; restart refuses adoption | root actor; Unknown cleanup possible |

### 5.4 Failure after startup, before RELEASE

If an artifact mismatch is detected after the owner/recovery started but before
target RELEASE:

- Refuse preparation if HELD is not yet accepted.
- Invalidate pending RELEASE authority: the controller must not send
  `ReleaseRequest`; if a release call is in flight, the owner's arbiter still
  checks its capability/manifest/deadline and the controller records the
  mismatch; no second attempt.
- Perform the existing identity-checked native safety stop (C2/Policy A paths)
  where a stop is already obligated; do not invent a new deadline.
- Preserve recovery capability: recovery keeps its stop/cleanup authority and
  its installation identity; it never gets release.
- Return a bounded failure or `Unknown`; never `Prepared`/`Released`.
- Preserve cleanup uncertainty (20 Confirmed / 12 Unknown categories unchanged).

### 5.5 Gate C result

Verified artifact identity remains meaningful through the actual execution and
loading path because the install tree is root-owned and immutable for the
trusted uid, files are verified from fds, the loader environment is cleared, and
each surviving actor echoes the identity before authority. The residual race is
a root/admin or kernel compromise replacing bytes; that actor is outside the
accepted model and is documented, not hidden.

---

## 6. Gate D — Multi-process and recovery installation consistency

### 6.1 Installation identity propagation

Add a bounded `InstallationIdentity` to the recovery bootstrap and owner
handshake:

```
InstallationIdentity {
  ArtifactSetSha256: 32 bytes
  OwnerClosureSha256: 32 bytes
  RecoveryClosureSha256: 32 bytes
  CoordinatorSha256: 32 bytes
  SeccompFilterSha256: 32 bytes
  RuntimeInventorySha256: 32 bytes
  ManifestSchema: u16, OwnerProtocol: u16, CoordinatorProtocol: u16,
  Bootstrap: u16, ProfileId/ProfileVersion: bounded ASCII,
  Generation: 32 bytes (same as ArtifactSetSha256), Qualified: bool
}
```

- **Controller:** verifies the install, builds the identity from the verified
  manifest, passes it in `RecoveryConfiguration` (version 2; version 1 is refused
  for the qualified profile).
- **Recovery:** verifies its own role closure and the identity before
  associating; compares with its own loaded location; sends `InstallationEcho`.
- **Owner:** verifies its own role closure and runtime before acquiring the
  journal writer/allocating domain authority; sends `InstallationEcho` before
  `StartAuthority`.
- **Controller:** compares both echoes; any mismatch aborts and requests stop.
  No domain, staging, capability or RELEASE exists on mismatch.

This adds no release authority. The owner still mints the capability locally and
is the sole RELEASE writer; recovery never receives it.

### 6.2 Installation-identity handshake wire specification (remediation for B6-Q1-F07)

The following defines the exact wire-level contract for the installation-identity
verification exchange. Message IDs are selected from the existing
`OwnerProtocolMessageType` / `RecoveryMessageType` namespace after verifying
available values (current max: `OwnerProtocolMessageType = 7`,
`RecoveryMessageType = 5`).

**Protocol version requirement**: `RecoveryConfiguration` version 2 (qualified
profile). Version 1 has no identity fields and is refused.

#### 6.2.1 Message types and numeric identifiers

| Message | Numeric ID | Direction | Channel | Max encoded size |
|---|---|---|---|---|
| `RecoveryConfigV2` (extends `RecoveryConfiguration`) | N/A (bootstrap JSON) | Controller → Recovery | Recovery stdin (fd 0, length-prefixed JSON) | 8 KiB |
| `InstallationEchoRequest` (Recovery→Controller) | `RecoveryMessageType = 6` (new) | Recovery → Controller | Recovery stdout (fd 1, framed) | 512 bytes |
| `InstallationEchoResponse` (Controller→Recovery) | `RecoveryMessageType = 7` (new) | Controller → Recovery | Controller stdin (fd 0, framed) | 512 bytes |
| `OwnerInstallationEcho` (Owner→Controller) | `OwnerProtocolMessageType = 8` (new) | Owner → Controller | Owner stdout (fd 1, framed) | 512 bytes |
| `OwnerStartAuthority` (Controller→Owner) | `OwnerProtocolMessageType = 3` (existing) | Controller → Owner | Controller stdout (fd 1, framed) | 1 KiB |

**Framing**: All post-bootstrap messages use the existing length-prefixed frame
(4-byte big-endian length + payload). The coordinator/seccomp filter fds are
passed via the existing fd-passing mechanism (fd 6 for filter, fd 7 for
coordinator path).

#### 6.2.2 `RecoveryConfigV2` payload (controller → recovery, at spawn)

```json
{
  "Version": 2,
  "Recovery": { "PinnedExecutable": { ... } },
  "Owner": { "PinnedExecutable": { ... } },
  "Integrated": {
    "Runtime": { "HostPath": "...", "RuntimeDirectory": "...", "HostFxrDirectory": "..." },
    "InstallationIdentity": {
      "ArtifactSetSha256": "<64 hex>",
      "OwnerClosureSha256": "<64 hex>",
      "RecoveryClosureSha256": "<64 hex>",
      "CoordinatorSha256": "<64 hex>",
      "SeccompFilterSha256": "<64 hex>",
      "RuntimeInventorySha256": "<64 hex>",
      "ManifestSchema": 1,
      "OwnerProtocol": 1,
      "CoordinatorProtocol": 1,
      "Bootstrap": 1,
      "ProfileId": "ae4-linux-fixed-fixture-v1",
      "ProfileVersion": "1",
      "Generation": "<64 hex>",
      "Qualified": true
    }
  }
}
```

The `InstallationIdentity` is **required** when `Integrated` is non-null.
`Qualified` must be `true` for the qualified profile. The recovery validates
its own five-file closure hash matches `RecoveryClosureSha256` and its
`Assembly.GetEntryAssembly().Location` hash matches the manifest entry before
proceeding.

#### 6.2.3 `InstallationEchoRequest` (recovery → controller, after load, before `Ready`)

Payload (binary, little-endian):

| Offset | Size | Field |
|---|---|---|
| 0 | 32 | `ArtifactSetSha256` (recovery's computed installation digest) |
| 32 | 32 | `RecoveryClosureSha256` (recovery's computed role closure hash) |
| 64 | 2 | `OwnerProtocol` (u16) |
| 66 | 2 | `CoordinatorProtocol` (u16) |
| 68 | 2 | `Bootstrap` (u16) |
| 70 | 1 | `Role` (0 = recovery, 1 = owner) |
| 71 | 1 | `Qualified` (0/1) |
| 72 | 4 | Reserved (zero) |
| 76 | — | Optional: `ProfileId`/`ProfileVersion` UTF-8, length-prefixed (max 64 bytes each) |

Total: ≤ 256 bytes. The controller verifies every field matches its expected
identity from the manifest. `Role` must be 0. Mismatch → controller sends
`InstallationEchoResponse` with `Success=0` and aborts.

**Projection from `InstallationIdentity` to the echo payload (Q2-F09).** The echo
carries the subset of the §6.1 `InstallationIdentity` struct that the receiver needs
to decide the handshake; it is not a full re-transmission of that struct. The
mapping is explicit:

- `ArtifactSetSha256` covers both `InstallationIdentity.ArtifactSetSha256` and
  `InstallationIdentity.Generation` (the design requires
  `Generation == ArtifactSetSha256`).
- Offset 32 carries the **sender's role closure**: `RecoveryClosureSha256` when
  `Role = 0` (this request) or `OwnerClosureSha256` when `Role = 1` (the
  `OwnerInstallationEcho` of §6.2.5). The receiver selects the expected value for
  the role it expects and never trusts the peer's own claim.
- `OwnerProtocol`, `CoordinatorProtocol` and `Bootstrap` map to the like-named
  `InstallationIdentity` fields.
- `Qualified` maps to `InstallationIdentity.Qualified`.
- The optional tail `ProfileId`/`ProfileVersion` maps to the like-named fields.
- `InstallationIdentity.ManifestSchema` is **not** carried in the echo: the
  manifest-schema constant is compared against the compiled value at admission,
  and the handshake is not the schema-version check point. `CoordinatorSha256`,
  `SeccompFilterSha256` and `RuntimeInventorySha256` are likewise not carried
  separately; they are covered by the `ArtifactSetSha256` closure and the
  role-closure checks.

This projection is a documented mapping only. It adds no message field, changes no
message id, ordering, channel or authority, and does not make either peer the source
of the other's expected identity. If a future implementation needs a field not in
this projection, that is a new wire decision, not an implied one.

#### 6.2.4 `InstallationEchoResponse` (controller → recovery)

Payload (binary):

| Offset | Size | Field |
|---|---|---|
| 0 | 1 | `Success` (0/1) |
| 1 | 1 | `MismatchReason` (enum: 0=none, 1=digest, 2=closure, 3=protocol, 4=generation, 5=qualified) |
| 2 | 2 | Reserved (zero) |
| 4 | 32 | `ExpectedArtifactSetSha256` (controller's expected value) |

If `Success=0`, recovery logs the mismatch, performs native safety stop (C2/Policy
A), and exits without `Ready`. If `Success=1`, recovery sends `Ready` (existing
`RecoveryMessageType = 1`).

#### 6.2.5 `OwnerInstallationEcho` (owner → controller, after load, before `StartAuthority`)

Same binary structure as `InstallationEchoRequest`, with `Role = 1` (owner).
The owner computes its own role closure hash (`OwnerClosureSha256`) and sends it
after verifying its loaded assemblies against the manifest (same check as
recovery). The controller verifies `OwnerClosureSha256` matches the manifest.
Mismatch → controller does not send `OwnerStartAuthority`; aborts and requests
stop.

#### 6.2.6 Expected identity provenance

The **receiver has a trusted expected identity independently of the peer's
claim**. The controller's expected identity comes from the verified manifest
(recomputed from installed bytes, bound to `active` and root-owned receipt). The
recovery's expected identity comes from `RecoveryConfigV2.Integrated.InstallationIdentity`
(which the controller built from the same manifest). The owner's expected
identity comes from the same source via the controller's `OwnerStartAuthority`
payload (which includes the installation identity). An actor reporting its own
digest cannot qualify itself; the receiver compares against its independent
expected value.

#### 6.2.7 Recovery restriction

The handshake **must not grant recovery RELEASE authority**. No new bearer
capability is derived solely from an installation digest. The recovery's
`InstallationEcho` is detection only; it never receives a capability, writer, or
release frame. Cross-generation reconciliation fails conservatively: if the
controller's `active` generation changes, the recovery (started under the old
generation) will echo the old identity; the controller (now on new generation)
will mismatch and refuse. The surviving recovery retains its stop/cleanup
authority under the old generation; no release is possible.

#### 6.2.8 Ordering and timeout behavior

1. Controller spawns recovery with `RecoveryConfigV2` (fd 0).
2. Recovery loads, verifies its closure, sends `InstallationEchoRequest` (fd 1).
3. Controller verifies, responds `InstallationEchoResponse` (fd 0).
4. Recovery receives response; if `Success=1`, sends `Ready` (fd 1).
5. Controller spawns owner with `OwnerStartAuthority` including installation identity.
6. Owner loads, verifies its closure, sends `OwnerInstallationEcho` (fd 1).
7. Controller verifies; if success, proceeds to HELD; if mismatch, aborts.

**Timeouts**: Each exchange has a 5-second deadline (consistent with existing
bootstrap timeouts). Timeout → mismatch → abort. No retries.

**Replay/duplication**: Each echo includes the full `ArtifactSetSha256` and
`Generation` (same value). A replay from a previous generation fails the
`Generation` check. The controller tracks which echoes it has answered for the
current operation; duplicate echoes are refused.

**Cancellation**: If the controller dies before sending `OwnerStartAuthority`,
the owner never receives it and exits on lifeline loss (existing behavior). The
recovery retains its identity and stop authority.

**Cleanup on mismatch**: All processes exit; no domain created; no staging
committed; journal records the mismatch; cleanup follows existing C2/Policy A
paths.

### 6.3 Required properties

| # | Property | Mechanism |
|---|---|---|
| 1 | Controller cannot enroll an arbitrary executable as owner/recovery | install manifest + role closure + post-start echo; no caller paths for qualified profile |
| 2 | Recovery uses authorized closure if controller exits | recovery started from install generation; retains its own identity; no controller re-supply |
| 3 | Owner receives only its intended role/capabilities | existing bootstrap roles + no capability transfer; identity echo does not add authority |
| 4 | Recovery never acquires RELEASE | `RecoveryObligation.ReleaseAuthority => false`; no writer/capability in recovery |
| 5 | Reconnection cannot change generation | `Reconnect` refused; no new association; identity fixed at startup |
| 6 | Stale/incompatible controller cannot adopt owner | execution/generation/installation echo + protocol versions; mismatch refuses |
| 7 | Mixed protocol versions cannot silently interoperate | compiled constants compared at every handshake; mismatch refuses |
| 8 | Replaced helper cannot be used by filename | manifest role/hash; no PATH; coordinator/filter staged from verified source paths with claimed digests under a verified staging parent (no retained source-root fd); Bubblewrap executes the pinned coordinator through `/proc/self/fd/11` |
| 9 | Missing native/managed dependency prevents admission | exact deps/runtime asset set; missing/extra refuses |
| 10 | Controller failure does not cause uncontrolled resolution | recovery/owner already loaded; no PATH; restart uses install generation, not ambient paths |

### 6.4 Recovery after controller death

Facts that must survive controller death:

- The recovery process's own installation identity, role closure hashes, runtime
  pin and generation (in memory and retained fds).
- The owner's identity echo and installation generation (in the owner; recovery
  observes only stop/lifetime, not release).
- The durable `StopRecord` deadline, boot/epoch/frequency, execution/lease
  generation and journal state (existing B5 contract).
- The recovery's retained domain/staging handles and pidfd.

Recovery does **not** reconstruct release authority from disk paths, PIDs,
passive identifiers or a new installation. It may inspect/reconcile/stop under
the existing contract; all B5 deadline/boot/clock restrictions remain.

### 6.5 Deployment generations

**Policy: side-by-side immutable generations; no cross-generation adoption.**

- A new generation is installed as a new directory; `active` is updated
  atomically for new admissions.
- Existing executions keep their generation in memory and retained handles; the
  controller records it in the operation; recovery/owner echo it.
- A controller cannot adopt an owner/recovery from another generation; a
  reconnect cannot change it.
- **Correction (remediation for B6-Q1-F11)**: The design previously described a
  "recovery restart after active changed" branch as if it were a current
  mechanism. **No reconnect listener or post-controller recovery relaunch exists
  today.** If the controller dies and a recovery for the old execution must be
  restarted after `active` changed, the new recovery starts under the new
  generation and refuses the old owner (identity mismatch); the owner self-stops
  on lifeline loss. There is no disk-only reconstruction of release authority.
  This hypothetical future support is identified separately and is not part of B6.
- Old generations are not deleted automatically. The removal script refuses the
  active generation and refuses when a process still holds/maps its files; if
  that cannot be determined, removal is refused (Unknown).
- No rolling upgrade, migration, persistent daemon or new journal schema is
  introduced. The installation generation is not written into the v3 journal in
  B6; if a future policy requires per-execution generation durability, it needs
  a versioned schema decision and review.

This is the smallest policy that preserves no-substitution and the accepted
recovery contract. It may lose recovery continuity after a controller crash plus
a deployment change; that is a conservative `Unknown`, not a silent adoption.

### 6.6 Gate D result

All surviving actors agree on one installed identity/generation; no new RELEASE,
reconnect or recovery authority is created.

---

## 7. Gate E — Native packaging and local installation

### 7.1 Build inputs and candidate construction

Proposed build (not executed by this commission):

```
dotnet publish src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj \
  -c Release -r linux-x64 --self-contained false \
  -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false \
  -o <candidate>/owner
dotnet publish src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj \
  -c Release -r linux-x64 --self-contained false \
  -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false \
  -o <candidate>/recovery
dotnet publish src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj \
  -c Release -r linux-x64 --self-contained false \
  -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false \
  -o <candidate>/provider
gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp04 \
  -Wl,--build-id=none -Inative/linux-m1 \
  -o <candidate>/native/exec-coordinator native/linux-m1/exec-coordinator.c
python3 native/linux-m1/seccomp_filter_policy.py --check
python3 native/linux-m1/seccomp_filter_policy.py --binary > <candidate>/native/seccomp-filter.bin
```

Record exact `dotnet --info`, `gcc --version`, `python3 --version`,
`uname -smr`, distro, `ldd --version`, `bwrap --version`, `sha256sum`,
`file`/`readelf` output. No binary reproducibility claim. The build must not
include `tests/Fixtures`, test coordinators, D6.4 variants, PDBs, apphosts (if
`UseAppHost=false`), or build-only packages in the runtime asset set.

The current `deps.json` for Owner/Recovery lists `Microsoft.SourceLink.GitHub` →
`System.IO.Hashing` as a runtime library. The candidate step must either exclude
build-only assets from `deps.json` or include every listed runtime asset in the
manifest; the verifier refuses a deps-listed asset that is absent from the
manifest. This is an implementation finding (B6-F10), not a reason to ignore the
dependency.

### 7.2 Proposed installed layout

All install metadata and executables are root:root; data 0444, executables
0555; directories 0755; no symlinks/hardlinks/setuid/file caps.

```
/opt/gagamba/                                  root:root 0755
  active                                       root:root 0444  "<digest>\n"
  installations/
    <artifactSetSha256>/                        root:root 0755
      manifest.json                             root:root 0444
      install-receipt.json                      root:root 0444
      provider/
        Gagamba.Execution.dll                   root:root 0444
        Gagamba.Execution.Linux.M1.dll          root:root 0444
        Gagamba.Runtime.dll                     root:root 0444  (only if host uses it)
      recovery/
        Gagamba.Execution.Linux.M1.Recovery.dll root:root 0444
        ...Recovery.deps.json                   root:root 0444
        ...Recovery.runtimeconfig.json          root:root 0444
        Gagamba.Execution.Linux.M1.dll          root:root 0444
        Gagamba.Execution.dll                   root:root 0444
        <exact deps.json runtime assets>        root:root 0444
      owner/
        Gagamba.Execution.Linux.M1.Owner.dll    root:root 0444
        ...Owner.deps.json                      root:root 0444
        ...Owner.runtimeconfig.json             root:root 0444
        Gagamba.Execution.Linux.M1.dll          root:root 0444
        Gagamba.Execution.dll                   root:root 0444
        <exact deps.json runtime assets>        root:root 0444
      native/
        exec-coordinator                        root:root 0555
        seccomp-filter.bin                      root:root 0444
  qualifications/
    <qualificationId>.json                      root:root 0444
```

No mutable configuration file selects the profile. The profile ID/version,
seccomp digest, argv and authority/resources are compiled constants and are
recorded in the manifest; an installed JSON policy file is not introduced. The
operator-supplied cgroup parent, staging parent, journal and evidence directories
remain runtime state outside the install root, user-owned 0700.

### 7.3 Install, verification and removal commands (proposed)

- `eng/linux-m1/build-install-candidate.sh <out>` — build, manifest, digest; no
  install.
- `sudo eng/linux-m1/install-candidate.sh --candidate <dir> --expected-digest <64 hex> --generation <64 hex>`
  — verify the candidate digest against the expected reviewed value, copy
  without content change, set root:root modes, write receipt, fsync, atomically
  update `active`. Refuse missing/mismatched/extra files, non-root caller,
  non-root-owned ancestors, existing generation directory and any content
  modification. The installer script itself must be reviewed and its SHA-256
  recorded; it is run once and is not part of the runtime artifact set.
- `eng/linux-m1/verify-install.sh --root /opt/gagamba` — unprivileged read-only
  recomputation of the exact set, hashes, modes, ownership, no symlinks, deps
  asset set and qualification binding; prints `Qualified`, `Installed`, or a
  typed refusal. This is operator evidence; the production verifier is the
  admission authority.
- `sudo eng/linux-m1/qualify-install.sh --root /opt/gagamba --qualification <record>`
  — after B6-T01–T25 pass against the installed digest, install the root-owned
  qualification record; refuse if the record digest != active digest.
- `sudo eng/linux-m1/remove-generation.sh <digest>` — refuse `active`, refuse if
  a process still holds/maps the generation files, remove only the exact
  digest-named directory, preserve `qualifications/`.

These are bounded local tools, not a package manager or a signing service.

### 7.4 Installation privileges and assumptions

- **Install:** uid 0 (root) in the disposable WSL2 instance. The build user is
  unprivileged. Root's act of installing is the authorization decision.
- **Runtime:** non-root unprivileged uid with empty `CapEff`, as required by
  `RecoveryInterop.IsUnprivileged`. No setuid helper, no file capabilities, no
  privileged daemon.
- **Ancestors:** `/opt`, `/opt/gagamba` and the generation/qualification dirs
  root-owned, not group/other-writable. If `/opt` is not root-owned on a host,
  the installer refuses.
- **Test/development:** a user-owned install is permitted only as
  `Installed/Unqualified`; the verifier reports the ownership deficiency and
  restricted admission refuses. This keeps Windows/WSL2 development practical
  without weakening the qualified path.

### 7.5 Cleanup and removal expectations

- Disposable WSL2 qualification runs remove their generation, installed files,
  processes, cgroups, staging, journal and evidence directories; any residue is
  recorded as cleanup `Unknown`.
- The removal script never deletes by wildcard, a relative path, the active
  generation, or a directory whose recomputed digest differs from its name.
- Old generations are retained until explicitly removed; this is intentional to
  avoid deleting files still mapped by a surviving execution.

### 7.6 Compatibility and refusal conditions

| Condition | Result |
|---|---|
| RID not `linux-x64` (including `linux-arm64` until a matching filter exists) | `UnsupportedProfile` |
| Kernel/glibc/cgroup2/openat2/pidfd/SO_PEERPIDFD/subreaper/userns/seccomp missing | `PrerequisitesUnavailable` |
| Bubblewrap version/hash/ownership/caps mismatch | `UnqualifiedInstallation` |
| .NET host/runtime/hostfxr version or inventory mismatch | `InstallationUnavailable`/`UnqualifiedInstallation` |
| Manifest schema/owner protocol/coordinator protocol/bootstrap mismatch | `UnqualifiedInstallation` |
| Profile ID/version/RID/seccomp/filter/argv/authority/resources mismatch | `UnsupportedProfile`/`UnqualifiedInstallation` |
| Missing/extra artifact, bad mode/uid/gid/nlink, symlink, setuid, file caps | `UnqualifiedInstallation` |
| `active`/receipt/manifest/digest/qualification disagreement | `Unavailable`/`UnqualifiedInstallation` |
| Unprivileged requirement not met (root or non-empty caps) | `UnqualifiedInstallation` |
| Test-only artifact/assembly/symbol in the closure | `UnqualifiedInstallation` |

### 7.7 Test-only artifact exclusion

The install role/kind/path grammar and verifier deny list exclude:

- test-only coordinators (`b3-release-coordinator`, `wp04-qualification-coordinator`,
  `wp05-qualification-coordinator`, D5 variants);
- synthetic authority providers, fixture targets/probes and `tests/Fixtures/*`;
- fault-injection hooks (`Int01QualificationFault`, `QualificationFault`,
  `QualificationNativeStop`, `QualificationBeforeCaptureStore`);
- D6.4 isolated fixture variants and `Gagamba.Execution.Linux.M1.TestFixture`;
- test-only friend assemblies/contracts (`*.Tests`, `*.TestFixture`,
  `B2Controller`, `*.Fixture.*`);
- synthetic clocks (`FakeDomainClock`, `RelativeTestClock`; production uses
  sealed `StopwatchDomainClock.Instance`);
- unqualified bootstrap alternatives (the legacy non-integrated
  `/usr/bin/dotnet` + inherited-environment path).

A test artifact intentionally retained inside a private experimental package (if
any) must be in a separate non-packable test candidate, with a distinct
directory/assembly name and no production role; it is never selected by the
installed provider. Relabeling a descriptor does not promote an experimental
artifact; the manifest/receipt/qualification digest chain and the role/hash deny
list control promotion.

### 7.8 Gate E result

A precise local installation layout, build/install/verify/remove plan and
positive/negative dependency-resolution behavior exist. Publication remains
separately gated.

---

## 8. Gate F — Admission and fail-closed behavior

### 8.1 Stage table

| Stage | Verification prerequisite | If unsatisfied |
|---|---|---|
| `Describe()` | anchor readable; manifest exact; receipt; qualification presence; host probe | `Unavailable`/`Unqualified`; no operation |
| `CreateOperation(selection)` | full install verify; selection fields equal; protocol/RID/profile equal; prerequisites verified | typed `RestrictedOperationCreation.Reason`; no operation |
| `PrepareAsync` start | operation generation still active; controller assemblies still match; runtime pin; capture/readback component present | `Failed`/`InvalidBinding`; no domain/staging |
| journal intent | existing journal writer/intent proof; no new authority | `Failed`; Unknown cleanup |
| spawn owner/recovery | exact closure; pinned host; cleared env; post-start echo; no PATH | abort/stop; no domain |
| HELD acceptance | existing coordinator uid/seccomp/namespace/mount/cgroup checks + installation echo | abort; no RELEASE |
| before RELEASE | generation unchanged; identity echoes; deadlines; existing C1–C4 | `Refused`/`Unknown`; no second attempt |
| during execution | no kill solely on manifest mismatch; existing obligation/lifeline | `Unknown`/`Confirmed`; cleanup components separate |
| recovery | same generation/identity; no release; retained authority | `Refused`; no adoption |
| cleanup | existing reconciliation; `Confirmed` only if all components | `Unknown` preserved |

### 8.2 Pre-admission

An unsupported or unqualified installation is not selectable as a restricted
provider. `UnavailableRestrictedExecutionProvider` remains the default; no
ordinary `IExecutionProvider` is a fallback; no environment override,
`allowUnqualified` flag or test-coordinator path selects a provider.

### 8.3 Before preparation

Missing, incompatible or substituted mandatory artifacts refuse before target
initialization. The refusal distinguishes:

- installation failure (`InstallationUnavailable`, `UnqualifiedInstallation`);
- unsupported sandbox capability (`UnsupportedProfile`,
  `PrerequisitesUnavailable`);
- invalid caller binding (`InvalidBinding`);
- missing runtime dependency (`InstallationUnavailable`/
  `RuntimeDependencyMissing`).

### 8.4 Before release

An artifact mismatch discovered after preparation is not an ordinary successful
preparation. The controller refuses `ReleaseRequest`, preserves the one-shot
release rule and stop-first invariant, and returns a bounded failure/`Unknown`.
The owner's capability/arbiter still governs; no second physical RELEASE.

### 8.5 During execution and recovery

A later disk-manifest mismatch alone does not authorize killing an already
running sandbox. The relationship is:

- **Verified retained identity** (in memory/fds) is the execution's identity.
- **Native process identity** (pidfd/direct peer) is the lifetime authority.
- **Retained stopping authority** (recovery/owner) is unchanged by a manifest
  mismatch.
- **Observed runtime state** and **deployment changes** are recorded, not
  converted into stop authority.
- **Existing stop deadlines** are authoritative and not renewed.
- **Cleanup/journal evidence** remains separate; no fabricated `Confirmed`.

A deployment change requires: refuse new release; keep existing stop/lifeline
obligations; preserve `Unknown` if cleanup is uncertain; no disk-only adoption.

### 8.6 Failure vocabulary

Reuse existing Gagamba semantics; add only bounded, additive diagnostics:

| Situation | Existing result | Additive diagnostic (proposal) |
|---|---|---|
| Unsupported platform/RID | `RestrictedRefusalReason.UnsupportedProfile` | `UnsupportedRuntimeIdentifier` |
| Missing installation | `InstallationUnavailable` | `InstallationMissing` |
| Untrusted/unverified | `UnqualifiedInstallation` | `UntrustedMetadata` |
| Artifact integrity mismatch | `UnqualifiedInstallation` | `ArtifactIntegrityMismatch` |
| Protocol/ABI mismatch | `UnqualifiedInstallation`/`UnsupportedProfile` | `ProtocolIncompatible` |
| Missing runtime dependency | `InstallationUnavailable`/`PrerequisitesUnavailable` | `RuntimeDependencyMissing` |
| Preparation failure | existing `RestrictedPrepareKind.Failed` | — |
| Target execution failure | existing outcome (`TargetFailed`/`Unknown`) | — |
| Native stop failure | existing `RestrictedStopSubmission.Unknown/Refused` | — |
| Cleanup uncertainty | `RestrictedEvidenceState.Unknown` | — |

No installation failure is converted into successful restricted execution or a
fabricated cleanup result.

### 8.7 Gate F result

Every stage has an explicit verification prerequisite and fail-closed outcome,
without adding release authority.

---

## 9. Descriptor and wire changes (bounded)

B6 implementation would touch:

- `Gagamba.Execution/Restricted/Control/RestrictedProvider.cs`: descriptor v2
  additive fields + internal factory; no public constructor/factory; existing
  v1 remains `Unavailable`; `RestrictedProfileSelection` unchanged.
- `Gagamba.Execution.Linux.M1/Installed/*`: canonical manifest model/codec,
  verifier, installation identity, production composition.
- `Bootstrap/RecoveryProtocol.cs`: `RecoveryConfiguration` v2 with
  `InstallationIdentity`; v1 refused for the qualified profile.
- `Bootstrap/RecoveryHost.cs`, `IntegratedRecoveryHost.cs`: full closure
  verification and echo.
- `Native/OwnerProtocol.cs`, `Lifecycle/OwnerPreparationTransaction.cs`: bounded
  `InstallationEcho` before authority allocation.
- `Lifecycle/M1RecoveryScope.cs`, `Bootstrap/RecoveryConnection.cs`: require the
  pinned runtime; remove the PATH/`/usr/bin/dotnet` legacy path from qualified
  composition.
- `Bootstrap/ExperimentalRuntimePin.cs`: root-only install variant or reuse with
  a stricter ownership predicate; no change to its runtime inventory semantics.
- `Directory.Build.props` / csproj if needed to keep build-only packages out of
  runtime `deps.json` (B6-F10).

No new public release authority; no public manifest/verifier API; no package
publication.

---

## 10. Alternatives and decisions

### 10.1 Alternatives (summary)

| Alternative | Decision | Reason |
|---|---|---|
| A. Root-owned immutable local installation + receipt + qualification record | **Recommended** | smallest, explicit trust anchor, no external signing, local-first, testable |
| B. Trusted signed artifact manifest | Deferred | adds key management/signing without closing a threat in the accepted same-UID-excluded model; not a hidden prerequisite |
| C. Existing caller-populated profile | **Rejected for production** | B3-F04: caller can point at any compatible-looking files; no independent installation trust |

### 10.2 Decisions recorded by this design

- **D1:** artifact-set identity is the SHA-256 of a canonical manifest over exact
  application-controlled bytes; source commit/tree is descriptive.
- **D2:** authorization is a root-owned install receipt plus an accepted
  qualification record; neither is supplied by the artifact directory.
- **D3:** a rebuild is a new artifact set unless its recomputed digest matches a
  qualified digest; source-only promotion is prohibited.
- **D4:** initial qualified profile is `linux-x64` only; `linux-arm64` is
  unsupported until a matching seccomp policy/build exists.
- **D5:** side-by-side immutable generations; no cross-generation adoption; no
  new journal schema.
- **D6:** test-only artifacts are excluded by role/kind/path grammar, known
  hashes/assembly names and the manifest/receipt/qualification chain.
- **D7:** descriptor v2 is additive/passive; `Describe()` is not admission.
- **D8:** the legacy non-integrated `/usr/bin/dotnet` + inherited-environment
  path is refused for the qualified profile.
- **D9:** platform baseline is explicit, version-constrained and evidence-bound;
  unidentified system components are not silently trusted.

### 10.3 Human decisions still required

These are bounded choices, not design contradictions. Implementation must not
manufacture approval.

- **HD-1 (install privilege):** confirm that B6 qualification may use `sudo` in
  a disposable WSL2 instance to create the root-owned install. If not, the
  native `Qualified` tests are Pending and only deterministic verifier tests run.
- **HD-2 (public descriptor expansion):** approve the additive descriptor v2
  fields (or keep them internal until B7). Recommended: additive, passive,
  no public factory.
- **HD-3 (native host):** confirm B6 initial qualification remains WSL2-only
  with native Linux-host qualification deferred to B5-L01/B7.
- **HD-4 (generation retention):** confirm old immutable generations are retained
  until explicit removal (no auto-GC) and that removal requires no active
  holders.
- **HD-5 (globalization mode):** select invariant globalization for the trusted
  managed controller/owner/recovery components (recommended, conditional on
  demonstrated compatibility) or record the non-invariant ICU baseline with its
  platform-baseline limitation. This is a separate decision from HD-3 and does not
  impose invariant globalization on the untrusted target.

---

## 11. Remaining qualifications and non-claims

B6 does **not** establish:

- A production restricted provider or `RestrictedExecutionRuntime.Create()`
  activation.
- Native Linux-host or macOS qualification; WSL2 is not a native Linux host.
- Binary reproducibility across rebuilds/toolchains.
- Resistance to root/admin/kernel compromise, same-UID hostile host processes or
  a compromised trusted bootstrap.
- Complete cleanup for every fault; existing 20 Confirmed / 12 Unknown categories
  and B4Q1-F02–F04/Q1R-08/R1-F02 dispositions remain.
- That a passing test with an unavailable loader/native feature proves sandbox
  enforcement; such cases are Pending/Unsupported, not Passed.
- Any new release authority, deadline, capability or recovery path.
- NuGet/public package publication, signing, remote attestation, TPM/secure boot,
  containers/VMs, Hufu integration or automatic updates.

B7 and B5-L01 must eventually establish native Linux-host installation/lifecycle
qualification, broader fault coverage and independent reproduction. B6 evidence
is a prerequisite, not a substitute.

---

## 12. Proposed bounded implementation commission

**ID:** `GAGAMBA-RC2B-06-B6-IMPL-01` (proposal only; not executed).

### 12.1 Objective

Implement the installed-artifact verifier, manifest/receipt/qualification model,
installed-provider descriptor and identity propagation, plus the local
build/install/verify/remove candidate and the B6-T01–T25 qualification harness.
Do not activate production restricted execution.

### 12.2 Expected production files

- `src/Gagamba.Execution/Restricted/Control/RestrictedProvider.cs` — additive
  descriptor v2 fields, internal factory, diagnostics; no public authority.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledArtifactManifest.cs` — new.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledArtifactVerifier.cs` — new.
- `src/Gagamba.Execution.Linux.M1/Installed/InstallationIdentity.cs` — new.
- `src/Gagamba.Execution.Linux.M1/Installed/InstalledRestrictedProvider.cs` —
  new production composition; refuses caller-populated `B3FixtureProfile`.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryProtocol.cs` — v2
  configuration + identity.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryHost.cs`,
  `IntegratedRecoveryHost.cs` — closure verification/echo.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryConnection.cs` — require
  pinned runtime; refuse legacy PATH path for qualified profile.
- `src/Gagamba.Execution.Linux.M1/Bootstrap/ExperimentalRuntimePin.cs` — stricter
  ownership predicate for installed closure (root-only) without changing runtime
  inventory semantics.
- `src/Gagamba.Execution.Linux.M1/Native/OwnerProtocol.cs`,
  `Lifecycle/OwnerPreparationTransaction.cs` — bounded `InstallationEcho`.
- `src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs` — require runtime
  pin for qualified composition.
- `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs` — remain `Unavailable`;
  no production activation.
- `Directory.Build.props`/csproj only if required to remove build-only packages
  from runtime `deps.json`.

### 12.3 Expected test/tooling files

- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstalledArtifactManifestTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstalledArtifactVerifierTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstalledDescriptorTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstallationAdmissionTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/NativeInstallationQualificationTests.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstallationFaultTests.cs`
- `tests/Fixtures/` additions only for bounded positive/negative test artifacts;
  never production.
- `eng/linux-m1/build-install-candidate.sh`
- `eng/linux-m1/install-candidate.sh` (root-run, reviewed)
- `eng/linux-m1/verify-install.sh`
- `eng/linux-m1/qualify-install.sh`
- `eng/linux-m1/remove-generation.sh`
- `eng/linux-m1/qualify-installation.sh`

### 12.4 Native artifact-build requirements

- Static x86-64 coordinator from `native/linux-m1/exec-coordinator.c` with the
  exact reviewed gcc flags and no build-id.
- Seccomp filter 672 bytes from `seccomp_filter_policy.py --binary`; `--check`
  must pass; digest must equal `2aa879...d108`.
- Owner/Recovery/Provider publish closures with exact deps/runtimeconfig;
  `UseAppHost=false`, no PDBs, no test assemblies.
- Exact .NET host/runtime/hostfxr and bwrap 0.11.1 installed/recorded.
- No arm64 claim.

### 12.5 Acceptance criteria

- B6-T01–T25 implemented with deterministic/native scope as specified; all
  required native cases run on the declared WSL2 x86-64 host; no required case
  silently skipped; T25 (B2 install closure) passes, not skips.
- The installed artifact-set digest equals the qualified digest; every mandatory
  artifact and platform baseline is recorded.
- Negative tests prove refusal for altered/missing/test-only/wrong-protocol/
  wrong-RID/tampered/partial/untrusted installations before target execution.
- Descriptor reports `Unavailable`/`Unqualified`/`Qualified` truthfully; no
  optimistic state.
- C1–C4, R1-F03 Option A, B1–B5, one physical RELEASE, independent recovery,
  original deadlines, protected capture and conservative cleanup are preserved.
- `RestrictedExecutionRuntime.Create()` remains production-unavailable.
- Source-bound evidence artifact, independent review, owner acceptance.

### 12.6 Stop conditions

Stop and record a finding if:

- implementation requires changing C1–C4/B5 semantics, the release authority
  model, or the accepted recovery contract;
- the install cannot be root-owned in the declared environment;
- a required native case cannot run and would be skipped;
- the source advances from the authorized baseline without reconciliation;
- the design requires signing infrastructure, a package manager, a daemon,
  remote attestation or publication;
- test-only artifacts cannot be excluded from the production closure;
- a manifest/receipt/qualification digest cannot be independently reproduced.

### 12.7 Independent review handoff

Provide the exact source commit/tree, candidate digest, manifest, receipt,
qualification record, raw logs, negative evidence, cleanup state, host identity,
toolchain and known limitations. The reviewer must challenge the root of trust,
circular/self-asserted attestation, dependency closure completeness, hash-then-
execute windows, loader/environment substitution, generation binding, test-only
inclusion, source-vs-binary identity and unsupported WSL2 claims. Separate
design correctness, implementability, native verification still required and
production qualifications not established.

---

## 13. Design acceptance mapping

| Gate | Where addressed | Status |
|---|---|---|
| B6-S1 complete closure | §2, §7.2 | source-backed inventory; install candidate is an implementation obligation |
| B6-S2 independent trust | §4.2–4.4 | root-owned receipt + qualification record; no self-asserted hashes |
| B6-S3 verification-to-use | §5 | fd hashing + root-owned tree + cleared env + echo; residual root race documented |
| B6-S4 multi-process consistency | §6 | installation identity/echo; no cross-generation adoption |
| B6-S5 fail-closed admission | §8 | staged prerequisites and typed refusals |
| B6-S6 authority preservation | §3, §5.4, §6, §11 | no new RELEASE/capability/deadline; recovery never releases |
| B6-S7 evidence | §12, qualification plan | source-bound tests and raw evidence |
| B6-S8 bounded complexity | §1, §7, §12 | one local install/verifier; no package manager/signing service |
