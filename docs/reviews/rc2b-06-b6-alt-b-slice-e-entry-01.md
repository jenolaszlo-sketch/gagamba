# ALT-B Slice E entry review — atomic validation bootstrap

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-ENTRY-01`
Date: 2026-11-07 (Asia/Manila). Reviewer: DeepSeek. Role: independent architecture and milestone-entry review; documentation and source inspection only.

**Formal entry disposition: ENTRY SUPPORTED WITH QUALIFICATIONS.**

Slice E is ready for a bounded, refusal-only first implementation package, but it is **not** ready for any package that enables positive validation authority. The accepted design requires positive validation to arrive atomically (plan §3.5); its detailed schema is accepted by hash through the F02/Alternative B decisions but is frozen in external packages, and one administrative approval-chain decision is not yet recorded in-repo. This review defines the smallest defensible first package and records the reconciliation prerequisites for the later packages. It authorizes nothing.

---

## 1. Verified baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `2d39380d0c0b61753097c197ce7c1df03cdddcb7` (matches the commission) |
| Starting tree | `abc7c1d9b3459afca35a586a8dfc6d3ae30f1e62` (matches the commission) |
| Worktrees | exactly one canonical checkout (`git worktree list`) |
| Tracked / index state | clean (no staged or modified paths) |
| Historical untracked paths | 44 (`git status --porcelain -uall`) |
| Push | not performed |

Baseline matches the commission exactly. No reset, clean, rebase or repair was needed or performed. This review changed no production or test source and created no authority.

---

## 2. Source-of-truth inventory

### 2.1 In-repository authoritative records

| Record | Path | What it binds |
|---|---|---|
| Alternative B design acceptance | `docs/decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md` | Owner-side manifest authority; owner Q10; prevalidation scan + corrected residual (§2); validation-only early direct authentication (§3.4); prevalidation contract (§3.3); non-claims (§5) |
| Bounded implementation handoff | `docs/plans/rc2b-06-b6-integrated-start-authority-impl-01.md` | Slice ordering P1–P4/E/N; **§3.5** Slice E atomic dependency set and the five-step validation-only R/O/C ordering; §3.6 Slice N; §4 what may precede E |
| Mechanism B acceptance | `docs/decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md` | Final-admission separation (Q02, §4); the Q01–Q10 qualifications; the S2/final/activation separation that Slice E must not collapse |
| F02 trust-store decision | `docs/decisions/rc2b-06-b6-s2-f02-trust-store-owner-decision.md` | Administrative approval store `/etc/gagamba/accepted-qualifications/<id>.json`; runtime never reads it; withdrawal semantics |
| Installed-artifact design | `docs/design/rc2b-06-b6-installed-artifact-trust.md` | Gates A–D; manifest/receipt/qualification-record binding (§4.5); descriptor semantics (§4.6); verification-to-execution (§5); installation-identity handshake (§6.1–6.2) |
| B6 qualification plan | `docs/plans/rc2b-06-b6-qualification.md` | B6-T01–T25 (including T24 production/test separation and T25-B installed closure); V/I/N classes |
| P4 coverage acceptance | `docs/decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md` | Binding prerequisites: F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, 331 native skips; Slice E exclusion |
| P4-03 Q1 handoff | `docs/reviews/rc2b-06-b6-alt-b-p4-03-q1-handoff.md` | The reflection-fabrication boundary note carried as Q1-F03 |
| P4 29-row inventory | `docs/evidence/rc2b-06-b6-alt-b-p4-01-sink-inventory.md` | Sink rows S01–S29 and the T01–T23 mapping |

### 2.2 External accepted-by-hash design inputs (frozen)

| Record | Package | Status |
|---|---|---|
| Validation bootstrap compatibility | `Solo/gagamba-integrated-start-authority-owner-decision-01/validation-bootstrap-compatibility.md` | The exact validation contract table, the K/M/F/S + A/L/C/R/O/D notation, GAVA1, v3 validation-pre, protected roots, the release-correlation formula, and the two additive clarifications. Caption reads "PROPOSED — HUMAN OWNER APPROVAL PENDING" for the clarifications; those were subsequently accepted by the Alternative B decision (§3.3–§3.4) |
| Validation bootstrap assessment | `Solo/gagamba-integrated-start-authority-owner-decision-q1/validation-bootstrap-assessment.md` | Independent assessment; states the frozen Mechanism B documents "are accepted through the later canonical F02 owner decision despite historical PENDING captions" |
| Implementation and test handoff (T01–T23) | `Solo/gagamba-integrated-start-authority-owner-decision-01/implementation-and-test-handoff.md` | The P/E/N slice split; T01–T23 case matrix; positive controls G/H/V/L |
| Frozen Mechanism B documents | referenced via F02 acceptance `§2` (`mechanism-b-session-capability.md`, `actor-and-descriptor-binding.md`, `case-policy-and-action-contract.md`) | Accepted by hash through F02; **not present in the repository tree** |

The last row is the material provenance gap: the exact bytes of the frozen validation contract (K/M/F/S schemas, v3-pre layouts, GAVA1 header, approval-chain roots) are authoritative only through external packages and prior-decision hashes, not through an in-repo design record.

### 2.3 Naming reconciliation (no invented semantics)

Notation is used exactly as the accepted design defines it and is **not** expanded here:

- **A** independent root administrator/authorizer; **L** fixed reviewed launcher and continuing non-root lifetime guard; **C** original controller; **R** original recovery; **O** original owner; **D** non-root test conductor (`validation-bootstrap-compatibility.md` §"Exact validation contract").
- **K** protected catalog, **M** fixture manifest, **F** fixture policy, **S** finite action script; **H** exact SHA-256 over strict canonical bytes; **P** the protected eligibility approval record.
- **GAVA1** the accepted dedicated authenticated direct-context exchange (AF_UNIX `SOCK_SEQPACKET`, 64-byte header binding version/type/role/length/sequence/session/execution/lease with exact SCM credentials).
- **R/O/C** recovery/owner/controller ordering.
- **v3 validation-pre** the prepublication bootstrap parser with `status0` and separate `InstalledIntegrity`/`ValidatedPreSession` types.

### 2.4 Existing code surface (verified)

| Mechanism | Path / symbol | Role |
|---|---|---|
| Fixed-root active verification | `Installed/InstalledFileSystem.cs` `VerifyActive` | Reads `/opt/gagamba/active`, digest-named generation, `manifest.json`, `install-receipt.json`, receipt-selected `qualifications/<id>.json`; descriptor-relative no-follow; stability/pinning rechecks; Qualified only with receipt-bound record + `InstalledPayload.CheckHost` |
| Candidate/staged verification | `InstalledFileSystem.VerifyPayload` / `VerifyStaged` | No active/receipt/record; returns no trusted identity |
| Protected directory reader | `InstalledFileSystem.OpenProtectedDirectory` | Root-owned `0755` leaf; root-owned, non-group/other-writable ancestors |
| Platform/host checks | `Installed/InstalledPayload.cs` `CheckHost`/`CheckPlatform` | WSL2/kernel/glibc/runtime/hostfxr/loader/bwrap digests; loader-preload; cgroup2 |
| Selected installation | `Installed/LoadedInstallation.cs` `Verify`, `Revalidate`, `VerifyFinalAdmission`, `VerifyLoaded` | Verifies loaded role closure, runtime, `/proc/self/maps`; no injectable trust source |
| Final admission | `Installed/FinalAdmissionVerifier.cs` | Three protected stores; CJSON-1; exact binding; fresh S2; private `Grant`; `ForTest` Debug-only |
| Admission separation | `Installed/AdmissionAuthority.cs` | `IsFinalAdmissionEligible`; `ProductionActivationAuthorization` has **no factory** |
| Generation gate | `Installed/ReleaseGenerationGate.cs` | `RequireStillQualified`, `MapFailure`, `RequireBoundGenerationActive` |
| Identity record | `Installed/InstallationIdentity.cs` | `Validate` currently requires `Qualified == true`; `Generation == ArtifactSetSha256` |
| Result states | `Installed/InstalledMetadata.cs` `InstalledState`/`InstalledResult` | `Unavailable`/`Unqualified`/`InstalledUnqualified`/`Qualified`; `Fresh` for no qualification |
| Owner contexts | `Lifecycle/OwnerAuthorityContext.cs` | `Qualified` (`FromVerified`; `ForTest` Debug-only), `Legacy`, and `Validation` with a **private constructor and no factory** |
| Owner manifest gate | `Lifecycle/OwnerManifestAdmissionGate.cs` | Early + late final-admission refusals |
| Owner release gate | `Lifecycle/OwnerReleaseGenerationGate.cs` | Pre-RELEASE generation verification (P3); `VerifyObserved` Debug-only |
| Owner transaction | `Lifecycle/OwnerPreparationTransaction.cs` | Early gate `:92` before `new JournalFileStorage()` `:104`; writer `:122`; intent `:139`; `WriterReady` `:145`; direct accept `:150–151`; `StartAuthority` `:165` |
| Recovery dispatch | `Bootstrap/RecoveryHost.cs` | v1 legacy parent `:59`; v2 integrated parent after echo `:66–68` |
| Integrated recovery | `Bootstrap/IntegratedRecoveryHost.cs` | `RequireIntegrated` first statement `:23`; writer `:77`; `StartOwnerPreparation` `:89` |
| Recovery echo / F01 | `Bootstrap/QualifiedRecoveryEcho.cs` | recovery S2 `:23`; `Require` `:31–32`; launch plan `:33` |
| Owner preflight | `Bootstrap/QualifiedOwnerPreflight.cs` | Post-Start owner S2 before writer; returns `Qualified.FromVerified` |
| Direct channel | `Bootstrap/OwnerDirectConnection.cs`, `Bootstrap/DirectSession.cs`, `Bootstrap/DirectBootstrapPacket.cs` | Existing owner echo/GID1 (`Magic = 0x31444947`) |
| B3 factory/constructor | `Restricted/B3Qualification.cs`, `Restricted/B3ProviderAdmission.cs` | S01/S02 provider gates |
| Journal/domain/staging | `Lifecycle/M1RecoveryScope.cs` | `StartOwnerPreparation` `:302`; `TrySpawnOwner` `:220`; current `WriterReady` wait `:418` then `DirectBootstrap` `:435` and `DirectRendezvous` `:439`; `StartAuthority` `:455` |

Repository-wide searches confirm **no** implementation of GAVA1, `InstalledIntegrity`, `ValidatedPreSession`, `AuthorityMode/AuthorityHash`, `DROP-COMMIT`, the validation catalog/mixture/policy/action roots, or a root authorizer/launcher/driver.

---

## 3. Existing-versus-missing mechanism matrix

Class legend: **(1)** implemented; **(2)** accepted design not implemented; **(3)** proposed—needs an owner decision; **(4)** missing or contradictory; **(5)** deferred to Slice N or later.

| # | Slice E prerequisite (plan §3.5) | Class | Evidence / exact state |
|---|---|---|---|
| E-a | Independent live root authorizer **A** | 2 | Required by plan §3.5; zero source implementation. A is a human/administrative trust root (`validation-bootstrap-assessment.md`) |
| E-b | Fixed reviewed launcher **L** + permanent privilege drop + original custody/pidfds/SCM | 2 | Required by plan §3.5 and §3.4 step 3; no launcher/driver exists. `docs/plans/rc2b-06-b6-native-installed-driver-handoff.md` is driver **D**, implementation-required |
| E-c | Protected catalog/approval chain (**P**, K/M/F/S) | 2 / 3 | Roots are specified only externally (`/opt/gagamba/validation-catalogs`, `validation-fixture-manifests`, `validation-policies`, `validation-actions`, `validation-fixtures`; `/etc/gagamba/accepted-validation-catalogs/<Hk>.json`). F02 defined only `accepted-qualifications`; the validation-catalog approval record needs an explicit decision |
| E-d | Existing S2 fixed-root verification + protected readers | 1 | `InstalledFileSystem.VerifyActive`/`OpenProtectedDirectory`; `LoadedInstallation.Verify` |
| E-e | Final-admission separation and private grant | 1 | `FinalAdmissionVerifier`/`AdmissionAuthority` |
| E-f | Non-forgeable **prepublication integrity projection** (null qualification, full host/loaded-role checks) | 2 / 4 | Required by owner decision §3.3 and compatibility §"release correlation" line 52; today `VerifyActive` returns `InstalledUnqualified` (`Fresh`) without a qualification record, and `InstallationIdentity.Validate` requires `Qualified == true`. No distinct prepublication type exists |
| E-g | v3 validation-pre bootstrap parser (`status0`, `InstalledIntegrity`/`ValidatedPreSession`, `AuthorityMode`/`AuthorityHash`) | 2 / 4 | `RecoveryProtocol.RecoveryConfiguration.Validate` admits only `Version` 1 or 2 (`:156`); no v3 identity type |
| E-h | GAVA1 authenticated direct-context exchange | 2 | Zero source implementation (case-insensitive search across `src/` and `eng/`). GID1 exists; GAVA1 does not |
| E-i | Exact case policy + finite fixture + one fixed template | 2 | Defined in the frozen `case-policy-and-action-contract.md` (accepted via F02); not in-tree |
| E-j | Original session/execution/lease/preparation/correlation ID preservation | 2 | Partially present (`DirectAssociation` carries execution/store-generation/session/controller/recovery); full original-ID propagation across integrated preparation is not implemented |
| E-k | All pre-effect sink checks | 1 | P1–P4 accepted (21 COVERED / 5 AUTHORIZED CONTINUATION / 3 LEGACY-TEST-ONLY) |
| E-l | Owner policy + release-correlation recomputation | 2 | The release-correlation formula is external; owner currently consumes `ApprovalCorrelationSha256` (`Execution/ReleaseArbiter.cs`) without the validation correlation |
| E-m | Owner-side phase-specific final pre-write generation gate | 1 / 2 | `OwnerReleaseGenerationGate` exists for normal/post (Qualified); the prepublication branch ("separate original InstalledIntegrity selection, null qualification") is unimplemented |
| E-n | R/O/C validation-only ordering amendment | 2 | Accepted by owner decision §3.4. Current source is the inverse: `M1RecoveryScope:418` waits `WriterReady` before `:435` `DirectBootstrap`; `OwnerPreparationTransaction:145` sends `WriterReady` after writer/intent `:122/:139` |
| E-o | Stop service at every barrier + original deadline handling | 1 | Existing C3/stop/reap/reconcile; must be preserved, not extended |
| E-p | Driver **D** + exact driver dispatch + production closure checks | 2 / 5 | `rc2b-06-b6-native-installed-driver-handoff.md`; native (Slice N) for positives |
| E-q | Validation context factory (`OwnerAuthorityContext.Validation`) | 2 | Private constructor, no factory (`OwnerAuthorityContext.cs:86–91`); the source comment requires the factory to land "together with the complete issuer/context/policy checks" |
| E-r | Production/test separation for the new candidate | 1 / 2 | T24 basis exists (`GAGAMBA_PRODUCTION`, production verifier); the new validation surfaces must be re-checked in the resulting candidate |
| E-s | Native protected pre/post/normal qualification | 5 | Slice N, explicitly outside this review |

---

## 4. Slice E authority and lifecycle architecture

### 4.1 Independent root authorizer (A)

- **Trusted authority source.** The human/trusted root administrator **A** plus the protected filesystem is the root of trust; the reviewed topology is a premise (`validation-bootstrap-assessment.md`; owner decision §5 "Trusted-root bounds").
- **Authorization/approval boundaries.** A reviews a **live** challenge and authorizes **L**; L verifies the exact protected catalog eligibility **P** and the original **C** before issuing; issuance is a B1 marker with permanent privilege drop and an authenticated DROP-COMMIT (`validation-bootstrap-compatibility.md` "Independent issuer"). A may exit after the accepted response; non-root L remains the lifetime guard.
- **Identity/binding.** Capability/K bind artifact-set SHA, source commit/tree, profile/version/RID/scope, host/boot/platform and phase. Post uses the actual S2 qualification ID/raw hash; pre uses both null plus a separate full integrity/host projection.
- **Replay/substitution/downgrade refusal.** Fresh `getrandom` nonce, boot and monotonic admit-before/session-end; one-use B1/B2/action latches; exact SCM credentials; duplicate/expired/reconnected/replacement-child/PID-reuse refuse.
- **Independence from untrusted input.** No producer-created approval, standing approval file, arbitrary launcher command or target-supplied path is authority.
- **Who may construct validation authority.** Only L's issuance (after A/P) produces the one-use capability; **O** consumes it after its own independent checks; **R/D** never hold it.
- **Circular-trust check.** The graph `M -> F -> S -> K -> review -> owner receipt -> P -> live response -> capability` is acyclic (compatibility §"Eligibility and provenance"). No code-level circularity: A/P are human/administrative roots; L verifies P; L does not author P. **No circular trust dependency found.**

### 4.2 Fixed launcher and custody (L, B, D)

- **Immutability/identity.** L is a fixed reviewed root-protected launcher; its exact bytes/identity must be pinned like the install closure (Gate C). This is **accepted design, unimplemented**.
- **Authorized process creation.** Trusted L spawn retains the original C pidfd; root-authenticated OFFER on the inherited L-C channel plus DROP-COMMIT from the permanently-dropped L establishes issuance (`validation-bootstrap-compatibility.md` "Original actor/process").
- **Custody/privilège drop.** Sealed root-owned `0444` read-only memfd `<=65536` bytes with all four seals, exact hash via `pread`; C gets exactly two OFFER rights; R/O receive only child-local `fd100/101/102` (memfd, L pidfd, sideband) via per-spawn allowlists; `MSG_CMSG_CLOEXEC`; close on malformed/truncated/extra ancillary; no process-wide inheritance window.
- **Failure/partial cleanup.** L loss/expiry ends positive progress; R keeps sole reaping and original intent reconciliation; cleanup `Unknown` is never recast as success.
- **Slice E vs Slice N separation.** The **interface, launcher and custody contract** can be source-implemented and deterministic-tested under Slice E; **actual root privilege drop, real SCM/pidfd custody, and loaded-closure positives require Slice N**. Merely having a managed implementation does **not** qualify custody.

### 4.3 Protected catalog and approval chain (K/M/F/S, P, case policy, GAVA1)

- **Protected catalog and approval.** P is a root-owned eligibility approval; K is the protected catalog. P grants **eligibility, not a live invocation**. Runtime roots (external spec): `/opt/gagamba/validation-catalogs/<Hk>.json`, `validation-fixture-manifests/<Hm>.json`, `validation-policies/<Hf>.json`, `validation-actions/<Hs>.json`, fixtures under `validation-fixtures/<Hm>/`, all `root:root 0755` directories with single-link `root:root 0444` metadata; opened descriptor-relatively, no-follow, pin/hash/parse/reencode/reacquire, private immutable bytes.
- **Protected K/M/F/S artifacts.** As above. **Hashes alone do not establish human provenance**; provenance is administrative.
- **Case policy.** Exactly one approved K/M/F/S and finite B2 no-release / B3 no-release / B3 release template; B2 creates no writer/domain/staging; B3 release requires exact `allowRelease` in K/F and one fixed index-2 action.
- **GAVA1.** The dedicated authenticated direct-context exchange; INIT C→R/R→O and ACK bind capability+catalog hashes; DIRECT-CONTEXT C→O and ACK confirm inherited context before staging. Existing GID1 is **not** a substitute (`RecoveryConnection`/`DirectSession`).
- **Original-ID preservation.** Fresh C-generated session/execution/lease bind capability, all GAVA1 frames and the original topology; preserved through integrated preparation instead of regenerated.
- **Identity/generation binding.** Exact artifact/source/profile/host/boot/phase; no `latest`, fallback, alternate runtime or adoption.
- **Immutability/approval/refusal.** Root-owned metadata is immutable by non-root; only root may approve changes; absence/unsafe/malformed/duplicate/extra/mismatched/expired input refuses.
- **Open decision.** The validation-catalog approval root `/etc/gagamba/accepted-validation-catalogs/<Hk>.json` has not been the subject of an in-repo owner decision analogous to F02's `accepted-qualifications`. This is a recorded gap (class 3).

### 4.4 Prepublication verification

- **What must be checked before publication.** Exact candidate digest, protected K/M/F/S, host/boot/platform, original IDs; the **separate** prepublication integrity projection (`validation-bootstrap-compatibility.md` line 9: `InstalledFileSystem.VerifyActive:139-147` returning `InstalledUnqualified` before `InstalledPayload.CheckHost:156` "is not sufficient prevalidation evidence"; `InstallationIdentity.Validate` requiring `Qualified=true` is why a distinct pre type is needed).
- **Authoritative evidence.** Root-owned protected bytes and the fixed-root reads; never self-attested.
- **v3 pre-parser role/constraints.** Prepublication implements `v3 validation-pre` with `status0` and separate `InstalledIntegrity`/`ValidatedPreSession`; authenticated INIT and matching pre catalog before decoding positive bootstrap; exact `AuthorityMode`/`AuthorityHash`, `8192`-byte bound and phase-specific echo layouts from the frozen contract. Old parsers refuse unknown versions/modes.
- **Malformed/incomplete/contradictory/stale/substituted.** Refuse; no synthetic `Qualified`, no wrapping as `LoadedInstallation`.
- **Re-verification after restart.** No disk resume or authority reconstruction; a fresh independently approved post session is required after publication changes the receipt/phase.
- **Partial validation exposure.** The design forbids exposing partial validation; a prepublication session may not upgrade to postpublication authority (`validation-bootstrap-assessment.md`; owner decision §3.3).

### 4.5 Atomic R/O/C pre-writer ordering (state model)

Accepted ordering (owner decision §3.4; plan §3.5):

`C action0/B2 consumed -> R verifies INIT/catalog/policy -> R existing intent/journal writer + single O spawn -> O validates inherited INIT/role/integrity, context ACK, receives Start (no writer) -> R sends DirectBootstrap (C pidfd + safety lifeline) after Start, before WriterReady -> O arms safety monitor -> O sends DirectRendezvous; R forwards to C without WriterReady -> C/O GID1 + GAVA1 direct context -> O enters RunTransaction, acquires/proves writer, sends WriterReady, receives StartAuthority -> staging/domain/manifest/HELD -> pre-phase generation gate -> sole physical RELEASE`.

State-transition model and failure-between-steps test:

| Transition | Predecessor required | First irreversible/observable effect | Atomicity | Crash/restart | Refusal/cleanup |
|---|---|---|---|---|---|
| S0→S1 C crosses B2 consumption | accepted B2 | B2/action0 latch burned | one-use | no resume | refuse; original stop |
| S1→S2 R verifies INIT/catalog/policy then intent+spawn | S1 | **R journal writer + durable `AllocationIntent`** (`M1RecoveryScope:371`) | R writer handoff/disposal before O spawn unchanged | no disk resume | `FailOwnerPreparation`; retained recovery resources |
| S2→S3 O inherited-INIT/role/integrity, context ACK, Start | S2 | O context ACK (no writer) | C/R/O atomic | bounded deadline | refuse; O never writes |
| S3→S4 R sends DirectBootstrap (after Start, before WriterReady) | S3 | safety-lifeline transfer; safety monitor armed | R/O atomic | stop serviced | close delivered fds; stop |
| S4→S5 O DirectRendezvous; R forwards | S4 | rendezvous address only (never a release endpoint) | R→C forward without WriterReady | bounded deadline | refuse; no second owner |
| S5→S6 C/O GID1+GAVA1 direct context | S5 | authenticated direct context | **atomic**; mixed old/new refuse | no reconstruction | bounded refusal; stop |
| S6→S7 O RunTransaction, writer acquire/prove, WriterReady, StartAuthority | S6 matching context | **O journal writer acquisition** (first O protected effect) | not partial | writer fence on O death | refuse; stop |
| S7→S8 staging/domain/manifest/HELD | S7 | staging dir/files, domain, Prepared, capability | P2 early/late | accounted cleanup | abort; never completion |
| S8→S9 pre-phase generation gate | S8 | fresh protected read (no new effect) | single decision | accepted residual | refuse -> zero attempts + existing stop |
| S9→S10 sole RELEASE | S9 | sole physical `TryWriteOnce` | at most one attempt | post-observation residual | stop under original C3 |

Tested against failure between every adjacent step: the only **newly moved** boundary is S3–S6 (context before O writer). An interruption there occurs before the owner's first protected writer effect and is covered by the existing bounded deadline + safety monitor; R's earlier intent/journal effect at S1→S2 is unchanged and still precedes the owner. The design admits a **post-observation residual** and does **not** promise atomicity between the verifier's last stable observation and the physical write (owner decision §2). Where atomicity depends on the frozen GAVA1/K/M/F/S contract (S5→S6), this review records it as **implementable but not yet specified in-tree** rather than inventing an implementation. No "publication-before-authorization" or "publication-before-verification" path is permitted: the prepublication session cannot upgrade to postpublication, and no synthetic `Qualified`/reused grant may enter the normal gate.

---

## 5. Threat and failure matrix

Boundary legend: A/L/C/R/O/D and the protected store. "Evidence class": D deterministic, U unprivileged process, N native (Slice N).

| # | Scenario | Required invariant | Expected refusal/recovery | Responsible boundary | Evidence class |
|---|---|---|---|---|---|
| T-1 | Forged validation authority | Only L-issued capability after A/P is authority | Refuse; no manifest/staging/HELD/RELEASE | A/L/O | D + N |
| T-2 | Stale/replayed approval | Fresh nonce/boot/sequence; one-use latches | Refuse; burn latches; no new topology | L/C/R/O | D + U + N |
| T-3 | Catalog/artifact substitution | Capability/K bind exact digests/profile/phase | Refuse before effects | L/C/R/O | D + N |
| T-4 | Launcher replacement | L pinned/root-owned; reject stale/foreign pidfd | Refuse; close unexpected fds | C/R/O | D + N |
| T-5 | Privilege/custody confusion | Permanent drop; exact SCM rights; no fd window | Refuse; close on bad cmsg | L/C/R/O | U + N |
| T-6 | Identity/generation mismatch | Exact identity + qualification-ID/raw-hash binding | Refuse; never adopt newer | O/C | D + N |
| T-7 | Partial protected-resource acquisition | Acquire-then-verify; no partial authority | Accounted cleanup; never completion | O/R | N |
| T-8 | Process termination during bootstrap | Bounded deadlines; safety monitor | Original stop/reap; `Unknown` preserved | R/O | D + U + N |
| T-9 | Crash between verification and publication | No partial exposure; phase boundary | Refuse on resume; fresh post invocation | O/C | D + N |
| T-10 | Crash during R/O/C pre-writer ops | Context-before-writer atomic; writer fence | Stop service; no authority reconstruction | R/O | D + N |
| T-11 | Concurrent validation attempts | One-use issuance/action latches; at-most-one attempt | Refuse; `C4` duplicate semantics retained | A/L/O | D + U + N |
| T-12 | Recovery from incomplete durable state | No disk-only reconstruction | `Unknown`; no release | R/O | N |
| T-13 | Corrupted/contradictory evidence | Strict canonical parse/reencode; exact rechecks | Refuse | All | D + N |
| T-14 | Debug-only authority leaking into RELEASE | Compile-time exclusion; production verifier | Release contains no test factories | Build/verifier | D + N |
| T-15 | Prepublication session upgraded to post | Phase/receipt/active change refuses | Refuse; fresh post invocation | O/C | D + N |
| T-16 | Validation used for another workload | Exact F/M/S vs request/staged bytes | Refuse before corresponding effects | O | D + N |

No control in this table is represented as implemented unless §3 classifies it (1). Forged authority, launcher replacement, GAVA1, custody and all protected positives are **proposed/accepted-not-implemented** and cannot be qualified until Slice N.

---

## 6. P4 interaction assessment

Impact relative to the accepted P4 refusal-only coverage (`rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md`; 29-row reconciliation).

| P4 boundary | Impact | Evidence |
|---|---|---|
| New production callers of P4-protected sinks | **RE-AUDIT REQUIRED** for validation-enabled packages | A validation session re-enters `RecoveryHost.RunCore`, `IntegratedRecoveryHost.Run`, `M1RecoveryScope.StartOwnerPreparation`, `OwnerPreparationTransaction.Run` and adds an L→C issuance path |
| S10/S11 three-caller domination (F-R1/O-R1) | **RE-AUDIT REQUIRED** | The installed route remains F01-gated, but the validation ordering amendment changes where the owner's first protected effect sits; the closed caller graph must be re-established for validation entries |
| S28 test-only (O-R2) | **UNCHANGED** (verify) | Validation does not construct `PreparationOwnershipCoordinator`; keep it production-unreachable |
| F01/F02 and provider-gate dominance (S02/S03/S07–S09) | **RE-AUDIT REQUIRED** | `IntegratedRecoveryHost.Run:23` must remain the first statement for **all** validation entries; provider gates must not be bypassed by a validation branch |
| Qualified→legacy downgrade | **RE-AUDIT REQUIRED** | `RecoveryProtocol.Validate` (`:156–177`) and `OwnerDirectConnection` classification must refuse validation→legacy and legacy→validation; no boolean/flag path |
| First protected effect | **RE-AUDIT REQUIRED** | Moved for validation sessions (O writer after direct context); must be documented and re-verified |
| RELEASE refusal behavior | **RE-AUDIT REQUIRED** | The pre-phase generation gate must add refusal, never weaken the sole-writer/one-attempt/C3 rules |
| Observation-to-effect residual | **UNCHANGED** | Preserved, not extended (owner decision §2) |
| `NATIVE-PENDING` rows / 331 native skips | **UNCHANGED** | Still not passes |

Only the **inert** first package (§7, E-1) is expected to be P4-neutral; every package that adds GAVA1/validation-wire/ordering requires a fresh P4 sink and authority audit before its first protected effect (obligation O-R1).

---

## 7. Slice E versus Slice N boundary

| Concern | Slice E (this milestone) | Slice N |
|---|---|---|
| Source/module design of A/L/D, K/M/F/S readers, GAVA1, v3-pre, contexts | Yes (behind unconditional refusal) | No |
| Deterministic (V/I) tests | Yes | No |
| Real privilege drop, SCM/pidfd custody, loaded closure | Interface/refusal only | **Yes** |
| Real protected S/R positives, pre/post/normal | refusal-only probes | **Yes** |
| Generation switches and races | pure/deterministic | **Yes** |
| Native RELEASE, stop, reap, partial cleanup | not claimed | **Yes** |
| Provisioning of protected stores/host identity | No | **Yes** |

Slice E must not be declared complete, and no positive validation may be enabled, on deterministic evidence alone.

---

## 8. Recommended implementation-package sequence

Positive validation enabling is **atomic** (plan §3.5; handoff §"Bounded work"). The sequence below is therefore ordered by *permitted development boundary*, not by independently-enabled authority. Only packages that keep the system refusing may be implemented before the atomic enablement package; none may be enabled partially.

**E-1 (recommended first package) — non-forgeable prepublication integrity projection (refusal-only, unreachable).**
- **Commission ID:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-INTEGRITY-PROJECTION`.
- **Purpose:** produce the distinct prepublication integrity selection required by owner decision §3.3, without a qualification record and without a synthetic `Qualified`.
- **Authorized architectural scope:** extract/reuse the trusted S2 filesystem/payload/host/loaded-role checks into an internal, non-forgeable prepublication integrity projection that (a) performs full fixed-root integrity + platform/host + loaded-role verification, (b) carries a null qualification association, and (c) is a **distinct type**, never `InstalledUnqualified` alone and never a wrapped `LoadedInstallation`.
- **Existing prerequisites:** `InstalledFileSystem`, `InstalledPayload.CheckHost`, `LoadedInstallation` (class 1).
- **Expected components:** `Installed/InstalledFileSystem.cs`, `Installed/InstalledPayload.cs`, `Installed/InstalledMetadata.cs` (new state/type), possibly a new `Installed/PrepublicationIntegrity.cs`.
- **Required invariants:** non-forgeable; no injectable verifier; Release excludes any test seam; exactly one production factory; no consumer wired.
- **Permitted changes:** internal verification/result types and their deterministic tests.
- **Exclusions:** no GAVA1, no K/M/F/S, no v3 wire, no `OwnerAuthorityContext.Validation` factory, no issuer/L/D, no ordering change, no live path.
- **Evidence:** unit/negative tests for absence/mismatch/malformed and for "not a synthetic Qualified"; a gate-removal negative control.
- **Independent qualification:** deterministic V; native-pending for a real provisioned positive.
- **Acceptance:** production still refuses everything; no new authority; P4-neutral.
- **Stop:** do not wire a consumer.

