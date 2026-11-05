# B6 integrated Start authority bounded implementation handoff 01

Date: 2026-11-06 (Asia/Manila)

Commission context: `GAGAMBA-RC2B-06-B6-INTEGRATED-START-AUTHORITY-OWNER-DECISION-01`

Owner authority: [Alternative B design acceptance](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md)

Canonical baseline: commit `cbf1a98b157d06243c488bf723d8eca46518a36e`, tree `0b1966772365fa0336d121a3b1352615f0ad1605`

Status: **IMPLEMENTATION HANDOFF PREPARED - DESIGN ACCEPTANCE ONLY - NO IMPLEMENTATION AUTHORIZED**

## 1. Purpose

This handoff converts the accepted Alternative B design into bounded implementation slices. It identifies:

- the refusal-only changes that can safely precede the atomic validation-bootstrap dependencies;
- the validation-bootstrap dependencies that must not be partially exposed;
- the exact owner, recovery and controller boundaries affected;
- the required deterministic, unprivileged-process and protected-native evidence;
- the preservation and stop conditions for the next commission.

It does not authorize implementation, provisioning, publication, native qualification or production activation. No source or test code was changed in preparing it.

## 2. Accepted invariants

Every slice must preserve:

- owner-side manifest authorization for new qualified authority;
- owner-side fresh active-generation verification after durable intent and immediately before the final pre-write gate;
- the existing controller-side Q10 guard;
- the owner as the sole physical RELEASE writer and at most one physical attempt;
- the original C3 absolute deadline and Policy A expired-stop behavior;
- original C1 ambiguity, C2 stop consumption, C4 duplicate and first-correlation binding;
- exact original installation generation and first qualification-record association, with no reassociation or adoption;
- immutable original actor, session, execution, lease and preparation identities;
- stopping, reaping, reconciliation and cleanup for already admitted operations without renewed general admission;
- conservative `Unknown` outcomes and no fabricated completion;
- legacy v1 behavior without installed or validation claims;
- no recovery manifest authority or RELEASE authority;
- public production activation remains unavailable.

## 3. Slice ordering

### 3.1 Slice P1 - retain owner selection and add private contexts

**Safe before atomic validation bootstrap: yes; inert/unreachable until a gate consumes it.**

Work:

- Return the actual verified `LoadedInstallation` from `QualifiedOwnerPreflight.Verify` instead of discarding it.
- Retain the full `InstallationIdentity`, manifest, `QualificationId` and `QualificationRecordSha256` from the first owner-local verification.
- Add sealed, internal, disjoint owner contexts for normal qualified, independently authorized validation and legacy experimental operations.
- Keep the validation context unreachable and non-constructible from ordinary callers.
- Keep the owner's later direct-echo comparison against the first retained tuple; it may not replace that tuple.
- Add deterministic tests proving that a later echo or caller Boolean cannot replace the first selection.

Exit:

- No positive validation path.
- No general-admission claim from context retention alone.
- Public production remains unavailable.
- Existing legacy behavior and accepted tests remain unchanged.

### 3.2 Slice P2 - normal qualified owner manifest authorization, refusal-only

**Safe before atomic validation bootstrap: yes, as a general-only narrowing gate; not bootstrap-complete.**

Work:

- Require the retained normal qualified context before new installed allocation, staging, capability minting or HELD.
- Reuse existing protected `VerifyFinalAdmission()` and `IsFinalAdmissionEligible(...)`; do not create a second trust root.
- Perform a fresh protected final-admission check at the operation-bound manifest acceptance, after the manifest is known and before the `Prepared` transition, capability mint or publisher call.
- Refuse missing, malformed, unavailable, stale, replaced, withdrawn or mismatched authority.
- Keep S2 `Qualified`, final admission, validation and production activation distinct.
- Preserve original stopping and cleanup for partially staged resources.
- Classify installed authority from authenticated bootstrap identity, not `Profile.QualifiedInstallation`.
- Refuse installed paths that cannot supply the required private context; do not add a temporary bypass.
- Keep all validation construction and issuance refusing/unreachable.

Exit:

