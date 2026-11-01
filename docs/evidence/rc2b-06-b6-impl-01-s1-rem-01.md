# GAGAMBA-RC2B-06-B6-IMPL-01-S1-REM-01

Date: 2026-11-01 (Asia/Manila)
Remediation slice: focused production/test build separation
Baseline source SHA: `1cbe4dfe9c4d19aba8708079b2f617b2ddafcbc0`
Final source SHA: implementation commit identified in section A; verification receipt follows the implementation commit
Branch: canonical `main`
Host: Microsoft Windows 11 Home Single Language, 10.0.26200, x64
SDK: .NET 10.0.401; MSBuild 18.9.11.42413; runtime 10.0.12
Native prerequisites: Windows deterministic environment; Ubuntu WSL2 stopped, docker-desktop WSL2 running (inventory only)
Qualification scope: managed build configuration, assembly metadata/IL, portable regression tests
Explicit exclusions: privileged/native qualification, installed artifact trust, S2+, wire changes, publication, production activation

## A. Source baseline and custody

Canonical repository: `https://github.com/jenolaszlo-sketch/gagamba.git`, checkout `C:/Users/Laszlos/source/repos/Gagamba`.
Starting commit `1cbe4dfe9c4d19aba8708079b2f617b2ddafcbc0`, tree `cb178340c21a178914a4233c9e3ecc5a2d9b23ff`, parent `24ac359bdecd36384718f40de26220ea4a1aa329` (tree `3e0dded2898aa9aff5d105d318335dd9609c989d`). Remote `main` was independently queried and matched the starting S1 commit. No fetch, branch, linked worktree, reset, rebase, stash, clean or push was performed.

Tracked source was clean. The canonical checkout already contained unrelated **untracked** review files, tools, native spikes and old test results: see `artifacts/b6-s1-rem-01/initial-status.txt`. They were preserved, not staged or overwritten. Thus this was a clean tracked baseline, not an empty-untracked checkout. The initial worktree inventory contains **74 total entries including canonical main**; all paths and historical heads are retained. No worktree was created or removed.

Implementation/source binding and post-commit verification are recorded in the final evidence receipt below. `source-inputs.sha256` beside this report binds each changed build/probe input independently of documentation commits. No existing runtime `.cs` file or existing test assertion source was edited. The original S1 evidence remains unchanged at [S1 record](rc2b-06-b6-impl-01-s1-production-build-separation.md).

Q1 is external historical evidence at `C:/Users/Laszlos/source/repos/Solo/b6-impl-01-s1-q1-review`. All twelve required reports, the additional report files, probe source and actual `evidence/MANIFEST.md` were inspected. Root report/probe hashes are preserved in `q1-evidence.sha256`; none of those files were modified. Q1 is **S1 SUPPORTED WITH QUALIFICATIONS**; owner disposition is **FOCUSED S1 REMEDIATION REQUIRED**, not owner acceptance.

Accepted [specification decision](../decisions/rc2b-06-b6-spec-owner-acceptance.md), [wire decision](../decisions/rc2b-06-b6-wire-owner-acceptance.md), [design](../design/rc2b-06-b6-installed-artifact-trust.md) and [qualification plan](../plans/rc2b-06-b6-qualification.md) remain authoritative. Installation trust/native T24 mutation and installed-record enforcement are not implemented here.

Raw evidence is retained locally under ignored `artifacts/b6-s1-rem-01/`; durable summaries and its SHA-256/length manifest are beside this report. Large binaries, TRX and multi-megabyte IL/build logs are not committed. The custodian is this canonical checkout and its local artifacts directory; this is producer evidence, not a recreated independent review. The final Git IDs are supplied in the handoff, since a document cannot embed its own containing commit/tree ID without changing it.

## B. Original Q1 findings

