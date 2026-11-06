# P4-03 qualified B3 provider-authority enforcement

Date: 2026-11-06 (Asia/Manila). Slice: GAGAMBA-RC2B-06-B6-ALT-B-P4-03-IMPL-01.

**P4-03 IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1**

| Evidence binding | Identity / scope |
|---|---|
| Starting source / tree | `5d47624bd6661f0065f3a3cca11b72ac52bd9a3c` / `dbb810dafc7a371b0c8756d73f1f70123e95e7f2` |
| Final implementation/tests / tree | `ead317ee96c6c177ca1fc1568192c6bf6ea28a90` / `1b65806e3089ec30d5d1bd6942a104267b2c03d7` |
| Source / test subtrees | `e558d9e9034008dcc7d4cd0f0bf43fe838764bc5` / `fd96cbbf08ec48fe7f054e9c48966ae80be8b163` |
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, canonical `main`, one worktree |
| Host / tools | Windows 10.0.26200 win-x64; SDK 10.0.401, MSBuild 18.9.11, .NET runtime 10.0.12; PowerShell, Git, Python and dotnet/vstest |
| Native prerequisites | Protected Linux installation, genuine installed provider/C/R/O, delegated cgroup v2 and protected native-positive/mutation controls unavailable/not exercised |
| Qualification scope | Source implementation, actual portable constructor refusal, exact-tuple decision/wiring tests, baseline/fixed Debug/Release probes, full Windows M1 regression |
| Exclusions | Full P4/Q04/S09 acceptance; positive validation/Slice E; native/Slice N; provisioning, activation, push, independent Q1 or owner acceptance |

The documentation delivery commit is recorded after commit in the external receipt to avoid self-reference. Source and tests are committed separately from these documents. Actual model participation is Codex implementation with `gpt-6-luna` assistance for test drafting, portable constructor probes and a read-only sink/contract audit. This is producer implementation evidence, not independent Q1 or cross-model certification.

## Preflight and governing contract

Initial P4-03 status: **NOT STARTED**. Expected HEAD/tree and ancestry matched; P4-02 implementation/evidence and [owner acceptance](../decisions/rc2b-06-b6-alt-b-p4-02-owner-acceptance-01.md) were present. Tracked files/index were clean, one canonical main worktree remained, and all 44 historical untracked paths and SHA-256 values matched. No committed or unfinished equivalent P4-03 work was found. Relevant other Codex chats were idle/not loaded, process/status checks found no concurrent Gagamba writer, and no index lock existed; no global exclusion lock is claimed. Root alone modified canonical files; Luna assistance wrote external artifacts only.

Verified historical manifests before work: P4-01 71/71; P4-02 producer 732/732; P4-02 Q1 2,512/2,512; P4-02 owner-acceptance receipts 20/20. Their bytes and all earlier owner decisions remain unchanged.

Authority comes from existing contracts: [P4 handoff, subsequent bounded constructor correction](../plans/rc2b-06-b6-alt-b-p4-implementation-handoff.md), [accepted implementation plan §§3.4–3.5](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md), [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md), [Mechanism B separation and gates](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md), and the existing [P1](../decisions/rc2b-06-b6-alt-b-p1-owner-acceptance-01.md), [P2](../decisions/rc2b-06-b6-alt-b-p2-owner-acceptance-01.md), [P3](../decisions/rc2b-06-b6-alt-b-p3-owner-acceptance-01.md) decisions. Normal qualified provider authority requires actual provider-role `LoadedInstallation.Verify("provider")`, protected `VerifyFinalAdmission()` and exact `AdmissionAuthority.IsFinalAdmissionEligible` binding. S2 Qualified, final admission, validation and activation remain distinct. No new trust definition, issuer, grant factory, protected store, wire/schema or public provider is introduced.

## Original omission, correction and exact call graph

**F02 remains CLOSED:** qualified/non-integrated construction still throws before setup/directory effects. **P4-03 corrects Q1 O1 / inventory S02:** qualified/integrated direct construction previously created a journal directory without constructor-local provider authority. Q1 had found only one production caller, the already guarded factory; no current factory/manifest/RELEASE bypass was demonstrated. The new guard closes the local sink without reclassifying that original finding.

Locations below are relative to `src/Gagamba.Execution.Linux.M1/` at `ead317ee`:

