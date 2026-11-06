# RC2B-06 B6 — Alternative B P2 owner sequencing clarification

**P2 SEQUENCING CLARIFICATION — ADDITIVE, NOT A REWRITE OF THE ORIGINAL OWNER DECISION**

Date: 2026-11-06 (Asia/Manila)

Decision: `GAGAMBA-RC2B-06-B6-ALT-B-P2-SEQUENCE-REM-01`

Remediation commission: `GAGAMBA-RC2B-06-B6-ALT-B-P2-SEQUENCE-REM-01`

Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`

Canonical baseline: commit `ec9758a69c8d1946b76d7d5e8e5578102c41bd63`, tree `bff333b79f270d2adf55021f81a3a9d8a2ac6837`

## 1. Why this clarification is recorded

Independent P2-Q1 returned **P2-Q1 DESIGN DECISION REQUIRED** (`Solo/gagamba-alt-b-p2-q1`, `REPORT.md` SHA-256 `a389894c9555ac42353db4cbe6564e0155c200a7d02040f5e9a25e69c61428f7`). It found no demonstrated bypass of the existing operation-bound owner manifest gate, but observed a sequencing/scope conflict: the accepted [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) §3.1 requires absent, stale, withdrawn or mismatched authority to be refused **before new allocation and staging**, and the accepted [implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) §3.2 exit also names the writer and domain, while the P2 implementation performed journal, writer, allocation/domain and staging effects before its owner final-admission check. P2-Q1 finding **F1** classified this as an OBSERVATION requiring an owner design decision, not a demonstrated unauthorized manifest grant (`findings.md` SHA-256 `306011dc827668864dcfebd069c005dfbb4d044e214816ae55a5db23914d20b7`; effect ordering in `effect-ordering.md` SHA-256 `f0c424c3950b8fec7a40cabf322c9a019c75c49c1cb26404b237b8953b1dfacc`).

This document records the owner's sequencing decision. It is **additive**. It does not rewrite, relax or replace the original owner decision, the accepted P1 decision, the implementation handoff, the P2 evidence or the P2-Q1 review. Where the original decision requires pre-effect authorization, this clarification preserves that requirement by adding an explicit early check; it is not a waiver.

## 2. Owner sequencing decision

The owner selects this interpretation of the accepted Alternative B authority contract:

> Qualified integrated execution requires valid final admission **before its first protected initialization effect**, and a fresh final-admission revalidation **before its operation-bound manifest grant**.

The two checks protect different transitions:

- **Early check.** Prevent unauthorized initiation of journal, writer, domain, allocation or staging effects. It runs after the trusted first installation verification and before the first protected initialization effect.
- **Late check.** Prevent manifest authorization after acceptance withdrawal, generation replacement or other authority invalidation. It is the existing operation-bound P2 recheck and remains independent and fresh.

A late refusal may occur after earlier effects that were initiated under valid admission. Those effects must then be handled by the existing safe stopping and cleanup mechanisms. The early check refuses **without** journal, writer, domain, allocation or staging effects when the initial evidence is missing, unavailable, malformed, withdrawn, stale or mismatched.

This preserves the stronger existing owner contract. It is a clarification, not a waiver of pre-effect authorization.

## 3. Exact accepted pre-effect ordering

Baseline `OwnerPreparationTransaction.RunTransaction` (`src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`, baseline tree `bff333b7…`) already refuses a retention/wire mismatch before the first effect, but performed the only final-admission check after staging, immediately before the `Prepared` transition. The clarification adds the early check where the owner contract requires it.

The corrected qualified integrated order is:

1. Trusted first installation verification — `QualifiedOwnerPreflight.Verify` (owner preflight) returns `OwnerAuthorityContext.Qualified.FromVerified(LoadedInstallation)` with the full retained identity, manifest, qualification ID and raw record hash.
2. Retained authority requirement — `RunTransaction` requires the actual retained `LoadedInstallation` before any effect.
3. **Early final-admission check** — the same shared `OwnerManifestAdmissionGate.TryRequire` decision, bound to the retained identity/qualification/record tuple, before `new JournalFileStorage()`.
4. Journal storage, ownership journal, exclusive writer acquisition and allocation-intent proof; `WriterReady`.
5. Direct authentication and the retained-tuple echo (P1 ordering unchanged; the echo still occurs after writer acquisition and `WriterReady`).
6. Allocation context, domain creation/transfer, registered state.
7. Staging directory/files and staged-object description.
8. Manifest authorization decode and identity binding.
9. **Late operation-bound final-admission recheck** — an independent fresh `TryRequire` before `Prepared`, capability mint and HELD.
10. `Prepared` record/ack, publisher and capability, HELD launch and release phase.

The exact source line anchors at the implementation revision are recorded in the remediation [evidence](../evidence/rc2b-06-b6-alt-b-p2-sequence-rem-01.md) §5 and are bound to the implementation commit's tree.

## 4. Relationship to the original decision and handoff

- The original owner decision §3.1 ("refuse … before new allocation, staging, capability minting or HELD") and the handoff §3.2 exit ("refusal before new owner writer, domain, staging, capability or HELD effects") are satisfied for the **integrated direct owner path** by the early check for the absent/stale/withdrawn/mismatched cases the owner contract names.
- The original decision §3.1 "repeat the protected final-admission check for the operation-bound manifest acceptance" remains satisfied by the late check.
- P4's complete pre-effect sink inventory, the non-integrated B2/`QualifiedB2Owner` route, P3, Slice E and Slice N are **not** closed by this clarification. They remain open obligations described by the handoff.
- No new authority, writer, deadline, validation path or production activation is created. Validation remains non-constructible.

## 5. Preservation and scope

- Work remains on canonical `main`; one canonical checkout at `C:\Users\Laszlos\source\repos\Gagamba`.
- No branch, linked worktree, reset, rebase, stash, clean, prune, force operation or push.
- The 44 historical untracked paths remain unchanged.
- Prior owner decisions, evidence, reviews and reviewer bytes remain unchanged.
- This clarification is documentation; the source remediation is committed separately.

## 6. Disposition

**P2 SEQUENCING CLARIFIED — EARLY PRE-EFFECT FINAL ADMISSION REQUIRED; LATE OPERATION-BOUND RECHECK RETAINED. NOT A REWRITE OF THE ORIGINAL OWNER DECISION.**
