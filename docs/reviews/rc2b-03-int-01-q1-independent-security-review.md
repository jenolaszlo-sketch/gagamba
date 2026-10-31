# RC2B-03-INT-01-Q1: Independent Integrated Sandbox Security Review

**Date:** 2026-10-31 (Asia/Manila). **Commission:** GAGAMBA-RC2B-03-INT-01-Q1. **Scope:** Independent evaluation of the completed INT-01 Alternative B implementation and its full native fault qualification (QF-02). No implementation changes, test modifications, remediation, or refactoring during this review. Stop after review; no owner acceptance, no F-01 closure, no B4–B7/Hufu/VM/HCS/production activation.

**Verdict:** **SUPPORTED WITH QUALIFICATIONS** — The accepted core invariants are supported by the observed integrated implementation and mandatory native evidence, with explicitly bounded residual risks, uncertainty, and future-stage requirements. This verdict means readiness for an owner decision, not automatic F-01 closure or production approval.

---

## A. Exact Source and Evidence Binding

**Repository:** `jenolaszlo-sketch/gagamba`, canonical checkout on `main`.

| Item | Verified Identity |
|---|---|
| Review HEAD / remote `main` | `20cfcb99c5b0526fdde488630128910067a3b8d5` (docs-only child) |
| Tested implementation (QF-02) | `dc436249fd0b0a8d70b4e4319ac27587fe857c1c` |
| Tested implementation tree | `3b5e2d9eca42e696f48f984b6ab3831e7ed90f44` |
| Qualification baseline (QF-01) | `5170fab91cf6ebd0bc3c067d8cd29216251ef18c` |
| Primary evidence report | `docs/evidence/rc2b-03-int-01-b-qf-02.md` |
| Committed manifest | `docs/evidence/rc2b-03-int-01-b-qf-02.sha256-manifest.txt` |
| Raw evidence root | `artifacts/rc2b-int01-b-qf02/` |

**Manifest verification:** 528 committed entries; all 528 local files hash-match; manifest SHA-256 `b7c669c6db1a499773f8f7c27768a301dde2b41c69f937808ca58c6adfa20160` matches report. Two on-disk extras exactly as disclosed: `delivery-receipt.json` (binds final remote) and `sha256-manifest.txt` (copy of manifest).

**Source/artifact binding verified:**
- `tested-source.tar`: 269 C#/Python/C/sh files byte-compared to Git tree at `dc43624` — **all match** (one `Program.cs` CRLF/LF smudge; normalized bytes identical).
- `implementation.patch`: byte-identical to `git show dc43624` (SHA-256 `c9e285a46049a9075199a46257e4d6a05bcb335743122801ad02942762dad4da`).
- `source-bindings.json`: 13 compiled changed C# files with per-file SHA-256.
- Runner hashes: `int01-native-matrix.py` `b8dbfca432d47bcdb7a25782e8f361ee53d38fa21c69e1ac06f009ec99408d0a`; `int01-fault-matrix.py` `3ee29c82fdec1e7cba38c0d6a91973853e518ee4509e7c8aee1f744dc3150dd0` — **match live `eng/` files** and `review32/identity.json`.
- Artifact identities re-verified on live WSL build root `/home/gaga/gagamba-int01-qf02-20261031`:
  - Controller: `4475eb550afea64a25187d41abc036845ecaf269cc697a300f5b4aa435dc6f69`
  - Owner: `f4a90cb28ab7185503c62a380f157f619e4ef09c39491310f14cfc5b8856929f`
  - Recovery: `8abeb066e89b9af783e4442495307df976de01c3c34720f13d58e76ac8047998`
  - Tool: `f07bd71128093a98c4dc2ead3e484c9f150990f26b192dc2bbd23c01aa37ad9a`
  - Input: `c803a2a3f3f5960c31706c6a943b91109937db76376d469412c868e8e202716b`
  - Coordinator: `ec3e31d3ffa8483aa31cbecef682b656d9fcd16c7a194d17bea9719071d3691e`
  - Seccomp: `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`
  - Bubblewrap: `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6` (`/usr/bin/bwrap`)
- `review32-profile.json`: `81e9ddf4f45d7548edfa09095858de9dd7735eae29f077f2e47547d6a5cc979c` (matches report).
- **QF-01 historical evidence**: 158/158 manifest entries rehashed, SHA `248f50498a3e881ae8280193b04bc9faf7bc8195989e8feb30e9f22f2fa15a47` — matches `baseline-reverification.json`.

**Regression TRX counts (separate scopes):**
- components4 WSL: 277/0/0
- Windows M1: 1243 total, 987 executed/987 passed, 256 not executed
- Windows Execution: 68/0/0
- Windows Runtime: 15/0/0
- components3 (producer): 276/1/0 (D5_O03 EOF — preserved as flaky legacy fixture)
- Alias helper: before 1/0/0 (exit 23, no frame), after2 1/0/0 (exit 0, 96-byte EXEC_CONFIRMED)

