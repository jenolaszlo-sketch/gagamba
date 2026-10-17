# WP05 v3 Durable Schema Implementation

Date: 2026-10-17 (Asia/Manila)

Remediation slice: WP05 v3 durable ownership schema foundation. This is an
implementation/evidence tranche, not final WP05 qualification and not release
transport.

Baseline source SHA: `84a6aac92647b1c09bd729fe6171d62b34063047`
Implementation SHAs: `3ffb843f5382e914b7d51325644424f98a361c00` (schema/codec),
`959bac1fad1c3ec8a5a5834ceb94712461f60edb` (transitions/recovery)
Branch: `qualification/ar7-feasibility`
Host: Windows development host; WSL2 Ubuntu used only for focused Linux fence and
owner-protocol regressions
Architecture: x86_64
Runtime: .NET 10.0.112
Native prerequisites exercised: existing Linux `flock` journal fence and existing
owner protocol tests; no target, coordinator RELEASE or delegated cgroup
qualification in this tranche
Qualification scope: portable schema/codec/transition/journal/recovery tests and
focused existing Linux journal-fence/WP04 owner-protocol regressions
Explicit exclusions: release transport, release arbitration wiring, coordinator
RELEASE, native post-release behavior, owner release orchestration, target or
fixture execution, WP05 full qualification, WP10, seccomp changes

## 1. Starting binding

Verified branch, exact starting HEAD `84a6aac`, clean tree, accepted ADR 0004
revision 2, complete Luna review preservation, clarification rereview, and no
prior WP05 schema implementation. WP04 remained accepted; G2 remained accepted;
WP05/WP10 remained unqualified.

## 2. Files changed

Implementation commit `3ffb843`:

- `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipRecord.cs`
- `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipValidation.cs` (new)
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationSpecification.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryTestSupport.cs`
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/OwnershipV3CodecTests.cs` (new)

Implementation commit `959bac1`:

- `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs`
- `src/Gagamba.Execution.Linux.M1/Recovery/OwnershipTransitions.cs` (new)
- `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs`
- `src/Gagamba.Execution.Linux.M1/Recovery/ReleaseRecoveryClassification.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/OwnershipV3TransitionTests.cs` (new)
- `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/ReleaseRecoveryClassificationTests.cs` (new)

The only WP04 production change is the explicit `LegacyVersion` constant in the
two pre-release snapshot constructors. Ordinary HELD/abort remains on version 2.

## 3. Schema version and complete v3 field inventory

Version 2 remains the legacy pre-release schema. Version 3 adds an optional tail
after the existing version-2 fields:

| Field | Representation |
|---|---|
| Release tag | `0=Absent`, `1=Intent`, `2=Committed` |
| Intent fields | `IntentAt`, `SetupDeadline`, `OverallDeadline` (absolute monotonic ns) |
| Committed fields | `CommittedAt`, `DeliveryResult` |
| Stop tag/fields | presence, `StopAcceptedAt`, `StopCause`, `StopDeadline` |
| Exec tag/fields | `ExecTag`, `FailedBeforeExec.errno` or `Confirmed.basis` |
| Root tag/fields | presence, `RootKind`, `RootExitCode`, `RootSignal`, `RootCoreDumped`, `RootRawWaitPresent`/`RootRawWaitStatus` |

`ReleaseRecord` carries the intent fields unchanged through Committed.
`RecordedPreparation` remains the only release identity; no release UUID/nonce was
added. `BootId` is inherited unchanged; no second boot identifier exists.

## 4. Exact durable enum encodings

