# Gagamba Linux M1 — WP05 D6.5 Fixture Isolation and Source-Equivalence Corrections

- Author: **Luna**
- Status: **PROPOSED DOCUMENTATION AMENDMENT — READY FOR FOCUSED GLM RE-REVIEW**
- Approval: **C1–C4 PROPOSED - NOT APPROVED; C3 POLICY PENDING HUMAN**
- Implementation: **NOT AUTHORIZED**

## 1. Git and evidence binding

- Repository: C:\Users\Laszlos\source\repos\Gagamba; origin: https://github.com/jenolaszlo-sketch/gagamba.git.
- D6.5 branch: design/wp05-d6-5-fixture-isolation-equivalence; worktree: C:\Users\Laszlos\source\repos\Gagamba-d6-5.
- Exact D6.5 base and intended immediate parent: D6.4 preservation 1fedce7199c1a3910ee5b1e0816670dfb7f470e9.
- D6.4 producer: 08938e96c69cfd573a284589c986f53f041c5803 on design/wp05-d6-4-r1-fixture-clarification.
- GLM D6.4 independent review: 8b95c52abdebf8fd9a0235dd5a69082456e1984f on review/wp05-d6-4-glm-independent-fixture; authoritative review document docs/reviews/gagamba-linux-m1-wp05-d6-4-glm-independent-fixture-review.md; blob 632c311da26e511519c5fbb44cb86efe9b0bb6e1.
- D6.4 preservation: 1fedce7199c1a3910ee5b1e0816670dfb7f470e9 on preservation/wp05-d6-4-glm-independent-fixture-review, whose parent is the D6.4 producer. The review commit is a separate sibling child of the producer and is not merged into this base.
- Frozen R12 production: 38f6981b2a60daab2df37c92913a6760c1bef131; tree 6854675d6b37a49d5302d6104103bdbfbc268040; harness Q3 36a124d7dff98ecfaf956340767fe1768ad5b17f.

The preservation history and GLM review history remain separately attributable. This record describes proposed future checks; no test variant or binary was built for D6.5.

## 2. GLM findings

GLM disposition: WP05 D6.4 GLM INDEPENDENT REVIEW COMPLETE - OPTION B: FIXTURE TESTABILITY PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED.

GLM accepted the D6.4 injection boundary, fallback read, deterministic R1-T13–T15 expectations, aggregate and durable-progress results, and the seeded DomainRemoval basis for zero removal calls.

- **D6.4-R1 — Friend-assembly metadata is incomplete.** TransactionStateModel.cs unconditionally grants M1 internals to M1.Tests and M1.Owner, so the shared-source variant would inherit both grants. The regular production M1 assembly must keep those two grants; the variant must grant only D6_4.Tests. RestrictedManifest.cs also needs the M1.TestFixture and D6_4.Tests friends. The latter is required by GLM for the dedicated test assembly compiling the linked recovery support and internal contract use. The additional core friends are emitted metadata in the production Gagamba.Execution assembly; “compile-time-only” does not describe where they appear.
- **D6.4-R2 — Production/test source equivalence is not required concretely.** The variant must consume the identical production M1 source inventory by construction, with no copied source tree, bidirectional source-graph and content checks, bounded conditional compilation, and production artifact-purity checks. A test-built variant is not binary-identical evidence for production.

No other D6.4 finding is reopened. The accepted C3 mapping, fixture boundary, and R1-T13–T15 behavior remain unchanged.

## 3. Selected MSBuild configuration

Use the same src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj for the production assembly and the test variant. Do not create a copied or parallel production source project.

Static inspection at the D6.4 base found that the M1 project uses SDK default Compile items; its project file sets net10.0, nullable/implicit usings, IsPackable=false, and a ProjectReference to Gagamba.Execution. It does not set AssemblyName, DefineConstants, Compile includes/removals, or output paths. Directory.Build.props does not set those values, and no Directory.Build.targets exists. The Owner project references the regular M1 project by its explicit project path.

