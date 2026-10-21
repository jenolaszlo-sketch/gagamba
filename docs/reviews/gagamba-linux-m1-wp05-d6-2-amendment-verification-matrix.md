# WP05 D6.2 Amendment Verification Matrix (GLM)

Status: **PROPOSED - NOT APPROVED**
This matrix records what was *verified* for the four D6.2 precision amendments at review time. It does not approve anything, does not authorize production work and does not close a gate. Scope is locked to the D6.2 amendments (D6.1-R1..R4 corrections); the earlier D6.0/D6.1 amendment rows are not re-listed here beyond what is required to answer the focused questions.

## 1. Binding

| Item | Value (verified) |
|---|---|
| Review input branch/HEAD | `design/wp05-d6-2-contract-precision-amendments` @ `ae09afedddeb703e6bde226bd793b375896c3064` |
| D6.2 diff base | `d39dab60152a6f83724760b3122f61d6cf928be3` (D6.1 preservation) |
| D6.2 commit chain | `d39dab60152a6f83724760b3122f61d6cf928be3` -> `d089f9382b89fc53ef4cbf1db28d1a40bf93baf9` -> `ab911d1deeb89b965f751dea8a9c1e82117bbcdf` -> `ae09afedddeb703e6bde226bd793b375896c3064` |
| R12 production | `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040` |
| Frozen harness Q3 | `36a124d7dff98ecfaf956340767fe1768ad5b17f` |
| D6.1 review tip | `77f559a7dfc1702b33812d5c3d5c42ac925f0343` (not an ancestor of the D6.2 branch - verified) |
| Fixture test commit / evidence tip | `dc4c372b180abef1b27038d94f49af90226fa548` / `338a98af269e6b5839f3ccc2e0881f016153b01e` (= branch tip) |
| This review's first commit | `01f4d8c61de635ffa947502b7a54513fc1c5e6f4` (document: `docs/reviews/gagamba-linux-m1-wp05-d6-2-focused-independent-review.md`) |

## 2. Main verification matrix

| Amendment | D6.2 promise (where) | Verification executed | Observed result | Verdict | Gap carrying forward |
|---|---|---|---|---|---|
| D6.1-R1 (structured outcomes) | Typed internal `StopOutcome` (`KillDispatchOutcome`, `StopPopulationObservation`, `TimedOut`), Reconciler maps from typed fields not `Error` text, complete component/aggregate truth table, explicit `JournalFinalization` row, precedence Failed > QuarantinedUnknown > CleanupConfirmed (C3 §D6.2-R1) | Read C3 §D6.2-R1 in full; re-read R12 `DomainLease.cs:11-18,108-143`, `RestrictedDomain.cs:15-22`, `Reconciler.cs:215-248,250-287,423-471`, `CgroupInterop.cs:174-201`, planner `Reconciler.cs:75-97`; confirmed R12 aggregation defects at `:429-436` (`finalizationFailed -> Failed`), `:450-459` (missing-persistence Unknown) and `:465-471` (final-write override to `Failed`); confirmed test seams `FakeDomainBackend`/`FakeDomainClock`/`FakeJournalStorage` exist in `RecoveryTestSupport.cs`; cross-checked R1-T1..T12 names across C3, register and matrix | Mechanism implementable and correct; one internal contradiction: mapping-table row 1 (`Kill=NotAttempted` -> unconditional `DomainEmpty = Unknown`) conflicts with the retained diagnostic-fallback paragraph and with row 2's conditional `DomainEmpty` resolution | **PARTIALLY ADDRESSED** | **D6.2-R1** (required, documentation-only) |
| D6.1-R2 (clock provenance) | Sealed internal production clock on `Stopwatch.GetTimestamp()` + exact `OwnerClock` conversion, constant `MonotonicClockBasis(MonotonicEpoch.Qpc, Stopwatch.Frequency)`, no injected epoch, rejection before subtraction, fail-closed to `QuarantinedUnknown`, honest `Qpc` naming, R2-A..F behavior tests (C3 §D6.2-R2) | Read C3 §D6.2-R2 in full; verified conversion identity at `OwnerPreparationTransaction.cs:21-32`; verified `OwnerDomainClock` construction-relative epoch + dead code (single `git grep` hit = declaration); verified `LaunchDomainClock` same primitive/conversion (`BubblewrapAdapter.cs:510-521`); verified validator immutability bounds (`OwnershipValidation.cs:145-175`) and transition immutability (`OwnershipTransitions.cs:108-151`, "the first accepted stop is immutable" at `:117`); verified cross-boot refusal placement (`Reconciler.cs:75-97`); noted `InternalsVisibleTo` to the Owner assembly (`TransactionStateModel.cs:11-12`) | All structural and testable claims check out; the seam boundary remains a review-enforced invariant for internals-visible assemblies | **VERIFIED ADDRESSED** | None (one non-blocking disclosure recommendation) |
| D6.1-R3 (retention collision) | Three orderings (ambiguity-first / stop-first / already-retained); stop-won refusal retained in `_retainedRelease` with exact `Refused(MayHaveExecuted, NotAttempted, DurableCommitted=false, LiveExecutionExcluded=true)` field values and reason string `release gate refused: stop won during durable intent`; later callers `Duplicate`; separate `_retainedStop`; no second Intent/FD4; no journal I/O under `_gate`; first-retainer rules unchanged (C1 §D6.2-R3) | Read C1 §D6.2-R3 in full; re-read the arbiter at R12 (`:1-5, 23-27, 45-62, 64-79, 81-115, 117-132, 134-146, 148-155, 157-183, 185-223, 226-297, 299, 301-308`), `ReleaseDecision.cs` (enum + records), `PreparedTransaction.cs:60-69`; confirmed the stop-won branch retains (`:127-131`), the retained-Duplicate chain (`:50-52, :120-123`), the helper field values (`:301-303`), the `WriteTransition`-failure unretained return (`:110-114`), no journal I/O inside any `_gate` block, and `RequestStop`'s non-consumption at R12 (the fixture name `StopFirstLeavesCapabilityLiveOnR12` documents the composed-contract gap) | Ordering contract matches R12 exactly where it claims R12 behavior; consumption wording assumes C2 composition in one sentence (correct under C2, worth one clarifying clause) | **VERIFIED ADDRESSED** | None (one non-blocking wording recommendation) |
| D6.1-R4 (fixture provenance) | Exact test-only fixture binding: branch, test commit, parent, tree, file, blob, SHA-256, R12 `src`/`native` identity, evidence manifest blobs, 13 test names, no copy/merge (C1 §D6.2-R4) | Full Git identity verification: branch tip == evidence tip `338a98af...`; commit `dc4c372...` resolves; parent `dbfecada...`; tree `1f1c8669...`; file blob `ccde6176...`; SHA-256 recomputed on raw blob bytes via cmd redirect + `certutil` = `cd5cd00c...` exact; `git rev-parse dc4c372:src` == `git rev-parse 38f6981b:src` == `406f52da...`; `git diff --exit-code 38f6981b dc4c372 -- src native` rc=0; file absent from R12 tree; all four evidence blobs resolve at the tip (`e203b5c3...`, `e35e4764...`, `fd7dc44e...`, `03beaf88...`); 13/13 method names extracted and matched exactly; D6.2 diff contains no fixture file | Every identity binds to the byte and commit; attribution (test-only, not-R12, no-ancestry-claim) is accurate | **VERIFIED ADDRESSED** | None |

