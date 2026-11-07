# Slice E-1 F-01 owner acceptance — semantic integrity remediation closure

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-OWNER-ACCEPTANCE-01`

**F-01 CLOSED WITH QUALIFICATIONS**

Date: 2026-11-07 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This decision formally closes the demonstrated E-1 F-01 semantic integrity defect **within the accepted E-1 refusal-only, production-unreachable boundary**. It does not enable positive validation, does not qualify the projection for production integration, does not close F-02 or F-03, and does not authorize E-2 through E-7, Slice N, or activation.

## 1. Verified starting baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `b31a06282df600af631054f5f44a85c4529098a5` (matches the commission) |
| Starting tree | `38fa0821c58d460079dc910c24df891dd1f89fd0` (matches the commission) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (local `main` four commits ahead; remote unchanged) |
| Push | none |

Baseline matched exactly. No reset, clean, rebase, prune, merge or extra worktree was used. Independent checks for this record: `git diff 965f203 b31a062 -- src tests` is empty (the reviewed implementation is exactly the committed source); the non-`docs` delta baseline→HEAD is exactly the two authorized files.

## 2. Exact implementation and qualification revisions

| Role | Revision |
|---|---|
| Reviewed implementation | `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f`, tree `7edfedd6badebda029ce1e3371445d973c6fd82b` |
| F-01 remediation authorization | [remediation entry](rc2b-06-b6-alt-b-slice-e-01-f01-remediation-entry-01.md) |
| Producer implementation report / evidence | [report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f01-impl-01.md) / [evidence](../evidence/rc2b-06-b6-alt-b-slice-e-01-f01-impl-01.md) |
| Independent qualification | [Q2 report](../reviews/rc2b-06-b6-alt-b-slice-e-01-f01-q2.md) / [Q2 evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-f01-q2-evidence/) |
| Original E-1 finding | [Q1 report](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md) |
| Decision baseline | `b31a062` / tree `38fa0821` |

## 3. Owner evaluation of semantic correctness

The authorized remediation added, in the private `PrepublicationIntegrityProjection.FromEvidence`, three equality checks against authoritative manifest-derived values (`src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs:95–98`):

- `OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1)`
- `RecoveryClosureSha256 == InstallationIdentity.RoleHash(manifest, 0)`
- `RuntimeInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256")`

Independently confirmed authoritative and correctly bound:

- `InstallationIdentity.RoleHash` (`Installation/InstallationIdentity.cs:35–45`) is the single repository role-closure definition, with discriminator `role == 0 ? "recovery-" : "owner-"` — so `RoleHash(…,1)` is exactly the owner closure and `RoleHash(…,0)` the recovery closure. The artifact bytes used are the manifest's role artifacts.
- `InstallJson.Hex(JsonElement, key, length=64)` (`InstalledManifest.cs:80`) reads the manifest platform value; that value is also host-bound by `InstalledPayload.CheckHost` (`InstalledPayload.cs:145–150`) in the verifier.
- The authoritative manifest reaches `FromEvidence` only through `Verify()` → `VerifyActiveIntegrity()` (fixed-root reads with digest-to-`active` binding, payload closure, stability and host checks). `ForTest` is Debug-only.
- A failed comparison throws `InvalidDataException` before the projection is constructed; there is no catch around the checks and no fallback.

The owner accepts that the three relationships are enforced against authoritative manifest evidence and that their failure prevents trusted projection construction.

## 4. Q1-to-Q2 before/after evidence

| Case | Q1 (pre-fix) | Q2 (post-fix) |
|---|---|---|
| B5 incorrect owner closure | ACCEPTED | refused |
| B6 incorrect recovery closure | ACCEPTED | refused |
| B7 incorrect runtime inventory | ACCEPTED | refused |
| Consistent evidence | accepted | accepted |

Independently reproduced in Q2 with original probes (`...q2-evidence/probe-results-committed.txt`, `failures=0`): the invalid cases refuse (`InvalidDataException`); a **swapped** owner/recovery closure refuses; a **role-specific owner-artifact substitution** carrying the old closure refuses; a correctly rebuilt consistent tuple is accepted (positive control). Refusals are therefore caused by semantic inconsistency, not incidental parsing/fixture failure.

## 5. Independent gate-removal evidence

Q2 constructed a fresh disposable mutation (external scratch, canonical source/history untouched) deleting the two semantic `Require` statements. Retained artifacts:

- Committed candidate: `probe-results-committed.txt` (`failures=0`); retained Q1 probe B5/B6/B7 `refused`.
- Gate-removed candidate (Debug M1 SHA-256 `76419C5E918570282F232B8CAC1DB3572897D1948034E8C915E32F88DC6ECA2E`): `probe-results-gateremoved.txt` (`failures=7`); the Q1 probe returns B5/B6/B7 to `ACCEPTED`.

Removing the specific comparisons changes refuse→accept; restoring the committed source restores refusal. The owner accepts this as demonstrated causality: the new checks are load-bearing. No further gate-removal experiment is required.

