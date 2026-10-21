# WP05 D6.2 Precision Amendment Reconciliation

Status: **PROPOSED - NOT APPROVED**
Design status: **DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW**
Human decisions: **PENDING HUMAN**
Production remediation: **NOT AUTHORIZED**
Release qualification: **NOT ESTABLISHED**

This register reconciles the D6.2 documentation-only revision against the
independent GLM D6.1 review at
`77f559a7dfc1702b33812d5c3d5c42ac925f0343`. The four findings are carried from
`docs/reviews/gagamba-linux-m1-wp05-d6-1-independent-contract-design-review.md`
section 10 and the amendment matrix at
`docs/reviews/gagamba-linux-m1-wp05-d6-1-amendment-review-matrix.md`.
`DESIGN TEXT ADDRESSED` means the proposed D6.2 text now contains the
correction; it does not mean GLM verified it, any contract is approved, any
remediation is authorized or any release gate is accepted.

Binding: R12 production
`38f6981b2a60daab2df37c92913a6760c1bef131`, tree
`6854675d6b37a49d5302d6104103bdbfbc268040`; D6.1 preservation base
`d39dab60152a6f83724760b3122f61d6cf928be3`.

## R1 - structured reconciliation outcome discrimination

**Exact GLM finding.** D6.1 correctly names the Failed / Unknown-quarantine /
Confirmed / Refused categories but does not name a mechanism that can decide
which occurred. `StopOutcome` collapses kill-write failure and population
observation failure into `Stopped=false, TimedOut=false` (`DomainLease.cs:11-18,
108-143`), and the aggregate maps final persistence failure to `Failed`
(`Reconciler.cs:465-471`) although the proposed contract requires
Unknown/quarantine after otherwise confirmed cleanup.

**Source review document / section.** GLM main review section 10 (D6.1-R1);
amendment matrix row A-C3-2; human approval readiness C3(a)/(b).

**R12 source evidence.** `DomainLease.StopOutcome` and
`StopAndAwaitEmpty` (`Lifecycle/DomainLease.cs:11-18,98-143`),
`DomainPopulationResult` (`Lifecycle/RestrictedDomain.cs:15-21`), Reconciler
DomainStop/DomainEmpty mapping (`Recovery/Reconciler.cs:217-287`), aggregation
and JournalFinalization (`Recovery/Reconciler.cs:423-471`).

**Previous D6.1 gap.** No typed discrimination; error-string classification was
forbidden but no replacement was specified; no explicit JournalFinalization
aggregation row.

**D6.2 proposed correction.** C3 §D6.2-R1 selects R1-A: extend the internal
`StopOutcome` with `KillDispatchOutcome` and `StopPopulationObservation`
(plus `TimedOut` and diagnostic `Error`), map the Reconciler components from
those typed fields, and add the complete aggregate truth table. `Failed` is
reserved for a definitive operation failure; attempted-but-unconfirmed,
population-unknown and kill-unknown are `Unknown`; identity/authority refusal
is `Refused`; an otherwise successful cleanup whose only failure is durable
finalization is `QuarantinedUnknown`. A component `Failed` takes precedence
over a persistence failure so negative evidence is not lost. R1-B (separate
kill/observation operations) is rejected as larger and duplicative.

**Affected files.** `docs/decisions/wp05-d6-0-c3-stop-deadline.md` §D6.2-R1;
`docs/decisions/gagamba-linux-m1-wp05-d6-0-evidence-and-gate-matrix.md`
D6.2 rows; this register. Future production scope (not authorized):
`Lifecycle/DomainLease.cs`, `Recovery/Reconciler.cs`.

**Acceptance-test obligations.** R1-T1..T12 in C3 §D6.2-R1: typed kill failure;
kill attempted/populated; kill unknown; population unavailable; confirmed
empty; stop-progress persistence failure; final persistence failure after
confirmed empty; genuine operation failure; invalid authority; identity
mismatch; mixed components; repeated uncertain finalization. Use typed fake
domain/journal seams, not error text or sleeps.

**Cross-contract implications.** C1's "no fabricated deadline" and C3's
no-record rule are preserved. R1 does not change C2 consumption, C4 retention
or the journal schema.

**Remaining human decisions.** C3 contract approval (subject to focused
review); C3 expired-deadline policy choice remains separate and pending.

**Status.** `DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW`.

## R2 - structurally corroborated monotonic clock provenance

**Exact GLM finding.** `MonotonicEpoch.Qpc` as an enum label does not prove the
timestamp origin. A construction-relative `Stopwatch.StartNew()` clock could
claim `Qpc` while returning incompatible values. The contract must bind epoch
and conversion to a trusted primitive, with behavior-based compatibility and
rejection tests.

**Source review document / section.** GLM main review section 10 (D6.1-R2);
amendment matrix row A-C3-1; human approval readiness C3.

