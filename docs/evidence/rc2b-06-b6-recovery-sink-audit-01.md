# GAGAMBA-RC2B-06-B6-RECOVERY-SINK-AUDIT-01 — recovery admission-sink audit

Commission: Q10 owner acceptance (Part A) and internal recovery
initialization-path admission-sink audit (Part B). Executor: GLM Flash in
OpenCode, 2026-11-05. Reviewer: Terra High (separately commissioned if
remediation were required — none was). No production code was modified.

## 1. Disposition

**SINK COVERAGE SUPPORTED WITH QUALIFICATIONS.**

Every reachable production recovery-initialization path is bounded by
existing admission authority; the internal `Start` methods are lifecycle
primitives whose only direct callers are test-only; the recovery-actor hosts
are continuation of an already-gated bootstrap. No CLASS D admission gap was
demonstrated. Native positive controls (real root-owned S2 installation)
remain pending and are not claimed.

## 2. Identities

- Starting HEAD `daac1a187522e90d0df9cf006c5f0e2309ea0dbc` (tree
  `2b7042f77ff3db54a0a69ba7f34634ecfd0c61a0`) as expected.
- Q10 owner acceptance commit `74362f53f912ca89a5bdff6bf15bd4cc68afa4f7`
  (Part A; the only commit of this commission; docs-only).
- Preflight: clean tracked/index state; exactly one worktree; the 44
  historical untracked paths preserved throughout; no concurrent writer; Q10
  safety bundle and consolidation archive verified present.
- Audit probes ran from an untracked file and were removed; final tree
  byte-identical to `74362f5` (`git diff HEAD` empty).

## 3. Recovery call graph (source-bound, complete)

Sinks: `RecoveryConnection.Start` (`Bootstrap/RecoveryConnection.cs:31`) and
`QualifiedRecoveryConnection.Start` (`Bootstrap/QualifiedRecoveryConnection.cs:13`,
dispatched only from `RecoveryConnection.cs:33`).

### Controller side

| Caller of `Start` | Location | Visibility/ownership | Gates upstream |
|---|---|---|---|
| `IntegratedB3Client.Prepare` | `Bootstrap/IntegratedB3Client.cs:67` | internal, production | `profile.QualifiedInstallation` → `LoadedInstallation.Verify("provider")` + `VerifyFinalAdmission` + `AdmissionAuthority.IsFinalAdmissionEligible` (:54-62) **immediately before Start** |
| `B2Controller` fixture `--controller` | `tests/Fixtures/B2Controller/Program.cs:28` | test fixture executable (`InternalsVisibleTo("Gagamba.Execution.Linux.M1.B2Controller")`, `Bootstrap/Visibility.cs:2`) | none at fixture level (stdin config); S2 re-derived inside Start |
| `B3RestrictedLifecycleTests` | `:712, :750` | test assembly | v1 legacy configs |
| `InstallationWireTests` | `:165` | test assembly | adversarial: v2+DelegatedParent must throw before allocation |

### Upstream of the production caller

`IntegratedB3Client.Prepare` ← `B3Qualification.PrepareIntegrated`
(`Restricted/B3Qualification.cs:489`) ← `PrepareCore` (:354) ←
`B3PreparationOwner.PrepareAsync` (:328) ← `RestrictedExecutionOperation`
(prepare flow). The only creator of `B3PreparationOwner`/
`RestrictedExecutionOperation` is `B3Qualification.TryCreate` (:77), which
enforces final admission itself (:188-194: `LoadedInstallation.Verify` +
`VerifyFinalAdmission` + `IsFinalAdmissionEligible` →
`UnqualifiedInstallation`). **`TryCreate` has no production caller** —
public activation is `RestrictedExecutionRuntime.Create()`
(`src/Gagamba.Runtime/RestrictedExecutionRuntime.cs:9`) which returns
`UnavailableRestrictedExecutionProvider`, whose `CreateOperation` refuses
every profile (`InstallationUnavailable` for known ids, `UnsupportedProfile`
otherwise; `RestrictedProvider.cs:143-153`). Hence: **outer activation gate
default-disabled + two independent final-admission checks on the only
production route to `Start`.**

