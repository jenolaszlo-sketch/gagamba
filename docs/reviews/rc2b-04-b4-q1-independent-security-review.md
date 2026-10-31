# RC2B-04-B4-Q1: Independent Protected Capture and Native Stop Security Review

**Commission:** GAGAMBA-RC2B-04-B4-Q1.
**Date:** 2026-10-31 (Asia/Manila).
**Reviewer role:** independent security review only. No implementation, remediation, assertion change, owner acceptance, or producer-corpus modification was performed. All producer artifacts, historical decisions and raw evidence were read in place and rehashed; every reviewer-owned result is kept separate under `artifacts/review-rc2b-04-b4-q1/` (untracked, hashes in §K).

**Verdict (§L): SUPPORTED WITH QUALIFICATIONS.**

---

## A. Git, source and artifact binding

| Identity | Claimed | Independently verified |
| --- | --- | --- |
| Remote `main` | `ca7200937fe12f0cbccccd72ee157528630e9670` | `git fetch origin main`; `origin/main` = `ca720093…` — match |
| Tested implementation | `4bb4943df69af5f95b65300d90852c04d08a1926` | Commit exists; sole child of baseline `4a181dc41d865bd46c6fca68a64fc7ea3a7fb26b` — match |
| Tested implementation tree | `6c9535013e98fb83f72d4b628a03c59b0e99a1c7` | `git rev-parse 4bb4943^{tree}` = `6c953501…` — match |
| Original B4 baseline | `4a181dc41d865bd46c6fca68a64fc7ea3a7fb26b` | Direct parent of the implementation commit — match |
| Evidence commit | documentation-only child | `ca72009` modifies exactly: `docs/PROJECT-STATUS.md`, `docs/handoff.md`, `docs/work-queue.md` (pointers) + new `docs/evidence/rc2b-04-b4-impl-01.md`, `…sha256-manifest.txt`. No historical decision, review or evidence document was modified. |
| Subtree file count | 306 source/test/native/eng files compared | `git ls-tree -r 4bb4943 --name-only src tests native eng` = exactly 306 |
| 306-file source comparison | zero differences | `git archive 4bb4943 src tests native eng` (306 files) byte-compared against `artifacts/rc2b-04-b4-impl-01/tested-source.tar` (1,359 files): 0 only-in-commit, 0 content differences. The private-build-source binding claim is independently established. |
| Runner identities | `identity.json` pins | `eng/b4-native-matrix.py` = `fd0f1aa5…`, `eng/int01-native-matrix.py` = `b8dbfca4…`, `eng/int01-fault-matrix.py` = `e642c194…` — all three match the committed working tree byte-for-byte |
| Binary/native pins | `artifact-identities.json` | 21/21 spot-verified in WSL: Owner/Recovery/B2Controller DLL closures (incl. shared `Gagamba.Execution.Linux.M1.dll` `871a59ba…` and `Gagamba.Execution.dll` `5a233789…` across all three), native `target-0`…`target-6`, all five coordinator variants — all match |
| Delivery receipt | Main/RemoteMain | `delivery-receipt.json` records `Main=ca72009…`, `RemoteMain=ca72009…` — match |
| Environment pins | evidence §4 | `environment-final.json`: UID 1000, kernel `6.6.114.1-microsoft-standard-WSL2`, Ubuntu 26.04.1, glibc 2.43, systemd 259.5, .NET 10.0.13/SDK 10.0.113, boot `b87db9d9-f88e-4bb2-8b65-d85879947625` — consistent with the report |

Historical preservation: QF-01 (158) + QF-02 (528) manifests rehashed — **686/686 match, zero differences**. `git diff --name-status 4a181dc ca72009` shows no historical path modified. Historical INT-01 raw evidence remains unchanged.

## B. Evidence manifest verification

`docs/evidence/rc2b-04-b4-impl-01.sha256-manifest.txt`: SHA-256 `b02f2f73660c8e8aa241fb59bed88542fb6bd3db1bd4c716db102c1d59800397` (matches the report), 281 entries. All 281 entries were rehashed against `artifacts/rc2b-04-b4-impl-01/`: **281 OK / 0 mismatch / 0 missing**. Two on-disk files are outside the manifest — `delivery-receipt.json` and `sha256-manifest.txt` — both deliberately separate per evidence §1. The raw corpus is present and internally consistent; the manifest is not merely committed, it reproduces.

## C. Capture custody and concurrent quota assessment

Source: `src/Gagamba.Execution.Linux.M1/Launch/ProtectedCapture.cs`, integration in `Launch/BubblewrapAdapter.cs`, `Lifecycle/OwnerPreparationTransaction.cs`, `Bootstrap/OwnerSafetyMonitor.cs`.

