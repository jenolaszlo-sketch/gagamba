# WP05 D4 Independent Crash-Witness Review - 2026-10-20

Status: independent reviewer (opencode/GLM) adversarial review of the DeepSeek
D4 tranche `implementation/wp05-d4-release-owner-witness`. This document is a
review artifact only. It does not repair the fixture, does not authorize
production changes, and does not accept any release gate. Bounds follow
`docs/remediation-evidence-standard.md`; the reviewed slate is the producer
report `docs/evidence/linux-m1/wp05-d4-release-owner-crash-witness-evidence-2026-10-20.md`
plus its artifact set, read against the required-work baseline blob
`1768ec2a98e69349ca46137f4194fb0d49233772`
(`docs/reviews/gagamba-linux-m1-wp05-d4-d6-required-qualification-work.md`).

Disposition: `WP05 D4 FRESH-PROCESS RELEASE FIXTURES AND NO-RESEND DIAGNOSTICS
REVIEWED AND INDEPENDENTLY REPRODUCED - EVIDENCE SUPPORTED WITH QUALIFICATIONS;
CORRECTIONS AND D5 ITEMS REQUIRED - C1-C4 UNRESOLVED; RELEASE NOT QUALIFIED`.

## A. Repository and source identity (verified)

- Canonical repo `C:\Users\Laszlos\source\repos\Gagamba`;
  brand-new review branch/worktree
  `C:\Users\Laszlos\source\repos\gagamba-review-d4`
  at `review/wp05-d4-crash-witness-qualification` ===
  `8254cf89f091eb5baf305d125e00cd2bf61a1ca1` (exact D4 tip).
- D4 commit chain verified by `git merge-base --is-ancestor`, each TRUE:
  `7dd5ffa…` (D1-D3 preservation) -> `26c73db…` (D4.0 design) ->
  `acebcc8…` (D4.1 owner fixture) -> `e36a9ea…` (D4.2 supervisor/probes) ->
  `de1ee93…` (D4.2b retry refusal; evidence source HEAD) -> `cebee05…`
  (evidence) -> `8254cf8…` (manifest-verification binding).
