# RC2B-06 B6 — Installed-Artifact and Deployment Qualification Plan

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-01.

**Status:** Test design only. No test is executed, passed or claimed by this
document. Implementation is not authorized.

**Design:** [rc2b-06-b6-installed-artifact-trust.md](../design/rc2b-06-b6-installed-artifact-trust.md).

**Baseline:** `2cd8c1d716ed2e3e61156819730cd3d39e81844b` (tree
`e9ed9989ee35c43557e0a93fdec4d313af4f740e`).

---

## 1. Scope and environment

### 1.1 Initial qualification scope

- Windows host with WSL2 Ubuntu 26.04+, x86-64, as used by B3–B5.
- Unprivileged user (`uid != 0`) with empty `CapEff` (`RecoveryInterop.IsUnprivileged`).
- Operator-delegated cgroup2 subtree (`systemd-run --user --property="Delegate=memory pids"`,
  as in B5).
- Bubblewrap 0.11.1 at `/usr/bin/bwrap`.
- .NET 10.0.x host/hostfxr/shared framework matching the installed pin.
- glibc >= 2.43 (atomic `posix_spawnattr_setcgroup_np`), `openat2`, `pidfd_*`,
  `SO_PEERPIDFD`, `PR_SET_CHILD_SUBREAPER`, user namespaces, seccomp.
- Root (`sudo`) available **inside the disposable WSL2 instance** for the
  root-owned install. If it is not available, the native `Qualified` portions
  are `Pending`, not `Passed` (HD-1).
- No host-wide modification; the instance is disposable and cleaned at the end.

Native Linux-host and macOS are out of scope. Windows managed-metadata tests do
not count as Linux installation qualification. A test that passes only because
the loader or native feature is unavailable is **not** proof of enforcement; it
is `Unsupported`/`Pending`, and blocks the gate if mandatory.

### 1.2 Test classes

- **V — verifier/deterministic:** pure manifest/descriptor/selection/refusal
  rules; no process start, no native allocation. May run on Windows for portable
  logic, but a Linux metadata replay is recorded separately.
- **I — integration deterministic:** in-process controller/verifier with test
  doubles, no real owner; proves sequencing and fail-closed transitions.
- **N — native:** real root-owned install, real processes, cgroup, Bubblewrap,
  coordinator, journal, stop, recovery. Required where loading/execution/OS
  behavior is material.
- **N-root:** native case that needs a root-owned install or a root mutation in
  the disposable instance. If root is unavailable, record `Pending` with reason.

A case marked `V+I+N` has independent subcases; no subcase may substitute for
another. A model-only pass is not a native pass.

### 1.3 Fixtures

| Fixture | Purpose | Must not enter production |
|---|---|---|
| `candidate-A` | exact build from the authorized commit; positive control | no |
| `candidate-A-rebuild` | independent rebuild from the same commit/toolchain | no |
| `candidate-B` | different source or tampered artifact set | no |
| `test-coordinator-b3` | `tests/Fixtures/b3-release-coordinator.c` built for T08 | yes |
| `test-coordinator-wp05` | `tests/Fixtures/wp05-qualification-coordinator.c` | yes |
| `fixture-target` | `tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture` | yes |
| `test-install` | user-owned install tree for deterministic negative tests | yes |
| `root-install` | root-owned `/opt/gagamba` generation in WSL2 | n/a |
| `mismatch-protocol` | test-only owner/recovery/coordinator built with protocol v2 | yes |
| `missing-capture` | test-only build without capture/readback member | yes |
| `fault-inject` | `Int01QualificationFault`/D6.4 variant candidate | yes |
| `root-flip` | root helper that changes `active`/bytes between phases (T13/T17) | test only |

All test-only fixtures live under `tests/`/`artifacts/` and are non-packable. The
production candidate must not reference them.

---

## 2. Per-test specification

Each case records: required environment; initial valid positive control; fault or
mutation; expected behavior; execution phase; trusted observation source;
required negative evidence; native requirement; artifacts to retain; cleanup;
limitations/skips.

