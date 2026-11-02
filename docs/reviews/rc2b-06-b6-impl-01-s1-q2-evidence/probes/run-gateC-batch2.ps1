param([string]$Rev)
$ErrorActionPreference = 'Continue'
$m1 = Join-Path $Rev 'src-remediated\src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$cases = @(
  @('c06b-contradictory-esc', '-c', 'Release', '-p:DefineConstants=GAGAMBA_PRODUCTION%3BGAGAMBA_WP05_D6_4_TEST_FIXTURE'),
  @('c07a-debug-forced-prod', '-c', 'Debug', '-p:DefineConstants=GAGAMBA_PRODUCTION'),
  @('c07b-test-forced-prod', '-c', 'Test', '-p:DefineConstants=GAGAMBA_PRODUCTION'),
  @('c19-unknown-config', '-c', 'Unclassified'),
  @('c20a-release-fixture-carrier', '-c', 'Release', '-p:GagambaWp05D64FixtureEnabled=true'),
  @('c20b-debug-fixture-no-identity', '-c', 'Debug', '-p:GagambaWp05D64FixtureEnabled=true'),
  @('c22-release-explicit-valid', '-c', 'Release', '-p:DefineConstants=GAGAMBA_PRODUCTION')
)
foreach ($c in $cases) {
  $name = $c[0]
  $cfg = $c[1]; $rest = $c[2..($c.Length-1)]
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet build $m1 $cfg @rest --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object {
      if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] }
    } | Sort-Object -Unique) -join ','
  Write-Output ("{0}: EXIT={1} DIAG={2}" -f $name, $code, $diag)
}