- Production immutability re-verified on this branch:
  `git diff 338a98a… de1ee93… -- src native artifacts` is EMPTY (R12 tip
  `338a98af269e6b5839f3ccc2e0881f016153b01e` is the parent of the first
  D1–D3 commit) and `git diff de1ee93… 8254cf8… -- tests` is EMPTY (the
  `tests` tree is identical at the evidence source HEAD and the manifest
  binding tip).
  Frozen R12 tree `6854675d6b37a49d5302d6104103bdbfbc268040`;
  baseline `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Four load-bearing source files byte-verified inside the canonical repository
  at the evidence source HEAD (SHA-256 of committed blob bytes, all matching
  producer `source-head.txt`):
  - `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4OwnerHost.cs`
    `09b2fd82c1d93ef8cf8476ca81b6e15f03fb5025996bd4f3e6199378ec0234e0`
  - `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs`
    `70b8b602bc75f399d60d6b4657200312f51052dde7c4d58fa336dd9fea56677f`
  - `tests/Fixtures/Wp05D4Owner/Wp05D4Owner.csproj`
    `70155896bb8c2db4d34ecebe92a15eb4858a9682c1e82cfbbf96376ff29c347c`
  - `tests/Fixtures/Wp05D4Owner/Program.cs`
    `b2223942f67b94b5fec8054d6ed1a66066ceb9d9e63b27cba8c5162ddf9f9dcf`
  - `docs/plans/gagamba-linux-m1-wp05-d4-fixture-design.md`
    `99e4f60eab692f2e872b23ce17208167f65675257df9b23288670851cbd5dea3`.
- Harness/fixtures: `Wp05D4Owner` is a thin 3-line owner entrypoint
  (`return Wp05D4OwnerHost.Run(args);`) over the test assembly
  (`IsPackable=false`). It composes real R12 machinery: real
  `OwnershipJournal`/`JournalFileStorage`, real `PreparedTransaction` mint via
  `PreparationPublisher.Publish`, real `HeldExecution` via
  `BubblewrapAdapter.PrepareHeldAsync`, real cgroup child domain via
  `ScopedAllocationContext` + `CgroupInterop.TryCreateDomain` with WP04
  limits, real control pipe. No target is forked.

## B. Full source audit (read files)

Read in full (no skimmed ranges): `Wp05D4OwnerHost.cs` (974 lines),
`Wp05D4SupervisorTests.cs` (781 lines), `Wp05D4Owner/Program.cs` and csproj,
the D4.0 design doc, the producer evidence report, and the following artifact
files: `crash-matrix.txt`, `witness-semantics.txt`, `coverage-map.txt`,
`source-head.txt`, `commands.txt`, `environment.txt`, `exclusions.txt`,
`cleanup.txt`, `processes.txt`, `review-binding.txt`,
`compact/case-index.txt`, `d4-supervisor-run1.log`, `d4-supervisor-run2.log`,
`regressions.log`. R12 anchor code re-verified live (`ReleaseArbiter.cs` fade
positions and decision kinds; `HelperInstallation.PinnedSha256`;
`OwnershipRecord.cs` version constants).

### B.1 Verified strengths

1. **Real production path, not a mock owner.** The owner session installs the
   real journal fence (`Journal.TryAcquireWriter`), real allocation intent /
   registered / prepared snapshot writes, real cgroup child domain, real
   WP04 limits, real staged closure via `SafeCopyIn.Prepare`, real manifest
   and preparation identity, real bwrap `PreparedTransaction`, real
   `HeldExecution` and the real production pre-exec coordinator. The release
   gate is the unchanged R12 `ReleaseArbiter.RequestRelease` — its retained
   decision semantics (`Kind = Duplicate` for later calls), the concurrent
   refusal path (`TryPlanReleaseIntent` against a durable Intent snapshot),
   `before-consume` / `after-consume` / `after-write` / `after-committed`
   barrier names, and the `WouldBlock`/`BrokenPipe`/`Partial` exclusion for
   `LiveExecutionExcluded` were re-verified in the R12 source.
2. **Barrier names and boundaries match the R12 code exactly.** TestBarrier
   hooks used by the fixture (`after-intent`, `after-consume`, `after-write`,
   `after-committed`, `after-stop`) are the actual hooks in
   `ReleaseArbiter.RequestRelease`/`RequestStop`; no boundary is fabricated
   by test code.
3. **Witness lanes are genuinely separated.** Lane B (real) = owner uses
   `D4WitnessingTransport` wrapping the real `HeldExecution.TryWriteRelease`
   (produces `PreWriteWitnessAcknowledged`, `WriteReturned`,
   `CoordinatorExitObserved` after real `WrapperReaper.TryReap`). Lane A
   (test-only) = replacements use `D4RecordingTransport`, which records
   `TestTransportAttempt` and never touches FD4. Side effects are attributed
   correctly (the D4-8 Lane-A release legitimately writes `Committed` in the
   journal but is labelled diagnostic — see G.3).
4. **Fresh-process replacement owns nothing inherited.** The replacement
   process receives only `--journal`, `--source` (for the explicit
   diagnostic), `--mode`, and witness/result control paths — no capability,
   no `HeldExecution`, no FD4 writer, no in-memory arbiter. Writer acquisition
   is a fresh `TryAcquireWriter` against the real fence; recovery is fresh
   `ReadLatest`/`TryRecoverCommit`/`ReleaseRecoveryClassifier`. The diagnostic
   capability in `replace-release`/`retry-release` is newly minted by test
   code from journal-recorded preparation identity
   (`release_source=new-test-only-capability` — explicit).
5. **PID-reuse safety is anchored.** Kill target is a `.NET` direct child
   (`Process.Kill(entireProcessTree:false)` — targeted `kill(pid, SIGKILL)`),
   mapped to the kernel-observed signal death, with `/proc/<pid>/stat`
   start-time identity (`pid:starttime`) recorded before the kill and
   `OwnerReady` in `owner.ready` asserted against the live descendant.
    Superset control probe `wait4` reports `signaled=1 termsig=9` on this
    kernel; see D.
6. **No second writes.** Case evidence (mine and the producer's) shows
   `TestTransportAttempt`/`PreWriteWitnessAcknowledged` counts consistent
   with at-most-once observed attempts; D4-4 (crash after durable
   Committed) preserves Committed with no second write; D4-2b refuses a
   retry over durable Intent with 0 transport attempts (real R12 refusal
   semantics re-read at `ReleaseArbiter.cs:92`); D4-8's Lane-A diagnostic
   release is separately countable and never claims FD4.
7. **The supervisor really does not own the journal fence while the owner
   lives.** D4-12 asserts writer refusal with the owner alive (real
   `TryAcquireWriter` refusal from the live fence) and fence release only
   after death (`AssertJournalFenceReleased`). D4-14 shows second contender
   refusal (`contend` mode), overlapping in time with the first contender
   holding the fence (barrier-serialized). Owner FD inventory (control FD,
   journal lock FD) is verified from `/proc`.

### B.2 Verified limitations (disclosed by producer, accepted as bounds)

- Storage gates are test-only `IJournalStorage` seam pauses, not kernel/ptrace
  breakpoints (design doc §4 admits this). Boundaries D4-C01/C02 and the
  stop-gate variants are component-gates.
- Coordinator acceptance after a kill is genuinely unknown: fixture asserts
  `CoordinatorExitObserved == 0` when the owner never reaps (D4-3, D4-4) —
  acceptance is neither inferred from `WriteReturned=Delivered` nor from
  journal state. `witness-semantics.txt` states this explicitly.
- CE-04's fresh-process second-owner variant is NOT implemented: the
  required-work doc's item 3 ("spawning a second owner process against the
  same journal") is satisfied for CE-01/CE-02/CE-03 through fresh replacement
  processes, but for CE-04 the implemented variant is the in-owner
  three-caller interleaving (D4-9) plus two mere `contend` writer holders
  (D4-14), not two independent owner `RequestRelease` races. D4-C08 covers
  writer-fence contention only. This is a real gap; correction CR-3.
- The fixture's owner mirror snapshot uses `OwnershipSnapshotCodec.LegacyVersion`
  (2) rather than `OwnershipSnapshotCodec.CurrentVersion` (3) — see CR-11.

## C. Static-correctness per-case review

Cross-checked producer's `coverage-map.txt` against code; additionally my
rerun witness artifacts (see `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/case-index-run1.txt`)
reproduce real-event counts:

| Case | Source asserts (verified in code) | My rerun observed events |
|---|---|---|
| D4-1 (C-06 no-resend baseline) | `PreWriteWitnessAcknowledged=1`, `WriteReturned=1`, `CoordinatorExitObserved=1`, `TestTransportAttempt=0`; journal `Committed`; `coordinator_exit=0`; replacement recover-only | matches; real FD4 + coordinator exit 0 |
| D4-2 (C-03 post-intent kill) | kill after `after-intent`; journal `Intent`; replacement `MayHaveExecuted`; no transport attempts | matches |
| D4-2b (CE-01 retry refusal) | fresh replacement with newly minted test-only capability re-tries release over durable Intent → R12 refusal (NotCommitted, 0 attempts; witness unchanged) | matches |
| D4-3 (C-05 post-FD4 kill) | kill after `after-write`; one real write; `CoordinatorExitObserved=0`; Intent preserved; acceptance unknown | matches |
| D4-4 (C-06 post-committed kill) | journal `Committed`; replacement recovers Committed; no second write | matches |
| D4-5 (C-04 post-consume pre-write kill) | capability consumed in dead owner; no FD4 witness | matches |
| D4-6 (CE-02 ambiguous intent) | real storage seal-unlink fault; owner decision `Refused(NotCommitted)`/0 attempts; replacement `TryRecoverCommit` succeeds (`RecoverySealConfirmed=recovered` in my run) `MayHaveExecuted`, no resend | matches |
| D4-7 (C-07a durable stop) | accept runtime stop; `StopRecord` durable; replacement sees `stop=true`, no release | matches |
| D4-8 (CE-03 failed durable stop) | runtime accepted, durable write fails; replacement `replace-release` diagnostic Lane-A with newly minted authority (1 `TestTransportAttempt`, 0 `PreWriteWitnessAcknowledged`) | matches |
| D4-9 (CE-04 in-owner) | winner `Released`/fd `transport_attempts=1`, concurrent `Refused`, later `Duplicate`; coordinator exit 0 | matches |
| D4-10 (C-01 pre-intent gate) | `create:gen.seal` storage gate pre-intent; kill; journal `Prepared` (no Release); replacement no release | matches |
| D4-11 (C-02 during intent gate) | `write:gen.tmp` gate; kill; `ReadLatest` fails seal; recovery `RecoverySealConfirmed=recovered`, NotCommitted final | matches (my run recovered with explicit seal handling) |
| D4-12 (negative control) | replacement while owner live → exit code 3 = `RecoveryWriterRefused`; fence freed after kill | matches |
| D4-13 (stop-persistence gate) | stop candidate `write:gen.tmp` gate; recovery gets explicit "commit seal" recovery, no/not fabricated stop | matches |
| D4-14 (C-08 contenders) | first contender holds (barrier) before second; second refuses; no second journal writer; no release | matches |

The crash matrix `D4-C01..C08 + CE-01..CE-04` semantics stated by the
producer's report are consistent with the fixture code AND reproduced at
runtime (2 runs in my set). No case observed a second `PreWriteWitness-`
event, second `TestTransportAttempt`, or second coordinator `exit 0`.

## D. SIGKILL oracle review (the load-bearing question)

The fixture kills by `_owner.Kill(entireProcessTree:false)` and asserts
`ExitCode == 137`. Reviewer standards previously required kernel-level signal
evidence instead of "bare exit 137". Findings:

1. **Runtime semantics (source-verified).** .NET 10 BCL `Process.Kill()` on
   Unix calls `SignalCore(PosixSignal.SIGKILL)` → `kill(processId, SIGKILL)`
   and `ProcessExitStatus.ExitCode` is documented and implemented as
   `128 + signal` for signal deaths, with the true signal recorded
   separately in `ProcessExitStatus.Signal` (public surface for the
    launched-child `Process` object exposes `ExitCode` only). Fetched
   upstream source (`Process.Unix.cs`, `ProcessExitStatus.cs`).
2. **Kernel truth (reviewer control).** A C probe on the supported WSL2
   profile (fork + `kill(2, SIGKILL)` + `wait4`) reports
   `signaled=1 termsig=9` (see `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/controls/wait4probe.txt`).
3. **BCL truth (reviewer control).** A .NET 10.0.112 probe kills a child that
   has trapped SIGTERM (`trap "" TERM`): the child still dies and
   `ExitCode == 137`—the `.Kill()` call therefore delivered SIGKILL, not
   SIGTERM or an abort (`docs/.../controls/dnet-kill-control.txt`).
   Contrast control: a directly SIGTERM-killed child exits 143
   (`term-control.txt`).
4. **Ambiguity boundary (reviewer control).** A child that voluntarily
   exits `137` also reports `ExitCode == 137`
   (`dnet-self-exit-control.txt`). Bare `137` alone is not a complete
   disambiguation; the fixture's boundedness comes from three further
   properties: (a) the owner is a direct child started by the supervisor
   itself, (b) the audited fixture source never self-returns 137 (the
   fixture's own bounded exit paths are 0, 1 and 3; the fixture body has no
   `Environment.Exit(137)` and does not forward/return 137), and (c) the
   journal flock fence-release assertion is real kernel corroboration of
   owner death (POSIX flock can only be reacquired once the owner's fds
   died).
5. **Assessment.** The kill oracle is accepted for this fixture as
   **measured** (kernel-anchored .NET semantics + direct-child targeting +
   audited bounded self-exit codes + fence-release corroboration), matching
   the producer's own described mechanics. The residual gap (public-facing
   `Process.ExitCode` cannot expose `WIFSIGNALED` classification) is
   recorded as CR-1 for a D5+ correction with `wait4`/`waitid`-status
   validation or pidfd teardown; it does not invalidate the measured
   results.

## E. Technique cross-checks

- **xunit parallelism.** All 15 D4 facts share one class (one xunit
  collection) but xunit.v3 still parallelizes within a collection in
  this profile — in my initial permission-failure probe all 15 facts
  printed start stamps within ~3 ms of each other
  (`[00:00:00.91–.93]`). This explains the producer's "Duration: 7 s"
  summary inside a longer shell wall time and is NOT counter-evidence
  (the per-case cgroup/journal/witness trees are independent; the
  fixture never uses `waitid(-1)`). It is, however, a Q3-harness standing
  consideration:
  the `Category=ProcessGlobalReaping` discipline should document that the
  same suite is designed to tolerate collection-level parallel fan-out,
  or the runner must serialize it (booking in CR-8).
- **Process-global reaping safety.** The supervisor only reaps its own
  direct child (`WaitForExitAsync`) and only kills its own `Process`
  handle; no `waitpid(-1)`, no `pdeathsig` reliance, no shared
  `SIGPIPE` masking. Kill targets are direct children (audited).
- **CE-01 asymmetric-wedge cross-process shape.** D4-2b explicitly
  refuses a persistent retry over an existing durable Intent with a NEW
  (freshly minted) test-only capability and a Lane-A transport — the
  coarse CE-01/C4 wedge observable at the plain decision boundary.
  D4-9 covers the in-owner concurrent shape. The cross-process
  second-owner-with-own-capability RequestRelease race remains
  unimplemented (CR-3).
- **CE-04 three-caller shape.** Winner starts at `after-intent` while the
  concurrent caller rejects via `TryPlanReleaseIntent`-refusal; later
  caller maps to `Duplicate` (retained-release rewrite in
  `ReleaseArbiter.RequestRelease`; verified in R12 source). A real FD4
  write and coordinator exit 0 are observed exactly once.
- **Recovery ambiguity handling.** `ReadLatest`-failed seal cases route
  through `TryRecoverCommit` and classify `MayHaveExecuted`, but with the
  explicit "no attempt" gate (`release_attempted=false`) in every
   counterpart, matching CE-02 semantics (Intent content durable behind
   seal). My rerun journal-slot and witness counts (`myruns/`,
   `case-index-run1.txt`) confirm runtime behavior identical to the
   producer's.

## F. Regressions and incidental coverage

- My rerun of the producer's focused regression filter executed
  **66 passed / 0 failed / 0 skipped** (wall 10.9 s on external clock,
  xunit reported 6 s), on the same R12-identical Linux checkout the
  producer used. Producer's `regressions.log` reports the same 66
  counts. The three previously `Unavailable`
  `Wp05ReleasePathIndependentQualificationTests` in my rerun ran against
  the native/WP04 artifacts whose hashes are frozen and unchanged; they
  ran incidentally re-validated but are NOT considered qualification
  here, consistent with the required-work doc's "converted by generating
  their required artifacts" — the artifacts exist in the producer's tree
  (verified by frozen SHA identity), but their
  *qualification row status* still belongs to D5.
- No full assembly, no D5 native negative suite, no target workload was
  run. G3 labels were not touched.

## G. Correctness risks by severity

### G.1 Blocker-level risks: none found.

No production change, no unbounded fallback, no unauthorized transport
grant, no witness-rewrite semantics, and no spoofed supervisor ownership.
The fixture imports real R12 arcs (intent → transport observation →
committed), and its negative controls genuinely bind the fences.

### G.2 Real qualification risks (must appear in the corrections list — see companion document)

1. **CR-1 (SIGKILL oracle strength):** public `ExitCode==137` is bounded
   but cannot classify `WIFSIGNALED` directly; D5+ should bind the kill
   semantics to `wait4/waitid` status or pidfd teardown or accept-
   and-pin the kernel/BCL corroboration record into a durable
   `docs/evidence/` artifact rather than a reviewer's disposable replay
   (this review provides the current control artifacts).
2. **CR-2 (doc-comment invariance):** `Wp05D4SupervisorTests.cs` says
   "supervisor creates only test-owned processes and files; it never
   acquires release authority or an owner journal lease", but
   `AssertJournalFenceReleased` does transiently acquire the writer
   fence (line 662–669) and `D4-9` and `D4-1` read coordinator exit via
   `RecordCoordinatorExit` inside the owner. The supervisor never holds
   release authority; the comment is nonetheless too narrow. Reword.
3. **CR-3 (CE-04 fresh-process second owner):** required-work doc D4 item
   3 explicitly asks for two OWNER processes against the same journal.
   Implemented D4-9 alone is in-owner; D4-14 contends over the writer
   fence only. An unimplemented sub-case, not a false claim; record as
   D5 residual.
4. **CR-4 (trivially-true prerequisite stub):** the owner passes
   `Environment.ProcessId > 0` as the "owner process liveness"
   prerequisite; D4-D6 spec accepts supervisor-owned fixture composition
   but the surrogate should be disclosed in the design doc (it is not
   reconciled in the D4.0 store).
5. **CR-5 (storage-gate identity):** `D4JournalStorage.Gate` waits for a
   `go.txt` file but does not verify the writing process; a directory
   external to the fixture could release the gate (no writer UID check).
   Bounds: D4_C-base rows are still "component-gate" — no prohibited
   promotion is claimed; acceptable, but a future production-context
   gate design must bind writer identity.
6. **CR-6 (stop deadline verification):** D4-7 checks
   `stop_accepted=True`/`stop_durable=True` but does not validate the
   recorded `StopDeadlineNanoseconds` policy against the allowed stop
    window (`OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds`);
   it only mirrors the journal's value. Privacy-observation only; not a
   fidelity rejection.
7. **CR-7 (witness file races):** owner, replacement, and supervisor
   append to the same witness file without an inter-process lock.
   `D4Witness.Append` uses `FileMode.Append` + `Flush(flushToDisk:true)`
   and each line stays under a byte-scoped flush boundary; merged lines
   remain plausible-looking and small. Racy but self-contained; drop a
   "single writer at a time from the test point-of-view" discipline or
   serialize appends with a per-case lock in the D5 hardening.
8. **CR-8 (xunit within-class parallelism):** see E; the suite is safe
   in the current fixture profile, but the per-category harness
   discipline (`.Q3` Phase-2 doc) should record the expected
   collection-parallel tolerance or force `[Collection]` serialization
   to avoid silent robustness drift in later slices.
9. **CR-9 (owner start-time check is pid-only):**
   `Assert.True(c.OwnerStart.Contains($"{c.Owner.Id}:"))` ignores the
   starttime digits after the colon. The kernel-anchored reuse safety
   currently rests on the direct-child handle plus kernel-observed
   death, which is real, but the assertion itself does not compare the
   starttime. Strengthen by comparing the full `pid:starttime` value
   with the ready-time value (straightforward fix, recorded for the
   D5 handoff).
10. **CR-10 (no `UtimeStable`/`waitpid` code eval on SIGKILL vs
    dotnet's combo):** the consumer expectations preserve crash state
    while `WaitForExitAsync` reaps the child — .NET's targeted reaping
    is the single writer of exit state. `WaitForExitAsync` blocks the
    supervisor thread only after `Kill`, reducing PID-reuse exposure.
    Acceptable as-is; no change required.
11. **CR-11 (LegacyVersion mirror):** `OwnerSession.MirrorRegistered`
    writes the owner's intermediate `registered` snapshot with
    `OwnershipSnapshotCodec.LegacyVersion` (2) though
    `OwnershipSnapshotCodec.CurrentVersion` (3) exists and the frozen
    `review-binding.txt` says "OwnershipSnapshot v3". Since the fixture
    does not aim to mirror a production field exactly and the journal
    codec tolerates both (2 and 3 validated), this is a fidelity nit,
    not a defect — align or document as deliberate.

### G.3 Not-a-defect notes

- D4-8's replacement Lane-A release yields a durable `Committed` journal
  record without any FD4 write. This is intentional and disclosed:
  "diagnostic only" — the real owner was killed and the journal
  transition is a genuine R12 arbiter change with a test-only
  transport result. It does NOT claim the real coordinator accepted.
- The storage seam's `AmbiguousSealFailureInjected` events act only as
  test-source annotations, never as delivery claims.
- The 133-entry artifact manifest verifies cleanly against committed
  blob bytes (`sha256sum -c`: 133 OK / 0 failed, including the report's
  own SHA-256 `53c76c575fdf74c9543614bb629a39257b9255c81b2d963102aeb3157d79fa58`).

## H. Reviewer rerun evidence (summary)

Full set in `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/`:

- `final-binding.txt`, `checkout-identity.txt` and `scripts-sha.txt`:
  branch/HEAD binding, bound source HEAD, and reviewer script SHA identities.
- `drr-run1-cg.summary`, `drr-run2-cg.summary`: my reruns
  (15/0/0 ×2, wall 11.5 s / 11.3 s; xunit-reported duration 7 s each
  as consumed by VSTest);
  `drr-regressions-cg.summary`: 66/0/0.
- `case-index-run1.txt` and `myruns/gagamba-d4-D4-*.txt`: per-case
  counts and artifacts from my run1.
- `controls/`: `wait4probe.txt` (kernel termsig 9), `term-control.txt`
  (shell-born SIGTERM death exits 143), `dnet-kill-control.txt`
  (SIGTERM-trapping child dies to `Process.Kill` with exit 137),
  `dnet-self-exit-control.txt` (self-exit 137 also returns 137 — the
  documented `Process.ExitCode` ambiguity),
  `dnet-build.log`.
- `manifest-verify.txt`: `sha256sum -c` over the extracted committed
  artifact tree; 133 entries checked, 0 mismatches (report's own SHA
  `53c76c…` revalidated).
- `leftover-procs-after.txt`: no residual fixture child processes after
  the runs (only unrelated reviewer tooling).

## I. Corrections and D5 handoff

Detailed list in the companion document
`docs/reviews/gagamba-linux-m1-wp05-d4-required-corrections-and-d5-handoff.md`
(CR-1..CR-11 plus D5 rows). The required-work spec's mandated D5 items
are NOT swallowed by D4: D5 native FD5 negative statuses and formal
release-path artifact conversion remain mandatory and are handed off
unchanged.

## J. Final statement

Boundaries, fences, witness lanes, kill mechanics, and no-resend
behaviors match the producer's artifacts and the unchanged R12 sources.
The D4 slice is an HONEST, MEASURED, REAL-PROCESS diagnostic that does
not over-claim. It cannot qualify the release on its own (C1-C4 remain
unresolved; CE-04 fresh-process and native negatives remain open) —
Option B applies:

`WP05 D4 FRESH-PROCESS RELEASE FIXTURES AND NO-RESEND DIAGNOSTICS
REVIEWED AND INDEPENDENTLY REPRODUCED - EVIDENCE SUPPORTED WITH
QUALIFICATIONS; CORRECTIONS AND D5 ITEMS REQUIRED - C1-C4 UNRESOLVED;
RELEASE NOT QUALIFIED`
