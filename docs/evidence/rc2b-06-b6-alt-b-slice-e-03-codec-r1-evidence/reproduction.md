# R1 producer verification reproduction

Use fresh external build, test and log roots. Use the canonical checkout at the commit that introduced this report, or verify that the three implementation blobs still match the recorded source ledger. The verification script uses the original external Q1 package as a read-only input. It validates the original Debug and Release DLLs against the original Q1 hash ledger before reuse. The Q1 report and all original evidence are also preserved in canonical Git. [Source ledger](raw/source-identities.json) · [Original package reproduction](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/reproduction.md)

```powershell
git log --diff-filter=A -1 --format='%H %T' -- docs/evidence/rc2b-06-b6-alt-b-slice-e-03-codec-r1.md
& "$bundle/Build-R1.ps1" -Source $repo -Artifacts "$run/qualified" -Logs "$run/logs"
& "$bundle/Verify-R1.ps1" -Source $repo -Run $run -Q1 $originalQ1
```

The baseline new-regression run exits 1 by design with 21 failures and three controls passing. The unchanged Q1 probe exits 1 against each original DLL and 0 against each corrected DLL. The artifact logs record each command, status, and negative control. Focused and R1 tests run with the original or corrected DLL of the named configuration in a disposable test layout. The broad test layout supplies the actual Release DLL at the existing test-only discovery path, while retaining the project-selected dependencies. [Exit ledger](raw/verification-exits.txt) · [Build exits](raw/build-exits.txt)

The ordinary VSTest attempt below failed at testhost connection after 15 seconds. The subsequent xUnit v3 runner invocation executes the test assembly in-process and writes the reported XML. Corrected Release was measured separately with the actual Release codec DLL for original focused cases, the R1 tests and the unchanged Q1 probe.

```powershell
$env:VSTEST_CONNECTION_TIMEOUT = '15'
dotnet vstest "$run/qualified/bin/Gagamba.Execution.Linux.M1.Tests/debug/Gagamba.Execution.Linux.M1.Tests.dll" '/TestCaseFilter:FullyQualifiedName~ValidationPreCodec|FullyQualifiedName~InstallationWireTests' "/ResultsDirectory:$run/vstest" '/Logger:trx;LogFileName=focused-debug.trx'
```

[Aborted VSTest log](raw/vstest-debug.log) · [Debug original focused](raw/focused-original-debug.xml) · [Release original focused](raw/focused-original-release.xml) · [Debug broad](raw/managed-broad-debug.xml) · [Release broad](raw/managed-broad-release.xml) · [Debug regressions before](raw/regression-before-debug.xml) · [Debug regressions after](raw/regression-after-debug.xml) · [Release regressions before](raw/regression-before-release.xml) · [Release regressions after](raw/regression-after-release.xml)

The codec/build scripts use `NuGetAudit=false` with cached dependencies. This bypasses the unavailable advisory feed and makes no package-vulnerability claim. The preparation checks use the unchanged repository scripts. The production-build validator's nested verifier uses an explicit external artifact directory.

```powershell
Set-Location -LiteralPath $repo
$env:PYTHONDONTWRITEBYTECODE = '1'
pwsh -NoProfile -File eng/verify-preparation.ps1
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/production-build-verifier"
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-build-controls"
```

[Preparation validation](raw/preparation-final.log) · [Preparation regression tests](raw/preparation-tests.log) · [51 build and binary controls](raw/production-build-controls.json) · [Production-build log](raw/production-build-controls.log)

The two exact replay ledgers record the original Q1 result, the repeated baseline result and the corrected result for each original counterexample. The full corrected ledgers contain all 2,383 assertions per configuration. R1 log and XML copies use portable UTF-8 line endings. The Q1 import is byte-exact. [Debug](raw/q1-all-after-debug.jsonl) · [Release](raw/q1-all-after-release.jsonl)