```text
Public Gagamba.Runtime.RestrictedExecutionRuntime.Create remains unavailable.
Internal Restricted/B3Qualification.TryCreate :77
  :97 rejects qualified/non-integrated topology
  :188–193 retains first actual provider LoadedInstallation and checks protected final admission
  :199 new B3PreparationOwner(profile, passive descriptor, original providerInstallation)
  :201 RestrictedExecutionOperation is created only after successful constructor return
Direct B3PreparationOwner(profile, descriptor) :274–276
  delegates to the context-bearing constructor with null; qualified construction refuses
Context-bearing constructor :279–295
  :282–285 preserves F02 topology refusal
  :287 B3ProviderAdmission.Require(profile, providerInstallation)
  :288–293 state/scope-or-client/journal object setup
  :295 Directory.CreateDirectory(profile.JournalDirectory)
B3ProviderAdmission.Require :10–31
  :12–17 legacy rejects installed context; ordinary legacy with null remains
  :19–24 requires retained qualified manifest/identity/qualification ID/raw record hash
  :29 LoadedInstallation.Verify("provider", retained.Identity) rechecks actual provider role
  :30 RequireStillQualified(observed.Selection, original) compares the entire retained tuple
  :31 retained.VerifyFinalAdmission() + RequireEligible(original, fresh private grant)
  RequireEligible :36–41 applies existing eligibility and compares the grant's fresh S2 tuple
Subsequent owner path (unchanged)
  Prepare -> IntegratedB3Client.Prepare :49–67 provider S2/final before recovery spawn
  -> QualifiedRecoveryConnection.Start -> RecoveryHost / QualifiedRecoveryEcho
  -> P4-02 recovery-local pre-effect gate -> IntegratedRecoveryHost.Run :23
  -> initial writer :77 -> M1RecoveryScope staging parent :319 / durable intent :371 / O spawn :389
  -> owner first S2/P1 -> P2 early admission :92 -> allocation/staging
  -> P2 late operation-bound admission :431 -> Prepared/publish/capability -> HELD
  -> P3 owner after-intent generation gate / sole physical RELEASE; separate controller Q10
```

The factory passes its actual first `LoadedInstallation`, not a reconstructed record or later selected generation. The constructor re-enters the literal provider role with the original identity, compares full identity plus qualification ID/raw hash, then obtains a fresh protected final grant through the retained object. The grant verifier independently reads the fixed protected host/selector/record stores, checks exact bindings/stability and freshly revalidates S2. A changed generation or qualification refuses; it is never assigned back to the retained selection. No grant is cached or reused to admit another construction. The existing final-admission schema has no separate constructor/session-expiry field; missing, withdrawn, stale, malformed and unavailable evidence follows the existing verifier's semantics. Validation expiry/custody belongs to Slice E, not a new invented rule here.

The local check precedes `_profile`, scope/client/journal setup and directory creation. The instance's private monitor object and the factory's passive descriptor are memory-only, not capability publication. Verification itself may open read-only protected metadata descriptors as required by the existing trusted reader; no operation writer, delegated construction resource, process, intent, staged object, capability or HELD state is acquired by this guard. The first observed filesystem construction effect is journal-directory creation. The read is outside stop/custody/journal locks and starts no operation or new deadline. Later P1/P2/P3/Q10/C3 and original stop/reap/reconciliation checks remain untouched; cleanup writers are not gated on new admission.

## Baseline controls and effects

| Probe / check | Original P4-02 observation | Corrected observation |
|---|---|---|
| Same portable real-constructor probe, Debug and Release, qualified/integrated with no authority | Constructs integrated client and creates fresh journal directory | Two-argument and three-argument/null-context routes throw InvalidDataException before directory creation |
| Same fixed-refusal oracle against both original binaries | Expected exit 1 specifically because the qualified/integrated directory is created | Expected exit 0 on both committed fixed binaries |
| Exact new xUnit `ExistingTwoArgumentQualifiedIntegratedConstructorRefusesMissingProviderBeforeJournal` | Expected one failure, “No exception was thrown”; no MissingMethodException or constructor-arity substitution | Passes on fixed committed source |
| F02 topology, both configurations | Qualified/non-integrated already refuses | Still refuses as InvalidOperationException, no directory |
| Legacy controls, both configurations | Non-integrated scope and integrated client classify correctly and create directory | Same; each probe observes and removes only its own empty directory |
| Exact-tuple and actual constructor negatives | Synthetic data is decision/negative test input only | Missing/null/unqualified/unavailable/incomplete/fabricated contexts refuse; exact identity, qualification ID/hash, withdrawn grant and replacement-grant observations refuse |

