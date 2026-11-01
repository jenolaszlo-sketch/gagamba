# GAGAMBA-RC2B-06-B6-WIRE-Q1 — Findings

**Review ID:** GAGAMBA-RC2B-06-B6-WIRE-Q1
**Date:** 2026-11-01

---

## Finding Classification

| Severity | Definition |
|----------|------------|
| **Blocker** | Prevents wire contract from being implementable or safe; must be resolved before acceptance |
| **Major** | Significant design/implementation gap; blocks implementation but not wire-contract review |
| **Minor** | Documentation inconsistency or clarification needed; does not block |
| **Observation** | Noted for implementer awareness; no action required for wire contract |

---

## Findings Summary

| ID | Severity | Category | Summary |
|----|----------|----------|---------|
| F01 | Blocker | Wire Namespace | Original design claimed wrong enum maxima (5/7 vs actual 8/35) — **RESOLVED** in wire delta by allocating 9/10/36 |
| F02 | Major | Framing | Original design described non-existent "4-byte big-endian length" framing — **RESOLVED** in wire delta |
| F03 | Major | Transport | Original design claimed bootstrap on stdin, echoes on stdout — **RESOLVED** in wire delta |
| F04 | Major | Ordering | Original design claimed Ready=1, StartAuthority=controller→owner — **RESOLVED** in wire delta |
| F05 | Major | Identity Provenance | Original design claimed owner identity from StartAuthority — **RESOLVED** in wire delta (withdrawn) |
| F06 | **Qualification** | Implementation | `IntegratedRecoverySpecification` missing `InstallationIdentity` field (required for RecoveryConfigV2) |
| F07 | **Qualification** | Implementation | `RecoveryConfiguration.Validate()` requires Version==1; v2 validation not implemented |
| F08 | **Qualification** | Implementation | B2 legacy owner (`B2OwnerLifeline`) never sends `WriterReady`; echo placement differs from delta description |
| F09 | Minor | Documentation | Wire delta §2.4 step 6 says "before WriterReady (2) (B2 path)" but B2 path doesn't use WriterReady |
| F10 | Observation | Implementation | `HasValidControlPayloadShape` needs explicit case for OwnerInstallationEcho (36) |
| F11 | Observation | Implementation | `DirectControl` and `ForbiddenRecovery` need type 36 added |
| F12 | Observation | Implementation | `RecoveryWire.Accept` needs explicit payload bound check for types 9/10 |
| F13 | Blocker | Authority | **VERIFIED ABSENT** — No RELEASE authority transfer in echo messages |
| F14 | Blocker | Authority | **VERIFIED ABSENT** — No reconnect/retransmission/re-adoption introduced |
| F15 | Blocker | Preservation | **VERIFIED PRESERVED** — All B1–B5 semantics, C1–C4, one-shot, independent stop |

---

## Detailed Findings

### F01: Original Design Enum Maxima Incorrect (RESOLVED)

**Severity:** Blocker (in original design) → **RESOLVED** in wire delta
**Source Reference:** Design §6.2 claimed "current max: OwnerProtocolMessageType = 7, RecoveryMessageType = 5"
**Actual Source:** `OwnerProtocol.cs:12–53` (max 35), `RecoveryProtocol.cs:8` (max 8)
**Effect:** Original design allocated IDs 6, 7, 8, 3 — all collisions
**Resolution:** Wire delta correctly allocates unused IDs: RecoveryMessage 9/10, OwnerProtocolMessageType 36
**Status:** ✅ **Resolved in wire delta** — no longer a blocker

---

### F02: Non-Existent Framing Described (RESOLVED)

**Severity:** Major (in original design) → **RESOLVED** in wire delta
**Source Reference:** Design §6.2: "All post-bootstrap messages use the existing length-prefixed frame (4-byte big-endian length + payload)"
**Actual Source:** No big-endian writes in `src/`; actual framing is 48-byte LE OwnerProtocol header or 64-byte LE GID1 header
**Resolution:** Wire delta §1.2 correctly describes 48-byte LE header over SOCK_SEQPACKET
**Status:** ✅ **Resolved in wire delta**

---

### F03: Incorrect Transport Description (RESOLVED)

