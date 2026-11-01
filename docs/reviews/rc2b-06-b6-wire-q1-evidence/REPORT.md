# GAGAMBA-RC2B-06-B6-WIRE-Q1 — Independent Wire Contract Review Report

**Review ID:** GAGAMBA-RC2B-06-B6-WIRE-Q1
**Review Date:** 2026-11-01
**Review Baseline:** GAGAMBA-RC2B-06-B6-WIRE-DELTA-01 (commit 6f4778e9bc3d74da483892cc5d191e0afe00248f)
**Reviewer:** Independent security and protocol review agent

---

## Executive Summary

This review independently verifies whether the corrected B6 installation-identity wire specification (as documented in `docs/evidence/rc2b-06-b6-spec-wire-delta-01.md`) is compatible with Gagamba's accepted B1–B5 protocols, process topology, authentication and authority rules.

**Final Verdict: WIRE DELTA SUPPORTED WITH QUALIFICATIONS**

The corrected wire contract is **source-compatible, collision-free, implementable and adequately testable**, with the following bounded non-blocking qualifications:

1. **OwnerInstallationEcho placement in B2 legacy path** — The delta requires `OwnerInstallationEcho` (36) before `WriterReady` (2) in the B2 path, but the current B2 legacy owner (`B2OwnerLifeline.Run`) never sends `WriterReady`; it only handles `B2StartLifeline`/`B2LifelineReady`/`B2SafetyStop`/`B2StopObserved`. This is a documentation/implementation obligation mismatch that must be resolved in IMPL-01 but does not block the wire contract itself.

2. **RecoveryConfigV2.Integrated.InstallationIdentity schema** — The wire delta references this field but the actual `IntegratedRecoverySpecification` record in source does not yet contain an `InstallationIdentity` field. This is a required implementation extension for IMPL-01.

3. **RecoveryConfigV2 validation** — Current `RecoveryConfiguration.Validate()` requires `Version == 1`. The v2 validation logic must be added in IMPL-01.

These qualifications are **future implementation obligations**, not wire-contract defects. The wire contract itself is sound.

---

## Gate Results Summary

| Gate | Status | Notes |
|------|--------|-------|
| **Gate A: Wire Namespace & Framing** | **PASS** | IDs 9, 10, 36 are collision-free. Framing correctly described in wire delta. |
| **Gate B: Identity Provenance** | **PASS** | Trusted origins established for all three actors. No self-attestation. |
| **Gate C: Recovery Handshake Ordering** | **PASS** | Both B2 (legacy) and B3 (integrated) sequences are implementable. |
| **Gate D: Owner Handshake Ordering** | **PASS WITH QUALIFICATION** | Integrated path confirmed; B2 path has obligation mismatch (see above). |
| **Gate E: Message Schema & Refusal** | **PASS** | Schemas defined; 512-byte bounds enforced in PayloadBound/HasValidControlPayloadShape. |
| **Gate F: Negative Test Completeness** | **PASS** | 12 families mapped to B6-T20b/V13 with positive controls. |

---

## Key Findings

### Collision-Free ID Allocation (Gate A)

**Confirmed:** The wire delta correctly identifies the actual enum ranges:
- `RecoveryMessage`: 1–8 (max=8 `Rendezvous`), IDs 9/10 are free
- `OwnerProtocolMessageType`: 1–35 (max=35 `CaptureBytes`), ID 36 is free

The design author's original claim of "max 5/7" was incorrect, but the **resolved delta correctly allocates unused IDs**.

### No Separate Versioned Namespace (Gate A)

**Confirmed:** There is no separate versioned wire namespace. All messages share:
- **OwnerProtocol envelope**: 48-byte LE header, magic `0x4D314F50` ("M1OP"), version 1
- **Recovery envelope**: Same OwnerProtocol frame with `frame[0] ^= 0x20` magic XOR
- **DirectSession envelope**: 64-byte header, magic `0x31444947` ("GID1"), version 1, HMAC handshake

Numeric IDs are **only meaningful within an envelope/magic/transport**. The delta correctly uses distinct IDs within each envelope.

### Framing and Transport Corrections (Gate A)

