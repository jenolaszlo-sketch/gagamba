# WP05 D6.6 GLM Independent Review Preservation Checkpoint

Date: 2026-10-21 (Asia/Manila)
Status: **PRESERVED AFTER VERIFICATION — OPTION A; D6.5-R2a VERIFIED ADDRESSED IN DESIGN; D6.4-R1 AND D6.4-R2 DESIGN FINDINGS CLOSED; C1-C4 HUMAN DECISIONS AND C3 POLICY SELECTION PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

This checkpoint records evidence custody only. It does not implement, remediate, or qualify anything, and it selects no C3 policy and approves no contract.

## 1. Repository and frozen evidence identity

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Remote: `https://github.com/jenolaszlo-sketch/gagamba.git`
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`; source tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen WP05 harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`; tree `f764722997130f06c8a62c45a4230805aa538ea5`.
- D6.5 preservation base: `preservation/wp05-d6-5-glm-independent-isolation-equivalence-review` at `114801e4f2b698fb3d82496b2a9926220b206f4c` — the direct parent of the D6.6 producer commit.

All frozen identities above were re-verified (`git cat-file -t` / `git rev-parse` / `git ls-tree`) before any documentation was written or published. R12 tree and harness Q3 are unchanged and not reopened by D6.6.

## 2. Producer, review, and preservation bindings

| Role | Branch | Commit | Immediate parent |
|---|---|---|---|
| Luna D6.6 producer | `design/wp05-d6-6-fixture-symbol-delivery` | `4bcdac8ac0941f6ec70fb401db7f204f38a5770e` | `114801e4f2b698fb3d82496b2a9926220b206f4c` |
| GLM independent review | `review/wp05-d6-6-glm-independent-symbol-delivery` | `3c3d336c0a47ccb18627a5571298df8d0d1f0683` | `4bcdac8ac0941f6ec70fb401db7f204f38a5770e` |
| Preservation | `preservation/wp05-d6-6-glm-independent-symbol-delivery-review` | This checkpoint commit (`git rev-parse HEAD` on this branch) | `4bcdac8ac0941f6ec70fb401db7f204f38a5770e` |

- GLM review document: `docs/reviews/gagamba-linux-m1-wp05-d6-6-glm-independent-symbol-delivery-review.md`.
- Expected and verified blob: `0dbe1ab4cab9b5efc09cf15983f36fc61c11c3c2`.
- The review commit adds exactly that one document; it does not modify the producer documents.

Required graph, verified by commit parents and ancestry checks:

```text
114801e4  D6.5 preservation
    │
4bcdac8   Luna D6.6 producer
    │
    +--- 3c3d336   GLM review
    │
    +--- NEW        D6.6 preservation
```

`4bcdac8` is the immediate parent of both GLM's review and this preservation checkpoint; the review and preservation are separate sibling histories, and neither sibling tip is an ancestor of the other. The producer's parent `114801e4` is its exact immediate parent (the D6.5 preservation, itself the direct child of the D6.5 producer `6bd714d894ad8152a87692f749521abf79916bf5`). No merge, rebase, cherry-pick, or amend connects the review and preservation histories; GLM's review is referenced only through the exact commit and blob identities above.

## 3. Scope, diff, and worktree safety

