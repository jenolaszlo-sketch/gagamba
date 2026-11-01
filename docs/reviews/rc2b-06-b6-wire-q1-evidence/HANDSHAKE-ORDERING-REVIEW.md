# GAGAMBA-RC2B-06-B6-WIRE-Q1 — Handshake Ordering Review

**Review ID:** GAGAMBA-RC2B-06-B6-WIRE-Q1
**Date:** 2026-11-01
**Gate:** C & D — Recovery and Owner handshake ordering

---

## 1. Overview

This gate reviews the exact message sequences for:
- **Gate C:** Recovery handshake ordering (B2 legacy path + Integrated B3 path)
- **Gate D:** Owner handshake ordering (Integrated path + B2 legacy path)

The wire delta requires:
- Recovery sends `InstallationEchoRequest` (9) → Controller responds `InstallationEchoResponse` (10) **before** recovery publishes `Ready` (2, B2) or `Rendezvous` (8, Integrated)
- Owner sends `OwnerInstallationEcho` (36) as **first** owner-channel message, before `StagedObjects` (12, Integrated) or `WriterReady` (2, B2)

---

## 2. Gate C: Recovery Handshake Ordering

### 2.1 B2 Legacy Path (Non-Integrated)

**Source:** `RecoveryHost.RunCore()` (`RecoveryHost.cs:19–179`)

#### Current Sequence (without echo):

```
1. Controller: RecoveryConnection.Start(config)
   ├─ Spawns recovery process with GAGAMBA_B2_CONTROL_FD + GAGAMBA_B2_CREDENTIAL_FD
   ├─ Sends bootstrap JSON on credential FD
   ├─ Sends Associate (1) on control FD
   └─ Waits for Ready (2) or Refused (7)

2. Recovery: RunCore()
   ├─ Receives bootstrap on credential FD → Decode + Validate (Version==1)
   ├─ Verifies controller PID/UID + own assembly location
   ├─ Receives Associate on control FD → Authenticate
   ├─ Spawns owner (B2RetainedDomain.Start or socketpair + B2OwnerLifeline)
   ├─ Sends Ready (2) with Snapshot payload
   └─ Enters main loop (Stop/Inspect/Refused handling)
```

#### Required B6 Sequence (with echo):

```
1. Controller: RecoveryConnection.Start(config) [Version=2]
   ├─ Spawns recovery
   ├─ Sends bootstrap v2 JSON on credential FD (includes InstallationIdentity)
   ├─ Sends Associate (1) on control FD
   └─ Waits for echo/Ready

2. Recovery: RunCore()
   ├─ Receives bootstrap v2 on credential FD → Decode + Validate (Version==2)
   ├─ Verifies controller PID/UID + own assembly location
   ├─ Extracts InstallationIdentity from config.Integrated
   ├─ Receives Associate on control FD → Authenticate
   ├─ Sends InstallationEchoRequest (9) on control FD
   │   Payload: {ArtifactSetSha256, RecoveryClosureSha256, protocols, Role=Recovery, Qualified}
   ├─ Receives InstallationEchoResponse (10) on control FD
   │   Validates Success=1, MismatchReason=0
   │   On mismatch: Stop via C2/Policy A path (no Ready sent)
   ├─ Spawns owner (legacy B2 path)
   ├─ Sends Ready (2) with Snapshot payload
   └─ Enters main loop
```

#### Deadlock/Timeout Analysis:

- **No deadlock:** Echo exchange is synchronous request/response on existing control FD
- **Timeout:** Uses existing `Inspect` timeout (1000ms) or control channel receive timeout
- **No bypass of independent stop:** Recovery still enters main loop with `RecoveryObligation` stop mechanism; echo failure → `Stop("...")` → existing stop path
- **Controller stop handling:** If controller dies during echo, recovery's `PidFd.TryExited(controllerFd)` detects it → `Stop("ControllerExited")` (existing behavior)

### 2.2 Integrated B3 Path

