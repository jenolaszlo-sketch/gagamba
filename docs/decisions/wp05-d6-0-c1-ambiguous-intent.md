# WP05 D6.0 C1 - R09 Ambiguous ReleaseIntent

Status: **PROPOSED - NOT APPROVED**

Decision owner: human WP05 release/security decision holder, after independent
GLM design review. DeepSeek is the design author; this record does not approve
or implement C1.

## 1. Original requirement

C1 (required-changes blob `73eb09af...`): distinguish a definite pre-Intent
persistence failure from an ambiguous seal-release failure in which both slots
may hold the fully durable Intent content while the write reports failure;
never report a false definite `NotCommitted`; in the ambiguous case request a
bounded stop; define the stop mechanics while the journal is uncertain and
unpersistable; preserve eventual `MayHaveExecuted` recovery and no FD4.

Requirement IDs: R09; blocked gates G3-A11 and G3-B08.

## 2. Current R12 behavior (`SOURCE-CONFIRMED`)

- `ReleaseArbiter.RequestRelease` calls
  `OwnershipJournal.WriteTransition` inside `_journalGate`
  (`Execution/ReleaseArbiter.cs:110-114`).
- Every failure is collapsed to
  `Refused(NotCommitted, "release intent was not durably acknowledged")`.
- `OwnershipJournal.Write` writes complete, fsynced content into both slots
  before removing `gen.seal` (`Recovery/OwnershipJournal.cs:424-489`). A final
  `Unlink(gen.seal)` failure returns `"seal release failed, commit ambiguous"`
  while both slots can be durable (`:486-489`).
- `Resolve` treats any present seal as uncertain (`:773-780`).
  `TryRecoverCommit` requires the real fence and refuses while a live writer
  holds it (`:712-770`).
- `RequestStop` accepts runtime stop, then attempts `WriteMutation`; under a
  seal that mutation fails and `DurableStop=false`, but runtime
  `_stopAccepted` remains (`ReleaseArbiter.cs:226-296`).
- `ReleaseRecoveryClassifier` maps any ReleaseRecord, including Intent, to
  `MayHaveExecuted` (`Recovery/ReleaseRecoveryClassification.cs:28-38`).

The live owner cannot run recovery while it retains the writer lease:
`TryRecoverCommit` takes the same exclusive fence and refuses while the lease
is live. A live lease must be relinquished only by process death/disposal, not
by handing it to a replacement. The only bounded stop channel in the wedge is
runtime acceptance in the live owner; a durable StopRecord cannot be written
while the seal blocks journal mutation. Durable stop evidence is best-effort;
the post-death seal recovery and the absence of resend are the backstop.

## 3. Independent evidence

- `SOURCE-CONFIRMED`: the two failure shapes are distinct in journal source but
  collapsed in `RequestRelease`.
- `PRODUCER-PROBED` / `INDEPENDENTLY REPRODUCED`: D2
  `AmbiguousSealFailureLeavesUncertainIntentAndNoStop`; D4 `D4-6`
  (`Refused(NotCommitted)`, 0 transport, seal error, recovery Intent ->
  `MayHaveExecuted`); D2 `DefiniteIntentFailureIsDistinctFromAmbiguousPersistence`.
- `UNPROVEN`: production owner-level bounded domain stop in the wedge; no
  production owner exists at R12 (WP08/WP10 deferred).

## 4. Candidate contracts

| Option | Contract | Assessment |
|---|---|---|
| C1-A | Add an explicit ambiguous-persistence release decision; `MayHaveExecuted`; no FD4; bounded runtime stop; best-effort durable stop; retained ambiguous result. | Recommended: truthful and explicit; small runtime API change; no schema change. |
| C1-B | Keep `Refused`, add a separate uncertainty classification field. | Viable but two fields must be checked; easier to misread as ordinary refusal. |
| C1-C | Keep `NotCommitted` and document that it may not describe durable truth. | Rejected: violates R09 and makes consumer retries/telemetry unsafe. |

## 5. Recommended C1 contract

1. Definite write failure before any Intent content is acknowledged:
   `Refused(NotCommitted)`, no FD4, no false durable claim. Capability remains
   live unless a separate stop wins (C2).
