# GAGAMBA-RC2B-06-B6-WIRE-Q1 — Wire Namespace & Framing Review

**Review ID:** GAGAMBA-RC2B-06-B6-WIRE-Q1
**Date:** 2026-11-01
**Gate:** A — Exact wire namespace and framing

---

## 1. Enumeration of All Existing Numeric Message IDs

### 1.1 RecoveryMessage Enum (RecoveryProtocol.cs:8)

```csharp
internal enum RecoveryMessage : byte {
    Associate = 1,
    Ready = 2,
    Stop = 3,
    Inspect = 4,
    Snapshot = 5,
    Reconnect = 6,
    Refused = 7,
    Rendezvous = 8
}
```

**Current maximum:** 8 (`Rendezvous`)
**Proposed new IDs:** 9 (`InstallationEchoRequest`), 10 (`InstallationEchoResponse`)
**Status:** ✅ **COLLISION-FREE** — IDs 9 and 10 are unused

### 1.2 OwnerProtocolMessageType Enum (OwnerProtocol.cs:12–53)

```csharp
internal enum OwnerProtocolMessageType : byte {
    Start = 1,
    WriterReady = 2,
    StartAuthority = 3,
    DomainRecoveryAuthority = 4,
    DomainAuthorityRetained = 5,
    DomainRegistered = 6,
    DomainRegistrationRetained = 7,
    StagingAuthority = 8,
    StagingAuthorityRetained = 9,
    StagingAccounted = 10,
    StagingMayProceed = 11,
    StagedObjects = 12,
    ManifestAuthorization = 13,
    PreparedDescription = 14,
    PreparedAcknowledgement = 15,
    BarrierReached = 16,
    Continue = 17,
    Completed = 18,
    Aborted = 19,
    HeldReady = 20,
    AbortHeld = 21,
    HeldAborted = 22,
    B2StartLifeline = 23,
    B2LifelineReady = 24,
    B2SafetyStop = 25,
    B2StopObserved = 26,
    ReleaseRequest = 27,
    ReleaseDecision = 28,
    StopRequest = 29,
    StopDecision = 30,
    ExecutionOutcome = 31,
    DirectBootstrap = 32,
    DirectRendezvous = 33,
    CaptureRead = 34,
    CaptureBytes = 35
}
```

**Current maximum:** 35 (`CaptureBytes`)
**Proposed new ID:** 36 (`OwnerInstallationEcho`)
**Status:** ✅ **COLLISION-FREE** — ID 36 is unused

---

## 2. Design Author's Original Claim vs. Actual Source

| Design Claim | Actual Source | Disposition |
|--------------|---------------|-------------|
| "current max: OwnerProtocolMessageType = 7" | Max = 35 (CaptureBytes) | **FALSE** — corrected in wire delta |
| "current max: RecoveryMessageType = 5" | Max = 8 (Rendezvous) | **FALSE** — corrected in wire delta |
| "IDs selected after verifying available values" | Original design allocated 6/7/8/3 — all occupied | **FALSE** — wire delta re-allocates to 9/10/36 |

**Wire delta correctly resolves all collisions by using genuinely unused IDs.**

---

## 3. Envelope and Framing Verification

### 3.1 OwnerProtocol Envelope (Primary Envelope)

**Source:** `OwnerProtocol.cs:65–203`

| Field | Offset | Size | Endianness | Value/Constraint |
|-------|--------|------|------------|------------------|
| Magic | 0 | 4 | LE | `0x4D314F50` ("M1OP") |
| Version | 4 | 2 | LE | `1` (CurrentVersion) |
| Type | 6 | 1 | — | `OwnerProtocolMessageType` |
| Reserved | 7 | 1 | — | Must be 0 |
| ExecutionId | 8 | 16 | — | Guid (non-empty) |
| LeaseGeneration | 24 | 16 | — | Guid (non-empty) |
| Sequence | 40 | 4 | LE | uint32 (per-direction, monotonic) |
| PayloadLength | 44 | 4 | LE | uint32 (≤ 60KB) |
| Payload | 48 | N | — | Variable |

**Constants:**
- `HeaderBytes = 48`
- `MaxPayloadBytes = 60 * 1024`
- `MaxFrameBytes = HeaderBytes + MaxPayloadBytes`

**Validation in `TryDecode` (lines 151–202):**
- Frame length bounds check
- Magic == `0x4D314F50`
- Version == `1` (exact match — no version negotiation)
- Type is defined enum value
- Reserved byte == 0
- ExecutionId matches expected
- LeaseGeneration matches expected
- Payload length matches frame length

### 3.2 Recovery Envelope (Derived from OwnerProtocol)

**Source:** `RecoveryProtocol.cs:10–38`

- **Encoding:** `RecoveryWire.Create()` calls `_sequence.Create((OwnerProtocolMessageType)(byte)type, payload)` then `frame[0] ^= 0x20`
- **Decoding:** `RecoveryWire.Accept()` copies frame, does `copy[0] ^= 0x20`, then calls `_sequence.TryAccept()` with allowed types from `RecoveryMessage` enum
- **Magic gating:** The XOR distinguishes recovery frames from owner frames on the wire
- **Payload bounds:** `PayloadBound()` returns 512 for most types, 4096 for `Rendezvous`/`Ready`/`Snapshot`

