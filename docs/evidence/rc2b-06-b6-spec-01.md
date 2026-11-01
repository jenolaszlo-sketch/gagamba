# RC2B-06 B6-SPEC-01 — Source, Decision and Gap Traceability

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-01 (specification only).

**Status:** Documentation-only evidence report. No implementation, test execution,
native run, package build, installation or production activation is claimed.

**Design:** [rc2b-06-b6-installed-artifact-trust.md](../design/rc2b-06-b6-installed-artifact-trust.md).

**Qualification plan:** [rc2b-06-b6-qualification.md](../plans/rc2b-06-b6-qualification.md).

---

## 1. Source identity

| Item | Verified value |
|---|---|
| Repository | `jenolaszlo-sketch/gagamba` |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| Starting HEAD | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` |
| Starting tree | `e9ed9989ee35c43557e0a93fdec4d313af4f740e` |
| `origin/main` | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` (equal; no ahead/behind) |
| Tracked working tree | clean before this documentation change |
| Worktrees | 74 pre-existing; no new branch or worktree created |
| `src` tree | `ff5bb98dc4a3566dd4c8e8b58738979d6e9c6997` |
| `tests` tree | `fb2424c92854bbc24e9adec1dd2e35ef97b0a716` |
| `native` tree | `85631d2f04512376621d3d09f51d67fd90f45335` |
| `eng` tree | `9fbc9034cb3e7a611251e8f31356ffd3c0aa2b61` |
| `.github` tree | `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` |
| `docs` tree | `f958119d425a0cc4fd6613dae8383710b9bffab8` |

No source drift from the expected baseline. Existing untracked files (test
results, spikes, review drafts, `__pycache__`) were not created or modified by this
commission.

---

## 2. Documents inspected

Decision/contract chain:

- `docs/decisions/rc2b-05-b5-owner-acceptance.md`
- `docs/reviews/rc2b-05-b5-q1-independent-security-review.md`
- `docs/evidence/rc2b-05-b5-impl-01.md`
- `docs/decisions/rc2b-04-b4-owner-acceptance.md`
- `docs/decisions/rc2b-03-int-01-owner-acceptance.md`
- `docs/decisions/rc2b-03-b3-owner-acceptance.md`
- `docs/decisions/rc2b-02-b2-owner-acceptance.md`
- `docs/decisions/rc2b-01-b1-owner-acceptance.md`
- `docs/decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md`
- `docs/design/restricted-consumer-contract-rc1.md`
- `docs/plans/restricted-consumer-contract-implementation.md`
- `docs/security-model.md`
- `docs/PROJECT-STATUS.md`, `docs/handoff.md`, `docs/work-queue.md`
- `docs/fixture-protocol.md`
- `docs/implementation-plan.md`, `docs/testing-and-ci.md`
- `docs/remediation-evidence-standard.md`

Applicable obligations preserved:

- **RC-T13** (artifact hash/role/RID/protocol/build/dependency/test-coordinator
  substitution).
- **RC-T17** (package/dependency closure identity).
- **RC-T20** (surviving controller/recovery/owner topology and retained
  authority).
- **RC-T21** (test coordinator, missing helper, unsupported deployment refusal).
- B2/B3 native installation/lifecycle obligations; **B5-L04** (`artifacts/b2/install`
  absent, installation cases skipped).
- B3-F04 (caller-populated pins) and B3-F06 (truthful `Unavailable` descriptor).
- B4Q1-F01 (20 Confirmed / 12 Unknown), B4Q1-F02–F04, Q1R-08, R1-F02 preserved.
- B5-F01–F07 and B5-L01–L07 preserved.

No historical decision, review, evidence document, manifest or raw artifact was
modified.

---

## 3. Source paths inspected and observed behavior

