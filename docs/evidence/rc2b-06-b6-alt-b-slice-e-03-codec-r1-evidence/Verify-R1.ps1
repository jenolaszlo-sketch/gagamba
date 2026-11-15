param(
    [Parameter(Mandatory)][string]$Source,
    [Parameter(Mandatory)][string]$Run,
    [Parameter(Mandatory)][string]$Q1
)
$ErrorActionPreference = 'Stop'
$env:NuGetAudit = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$logs = Join-Path $Run 'logs'
$bin = Join-Path $Run 'qualified/bin'
$original = Join-Path $Q1 'run/qualified/bin'
$evidence = Join-Path $Q1 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence'
$core = 'Gagamba.Execution.Linux.M1'
$tests = "$core.Tests"
$regression = '*ValidationPreCodecRemediationTests'
$schema = Join-Path $Source 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md'

function Invoke-DotnetLog([string]$Name, [string[]]$Arguments, [int]$Expected) {
    $log = Join-Path $logs "$Name.log"
    'dotnet ' + ($Arguments -join ' ') | Set-Content $log
    dotnet @Arguments *>> $log
    $exitCode = $LASTEXITCODE
    "EXIT $exitCode" | Add-Content $log
    "$exitCode $Name (expected $Expected)" | Tee-Object -Append (Join-Path $logs 'verification-exits.txt')
    if ($exitCode -ne $Expected) { Get-Content $log -Tail 55; throw "Unexpected exit for $Name" }
}

$coreRelease = Join-Path $bin "$core/release"
$mirrorRelease = Join-Path $Run "managed-layout/src/$core/bin/Release/net10.0"
$mirror = Join-Path $Run 'managed-layout'
New-Item -Force -ItemType Directory $mirrorRelease | Out-Null
Copy-Item -LiteralPath (Join-Path $Source 'Gagamba.sln') -Destination $mirror
Copy-Item -Path "$coreRelease/*.dll" -Destination $mirrorRelease

$hashFile = Join-Path $logs 'test-bindings.txt'
$pin = Get-Content -Raw -LiteralPath "$evidence/test-binary-bindings.json" | ConvertFrom-Json -AsHashtable
foreach ($config in @('debug','release')) {
    $cfg = (Get-Culture).TextInfo.ToTitleCase($config)
    $testOutput = Join-Path $bin "$tests/$config"
    $before = Join-Path $Run "original-test-layout/$config"
    $after = Join-Path $Run "candidate-test-layout/$config"
    $broad = Join-Path $mirror "tests/$config"
    New-Item -Force -ItemType Directory $before,$after,$broad | Out-Null
    foreach ($layout in @($before,$after,$broad)) {
        Copy-Item -Path "$testOutput/*" -Destination $layout -Recurse -Force
        if ((Get-FileHash "$layout/$tests.dll").Hash -ne (Get-FileHash "$testOutput/$tests.dll").Hash) {
            throw "Test assembly unexpectedly changed in $layout"
        }
    }
    $originalCore = "$original/$core/$config/$core.dll"
    if ((Get-FileHash $originalCore).Hash -ne $pin[$config].core[0].sha256) {
        throw "Original $config DLL does not match Q1 evidence"
    }
    Copy-Item -LiteralPath $originalCore -Destination "$before/$core.dll" -Force
    Copy-Item -LiteralPath "$original/$core/$config/Gagamba.Execution.dll" -Destination "$before/Gagamba.Execution.dll" -Force
    Copy-Item -LiteralPath "$bin/$core/$config/$core.dll" -Destination "$after/$core.dll" -Force
    Copy-Item -LiteralPath "$bin/$core/$config/Gagamba.Execution.dll" -Destination "$after/Gagamba.Execution.dll" -Force

    Get-FileHash "$before/$core.dll","$after/$core.dll","$broad/$core.dll","$after/$tests.dll","$broad/$tests.dll" |
        ForEach-Object { "$($_.Hash.ToLowerInvariant())  $($_.Path)" } | Add-Content $hashFile

    Invoke-DotnetLog "regression-before-$config" @("$before/$tests.dll",'-noColor','-class',$regression,'-xml',"$logs/regression-before-$config.xml") 1
    Invoke-DotnetLog "regression-after-$config" @("$after/$tests.dll",'-noColor','-class',$regression,'-xml',"$logs/regression-after-$config.xml") 0
    Invoke-DotnetLog "focused-original-$config" @("$after/$tests.dll",'-noColor','-class','*ValidationPreCodecBootstrapTests',
        '-class','*ValidationPreCodecBinaryTests','-class','*InstallationWireTests','-xml',"$logs/focused-original-$config.xml") 0
    Invoke-DotnetLog "managed-broad-$config" @("$broad/$tests.dll",'-noColor','-xml',"$logs/managed-broad-$config.xml") 0

    foreach ($side in @('before','after')) {
        $candidate = if ($side -eq 'before') { "$original/$core/$config" } else { "$bin/$core/$config" }
        $exit = if ($side -eq 'before') { 1 } else { 0 }
        $probeOut = Join-Path $Run "probe-$side-$config"
        $probeBuild = Join-Path $Run "probe-build/$side"
        Invoke-DotnetLog "q1-probe-$side-$config" @('run','--project',"$evidence/probe/Probe.csproj",'-c',$cfg,
            '--artifacts-path',$probeBuild,'-p:UseSharedCompilation=false',"-p:Candidate=$candidate",'--',$probeOut,$schema) $exit
        $probeCopy = "$probeBuild/bin/Probe/$config/$core.dll"
        if ((Get-FileHash $probeCopy).Hash -ne (Get-FileHash "$candidate/$core.dll").Hash) {
            throw "Probe did not resolve $side $config candidate"
        }
        Get-FileHash $probeCopy | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $($_.Path)" } | Add-Content $hashFile
    }
}

$scannerArgs = foreach ($name in @($core,"$core.Owner","$core.Recovery",'InstallTool','Gagamba.Execution',$tests)) {
    foreach ($config in @('debug','release')) {
        if ($name -eq $tests) {
            Get-Item "$bin/$name/$config/$name.dll","$bin/$name/$config/$core.dll","$bin/$name/$config/Gagamba.Execution.dll"
        } else { Get-ChildItem "$bin/$name/$config/*.dll" -File }
    }
}
$scannerBuild = Join-Path $Run 'scanner'
Invoke-DotnetLog 'scanner-build' @('build',"$evidence/scan/Scan.csproj",'-c','Release','--artifacts-path',
    $scannerBuild,'-p:UseSharedCompilation=false') 0
$scanner = "$scannerBuild/bin/Scan/release/Scan.dll"
$scanPaths = @($scannerArgs.FullName)
dotnet $scanner @scanPaths *> "$logs/compiled-scan.jsonl"
$scanExit = $LASTEXITCODE
"$scanExit compiled-scan (expected 0)" | Add-Content "$logs/verification-exits.txt"
if ($scanExit -ne 0) { throw 'Compiled scan failed' }

$verifier = "$bin/VerifyProductionAssembly/release/VerifyProductionAssembly.dll"
Invoke-DotnetLog 'verifier-release' @($verifier,'--production',"$bin/$core/release/$core.dll") 0
Invoke-DotnetLog 'verifier-debug' @($verifier,'--production',"$bin/$core/debug/$core.dll") 1
if (!(Select-String -LiteralPath "$logs/verifier-debug.log" -SimpleMatch "assembly configuration is 'Debug', expected Release")) {
    throw 'Wrong verifier failure diagnostic'
}