### Recovery-actor side (spawned processes)

`RecoveryHost.Main` (`Bootstrap/RecoveryHost.cs`) receives the bootstrap over
the credential fd, validates config (:41), verifies its own executable
identity against the config (:44-47), pins the controller via parent pidfd
(:50-52), authenticates the Associate frame (:59-62), and for v2 runs the
installation-identity echo (:64) before dispatching:

- v2 B2 → `QualifiedB2Recovery.Run` (:66) — stop-only qualified lifeline;
  owner spawned from the installed plan with identity echo barriers
  (`QualifiedB2Recovery.cs:72`).
- v2 integrated → `IntegratedRecoveryHost.Run` (:68) — controller identity
  pinned by pidfd live-reference (`IntegratedRecoveryHost.cs:29`); the
  recovery-side manifest-authorization callback **throws**
  ("Recovery cannot authorize a manifest", :77) — only the controller
  authorizes.
- v1 delegated → `B2RetainedDomain.Start` (:74) — bounded stop-only
  qualification: stops before AllocationRegistered/copy/Prepared/Mint
  (class doc :8-9; authorization callback throws, :24; test barrier
  after-domain-retained, :26).
- v1 lifeline → `TrySpawnOwner` (:81) — plain pinned-owner spawn for the
  legacy B2 wire.

### Topology gates at the sinks themselves

`RecoveryConfiguration.Validate` (`Bootstrap/RecoveryProtocol.cs:154-178`):
v1 cannot claim qualification (:162-163); v2 delegated-parent B2 refused
"qualified B2 topology refused" before any parent/journal/domain access
(:168-169); v2 integrated requires a validated `InstallationIdentity` (:174-176).
`QualifiedRecoveryConnection.Start` re-derives trust from the filesystem
(:18 `LoadedInstallation.Verify("provider")` — throws unless Qualified) and
refuses a supplied identity that differs from real S2 (:19-20) — a caller
cannot talk its way past the sink with a fabricated identity, though final
admission is enforced upstream, not inside `Start`.

## 4. Classification (per commission §10)

| Route | Class | Rationale |
|---|---|---|
| `IntegratedB3Client.Prepare` → `Start` (production) | **A — protected upstream** | `TryCreate` final admission + `Prepare` final admission + default-disabled public factory dominate the only production entry chain; verified by exhaustive caller enumeration (grep of all `Start` references; single production caller; single dispatch to `QualifiedRecoveryConnection.Start`). |
| `IntegratedRecoveryHost.Run`, `QualifiedB2Recovery.Run`, `B2RetainedDomain.Start/Stop`, v1 lifeline `TrySpawnOwner` | **B — internal lifecycle continuation** | Execute only inside an already-spawned recovery actor whose bootstrap was established by the gated controller `Start`; recovery cannot authorize manifests; stop/reconciliation duties (retained lease stop, reaping, journal reconciliation) must not be blocked by a fresh admission check — adding one here would violate the commission's continuation rule. |
| B2Controller fixture `--controller` direct `Start`; test assemblies | **C — test-only** | Test fixture executable + `InternalsVisibleTo` test grants; zero production references to `B2Controller` outside the visibility declaration (verified); the fixture's command surface is stop-only (observer/stop/inspect/replay). Evidence preserved in the audit package. |
| — | **D — none demonstrated** | No reachable production path creates new qualified restricted-execution authority without the required admission protection. |
| — | **E — none** | The caller/authority chains are fully established. |

Composition rule recorded for future work: any new production caller of
`RecoveryConnection.Start`/`QualifiedRecoveryConnection.Start` must route
through `B3Qualification.TryCreate` or `IntegratedB3Client.Prepare` (or add an
equivalent explicit final-admission gate); a direct call would reclassify the
route as CLASS D and require remediation.

## 5. Admission vs continuation

