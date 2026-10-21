# WP05 D6.3 Terra Independent Review Preservation Checkpoint

Date: 2026-10-21 (Asia/Manila)
Status: **PRESERVED FOR HUMAN DECISION — OPTION B; NO CONTRACT APPROVED**

## 1. Repository and source identity

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Remote: `https://github.com/jenolaszlo-sketch/gagamba.git`
- Frozen R12 production: `38f6981b2a60daab2df37c92913a6760c1bef131`; source tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Frozen WP05 harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D6.2 preservation: `preservation/wp05-d6-2-focused-independent-review` at `31132513a983bb37cfd609f56541f6ab8d44af84`.
- GLM D6.2 review Q3: `3bce34b6b449a863f784949c0d7be5b0b56d4458`; finding D6.2-R1 is in `docs/reviews/gagamba-linux-m1-wp05-d6-2-focused-independent-review.md`, section 5.4.

## 2. Producer, review, and preservation bindings

| Role | Branch | Commit | Parent |
|---|---|---|---|
| Luna D6.3 producer | `design/wp05-d6-3-c3-outcome-correction` | `4209581a03562b434badd7a5f9927663559911be` | `31132513a983bb37cfd609f56541f6ab8d44af84` |
| Terra independent review | `review/wp05-d6-3-terra-independent-c3-outcome` | `cda2f22221c4db12ced3a4317362e80167270a63` | `4209581a03562b434badd7a5f9927663559911be` |
| Preservation | `preservation/wp05-d6-3-terra-independent-c3-review` | This checkpoint commit (`git rev-parse HEAD` on this branch) | `4209581a03562b434badd7a5f9927663559911be` |

Terra's exact review file is `docs/reviews/gagamba-linux-m1-wp05-d6-3-terra-independent-c3-review.md`; blob `3bb7fd2322670d9f9977004661ab2a72760798d4`. The review commit adds only that file. The preservation commit is a direct child of Luna's producer commit and is not based on Terra's review.

Required graph, verified by commit parents and ancestry checks:

```text
31132513 D6.2 preservation
  └─ 4209581a Luna D6.3 producer
       ├─ cda2f222 Terra review
       └─ [this preservation commit]
```

`31132513` is an ancestor of Luna's commit; Luna's commit is the immediate parent of both Terra's review and this preservation checkpoint. Neither sibling is an ancestor of the other.

## 3. Scope, diff, and worktree safety

- Luna's diff from D6.2 preservation contains exactly `docs/decisions/wp05-d6-0-c3-stop-deadline.md` and `docs/decisions/gagamba-linux-m1-wp05-d6-3-c3-outcome-correction.md`. It is documentation-only.
- Terra's diff from Luna contains exactly the independent review document named above. Its worktree was clean before preservation; its branch was absent from the remote at pre-publication inspection.
- The preservation diff is restricted to this checkpoint, `docs/handoff.md`, and `docs/work-queue.md`. No source, test, native, package, schema, CI, harness, or evidence artifact is changed.
- Luna's producer worktree `C:\Users\Laszlos\source\repos\Gagamba-d6-3` was clean at the producer SHA. Terra's reviewer worktree `C:\Users\Laszlos\source\repos\Gagamba-review-d63-terra` was clean at the review SHA.
- The canonical checkout remained on `implementation/wp05-release-qualification-fixtures` at `338a98af269e6b5839f3ccc2e0881f016153b01e` with its pre-existing untracked files listed below. They were preserved untouched:
  - `-`
  - `"; done; grep -c M1.Tests.ReleaseArbiterTests _LAPTOP-KBD8D8JG_2026-10-18_10_10_39.trx"`
  - `spikes/Gw1bLaunch.Tests/TestResults/`
  - `spikes/cgexec/`, `spikes/cgspawn/`, `spikes/pspawn/`
  - `spikes/strace-clone.sh`, `spikes/strace-grep.sh`, `spikes/strace-owner.sh`, `spikes/strace-tail.sh`, `spikes/strace-window.sh`
  - `spikes/wp05-qual-probe/`
  - `tests/Fixtures/Gagamba.Fixture.SelfTest/TestResults/`
  - `tests/Gagamba.Conformance.Tests/TestResults/`
  - `tests/Gagamba.Execution.Linux.M1.Tests/TestResults/`
  - `tests/Gagamba.Execution.Linux.Tests/TestResults/`
  - `tests/Gagamba.Execution.MacOS.Tests/TestResults/`
  - `tests/Gagamba.Execution.Tests/TestResults/`
  - `tests/Gagamba.Execution.Windows.Tests/TestResults/`
  - `tests/Gagamba.Runtime.Tests/TestResults/`
