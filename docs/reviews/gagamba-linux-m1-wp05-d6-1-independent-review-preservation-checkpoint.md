# WP05 D6.1 Independent Review Preservation Checkpoint

Status: **PRESERVATION ONLY - NOTHING APPROVED, NO REMEDIATION AUTHORIZED**

Date: 2026-10-21
Displacing role: preservation and publication engineer (independent of the
producer and of the independent reviewer).

This checkpoint preserves the independent GLM adversarial review of the WP05
D6.1 documentation-only contract-design revision, its exact Git provenance,
its findings and its approval boundaries. It does not modify the D6.1 producer
design, does not resolve any review finding, does not select any policy, and
does not change any qualification scope. Historical status vocabulary:
`SOURCE-BOUND`, `INDEPENDENTLY REVIEWED`, `PRESERVED`, `PUBLISHED`,
`REMOTE VERIFIED`, `PROPOSED`, `PENDING HUMAN`, `NOT QUALIFIED`.

## 1. Preservation identity

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
  (checked out on `implementation/wp05-release-qualification-fixtures`
  `338a98af269e6b5839f3ccc2e0881f016153b01e`; remote
  `https://github.com/jenolaszlo-sketch/gagamba.git`). The canonical worktree,
  its untracked files and all other worktrees were left untouched.
- Preservation worktree: `C:\Users\Laszlos\source\repos\Gagamba-preserve-d61`
  (new disposable worktree; no prior worktree pruned or modified).
- Preservation branch:
  `preservation/wp05-d6-1-independent-contract-design-review`, created
  directly at the D6.1 producer final HEAD
  `750914d93edaf5490b42bf0787f20bb0c5089767` as its immediate parent.
- Preservation scope: one documentation-only commit containing exactly the
  three allowed files: this checkpoint plus `docs/handoff.md` and
  `docs/work-queue.md` coordination entries. The independent review documents
  are NOT copied into this branch; they remain on the review branch and are
  referenced by commit SHAs, blob SHAs and paths.

## 2. Frozen production and harness identities (SOURCE-BOUND, unchanged)

- Frozen production baseline R12:
  `38f6981b2a60daab2df37c92913a6760c1bef131`, production tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`
  (verified: `git rev-parse 38f6981b^{tree}` = this tree; and
  `git merge-base --is-ancestor 38f6981b 750914d` = True).
- Frozen harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f` (exact commit
  resolved locally; unchanged).
- No D6.1 production modification occurred: the complete D6.1 diff (below)
  touches only Markdown documentation.

## 3. Prior checkpoints (PRESERVED, unchanged)

- D5 preservation: `578b2084cfa09be76e73d0fb26e09f8d823f9ae6` on
  `preservation/wp05-d5-independent-native-status-review`.
- D6.0 producer design: `23df1c566e7d8f072d924f374bf6891c726c03fe` on
  `analysis/wp05-d6-0-release-contract-decisions`
  (A `726e081b...` -> B `1e0f126b...` -> C `23df1c56...`).
- D6.0 independent GLM review Q3:
  `2a235c391673258023d3b3058ad7bbb31cc20406` on
  `review/wp05-d6-0-independent-contract-design`.
- D6.0 preservation: `947aebe7df2eefd116cfba8a4b3e1baee2067e0d` on
  `preservation/wp05-d6-0-independent-contract-design-review`.
- None of these identities was modified, rebased or republished.

## 4. D6.1 producer design (design/wp05-d6-1-contract-amendments)

Producer-reported ten commits, all resolved to full local SHAs; the parent
chain was verified commit-by-commit (`git log --format="%h %p ..."` over the
range; each listed parent matched the recorded chain):

1. `d662e0df465bea7302d5aa5747524e820076335b` (parent = D6.0 preservation
   `947aebe7df2eefd116cfba8a4b3e1baee2067e0d`), "revise D6.1 C1 and C2 contract amendments"