- Deterministic tests prove owner-side refusal before new owner writer, domain, staging, capability or HELD effects.
- Tests prove a controller assertion, bare hash, successful Start or false profile flag is insufficient.
- Tests prove a legacy selector cannot satisfy installed admission and verifier failure does not fall back to legacy.
- The slice is reported as incomplete for Q04/bootstrap until the complete sink inventory and atomic validation integration land.

### 3.3 Slice P3 - owner pre-RELEASE generation gate, refusal-only

**Safe before atomic validation bootstrap: yes; applies to already admitted normal qualified operations and introduces no validation authority.**

Work:

- Carry the retained owner selection and context across the in-process boundary into `HeldReleasePhase`; no wire bearer or disk reconstruction.
- After durable intent and the existing outside-lock after-intent barrier, run the fresh generation verification outside the runtime gate, journal gate, safety latches and capability-consumption lock.
- Compare the full retained qualified identity plus qualification ID and raw record hash; no null skip for a qualified operation.
- Immediately enter the existing final runtime gate and recheck retained result, stop, deadlines, prerequisites and capability liveness.
- On refusal, prevent a new physical attempt, submit the existing stop path under the original context and C3 authority, and retain the conservative post-intent result.
- Preserve the existing controller Q10 check and the existing sole writer.
- Add deterministic tests through the actual arbiter with a counting write channel, controlled clock/journal and instrumented verifier barriers. Do not test only the pure equality helper.

Exit:

- Missing, stale or mismatched active selection refuses with zero physical attempts.
- A switch during the intent window is detected if it is still observable at the verifier's last relevant stable observation.
- A switch after that observation is reported as the accepted residual, not as a promised detection.
- Concurrent release/stop still yields at most one physical attempt and preserves C1/C2/C4.
- Deadline expiry or stop wins during a blocked verification; a late worker cannot revive authority.

### 3.4 Slice P4 - pre-effect sink audit and missing-context refusals

**Safe before atomic validation bootstrap: only as refusal-only narrowing; it must not claim complete coverage.**

Work:

- Audit every installed production admission, allocation, helper-spawn, recovery-spawn, operation-construction, journal-effect, staging, preparation and release entry point.
- Include `RecoveryConnection.Start`, `QualifiedRecoveryConnection.Start`, original recovery startup, `IntegratedRecoveryHost`, owner startup and transaction entry.
- Where the authenticated path can already supply the required private context, require it before effects.
- Where the context cannot yet be supplied, refuse the installed path and record the path as incomplete. Do not add a permissive default, Boolean, test-mode bypass or legacy fallback.
- Preserve recovery's original stop, reaping and journal duties; do not move stopping authority to the controller or validation driver.
- Keep validation sessions refusing until Slice E is complete.

Exit:

- The audit produces an explicit sink inventory with implemented refusal, pending dependency or excluded legacy disposition for every entry point.
- No document or test reports P1-P4 as full Q04/S09 coverage or bootstrap compatibility.
- A direct Start caller that omits final admission cannot obtain new installed manifest, staging, HELD or RELEASE authority through an unguarded sink.

### 3.5 Slice E - atomic validation-bootstrap integration

**Safe before atomic validation bootstrap: no. This slice is the atomic dependency set and must not be exposed partially.**

Any exposed installed validation path requires all of the following to be complete together:

- independent live root authorizer A, fixed reviewed launcher L and protected catalog/approval chain;
- permanent privilege drop, original process custody, pidfds and SCM credentials;
- protected K/M/F/S acquisition, exact case policy and finite fixture;
- GAVA1 authenticated direct-context exchange;
- prepublication full integrity verifier and v3 pre bootstrap parser;
- original session, execution, lease, preparation and correlation identities;
- all pre-effect sink checks;
- owner policy and release-correlation recomputation;
- owner-side phase-specific final pre-write generation gate;
- stop service at every barrier and original deadline handling;
- exact driver dispatch, production closure checks and test-instrumentation exclusion;
- atomic recovery/owner/controller ordering for the validation-only early direct authentication sequence.

A positive validation path may be implemented in isolated internal modules behind unconditional refusal before Slice E is complete. It may not be reachable, enabled or described as complete until every dependency above is present.

The approved validation-only ordering is:

