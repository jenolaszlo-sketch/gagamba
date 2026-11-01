# RC2B-06 B6-SPEC-R1 — Remediation Evidence for Q1 Findings

**Commission:** GAGAMBA-RC2B-06-B6-SPEC-R1 (design remediation only).

**Status:** Documentation-only. No implementation, test execution, native run,
package build, installation or production activation is claimed.

**Original specification:** `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b`
(tree `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7`), parent
`2cd8c1d716ed2e3e61156819730cd3d39e81844b`.

**Q1 review and R1 parent baseline:** Canonical `main` at
`08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` (tree `630b803d57e27a0044686595e108868072aa20ea`).

**R1 remediation:** `77ee4414df37b9f88979bde02742756538ba45af`
(tree `62736d355e2650b5f40cdc3d904ed49466cbf025`), recorded by the follow-up
`B6-SPEC-R1-BIND-01` provenance receipt
(`docs/evidence/rc2b-06-b6-spec-r1-bind-01-receipt.md`).

**Q1 review:** `docs/reviews/rc2b-06-b6-spec-q1-independent-review.md`

---

## 1. Exact baseline source binding

| Item | Verified value |
|---|---|
| Repository | `jenolaszlo-sketch/gagamba` |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| Q1 review / R1 parent baseline HEAD | `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` |
| Q1 review / R1 parent baseline tree | `630b803d57e27a0044686595e108868072aa20ea` |
| Original specification HEAD | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` |
| Original specification tree | `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7` |
| R1 remediation HEAD | `77ee4414df37b9f88979bde02742756538ba45af` |
| R1 remediation tree | `62736d355e2650b5f40cdc3d904ed49466cbf025` |
| Parent baseline (pre-B6) | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` (tree `e9ed9989ee35c43557e0a93fdec4d313af4f740e`) |
| `origin/main` | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` (equal; no ahead/behind) |
| Worktrees | 74 pre-existing; none created or modified |
| `src` tree | `ff5bb98dc4a3566dd4c8e8b58738979d6e9c6997` (unchanged from baseline) |
| `tests` tree | `fb2424c92854bbc24e9adec1dd2e35ef97b0a716` (unchanged) |
| `native` tree | `85631d2f04512376621d3d09f51d67fd90f45335` (unchanged) |
| `eng` tree | `9fbc9034cb3e7a611251e8f31356ffd3c0aa2b61` (unchanged) |
| `.github` tree | `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` (unchanged) |
| `docs` tree | `f958119d425a0cc4fd6613dae8383710b9bffab8` (modified by this remediation) |

No source drift from the expected baseline in `src/`, `tests/`, `native/`,
`eng/`, `.github/`. Only `docs/` modified. Existing untracked files preserved.

---

## 2. Q1 finding inventory

| Finding ID | Severity | Short name | Status |
|---|---|---|---|
| **B6-Q1-F01** | Major | T25 contradicts installed-closure design | **Addressed** |
| **B6-Q1-F02** | Major | Test-only functionality in production M1 | **Addressed** |
| **B6-Q1-F03** | Minor | FD-based SafeCopyIn overstatement | **Addressed** |
| **B6-Q1-F04** | Major | Incomplete .NET native dependency closure | **Addressed** |
| **B6-Q1-F05** | Minor | Legacy skipped-test accounting ambiguity | **Addressed** |
| **B6-Q1-F06** | Minor | Ancestor predicate sticky-bit exception | **Addressed** |
| **B6-Q1-F07** | Major | Installation-identity handshake not wire-specified | **Addressed** |
| **B6-Q1-F08** | Observation | Descriptor v2 expansion larger than needed | **Addressed** |
| **B6-Q1-F09** | Observation | System.IO.Hashing leakage in deps.json | **Addressed** |
| **B6-Q1-F10** | Observation | Controller load-path wording | **Addressed** |
| **B6-Q1-F11** | Observation | Hypothetical recovery restart branch | **Addressed** |
| **B6-Q1-F12** | Observation (no defect) | /proc/self/fd/11 semantics | **Preserved** |
| **B6-Q1-F13** | Major | Qualification-record association ambiguous | **Addressed** |
| **B6-Q1-F14** | Minor | First-install provenance label | **Addressed** |

---

## 3. Finding-to-amendment matrix

| Finding | Design amendment | Plan amendment | Evidence section |
|---|---|---|---|
| F01 | §2.5 (legacy path refused), §7.6 (refusal condition) | T25 split into T25-A/T25-B | §4.1 |
| F02 | §2.5 (production/test separation: Alternative A recommended) | T24 rewritten with executable proof obligations | §4.2 |
| F03 | §5.2 step 6 (wording corrected to claimed-digest path copies) | — | §4.3 |
| F04 | §2.7 (new: native .NET runtime dependency closure table) | T20a (static + runtime + substitution tests) | §4.4 |
| F05 | — | T25-A enumerates exact skipped cases and disposition | §4.5 |
| F06 | §5.2 step 1 (stricter ancestor predicate, sticky exception dropped) | V04 covers stricter predicate | §4.6 |
| F07 | §6.2 (new: wire specification with message IDs, payloads, ordering) | T20b (handshake mutation subcases) | §4.7 |
| F08 | §4.6 (minimal public v2 surface; diagnostics internal) | — | §4.8 |
| F09 | §2.3 (fresh candidate publish inspection required) | T10 covers deps closure; candidate build recheck | §4.9 |
| F10 | §2.3 (load-path-immaterial digest comparison) | — | §4.10 |
| F11 | §6.5 (hypothetical branch marked explicitly) | — | §4.11 |
| F12 | — (no defect; design already correct) | — | §4.12 |
| F13 | §4.5 (receipt adds qualificationId + qualificationRecordSha256; explicit selection) | T20c (9 deterministic mismatch subcases) | §4.13 |
| F14 | §4.3 (first install: expectedDigestSource="unverified-candidate", state=Installed/Unqualified) | T20c subcase 1 | §4.14 |

---

## 4. Source-backed justification for each correction

### 4.1 F01/F05 — T25 split (T25-A: historical accounting, T25-B: actual B6 closure)

**Source behavior**: `B3RestrictedLifecycleTests.cs:628–800` contains the previously
skipped B2 installation cases. They use `RecoveryConfiguration` v1,
non-integrated topology, `artifacts/b2/install` path pins, `/usr/bin/dotnet` with
ambient environment, and `FileName="dotnet"` owner spawn (`RecoveryConnection.cs:52–58`,
`M1RecoveryScope.cs:194–205`). The design §7.6/D8 explicitly refuses this legacy
path for the qualified profile.

**Correction**: T25 is split. T25-A documents the four exact skipped cases and
their disposition (all remain legacy regressions, explicitly unqualified). T25-B
specifies five new native tests using the integrated `RecoveryConfiguration` v2
with pinned runtime and installation identity. No historical skip is counted as
a pass. Relationship to INT-01 documented without claiming byte-identical replay.

### 4.2 F02 — Production/test separation (compile-time exclusion recommended)

**Source behavior**: The following are compiled into production
`Gagamba.Execution.Linux.M1.dll`:
- `Int01QualificationFault` / `Int01Qualification` (`Bootstrap/Int01QualificationFault.cs`)
- `OwnerSafetyMonitor.QualificationNativeStop` (`OwnerSafetyMonitor.cs:38`, wired at `OwnerPreparationTransaction.cs:97–98`)
- `BubblewrapAdapter.QualificationBeforeCaptureStore` (`BubblewrapAdapter.cs:25`, wired at `OwnerPreparationTransaction.cs:394`)
- `SafeCopyIn.CopyTestHooks` / `BarrierDeathException` (`SafeCopyIn.cs:43–56`)
- `JournalFileStorage.QualificationBeforeRename` (`JournalFileStorage.cs:33,88`)
- `FakeDomainClock` / `RelativeTestClock` / open `IDomainClock` constructor

A file/assembly hash deny list cannot exclude individual types/members.

**Correction**: Alternative A (compile-time exclusion via `GAGAMBA_PRODUCTION`
define constant on `Release` configuration) is recommended based on actual source
coupling — the hooks are isolated to specific types and call sites. Seven proof
obligations are specified as executable tests (type/member inspection,
environment enumeration, manifest deny list, test variant availability, identity
separation, regression suite, qualification record build configuration label).
The implementation boundary for IMPL-01 identifies exact source changes (csproj
define, `#if !GAGAMBA_PRODUCTION` wrappers, `Directory.Build.props`).
Bounded alternative (separate test adapter assembly) documented if A proves
infeasible.