**Source:** `RecoveryConnection.Start()` → `IntegratedRecoveryHost.Run()` (`RecoveryConnection.cs:78–100`, `IntegratedRecoveryHost.cs:16–210`)

#### Current Sequence (without echo):

```
1. Controller: RecoveryConnection.Start(config) [Integrated != null]
   ├─ Spawns recovery
   ├─ Sends bootstrap v1 JSON on credential FD
   ├─ Sends Associate (1) on control FD
   ├─ Receives Rendezvous (8) on control FD
   │   Payload: OwnerRendezvous (address + association), 1 descriptor (owner pidfd)
   ├─ Validates association (execution, generation, session, controller, recovery)
   ├─ Establishes DirectSession to owner address
   └─ Receives StagedObjects (12) as first owner-channel message

2. Recovery: RunCore() → IntegratedRecoveryHost.Run(config, ...)
   ├─ Validates integrated prerequisites
   ├─ Starts OwnerPreparationTransaction via scope.StartOwnerPreparation
   ├─ Owner launches, publishes DirectRendezvous (33) on recovery channel
   ├─ Recovery receives owner pidfd, publishes Rendezvous (8) on control FD
   └─ Main loop (Stop/Inspect/Snapshot)
```

#### Required B6 Sequence (with echo):

```
1. Controller: RecoveryConnection.Start(config) [Version=2, Integrated != null]
   ├─ Spawns recovery
   ├─ Sends bootstrap v2 JSON on credential FD (includes InstallationIdentity)
   ├─ Sends Associate (1) on control FD
   ├─ Receives InstallationEchoRequest (9) on control FD
   ├─ Sends InstallationEchoResponse (10) on control FD
   ├─ Receives Rendezvous (8) on control FD
   │   Payload: OwnerRendezvous, 1 descriptor (owner pidfd)
   ├─ Validates association
   ├─ Establishes DirectSession to owner address
   └─ Receives OwnerInstallationEcho (36) as FIRST owner-channel message
      (before StagedObjects (12))

2. Recovery: RunCore() → IntegratedRecoveryHost.Run(config, ...)
   ├─ Validates integrated prerequisites
   ├─ Receives Associate (1) → Authenticate
   ├─ Sends InstallationEchoRequest (9) on control FD
   ├─ Receives InstallationEchoResponse (10) on control FD
   │   On mismatch: Stop via existing C2 path
   ├─ Starts OwnerPreparationTransaction
   ├─ Owner launches, publishes DirectRendezvous (33)
   ├─ Recovery receives owner pidfd, publishes Rendezvous (8)
   └─ Main loop
```

#### Key Observations:

- **Echo before Rendezvous:** Recovery must complete echo exchange **before** publishing `Rendezvous` (8)
- **Controller handles new message:** `RecoveryConnection.Start()` currently does:
  ```csharp
  if (!FdPassing.TryReceiveRecord(control, 7000, out var record, out _)
      || !wire.Accept(record!.Payload, out var type, out var data) || type != RecoveryMessage.Rendezvous
      || record.DescriptorCount != 1) throw new IOException("Recovery lifetime role refused");
  ```
  Must be extended to handle `InstallationEchoRequest` (9) before `Rendezvous` (8)
- **Concurrent controller stop:** Recovery's main loop (`IntegratedRecoveryHost.Run` lines 94–114) monitors controller PIDFD; if controller dies during echo, `Stop("ControllerExited")` triggers
- **Cancellation:** `RecoveryConnection.Start()` is synchronous; no async cancellation during bootstrap
- **Recovery death:** If recovery dies before echo, controller's `FdPassing.TryReceiveRecord` times out (7000ms) → exception → cleanup

---

## 3. Gate D: Owner Handshake Ordering

### 3.1 Integrated Path (DirectSession)

**Source:** `OwnerDirectConnection.Accept()` (`OwnerDirectConnection.cs:15–90`) → `OwnerPreparationTransaction.Run()` (`OwnerPreparationTransaction.cs:27–439`) ↔ `IntegratedB3Client.Prepare()` (`IntegratedB3Client.cs:49–105`)