**Severity:** Major (in original design) → **RESOLVED** in wire delta
**Source Reference:** Design: "bootstrap on Recovery stdin (fd 0, length-prefixed JSON)" and "echoes travel on stdout/stdin fd 0/1"
**Actual Source:** 
- Bootstrap: `GAGAMBA_B2_CREDENTIAL_FD` (SOCK_SEQPACKET) — `RecoveryConnection.cs:69`, `RecoveryHost.cs:36`
- Control/Echo: `GAGAMBA_B2_CONTROL_FD` (SOCK_SEQPACKET) — `RecoveryConnection.cs:76`, `RecoveryHost.cs:57`
- Owner channel: Inherited socketpair (B2) or DirectSession SOCK_STREAM (Integrated)
**Resolution:** Wire delta §1.2 correctly identifies actual transports
**Status:** ✅ **Resolved in wire delta**

---

### F04: Incorrect Ordering Model (RESOLVED)

**Severity:** Major (in original design) → **RESOLVED** in wire delta
**Source Reference:** Design: "recovery echo before Ready; owner echo before StartAuthority; controller spawns owner with OwnerStartAuthority"
**Actual Source:**
- Integrated recovery sends `Rendezvous` (8), not `Ready` (2) — `IntegratedRecoveryHost.cs:61`
- `StartAuthority` (3) is **recovery→owner** (descriptors), not controller→owner — `M1RecoveryScope.cs:411`, `OwnerPreparationTransaction.cs:103`
- Controller never sends `OwnerStartAuthority` — that message doesn't exist
**Resolution:** Wire delta §1.3 and §2.4 correct all ordering
**Status:** ✅ **Resolved in wire delta**

---

### F05: Owner Identity Provenance Error (RESOLVED)

**Severity:** Major (in original design) → **RESOLVED** in wire delta
**Source Reference:** Design §6.2 claimed owner gets expected identity via controller→owner `StartAuthority`
**Actual Source:** `StartAuthority` is recovery→owner; owner gets identity from `DirectBootstrap` (32) via recovery channel — `OwnerDirectConnection.cs:33–41`
**Resolution:** Wire delta §2.3 explicitly withdraws the incorrect claim
**Status:** ✅ **Resolved in wire delta**

---

### F06: Missing InstallationIdentity Field in IntegratedRecoverySpecification

**Severity:** Qualification (Implementation Obligation)
**Source Reference:** Wire delta §2.1: `RecoveryConfigV2` carries `Integrated.InstallationIdentity`
**Actual Source:** `IntegratedRecoveryHost.cs:12–13` — `IntegratedRecoverySpecification` has no `InstallationIdentity` field
**Effect:** Controller cannot populate expected identity in bootstrap v2 for recovery
**Required Correction:** Add `InstallationIdentity` field to `IntegratedRecoverySpecification` record
```csharp
internal sealed record IntegratedRecoverySpecification(
    B3FixtureProfile Profile,
    Guid Issuer,
    Guid StoreGeneration,
    long AcceptedAtNs,
    long ExpiresAtNs,
    DirectProcessIdentity Controller,
    ExperimentalRuntimePin Runtime,
    InstallationIdentity InstallationIdentity  // NEW
)
```
where `InstallationIdentity` contains artifact-set digest + role closure digests.
**Blocks:** B6 implementation (IMPL-01), not wire-contract review
**Status:** ⚠️ **Implementation Obligation**

---

### F07: RecoveryConfiguration Version 2 Validation Missing

**Severity:** Qualification (Implementation Obligation)
**Source Reference:** Wire delta §2.1: "Bootstrap v1 vs v2. Version = 1 has no identity and remains refused for the qualified profile; Version = 2 is the qualified record"
**Actual Source:** `RecoveryProtocol.cs:118` — `Validate()` requires `Version != 1`
**Effect:** Bootstrap v2 will be rejected by current validation
**Required Correction:** Extend `Validate()` to accept `Version == 2` when `Integrated != null` and `Integrated.InstallationIdentity` is present
```csharp
internal void Validate()
{
    if (Version != 1 && Version != 2)  // Allow v2
        throw new InvalidDataException("Invalid B2 bootstrap configuration");
    if (Version == 2 && (Integrated == null || Integrated.InstallationIdentity == null))
        throw new InvalidDataException("B6 qualified bootstrap requires InstallationIdentity");
    // ... existing checks ...
}
```
**Blocks:** B6 implementation (IMPL-01), not wire-contract review
**Status:** ⚠️ **Implementation Obligation**

---

### F08: B2 Legacy Owner Echo Placement Mismatch

