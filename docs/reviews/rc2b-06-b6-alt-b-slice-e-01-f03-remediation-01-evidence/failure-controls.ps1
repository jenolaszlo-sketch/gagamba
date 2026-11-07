$ErrorActionPreference = 'Stop'
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$dir = Join-Path $env:TEMP ('f03ctl-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $dir | Out-Null

function New-Stub([string]$Name, [int]$ExitCode, [string]$Diagnostic) {
    $p = Join-Path $dir ($Name + '.cmd')
    $body = "@echo off`r`n"
    if ($Diagnostic) { $body += "echo error $Diagnostic 1>&2`r`n" }
    $body += "exit /b $ExitCode`r`n"
    [IO.File]::WriteAllText($p, $body, [Text.Encoding]::ASCII)
    return $p
}

# Replica of the CORRECTED Invoke-Probe capture + assertion expression, driven by
# disposable stubs instead of `dotnet`. Exercises the same semantics.
function Test-Probe([string]$Name, [string]$Stub, [string]$ExpectedDiagnostic) {
    $log = Join-Path $dir ($Name + '.log')
    ('stub ' + $Stub) | Set-Content -LiteralPath $log -Encoding UTF8
    $probeErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { & cmd.exe /c $Stub >> $log 2>&1; $code = $LASTEXITCODE }
    finally { $ErrorActionPreference = $probeErrorAction }
    $text = Get-Content -LiteralPath $log -Raw -Encoding UTF8
    $ok = if ($ExpectedDiagnostic) { $code -ne 0 -and $text.Contains($ExpectedDiagnostic) } else { $code -eq 0 }
    Write-Output "$Name exit=$code ok=$ok"
    return $ok
}

$correct = New-Stub 'correct' 1 'GAGAMBA002'
$zero = New-Stub 'zero' 0 'GAGAMBA002'
$wrong = New-Stub 'wrong' 1 'GAGAMBA999'
$missing = New-Stub 'missing' 1 ''

$r1 = Test-Probe 'stub-correct' $correct 'GAGAMBA002'
$r2 = Test-Probe 'stub-zero-despite-diagnostic' $zero 'GAGAMBA002'
$r3 = Test-Probe 'stub-wrong-diagnostic' $wrong 'GAGAMBA002'
$r4 = Test-Probe 'stub-missing-diagnostic' $missing 'GAGAMBA002'

Write-Output ("CONTROL correct_ok=" + $r1 + " zero_ok=" + $r2 + " wrong_ok=" + $r3 + " missing_ok=" + $r4)
Remove-Item $dir -Recurse -Force
