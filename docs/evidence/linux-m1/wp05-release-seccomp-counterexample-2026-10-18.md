# WP05 Release-Protocol Seccomp Counterexample

Date: 2026-10-18
Status: INDEPENDENT QUALIFICATION STOPPED - PRODUCT DEFECT CONFIRMED

## 1. Repository and revision binding

- Repository: Gagamba
- Branch: `review/wp05-release-seccomp-counterexample` (based on `qualification/ar7-feasibility`)
- Implementation HEAD (WP05 release protocol): `b134cb33a2d1ca1fa25a284b5ae9de18f1de4a6d`
- Accepted pre-implementation baseline: `c4b80db83e52a5125c022c0e217913654ebe3421`
- WP05 managed implementation commit: `4bd2f3a` (managed arbitration/release/status)
- WP05 native implementation commit: `08cda01` (no-target native RELEASE coordinator/tests)
- Q1 probe commit: `99dc8c2` (reviewer-authored, on the review branch)
- Production source diff against `b134cb33a2d1ca1fa25a284b5ae9de18f1de4a6d`: empty (no production code modified during investigation)
- Untracked qualification artifacts (test results, spikes, strace logs) were NOT committed; only the probe source and evidence documents are preserved.

## 2. Environment

- OS: Windows 11 (host), WSL2 Ubuntu, kernel 6.6.114.1-microsoft-standard-WSL2
- Delegated cgroup: `/sys/fs/cgroup/gaga-delegated` (uid 1000 / gaga)
- Qualification runner: `spikes/cgexec/cgexec` (runs as root, drops to uid 1000 in delegated cgroup)
- Environment variables: `GAGAMBA_DELEGATED_CGROUP`, `GAGAMBA_REPOSITORY_ROOT=/tmp/wp05-qual-src`, `GAGAMBA_OWNER_DLL`
- .NET 10.0.12, xUnit 3.1.4, VSTest 18.0.2
- Bubblewrap 0.11.1, `/usr/bin/bwrap`
- Helper: `HelperInstallation.PinnedSha256` (seccomp 672 bytes)

## 3. Q1 independent qualification probe

File: `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs`
(585 lines, 3 Facts, committed at `99dc8c2`)

The probe composes the full production release chain in-process via `BubblewrapAdapter.PrepareHeldAsync` (internal, directly callable; the production owner program has no integrated release wiring). It mirrors the real owner sequence: spec -> intent -> ScopedAllocationContext -> TryCreateDomain -> DomainLease -> MirrorRegistered -> Wp04 limits -> SafeCopyIn -> manifest/identity -> prepared snapshot -> PreparationPublisher.Publish -> PrepareHeldAsync -> ReleaseArbiter.

### Fact 1: `HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary`

- Expected: ReleaseArbiter decision = Released/Delivered/DurableCommitted=true; duplicate = Duplicate; journal ReleaseTag = Committed; coordinator exit code = 0 (pre-exec boundary); status drain = no frame.
- Observed: ReleaseArbiter decision = Released/Delivered/DurableCommitted=true (passed); duplicate = Duplicate (passed); journal committed (passed); coordinator exit code = **139** (SIGSEGV) instead of 0 (FAILED); status drain = no frame (passed).
- Root cause: seccomp filter denies `exit_group` (syscall 231).

### Fact 2: `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport`

- Expected: ReleaseArbiter decision = Refused/NotCommitted/NotAttempted; capability remains live; journal stays at Prepared (no Release); coordinator exit = 133 (abort path) or 139.
- Observed: ReleaseArbiter decision = Refused (passed); capability live (passed); journal at Prepared (passed); coordinator exit = 137 (SIGKILL, from domain.StopAndAwaitEmpty) — within the accepted range.
- Status: PASSED (all assertions pass).

### Fact 3: `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent`

- Expected: coordinator exit = 130 (fail(30) = 120 + 30%20 = 130); status frame = ProtocolFailed(30); arbiter refuses expired intent; journal has no Release.
- Observed: coordinator exit = **139** (SIGSEGV) instead of 130 (FAILED); status frame = ProtocolFailed(30) (passed); arbiter refuses (passed); journal no Release (passed).
- Root cause: seccomp filter denies `exit_group` (231) and `futex` (202) — the coordinator calls fail(30) which returns 130, then main returns, C runtime calls exit_group(130) which is denied -> SIGSEGV.

