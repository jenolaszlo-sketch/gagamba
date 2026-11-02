param([string]$Rev)
$ErrorActionPreference = 'Continue'
$r = Join-Path $Rev 'src-remediated'
$owner = Join-Path $r 'src\Gagamba.Execution.Linux.M1.Owner\Gagamba.Execution.Linux.M1.Owner.csproj'
$recovery = Join-Path $r 'src\Gagamba.Execution.Linux.M1.Recovery\Gagamba.Execution.Linux.M1.Recovery.csproj'
$sln = Join-Path $r 'Gagamba.sln'

function Run([string]$name, [string]$target, [string[]]$rest) {
  $log = Join-Path $Rev ("evidence\" + $name + ".log")
  dotnet build $target @rest --nologo -v:m *> $log
  $code = $LASTEXITCODE
  $diag = (Select-String $log -Pattern 'GAGAMBA\d+' | ForEach-Object {
      if ($_.Line -match '(GAGAMBA\d+)') { $Matches[1] }
    } | Sort-Object -Unique) -join ','
  $sev = (Select-String $log -Pattern 'Seven surfaces: \d/\d' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique) -join ' | '
  Write-Output ("{0}: EXIT={1} DIAG={2} SEV={3}" -f $name, $code, $diag, $sev)
}

Run 'c09a-owner-contradictory' $owner @('-c','Release','-p:DefineConstants=')
Run 'c09b-recovery-contradictory' $recovery @('-c','Release','-p:DefineConstants=')
Run 'b3-owner-release-direct' $owner @('-c','Release','--artifacts-path',(Join-Path $Rev 'work\b3-owner-artifacts'))
Run 'b4-recovery-release-direct' $recovery @('-c','Release','--artifacts-path',(Join-Path $Rev 'work\b4-recovery-artifacts'))
Run 'c10-solution-contradictory' $sln @('-c','Release','-p:DefineConstants=')
