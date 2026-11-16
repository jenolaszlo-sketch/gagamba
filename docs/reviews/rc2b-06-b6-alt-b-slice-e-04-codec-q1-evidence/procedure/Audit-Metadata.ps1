$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../Gagamba')).Path
$run = Join-Path $PSScriptRoot 'run'
$logs = Join-Path $run 'logs'
$bin = Join-Path $run 'candidate/bin'
$m1 = 'Gagamba.Execution.Linux.M1'
$producer = (Get-Item "$repo/docs/evidence/*04-codec-implementation-01-evidence").FullName
$q1 = (Get-Item "$repo/docs/reviews/*03-codec-q1-evidence").FullName
$scan = "$run/scan-source"
New-Item -ItemType Directory -Force "$scan/scan" | Out-Null
Copy-Item -LiteralPath "$producer/procedure/Scan.cs" -Destination $scan
Copy-Item -LiteralPath "$q1/scan/Scan.csproj" -Destination "$scan/scan"
$env:NuGetAudit = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$scanBuild = "$run/scan-build"
$argsBuild = @('build', "$scan/scan/Scan.csproj", '-c', 'Release', '--artifacts-path', $scanBuild)
$log = "$logs/metadata-commands.log"
ConvertTo-Json -InputObject (@('dotnet') + $argsBuild) -Compress | Set-Content $log
dotnet @argsBuild *>> $log
$code = $LASTEXITCODE
"EXIT $code" | Add-Content $log
if ($code) { exit $code }
$tool = "$scanBuild/bin/Scan/release/Scan.dll"
Get-FileHash "$scan/Scan.cs", "$scan/scan/Scan.csproj", $tool | ForEach-Object { "$($_.Hash)  $($_.Path)" } | Add-Content $log
$files = foreach ($config in @('debug', 'release')) {
    foreach ($name in @($m1, "$m1.Owner", "$m1.Recovery", 'InstallTool', 'Gagamba.Execution')) {
        Get-ChildItem "$bin/$name/$config/*.dll" -File | Sort-Object Name | Select-Object -ExpandProperty FullName
        $copy = "$bin/$name/$config/$m1.dll"
        if ($name -in @("$m1.Owner", "$m1.Recovery", 'InstallTool')) {
            if ((Get-FileHash $copy).Hash -ne (Get-FileHash "$bin/$m1/$config/$m1.dll").Hash) { throw "core identity $copy" }
        }
    }
    Get-ChildItem "$bin/$m1.Tests/$config/*.dll" -File | Where-Object { $_.Name -like 'Gagamba.*' } |
        Sort-Object Name | Select-Object -ExpandProperty FullName
}
foreach ($codec in @('e3', 'e4')) {
    $env:GAGAMBA_SCAN_E4 = if ($codec -eq 'e4') { '1' } else { '0' }
    "GAGAMBA_SCAN_E4=$env:GAGAMBA_SCAN_E4" | Add-Content $log
    ConvertTo-Json -InputObject (@('dotnet', $tool) + $files) -Compress | Add-Content $log
    dotnet $tool @files > "$logs/metadata-$codec.jsonl"
    $code = $LASTEXITCODE
    "EXIT $code" | Add-Content $log
    if ($code) { exit $code }
}
$verifier = "$bin/VerifyProductionAssembly/release/VerifyProductionAssembly.dll"
Get-FileHash $verifier | ForEach-Object { "$($_.Hash)  $($_.Path)" } | Add-Content $log
foreach ($config in @('debug', 'release')) {
    $vLog = "$logs/verifier-$config.log"
    $vArgs = @($verifier, '--production', "$bin/$m1/$config/$m1.dll")
    ConvertTo-Json -InputObject (@('dotnet') + $vArgs) -Compress | Set-Content $vLog
    dotnet @vArgs *>> $vLog
    $code = $LASTEXITCODE
    "EXIT $code" | Add-Content $vLog
    if ($config -eq 'debug') {
        if ($code -ne 1 -or !(Select-String -LiteralPath $vLog -SimpleMatch "assembly configuration is 'Debug', expected Release")) { throw 'Debug refusal differed' }
    } elseif ($code -ne 0) { throw 'Release refused' }
}