### B6-T01 — Exact local installation

- **Environment:** WSL2 x86-64, unprivileged user, delegated cgroup, root install
  available; `candidate-A`.
- **Positive control:** `verify-install` reports `Qualified`; `Describe()` returns
  `Qualified` with non-null artifact set, protocol 1, RID `linux-x64`; `CreateOperation`
  returns an operation; prepare reaches real HELD; abort reconciles; no target
  constructor marker before HELD.
- **Mutation:** none (control).
- **Expected:** accepted; all mandatory artifacts/platform baseline verified;
  installed digest == manifest == receipt == qualification == selection.
- **Phase:** describe → create → prepare → HELD → abort/cleanup.
- **Observation:** verifier result, descriptor, real owner/recovery pids, journal,
  coordinator HELD frame, mount/uid/seccomp facts, cleanup.
- **Negative evidence:** none required for control; record zero test-only
  assembly/process and no target marker before release.
- **Native:** required (N).
- **Retain:** manifest, receipt, qualification, `active`, per-file hashes, process
  inventory, journal, HELD frame, cleanup tally.
- **Cleanup:** abort, reap, remove generation/cgroup/staging/journal/evidence;
  record residue.
- **Limits:** WSL2 only; no native Linux-host claim.

### B6-T02 — Managed assembly hash substitution

- **Environment/candidate:** `root-install` with one managed artifact
  (`owner/Gagamba.Execution.Linux.M1.dll`, then recovery, then provider) replaced
  by a same-length one-byte mutation in a separate generation.
- **Positive control:** unmodified generation passes T01 admission.
- **Mutation:** mutate byte; update no metadata; point `active` at the tampered
  generation (test root helper) or install it as a new generation.
- **Expected:** `UnqualifiedInstallation` with `ArtifactIntegrityMismatch` before
  target execution; no domain/owner/recovery for that operation; no fallback to
  the old generation.
- **Phase:** describe/create/prepare-start.
- **Observation:** verifier digest, descriptor, process inventory, journal (no
  new intent), cgroup inventory.
- **Negative evidence:** no owner/recovery process, no cgroup, no target marker.
- **Native:** required (N-root).
- **Retain:** tampered manifest + actual hash, refusal log, process/cgroup
  inventory, cleanup.
- **Limits:** if root mutation is unavailable, deterministic V subcase only and
  native is Pending.

### B6-T03 — Native executable substitution

- **Positive control:** exact static coordinator passes T01.
- **Mutation:** replace `native/exec-coordinator` with a byte-mutated static ELF
  of the same length in a new generation; separately attempt `bwrap` path
  replacement in a test-owned copy.
- **Expected:** verifier refuses before staging/spawn; no target execution; no
  fallback to a test coordinator or PATH.
- **Phase:** create/prepare.
- **Observation:** coordinator hash/ELF header, verifier refusal, no bwrap child.
- **Negative evidence:** no coordinator/bwrap process, no domain.
- **Native:** required (N-root for install; N for bwrap test copy).
- **Retain:** ELF `file`/`readelf`, hashes, refusal, cleanup.
- **Limits:** system `bwrap` replacement requires root and is a platform-baseline
  test; if unavailable, mark Pending.

### B6-T04 — Native helper missing

- **Positive control:** exact closure.
- **Mutation:** remove coordinator, seccomp filter, or bwrap; one at a time.
- **Expected:** explicit `InstallationUnavailable`/`UnqualifiedInstallation`; no
  PATH/system fallback; no target.
- **Phase:** create/prepare.
- **Observation:** verifier exact-set check; process inventory.
- **Negative evidence:** no substituted helper process.
- **Native:** required (N).
- **Retain:** missing-path inventory, refusal, cleanup.
- **Limits:** bwrap is platform baseline; removing it in the disposable instance
  requires root and is restored after the case.

### B6-T05 — Wrong RID/architecture

- **Positive control:** `linux-x64` manifest accepted.
- **Mutation:** manifest `rid=linux-arm64`; or an ELF with the wrong `e_machine`;
  or run an x86-64 install profile on an arm64 probe.
