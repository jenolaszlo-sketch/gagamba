# RC2B-03-INT-01-D01 owner architecture disposition

Commission: **GAGAMBA-RC2B-03-INT-01-D01-OWNER-01**.
Date: 2026-10-30 (Asia/Manila). Documentation-only owner decision.

## 1. Owner decision and authority

**APPROVE ALTERNATIVE B FOR BOUNDED EXPERIMENTAL IMPLEMENTATION WITH QUALIFICATIONS.**

**Alternative A: DECLINED AS PROPOSED.** No RC-1 process-ownership amendment is
approved or required for selected Alternative B. The architectural question is
resolved for experimental implementation; the next task is to build and qualify B,
not reconsider A.

The owner's explicit instruction in this conversation, titled
GAGAMBA-RC2B-03-INT-01-D01-OWNER-01, supplies this decision and the bounded continuation
authorization. This is conversation-based owner authority, not a digital signature
or independent authentication of a human identity. The [Q1 architecture review](../reviews/rc2b-03-int-01-d01-independent-review.md)
is SUPPORTED WITH QUALIFICATIONS and recommends B. Its disclosed provenance remains:
the same conversation produced the gate and critical reassessment; no different
human, model or separately isolated reviewer is claimed.

This records an implementation direction, not a completed or qualified security
boundary, acceptance of security claims, or implementation of the architecture.

## 2. Verified Git and source binding

Repository: `jenolaszlo-sketch/gagamba`; canonical checkout directly on `main`.
Local HEAD, tracking `origin/main` and live remote `main` matched the starting commit.
No tracked working or staged changes were present; existing untracked files are preserved.

| Item | Verified identity |
|---|---|
| Starting main / Q1 report commit | `c2df34de03e2474622aa2614e42f9089cc45b962` |
| Review delivery root tree | `b1817487d2499874a8a1e522d59e23b17836b6ca` |
| Architecture gate / Q1 direct parent | `adc7f45479f9c6eb6766cee0a5108f1c0dbc27ac` |
| Architecture gate root tree | `d5e369fc229f2ed5f392803ff97f6b081c9b1243` |
| Original implementation baseline / gate direct parent | `98d46f308d27d7ac3fd95079d6bc5b5d9bad482d` |
| Baseline root tree | `32669d928723fc305a1eda808e855de0a62b2683` |
| [D01 gate record](../evidence/rc2b-03-int-01.md) blob | `76aa6a8d0868416c284a702d9a598a416589ab28` |
| Q1 report blob | `157eb6982e68931c253cec17105cf30818081888` |
| [RC-1 design](../design/restricted-consumer-contract-rc1.md) blob | `4b300b01397e7f68d978fadbc36164dda17c8721` |
| [RC-1 owner acceptance](restricted-consumer-rc1-owner-acceptance.md) blob | `9e7576d294fa7477b7206e752a3981a1d794bb4a` |
| [B2 owner acceptance](rc2b-02-b2-owner-acceptance.md) blob | `3056254758f56d8a1cb99dd9b03f08e6e05fa689` |
| [B3 owner acceptance](rc2b-03-b3-owner-acceptance.md) blob | `0061fa3d64a3198a599fdc5b2c926576248bf266` |
| [Accepted C1–C4 agreements](gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md) blob | `a0d1ad409b2d7fb0985abf7db0b0c5257cc6425e` |

Verified linear ancestry: baseline -> gate -> Q1 review; each successor has its
predecessor as sole parent. Gate/review changes are documentation only. The protected
trees below are identical at all three commits and must remain identical here.

| Protected tree | Git identity |
|---|---|
| src | `c28e8dc345b05549c45ea93be1958aa88141b1da` |
| tests | `ad6f2796ca81f489d8daa9c89f69a44fd68228e5` |
| native | `85631d2f04512376621d3d09f51d67fd90f45335` |
| eng | `0a6dba6709001bbd049595c1200ec9ee471f2b1d` |
| .github | `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` |

The final decision commit/root tree and remote receipt are returned in the completion
handoff, avoiding a self-referential commit identity. No new implementation identity exists.

## 3. Architecture disposition

A controller-parented owner with sibling recovery is not equivalent to RC-1.
A sibling subreaper cannot reap the owner's process merely because it retains a
pidfd. Observing termination is not reaping, collecting exit state or acquiring
the journal-writer fence. Do not authorize A's proposed parentage amendment.
The original D01 gate and Q1 review remain unchanged historical records.

