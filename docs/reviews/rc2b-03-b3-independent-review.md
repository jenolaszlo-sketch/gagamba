# GAGAMBA-RC2B-03-B3 Independent Owner Preparation, Release and Topology Security Review

**Date:** 2026-10-30
**Reviewer:** Independent adversarial review
**Repository:** `jenolaszlo-sketch/gagamba`, canonical checkout on `main`
**Repository root reviewed:** `C:\Users\Laszlos\source\repos\Gagamba`
**Delivery binding:** `3b691c290bcdaf9c01191eb2ed8e552f912eed40` (tree `74641749d8bd5459d47d48b6e3c90898dfdb3bd4`)
**Verdict:** **SUPPORTED WITH QUALIFICATIONS**

> Scope note. This review covers the implemented RC2B-03 B3 slice. It does **not** grant owner
> acceptance, does **not** begin B4, and does **not** redesign the architecture. It records the
> separate controller/recovery/owner **topology conflict** as its own explicit disposition (§F).

---

## A. Exact Git / source / review identities

All identities were verified with `git cat-file`, `git rev-parse`, and `git merge-base --is-ancestor`
against the canonical checkout before any analysis. The current checkout was directly on `main`
with `main == origin/main` at delivery.

| Identity | Value | Verified |
|---|---|---|
| Canonical `main` delivery (HEAD) | `3b691c290bcdaf9c01191eb2ed8e552f912eed40` | commit ✓ |
| Delivery tree | `74641749d8bd5459d47d48b6e3c90898dfdb3bd4` | `rev-parse HEAD^{tree}` ✓ |
| Baseline | `4067f81a8eb095e010db64afd67fd60ddcbccbd4` | commit ✓, ancestor of HEAD ✓ |
| M-01 fix | `880babe9c170756e007dc2b563f92a1e7466990c` | commit ✓, ancestor of HEAD ✓ |
| B3 implementation | `fcfe289ea10b5240054f8d985678ac3361d7ca9f` | commit ✓ (tree `d6f66a4abed83f7f6ffe4468154e9cc17114cbed`), ancestor ✓ |
| Held-phase correction | `a67a640c3de69ee268842fda8ffd189f29e6c9d8` | commit ✓, ancestor of HEAD ✓ |
| Final implementation tree | `05535a2592a58565650d59994cb1fb1af099ef60` | `rev-parse a67a640^{tree}` ✓ |
| Review source | `a67a640c3de69ee268842fda8ffd189f29e6c9d8` | ✓ |
| Producer evidence | `docs/evidence/rc2b-03-b3.md` | present ✓ |

Ancestry is linear and verified: `4067f81` -> `880babe` -> `fcfe289` -> `a67a640` -> `3b691c2`.
Each listed commit is an ancestor of the delivery HEAD (`merge-base --is-ancestor` exit code 0 for all).

**Delivery commit content.** `3b691c2` is the evidence/status commit. It changes `docs/PROJECT-STATUS.md`,
adds `docs/evidence/rc2b-03-b3.md`, and adds a **test-only** evidence-retention change to
`tests/.../Restricted/B3RestrictedLifecycleTests.cs` (a `GAGAMBA_B3_KEEP_EVIDENCE` guard plus a
JSON dump of the prepared identity). No production source changes in the delivery commit; production
source at `3b691c2` is byte-identical to the review source `a67a640`.

**Review artifact (this file).** Committed directly on `main` as the sole change of its commit; no
existing file was modified.

---

## B. Independently reproduced tests and evidence-only findings

### B.1 Environment used for independent execution

| Property | Value |
|---|---|
| Host OS | Microsoft Windows 11 Home Single Language 10.0.26200 build 26200 (AMD64) |
| SDK | `dotnet 10.0.401` (only SDK installed) |
| Build | `dotnet build tests/Gagamba.Execution.Linux.M1.Tests -c Release` -> **succeeded, 0 warnings, 0 errors** |
| Tested binaries | built from the working tree at `3b691c2` (production source identical to `a67a640`) |

### B.2 Independently reproduced (Windows, executed by this review)

| Suite / filter | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| M1 full parallel phase `Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState` | 981 | 0 | 91 | matches producer Windows parallel-phase count exactly |
| `RestrictedControlB3Tests` + `ReleaseAdmissionTests` + `OwnerHeldWireCodecTests` + `RecoveryConnectionDisposalTests` | 31 | 0 | 3 | 3 skips are the Linux-only socketpair M-01 cases |
| `RestrictedControlB1Tests` | 45 | 0 | 0 | updated B1 suite |
| `Gagamba.Execution.Tests` | 68 | 0 | 0 | regression baseline |
| `Gagamba.Runtime.Tests` | 15 | 0 | 0 | regression baseline |