- **Expected:** `UnsupportedProfile`; no execution; no weakened restriction.
- **Phase:** describe/create.
- **Observation:** descriptor, ELF header, host RID.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V required; native N for ELF/host mismatch if an
  arm64 host is available; otherwise record Pending for arm64.
- **Retain:** manifest, ELF header, host `uname`.
- **Limits:** no arm64 hardware in initial scope; arm64 remains unsupported.

### B6-T06 — Protocol version mismatch

- **Positive control:** protocol 1 both sides.
- **Mutation:** test-only owner/recovery/coordinator built with owner/coordinator
  version 2, or a manifest declaring schema/owner/coordinator/bootstrap 2.
- **Expected:** no incompatible actor startup or adoption; `ProtocolIncompatible`;
  controller aborts before domain; owner/recovery refuse association; no release.
- **Phase:** create/spawn/handshake.
- **Observation:** compiled constants, echo/refusal, process exit, journal.
- **Negative evidence:** no HELD, no RELEASE, no domain.
- **Native:** required (N) with the test-only mismatch build.
- **Retain:** mismatch build identity, refusal, process/journal inventory.
- **Limits:** mismatch build is test-only; never production.

### B6-T07 — Build/artifact identity mismatch

- **Positive control:** `candidate-A` digest matches qualification.
- **Mutation:** install `candidate-A-rebuild` (same commit, independent build) or a
  candidate with one changed build input; do not update the qualification record.
- **Expected:** `Unqualified`; refusal before target; no source-only promotion;
  the old qualified generation is not silently used.
- **Phase:** describe/create.
- **Observation:** recomputed digest vs qualification digest; build identity.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V + native N (independent rebuild).
- **Retain:** both manifests/digests, toolchain logs, refusal.
- **Limits:** no binary reproducibility claim; identical bytes would pass only if
  digest matches.

### B6-T08 — Test coordinator substituted

- **Positive control:** exact production coordinator.
- **Mutation:** install `b3-release-coordinator` or `wp05-qualification-coordinator`
  as `native/exec-coordinator` (same filename, different hash/role/build identity).
- **Expected:** refused as unqualified; no target; the filename match is
  irrelevant; no fallback.
- **Phase:** create/prepare.
- **Observation:** hash/role/build identity, verifier refusal.
- **Negative evidence:** test coordinator process absent; target marker absent.
- **Native:** required (N).
- **Retain:** test coordinator hash, refusal, process inventory.
- **Limits:** if the test coordinator is accidentally included in the production
  candidate, the whole qualification is invalid.

### B6-T09 — Security-policy artifact replaced

- **Positive control:** exact 672-byte filter `2aa879...d108`.
- **Mutation:** replace filter with a same-length altered policy; or change
  length; or alter the compiled `FilterSha256` in a test build.
- **Expected:** refusal before spawn; no relaxed fallback; no target.
- **Phase:** create/prepare.
- **Observation:** filter hash/length, verifier refusal, bwrap child absence.
- **Negative evidence:** no domain/coordinator.
- **Native:** required (N).
- **Retain:** both filter hashes, length, refusal.
- **Limits:** a valid but weaker filter cannot be constructed without changing
  the generator; the test uses byte mutation to prove integrity refusal, plus a
  test-only generator variant for policy identity.

### B6-T10 — Dependency omitted or changed

- **Positive control:** exact deps/runtimeconfig/assets.
- **Mutation:** remove `Gagamba.Execution.dll` from recovery; remove a deps-listed
  runtime asset; change deps.json to resolve a same-named file elsewhere; add an
  extra managed assembly.
- **Expected:** no silent alternate resolution; refusal; no fallback to PATH,
  shared store or another runtime.
- **Phase:** create/spawn.
- **Observation:** deps asset set vs manifest; loaded assembly locations; refusal.
- **Negative evidence:** no owner/recovery process; no target.
- **Native:** required (N).
- **Retain:** deps/manifest diff, loaded-location evidence, refusal.
- **Limits:** the current `System.IO.Hashing` build-only leakage must be resolved
  or explicitly manifest-pinned; otherwise the case records a real closure
  ambiguity.

