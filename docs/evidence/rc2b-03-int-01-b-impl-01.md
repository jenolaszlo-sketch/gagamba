# RC2B-03-INT-01-B-IMPL-01 implementation checkpoint and environmental refusal

Date: 2026-10-30 (Asia/Manila).
Remediation slice: GAGAMBA-RC2B-03-INT-01-B-IMPL-01.
Baseline source: `89499676dc7cefaf51e39e1ae8758ca9b6561e57`.
Final tested source: `17a61f5be2602445fdb195a7ef76e9d88f14640e`.
Branch: canonical `main`, `jenolaszlo-sketch/gagamba`.
Windows: 10.0.26200 x64; SDK 10.0.401, runtime 10.0.12.
WSL2: Ubuntu 26.04 x86_64, kernel `6.6.114.1-microsoft-standard-WSL2`,
glibc 2.43, GCC 15.2.0, SDK 10.0.113, runtime 10.0.13.
Relevant native prerequisites: SO_PEERPIDFD and unprivileged socket/pidfd
primitives available; **no authorized delegated parent/host launcher established**.
Qualification scope: Windows builds/managed regressions; WSL2 authentication,
lifetime-component, stream, journal-fence and exec/FD primitives only.
Exclusions: genuine integrated execution/HELD/RELEASE/domain stop, integrated
death/retention/race qualification, independent review, native Linux-host/macOS
qualification, full B4â€“B7, Hufu and production execution.

**Verdict: BLOCKED BY ENVIRONMENTAL PREREQUISITE.**

**Native integration: BLOCKED_ENVIRONMENT / NOT_REPRODUCED.** This is a
partial implementation delivery, not completion of the commission or readiness
for security acceptance. The integrated composition exists and builds, but the
required real execution has not occurred. The core native runner has eight
implemented cases that were not run; its 24 additional required integration
cases remain pending fixture/coverage work. Other authentication negatives
identified below also remain unqualified. Green component tests do not close
those gaps. **F-01 remains MAJOR/OPEN. No self-approval or owner acceptance.**

## 1. Authority, source custody and Git identities

Governing authority remains the unchanged [D01 owner decision](../decisions/rc2b-03-int-01-d01-owner-decision.md)
and [Q1 review](../reviews/rc2b-03-int-01-d01-independent-review.md).
Approved Alternative B is implemented as the experimental direction; no RC-1,
C1â€“C4 or Option A amendment is made. Historical decisions, reviews and evidence
were not edited.

The owner subsequently confirmed that neither historical `/sys/fs/cgroup/gaga-delegated`
nor `/sys/fs/cgroup/gagamba-b3` is current authorization. Only read-only discovery
was authorized: no delegation provisioning, cgroup permission changes, elevated
migration or weaker launch path. A separate owner decision is required for
provisioning or an alternative launch mechanism. Work stops at that boundary.

| Checkpoint | Commit | Root tree |
|---|---|---|
| Baseline, initially equal to remote main | `89499676dc7cefaf51e39e1ae8758ca9b6561e57` | `33d3da6a81bf79e99c613e40286a1a4b477db76c` |
| Authentication primitives | `ab524b3c7025e19f521228bc644850d18c8533c1` | `8187b643be86134297bcb3a00471a8fe67f1ec88` |
| Integrated source composition | `4257c39025e78a3479707347e83b1f53e04599c2` | `cb7c7bb91d2834e446ecd9d228d24240d9a6702f` |
| Lifeline/test refinement; analyzer error subsequently corrected | `7ddfe3ee5bff618526865646c866c0701503aa33` | `2383c603294d9df3782a0a9e7490f011c10fb018` |
| Final tested implementation and test correction | `17a61f5be2602445fdb195a7ef76e9d88f14640e` | `f273f07d6da3700d3af04c3703715a48667619b5` |

Final tested subtree identities: src `2748fb78af6a2658ae15b34db5903f2e120b6755`;
tests `377fae5bfa0b923073026f3a3dfaf030b53652dd`;
native `85631d2f04512376621d3d09f51d67fd90f45335`;
eng `70b0e46a1ec0f789018a8af47f7e3672940cec6e`;
.github `eb61716a96da2a23704365df6ec2d7fafa0f8cf9`.
Native fixture sources under tests changed; the separate native Git subtree did not.

All work used the canonical Windows checkout directly on main. No new branch,
linked worktree, reset, rebase, amend, force push, tag or publication was used.
Pre-existing untracked artifacts remain. The final evidence/status commit is a
separate documentation delivery; its commit/tree, normal push receipt and CI
status are returned in the completion handoff, avoiding self-referential hashes.

