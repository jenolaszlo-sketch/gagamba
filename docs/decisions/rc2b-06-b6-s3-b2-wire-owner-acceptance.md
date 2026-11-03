# Owner acceptance of the authenticated B2 installation-evidence channel

**B2 WIRE DELTA ACCEPTED WITH QUALIFICATIONS — S3 READY TO RESUME**

2026-11-03 · GAGAMBA-RC2B-06-B6-S3-B2-WIRE-OWNER-01 · Parent commission GAGAMBA-RC2B-06-B6-IMPL-01-S3

## Decision and source binding

**S3 B2 WIRE CONTRACT RESOLVED BY ACCEPTED OWNER AMENDMENT.** The controller in the existing B2 topology has no endpoint on the owner lifeline. Recovery owns the socketpair, receives 24 and reports its own Ready. The owner accepts the reviewed amendment in full as an additive change to WI-03 and the non-integrated v2 bootstrap placement. For qualified non-integrated B2, the prior mapping of 36 to the inherited B2 socketpair is replaced by the direct connection below. The earlier stop was correct for its revision. [Historical blocker and source map](../evidence/rc2b-06-b6-impl-01-s3.md) · [Prior wire decision](rc2b-06-b6-wire-owner-acceptance.md) · [Original wire delta](../evidence/rc2b-06-b6-spec-wire-delta-01.md)

Canonical checkout on Windows is `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`, origin `https://github.com/jenolaszlo-sketch/gagamba.git`. Each row is the sole parent of the next.

| Revision | Commit | Tree |
|---|---|---|
| Accepted S2 | `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6` | `edc0100214e480185cb9b89faf201f540c8aadb7` |
| S3 blocker and source inspected by reviewer | `ccc7e0b316d5ad5d9da645108798aee28cbc8fc1` | `ff96f4753dc4a08d83827b7bf05fd567d8bfad0f` |
| Amendment, matrix and independent review accepted here | `92d9ef70ca22b49db1c1e393349d6c7210ba6a8e` | `7360a0dec8234611095aee35a12ed204f7a650f9` |

The two post-S2 commits each changed five documentation/evidence paths. All six source blobs and ten excerpts in the blocker map match the amendment revision. At entry, live and tracking `origin/main` both resolved to `401e1ea7f0be06b27bc188c827dd951016b3c62a`, ten commits behind local main. The 44 untracked files matched the preceding path-and-content inventory. All 74 worktree registrations matched after accounting for the canonical branch's new tip.

| Accepted text | SHA-256 of committed bytes, verified against the prior delivery |
|---|---|
| [Normative amendment](../design/rc2b-06-b6-s3-b2-wire-delta-01.md) | `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6` |
| [Evidence and qualification matrix](../evidence/rc2b-06-b6-s3-b2-wire-delta-01.md) | `6c7daf1d3763da234b65cdf44451fff94316c8c254bafc6164b59bbb0fca0fbc` |
| [Independent reviewer report](../reviews/rc2b-06-b6-s3-b2-wire-delta-01-independent-review.md) | `23405a8f61cb5c52867d4f72199d027ee364ab68a39768b6aec14b900cd4f01f` |

## Architectural decisions

