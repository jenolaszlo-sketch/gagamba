# RC2B-06 B6 WIRE-DELTA-01 — Installation-Identity Protocol Reconciliation

**Commission:** GAGAMBA-RC2B-06-B6-WIRE-DELTA-01.

**Status:** documentation-only contract correction. No production, native, test or
workflow change. No branch or worktree was created. Nothing was pushed.

**Starting HEAD:** `59d7ac050ff25e515d50a8a7f1a46a0ce466e640`.

**Disposition:** **WIRE DELTA READY FOR INDEPENDENT REVIEW** (see §8).

This report reconciles the accepted B6 installation-identity wire specification
(design §6.2) with the actual B1–B5 implementation. It does **not** reopen the
accepted installation architecture, the owner acceptance
([../decisions/rc2b-06-b6-spec-owner-acceptance.md](../decisions/rc2b-06-b6-spec-owner-acceptance.md))
or HD-1–HD-5, and it does not implement B6.

---

## 1. Determination

**The proposed IDs are genuine collisions, not a distinct versioned namespace.**

The design asserts (design §6.2) that IDs are "selected from the existing
`OwnerProtocolMessageType` / `RecoveryMessageType` namespace after verifying
available values (current max: `OwnerProtocolMessageType = 7`,
`RecoveryMessageType = 5`)."

That premise is **factually false**, and the allocated IDs are **already occupied**:

| B6 proposal | Claimed ID | Actually occupied by | Source |
|---|---|---|---|
| `InstallationEchoRequest` (Recovery→Controller) | `RecoveryMessageType = 6` | `RecoveryMessage.Reconnect = 6` | `Bootstrap/RecoveryProtocol.cs:9` |
| `InstallationEchoResponse` (Controller→Recovery) | `RecoveryMessageType = 7` | `RecoveryMessage.Refused = 7` | `Bootstrap/RecoveryProtocol.cs:9` |
| `OwnerInstallationEcho` (Owner→Controller) | `OwnerProtocolMessageType = 8` | `OwnerProtocolMessageType.StagingAuthority = 8` (a descriptor-bearing transfer type) | `Native/OwnerProtocol.cs:21` |
| (same owner ID 8 in the recovery namespace) | — | `RecoveryMessage.Rendezvous = 8` | `Bootstrap/RecoveryProtocol.cs:9` |
| `OwnerStartAuthority` row (design says Controller→Owner) | `OwnerProtocolMessageType = 3` | `StartAuthority = 3` exists but is **recovery→owner**, descriptor-bearing | `Lifecycle/M1RecoveryScope.cs:411`; `Lifecycle/OwnerPreparationTransaction.cs:103` |

Actual current maxima are far above the design's claim: `RecoveryMessage` max = **8**
(`Rendezvous`); `OwnerProtocolMessageType` max = **35** (`CaptureBytes`), with 1–35
all defined (`Native/OwnerProtocol.cs:12-53`).

### 1.1 There is no separate versioned wire namespace

Different C# enum names do **not** create separate wire namespaces. The actual model is:

- **One shared envelope** — `OwnerProtocol` (`Native/OwnerProtocol.cs:65-203`): `Magic`
  `0x4D314F50` ("M1OP"), `CurrentVersion = 1`, `HeaderBytes = 48`,
  `MaxPayloadBytes = 60*1024`. Header layout (all **little-endian**): magic u32 @0,
  version u16 @4, type u8 @6, reserved u8 @7, `ExecutionId` Guid @8, `LeaseGeneration`
  Guid @24, `Sequence` u32 @40, payload-length u32 @44, payload @48.
- **Recovery frames reuse that same envelope**, distinguished only by a first-byte XOR
  (`frame[0] ^= 0x20`) and the separate inherited socket: `RecoveryWire.Create` encodes
  with `OwnerProtocolSequencer` then does `frame[0] ^= 0x20`
  (`Bootstrap/RecoveryProtocol.cs:16-23`); `Accept` reverses it (`:24-38`). The type byte
  therefore shares the numeric space of `OwnerProtocolMessageType`, gated by the
  magic byte, the `RecoveryMessage` enum membership, and the per-direction sequence.