| Finding | Original severity | Reviewer evidence | Proposed producer disposition |
| --- | --- | --- | --- |
| FINDING-001 | MAJOR | `FINDINGS.md`, `SOURCE-BINDING.md`, `BUILD-ISOLATION.md`: Release solution test compilation references removed hooks | Corrected locally; independent Q2 closure required |
| FINDING-002 | MAJOR | `FINDINGS.md`, `BUILD-ISOLATION.md`, `BINARY-INSPECTION.md`: global empty DefineConstants yields hook-enabled Release | Corrected locally; independent Q2 closure required |

Q1 reported CI run `41311797002` failure and 1,245 passed / 289 skipped. Its preserved raw `evidence/` contains a manifest rather than all underlying CI/test logs. The preserved `InspectHooks` implementation directly enumerates metadata; some additional IL/string sampling claims are report-level observations, not implemented by that probe. This remediation independently reproduces failures and retains its own command output and stronger IL inspection, without rewriting Q1's provenance.

## C. Independently reproduced failing baseline

Before editing:

```powershell
dotnet build Gagamba.sln -c Release --nologo
dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release '-p:DefineConstants=' --artifacts-path artifacts/b6-s1-rem-01/before-empty --nologo
```

The first command exited 1: **25 errors / 0 warnings**, exclusively in `Gagamba.Execution.Linux.M1.Tests`. `before-solution-release.log` retains every project, file, line and diagnostic. Missing `CopyTestHooks` caused CS0246 in BindingTests, CopyInTests and NativeCopyInTests. IntegratedBarrierTests had CS7036/CS1729 because conditional fixture signatures no longer matched callers; NonRootQualificationTests had CS1744/CS1503 positional/named argument mismatches. Those are distinct API/reference and conditional-signature consequences, not just a missing using statement.

The second command exited **0**, writing a Release-labeled M1 DLL in a previously unused artifacts root. The independent new verifier subsequently rejects that actual retained binary: **4/4 hook types and 3/3 hook properties present**, even though AssemblyConfiguration is Release. See `before-empty.log` and `before-empty-inspection.log`. The unsafe baseline binary remains local for independent negative-control inspection.

## D. Root cause and target ordering

S1 appended `GAGAMBA_PRODUCTION` in Directory.Build.props for every Release project. The ordinary M1 test project's ProjectReference therefore resolved the production M1 variant, while its source still needed qualification hooks. Test-local conditional sections were also disabled while other calls retained the instrumented signature. D6.4 had its own distinct assembly/carrier/path variant, but inherited Release when invoked with Release; that specialized variant must remain instrumented independently of the test runner's optimization configuration.

An MSBuild global `DefineConstants` prevents the props assignment from appending the production symbol. `Microsoft.CSharp.Core.targets` supplies **the effective `$(DefineConstants)` directly to Csc**. A label or early assignment is consequently insufficient.

The late invariant target runs immediately before CoreCompile (after `_BeforeVBCSCoreCompile` dependencies), including incremental builds. `compiler-diagnostic.log` records the target sequence and the actual Csc DefineConstants parameter. An additional early missing-symbol rejection runs before PrepareForBuild, preventing invalid Release graphs from resolving differently scoped dependency instances first. This was added after an adversarial solution probe hit CS2012 while two global-property instances targeted the shared Execution output (the diagnostic mentioned Defender). That unrelated compilation failure was **not** counted as successful symbol enforcement. `early-refusal.log` demonstrates the final intended GAGAMBA002 diagnostic for all three trusted projects.

## E. Implementation

| Files | Change and rationale |
| --- | --- |
| `Directory.Build.props` | Scope the Release production symbol to M1, owner and recovery; test source no longer receives a production symbol merely because its runner is Release |
| `Directory.Build.targets` | Early missing-symbol refusal, effective pre-Csc configuration invariants, post-copy verification for each trusted candidate closure, input and final-output publish verification |
| M1.Tests csproj | Explicit Debug references for M1 and the owner process used by RecoveryHarness; standard bin/Debug and obj/Debug isolation |
| D6_4.Tests csproj | Retain TestFixture assembly name, carrier and isolated restore/bin/obj roots; explicitly use Debug in reference and restore properties |
| `eng/VerifyProductionAssembly/` | Standalone PE metadata/IL verifier, independent of repository imports and SourceLink, executable directly by Q2 |
| `eng/test-production-build.ps1` | Reproducible build-order, symbol, project-reference, stale-output and publish probes with exact argument arrays and logs |
| `eng/test-production-regressions.ps1` | Additional compilation of unchanged hook-independent safety tests against actual Release M1; full original instrumented suite is retained |
| Evidence and current pointers | This report, compact evidence manifests and current status/queue/handoff addenda |

