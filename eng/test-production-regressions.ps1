[CmdletBinding()]
param([string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $EvidenceDirectory) {
    $EvidenceDirectory = Join-Path $repo ('artifacts/b6-production-tests/' + [Guid]::NewGuid().ToString('N'))
}
$EvidenceDirectory = [IO.Path]::GetFullPath($EvidenceDirectory)
New-Item -ItemType Directory -Force $EvidenceDirectory | Out-Null
$tests = Join-Path $repo 'tests/Gagamba.Execution.Linux.M1.Tests'
# Additional production-candidate coverage: the unchanged complete Debug/Release
# qualification suite remains mandatory. Only these five hook-dependent source
# files are omitted from this additional compilation; their assertions still run
# in the instrumented suite. Keep the exclusion list visible in the evidence.
$excluded = @('Preparation/BindingTests.cs','Preparation/CopyInTests.cs','Preparation/NativeCopyInTests.cs','Recovery/IntegratedBarrierTests.cs','Recovery/NonRootQualificationTests.cs')
$excluded | Set-Content (Join-Path $EvidenceDirectory 'instrumented-only-sources.txt')
$excludes = (($excluded | ForEach-Object { Join-Path $tests $_ }) + @("$tests/bin/**/*.cs", "$tests/obj/**/*.cs")) -join ';'
$project = Join-Path $EvidenceDirectory 'ProductionRegression.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable>
    <AssemblyName>Gagamba.Execution.Linux.M1.Tests</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit.v3" Version="3.2.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" PrivateAssets="all" />
    <ProjectReference Include="$repo/src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj" />
    <ProjectReference Include="$repo/src/Gagamba.Execution/Gagamba.Execution.csproj" />
    <Compile Include="$tests/**/*.cs" Exclude="$excludes" />
  </ItemGroup>
</Project>
"@ | Set-Content $project
$log = Join-Path $EvidenceDirectory 'production-regressions.log'
& dotnet test $project -c Release --nologo --logger trx --results-directory (Join-Path $EvidenceDirectory 'trx') *> $log
$result = $LASTEXITCODE
Get-Content $log -Tail 12
if ($result -ne 0) { throw "Production regressions failed. See $log" }
