param([string]$Repository,[string]$Run)
$ErrorActionPreference='Stop'
$s=Split-Path -Parent (Split-Path -Parent $Run)
$q=Join-Path $s 'gagamba-e03-codec-r1-q2/package/docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence'
$env:NuGetAudit='false'
$source=Get-Content -Raw "$q/Run-Q2.ps1"
$source=$source.Replace("$"+'original = Join-Path $Run ''original/bin''',"$"+'original = Join-Path (Split-Path -Parent (Split-Path -Parent $Run)) ''gagamba-e03-codec-r1-q2/run/original/bin''')
$source=$source.Replace("    foreach ("+'$config in '+"@('debug','release')) {","    foreach ("+'$config in '+"@('debug','release')) {")
$needle='        Invoke-Logged "combined-focused-$config" (@($exe,''-noColor'')+$old+$new+@(''-xml'',"$logs/combined-focused-$config.xml")) 0'
if(!$source.Contains($needle)){throw 'upstream runner changed'}
$source=$source.Replace($needle,@'
        $r2 = @('-class','*ValidationPreCodecR2Tests')
        Invoke-Logged "r2-focused-$config" (@($exe,'-noColor')+$r2+@('-xml',"$logs/r2-focused-$config.xml")) 0
        Invoke-Logged "combined-focused-$config" (@($exe,'-noColor')+$old+$new+$r2+@('-xml',"$logs/combined-focused-$config.xml")) 0
'@)
[IO.File]::WriteAllText("$Run/Run-R2-expanded.ps1",$source)
Copy-Item -LiteralPath "$q/Q2Cases.cs" -Destination "$Run/Q2Cases.cs" -Force
& "$Run/Run-R2-expanded.ps1" -Repository $Repository -Run $Run
$lib='Gagamba.Execution.Linux.M1'
$tests="$lib.Tests"
foreach($cfg in @('Debug','Release')){
 $low=$cfg.ToLowerInvariant()
 $fixed="$Run/candidate/bin/$lib/$low"
 $baseline="$Run/before/build/bin/$lib/$low"
 $layout="$Run/r2-red/$low"
 New-Item -Force -ItemType Directory $layout | Out-Null
 Copy-Item -Path "$Run/candidate/bin/$tests/$low/*" -Destination $layout -Recurse -Force
 Copy-Item -Path "$baseline/*.dll" -Destination $layout -Force
 dotnet "$layout/$tests.dll" -noColor -class '*ValidationPreCodecR2Tests' -xml "$Run/logs/r2-before-$low.xml" *> "$Run/logs/r2-before-$low.log"
 $status=$LASTEXITCODE
 "$status r2-before-$low" | Add-Content "$Run/logs/q2-exits.txt"
 if($status -ne 1){throw "Expected red deployment ($cfg)"}
 dotnet run --project "$q/findings-probe/Findings.csproj" -c $cfg --artifacts-path "$Run/after-probe" -p:Candidate="$fixed" -- "$q/findings-inputs" *> "$Run/logs/after-$low.jsonl"
 if($LASTEXITCODE -ne 0){throw "Probe execution failed ($cfg)"}
 $rows=Get-Content "$Run/logs/after-$low.jsonl" | ForEach-Object { $_ | ConvertFrom-Json }
 if(@($rows).Count -ne 13 -or @($rows|Where-Object {$_.result -ne 'Expectation' -or $_.published}).Count){throw "Incorrect refusal ($cfg)"}
 "0 findings-after-$low" | Add-Content "$Run/logs/q2-exits.txt"
 Get-FileHash "$fixed/$lib.dll","$baseline/$lib.dll","$layout/$lib.dll","$layout/$tests.dll","$Run/after-probe/bin/Findings/$low/$lib.dll","$Run/before/probe/bin/Findings/$low/$lib.dll" |
 ForEach-Object {"$($_.Hash) $($_.Path)"} | Add-Content "$Run/logs/r2-bindings.txt"
}
