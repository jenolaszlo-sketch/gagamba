# WP05 Seccomp Clone-Tail Remediation

Date: 2026-10-19
Status: IMPLEMENTED - INDEPENDENT SECURITY REQUALIFICATION REQUIRED
Scope: bounded remediation of the independently preserved WP05 seccomp counterexample

## 1. Repository identity

- Repository: Gagamba
- Remediation branch: `remediation/wp05-seccomp-clone-tail`
- Accepted production baseline: `c4b80db83e52a5125c022c0e217913654ebe3421`
- WP05 implementation base: `b134cb33a2d1ca1fa25a284b5ae9de18f1de4a6d`
- Reviewer Q1 probe (authoritative on `review/wp05-release-seccomp-counterexample`): `99dc8c257088ab5fb792e54e87447f058cfce5e2`, imported unchanged by cherry-pick as `b65d829`
- Reviewer Q2 evidence/review: `712e0fc2dc56c0b14f12ca75c7d48137dae8a500`, imported unchanged by cherry-pick as `6499ec3`
- R2 generator correction: `e77a53bcb57b0718b5b87d78b9a22c8a9520fe8a`
- R3 regenerated filter and pins: `b225fe4830ad4d1f037297c79137c495cd8cdc20`
- R4 regression tests: `6fe73fad9ffa6a8883d3f1feaea0cb8251dc68e1`
- R5: this evidence document and tracker updates
- Production release/coordinator sources were not modified. The production change is limited to the seccomp generator, its regenerated `.hex`, the adapter/build pins, and policy tests.

## 2. Verified original defect

Authoritative policy source: `native/linux-m1/seccomp_filter_policy.py`.
Generated artifact: `native/linux-m1/seccomp-filter.hex`, decoded by the native build into `artifacts/linux-m1/wp04/seccomp-filter.bin` (the `artifacts/` tree is gitignored; the build recreates it).

The historical pinned artifact was 672 bytes, SHA-256
`4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`.
Decoding the checked-in `.hex` and the generator's `--binary` output both reproduced those exact bytes. Instruction numbering was identical in source, `.hex` and `.bin`.

Relevant control flow (instruction index: opcode `jt`/`jf` constant -> targets):

| Index | Instruction | Meaning | True | False |
|---:|---|---|---:|---:|
| 0 | `LD k=4` | load `seccomp_data.arch` | 1 | 1 |
| 1 | `JEQ k=0xC000003E jt=1 jf=0` | require x86-64 | 3 | 2 |
| 2 | `RET KILL_THREAD` | reject other architecture | 3 | 3 |
| 3 | `LD k=0` | load syscall number | 4 | 4 |
| 4 | `JGE k=0x40000000 jf=1` | reject x32 | 5 | 6 |
| 5 | `RET EPERM` | x32 denial | 6 | 6 |
| 46-47 | `JEQ clone3 -> RET ENOSYS` | clone3 unavailable | - | - |
| 48-64 | constrained `arch_prctl`/`prctl` checks | exact startup operations | 83/52/65 | 64 |
| 65-78 | constrained `close_range` check | exact descriptor closure | 83/66.. | 79 |
| **79** | **`JEQ k=56 jf=1`** | **clone dispatch** | **80** | **81 (defect)** |
| 80 | `LD k=16` | load clone flags | 81 | 81 |
| 81 | `JSET k=0x7E020080 jf=1` | namespace mask test | 82 | 83 |
| 82 | `RET EPERM` | deny namespace-creating clone | 83 | 83 |
| 83 | `RET ALLOW` | default allow | 84 | 84 |

Defect: for a non-clone syscall, instruction 79 took `jf=1` into instruction 81 without executing instruction 80. The accumulator still held the syscall number, so instruction 81 tested `syscall_number & 0x7E020080`. Any syscall number overlapping the mask was denied with EPERM. The architecturally intended path was the default ALLOW at instruction 83 (`jf=3`).

Verified dispositions of the historical artifact (offline classic-BPF evaluation of the actual pinned bytes, re-confirmed by kernel enforcement in section 5):

