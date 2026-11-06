# GAGAMBA-RC2B-06-B6-ALT-B-P4-02-OWNER-ACCEPTANCE-01

**P4-02 SOURCE IMPLEMENTATION ACCEPTED WITH EXPLICIT QUALIFICATIONS**

Date: 2026-11-06 (Asia/Manila). Executor: OpenAI Codex. Scope: documentation-only owner decision on canonical `main`; no push.

This owner approval is limited to the independently reviewed P4-02 corrections for F01 and F02. Supporting disposition: **P4-02-Q1 SUPPORTED WITH QUALIFICATIONS**. Both findings are **CLOSED within the reviewed P4-02 scope**. This is not acceptance of P4 as a whole, broader Q04/S09 sink coverage, positive validation bootstrap, native qualification or production activation.

## 1. Source identity and completed independent evidence

| Item | Exact identity |
|---|---|
| Accepted reviewed HEAD / evidence commit | `645b85b4200efd5a13f22c83bd1a9aa1b881f5ed` |
| Reviewed HEAD tree | `40d016251705034c07c9f795f89da161d262f9d4` |
| P4-02 implementation | `b96e86950c8bbb8ef44b00b5076afe9c17a22f2b` |
| Implementation tree | `b5c331211d0712707397ec7161e34ad7a5dc3861` |
| Implementation baseline | `6f910669972db51959c5f0920a775e3d6789db06`, tree `2baffb9989d53209c0ecd6c8d5c60e17ba183613` |
| Canonical repository | `C:\Users\Laszlos\source\repos\Gagamba`, `main`, one worktree |
| Independent Q1 package | `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p4-02-q1` |
| Q1 manifest | All **2,512** self-excluded SHA-256 entries verified, zero mismatches; manifest SHA-256 `59c607dd8395bb9589c7d55f1b14e3589067f983fc1cc4ca73bd862b5762586c` |
| Q1 `REPORT.md` SHA-256 | `4a56bb0da461d595b608838fba0687473a2242dd2c7db43f3754bb97e573c7f6` |
| Q1 `findings.md` SHA-256 | `c196a9707d50d59e221cf02b4cf822991503ce3a3a95bf19762caed24dad19f3` |

At this decision's preflight, HEAD had not advanced from the expected evidence commit; implementation and evidence were reachable, tracked files/index were clean, one canonical worktree remained, and all 44 historical untracked path hashes matched Q1. Q1 was completed, with no equivalent owner acceptance or superseding decision found. Session/process/status checks found no concurrent Gagamba writer; no global exclusion lock is claimed. The independent package and historical evidence are preserved unchanged.

Q1 independently built a Git-free export and bound all 1,851 committed files to Git blobs, separately verifying Git's CRLF conversion and unchanged exported bytes. Q1 ran in a fresh Codex review session with `gpt-6-luna` assistance for topology and authority probes and the lifecycle/contract audit. The primary session assessed the evidence and made the Q1 disposition. This records actual engineering participation; a separate session or Luna assistance is not independent-model certification or native proof. This acceptance task uses Codex with Luna assistance to check the recommended next boundary; it performs no new qualification run.

Committed supporting records: [P4-02 producer evidence](../evidence/rc2b-06-b6-alt-b-p4-02-impl-01.md), [Q1 handoff](../reviews/rc2b-06-b6-alt-b-p4-02-q1-handoff.md), [29-row inventory](../evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md), [P4 handoff](../plans/rc2b-06-b6-alt-b-p4-implementation-handoff.md) and [accepted implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md). The external Q1 `REPORT.md`, `findings.md`, `effect-ordering.md`, `authority/authority-probes.md`, `topology/topology-assessment.md` and `lifecycle-assessment.md` supply the independent conclusions and precise limits.

## 2. Accepted F01 properties and boundary

**F01 — CLOSED within the normal-qualified integrated recovery initialization boundary reviewed by Q1.**

