param(
  [Parameter(Mandatory)][string]$Repository,
  [Parameter(Mandatory)][string]$Run,
  [switch]$ProbesOnly,
  [switch]$ManagedOnly
)
$ErrorActionPreference = 'Stop'
$env:NuGetAudit = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$e3 = 'Gagamba.Execution.Linux.M1'
$testName = "$e3.Tests"
$q1 = Join-Path $Repository 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence'
$schema = Join-Path $Repository 'docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md'
$candidate = Join-Path $Run 'candidate/bin'
$original = Join-Path (Split-Path -Parent (Split-Path -Parent $Run)) "gagamba-e03-codec-r1-q2/run/original/bin"
$logs = Join-Path $Run 'logs'
New-Item -ItemType Directory -Force $logs | Out-Null

function Invoke-Logged([string]$Name, [string[]]$Command, [int]$Expected) {
    $log = Join-Path $logs "$Name.log"
    ('dotnet ' + ($Command -join ' ')) | Set-Content $log
    & dotnet @Command *>> $log
    $code = $LASTEXITCODE
    "EXIT $code" | Add-Content $log
    "$code $Name"
    if ($Expected -ge 0 -and $Expected -ne $code) {
        Get-Content $log -Tail 35
        throw "Unexpected exit code for $Name"
    }
}
$bindings = [Collections.Generic.List[object]]::new()
function Bind([string]$Label, [string]$File, [string]$ShouldMatch = '') {
    $digest = (Get-FileHash -LiteralPath $File).Hash.ToLowerInvariant()
    if ($ShouldMatch -and $digest -ne (Get-FileHash -LiteralPath $ShouldMatch).Hash.ToLowerInvariant()) {
        throw "Dependency mismatch for $Label"
    }
    $bindings.Add(@{ label=$Label; path=$File; sha256=$digest })
}
if (!$ManagedOnly) {
    $replaySource = Join-Path $Run 'frozen-q1-probe'
    $extraSource = Join-Path $Run 'q2-probe'
    New-Item -ItemType Directory -Force $replaySource,$extraSource | Out-Null
    foreach ($name in @('Program.cs','Probe.csproj')) {
        Copy-Item -LiteralPath "$q1/probe/$name" -Destination $replaySource -Force
        Copy-Item -LiteralPath "$q1/probe/$name" -Destination $extraSource -Force
        Bind "imported source $name" "$replaySource/$name" "$q1/probe/$name"
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Q2Cases.cs') -Destination $extraSource -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Q3Cases.cs') -Destination $extraSource -Force
    $text = Get-Content -Raw -LiteralPath "$extraSource/Program.cs"
    $needle = 'return failures == 0 ? 0 : 1;'
    if ($text.IndexOf($needle) -ne $text.LastIndexOf($needle) -or !$text.Contains($needle)) { throw 'Wrong probe source' }
    [IO.File]::WriteAllText("$extraSource/Program.cs",
        $text.Replace($needle, 'failures += Q2Cases.Run(output, Witness, capability); failures += Q3Cases.Run(output, Witness, capability);'+[Environment]::NewLine+$needle))
    foreach ($config in @('debug','release')) {
        $cfg = [Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($config)
        foreach ($track in @('original','candidate')) {
            $signedBuild = if ($track -eq 'original') { $original } else { $candidate }
            $corePath = "$signedBuild/$e3/$config"
            $artifacts = "$Run/replay-artifacts-$track"
            $out = "$Run/replay-$track-$config"
            $exit = if ($track -eq 'original') { 1 } else { 0 }
            Invoke-Logged "replay-$track-$config" @('run','--project',"$replaySource/Probe.csproj",'-c',$cfg,
              '--artifacts-path',$artifacts,'-p:UseSharedCompilation=false',"-p:Candidate=$corePath",'--',$out,$schema) $exit
            Bind "Q1 replay $track $config" "$artifacts/bin/Probe/$config/$e3.dll" "$corePath/$e3.dll"
        }
        $actual = "$candidate/$e3/$config"
        $artifacts = "$Run/supplement-artifacts"
        Invoke-Logged "supplement-$config" @('run','--project',"$extraSource/Probe.csproj",'-c',$cfg,
          '--artifacts-path',$artifacts,'-p:UseSharedCompilation=false',"-p:Candidate=$actual",'--',"$Run/supplement-$config",$schema) -1
        Bind "Q2 supplement $config" "$artifacts/bin/Probe/$config/$e3.dll" "$actual/$e3.dll"
    }
    ConvertTo-Json -InputObject @($bindings.ToArray()) -Depth 4 | Set-Content "$logs/probe-bindings.json"
}
if (!$ProbesOnly) {
    $layout = Join-Path $Run 'broad-layout'
    $releaseLocation = "$layout/src/$e3/bin/Release/net10.0"
    New-Item -ItemType Directory -Force $releaseLocation | Out-Null
    Copy-Item -LiteralPath (Join-Path $Repository 'Gagamba.sln') -Destination $layout
    Copy-Item -Path "$candidate/$e3/release/*.dll" -Destination $releaseLocation
    Bind 'actual Release at broad reachability discovery path' "$releaseLocation/$e3.dll" "$candidate/$e3/release/$e3.dll"
    $old = @('-class','*ValidationPreCodecBootstrapTests','-class','*ValidationPreCodecBinaryTests','-class','*InstallationWireTests')
    $new = @('-class','*ValidationPreCodecRemediationTests')
    foreach ($config in @('debug','release')) {
        $focused = "$Run/focused-layout/$config"
        $broad = "$layout/tests/$config"
        New-Item -ItemType Directory -Force $focused,$broad | Out-Null
        foreach ($path in @($focused,$broad)) {
            Copy-Item -Path "$candidate/$testName/$config/*" -Destination $path -Recurse -Force
            Bind "$config test DLL in $path" "$path/$testName.dll" "$candidate/$testName/$config/$testName.dll"
        }
        foreach ($dll in @("$e3.dll",'Gagamba.Execution.dll')) {
            Copy-Item -LiteralPath "$candidate/$e3/$config/$dll" -Destination $focused -Force
            Bind "$config focused $dll" "$focused/$dll" "$candidate/$e3/$config/$dll"
        }
        Bind "$config broad core fixture" "$broad/$e3.dll" "$candidate/$e3/debug/$e3.dll"
        $exe = "$focused/$testName.dll"
        Invoke-Logged "original-focused-$config" (@($exe,'-noColor')+$old+@('-xml',"$logs/original-focused-$config.xml")) 0
        Invoke-Logged "r1-focused-$config" (@($exe,'-noColor')+$new+@('-xml',"$logs/r1-focused-$config.xml")) 0
        $r2 = @('-class','*ValidationPreCodecR2Tests')
        Invoke-Logged "r2-focused-$config" (@($exe,'-noColor')+$r2+@('-xml',"$logs/r2-focused-$config.xml")) 0
        Invoke-Logged "combined-focused-$config" (@($exe,'-noColor')+$old+$new+$r2+@('-xml',"$logs/combined-focused-$config.xml")) 0
        Invoke-Logged "e4-focused-$config" @($exe,'-noColor','-class','*Gava1ContextCodecTests','-xml',"$logs/e4-focused-$config.xml") 0
        Invoke-Logged "broad-$config" @("$broad/$testName.dll",'-noColor','-xml',"$logs/broad-$config.xml") 0
    }
    ConvertTo-Json -InputObject @($bindings.ToArray()) -Depth 4 | Set-Content "$logs/test-bindings.json"
    $toolDir = Join-Path $Run 'scan-project'
    New-Item -ItemType Directory -Force "$toolDir/scan" | Out-Null
    Copy-Item -LiteralPath "$Run/Scan.cs" -Destination "$toolDir/Scan.cs" -Force
    Copy-Item -LiteralPath "$q1/scan/Scan.csproj" -Destination "$toolDir/scan/Scan.csproj" -Force
    $scanBuild = Join-Path $Run 'scan-artifacts'
    Invoke-Logged 'scan-build' @('build',"$toolDir/scan/Scan.csproj",'-c','Release','--artifacts-path',$scanBuild,
        '-p:UseSharedCompilation=false') 0
    $files = foreach ($name in @($e3,"$e3.Owner","$e3.Recovery",'InstallTool','Gagamba.Execution',$testName)) {
        foreach ($config in @('debug','release')) {
            if ($name -eq $testName) {
                Get-Item "$candidate/$name/$config/$name.dll","$candidate/$name/$config/$e3.dll","$candidate/$name/$config/Gagamba.Execution.dll"
            } else { Get-ChildItem "$candidate/$name/$config/*.dll" -File }
        }
    }
    $paths = @($files.FullName)
    & dotnet "$scanBuild/bin/Scan/release/Scan.dll" @paths *> "$logs/compiled-scan.jsonl"
    $exitScan = $LASTEXITCODE
    "$exitScan compiled-scan" | Set-Content "$logs/scanner-exit.txt"
    if ($exitScan -ne 0) { throw 'Compiled scan failed' }
    "dotnet $scanBuild/bin/Scan/release/Scan.dll" | Set-Content "$logs/scanner-command.txt"
    $paths | Add-Content "$logs/scanner-command.txt"
    $env:GAGAMBA_SCAN_E4 = '1'
    try {
        & dotnet "$scanBuild/bin/Scan/release/Scan.dll" @paths *> "$logs/compiled-scan-e4.jsonl"
        if ($LASTEXITCODE) { throw 'E-4 compiled scan failed' }
    } finally { Remove-Item Env:GAGAMBA_SCAN_E4 }
    $verifier = "$candidate/VerifyProductionAssembly/release/VerifyProductionAssembly.dll"
    Invoke-Logged 'verifier-release' @($verifier,'--production',"$candidate/$e3/release/$e3.dll") 0
    Invoke-Logged 'verifier-debug' @($verifier,'--production',"$candidate/$e3/debug/$e3.dll") 1
    if (!(Select-String -LiteralPath "$logs/verifier-debug.log" -SimpleMatch "assembly configuration is 'Debug', expected Release")) {
        throw 'Unexpected Debug verifier error'
    }
}

$inputs = Join-Path $Repository 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence/findings-inputs'
foreach ($config in @('debug','release')) {
    Invoke-Logged "q2-exact-$config" @('run','--project',"$Run/finding-project/Findings.csproj",'-c',$config,
      '--artifacts-path',"$Run/findings", "-p:Candidate=$candidate/$e3/$config",'--',$inputs) 0
    Bind "Q2 exact $config" "$Run/findings/bin/Findings/$config/$e3.dll" "$candidate/$e3/$config/$e3.dll"
}
ConvertTo-Json -InputObject @($bindings.ToArray()) -Depth 4 | Set-Content "$logs/all-bindings.json"