**Severity:** Qualification (Implementation Obligation)
**Source Reference:** Wire delta §2.4 step 6: "before WriterReady (2) (B2 path)"
**Actual Source:** `RecoveryHost.cs:182–224` (`B2OwnerLifeline.Run`) — B2 owner handles: `B2StartLifeline` (23) → `B2LifelineReady` (24) → waits for `B2SafetyStop` (25) → sends `B2StopObserved` (26). **Never sends `WriterReady` (2).**
**Effect:** Wire delta's B2 ordering description doesn't match actual B2 message flow
**Required Correction:** In IMPL-01, extend `B2OwnerLifeline.Run` to send `OwnerInstallationEcho` (36) after `B2LifelineReady` (24) and before stop-wait loop
```csharp
if (!channel.TrySend(OwnerProtocolMessageType.B2LifelineReady, [], out _)) return 3;
// NEW: Send installation identity echo
var echoPayload = BuildOwnerInstallationEcho(); // OwnerClosureSha256 + protocols + role + qualified
if (!channel.TrySend(OwnerProtocolMessageType.OwnerInstallationEcho, echoPayload, out _)) return 3;
// Then enter stop-wait loop
```
**Note:** This is an implementation obligation. The wire contract correctly specifies "first owner-channel message" — the B2 legacy path just needs to emit it at the right point in its distinct flow.
**Blocks:** B6 implementation (IMPL-01), not wire-contract review
**Status:** ⚠️ **Implementation Obligation**

---

### F09: Documentation Inconsistency — WriterReady on B2 Path

**Severity:** Minor
**Source Reference:** Wire delta §2.4 step 6: "before WriterReady (2) (B2 path)"
**Actual Source:** B2 path uses `B2LifelineReady` (24), not `WriterReady` (2)
**Effect:** Minor confusion in documentation; implementer must map to correct message type
**Resolution:** Update wire delta to say "before `B2LifelineReady` (24) / stop-wait loop (B2 path)" or clarify that WriterReady is not on B2 path
**Status:** 📝 **Documentation clarification needed**

---

### F10: HasValidControlPayloadShape Missing Case for Type 36

**Severity:** Observation (Implementation Detail)
**Source Reference:** `OwnerProtocol.cs:81–111` — `HasValidControlPayloadShape` switch expression
**Required:** Add explicit case for `OwnerInstallationEcho` (36) with 512-byte bound
```csharp
OwnerProtocolMessageType.OwnerInstallationEcho => message.Payload.Length <= 512,
```
**Status:** 📝 **Implementation Detail**

---

### F11: DirectControl/ForbiddenRecovery Missing Type 36

**Severity:** Observation (Implementation Detail)
**Source Reference:** `OwnerProtocol.cs:291–299` (DirectControl), `300–306` (ForbiddenRecovery)
**Required:** 
- Add 36 to `DirectControl` (controller side accepts it)
- Add 36 to `ForbiddenRecovery` (recovery side refuses it)
```csharp
internal static bool DirectControl(OwnerProtocolMessageType type) => type is
    // ... existing ...
    or OwnerProtocolMessageType.OwnerInstallationEcho;  // ADD

private static bool ForbiddenRecovery(OwnerProtocolMessageType type) => type is
    // ... existing ...
    or OwnerProtocolMessageType.OwnerInstallationEcho;  // ADD
```
**Status:** 📝 **Implementation Detail**

---

### F12: RecoveryWire.Accept Payload Bound for Types 9/10

**Severity:** Observation (Implementation Detail)
**Source Reference:** `RecoveryProtocol.cs:24–38` — `Accept` method payload validation
**Current:** `PayloadBound` returns 512 for types 9/10 (default case)
**Accept validation (line 35–37):**
```csharp
return type == RecoveryMessage.Associate ? payload.Length == 48
    : type is RecoveryMessage.Ready or RecoveryMessage.Snapshot or RecoveryMessage.Rendezvous ? payload.Length <= PayloadBound(type)
    : payload.Length == 0;
```
**Required:** Extend to allow non-zero payload for types 9/10:
```csharp
: type is RecoveryMessage.InstallationEchoRequest or RecoveryMessage.InstallationEchoResponse ? payload.Length <= PayloadBound(type)
: payload.Length == 0;
```
**Status:** 📝 **Implementation Detail**

---

### F13: No RELEASE Authority in Echo Messages (VERIFIED ABSENT)

**Severity:** Blocker (would be) → **VERIFIED ABSENT**
**Verification:**
- `RecoveryObligation.ReleaseAuthority => false` hardcoded (`RecoveryProtocol.cs:68`)
- `RecoveryMessage` enum has no `ReleaseRequest`/`ReleaseDecision` — those are OwnerProtocol types 27/28
- `RecoveryWire.Accept` only allows `RecoveryMessage` enum values (1–8, soon 9/10)
- `ForbiddenRecovery` blocks owner-channel messages (including ReleaseRequest 27) on recovery channel
- Echo payloads contain only identity digests, no capabilities/handles/bearers
**Conclusion:** Wire contract **does not create, restore, transfer, or imply RELEASE authority**
**Status:** ✅ **Verified — No Blocker**

