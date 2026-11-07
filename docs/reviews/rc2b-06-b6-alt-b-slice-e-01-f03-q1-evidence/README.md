# Slice E-1 F-03 independent Q1 reviewer evidence

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-Q1`
Reviewed implementation: `52037fa5738609b1469089835380a0cc94e047f1` (eng subtree `544808fbca2f87127d9e402bbd103a2f6a35d2ba`), baseline `c14c8b47da129342c70fe0ab5cb1b337dff7a329` / tree `587ef13ef15303bbbfd19419ecdf7fa9352c100c`.

Independently authored reviewer probes, run under Windows PowerShell 5.1.26100.9711. Not producer artifacts.

## Contents

- `q1-probe-logic.ps1` / `probe-logic-results.txt` — the **exact canonical `Invoke-Probe`** (extracted verbatim from `eng/test-production-build.ps1` at run time and verified to contain the `& dotnet @Arguments >> $log 2>&1` capture) driven by a disposable `dotnet.cmd` stub for eight exit-code/diagnostic scenarios.
- `q1-launch-failure.ps1` / `launch-failure-results.txt` — stale non-zero `$LASTEXITCODE` followed by an unresolvable `dotnet`; shows the canonical function throws, records no probe row, and restores `$ErrorActionPreference`.
- `harness-summary.txt` — the full harness run under Windows PowerShell 5.1.26100.9711 (51 probes, 0 failed, per-probe table).
- `key-diagnostics.txt` — `missing-symbol.log` encoding and diagnostic location (diagnostic from actual MSBuild output, not the echoed header).
- `verifier-preservation.txt` — standalone verifier evidence (RELEASE PASS exit 0; Debug-as-production FAIL exit 1 with the unchanged hook list) and confirmation that `eng/VerifyProductionAssembly` source is identical to the F-03 baseline.

## Reproduce

```text
powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1 -EvidenceDirectory <tmp>
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/q1-probe-logic.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/q1-launch-failure.ps1
```

## Limitations

Windows PowerShell 5.1 was executed in full. PowerShell 7 (`pwsh`) is not installed, so cross-version compatibility is by inspection, not execution. The probe-logic script uses a machine-local repository path; adjust as needed. No Gagamba runtime production or test source was modified.