These reproduce the producer's Windows counts in evidence §10 (981/0/91; 68/0/0; 15/0/0).

### B.3 Native (WSL2 / Linux) evidence — **NOT independently reproduced**

The producer's positive native-fixture and real-process cases were **not** independently executed by
this review. Reason:

- The mission forbids modifying host-global cgroup delegation or elevating privileges merely to force
  tests to run. The WSL2 Ubuntu 26.04 instance present on the host
  (`Linux 6.6.114.1-microsoft-standard-WSL2`, glibc/dotnet present, `bwrap 0.11.1`) currently has
  **no operator-delegated cgroup parent** (`/sys/fs/cgroup/gagamba-b3` is absent; `gagamba` uid 1001
  exists but no delegated subtree does), and the default WSL user is `root` (uid 0).
- The B3 process/native tests require an operator-supplied `GAGAMBA_DELEGATED_CGROUP`, a test host
  running *inside* that subtree with uid != 0 and empty effective capabilities, and the
  `artifacts/b2/install` closure (absent). Provisioning that delegation is exactly the host-global
  change the commission prohibits.
- Therefore the native results are **producer-evidence-reviewed only**, not independently
  reproduced. Stated plainly as required.

### B.4 Artifact hash verification (evidence-only checks, independently performed on this host)

| Artifact | Independently computed SHA-256 | Matches evidence |
|---|---|---|
| `artifacts/linux-m1/b3/b3-release-coordinator` | `91c987c7a9563a21104f58ea3ba32da918fd9cb8309c0142573ab76c39fd2d0c` | ✓ |
| `tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture` | `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305` | ✓ (also matches committed `fixture.sha256`) |
| `artifacts/linux-m1/wp04/seccomp-filter.bin` | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` | ✓ |
| `artifacts/linux-m1/wp04/exec-coordinator` | `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b` | ✓ |

### B.5 CI

The public run page for run `40987712653` (job `ci #46`, commit `3b691c2`, branch `main`) reports
**Status: Success**, total duration 4m59s, and explicitly shows **"Re-run triggered"**. The rerun is
real and is preserved here as a fact; it is not itself a B3 defect. The run page is public but the
per-job logs require sign-in, so CI was reviewed as an execution-status record, not independently
reproduced. Three annotations are environment-only (Node.js 20 deprecation).

---

## C. Preparation and HELD assessment

**The owner preparation is genuine.** Trace of the accepted path (`B3Qualification.TryCreate` ->
`B3PreparationOwner.PrepareCore` -> `M1RecoveryScope.StartOwnerPreparation` ->
`OwnerPreparationTransaction.Run`):

1. **Trusted-origin identities.** `B3PreparationOwner.PrepareCore`
   (`src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs:295-427`) mints fresh
   `ExecutionId`/`LeaseGeneration`/`IssuerId`/`StoreGenerationId`/`PreparationId`, reads the real boot
   id from the kernel (`CgroupInterop.TryReadBootId`), and requires exact enrollment ids.
2. **Removed caller-forged readiness.** The manifest is built only from the *actual staged objects*
   (`BuildManifest`, `:450-528`) with each staged SHA-256 re-verified against the profile pins
   (`VerifyStaged`). The owner — not the caller — performs `SafeCopyIn`, mints the capability and
   holds the capability store (`OwnerPreparationTransaction`), so readiness is provider-origin.
3. **Durable preparation before capability mint.** The staging/domain authority handshakes and the
   `AllocationIntent`/`Prepared` journal transitions complete before `PreparationPublisher.Publish`
   (`OwnerPreparationTransaction.cs:320-348`), i.e. before Mint.
4. **Genuine HELD.** The coordinator HELD frame (`GGM1CTL\0`) is decoded and validated
   (`CoordinatorProtocol.TryDecode`/`TryDecodeHeldReport`), the pre-HELD constructor-marker oracle is
   enforced by the adapter (`BubblewrapAdapter.cs:150-152`), and the owner re-verifies the whole HELD
   report (uid/euid/gid, no-new-privs, seccomp=2, dumpable=0, CapEff/Prm/Amb=0, private namespaces,
   mount tree and retained cgroup path ending in the domain name) before returning `Prepared`
   (`B3Qualification.cs:359-403`). Deadline echo is owner-origin and strictly ordered.
