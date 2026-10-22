# WP05 D6.8 C2 - Stop-First Capability Invalidation Evidence

Status: **D6.8 IMPLEMENTATION AND DETERMINISTIC TEST EVIDENCE - D6.8.4 EVIDENCE
CORRECTIONS APPLIED (F-01 LIVE-CAPABILITY OVERFLOW REGRESSION; F-02 WORDING
CORRECTION); FOCUSED GLM FINDING-CLOSURE REVIEW PENDING; LINUX RUNTIME
QUALIFICATION PENDING; RELEASE NOT QUALIFIED**

Scope: WP05 D6.8 C2 / R08 only. C1, C3 and C4 are not implemented. No durable
journal schema, coordinator protocol, FD4 transport, native sandbox, production
Owner integration, configuration, packaging or CI change is included.

Interpretation: tests passing demonstrates the behavior covered by the executed
tests on the recorded environment. It does not close G3 or WP05, does not
replace independent GLM source review, does not qualify a Linux runtime, and
does not establish release acceptance.

## 1. Git binding

| Item | Identity |
|---|---|
| Repository | `C:\Users\Laszlos\source\repos\Gagamba` (`https://github.com/jenolaszlo-sketch/gagamba.git`) |
| Worktree | `C:\Users\Laszlos\source\repos\Gagamba-d6-8-c2` |
| Branch | `implementation/wp05-d6-8-c2-stop-capability` |
| Implementation base (D6.7 preservation) | `5993989e148b2e305a410c582eeaa1321bb60637` (parent `01063c16...`) |
| D6.8.1 tests-first commit | `9bafb41` - `test(wp05): add C2 stop-capability regression coverage` |
| D6.8.2 production commit | `9992e7211f60d9a9a70ec4a6e4bdf907790eef19` - `fix(wp05): consume release capability on accepted stop` |
| Final implementation tree | `366f52b4dbd9f0099227b5df3c38d87cb94855a3` |
| Base `src/` tree | `406f52da43b62920128151b2f5706c32d45b6db7` |
| Implementation `src/` tree | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` |
| ReleaseArbiter.cs base blob | `9f8533fd745c04b060c97c80c68da3724643922a` |
| ReleaseArbiter.cs implementation blob | `dca9ea1d6feea9fdb752de7d3e90f80431b85c58` |

The implementation branch is a direct descendant of the D6.7 preservation. No
push, merge, amend or rebase was performed. Frozen R12 and harness Q3 commits
were not modified.

### 1.1 Authorization and frozen evidence verification

| Evidence | Verified value |
|---|---|
| D6.7 human approval producer | `01063c164391e0676ae1f6087d2c0865f7692511`; immediate parent of the preservation commit |
| D6.7 GLM integrity review | `546f284fbdbbcaa76f8b5557328fcd2d502a86c0`; sibling of preservation, both children of `01063c1`; review document blob `fa78dc9a8a0023961de95fff3358b8dd0add10a6` at `docs/reviews/gagamba-linux-m1-wp05-d6-7-glm-human-decision-integrity-review.md` |
| D6.7 preservation | `5993989e148b2e305a410c582eeaa1321bb60637`; records the review binding in `docs/reviews/gagamba-linux-m1-wp05-d6-7-glm-review-preservation-checkpoint.md` |
| C2 approval | `docs/decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md` section 4: human approved C2 as specified; "C2 — Stop-first capability invalidation (R08) | APPROVED" |
| R12 production | commit `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040` |
| WP05 harness Q3 | commit `36a124d7dff98ecfaf956340767fe1768ad5b17f`, tree `f764722997130f06c8a62c45a4230805aa538ea5` |
| R12/Q3 ancestry | R12 is an ancestor of Q3; R12 is an ancestor of the D6.7 preservation |

Pre-implementation tree identity proof: `git diff --stat
5993989e148b2e305a410c582eeaa1321bb60637:src
38f6981b2a60daab2df37c92913a6760c1bef131:src` produced no output and
`git rev-parse` returned the identical tree object
`406f52da43b62920128151b2f5706c32d45b6db7` for both. The D6.7 preservation
`src/` tree had no difference from frozen R12 before implementation.

The historical `PROPOSED - NOT APPROVED` text in
`docs/decisions/wp05-d6-0-c2-stop-capability.md` remains unchanged and is
superseded by the D6.7 approval record. No historical document was rewritten.

## 2. Production change (D6.8.2)

Single production file: `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`
(+17/-3). No other production file changed. Full diff:
`git show 9992e7211f60d9a9a70ec4a6e4bdf907790eef19`.

`ReleaseArbiter.RequestStop(StopCauseKind cause)` now calls the new private
`ConsumeStopCapability()` under `_gate` immediately after the duplicate-stop
check and before any of the three first-acceptance branches (clock unavailable,
deadline overflow, valid deadline). `_stopAccepted = true` was consolidated to
that same point; the three branch decisions and reasons are unchanged.

Linearization point: the first runtime stop acceptance and the shared-capability
consumption through `PreparedTransaction.TryConsume` (issuing
`PreparationStore`) executed under the single `_gate` arbitration boundary. The
consumption does not depend on `_journalGate` or on the later durable StopRecord
write, which remains unchanged and continues after the gate.

`ConsumeStopCapability()` behavior:

- `TryConsume == true`: capability consumed irreversibly by the issuing store.
- `TryConsume == false` with `Consumed` (release-first), `Aborted` or `Expired`:
  the stop is still accepted; the store holds no further release authority; the
  capability is not reactivated and no history is rewritten. Note: the existing
  store refuses an expired capability without setting the consumed flag, and
  `IsLive` is already false for it; this is existing store semantics, not new
  authority.
- `TryConsume == false` with `Foreign`: no live authority in the issuing store;
  the stop stands.
- Any refusal that would leave `IsLive == true`: throws
  `InvalidOperationException` instead of silently reporting revocation. Under the
  inspected store semantics this state is unreachable for a store-issued
  capability (entries are never removed; a reference-equal entry with matching
  issuer/generation/digest can only refuse via Consumed/Aborted/Expired).

Locking and authority review:

- No journal I/O is introduced while `_gate` is held; no waits added under
  `_gate`; no new lock. Lock order is unchanged: arbiter `_gate` then store
  `_gate`, exactly as the pre-existing release paths already do with
  `IsLive`/`TryConsume`.
- The release-path veto on `_stopAccepted` (`NotCommitted` before durable
  intent; `MayHaveExecuted` after durable intent) is unchanged. C2 adds no new
  release authority and no new capability store, token or field.
- No durable schema, journal layout, transition planner, coordinator protocol or
  transport change.

## 3. Test change (D6.8.1)

Test-only changes in `9bafb41`:

- New `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs`
  with C2-T01..T12 (deterministic in-memory harness, barrier synchronization,
  bounded watchdogs; no sleeps as race control).
- `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`:
  the four obsolete R12 expectations are replaced by explicitly named C2
  regressions:
  - `StopFirstLeavesCapabilityLiveOnR12` -> `StopFirstConsumesCapabilityC2`
  - `StopWithUnavailableClockIsLiveAcceptedWithoutDurableStop` ->
    `StopWithUnavailableClockConsumesCapabilityC2`
  - `StopDeadlineOverflowIsLiveAcceptedWithoutDurableStop` ->
    `StopDeadlineOverflowConsumesCapabilityC2`
  - `StopPersistenceFailureAllowsSecondArbiterReleaseOnR12` ->
    `StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2`
  The original probe bodies and names remain recoverable from the immutable base
  commit and prior preservation evidence.
- `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs`
  (`D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease`, Linux-only): the
  `capability_live` expectation is updated from `True` to `False` because the
  accepted durable stop now consumes the runtime capability. The test's
  durable-stop/fresh-lease refusal behavior is unchanged. `D4_8_FailedDurableStopAllowsOnlyDiagnosticReplacementRelease`
  is deliberately unchanged: its replacement process mints a
  `new-test-only-capability` in a fresh store while the journal has no
  StopRecord; C2 closes the shared-capability window, and recovery
  classification of failed durable stops remains C1/C3/C4 territory.

## 4. Environment

| Item | Value |
|---|---|
| OS | Windows 10.0.26200, x64 (`win-x64`) |
| .NET SDK | 10.0.401 (MSBuild 18.9.11); only SDK installed |
| .NET host runtime | 10.0.12 (runtimes 8.0.31 / 9.0.20 / 10.0.12) |
| Target framework | net10.0, `Release`, `TreatWarningsAsErrors=true` |
| Test stack | xunit.v3 3.2.0, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 18.0.1 |
| Git | 2.55.0.windows.3 |

Linux OS-level qualification was not performed on this host. Linux-only classes
skip with platform/precondition reasons (see section 7).

## 5. Red phase (unchanged production, D6.8.1 tree)

Baseline before test changes (base commit, focused):
`ReleaseArbiterTests` + `Wp05ReleaseContractProbes` = 29 passed / 0 failed.

With the new and revised tests against the unchanged production source:

```
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo --logger "console;verbosity=normal" \
  --filter 'FullyQualifiedName~ReleaseArbiterTests|FullyQualifiedName~Wp05ReleaseContractProbes|FullyQualifiedName~Wp05D68C2StopCapabilityTests'
