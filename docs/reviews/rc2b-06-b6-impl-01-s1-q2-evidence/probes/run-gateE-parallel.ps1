param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$m1proj = Join-Path $r 'src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$rel = Join-Path $Rev 'work\e9-rel-artifacts'
$dbg = Join-Path $Rev 'work\e9-dbg-artifacts'
$p1 = Start-Process -FilePath 'dotnet' -ArgumentList @('build',$m1proj,'-c','Release','--artifacts-path',$rel,'--nologo') -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $Rev 'evidence\e9-rel-stdout.log') -RedirectStandardError (Join-Path $Rev 'evidence\e9-rel-stderr.log')
$p2 = Start-Process -FilePath 'dotnet' -ArgumentList @('build',$m1proj,'-c','Test','--artifacts-path',$dbg,'--nologo') -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $Rev 'evidence\e9-dbg-stdout.log') -RedirectStandardError (Join-Path $Rev 'evidence\e9-dbg-stderr.log')
Wait-Process -Id $p1.Id; Wait-Process -Id $p2.Id
Write-Output ("e9-rel-exit={0}" -f $p1.ExitCode)
Write-Output ("e9-test-exit={0}" -f $p2.ExitCode)
$relDll = Join-Path $rel 'bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll'
$testDll = Join-Path $dbg 'bin\Gagamba.Execution.Linux.M1\test\Gagamba.Execution.Linux.M1.dll'
$probe = Join-Path $Rev 'probes\Q2Inspect\bin\Release\net10.0\Q2Inspect.dll'
dotnet $probe --production $relDll *> (Join-Path $Rev 'evidence\e9-rel-inspect.log'); Write-Output ("e9-rel-inspect-exit={0}" -f $LASTEXITCODE)
dotnet $probe --instrumented $testDll *> (Join-Path $Rev 'evidence\e9-test-inspect.log'); Write-Output ("e9-test-inspect-exit={0}" -f $LASTEXITCODE)
Select-String (Join-Path $Rev 'evidence\e9-test-inspect.log') -Pattern 'Configuration|Seven surfaces|Q2-INSPECT' | ForEach-Object { $_.Line.Trim() }
