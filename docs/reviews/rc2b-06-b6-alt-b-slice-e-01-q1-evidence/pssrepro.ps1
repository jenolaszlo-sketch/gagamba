$ErrorActionPreference='Stop'
$ev = "C:\Users\Laszlos\AppData\Local\Temp\opencode\e1q1-pssrepro"
New-Item -ItemType Directory -Force $ev | Out-Null
$log = Join-Path $ev 'missing-symbol.log'
$argList = @('build','C:\Users\Laszlos\source\repos\Gagamba\src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj','-c','Release','--artifacts-path',(Join-Path $ev 'out'),'--nologo','-p:DefineConstants=TRACE')
('dotnet ' + ($argList -join ' ')) | Set-Content $log
& dotnet @argList >> $log 2>&1
$code = $LASTEXITCODE
$text = Get-Content -LiteralPath $log -Raw
Write-Output ("psversion=" + $PSVersionTable.PSVersion.ToString())
Write-Output ("exit=" + $code)
Write-Output ("contains_GAGAMBA002=" + $text.Contains('GAGAMBA002'))
Write-Output ("contains_spaced=" + $text.Contains('G A G A M B A 0 0 2'))
Write-Output ("text_len=" + $text.Length)
$bytes = [IO.File]::ReadAllBytes($log)
Write-Output ("first6bytes=" + (($bytes[0..5] | ForEach-Object { $_.ToString('X2') }) -join ' '))
Write-Output ("raw_dotnet_has_GAGAMBA002=" + (((& dotnet @argList 2>&1) | Out-String).Contains('GAGAMBA002')))