- **Fixed ceilings are code, not configuration.** `StreamCeiling=65536`, `AggregateCeiling=131072`, `ReadCeiling=16384` are constants; the constructor throws if `manifest.Channels` requests more. Each stream allocates exactly one fixed `byte[limit]`; there is one 4096-byte stack read buffer per drain thread. Storage cannot grow: after overflow `retain=0` and the drain only discards while `Seen` (saturating at `ulong.MaxValue/2`) keeps counting.
- **Aggregate accounting is synchronized.** The shared `_stored` counter and both `Stored` cursors are updated only under `_gate`; `room = min(stream remaining, aggregate remaining)` can never be negative. The producer's `simultaneous-overflow` run observed `65536+65536 = 131072` stored — exactly the aggregate ceiling — with both streams latching overflow.
- **Drains start before admission.** `ProtectedCapture` is constructed (both threads started) immediately before `NativeLaunchInterop.TrySpawn` in `PrepareHeldAsync`. Helper/setup output is charged to the same budgets; `SettleSetup` implements the synchronized quiet-drain barrier that preserves the pre-HELD constructor-marker check.
- **No target access to custody.** The `LaunchDescriptorMap` handed to the sandbox contains only write ends (FD 1/2) and the fixed FD set 0–11; the read ends live only in owner memory and the parent closes every child endpoint immediately after the atomic spawn. The stdin writer is disposed before spawn (FD 0 is provably EOF-only). Capture APIs accept a stream selector and checked offsets only — no path, no descriptor.
- **No integer/negative conversion vulnerability found.** `Seen` saturates; `(int)count` is bounded by the 4096 buffer; `Read()` rejects `offset > 65536`, `count < 0 or > 16384`, and slices only within `[0, Stored)`. All `checked` conversions reviewed.
- **Lifecycle.** `Fault` bounds the drain to +6 s and invokes the stop callback exactly once (Interlocked); `AcceptStop`/`BindCapture` re-bound the drain when stop latches first; `Finish` bounds +1 s and joins; expiry disposes the reader, marks `Expired`, never invents EOF, and never extends any deadline (`BoundDrain` only shortens — proven by the committed `UnclosedDescendantWriterExpiresWithoutInventingEofOrExtendingDeadline` test and independently rerun). Missing custody or failure state is never empty-success: `Healthy` gates the release prerequisites, and `Complete=false` forces outcome `Unknown` (§E, §H).
- **Findings C-1 (LOW) and C-2 (INFO)** below: drain-thread mortality outside the three caught exception types, and the fixture-gated store-failure hook living in the production adapter.

## D. Capture-reader API security

Source: `src/Gagamba.Execution/Restricted/Control/RestrictedExecutionOperation.cs` (public facade), `RestrictedResults.cs` (results), `Native/OwnerProtocol.cs` (wire), `Lifecycle/HeldReleasePhase.cs` (owner handler), `Bootstrap/IntegratedB3Client.cs` (client), `Lifecycle/OwnerHeldWire.cs`.

- **Bounds before allocation.** The facade validates stream tag, `count` in `[0,16384]` and `offset ≤ 65536` before any allocation; `ProtectedCapture.Read` re-validates under the gate and returns `state.Buffer.AsSpan(offset, len).ToArray()` — an immutable copy; the committed mutation test proves caller-side writes do not affect custody.
- **Exactly one pending read.** `_captureReadInFlight` under the operation gate; concurrent second calls return `Unavailable` immediately (no queueing, no accumulation). A cancelled wait (`WaitAsync(waitCancellation)`) throws to the caller but does **not** free the slot — the slot is released only in the core task's `finally` when the actual exchange completes. The committed B1 regression proves: cancelled wait → `OperationCanceledException`, subsequent read refused while the first is still in flight, and a stop request still submits `LocallyQueued` during an in-flight read — capture reads cannot block stop.
- **Wire shape enforced on both channel paths.** `CaptureRead=34` payload length exactly 13; `CaptureBytes=35` payload 1–16385. `OwnerProtocol.HasValidControlPayloadShape` is applied in `TryReceiveAllowedWithin` for both the direct-session and socket paths before any handler runs, so a malformed capture read cannot reach the owner handler (no crash path). The owner handler additionally guards `payload[0] ≤ 1` and answers `[0]` on refusal.
- **Association and role authority.** `CaptureRead`/`CaptureBytes` are in `DirectControl` (original controller only) and in `ForbiddenRecovery` (recovery may neither send nor receive them). The committed authority test proves a `RecoveryOnly` channel refuses both with the role error. No descriptor, bearer or release authority has any wire representation in the capture path; session sequencing, framing, cancellation and reconnect rules are unchanged, and the new messages ride the existing sequenced channel.
- **Recovery gains no authority through the new surface.** The recovery cleanup path uses only retained handles and the journal (§G); no capture or release capability is reachable from recovery code.

## E. Native stop independence and process evidence

The four populated-domain cases were verified field-by-field from producer raw JSON (`artifacts/rc2b-04-b4-impl-01/native-final/cases/`) and re-executed independently (§K). For every case, both records show:

