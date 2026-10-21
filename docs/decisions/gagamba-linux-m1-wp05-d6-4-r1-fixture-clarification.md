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
| Friend access | Execution/TransactionStateModel.cs:11-12 currently grants Gagamba.Execution.Linux.M1.Tests and Gagamba.Execution.Linux.M1.Owner unconditionally. Gagamba.Execution/Restricted/RestrictedManifest.cs:11-12 currently grants the regular M1 and M1.Tests assemblies. | Conditional M1 friend metadata must emit exactly the current two names in production and only Gagamba.Execution.Linux.M1.D6_4.Tests in the variant. Shared Gagamba.Execution metadata adds Gagamba.Execution.Linux.M1.TestFixture and Gagamba.Execution.Linux.M1.D6_4.Tests; the first serves the variant, the second the linked test support/test assembly. Both added names remain in production core metadata; see section 4 for exposure and checks.

R12's `DomainPopulationResult` is defined at `Lifecycle/RestrictedDomain.cs:15-21`: known Empty is `Emptied`, known non-empty is `Populated`, and unavailable evidence is `Unknown(error)`. `RecoveryPlanner` and identity checks are not bypassed by this fixture. The diagnostic `Error` value is never parsed or used as a success oracle.

## 4. Chosen test-only mechanism

**Proposed future test build only; not an R12 API and not implemented.** Use the existing src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj as the single M1 source project, evaluated once for production and once for the test variant. Static inspection at the D6.4 base found that the project sets TargetFramework=net10.0, nullable/implicit-using options, IsPackable=false, and a reference to Gagamba.Execution; it does not override Compile, AssemblyName, DefineConstants, or output paths. Directory.Build.props has no such overrides and there is no Directory.Build.targets. SDK default compile items supply the M1 source inventory. A static dotnet msbuild -getProperty/-getItem:Compile comparison at Configuration=Release and TargetFramework=net10.0, using production properties and the proposed distinct assembly/output properties, returned 44 Compile paths for each evaluation with identical full paths. The fixture symbol was not part of that path-only query. A static scan found no existing preprocessor directives or D6.4 fixture-symbol uses in the M1 source. No build, test, or binary check was run.

The dedicated Gagamba.Execution.Linux.M1.D6_4.Tests project must reference that same M1 project with variant-specific properties: AssemblyName=Gagamba.Execution.Linux.M1.TestFixture; the production DefineConstants set plus only GAGAMBA_WP05_D6_4_TEST_FIXTURE; unique BaseIntermediateOutputPath, MSBuildProjectExtensionsPath, and BaseOutputPath; and IsPackable=false. Production and variant use the same configuration and target framework. Do not copy M1 source files or create a parallel production tree. The test project links RecoveryTestSupport.cs and its test sources into the dedicated test assembly; those are test-project inputs, not additional M1 variant sources.

The variant properties must stay scoped to the M1 project instance. On the M1-to-Gagamba.Execution project reference, remove the variant-only global properties (AssemblyName, DefineConstants, the three isolated path properties, and IsPackable) before evaluating the dependency. This keeps the shared core project at its normal identity and output paths; the test project also references that normal core project. Confirm the evaluated project graph and resolved assembly paths. Owner continues to reference the regular M1 project with no variant properties. The normal production build uses its existing output directories, and variant outputs, intermediate files, generated files, and NuGet assets use disjoint paths.

### Fixed marker and injection boundary

The proposed D6.2 typed StopOutcome and classifier do not exist in R12. The future authorized implementation must supply them first. In the test variant only, place this fixed marker in the shared Reconciler.cs source under the fixture symbol. It accepts no arbitrary result, delegate, or payload:

~~~csharp
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
~~~

Under that same symbol only, add the conditional Reconciler field and test-only overload:

~~~csharp
internal Reconciler(
    IDomainClock clock,
    TimeSpan stopAllowance,
    NotAttemptedStopTestFixture fixture);
~~~

