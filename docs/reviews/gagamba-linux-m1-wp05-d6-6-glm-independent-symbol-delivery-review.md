# WP05 D6.6 — GLM Independent Focused Review (Fixture-Symbol Delivery)

**Reviewer:** GLM (independent, adversarial)
**Producer:** Luna
**Scope:** D6.5-R2a only — the MSBuild fixture-symbol delivery mechanism. Documentation-only design review; no implementation is authorized or performed.
**Prior finding:** D6.5 GLM review section 12, blob `722613ebe1d4b0439e15704fcb203e19a07cd813`

**Disposition:** **WP05 D6.6 GLM INDEPENDENT REVIEW COMPLETE - OPTION A: D6.5-R2a VERIFIED ADDRESSED IN DESIGN; D6.4-R1 AND D6.4-R2 DESIGN FINDINGS CLOSED; C1-C4 READY FOR INDIVIDUAL HUMAN CONTRACT DECISIONS; C3 POLICY SELECTION PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

**R2a verdict: VERIFIED ADDRESSED (design).** The proposed carrier-property delivery, M1-local conditioned append, and edge property isolation are mutually consistent, source-grounded against the installed .NET SDK 10.0.401 targets, and preserve the required invariant **Variant compiler symbols = Production compiler symbols + exactly one fixture symbol**. All compiler-input verification remains a future qualification requirement; none of it was executed in this review.

## 1. Exact Git binding and ancestry