Subsequent packages (each requires the noted decision before implementation):

- **E-2 — protected K/M/F/S readers + case policy** (refusal-only). *Reconciliation prerequisite:* commit the frozen catalog/fixture/policy/action schema in-repo and record the **validation-catalog approval-chain** decision (class 3).
- **E-3 — v3 validation-pre codec/parser** (refusal-only; old parsers refuse). *Prerequisite:* frozen v3 layouts committed in-repo.
- **E-4 — GAVA1 direct-context exchange** (refusal-only). *Prerequisite:* frozen GAVA1 header committed in-repo; **P4 re-audit**.
- **E-5 — L / privilege drop / original custody interface** (refusal-only). *Prerequisite:* driver/L design committed in-repo.
- **E-6 — atomic enablement:** `OwnerAuthorityContext.Validation` factory + owner policy/correlation + phase-specific pre-write gate + the R/O/C ordering amendment + live GAVA1 + all sinks. Cannot be partially enabled. **P4 re-audit required.**
- **E-7 — driver D + production closure checks.**
- **N — Slice N** protected native qualification (separate authorization).

First recommended package: **E-1**. It is the smallest boundary that is fully specified in-repo (owner decision §3.3), inert, and independently qualifiable at the deterministic level, and it does not depend on the disputed external schema.

