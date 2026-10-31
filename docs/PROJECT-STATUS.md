# Gagamba project status

> **B4Q1-F01-R2 COMPLETE — STRICT CLEANUP TYPES (2026-10-31).**
> [Source-bound evidence](evidence/rc2b-04-b4-q1f01-r2-cleanup-type-hardening.md); tested implementation `0e89cc710fb235569191baf44d9188eda8c9f9a8`, tree `4caf454100cbcc3999467d55b95bbc7c564c0766`, from `9bae9a724aea54710a80527a9404b62cdc224882`.
> `classify_dispose` now requires an exact integer live state `1`; both classification entry points apply the shared strict durable-verdict validator. Boolean, float, string, null and malformed values cannot confirm cleanup; evidence fields and `Confirmed`/`Unknown` vocabulary are unchanged.
> Deterministic classifier **16/16**, complete `eng/tests` **35/35**; accepted INT-01 raw evidence reclassified **20 Confirmed / 12 Unknown** with zero flips.
> Independent verification of the exact patch: **SUPPORTED**, no findings.
> R1 evidence, the independent review, B4Q1-F02–F04 and R1-F02 remain unchanged; no production/journal/security-contract change; B5 not started.
> Historical blocks below are unchanged.

> **B4Q1-F01-R1 COMPLETE — CLEANUP CLASSIFICATION CORRECTED (2026-10-31).**
> [Source-bound evidence](evidence/rc2b-04-b4-q1f01-cleanup-classification.md); tested implementation `4516c24e01fbea7cc40c44b1e7c3c78109b5b438`, tree `ce1fd2f72e6bb32745bdf348184c4f87728d718a`, from baseline `37a90871fa820cf34f663355fb0d7cc918a08e16`.
> The native harness now requires the authoritative durable journal verdict (`OwnershipCleanupState.Confirmed`) plus final physical/reaping/fencing flags before reporting Confirmed; a successfully persisted Unknown verdict is not Confirmed cleanup.
> WSL2 full INT-01 matrix **32/0/0**, cleanup **20 Confirmed / 12 Unknown** in two identical runs; exactly one correction (`missing-staging-ack`: Confirmed → Unknown); `missing-domain-ack` remains Unknown; both keep physical-improvement annotations.
> B4 matrix **13/0/0**, cleanup **12/1** unchanged; WSL2 component batch **275/0/0** including B4 capture/cleanup/disposal **12/12**; deterministic classifier regression **13/13** (whole `eng/tests` suite 32/32).
> B4Q1-F02–F04 and R1-F02 unchanged; C1–C4, INT-01 and B4 acceptance preserved; no production source, journal-contract or authority change. B5-B7, VM/HCS, Hufu, native Linux/macOS and production remain gated; **B5 requires a separate bounded commission**.
> The B4 acceptance blockquote and checkpoints below are historical and remain unchanged.

> **B4 ACCEPTED WITH QUALIFICATIONS (2026-10-31).**
> Owner decision: [RC2B-04-B4 Owner Acceptance](decisions/rc2b-04-b4-owner-acceptance.md).
> Independent review: [SUPPORTED WITH QUALIFICATIONS](reviews/rc2b-04-b4-q1-independent-security-review.md).
> Tested implementation: `4bb4943df69af5f95b65300d90852c04d08a1926`, tree `6c9535013e98fb83f72d4b628a03c59b0e99a1c7`; producer evidence `ca7200937fe12f0cbccccd72ee157528630e9670`.
> B4 confined matrix **13/0/0** (8 independently rerun); INT-01 regression **32/0/0** producer; independent capture/cleanup/disposal component tests **12/0/0**; 281-file evidence manifest verified.
> **Cleanup corrected to 20 Confirmed / 12 Unknown (B4Q1-F01 erratum):** `missing-staging-ack`'s durable journal verdict is Unknown; the qualification harness's five-flag classification must be corrected before future stages use its aggregate Confirmed count as authoritative.
> B4Q1-F02–F04 accepted as bounded residuals (filesystem-identity hardening, drain-fault handling, store-failure-hook exclusion carried forward).
> R1-F02 remains globally OPEN; INT-01 acceptance and C1-C4 preserved. B5-B7, VM/HCS, Hufu, native Linux/macOS and production remain gated; **B5 requires a separate bounded commission**.
> Earlier checkpoints below, including statements that B4 review/acceptance is pending, are historical.

