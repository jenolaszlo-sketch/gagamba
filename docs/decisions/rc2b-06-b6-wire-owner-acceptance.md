# RC2B-06 B6 Installation-Identity Wire Contract — Owner Acceptance with Qualifications

**Commission:** GAGAMBA-RC2B-06-B6-WIRE-OWNER-01
**Date:** 2026-11-01
**Status:** Accepted with qualifications — implementation not authorized
**Parent Decision:** `docs/decisions/rc2b-06-b6-spec-owner-acceptance.md` (commit `59d7ac050ff25e515d50a8a7f1a46a0ce466e640`)
**Reviewed Delta:** `docs/evidence/rc2b-06-b6-spec-wire-delta-01.md` (commit `6f4778e9bc3d74da483892cc5d191e0afe00248f`)
**Independent Review:** `docs/reviews/rc2b-06-b6-wire-q1-independent-review.md`

---

## A. Source Identity

| Artifact | Commit | Tree |
|----------|--------|------|
| B5 accepted baseline | `2cd8c1d716ed2e3e61156819730cd3d39e81844b` | `e9ed9989ee35c43557e0a93fdec4d313af4f740e` |
| Original B6 specification | `cc47f4dd2dc23895dc1d0cfacea81301ec75f08b` | — |
| B6 independent specification Q1 | `08ebcb5479e5ffb78189c06f1bd5c02d49f2f7df` | — |
| B6 specification R1 | `77ee4414df37b9f88979bde02742756538ba45af` | — |
| R1 provenance receipt | `5b3466719a50df072848ecc5f832adbaeb06b3bf` | — |
| B6 Q2 evidence preservation | `f63fd9688b1d85f56f8fd9619b2c3a5f1e470bf0` | — |
| B6 Q2 corrections | `00322320cd36114525cc7231df3f1147641d84db` | — |
| B6 specification owner acceptance | `59d7ac050ff25e515d50a8a7f1a46a0ce466e640` | — |
| **Wire-delta correction (reviewed)** | `6f4778e9bc3d74da483892cc5d191e0afe00248f` | `bb9c0bfe24e3807599826878c7f2448efc2c0392` |

---

## B. Owner Acceptance

**B6 WIRE DELTA ACCEPTED WITH QUALIFICATIONS**

The independently reviewed installation-identity wire contract is accepted as an addendum to the B6 specification owner decision. The corrected protocol extends the existing trusted-process communication system (B1–B5) rather than introducing a new generic framing protocol.

---

## C. Accepted Protocol

### C.1 Message Identities

| Message | Numeric Identity | Direction | Envelope |
|---------|------------------|-----------|----------|
| `RecoveryConfigV2` | Bootstrap `Version = 2` | Controller → Recovery | JSON on credential FD (`GAGAMBA_B2_CREDENTIAL_FD`) |
| `InstallationEchoRequest` | `RecoveryMessage = 9` | Recovery → Controller | OwnerProtocol M1OP with `frame[0] ^= 0x20` |
| `InstallationEchoResponse` | `RecoveryMessage = 10` | Controller → Recovery | OwnerProtocol M1OP with `frame[0] ^= 0x20` |
| `OwnerInstallationEcho` | `OwnerProtocolMessageType = 36` | Owner → Controller | OwnerProtocol M1OP (over DirectSession GID1 or B2 socketpair) |

Existing message identities unchanged:
- `RecoveryMessage`: 1–8 (`Associate` through `Rendezvous`)
- `OwnerProtocolMessageType`: 1–35 (`Start` through `CaptureBytes`)

### C.2 Framing (Retained from B1–B5)

- `OwnerProtocol` M1OP envelope: magic `0x4D314F50`, version 1, 48-byte little-endian header
- Recovery envelope: OwnerProtocol frame with `frame[0] ^= 0x20` magic XOR
- `DirectSession` GID1 envelope: magic `0x31444947`, version 1, 64-byte header, HMAC handshake
- Execution/generation identities and per-direction sequencing preserved
- Role restrictions: `RecoveryOnly`/`ForbiddenRecovery` and `DirectControl` allowlists
- Descriptor-passing safeguards (`SCM_RIGHTS`, `SCM_CREDENTIALS`) preserved

**No new framing protocol introduced.** Version 1 wire framing retained. Version-2 bootstrap record distinguishes B6-qualified contract.

### C.3 Payload Rules

- Maximum echo payload: **512 bytes**
- Explicit message-specific validation required in implementation:
  - Payload bounds (not generic maximum or permissive default)
  - Field canonicalization (exact identity comparison)
  - Sequence validation (per-direction monotonic)
  - Execution/generation binding
  - Role/direction checks
  - Refusal of malformed, oversized, duplicate, or replayed frames

