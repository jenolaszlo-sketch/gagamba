# Q4 execution and evidence verification

Qualification ran on Windows x64 with .NET SDK 10.0.401 and runtime 10.0.12. `NuGetAudit=false` selected the established cached restore arrangement. The SDK and runtime commands and exits are recorded in the environment log. The source was read from canonical `main` at `ffdad5d05fefb6d260c884d61c791c6d9be91df5`, with no non-documentation change from implementation commit `8be61181e804b2dfafb0797eb54c92d2f234ff86`. All new binaries, build intermediates and test execution outputs were under Solo. [Environment](raw/environment.txt) · [Repository audit](raw/preflight.json) · [Test and tool SHA-256](raw/artifacts.json)

This uses the committed Q3 build and runner scripts at their pinned revision. `$repo` is `C:/Users/Laszlos/source/repos/Gagamba` and `$scratch` is `C:/Users/Laszlos/source/repos/Solo/gagamba-e03-codec-r2-q4`. For a repeat run, use a fresh scratch directory next to the preserved earlier inputs in Solo. Copy the included Q4 wrapper to that directory. The wrapper checks the original baseline SHA-256 values against the committed probe bindings and stages the preserved R1 assemblies, whose hashes the committed runner checks. The fresh candidate DLLs have their own identities. The ten build commands and exit codes are in `raw/build-*.log` and the build exit ledger. [Candidate build script](../rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/Build-Candidate.ps1) · [Committed runner](../rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/Run-Q3.ps1) · [Build exits](raw/build-exits.txt) · [Replay exits](raw/replays.log) · [Wrapper](Run-Q4.ps1)

```powershell
$repo = 'C:/Users/Laszlos/source/repos/Gagamba'
$scratch = 'C:/Users/Laszlos/source/repos/Solo/gagamba-e03-codec-r2-q4'
$run = "$scratch/run"
$q3 = "$repo/docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence"
python "$scratch/preflight.py" $repo
& "$q3/Build-Candidate.ps1" -Source $repo -Artifacts "$run/candidate" -Logs "$run/logs"
pwsh -NoProfile -File "$scratch/Run-Q4.ps1" -Repository $repo
```

The preflight exited 0 on the starting checkout, using the retained OWNER-01 inventory in Solo. The exact baseline check is intended for the starting revision. [Preflight script](preflight.py)

The VSTest check ran `dotnet vstest` with `VSTEST_CONNECTION_TIMEOUT=15`, the Debug test DLL, the codec and installation-wire filter, and a TRX results argument. It exited 1 before tests executed. The measured runs use `dotnet <test-assembly> -noColor` with the selected `-class` arguments for focused suites and `-xml` for each result file. Their logs record the full paths, arguments and exits. Both configurations used xUnit.net v3 in-process `3.2.0+9e422989c1`. Each focused layout binds the actual corresponding candidate core. The broad suites preserve the existing Debug project-reference fixture and the test discovery path for actual Release inspection. [VSTest attempt](raw/vstest-debug.log) · [Debug focused command](raw/combined-focused-debug.log) · [Release focused command](raw/combined-focused-release.log) · [Debug broad command](raw/broad-debug.log) · [Release broad command](raw/broad-release.log) · [Managed artifact bindings](raw/test-bindings.json)

The Q1 original replay exits 1 for its expected 17 discrepancies and the R2 replay exits 0 in each configuration. The two Q2 and Q3 supplemental runs exit 0 with 876 and 56 matched rows per configuration. The 13-case Q2 diagnostic runner exits 0 after recording each case, including the expected predecessor exceptions. Its explicit command and artifact bindings are recorded separately. [Debug Q1 replay](raw/replay-candidate-debug.log) · [Release Q1 replay](raw/replay-candidate-release.log) · [Debug supplement](raw/supplement-debug.log) · [Release supplement](raw/supplement-release.log) · [Exact Q2 commands](raw/q2-exact-commands.json) · [Probe bindings](raw/probe-bindings.json) · [Exact Q2 bindings](raw/q2-exact-bindings.txt)

The following ran separately from canonical `main` and exited 0. The build-policy ledger records all 51 individual commands and exit codes, including expected refusals. The compiled scan records the ordered input paths and SHA-256 values. The verifier logs bind the actual inspected core MVIDs and hashes to their exit codes. The P4 command ledger records the exact source revisions and scan expression. [Per-control arguments and results](raw/production-controls.json) · [Preparation regressions](raw/preparation-tests.log) · [Preparation validation](raw/preparation.log) · [Compiled scan command](raw/compiled-scan-command.json) · [Release verification](raw/verifier-release.log) · [Debug refusal](raw/verifier-debug.log) · [P4 command ledger](raw/p4-command.json)

```powershell
Set-Location -LiteralPath $repo
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier"
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-controls"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
pwsh -NoProfile -File eng/verify-preparation.ps1
```

The manifest covers the report and every file under this directory except itself. Evidence text uses LF and logs have trailing formatting whitespace removed. Verify the committed bytes with the following from the evidence directory in Git Bash. If checkout conversion changes the bytes, use the Git blob bytes from the Q4 commit. [SHA-256 manifest](SHA256SUMS.txt)

```bash
sha256sum --check SHA256SUMS.txt
```
