# WP05 Release-Protocol Seccomp Counterexample Review

Date: 2026-10-18

## Disposition

**WP05 SECCOMP COUNTEREXAMPLE PRESERVED - BOUNDED REMEDIATION REQUIRED**

Independent WP05 qualification STOPPED. The production coordinator's release and expiry paths fail under the pinned seccomp filter. WP05 acceptance remains BLOCKED.

## Summary

The independent qualification of the WP05 release-protocol tranche discovered a product defect in the pinned seccomp filter (`seccomp-filter.bin`, sha256 `4ee63c48…`). The filter's clone-tail namespace-flag check has an incorrect jump target (`jf=1` instead of `jf=3`), causing non-clone syscalls with bit 7 set (including `exit_group`, `futex`, `set_tid_address`, `gettid`, `tgkill`) to be denied with EPERM. This prevents the coordinator from exiting after a successful release, producing SIGSEGV instead of exit 0.

## Scope of this review

This review covers evidence preservation only. No remediation was performed. No production code was modified. No WP05 acceptance claim is made.

## Independent qualification probe

File: `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs`

Three Facts:

1. `HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary` — FAILED (coordinator exit 139 instead of 0)
2. `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport` — PASSED
3. `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent` — FAILED (coordinator exit 139 instead of 130)

The probe composes production components through internal APIs (not the production owner program, which has no release wiring). The normal production-owner end-to-end path remains unqualified.

## Confirmed counterexample

- Production coordinator reads the 128-byte RELEASE frame, validates it, closes fd 4, then calls `exit_group(0)` which returns EPERM. The process then SIGSEGVs (si_code=SI_KERNEL, si_addr=NULL).
- Managed reaper reports 139 = 128 + 11 (SIGSEGV), a signal-derived value, not a normal exit.
- Bubblewrap propagates the SIGSEGV as its own exit status.
- Strace evidence preserved in the evidence document.

## Seccomp instruction details

- Classic BPF instruction index 79: `JMP JEQ k=56 (clone) jt=0 jf=1`
- Current false-jump: jf=1 (lands on instruction 81, the JSET)
- Corrected false-jump: jf=3 (lands on instruction 83, ALLOW)
- Instruction 81: `JSET k=0x7E020080` (namespace mask)
- For clone: instruction 080 loads clone flags, JSET checks namespace flags.
- For non-clone: instruction 080 is skipped, JSET tests syscall number against mask.
- Affected: any non-clone syscall with bit 7 set (0x80).

## Differential control

- Original filter (jf=1): trivial executable exits 139 (SIGSEGV).
- Experimental filter (jf=3, 1 byte changed): trivial executable exits 0.
- This is diagnostic evidence, not an accepted production fix.

## What was NOT qualified

- Full production-owner end-to-end release path (owner has no release wiring).
- Fresh-process crash matrix.
- Release/stop races and no-resend.
- Native WP04 HELD/abort regression with the new coordinator.
- Real target fork/exec (WP10 scope, not WP05).

## Environment

- WSL2 entered a broken state (CreateInstance/E_FAIL) after investigation.
- No WSL rebuild was performed.

## Production diff

- Empty. No production code, filter, generator, or pinned hash was modified.

## Required remediation (for implementer)

1. Fix `seccomp_filter_policy.py`: change jf=1 to jf=3 at clone JEQ.
2. Regenerate pinned filter (both .bin and .hex).
3. Rebind binary size/hash.
4. Verify permitted syscalls allowed; prohibited operations denied.
5. Independently qualify revised policy.
6. Re-run WP05 release qualification with corrected filter.

## Final disposition

WP05 SECCOMP COUNTEREXAMPLE PRESERVED - BOUNDED REMEDIATION REQUIRED
PY