B preserves the accepted RC-1 topology:

- Independent recovery spawns the owner and retains the owner pidfd and verified
  subreaper/reaping responsibility.
- Recovery retains native domain and staging recovery authority.
- Owner retains the live preparation capability, journal writer and sole RELEASE writer.
- Controller connects directly to an owner-created authenticated release endpoint.
- Recovery never receives a release secret, connected release endpoint or release capability.

## 4. Required direct authentication

Implement the reviewed Unix-domain **SOCK_STREAM** construction (Q1 §2), including:

1. An owner-created protected listener, fresh trusted directory and verified endpoint
   creation; reject symlinks, stale/substituted paths and invalid bounds.
2. A fresh direct controller-to-owner connection with mutual exact expected PID/UID/GID
   checks in the documented PID/user namespaces.
3. `SO_PEERPIDFD`-backed live peer identity, feature-tested and fail-closed; compare
   retained live expected identities from the trusted bootstrap, without PID/path reconstruction.
4. Immutable original execution, generation and session binding, derived from trusted
   pinned experimental bootstrap identities. Recovery transfers only explicit non-release
   identity/lifetime evidence, never a release bearer.
5. After mutual peer checks, fresh direct CSPRNG challenges, a one-use direct secret
   and cryptographic confirmation of a versioned transcript binding both roles,
   identities, execution/generation and challenges (reviewed HMAC-SHA-256 confirmation).
   No release secret in recovery, argv, environment, files, journals or telemetry.
6. Strict bounded framing and sequencing: validate length before allocation, handle
   partial reads and coalesced messages correctly, reject ancillary descriptors and
   truncation, bound attempts and total handshake time against original deadlines.
7. One original non-resumable release session; close/unlink the listener after association.
   Missing authentication ACK, peer/session loss, replay or second association cannot
   restore authority. No resumed release authority after session loss.
8. Atomic CLOEXEC socket/pidfd creation and explicit prevention of fork inheritance
   and descriptor transfer to recovery, coordinator or target. CLOEXEC alone is not
   fork isolation. No release descriptor or secret passes through recovery.

An endpoint pathname, matching UID or known numeric PID is insufficient authorization.
No privileged fallback, PID/path identity reconstruction or hardcoded test secret is
permitted. Authentication failure refuses the affected experiment.

## 5. Independent recovery and safety monitoring

The real B3 transaction must run under the independently surviving B2 recovery process.
Recovery must retain actual native handles, including the exact validated domain/staging
authority and owner/controller lifetime evidence. Preserve real authority-retention
barriers and independent retention ACKs before mint/publication of releasable preparation.
Recovery's ACK is not controller release authorization; the owner's writer fence,
capability and RELEASE writer remain owner-local.

Keep recovery safety-stop authority available throughout preparation, authority
retention, HELD, release arbitration, post-release observation/drain, owner failure
and cleanup. Do not block its safety monitor behind synchronous preparation, owner
channel exchanges, capture reads or controller callbacks. Independently monitor actual
recovery lifeline and original controller association in the owner during all phases.
Observed loss is terminal and must feed the existing stop arbiter and native stop path.
Recovery's native safety stop must remain available even if owner IPC is wedged.

## 6. Death/release race qualification

Preserve C1–C4's accepted owner-local ordering and the existing release arbiter:

- Loss already accepted by the owner stop arbiter prevents a later release.
- Loss observed before the final release-prerequisite check prevents release.
- Release already committed at the accepted owner arbitration point retains its
  original decision even if another process subsequently dies.
- Recovery/owner safety-stop obligations remain effective after that race; preserve
  separate delivery, execution, stop and cleanup facts and uncertainty.
- No second RELEASE or reconstructed authority is permitted.

A process-death notification or pidfd poll is not atomic with physical RELEASE.
**Do not claim that every physical RELEASE after another process's wall-clock death
is impossible.** That stronger guarantee requires a separately reviewed enforceable
fence. This authorization approves no stronger global death/write ordering claim.
A recovery stop send is not an accepted owner stop until the owner arbitration point.

## 7. Experimental trust boundary

Full B6 production installation attestation is not required to begin this bounded
implementation. Experiments nevertheless require exact pinned recovery/owner/runtime/
native artifacts, protected bootstrap inputs and closure, rejection of missing or
substituted dependencies, kernel feature verification, immutable per-operation expected
identities, unprivileged execution and already authorized delegated cgroup resources.
No public provider activation is permitted. If prerequisites cannot be met, refuse the
affected experiment rather than weaken authentication or provision privileged fallbacks.

