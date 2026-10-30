# RC2B-03-INT-01 architecture compatibility gate

Date: 2026-10-30 (Asia/Manila).
Remediation slice: GAGAMBA-RC2B-03-INT-01, mandatory architecture decision gate (§5).
Baseline and final source: `98d46f308d27d7ac3fd95079d6bc5b5d9bad482d`.
Baseline/source root tree: `32669d928723fc305a1eda808e855de0a62b2683`.
Branch / repository: canonical `main`, `jenolaszlo-sketch/gagamba`.
Host: Windows 10.0.26200, X64; .NET SDK 10.0.401.
Available WSL2: kernel `6.6.114.1-microsoft-standard-WSL2`, x86_64,
glibc 2.43, SDK 10.0.113, cgroup2fs; default invocation uid/gid 0.
Native prerequisite observation: historical `/sys/fs/cgroup/gagamba-b3` is absent;
no alternate authorized delegated parent was selected or provisioned.
Qualification scope: source/descriptor-flow assessment and documentation validation only.
Exclusions: topology implementation, new execution/failure qualification, finding closure,
B4–B7, native Linux-host qualification, Hufu and production activation.

**Final disposition: BLOCKED BY CONTRACT DELTA.**

The commission reached its explicit stop condition before topology changes. This is
not an implemented integration or a ready-for-review implementation claim. The bounded
design-delta finding below requires independent security review and owner approval.
No owner acceptance is issued. B3 remains ACCEPTED WITH QUALIFICATIONS; F-01 remains
MAJOR and open. The environment observation is an additional future native prerequisite,
not the reason for the contract-gate disposition.

## 1. Git and source binding

Before modification, local HEAD, tracking `origin/main` and live remote `main` were
`98d46f308d27d7ac3fd95079d6bc5b5d9bad482d`. No tracked working/staged changes
were present. Historical untracked files were preserved. This delivery changes only
this record and current pointers in PROJECT-STATUS, handoff and work queue.

| Unchanged protected tree | Git identity |
|---|---|
| src | `c28e8dc345b05549c45ea93be1958aa88141b1da` |
| tests | `ad6f2796ca81f489d8daa9c89f69a44fd68228e5` |
| native | `85631d2f04512376621d3d09f51d67fd90f45335` |
| .github | `eb61716a96da2a23704365df6ec2d7fafa0f8cf9` |
| eng | `0a6dba6709001bbd049595c1200ec9ee471f2b1d` |

There is **no implementation commit**. Final implementation/source identity is the
unchanged baseline above. The documentation commit/tree and remote/CI receipt are
returned in the completion handoff. Historical reviews, evidence, decisions and
production-unavailable runtime selection are unchanged.

## 2. Traced authority and descriptor flows

References below identify inspected source at the baseline; they are source observations,
not fresh process observations. Historical runtime evidence remains attributable to the
[B3 producer](rc2b-03-b3.md) and [independent review](../reviews/rc2b-03-b3-independent-review.md).

| Component | Current source-confirmed relationship |
|---|---|
| `B3PreparationOwner` (`Restricted/B3Qualification.cs:205,351,570`) | Creates its own in-process `M1RecoveryScope`; starts the actual preparation and sends release on that scope's owner channel. The controller hosts this scope. |
| `M1RecoveryScope` (`Lifecycle/M1RecoveryScope.cs:173,243,322`) | Spawns a direct-child owner with inherited `GAGAMBA_BOOTSTRAP_FD`, immediately retains its CLOEXEC pidfd and holds the peer endpoint. It writes initial intent, releases its writer before owner acquisition, validates received domain/staging handles and acknowledges retention. |
| Retention barrier (`M1RecoveryScope.cs:414,476,566`) | Domain, staging and prepared acknowledgements currently come from the scope that also owns the eventual release-request channel. In B3 that is the controller, not independent B2 recovery. |
| `OwnerPreparationTransaction` (`Lifecycle/OwnerPreparationTransaction.cs:64,155,164,320–348`) | Owner acquires the writer, transfers duplicate recovery handles, awaits retention, durably records Prepared, then mints an owner-local capability. Same channel later reaches `HeldReleasePhase`. |
| `HeldReleasePhase` (`Lifecycle/HeldReleasePhase.cs:71,209–223`) | Opens the parent pidfd and maps it to `RecoveryAlive`; `CallerAlive` is true while servicing the request. It has no separate authenticated B2 recovery lifeline. |
| `RecoveryHost` (`Bootstrap/RecoveryHost.cs:28,51,66–85,128`) | Verifies controller bootstrap peers, detaches lifetime, creates its own scope, starts a B2 lifeline owner or retained-domain barrier owner and proves/reaps that direct child's exit. It does not supervise B3's owner. |
| `RecoveryConnection` / `RecoveryProtocol` | Controller-to-recovery one-use session credential is delivered to recovery. B2 wire permits Associate/Ready/Stop/Inspect/Snapshot/Reconnect/Refused only. Its credential is not an end-to-end controller-to-owner secret. |
| `OwnerProtocolChannel` / `OwnerProtocolSequencer` | Inherited endpoint possession, execution/generation binding, sequence and shape checks protect the current association. There is no independently authenticated release-message signature or per-message sender credential check. Sequence is not sender authentication against a holder of the endpoint. |
| `DomainHandleTransfer` / `FdPassing` | Exact ordered seven-role domain descriptors and bounded SCM_RIGHTS records carry retained native authority, not release authorization. Descriptor receipt is not evidence of an independent recovery actor unless that process actually owns the validated handles. |
| `ReleaseArbiter` | Existing owner-local durable Intent, gate ordering, C2 consumption and one transport writer remain unchanged. Moving its parent cannot supply a missing independent lifeline or authenticate a forwarded request. |
| Owner-exit boundary (`M1RecoveryScope.cs:1075–1116`) | Retained pidfd observation precedes `Process.WaitForExit` for the direct child. This implementation does not establish the same reaping contract for a sibling owner. A live owner's writer must not be taken over. |