A static Release/net10.0 dotnet msbuild property and Compile-item evaluation compared the production project with the same project using the proposed distinct variant AssemblyName and isolated output properties. It returned 44 Compile paths in each evaluation, with identical full paths. The queried default DefineConstants value was TRACE;RELEASE. That path comparison did not add the fixture symbol and did not invoke compilation, tests, or binary inspection. A static scan found no existing preprocessor directives or D6.4 fixture-symbol uses in the M1 source.

The future D6_4.Tests project references the same M1 project with a variant configuration that sets AssemblyName to Gagamba.Execution.Linux.M1.TestFixture, appends only GAGAMBA_WP05_D6_4_TEST_FIXTURE to the production DefineConstants set, sets IsPackable=false, and gives BaseIntermediateOutputPath, MSBuildProjectExtensionsPath, and BaseOutputPath distinct variant directories. Configuration and TargetFramework remain equal to production. The symbol must be appended without replacing SDK/configuration constants.

Scope these global properties to the M1 project instance. The M1-to-Gagamba.Execution ProjectReference must remove AssemblyName, DefineConstants, BaseIntermediateOutputPath, MSBuildProjectExtensionsPath, BaseOutputPath, and IsPackable from the child property set. Otherwise the M1 variant properties could leak into the shared core project. Verify that Gagamba.Execution resolves to its normal assembly identity and output path. Owner continues to resolve the regular M1 project; D6_4.Tests references the variant, normal Gagamba.Execution, and test packages, and has no Owner reference.

| Build dimension | Production M1 | D6.4 test variant |
|---|---|---|
| Project and shared source | Existing M1 csproj and source files | The same M1 csproj and exact same production source files |
| Assembly identity | Gagamba.Execution.Linux.M1 | Gagamba.Execution.Linux.M1.TestFixture |
| Target framework/configuration | net10.0; same selected configuration | Identical to production |
| DefineConstants | Existing evaluated set; fixture symbol absent | Production set plus only GAGAMBA_WP05_D6_4_TEST_FIXTURE |
| Fixture marker and Reconciler seam | Absent; real bounded stop | Fixed marker and guarded substitution at the accepted stop-result boundary |
| M1 friend set | M1.Tests and M1.Owner | D6_4.Tests only |
| Output and intermediates | Existing production paths | Separate bin, obj, MSBuildProjectExtensionsPath, generated-file and package-asset paths |
| Packaging | Existing production policy; M1 is non-packable | IsPackable=false; not packed or published |
| Owner graph | Owner resolves regular M1 | Owner is excluded |

The only intended M1 source-level differences are guarded regions in Reconciler.cs (fixed marker, field, overload, and stop-result selection) and TransactionStateModel.cs (conditional friend list). RecoveryTestSupport.cs and the future acceptance cases are compiled in D6_4.Tests, not added to the M1 variant source inventory.

## 4. Friend-access arrangement

Use one conditional assembly-attribute block in the shared TransactionStateModel.cs source:

~~~csharp
#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.D6_4.Tests")]
#else
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(
    "Gagamba.Execution.Linux.M1.Owner")]
#endif
~~~

| Assembly | Required friend names | Why |
|---|---|---|
| Regular production Gagamba.Execution.Linux.M1 | Gagamba.Execution.Linux.M1.Tests; Gagamba.Execution.Linux.M1.Owner | Preserve the exact existing production grants. Do not add D6_4.Tests. |
| Test-variant Gagamba.Execution.Linux.M1.TestFixture | Gagamba.Execution.Linux.M1.D6_4.Tests only | Lets the dedicated test assembly access the variant's internal Reconciler and ownership/journal contracts; excludes Owner and normal M1.Tests. |
| Shared Gagamba.Execution | Existing Gagamba.Execution.Linux.M1 and Gagamba.Execution.Linux.M1.Tests, plus Gagamba.Execution.Linux.M1.TestFixture and Gagamba.Execution.Linux.M1.D6_4.Tests | The variant needs core internal RestrictedManifest contracts. GLM requires the D6_4.Tests grant for the dedicated test assembly's linked recovery support/internal contract use. Both additions remain in production core metadata. |

