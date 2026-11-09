# P0 reproducibility commands

Date: 2026-11-09 (Asia/Manila)
Baseline: `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe` / tree `934bf6567e1ee10bff59d3669bf0ca2a95c6d6d3`

Run from `C:\Users\Laszlos\source\repos\Gagamba`. The local external package is under `C:\Users\Laszlos\source\repos\Solo\`; it is not part of the repository.

## 1. Baseline

```powershell
git rev-parse HEAD
git rev-parse 'HEAD^{tree}'
git branch --show-current
git worktree list
git status --porcelain -uall
git rev-parse origin/main
git rev-list --count origin/main..main
```

Expected: HEAD `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`; tree `934bf6567e1ee10bff59d3669bf0ca2a95c6d6d3`; branch `main`; one worktree; 44 historical untracked paths plus the additive P0 documentation paths; `origin/main` `53850aa158cf26f5a40fb9bd51c606a111188ef3`; local `main` 16 ahead.

## 2. Preserve and verify the 44 historical untracked paths

```powershell
$hist = Get-Content 'docs\evidence\rc2b-06-b6-native-input-freeze-01\untracked-before.json' -Raw | ConvertFrom-Json
$hist = $hist | Sort-Object path
$diff = @()
$lines = @()
$map = [ordered]@{}
foreach ($e in $hist) {
  if (-not (Test-Path -LiteralPath $e.path)) { $diff += "MISSING`t$($e.path)"; continue }
  $h = (Get-FileHash -LiteralPath $e.path -Algorithm SHA256).Hash.ToLower()
  if ($h -ne $e.sha256) { $diff += "HASH`t$($e.path)" }
  $lines += "$($e.path)`t$h"
  $map[$e.path] = $h
}
$linesText = ($lines -join "`n") + "`n"
$lineBytes = [System.Text.Encoding]::UTF8.GetBytes($linesText)
$mapBytes = [System.Text.Encoding]::UTF8.GetBytes(($map | ConvertTo-Json -Compress))
$sha = [System.Security.Cryptography.SHA256]::Create()
'lines_digest=' + ([BitConverter]::ToString($sha.ComputeHash($lineBytes)) -replace '-', '').ToLower()
'map_digest=' + ([BitConverter]::ToString($sha.ComputeHash($mapBytes)) -replace '-', '').ToLower()
"historical_count=$($hist.Count) diff_count=$($diff.Count)"
$diff
```

Expected:

```text
lines_digest=de3cf84e99a472ebaa561260254db0b57b87495de52ad89e6593675f7e39bfb1
map_digest=b6e439f7f9da8b3f9b6004d0ad72210c923ba80f3f4f8ba2c7cdc66a417a9327
historical_count=44 diff_count=0
```

## 3. F02 frozen manifest, recheck, and 15 files

```powershell
$f02 = 'C:\Users\Laszlos\source\repos\Solo\gagamba-f02-authority-mechanism-decision-01'
Get-FileHash "$f02\recheck-input-SHA256SUMS.txt" -Algorithm SHA256
Get-FileHash "$f02\independent-recheck.md" -Algorithm SHA256
Get-Content "$f02\recheck-input-SHA256SUMS.txt" | Where-Object { $_ -match '\S' } | ForEach-Object {
  $p = $_ -split '\s+', 2
  $file = $p[1].Trim()
  $actual = (Get-FileHash (Join-Path $f02 $file) -Algorithm SHA256).Hash.ToLower()
  [pscustomobject]@{
    Match = ($actual -eq $p[0])
    Expected = $p[0]
    Actual = $actual
    Size = (Get-Item (Join-Path $f02 $file)).Length
    File = $file
  }
}
```

Expected: manifest `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`; recheck `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6`; all 15 `Match=True`.

## 4. F02 owner-package support

```powershell
$own = 'C:\Users\Laszlos\source\repos\Solo\gagamba-f02-authority-mechanism-owner-01'
@(
  'final-design-binding.md',
  'accepted-contract-and-qualification-matrix.md',
  'negative-test-reconciliation.md',
  'implementation-authorization-boundary.md'
) | ForEach-Object {
  Get-FileHash (Join-Path $own $_) -Algorithm SHA256
}
Get-FileHash "$f02\SHA256SUMS.txt" -Algorithm SHA256
Get-FileHash "$f02\independent-review.md" -Algorithm SHA256
```

Expected: `5755748b...`, `d8ba4a64...`, `9deeb26b...`, `9eb472d6...`, `a9c5dc71...`, `f09bc803...`.

## 5. Alternative B inputs

```powershell
$int = 'C:\Users\Laszlos\source\repos\Solo\gagamba-integrated-start-authority-owner-decision-01'
$q1 = 'C:\Users\Laszlos\source\repos\Solo\gagamba-integrated-start-authority-owner-decision-q1'
Get-FileHash "$int\validation-bootstrap-compatibility.md" -Algorithm SHA256
Get-FileHash "$int\implementation-and-test-handoff.md" -Algorithm SHA256
Get-FileHash "$q1\validation-bootstrap-assessment.md" -Algorithm SHA256
```

Expected: `896532d7...`, `9a1a195b...`, `3c4307f6...`.

## 6. Committed record hashes

```powershell
@(
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-entry-01-evidence/README.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f01-owner-acceptance-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f03-owner-acceptance-01.md',
  'docs/decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md',
  'docs/decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md',
  'docs/plans/rc2b-06-b6-integrated-start-authority-impl-01.md',
  'docs/decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md',
  'docs/decisions/rc2b-06-b6-s2-f02-trust-store-owner-decision.md',
  'docs/design/rc2b-06-b6-installed-artifact-trust.md',
  'docs/remediation-evidence-standard.md',
  'docs/reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md'
) | ForEach-Object {
  $h = (Get-FileHash $_ -Algorithm SHA256).Hash.ToLower()
  $s = (Get-Item $_).Length
  "$h`t$s`t$_"
}
```

Expected: values in `digest-verification.txt` and `record-hashes.txt`.

## 7. Acceptance ancestry

```powershell
@(
  '4aab87761078a92d05cd08aa1998cbe2c4f6aaf1',
  'd804d9062bab5d9d540df5c2d64f1b2c31bdd667',
  '142cc2308dfbe7fbab5231e1db0a6fcecc9922e6',
  'c7b76210928e1c6e14ea9bd64b2b5016d1b21f2e',
  '8bd71425eb31b69290affa2b86c78afce1bd0882',
  '1ed1fc706ea76954e4f1d7d43673048ef82072ec',
  'e86254e60fb3ded770156519e1d3e15e73bf36fc',
  '6d08ae569c03e96872fcc5d330435991952207d6',
  '2d39380d0c0b61753097c197ce7c1df03cdddcb7',
  'a35ce108d3f65c1a560a4b01ef91fa3d9ade900d',
  '504b6d06ac88591072b11f428ae88cade390c075'
) | ForEach-Object {
  git merge-base --is-ancestor $_ HEAD
  "$_ ancestor_exit=$LASTEXITCODE"
}
```

Expected: every `ancestor_exit=0`.

## 8. Source and authority state

```powershell
git diff --stat 4c5a42f0fecff90dcb30088ca0203956e7a80032 HEAD -- src tests eng
Get-ChildItem src,tests,eng -Recurse -File -Include *.cs,*.ps1,*.py,*.sh,*.csproj,*.json,*.yml,*.yaml |
  Select-String -Pattern 'accepted-validation-catalogs|validation-catalog|GAVA|validation-pre|fixtureManifestSha256|fixturePolicySha256|actionScriptSha256|catalogApprovalSha256|ownerCatalogAcceptanceSha256|catalogReviewSha256'
