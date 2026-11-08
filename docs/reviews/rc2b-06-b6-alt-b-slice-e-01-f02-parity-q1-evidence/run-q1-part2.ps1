# F-02-PARITY-Q1 part 2: compile-valid ordering mutation + comment-strip IL comparison.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$q1 = "C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-q1"
$mut = "$q1\mut"
$out = "$q1\out\q1-mutations"
$pristine = "$q1\pristine"
$pre = "$mut\src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.Prepublication.cs"
$act = "$mut\src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.cs"
$prj = "$mut\src\Gagamba.Execution.Linux.M1\Installed\PrepublicationIntegrityProjection.cs"
$testProj = "$mut\tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj"
$probe = "$q1\probe\bin\Release\net10.0\Probe.dll"
$mutDll = "$mut\src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll"

function ReadL([string]$p) { [System.IO.File]::ReadAllLines($p) }
function WriteL([string]$p, $l) {
  [System.IO.File]::WriteAllText($p, (($l -join "`n") + "`n"))
  (Get-Item $p).LastWriteTime = Get-Date
}
function Find1($l, [string]$pat) {
  $h = @(for ($i = 0; $i -lt $l.Count; $i++) { if ($l[$i] -match $pat) { $i } })
  if ($h.Count -ne 1) { throw "pattern '$pat' matched $($h.Count) line(s)" }
  return [int]$h[0]
}
function Restore {
  Copy-Item "$pristine\Pre.cs" $pre -Force
  Copy-Item "$pristine\Active.cs" $act -Force
  Copy-Item "$pristine\Projection.cs" $prj -Force
  (Get-Item $pre).LastWriteTime = Get-Date
  (Get-Item $act).LastWriteTime = Get-Date
  (Get-Item $prj).LastWriteTime = Get-Date
}

function RunTests([string]$name, [string]$desc) {
  $cmd = "dotnet test `"$testProj`" -c Debug --nologo --filter `"FullyQualifiedName~InstalledVerificationParityTests|FullyQualifiedName~PrepublicationIntegrityProjectionTests`" 2>&1"
  $o = & cmd.exe /c $cmd
  $o | Out-File "$out\$name.out.txt" -Encoding utf8
  $sum = $o | Select-String -Pattern "^(Passed|Failed)!  - " | Select-Object -Last 1
  $sumLine = if ($sum) { $sum.Line.Trim() } else { "(no test summary)" }
  $failed = @($o | Select-String -Pattern "Failed (Gagamba\.Execution\.Linux\.M1\.Tests\.[A-Za-z0-9_.]+)" |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -Unique)
  $errs = @($o | Select-String -Pattern "error [A-Z]{2,4}\d+" | ForEach-Object { $_.Line.Trim() } | Select-Object -First 2)
  $asserts = @()
  for ($i = 0; $i -lt $o.Count; $i++) {
    if ($o[$i] -match '^\s*Error Message:\s*$' -and $i + 1 -lt $o.Count) {
      $m = $o[$i + 1].Trim()
      if ($m) { $asserts += $m; if ($asserts.Count -ge 4) { break } }
    }
  }
  $rec = "MUTATION: $name`nDESCRIPTION: $desc`nSUMMARY: $sumLine`nFAILED: $($failed -join '; ')`nBUILD-ERRORS: $($errs -join ' || ')`nASSERTIONS: $($asserts -join ' | ')`n"
  $rec | Out-File "$out\$name.summary.txt" -Encoding utf8
  Write-Host $rec
}

# ---- R-10b: compile-valid ordering mutation (swap EnsureExact and CheckDependencies) ----
Restore
$l = ReadL $pre
$iE = Find1 $l 'EnsureExact\(scan'
$iC = Find1 $l 'InstalledPayload\.CheckDependencies\('
$ensure = $l[$iE]; $deps = $l[$iC]
$l[$iE] = $deps; $l[$iC] = $ensure
WriteL $pre $l
RunTests "R-10b-reorder-EnsureExact-CheckDependencies-prepublication" "Swapped EnsureExact and InstalledPayload.CheckDependencies statements in VerifyActiveIntegrity (both calls remain present)"

# ---- Comment-strip IL comparison ----
Restore
$l = ReadL $pre
$i = Find1 $l '// F-02: the fixed-root integrity prefix below mirrors VerifyActive'
WriteL $pre @($l[0..($i - 1)] + $l[($i + 5)..($l.Count - 1)])
$l = ReadL $act
$i = Find1 $l '// F-02: the fixed-root integrity prefix below is mirrored by'
WriteL $act @($l[0..($i - 1)] + $l[($i + 5)..($l.Count - 1)])
Write-Host "comment blocks stripped; building..."
$build = & cmd.exe /c "dotnet build `"$mut\src\Gagamba.Execution.Linux.M1\Gagamba.Execution.Linux.M1.csproj`" -c Debug --nologo 2>&1"
$build | Out-File "$out\commentstrip.build.txt" -Encoding utf8
Write-Host ($build | Select-Object -Last 3)
Write-Host "=== stripped-build method IL hashes ==="
& dotnet $probe ilhash $mutDll "Gagamba.Execution.Linux.M1.Installed.InstalledFileSystem" "VerifyActive" | Tee-Object "$out\commentstrip.VerifyActive.ilhash.txt"
& dotnet $probe ilhash $mutDll "Gagamba.Execution.Linux.M1.Installed.InstalledFileSystem" "VerifyActiveIntegrity" | Tee-Object "$out\commentstrip.VerifyActiveIntegrity.ilhash.txt"
Write-Host "canonical Debug VerifyActive          = 62145748CDBCC2ED49DFB078ADEDA5954D3C38B44A16548BB17073792E547143 1030"
Write-Host "canonical Debug VerifyActiveIntegrity = 78C979236FBFE3F2A46F7B84EEB32FDD10945C1F0A75EF663321711314ACB5D9 739"
Restore
Write-Host "PART 2 COMPLETE"