### 4.3 F03 — SafeCopyIn wording correction

**Source behavior**: `CopyPlan` carries `SourceRootPath` string
(`CopyHost.cs:18,71,124`); `SafeCopyIn` opens the source root by path with
`O_NOFOLLOW` and `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`
(`SafeCopyIn.cs:73–85,289–313,424`). Only `StagingParentAuthority` is a
retained fd. The content is claimed-digest bound, but the source root is
reopened by path.

**Correction**: Design §5.2 step 6 wording changed from "copied from verified
source fds" to "copied from verified source paths with claimed digests under a
verified staging parent." The trust residual (root-only path replacement) is
documented. Implementation may add `SourceRootAuthority` fd in IMPL-01; not
required for the trust claim.

### 4.4 F04 — Native .NET runtime dependency closure

**Source behavior**: The artifact inventory (A21–A22) covers loader/glibc and
Bubblewrap shared libraries, but not the .NET runtime's own `dlopen`
dependencies. `ExperimentalRuntimePin.cs` verifies runtime directory inventory
but not the native libraries loaded by `GlobalizationNative.so`,
`System.Net.Security.Native.so`, `System.IO.Compression.Native.so`. No
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` setting is declared.

**Correction**: New §2.7 defines a four-row table classifying ICU, OpenSSL, zlib,
and native runtime loader components. For each: load mechanism, triggering
feature, file family, resolution source, deployment control, minimum platform
requirement, mandatory/conditional/excluded, independent observation method,
refusal behavior. Invariant globalization recommended to eliminate ICU
conditional dependency. Verification plan requires static `ldd`/`readelf`,
runtime `/proc/<pid>/maps`, controlled substitution, missing-library, and
roll-forward tests. No single `ldd` or trivial code path claimed exhaustive.

### 4.5 F05 — Legacy skipped-test accounting (covered by T25-A)

**Source behavior**: Plan T25 did not distinguish previously skipped cases from
new installed-closure cases.

**Correction**: T25-A enumerates the exact four skipped test configurations,
their source lines, skip reasons, and explicit disposition (all remain legacy
regressions, explicitly unqualified). T25-B defines new installed-closure tests
with no overlap.

### 4.6 F06 — Stricter ancestor predicate

**Source behavior**: `ExperimentalRuntimePin.AncestorsProtected` (`ExperimentalRuntimePin.cs:79–90`)
accepts root-owned ancestor with group/other write when sticky bit is set
(`owner==0 && (mode&0x200)`). The design §5.1 required "no group/other-writable
ancestors."

**Correction**: §5.2 step 1 specifies stricter predicate: every ancestor to `/`
must be root-owned (`uid==0`) AND not group-writable (`mode&020==0`) AND not
other-writable (`mode&002==0`). Sticky-bit exception dropped. Enforced at
admission (V04) and verified in T01/T04.

### 4.7 F07 — Installation-identity handshake wire specification

**Source behavior**: Design §6.1 named `InstallationIdentity` fields but no wire
IDs, payload bounds, direction, sequence, channel, or owner expected-identity
delivery. Existing protocol: `OwnerProtocolMessageType` max 7, `RecoveryMessageType` max 5
(`OwnerProtocol.cs`, `RecoveryProtocol.cs`).

**Correction**: New §6.2 defines:
- `RecoveryConfigV2` (bootstrap JSON, Version=2, required `InstallationIdentity`)
- `InstallationEchoRequest` (`RecoveryMessageType=6`, recovery→controller, ≤256 bytes binary)
- `InstallationEchoResponse` (`RecoveryMessageType=7`, controller→recovery)
- `OwnerInstallationEcho` (`OwnerProtocolMessageType=8`, owner→controller, same structure, Role=1)
- Framing: existing length-prefixed (4-byte BE + payload)
- Expected identity provenance: receiver has independent expected value from
  manifest/receipt; peer's claim is compared, not trusted
- Recovery restriction: no RELEASE authority derived; cross-generation fails
  conservatively
- Ordering: recovery echo before `Ready`; owner echo before `StartAuthority`
- Timeouts: 5s; no retries; replay/duplication/cancellation/cleanup specified

### 4.8 F08 — Minimal public descriptor surface

**Source behavior**: Proposed v2 descriptor included `Artifacts`, `Verification`,
`Platform` inventories (public).

**Correction**: §4.6 reduces public v2 to admission/refusal fields only:
`InstallationState`, `RefusalReason`+`InstallationDiagnostic`, `InstallationId`,
`ProfileId/Version`, `BootstrapVersion`, `RuntimeIdentifier`, `Architecture`,
`Protocol` constants, `QualificationId/Scope/EvidenceSha256/Limitations`,
`HostPrerequisites` booleans, `CaptureReadbackAvailable`. Per-file `Artifacts`,
`Verification`, full `Platform` remain internal until B7 consumer exists.

### 4.9 F09 — System.IO.Hashing fresh candidate inspection

**Source behavior**: Existing Release output shows `Microsoft.SourceLink.GitHub`
→ `System.IO.Hashing` in `deps.json` and `System.IO.Hashing.dll` present
(observed in `src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/`).
`Directory.Build.props` has `SourceLink PrivateAssets=all`.

**Correction**: Design §2.3 and §7.1 state the candidate publish must be
rechecked; the design rule (exclude build-only assets or manifest-pin every
deps-listed runtime asset) stands. T10 covers deps closure verification.
Qualification plan requires fresh publish inspection, not historical output.

### 4.10 F10 — Controller load-path-immaterial verification

**Source behavior**: `B3Qualification.cs:158–165` hashes `Assembly.Location` of
loaded owner/recovery assemblies against their role closures. The controller
provider is verified the same way.

**Correction**: Inventory table A1 "Resolution" column changed from "host loads
from install root" to "host loads assembly; verifier hashes `Assembly.Location`
against manifest provider role." §2.3 text corrected similarly. Load path is
immaterial; digest comparison is the verification.

### 4.11 F11 — Hypothetical recovery restart marked

**Source behavior**: Design §6.3 described "recovery restart after active
changed" as a current branch. `RecoveryHost.cs:148` has no reconnect listener;
no post-controller recovery relaunch exists.

**Correction**: §6.5 explicitly marks this as hypothetical: "No reconnect
listener or post-controller recovery relaunch exists today." The conservative
behavior (new recovery under new generation refuses old owner; owner self-stops)
is the actual mechanism.

### 4.12 F12 — /proc/self/fd/11 preserved

**Source behavior**: `AtomicCgroupSpawn.cs:500–515` duplicates CLOEXEC bwrap fd
with `F_DUPFD_CLOEXEC`, adds `dup2` to slot 11, closes from 12, `posix_spawn`s
`/proc/self/fd/11`. Pathname not used after open.

**Correction**: No change. Design already accurate. Q1-F12 recorded as
independently supported.

### 4.13 F13 — Qualification record association

**Source behavior**: Receipt had `expectedDigestSource` but no `qualificationId`
or `qualificationRecordSha256`. Qualifications directory used `<id>.json` with
no selection rule. Verifier had no defined precedence for zero/one/multiple
records.

**Correction**: §4.5 receipt adds `qualificationId` and `qualificationRecordSha256`.
Verifier selects by explicit `qualificationId` (no search), verifies record hash,
verifies `artifactSetSha256` matches `active`, verifies profile/RID/scope match,
verifies `buildConfiguration="Production"`. Nine required states defined with
deterministic `Unqualified` behavior for each mismatch. Fresh install is
`Installed/Unqualified`; qualification acceptance is separate trusted action.

### 4.14 F14 — First-install provenance label

**Source behavior**: §7.3 `install-candidate.sh --expected-digest` implied
independent expected value at first install.

**Correction**: §4.3 step 3: first install records
`expectedDigestSource="unverified-candidate"`; state is `Installed/Unqualified`.
Receipt updated to `expectedDigestSource="qualification-record"` only after
trusted qualification action. Descriptor never reports `Qualified` without
accepted qualification record.

---

## 5. Revised security assertions

| Assertion | Status | Evidence |
|---|---|---|
| Root-owned immutable installation generations | Preserved | §4.1, §5.2, §7.2 |
| Cryptographic exact-artifact-set identity | Preserved | §2.2, §4.3, §4.5 |
| Separately trusted qualification acceptance | Preserved + clarified | §4.3, §4.5, §4.13 |
| Verification before admission and RELEASE | Preserved | §5.2, §6.2, §8.1–8.4 |
| FD-anchored Bubblewrap execution | Preserved | §5.2 step 6, `AtomicCgroupSpawn` |
| Controller/owner/recovery authority separation | Preserved | §3.2, §6.1, §6.2 |
| No disk-only authority reconstruction | Preserved | §6.5, T16/T18 |
| Conservative refusal of uncertainty | Preserved | §8, all mismatch→Unqualified |
| No test-only code in qualified production closure | **Newly addressed** | §2.5, T24 proof obligations |
| Complete native loader dependency disposition | **Newly addressed** | §2.7, T20a |
| Wire-specified installation identity handshake | **Newly addressed** | §6.2, T20b |
| Deterministic qualification record selection | **Newly addressed** | §4.5, T20c |
| First install = Installed/Unqualified | **Newly addressed** | §4.3, §4.5, T20c |

---

## 6. Revised test coverage

| Test | Scope | Addresses |
|---|---|---|
| T01–T19 | As before | Baseline closure, substitution, refusal |
| T20 | Verification unavailable | F13/F14 (missing anchor) |
| T20a | Native runtime dependency closure | F04 |
| T20b | Installation-identity handshake | F07 |
| T20c | Qualification record association | F13, F14 |
| T21 | Malformed trust metadata | Baseline |
| T22 | Installed closure after restart | Baseline |
| T23 | Capture/readback component | Baseline |
| T24 | Production/test separation | F02 |
| T25-A | Historical skipped test accounting | F01, F05 |
| T25-B | Actual B6 installed closure | F01 |
| V01–V12 | Deterministic verifier suites | Baseline + V04 (stricter ancestor) |

All mandatory native cases on declared WSL2 x86-64 host are required `Passed`;
required cases that cannot run are `Pending` and block completion.

---

## 7. Open HD-1–HD-4 decisions (for owner approval)

### HD-1: Privileged WSL2 installation

- **Proposed decision**: Approve narrowly scoped `sudo` in a disposable WSL2
  instance for the root-owned install.
- **Exact privilege categories**: `mkdir -p /opt/gagamba`, `chown root:root`,
  `chmod 0755`, `cp -a` candidate tree, `chown -R root:root`, `chmod -R 0444/0555`,
  atomic `mv` for `active`, write `install-receipt.json`, write qualification
  record. No setuid helper, no daemon, runtime remains unprivileged.
- **Installation paths**: `/opt/gagamba/`, `/opt/gagamba/installations/<digest>/`,
  `/opt/gagamba/qualifications/`.
- **Ownership/mode**: All root:root; directories 0755; files 0444 (data) or 0555
  (executables); no symlinks/hardlinks/setuid/file caps.
- **Host-state effects**: Disposable WSL2 instance; no host-wide modification;
  instance cleaned at end.
- **Cleanup/rollback**: `remove-generation.sh` refuses active/held/in-use
  generations; residue recorded as `Unknown`.
- **Security consequence**: Root/admin is documented trust anchor; target cannot
  write install tree. Residual root-only replacement race documented, not hidden.
- **Implementation consequence**: Install/remove/qualify scripts run as root;
  candidate build remains unprivileged.
- **Required owner approval**: Yes (blocks native Qualified cases if not approved).
- **Blocks IMPL-01**: Yes — without root install, native Qualified is Pending.

### HD-2: Descriptor exposure

- **Proposed decision**: Approve only minimal public v2 surface
  (`InstallationState`, `RefusalReason`+`InstallationDiagnostic`,
  `InstallationId`, `ProfileId/Version`, `BootstrapVersion`,
  `RuntimeIdentifier`, `Architecture`, `Protocol` constants,
  `QualificationId/Scope/EvidenceSha256/Limitations`,
  `HostPrerequisites`, `CaptureReadbackAvailable`). Keep per-file `Artifacts`,
  `Verification`, full `Platform` internal until B7.
- **Alternatives**: Full v2 public (adds compatibility burden, no B6 consumer);
  defer all to internal (no public admission reporting).
- **Security consequence**: Smaller public API surface reduces compatibility
  burden; no authority change.
- **Implementation consequence**: Internal descriptor projects to public minimal
  fields; no public factory/constructor.
- **Required owner approval**: Yes (or record deferral).
- **Blocks IMPL-01**: No — can implement internal first, public decision deferred.

### HD-3: Platform scope

- **Proposed decision**: Confirm B6 initial qualification is WSL2 x86-64 only;
  native Linux-host qualification deferred to B5-L01/B7; Arm64 unsupported
  until matching seccomp filter/build exists.
- **Alternatives**: Attempt native Linux-host in B6 (overclaims B5 scope);
  add Arm64 filter/build now (expands scope without qualification).
- **Security consequence**: Matches accepted B5 scope and x86-64-only seccomp
  policy; avoids overclaim from generic Linux capability check.
- **Implementation consequence**: No arm64 claim; qualification runs in WSL2;
  evidence labels platform explicitly.
- **Required owner approval**: Yes (or record deferral).
- **Blocks IMPL-01**: No — WSL2 is the declared scope.

### HD-4: Generation retention

- **Proposed decision**: Retain immutable generations until explicit removal;
  removal refuses active, HELD, or known in-use generations; if retained
  ownership cannot be established, return `Unknown` and refuse removal. No
  auto-GC. No support for controller restart/re-adoption that does not exist.
- **Alternatives**: Auto-GC after timeout (risks deleting mapped files); allow
  removal of active with force (breaks running executions).
- **Security consequence**: Preserves active-generation pre-RELEASE policy;
  prevents deleting files still mapped by surviving execution; conservative
  cleanup.
- **Implementation consequence**: Removal script checks active pointer and
  holders; operator cleanup procedure documented.
- **Required owner approval**: Yes (or record deferral).
- **Blocks IMPL-01**: No — retention policy is a runtime behavior.

---

## 8. Unresolved implementation dependencies

1. **Production/build separation** (F02): Requires csproj/Directory.Build.props
   changes to define `GAGAMBA_PRODUCTION` for `Release` and wrap test hooks in
   `#if !GAGAMBA_PRODUCTION`. Exact types/members listed in §2.5 design.
   Bounded alternative (separate test adapter assembly) if compile-time exclusion
   breaks qualification testability.