```

Result: **41 total, 30 passed, 11 failed** - every failure is an `Assert.False`
on `Capability.IsLive` (or an assertion downstream of it):

1. `Wp05ReleaseContractProbes.StopFirstConsumesCapabilityC2`
2. `Wp05ReleaseContractProbes.StopWithUnavailableClockConsumesCapabilityC2`
3. `Wp05ReleaseContractProbes.StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2`
4. `Wp05D68C2StopCapabilityTests.C2_T01_NormalStopFirstConsumesCapabilityAndBlocksRelease`
5. `Wp05D68C2StopCapabilityTests.C2_T02_ClockUnavailableStopConsumesCapabilityWithoutDurableStop`
6. `Wp05D68C2StopCapabilityTests.C2_T04_StopRecordPersistenceFailureStillConsumesCapability`
7. `Wp05D68C2StopCapabilityTests.C2_T05_DuplicateStopIsIdempotentWithoutSecondConsumeOrDeadline`
8. `Wp05D68C2StopCapabilityTests.C2_T06_ReleaseAfterStopWritesNoIntentAndNoPhysicalRelease`
9. `Wp05D68C2StopCapabilityTests.C2_T07_SecondArbiterAfterFailedDurableStopCannotRelease`
10. `Wp05D68C2StopCapabilityTests.C2_T11_ReplacementContextCannotInheritConsumedCapability`
11. `Wp05D68C2StopCapabilityTests.C2_T12_CompetingStopAndReleaseHaveOneWinnerAndNoPhysicalRelease`

Honest red-phase note: `C2_T03` and the overflow probe already passed before the
production change because the pre-existing store treats the capability as
expired at the overflow timestamp (`IsLive == false` via expiry). The C2
requirement "deadline overflow must not preserve a live capability" was already
met by expiry for that artificial clock; the normal, clock-unavailable and
failed-persistence closures are the red-proven deltas. The overflow path still
runs the same `ConsumeStopCapability()` acceptance step after the fix.

## 6. Green phase (D6.8.2 tree)

Focused selection, three consecutive runs (build + two `--no-build` repeats):

```
Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41
Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41
Passed! - Failed: 0, Passed: 41, Skipped: 0, Total: 41
```

Full affected project, supported profile regular phase (process-global classes
excluded, per `eng/linux-m1-qualified-tests.sh`):

```
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo --logger "console;verbosity=minimal" \
  --filter 'Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState'
