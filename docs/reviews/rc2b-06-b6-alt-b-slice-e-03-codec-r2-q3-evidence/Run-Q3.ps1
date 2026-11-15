param([string]$Repository,[string]$Run,[string]$OriginalBin,[string]$R1Bin)
$ErrorActionPreference='Stop'
$env:NuGetAudit='false'
$e3='Gagamba.Execution.Linux.M1'
$logs="$Run/logs"
$q2="$Repository/docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r1-q2-evidence"
$coreHashes=@{
 debug='4E107CF8C22637D8925F678E4AE102BCEDB908AA4F2EFE60476B63B1A025F4DA'
 release='BE3862B0124DD0EC09C6FEE2B0042F7A22175CCAAFFB683B9777184C88DE2F79'
}
$src=Get-Content -LiteralPath "$q2/Run-Q2.ps1" -Raw
# Pass the preserved original build through the generated runner's binding.
$env:GAGAMBA_Q3_ORIGINAL_BIN=$OriginalBin
$src=$src.Replace('$original = Join-Path $Run ''original/bin''','$original = $env:GAGAMBA_Q3_ORIGINAL_BIN')
$needle='        Invoke-Logged "combined-focused-$config" (@($exe,''-noColor'')+$old+$new+@(''-xml'',"$logs/combined-focused-$config.xml")) 0'
if (!$src.Contains($needle)){throw 'The committed runner does not match the expected structure'}
$src=$src.Replace($needle,@'
        $r2 = @('-class','*ValidationPreCodecR2Tests')
        Invoke-Logged "r2-focused-$config" (@($exe,'-noColor')+$r2+@('-xml',"$logs/r2-focused-$config.xml")) 0
        Invoke-Logged "combined-focused-$config" (@($exe,'-noColor')+$old+$new+$r2+@('-xml',"$logs/combined-focused-$config.xml")) 0
'@)
$src=$src.Replace("    Copy-Item -LiteralPath (Join-Path "+'$PSScriptRoot'+ " 'Q2Cases.cs') -Destination "+'$extraSource'+" -Force",
@'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Q2Cases.cs') -Destination $extraSource -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Q3Cases.cs') -Destination $extraSource -Force
'@)
$src=$src.Replace("'failures += Q2Cases.Run(output, Witness, capability);'","'failures += Q2Cases.Run(output, Witness, capability); failures += Q3Cases.Run(output, Witness, capability);'")
if (!$src.Contains('Q3Cases.Run') -or !$src.Contains("'Q3Cases.cs'") -or !$src.Contains('$env:GAGAMBA_Q3_ORIGINAL_BIN')) {throw 'Runner injection failed'}
Copy-Item -LiteralPath "$q2/Q2Cases.cs" -Destination "$Run/Q2Cases.cs" -Force
Copy-Item -LiteralPath "$PSScriptRoot/Q3Cases.cs" -Destination "$Run/Q3Cases.cs" -Force
[IO.File]::WriteAllText("$Run/expanded-runner.ps1",$src)
& "$Run/expanded-runner.ps1" -Repository $Repository -Run $Run

$probe="$Run/finding-project"
New-Item -Force -ItemType Directory $probe | Out-Null
foreach ($name in @('Program.cs','Findings.csproj')) {
    Copy-Item -LiteralPath "$q2/findings-probe/$name" -Destination $probe -Force
    if((Get-FileHash "$q2/findings-probe/$name").Hash -ne (Get-FileHash "$probe/$name").Hash){throw 'Q2 probe copy changed'}
}
foreach ($cfg in @('Debug','Release')){
    $c=$cfg.ToLowerInvariant()
    foreach($side in @('r1','r2')){
        $build = if($side -eq 'r1'){$R1Bin}else{"$Run/candidate/bin"}
        $dll="$build/$e3/$c"
        if($side -eq 'r1' -and (Get-FileHash "$dll/$e3.dll").Hash -ne $coreHashes[$c]){throw 'R1 DLL differs from R2 baseline build'}
        $a="$Run/findings-$side"
        & dotnet run --project "$probe/Findings.csproj" -c $cfg --artifacts-path $a -p:UseSharedCompilation=false "-p:Candidate=$dll" -- "$q2/findings-inputs" *> "$logs/q2-exact-$side-$c.jsonl"
        $status=$LASTEXITCODE
        "$status exact-$side-$c" | Add-Content "$logs/q3-exits.txt"
        if($status -ne 0){throw "Q2 replay failed for $side $cfg"}
        $copy="$a/bin/Findings/$c/$e3.dll"
        if((Get-FileHash $copy).Hash -ne (Get-FileHash "$dll/$e3.dll").Hash){throw 'Probe bound wrong DLL'}
        Get-FileHash $copy | ForEach-Object {"$($_.Hash) $($_.Path)"} | Add-Content "$logs/q2-exact-bindings.txt"
    }
}
