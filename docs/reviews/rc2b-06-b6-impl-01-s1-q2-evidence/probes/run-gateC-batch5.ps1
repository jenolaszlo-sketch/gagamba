param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$m1proj = Join-Path $r 'src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj'
$bin = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Release\net10.0\Gagamba.Execution.Linux.M1.dll'
$debugDll = Join-Path $r 'src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll'

function HashOf([string]$p) { (Get-FileHash $p -Algorithm SHA256).Hash.Substring(0,16) }
function Run([string]$name, [string]$tool, [string[]]$rest) {
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  & $tool @rest *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object { if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] } } | Sort-Object -Unique) -join ','
  $sev = (Select-String $log -Pattern 'Seven surfaces: \d/\d' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique) -join ' | '
  Write-Output ("{0}: EXIT={1} DIAG={2} SEV={3}" -f $name, $code, $diag, $sev)
}

Run 'c15-publish-valid' (Get-Command dotnet).Source @('publish',$m1proj,'-c','Release','-o',(Join-Path $Rev 'work\c15-publish'))
$p = Join-Path $Rev 'work\c15-publish\Gagamba.Execution.Linux.M1.dll'
Write-Output ("c15-published-bin={0}" -f (HashOf $p))
Run 'c16-publish-contradictory' (Get-Command dotnet).Source @('publish',$m1proj,'-c','Release','-p:DefineConstants=','-o',(Join-Path $Rev 'work\c16-publish'))
Write-Output ("c16-published-exists={0}" -f (Test-Path (Join-Path $Rev 'work\c16-publish\Gagamba.Execution.Linux.M1.dll')))
Run 'c17-publish-nobuild-valid' (Get-Command dotnet).Source @('publish',$m1proj,'-c','Release','--no-build','-o',(Join-Path $Rev 'work\c17-publish'))
Write-Output ("c17-published-bin={0}" -f (HashOf (Join-Path $Rev 'work\c17-publish\Gagamba.Execution.Linux.M1.dll')))
Copy-Item $debugDll $bin -Force
Write-Output ("c17b-corrupt-bin={0}" -f (HashOf $bin))
Run 'c17b-publish-nobuild-corrupt' (Get-Command dotnet).Source @('publish',$m1proj,'-c','Release','--no-build','-o',(Join-Path $Rev 'work\c17b-publish'))
Write-Output ("c17b-published-exists={0}" -f (Test-Path (Join-Path $Rev 'work\c17b-publish\Gagamba.Execution.Linux.M1.dll')))
Run 'c17c-publish-nobuild-contradictory' (Get-Command dotnet).Source @('publish',$m1proj,'-c','Release','-p:DefineConstants=','--no-build','-o',(Join-Path $Rev 'work\c17c-publish'))
Run 'c14c-msbuild-nobuild-valid' (Get-Command dotnet).Source @('msbuild',$m1proj,'-t:Build','-p:Configuration=Release','-p:NoBuild=true','--nologo','-v:m')
Write-Output ("c14c-post-bin={0}" -f (HashOf $bin))
Run 'c12d-valid-repair' (Get-Command dotnet).Source @('build',$m1proj,'-c','Release')
Write-Output ("c12d-post-bin={0}" -f (HashOf $bin))