5. **No target initialization before release.** The coordinator program performs no `fork`/`exec`
   until a **validated** 128-byte RELEASE frame has been read (`tests/Fixtures/b3-release-coordinator.c:328-367`);
   `launch_target` is only reached after `break`. The adapter additionally fails HELD if the
   constructor marker is present (`BubblewrapAdapter.cs:150-152`).
6. **Owner retains capability, journal writer and sole RELEASE writer.** The `PreparedTransaction`
   capability, the `JournalWriterLease` and the single-use `HeldExecution` control writer stay in the
   owner process. `M1RecoveryScope` receives only domain/staging handle duplicates; it never receives
   a capability or the release writer. No B3 wire message carries a capability, handle or bearer
   (`OwnerHeldWire.cs:1-5` and `OwnerProtocol.cs` B3 message set).
7. **No passive reference becomes live authority.** `PreparedRestrictedExecution` has an internal
   constructor and an internal `Association` object; `RequestReleaseAsync` requires
   `ReferenceEquals(exactPreparation, _prepared)` plus the association and the exact identity/digest
   (`RestrictedExecutionOperation.cs:245-251`). A deserialized or copied reference cannot release.

**Fixed-fixture input bounds and pinning.** `B3Qualification.TryCreate` (`:70-156`) rejects
non-Linux, root, or unprivileged-mismatch (`RestrictedRefusalReason.UnqualifiedInstallation`),
requires fully-qualified paths, rejects path-traversal source names, bounds
`ExpiresLead <= 120 s`, `TotalBudget <= 64 MiB`, `4 <= MaxObjects <= 128`, and verifies the owner DLL,
tool, input, coordinator, filter (length + hash) before any allocation. No public caller reaches this
entry point.

**Provenance qualifications (see findings).** The "fixed profile" is a caller-populated record
(§H F-04); the HELD uid/seccomp/capability facts are coordinator self-reports read from
`/proc/self` (§H F-05); container uid 0 is correctly treated as *not* host privilege (host uid 0 is
refused, `CapEff=0` required). The controller re-verifies the mount tree/namespace distinctness from
the same frame but cannot independently confirm uid/seccomp/no-new-privs/capabilities.

---

## D. R1-F03 / C1–C4 and one-shot release assessment

### D.1 R1-F03 Option A — correctly preserved at both layers

- **Public operation admission** (`RestrictedExecutionOperation.RequestReleaseAsync`,
  `src/Gagamba.Execution/.../RestrictedExecutionOperation.cs:236-307`): retained decision -> `Duplicate`
  with the original C4 decision and an independent binding diagnostic; invalid length ->
  `InvalidArgument`/`InvalidCorrelation`; a pre-retention mismatch against the admitted binding ->
  `Refused`/`Mismatch` with **no submission, no retention, no consumption**; the first fully
  admissible call atomically binds the correlation under `_gate`; a concurrent loser is
  `Refused(ReleaseInProgress)`.
- **Owner-side arbitration** (`ReleaseArbiter.RequestRelease`,
  `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs:54-193`): first admissible correlation
  binds; a later pre-retention mismatch only refuses that call (`:87-96`, `:136-143`); absent vs
  present all-zero 32 bytes are distinct (`IsValidCorrelation`/`CorrelationEqual`, `:512-517`);
  invalid lengths refuse. All in `_gate`; no race replaces the admitted correlation between admission
  and retention.

The two layers cannot manufacture additional authority: the owner is reached only through the public
layer, and both use first-wins admission. The public layer may admit a correlation on a call that the
owner later refuses pre-intent (e.g. deadline), but the owner then never admits a different
correlation (it is gated upstream), so no extra release is possible. The mission's warning that "two
individually passing admission suites are not proof of end-to-end correctness" is addressed by the
real-process `B3StopBeforeRelease` / `B3BarrierStopWins` cases (producer WSL2 evidence) plus the
deterministic owner/public tests.

### D.2 C1 — ambiguous intent persistence

`ReleaseArbiter.cs:195-267` handles `JournalFailureKind.AmbiguousAfterContent`: it retains
`AmbiguousPersistence` (`MayHaveExecuted`, `Delivery.NotAttempted`, `DurableCommitted=false`,
`LiveExecutionExcluded=true`) or the stop-won `Refused(MayHaveExecuted)` per the D6.2 first-retainer
rule, releases `_gate`, then issues the bounded tail stop. The ambiguity flag is raised **only** for
the post-content seal-release failure on a durable Intent (`OwnershipJournal.cs:498-505`); ordinary
write/fsync/rename failures are ordinary refusals. No false definite `NotCommitted`, no second Intent,
no FD4.