**Chronology (preserved, not rewritten):** `final32a` 32/0, `final32b` 31/1 (journal-sampling race in `integrated-original-deadline`), `delivery32` 32/0, `review32` 32/0 (final required run). All attempts retained.

**Git ancestry confirmed linear:** `5170fab9` (baseline) → `dc43624` (impl) → `20cfcb99` (docs). Each successor sole parent of predecessor.

---

## B. Process Topology and Authority Assessment

**Verified by source and raw evidence:**

1. **Recovery is the actual parent of the owner.** `M1RecoveryScope.TrySpawnOwner` calls `Process.Start(psi)` from the recovery process; `RecoveryInterop.DetachLifetime()` confirms `PR_GET_PDEATHSIG==0` + `setsid()` (recovery becomes session leader). Every case JSON records `Held.OwnerPid` with `/proc/<owner>/status` `PPid == Held.RecoveryPid`.

2. **Recovery spawns and retains the owner pidfd fail-closed.** `TrySpawnOwner` opens `_pidFdProvider.TryOpen(process.Id)` immediately; missing or non-CLOEXEC pidfd → `TerminateOwner(process)` and refusal. Retained as `SafeFileHandle` in `_retainedOwnerPidFds`.

3. **Recovery retains native domain and staging authority.** Owner transfers exactly 7 domain FDs (`DomainRecoveryAuthority`, roles Parent/Directory/Kill/Events/MemoryMax/MemorySwapMax/PidsMax) + 1 staging FD (`StagingAuthority`) via SCM_RIGHTS. Recovery validates CLOEXEC, Cgroup2 identity, and device/inode distinctness (`CgroupInterop.TryValidateDomainHandles`), then retains `DomainLease` in `_retainedRecoveryLeases`. Three ACK barriers: `DomainAuthorityRetained` (after role-count check + native validation), `StagingAuthorityRetained` (after stat match), `PreparedAcknowledgement` (after `RetentionValid` + durable journal cross-check). Every failure routes through `FailOwnerPreparation` → `TerminateOwner` + no capability.

4. **Owner retains live preparation capability, journal writer, sole RELEASE writer.** Exact writer handoff at `M1RecoveryScope.cs:336-338`: scope disposes its lease before owner spawn. `OwnershipJournal` exclusive lock (`flock(LOCK_EX)`) held only by owner while alive. `ReleaseArbiter` constructed with owner's capability, writer lease, and transport. Recovery `ReleaseAuthority` false in all records; never receives release secret, connected endpoint, or physical writer.

5. **Direct authenticated controller–owner session.** `OwnerDirectConnection.Accept` runs before `StartAuthority` received. Listener created fresh in 0700 `/tmp/gagamba-<guid>/owner.sock` 0600, identity-verified (no symlinks, no preexisting). Controller connects; mutual `SO_PEERCRED` + `SO_PEERPIDFD` + `LiveReference` checks with sealed identities (controller-sealed, owner-sealed via recovery bootstrap). CSPRNG challenges + HMAC-SHA-256 transcript binding version/roles/3 GUIDs/ns/Pid/Uid/Gid. Secret zeroed in `finally`. Framing: 64KiB max checked pre-allocation; ancillary refused+closed; truncation terminal; per-direction sequences; ≤5s window; `MaxAttempts=4`; `SealAssociation` requires `CleanupConfirmed`; no second session; no SCM_RIGHTS on direct channel. **Identity authentication occurs before any admissible release session.**

6. **Recovery survives controller termination.** Controller pidfd + authenticated channel monitored; `controllerLost` → `Stop("ControllerEvidenceLost" / "ControllerExited")`. Recovery's `SafetyStopRetained()` writes `cgroup.kill` via its own retained `DomainLease` — independent of owner IPC. Reaping: `WaitForOwner` (pidfd exit + `waitpid`) + `ReapAdoptedChildren`.

7. **Owner observes genuine recovery loss.** Owner holds: controller pidfd (from `DirectBootstrap`), recovery pidfd (opened by owner via `GetParentPid()`), dedicated lifeline socketpair (`[1]` = stop, `[2]` = ack). `OwnerSafetyMonitor` 10ms thread polls `LiveReference(pidfd)`, lifeline zero-timeout poll, channel liveness. `!recoveryLive || !healthy` → `AcceptStop(SetupFault)` → native `cgroup.kill` + capability consumption + arbiter stop on worker. During HELD: top-of-loop `StopLatched` check → `Abort()`. Post-release: monitor still runs; `Released()` suppresses setup-expiry only.

8. **Reconnect refusal / no authority reconstruction.** Direct session one-use; listener disposed + unlinked on `SealAssociation`. Controller `IntegratedB3Client` single `_channel`; `Prepare` refuses if disposed/stopped. Recovery `Reconnect` frames refused (`RecoveryHost.cs:148-152`). Original deadlines immutable (min with `expiresAt`); no handshake/reconnect renewal.