#### Current Sequence (without owner echo):

```
Owner (OwnerDirectConnection.Accept):
  1. Receives DirectBootstrap (32) from recovery on recovery channel
  2. Verifies DirectAssociation + SO_PEERPIDFD on descriptors
  3. Creates listener, sends DirectRendezvous (33) with address
  4. Accepts controller connection → DirectSession.Establish (HMAC handshake)
  5. Creates OwnerProtocolChannel over DirectSession

Owner (OwnerPreparationTransaction.Run):
  6. Receives Start (1) from controller on owner channel
  7. Sends WriterReady (2) to controller
  8. Receives StartAuthority (3) from recovery (on recovery channel, not owner channel!)
  9. ... preparation continues ...
  10. Sends StagedObjects (12) to controller on owner channel  ← FIRST owner→controller message
  11. Receives ManifestAuthorization (13)
  12. Sends PreparedDescription (14)
  13. Receives PreparedAcknowledgement (15)
  14. Receives HeldReady (20)

Controller (IntegratedB3Client.Prepare):
  1. Establishes DirectSession to owner
  2. Receives StagedObjects (12) as FIRST message from owner
  3. Sends ManifestAuthorization (13)
  4. Receives PreparedDescription (14)
  5. Sends PreparedAcknowledgement (15)
  6. Receives HeldReady (20)
```

#### Required B6 Sequence (with owner echo):

```
Owner (OwnerDirectConnection.Accept):
  1. Receives DirectBootstrap (32) from recovery
  2. Verifies DirectAssociation + SO_PEERPIDFD
  3. Creates listener, sends DirectRendezvous (33)
  4. Accepts controller connection → DirectSession.Establish
  5. Creates OwnerProtocolChannel

Owner (OwnerPreparationTransaction.Run):
  6. Receives Start (1) from controller
  7. Sends OwnerInstallationEcho (36) as FIRST owner-channel message  ← NEW
     Payload: {OwnerClosureSha256, protocols, Role=Owner, Qualified}
  8. Receives echo response? (Wire delta says echo is one-way owner→controller)
  9. Sends WriterReady (2)
  10. Receives StartAuthority (3) from recovery
  11. ... preparation continues ...
  12. Sends StagedObjects (12) (now SECOND owner-channel message)

Controller (IntegratedB3Client.Prepare):
  1. Establishes DirectSession
  2. Receives OwnerInstallationEcho (36) as FIRST message  ← NEW
     Validates against expected OwnerClosureSha256 from manifest
  3. On mismatch: Abort (no HELD, no domain, no ReleaseRequest)
  4. Receives WriterReady (2)
  5. Receives StagedObjects (12) (now second)
  6. Continues as before
```

#### Critical Verification Points:

1. **Owner has verified expected identity before echo:** Yes — `DirectSession.Establish` completes at step 4, which validates `DirectAssociation` with `SO_PEERPIDFD`. The owner knows its expected closure digest from the manifest (via `B3Qualification.VerifySha256` at `IntegratedRecoveryHost.cs:34`).

2. **DirectSession authenticated before echo:** Yes — echo sent over `OwnerProtocolChannel` created from established `DirectSession` (step 5).

3. **Controller expects echo before StagedObjects:** Yes — `IntegratedB3Client.Prepare` currently expects `StagedObjects` first (line 71). Must be changed to expect `OwnerInstallationEcho` (36) first.

4. **Echo does not consume RELEASE capability:** Confirmed — echo is a control message (type 36), not a transfer type. `HasValidControlPayloadShape` will enforce payload bounds. No descriptors.

5. **Lost acknowledgement handling:** The echo is one-way (owner→controller). If lost, controller times out (30s `TransitionTimeoutMilliseconds`) → abort. The "first valid peer attempt consumes association" rule (DirectSession line 72) applies to `DirectSession.Establish`, not to the echo.

6. **Replay protection:** `OwnerProtocolSequencer` per-direction sequence + `ExecutionId`/`LeaseGeneration` binding prevents replay across generations.

### 3.2 B2 Legacy Path (Owner Lifeline)