1. `PopulationBefore`: `cgroup.events` = `populated 1` on a real domain inode (the target was admitted and live).
2. Owner paused at the `owner-before-observation` barrier (`fault-owner-before-observation.json` written; `Point()` blocks until the harness continues it).
3. `PopulationBeforeOwnerExit`: `populated 0` sampled from `cgroup.events`/`cgroup.procs` while `OwnerAliveAtEmptySample=True` (pidfd probe) — the Q1R-01 pre-exit population evidence.
4. `fault-owner-native-stop.json` with `KernelAccepted=True` and the expected cause.
5. Observer continuation only after the empty observation (`continue-owner-before-observation` touched afterwards).
6. The test watchdog did not produce the observed stop: harness SIGKILLs execute only in the runner's `finally` (recorded in `WatchdogActions`), after the empty sample and native-stop receipt; the stop latency window (≤2 s) is far inside the unit's `RuntimeMaxSec=300`.

| Case | Producer upper bound | Independent rerun upper bound | Cause (both) |
| --- | --- | --- | --- |
| `controller-loss-with-blocked-observer` | 8.738 ms | 8.802 ms | `CallerLoss` |
| `full-status-buffer-stop` | 11.143 ms | 2.427 ms | `SetupFault` |
| `overflow-with-blocked-observer` | 180.266 ms | 169.820 ms | `OutputOverflow` |
| `recovery-loss-with-blocked-observer` | 6.721 ms | 9.094 ms | `SetupFault` |