Current process arrangements therefore remain the two arrangements documented in
B3 evidence §3. No new PIDs, pidfds, domain handles, process ancestry or integrated
RELEASE observations were produced by this gate assessment.

## 3. Alternatives and bounded design-delta finding

**INT-01-D01 — RC-1 process ownership / reaping delta requires review.**

[RC-1 §RC-D05](../design/restricted-consumer-contract-rc1.md) states:
“That process owns `M1RecoveryScope`, enables subreaping, opens the delegated parent
and spawns/retains the per-execution owner.” Its holder table also assigns recovery
the owner pidfd, subreaper and takeover authority after proven owner exit.
The [accepted B3 owner decision §7](../decisions/rc2b-03-b3-owner-acceptance.md)
does not approve either alternative; changes to RC-1 security semantics require
a scoped design delta and independent review.

| Alternative | Assessment and decision at this gate |
|---|---|
| A: controller-parented owner, independent stop/recovery actor | Smaller reuse of the existing B3 private controller/owner association. However, recovery no longer spawns or directly reaps this owner. Handle/pidfd transfer and retention ACKs would need a new authenticated association before mint; parent-only liveness is insufficient. This changes the quoted RC-1 process contract and cannot be treated as an equivalent refactor. **Candidate for bounded delta review, not approved or implemented.** |
| B: recovery-parented owner, independent controller authorization | Preserves the stated RC-1 parentage. The existing B2 session secret is known to recovery, and passing/relaying the current owner endpoint lets a holder inject frames. Neither sequencing nor SO_PEERCRED alone fixes that. The B3 review §F calls for B6-grade pinned controller-to-owner authentication for this route; no such independently accepted association/installation trust binding exists in the inspected baseline. Inventing a test secret or treating caller-populated experimental pins as production trust would violate the commission. **Remains an alternative requiring a concrete independently reviewed bootstrap/authentication basis; not declared impossible and not implemented here.** |

Proposed delta for **A**, presented for review rather than silently applied:

- Allow the original controller to spawn the transaction owner and retain its private
  release association. Recovery remains an independent process with **stop/recovery
  authority only**. Neither endpoint nor release credentials pass through recovery.
- Replace recovery's direct-child creation/reaping responsibility for this owner with
  an authenticated, identity-bound transfer of its live pidfd and exact native domain/
  staging authority. Recovery must acknowledge validated retention before publication
  can become releasable. Passive records, PIDs and paths cannot supply this authority.
- Explicitly decide **who reaps the owner and orphaned descendants after controller
  death**. Recovery being a subreaper does not by itself establish adoption/reaping of
  a controller-parented owner in the current code. This is an unresolved changed failure
  assumption, not a guarantee to waive implicitly. If equivalent reaping requires a
  different ancestor/supervisor arrangement, reject this candidate or review that change.
- Review whether retained pidfd exit proof plus the existing exclusive writer fence
  is sufficient for permitted non-parent recovery. Do not change takeover conditions,
  claim reaping from a PID disappearance, or acquire a writer while the owner is live.
- Require distinct controller-session, recovery-process and recovery-retention lifelines,
  with no release while mandatory authority is lost. Review the enforceable stop/release
  linearization point; do not promise absolute ordering from a liveness poll alone.