- **A third envelope exists for the integrated owner channel** — `DirectSession`
  (`Bootstrap/DirectSession.cs:24-184`): its own `Magic` `0x31444947` ("GID1"),
  64-byte header, own versions/sequence, HMAC handshake frames 1–5, and control frames
  of type **16** that carry the encoded `OwnerProtocol` frame as payload
  (`DirectSession.cs:141-146,174-183`).

So numeric IDs are meaningful **only within an envelope/magic/transport**. The design
treated a single shared type-byte space as if the enum names partitioned it; they do not.

### 1.2 Framing and transport descriptions are also incorrect

- The design says: "All post-bootstrap messages use the existing length-prefixed frame
  (**4-byte big-endian length + payload**)." No such framing exists in this protocol
  family (there is **no** big-endian write anywhere in `src/`). The real framing is the
  48-byte little-endian header above (or the 64-byte `DirectSession` header), over
  `SOCK_SEQPACKET` record boundaries with `SCM_RIGHTS` fd passing.
- The design says the bootstrap is delivered on "Recovery stdin (fd 0, length-prefixed
  JSON)" and echoes travel on "stdout/stdin fd 0/1". In reality:
  - The bootstrap JSON is delivered on its own inherited **`SOCK_SEQPACKET` credential
    descriptor** (`GAGAMBA_B2_CREDENTIAL_FD`), received with `FdPassing.TryReceiveMessage`
    (`Bootstrap/RecoveryHost.cs:22-40`; `Bootstrap/RecoveryConnection.cs:49-72`).
  - Recovery control/Echo traffic travels on the inherited **control descriptor**
    (`GAGAMBA_B2_CONTROL_FD`), not stdout/stdin (`RecoveryHost.cs:28-30,56-59`).
  - The owner channel is an inherited socketpair (`RecoveryHost.cs:73-83`) or the
    separately authenticated `DirectSession` `SOCK_STREAM`
    (`IntegratedB3Client.cs:68-70`; `OwnerDirectConnection.cs:53-81`).

### 1.3 Ordering model is incorrect

The design's handshake model ("recovery echo before `Ready`; owner echo before
`StartAuthority`; controller spawns owner with `OwnerStartAuthority`") does not match the
integrated topology actually implemented:

- The integrated recovery **never sends `Ready`**. After `Associate` and closure checks it
  starts the owner and publishes `Rendezvous` (`RecoveryMessage = 8`) carrying the owner's
  direct-session address plus one pidfd
  (`Bootstrap/IntegratedRecoveryHost.cs:61-62`; `RecoveryConnection.cs:79-100`).
- The controller then connects directly to the owner over a `DirectSession` and the
  owner's **first** message is `StagedObjects` (`OwnerProtocolMessageType = 12`), not an
  echo (`IntegratedB3Client.cs:71`; `Lifecycle/OwnerPreparationTransaction.cs:317`).
- `StartAuthority` (`OwnerProtocolMessageType = 3`) is sent **recovery→owner** and carries
  two descriptors (`M1RecoveryScope.cs:411-415`; consumed at
  `OwnerPreparationTransaction.cs:103-120`); it is not a controller→owner message.
- The owner's trusted expected controller/recovery identity already arrives before any
  authority via the immutable `DirectAssociation` (execution, store generation, original
  session, controller/owner/recovery `DirectProcessIdentity`) delivered with
  `DirectBootstrap` (`OwnerProtocolMessageType = 32`) and verified by
  `DirectSession.Establish` with `SO_PEERPIDFD` peer checks
  (`OwnerDirectConnection.cs:33-75`; `DirectSession.cs:52-59,113-124`).
- Current bootstrap version is `Version = 1` and is validated as exactly 1
  (`IntegratedB3Client.cs:53`; `RecoveryProtocol.cs:118`).