1. Controller crosses accepted B2 consumption and action 0; recovery verifies INIT/catalog/policy before its existing intent/journal effects and single owner spawn.
2. Owner validates inherited INIT, original recovery, local protected inputs and actual installed role/integrity, sends context ACK and receives Start specification, without acquiring a journal writer.
3. Recovery sends `DirectBootstrap` after Start and before waiting for `WriterReady`; owner arms its existing safety monitor.
4. Owner sends `DirectRendezvous`; recovery forwards it and the owner pidfd to the controller without waiting for `WriterReady`; controller and owner complete GID1 and GAVA direct-context.
5. Only after matching context and phase-specific installed checks may owner enter `RunTransaction`, acquire/prove the writer, send actual `WriterReady` and receive `StartAuthority`.

Recovery and owner must change atomically. Mixed old/new validation actors refuse under bounded deadlines with no fallback, retry, second spawn or second owner.

Exit:

- All positive validation requirements are present or the path remains unreachable.
- No temporary synthetic `Qualified`, Boolean skip, final-admission bypass or alternate issuer exists.
- Refusal burns applicable one-use/action latches and continues original stop/cleanup.
- B2 validation creates no writer/domain/staging; B3 no-release cases produce zero physical attempts; only the exact approved B3 release template may produce one.

### 3.6 Slice N - protected native qualification

**Safe before atomic validation bootstrap: no. Separate authorization required.**

Native qualification must use a separately authorized disposable host and independently approved exact cases. It must exercise:

- real non-root controller, recovery and owner processes;
- real protected installed stores and stable protected reads;
- normal, HELD, prepublication and postpublication validation, plus legacy controls;
- missing/withdrawn final authority and changed protection;
- descriptor, credential, seal and privilege-drop failures;
- generation switches before and after the last relevant observation;
- loss, expiry, concurrent release/stop, deadline expiry and cleanup;
- exact candidate, source, host, boot, kernel, runtime, feature and delegation binding.

Any new binary requires a new candidate digest and a complete evidence sequence. Missing, skipped or unknown mandatory native cases are not passes. Publication and activation remain separate decisions.

## 4. What may and may not precede Slice E

| Change | May precede E? | Condition |
|---|---|---|
| Owner selection retention and sealed contexts | Yes | Inert; validation unreachable; no general-grant claim. |
| Normal qualified owner manifest refusal gate | Yes | Narrowing only; explicitly incomplete for Q04/bootstrap. |
| Owner pre-RELEASE generation refusal gate | Yes | Normal qualified operations only; preserves controller Q10 and existing arbiter. |
| Missing-context refusal at installed Start/recovery/owner sinks | Yes | Refusal only; explicit pending inventory; no permissive default. |
| Validation positive path, issuer, GAVA1, v3 pre, privilege drop or driver | No | Atomic Slice E; unreachable until all dependencies complete. |
| Validation-only early direct ordering exposed to a live path | No | May be co-developed behind refusal, but R/O/C change atomically and no live validation is enabled early. |
| Native protected positives or generation-race qualification | No | Separate Slice N authorization and protected host. |
| Production activation, publication, package release or public factory | No | Separate owner decisions; not part of this handoff. |

## 5. Test and evidence expectations

The next commission must select exact runnable filters after implementation and report every suite, source revision, host and skip honestly. The independent handoff's T01-T23 matrix is the governing case list; the following grouping is the minimum evidence map.

| Group | Cases | Required level |
|---|---|---|
| Owner manifest refusal | T01-T05, T21 | Deterministic through owner wiring; native protected positive/negative for installed rows |
| Actor/descriptor/session | T06-T09 | Deterministic and real unprivileged process/descriptor tests |
| Controller Q10 and owner release | T10-T12, T22 | Deterministic after-intent/final-gate wiring; native for protected active reads and residual |
| Concurrency/deadline/stop | T13-T15 | Deterministic controlled clock/journal/writer; unprivileged and native stop/race controls |
| Validation positive and scope | T16-T18, T20 | Deterministic plus protected native; no final acceptance in validation controls |
| Legacy compatibility | T19 | Deterministic and native legacy controls |
| Administrative ordering | T23 | Deterministic marker/lock ordering; native where protected stores are involved |

Required controls:

- **G:** exact original qualified installation and final acceptance produce one HELD and one physical RELEASE.
- **H:** genuine HELD enables controlled generation-switch, stop and deadline mutation tests with zero or one attempt as specified.
- **V:** fresh independently approved validation exact-case control, with B2/no-writer, B3/no-release and B3/one-release variants.
- **L:** accepted legacy v1 unqualified and stop-only B2 controls.

Every native pair must preserve exact source/tree and candidate hashes, host/boot/kernel/runtime/features/delegation, actual UID/namespace/pidfd relationships, protected metadata hashes, mutation cutpoints, owner decisions, manifest/capability/intent observations, target-init/exec oracle, physical writer attempt count, stop deadline, component cleanup and durable finalization. A missing output, disappearing PID, successful API return or skipped case is insufficient.

Inherited regressions must include `FinalAcceptanceTests`, installation-wire tests, recovery B2, direct authentication, release-arbiter/prepared-transaction, B3/INT-01, B4 capture/blocked-observer stop, B5 C3/reconciliation and the isolated D6.4 fixture, plus production/instrumented closure checks.

## 6. Exact source scope to be commissioned

Owner-boundary files:

- `src/Gagamba.Execution.Linux.M1/Bootstrap/QualifiedOwnerPreflight.cs`
- `src/Gagamba.Execution.Linux.M1/Bootstrap/OwnerDirectConnection.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/OwnerPreparationTransaction.cs`
- `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs`
- `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`
- a sealed owner-context implementation reusing `Installed/LoadedInstallation.cs`, `Installed/FinalAdmissionVerifier.cs`, `Installed/AdmissionAuthority.cs` and `Installed/ReleaseGenerationGate.cs`

Sink audit must also cover:

- `Bootstrap/RecoveryConnection.cs`
- `Bootstrap/QualifiedRecoveryConnection.cs`
- `Bootstrap/IntegratedRecoveryHost.cs`
- `Bootstrap/IntegratedB3Client.cs`
- `Restricted/B3Qualification.cs` and B3 preparation-owner construction
- `Lifecycle/M1RecoveryScope.cs`
- owner/recovery entry points, trusted launch fd mapping and production closure gates

Validation later additionally requires the accepted strict new authority components, protected readers, GAVA1, v3 pre codec, exact case policy and driver. Those are not part of P1-P4.

Do not modify `Launch/HeldExecution.cs` transport behavior, create a new writer, add a new journal schema or change public APIs in the refusal-only slices.

## 7. Preservation and verification

- Work directly on canonical `main`; no branch or linked worktree.
- Preserve the 44 historical untracked paths and the single canonical checkout.
- Do not rewrite historical evidence, reviews, reviewer bytes or prior owner decisions.
- Do not create protected records, host identities, packages or approval stores on the real host.
- Build and test commands must be recorded with exact configuration and SDK.
- Native skips remain skips; unavailable native qualification is reported as pending.
- No push, history rewrite, reset, rebase, stash, clean, prune or force operation.

## 8. Exit criteria for the next implementation commission

The next commission may report a refusal-only slice complete only when:

1. The exact source and tests are committed on canonical `main`.
2. The slice is explicitly marked incomplete for bootstrap/Q04 where applicable.
3. Deterministic tests exercise owner wiring and the final after-intent path, not only pure helpers.
4. A negative control proves that removing or bypassing the owner wiring fails the expected assertion.
5. Every native or protected-store positive is explicitly pending unless separately executed and reviewed.
6. The controller Q10 path and sole-writer behavior are preserved.
7. Stopping and cleanup remain available without renewed general admission.
8. Preservation of the 44 untracked paths and one canonical checkout is rechecked after commit.
9. No validation positive path, production activation, publication, package release or privileged operation is enabled.

## 9. Explicit non-authorization

This handoff does not authorize:

- production source or test modification;
- validation issuance, positive validation execution or GAVA implementation exposure;
- protected-store provisioning, host identity creation or privileged installation;
- native qualification or any claim derived from skipped/ported tests;
- final-acceptance publication, administrative withdrawal or quarantine;
- package publication, public factory enablement or production activation;
- branch, worktree, push or history operations.

The next step is a separate bounded implementation authorization that names the refusal-only slices approved to start and keeps Slice E behind its atomic dependency gate.
