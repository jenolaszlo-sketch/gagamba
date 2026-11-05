# RC2B-06 B6 — Alternative B P1 owner acceptance

**P1 SOURCE IMPLEMENTATION ACCEPTED WITH EXPLICIT QUALIFICATIONS**

Date: 2026-11-06 (Asia/Manila)

Decision: `GAGAMBA-RC2B-06-B6-ALT-B-P1-OWNER-ACCEPTANCE-01`

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-P1-IMPL-01`

Governing design decision: [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md)

Accepted handoff: [Integrated Start authority implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md)

Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`

Canonical HEAD at acceptance: commit `b1bf46c7110f479f94f6d5ce631cbdef7a66a5d2`, tree `d71959a070ba6658c6f9b96b82d2fa35bd0d781e`

Accepted implementation: commit `2ae1121777c5a9ade4e405a485472395cc5219b0`, tree `431114267e245854d91f6066783f75733eff3141`, direct child of baseline `d804d9062bab5d9d540df5c2d64f1b2c31bdd667`

Executor of the implementation and of this record: GLM Flash in OpenCode

## 1. Decision

The human owner approves **P1 SOURCE IMPLEMENTATION ACCEPTED WITH EXPLICIT QUALIFICATIONS**.

This acceptance is limited to the implementation at `2ae1121777c5a9ade4e405a485472395cc5219b0` and its independent P1-Q1 review. It accepts Slice P1 only — retaining the owner's first verified qualified selection and adding sealed, disjoint owner authority contexts — and does not accept P2-P4, Slice E, Slice N, native qualification, protected-store provisioning, publication or production activation.

## 2. Accepted properties

Accepted within the demonstrated source scope:

1. Qualified owner context originates from the first trusted `LoadedInstallation` verification (`QualifiedOwnerPreflight.Verify` returns the actual verified result through `OwnerAuthorityContext.Qualified.FromVerified`).
2. The full installation identity, manifest, qualification ID and raw qualification-record hash are retained from that first owner-local verification.
3. Subsequent observations cannot replace the original selection; the later direct echo compares against the retained tuple and never adopts or caches the fresh observation.
4. Qualified, validation and legacy contexts remain disjoint sealed cases with a private base constructor and no caller Boolean selector.
5. Validation authority is non-constructible through ordinary production paths (private constructor, no factory; the Debug-only `ForTest` factory is excluded from Release by `#if !GAGAMBA_PRODUCTION`).
6. The transaction and the in-process HELD handoff retain the original authority context; `HeldReleasePhase` refuses a qualified phase without retained qualified authority before constructing its release machinery, and P1 does not consume the context to authorize RELEASE.
7. Legacy, stopping, recovery, C3 and sole-RELEASE-writer behavior remain within their existing contracts; no new grant, writer, deadline, admission read, validation path or production activation is introduced.

## 3. P1 effect-ordering contract

P1's two consistency checks occur at different points in the owner transaction. The distinction must be preserved exactly and must not be restated as though all initialization effects follow the later echo.

1. **Early context/wire classification (before effects).** `OwnerPreparationTransaction` classifies the request once and performs the retention/wire shape check before any journal, writer, allocation or staging effect. In `OwnerPreparationTransaction.cs` this refusal precedes `new JournalFileStorage()`, `OwnershipJournal` construction and `TryAcquireWriter`; a qualified-shaped request without retained qualified context, or a legacy-shaped request carrying qualified context, aborts at this point.

2. **Later direct-echo comparison (after effects).** The later echo runs inside `OwnerDirectConnection.Accept`, which `OwnerPreparationTransaction` calls only **after** journal construction, writer acquisition (`TryAcquireWriter`), the boot-identity check, the existing `TryProveAllocationIntent` allocation-intent proof and the `WriterReady` publication. The echo then re-enters the trusted fixed-root verifier (`LoadedInstallation.Verify("owner", qualified.Identity)`), compares the fresh observation against the first retained tuple through `ReleaseGenerationGate.RequireStillQualified` (entire identity record, qualification ID and raw record hash, with fresh `Qualified` state) and sends the retained identity.

Consequences that are explicitly **not** claimed:

- A changed later observation can cause refusal **only after** writer acquisition, the allocation-intent proof and `WriterReady`; the writer is disposed when the exception unwinds. P1 does not move the existing echo earlier and does not claim that the later echo precedes those effects.
- P1 does not implement or claim P2/P4 pre-effect final-admission enforcement or full admission-sink ordering. The early context/wire check is a refusal-only consistency check, not a final-admission grant.

