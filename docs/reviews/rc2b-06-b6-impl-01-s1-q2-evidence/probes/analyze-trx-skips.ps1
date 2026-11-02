param([string]$Rev)
$ErrorActionPreference = 'Stop'
$trxRoot = Join-Path $Rev 'work\trx-f1'
$suppTrx = Join-Path $Rev 'work\trx-f2'

function Analyze([string]$path, [string]$label) {
  [xml]$doc = Get-Content $path
  $tests = $doc.TestRun.Results.UnitTestResult
  $skipped = $tests | Where-Object { $_.outcome -eq 'NotExecuted' }
  Write-Output ("== {0}: total={1} passed={2} failed={3} notexecuted={4}" -f $label, $tests.Count, ($tests | Where-Object { $_.outcome -eq 'Passed' }).Count, ($tests | Where-Object { $_.outcome -eq 'Failed' }).Count, $skipped.Count)
  $skipped | ForEach-Object {
    $reason = $_.Message
    if ($_.ResultMessage) { if (-not $reason) { $reason = $_.ResultMessage.'#text' } }
    $reason
  } | Group-Object { if ($_) { ($_ -replace '\d+', 'N') } else { '<no message>' } } | Sort-Object Count -Descending | ForEach-Object { ("  {0,4} x {1}" -f $_.Count, ($_.Name -split "`n")[0].Trim().Substring(0, [Math]::Min(150, ($_.Name -split "`n")[0].Trim().Length))) }
}

$m1 = Get-ChildItem $trxRoot -Filter 'f-m1-debug.trx' | Select-Object -First 1
Analyze $m1.FullName 'M1 Debug'
$lx = Get-ChildItem $trxRoot -Filter 'f-linux-debug.trx' | Select-Object -First 1
Analyze $lx.FullName 'Linux Debug'
$mac = Get-ChildItem $trxRoot -Filter 'f-macos-debug.trx' | Select-Object -First 1
Analyze $mac.FullName 'macOS Debug'