**Source:** `B2OwnerLifeline.Run()` (`RecoveryHost.cs:182–224`) ↔ `RecoveryHost.RunCore()` lines 73–84

#### Current Sequence:

```
Recovery (RecoveryHost.RunCore):
  1. Creates socketpair (recoveryEnd, ownerEnd)
  2. Spawns owner with ownerEnd + args: --exec <exec> --lease <gen> --b2-lifeline 1
  3. Sends B2StartLifeline (23) on recoveryEnd
  4. Receives B2LifelineReady (24) on recoveryEnd
  5. Enters main loop (sends B2SafetyStop (25) on stop)

Owner (B2OwnerLifeline.Run):
  1. Receives B2StartLifeline (23) on inherited FD
  2. Verifies recovery PID/UID + evidence directory
  3. Opens PIDFD to recovery
  4. Sends B2LifelineReady (24)
  5. Waits for B2SafetyStop (25) or recovery exit
  6. Sends B2StopObserved (26) on stop acknowledgement
```

#### Required B6 Sequence (with owner echo):

**Wire delta §2.4 step 6:** "The owner sends `OwnerInstallationEcho` (36) as its **first** owner-channel message, before `StagedObjects` (12) (qualified profile) or before `WriterReady` (2) (B2 path)."

**Problem:** The B2 legacy owner **never sends `WriterReady` (2)**. The B2 lifeline path uses `B2StartLifeline`/`B2LifelineReady`/`B2SafetyStop`/`B2StopObserved` — completely different message types from the preparation transaction path.

**Current B2 owner message flow:**
- Receives: `B2StartLifeline` (23)
- Sends: `B2LifelineReady` (24)
- Receives: `B2SafetyStop` (25)
- Sends: `B2StopObserved` (26)

**Required for B6:** Owner must send `OwnerInstallationEcho` (36) **after `B2LifelineReady` (24)** and **before entering stop-wait loop**.

#### Resolution:

This is an **implementation obligation for IMPL-01**, not a wire-contract defect. The B2 legacy owner (`B2OwnerLifeline.Run`) must be extended:

```csharp
// After sending B2LifelineReady (24), before stop-wait loop:
if (!channel.TrySend(OwnerProtocolMessageType.OwnerInstallationEcho, echoPayload, out _))
    return 3; // or appropriate error
```

The wire contract correctly specifies the ordering; the implementation must adapt the B2 legacy path.

---

## 4. Authority and One-Shot Connection Rules

### 4.1 Owner Echo Cannot Be Emitted Before Bootstrap Authority

- **Integrated:** Owner receives `DirectBootstrap` (32) with `DirectAssociation` → verifies → `DirectSession.Establish` → **then** sends echo. Authority (writer, capability) comes much later (`StartAuthority`, `Mint`).
- **B2:** Owner receives `B2StartLifeline` with recovery PID + evidence dir → verifies → sends `B2LifelineReady` → **then** sends echo. No authority transferred yet.

✅ **Echo emitted after bootstrap verification, before any authority.**

### 4.2 Echo Does Not Consume RELEASE Capability

- `RecoveryObligation.ReleaseAuthority => false` (hardcoded)
- Owner echo (36) is a control message, not a transfer type
- No descriptors in echo payload
- `ReleaseRequest` (27) only sent in Held phase, after `HeldReady` (20)

✅ **No RELEASE authority implied.**

### 4.3 Lost Acknowledgement / One-Shot Rule

- `DirectSession.Establish`: "A verified original-peer attempt consumes this association even when its ACK is lost" (OwnerDirectConnection.cs:72)
- This applies to the **DirectSession handshake** (frames 1–5), not to the owner echo
- Owner echo is sent **after** `DirectSession` is established and sealed (`listener.SealAssociation()`)
- If echo lost: controller times out → aborts → recovery stops → no second association possible

✅ **First valid peer attempt (DirectSession) remains subject to one-shot rule.**

### 4.4 Replay Across Execution/Session/Generation

