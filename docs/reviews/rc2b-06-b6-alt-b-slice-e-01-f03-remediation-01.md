# Slice E-1 F-03 remediation report — Windows PowerShell 5.1 verification-harness log capture

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-REMEDIATION-01`
Date: 2026-11-07 (Asia/Manila). Producer disposition: **IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION**.

Baseline `1ed1fc706ea76954e4f1d7d43673048ef82072ec` (tree `5ca31e86940d14805759b416b995e1cade952a0a`); corrected script commit `52037fa5738609b1469089835380a0cc94e047f1` (tree `ce921c0c0013f7baceac6c898f758ca1587f5ec4`, eng subtree `544808fbca2f87127d9e402bbd103a2f6a35d2ba`). This commission does not close F-03; independent qualification is required.

Reviewer evidence: `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-f03-remediation-01-evidence/`.

## 1. Verified baseline

`main`, one canonical worktree, tracked/index clean, 44 historical untracked paths preserved. HEAD/tree matched the commission exactly. Remote state was not fetched, merged or rebased.

## 2. Root-cause reconstruction

`eng/test-production-build.ps1`, `Invoke-Probe`:

- `('dotnet ' + ($Arguments -join ' ')) | Set-Content $log` writes the command header with the default encoding (single-byte, no BOM).
- `& dotnet @Arguments >> $log 2>&1` appends the process output with `Out-File`, whose default encoding under Windows PowerShell 5.1 is **UTF-16LE**.
- `Get-Content -LiteralPath $log -Raw` then decodes the mixed-encoding file as single-byte, so the expected `GAGAMBA002` diagnostic is not contiguous and `$text.Contains('GAGAMBA002')` returns false. The `missing-symbol` negative control therefore threw `Probe failed` despite a correct verifier exit `1`.

A **second, pre-existing Windows PowerShell 5.1 defect was unmasked** by fixing the encoding: any probe whose expected diagnostic is written to **stderr** (the assembly verifier's `RESULT: FAIL`) is turned into an error record by PowerShell, and with `$ErrorActionPreference = 'Stop'` it aborts the harness before `$ok` can be evaluated. The original script never reached those verifier-FAIL controls because it failed earlier at `missing-symbol`. Windows PowerShell 5.1 is the target platform for this harness; both defects are harness-only and neither involves the verifier.

## 3. Pre-fix reproduction evidence

Reproduced under **Windows PowerShell 5.1.26100.9711** (`repro-encoding.ps1`, `repro-stderr.ps1`):

```
PSVersion=5.1.26100.9711
ORIG exit=1 containsGAGAMBA002=False      # mixed encoding; first4 = 64 6F 74 6E
ORIG threw=True exit=                      # verifier stderr aborts under Stop
```

This matches the independent Q1 F-03 finding.

## 4. Exact script correction

Only `eng/test-production-build.ps1` changed (+26/−4):

1. After `$ErrorActionPreference = 'Stop'`: `$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'` — pins `>>`/`Out-File` (the process-output append and the `Get-FileHash` append) to UTF-8.
2. Header write: `Set-Content -LiteralPath $log -Encoding UTF8`.
3. Log read: `Get-Content -LiteralPath $log -Raw -Encoding UTF8`.
4. Scoped error preference around the native capture:
   ```powershell
   $probeErrorAction = $ErrorActionPreference
   $ErrorActionPreference = 'Continue'
   try { & dotnet @Arguments >> $log 2>&1; $code = $LASTEXITCODE }
   finally { $ErrorActionPreference = $probeErrorAction }
   ```

## 5. Encoding and exit-code guarantee

- The header, the appended process output and the read all use an explicit UTF-8 encoding, so diagnostic text is contiguous and detectable. `$LASTEXITCODE` is captured immediately after the native call and is unchanged.
- The scoped `Continue` applies **only** around the native capture; the assertion (`$ok`) and the `throw "Probe failed…"` below are unchanged and remain in force (a PowerShell `throw` is independent of `$ErrorActionPreference`). No check was skipped, softened or replaced.

## 6. Positive and negative verification matrix (observed)

| Scenario | Required | Observed (corrected harness) |
|---|---|---|
| Valid RELEASE assembly | Accepted | `inspect-*-Release` exit 0, passed |
| RELEASE assembly with prohibited hook (instrumented inspected as production) | Rejected | `stale-instrumented-binary-inspection` exit 1, `FAIL`, passed |
| Debug inspected as production | Rejected with expected diagnostic | `stale-instrumented-binary-inspection` / owner/recovery equivalents, passed |
| Missing required symbol | Rejected with `GAGAMBA002` | `missing-symbol` exit 1, `GAGAMBA002`, passed |
| Other policy negatives | Rejected with expected code | `unknown-configuration` `GAGAMBA001`, `debug-forced-production` `GAGAMBA003`, `publish-*-empty-symbols` `GAGAMBA002`, all passed |
| Consistently encoded log | Diagnostic detected | first bytes `EF BB BF 64`, `containsGAGAMBA002=True` |

Full harness: **51/51 probes passed, exit 0** (`harness-summary.txt`).

## 7. Independent failure-control results

Disposable stubs through a replica of the corrected capture/assertion semantics (`failure-controls.ps1`):

```
stub-correct (exit 1, GAGAMBA002)                 ok=True
stub-zero-despite-diagnostic (exit 0, GAGAMBA002) ok=False
stub-wrong-diagnostic (exit 1, GAGAMBA999)        ok=False
stub-missing-diagnostic (exit 1, no diagnostic)   ok=False
```

The corrected assertion still rejects an invalid result (wrong exit status, wrong diagnostic, or absent diagnostic). No weakened verifier artifact was built or committed.

## 8. Windows PowerShell 5.1 execution results

- Invocation: `powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1 -EvidenceDirectory <tmp>`.
- PowerShell version: `5.1.26100.9711`.
- Result: `All 51 build/binary probes passed`, script exit `0`.
- Diagnostic detection: `missing-symbol` matched `GAGAMBA002`; the verifier-FAIL controls matched `FAIL`.

## 9. PowerShell 7 results

`pwsh` is **not available** in this environment, so PowerShell 7 was not executed. The correction uses only explicit `-Encoding UTF8`/`Out-File:Encoding` facilities that behave consistently (BOM difference aside) on PS 5.1 and PS 7; cross-version compatibility is by construction, not executed, and is recorded as a limitation for independent review.

## 10. Scope and security-regression assessment

`git diff` is confined to `eng/test-production-build.ps1`. No Gagamba runtime production or test source changed; no production verifier implementation changed; no verifier policy relaxed; no security diagnostic removed; no positive-validation authority, execution capability, E-1 or P4 behavior changed. F-01 remains closed with qualifications; F-02 remains open; `NATIVE-PENDING` and the 331 unqualified native skips preserved.

## 11. Remaining limitations

- PowerShell 7 compatibility is unexecuted (§9).
- The stderr/error-preference handling is a second harness correction required for the harness to complete under PS 5.1; it is documented here for independent scrutiny.
- The harness remains a build/binary regression tool; it does not qualify protected native behavior (`NATIVE-PENDING`, Slice N).
- F-03 is not closed by this report.

## 12. Independent qualification handoff

Recommended: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-Q1`. The reviewer should reconstruct the original encoding defect, execute the full harness on Windows PowerShell 5.1 the reviewer controls, independently challenge the exit-code and diagnostic matching (including their own invalid stubs), confirm no assembly-verifier policy changed, assess cross-version behavior, and classify any remaining findings. Only a later owner decision may close F-03.