## 2. Current environment and explicit refusal

Final discovery ran as UID/GID 1000, with CapInh/CapPrm/CapEff/CapAmb all zero.
Membership was `0::/init.scope`. The read-only scan covered directories through
depth three: 32 cgroups, one writable directory candidate. Deeper descendants
were not inventoried; this is not an exhaustive host authorization inventory.

| Observation | Current result |
|---|---|
| Shared systemd user subtree | `/sys/fs/cgroup/user.slice/user-1000.slice/user@1000.service` |
| Candidate directory | UID/GID 1000, mode 0755, writable by this user |
| Candidate controllers / subtree_control | `cpu memory pids` / `cpu memory pids` |
| Candidate cgroup.procs | UID/GID 1000, mode 0644, writable |
| Candidate cgroup.kill | UID/GID 0, mode 0200, not writable by UID 1000 |
| Root/common ancestor cgroup.procs | UID/GID 0, mode 0644, not writable |
| Actual execution placement | `/init.scope`, outside the candidate |
| Authorized parent / launch procedure | None established |
| Migration / service-mediated launch attempted | No / No |

The writable shared systemd subtree is **not selected, adopted or authorized**
for this task. Its existence and access bits do not prove a permitted migration
or qualified launcher. Direct migration's common-ancestor write prerequisite is
unavailable in this context. No process migration was attempted. No systemd scope,
delegation, permission change or privileged fallback was created.

`eng/int01-native-matrix.py --preflight-only` refused before launching a
controller: five UID/capability checks passed; four declared-environment/artifact
checks failed (no declared operator parent, host relationship, integrated pinned
profile or controller artifact in that invocation). The core matrix and genuine
controller/recovery/owner failure scenarios are **NOT RUN**, not eight passes.
The later read-only discovery does not upgrade that refusal. WSL2 is not an
independent native Linux host.

## 3. Source implementation and authority separation

This table describes code, not observed integrated process ancestry.

| Authoritative path | Implemented behavior; qualification limit |
|---|---|
| RecoveryHost / IntegratedRecoveryHost | After B2 association and lifetime detachment, owns M1RecoveryScope and one bounded preparation worker. Recovery remains the safety-monitor/reaper thread; native stop is outside preparation/observation/journal workers. Actual survival and domain retention are unqualified. |
| M1RecoveryScope | Spawns the actual OwnerPreparationTransaction executable, retains its pidfd, transfers the existing exact seven-role domain mapping and staging authorities, and checks retention/registered/prepared barriers. Publication of retained resources is synchronized without holding the resource lock over owner protocol waits. |
| Integrated B3 controller | Creates no controller-side M1RecoveryScope. Receives a bounded rendezvous and owner pidfd through explicit non-release roles, then connects directly. Legacy B3 is retained only for its existing separate-profile regressions; an integrated failure does not fall back to it. |
| OwnerPreparationTransaction | Reuses the existing closure, journal, store, mint, HELD launcher and ReleaseArbiter. Recovery handles custody ACKs; the authenticated controller handles staged-description validation, manifest admission and preparation ACKs. Both barriers precede usable publication. |
| DirectBootstrapPacket / dedicated lifeline | Versioned fixed controller-process-reference and recovery-safety-lifeline roles, separate from domain roles. Exact socket type, CLOEXEC, recovery peer credentials and disjoint endpoint identity are checked. These descriptors cannot request physical RELEASE. |
| OwnerSafetyMonitor | Dedicated monitor plus at most one durable-stop worker. Separately samples controller pidfd/session, genuine recovery pidfd/lifeline, retained readiness and original cutoffs. Latches stop, independently attempts native kill, then feeds the existing arbiter. An owner stop-latch ACK is distinct from recovery retention. |
| HeldReleasePhase | Integrated RecoveryAlive uses the actual recovery relationship; final checks consult terminal loss and live references. The accepted release winner is sampled without a callback under the arbiter gate. Setup admission does not become a new execution deadline while post-write journal I/O is blocked. |
| Recovery takeover | Requires retained owner exit evidence, direct-child/adopted reaping, and a separately acquired exclusive journal writer. Uses retained native/staging identities. Its immediate existing-reconciler attempt uses zero additional waiting allowance and verifies current boot identity. This is not full sealed C3/B5 reconciliation. |

The owner monitor begins after the immutable identity bootstrap and before native
allocation. Earlier pin/control validation has no live domain/capability and is
supervised by recovery. Blocked bootstrap/pin processing, genuine retention loss,
blocked observation/drain and cleanup races still require the integrated matrix;
source structure is not proof that every commissioned blocking case is covered.

