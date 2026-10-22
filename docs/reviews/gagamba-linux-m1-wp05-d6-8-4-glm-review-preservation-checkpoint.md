# WP05 D6.8.4 GLM F-01/F-02 Closure Review Preservation Checkpoint

Status: **PRESERVED - F-01 AND F-02 CLOSED BY INDEPENDENT GLM VERIFICATION;
PRODUCTION SOURCE UNCHANGED; LINUX RUNTIME QUALIFICATION OUTSTANDING; RELEASE
NOT QUALIFIED**

Preservation engineer: DeepSeek V4.1. This checkpoint preserves the D6.8.4 C2
evidence corrections and GLM's independent Option A finding-closure review as
immutable sibling histories. No production, test or evidence content was
changed during preservation.

## 1. Repository and base

- Repository: `C:\Users\Laszlos\source\repos\Gagamba`
  (origin `https://github.com/jenolaszlo-sketch/gagamba.git`).
- Preservation base (D6.8.4 producer evidence commit):
  `01da5d38a60a6f369c45a87f5bb18e2d8240081c`.
- Preservation worktree:
  `C:\Users\Laszlos\source\repos\Gagamba-preserve-d684`.
- Preservation branch:
  `preservation/wp05-d6-8-4-glm-f01-f02-closure-review`.

## 2. D6.8.4 producer commit chain

| Commit | Role | Immediate parent | Tree |
|---|---|---|---|
| `7177b3b83658de74b795de0c072589c590119c0a` | D6.8 preservation (base) | `665b18fdca15f8ed30ae4ca93b490f29bf41a181` | `168c532ee35914a9028e89e7afc67644245482d2` |
| `0aa201c3c5689690217ff5ae429de0bcda6943ff` | D6.8.4a live-overflow regression (test commit) | `7177b3b83658de74b795de0c072589c590119c0a` | `f05c4065f2db9dd15e1fed531d53293fdb9c2f81` |
| `01da5d38a60a6f369c45a87f5bb18e2d8240081c` | D6.8.4b evidence correction | `0aa201c3c5689690217ff5ae429de0bcda6943ff` | `19d1241eb556f4452b38230e594d5abdb0a0f73a` |

Producer branch: `evidence/wp05-d6-8-4-c2-f01-f02`. The producer final tree is
`19d1241eb556f4452b38230e594d5abdb0a0f73a` (verified as the tree of
`01da5d38...` from the commit object itself).

## 3. GLM review commit and blob

- Branch: `review/wp05-d6-8-4-glm-f01-f02-closure`.
- Review commit: `55f3f7a10b4d6860b8e7e02776bb3da73f801db9`.
- Immediate parent: `01da5d38a60a6f369c45a87f5bb18e2d8240081c` (verified from
  the commit object).
- Review tree: `7a9d6ae7e6254b81af4300f8ab6a78d9efc804e6` (verified from the
  commit object).
- Review document:
  `docs/reviews/gagamba-linux-m1-wp05-d6-8-4-glm-f01-f02-closure-review.md`.
- Review document blob:
  `22f6998cca7a3dee5453f2bc932f7ca89116c4cf` (captured before this
  preservation commit was written).
- Review commit contents: exactly one file, the review document, +244/-0.

**Tree-identity note.** The D6.8.4 preservation assignment stated the review
tree as `e46ae4acc9359f3fe3c73782d03039520b9c4e37`. That value is incorrect:
`e46ae4ac...` is the unrelated working-tree HEAD tree of the canonical
checkout at review time, which the GLM review report mislabeled as the review
commit's tree. The authoritative tree of `55f3f7a1...`, read directly from the
commit object (`git cat-file commit`), is
`7a9d6ae7e6254b81af4300f8ab6a78d9efc804e6`. This is a reporting error only;
the review commit, its parentage, its contents and its blob are intact and
verified. Git history was not rewritten.

## 4. Verified sibling ancestry

```text
7177b3b8  D6.8 preservation
    |
0aa201c3  D6.8.4 live-overflow regression
    |
01da5d38  D6.8.4 evidence correction
    |
    +---- 55f3f7a1  GLM closure review        (sibling A)
    |
    +---- NEW       D6.8.4 preservation       (sibling B, this commit)
```

Both siblings are direct children of `01da5d38...`; neither is an ancestor of
the other. No merge, rebase, amend or cherry-pick was performed.

## 5. GLM disposition (verbatim)

`WP05 D6.8.4 GLM INDEPENDENT F-01/F-02 CLOSURE REVIEW COMPLETE - OPTION A: F-01 LIVE-CAPABILITY OVERFLOW REGRESSION VERIFIED; F-02 EVIDENCE PRECISION CORRECTED; BOTH FINDINGS CLOSED; PRODUCTION SOURCE UNCHANGED; LINUX RUNTIME QUALIFICATION OUTSTANDING; RELEASE NOT QUALIFIED`

