param([ValidateSet('Debug','Release')][string]$Configuration)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../Gagamba')).Path
$run = Join-Path $PSScriptRoot 'run'
$logs = Join-Path $run 'logs'
$config = $Configuration.ToLowerInvariant()
$m1 = 'Gagamba.Execution.Linux.M1'
$core = "$run/candidate/bin/$m1/$config"
$witness = "$repo/tests/$m1.Tests/Bootstrap/ValidationPreCodecVectors.cs"
$source = "$repo/src/$m1/Bootstrap/Gava1ContextCodec.cs"
$altered = "$run/sensitivity-$config.cs"
$text = Get-Content -LiteralPath $source -Raw
$expression = '!' + 'originals.MatchesCatalog(frame[(HeaderSize + DigestSize)..])'
if ([regex]::Matches($text, [regex]::Escape($expression)).Count -ne 1) { throw 'Wrong sensitivity input' }
[IO.File]::WriteAllText($altered, $text.Replace('class Gava1ContextCodec', 'class Gava1SensitivityCopy').Replace($expression, 'false'))
$witnessDoc = (Get-Item "$repo/docs/reviews/*03-qe1-frozen-record-prep-01-evidence/binary-vectors.md").FullName
$preimage = [regex]::Match((Get-Content $witnessDoc -Raw), '(?m)^Preimage ([a-f0-9]+)').Groups[1].Value
$r01 = [regex]::Match((Get-Content $witnessDoc -Raw), '(?m)^R01 ([a-f0-9]+)').Groups[1].Value
$bytes = [Convert]::FromHexString($preimage)
$digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
if ($bytes.Length -ne 258 -or $digest -cne $r01 -or !(Select-String -LiteralPath "$PSScriptRoot/Program.cs" -SimpleMatch $r01)) { throw 'R01 mismatch' }
@{ witness=$witnessDoc; size=$bytes.Length; digest=$digest } | ConvertTo-Json | Set-Content "$logs/release-witness-$config.json"
$env:NuGetAudit = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$artifacts = "$run/probe-exact-$config"
$references = "$run/probe-references-$config"
New-Item -ItemType Directory -Force $references | Out-Null
foreach ($dep in @("$m1.dll", 'Gagamba.Execution.dll')) {
    Copy-Item -LiteralPath "$core/$dep" -Destination $references -Force
}
$argsBuild = @('build', "$PSScriptRoot/Probe.csproj", '-c', $Configuration, '--artifacts-path', $artifacts,
    '-p:UseSharedCompilation=false', "-p:WitnessSource=$witness", "-p:Core=$references", "-p:SensitivitySource=$altered")
$log = "$logs/probe-$config.log"
ConvertTo-Json -InputObject (@('dotnet') + $argsBuild) -Compress | Set-Content $log
dotnet @argsBuild *>> $log
$code = $LASTEXITCODE
"BUILD EXIT $code" | Add-Content $log
if ($code) { Get-Content $log; exit $code }
$probeDir = "$artifacts/bin/Probe/$config"
$probe = "$probeDir/$m1.Tests.dll"
foreach ($f in @($witness, $source, $altered, $probe, "$probeDir/$m1.dll", "$probeDir/Gagamba.Execution.dll")) {
    Get-FileHash -LiteralPath $f | ForEach-Object { "$($_.Hash)  $($_.Path)" } | Add-Content $log
}
foreach ($dep in @("$m1.dll", 'Gagamba.Execution.dll')) {
    if ((Get-FileHash "$probeDir/$dep").Hash -ne (Get-FileHash "$core/$dep").Hash) { throw 'Candidate dependency mismatch' }
}
"dotnet $probe" | Add-Content $log
dotnet $probe > "$logs/supplement-$config.tsv" 2>> $log
$code = $LASTEXITCODE
"PROBE EXIT $code" | Add-Content $log
Get-Content $log -Tail 3
exit $code
