# WP05 D6.5 GLM Independent Review Preservation Checkpoint

Date: 2026-10-21 (Asia/Manila)
Status: **PRESERVED FOR DOCUMENTATION CORRECTION — OPTION B; D6.4-R1 VERIFIED ADDRESSED; D6.4-R2 PARTIALLY ADDRESSED; D6.5-R2a OPEN; NO CONTRACT APPROVED; RELEASE NOT QUALIFIED**

This checkpoint records evidence custody only. It does not implement, remediate, or qualify anything, and it selects no delivery mechanism for the open finding.

## 1. Repository and frozen evidence identity

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Remote: `https://github.com/jenolaszlo-sketch/gagamba.git`
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`; source tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen WP05 harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`; tree `f764722997130f06c8a62c45a4230805aa538ea5`.
- D6.4 preservation base: `preservation/wp05-d6-4-glm-independent-fixture-review` at `1fedce7199c1a3910ee5b1e0816670dfb7f470e9` — the direct parent of the D6.5 producer commit.

All frozen identities above were re-verified (`git cat-file -t` / `git rev-parse`) before any documentation was written or published. R12 tree and harness Q3 are unchanged and not reopened by D6.5.

## 2. Producer, review, and preservation bindings

| Role | Branch | Commit | Immediate parent |
|---|---|---|---|
| Luna D6.5 producer | `design/wp05-d6-5-fixture-isolation-equivalence` | `6bd714d894ad8152a87692f749521abf79916bf5` | `1fedce7199c1a3910ee5b1e0816670dfb7f470e9` |
| GLM independent review | `review/wp05-d6-5-glm-independent-isolation-equivalence` | `1051018c272a578613309c07cb4d23d627216482` | `6bd714d894ad8152a87692f749521abf79916bf5` |
| Preservation | `preservation/wp05-d6-5-glm-independent-isolation-equivalence-review` | This checkpoint commit (`git rev-parse HEAD` on this branch) | `6bd714d894ad8152a87692f749521abf79916bf5` |

- GLM review document: `docs/reviews/gagamba-linux-m1-wp05-d6-5-glm-independent-isolation-equivalence-review.md`.
- Expected and verified blob: `722613ebe1d4b0439e15704fcb203e19a07cd813`.
- The review commit adds exactly that one document; it does not modify the producer documents.

Required graph, verified by commit parents and ancestry checks:

```text
1fedce71 D6.4 preservation
    │
6bd714d8 Luna D6.5 producer
    │
    +---- 1051018c  GLM review
    │
    +---- NEW        D6.5 preservation
```

`6bd714d8` is the immediate parent of both GLM's review and this preservation checkpoint; the review and preservation are separate sibling histories, and neither sibling tip is an ancestor of the other. The producer's parent `1fedce71` is its exact immediate parent. No merge, rebase, cherry-pick, or amend connects the review and preservation histories; GLM's review is referenced only through the exact commit and blob identities above.

## 3. Scope, diff, and worktree safety

- Luna's producer diff from `1fedce71` contains exactly two documentation files: `docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md` (modified, +92/-20 region) and `docs/decisions/gagamba-linux-m1-wp05-d6-5-fixture-isolation-equivalence.md` (added, 142 lines). Documentation-only: no source, test, native, CI, package, or durable-schema change.
- GLM's review diff from `6bd714d8` contains exactly the single review document named in section 2 (121 lines added). No other file is touched.
- The preservation diff is restricted to this checkpoint, `docs/handoff.md`, and `docs/work-queue.md`. No source, test, native, package, schema, CI, harness, or evidence artifact is changed. The producer's two D6.5 design documents and GLM's review document are not modified.
- Luna's producer worktree `C:\Users\Laszlos\source\repos\Gagamba-d6-5` was on the exact producer commit `6bd714d8`; GLM's reviewer worktree `C:\Users\Laszlos\source\repos\Gagamba-review-d65-glm` was on the exact review commit `1051018c`. Neither was modified by this preservation.
- The canonical checkout remained on `implementation/wp05-release-qualification-fixtures` at `338a98af269e6b5839f3ccc2e0881f016153b01e` with its pre-existing unrelated untracked files (trailing redirection artifacts, `NEM-INDEPENDENT-REVIEW.md`, `spikes/*` scripts and test-output directories, and `tests/*/TestResults/` directories). They were preserved untouched; no worktree was reset, rebased, amended, pruned, or deleted.
- No conflicting `preservation/wp05-d6-5*` branch or `Gagamba-preserve-d65` worktree existed before creation.

