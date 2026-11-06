# Alternative B P4-02 independent Q1 handoff

Date: 2026-11-06 (Asia/Manila)

Source to review: `b96e86950c8bbb8ef44b00b5076afe9c17a22f2b`, tree `b5c331211d0712707397ec7161e34ad7a5dc3861`, canonical `main`; baseline `6f910669972db51959c5f0920a775e3d6789db06`, tree `2baffb9989d53209c0ecd6c8d5c60e17ba183613`.

Producer disposition: **P4-02 IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1.** This document prepares review only. No independent review or owner acceptance was started.

Read [implementation evidence and closure matrix](../evidence/rc2b-06-b6-alt-b-p4-02-impl-01.md), [P4-01 inventory](../evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md), [P4 implementation handoff](../plans/rc2b-06-b6-alt-b-p4-implementation-handoff.md), [accepted plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md), [Alternative B decision](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) and [P2 sequencing clarification](../decisions/rc2b-06-b6-alt-b-p2-sequence-rem-01-owner-clarification.md). Existing P1/P2/P3, Mechanism B and B2 lifecycle contracts remain unchanged.

## Exact change and claimed closure

Seven source/test files changed:

- `Bootstrap/RecoveryInitializationAdmission.cs`: normal-qualified protected decision, exact retained tuple check, strict local config/legacy separation and bounded original-controller/stop servicing; Debug-only observer cannot supply authority.
- `Bootstrap/QualifiedRecoveryEcho.cs`: normal integrated final check in the existing loaded-role worker before owner plan/echo.
- `Bootstrap/RecoveryHost.cs`: preserve early v1 parent opening; defer v2 integrated parent until successful echo/check.
- `Bootstrap/IntegratedRecoveryHost.cs`: independently require retained matching authority before any initialization entry/journal/safety/worker setup; cleanup path unchanged.
- `Restricted/B3Qualification.cs`: direct constructor rejects qualified/non-integrated topology before scope/journal/directory effects; factory check retained.
- `Bootstrap/RecoveryInitializationAdmissionTests.cs` and `Restricted/B3PreparationOwnerTopologyTests.cs` under the M1 tests project: 14 new cases.

**F01 claim:** the audited current normal-qualified direct Start route cannot acquire the installed parent, initial writer/durable intent or operation-owner authority without the independently verified recovery pre-effect check; direct IntegratedRecoveryHost entry cannot omit/mismatch retained authority. C bootstrap recovery/stop machinery may exist before refusal under T01. No unauthorized manifest or RELEASE was demonstrated at baseline or introduced by the fix.

**F02 claim:** the exact prohibited qualified/non-integrated constructor shape refuses before construction effects in Debug and Release. Legacy construction and integrated classification remain. The integrated classification control is not a protected provider-admission positive; wider direct-constructor private-context coverage is not globally closed by this topology correction. Review this scope distinction explicitly against the commission and broader P4 handoff rather than silently treating the probe as authority.

The normal-qualified gate has no prepublication validation exception. Slice E's independent issuer/context/integrity/custody/policy ordering remains unimplemented and refusing. No new cross-process authority bearer, wire/schema, manifest grant or RELEASE writer exists.

## Questions requiring independent answers

1. Does F01 enforcement precede **every protected initialization effect it claims to guard**, including installed parent acquisition, initial writer, durable intent and operation-owner spawn? Independently follow both forward dispatch and reverse capability transfer; do not infer dominance from a helper's name alone.
2. Is authority independently trusted and bound to the correct original installation/session? Check the actual first recovery LoadedInstallation, full expected identity, protected grant and fresh qualification-ID/raw-record-hash comparison, plus unchanged original process/session/execution checks. Can caller data replace any part?
3. Can a direct integrated Start caller bypass the new refusal, or can direct IntegratedRecoveryHost invocation use null/mismatched context, a profile flag, copied wire identity or legacy classification to proceed? Repeat the external mutation negative control without changing canonical source.
4. Does the implementation preserve the prepublication validation-bootstrap boundary? Verify that current normal-qualified checks neither authorize synthetic Qualified nor pretend Slice E exists, while B2 evidence/stopping avoids a blanket final-admission requirement. Identify any missing accepted authority prerequisite precisely.
5. Can recovery still stop, reap and reconcile existing operations after withdrawal/failure/loss? Verify cleanup writer reacquisition remains distinct from the gated initial writer, protected reads occur outside stop/runtime/custody/journal locks, the scan cap only shortens original deadlines, and a late worker cannot revive authority.
6. Does direct production B3 construction reject F02 before journal-directory or other protected effects? Reproduce original Debug/Release directory creation, the identical expected-refusal negative control and fixed refusal using the same reflection probe. Separate constructor-shape incompatibilities from product behavior.
7. Are qualified/legacy paths isolated? Verify qualified/non-integrated refusal, v1 installed-claim/context refusal, unchanged legacy/delegated B2, false-profile handling on authenticated v2, and honest integrated-constructor classification. Assess any broader P4 constructor-context residual separately from the named topology finding.
8. Are P1 retention, P2 early/late owner admission, P3 after-intent owner generation verification, controller Q10, original C3 and sole one-attempt RELEASE writer unchanged? Do not replace owner checks with recovery's initialization decision.
9. Are deferred Slice E/native controls honestly classified? Source/deterministic closure is not protected-positive or native cleanup qualification. Verify the preserved observation-to-effect residual and no global Q04/S09/bootstrap/activation claim.

## Executable evidence and reproduction

External package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-02-impl-01`; verify `SHA256SUMS.txt` before reuse. `commands.json` and each log record exact invocation; `source-identities.json`, `source-test.patch`, `source-index.txt`, `call-audit.txt`, snapshots and `preserved-authority-boundaries.json` bind the source. Final source and documentation receipt records are external to avoid self-referential commit hashes.

Final Windows checks: new 14/14; P1/P2/P3/recovery 200 passed/20 skipped; stopping/reconciliation 84 passed/17 skipped; full M1 1,349 passed/331 skipped/zero failed; isolated D6.4 3/3; Debug and Release M1/Owner/Recovery builds zero warnings/errors; Release production verifier passes and Debug negative control fails as intended. Counts overlap; no native skip is a pass.

Negative controls: exact Debug F02 regression fails on original constructor; cross-configuration expected-refusal probe fails on both original binaries and passes on both fixed binaries. Removing only the local authority call in an external Debug DLL makes two F01 tests fail with initialization count 1 instead of 0. The canonical binaries remain intact. The observer control deliberately enters legacy setup and fails its invalid runtime; it is not a native positive.

The package retains initial namespace/type-lookup harness errors and invalid attempts to load a Debug test DLL against Release's different constructor shape. Those are explicitly excluded as proof of product behavior. Use the corrected reflection probe and final valid runs. Review was not commissioned as part of implementation; Luna participation is producer assistance only.

## Native and repository limits

All 331 native skipped cases remain unqualified. Required protected controls include real S2/final admission positive and missing/withdrawn authority via direct Start, generation/qualification changes, actual actor/descriptor custody, blocked-read stop/deadline behavior, partial-resource cleanup and the post-scan residual on an exact candidate/host/boot/runtime/delegation. No protected store, host identity or native target was fabricated. Native provisioning and Slice N need separate authorization.

Review canonical main read-only or use the external evidence package. Preserve prior evidence/owner decisions and all 44 historical untracked paths. Do not create linked worktrees/branches, reset/clean/restore/rebase/prune, push, implement Slice E, provision, activate or automatically accept the owner decision. Any change proposed after review must be separately attributable and appropriately reverified.