| Enum | Values |
|---|---|
| `ReleaseTag` | `0=Absent`, `1=Intent`, `2=Committed` |
| `DeliveryResult` | `0=NotAttempted`, `1=Delivered`, `2=WouldBlock`, `3=BrokenPipe`, `4=Partial`, `5=Unknown` |
| `StopCause` | existing `0=CallerStop` through `7=TransportFailure` |
| `ExecTag` | `0=NotAttempted`, `1=FailedBeforeExec`, `2=Confirmed`, `3=Unknown` |
| `ExecBasis` | `0=None`, `1=CoordinatorFrame`, `2=ExecutableObjectIdentity`, `3=ReviewedNormalExitRule` |
| `RootTag` | `0=NotObserved`, `1=Observed` |
| `RootKind` | `0=Unknown`, `1=NotStarted`, `2=Exited`, `3=Signalled` |

The decoder rejects unknown/reserved values and invalid presence/field
combinations; no compiler ordinal is relied on. Exact v3 tail bytes are asserted
for all tags, `DeliveryResult`, `StopCause`, `ExecBasis` and `RootKind`.

## 5. v2 compatibility and lazy v2→v3

- v3 code accepts valid v2 snapshots; v2 fields retain their exact meaning and no
  release/stop/exec/root fact is inferred.
- A representative v2 record has a fixed golden SHA-256:
  `5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE`.
- Unsupported versions fail closed even with a valid checksum.
- The encoder selects v2 when all new facts are absent and v3 on the first WP05
  fact; no bulk rewrite of historical records was introduced.
- The pure transition test converts a complete v2 Prepared record to v3 and
  proves the execution identity, `BootId`, lease generation, preparation digest
  and allocation inventory are preserved; no implicit stop/observation/release
  fact is added.
- Ordinary WP04 `BuildIntent`/`BuildRegistered` paths explicitly use
  `LegacyVersion`.

## 6. Invariant matrix

Enforced at encode/decode and transition boundaries:

| Invariant | Result |
|---|---|
| Release/observation requires `Stage=Prepared` and `Preparation` | Refused otherwise |
| Release preparation execution identity matches snapshot execution identity | Refused otherwise |
| Intent has no committed timestamp and `DeliveryResult=NotAttempted` | Refused otherwise |
| Committed has `CommittedAt >= IntentAt` and a concrete delivery result | Refused otherwise |
| Intent + exec/root observation | Refused |
| Observations require `ReleaseTag=Committed` | Refused otherwise |
| Exited/Signalled require `Exec=Confirmed` | Refused otherwise |
| NotStarted requires `Exec=FailedBeforeExec` | Refused otherwise |
| Root Unknown requires `Exec=Confirmed` or `Exec=Unknown` | Refused otherwise |
| FailedBeforeExec requires positive errno and no basis | Refused otherwise |
| Confirmed requires a valid basis and no errno | Refused otherwise |
| Delivery `WouldBlock`/`BrokenPipe`/`Partial` with Confirmed/FailedBeforeExec/known root | Refused; `Unknown` may coexist with validated status |
| `ReviewedNormalExitRule` without Exited root | Refused |
| Raw wait with known Exited/Signalled | Must match kind, code/signal and core flag |
| StopDeadline | Exactly `StopAcceptedAt + 5_000_000_000` ns |
| Second/different stop | Refused; first stop immutable |
| Observation regression/contradiction | Refused |
| Release intent/committed/deadline mutation | Refused |
| Fact disappearance or schema-version regression | Refused |

## 7. Monotonic snapshots, stale baseline and writer fencing

`OwnershipJournal` now has two full-snapshot update helpers:

- `WriteTransition(lease, expectedBaseline, next)` requires the expected baseline
  to encode byte-for-byte identically to the latest acknowledged snapshot, then
  validates the transition and writes exactly `baseline + 1`.
- `WriteMutation(lease, mutate)` reads the latest acknowledged snapshot under the
  live lease, applies one full-snapshot mutation, validates it, and writes
  `baseline + 1`. Cleanup progress uses this path.