- Luna's producer diff from `114801e4` contains exactly three documentation files: `docs/decisions/gagamba-linux-m1-wp05-d6-4-r1-fixture-clarification.md` (modified), `docs/decisions/gagamba-linux-m1-wp05-d6-5-fixture-isolation-equivalence.md` (modified), `docs/decisions/gagamba-linux-m1-wp05-d6-6-fixture-symbol-delivery.md` (added). Documentation-only: no source, test, native, CI, package, or durable-schema change.
- GLM's review diff from `4bcdac8` contains exactly the single review document named in section 2. No other file is touched.
- The preservation diff is restricted to this checkpoint, `docs/handoff.md`, and `docs/work-queue.md`. No source, test, native, package, schema, CI, harness, or evidence artifact is changed. The producer's three D6.4/D6.5/D6.6 design documents and GLM's review document are not modified.
- Luna's producer worktree `C:\Users\Laszlos\source\repos\Gagamba-d6-6` was on the exact producer commit `4bcdac8`; GLM's reviewer worktree `C:\Users\Laszlos\source\repos\Gagamba-review-d66-glm` was on the exact review commit `3c3d336`. Neither was modified by this preservation.
- The canonical checkout remained on `implementation/wp05-release-qualification-fixtures` at `338a98af269e6b5839f3ccc2e0881f016153b01e` with its pre-existing unrelated untracked files (trailing redirection artifacts, `NEM-INDEPENDENT-REVIEW.md`, `spikes/*` scripts, probes and test-output directories, and `tests/*/TestResults/` directories). They were preserved untouched; no worktree was reset, rebased, amended, pruned, or deleted.
- No conflicting `preservation/wp05-d6-6*` branch or `Gagamba-preserve-d66` worktree existed before creation.

## 4. GLM's exact independent disposition

GLM's committed review reports, verbatim (as the disposition in the review header and again in its final section):

**WP05 D6.6 GLM INDEPENDENT REVIEW COMPLETE - OPTION A: D6.5-R2a VERIFIED ADDRESSED IN DESIGN; D6.4-R1 AND D6.4-R2 DESIGN FINDINGS CLOSED; C1-C4 READY FOR INDIVIDUAL HUMAN CONTRACT DECISIONS; C3 POLICY SELECTION PENDING; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

The finding statuses, preserved separately:

- **D6.5-R2a: VERIFIED ADDRESSED (design).** The proposed carrier-property delivery, M1-local conditioned append, and edge property isolation are mutually consistent, source-grounded against the installed .NET SDK 10.0.401 targets, and preserve the invariant **Variant compiler symbols = Production compiler symbols + exactly one fixture symbol**.
- **D6.4-R1: closed in design** (verified addressed; not reopened).
- **D6.4-R2: closed in design** — its sole remaining cause, D6.5-R2a, is verified addressed; the same-project variant strategy, identical-source requirements, production-purity checks, and the non-binary-identical evidence limitation remain supported.
- **Blocking findings: none.** All three R2a infeasibility mechanisms — (a) global `DefineConstants` replacing configuration constants, (b) suppression of target-execution-time implicit constants, (c) semicolon splitting in `Properties`/`-p:` — are each eliminated by the specified design.

## 5. Independently supported design claims (R2a closure evidence)

From the committed review, verified against the pinned project files, the installed SDK 10.0.401 targets, and non-mutating evaluations at `4bcdac8`:

