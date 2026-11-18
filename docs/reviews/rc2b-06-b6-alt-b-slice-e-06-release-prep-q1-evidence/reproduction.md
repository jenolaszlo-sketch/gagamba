# Q1 reproduction and evidence map

Candidate `4202a2f7a99b85cfe1caf70d6a583f58e6551283`, tree `f987f32ced441263167649307003536dd493dd66`. Work with a fresh disposable root outside canonical Gagamba. `$repo` names the canonical repository, `$src` the extracted exact candidate tree, `$run` a writable build/results directory, and `$annex` this Q1 evidence directory. Historical build tooling is invoked from its pinned paths. Reviewer code in `procedure` carries a trailing `.txt` to keep it as evidence. Copy to scratch and remove that suffix for compilation or PowerShell execution.

## Identity, archive, and runner prerequisites

1. Read [reviewer.md](reviewer.md). Run `python "$annex/procedure/q1-preflight.py.txt" $repo "$run/preflight.json"` with read-only canonical and GitHub access. Initial mode expects the starting producer evidence commit at local main. Use `--post` at the additive Q1 evidence commit. The receipt covers the exact nine-commit chain, all governing manifests, both source inventories, historical originals of the three authorized status notes, and 44 historical untracked paths and hashes. Compare the checkout's path and byte checks with [raw/preflight.json.gz](raw/preflight.json.gz).
2. Export the exact candidate with `git --git-dir="$repo/.git" archive --format=tar --output="$run/candidate.tar" 4202a2f7a99b85cfe1caf70d6a583f58e6551283`. Extract with `tar -xf "$run/candidate.tar" -C $src`. The archive deliberately has no Git working-tree metadata. Keep qualification logs and builds external. All historical source object checks use canonical Git.
3. Use .NET SDK 10.0.401, .NET runtime 10.0.12, Python 3.14.8, Git 2.55.0.windows.3 and Windows PowerShell 5.1 for policy. Use the historical restore settings `NuGetAudit=false` and `RestoreIgnoreFailedSources=true`. Archive-based builds may report source-control discovery warnings. Record the build outputs by their own raw hashes.

## Managed suite and historical control replay

```powershell
$e5build = "$src/docs/evidence/rc2b-06-b6-alt-b-slice-e-05-custody-implementation-01-evidence/procedure"
$e5review = "$src/docs/reviews/rc2b-06-b6-alt-b-slice-e-05-custody-q1-evidence"
$f02 = "$repo/docs/evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence"
& "$e5build/Build-Exact.ps1" -Repository $src -Scratch $run
Copy-Item "$f02/procedure/run-managed.py.txt" "$run/run-managed.py"
foreach ($cfg in 'Debug','Release') {
  python "$run/run-managed.py" $src $run $cfg
}
& "$e5build/Run-E4-Supplement.ps1" -Repository $src -Scratch $run
Copy-Item "$e5build/SensitivityProgram.txt" "$run/SensitivityProgram.cs"
Copy-Item "$e5build/SensitivityProject.txt" "$run/Sensitivity.csproj"
python "$e5build/Run-Sensitivity.py" $src $run
New-Item -ItemType Directory "$run/e5-replay"
Copy-Item "$e5review/Run-Independent.ps1","$e5review/probe" "$run/e5-replay" -Recurse
& "$run/e5-replay/Run-Independent.ps1" -Repo $src -Scratch $run
python "$e5review/run-mutations.py" $src $run
Copy-Item "$f02/procedure/run-e6-mutations.py.txt" "$run/run-e6-mutations.py"
python "$run/run-e6-mutations.py" $src $run
```

Check each exit and the exact selections. The 14 builds, 10 VSTest commands and corresponding xUnit fallback invocations are in the retained command receipts. Each TRX reports zero executed. Each XML reports the real run, including test name and method, status, skip reason and case multiplicity. Focused Release loads the Release core and exact Release discovery copy. The broad Release fixture reference uses Debug under the frozen layout. The accepted producer XMLs were compared on type, method, display name, result and skip reason with duplicate multiplicities preserved. The 331 skips per configuration were also matched against the E-5 independent baseline. E-4 supplemental runs include a separate 1,945th comparison-omission witness. [Summary](raw/summary.json) · [Debug bindings](raw/results/debug/bindings.json) · [Release bindings](raw/results/release/bindings.json) · [Debug skips](raw/native-skips-debug.json) · [Release skips](raw/native-skips-release.json) · [Original E-5 replay](../../evidence/rc2b-06-b6-alt-b-slice-e-05-custody-implementation-01-evidence/reproduction.md)

## Independent byte oracle, adversarial cases, and sensitivity

```powershell
python "$annex/procedure/oracle.py.txt" $src "$run/cases.json"
New-Item -ItemType Directory "$run/q1-probe"
Copy-Item "$annex/procedure/Program.cs.txt" "$run/q1-probe/Program.cs"
Copy-Item "$annex/procedure/Probe.csproj.txt" "$run/q1-probe/Probe.csproj"
foreach ($cfg in 'Debug','Release') {
  $lower = $cfg.ToLowerInvariant()
  $core = "$run/candidate-exact/bin/Gagamba.Execution.Linux.M1/$lower/Gagamba.Execution.Linux.M1.dll"
  dotnet build "$run/q1-probe/Probe.csproj" -c $cfg "-p:Q1Core=$core" --nologo -m:1
  dotnet "$run/q1-probe/bin/$cfg/net10.0/Gagamba.Execution.Linux.M1.Tests.dll" "$run/cases.json"
}
python "$annex/procedure/review-controls.py.txt" $src "$run/q1-controls" "$annex/procedure/Program.cs.txt" "$run/cases.json"
```