## 4. GLM's exact independent disposition

GLM's committed review reports, verbatim:

**WP05 D6.5 GLM INDEPENDENT REVIEW COMPLETE - OPTION B: D6.4-R1/R2 PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

The individual finding statuses, preserved separately:

- **D6.4-R1: VERIFIED ADDRESSED.**
- **D6.4-R2: PARTIALLY ADDRESSED.**
- **D6.5-R2a: OPEN** — a narrow MSBuild fixture-symbol delivery correction required before R2 is fully addressed.

R1 is resolved in design and is not reopened. R2 remains partially addressed solely because of the new finding R2a. No production remediation is authorized by the review, and no release is qualified.

## 5. R1 — VERIFIED ADDRESSED (technical evidence)

GLM verified against frozen R12 source, the project files, and the D6.5 documents:

- **Production M1 friend set preserved exactly.** With the fixture symbol undefined, the specified conditional block emits byte-identical grants to `Gagamba.Execution.Linux.M1.Tests` and `Gagamba.Execution.Linux.M1.Owner` — exactly the current `src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:11-12`; no `D6_4.Tests` grant in production.
- **Test-variant friend set.** With the symbol defined, the variant emits only `Gagamba.Execution.Linux.M1.D6_4.Tests`; the `M1.Tests` and `M1.Owner` grants are structurally excluded by the `#if/#else` form. Achievable exactly as specified.
- **Conditional mechanism quality.** `GAGAMBA_WP05_D6_4_TEST_FIXTURE` is the only fixture symbol; a repo-wide scan at the D6.5 base found zero `#if/#else/#endif` directives and zero fixture-symbol uses in `src/` and `tests/` (docs only), and no `DefineConstants`/symbol assignment in any csproj or `Directory.Build.props` (no overrides; no `Directory.Build.targets`), so production evaluation cannot define it. No duplicate generated assembly attributes result.
- **Shared `Gagamba.Execution` grants.** Both added names are present and both are necessary: the variant (assembly name `Gagamba.Execution.Linux.M1.TestFixture`) compiles M1 sources consuming internal `Restricted` contracts; `Gagamba.Execution.Linux.M1.D6_4.Tests` is required because `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryTestSupport.cs` (linked into the dedicated test assembly) uses internal `Gagamba.Execution` types. Existing grants are kept.
- **No production fixture emission.** With the symbol undefined, every guarded member (marker type, field, overload, selection branch, variant friend) is excluded from the production M1 compilation.
- **Metadata and security boundary stated accurately.** The design's correction that unsigned `InternalsVisibleTo` grants are name-based compiler access — not a cryptographic security or runtime sandbox boundary — is confirmed: zero matches for `SignAssembly`, `AssemblyOriginatorKeyFile`, `PublicSign`, or strong-name properties across all csproj/props/targets. The residual named-friend exposure is bounded and consistent with the pre-existing `M1.Tests` precedent; the marginal production-metadata exposure is two friend names, no code.

## 6. R2 — PARTIALLY ADDRESSED (technical evidence)

GLM independently assessed the same-project variant approach and the property-removal mechanism:

