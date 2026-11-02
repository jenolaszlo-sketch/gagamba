$ErrorActionPreference = 'Continue'
$rev = 'C:\Users\Laszlos\source\repos\Solo\b6-impl-01-s1-q2-review'
$r = Join-Path $rev 'src-remediated'
$ev = Join-Path $r 'work-artifacts\supplemental'
$log = Join-Path $rev 'evidence\f-supplemental.log'
& (Join-Path $r 'eng\test-production-regressions.ps1') -EvidenceDirectory $ev *> $log
$code = $LASTEXITCODE
$summary = (Select-String $log -Pattern 'passed!|failed!' | Select-Object -Last 2 | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
Write-Output ("f-supplemental: EXIT={0} SUMMARY={1}" -f $code, $summary)