| Path | Observed fact relevant to B6 |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs` | Internal `B3FixtureProfile` carries caller-populated owner path/hash, source/tool/input/coordinator/filter names/hashes, staging/journal/delegated paths, budgets; `TryCreate` path-checks and hashes before staging; integrated profile additionally verifies runtime, private dirs and role closures. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs` | Original controller direct session; no capability/store/RELEASE writer; validates manifest/description/HELD and recovery snapshot. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedRecoveryHost.cs` | Real recovery-parented owner; safety stop; reaping; journal cleanup with sealed clock; never authorizes a manifest. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryHost.cs` | Recovery bootstrap; verifies peer/uid/parent, `config.Recovery.Verify(...) == Assembly.GetEntryAssembly().Location`; detaches lifetime; subreaper; integrated dispatch. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryProtocol.cs` | `PinnedExecutable` is exactly five file names, hashed by path; `RecoveryConfiguration` v1; strict JSON; `PinnedExecutable` ignores deps assets, modes, ownership, links and extras. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryConnection.cs` | Integrated path uses the runtime pin; legacy path uses `ProcessStartInfo("/usr/bin/dotnet")` and inherits ambient environment. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs` | `TrySpawnOwner` uses `FileName = "dotnet"` unless a runtime pin is supplied. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ExperimentalRuntimePin.cs` | .NET host/runtime/hostfxr exact pins and inventories; clears environment; `DOTNET_ROLL_FORWARD=LatestPatch`; ownership predicate accepts current-uid ancestors, not root-only. |
| `src/Gagamba.Execution.Linux.M1/Launch/HelperInstallation.cs` | `/usr/bin/bwrap` 0.11.1, root:root 0755, no file caps, exact SHA-256, fd identity/CLOEXEC; no PATH. |
| `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs`, `BubblewrapArguments.cs` | Fixed argv; 672-byte seccomp filter with compile-time digest; fd-based sealed memfds; coordinator staged and bound at `/coordinator`; exact mount/namespace/cgroup checks. |
| `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs` | `posix_spawnattr_setcgroup_np`; descriptor map; executes `/proc/self/fd/11` for the opened bwrap object; glibc >= 2.43. |
| `src/Gagamba.Execution.Linux.M1/Preparation/SafeCopyIn.cs` | Descriptor-relative `O_NOFOLLOW`/`openat2` acquisition; size stability; SHA-256; read-only reopen; device/inode/mode/nlink/dense-accounting checks; claimed-digest mismatch. |
| `src/Gagamba.Execution.Linux.M1/Native/OwnerBootstrap.cs` | Exact four bootstrap roles; no release authority transfer. |
| `src/Gagamba.Execution.Linux.M1/Native/OwnerProtocol.cs` | Version 1; bounded frames; no capability/writer serialization. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs` | Owner verifies runtime only (`request.DirectIdentity.Runtime.VerifyCurrent()`), not its own closure; journal intent before authority; domain authority after intent. |
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`, `Lifecycle/OwnerHeldWire.cs`, `Launch/HeldExecution.cs` | One physical RELEASE; owner-local capability/writer; C1–C4 preserved. |
| `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs`, `Lifecycle/MonotonicClock.cs` | Sealed C3 clock; authoritative StopRecord deadline; Policy A; typed outcomes; no deadline renewal. |
| `src/Gagamba.Execution/Restricted/Control/RestrictedProvider.cs` | Passive descriptor version 1, all nulls/`Unavailable`; `RestrictedProfileSelection` already has profile/version/RID/qualification/artifact-set digest; `CreateOperation` refuses experimental profiles. |
| `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs` | `Create()` returns `UnavailableRestrictedExecutionProvider`; production unavailable. |
| `native/linux-m1/exec-coordinator.c` | Trusted setup coordinator; no target launch or RELEASE path; protocol v1. |
| `native/linux-m1/protocol.h`, `release_protocol.h` | Magic/version/header 96/max frame 4096/config header 202/report version 1; release frame 128/sequence 1. |
| `native/linux-m1/seccomp_filter_policy.py`, `seccomp-filter.hex` | x86-64 `AUDIT_ARCH_X86_64`; generator version 4; 672-byte output; pinned digest. |
| `eng/linux-m1/build-native.sh` | Builds test fixture, `artifacts/linux-m1/wp04/exec-coordinator`, filter, WP05 qualification coordinator; no installer. |
| `tests/Fixtures/*`, `tests/Gagamba.Execution.Linux.M1.D6_4.Tests/*` | Test-only coordinators/targets/probes/variant; D6.4 symbol is isolated to the variant assembly/output. |
| `src/.../Bootstrap/Int01QualificationFault.cs`, `OwnerSafetyMonitor.cs`, `ProtectedCapture.cs` | Fault-injection/qualification seams compiled into production M1, reachable only by friend/test composition or reflection. |

---

## 4. Current artifact-resolution behavior (summary)

- The only trusted composition that binds B1 to the real owner is internal
  `B3Qualification.TryCreate(profile)`; `profile` is a caller-populated record
  (`B3FixtureProfile`), and the integrated probe deserializes it from JSON.
- Recovery/owner closures are `PinnedExecutable` five-file sets hashed by path.
- The integrated runtime pin verifies the .NET host/runtime/hostfxr and private
  directories; it accepts current-uid-owned ancestors.
