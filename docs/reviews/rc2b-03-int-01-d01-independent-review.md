# RC2B-03-INT-01-D01-Q1: Process ownership and authentication architecture review

Date: 2026-10-30 (Asia/Manila).
Commission: GAGAMBA-RC2B-03-INT-01-D01-Q1.
Scope: architecture security review only; no implementation or contract approval.
**Verdict: SUPPORTED WITH QUALIFICATIONS. Recommendation: B.**

Preserve RC-1's recovery-parented owner and add a separately established, mutually
authenticated direct controller-to-owner association. A direct Unix connection need
not pass either connected endpoint or an authentication secret through recovery.
Full B6 production installation attestation is not inherently required to implement
this bounded experimental design. Exact experimental artifacts, trusted bootstrap,
live peer identity and process-loss qualification are required.

**Alternative A is not approvable as proposed: it is not equivalent to RC-1.** Its
unresolved sibling-owner reaping responsibility is real. It is also avoidable by B.
Do not approve A's ownership amendment on the assumption that B is impossible until
all B6 work is complete. This report recommends a design; it does not authorize its
implementation, amend RC-1, close F-01 or accept INT-01.

Review provenance: a separately commissioned critical reassessment using source,
primary Linux references and new standalone probes. The same conversation produced
the D01 gate record; no different human, model or separately isolated reviewer is
claimed. This continuity is disclosed for the owner's assessment of review independence.

## 1. Verified source and contract binding

| Item | Verified identity |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting HEAD / live remote main | `adc7f45479f9c6eb6766cee0a5108f1c0dbc27ac` |
| Reviewed documentation root tree | `d5e369fc229f2ed5f392803ff97f6b081c9b1243` |
| Unchanged implementation baseline / direct parent | `98d46f308d27d7ac3fd95079d6bc5b5d9bad482d` |
| Implementation baseline root tree | `32669d928723fc305a1eda808e855de0a62b2683` |
| src / tests trees at both commits | `c28e8dc345b05549c45ea93be1958aa88141b1da` / `ad6f2796ca81f489d8daa9c89f69a44fd68228e5` |
| native / .github / eng trees at both commits | `85631d2f04512376621d3d09f51d67fd90f45335` / `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` / `0a6dba6709001bbd049595c1200ec9ee471f2b1d` |
| [D01 gate record](../evidence/rc2b-03-int-01.md) blob | `76aa6a8d0868416c284a702d9a598a416589ab28` |
| [RC-1 design](../design/restricted-consumer-contract-rc1.md) blob | `4b300b01397e7f68d978fadbc36164dda17c8721` |
| [RC-1 owner decision](../decisions/restricted-consumer-rc1-owner-acceptance.md) blob | `9e7576d294fa7477b7206e752a3981a1d794bb4a` |
| [B1](../decisions/rc2b-01-b1-owner-acceptance.md) / [B2](../decisions/rc2b-02-b2-owner-acceptance.md) / [B3](../decisions/rc2b-03-b3-owner-acceptance.md) decision blobs | `176247d1286fc38f3809776ee93a725ba2149354` / `3056254758f56d8a1cb99dd9b03f08e6e05fa689` / `0061fa3d64a3198a599fdc5b2c926576248bf266` |
| [Accepted C1–C4 agreements](../decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md) blob | `a0d1ad409b2d7fb0985abf7db0b0c5257cc6425e` |

`git show` confirms the gate commit's sole parent and root tree above. Its only changes
are PROJECT-STATUS, handoff, work queue and the D01 evidence file. `git diff 98d46f3
adc7f45 -- src tests native .github eng docs/decisions` is empty. Thus the gate commission
made no implementation change. Existing untracked files remain untouched. The new
report commit/tree and post-push remote verification are returned in the final handoff.

RC-1 §RC-D05 assigns recovery creation/subreaping/retention of the owner, independent
stop authority and post-exit recovery; the owner alone holds its preparation capability,
journal writer and physical RELEASE writer. The original controller generation alone
may request release. Same-UID hostile host code/admins are excluded. B1 approves Option A;
B3 implements it within its fixture scope. B2/B3 acceptance does not qualify their
combined topology. The [B3 independent review](rc2b-03-b3-independent-review.md) §F and
F-01–F-08 remain preserved, including the producer-only native evidence boundary.