2. `d1424c7fc011e1f56e7870650f7050a00b0c4f5e`, "revise D6.1 C3 and C4 contract amendments"
3. `279345303f5bcb3ad4322c3621eadb5e014f8864`, "reconcile D6.1 package amendments and scope"
4. `a345e314c04da4b21d1adb26d4c302aaad5df1ac`, "prepare D6.1 approval and focused review handoff"
5. `ec973ccc403cdcc0bbfe749cf3d95fbcc2f9789d`, "complete D6.1 interaction revision"
6. `a6e092b494e74a84dee9738db2df3c8938f330ab`, "bind D6.1 focused review to design commit"
7. `e63a6d4a18aad87b88f64939194b18dd102ad9bd`, "mark D6.1 C3 contract choice pending"
8. `29b58e2e53613591158fb58773050a803a0970a8`, "align D6.1 C3 policy wording"
9. `bdd711d716fe2bcf0cd06183efc4d36ab030ece4` (the D6.1 design revision that
   adds the clock-epoch corroboration mechanism), "specify D6.1 clock epoch corroboration"
10. `750914d93edaf5490b42bf0787f20bb0c5089767` (final HEAD), "bind D6.1 review to final design commit"

- Branch `design/wp05-d6-1-contract-amendments` locally points at exactly
  `750914d93edaf5490b42bf0787f20bb0c5089767` (verified).
- Ancestry precision (recorded exactly): the D6.0 preservation commit
  `947aebe7...` is an **ancestor** of the D6.1 final HEAD, not its immediate
  parent. The immediate parent of `750914d...` is `bdd711d7...`; the D6.1
  chain begins at `d662e0d...`, whose parent is the D6.0 preservation commit.
  Verified: `git merge-base --is-ancestor 947aebe7 750914d` = True. The GLM
  completion report's use of "parent" for the `947aebe7` -> `750914d`
  relationship is recorded as loose wording; the Git facts above govern.
- Diff scope from the D6.0 preservation base (verified with
  `git diff --numstat 947aebe7 750914d`): exactly **13 Markdown files**,
  **+772 / -2**, with `c3-stop-deadline.md` being the only file containing
  deletions (2 lines, later replaced by corrected C3 wording). No test,
  production source, native, engineering or artifact file changed. For
  reference: 10 files under `docs/decisions/` (three new: D6.1 amendment
  reconciliation, human approval questions, focused review handoff; seven
  existing C1-C4/D6.0 documents amended), plus `docs/handoff.md` (+4),
  `docs/work-queue.md` (+4), and
  `docs/plans/gagamba-linux-m1-wp05-d6-remediation-proposal.md` (+41,
  documentation binding).
- Note on the reported list: handoff/work-queue coordination lines and the
  remediation-proposal binding are part of the 13 documented files. All 13
  are documentation; the reported claim holds.

## 5. Independent GLM D6.1 review (review branch, INDEPENDENTLY REVIEWED)

- Review branch: `review/wp05-d6-1-independent-contract-design-2`
  (worktree `C:\Users\Laszlos\source\repos\Gagamba-review-d61-glm`; clean;
  branch unpublished at review time).
- Local branch tip: `77f559a7dfc1702b33812d5c3d5c42ac925f0343`.
- Resolution of the reported SHAs (all matched their abbreviated identities):
  1. Q1 `ae53323f2e919105cbbc3f14d49858097f3aebec`, parent =
     `750914d93edaf5490b42bf0787f20bb0c5089767` (exact D6.1 final HEAD). "record independent D6.1 contract design review"
  2. Q2 `96dbd49813e6eb5d34511216ce7e7de02f315d38`, parent = Q1.
     "record independent D6.1 amendment matrix and interaction register"
  3. Q3 `77f559a7dfc1702b33812d5c3d5c42ac925f0343`, parent = Q2.
     "record independent D6.1 human approval readiness"