### D.3 C2 — irreversible stop consumption

`RequestStop` (`ReleaseArbiter.cs:416-487`) consumes the capability at the first runtime acceptance
under `_gate` (`ConsumeStopCapability`, `:538-550`), for the normal, clock-unavailable and
deadline-overflow paths; a duplicate stop returns the retained stop without consuming again; a
release-first stop records `ReleaseAlreadyCommitted` from `_releaseRuntimeCommitted` (set before
transport, `:346`). Capability consumption is independent of durable-stop persistence.

### D.4 C3 — clock disclosure and no invented continuity

B3 uses the owner `Stopwatch` monotonic clock (`OwnerClock`, `HeldReleasePhase.cs:499-509`) and
discloses it as `owner-monotonic-stopwatch-v1` with `FrequencyHz = Stopwatch.Frequency`
(`B3Qualification.cs:405-409`). It is **not** presented as the sealed production C3 clock; evidence
§5 states the limitation and B5 is explicitly not accepted. Deadlines are the owner-origin echoes
(no renewal; the held lifetime ends at `_overallDeadline`, `HeldReleasePhase.cs:114-121`). The stop
deadline addition is `checked` (`ReleaseArbiter.cs:439-452`; `OwnershipTransitions.cs:130-143`).
B3's cleanup reconciliation still uses the pre-C3 caller allowance (`B3Qualification.cs:732`), which
is consistent with the disclosed deferral of the sealed C3 implementation.

### D.5 C4 — duplicate/concurrent semantics and one-shot release

Sequential repeats return `Duplicate` of the retained decision; a concurrent loser is `Refused`
(public: `ReleaseInProgress`; owner-side: intent-already-present refusal). At most one physical write:
`HeldExecution.TryWriteRelease` uses `Interlocked.Exchange` (`HeldExecution.cs:49-64`), the arbiter
consumes the capability once, and the journal planning refuses a second intent. Independently
reproduced by the passing `ReleaseAdmissionTests` and pre-existing arbiter concurrency tests.

---

## E. Corrected ordering defects and M-01 disposition

### E.1 F01 — exec/root journal transition (fixed correctly)

`HeldReleasePhase.PersistObservations` (`:398-433`) performs **two sequential acknowledged
mutations**: exec first, then root. The transition model enforces ordering and single-assignment:
exec requires a committed release (`OwnershipTransitions.cs:153-199`), root requires exec
(`:201-241`), root cannot precede exec, and an identical repeat is an idempotent no-op that does not
rewrite. A failure between the two mutations leaves truthful partial state (release committed, no
exec/root, or exec durable, no root). A committed release never implies confirmed execution:
`Classify` returns `Succeeded` only with `Delivery==Delivered` **and** `Exec==Confirmed`
(`HeldReleasePhase.cs:367-391`), and `PersistObservations` only writes when
`decision.DurableCommitted && facts.Exec in {Confirmed, FailedBeforeExec}`.

### E.2 F02 — stop/abort channel interleaving (fixed correctly)

The correction adds a reentrant per-owner exchange gate (`M1RecoveryScope._ownerChannelGates`,
`:92-97`, `OwnerChannelGate`, `:694-706`) used by `AbortHeld` (`:629`), `StopHeld` (`:679`),
`ContinueHeld` (`:715`) and `ReleaseHeld` (`:738`), and settles a pending disposal stop (bounded)
before the abort exchange (`B3Qualification.cs:642-711`). The owner's sequenced channel strictly
enforces allowed types and monotonic per-direction sequence, failing closed on any unexpected frame
(`OwnerProtocol.cs:239-264`, `:338-384`), so no response can be matched to the wrong request.
Reentrancy is safe (the barrier callback submits a stop on the same thread, `HeldReleasePhase.cs:230-274`).
Stop remains independently available (the barrier loop accepts `StopRequest`/`AbortHeld`,
`:244-273`), cancellation cannot abandon the mandatory disposal stop (the stop submission task is
awaited before the abort), disposal cannot claim cleanup success without owner exit
(`RestrictedCleanup.State` requires all four components confirmed), journal writer leases are
reacquired only after proven owner exit (`DisposeCore`/`Reconcile`), and timeouts preserve `Unknown`
rather than completion. The retained failing-to-passing evidence is consistent with the fix; the
6/6 repetition is supporting, not exclusive, evidence.

### E.3 M-01 — **CLOSED**

