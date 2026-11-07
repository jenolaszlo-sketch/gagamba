$ErrorActionPreference = 'Stop'
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$repo = 'C:\Users\Laszlos\source\repos\Gagamba'
$dir = Join-Path $env:TEMP ('f03q1lf-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $dir | Out-Null
$emptyDir = Join-Path $dir 'bin'; New-Item -ItemType Directory -Force $emptyDir | Out-Null

# Extract the CANONICAL Invoke-Probe verbatim.
$canon = Get-Content (Join-Path $repo 'eng/test-production-build.ps1')
$start = ($canon | Select-String -Pattern '^function Invoke-Probe').LineNumber
$end = ($canon | Select-String -Pattern '^function Inspect-M1').LineNumber - 1
Invoke-Expression ($canon[($start - 1)..($end - 1)] -join "`n")

$EvidenceDirectory = $dir
$rows = [Collections.Generic.List[object]]::new()

# Establish a stale nonzero $LASTEXITCODE from a previous process.
& cmd.exe /c 'exit /b 1' 2>$null | Out-Null
Write-Output ("stale_last_exit_code=" + $LASTEXITCODE)

# Remove dotnet from PATH so `& dotnet` cannot launch.
$savedPath = $env:PATH
$env:PATH = $emptyDir + ';' + (Join-Path $env:SystemRoot 'System32')
$eapBefore = $ErrorActionPreference
$threw = $false
try { Invoke-Probe 'q1-launch-failure' @('build','x') 'GAGAMBA002' } catch { $threw = $true }
$eapAfter = $ErrorActionPreference
$env:PATH = $savedPath

Write-Output ("launch_failure threw=$threw eapBefore=$eapBefore eapAfter=$eapAfter rows_recorded=" + $rows.Count)
$falsePass = ($rows.Count -gt 0 -and $rows[0].Passed)
Write-Output ("false_pass_possible=$falsePass")
Remove-Item $dir -Recurse -Force
