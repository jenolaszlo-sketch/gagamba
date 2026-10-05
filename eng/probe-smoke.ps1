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

Write-Host "probe-smoke: root=$root config=$Configuration"
Write-Host 'probe-smoke: proves the availability probe builds, runs end-to-end, and emits schema-valid evidence. Aggregate Passed is NOT a CI gate: GitHub-hosted windows-latest lacks the experimental processmodel.dll (BaseContainer tier), so the host verdict is expected to be MODULE-ABSENT. The strict eng/probe.ps1 remains the qualified-host (Windows 11 25H2+) gate.'
& dotnet build (Join-Path $root 'spikes/Gw1aProbe/Gw1aProbe.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "probe build failed ($LASTEXITCODE)" }

& dotnet run --no-build -c $Configuration --project (Join-Path $root 'spikes/Gw1aProbe/Gw1aProbe.csproj') -- --repo-root "$root" --out "$artifacts"
$probeExit = $LASTEXITCODE
# Exit 0 = aggregate Passed (qualified host). Exit 1 = run completed and
# reported a non-Passed verdict (e.g. MODULE-ABSENT on a hosted runner).
# Anything else (crash, infra fault) fails the smoke.
if ($probeExit -ne 0 -and $probeExit -ne 1) { throw "probe faulted ($probeExit): run did not complete" }

$latest = Get-ChildItem -LiteralPath $artifacts -Filter 'windows-probe-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'no probe report emitted' }
$report = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
Write-Host ("probe-smoke: report={0} aggregate={1} passed={2}/{3} mandatoryComplete={4}" -f
    $latest.Name, $report.summary.aggregate, $report.summary.passed, $report.summary.total,
    $report.summary.mandatoryComplete)

# Evidence envelope must validate: validator errors mean corrupt evidence,
# which fails CI regardless of the host verdict.
foreach ($field in @('runId', 'schemaVersion', 'evidenceKind', 'summary', 'cases', 'cleanup')) {
    if ($null -eq $report.$field) { throw "probe report missing field: $field" }
}
if ($report.evidenceKind -ne 'CapabilityProbe') { throw 'unexpected evidence envelope' }
if ($report.summary.total -ne @($report.cases).Count) { throw 'summary total does not match cases' }
Write-Host 'probe-smoke: evidence envelope valid'