No capability, store, journal lock open-file description, physical RELEASE writer,
connected direct-release endpoint or direct session secret is routed through
recovery. This exclusion is source-inspected, not an integrated runtime custody
qualification. The actual integrated controller/recovery/owner PIDs, domain/staging
FD inventories, HELD witness, release count, target outcome, reaping and takeover
proof are **unavailable / NOT_REPRODUCED**.

## 4. Authentication and stream evidence

DirectPeerIdentity obtains SO_PEERPIDFD from the connected socket and verifies
SO_PEERCRED plus retained live expected references, before/after relevant checks.
There is no numeric-PID reopen, UID-only or privileged authentication fallback.
All bootstrap actors are required to share the documented PID/user namespaces;
no namespace-conversion fallback exists.

The owner creates a fresh 0700 directory and 0600 AF_UNIX SOCK_STREAM endpoint.
Directory/socket identities, symlink refusal, bounded address/attempt/deadline,
atomic socket/accept CLOEXEC and verified unlink/removal are enforced. Only wrong
OS peers may be rejected without consuming the original attempt. Once the original
controller is verified, any handshake failure, including lost ACK, is terminal.
Successful association removes/closes the listener; no resumed session exists.

Fresh direct challenges and a distinct CSPRNG secret confirm a versioned
HMAC-SHA-256 transcript containing roles, identities, execution, original store
generation, original session and challenges. Lease generation remains separately
bound by the inner owner protocol. Temporary key/frame material is erased.
The B2 secret is not the direct secret. No secret/challenge/MAC is recorded in the
evidence files. This is not protection against arbitrary trusted-bootstrap
compromise or hostile same-UID memory/descriptor theft.

Bounded stream framing checks length before allocation, reads exact partial
prefix/body lengths, leaves coalesced following frames queued, rejects unexpected
ancillary data while closing delivered rights, validates independent sequences,
and refuses malformed/truncated/timed-out/lost-peer sessions. There is no async
message pump. Existing target spawn actions overwrite slots 0â€“11 and closefrom(12);
the new direct endpoint is absent from the target descriptor map. The actual
integrated inherited inventory remains unqualified.

Final **authentication primitive** run: controller test process PID **829**;
direct probe process PID **930**. Controller observed peer PID/UID/GID
930/1000/1000 with socket-derived pidfd 137, CLOEXEC and live expected owner reference.
Probe observed 829/1000/1000 with socket-derived pidfd 72, CLOEXEC and live expected
controller reference. Both used PID namespace `pid:[4026532398]` and user namespace
`user:[4026531837]`. The transcript's correlation-only Recovery role, PID 781, is
the test adapter ancestor, **not** the B2 recovery actor. These are not the three
actors of an integrated B3 run and must not be presented as such.

21/0/0 tests cover mutual real-process association, exact wrong identities and
execution/generation/session, stale retained references, same-UID peer mismatch,
fragmentation/coalescing, invalid lengths, truncated timeout, ancillary-FD close,
listener substitution/modes, lost ACK/reflected confirmation, peer exit, second
connection refusal and transcript binding. Actual recovery impersonation, forced
numeric PID recycling and the full integrated negative set remain unqualified.

