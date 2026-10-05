$ErrorActionPreference = 'Continue'
$d = Join-Path $env:RUNNER_TEMP 'gw2diag'
Remove-Item -Recurse -Force $d -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $d | Out-Null
$holder = Join-Path $d 'holder.ps1'
# Instrumented: record param binding, then the lock attempt, then sleep.
$body = @'
param([string]$Dir,[string]$Lock)
Set-Content -Path (Join-Path $Dir 'params.txt') -Value "Dir=[$Dir] Lock=[$Lock] argc=$($args.Count)"
try {
  $f=[System.IO.File]::Open((Join-Path $Dir $Lock),'OpenOrCreate','Write','None')
  Set-Content -Path (Join-Path $Dir 'locked.txt') -Value 'yes'
} catch {
  Set-Content -Path (Join-Path $Dir 'lockerror.txt') -Value "$_"
}
Start-Sleep -Seconds 30
'@
Set-Content -Path $holder -Value $body

$pwsh = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
Write-Host "os=$([Environment]::OSVersion.VersionString)"

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
    $locked = $false
    try { $fs = [System.IO.File]::Open($lock, 'OpenOrCreate', 'ReadWrite', 'None'); $fs.Close() } catch { $locked = $true }
    $exited = $p.HasExited
    Write-Host "[$label] locked=$locked exited=$exited exit=$(if ($exited) { $p.ExitCode } else { 'n/a' })"
    foreach ($f in 'params.txt', 'locked.txt', 'lockerror.txt') {
        $fp = Join-Path $d $f
        if (Test-Path $fp) { Write-Host "[$label] $f=$((Get-Content $fp -Raw).Trim())" }
    }
    if (-not $exited) { $p.Kill() }
    Remove-Item (Join-Path $d 'params.txt'), (Join-Path $d 'locked.txt'), (Join-Path $d 'lockerror.txt') -ErrorAction SilentlyContinue
}

$rich = @{
    SYSTEMROOT = $env:SystemRoot; SYSTEMDRIVE = 'C:'; WINDIR = $env:WINDIR
    PATH = "$env:SystemRoot\System32;$env:SystemRoot\System32\WindowsPowerShell\v1.0"
    PATHEXT = '.COM;.EXE;.BAT;.CMD'; TEMP = $env:TEMP; TMP = $env:TMP
    USERPROFILE = $env:USERPROFILE; HOMEDRIVE = $env:HOMEDRIVE; HOMEPATH = $env:HOMEPATH
    APPDATA = $env:APPDATA; LOCALAPPDATA = $env:LOCALAPPDATA
    COMPUTERNAME = $env:COMPUTERNAME; NUMBER_OF_PROCESSORS = $env:NUMBER_OF_PROCESSORS
    PROCESSOR_ARCHITECTURE = $env:PROCESSOR_ARCHITECTURE
    PSModulePath = "$env:SystemRoot\system32\WindowsPowerShell\v1.0\Modules"; OS = $env:OS
}
Test-Launch 'rich' $rich
$inh = @{}
foreach ($k in [Environment]::GetEnvironmentVariables().Keys) { $inh[$k] = [Environment]::GetEnvironmentVariable($k) }
Test-Launch 'inherited' $inh
Write-Host 'diag done'
