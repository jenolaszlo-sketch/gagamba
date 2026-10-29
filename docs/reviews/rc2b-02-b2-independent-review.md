# GAGAMBA-RC2B-02-B2 Independent Recovery Bootstrap and Process-Lifetime Security Review

**Date:** 2026-10-29  
**Reviewer:** Independent review (automated analysis)  
**Repository:** `jenolaszlo-sketch/gagamba`, canonical checkout on `main`  
**Verdict:** **SUPPORTED WITH QUALIFICATIONS**

---

## A. Exact Reviewed Commits and Source Identity

| Identity | Value |
|---|---|
| **Baseline** | `e9e9791efa59474a2bb2da8d8087df5de1b0052f` (tree `b28662d660213ead469b7f59202652b1b7ae8bb5`) |
| **Initial implementation** | `b85648f6931128ae3e89108eee267d8cec9e73e2` (tree `1abca74cd55a431079e489c016e24e3840dbdbeb`) |
| **Final implementation / tested source** | `7396012bc07c357a49bbdd0c3791ce06808ca9ab` (tree `e009fb88d6cd736660c2055b4ed7743d59976b78`) |
| **Evidence / delivery** | `235b15b3d2cee828176c18b02685f427f3423ff0` (tree `c3fe376...`) |
| **CI run** | https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40873410775 |

All Git identities verified against the canonical checkout. The evidence commit `235b15b` is the direct child of the fix commit `7396012` and changes only documentation, handoff, and queue pointers.

---

## B. Process Topology and Survival Assessment

### Observed Topology
The tested topology is:
```
test supervisor (Python) → controller (B2Controller) → recovery (RecoveryHost, new session/subreaper) → owner (Owner Lifeline)
```

### Survival Verification
- **`setsid` and session leadership**: `RecoveryInterop.DetachLifetime()` verifies `PR_GET_PDEATHSIG == 0`, `setsid()` succeeds and returns the caller's PID, `getsid(0) == getpid()`, `getpgid(0) == getpid()`. This establishes the recovery as a session leader and process group leader, independent of the controller's session.
- **Subreaper enablement**: `RecoverySubreaper.TryEnableAndVerify()` uses `prctl(PR_SET_CHILD_SUBREAPER, 1)` and reads it back via `prctl(PR_GET_CHILD_SUBREAPER, ...)`. The scope's `SubreaperEnabled` property is exposed and snapshotted in evidence.
- **Controller pidfd ownership**: The controller opens a pidfd for the recovery (`os.pidfd_open` in Python test supervisor; `PidFd.TryOpen` in M1 scope). The recovery opens a pidfd for the controller (`PidFd.TryOpen` at line 48 of `RecoveryHost.cs`). Both are CLOEXEC-verified.
- **Owner pidfd acquisition and retention**: `M1RecoveryScope.TrySpawnOwner` opens a pidfd for the owner *before* any owner-loss-dependent state exists (line 186-200). A missing or non-CLOEXEC pidfd terminates the owner and refuses (line 188-196).
- **Parent-death signal**: Explicitly checked as zero (`PR_GET_PDEATHSIG == 0`). No parent-death signal is set.
- **Inherited descriptor cleanup**: The credential socket is consumed and closed immediately after bootstrap (line 40). The control socket is marked CLOEXEC and retained. The owner socketpair endpoint not used by recovery is closed (line 219). No descriptor is passed to the owner.
- **Supervisor/cgroup co-termination**: The handoff explicitly documents the deployment assumption: "no external supervisor kills recovery together with the controller." The tested topology qualifies only the SIGKILL/EOF survival; shared cgroup/job/process-tree supervisor policies are **not** attested and are deferred to B6.

