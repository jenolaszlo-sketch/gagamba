# Reproduction and evidence map

Use canonical Gagamba with the exact candidate in the predecessor ancestry and initially clean tracked files. The qualification merge-free sequence is published E-4, authorized E-5 docs, exact candidate, producer evidence, then this evidence package. First run `python verify-integrity.py <external-receipt.json> [expected-local-head]` with read-only canonical Git and live origin access. The receipt verifies all SHA-256 entries at pinned revisions, local HEAD and working bytes, then checks exact candidate blobs, source boundary and historical untracked inventory. The candidate and producer commits are independently checked against the frozen allowlist. [Preflight](preflight.json)

The run used Windows x64, Git 2.55.0.windows.3, .NET SDK 10.0.401 and runtime 10.0.12, Python 3.14.8 and Windows PowerShell 5.1.26100.9818 for existing policy. Build restore used cached packages with `NuGetAudit=false` and `RestoreIgnoreFailedSources=true`. A scratch path outside canonical Gagamba held all qualification binaries, probe builds, alternate references and logs. [Environment](raw/environment.log) · [Build exits](raw/build-exact/build-exits.txt)

## Existing gates

Set `$repo` to canonical Gagamba, `$run` to a fresh writable absolute path and `$annex` to this evidence directory. Use the unchanged producer procedure from its verified documentation path. Reproduce its artifact builds, managed suite runner, E-4 supplemental runner, frozen E-5 sensitivity controls, policy/preparation workflow and compiled cross-check. Allow the prescribed 15-second VSTest connection deadline. Count execution only when the TRX or in-process result shows executed cases. [Pinned reproduction scripts and commands](../../evidence/rc2b-06-b6-alt-b-slice-e-05-custody-implementation-01-evidence/reproduction.md)

- Candidate build commands and exits are in [build-exact](raw/build-exact/build-exits.txt). The fourteen builds completed with exit 0. The [Debug](raw/results/debug/bindings.json) and [Release](raw/results/release/bindings.json) receipts bind the selected test, core, execution dependency and discovery copies.
- For xUnit XML and scanner JSONL stored with `.gz`, use standard gzip decompression. The uncompressed original XML hashes are in [execution-summary.json](raw/execution-summary.json). [Managed membership](raw/managed-membership.tsv) preserves display name, duplicate multiplicity, result, skip reason and focused membership. E-5 is included in broader M1. The two historical E-4 supplemental ledgers each include 1,944 exact cases and a separate disposable omission witness. [Debug supplement](raw/logs/supplement-debug.tsv.gz) · [Release supplement](raw/logs/supplement-release.tsv.gz)
- The 17 frozen E-5 controls in each configuration cover comparisons, premises, ordering, terminality, ownership, deadline and stop, and an injected bounded exception at snapshot publication. [Debug](raw/sensitivity-Debug.tsv) · [Release](raw/sensitivity-Release.tsv) · [Derivative source hashes](raw/sensitivity-variants.json)
- `eng/test-production-build.ps1` ran under Windows PowerShell 5.1 with policy outputs redirected. `python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v` and `eng/verify-preparation.ps1` finished at exit 0. The exact Release binary gave verifier PASS and the exact Debug binary refused as production. [51 policy rows](raw/policy-exact/results.json) · [Preparation regressions](raw/preparation-tests.log) · [Preparation validation](raw/preparation.log) · [Release verifier](raw/audit/verifier-release.log) · [Debug refusal](raw/audit/verifier-debug.log)

## Independent supplemental and mutation corpus

```powershell
& "$annex/Run-Independent.ps1" -Repo $repo -Scratch $run
python "$annex/run-mutations.py" $repo $run
```

`Run-Independent.ps1` reads the accepted E-3 grammar templates and builds the reviewer probe against the selected Debug and Release candidate DLLs. It derives new context/digest operands and independent expected values. The test harness uses separate expected objects and source bytes for all negative controls. The constructed E-2 chain uses deterministically serialized canonical bytes with independently selected content and normal SHA-256 hashing. Negative advances require exact refusal, null snapshot and a second, contextually valid attempt that still refuses terminally. The two contender test uses barriers, with one drop/consume ordering witness supplied all otherwise-due premises. [Probe source](probe/Program.cs) · [Composition and synthetic chain](probe/Composition.cs) · [Debug outcomes](raw/independent-debug.tsv) · [Release outcomes](raw/independent-release.tsv)

The mutation script generates one disposable compiled namespace per deviation and a dispatcher. For each configuration it proves a valid sequence still works, runs the same independently checked exact-core witness against the derivative and requires a detectable bad outcome. The gate-removal control uses a rendezvous between validation and commit so both contenders observe the predicate before committing. The verifier receipt lists each removed/added predicate, exact witness, source hashes and compilation/run identities. [20 source receipts](raw/independent-derivative-source.json) · [Debug paired outcomes](raw/independent-derivative-debug.tsv) · [Release paired outcomes](raw/independent-derivative-release.tsv)

## Independent assembly and P4 audit

```powershell
dotnet build "$annex/metadata/Scan.csproj" -c Release --artifacts-path "$run/independent-metadata" --nologo
Push-Location $repo
foreach ($cfg in 'Debug','Release') {
    $lower = $cfg.ToLowerInvariant()
    dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c $cfg `
        --artifacts-path "$run/independent-il-control/$lower" `
        "-p:CustomAfterMicrosoftCommonTargets=$annex/metadata/ReviewProbe.targets" --nologo -m:1
}
Pop-Location
python "$annex/audit-source.py" $repo $run
python "$annex/run-scan.py" $run
```

Run the documented producer cross-check first because the independent runner reconciles the two selected assembly inventories after running its own scanner. The positive scanner controls identify existing test-facing E-3/E-4 calls, ordinary P4 edges and authorized E-5 test callers. The independent disposable negative is a single synthetic reference whose only implementation calls the model with missing operands. It is compiled and scanned without executing the control. [Scanner source](metadata/Program.cs) · [Scanner findings and hashes](raw/independent-scan/summary.json)

Finally on the historical inputs run `python collect-execution.py <repo> <run> <new-evidence-output>`. It compares actual counters, TRX, membership and accepted baselines, records raw artifact results and extracts existing P4 cases from both broad runs. Verify the SHA-256 manifest, remove the entire disposable run directory and externally record post-commit repository and remote identity. [Integrity checker](verify-integrity.py) · [Execution collector](collect-execution.py) · [Cleanup receipt](raw/cleanup.json)
