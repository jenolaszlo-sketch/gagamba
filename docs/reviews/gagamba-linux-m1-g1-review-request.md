# WP01 G1 independent-review request

This document is a review request, not a review and not an acceptance
artifact. It indexes exact sources and evidence for the independent G1
reviewer. The author has implemented WP01 and performed this preflight; the
author has not independently accepted G1.

## Revision under review

- WP01 revision to review: the commit carrying this file (resolve with `git log -1 --format=%H`; delivery reports the SHA)
- Parent/base revision: `9b9264d` (plan HEAD before implementation)
- Branch: `qualification/ar7-feasibility`; worktree must be clean at review
- Changed-file inventory (13 files, +2809/−0 plus this request):
  `src/Gagamba.Execution/Restricted/RestrictedManifest.cs`,
  `src/Gagamba.Execution/Restricted/RestrictedTransaction.cs`,
  `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`,
  `src/Gagamba.Execution.Linux.M1/Preparation/PreparedTransaction.cs`,
  `src/Gagamba.Execution.Linux.M1/Preparation/PreparationStore.cs`,
  `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs`,
  `tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj`,
  `tests/Gagamba.Execution.Linux.M1.Tests/Contracts/ManifestTests.cs`,
  `tests/Gagamba.Execution.Linux.M1.Tests/PreparationIdentityTests.cs`,
  `tests/Gagamba.Execution.Linux.M1.Tests/StateModelTests.cs`,
  `Gagamba.sln` (additive project/config/nesting entries only),
  `docs/evidence/gagamba-linux-m1.md`, `docs/evidence/linux-m1/wp01.md`

## WP01 scope and non-scope

In scope: Section 5 value contracts, canonical preparation digest,
capability/store skeleton and pure Section 6 transitions with injected fake
clock/journal/transport. Out of scope: filesystem copy, P/Invoke, process
launch, real release transport, provider registration, native tests (recorded
NotApplicable), WP02+ work, public API or package changes. No existing file
was modified except additive solution entries.

## Section 5 contracts to implementation

| Plan contract | Implementation |
|---|---|
| RestrictedManifest + AuthorityManifest + ResourceManifest + ClosureIdentity + InvocationManifest + ChannelManifest + PreparationIdentity | `Restricted/RestrictedManifest.cs` (explicit fail-closed constructors, ordinal-sorted duplicate-rejecting storage, copied immutable arrays) |
| ReleaseDisposition, ExecDisposition, RootResult/HelperResult (separate kinds), ExecutionOutcome, StopCauseKind, CaptureResult, CleanupState, CleanupReport, TransactionState, InvariantViolation, TransactionObservation | `Restricted/RestrictedTransaction.cs` |
| Canonical encoding + digest | `ManifestCodec` in `RestrictedManifest.cs`: schema u16, u32-prefixed strict UTF-8, fixed-width LE integers, u8 kind tags, u32 counts, raw 32-byte digests, RFC UUID bytes |
| PreparedTransaction + nested PreparationStore | `Preparation/PreparedTransaction.cs` (sealed partial, private constructor, disposal invalidation) and `Preparation/PreparationStore.cs` (mint on issuer/generation/digest/expiry match; reference-equality consume; abort/dispose flags) |
| Pure transition model + fake dependency contracts | `Execution/TransactionStateModel.cs` (`PublishPrepared`, `MarkOwned`, `TryCommitRelease`, `AcceptStop`, `ObserveExec`, `ObserveRoot`, `ObserveHelper`, `ObserveCaptureTerminal`, `ObserveDomainEmpty`, `PublishTerminal`; `ITransactionClock`, `ITransactionJournal`, `IReleaseTransport`; `OwnedPrerequisites`; `TransitionAttempt`) |

## Section 6 transitions to methods and tests