`GuardedDescriptor` (`src/.../Bootstrap/GuardedDescriptor.cs`) serializes the disposed check, every
descriptor use and the single close on one reentrant monitor; `TryUse` returns false once closed and
never invokes the callback; `Dispose` sets the terminal state under the lock, closes exactly once
outside it, and waits for in-flight use. `RecoveryConnection.Send`/`Inspect`/`ObserveAsync` now all
route through `_control.TryUse` (`RecoveryConnection.cs:74-95`); `Control` returns -1 after disposal.
Cancellation ordering and the null loss observation are preserved. Independently reproduced: the four
pure-managed gate tests pass on Windows (close-exactly-once under 8 concurrent disposers; disposal
waits for in-flight use; reentrant use; terminal stability). The three real-socketpair cases skip on
Windows and were evidence-reviewed. No minimal counterexample was found; **M-01 is CLOSED**.

---

## F. Topology-conflict disposition (explicit)

**Classification: Acceptable bounded qualification with explicit deferred integration.**

Independently verified facts (source + descriptor ownership traced at runtime):

1. The positive B3 fixture uses a **distinct, controller-owned `M1RecoveryScope` topology**, not the
   accepted B2 recovery-process topology. `B3PreparationOwner` owns an in-process `M1RecoveryScope`
   (`B3Qualification.cs:205`) and spawns the owner as its **direct child** via `Process.Start`
   (`M1RecoveryScope.TrySpawnOwner`, `:173-236`). The release-capable association is the
   controller-created inherited `SOCK_SEQPACKET` channel handed to the owner through
   `GAGAMBA_BOOTSTRAP_FD` and never through recovery.
2. The owner in the positive fixture has **no dependency on the independent B2 recovery process**.
   Its liveness prerequisite is its **parent pid** (`HeldReleasePhase.cs:71` opens a pidfd on
   `RecoveryInterop.GetParentPid()` = libc `getppid`; `Prerequisites()` `:209-224` reports
   `RecoveryAlive` from that pidfd). In this composition the parent is the controller-side
   `M1RecoveryScope` (the controller/test host), not the independent recovery process.
3. The test `B3RecoveryDeathWhileTheTransactionOwnerIsHeldAltersNothingAndReleaseStillWorks`
   (`tests/.../B3RestrictedLifecycleTests.cs:744-780`) starts a B2 recovery process with
   `delegatedParent: null` (lifeline-only), then starts the HELD owner under the test host's own
   `M1RecoveryScope`, kills the unrelated B2 recovery process, and asserts the owner still releases.
   Release succeeding here is **evidence that the two topologies are not integrated**, not evidence
   that the release path is safe under genuine recovery-authority loss.
4. Authority separation **is** preserved: the independent recovery process reports
   `ReleaseAuthority = false` (`B3RecoveryTopologyRetainsItsDomainWithoutReleaseAuthority`, `:707-742`),
   holds only the accepted B2 wire, and receives no capability or release writer. This matches RC-1
   D05's "Recovery process ... no release-authority reconstruction" and B2's accepted scope.
5. A genuine loss of the *owner's recovery authority* does not leave release possible **in the
   current composition** — the owner aborts autonomously on controller-channel loss
   (`B3ControllerChannelLossWhileHeldAbortsWithoutRelease`, `:553-578`) and the arbiter refuses
   `_stopAccepted`/need prerequisites. However, the intended *independent* recovery does not exist in
   this path, so its failure is simply not observed. This is the deferred integration, not a
   release-authority leak.

Per the mission's prohibition, **no release-capable bearer is transferred through recovery**, and
this review does not propose doing so. The two admissible future integrations are the ones the
producer names: a controller-parented transaction owner with recovery retaining only lifeline duties,
or B6-grade pinned controller->owner authentication.

**Disposition:** the current B3 scope may be accepted independently while recording this gap, but
**integrated controller/recovery/owner supervision must be established before any further live
release qualification.** The `B3RecoveryDeath...` test must be cited only as a separation
demonstration, never as a recovery-loss safety property.

---

## G. Native fixture evidence and remaining restrictions

### G.1 Assessment of the fixed-fixture positive run (producer evidence, not independently reproduced)

The sequence claimed in evidence §7 is consistent with the source:

