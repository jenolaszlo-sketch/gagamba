# RC2A-02C reproduction commands

Run from the repository root at final tested commit
`c4837b0b434fc75ebc3e08d278bdc09c3965a181` on Windows with the .NET SDK
10.0.401. Package versions come from the committed project files. The successful
restore uses cached packages and disables only NuGet vulnerability auditing,
whose service endpoint was unreachable in the recorded network environment.

## Build attempts

Initial command (restore failed with NU1900 because the NuGet vulnerability
service could not be reached):

```powershell
dotnet build tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --verbosity minimal --nologo
```

Successful offline-compatible build:

```powershell
dotnet build tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --verbosity minimal --nologo -p:NuGetAudit=false
```

The result was 0 warnings and 0 errors. The property does not change package
versions or project source.

## Focused test run

An earlier unprivileged attempt at tests-only commit
`90895ee0865caeeacce2570dce836f25e1e5279f` used the command below. It aborted
before running tests because VSTest could not connect to its testhost within 90
seconds. Its TRX, log and exit file are retained under
`runs/windows/iteration-90895ee/`.

```powershell
$runDir = (Resolve-Path 'docs\evidence\restricted-consumer-contract-rc2a-02c-q2as-rcd04\runs\windows\iteration-90895ee').Path
$env:RC2A02C_EVIDENCE_DIRECTORY = $runDir
$filter = 'FullyQualifiedName~RestrictedContractTests.PassiveReferenceCopiesAndComparesContents|FullyQualifiedName~RestrictedContractTests.PassiveReferenceRejectsInvalidInputsByParameterName|FullyQualifiedName~RestrictedContractTests.PublicSurfaceIsSealedReadOnlyAndPassive'
dotnet test tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --no-build --no-restore --filter $filter --logger 'trx;LogFileName=rc2a-02c-passive-reference.trx' --results-directory $runDir --verbosity minimal
$code = $LASTEXITCODE
Set-Content -Encoding ascii (Join-Path $runDir 'test.exit') $code
exit $code
```

The same filter passed in an earlier successful iteration at commit
`90895ee0865caeeacce2570dce836f25e1e5279f`; its artifacts are under
`runs/windows/iteration-90895ee/`. The final evidence run at
`c4837b0b434fc75ebc3e08d278bdc09c3965a181` uses the command below, so the
complete inherited public surface is included in the inventory:

```powershell
$runDir = (Resolve-Path 'docs\evidence\restricted-consumer-contract-rc2a-02c-q2as-rcd04\runs\windows\final').Path
$env:RC2A02C_EVIDENCE_DIRECTORY = $runDir
$filter = 'FullyQualifiedName~RestrictedContractTests.PassiveReferenceCopiesAndComparesContents|FullyQualifiedName~RestrictedContractTests.PassiveReferenceRejectsInvalidInputsByParameterName|FullyQualifiedName~RestrictedContractTests.PublicSurfaceIsSealedReadOnlyAndPassive'
dotnet test tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --no-build --no-restore --filter $filter --logger 'trx;LogFileName=rc2a-02c-passive-reference.trx' --results-directory $runDir --verbosity minimal --diag (Join-Path $runDir 'vstest-diag.log') 2>&1 | Tee-Object -FilePath (Join-Path $runDir 'test.log')
$code = $LASTEXITCODE
Set-Content -Encoding ascii (Join-Path $runDir 'test.exit') $code
exit $code
```

The final run completed with 3 passed, 0 failed and 0 skipped. The reflection test writes
`public-surface-inventory.json` into the directory named by
`RC2A02C_EVIDENCE_DIRECTORY`.