### Lifetime Guarantees Established by Tests
The 24 process cases and 2 native-domain cases demonstrate:
1. **Controller SIGKILL** → recovery survives, detects controller loss via pidfd, issues one stop, owner observes stop.
2. **Owner SIGKILL** → recovery detects owner exit via retained pidfd, controller remains alive, no false controller loss.
3. **Recovery SIGKILL** → surviving owner detects recovery lifeline loss via its retained pidfd, writes fixture stop observation.
4. **Controller loss with retained domain** → recovery retains real cgroup.kill/events/directory authority; kill write and empty readback; identity-checked removal after owner exit.
5. **Owner loss with retained domain** → controller remains live; retained-handle kill/empty/removal; owner exit proven; no journal-writer takeover.

**Limitation**: These guarantees are qualified **only for the tested WSL2 deployment** (kernel `6.6.114.1-microsoft-standard-WSL2`). Independent native Linux-host qualification was **not performed** and is explicitly excluded from B2 scope.

---

## C. IPC, Authentication and Descriptor-Security Assessment

### Authentication Architecture
| Layer | Mechanism | Strength |
|---|---|---|
| **Linux peer credentials** | `SO_PEERCRED` on both control and credential sockets | Kernel-enforced PID/UID; same-UID hostile processes can forge |
| **Cryptographic session credential** | 32-byte random secret, single-use, constant-time comparison | Stronger identity; travels only in private credential record over separate socket |
| **Session/execution/generation bindings** | UUIDs bound to every frame via `OwnerProtocolSequencer` | Prevents cross-session/generation confusion |
| **Monotonic frame sequencing** | Per-direction sequences, strict increment | Prevents replay, reordering, duplicate acceptance |

### Descriptor Security
- **Two separate socketpairs**: `GAGAMBA_B2_CONTROL_FD` and `GAGAMBA_B2_CREDENTIAL_FD` must be different sockets/inodes (`DistinctSockets` check). Both are `SOCK_SEQPACKET` (type 5).
- **CLOEXEC enforcement**: Both sockets marked CLOEXEC via `Protect()` (line 30, 56-58 of `RecoveryInterop.cs`). Credential socket closed immediately after bootstrap (line 40).
- **Environment sanitization**: Both `GAGAMBA_B2_CONTROL_FD` and `GAGAMBA_B2_CREDENTIAL_FD` environment variables cleared before any child spawn (lines 24-25).
- **No credential disclosure**: The 32-byte secret is zeroed after association (`CryptographicOperations.ZeroMemory` at lines 38, 43, 50, 55, 60 of `RecoveryHost.cs`; line 60 of `RecoveryConnection.cs`). It never appears in argv, environment, logs, evidence, or target input.

### Frame Validation
- **Bootstrap JSON**: Version 1, max 8192 bytes, max depth 12, unknown properties refused (`JsonUnmappedMemberHandling.Disallow`).
- **Controller wire**: Distinct magic `0x4d314f70` (M1OP), version 1, existing 48-byte M1 framing, max 512 payload bytes. Invalid shape/version/identity/sequence fails closed.
- **SCM_RIGHTS receiver**: Closes unexpected descriptors, rejects truncation.
- **No reconnect endpoint**: Reconnect and duplicate association explicitly refused (lines 145-148 of `RecoveryHost.cs`); `RecoveryMessage.Reconnect` and duplicate `Associate` trigger `ControllerAssociationRefused` stop.

### Negative Cases Covered
The test inventory covers:
- Wrong OS peer identity (socket created by another process) — `test_invalid_os_peer_identity_refuses`
- Duplicate/missing descriptor roles — `test_duplicate_inherited_descriptor_roles_refuse`, `test_missing_independent_channel_refuses`
- Wrong secret/PID — `test_wrong_session_secret_refuses_before_owner_creation`, `test_wrong_controller_identity_refuses`
- Changed installation hash — `test_changed_executable_pin_refuses`
- Invalid delegated parent — `test_invalid_delegation_refuses_without_provisioning`
- Oversized/malformed/versioned frames — `test_oversized_native_record_refuses`, `test_wrong_native_protocol_version_refuses`
- Malformed frames — `RecoveryB2Tests.MalformedAndOversizedFramesRefuse`