### B6-T11 — Loader environment substitution

- **Positive control:** controller started with a clean environment; exact
  install.
- **Mutation:** set `DOTNET_ROOT`, `DOTNET_MULTILEVEL_LOOKUP=1`,
  `DOTNET_ADDITIONAL_DEPS`, `DOTNET_SHARED_STORE`, `DOTNET_STARTUP_HOOKS`,
  `COREHOST_TRACE*`, `LD_PRELOAD`, `LD_LIBRARY_PATH`, `LD_AUDIT`, and a fake
  `dotnet`/`bwrap` earlier in `PATH`.
- **Expected:** children run with the fixed environment and pinned runtime; the
  fake `dotnet`/`bwrap` is never executed; if the host runtime cannot satisfy the
  pin, refuse. The untrusted override is rejected or rendered ineffective within
  the declared model.
- **Phase:** spawn.
- **Observation:** child `/proc/<pid>/environ`, loaded assembly locations,
  marker files in fake tools (must be absent), runtime directory.
- **Negative evidence:** fake-tool marker absent; no alternate runtime.
- **Native:** required (N).
- **Retain:** controller env, child env, marker absence, runtime inventory.
- **Limits:** same-UID host processes are outside the model; the test proves
  Gagamba does not propagate ambient env.

### B6-T12 — Symlink/path substitution

- **Positive control:** regular root-owned files.
- **Mutation:** replace owner.dll with a symlink to another file; make the
  generation dir a symlink; manifest path with `..`/absolute/empty component;
  hardlink the entry; replace a parent dir with a symlink.
- **Expected:** identity-checked refusal (`UnqualifiedInstallation`); no process;
  no path escape.
- **Phase:** describe/create.
- **Observation:** openat2 errno, `lstat`/`fstat`, manifest grammar, refusal.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V + native N (symlink in a root-owned test generation).
- **Retain:** link targets, errno, refusal.
- **Limits:** root-owned dir prevents non-root creation; the test uses a root
  helper or a test-owned generation marked Unqualified.

### B6-T13 — Artifact changed after verification

- **Positive control:** verify then spawn with no change.
- **Mutation:** after successful verification and before spawn/load, replace a
  managed artifact or the coordinator (root helper / test-owned generation).
- **Expected:** verification-to-use guarantee where the OS establishes it
  (Bubblewrap fexecve fd; coordinator/filter staged from verified fds); managed
  owner/recovery detect a post-start closure mismatch and refuse before authority.
  If the mutation is root-only and not detectable, record the explicit residual
  and do not claim atomicity.
- **Phase:** verify → spawn/load.
- **Observation:** retained fd identity, post-start role-closure hash, echo,
  process exit, no domain/release.
- **Negative evidence:** no HELD, no RELEASE, no target marker.
- **Native:** required (N-root for managed path; N for coordinator/bwrap fd).
- **Retain:** pre/post hashes, fd identities, echo/refusal, cleanup.
- **Limits:** root/admin replacement is outside the model; the test must state
  whether the mutation was root-only and mark the residual accordingly.

### B6-T14 — Concurrent partial installation

- **Positive control:** complete generation, atomic `active`.
- **Mutation:** create a generation with half the files; point `active` at it;
  concurrently run installer + verifier; leave a temp dir/partial manifest.
- **Expected:** no mixed-generation admission; `PartialInstallation`/`Unqualified`;
  `active` never names an incomplete generation.
- **Phase:** install/describe/create.
- **Observation:** exact-set check, active pointer, installer atomic rename,
  refusal.
- **Negative evidence:** no admission from partial generation.
- **Native:** deterministic V + native N (installer script).
- **Retain:** partial tree inventory, active pointer, refusal.
- **Limits:** root install required for the atomic step.

### B6-T15 — Controller/owner/recovery version mismatch

