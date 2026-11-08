$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$scratch  = "C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-mutation"
$evidence = "C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-evidence\mutation"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null

$pre      = Join-Path $scratch "src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.Prepublication.cs"
$act      = Join-Path $scratch "src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.cs"
$testProj = Join-Path $scratch "tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj"
$pristine = Join-Path $scratch "_pristine"
New-Item -ItemType Directory -Force -Path $pristine | Out-Null
Copy-Item $pre (Join-Path $pristine "Pre.cs") -Force
Copy-Item $act (Join-Path $pristine "Active.cs") -Force

function Get-Lines([string]$path) { [System.IO.File]::ReadAllLines($path) }
function Save-Lines([string]$path, $lines) { [System.IO.File]::WriteAllText($path, (($lines -join "`n") + "`n")) }
function Find-One($lines, [string]$pattern) {
  $hits = @(for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i] -match $pattern) { $i } })
  if ($hits.Count -ne 1) { throw "pattern '$pattern' matched $($hits.Count) line(s)" }
  return [int]$hits[0]
}
function Remove-Line($lines, [int]$index) { return @($lines[0..($index - 1)] + $lines[($index + 1)..($lines.Count - 1)]) }
function Restore-Pristine {
  Copy-Item (Join-Path $pristine "Pre.cs") $pre -Force
  Copy-Item (Join-Path $pristine "Active.cs") $act -Force
  (Get-Item $pre).LastWriteTime = Get-Date
  (Get-Item $act).LastWriteTime = Get-Date
}

$results = New-Object System.Collections.Generic.List[string]

function Run-Parity([string]$name, [string]$description) {
  Write-Host "== $name =="
  $cmdLine = "dotnet test `"$testProj`" -c Debug --nologo --filter FullyQualifiedName~InstalledVerificationParityTests 2>&1"
  $out = & cmd.exe /c $cmdLine
  $out | Out-File (Join-Path $evidence "$name.out.txt") -Encoding utf8
  $summaryLine = $out | Select-String -Pattern "^(Passed|Failed)!  - " | Select-Object -Last 1
  if ($null -eq $summaryLine) {
    $summary = "(no test summary - build failure?)"
  } else {
    $summary = $summaryLine.Line.Trim()
  }
  $failed = @($out | Select-String -Pattern "Failed (Gagamba\.Execution\.Linux\.M1\.Tests\.Installed\.InstalledVerificationParityTests\.[A-Za-z0-9_]+)" |
    ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -Unique)
  $buildErr = @($out | Select-String -Pattern "error CS\d+" | ForEach-Object { $_.Line.Trim() } | Select-Object -First 3)
  $entry = "MUTATION: $name`nDESCRIPTION: $description`nSUMMARY: $summary`nFAILING-TESTS: $($failed -join '; ')`nBUILD-ERRORS: $($buildErr -join ' || ')`n"
  $entry | Out-File (Join-Path $evidence "$name.summary.txt") -Encoding utf8
  Write-Host $entry
  $results.Add($entry)
}

# ---- M-00: positive control (unmutated scratch copy) ----
Restore-Pristine
Run-Parity "M-00-baseline" "Unmutated scratch copy; prediction: 10/10 pass (positive control)"

# ---- M-01: remove Walk from prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$i = Find-One $L 'Walk\(generation\.Value'
Save-Lines $pre (Remove-Line $L $i)
Run-Parity "M-01-remove-Walk-prepublication" "Removed the Walk(...) call from VerifyActiveIntegrity; prediction: SharedIntegrityPrefixAppearsInBothPaths and the shared-prefix order test fail"

# ---- M-02: remove CheckHost from prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$i = Find-One $L 'InstalledPayload\.CheckHost\(manifest\);'
Save-Lines $pre (Remove-Line $L $i)
Run-Parity "M-02-remove-CheckHost-prepublication" "Removed InstalledPayload.CheckHost(manifest) from VerifyActiveIntegrity; prediction: BothPathsRequireHostBaselineAndSelectorRecheck fails"

# ---- M-03: inject qualification-record acceptance into prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$i = Find-One $L '_ = InstalledMetadata\.Receipt\('
$inject = '            InstalledMetadata.AcceptRecord(receiptBytes, InstalledMetadata.Receipt(receiptBytes, manifest), manifest); // MUTATION'
Save-Lines $pre @($L[0..$i] + $inject + $L[($i + 1)..($L.Count - 1)])
Run-Parity "M-03-inject-AcceptRecord-prepublication" "Injected an InstalledMetadata.AcceptRecord call into VerifyActiveIntegrity; prediction: PrepublicationPathNeverEstablishesQualificationAssociation and the intentional-differences test fail"