Existing role/authority restrictions that must be preserved:
`OwnerProtocolChannel.RecoveryOnly` + `ForbiddenRecovery` (`OwnerProtocol.cs:290,300-306`),
`DirectControl` allowlist (`OwnerProtocol.cs:291-299`), per-direction sequence with
duplicate/stale refusal (`OwnerProtocol.cs:249-274`), and
`RecoveryObligation.ReleaseAuthority => false` (`RecoveryProtocol.cs:68`).

---

## 2. Resolved wire contract (the delta)

The installation-identity exchange is defined by **extending the existing authenticated,
sequenced envelopes with new unused IDs**, not by inventing a parallel framing mechanism.

### 2.1 Namespace, IDs and versions

| Message | Wire namespace / envelope | Numeric ID | Direction | Transport | Max payload |
|---|---|---|---|---|---|
| `RecoveryConfigV2` | bootstrap record: `RecoveryConfiguration.Version = 2` (JSON) — the **bootstrap-record version axis**, not the frame version | N/A (record `Version = 2`) | Controller → Recovery | inherited credential `SOCK_SEQPACKET` (`GAGAMBA_B2_CREDENTIAL_FD`) | 8192 (`RecoveryConfiguration.MaxBytes`) |
| `InstallationEchoRequest` | recovery envelope (`OwnerProtocol` 48-byte LE header, recovery magic `frame[0] ^ 0x20`) | **`RecoveryMessage = 9`** | Recovery → Controller | inherited control `SOCK_SEQPACKET` (`GAGAMBA_B2_CONTROL_FD`) | 512 |
| `InstallationEchoResponse` | recovery envelope (same magic) | **`RecoveryMessage = 10`** | Controller → Recovery | same control channel | 512 |
| `OwnerInstallationEcho` | owner envelope (`OwnerProtocol` 48-byte LE header, M1OP magic) carried over the owner channel | **`OwnerProtocolMessageType = 36`** | Owner → Controller | qualified profile: `DirectSession` control frame type 16 (GID1 envelope); B2 path: inherited owner socketpair | 512 |

- **Frame version stays 1.** `OwnerProtocol.CurrentVersion` (1) and the `DirectSession`
  version (1) are **not** bumped: `TryDecode` requires the exact version, so a bump would
  refuse all existing B1–B5 frames. The new IDs are additive and unknown-ID frames already
  fail closed (`Enum.IsDefined`, `OwnerProtocol.cs:174`; `RecoveryProtocol.cs:28`).
- **Bootstrap v1 vs v2.** `Version = 1` has no identity and remains refused for the
  qualified profile (`RecoveryProtocol.cs:118`); `Version = 2` is the qualified record that
  carries `Integrated.InstallationIdentity`.
- **Do not use** `RecoveryMessage` 6/7/8 or `OwnerProtocolMessageType` 8. `StartAuthority`
  (3) is retained unchanged as a recovery→owner transfer type.
- The recovery IDs 9/10 are numerically adjacent to owner types 9 (`StagingAuthorityRetained`)
  and 10 (`StagingAccounted`); this is safe **only** because the magic byte gates direction
  (`OwnerProtocol.TryDecode` requires the M1OP magic; `RecoveryWire.Accept` restores and
  requires it after the XOR). This is exactly the "distinct magic prevents a controller
  packet being used as an owner packet" rule (`RecoveryProtocol.cs:19`) and is a mandatory
  negative test (§5.12).

### 2.2 Payload schemas

The echo payload schemas of design §6.2.3/§6.2.4 are retained unchanged (they are payload
contents, not envelope framing): `ArtifactSetSha256` (32) + role closure (32) + protocol
u16s + `Role` + `Qualified` + reserved + optional profile tail for the request; and
`Success`/`MismatchReason`/`ExpectedArtifactSetSha256` for the response. The struct→echo
projection of design §6.2.3 (Q2-F09) is unchanged.

### 2.3 Expected-identity provenance

