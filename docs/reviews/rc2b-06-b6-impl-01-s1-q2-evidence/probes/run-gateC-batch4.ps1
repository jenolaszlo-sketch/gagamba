param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$m1proj = Join-Path $r 'src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$bin = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Release\net10.0\Gagamba.Execution.Linux.M1.dll'
$debugDll = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll'

function HashOf([string]$p) { (Get-FileHash $p -Algorithm SHA256).Hash.Substring(0,16) }
function Run([string]$name, [string[]]$rest, [string]$target = $m1proj) {
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet build $target @rest --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object { if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] } } | Sort-Object -Unique) -join ','
  $sev = (Select-String $log -Pattern 'Seven surfaces: \d/\d' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique) -join ' | '
  Write-Output ("{0}: EXIT={1} DIAG={2} SEV={3} BIN={4}" -f $name, $code, $diag, $sev, (HashOf $bin))
}

Write-Output ("c11-pre-hash-of-release-bin={0}" -f (HashOf $bin))
Run 'c11-contradictory-after-valid' @('-c','Release','-p:DefineConstants=TRACE')
Write-Output ("c11-post-contradiction-bin={0}" -f (HashOf $bin))
Run 'c13-no-restore-contradictory' @('-c','Release','-p:DefineConstants=','--no-restore')
Write-Output ("c13-post-bin={0}" -f (HashOf $bin))
Run 'c14a-no-build-valid' @('-c','Release','--no-build')
Write-Output ("c14a-post-bin={0}" -f (HashOf $bin))
Copy-Item $debugDll $bin -Force
Write-Output ("c14b-corrupted-with-debug-bin={0}" -f (HashOf $bin))
Run 'c14b-no-build-corrupt' @('-c','Release','--no-build')
Write-Output ("c14b-post-nobuild-bin={0}" -f (HashOf $bin))
Run 'c12-valid-rebuild-over-corrupt' @('-c','Release')
Write-Output ("c12-post-repair-bin={0}" -f (HashOf $bin))
Copy-Item $debugDll $bin -Force
Write-Output ("c12b-corrupted-again-bin={0}" -f (HashOf $bin))
Run 'c12c-incremental-valid-over-corrupt' @('-c','Release')
Write-Output ("c12c-post-bin={0}" -f (HashOf $bin))