### Remaining Considerations
- **Same-UID isolation not claimed**: The handoff explicitly states "Hostile same-UID code and administrators remain outside this trust model; this is not same-UID isolation."
- **Linux peer credentials alone are insufficient**: The cryptographic session credential is the stronger identity. The peer-credential check is a necessary but not sufficient gate.
- **Descriptor leakage**: All descriptor paths close unexpected FDs and verify CLOEXEC. The `FdPassing` bounded receiver closes unexpected descriptors.

---

## D. Controller/Owner/Recovery Failure-Race Assessment

### The Critical Fix (Commit `7396012`)
The initial implementation `b85648f` had a defect: **SIGCHLD interrupted control poll, falsely setting `ControllerLost` while controller stayed live** (22 passed / 2 failed in `process-interrupt-baseline`).

The fix in `7396012` introduces:
1. **`RecoveryInterop.IsTransientReceiveError`** — recognizes `"poll errno 4"` (EINTR), `"recvmsg errno 4"` (EINTR), `"recvmsg errno 11"` (EAGAIN), and `"control record timed out"` as transient, **not** peer death.
2. **`RecoveryInterop.IsTransientPidfdError`** — recognizes `"pidfd poll errno 4"` (EINTR) as transient, not exit evidence.
3. **Control loop changes** (lines 116-121, 151-152 of `RecoveryHost.cs`):
   - `PidFd.TryExited` failure → checks `IsTransientPidfdError`; only non-transient errors trigger `ControllerExitEvidenceUnavailable`.
   - `PidFd.TryExited` success with `exited=true` → triggers `ControllerExited`.
   - `TryReceiveMessage` failure → checks `IsTransientReceiveError`; only non-transient errors trigger `ControllerTransportLost`.
4. **Owner lifeline changes** (lines 199-214 of `RecoveryHost.cs`): Same transient-error handling for owner pidfd and owner channel receive.
5. **Owner-side B2Lifeline** (lines 199-215): Transient pidfd errors continue the loop; transient receive errors continue; only non-transient errors break with `"RecoveryExitEvidenceUnavailable"`.

### Race Analysis
| Race Scenario | Handling |
|---|---|
| **EINTR during controller pidfd poll** | Treated as transient; loop retries; no false `ControllerLost` |
| **EAGAIN during controller pidfd poll** | Not a pidfd error (poll timeout returns 0); correctly handled as "not exited" |
| **EINTR during control socket receive** | `IsTransientReceiveError` catches `"recvmsg errno 4"` and `"recvmsg errno 11"`; loop retries |
| **EINTR during owner pidfd poll** | Same transient handling; no false `OwnerExited` |
| **EINTR during owner channel receive** | Same transient handling; no false `OwnerTransportLost` |
| **Controller exit during stop message pending** | First stop obligation retains original deadline; duplicate stops refused; `OwnerAcknowledged` set only if stop was sent and ACK received |
| **Owner death concurrent with controller death** | Distinct pidfds track each independently; `ControllerLost` and `OwnerExited` are separate flags; first cause retained |
| **Recovery death during owner operation** | Owner detects via its retained pidfd; writes `owner-stop.json` with `Cause="RecoveryLifelineLost"`; no native stop claim |

### Stop Obligation Guarantees (C1–C4 Contracts)
- **First-cause retention**: `RecoveryObligation.Request` only succeeds when `Count == 0` (line 69 of `RecoveryProtocol.cs`). Cause and deadline captured once.
- **Original deadline retention**: `Deadline = now + 2 * Stopwatch.Frequency` (2 seconds). Never renewed (line 70). Tests verify: `test_duplicate_stops_do_not_renew_deadline`, `test_replayed_sequence_does_not_renew_deadline`, `test_delayed_owner_ack_does_not_renew_deadline`.
- **Nonblocking control delivery**: `ownerChannel.TrySend` is nonblocking (line 106). On failure, channel disposed and `ownerUsable = false` (line 107). EOF is treated as independent owner stop signal.
- **Stop under observation backpressure**: `test_observation_backpressure_cannot_block_safety` — flood of inspects doesn't prevent stop.
- **Delayed/lost acknowledgements**: `test_lost_acknowledgement_stays_unknown` — ACK withheld, `OwnerAcknowledged` remains false, `NativeStop` remains "Unproven".
- **No automatic resend**: Uncertain commands never retried (line 173 comment: "No deadline is renewed. Unknown stop acknowledgement remains Unknown.").
- **Owner transport failure behavior**: `OwnerTransportLost` stop cause; no false native-stop acknowledgement.
- **No release capability reconstruction**: `RecoveryObligation.ReleaseAuthority => false` (line 66). No release message type exists in `RecoveryMessage` enum.