- Additive-only verification (`git diff --numstat 750914d 77f559a`): exactly
  four new files under `docs/reviews/`, **+1112 / -0** (637 + 40 + 208 + 227),
  matching the reported total. No producer contract document
  (`docs/decisions/...`), no implementation or test file was changed.
  Reviewed-file paths verified:
  1. `docs/reviews/gagamba-linux-m1-wp05-d6-1-independent-contract-design-review.md`
     - blob `252f160c8c7da681a5ab337b195469f112ba4cf8`
  2. `docs/reviews/gagamba-linux-m1-wp05-d6-1-amendment-review-matrix.md`
     - blob `517f87a182876f6881a19904d65962b55f9d5695`
  3. `docs/reviews/gagamba-linux-m1-wp05-d6-1-interaction-register.md`
     - blob `09a27340573f486db9e36602eb2c22a8de60e8e4`
  4. `docs/reviews/gagamba-linux-m1-wp05-d6-1-human-approval-readiness.md`
     - blob `0a80789e1a64f6702a40eb13fa2d88809fe716bd`
- Provenance observation (recorded fact, no action): a separate earlier local
  branch `review/wp05-d6-1-independent-contract-design` (worktree
  `gagamba-review-d61`, tip `8aded096f0ed55375c35915378232e95cb8ba33d`) is
  another earlier run of the same reviewer task. The reviewed and published
  independent review artifact is the `-2` branch at `77f559a...`; the earlier
  branch is not published by this preservation and its contents were not
  merged, adopted or evaluated here.
- The independent review read the D6.1 documents at exactly `750914d...` and
  verified applicable statements against the frozen R12 sources
  (`38f6981b...`/`6854675d...`). `DESIGN TEXT ADDRESSED` wording was
  independently recorded as design support, not technical verification.

## 6. Ancestry structure (verified)

```text
D6.0 preservation 947aebe7df2eefd116cfba8a4b3e1baee2067e0d
      |
      +-- d662e0d -> d1424c7 -> 2793453 -> a345e31 -> ec973cc
                          -> a6e092b -> e63a6d4 -> 29b58e2
                          -> bdd711d -> 750914d  (D6.1 final HEAD)
                                    |
                                    +-- ae53323 (GLM Q1)
                                    |     +-- 96dbd49 (GLM Q2)
                                    |             +-- 77f559a (GLM Q3)
                                    |
                                    +-- <preservation commit>  (this branch)
```

- GLM Q1 is a direct child of `750914d`; Q2 a direct child of Q1; Q3 a direct
  child of Q2 (verified parent SHAs).
- The preservation commit's immediate parent is exactly
  `750914d93edaf5490b42bf0787f20bb0c5089767`; it does not descend from GLM
  Q3. The GLM review and this preservation history are separate sibling
  histories; no merge and no cherry-pick connects them.
- `947aebe7...` is an ancestor of the D6.1 final HEAD (True), and also an
  ancestor of GLM Q3 (True, via the producer chain).
- The adversarial required-changes commit `e4b0a798e944ddfb144901c63b0c444e7980533b`
  is NOT an ancestor of `750914d...` or `947aebe7...` (both checks False).

## 7. Original adversarial review provenance (unchanged)

- Branch: `review/wp05-release-plan-adversarial-review`; commit
  `e4b0a798e944ddfb144901c63b0c444e7980533b`; required-changes blob
  `73eb09af27c5dbc896739ebf48489862ec13e0a9` verified at
  `docs/reviews/gagamba-linux-m1-wp05-release-plan-required-changes.md`.
- That branch is not an ancestor of the D6.0/D6.1 producer lineage; it is
  preserved through its published ref and checkpoint references (see the D6.0
  preservation checkpoint and the plan-review preservation entry). It was not
  merged or cherry-picked into any producer or preservation history.

## 8. Independent verdict (Option B, preserved verbatim)

Disposition line from
`docs/reviews/gagamba-linux-m1-wp05-d6-1-independent-contract-design-review.md`
(blob `252f160c8c7da681a5ab337b195469f112ba4cf8`, section 12):