Baseline DLLs are manifest-verified reviewer-built P4-02 Q1 binaries; `git rev-parse` confirms their `645b85b:src` and P4-03 baseline `5d47624:src` both equal `24db6ae42402418082470e746250f60364e4c12f`. Baseline was not rebuilt for this commission. Probe DLL SHA/MVID and expected exits are recorded in raw logs. Earlier precommit fixed results are retained separately from final committed-source reruns; no precommit metadata is silently relabeled.

Actual-constructor synthetic identity/qualification mutations refuse on this unprovisioned host during the real provider S2 path. Exact mismatch semantics are additionally tested in the shared production eligibility decision, and actual constructor/helper/factory wiring is checked in IL. This combination is source/deterministic evidence; it is not a protected-native wrong-identity/withdrawal trace or a protected positive. One pure matching-tuple decision fixture does not authorize actual construction. There is no injected production verifier, Boolean bypass or new Debug authority factory.

The old P4-02 qualified/integrated synthetic classification control intentionally had no authority; it now must refuse. Its branch-classification control is explicitly renamed/scoped to legacy integrated construction, while the new authority refusal regression detects the original qualified omission. F02 and ordinary legacy assertions are unchanged. No expected authority oracle was weakened. The initial test build's missing contract namespace import and a staged trailing-blank-line check are retained as development/check receipts; both were corrected before the source commit and final checks.

## Final checks on committed implementation

Counts overlap between filters/full suite and must not be added as distinct tests.

| Check | Passed | Failed | Skipped / result |
|---|---:|---:|---|
| New P4-03 tests | 19 | 0 | 0 |
| P4-02 recovery/topology regressions | 14 | 0 | 0 |
| P1/P2/P3/authority/authentication/B3/recovery/C3/capture/installation filter | 352 | 0 | 58 native skipped |
| Full Windows M1 | **1,368** | **0** | **331 native skipped; 1,699 total** |
| Isolated D6.4 | 3 | 0 | 0 |
| Debug test/dependency build | — | 0 errors | 0 warnings |
| Release M1/Owner/Recovery builds | — | 0 errors | 0 warnings |
| Release production assembly verifier | — | — | PASS |
| Debug production-verifier negative control | — | — | Expected FAIL/exit 1; detects instrumented and test-authority surfaces |
| Baseline fixed-oracle / exact new regression | — | Expected failures | Baseline defect controls only, not corrected product failures |

The full result is the reference 1,349 passes plus 19 new passes with unchanged 331 native skips. All skips remain unqualified. Final binaries are built from the committed candidate, not an inferred historical full-suite count. Committed M1 SHA-256: Debug `9dff7c7b49419c0da6d33ddced3b38396ea565910f5d8c0b9ec58b9c67b0a6ab`; Release `51defeb5749b33e4b0748db6b1e84ffe703cb970dae20eec64e5f3956c1f1321`. Test DLL `522f836189a782df9fdee295703aa2e2d5e44dcfba99ee53c84eb5e51195349b`. Source archive SHA-256 `ad84c3284e3ac69689583a88281912d25dcfeb6432aa1fac9ce3c404cabc321f`. The 1,854-file archive is bound to Git blobs with only individually verified Git CRLF conversion; no extra checkout/worktree exists.

## Remaining P4 sink matrix

The original [29-row inventory](rc2b-06-b6-alt-b-p4-01-sink-inventory.md) remains historical and unchanged. The following current dispositions plus the raw `remaining-sink-assessment.md` map every row; only S02 receives a new production guard in P4-03.