---

## 9. Qualification strategy

1. **Source-level architectural evidence:** exact committed source/tree, symbol-level citations, a negative control proving removal/bypass of the new seam fails the assertion, and a production-verifier check that Release excludes test seams.
2. **Managed deterministic tests (V/I):** parse/reencode/refusal, binding, ordering (including the R/O/C order via an instrumented trace), and "no synthetic `Qualified`" checks.
3. **Durable restart/crash probes (I/U):** no disk resume, latch/sequence transitions, writer fence on death.
4. **RELEASE-specific negative tests:** Debug-only factories absent from Release (`FinalAdmissionVerifier.Grant.ForTest`, `OwnerAuthorityContext.Qualified.ForTest`, `OwnerReleaseGenerationGate.VerifyObserved`); validation never becomes general admission.
5. **Protected native tests (Slice N):** real S/R/O, protected stores, custody, races, native RELEASE/reap/cleanup.
6. **Actual protected installed positive execution:** only after Slice N; no test-only factory, mocked root approval or Debug qualification substitutes.

Impossible-until-Native claims: real privilege drop/custody; real GAVA1 with SCM credentials; a valid prepublication positive; valid post publication positive; native RELEASE; native stopping/reaping/partial cleanup. These must be reported as `NATIVE-PENDING`, and the 331 native skips remain not-passes.