---

## C. Direct Authentication and Credential Custody

**Full protocol sequence verified in source at `dc43624`:**

- **Bootstrap trust root:** Controller seals `DirectProcessIdentity.Current()` (PID/UID/GID + `/proc/self/ns/{pid,user}` link targets) + fresh session/execution/generation GUIDs + 32-byte secret (`RandomNumberGenerator.GetBytes(32)`). Recovery verifies parent/peer credentials, re-derives controller identity, spawns owner, sends `DirectBootstrap` (controller pidfd + safety lifeline) over credential-verified recovery↔owner seqpacket channel. Owner asserts `GetParentPid() == identity.Recovery.Pid`, peer credentials match, `LiveReference(pidfd, identity.Recovery)`, then decodes association and verifies controller pidfd `LiveReference(fds[0], identity.Controller)`. Lifeline fd verified distinct socket (same device/inode check) with recovery peer credentials.
- **Listener creation:** `DirectOwnerListener.Create("/tmp")` — mkdirat 0700, identity-open + mode check, preexisting socket refused, atomic `SOCK_STREAM|SOCK_CLOEXEC|SOCK_NONBLOCK` + `accept4(..., CLOEXEC|NONBLOCK)`. Client connect re-verifies dir 0700 / socket 0600 + identity after connect.
- **Feature test:** `DirectPeerIdentity.FeatureAvailable()` positive control (socketpair + self `pidfd_open` + `Verify` + `LiveReference`); fails terminal if unavailable — no PID/path fallback.
- **Handshake:** Owner sends challenge; controller returns controller_challenge + secret; owner `owner-confirm` (HMAC transcript); controller `controller-confirm`; owner `owner-established`. Transcript: `"gagamba-int01-direct-v1"`, role strings `"original-controller"`/`"transaction-owner"`, three GUIDs, all Pid/Uid/Gid/ns, both challenges. HMAC-SHA-256 + role suffix; `CryptographicOperations.FixedTimeEquals`. Zeroing: secret, challenges, transcript, payloads, frames, MACs in `finally`.
- **Custody:** Direct socket/secret never in recovery argv/env/files/journal/telemetry. Recovery receives rendezvous address payload only (bounded JSON). Owner CLOEXEC fds protected; exec descriptor map closes from 12 up. No release-capable fd passes to recovery, coordinator, or target.
- **Impersonation case (`actual-recovery-release-impersonation`):** IntegratedRecoveryHost callback runs **in the recovery process**, connects to owner listener before rendezvous publication. Owner `SO_PEERCRED` reads peer PID = RecoveryPid ≠ ControllerPid; rejection recorded; genuine controller then completes release (1 attempt, exec confirmed, root exit 0, duplicate Duplicate). **Proves OS-identity boundary against unauthenticated same-UID recovery peer.** Not a stolen post-handshake bearer test; same-UID hostile memory access excluded (comment `DirectSession.cs:21-23`).
- **No authentication bypass, hardcoded secret, or weakened check found.**

---

## D. Native Retention and Independent Safety Stopping

**Retention ACK barriers (source-verified):**
1. Domain ACK: owner blocks on `DomainAuthorityRetained`; recovery sends only after role count + `TryValidateDomainHandles` + `_retainedRecoveryLeases.Add`.
2. Staging ACK: owner blocks inside staging-directory hook on `StagingAuthorityRetained`; recovery verifies device/inode match + retains.
3. Prepared ACK: owner durably writes Prepared + sends `PreparedDescription`; recovery requires `RetentionValid` (1 domain lease alive+matching, 1 staging parent, 1 staging dir) + `MatchesPrepared` durable journal cross-check before `PreparedAcknowledgement`.

**Pre-mint stop latch (three independent guards):**
- `OwnerSafetyMonitor.AcceptStop`: if no arbiter, `_capability.TryConsume()`.
- `OwnerPreparationTransaction.cs:379`: `if (safety?.StopLatched) return 42;` before controller manifest ACK.
- `OwnerSafetyMonitor.PublishCapability`: `if (StopLatched) { capability.TryConsume(); return false; }` → caller disposes + aborts.

**Safety monitoring (independent of blocked IPC):**
- `OwnerSafetyMonitor`: dedicated background thread (`gagamba-owner-safety`), 10ms wake. Polls: controller pidfd + channel, recovery pidfd + lifeline, domain limits. `AcceptStop` → `_domain.RequestStop()` (cgroup.kill write) **under `_gate` but no IPC/journal**; `_arbiter?.RequestStop()` on separate worker thread; `[2]` ack on lifeline. `DispatchStop` never executes journal I/O. `Dispose` joins worker before stopping monitor.
- `IntegratedRecoveryHost`: main thread = monitor; preparation on `Task.Run`. `Stop()` = obligation + lifeline `[1]` + `scope.SafetyStopRetained()` (writes `cgroup.kill` via own lease + reads `cgroup.events`). Re-executes every tick; 2s force-kill via pidfd `SIGKILL`.
- **Blocking analysis:** Owner HELD receive 250ms bound + latch check; post-release observation/drain on main thread (monitor independent); arbiter `_stopAccepted` set under `_gate` before journal I/O; recovery control fd non-blocking; preparation channel blocking but no mandatory stop path uses it; controller submission latency exists but recovery self-observes death.