2. Ambiguous seal-release failure after both slots hold Intent:
   - `ReleaseDecisionKind.AmbiguousPersistence` (or an explicit equivalent
     named in the implementation review); `DurableRelease=MayHaveExecuted`;
     `Delivery=NotAttempted`; `DurableCommitted=false`;
     `LiveExecutionExcluded=true`; reason identifies journal ambiguity.
   - No FD4 and no retry of the intent write.
   - Request `RequestStop(StopCauseKind.CallerStop)` after releasing the
     journal gate. C2 consumes/invalidate the capability before any later
     release.
   - Runtime stop acceptance is the bounded channel; with a valid clock the
     runtime deadline is `now + 5_000_000_000 ns`; with clock unavailable or
     overflow it is zero. Durable StopRecord is attempted best-effort and may
     remain absent.
   - The live owner retains its fence until it exits or disposes; it does not
     transfer the lease to another process. Recovery after death acquires a
     fresh lease, resolves the seal, and classifies Intent conservatively.
3. The ambiguous result is retained. Later same-instance calls receive
   `Duplicate` of the ambiguous result; they cannot retry release. This is the
   C4 interaction.
4. Repeated ambiguity/stop persistence failure remains runtime-only; no false
   durable stop, no FD4, no fresh reattempt.
5. Operator-visible state must identify: runtime stop accepted, durable stop
   absent, journal ambiguous, no FD4 attempt, recovery required.
6. Capability state follows C2: not live after stop acceptance. If C1 lands
   before C2, C1 must still consume the capability itself; the recommended
   implementation order is C2 first.
7. Domain stop and cleanup execution remain WP08/WP10; C1 does not invent an
   owner integration or target behavior.

Linearization point: the capability consumption/stop acceptance under `_gate`
after the ambiguous journal result is observed. Durable Intent is already
content-durable; durable stop is necessarily best-effort.

Forbidden: false `NotCommitted`; FD4; second intent write; treating seal
uncertainty as absence; handing the live lease to a replacement; claiming a
durable stop when the write failed.

Production symbols expected: `ReleaseArbiter.RequestRelease`,
`ReleaseDecisionKind`/`ReleaseDecision`, possibly a small stop/uncertainty
field. No `OwnershipSnapshot` v3 change.

## 6. Rejected alternatives

- C1-B: not selected because the recommended explicit decision/uncertainty is
  less likely to be misread by consumers.
- C1-C: rejected as incompatible with R09's explicit no-false-NotCommitted
  requirement.
- Durable stop retry loop while sealed: rejected; it cannot create a durable
  StopRecord and would block the owner indefinitely.
- Releasing the writer lease in-process to let recovery run: rejected; it
  creates an unnecessary live-owner/second-writer ambiguity and is outside the
  single-writer contract. Process death/disposal is the handoff.

## 7. Acceptance tests (post-authorization implementation)

1. Definite pre-Intent write failure: `Refused`, no intent, no FD4.
2. Ambiguous post-content seal failure: explicit ambiguous decision, no FD4.
3. Both slots contain Intent after ambiguity (read slots/recovery evidence).
4. No false definite `NotCommitted`.
5. Bounded runtime stop requested and recorded; durable stop attempted.
6. Forced durable-stop failure during wedge: runtime accepted, durable absent,
   no claim otherwise.
7. Retained ambiguous result: later calls Duplicate; no second write.
8. Owner SIGKILL; fresh writer acquisition.
9. Seal recovery returns Intent; classifier `MayHaveExecuted`.
10. No second RELEASE/FD4 attempt; no second coordinator acceptance.
11. Capability not reusable after stop (C2).
12. Cleanup bounded; no target execution.

Reusable fixtures: D2 ambiguous seal probe, D4 fresh-process supervisor and
D4-6, D5 no-target process/status harness; tests must be rerun against the new
production tree and separately evidenced.

## 8. Compatibility, risks and open questions

- No schema change; runtime enum/decision compatibility matters. Internal
  consumers and tests must be updated for the new decision kind.
- New risk: callers might treat `AmbiguousPersistence` as a definite failure;
  documentation and decision tests must forbid that.
- The domain stop is not implemented by the arbiter; WP08/WP10 must consume the
  runtime stop and perform bounded domain cleanup.
- Open: exact name/field shape of the explicit decision; whether a distinct
  durable-uncertainty field is needed in addition to the decision kind.
- Open: human policy on whether a runtime-only bounded stop is acceptable when
  no durable StopRecord can be written. Recommendation: yes, with truthful
  uncertainty and recovery/operator backstop.

Review status: independent GLM design review required. Approval: **NOT
APPROVED**.