`WP05 D6.1 INDEPENDENT DESIGN REVIEW COMPLETE - OPTION B: DESIGN SUPPORTED WITH SPECIFIC REQUIRED AMENDMENTS; HUMAN CONTRACT DECISIONS HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED`

Preserved meaning: the review found no structural contract contradiction
requiring redesign, but four precision requirements remain open (R1-R4);
the favorable C2/C4 findings remain review support, not approvals; C3 Policy
A was found technically supportable and is NOT selected; documentation-only
work is not completed remediation.

## 9. Amendment closure results (fifteen amendment rows + the C3 policy row)

Authoritative source: the independent amendment-review matrix (blob
`517f87a182876f6881a19904d65962b55f9d5695`). Reviewed dispositions, preserved
without strengthening:

| Row | Disposition | Maps to |
|---|---|---|
| A-C1-1 | PARTIALLY ADDRESSED | D6.1-R3 |
| A-C1-2 | VERIFIED ADDRESSED (substantive design), with a separate evidence-provenance defect | D6.1-R4 |
| A-C1-3 | VERIFIED ADDRESSED | none |
| A-C2-1 | VERIFIED ADDRESSED | none |
| A-C2-2 | VERIFIED ADDRESSED | none |
| A-C2-3 | VERIFIED ADDRESSED | none |
| A-C3-1 | PARTIALLY ADDRESSED | D6.1-R2 |
| A-C3-2 | PARTIALLY ADDRESSED | D6.1-R1 |
| A-C3-3 | VERIFIED ADDRESSED | none |
| A-C3-4 | VERIFIED ADDRESSED | none |
| A-C4-1 | VERIFIED ADDRESSED | none |
| A-PKG-1 | VERIFIED ADDRESSED | none |
| A-PKG-2 | VERIFIED ADDRESSED | none |
| A-PKG-3 | VERIFIED ADDRESSED | none |
| A-PKG-4 | VERIFIED ADDRESSED (satisfied - no change) | none |
| C3 zero-wait policy | OPEN - HUMAN POLICY (PRESERVED) | human holder |

Eleven of fifteen rows are independently verified as addressed; A-C1-2 is
verified for its substantive design while its test-inventory citation carries
the separate D6.1-R4 provenance defect; three rows (A-C1-1, A-C3-1, A-C3-2)
are partially addressed. No row is NOT ADDRESSED, and none is CONTRADICTED BY
SOURCE.

## 10. New reviewer requirements D6.1-R1 through R4 (OPEN; carried without alteration)

- **`D6.1-R1 OPEN - C3 STRUCTURED OUTCOME DISCRIMINATION REQUIRED`**
  Affected contract: C3. Finding (review main document, section 10; row
  A-C3-2): the proposed Reconciler contract does not identify a sufficient
  mechanism to distinguish actual kill-write failure, stop attempted with
  completion unconfirmed, population observation failure, confirmed-empty
  domain and reconciliation operation failure; the design must not rely on
  interpreting human-readable error strings. A finalization aggregation issue
  around `Reconciler.cs:465-471` (final persistence failure currently yields
  an aggregate Failed where the declared rule is Unknown) must be named in the
  corrected mapping. Feedback cited at R12 (`DomainLease.cs:11-18/108-143`
  StopOutcome collapse; `Reconciler.cs:232-236/429-436/437-442/450-459/465-471`).
  Future correction: state how component outcomes map to aggregate results,
  including the finalization path. This is a contract-design amendment, not an
  implemented fix; no API shape is chosen here.