---

## D. Accepted Handshake Topology

### D.1 Legacy B2 Path (Non-Integrated)

For B6-qualified version-2 bootstrap:

1. Controller supplies version-2 bootstrap data on credential FD
2. Recovery authenticates existing controller association (`Associate` = 1)
3. Recovery validates its trusted installation identity (self + config)
4. Recovery sends `InstallationEchoRequest` (9)
5. Controller validates request against independently trusted expected identity
6. Controller sends `InstallationEchoResponse` (10)
7. Recovery validates response
8. Existing B2 readiness (`Ready` = 2) and safety behavior continues

**Legacy v1 operation:** Must not be mislabeled as B6-qualified. Version-1 bootstrap refused for qualified profile.

### D.2 Integrated B3 Path

Installation-identity exchange before `RecoveryMessage.Rendezvous` (8):

1. Controller supplies version-2 bootstrap with `Integrated.InstallationIdentity`
2. Recovery authenticates association
3. Recovery sends `InstallationEchoRequest` (9)
4. Controller validates and responds `InstallationEchoResponse` (10)
5. Recovery validates response
6. Recovery publishes `Rendezvous` (8) with owner process-reference
7. Controller establishes authenticated `DirectSession`
8. Owner sends `OwnerInstallationEcho` (36) as **first** owner-channel message
9. Controller validates echo against expected installation identity
10. Existing `StagedObjects` (12) and preparation flow continues

**Do not substitute `Ready` for integrated `Rendezvous`.**

### D.3 Recovery Failure

If either installation echo fails:
- No B6-qualified readiness established
- No qualified rendezvous accepted
- No RELEASE authority granted
- Existing retained stop and cleanup obligations remain enforceable
- Refusal explicit and evidence-backed
- No implicit recovery reconnect or cross-generation retry

---

## E. Identity Provenance

### Controller
Expected installation identity from independently qualified installation manifest, receipt, and accepted qualification record at `/opt/gagamba/`. Bound to installed artifact set, not caller-supplied configuration.

### Recovery
Receives expected identity through authenticated version-2 bootstrap (`RecoveryConfigV2.Integrated.InstallationIdentity`). Must verify against its own trusted installed closure before sending successful echo.

### Owner
Obtains expected B6 installation identity through trusted bootstrap source (`DirectAssociation` + `DirectBootstrap` via recovery channel, verified by `DirectSession.Establish` with `SO_PEERPIDFD`). **A process identity is not an artifact-set identity.** Owner must bind expected identity to installed bytes it actually consumes. Owner self-attestation does not qualify itself.

### Invariant
Controller, owner, and recovery must agree on the same qualified installation identity. No actor may satisfy this by copying an unverified value from the peer it authenticates. Mismatch, missing identity, or unverified closure → refusal. No cross-generation adoption.

---

## F. Authority Restrictions

The owner decision explicitly preserves every accepted B1–B5 authority invariant:

- Owner is the **sole physical RELEASE writer**
- Recovery **never receives RELEASE authority** (`RecoveryObligation.ReleaseAuthority => false`)
- No new RELEASE token, bearer, or descriptor serialized
- No recovery-driven admission shortcut permitted
- Existing one-shot original-controller association enforced
- Existing controller-loss behavior remains conservative
- Existing C3 durable deadline obligation remains authoritative
- HELD stopping and cleanup semantics unchanged
- Journal uncertainty not upgraded to success
- No disk-only authority reconstruction
- No new cross-generation adoption
- No reconnect/retransmission bypassing existing safety decisions

**The installation echo establishes a checked identity condition. It is not a capability. It must not be implemented as one.**

---

## G. Independent Review Disposition

| Gate | Disposition | Evidence |
|------|-------------|----------|
| A. Wire namespace and framing | PASS | IDs 9/10/36 collision-free; framing/source-verified |
| B. Trusted identity provenance | PASS | Three independent trust origins; available before echo |
| C. Recovery handshake ordering | PASS | B2 and Integrated sequences implementable |
| D. Owner handshake ordering | PASS WITH QUALIFICATION | Integrated confirmed; B2 requires implementation mapping |
| E. Message schema and refusal | PASS | 512-byte bounds; role checks; no RELEASE authority |
| F. Negative-test mapping | PASS | 12 families → B6-T20b/V13; positive controls exist |

**Qualifications (implementation obligations, not wire-contract defects):**
1. `IntegratedRecoverySpecification` missing `InstallationIdentity` field
2. `RecoveryConfiguration.Validate()` requires Version==1; v2 validation absent
3. B2 legacy owner path differs from integrated path (references `WriterReady` where B2 uses `B2LifelineReady`)

---

## H. Implementation Obligations (WI-01 through WI-06)