| Claimed transition | Trusted basis in source | Assessment |
|---|---|---|
| Real HELD evidence | owner-validated coordinator HELD frame, re-verified by the controller | credible (mediator-qualified) |
| Constructor marker absent before release | adapter capture check before HELD (`BubblewrapAdapter.cs:150-152`); coordinator executes nothing before a valid RELEASE | credible |
| Durable intent | `Journal.WriteTransition` under `_journalGate` before the gate | credible |
| Single physical RELEASE | `Interlocked` one-shot writer + single capability consume + no second intent | credible |
| Coordinator-derived exec observation | `ExecTag.CoordinatorFrame` from the status sequence (`HeldReleasePhase.ObserveExecution`) | credible |
| Root exit observation | `RootKind.Exited` + raw wait status from the coordinator frame (sequence 3) | credible |
| Bounded stdout/stderr | `DrainBounded` (64 KiB cap, 2 s allowance), digests reported separately | credible |
| Wrapper reaping | retained `PidFdChildReaper` | credible |
| Domain emptiness | retained domain-handle `ObservePopulation` | credible |

The proven facts do **not** equate: RELEASE delivered with target executed (the arbiter keeps
`Delivered` distinct from exec); a coordinator frame with complete containment; target exit with
complete capture (overflow/EOF are separate flags); empty cgroup with finalized cleanup; process
disappearance with durable stop; cooperative fixture stopping with hostile-population enforcement.
The source preserves every one of these distinctions.

The fixture coordinator has **no unintended early-exec path**: `main` (`b3-release-coordinator.c`,
`:328-390`) reads and strictly validates exactly one 128-byte release frame (`ggm1_release_frame_valid`,
`release_protocol.h:31-52`) before `launch_target`; `write_frame`/`fail` paths never exec. Release
frame validation binds magic, version, kind, length, sequence, execution id, preparation id, digest,
generation, overall-deadline echo, zero padding and `now < deadline`.

### G.2 Remaining restrictions (carried forward; not B3 defects)

- B3's positive execution is a controlled experimental fixture, not production registration
  (`RestrictedExecutionRuntime.Create()` still returns `UnavailableRestrictedExecutionProvider`;
  `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs:9`).
- The full independent B2 recovery process is not integrated with the release-capable owner path (§F).
- Populated-domain stopping is evidenced only for the cooperative fixture.
- B4 protected capture/independent stop, B5 sealed C3 clock/durable reconciliation, B6 deployment
  attestation and B7 comprehensive native fault qualification are **not accepted**.
- No independent native Linux-host qualification exists (this review could not run native tests).
- Same-UID hostile-process isolation is not claimed.
- Hufu entry gates remain separate.

---

## H. Findings

### F-01 — Controller/recovery/owner topology conflict — **MAJOR** (blocks later integration & production, not limited-scope B3 acceptance)

- **Location:** `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs:205,305-351`;
  `src/.../Lifecycle/M1RecoveryScope.cs:173-236`; `src/.../Lifecycle/HeldReleasePhase.cs:71,209-224`;
  `tests/.../Restricted/B3RestrictedLifecycleTests.cs:744-780`.
- **Trigger:** run the positive fixture and the "recovery death" case.
- **Expected vs actual:** expected (per RC-1 D05) recovery to be an independent process that survives
  controller death and retains stop/cleanup authority; actual B3 release-capable owner is a direct
  child of the controller-side `M1RecoveryScope`, and the killed "recovery" in the test is an
  unrelated lifeline process.
- **Consequence:** the release path is not supervised by the independently surviving recovery actor;
  recovery-loss safety for the release path is not demonstrated.
- **Smallest remediation:** integrate one controller-parented transaction owner with recovery holding
  only lifeline duties (or B6 pinned controller->owner authentication); re-run the recovery-loss case
  against the integrated topology.
- **Blocks B3 acceptance?** No, if recorded as deferred. **Blocks later integration/production?** Yes.

### F-02 — `ReleasePrerequisites.RecoveryAlive` is bound to the parent (controller) pidfd — **MINOR**

- **Location:** `src/.../Lifecycle/HeldReleasePhase.cs:71,215-223`.
- **Trigger:** normal HELD release.
- **Expected vs actual:** expected an independent recovery-liveness axis; actual axis is the owner's
  `getppid` (the controller-side scope in this composition).
- **Consequence:** the name overstates the meaning and would silently change semantics if the owner
  is later reparented under the independent recovery.
- **Smallest remediation:** rename/document the axis as the scope/controller-parent liveness, or bind
  it to the independent recovery lifeline once integrated.
- **Blocks B3 acceptance?** No.

### F-03 — Arbiter correlation diagnostic imprecision at entry — **MINOR**

- **Location:** `src/.../Execution/ReleaseArbiter.cs:59-96,519-523`.
- **Trigger:** an invalid-length (or mismatching) correlation presented after a stop or after a
  retained decision.
