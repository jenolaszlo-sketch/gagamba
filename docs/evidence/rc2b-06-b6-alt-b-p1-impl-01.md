# Alternative B P1 - owner authority retention and refusal evidence

**P1 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW**

Date: 2026-11-06 (Asia/Manila)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-P1-IMPL-01`

Governing decision: [Alternative B owner decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md)

Accepted handoff: [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md)

Canonical baseline: `d804d9062bab5d9d540df5c2d64f1b2c31bdd667` (tree `0f366e6a411e417dc7f7d0b3c1cef97899fb00cc`)

Implementation source: `2ae1121777c5a9ade4e405a485472395cc5219b0` (tree `431114267e245854d91f6066783f75733eff3141`)

Branch: canonical `main`. No push.

## 1. Disposition

**P1 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

P1 is limited to retaining the owner's first verified qualified selection and adding sealed, disjoint owner authority contexts. It does not enforce owner manifest admission, owner pre-RELEASE generation verification, complete pre-effect sink coverage, validation issuance, native qualification or production activation.

The implementation is deterministic-tested on Windows. Protected-root and native rows remain pending and are not claimed as passes.

## 2. Exact P1 requirements

From the accepted plan, P1 required:

1. Return the actual verified `LoadedInstallation` from `QualifiedOwnerPreflight.Verify` instead of discarding it.
2. Retain the full `InstallationIdentity`, manifest, `QualificationId` and `QualificationRecordSha256` from the first owner-local verification.
3. Add sealed, internal, disjoint owner contexts for normal qualified, independently authorized validation and legacy experimental operations.
4. Keep the validation context unreachable and non-constructible from ordinary callers.
5. Keep the later direct-echo comparison against the first retained tuple; it may not replace that tuple.
6. Add deterministic tests proving that a later echo or caller Boolean cannot replace the first selection.
7. Preserve legacy behavior and avoid exposing positive validation authority.

The exit condition was: no positive validation path, no general-admission claim from retention alone, public production unavailable, and existing accepted behavior unchanged.

## 3. Production changes

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerAuthorityContext.cs` | New sealed owner-local context with disjoint `Qualified`, `Validation` and `Legacy` cases. Only the qualified case carries a retained `InstalledResult`; validation has a private constructor and no factory; legacy is a singleton. No Boolean selector. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/QualifiedOwnerPreflight.cs` | Returns `OwnerAuthorityContext.Qualified.FromVerified(installation)` from the actual first `LoadedInstallation.Verify` result instead of discarding it. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/OwnerDirectConnection.cs` | Requires the retained context to agree with the wire shape. The later echo verifies against the retained identity and compares the observed selection to the retained tuple before sending the existing echo. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs` | Classifies qualified versus legacy authority once before `RunTransaction`; refuses retention/wire mismatch before journal, writer, allocation or staging effects; passes the context through direct acceptance and the HELD handoff. |
| `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs` | Retains the context across the in-process HELD handoff and refuses a qualified phase without retained qualified authority. P1 does not consume it for release. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/OwnerAuthorityContextTests.cs` | Ten deterministic retention, echo, isolation and no-Boolean tests. |

No production change was made to `ReleaseArbiter`, `HeldExecution`, the owner safety monitor, recovery, reconciliation, the release transport, the journal schema or the C3 deadline.

## 4. Authority and refusal behavior

### 4.1 First verified selection is retained

The owner now keeps the actual `LoadedInstallation` returned by the pre-writer S2 verification. The context exposes the complete retained `InstalledResult`, plus the exact identity, manifest, qualification ID and raw qualification-record hash. The wire's expected identity is used for the initial verification, but the retained result becomes the owner-local tuple for the rest of the process.

### 4.2 Later echo cannot replace the first tuple

The direct echo path still re-enters the trusted fixed-root verifier. It then compares the fresh observation against the first retained `InstalledResult` using the existing full-decision helper:

- exact `InstallationIdentity` equality;
- exact qualification ID equality;
- exact raw qualification-record hash equality;
- fresh state must be `Qualified`.

A mismatch refuses and does not assign, cache or adopt the later observation. A successful echo sends the retained identity, not the later object.

### 4.3 Disjoint contexts and no caller Boolean

`OwnerAuthorityContext` is abstract with a private constructor. The nested cases are sealed:

- `Qualified` requires a retained selection; production construction is only through the verified `LoadedInstallation` factory.
- `Legacy` is a singleton and carries no installed identity.
- `Validation` has only a private constructor and no factory. Ordinary callers cannot construct or obtain it. The atomic validation slice must add its only factory together with the complete issuer, context, policy and sink checks.

