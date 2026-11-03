# B6-S3 source map and B2 owner-channel contract conflict

**S3 BLOCKED BY ACCEPTED CONTRACT CONFLICT.**

2026-11-03 · GAGAMBA-RC2B-06-B6-IMPL-01-S3. The user selected **Record the conflict and stop** after reviewing the B2 owner-echo transport question. This report preserves the inspected baseline and the required bounded wire decision.

## A. Source and environment

Canonical `C:\Users\Laszlos\source\repos\Gagamba`, `main`, origin `https://github.com/jenolaszlo-sketch/gagamba.git`. Starting commit `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6`, tree `edc0100214e480185cb9b89faf201f540c8aadb7`. The enclosing documentation commit identifies the final report revision. Source at that commit is identical to the starting implementation. Windows NT 10.0.26200.0, x64, .NET SDK 10.0.401. Inspection and evidence-integrity checks ran on Windows. The tracked tree and index were clean at entry. Live and tracking `origin/main` both resolved to `401e1ea7f0be06b27bc188c827dd951016b3c62a`, eight commits behind the starting HEAD.

All 71 Q1 files were rechecked against original reviewer bytes and committed blobs. The 14-artifact candidate and its per-file hashes were rechecked. The original 44 untracked file hashes and 74 worktree registrations agree with the preceding acceptance inventory. [Source excerpts and Git blob identities](rc2b-06-b6-impl-01-s3/source-map.json)

## B. Accepted authority and conflict

The [wire decision](../decisions/rc2b-06-b6-wire-owner-acceptance.md), particularly WI-03, correctly identifies B2 mapping as an implementation obligation. The [wire delta](rc2b-06-b6-spec-wire-delta-01.md) assigns owner echo 36 to owner→controller and recovery echoes 9/10 to their separate namespace. The S3 commission additionally requires the B2 echo after `B2LifelineReady`, controller validation against independently expected identity, and refusal of owner echo 36 on recovery-only channels. These requirements preserve separate authentication and sequence domains.

The concrete B2 source creates the owner socketpair inside recovery. Recovery retains one end and passes the other to its child. The controller holds only the controller↔recovery channel. The B2 owner sends `B2LifelineReady` to recovery. Recovery then sends a recovery-envelope `Ready` snapshot to the controller. The owner-control loop accepts `B2StopObserved`. No owner endpoint, authenticated direct session or owner-echo relay is delivered to the B2 controller.

The accepted requirement that **the controller validate owner echo 36 on the inherited B2 owner channel** therefore lacks a controller endpoint in this topology. Forwarding raw owner frames onto the existing recovery control channel conflicts with its required envelope/direction gate. Recasting the owner echo as recovery request 9 conflicts with its prescribed role 0 and recovery-closure fields. Treating recovery verification alone as controller verification weakens the requested independent recipient check.

This is an unresolved transport/authentication decision for the qualified B2 extension. Legacy B2 remains supported within its accepted boundary. The finding does not establish a defect in its existing lifecycle behavior. The user directed a stop before selecting a new route.

## C–H. Implementation map

| Required integration | Actual entry and planned gate |
|---|---|
| Controller expected identity | `InstalledFileSystem.VerifyActive` and protected manifest/receipt-selected record. The returned trusted state currently exposes digest and qualification ID. S3 must derive the full identity from the same verified generation. |
| Recovery bootstrap | `RecoveryConfiguration.Decode/Validate` in `Bootstrap/RecoveryProtocol.cs`. Currently version 1 only. `IntegratedRecoverySpecification` has no installation field. |
| Recovery send and controller validation | Insert after Associate authentication in `RecoveryHost.RunCore`, before B2 Ready or entry into integrated preparation. `RecoveryConnection.Start` must validate request 9 and send response 10 before accepting Ready/Rendezvous. |
| B2 owner | `B2OwnerLifeline.Run` sends B2LifelineReady on the recovery-owned socket. Owner closure verification and echo must follow that event. The controller recipient/transport requires the decision above. |
| Integrated owner provenance | `DirectAssociation` and `DirectBootstrapPacket` under `OwnerDirectConnection.Accept` and `DirectSession.Establish`. Process identity currently has no artifact identity. A bounded authenticated extension is required. |
| Integrated owner echo | `OwnerPreparationTransaction` obtains the direct channel before StagedObjects. Its controller counterpart `IntegratedB3Client.Prepare` currently receives StagedObjects first. Both need the qualified echo gate. |
| Existing refusal and stop | `RecoveryObligation`, recovery Stop and pidfd monitoring, `OwnerSafetyMonitor` and `M1RecoveryScope`. Existing original deadlines, Unknown cleanup and owner-only RELEASE remain governing requirements. |
| Wire codecs | `RecoveryWire`, `OwnerProtocol` and existing sequencers. IDs 9/10/36, message-specific shape/direction rules and golden bytes remain to be implemented. |

WI-01 through WI-06 remain open for S3. Checkpoint S3-A has source mapping only. S3-B through S3-E have no implementation completion evidence.

## I–L. Verification and authority limits

No S3 codec, bootstrap, role-closure checker or handshake was implemented. B6-V13, B6-T20b, closure mutations and production-binary qualification were not executed for S3. Existing regression totals remain historical S2 evidence. The current source inventory is a static observation, with source blob identities and exact excerpts available in the linked JSON. It makes no native handshake claim.

The evidence checks read the preserved manifest, compared every original/canonical/committed file, inspected the original candidate archive without extraction, and compared the untracked hash inventory and worktree registrations. Their exit code was 0. Documentation preparation and committed whitespace checks validate this report only. The final handoff supplies the enclosing commit/tree and post-commit preservation result.

## M–N. Host and deferred gates

The accepted S2 observation of a WSL2 `0:0 0777` root remains a native qualification blocker. This commission performed no new Linux qualification run. Root-owned installation, native loader/runtime closure, passive descriptor v2, full pre-RELEASE generation enforcement and production activation remain separately gated. The runtime factory remains unavailable.

## O. Required decision and resumption

A focused wire amendment must select and specify the B2 owner→controller route after B2LifelineReady. The two proposed choices were an explicitly authenticated direct B2 owner/controller channel with bounded non-release descriptor passing, or a specified recovery forwarding contract.

The decision must state the recipient, expected-identity provenance, envelope and role rules, per-channel sequence ownership, timeout, failure/stop behavior and descriptor lifetime. It must preserve recovery’s independent lifeline, one physical RELEASE attempt, original deadlines and recovery’s lack of RELEASE capability. The owner echo itself remains identity evidence with zero transferred authority.

After that decision, resume S3-A through S3-E under the original commission, then produce source-bound golden vectors, handshake and loaded-closure probes, applicable regressions and production/test separation evidence. Independent S3-Q1 remains required after implementation. The present report is a blocker handoff.
