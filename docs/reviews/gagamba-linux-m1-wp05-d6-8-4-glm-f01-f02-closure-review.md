# WP05 D6.8.4 GLM Independent F-01/F-02 Closure Review

Status: **OPTION A - BOTH FINDINGS CLOSED; PRODUCTION SOURCE UNCHANGED; LINUX
RUNTIME QUALIFICATION OUTSTANDING; RELEASE NOT QUALIFIED**

Reviewer: GLM (independent). Producer: DeepSeek V4.1. This is a focused closure
review of D6.8.4 only; it is not a new review of the complete C2
implementation, the C1-C4 contracts, or any other work package.

## 1. Git binding

Repository: `C:\Users\Laszlos\source\repos\Gagamba`
(origin `https://github.com/jenolaszlo-sketch/gagamba.git`; the D6.8.4 branch is
local only, which is acceptable).

All objects verified present, including the original review document blob,
which lives on a sibling branch and resolves independently of the producer
checkout.

| Object | Role | Verified |
|---|---|---|
| `7177b3b83658de74b795de0c072589c590119c0a` | D6.8 preserved baseline | commit; tree `168c532ee35914a9028e89e7afc67644245482d2` |
| `0aa201c3c5689690217ff5ae429de0bcda6943ff` | D6.8.4a test commit | immediate parent `7177b3b8...` (verified); tree `f05c4065f2db9dd15e1fed531d53293fdb9c2f81` |
| `01da5d38a60a6f369c45a87f5bb18e2d8240081c` | D6.8.4b evidence commit | immediate parent `0aa201c3...` (verified); tree `19d1241eb556f4452b38230e594d5abdb0a0f73a` |
| `19d1241eb556f4452b38230e594d5abdb0a0f73a` | Final producer tree | equals tree of `01da5d38...` (verified) |
| `959f2d3162bdfbecac2723f50acae8459b2fa310` | D6.8 GLM review commit | commit (untouched; tree `f6641af007d8916efa1f8925765d4353eb806f65`) |
| `dee4ca9244f5a8c5613152b9a3a9a90a96294ed2` | Original review document | blob, read at that exact object |
| `9bafb411035567e007d105587247a9b3712b7bf1` | D6.8.1 tests-first commit | commit; direct parent of fix `9992e72`; tree `1658b62c865ec9baf2820f3186b4453023e54eb6`; `src/` tree `406f52da43b62920128151b2f5706c32d45b6db7` |
| `665b18fdca15f8ed30ae4ca93b490f29bf41a181` | Pre-correction evidence commit | commit; historical record intact |

Review worktree:
`C:\Users\Laszlos\source\repos\Gagamba-review-d684-glm`, branch
`review/wp05-d6-8-4-glm-f01-f02-closure`, HEAD `01da5d38...` (immediate parent
of the review commit is the producer evidence commit). No existing worktree was
modified; nothing was pushed, rebased, merged, amended or cherry-picked.

## 2. Scope and tree comparison

`git diff 7177b3b8... 01da5d38...` yields exactly three changed files, matching
the authorized set:

1. `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs`
   (harness `expiry` parameter, T03 rename, new T03b; +32/-4 in `0aa201c3`)
2. `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`
   (rename only; 1 line)
3. `docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md`
   (status, section 8 paragraph, coverage map, section 9 bullet, addendum
   section 12; +130/-11 in `01da5d38`)

Production identity resolved: the `src/` tree is
**`84cfabb8f82e3ac4061776cc9a9a8c4d5354a126`** at `7177b3b8`, `0aa201c3` and
`01da5d38` - byte-identical throughout D6.8.4. No production, native, journal,
schema, build, CI or packaging change exists in the diff. The D6.8 GLM review
commit and the D6.8 preservation checkpoint are untouched.

## 3. F-01 test construction and verdict

Test inspected at `01da5d38`:
`Wp05D68C2StopCapabilityTests.C2_T03b_LiveCapabilityOverflowStopConsumesCapability`.

### Preconditions (verified against source, not inferred from the name)

1. **Validly minted.** The harness mints through the issuing
   `PreparationStore.Mint` with `expiry = long.MaxValue - 1` while the
   `CountingClock` is at its initial value `1_000`
   (`Wp05D68C2StopCapabilityTests.cs:12`), so the mint constraint
   `expiresAtNs > _clock.NowNanoseconds` (`PreparationStore.cs:95-99`) holds;
   all other mint validations pass by construction.
2. **`ExpiresAtNs > NowNanoseconds` at stop.** The test asserts
   `Capability.ExpiresAtNs > overflowNow` immediately before `RequestStop`;
   `long.MaxValue - 1 > long.MaxValue - 2_000_000_000L` by 2e9 ns.
3. **`Capability.IsLive == true` before stop.** Asserted in the test.
   `IsLive` is a store `TryLookup` (`PreparedTransaction.cs:52-57`) that
   refuses consumed/aborted/expired entries and checks reference equality, so
   this assertion simultaneously establishes precondition 4.
