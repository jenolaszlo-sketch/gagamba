[CmdletBinding()]
param([string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $EvidenceDirectory) {
    $EvidenceDirectory = Join-Path $repo ('artifacts/b6-build-regression/' + [Guid]::NewGuid().ToString('N'))
}
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
New-Item -ItemType Directory -Force $EvidenceDirectory | Out-Null
$rows = [Collections.Generic.List[object]]::new()
$m1 = Join-Path $repo 'src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj'
$owner = Join-Path $repo 'src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj'
$recovery = Join-Path $repo 'src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj'
$tool = Join-Path $repo 'eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj'
function Invoke-Probe([string]$Name, [string[]]$Arguments, [string]$ExpectedDiagnostic = '') {
    $log = Join-Path $EvidenceDirectory ($Name + '.log')
    ('dotnet ' + ($Arguments -join ' ')) | Set-Content $log
    & dotnet @Arguments >> $log 2>&1
    $code = $LASTEXITCODE
    $text = Get-Content -LiteralPath $log -Raw
    $ok = if ($ExpectedDiagnostic) { $code -ne 0 -and $text.Contains($ExpectedDiagnostic) } else { $code -eq 0 }
    $rows.Add([pscustomobject]@{Name=$Name; Exit=$code; Expected=$ExpectedDiagnostic; Passed=$ok; Arguments=$Arguments})
    $rows | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $EvidenceDirectory 'results.json')
    Write-Output "$Name exit=$code passed=$ok"
    if (-not $ok) { throw "Probe failed: $Name. Inspect $log" }
}
function Inspect-M1([string]$Name, [string]$Artifacts, [string]$Configuration) {
    $mode = if ($Configuration -eq 'Release') { '--production' } else { '--instrumented' }
    $binary = Join-Path $Artifacts ('bin/Gagamba.Execution.Linux.M1/' + $Configuration.ToLowerInvariant() + '/Gagamba.Execution.Linux.M1.dll')
    Invoke-Probe $Name @('run','--project',$tool,'-c','Release','--no-launch-profile','--',$mode,$binary)
    Get-FileHash -Algorithm SHA256 $binary | Format-List >> (Join-Path $EvidenceDirectory ($Name + '.log'))
}
Push-Location $repo
try {
    # Normal build order, repeated incremental builds, and distinct fresh roots.
    $orderRoot = Join-Path $EvidenceDirectory 'order'
    $index = 0
    foreach ($configuration in @('Release','Debug','Release','Release','Debug','Debug','Test')) {
        $index++
        Invoke-Probe "order-$index-$configuration" @('build',$m1,'-c',$configuration,'--artifacts-path',$orderRoot,'--nologo')
        Inspect-M1 "inspect-order-$index-$configuration" $orderRoot $configuration
    }
    $debugRoot = Join-Path $EvidenceDirectory 'fresh-debug'
    Invoke-Probe 'fresh-debug' @('build',$m1,'-c','Debug','--artifacts-path',$debugRoot,'--nologo')
    Inspect-M1 'inspect-fresh-debug' $debugRoot 'Debug'
    foreach ($entry in @(@('owner',$owner),@('recovery',$recovery))) {
        Invoke-Probe ($entry[0]+'-release') @('build',$entry[1],'-c','Release','--artifacts-path',(Join-Path $EvidenceDirectory $entry[0]),'--nologo')
    }
    Invoke-Probe 'solution-release' @('build',(Join-Path $repo 'Gagamba.sln'),'-c','Release','-m','--artifacts-path',(Join-Path $EvidenceDirectory 'solution'),'--nologo')
    $negativeCases = @(
        @('missing-symbol',@('-p:DefineConstants=TRACE'),'GAGAMBA002'),
        @('empty-symbols',@('-p:DefineConstants='),'GAGAMBA002'),
        @('fixture-only',@('-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE'),'GAGAMBA002'),
        @('production-and-fixture',@('-p:DefineConstants=GAGAMBA_PRODUCTION%3BGAGAMBA_WP05_D6_4_TEST_FIXTURE'),'GAGAMBA004'),
        @('release-fixture-carrier',@('-p:GagambaWp05D64FixtureEnabled=true'),'GAGAMBA004'),
        @('spoof-helper-properties',@('-p:DefineConstants=','-p:_GagambaHasProductionSymbol=True'),'GAGAMBA002')
    )
    foreach ($case in $negativeCases) {
        $caseRoot = Join-Path $EvidenceDirectory $case[0]
        Invoke-Probe $case[0] (@('build',$m1,'-c','Release','--artifacts-path',$caseRoot,'--nologo') + $case[1]) $case[2]
        if (Get-ChildItem $caseRoot -Recurse -Filter 'Gagamba.Execution.Linux.M1.dll') { throw "Rejected fresh build emitted M1: $($case[0])" }
    }
    Invoke-Probe 'unknown-configuration' @('build',$m1,'-c','Unclassified','--artifacts-path',(Join-Path $EvidenceDirectory 'unknown'),'--nologo') 'GAGAMBA001'
    Invoke-Probe 'fixture-without-isolated-paths' @('build',$m1,'-c','Debug','-p:GagambaWp05D64FixtureEnabled=true','-p:AssemblyName=Gagamba.Execution.Linux.M1.TestFixture','--artifacts-path',(Join-Path $EvidenceDirectory 'unisolated-fixture'),'--nologo') 'GAGAMBA008'
    Invoke-Probe 'debug-forced-production' @('build',$m1,'-c','Debug','-p:DefineConstants=GAGAMBA_PRODUCTION','--artifacts-path',(Join-Path $EvidenceDirectory 'forced-debug'),'--nologo') 'GAGAMBA003'
    Invoke-Probe 'debug-fixture-symbol-alone' @('build',$m1,'-c','Debug','-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE','--artifacts-path',(Join-Path $EvidenceDirectory 'fixture-symbol'),'--nologo') 'GAGAMBA006'
    foreach ($entry in @(@('owner',$owner),@('recovery',$recovery),@('solution',(Join-Path $repo 'Gagamba.sln')))) {
        Invoke-Probe ($entry[0]+'-empty-symbols') @('build',$entry[1],'-c','Release','-p:DefineConstants=','--artifacts-path',(Join-Path $EvidenceDirectory ($entry[0]+'-empty')),'--nologo') 'GAGAMBA002'
    }
    Invoke-Probe 'stale-valid-output-empty-symbols' @('build',$m1,'-c','Release','-p:DefineConstants=','--artifacts-path',$orderRoot,'--nologo') 'GAGAMBA002'
    # Explicit ProjectReference global-property override, independent of CLI propagation.
    $probeProject = Join-Path $EvidenceDirectory 'ReferenceProbe.csproj'
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup><ProjectReference Include="$m1" AdditionalProperties="DefineConstants=" /></ItemGroup>
</Project>
"@ | Set-Content $probeProject
    Invoke-Probe 'project-reference-empty-symbols' @('build',$probeProject,'-c','Release','--artifacts-path',(Join-Path $EvidenceDirectory 'reference'),'--nologo') 'GAGAMBA002'
    foreach ($entry in @(@('m1',$m1),@('owner',$owner),@('recovery',$recovery))) {
        $publishRoot = Join-Path $EvidenceDirectory ('publish-'+$entry[0])
        Invoke-Probe ('publish-'+$entry[0]) @('publish',$entry[1],'-c','Release','--artifacts-path',$publishRoot,'--nologo')
        Invoke-Probe ('publish-'+$entry[0]+'-empty-symbols') @('publish',$entry[1],'-c','Release','--no-build','-p:DefineConstants=','--artifacts-path',$publishRoot,'--nologo') 'GAGAMBA002'
    }
    # A newer hook-enabled output must not pass an incremental build or no-build publish.
    $releaseBinary = Join-Path $orderRoot 'bin/Gagamba.Execution.Linux.M1/release/Gagamba.Execution.Linux.M1.dll'
    $debugBinary = Join-Path $orderRoot 'bin/Gagamba.Execution.Linux.M1/debug/Gagamba.Execution.Linux.M1.dll'
    Copy-Item -LiteralPath $debugBinary -Destination $releaseBinary
    Invoke-Probe 'stale-instrumented-binary-inspection' @('run','--project',$tool,'-c','Release','--no-launch-profile','--','--production',$releaseBinary) 'FAIL'
    Invoke-Probe 'stale-instrumented-no-build-publish' @('publish',$m1,'-c','Release','--no-build','--artifacts-path',$orderRoot,'--nologo') 'FAIL'
    # A ref-disabled owner build must still inspect its own output closure.
    $ownerRoot = Join-Path $EvidenceDirectory 'owner'
    $ownerM1 = Join-Path $ownerRoot 'bin/Gagamba.Execution.Linux.M1.Owner/release/Gagamba.Execution.Linux.M1.dll'
    $ownerSourceM1 = Join-Path $ownerRoot 'bin/Gagamba.Execution.Linux.M1/release/Gagamba.Execution.Linux.M1.dll'
    Copy-Item -LiteralPath $debugBinary -Destination $ownerSourceM1
    Copy-Item -LiteralPath $debugBinary -Destination $ownerM1
    Invoke-Probe 'owner-stale-ref-disabled' @('build',$owner,'-c','Release','-p:BuildProjectReferences=false','--artifacts-path',$ownerRoot,'--nologo') 'FAIL'
    # Rebuild the affected isolated outputs and verify a genuine candidate again.
    Invoke-Probe 'restore-valid-release' @('build',$m1,'-c','Release','--no-incremental','--artifacts-path',$orderRoot,'--nologo')
    Inspect-M1 'inspect-restored-release' $orderRoot 'Release'
    Write-Output "All $($rows.Count) build/binary probes passed. Evidence: $EvidenceDirectory"
} finally { Pop-Location }