| Rows | Current guard, first effect and remaining boundary |
|---|---|
| S01 | Factory actual provider S2/final precedes operation construction and passes the original tuple to S02. Preserved and strengthened by local sink check. |
| S02 | **P4-03 corrected:** retained provider role/S2/final and exact original tuple required before setup/journal-directory effect. F02 topology remains closed. Protected valid/mismatch pair pending Slice N. |
| S03 | Integrated client separately checks actual provider S2/final before recovery spawn. No new guard here. |
| S04 | Legacy v1 bootstrap retains accepted pins/stop and no installed-identity claim. |
| S05–S06 | Qualified Start and original recovery authentication/subreaper establish bounded bootstrap/stop machinery before operation initialization; not manifest authority. |
| S07–S09 | P4-02 F01 covers installed parent acquisition, recovery-role echo/admission and local integrated missing-context refusal before writer/safety/worker effects. No new uncovered route found. |
| S10–S11 | Staging-parent retention, durable intent and operation-owner spawn are dominated by F01 on current installed integrated callers; other callers are legacy/B2/test. No blanket local admission insertion; installed launch integrity remains. |
| S12–S14 | Non-integrated qualified B2 is evidence/stop-only; delegated qualified B2 refuses before effects; legitimate v1 delegated control remains legacy. No execution-admission gate added to stopping. |
| S15–S17 | P1 actual owner context and P2 early check precede owner writer/allocation; later direct echo retains its accepted after-writer ordering. No new authority from echo. |
| S18–S20 | P2 early admission dominates domain, descriptor/staging transfer and copy effects; accounted cleanup remains. |
| S21–S23 | Independent late P2 admission precedes Prepared/store/capability publication and HELD. No new publisher/HELD guard needed on audited current route. |
| S24–S25 | P3 owner after-intent generation check and sole RELEASE writer, plus separate controller Q10, remain unchanged with accepted observation residual. |
| S26–S27 | Original stopping, reap, retained cleanup, writer reacquisition and reconciliation remain lifecycle continuation without new admission; native behavior pending. |
| S28 | Coordinator/low-level copy/publisher constructors have only test callers at this source. `internal` is not authority; a future production caller needs a fresh context/effect audit. |
| S29 | Existing conditional fixture exclusions/production verifier and disabled public factory remain. No production activation. |

No immediate current unauthorized-execution or RELEASE route was found by this bounded audit. Absence of a local check at an already dominated generic callee is not reported as a new gap. Wider constructor/direct-entry coverage, future caller invariants and full Q04/S09 remain explicit obligations; P4 as a whole is incomplete and unaccepted. P1–P3 and P4-02 retain their existing bounded qualifications.

## Slice E, native and cleanup qualifications

This is a normal-qualified refusal mechanism, not a definition that final acceptance is the only possible future authority category. [Plan §3.4](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) permits this narrowing before validation integration; §3.5 requires the complete distinct Slice E context and atomic R/O/C changes before positive validation. Independent issuer/launcher, protected catalog/inputs, session/process custody, integrity projection, exact case policy, GAVA, original IDs, pre-writer direct ordering and phase-specific owner generation verification remain unavailable/refusing. No positive validation exception or synthetic Qualified object is exposed. Slice E T16–T18/T20 remains open; native installed-positive/negative execution qualification remains Slice N.

Protected real S2/final-positive, withdrawal/mismatch, host/role closure, process/pidfd/SCM custody, bounded-read stop, native RELEASE and partial-resource lifecycle/cleanup need separately authorized exact-host qualification. The accepted observation-to-effect residual remains: changes after the last relevant stable observation may escape detection, including before verifier return. A later gate can refuse after earlier effects admitted under valid authority; original stopping/reconciliation remains responsible.

New tests/probes use fresh nonced absent journal paths, assert rejection before directory existence and no returned owner, and delete only their own observed empty directories nonrecursively. Legacy controls actually create directories and then clean them; they are not described as zero effects. No new test/probe launches a Linux helper/domain/target or provisions protected authority. Native cleanup status is **Unknown/pending**, never inferred from portable absence or successful API return.

## Delivery and next step

External package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-03-impl-01`. It contains exact commands, initial/source/final preservation, frozen source/test patch and archive, source index/caller audit, binary SHA/MVIDs, successful and expected-failing logs/TRX, portable probes, model contribution scope, remaining matrix and self-excluded `SHA256SUMS.txt`. Earlier evidence/owner bytes and all 44 historical paths are preserved. No push, branches/worktrees, history operations, provisioning, activation, unrelated P4, Slice E/N, independent Q1 or owner acceptance.

Next: separately commissioned independent Q1, following the [review handoff](../reviews/rc2b-06-b6-alt-b-p4-03-q1-handoff.md). Stop at this producer handoff.