New admission (controller `Start` chain) — final-admission enforced at
`TryCreate` and re-enforced at `Prepare`, immediately upstream of the sink.
Continuation (recovery-actor hosts; retained-domain stop/cleanup;
reconciliation) — deliberately not re-gated: the accepted contracts preserve
stopping authority for already-admitted operations regardless of later
acceptance changes (Q10 decision §1; B5/B6 stop-authority contracts). No
blanket check was proposed or needed.

## 6. Independent probes (deterministic, 4/4)

Executed from an untracked test file (removed afterwards; source + raw log in
the audit package):

1. `QualifiedV2StartRefusesWithoutTrustedContext` — well-formed v2 B2 config
   into internal `Start` on a non-qualified host → typed refusal before any
   recovery/owner process exists.
2. `LegacyV1StartRefusesOutsideLinux` — v1 legacy `Start` refuses outside the
   supported unprivileged-Linux environment.
3. `TryCreateRefusesBeforeAnyAuthorityOnNonLinux` — `TryCreate` returns
   `UnqualifiedInstallation` with no operation and no owner instance.
4. `PublicFactoryCannotCreateAnyRestrictedOperation` — the public factory's
   provider refuses both the fixture profile id and unknown ids; no operation
   is ever created.

Existing committed coverage independently confirmed:
`InstallationWireTests.BootstrapSeparatesV1V2AndRefusesDelegatedQualifiedBeforeAllocation`
(v1-claim-qualification, delegated-qualified refusal incl. a direct
adversarial `Start` call, missing-identity refusal, decode attacks) and
`InvalidIdentitiesCannotClaimQualification` (six identity-field mutations).

Native-pending (not executed, not claimed): the real qualified-integrated
positive (S2 Qualified + final-admitted selection on a trusted root),
wrong-installation/wrong-process-identity negatives against a real recovery,
and already-admitted stop/recovery after acceptance changes.

## 7. Adversarial questions (commission §9) — answered

- Unqualified caller starting qualified recovery? Must pass `Validate` + the
  S2 re-derivation inside `QualifiedRecoveryConnection.Start` (:16-20); on a
  host without the qualified installation it refuses (probe 1 on Windows;
  native positive otherwise pending).
- Omit `IntegratedRecovery` yet reach qualified owner creation? No:
  `TryCreate` refuses qualified-without-integrated; v2-without-integrated is
  the stop-only B2 topology (never HELD/RELEASE).
- Legacy configuration supplying a qualified generation identity? Refused —
  v1 cannot claim qualification (Validate :162-163).
- Untrusted caller supplying an accepted-looking selection? The identity is a
  wire value; the sink re-derives provenance from `VerifyActive` and compares
  (:18-20). Final admission is upstream; a fabricated selection cannot
  substitute for the host's actual S2 state.
- Fixture entry reachable through production composition? No production
  reference to the fixture assembly exists (verified); its grant is an
  explicit test-only `InternalsVisibleTo`.
- Missing final acceptance permitting new restricted-operation authority? Not
  through any production path (both gates + disabled factory). A test-fixture
  direct `Start` can spawn a qualified *recovery process* without final
  admission — stop-only surface, no manifest authority (authorization
  callbacks throw), no RELEASE route — CLASS C.
- Existing stopped/stopping operation retaining recovery authority? Yes — the
  continuation classes preserve retained-lease stop, reaping and
  reconciliation independently of admission state.

## 8. Remediation

None required (no CLASS D). The O1-style hardening option recorded by the Q10
review (explicit fail-closed for `qualified && bound is null`) remains
optional and non-blocking. Recommended next commission: the native
trusted-root qualification milestone (Mechanism B Q1 disposition rows + Q10
live matrix rows + the sink-audit positive/negative controls listed above).

## 9. Preservation

No branches, worktrees, resets, cleans, prunes or pushes. One commit
(`74362f5`, docs-only, Part A). Probe artifacts externalized to
`C:\Users\Laszlos\source\repos\Solo\gagamba-recovery-sink-audit-01`
(SHA-256 manifest inside).