- **Controller:** from the verified manifest recomputed from installed bytes, bound to
  `active` and the root-owned receipt.
- **Recovery:** from `RecoveryConfigV2.Integrated.InstallationIdentity`, which the
  controller built from the verified manifest; the recovery additionally verifies its own
  role-closure hash against its loaded `Assembly` location.
- **Owner:** from the immutable `DirectAssociation`/`DirectBootstrap` the owner already
  verifies before authority (`DirectSession.Establish` transcript binds
  execution/generation/original-session and the three `DirectProcessIdentity` values under
  `SO_PEERPIDFD`). The design's former claim that the owner's expected identity arrives via
  a controller→owner `StartAuthority` is **withdrawn**; `StartAuthority` is recovery→owner.

An actor reporting its own digest cannot qualify itself; each receiver compares the peer's
claim against its own independently held expected value.

### 2.4 Ordering (aligned to the real topology)

Accepted B1–B5 ordering is preserved; the echo is inserted only where an expected identity
already exists:

1. Controller creates the recovery, sends bootstrap (`Version = 2`) on the credential
   channel, then `Associate` (1) on control.
2. Recovery verifies its closure and the identity.
3. Recovery sends `InstallationEchoRequest` (**9**) on control; controller verifies and
   sends `InstallationEchoResponse` (**10**).