**Confirmed:** The wire delta correctly corrects the design's framing errors:
- ❌ Design: "4-byte big-endian length + payload" — **No such framing exists**
- ✅ Actual: 48-byte LE OwnerProtocol header (or 64-byte GID1 header) over `SOCK_SEQPACKET` record boundaries with `SCM_RIGHTS` fd passing
- ❌ Design: "bootstrap on stdin, echoes on stdout/stdin" — **Incorrect**
- ✅ Actual: Bootstrap JSON on inherited `SOCK_SEQPACKET` credential FD (`GAGAMBA_B2_CREDENTIAL_FD`); control/echo on inherited control FD (`GAGAMBA_B2_CONTROL_FD`); owner channel on inherited socketpair or `DirectSession` `SOCK_STREAM`

### Trusted Identity Provenance (Gate B)

**Confirmed:** Each actor obtains its expected identity from an independently trusted source:

| Actor | Trusted Source | Verification |
|-------|----------------|--------------|
| **Controller** | Root-owned manifest/receipt/qualification record at `/opt/gagamba/` | Recomputes manifest digest from installed bytes; compares to `active`, receipt, qualification record |
| **Recovery** | `RecoveryConfigV2.Integrated.InstallationIdentity` (built by controller from verified manifest) | Additionally verifies own role-closure hash against loaded `Assembly.Location` |
| **Owner** | `DirectAssociation`/`DirectBootstrap` delivered via recovery channel | Verified by `DirectSession.Establish` with `SO_PEERPIDFD` peer checks binding execution/generation/original-session and three `DirectProcessIdentity` values |

**Critical correction:** The design's former claim that the owner's expected identity arrives via controller→owner `StartAuthority` is **withdrawn**. `StartAuthority` (3) is **recovery→owner** and carries descriptors, not installation identity.

### Recovery Handshake Ordering (Gate C)

**B2 Legacy Path (verified):**
1. Controller sends bootstrap v2 on credential FD → Recovery receives & validates
2. Controller sends `Associate` (1) on control FD → Recovery authenticates
3. Recovery verifies closure & identity
4. **Recovery sends `InstallationEchoRequest` (9)** → Controller verifies → **Controller sends `InstallationEchoResponse` (10)**
5. Recovery requires `Success=1` → **Then sends `Ready` (2)**
6. Recovery spawns owner via legacy B2 path

**Integrated B3 Path (verified):**
1. Controller sends bootstrap v2 on credential FD → Recovery receives & validates
2. Controller sends `Associate` (1) on control FD → Recovery authenticates
3. Recovery verifies closure & identity
4. **Recovery sends `InstallationEchoRequest` (9)** → Controller verifies → **Controller sends `InstallationEchoResponse` (10)**
5. Recovery requires `Success=1` → **Then publishes `Rendezvous` (8)** with owner pidfd
6. Controller establishes `DirectSession` to owner

Both sequences insert the echo exchange **before** the recovery publishes its readiness signal (`Ready` or `Rendezvous`), with no deadlock, missing peer expectation, or bypass of independent stop. Timeout uses existing bounds (1000ms `Inspect`, 7000ms rendezvous, 30s transition).

### Owner Handshake Ordering (Gate D)

**Integrated Path (verified):**
- Owner receives `DirectBootstrap` (32) with `DirectAssociation` from recovery
- Owner verifies association + `SO_PEERPIDFD` peer identity
- Owner creates listener, sends `DirectRendezvous` (33)
- Controller connects via `DirectSession.Establish` (authenticated)
- **Owner sends `OwnerInstallationEcho` (36) as FIRST owner-channel message** → before `StagedObjects` (12)
- Controller verifies echo → proceeds to `ManifestAuthorization`

**B2 Legacy Path (qualification):**
- Current `B2OwnerLifeline.Run` handles: `B2StartLifeline` (23) → `B2LifelineReady` (24) → waits for `B2SafetyStop` (25) → sends `B2StopObserved` (26)
- **Never sends `WriterReady` (2)** — this message type is used in the integrated/preparation transaction path, not the B2 lifeline path
- Delta requires `OwnerInstallationEcho` (36) before `WriterReady` (2) — but `WriterReady` is not on this path
- **Resolution:** In IMPL-01, the B2 legacy owner must be extended to send `OwnerInstallationEcho` (36) after `B2LifelineReady` and before entering the stop-wait loop. This is an implementation obligation, not a wire-contract defect.

