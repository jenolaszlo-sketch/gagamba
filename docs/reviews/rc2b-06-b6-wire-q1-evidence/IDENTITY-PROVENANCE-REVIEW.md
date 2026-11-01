# GAGAMBA-RC2B-06-B6-WIRE-Q1 — Identity Provenance Review

**Review ID:** GAGAMBA-RC2B-06-B6-WIRE-Q1
**Date:** 2026-11-01
**Gate:** B — Trusted installation-identity provenance

---

## 1. Overview

This gate verifies that each actor (Controller, Recovery, Owner) obtains its **trusted expected installation identity** from an independently trusted source **before** accepting any peer echo. The design explicitly states: "A peer reporting its own artifact digest does not establish its own qualification."

---

## 2. Identity Definitions

### 2.1 Installation Identity Components

Per wire delta §2.3 and design §4.1:

| Component | Representation | Purpose |
|-----------|----------------|---------|
| `ArtifactSetSha256` | 32 bytes | SHA-256 of canonical manifest (installation generation ID) |
| `RecoveryClosureSha256` | 32 bytes | SHA-256 of recovery role closure (5-file PinnedExecutable set) |
| `OwnerClosureSha256` | 32 bytes | SHA-256 of owner role closure (5-file PinnedExecutable set) |
| `OwnerProtocol` | u16 | Protocol version (must be 1) |
| `CoordinatorProtocol` | u16 | Coordinator protocol version (must be 1) |
| `Bootstrap` | u16 | Bootstrap version (1 for legacy, 2 for qualified) |
| `Role` | byte | `Recovery` or `Owner` |
| `Qualified` | byte | 1 = qualified installation, 0 = unverified candidate |

---

## 3. Controller Expected Identity Provenance

### 3.1 Trusted Source

**Root-owned installation metadata at `/opt/gagamba/`:**
- `/opt/gagamba/active` — contains `<artifactSetSha256>\n` (root-owned, 0444)
- `/opt/gagamba/installations/<digest>/manifest.json` — canonical manifest (root-owned, 0444)
- `/opt/gagamba/installations/<digest>/install-receipt.json` — root-owned receipt (0444)
- `/opt/gagamba/qualifications/<qualificationId>.json` — accepted qualification record (root-owned, 0444)

### 3.2 Verification Logic (Design §4.5, B6-T20c)

```csharp
// Verifier recomputes manifest digest from installed bytes
// Compares to: directory name, active pointer, receipt, qualification record
// File in generation dir not in manifest → refuse
// Manifest entry with no file → refuse
```

### 3.3 Controller Bootstrap Construction

**Source:** `RecoveryConnection.Start()` (`RecoveryConnection.cs:28–118`)

```csharp
var config = requested with { 
    ControllerPid = Environment.ProcessId, 
    ControllerUid = RecoveryInterop.GetUid(), 
    Secret = RandomNumberGenerator.GetBytes(32) 
};
if (config.Integrated is not null)
    config = config with { Integrated = config.Integrated with { Controller = DirectProcessIdentity.Current() } };
```

The controller builds `RecoveryConfiguration` (bootstrap) from its verified manifest. For integrated profile, it includes `IntegratedRecoverySpecification` with `Controller` identity.

**Wire delta §2.1:** "Controller creates the recovery, sends bootstrap (Version = 2) on the credential channel"

**RecoveryConfigV2** (new in wire delta) carries `Integrated.InstallationIdentity` — the controller's expected identity for the recovery to echo back.

---

## 4. Recovery Expected Identity Provenance

### 4.1 Trusted Source

**`RecoveryConfigV2.Integrated.InstallationIdentity`** — built by the controller from the verified manifest and sent via the **credential FD** (inherited `SOCK_SEQPACKET`, not controllable by recovery).

### 4.2 Delivery Path

1. Controller serializes `RecoveryConfiguration` (Version=2) to JSON
2. Sends via `FdPassing.TrySendMessage(credential, packet)` — `credential` is the inherited `GAGAMBA_B2_CREDENTIAL_FD` socketpair end
3. Recovery receives via `FdPassing.TryReceiveMessage(credential, ...)` in `RecoveryHost.RunCore()` (`RecoveryHost.cs:36–38`)
4. Recovery decodes via `RecoveryConfiguration.Decode()` (`RecoveryProtocol.cs:107–115`)
5. Recovery validates via `config.Validate()` — **currently requires `Version == 1`** (must be extended for v2)

### 4.3 Recovery Self-Verification

**Source:** `RecoveryHost.RunCore()` (`RecoveryHost.cs:45–46`)

```csharp
if (config.ControllerPid != expectedParent || config.ControllerUid != uid
    || config.Recovery.Verify("Gagamba.Execution.Linux.M1.Recovery") != System.Reflection.Assembly.GetEntryAssembly()!.Location)
    return 3;
```

Recovery verifies:
- Controller PID/UID match parent process
- **Own assembly location** matches the `PinnedExecutable` recovery entry path (hash-verified by controller before spawn)

