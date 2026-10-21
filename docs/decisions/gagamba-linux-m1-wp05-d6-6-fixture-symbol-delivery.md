# Gagamba Linux M1 — WP05 D6.6 Fixture-Symbol Delivery Correction

- Author: Luna
- Independent reviewer: GLM
- Scope: D6.5-R2a only
- Status: PROPOSED DOCUMENTATION CORRECTION — FOCUSED INDEPENDENT GLM REVIEW REQUIRED
- Implementation: NOT AUTHORIZED

## 1. Exact Git and evidence binding

- Repository: C:\Users\Laszlos\source\repos\Gagamba
- Origin: https://github.com/jenolaszlo-sketch/gagamba.git
- Branch: design/wp05-d6-6-fixture-symbol-delivery
- Worktree: C:\Users\Laszlos\source\repos\Gagamba-d6-6
- Exact branch base and required immediate parent of the D6.6 producer commit: D6.5 preservation 114801e4f2b698fb3d82496b2a9926220b206f4c.
- D6.5 Luna producer: 6bd714d894ad8152a87692f749521abf79916bf5.
- D6.5 GLM review: 1051018c272a578613309c07cb4d23d627216482. Review document: docs/reviews/gagamba-linux-m1-wp05-d6-5-glm-independent-isolation-equivalence-review.md; expected blob: 722613ebe1d4b0439e15704fcb203e19a07cd813.
- D6.5 preservation is the requested base. The D6.5 producer, review, and preservation objects all resolve; the review document blob matches the supplied value.
- Frozen R12 production: 38f6981b2a60daab2df37c92913a6760c1bef131. Frozen harness Q3: 36a124d7dff98ecfaf956340767fe1768ad5b17f.
- The D6.6 producer is the sole documentation commit at the named branch tip, directly above the exact preservation commit. Its resulting SHA is reported in the producer handoff.

## 2. GLM finding

GLM's D6.5 section 12 identifies D6.5-R2a as the sole remaining blocking documentation finding. Passing DefineConstants as an MSBuild global property replaces the project's configuration constants, can suppress SDK implicit framework constants assigned during target execution, and makes a semicolon-separated union unsafe to pass through ProjectReference Properties or -p:.

This correction specifies one delivery mechanism. It does not reopen the accepted same-project variant design, friend arrangement, source-graph checks, production-purity checks, fixture behavior, or binary-identity limitation.

## 3. Selected mechanism and SDK ordering

The D6_4.Tests-to-M1 ProjectReference supplies the dedicated boolean carrier GagambaWp05D64FixtureEnabled=true only for the test-variant M1 instance. It does not supply DefineConstants or any semicolon-separated compiler-symbol list as a project-reference property.

In src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj, place this conditioned PropertyGroup immediately after the existing unconditional project PropertyGroup and before its ProjectReference ItemGroup:

~~~xml
<PropertyGroup Condition="'$(GagambaWp05D64FixtureEnabled)' == 'true'">
  <DefineConstants>$(DefineConstants);GAGAMBA_WP05_D6_4_TEST_FIXTURE</DefineConstants>
</PropertyGroup>
~~~

This is an M1-local property append, not a global DefineConstants override and not an arbitrary symbol-injection interface. With the carrier unset or false, the group does not execute and normal production M1 has no fixture symbol. With the carrier true, it appends the fixed fixture symbol once.

The source ordering was inspected against the installed .NET SDK 10.0.401 used by the repository's SDK selection:

- The SDK C# props establish TRACE before the M1 project body is evaluated.
- The SDK C# targets append the active configuration symbol, RELEASE for the inspected Release evaluation, to the current DefineConstants value.
- Microsoft.NET.Sdk.BeforeCommon.targets defines AddImplicitDefineConstants with AfterTargets=PrepareForBuild; that target appends the applicable target-framework, platform, and compatibility constants during build execution.
- The M1 project body is evaluated before those SDK targets. Its conditioned append therefore remains in the accumulated value when configuration and implicit framework constants are added. No CoreCompile hook is needed for this selected design.