### WI-01: Installation Identity in Recovery Bootstrap
- Define canonical `InstallationIdentity` type (artifact-set digest + role closure digests)
- Add required version-2 field to `IntegratedRecoverySpecification`
- Validate against qualified installed closure
- Reject absent, malformed, mismatched identities
- Preserve distinct legacy v1 behavior

### WI-02: Recovery Configuration Version Handling
- Accept version-2 bootstrap only for B6-qualified path
- Enforce all required v2 fields
- Preserve correct v1 behavior
- Refuse unsupported/incomplete versions
- Enforce existing size/decoding limits

### WI-03: B2 Owner Echo Ordering and Verification
- Keep `B2LifelineReady` semantics intact
- Introduce owner echo at reviewed B2 insertion point (after `B2LifelineReady`)
- Identify actual verification recipient
- Establish trusted expected identity provenance
- Ensure qualified admission cannot precede successful verification
- Preserve independent stop behavior
- Test failed, late, missing, replayed echoes

### WI-04: Integrated Owner Identity
- Show how expected installation identity reaches owner independently of echo
- `DirectAssociation` process-identity structure must not be presented as containing artifact-set digest
- Any extension must preserve authenticated association, roles, session binding

### WI-05: Explicit Decoder Constraints
Implement message-specific:
- Payload bounds (512 bytes)
- Shape validation (`HasValidControlPayloadShape` case for 36)
- Message direction (recovery 9/10: recovery→controller / controller→recovery; owner 36: owner→controller)
- Role authorization (add 36 to `DirectControl`, `ForbiddenRecovery`)
- Sequence/replay refusal (existing sequencer)
- Version compatibility

### WI-06: Production Qualification
- Tests exercise actual production-configuration binaries
- Native process topology (B2 + integrated)
- Instrumented/test-hook builds cannot substitute
- Subject to B6 installation trust, native closure, qualification gates

---

## I. Qualification Test Obligations

### B6-T20b: Installation-Identity Handshake Qualification
Eight mutation subcases:
1. Valid installed identities
2. Incorrect recovery identity
3. Incorrect owner identity
4. Missing identity
5. Protocol/version mismatch
6. Generation mismatch
7. Replay or duplicate message
8. Failure to obtain qualified readiness/release following refusal

### B6-V13: Deterministic Wire Namespace/Direction Gate
Ten assertions + 12 negative-test families:
- Old IDs retain meanings; new IDs cannot collide
- Envelope magic cannot be confused
- Unsupported versions refuse
- Incorrect sender/receiver roles refuse
- Malformed/oversized frames refuse
- Wrong execution/session/generation refuses
- Existing sequence rules enforced
- Bootstrap v1 does not qualify as B6 v2
- No new recovery RELEASE authority

---

## J. Preserved Prior Decisions

**B1–B5 semantics and all accepted B6 architecture decisions preserved unchanged:**
- Root-owned immutable installation architecture (HD-1–HD-5)
- One physical RELEASE attempt (owner only)
- C1–C4, R1-F03 Option A
- Independent native stop and controller-loss recovery
- HELD state, protected capture, journal uncertainty
- B5 reconciliation with sealed monotonic clock

**HD-1–HD-5 Unchanged:**
- HD-1: Restricted privileged installation in disposable WSL2
- HD-2: Minimal passive public descriptor v2
- HD-3: WSL2 x86-64 experimental qualification only
- HD-4: Immutable generation retention
- HD-5: Conditional invariant globalization

---

## K. Scope Limits

**This acceptance covers the wire contract only.** It does not authorize:
- Privileged installation execution
- Native Linux-host qualification (remains B5-L01/B7)
- Arm64 support
- Broader public API
- Automatic installation cleanup
- Production restricted execution (`RestrictedExecutionRuntime.Create()`)
- Hufu integration
- VM/HCS work
- Any B7 work

---

## L. Next Authorized Work

Preparation of separate **`B6-IMPL-01`** implementation commission.

The implementation handoff identifies:
- Accepted B6 design + Q2 corrections + wire-Q1 acceptance
- HD-1–HD-5
- All implementation obligations WI-01 through WI-06
- Proposed implementation phases (see `FINAL-HANDOFF.md`)

---

## M. Final Owner Disposition

**Source-bound wire contract accepted with qualifications.**

This document is a dated addendum to `docs/decisions/rc2b-06-b6-spec-owner-acceptance.md`. The original owner decision and historical wire-delta evidence are not rewritten.

**Disposition:** **B6 WIRE ACCEPTED WITH QUALIFICATIONS — READY FOR IMPLEMENTATION COMMISSION**

All required independent evidence preserved, corrected protocol recorded as accepted B6 addendum, qualifications explicit, no material contradiction remains.