Confirmed present in the committed review document blob
`22f6998cca7a3dee5453f2bc932f7ca89116c4cf` (sections "Status" and "Final
disposition"), not taken from the producer's report alone.

## 6. F-01 CLOSED - live-capability overflow regression

Test: `C2_T03b_LiveCapabilityOverflowStopConsumesCapability`
(`Wp05D68C2StopCapabilityTests.cs`), verified construction: initial clock
`1_000`; capability expiry `long.MaxValue - 1` (minted before the clock
advance, satisfying `PreparationStore.cs:95-99`); stop clock
`long.MaxValue - 2_000_000_000L`; stop-confirmation allowance
`5_000_000_000` ns (`OwnershipRecord.cs:202`), so the checked deadline
addition overflows by 3e9 ns. Preconditions asserted in the test: expiry
exceeds the stop timestamp and `IsLive == true` immediately before
`RequestStop` (which also proves no prior consumption, since `IsLive` is a
store `TryLookup` refusing consumed entries). All required postconditions are
asserted: stop accepted; `DurableStop == false`; accepted-at preserved;
deadline zero; no durable StopRecord; journal sequence unchanged; no
ReleaseIntent; zero transport attempts; capability no longer live; subsequent
`TryConsume` refuses with `InvariantViolation.Consumed` - genuine consumption,
not expiry (the capability is unexpired post-stop and the store checks
`Consumed` before `Expired`, `PreparationStore.cs:184-188`).

Independently reproduced red/green evidence (GLM, Windows 10.0.26200 x64,
.NET SDK 10.0.401, net10.0 Release):

| Execution | Source | Result |
|---|---|---|
| Red | tests-first `9bafb411...` (pre-fix R12, `src/` `406f52da...`) + D6.8.4a test patch uncommitted | 42 focused tests: 30 passed, 12 failed; T03b failed at `Assert.False(h.Capability.IsLive)` (test line 196, Expected False / Actual True) because the capability remained live after stop |
| Green | `0aa201c3...` tests against unchanged D6.8 production (`src/` `84cfabb8...`) | 42 focused tests passed; T03b passed three consecutive repetitions |

The red failure is specifically missing stop-capability consumption; the only
variable between red and green is the production stop behavior.

## 7. F-02 CLOSED - duplicate-stop evidence precision

The corrected evidence document
(`docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md`, section 8
and addendum 12.2) now distinguishes: (a) the observed counting-clock behavior
(the duplicate stop performs the expected entry clock read and preserves the
original decision and deadline); (b) source-level ordering (the duplicate-stop
early return at `ReleaseArbiter.cs:232-235` precedes `ConsumeStopCapability()`
at `:237`, with no other reachable consume call site); and (c) the store
invariant (`PreparationStore` enforces single-use authority; `TryConsume`
refuses consumed entries at `:184-188` and `TryLookup` short-circuits at
`:153-157`, both before their clock reads). The corrected text states
explicitly that the counting clock does not directly count `TryConsume`
invocations and attributes exactly-once consumption to source order. GLM's
committed review explicitly accepted the corrected wording (F-02 verdict:
CLOSED).

## 8. No new findings

GLM reported zero new findings. The D6.8.4 diff is test-only and
evidence-only; nothing was deleted or weakened, no assertion was masked, and
the historical D6.8 GLM review (blob `dee4ca9244f5a8c5613152b9a3a9a90a96294ed2`)
correctly records F-01/F-02 as open at that time and remains unchanged.

## 9. Unchanged production source identity

The production `src/` tree is `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` at
all four commits: D6.8 preservation base `7177b3b8...`, D6.8.4 test commit
`0aa201c3...`, D6.8.4 evidence commit `01da5d38...`, and GLM review commit
`55f3f7a1...`. No D6.8.4 production change exists.

## 10. Exact producer change scope

`git diff 7177b3b8... 01da5d38...`: exactly three files, +162/-15.

| File | Lines |
|---|---|
| `docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md` | +130/-11 |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs` | +31/-3 |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` | +1/-1 |

No production, build, packaging, CI, journal, schema or native files changed.

## 11. Independently reproduced test results

Windows-hosted (Windows 10.0.26200 x64, .NET SDK 10.0.401, net10.0 Release),
reported by GLM from independent reproduction:

- Focused arbiter/probe/C2 selection: 42 passed, 0 failed (red run at
  `9bafb411...` + D6.8.4a patch: 42 total, 30 passed, 12 failed, T03b among
  the failures).
- T03b: passed three consecutive repetitions against D6.8 production.
- Affected profile (`Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState`):
  424 passed, 86 skipped, 0 failed (510 total).
- Solution build: 0 warnings, 0 errors.
- Red T03b failure detail reproduced exactly (line 196, `IsLive` assertion).

All executions are Windows-hosted; Linux-only classes are skipped on this
host.

## 12. Linux-native qualification still outstanding

No native Linux runtime qualification has been performed or claimed for C2 or
the D6.8.4 corrections. The updated `D5_CR3b` expectation and all other
Linux-specific classes remain skipped on the Windows host and unqualified.

## 13. Owner integration, G3 and WP05 release boundaries

The preserved evidence supports: human-approved C2 implementation; independent
source-level review; deterministic Windows-hosted regression testing; genuine
live-capability overflow discrimination; accurate duplicate-stop evidence; no
blocking GLM findings. It does not establish: native Linux C2 qualification;
complete process-level stop/recovery qualification; WP08 production Owner
integration; WP10 execution integration; full G3 acceptance; complete WP05
release acceptance; deployment readiness. C1, C3 and C4 contracts remain
human-approved but their production implementation is not authorized by this
preservation. Release is not qualified.

## 14. Next proposed step

**D6.8.5 - C2 native Linux qualification**: run the corrected source/test
state - including the live-overflow regression `C2_T03b` and the previously
skipped Linux-specific tests - under the approved Linux qualification plan on
native Linux. Not started and not authorized by this checkpoint. Thereafter,
C1 - Ambiguous ReleaseIntent (R09) remains the next proposed implementation
tranche, requiring its own separate explicit authorization.
