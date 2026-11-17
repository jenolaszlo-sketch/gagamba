param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string]$Scratch)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath $Repository).Path
$run = (Resolve-Path -LiteralPath $Scratch).Path
Set-Location -LiteralPath $repo
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:RestoreIgnoreFailedSources = 'true'
$env:NuGetAudit = 'false'
$bin = Join-Path $run 'candidate-exact'
$logs = Join-Path $run 'build-exact'
& 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/Build-Candidate.ps1' -Source $repo -Artifacts $bin -Logs $logs
if ($LASTEXITCODE) { exit $LASTEXITCODE }
foreach ($cfg in 'Debug','Release') {
    foreach ($project in 'src/Gagamba.Runtime/Gagamba.Runtime.csproj','tests/Fixtures/B2Controller/B2Controller.csproj') {
        $log = Join-Path $logs ("build-$cfg-" + [IO.Path]::GetFileNameWithoutExtension($project) + '.log')
        "dotnet build $project -c $cfg --artifacts-path $bin --nologo -m:1" | Set-Content $log
        dotnet build $project -c $cfg --artifacts-path $bin --nologo -m:1 *>> $log
        $code = $LASTEXITCODE
        "EXIT $code" | Add-Content $log
        "$code build $cfg $project" | Tee-Object -Append (Join-Path $logs 'build-exits.txt')
        if ($code) { Get-Content $log -Tail 25; exit $code }
    }
}
exit 0