> **Prior B4 implementation checkpoint (2026-10-31): B4 IMPLEMENTED — READY FOR INDEPENDENT B4 SECURITY REVIEW (superseded by the acceptance above).**
> Owner authorization: GAGAMBA-RC2B-04-B4-IMPL-01, from canonical `4a181dc`.
> [Source-bound B4 evidence](evidence/rc2b-04-b4-impl-01.md); implementation `4bb4943df69af5f95b65300d90852c04d08a1926`.
> B4 confined matrix **13/0/0**; INT-01 regression **32/0/0**, cleanup **21 Confirmed / 11 Unknown** (tally corrected to **20/12** by the B4Q1-F01 erratum in the owner decision above).
> Protected capture, independent stop/disposal hardening and applicable retained-resource cleanup implemented.
> Windows M1 **989/0/267**, Execution **68/0/0**, Runtime **15/0/0**; WSL components **272/0/3** plus all three missing-helper cases **3/0/0**; direct authentication **21/0/0**.
> Q1R-01/06/07 addressed within declared scope; R1-F02 remains globally OPEN. INT-01 acceptance and C1-C4 preserved.
> Independent B4 review/owner acceptance were pending at this checkpoint (historical; both have since completed).
> Checkpoints below are historical and do not supersede this disposition.

Canonical current summary; historical decisions and independent reviews remain immutable.
Status date: 2026-10-31 (Asia/Manila).

## B4 Owner Acceptance Current Status