## 6. RELEASE isolation and production reachability

- RELEASE production-assembly verifier **PASS** (0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories); Debug `--production` negative control **FAIL** as expected, naming `PrepublicationIntegrityProjection::ForTest`.
- Compiled RELEASE IL: the only methods referencing the E-1 types outside the E-1 type are `InstalledFileSystem::VerifyActiveIntegrity` (evidence construction) and the `InstalledIntegrityEvidence` record members; `FromEvidence` is called only by `Verify`; **`Verify` has no caller**; `ForTest` is absent. No validation-authority factory, no new public API, no new protected effect; `QualificationId`/`QualificationRecordSha256` remain `null`. The trusted construction boundary has not expanded.

## 7. P4 neutrality

The non-`docs` delta baseline→HEAD is exactly `PrepublicationIntegrityProjection.cs` (+14) and `PrepublicationIntegrityProjectionTests.cs` (+63). No `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/`, `InstalledFileSystem*.cs` change: F01/F02 enforcement, S10/S11 domination, S28 reachability, B3 constructor authority, provider gates, first protected effects, qualified→legacy downgrade refusal, RELEASE refusal and R/O/C ordering are unchanged. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 unqualified native skips are preserved.

## 8. Formal F-01 disposition

**F-01 CLOSED WITH QUALIFICATIONS.** Within the accepted E-1 refusal-only boundary the original defect is corrected: all three semantic relationships are enforced against authoritative manifest evidence; independently constructed incorrect inputs refuse while valid relationships remain constructible; the checks are demonstrably load-bearing; and no unauthorized authority or execution path was introduced.

**Meaning and limitations of closure.** Closure means only the above. It does **not** mean positive validation is enabled, that `OwnerAuthorityContext.Validation` is constructible, that a production consumer exists or is authorized, that the projection is qualified for production integration, that protected native positives are qualified, or that E-2 through E-7 are authorized. F-02 and F-03 remain open; native remains `NATIVE-PENDING`; the 331 native skips remain unqualified and are not passes. F-01 closure is not incremental authorization to construct trusted positive validation authority.

## 9. Binding future consumer exception-handling requirement

Before any production consumer of the E-1 projection is authorized, it must be demonstrated that **every** integrity-verification exception and failure mode results in fail-closed behavior — no fallback, downgrade, partial authorization, or continuation into a protected effect. This covers at least: semantic mismatches (`InvalidDataException`), missing role material (`InvalidOperationException`), missing runtime metadata (`KeyNotFoundException`), and any future failure mode. Specific binding:

> **F-01 consumer-integration obligation (binding):** any future RELEASE consumer of `PrepublicationIntegrityProjection` must treat any construction failure as a hard refusal and must not catch a subset of exception types in a way that permits continuation. Exception-type normalization is not required; correct fail-closed behavior is required.

This obligation does **not** reopen the demonstrated semantic defect; it governs future integration only. No consumer exists today, so it is not currently exercised.

## 10. Preserved F-02 and F-03 findings

- **F-02 (open).** `VerifyActive`/`VerifyActiveIntegrity` duplication was neither consolidated nor altered by F-01. A fresh parity assessment is required whenever either verification path changes, and a documented consolidation-versus-parity decision is required before production integration. F-01 closure does not resolve F-02.
- **F-03 (open).** The `eng/test-production-build.ps1` Windows PowerShell 5.1 encoding defect is unresolved. The standalone production assembly verifier is independently qualified, but the complete harness is **not** established as passing and must not be recorded as PASS. Preserve F-03 for bounded tooling remediation and requalification.

## 11. Slice E / Slice N exclusions and prerequisites

Preserved sequence and prerequisites (none advanced by this decision):

- E-2 K/M/F/S integrity and validation-catalog approval; E-3 v3-pre codec; E-4 GAVA1 (fresh P4 re-audit); E-5 launcher/custody interface; E-6 atomic positive-validation enablement and R/O/C correction (fresh P4 re-audit); E-7 driver; **Slice N** protected native qualification.
- Frozen-schema provenance reconciliation and the validation-catalog approval-chain owner decision remain unresolved prerequisites.
- No positive validation, atomic bootstrap, launcher, custody/privilege drop, validation-authority factory, protected native execution, public API or activation is authorized.

## 12. Recommended next commission

A separate bounded commission for **F-03 PowerShell 5.1 verification-harness remediation** (contained tooling defect with a known reproduction, correctable independently of Slice E architecture). F-02 verification-parity controls and the E-2 architecture prerequisites remain subsequent work requiring their own authorization. None is authorized or started automatically.

## 13. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. The original F-01 finding, the Q1/Q2 evidence and the implementation evidence are preserved unchanged. The external acceptance receipt is under `Solo/gagamba-alt-b-slice-e-01-f01-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No production or test change, no F-02/F-03 remediation, no E-2 through E-7 work, no Slice N, no validation authority, no activation, no branch/worktree, no reset/rebase/clean/prune, and no push. All 44 historical untracked paths preserved. **Stop after committing this owner decision.**
