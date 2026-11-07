$ErrorActionPreference='Stop'
$repo = 'C:\Users\Laszlos\source\repos\Gagamba'
$m1 = Join-Path $repo 'src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj'
$out = Join-Path $env:TEMP ('f03art-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $out | Out-Null
$a = @('build',$m1,'-c','Release','--artifacts-path',$out,'--nologo','-p:DefineConstants=TRACE')

Write-Output ("PSVersion=" + $PSVersionTable.PSVersion.ToString())

# --- defect reproduction: header via Set-Content (no BOM/single-byte), output via >> (UTF-16LE) ---
$log1 = Join-Path $out 'orig.log'
('dotnet ' + ($a -join ' ')) | Set-Content $log1
& dotnet @a >> $log1 2>&1
$code1 = $LASTEXITCODE
$text1 = Get-Content -LiteralPath $log1 -Raw
Write-Output ("ORIG exit=$code1 containsGAGAMBA002=" + $text1.Contains('GAGAMBA002'))
$b1 = [IO.File]::ReadAllBytes($log1)
Write-Output ("ORIG first4=" + (($b1[0..3] | ForEach-Object { $_.ToString('X2') }) -join ' '))

# --- candidate fix: force UTF-8 for Out-File (>>), Set-Content and read ---
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$PSDefaultParameterValues['Set-Content:Encoding'] = 'utf8'
$log2 = Join-Path $out 'fix.log'
('dotnet ' + ($a -join ' ')) | Set-Content $log2
& dotnet @a >> $log2 2>&1
$code2 = $LASTEXITCODE
$text2 = Get-Content -LiteralPath $log2 -Raw -Encoding UTF8
Write-Output ("FIX  exit=$code2 containsGAGAMBA002=" + $text2.Contains('GAGAMBA002'))
$b2 = [IO.File]::ReadAllBytes($log2)
Write-Output ("FIX  first4=" + (($b2[0..3] | ForEach-Object { $_.ToString('X2') }) -join ' '))
Remove-Item $out -Recurse -Force
