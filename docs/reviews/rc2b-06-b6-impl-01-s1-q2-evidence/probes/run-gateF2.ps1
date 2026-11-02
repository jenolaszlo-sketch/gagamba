param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$trxRoot = Join-Path $Rev 'work\trx-f2'
New-Item -ItemType Directory -Force $trxRoot | Out-Null
$projects = @(
  @('f-m1-release-runner', 'tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj'),
  @('f-d64-release-runner', 'tests\Gagamba.Execution.Linux.M1.D6_4.Tests\Gagamba.Execution.Linux.M1.D6_4.Tests.csproj')
)
foreach ($p in $projects) {
  $name = $p[0]; $proj = Join-Path $r $p[1]
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet test $proj -c Release --nologo --logger "trx;LogFileName=$name.trx" --results-directory $trxRoot *> $log
  $code = $LASTEXITCODE
  $summary = (Select-String $log -Pattern 'passed!|failed!|Total tests' | Select-Object -Last 2 | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
  Write-Output ("{0}: EXIT={1} SUMMARY={2}" -f $name, $code, $summary)
}
# Supplemental actual Release M1 candidate suite (producer-provided script from reviewed source)
$ev = Join-Path $Rev 'work\supplemental-evidence'
pwsh -NoProfile -ExecutionPolicy Bypass -File (Join-Path $r 'eng\test-production-regressions.ps1') -EvidenceDirectory $ev *> (Join-Path $Rev 'evidence\f-supplemental.log')
$code2 = $LASTEXITCODE
$summary2 = (Select-String (Join-Path $Rev 'evidence\f-supplemental.log') -Pattern 'passed!|failed!|Total tests' | Select-Object -Last 2 | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
Write-Output ("f-supplemental: EXIT={0} SUMMARY={1}" -f $code2, $summary2)
