$ErrorActionPreference = 'Stop'
$rev = 'C:\Users\Laszlos\source\repos\Solo\b6-impl-01-s1-q2-review'
$r = Join-Path $rev 'src-remediated'
$files = @(
  @('m1-release', "$r\src\Gagamba.Execution.Linux.M1\bin\Release\net10.0\Gagamba.Execution.Linux.M1.dll"),
  @('owner-release', "$r\src\Gagamba.Execution.Linux.M1.Owner\bin\Release\net10.0\Gagamba.Execution.Linux.M1.Owner.dll"),
  @('recovery-release', "$r\src\Gagamba.Execution.Linux.M1.Recovery\bin\Release\net10.0\Gagamba.Execution.Linux.M1.Recovery.dll"),
  @('execution-release', "$r\src\Gagamba.Execution\bin\Release\net10.0\Gagamba.Execution.dll"),
  @('m1-debug-instrumented', "$r\src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll"),
  @('owner-debug', "$r\src\Gagamba.Execution.Linux.M1.Owner\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.Owner.dll"),
  @('d64-testfixture', "$r\src\Gagamba.Execution.Linux.M1\bin\d6-4-variant\Debug\net10.0\Gagamba.Execution.Linux.M1.TestFixture.dll"),
  @('m1-tests-release-closure-copy', "$r\tests\Gagamba.Execution.Linux.M1.Tests\bin\Release\net10.0\Gagamba.Execution.Linux.M1.dll"),
  @('m1-test-config-e9', "$rev\work\e9-dbg-artifacts\bin\Gagamba.Execution.Linux.M1\test\Gagamba.Execution.Linux.M1.dll"),
  @('m1-release-fresh-e3', "$rev\work\e3-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll"),
  @('m1-release-fresh-e5', "$rev\work\e5-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll"),
  @('m1-release-fresh-e9', "$rev\work\e9-rel-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll"),
  @('published-m1-c15', "$rev\work\c15-publish\Gagamba.Execution.Linux.M1.dll"),
  @('unsafe-baseline-a2', "$rev\work\a2-before-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll")
)
$out = @()
foreach ($f in $files) {
  if (Test-Path $f[1]) {
    $h = (Get-FileHash $f[1] -Algorithm SHA256).Hash
    $sz = (Get-Item $f[1]).Length
    $out += ("{0}  {1}  {2}" -f $h, $sz, $f[0])
    Write-Output ("{0} {1} {2}" -f $f[0], $sz, $h)
  } else {
    $out += ("MISSING  {0}" -f $f[0])
    Write-Output ("{0} MISSING" -f $f[0])
  }
}
$out | Set-Content (Join-Path $rev 'evidence\artifact-hashes.txt')
"--- closure contamination check (owner/recovery Release bins) ---"
foreach ($d in @("$r\src\Gagamba.Execution.Linux.M1.Owner\bin\Release\net10.0", "$r\src\Gagamba.Execution.Linux.M1.Recovery\bin\Release\net10.0")) {
  $bad = Get-ChildItem $d -Filter '*.dll' | Where-Object { $_.Name -match 'Tests|TestFixture|B2Controller|VerifyProductionAssembly' }
  Write-Output ("{0}: suspicious-assemblies={1}; dll-count={2}" -f (Split-Path (Split-Path $d -Parent) -Parent) -replace '.*\\','' , ($bad | Measure-Object).Count, (Get-ChildItem $d -Filter '*.dll' | Measure-Object).Count)
}
