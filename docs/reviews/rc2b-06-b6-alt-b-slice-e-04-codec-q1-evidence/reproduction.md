# E-4 qualification procedure and evidence

`$repo` is the sole canonical Gagamba checkout and `$scratch` is `C:/Users/Laszlos/source/repos/Solo/gagamba-e04-codec-q1`. Copy the qualification procedure files into a fresh external scratch directory of the same depth under `Solo`, with `run/logs` created. The scripts use the sibling canonical Gagamba checkout and existing F02 and E-3 evidence packages for pinned historical inputs. Source inspection and project build commands ran against the canonical checkout. Generated binaries, probes, and test results stayed in external scratch. The preflight receipt binds the four authorized files and the exact normative hashes. [Preflight](preflight.json) · [Environment](raw/environment.txt)

```powershell
$repo = 'C:/Users/Laszlos/source/repos/Gagamba'
$scratch = 'C:/Users/Laszlos/source/repos/Solo/gagamba-e04-codec-q1'
$run = "$scratch/run"
New-Item -ItemType Directory -Force "$run/logs" | Out-Null
python -B "$scratch/preflight.py"
$q3 = "$repo/docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence"
& "$q3/Build-Candidate.ps1" -Source $repo -Artifacts "$run/candidate" -Logs "$run/logs"
& "$scratch/Run-Managed.ps1" -Configuration Debug
& "$scratch/Run-Managed.ps1" -Configuration Release
& "$scratch/Run-Probe.ps1" -Configuration Debug
& "$scratch/Run-Probe.ps1" -Configuration Release
& "$scratch/Audit-Metadata.ps1"
& "$scratch/Audit-P4.ps1"
```

The initial sandboxed build and policy harness attempts failed during project lookup. The retry used canonical read access with the normal Git and .NET path and all output under Solo. The ten project build logs record their configurations and statuses. The xUnit XML preserves actual test identities, and the separate VSTest logs and TRX record the six aborted runs. The supplemental TSV rows include case ID, clause, expected and observed categories, result availability, decision and SHA-256 of wire bytes where applicable. `Run-Probe.ps1` hashes the E-3 witness fixture and compares the staged runtime copies with the pinned fresh core artifacts. Its disposable scope recompiles a renamed copy omitting one equality comparison solely for the I02 sensitivity control. That row is separate from the 1,944 results on the exact production core. [Build exits](raw/build-exits.txt) · [Summary](summary.json) · [Procedure](procedure/)

The E-3 replay used the committed E-4 procedure in probes-only mode. Check the original Debug and Release M1 core hashes against the `Q1 replay original` entries in the pinned Q3 probe bindings. Copy the producer's `Q2Cases.cs`, `Q3Cases.cs` and `finding-project/` from `procedure/` into `$run`. Invoke `Run-E4.ps1` with the canonical `-Repository`, external `-Run`, and `-ProbesOnly`. The raw results preserve the predecessor sensitivity, matching candidate and bounded exact Q2 outcomes. [Replay command and exits](raw/e3-replay.log) · [Artifact bindings](raw/all-bindings.json)

Run from canonical Gagamba for the independent policy and preparation checks. The `production-controls.json` file preserves arguments and the observed and expected result of each probe. `Summarize.py` reconciles test counts and exact case membership against Q4 and checks the other logged outcomes. Save its JSON output with the run.

```powershell
Set-Location -LiteralPath $repo
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$env:PYTHONDONTWRITEBYTECODE = '1'
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-controls-retry"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
pwsh -NoProfile -File eng/verify-preparation.ps1
python -B "$scratch/Summarize.py"
```

The manifest covers the report and every file under this directory except the manifest itself. Logs were normalized to LF and trailing formatting whitespace removed before hashing the committed bytes. Verify from the evidence directory against the checkout on LF or use `Verify-Evidence.py` against Git blobs. After the commit, `preflight.py --post` verifies the local historical inventory and evidence again and checks that the candidate is the direct parent and the only additions are the report and annex. The final commit and tree receipt stays outside the self-hashed commit.

```powershell
python -B procedure/Verify-Evidence.py
```
