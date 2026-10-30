# Gagamba project status

Canonical current summary; historical decisions and independent reviews remain immutable.
Status date: 2026-10-30 (Asia/Manila).

## RC2B-03-INT-01 owner architecture disposition

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
RC2B-03 B3 is subsequently ACCEPTED WITH QUALIFICATIONS; RC2B-03-INT-01 has reached its architecture gate and is BLOCKED BY CONTRACT DELTA.

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
| RC2B-03-INT-01 | **BLOCKED BY CONTRACT DELTA**; [INT-01-D01](evidence/rc2b-03-int-01.md) requires independent security review and owner approval; no topology implementation/native qualification; F-01 remains MAJOR and OPEN |
| B4 | **NOT STARTED**; dependent integration must follow RC2B-03-INT-01; separate commission required |
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
reviewed and owner-accepted WITH QUALIFICATIONS. RC2B-03-INT-01 has reached its architecture gate and is BLOCKED BY CONTRACT DELTA.
Native and Hufu gates retain their independent dispositions; no production activation
is authorized.

Maintain this file as the single current project summary; handoff/work-queue point here.
Keep accepted decisions, qualification packages and independent reviews immutable.