```

Expected: empty diff; `NO MATCHES` for the E-2/GAVA1/v3 schema symbols.

## 9. P0 deliverable hashes

```powershell
@(
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md',
  'docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/README.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/baseline-and-preservation.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/digest-verification.txt',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/acceptance-mapping.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/schema-extraction-map.md',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/source-and-authority-state.txt',
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0-evidence/reproducibility-commands.md'
) | ForEach-Object {
  $h = (Get-FileHash $_ -Algorithm SHA256).Hash.ToLower()
  $s = (Get-Item $_).Length
  "$h`t$s`t$_"
}
```

Expected: values in `record-hashes.txt`. The record-hashes file itself is intentionally not self-hashed.

## 10. Link and claim checks

```powershell
# Every relative Markdown link in the P0 report and proposed record should resolve.
$files = @(
  'docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p0.md',
  'docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md'
)
foreach ($file in $files) {
  $dir = Split-Path $file -Parent
  $text = Get-Content $file -Raw
  [regex]::Matches($text, '\]\(([^)]+)\)') | ForEach-Object {
    $target = $_.Groups[1].Value
    if ($target -notmatch '^(https?:|#)') {
      $path = Join-Path $dir ($target.Split('#')[0])
      if (-not (Test-Path -LiteralPath $path)) { "MISSING`t$file`t$target" }
    }
  }
}
```

Expected: no `MISSING` lines.

The P0 report contains the formal disposition phrase exactly once, in §17. The proposed record does not claim owner acceptance, schema adoption, catalog approval, trust-anchor creation, or runtime authority.