4. **No prior consumption.** Follows from the `IsLive` assertion (consumed
   entries fail `TryLookup`) and from the test performing no other consuming
   operation before `RequestStop`.
5. **Genuine checked overflow.** `ReleaseArbiter.RequestStop` computes
   `checked { deadline = now + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds }`
   with the allowance equal to `5_000_000_000`
   (`OwnershipRecord.cs:202`). At
   `now = long.MaxValue - 2_000_000_000L` the addition exceeds `long.MaxValue`
   by 3e9 ns and must throw `OverflowException`; the `now <= 0` branch is not
   reachable at this clock value. `ConsumeStopCapability()` executes before
   both branches (`ReleaseArbiter.cs:237` before `:240-247` and `:249-261`), so
   the overflow path consumes a live capability.

### Postconditions (all ten asserted in the test)

1. `stop.Accepted` true.
2. `stop.DurableStop` false.
3. `stop.StopAcceptedAtNanoseconds == overflowNow`.
4. `stop.StopDeadlineNanoseconds == 0` (overflow decision shape,
   `ReleaseArbiter.cs:252-258`).
5. `h.Latest().Stop == null` - no durable StopRecord.
6. `h.Sequence()` equals the pre-stop baseline - journal sequence unchanged.
7. `h.Latest().Release == null` - no ReleaseIntent.
8. `h.Transport.Attempts == 0` - zero transport attempts.
9. `h.Capability.IsLive == false` after stop.
10. `h.Capability.TryConsume(...)` fails with `InvariantViolation.Consumed`.

### Consumption vs. expiry discrimination

The post-stop `TryConsume` verdict is genuine consumption, not expiration:

- The capability is not expired at the post-stop clock
  (`ExpiresAtNs = long.MaxValue - 1` vs `now = long.MaxValue - 2_000_000_000L`),
  so the `Expired` branch (`PreparationStore.cs:199-203`) is unreachable.
- Independently, the store's `TryConsume` checks `entry.Consumed`
  (`PreparationStore.cs:184-188`) before the expiry comparison, so a `Consumed`
  verdict cannot be produced by expiry alone.
- The pre-stop `IsLive == true` assertion rules out a pre-existing `Consumed`
  flag, so `Consumed` can only have been set by the stop path's
  `ConsumeStopCapability()` through the issuing store.

The last assertion therefore correctly distinguishes genuine C2 consumption
from simple expiration. The overflow acceptance cannot be explained by any
expiry-backed closure.

### Renamed historical probes

- `C2_T03_DeadlineOverflowStopConsumesCapabilityWithoutDurableStop` ->
  `C2_T03_ExpiredCapabilityOverflowStopAcceptedWithoutDurableStop`; body
  unchanged (default harness expiry `1_000_000_000`, clock advanced to the
  overflow value - the capability is expired at stop).
- `StopDeadlineOverflowConsumesCapabilityC2` ->
  `ExpiredCapabilityOverflowStopAcceptedC2`; body unchanged (rename-only diff,
  1 line).

No test was deleted or weakened; the renamed tests still exercise their
original expired-capability cases and are no longer presented as
live-capability evidence; T03b adds the missing live-capability case. The
original D6.8.1 names and bodies remain recoverable at `665b18f...` /
`7177b3b8...`.

**F-01 verdict: CLOSED** - live-capability overflow regression is genuinely
discriminating and supported by independently reproduced red/green evidence.

## 4. Red/green evidence verification

Producer-reported evidence was read from evidence document section 12.1 and
independently reproduced with isolated disposable checkouts (no frozen branch
or committed evidence was modified):

| Execution | Setup | Reported | Independently reproduced |
|---|---|---|---|
| Red | tests-first `9bafb411...` (pre-fix `src/` `406f52da...`) + D6.8.4a test patch applied uncommitted in a disposable checkout | 42 total, 30 passed, 12 failed; T03b fails at `Assert.False(h.Capability.IsLive)` | Yes: **42 total, 30 passed, 12 failed**; T03b failed with `Assert.False() Failure - Expected: False, Actual: True` at `Wp05D68C2StopCapabilityTests.cs:196` |
| Green | `0aa201c3...` tests against unchanged D6.8 production (`src/` `84cfabb8...`) | 42 focused passed; T03b passed 3x | Yes: **42 passed / 0 failed**; T03b passed 3 consecutive executions |

The red failure is specifically the discriminating assertion (the live
capability remained live after stop acceptance because pre-fix R12 stop
behavior does not consume), not an unrelated fixture failure: the identical
patch passes 42/42 against the unchanged D6.8 production source, and the only
difference between the two executions is the production stop behavior. The
producer's red/green commands, identities and outcome claims are accurate.

## 5. Historical probe preservation