Primary mechanism references: [Linux v6.6 socket implementation](https://github.com/torvalds/linux/blob/v6.6/net/core/sock.c)
and [unix(7)](https://man7.org/linux/man-pages/man7/unix.7.html).

## 5. Findings and baseline-to-fix observations

| Finding / probe | Baseline | Change / final observation | Disposition |
|---|---|---|---|
| F-01 | Accepted deferred major topology blocker | Recovery-parented composition is present in source; genuine execution and full fault matrix not reproduced | **OPEN**, not closed or ready for acceptance |
| F-02 | Parent/controller represented RecoveryAlive | Distinct recovery identity, pidfd and safety lifeline; four live-process/component loss tests pass, including capability publication refusal after loss | Source addressed; actual integrated recovery death still pending |
| F-03 | Retained/stop paths report invalid length as mismatch | BindingFor checks validity first; two managed regressions preserve C4 decision, journal bytes and write count | Fixed in source; Windows/WSL2 regressions pass |
| F-07 | Unrelated B2 death mislabeled as B3 recovery failure | Old test renamed as a topology-separation demonstration; core runner targets actual integrated recovery | Clarified; genuine test **NOT RUN** |
| F-08 / Q1-F06 | Coordinator clears confirmation CLOEXEC; failed exec also reaped twice | Keep CLOEXEC, bounded confirmation read plus pinned executable-object corroboration; root wait collected once. Static probe tests successful/failed exec and pre-exec SIGKILL/normal exit, with actual FDs 0â€“2 | Four primitive cases pass; confined integrated inventory remains pending |
| Q1-F03 | Direct exact-peer association missing | Direct authenticated one-use stream and fail-closed feature test implemented; 21 primitive tests pass | Integrated closure pending |
| Q1-F04 | Genuine recovery liveness/race handling missing | Distinct live relationships and existing arbiter integration; original accepted winner preserved | Genuine death/release races pending |
| Q1-F05 | Blocking preparation/observation can disable safety | Dedicated recovery monitor/preparation worker and owner monitor/stop worker; synchronized custody publication | Full blocked-operation matrix pending |

The unchanged baseline was not separately executed with the new regressions.
Baseline defect attribution is the preserved independent review and source, not
an invented baseline test failure. F-03's first new regression failed because it
compared decoded array-bearing records by reference; byte comparison corrected
the test. That was not a reproduced product defect. The first FD-oracle probe
saw a dynamic loader descriptor in /bin/sleep; the static startup inventory
corrected the oracle. Neither attempt is erased or recast as a pass.

## 6. Release, preparation and remaining matrix gates

Existing ReleaseArbiter, C1 ambiguity/first-retainer, C2 irreversible stop,
C4 duplicate/concurrent semantics and R1-F03 Option A are retained. The only
arbiter decision change is F-03 diagnostics; the added winner getter is read-only.
HeldExecution counts actual physical write-call attempts without creating another
arbiter. Missing count evidence remains nullable Unknown. Loss accepted before
release prevents later release; an already winning release keeps its decision,
with subsequent stop independently effective. No wall-clock death/write atomicity
is claimed. Full sealed C3 remains separate.

The native core runner contains positive one-release/duplicate, stop-before-release,
and actual controller/recovery/owner SIGKILL while HELD and after release. **All
eight are NOT RUN.** Its 24 additional cases are explicitly pending, including
preparation/retention deaths and missing/lost ACKs, role/count/partial transfers,
after-Intent/ambiguous-delivery deaths, blocked-observer stop, simultaneous loss,
stop-before-mint/pre-write, persistence/ack ambiguity, deadline, actual recovery
impersonation and integrated live-owner/post-exit writer fencing. The runner is
not the complete commissioned matrix. Primitive journal-fence tests do not
substitute for integrated owner-exit/reaping/takeover evidence.

Consequently every topology-dependent acceptance gate remains unchecked. There
is no observed integrated HELD, physical RELEASE count or native target outcome.
No preparation remint, release resend, reconnect restoration or production
provider registration was added. RestrictedExecutionRuntime.Create remains
production-unavailable. B4â€“B7, Hufu and production gates remain closed.

## 7. Executed checks and reproducible commands

Counts are passed/failed/skipped; overlapping suites are not summed as distinct tests.

| Platform / check | Result |
|---|---|
| Windows final solution and controller fixture builds | 0 warnings / 0 errors |
| Windows M1 supported parallel phase | **985 / 0 / 95**, 1,080 total |
| Windows focused auth/safety/admission/wire | **18 / 0 / 24**, 42 total |
| Windows Execution | **68 / 0 / 0** |
| Windows Runtime | **15 / 0 / 0** |
| WSL2 direct authentication | **21 / 0 / 0** |
| WSL2 owner-safety/admission/owner-protocol/B2/wire/journal-fence components | **85 / 0 / 0** |
| WSL2 exec/FD oracle | **4 / 0 / 0**, all direct children reaped |
| Integrated native matrix | **BLOCKED_ENVIRONMENT / NOT_REPRODUCED** |
| Independent native Linux host / macOS | **NOT RUN** |

Final source and tests were clean at 17a61f5 before qualification. Windows final
M1/focused and WSL2 runs were rebuilt/bound to that commit. Execution/Runtime
runs preceded the final test-only/lifeline refinements; their source was unchanged.
All evidence is producer execution. No independent reviewer or independent native
host execution is claimed. Ordinary CI, reported in the final handoff, is not
confinement or integrated qualification.

```powershell
dotnet build Gagamba.sln -c Release --no-restore --nologo
dotnet build tests/Fixtures/B2Controller/B2Controller.csproj -c Release --no-restore
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --no-restore --filter 'Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState'
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~OwnerSafetyMonitorTests|FullyQualifiedName~ReleaseAdmissionTests|FullyQualifiedName~OwnerHeldWireCodecTests'
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-build --no-restore
dotnet test tests/Gagamba.Runtime.Tests/Gagamba.Runtime.Tests.csproj -c Release --no-build --no-restore
wsl -d Ubuntu -u gaga -- bash /mnt/c/Users/Laszlos/source/repos/Gagamba/eng/int01-native-primitives.sh
wsl -d Ubuntu -u gaga -- python3 /mnt/c/Users/Laszlos/source/repos/Gagamba/eng/int01-cgroup-discovery.py --report /mnt/c/Users/Laszlos/source/repos/Gagamba/artifacts/rc2b-int01/wsl2/cgroup-discovery.json
wsl -d Ubuntu -u gaga -- python3 /mnt/c/Users/Laszlos/source/repos/Gagamba/eng/int01-native-matrix.py --preflight-only --report /mnt/c/Users/Laszlos/source/repos/Gagamba/artifacts/rc2b-int01/wsl2/integrated-matrix
```

Earlier failed attempts are preserved locally: DrvFS MSBuild timestamp denial;
temporary WSL build disappearance between invocations; apphost execute-bit test
discovery failure (zero tests, not success); two initial negative-test exception
expectation errors; F-03 test comparison errors; dynamic-loader inventory timing;
and xUnit2031 at 7ddfe3e, committed before checking the build exit code and
corrected by the verified 17a61f5 follow-up. No amend/rewrite hid that checkpoint.

## 8. Raw evidence, artifacts and cleanup

Local root: `C:\Users\Laszlos\source\repos\Gagamba\artifacts\rc2b-int01`.
The 99-file local inventory is `sha256-manifest.txt`, SHA-256
`6bb7cc166a97f7c004ece972fec970a9d09f76f1b697cfa4536279aa3e1033dd`.
This seals the qualification corpus; later delivery-validation and remote/CI receipts are separate.
It covers logs/TRX, source/runtime identities, peer evidence, discovery/refusal,
artifact digests, retained failed attempts and `wsl2/primitive-build.tar.gz`
(9,119,525 bytes). The archive preserves executables outside Git; no off-device
backup was performed. The manifest is suitable for verifying such a copy.

Final native artifact SHA-256 values:

| Artifact | SHA-256 |
|---|---|
| Fixed fixture | `f07bd71128093a98c4dc2ead3e484c9f150990f26b192dc2bbd23c01aa37ad9a` |
| Static exec target probe | `6d07769d369bcf964b0c47344e4dd862d0fe0ffe4f36381e7d589bd4fd465980` |
| Exec oracle probe | `dacf1d76d4a6d51882447c2f80b8adcb02a366d72e942a85b1ac620b20fb0800` |
| B3 coordinator | `ec3e31d3ffa8483aa31cbecef682b656d9fcd16c7a194d17bea9719071d3691e` |

The primitive exec target PID 1070 was corroborated before exit (176,138 ns in
the final sample); its startup report and /proc inventory both showed 0/1/2.
Failed exec reported errno 2; pre-exec SIGKILL and normal exit stayed Unknown.
These are ordinary unprivileged primitive fixtures, not confined B3 execution.
The four native probe children were reaped through waitpid. Socket/pidfd handles
are scoped/disposed and the positive rendezvous removal/second-connect refusal
was checked. Build artifacts and selected raw evidence are intentionally retained.
No cgroup was allocated or changed. Integrated domain/descendant/staging cleanup,
reaping and writer takeover status is **Unknown / NOT_REPRODUCED**, never successful
by inference from a missing PID, target output, EOF or owner termination.

## 9. Handoff and smallest next action

This delivery records implemented source and tested independent components, with
native integration blocked and required fault coverage incomplete. No F-01
closure, acceptance or ready-for-security-review verdict is issued.

The next action is an **owner environment decision** establishing an authorized
delegated parent and unprivileged host-launch procedure (or separately authorizing
provisioning/an alternative mechanism). Then complete pending fault fixtures,
verify exact protected artifact/runtime/native pins, establish real recovery-parented
HELD without target initialization, and execute the full genuine matrix before
independent security review. Do not begin B4 or production execution.

Documentation delivery checks passed: 12 required files, 220 Markdown documents, 921 local links and 42 historical compatibility references; original proposal digest preserved. Whitespace checks passed and the delivery changes exactly the new evidence plus PROJECT-STATUS, handoff and work queue. Tested src/tests/native/eng/.github identities remain unchanged.
