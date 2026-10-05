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

Write-Host "launch-smoke: root=$root config=$Configuration"
Write-Host 'launch-smoke: proves the spike builds, runs end-to-end, and emits schema-valid evidence. Aggregate Passed is NOT required: open engine gates fail legs by design (see docs/work-queue.md); that verdict lives in the queue, not in CI.'
& dotnet build (Join-Path $root 'spikes/Gw1bLaunch/Gw1bLaunch.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "launch spike build failed ($LASTEXITCODE)" }

& dotnet run --no-build -c $Configuration --project (Join-Path $root 'spikes/Gw1bLaunch/Gw1bLaunch.csproj') -- --repo-root "$root" --out "$artifacts"
$spikeExit = $LASTEXITCODE
# Exit 0 = aggregate Passed; exit 1 = legs failed but the run completed and
# reported. Anything else (crash, infra fault) fails the smoke.
if ($spikeExit -ne 0 -and $spikeExit -ne 1) { throw "launch spike faulted ($spikeExit): run did not complete" }

$latest = Get-ChildItem -LiteralPath $artifacts -Filter 'windows-launch-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'no launch report emitted' }
$report = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
Write-Host ("launch-smoke: report={0} aggregate={1} passed={2}/{3} mandatoryComplete={4}" -f
    $latest.Name, $report.summary.aggregate, $report.summary.passed, $report.summary.total,
    $report.summary.mandatoryComplete)

# Evidence envelope must validate: validator errors mean corrupt evidence,
# which fails CI regardless of leg verdicts. Recompute from the file with
# a strict schema check (validatorErrors equivalent): required top-level
# fields and a derived summary block must be present.
foreach ($field in @('runId', 'schemaVersion', 'evidenceKind', 'summary', 'cases', 'cleanup')) {
    if ($null -eq $report.$field) { throw "launch report missing field: $field" }
}
if ($report.evidenceKind -ne 'CapabilityProbe') { throw 'unexpected evidence envelope' }
if ($report.summary.total -ne @($report.cases).Count) { throw 'summary total does not match cases' }
Write-Host 'launch-smoke: evidence envelope valid'