- Qualified integrated recovery independently checks its actual retained recovery-role S2 selection and protected final-admission authority before the reviewed protected effects. `QualifiedRecoveryEcho` checks before its positive continuation; `RecoveryHost` defers installed delegated-parent acquisition until that check succeeds; `IntegratedRecoveryHost.Run` checks independently before journal/safety/worker initialization and the initial writer, durable intent and operation-owner spawn.
- Direct-entry missing or mismatched retained-context requests refuse before initialization entry. Required full installation identity, original qualification ID/raw record hash, generation and original process/session bindings are preserved. Installation admission does not replace the private one-use association, original peer/pidfd authentication or immutable direct-session binding.
- Qualified execution cannot obtain legacy admission by a false profile flag, missing authority or verifier failure. Legacy v1 cannot carry installed authority. No new manifest, capability, HELD or RELEASE authority is created by the recovery guard. Owner P2 early/late admission, P3 owner generation verification, controller Q10 and the sole physical RELEASE writer remain separate.
- Already admitted operations retain their original stopping, safety, reap, cleanup writer and reconciliation duties without renewed final admission; original deadlines are not renewed.

The first claimed protected capability is the installed delegated-parent acquisition (`RecoveryHost.cs:68`); the initial writer is `IntegratedRecoveryHost.cs:77`, with lock-file open/create/flock in `JournalFileStorage`; downstream staging-parent retention, durable AllocationIntent and owner spawn are at `M1RecoveryScope.cs:319–389`. Existing bootstrap/stop machinery may precede the check under T01. This does not assert that all Start effects are forbidden until admission or that every generic low-level callee is universally gated.

The following precise lifecycle qualification is retained verbatim from Q1 `REPORT.md`:

> New initial refusal occurs before owner O exists. The first successful gate may already have authorized parent acquisition before a later fresh local refusal; scope disposal then releases that retained descriptor. It must not be described as zero earlier effects. Already admitted operations retain original safety stop, pidfd signaling, reap, retained-resource cleanup, cleanup writer reacquisition and reconciliation without renewed final admission. New scans run outside stop/custody/journal locks and use minima of original setup/expiry and the scan cap; late verifier completion cannot resume an aborted caller. No source-visible new refusal leak was found. Native partial-resource cleanup remains **Unknown/pending**.

## 3. Accepted F02 properties and boundary

**F02 — CLOSED for direct qualified/non-integrated B3 construction.**

- The real `B3PreparationOwner` constructor rejects `QualifiedInstallation=true` with `IntegratedRecovery=null` before scope/client/journal construction and before journal-directory creation (`Restricted/B3Qualification.cs:271–285`). The factory refusal remains.
- Valid legacy constructor behavior remains within its accepted scope. Qualified/integrated topology restrictions remain enforced as reviewed through the existing guarded production route; the synthetic integrated constructor control establishes branch classification only.
- Baseline and fixed Debug/Release probes establish original directory creation and fixed refusal with an absent directory, with legacy/classification controls. No protected installed-positive constructor success is inferred.

Qualified/integrated direct construction still lacks constructor-local provider S2/final verification. At the reviewed source its sole production caller is `B3Qualification.TryCreate:196`, dominated by the protected provider check at 187–192. Q1 demonstrated no current production authority bypass through that residual. F02 closure does not close S02's broader missing-context obligation; O1 below remains open.

## 4. Evidence counts and mandatory qualifications

| Q1 check | Result and scope |
|---|---|
| New P4-02 focused tests | 14 passed, 0 failed, 0 skipped |
| Authority/authentication/P1/P2/P3/stopping/C3/reconciliation/capture/installation filter | 338 passed, 0 failed, **56 native skipped / NotExecuted** |
| Isolated D6.4 | 3 passed, 0 failed |
| Independent builds and closure | Debug/Release builds succeeded; Git-free SourceLink warnings disclosed; Release production verifier PASS; Debug negative control refused as expected |
| Constructor and authority controls | Baseline/fixed Debug/Release constructor matrix; six direct-entry/session controls; same fail-fast harness detects four expected zero-entry violations on a separate gate-deleted DLL copy made with the verified producer PE mutator; original image unchanged |
| Full M1 | **Not independently rerun by Q1.** Producer's 1,349 passed / 331 skipped / 0 failed is verified producer evidence only |