**Critical validation in `Accept` (lines 24–38):**
- Frame length bounds
- Type is defined `RecoveryMessage` enum value
- Payload length ≤ `PayloadBound(type)`
- Sequence validation via `OwnerProtocolSequencer`
- Type must be in allowed `RecoveryMessage` set
- `Associate` requires exactly 48-byte payload; `Ready`/`Snapshot`/`Rendezvous` ≤ 4096; others require 0-byte payload

### 3.3 DirectSession Envelope (Integrated Owner Channel)

**Source:** `DirectSession.cs:24–185`

| Field | Offset | Size | Endianness | Value |
|-------|--------|------|------------|-------|
| Magic | 0 | 4 | LE | `0x31444947` ("GID1") |
| Version | 4 | 2 | LE | `1` |
| Type | 6 | 2 | LE | Frame type (1–5 handshake, 16 = control) |
| Sequence | 8 | 4 | LE | uint32 (per-direction) |
| ExecutionId | 12 | 16 | — | Guid |
| Generation | 28 | 16 | — | Guid |
| OriginalSession | 44 | 16 | — | Guid |
| PayloadLength | 60 | 4 | LE | int32 |
| Payload | 64 | N | — | Variable |

**Handshake frames (types 1–5):** HMAC-authenticated challenge/response binding `DirectAssociation`
**Control frames (type 16):** Carry encoded `OwnerProtocol` frame as payload

**Key methods:**
- `SendControl(byte[] encodedOwnerFrame)` — wraps OwnerProtocol frame in GID1 type 16
- `TryReceiveControl()` — unwraps GID1 type 16, returns OwnerProtocol frame

---

## 4. Transport Verification

| Message | Transport | FD Environment Variable | Direction |
|---------|-----------|------------------------|-----------|
| Recovery bootstrap (JSON) | `SOCK_SEQPACKET` credential FD | `GAGAMBA_B2_CREDENTIAL_FD` | Controller → Recovery |
| Recovery control (Associate, Echo, Ready, etc.) | `SOCK_SEQPACKET` control FD | `GAGAMBA_B2_CONTROL_FD` | Bidirectional |
| Owner channel (B2 legacy) | `SOCK_SEQPACKET` socketpair | Inherited fd (no env var) | Recovery ↔ Owner |
| Owner channel (Integrated) | `SOCK_STREAM` (DirectSession) | Connected to owner listener | Controller ↔ Owner |

**Confirmed:** No stdin/stdout framing. No big-endian length prefix. All framing is fixed-header over record-oriented sockets.

---

## 5. Role and Direction Restrictions

### 5.1 OwnerProtocolChannel Role Checks (OwnerProtocol.cs:290–306)

```csharp
// Controller-side allowed control messages
DirectControl(type) => type is
    StagedObjects, ManifestAuthorization, PreparedDescription, PreparedAcknowledgement,
    HeldReady, AbortHeld, HeldAborted, ReleaseRequest, ReleaseDecision,
    StopRequest, StopDecision, ExecutionOutcome, CaptureRead, CaptureBytes,
    Aborted, BarrierReached, Continue;

// Recovery-side forbidden messages (recovery cannot send these)
ForbiddenRecovery(type) => type is
    StagedObjects, ManifestAuthorization, HeldReady, AbortHeld, HeldAborted,
    ReleaseRequest, ReleaseDecision, StopRequest, StopDecision, ExecutionOutcome,
    CaptureRead, CaptureBytes;
```

### 5.2 RecoveryWire Allowed Types (RecoveryProtocol.cs:30)

```csharp
var allowed = Enum.GetValues<RecoveryMessage>()
    .Select(x => (OwnerProtocolMessageType)(byte)x).ToArray();
```

Only `RecoveryMessage` enum values (1–8) are accepted on recovery channel. Owner types (9–35) are rejected by the `allowed.Contains(message.Type)` check.

### 5.3 RecoveryOnly Flag (OwnerProtocolChannel.cs:290, 340, 402)

When `RecoveryOnly = true`:
- `TrySend` refuses any `ForbiddenRecovery` type
- `TryReceiveAllowed` refuses any `ForbiddenRecovery` in allowedTypes

---

## 6. Payload Bounds Verification

### 6.1 RecoveryWire.PayloadBound (RecoveryProtocol.cs:12–13)

```csharp
internal const int MaxPayload = 512;
internal static int PayloadBound(RecoveryMessage type) =>
    type is RecoveryMessage.Rendezvous or RecoveryMessage.Ready or RecoveryMessage.Snapshot
        ? 4096
        : MaxPayload;
```

**For new messages:**
- `InstallationEchoRequest` (9): 512 bytes (default)
- `InstallationEchoResponse` (10): 512 bytes (default)

### 6.2 OwnerProtocol.HasValidControlPayloadShape (OwnerProtocol.cs:81–111)

