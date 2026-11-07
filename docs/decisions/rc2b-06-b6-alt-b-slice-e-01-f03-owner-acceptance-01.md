# Slice E-1 F-03 owner acceptance — verification-harness log capture closure

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-OWNER-ACCEPTANCE-01`

**F-03 CLOSED WITH QUALIFICATIONS**

Date: 2026-11-08 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This decision formally closes the demonstrated F-03 verification-harness defect (both the mixed-encoding capture and the Windows PowerShell 5.1 native-stderr abort) **within the authorized Windows PowerShell 5.1 verification-harness scope**. It does not change the production verifier, does not authorize F-02 remediation, E-2 through E-7, Slice N, or activation, and does not claim PowerShell 7 qualification.

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `01a0a3e6e1a93813a1c921eac452d1eeaa796987` (matches the commission) |
| Starting tree | `9d8b8e54c9f091e599d30303ba9fb2122673b516` (matches the commission) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized; local `main` eight ahead) |
| Push | none |

Independent checks for this record: `git diff 52037fa HEAD -- eng` is empty (the harness is unchanged since its implementation commit); `git diff 1ed1fc7 HEAD -- eng/VerifyProductionAssembly` is empty (the verifier is unchanged since the F-03 baseline); the Q1 report and evidence are committed at this baseline.

## 2. Reviewed implementation revision

| Role | Revision |
|---|---|
| F-03 implementation (script) | `52037fa5738609b1469089835380a0cc94e047f1`, tree `ce921c0c0013f7baceac6c898f758ca1587f5ec4` (eng subtree `544808fbca2f87127d9e402bbd103a2f6a35d2ba`) |
| F-03 baseline | `1ed1fc706ea76954e4f1d7d43673048ef82072ec`, tree `5ca31e86940d14805759b416b995e1cade952a0a` |
| Producer report | [remediation report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f03-remediation-01.md) and its [evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-f03-remediation-01-evidence/) |
| Independent qualification | [Q1 report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f03-q1.md) and its [Q1 evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-f03-q1-evidence/) |
| Original finding | [E-1 Q1](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md) (F-03), preserved by the [E-1 owner acceptance](rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md) |

Non-`docs` diff from baseline to the reviewed revision is exactly `eng/test-production-build.ps1` (+26/−4).

## 3. Independent Q1 evidence

The owner accepts the independent Q1 disposition **SUPPORTED WITH QUALIFICATIONS** and has re-verified the material claims against the committed evidence:

- The exact canonical `Invoke-Probe` was exercised (the Q1 probe extracts the function verbatim at run time; the evidence records `canonical-function-extracted lines=24 contains-dotnet-call=True`), not a simplified substitute.
- Disposable-stub exit-code/diagnostic controls and the launch-failure/stale-`$LASTEXITCODE` control are recorded in the Q1 evidence directory.
- The full Windows PowerShell 5.1 harness run, the corrected `missing-symbol` log, and the standalone-verifier evidence are recorded.

## 4. F-03-A encoding correction assessment

Accepted. The harness now writes the probe header with `Set-Content -LiteralPath $log -Encoding UTF8`, pins `Out-File`/`>>` to UTF-8 via `$PSDefaultParameterValues['Out-File:Encoding']='utf8'`, and reads the log with `Get-Content -LiteralPath $log -Raw -Encoding UTF8`. Independent evidence: `missing-symbol.log` begins `EF BB BF 64` (UTF-8 BOM), `containsGAGAMBA002=True`; the diagnostic originates from actual MSBuild output (`Directory.Build.targets(11,5): error GAGAMBA002`, lines 5 and 9) and the echoed header line 1 does **not** contain it (no header/fixture false positive). `results.json` serialization is unaffected. The original mixed-encoding failure is corrected.

## 5. F-03-B stderr correction assessment

Accepted. `$ErrorActionPreference` is saved, set to `Continue` only around the native capture (`& dotnet @Arguments >> $log 2>&1; $code = $LASTEXITCODE`), and restored in `finally`. Independent evidence: a verifier-style stderr `RESULT: FAIL` no longer aborts and is evaluated (`ok=True`); stdout/stderr are captured to the log; `$LASTEXITCODE` remains authoritative; the original `$ok` assertion and `throw "Probe failed…"` are unchanged and preference-independent. The original PowerShell 5.1 native-stderr abort is corrected.

## 6. Exit-code and diagnostic integrity

Accepted. Using the exact canonical logic (verbatim extraction): exit 1 + expected `GAGAMBA002` → accepted; **exit 0 despite the diagnostic → rejected**; incorrect diagnostic → rejected; missing diagnostic → rejected; expected-success exit 0 → accepted; expected-success nonzero → rejected; stderr `FAIL` → accepted. No false-PASS path was demonstrated within the tested controls.

## 7. Failure-path preference restoration

Accepted. `$ErrorActionPreference` was observed restored to `Stop` after every tested case, including throwing cases. The launch-failure control with a stale `$LASTEXITCODE=1` (`stale_last_exit_code=1; launch_failure threw=True; eapBefore=Stop; eapAfter=Stop; rows_recorded=0; false_pass_possible=False`) shows that an unresolvable executable fails closed with no result row and no false PASS.

## 8. Windows PowerShell 5.1 harness results

Independently reproduced: `powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1` under **Windows PowerShell 5.1.26100.9711** → **51 build/binary probes executed, 51 passed, 0 failed, 0 skipped, script exit 0**, final `inspect-restored-release` reached. This supports the required Windows PowerShell 5.1 boundary; it does not establish protected native execution qualification.

## 9. Verifier preservation

Accepted. `eng/VerifyProductionAssembly/Program.cs` is unchanged from the F-03 baseline. The RELEASE verifier still reports `RESULT: PASS` with exit 0 (0/4 hook types, 0/3 hook properties, 14/14 core types); a Debug assembly inspected as production still reports `RESULT: FAIL` with exit 1 and the unchanged forbidden-hook list (including `PrepublicationIntegrityProjection::ForTest`). No diagnostic code or security policy was relaxed; no new production authority or API was introduced. F-03 closure is **harness correctness only**; the verifier's security rules are unchanged.

## 10. Formal F-03 disposition

**F-03 CLOSED WITH QUALIFICATIONS.**

Accepted only, within the authorized Windows PowerShell 5.1 verification-harness scope:

1. Correction of the mixed-encoding diagnostic-capture defect.
2. Correction of the PowerShell 5.1 native-stderr abort defect.
3. Successful full 51-probe Windows PowerShell 5.1 harness execution.
4. Independently tested exit-code and diagnostic matching.
5. Independently tested false-PASS resistance.
6. Preservation of the existing production verifier.
7. Preservation of existing E-1 and P4 guarantees.

F-03 closure does **not** imply: PowerShell 7 has been executed or qualified; all possible process-launch and filesystem failures have been exhaustively tested; the production verifier's security model has changed; protected native execution is qualified; positive validation is constructible; E-2 through E-7 are authorized; or production activation is authorized.

## 11. Binding Q1-R1/R2/R3 qualifications

- **Q1-R1 (PowerShell 7 not executed).** PowerShell 5.1 is independently qualified for F-03; PowerShell 7 compatibility has not been executed and is asserted only by inspection. A later PowerShell 7 run may provide additional evidence if cross-version support becomes required. Cross-version qualification is **not** claimed. This does not block F-03 closure.
- **Q1-R2 (nonzero-exit semantics).** The harness's existing negative-control contract accepts any nonzero process exit status when the required diagnostic is present. This is a retained contract characteristic, not an F-03 regression; no reachable false-PASS path was demonstrated. If a future probe requires an exact exit code, an explicit assertion for that new contract must be added; the existing contract must not be silently changed.
- **Q1-R3 (scoped preference relaxation).** The `$ErrorActionPreference='Continue'` change is limited to native output capture and is `finally`-restored; this is a documented implementation boundary. Any later change to `Invoke-Probe` must retain strict exit-code and diagnostic validation and guaranteed preference restoration.

## 12. F-01 / F-02 status preservation

- **F-01 — CLOSED WITH QUALIFICATIONS.** Preserve the binding future-consumer obligation: before any production consumer of the E-1 projection is authorized, every integrity-verification exception and failure mode must be demonstrated fail-closed with no fallback, downgrade, partial authorization, or continuation into a protected effect. Do not reopen F-01 without new evidence.
- **F-02 — OPEN.** The `VerifyActive`/`VerifyActiveIntegrity` duplication remains a divergence risk. Preserve the parity assessment whenever either path changes and a consolidation-versus-parity owner decision before production integration. F-02 is not closed and its remediation is not authorized here.

## 13. P4 and Slice E/N exclusions

P4 refusal-only acceptance and F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and all 331 unqualified native skips are preserved. E-1 remains accepted with qualifications. E-2 through E-7 remain separately unauthorized; frozen-schema provenance, validation-catalog approval, E-4/E-6 P4 re-audits, atomic R/O/C ordering, root-authorizer and launcher requirements, protected custody and Slice N qualification are preserved. No positive validation activation is authorized.

## 14. Recommended next commission

A separate bounded architecture and owner-decision commission for **F-02 verification-path divergence control**, comparing `VerifyActive` and `VerifyActiveIntegrity`, evaluating safe consolidation versus explicit parity controls, and determining the minimum independently qualifiable approach before future production integration. F-02's recommendation does not authorize implementation. The subsequent Slice E-2 frozen-schema and validation-catalog approval work also requires separate owner authorization. None is started automatically.

## 15. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Historical reports, decisions and evidence are preserved unchanged. The external acceptance receipt is under `Solo/gagamba-alt-b-slice-e-01-f03-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No runtime or tooling implementation change, no test modification, no verifier rule change, no F-01/F-02 remediation, no E-2 through E-7 work, no Slice N, no activation, no branch/worktree, no reset/clean/merge/rebase/prune, and no push. All 44 historical untracked paths preserved. **Stop after committing this owner decision.**
