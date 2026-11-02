param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$m1proj = Join-Path $r 'src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$cases = @(
  @('c-g5-malformed-carrier', @('-c','Debug','-p:GagambaWp05D64FixtureEnabled=banana')),
  @('c-g6-fixture-symbol-no-carrier', @('-c','Debug','-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE')),
  @('c-g7-release-fixture-assemblyname', @('-c','Release','-p:DefineConstants=GAGAMBA_PRODUCTION','-p:AssemblyName=Gagamba.Execution.Linux.M1.TestFixture'))
)
foreach ($c in $cases) {
  $log = Join-Path $Rev ("evidence\" + $c[0] + ".log")
  dotnet build $m1proj @($c[1]) --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object { if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] } } | Sort-Object -Unique) -join ','
  Write-Output ("{0}: EXIT={1} DIAG={2}" -f $c[0], $code, $diag)
}
# target-ordering evidence: which targets ran in the c02 refusal
Select-String (Join-Path $Rev 'evidence\c02-empty.log') -Pattern 'RejectMissingGagambaProductionSymbol|ValidateGagambaCompilationIdentity|CoreCompile' | ForEach-Object { $_.Line.Trim() } | Select-Object -First 6
