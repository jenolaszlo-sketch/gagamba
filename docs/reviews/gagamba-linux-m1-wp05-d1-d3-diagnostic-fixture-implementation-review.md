# WP05 D1-D3 Independent Fixture and Evidence Review - 2026-10-20

Status: independent post-implementation review of the test-only release
fixtures and counterexample-probe slice. This is a reviewer document. It
reviews, binds and reproduces; it does not decide C1-C4, does not modify
production code, does not implement further fixtures, and does not accept any
WP05 release gate.

Disposition: `WP05 D1-D3 DIAGNOSTIC FIXTURES REVIEWED AND INDEPENDENTLY
REPRODUCED - C1-C4 UNRESOLVED; C7/C9 PARTIAL; RELEASE NOT QUALIFIED`.

## 1. Scope and bindings

Reviewed slice: branch `implementation/wp05-release-qualification-fixtures`,
test-only commits on top of the preservation base.

- Base (planning/preservation): `24e7498f1940c8e985d229a48b208fe4a0065b26`.
- D1: `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c` — fresh-process lease fixture.
- D2: `dc4c372b180abef1b27038d94f49af90226fa548` — 13 contract probes.
- D3: `338a98af269e6b5839f3ccc2e0881f016153b01e` — evidence record, C8 plan
  note, handoff/work-queue updates. D3 tree:
  `f05c48472cdf08b9c51acb2dc97cfed5cf7050f2`.