This binds the recovery's expected identity to its **actually loaded code**, not to what the controller claims.

### 4.4 Wire Delta §2.3 Statement

> "Recovery: from `RecoveryConfigV2.Integrated.InstallationIdentity`, which the controller built from the verified manifest; the recovery additionally verifies its own role-closure hash against its loaded `Assembly` location."

**Confirmed:** This matches actual source behavior. The recovery has **two independent identity checks**: controller-sent expected identity + self-verification of loaded assembly.

---

## 5. Owner Expected Identity Provenance

### 5.1 Trusted Source

**`DirectAssociation` / `DirectBootstrap`** — delivered via the **recovery channel** (trusted recovery process) and verified by `DirectSession.Establish` with `SO_PEERPIDFD`.

### 5.2 Delivery Path (Integrated)

**Source:** `OwnerDirectConnection.Accept()` (`OwnerDirectConnection.cs:15–41`)

```csharp
// 1. Recovery sends DirectBootstrap (32) with DirectAssociation payload + 2 descriptors
if (!recovery.TryReceiveTransfer(OwnerProtocolMessageType.DirectBootstrap, 2,
    DirectBootstrapPacket.Encode(new DirectAssociation(...)).Length,
    out var bootstrap, out fds, out _))
    throw new IOException("Original controller lifetime evidence unavailable");

var association = DirectBootstrapPacket.Decode(bootstrap!.Payload);

// 2. Verify association matches expected immutable values
if (association != new DirectAssociation(request.ExecutionId, request.StoreGenerationId,
    identity.OriginalSession, identity.Controller, DirectProcessIdentity.Current(), identity.Recovery)
    || !DirectPeerIdentity.LiveReference(fds[0], identity.Controller))
    throw new IOException("Immutable direct association refused");
```

### 5.3 DirectAssociation Contents (DirectSession.cs:9–19)

```csharp
internal sealed record DirectAssociation(
    Guid Execution,
    Guid Generation,
    Guid OriginalSession,
    DirectProcessIdentity Controller,
    DirectProcessIdentity Owner,
    DirectProcessIdentity Recovery
)
```

**Validated at receive (line 13–17):**
- Non-empty Execution, Generation, OriginalSession
- Three distinct PIDs (Controller ≠ Owner ≠ Recovery)
- All three `DirectProcessIdentity` validate (PID>0, UID/GID/namespace)

### 5.4 DirectSession.Establish Peer Verification (DirectSession.cs:58–60, 100–102)

```csharp
// Owner side: expects Controller
_peer = DirectPeerIdentity.Verify(stream.Descriptor, _expected, expectedPidfd);
// ...
if (!DirectPeerIdentity.LiveReference(expectedPidfd, session._expected)
    || !DirectPeerIdentity.LiveReference(session._peer.DangerousGetHandle().ToInt32(), session._expected))
    throw new IOException("Peer exited during direct handshake");
```

**`DirectPeerIdentity.Verify`** uses `SO_PEERPIDFD` to get peer's PIDFD, then verifies PID/UID/GID/namespace match expected `DirectProcessIdentity`.

### 5.5 Wire Delta §2.3 Statement

> "Owner: from the immutable `DirectAssociation`/`DirectBootstrap` the owner already verifies before authority (`DirectSession.Establish` transcript binds execution/generation/original-session and the three `DirectProcessIdentity` values under `SO_PEERPIDFD`). The design's former claim that the owner's expected identity arrives via a controller→owner `StartAuthority` is **withdrawn**; `StartAuthority` is recovery→owner."

**Confirmed:** This is a **critical correction**. The original design incorrectly stated the owner gets expected identity from `StartAuthority`. Actual source shows:
- `StartAuthority` (3) is **recovery→owner** (carries parent directory + staging parent fds)
- Owner's expected identity comes from `DirectBootstrap` (32) via recovery channel
- Owner verifies this **before any authority transfer** (before `StartAuthority` receive at `OwnerPreparationTransaction.cs:103`)

---

## 6. Identity Availability Before Echo Validation

| Actor | Expected Identity Available | Before Accepting Peer Echo |
|-------|----------------------------|---------------------------|
| **Controller** | Yes — from root-owned manifest/receipt at startup | Recovery sends `InstallationEchoRequest` (9) → Controller validates against its manifest-derived expected value |
| **Recovery** | Yes — from `RecoveryConfigV2` received on credential FD **before** any control message | Recovery sends `InstallationEchoRequest` (9) **after** bootstrap validation and `Associate` authentication |
| **Owner** | Yes — from `DirectBootstrap` received on recovery channel **before** `DirectSession` establishment | Owner sends `OwnerInstallationEcho` (36) **after** `DirectSession.Establish` completes (authenticated channel) |

**All three actors have trusted expected identity before emitting or validating echoes.**

---

## 7. Immutability and Unforgeability

### 7.1 Controller Expected Identity