Covered in section 3 above: rename-only changes, original bodies intact,
original cases still exercised, T03b additive, full pre-D6.8.4 history
recoverable from Git at `665b18f...` and the D6.8 baseline `7177b3b8...`.

## 6. F-02 evidence correction and verdict

The corrected section 8 paragraph and addendum section 12.2 of
`docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md` now separate
three forms of evidence, each verified against the implementation at
`01da5d38`:

1. **Observed behavior (clock count):** the duplicate stop returns the
   retained decision with the original accepted-at, deadline and durable
   evidence, and performs the expected entry clock read. The corrected text
   explicitly states the counting clock "does not directly count TryConsume
   calls" - the exact limitation this review identified in D6.8.
2. **Source-level proof:** verified in `ReleaseArbiter.RequestStop` - the
   duplicate early return (`_retainedStop is not null` ->
   `return _retainedStop with { Duplicate = true; }`, lines 232-235) precedes
   `ConsumeStopCapability()` (line 237), and no other `TryConsume` call site is
   reachable from `RequestStop`. The addendum's line citations are exact.
3. **Store invariant:** verified in `PreparationStore` - `TryConsume` refuses
   an already-consumed capability with `InvariantViolation.Consumed`
   (`:184-188`) and `TryLookup` short-circuits consumed entries (`:153-157`),
   both before any clock read, so single-use authority is enforced even against
   a later consume attempt.

The new wording is accurate, appropriately scoped (exactly-once invocation is
attributed to source order; the clock count is a supporting observation only),
and consistent with the implementation. The original overstatement is not
merely moved elsewhere: section 8, the coverage map row for T05 and the
section 9 limitation bullet were all corrected coherently. No production
instrumentation is required; the source proof is sound.

**F-02 verdict: CLOSED** - evidence precisely distinguishes clock observation,
source ordering and store authority.

## 7. Independently reproduced tests

Environment: Windows 10.0.26200 x64, .NET SDK 10.0.401 (host runtime
10.0.12/10.0.26200), target framework `net10.0`, configuration Release,
xUnit. Commands as recorded in the evidence document.

| Scope | Command / filter | Producer reported | Independently reproduced |
|---|---|---|---|
| Focused arbiter/probe/C2 | `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName~ReleaseArbiterTests\|FullyQualifiedName~Wp05ReleaseContractProbes\|FullyQualifiedName~Wp05D68C2StopCapabilityTests"` | 42 passed, 0 failed | **42 passed, 0 failed, 0 skipped** |
| T03b stability | same filter restricted to `C2_T03b` | passed 3x | **passed 3/3** |
| Affected profile | same project, `--filter "Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState"` | 424 passed, 86 skipped, 0 failed | **424 passed, 86 skipped, 0 failed (510 total)** |
| Solution build | `dotnet build Gagamba.sln -c Release --nologo` | 0 warnings, 0 errors | **Build succeeded; 0 warnings, 0 errors** |
| Red T03b | disposable checkout `9bafb411...` + uncommitted D6.8.4a test patch | fails at `IsLive` assertion | **reproduced (line 196, Expected False / Actual True)** |

Passing and skipped counts match exactly; no test was unavailable. Skips are
the known Linux-only classes on a Windows host. Windows-hosted execution does
not establish Linux-native qualification.

## 8. Remaining findings

None. Both historical findings are closed by D6.8.4:

- **F-01 (non-blocking, historical): CLOSED.** Evidence: new
  `C2_T03b_LiveCapabilityOverflowStopConsumesCapability` plus reproduced
  red/green executions. No counterexample remains.
- **F-02 (non-blocking, historical): CLOSED.** Evidence: corrected section 8,
  coverage map and addendum 12.2. No overstatement remains.

No new findings were introduced by D6.8.4. The diff is test-only and
evidence-only; nothing in it weakens an existing assertion, masks a failure or
expands authority. No C2 production invariant was contradicted; no
counterexample exists.

## 9. Linux qualification limitations

- All D6.8.4 test executions, including this review's reproductions, are
  Windows-hosted. Linux-only test classes are skipped on this host.
- No native Linux runtime qualification was performed by the producer in
  D6.8.4 or by this review. Linux runtime qualification remains outstanding.
- Owner integration, G3 and release acceptance remain outstanding. Release is
  not qualified.

## 10. Final disposition

Both F-01 and F-02 are independently verified as closed with production source
unchanged (`src/` tree `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` throughout).

`WP05 D6.8.4 GLM INDEPENDENT F-01/F-02 CLOSURE REVIEW COMPLETE - OPTION A: F-01 LIVE-CAPABILITY OVERFLOW REGRESSION VERIFIED; F-02 EVIDENCE PRECISION CORRECTED; BOTH FINDINGS CLOSED; PRODUCTION SOURCE UNCHANGED; LINUX RUNTIME QUALIFICATION OUTSTANDING; RELEASE NOT QUALIFIED`