- **Expected vs actual:** expected the invalid-length form to be reported as `InvalidArgument`;
  actual the retained-Duplicate/stop-won path computes `BindingFor(...)` which yields `Mismatch` for
  an invalid-length array. The public operation layer reports `InvalidArgument` correctly, so the
  caller-facing behaviour is correct.
- **Consequence:** diagnostic only; no authority difference, no consumption, no release.
- **Smallest remediation:** apply the length validity check before `BindingFor` in the retained/stop
  entry paths.
- **Blocks B3 acceptance?** No.

### F-04 — "Fixed" profile is a caller-populated record, not a compiled-in immutable pin set — **OBSERVATION**

- **Location:** `src/.../Restricted/B3Qualification.cs:34-59,70-156`; test harness `Fixture.Profile()`
  in `B3RestrictedLifecycleTests.cs:197-203`.
- **Trigger:** any trusted-internal caller constructing a `B3FixtureProfile`.
- **Expected vs actual:** `TryCreate` verifies file hashes *against profile-supplied pins*, so the
  pinning is only as strong as the internal caller. There is no public path to it.
- **Consequence:** acceptable for a trusted-internal B3 entry point; must not be presented as a
  production immutable profile. B6 must supply reviewed immutable pins.
- **Smallest remediation:** none for B3; B6 fixes the pins as an installed inventory.
- **Blocks B3 acceptance?** No.

### F-05 — HELD uid/seccomp/capability facts are coordinator self-reports — **OBSERVATION**

- **Location:** `tests/Fixtures/b3-release-coordinator.c:158-238`; consumer
  `src/.../Restricted/B3Qualification.cs:380-392`.
- **Trigger:** HELD establishment.
- **Expected vs actual:** the facts are read from `/proc/self` *inside the sandbox by the
  coordinator* and transmitted; the controller re-validates the mount tree and namespace distinctness
  from the same frame but cannot independently confirm uid/seccomp/no-new-privs/capabilities.
- **Consequence:** provenance is "kernel-sourced but coordinator-mediated". This is consistent with
  evidence §6's basis column and must not be described as an independent kernel observation by the
  trusted host. Container uid 0 is correctly not treated as host privilege.
- **Smallest remediation:** none for B3; document the mediation explicitly (already partly done).
- **Blocks B3 acceptance?** No.

### F-06 — B3 prepared views carry an `Unavailable` provider descriptor — **OBSERVATION**

- **Location:** `src/Gagamba.Execution/.../RestrictedProvider.cs:106-131`;
  `B3Qualification.cs:146-149,410-411`.
- **Trigger:** inspect `prepared.Provider` on a B3 view.
- **Expected vs actual:** `InstallationState == Unavailable`, all identities null. Truthful for "no
  production registration", potentially confusing to a consumer.
- **Smallest remediation:** B6 supplies the real descriptor.
- **Blocks B3 acceptance?** No.

### F-07 — "Recovery death" test is a separation demonstration only — **OBSERVATION** (subsumed by F-01)

- **Location:** `tests/.../Restricted/B3RestrictedLifecycleTests.cs:744-780`.
- **Trigger:** kill the unrelated B2 lifeline process while HELD.
- **Consequence:** the test title suggests a recovery-loss safety property; it proves only that the
  two topologies are unintegrated.
- **Smallest remediation:** rename/re-document, and add the integrated-topology case when §F is done.
- **Blocks B3 acceptance?** No. **Blocks later integration?** Contributes to F-01.

### F-08 — Coordinator exec-confirmation oracle clears `FD_CLOEXEC` and passes an extra fd to the target — **OBSERVATION**

- **Location:** `tests/Fixtures/b3-release-coordinator.c:254-301` (child clears `FD_CLOEXEC` on the
  `confirm` pipe write end before `execve`).
- **Trigger:** successful `execve("/tool")`.
- **Expected vs actual:** expected the write end to stay `CLOEXEC` so the parent observes EOF at once
  (positive exec confirmation) and the target inherits only fds 0-2; actual the target inherits the
  spare descriptor and the parent's read blocks until the target exits.
- **Consequence:** test-fixture fidelity only. It does not produce a false `Succeeded` (a non-exec
  child death yields a signalled root -> `TargetFailed`), and the fixed target exits promptly so the
  sequence still holds, but the oracle is weaker than intended and the target inherits an extra fd.
- **Smallest remediation:** keep `FD_CLOEXEC` set on the confirmation write end.
- **Blocks B3 acceptance?** No (test-only fixture).