- **B2-HD-01 · Approve with qualifications.** On a fresh controller-generated session, C creates a single-use abstract AF_UNIX SOCK_SEQPACKET listener before spawning R. O connects after its original lifeline exchange. On the dedicated connection C sends only the expected installation identity and O sends only its checked installation echo. The address grants no authority. A wrong first connector terminates the attempt.
- **B2-HD-02 · Accept the original verified launcher within the trusted host boundary.** The dedicated B2 evidence channel authenticates the original owner process under the accepted trusted-launcher assumption. It does not claim protection against a malicious trusted launcher, host administrator or compromised kernel. A verified installed launcher, protected executable and pinned runtime select the original child. Pidfds and kernel credentials establish process identity. S2 and checks on the files and runtime actually loaded establish binary integrity. Both proofs are required. [B6 threat model](../design/rc2b-06-b6-installed-artifact-trust.md) · [Specification owner decision](rc2b-06-b6-spec-owner-acceptance.md)
- **B2-HD-03 · Exclude qualified delegated-parent B2.** That branch enters retained-domain preparation without 24. Refuse the qualified request before opening the parent, journal or domain and before allocating authority. Synthesized 24, weaker readiness and alternate qualification are forbidden. A future extension requires its own topology design and qualification decision. Both existing v1 branches retain their unqualified behavior. [B2 owner decision](rc2b-02-b2-owner-acceptance.md)
- **B2-HD-04 · Reject recovery forwarding.** R knows the bootstrap secret, digests and sequences. Its credentials authenticate R. Neither 9, an assertion that R saw 36, copied bytes nor an inner envelope proves the writer was O. Recovery type 13 is forbidden. A handed-down socketpair with both actual writers authenticated is viable but unselected. It would add intermediate endpoint aliases, transfers and disposal obligations. S3 implements only the direct connection.

## Process and installation identity

C pins its verified R child before bootstrap. R pins the original C using the private inherited control and credential sockets, parent checks and one-shot Associate. R, the subreaper and sole reaper of O, binds its one authorized O spawn, returned Process instance, live safe handle, private lifeline, execution, lease and session. Receipt of 24 on that exclusive lifeline after pinning closes the post-spawn identification gap under the trusted-launcher premise. R exports only its retained references. Caller-selected handles, reopening a packet PID, pin replacement and cross-generation adoption refuse.

C and O each compare the actual SO_PEERPIDFD against the opposite original pin, verify SO_PEERCRED, expected non-root UID/GID, three distinct live PIDs, the O→R→C parent chain, trusted procfs and common PID/user/time namespaces. Bracket identity reads with live polls. Both peers must lack credential-forging capabilities. Enable SO_PASSCRED before data as specified. On **each actual 37 and 36**, require exactly one kernel-validated SCM_CREDENTIALS of 12 bytes matching the pinned sender, zero rights and no unknown or truncated ancillary data. A matching claimed PID, connected socket or earlier credential check cannot satisfy either receive.

C derives the typed expectation in the same successful argument-free S2 VerifyActive pass over the fixed root, 14-file generation, manifest, receipt-selected qualification record, host and final selector. Preserve the qualification ID. Generation is the raw artifact-set digest. Derive each role closure exactly as amendment §2 specifies using the domain prefix, role byte and five ordered raw file hashes. C verifies its loaded provider. R and O independently verify the installed selection and their actual loaded role assemblies, pinned file identity, deps/runtimeconfig, host, framework and loader before echoing. C checks the expected role hash, set and protocol fields and revalidates the same S2 result before 12 and qualified return. S2 manifest hashing, installer, immutable generation retention and F02's separate administrative publication approval still govern. [S2 acceptance](rc2b-06-b6-impl-01-s2-owner-acceptance.md) · [F02 owner decision](rc2b-06-b6-s2-f02-trust-store-owner-decision.md)

## Wire and readiness contract

Use the byte layouts and validation rules in §§1–4 of the hash-bound amendment. Non-integrated v2 uses B2InstallationIdentity and B2DeadlineNs on the credential fd with Integrated and DelegatedParent null. Retain the 8192-byte bootstrap bound, strict shape and owner selector `--b2-lifeline 2`. M1OP has a 48-byte little-endian v1 header bound to execution GUID, lease-generation GUID, sequence and length. RecoveryWire XORs octet 0 with 0x20. B2 uses no GID1 wrapper. New binary payloads have a 512-byte ceiling and exact narrower shapes. Select the decoder by channel, topology, version, direction, phase and descriptor role.