The existing M1 project has no local DefineConstants assignment or Compile-item override. A permitted non-mutating Release/net10.0 evaluation at the exact D6.5 preservation base returned DefineConstants=TRACE;RELEASE, AssemblyName=Gagamba.Execution.Linux.M1, BaseOutputPath=bin\, and BaseIntermediateOutputPath=obj\. This evaluation did not execute targets and is not compiler verification.

## 4. Project-reference property isolation

Keep the carrier scoped to the M1 test variant. On the M1-to-Gagamba.Execution ProjectReference, set the exact GlobalPropertiesToRemove item metadata below:

~~~xml
<ProjectReference Include="..\Gagamba.Execution\Gagamba.Execution.csproj"
                  GlobalPropertiesToRemove="AssemblyName;DefineConstants;BaseIntermediateOutputPath;MSBuildProjectExtensionsPath;BaseOutputPath;IsPackable;GagambaWp05D64FixtureEnabled" />
~~~

The existing removals for AssemblyName, DefineConstants, BaseIntermediateOutputPath, MSBuildProjectExtensionsPath, BaseOutputPath, and IsPackable remain. The carrier is added to that list. Microsoft.Common.CurrentVersion.targets passes the ProjectReference GlobalPropertiesToRemove metadata through the MSBuild task's RemoveProperties input in the project-reference build path. DefineConstants removal remains defense in depth; the test variant does not pass a DefineConstants global property.

Future graph/build-log verification must show all of the following:

1. The variant M1 project evaluation receives GagambaWp05D64FixtureEnabled=true.
2. Normal production M1 receives no true carrier and does not enable fixture mode.
3. The child Gagamba.Execution project evaluation has no GagambaWp05D64FixtureEnabled global property after the M1-to-core edge is processed.
4. Gagamba.Execution compiler input contains no fixture symbol and its ordinary compilation properties remain in effect.
5. Gagamba.Execution retains AssemblyName=Gagamba.Execution and its normal Release/net10.0 output/intermediate locations: src/Gagamba.Execution/bin/Release/net10.0, src/Gagamba.Execution/obj/Release/net10.0, and MSBuildProjectExtensionsPath under src/Gagamba.Execution/obj.
6. The test project resolves Gagamba.Execution.Linux.M1.TestFixture plus normal Gagamba.Execution; Owner continues to resolve regular Gagamba.Execution.Linux.M1 and has no path to the variant. Variant M1 outputs and intermediates remain disjoint from production paths.

## 5. Future compiler-symbol acceptance checks

These are required for a later authorized implementation. No compiler verification is claimed here.

Capture the final DefineConstants input of each actual Csc task in the build binary log, scoped by project instance, or capture the exact compiler command/response-file argument consumed by Csc and reconcile it with the task input. Include production M1, test-variant M1, and the shared Gagamba.Execution project. Do not use evaluated property output as a substitute for compiler inputs.

Let P be the exact production M1 compiler-symbol set and V be the test-variant M1 compiler-symbol set for the same Configuration and TargetFramework. Require:

- Production M1 retains TRACE, RELEASE, and every applicable SDK-generated target-framework/platform symbol; it has no fixture symbol.
- Every symbol in P remains in V. The set difference V minus P is exactly {GAGAMBA_WP05_D6_4_TEST_FIXTURE}; the fixture token occurs exactly once in the compiler input, with no other added, removed, or duplicated token.
- Gagamba.Execution has no fixture symbol, no fixture carrier in its child project global properties, and retains its normal configuration, TargetFramework, assembly identity, output path, and intermediate path.