The oracle checks both frozen raw byte witnesses before generating seeded test inputs. The probe uses the existing M1 Tests friend assembly name in its own isolated directory and calls the compiled entry points through typed parameters. It never loads or changes the product sources. Run the probe with an optional exact case ID as the second argument to reproduce a single witness. A control earns sensitivity only when a valid release projection still produces material, the designated counterexample fails with the expected observation, and the mutated probe neither crashes nor reports an unrelated failure. Copy ablations use the IL Array.Clone witness to respect the contract's stable-input boundary. Output ownership controls use ordinary immutable access and between-call mutation. [Oracle cases](raw/independent-cases.json.gz) · [All raw vector octets](raw/vectors.json) · [Debug outcomes](raw/q1-probe-debug-final.jsonl.gz) · [Release outcomes](raw/q1-probe-release-final.jsonl.gz) · [Reviewer derivative receipts](raw/q1-controls/receipts.json) · [Frozen derivative receipts](raw/e6-mutation-source.json)

## F02 closure replay

Copy `run-naming.ps1.txt` to scratch as `run-naming.ps1`. Use `-Root` set to a disposable root whose `run` folder holds outputs, `-Repo $repo`, `-Label f01 -Revision 7d298ddd9d83e555a5c16b6c0cab8f36e46b8822`, then `-Label blocked -Revision 53ffe7a21408a7e9674cf50b7098a26887c070b5`. Run `dotnet <focused test DLL> -noColor -method '*ValidationReachabilityTests.ValidationMemberNamesDoNotImplyAuthority' -xml <output>` against the selected remediated Debug and Release copies. Copy `f02-reflection.ps1.txt` to scratch and invoke in six fresh PowerShell processes with `-Dll`, `-Stage f01|blocked|remediated`, `-Configuration debug|release` and `-Output` for each. For the historical byte witness, build a copy of the independent probe against each blocked DLL with its references to the amended property changed to `CapabilityHash`. Run only `R01.Prepare`, `R01.Compare`, `Mixed.Prepare`, `Mixed.Compare` and `R02.Compare`. The five cases' exact output rows matched the remediated probes in each configuration. [Remediated Debug naming result](raw/remediated-naming-debug.xml.gz) · [Remediated Release naming result](raw/remediated-naming-release.xml.gz) · [Blocked Debug reflection](raw/f02-reflection-blocked-debug.json) · [Blocked Release reflection](raw/f02-reflection-blocked-release.json)

## Policy, preparation, and compiled isolation

Run the unchanged `$src/eng/test-production-build.ps1` with Windows PowerShell 5.1 using `-EvidenceDirectory "$run/policy-confirmed"`. Set `UseArtifactsOutput=true` and `ArtifactsPath="$run/control-verifier-confirmed"`. The successful run used the exact archive with a fresh output root outside the task sandbox. Two earlier restricted runs stopped during `owner-release` restore. On canonical `$repo` with bytecode generation disabled (`PYTHONDONTWRITEBYTECODE=1`), run `python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v` and Windows PowerShell `eng/verify-preparation.ps1 -RepositoryRoot $repo`. The `VerifyProductionAssembly.dll --production` invocation against the exact separately built core returns Debug exit 1 and Release exit 0. [Policy outcomes](raw/policy-confirmed/results.json) · [Preparation tests](raw/preparation-tests.log.gz) · [Preparation](raw/preparation.log.gz) · [Debug verifier](raw/verifier-debug.log.gz) · [Release verifier](raw/verifier-release.log.gz)

Create `$run/d64-candidate-source` from the exact archive's `src tests eng Directory.Build.props Directory.Build.targets README.md .gitattributes`. Build the D6.4 test project in both configurations with `-p:EnableSourceLink=false --nologo -m:1`, using its ordinary relative artifact output layout. Generate and compile the historical scanner with the F02 [generator](../../evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/procedure/generate-scanner.py.txt) and the scanner build command in the [F02 reproduction](../../evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-f02-remediation-01-evidence/reproduction.md). Copy the F02 raw candidate receipt to `$run/verified-source-receipt.json`. Build the exact M1 project with the reviewer [synthetic edge](procedure/SyntheticEdge.cs.txt) included using [the supplied target](procedure/IndependentQ1Probe.targets.txt), editing that target's Include to the absolute path of the copied `IndependentQ1Edge.cs`. Pass it with `-p:CustomAfterMicrosoftCommonTargets=<target>` and send the outputs to `$run/e6-inbound-exact/<lowercase-configuration>`. Use only compile and scan for these edges.

Run `python "$annex/procedure/adapt-isolation-replay.py.txt" "$f02/procedure/run-isolation.py.txt" "$run/q1-isolation-replay.py"`, then `python "$run/q1-isolation-replay.py" $repo $run`. Compile [the independent scanner](procedure/MetadataScan.cs.txt) as `$run/q1-metadata/Program.cs` with [MetadataScan.csproj](procedure/MetadataScan.csproj.txt), using `dotnet build -c Release --nologo -m:1` and standard output layout. Run `python "$annex/procedure/run-metadata.py.txt" $run`. The two scan inventories retain assembly path, role, raw hash, identity, module ID, and checked edge results. [Isolation and 29 dispositions](p4.md) · [Independent inventory summary](raw/q1-metadata/summary.json)

## Artifact integrity

Unpack `.gz` members with standard gzip. They retain original output bytes. `procedure/collect.py.txt` reconciles case and policy membership against Gagamba's frozen producer and inherited receipts. `SHA256SUMS.txt` binds this report and all Q1 evidence and omits the manifest itself. The final repository receipt is produced after commit by the same read-only preflight script with `--post`. The disposable source archives and build directories are removed after evidence collection.