**R12 source evidence.** `OwnerClock` uses `Stopwatch.GetTimestamp()` with the
exact conversion (`Lifecycle/OwnerPreparationTransaction.cs:21-32`);
`LaunchDomainClock` uses the same primitive/conversion
(`Launch/BubblewrapAdapter.cs:510-521`); `OwnerDomainClock` uses
construction-relative `Stopwatch.StartNew()` (`OwnerPreparationTransaction.cs:34-39`)
and has no identified production caller at R12 (dead code); StopRecord
validation (`Recovery/OwnershipValidation.cs:145-175`); cross-boot refusal
(`Recovery/Reconciler.cs:75-97`).

**Previous D6.1 gap.** Enum-only corroboration; no sealed implementation or
frequency binding; no explicit definitional scoping of the `Qpc` name.

**D6.2 proposed correction.** C3 §D6.2-R2 requires a sealed internal
`StopwatchDomainClock` with a constant
`MonotonicClockBasis(MonotonicEpoch.Qpc, Stopwatch.Frequency)`, using the shared
`OwnerClock` conversion and no injected epoch. The Reconciler rejects
incompatible epoch/frequency before subtraction and fails closed to
Unknown/quarantine; no fabricated window. `Qpc` is defined as the
`Stopwatch.GetTimestamp()` epoch with the exact conversion, Windows QPC or
Linux CLOCK_MONOTONIC-backed Stopwatch, comparable only within the same basis
and boot context. `OwnerDomainClock` remains incompatible and dead code; no
active production failure is claimed.

**Affected files.** `docs/decisions/wp05-d6-0-c3-stop-deadline.md` §D6.2-R2;
evidence matrix D6.2 rows; this register. Future production scope (not
authorized): the clock implementation and `IDomainClock` basis.

**Acceptance-test obligations.** R2-A compatible epoch; R2-B incompatible
relative epoch rejected before subtraction; R2-C false enum-only provenance /
sealed-clock source invariant; R2-D cross-boot refusal; R2-E frequency,
overflow, nonpositive, unavailable and incompatible-epoch failure; R2-F
repeated reconciliation/new process/partial cleanup without deadline reset.

**Cross-contract implications.** R2 prevents a fabricated remaining deadline
(C1×C3); clock rejection cannot revive release authority (C2×C3); C4 duplicate
retention is unaffected. No new durable field or schema.

**Remaining human decisions.** C3 contract approval and C3 expired-deadline
policy. No clock implementation is authorized.

**Status.** `DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW`.

## R3 - ambiguous retention versus concurrent stop ordering

**Exact GLM finding.** D6.1 states ambiguous retention unconditionally. The
after-intent `_gate` block checks a retained release first and
`_stopAccepted` second (`ReleaseArbiter.cs:120-131`), so a stop can win between
the `_journalGate` release and the ambiguous caller's retention. The ambiguous
caller may then return a stop-won refusal or `Duplicate` of a prior retained
result, not its own ambiguous result. D6.1 did not specify this.

**Source review document / section.** GLM main review sections 3/10 (D6.1-R3);
interaction register Pair 1; amendment matrix row A-C1-1.

**R12 source evidence.** Entry and after-intent gates
(`Execution/ReleaseArbiter.cs:48-62,118-131`), journal intent phase
(`:81-115`), stop phase (`:226-296`), release-first retention
(`:148-183,215-223`), sealed journal (`Recovery/OwnershipJournal.cs:424-490`),
C2 store-local consume (`Preparation/PreparedTransaction.cs:60-69`).

**Previous D6.1 gap.** Unconditional retention claim; no stop-first or
already-retained returned-result rule; no barrier expectations for the
collision.

**D6.2 proposed correction.** C1 §D6.2-R3 specifies three orderings:
ambiguity-first (retain `AmbiguousPersistence`, tail stop, later Duplicate);
stop-first (stop is authoritative, C2 consumes, ambiguous caller retains/returns
the stop-won `Refused(MayHaveExecuted, NotAttempted, DurableCommitted=false)`
or Duplicate of a prior retained result); already-retained (Duplicate of the
prior result, no overwrite). The stop-won refusal never asserts absent durable
Intent; the sealed journal may still recover to `MayHaveExecuted`; no second
Intent/FD4, no new waiting protocol, no reentrancy assumption, no journal I/O
under `_gate`.

**Affected files.** `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md` §D6.2-R3;
`docs/decisions/wp05-d6-0-c2-stop-capability.md` D6.2 cross-reference;
`docs/decisions/wp05-d6-0-c4-duplicate-semantics.md` D6.2 precision note;
evidence matrix D6.2 rows; this register; focused handoff.

**Acceptance-test obligations.** R3-T1..T8: ambiguity-first; stop-first;
already-retained; concurrent committed writer opposing order;
clock-unavailable stop collision; durable-stop persistence failure; repeated
release after winner retained; owner death after ambiguous persistence. Each
row specifies barrier placement, expected winner/kind, durable classification,
capability state, FD4 count, journal state, recovery/no-resend and no
lock-inversion/deadlock assertion.