- `OwnerProtocolSequencer` binds to `ExecutionId` + `LeaseGeneration`
- `TryDecode` validates `executionId == expectedExecutionId && leaseGeneration == expectedLeaseGeneration`
- `DirectAssociation` includes `Execution`, `Generation`, `OriginalSession`
- Replay from different generation fails ExecutionId/LeaseGeneration check
- Replay within same generation fails sequence check (stale/duplicate)

✅ **Replay prevented by envelope binding + sequencer.**

---

## 5. Channel Topology Differences

| Aspect | B2 Legacy Path | Integrated B3 Path |
|--------|----------------|-------------------|
| Recovery→Controller | `SOCK_SEQPACKET` control FD | `SOCK_SEQPACKET` control FD |
| Recovery→Owner | `SOCK_SEQPACKET` socketpair | Recovery spawns owner, owner creates listener |
| Controller→Owner | N/A (controller talks to recovery only) | `SOCK_STREAM` DirectSession (authenticated) |
| Owner first message | `B2LifelineReady` (24) | `StagedObjects` (12) — **now `OwnerInstallationEcho` (36)** |
| Owner echo placement | After `B2LifelineReady`, before stop-wait | After `DirectSession.Establish`, before `WriterReady`/`StagedObjects` |

**Key insight:** The two paths have **different channel topologies**. The wire delta correctly specifies ordering **per path**, not a single universal ordering.

---

## 6. Gate C & D Exit Criteria — Satisfied (with B2 Qualification)

### Gate C: Recovery Handshake
✅ **B2 Legacy:** Echo exchange insertable before `Ready` (2) — no deadlock, uses existing timeouts, preserves stop mechanism
✅ **Integrated B3:** Echo exchange insertable before `Rendezvous` (8) — `RecoveryConnection.Start` can be extended to handle new message type
✅ **No unhandled intermediate states:** Echo failure → existing `Stop()` path
✅ **No authority changes:** Recovery never gets RELEASE; echo is verification only

### Gate D: Owner Handshake
✅ **Integrated Path:** Owner echo (36) as first owner-channel message — after authenticated `DirectSession`, before `WriterReady`/`StagedObjects`
✅ **Owner has trusted identity before echo:** `DirectSession.Establish` with `SO_PEERPIDFD` completes first
✅ **Controller expects echo first:** `IntegratedB3Client.Prepare` must be updated (implementation obligation)
✅ **B2 Legacy Path:** **QUALIFICATION** — Current B2 owner never sends `WriterReady`; must send echo after `B2LifelineReady`. Implementation obligation for IMPL-01.
✅ **No RELEASE authority:** Echo is control message, no descriptors
✅ **Replay/one-shot preserved:** Sequencer + ExecutionId/LeaseGeneration binding

---

## 7. Implementation Obligations (for IMPL-01)

### Recovery Side:
1. `RecoveryHost.RunCore()`: After `Associate` authentication, before `Ready`/`Rendezvous`, send `InstallationEchoRequest` (9) and wait for `InstallationEchoResponse` (10)
2. `IntegratedRecoveryHost.Run()`: Same — echo before `Rendezvous` publication
3. On echo mismatch: Call `Stop("InstallationIdentityMismatch")` — uses existing stop mechanism

### Controller Side:
1. `RecoveryConnection.Start()`: After sending `Associate`, receive `InstallationEchoRequest` (9), send `InstallationEchoResponse` (10), then receive `Ready` (B2) or `Rendezvous` (Integrated)
2. `IntegratedB3Client.Prepare()`: After `DirectSession.Establish`, receive `OwnerInstallationEcho` (36) first, validate, then receive `WriterReady`/`StagedObjects`

### Owner Side:
1. `OwnerPreparationTransaction.Run()`: After `Start` (1) received, send `OwnerInstallationEcho` (36) before `WriterReady` (2)
2. `B2OwnerLifeline.Run()`: After `B2LifelineReady` (24), send `OwnerInstallationEcho` (36) before stop-wait loop

These are **implementation obligations**, correctly not part of the wire-contract review scope.