- **`D6.1-R2 OPEN - C3 STRUCTURAL CLOCK EPOCH CORROBORATION REQUIRED`**
  Affected contract: C3. Finding (main review section 10; row A-C3-1): the
  `MonotonicEpoch.Qpc` label proposed by D6.1 commit `bdd711d7...` is
  convention-based, not structurally corroborated; a shared enum name alone
  does not prove timestamp comparability. The next design must bind the
  timestamp contract to the actual trusted primitive - `Stopwatch.GetTimestamp()`
  and `Stopwatch.Frequency` with consistent origin and units - with real
  compatibility behavior, explicit rejection of incompatible epoch values and
  a negative fixture proving an incompatible clock fails closed. Independently
  confirmed source facts preserved: `OwnerClock` is QPC-based
  (`OwnerPreparationTransaction.cs:21-32`); `LaunchDomainClock` uses a
  compatible source (`BubblewrapAdapter.cs:510-521`); `OwnerDomainClock` is
  construction-relative and incompatible (`OwnerPreparationTransaction.cs:34-39`);
  and the incompatible `OwnerDomainClock` implementation has no identified
  production caller in the reviewed tree (dead code). No clock implementation
  is modified; the proposed clock contract is not qualified by this statement.
- **`D6.1-R3 OPEN - C1 AMBIGUOUS RETENTION COLLISION RULE REQUIRED`**
  Affected contract: C1, interacting with C2 and C4. Finding (main review
  sections 3/10; interaction register Pair 1): the proposed lock sequence is
  implementable and establishes no new `_gate`/`_journalGate` inversion, but
  the design does not specify the case where stop acceptance wins before the
  ambiguous release result becomes retained and visible (window between
  `ReleaseArbiter.cs:115` and the retention block at `:120-131`, where
  stop-won callers retain first and an ambiguous caller may subsequently
  receive a Duplicate of the stop-won refusal while the D6.1 text states
  ambiguous retention unconditionally). This is a missing ordering and
  returned-result rule, not a demonstrated second physical release. The
  counterexample sequence is preserved in the interaction register (blob
  `09a27340573f486db9e36602eb2c22a8de60e8e4`, Pair 1) and must be carried
  forward. Future amendment must specify: stop-winning ordering; ambiguous
  retention timing; results returned to competing callers; capability
  consumption semantics; which decision becomes authoritative for later
  Duplicate calls; no second FD4; deterministic opposing-order probe
  expectations. The collision policy itself is not chosen during preservation.
- **`D6.1-R4 OPEN - C1 FIXTURE SOURCE PROVENANCE CORRECTION REQUIRED`**
  Affected contract: C1, with supporting evidence references. Finding (main
  review section 10; row A-C1-2): the D6.1 documentation cites
  `Wp05ReleaseContractProbes` as though the fixture were present in the
  reviewed frozen R12 production tree, but the fixture belongs to a separate
  test/harness lineage: the implementation fixtures branch
  `implementation/wp05-release-qualification-fixtures` at D2 checkout
  `dc4c372...` (test-only, `src/` byte-identical to R12; probe file
  `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs`,
  bound in the D1-D3/D4/D5 evidence and `SHA256SUMS` artifacts). This is a
  provenance defect, not a newly established runtime defect. Future
  amendment: identify the exact fixture source (branch, commit SHA, file
  path, test symbol) and distinguish production source evidence from
  test-only diagnostic evidence in the inventory. Test files are not copied
  into the R12 source history; no ancestry relationship is invented.

## 11. Contract-level verdicts (all four PROPOSED - NOT APPROVED)

| Contract | GLM verdict | Remaining action |
|---|---|---|
| C1 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | D6.1-R3 and D6.1-R4 |
| C2 | DESIGN SUPPORTED FOR HUMAN DECISION | Human approval pending |
| C3 | CONTRACT CHOICE REQUIRED | D6.1-R1/R2 plus human expired-deadline policy choice |
| C4 | DESIGN SUPPORTED FOR HUMAN DECISION | Human approval pending |

Distinct preserved gate layers (none collapsed): engineering amendment
readiness; independent design-review verdict; human contract approval
(`PROPOSED - NOT APPROVED` for all); production implementation authorization;
runtime qualification; release qualification. C3 Policy A (expired deadline
-> identity-checked kill + one immediate population observation) was found
technically supportable against R12 mechanics (`Reconciler.cs:231`;
`DomainLease.cs:108-143`) and is NOT selected; the policy status remains a
human choice with Policy B (separately specified bounded cleanup/observation
interval) as the recorded alternative.

