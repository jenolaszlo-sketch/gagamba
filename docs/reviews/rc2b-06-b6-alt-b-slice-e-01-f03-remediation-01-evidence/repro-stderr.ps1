$ErrorActionPreference='Stop'
$PSDefaultParameterValues['Out-File:Encoding']='utf8'
$repo='C:\Users\Laszlos\source\repos\Gagamba'
$tool=Join-Path $repo 'eng/VerifyProductionAssembly/VerifyProductionAssembly.csproj'
$dbg=Join-Path $repo 'src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll'
$a=@('run','--project',$tool,'-c','Release','--no-launch-profile','--','--production',$dbg)
$out=Join-Path $env:TEMP ('f03stderr-' + [Guid]::NewGuid().ToString('N')); New-Item -ItemType Directory -Force $out | Out-Null

# ORIGINAL pattern (Stop active): expect a NativeCommandError throw on stderr.
$log1=Join-Path $out 'orig.log'
('dotnet ' + ($a -join ' ')) | Set-Content -LiteralPath $log1 -Encoding UTF8
$threw=$false
try { & dotnet @a >> $log1 2>&1; $c1=$LASTEXITCODE } catch { $threw=$true; $c1=$null }
Write-Output "ORIG threw=$threw exit=$c1"

# FIX pattern: relax error preference only around the capture.
$log2=Join-Path $out 'fix.log'
('dotnet ' + ($a -join ' ')) | Set-Content -LiteralPath $log2 -Encoding UTF8
$eap=$ErrorActionPreference; $ErrorActionPreference='Continue'
try { & dotnet @a >> $log2 2>&1; $c2=$LASTEXITCODE } finally { $ErrorActionPreference=$eap }
$t2=Get-Content -LiteralPath $log2 -Raw -Encoding UTF8
Write-Output ("FIX  exit=$c2 containsFAIL=" + $t2.Contains('RESULT: FAIL'))
Remove-Item $out -Recurse -Force
