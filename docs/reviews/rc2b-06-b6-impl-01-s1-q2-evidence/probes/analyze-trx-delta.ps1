param([string]$Rev)
$ErrorActionPreference = 'Stop'
$m1trx = Join-Path $Rev 'work\trx-f1\f-m1-debug.trx'
$reltrx = Join-Path $Rev 'work\trx-f2\f-m1-release-runner.trx'
$supp = Get-ChildItem (Join-Path $Rev 'src-remediated\work-artifacts\supplemental\trx') -Filter '*.trx' | Select-Object -First 1
$excluded = 'BindingTests','CopyInTests','NativeCopyInTests','IntegratedBarrierTests','NonRootQualificationTests'

foreach ($pair in @(@('M1-Debug', $m1trx), @('M1-Release-runner', $reltrx), @('Supplemental-Release-M1', $supp.FullName))) {
  [xml]$doc = Get-Content $pair[1]
  $defs = @{}
  foreach ($td in $doc.TestRun.TestDefinitions.UnitTest) { $defs[$td.id] = $td.TestMethod.className }
  $results = $doc.TestRun.Results.UnitTestResult
  $excl = $results | Where-Object { $excluded -contains ($defs[$_.testId] -replace '^.*\.','') }
  $b5 = $results | Where-Object { ($defs[$_.testId] -match 'B5') }
  Write-Output ("== {0}: total={1} passed={2} skipped={3} | excluded-classes: total={4} passed={5} skipped={6} | B5-classes: total={7} passed={8} skipped={9}" -f `
    $pair[0], $results.Count, ($results | Where-Object outcome -eq 'Passed').Count, ($results | Where-Object outcome -eq 'NotExecuted').Count,
    $excl.Count, ($excl | Where-Object outcome -eq 'Passed').Count, ($excl | Where-Object outcome -eq 'NotExecuted').Count,
    $b5.Count, ($b5 | Where-Object outcome -eq 'Passed').Count, ($b5 | Where-Object outcome -eq 'NotExecuted').Count)
  if ($pair[0] -eq 'M1-Debug') {
    $excl | ForEach-Object { $defs[$_.testId] } | Group-Object | Sort-Object Count -Descending | ForEach-Object { ("   {0,4} x {1}" -f $_.Count, $_.Name) }
  }
}