2. **Source-root fd authority** (F03): `CopyPlan`/`SafeCopyIn` may add
   `SourceRootAuthority` fd in IMPL-01; not required for current trust claim.

3. **Invariant globalization decision** (F04): If owner chooses non-invariant,
   ICU version becomes mandatory platform-baseline entry with qualification
   limitation.

4. **Wire protocol implementation** (F07): New message IDs (`RecoveryMessageType=6,7`,
   `OwnerProtocolMessageType=8`) must be allocated in `RecoveryProtocol.cs` and
   `OwnerProtocol.cs` without collision.

5. **Qualification record installation** (F13): `qualify-install.sh` must write
   root-owned record with `acceptedAtUtc` from trusted owner action; not
   self-generated.

---

## 9. Independent Q2 handoff

**Review focus**: Verify that the five major Q1 findings (F01, F02, F04, F07, F13)
and bounded findings (F03, F05, F06, F08–F11, F14) are addressed in the design
and plan with source-backed corrections. Confirm no new material contradiction
is introduced. Evaluate HD-1–HD-4 as explicit decisions for owner, not assumed.

**Documents to review**:
- `docs/design/rc2b-06-b6-installed-artifact-trust.md` (amended)
- `docs/plans/rc2b-06-b6-qualification.md` (amended)
- This remediation evidence artifact

