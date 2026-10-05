$ErrorActionPreference = 'Continue'
$d = Join-Path $env:RUNNER_TEMP 'gw2diag'
Remove-Item -Recurse -Force $d -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $d | Out-Null
$holder = Join-Path $d 'holder.ps1'
# Same shape as the GW-2 test fixture: acquire an exclusive lock, sleep.
Set-Content -Path $holder -Value "param([string]`$Dir,[string]`$Lock)`n[System.IO.File]::Open((Join-Path `$Dir `$Lock),'OpenOrCreate','Write','None') | Out-Null`nStart-Sleep -Seconds 30"

$pwsh = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
Write-Host "pwsh=$pwsh exists=$(Test-Path $pwsh) sysdir=$([Environment]::SystemDirectory) os=$([Environment]::OSVersion.VersionString)"

function Test-Launch([string]$label, [hashtable]$envVars) {
    $lockName = "$label.lock"
    $lock = Join-Path $d $lockName
    Remove-Item $lock -ErrorAction SilentlyContinue
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $pwsh
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$holder`" -Dir `"$d`" -Lock `"$lockName`""
    $psi.WorkingDirectory = $d
    $psi.UseShellExecute = $false
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardOutput = $true
    $psi.Environment.Clear()
    foreach ($k in $envVars.Keys) { $psi.Environment[$k] = $envVars[$k] }
    $p = [System.Diagnostics.Process]::Start($psi)
    Start-Sleep -Seconds 8
    $held = $false
    try {
        $fs = [System.IO.File]::Open($lock, 'OpenOrCreate', 'ReadWrite', 'None')
        $fs.Close()
    } catch { $held = $true }
    $exited = $p.HasExited
    $exitCode = if ($exited) { $p.ExitCode } else { 'n/a' }
    Write-Host "[$label] fileExists=$(Test-Path $lock) locked=$held exited=$exited exit=$exitCode"
    if ($exited) {
        Write-Host "[$label] stderr=$($p.StandardError.ReadToEnd())"
        Write-Host "[$label] stdout=$($p.StandardOutput.ReadToEnd())"
    }
    if (-not $exited) { $p.Kill() }
}

Test-Launch 'empty' @{}
Test-Launch 'minimal' @{ SYSTEMROOT = $env:SystemRoot; SYSTEMDRIVE = 'C:' }
$rich = @{
    SYSTEMROOT = $env:SystemRoot
    SYSTEMDRIVE = 'C:'
    WINDIR = $env:WINDIR
    PATH = "$env:SystemRoot\System32;$env:SystemRoot\System32\WindowsPowerShell\v1.0"
    PATHEXT = '.COM;.EXE;.BAT;.CMD'
    TEMP = $env:TEMP
    TMP = $env:TMP
    USERPROFILE = $env:USERPROFILE
    HOMEDRIVE = $env:HOMEDRIVE
    HOMEPATH = $env:HOMEPATH
    APPDATA = $env:APPDATA
    LOCALAPPDATA = $env:LOCALAPPDATA
    COMPUTERNAME = $env:COMPUTERNAME
    NUMBER_OF_PROCESSORS = $env:NUMBER_OF_PROCESSORS
    PROCESSOR_ARCHITECTURE = $env:PROCESSOR_ARCHITECTURE
    PSModulePath = "$env:SystemRoot\system32\WindowsPowerShell\v1.0\Modules"
    OS = $env:OS
}
Test-Launch 'rich' $rich
$inh = @{}
foreach ($k in [Environment]::GetEnvironmentVariables().Keys) { $inh[$k] = [Environment]::GetEnvironmentVariable($k) }
Test-Launch 'inherited' $inh
Write-Host 'diag done'