- **Positive control:** all actors protocol 1/installation A.
- **Mutation:** owner/recovery built with protocol 2 or installation B while the
  controller has A; or a stale controller tries to connect to an owner from B.
- **Expected:** no mixed trusted topology; handshake refuses; controller aborts;
  owner/recovery exit without releasing; recovery never gets release.
- **Phase:** spawn/handshake/HELD.
- **Observation:** echo, protocol constants, process exit, journal.
- **Negative evidence:** no HELD, no RELEASE, no domain.
- **Native:** required (N).
- **Retain:** both identities, refusal, process/journal inventory.
- **Limits:** test builds only.

### B6-T16 — Controller death after verified startup

- **Positive control:** normal HELD/abort.
- **Mutation:** kill the controller after verified startup, while HELD or during
  release/stop.
- **Expected:** recovery retains its previously authorized identity and stop
  authority; no release authority reconstructed; original deadlines retained;
  cleanup Confirmed or Unknown truthfully.
- **Phase:** HELD/run/recovery.
- **Observation:** real pids/pidfds, recovery snapshot, journal, cgroup, cleanup.
- **Negative evidence:** no new RELEASE, no disk-only adoption.
- **Native:** required (N); can reuse the existing INT-01 controller-death case
  but must bind the installed identity.
- **Retain:** pids, pidfds, journal, stop record, cleanup tally.
- **Limits:** WSL2 topology only; B5-L01 remains.

### B6-T17 — Deployment changed while owner HELD

- **Positive control:** generation A HELD, release allowed.
- **Mutation:** install generation B and flip `active` after HELD, before
  `ReleaseRequest`; optionally replace A's files (root helper).
- **Expected:** no substituted artifact receives RELEASE; controller refuses
  release; existing stop/lifeline path stops; result `Refused`/`Unknown`; no
  second physical attempt; generation B is not adopted.
- **Phase:** HELD → before release.
- **Observation:** active pointer, controller decision, owner arbiter, journal,
  process exit.
- **Negative evidence:** physical release count 0 or at most the one already
  committed; no B artifact in the HELD operation.
- **Native:** required (N-root for active flip); deterministic I subcase for the
  refusal transition.
- **Retain:** pointer before/after, journal, release decision, cleanup.
- **Limits:** if root flip is unavailable, native is Pending; do not claim it.

### B6-T18 — All trusted holders lost

- **Positive control:** recovery retains authority after controller death.
- **Mutation:** kill controller, recovery and owner; then start a new controller
  and attempt inspect/reconcile/release from journal/paths only.
- **Expected:** no disk-only authority reconstruction; inspection reports
  available evidence with `Unknown`; release refused; cleanup Unknown preserved.
- **Phase:** recovery/cleanup.
- **Observation:** journal, process inventory, cgroup, new controller refusal.
- **Negative evidence:** no RELEASE, no adoption.
- **Native:** required (N); existing INT-01 all-holders-loss case extended with
  install identity.
- **Retain:** journal, pids, cgroup, refusal, cleanup.
- **Limits:** existing 12 Unknown categories remain.

### B6-T19 — Unsupported host prerequisite

- **Positive control:** all prerequisites present.
- **Mutation:** remove delegated cgroup; unset/disable `SO_PEERPIDFD` (probe/test
  build); old glibc; missing bwrap; non-empty caps; root uid; arm64 profile.
- **Expected:** explicit refusal; no weakening; no fallback; no target.
- **Phase:** describe/create.
- **Observation:** feature probes, descriptor, refusal.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V + native N for cgroup/feature absence.
- **Retain:** probe results, host identity, refusal.
- **Limits:** some features cannot be removed without host modification; record
  Pending rather than simulating a pass.

### B6-T20 — Installation verification unavailable

- **Positive control:** readable anchor.
- **Mutation:** remove `active`, receipt, manifest or qualification record; make
  them unreadable; make the trust directory inaccessible.
- **Expected:** fail closed; `Unavailable`/`Unqualified`; no optimistic
  descriptor; no operation.