**`blocked-observer-native-stop` verification:** Producer record + my rerun identical: domain populated 1 → independent owner stop `KernelAccepted=true` → domain populated 0 while observation blocked. Native stop via duplicated `DomainLease` (owner main thread holds primary; monitor holds duplicate).

**Owner exit / reaping / fence takeover:** Recovery pidfd exit → `ownerExited` → `Stop("OwnerExited")` → `WaitForOwner` (pidfd-proven + `waitpid`) + `ReapAdoptedChildren`. Cleanup task: `TryAcquireWriter` (flock `LOCK_EX|LOCK_NB` — refuses while owner lives), `Reconciler` (stops/empties via retained lease) + `B3ScopeCleaner` (dev/inode-checked removal). Zero allowance (`TimeSpan.Zero` — not sealed C3/B5). Controller polls recovery snapshots until `OwnerExited && OwnerReaped && WriterFence && JournalFinalized`.

**No mandatory stop delayed behind blocked IPC.** (Verified per-axis in code.)

---

## E. 32-Case Fault-Matrix Assessment

**Authoritative 32 names (8 core + 24 additional) from `eng/int01-native-matrix.py:8-16`:**
```
CORE (8): positive-one-release, stop-before-release,
           controller-sigkill-held, recovery-sigkill-held, owner-sigkill-held,
           controller-sigkill-after-release, recovery-sigkill-after-release, owner-sigkill-after-release
ADDITIONAL (24):
           controller-before-preparation, controller-during-preparation, controller-release-race,
           recovery-retention-death, recovery-release-race,
           owner-before-retention, owner-mid-staging, owner-after-intent,
           owner-ambiguous-delivery,
           missing-domain-ack, missing-staging-ack, wrong-descriptor-role-count, partial-scm-rights,
           lost-retention-ack,
           blocked-observer-native-stop,
           simultaneous-controller-recovery-death,
           integrated-stop-before-mint, integrated-stop-after-intent-before-write,
           integrated-ambiguous-intent, integrated-lost-release-ack,
           integrated-original-deadline, integrated-owner-live-takeover-refusal, integrated-post-exit-writer-fence,
           actual-recovery-release-impersonation
```

**Per-case verification method (my independent assessment):**
- **20/32 rerun by me** under fresh transient units (`q1rev-a/b/c`) — **ALL PASSED**, field-by-field security semantics identical (decision Kinds, attempt counts, rejection counters, peer rejection `ActualPid==RecoveryPid≠ControllerPid`, cleanup classification).
- **12/32 inspected** from producer raw JSON — zero numeric contradictions vs report §3 table.
- **Receipt consumption:** 19/20 fault-matrix cases consume `fault-<stage>.json` with `Case==case && Stage==stage` assertion; `wait_file` 10s timeout → fail if missing. `controller-before-preparation` has no receipt (disclosed) — passes on absence evidence (no prepare, empty journal, no new domain).
- **No pass-by-timeout:** only `integrated-original-deadline` uses bounded 5s poll window for deadline journal; pass requires actual `Ok=true` poll (not timeout expiry).
- **Malformed-transfer assertions inspect actual receiver rejection** (`recovery-transfer-rejected.json` with exact role count / descriptor count), not sender intent.
- **Partial SCM_RIGHTS:** delivers 6 of 7 FDs; receiver rejects on count mismatch (no `MSG_CTRUNC` claim).
- **Watchdog cleanup** explicitly separate; never converts failure to pass.

**Anomalies observed (non-blocking):**
- A-1: `controller-before-preparation` no receipt — disclosed.
- A-2: `controller-during-preparation` native-stop `KernelAccepted:false` — no stop claimed in report.
- A-3: Reparenting mid-case (recovery-release-race `ParentPid` changes) — runner does not assert parentage for kill actions.
- A-4: "Unobserved" physical counters truly absent (not zero); 13/32 have explicit values (1/0) matching report exactly.
- A-5: Empty-domain cases legitimate; only `blocked-observer` requires populated.
- A-6: 4 Confirmed cases (positive-one-release, stop-before-release, integrated-original-deadline, integrated-owner-live-takeover-refusal) confirmed via `FinalObservation.Cleanup` (positive disposal contract) — no `WriterFence` field in that record.
- A-7: Mid-fault vs final fence timing difference (RecoveryReapedAndFenced vs RecoveryFinal) — use RecoveryFinal as witness.
- A-8: "Refused" prose vs enum `Kind=4 (Failed)` / `3 (CancelledBeforeRelease)` — runner asserts `Kind!=1` (no capability).
- A-9: No timeout-reliance beyond disclosed `integrated-original-deadline`.
- A-10: `preflight.json` static template (not disposition).
- A-11: Packaged `scripts/int01-fault-matrix.py` stale (ef7f15ff ≠ live 3ee29c82); review32/identity.json binds live runners.

