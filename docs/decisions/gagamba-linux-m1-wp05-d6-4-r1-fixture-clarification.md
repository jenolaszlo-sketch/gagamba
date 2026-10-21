# Gagamba Linux M1 — WP05 D6.4 R1-T13–T15 Deterministic Fixture Clarification

- Author: **Luna**
- Status: **PROPOSED DOCUMENTATION AMENDMENT — READY FOR FOCUSED GLM REVIEW**
- Approval: **C1–C4 PROPOSED - NOT APPROVED; C3 POLICY PENDING HUMAN**
- Implementation: **NOT AUTHORIZED**

## 1. Git and evidence binding

- Repository: `C:\Users\Laszlos\source\repos\Gagamba`; origin: `https://github.com/jenolaszlo-sketch/gagamba.git`.
- D6.4 branch: `design/wp05-d6-4-r1-fixture-clarification`; exact base and immediate parent: D6.3 preservation `004a12d62bd90bb11217e4e57fa8326f7afafecf`.
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`; tree `6854675d6b37a49d5302d6104103bdbfbc268040`; frozen harness Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D6.3 Luna producer: `4209581a03562b434badd7a5f9927663559911be`.
- D6.3 Terra review: `cda2f22221c4db12ced3a4317362e80167270a63`, direct child of Luna. Authoritative document: `docs/reviews/gagamba-linux-m1-wp05-d6-3-terra-independent-c3-review.md`, blob `3bb7fd2322670d9f9977004661ab2a72760798d4`.
- D6.3 preservation: `004a12d62bd90bb11217e4e57fa8326f7afafecf`, direct child of Luna and this D6.4 branch base.
- This handoff is part of the D6.4 commit on the named branch; that commit's immediate parent is the exact base above. Its full SHA is reported with the commit result.

## 2. Terra's finding

Terra's committed disposition is:

**WP05 D6.3 TERRA INDEPENDENT REVIEW COMPLETE - OPTION B: D6.2-R1 PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; HUMAN C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

In section 6, Terra accepted the D6.3 outcome expectations but found R1-T13–T15 did not specify how tests supply `KillDispatchOutcome.NotAttempted` or inject the fallback observations. Frozen R12 calls `WriteKill` before bounded population polling; setting only `FakeDomainBackend` population state cannot produce that typed stop outcome. Terra assigned no separate finding identifier, so D6.4 creates none.

The accepted mapping remains unchanged:

| Kill | Trusted fallback | DomainStop | DomainEmpty |
|---|---|---|---|
| `NotAttempted` | Empty | `Unknown` | `Confirmed` |
| `NotAttempted` | Populated | `Unknown` | `Unknown` |
| `NotAttempted` | Unknown/unavailable | `Unknown` | `Unknown` |

## 3. Source facts: R12 and proposed behavior

| Evidence | Exists in frozen R12 | D6.4 proposed fixture / future contract |
|---|---|---|
| Stop outcome | `Lifecycle/DomainLease.cs:11-18` has the old `StopOutcome(bool Stopped, bool TimedOut, string Error)` with `Success`, `Timeout`, and `Failure`; it has no typed kill-dispatch result. | The D6.2/D6.3 proposed `StopOutcome(KillDispatchOutcome, StopPopulationObservation, TimedOut, Error)` remains design-only. It is not an R12 constructor. |
| Stop and fallback | `DomainLease.cs:102-104,108-143` exposes `ObservePopulation()` separately; `StopAndAwaitEmpty()` calls `WriteKill()` at `:116` before reads at `:125`. `Recovery/Reconciler.cs:231` invokes the bounded stop. | The fixture substitutes one typed result at the Reconciler stop-result boundary and then lets the existing fallback call `liveLease.ObservePopulation()`. |
| R12 mapping | `Reconciler.cs:250-285` confirms and persists `DomainEmpty` for known Empty. In R12, known Populated is `Failed` at `:277-280`; unknown is `Unknown` at `:282-285`. | The proposed `NotAttempted` rows classify Populated and unavailable/inconclusive as `DomainEmpty=Unknown`. The tests target the future typed classifier; this is not claimed as R12 behavior. |
| Planning and identity | `Reconciler.cs:75-97,224-228` requires matching boot, a live authority lease, and matching retained identity before the stop boundary. | All three tests retain that live lease and identity. T15 uses `ReadFault=true`; it does not remove or mismatch the lease. |
| Aggregate and finalization | `Reconciler.cs:423-471` gives required-component failure precedence, then Unknown, and prevents unavailable/failed finalization from yielding confirmed cleanup. | The cases assert `QuarantinedUnknown` because `DomainStop` is Unknown, with healthy journal finalization confirmed. |
| Fake population and accounting | `Recovery/RecoveryTestSupport.cs:142-213` defines `FakeDomainBackend`: `Populated`, `ReadFault`, `KillAccepted`, and `KillWrites`; `ReadPopulation()` returns `Emptied`, `Populated`, or `Unknown`. `KillWrites` increments only after the disposed/fault checks. There is no read-call counter. | In the linked test-support source, add counters at method entry: `WriteKillCalls`, `ReadPopulationCalls`, and `RemoveChildCalls`. Assert zero, one, and zero respectively. No production backend counter is proposed. |
| Journal | R12 `TryPersist` at `Reconciler.cs:165-210` uses `WriteMutation` for an existing snapshot and can use `WriteNext` only when no baseline exists. Test support provides `FakeJournalStorage` and v3 snapshot helpers. | Seed a valid v3 baseline before reconciliation, so the cases exercise the existing mutation path, then observe `journal.ReadLatest()`. No new record kind or schema. |
| Friend access | `Execution/TransactionStateModel.cs:11-12` grants M1 internals to M1.Tests and M1.Owner; `Gagamba.Execution/Restricted/RestrictedManifest.cs:11-12` grants RestrictedManifest internals to normal M1 and M1.Tests. | The M1 test variant must grant internals only to `Gagamba.Execution.Linux.M1.D6_4.Tests` and omit Owner. Because the variant has a distinct assembly name, `RestrictedManifest.cs` needs a compile-time friend declaration for `Gagamba.Execution.Linux.M1.TestFixture`; that adds no runtime method or public API. |

R12's `DomainPopulationResult` is defined at `Lifecycle/RestrictedDomain.cs:15-21`: known Empty is `Emptied`, known non-empty is `Populated`, and unavailable evidence is `Unknown(error)`. `RecoveryPlanner` and identity checks are not bypassed by this fixture. The diagnostic `Error` value is never parsed or used as a success oracle.

## 4. Chosen test-only mechanism

**Proposed future test build only; not an R12 API and not implemented:** compile the M1 sources as a separate `Gagamba.Execution.Linux.M1.TestFixture` assembly with `GAGAMBA_WP05_D6_4_TEST_FIXTURE`, isolated `obj`/`bin` paths, and `IsPackable=false`. A dedicated `Gagamba.Execution.Linux.M1.D6_4.Tests` project references only that M1 variant (plus `Gagamba.Execution` and test packages), links the existing `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryTestSupport.cs` into its own test assembly, and does not reference/build Owner. The normal M1 and Owner projects keep their regular assembly and output paths; release/publish builds do not reference or package the fixture variant.

Because the proposed D6.2 typed `StopOutcome` and classifier are not in R12, the future authorized implementation must first supply that typed classifier. In the fixture build only, add a marker `NotAttemptedStopTestFixture` that accepts no arbitrary result, delegate, or payload and returns only:

```csharp
#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
internal sealed class NotAttemptedStopTestFixture
{
    internal StopOutcome CreateOutcome() => new(
        KillDispatchOutcome.NotAttempted,
        StopPopulationObservation.NotObserved,
        TimedOut: false,
        Error: "D6.4 test fixture");
}
#endif
```

Under the same compile symbol only, add a conditional Reconciler field and this test-only overload:

```csharp
internal Reconciler(
    IDomainClock clock,
    TimeSpan stopAllowance,
    NotAttemptedStopTestFixture fixture);