- **Phase:** describe/create.
- **Observation:** verifier error, descriptor, refusal.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V + native N.
- **Retain:** missing/unreadable evidence, refusal.
- **Limits:** none.

### B6-T21 — Malformed or self-asserted trust metadata

- **Positive control:** canonical manifest/receipt/qualification.
- **Mutation:** duplicate roles/paths, unsorted artifacts, all-zero digest, wrong
  hash length, `Qualified` with empty artifacts, receipt/manifest digest
  disagreement, qualification from another digest, manifest inside the artifact
  directory claiming trust.
- **Expected:** cannot authorize installation; refusal; never best-effort.
- **Phase:** describe/create.
- **Observation:** canonical parser, digest comparison, refusal.
- **Negative evidence:** no process/domain.
- **Native:** deterministic V required; native N optional.
- **Retain:** malformed metadata, refusal matrix.
- **Limits:** none.

### B6-T22 — Installed closure after orderly restart

- **Positive control:** T01.
- **Mutation:** stop all processes; restart controller; same generation; repeat
  describe/create/prepare/abort.
- **Expected:** same qualification and identity rules; same digest; no drift; no
  stale/optimistic state; cleanup same.
- **Phase:** restart.
- **Observation:** descriptor, digest, process/journal, cleanup.
- **Negative evidence:** no different generation/identity.
- **Native:** required (N).
- **Retain:** before/after identities, restart logs, cleanup.
- **Limits:** WSL2 only.

### B6-T23 — Required capture/readback component missing

- **Positive control:** installed provider has `ProtectedCapture`,
  `ReadCaptureAsync` and capture readback.
- **Mutation:** test-only build without the capture type/method or with capture
  readback disabled; or a host composition that does not bind capture.
- **Expected:** refusal; no incomplete successful enrollment; no HELD; no
  RELEASE.
- **Phase:** describe/create/prepare.
- **Observation:** required-member presence check, refusal, process inventory.
- **Negative evidence:** no HELD, no target, no release.
- **Native:** required (N) for the test build; deterministic V for member
  presence.
- **Retain:** missing-member evidence, refusal.
- **Limits:** the test build is test-only.

### B6-T24 — Fault-injection/test variant in ordinary installation

- **Positive control:** production candidate without test symbols/assemblies.
- **Mutation:** install D6.4 `Gagamba.Execution.Linux.M1.TestFixture`, a build
  with `GAGAMBA_WP05_D6_4_TEST_FIXTURE`, `Int01QualificationFault`, synthetic
  clocks, or a test-only friend assembly.
- **Expected:** excluded or refused; never selected; no production authority;
  relabeling the descriptor does not promote it.
- **Phase:** install/describe/create.
- **Observation:** assembly names/symbols/role/hash deny list, refusal.
- **Negative evidence:** no test variant process/domain.
- **Native:** deterministic V + native N.
- **Retain:** variant identity, refusal.
- **Limits:** none.

### B6-T25 — Native B2 installation closure

- **Positive control:** the real installed owner/recovery/bootstrap closure
  replaces the absent `artifacts/b2/install`.
- **Mutation:** none for the positive case; separately remove one closure member
  to prove refusal.
- **Expected:** the previously skipped `B3RestrictedLifecycleTests` native
  installation/topology cases run and pass against the actual installed closure;
  no `Installation closure missing` skip; the recovery topology retains its
  domain without release authority; cleanup is truthful.
- **Phase:** install/spawn/recovery.
- **Observation:** real recovery/owner processes, retained domain, journal,
  recovery snapshot, cleanup.
- **Negative evidence:** zero skipped installation cases; a missing-member
  subcase refuses.
- **Native:** required (N), mandatory; a skip is not a pass.
- **Retain:** install closure, test log with no skip, process/journal/cleanup.
- **Limits:** WSL2 only; this closes B5-L04 for the installed profile, not native
  Linux-host qualification.

---

## 3. Deterministic verifier suites (supporting V/I)

These are required in addition to T01–T25 and may be counted once each, not as
substitutes for native cases.

