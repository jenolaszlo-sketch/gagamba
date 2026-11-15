# Reproduce Q2

Use fresh external artifact and results roots. Set `$repo` to canonical Gagamba, `$originalSource` to the preserved standalone checkout of `34eef4c8e7d814e15cb1bfce27df032e75766f19`, `$run` to an empty external directory and `$bundle` to this evidence directory. The canonical build read the verified R1 tree. Original and corrected artifacts were rebuilt separately. A further advisory-enabled restore of the original M1 project failed with `NU1900`. Subsequent builds resolved cached packages with advisory checks disabled, which establishes no package advisory result. [Preflight identities](raw/identities.json) · [Advisory restore log](raw/advisory-restore.log)

```powershell
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/original"
$originalProject = "$originalSource/src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj"
foreach ($cfg in @('Debug','Release')) {
    dotnet build $originalProject -c $cfg --nologo -m:1 --artifacts-path "$run/original"
}
& "$bundle/Build-PinnedCandidate.ps1" -Source $repo -Artifacts "$run/candidate" -Logs "$run/logs"
& "$bundle/Run-Q2.ps1" -Repository $repo -Run $run -ProbesOnly
& "$bundle/Run-Q2.ps1" -Repository $repo -Run $run -ManagedOnly
```

The original replays exit 1 for the 17 intentional reproduced divergences. The corrected replays exit 0. Supplemental Debug and Release exit 1 for the 13 new unexpected results. The script copies the original Q1 probe and scanner unchanged from the verified import. The supplementary extension is a separate reviewer-authored source file loaded only in the external probe build. [Probe bindings](raw/probe-bindings.json) · [Supplementary source](Q2Cases.cs) · [Execution exit codes](raw/q2-exits.txt)

The following form runs the separate exception stack capture. Apply it to each original/candidate and Debug/Release pair using the matching M1 output directory in `$binaryDir`. The probe returns 0 after all thirteen diagnostics have been captured, including codec exceptions. This is capture completeness, while the assertion probe reports the failure verdict.

```powershell
dotnet run --project "$bundle/findings-probe/Findings.csproj" -c $configuration --artifacts-path $freshProbeDir -p:UseSharedCompilation=false -p:Candidate="$binaryDir" -- "$bundle/findings-inputs"
```

VSTest 18.7.0 timed out after 15 seconds while connecting to testhost. Its aborted result was excluded. The xUnit v3 in-process runner executed the exact filters and wrote XML with explicit DLL hash bindings. The broad mirror made the actual Release artifact available to the two inherited reachability checks. SDK 10.0.401 and runtime 10.0.12 ran on Windows NT 10.0.26200.0 x64. [VSTest log](raw/vstest-debug.log) · [Test hashes](raw/test-bindings.json) · [Debug combined](raw/combined-focused-debug.xml) · [Release combined](raw/combined-focused-release.xml) · [Debug broader suite](raw/broad-debug.xml) · [Release broader suite](raw/broad-release.xml)

```powershell
Set-Location -LiteralPath $repo
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
pwsh -NoProfile -File eng/verify-preparation.ps1
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-controls"
```

The scanner and verifier run through the managed portion of the Q2 script. The scanner exits 0 with both known test-caller positive controls and zero production hits. The production verifier exits 0 for Release and 1 for Debug with its expected policy diagnostic. The production-build controls generate local scratch artifacts only. [Preparation checks](raw/preparation-tests.log) · [Final preparation validator](raw/preparation-final.log) · [51 existing production controls](raw/production-controls.json) · [Release policy](raw/verifier-release.log) · [Debug policy](raw/verifier-debug.log)

The checksum manifest covers the sibling report and all evidence files except the manifest. It hashes exact staged Git bytes. A later Windows checkout may convert the line endings of textual files. In that case verify each Git blob with `git cat-file blob <Q2-commit>:<repo-relative-path>` and SHA-256. The findings fixtures are included with their exact synthetic bytes. [Manifest](SHA256SUMS.txt)