The Gagamba.Execution friend attributes are emitted into that shared assembly's metadata and grant compile-time access by assembly name. They add no runtime methods or public API. The inspected project configuration has no strong-name signing. These names do not provide cryptographic identity or a runtime sandbox; a separately named assembly using one of the friend names can compile against Gagamba.Execution internals. The normal Owner/release graph must not include the test variant, and the regular M1 binary must not contain the fixture mechanism. These checks constrain the official build graph but do not erase the residual named-friend exposure.

## 5. Source-graph and conditional-compilation verification

The future verifier compares the actual production and variant builds, not only project-file text:

1. Start from one clean source revision and use the same Configuration and TargetFramework. Record the commit, dirty state, evaluated AssemblyName, DefineConstants, output properties, and project-reference graph for each M1 project instance.
2. Capture evaluated Compile items using MSBuild evaluation and capture the actual Csc source list from a build-generated binlog after generated compile inputs have been added.
3. Normalize tracked sources to canonical Git-root-relative paths. Classify each input as shared M1 production source, explicitly allowlisted fixture/test input, or generated input. Hash each file's actual compiled bytes with SHA-256.
4. Let P be the production M1 shared-source set and V be the test variant's shared-production-source set. Require P=V in both directions and equal hashes for every shared path. Any missing, copied, substituted, or unclassified M1 input fails. In the selected design the M1 fixture-only source-file allowlist is empty; fixture code occupies guarded regions in the shared files listed in section 3. RecoveryTestSupport.cs and acceptance test sources are allowlisted only for the separate D6_4.Tests project.
5. Include every generated Csc input in the manifest. Record its generating target/tool, logical normalized path, and hash. The same TFM/configuration should keep framework/configuration generated sources equivalent. Allow only identified assembly-identity metadata differences required by the variant name. Any substantive generated source difference or unexplained compiler input fails; do not silently exclude obj or generated source files.
6. Compare effective DefineConstants as sets: the variant may add exactly GAGAMBA_WP05_D6_4_TEST_FIXTURE. Scan all shared and generated inputs for that symbol; allow it only in the conditional friend block and the fixed fixture/Reconciler declarations in TransactionStateModel.cs and Reconciler.cs. Parse both builds with their actual preprocessor symbols and compare normalized active source outside those allowlisted regions. A source inventory comparison alone does not establish conditional-source equivalence.

The future check must fail on either direction of source drift, an unexpected source-generation difference, a new fixture-symbol use, or a difference in active production code outside the named guarded regions. None of these build-time checks is implemented or executed in D6.5.

## 6. Production-purity and dependency-graph checks

Future implementation must inspect evaluated configuration, build outputs, and emitted assembly metadata/identities:

1. Regular M1 emits exactly the two existing M1.Tests and M1.Owner friends, no D6_4.Tests grant, no fixture symbol, marker type, fixture overload/field, or synthetic stop-selection branch.
2. Test-variant M1 emits only the D6_4.Tests friend, has the variant assembly identity and fixture symbol, and contains the fixed marker/seam.
3. Gagamba.Execution emits its existing friends plus both new friend names.
4. The D6_4.Tests project resolves the test variant and normal Gagamba.Execution, and its project graph does not reference/build Owner.
5. Owner resolves the regular production M1 assembly identity. Its project/dependency graph contains no fixture variant.
6. Normal release/publish graphs and produced artifacts contain neither the test variant nor D6_4.Tests; the test variant is non-packable.
7. Production and variant output, intermediate, generated, and package-asset paths are disjoint and no build can overwrite the other's artifacts.
8. Checks inspect resolved identities and emitted metadata/compiler inputs, not just project names, filenames, or a documented compilation-symbol value.

These are future requirements. D6.5 did not build either M1 assembly, inspect emitted binaries, or run tests.