- Bubblewrap is opened/hashed and executed by fd; the coordinator/filter are
  staged from source fds with claimed digests; the owner DLL is path-hashed and
  then path-spawned.
- The owner does not self-verify its own role closure; recovery does.
- No installation manifest, receipt, generation, active pointer, qualification
  record or installed descriptor exists.
- The public descriptor truthfully reports `Unavailable`; production selection is
  unavailable.

---

## 5. Identified gaps and findings

Severity: **BLOCKER** for B6 implementation, **MAJOR** for trust/closure,
**MINOR** diagnostic/robustness, **OBSERVATION**.

| ID | Severity | Finding | Consequence | Smallest action |
|---|---|---|---|---|
| B6-F01 | BLOCKER | No installed artifact candidate, installer, manifest, receipt, generation or qualification record; B3-F04/F06 remain open. | No trustworthy local installation exists. | Implement the local install/verify candidate (design §7). |
| B6-F02 | MAJOR | `PinnedExecutable` is a hardcoded five-file path-hash closure; it ignores `deps.json` runtime assets, apphost, PDBs, extra files, symlinks, `nlink`, ownership, mode and file caps. | A compatible-looking extra/substituted file can be present; closure can be incomplete. | Replace with the exact manifest verifier; refuse extras; enumerate deps assets. |
| B6-F03 | MAJOR | `B3Qualification.TryCreate` uses path `File.Exists`/`VerifySha256` for the owner DLL and source artifacts before staging. | Hash-then-use window on the owner path; staging digest mitigates tool/input/coordinator/filter but not the owner entry. | Add root-owned install + fd verification + post-start echo. |
| B6-F04 | MAJOR | `RecoveryConnection.Start` legacy path uses `/usr/bin/dotnet` and inherits environment; `TrySpawnOwner` uses `FileName="dotnet"` when runtime pin is null. | PATH/ambient environment could select a different host for a non-integrated profile. | Require the integrated pinned runtime for qualified composition; refuse legacy. |
| B6-F05 | MAJOR | `ExperimentalRuntimePin.Protected` accepts a current-uid owner for private directories and ancestors (host/runtime/hostfxr remain root-only). | A user-owned install root/ancestor is same-UID writable and would not meet the proposed install trust anchor. | Root-only ownership for the installed closure and ancestors; user-owned is `Unqualified`. |
| B6-F06 | MAJOR | No installation identity/generation in `RecoveryConfiguration` or the owner handshake; only per-role hashes. | Cross-generation adoption or mixed installation cannot be detected. | Add bounded `InstallationIdentity` + `InstallationEcho` before authority. |
| B6-F07 | MAJOR | Owner does not verify its own closure (only runtime). | Substitution between recovery's verification and owner load is not detected by the owner. | Owner role-closure self-check + echo. |
| B6-F08 | MAJOR | Test-only seams (`B3FixtureProfile`, `Int01QualificationFault`, `QualificationNativeStop`, `QualificationBeforeCaptureStore`) are compiled into production M1. | Accidental/reflective use could enable fault injection or caller pinning. | Production composition excludes them; verifier refuses test variants; consider build separation. |
| B6-F09 | MAJOR | Seccomp filter is x86-64-only, but `RecoveryInterop.Supported` accepts Arm64. | An arm64 host could pass platform admission with a wrong filter. | Restrict qualified RID to `linux-x64`; arm64 unsupported until a matching filter/build. |
| B6-F10 | MINOR | Owner/recovery `deps.json` lists build-only `Microsoft.SourceLink.GitHub` → `System.IO.Hashing` runtime asset. | Closure inventory ambiguity; build-only package in runtime graph. | Exclude build-only assets or manifest-pin every deps runtime asset; refuse missing. |
| B6-F11 | MINOR | `DOTNET_ROLL_FORWARD=LatestPatch` + runtimeconfig `10.0.0` resolves to the installed patch; exact pin catches mismatch. | OS runtime patch updates break admission until re-pin. | Record exact runtime; refuse mismatch; document re-qualification. |
| B6-F12 | MINOR | `HelperInstallation` hardcodes `/usr/bin/bwrap`; bwrap package/shared-library closure is not inventoried. | Version drift refuses, but the platform baseline is not fully evidenced. | Record bwrap package version, `ldd` hashes and glibc; treat as platform baseline. |
| B6-F13 | MINOR | No installation-level capture/readback component admission check. | A build without capture could enroll incompletely. | Required-member presence check + T23. |
| B6-F14 | OBSERVATION | No B6 evidence artifact or tests existed before this design. | Implementation/qualification must start from this plan. | Use `docs/plans/rc2b-06-b6-qualification.md`. |
| B6-F15 | OBSERVATION | `B3FixtureProfile` is caller JSON in the INT-01 controller probe. | Caller controls all pins and paths. | Production installed profile is non-JSON and independently verified. |
| B6-F16 | OBSERVATION | No native Linux-host qualification; WSL2 only. | No broader Linux claim. | B5-L01/B7 separate requirement. |
| B6-F17 | OBSERVATION | No arm64 filter/build or macOS restricted execution. | RID/OS claims must remain bounded. | Explicit refusal; future separate qualification. |
| B6-F18 | OBSERVATION | Existing INT-01 cleanup remains 20 Confirmed / 12 Unknown. | B6 must not fabricate cleanup success. | Preserve typed Unknown; T16/T18 assert it. |

