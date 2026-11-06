# GAGAMBA-RC2B-06-B6-ALT-B-P4-COVERAGE-OWNER-ACCEPTANCE-01

**P4 REFUSAL-ONLY COVERAGE ACCEPTED WITH QUALIFICATIONS**

Date: 2026-11-07 (Asia/Manila). Executor: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This is a bounded owner decision on the completed **refusal-only P4 coverage reconciliation**, not an implementation or qualification task. It accepts only the reconciled refusal-only coverage of Alternative B P4, subject to the binding qualifications in §5. It does **not** accept Slice E, Slice N, positive validation, protected native qualification, production activation or any broader Q04/S09 claim.

## 1. Source identity and completed evidence

| Item | Exact identity |
|---|---|
| Canonical repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main`, one worktree |
| Decision baseline HEAD / tree | `dfe820170dc45efe57f8e6c74b73ffff6cf57a91` / `660b72f7adebc5b550a643af4afbeeb5d43f4ec3` (matches the commission) |
| Reconciled / source-reviewed HEAD / tree | `762cdf2796f744fb99b75f30e83813a48d048a97` / `3fdb16557b232d34927bfcbc5d35b5b58635b29b` |
| Commits between reviewed HEAD and decision baseline | exactly one: `dfe8201 docs: reconcile final refusal-only P4 coverage`; **zero non-docs diff** (production/test source byte-identical) |
| Primary evidence | `docs/evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md`; in-repo SHA-256 `b3f851aa878d64315af70902d5148819066b684f7d3d3e35e1c4bc49c2ec4d0e` |
| Reconciliation package | `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-coverage-recon-01`; `SHA256SUMS.txt` **3/3** verified; in-repo copy byte-identical to the package copy |
| Independent P4-02 Q1 package | `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-02-q1`; `REPORT.md` SHA-256 `4a56bb0da461d595b608838fba0687473a2242dd2c7db43f3754bb97e573c7f6`; `findings.md` SHA-256 `c196a9707d50d59e221cf02b4cf822991503ce3a3a95bf19762caed24dad19f3` |
| Independent P4-03 Q1 package | `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-03-q1`; `REPORT.md` SHA-256 `510395ba7ff1d1c6006f3c64e2bcecdd36d2bd52e4b2dcffb2d61c9c6d9ec694`; `findings.md` SHA-256 `3ecdfe2767c5a099eeccd07d32211a6e54e0e06cf657bd39c1f8522f9eedfacc`; `owner-recommendation.md` SHA-256 `9e3519e0e47af90136008c49773a3be8ec4647db51c9c198cd6484bb89197227` |
| Prior owner decisions read | [P4-02 acceptance](rc2b-06-b6-alt-b-p4-02-owner-acceptance-01.md), [P4-03 acceptance](rc2b-06-b6-alt-b-p4-03-owner-acceptance-01.md), and the P1/P2/P3, Mechanism B, Alternative B design and controller Q10 decisions they cite |

At preflight HEAD `dfe8201` and tree `660b72f7…` matched the commission exactly; the branch is `main`; there is exactly one canonical worktree; tracked files/index are clean; and all **44** historical untracked paths are preserved. The reconciliation records the reconciled reviewed HEAD as `762cdf2` / `3fdb1655…`. Independent checking found that the only commit from that reviewed HEAD to this decision baseline is the docs-only reconciliation commit, with no production or test source change, so the reconciled source facts bind to the decision baseline.

The reconciliation is an **independent source/evidence reconciliation**: it re-traced all 29 sinks in source and reused the already executed P4-02/P4-03 independent qualifications rather than rerunning them, and it deliberately did **not** rerun the full Windows M1 suite. This record does not treat the producer's conclusions as independent evidence; §2 records the checks the owner-acceptance recorder performed directly.

## 2. Independent verification performed for this record

- **Baseline integrity.** Exact commit/tree/branch, single worktree, clean tracked/index state and 44 preserved untracked paths were verified directly.
- **Source binding.** `git log 762cdf2..dfe8201` returns only the docs commit; `git diff --stat` over all non-`docs` paths between the two revisions is empty. The reconciled 29-row source facts therefore apply to the decision baseline.
- **Evidence integrity.** The reconciliation package `SHA256SUMS.txt` verifies 3/3, and the committed `docs/evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md` is byte-identical (SHA-256 `b3f851aa…`) to the package copy. The independent P4-02 Q1 and P4-03 Q1 `REPORT.md`/`findings.md` (and P4-03 `owner-recommendation.md`) hashes match, exactly, the hashes recorded in the prior owner acceptances.
- **Closed production caller graph (reverse calls).** Independently reproduced at the decision baseline: `IntegratedRecoveryHost.Run` has the sole production caller `Bootstrap/RecoveryHost.cs:72`; `QualifiedRecoveryConnection.Start` the sole caller `Bootstrap/RecoveryConnection.cs:33`; `RecoveryConnection.Start` the sole caller `Bootstrap/IntegratedB3Client.cs:67`; `new HeldReleasePhase` the sole construction `Lifecycle/OwnerPreparationTransaction.cs:531`; `new B3PreparationOwner` the sole production construction `Restricted/B3Qualification.cs:199` (guarded, three-argument).
- **S10/S11 domination basis.** `StartOwnerPreparation` has exactly three production callers — `Bootstrap/IntegratedRecoveryHost.cs:89` (installed route), `Bootstrap/B2RetainedDomain.cs:27` (legacy v1) and `Restricted/B3Qualification.cs:426` (legacy non-integrated); `TrySpawnOwner` has exactly three — `Lifecycle/M1RecoveryScope.cs:389` (inside S10), `Bootstrap/RecoveryHost.cs:85` (legacy v1) and `Bootstrap/QualifiedB2Recovery.cs:72` (B2 evidence). Only the `IntegratedRecoveryHost` route is installed, and it is gated before entry.
- **S28 test-only basis.** `Lifecycle/PreparationOwnershipCoordinator.cs` has no production construction site; its only constructor call is `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/PreparationOwnershipTests.cs:61`.
- **Enforcement-before-effect ordering (source order).** `IntegratedRecoveryHost.Run` calls `RecoveryInitializationAdmission.RequireIntegrated` as its first statement (`:23`), before `VerifyPrivateDirectory` (`:29–30`), `JournalFileStorage`/journal (`:49–50`), the initial writer (`:77`) and `StartOwnerPreparation` (`:89`). `B3PreparationOwner(profile, provider, providerInstallation)` performs the F02 topology refusal (`:282–285`), then `B3ProviderAdmission.Require` (`:287`), before scope/client/journal object setup (`:288–293`) and `Directory.CreateDirectory` (`:295`). `RecoveryHost.RunCore` opens the v1 delegated parent pre-association (`:59`) but defers the v2 integrated parent until after `QualifiedRecoveryEcho.Run` returns (`:66–68`). `QualifiedRecoveryEcho.Run` runs `RecoveryInitializationAdmission.Require` (`:31–32`) before returning a positive launch plan (`:33`). `OwnerPreparationTransaction.RunTransaction` runs the early refusal (`:63–102`) before `new JournalFileStorage()` (`:104`). `RestrictedExecutionRuntime.Create()` returns `UnavailableRestrictedExecutionProvider` (`:9`).
- **No execution rerun.** This record did not re-execute the deterministic suites or any native case; it inspected source and verified evidence integrity. The full Windows M1 suite was deliberately not rerun (no material inconsistency was found that would require it).

## 3. Decision

**P4 REFUSAL-ONLY COVERAGE ACCEPTED WITH QUALIFICATIONS.**

The evidence supports acceptance **only** of the following, within the reconciled scope:

1. **Complete 29-row disposition.** 21 **COVERED** (S01, S02, S03, S05, S07, S08, S09, S10, S11, S15–S25, S29), 5 **AUTHORIZED CONTINUATION** (S06, S12, S13, S26, S27), 3 **LEGACY / TEST-ONLY** (S04, S14, S28). Zero **SLICE E DEPENDENCY**, zero **GAP**, zero **INCONCLUSIVE**.
2. **Enforcement or demonstrated domination before the first protected effect** on reachable production paths, including the S10/S11 dominated route (via the F01 gate at `IntegratedRecoveryHost.Run:23`) and the S28 test-only disposition.
3. **No demonstrated production authority or RELEASE bypass** within the reconciled scope.
4. **Preservation of all existing qualifications and exclusions** (P1–P3, P4-02/P4-03, Mechanism B, controller Q10, and the accepted observation-to-effect residual), as restated in §5–§6.

`AUTHORIZED CONTINUATION` and `LEGACY / TEST-ONLY` are accepted as such and are **not** reinterpreted as unconditional `COVERED`. The `SLICE E DEPENDENCY` class is empty; validation rows (plan T16–T18/T20) are validation-bootstrap obligations and are not P4 sinks.

## 4. Accepted claims and supporting evidence

| Accepted claim | Supporting evidence |
|---|---|
| 29-row disposition, zero GAP/INCONCLUSIVE | Reconciliation §3–§4 (tally and per-row matrix); independently re-verified closed caller graph and enforcement ordering (§2) |
| Enforcement/domination before first protected effect | Reconciliation §4–§5; independent source-order checks of S02/S03/S05/S07/S08/S09/S15/S16/S21/S24/S25/S29 and the S10/S11 domination basis |
| No demonstrated production authority or RELEASE bypass | Reconciliation §5 (downgrade/missing-context refusals), §6–§7; P4-02 Q1 and P4-03 Q1 independent qualifications (hashes verified) |
| S01/S03 factory/client provider gates; F01 recovery-initialization gate; P1; P2; P3/Q10; Q10 controller reread | [P4-02 acceptance](rc2b-06-b6-alt-b-p4-02-owner-acceptance-01.md), [P4-03 acceptance](rc2b-06-b6-alt-b-p4-03-owner-acceptance-01.md), [29-row inventory](../evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md), plan §§3.1–3.4 |
| S02 constructor-provider boundary (Q1 O1 closed) | P4-03 acceptance §§2; producer [P4-03 evidence](../evidence/rc2b-06-b6-alt-b-p4-03-impl-01.md); independent P4-03 Q1 (hash-verified) |

## 5. Binding qualifications (preserved)

- **F-R1 / O-R1 — S10/S11 rely on the audited caller graph and upstream F01 domination, not local enforcement.** The generic `M1RecoveryScope.StartOwnerPreparation`/`TrySpawnOwner` callees have no local admission check; safety comes from the F01 gate dominating their only installed caller. **Any new production caller of these generic methods (or of `RecoveryConnection.Start`, `IntegratedRecoveryHost.Run`, `OwnerPreparationTransaction.Run` or a `B3PreparationOwner` constructor) invalidates the corresponding domination claim and requires a fresh sink and authority audit before its first protected effect.** No remediation is required now.
- **O-R2 — S28 remains test-only.** `PreparationOwnershipCoordinator` and the low-level copy/publisher/launcher routes have no production caller at this revision; `internal` visibility does not establish authority. Any future production caller requires a fresh sink audit.
- **O-R3 — P4-03 Q1-F01 refusal-reason typing remains open.** In the narrow window between the factory read and the constructor re-read, an `InvalidDataException` from `B3ProviderAdmission.Require` is typed `InstallationUnavailable` rather than `UnqualifiedInstallation`. Both paths refuse with zero effects and no authority granted.
- **Accepted observation-to-effect residual.** A change after the verifier's last relevant stable observation, including before its return, may escape detection, and a later gate may refuse after earlier effects established under valid admission. No atomic root-mutation/effect lock or retroactive cancellation is claimed.
- **`NATIVE-PENDING` on applicable covered rows.** S05, S08, S09, S11, S21, S22, S23, S24, S25 and the domination/integrity rows rest on source, deterministic and IL evidence, not on a protected installed positive. Real protected S2/final positives and withdrawal/mismatch, process/pidfd/SCM custody and privilege drop, blocked-read/bounded stop, generation switching, native RELEASE and partial-resource cleanup remain unqualified (Slice N).
- **331 unqualified native skips.** The 331 native skips (and the 56/58 filter skips in the reused Q1 runs) are **not** passes and are not counted as coverage. Portable directory absence/removal is not native lifecycle proof.
- **Evidence provenance limits.** The reconciliation is source-only and reused the P4-02/P4-03 executed evidence; P4-02 Q1 did not independently rerun full M1 (producer 1,349/331/0 is producer evidence only), while P4-03 Q1 did rerun full M1 (1,368/331/0). Neither Q1 is cross-model certification. Carried qualifications include non-bit-reproducible rebuilt binaries and the reflection-fabricated `LoadedInstallation` boundary note.
- **F01/F02 closures remain scoped.** F01 is closed for the normal-qualified integrated recovery initialization boundary and F02 for the prohibited qualified/non-integrated constructor topology; wider constructor-context, direct-entry and Q04/S09 coverage were not claimed globally and are not accepted here.

## 6. Explicit exclusions (not authorized or implied)

This decision must not authorize or imply acceptance of:

- Slice E atomic validation bootstrap or its T16–T18/T20 obligations;
- Slice N protected native qualification;
- protected installed positive execution;
- production activation or deployment readiness;
- broader Q04/S09 claims beyond the refusal-only scope;
- any new production implementation or test scope.

Validation authority remains **non-constructible within the existing qualified release boundary**: `OwnerAuthorityContext.Validation` has a private constructor and no factory, `Qualified.ForTest` is excluded from Release, and the Release production verifier continues to exclude the Debug-only authority factories and qualification surfaces. No new manifest or RELEASE authority is accepted; the owner remains the sole physical RELEASE writer and recovery remains stop-only.

## 7. Unresolved obligations

1. **Slice E (atomic validation bootstrap):** independent live root authorizer, fixed reviewed launcher, protected catalog/approval chain, privilege drop with original custody/pidfds/SCM, protected K/M/F/S acquisition, exact case policy, GAVA1, prepublication integrity verifier, v3 pre bootstrap parser and the atomic R/O/C pre-writer ordering. Must arrive atomically; the path stays refusing until then.
2. **Slice N (protected native qualification):** all §5 `NATIVE-PENDING` rows, the observation-to-effect residual, native stop/reap/reconcile and partial cleanup on a separately authorized disposable host.
3. **Future-caller invariants:** O-R1 reverse-call index currency and O-R2 S28 re-audit; O-R3 refusal-reason typing.
4. **Broader Q04/S09** coverage and production activation require separate owner decisions.

## 8. Recommended next independent milestone

**Slice E (atomic validation bootstrap)** if positive validation is wanted; otherwise **Slice N (protected native qualification)** on a separately authorized disposable host with exact candidate/host/boot/kernel/runtime/delegation binding. Each requires its own authorization and must not start automatically. Do not read this acceptance as authorization for either.

## 9. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Production, tests, historical evidence and previously accepted owner decisions remain unchanged. The external acceptance receipt is under `Solo/gagamba-alt-b-p4-coverage-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No new branch/worktree, reset, clean, restore, rebase, prune, push, privileged provisioning, production activation, or Slice E/N implementation. Preserve all 44 historical untracked paths. Stop after recording the owner decision; do not automatically begin Slice E or Slice N or any P4 implementation.
