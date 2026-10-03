[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'
if (-not $PSBoundParameters.ContainsKey('RepositoryRoot')) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) { throw 'RepositoryRoot must be non-empty.' }
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "fixture-selftest: root=$root config=$Configuration"
& dotnet build (Join-Path $root 'tests/Fixtures/Gagamba.Fixture.Worker/Gagamba.Fixture.Worker.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "worker build failed ($LASTEXITCODE)" }
& dotnet build (Join-Path $root 'tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj') -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "harness build failed ($LASTEXITCODE)" }
& dotnet test (Join-Path $root 'tests/Fixtures/Gagamba.Fixture.SelfTest/Gagamba.Fixture.SelfTest.csproj') -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "selftest unit failed ($LASTEXITCODE)" }

& dotnet run --no-build -c $Configuration --project (Join-Path $root 'tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj') -- --repo-root "$root" --out "$artifacts"
if ($LASTEXITCODE -ne 0) { throw "harness failed ($LASTEXITCODE): FixtureSelfTest aggregate is not Passed" }

$latest = Get-ChildItem -LiteralPath $artifacts -Filter 'fixture-selftest-*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $latest) { throw 'no evidence report emitted' }
$report = Get-Content -LiteralPath $latest.FullName -Raw | ConvertFrom-Json
Write-Host ("fixture-selftest: report={0} aggregate={1} passed={2}/{3} mandatoryComplete={4} cleanup={5}" -f
    $latest.Name, $report.summary.aggregate, $report.summary.passed, $report.summary.total,
    $report.summary.mandatoryComplete, $report.cleanup.status)
if ($report.summary.aggregate -ne 'Passed') { throw 'evidence aggregate is not Passed' }
if ($report.schemaVersion -ne 1 -or $report.evidenceKind -ne 'FixtureSelfTest') { throw 'unexpected evidence envelope' }
Write-Host 'fixture-selftest: Passed'