This delta changes process ownership/failure assumptions, **not** C1–C4, Option A,
original deadlines, sole owner RELEASE/writer authority, no bearer through recovery,
or no authority reconstruction. Those requirements remain mandatory. If the review
cannot preserve the required recovery guarantees, A must not be selected; assess B
with a specific authenticated bootstrap instead. No new general service is proposed.

**Independent security review and owner approval are requested for INT-01-D01 before
the contract-changing topology dependency resumes.** This request does not dispatch a
review chat, grant approval or authorize B6 production installation work. The stop is
required by commission §§5 and 19 Step 1, not by a filesystem approval rejection.

## 4. Findings and implementation state

| Finding | Current disposition |
|---|---|
| F-01 MAJOR | **Still open.** No integrated topology or process-failure qualification; no closure or owner acceptance. |
| F-02 MINOR | **Still open.** Parent/controller-only `RecoveryAlive` is unchanged; no genuine recovery-lifeline correction claimed. |
| F-03 MINOR | **Still open.** Diagnostic correction and focused regressions are not performed in this stopped gate slice. |
| F-07 OBSERVATION | **Still open.** Historical separation test is unchanged; no genuine integrated recovery-death test exists from this work. |
| F-08 OBSERVATION | **Still open.** `FD_CLOEXEC`/exec oracle remains uncorrected; no new target FD 0–2 claim. |
| F-04–F-06 | Unchanged experimental-pin, coordinator-mediated-fact and production-descriptor/B6 dispositions. |
| M-01 | Previously CLOSED; unchanged. R1-F02 remains globally OPEN. |

Subordinate corrections remain queued behind the architecture gate in the commissioned
sequence; no implementation or new regression tests were added. The accepted B1/B2/B3,
C1–C4 and Option A implementations were not modified or requalified.

## 5. Checks and qualification boundary

| Check / experiment | Result and provenance |
|---|---|
| Git HEAD / `git ls-remote origin refs/heads/main` | Producer-executed; baseline binding matched before changes. |
| Named ownership/descriptor source inspection | Producer-executed source assessment, summarized above; not independent review. |
| Windows `dotnet --version`, OSDescription / OSArchitecture | Producer-executed read-only inventory: 10.0.401; Windows 10.0.26200; X64. |
| WSL2 `uname -a`, `id`, `getconf GNU_LIBC_VERSION`, `dotnet --version`, `stat -fc %T /sys/fs/cgroup` | Producer-executed read-only inventory; host facts in header. Root invocation is not unprivileged qualification. |
| WSL2 `ls -ld /sys/fs/cgroup/gagamba-b3`; retained `test -d` check | NOT AVAILABLE: no such directory; `test -d` exits 1. No provisioning, privilege elevation or unrelated cgroup change performed. |
| Integrated controller/recovery/owner death matrix, HELD/release/stop/native FD oracle tests | **NOT RUN — contract gate stopped implementation.** No integrated pass count, RELEASE count, stop acknowledgement or cleanup result exists. |
| B1/B2/B3, C1–C4, M-01, protocol/journal regression reruns | **NOT RUN in this documentation-only gate slice.** Prior counts remain historical evidence, not new executions. |
| `eng/verify-preparation.ps1` | **Passed**: 12 required files, 217 Markdown documents, 903 local links, 5 historical compatibility documents / 42 references; original proposal hash unchanged. Invoked from PowerShell as `& .\eng\verify-preparation.ps1`. |
| `git diff --check` / `git diff --cached --check`, protected-path and exact four-file scope checks | **Passed**. Documentation/whitespace validation only; no backend qualification. |

Raw source trace and validation/environment logs are retained locally under ignored
`artifacts/rc2b-03-int-01/architecture-gate/`; no executable artifacts were built.
No raw volatile process log or large binary is committed. Off-device backup of local
evidence is not established by this commission. No test-owned native domain, target or
owner/recovery fixture was created, so native workload cleanup is **not applicable**,
not independently confirmed cleanup of an integrated execution.

All B4–B7, hostile populated-domain stopping, sealed C3, installed deployment,
native Linux-host, same-UID isolation and Hufu restrictions remain. Production
`RestrictedExecutionRuntime.Create()` still returns the unavailable provider.
**B4: NOT STARTED.**

Smallest next task: independent security review of **INT-01-D01**, then an explicit
owner decision on the bounded process-ownership delta (or a reviewed concrete B
authentication basis). Resume the integration only under that disposition, with an
authorized unprivileged delegated native environment and the full real-process matrix.