---

## 10. Formal entry disposition and qualifications

**ENTRY SUPPORTED WITH QUALIFICATIONS.**

- **What the current evidence proves:** the P4 refusal-only coverage is accepted (29-row reconciliation); the existing S2/final-admission separation and protected readers are implemented and reviewed (P1–P4); validation authority is currently non-constructible; the Slice E atomic dependency set and the validation-only R/O/C ordering are accepted design.
- **What remains proposed:** the detailed frozen validation contract (K/M/F/S schemas, GAVA1 header, v3-pre layouts, release correlation) is accepted by hash through F02/Alternative B but lives only in external packages. The validation-catalog approval chain is a proposed administrative mechanism without an in-repo decision.
- **What remains blocked:** no package may enable positive validation until the complete set is present; E-2..E-5 cannot start until the frozen schema is committed in-repo and the approval-chain decision is recorded; all protected positives await Slice N.
- **Assumptions preserved:** trusted host root/kernel/filesystem/topology; the corrected observation-to-effect residual; recovery stop-only and sole owner writer; no new issuer/writer; P1–P4 qualifications and all 331 unqualified native skips.
- **P4 re-audit:** required for E-4 and E-6 (GAVA1/wire, validation context, ordering), per obligation O-R1; E-1..E-3 are expected P4-neutral but must be confirmed.
- **Slice N dependencies outside scope:** provisioning, real custody/privilege drop, protected positives, native release/stop/reap.
- **Independently qualifiable first package:** yes — **E-1**, at the source/deterministic level (native-pending).