The synthetic result enters at the Reconciler stop-result boundary after RecoveryPlanner.Plan() and liveLease.VerifyIdentity() succeed, and only when DomainStop progress is unconfirmed. The fixture build selects fixture.CreateOutcome() instead of liveLease.StopAndAwaitEmpty(), so WriteKill is not called. The future typed classifier must leave DomainStop=Unknown for this fixed input. The ordinary DomainEmpty fallback then calls the real liveLease.ObservePopulation() exactly once, reaching the linked FakeDomainBackend.ReadPopulation(); the marker's Population=NotObserved records that the synthetic stop path performed no population read. Planning, identity, fallback classification, aggregation, and journal persistence remain on the shared production source path. The regular production build has no marker, field, overload, or selection branch and always invokes the bounded stop.

The marker cannot inject Accepted, Failed, arbitrary Unknown, or a caller-authored outcome. It changes only the stop result for R1-T13–T15; the test-build variant and support counters remain future work.

### Friend metadata

Select one shared-source conditional declaration in Execution/TransactionStateModel.cs; do not also emit duplicate generated assembly attributes:

~~~csharp
#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.D6_4.Tests")]
#else
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.Owner")]
#endif
~~~

With the fixture symbol undefined, the production M1 assembly must retain exactly its existing M1.Tests and M1.Owner friend grants. With the symbol defined, the test-variant M1 assembly grants internals only to Gagamba.Execution.Linux.M1.D6_4.Tests; it emits neither the M1.Tests nor M1.Owner grant. The regular production M1 assembly does not friend D6_4.Tests.

Gagamba.Execution/Restricted/RestrictedManifest.cs keeps its existing friends to Gagamba.Execution.Linux.M1 and Gagamba.Execution.Linux.M1.Tests and adds both Gagamba.Execution.Linux.M1.TestFixture and Gagamba.Execution.Linux.M1.D6_4.Tests. The variant needs the first grant to compile against internal RestrictedManifest contracts. GLM identified the second as required for the dedicated test assembly that compiles the linked recovery support and its internal contract use. The M1 variant's own IVT grant separately gives that test assembly access to M1's internal ownership and journal contracts.

These InternalsVisibleTo attributes are emitted assembly metadata. The two additional Gagamba.Execution friend names remain in the shared production Gagamba.Execution metadata; they add no runtime method or public API, but they grant compile-time access to internals by those assembly names. The inspected projects have no strong-name signing configuration, so these names are not a cryptographic boundary and must not be described as a runtime sandbox. The residual risk is that another assembly using either friend name can compile against Gagamba.Execution internals. Production isolation comes from the synthetic fixture being absent from the regular M1 assembly and from Owner/release dependency graphs. The artifact and graph checks below must establish that boundary; they cannot remove the shared core's friend metadata exposure.

### Source-graph and conditional-compilation verification

For the same clean source revision, configuration, and target framework, capture the evaluated M1 Compile items for production and variant with dotnet msbuild -getProperty and -getItem:Compile. Also capture the actual Csc source inputs from a future build binlog after generated compile inputs are added. Record the effective project properties, compiler constants, resolved paths, source origin, and SHA-256 of each input's actual bytes.

Normalize tracked source identities to canonical Git-root-relative paths. Let P be the production M1 shared-source set and V the test variant's shared-production-source set. Require set equality in both directions: P = V. Require matching SHA-256 for each shared file. Any missing, substituted, copied, or unclassified M1 source fails the check. The selected design has no fixture-only M1 source-file addition: the marker and guarded stop selection belong in Recovery/Reconciler.cs, and the conditional friend block belongs in Execution/TransactionStateModel.cs. RecoveryTestSupport.cs and the acceptance tests are separately enumerated inputs of D6_4.Tests, not members of V.

Do not silently omit generated sources. The binlog-derived manifest must include every actual compiler input, including SDK-generated framework and assembly metadata, with its generating target, normalized logical path, and hash. Production and variant hold Configuration and TargetFramework equal. Allow only identified assembly-identity metadata differences required by the distinct M1 assembly name; framework/configuration metadata should otherwise match. The chosen design generates no fixture application source. Any other generated source difference, substantive generated code, or unclassified compiler input fails verification unless it is individually allowlisted and justified.