| ID | Suite | Required assertions |
|---|---|---|
| B6-V01 | Canonical manifest codec | deterministic field order, strict types, sorted artifacts, no self-digest, round-trip byte identity, digest stability |
| B6-V02 | Path grammar | accept relative single-component paths; reject absolute, empty, `.`, `..`, separators in components, NUL, overlong; no normalization that widens |
| B6-V03 | Artifact-set digest | every byte/field change changes the digest; reordering normalizes only where specified; duplicate role/path refuses |
| B6-V04 | File metadata | mode/uid/gid/nlink/setuid/setgid/file-cap/symlink/hardlink/device/FIFO/socket refusal |
| B6-V05 | Exact set | missing/extra file, extra directory, unexpected JSON/PDB/test assembly refusal |
| B6-V06 | Deps closure | deps.json runtime asset set == manifest managed set; missing/extra refuses; no shared-store/PATH fallback |
| B6-V07 | Receipt/anchor | active == dir name == manifest digest == receipt; missing/unreadable refuses; atomic pointer semantics |
| B6-V08 | Qualification binding | digest/profile/RID/scope mismatch → Unqualified; missing record → Unqualified; qualified only on exact match |
| B6-V09 | Descriptor contradictions | Qualified with null/empty/duplicate/all-zero/unsorted fields refuses; Unavailable/Unqualified truthful; no optimistic state |
| B6-V10 | Selection binding | ProfileId/Version/RID/QualificationId/ArtifactSetSha256 exact match; mismatch refuses with InvalidBinding/Unqualified |
| B6-V11 | Refusal vocabulary | each failure maps to the intended typed reason/diagnostic; no installation failure becomes Prepared/Released/Confirmed |
| B6-V12 | Test-only deny list | known test coordinator/fixture/D6.4/synthetic clock/fault hook names/hashes refuse; relabeling does not promote |

B6-V01–V12 run on Windows (portable logic) and are re-run on Linux; the Linux
re-run is recorded separately but is still not native execution.

---

## 4. Positive/negative control discipline

- Every negative case starts from a passing positive control in the same
  environment and records its result.
- The target marker (`gagamba-m1-fixture: ctor-marker`) must be absent before
  HELD; a missing marker is useful only because the fixture itself has a positive
  control.
- Negative evidence must be independent: process inventory, cgroup inventory,
  journal absence, coordinator frame absence, marker absence, `lsof`/`/proc`
  maps, not merely a boolean returned by the code under test.
- A test whose loader/native feature is unavailable is `Unsupported`/`Pending`
  with a reason; it is not a pass and blocks the gate if mandatory.
- No test may be counted twice across deterministic/native classes.

---

## 5. Evidence and reproducibility

For each run retain:

- exact starting/implementation commit and tree, dirty flag, source inventory;
- exact candidate path and artifact-set digest; per-file SHA-256, length, mode,
  uid/gid, nlink, RID, build identity;
- manifest, install receipt, `active`, qualification record;
- toolchain: `dotnet --info`, SDK, gcc, Python, distro, kernel, glibc, bwrap,
  `ldd`, `file`/`readelf`;
- commands and exit codes; raw logs; failing-before observations;
- per-case expected/actual, phase, observation source, negative evidence;
- owner/recovery/controller pids and pidfds, cgroup inventory, journal snapshots,
  coordinator HELD frame, mount/uid/seccomp facts;
- cleanup state: processes, cgroups, staging, journal, evidence, generation;
  `Confirmed`/`Failed`/`Unknown` separately; no fabricated success;
- skipped/unsupported cases with reasons;
- independent review commit and findings;
- exact host identity and whether the run was WSL2 or native Linux.

Rules:

- Producer execution, independent inspection and independent reproduction are
  distinct. A reviewed log is not an independent run.
- A package/artifact hash is integrity evidence, not reproducibility or
  authorization.
- A valid manifest is not proof of production authorization or sandbox isolation.
- Evidence must be bound to the exact installed digest; a rebuild invalidates it.

---

## 6. Independent review requirements

The reviewer must be a different model/agent from the specification/implementation
author. The review must:

1. Verify source/commit/tree and candidate digest independently.
2. Inspect the actual install tree and recompute every hash.
3. Challenge the root of trust and any circular/self-asserted metadata.
4. Verify managed/native dependency inventories against `deps.json`, `ldd`,
   `readelf` and the runtime/hostfxr directories.
5. Attempt or inspect hash-then-execute replacement windows, including root-only
   residuals; reject overclaims.
6. Attempt loader/environment substitution, symlink/path substitution, parent
   replacement and extra/missing files.
7. Verify installation-generation binding across controller/owner/recovery and
   the no-cross-generation-adoption rule.
8. Verify test-only artifacts are absent/refused; attempt to install one.
9. Verify source identity is not treated as binary identity; rebuild digest
   mismatch is refused.
10. Verify no unnecessary public API/schema expansion or new release authority.
11. Separate WSL2 results from native Linux-host claims; do not accept WSL2 as
    native Linux qualification.
12. Distinguish design correctness, implementability, native verification still
    required and production/security qualifications not established.

No self-approval. The review must explicitly state what it executed versus
inspected.

---

## 7. Exit criteria

B6 implementation/qualification is complete only when:

- B6-T01–T25 are implemented with the declared deterministic/native subcases.
- All mandatory native cases on the declared WSL2 x86-64 host are `Passed`;
  required cases that cannot run are `Pending` and block completion.
- **T25 runs the actual B2 installed closure and records zero installation-closure
  skips.**
- B6-V01–V12 pass; no required negative case is skipped.
- The installed digest equals the qualified digest; every artifact and platform
  baseline is recorded; no test-only artifact is in the production closure.
- C1–C4, R1-F03 Option A, B1–B5, one physical RELEASE, independent recovery,
  original deadlines, protected capture and conservative cleanup are preserved.
- `RestrictedExecutionRuntime.Create()` remains production-unavailable.
- Source-bound evidence, independent review and owner acceptance exist.
- Native Linux-host qualification remains explicitly deferred to B5-L01/B7;
  no broader claim is made.

A green deterministic suite alone is not completion. A native case that passes
because the loader/feature is missing is not completion. A source-only rebuild is
not a qualified binary.

---

## 8. Explicit non-qualifications

This plan does not qualify:

- production restricted execution or a production provider;
- native Linux-host, macOS, arm64 or Windows restricted execution;
- binary reproducibility;
- resistance to root/admin/kernel/same-UID host compromise;
- complete cleanup for every fault;
- package/NuGet publication, signing, remote attestation, TPM/secure boot,
  containers/VMs, Hufu integration, automatic updates/rollback;
- B7 comprehensive lifecycle/fault qualification.

---

## 9. Traceability to required cases

| Required ID | Plan section | Primary class |
|---|---|---|
| B6-T01 | §2 T01 | N |
| B6-T02 | §2 T02 | V + N-root |
| B6-T03 | §2 T03 | V + N-root |
| B6-T04 | §2 T04 | N |
| B6-T05 | §2 T05 | V + N |
| B6-T06 | §2 T06 | V + N |
| B6-T07 | §2 T07 | V + N |
| B6-T08 | §2 T08 | N |
| B6-T09 | §2 T09 | N |
| B6-T10 | §2 T10 | V + N |
| B6-T11 | §2 T11 | N |
| B6-T12 | §2 T12 | V + N |
| B6-T13 | §2 T13 | N-root (residual explicit) |
| B6-T14 | §2 T14 | V + N |
| B6-T15 | §2 T15 | N |
| B6-T16 | §2 T16 | N |
| B6-T17 | §2 T17 | I + N-root |
| B6-T18 | §2 T18 | N |
| B6-T19 | §2 T19 | V + N |
| B6-T20 | §2 T20 | V + N |
| B6-T21 | §2 T21 | V |
| B6-T22 | §2 T22 | N |
| B6-T23 | §2 T23 | V + N |
| B6-T24 | §2 T24 | V + N |
| B6-T25 | §2 T25 | N (mandatory, no skip) |
