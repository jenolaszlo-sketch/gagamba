# Slice E-1 F-01 implementation report and Q2 handoff

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-IMPL-01`
Date: 2026-11-07 (Asia/Manila). Producer disposition: **IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION**.

Implementation `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f`, tree `7edfedd6badebda029ce1e3371445d973c6fd82b`, from baseline `9a8007fb923174f00dd5954ab47b8fdf4866ebba`, tree `b465805f7d6e50e6f4d59f734cae008cc02be1c8`. Authorized by `docs/decisions/rc2b-06-b6-alt-b-slice-e-01-f01-remediation-entry-01.md`.

Read the durable evidence: [F-01 implementation evidence](../evidence/rc2b-06-b6-alt-b-slice-e-01-f01-impl-01.md).

## Exact change and claim

Two files, +77 lines:
- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs` — in the private `FromEvidence`, three checks binding owner/recovery closures to `InstallationIdentity.RoleHash(manifest,1/0)` and runtime inventory to `InstallJson.Hex(manifest.Platform,"runtimeInventorySha256")`.
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/PrepublicationIntegrityProjectionTests.cs` — positive control, the Q1 B5/B6/B7 reproduction theory, and two fail-closed cases.

**Claim:** the E-1 projection's derived fields are now deterministic functions of the authoritative manifest; a structurally valid but semantically incorrect closure or runtime inventory is refused. Refusal-only, production-unreachable, no authority. F-01 is **not** declared closed.

## Producer execution (exact counts on the committed revision)

- Debug and RELEASE M1 builds: 0 warnings / 0 errors.
- E-1 focused: **23 passed / 0 failed / 0 skipped** (was 17; +6).
- Integrity/authority/release filter: 238 passed / 32 skipped / 0 failed.
- Full Windows M1: **1391 passed / 331 skipped / 0 failed** (1722); the 331 native skips are not passes.
- RELEASE production-assembly verifier PASS; Debug negative control FAIL naming `PrepublicationIntegrityProjection::ForTest`.
- Gate-removal control (external scratch, canonical source untouched): committed refuses B5/B6/B7; gate-removed copy accepts them.
- RELEASE IL: `FromEvidence` called only by `Verify`; `Verify` has no caller; `ForTest` absent. Diff confined to the two authorized files (P4-neutral).

## Required independent challenges (Q2)

1. **Exact semantics.** Confirm the three relationships against `InstallationIdentity.RoleHash` (`InstallationIdentity.cs:35–45`) and the manifest runtime-inventory value, and that no alternative normalization was introduced.
2. **Construct original adversarial inputs.** Do not reproduce the producer tests. Build semantically incorrect but structurally valid tuples through the private/test boundary and verify refusal; confirm a consistent tuple still builds.
3. **Q1 closure.** Re-run the Q1 B5/B6/B7 cases and confirm they now refuse; record the changed outcome.
4. **Load-bearing gate.** Independently reproduce a gate-removal control (external mutated copy) or inspect the committed IL to show the checks execute on the construction path.
5. **Trust boundary.** Confirm the manifest is authoritative only via `Verify()` (fixed-root verifier) and that no new construction route or caller-fed factory exists.
6. **RELEASE isolation.** Confirm no `ForTest` and no validation-authority factory in RELEASE; no new public API.
7. **Production unreachability.** Confirm no production consumer / new sink caller; P4 neutrality for the exact revision (F-R1/O-R1, O-R2, O-R3, residual, `NATIVE-PENDING`, 331 skips preserved).
8. **Immutability/provenance.** Confirm private constructor, internal visibility, get-only fields, null qualification association, and provenance classifications unchanged.

A new qualification report must bind to the exact implementation revision.

## Limits

Native protected `Verify()` success and all protected positives remain `NATIVE-PENDING` (Slice N). The runtime loaded-role check remains deferred. F-02 (duplicated `VerifyActive`/`VerifyActiveIntegrity` logic) and F-03 (`eng/test-production-build.ps1` PowerShell 5.1 defect) remain open and untouched. Preserve one canonical main worktree and all 44 historical untracked paths; do not push, branch, provision, or start E-2/Slice N automatically. Only a later `F-01-OWNER-ACCEPTANCE` may formally close F-01.