The 56 skipped review cases and all 331 producer native skips are not passes. Focused/filter counts do not establish full M1 regression reproduction. Source-level ordering, IL and deterministic tests do not establish complete protected native lifecycle behavior, process custody, writer/domain effects, native stopping/reaping or partial cleanup. Protected native installed-positive qualification remains **Slice N, pending**; actual native cleanup remains Unknown/pending.

P4-02 is not the entirety of P4. Broader constructor-context and direct-entry coverage, full Q04/S09 and generic low-level caller coverage remain open. P1, P2 and P3 retain their existing bounded qualifications under their [P1](rc2b-06-b6-alt-b-p1-owner-acceptance-01.md), [P2](rc2b-06-b6-alt-b-p2-owner-acceptance-01.md) and [P3](rc2b-06-b6-alt-b-p3-owner-acceptance-01.md) decisions; those decisions are not rewritten, superseded or expanded by this acceptance.

Positive validation bootstrap remains atomic **Slice E**. P4-02 is compatible with the accepted plan only as refusal-only narrowing: unsupported validation contexts stay refusing. Mechanism B's validation-before-final-admission design is not implemented by sending validation through the normal Qualified/final gate. Independent issuer/context, protected inputs, integrity projection, GAVA, policy/custody and coordinated R/O/C ordering must arrive together. There is no new prepublication exception, synthetic Qualified authority or positive validation/bootstrap qualification. No production activation is authorized.

The accepted observation-to-effect residual remains: a change after the verifier's last relevant stable observation, including before its return, may escape detection; later refusal can follow earlier effects established under valid admission. No root-mutation/effect lock or retroactive cancellation is claimed.

### Original Q1 findings and residual obligations (verbatim)

The following is the original Q1 `findings.md` body after its title. O1, Q1 and Q2 retain their scope, consequence and suggested future correction; none is silently discarded or promoted into new acceptance:

Disposition: **P4-02-Q1 SUPPORTED WITH QUALIFICATIONS**. No demonstrated actionable in-scope implementation defect remains. F01 CLOSED for normal-qualified integrated recovery pre-effect refusal; F02 CLOSED for qualified/non-integrated direct B3 construction.

## O1 — Qualified integrated constructor-local authority remains broader P4 work

Location: `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs:271–285`. Contract: P4-01 inventory S02 and implementation handoff's broader constructor-context requirement. The new constructor guard rejects only `QualifiedInstallation && IntegratedRecovery is null`; integrated qualified direct construction still has no local S2/final check before directory creation.

Reproducer: independently authored topology probe, qualified=true with synthetic non-null IntegratedRecovery, Debug/Release. Constructor chooses IntegratedB3Client and creates the fresh journal directory without protected verification. This is an observed constructor-context omission, not an authorized positive.

Consequence: a future unguarded caller could create effects before installation admission. Current reverse callers show only `B3Qualification.TryCreate` at line 196 in production, dominated by provider S2/final verification at 187–192. No current production authority bypass was demonstrated; it does not reopen the user's exact prohibited-topology F02 closure. Preserve this omission rather than claim general S02/P4 acceptance.

Smallest future correction: separately commission constructor-local protected provider verification or an unforgeable verifier-derived prerequisite before directory effects, preserving legacy and legitimate stop duties. No remediation was made here.

## Q1 — Validation/bootstrap dependency

