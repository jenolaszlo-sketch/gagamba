param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$sln = Join-Path $r 'Gagamba.sln'
$m1proj = Join-Path $r 'src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$ownerproj = Join-Path $r 'src\Gagamba.Execution.Linux.M1.Owner\Gagamba.Execution.Linux.M1.Owner.csproj'
$m1rel = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Release\net10.0\Gagamba.Execution.Linux.M1.dll'
$m1dbg = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll'
$owndbg = Join-Path $r 'src\Gagamba.Execution.Linux.M1.Owner\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll'
$exedbg = Join-Path $r 'src\Gagamba.Execution\bin\Debug\net10.0\Gagamba.Execution.dll'
$exerel = Join-Path $r 'src\Gagamba.Execution\bin\Release\net10.0\Gagamba.Execution.dll'

function HashOf([string]$p) { if (Test-Path $p) { (Get-FileHash $p -Algorithm SHA256).Hash.Substring(0,16) } else { '<missing>' } }
function Run([string]$name, [string]$target, [string[]]$rest) {
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet build $target @rest --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+|error ' | ForEach-Object { if ($_.Line -match '(GAGAMBA\d+|error \w+)') { $Matches[1] } } | Sort-Object -Unique) -join ','
  $sev = (Select-String $log -Pattern 'Seven surfaces: \d/\d' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique) -join ' | '
  Write-Output ("{0}: EXIT={1} DIAG={2} SEV={3}" -f $name, $code, $diag, $sev)
}

Run 'e1-solution-debug' $sln @('-c','Debug')
Write-Output ("e1-hashes: m1dbg={0} owndbg-m1={1} exedbg={2}" -f (HashOf $m1dbg), (HashOf $owndbg), (HashOf $exedbg))
Run 'e2-release-after-debug' $sln @('-c','Release')
Run 'e3-m1-release-fresh' $m1proj @('-c','Release','--artifacts-path',(Join-Path $Rev 'work\e3-artifacts'))
Write-Output ("e4-pre-hash={0}" -f (HashOf $m1rel))
Run 'e4-incremental-release-m1' $m1proj @('-c','Release')
Write-Output ("e4-post-hash={0}" -f (HashOf $m1rel))
Run 'e5-solution-release-parallel-fresh' $sln @('-c','Release','-m','--artifacts-path',(Join-Path $Rev 'work\e5-artifacts'))
$e5m1 = Join-Path $Rev 'work\e5-artifacts\bin\Gagamba.Execution.Linux.M1\release\Gagamba.Execution.Linux.M1.dll'
Write-Output ("e5-fresh-m1-release-hash={0} (in-place={1})" -f (HashOf $e5m1), (HashOf $m1rel))
# E8: stale production binary inside owner Debug output, then owner Debug build
Copy-Item $m1rel $owndbg -Force
Write-Output ("e8-pre-stale-owner-debug-m1={0} (prod={1} debug={2})" -f (HashOf $owndbg), (HashOf $m1rel), (HashOf $m1dbg))
Run 'e8-owner-debug-over-stale-prod' $ownerproj @('-c','Debug')
Write-Output ("e8-post-owner-debug-m1={0}" -f (HashOf $owndbg))