- **Same-project variant feasibility.** The single `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj` evaluated once per identity is compatible with the inspected configuration; the csproj sets only `TargetFramework=net10.0`, nullable/implicit-using options, `IsPackable=false`, and the `Gagamba.Execution` reference; it does not set `AssemblyName`, `DefineConstants`, `Compile` modifications, or output paths.
- **Static source-item evidence reproduced.** `dotnet msbuild -getItem:Compile -getProperty:DefineConstants` at `Configuration=Release` returned **44 Compile paths** for the production evaluation and **44 identical full paths (zero differences)** for the variant evaluation with `AssemblyName=Gagamba.Execution.Linux.M1.TestFixture` and isolated intermediate/output paths; production `DefineConstants` evaluates to `TRACE;RELEASE`. This is **static source-item evidence only** — no compilation, test, or binary inspection occurred, and it is not proof of completed variant compilation or binary equivalence. The design's honest framing is preserved.
- **Project-reference property-removal mechanism supported.** Removing the six variant-only properties at the M1→`Gagamba.Execution` edge is implementable via `GlobalPropertiesToRemove` / `RemoveProperties` on the `ProjectReference` item (`Microsoft.Common.CurrentVersion.targets` 10.0.401, lines 1044/1073/1102); `BaseOutputPath` is not in the default removal list, so the design's explicit inclusion is necessary and correct. With the six removed, `Gagamba.Execution` evaluates its normal identity and paths; no unexpected reference-assembly path, artifact collision, or identity inconsistency was identified.
- **Source-graph verification adequate.** The documented future verifier collects actual compiler inputs (evaluated `Compile` items and the Csc source list from a build binlog), normalizes tracked sources to canonical Git-root-relative paths, requires bidirectional set equality `P=V` with equal SHA-256 per shared path, records generated inputs, compares effective `DefineConstants` sets, and fails on drift in either direction. Path identity, source-content identity, and compiled-code equivalence are correctly distinguished; the documents do **not** claim compiled-control-flow identity.
- **Production artifact-purity checks sufficient and feasible.** The seven documented checks cover exact production/variant friend sets in emitted M1 metadata, both core friend additions, `D6_4.Tests` resolving the variant and not Owner, Owner resolving the regular M1 identity, production M1 free of the fixture symbol/marker/overload/field/selection branch, disjoint obj/bin/extensions/generated/package paths, and non-packable variant absence from release/publish graphs.
- **Binary-identity limitation consistently stated.** A test-built variant is not binary-identical evidence for the production assembly; the R1-T13–T15 claim is bounded to the typed classifier, the source-verified shared production logic, the fixed stop-result injection, and the real fallback/persistence/aggregation paths exercised through the variant — explicitly excluding binary identity, normal production stop-path execution, Owner integration, native sandbox qualification, end-to-end release qualification, and untested build configurations.

The sole reason R2 is not fully addressed is R2a (section 7).

## 7. R2a — OPEN (sole blocking finding)

**D6.5-R2a — the fixture-symbol `DefineConstants` delivery mechanism is unspecified, and the natural mechanism is demonstrably infeasible.** Affected sections: D6.5 §3 (and the amended D6.4 §4 variant-property paragraph). GLM's evaluation basis:

- (a) A global `DefineConstants` carrying only the symbol evaluates as the symbol **alone**, discarding the SDK's `TRACE;RELEASE` (project assignments cannot override a global property).
- (b) `Microsoft.NET.Sdk.BeforeCommon.targets:290-298` appends the implicit framework constants by target-execution-time assignment, which a global `DefineConstants` also suppresses, so the naive route drops the entire SDK constant set, not just two constants.
- (c) MSBuild splits `Properties`/`-p:` values on `;` (observed `MSB1006`), so a multi-constant union requires `%3B` escaping.

Practical consequence: an implementer following the document as written produces a variant that fails the design's own §5.6 set-equality verifier, or is tempted to weaken the invariant. GLM's smallest required correction: name a working delivery mechanism — for example a dedicated custom global property (e.g. `GagambaD64FixtureDefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE`) passed on the `D6_4.Tests`→M1 reference and appended to `DefineConstants` inside the M1 project via a conditioned assignment or a target running before `CoreCompile`, never by passing `DefineConstants` itself as a global property; and state that the §3 edge-removal list must track the actual variant global properties. This is a documentation amendment within D6.5's scope; no redesign of the fixture or the same-project mechanism is required.

**This preservation does not select or implement the mechanism.** R2a is carried forward as the sole input to D6.6.

Optional non-blocking observations also recorded by GLM (no action required for preservation): runtime name-based friend matching nuance; trailing-slash requirement for `BaseIntermediateOutputPath`/`BaseOutputPath`; shared `Gagamba.Execution` intermediates under concurrent builds; static evaluations not exercising restore/NuGet asset generation; naming the exact reference metadata attribute (`GlobalPropertiesToRemove`, honored as MSBuild task `RemoveProperties`).

## 8. Preserved acceptance and approval boundaries

R1-T13–T15 remain unchanged (future acceptance specifications — not implemented, not executed):