No constructor, method parameter or Boolean property selects an authority case. The Debug-only `ForTest` factory is excluded from Release by `#if !GAGAMBA_PRODUCTION`; the Release verifier reported zero qualification hook types and properties.

### 4.4 Refusal before protected effects

`OwnerPreparationTransaction` classifies the request once:

- authenticated installed identity requires the retained qualified context;
- an unqualified/legacy request uses the legacy context;
- a wire/context mismatch aborts before the journal, writer, allocation or staging path;
- `HeldReleasePhase` refuses a qualified request without retained qualified context before its release machinery is constructed.

These are refusal-only consistency checks. They do not grant manifest admission, staging, HELD or RELEASE authority.

### 4.5 Trust inputs

The only production source of a qualified context is `LoadedInstallation.Verify("owner", expected)`, which re-enters S2's fixed-root verifier and loaded-role checks. No caller hash, Boolean, controller assertion, wire echo or validation mode can supply or replace the retained tuple. Process authentication remains necessary but is still not treated as execution authorization.

## 5. Stopping and lifecycle preservation

- Original actor, recovery and session association checks are unchanged.
- Recovery remains stop-only and retains its reaping/reconciliation duties.
- The original C3 deadline and Policy A stop behavior are unchanged.
- The release arbiter, sole physical writer, one-attempt rule, C1/C2/C4 handling and delivery observation path are unchanged.
- Already admitted operations are not gated on a new general-admission read.
- Legacy v1 experimental behavior continues through the separate legacy context; it cannot be promoted to qualified authority.
- `RestrictedExecutionRuntime.Create()` remains unavailable.

## 6. Tests and exact results

Commands were run from `C:\Users\Laszlos\source\repos\Gagamba` on Windows with SDK `10.0.401`.

| Check | Result |
|---|---|
| Debug build of M1 tests | **0 warnings, 0 errors**; instrumented verifier PASS |
| `OwnerAuthorityContextTests` | **10 passed / 0 skipped / 0 failed** |
| `FinalAcceptanceTests` + `InstallationWireTests` + `RecoveryB2Tests` + `DirectAuthenticationTests` + `ReleaseArbiterTests` | **147 passed / 20 skipped / 0 failed / 167 total** |
| Full Windows M1 Debug runner | **1276 passed / 331 skipped / 0 failed / 1607 total** |
| Release build of `Gagamba.Execution.Linux.M1` | **0 warnings, 0 errors**; production verifier PASS; 0/4 hook types, 0/3 hook properties |

The previous full Windows M1 count was 1266 passed / 331 skipped / 0 failed (1597 total). P1 adds exactly ten deterministic tests; the totals reconcile to 1276 / 331 / 0 (1607).

The 20 skips in the focused regression filter and the 331 skips in the full runner are existing Linux-native classes. They were not executed and are not passes.

### 6.1 P1 deterministic test mapping

| Test | P1 claim |
|---|---|
| `FirstVerifiedSelectionIsRetainedExactly` | Full retained selection, identity, manifest, qualification ID and record hash. |
| `LaterEchoWithExactRetainedTupleIsAccepted` | Later echo with the exact tuple is accepted without replacing retention. |
| `LaterEchoWithChangedGenerationRefusesAndDoesNotReplaceFirstTuple` | Changed generation refuses and first tuple remains unchanged. |
| `LaterEchoWithChangedQualificationIdRefuses` | Qualification ID is part of the binding. |
| `LaterEchoWithChangedQualificationRecordRefuses` | Raw record hash is part of the binding. |
| `LaterEchoWithUnqualifiedObservationRefuses` | Fresh/unqualified observations cannot satisfy a retained qualified tuple. |
| `LegacyContextCannotBePromotedToQualified` | Legacy isolation at the type level. |
| `ValidationContextHasNoCallerConstructibleFactory` | Validation case has only a private constructor and no factory. |
| `AuthorityContextsHaveNoCallerBooleanSelector` | No Boolean parameter or property selects or upgrades authority. |
| `AuthorityContextBaseHasNoPublicConstructor` | Callers cannot construct the base context. |

## 7. T01-T23 traceability

| Cases | P1 disposition |
|---|---|
| T01-T05 | **P2 pending.** P1 retains the selection and refuses later echo mismatches, but does not add owner manifest/final-admission enforcement. |
| T06-T09 | **Existing actor/descriptor controls preserved.** Deterministic/focused suites pass; native cases remain skipped pending native qualification. P1 adds no alternate actor path. |
| T10-T12 | **P3 pending.** Controller Q10 and release behavior are unchanged; the owner pre-RELEASE generation gate is not implemented in P1. |
| T13-T15 | **Existing arbiter/stop/deadline behavior preserved.** No new writer, deadline or admission read; full deterministic runner green. P3 adds the owner pre-write scan. |
| T16-T18, T20 | **Slice E pending.** No validation issuance or positive validation path exists. |
| T19 | **Legacy context preserved and isolated.** Deterministic context tests cover non-promotion; native legacy positives were not rerun. |
| T21 | **Partially satisfied by P1.** First retained tuple and no second trust root are implemented and tested; owner manifest consumption remains P2. |
| T22 | **P3 pending.** P1 makes no post-scan detection claim. |
| T23 | **P4/E pending.** Administrative lock/marker ordering is untouched. |

