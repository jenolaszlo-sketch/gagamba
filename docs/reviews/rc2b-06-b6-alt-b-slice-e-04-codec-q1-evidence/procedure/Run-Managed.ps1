param([ValidateSet('Debug','Release')][string]$Configuration)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../Gagamba')).Path
$run = Join-Path $PSScriptRoot 'run'
$logs = Join-Path $run 'logs'
$bin = Join-Path $run 'candidate/bin'
$config = $Configuration.ToLowerInvariant()
$m1 = 'Gagamba.Execution.Linux.M1'
$test = "$m1.Tests"
$layout = "$run/broad-$config"
$actualRelease = "$layout/src/$m1/bin/Release/net10.0"
$focused = "$run/focused-$config"
$broad = "$layout/tests/$config"
New-Item -ItemType Directory -Force $actualRelease,$focused,$broad | Out-Null
Copy-Item -LiteralPath "$repo/Gagamba.sln" -Destination $layout
Copy-Item -Path "$bin/$m1/release/*.dll" -Destination $actualRelease
Copy-Item -Path "$bin/$test/$config/*" -Destination $focused -Recurse -Force
Copy-Item -Path "$bin/$test/$config/*" -Destination $broad -Recurse -Force
foreach ($dll in @("$m1.dll", 'Gagamba.Execution.dll')) {
    Copy-Item -LiteralPath "$bin/$m1/$config/$dll" -Destination $focused -Force
}
$bindings = @(
    @("$focused/$m1.dll", "$bin/$m1/$config/$m1.dll"),
    @("$focused/Gagamba.Execution.dll", "$bin/$m1/$config/Gagamba.Execution.dll"),
    @("$focused/$test.dll", "$bin/$test/$config/$test.dll"),
    @("$broad/$test.dll", "$bin/$test/$config/$test.dll"),
    @("$broad/$m1.dll", "$bin/$m1/debug/$m1.dll"),
    @("$actualRelease/$m1.dll", "$bin/$m1/release/$m1.dll")
) | ForEach-Object {
    $actual, $reference = $_
    $hash = (Get-FileHash -LiteralPath $actual).Hash
    if ($hash -ne (Get-FileHash -LiteralPath $reference).Hash) { throw "layout mismatch $actual" }
    @{ actual = $actual; reference = $reference; sha256 = $hash }
}
ConvertTo-Json -InputObject @($bindings) | Set-Content "$logs/bindings-$config.json"

function Run([string]$Name, [string[]]$Arguments) {
    $path = "$logs/$Name-$config.log"
    ConvertTo-Json -InputObject (@('dotnet') + $Arguments) -Compress | Set-Content $path
    & dotnet @Arguments *>> $path
    $code = $LASTEXITCODE
    "EXIT $code" | Add-Content $path
    Write-Host "$Name $config exit=$code"
    return $code
}

$env:VSTEST_CONNECTION_TIMEOUT = '15'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$e3 = @('ValidationPreCodecBootstrapTests','ValidationPreCodecBinaryTests','InstallationWireTests',
    'ValidationPreCodecRemediationTests','ValidationPreCodecR2Tests')
$suites = @(
    @{Name='e4'; Path="$focused/$test.dll"; Classes=@('Gava1ContextCodecTests')},
    @{Name='e3'; Path="$focused/$test.dll"; Classes=$e3},
    @{Name='broad'; Path="$broad/$test.dll"; Classes=@()}
)
foreach ($suite in $suites) {
    $name = $suite.Name
    $trx = "$name-$config.trx"
    $argsV = @('vstest', $suite.Path, "/Logger:trx;LogFileName=$trx", "/ResultsDirectory:$logs")
    if ($suite.Classes.Count) {
        $filter = ($suite.Classes | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
        $argsV += "/TestCaseFilter:$filter"
    }
    $code = Run "vstest-$name" $argsV
    if ($code -ne 0 -or !(Test-Path "$logs/$trx")) {
        $argsX = @($suite.Path, '-noColor')
        foreach ($class in $suite.Classes) { $argsX += @('-class', "*$class") }
        $argsX += @('-xml', "$logs/$name-$config.xml")
        if ((Run "xunit-$name" $argsX) -ne 0) { throw "$name $config failed" }
    }
}