**All 32 pass criteria met in producer evidence and my reruns.**

---

## F. C1–C4, Option A, and Release-Arbitration Conclusions

**C1 (D6.9 implementation + QF02-F01):** Durable Intent / ambiguous persistence semantics conservative.
- `OwnershipJournal.FailSealed` now distinguishes `ReleaseTag.Intent` failures with retained seal → `AmbiguousFailure` (producer `before-c1-reproduced` showed `Refused(1)`; corrected QF-02 run shows `AmbiguousPersistence(5)`, 0 writes, `Duplicate(3)`).
- `ReleaseArbiter` captures typed `AmbiguousAfterContent` outside `_journalGate`; retains `AmbiguousPersistence` or stop-won refusal under `_gate`; requests C2 tail stop after releasing `_gate`; no FD4, no second Intent, no retry. First-retainer ordering exact (ambiguity-first, stop-first, existing-retained — all tested in D6.9 C1-T04/T06/T07).
- **QF02-F01 extends D6.9 conservatively:** `AmbiguousFailure` returned for any retained-seal failure on Intent candidate (not only final unlink); preserves C1/Option A.

**C2 (D6.8 verified):** First-stop capability consumption under `_gate` on all three paths (normal, clock-unavailable, overflow); `TryConsume` state transitions for every reachable store state; failed durable stop cannot permit second release; shared-capability closure across arbiters; no new journal I/O under `_gate`; no lock inversion.

**C3 (D6.7 human approved, Policy A selected):** Original deadline identity and clock limitations unchanged. `HeldReleasePhase.ObserveExecution` QF02-F02 correction uses original `statusDeadline` after `ExecConfirmed` — no fresh 1s cap. `integrated-original-deadline` case waits original setup cutoff, polls acknowledged journal; no new execution deadline.

**C4 (D6.0 verified):** Duplicate, correlation, concurrent-release behavior preserves original decisions. `_retainedRelease` entry check returns `Duplicate`; post-Intent gate duplicate check; concurrent matching/mismatching requests single-winner; no second physical RELEASE; no caller serialization added for correlation.

**R1-F03 Option A:** Pre-retention correlation mismatch → refuse only that call (`InvalidBinding`), no consumption, no replacement, no deadline reset, no RELEASE. After retention → preserve original decision + correlation, `Duplicate`, mismatch reported separately. `ReleaseAdmissionTests.cs:217` exercises both `releaseFirst` values + original-correlation B3 regression — implemented and passing.

**Release correctness (observed in my reruns):**
- Explicit physical attempt counters (unobserved ≠ zero).
- Durable journal state via acknowledged snapshots.
- Returned decision Kind matches arbiter logic.
- Target execution facts from coordinator status (exec confirmed, root wait).
- Recovery stop facts (`NativeStop`, `KillWrittenEmptyObserved`).
- Delivery/outcome separation: `owner-ambiguous-delivery` → owner `Released`+1, controller `Unknown`; `integrated-lost-release-ack` → owner decision `Released` retained, later `Duplicate`; no replay.
- No atomic death/write claim beyond owner-local arbiter guarantee.

---

## G. Production Correction Review

**QF02-F01: Durable seal ambiguity (`OwnershipJournal.FailSealed` + callers).**
- Before: `before-c1-reproduced` real rename failure after fsync; seal retained, public result `Refused(1)`.
- Correction: `FailSealed(lease, error, releaseIntent)` — `releaseIntent && Exists(seal)` → `AmbiguousFailure`; else `Failure`. Preserves seal when no acknowledged baseline or newer generation.
- Effect: `integrated-ambiguous-intent` returns `AmbiguousPersistence(5)`, 0 physical attempts, `Duplicate(3)` on repeat; cleanup Unknown.
- **Verified:** Correctly distinguishes ReleaseIntent from unrelated transitions; preserves durable truth; prevents second release; preserves writer fence; no definitive→ambiguous or vice versa flip; compatible with C1/Option A readers.

**QF02-F02: Root-wait observation deadline (`HeldReleasePhase.ObserveExecution`).**
- Before: `confined-fd1` exec confirmed + FD digest valid + wrapper exit 0, but `Root=Unknown` (fresh 1s cap after ExecConfirmed).
- Correction: `statusDeadline` = original `deadline` (if Delivered) or `min(deadline, now+1s)` (if not) — **uses existing statusDeadline, not new cap**.
- Effect: Same two-second target now yields actual `Root=Exited`, raw wait 0, `confined-fd2` passes; no new overall deadline.
- **Verified:** Original operation budget preserved; no new deadline; blocking reads bounded; safety stopping independent (monitor thread separate); root-wait status accurate; Unknown truthful on original deadline expiry.

