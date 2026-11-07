# Slice E-1 owner acceptance — non-forgeable prepublication integrity projection

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-OWNER-ACCEPTANCE-01`

**E-1 ACCEPTED WITH QUALIFICATIONS**

Date: 2026-11-07 (Asia/Manila). Recorder: DeepSeek. Scope: documentation-only owner decision on canonical `main`; no push.

This decision accepts only the completed **E-1 refusal-only prepublication integrity projection** at the reviewed implementation revision. E-1 is complete **as a bounded refusal-only milestone**, not as a positive validation capability. This acceptance does not authorize implementation, remediation, positive validation, production activation, or progression into E-2.

## 1. Verified starting baseline and exact revisions

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `db832452474c378d2c1ed8f515325c23f9ee7aab` (matches the commission) |
| Starting tree | `ccd5c1f0d68a8b21416cf683fbd8eca20d2df813` (matches the commission) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| Accepted implementation revision | `2259fce59b44b6af6448a38685a0635d560d0900`, tree `77610b49ecae5398d633f1db43d4f01604694ee1` |
| Independent qualification revision | `db832452474c378d2c1ed8f515325c23f9ee7aab`, tree `ccd5c1f0d68a8b21416cf683fbd8eca20d2df813` |
| Push | none |

The implementation was independently confirmed unchanged from `2259fce` to the decision baseline: `git diff --stat 2259fce db83245 -- src tests eng` is empty. The accepted revision is therefore exactly the revision reviewed by independent Q1.

## 2. Evidence reviewed

Reconciled against the actual records (not merely producer/reviewer conclusions):

- [E-1 entry owner authorization](rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) — authorizes only E-1 (§§4–8); anticipates partial/fail-closed outcomes and the Q-E1–Q-E5 qualifications.
- [Slice E entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md) — reconstruction, prerequisites and P4 interaction.
- [Implementation handoff](../reviews/rc2b-06-b6-alt-b-slice-e-01-impl-01.md) and [implementation evidence](../evidence/rc2b-06-b6-alt-b-slice-e-01-impl-01.md).
- [Independent Q1 qualification report](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md) and its [reviewer evidence](../reviews/rc2b-06-b6-alt-b-slice-e-01-q1-evidence/).
- [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md) §§3.3 and 3.5; [Mechanism B acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md); [F02 trust-store decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md).
- [P4 coverage reconciliation](../evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md) and [P4 owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md).
- Implementation source: `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs`, `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.Prepublication.cs`, and the `eng/VerifyProductionAssembly/Program.cs` delta.

Independent checks performed for this record: exact baseline/tree/branch/worktree/44 untracked paths; implementation unchanged since `2259fce`; the F-01 cross-check gap present in source at the accepted revision; no production caller of `PrepublicationIntegrityProjection.Verify()`.

## 3. Formal disposition

**E-1 ACCEPTED WITH QUALIFICATIONS.**

The independent Q1 disposition **SUPPORTED WITH QUALIFICATIONS** is accepted. No blocking defect, unauthorized execution path, or failed required invariant was demonstrated. E-1 is accepted only within the scope in §5 and subject to the binding F-01/F-02/F-03 qualifications in §6.

## 4. Acceptance question and claims

The authorized scope: an internal, non-forgeable, refusal-only prepublication integrity projection that remains unreachable from production execution paths and cannot independently construct validation authority. Determination, separating demonstrated claims, boundary-limited claims, defects, and exclusions:

**A. Construction boundary — accepted.** `PrepublicationIntegrityProjection` is internal, sealed and immutable with get-only properties; its constructor is private; the only RELEASE construction route is `Verify()`, which invokes `InstalledFileSystem.VerifyActiveIntegrity()`; `FromEvidence` is private; the Debug-only `ForTest(InstalledIntegrityEvidence)` seam is compile-time excluded from RELEASE and permanently tagged `DeterministicTestOnly`. Ordinary caller-provided hashes, identities, manifests, result objects or flags cannot directly construct trusted state. Non-forgeability is claimed only against ordinary callers and untrusted evidence, **not** against arbitrary privileged code inside the trusted process (reflection is outside the accepted threat model).

**B. Verification behavior — accepted, with limited guarantees.** `VerifyActiveIntegrity` reproduces the shared verification prefix of `VerifyActive` (identical fixed-root/payload/manifest/receipt/stability reads via the same private primitives), adds an unconditional `CheckHost`, and intentionally omits the qualification-record association check — consistent with the null-association contract. The qualified path is byte-identical and untouched (no diff in `InstalledFileSystem.cs`). The resulting projection's guarantees are **limited to prepublication integrity**; the Gate C runtime loaded-assembly "verification-to-use" check is **not** performed and remains an explicit deferred obligation.

**C. Production isolation — accepted.** Independent RELEASE IL analysis supports: no production caller of `Verify()`, no production consumer of the projection, no RELEASE `ForTest`, no validation-authority factory, no positive validation activation, and no new protected effect. A type compiled into RELEASE is not equivalent to a type reachable from production execution; the E-1 chain is dead code in RELEASE.

**D. P4 preservation — accepted.** E-1 is P4-neutral for the reviewed revision: the non-`docs` diff from the P4 reconciliation source to `2259fce` is only the four E-1 files; `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/` and `InstalledFileSystem.cs` are unchanged. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 unqualified native skips are preserved. P4 acceptance is **not** expanded and no previously accepted boundary is reopened.

## 5. Exact acceptance boundary

If accepted, only the following is accepted, tied to revision `2259fce`:

1. The internal E-1 projection and its existing trusted construction route.
2. Immutability and type-level separation from qualified installed identity (null `QualificationId`/`QualificationRecordSha256` on every path).
3. The independently verified RELEASE isolation (no `ForTest`, no validation factory, no new public API).
4. The current production-unreachable status.
5. Existing refusal behavior within the qualified test environment.
6. P4 neutrality for the reviewed revision.
7. The independent Q1 evidence, subject to F-01 through F-03 and the other preserved limitations.

This acceptance must **not** imply: validation authority is constructible; positive validation can execute; a trusted validation catalog exists; K/M/F/S approval is implemented; GAVA1 or v3-pre is implemented; atomic R/O/C validation ordering exists; native protected positives are qualified; production activation is authorized; or E-2 through E-7 are authorized.

## 6. Binding findings and qualifications

**F-01 — missing semantic cross-checks (mandatory closure prerequisite).** Independent adversarial probes demonstrated that `FromEvidence` accepts structurally valid but semantically incorrect owner closure, recovery closure and runtime inventory: it checks only well-formed 64-hex and that `ArtifactSetSha256 == manifest.Digest`; it does **not** cross-check `OwnerClosureSha256`/`RecoveryClosureSha256` against `InstallationIdentity.RoleHash(manifest,1/0)` or `RuntimeInventorySha256` against `platform.runtimeInventorySha256`. This is non-blocking today because `FromEvidence` is private, the production route uses verified evidence, and the construction chain has no RELEASE consumer. **F-01 must be closed before:** the first production consumer of the projection, any reliance on its semantic integrity for an authorization decision, or any expansion of its trusted construction boundary. Required remediation: establish the actual relationships against authoritative evidence (not merely well-formedness). The later independent reviewer must construct semantically incorrect but structurally valid inputs and verify refusal. **Not remediated under this commission.**

**F-02 — duplicated verification logic (tracked obligation).** `VerifyActiveIntegrity` partially duplicates `VerifyActive`. Current qualified behavior was independently compared and supported, but duplicated security-sensitive logic risks future divergence. **When either verification path changes, a parity analysis is required.** Before any production integration relying on the projection, a fresh assessment must decide between consolidating shared verification logic and explicit parity tests/maintenance controls. A risky refactor is not automatically required. **Not modified under this commission.**

**F-03 — PowerShell 5.1 verification-harness defect (tracked tooling obligation).** The `missing-symbol` negative control correctly emits `GAGAMBA002` and exits non-zero, but `eng/test-production-build.ps1`'s PS 5.1 logging path mixes encodings (BOM-less `Set-Content` header then UTF-16LE `>>` output), so its `Get-Content -Raw` diagnostic-string assertion fails. The production assembly verifier itself was independently confirmed correct. Recorded: the complete PowerShell verification script **did not PASS**; the underlying assembly-verifier negative control **behaved correctly**; F-03 is a test-harness compatibility defect, not a demonstrated E-1 security defect. A bounded tooling correction and independent rerun are required before relying on the full script as passing evidence. **Not altered under this commission.**

Non-blocking observations retained from Q1: O1 (evidence-record constructor flags on an internal type), O2 (intentional record-association omission and the stronger `CheckHost`), O3 (failure typing not uniformly `InvalidDataException`, still fail-closed), O4 (RELEASE checks rely on IL/metadata inspection and the canonical verifier; no native protected positive).

## 7. Test evidence

Independently reproduced (see Q1 report §11); not upgraded beyond what was demonstrated:

| Verification | Result |
|---|---|
| Debug build | 0 warnings, 0 errors |
| RELEASE build | 0 warnings, 0 errors |
| E-1 tests | 17 passed / 0 failed / 0 skipped |
| Focused integrity/authority/release | 232 passed / 32 skipped / 0 failed |
| Full Windows M1 suite | 1,385 passed / 331 skipped / 0 failed |
| RELEASE production verifier | PASS |
| Debug negative control | Expected rejection (names `PrepublicationIntegrityProjection::ForTest`) |
| PowerShell 5.1 full harness | FAIL, classified F-03 |

The 331 skipped native tests are **unqualified, not successful**. The full suite was not rerun for this documentation decision; no material evidence contradiction required a specific check.

## 8. Preserved architectural and native qualifications

Remaining Slice E prerequisites (from the entry review) remain in force and are not satisfied by E-1:

- Frozen-schema provenance reconciliation for K/M/F/S, GAVA1, v3-pre and release correlation (Q-E1).
- A separate owner decision for the validation-catalog approval chain (Q-E2).
- E-4/E-6 P4 re-audits (Q-E5).
- E-6 atomic enablement and the R/O/C ordering correction (Q-E3/Q-E4).
- Independent root authorizer and fixed launcher; protected custody and privilege-drop requirements.
- E-7 driver qualification.
- Slice N protected native qualification.

Positive validation must remain non-constructible until the complete accepted atomic-enablement boundary is established. A series of preparatory package acceptances must not become implicit incremental activation authorization. `NATIVE-PENDING` and the 331 unqualified native skips are preserved; the observation-to-effect residual is unchanged.

## 9. Mandatory closure prerequisites and authorization boundaries

- **F-01** is a mandatory closure prerequisite before production integration, semantic reliance, or trusted-boundary expansion (see §6).
- **F-03** must be corrected and independently requalified before the full verification script is cited as passing.
- Any future change to either verification path requires the **F-02** parity analysis.
- No production consumer of the projection may be introduced without a fresh P4/authority audit and an explicit authorization.

## 10. Recommended next commissions

1. **F-01 hardening:** semantic cross-checks (`RoleHash`, platform runtime inventory) plus adversarial regressions that submit semantically incorrect but structurally valid inputs. Prioritize before any production consumer.
2. **F-03 tooling correction:** PowerShell 5.1 encoding fix and verification-harness requalification.
3. **F-02 divergence control:** assess shared verification extraction versus explicit parity coverage before the relevant production-integration milestone.
4. **E-2 entry prerequisites:** frozen-schema provenance reconciliation and the validation-catalog approval-chain owner decision.

These recommendations do not authorize implementation. **Do not automatically start E-2.**

## 11. Preservation and stop

This decision adds this document and updates only the current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Production, tests, historical evidence and previously accepted owner decisions remain unchanged. The external acceptance receipt is under `Solo/gagamba-alt-b-slice-e-01-owner-acceptance-01`; the commit identity is recorded there after commit to avoid self-reference.

No implementation, remediation, F-01/F-02/F-03 change, new validation authority, E-2 through E-7 work, GAVA1, launcher/driver, atomic enablement, Slice N execution, production activation, branch, extra worktree, reset/clean/rebase/prune or push. Preserve all 44 historical untracked paths. **Stop after committing this owner decision; do not begin remediation or the next Slice E package automatically.**