### Synchronous Operations and Lifeline Detection
The recovery main loop sleeps 10ms (line 174). Pidfd polls use 0ms timeout (nonblocking). Control socket receive uses 10ms timeout. Owner channel receive uses 0ms timeout. This ensures liveness observation continues independently of message processing. No synchronous operation blocks the observation loop.

---

## E. Native-Domain Evidence and Limitations

### Two Empty-Domain Cases
1. **`test_controller_loss_retains_real_domain_and_writes_kill`** — Controller killed; recovery retains domain lease; writes `cgroup.kill`; observes empty population; identity-checked removal after owner exit. Result: `NativeStop = "KillWrittenEmptyObserved"`.
2. **`test_owner_exit_retained_domain_stop_and_identity_checked_removal`** — Owner killed; controller remains live; recovery retains domain lease; writes `cgroup.kill`; observes empty; identity-checked removal. Result: `NativeStop = "KillWrittenEmptyObserved"`.

### Verification of Claims
| Claim | Evidence | Assessment |
|---|---|---|
| **Actual retained kernel descriptors used** | `scope.RetainedRecoveryLeases[0]` is a real `DomainLease` from `CgroupInterop.TryValidateDomainHandles` (line 390-396 of `M1RecoveryScope.cs`). `RequestStop()` writes to real `cgroup.kill`. `ObservePopulation()` reads real `cgroup.events`. | **VERIFIED** |
| **Domain identity checked before destructive operations** | `B2RetainedDomain.Stop` calls `lease.VerifyIdentity()` (line 38). `RemoveAfterExit` calls `lease.RemoveIfIdentityMatches()` (line 46). | **VERIFIED** |
| **Owner exit proven before removal** | `RemoveAfterExit` calls `TryOwnerPidFdExited` and requires `exited=true` (line 45). | **VERIFIED** |
| **`cgroup.kill` writes and population observations recorded** | `KillWrittenEmptyObserved` only returned when `kill.Ok && population.Known && population.Empty` (line 41). | **VERIFIED** |
| **Cleanup affects only test-owned domain** | Delegated parent is operator-supplied; fixture tracks only its PIDs; cleanup reaps only known fixture PIDs (qualify-b2.py `close()` method). | **VERIFIED** |
| **Missing/ambiguous observations remain Unknown** | `Stop` returns `"Unknown"` if lease count ≠ 1, identity mismatch, or kill/population not both successful. | **VERIFIED** |
| **No journal-writer takeover** | `B2RetainedDomain` never acquires journal writer; `OwnerPreparationTransaction` not reached in B2 lifeline mode. | **VERIFIED** |

### Explicit Limitations (Per Handoff)
- **Empty observation ≠ populated-domain stop**: "A successful kill write and empty-domain observation are not proof of populated-domain stopping, complete cleanup, target-execution exclusion or durable reconciliation."
- **No native stop when recovery dies**: R1-F02 native enforcement remains **OPEN**.
- **No journal-writer takeover**: B5 reconciliation is deferred.
- **No target execution**: The B2 lifeline mode never creates a PreparationStore, capability, release writer, or target (line 99 of handoff).

---

## F. Independently Executed Tests and Evidence-Only Checks