| Test | Fallback | DomainStop | DomainEmpty | Aggregate | Durable bits |
|---|---|---|---|---|---|
| T13 | Empty | Unknown | Confirmed | QuarantinedUnknown | `0x08 → 0x0A` |
| T14 | Populated | Unknown | Unknown | QuarantinedUnknown | `0x08` |
| T15 | Unknown | Unknown | Unknown | QuarantinedUnknown | `0x08` |

The zero-removal assertion depends on the seeded `DomainRemoval` bit routing the removal block to the existing skip path; it does not rest on any assumption that `DomainStop=Unknown` suppresses removal.

Preserved unchanged:

- C1–C4: `PROPOSED - NOT APPROVED`.
- C3: `C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor Policy B is selected.
- No production remediation is authorized.
- No G3 acceptance.
- Release is not qualified.
- H1–H5, WP08, WP10 and portability deferrals unchanged.

Accepted fixture behavior (fixed marker `Kill=NotAttempted`/`Population=NotObserved`/`TimedOut=false`; injection after `RecoveryPlanner.Plan()` and `VerifyIdentity()` only while `DomainStop` is unconfirmed; real `ObservePopulation()` fallback exactly once; `WriteKillCalls=0`; `RemoveChildCalls=0` on the seeded-`DomainRemoval` basis) is retained without regression.

## 9. Evidence and qualification boundaries

No D6.5 production implementation occurred; no tests were executed; no native code was changed; no new source-bound runtime qualification exists. The R2 44/44 identical Compile-path result and `TRACE;RELEASE` evaluation are static source-item evidence, not proof of completed variant compilation or binary equivalence, and are not upgraded here to runtime qualification. Frozen R12 remains `38f6981b2a60daab2df37c92913a6760c1bef131` / tree `6854675d6b37a49d5302d6104103bdbfbc268040`; frozen harness Q3 remains `36a124d7dff98ecfaf956340767fe1768ad5b17f`. No G3 gate is accepted; no release is qualified; no tags, packages, or default-branch changes are created.

## 10. Next task — D6.6

**WP05 D6.6 — MSBuild Fixture-Symbol Delivery Correction** (documentation-only, Luna; Pending — not implemented, approved, or completed). The sole blocking correction (D6.5-R2a) requires:

1. A dedicated custom MSBuild property enabling the fixture variant.
2. Appending `GAGAMBA_WP05_D6_4_TEST_FIXTURE` inside the M1 project.
3. Preserving `TRACE`, `RELEASE` and SDK-generated framework constants.
4. Avoiding a global `DefineConstants` override.
5. Preventing the carrier property from propagating into `Gagamba.Execution`.
6. Specifying verification against actual compiler inputs and final defined constants.
7. A focused independent GLM review afterward.

Do not implement the fixture, change production code, or select C3 policy during D6.6 beyond the documentation correction and its focused review.

## 11. Publication and fresh-clone verification

Before publication, `git ls-remote origin` found no D6.5 producer, GLM review, or preservation ref on origin; all previously published refs were left untouched. No remote publication is claimed by this checkpoint at creation time.

| Ref published by ordinary non-force push | Commit |
|---|---|
| `design/wp05-d6-5-fixture-isolation-equivalence` | `6bd714d894ad8152a87692f749521abf79916bf5` |
| `review/wp05-d6-5-glm-independent-isolation-equivalence` | `1051018c272a578613309c07cb4d23d627216482` |
| `preservation/wp05-d6-5-glm-independent-isolation-equivalence-review` | This checkpoint commit, direct child of Luna's D6.5 commit |

After publication, a fresh clone must verify the three exact remote refs, the exact producer/review/preservation parent bindings, the review-and-preservation sibling relationship, the producer (two documentation files) and review (one review document) and preservation (three documentation files) diff scopes, the GLM review blob `722613ebe1d4b0439e15704fcb203e19a07cd813`, the verbatim Option B disposition, R1 addressed / R2 partially addressed / R2a open, D6.6 pending in the work queue, C1–C4 unapproved and the C3 policy pending, unchanged R12 tree and harness Q3 identities, and `git fsck --full` exit status and output.

**Preservation is evidence custody only. It does not implement R1/R2/R2a, modify the producer or review documents, approve C1-C4, choose C3 policy, authorize production remediation or test implementation, or qualify a release.**
