# Alternative B P4-02 implementation evidence

Date: 2026-11-06 (Asia/Manila)

Commission: GAGAMBA-RC2B-06-B6-ALT-B-P4-02-IMPL-01

Baseline: `6f910669972db51959c5f0920a775e3d6789db06`; tree `2baffb9989d53209c0ecd6c8d5c60e17ba183613`.

Final source/test commit: `b96e86950c8bbb8ef44b00b5076afe9c17a22f2b`; tree `b5c331211d0712707397ec7161e34ad7a5dc3861`, canonical `main`. Documentation delivery is a separate child; its identity is in the external final receipt.

Host: Windows 10.0.26200, win-x64; .NET SDK 10.0.401, runtime 10.0.12, VSTest 18.7.0. Native Linux protected installation, original non-root C/R/O, delegated cgroup v2 and protected positive controls were not provisioned or exercised.

Scope: refusal-only enforcement of the named F01 recovery-initialization and F02 prohibited-constructor-topology findings; deterministic regressions, source/capability audit and Q1 preparation. Exclusions: Slice E, positive prepublication validation, Slice N, provisioning, activation, publication, push and automatic independent review.

**P4-02 IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1.** This is producer source/deterministic evidence, not owner acceptance, full P4/Q04/S09 coverage, bootstrap compatibility or native qualification.

## Preflight and authority

Expected HEAD/tree matched; tracked/index state was clean, with one canonical `main` worktree. Initial P4-02 status was **NOT STARTED**. No intervening implementation or active concurrent Gagamba modifier was found through Git state/history, Codex chat inventory and process inspection. The P4-01 manifest SHA-256 was `7881857325ae064139f707fc3ae3e35b5047d933307b6e383116fc591e4c92be`; all 71 entries matched. All 44 historical untracked paths/bytes matched the previous inventory. The same checks are repeated at delivery. No historical evidence or owner decision was rewritten.

Governing inputs: [Alternative B design](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md), [canonical plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) §§3.4–3.5/5–8, [P4-01 inventory](rc2b-06-b6-alt-b-p4-01-sink-inventory.md), [P4 handoff](../plans/rc2b-06-b6-alt-b-p4-implementation-handoff.md), accepted [P1](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md), [P2](../decisions/rc2b-06-b6-alt-b-p2-owner-acceptance-01.md), [sequencing clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md), [P3](../decisions/rc2b-06-b6-alt-b-p3-owner-acceptance-01.md) and [Mechanism B](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md).

P4 requires trusted context before installed effects, refusal when that context cannot be supplied, no Boolean/echo/legacy substitute, and preservation of original stop/reap/journal duties. Normal qualified initialization and operation-bound manifest acceptance have independent checks. Recovery remains stop-only; the owner remains the sole physical RELEASE writer, with at most one attempt and original C3 deadlines. Positive validation requires the atomic Slice E dependency set and remains unavailable.

Luna (`gpt-6-luna`) handled F02's small guard/tests, initial F01 test construction and the external cross-configuration constructor probe. The parent integrated F01, completed wiring/counter tests, corrected harness issues and executed verification. These are implementation subtasks, not an independent Q1 or cross-model qualification claim.

## Implemented enforcement

Paths below are relative to `src/Gagamba.Execution.Linux.M1/` and bound to the source commit above. Original production chain was reconstructed in both directions: direct qualified Start → QualifiedRecoveryConnection → RecoveryHost → QualifiedRecoveryEcho → IntegratedRecoveryHost → M1RecoveryScope initial intent/spawn → owner P1/P2/P3. The actual initial recovery effects preceded the owner process, so the owner's P2 check could not dominate them.

