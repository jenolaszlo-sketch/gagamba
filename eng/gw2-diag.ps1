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
$rich2 = @{}
foreach ($k in $rich.Keys) { $rich2[$k] = $rich[$k] }
$extra = @{
    ProgramData = $env:ProgramData
    ProgramFiles = ${env:ProgramFiles}
    'ProgramFiles(x86)' = ${env:ProgramFiles(x86)}
    CommonProgramFiles = $env:CommonProgramFiles
    'CommonProgramFiles(x86)' = ${env:CommonProgramFiles(x86)}
    ALLUSERSPROFILE = $env:ALLUSERSPROFILE
    PUBLIC = $env:PUBLIC
    USERNAME = $env:USERNAME
    USERDOMAIN = $env:USERDOMAIN
    LOGONSERVER = $env:LOGONSERVER
    PROCESSOR_ARCHITEW6432 = $env:PROCESSOR_ARCHITEW6432
    PROCESSOR_IDENTIFIER = $env:PROCESSOR_IDENTIFIER
    PROCESSOR_LEVEL = $env:PROCESSOR_LEVEL
    PROCESSOR_REVISION = $env:PROCESSOR_REVISION
    DriverData = $env:DriverData
    OneDrive = $env:OneDrive
    ChocolateyInstall = $env:ChocolateyInstall
    VCPKG_INSTALLATION_ROOT = $env:VCPKG_INSTALLATION_ROOT
    RUNTIME_IDENTIFIER = $env:RUNTIME_IDENTIFIER
    RUNNER_TEMP = $env:RUNNER_TEMP
    RUNNER_TOOL_CACHE = $env:RUNNER_TOOL_CACHE
    RUNNER_OS = $env:RUNNER_OS
    ImageOS = $env:ImageOS
    '=C:' = 'C:'
}
foreach ($k in $extra.Keys) { if ($extra[$k]) { $rich2[$k] = $extra[$k] } }
Test-Launch 'rich2' $rich2
$rich3 = @{}
foreach ($k in $rich2.Keys) { $rich3[$k] = $rich2[$k] }
$rich3['ComSpec'] = $env:ComSpec
Test-Launch 'rich3+ComSpec' $rich3
$a = @{}; foreach ($k in $rich3.Keys) { $a[$k] = $rich3[$k] }
$a['PSModuleAnalysisCachePath'] = $env:PSModuleAnalysisCachePath
Test-Launch 'a+PSModuleAnalysisCachePath' $a
$b = @{}; foreach ($k in $rich3.Keys) { $b[$k] = $rich3[$k] }
$b['PSModulePath'] = $env:PSModulePath
Test-Launch 'b+fullPSModulePath' $b
$c = @{}; foreach ($k in $rich3.Keys) { $c[$k] = $rich3[$k] }
$c['POWERSHELL_DISTRIBUTION_CHANNEL'] = $env:POWERSHELL_DISTRIBUTION_CHANNEL
Test-Launch 'c+POWERSHELL_DIST' $c
$inh = @{}
foreach ($k in [Environment]::GetEnvironmentVariables().Keys) { $inh[$k] = [Environment]::GetEnvironmentVariable($k) }
Test-Launch 'inherited' $inh
Write-Host 'diag done'