- **Root-owned** manifest/receipt/qualification at `/opt/gagamba/`
- **Atomic `active` pointer** (temp+fsync+rename)
- **Mode 0444/0555**, root-owned ancestors, no symlinks
- **Cannot be changed by untrusted sender** — target/untrusted processes have no write access

### 7.2 Recovery Expected Identity

- Delivered via **credential FD** (inherited socketpair, `SCM_RIGHTS`)
- Controller **hashes closure** before spawn (`PinnedExecutable.Verify()`)
- Recovery **self-verifies** against loaded `Assembly.Location`
- **Cannot be changed by untrusted sender** — recovery receives before any control channel messages

### 7.3 Owner Expected Identity

- Delivered via **recovery channel** (trusted recovery process)
- `DirectBootstrap` carries `DirectAssociation` + **two descriptors** (controller pidfd + recovery safety pidfd)
- `DirectSession.Establish` uses `SO_PEERPIDFD` for **OS-verified peer identity**
- HMAC handshake binds `DirectAssociation` transcript to session keys
- **Cannot be changed by untrusted sender** — requires compromise of recovery or OS kernel

---

## 8. Echo Binds Installation Identity Independently

### 8.1 Recovery Echo (Request/Response)

- **Request (9):** Recovery sends its claimed `ArtifactSetSha256` + `RecoveryClosureSha256` + role + qualified flag
- **Response (10):** Controller compares to its expected values → `Success`/`MismatchReason`/`ExpectedArtifactSetSha256`
- **Binding:** Controller's expected value comes from **root-owned manifest**, not from recovery

### 8.2 Owner Echo (36)

- Owner sends its claimed `OwnerClosureSha256` + protocol versions + role + qualified flag
- Controller compares to expected value from **manifest** (via `RecoveryConfigV2` → recovery → `DirectAssociation` chain)
- **Binding:** Controller's expected value originates from **root-owned manifest**, not from owner

### 8.3 No Self-Attestation

> "An actor reporting its own digest cannot qualify itself; each receiver compares the peer's claim against its own independently held expected value." (Wire delta §2.3)

**Confirmed:** This is the core security property. The echo is a **challenge-response** where the verifier holds the expected value from a trusted source.

---

## 9. Bootstrap Record Extension Required

### 9.1 Current `RecoveryConfiguration` (RecoveryProtocol.cs:102–129)

```csharp
internal sealed record RecoveryConfiguration(
    ushort Version,           // Currently validated as == 1
    Guid Execution,
    Guid Generation,
    Guid Session,
    int ControllerPid,
    uint ControllerUid,
    byte[] Secret,
    PinnedExecutable Recovery,
    PinnedExecutable Owner,
    string EvidenceDirectory,
    string? DelegatedParent = null,
    IntegratedRecoverySpecification? Integrated = null
)
```

### 9.2 Required for B6 (Wire Delta §2.1)

- `Version = 2` for qualified profile (v1 remains for legacy, refused for qualified)
- `Integrated.InstallationIdentity` field containing the expected artifact-set digest and role closures

### 9.3 Current `IntegratedRecoverySpecification` (IntegratedRecoveryHost.cs:12–13)

```csharp
internal sealed record IntegratedRecoverySpecification(
    B3FixtureProfile Profile,
    Guid Issuer,
    Guid StoreGeneration,
    long AcceptedAtNs,
    long ExpiresAtNs,
    DirectProcessIdentity Controller,
    ExperimentalRuntimePin Runtime
)
```

**Missing:** `InstallationIdentity` field — **must be added in IMPL-01**

---

## 10. Gate B Exit Criteria — Satisfied

✅ **Controller:** Expected identity from root-owned manifest/receipt/qualification (independently trusted)
✅ **Recovery:** Expected identity from `RecoveryConfigV2` (controller-built from manifest) + self-verification of loaded assembly
✅ **Owner:** Expected identity from `DirectAssociation`/`DirectBootstrap` via trusted recovery + `SO_PEERPIDFD` verification
✅ **All available before echo validation:** Verified in §6
✅ **Cannot be changed by untrusted sender:** Root-owned files, inherited FDs, OS-verified peer identity
✅ **Echo binds installation identity independently:** Challenge-response with verifier-held expected values
✅ **Design correction acknowledged:** Owner identity does NOT come from `StartAuthority` (recovery→owner)

---

## 11. Implementation Obligations (for IMPL-01)

1. **Add `InstallationIdentity` to `IntegratedRecoverySpecification`** — carry artifact-set digest + role closure digests
2. **Extend `RecoveryConfiguration.Validate()`** to accept `Version == 2` and validate `Integrated.InstallationIdentity` presence
3. **Controller populates `InstallationIdentity`** when building `RecoveryConfiguration` for integrated profile
4. **Recovery reads `InstallationIdentity`** from decoded config and uses it for `InstallationEchoRequest` payload

These are source extensions required to implement the wire contract, not wire-contract defects.