All bounds are measurements of those executions, not guaranteed maxima (the overflow bound includes the target's pre-output pause). `full-status-buffer-stop` saturated the coordinator status pipe with 64,512 pending of 65,536 nominal bytes (page 4096, atomic frame 96) — the owner sampled **its own** retained status FD via `FIONREAD`/`F_GETPIPE_SZ` (`Int01Qualification.ObserveFullStatusPipe`); no `/proc/<owner>/fd` access exists.

Deadline and admission preservation: `Fault`'s +6 s drain bound and `Finish`'s +1 s bound only shorten; the monitor's overall/setup deadlines are unchanged; the status-backpressure observation hook is itself bounded by `min(overallDeadline, now+2s)`. Release admission was strengthened, not weakened: `Prerequisites()` now additionally requires `Capture.Healthy` and open stdout/stderr/status readers, so capture faults inhibit release; overflow terminates execution through the safety monitor without waiting for blocked observers, controller responses, journal settlement, or the other drain. `target-status-is-data` (both producer and rerun): trusted root exit **7**, outcome `TargetFailed(5)`, while the captured stdout retains the fake `{"Exec":"Confirmed","Root":{"ExitCode":0}}` JSON as data — captured output never classifies exec/root.

## F. Disposal and reaping lifecycle assessment

**Q1R-06 (`OwnerSafetyMonitor.Dispose`)** — verified fixed in `Bootstrap/OwnerSafetyMonitor.cs`:

- `Dispose` first calls `AcceptStop(StopCauseKind.CallerStop)`: latches stop, preserves the first cause (`_firstCause ??=`), bounds the capture drain, requests the native stop on the published domain, consumes the capability when no arbiter is bound, and dispatches the durable stop worker — all before any descriptor is closed.
- The monitor thread keeps running (native stop + population observation) while `Dispose` joins the durable stop worker, so wedged journal I/O cannot suspend native stopping; the domain and pidfd descriptors are disposed only after the monitor joins. No nested locks exist (no lock inversion); the worker join can extend `Dispose`'s own duration but never the retained operation deadline.
- C2 vocabulary preserved: the committed regression asserts the capability is consumed (`IsLive == false`), a durable `StopRecord` exists, a post-consumption release is **not** `Released`, and the physical transport attempt count is 0. It does not require a `Stopped` decision — `Refused` after consumption is accepted, exactly as the mission instructs. Failing-before evidence: `baseline-disposal/baseline-disposal.trx` = 0 passed / 1 failed.

**Q1R-07 (recovery loop)** — verified in `Bootstrap/IntegratedRecoveryHost.cs`: the one-second blocking `WaitForOwner` is replaced by `WaitForOwner(TimeSpan.Zero)` + `ReapAdoptedChildren(TimeSpan.Zero)`, retried each ~10 ms loop iteration until collection completes (line 124 and 128–129), so:

- Eventual owner-exit collection and adopted-children reaping are preserved (retried, not dropped); subreaper responsibilities unchanged.
- Native stop progress is independent: `Stop()`/`SafetyStopRetained()` execute every iteration regardless of reaping or journal state; the stop obligation never depends on journal settlement or capture completion.
- No simultaneous journal writers: the cleanup task starts only after `ownerExited && ownerReaped && preparation.IsCompleted`, and `writerFence` is set only after `journal.TryAcquireWriter` succeeds inside the cleanup task.
- No spinning or unbounded resource use (10 ms sleep; bounded, zero-timeout probes). Owner exit is classified only from the kernel pidfd fact; cleanup classification is separate (§I).
- Residual note: a permanently un-reapable owner would keep the loop alive without new allowances (bounded by the service `RuntimeMaxSec`); a liveness-only observation, not a security defect.

## G. Retained-directory identity and cleanup review

Source: `Restricted/B3Qualification.cs` (`B3ScopeCleaner`), `Bootstrap/IntegratedRecoveryHost.cs` (fallbacks), `Lifecycle/DomainLease.cs` (`RemoveIfIdentityMatches`), `Preparation/Native/PreparationInterop.cs`.

- **Mechanics.** Staging-directory removal requires: (1) `StatFd` on the retained staging descriptor matching the allocation identity, (2) reopen of the **name** under the retained parent via `openat2` with `O_NOFOLLOW` + `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`, (3) `StatHandle` of the reopened object matching the retained identity, (4) `unlinkat(AT_REMOVEDIR)`. `AT_REMOVEDIR` is rmdir semantics: empty-only, never follows symlinks, cannot recurse. Domain removal re-looks the child up under the retained parent and requires the exact captured dev/ino before removal.
- **The three controls verified twice.** Producer raw records and my independent rerun (12/12 components, including `RetainedDirectoryCleanupTests`) both show: original → `Cleaned` and removed; replacement directory at the same name → `Mismatch`, both objects preserved; symlink at the same name → `Mismatch`, preserved. Baseline failing-before: `baseline-cleanup/baseline-cleanup.trx` = 1 passed / 2 failed (the original positive passed; replacement and symlink were incorrectly removed/refused before the fix).
- **Fallbacks are anchor-bound.** The recovery domain fallback runs only after reaping and writer fencing, requires `lease.VerifyIdentity().Match`, `RequestStop().Ok`, and an observed-empty population before `RemoveIfIdentityMatches()`. The staging fallback requires exactly one retained staging directory **and** a journal staging allocation **and** a `StatFd`-derived identity from the live retained descriptor (the journal's dev/ino is never trusted over the retained descriptor — the `with { Device, Inode }` override takes identity from the retained fd and the name must still resolve to it under the retained parent). A journal-only name with no retained descriptor is never adopted; missing accounting skips cleanup rather than fabricating it.
- **G-1 (LOW, residual risk — reported, not remediated).** The final identity validation and `unlinkat` are two separate syscalls on a name; they are not atomic. A concurrent same-UID actor can substitute an **empty** directory at the name inside the window, and rmdir would remove the substitute while the retained original survives (renamed away). Nonempty substitutions fail closed (`ENOTEMPTY`), symlinks are refused (both by `openat2` and by `unlinkat`), and no unknown content can be deleted. This is exactly the residual risk the commission asked to be stated if race-free identity-bound removal cannot be guaranteed: **it cannot, with plain POSIX name-based removal**. Within the accepted threat model (same-UID hostile host code explicitly excluded from isolation claims) this is a bounded residual risk; the "fixed for qualified Linux filesystem path" claim is accurate for the tested controls but is not a race-freedom claim. A future hardening (e.g., rename-then-verify dance or kernel-assisted removal) belongs to later stages.

## H. Thirteen-case qualification audit

Producer matrix: `native-final/cases/results.json` = 13 passed / 0 failed / 0 skipped, exact case list matching the commission. Per-case facts below combine producer raw JSON, source review, and — where marked ✓R — my independent rerun in a fresh transient unit.

| Case | Injection / stage | Actual fault evidence (raw) | Security invariant | Native evidence | Capture / release / outcome | Cleanup | Independent |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `copied-custody-positive` | none (control) | exact retained copies | custody fidelity; `Read` returns copies | populated 1; release delivered | 20+20 bytes stored/observed, Complete; copies byte-exact; Outcome=Succeeded(4) | Confirmed | ✓R |
| `stdout-overflow` | target mode 1 (1 MiB stdout) | stored 65536, observed 135168, Overflow | first excess byte stops | populated 1 → stopped | Outcome=Unknown(0); release attempted once | Confirmed | ✓R |
| `stderr-overflow` | target mode 2 | stderr 65536/135168, Overflow | same, stderr side | populated 1 | Outcome=Unknown | Confirmed | inspected (mechanism = stdout rerun) |
| `simultaneous-overflow` | target mode 3 | both 65536/135168; aggregate 131072 | aggregate ceiling exact | populated 1 | Outcome=Unknown | Confirmed | inspected |
| `overflow-with-blocked-observer` | mode 1 + owner barrier | overflow latched while owner paused | stop without observer | 180.3 ms; KernelAccepted; owner alive at empty | stored 65536 truncated; Outcome=Unknown | Confirmed | ✓R (169.8 ms) |
| `recovery-loss-with-blocked-observer` | recovery SIGKILL at barrier | reaper death | stop without reaper; loss not promoted | 6.7 ms; KernelAccepted | Outcome=Unknown | **Unknown** (asserted by runner) | ✓R (9.1 ms) |
| `controller-loss-with-blocked-observer` | controller SIGKILL at barrier | CallerLoss | stop without controller | 8.7 ms; KernelAccepted | no release result (controller gone) | Confirmed | ✓R (8.8 ms) |
| `full-status-buffer-stop` | status-flood coordinator + queued stop | status pipe 64512/65536 full | stop with status channel blocked | 11.1 ms; KernelAccepted | Outcome=Unknown | Confirmed | ✓R (2.4 ms) |
| `setup-output-overflow` | coordinator floods before HELD | 20,000×13 B pre-HELD writes | setup output charged to same budgets | no admission | Preparation Kind=4 (refused), no release | Confirmed | ✓R |
| `setup-constructor-marker` | marker before HELD | marker on stderr pre-HELD | constructor output forbids publication | no admission | Preparation Kind=4; `Release=None` in journal | Confirmed | inspected (mechanism = setup rerun) |
| `capture-store-failure` | `b4-store-failure` hook | both streams ReadFailure; first 20 B not stored | custody failure stops | native stop cause `OutputReadFailure`, KernelAccepted | Outcome=Unknown; FinalDrain=Incomplete | Confirmed | component test rerun (same hook) |
| `root-exit-descendant-writer` | mode 6 (forked writer) | root exit 0; descendant writes 1.5 s later | root exit ≠ all writers exited; drain outlives root | populated 1 | stdout 31 B = root+descendant, Complete; Outcome=Succeeded | Confirmed | component test rerun (drain-outlives-writer) |
| `target-status-is-data` | mode 4 (fake status, exit 7) | fake `Exec/Root` JSON captured | captured output never classifies exec/root | populated 1 | root exit **7**; Outcome=TargetFailed(5); fake JSON retained | Confirmed | ✓R |

Unique coverage: 13/13 cases covered (8 independently rerun, 5 inspected with their mechanisms independently exercised — no overlap inflation; see §K).

## I. INT-01 regression and cleanup reconciliation

`int01-final/cases/`: 32 case JSONs — **32 passed / 0 failed**, cleanup **21 Confirmed / 11 Unknown** by the harness rule (`Confirmed` iff all five `RecoveryFinal` flags true). The 32 names match the accepted inventory exactly; the 11 harness-Unknown cases match the historical categories (5 recovery-death, `controller-before-preparation`, `owner-before-retention`, `partial-scm-rights`, `wrong-descriptor-role-count`, `integrated-ambiguous-intent`, `missing-domain-ack`, `missing-staging-ack`).

**I-1 (finding F01 — MEDIUM, reporting/classification).** `missing-staging-ack` is classified **Confirmed**, but the durable journal verdict is **Unknown**:

- Producer run (`gagamba-qf02-pbim_72j`): `integrated-journal-final.json` = `Sequence 5, Cleanup 4`. `OwnershipCleanupState.Unknown = 4` (`Recovery/OwnershipRecord.cs`). The raw journal generations `gen.a`/`gen.b` (548 bytes, identical) corroborate. The staging allocation at injection is `prep-001, Device=0, Inode=0, Confirmed=false` — the omitted ACK means the accounting was never completed.
- Code path: the Reconciler's staging component compares `StatFd(retained)` against the journal entry `(0,0)` → `Mismatch` → staging `Unknown` → final verdict `Unknown` → `JournalFinalization` is recorded **Confirmed as the persistence of an Unknown verdict** (`Reconciler.cs` lines 437–464: `TryPersist(bits, verdict)` succeeds for `verdict=Unknown`). The new B4 fallback then physically removes the retained staging directory *after* reconciliation without any journal write, flipping the snapshot flag `StagingRemoved` to true. The harness's five-flag rule then reports `Confirmed`.
- **Independently reproduced** (`artifacts/review-rc2b-04-b4-q1/int01-rerun/missing-staging-ack/`): the rerun passed with identical flags and its own `integrated-journal-final.json` again records `Cleanup 4, Sequence 5`, with the identical unconfirmed `prep-001 (0,0,false)` allocation.
- Expected per accepted contract: the INT-01 owner acceptance §7 assigns `missing-staging-ack` to the "Missing acknowledgment finalization … component stop evidence not promoted to full cleanup" Unknown category, and the B4 evidence's own `missing-domain-ack` treatment ("improved physical cleanup must not be promoted … journal finalization is not promoted") applies the same standard. Journal finalization *happened* here, but what was finalized is an **Unknown** verdict — the same-UID-cannot-fabricate-accounting rule is upheld by the implementation and overstated by the classification.
- Actual result: physical cleanup completed (domain removed/empty, owner reaped, writer fenced, staging directory physically removed) — the improvement is real — but the durable record does not support "Confirmed".
- Disposition requested: erratum. The truthful tally is **20 Confirmed / 12 Unknown**, with `missing-staging-ack` carrying the same "physical cleanup improved, durable verdict Unknown" annotation as `missing-domain-ack`. The implementation needs no change; the runner's classification semantics (or the report's reading of them) do. This does not alter any B4 security invariant.

**Other reconciliation verified clean:**

- `missing-domain-ack`: journal `Cleanup=0` (NotStarted) with `DomainRemoved=True`, `StagingRemoved=False`, `JournalFinalized=False` → correctly **Unknown**; physical improvement honestly not promoted.
- `owner-before-retention`, `wrong-descriptor-role-count`, `partial-scm-rights`: journal `Cleanup=0`, no retained-domain cleanup → correctly Unknown (incomplete authority transfer / no lease salvaged).
- Recovery-death cases and `integrated-ambiguous-intent`: no `RecoveryFinal` (no surviving witness) → correctly Unknown; no reaper/finalization authority was created for them.
- `controller-before-preparation`: no journal, no domain, no transaction → correctly Unknown.
- The harness's Confirmed cases that were checked at journal level (`blocked-observer-native-stop`, `lost-retention-ack`) record `Cleanup=2 (Confirmed)` — consistent.
- Gagamba cleanup is never inferred from systemd or watchdog cleanup: `WatchdogActions` are reported separately, `PopulationBeforeWatchdog` is recorded when a domain still exists, and the runner asserts the recovery-loss case **cannot** be Confirmed.

## J. Findings and severities

| ID | Severity | Finding | Expected vs actual | Evidence | Disposition requested |
| --- | --- | --- | --- | --- | --- |
| **B4Q1-F01** | **MEDIUM** (reporting/classification; no implementation defect) | `missing-staging-ack` promoted to Confirmed although the durable journal verdict is `Unknown` | Expected: Unknown per owner-acceptance §7 category and the project's no-fabricated-accounting rule. Actual: Confirmed via five-flag rule after a post-finalization physical fallback removal that the journal never recorded | Producer journal `Cleanup=4`; independent rerun journal `Cleanup=4`; `Reconciler.cs` Unknown-verdict persistence; `IntegratedRecoveryHost.cs` post-reconcile fallback | Owner-recorded erratum: cleanup tally 20 Confirmed / 12 Unknown with the physical-improvement annotation; optional future runner-semantics correction |
| **B4Q1-F02** | LOW (residual risk, within accepted threat model) | Identity-bound removal is check-then-remove by name; not atomic | Expected: race-free identity-bound removal. Actual: a same-UID concurrent actor can substitute an empty directory inside the stat→rmdir window (nonempty fails closed; symlinks refused; retained object survives) | `B3ScopeCleaner.CleanOne` / `DomainLease.RemoveIfIdentityMatches` syscall sequence; controls verified in both records | Report as bounded residual risk; defer hardening to a later stage; do not claim race-freedom |
| **B4Q1-F03** | LOW (robustness) | `ProtectedCapture.Drain` catches only `IOException`/`ObjectDisposedException`/`InvalidOperationException`; an exotic drain-thread exception exits silently without a fault witness or stop | Expected: any drain failure yields an explicit fault. Actual: exotic exception (e.g., OOM) leaves `Healthy=true`; outcome still fails closed (no EOF → `Complete=false` → Unknown) and kernel pipe bounds prevent unbounded growth, but the overflow-stop guarantee is narrowed in that window | `ProtectedCapture.cs` catch clause | Accept as bounded robustness note for B5+; no B4 blocker |
| **B4Q1-F04** | INFO (hygiene) | Test-only store-failure hook (`QualificationBeforeCaptureStore` / `_beforeStore`) lives in production `BubblewrapAdapter`/`ProtectedCapture`, gated to the explicit `b4-store-failure` fixture profile (null otherwise) | Expected: test seams outside production paths. Actual: inert in production; reachable only under a validated experimental fault profile | `BubblewrapAdapter.cs` line 25/118; `OwnerPreparationTransaction.cs` lines 407–410 | Accept for the experimental profile; move to a test seam before production hardening |

Explicitly checked and **not** defects: the malformed-`CaptureRead` crash concern (payload shape enforced on both channel paths before the handler); lock inversion in disposal (none); capture reads blocking stop (refuted by committed test); `BoundDrain` extension (only shortens, proven); recovery spin (10 ms sleep); symlink following (openat2 resolve flags + `AT_REMOVEDIR`); aggregate over-allocation (exact ceiling observed); deadline extension (none found; `Math.Min` bounds everywhere); production provider activation (`RestrictedExecutionRuntime.Create()` still returns `UnavailableRestrictedExecutionProvider`).

## Disposition of carried items (per §13 of the commission)

- **Q1R-01** — **Improved / addressed within this profile**: population is sampled while the owner is alive and before observer continuation in all four B4 cases and in the INT-01 `blocked-observer-native-stop` case; identity/inode recorded; native receipts present. Verified in source (`OwnerSafetyMonitor.Monitor`, fault-matrix change), raw records, and independent reruns.
- **Q1R-06** — **Fixed** (bounded implementation correction with failing-before evidence); C2 vocabulary preserved; no second physical RELEASE.
- **Q1R-07** — **Fixed** (nonblocking reaping); stop authority and obligation independent of journal settlement and capture completion.
- **R1-F02** — **Partially addressed; remains globally OPEN.** The B4 experimental-profile obligations (queued stop/pre-mint ordering, first cause, capture faults, native stop under blocked observation) are qualified here; lifecycle/platform/deployment obligations are not closed. R1-F02 must not be closed globally by this review.
- **Retained-directory identity safety** — supported for the tested controls, with the bounded residual race (F02).
- **Capture quota/concurrency correctness** — supported (code-enforced ceilings, synchronized accounting, exact-ceiling observation, independent reruns).
- **Capture custody and access control** — supported (private owner memory; no target reachability; authenticated controller-only; recovery forbidden; single-slot immutable-copy reads).
- **Cleanup Unknown categories** — correctly represented in 10 of 11 Unknown cases and in the improved-but-not-promoted `missing-domain-ack`; `missing-staging-ack` is the one over-promotion (F01).

Not claimed: native Linux-host qualification (WSL2 only), same-UID malicious-host isolation, B6 attestation, sealed B5 C3, B7 completion, production readiness.

## K. Independent test execution, exact case inventory and evidence limitations

**Environment.** All independent execution ran on the same WSL2 Ubuntu 26.04.1 host (UID 1000; `CapInh/CapPrm/CapEff/CapAmb` all zero inside the units — the WSL session's inherited `CAP_CHECKPOINT_RESTORE` bit was explicitly dropped with `setpriv` before the preflight; `NoNewPrivs=1`; `Delegate=memory pids`, `DelegateSubgroup=host`, `ExitType=cgroup`, `KillMode=control-group`, `RuntimeMaxSec=240–300`, `TimeoutStopSec=10`, `--collect --wait --pipe`), each run in a **fresh transient user unit** owned by this review (`gagamba-b4-rv-q1-native4/6/7`, `-comp`, `-int01b`). Artifacts were the producer-recorded binaries, re-verified against `artifact-identities.json` (21/21) immediately before use; profiles, workspaces and reports were reviewer-owned (`/home/gaga/gagamba-b4-review-q1`, `artifacts/review-rc2b-04-b4-q1/`). The committed runners were used unmodified (hashes in §A).

**Native B4 matrix — 8 of 13 independently rerun, all passed:**
`copied-custody-positive`, `stdout-overflow`, `controller-loss-with-blocked-observer`, `recovery-loss-with-blocked-observer`, `overflow-with-blocked-observer`, `full-status-buffer-stop`, `target-status-is-data`, `setup-output-overflow`.

**Inspected only (5), with mechanism-level independent coverage:** `stderr-overflow` (stdout-overflow rerun covers the stream-symmetric mechanism), `simultaneous-overflow` (aggregate ceiling additionally verified from producer raw + code), `setup-constructor-marker` (setup-stage refusal rerun via `setup-output-overflow`), `capture-store-failure` (the identical injection hook rerun via `CaptureStoreFailureStopsWithoutWaitingForOtherPipe`), `root-exit-descendant-writer` (the drain-outlives-writer property rerun via `UnclosedDescendantWriterExpiresWithoutInventingEofOrExtendingDeadline`). Total unique coverage: **13/13 with zero rerun/inspected overlap** (the INT-01 inventory error is not repeated).

**Component regressions independently rerun:** 12/12 passed / 0 skipped — `ProtectedCaptureTests` (8), `RetainedDirectoryCleanupTests` (3 controls), `OwnerSafetyDisposalTests` (1, native-primitives profile).

**INT-01 single-case rerun:** `missing-staging-ack` — passed with identical five flags and journal `Cleanup=4` (the basis of finding F01).

**Reviewer-owned raw results (untracked, retained locally):** `artifacts/review-rc2b-04-b4-q1/` — key artifacts SHA-256:

```
73ed358a0ec34be41d8ced0b08c4cbe1e4df1b28bdf9edc46040f48140ed3f78  native-rerun/copied-custody-positive/copied-custody-positive.json
536a3f86b16a5b47d5a6696f96ff0ab9361ef264585268698dcd752ab893cfb4  native-rerun/overflow-with-blocked-observer/overflow-with-blocked-observer.json
6f1ffa7f7248c9f95349f85e07566f010497b20fd6a4404ae539879da2367d16  native-rerun/recovery-loss-with-blocked-observer/recovery-loss-with-blocked-observer.json
cc89475543acf21a4266413317d471ec6d266f342d22a970501f0d36fb05d372  native-rerun/full-status-buffer-stop/full-status-buffer-stop.json
977fd7bc2f5f46cafb594865ba6d9ab7b0b23425a78bed957fb897796ef45d17  native-rerun/target-status-is-data/target-status-is-data.json
f34d62d377f1b523b5cdc58a35e59929d177f55d48788646e6f2f72cb990c53b  native-rerun/controller-loss-with-blocked-observer/controller-loss-with-blocked-observer.json
f615d2af698f7ecc2fec88a294169a45487025adb93ac23049ce3910ae94eac7  native-rerun/setup-output-overflow/setup-output-overflow.json
38b79793e9fcf4814dd70403e4d273ec839a1e074ce69f21618d21fae1f834c2  native-rerun/stdout-overflow/stdout-overflow.json
e7ff7469ca552f3aae661940341a769a6d1ed78f4a074ba1596093c2c89eb50f  components-rerun/reviewer-components.trx
32812124aecec29c5b3fef1bda7169f31b8bd02c099faeda489dead65b5fb7c3  int01-rerun/missing-staging-ack/missing-staging-ack.json
```

**Post-run host audit (this review):** zero remaining reviewer service cgroups, zero live reviewer actors, zero failed units; reviewer test workspaces intentionally retained. The producer's final audit (`SourceFilesChecked 306, mismatches none, RemainingUnits none, RemainingActors none`) is consistent with my own independent probes.

**Evidence and regression audit (§12 of the commission):**

- Windows M1 final TRX: 1,256 results = **989 Passed + 267 NotExecuted** (Linux-native/root-only classes; incl. `ProtectedCaptureTests` 7, `RetainedDirectoryCleanupTests` 3, `OwnerSafetyDisposalTests` 1). The evidence's "989/0/0" is the executed subset; the handoff's "989/0/267" is the full TRX — same file, both accurate, no synthetic total. The earlier `windows-m1.trx` (988/1252) is a superseded iteration; 0 failed in both.
- WSL components TRX: 275 = **272 Passed + 3 NotExecuted** (`Wp05D5NativeStatusTests`, `Wp05ReleasePathIndependentQualificationTests`, `Wp05D4SupervisorTests`); the three helper-dependent cases were then executed separately, each 1/0/0 (`D4_11`, `D5_O03`, `ForgedIdentityGateRefusal…`) — 275 selected cases executed across records; the original three skips are disclosed, not rewritten. Authentication ran separately: 21/0/0. Focused capture+disposal: 9/0/0 (names verified). Execution 68/0/0; Runtime 15/0/0.
- Failing-before evidence distinguished: `baseline-disposal.trx` (0/1) and `baseline-cleanup.trx` (1/2) are genuine implementation-defect reproductions; the status-pipe-witness, protected-descriptor-observation and transient journal-read failures documented in evidence §2 were producer **qualification-harness** defects, honestly recorded as failed iterations (`native12` 10/12, `native13` 12/13 preserved).
- `D5_O03` passing once (legacy-d5 1/0/0, independently rerun here in the components batch) does **not** close the inherited Q1R-08 flake; the evidence says the same.

**Limitations of this review:** WSL2 is not native Linux; all stop latencies are run observations, not guarantees; `missing-domain-ack`/`missing-staging-ack` journal-state conclusions rest on the production-decoded `integrated-journal-final.json` plus raw `gen.a`/`gen.b` bytes (binary format not independently re-decoded field-by-field); the 5 inspected-only native cases were not re-executed end-to-end; no macOS, no native-Linux-host, no Windows-restricted-backend qualification exists to review.

## L. Final verdict and recommended owner action

**Verdict: SUPPORTED WITH QUALIFICATIONS.**

The mandatory B4 invariants are supported within the declared WSL2 experimental scope, on independently verified evidence: fixed capture custody with enforced stream/aggregate ceilings and correct concurrent accounting; overflow, read-failure, custody-failure and expiry handling that fails closed and stops natively without waiting for blocked observers; a bounds-checked, single-slot, immutable-copy capture API restricted to the authenticated original controller; independent native stop proven with populated domains, exact process identities, kernel-accepted receipts and pre-exit emptiness in all four blocked-observation cases (independently rerun); the Q1R-06 disposal correction and Q1R-07 nonblocking reaping verified against their failing-before baselines; identity-bound retained-directory cleanup with all three controls passing twice.

Qualifications (bounded, none blocking the invariants above):

1. **B4Q1-F01 (MEDIUM, erratum required):** the cleanup tally "21 Confirmed / 11 Unknown" overstates the durable record for `missing-staging-ack` (journal verdict Unknown, independently reproduced). Truthful tally: **20 Confirmed / 12 Unknown**, with physical-cleanup improvements annotated for `missing-domain-ack` and `missing-staging-ack`.
2. **B4Q1-F02 (LOW):** identity-bound removal is not race-free against concurrent same-UID substitution (empty-directory window); within the accepted threat model, carry as a documented residual risk.
3. **B4Q1-F03 (LOW) / B4Q1-F04 (INFO):** drain-thread mortality gap and the fixture-gated store-failure hook in production code — carry as B5+ hardening notes.
4. Standing scope qualifications remain: WSL2-only, no native Linux/macOS host, D5_O03 flake unresolved (Q1R-08), R1-F02 globally OPEN, C1–C4 and Option A unchanged, B5–B7/Hufu/VM/HCS/production gated.

**Recommended owner action:** record the B4Q1-F01 erratum (either correct the cleanup tally to 20/12 or record the classification-semantics erratum with the 20/12 reading), then decide B4 acceptance on the basis of this review. Do not close R1-F02 globally; do not authorize B5–B7, Hufu, VM/HCS, packaging or production activation by this result. The implementation requires no remediation for this review's findings; the classification correction is documentation-level.

*Reviewer: independent GLM security review (OpenCode), 2026-10-31. Producer corpus and historical decisions untouched; reviewer raw results kept separate as listed in §K.*