No BLOCKING findings were identified within the declared B3 scope.

---

## I. Failure/attack-scenario coverage map

Real processes / real kernel resources are used by: `B3RestrictedLifecycleTests` (all HELD/release/
stop/abort/deadline/recovery cases), the B2 real-process suites, and the native domain/owner-death
suites — all Linux/WSL2, skipped on this host. Controlled barriers are used by `ReleaseArbiter`
(`TestBarrier`: `after-intent`, `before-consume`, `after-consume`, `after-write`, `after-committed`,
`after-ambiguous-intent`, `after-ambiguous-retention`), the `release-after-intent` HELD barrier and
`Barrier`/`ManualResetEventSlim` in tests. Mocks/fakes are used by `ReleaseAdmissionTests`
(`FakeClock`, `FakeTransport`, `FakeJournalStorage`), `RestrictedControlB3Tests`/B1Tests (fake
`IRestrictedOperationOwner`). Cases covered: stop before release intent (`B3StopBeforeRelease`,
`StopWinsBeforeIntent...`); stop during arbitration (`B3BarrierStopWins...`); release-before-stop
(`B3ReleaseWinsThenStop...`); two concurrent releases (`ReleaseArbiterTests`,
`ConcurrentLoserIsRefused...`); concurrent matching/mismatching (`ConcurrentMatchingAndMismatching...`);
controller death while HELD (`B3ControllerChannelLoss...`); owner death while HELD
(`B3OwnerDeath...`); recovery death while HELD (`B3RecoveryDeath...`, see F-07); lost release/stop
ack (`LostReleaseAckIsUnknown...`, B2 lost-ack suite); deadline expiry (`B3HeldOverallDeadline...`);
malformed frames (`CoordinatorStatusReaderTests`, `OwnerHeldWireCodecTests`, native `fail()`);
partial journal persistence (`Wp05D69C1AmbiguousIntentTests`, journal tests); invalid generation /
wrong prepared object (`ReleaseAdmissionTests`, `RestrictedControlB3Tests`, owner identity check);
descriptor closure during IPC (`RecoveryConnectionDisposalTests`); aborted preparation/re-entry
(barrier suites); failed observation after committed release (`DeliveredWriteIsNeverConverted...`);
failed cleanup with surviving owner resources (`B3OwnerDeath...` leaves the recovery lease alive,
cleanup `Unknown`). No deferred B4/B5/B6/B7 feature is reclassified as a B3 code defect.

---

## J. Final verdict and answers

**Verdict: SUPPORTED WITH QUALIFICATIONS.**

1. **Is M-01 closed?** **Yes.** `GuardedDescriptor` serializes disposed-check/use/close; `Send`/
   `Inspect`/`ObserveAsync` all route through it; close-exactly-once and wait-for-in-flight-use are
   independently reproduced. No counterexample found.
2. **Is B3's owner preparation genuine?** **Yes**, within the fixed experimental fixture: owner-origin
   identities, real staged closure, durable preparation before Mint, genuine HELD, no target
   initialization before release, capability/release writer owner-local.
3. **Are R1-F03 and C1–C4 correctly preserved?** **Yes**, at both the public operation and the
   owner-side arbiter, with the evidence-reviewed native ordering cases. F-03 is a diagnostic nit.
4. **Is the single physical RELEASE guarantee supported?** **Yes**: one-shot `HeldExecution`, single
   capability consume, no second intent, retained-decision `Duplicate`, verified by tests.
5. **Is the positive native fixture evidence credible?** **Credible but not independently
   reproduced.** Source, artifact hashes and the coordinator's strict validation support it; this
   review could not run native tests (no delegated cgroup; prohibited to provision).
6. **Is B3 eligible for owner acceptance within a limited scope?** **Yes, with the enumerated
   qualifications** (§G.2, §H), and only if F-01 is recorded as deferred integration.
7. **Is the B2/B3 topology conflict blocking, deferred, or evidence-insufficient?**
   **Acceptable bounded qualification with explicit deferred integration** (deferred; it blocks later
   integration and production qualification, not limited-scope B3 acceptance).
8. **Smallest required next engineering action.** Establish integrated controller/recovery/owner
   supervision — a controller-parented transaction owner with recovery retaining lifeline-only duties
   (or B6 pinned controller->owner authentication) — and re-run the recovery-loss case against that
   integrated topology, **without** transferring a release-capable bearer through recovery.

Owner acceptance is **not** issued here, and B4 is **not** started.
