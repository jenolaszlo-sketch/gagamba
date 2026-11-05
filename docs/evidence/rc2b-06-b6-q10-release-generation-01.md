# GAGAMBA-RC2B-06-B6-Q10-RELEASE-GENERATION-01 — source-bound evidence

Commission: pre-RELEASE active-generation verification and refusal safety (Q10
mandatory non-waiver closure). Executor: GLM Flash in OpenCode, 2026-11-05.
Independent reviewer: Terra High (separately commissioned). Disposition:
**Q10 IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT REVIEW**; native
root-owned qualification remains pending and unclaimed.

## 1. Source identity

- Starting commit `c336df2b40c5615f16874cfa2a25cb36a3feba85` (tree
  `e3be5938e4086074f6dc39fcd8147bbe38fca30f`; branch `main`, single canonical
  checkout).
- Implementation commit `7251c37349dc08ac39a34160af889cc25c85e6af`
  (tree `9cdb6b69dbf74aa7f4d0dcca10aa332ececcf471`; 6 files, +226/−5): the Q10
  pre-RELEASE reread and refusal.
- Preflight verified: HEAD `c336df2`, tracked/staged clean, exactly the 44
  historical untracked paths (all still present after implementation), one
  registered worktree, 99 branches, Mechanism B commits reachable, no
  concurrent agent. Pre-implementation verified bundle (complete history,
  mirror-clone proven, 99/99 branches):
  `Solo/gagamba-q10-release-generation-01-glm-20261105T214254Z/bundles/gagamba-all-refs-c336df2b40c5.bundle`,
  SHA-256 `17859d3d7e3355962cffa7a5f996ac46f19fab218e8af82427de778ce8695d99`.

## 2. Governing requirements

