param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$trxRoot = Join-Path $Rev 'work\trx-f1'
New-Item -ItemType Directory -Force $trxRoot | Out-Null
$projects = @(
  @('f-m1-debug', 'tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj'),
  @('f-execution-debug', 'tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj'),
  @('f-conformance-debug', 'tests\Gagamba.Conformance.Tests\Gagamba.Conformance.Tests.csproj'),
  @('f-runtime-debug', 'tests\Gagamba.Runtime.Tests\Gagamba.Runtime.Tests.csproj'),
  @('f-linux-debug', 'tests\Gagamba.Execution.Linux.Tests\Gagamba.Execution.Linux.Tests.csproj'),
  @('f-macos-debug', 'tests\Gagamba.Execution.MacOS.Tests\Gagamba.Execution.MacOS.Tests.csproj'),
  @('f-windows-debug', 'tests\Gagamba.Execution.Windows.Tests\Gagamba.Execution.Windows.Tests.csproj'),
  @('f-d64-debug', 'tests\Gagamba.Execution.Linux.M1.D6_4.Tests\Gagamba.Execution.Linux.M1.D6_4.Tests.csproj')
)
foreach ($p in $projects) {
  $name = $p[0]; $proj = Join-Path $r $p[1]
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet test $proj -c Debug --nologo --logger "trx;LogFileName=$name.trx" --results-directory $trxRoot *> $log
  $code = $LASTEXITCODE
  $summary = (Select-String $log -Pattern 'passed!|failed!|Total tests|Passed!|Failed!' | Select-Object -Last 2 | ForEach-Object { $_.Line.Trim() }) -join ' ;; '
  Write-Output ("{0}: EXIT={1} SUMMARY={2}" -f $name, $code, $summary)
}