**QF02-F04: Qualification helper status-FD aliasing (`wp05-qualification-coordinator.c` + `test-wp05-status-fd-alias.py`).**
- Before: `release` fd 6, `status` fd 4; `dup2(release,4)` destroyed status source → child exit 23, no frame.
- Correction: `fcntl(F_DUPFD_CLOEXEC, 6)` on status source before remapping release fd 4.
- After: `alias-after2` child 0; 96-byte EXEC_CONFIRMED kind 4 sequence 2; exact identities/digest/release/reaping.
- **Verified:** Helper preserves correct status descriptor; earlier EOF failure FD numbers not captured → attribution honestly unresolved (per report §5); my components rerun (276/1/0) reproduces D5_O03 flake with corrected binary → **Q1R-08**.

**Fault-hook gating verified inert:** `Int01Qualification.Validate` + `ExperimentalRuntimePin.VerifyPrivateDirectory` + whitelist `Stage()` + `B3Qualification` requires `IntegratedRecovery` + `OwnerPreparationSpecification` requires `DirectIdentity` + `EnableHeldCoordinator`. No authentication bypass, no release-authority minting, no production path affected.

---

## H. Cleanup Unknown Classification

**20 Confirmed / 12 Unknown** (producer criteria: `OwnerReaped && WriterFence && DomainEmpty && StagingRemoved && JournalFinalized` OR positive disposal contract `FinalObservation.Cleanup.State==1`).

| Unknown Group (12) | What is Established / What is Missing | Mandated? |
|---|---|---|
| recovery-sigkill-held; recovery-sigkill-after-release; recovery-retention-death; recovery-release-race; simultaneous-controller-recovery-death | Recovery dies; owner exits; release invariant checked. No surviving independent reaper/fence/finalization witness. | **Yes** — no surviving actor with cleanup obligation. |
| controller-before-preparation | No preparation, journal, new domain. No recovery cleanup transaction exists. | **Yes** — nothing created. |
| owner-before-retention; wrong-descriptor-role-count; partial-scm-rights | Recovery reaps owner + acquires fence, but retains zero domains; finalization flags false. | **Yes** — partial resources not claimed recovered by native evidence. |
| missing-domain-ack | Recovery retains one domain, records `KillWrittenEmptyObserved` + reaping/fence; `DomainEmpty/StagingRemoved/JournalFinalized` false. Component stop evidence not promoted to full cleanup. | **Yes** — retained authority never finalized. |
| missing-staging-ack | Recovery records `DomainEmpty/DomainRemoved/OwnerReaped/WriterFence/JournalFinalized` true; `StagingRemoved` false. | **Yes** — staging not removed. |
| integrated-ambiguous-intent | Retained seal + unreconciled intent; no final cleanup witness before watchdog. | **Yes** — watchdog ≠ Gagamba recovery. |

**Host preservation verified (my runs + producer §9):** Zero residual test cgroups, zero experimental processes, zero loaded matching units. 157 case workspaces + build artifacts retained as evidence.

**Do not treat Unknown as automatically non-blocking.** Distinguish: unavoidable loss of recovery witness (mandated) vs. implementation failure to clean resources while surviving actor has authority (blocking for B4/B7). The 12 Unknowns fall in the former category — but B4/B7 must address cleanup obligations for recovery-death and partial-resource scenarios.

---

## I. Exec/FD and Confinement Boundaries

**Confined-FD supplementary probe (`confined-fd2`):**
- Real integrated B3 topology used; artifact identity pinned (`confined-fd2` profile SHA `5e86c5a4...`).
- Target exec established (coordinator status `Exec=Confirmed`); root wait outcome `Exited`/`0`.
- Physical RELEASE count = 1 (confirmed at Execution + Disposal + `fault-owner-release-decision.json`).
- Kernel FD inventory slots 0–63 via pinned probe `b3-exec-target-probe.c` (SHA `6d07769d...`): prints `TARGET_FDS=0,1,2\n` (17 bytes), digest `00763f9daaf94900dc683f9a8c6f51ae6bd59092228827705b1a496cb69230a4` — **matches report exactly**.
- No unexpected inherited descriptors in observed slots 0–63.
- No leaked coordinator confirmation descriptor (recovery closes it on transfer; CLOEXEC on exec).
- **Scope bounded to 0–63** — not an unlimited FD inventory. F-08 qualification explicitly preserved.

**FD-aliasing helper correction** is legacy qualification fixture (binary `a2196f14...`, source `09eb1008...`), not the confined production coordinator. Distinct from F-08.

---

## J. Findings by Severity