- [Design §5.2 item 8](../design/rc2b-06-b6-installed-artifact-trust.md) ("Before
  release"): the controller re-reads `active` and requires the operation's
  generation to still be active; if it changed, the controller does not send
  `ReleaseRequest`; it submits the existing stop path and records the result.
- [Owner acceptance §3 Q10](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md)
  (mandatory non-waiver): mismatch prevents a new request and uses the existing
  stop path with the original context and C3 authority; approval withdrawal is
  not an active-generation switch; an in-flight request remains subject to the
  existing owner arbiter, stop precedence, at-most-one physical attempt and
  honest Unknown rules; no retroactive cancellation.
- Prior independent findings: Q1 O1/O2 (`831c980e…` review) and the continuation
  report's Q10 assessment — both inspected send sites lacked the check; this
  slice closes that gap without touching the owner-side arbiter.

## 3. RELEASE call-site inventory (production)

| # | Sender | Route | Bound identity | Fresh active observation before this change | Status |
|---|---|---|---|---|---|
| 1 | `IntegratedB3Client.Release` (`Bootstrap/IntegratedB3Client.cs`) → `OwnerProtocolMessageType.ReleaseRequest` | qualified integrated | S2 `InstallationIdentity` (Generation = ArtifactSetSha256) retained immutably by `QualifiedRecoveryConnection` (`Installation=selected`); owner echo + deadline revalidate at preparation | admission/preparation only (`LoadedInstallation.Verify`, `VerifyFinalAdmission`, `InstallationDeadline.Verify(Revalidate)`) | **Q10 gate added** inside the send critical section |
| 2 | `M1RecoveryScope.ReleaseHeld` (`Lifecycle/M1RecoveryScope.cs`) → `ReleaseRequest` | legacy nonintegrated | preparation Guids only; no installed-generation mechanism | none (out of scope of the B6 installed-generation model) | unchanged; accepted legacy contract preserved |
| — | `HeldReleasePhase.HandleReleaseRequest` (owner) | receiver | retained capability | — | unchanged (sole arbiter writes once) |
| — | `tests/Fixtures/B2Controller` | test fixture calling `RecoveryConnection.Start` | — | — | unchanged; stop-only lifeline; part of the separate sink-coverage project |

`B3Qualification.TryCreate` refuses `QualifiedInstallation` without
`IntegratedRecovery` (`InvalidBinding`), so every qualified operation is
integrated and retains an S2 selection; the legacy route is never qualified.
Route 2 therefore has no trusted active-generation identity to re-read; adding
one would change the accepted legacy contract and was not done. "Generation" in
Q10 is the S2 installed generation (`ArtifactSetSha256`), distinct from the
session-scoped preparation `GenerationId` Guid.

## 4. Production changes (`7251c37`)

1. **New `Installed/ReleaseGenerationGate.cs`** — the single Q10 decision:
   `RequireStillQualified(fresh, bound)` (Qualified state + exact `Identity`,
   `QualificationId`, `QualificationRecordSha256`), `MapFailure`
   (`InvalidDataException` → `UnqualifiedInstallation`; any other observation
   failure → `InstallationUnavailable`), and `RequireBoundGenerationActive`
   (null retained selection refuses fail-closed). The gate never adopts a newer
   generation and never accepts caller-supplied trusted state.
2. **`LoadedInstallation.Revalidate()`** now delegates to
   `RequireStillQualified(InstalledFileSystem.VerifyActive(), Selection)` —
   identical semantics (same condition and message); admission and RELEASE use
   one decision.
3. **`IntegratedB3Client.Release`** — inside the existing `_exchange` critical
   section and immediately before request construction: if the operation
   retains an installation (`bound is not null`), run
   `RequireBoundGenerationActive`; on refusal return a typed
   `HeldOwnerReleaseOutcome` (`GenerationRefusal`) with **no request sent**.
   Unqualified experimental integrated operations (no retained installation)
   keep their accepted behavior.
4. **`HeldOwnerReleaseOutcome`** gains `GenerationRefusal`
   (default `RestrictedRefusalReason.None`); all existing constructions are
   source-compatible.
5. **`B3Qualification.ReleaseCore`** — on a generation refusal: submit the
   existing stop path (`StopCore(RestrictedStopReason.Invalidation)` →
   `StopCauseKind.SetupFault`, original context and C3 authority) and return
   `RefusedResult(reason)` = `Refused / NotCommitted / NotAttempted / Unknown`.
   The release verdict is independent of the stop outcome; retained stopping
   authority is unchanged.

**Ordering:** the fresh `Revalidate()` (argument-free trusted `VerifyActive`
re-entry) is the last action before request construction, inside the same lock
as the send, with no intervening I/O.

**Documented residual race:** between the observation and the owner's release
effect a root actor can still change the installation. This is the accepted B6
residual ("Artifact changed after verification, before release — root actor;
documented"); the owner echo, immutable generations and the stop path remain
the detection/authority layers. The accepted contract requires the reread
"immediately before" the request, which this implements; stronger
synchronization (e.g., holding an installation read lock across the owner
effect) would be a new security design decision and was not attempted.

## 5. Tests (`tests/.../Installed/ReleaseGenerationGateTests.cs`, deterministic, 9/9)

- Unchanged active generation verifies (positive; authorized RELEASE remains
  possible).
- Changed active generation refuses → `UnqualifiedInstallation` (never adopts).
- Missing active selector refuses; unqualified/fresh selections refuse.
- Replaced qualification record refuses; changed qualification id refuses.
- **Earlier admitted selection does not substitute for the fresh reread** —
  bound selection is Qualified by construction; a vanished or changed active
  selection still refuses.
- Observation-failure mapping: `IOException`/`UnauthorizedAccess`/unexpected →
  `InstallationUnavailable`.
- Missing retained selection refuses fail-closed (`q10:` typed error).

Not deterministically executable on Windows (require the trusted fixed root
and live topologies; existing native suites cover the unchanged paths):
generation change while HELD with stop still available; controller loss;
recovery/owner loss; deadline expiry during verification; duplicate/racing
physical-attempt semantics (operation-level in-flight latch
`RestrictedExecutionOperation` C4 + owner arbiter unchanged, existing
`ReleaseArbiterTests`/B3 suites); legacy configuration regression (legacy path
untouched). These remain required native qualification rows — none is counted
as passed.

## 6. Verification results (executor-run, Windows, Debug)

- `dotnet build Gagamba.sln -c Debug`: **0 warnings / 0 errors**.
- Focused `ReleaseGenerationGateTests`: **9 passed / 0 failed / 0 skipped**.
- Focused `FinalAcceptanceTests`: **69 passed / 0 failed / 0 skipped** (unchanged).
- Full Windows M1 runner: **1266 passed / 331 skipped / 0 failed (1597 total)**
  — the prior 1257/331/0 plus the 9 new deterministic tests; all 331 skips are
  the pre-existing Linux-native classes. Native skips are not passes.

## 7. Lifecycle authority preservation

No new RELEASE writer (the owner arbiter is untouched and remains the sole
physical writer); no RELEASE authority to recovery; no C3 renewal (stop path
uses original deadlines; gate adds no deadline logic); HELD untouched;
stopping is never reported as release (`Refused/NotCommitted/NotAttempted`);
no authority reconstruction from disk (the gate only refuses); no generation
adoption; Unknown cleanup vocabulary unchanged; no production activation (the
public factory remains unavailable); no test-only production bypass.

## 8. Still pending (not claimed)

Native root-owned S2/protected-store qualification; the live-topology Q10
matrices (switch-while-HELD, in-flight ordering, Unknown) on a disposable
trusted-root host with delegated cgroups; the separate admission sink-coverage
project (the `RecoveryConnection.Start` internal initialization route remains
open under Q04/S09). Q1's blocked-by-environment disposition is unchanged.

## 9. Independent reviewer questions

1. Does `RequireStillQualified` capture the full accepted Q10 equality
   (state, identity, qualification id, record hash) — and is comparing the
   raw receipt-selected record hash the correct binding (vs the final-record
   hash, which admission binds separately)?
2. Is refusing — rather than failing-Unknown — the correct public verdict when
   the fresh observation cannot complete (missing selector, unreadable root)?
3. Is `StopReason.Invalidation → SetupFault` an acceptable existing-vocabulary
   mapping for the auto-submitted stop after a generation refusal?
4. Does the unqualified-integrated skip (`bound is null` → no gate) correctly
   preserve the accepted experimental contract without creating a Q10 bypass
   for any qualified route (TryCreate refuses qualified-without-integrated)?
5. Is the documented observation→effect residual acceptable for this slice, or
   does the accepted contract require stronger synchronization before native
   RELEASE qualification?
6. Do the 9 deterministic tests plus the named native-pending rows cover the
   required case list, and which additional native rows must the qualification
   matrix add?