| Probe | Result |
|---|---|
| `read` (0), `write` (1), `exit` (60) | ALLOW |
| `exit_group` (231), `futex` (202), `set_tid_address` (218), `gettid` (186), `tgkill` (234) | EPERM |
| `clone` flags 0 / `CLONE_VM|CLONE_VFORK` | ALLOW |
| `clone` with `CLONE_NEWUSER`, `CLONE_NEWNS|CLONE_NEWNET`, `CLONE_NEWPID|CLONE_NEWUSER|CLONE_NEWUTS` | EPERM |
| `clone3` | ENOSYS |
| explicit denials (`socket`, `mount`, `unshare`, `setns`, ...) | EPERM |
| constrained `arch_prctl`/`prctl`/`close_range` | unchanged exact allow/deny |
| wrong architecture / x32 number | KILL_THREAD / EPERM |

A sweep of syscall numbers 0..1023 on the historical artifact versus the corrected artifact showed 495 changed decisions, every one EPERM -> ALLOW, all on non-clone numbers whose bits intersect the namespace mask. No previously allowed or explicitly denied decision changed in the other direction.

The independent reviewer's strace evidence (read of the 128-byte RELEASE frame, `close(4)`, `exit_group(0) = -1 EPERM`, `SIGSEGV`, wrapper value 139) is preserved in `docs/evidence/linux-m1/wp05-release-seccomp-counterexample-2026-10-18.md`. This remediation reproduced the kernel-level denial and termination directly (section 5) rather than relying on that summary.

## 3. Correction

The generator already computes tail offsets structurally (`arch_prctl_pc`, `prctl_pc`, `close_range_pc`, `allow_pc`). The clone dispatch was the sole hard-coded false jump. The smallest correction consistent with that organization replaces the hard-coded `jf=1` with a structural rewrite after `allow_pc` is known:

```python
p[clone_pc] = Instruction(
    BPF_JMP_JEQ_K,
    jf=allow_pc - clone_pc - 1,
    k=SYS_CLONE,
    note="match clone; non-clone syscalls continue to the default ALLOW path",
)
```

Effective byte-level change: instruction 79 `jf` 1 -> 3 (target 81 -> 83). The comment that previously described the syscall-number JSET fallthrough as a preserved disposition was corrected to describe the default-ALLOW path. No allowlist entries were added, no mask changed, and no policy was widened beyond the demonstrated control-flow fix.

## 4. Artifact identity