Contract: accepted implementation plan §3.4–3.5, Mechanism B validation-before-final-admission design. Normal Qualified final admission is not a substitute for independently authorized prepublication validation. Slice E is unavailable and must remain refusing until its issuer, protected inputs, GAVA, integrity/context, exact policy, custody and R/O/C ordering arrive atomically. P4-02 is compatible only as the allowed refusal narrowing, not positive bootstrap completion. No unresolved new design decision was found.

## Q2 — Native and observation-to-effect obligations

Real protected S2/final-positive/withdrawal direct Start, process/pidfd/session/SCM custody, original deadline/blocked-read stop, parent/writer/intent cutpoint traces, generation switches, native RELEASE and partial cleanup/reaping remain pending. Portable non-entry/IL assertions do not qualify those effects. All native skips remain unqualified; actual native cleanup is Unknown/pending. Accepted changes after last stable observation can escape detection. No locking or retroactive cancellation claim was added.


## 5. Recommended next boundary: P4-03 (not commissioned here)

Recommend **qualified integrated B3 constructor-context refusal and bounded caller/context audit**, starting with inventory **S02 / Q1 O1**. The accepted P4 handoff already requires actual provider S2/final verification, or an unforgeable verifier-derived prerequisite, before qualified integrated constructor directory effects. The named prohibited-topology fix remains closed.

| Route | First protected effect / current state | Recommended bounded P4-03 work |
|---|---|---|
| S02 `B3PreparationOwner` qualified/integrated direct construction | Scope/client/journal objects precede `Directory.CreateDirectory(profile.JournalDirectory)` at 285; no constructor-local protected context. Sole current production factory is guarded | Require actual protected provider verification or an unforgeable verifier-derived prerequisite at constructor entry before construction effects. Refuse absent/invalid/mismatched context; no Boolean, profile, echo or bare grant tuple as authority. Keep factory defense and legacy behavior. |
| S09 direct `IntegratedRecoveryHost.Run` | Local missing/mismatched context refusal already closed by F01; first downstream protected setup includes safety descriptors then initial writer | Reconfirm the existing entry guard and caller/context bindings in the same audit; do not reopen it without new evidence or claim universal low-level coverage. |
| S10/S11 generic scope preparation / installed owner launch | Staging-parent acquisition, durable intent and owner spawn; audited installed caller is dominated by F01. Other current callers are legacy/B2 | Recheck reverse callers and missing-context classification. Add refusal only for a demonstrated uncovered installed initialization route; preserve accepted bootstrap/stop-only and legacy duties. These are not newly demonstrated bypasses. |
| S28 low-level coordinator/copy/publisher/launcher constructors | Potential journal/copy/publication effects; currently test-only by reverse callers | Retain explicit test-only/current-caller disposition. Any discovered production installed route must supply trusted private context or refuse before its first protected effect; do not impose admission on cleanup writers. |

Suggested P4-03 evidence: real direct constructor missing/invalid authority with zero journal-directory creation, legacy controls, Debug/Release checks, actual wiring/order and bypass-negative control; updated 29-row dispositions and preserved stopping/P1–P3/Q10. Simulated fixtures must remain labeled and Release must exclude authority test hooks. Protected positive controls remain Slice N.

The normal-qualified S02 refusal correction can precede Slice E. Any route requiring a validation authority context remains unavailable until the entire Slice E dependency set (T16–T18/T20) and validation-specific pre-writer ordering are integrated atomically. Do not expose validation, create a temporary final-admission exception, broaden all journal constructors, or gate stop/reconciliation on renewed admission. No P4-03 implementation is authorized by this recommendation.

## 6. Preservation and stop

This decision updates only this new decision and the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Production, tests, historical evidence and previously accepted owner decisions remain unchanged. External acceptance receipts are under `Solo/gagamba-alt-b-p4-02-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No new branch/worktree, reset, clean, restore, rebase, prune, push, provisioning, production activation or Slice E/N/P4-03 implementation. Preserve all 44 historical untracked paths. Stop after this bounded acceptance and the recommended next boundary.
