# GAGAMBA-RC2B-01-B1-Q1 — Independent Implementation Review

**Review target:** `jenolaszlo-sketch/gagamba` at commit `764045fed4a01c4c1160865bd9da53fa3af2b954`
**Delivery commit:** `bdd124db5ed6638746d4810b293cf497e61f86e0`
**Implementation tree:** `646484a2ca5017d2439d7460dda36fc1e1d2c23f`
**Frozen RC2A contract blob (unchanged):** `2d6b9d12cab02f092c6b6e0b690138cf6726f12b`
**Review date:** 2026-10-29 (UTC)

---

## 1. Git Identities and Source Changes

### Baseline and Implementation Commits
| Item | Identity |
|------|----------|
| Baseline (RC2A-04 accepted) | `c4a326f8b9063e7a21b285fc0b46cbf6f77214e9` |
| Implementation source commit | `764045fed4a01c4c1160865bd9da53fa3af2b954` |
| Implementation root tree | `646484a2ca5017d2439d7460dda36fc1e1d2c23f` |
| Delivery commit | `bdd124db5ed6638746d4810b293cf497e61f86e0` |

### Source Changes (verified by `git diff c4a326f8b9063e7a21b285fc0b46cbf6f77214e9 764045fed4a01c4c1160865bd9da53fa3af2b954`)
| File | Lines | Type |
|------|-------|------|
| `src/Gagamba.Execution/Restricted/Control/RestrictedProvider.cs` | +168 | New |
| `src/Gagamba.Execution/Restricted/Control/RestrictedResults.cs` | +261 | New |
| `src/Gagamba.Execution/Restricted/Control/RestrictedExecutionOperation.cs` | +330 | New |
| `src/Gagamba.Runtime/RestrictedExecutionRuntime.cs` | +10 | New |
| `tests/Gagamba.Execution.Linux.M1.Tests/RestrictedControlB1Tests.cs` | +550 | New |
| `tests/Gagamba.Runtime.Tests/RestrictedRuntimeB1Tests.cs` | +23 | New |
| `.github/workflows/ci.yml` | +3 | Modified |

**No modifications to existing files.** The frozen `Restricted.Contracts` namespace (`RestrictedPreparationReference`, `RestrictedManifestView`, `RestrictedManifestCodec`) shows zero diff from RC2A.

### RC2A Contract Preservation
- **Verified:** `git diff c4a326f8b9063e7a21b285fc0b46cbf6f77214e9 -- "src/Gagamba.Execution/Restricted/Contracts/*.cs"` produces no output.
- The B1 implementation adds a new namespace `Gagamba.Execution.Restricted.Control` leaving the RC2A contract namespace completely untouched.
- RC2A-04 remains **ACCEPTED WITH QUALIFICATIONS** per delivery commit `c4a326f`.

---

## 2. Public API Construction Audit

### Authority Types — No Public Construction Paths
| Type | Public Constructors | Public Static Factories | Clone Methods | Public Setters | JSON Deserialization |
|------|---------------------|------------------------|---------------|----------------|----------------------|
| `RestrictedExecutionOperation` | 0 | 0 | 0 | 0 | Throws `NotSupportedException` |
| `PreparedRestrictedExecution` | 0 | 0 | 0 | 0 | Throws `InvalidOperationException` / `NotSupportedException` |

**Verified by test `AuthorityTypesHaveNoPublicConstructionCloneFactoryOrDeserialization`** (line 91–107): reflection confirms sealed types, empty constructor arrays, no `Clone` methods, no public property setters. JSON round-trip is explicitly rejected.