- The separate clean local branch `design/wp05-d6-3-c3-outcome-reconciliation` at `8572e1a042fd12d4bcd0c06729066babf04a94ed` descends from Luna's D6.3 commit but is outside this exact producer/review preservation. It was not modified or published here.
- All other available registered worktrees were clean at inspection. Three pre-existing `C:\Windows\Temp\opencode\gagamba-conformance*` worktree registrations were already missing/prunable; none was pruned or changed.

## 4. Terra's exact independent disposition

Terra's committed review reports:

**WP05 D6.3 TERRA INDEPENDENT REVIEW COMPLETE - OPTION B: D6.2-R1 PARTIALLY ADDRESSED; SPECIFIC DOCUMENTATION CORRECTION REQUIRED; HUMAN C3 CONTRACT DECISION HELD; NO PRODUCTION REMEDIATION AUTHORIZED; RELEASE NOT QUALIFIED**

The original mapping contradiction is corrected. Terra supports the proposed outcome semantics and source distinctions, and cases A-F pass at design level. The sole remaining required issue is the deterministic testability of R1-T13–T15. Terra did not assign a separate formal identifier to this testability issue; no new finding ID is created here.

## 5. Corrected C3 behavior and cases A-F

| Case | Proposed component result | Terra design-level result |
|---|---|---|
| A — `Kill=NotAttempted`, trusted fallback Empty | `DomainStop=Unknown`; `DomainEmpty=Confirmed`; aggregate `QuarantinedUnknown`; no kill/termination or `CleanupConfirmed` inference | Pass |
| B — `Kill=NotAttempted`, fallback Populated | `DomainStop=Unknown`; `DomainEmpty=Unknown`; no new stop or wait | Pass |
| C — `Kill=NotAttempted`, unavailable/inconclusive fallback | Both domain components Unknown; no empty evidence, retry, or deadline reset | Pass |
| D — `Kill=Failed`, trusted fallback Empty | `DomainStop=Failed`; `DomainEmpty=Confirmed`; definite failure remains authoritative in aggregation | Pass |
| E — `Kill=Accepted`, population Empty | Existing confirmed-cleanup mapping remains unchanged; finalization remains a separate requirement | Pass |
| F — `Kill=Unknown` / uncertain observation | `DomainStop=Unknown`; `DomainEmpty=Unknown`; fallback resolution is not expanded to unknown dispatch | Pass |

These are proposed contract semantics only. `DomainStop` and `DomainEmpty` remain separate facts. Existing aggregate precedence remains `Failed` before `QuarantinedUnknown` before `CleanupConfirmed`; positive population evidence does not erase a definite required-operation failure. `JournalFinalization=Unknown` cannot establish completion. No new stop authority, deadline allowance, journal record, schema, RELEASE claim, or cleanup claim is introduced.

## 6. R1-T13–T15 expected results and open testability requirement

| Test | Stop outcome | Fallback observation | `DomainStop` | `DomainEmpty` |
|---|---|---|---|---|
| R1-T13 | `NotAttempted` | Empty | Unknown | Confirmed |
| R1-T14 | `NotAttempted` | Populated | Unknown | Unknown |
| R1-T15 | `NotAttempted` | Unavailable or inconclusive | Unknown | Unknown |

Terra found the expected outcomes correct but their deterministic reproduction mechanism unspecified. R12 `DomainLease.StopAndAwaitEmpty` calls `WriteKill` before reading population (`src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs:108-143`), so the existing fake backend's population result alone cannot produce `KillDispatchOutcome.NotAttempted`. Frozen R12 does not contain the proposed typed seam or fixture.

