# P4-03 independent Q1 handoff

Date: 2026-11-06 (Asia/Manila). Producer disposition: **P4-03 IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1**.

Review implementation/tests `ead317ee96c6c177ca1fc1568192c6bf6ea28a90`, tree `1b65806e3089ec30d5d1bd6942a104267b2c03d7`, against baseline `5d47624bd6661f0065f3a3cca11b72ac52bd9a3c`, tree `dbb810dafc7a371b0c8756d73f1f70123e95e7f2`. Documentation is committed separately; exact delivery identity is in the external receipt. This handoff starts no independent review or owner acceptance.

Read [implementation evidence and remaining matrix](../evidence/rc2b-06-b6-alt-b-p4-03-impl-01.md), [P4-02 acceptance with original Q1 qualifications](../decisions/rc2b-06-b6-alt-b-p4-02-owner-acceptance-01.md), [29-row inventory](../evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md), [P4 implementation handoff](../plans/rc2b-06-b6-alt-b-p4-implementation-handoff.md), [accepted plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md), Alternative B/P1–P3 and Mechanism B owner decisions referenced there.

## Exact change and claim

Only four source/test files changed:

- New `Restricted/B3ProviderAdmission.cs`: normal-qualified provider-role/S2/final guard over the retained actual first selection; legacy/context mismatch refusal; existing eligibility and full-tuple comparison; no injected verifier, grant factory, validation exception or operation authority.
- `Restricted/B3Qualification.cs`: factory passes its original verified selection; original two-argument constructor delegates null to a new context-bearing constructor; F02 topology refusal remains before the local gate, which precedes scope/client/journal setup and directory creation.
- New `Restricted/B3ProviderAdmissionTests.cs`: 19 portable test rows covering actual missing/synthetic-context refusal, exact tuple decisions, actual factory/constructor/verifier IL wiring and legacy compatibility.
- `Restricted/B3PreparationOwnerTopologyTests.cs`: previously authorityless qualified/integrated synthetic classification control is explicitly legacy integrated classification; new authority regression now requires the formerly allowed qualified shape to refuse. Existing F02/legacy assertions remain.

Claim: S02/Q1 O1 is corrected for current normal-qualified construction. A qualified/integrated direct constructor must carry the factory's first actual provider authority and pass fresh provider-role/S2 and protected final checks before construction effects. Missing context cannot obtain a journal directory or owner object. Factory and later controller/recovery/owner gates remain separate. This is not full P4/Q04/S09, positive validation or native qualification.

## Required independent challenges

1. Trace the sole production factory caller and both constructors. Confirm the original two-argument entry cannot bypass the three-argument local gate, F02 remains first, and no alternate construction/call path reaches directory or operation publication without actual authority.
2. Verify provenance: actual `LoadedInstallation.Verify("provider")`, literal provider-role recheck with original identity, full identity/qualification ID/raw hash comparison, then fresh protected final verification against the original selection. A new observation must not replace the original. Check missing, unavailable, stale, withdrawn, malformed and mismatched cases. No Boolean/profile/echo/bare tuple can substitute.
3. Follow the first effects, including memory-only descriptor/monitor construction versus protected metadata reads, scope/client/journal setup, journal-directory creation, and later writer/staging-parent/intent/spawn/publication. Source ordering and IL token order are corroboration, not a live native effect trace.
4. Reproduce the exact new two-argument missing-authority regression against original and corrected binaries. Baseline must fail because no exception is thrown, not because of an API/arity mismatch. Repeat the portable Debug/Release matrix; separate legacy success from qualified admission. The original two-argument signature is retained specifically as a real direct entry, not a permissive fallback.
5. Challenge mismatch/fabricated/validation inputs through actual construction and pure decisions. A pure matching synthetic grant is not a constructor or protected positive. Windows actual synthetic mismatches refuse at provider S2; protected-native mismatch pairing remains unproved. Confirm Release contains no test grant factories, injected verifier or qualification hooks.
6. Confirm the old synthetic classification test change is the intended authority narrowing and does not hide a qualified positive regression. Valid protected qualified construction is pending native verification, not established by the legacy branch control.
7. Independently check remaining S09/S10/S11/S28 routes before staging-parent acquisition, initial writer, intent, owner spawn, Prepared/publication and HELD. Do not insert new guards only because a dominated generic callee lacks a local check. Report an immediate unauthorized execution/RELEASE route if demonstrated.
8. Preserve P1/P2/P3, P4-02 F01/F02, controller Q10, sole owner RELEASE, original C3 and stop/reap/reconciliation without renewed admission. Check source unchanged and actual tests rather than assuming lifecycle coverage from green constructor tests.
9. Assess bootstrap design honestly: final admission is the current normal-qualified mechanism, not the only future category. Validation contexts remain unavailable until atomic Slice E issuer/session/custody/integrity/policy/GAVA/ordering. No prepublication exception or positive validation should appear here. Native installed-positive/withdrawal/blocked-read/cleanup controls stay Slice N/pending.

## Producer execution and reproduction limits

External package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-03-impl-01`; verify its self-excluded SHA-256 manifest before reuse. `commands.json`, `test-summary.json` and logs/TRX contain exact arguments/counts; `source-freeze.json`, `source-identities.json`, `source-test.patch` and source archive bind the candidate; `constructor-probe` contains real-constructor baseline/fixed probes, hashes, MVIDs and expected failures.

Committed-source checks: new 19/19; P4-02 14/14; relevant regression 352 passed / 58 native skipped; full Windows M1 **1,368 passed / 331 native skipped / 0 failed (1,699)**; isolated D6.4 3/3; Debug test/dependency and Release M1/Owner/Recovery builds zero warnings/errors; Release production verifier PASS and Debug negative control fails as intended. Counts overlap. All 331 native skips remain unqualified. This commission reran the full Windows M1 suite; it did not qualify protected Linux positives or validation.

Baseline source subtree equals reviewed P4-02 Q1 source. Preserved reviewer-built Q1 binaries were reused for baseline, not rebuilt. The same fixed oracle fails against both baseline configurations; the exact new xUnit fails “No exception was thrown.” Final fixed matrices run on the committed candidate. Initial test-import and staged-whitespace development errors are retained/disclosed, not product results. No assertion was weakened.

This is Codex producer execution with `gpt-6-luna` implementation assistance (test draft, probe and read-only audit), not independent Q1 or cross-model certification. Reviewer should independently build/export committed source where useful without branches or linked worktrees.

## Repository and stop boundary

Preserve one canonical main worktree, all 44 historical untracked paths/bytes, prior packages and accepted owner decisions. No production activation, privileged provisioning, Slice E/N, unrelated P4, push, reset/clean/restore/rebase/prune or automatic owner acceptance. A separate Q1 commission determines disposition; this handoff does not authorize remediation. Native cleanup remains Unknown/pending; portable directory absence/removal is not native lifecycle proof.