## 3. Test-obligation inventories (specification-level, not executed)

| Inventory | Source | Consistency check | Result |
|---|---|---|---|
| R1-T1..T12 (12 rows) | C3 §D6.2-R1 table, register R1 row, matrix "R1 required test row names" | Three-way name/order match; seam-only (`FakeDomainBackend`, `FakeDomainClock`, `FakeJournalStorage`); no sleep/error-text oracle; T9 authority split consistent with `RecoveryPlanner.Plan` and staging rules | SPEC-CONSISTENT (row-1 table defect handled by D6.2-R1; test rows unaffected) |
| R2-A..F (6 rows) | C3 §D6.2-R2 table, register R2 row, matrix names | Names match; behavior-based; no production extensibility added | SPEC-CONSISTENT |
| R3-T1..T8 (8 rows) | C1 §D6.2-R3 barrier table, register R3 row, matrix names | Barrier placements map onto existing `TestBarrier` points; expected winners/strings match verified R12 behavior for R3-B; negative assertions (no inversion, no journal I/O under `_gate`, no second FD4) consistent with verified lock structure | SPEC-CONSISTENT |
| R4 binding proof | C1 §D6.2-R4 table, matrix "R4 required binding proof" | All rows executed and passed during this review (see matrix row 4); "no false ancestry" handled as a tree-identity, not an ancestry claim | PROOF-EXECUTED |

No test was run by this review. The inventories remain future specifications for per-tranche authorized implementation.

## 4. Cross-contract pairs after D6.2

| Pair | Verified basis | Verdict |
|---|---|---|
| C1 (x) C2 | C2 D6.1 consumption section unchanged; C1 §D6.2-R3; arbiter `:125-131, :232-235`; `PreparedTransaction.cs:60-69` (store-local consume, release arbiter the only production caller) | CONSISTENT |
| C1 (x) C3 | No fabricated deadline/journal window anywhere in D6.2; C3 fail-closed rules; arbiter/Reconciler separation | CONSISTENT |
| C1 (x) C4 | Taxonomy strings verified byte-exact against the arbiter (`:55-60`, `:92-93`, `:128-131`, `:301-303`); sealed-journal `ReadLatest` error `commit seal present: durability ambiguous, explicit recovery required` at `OwnershipJournal.cs:780` matches the C4 note's interpolation | CONSISTENT |
| C2 (x) C3 | Clock/finalization uncertainty cannot revive release authority; consumption path independent | CONSISTENT |
| C2 (x) C4 | Both D6.2 sections explicitly non-reopening; retention asymmetry and histories preserved | CONSISTENT |
| C3 (x) C4 | Immutability verified (`OwnershipValidation.cs:145-175`, "the first accepted stop is immutable" `OwnershipTransitions.cs:117`); R1-T12/R2-F no-reset rows | CONSISTENT |

## 5. Findings carried into the register

- **D6.2-R1 (required, documentation-only):** reconcile C3's `Kill=NotAttempted` component row with the retained `ObservePopulation` fallback rule (prefer: `DomainEmpty` confirmed when the diagnostic observation proves empty, as in the `Kill=Failed` row). One sentence; the aggregate truth table is unaffected for other rows. Until this lands, C3's R1 design text is `PARTIALLY ADDRESSED - AWAITING D6.2-R1 CORRECTION`.
- Non-blocking recommendations (attach to future slices, do not gate): the C3 implementation slice should disclose the internals-visible assembly set (`TransactionStateModel.cs:11-12` includes the Owner assembly) when it restructures the Reconciler constructor; C1's R3-B case 4 should add one clause marking "consumed at first stop acceptance" as C2-contract (composed) semantics, since at R12 the release caller's `:127` consume is the first consume.

## 6. Status boundary

Nothing in this matrix approves C1-C4, selects the C3 policy, authorizes production remediation, runs a test, or accepts a gate. All gate rows in the D6.0 evidence/gate matrix remain diagnostic/blocked; release remains not qualified. The next bounded item is the D6.2-R1 documentation-only correction followed by the human approval questions in the D6.2 addendum.
