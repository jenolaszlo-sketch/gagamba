param([Parameter(Mandatory)][string]$Repository)
$ErrorActionPreference = 'Stop'
$run = Join-Path $PSScriptRoot 'run'
$logs = Join-Path $run 'logs'
$codeName = 'Gagamba.Execution.Linux.M1'
$q3 = Join-Path $Repository 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence'
$solo = Split-Path -Parent $PSScriptRoot
$original = Join-Path $solo 'gagamba-e03-codec-r1-q2/run/original/bin'
$preservedR1 = Join-Path $solo 'gagamba-e03-codec-r2-q3/run/findings-r1/bin/Findings'
$r1 = Join-Path $run 'r1-bin'
if (!(Test-Path "$run/preflight.json") -or !(Test-Path "$logs/build-exits.txt")) { throw 'Missing Q4 preflight or build evidence' }
$originalBindings = Get-Content -Raw -LiteralPath "$q3/raw/probe-bindings.json" | ConvertFrom-Json
foreach ($config in @('debug','release')) {
    $originalCore = "$original/$codeName/$config/$codeName.dll"
    $binding = $originalBindings | Where-Object label -eq "Q1 replay original $config"
    if ((Get-FileHash -LiteralPath $originalCore).Hash -ne $binding.sha256) { throw "Original $config identity mismatch" }
    $target = "$r1/$codeName/$config"
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    foreach ($dll in @("$codeName.dll",'Gagamba.Execution.dll')) {
        Copy-Item -LiteralPath "$preservedR1/$config/$dll" -Destination $target
    }
}
Set-Location -LiteralPath $Repository
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
@("Windows $([Environment]::OSVersion.Version)", "Architecture $([Runtime.InteropServices.RuntimeInformation]::OSArchitecture)", 'dotnet --version') | Set-Content "$logs/environment.txt"
dotnet --version >> "$logs/environment.txt"
"EXIT $LASTEXITCODE" | Add-Content "$logs/environment.txt"
'dotnet --list-runtimes' | Add-Content "$logs/environment.txt"
dotnet --list-runtimes >> "$logs/environment.txt"
"EXIT $LASTEXITCODE" | Add-Content "$logs/environment.txt"
"& '$q3/Run-Q3.ps1' -Repository '$Repository' -Run '$run' -OriginalBin '$original' -R1Bin '$r1'" | Set-Content "$logs/replays.log"
try {
    & "$q3/Run-Q3.ps1" -Repository $Repository -Run $run -OriginalBin $original -R1Bin $r1 *>> "$logs/replays.log"
    'EXIT 0' | Add-Content "$logs/replays.log"
} catch {
    $_ | Out-String | Add-Content "$logs/replays.log"
    'EXIT 1' | Add-Content "$logs/replays.log"
    Get-Content "$logs/replays.log" -Tail 35
    throw
}
Get-Content "$logs/replays.log" -Tail 25