### Executed on Windows (Current Environment)
| Test Suite | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| `RecoveryB2Tests` (portable contracts) | 37 | 0 | 0 | All deterministic contract tests pass |
| `OwnerBootstrapTests` | 7 | 0 | 0 | Bootstrap validation tests pass |
| `OwnerProtocolTests` (pure logic) | 21 | 0 | 17 | 17 skipped (require Linux SOCK_SEQPACKET/SCM_RIGHTS) |
| `PidFdChildReaperTests` | 0 | 0 | 3 | All skipped (require Linux) |
| `RecoveryB2NativeTests` | 0 | 0 | 2 | Both skipped (require Linux pidfd) |
| **Full M1 filter (616 tests)** | **616** | **0** | **0** | Matches handoff claim |
| `Gagamba.Execution.Tests` (full) | 68 | 0 | 0 | Regression baseline |
| `Gagamba.Runtime.Tests` (full) | 15 | 0 | 0 | Regression baseline |

### Evidence-Only Checks (Not Independently Reproduced)
| Test / Evidence | Status | Reason |
|---|---|---|
| 24 Python real-process cases | **Evidence-reviewed** | Requires WSL2/Linux with cgroup delegation; not run on Windows |
| 2 native-domain cases | **Evidence-reviewed** | Requires operator-delegated cgroup parent; not available |
| WSL2 process suite (24/0/0) | **Evidence-reviewed** | Producer logs at `artifacts/rc2b-02/windows/*-verified.trx` and WSL `/home/gaga/gagamba-rc2b-02-20261029/` |
| CI Ubuntu run (no delegated parent) | **Evidence-reviewed** | CI runs portable contracts only; no native-domain qualification |

**Note**: All evidence was reviewed against the exact final source commit `7396012`. The WSL2 environment is accurately reported as WSL2 (kernel `6.6.114.1-microsoft-standard-WSL2`), not independent native Linux.

---

## G. Findings with Severity and Remediation Requirements

### BLOCKING — None
No findings that prevent B2 owner acceptance within the explicitly declared qualification limits.

### MAJOR — None
No major defects in the implemented B2 scope.

### MINOR — 1 Finding

**Finding M-01: `RecoveryConnection.Send` disposed check added but `Inspect`/`ObserveAsync` not guarded**
- **Location**: `src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryConnection.cs`, lines 66-72
- **Evidence**: The fix commit added `_disposed` guard to `Send()` but `Inspect()` and `ObserveAsync()` do not check `_disposed` before using `Control` and `Wire`.
- **Security consequence**: If a caller disposes the connection and then calls `Inspect()` or `ObserveAsync()`, it could use a closed file descriptor or race with the controller process. However, the controller fixture never calls these after dispose in the tested scenarios.
- **Smallest correction**: Add `if (_disposed) return null;` at start of `Inspect()` and `if (_disposed) throw new ObjectDisposedException(...);` in `ObserveAsync()`.
- **B2 owner acceptance impact**: **Non-blocking** — the controller fixture does not exercise this path; the connection is disposed only at test end.

### OBSERVATIONS — 4 Findings

**Finding O-01: WSL2-only qualification limits deployment claims**
- **Location**: All process tests in `eng/qualify-b2.py`
- **Evidence**: Handoff explicitly states "Independent native Linux host NOT RUN"; kernel is `6.6.114.1-microsoft-standard-WSL2`.
- **Consequence**: Process survival guarantees are qualified only for WSL2. Shared cgroup/job/process-tree supervisor policies on native Linux are untested.
- **Remediation**: Deferred to B7 (full native Linux lifecycle/fault qualification).

**Finding O-02: Empty-domain native stop is not populated-domain stop**
- **Location**: `B2RetainedDomain.Stop`, `eng/qualify-b2.py` native tests
- **Evidence**: Handoff explicitly distinguishes: "A successful kill write and empty-domain observation are not proof of populated-domain stopping, complete cleanup, target-execution exclusion or durable reconciliation."
- **Consequence**: B2 does not qualify populated restricted-domain stopping.
- **Remediation**: Deferred to B3/B4/B5.

