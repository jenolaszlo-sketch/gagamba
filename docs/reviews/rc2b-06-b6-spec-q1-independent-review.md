# RC2B-06 B6-SPEC-Q1 — Independent Installed-Artifact Trust and Deployment Design Review

**Commission:** `GAGAMBA-RC2B-06-B6-SPEC-Q1`.

**Review type:** Independent, adversarial architecture and security design review.
Read-only; no implementation, package build, installer, test execution, host
change, branch or worktree.

**Reviewer:** GLM-5.3-Flash (opencode), a different model family from the
specification author (DeepSeek V4.1 Flash). The reviewer is independent of the
B3–B5 implementation and evidence chain. The specification author transcribed the
reviewer's findings and verified the cited source lines; the author did not
approve the design.

**Reviewed specification:** `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b`
(tree `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7`), parent
`2cd8c1d716ed2e3e61156819730cd3d39e81844b`.

**Verdict:** **REMEDIATION REQUIRED.**

**Recommendation:** **DESIGN REMEDIATION FIRST.**

---

## A. Git/source binding and review scope

| Item | Verified value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical `main` |
| Specification HEAD | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` |
| Specification tree | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` |
| Parent baseline | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` (tree `e9ed9989ee35c43557e0a93fdec4d313af4f740e`) |
| Diff from parent | exactly six documentation paths, no source/test/native change |
| Remote state | specification commit is local-only; `origin/main` remains at `2cd8c1d` |
| Worktrees | 74 pre-existing; none created or modified |

Changed paths: `docs/design/rc2b-06-b6-installed-artifact-trust.md`,
`docs/plans/rc2b-06-b6-qualification.md`,
`docs/evidence/rc2b-06-b6-spec-01.md`, `docs/PROJECT-STATUS.md`,
`docs/handoff.md`, `docs/work-queue.md`.

**Executed read-only:** git identity/diff verification; source inspection;
independent recomputation of the 672-byte seccomp filter digest from
`native/linux-m1/seccomp-filter.hex`; inspection of the existing Release build
output for the Owner/Recovery `deps.json`.

**Not executed:** no test run, no `dotnet publish`, no WSL2 run, no native build,
no installer, no installation, no cleanup exercise. This is an inspection-based
design review, not implementation qualification.

**Review boundary:** the review assesses whether the design is correct,
implementable and appropriately bounded. It does not claim production
qualification, native Linux-host qualification, binary reproducibility or
sandbox enforcement.

---

## B. Reviewed contracts and source paths

Accepted decisions/contracts read:

- B1, B2, B3, INT-01, B4 and B5 owner acceptances.
- C1–C4 and C3 Policy A human approvals.
- RC-1 restricted-consumer contract and RC-2B implementation plan.
- `docs/security-model.md`, `docs/PROJECT-STATUS.md`, handoff/work-queue.

Source inspected directly:

- `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedRecoveryHost.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryHost.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryConnection.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryProtocol.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/ExperimentalRuntimePin.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/Int01QualificationFault.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerSafetyMonitor.cs`
- `src/Gagamba.Execution.Linux.M1/Native/OwnerProtocol.cs`
- `src/Gagamba.Execution.Linux.M1/Native/OwnerBootstrap.cs`
- `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs`
- `src/Gagamba.Execution.Linux.M1/Native/JournalFileStorage.cs`
- `src/Gagamba.Execution.Linux.M1/Preparation/SafeCopyIn.cs`
- `src/Gagamba.Execution.Linux.M1/Preparation/CopyHost.cs`
- `src/Gagamba.Execution.Linux.M1/Preparation/Native/PreparationInterop.cs`
- `src/Gagamba.Execution.Linux.M1/Launch/HelperInstallation.cs`
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs`
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapArguments.cs`
- `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/MonotonicClock.cs`
- `src/Gagamba.Execution/Restricted/Control/RestrictedProvider.cs`
- `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Restricted/B3RestrictedLifecycleTests.cs`
- `tests/Gagamba.Execution.Linux.M1.D6_4.Tests/Gagamba.Execution.Linux.M1.D6_4.Tests.csproj`
- `eng/linux-m1/build-native.sh`

The design's load-bearing source claims were checked line-by-line. They are
accurate except where findings below identify wording or association gaps.

---

## C. Trust anchor and qualification provenance

### C.1 What is sound

The proposed root-owned immutable generation, root-owned receipt and root-owned
qualification record are the smallest mechanism that fits the accepted
trusted-host model. The chain is:

`active` (root 0444) → generation directory named by the recomputed canonical
manifest digest → root-owned `install-receipt.json` → root-owned qualification
record with the same digest.

The verifier recomputes the manifest digest from the installed bytes and refuses
missing/extra files, so a qualification record cannot authorize a different byte
set. The target cannot write the root-owned tree. The design explicitly names
root as the trusted installer and documents the root/admin residual race rather
than hiding it. Source confirms the existing primitive precedent: fd-based
Bubblewrap open/hash/identity (`HelperInstallation.cs`), root-oriented runtime
pins (`ExperimentalRuntimePin.cs`), and passive descriptor/refusal vocabulary
(`RestrictedProvider.cs`).

The rebuild rule is correct: same commit is not byte identity; a new build is a
new artifact set unless its recomputed digest matches a qualified digest.

### C.2 Qualification association is not unambiguous

The design names `/opt/gagamba/qualifications/<id>.json` but never states how the
verifier selects exactly one record for the active digest. The install receipt
contains `expectedDigestSource` but no `qualificationId` or
`qualificationRecordSha256`. If zero, one or several records match the digest,
the verifier has no defined precedence. A stale record for the same digest (for
example, an earlier narrower or broader scope) could be selected, or a new
record could be written by root without an atomic pointer update.

This is a real association gap, not a cryptographic one: root is trusted, but
the design's own claim is an exact, deterministic qualification association.
See **B6-Q1-F13**.

### C.3 First-install provenance

The two-phase model (`Installed` then `Qualified`) is the right shape, but §7.3
requires `install-candidate.sh --expected-digest` on every install. For a first
install there is no independent expected digest; root can compute it from the
candidate. That is acceptable as an integrity check, but it must be labeled
`UnverifiedOrigin`/`Installed`, not treated as an independent review value. The
qualification record is the later authorization. The design should state this
explicitly. See **B6-Q1-F14**.

### C.4 Gate A conclusion

The trust architecture is sound and does not require signing infrastructure
within the accepted model. It does require an explicit single-record association
and first-install provenance semantics before implementation can claim
`Qualified` deterministically.

---

## D. Artifact and dependency closure findings

The 31-entry inventory is the strongest part of the design. Independently
verified:

- Seccomp filter binary digest is `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`
  and length 672, matching `BubblewrapAdapter.FilterSha256/FilterLength`.
- Bubblewrap pin `/usr/bin/bwrap`, version `0.11.1`, SHA-256 `523da3e7…b76b6`,
  root:root 0755, no file capabilities, fd-based open/exec.
- Coordinator protocol constants: `GGM1_VERSION=1`, header 96, config header 202,
  report version 1, release frame 128, release sequence 1; C# side checks the
  same values in `B3Qualification.WireIdentitiesMatch`.
- Owner/recovery five-file `PinnedExecutable` closure and the `deps.json` /
  `runtimeconfig.json` role.
- `RecoveryObligation.ReleaseAuthority => false` and the reconnect refusal.

Two closure gaps remain:

1. **.NET native `dlopen` dependencies are not inventoried.** The design lists
   glibc/loader and Bubblewrap's shared libraries (A21–A22), but not the native
   libraries the .NET runtime itself may load: ICU (globalization), OpenSSL
   (`libssl`/`libcrypto`) and zlib are typical. The shared-runtime directory
   hash covers the managed runtime's own `.so` files, not their system
   dependencies. The design does not set
   `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` or prove ICU is unnecessary. This
   contradicts the §2.6 claim of no unidentified loader input unless recorded as
   a bounded platform limitation. See **B6-Q1-F04**.

2. **Build-only dependency ambiguity.** The existing Release output for
   Owner/Recovery lists `Microsoft.SourceLink.GitHub` → `System.IO.Hashing` in
   `deps.json` and contains `System.IO.Hashing.dll`. This was observed in build
   output, not a fresh publish. The design already says to either exclude it or
   manifest-pin every deps-listed runtime asset; that is the right rule. It must
   be rechecked in the publish candidate and recorded. See **B6-Q1-F09**.

The conditional inclusion of `Gagamba.Runtime.dll` is also under-specified: if
the host composes through it, its platform-provider references and lazy loading
need a recorded closure. This is an observation, not a current defect, because
B6 does not activate a public runtime path.

**Gate B conclusion:** the inventory is source-backed and substantially complete
for Gagamba-controlled artifacts, but B6-S1 cannot be claimed until the native
runtime `dlopen` baseline is inventoried or explicitly bounded.

---

## E. Verification-to-execution assessment

### E.1 What is verified

- `/proc/self/fd/11` execution is correctly described. `AtomicCgroupSpawn`
  duplicates the CLOEXEC Bubblewrap fd with `F_DUPFD_CLOEXEC`, adds a `dup2` file
  action to slot 11, closes from 12, and `posix_spawn`s `/proc/self/fd/11`. The
  child resolves the retained fd-11 inode; the pathname is not used after the
  original open. This is a real fd-anchored exec for Bubblewrap.
- The coordinator and seccomp filter are staged through `SafeCopyIn`, which
  opens sources relative to a source root with `O_NOFOLLOW` and
  `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`, checks regular
  file, size stability, digest, read-only reopen, device/inode/mode/link count
  and dense accounting.
- The design correctly refuses the legacy `/usr/bin/dotnet` +
  inherited-environment path and the `FileName="dotnet"` PATH fallback for the
  qualified profile; source confirms both exist in `RecoveryConnection.Start` and
  `M1RecoveryScope.TrySpawnOwner`.
- The residual root-only replacement race is documented, not hidden. Root/admin
  compromise is outside the accepted model.

### E.2 Gaps

- **Staging is not source-fd anchored.** `CopyPlan` carries a
  `SourceRootPath` string; `SafeCopyIn` opens the source root by path and only
  has a retained *staging-parent* authority. The design's "copied from verified
  source fds" overstates the current API. Content is still claimed-digest bound,
  so this is a wording/API-seam finding, not a trust contradiction. See
  **B6-Q1-F03**.
- **Ancestor predicate exception.** `ExperimentalRuntimePin.AncestorsProtected`
  accepts a root-owned ancestor with group/other write when the sticky bit is
  set (`owner==0 && (mode&0x200)`), i.e. `/tmp`-like directories. The design says
  "no group/other-writable" for the installed closure; reusing the existing
  predicate would accept a sticky world-writable ancestor. The stricter install
  predicate must drop that exception. See **B6-Q1-F06**.
- **Controller load wording.** The design says the host loads the provider from
  the install root; the practical binding is hashing the already-loaded
  `Assembly.Location` against the manifest's provider role, as
  `B3Qualification` already does for owner/recovery pins. Load path is
  immaterial. Reword. See **B6-Q1-F10**.

**Gate C conclusion:** the verification-to-use mechanism is sound for the
accepted model, with explicitly documented root-only residuals and two bounded
API/wording corrections. No atomicity is overclaimed.

---

## F. Multi-process generation and recovery assessment

The design correctly preserves the accepted topology:

- Recovery never has RELEASE authority (`RecoveryObligation.ReleaseAuthority =>
  false`).
- Reconnect is refused; no generation can be restored from a passive record.
- Owner capability/store/RELEASE writer remain owner-local.
- C3 StopRecord/deadline, Policy A and B5 boot/clock restrictions are unchanged.
- The pre-RELEASE active-generation recheck is a controller-side conservative
  refusal, not a new owner gate; it does not conflict with C1–C4 or the arbiter.
- Side-by-side immutable generations with no cross-generation adoption are the
  smallest policy; no journal schema change is required.

Two specification issues:

1. **The installation identity echo is not wire-specified.** The design names a
   32-byte digest + u16 versions + role bit, but not the exact new
   `RecoveryMessage`/`OwnerProtocolMessageType` IDs, payload bound, sequence
   direction, or which message carries the owner's expected identity. This is a
   security-relevant ordering (before `WriterReady`/`StartAuthority`), so it
   must be precise before implementation. See **B6-Q1-F07**.
2. **The "recovery restart after active changed" branch is hypothetical.** There
   is no reconnect listener or post-controller recovery relaunch today. The
   design reads as if it is a current mechanism. Mark it hypothetical. See
   **B6-Q1-F11**.

**Gate D conclusion:** generation/recovery authority semantics are correct;
the identity propagation needs a bounded wire addendum before implementation.

---

## G. Test-only separation and descriptor compatibility

### G.1 Test-only separation

The D6.4 variant is isolated correctly: separate assembly name/output paths,
`GAGAMBA_WP05_D6_4_TEST_FIXTURE` only in the dedicated project, non-packable,
excluded from production. The design's deny list covers separate test
assemblies and fixtures.

However, the design's §7.7 claim is too strong. The following are compiled into
the pinned production `Gagamba.Execution.Linux.M1.dll`:

- `Int01QualificationFault`/`Int01Qualification` (`Bootstrap/Int01QualificationFault.cs`);
- `OwnerSafetyMonitor.QualificationNativeStop` (`OwnerSafetyMonitor.cs:38`,
  wired at `OwnerPreparationTransaction.cs:97–98`);
- `BubblewrapAdapter.QualificationBeforeCaptureStore`
  (`BubblewrapAdapter.cs:25`, wired at `OwnerPreparationTransaction.cs:394`);
- `SafeCopyIn.CopyTestHooks` / `BarrierDeathException`
  (`Preparation/SafeCopyIn.cs:43–56`);
- `JournalFileStorage.QualificationBeforeRename`
  (`Native/JournalFileStorage.cs:33,88`).

A file/assembly hash deny list cannot remove them from the production assembly,
and T24's positive control "production candidate without test symbols/assemblies"
is not satisfiable by the current production source. Reachability is mitigated
because the installed provider refuses a caller-populated `B3FixtureProfile` and
no production configuration carries a fault, but the closure contains dormant
hooks. The plan must either conditionally compile them out or record the bounded
residual and narrow T24. See **B6-Q1-F02**.

### G.2 Descriptor compatibility

The descriptor is passive and cannot mint RELEASE, create a preparation
capability, bypass preparation or serve as a recovery bearer. `CreateOperation`
re-verifies; `RestrictedExecutionRuntime.Create()` remains the unavailable
provider. Existing `RestrictedRefusalReason` numeric identities are preserved;
additive diagnostics do not change existing refusal contracts.

The proposed public v2 surface is larger than B6 needs. `InstallationState`,
`RefusalReason`, `ArtifactSetSha256`, profile/version/RID/qualification ID are
sufficient for admission/refusal reporting; per-file `Artifacts`, `Verification`
and `Platform` inventories duplicate internal manifest data. Keep those internal
until a real consumer exists. This is HD-2, not a defect. See **B6-Q1-F08**.

**Gate G conclusion:** test-only separation needs a concrete build/seam decision;
descriptor v2 is compatible but should be minimized.

---

## H. Qualification matrix assessment

T01–T25 and V01–V12 are concrete, have positive controls, independent negative
evidence and native/deterministic classification. The matrix correctly covers
exact install, managed/native substitution, missing helper, RID, protocol,
rebuild identity, test coordinator, filter replacement, dependency omission,
loader environment, symlink/path, verify-to-use, partial install, actor version
mismatch, controller death, HELD deployment change, all-holders-loss,
unsupported host, unavailable verification, malformed trust metadata, restart,
missing capture, test variant and B2 closure.

Three qualification defects:

1. **T25 contradicts D8.** T25 says the previously skipped
   `B3RestrictedLifecycleTests` B2-install cases run against the actual installed
   closure. Those tests build `RecoveryConfiguration` v1, non-integrated, with
   `artifacts/b2/install` path pins, and `RecoveryConnection.Start` then uses
   `/usr/bin/dotnet` with ambient environment; the owner is spawned with
   `FileName="dotnet"` when no runtime pin is supplied. That is exactly the
   legacy path the design refuses for the qualified profile. The tests cannot
   pass as written against the qualified install. T25 must require migrating the
   B2 tests to the integrated `RecoveryConfiguration` v2 with pinned runtime and
   installation identity, and must keep the test-built coordinator fixture only
   as the T01/T08 positive control. See **B6-Q1-F01** and **B6-Q1-F05**.
2. **T24's positive control is not writable** while fault hooks remain in the
   production assembly. See **B6-Q1-F02**.
3. **Platform dlopen closure** (F04) and **qualification record selection**
   (F13) affect whether T01/T20/T21 can prove a deterministic `Qualified`
   association.

Otherwise the matrix is appropriately bounded. WSL2 is not native Linux; the
plan says so. A skip is Pending, not Passed. No fake owner or reconstructed
profile is used for a qualified claim.

**Gate H conclusion:** the matrix is strong but cannot be accepted as written
until T25 is reconciled with D8 and T24's seam claim is corrected.

---

## I. Implementation feasibility and minimality

The proposed `GAGAMBA-RC2B-06-B6-IMPL-01` boundary is bounded and avoids Hufu,
VM/HCS, B7, signing, publication and a package manager. It adds no durable
journal schema and no public release authority.

It is not yet complete enough to implement the design as written:

- `Preparation/SafeCopyIn.cs`, `CopyPlan` and `CopyHost` are not in the expected
  file list, yet the design relies on a source-root fd anchor that the current
  `CopyPlan` string path does not provide. Either add a retained source-root
  authority or reword the design to the existing claimed-digest path model.
- `Bootstrap/RecoveryProtocol.cs`, `OwnerProtocol.cs` and
  `OwnerPreparationTransaction.cs` are listed, but the exact identity-echo wire
  (IDs, payload, ordering, channel) is not designed.
- The install/qualification receipt/pointer association (F13) is not in the
  layout or installer steps.
- The production/test seam separation (F02) is not in the build boundary.

These are bounded design-text/build-boundary additions, not an architecture
change. The implementation remains feasible as a local installation capability.

---

## J. HD-1–HD-4 recommendations

| HD | Recommended choice | Rationale | Security impact | Implementation impact | Remaining qualification | Owner approval before IMPL? |
|---|---|---|---|---|---|---|
| **HD-1** | Approve narrowly scoped `sudo` in a disposable WSL2 instance for the root-owned install, as a precondition of IMPL-01. | Without root, the qualified native chain collapses to Pending; the plan forbids silent skips. Root is already the trusted installer in the design. No setuid helper; runtime remains unprivileged. | Root/admin is the documented trust anchor; target cannot write the install tree. | Install/remove/qualify scripts run as root; candidate build remains unprivileged. | Record exact instance, commands, paths, ownership, cleanup and residue. | **Yes** |
| **HD-2** | Approve only the minimal public v2 surface (`InstallationState`, `RefusalReason`, `ArtifactSetSha256`, profile/version/RID/qualification ID); keep per-file `Artifacts`, `Verification` and `Platform` internal until a consumer/B7. | Avoids unnecessary public API/schema expansion; B6 does not activate a production provider. | No authority change; smaller report surface. | Internal descriptor projects to public minimal fields. | Public consumer compatibility deferred. | **Yes** (or record deferral) |
| **HD-3** | Confirm WSL2 x86-64 for B6 initial qualification; native Linux host deferred to B5-L01/B7; Arm64 unsupported until a matching filter/build exists. | Matches accepted B5 scope and the x86-64-only seccomp policy; avoids an overclaim from a generic Linux capability check. | No arm64 claim; no native Linux-host claim. | Qualification runs in WSL2; evidence labels platform. | B5-L01/B7 native host. | **Yes** (or record deferral) |
| **HD-4** | Retain immutable generations until explicit removal; removal refuses active/held/in-use generations and reports `Unknown` if holders cannot be determined. | Preserves the active-generation pre-RELEASE policy and prevents deleting files still mapped by a surviving execution. | No silent loss of stop/recovery authority; conservative cleanup. | Removal script checks the active pointer and holders; no auto-GC. | Operator cleanup procedure and evidence. | **Yes** (or record deferral) |

---

## K. Findings and required design remediations

### K.1 Findings

| ID | Severity | Contract/design requirement | Reference | Expected vs actual | Evidence | Concrete failure scenario | Smallest correction | Blocks IMPL-01? |
|---|---|---|---|---|---|---|---|---|
| **B6-Q1-F01** | **Major** — demonstrated contradiction | Gate G/T25; design D8/§7.7; plan §2 T25/§7 | Design refuses legacy PATH/ambient-env; T25 requires the previously skipped B2-install tests against the qualified closure. | Expected: T25 runs a pinned, integrated installed topology. Actual: the only existing B2-install tests use `RecoveryConfiguration` v1, non-integrated, `artifacts/b2/install`, `/usr/bin/dotnet` + ambient env, and `FileName="dotnet"` owner spawn. | `B3RestrictedLifecycleTests.cs:628–671`; `RecoveryConnection.cs:52–58`; `RecoveryHost.cs:67–78`; `M1RecoveryScope.cs:194–205`. | The test either runs the refused legacy path (contradicting D8/T11) or cannot run against the installed v2 closure; "no skip" is unprovable. | Require migration of the B2 install tests to integrated `RecoveryConfiguration` v2 with pinned runtime/identity and the installed generation; keep the test coordinator only as the T01/T08 control. | **Yes (T25 exit)** |
| **B6-Q1-F02** | **Major** — unsupported assumption | Gate E/§7.7; plan T24/§12.5 | Design says test/fault hooks are excluded from production closure. | Expected: hash/assembly deny list excludes them. Actual: `Int01QualificationFault`, `QualificationNativeStop`, `QualificationBeforeCaptureStore`, `CopyTestHooks`/`BarrierDeathException`, `JournalFileStorage.QualificationBeforeRename` are compiled into production M1. | `Bootstrap/Int01QualificationFault.cs`; `OwnerSafetyMonitor.cs:38`; `OwnerPreparationTransaction.cs:97–98,394`; `BubblewrapAdapter.cs:25`; `SafeCopyIn.cs:43–56`; `JournalFileStorage.cs:33,88`. | The production candidate cannot satisfy "no test symbols" by hash; T24's positive control is unwritable; dormant hooks remain in the qualified closure. | Conditional-compile the hooks out of production, or record the dormant-hook residual explicitly and narrow T24 to separate test assemblies/variants. | **Yes (T24/closure claim)** |
| **B6-Q1-F03** | Minor | Gate C/§5.2 #6; §9 file boundary | Design says coordinator/filter are copied from verified source fds; current `CopyPlan.SourceRootPath` is a string and `SafeCopyIn` opens the source root by path. | Expected: retained source-root fd. Actual: only `StagingParentAuthority` is an fd; source root is reopened by path. Content is still claimed-digest bound. | `SafeCopyIn.cs:73–85,289–313,424`; `CopyHost.cs:18,71,124`. | A root actor could replace the source root path after verification; non-root cannot. The trust residual is root-only but the wording overstates the current API. | Add `SourceRootAuthority` to `CopyPlan`/`CopyPlan` construction, or reword to claimed-digest path copies under a verified root. | No |
| **B6-Q1-F04** | **Major** — unsupported completeness assumption | Gate B/B6-S1/§2.6 | "No unidentified security-critical artifact or loader input" vs missing .NET native `dlopen` baseline (ICU/OpenSSL/zlib) and no invariant-globalization stance. | Expected: every loader input inventoried or explicitly bounded. Actual: A21–A22 cover loader/glibc/bwrap libs, not the .NET runtime's native dependencies. | Source/manifest inspection; .NET runtime architecture; no WSL2 execution. | A root-owned ICU/SSL/zlib update or mismatch is outside the manifest and can change runtime behavior; the closure claim is not demonstrated. | Inventory `ldd`/`dlopen` dependencies of the pinned runtime and bwrap, or set invariant globalization and record a bounded platform limitation; add to the manifest/admission check. | **Yes (B6-S1 claim)** |
| **B6-Q1-F05** | Minor | Gate G/T25 wording | Plan T25 does not distinguish the previously skipped cases from new installed-closure cases or state which fixture remains. | Expected: unambiguous test set. Actual: "previously skipped B3 tests" vs installed closure migration is ambiguous. | Plan §2 T25; `B3RestrictedLifecycleTests.cs:637–800`. | Counting a migrated test as a previously skipped pass (or vice versa) would overstate B5-L04 closure. | Enumerate the exact migrated test names, the fixture kept, and the new installed-closure subcase. | No |
| **B6-Q1-F06** | Minor | Gate C/H/§5.1 #1 | Install ancestor predicate must reject group/other-writable directories. Existing `AncestorsProtected` accepts root-owned sticky (`/tmp`-like) ancestors. | Expected: strict root-only, no group/other write. Actual: `owner==0 && mode&0x200` exception. | `ExperimentalRuntimePin.cs:79–90`. | A root-owned install under a sticky world-writable ancestor could pass a reused predicate, weakening the immutability boundary. | Specify and implement a stricter predicate that drops the sticky exception; add to V04. | No |
| **B6-Q1-F07** | **Major** — under-specified security ordering | Gate D/§6.1/§5.2 #7 | Installation identity echo is security-relevant but lacks exact wire IDs, payload bounds, direction, sequence, delivery channel and owner expected-identity path. | Expected: implementable/reviewable protocol. Actual: field list only. | Design §6.1; `OwnerProtocol.cs`; `RecoveryProtocol.cs`. | A wrong/missing echo could be treated as success, or the owner could accept an identity not bound to the controller's expected generation. | Add a bounded wire addendum: exact message IDs, ≤N bytes, sequence/direction, recovery echo before `Ready`, owner echo before `WriterReady`/`StartAuthority`, and owner expected identity in the start request. | **Yes (identity binding)** |
| **B6-Q1-F08** | Observation | Gate F/HD-2 | Public v2 expansion is larger than B6 needs. | Expected: minimal public surface. Actual: per-file artifacts/platform/verification public. | `RestrictedProvider.cs:106–131`. | Unnecessary API/schema surface; future compatibility burden. | Keep diagnostics internal; expose only state/refusal/artifact-set/profile/qualification. | No |
| **B6-Q1-F09** | Observation | Gate B/§7.1 | `System.IO.Hashing` leakage claim. | Expected: confirm in a fresh publish. Actual: observed in existing Release output; not independently rebuilt. | `src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/*.deps.json` and `System.IO.Hashing.dll`; `Directory.Build.props` SourceLink `PrivateAssets=all`. | A publish may differ; the design's rule (exclude or pin) is correct but the evidence is build-output-specific. | Recheck in the candidate publish; exclude or manifest-pin every deps runtime asset; refuse missing. | No |
| **B6-Q1-F10** | Observation | Gate B/§2.2 A1 | "host loads from install root" wording. | Expected: bind executed controller closure to manifest. Actual: practical mechanism is hashing `Assembly.Location` against the provider role, as existing owner/recovery checks do. | `B3Qualification.cs:158–165`. | A literal load-from-install-root requirement is unnecessary and may not match the host app. | Reword to load-path-immaterial digest comparison. | No |
| **B6-Q1-F11** | Observation | Gate D/§6.3 | Post-active-change recovery restart branch reads as a current mechanism. | Expected: honest scoping. Actual: no reconnect listener/relaunch exists today. | `RecoveryHost.cs:148`; design §6.3. | Overstates a capability and complicates the review. | Mark hypothetical or delete. | No |
| **B6-Q1-F12** | Observation (verification, no defect) | Gate C/fd-11 | `/proc/self/fd/11` semantics. | Expected/actual: fd is duplicated to 11, closefrom(12), `posix_spawn("/proc/self/fd/11")`; pathname not used after open. | `AtomicCgroupSpawn.cs:500–515`. | None; the design does not overclaim. | None. | No |
| **B6-Q1-F13** | **Major** — association gap | Gate A/§4.5/§7.2 | Qualification record is not unambiguously selected/bound to the installed digest. | Expected: one deterministic record per active generation. Actual: receipt has no `qualificationId`/record hash; `<id>.json` selection undefined; multiple/stale records possible. | Design §4.5 (`install-receipt.json` fields), §7.2, §4.6 (`QualificationId`). | A stale/narrower/broader qualification record for the same digest could be reported `Qualified`, or two records could make admission ambiguous. | Add `qualificationId` + `qualificationRecordSha256` to the root-owned receipt or an atomic `qualified` pointer; refuse zero/multiple/contradictory records. | **Yes (Qualified association)** |
| **B6-Q1-F14** | Minor | Gate A/§4.3/§7.3 | First install requires `--expected-digest`, but no independent expected value exists before qualification. | Expected: first install is `Installed/Unqualified`; qualification record later authorizes. Actual: command implies an independent expected digest at first install. | Design §4.3, §7.3. | Root could treat a self-computed digest as reviewed, blurring Installed vs Qualified. | State `expectedDigestSource=unverified-candidate` for first install; require the qualification record for `Qualified`. | No |

### K.2 Required remediations before implementation

1. **T25 reconciliation (F01/F05):** amend the qualification plan to migrate the
   B2 install tests to the integrated `RecoveryConfiguration` v2 with pinned
   runtime and installation identity; keep the test-built coordinator only as a
   positive control; enumerate the previously skipped tests and the new
   installed-closure subcase.
2. **Test-seam separation (F02):** either conditionally compile the production
   fault hooks out, or record the dormant-hook residual explicitly in design,
   plan and qualification record and narrow T24. Do not claim the production
   assembly is free of test seams by hash/name.
3. **Identity wire addendum (F07):** specify the exact `RecoveryConfiguration` v2
   JSON fields, new message IDs, payload bounds, sequence/direction, and owner
   expected-identity delivery before `WriterReady`.
4. **Qualification association (F13):** bind a single qualification record to
   the installed digest via the receipt or a root-owned qualified pointer;
   define zero/multiple/stale behavior.
5. **Platform dlopen closure (F04):** inventory ICU/OpenSSL/zlib (or set
   invariant globalization and record the bounded limitation) and add the
   result to the manifest/admission check.
6. **Minor wording/API corrections (F03/F06/F10/F11/F14):** source-root fd or
   claimed-digest wording, strict ancestor predicate, load-path-immaterial
   controller binding, hypothetical recovery-restart wording, first-install
   provenance label.

---

## L. Verdict and smallest next action

**Verdict: REMEDIATION REQUIRED.**

The architecture is sound: the root-owned immutable generation + receipt +
qualification record is the right local trust anchor; the seccomp/bwrap/coordinator
inventory is source-accurate; fd-11 Bubblewrap execution and the owner/recovery
authority separation are real; the residual root-only race is honestly bounded.
No signing service or package manager is needed.

However, the specification cannot be accepted as implementation-ready because:

- T25 contradicts the design's refusal of the legacy B2 path (F01);
- the "no test-only closure" claim is false for the current production assembly
  (F02);
- the identity echo is not wire-specified (F07);
- the qualification record is not unambiguously bound to the installed digest
  (F13);
- the native runtime `dlopen` closure is not inventoried or bounded (F04).

These are bounded documentation/design corrections, not an architecture rewrite.
Once remediated and re-reviewed, the design can proceed to a bounded
`GAGAMBA-RC2B-06-B6-IMPL-01` after HD-1–HD-4 are recorded.

**Smallest next action: DESIGN REMEDIATION FIRST.**

Do not start B6 implementation. Amend the design/plan for F01, F02, F04, F07 and
F13; record the minor corrections; then request a focused delta review. Record
HD-1–HD-4 owner decisions before the implementation commission, with HD-1
(root install in disposable WSL2) and HD-2 (minimal descriptor v2) resolved
first.