## 4. Seccomp counterexample

### Pinned filter identity

- Artifact: `seccomp-filter.bin` (also referenced as `artifacts/linux-m1/wp04/seccomp-filter.bin`)
- Size: 672 bytes (84 BPF instructions)
- SHA-256: `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`
- Generator: `native/linux-m1/seccomp_filter_policy.py` (GENERATOR_VERSION = 4)
- Generator output (`.hex`): SHA-256 `c476fbc01094814f1d39cff2230331f19dfa93cd8039c1b7a5e55c1d9a4a9fd7` (different from pinned .bin)
- The pinned .bin and the generator output both contain the same defect.

### Instruction layout (relevant tail)

```
078: RET K -> ERRNO(1)          (close_range deny)
079: JMP JEQ k=56 (clone) jt=0 jf=1   (true->80 false->81)
080: LD abs k=16                (load clone flags -- only reached for clone)
081: JMP JSET k=0x7E020080 jt=0 jf=1  (true->82 false->83)
082: RET K -> ERRNO(1)          (deny)
083: RET K -> ALLOW             (allow)
```

### Root cause

For clone (syscall 56), instruction 079 is true -> 080 (load args[0] = clone flags) -> 081 (JSET namespace mask) -> if namespace flags set, deny; else allow.

For non-clone syscalls (e.g., exit_group = 231), instruction 079 is false -> jf=1 -> 081 (JSET). But instruction 080 (LD clone flags) was skipped, so the accumulator still holds the syscall number (231), not clone flags. The JSET then tests `(syscall_nr & 0x7E020080) != 0`. For exit_group (231 = 0xE7), `0xE7 & 0x80 = 0x80 != 0` -> true -> 082 (deny/EPERM).

This denies any non-clone syscall whose number has bit 7 (0x80) set.

### Affected syscalls (observed)

| Syscall | Number | Hex | Bit 7 set? | Observed |
|---------|--------|-----|------------|----------|
| exit_group | 231 | 0xE7 | Yes | EPERM -> SIGSEGV |
| futex | 202 | 0xCA | Yes | EPERM |
| set_tid_address | 218 | 0xDA | Yes | EPERM |
| gettid | 186 | 0xBA | Yes | EPERM |
| tgkill | 234 | 0xEA | Yes | EPERM |

Syscalls without bit 7 set (read=0, write=1, exit=60) are allowed.

### Strace evidence (release path)

```
12787 poll([{fd=4, events=POLLIN}], 1, 250) = 1 ([{fd=4, revents=POLLIN}])
12787 read(4, "GGM1CTL\0...", 128) = 128
12787 poll([{fd=4, events=POLLIN}], 1, 0) = 0 (Timeout)
12787 close(4) = 0
12787 exit_group(0) = -1 EPERM (Operation not permitted)
12787 --- SIGSEGV {si_signo=SIGSEGV, si_code=SI_KERNEL, si_addr=NULL} ---
12787 +++ killed by SIGSEGV +++
12786 <... wait4 resumed>, [{WIFSIGNALED(s) && WTERMSIG(s) == SIGSEGV}], 0, NULL) = 2
```

The managed reaper (`PidFdChildReaper`) reported exit code 139 = 128 + 11 (SIGSEGV). This is a signal-derived value (WIFSIGNALED), not a normal exit status. Bubblewrap propagated the coordinator's SIGSEGV as its own exit status.

### Differential control (experimental fixed filter)

- Original filter: `seccomp-filter.bin`, sha256 `4ee63c48…`, jf=1 at instruction 79.
- Experimental filter: `/tmp/fixed-filter.bin` (disposable, not committed), jf=3 at instruction 79 (all other bytes identical).
- Exact difference: 1 byte changed (offset 635: 0x01 -> 0x03).
- Original behavior: `/tmp/t` (static glibc, return 0) under bwrap in user namespace -> exit 139 (SIGSEGV). exit_group, futex, set_tid_address, gettid, tgkill all return EPERM.
- Experimental behavior: `/tmp/t` under bwrap in user namespace -> exit 0. set_tid_address and gettid return valid results. exit_group succeeds (process exits cleanly).
- Commands: `bwrap --ro-bind / / --unshare-user --unshare-pid --uid 0 --gid 0 --seccomp 3 -- /tmp/t 3< <filter>`
- Environment: WSL2 Ubuntu, kernel 6.6.114, Bubblewrap 0.11.1, delegated cgroup.
- Limitation: the experimental filter was NOT independently verified to preserve all intended security restrictions. Only the trivial-executable exit path was tested. The implementer must verify that prohibited namespace-creating clone operations remain denied.

