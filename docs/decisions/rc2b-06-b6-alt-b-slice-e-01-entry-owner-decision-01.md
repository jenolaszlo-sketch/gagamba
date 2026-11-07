# Slice E entry owner decision — bounded E-1 authorization

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-ENTRY-OWNER-DECISION-01`

Date: 2026-11-07 (Asia/Manila). Recorder and architecture acceptance reviewer: Codex. Scope: owner-decision documentation only, on canonical `main`.

## 1. Formal owner disposition

**SLICE E ENTRY ACCEPTED WITH QUALIFICATIONS — E-1 IMPLEMENTATION AUTHORIZED**

The completed [entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md), whose disposition is **ENTRY SUPPORTED WITH QUALIFICATIONS**, is accepted subject to the binding limits below. This decision authorizes only the first bounded **E-1: non-forgeable prepublication integrity projection** implementation package under a separate commission. It does not accept an E-1 implementation or authorize its automatic execution under this decision-recording commission.

Positive validation remains non-constructible and unauthorized. E-2 through E-7 are approved only as an ordered planning baseline; their implementation and individual design details are not approved by this record. Slice E completion, protected positive execution, Slice N, publication and production activation are not accepted.

The disposition follows the evidence rather than treating the entry recommendation as approval: the accepted design supplies an existing protected-root integrity basis that can support isolated preparatory work, while the current code lacks the complete prepublication constructor. That supports bounded E-1 work, not a claim that a trusted projection already exists. If its complete construction boundary cannot be implemented by the accepted reuse described in §4, E-1 must remain inert or fail closed and report the exact missing requirement. A new trust source requires a separate decision.

## 2. Verified starting baseline and preservation

| Item | Verified value |
|---|---|
| Canonical checkout | `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `main` |
| Starting commit | `2c6b3c9ab0022c4c44065422c45c2449570d3ccb` |
| Starting tree | `e3a7b936512342fb25eb41bae4f7d3e82645096f` |
| Worktrees | Exactly one canonical worktree |
| Tracked/index state | Clean; both unstaged and staged diffs empty |
| Historical untracked paths | Exactly 44; each path and SHA-256 captured before edits |
| Preservation inventory digest | `56e8acd2c91f04dd608c39645aeb5506d076f8576aaf4489e13ddb6223158b2c` |
| Source continuity | No non-`docs/` diff from P4 reconciliation source `762cdf2796f744fb99b75f30e83813a48d048a97` to this baseline |

The expected baseline matches. The initial sandboxed Git status attempt was inconclusive because repository access was denied; the subsequent successful canonical-checkout check established cleanliness. No repair, reset, clean, rebase or additional worktree was used.

The inventory digest is SHA-256 of UTF-8 JSON mapping sorted `git ls-files --others --exclude-standard -z` paths to content SHA-256 values, with sorted keys and compact separators. It is a content inventory, not the historical porcelain-status-list digest. Baseline and post-commit receipts are retained outside the repository in `Solo/gagamba-alt-b-slice-e-entry-owner-decision-01/`. The final commit/tree are recorded in the post-commit receipt and handoff to avoid self-reference.

## 3. Evidence, authority and architecture assessment

### 3.1 Governing records inspected