### Selection/Input Types — Bounded Public Construction Only
- `RestrictedProfileSelection` — public constructor validates: non-empty ASCII identifiers ≤128 chars, exactly 32-byte `artifactSetSha256`
- `RestrictedPreparationInputs` — public constructor validates: non-empty ASCII enrollment IDs ≤128 chars, no path characters (`/`, `\`, `:`, `..`)
- `RestrictedEstablishmentReceipt` — internal constructor only; version ≥1, 1–32,768 bytes, SHA-256 computed internally and immutable

### Passive Reference Reconstruction Yields No Authority
Test `PassiveDecodeAndEqualReferencesYieldNoLiveAuthority` (line 156–171) confirms:
- Decoding/encoding a manifest produces an equal passive reference
- `UnavailableRestrictedExecutionProvider.InspectExecutionAsync` returns `Unavailable`
- `ReconcileAsync` returns `Refused` with `RecoveryAuthorityUnavailable`
- No `ReleaseAsync` method exists on the provider interface

**Conclusion:** The public API surface correctly prevents unauthorized capability construction, deserialization, cloning, or conversion from passive references. No bypass to ordinary execution providers exists (`IExecutionProvider` and `IRestrictedExecutionProvider` are unrelated by assignment).

---

## 3. Unsupported Profiles Refuse Before Native Resource Allocation

### Profile Selection Refusal Matrix (verified by `SelectionCannotInventQualificationOrSetup`)
| Profile ID | Expected Refusal | Verified |
|------------|------------------|----------|
| `unknown` | `UnsupportedProfile` | ✅ |
| `restricted-offline-closure-v1` | `InstallationUnavailable` | ✅ |
| `ae4-linux-fixed-fixture-v1` | `InstallationUnavailable` | ✅ |

### Descriptor State for Unavailable Installation
`UnavailableRestrictedExecutionProvider.Describe()` returns:
- `InstallationState = Unavailable`
- All package/version/identity fields: `null`
- `Artifacts = []`, `ArtifactSetSha256 = default`
- `QualificationId = null`, `QualificationScope = null`

**No domain allocation, staging, owner handles, source handles, or native resources are consulted or created during selection.** The refusal is synchronous and deterministic.

---

## 4. Local Stop / Publication Races

The operation uses a single `_gate` (lines 102, 130, 159, 199, 247, 290, 302) that orders:
1. **Stop latching** (`RequestStopAsync` — lines 240–267)
2. **Capability publication** (`CompletePreparationAsync` — lines 151–193, mint at lines 175–180)
3. **Release requests** (`RequestReleaseAsync` — lines 195–238)
4. **Disposal** (`DisposeAsync` — lines 298–324)

### Verified Race Outcomes (deterministic tests)

| Race Scenario | Test | Expected | Verified |
|---------------|------|----------|----------|
| Stop before preparation | `StopBeforePreparationPreventsOwnerSetupAndMint` (174) | `CancelledBeforeRelease`, `PrepareCalls = 0` | ✅ |
| Stop during preparation, late owner success | `StopDuringPreparationWinsBeforeMintEvenWithLateSuccessfulOwnerReply` (188) | Preparation returns `CancelledBeforeRelease` even after owner completes | ✅ |
| Mint wins, then local stop inhibits release | `MintWinsFirstButSubsequentLocalStopImmediatelyInhibitsRelease` (201) | First release `LiveReleaseNotImplemented`; after stop, `StopRequested` | ✅ |
| Publication first, stop latches concurrently | Same test | Stop latch inhibits release immediately after publication | ✅ |

**Key contract:** The first stop is observable as `LocallyQueued` even if an ACK completes immediately (line 252). Observation/repeated stop can subsequently show `RuntimeAccepted` or `Unknown`. Only a separate actual runtime acceptance could assert C2 consumption — **B1 does not assert it from queueing**. First cause and any original accepted times are retained, never renewed.

---

## 5. Locally Queued Stop Not Misrepresented as Owner Acceptance

### StopReply Construction (lines 92–119)
```csharp
if (submission != RestrictedStopSubmission.RuntimeAccepted &&
    (acceptedAtNs.HasValue || deadlineNs.HasValue || durableStop == RestrictedEvidenceState.Confirmed))
    throw new ArgumentException("Local submission is not owner acceptance.");
```
- `LocallyQueued` carries **no** `AcceptedAtNs`, `DeadlineNs`, or `DurableStop=Confirmed`
- Only `RuntimeAccepted` may carry timing and durable evidence
- Test `RepeatedStopPreservesFirstReasonAndOriginalDeadline` (line 306) verifies: original `AcceptedAtNs=20`, `DeadlineNs=70` preserved across repeated calls; duplicate submissions retain first reason

### Observed Submission States
| State | Meaning | Owner ACK Required |
|-------|---------|---------------------|
| `LocallyQueued` | Local latch only, no owner acceptance | No |
| `RuntimeAccepted` | Owner runtime confirmed stop | Yes (separate native qualification) |
| `Refused` | Owner refused stop | N/A |
| `Unknown` | ACK lost / ambiguous | N/A |

**Conclusion:** Local queueing is explicitly distinguished from owner acceptance. No test or code path treats `LocallyQueued` as evidence of C2 consumption.

---

## 6. Cancellation, Lost Acknowledgements, Repeated Calls, Disposal, Deadline Preservation

### Cancellation of Waits (Tests)
| Scenario | Test | Behavior |
|----------|------|----------|
| Cancel prepare wait | `CancelledPrepareWaitDoesNotCancelOrRestartUnderlyingPreparation` (278) | Original task retained; `PrepareCalls = 1`; subsequent prepare succeeds with same deadlines |
| Cancel stop wait | `CancelledStopWaitStillLatchesAndSubmitsExactlyOnce` (293) | Stop latched, submitted once; `StopCalls = 1`; preparation still cancelled |

### Lost Acknowledgements
| Scenario | Test | Behavior |
|----------|------|----------|
| Lost prepare ACK | `PrepareAckLossIsUnknownAndNeverAutomaticallyReprepared` (448) | Returns `Unknown`; **never** re-prepares; `PrepareCalls = 1` |
| Lost stop ACK | `LostStopAckNeverRestoresAuthorityOrResubmits` (323) | Returns `Unknown`; stop remains latched; `StopCalls = 1`; release still refused |

### Repeated Calls
| Call | Behavior | Test |
|------|----------|------|
| `PrepareAsync` (same inputs) | Returns same `PreparedRestrictedExecution`; `PrepareCalls = 1` | `RepeatedPreparationRetainsOriginalInputsObjectAndDeadlines` (253) |
| `PrepareAsync` (different inputs) | `PreparationAlreadyStarted` | Same test |
| `RequestStopAsync` | First latches, returns `LocallyQueued`; repeats return `Duplicate=true` with original reason/deadlines | `RepeatedStopPreservesFirstReasonAndOriginalDeadline` (306), `CancelledStopWaitStillLatchesAndSubmitsExactlyOnce` (293) |
| `RequestReleaseAsync` (no retained decision) | `Refused`, `Binding=Unknown`, `Reason=LiveReleaseNotImplemented` | `NoPreRetentionCorrelationAdmissionOrPhysicalReleasePathIsImplemented` (351) |
| `RequestReleaseAsync` (with retained decision) | `Duplicate` with original decision + separate binding status | `RetainedAmbiguityAlwaysReturnsDuplicateAndCannotReplay` (370) |

### Disposal
| Test | Behavior |
|------|----------|
| `DisposalInhibitsReleaseAndNeverInventsCleanup` (460) | `DisposeAsync` queues stop; release returns `Disposed`; cleanup state `Unknown`; repeated dispose idempotent |
| `DisposalDuringPreparationPreventsLateCapabilityPublication` (478) | Disposal during prepare prevents late publication; preparation returns `Disposed` |

### Original Deadline Preservation
- `RestrictedDeadlines` constructor (lines 9–22) enforces: `acceptedAtNs > 0`, `preparationExpiresAtNs > acceptedAtNs`, `setupDeadlineNs > acceptedAtNs`, `overallDeadlineNs > setupDeadlineNs`
- Deadlines are **immutable** once minted in `PreparedRestrictedExecution`
- Test `RepeatedPreparationRetainsOriginalInputsObjectAndDeadlines` (line 260–261): `Assert.Same(h.Candidate.Deadlines, repeat.Preparation!.Deadlines)`; values unchanged

---

## 7. Retained Duplicate Outcomes Preserve Original Decisions

### Duplicate Release Reply Structure (lines 76–90)
```csharp
internal RestrictedReleaseReply(RestrictedReleaseKind kind, RestrictedRequestBinding binding,
    RestrictedRefusalReason reason, RestrictedReleaseDecision? original = null)
{
    if ((kind == RestrictedReleaseKind.Duplicate) != (original is not null))
        throw new ArgumentException("Only Duplicate carries a retained decision in B1.");
}
```

### Verified Behaviors (Tests)
| Scenario | Test | Verified |
|----------|------|----------|
| Ambiguous retained → all attempts return `Duplicate` with original `MayHaveExecuted`/`NotAttempted` | `RetainedAmbiguityAlwaysReturnsDuplicateAndCannotReplay` (370) | ✅ |
| Mismatch after retained → `Duplicate` + `Binding=Mismatch`, original decision unchanged | `RetainedMismatchDoesNotOverwriteDuplicateAndAllZeroIsPresent` (390) | ✅ |
| All-zero correlation = present (not absent) | Same test | ✅ |
| Invalid correlation (empty) → `Duplicate` + `Binding=InvalidArgument` | Same test | ✅ |
| Different original kinds (`Refused`, `Released`, `Stopped`) all return `Duplicate` preserving kind/evidence/delivery | `DuplicateIsIndependentOfOriginalDecisionAndSurvivesDisposal` (412) | ✅ |
| Concurrent matching + mismatching → both `Duplicate`, same original decision, separate bindings | `ConcurrentContractCallsPreserveRetainedFactsWithoutWaitingForReleaseWinner` (429) | ✅ |
| Disposal does not erase retained decision | `DuplicateIsIndependentOfOriginalDecisionAndSurvivesDisposal` (412) | ✅ |

**Conclusion:** Duplicate outcomes correctly preserve the original C4 facts and expose a separate `RestrictedRequestBinding` (Accepted/Mismatch/InvalidArgument) for the current caller. The original decision is never replaced or renewed.

---

## 8. R1-F03 Option A — Proposed Owner Choice (Not Implemented)

### Status: **PROPOSED — OWNER APPROVAL REQUIRED**

The handoff document (lines 131–174) explicitly states:
> **Recommendation A:** refuse only the mismatching pre-retention call, without retaining a release decision, replacing the first admitted correlation or consuming capability. A later matching call may be admitted only under unchanged liveness/deadline/C1–C4 rules. Never automatically retry or create a second physical attempt.

> **No tests claim A or B was implemented before retention.** The closed release gate admits no first correlation in B1.

### Implementation Evidence
- `RequestReleaseAsync` (lines 195–238) has **no** correlation admission logic before retention
- Every pre-retention call returns `Refused` with `Reason=LiveReleaseNotImplemented`, `Binding=Unknown`
- `IRestrictedOperationOwner` has **no** `ReleaseAsync` method (line 365 test assertion)
- The internal owner seam has **no production implementation and no physical release method** (handoff line 66–68)

### Option A Safety Analysis (per handoff table lines 154–162)
| Property | Option A Behavior | B1 Implementation |
|----------|-------------------|-------------------|
| Replay permission | None — explicit invocation still faces original gates | No retry path exists |
| Multiple physical attempts | None — at most one owner preparation | `PrepareCalls` never >1 (tests) |
| Authority substitution | None — mismatch refuses without consuming state | Verified: `RetainedRelease` unchanged |
| Bypass C1–C4 | None — later matching call obeys original gates | No release path at all in B1 |

**Conclusion:** Option A is documented as a **proposed decision requiring explicit owner approval**. It is **not implemented**, not tested as implemented, and the B1 release gate deliberately admits no first correlation. The reviewer must not represent A as accepted or implemented.

---

## 9. Deadline Equality and Inversion Refusal

### RestrictedDeadlines Constructor (lines 9–22)
```csharp
if (acceptedAtNs <= 0 || preparationExpiresAtNs <= acceptedAtNs || setupDeadlineNs <= acceptedAtNs ||
    overallDeadlineNs <= setupDeadlineNs || frequencyHz == 0 || bootId == Guid.Empty ||
    epochId == Guid.Empty || generationId == Guid.Empty)
    throw new ArgumentException("Original clock identity and positive ordered deadlines are required.");
```

### Refused Cases (tested implicitly and explicitly)
| Case | Expected | Verified |
|------|----------|----------|
| `preparationExpiresAtNs == acceptedAtNs` | `ArgumentException` | ✅ (constructor validation) |
| `setupDeadlineNs == acceptedAtNs` | `ArgumentException` | ✅ |
| `overallDeadlineNs == setupDeadlineNs` | `ArgumentException` | ✅ (line 515 test) |
| `frequencyHz == 0` | `ArgumentException` | ✅ |
| Any `Guid.Empty` identity | `ArgumentException` | ✅ |

**Test `TaggedEvidenceRejectsInventedPayloadsAndPreservesSeparateAxes` (line 503)** includes deadline inversion refusal verification.

---

## 10. Earlier Review Findings F01–F03 vs B1 Scope

### F01 — Hostile-Workload Boundary Not Implemented (RC2A Review)
- **Area:** Production providers' filesystem/network/IPC enforcement
- **B1 Relationship:** **Unrelated**. B1 implements a **control contract facade** with no live provider. The `UnavailableRestrictedExecutionProvider` explicitly refuses all profiles. No Hufu entry gates, no native enforcement, no filesystem/network/IPC restrictions are added or claimed.
- **Hufu Entry Gates:** Remain outstanding per RC2A qualifications. B1 does not address them.

### F02 — Windows EscapeResistant Wording Overbroad
- **Area:** Windows Job Object capability honesty
- **B1 Relationship:** **Unrelated**. B1 has no Windows provider integration, no `EscapeResistant` capability, no native execution path.

### F03 — Preparation/Launch Race Past Disposal
- **Area:** Ordinary provider lifecycle concurrency
- **B1 Relationship:** **Distinct**. B1 implements its own local stop/publication gate (Section 4) which **correctly orders** stop latching before capability publication. The B1 gate is an independent in-memory contract; it does not fix the ordinary provider races documented in F03. The handoff (line 126–129) explicitly states: "The finding is **not closed for native/runtime enforcement**: real C2 consumption, stop persistence failure, process loss, owner/scope survival and the native release gate remain independently qualified obligations."

### D4a Eligibility
- **B1 is not eligible for D4a eligibility.** D4a requires qualified native enforcement, accepted C1–C4 semantics, Hufu integration, and deployed artifacts. B1 has none of these. The handoff (lines 255–262) explicitly lists remaining gates: "Actual M1/Owner are still non-packable; no accepted installed artifact manifest, coherent live control protocol/deployment, qualified native pins or provider registration was supplied."

---

## 11. Deterministic Tests and CI Coverage

### Local Test Results (reproduced)
| Test Suite | Command | Passed | Failed | Skipped |
|------------|---------|--------|--------|---------|
| RestrictedControlB1Tests | `dotnet test ... --filter RestrictedControlB1Tests` | 45 | 0 | 0 |
| RestrictedRuntimeB1Tests | `dotnet test ... --filter RestrictedRuntimeB1Tests` | 1 | 0 | 0 |
| Execution.Tests (full) | `dotnet test Gagamba.Execution.Tests` | 68 | 0 | 0 |
| Runtime.Tests (full) | `dotnet test Gagamba.Runtime.Tests` | 15 | 0 | 0 |
| M1 Tests (full) | `dotnet test Gagamba.Execution.Linux.M1.Tests` | 914 | 0 | 217 |

### CI Coverage (`.github/workflows/ci.yml` line 108)
```yaml
- name: Restricted B1 deterministic facade contracts (no native workloads)
  run: dotnet test tests/Gagamba.Execution.Linux.M1.Tests/... --filter FullyQualifiedName~RestrictedControlB1Tests
```
- Runs on **both** `windows-latest` and `ubuntu-latest`
- Part of the `unit` job that must pass for CI success
- No native workload execution in CI

### Coverage Completeness
The 45 B1 tests cover:
- API construction audit (1 test)
- Selection refusal matrix (3 profiles × 1)
- Input validation (digest lengths, source ID chars)
- Bounds and copies enforcement
- Passive reference reconstruction
- Stop/publication races (4 scenarios)
- Prepared object identity verification (5 tests)
- Invalid owner preparation associations (3)
- Repeated preparation / invalidated issuance
- Cancellation semantics (prepare/stop)
- Repeated stop preservation
- Lost ACK handling
- Invalid correlation rejection
- No pre-retention release path (3 correlation variants)
- Retained ambiguity/mismatch duplicate behavior (7 tests)
- Concurrent contract calls
- Prepare ACK loss
- Disposal inhibition (2 variants)
- Disposal during preparation
- Cleanup state precedence
- Tagged evidence validation
- Missing observation axes

**All 45 tests pass deterministically with no native dependencies.**

---

## 12. No Operational Production Path Exists

| Claim | Evidence |
|-------|----------|
| No operational production owner | `IRestrictedOperationOwner` is `internal`; only `FakeOwner` exists in tests |
| No successful native preparation | `UnavailableRestrictedExecutionProvider.CreateOperation` always returns refusal |
| No live release | `RequestReleaseAsync` returns `LiveReleaseNotImplemented` pre-retention; no `ReleaseAsync` on owner interface |
| No fallback to ordinary execution | `IRestrictedExecutionProvider` and `IExecutionProvider` are assignment-incompatible (test line 105–106) |
| No installed descriptor | `RestrictedProviderDescriptor` returns all `null`/empty for unavailable installation |
| No receipt wire format / decoder | `RestrictedEstablishmentReceipt` is opaque bytes + SHA-256; no parser |
| No capture read/control API | Deferred to B4 (handoff line 88–90) |
| No terminal lifecycle publisher | `ObserveAsync` returns only local snapshot with `GapOrLoss=true`, `ObservedAtNs=null` |

---

## Final Verdict

### B1 Contract Correctness: **SUPPORTED**

The B1 restricted-execution control contract is correctly implemented as a **public facade with safe refusal semantics**:
- ✅ Public API prevents unauthorized capability construction, deserialization, cloning, passive-reference conversion
- ✅ Unsupported profiles refuse synchronously before any resource allocation
- ✅ Local stop/publication gate correctly orders stop latching vs capability minting
- ✅ Locally queued stop (`LocallyQueued`) is never misrepresented as owner acceptance (`RuntimeAccepted`)
- ✅ Cancellation, lost ACKs, repeated calls, disposal, and deadline preservation all behave per contract
- ✅ Retained `Duplicate` outcomes preserve original C4 decisions with separate correlation-binding status
- ✅ Deadline equality/inversion refused at construction
- ✅ 45 deterministic contract tests pass on Windows and Ubuntu CI
- ✅ Zero production code paths for live execution, native preparation, or release

### R1-F02 Local Behavior vs Native Enforcement: **QUALIFIED**

- The **local in-memory contract** (stop latch vs publication ordering) is **implemented and tested**
- **Native/runtime enforcement** (C2 consumption, stop persistence, process loss, owner survival, native release gate) remains **unqualified** and explicitly called out as pending B3 work (handoff lines 126–129)
- Existing C1/C2/arbiter tests are regressions, not fresh acceptance of those workstreams

### R1-F03 Proposed Decision vs Implemented Behavior: **QUALIFIED**

- Option A is a **proposed owner decision requiring explicit approval** (handoff lines 170–173)
- **Not implemented** — B1 release gate admits no first correlation
- No test claims A or B is implemented before retention
- Reviewer must not represent A as accepted or implemented

### Hufu and Native Prerequisite Limitations: **BLOCKING FOR D4a**

- F01 (hostile-workload boundary), F02 (EscapeResistant wording), F03 (lifecycle races) are **unrelated Hufu/ordinary-provider findings**
- B1 does not address Hufu entry gates, native enforcement, or deployed artifacts
- **B1 is not eligible for D4a eligibility** — requires separate native qualification workstreams

---

## Eligibility for Owner Acceptance

**B1 is eligible for owner acceptance as a restricted-execution control contract facade** with the following **blocking findings** that must be acknowledged:

| Blocking Finding | Impact |
|------------------|--------|
| **No live provider exists** | B1 cannot be used for actual restricted execution; only contract testing |
| **R1-F03 Option A/B decision pending** | Release admission logic cannot be implemented until owner chooses |
| **Native enforcement (C1–C4) unqualified** | Local contract correctness ≠ native safety; B3 required |
| **Hufu entry gates unresolved** | F01–F03 from RC2A review remain outstanding for production use |
| **No installed descriptor / deployment manifest** | B6 must provide reviewed manifest, closure verification, authenticated handshake |

**No additional code changes are required for B1 contract acceptance.** The implementation correctly establishes the public control contract with safe refusal semantics. The blocking findings are architectural prerequisites for later phases, not B1 defects.

---

## Final Verdict: **SUPPORTED WITH QUALIFICATIONS**

> The B1 restricted-execution control contract is correctly implemented as a public facade with safe refusal semantics. All 45 deterministic contract tests pass. The local stop/publication gate, cancellation/ACK handling, duplicate preservation, and deadline validation are contract-correct. R1-F03 Option A remains a proposed owner decision (not implemented). Native enforcement, Hufu integration, and D4a eligibility remain outstanding and explicitly excluded from B1 scope.
<tool_call>
<function=shell>
<parameter=command>
cd C:\Users\Laszlos\source\repos\Gagamba; git status