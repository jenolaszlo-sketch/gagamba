# Slice E-1 F-03 remediation — reproducible tooling evidence

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F03-REMEDIATION-01`
Corrected script commit: `52037fa5738609b1469089835380a0cc94e047f1` (eng subtree `544808fbca2f87127d9e402bbd103a2f6a35d2ba`), baseline `1ed1fc706ea76954e4f1d7d43673048ef82072ec`.

These are disposable verification-tooling probes and their observed output. No Gagamba runtime production or test source is involved.

## Contents

- `repro-encoding.ps1` / `repro-encoding-results.txt` — pre-fix vs corrected log-capture encoding on the `missing-symbol` build under Windows PowerShell 5.1. Pre-fix: `containsGAGAMBA002=False` (mixed encoding, first bytes `64 6F 74 6E`). Corrected: `containsGAGAMBA002=True`, first bytes `EF BB BF 64` (UTF-8).
- `repro-stderr.ps1` / `repro-stderr-results.txt` — pre-fix vs corrected handling of a verifier that writes `RESULT: FAIL` to stderr. Pre-fix: `threw=True` under `$ErrorActionPreference='Stop'`. Corrected: `exit=1 containsFAIL=True`.
- `failure-controls.ps1` / `failure-controls-results.txt` — disposable stubs driven through a replica of the corrected capture/assertion semantics: correct (exit 1, `GAGAMBA002`) `ok=True`; exit 0 despite the diagnostic `ok=False`; wrong diagnostic `ok=False`; missing diagnostic `ok=False`.
- `harness-summary.txt` — the full `eng/test-production-build.ps1` run under Windows PowerShell 5.1.26100.9711 (`total=51 failed=0`, per-probe table).
- `missing-symbol-log-head.txt` — the corrected `missing-symbol.log`: first bytes `EF BB BF 64`, `containsGAGAMBA002=True`.

## Reproduce

```text
powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1 -EvidenceDirectory <tmp>
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/repro-encoding.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/repro-stderr.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/failure-controls.ps1
```

## Limitations

Windows PowerShell 5.1 is the target and was executed in full. PowerShell 7 (`pwsh`) is not installed in this environment, so cross-version compatibility is by construction (explicit `Out-File`/`Set-Content`/`Get-Content` UTF-8) rather than executed. The evidence scripts use a machine-local repository path; adjust as needed.