This section is the canonical wording of the correction raised by the independent review observation on echo ordering. It does not weaken any refusal, stop, deadline or sole-writer requirement.

## 4. Independent evidence basis

Independent reviewer: separate P1-Q1 review, executed in Codex (not the originally named Terra High environment). Package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p1-q1`.

Disposition: **P1-Q1 SUPPORTED WITH QUALIFICATIONS** — no actionable P1 defect demonstrated; the owner is recommended to accept P1 only, conditional on the outstanding P2-P4, Slice E and native obligations, without automatic advancement or activation.

- Review report `REPORT.md` SHA-256 `2a9868fb11d169d271487713b26c7018096d75530bbc002b1f9462838f94956b` (recomputed for this record).
- Producer evidence package `Solo/gagamba-alt-b-p1-impl-01`; its `SHA256SUMS.txt` SHA-256 `90910848564faa72ab1d38408f854df119ff7ff625fe2ddb750555e4ccb84291`; reviewer confirmed all 11 producer manifest entries match.
- Reviewer independently reproduced, on Windows .NET SDK 10.0.401 / VSTest 18.7.0, using hash-matched producer Debug binaries (test DLL SHA-256 `1c041bb6fb30ba493fc9423fd33e8718168cb36f42fd1669aa1f297da0203a3e`): `OwnerAuthorityContextTests` 10 passed / 0 skipped / 0 failed; the focused regression filter 147 passed / 20 skipped / 0 failed (167 total); full Windows M1 runner 1276 passed / 331 skipped / 0 failed (1607 total). Results agree with the producer.
- Reviewer probe independently exercised manifest-mutation refusal and null echo against real decision logic and inspected constructors; the copied Release assembly SHA-256 `1015dd35da18fb94d2cf1b46d0b9e5c40195b933d8ad90297768402a1d30b3a7` matches the producer, with `ForTest` and the reported hook types/properties absent.
- Producer evidence: [P1 implementation evidence](../evidence/rc2b-06-b6-alt-b-p1-impl-01.md).

## 5. Retained observations (accepted with the implementation)

- **O1 — echo ordering.** The later echo mismatch is detectable only after writer acquisition and `WriterReady`; this reflects existing sequencing and is codified in section 3. P2-P4 and native qualification must address their separately approved obligations. Non-blocking; no P1 defect.
- **O2 — hash-matched binaries.** The independent suite ran previously built, hash-matched producer binaries rather than an independently rebuilt assembly; the source-to-binary relationship rests on producer build evidence, hashes and review, not an independent clean rebuild. No native qualification follows. (See qualification 2.)

## 6. Explicit qualifications (non-waivable)

1. **Effect ordering.** The early context/wire check precedes journal and writer acquisition; the later echo comparison occurs after writer acquisition, the existing allocation-intent proof and `WriterReady` publication. This exact ordering distinction is preserved in section 3; no document may claim that all initialization effects occur after the later echo.
2. **No independent rebuild.** Independent tests used hash-matched producer binaries; there was no separate independent rebuild of the accepted source.
3. **Reviewer environment.** The independent reviewer executed in Codex, not the originally named Terra High environment.
4. **Native skips unqualified.** The 331 skipped native cases remain unqualified and are not passes.
5. **Scope limit.** P1 does not establish final-admission enforcement and does not grant validation authority.

## 7. Preservation and change scope

- Work remains on canonical `main`; one canonical checkout at `C:\Users\Laszlos\source\repos\Gagamba`.
- No branch, linked worktree, reset, rebase, stash, clean, prune, force operation or push.
- The 44 historical untracked paths remain unchanged (`git status --porcelain -uall` reports exactly 44 untracked paths; the collapsed `git status --porcelain` view reports 28 directory-level entries).
- This decision is additive and documentation-only. No production source or test code is changed. Historical evidence, reviews, reviewer bytes and prior owner decisions remain unchanged.
- No P2-P4, Slice E or Slice N implementation, no native infrastructure provisioning, no package, publication or production activation.

## 8. Disposition and next gate

**P1 SOURCE IMPLEMENTATION ACCEPTED WITH EXPLICIT QUALIFICATIONS. SLICE P1 ONLY; P2-P4, SLICE E, SLICE N, NATIVE QUALIFICATION AND PRODUCTION ACTIVATION REMAIN UNAUTHORIZED.**

The next steps — owner manifest authorization (P2), owner pre-RELEASE generation gate (P3), pre-effect sink refusal (P4), the atomic validation-bootstrap integration (Slice E) and protected native qualification (Slice N) — each require a separate explicit owner authorization. This acceptance starts no further work automatically and creates no general-admission or validation authority.