## 12. Critical technical findings (preserved without strengthening)

- **C1** (main review sections 3/8; matrix rows): the proposed lock sequence
  can avoid prohibited lock inversion; the ambiguity contract remains
  source-consistent; the retained result vocabulary (`AmbiguousPersistence`,
  `MayHaveExecuted`, `NotAttempted`, `DurableCommitted=false`) is viable
  against existing R12 vocabulary; no second FD4 path was established by the
  reviewed design. The stop-before-retention collision remains under-specified
  (D6.1-R3); C1 is therefore not reported as fully ready for human approval.
- **C2** (main review sections 4/8; matrix rows): all three stop-acceptance
  paths admit the proposed capability consumption
  (`ReleaseArbiter.cs:226-297` normal/clock-unavailable/overflow); the
  capability is single-use and store-local; duplicate stop does not re-consume
  (`:232-235`); release-first history is preserved; `ReleaseAlreadyCommitted`
  covers the consume-before-transport window and is not proof that transport
  succeeded. Supported for human decision; not implemented, not approved.
- **C3** (main review sections 5/8; matrix rows): persisted deadline
  immutability (`OwnershipValidation.cs:145-175`), the recorded clock mismatch
  and corroboration need, no-reset deadline derivation
  (`remaining = max(0, StopDeadlineNanoseconds - corroborated now)`), the
  no-StopRecord cleanup-only distinction, technical feasibility of expired
  deadline Policy A, and already-empty Option B consistency
  (`Reconciler.cs:217-248`) are independently confirmed. The clock-proof and
  structured outcome-discrimination contracts remain incomplete (D6.1-R1/R2);
  the expired-deadline policy remains a human choice. C3 is not approved.
- **C4** (main review sections 7/8; matrix row): concurrent loser refusal,
  sequential Duplicate semantics, retention asymmetry, exact R12 reason
  strings (verified verbatim at `ReleaseArbiter.cs:59/78/86/93/99/107/113/130/144/153`,
  `OwnershipTransitions.cs:23/29`, relabels `:52/:122`), and no production
  `ReleaseDecisionKind` switch consumers in the reviewed scope were
  independently confirmed. Supported for human decision; not implemented,
  not approved.

## 13. Interaction review (six pairs)

Authoritative source: the interaction register (blob
`09a27340573f486db9e36602eb2c22a8de60e8e4`). Verdicts preserved:

| Pair | Status |
|---|---|
| C1 x C2 | Supported; retention-collision ordering UNRESOLVED-BY-TEXT -> D6.1-R3; capability reuse race RULED OUT |
| C1 x C3 | Supported; fabricated-deadline attempt RULED OUT (validator forbids deadline <= 0 / non-equal deadline) |
| C1 x C4 | Supported; retry-to-second-write RULED OUT (sealed journal makes forward paths unreachable); NotCommitted-shape refusal documented as an observation; optional retry-futility note folded into D6.1-R4 tail |
| C2 x C3 | Supported; authority transfer attempt RULED OUT |
| C2 x C4 | Supported; history-rewrite attempt RULED OUT |
| C3 x C4 | Supported; window-reset attempt RULED OUT |

No structural cross-contract redesign requirement was found. The C1 retention
collision is a documentation-level semantic gap and remains open under
D6.1-R3; it must not be collapsed into a harmless concurrency note. No new
durable journal record or schema change was recommended by any interaction.

## 14. Qualification boundaries (unchanged, explicitly preserved)

- R12 production baseline `38f6981b...`/`6854675d...`, OwnershipSnapshot v3,
  the FD4 and FD5 protocols, frozen harness Q3 `36a124d7...`, D1-D3
  diagnostic tests, D4 supervisor and crash evidence, D5 native FD5 evidence,
  and existing source-bound qualification limits remain unchanged. No D6.1
  production implementation occurred; no new qualification run was performed
  by the review or this preservation; the independent review is design
  evidence only.