No solution project was removed and no solution configuration was hidden. D6.4 was already outside Gagamba.sln and remains separately exercised. No original test source/assertion was deleted, edited, weakened or given a new skip.

## F. Supported and refused build contract

| Entry/configuration | Contract |
| --- | --- |
| Trusted M1/owner/recovery Release | Must effectively define GAGAMBA_PRODUCTION; no D6.4 carrier, fixture symbol or fixture identity; actual M1 binary must pass independent inspection |
| Direct M1 Debug or Test | Instrumented; production symbol forbidden; normal configuration-separated outputs |
| M1.Tests Debug or Release | Complete original assertions; M1 and owner explicitly use Debug; runner output remains its requested configuration |
| D6_4.Tests Debug or Release | Complete three original assertions; M1 uses Debug plus existing TestFixture identity and isolated d6-4-variant roots |
| D6.4 global ArtifactsPath output remapping or unisolated fixture paths | Explicit GAGAMBA008 refusal; use its dedicated existing reference/restore configuration |
| Release empty/TRACE-only/fixture-only constants | GAGAMBA002 refusal before reference resolution and again at the compiler boundary |
| Release production plus fixture symbol/carrier | GAGAMBA004 refusal |
| Debug/Test forced production symbol | GAGAMBA003 refusal |
| Fixture symbol without carrier; invalid carrier/identity | GAGAMBA005/006/007 as applicable; missing isolated roots GAGAMBA008 |
| Other trusted-component Configuration values | GAGAMBA001 refusal |
| Publish and publish without rebuilding | Current configuration checked; actual input M1 and final published M1 inspected; unsafe/missing binary fails |

Normal Release and Debug have the same canonical simple assembly name, but **different Configuration, MVID, binary hashes, intermediate paths and binary paths**. D6.4 also retains its distinct simple name. No strong-name identity or installed-provider selection guarantee is claimed. Shared Gagamba.Execution is configuration-separated and has no conditional compilation directives; its existing carrier/property removal remains unchanged.

The supported boundary is these repository build entry points and declared configurations. It does not resist modification of build scripts, verifier/compiler binaries, arbitrary custom targets/imports, or disabling repository imports. No general build provenance or installation attestation is claimed.

## G. Binary qualification and closure

Run independently, without executing the target assembly:

```powershell
dotnet run --project eng/VerifyProductionAssembly -c Release -- --production <M1.dll> --dump
dotnet run --project eng/VerifyProductionAssembly -c Release -- --instrumented <M1.dll> --dump
```

The tool checks **the accepted seven**, preserving the original count: Int01QualificationFault; Int01Qualification; OwnerSafetyMonitor.QualificationNativeStop; BubblewrapAdapter.QualificationBeforeCaptureStore; CopyTestHooks; BarrierDeathException; JournalFileStorage.QualificationBeforeRename. CopyTestHooks and BarrierDeathException are actually namespace-level types in SafeCopyIn.cs, despite shorthand nested notation in parts of the design and old review probe; the new verifier uses their actual metadata names.

Production: **0/4 hook types, 0/3 hook properties**, all **12/12** expected safety types present. All method bodies are decoded; metadata names, fields/accessors, references, generated hook names, IL token references and selected canonical fault strings are inspected. Full production and instrumented type/IL dumps are retained. The verifier's README states that TypeSpec/generic signature operands are opaque tokens, with referenced names covered through metadata tables; this is a bounded inspection, not a general IL proof or a cryptographic provenance verifier.