**Reviewer must**:
1. Verify exact baseline and review commit ancestry.
2. Confirm unmodified accepted production/source trees.
3. Check that every correction cites source behavior or accepted contract.
4. Validate that T25-A/T25-B, T24, T20a/T20b/T20c are implementable and testable.
5. Confirm HD-1–HD-4 are clearly identified for owner decision.
6. Verify no implementation, installation, or test execution is claimed.

**Starting Git identity for Q2 review**:
- Original specification: `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` (tree `6e25ac365cc7a3d5f761dd0ff5306af2e5b60fe7`)
- Q1 review and R1 parent baseline: `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` (tree `630b803d57e27a0044686595e108868072aa20ea`)
- R1 remediation: `77ee4414df37b9f88979bde02742756538ba45af` (tree `62736d355e2650b5f40cdc3d904ed49466cbf025`)

---

## 10. Final disposition

**DISPOSITION: READY FOR FOCUSED Q2 REVIEW**

All five major Q1 findings are addressed in the design and plan; bounded
findings are corrected; HD-1–HD-4 remain clearly identified for owner decision.
No major finding remains unresolved. No proposed correction introduces a new
material contradiction.

| Finding | Status | Evidence |
|---|---|---|
| B6-Q1-F01 | **Addressed** | T25 split; T25-A accounting; T25-B new closure tests |
| B6-Q1-F02 | **Addressed** | Production/test separation design (Alt A); T24 executable proof obligations |
| B6-Q1-F03 | **Addressed** | SafeCopyIn wording corrected to claimed-digest path copies |
| B6-Q1-F04 | **Addressed** | §2.7 native dependency table; T20a static+runtime+substitution tests |
| B6-Q1-F05 | **Addressed** | T25-A enumerates exact skipped cases and disposition |
| B6-Q1-F06 | **Addressed** | Stricter ancestor predicate; sticky exception dropped |
| B6-Q1-F07 | **Addressed** | §6.2 wire spec with IDs, payloads, ordering, provenance, restrictions |
| B6-Q1-F08 | **Addressed** | Minimal public v2 surface; diagnostics internal |
| B6-Q1-F09 | **Addressed** | Fresh candidate publish inspection required; T10 covers deps closure |
| B6-Q1-F10 | **Addressed** | Load-path-immaterial digest comparison wording |
| B6-Q1-F11 | **Addressed** | Hypothetical recovery restart explicitly marked |
| B6-Q1-F12 | **Preserved** | No defect; fd-11 semantics correct |
| B6-Q1-F13 | **Addressed** | Receipt adds qualificationId+recordSha256; explicit selection; 9 states |
| B6-Q1-F14 | **Addressed** | First install = Installed/Unqualified; unverified-candidate label |

The design is precise enough for independent verification and bounded
implementation authorization (after HD-1–HD-4 owner decisions).