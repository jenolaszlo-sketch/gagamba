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

Write-Host "launch-spike: root=$root config=$Configuration"
Write-Host 'launch-spike: invokes the experimental Windows sandbox API under disposable identities; per-user profiles are deleted afterwards. No elevation, no host-wide changes.'
& dotnet build (Join-Path $root 'spikes/Gw1bLaunch/Gw1bLaunch.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "launch spike build failed ($LASTEXITCODE)" }

& dotnet run --no-build -c $Configuration --project (Join-Path $root 'spikes/Gw1bLaunch/Gw1bLaunch.csproj') -- --repo-root "$root" --out "$artifacts"
if ($LASTEXITCODE -ne 0) { throw "launch spike failed ($LASTEXITCODE): aggregate is not Passed" }

$latest = Get-ChildItem -LiteralPath $artifacts -Filter 'windows-launch-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'no launch report emitted' }
$report = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
Write-Host ("launch-spike: report={0} aggregate={1} passed={2}/{3} mandatoryComplete={4} cleanup={5}" -f
    $latest.Name, $report.summary.aggregate, $report.summary.passed, $report.summary.total,
    $report.summary.mandatoryComplete, $report.cleanup.status)
if ($report.summary.aggregate -ne 'Passed') { throw 'launch aggregate is not Passed' }
Write-Host 'launch-spike: Passed'
