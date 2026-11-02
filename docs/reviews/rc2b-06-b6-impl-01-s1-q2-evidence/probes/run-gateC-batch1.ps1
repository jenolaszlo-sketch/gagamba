param([string]$Rev)
$ErrorActionPreference = 'Continue'
$m1 = Join-Path $Rev 'src-remediated\src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$cases = @(
  @('c02-empty', '-p:DefineConstants='),
  @('c03-trace', '-p:DefineConstants=TRACE'),
  @('c04-unrelated', '-p:DefineConstants=REVIEWER_UNRELATED_CONST'),
  @('c18-prefixjunk', '-p:DefineConstants=XGAGAMBA_PRODUCTION'),
  @('c05-fixtureonly', '-p:DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE'),
  @('c06-contradictory', '-p:DefineConstants=GAGAMBA_PRODUCTION;GAGAMBA_WP05_D6_4_TEST_FIXTURE')
)
foreach ($c in $cases) {
  $log = Join-Path $Rev ("evidence\" + $c[0] + ".log")
  dotnet build $m1 -c Release $c[1] --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object {
      if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] }
    } | Sort-Object -Unique) -join ','
  Write-Output ("{0}: EXIT={1} DIAG={2}" -f $c[0], $code, $diag)
}