The effective test-variant DefineConstants set must equal the production set plus exactly GAGAMBA_WP05_D6_4_TEST_FIXTURE. Scan all M1 source and generated inputs for that symbol. Its only allowed guarded regions are: the friend declaration in TransactionStateModel.cs; the fixed NotAttemptedStopTestFixture type; and the fixture-only Reconciler field, constructor, and stop-result selection branch in Reconciler.cs. Parse both configurations with their actual preprocessor symbols and compare normalized active source outside those allowlisted regions. Any other symbol use or code difference fails verification. A source-path comparison alone is insufficient.

### Production-purity and graph checks

These are future build-time checks, not checks claimed as completed by D6.4 or D6.5:

1. Inspect emitted M1 assembly identities and IVT metadata: production has exactly the existing two M1 friends and no D6_4.Tests friend; the variant has only the D6_4.Tests friend.
2. Inspect Gagamba.Execution metadata for the existing friends plus both required new names.
3. Confirm the dedicated test project resolves the test-variant M1 identity and does not reference or build Owner.
4. Confirm Owner resolves the regular production M1 identity and its project/dependency graph contains no test variant.
5. Confirm the regular production M1 compiler options omit the fixture symbol and its binary has no NotAttemptedStopTestFixture, fixture-only overload, field, or selection branch.
6. Confirm production and variant obj, bin, MSBuildProjectExtensionsPath, generated files, and package assets do not overlap or overwrite one another.
7. Confirm the test variant and D6_4.Tests are non-packable and absent from normal release/publish dependency graphs and artifacts.

**A test-built variant is not binary-identical evidence for the production assembly.** R1-T13–T15 evidence is bounded to the future typed classifier, shared production source logic whose identical source inventory is verified, controlled fixed stop-result injection, and real fallback observation and durable-progress behavior exercised through the variant. It does not establish binary identity, normal production stop-path execution, production Owner integration, native sandbox qualification, end-to-end release qualification, or behavior under untested build configurations. The fixture remains useful for these defensive cases because the planner, identity checks, classifier, fallback read, persistence, and aggregation execute in the shared Reconciler source; only the fixed stop result is substituted.
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

For all three cases, no allocations make Staging confirmed without a cleaner call; the seeded DomainRemoval bit skips removal. Assert the existing v3 record was mutated, its version and schema stayed the same, unrelated fields stayed unchanged, and final durable Cleanup state is Unknown (the seeded state is InProgress). T13 adds only the truthful DomainEmpty progress bit. T14/T15 add neither domain bit. The healthy writer makes journal finalization confirmed; unavailable population evidence is not a journal failure.

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

1. Does the exact R12 source still support the stop-first, retained-lease fallback, identity, fake-backend, journal, and aggregation statements? Is R12 Populated=Failed clearly separated from the proposed mapping?
2. Are the production and test-variant M1 friend sets explicit and achievable, and are both additional Gagamba.Execution friend names accounted for?
3. Is the emitted-metadata exposure of the two shared-core friend grants accurately described, including the residual name-based access risk and the limit of production graph isolation?
4. Is the same-project MSBuild strategy compatible with the inspected configuration, including variant-property isolation at the M1-to-core project-reference edge?
5. Does the future bidirectional manifest compare canonical production-source paths and content hashes, account for generated sources, and fail on unallowlisted inputs?
6. Are the fixture-symbol differences limited to the named friend block, fixed marker, and Reconciler seam, with other active production code verified as shared?
7. Do the proposed artifact checks verify production purity, Owner identity, release graph exclusion, and nonoverlapping outputs without relying on project filenames alone?
8. Is the test-built variant's binary-identity limitation stated clearly and its evidence scope bounded?
9. Are the fixed injection boundary, real fallback, R1-T13–T15 results, durable bits, aggregate, and negative assertions unchanged?
10. Does any source-grounded isolation or source-equivalence blocker remain, or are remaining details implementation refinements?

**Handoff:** review this D6.4 documentation commit only. Do not approve C1–C4, select C3 Policy A/B, authorize implementation, accept G3, or qualify release.