**OPEN — R1-T13–T15 deterministic test fixture and injection specification required.** The next documentation-only work item is **WP05 D6.4 — R1-T13–T15 Deterministic Fixture Specification**. It must specify a future test-only seam at the proposed Reconciler outcome boundary that supplies `KillDispatchOutcome.NotAttempted` without calling `WriteKill`, invokes the existing fallback path, injects Empty/Populated/unavailable-or-inconclusive observations, and asserts component outcomes, aggregate classification, durable progress, and no fabricated kill-success evidence. This is a future testability requirement, not an implemented fixture or production API.

## 7. Source and evidence provenance

Terra independently bound its analysis to R12 commit `38f6981b2a60daab2df37c92913a6760c1bef131` and source tree `6854675d6b37a49d5302d6104103bdbfbc268040`. Relevant frozen R12 evidence:

- `src/Gagamba.Execution.Linux.M1/Lifecycle/DomainLease.cs:102-104` exposes retained-lease population observation separately; `:108-143` shows the existing kill-first bounded stop.
- `src/Gagamba.Execution.Linux.M1/Lifecycle/RestrictedDomain.cs:15-21` models known/empty population independently.
- `src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs:263-275` confirms and persists the existing DomainEmpty progress bit on trusted Empty fallback evidence; it proves no kill success.
- `Recovery/Reconciler.cs:423-471` is the existing aggregation/finalization source context; the corrected typed rules remain proposed and are not R12 behavior.

Frozen harness Q3 remains `36a124d7dff98ecfaf956340767fe1768ad5b17f`. OwnershipSnapshot v3, FD4 and FD5 protocols, D1-D5 source-bound evidence, D6.0/D6.1/D6.2 reviews, and prior preservation checkpoints remain unchanged. The producer and reviewer commits change only the listed documentation; no tests were executed and no runtime qualification exists.

## 8. Approval and qualification boundaries

| Contract | Status |
|---|---|
| C1 | Design independently supported; human approval pending |
| C2 | Design independently supported; human approval pending |
| C3 | Outcome semantics corrected; D6.4 testability clarification and policy decision pending |
| C4 | Design independently supported; human approval pending |

All remain `PROPOSED - NOT APPROVED`. C3 additionally remains `C3 ZERO-WAIT POLICY - PENDING HUMAN`; neither Policy A nor Policy B is selected. No G3 gate is accepted. The component-only evidence boundary remains: `ReleaseArbiter` and `Reconciler` are not qualified as an integrated production owner. No production remediation is authorized; release is not qualified.

Unchanged deferrals: H1 production status wiring; H2 destructive FD5 descriptor tests; H3 optional raw SIGKILL witness; H4 individual C1-C4 human decisions; H5 historical D5 ordering note; WP08 owner orchestration; WP10 status and target integration; full G3 acceptance; and remaining portability qualification.

## 9. Publication and fresh-clone verification

Before publication, `git ls-remote` found no D6.3 producer, Terra review, or preservation ref on origin; the existing D6.2 preservation ref remained at `31132513a983bb37cfd609f56541f6ab8d44af84`. No remote publication is claimed by this checkpoint at creation time.

| Ref to publish by ordinary non-force push | Expected commit |
|---|---|
| `design/wp05-d6-3-c3-outcome-correction` | `4209581a03562b434badd7a5f9927663559911be` |
| `review/wp05-d6-3-terra-independent-c3-outcome` | `cda2f22221c4db12ced3a4317362e80167270a63` |
| `preservation/wp05-d6-3-terra-independent-c3-review` | This checkpoint commit, direct child of Luna's commit |

After publication, a fresh clone must verify the three exact refs, parent/sibling ancestry, producer/reviewer/preservation file scopes, Terra review blob `3bb7fd2322670d9f9977004661ab2a72760798d4`, verbatim Option B text, corrected mapping and R1-T13–T15 expectations, open fixture requirement, no approved contract, pending C3 policy, unchanged R12 tree and harness commit, intact D6.2 preservation ref, clean preservation checkout, and `git fsck --full` exit status/warnings. The final preservation report records the publication result; no force update, tag, package, default-branch change, or unrelated ref is permitted.

**Preservation is evidence custody only. It does not implement D6.4, approve C1-C4, choose C3 policy, authorize production remediation, or qualify a release.**