| Message and direction | Authorized channel, payload and rights |
|---|---|
| Recovery 9 InstallationEchoRequest, R→C / 10 InstallationEchoResponse, C→R | Original control following Associate 1. Role-0 echo of 76 bytes, then 36-byte result/reason/reserved/digest. Exact successful 10 precedes O spawn. Zero rights. |
| Owner 23 B2StartLifeline, R→O / 24 B2LifelineReady, O→R | Private original lifeline. Existing start JSON and empty 24. Zero rights. R validates 24 before either reference transfer. |
| Owner 38 B2EchoBootstrap, R→O | Lifeline. Session GUID, three i32 C/R/O PIDs, zero u32, i64 absolute deadline, exactly 40 bytes. One original C pidfd. |
| Recovery 11 B2OwnerBound, R→C | Original control. Same 40-byte binding, one original O spawn pidfd. |
| Owner 37 B2OwnerExpectation, C→O | Authenticated evidence connection only. Session GUID, six raw SHA-256 fields, four u16 constants, Qualified=1 and two length-prefixed exact ASCII profile strings. Exactly 219 plus both string lengths, maximum 347 bytes. Zero rights. |
| Owner 36 OwnerInstallationEcho, O→C | Same connection. Exactly 76 bytes, artifact set, computed O closure, three u16 protocols, role 1, Qualified=1 and four zero bytes. Zero rights. |
| Recovery 12 B2OwnerEchoAccepted, C→R | Original control. Exactly 48 bytes, session GUID and original expected artifact-set digest. Positive only, zero rights. |
| Recovery 2 Ready, R→C | Original control. Existing snapshot JSON, at most 4096 bytes, zero rights. |

Successful type.sequence traces are C→R **1.1, 10.2, 12.3**, R→C **9.1, 11.2, 2.3**, R→O **23.1, 38.2**, O→R **24.1**, C→O **37.1**, O→C **36.1**. Each direction on each socket owns its counter. After valid 24, R sends 38 then 11. O may queue its connection after validating 38. C must validate 11 and the actual peer before 37. O authenticates C, consumes 37 and its clean write-half shutdown, verifies its loaded closure, sends 36 and half-closes. C checks credentials, binding, bytes, live pins and complete drain before sending 12. Distinguish an empty or surplus packet from drained peer shutdown. R compares the session and digest in 12 before Ready. No retry or resynchronization.

R sends Ready once under its stop latch, after servicing pending loss and checking pins and deadline. Publish the diagnostic marker only after successful send. Require the exact snapshot property set, pinned PIDs, R's setsid PID as SessionId, Subreaper true, StopCount and RetainedDomains zero, null Cause and StopDeadline, NativeStop “Unproven” and every other boolean false. C separately revalidates S2 and checks pending stop, both live pins and deadline at its one-time local commit. A lost 12 prevents R Ready. A lost Ready prevents C-qualified return. Death after the final live observation is handled by the existing stop obligation.

## Custody, refusal and compatibility

R retains both original pidfds. Only 38 and 11 duplicate one authorized borrowed pin each under the custody lock. Receive with MSG_CMSG_CLOEXEC, enforce exactly one SCM_RIGHTS int, fd greater than 2, CLOEXEC, kernel pidfd type, role and liveness before detaching once. Dispose every installed right on every error, including empty or truncated records. Unconsumed queued copies die with the receiver socket. C keeps both original-helper pins through its final S2 and Ready gate. O keeps its independent R stop reference. Release temporary pins and sockets at last use or failure. Only intended child ends may cross exec, with spawn-local mapping and atomic CLOEXEC. Evidence sockets are never handed down.

Set one absolute monotonic D before R spawn after S2, bounded by the earlier of 7000 ms and any earlier applicable deadline. Keep the 5000 ms echo cap, 5000 ms bootstrap/Associate and 3000 ms wait for 24, and the original 30/35 s lifetimes, once-only two-second stop and B5 C3 durable deadline. Stop preempts success. R and O service the original lifeline and pidfds independently of bounded verification workers. Never hold the stop lock across hashing or a blocking operation.