## 7. Preserved fixture and acceptance behavior

The accepted D6.4 mechanism is unchanged: the test-only marker returns only Kill=NotAttempted, Population=NotObserved, TimedOut=false; it accepts no caller-supplied result, delegate, or payload. It enters at the stop-result boundary after planning and identity verification, bypasses only the bounded stop operation, and leaves the real ObservePopulation fallback active once.

| Test | Fallback result | DomainStop | DomainEmpty | Aggregate | Durable CompletedCleanup |
|---|---|---|---|---|---|
| R1-T13 | Empty | Unknown | Confirmed | QuarantinedUnknown | 0x08 -> 0x0A |
| R1-T14 | Populated | Unknown | Unknown | QuarantinedUnknown | 0x08 |
| R1-T15 | Unknown while the matching live lease remains | Unknown | Unknown | QuarantinedUnknown | 0x08 |

All cases keep the matching live lease/identity, seeded v3 baseline with Cleanup=InProgress and DomainRemoval=0x08, and a healthy journal writer. They require zero WriteKill calls, exactly one fallback population read, zero RemoveChild calls, zero staging-cleaner calls, no deadline reset, no DomainStop confirmation, and no CleanupConfirmed. T13 adds only DomainEmpty; T14/T15 add no domain bit. Final durable Cleanup is Unknown. The existing seeded DomainRemoval bit, not DomainStop=Unknown, is the basis for zero removal calls. These remain future acceptance specifications, not implemented R12 tests.

## 8. Evidence limits and approval boundaries

**A test-built variant is not binary-identical evidence for the production assembly.** The R1-T13–T15 evidence claim is limited to the typed classifier, shared source inventory verified against production, controlled fixed stop-result injection, and the real fallback observation, persistence, and aggregation paths executed through the variant. It does not prove binary identity, execution of the normal production stop path, Owner integration, native sandbox qualification, end-to-end release qualification, or behavior for untested build configurations.

D6.5 changes documentation only. It does not implement the fixture, change source/build configuration, modify R1-T1–T15 outcomes, or alter the C3 mapping, aggregation precedence, journal semantics, or clock/deadline policy. No tests or build qualification were run. C1–C4 remain proposed and unapproved; C3 Policy A/B remains pending human selection. No production remediation is authorized, G3 is not accepted, and release is not qualified. H1–H5, WP08, WP10, full G3, and portability qualification remain deferred.

**Proposed final disposition:** WP05 D6.5 FIXTURE ISOLATION AND SOURCE-EQUIVALENCE CORRECTIONS COMPLETE - GLM R1/R2 ADDRESSED IN DESIGN; FOCUSED INDEPENDENT REVIEW REQUIRED; C3 POLICY AND C1-C4 APPROVAL PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED

## 9. Focused GLM re-review questions

1. Are the production and test-variant M1 friend sets now achievable exactly as specified?
2. Are both required Gagamba.Execution friend grants accounted for, with existing grants preserved?
3. Is the emitted-metadata and residual name-based exposure description accurate?
4. Is the same-project MSBuild strategy concrete and compatible with the inspected configuration, including preventing variant properties from leaking into Gagamba.Execution?
5. Does the verifier compare the evaluated and actual source graphs in both directions, with canonical paths and content hashes?
6. Are generated inputs fully recorded and only assembly-identity metadata differences allowlisted?
7. Are conditional-compilation differences limited to the exact fixture and friend regions, with other active source shown equivalent?
8. Do production-purity checks inspect emitted identities, metadata, Owner/release graphs, and isolated outputs?
9. Are the test-built variant's evidence limits explicit and honest?
10. Are all previously verified R1-T13–T15 outcomes and negative assertions preserved? Does any blocking R1/R2 finding remain, or only implementation detail?

**Handoff:** independently review the exact D6.5 documentation commit and the inspected source/MSBuild configuration. Luna has not performed or claimed the independent review. Do not approve C1–C4, select C3 Policy A/B, authorize implementation, accept G3, or qualify release.
