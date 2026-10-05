[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'
if (-not $PSBoundParameters.ContainsKey('RepositoryRoot')) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { throw 'RepositoryRoot must be a non-empty path.' }
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "probe: root=$root config=$Configuration"
& dotnet build (Join-Path $root 'spikes/Gw1aProbe/Gw1aProbe.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "probe build failed ($LASTEXITCODE)" }

& dotnet run --no-build -c $Configuration --project (Join-Path $root 'spikes/Gw1aProbe/Gw1aProbe.csproj') -- --repo-root "$root" --out "$artifacts"
if ($LASTEXITCODE -ne 0) { throw "probe failed ($LASTEXITCODE): CapabilityProbe aggregate is not Passed" }

$latest = Get-ChildItem -LiteralPath $artifacts -Filter 'windows-probe-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'no probe report emitted' }
$report = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
Write-Host ("probe: report={0} aggregate={1} passed={2}/{3} mandatoryComplete={4} cleanup={5}" -f
    $latest.Name, $report.summary.aggregate, $report.summary.passed, $report.summary.total,
    $report.summary.mandatoryComplete, $report.cleanup.status)
if ($report.summary.aggregate -ne 'Passed') { throw 'probe aggregate is not Passed' }
if ($report.evidenceKind -ne 'CapabilityProbe') { throw 'unexpected evidence envelope' }
Write-Host 'probe: Passed'