Instrumented Debug and Test retain all seven; D6.4 retains them plus its specialized fixture identity. A hook-enabled Debug DLL relabeled by path as Release is rejected. The unsafe pre-fix Release DLL is rejected even with the correct AssemblyConfiguration label. Metadata absence removes the seven reflection/environ-switch activation surfaces; no runtime switch was introduced to resurrect them. Instrumented hook behavior remains exercised by the unchanged copy/binding and recovery tests.

FakeDomainClock and RelativeTestClock reside in test source, absent from production M1. **Clock qualification preserved explicitly:** Reconciler's internal IDomainClock constructor still exists in Release, as at S1; production call sites use the sealed StopwatchDomainClock overload. The old Q1 inventory's wording "sealed in production" should not be read as metadata removal of that overload. This remediation neither removes it nor claims universal absence of every injection interface; Q2 should examine this inherited design/implementation qualification separately from the exact seven exclusions. All 61 B5 clock/deadline/authority tests execute against actual Release M1 in the supplementary suite.

The legitimate production FixtureConstructorMarker sentinel remains: it is safety code that refuses unexpected fixture output, not the fixture itself. Positive-control inspection caught and corrected an overly broad initial string ban before acceptance.

`binary-inventory.json` records full SHA-256, size and path for normal Release M1, owner, recovery, Execution, instrumented M1 and D6.4, plus copied production closures. Each copied M1 must match its production source in a coherent build. Production closure contains no Tests/TestFixture/B2Controller/inspection-tool assembly. System.IO.Hashing remains present through SourceLink in owner/recovery output, as already qualified by Q1; no dependency redesign was attempted. Different historical producer/Q1 hashes and different isolated output roots are not claimed byte-reproducible across environments. Stability is checked within unchanged build roots.

## H. Regression preservation

Framework: .NET 10, xUnit v3 3.2.0, Microsoft.NET.Test.Sdk 18.0.1, VS runner 3.1.4. Exact commands/logs and TRX counters/skip reasons are in `test-summary.json` and the raw-evidence manifest. Commands use `dotnet test tests/<project>/<project>.csproj -c Debug --nologo --logger trx --results-directory <recorded path>`; the two hook-dependent projects also run with `-c Release`.

| Suite | Passed | Failed | Skipped | Discovered |
| --- | ---: | ---: | ---: | ---: |
| M1 Debug | 1050 | 0 | 269 | 1319 |
| Execution Debug | 68 | 0 | 0 | 68 |
| Conformance Debug | 6 | 0 | 0 | 6 |
| Runtime Debug | 15 | 0 | 0 | 15 |
| Linux Debug | 26 | 0 | 16 | 42 |
| macOS Debug | 47 | 0 | 4 | 51 |
| Windows Debug | 30 | 0 | 0 | 30 |
| D6.4 Debug | 3 | 0 | 0 | 3 |
| **Q1-comparable Debug total** | **1245** | **0** | **289** | **1534** |
| M1 Release runner / Debug M1 | 1050 | 0 | 269 | 1319 |
| D6.4 Release runner / Debug TestFixture | 3 | 0 | 0 | 3 |
| Additional actual Release M1 candidate regression | 1019 | 0 | 199 | 1218 |

The additional production compilation reuses the original friend test assembly name in an isolated project/output path; it is not deployed and does not replace the main suite. It explicitly omits five hook-dependent files: Preparation/{BindingTests,CopyInTests,NativeCopyInTests}.cs and Recovery/{IntegratedBarrierTests,NonRootQualificationTests}.cs. This accounts for **101 cases (31 portable passes, 70 environmental skips)** relative to the full instrumented suite, with **zero unexpected added/changed outcomes**. Full per-test accounting is retained in `production-supplement-instrumented-only-tests.json`. All those assertions still execute or report their original environmental skip in the full suites. No original test is excluded from M1.Tests Release or Debug.