| Entry / finding | Original omission and first protected effect | New boundary and trusted source | Refusal, ordering and cleanup | Disposition / remaining qualification |
|---|---|---|---|---|
| Direct `RecoveryConnection.Start` / `QualifiedRecoveryConnection.Start`, F01 | S2 identity verified, but caller need not perform the client's final check. Bootstrap recovery process/pins precede operation initialization | Original S2/actor/deadline checks retained. Recovery enforces its own normal-initialization check below; no new global Start gate | T01 still permits bootstrap/stop machinery. A direct caller cannot use that success to obtain the following protected operation effects without the recovery checks | **CLOSED for the named current-production route**, by downstream enforcement; no bootstrap-wide or native-positive claim |
| `QualifiedRecoveryEcho.Run:23–35`, F01 | Successful loaded-role S2/echo was not final admission. Installed parent could already have been opened | Existing bounded verification worker invokes `RecoveryInitializationAdmission.Require(selection, expected)` at 32 before owner launch plan/positive echo. Actual `LoadedInstallation.Verify("recovery", expected)` supplies the first retained selection | Protected verifier reads the fixed selector/record/host stores; failure aborts before successful echo and installed parent access. Same original controller pin/control-loss service and echo deadline/cap; no writer lock held | **CLOSED** for normal v2 integrated initialization; B2 evidence-only branch skips this operation gate |
| `RecoveryHost.RunCore:59,66–68`, F01 | Installed delegated parent opened before recovery S2/admission | The old early parent-open call is explicitly v1 only. v2 integrated parent open occurs after successful qualified echo/verification | Missing final authority cannot reach installed parent acquisition. Legacy ordering preserved. If later withdrawal is observed after an earlier valid check, already opened parent authority is disposed by the original recovery scope on unwind | **CLOSED** for S07 ordering; real descriptor trace/cleanup remains native-pending |
| `RecoveryInitializationAdmission.Require:11–15, RequireEligible:28–35`, F01 | Wire identity, a bare S2 result or nominal context could otherwise be confused with admission | Requires actual retained LoadedInstallation, full expected identity equality and non-null original qualification ID/raw record hash; invokes existing protected `VerifyFinalAdmission`, then existing `IsFinalAdmissionEligible` and `RequireStillQualified(grant.Selection, retained)` | A grant's fresh observation cannot replace the original tuple. No injected verifier, bare binding, Boolean or validation exception is a production authority input. No owner context is manufactured for R | **CLOSED** normal-authority predicate; protected positive and live metadata mutation pending |
| `IntegratedRecoveryHost.Run:23`, F01 alternate/direct entry | Optional `installed=null` was not locally refused before journal/storage/safety setup and initial worker | `RequireIntegrated(config, installed, control, controllerFd)` is the first source statement, before all existing initialization | Strict config validation rejects installed claims in v1; v2 requires retained context and matching identity, then a fresh protected check on a bounded worker. Polls original controller pin and queued control/EOF/loss. Scan deadline is min(original setup, original expiry, five-second scan cap), never extends authority. No locks acquired across reads | **CLOSED** local missing/mismatched context path. Final protected success remains native-pending |
| `IntegratedRecoveryHost.Run:49,77,89` → `M1RecoveryScope.StartOwnerPreparation:371,389`, F01 effects | Initial journal writer/lock, durable AllocationIntent and operation-owner spawn preceded O's first preflight/P2 check | The local check dominates journal/storage/IntegratedOwnerLaunch and preparation worker creation. Scope still writes intent, relinquishes the writer, spawns O and waits WriterReady in its original order | On initial refusal, initialization entry is not attempted. The initial writer is distinct from cleanup writer reacquisition at IntegratedRecoveryHost:164. Recovery still cannot authorize a manifest; original throwing callback remains | **CLOSED** for initial writer/intent/operation-owner effects on this route; exact native absence trace pending |
| `B3PreparationOwner` constructor, `Restricted/B3Qualification.cs:271–285`, F02 | Direct qualified/non-integrated construction created a scope, journal object and directory; factory already rejected it | Constructor now rejects `QualifiedInstallation && IntegratedRecovery is null` at entry, before all construction effects | Throws InvalidOperationException; no directory, scope/client or journal created for the prohibited shape. Factory guard remains; legacy constructor and integrated classification controls are preserved | **CLOSED for the named prohibited topology**. Integrated classification alone does not constitute a trusted admission or protected positive |
| Qualified non-integrated B2 / QualifiedB2Owner | Separate S2 evidence/lifeline topology, no writer/domain/staging/manifest/HELD/RELEASE dispatch | No new admission gate. Qualified delegated-parent B2 refusal and original actor/descriptor checks remain | Stop/evidence setup does not become execution admission. Original stop/lifetime/reap remains available; v1 delegated retained-domain controls remain legacy | **PRESERVED**; native B2 identity/stop qualification unchanged |
| Owner P1/P2/P3 and recovery continuation | Owner early/late final gates and after-intent generation verification already accepted | These files are byte-identical to baseline; 13 authority/lifecycle boundary files are hashed in `preserved-authority-boundaries.json` | No renewed admission on stop, reap, cleanup writer, retained-domain removal or reconciliation. Original C3 and sole one-attempt writer unchanged; uncertainty remains Unknown | **PRESERVED**, with prior native qualifications/residuals |