| Item | Verified value |
|---|---|
| Repository / remote | `C:\Users\Laszlos\source\repos\Gagamba`; origin `https://github.com/jenolaszlo-sketch/gagamba.git` |
| D6.6 producer branch | `design/wp05-d6-6-fixture-symbol-delivery` (local worktree `C:\Users\Laszlos\source\repos\Gagamba-d6-6`, clean) |
| D6.6 producer commit | `4bcdac8ac0941f6ec70fb401db7f204f38a5770e` — resolves; sole commit at the branch tip |
| Immediate parent | `114801e4f2b698fb3d82496b2a9926220b206f4c` (D6.5 preservation) — verified exact via `git rev-parse <sha>^` |
| Changed files | Exactly three, matching the expected scope: `docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md` (modified), `docs/decisions/gagamba-linux-m1-wp05-d6-5-fixture-isolation-equivalence.md` (modified), `docs/decisions/gagamba-linux-m1-wp05-d6-6-fixture-symbol-delivery.md` (added). 150 insertions / 13 deletions; no source, test, project, native, CI, or schema change. |
| Prior evidence | D6.5 producer `6bd714d894ad8152a87692f749521abf79916bf5`; D6.5 GLM review `1051018c272a578613309c07cb4d23d627216482`; review blob `722613ebe1d4b0439e15704fcb203e19a07cd813` (read directly from the committed review, including section 12); D6.5 preservation `114801e4f2b698fb3d82496b2a9926220b206f4c`; frozen R12 `38f6981b2a60daab2df37c92913a6760c1bef131` (tree `6854675d6b37a49d5302d6104103bdbfbc268040`); harness Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f` — all resolve and match the reported identities |
| Publication state | Producer branch contained in no remote-tracking ref (`for-each-ref refs/remotes` has no `d6-6` entry) — consistent with "local and unpublished"; no remote requirement imposed |
| Review worktree | `C:\Users\Laszlos\source\repos\Gagamba-review-d66-glm`, branch `review/wp05-d6-6-glm-independent-symbol-delivery`, created from the exact producer commit `4bcdac8`; this review commit is its only commit and a direct child of the producer. Luna's worktree was not touched; no merge, rebase, amend, or cherry-pick. |

## 2. Original D6.5-R2a finding

From the committed D6.5 GLM review (section 12):

> **D6.5-R2a — The fixture-symbol DefineConstants delivery mechanism is unspecified, and the natural mechanism is demonstrably infeasible.** (a) A global `DefineConstants` carrying only the symbol evaluates as the symbol **alone**, discarding the SDK's `TRACE;RELEASE` (project assignments cannot override a global property); (b) `AddImplicitDefineConstants` appends the implicit framework constants by target-execution-time assignment, which a global `DefineConstants` also suppresses; (c) MSBuild splits `Properties`/`-p:` values on `;`, so a multi-constant union requires `%3B` escaping.

The prescribed smallest correction was: a dedicated custom carrier property passed on the `D6_4.Tests`→M1 reference, appended to `DefineConstants` inside the M1 project (conditioned assignment or a target before `CoreCompile`), never passing `DefineConstants` itself as a global property, and an edge-removal list that tracks the actual variant global properties.

## 3. D6.6 correction summary

D6.6 §3–§5 specify exactly that mechanism:

1. **Carrier:** `GagambaWp05D64FixtureEnabled=true` supplied only on the `D6_4.Tests`→M1 `ProjectReference`. No `DefineConstants` or semicolon-separated symbol list is passed as a project-reference property.
2. **Append:** a conditioned `PropertyGroup` inside `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`, placed immediately after the existing unconditional PropertyGroup and before its ProjectReference ItemGroup, appending the single literal `GAGAMBA_WP05_D6_4_TEST_FIXTURE` to `$(DefineConstants)`.
3. **Isolation:** the M1→`Gagamba.Execution` ProjectReference gains `GlobalPropertiesToRemove="AssemblyName;DefineConstants;BaseIntermediateOutputPath;MSBuildProjectExtensionsPath;BaseOutputPath;IsPackable;GagambaWp05D64FixtureEnabled"` — the six previously established removals preserved, the carrier added.
4. **Acceptance:** future Csc-task-input/binlog verification with the set-difference invariant, explicitly rejecting evaluated-property output as a symbol oracle.

The two amended documents (D6.4 §4 variant-property paragraphs; D6.5 §3/§5/§6) were updated consistently with the same XML snippets and removal list. The three documents are mutually consistent.

## 4. Review A — Carrier property

Verified against the pinned project files and repo-wide scan at `4bcdac8`:

1. **Single unambiguous purpose.** `GagambaWp05D64FixtureEnabled` has no SDK meaning, no default assignment anywhere in the repository or the installed SDK, and exactly one proposed consumer (the conditioned M1 PropertyGroup). It is a boolean gate, not a value channel.
2. **Production M1 does not enable it.** Repo-wide scan: the carrier name appears only in the three design documents and two handoff/checkpoint records — no csproj, `Directory.Build.props` (inspected: no `DefineConstants`/symbol/carrier assignments), no `Directory.Build.targets` (does not exist). Normal M1, Owner, and core evaluations cannot define it.
3. **No global `DefineConstants` override.** D6.6 §3 explicitly forbids supplying `DefineConstants` as a project-reference property, and the D6.4/D6.5 amendments add the matching prohibition ("Do not pass DefineConstants as a global property"). `DefineConstants` removal at the core edge remains as defense in depth. This closes all three R2a sub-findings (a)–(c).
4. **Fixed constant, not caller-supplied text.** The conditioned assignment is `<DefineConstants>$(DefineConstants);GAGAMBA_WP05_D6_4_TEST_FIXTURE</DefineConstants>` — a literal token; no caller-controlled string is concatenated. The carrier can only select between "append the fixed symbol" and "append nothing".
5. **Consumed only by the intended variant instance.** The sole consumer is the M1 project body, evaluated once per project instance. The core receives no carrier (section 6 below); Owner references M1 plainly (`src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj` — plain `ProjectReference`, no properties metadata) and `Gagamba.Execution` has no conditioned group.
6. **No accidental enablement.** The condition is the exact string comparison `== 'true'` — unset, empty, `false`, `TRUE`, or `1` all fail closed. Normal Owner and release builds supply no carrier; the symbol cannot be enabled by ambient configuration.

**Reference-metadata sufficiency.** A `ProjectReference` `Properties`/`AdditionalPropertiesFromProject` style property assignment is delivered to the child build as a global property. I verified the delivery class non-mutatingly by evaluating the M1 project with a global `-p:GagambaWp05D64FixtureEnabled=true` (the equivalent of reference metadata): the global property is visible to project-body conditions in the child evaluation. Because the conditioned group does not yet exist in the project (D6.6 is documentation-only), the evaluation correctly returned the unchanged `TRACE;RELEASE` — the mechanism is deliverable, and its effect is a future implementation check. This is sufficient to judge the documented mechanism feasible.

**Review A verdict: supported.** The carrier is narrow, fixed, fail-closed, and delivered by the documented project-reference metadata to exactly the intended M1 project instance.

## 5. Review B — DefineConstants append and SDK ordering

Independently verified against the installed SDK 10.0.401 (the only installed SDK) targets and the M1 project's actual import order — not from comments or assertions:

| Phase | Mechanism | Evidence (SDK 10.0.401) | Effect on `DefineConstants` |
|---|---|---|---|
| Props phase (before M1 body) | `TRACE` established | `Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.Sdk.CSharp.props:21-22`; M1 `-preprocess` shows the import at line 1092, M1 body at 1491 | Append (idempotent guard for the non-empty case) |
| Project body (proposed append position) | `;GAGAMBA_WP05_D6_4_TEST_FIXTURE` | D6.6 §3 snippet; feasible placement (M1 PropertyGroup lines 3-8, ProjectReference ItemGroup 10-12) | Append, once per evaluation |
| Targets phase (after M1 body, still evaluation) | Configuration symbol | `Microsoft.NET.Sdk.CSharp.targets:16-31` (`ImplicitConfigurationDefine=$(Configuration.ToUpperInvariant())`, then `$(DefineConstants);$(ImplicitConfigurationDefine)`); `-preprocess` import at line 15575 > body 1491 | Append → `RELEASE` for the Release configuration |
| Target execution | Implicit framework/platform constants | `Microsoft.NET.Sdk.BeforeCommon.targets:290-298` (`AddImplicitDefineConstants`, `AfterTargets="PrepareForBuild"`, `$(DefineConstants);@(_ImplicitDefineConstant)`) | Append (NET10_0, NET10_0_OR_GREATER, NETCOREAPP, etc. for net10.0) |
| Final compiler input | Csc consumption | `Roslyn/Microsoft.CSharp.Core.targets:35` (`CoreCompile`), `:97`/`:111` (`DefineConstants="$(DefineConstants)"`); `CoreBuildDependsOn` (`Microsoft.Common.CurrentVersion.targets:941-959`) orders `PrepareForBuild` before `Compile`, so `AddImplicitDefineConstants` runs before `CoreCompile` | Read-only consumption of the accumulated value |

Verified reproduction (non-mutating, permitted): `dotnet msbuild -getProperty:DefineConstants -p:Configuration=Release` on the base M1 project returns `TRACE;RELEASE`, with `AssemblyName=Gagamba.Execution.Linux.M1`, `BaseIntermediateOutputPath=obj\`, `BaseOutputPath=bin\` — matching D6.6 §3's reported evaluation exactly. `Gagamba.Execution` evaluates to `TRACE;RELEASE`, `Gagamba.Execution`, `bin\Release\net10.0\`, `obj\Release\net10.0\`, `MSBuildProjectExtensionsPath` under `src\Gagamba.Execution\obj\` — matching D6.6 §4.5.

**Replacement challenge (explicitly performed).** I searched the installed SDK for every post-body assignment to `DefineConstants`:

- `Microsoft.NET.Sdk.CSharp.targets:31` — append.
- `Microsoft.NET.Sdk.BeforeCommon.targets:295` — append.
- `Microsoft.NET.Sdk.BeforeCommon.targets:278` (`_DisableDiagnosticTracing`) — the only non-append mutation: an item round-trip that strips `TRACE` when `DisableDiagnosticTracing=true`. No default for `DisableDiagnosticTracing` exists anywhere in the installed SDK or the repository, so the target is inert for this design; and if it ever ran, the item round-trip preserves all other tokens in order, so the fixture symbol would survive. The §5 acceptance checks would flag the lost `TRACE`.
- `Microsoft.NET.PreserveCompilationContext.targets:27` — item metadata recording the current value for the dependency file, not a property replacement.
- `Microsoft.CSharp.CurrentVersion.targets:254` — a Csc task consuming `$(DefineConstants)` (read-only).

No SDK target or later import replaces the locally appended value in the installed 10.0.401 targets under this repository's configuration.

**Duplicate-append challenge.** The fixture symbol is introduced by exactly one evaluation-time assignment in the project body; each build pass evaluates the project fresh (no cross-evaluation accumulation), no SDK target adds the fixture token, and `AddImplicitDefineConstants` appends its own constants once per build pass (multi-TFM inner builds are fresh evaluations). Repeated evaluation or target execution cannot append the fixture symbol twice. The §5 "occurs exactly once" check remains the runtime falsifier.

**Invariant feasibility.** Because every mutation between the M1 body and the Csc task input is an append (or an inert opt-in round-trip), and the framework constants depend only on `TargetFramework`/configuration (identical for production and variant), the final Csc symbol input satisfies V = P ∪ {GAGAMBA_WP05_D6_4_TEST_FIXTURE} by construction. The evaluated-order nuance (`TRACE;GAGAMBA_WP05_D6_4_TEST_FIXTURE;RELEASE`) is irrelevant because the invariant is set-based, as D6.6 §5 specifies.

**Review B verdict: supported.** The ordering is correct as documented; no correction needed. The D6.6 §3 ordering narrative matches the actual SDK files and import order line-for-line.

## 6. Review C — Property isolation

Verified against `Microsoft.Common.CurrentVersion.targets` (SDK 10.0.401) and the project files:

- **Mechanism.** The project-reference build path passes `RemoveProperties="%(_MSBuildProjectReferenceExistent.GlobalPropertiesToRemove)$(_GlobalPropertiesToRemoveFromProjectReferences)"` to the MSBuild task (lines 1044/1073/1102, also 2140/2160/2181/2799/5268/5972 and publish at `Microsoft.NET.Publish.targets:897`; the TFM-negotiation child invocation at 1852/1868 also honors the same metadata). `GlobalPropertiesToRemove` metadata on the `ProjectReference` item therefore removes the listed global properties from the child build. The D6.6 §4 claim is exact.
- **Carrier inclusion.** The documented metadata lists all six previously established removals (`AssemblyName;DefineConstants;BaseIntermediateOutputPath;MSBuildProjectExtensionsPath;BaseOutputPath;IsPackable`) plus `GagambaWp05D64FixtureEnabled` — the edge-removal list now tracks the actual variant global properties, exactly as the D6.5 review required. The concatenation with the SDK's default list is safe: `_GlobalPropertiesToRemoveFromProjectReferences` is built with a leading `;` (`;OutputPath`), so the metadata value without a trailing semicolon parses correctly.
1. **Carrier reaches the variant M1 project** — via the D6_4.Tests→M1 reference properties (delivery class verified in section 4).
2. **Carrier does not reach `Gagamba.Execution`** — removed at the M1→core edge; the TFM-negotiation child invocation strips the same list.
3. **Fixture symbol does not reach the core** — the core has no conditioned group, evaluates `TRACE;RELEASE`, and the symbol is appended only inside the variant M1 instance; §5 requires the core's Csc input to contain no fixture token.
4. **Normal core assembly identity** — `Gagamba.Execution` (evaluated, confirmed above; documented in D6.6 §4.5 and D6.5 §6.3).
5. **Normal core output/intermediate paths** — `bin\Release\net10.0\`, `obj\Release\net10.0\`, extensions path under `src\Gagamba.Execution\obj\` (evaluated, confirmed above).
6. **Owner references the regular M1** — plain `ProjectReference` with no variant properties in `src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj`; no path from Owner to the variant.
7. **Compatibility with the same-project variant architecture** — the reference-edge removal is the same mechanism previously supported in the D6.5 review (`GlobalPropertiesToRemove`/`RemoveProperties`); adding one name changes nothing structural. Variant M1 outputs/intermediates/extensions remain disjoint by the previously established path properties.

**Status of checks in this review.** Checks 4–6 and the core `DefineConstants`/paths evaluation were **actually performed** during this review (static inspection and non-mutating property evaluation at `4bcdac8`). Checks 1–3 (the carrier's presence/absence in child *evaluations under a real project-reference build*) are **documented future checks** (D6.6 §4 items 1–3; D6.5 §5.7, §6.3) and were **not** executed here, because the carrier and conditioned group do not exist as code and no build was authorized. The review validates the mechanism, not an implemented build.

**Review C verdict: supported.** The documented isolation contract is precise, uses the correct supported metadata, and is consistent with the same-project variant architecture.

## 7. Review D — Compiler-input verification

D6.6 §5 requires the final `DefineConstants` input of each actual Csc task (binlog, scoped by project instance) or the exact compiler command/response-file argument consumed by Csc, reconciled with the task input, and explicitly rejects `dotnet msbuild -getProperty:DefineConstants` as a symbol oracle because it does not execute `AddImplicitDefineConstants` (AfterTargets=`PrepareForBuild`). That statement is technically correct against the installed SDK (verified above: the implicit constants are assigned at target-execution time, after evaluation).

Adequacy against the required matrix:

| Build | Required result | Coverage in D6.6 |
|---|---|---|
| Production M1 | Normal constants intact; fixture absent | §5 bullet 1 + failure condition "any production configuration/SDK symbol is lost" / "production contains the fixture token" |
| Variant M1 | Normal constants intact; fixture exactly once | §5 set-difference V − P = {GAGAMBA_WP05_D6_4_TEST_FIXTURE}, token exactly once, no other delta |
| Gagamba.Execution | Normal constants and identity; no fixture symbol | §5 bullet 3 + §4 items 3–5 (carrier absent, no fixture Csc token, `AssemblyName=Gagamba.Execution`, documented paths) |
| Owner | Regular M1 dependency; no fixture variant | D6.5 §6.5, §6.7 (Owner resolves regular identity; no variant in graph), preserved unchanged |

Detection capability of the documented evidence (final compiler inputs, not evaluation):

- Lost `TRACE`/`RELEASE` — detected (production-retention requirement + failure conditions).
- Missing SDK framework constants — detected ("every applicable SDK-generated target-framework/platform symbol").
- Duplicate fixture symbols — detected ("occurs exactly once"; "zero or multiple fixture tokens" fails).
- Unexpected additional symbols — detected ("no other added, removed, or duplicated token"; "any other symbol delta" fails).
- Property leakage — detected (carrier-absence checks at the core child evaluation and Csc input).
- Wrong assembly identity / incorrect output paths — detected (§4 items 4–6, D6.5 §6.3/§6.7).
- Production/test artifact contamination — detected (disjoint path requirements and "production contains the fixture token" failure; emitted-metadata checks in D6.5 §6).

These are correctly framed as **future qualification requirements** — "required for a later authorized implementation. No compiler verification is claimed here." Nothing in this review executed them. The procedure is sufficiently precise to be falsifiable and to establish final compiler inputs; the response-file reconciliation clause closes the last evidence gap between Csc task input and the argument actually consumed by the compiler.

**Review D verdict: supported.** The acceptance procedure is precise and correctly scoped to the compiler-input oracle.

## 8. Preserved findings and accepted contracts

Verified from the exact diff of `4bcdac8` against its parent:

- **D6.4-R1 remains VERIFIED ADDRESSED.** The D6.4 amendment touches only the two variant-property paragraphs (delivery mechanism and edge-removal list); the friend-attribute conditional block, marker, injection boundary, and R1-T13–T15 sections are unchanged.
- **D6.4-R2 / R2a scope.** D6.5 §3/§5/§6 changes are confined to the fixture-symbol delivery specification and its verification wording. The same-project variant strategy, identical-source requirements, production-purity checks, and the non-binary-identical evidence limitation are preserved verbatim in substance; D6.6 §6 states this explicitly.
- **Conditional friend attributes, same-project variant architecture, production source inventory** — unchanged (no source or project file modified; the diff contains exactly the three documents).
- **Fixed `NotAttempted`/`NotObserved` fixture marker, post-planning post-identity injection boundary, real `ObservePopulation()` fallback, zero-removal via seeded `DomainRemoval`, existing durable-progress semantics, no fabricated cleanup confirmation** — preserved in D6.6 §6 and untouched in the amended documents.
- **R1-T13–T15** remain future acceptance specifications; the D6.6 §6 table matches the required values exactly:

| Test | DomainStop | DomainEmpty | Aggregate | Durable bits |
|---|---|---|---|---|
| R1-T13 | Unknown | Confirmed | QuarantinedUnknown | `0x08 → 0x0A` |
| R1-T14 | Unknown | Unknown | QuarantinedUnknown | `0x08` |
| R1-T15 | Unknown | Unknown | QuarantinedUnknown | `0x08` |

- **No source, tests, project files, native code, or CI configuration changed** — confirmed by `git diff --name-status` (three documentation files only).

## 9. Blocking findings

None. R2a's three infeasibility mechanisms ((a) global `DefineConstants` replacing configuration constants, (b) suppression of target-execution-time implicit constants, (c) semicolon splitting in `Properties`/`-p:`) are each eliminated by the specified design: the carrier is a custom property; the append is a project-body assignment that survives every subsequent SDK append; and no symbol list is passed through `Properties`/`-p:`.

## 10. Optional observations (non-blocking)

1. **SDK pinning.** The ordering verification is scoped to installed SDK 10.0.401. The repository has no `global.json`; the SDK is selected by machine installation. A future qualification should either add a `global.json` pin or re-verify the §3 ordering narrative against whatever SDK is installed, since the ordering depends on SDK file content. The §5 checks would catch symbol-set drift at qualification time regardless.
2. **Carrier casing.** The condition compares the exact string `true`; an implementer passing `TRUE` or `1` would silently fail to enable the fixture (fail-closed, and the §5 exactly-once check would catch it). Worth keeping the documented lowercase form in the D6_4.Tests project reference.
3. **`_DisableDiagnosticTracing` note.** The only non-append post-evaluation mutation in the SDK is opt-in via `DisableDiagnosticTracing=true` (no default set here). If that property were ever enabled, `TRACE` would be stripped from the final compiler input; the production-retention check would flag it. No action needed for this design.
4. **Trailing slashes.** Carried from the D6.5 optional observations: the variant `BaseIntermediateOutputPath`/`BaseOutputPath` values must end with a trailing separator for correct path concatenation; D6.6 §4.5's documented core paths already use the correct form.
5. **Metadata concatenation detail.** The SDK appends its default removal list with a leading semicolon (`Microsoft.Common.CurrentVersion.targets:817-819`), so the documented `GlobalPropertiesToRemove` value without a trailing semicolon is safe; if a future editor appends a name, keep the semicolon-separated form.

## 11. Human-decision readiness

With R2a verified addressed in design, D6.4-R1 and D6.4-R2 are fully addressed in design. The C1–C4 contracts are ready for individual human decisions; the C3 expired-deadline policy remains a separate human choice. C1–C4 remain `PROPOSED - NOT APPROVED`; C3 remains `C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor Policy B is selected. This review authorizes no implementation, production remediation, test execution, contract approval, G3 acceptance, or release qualification. H1–H5, WP08, WP10, full G3, and portability qualification remain deferred.

## 12. Final disposition

**WP05 D6.6 GLM INDEPENDENT REVIEW COMPLETE - OPTION A: D6.5-R2a VERIFIED ADDRESSED IN DESIGN; D6.4-R1 AND D6.4-R2 DESIGN FINDINGS CLOSED; C1-C4 READY FOR INDIVIDUAL HUMAN CONTRACT DECISIONS; C3 POLICY SELECTION PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

R2a is verified addressed: the carrier-property delivery, the M1-local conditioned append, and the `GlobalPropertiesToRemove` edge isolation were independently verified against the installed SDK 10.0.401 targets, the pinned project files, and non-mutating evaluations that reproduced the documented property values exactly (`TRACE;RELEASE`; `Gagamba.Execution`; `bin\Release\net10.0\`; `obj\Release\net10.0\`). The invariant V = P + one fixture symbol is preserved by evaluation/import/target ordering as documented. All compiler-input verification remains a documented future requirement, correctly demarcated. This review made no source, project, or build change; ran no build, test, or compilation; and its only commit is this document.
