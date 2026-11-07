$ErrorActionPreference = 'Stop'
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$repo = 'C:\Users\Laszlos\source\repos\Gagamba'
$dir = Join-Path $env:TEMP ('f03q1-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $dir | Out-Null
$stubDir = Join-Path $dir 'bin'; New-Item -ItemType Directory -Force $stubDir | Out-Null

# Disposable `dotnet` stub: behaviour driven by env vars.
[IO.File]::WriteAllText((Join-Path $stubDir 'dotnet.cmd'),
"@echo off`r`nif not `"%STUB_DIAG%`"==`"`" echo error %STUB_DIAG% 1>&2`r`nexit /b %STUB_EXIT%`r`n",
[Text.Encoding]::ASCII)
$env:PATH = $stubDir + ';' + $env:PATH

# Extract the CANONICAL Invoke-Probe verbatim from the committed script.
$canon = Get-Content (Join-Path $repo 'eng/test-production-build.ps1')
$start = ($canon | Select-String -Pattern '^function Invoke-Probe').LineNumber
$end = ($canon | Select-String -Pattern '^function Inspect-M1').LineNumber - 1
$fnText = ($canon[($start - 1)..($end - 1)] -join "`n")
Invoke-Expression $fnText
Write-Output ("canonical-function-extracted lines=" + ($end - $start) + " contains-dotnet-call=" + $fnText.Contains('& dotnet @Arguments >> $log 2>&1'))

$EvidenceDirectory = $dir
$rows = [Collections.Generic.List[object]]::new()

function Run-Case([string]$name,[int]$stubExit,[string]$stubDiag,[string]$expected) {
    $env:STUB_EXIT = "$stubExit"; $env:STUB_DIAG = $stubDiag
    $before = $ErrorActionPreference
    $threw = $false
    try { Invoke-Probe $name @('build','x') $expected } catch { $threw = $true }
    $passed = if ($rows.Count -gt 0) { $rows[$rows.Count - 1].Passed } else { $null }
    $after = $ErrorActionPreference
    Write-Output ("CASE $name stubExit=$stubExit diag='$stubDiag' expected='$expected' ok=$passed threw=$threw eapBefore=$before eapAfter=$after")
}

Run-Case 'q1-negative-correct' 1 'GAGAMBA002' 'GAGAMBA002'
Run-Case 'q1-zero-despite-diag' 0 'GAGAMBA002' 'GAGAMBA002'
Run-Case 'q1-wrong-diag' 1 'GAGAMBA999' 'GAGAMBA002'
Run-Case 'q1-missing-diag' 1 '' 'GAGAMBA002'
Run-Case 'q1-exit2-with-diag' 2 'GAGAMBA002' 'GAGAMBA002'
Run-Case 'q1-success-expected-ok' 0 '' ''
Run-Case 'q1-success-expected-but-nonzero' 1 '' ''
Run-Case 'q1-stderr-fail-control' 1 'FAIL' 'FAIL'

Remove-Item $dir -Recurse -Force