| Plan transition | Method | Covering tests |
|---|---|---|
| Start → Preparing | Model constructor (starts Preparing) | `CompleteTransitionTable` initial state |
| Preparing → Prepared | `PublishPrepared` (live-capability check, journal) | Table legal/illegal, terminal immutability |
| Prepared → Owned | `MarkOwned` (four prerequisites, all required) | Table, prerequisite refusal |
| Owned → ReleaseCommitted | `TryCommitRelease` (binding/expiry checks, intent journaled before single transport write, gate recheck, consume+commit+send) | Success with intent-before-send ordering, expiry/foreign/digest/consumed/aborted refusals, reentrant-stop-wins, duplicate no-rewrite |
| ReleaseCommitted → Running | `ObserveExec(Confirmed)` | `ExecConfirmedMovesToRunning` |
| Any nonterminal → Stopping | `AcceptStop` (first cause stable, later causes bounded at 8, capability invalidated, terminal immutable) | Stability/bound tests, terminal refusal |
| Observation loss → Unknown | `PublishTerminal` decision table (see result map) | Unknown combinations, incomplete-capture, FailedBeforeExec, setup-fault terminals |
| Running → Completed | `PublishTerminal` (known root + empty + terminal capture) | Succeeded/TargetFailed/signalled variants |

## Canonical field order and discriminants

Order: schema u16, implementation ID, authority (namespaces, capabilities, noNewPrivs, filter digest, filter length, read-only destinations, writable stores, three denial flags), resources (13 budgets then inode qualification), closure (objects then helper dependencies), invocation (tool, mode, input, argv, environment, work directory), channels (stdin EOF, three limits). Discriminants are the `BudgetValueKind` byte (Absent 0 / Exact 1 / Unlimited 2) and the pinned enum bytes; there are no other optional fields. Duplicates are rejected for namespaces, roles, store paths and environment keys.

## Golden vector and cross-check

- Length 797 bytes, SHA-256 `dee322bcada586327661c5bcbc1c9673b8837e28354838ff9a53cfef01f22530`, pinned in `CanonicalGoldenBytes`
- Generated by an independent Python implementation of the same field order and hand-decoded field by field (recorded in `docs/evidence/linux-m1/wp01.md` section 3); the passing test proves both implementations agree byte-for-byte
- Guid RFC vector and full enum-number pinning in `EnumNumbersPinned` / `GuidRfcByteOrderVector`

## Capability mint/consume/abort ownership map

Only `PreparationStore.Mint` constructs capabilities (private constructor, no factory/clone/deserialize route). Consume path validates store membership, reference equality, issuer, generation, digest, expiry and unused state, in that order, then marks consumed. Dispose/Abort set a permanent aborted flag; abort-then-consume reports Aborted, never success. Duplicate consume reports Consumed. Fabricated objects (foreign store, disposed instance) report Foreign/Aborted.

## Result and Unknown separation map

Root, helper, capture and cleanup are independent records and fields; the terminal observation keeps them separate. Cleanup resolves Unknown in WP01 (only domain emptiness is observable; Confirmed requires all items). Known root coexists with Unknown cleanup (tested for exit 0, exit 17 and Unknown root). Unknown is never mapped to an exit code. Terminal publication is single-shot immutable.

## Test inventory and latest results

39/39 passed, 0 failed, 0 skipped (`dotnet test`, net10.0, xUnit v3; TRX at `artifacts/linux-m1/wp01/wp01.trx`): 12 contract, 9 identity, 18 state-model tests covering every Section 5 field/enum, golden bytes/hash, refusal codes, the full transition table, single-write arbitration, transport faults, reentrant stop, stop-cause stability, terminal combinations and waiter-cancellation isolation. Regression sweep of affected existing code: `Gagamba.Execution.Tests` 20/20 passed.

## Public API, package and reference diff

No public type added (all new types internal; verified by inspection). No package version changed (test project reuses 18.0.1/3.2.0/3.1.4). New references only: M1→Gagamba.Execution; tests→M1+Gagamba.Execution. New solution entries only. New InternalsVisibleTo entries only: Gagamba.Execution→M1+M1.Tests, M1→M1.Tests. Native: NotApplicable (no native tests exist or are claimed).

## Known limitations (for G1 to weigh)

- Single-threaded model and unbounded in-memory store; concurrency and eviction arrive with later packages.
- Cleanup components beyond domain-empty observation are unfilled by design in WP01.
- Duplicate release calls are refused as Consumed with the decision retained in model state (no second write); G1 may rule on this reading of the plan's duplicate-call sentence.
- Requested and established manifests in the terminal observation reference the same prepared object until WP06 provides a separate establishment report.
- Golden vectors pin one manifest shape; schema evolution requires a version change.

## Exact G1 acceptance criterion

Per plan Section 18, G1 (before WP02) requires: internal value/capability shapes and pure states match the ADR; immutable canonical identity; no public API/registration; WP01 has executable tests and no architectural placeholders.