All 289 Debug skips retain Q1's totals: M1 269 are named Linux/openat2/cgroup/pidfd/socket/mount/lifetime prerequisites; Linux 16 comprise 14 Linux-native and 2 delegated-cgroup cases; macOS 4 comprise 3 Unix helper-shim and 1 Unix-permissions cases. Exact disjoint M1 skip reasons/counts are recorded from TRX, not guessed categories. Native skips are **not passes**. Producer S1's 366/160 and Q1's 1245/289 remain separate historical observations. This run reproduces Q1's broader inventory.

B1–B5 preservation: no existing runtime source changed; original release arbitration/admission, HELD, durable journals, ownership, controller-loss, cleanup/ambiguity and clock/deadline assertions remain. All 61 B5 cases pass against both instrumented and actual Release M1; D6.4 three specialized assertions pass. This is portable/managed evidence only: native stop, real retained cgroups and native capture/cleanup remain NOT ATTEMPTED here.

## I. Negative tests, order and reproducibility

Run `pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory <fresh directory>`. The final matrix's `results.json` stores each exact argument vector, expected diagnostic, exit and pass condition. See the final receipt for its count/result.

Coverage includes fresh Release; fresh Debug; Debug→Release; Release→Debug; repeated Release/Debug; direct Test; direct owner/recovery; parallel full Release solution; missing/empty/test-only/combined symbols; Release fixture carrier; Debug forced production; malformed fixture isolation; a fake helper-validation global; unknown config; CLI owner/recovery/solution overrides; explicit ProjectReference DefineConstants override; cached output with missing symbols; M1/owner/recovery publish and no-build invalid-property publish; stale Debug-as-Release binary and stale no-build publish; owner build with project references disabled and a stale instrumented transitive source; rebuilding valid Release after rejection.

One valid explicit `DefineConstants=GAGAMBA_PRODUCTION` direct Release build also passes. Concurrent separate-root Release and Debug builds both pass; final metadata checks confirm their respective variants. Same-output-root overlapping builds are not claimed safe. D6.4's special output remapping restriction is explicit.