A dotnet msbuild -getProperty:DefineConstants query alone does not establish the final compiler-symbol set: it evaluates project properties but does not execute AddImplicitDefineConstants, which appends SDK framework/platform symbols after PrepareForBuild. Evaluated properties remain useful for checking the carrier, project identity, and configured paths; the Csc task input or actual consumed command/response file is the symbol oracle.

Failure conditions are: any production configuration/SDK symbol is lost; production contains the fixture token; the variant is missing a production token, has zero or multiple fixture tokens, or contains any other symbol delta; the carrier reaches Gagamba.Execution; the core compiler receives the fixture symbol; or the core project's normal identity/output/intermediate properties change.

## 6. Preserved findings and fixture contract

D6.4-R1 remains VERIFIED ADDRESSED. The D6.4-R2 same-project test-variant strategy, identical-source requirements, production-purity checks, and stated non-binary-identical evidence limitation remain supported; this correction closes only the D6.5-R2a documentation gap. No accepted design is reopened.

The fixed fixture behavior remains unchanged: NotAttempted/NotObserved; injection after planning and identity verification; real fallback ObservePopulation call; zero WriteKill calls; zero removal calls through seeded DomainRemoval; existing journal semantics; no fabricated cleanup confirmation.

| Test | Fallback | DomainStop | DomainEmpty | Aggregate | Durable bits |
|---|---|---|---|---|---|
| R1-T13 | Empty | Unknown | Confirmed | QuarantinedUnknown | 0x08 → 0x0A |
| R1-T14 | Populated | Unknown | Unknown | QuarantinedUnknown | 0x08 |
| R1-T15 | Unknown | Unknown | Unknown | QuarantinedUnknown | 0x08 |

R1-T13–T15 remain future test specifications, not executed tests.

## 7. Evidence limitations and approval boundaries

Inspected evidence: the pinned M1 project XML; the D6.5 GLM review, including sections 7 and 12; the installed SDK 10.0.401 C# props/targets and AddImplicitDefineConstants target; the installed Microsoft.Common.CurrentVersion.targets RemoveProperties path; and non-mutating property evaluations for the base M1, shared core, and Owner projects. The existing Release/net10.0 M1 evaluation reports TRACE;RELEASE. The selected append is source-grounded and feasible by MSBuild import/target ordering.

Not implemented or executed: the carrier property, project-reference metadata, conditioned M1 append, Csc invocation, build, fixture compilation, test, binlog, or binary inspection. No executed compiler verification is claimed. D6.6 changes only the three requested documentation files.

C1–C4 remain PROPOSED - NOT APPROVED. C3 remains C3 ZERO-WAIT POLICY - PENDING HUMAN. No implementation, production remediation, test execution, G3 acceptance, or release qualification is authorized or claimed. Release is not qualified.

## 8. Focused independent GLM review questions

1. Does the conditioned project-level append occur in the correct order relative to the SDK's configuration and implicit framework constants, based on the cited SDK files?
2. Is GagambaWp05D64FixtureEnabled a sufficiently narrow carrier, supplied only to the M1 variant and incapable of injecting arbitrary compiler symbols?
3. Is GlobalPropertiesToRemove the exact supported M1-to-core property-removal metadata, including the carrier and the preserved existing removals?
4. Do the future Csc/binlog checks establish production compiler symbols, exactly one fixture-symbol delta for the variant, and absence of both carrier and symbol in Gagamba.Execution?
5. Are the expected shared-core identity and output/intermediate paths checked through resolved project/build evidence?
6. Does D6.5-R2a have any remaining source-grounded delivery blocker? Review this R2a correction only; do not reopen previously accepted D6.4 findings or select/approve any C1–C4 policy.

**Final disposition:** WP05 D6.6 FIXTURE-SYMBOL DELIVERY DESIGN COMPLETE - D6.5-R2a ADDRESSED IN DOCUMENTATION; FOCUSED INDEPENDENT GLM REVIEW REQUIRED; C3 POLICY AND C1-C4 APPROVAL PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED.