**Cross-contract implications.** C1×C2: C2 consumption at first stop
acceptance; C1×C4: sequential Duplicate of stop-won refusal; C2×C4: stop-first
and release-first history preserved; C1×C3: no fabricated deadline. No
second physical RELEASE.

**Remaining human decisions.** C1 contract approval after focused review.
C2/C4 remain supported for human decision, not reopened.

**Status.** `DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW`.

## R4 - diagnostic test-fixture source provenance

**Exact GLM finding.** D6.1's consumer inventory cited
`Wp05ReleaseContractProbes` as though it existed in frozen R12. It exists only
on `implementation/wp05-release-qualification-fixtures` at `dc4c372` (test-only;
`src` identical to R12). The inventory must distinguish R12 production symbols
from the fixture asset and bind the exact source.

**Source review document / section.** GLM main review section 10 (D6.1-R4);
amendment matrix row A-C1-2; preservation checkpoint section 10.

**R12 source evidence.** The fixture path is absent from R12 tree
`6854675d...`; the fixture branch's `src` tree
`406f52da43b62920128151b2f5706c32d45b6db7` equals R12 `src`, and its `native`
diff is empty.

**Previous D6.1 gap.** False production attribution; no branch/commit/file/blob
binding in the consumer inventory.

**D6.2 proposed correction.** C1 §D6.2-R4 records the exact test-only binding:
branch `implementation/wp05-release-qualification-fixtures`; test commit
`dc4c372b180abef1b27038d94f49af90226fa548`; evidence tip
`338a98af269e6b5839f3ccc2e0881f016153b01e`; file
`tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`;
blob `ccde61763664a4ed56e6d1941c20d61cc0cf2b5f`; SHA-256
`cd5cd00c4c4ef213239047a85ed8f26129e45bb5cad0ddd2c3fe646a407da2b4`;
artifact `SHA256SUMS` and `source-head.txt` bindings; the 13 test method
names. The inventory is split into R12 production consumers and test-only
fixture assets. No fixture is copied or merged.

**Affected files.** `docs/decisions/wp05-d6-0-c1-ambiguous-intent.md` §D6.2-R4;
evidence matrix D6.2 rows; this register.

**Acceptance-test obligations.** Binding inspection only: exact fixture branch,
full commit, file path, blob and SHA-256; `src`/`native` comparison to R12;
evidence manifest binding; no false ancestry. No test is run or changed.

**Cross-contract implications.** R4 is provenance-only. It does not change C1,
C2, C3 or C4 semantics. The fixture remains diagnostic evidence for R12
behavior, not a production symbol or future qualification.

**Remaining human decisions.** None separately; C1 approval includes the
corrected inventory binding.

**Status.** `DESIGN TEXT ADDRESSED - AWAITING INDEPENDENT REVIEW`.

## Cross-contract consistency after D6.2

The six D6.1 interactions remain covered:

| Pair | D6.2 effect |
|---|---|
| C1 × C2 | Stop-first ordering preserves C2 consumption at first stop acceptance; ambiguous caller's report is conditional; tail stop idempotent. |
| C1 × C3 | No new deadline; ambiguity/stop outcome does not fabricate a StopRecord or a fresh window. |
| C1 × C4 | Sequential Duplicate of stop-won refusal or retained ambiguous result; one physical FD4; no retry-to-second-write. |
| C2 × C3 | Clock/finalization uncertainty cannot revive release authority; C2 consumption is independent. |
| C2 × C4 | Stop-first and release-first history preserved; no new `ConcurrentRefusal` or waiting protocol. |
| C3 × C4 | Duplicate/reconciliation does not reset the immutable deadline; recovery classification remains separate. |

No new contradiction was found. No new durable record, schema change or
contract redesign is introduced.

## Remaining human decisions

- C1 contract approval: **PENDING HUMAN**, after focused D6.2 verification.
- C2 contract approval: **PENDING HUMAN**; GLM support unchanged.
- C3 expired-deadline policy: **C3 ZERO-WAIT POLICY - PENDING HUMAN**.
- C3 contract approval: **PENDING HUMAN**, after focused verification.
- C4 contract approval: **PENDING HUMAN**; GLM support unchanged.
- Implementation authorization: **PENDING HUMAN**, per tranche; no blanket
  authorization.

## Qualification boundaries

No production, test, native, engineering or fixture change is made by D6.2.
R12 production, OwnershipSnapshot v3, FD4/FD5 protocol contracts, frozen
harness Q3, D1-D5 evidence and prior independent reviews remain unchanged. No
new tests are run, no gate is accepted and release remains not qualified. H1-H5
and WP08/WP10 remain deferred.