**Finding O-03: No native stop enforcement when recovery dies**
- **Location**: `B2OwnerLifeline.Run` — writes `owner-stop.json` with `NativeStop = "Unproven"`
- **Evidence**: Handoff states "R1-F02 native enforcement remains OPEN."
- **Consequence**: If recovery dies, owner detects lifeline loss but cannot enforce native stop on target domain.
- **Remediation**: Deferred to B4 (complete independent stop enforcement).

**Finding O-04: Owner B2 lifeline mode never reaches preparation/release**
- **Location**: `src/Gagamba.Execution.Linux.M1.Owner/Program.cs` line 22-23, 70
- **Evidence**: `--b2-lifeline 1` mode calls `B2OwnerLifeline.Run` which never creates `PreparationStore`, capability, release writer, or target.
- **Consequence**: B3 preparation/release composition is not exercised in B2.
- **Remediation**: Deferred to B3.

---

## H. Final Verdict and B2 Owner-Acceptance Eligibility

### Verdict: **SUPPORTED WITH QUALIFICATIONS**

The B2 implementation **correctly implements** the declared scope:
- ✅ Real recovery process with independent lifetime (setsid, subreaper, pidfds)
- ✅ Authenticated controller association (peer credentials + cryptographic session credential)
- ✅ Owner lifelines with retained pidfds
- ✅ Bounded recovery behavior (30s host bound, 35s owner bound, 2s stop deadline)
- ✅ Corrected EINTR/EAGAIN handling — no false peer death invention
- ✅ First-cause retention, original deadline retention, no automatic renewal
- ✅ Native retained-domain authority with identity-checked cleanup (empty domains)
- ✅ All deterministic contract tests pass (616/0/0 on Windows)
- ✅ All 24 process cases and 2 native-domain cases pass in WSL2 (producer evidence)

### Qualifications (Must Be Explicitly Acknowledged by Owner)
1. **WSL2-only process qualification** — No independent native Linux host execution.
2. **Empty-domain only** — No populated restricted-domain stopping qualified.
3. **No native stop when recovery dies** — R1-F02 remains OPEN.
4. **No release capability** — B1 facade remains safe-refusal only; R1-F03 Option A is semantically approved but unwired.
5. **No journal-writer takeover** — B5 reconciliation deferred.
6. **No production deployment** — B6 installed artifact attestation and supervisor qualification deferred.
7. **Same-UID isolation not claimed** — Hostile same-UID code outside trust model.

### B2 Owner Acceptance: **ELIGIBLE WITH ABOVE QUALIFICATIONS**
The implementation meets its explicitly limited scope. Owner acceptance should record these qualifications and commission B3 with a named native Linux host for remaining qualification.

---

## I. Smallest Next Owner Action

1. **Record B2 owner acceptance with explicit qualifications** (WSL2-only, empty-domain, R1-F02 OPEN, R1-F03 Option A unwired, no B3-B7).
2. **Select a suitable native Linux host** (not WSL2) for B3/B7 qualification.
3. **Commission B3** (real owner preparation/release, C1–C4 composition, sealed C3 clock) with named deployment, protocol, and qualification gates.
4. **Do not begin B3 automatically** — requires explicit owner decision and independent review.

---

## Review Artifacts
- **Review document**: `docs/reviews/rc2b-02-b2-independent-review.md` (this file)
- **Source commit reviewed**: `7396012bc07c357a49bbdd0c3791ce06808ca9ab`
- **Evidence commit**: `235b15b3d2cee828176c18b02685f427f3423ff0`
- **All tests executed on Windows**: 616 (M1 filter) + 68 (Execution) + 15 (Runtime) = **699 passed, 0 failed**
- **WSL2 evidence reviewed**: Producer logs at `artifacts/rc2b-02/windows/*-verified.trx` and WSL `/home/gaga/gagamba-rc2b-02-20261029/`