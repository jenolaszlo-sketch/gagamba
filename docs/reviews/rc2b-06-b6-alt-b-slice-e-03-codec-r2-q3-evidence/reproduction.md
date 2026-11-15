# Q3 reproduction

Use fresh local scratch outputs. Set `$repo` to canonical Gagamba, `$run` to the external Q3 run directory, `$bundle` to this directory, `$originalBin` to the independently built original M1 artifact roots preserved from Q2, and `$r1Bin` to the R1 baseline artifact roots preserved from R2. Both original configurations were challenged using the same exact historical Q1 inputs and outputs. The R1 artifact hashes are checked against the R2 evidence before each exact Q2 replay. The candidate is freshly built from the verified canonical R2 checkout. [Identity ledger](raw/identities.json) · [Q2 artifact hashes](raw/q2-exact-bindings.txt)

```powershell
python "$bundle/preflight.py" $repo $run <prior-R1-preservation-ledger>
& "$bundle/Build-Candidate.ps1" -Source $repo -Artifacts "$run/candidate" -Logs "$run/logs"
& "$bundle/Run-Q3.ps1" -Repository $repo -Run $run -OriginalBin $originalBin -R1Bin $r1Bin
```

The runner uses the committed Q2 orchestration and Q1 scanner unchanged at their repository locations. For the scratch run it adds the R2 class filter and Q3 supplemental probe. The copied original Q1 corpus and Q2 findings probe are verified. All managed xUnit logs include their exact commands and exit codes. The original Q1 replays exit 1 when the 17 expected predecessor discrepancies reproduce. Candidate and supplemental runners exit 0. The 13-case findings executable exits 0 after recording complete diagnostic rows, including the R1 exceptions.

```powershell
$env:VSTEST_CONNECTION_TIMEOUT = '15'
dotnet vstest "$run/candidate/bin/Gagamba.Execution.Linux.M1.Tests/debug/Gagamba.Execution.Linux.M1.Tests.dll" '/TestCaseFilter:FullyQualifiedName~ValidationPreCodec|FullyQualifiedName~InstallationWireTests' "/ResultsDirectory:$run/vstest" '/Logger:trx;LogFileName=focused-debug.trx'
```

This attempt exited 1 for Windows testhost connection failure. No tests executed through that adapter. The xUnit v3 in-process runner supplied the measured results. The NuGet advisory feed failure was observed during Q2. Q3 used the established cached restore with `NuGetAudit=false`. The result establishes no package advisory status. The restricted environment denied `dotnet --info` workload-service enumeration. SDK and runtime versions were obtained using `dotnet --version` and `dotnet --list-runtimes`. [Environment](raw/environment.txt) · [Aborted VSTest](raw/vstest-debug.log) · [In-process input hashes](raw/test-bindings.json) · [Probe input hashes](raw/probe-bindings.json)

```powershell
Set-Location -LiteralPath $repo
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier"
$env:PYTHONDONTWRITEBYTECODE = '1'
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-controls"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
pwsh -NoProfile -File eng/verify-preparation.ps1
```

All these gates exited 0. The assembly verifier's separate production-mode run returns PASS/0 on actual Release M1 and expected policy refusal/1 on actual Debug M1. The unchanged metadata scan returns 0 with zero production callers and positive test controls. [Production controls and arguments](raw/production-controls.json) · [Preparation](raw/preparation-final.log) · [Release](raw/verifier-release.log) · [Debug](raw/verifier-debug.log) · [Exit ledger](raw/execution-exits.txt)

The checksum manifest covers the report and evidence except the manifest itself. Use exact Git blob bytes if checkout line endings have been converted. Locate the Q3 commit by its report path, for example with `git log -1 --diff-filter=A --format="%H %T" -- docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3.md`. [Manifest](SHA256SUMS.txt)
