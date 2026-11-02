# RC2B-06 B6-IMPL-01-S2 — Implementation Commission Handoff (Planning Evidence)

**Status:** Prepared handoff — NOT an implementation authorization
**Predecessor:** GAGAMBA-RC2B-06-B6-IMPL-01-S1 — ACCEPTED WITH QUALIFICATIONS (`docs/decisions/rc2b-06-b6-impl-01-s1-owner-acceptance.md`)
**Date:** 2026-11-02
**Canonical baseline:** `main` (S1 acceptance chain complete through `401e1ea7`; Q2 evidence preservation `5ad9388`)

---

## 1. Objective

S2 should establish the core installed-artifact trust mechanism for B6, consistent with the accepted B6 architecture, HD-1–HD-5, the accepted wire protocol, S1's production/test separation, future native closure qualification, and existing B1–B5 authority restrictions. B6 overall remains **experimental, installed-artifact trust not yet implemented or qualified.**

## 2. Recommended scope

1. **Immutable installed generations.** Installed artifact sets are immutable once created; no in-place mutation of a published generation.
2. **Canonical artifact manifest and artifact-set digest.** A canonical, hash-addressed manifest binding every installed artifact byte (see §4 on cross-root variance).
3. **Qualification-record association.** Each generation carries explicit qualification records stating what was qualified, against which source revision, and with which qualifications.
4. **Root-owned installation receipt.** A trusted, root-owned record of the installation act (who/what/when/source identities), per HD-1 policy scoping.
5. **Active-generation selection and verification.** Deterministic selection of the active generation with verification before use.
6. **Fail-closed trust verification.** Missing, substituted or conflicting artifacts refuse before dispatch; consistent with the standing rule that unsupported policy, missing mechanisms and setup failure reject before target dispatch, with no unrestricted fallback.
7. **Preservation of prior generations and recovery authority.** Generation retention per HD-4 (no automatic garbage collection); accepted recovery authority semantics preserved.
8. **Safe refusal for missing, substituted or conflicting artifacts.** Typed, diagnosable refusals; no silent repair of trust state.

## 3. Consistency requirements

- Accepted B6 architecture and B6 owner decisions (HD-1–HD-5) — unchanged.
- Accepted wire protocol: `RecoveryConfigV2` (bootstrap v2), RecoveryMessage 9/10, OwnerProtocolMessageType 36 — unchanged; S2 consumes it, not redefines it.
- S1 production/test separation: production artifacts exclude the seven accepted qualification-only surfaces; instrumented variants retain them; S2 must not weaken the S1 build/binary invariants.
- Future native closure qualification: S2 must leave native dependency closure (host/runtime, ICU/globalization alternative, OpenSSL, zlib, loader, Bubblewrap, Seccomp, RID-specific artifacts) explicitly outstanding.
- Existing B1–B5 authority restrictions: no RELEASE authority change, no production activation.

## 4. S2 sequencing (mandatory)

- **Do not begin by implementing wire echo messages.** The expected installation identity must originate from the trusted installed-generation mechanism.
- The wire handshake (`InstallationEchoRequest`/`Response`, `OwnerInstallationEcho`) must later **consume** that identity rather than invent it. Wire-identity implementation follows generation/receipt establishment, not precedes it.

## 5. Determinism caveat (from S1/Q2)

Q2-OBS-03 established that binary hashes are stable within an unchanged build root but differ across isolated output roots (path-derived PE inputs such as SourceLink/PDB references). S2 manifest design must therefore bind **actual installed bytes** as observed at installation time, and must not assume cross-root or cross-environment byte equality of separately built copies.

## 6. Privilege boundary

- HD-1 allows narrowly scoped privileged installation within disposable WSL2 qualification environments, **subject to explicit execution authorization**.
- This S1 owner-acceptance commission does **not** authorize executing any privileged installation command.
- The future S2 specification must explicitly distinguish ordinary installer implementation (code and tests, unprivileged) from actual privileged installation execution (separately authorized, disposable WSL2 environment only).

## 7. S2 exclusions (require separate authorization)

S2 must not automatically authorize:

- Production restricted provider activation.
- Public B7 functionality.
- Hufu integration.
- VM/HCS implementation.
- Native Linux-host acceptance (B5-L01 remains open).
- Arm64 support (unsupported under HD-3).
- Automatic generation garbage collection (HD-4).
- Broad public API expansion.

## 8. Inherited obligations carried into S2

- System.IO.Hashing via SourceLink dependency-metadata observation (Q1 FINDING-004 lineage; preserved by Q2).
- Full native dependency closure and exact runtime-consumed closure.
- Native execution qualifications: WSL2 x86-64 B6 qualification, native Linux-host B5-L01, all environmental skip classes.
- Inherited clock-constructor qualification (`Reconciler(IDomainClock, TimeSpan)` internal seam) — open, not remediated by S1.
- Trusted root-owned qualification-record enforcement and forged-record rejection (T24-G boundary).

## 9. Non-goals

This document is planning evidence only. It authorizes no implementation, no commission start, no privileged execution, no push and no production activity. A subsequent owner commission must authorize S2 implementation explicitly.