- **Dedicated carrier property.** `GagambaWp05D64FixtureEnabled` has no SDK meaning, no default assignment anywhere in the repository or installed SDK, and one proposed consumer (the conditioned M1 PropertyGroup). The condition is the exact string comparison `== 'true'`; unset, empty, `false`, `TRUE`, or `1` all fail closed. Production M1, Owner, and core evaluations cannot define it.
- **M1-local append of the fixture symbol.** A conditioned `PropertyGroup` inside `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`, placed immediately after the existing unconditional PropertyGroup and before its ProjectReference ItemGroup, appends the single literal `GAGAMBA_WP05_D6_4_TEST_FIXTURE` to `$(DefineConstants)` once per evaluation; no caller-controlled text; no arbitrary symbol-injection interface.
- **Existing and SDK-generated compiler constants preserved by the specified ordering.** TRACE is established in the SDK C# props phase before the M1 body; `Microsoft.NET.Sdk.CSharp.targets` appends the configuration symbol (`RELEASE`); `AddImplicitDefineConstants` (`AfterTargets="PrepareForBuild"`) appends the implicit framework/platform constants during target execution before `CoreCompile` consumes the accumulated value. Every post-body mutation in the installed targets is an append (or the inert opt-in `_DisableDiagnosticTracing` item round-trip), so V = P ∪ {GAGAMBA_WP05_D6_4_TEST_FIXTURE} holds by construction; the evaluated-order nuance is irrelevant because the invariant is set-based.
- **No global `DefineConstants` replacement.** The design forbids supplying `DefineConstants` as a project-reference property; the D6.4/D6.5 amendments carry the matching prohibition; `DefineConstants` removal at the core edge remains defense in depth.
- **Carrier removal at the M1-to-core project-reference boundary.** `GlobalPropertiesToRemove` (MSBuild task `RemoveProperties`) on the M1→`Gagamba.Execution` ProjectReference preserves the six previously established removals and adds the carrier; the SDK leading-semicolon concatenation parses it correctly; the TFM-negotiation child invocation honors the same metadata; Owner references the regular M1 plainly with no path to the variant.
- **Actual compiler-input verification specified for future implementation.** The design requires the final `DefineConstants` input of each actual Csc task (binlog, scoped by project instance) or the exact compiler command/response-file argument consumed by Csc, reconciled with the task input, and explicitly rejects `dotnet msbuild -getProperty:DefineConstants` as a symbol oracle because `AddImplicitDefineConstants` executes after evaluation.
- **Verified non-mutating reproductions.** Base M1 evaluates `DefineConstants=TRACE;RELEASE`, `AssemblyName=Gagamba.Execution.Linux.M1`, `BaseOutputPath=bin\`, `BaseIntermediateOutputPath=obj\`; `Gagamba.Execution` evaluates `TRACE;RELEASE`, `Gagamba.Execution`, `bin\Release\net10.0\`, `obj\Release\net10.0\`, with `MSBuildProjectExtensionsPath` under `src\Gagamba.Execution\obj\` — reproducing the documents' reported values exactly. Reference-metadata delivery class was verified by evaluating M1 with a global `-p:GagambaWp05D64FixtureEnabled=true` (non-mutating); the conditioned group does not exist yet, so the evaluation correctly returned the unchanged value — the mechanism is deliverable, and its effect is a future implementation check.

## 6. Evidence and qualification limitations (preserved exactly)

- The review is documentation-only: it made no source, project, or build change; ran no build, test, or compilation; and its only commit is the review document.
- Checks 1–3 of the review's isolation section (carrier presence/absence in child evaluations under a real project-reference build) are **documented future checks** and were not executed; checks 4–6 and the core `DefineConstants`/paths evaluations were performed statically and non-mutatingly at `4bcdac8`.
- The carrier property, project-reference metadata, conditioned M1 append, Csc invocation, build, fixture compilation, tests, binlogs, and binary inspection are not implemented and not executed; **no executed compiler verification is claimed**.
- The equivalence invariant is a set-based **design claim**; its runtime falsifier remains the future exactly-once/set-difference compiler-input check.
- A test-built variant remains **not binary-identical production evidence**; behavioral conclusions remain bounded accordingly.

## 7. Optional non-blocking observations (five, preserved)

1. **SDK pinning.** The ordering verification is scoped to installed SDK 10.0.401; the repository has no `global.json`. A future qualification should pin or re-verify; the future §5 checks would catch symbol-set drift regardless.
2. **Carrier casing.** Keep the documented lowercase `true`; `TRUE` or `1` fails closed and would be caught by the exactly-once check.
3. **`_DisableDiagnosticTracing`.** The only non-append post-evaluation SDK mutation; opt-in, no default in this repository; if ever enabled it strips `TRACE` and the production-retention check would flag it. No action needed for this design.
4. **Trailing slashes.** Variant `BaseIntermediateOutputPath`/`BaseOutputPath` values must end with a trailing separator; the documented core paths already use the correct form.
5. **Metadata concatenation detail.** The documented `GlobalPropertiesToRemove` value without a trailing semicolon is safe (the SDK default list carries a leading semicolon); keep the semicolon-separated form on future edits.

## 8. Preserved acceptance and approval boundaries

R1-T13–T15 remain unchanged (future acceptance specifications — not implemented, not executed):

| Test | Fallback | DomainStop | DomainEmpty | Aggregate | Durable bits |
|---|---|---|---|---|---|
| R1-T13 | Empty | Unknown | Confirmed | QuarantinedUnknown | `0x08 → 0x0A` |
| R1-T14 | Populated | Unknown | Unknown | QuarantinedUnknown | `0x08` |
| R1-T15 | Unknown | Unknown | Unknown | QuarantinedUnknown | `0x08` |

Preserved unchanged:

- C1–C4: `PROPOSED - NOT APPROVED`; now **ready for individual human contract decisions**.
- C3: `C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor Policy B is selected.
- No production remediation is authorized.
- No G3 acceptance.
- Release is not qualified.
- H1–H5, WP08, WP10, full G3 and portability deferrals unchanged.
- Accepted fixture behavior (fixed marker `Kill=NotAttempted`/`Population=NotObserved`/`TimedOut=false`; injection after `RecoveryPlanner.Plan()` and `VerifyIdentity()`; real `ObservePopulation()` fallback exactly once; `WriteKillCalls=0`; zero removal calls through the seeded `DomainRemoval`; existing journal semantics; no fabricated cleanup confirmation) is retained without regression.