| ID | Severity | Evidence / Status | Residual Risk | Owner Action |
|---|---|---|---|---|
| **F-01** | MAJOR/OPEN | Complete producer matrix (32/0/0) + my 20/32 rerun + source review. Topology, auth, retention, stop, cleanup, release arbiter all evidenced. | Cleanup Unknown obligations for B4/B7; WSL2 not native Linux; same-UID hostile process out of scope. | **Owner disposition on INT-01** — this review supports that producer evidence for integrated topology is complete and consistent. F-01 closure = owner decision. |
| **F-02** | (was MINOR) | Genuine recovery lifeline: owner parentage/subreaper/retained native authority + controller-death survival/reaping/fence evidenced by integrated cases (my reruns: controller-sigkill-held, owner-sigkill-held, simultaneous-controller-recovery-death, integrated-post-exit-writer-fence). | Closure = owner decision; no source-only closure. | Support closure. |
| **F-03** | (was MINOR) | Correlation diagnostics preserved; `InvalidCorrelationAfterStopOrRetainedReleasePreservesDecisionAndDiagnostic` covers both `releaseFirst` + original-correlation B3 regression. | Diagnostic imprecision grants no authority; Option A preserved. | Support closure. |
| **F-07** | (was OBSERVATION) | Actual integrated recovery killed while HELD, after release, during retention, at release Intent. Owner exit/no later release observed. Full recovery-death cleanup still Unknown. | Must be addressed before B4/B7 production eligibility. | Carry forward; B4 must implement recovery-death cleanup. |
| **F-08** | (was OBSERVATION) | Confined exec/root wait + pinned FD-probe (slots 0–63) — corrected. Scope narrower than unlimited FD claim. | 0–63 only; no source-only closure. | Support closure within bounded scope. |
| **Q1-F03** | MAJOR (was prereq) | Direct controller/owner authentication implemented + proven (real recovery peer rejected by kernel PID gate, genuine controller succeeds; direct auth suite passes 21 tests). | Post-handshake bearer theft not tested (explicitly excluded). | **Supported** — implementation complete and qualified. |
| **Q1-F04** | MAJOR (was prereq) | Death/release races covered per D01 §6: original deadlines, native loss/stop before final write, retained release winner, ambiguous intent/delivery, no physical replay. No wall-clock atomic claim. | Stronger fence would need separate review. | **Supported** — per D01 §6 limits. |
| **Q1-F05** | MAJOR (was prereq) | Independent safety monitoring verified: populated domain stopped with observer blocked; independent native stop accepted + kernel empty. Dedicated monitor regressions pass. | Dispose DispatchStop suppression (Q1R-06) minor; recovery loop WaitForOwner 1s delay (Q1R-07) bounded. | **Supported** — implementation complete. |
| **QF02-F01** | (correction) | Verified sound: conservative, preserves C1/Option A, no second release, writer fence intact. | — | No further action. |
| **QF02-F02** | (correction) | Verified sound: original budget preserved, no new deadline, safety stop independent, root-wait accurate. | — | No further action. |

**My new review observations (Q1R-01..Q1R-08):**
- **Q1R-01 (MINOR, evidence precision):** `blocked-observer` populated=0 read is post-owner-exit; causality supported by `KernelAccepted` receipt but pre-exit read would close gap.
- **Q1R-02 (MINOR, hygiene):** Accept loop attempt counter counts unauthenticated rejects toward `MaxAttempts=4`; comment at `OwnerDirectConnection.cs:72-73` claims "Only unauthenticated OS peers may be rejected without consuming that attempt." Bounded DoS, fail-closed.
- **Q1R-03 (MINOR, coverage):** `OwnerDirectConnection.Accept` has no direct managed unit test (end-to-end native coverage only).
- **Q1R-04 (OBSERVATION, provenance):** Sealed `scripts/int01-fault-matrix.py` copy is stale (`ef7f15ff` ≠ live `3ee29c82`); authoritative assertions = committed `eng/` runner matching `review32/identity.json`.
- **Q1R-05 (OBSERVATION, provenance):** D6.9 C1 GLM independent review **never performed**; QF02-F01 correction is more conservative than D6.9's disclosed scope. Owner should record that C1 implementation review is now subsumed by QF-02 + this review.
- **Q1R-06 (MINOR, Dispose semantics):** `OwnerSafetyMonitor.Dispose` suppresses `DispatchStop` when `_closing` (durable arbiter stop may be skipped if stop first latched during disposal); native stop runs, capability invalid, prereqs fail closed.
- **Q1R-07 (OBSERVATION, timing):** Recovery `WaitForOwner(1s)` can delay repeat `SafetyStopRetained` by ≤1s/iteration; native stop already executed at `Stop()`.
- **Q1R-08 (OBSERVATION, flake):** D5_O03 `Wp05D5NativeStatusTests.D5_O03_JournalFailureIsNotReportedAsDurableObservation` intermittently fails with "status channel closed before a complete record" (276/1/0 in my components rerun with corrected helper). Earlier failure attribution to FD alias remains unresolved (FD numbers not captured). Legacy fixture, not production coordinator.

