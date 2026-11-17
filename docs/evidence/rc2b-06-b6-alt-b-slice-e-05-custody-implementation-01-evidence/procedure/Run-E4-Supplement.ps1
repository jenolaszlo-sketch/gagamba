param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string]$Scratch)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath $Repository).Path
$run = (Resolve-Path -LiteralPath $Scratch).Path
$q = Join-Path $repo 'docs/reviews/rc2b-06-b6-alt-b-slice-e-04-codec-q1-evidence/procedure'
$probe = Join-Path $run 'e4-supplement'
New-Item -ItemType Directory -Force $probe,(Join-Path $run 'logs') | Out-Null
Copy-Item -LiteralPath "$q/Program.cs","$q/Probe.csproj" -Destination $probe
$script = [IO.File]::ReadAllText("$q/Run-Probe.ps1")
$script = $script.Replace('$repo = (Resolve-Path (Join-Path $PSScriptRoot ''../../Gagamba'')).Path',
    ('$repo = ' + "'" + $repo.Replace("'", "''") + "'"))
$script = $script.Replace('$run = Join-Path $PSScriptRoot ''run''',
    ('$run = ' + "'" + $run.Replace("'", "''") + "'"))
$script = $script.Replace('$run/candidate/bin','$run/candidate-exact/bin')
[IO.File]::WriteAllText("$probe/Run-E4-Probe.ps1", $script)
foreach ($cfg in 'Debug','Release') {
    & "$probe/Run-E4-Probe.ps1" -Configuration $cfg
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}
exit 0