Every write holds the existing `JournalWriterLease`/`flock`; stale baseline,
wrong sequence, changed lineage, dead/foreign lease and non-monotonic fact
changes fail closed. Exactly one release/stop/observation fact may change per
transition; cleanup-only progress is separate. Once durable, release intent,
commit, delivery, stop, exec and root facts cannot disappear. `Reconciler`
cleanup writes preserve all of them and use the latest baseline; a no-op repeat
is idempotent without a new write. No second journal, field-level write or
last-writer-wins merge was introduced.

## 8. Exec/root observation trust

The schema types document that the coordinator, not the owner, is the sole
target-reaper/executable-object observation source. The owner validates and
persists trusted coordinator-derived facts; the implementation does not wire the
future coordinator status protocol. Observations are evidence only: no release,
capability reconstruction, retry, target-execution authorization or fence
override is possible from the durable record. Recovery reads observations and
never invents them.

## 9. Recovery classification

`ReleaseRecoveryClassifier` is a pure durable projection:

- no release → `NotCommitted` / root `NotStarted`;
- caller stop before release → `CancelledBeforeRelease`;
- Intent/Committed → `MayHaveExecuted`; observations narrow exec/root/outcome;
- FailedBeforeExec → root `NotStarted` (even without a separate root record) and
  `FailedToEstablish`;
- Confirmed + Exited/Signalled → known root; aggregate `Succeeded`/`TargetFailed`
  requires separate domain-empty/capture-complete facts; a stop yields `Stopped`;
- Unknown exec/root remains `Unknown`;
- cleanup state is returned separately and never rewrites release/execution
  history.

## 10. StopDeadline and recovery takeover

`StopDeadline = StopAcceptedAt + 5_000_000_000 ns` is validated exactly. The
one-second final drain remains a runtime bound and is not persisted as the stop
deadline. A persisted stop is immutable; a recovery takeover cannot extend or
rewrite the first stop allowance. Expiry only bounds teardown and never
authorizes release or resend.

## 11. Tests and checks

| Check | Result |
|---|---|
| Windows full Linux M1 project | **315 passed, 156 skipped, 0 failed / 471** |
| New v3 codec tests | 19/19 |
| New transition/journal/fence tests | 17/17 |
| New recovery-classification tests | 11/11 |
| WSL focused `JournalFencingTests` | 6/6 |
| WSL focused `OwnerProtocolTests` | 21/21 |
| Focused WP04/journal Windows selection | 64/64 |
| Canonical manifest vector | 1/1; 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` |
| Solution build | 0 warnings, 0 errors |
| v2 golden record | SHA-256 `5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE` |

The 156 skips are existing Linux/native tests unavailable on the Windows host.
No new test was skipped. No target/fixture execution occurred.

## 12. WP04 and G2 boundary

- WP04 production HELD/abort behavior is unchanged; only the two pre-release
  version constants were made explicitly legacy. The full portable suite and the
  WSL owner-protocol selection pass. Native WP04 HELD/owner-death tests were not
  rerun in this tranche because no WP04 native behavior changed; they remain the
  accepted WP04 evidence.
- G2 writer fencing uses the existing lease and real `flock`; the WSL focused
  fence tests pass 6/6. No G2 counterexample was produced, no unrelated native
  topology was requalified, and G2 was not reopened wholesale.

## 13. Limitations and deferred work

This tranche is **not** final WP05 qualification. Deferred:

- release pipe transport and release/stop runtime arbitration wiring;
- coordinator `RELEASE` reader/protocol and post-release status path;
- native post-release branch;
- owner release orchestration;
- integrated process-loss qualification and WP05 independent qualification;
- WP10 fixture target execution;
- deferred wrong-audit-architecture seccomp generator decision;
- non-root delegated native schema/journal qualification (not required for this
  portable schema tranche, but needed before release transport is connected).

No production defect, ADR defect, G2 counterexample or WP04 counterexample was
exposed. The implementation is not labelled qualified or accepted.

**WP05 V3 SCHEMA IMPLEMENTED - INDEPENDENT QUALIFICATION REQUIRED**