Binding qualifications for any subsequent authorization:

1. Positive validation enabling is atomic; no partial exposure, no boolean/flag bypass, no temporary synthetic `Qualified`.
2. The frozen validation contract must be committed as an authoritative in-repo record before E-2..E-5.
3. The validation-catalog approval chain requires an explicit owner decision.
4. The R/O/C ordering amendment must change R/O/C atomically; mixed old/new actors refuse under bounded deadlines.
5. P4 re-audit is required for E-4/E-6 before their first protected effect.
6. No `OwnerAuthorityContext.Validation` factory may land before the complete issuer/context/policy checks (per the existing source comment).
7. All native/protected positives remain `NATIVE-PENDING`; tests are not results.

---

## 11. Proposed next owner-decision commission

`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-ENTRY-OWNER-DECISION-01` — decide the bounded authorization for **package E-1 only** (non-forgeable prepublication integrity projection, refusal-only, unreachable), and record the reconciliation prerequisites for E-2..E-5 and the validation-catalog approval-chain decision. This review does not authorize that package; a separate owner decision is required.

---

## 12. Evidence and limitations

- **Inspected:** the in-repo records in §2.1; the external frozen records in §2.2; source files in §2.3–§2.4 and §4; repository-wide searches for GAVA/`InstalledIntegrity`/validation stores (all absent).
- **Newly generated:** this review record only.
- **Not executed:** no build, no test run, no native run, no provisioning; the full suite was not rerun. No production or test source changed; no file was created outside `docs/`.
- **Explicit limitations:** this is a read-only architectural review. It does not independently verify the external frozen bytes beyond the hashes recorded by prior owner decisions. It does not qualify any native behavior. It does not reopen the accepted P4 coverage or the P1–P4 qualifications.
- **Preservation:** one canonical worktree on `main`; all 44 historical untracked paths preserved; no branch, no worktree, no reset/rebase/clean, no push.