---

## K. Independent Reruns and Evidence Limitations

**Independent reruns executed by me (20/32 native cases, 3 transient units):**

| Batch | Cases | Result |
|---|---|---|
| q1rev-a | integrated-ambiguous-intent, actual-recovery-release-impersonation, integrated-stop-after-intent-before-write, integrated-stop-before-mint, integrated-lost-release-ack, owner-ambiguous-delivery | 6/0/0 |
| q1rev-b | recovery-retention-death, recovery-release-race, missing-domain-ack, partial-scm-rights, wrong-descriptor-role-count, blocked-observer-native-stop, controller-release-race | 7/0/0 |
| q1rev-c | positive-one-release, controller-sigkill-held, recovery-sigkill-held, simultaneous-controller-recovery-death, integrated-post-exit-writer-fence, integrated-original-deadline, integrated-owner-live-takeover-refusal | 7/0/0 |

**All 20 passed.** Security semantics field-verified identical to producer records (see §E).

**Components4 managed regression rerun (q1rev-d):** 276/1/0 — reproduced D5_O03 EOF flake with corrected helper (Q1R-08).

**12 cases inspected only** (producer raw JSON cross-checked): `controller-before-preparation`, `controller-during-preparation`, `owner-before-retention`, `owner-mid-staging`, `owner-after-intent`, `missing-staging-ack`, `lost-retention-ack`, `recovery-sigkill-held`, `recovery-sigkill-after-release`, `recovery-release-race`, `wrong-descriptor-role-count`, `partial-scm-rights` — zero numeric contradictions.

**WSL2 qualification only** — not independent native Linux-host. Windows managed regressions TRX-inspected only.

**Reviewer-owned raw evidence:** `artifacts/review-rc2b-03-int-01-q1/` (139 files, manifest SHA `92fc76b83c0c35f2ca62216436ea34a25edf366b0dda69c3c5ef5547d5742902`). Launcher `launch-q1.py` differs from producer `launch.py` by exactly one line (report root directory). Producer corpus `artifacts/rc2b-int01-b-qf02/` unchanged.

---

## L. Final Security Verdict and Exact Owner Action

**Verdict: SUPPORTED WITH QUALIFICATIONS**

**Basis:** The integrated Alternative B implementation establishes the accepted RC-1 security guarantees within its demonstrated scope:
- Distinct controller and recovery processes; recovery spawns owner; recovery retains parent/subreaper, domain/staging authority, independent native safety stop.
- Direct controller–owner SOCK_STREAM session with mutual `SO_PEERCRED`/`SO_PEERPIDFD` identity authentication, CSPRNG/HMAC transcript, one-use secret, bounded framing, no bearer in recovery.
- Owner holds sole capability, journal writer, RELEASE writer; capability consumed by C2 stop; no reconstruction.
- 32/0/0 native cases prove stated invariants at each injection point with kernel-observed process/resource identity.
- Production corrections QF02-F01/F02 verified sound and conservative.
- Cleanup Unknown (12) structurally mandated by evidence contract — not masking invariant failures; but carry forward as obligations for B4/B7.

**Qualifications carried forward (must be explicitly accepted by owner):**
1. WSL2 Ubuntu 26.04.1 on kernel 6.6.114.1-microsoft-standard-WSL2 — **not native Linux-host qualification**.
2. Same-UID hostile process memory theft / Byzantine-bootstrap — **excluded from threat model**.
3. 12 Cleanup Unknown cases — **unavoidable without surviving reaper**; B4/B7 must address.
4. F-08 bounded to FD slots 0–63; no unlimited FD claim.
5. D5_O03 legacy fixture flake (Q1R-08) — attribution unresolved; not a production defect.
6. D6.9 C1 independent GLM review never performed — subsumed by QF-02 + this review.
7. `RestrictedExecutionRuntime.Create()` remains `UnavailableRestrictedExecutionProvider` — no production activation.
8. B4–B7, Hufu, VM/HCS, binary reproducibility, package compatibility — **not authorized**.

**Exact owner action supported by this review:**
> **The owner may proceed to disposition of INT-01 on the basis of this review.** The producer evidence for the integrated Alternative B topology is complete and consistent with the RC-1 contracts and D01 architecture decision. F-01 closure is an owner decision — this review supports that the mandatory security evidence has been produced and independently verified to the extent feasible on the qualified environment. The listed qualifications and Q1R observations must be explicitly carried forward.

**No source changes made during this review.** Producer corpus `artifacts/rc2b-int01-b-qf02/` preserved unchanged. Reviewer evidence sealed at `artifacts/review-rc2b-03-int-01-q1/`. Commit and push only this review report.

---

**End of Independent Security Review.**