```

Result: **509 total, 423 passed, 86 skipped, 0 failed**.

Process-global classes, each in its own test host (script list) plus the
D4 supervisor class:

| Class | Result |
|---|---|
| `Recovery.NonRootQualificationTests` | 31 skipped, 0 failed |
| `Recovery.OwnerDeathQualificationTests` | 7 skipped, 0 failed |
| `Recovery.IntegratedBarrierTests` | 1 passed, 14 skipped |
| `Recovery.DomainAuthorityTests` | 21 skipped, 0 failed |
| `Recovery.RecoveryHarness` | 5 skipped, 0 failed |
| `Recovery.Wp05TerminationObserverTests` | 3 skipped, 0 failed |
| `Launch.Wp05SigpipeDispositionTests` | 1 skipped, 0 failed |
| `D4.Wp05D4SupervisorTests` | 17 skipped, 0 failed |

Whole-assembly totals across the two phases: **609 total, 424 passed, 185
skipped, 0 failed**.

Adjacent build regression: `dotnet build Gagamba.sln -c Release --nologo`
succeeded with 0 warnings and 0 errors (warnings are errors in this repository).

Hygiene: `git diff --check` exit 0 with no output.

## 7. Skipped / unavailable tests

All skips are pre-existing environment skips (Linux-only platform guards,
missing delegated cgroup / native artifacts, or `SkipUnlessQualified`), not
suppressed C2 coverage:

- Linux-only release/recovery qualification classes listed in section 6
  (`Wp05ReleasePathIndependentQualificationTests`, native copy-in/protocol
  classes, mount verifier, seccomp pin, journal fencing, owner protocol,
  lease-process probes, and the D4/D5 supervisor suite).
- The updated `D5_CR3b` expectation is therefore committed but not executed on
  this host; Linux runtime qualification remains pending with its own approved
  plan.

No test was skipped, deleted or weakened to obtain a green result.

## 8. Coverage map and invariant disposition

| Matrix | Executed test(s) | Required result | Status |
|---|---|---|---|
| C2-T01 | `C2_T01_...`, `StopFirstConsumesCapabilityC2` | Accepted; durable stop; capability not live | pass |
| C2-T02 | `C2_T02_...`, `StopWithUnavailableClockConsumesCapabilityC2` | Accepted; no durable stop; not live | pass |
| C2-T03 | `C2_T03_ExpiredCapabilityOverflowStopAcceptedWithoutDurableStop`, `ExpiredCapabilityOverflowStopAcceptedC2` (D6.8.4 names; see 12.1) | Accepted; no durable stop; not live | pass (expired-capability closure; see 5 and 12.1) |
| C2-T03b | `C2_T03b_LiveCapabilityOverflowStopConsumesCapability` (D6.8.4) | Live capability consumed on overflow acceptance; no durable StopRecord; no Intent; no FD4 | pass (red-proven against pre-fix R12 stop behavior; see 12.1) |
| C2-T04 | `C2_T04_...`, `StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2` | Runtime accepted; durable absent; not live | pass |
| C2-T05 | `C2_T05_...` | Idempotent; no fresh deadline; exactly-once consumption by source order | pass (see 8 and 12.2) |
| C2-T06 | `C2_T06_...` | Refused; no new Intent or FD4 | pass |
| C2-T07 | `C2_T07_...` | Second arbiter refused; zero release writes | pass |
| C2-T08 | `C2_T08_...` | Conservative `MayHaveExecuted`; no FD4 | pass |
| C2-T09 | `C2_T09_...` | Release evidence unchanged | pass |
| C2-T10 | `C2_T10_...` | Stop accepted; capability stays consumed | pass |
| C2-T11 | `C2_T11_...` | No inherited capability; no RELEASE resend | pass (component level only) |
| C2-T12 | `C2_T12_...` | Valid winner; no extra physical RELEASE | pass |

C2-T04/C2-T07 use `FakeJournalStorage.FailAt("write", 1)` after the seeded
prepared baseline and assert separately: stop accepted at runtime; `DurableStop`
false; no StopRecord; `IsLive` false; a second arbiter sharing the same
capability is refused; zero transport attempts; no Intent or RELEASE record; the
journal sequence is unchanged. The refusal is therefore proven to come from
capability invalidation, not from a persisted StopRecord.

C2-T05 confirms that a duplicate stop preserves the original StopDecision and
does not establish a fresh deadline. The counting clock observes the expected
entry clock read, but does not directly count TryConsume calls. Exactly-once
invocation on the duplicate path is supported by the source-level early return
before ConsumeStopCapability, while the PreparationStore independently
enforces single-use capability semantics. (Corrected in D6.8.4 for GLM finding
F-02; the original wording overstated what the clock count establishes. See
12.2.)

C2-T07/C2-T06 prove zero release writes. C2-T08 preserves the existing
post-Intent conservative classification. C2-T09/T10 prove release-first history,
delivery evidence and durable Committed evidence are unchanged and that the
retained release decision is returned as a duplicate without a new write.
C2-T11 proves, at component level with a fresh store and the same journal, that
(a) the issuing store cannot re-mint the consumed preparation identity,
(b) the consumed capability stays dead, and (c) a fresh-owner-style replacement
is refused by the durable stop with zero transport. It does not claim production
Owner integration (WP08/WP10 remain out of scope), and the multi-instance
construction remains the deliberately synthetic CE-03 diagnostic: the CE-03
window is outside the accepted single-arbiter owner model and is closed here by
the capability contract, not by composition.

Acceptance criteria disposition: (1)-(11) satisfied by the diff and tests above;
no C1/C3/C4 production change; no new authority, schema, lock inversion or
unbounded wait; independent GLM review is explicitly still pending.

## 9. Known limitations

- Windows-only execution; Linux OS-level qualification outstanding.
- The expired-capability overflow variants (`C2_T03`, the overflow probe; see
  12.1 for their D6.8.4 names) close via existing store expiry semantics
  (`TryConsume -> Expired`, `IsLive -> false`) rather than the consumed flag.
  The D6.8.4 live-capability overflow regression (`C2_T03b`) separately proves
  consumption on the overflow path; reported explicitly for review.
- Failed durable stop plus owner death with a newly minted test-only capability
  remains the D4-8 diagnostic under recovery; that is C1/C3/C4/durable-recovery
  scope, not authorized here.
- Test-pass evidence is not release acceptance; G3/WP05 remain open.

## 10. Independent review handoff (GLM)

Base: `5993989e148b2e305a410c582eeaa1321bb60637`. Tests: `9bafb41`. Production:
`9992e7211f60d9a9a70ec4a6e4bdf907790eef19` (tree
`366f52b4dbd9f0099227b5df3c38d87cb94855a3`). Source tree comparison: base `src/`
= `406f52da...` equals frozen R12; implementation `src/` = `84cfabb8...`.

Review questions:

1. Is the capability invalidation performed at the correct linearization point
   (first stop acceptance plus `TryConsume` under `_gate`) for all three
   acceptance paths?
2. Is the `TryConsume` failure handling safe and honest, including the
   unreachable-live-state throw and the release-first `Consumed` case?
3. Do the shared-capability multi-instance tests actually prove C2 rather than
   relying on the durable StopRecord, journal writer fence or `_stopAccepted`?
4. Is release-first history, delivery evidence and durable Committed evidence
   provably unchanged, and is the post-Intent stop race still conservatively
   classified?
5. Is the lock ordering and absence of journal I/O under `_gate` preserved?
6. Are the tests adversarial (fault injection, counting-clock duplicate proof,
   barrier-controlled competition) rather than confirmatory?
7. Does the production diff contain any unauthorized contract change?

## 11. Disposition

`WP05 D6.8 C2 IMPLEMENTATION COMPLETE - STOP CAPABILITY CONSUMED AT FIRST
RUNTIME ACCEPTANCE; DETERMINISTIC TESTS PASS; INDEPENDENT GLM SOURCE REVIEW
REQUIRED; C1 C3 C4 IMPLEMENTATION NOT AUTHORIZED; RELEASE NOT QUALIFIED`

## 12. D6.8.4 evidence correction addendum (F-01 / F-02)

Added by the D6.8.4 correction series on branch
`evidence/wp05-d6-8-4-c2-f01-f02`, based directly on the D6.8 preservation
commit `7177b3b83658de74b795de0c072589c590119c0a`. This addendum addresses the
two non-blocking findings raised by the GLM independent review
(`959f2d3162bdfbecac2723f50acae8459b2fa310`; review document blob
`dee4ca9244f5a8c5613152b9a3a9a90a96294ed2`). It does not rewrite the D6.8
historical record above; the pre-correction evidence remains recoverable at its
original commit `665b18fdca15f8ed30ae4ca93b490f29bf41a181`. The GLM review did
not verify the new D6.8.4 test; it classified the gap as non-blocking finding
F-01. Focused GLM finding-closure review is pending.

Commit structure: D6.8.4a test commit
`0aa201c3c5689690217ff5ae429de0bcda6943ff`
(`test(wp05): prove live-capability invalidation on stop deadline overflow`;
immediate parent `7177b3b8...`; two test files, +32/-4); D6.8.4b evidence commit
`docs(wp05): address D6.8 GLM findings F-01 and F-02` (direct child of
`0aa201c3...`; this document only). No production file, schema, build,
packaging or CI change: the production `src/` tree remains
`84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` throughout D6.8.4.

### 12.1 F-01 - live-capability overflow regression

D6.8.1's `C2_T03_DeadlineOverflowStopConsumesCapabilityWithoutDurableStop` and
`StopDeadlineOverflowConsumesCapabilityC2` used a capability expiring at
`1_000_000_000` with the clock set to `long.MaxValue - 2_000_000_000L`, so the
capability was already expired at stop acceptance and `IsLive == false` was
satisfied by store expiry rather than by C2 consumption (GLM finding F-01).

D6.8.4 adds `C2_T03b_LiveCapabilityOverflowStopConsumesCapability` in
`Wp05D68C2StopCapabilityTests.cs`, using a small optional `expiry` parameter on
the existing harness constructor (default unchanged at `1_000_000_000`; no
fixture duplication). The capability is minted with
`expiry = long.MaxValue - 1` while the clock is at its ordinary initial value
(`1_000`), satisfying the store's mint contract
(`expiresAtNs > clock.NowNanoseconds`, `PreparationStore.cs:96-99`). Only after
minting does the test advance the clock to
`overflowNow = long.MaxValue - 2_000_000_000L`. Immediately before
`RequestStop` the test asserts `Capability.ExpiresAtNs > overflowNow` and
`Capability.IsLive`, so the capability is provably live at stop acceptance and
only the `checked` deadline addition
(`now + StopConfirmationAllowanceNanoseconds = now + 5_000_000_000`) overflows.

Required stop outcomes asserted: stop accepted; `DurableStop == false`;
`StopAcceptedAtNanoseconds == overflowNow`; `StopDeadlineNanoseconds == 0`; no
durable StopRecord (`Latest().Stop == null`); journal sequence unchanged (no
ReleaseIntent); `Latest().Release == null`; zero transport attempts
(`Transport.Attempts == 0`); `IsLive == false`; and the post-stop capability
state is `Consumed`, not merely expired (`TryConsume` refuses with
`InvariantViolation.Consumed`). The `TryConsume` verification runs after the
stop and therefore cannot consume the capability before it.

Rename for accuracy: the expiry-masked variants are retained as separate
expired-capability behavior tests under honest names -
`C2_T03_DeadlineOverflowStopConsumesCapabilityWithoutDurableStop` ->
`C2_T03_ExpiredCapabilityOverflowStopAcceptedWithoutDurableStop`, and
`StopDeadlineOverflowConsumesCapabilityC2` ->
`ExpiredCapabilityOverflowStopAcceptedC2`. They are not represented as proof of
live-capability consumption.

Red/green proof (Windows 10.0.26200 x64, .NET SDK 10.0.401, host 10.0.12,
net10.0 Release):

| Execution | Source identity | Observed result |
|---|---|---|
| Red: D6.8.4a test patch applied uncommitted to a disposable checkout at tests-first `9bafb411035567e007d105587247a9b3712b7bf1` (unchanged pre-fix production; `src/` tree `406f52da43b62920128151b2f5706c32d45b6db7`) | pre-fix R12 stop behavior | Focused selection: **42 total, 30 passed, 12 failed** - the D6.8.1 red list (11 failures) plus `C2_T03b`. `C2_T03b` failed at `Wp05D68C2StopCapabilityTests.cs:196` (`Assert.False(h.Capability.IsLive)`) with `Assert.False() Failure / Expected: False / Actual: True`: pre-fix stop acceptance does not consume, so the live capability remained live after the overflow stop |
| Green: D6.8.4a commit `0aa201c3c5689690217ff5ae429de0bcda6943ff` against unchanged D6.8 production (`src/` tree `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126`) | D6.8 C2 production | `C2_T03b` passed on three consecutive executions; focused selection **42 passed / 0 failed** |

The red failure is the discriminating `IsLive` assertion (GLM predicted exactly
this red behavior); the green execution confirms consumption via the store's
`Consumed` state. No production code was changed to make the test pass.

### 12.2 F-02 - duplicate-stop evidence precision

Original D6.8.1 wording in section 8 overstated the counting-clock result as
proving the duplicate path performs "no `TryConsume`/`IsLive` read". That
conclusion is too strong: `PreparationStore.TryConsume` returns `Consumed`
(`PreparationStore.cs:185-189`) and `TryLookup` short-circuits on consumed
entries (`PreparationStore.cs:153-157`) before their clock reads, so clock
counting cannot distinguish an absent second `TryConsume` invocation from a
refused second invocation that never reached the clock.

The corrected paragraph (replacing the original text in section 8) distinguishes
three different forms of evidence:

- **Observed test result:** the duplicate stop performs the expected entry clock
  read and preserves the original StopDecision, deadline and durable evidence.
- **Source-level proof:** the duplicate-stop early return in
  `ReleaseArbiter.RequestStop` (`ReleaseArbiter.cs:232-235`) precedes
  `ConsumeStopCapability()` (`:237`), so duplicate-stop execution does not reach
  the consumption call site; no other `TryConsume` call site is reachable from
  `RequestStop`.
- **Store invariant:** the issuing `PreparationStore` independently enforces
  single-use authority even if another operation attempts to consume an
  already-consumed capability.

These are not conflated: exactly-once invocation is a source-order property;
the clock count is a supporting observation only. No production instrumentation
was added to count consumption calls, and no C2 semantics changed.

### 12.3 D6.8.4 scope

Test-only: `Wp05D68C2StopCapabilityTests.cs` (harness expiry parameter, T03
rename, new T03b) and `Wp05ReleaseContractProbes.cs` (rename only).
Evidence-only: this document (status, section 8 paragraph, coverage map,
section 9 bullet, this addendum). No production, schema, coordinator, native,
build, packaging or CI change. Linux runtime qualification remains outstanding;
release is not qualified; C1, C3 and C4 implementation remains unauthorized.