Explicit cases for each control message type. **Default case (line 110):**
```csharp
_ => !IsTransferType(message.Type)
```

**Transfer types (require descriptors, validated separately):**
- `StartAuthority` (3)
- `DomainRecoveryAuthority` (4)
- `StagingAuthority` (8)
- `DirectBootstrap` (32)

**Non-transfer types:** Must have explicit case or fall through to default (which allows zero-length payload).

**For `OwnerInstallationEcho` (36):** Must add explicit case in `HasValidControlPayloadShape` with 512-byte bound.

---

## 7. Confusion Tests (Cross-Envelope)

| Scenario | Expected Behavior | Mechanism |
|----------|-------------------|-----------|
| Recovery frame (magic XOR) fed to OwnerProtocol.TryDecode | Refused — magic mismatch | `TryDecode` checks `Magic == 0x4D314F50` |
| Owner frame (M1OP) fed to RecoveryWire.Accept | Refused — magic restored, type not in RecoveryMessage enum | `Accept` restores XOR, checks `Enum.IsDefined((RecoveryMessage)frame[6])` |
| Recovery type 9 (`InstallationEchoRequest`) vs Owner type 9 (`StagingAuthorityRetained`) | **Distinct** — different envelope magic gates them | Recovery: magic XOR; Owner: M1OP magic |
| Recovery type 10 (`InstallationEchoResponse`) vs Owner type 10 (`StagingAccounted`) | **Distinct** — different envelope magic | Same as above |
| Owner type 36 on recovery channel | Refused — not in `RecoveryMessage` enum | `Accept` checks `Enum.IsDefined((RecoveryMessage)frame[6])` |
| Owner type 36 on DirectSession (controller side) | Accepted — added to `DirectControl` allowlist | `OwnerProtocolChannel.DirectControl` must include 36 |
| Owner type 36 on recovery channel (RecoveryOnly=true) | Refused — `ForbiddenRecovery(36)` must return true | `OwnerProtocolChannel.ForbiddenRecovery` must include 36 |

---

## 8. OwnerProtocolSequencer Reuse

**Source:** `OwnerProtocol.cs:208–275`

- Single sequencer per channel endpoint
- Per-direction sequences: `_nextSend` and `_nextReceive`
- `Create()` increments send sequence, encodes frame
- `TryAccept()` validates execution/generation identity, then checks expected type and exact next sequence
- `TryAcceptDecoded()` allows multiple allowed types but enforces exact sequence

**RecoveryWire reuses this sequencer** (`RecoveryProtocol.cs:14–15`):
```csharp
private readonly OwnerProtocolSequencer _sequence;
internal RecoveryWire(Guid execution, Guid generation) => _sequence = new(execution, generation);
```

This means **recovery and owner channels share the same sequence space per endpoint** — a recovery message consumes a sequence number that the owner channel would also consume if on the same socket. This is correct because they are on **different inherited sockets** (control FD vs owner socketpair).

---

## 9. Gate A Exit Criteria — Satisfied

✅ **Collision-free IDs:** 9, 10 (recovery), 36 (owner) are unused in their respective enum namespaces
✅ **Exactly framed:** 48-byte LE OwnerProtocol header (recovery with XOR), 64-byte LE GID1 header (DirectSession)
✅ **Appropriately bounded:** 512-byte default for echoes; explicit `HasValidControlPayloadShape` case required for 36
✅ **Role restrictions:** `RecoveryOnly`/`ForbiddenRecovery` and `DirectControl` enforce channel-appropriate types
✅ **No confusion:** Magic byte + enum membership + sequence + execution/generation binding prevent cross-envelope confusion
✅ **No version bump:** Frame version stays 1; bootstrap record version (v2) is the version axis

---

## 10. Required Implementation Extensions (for IMPL-01)

1. **RecoveryProtocol.cs:** Add `InstallationEchoRequest = 9`, `InstallationEchoResponse = 10` to `RecoveryMessage` enum
2. **RecoveryProtocol.cs:** Extend `PayloadBound` — echoes use default 512 (no change needed)
3. **RecoveryProtocol.cs:** Extend `Accept` validation — `type is RecoveryMessage.InstallationEchoRequest or RecoveryMessage.InstallationEchoResponse ? payload.Length <= 512 : ...`
4. **OwnerProtocol.cs:** Add `OwnerInstallationEcho = 36` to `OwnerProtocolMessageType` enum
5. **OwnerProtocol.cs:** Add case to `HasValidControlPayloadShape` for `OwnerInstallationEcho` → `payload.Length <= 512`
6. **OwnerProtocol.cs:** Add 36 to `DirectControl` allowlist
7. **OwnerProtocol.cs:** Add 36 to `ForbiddenRecovery` check
8. **RecoveryConfiguration.cs:** Add `Version == 2` validation path with `Integrated.InstallationIdentity` field
9. **IntegratedRecoverySpecification.cs:** Add `InstallationIdentity` field (artifact-set digest + role closure digests)

These are **implementation obligations**, not wire-contract defects.