The caller-populated B3 profile is not production attestation. Q1's trusted-bootstrap
and RC-1 threat assumptions remain; this does not establish Byzantine-bootstrap
attestation or same-UID hostile-process isolation. Historical absent-delegation/root
observations are not new environment measurements or authorization to alter the host.

## 8. Finding dispositions

| Finding | Owner disposition / remaining requirement |
|---|---|
| Q1-F01 MAJOR | A lacks required reaping semantics. **DECLINED AS PROPOSED**; no RC-1 parentage amendment. Historical finding preserved. |
| Q1-F02 MINOR | Accept the distinction between experimental authentication and complete B6 production attestation, subject to §7. |
| Q1-F03 MAJOR | Actual direct controller/owner authentication remains unimplemented; required before B can be qualified. |
| Q1-F04 MAJOR | Actual recovery-liveness monitoring and death/release race limits require implementation and qualification under §§5–6. |
| Q1-F05 MAJOR | Safety monitoring must remain independent of blocking preparation and observation; implementation/qualification required. |
| Q1-F06 OBSERVATION | B3 F-08 exec-confirmation descriptor defect remains open; correct/qualify before affected FD 0–2 claims. |
| B3 F-01 MAJOR | **OPEN, ACCEPTED DEFERRED INTEGRATION BLOCKER**; not waived or closed by architecture approval. Genuine integrated process tests and independent review required before any closure claim; owner acceptance remains required. |
| B3 F-02 / F-03 / F-07 / F-08 | Unresolved: actual recovery lifeline, correlation diagnostic, topology-separation test clarification and exec-confirmation FD/oracle correction remain commissioned follow-ups. |
| B3 F-04–F-06 | Prior experimental-pin, coordinator-mediated-fact and production-descriptor/B6 dispositions unchanged. |
| M-01 | **CLOSED**, unchanged. |
| R1-F02 | Globally **OPEN**, unchanged. |

B2/B3 and RC-1 acceptances, C1–C4 and approved R1-F03 **Option A** remain preserved.
R1-F03 Option A is separate from the declined architecture Alternative A.

## 9. Authorized next milestone and stop boundary

**GAGAMBA-RC2B-03-INT-01-B-IMPL-01**

Status: **AUTHORIZED FOR BOUNDED IMPLEMENTATION, NOT STARTED.**

The separate bounded continuation may integrate actual recovery-parented B3 execution,
implement direct authenticated controller/owner communication and live owner/recovery/
controller monitoring, preserve actual authority-retention barriers, test actual
controller/recovery/owner failures, correct F-02/F-03/F-07/F-08 as previously commissioned,
and produce source-bound evidence for independent review. Preserve C1–C4, Option A,
original deadlines, no authority reconstruction and at-most-one physical RELEASE.

Carry the Q1 §6 genuine integrated process/authentication/retention/failure matrix,
stream fragmentation/coalescing/oversize and replay negatives, blocked-observer safety
stop, FD inventories/exec oracle, stop-before-mint and pre-write/release-first races,
owner-live fence refusal, post-exit takeover and no-resend evidence. Observe actual
parentage, pidfds, retained authority, stop, domain population, journal transitions,
RELEASE count, target effects and reaping separately. Mocks, skips, empty domains and
primitive feasibility probes cannot substitute for mandatory native process evidence.
No F-01 CLOSED claim precedes genuine integrated process tests and independent review.

No B4, B5, B6, B7, Hufu or production activation is authorized by this decision.
Production `RestrictedExecutionRuntime.Create()` remains unavailable. No independent
native Linux-host qualification, hostile populated-domain stop, complete cleanup,
sealed C3 or installed production boundary is accepted here.

## 10. Documentation delivery and validation

Only this decision and current pointers in [PROJECT-STATUS](../PROJECT-STATUS.md),
[handoff](../handoff.md) and [work queue](../work-queue.md) change. Original D01 evidence,
Q1 report, all prior acceptances and all implementation files remain unchanged.
No implementation, build, native execution or process qualification runs in this task.

Run `eng/verify-preparation.ps1`, `git diff --check` and the staged whitespace check;
verify exact four-file documentation-only scope and unchanged protected identities.
Actual check results and final commit/tree/remote binding are returned in the completion
handoff. Commit and push directly to canonical `main`, without branches, worktrees or
force pushes. **Stop after this decision delivery; do not start B-IMPL-01 in this task.**
