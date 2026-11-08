# F-02-PARITY-Q1 reviewer-owned mutation harness (external scratch only).
# Runs 1 positive control + 11 removal/reorder mutations + 4 call-preserving mutations
# against the committed parity suite and the E-1/F-01 projection class.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$q1 = "C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-q1"
$mut = "$q1\mut"
$out = "$q1\out\q1-mutations"
$pristine = "$q1\pristine"
New-Item -ItemType Directory -Force -Path $out, $pristine | Out-Null

$pre = "$mut\src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.Prepublication.cs"
$act = "$mut\src\Gagamba.Execution.Linux.M1\Installed\InstalledFileSystem.cs"
$prj = "$mut\src\Gagamba.Execution.Linux.M1\Installed\PrepublicationIntegrityProjection.cs"
$testProj = "$mut\tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj"
$probe = "$q1\probe\bin\Release\net10.0\Probe.dll"
$mutDll = "$mut\src\Gagamba.Execution.Linux.M1\bin\Debug\net10.0\Gagamba.Execution.Linux.M1.dll"

Copy-Item $pre "$pristine\Pre.cs" -Force
Copy-Item $act "$pristine\Active.cs" -Force
Copy-Item $prj "$pristine\Projection.cs" -Force

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
function Cut($l, [int]$i) { return @($l[0..($i - 1)] + $l[($i + 1)..($l.Count - 1)]) }
function Restore {
  Copy-Item "$pristine\Pre.cs" $pre -Force
  Copy-Item "$pristine\Active.cs" $act -Force
  Copy-Item "$pristine\Projection.cs" $prj -Force
  (Get-Item $pre).LastWriteTime = Get-Date
  (Get-Item $act).LastWriteTime = Get-Date
  (Get-Item $prj).LastWriteTime = Get-Date
}

$records = New-Object System.Collections.Generic.List[string]

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
  $records.Add($rec)
}

# ---------------- Q0: positive control on the untouched scratch copy ----------------
Restore
RunTests "Q0-baseline" "Unmutated scratch copy; expected: parity 10/10 + projection class 23/23 all pass"
Write-Host "=== baseline IL hashes (mut build) ==="
& dotnet $probe ilhash $mutDll "Gagamba.Execution.Linux.M1.Installed.InstalledFileSystem" "VerifyActive" | Tee-Object "$out\Q0.VerifyActive.ilhash.txt"
& dotnet $probe ilhash $mutDll "Gagamba.Execution.Linux.M1.Installed.InstalledFileSystem" "VerifyActiveIntegrity" | Tee-Object "$out\Q0.VerifyActiveIntegrity.ilhash.txt"

# ---------------- Removal / reorder matrix ----------------
Restore
$l = ReadL $pre; $i = Find1 $l 'Walk\(generation\.Value'; WriteL $pre (Cut $l $i)
RunTests "R-01-remove-Walk-prepublication" "Deleted 'Walk(generation.Value, ...)' from VerifyActiveIntegrity"

Restore
$l = ReadL $pre; $i = Find1 $l 'EnsureExact\(scan'; WriteL $pre (Cut $l $i)
RunTests "R-02-remove-EnsureExact-prepublication" "Deleted 'EnsureExact(scan, includeReceipt: true);' from VerifyActiveIntegrity"

Restore
$l = ReadL $pre; $i = Find1 $l 'InstalledPayload\.CheckDependencies\('; WriteL $pre (Cut $l $i)
RunTests "R-03-remove-CheckDependencies-prepublication" "Deleted 'InstalledPayload.CheckDependencies(manifest, scan.Configs);' from VerifyActiveIntegrity"

Restore
$l = ReadL $pre; $i = Find1 $l 'InstalledPayload\.CheckHost\(manifest\);'; WriteL $pre (Cut $l $i)
RunTests "R-04-remove-CheckHost-prepublication" "Deleted 'InstalledPayload.CheckHost(manifest);' from VerifyActiveIntegrity"

Restore
$l = ReadL $act; $i = Find1 $l 'InstalledPayload\.CheckHost\(manifest\);'; WriteL $act (Cut $l $i)
RunTests "R-05-remove-CheckHost-qualified" "Deleted the qualified-branch 'InstalledPayload.CheckHost(manifest);' from VerifyActive"

Restore
$l = ReadL $pre
$i = Find1 $l 'RecheckSelectors\(root\.Value'
$j = Find1 $l 'generationBefore, manifestBefore, receiptBefore, null, default\);'
WriteL $pre @($l[0..($i - 1)] + $l[($j + 1)..($l.Count - 1)])
RunTests "R-06-remove-RecheckSelectors-prepublication" "Deleted the 3-line RecheckSelectors(...) block from VerifyActiveIntegrity"