4. **Before** the recovery publishes `Rendezvous` (8) in the integrated profile (and
   **before** `Ready` (2) in the non-integrated B2 path: `Ready` is **2**, not 1 — the
   design's "`Ready` … ID 1" was also wrong), the recovery requires `Success = 1`;
   otherwise it stops via the existing C2/Policy A path.
5. The existing integrated flow proceeds: recovery starts the owner, publishes
   `Rendezvous` (8) with the owner pidfd, and the controller establishes the
   `DirectSession`.
6. The owner sends `OwnerInstallationEcho` (**36**) as its **first** owner-channel
   message, before `StagedObjects` (12) (qualified profile) or before `WriterReady` (2)
   (B2 path). On mismatch the controller aborts and requests stop; no HELD, no domain, no
   `ReleaseRequest`.
7. `StartAuthority` (3) remains recovery→owner and is unaffected.

### 2.5 Timeout, duplication, refusal, compatibility

- **Timeout:** each echo exchange uses the existing bootstrap/transition bound (recovery
  `Inspect` uses 1000 ms; the rendezvous receive uses 7000 ms; `DirectSession` control uses
  the 30 s transition timeout — `RecoveryConnection.cs:81,130`; `OwnerProtocol.cs:281`).
  Keep **no retries**; timeout → mismatch → abort.
- **Duplication/replay:** the existing per-direction `OwnerProtocolSequencer` refuses stale
  or duplicate sequence numbers (`OwnerProtocol.cs:249-274`); the echo does not add a second
  sequence mechanism. A replayed echo from another generation fails the bound
  `ExecutionId`/`LeaseGeneration` check (`OwnerProtocol.cs:180-186`).
- **Refusal:** any mismatch → recovery `Refused` (7) / C2 stop, or owner abort; no domain,
  staging, capability or RELEASE on mismatch. Recovery never receives RELEASE
  (`RecoveryObligation.ReleaseAuthority => false`).
- **Role/auth:** `RecoveryOnly` + `ForbiddenRecovery` must refuse the owner echo (36) from a
  recovery channel; `DirectControl` must include 36 for the controller side
  (`OwnerProtocol.cs:290-306`). Recovery echoes are gated by the recovery magic and the
  `RecoveryMessage` membership.
- **v1 compatibility:** all existing B1–B5 message IDs, envelopes, versions and sequences
  are unchanged. A v1 bootstrap presented as B6-qualified is refused. No existing message is
  renamed, renumbered or reinterpreted.

---

## 3. Implementation obligations (for B6-IMPL-01; not done here)

- Add `RecoveryMessage` members 9/10 and `PayloadBound` handling; add an explicit
  `OwnerProtocolMessageType = 36` and its `HasValidControlPayloadShape` case.
- Add 36 to `DirectControl`; ensure `ForbiddenRecovery(36)` refuses it.
- Emit/consume the echo at the §2.4 points only.
- Wire `Version = 2` bootstrap validation.
These are recorded as obligations, not implemented by this commission.

---

## 4. Qualification-plan assertions to update

Design §6.2 and plan `B6-T20b` must reference IDs 9/10/36, the real transports and the
§2.4 ordering. A deterministic wire-namespace suite (`B6-V13`) is added for the
magic/direction gate. See the plan.

---

## 5. Required negative tests (deterministic)

| # | Test | Expectation |
|---|---|---|
| 1 | Every old message ID under the existing protocol (`RecoveryMessage` 1–8; `OwnerProtocolMessageType` 1–35) | Accepted exactly as before; semantics unchanged |
| 2 | Every new installation-identity message (recovery 9/10; owner 36) | Accepted only in its correct envelope/direction |
| 3 | Wrong protocol version (frame version ≠ 1, bootstrap `Version` ≠ 2 for qualified) | Refused (`magic/version mismatch`; `Invalid B2 bootstrap configuration`) |
| 4 | Wrong direction/sender role (e.g. owner echo on a `RecoveryOnly` channel) | Refused (`owner channel authority role refused`) |
| 5 | Incorrect execution/generation identity | Refused (`transaction identity mismatch`) |
| 6 | Duplicate or replayed echo | Refused (`stale or duplicate owner protocol sequence`) |
| 7 | Missing or late echo | Timeout → mismatch → abort; no HELD/domain |
| 8 | Malformed and oversized payload (beyond 512 / beyond envelope bound) | Refused; no best-effort |
| 9 | Owner echo before trusted expected identity is available | Refused; owner never proceeds without the verified association |
| 10 | Recovery attempting to obtain RELEASE | Refused; `ReleaseAuthority => false`; no release frame/writer |
| 11 | Old v1 bootstrap presented as B6-qualified v2 | Refused |
| 12 | Conflicting interpretation across wire namespaces (recovery type 9 vs owner type 9; recovery frame fed to owner decoder and vice versa) | Refused by the magic/role gate; **no valid old message is interpreted as a new identity message** |

Existing suites to extend: `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/RecoveryB2Tests.cs`,
`.../Recovery/OwnerProtocolTests.cs`, `.../Restricted/OwnerHeldWireCodecTests.cs`,
`.../Bootstrap/OwnerSafetyMonitorTests.cs`, `.../Launch/ReleaseProtocolTests.cs`.

---

## 6. Preservation

- No production, native, test or workflow source changed.
- The accepted B6 architecture, owner decision, HD-1–HD-5, and the Q1/Q2 reviews and all
  evidence are unchanged.
- No branch or worktree created; nothing pushed.

---

## 7. Residual items for focused independent review

1. **Owner-echo placement** (§2.4 step 6) is a security-relevant ordering choice in the
   real integrated topology; it must be independently checked, not self-approved.
2. **Owner expected-identity provenance** now derives from the `DirectAssociation`; confirm
   this preserves the accepted "owner verifies before authority" guarantee.
3. **`OwnerProtocolMessageType = 36`** and recovery 9/10 allocations must be confirmed
   collision-free against the frozen B1–B5 enums at the implementation revision.

---

## 8. Disposition

**WIRE DELTA READY FOR INDEPENDENT REVIEW.**

The apparent collision is real and is now resolved by a narrow, source-backed contract
delta that reuses the existing authenticated, sequenced envelopes and allocates unused IDs
(9/10 recovery, 36 owner), with corrected transports, versions and ordering. Because this
changes a security-relevant wire contract, it **must not be self-approved**: it requires a
focused independent review before incorporation into the accepted design. If that review
finds the ordering/placement cannot be reconciled without changing accepted B1–B5 ordering,
the item escalates to **REMEDIATION REQUIRED** at that point.
