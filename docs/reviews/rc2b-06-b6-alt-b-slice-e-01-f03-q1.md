# Slice E-1 F-03 independent Q1 qualification report

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-Q1`
Date: 2026-11-08 (Asia/Manila). Reviewer: DeepSeek (independent session). Scope: independent qualification of the F-03 verification-harness remediation; documentation and reviewer evidence only.

**Disposition: SUPPORTED WITH QUALIFICATIONS.**

Both F-03 defects are independently verified as corrected without weakening exit-code validation, diagnostic matching, negative controls, or verifier security policy. Bounded non-blocking residuals remain (PowerShell 7 unexecuted; the harness's pre-existing any-nonzero negative-control semantics). This is evidence for a subsequent owner decision, **not** owner acceptance; F-03 is **not** formally closed here.

Reviewer evidence: `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-f03-q1-evidence/`.

## 1. Verified repository baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD / tree | `c14c8b47da129342c70fe0ab5cb1b337dff7a329` / `587ef13ef15303bbbfd19419ecdf7fa9352c100c` (matches) |
| Worktrees / tracked-index | exactly one; clean |
| Historical untracked paths | 44 preserved |
| `origin/main` (recorded, not modified) | `53850aa158cf26f5a40fb9bd51c606a111188ef3`; local `main` 7 ahead |
| Push | none |

## 2. Reviewed remediation commit

Script commit `52037fa5738609b1469089835380a0cc94e047f1` (eng subtree `544808fbca2f87127d9e402bbd103a2f6a35d2ba`). Non-`docs` diff from the F-03 baseline `1ed1fc7` to HEAD is exactly `eng/test-production-build.ps1` (+26/−4); `eng/VerifyProductionAssembly` is **unchanged**.

## 3. Original encoding defect reconstruction

`Invoke-Probe` wrote a BOM-less single-byte header with `Set-Content`, appended native output with `>>` (`Out-File`, UTF-16LE under Windows PowerShell 5.1), then read the mixed file with unencoded `Get-Content -Raw`; `Contains('GAGAMBA002')` was therefore false despite the diagnostic being present. A second, previously masked PS 5.1 defect: a probe whose expected diagnostic is written to **stderr** (the verifier's `RESULT: FAIL`) became an error record and aborted under `$ErrorActionPreference='Stop'`.

## 4. Independent encoding verification

- Corrected `Invoke-Probe` (verbatim from the committed script): header `Set-Content -LiteralPath $log -Encoding UTF8`; capture `& dotnet @Arguments >> $log 2>&1` with `$PSDefaultParameterValues['Out-File:Encoding']='utf8'`; read `Get-Content -LiteralPath $log -Raw -Encoding UTF8`.
- `missing-symbol.log` first bytes `EF BB BF 64` (UTF-8 BOM); `containsGAGAMBA002=True`.
- The diagnostic originates from actual MSBuild output (`Directory.Build.targets(11,5): error GAGAMBA002`, log lines 5 and 9); the echoed header line 1 does **not** contain it — no header/fixture false positive.
- All output paths for a probe use the same encoding; `Inspect-M1`'s `Get-FileHash … >> log` append inherits the same `Out-File` UTF-8 setting. Repeated probes are deterministic (51/51 across the full run; the probe-logic scenarios were re-run).
- Scope of the encoding setting: script-scoped `$PSDefaultParameterValues['Out-File:Encoding']` affects only `Out-File`/redirection; `results.json` (`Set-Content`) keeps its prior encoding, so no unintended serialization change.

## 5. Native stderr and error-preference analysis

`$ErrorActionPreference` is saved (`$probeErrorAction`), set to `Continue` for the native capture only, and restored in `finally`. The capture, `$LASTEXITCODE`, diagnostic read and `$ok` assertion are the only affected code; the `throw "Probe failed…"` is unchanged and is independent of the preference. A verifier-FAIL probe (`stderr` `RESULT: FAIL`) is now captured and evaluated instead of aborting.

## 6. Exit-code and diagnostic-control matrix

Exact canonical `Invoke-Probe` (extracted verbatim; verified to contain the real capture) driven by disposable `dotnet.cmd` stubs:

| Scenario | Expected | Observed |
|---|---|---|
| exit 1 + `GAGAMBA002` (negative control) | accepted | `ok=True` |
| exit 0 despite `GAGAMBA002` | rejected | `ok=False` (fail closed) |
| exit 1 + `GAGAMBA999` (wrong diagnostic) | rejected | `ok=False` |
| exit 1 + no diagnostic | rejected | `ok=False` |
| exit 0, success expected | accepted | `ok=True` |
| exit 1, success expected | rejected | `ok=False` |
| `FAIL` on stderr (verifier-style) | accepted | `ok=True`, no abort |
| exit 2 + `GAGAMBA002` | existing semantics (`$code -ne 0`) | `ok=True` (documented characteristic) |

`$ErrorActionPreference` was observed back at `Stop` after every case, including the throwing ones.

## 7. Failure-path restoration results

Launch-failure / stale-exit control (`q1-launch-failure.ps1`, canonical function):

```
stale_last_exit_code=1
launch_failure threw=True eapBefore=Stop eapAfter=Stop rows_recorded=0
false_pass_possible=False
```

An unresolvable `dotnet` throws before `$ok` is computed and before any row is recorded, so a stale non-zero `$LASTEXITCODE` cannot create a false PASS; the preference is restored by `finally`. A missing/unreadable log (or failed log write) is likewise outside the relaxed window and fails closed under the restored `Stop`.

## 8. Full Windows PowerShell 5.1 harness execution

Independently reproduced:

- Command: `powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1 -EvidenceDirectory <tmp>`.
- PowerShell version: **5.1.26100.9711**.
- Result: **All 51 build/binary probes passed**, script exit **0**; `results.json` total 51, failed 0; no skipped probes; the script reached its final `inspect-restored-release` step.
- Key negatives: `missing-symbol` exit 1 / `GAGAMBA002`; `unknown-configuration` `GAGAMBA001`; `debug-forced-production` `GAGAMBA003`; `debug-fixture-symbol-alone` `GAGAMBA006`; `stale-instrumented-binary-inspection` / `stale-instrumented-no-build-publish` exit 1 / `FAIL`.

## 9. Standalone verifier preservation

- `eng/VerifyProductionAssembly/Program.cs` is identical to the F-03 baseline (`git diff` empty).
- RELEASE assembly → `RESULT: PASS`, exit 0 (0/4 hook types, 0/3 hook properties, 14/14 core types).
- Debug assembly inspected as production → `RESULT: FAIL`, exit 1, listing the unchanged hook surfaces (including `PrepublicationIntegrityProjection::ForTest`). No forbidden hook became acceptable; no new public authority.

## 10. PowerShell 7 compatibility assessment

`pwsh` is **not available** in this environment; PowerShell 7 was not executed. The correction uses only explicit `-Encoding UTF8` and `$PSDefaultParameterValues['Out-File:Encoding']`, which are supported in both PowerShell 5.1 and 7 (differing only in BOM emission, which the explicit read handles). Cross-version compatibility is by inspection, not execution, and is recorded as a non-blocking limitation. PowerShell 5.1 — the required platform — passed in full.

## 11. Security and scope regression

- Non-`docs` diff: only `eng/test-production-build.ps1`.
- No runtime production or test source changed; no verifier implementation or policy changed; no security diagnostic weakened; no E-1 authority boundary, P4 protection/refusal path, or public API changed; no positive validation enabled.
- Preserved: E-1 accepted with qualifications; F-01 closed with qualifications; F-02 open; P4 refusal-only boundaries; `NATIVE-PENDING`; all 331 unqualified native skips.

## 12. Findings and qualifications

- **F-03-A (corrected).** Mixed-encoding capture: independently reproduced and corrected; diagnostic detection verified.
- **F-03-B (corrected).** Native stderr abort: independently reproduced and corrected; verifier-FAIL probes now evaluated.
- **Q1-R1 (non-blocking limitation).** PowerShell 7 compatibility is unexecuted.
- **Q1-R2 (retained characteristic).** Negative controls accept any non-zero exit status (`$code -ne 0`), not strictly `1`. This is the existing harness semantics, unchanged by F-03; no reachable false-PASS path was demonstrated (exit 0 + diagnostic is rejected).
- **Q1-R3 (observation).** The error-preference relaxation is a second harness correction beyond the encoding defect; it is narrowly scoped, `finally`-restored, and verified.

No demonstrated harness correctness defect, false-PASS path, lost exit status, uncontained invocation failure, or weakened security assertion remains.

## 13. Formal Q1 disposition

**SUPPORTED WITH QUALIFICATIONS.** Both F-03 defects are demonstrated corrected within the required Windows PowerShell 5.1 boundary; exit-code and diagnostic assertions remain authoritative and strict for the cases tested; the full harness completes 51/51; the standalone verifier and its policy are unchanged. Residuals are bounded and non-blocking (Q1-R1–R3).

## 14. Recommendation

Recommend the separate owner decision `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-OWNER-ACCEPTANCE-01` to evaluate this remediation and Q1 evidence; only that decision may formally close F-03. Do not begin E-2, Slice N, or any other milestone automatically.
