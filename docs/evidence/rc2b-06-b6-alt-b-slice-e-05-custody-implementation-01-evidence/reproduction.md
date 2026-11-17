# Producer procedure and independent qualification handoff

Use the canonical Gagamba checkout containing implementation commit `28895d55de98eea4bc402373b653f7216e092843` and tree `e314f2e8a1259e2263c8494bb0c8ca9f7eb27016`. If HEAD includes this evidence commit, verify that the intervening delta is only the producer report and annex. Verify the six candidate blobs and SHA-256 values and independently pin the original authorization, contract and requirements. Use a fresh reviewer uninvolved in implementation or producer verification. [Candidate receipt](raw/candidate.json) · [Authorization bindings](../../decisions/rc2b-06-b6-alt-b-slice-e-05-entry-auth-01-evidence/bindings-and-allowlist.md)

## Producer procedure

The host was Windows x64, SDK 10.0.401 and runtime 10.0.12. Policy used Windows PowerShell 5.1.26100.9818. Artifact build and policy runners used cached packages and `NuGetAudit=false`, so advisory coverage and PowerShell 7 policy qualification remain outside the claim. The initial test build hit the xUnit cancellation analyzer and was corrected before the candidate commit. Preparation checkout tests initially hit the restricted Git worktree context and passed on rerun with canonical read access. The final build and required policy probes ran from the committed candidate.

Set `$repo` to the canonical checkout, `$run` to a fresh writable external scratch directory, and `$procedure` to this annex's procedure directory. Stage the tooling into scratch as shown. The scripts place build outputs, results and disposable mutations outside canonical source. Keep ordinary Debug and Release build identities and the inherited Debug fixture reference.

```powershell
Copy-Item -Path "$procedure/*" -Destination $run
New-Item -ItemType Directory -Force "$run/metadata-tool" | Out-Null
Copy-Item "$procedure/MetadataProgram.txt" "$run/metadata-tool/Program.cs"
Copy-Item "$procedure/MetadataProject.txt" "$run/metadata-tool/Audit.csproj"
Copy-Item "$procedure/SensitivityProgram.txt" "$run/SensitivityProgram.cs"
Copy-Item "$procedure/SensitivityProject.txt" "$run/Sensitivity.csproj"
Copy-Item "$procedure/IsolationReference.txt" "$run/IsolationReference.cs"
Copy-Item "$procedure/IsolationProbe.txt" "$run/IsolationProbe.targets"
Copy-Item "$procedure/../raw/candidate.json" "$run/verified-source-receipt.json"

& "$run/Build-Exact.ps1" -Repository $repo -Scratch $run
python "$run/Run-Managed-Exact.py" $repo $run Debug
python "$run/Run-Managed-Exact.py" $repo $run Release
& "$run/Run-E4-Supplement.ps1" -Repository $repo -Scratch $run
python "$run/Run-Sensitivity.py" $repo $run

Set-Location -LiteralPath $repo
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier-exact"
$env:NuGetAudit = 'false'
$env:RestoreIgnoreFailedSources = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
powershell -NoProfile -ExecutionPolicy Bypass -File eng/test-production-build.ps1 -EvidenceDirectory "$run/policy-exact"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
powershell -NoProfile -ExecutionPolicy Bypass -File eng/verify-preparation.ps1 -RepositoryRoot $repo
Remove-Item Env:UseArtifactsOutput
Remove-Item Env:ArtifactsPath

python "$run/Run-Isolation.py" $repo $run
python "$run/Audit-P4.py" $repo $run
python "$run/Summarize.py" $repo $run
```

Check exits at each step. The isolation script expects direct Debug-as-production exit 1 and exit 1 on both isolated inbound-call controls. The producer's eight VSTest attempts ended at the inherited 15-second connection timeout with zero executed cases. The exact counts and membership come from the subsequent xUnit v3 in-process 3.2.0+9e422989c1 runs. The logs include actual runner commands, exit codes, selected DLLs and test counts. Historical display-name truncation produces duplicate visible names in one theory, so the comparison preserves multiplicity. [Testhost commands and outputs](raw/runner-commands.log) · [Selected assembly bindings](raw/test-binary-bindings.json) · [Build command and exit ledger](raw/build-commands.log) · [Build-policy probes](raw/production-controls.json) · [Preparation regressions](raw/preparation-tests.log) · [Preparation validation](raw/preparation.log)

The E-4 supplemental output is byte-identical from Debug and Release. It contains 1,944 cases on the candidate and one historical-format disposable comparison-omission control. E-5 uses separately hashed synthetic derivatives for 16 paired behavioral controls and one bounded managed exception control in each configuration. Generating these copies does not change canonical source. The companion verification receipts record both component hashes and actual loaded core hashes. [E-4 supplemental cases](raw/e4-supplement.tsv) · [Supplement invocations](raw/e4-probe.log) · [E-5 sensitivity invocations](raw/sensitivity-build.log) · [Derivative identities](raw/sensitivity-variants.json) · [Isolation invocations](raw/audit-commands.log)

Disposable build and sensitivity paths were removed after collecting receipts. The eight timed-out testhosts had exited. Candidate source, exact build artifacts and recorded results were preserved. The committed text ledgers use LF and preserve empty TSV fields. Raw output hashes refer to the original scratch files. [Cleanup receipt](raw/cleanup.json)

## Independent qualification

Establish independence and exact source/assembly identities before running cases. The scripts provide producer reproduction, and the reviewer independently derives operands, expected results, transition and refusal oracles, hash-consistent contradictory chains, omitted-predicate witnesses and a forbidden incoming reference. Bind actual selected Debug/Release core, test, discovery, copied and applicable publish artifacts. Record runner limits, exact case membership, failures, skips, all policy outcomes, analysis scope and cleanup. Repeat the G01–G08 gates and reconstruct the exact P4 graph, 29-row disposition and applicable effects/refusals. Preserve E5-R22/E5-R23 and every carried qualification. The independent finding precedes a separate owner decision. [Frozen verification gates](../../decisions/rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/qualification.md) · [Producer carry-forward](carry-forward.md)