**B4 ACCEPTED WITH QUALIFICATIONS.**
Owner decision: [RC2B-04-B4 Owner Acceptance](decisions/rc2b-04-b4-owner-acceptance.md).
Independent Q1 review: [SUPPORTED WITH QUALIFICATIONS](reviews/rc2b-04-b4-q1-independent-security-review.md).
Tested implementation: `4bb4943df69af5f95b65300d90852c04d08a1926`, tree
`6c9535013e98fb83f72d4b628a03c59b0e99a1c7`; producer evidence `ca7200937fe12f0cbccccd72ee157528630e9670`; verified ancestry `4a181dc4` → `4bb4943d` → `ca720093` → `b95a8230`.
Native qualification: B4 confined matrix **13/0/0** producer; **8/0/0** independently rerun with the remaining five cases inspected under mechanism-level independent coverage (zero overlap); independent capture/cleanup/disposal component tests **12/0/0**; INT-01 regression **32/0/0** producer; focused independent `missing-staging-ack` reproduction; verified 281-file evidence manifest.
**Cleanup classification erratum (B4Q1-F01, MEDIUM): the corrected INT-01 cleanup tally is 20 Confirmed / 12 Unknown** (the producer report's 21/11 overstated `missing-staging-ack`, whose durable journal verdict is `OwnershipCleanupState.Unknown = 4`). Physical cleanup improvements are not promoted to Confirmed for `missing-staging-ack` or `missing-domain-ack`. The qualification harness's five-flag classification **has been corrected by B4Q1-F01-R1** ([evidence](evidence/rc2b-04-b4-q1f01-cleanup-classification.md)) and **type-hardened by B4Q1-F01-R2** ([evidence](evidence/rc2b-04-b4-q1f01-r2-cleanup-type-hardening.md)): the WSL2 INT-01 matrix now reports **20 Confirmed / 12 Unknown** under the durable-verdict contract, and future stages must use this corrected classification.
Accepted capabilities: owner-private protected stdout/stderr capture; fixed per-stream and aggregate retained-byte limits; synchronized quota accounting across concurrent drains; bounded native draining and final-drain behavior; independent stopping on the qualified capture faults; capture-reading API limited to the authenticated controller with checked offsets/counts, 16-KiB maximum reads, a single in-flight read and immutable copied results; no recovery capture-reading or release authority; independent native stopping while observation/status handling is blocked; populated-domain empty observation before owner exit; corrected `OwnerSafetyMonitor` disposal and stop dispatch; nonblocking recovery reaping; identity-checked retained-resource cleanup within demonstrated constraints; C1–C4, Option A and original deadline semantics preserved.
Findings: B4Q1-F01 accepted as a reporting erratum with the harness classification correction required; B4Q1-F02 residual check-then-remove race accepted within the existing threat model (no atomic/race-free claim; filesystem-identity hardening carried into later lifecycle/security qualification); B4Q1-F03 bounded drain-thread robustness limitation accepted (broader fault handling required before production eligibility); B4Q1-F04 gated experimental store-failure hook accepted (production exclusion/isolation required).
Inherited: Q1R-01 addressed within the B4 experimental profile; Q1R-06 and Q1R-07 fixed and independently reviewed; Q1R-08 (legacy D5_O03 flake) remains unresolved; **R1-F02 remains globally OPEN**, partially satisfied by B4 evidence; INT-01 remains ACCEPTED WITH QUALIFICATIONS; F-01 remains CLOSED within its accepted integrated-topology scope.
**This is not a production or cross-platform qualification.** WSL2 experimental scope only: no native Linux-host/macOS qualification, no isolation from malicious same-UID host processes, no B5 sealed-C3 durability/deadline reconciliation, no B6 installed-artifact/deployment attestation, no B7 comprehensive lifecycle/fault qualification, no Hufu, no VM/HCS and no general-purpose or production restricted-execution activation. `RestrictedExecutionRuntime.Create()` remains production-unavailable.
**B5 is not authorized; it requires a separate bounded commission.**
Prior checkpoints below are historical.

## INT-01 Owner Acceptance Current Status

**INT-01 ACCEPTED WITH QUALIFICATIONS.**
Owner decision: [RC2B-03-INT-01 Owner Acceptance](decisions/rc2b-03-int-01-owner-acceptance.md).
Independent Q1 review: [SUPPORTED WITH QUALIFICATIONS](reviews/rc2b-03-int-01-q1-independent-security-review.md).
Tested implementation: `dc436249fd0b0a8d70b4e4319ac27587fe857c1c`, tree
`3b5e2d9eca42e696f48f984b6ab3831e7ed90f44`.
Native qualification: **32/0/0 (8 core + 24 additional)** producer evidence; **20/0/0** independent native reruns (16 unique) + 8 independently inspected raw JSONs = 28/32 independently verified; 4 core cases producer-evidence only (inventory correction in owner decision §2).
Cleanup: 20 Confirmed / 12 Unknown with missing witnesses disclosed and categorized (owner decision §7). Outer host check has no remaining test cgroups/processes. Two production defects corrected (retained-seal ambiguity QF02-F01, root observation budget QF02-F02); failing-before evidence preserved, alongside corrected qualification-helper FD collision (QF02-F04).
WSL affected components **277/0/0**; Windows M1 **987/0/256**, Execution **68/0/0**, Runtime **15/0/0** (separate overlapping scopes; skips disclosed). Supplementary confined exec/FD/root-wait probe passes.
**F-01 CLOSED** within INT-01 integrated-topology scope (owner decision §4). Historical QF-01 F-01 remains preserved.
**B4-B7, VM/HCS, Hufu, production remain gated.** Next: B4 requires separate bounded implementation authorization.
Prior checkpoints below are historical.

## INT-01-B-QF-01 prior qualification checkpoint

**QUALIFICATION INCOMPLETE / REMEDIATION REQUIRED.**
[QF-01 evidence](evidence/rc2b-03-int-01-b-qf-01.md), tested runner source
`51f615534b468fa3977afc867e5603abdf09bdae`. Eight core cases and four added
native cases pass; twenty required additional faults remain unimplemented/unexecuted.
Historical Unknown cleanup records are unchanged. New owner-death samples await
independent recovery-final evidence and confirm cleanup; recovery-death cleanup
remains Unknown. Windows M1 987/0/256, Execution 68/0/0, Runtime 15/0/0;
WSL selected components 201/0/3, with all skips disclosed. No review-ready verdict.
ENV-01 commits are pushed normally through 988ef8f; new QF-01 commits remain local.
No production source/contracts changed; F-01 remains MAJOR/OPEN. Next work under
existing authorization: finish twenty integrated fault fixtures and applicable
native regressions. B4-B7, Hufu and production remain gated.

## INT-01-ENV-01 prior native checkpoint

**FIRST GENUINE INTEGRATED EXECUTION REPRODUCED; CORE RUNNER 8 PASSED / 0 FAILED.**
[ENV-01 evidence](evidence/rc2b-03-int-01-env-01.md), tested source
`a80b622168ba4e1a5250f06ea81f36b0995cc374`. Disposable systemd user-unit
memory/pids delegation and direct `host` launch verified on WSL2 as UID 1000
with zero checked capabilities; native preflight 12/12. Actual recovery parentage,
retained native authority, HELD, one RELEASE, target exec/exit 0, Duplicate and
positive cleanup observed. Internal evidence serialization fixed; production
source and accepted contracts unchanged. Four core case records retain Cleanup
Unknown; 24 additional cases remain NOT RUN / NOT QUALIFIED. No remaining test
processes or unit cgroups; raw evidence retained. This bounded attempt stops here.
**F-01 remains MAJOR/OPEN**, pending complete evidence, independent review and
owner acceptance. Next separately commissioned work: pending fixtures and stronger
fault/cleanup observations, then review. B4 NOT STARTED; B4-B7/Hufu/production
remain gated. All accepted qualifications and C1-C4/Option A remain.

## INT-01-B-IMPL-01 prior implementation checkpoint

**PARTIAL SOURCE DELIVERY — BLOCKED_ENVIRONMENT / NOT_REPRODUCED.**
[Source-bound evidence](evidence/rc2b-03-int-01-b-impl-01.md).
Tested source `17a61f5be2602445fdb195a7ef76e9d88f14640e`, tree
`f273f07d6da3700d3af04c3703715a48667619b5`. The approved Alternative B composition
is implemented in experimental source and builds; genuine integrated B3 HELD,
native retention, release, death/race monitoring, reaping and writer takeover have
not been reproduced. This commission is not complete or security-accepted.
The core native runner has eight unexecuted cases and 24 additional pending cases.

Windows: M1 985/0/95, focused 18/0/24, Execution 68/0/0, Runtime 15/0/0.
WSL2: authentication 21/0/0, component contracts/monitor/fence 85/0/0,
exec/FD oracle 4/0/0. Counts overlap; primitive/managed passes are not integrated
confinement, native Linux-host qualification or independent security review.

The owner confirmed no current authorized delegation or host-launch procedure.
Read-only discovery found a writable shared systemd user subtree, but execution
remains in `/init.scope`; the common ancestor's migration file is not writable.
The shared subtree was not adopted. No delegation provisioning, cgroup permission
change, migration or privileged fallback occurred. A separate owner environment
decision is required before native integration can resume.

**F-01 MAJOR/OPEN**; no closure or self-approval. F-03 diagnostics are corrected and
regression-tested; F-07 is clarified; F-08 has passing exec/FD primitive controls.
F-02 and Q1-F03/F04/F05 and confined F-08 still need genuine integrated evidence.
Preserve C1–C4, Option A, uncertainty and all accepted qualifications. M-01 CLOSED;
R1-F02 globally OPEN. **B4 NOT STARTED**; B4–B7, Hufu and production remain gated.
Next: an owner decision establishing the environment, then pending fault fixtures
and the actual matrix before independent review. Stop at that boundary.

## RC2B-03-INT-01 owner architecture disposition (prior decision checkpoint)

The following decision-stage description is historical; implementation status above supersedes its next-work pointer.

**ALTERNATIVE B APPROVED WITH QUALIFICATIONS FOR BOUNDED EXPERIMENTAL IMPLEMENTATION**
under **GAGAMBA-RC2B-03-INT-01-D01-OWNER-01**;
[owner decision](decisions/rc2b-03-int-01-d01-owner-decision.md).
**Alternative A DECLINED AS PROPOSED.** No RC-1 process-ownership amendment is approved
or required for B. The original [D01 gate](evidence/rc2b-03-int-01.md) and
[Q1 review](reviews/rc2b-03-int-01-d01-independent-review.md) remain historical and unchanged.
Q1 is SUPPORTED WITH QUALIFICATIONS, recommends B and discloses same-conversation
gate/reassessment provenance. The architectural question is resolved for experimental
implementation; the next task is to build and qualify B, not reconsider A.

Preserve recovery-parented ownership, verified subreaping/reaping and retained native
domain/staging authority. The owner alone retains preparation capability, journal
writer and sole RELEASE writer. Require an owner-created protected SOCK_STREAM
listener, fresh direct controller/owner connection, exact expected PID/UID/GID and
feature-tested fail-closed `SO_PEERPIDFD` checks, immutable original identity/session,
one-use direct challenge and cryptographic confirmation, bounded stream framing and
sequencing, no resumed authority and no release secret/endpoint through recovery.
Real B3 must run under surviving B2 recovery; safety monitoring/native stop remain
independent of preparation, owner exchanges, capture/observation and controller callbacks.

Retain C1–C4 owner-local arbitration: accepted loss blocks later release; loss observed
before the final prerequisite check blocks release; an already committed release keeps
its original decision and subsequent safety-stop obligations. A pidfd poll/death event
is not atomic with physical RELEASE. No absolute wall-clock death-before-write
exclusion is claimed; stronger ordering needs a separately reviewed enforceable fence.
Preserve Option A, original deadlines, one RELEASE and no reconstructed authority.

Exact pinned experimental artifacts/runtime/native closure, protected bootstrap,
kernel features, immutable expected identities, unprivileged execution and already
authorized delegated cgroups are mandatory. Refuse unmet prerequisites. Full B6
production attestation is not required to begin; the caller-populated B3 profile is
not production attestation. No public provider activation is authorized.

**Next: GAGAMBA-RC2B-03-INT-01-B-IMPL-01 — AUTHORIZED FOR BOUNDED IMPLEMENTATION,
NOT STARTED.** This decision implements nothing and qualifies no security claim.
Q1-F01–F06 remain recorded with owner dispositions in decision §8. B3 F-01 remains
MAJOR/OPEN pending genuine integrated process tests, independent review and owner
acceptance; F-02/F-03/F-07/F-08 remain unresolved. M-01 CLOSED; R1-F02 globally OPEN.
No B4/B5/B6/B7, Hufu or production activation is authorized. **B4: NOT STARTED.**
Production restricted execution remains unavailable. Stop after this decision delivery.

## RC2B-03 B3 current disposition

**RC2B-03 B3 ACCEPTED WITH QUALIFICATIONS** under GAGAMBA-RC2B-03-B3-OWNER-01;
[owner decision](decisions/rc2b-03-b3-owner-acceptance.md).
[Independent review](reviews/rc2b-03-b3-independent-review.md): **SUPPORTED WITH
QUALIFICATIONS**, no blocking defects within the accepted limited B3 scope.
No further B3 implementation remediation is required for that acceptance.
Verified linear binding: baseline `4067f81a8eb095e010db64afd67fd60ddcbccbd4` ->
M-01 correction `880babe9c170756e007dc2b563f92a1e7466990c` -> initial implementation
`fcfe289ea10b5240054f8d985678ac3361d7ca9f` -> corrected implementation
`a67a640c3de69ee268842fda8ffd189f29e6c9d8` (tree
`05535a2592a58565650d59994cb1fb1af099ef60`) -> evidence delivery
`3b691c290bcdaf9c01191eb2ed8e552f912eed40` -> review
`15ae8df593907c342f4dad49b1d8590353e0743b` (tree
`10599d5f4fe86ea1f6c0652d1a277fabfc37f39d`).

Accepted behavior: real owner-origin preparation identity, verified staged closure,
durable preparation before mint, owner-local capability/journal writer, genuine
coordinator HELD and no target initialization before release in the fixed experimental
fixture, original deadlines/generation, implemented R1-F03 Option A, C1–C4 arbitration,
exactly one physical RELEASE, retained Duplicate/concurrent refusal, conservative
ambiguity/lost-ack handling, controlled execution with separate native outcome facts,
corrected exec/root journal and stop/abort exchange ordering, and **M-01 CLOSED**.

Evidence provenance: the reviewer independently executed Windows M1 parallel
981/0/91, focused B3 31/0/3, B1 45/0/0, Execution 68/0/0 and Runtime 15/0/0
(passed/failed/skipped; overlapping suites). Available coordinator, fixture and
seccomp artifact hashes were independently checked. Native WSL2 positive and
process-failure results are **producer evidence, reviewed only**, not independently
reproduced: the reviewer lacked operator-delegated cgroups and did not alter the host.
They are credible and source-consistent, but do not qualify a native Linux host.
[CI 40987712653](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40987712653)
succeeded on **attempt 2**; rerun history and failed producer attempts are preserved.

**F-01 MAJOR: ACCEPTED DEFERRED INTEGRATION BLOCKER.** The positive fixture uses a
controller-owned in-process `M1RecoveryScope`, not B2's independently surviving
recovery process. Killing the unrelated B2 recovery process while B3 continues
demonstrates topology separation, not safe release under genuine integrated recovery
loss. No release capability/bearer passed through recovery. Acceptance establishes
neither integrated supervision, independent recovery ownership of this transaction,
release safety on integrated recovery loss, nor complete native stop/cleanup after
controller/recovery failure. F-01 is not waived and must be resolved before further
integrated live-release qualification, topology-dependent B4, Hufu or production.

| Finding | Preserved disposition |
|---|---|
| F-02 MINOR | `RecoveryAlive` is controller/parent liveness; bind the actual recovery lifeline during integration. Naming is not independent recovery evidence. |
| F-03 MINOR | Invalid-length correlation after stop/retention may yield arbiter `Mismatch` instead of `InvalidArgument`; diagnostic only, no extra authority. Focused correction/regression tests required; preserve Option A. |
| F-04 OBSERVATION | Caller-populated trusted-internal experimental profile; B6 must provide reviewed installation pins. |
| F-05 OBSERVATION | HELD uid/seccomp/capability facts are kernel-sourced but coordinator-mediated, not independent trusted-host observations. |
| F-06 OBSERVATION | Prepared descriptor truthfully reports `Unavailable`; B6 must supply a qualified descriptor before production registration. |
| F-07 OBSERVATION | Recovery-death test proves topology separation; clarify/rename alongside genuine integrated recovery-loss testing. |
| F-08 OBSERVATION | Coordinator clears exec-confirmation `FD_CLOEXEC`, leaking an extra target fd and weakening the oracle. Still uncorrected; fix before affected FD 0–2 native qualification claims. |

**RC2B-03-INT-01-B-IMPL-01: AUTHORIZED FOR BOUNDED IMPLEMENTATION, NOT STARTED.**
The [D01 owner decision](decisions/rc2b-03-int-01-d01-owner-decision.md) selects B with
the constraints in the current summary above and decision §§3–9. Build and qualify
the real recovery-parented B3 lifecycle, direct authentication, live independent
monitoring and retention barriers; test actual process failures and correct the
commissioned F-02/F-03/F-07/F-08 follow-ups. The historical gate and Q1 review remain
unchanged. Architecture approval does not close F-01 or authorize B4. No implementation
or integrated native qualification occurred in this owner-decision task.

**Restrictions preserved:** no integrated B2/B3 topology or fully independent B3 recovery
supervision; no hostile populated-domain stopping or complete failure cleanup; no B4
protected capture/complete independent stop, B5 sealed C3 durable deadlines/reconciliation,
B6 installed-artifact/deployment attestation or B7 native lifecycle/fault qualification;
no independent native Linux-host qualification, same-UID hostile-process isolation,
Hufu D4a/D4b eligibility or general-purpose/production restricted execution.
`RestrictedExecutionRuntime.Create()` remains production-unavailable. The test-only
fixture is not a production provider; cooperative stopping, domain emptiness and owner
`Stopwatch` time do not qualify hostile stopping, complete cleanup or sealed C3.

## RC2B-02 B2 current disposition

**RC2B-02 B2 ACCEPTED WITH QUALIFICATIONS** under GAGAMBA-RC2B-02-B2-OWNER-01.
The [owner decision](decisions/rc2b-02-b2-owner-acceptance.md) accepts the recovery
bootstrap and process-lifetime infrastructure within its demonstrated scope. Final
tested source `7396012bc07c357a49bbdd0c3791ce06808ca9ab` (tree
`e009fb88d6cd736660c2055b4ed7743d59976b78`), evidence delivery
`235b15b3d2cee828176c18b02685f427f3423ff0`, and independent review
`0602f32f94593e7eebe6e02eccbb1e71b1b7ae60` (tree
`7984e0d4ce25dbabbedf9669a3a30821179bb5df`) form the verified linear delivery chain.
[Independent review](reviews/rc2b-02-b2-independent-review.md): **SUPPORTED WITH
QUALIFICATIONS**, with no blocking or major findings. No further B2 implementation
remediation is required for owner acceptance of the demonstrated scope.

The reviewer independently executed 699 passing Windows tests: 616 focused M1,
68 Execution and 15 Runtime (0 failed / 0 skipped). The reviewer inspected
[producer WSL2 evidence](evidence/rc2b-02-b2.md) without independently reproducing
the Linux process cases. The producer's 24 passing real-process cases include the
two empty-domain cases; those two are not additional distinct tests. Producer WSL2
focused M1 results are 70/0/0 and overlap Windows coverage. [Delivery CI
40873410775](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40873410775)
completed with 10 passing jobs and one conditional skipped job; it does not establish
independent native Linux-host qualification.

**M-01: CLOSED by B3** at `880babe9`, independently supported by B3 review §E.3.
The B2 decision retains its historical OPEN disposition. Required disposed-state guards for
`RecoveryConnection.Inspect()` and `ObserveAsync()`, concurrent disposal verification
and focused regression tests were completed before B3 operationally consumed these
methods, preserving cancellation and loss-observation semantics.

Acceptance does not establish native Linux-host qualification outside WSL2;
populated restricted-domain stopping; native stop enforcement when recovery dies;
complete R1-F02 runtime enforcement; B3 live preparation/release composition;
R1-F03 Option A release-admission implementation; B4 protected capture/complete
independent stop; B5 sealed C3 deadline/reconciliation; B6 deployment/artifact
attestation; B7 lifecycle/failure qualification; same-UID hostile-process isolation;
Hufu D4a/D4b eligibility; or production restricted-execution activation.
The two-second volatile B2 stop obligation is not a sealed C3 durable StopRecord.
Empty-domain kill/readback proves neither populated stopping nor complete cleanup.
The B1 public facade remains safely disabled for live restricted execution, and no
production security claim is expanded.

**RC2B-03 B3: implemented under a separate bounded commission** (see the current
disposition above and the [evidence](evidence/rc2b-03-b3.md)); the B3 implementation
connects the accepted B1 facade to real owner preparation in a controller-owned scope and a
one-shot release composition, preserving the identity/evidence, C1-C4, approved
Option A, no-repeated-RELEASE and recovery-without-release-authority requirements.
Bounded local WSL2 development/qualification was used; no independent native Linux
host has been selected and none is claimed. B4/B5/B6/B7 and Hufu gates remain
separate.

## RC2B-01 B1 current disposition

**ACCEPTED WITH QUALIFICATIONS** by GAGAMBA-RC2B-01-B1-OWNER-01 at
[the owner decision](decisions/rc2b-01-b1-owner-acceptance.md), covering only the separate
restricted-execution public facade's control contract, safe-refusal behavior and deterministic
local semantics. Contract code and deterministic tests are implemented at
`764045fed4a01c4c1160865bd9da53fa3af2b954` (tree `646484a2ca5017d2439d7460dda36fc1e1d2c23f`),
delivered at `bdd124db5ed6638746d4810b293cf497e61f86e0`, and independently reviewed at
`beb3fe4ee53d1c2d9a6cf97af83ca390d6137fef`
([review](reviews/rc2b-01-b1-independent-review.md), verdict **SUPPORTED WITH QUALIFICATIONS**).
The separate restricted provider refuses all live selection without installed qualification;
no successful native preparation/release or recovery is exposed. Final local verification:
630 distinct tests passed / 0 failed / 0 skipped (46 new B1 cases), Release solution build
0 warnings / 0 errors; CI run `40793941938` completed with 10 jobs passed and one conditional
launch job skipped. RC2A public regression subset: 48/0/0; frozen source/API/vectors and
native tree unchanged.

R1-F02 local stop/publication ordering is implemented and independently reviewed;
owner/runtime and process-loss enforcement remains **OPEN**. **R1-F03 Option A is APPROVED**
as the semantic owner contract for pre-retention correlation-mismatch refusal (see the
decision §4). At the B1 checkpoint, release admission remained unwired and returned
`LiveReleaseNotImplemented`; B3 subsequently implements Option A in its bounded scope.
Remaining B2-B7 obligations, sealed C3 clock/recovery, deployment
manifests/native pins, lifecycle qualification and Hufu remain separately gated. No native
workload, package publication or release tag was created.

The source-bound [B1 handoff](evidence/rc2b-01-b1.md) contains the API inventory, R1-F02
ordering, R1-F03 decision table/recommendation, exact commands/counts, adaptations and
remaining gates. B2 is subsequently ACCEPTED WITH QUALIFICATIONS as recorded above;
RC2B-03 B3 is subsequently ACCEPTED WITH QUALIFICATIONS; RC2B-03-INT-01 has approved Alternative B source implementation checkpoints; integrated native qualification is BLOCKED_ENVIRONMENT / NOT_REPRODUCED, and required fault coverage remains incomplete.

## Accepted component baseline

RC2A-03 is **ACCEPTED WITH QUALIFICATIONS** by GAGAMBA-RC2A-03-OWNER-01 at
`1ce5c37f25c3bf49f55fc6a31a930f8430358d24`, root tree
`6b7605ee89ec9545926143875cfd3e2410c8ac48`, parent / accepted R1
`271e34b45fa504f6bb7ef7359c4f2f1a9649e49d` (R1 tree
`f3f63129be98c5c9f9efba3302da331ae65139ee`).

Authority: [owner decision](decisions/restricted-consumer-contract-rc2a-03-owner-acceptance.md).
The owner decision's §§5–6 are the complete qualification/exclusion register.
Q2 report `a58f1b2a07d959c763bf6d08143797846948fe74`, final receipt
`e060c91225274f13d53421b613a5736225ef6de4` (tree
`23bd0006ba1178f3b84ee510d89c5c0179cddc91`) supports all 13 claims with qualifications
and closes Q1 F01–F04 **for R1**. Q1 `dd7e47d2c5f75af126ce4a081013bf21182d73ee`
remains authoritative historical evidence against original `c946df0`.
Review/assessment histories remain separate, linked by immutable commit identity.

Exact protected identities:

| Object | Git identity |
|---|---|
| src | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| native (preservation only) | `85631d2f04512376621d3d09f51d67fd90f45335` |
| tests | `9c906b349b0bf0cffdee331fb5700f9b7828c199` |
| Frozen API document | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` |

## Security and evidence boundary

Accepted scope is the passive restricted consumer value/codec/reference component.
Successful decoding grants no execution authority and proves no establishment or isolation.
No digest or passive reference establishes producer/provider provenance, temporal/deadline/
freshness/liveness, release/retained-state authority, sandbox confinement, recovery or lifecycle.
R1-F04 and D4a enrolled/synthetic versus D4b actual provider-origin timing-source limits remain.

Windows is primary; the RC2A-02A/B managed assertion set has separately preserved WSL2 evidence.
RC2A-02C's focused reference/surface qualification is Windows-only. WSL2 is not native Linux;
native Linux/macOS **consumer qualification** remain NOT RUN. Earlier native-track evidence
retains its own separately scoped dispositions and is not imported as consumer qualification.

Carry forward: two digest/recipe-bound large vectors; no binary/package/distribution/deployment
reproducibility or cross-process hash-determinism guarantee; corrected RC2A-01 R2A-F03 origin
and disclosed historical citation error; 40-count erratum; authorization audit-trail and review
method disclosures; canonical-rigidity correction and precise/broad-filter accounting; preserved
failed attempts and B R1/Q2 corrections; bounded re-encode reasoning; differing raw CRLF/LF
versus equal normalized/JSON representations; qualified E2/E6 and no mandatory consolidated run.
Existing R2A-F01/F02/F04 and R1-F01/F05 dispositions remain scoped.

## Milestones and outstanding work

| Item | Current disposition |
|---|---|
| RC-1 / RC1-A | Accepted with recorded qualifications |
| RC2A-01 implementation | Accepted with follow-up; follow-ups retain their scoped later dispositions |
| RC2A-02 | Accepted with qualifications at `9519e7cb612af6ff69c93c0d128553138533d9fd` |
| RC2A-03 | Accepted with qualifications at the exact owner decision above |
| RC2A-04 local package/clean consumer/mixed graph | **ACCEPTED WITH QUALIFICATIONS**; [owner decision](decisions/rc2a-04-owner-acceptance.md), qualifications unchanged |
| RC2B / RC-2B facade | B1 contract/refusal boundary **ACCEPTED WITH QUALIFICATIONS** ([decision](decisions/rc2b-01-b1-owner-acceptance.md)); R1-F03 Option A APPROVED and implemented within accepted B3 scope; R1-F02 native enforcement OPEN; native/lifecycle gates remain unqualified |
| RC2B-02 (B2) | **ACCEPTED WITH QUALIFICATIONS**; [owner decision](decisions/rc2b-02-b2-owner-acceptance.md); M-01 subsequently CLOSED by B3; native/operational exclusions retained |
| RC2B-03 (B3) | **ACCEPTED WITH QUALIFICATIONS**; [owner decision](decisions/rc2b-03-b3-owner-acceptance.md); M-01 CLOSED; Option A and one-shot fixture accepted; F-01 MAJOR deferred integration blocker; F-02–F-08 retained |
| RC2B-03-INT-01 | **PARTIAL SOURCE DELIVERY; BLOCKED_ENVIRONMENT / NOT_REPRODUCED**; [B-IMPL-01 evidence](evidence/rc2b-03-int-01-b-impl-01.md); full fault coverage and genuine native qualification pending; F-01 MAJOR/OPEN |
| B4 | **ACCEPTED WITH QUALIFICATIONS** ([owner decision](decisions/rc2b-04-b4-owner-acceptance.md)); WSL2 experimental scope; cleanup classification corrected to 20/12 (B4Q1-F01 erratum); B4Q1-F02–F04 carried as bounded follow-ups; B5 remains separately gated. |
| HUFU-R / R1-F06, AE4-B01 and Hufu integration | OPEN/BLOCKED; no integration authorization |
| Native C1 review, C3/C4 and broader native track | Separate commissions/dispositions; not accepted by RC2A-03 |
| GAGAMBA-INTEGRATION-01 | Stage 2A preservation verified; accepted baseline and 23 preservation refs published under Stage 2B authorization; final verification and CI outcome recorded separately |

## Repository integration state and next owner decision

The owner explicitly authorized GAGAMBA-INTEGRATION-01 Stage 2B on 2026-10-28:
publication of the exact 23 preservation refs and their histories, disclosed low-sensitivity
path details, controlled workflow suspension/restoration, accepted-lineage fast-forward,
this three-file documentation child and one post-push CI run. The other 77 provenance
finding groups repeat previously public account/path/hostname information.

The 23 checkpoint/review/assessment refs and accepted baseline were published and
verified before this documentation commit was prepared for installation. Seven review
and two assessment branches retain their independent histories; none was merged into main.
The accepted implementation/decision baseline remains exactly
`1ce5c37f25c3bf49f55fc6a31a930f8430358d24`. The integration status commit `12578dd41ce23b2e890c6f6b4a027db074f31f50` is its
direct child. Subsequent link maintenance and RC2A-04 qualification tooling retain
the production, native, API and test identities above unchanged.
Moving historical native code into main grants it no broader acceptance.

Stage 2A recovery was verified from the Git bundle, cold administrative metadata and all
72 worktree snapshots. The owner accepted the same-physical-disk backup limitation for
this non-destructive integration. Backup data and local-only work remain private.
No branch deletion, worktree retirement, repository migration, tag or package publication
is authorized. A verified off-device copy remains required before destructive retirement.

Final remote commit/ref verification, restoration of the three workflow states, and the
single CI run's exact final-main SHA binding and outcome are recorded in the separate
Stage 2B completion receipts and GitHub Actions. This summary does not substitute for
those results. Ordinary CI build/test/pack/probe results do not establish new consumer,
native-runtime, installed-package or distribution qualification.

RC2A-04 was separately commissioned on main. Exact local package source is
`82cf8983a61f60a95d0aed0233ef0efac985148b` (tree
`d37ad20f2d7e5ee4ee705e4c78b766df32b61f0c`); candidate version is
`0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b`.
The clean Windows package consumer passed 1,227 assertions using 21 exported vectors;
Windows and WSL2 each passed 48 public plus 457 precise portable regressions (0 failed,
0 skipped). Five coherent local packages and symbols were inspected. The mixed graph
with Hufu core preview.11 restored and loaded managed types without native activation;
the Hufu Sandbox package is unavailable, so its integration graph remains unqualified.
V19/V20 remain recipe/digest-bound, native Linux/macOS consumer qualification remains
NOT RUN, and no binary reproducibility or execution-authority claim is added.
No package was published. Automatic CI for the evidence commit is recorded separately.

RC2A-04 is subsequently ACCEPTED WITH QUALIFICATIONS in the linked owner decision, and
RC2B-01 B1 is then ACCEPTED WITH QUALIFICATIONS with R1-F03 Option A approved as a semantic
contract in [the B1 owner decision](decisions/rc2b-01-b1-owner-acceptance.md). B2 is now
ACCEPTED WITH QUALIFICATIONS in [the B2 owner decision](decisions/rc2b-02-b2-owner-acceptance.md).
RC2B-03 B3 is subsequently implemented under a separate bounded commission (current
disposition above; implementation `fcfe289e`, held-phase fix `a67a640c`), independently
reviewed and owner-accepted WITH QUALIFICATIONS. RC2B-03-INT-01 has approved Alternative B source implementation checkpoints; integrated native qualification is BLOCKED_ENVIRONMENT / NOT_REPRODUCED, and required fault coverage remains incomplete.
Native and Hufu gates retain their independent dispositions; no production activation
is authorized.

Maintain this file as the single current project summary; handoff/work-queue point here.
Keep accepted decisions, qualification packages and independent reviews immutable.