No source change was made to address these findings. They are implementation
obligations for a separately authorized `GAGAMBA-RC2B-06-B6-IMPL-01`.

---

## 6. Assumptions and unsupported claims

Assumptions used by the design:

- The trusted host/admin and kernel are trusted; same-UID hostile host processes
  and a compromised bootstrap are outside the accepted model.
- Root can create a root-owned `/opt/gagamba` tree in the disposable WSL2
  instance; runtime remains unprivileged.
- Bubblewrap 0.11.1 and the exact .NET runtime are available and can be pinned.
- The production coordinator is the static `native/linux-m1/exec-coordinator.c`
  build, not a test coordinator.
- The installed provider is composed by trusted host code, not by target input.

Explicitly unsupported/not claimed:

- Any B6 test result, install, digest or qualification (none was executed).
- Production restricted execution, provider activation or package publication.
- Binary reproducibility across rebuilds/toolchains.
- Native Linux-host/macOS/arm64 or Windows restricted execution.
- Resistance to root/admin/kernel/same-UID compromise.
- Complete cleanup for all faults; B7 lifecycle/fault qualification.
- That a WSL2 result is a native Linux-host result.
- That a source commit alone identifies a qualified binary.

---

## 7. Proposed implementation boundaries

`GAGAMBA-RC2B-06-B6-IMPL-01` (proposal only, not authorized):

- Production: installed manifest/verifier/identity in
  `Gagamba.Execution.Linux.M1/Installed/`; production composition separate from
  `B3FixtureProfile`; recovery configuration v2 + identity echo; owner echo before
  authority; pinned-runtime-only spawn; descriptor v2 additive fields; no public
  release authority.
- Tests: verifier/descriptor/admission deterministic suites; native installation
  and fault cases T01–T25; no test-only artifact in production.
- Build/tooling: candidate build, root install, verify, qualify, remove scripts;
  exact native/runtime pins.
- Evidence: installed digest, manifest/receipt/qualification, raw logs, negative
  evidence, cleanup, independent review.

Stop conditions and acceptance criteria are in design §12 and plan §7. No
production activation, package publication, B7, Hufu or native Linux-host
qualification is included.

---

## 8. Open human decisions

- **HD-1:** confirm `sudo` in the disposable WSL2 instance for the root-owned
  install; otherwise native `Qualified` cases are Pending.
- **HD-2:** approve additive descriptor v2 fields (or keep internal until B7);
  recommended additive/passive/no public factory.
- **HD-3:** confirm B6 initial qualification is WSL2-only with native Linux host
  deferred to B5-L01/B7.
- **HD-4:** confirm immutable generations are retained until explicit removal and
  removal refuses active/held generations.

These are bounded choices; none blocks the design from being reviewed.

---

## 9. Design readiness

The design is source-backed, identifies the complete artifact inventory, selects a
root-owned immutable installation trust anchor, defines exact artifact-set
identity, passive descriptor semantics, verification-to-use mechanisms with
honest residual races, multi-process generation binding, fail-closed admission
and a B6-T01–T25 qualification plan.

**Readiness: READY WITH QUALIFICATIONS.**

The core design is sound. The remaining qualifications are the absence of an
implemented install candidate/verifier (B6-F01–F18), the root-install decision
(HD-1), the bounded descriptor API decision (HD-2), WSL2-only initial
qualification (HD-3) and generation-retention policy (HD-4). No concrete design
contradiction prevents implementation planning.

**Next activity:** independent B6 specification review. Do not begin
implementation, production activation, publication, B7, Hufu or native Linux-host
qualification.