| | Historical | Corrected |
|---|---:|---:|
| Size | 672 bytes | 672 bytes |
| Raw SHA-256 | `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |
| Instruction count | 84 | 84 |
| Instruction 79 | `JEQ k=56 jt=0 jf=1` | `JEQ k=56 jt=0 jf=3` |
| Byte delta | - | exactly one byte, offset 635 (`0x01` -> `0x03`) |

Generation and binding:

- `python3 native/linux-m1/seccomp_filter_policy.py --write` regenerated `native/linux-m1/seccomp-filter.hex`.
- `python3 native/linux-m1/seccomp_filter_policy.py --binary > artifacts/linux-m1/wp04/seccomp-filter.bin` regenerated the raw artifact.
- The corrected generator output hashes to `2aa87901...` and is byte-for-byte identical to the independent reviewer's experimental one-byte filter (`/tmp/fixed-filter.bin` in the preserved review; diagnostic only, never committed).
- `eng/linux-m1/build-native.sh` now pins `2aa87901...`; the authoritative Linux build ran `--check`, regenerated the filter, verified the new digest, and rebuilt `exec-coordinator` and `wp05-qualification-coordinator` byte-identically to their historical hashes (`5e55e92b...`, `e20903cc...`). The fixture remained `ee25a876...`.
- `src/Gagamba.Execution.Linux.M1/Launch/BubblewrapAdapter.cs` `FilterSha256` now pins `2aa87901...`; `FilterLength` remains 672.
- The local (gitignored) `artifacts/linux-m1/wp04/SHA256SUMS` filter line was updated with the computed hash.
- `native/linux-m1/test_seccomp_filter_policy.py` binds generator -> checked-in `.hex` -> built `.bin` (when present) -> adapter pin -> build-script pin, so drift between any of them fails the suite.

## 5. Security regression matrix

Native kernel-enforced controls (Ubuntu 26.04.1, kernel 6.6.114.1-microsoft-standard-WSL2, delegated cgroup, uid 1000, empty `CapEff`, Bubblewrap 0.11.1, corrected filter installed via `--seccomp`):

Positive controls (expected success):

| Check | Result |
|---|---|
| `gettid`, `set_tid_address`, `futex(FUTEX_WAKE)` | rc=0; values returned, no errno |
| direct `exit_group(0)` | rc=0 |
| `fork()` (clone `SIGCHLD`) and child reaping | rc=0 |
| `pthread_create`/`pthread_join` | rc=0 (return code 0; printed stale errno 22 is not a failure) |
| `close_range(6, UINT_MAX, 0)` exact form | rc=0 |
| `prctl(PR_SET_DUMPABLE, 0)` and `PR_GET_DUMPABLE` | rc=0 |
| direct `exit(60)` | rc=60 |

Negative controls (expected denial preserved):

| Check | Expected | Result |
|---|---|---|
| `clone(CLONE_NEWUSER)` | EPERM | EPERM |
| `clone3` | ENOSYS | ENOSYS (38) |
| `unshare(CLONE_NEWNS)` | EPERM | EPERM |
| `setns` | EPERM | EPERM |
| `arch_prctl(ARCH_GET_FS)` | EPERM | EPERM |
| `prctl(PR_SET_DUMPABLE, 1)` | EPERM | EPERM |
| non-exact `close_range` | EPERM | EPERM |
| `socket` | EPERM | EPERM |
| x32 syscall number | EPERM | EPERM |

Historical-filter differential under the same wrapper:

| Check | Historical result | Corrected result |
|---|---|---|
| `gettid`/`set_tid_address`/`futex` | rc=1 process failure; all three EPERM; wrapper termination 139 | rc=0; all three succeed |
| direct `exit_group(0)` | wrapper termination 139 | rc=0 |

The historical bytes were reconstructed from `b134cb3:native/linux-m1/seccomp-filter.hex`, decoded, and hash-verified against `4ee63c48...` before use. An earlier attempt to read the filter from the 9p Windows mount failed before Bubblewrap installed it; that run was discarded, not reported as evidence.

Architecture handling remains unchanged: wrong audit architecture still returns `KILL_THREAD`; x32 numbers remain EPERM. The previously deferred wrong-architecture generator concern (`KILL_THREAD` vs historical `KILL_PROCESS`) is not touched by this remediation and remains a separate maintenance decision.

Limitations: only the enumerated ordinary and hostile syscalls were exercised on the kernel; the exhaustive 0..2047 disposition sweep is interpreter-level against the generated artifact. No new target fork/exec exists (WP10 boundary).

## 6. Test results

Portable policy suite (`native/linux-m1/test_seccomp_filter_policy.py -v`): **9/9 passed** on Windows and **9/9 passed** on Linux. The two new semantic regressions fail against the historical binary (`EPERM != ALLOW` for the affected numbers), proving they detect the original defect; the artifact-binding test fails on pin/artifact drift.

Linux 529-test assembly, delegated non-root profile (authoritative) and results:

| Run | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| Focused seccomp/release/WP04 selection | 83 | 0 | 0 | includes `SeccompFilterPinTests`, release frame/arbiter/status, native release protocol |
| `NonRootQualificationTests` | 31 | 0 | 0 | includes native WP04 HELD/abort positive control |
| Q1 probe, standalone run A | 3 | 0 | 0 | Fact 1, Fact 2, Fact 3 all passed |
| Q1 probe, standalone run B | 3 | 0 | 0 | all passed again |
| Full assembly, serialized | 493 | 1 | 35 | failure = Q1 Fact 2 abort-exit race |
| Full assembly, parallel run 1 | 491 | 3 | 35 | Q1 Facts 2/3 + NonRoot mode 2 |
| Full assembly, parallel run 2 | 491 | 3 | 35 | same class |
| Full assembly, parallel run 3 | 492 | 2 | 35 | Q1 Fact 2 + NonRoot mode 2 |

Linux 529-test assembly, root supplemental profile: **495 passed, 0 failed, 34 skipped** (the complement: delegated non-root rows skip as root).

Stability runs of the three parallel/serial failure sites (isolated):
- Q1 `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport`: 7/8 passed. Failure is the final exit-code assertion at `Wp05ReleasePathIndependentQualificationTests.cs:125`, which accepts 133 or 139 but not the SIGKILL 137 that occurs when the domain stop wins the abort race. Fact 1 (release to pre-exec) passed in every run.
- Q1 `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent`: 6/6 passed isolated; passed in the serialized full run; failed only in parallel full runs with `waitid(P_PIDFD) errno 10` (ECHILD).
- `NonRootWp04CoordinatorProtocolNegativesRefuseAndReconcile`: 5/5 isolated and 7/7 in its own suite; fails only in parallel full runs with "adopted children did not become reapable before the bound".

Classification: none of these are seccomp-policy regressions. Q1 Fact 2 is a preserved reviewer-probe race whose accepted exit-code set omits the SIGKILL outcome; Q1 Fact 3 and NonRoot mode 2 are parallel-load harness/timing flakes that pass deterministically in isolation and (for Fact 3) serialized. Per the remediation directive the reviewer probe expectations were not edited and production coordinator logic was not changed.

Windows: full solution Release build **0 warnings / 0 errors**; M1 assembly on Windows **366 passed, 0 failed, 163 skipped** (529 total; the three imported Q1 Facts and Linux-only rows skip).

Skip classification: 35 non-root-profile skips are the pre-existing root-only gates (21 `DomainAuthorityTests`/`RecoveryHarness` rows that create probe cgroups directly under `/sys/fs/cgroup`, 14 `IntegratedBarrierTests` rows that also require openat2). The root supplemental run executes them (34 complementary non-root rows skip). No new skips were introduced by this remediation.

## 7. Environment

- Host: Windows 11, WSL2 Ubuntu 26.04.1 LTS
- Kernel: `6.6.114.1-microsoft-standard-WSL2` x86_64
- Boot ID at test time: `c4bfa0c4-7867-4393-aded-9ba52b40a5ab`
- (Preparation earlier in the session ran under boot `5e61b980-ae40-4775-a45d-b7dad81293b5`; WSL restarted between preparation and testing. WSL was operational for all reported test runs; no `CreateInstance/E_FAIL` occurred during this remediation.)
- Delegated parent: `/sys/fs/cgroup/gaga-delegated` (cgroup2, `memory` and `pids` delegated; exec process in `gaga-delegated/host`)
- Test identity: uid/gid 1000, `CapEff=0000000000000000`, no new privileges; non-root.
- Bubblewrap: 0.11.1, `/usr/bin/bwrap` SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` (matches the accepted pin)
- Toolchain: dotnet 10.0.112, Python 3.14.4, gcc 15.2.0, glibc 2.43
- Linux workspace: `/home/gaga/wp05-qual-src` (fresh copy of the remediation tree; artifacts rebuilt there)