## 8. Deferred work

P1 deliberately does not implement:

- **P2:** owner manifest authorization and protected final-admission rechecks at owner entry and manifest acceptance.
- **P3:** owner pre-RELEASE active-generation verification after durable intent, before the final gate.
- **P4:** complete pre-effect sink inventory and missing-context refusals at every installed Start/recovery/owner sink.
- **Slice E:** independent root issuer, fixed launcher, protected catalog/approval chain, GAVA1, v3 prepublication bootstrap, privilege drop, protected readers, exact case policy, validation-only early direct ordering and all positive validation gates.
- **Slice N:** protected native qualification, protected-store positives, generation races, descriptor/custody failures and lifecycle regressions on an authorized host.
- Production activation, publication, protected provisioning, packages or push.

## 9. Preservation

- Baseline to source is a docs-only acceptance commit plus this P1 source/test commit.
- The 44 historical untracked paths remain unchanged.
- One canonical worktree remains at `C:\Users\Laszlos\source\repos\Gagamba`.
- No branch, worktree, reset, clean, rebase, prune, stash, force operation or push occurred.
- The owner decision and all prior evidence/review bytes are unchanged.

## 10. Evidence package

External raw evidence package:

`C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p1-impl-01`

Contents include the Debug build, focused, regression-filter, full M1 and Release build logs; source head; worktree list; status after; source diff stat; per-file SHA-256 values; test summary; and a SHA-256 manifest. `SHA256SUMS.txt` has SHA-256 `90910848564faa72ab1d38408f854df119ff7ff625fe2ddb750555e4ccb84291` and covers all 11 package files.

Source file SHA-256 values:

| File | SHA-256 |
|---|---|
| `OwnerAuthorityContext.cs` | `5372e4fc45013ddcaaf671c65cd1ef2d7bfc09848116e9940cb264b0ccb4669e` |
| `QualifiedOwnerPreflight.cs` | `96a179afe9a6e0e14a134c1c57ea3ec1014b41219a9c1b7d5bcd0bd7e241bda5` |
| `OwnerDirectConnection.cs` | `b61c85be33062726fa5defeba4fb67ac8ee2e0623c95553e6eb5daa5446ec8d2` |
| `OwnerPreparationTransaction.cs` | `e5abb11cc6cf558d7646d925c91b109694cb792229d7df52f9c8e6aa7c4864f7` |
| `HeldReleasePhase.cs` | `36bfd6dd5dec2e1f995881525be9a954f3055ddcb150acdb068dd3e51528222c` |
| `OwnerAuthorityContextTests.cs` | `c72cd04e78d1b004f3140c8cb71e1d0ee88ca88d8e934042d1040cf7ea45a71e` |

Release M1 DLL SHA-256: `1015dd35da18fb94d2cf1b46d0b9e5c40195b933d8ad90297768402a1d30b3a7`

Debug test DLL SHA-256: `1c041bb6fb30ba493fc9423fd33e8718168cb36f42fd1669aa1f297da0203a3e`

## 11. Independent review questions

1. Does the retained context contain the complete first verified tuple, and is the production construction path limited to the actual `LoadedInstallation.Verify` result?
2. Does the later echo compare against the retained tuple without any path that replaces it with the fresh observation or wire values?
3. Are the qualified, validation and legacy cases genuinely disjoint, with validation unreachable and no Boolean selector?
4. Do the transaction and HELD consistency refusals occur before the authority transition they protect, without becoming a new grant?
5. Is legacy behavior preserved without any qualified fallback?
6. Are the release arbiter, sole writer, C3 deadline and stopping/reconciliation paths untouched?
7. Do the ten deterministic tests cover the P1 claims, and are the skipped native rows correctly reported as pending rather than passed?
8. Does the Release verifier confirm that the Debug-only test factory is excluded from production?

## 12. Final disposition

**P1 IMPLEMENTED WITH QUALIFICATIONS - READY FOR INDEPENDENT REVIEW.**

Implementation and deterministic evidence are complete for P1. P2-P4, Slice E and Slice N remain unauthorized and unimplemented. Native and protected-root obligations remain pending.