- Component composition: `ReleaseArbiter` and `Reconciler` retain zero
  production owner call sites in the reviewed scope; their current contract
  evidence remains component-level. WP08 owner orchestration is not yet
  qualified; WP10 target/status integration remains separate; no full G3
  qualification is established. Explicit statement:
  **Component design readiness does not qualify the integrated sandbox lifecycle.**

## 15. H1-H5 and other deferrals (carried forward unchanged from D5/D6.0)

- **H1**: production status integration remains unwired, including D5-O05.
- **H2**: destructive inherited-descriptor, wrong-FD and helper-writer FD5
  cases remain unclaimed.
- **H3**: additional raw wait4/pidfd SIGKILL witness remains optional under
  the bounded accepted witness profile.
- **H4**: C1-C4 remain subject to human decisions (preserved in sections 9-11).
- **H5**: the D5 historical plan/Git ordering note remains preserved.
- Also deferred, unchanged: WP08 owner composition; WP10 status/target
  integration; full G3 acceptance; portability expansions (Windows/macOS
  enforcement); and any qualification not explicitly supported by previous
  probes. No new claims were created for these items.

## 16. Authorization boundary

This preservation approves no contract, selects no C3 policy, accepts no
gate, authorizes no production remediation, and does not change the meaning
of any historical qualification record. Documentation-only and Git-integrity
work does not make D6.1 `QUALIFIED`; D6.1 remains a proposed design contract
package plus an independent design review.

## 17. Next design/review sequence

1. D6.1 preservation (this checkpoint; local) and remote publication.
2. D6.2 documentation-only DeepSeek revision folding D6.1-R1 through D6.1-R4
   only (one narrowly scoped documentation change).
3. Focused independent GLM re-review of the folded rows only.
4. Individual human C1-C4 contract decisions and the separate C3
   expired-deadline policy decision (four questions; none answerable by
   reviews; C3 policy answer gates the C3 contract approval).
5. Separately authorized production remediation (not authorized by anything
   above).
6. Source-bound qualification.

## 18. Remote publication and independent verification instructions

- Publish exactly three refs, each with an ordinary non-force push:
  1. `design/wp05-d6-1-contract-amendments` -> remote tip expected
     `750914d93edaf5490b42bf0787f20bb0c5089767`
  2. `review/wp05-d6-1-independent-contract-design-2` -> remote tip expected
     `77f559a7dfc1702b33812d5c3d5c42ac925f0343`
  3. `preservation/wp05-d6-1-independent-contract-design-review` -> remote
     tip expected at the new preservation commit (immediate parent
     `750914d...`).
- Before every push: verify the remote URL
  (`https://github.com/jenolaszlo-sketch/gagamba.git`), the local branch tip
  and the destination ref; if the destination already exists at the exact
  expected commit it is already preserved; if it exists at a different
  commit, stop and report the conflict instead of reconciling.
- After publication, verify from a fresh disposable clone (not the existing
  worktrees): all three refs exist at the recorded SHAs; the producer chain
  is ten commits from `947aebe7...` with each parent matching section 4; GLM
  Q1-Q3 parentage as in sections 5-6; the preservation commit is a direct
  child of `750914d...` and a sibling not descendant of Q3; the four review
  blobs equal the section 5 identities; D6.1 changed only the thirteen
  documented files; the review added only the four review documents; the
  preservation commit changed only the three allowed documents; frozen R12
  source and harness identities remain unchanged; original D6.0 refs remain
  intact; the required-changes blob `73eb09af...` remains reachable from its
  published branch `review/wp05-release-plan-adversarial-review`; run
  `git fsck --full` and record its result; no release, tag or package
  operation occurred.
- The remote-verification facts of this preservation are recorded in the
  preservation report issued after publication, not inside this commit; this
  commit therefore records local `PRESERVED` status only and must not be read
  as a remote-verification claim.