## 8. Frozen contracts and boundaries

- Canonical G2 manifest: `ManifestTests.CanonicalGoldenBytes` passed; 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`. The manifest golden uses synthetic digests and was not regenerated.
- WP05 RELEASE frame: `ReleaseFrameTests.GoldenReleaseFrameIsExactly128Bytes` passed; 128 bytes, SHA-256 `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B`.
- Durable ownership schema remains v3; v3 codec/transition/qualification tests passed; no v4 and no second journal.
- No RELEASE transport redesign, no G2 topology change, no new target execution. The Q1 probe still stops at the WP05 pre-exec boundary.
- The original reviewer evidence and review are preserved byte-for-byte; the historical filter hash remains in historical records and was not rewritten.

## 9. Required follow-up

1. Independent security-policy requalification of the corrected filter (required before WP05 release qualification resumes).
2. WP05 production coordinator end-to-end release qualification (Fact 1 now passes to the pre-exec boundary; owner integration remains separate).
3. Fresh-process release/crash-matrix qualification.
4. Native WP04 HELD/abort regression passed in this environment (31/31 non-root suite); retain as implementation evidence, subject to independent confirmation.
5. Production-owner integration/scope decision remains open.
6. Preserved Q1 Fact 2 exit-code race (133/139 vs SIGKILL 137) and the parallel-load flakes are recorded; they are not product-seccomp failures and were not repaired here.
7. The deferred wrong-audit-architecture generator decision remains separate and unchanged.

No WP05 acceptance is claimed.
