param([switch]$BuildOnly)
$ErrorActionPreference = 'Stop'
$gitExe = (Get-Command git.exe).Source
$dotnetExe = (Get-Command dotnet.exe).Source
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$out = Join-Path $repo 'artifacts/ar7/P01'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$vs = & $vswhere -latest -products '*' -property installationPath
$dev = Join-Path $vs 'Common7/Tools/Launch-VsDevShell.ps1'
& $dev -Arch amd64 -HostArch amd64 -SkipAutomaticLocation | Out-Null
# This host's VS setup does not discover the installed SDK automatically.
# Pin and report the actual public headers/libraries; no private declarations.
$kit = 'C:\Program Files (x86)\Windows Kits\10'
$kitVersion = '10.0.26100.0'
$env:WindowsSDKVersion = $kitVersion
$env:INCLUDE += ";$kit\Include\$kitVersion\um;$kit\Include\$kitVersion\shared;$kit\Include\$kitVersion\ucrt"
$env:LIB += ";$kit\Lib\$kitVersion\um\x64;$kit\Lib\$kitVersion\ucrt\x64"
$binary = Join-Path $out 'p01.exe'
& cl.exe /nologo /std:c++17 /EHsc /W4 /MT /O2 (Join-Path $PSScriptRoot 'p01.cpp') "/Fe:$binary" "/Fo:$out/p01.obj" "/Fd:$out/p01.pdb" /link | Tee-Object -FilePath (Join-Path $out 'build.txt')
if ($LASTEXITCODE -ne 0) { throw 'Native build failed' }
if ($BuildOnly) { return }
$nonce = [Guid]::NewGuid().ToString('N')
$fixture = Join-Path $out "fixture-$nonce"
$run = Join-Path $out $nonce
New-Item -ItemType Directory -Path $run | Out-Null
$sourceState = & $gitExe -C $repo status --porcelain
$context = [ordered]@{
    baseline = '06c8ac7eff6b968804a5cdd6c651f1a0c6845d56'
    probeCommit = (& $gitExe -C $repo rev-parse HEAD)
    dirty = [bool]$sourceState
    sourceState = @($sourceState)
    nonce = $nonce; fixture = $fixture; profile = "GagambaAR7$nonce"
    sourceHash = (Get-FileHash (Join-Path $PSScriptRoot 'p01.cpp')).Hash
    scriptHash = (Get-FileHash $PSCommandPath).Hash
    helperHash = (Get-FileHash $binary).Hash
    os = [Environment]::OSVersion.VersionString
    architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    sdk = $env:WindowsSDKVersion; compiler = $env:VCToolsVersion
    powershell = $PSVersionTable.PSVersion.ToString()
    dotnet = (& $dotnetExe --version)
    watchdogSeconds = 30
}
$context | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'context.json')
# Observer is independent of the native host. Child jobs are associated atomically
# at creation and have kill-on-close, so terminating the host also stops children.
$process = Start-Process -FilePath $binary -ArgumentList "`"$fixture`" $nonce" -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $run 'stdout.txt') -RedirectStandardError (Join-Path $run 'stderr.txt')
$context.hostPid = $process.Id
$context.hostStartTimeUtc = $process.StartTime.ToUniversalTime().ToString('o')
$context | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'context.json')
$expired = -not $process.WaitForExit(30000)
if ($expired) { $process.Kill(); $process.WaitForExit(5000) | Out-Null }
$survivors = @(Get-Process -Name 'fixture' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq (Join-Path $fixture 'fixture.exe') })
$observation = [ordered]@{ hostExitCode = $process.ExitCode; watchdogExpired = $expired; fixtureAbsent = -not (Test-Path -LiteralPath $fixture); survivingFixtureProcesses = @($survivors | Select-Object Id,StartTime,Path) }
$observation | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $run 'observer.json')
Get-Content (Join-Path $run 'stdout.txt')
Get-Content (Join-Path $run 'stderr.txt')
$observation | ConvertTo-Json -Depth 5
if ($expired -or $process.ExitCode -ne 0 -or $survivors.Count -or -not $observation.fixtureAbsent) { throw "P01 unresolved; preserve $run and owned fixture identities for cleanup" }
