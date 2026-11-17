param([Parameter(Mandatory)][string]$Repo, [Parameter(Mandatory)][string]$Scratch)
$ErrorActionPreference = 'Stop'
$source = Join-Path $Repo 'tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationPreCodecVectors.cs'
$templateSource = Get-Content -LiteralPath $source -Raw
foreach ($tag in 'B2','Integrated') {
    $match = [regex]::Match($templateSource, 'private const string '+$tag+' = """(.+?)""";')
    if (!$match.Success) { throw "Missing template $tag" }
    [IO.File]::WriteAllText("$Scratch/$tag.json", $match.Groups[1].Value)
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'
$env:NuGetAudit = 'false'
foreach ($cfg in 'Debug','Release') {
    $lower = $cfg.ToLowerInvariant()
    $core = "$Scratch/candidate-exact/bin/Gagamba.Execution.Linux.M1/$lower"
    $out = "$Scratch/independent-exact"
    $buildArgs = @('build', "$PSScriptRoot/probe/Probe.csproj", '-c', $cfg, '--artifacts-path', $out,
                   '--nologo', "-p:Core=$core")
    $log = "$Scratch/independent-build-$lower.log"
    (ConvertTo-Json -Compress -InputObject $buildArgs) | Set-Content -LiteralPath $log
    dotnet @buildArgs *>> $log
    $code = $LASTEXITCODE
    "BUILD EXIT $code" | Add-Content -LiteralPath $log
    if ($code) { Get-Content $log -Tail 30; exit $code }
    $dll = "$out/bin/Probe/$lower/Gagamba.Execution.Linux.M1.Tests.dll"
    $runArgs = @($dll, "$Scratch/B2.json", "$Scratch/Integrated.json")
    (ConvertTo-Json -Compress -InputObject $runArgs) | Add-Content -LiteralPath $log
    $probe = Get-FileHash -Algorithm SHA256 -LiteralPath $dll
    $boundCore = Get-FileHash -Algorithm SHA256 -LiteralPath "$core/Gagamba.Execution.Linux.M1.dll"
    $result = "$Scratch/independent-$lower.tsv"
    dotnet @runArgs > $result
    $code = $LASTEXITCODE
    "RUN EXIT $code; probe $($probe.Hash); core $($boundCore.Hash)" | Add-Content -LiteralPath $log
    $loaded = (Get-Content -LiteralPath $result -TotalCount 2)[1]
    if ($loaded -notmatch ('^core\s+' + $boundCore.Hash + '$') -or $code) {
        Get-Content -LiteralPath $result | Select-String '\tFAIL|^TOTAL'
        throw "$cfg probe failed"
    }
    Get-Content -LiteralPath $result -Tail 1
}