## 2. B first: feasible authentication without routing a bearer

The defect is sharing a release-capable endpoint with recovery, not the use of Unix
IPC itself. In B, recovery creates only the stop/preparation lifeline; **the owner
creates its listener after it has been spawned**, and the controller connects directly.
The listener and connected release sockets never exist in recovery's descriptor table
through fork inheritance or SCM_RIGHTS. A rendezvous name may be public to recovery;
it confers no release authority.

Linux provides connection-time PID/UID/GID credentials. Those credentials alone do
not authenticate a holder to which someone later transfers a connected socket. A
fresh connection plus exact expected peers, stable live identity and no FD delegation
is a different construction. The selected design uses AF_UNIX **SOCK_STREAM** with
bounded framing, avoiding assumptions about credential support on other transports.
[Linux unix(7)](https://man7.org/linux/man-pages/man7/unix.7.html).

Use `SO_PEERPIDFD` to obtain the peer's kernel process reference directly, together
with `SO_PEERCRED`; do not open a new pidfd solely from an old numeric peer PID. The
Linux v6.6 implementation gets the socket's retained peer PID and prepares a pidfd
from it. Support must be feature-tested and absence/error must refuse this profile,
not trigger a PID/path fallback. This primitive was exercised on the available 6.6
WSL2 kernel; it is not already wrapped by Gagamba's `PidFd` helper.
[Linux v6.6 socket implementation](https://github.com/torvalds/linux/blob/v6.6/net/core/sock.c#L1673-L1701).

### 2.1 Concrete identity trust basis

Peer credentials identify a process, **not its executable hash**. The starting trust
basis remains the accepted trusted bootstrap and pinned Gagamba components:

- Controller verifies the exact experimental recovery/owner managed closure before
  spawn. B2 already has `PinnedExecutable.Verify`; its configuration records the
  original controller PID/UID/session and verifies the recovery's actual parent.
- Recovery records that controller identity once from its authenticated bootstrap,
  retains its pidfd, spawns exactly one pinned owner and immediately retains the owner
  pidfd. No user-supplied replacement PID, reconnect or session update may overwrite it.
- Recovery transfers **identity/lifetime evidence only**: a duplicate original-controller
  pidfd to the owner and a duplicate live owner pidfd to the controller, on explicit
  non-release bootstrap roles. These must be separate from the seven domain-FD roles.
  A pidfd can support lifecycle/stop operations; it is not a preparation/release bearer.
- The owner seals the expected controller PID/UID, execution and generation from that
  trusted bootstrap. Controller seals the expected owner identity from the verified
  spawn association. Both then independently compare their direct connection's kernel
  peer evidence with the expected retained live identity, in the same documented PID
  and user namespace. A live-reference check before/after identity matching prevents
  accepting a recycled numeric PID; `/proc` names alone supply no authority.

This is **not** Byzantine-bootstrap attestation. If arbitrary compromised recovery code
may lie about the initial process identities, replace pinned code or inspect another
same-UID process's memory, these assumptions do not hold. That stronger threat would
need another trust root/isolation design. It is outside RC-1's stated same-UID hostile
code exclusion, not a reason to give the legitimate recovery role a release bearer.
Recovery remains trusted for authentic bootstrap/lifetime facts and stop, but is never
given a release secret or connected release endpoint. Negative tests must still reject
the recovery process itself as a release peer, despite its matching UID.

### 2.2 Minimal direct association

1. Establish the existing authenticated controller/recovery association with fresh
   execution, generation and one-use B2 session. Verify unprivileged credentials,
   exact experimental closure and independent recovery lifetime before any release.
2. Recovery enables/verifies subreaping, opens the already delegated parent and spawns
   the real transaction owner. Keep all recovery resources outside the target domain.
   Retain exact owner pidfd before continuing preparation; parent exit races refuse.
3. Owner verifies its inherited recovery lifeline and actual recovery parent/pidfd;
   it obtains the sealed original-controller lifetime evidence above. Owner creates a
   private bounded listener in a fresh trusted directory (0700; socket 0600), after
   spawn. Verify directory identity; reject symlinks, stale/preexisting socket and
   path-length overflow. No abstract-name secrecy or UID-only authorization is assumed.
4. Forward only the rendezvous address and bounded identity facts on the B2 control
   channel. Controller connects; each side verifies exact PID/UID/GID, `SO_PEERPIDFD`,
   expected execution/generation and both original live process references. An address
   substitution serviced by recovery/helper fails the expected-owner check; a connection
   from recovery/helper fails the expected-controller check. Unlink/denial of service
   during bootstrap aborts rather than authenticates a substitute.
5. **Only after both peer checks**, exchange fresh CSPRNG challenges and a one-use
   bootstrap secret on that direct socket, confirm a versioned transcript containing
   both roles/identities, execution/generation and challenges (standard HMAC-SHA-256 key
   confirmation is sufficient). The secret is not an alternative to OS identity checks.
   No secret is in argv, environment, filesystem, journal, observer output or recovery.
   Erase it after the one permitted handshake; the connected session is not resumable.
6. Seal one original release-capable session in owner memory. Close/unlink the listener
   after successful association; do not let unauthenticated traffic consume the valid
   slot, but bound attempts and total handshake time. Errors, missing authentication
   ACK or deadline loss fail closed. Use independent per-direction sequences and
   execution/generation checks; replay and second association cannot restore authority.
7. Mark sockets and received pidfds CLOEXEC atomically (`SOCK_CLOEXEC`/`accept4`), and
   explicitly prevent fork inheritance or SCM_RIGHTS transfer to recovery, coordinator
   or target. CLOEXEC alone does not protect a subsequent fork. Bound stream length
   before allocation, tolerate partial reads/coalescing, reject ancillary FDs/truncation
   and enforce original handshake deadlines. Existing seqpacket framing cannot simply
   be applied to stream `recv` calls. No generic broker or new release arbiter is needed.

The challenge is exchanged after identity authentication, so recovery cannot learn it
by accepting a substituted listener or opening its own connection. It cannot forge
the controller's peer PID on an unprivileged direct connection. It cannot replay on a
new connection after session loss. Deliberate descriptor theft via ptrace or
`pidfd_getfd` is not prevented merely by CLOEXEC: the latter uses ptrace permission
checks. This review makes no same-UID hostile-process isolation claim. The actual
Gagamba recovery implementation must contain no release-FD acquisition/delegation path.
[pidfd_getfd(2)](https://man7.org/linux/man-pages/man2/pidfd_getfd.2.html).

### 2.3 Actual recovery ownership and retention

Recovery's scope must run `OwnerPreparationTransaction` for **this B3 execution**, not
the B2 barrier fixture. Keep native domain/staging transfers, identity checks and
retention ACKs on the owner/recovery channel; route controller admission and release
only over the direct channel. Owner awaits independent domain/staging retention and
registered/prepared corroboration before publishing any releasable preparation.

Retain delegated parent, the exact seven domain descriptors (parent/directory/kill/
events/three limit files), staging parent/directory, owner/controller pidfds and the
original stop obligation. Never transfer the owner's writer lock FD, capability or
RELEASE writer. B3's manifest authorization and HELD/results can use the authenticated
direct channel; controller constructs/checks the manifest from real staged descriptions
and verifies source pins. Recovery's retention ACK is not host release authorization.
No controller callback, observer or bulk message may block recovery safety handling.

The current synchronous `StartOwnerPreparation` must not occupy the only recovery
monitor loop. Adapt its staged protocol/worker orchestration with synchronized
retained-resource publication, and keep controller/owner-loss observation and native
stop available throughout bootstrap. Owner must monitor recovery during preparation,
HELD and post-release observation/drain, not only between release requests.

### 2.4 Experimental requirements versus B6

**Before implementation/experiments:** specify and review these bootstrap roles and
wire bounds; pin the exact experimental recovery, owner and dependency closure plus
coordinator/filter/tool/input; preserve immutable per-run expected hashes and refuse
changes/missing artifacts. Use a trusted local source/build area, fixed runtime path/
version/environment, no unqualified assembly resolution or writable-file substitution,
and record host/kernel/namespace/supervisor assumptions. Existing caller-populated pins
are experimental inputs, not installed attestation; current `PinnedExecutable.Verify`
is useful scaffolding, not a complete TOCTOU-proof installer. Protect the checked
closure from replacement during launch or refuse that environment.

**Before native acceptance:** feature-test peer pidfds and credential matching, verify
independence from the controller's job/session/cgroup supervisor, and provide an
already authorized unprivileged delegated cgroup environment. Real integrated fault
tests must pass. No automatic provisioning or privileged fallback is permitted.

**B6 later:** immutable production installation inventory, qualified runtime/native
dependency distribution, RID/protocol/source/artifact attestation and real provider
descriptor/registration remain mandatory for production. B need not complete all B6
to implement and test a trusted internal experiment. The historical phrase “B6-grade
pinned authentication” is not proof that experimental direct IPC needs production
installation acceptance first. Conversely this review supplies no missing production
installation guarantee. F-04–F-06 dispositions remain unchanged.

## 3. A: Linux process ownership and takeover assessment

A makes controller and recovery siblings with the owner under the controller. An
unrelated subreaper does not become that owner's parent when controller dies; adoption
goes to a living **ancestor** subreaper, otherwise the relevant namespace init. An
additional ancestor supervisor or recovery parenting the controller would be a further
topology/deployment change, not an effect of setting a flag in the sibling recovery.
Orphaned owner/coordinator descendants follow ancestry and PID-namespace rules, not
the logical recovery arrow. Thus A has no established recovery reaper for this tree.
[PR_SET_CHILD_SUBREAPER](https://man7.org/linux/man-pages/man2/PR_SET_CHILD_SUBREAPER.2const.html).

A non-parent can retain and poll an owner pidfd. That proves process termination,
not collection of wait status or reaping. `waitpid`/`waitid(P_PIDFD)` do not turn a
sibling into a waitable child; `WNOWAIT` observes a waitable child without reaping it.
The probe in §8 confirms `ECHILD` even with a readable retained pidfd. Current
`M1RecoveryScope.WaitForOwner` requires retained pidfd exit **and** direct-child
`Process.WaitForExit`; it cannot be reused unchanged as a sibling-owner recovery proof.
[pidfd_open(2)](https://man7.org/linux/man-pages/man2/pidfd_open.2.html),
[wait(2)](https://man7.org/linux/man-pages/man2/waitpid.2.html).

Writer exclusion is a further, separate fact. Takeover needs exact retained execution/
generation/domain authority, proof the original owner exited, the existing accepted
reaping path, and successful exclusive journal fencing/validated journal recovery.
A readable pidfd does not acquire the fence. A leaked duplicate of a flock-bearing
open file description can keep its lock alive; never inherit/transfer it to target or
recovery. Failure to acquire the existing fence or resolve the original journal means
Unknown/refusal, never forced unlock, remint or release resend.
[flock(2)](https://man7.org/linux/man-pages/man2/flock.2.html).

**Classification: Not equivalent to RC-1.** Non-parent exit observation could support a
separately amended design, but does not fill the missing reaping guarantee. Approving
A as drafted would leave that requirement unanswered. Adding a common supervisor is
an explicit new cost. B is narrower because it retains the accepted recovery ancestry,
pidfd/reaping and writer-takeover model while separating release IPC.

## 4. Comparative decision

| Criterion | B: recommended, subject to controls | A: proposed delta |
|---|---|---|
| RC-1 compatibility | Keeps recovery spawn/subreap/retain; direct protected original-controller association implements D05 | Amends D05 spawning/reaping responsibility; not equivalent as drafted |
| Release authority | Original controller direct session requests; owner capability and sole writer | Existing private controller-owner channel can be retained |
| Recovery authority | Exact native handles in actual owner parent before releasability | New authenticated handoff to non-parent; not implemented/proven |
| Owner lifetime | Recovery spawns, retains pidfd, reaps owner/adopted descendants | Controller spawns; recovery observes exit but cannot reap sibling |
| Controller death | Recovery remains owner ancestor and independently stops domain | Native stop may be possible with handles; adoption/reaping unresolved |
| Recovery death | Owner's genuine parent pidfd + lifeline loss stop obligation | Owner needs a separately authenticated non-parent recovery lifeline |
| Release/stop race | Existing owner arbiter gate; observed loss/stop ordering, no global death/write atomicity | Same race limitation; changing parentage does not solve it |
| Writer takeover | Proven exit/reaping plus existing exclusive fence and retained authority | Fence still necessary; reaping/takeover contract delta unresolved |
| Authentication | Exact direct peers + peer pidfds + one-use direct handshake; trusted pinned bootstrap identities | Existing release association; new independent recovery authentication needed |
| Native prerequisites | Unix peer credentials/pidfds, subreaping, bounded IPC and delegated cgroup; fail closed | Same stop resources plus new non-parent lifecycle/adoption proof |
| Complexity | New direct rendezvous/peer-pidfd helper; split channel routing and live monitoring; reuse transaction/arbiter | Reuses release channel but changes parent/reaper/recovery authority contract |
| Qualification | Full integrated process/death/retention/authentication matrix | Same matrix plus new supervisor/reaping proof and owner-approved amendment |

**Recommendation B; no process-ownership/security-contract amendment is required for
the bounded trust model above.** The concrete bootstrap/protocol and experimental
prerequisites need explicit owner authorization and subsequent implementation review.
This is not permission to weaken immutable identities, reaping, stop or release rules.

## 5. Release/stop fencing and failure-ordering limits

Keep `ReleaseArbiter`. `RequestStop` accepts stop/consumes capability under its existing
`_gate` (`ReleaseArbiter.cs:416–455`); the post-Intent release path checks stop/deadline/
prerequisites and consumes capability before committing the one write under that same
gate (`:282–365`). Those are the enforceable **owner-local** arbitration points.

Integrate distinct original-controller-session, controller-pidfd, recovery-pidfd,
recovery-channel/retention-validity and capability states. A dedicated bounded safety
monitor must make observed loss terminal, feed the existing `RequestStop`, and trigger
native stop independently of observers/drains. Admission and final gate prerequisites
must consult these states and fail closed on lost/unavailable evidence. No protocol wait,
journal I/O or callback into host policy may be introduced under the release gate.
Recovery's native `cgroup.kill` capability must remain usable even if owner IPC is wedged.

An event latch or pidfd poll is **not atomic with another process's death**. Loss already
accepted by `RequestStop` wins against later release under `_gate`. Loss observed before
the final prerequisite check must cause refusal. If death occurs after the final sample
while the owner has won admission, the write may race that death; preserve the release/
delivery facts, act on subsequent stop and report execution uncertainty. A stop sent by
recovery is not an accepted owner stop until the owner arbitration point. Native kill,
stop acceptance and domain-empty observation are separate facts.

Do not claim “recovery alive at every physical-write instant” or “wall-clock death before
write implies zero RELEASE”. Neither A nor B with these primitives proves that stronger
property. If that literal requirement is imposed, **both need an additional reviewed
kernel/resource fence; the present design cannot be authorized for that claim**. The
owner decision should explicitly retain the accepted C1–C4 gate ordering and this
concurrency qualification, rather than invent a total order across processes.

| Failure boundary | Required behavior in B / remaining authority |
|---|---|
| Controller dies during preparation | Recovery pidfd/EOF invalidates original session; recovery stops partial resources already retained. Owner aborts; never publishes releasability without all ACKs. Untransferred resource/cleanup gaps remain Unknown. |
| Controller dies while HELD | Owner independently sees direct EOF/pidfd loss and stops; recovery survives with the same domain handles and initiates safety stop. No new association/reconnect release. |
| Recovery dies while HELD | Owner sees actual recovery pidfd or lifeline loss, inhibits new admission, consumes via C2 stop and uses primary domain stop authority. Lost cleanup evidence remains Unknown. |
| Recovery dies concurrent with arbitration | Apply the owner-local ordering above; an already winning release may have been written. No second RELEASE and no invented pre-release prevention. |
| Owner dies before recovery retention ACK | No releasable preparation. Recovery uses whatever exact handles were actually received, proves/reaps owner exit, and records remaining resource uncertainty. No record-only adoption. |
| Owner dies after retained preparation | Recovery stops exact retained domain/staging resources, proves/reaps owner exit and uses existing fence/recovery rules; full B5 reconciliation remains pending. |
| Stop before mint | Owner pre-mint stop latch forbids publication of a live capability; make it observable. Recovery native stop alone is not proof this latch ran. |
| Stop wins after Intent before write | Existing gate consumes capability and refuses physical RELEASE; preserve durable Intent/MayHaveExecuted semantics and no retry. Test the actual stop acceptance barrier. |
| Release wins before stop / post-release death | Retain original decision, delivery, exec/root observations and one write. Native recovery/owner stop still acts; no claim the target never ran. |
| Recovery/owner bootstrap channel fails | Missing identity/authority or retention handshake refuses; stop with retained handles and retain durable facts, Unknown cleanup if unavailable. |
| Authentication/retention ACK lost | Do not publish releasability on the missing ACK; server-side success cannot be reconstructed through reconnect. Timeout is uncertainty, not permission to retry release or reset time. |
| Controller and recovery die together | Surviving owner invalidates both associations and stops using primary authority. If all holders die, evidence-only Unknown/quarantine; no disk/PID adoption. |
| Original deadline expires | Refuse at admission/final gate; retain original generation, clock and deadline. No handshake/reconnect retry renews them. |
| Reconnect after original generation loss | Read/stop/reconcile only under their own authentication; cannot recover handshake secret, operation association, capability or release generation. |

## 6. Minimum implementation changes and qualification gates

These are recommendations, **not changes made by this review**:

| Exact component | Minimum required adaptation |
|---|---|
| `Bootstrap/RecoveryConnection.cs`, `RecoveryProtocol.cs`, `RecoveryHost.cs`, `RecoveryInterop.cs` | Fixed experimental preparation configuration and bounded identity/pidfd/rendezvous metadata; preserve one-use B2 authentication and no B2 release message. Start actual B3 transaction under recovery; keep monitors alive during staged protocol. Do not share B2 secret as release secret. |
| `Lifecycle/M1RecoveryScope.cs` | Retain actual child owner, domain and staging authority; synchronize retained-handle publication; keep domain/prepared retention barriers and existing direct-child exit proof. Separate recovery protocol from direct controller admission. |
| `Lifecycle/OwnerPreparationTransaction.cs`, `Native/OwnerProtocol.cs` / `OwnerHeldWire.cs` | Explicit role-separated channels/state machines; controller manifest admission via direct authenticated session, native authority ACKs via recovery. Add pre-mint session/stop gates and exact original identity/deadlines. No capability serialization. |
| `Lifecycle/HeldReleasePhase.cs` | Actual recovery parent/lifeline and separate controller session/pidfd; independent safety handling during HELD/observations/drains. Feed existing arbiter; no parent-only proxy for all axes. |
| `Native/PidFd.cs` / `FdPassing.cs` or small adjacent helper | `SO_PEERPIDFD`, exact peer validation, CLOEXEC accept and bounded direct stream framing; reject ancillary descriptors on release channel. Preserve existing seqpacket recovery transfer semantics. |
| `Restricted/B3Qualification.cs` | Replace controller-owned recovery scope with authenticated B2 connection plus direct owner session, truthful retained-authority results and fixed experimental pins. No public/production registration. |
| `Execution/ReleaseArbiter.cs` | No arbitration redesign. Only commissioned F-03 diagnostic correction with regression tests; preserve C1–C4/Option A. |
| `tests/Fixtures/b3-release-coordinator.c`, B2/B3/bootstrap/protocol/journal suites | F-08 oracle/FD correction; F-07 separation-test clarification and genuine integrated recovery loss; deterministic plus real process matrix below. |

Required acceptance tests: genuine controller SIGKILL before HELD, while HELD and after
release; actual retaining recovery SIGKILL HELD/after release; owner death during prep,
HELD, after Intent, uncertain write and active execution; simultaneous controller/recovery
death; retention/authentication ACK loss, wrong FD roles/generation, partial transfer;
same-UID recovery/helper impersonation, substituted pathname/listener, stale/recycled PID
identity, peer exit during handshake, replay/second session and reconnect refusal;
fragmented/coalesced/oversized stream frames and bounded adversarial handshake load;
FD inventories across fork/exec, no target confirmation-FD leak and independent exec-failure
oracle; stop-before-mint, stop-after-Intent/pre-write, release-first, duplicate and original
expiry; blocked observer/capture with timely safety stop; owner-live fence refusal and
post-exit takeover; journal ambiguity/no resend. Include focused F-02/F-03/F-07/F-08,
M-01 and all affected B1/B2/B3/C1–C4 regressions.

Observe actual parentage/PIDs/pidfds, retained cgroup identity, kill write, population,
capability consumption, durable journal transitions, physical RELEASE count, target
effect and reaping separately. Successful sends/empty domains do not establish complete
cleanup; no mandatory native case is qualified by a mock, skip or these review probes.

## 7. Findings and explicit answers

| ID / severity | Exact location and fact | Consequence / required correction or decision | Blocking scope |
|---|---|---|---|
| Q1-F01 **MAJOR** | D01 §3 A; RC-1 D05 process paragraph/holder table; `M1RecoveryScope.cs:1092–1116`. Sibling recovery cannot reap the owner merely by enabling subreaping. | A is not equivalent; do not approve amendment as proposed. Specify/review a real ancestor/reaper or retain B. | Approving A; not B. |
| Q1-F02 **MINOR**, decision-relevant | D01 §3 B and B3 review §F “B6-grade” wording do not evaluate a listener created in owner followed by fresh direct mutually authenticated connection. Current lack of that implementation is not proof all production B6 must precede experimental work. | Distinguish experimental pin/bootstrap requirements from production installation attestation; B is feasible under §§2/5 qualifications. Historical records remain unchanged. | Using that premise to prefer A or declare B inherently unavailable; not bounded B implementation after authorization. |
| Q1-F03 **MAJOR**, implementation prerequisite | `RecoveryProtocol.cs:82–96`, `RecoveryHost.cs:27–49`, `RecoveryConnection.cs:25–62`, `OwnerProtocol.cs:273–390`. Existing trusted bootstrap is useful but no direct mutual peer/session proof exists. | Implement exact immutable identity provenance, peer pidfds and direct one-use handshake; endpoint/UID/sequence alone cannot authorize. No Byzantine-bootstrap attestation claim. | Acceptance of B until implemented/proven; A also needs independent recovery authentication. |
| Q1-F04 **MAJOR**, claim restriction | `HeldReleasePhase.cs:209–223`; `ReleaseArbiter.cs:282–365,416–455`; D01 §3 final bullet. Parent poll is not integrated recovery authority and death is not atomic with write. | Bind actual independent lifelines and terminal loss states into existing gate/stop path; qualify races as §5. Stronger death-before-write exclusion needs another reviewed fence. | Both alternatives for absolute death/write guarantee; B qualification until actual lifeline/stop tests pass. |
| Q1-F05 **MAJOR**, implementation prerequisite | `RecoveryHost.cs:64–85` starts owner before its monitor loop; `M1RecoveryScope.StartOwnerPreparation` performs synchronous multi-stage IPC; `HeldReleasePhase.ObserveExecution` blocks/drains. | Keep genuine safety monitoring/native stop independently available during preparation and post-release observation; no stop traffic behind a blocked controller exchange. | B qualification and any A design with the same blocking arrangement. |
| Q1-F06 **OBSERVATION** | Existing B3 F-08, `tests/Fixtures/b3-release-coordinator.c:254–301`. Extra inherited exec-confirmation FD is still present. | Correct and qualify oracle before FD 0–2 claims; EOF alone cannot rule out pre-exec death. No fixture correction claimed here. | A/B affected FD/oracle qualification, not B architectural feasibility. |

Explicit answers:

- **Is A approvable?** No, as proposed. Classification: **Not equivalent to RC-1**;
  additional ancestor/reaping design and owner-approved amendment would be needed.
- **Can B preserve RC-1 without a process-ownership delta?** Yes, under the bounded
  trusted-bootstrap and direct-authentication conditions above. It preserves recovery
  parenting/subreaping, sole owner authority, independent retained stop and original
  controller-only admission. New protocol implementation still requires review/testing.
- **Minimum authenticated bootstrap?** Existing pinned B2 bootstrap plus sealed original
  identities/live pidfds, owner-created rendezvous, fresh direct exact-peer/peer-pidfd
  checks, one-use direct challenge/secret confirmation and immutable non-resumable
  session; separate recovery retention/lifeline channel. No release bearer in recovery.
- **Unavoidable B6 dependencies before implementation?** No full B6 production completion.
  Exact trusted experimental artifact/runtime closure and protected launch inputs are
  unavoidable. If those cannot be secured/verified, refuse the experiment; do not claim
  production installation assurance. B6 remains required before production registration.
- **Exact next owner decision?** Decline A's ownership amendment as drafted; choose B
  and explicitly commission only bounded experimental INT-01 implementation with §§2/5/6
  gates and owner-local C1–C4 race semantics. Require stronger fencing review instead if
  an absolute physical-write/death order is demanded. This report makes neither decision.

F-01 remains MAJOR/open pending implementation, genuine integrated failure evidence,
independent implementation review and owner acceptance. F-02/F-03/F-07/F-08 remain
unfixed; M-01 remains previously closed; R1-F02 remains globally open. No B4 start,
B5 clock/reconciliation completion, B6 installation, B7 qualification, native Linux-host,
same-UID isolation, Hufu eligibility or production activation is established.

## 8. Review execution, provenance and delivery validation

New evidence is limited to source/contract review, upstream primary Linux references
linked above and **two standalone kernel-semantics probe scenarios**. It is separate
from historical producer B2/B3 runtime evidence and the earlier review's Windows runs.
No Gagamba build, test suite, target, coordinator, cgroup or integrated lifecycle ran.

Probe command: `wsl.exe -d Ubuntu -u gagamba -- python3
/mnt/c/Users/Laszlos/source/repos/Solo/.tmp/int01_d01_q1_probe.py`.
Environment: WSL2 Ubuntu, uid/gid 1001, kernel
`6.6.114.1-microsoft-standard-WSL2`, x86_64. No privilege elevation or delegation changes.
The initial scenario run passed; a strengthened run then added direct `SO_PEERPIDFD`
checks in both directions and also passed, zero failed scenarios. This is feasibility
evidence for the measured primitives, not test coverage of the proposed Gagamba protocol.

| Scenario | Strengthened-run observations |
|---|---|
| B direct Unix connection | Controller PID 314; recovery 431; actual recovery-child owner 432. Mutual exact peer credentials and live CLOEXEC `SO_PEERPIDFD` checks passed. Recovery's same-UID connection was rejected; recovery had no socket FDs while the direct connection was live; a repeated nonce response was refused. Recovery reaped owner. |
| A sibling recovery | Controller 433; sibling owner 434/recovery 435. Controller SIGKILL; supervisor 314 was the ancestor subreaper and became owner parent. Sibling recovery's `waitpid` returned ECHILD; retained owner pidfd became readable, but `waitid(P_PIDFD)` still returned ECHILD. Ancestor supervisor reaped owner. |

Probe uses a temporary 0700 directory, finite pipe/connection barriers, 5-second IO
bounds and 15/25-second watchdogs. It confirmed all probe children reaped and removed
its temporary socket/directory. No Gagamba/native domain cleanup is claimed. The nonce
echo is only a primitive probe, not implementation of the full transcript/bootstrap.

Probe SHA-256: `c3f269653123d3554c66fe372f15b32d206f4ffd3b2281643cabf3252c8f24f8`.
Script copy, observed JSON and validation/binding records are retained locally under
ignored `artifacts/rc2b-03-int-01-d01-q1/`; no raw process log or probe implementation is
committed. Off-device backup is not established by this review.

Delivery validation: `eng/verify-preparation.ps1` **PASSED** (12 required files,
218 Markdown documents, 911 local links, 5 historical compatibility documents / 42
references; original proposal hash unchanged). `git diff --check` and staged equivalent,
single-report scope and unchanged protected identities **PASSED**. The report commit/
tree and remote receipt accompany the handoff. Only this report is committed/pushed; current status/decisions/evidence remain
immutable. Stop after review; implementation, contract approval, B4 and activation remain
outside this commission.