Wrong or missing identity, peer, credential, fd, execution/session/lease, role, set or closure, malformed/oversize/cross-channel frames, replay, duplicate, deadline, transfer failure, missing confirmation, death, observed stop or loss terminates the positive path. No qualified progression, domain, staging, HELD or RELEASE follows refusal. R detects C loss and sends 25 or closes the lifeline. O observes stop/EOF or R death and follows 26/exit policy. R reaps the original O. C must leave detached R able to stop/reap once O could exist. Preserve first cause and deadline, never retry with another owner or generation, and retain Unknown for unproved exit/cleanup.

The B2 evidence channel has no lifecycle dispatch or writer. Recovery retains stop/cleanup only. Where RELEASE exists, only O may make one physical attempt. No disk record reconstructs authority. Integrated B3 retains DirectAssociation, DirectBootstrap and authenticated DirectSession with 9/10 before Rendezvous 8 and GID1 owner echo 36 before StagedObjects. The B2-only additions are forbidden there. Legacy B2 v1 readiness and lifeline service remain in their accepted scope.

## Independent review and S3 resumption

The report identifies GPT-6.1 Terra (high) as independent reviewer and GPT-6 Astra (high) as design participant. Its two reviewed hashes match the committed inputs. The reviewer closed the initially implicit Ready predicate after the exact snapshot fields, distinct session identities and separate R-send/C-return gates were specified. The post-spawn pin, substitute-child and synchronous-hash challenges produced the exclusive lifeline, trusted-launcher and independent stop obligations above. The owner adopts these qualifications. The original report remains the reviewer's record.

| Review question | Disposition carried into implementation |
|---|---|
| 1. Original O | Pass within trusted spawn custody, live peer and actual sender checks. |
| 2. R substitution | Pass against R sending or forwarding. Launcher honesty is a premise. |
| 3. Lifeline | Pass as designed. Demonstrate continuous 23–26 service during stalled verification. |
| 4. Direction and sequence | Pass with separate role, phase, envelope and counter checks. |
| 5. Descriptor lifetime | Pass as designed. Prove custody, exclusive inheritance, CLOEXEC, credential and ancillary handling, and drained shutdown on the kernel. |
| 6. RELEASE | Pass. No new operation, writer or second attempt. |
| 7. Failure | Pass as designed. Preserve stop once, R survival, local liveness limits and Unknown. |
| 8. Legacy B2 | Pass as designed. Exercise both v1 branches and early qualified delegated refusal. |
| 9. Integrated B3 | Pass as designed. Preserve separate B3 regression and qualified integration gates. |
| 10. Implementability | Pass. The reviewed bytes settle the wire choice. Loaded closure and OS behavior still require proof. |

Resume **GAGAMBA-RC2B-06-B6-IMPL-01-S3** at checkpoints S3-A–E under WI-01–06. Start from the blocker source map and the amendment's component-to-test matrix. Carry the verified S2 projection through production bootstrap and role checks, implement strict per-channel codecs and golden vectors, then the bounded barrier and stop handling. Preserve production/test separation and exercise B6-V13, B6-T20b and B1–B5 regressions with positive controls. Deliver source-bound results and outstanding prerequisites to independent S3-Q1.

**Evidence at decision time.** The architectural evidence is static source inspection and independent design review. All 28 adversarial cases are specifications. Zero were executed for this amendment or acceptance. S3 implementation and S3-Q1 are outstanding. The recorded WSL2 root is owned by 0:0 with mode 0777 and lacks a Qualified installation. Root-owned installation, real process/credential/descriptor behavior, native loader closure and production topology need separate qualification. This commission authorizes documentation only. Privileged execution, host permission changes, production activation, publication, S4/B7 and push are outside it. The original S3 implementation commission may resume separately.