---

### F14: No Reconnect/Retransmission/Re-Adoption (VERIFIED ABSENT)

**Severity:** Blocker (would be) → **VERIFIED ABSENT**
**Verification:**
- `OwnerProtocolSequencer` — per-direction monotonic sequence, no retries (`Create` increments, `TryAccept` requires exact next)
- `RecoveryWire` reuses same sequencer — same guarantees
- `DirectSession` — "A verified original-peer attempt consumes this association even when its ACK is lost. Only unauthenticated OS peers may be rejected without consuming that attempt." (`OwnerDirectConnection.cs:72–73`)
- `RecoveryAssociation.Authenticate` — one-time use (`Associated` flag prevents re-use)
- No message type for reconnect/retransmission in either protocol
- `Reconnect` (6) and `Refused` (7) are existing B2 messages for different semantics (duplicate association attempt → refused)
**Conclusion:** Wire contract **introduces no reconnect, retransmission, or re-adoption mechanism**
**Status:** ✅ **Verified — No Blocker**

---

### F15: All Accepted Behavior Preserved (VERIFIED PRESERVED)

**Severity:** Blocker (would be) → **VERIFIED PRESERVED**

| Behavior | Verification |
|----------|--------------|
| Original controller identity | `DirectAssociation` binds execution/generation/session/process IDs |
| Owner-only RELEASE | `RecoveryObligation.ReleaseAuthority => false`; only owner has capability/writer |
| One physical RELEASE attempt | `ReleaseArbiter` + `OwnerHeldWire` — single `ReleaseRequest`/`ReleaseDecision` |
| C1/C2/C4 behavior | `RecoveryObligation` stop mechanism unchanged; C3 deadline via `MonotonicClock` |
| Durable C3 deadline authority | `Reconciler` + `StopwatchDomainClock.Instance` sealed |
| Controller-loss recovery | `RecoveryHost`/`IntegratedRecoveryHost` retain stop/cleanup authority |
| Independent native stop | `RecoveryObligation.Request` + `M1RecoveryScope.SafetyStopRetained` |
| HELD state | `HeldReleasePhase` + `HeldExecution` |
| Protected capture | `ProtectedCapture` / `RestrictedCaptureRead` path |
| Journal uncertainty | 12 Unknown categories in `Reconciler` |
| B5 reconciliation | `Reconciler.Reconcile` with sealed clock |
| Existing refusal behavior | `Refused` (7), `Reconnect` (6) semantics unchanged |
| B1–B5 protocol semantics | All existing message IDs, envelopes, sequences unchanged |

**Conclusion:** Wire delta **preserves all accepted behavior**; no silent weakening
**Status:** ✅ **Verified — No Blocker**

---

## Findings by Category

### Wire Namespace & Framing (Gate A)
- F01: RESOLVED — Correct ID allocation
- F02: RESOLVED — Correct framing description
- F10, F11, F12: Implementation Details for IMPL-01

### Identity Provenance (Gate B)
- F05: RESOLVED — Correct provenance identified
- F06: Qualification — Missing `InstallationIdentity` field
- F07: Qualification — Missing Version 2 validation

### Handshake Ordering (Gates C & D)
- F03: RESOLVED — Correct transports
- F04: RESOLVED — Correct ordering model
- F08: Qualification — B2 legacy path mismatch
- F09: Minor — Documentation inconsistency

### Authority & Preservation (Gates B–E)
- F13: VERIFIED ABSENT — No RELEASE authority
- F14: VERIFIED ABSENT — No reconnect/re-adoption
- F15: VERIFIED PRESERVED — All B1–B5 behavior intact

---

## Verdict Impact

| Finding | Blocks Wire Delta Acceptance? | Blocks Implementation? |
|---------|------------------------------|------------------------|
| F01–F05 | No (resolved in delta) | No |
| F06 | No | **Yes (IMPL-01)** |
| F07 | No | **Yes (IMPL-01)** |
| F08 | No | **Yes (IMPL-01)** |
| F09 | No | No (doc only) |
| F10–F12 | No | **Yes (IMPL-01)** |
| F13–F15 | No (verified OK) | No |

**Overall:** Zero findings block **wire-delta acceptance**. Three findings (F06, F07, F08) block **implementation** and are correctly recorded as implementation obligations for IMPL-01. Three findings (F10–F12) are implementation details for IMPL-01.