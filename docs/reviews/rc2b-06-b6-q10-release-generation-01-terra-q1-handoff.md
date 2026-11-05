# Terra High Q1 handoff — GAGAMBA-RC2B-06-B6-Q10-RELEASE-GENERATION-01

Reviewer: Terra High (separately commissioned). Review target: implementation
`7251c37349dc08ac39a34160af889cc25c85e6af` (tree `9cdb6b69dbf74aa7f4d0dcca10aa332ececcf471`)
on canonical `main`, from baseline `c336df2b40c5615f16874cfa2a25cb36a3feba85`
(tree `e3be5938e4086074f6dc39fcd8147bbe38fca30f`). Executor: GLM Flash in
OpenCode, 2026-11-05. [Evidence](../evidence/rc2b-06-b6-q10-release-generation-01.md).

## What this slice claims

The Q10 mandatory non-waiver is implemented in source: the controller re-enters
S2's trusted fixed-root verifier immediately before constructing a new
`ReleaseRequest` (same critical section as the send, no intervening I/O) and
refuses — without a new request — unless the operation's bound installed
generation is still the active qualified selection with its exact qualification
id and record hash. A refusal submits the existing stop path (original context,
original C3 authority) and reports `Refused / NotCommitted / NotAttempted`.
Earlier admission-time or preparation-time verification never substitutes. The
bound generation is never replaced; approval withdrawal is not a generation
switch; in-flight requests keep the existing owner-arbiter, stop-precedence,
at-most-one-physical-attempt and honest-Unknown rules.

## Exact diff surface (6 files, +226/−5)

- New `src/Gagamba.Execution.Linux.M1/Installed/ReleaseGenerationGate.cs` —
  `RequireStillQualified` (shared with `LoadedInstallation.Revalidate`),
  `MapFailure`, `RequireBoundGenerationActive` (null retained selection refuses
  fail-closed).
- `Installed/LoadedInstallation.cs` — `Revalidate()` delegates to the shared
  decision (same condition and message as before).
- `Bootstrap/IntegratedB3Client.cs` — gate inside `Release`'s `_exchange`
  critical section, before request construction; unqualified experimental
  operations (no retained installation) skip the gate.
- `Lifecycle/M1RecoveryScope.cs` — `HeldOwnerReleaseOutcome` gains
  `GenerationRefusal` (default `None`); the legacy `ReleaseHeld` send path is
  otherwise untouched.
- `Restricted/B3Qualification.cs` — `ReleaseCore` handles a generation refusal:
  submit `StopCore(Invalidation)` (existing stop path, `SetupFault` cause) and
  return `RefusedResult(reason)`.
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/ReleaseGenerationGateTests.cs`
  — 9 deterministic decision tests.

## Executed results (producer-run, Windows, Debug, SDK 10.0.401)

Build 0 warnings/0 errors; `ReleaseGenerationGateTests` 9/9/0;
`FinalAcceptanceTests` 69/69/0 (unchanged); full Windows M1 runner
**1266 passed / 331 skipped / 0 failed (1597 total)** — prior 1257/331/0 plus
the 9 new tests; all skips are the pre-existing Linux-native classes. Native
skips are not passes. No privileged provisioning, no WSL2, no activation, no
push.

## Suggested verification commands

- `git diff c336df2..7251c37 --stat` and a full read of the six files above.
- `dotnet build Gagamba.sln -c Debug` (expect 0/0).
- `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build`
  (expect 1266/331/0).
- Focused: `--filter 'FullyQualifiedName~ReleaseGenerationGateTests'` (9) and
  `--filter 'FullyQualifiedName~FinalAcceptanceTests'` (69).

## Reviewer questions (also in the evidence, §9)

1. Is the `RequireStillQualified` equality complete and correct for Q10
   (Qualified state, `InstallationIdentity` value equality, qualification id,
   raw receipt-selected record hash)?
2. Refuse vs fail-Unknown when the fresh observation cannot complete — is
   `Refused/NotCommitted/NotAttempted` the truthful public verdict for a
   missing selector or unreadable fixed root?
3. Is `StopReason.Invalidation → SetupFault` acceptable existing vocabulary for
   the auto-submitted stop after a generation refusal?
4. Does the unqualified-integrated skip (`bound is null`) preserve the accepted
   experimental contract without opening a Q10 bypass for any qualified route?
   (`TryCreate` refuses `QualifiedInstallation` without `IntegratedRecovery`.)
5. Is the documented observation→effect residual (root actor can change the
   installation after the read) acceptable for this slice, or does the accepted
   contract require stronger synchronization before native RELEASE
   qualification?
6. Do the 9 deterministic tests plus the named native-pending rows cover the
   required case list (§6 of the commission)? Which rows must the native
   qualification matrix add, and which existing suites (B5C3, Wp05, INT-01,
   `ReleaseArbiterTests`) already carry the stopping/loss/race cases unchanged?

## Explicit limits of this slice

No admission sink-coverage changes (`RecoveryConnection.Start` internal
initialization route stays open under Q04/S09); no native qualification claims
— the live switch-while-HELD, in-flight-ordering, controller-loss,
recovery/owner-loss, deadline-expiry, duplicate-race and legacy-regression
matrices require a trusted root-owned disposable host with delegated cgroups
and remain pending; the Mechanism B Q1 blocked-by-environment disposition is
unchanged; production activation remains unavailable (no public factory); no
push occurred; preservation intact (44 historical untracked paths, single
canonical checkout, verified pre-implementation bundle).