The Release helper has no caller-supplied verifier. It obtains the final grant through the actual retained installation. `RequireEligible` is the shared exact-tuple decision over the existing private grant, not an issuer. The Debug-only `RecoveryInitializationTestFixture` is an observer: it can count entry into initialization but cannot provide a selection/grant or bypass a check. It and its references are absent from Release, checked by the production verifier's forbidden-TestFixture inventory.

## Validation and scope boundary

The new protected-final requirement applies only to the currently implemented **normal qualified v2 integrated initialization**. Its S2 contract already requires Qualified. It is not installed on generic Start or non-integrated B2 and does not renew admission for lifecycle continuation. Legacy v1 cannot carry installed identity/context. False profile flags do not bypass a v2 authenticated association: the recovery check follows config version and expected installed identity, not Profile.QualifiedInstallation.

There is no current positive prepublication validation route to preserve by bypassing this check. The separately authenticated validation context, independent issuer, custody/policy, integrity projection and early direct ordering remain Slice E. A future validation route must supply that complete authority atomically; it must not enter the normal-qualified check with synthetic Qualified, reuse a grant-shaped binding or add a test-mode exception. No GAVA/issuer/wire/schema/driver change was made. F01's **current normal-production boundary** needs no new cross-process grant: recovery already independently obtains the trusted loaded selection, and its protected final verifier binds the retained qualification association.

F02 closes the exact prohibited qualified/non-integrated shape named by this commission. The integrated-constructor probe is only a branch-classification control: it still creates a directory under a synthetic integrated profile and does not prove provider admission. The factory and prepare client retain their own protected gates. The broader P4 constructor-context/alternate-entry inventory must not be reported globally closed by this topology correction; any separate integrated-constructor pre-effect tightening and Slice E construction prerequisites remain explicit wider P4 work. Likewise, generic scope/launcher functions are not newly universal admission gates. Only their audited current production route is covered here.

The accepted trusted-root observation-to-effect residual remains. A change after the last relevant stable observation may escape detection. A fresh later refusal can follow earlier effects initiated under valid authority; original cleanup remains responsible. No atomic root-mutation/write lock or retroactive cancellation is claimed.

## Baseline, corrected and negative-control evidence

| Check | Baseline or bypass observation | Corrected observation / meaning |
|---|---|---|
| F02 original actual constructor, Debug and Release | External production-logic probes observe a journal directory for the prohibited shape | Same cross-configuration reflection probe on fixed binaries observes InvalidOperationException and `JournalCreated=false` |
| Exact new Debug F02 xUnit regression | Fails against original Debug: “No exception was thrown” | Passes against corrected Debug |
| Identical external refusal-regression predicate, Debug and Release | Both original binaries fail the fixed refusal expectation because the directory is created | Both fixed binaries pass; valid legacy and integrated-classification controls also pass |
| F01 actual direct Run missing/mismatched context | External Debug DLL copy has only the local RequireIntegrated call replaced with four stack pops plus nop; both counter tests fail with expected 0 / actual 1 | Unmodified committed source gives initialization-entry count 0 and the specific admission refusal, with no journal path created |
| Counter positive control | Deliberately invalid legacy runtime enters initialization (count 1), then fails native prerequisites | Distinguishes refused-before-entry from attempted-but-failed setup; never a native success |
| Tuple/authority tests | Missing grant, wrong full identity, qualification ID/raw hash, withdrawn acceptance and changed grant observation refuse | Matching **simulated** grant/retained tuple passes the shared decision only; reflection-created LoadedInstallation still must enter real protected verification and cannot manufacture a protected positive |

The initialization counter is deliberately not a writer/domain completion counter. Count 0 plus the local gate's source/IL dominance establishes non-entry into subsequent initialization; source tracing establishes that writer/intent/spawn are downstream. It is not a live Linux protected-resource trace. The first installed parent boundary is supported by the actual echo-worker and RecoveryHost ordering assertions and call audit, not by a fabricated Windows cgroup result.