- Product: unchanged R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Review binding (preservation branch, not present on this slice's branch):
  `e4b0a798e944ddfb144901c63b0c444e7980533b`; blobs
  `f1a062e1e12895426dcfb8b7b0b13a2e6d933918` (adversarial review),
  `73eb09af27c5dbc896739ebf48489862ec13e0a9` (required changes C1-C9),
  `0bbafa6b9725cce8a20858e1d64a8deec6134f15` (counterexample register). These
  bindings are reproduced from `review-binding.txt` and verified first-hand by
  the reviewer against the preservation branch.
- Frozen harness disposition `WP05 HARNESS QUALIFICATION (bounded)` at Q3
  `36a124d7dff98ecfaf956340767fe1768ad5b17f` is unchanged and not reopened.

## 2. Method

The following verification was performed by this reviewer first-hand:

1. Git ancestry and deltas: `24e7498..D1..D2..D3` verified with explicit SHAs;
   the diff `24e7498..D3` touches no `src/`, `native/` or `eng/` path. Added
   files: `tests/Fixtures/Wp05LeaseFixture/Program.cs` (+3),
   `tests/Fixtures/Wp05LeaseFixture/Wp05LeaseFixture.csproj` (+16),
   `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`
   (+415), `tests/.../Recovery/Wp05LeaseFixtureHost.cs` (+55),
   `tests/.../Recovery/Wp05LeaseProcessProbes.cs` (+172), plus evidence/docs.
   Producing commits D1/D2 were `test(wp05)`-typed; D3 is docs/evidence.
2. Byte-level binding: SHA-256 of the five added test files recorded in
   `source-head.txt` (producer evidence) were recomputed by the reviewer:
   `cd5cd00c4c4ef213239047a85ed8f26129e45bb5cad0ddd2c3fe646a407da2b4`,
   `ac6e3462ba1ebdf419e9681ff30006c6b9e9db357682f8c4352f6b26a09b7f86`,
   `15029e4d669f370154762fc1ca7ee7fd060459946bc69c070fac99455f7e6276`,
   `70155896bb8c2db4d34ecebe92a15eb4858a9682c1e82cfbbf96376ff29c347c`,
   `a27e4c23a933f446c4e2b0bc236f79e5081faa1203f08c7cd4e30d58a8b6a07e` — all
   match. The Linux checkout later used for the independent rerun ran these
   exact bytes.
3. Artifact integrity: all 23 entries of
   `docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-artifacts/SHA256SUMS`
   were recomputed against committed content: 23 verified, 0 mismatched,
   0 missing, 0 extra unlisted files in the artifact directory.
4. Probe-fidelity review: every probe was read line-by-line against the R12
   production sources (`ReleaseArbiter`, `OwnershipJournal`,
   `OwnershipTransitions`, `Reconciler`, `ReleaseObservationBuffer`,
   `CoordinatorStatusReader`, `HeldExecution`/`ReleasePipeWrite` seams,
   `exec-coordinator.c`, `wp05-qualification-coordinator.c`) and the prior
   review's required-changes items C1-C9.
5. Independent execution (partial Part L): the reviewer reran the four probe
   groups plus the fixture build on the supported Linux environment. Details
   in section 7; artifacts in
   `docs/reviews/gagamba-linux-m1-wp05-d1d3-review-artifacts/`.

## 3. D1: fresh-process lease fixture

`tests/Fixtures/Wp05LeaseFixture` is a non-packable console project that
references the test assembly only; no production or owner assembly consumes
it. `Wp05LeaseFixtureHost` opens `OwnershipJournal` with real `JournalFileStorage`
on a fresh temp directory. Reviewer findings:

- The owner process calls `TryAcquireWriter` and parks; ready/lock-file
  contents (`pid=`, `journal=`, `lock=<journal>/ownership.lock`,
  `sequence=`) are written only after a successful acquisition.
- The xUnit probe `FreshProcessLeaseIsNotInheritedAndReplacementReacquires`
  verifies through `/proc/<pid>/fd` that the fixture app process owns the lock
  FD, that the supervisor (test) process does not, then SIGKILLs the owner,
  waits for exit, and starts a replacement whose new process writes only after
  a fresh `TryAcquireWriter` with its own baseline. PIDs differ by assertion.
  Replacement success requires that the OS released the lock at SIGKILL, i.e.
  no lease inheritance by pathology.
- Cleanup deletes the temp directory and kills leftover processes; exit codes
  cover failure modes (2 missing args, 3 acquisition failure, 4/5 missing
  output path, 6 finished state).
- Reported and reproduced result: 1 passed / 0 failed / 0 skipped (Linux);
  Windows shows `Skipped: linux only` (0 passed, 1 skipped).
- Boundary (correctly claimed by the producer): this is lease/process
  separation only. It is not the G3-C release-owner SIGKILL-at-release-barrier
  supervisor, not a transport witness or coordinator receipt ledger, and does
  not exercise release decisions across the crash.

## 4. D2: contract probes

All 13 probes use real R12 production classes (`ReleaseArbiter`,
`OwnershipJournal`, `ReleaseObservationBuffer`, `CoordinatorStatusReader`,
`Reconciler`) with the existing test-only seams (fake journal storage, mutable
clock, fake domain backend, in-memory transport, `TestBarrier`). Reviewer
verdicts per probe (C-item = prior-review required change; CE = counterexample
register):

| Probe | C-item | Reviewer verdict |
|---|---|---|
| StopWithUnavailableClockIsLiveAcceptedWithoutDurableStop | C5 | Faithful: clock-unavailable stop accepted, no durable stop, release refused, capability live, no journal Stop. Matches R12 `ReleaseArbiter.RequestStop` clock-unavailable branch. |
| StopDeadlineOverflowIsLiveAcceptedWithoutDurableStop | C5 | Faithful: overflow deadline stop, acceptance recorded, deadline 0, release refused. Matches the overflow branch. |
| AmbiguousSealFailureLeavesUncertainIntentAndNoStop | C1/C6, CE-02 | Faithful: post-content seal-unlink injection; decision Refused(NotCommitted); no transport attempt; sanity rederived by `ReadLatest` failing with a "seal" error and `TryRecoverCommit` yielding `Intent` classified `MayHaveExecuted`. Injection point selection is self-validating. |
| DefiniteIntentFailureIsDistinctFromAmbiguousPersistence | C1/C6 | Faithful: definite write failure leaves journal readable, no Release, capability live. Distinct from the ambiguous shape. |
| StopFirstLeavesCapabilityLiveOnR12 | C2, CE-03 | Faithful: durable stop does not invalidate the capability; release refused at the journal guard; capability still live. |
| DurableStopBlocksReleaseIntentFromSecondArbiter | C2 | Faithful: a second arbiter over the same capability/journal is refused with no release intent present; durable stop record visible. |
| StopPersistenceFailureAllowsSecondArbiterReleaseOnR12 | C2, CE-03 | Faithful: stop-durable-write failure leaves no Stop record; an independent arbiter instance releases once (transport attempt 1, Committed). Reproduces the R12 multi-instance window at the component level. |
| ConcurrentDuplicateReleaseProducesOnePhysicalWrite | C4, CE-01/CE-04 | Faithful: deterministic via the `after-intent` `TestBarrier`; concurrent loser Refused(NotCommitted), later caller Duplicate, exactly one transport attempt, journal Committed. |
| ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired | C3 | Faithful: real `Reconciler` ignores the persisted stop deadline and consumes the caller's full allowance after recorded expiry (clock advanced by 5s, kill executed, outcome not CleanupConfirmed). Bound: reconciler seam only; no fresh-process or WP08-runner context. |
| StatusBufferDoesNotPersistBeforeDurableCommitted | C9 | Faithful: buffer flush before `MarkDurableCommitted` persists nothing; after the gate Exec/Root persist. |
| StatusBeforeCommittedPersistenceFailureDoesNotCreateObservation | C9 | Faithful: failed exec flush after commit creates no durable observation. |
| StatusReaderRejectsWrongIdentityAndSequence | C9 | Faithful: wrong identity byte and out-of-order sequence both refused. |
| StatusReaderRejectsEofAndMalformedFrames | C9 | Faithful: empty-frame EOF and malformed kind byte refused. |

Fidelity notes the reviewer confirms:

- Assertions are on decision kinds, durable journal state, transport attempt
  counts, time advanced, and kernel-visible lock FD relations, not on
  implementation-object internals. This avoids false-green coupling.
- C1/C6 injection (`FailAt("unlink", 2)` after harness seed) lands at the seal
  acknowledgement boundary; the test's own recovery assertions prove placement
  rather than relying on a magic counter.
- The C2 multi-instance window is reproduced with two arbiter instances in one
  test process (synthetic composition). It demonstrates the contract
  consequence (capability invalidation gap), not a real cross-process owner
  pair. This matches the producer's own exclusions record.
- The C4 loser runs while the winner is parked inside the probe barrier
  (deterministic, 5 s timeouts); there is no scheduler luck.
- C9 negatives are synthetic (in-memory frames/queue); the actual held-execution
  FD5 topology remains out of scope, as the producer recorded.

## 5. D3: evidence record and C8 linearization

`plan-c8.diff` matches the committed plan edit (two blocks: prerequisite 0 and
sequence item 2/3 note). The edit adds only the review-commit reference, the
C1-C4-are-unresolved clause, the C7 fresh-acquisition requirement and the C9
exact-frame/coordinator-only requirement. No G3 gate is tagged accepted by the
edit; the plan otherwise keeps its original gating semantics.

The evidence document and handoff/work-queue updates state the same bound
results and the same unavailable/limited items (three `Unavailable`
independent-qualification facts, C7/C9 partiality, C1-C4 unresolved). No
producer claim overstates the slice.

## 6. Evidence integrity audit

- `SHA256SUMS`: 23/23 entries verified byte-exact (see section 2.3).
- Artifact set is complete relative to `commands.txt`: every recorded command
  has a retained log or an explicitly empty bounded capture
  (`.procs`/residue/process lists are empty as claimed in `cleanup.txt`).
- `source-head.txt` binds branch/HEAD/tree at D2, R12 base/tree, and the
  SHA-256 of the five test files (reviewer-recomputed, match).
- Logs are internally consistent with the summary document: contract probes
  13/13, lease probe 1/1 (7 s vs the producer's 7 s; reviewer rerun 4 s),
  native release protocol 4/4, focused 49/0/3 with the three skips exactly
  `Wp05ReleasePathIndependentQualificationTests`.
- Windows logs: 13/13 contract probes; lease probe 0 passed/1 skipped
  (`linux only`); both consistent with the summary.
- No secrets were found in committed artifacts.

## 7. Independent execution

The reviewer reran the bounded suite on the supported Linux environment
described in the disposition's environment identity (Ubuntu WSL2,
boot `69d053fb-0fd9-4209-aa5b-be128424ed34`, uid/gid 1000 `gaga`, delegation
cgroup `0::/gaga-delegated/host`, capabilities Inh/Prm/Eff/Amb all zero,
.NET 10.0.112, GCC 15.2.0, bubblewrap 0.11.1, frozen seccomp filter SHA-256
`2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`).

Reviewer artifacts: `docs/reviews/gagamba-linux-m1-wp05-d1d3-review-artifacts/`,
bound in this commit: header (start time + checkout state),
`reviewer-environment.txt`, `reviewer-fixture-build.log`,
`reviewer-contract-probes.log`, `reviewer-lease-process.log`,
`reviewer-native-release-protocol.log`, `reviewer-focused-existing.log`,
footer (end time + clean state), plus `reviewer-rerun.sh` (the exact script).

| Group | Producer | Reviewer rerun | Delta |
|---|---|---|---|
| Fixture build | 0 errors/10 warnings | 0 errors/10 warnings | none |
| Wp05ReleaseContractProbes | 13/0/0 | 13/0/0 (104 ms) | none |
| Wp05LeaseProcessProbes | 1/0/0 (7 s) | 1/0/0 (4 s) | timing only |
| Native.ReleaseProtocolTests | 4/0/0 | 4/0/0 (438 ms) | none |
| Five focused classes | 49 passed/0 failed/3 skipped | 49/0/3, same 3 skip names | none |

The Linux checkout was at exactly D2 (`dc4c372…`) both before and after the
rerun (`reviewer-run-header.txt` / `reviewer-run-footer.txt`: clean=0). The
producer's manual SIGKILL trace `lease-trace.txt` was not rerun; the in-repo
xUnit probe (which asserts the same supervisor/owner FD relations) was rerun
successfully. The three `Unavailable` focused cases remain unavailable in this
environment (no generated native fixture / WP04 coordinator artifacts in the
checkout); they remain unpanicked and uncounted.

Reviewer rerun environment deltas vs `environment.txt`: the reviewer's
`/proc/self/status` capture additionally contains the `CapBnd` line
(nonzero, standard bounding set of the distro); the producer's capture
recorded Inh/Prm/Eff/Amb, all zero. No capability divergence in effect.

## 8. Findings

No blocking issue was found. Foundation-level observations for any successor
fixture commit:

- C2's second-arbiter reproduction is single-process synthetic; a real
  cross-process (second owner process, fresh journal, no shared in-memory
  state) reproduction of the CE-03 window will be required for any future
  release-owner work (currently part of the D4/D5 required work).
- C4's three-caller probe relies on the `TestBarrier` public hook; the C4
  contract question itself (regardless of this probe) remains a pending R02/R06
  decision for the originating review only — the diagnostic does not assert
  what the decided contract must be.
- The C3 reconciler probe measures the caller-supplied allowance; the
  persisted-deadline consumption issue it demonstrates is a real R12 fact, but
  any fix decision remains with R07 (out of scope here).
- The lease temp directory uses `Path.GetTempPath() + "gagamba-wp05-lease-" +
  guid`; no fixed-name conflict exists; cleanup is unconditional in `finally`.
- `Wp05LeaseProcessProbes` consults `/proc` (Linux-wrapped API); the skip
  gate is `OperatingSystem.IsLinux()` — Windows publishes `Skipped: linux only`
  rather than a fail. Verified in the Windows log.

## 9. Explicit boundaries of this review

- Reviewer did not modify any production source, any D1-D3 test source, the
  frozen harness, or the committed evidence; doc-only additions on the review
  branch `review/wp05-d1-d3-fixture-evidence-review` from `338a98af…`.
- Reviewer did not decide, settle, or modify C1-C4 or any WP05 gate; the
  disposition above does not accept any gate.
- The diagnostic evidence base bounds: no fresh-process/second-owner cross-check,
  no real held-execution FD5 topology, no native coordinator negative frames,
  no target execution, no WP08/WP10 wiring, and full M1 assembly remain outside
  this slice.