```

At the R12 stop-result boundary (`Reconciler.cs:224-231`), after `RecoveryPlanner.Plan()` and `liveLease.VerifyIdentity()` succeed, and only while DomainStop progress is unconfirmed, the fixture build selects `fixture.CreateOutcome()` instead of calling `liveLease.StopAndAwaitEmpty()`. In the future typed classifier this input must leave `DomainStop=Unknown`; the normal DomainEmpty fallback then calls the real `liveLease.ObservePopulation()`, which reaches the linked `FakeDomainBackend.ReadPopulation()`. The `Population=NotObserved` field records that the synthetic stop path did no population polling. The normal production build contains no fixture type, field, overload, branch, runtime flag, or injection API and always uses its real bounded stop.

The M1 test-variant assembly metadata grants internals only to `Gagamba.Execution.Linux.M1.D6_4.Tests` and omits `InternalsVisibleTo("Gagamba.Execution.Linux.M1.Owner")`; the normal M1 assembly retains its existing Owner friendship. `Gagamba.Execution/Restricted/RestrictedManifest.cs` currently grants internals to the normal M1 and M1.Tests assemblies. The separate `M1.TestFixture` variant also needs the compile-time-only friend declaration `InternalsVisibleTo("Gagamba.Execution.Linux.M1.TestFixture")` there so it can compile against `RestrictedManifest`. This metadata adds no runtime method or public surface. Normal Owner references the regular M1 assembly, not `M1.TestFixture`, and cannot construct the marker; the fixture variant is absent from its graph and from release artifacts. A future build check must confirm that the regular production M1 assembly contains no marker type or overload and that Owner resolves the regular M1 assembly.

The marker cannot inject Accepted, Failed, arbitrary Unknown, or a caller-authored result. It changes only the stop result for the three defensive cases; the ordinary planner, identity verification, live lease, fallback observation, aggregate, and journal paths remain active. The test-build variant and test-support counters are future implementation work. If the variant cannot be kept out of the Owner/release graph, or its Owner friend access cannot be excluded, this seam is unsafe and D6.4 must be held for a different reviewable design.
## 5. Deterministic R1-T13–T15 cases

Shared setup for every case:

1. Use a live, undisposed `DomainLease` whose backend handles are alive and whose full identity matches a v3 `OwnershipSnapshot` on the current boot. Preserve the regular `RecoveryPlanner.Plan()` and `VerifyIdentity()` path.
2. Use no recorded allocations and pass a test-local recording `IAllocationCleaner` whose call count must remain zero. Seed `RecoveryTestSupport.V3Snapshot(cleanup: OwnershipCleanupState.InProgress, completedCleanup: (byte)CleanupComponents.DomainRemoval)` with no allocations and `CompletedCleanup=DomainRemoval` only (`0x08`); leave `DomainStop` (`0x01`) and `DomainEmpty` (`0x02`) clear. This pre-existing removal bit avoids calling `RemoveIfIdentityMatches`; it is not set by the fixture or test run.
3. Acquire a healthy `JournalWriterLease` for `FakeJournalStorage`, seed the baseline before calling Reconcile, and pass the same journal and writer into Reconcile. Verify the final snapshot with `ReadLatest()` and confirm its codec version is unchanged.
4. Use the test-build-only `NotAttemptedStopTestFixture`. It returns `Kill=NotAttempted`, stop population `NotObserved`, `TimedOut=false`. The separate fallback read is supplied by the fake backend as specified below.

| Test | Fake backend fallback input | Component and aggregate results | Durable result and direct observations |
|---|---|---|---|
| R1-T13 | `Populated=false`, `ReadFault=false` → `DomainPopulationResult.Emptied` | `DomainStop=Unknown`; `DomainEmpty=Confirmed`; `JournalFinalization=Confirmed`; aggregate `QuarantinedUnknown` | `CompletedCleanup=DomainRemoval + DomainEmpty` (`0x0A`); `DomainStop` remains clear; `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed`. |
| R1-T14 | `Populated=true`, `ReadFault=false` → `DomainPopulationResult.Populated` | `DomainStop=Unknown`; `DomainEmpty=Unknown`; `JournalFinalization=Confirmed`; aggregate `QuarantinedUnknown` | `CompletedCleanup=DomainRemoval` only (`0x08`); `DomainStop` and `DomainEmpty` remain clear; `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed`. |
| R1-T15 | `ReadFault=true` → `DomainPopulationResult.Unknown(error)` while the live lease remains valid | `DomainStop=Unknown`; `DomainEmpty=Unknown`; `JournalFinalization=Confirmed`; aggregate `QuarantinedUnknown` | `CompletedCleanup=DomainRemoval` only (`0x08`); `DomainStop` and `DomainEmpty` remain clear; `WriteKillCalls=0`, `ReadPopulationCalls=1`, `RemoveChildCalls=0`; no `CleanupConfirmed`. |

For all three cases, no allocations make Staging confirmed without a cleaner call; the seeded DomainRemoval bit skips removal. Assert the existing v3 record was mutated, its version and schema stayed the same, unrelated fields stayed unchanged, and cleanup remains Unknown. T13 adds only the truthful DomainEmpty progress bit. T14/T15 add neither domain bit. The healthy writer makes journal finalization confirmed; unavailable population evidence is not a journal failure.

Negative assertions for every case:

- No `WriteKill()` invocation or accepted kill (`WriteKillCalls=0`; retain existing `KillWrites=0`).
- Exactly one fallback `ReadPopulation()` call; no bounded polling, delay, retry, or deadline reset (`FakeDomainClock.Delays=0`, clock value unchanged).
- No `RemoveChild()` call and no staging-cleaner call; no real/native backend is constructed.
- No new DomainStop confirmation bit, no fabricated kill acceptance, termination, release authority, or completion claim.
- Never infer success from the fixture's diagnostic `Error` string.
- Never return `CleanupConfirmed`; with `DomainStop=Unknown`, aggregate remains `QuarantinedUnknown`.

The R12 `ReconcileResult.DestructiveActionTaken` flag is set before the bounded-stop call on the live cleanup path; it is not a syscall counter. These tests use the fake backend's direct operation counters as the no-side-effect oracle and do not redefine that flag.

## 6. Scope and approval boundaries

D6.4 changes only the R1-T13–T15 setup/assertion text and this handoff. It does not change the C3 outcome mapping, R1-T1–T12, R2 clock provenance, or expired-deadline policy. C1, C2, and C4 are not reopened. All C1–C4 remain `PROPOSED - NOT APPROVED`; C3 remains `C3 ZERO-WAIT POLICY - PENDING HUMAN`. No implementation, production remediation, test execution, G3 acceptance, or runtime qualification is authorized or claimed. H1–H5, WP08, WP10, full G3, and portability qualification remain deferred. Release is not qualified.

## 7. Focused GLM review questions

1. Does the exact R12 source support the documented stop-first, retained-lease fallback, identity, fake-backend, journal, and aggregation statements? Is the R12 Populated=`Failed` behavior clearly separated from the proposed D6.3 mapping?
2. Does the dedicated test-build assembly, compile-time-only marker fixture, and conditional friend metadata keep synthetic outcomes inaccessible to the normal production assembly and Owner callers, including the known `InternalsVisibleTo` arrangement?
3. Does the synthetic result enter at one unambiguous Reconciler stop-result boundary after planning and identity verification, bypass only `StopAndAwaitEmpty()`/`WriteKill()`, and still invoke the real fallback `ObservePopulation()` exactly once?
4. Are the three fake backend inputs deterministic and correctly mapped to Empty, Populated, and Unknown without using a missing lease for T15?
5. Are the component results, aggregate precedence, journal finalization, and durable bit expectations correct for a seeded v3 baseline with only DomainRemoval already confirmed?
6. Do the proposed call counters and clock checks prove no kill, removal, retry, deadline reset, false cleanup success, or error-text inference?
7. Does the proposal preserve the accepted C3 mapping, `Kill=Failed` behavior, journal schema, stop authority, C3 human policy choice, and all approval/qualification boundaries?
8. Is the test-build separation concrete enough to implement without exposing a generic or Owner-controlled synthetic stop-result path? If not, identify the exact isolation defect and hold D6.4 rather than accepting an unsafe seam.

**Handoff:** review this D6.4 documentation commit only. Do not approve C1–C4, select C3 Policy A/B, authorize implementation, accept G3, or qualify release.