Restore
$l = ReadL $act; $i = Find1 $l 'InstalledMetadata\.AcceptRecord\(recordBytes'; WriteL $act (Cut $l $i)
RunTests "R-07-remove-AcceptRecord-qualified" "Deleted 'InstalledMetadata.AcceptRecord(recordBytes, receipt, manifest);' from VerifyActive"

Restore
$l = ReadL $pre; $i = Find1 $l '_ = InstalledMetadata\.Receipt\('
$inject = '            InstalledMetadata.AcceptRecord(receiptBytes, InstalledMetadata.Receipt(receiptBytes, manifest), manifest); // Q1 MUTATION'
WriteL $pre @($l[0..$i] + $inject + $l[($i + 1)..($l.Count - 1)])
RunTests "R-08-inject-AcceptRecord-prepublication" "Injected an InstalledMetadata.AcceptRecord call into VerifyActiveIntegrity"

Restore
$l = ReadL $act; $i = Find1 $l 'InstalledMetadata\.SelectedRecordName\(receipt\)'
$l[$i] = '            string recordName = receipt.QualificationId ?? throw new InvalidOperationException("q1 mutation"); // Q1 MUTATION'
WriteL $act $l
RunTests "R-09-remove-SelectedRecordName-qualified" "Replaced InstalledMetadata.SelectedRecordName(receipt) with a direct field read in VerifyActive"

Restore
$l = ReadL $pre
$iS = Find1 $l 'SameDigest\(manifest\.Digest'
$iR = Find1 $l 'InstalledMetadata\.Receipt\('
$same = $l[$iS]; $recpt = $l[$iR]
$l[$iS] = $recpt; $l[$iR] = $same
WriteL $pre $l
RunTests "R-10-reorder-SameDigest-Receipt-prepublication" "Swapped SameDigest and InstalledMetadata.Receipt statements in VerifyActiveIntegrity"

Restore
$l = ReadL $act; $i = Find1 $l 'Walk\(generation\.Value'; WriteL $act (Cut $l $i)
RunTests "R-11-remove-Walk-qualified" "Deleted 'Walk(generation.Value, ...)' from VerifyActive"

# ---------------- Call-preserving bypasses ----------------
Restore
$l = ReadL $pre; $i = Find1 $l 'InstalledPayload\.CheckHost\(manifest\);'
$l[$i] = '            if (Environment.GetEnvironmentVariable("GAGAMBA_Q1_SKIP_HOST") is null) { InstalledPayload.CheckHost(manifest); } // Q1 MUTATION'
WriteL $pre $l
RunTests "CP-01-CheckHost-behind-env-guard-prepublication" "Prepublication CheckHost guarded by an environment variable (bypassable check; IL call token preserved)"

Restore
$l = ReadL $pre
$i = Find1 $l 'RecheckSelectors\(root\.Value'
$j = Find1 $l 'generationBefore, manifestBefore, receiptBefore, null, default\);'
$swallow = '            try { RecheckSelectors(root.Value, installations.Value, qualifications.Value, digest, rootBefore, installationsBefore, qualificationsBefore, activeBefore, generationBefore, manifestBefore, receiptBefore, null, default); } catch (Exception) { } // Q1 MUTATION'
WriteL $pre @($l[0..($i - 1)] + $swallow + $l[($j + 1)..($l.Count - 1)])
RunTests "CP-02-RecheckSelectors-swallowing-prepublication" "Prepublication RecheckSelectors wrapped in a swallowing try/catch (non-enforcing recheck; IL call token preserved)"

Restore
$l = ReadL $act; $i = Find1 $l 'InstalledMetadata\.AcceptRecord\(recordBytes'
$l[$i] = '            if (Environment.GetEnvironmentVariable("GAGAMBA_Q1_SKIP_RECORD") is null) { InstalledMetadata.AcceptRecord(recordBytes, receipt, manifest); } // Q1 MUTATION'
WriteL $act $l
RunTests "CP-03-AcceptRecord-behind-env-guard-qualified" "Qualified-path AcceptRecord guarded by an environment variable (record acceptance bypassable; IL call token preserved)"

Restore
$l = ReadL $prj
$i = Find1 $l 'InstallJson\.Require\(evidence\.OwnerClosureSha256'
$j = Find1 $l '"prepublication runtime inventory mismatch"\);'
$wrap = @($l[0..($i - 1)] + '        if (Environment.ProcessId < 0) { // Q1 MUTATION' + $l[$i..$j] + '        } // Q1 MUTATION' + $l[($j + 1)..($l.Count - 1)])
WriteL $prj $wrap
RunTests "CP-04-F01-semantic-requires-behind-false-guard" "F-01 semantic Require checks in FromEvidence wrapped in a runtime-false guard (IL tokens preserved)"

Restore
$records | Out-File "$out\results.txt" -Encoding utf8
Write-Host "ALL Q1 MUTATIONS COMPLETE"
