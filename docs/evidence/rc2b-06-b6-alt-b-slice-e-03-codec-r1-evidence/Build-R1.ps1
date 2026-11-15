param(
  [Parameter(Mandatory)][string]$Source,
  [Parameter(Mandatory)][string]$Artifacts,
  [Parameter(Mandatory)][string]$Logs
)
$ErrorActionPreference = 'Stop'
New-Item -Force -ItemType Directory $Logs,$Artifacts | Out-Null
Set-Location -LiteralPath $Source
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = $Artifacts
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$projects = @(
  'tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj',
  'src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj',
  'src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj',
  'src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj',
  'eng/InstallTool/InstallTool.csproj'
)
foreach ($cfg in @('Debug','Release')) {
    foreach ($project in $projects) {
        $name = [IO.Path]::GetFileNameWithoutExtension($project)
        $log = Join-Path $Logs "build-$cfg-$name.log"
        "dotnet build $project -c $cfg --artifacts-path $Artifacts --nologo -m:1" | Set-Content $log
        dotnet build $project -c $cfg --artifacts-path $Artifacts --nologo -m:1 *>> $log
        $code = $LASTEXITCODE
        "EXIT $code" | Add-Content $log
        "$code build $cfg $name" | Tee-Object -Append (Join-Path $Logs 'build-exits.txt')
        if ($code -ne 0) { Get-Content $log -Tail 30; exit $code }
    }
}