## 9. Evidence and qualification boundaries

No D6.6 production implementation occurred; no tests were executed; no native code was changed; no new source-bound runtime qualification exists. All D6.5/D6.6 static evaluations (`TRACE;RELEASE`; identity and path values; the carrier delivery-class evaluation) remain non-mutating property/item evidence, not proof of completed variant compilation or binary equivalence, and are not upgraded here to runtime qualification. Frozen R12 remains `38f6981b2a60daab2df37c92913a6760c1bef131` / tree `6854675d6b37a49d5302d6104103bdbfbc268040`; frozen harness Q3 remains `36a124d7dff98ecfaf956340767fe1768ad5b17f` / tree `f764722997130f06c8a62c45a4230805aa538ea5`. No G3 gate is accepted; no release is qualified; no tags, packages, or default-branch changes are created.

## 10. Next task — human decisions

With R2a verified addressed in design, the D6.4–D6.6 fixture-design review cycle is closed **in design only**. The next stage belongs to humans:

1. **Individual human decisions on C1–C4** (all remain `PROPOSED - NOT APPROVED`).
2. **Separate C3 policy selection:** Policy A or Policy B for the expired-deadline zero-wait behavior (`C3 ZERO-WAIT POLICY - PENDING HUMAN`).

Only after those decisions, and only if separately authorized: production remediation and source-bound qualification. **Implementation is not authorized. Runtime qualification is outstanding.** H1–H5, WP08, WP10, full G3 and portability remain deferred.

## 11. Publication and fresh-clone verification

Before publication, `git ls-remote origin` found no D6.6 producer, GLM review, or preservation ref on origin; all previously published refs were left untouched. No remote publication is claimed by this checkpoint at creation time.

| Ref published by ordinary non-force push | Commit |
|---|---|
| `design/wp05-d6-6-fixture-symbol-delivery` | `4bcdac8ac0941f6ec70fb401db7f204f38a5770e` |
| `review/wp05-d6-6-glm-independent-symbol-delivery` | `3c3d336c0a47ccb18627a5571298df8d0d1f0683` |
| `preservation/wp05-d6-6-glm-independent-symbol-delivery-review` | This checkpoint commit, direct child of Luna's D6.6 commit |

After publication, a fresh clone must verify the three exact remote refs, the exact producer/review/preservation parent bindings, the review-and-preservation sibling relationship (both direct children of the D6.6 producer; neither descends from the other), the producer (three documentation files), review (one review document) and preservation (three authorized documentation files) diff scopes, the GLM review blob `0dbe1ab4cab9b5efc09cf15983f36fc61c11c3c2`, the verbatim Option A disposition, R1/R2/R2a closed in design only, C1–C4 `PROPOSED - NOT APPROVED`, C3 `C3 ZERO-WAIT POLICY - PENDING HUMAN`, unchanged R12 tree and harness Q3 identities, no production remediation/G3 acceptance/release qualification claimed, and `git fsck --full` exit status and output.

**Preservation is evidence custody only. It does not implement the fixture-symbol delivery, modify the producer or review documents, approve C1-C4, select C3 policy, authorize production remediation or test implementation, or qualify a release.**