| Evidence | Authority and interpretation |
|---|---|
| [Slice E entry review](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md), especially §§2–4, 6, 8–10 | Architectural recommendation and qualifications; no implementation authorization by itself |
| [Alternative B owner decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md), §§2, 3.2–3.5 | Accepted distinct prepublication integrity selection, full checks, phase separation, corrected residual and validation-only ordering |
| [Alternative B implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md), §§3.5–3.6, 4–5 | Atomic positive enablement; preparatory isolation; separate native gate |
| [Mechanism B acceptance](rc2b-06-b6-f02-mechanism-b-owner-acceptance.md), §§2–6 | Accepted-by-hash design and Q01–Q10; integrity, S2 qualification, final admission, validation and activation remain distinct |
| [F02 trust-store decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md) | Approval for `accepted-qualifications` only; no automatic validation-catalog approval authority |
| [Installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md), Gates A–D | Closure inventory, integrity versus authorization, fixed protected selection, verification-to-execution and actor identity requirements; historical design statements are not current implementation findings |
| [B6 qualification plan](../plans/rc2b-06-b6-qualification.md), T01–T25 including T20a–c/T25-A/B, V01–V13 and evidence classes | Deterministic/native separation, exact closure and refusal obligations, production/test separation; no test specification is a measured pass |
| [P4 reconciliation](../evidence/rc2b-06-b6-alt-b-p4-coverage-recon-01.md) and [owner acceptance](rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | Accepted 29-row refusal disposition: 21 COVERED, 5 AUTHORIZED CONTINUATION, 3 LEGACY / TEST-ONLY; qualifications preserved, not broadened |
| [Remediation evidence standard](../remediation-evidence-standard.md) | Durable evidence obligations for the subsequent security-sensitive implementation, not a claim that this documentation decision is an implemented remediation |

The following available external inputs were read and their SHA-256 values independently matched to Alternative B decision §4:

| External input under `Solo/` | Verified SHA-256 |
|---|---|
| `gagamba-integrated-start-authority-owner-decision-01/validation-bootstrap-compatibility.md` | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` |
| `gagamba-integrated-start-authority-owner-decision-q1/validation-bootstrap-assessment.md` | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` |
| `gagamba-integrated-start-authority-owner-decision-01/implementation-and-test-handoff.md` | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` |

Their historical PENDING captions remain untouched. Later canonical decisions establish the accepted semantics; neither these hashes nor this record reconstruct the full frozen schemas or supply the missing validation-catalog approval decision. This record verified these three inputs, not every historical external package or native result. The external handoff's T01–T23 numbering is distinct from B6-T01–T25.

### 3.2 Source findings at the baseline

Source paths below are under `src/Gagamba.Execution.Linux.M1/`.

| Source inspected | Existing behavior and implication for E-1 |
|---|---|
| `Installed/InstalledFileSystem.cs`: `VerifyActive`, `VerifyPayload`, `VerifyStaged` | Fixed-root selection, protected descriptor-relative reads, exact payload/dependency checks and selector stability exist. The fresh/null-qualification branch returns `InstalledResult.Fresh` **before** `CheckHost`. Candidate/staged checks do not supply trusted active identity. None is a complete prepublication projection. |
| `Installed/InstalledPayload.cs`: `CheckHost`, `CheckPlatform` | Host/platform/runtime/loader/helper checks exist and must not be omitted when reusing integrity verification. Their existence is source evidence, not a newly qualified protected positive. |
| `Installed/InstalledMetadata.cs`; `Installed/InstallationIdentity.cs` | Receipt parsing enforces the existing null association. `InstalledResult` projects an identity when a manifest exists, and `InstallationIdentity.Project` sets the wire `Qualified` field true; `Validate` requires true. Thus a wire field or even `Fresh.Identity` is not proof of qualification or prepublication trust. Preserve current qualified gates; do not wrap these values as authority. |
| `Installed/LoadedInstallation.cs`: `Verify`, `VerifyLoaded`, `Revalidate` | Private construction follows `VerifyActive` requiring actual Qualified state/association; loaded-role checks end in qualified revalidation. There is no ready-made distinct prepublication constructor. Extracting reusable checks must preserve normal qualified behavior and must not fabricate a `LoadedInstallation`. |
| `Lifecycle/OwnerAuthorityContext.cs`; `OwnerManifestAdmissionGate.cs` | Validation has a private constructor and no factory. Qualified retention and protected final-admission checks are separate. The projection must not satisfy either authority case. |
| `Bootstrap/QualifiedOwnerPreflight.cs`, `RecoveryProtocol.cs`, `IntegratedRecoveryHost.cs` | Owner preflight obtains actual qualified loaded selection; bootstrap accepts v1/v2, not v3-pre; integrated recovery starts with its admission gate. No E-1 wiring is authorized here. |
| `Lifecycle/OwnerPreparationTransaction.cs`, `M1RecoveryScope.cs` | Early owner admission precedes journal creation. Current recovery waits for WriterReady at line 418 before DirectBootstrap at 435 and DirectRendezvous at 439; owner sends WriterReady at 145 before direct accept. This is the known validation-only ordering obligation, not an E-1 change. |
| `Restricted/B3ProviderAdmission.cs` | Retained provider selection, fresh provider-role verification, original tuple and final-admission grant are required; a profile flag or synthetic selection is insufficient. |

Searches in `src/` and `eng/` found no GAVA1, `InstalledIntegrity`, `ValidatedPreSession`, validation-catalog store or validation construction matching the reviewed symbols. Together with source inspection, this supports the current non-constructibility finding; it does not prove a future implementation safe.

## 4. Exact E-1 authorization and construction boundary

E-1 may add only internal integrity-projection types, narrowly necessary verification support, explicit invariants, deterministic tests and documentation. Its contract is a **distinct immutable prepublication integrity result**, bound to the actual protected active selection and full fixed-root manifest/payload, platform/host and loaded-role checks, carrying a null qualification ID and null qualification-record hash. This represents verified integrity only; it does not approve a case, authorize a process or grant execution.

The accepted trust basis is the existing trusted host root/kernel/filesystem and fixed installation selection, not caller data. E-1 may extract/reuse those accepted checks inside an isolated verifier. If a successful production-code projection constructor is implemented, it must have one narrowly controlled internal construction route after all required checks; no production entry or consumer may call it in E-1. A private constructor or `internal` visibility alone is not a provenance proof.

No caller-provided `Qualified` bit, digest, identifier, manifest, `InstalledResult`, claimed verification outcome, selectable trust root or injected verifier may manufacture trusted projection state. Caller values may at most constrain comparisons against independently obtained evidence; they cannot replace that evidence. Test data must remain distinguishable from production provenance, with any authority-capable test seam compile-time excluded from RELEASE.

The current complete constructor is missing. The accepted design authorizes reuse of verification primitives; it does not authorize inventing a new issuer, administrative approval or attestation mechanism to bridge that gap. If full checks or their binding cannot be established within this contract, implement only an inert representation or an always-refusing boundary, disclose what remains unsupported, and stop before trusted success. Such partial work is not completion of the full projection contract and needs independent disposition and a subsequent owner decision before expansion.

“Refusal-only” means no new execution authority or positive validation path; it does not prohibit deterministic positive tests of valid internal data/projection behavior. “Production-unreachable” means no wiring from current runtime entry paths or protected sinks, including an internal consumer. Read-only verification logic may reuse accepted protected-read primitives in source, but this commission authorizes no protected resource acquisition or execution. Native provisioning and exercising protected positives remain outside E-1.

A phase, receipt or active-selection change must refuse rather than promote or adopt the observed state. Later authenticated session use, comparison to approved case policy, and fresh pre-write revalidation remain later-package obligations; E-1 neither creates those bindings nor claims to satisfy them.

## 5. Binding implementation invariants

1. **Non-forgeability:** ordinary caller-controlled fields cannot create a trusted integrity projection; production has no injectable trust source.
2. **Prepublication separation:** the projection is distinct from installed Qualified identity, never `InstalledUnqualified` alone or a wrapped `LoadedInstallation`; both qualification associations remain null.
3. **No validation authority:** it cannot construct, substitute for or convert to `OwnerAuthorityContext.Validation`, Qualified context, final admission or activation authority.
4. **Refusal-only:** missing, untrusted, malformed, contradictory, mismatched, substituted or unsupported evidence fails closed. No optimistic success on an unavailable check.
5. **No protected effect:** projection construction authorizes no launch, publication, RELEASE, writer/domain/staging or protected resource acquisition.
6. **No production reachability:** current production entry paths cannot activate positive validation or consume the projection; no new sink caller.
7. **No downgrade:** rejected validation/integrity evidence never falls back to legacy or qualified execution authority.
8. **No schema invention:** undocumented K/M/F/S, GAVA1 or v3-pre fields are not assumed authoritative or implemented here.
9. **RELEASE preservation:** Debug-only construction, injection and authority facilities remain absent from RELEASE; existing production/test separation remains effective.
10. **P4 preservation:** accepted refusals, caller domination, stop/reap/cleanup, original deadlines, sole owner RELEASE writer and at-most-one attempt remain unchanged.

## 6. Explicit exclusions

This decision does not authorize:

- E-2 through E-7 implementation, validation-catalog approval-chain implementation, or K/M/F/S protected-root work. E-1 has no accepted dependency on those new schemas/stores.
- v3-pre codec, GAVA1 implementation or wire changes, root authorizer, fixed launcher, driver, custody transfer, privilege drop or DROP-COMMIT.
- R/O/C ordering changes or any factory for `OwnerAuthorityContext.Validation`.
- Changes to `InstallationIdentity.Validate` that enable prepublication values to masquerade as qualified installations; synthetic Qualified results or promotion through existing result wrappers.
- Protected native qualification, Slice N, provisioning, publication, production activation or public API expansion.

Necessary work outside this boundary requires a separate decision; it cannot be absorbed as an incidental E-1 dependency.

## 7. Binding architectural qualifications and unresolved decisions

**Q-E1 — Frozen-schema provenance.** Authoritative K/M/F/S schemas, GAVA1 header, v3 validation-pre layouts and release correlation remain supported by external accepted packages and historical hashes, not a complete in-repository frozen-schema record. E-1 must not guess missing fields. Commission a separately reviewable provenance reconciliation that binds exact bytes, originating acceptances and limitations and commits the authoritative record before schema-dependent packages. The entry review's prerequisites for E-2–E-5 remain in force.

**Q-E2 — Validation-catalog approval.** `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` has no accepted owner decision defining its approval chain. F02 covers `/etc/gagamba/accepted-qualifications/<qualificationId>.json`; its authority does not automatically extend to validation catalogs. A later decision must establish who approves, the detached review/acceptance chain and exact accepted representation. No new approval schema is selected here.

**Q-E3 — Atomic enablement.** Positive validation enablement is atomic under the Alternative B **implementation plan §3.5**. E-1–E-5 are preparatory and cannot independently enable it. The Alternative B owner decision §3.5 concerns stopping/compatibility; it is not a substitute for the plan's dependency set. All issuer, catalog, custody, protocol, policy, original identity, sink, generation, stopping, driver-dispatch and closure dependencies remain mandatory together.

**Q-E4 — Existing ordering inversion.** Current WriterReady-before-direct-bootstrap/rendezvous is contrary to the accepted validation-only R/O/C sequence. E-6 must implement and qualify the atomic recovery/owner/controller change: direct authenticated context before owner writer effects, bounded mixed-version refusal and retained stop service. E-1 may not change ordering or label an early context ACK as WriterReady.

**Q-E5 — P4 re-audit.** E-4 and E-6 require fresh authority/sink audits before their first protected effects. E-1–E-3 are only provisionally P4-neutral; implementation and independent qualification must establish that for each exact revision. Preserve F-R1/O-R1 (S10/S11 rely on upstream F01 domination and the audited caller graph), O-R2 (S28 remains test-only; `internal` is not authority), and O-R3 (constructor-recheck refusal-reason typing remains open). Any new production caller invalidates the corresponding domination claim and requires re-audit; E-1 authorizes none.

Preserve the accepted observation-to-effect residual: a root change after the last relevant stable observation may escape detection, even before verifier return. No atomic root-mutation/effect lock or retroactive cancellation is promised. Applicable P4 rows remain `NATIVE-PENDING`; all **331 unqualified native skips** remain not-passes. The five AUTHORIZED CONTINUATION and three LEGACY / TEST-ONLY rows are not reclassified as unconditional COVERED. Protected positive execution, custody, native RELEASE, stopping/reaping and partial cleanup remain Slice N obligations. Existing P1–P4 and Mechanism B qualifications, including the threat-model limits and reflection-fabrication note, are retained.

## 8. Required E-1 implementation and independent qualification evidence

### 8.1 Implementation handoff

Require a durable `docs/evidence/` record following the remediation evidence standard, containing:

- Exact source/test diff, baseline and final commit/tree, construction sites, mutable inputs/retained state, host/build configuration, and reproducible commands with complete results and skips.
- Construction-boundary and authority-provenance analysis mapping every trusted field to the accepted verification that establishes it. Explicitly distinguish pure data, verified integrity and execution authority; document any fail-closed/inert limitation.
- Call-site and conversion audit showing no positive validation path, new production consumer, changed P4 sink or validation factory. Preserve normal qualified verification when extracting shared logic.
- Deterministic positive controls for valid internal behavior and negatives for missing, malformed, contradictory, substituted and mismatched inputs, qualification/phase confusion, mutated retained data and caller-forged state. Include a gate-removal/bypass negative control demonstrating that the meaningful assertion detects missing enforcement.
- RELEASE builds, production-verifier/refusal checks, and evidence that Debug-only facilities are absent. Run focused existing integrity/identity/authority/P4 regression checks appropriate to actual edits; report exact commands after choosing runnable filters. No speculative passing counts.
- Explicit limitations of mocked/synthetic inputs and unavailable native checks. Synthetic success must never count as protected-root or real loaded-role qualification. No protected native positive is required as proof of this bounded E-1 package.

### 8.2 Separate independent qualification

A separately commissioned reviewer must verify, against the exact committed implementation:

1. The types and construction/validation rules match this E-1 contract, including any declared inert limitation.
2. Ordinary caller-controlled fields and result objects cannot forge trusted projection state; visibility alone is insufficient.
3. Missing, malformed, inconsistent, unsupported or substituted evidence fails closed without a trusted partial result.
4. Projection state cannot be promoted to qualified installation identity, `LoadedInstallation` or general admission.
5. No production path constructs validation authority or reaches a positive validation path.
6. No new protected effect or production sink caller invalidates accepted P4 domination; the P4-neutrality claim is verified.
7. No Debug-only authority capability, injectable verifier or test construction seam leaks into RELEASE.
8. No unresolved K/M/F/S, GAVA1, v3-pre or approval-chain schema is assumed or implemented.
9. Existing production behavior, normal-qualified verification and P4 refusal/stop guarantees remain intact.

Qualification must add independently constructed negative cases rather than only reproduce producer tests. It must identify the construction sites and failed promotions it actually exercised and clearly separate source inspection, deterministic execution and native-pending claims. A subsequent **separate owner acceptance** is required after implementation and independent qualification; neither implementation completion nor a passing test suite automatically accepts E-1 or opens E-2.

## 9. Planning sequence and later authorization gates

| Package | Purpose | Status after this decision / next gate |
|---|---|---|
| E-1 | Non-forgeable integrity projection | Implementation authorized only within §§4–8, under its own commission; no production consumer |
| E-2 | K/M/F/S and approval decision | Planning only; frozen provenance reconciliation and explicit validation-catalog approval-chain owner decision first |
| E-3 | v3-pre codec | Planning only; authoritative frozen layouts and inherited prerequisites first |
| E-4 | GAVA1 | Planning only; frozen header, inherited prerequisites and fresh P4 re-audit required |
| E-5 | Launcher/custody interface | Planning only; accepted launcher/driver interface and provenance/approval prerequisites; no native qualification implied |
| E-6 | Atomic enablement | Planning only; complete plan §3.5 dependencies, ordering correction and fresh P4 audit; separate owner decision |
| E-7 | Driver | Planning only; exact dispatch/closure and separate authorization; does not waive atomic dependencies |
| Slice N | Protected native qualification | Separate authorized milestone and host/candidate/evidence binding |

This is development/review order, not permission to enable E-6 before required E-7 driver dispatch and closure exist. E-6 remains unreachable if any atomic dependency is unfinished; actual positive exposure also cannot be accepted on deterministic evidence alone. Later owner decisions must reassess dependencies and design details using then-current evidence, including the Slice N boundary. Approval of the sequence does not preapprove every proposed detail.

## 10. Recommended next commission and stop

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-IMPL-01`, limited to E-1 as recorded here. It should first make the construction-boundary explanation concrete, implement only what can be established using the accepted evidence and verification boundaries, record limitations instead of inventing authority, and hand off the exact committed package for independent qualification and later owner acceptance.

This commission changed documentation only: this new decision and additive current-state blocks in `docs/PROJECT-STATUS.md`, `docs/handoff.md` and `docs/work-queue.md`. Historical records remain intact. No production/test code, build, test-suite rerun, native probe, protected acquisition, provisioning, launcher/driver, validation factory, ordering change, branch, extra worktree or push was performed. **E-1 implementation was not started. Stop after committing this owner decision.**
