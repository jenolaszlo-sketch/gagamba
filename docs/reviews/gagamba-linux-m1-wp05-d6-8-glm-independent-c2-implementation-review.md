# Gagamba Linux M1 WP05 D6.8 — GLM Independent C2 Implementation Review

Reviewer: GLM (independent adversarial source and test reviewer)
Producer: DeepSeek V4.1
Scope: WP05 D6.8 C2 / R08 only. C1, C3 and C4 are human-approved contracts but
their production implementation was not authorized in this tranche and was not
performed. This review did not modify any source, test or producer artifact.

Status: **REVIEW COMPLETE — OPTION A: C2 AUTHORITY INVARIANT SUPPORTED BY SOURCE
AND DETERMINISTIC TESTS; NO BLOCKING FINDINGS; TWO NON-BLOCKING TEST-EVIDENCE
OBSERVATIONS; LINUX RUNTIME QUALIFICATION OUTSTANDING; RELEASE NOT QUALIFIED**

## A. Git binding

Repository: `C:\Users\Laszlos\source\repos\Gagamba`
(`https://github.com/jenolaszlo-sketch/gagamba.git`, verified via `git remote -v`).
The producer branch is local-only as reported; origin presence was not required.

### A.1 Verified commit chain

| Item | Verified value |
|---|---|
| D6.7 human-decision preservation base | `5993989e148b2e305a410c582eeaa1321bb60637` |
| Base tree | `8b0fed7cd9262c909883b7d002b6644918752892` |
| Base `src/` tree | `406f52da43b62920128151b2f5706c32d45b6db7` — identical to frozen R12 `src/` tree (verified by `git rev-parse` on both) |
| D6.8.1 tests-first commit | `9bafb411035567e007d105587247a9b3712b7bf1` (parent `5993989`) |
| D6.8.2 production commit | `9992e7211f60d9a9a70ec4a6e4bdf907790eef19` (parent `9bafb41`); tree `366f52b4dbd9f0099227b5df3c38d87cb94855a3` |
| D6.8.3 evidence commit (producer HEAD) | `665b18fdca15f8ed30ae4ca93b490f29bf41a181` (parent `9992e72`); final tree `ad2df42c80df1ffc6dc52673030de3790761723d` |
| `ReleaseArbiter.cs` base blob | `9f8533fd745c04b060c97c80c68da3724643922a` |
| `ReleaseArbiter.cs` implementation blob | `dca9ea1d6feea9fdb752de7d3e90f80431b85c58` |
| Implementation `src/` tree | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` |
| Frozen R12 production | commit `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040` — verified |
| Frozen WP05 harness Q3 | commit `36a124d7dff98ecfaf956340767fe1768ad5b17f`, tree `f764722997130f06c8a62c45a4230805aa538ea5` — verified |

All producer-reported SHAs, trees and blob identities were verified directly
with `git rev-parse`; every one matched. The chain `5993989 → 9bafb41 → 9992e72
→ 665b18f` is a strict linear parent chain (verified via `git log --format=%H %P`).
No frozen baseline was modified.

### A.2 Review worktree and binding

- Review worktree: `C:\Users\Laszlos\source\repos\Gagamba-review-d68-c2-glm`
- Review branch: `review/wp05-d6-8-glm-independent-c2`
- Created directly at the producer HEAD `665b18fdca15f8ed30ae4ca93b490f29bf41a181`;
  the review commit below is therefore a direct child of the producer HEAD.
- The producer worktree, canonical checkout and frozen evidence were not modified.
- No merge, amend, rebase, cherry-pick or push was performed.

## B. Scope

Full diff `5993989..665b18f` contains exactly five files:

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | +17/-3 (production commit only, `9bafb41..9992e72`) |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs` | +432 (new) |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` | +26/-14 approx. (four probe conversions) |
| `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs` | one line (`capability_live` True→False) |
| `docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md` | +328 (new) |

Production scope is exactly C2 authority consumption. Verified absent from the
diff: C1 ambiguous persistence (`AmbiguousPersistence`), C3 durable-deadline
consumption, C3 Policy A, C3 typed stop outcomes, C4 duplicate/refusal redesign,
Owner orchestration, WP08/WP10 integration, journal schema or transition-planner
changes, native process supervision changes, new release authority, and
packaging/CI/release configuration. A pattern search over the full diff for
contract-term markers found matches only inside the evidence document's own
scope statements. The only production `PreparedTransaction.TryConsume` call
sites in `src/` are the pre-existing release-gate consume
(`ReleaseArbiter.cs:149`), the pre-existing release-path cleanup consume
(`:307`), and the new stop consume (`:312`); `TransactionStateModel.cs:274` is
the pre-existing preparation-completion consume, untouched.

## C. C2 authority invariant — source-level verdict

**SUPPORTED.** The contract (`docs/decisions/wp05-d6-0-c2-stop-capability.md`
A-C2-1/A-C2-2/A-C2-3, as approved in
`docs/decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md` §4)
requires irreversible, idempotent capability consumption at the first runtime
stop acceptance under `_gate`, on all three acceptance paths, independent of
durable StopRecord persistence.

The implementation (`ReleaseArbiter.cs:226-297`):

- `ConsumeStopCapability()` executes at `:237`, after the duplicate early-return
  (`:232-235`) and before all three first-acceptance branches (`:240` clock
  unavailable, `:249-262` overflow, `:264` valid deadline). There is no branch,
  exception path or return between the consume and `_stopAccepted = true` at
  `:238`; both occur inside `lock (_gate)`.
- `_stopAccepted = true` was consolidated from the three branch bodies into the
  common pre-branch path; the branch decisions, deadlines and reasons are
  byte-for-byte unchanged from the D6.7/R12 base (verified against the full diff).
- Consumption is through the issuing store's single-use
  `PreparedTransaction.TryConsume` (`PreparedTransaction.cs:60-69` →
  `PreparationStore.TryConsume`, `PreparationStore.cs:167-214`), independent of
  `_journalGate` and of the later durable write, which remains after the gate
  (`:272-289`).

The required invariant — after an accepted first stop,
`PreparedTransaction.IsLive == false` — holds in every required condition:

| Condition | Why `IsLive == false` holds |
|---|---|
| Clock valid | `TryConsume` succeeds → `entry.Consumed = true` under the store `_gate`; `TryLookup` refuses consumed entries. Red-proven by C2-T01. |
| Clock unavailable | Same consume call executes before the `now <= 0` branch (red-proven by C2-T02). |
| Deadline overflows | Same consume call executes before the `checked` deadline computation (see finding F-01 for the test-discrimination nuance). |
| Durable StopRecord succeeds | Consumption is not conditioned on persistence (red-proven by C2-T01/T05/T06). |
| Durable StopRecord fails | `WriteMutation` failure only appends a reason; capability already consumed (red-proven by C2-T04/T07). |
| Another arbiter shares the capability | The store's `entry.Consumed` flag and reference-equality lookup are shared state; `IsLive`/`TryConsume` from any instance observe the consumption (red-proven by C2-T07). |

A release authority after stop is refused at three independent layers: the
same-instance `_stopAccepted` veto (`:55-61` before intent, `:125-131` after
intent), the capability `IsLive` checks (`:72`, `:139`), and the store's
single-use consume (`:149`). A second arbiter sharing the capability fails the
`:72` check before any durable Intent, so no Intent is written and no transport
attempt occurs. The store remains the sole authority boundary; the arbiter adds
no new capability, token or field.

## D. Consumption failure analysis (`TryConsume` state transitions)

Store refusal states (`PreparationStore.cs:167-214`, all under the store `_gate`):
`Foreign` (unknown ID / reference mismatch / issuer-generation mismatch / digest
mismatch), `Aborted`, `Consumed`, `Expired` (`ExpiresAtNs <= clock`). None of
these mutate the entry.

For a store-issued capability (reference-equal entry, matching issuer,
generation and digest — always true for the minted object the arbiter holds):

1. **Refusal with a still-live capability is unreachable.** Every refusal state
   implies `TryLookup` fails with the same clock value: `Consumed`/`Aborted` are
   checked directly by `TryLookup` (`:154-155`); `Expired` in `TryConsume`
   (`ExpiresAtNs <= now₁`) implies `ExpiresAtNs > now₂` fails for any
   `now₂ >= now₁` on a monotonic clock; `Foreign` for the genuine minted
   capability cannot occur because Mint validated issuer/generation/digest
   (`:81-99`). A disposed capability (`_store == null`) returns false with
   `IsLive == false` (`PreparedTransaction.cs:52-55`, `:62-66`). Therefore the
   `InvalidOperationException` guard (`ReleaseArbiter.cs:317-321`) is reachable
   only with a fabricated/mutated capability object or a non-monotonic
   (rewound) test clock — a fail-closed defense-in-depth guard, not a
   production path. The producer's reachability claim is confirmed.
2. **Already-consumed status (release-first, A-C2-2).** `TryConsume` returns
   false with `Consumed`; the guard observes `IsLive == false` and does not
   throw; the stop is still accepted; no reactivation, clone or history rewrite
   occurs. Red-proven by C2-T09/T10 (`ReleaseAlreadyCommitted == true`, release
   evidence byte-identical, exactly one transport attempt).
3. **Expired/aborted capabilities remain non-live** and the stop still stands
   (`:79-86` of the evidence document describes this accurately; the store
   refuses an expired capability without setting `Consumed`, and `IsLive` is
   already false).
4. **Exception safety of the guard.** On throw: `_stopAccepted` remains false
   (it is assigned only after the consume returns), `_retainedStop` stays null,
   the capability remains live by the guard's own condition, no journal state
   was touched (the throw precedes every acceptance branch and every journal
   operation), and the `_gate` lock is released by normal unwinding. The state
   is exactly "stop did not happen"; a later stop attempt retries. No partial
   runtime state and no masked invariant failure is possible: the guard throws
   precisely when a refusal would have left live authority un-revoked, which is
   the one state that must never be silently accepted.
5. **Duplicate durable-write interaction.** A second arbiter that also accepts
   stop attempts a StopRecord write; `TryPlanStop` refuses ("the first accepted
   stop is immutable", `OwnershipTransitions.cs:118-122`) inside the
   `WriteMutation` delegate, which is converted to a `JournalIoResult` failure
   (`OwnershipJournal.cs:649-656`) — no exception escapes, `DurableStop` is
   false for the second arbiter, and the first StopRecord is preserved. This
   behavior is pre-existing R12 semantics, unchanged by C2.

**Verdict:** the `TryConsume` handling is safe and honest for every reachable
store state; no reachable counterexample to the single-use authority invariant
was found.

## E. Concurrency and lock-order analysis

- **Lock order.** The only new synchronization interaction is arbiter `_gate` →
  store `_gate` inside `ConsumeStopCapability` (via `TryConsume`/`IsLive`). This
  is the same order the pre-existing release paths already use (`:72` `IsLive`
  under `_gate`, `:149` `TryConsume` under `_gate`). The store never calls back
  into arbiter or journal locks; `_journalGate` is never taken while `_gate` is
  held in any path (stop journal write at `:272-289` and release journal writes
  at `:81-115`/`:194-211` all occur after the gate block exits). No inversion,
  no new lock, no unbounded wait (the store's `_gate` guards only in-memory
  dictionary operations and a clock read).
- **No journal I/O under `_gate`.** Verified: the stop's `WriteMutation` occurs
  strictly after the acceptance gate block; unchanged from R12.
- **Linearization.** First stop acceptance + consumption are jointly atomic
  under `_gate`: a concurrent release serialized on `_gate` either observes
  `_stopAccepted == true` (refused) or finds the capability already consumed
  (refused at `:139`/`:72` with `MayHaveExecuted`/`NotCommitted` respectively).
  A second arbiter's release fails the `:72` pre-Intent check — no Intent, no
  transport. The store's internal `_gate` makes the single-use transition hold
  across arbiter instances.
- **Duplicate stop** returns the retained decision without re-consuming
  (`:232-235` precedes `:237`); the three acceptance branches are unreachable
  on re-entry.
- **Release-first history.** `TryPlanStop` appends a StopRecord and never
  rewrites Release/delivery/Committed facts (validator-enforced; confirmed by
  C2-T09/T10 asserting identical `IntentAtNanoseconds`, `CommittedAtNanoseconds`,
  delivery and exactly one transport attempt). Stop after durable Intent
  preserves conservative `MayHaveExecuted` (C2-T08/T12) with the Intent record
  retained and no RELEASE frame (`Transport.Attempts == 0`).
- **At most one physical RELEASE** remains possible: transport write is reached
  only after a successful single-use `TryConsume` in the release gate, and the
  stop veto plus consumed capability close every other route.
- **Race-test quality.** C2-T12 forces its claimed order deterministically:
  the release task is parked at the `after-intent` barrier (durably-Intent state
  proven by the `atIntent` event before the stop is requested), the stop runs on
  the main thread while the release holds no lock, and `resume` gates the
  release's gate entry after the stop. C2-T08 forces the same order
  synchronously via the barrier callback. Neither test relies on `Task.Run`
  timing for its ordering claim; both use `ManualResetEventSlim`/barrier
  synchronization with bounded waits and watchdog timeouts.

## F. Test quality

### F.1 C2-T01–T12 verdicts

| Test | Claim | Verdict |
|---|---|---|
| T01 | Normal stop consumes; release refused; no Release record | **Adequate.** Red-failed pre-fix on `IsLive`; asserts durable stop, refusal kind/disposition, zero transport, null Release. |
| T02 | Clock-unavailable stop consumes without durable stop | **Adequate.** Red-failed; consumes genuinely (expiry 1e9 ns > clock 0, so not expiry-masked). |
| T03 | Overflow stop consumes without durable stop | **Passes but non-discriminating** — see F-2. |
| T04 | Failed StopRecord persistence still consumes | **Adequate** — see F-3 for fault-placement verification. |
| T05 | Duplicate stop idempotent, no second consume | **Property holds; evidence claim overstated** — see F-4. |
| T06 | Release after stop: no Intent, no FD4 | **Adequate.** Sequence unchanged, zero transport. |
| T07 | Second arbiter after failed durable stop cannot release | **Adequate** — refusal attribution proven (see F-3). |
| T08 | Post-Intent stop stays conservative `MayHaveExecuted` | **Adequate.** Barrier-forced order; Intent preserved, zero transport. |
| T09 | Release-before-stop leaves release evidence unchanged | **Adequate.** Delivery/commit timestamps identical; duplicate release returns retained result. |
| T10 | Stop after consumed release cannot reactivate | **Adequate.** BrokenPipe delivery preserved; capability stays dead. |
| T11 | Replacement context cannot inherit consumed capability | **Adequate at stated component scope** — see F-5. |
| T12 | Competing stop/release: one winner, no physical release | **Adequate.** Event-forced ordering (E section). |

### F.2 Critical: deadline-overflow discrimination gap (finding F-01, non-blocking)

T03 and the overflow probe set the clock to `long.MaxValue - 2_000_000_000L`
while the harness capability expires at `1_000_000_000`, so the capability is
already expired at the stop: `TryConsume` refuses with `Expired` **without
setting `Consumed`**, and `IsLive == false` holds via expiry, not via C2
consumption. The producer disclosed this honestly (evidence §5, §9).

Feasibility of a live-capability overflow case: **constructible with existing
facilities.** `Mint` requires only `expiresAtNs > clock` at mint time
(`PreparationStore.cs:96-99`); minting with an expiry near `long.MaxValue`
(e.g. `long.MaxValue - 1`) at the default clock, then advancing the clock to
`long.MaxValue - 2_000_000_000L`, yields a capability that is live immediately
before `RequestStop` (`ExpiresAtNs > now`), makes the deadline arithmetic
overflow, and — post-fix — is genuinely consumed by `ConsumeStopCapability`
before the overflow branch. Pre-fix, `IsLive` would be **true** after that
stop, so such a test red-fails and discriminates the C2 change on the overflow
path.

Classification: **non-blocking**, with rationale: (1) the overflow path shares
the single unconditional consume call site (`:237`) that is red-proven by
T01/T02/T04; there is no code between that call and the overflow branch that
can un-consume or bypass it, so the property on the overflow path is
established by source structure plus the red-proven shared call site; (2) the
required observable outcome (accepted, `DurableStop` false, deadline 0, no
durable StopRecord, `IsLive == false`) is asserted and passes; (3) the gap is
mechanism attribution on one of three branches, not a missing behavior.
Recommended (optional) correction: add the live-capability overflow test
described above; it requires no production or harness changes.

### F.3 Critical: failed StopRecord persistence (T04/T07 and the converted probe)

Fault injection `FakeJournalStorage.FailAt("write", 1)`
(`RecoveryTestSupport.cs:24-28`) arms an occurrence-counted fault **from arming
time**; the harness constructor's seed `WriteNext` completes before `FailAt` is
called, so the faulted occurrence is the first `"write"` after arming — the
StopRecord's `WriteMutation → Write → WriteAll`. `h.Sequence()` between arming
and the stop performs only read operations. Placement verified: the injected
failure targets the intended StopRecord write after journal seeding.

T04/T07 then assert the full required set: stop accepted at runtime;
`DurableStop == false`; `Latest().Stop == null` (no durable StopRecord);
`IsLive == false`; a second arbiter sharing the capability refused with
`NotCommitted` and `NotAttempted`; `Transport.Attempts == 0`; journal sequence
unchanged (no Intent); no Release record. Refusal attribution to capability
invalidation is proven by elimination: the durable-StopRecord veto cannot be
the cause (no StopRecord exists), prerequisites/identity/deadlines are the same
valid values other tests release with, the second arbiter has fresh
`_stopAccepted`/`_retainedRelease` state, and the refusal occurs at the
pre-Intent `:72` check before any journal read or write.

### F.4 Critical: duplicate stop (T05) — exactly-once consumption evidence

The counting-clock delta (`readsAfter - readsBefore == 1`) proves exactly one
clock read on the duplicate path (the `now` read at `:228`) and therefore no
clock-reading store interaction (`Mint`, and the expiry checks inside
`TryLookup`/`TryConsume` all read the clock). It does **not** by itself prove
the absence of a `TryConsume` call: `TryConsume` returns `Consumed`
(`PreparationStore.cs:185-189`) before its clock read, and `TryLookup`
short-circuits on `entry.Consumed` before its clock read, so a hypothetical
erroneous second consumption attempt on the duplicate path would also add zero
clock reads and leave the returned duplicate decision unchanged. The evidence
document's claim that T05 "proves … no `TryConsume`/`IsLive` read" is therefore
an overstatement (finding F-02).

The exactly-once property itself is nonetheless established by source branch
ordering, confirmed by this review: the duplicate early-return at
`ReleaseArbiter.cs:232-235` precedes `ConsumeStopCapability()` at `:237`, and
there is no other `TryConsume` call site reachable from `RequestStop`. The
store's single-use semantics guarantee that even a second consume attempt
anywhere would be refused. The narrowest honest correction is to reword the
evidence claim to "no clock-reading store interaction" and attribute
exactly-once consumption to source order plus store semantics. A
consume-counting test seam is possible but would require production
instrumentation, which this review does not require because source structure
already establishes the property.

### F.5 Critical: fresh-owner behavior (T11) — scope honesty

T11 proves exactly three component-level facts: (a) the issuing store cannot
re-mint the consumed preparation identity (`ArgumentException`); (b) the
consumed capability stays dead; (c) a replacement arbiter with a capability
minted in a **fresh store** over the same journal is refused release because
the durable StopRecord blocks Intent — zero transport, no Release record. It
does not claim production Owner integration, crash/restart lifecycle
qualification, or durable stop recovery beyond this evidence; the evidence
document states this scope explicitly (§8, §9). Correctly scoped. The
deliberately unchanged `D4_8` diagnostic (failed durable stop + fresh-process
new-test-only-capability release) remains C1/C3/C4/durable-recovery territory
per the approved contract ("runtime invalidation is not inherited" by a fresh
owner); this review does not demand it.

### F.6 Historical probe conversions and the D4 one-line change

The four renamed R12 probes invert exactly the unsafe R12 expectations
(`IsLive == true` after stop; second-arbiter release succeeds after failed stop
persistence) into named C2 regressions with strictly stronger assertion sets
(the persistence-failure probe additionally pins the journal baseline sequence
and asserts no Release record). The historical unsafe behavior remains
recoverable as evidence at the base commit `5993989` and is no longer asserted
as desired by the active suite — verified by the red-phase reproduction, where
the R12 production failed exactly these renamed tests.

The D4 change (`D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`,
`Wp05D4SupervisorTests.cs:458`) flips only the `capability_live` observation
expectation `True → False`, which is a necessary C2 semantic consequence of the
accepted durable stop consuming the runtime capability. All other assertions in
the test (stop accepted and durable, release refused, replacement refused with
zero transport, no pre-write witness, no transport witness) are unchanged. It
weakens no release, witness or recovery assertion. The test is Linux-only
(`SkipUnlessQualified`) and was skipped on the Windows host — consistent with
the producer's disclosure.

## G. Independent reproduction

Environment (this review): Windows, .NET SDK 10.0.401 (only SDK), .NET host
10.0.12, net10.0 Release, xunit.v3 3.2.0 stack, Git 2.55.0.windows.3 — same
profile as the producer's recorded environment.

Commands and exact results (all executed by this reviewer):

| # | Execution | Command (abridged) | Result |
|---|---|---|---|
| 1 | Baseline focused (disposable worktree at `5993989`) | `dotnet test … -c Release --filter 'FullyQualifiedName~ReleaseArbiterTests\|FullyQualifiedName~Wp05ReleaseContractProbes'` | **29 passed, 0 failed** — matches |
| 2 | Red phase (disposable worktree at `9bafb41`, production unchanged — `git diff --stat 9bafb41..9992e72 -- src/` confirms only `ReleaseArbiter.cs` differs) | same filter + `Wp05D68C2StopCapabilityTests` | **41 total, 30 passed, 11 failed** — matches; every failure an `Assert.False` on `IsLive` (or downstream); the 11 failures are exactly the evidence §5 list; `C2_T03` and the overflow probe passed red (expiry masking, as disclosed) |
| 3 | Green focused (review worktree at `665b18f`) | same filter | **41 passed, 0 failed** — matches |
| 4 | Full affected profile | `--filter 'Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState'` | **509 total, 423 passed, 86 skipped, 0 failed** — matches |
| 5 | `Wp05D4SupervisorTests` class | class filter | **17 skipped, 0 failed** — matches |
| 6 | `IntegratedBarrierTests` class | class filter | **1 passed, 14 skipped, 0 failed** — matches |
| 7 | Solution build | `dotnet build Gagamba.sln -c Release` | **0 warnings, 0 errors** — matches |
| 8 | Hygiene | `git diff --check` | exit 0, no output — matches |

Whole-assembly arithmetic verified from runs 4-6 plus the producer's per-class
table: 509 + 100 process-global-class tests = 609 total; 423 + 1
(`IntegratedBarrierTests`) = 424 passed; 86 + 99 = 185 skipped; 0 failed.

Skipped tests inspected: all skips are Linux-only platform guards
(`SkipUnlessQualified` → `Assert.Skip("linux only")`), missing native/delegated
cgroup artifacts, or seccomp/mount/owner-protocol qualification classes. No C2
coverage is suppressed by a skip. The disposable red/baseline worktrees were
created from the exact test/production commits without rewriting history and
were removed after the runs. No test was modified to obtain a passing review.

## H. Findings

### Demonstrated implementation defects

None. No reachable counterexample to the C2 authority invariant was found.

### Missing or inadequate test evidence (non-blocking)

- **F-01 (non-blocking).** Overflow-path C2 discrimination gap.
  Evidence: `Wp05D68C2StopCapabilityTests.cs:161` (clock `long.MaxValue -
  2_000_000_000L` vs harness expiry `1_000_000_000`, `:66`);
  `Wp05ReleaseContractProbes.cs` overflow probe (same pattern). Consequence:
  the overflow branch's `IsLive == false` is currently satisfied by store
  expiry, not by the C2 consume; the test cannot distinguish the C2 fix from
  pre-existing expiry on that branch. Security consequence: none identified —
  an expired capability confers no release authority, and the shared consume
  call site is red-proven on the other branches. Smallest correction: add a
  live-capability overflow test (mint with expiry near `long.MaxValue`, then
  advance the clock to `long.MaxValue - 2_000_000_000L`; assert live before
  stop, accepted with deadline 0, no durable StopRecord, `IsLive == false`
  after). Feasible with existing facilities; no production change.
- **F-02 (non-blocking; evidence/reporting overstatement).** Evidence §8 claims
  the C2-T05 clock count "proves the duplicate path performs no store
  interaction … and no `TryConsume`/`IsLive` read". Strictly, `TryConsume`'s
  `Consumed` refusal (`PreparationStore.cs:185-189`) and `TryLookup`'s consumed
  short-circuit (`:153-157`) occur before their clock reads, so the count
  cannot detect a refused second consume. The exactly-once property holds by
  source order (`ReleaseArbiter.cs:232-235` precedes `:237`). Smallest
  correction: reword the evidence sentence to "no clock-reading store
  interaction" and attribute exactly-once consumption to source branch order
  plus store single-use semantics. No production instrumentation required.

### Observations (no action required)

- **F-03.** The `ConsumeStopCapability` `InvalidOperationException` guard is
  unreachable for store-issued capabilities under a monotonic clock and is a
  correct fail-closed invariant guard; its state transitions on throw are safe
  (section D.4).
- **F-04.** A second arbiter that also accepts stop receives a failed durable
  write via `WriteMutation`'s mutation-exception conversion ("the first
  accepted stop is immutable") rather than an exception; pre-existing R12
  semantics, unchanged and safe. Not covered by a dedicated cross-instance
  duplicate-stop test; noted as an optional future coverage item, not a C2
  defect.

### Evidence/reporting accuracy

All producer identity claims (commit chain, trees, blobs, base-`src/`
equality with frozen R12), all reported test totals, the red-phase failure
list, the honest overflow disclosure, the fault-injection description, the
skip inventory and the build/hygiene claims were independently verified and
are accurate. The only imprecision found is F-02.

## I. Qualification limitations

- **Native Linux:** all test evidence is Windows-hosted. Linux-only classes —
  including the updated `D5_CR3b` C2 expectation — are skipped on this host.
  No Linux runtime qualification was performed or claimed by the producer;
  this review performed none. Windows-hosted unit-test success does not
  establish native Linux qualification.
- **Owner integration:** C2 is implemented at the `ReleaseArbiter` component
  seam only. Production Owner orchestration (WP08/WP10) remains deferred; T11
  is component-level evidence only.
- **G3 / release:** not closed. C1, C3 (including Policy A) and C4 remain
  human-approved contracts without authorized implementation. Contract
  approval must not be read as source qualification.

## J. Final disposition

**Option A.**

`WP05 D6.8 GLM INDEPENDENT C2 IMPLEMENTATION REVIEW COMPLETE - OPTION A: C2 AUTHORITY INVARIANT SUPPORTED BY SOURCE AND DETERMINISTIC TESTS; NO BLOCKING FINDINGS; LINUX RUNTIME QUALIFICATION OUTSTANDING; C1 C3 C4 IMPLEMENTATION NOT AUTHORIZED; RELEASE NOT QUALIFIED`

Basis: the production change implements exactly the approved A-C2-1/A-C2-2/
A-C2-3 semantics at the specified linearization point with correct failure
handling; every stop acceptance path is covered by a single consume call site
executed before all branches; the single-use capability store remains the sole
authority boundary; release-first and duplicate-stop semantics are preserved;
all reported evidence reproduced exactly; the two non-blocking findings (F-01
overflow test discrimination, F-02 evidence wording) do not undermine the
invariant, which is established by source structure and red-proven tests on
the shared call site. Recommended follow-ups (non-blocking, no source change
during this review): the F-01 discriminating overflow test and the F-02
evidence-wording correction, each addressable in a later authorized tranche.