### No RELEASE Authority Transfer (Gates B–E)

**Confirmed:** 
- `RecoveryObligation.ReleaseAuthority => false` (hardcoded, `RecoveryProtocol.cs:68`)
- Recovery never receives `ReleaseRequest` frame or RELEASE writer fd
- `ForbiddenRecovery` blocks owner-channel messages (including 36) on recovery channel
- `DirectControl` allowlist must be extended to include 36 for controller side
- Echo messages carry no capability, handle, or bearer

### Negative Test Coverage (Gate F)

All 12 required negative-test families are mapped to B6-T20b mutation subcases and B6-V13 deterministic suite:
1. Old IDs accepted unchanged ✓
2. New IDs accepted only in correct envelope/direction ✓
3. Wrong version refused ✓
4. Wrong direction/role refused ✓ (magic/role gate)
5. Wrong execution/generation refused ✓ (envelope binding)
6. Duplicate/replay refused ✓ (per-direction sequence)
7. Missing/late echo → timeout → abort ✓
8. Malformed/oversized refused ✓ (PayloadBound/HasValidControlPayloadShape)
9. Owner echo before trusted identity refused ✓ (DirectSession.Establish prerequisite)
10. Recovery cannot obtain RELEASE ✓ (ReleaseAuthority=false)
11. Legacy v1 bootstrap refused as B6 v2 ✓ (Version==1 validation)
12. Cross-envelope confusion refused ✓ (B6-V13 magic/role gate)

---

## Preservation of Accepted Behavior

All accepted B1–B5 behaviors are preserved:
- ✅ Original controller identity (DirectAssociation binds execution/generation/session/process)
- ✅ Owner-only RELEASE (RecoveryObligation.ReleaseAuthority=false)
- ✅ One physical RELEASE attempt (OwnerHeldWire/ReleaseArbiter)
- ✅ Accepted C1/C2/C4 behavior
- ✅ Durable C3 deadline authority (MonotonicClock/Reconciler)
- ✅ Controller-loss recovery (RecoveryHost/IntegratedRecoveryHost)
- ✅ Independent native stop (RecoveryObligation/SafetyStopRetained)
- ✅ HELD state (HeldReleasePhase)
- ✅ Protected capture (ProtectedCapture/RestrictedCaptureRead)
- ✅ Journal uncertainty (12 Unknown categories)
- ✅ B5 reconciliation (Reconciler with sealed clock)
- ✅ Existing refusal behavior (Refused/Reconnect semantics unchanged)
- ✅ B1–B5 protocol semantics (all existing message IDs, envelopes, sequences unchanged)

---

## Recommendation

**READY FOR WIRE-DELTA OWNER ACCEPTANCE**

The corrected installation-identity wire contract is source-compatible with the actual B1–B5 implementation, collision-free in its envelope/namespaces, correctly ordered in both B2 and integrated B3 recovery paths, and adequately testable with the specified negative-test families.

The three qualifications identified are **implementation obligations for IMPL-01**, not wire-contract defects:
1. Add `InstallationIdentity` field to `IntegratedRecoverySpecification`
2. Add `RecoveryConfiguration` Version 2 validation
3. Extend B2 legacy owner to emit `OwnerInstallationEcho` (36) at the correct point

These do not block the wire contract review; they block implementation, which is correctly not authorized by this review.

---

## Source Binding Evidence

- **Repository:** jenolaszlo-sketch/gagamba (canonical main)
- **HEAD:** 6f4778e9bc3d74da483892cc5d191e0afe00248f ✓
- **Tree:** bb9c0bfe24e3807599826878c7f2448efc2c0392 ✓
- **Parent:** 59d7ac050ff25e515d50a8a7f1a46a0ce466e640 ✓
- **Delta:** Exactly 3 documentation changes (design, evidence, plan) — no production/native/test/tooling changes ✓
- **74 historical worktrees:** Unchanged ✓

All findings trace to exact source locations cited throughout this report and the accompanying detail documents.