All binary mutations were in an external copy. Canonical DLL/source remained unchanged. Mutant/baseline failures are expected negative controls, not failures in the corrected implementation.

## Final checks on the committed source

Counts overlap across filters and are not added as distinct tests.

| Check | Passed | Failed | Skipped / status |
|---|---:|---:|---|
| New P4-02 focused xUnit cases | 14 | 0 | 0 |
| P1/P2/P3, final acceptance, installation wire, recovery B2/direct authentication | 200 | 0 | 20 |
| Stopping/reconciliation/C3/B3/capture filter | 84 | 0 | 17 |
| Full Windows M1 | 1,349 | 0 | 331; 1,680 total |
| Isolated D6.4 variant | 3 | 0 | 0 |
| Debug test/dependency build | — | 0 errors | 0 warnings |
| Release M1 / Owner / Recovery builds | — | 0 errors each | 0 warnings each |
| Production assembly verifier, Release | — | — | PASS |
| Production verifier on Debug negative control | — | — | FAIL as expected; catches fixture/test authority surfaces |
| Constructor matrix: 3 cases each, baseline/fixed × Debug/Release | 12 expected observations | — | Includes observed baseline defect, not 12 qualification passes |
| Changed-document links / staged whitespace | — | — | Verified at documentation delivery |
| Repository-wide documentation preparation | — | — | Stops on the inherited archived `01-rc2b-06-b6-native-env-prep-01.md` relative link; that archived file and target absence match baseline. No historical repair was made |

The new full result is the accepted reference's 1,335 passes plus 14 new passing cases; the 331 native skips remain unqualified. No expected count or behavior oracle was changed to obtain this result.

Development/harness failures are retained: the first test build needed the missing IlCallScanner namespace import; a Debug test DLL could not directly use Release's shorter B3FixtureProfile constructor and produced MissingMethodException; the first reflection probe looked up the contract type in the wrong assembly. These are not claimed as valid baseline or Release regression evidence. The corrected cross-configuration probe resolves the actual constructor and contract assembly; its matching baseline/fixed refusal predicate supplies the valid evidence. Raw failed runs remain in the package alongside successful final runs.

## Cleanup, remaining cases and delivery

New portable constructor tests/probes use unique empty directories and remove them with nonrecursive deletion. Invalid topology creates none. No new test launches a Linux helper/domain/target or provisions a protected store. Missing/mismatch initialization tests create no journal path. Native stop/reap/fence/domain/staging/finalization evidence remains pending/Unknown, never inferred from these portable controls.

T01–T05/T21 map to normal recovery pre-effect refusal and retained-tuple checks; T06–T09 preserve original actor/descriptor/direct-channel boundaries; T13–T15 preserve stop/deadline/cleanup; T19 covers legacy and prohibited topology. T10–T12/T22 remain P3 and protected-native obligations. T16–T18/T20 remain the atomic Slice E validation dependency set. T23 retains existing protected selector semantics and keeps admission I/O out of stop/custody/journal locks. The complete [P4-01 mapping](rc2b-06-b6-alt-b-p4-01-sink-inventory.md) remains authoritative; this commission does not close all Q04/S09 sinks or administrative publication.

Before native acceptance, separately exercise real protected final-positive/negative direct Start, identity/qualification mutation, original C/R/O credentials and pidfds, descriptor/capability custody, stop/loss while a protected read blocks, original deadline expiry, partial-resource cleanup and the accepted post-observation residual. A new source binary requires new candidate hashes and exact host/boot/kernel/runtime/delegation binding. Slice N and root provisioning are not authorized here.

External raw package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-02-impl-01`, containing source identities/patch/index, closure matrix basis, commands, failed and successful logs/TRX, original/fixed constructor probes, isolated mutation evidence and self-excluded `SHA256SUMS.txt`. The [Q1 handoff](../reviews/rc2b-06-b6-alt-b-p4-02-q1-handoff.md) lists review questions and limits. Source/tests and documentation are committed separately. One canonical main worktree and all 44 historical paths/bytes are preserved; P4-01's 71-entry package remains unchanged. No push or independent Q1 was performed.