### Interpretation vs. observation

Directly observed:
- exit_group(0) returns EPERM (strace).
- Subsequent SIGSEGV (strace, si_code=SI_KERNEL).
- Managed reaper reports 139 (WIFSIGNALED, SIGSEGV).
- BPF bytecode at instruction 79 has jf=1, causing non-clone syscalls to reach JSET.
- Syscalls with bit 7 set return EPERM under the filter.
- Experimental filter (jf=3) allows clean exit.

Inferred (not directly proven):
- That ALL syscalls with bit 7 set are denied (bytecode supports this, but only 5 were tested).
- That the fix (jf=3) preserves all intended security restrictions (not verified).
- That the .hex generator output has the same defect (bytecode inspection confirms jf=1, but runtime test was only on .bin).

## 5. Production-owner integration boundary

- The Q1 probe composes production components through internal APIs (`PrepareHeldAsync`), NOT through the production owner program.
- The production owner program (`src/Gagamba.Execution.Linux.M1.Owner/Program.cs`) has no integrated release wiring (zero "release" mentions). It aborts held executions immediately after HELD.
- This means the Q1 probe exercises the coordinator and arbiter but NOT the owner's release orchestration.
- This is a scope boundary, not a confirmed second product defect. It must be checked against the authoritative WP05 implementation plan.

## 6. WSL environment failure

- After extensive strace investigation, WSL2 reported `CreateInstance/E_FAIL` (error code 6).
- Last successful state: WSL2 running, delegated cgroup available, qualification tests executing.
- First failure: during the final probe rebuild attempt, WSL stopped responding and could not be restarted.
- This is an environment failure, NOT the seccomp product defect.
- No WSL rebuild was attempted (per instructions: do not force rebuild).
- Any qualification rows that could not execute due to WSL unavailability remain unexecuted/inconclusive.

## 7. Missing evidence

- Raw strace logs were deleted to free /tmp space (tmpfs was full). Key excerpts are preserved in this document.
- The experimental filter (`/tmp/fixed-filter.bin`) exists only in the now-unavailable WSL /tmp; its digest was not recorded. The 1-byte difference (offset 635: 0x01 -> 0x03) is documented.
- No core dumps were captured (ulimit -c was 0 during initial runs).
- The /tmp/t static executable exists only in WSL /tmp (unavailable).

## 8. Required remediation (for implementer, not qualifier)

### A. Seccomp remediation
- Review generator control flow in `seccomp_filter_policy.py`.
- Fix the clone-tail jump: change `jf=1` to `jf=3` at the clone JEQ (instruction 79).
- Regenerate the pinned filter (both `.bin` and `.hex`).
- Rebind binary size/hash (size should remain 672 bytes; hash will change).
- Verify permitted ordinary syscalls (exit_group, futex, set_tid_address, gettid, tgkill, etc.) are allowed.
- Verify prohibited namespace-creating clone operations remain prohibited.
- Independently qualify the revised security policy.
- Keep the wrong-audit-architecture seccomp-generator issue visible as a distinct concern.

### B. WP05 release qualification (after seccomp fix)
- Reproduce the two failing coordinator cases (release + expiry) with corrected filter.
- Run production coordinator release to pre-exec boundary.
- Qualify owner integration per authoritative plan.
- Run fresh-process crash barriers.
- Qualify release/stop races and no-resend.
- Rerun native WP04 HELD/abort.
- Confirm no real target execution.
- Complete independent acceptance.

### C. Environment
- Restore a valid non-root delegated Linux qualification host.
- Record the new environment and boot identity.
- Do not silently transfer qualification claims between environments.

## 9. Cleanup status

- No production code was modified.
- No production artifacts were modified.
- The seccomp filter, policy generator, and pinned SHA remain unchanged.
- The probe source is committed (commit `99dc8c2`).
- WSL2 environment is broken (not repaired, per instructions).
- Temporary artifacts (strace logs, /tmp/t, fixed filter) were in WSL /tmp and are unavailable.
PY