Earlier development attempts are retained, not counted as successful final checks: an invalid XML comment, an MSBuild condition-parentheses warning, an unresolvable bare dotnet host under cmd (fixed using the SDK host's absolute path), a duplicate CLI Configuration probe that actually built Release (replaced with `-c Unclassified`), a stale owner-destination probe that correctly repaired itself (strengthened to corrupt the transitive source too), and a CS2012/Defender interruption before solution symbol validation (addressed by early refusal). None is reconstructed independent evidence. The final run must satisfy the intended diagnostics and actual-binary conditions.

## J. T24 requalification matrix

| Obligation | Demonstrated result | Qualification |
| --- | --- | --- |
| T24-A | Seven hooks absent from actual production metadata/IL; safety types retained | Bounded exact inventory; inherited internal clock overload disclosed above |
| T24-B | Seven excluded paths cannot be enabled by environment or reflection; no resurrection switch | No claim that arbitrary reflection over every internal API is a security boundary |
| T24-C | Candidate output has no qualification-only artifact closure | Installed manifest/role enforcement and native deps remain S2+ |
| T24-D | Debug/Test/D6.4 contain hooks; complete original test capability retained | Native hook execution prerequisites remain skipped on Windows |
| T24-E | Configuration, path, MVID and hash separation; D6.4 distinct name/root; order probes pass | No installed-provider/generation selection or cross-root byte-reproducibility claim |
| T24-F | 1245/0/289 full Debug, 1050/0/269 main Release runner, 3/0/0 D6.4, 1019/0/199 actual Release M1 | Environmental cases are unqualified; supplementary exclusions explicitly accounted |
| T24-G | Invalid effective Release symbols fail; actual output checked after builds and publishes, including stale negative controls | Supported build mechanism only; **no root-owned qualification-record verifier or forged installed-record refusal** |

The native T24-N separate-generation installation mutation is NOT ATTEMPTED and remains S2/later. This matrix does not declare full installed-artifact T24 acceptance.

## K. Q1 disposition and producer review

FINDING-001 and FINDING-002 are proposed locally remediated, subject to final receipt checks and independent Q2. This producer does **not** close Q1 independently or accept S1 for the owner. The historical verdict/disposition are unchanged.

Luna assisted with read-only Q1 evidence/accepted-contract inspection, staged verifier implementation, and producer code review. Accepted findings: inspect actual namespace-level hook types, preserve the legitimate marker guard, verify owner/recovery output with disabled reference builds, inspect final publish output, and remove an unnecessary supplementary test symbol. No independent Q2 certification was delegated or claimed. Model assistance is process evidence, not correctness proof.

## L. Remaining qualifications and boundaries

- Native execution, privileged operations, root-owned installation and qualification-record mutation: **NOT ATTEMPTED**. WSL inventory did not start Ubuntu, change delegation or configure the host.
- S2 dependency closure, System.IO.Hashing disposition, native dlopen/runtime prerequisites, root-owned receipts/records, active generations, handshake and manifest trust remain future work.
- The inherited internal clock constructor qualification is explicit in G/J; production call-site selection remains unchanged.
- D6.4's pre-existing explicit variant restore requirement is retained. Its ordinary dedicated project tests passed; arbitrary artifacts-output remapping is deliberately refused.
- Production candidate binaries are local managed build outputs, not root-owned installed generations. No native Linux/macOS, arm64, clean-package consumer, activation or installation acceptance is inferred.
- No B6-S2, new wire messages, descriptor versions, native dependency work, package publishing or provider activation. RestrictedExecutionRuntime.Create remains production-unavailable and unmodified.
- No privileged OS command, package upgrade, WSL/cgroup/kernel/namespace reconfiguration, host cleanup, branch/worktree creation, history rewrite or push occurred.
- Build logs/compilers can leave rejected local outputs; a failed build is not an accepted candidate. Every successful trusted Release output and normal published M1 copy passes the actual binary gate. Arbitrary tampering with tooling/build scripts is outside the declared boundary.

## M. Independent Q2 falsification instructions

Commission ID: **GAGAMBA-RC2B-06-B6-IMPL-01-S1-Q2**. Construct probes independently; do not treat producer tests as certification.

1. Bind canonical source, parent, exact implementation/tree and doc-only receipt; verify all 74 worktree paths and untouched Q1 evidence hashes.
2. Build the complete Release solution from new outputs; verify neither project removal nor assertion suppression explains success.
3. Recreate empty/TRACE-only/fixture constants, forced Debug production, contradictory carriers, unknown configs and ProjectReference overrides; require the intended explicit refusal rather than an unrelated failure.
4. Inspect evaluated constants and compiler target ordering; verify checks run with incremental outputs and through direct/solution/owner/recovery/publish entry points.
5. Independently enumerate the **actual** four hook types and three properties, accessors/fields, closures, metadata and IL references/fault branches; reject the retained unsafe baseline and renamed Debug controls.
6. Confirm Debug/Test/D6.4 hook presence and original meaningful assertion behavior, correct owner/reference configuration and restore/output isolation. Challenge D6.4 unsupported path combinations.
7. Independently rerun the Q1 eight-project inventory, reconcile TRX names/outcomes/skips, and inspect the 101-case explicit supplementary-production difference. Check actual Release M1 safety behavior.
8. Reorder and repeat builds, test separate-root concurrency, corrupt copied and transitive M1 outputs, disable project-reference builds and publish without rebuilding; verify correction or refusal before accepting output.
9. Verify owner/recovery candidate closure hashes and no test/tool assembly leakage; retain System.IO.Hashing's existing S2 qualification.
10. Examine the inherited internal clock overload qualification against accepted T24 design text; do not interpret Q1's sealed-composition wording as metadata exclusion.
11. Confirm T24-G claims only supported build identity and actual hook exclusion, with no forged installed-record enforcement claim.
12. Verify no B1–B5 runtime behavior change, S2 implementation, privileged/native qualification, production activation or unauthorized push.

## Final evidence receipt

Implementation commit, final verification identifiers, artifact hashes and selected disposition are recorded in the evidence-only follow-up after committing the bounded implementation. Until that receipt is present this document records the measured implementation work, not a completed disposition.