# ---- M-04: remove AcceptRecord from qualified path ----
Restore-Pristine
$L = Get-Lines $act
$i = Find-One $L 'InstalledMetadata\.AcceptRecord\(recordBytes'
Save-Lines $act (Remove-Line $L $i)
Run-Parity "M-04-remove-AcceptRecord-qualified" "Removed InstalledMetadata.AcceptRecord(...) from VerifyActive; prediction: QualifiedPathRetainsRecordSelectionAndAcceptance and the intentional-differences test fail"

# ---- M-05: remove CheckHost from qualified path ----
Restore-Pristine
$L = Get-Lines $act
$i = Find-One $L 'InstalledPayload\.CheckHost\(manifest\);'
Save-Lines $act (Remove-Line $L $i)
Run-Parity "M-05-remove-CheckHost-qualified" "Removed InstalledPayload.CheckHost(manifest) from VerifyActive's qualified branch; prediction: BothPathsRequireHostBaselineAndSelectorRecheck fails"

# ---- M-06: remove RecheckSelectors from prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$i = Find-One $L 'RecheckSelectors\(root\.Value'
$j = Find-One $L 'generationBefore, manifestBefore, receiptBefore, null, default\);'
Save-Lines $pre @($L[0..($i - 1)] + $L[($j + 1)..($L.Count - 1)])
Run-Parity "M-06-remove-RecheckSelectors-prepublication" "Removed the RecheckSelectors(...) block from VerifyActiveIntegrity; prediction: BothPathsRequireHostBaselineAndSelectorRecheck fails"

# ---- M-07: swap Walk and EnsureExact order in prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$iW = Find-One $L 'Walk\(generation\.Value'
$iE = Find-One $L 'EnsureExact\(scan'
$walkLine = $L[$iW]
$ensureLine = $L[$iE]
$L[$iW] = $ensureLine
$L[$iE] = $walkLine
Save-Lines $pre $L
Run-Parity "M-07-swap-Walk-EnsureExact-prepublication" "Swapped the Walk and EnsureExact statements in VerifyActiveIntegrity; prediction: SharedIntegrityPrefixOrderIsIdenticalInBothPaths fails"

# ---- M-08: move CheckHost after the return statement (call-preserving bypass attempt) ----
Restore-Pristine
$L = Get-Lines $pre
$iC = Find-One $L 'InstalledPayload\.CheckHost\(manifest\);'
$callLine = $L[$iC]
$L2 = @($L[0..($iC - 1)] + $L[($iC + 1)..($L.Count - 1)])
$iR = Find-One $L2 'return new InstalledIntegrityEvidence\('
Save-Lines $pre @($L2[0..($iR + 2)] + $callLine + $L2[($iR + 3)..($L2.Count - 1)])
Run-Parity "M-08-move-CheckHost-after-return" "Moved the CheckHost call after the return statement in VerifyActiveIntegrity; prediction: build rejection (TreatWarningsAsErrors -> CS0162) or documented non-detection if it compiles"

# ---- M-09: conditional runtime-false bypass that preserves the call token ----
Restore-Pristine
$L = Get-Lines $pre
$iC = Find-One $L 'InstalledPayload\.CheckHost\(manifest\);'
$L[$iC] = '            if (Environment.ProcessId == 0) { InstalledPayload.CheckHost(manifest); } // MUTATION: call-preserving conditional bypass'
Save-Lines $pre $L
Run-Parity "M-09-conditional-bypass-CheckHost-prepublication" "Guarded CheckHost with an always-false runtime condition, preserving the IL call token; prediction: NOT detected statically or behaviorally (documented static-presence limitation)"

# ---- M-10: remove SelectedRecordName from qualified path ----
Restore-Pristine
$L = Get-Lines $act
$i = Find-One $L 'InstalledMetadata\.SelectedRecordName\(receipt\)'
$L[$i] = '            string recordName = receipt.QualificationId ?? throw new InvalidOperationException("mutation"); // MUTATION: record selection removed'
Save-Lines $act $L
Run-Parity "M-10-remove-SelectedRecordName-qualified" "Replaced InstalledMetadata.SelectedRecordName(receipt) with a direct field read in VerifyActive; prediction: QualifiedPathRetainsRecordSelectionAndAcceptance fails"

# ---- M-11: remove CheckDependencies from prepublication ----
Restore-Pristine
$L = Get-Lines $pre
$i = Find-One $L 'InstalledPayload\.CheckDependencies\('
Save-Lines $pre (Remove-Line $L $i)
Run-Parity "M-11-remove-CheckDependencies-prepublication" "Removed InstalledPayload.CheckDependencies(...) from VerifyActiveIntegrity; prediction: SharedIntegrityPrefixAppearsInBothPaths and the shared-prefix order test fail"

Restore-Pristine
$results | Out-File (Join-Path $evidence "results.txt") -Encoding utf8
Write-Host "ALL MUTATIONS COMPLETE"
