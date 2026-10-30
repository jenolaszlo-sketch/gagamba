# RC2B-03 B3 owner acceptance, findings disposition and integration handoff

Commission: GAGAMBA-RC2B-03-B3-OWNER-01.
Date recorded: 2026-10-30 (Asia/Manila). Documentation-only owner decision.

## 1. Owner disposition

**RC2B-03 B3 ACCEPTED WITH QUALIFICATIONS.**

The owner accepts the demonstrated real owner preparation, HELD establishment,
release arbitration, R1-F03 Option A implementation and one-shot fixture execution
within their reviewed scope. The independent verdict is **SUPPORTED WITH
QUALIFICATIONS**. The reviewer reported no blocking defects within the accepted
B3 scope. No further B3 implementation remediation is required for acceptance of
this limited scope. F-01 remains a major deferred integration blocker, not waived.

Acceptance does not extend to an integrated B2/B3 recovery topology or production
restricted execution. Prior [B1](rc2b-01-b1-owner-acceptance.md),
[B2](rc2b-02-b2-owner-acceptance.md), [RC-1](restricted-consumer-rc1-owner-acceptance.md)
and [C1–C4](gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md) decisions retain
their original scope. The [RC-1 recovery and release contracts](../design/restricted-consumer-contract-rc1.md)
remain binding.

## 2. Verified Git binding and source identities

| Identity | Verified value |
|---|---|
| Repository / branch | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting HEAD and remote `main` | `15ae8df593907c342f4dad49b1d8590353e0743b` |
| B3 baseline | `4067f81a8eb095e010db64afd67fd60ddcbccbd4` |
| M-01 correction | `880babe9c170756e007dc2b563f92a1e7466990c` |
| Initial B3 implementation | `fcfe289ea10b5240054f8d985678ac3361d7ca9f` |
| Initial implementation tree | `d6f66a4abed83f7f6ffe4468154e9cc17114cbed` |
| Final corrected implementation | `a67a640c3de69ee268842fda8ffd189f29e6c9d8` |
| Final implementation tree | `05535a2592a58565650d59994cb1fb1af099ef60` |
| Evidence delivery | `3b691c290bcdaf9c01191eb2ed8e552f912eed40` |
| Evidence delivery tree | `74641749d8bd5459d47d48b6e3c90898dfdb3bd4` |
| Independent review | `15ae8df593907c342f4dad49b1d8590353e0743b` |
| Review tree | `10599d5f4fe86ea1f6c0652d1a277fabfc37f39d` |
| Producer evidence | [B3 evidence](../evidence/rc2b-03-b3.md), blob `fa2efadf3f64e37dbec07f98a0f9d2bf5199108d` |
| Independent report | [B3 review](../reviews/rc2b-03-b3-independent-review.md), blob `c841fe6133e8f078051916904a6d9bc3d91b0a1b` |

Verified before writing: `4067f81` -> `880babe` -> `fcfe289` -> `a67a640`
-> `3b691c2` -> `15ae8df` is a linear chain; every successor has its predecessor
as sole parent and every listed commit is an ancestor of starting HEAD. Local and
remote `main` agree. Delivery includes a test-only evidence-retention change;
production source is unchanged from the corrected implementation. The review
commit adds only its report. This decision is a documentation child of that review.

Protected starting trees: `src` `c28e8dc345b05549c45ea93be1958aa88141b1da`;
`tests` `ad6f2796ca81f489d8daa9c89f69a44fd68228e5`;
`native` `85631d2f04512376621d3d09f51d67fd90f45335`;
`.github` `eb61716a96da2a23704365df6ec2d7fafa0f8cf9`;
`eng` `0a6dba6709001bbd049595c1200ec9ee471f2b1d`.
The final commit/tree and remote receipt are reported in the delivery handoff.

## 3. Accepted demonstrated functionality

- Real owner-origin preparation identity and verified staged source closure.
- Durable preparation before capability mint; owner-local preparation capability
  and journal writer.
- Genuine coordinator HELD establishment and no target initialization before
  release in the fixed experimental fixture.
- Original owner deadlines and generation binding.
- Implemented R1-F03 Option A: pre-retention mismatch refuses only that call,
  without consumption, retention, correlation replacement, physical RELEASE or
  deadline reset. Absent correlation and present all-zero 32 bytes remain distinct.
- C1–C4 release/stop arbitration semantics, including retained `Duplicate` and
  concurrent refusal, with exactly one physical RELEASE attempt in the positive case.
- Conservative handling of ambiguous persistence and lost acknowledgements;
  neither permits release retry or authority reconstruction.
- Controlled execution and separately reported native outcome evidence. Delivery,
  exec, root status, capture, domain emptiness and cleanup remain distinct facts.
- Corrected exec/root journal ordering through separate durable mutations.
- Corrected stop/abort exchange ordering through serialized owner exchanges and
  bounded settlement of the submitted stop before abort.
- Closure of B2 finding **M-01**: guarded descriptor check/use/close serialization,
  disposed-state refusal and concurrent-disposal regression coverage, preserving
  cancellation and loss-observation semantics (review §E.3).

These accept the reviewed fixture and component semantics; they do not establish
complete sealed C3 durable deadline/reconciliation or integrated failure supervision.

## 4. Evidence provenance

The independent reviewer executed the following on Windows; these are recorded
review executions, not new executions by this documentation commission. Counts
overlap between suites and are not summed as distinct tests.

| Reviewer execution | Passed | Failed | Skipped |
|---|---:|---:|---:|
| M1 Windows parallel phase | 981 | 0 | 91 |
| Focused B3 tests | 31 | 0 | 3 |
| B1 tests | 45 | 0 | 0 |
| Execution tests | 68 | 0 | 0 |
| Runtime tests | 15 | 0 | 0 |

The reviewer independently checked available B3 coordinator, fixed target, WP04
coordinator and seccomp artifact hashes (review §B.4). Hash verification is distinct
from executing the artifacts.

The native WSL2 positive execution and process-failure tests were **not independently
reproduced**. The reviewer lacked the necessary operator-delegated cgroup environment
and did not alter the host to recreate it. Producer native results are credible and
consistent with reviewed source, but remain **producer evidence, reviewed only**.
They are neither independently executed results nor native Linux-host qualification.

[CI run 40987712653](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40987712653)
for delivery `3b691c290bcdaf9c01191eb2ed8e552f912eed40` succeeded on **attempt 2**.
GitHub reports `run_attempt: 2`, `conclusion: success`; the actual rerun history is
preserved, not recast as first-attempt success. CI is an execution-status record,
not independent native reproduction. Original failed producer attempts and their
corrections remain in evidence §9; no evidence or review is rewritten.

## 5. F-01 major topology finding and owner disposition

**F-01 MAJOR: ACCEPTED DEFERRED INTEGRATION BLOCKER.**

The positive B3 fixture uses a controller-owned, in-process `M1RecoveryScope`;
it does not run through B2's independently surviving recovery process. The existing
recovery-death test terminates an unrelated B2 recovery process while the
controller-owned transaction continues. This demonstrates topology separation,
not safe release under genuine integrated recovery loss.

Consequently B3 acceptance does not establish:

- Integrated controller/recovery/owner supervision.
- Independent recovery ownership of the B3 release-capable transaction.
- Qualified release under failure of the intended integrated recovery actor.
- Complete native stop and cleanup after controller/recovery failure.

No release capability or bearer was passed through recovery. F-01 does not block
acceptance of the demonstrated B3 fixture. However, **F-01 must be resolved before
further integrated live-release qualification, B4 integration dependent on that
topology, Hufu integration or production activation**. The owner does not waive F-01.

## 6. Other independent findings and dispositions

| Finding | Classification | Owner disposition / required follow-up |
|---|---|---|
| F-02 | **MINOR** | `RecoveryAlive` currently means controller/parent-process liveness, not an independent recovery actor. Correct its semantic binding with the actual recovery lifeline during integration. Misleading naming is not evidence of independent recovery. |
| F-03 | **MINOR** | Invalid-length correlations after stop or retention can yield owner-arbiter `Mismatch` rather than `InvalidArgument`. Diagnostic imprecision grants no additional release authority; the public operation layer validates correctly. Carry forward a focused correction and regression tests, preserving approved R1-F03 Option A. |
| F-04 | **OBSERVATION** | The experimental profile is caller-populated within a trusted internal path, not a compiled-in immutable production installation. B6 must provide reviewed installation pins. |
| F-05 | **OBSERVATION** | HELD uid/seccomp/capability facts are kernel-sourced but coordinator-mediated, not independently observed by the trusted host. Preserve that provenance. |
| F-06 | **OBSERVATION** | The prepared descriptor truthfully reports `Unavailable` because no production provider is installed. B6 must supply a real qualified descriptor before production registration. |
| F-07 | **OBSERVATION** | The recovery-death test demonstrates separation of two topologies; it is subsumed by F-01. Rename or clarify it when adding the genuine integrated recovery-loss test. |
| F-08 | **OBSERVATION** | The fixture coordinator clears `FD_CLOEXEC` on the exec-confirmation descriptor, giving the target an extra descriptor and delaying the EOF oracle until target exit. This weakens fidelity to the intended target FD 0–2 boundary. Correct the fixture before subsequent native qualification relies on that boundary; the current oracle is **not corrected** by this decision. |

Each finding remains independently recorded at its original severity. Acceptance
does not close these follow-ups or promote observations into qualified guarantees.

## 7. Next engineering milestone

**RC2B-03-INT-01: Integrated controller/recovery/owner supervision.**

Status: **NEXT PROPOSED IMPLEMENTATION, NOT STARTED.**

This integration must precede B4 work that depends on the final process topology.
A future implementation commission must establish:

1. One real, coherent controller/recovery/owner lifecycle.
2. A recovery process that remains independently alive when the controller exits.
3. Retained recovery/stop authority over the actual release-capable owner and domain.
4. A private controller-to-owner release association that does not grant recovery
   authority to forge, replay or duplicate release requests.
5. Owner awareness of genuine recovery-lifeline loss.
6. Correct controller, recovery and owner liveness identities.
7. Safe behavior under each single-process failure and relevant concurrent failures.
8. No unauthorized RELEASE after the genuine recovery obligation is lost.
9. No reconstruction of release capability from passive records, PIDs, paths or reconnect.
10. Preserved C1–C4 and R1-F03 guarantees.
11. A real integrated recovery-death test, replacing the misleading separation demonstration.
12. Accurate native stop, cleanup and uncertainty evidence.

The review identifies alternatives: a controller-parented transaction owner with
recovery retaining independent lifeline duties, or separately authenticated
controller-to-owner communication under applicable installation gates. These require
engineering assessment and are **not approved implementation decisions**. The future
commission must select the simplest architecture preserving original RC-1 authority
and recovery contracts. Recovery must not become a release proxy or receive a bearer.
If the selected topology changes accepted RC-1 security semantics, stop for a scoped
design delta and independent review.

Include F-02 and F-07 in integrated topology qualification. Schedule F-03 diagnostic
correction and F-08 fixture correction as small bounded changes before affected
qualification claims. F-04–F-06 retain their provenance and B6 deployment gates.
This decision does not authorize implementation. **B4: NOT STARTED**; do not start it
automatically.

## 8. Preserved restrictions

B3 acceptance does not establish:

- An integrated B2/B3 release-capable topology or fully independent recovery
  supervision of B3.
- Native stopping of hostile populated domains or complete native failure cleanup.
- B4 protected capture and complete independent stop.
- B5 sealed C3 durable deadlines/reconciliation. The retained owner `Stopwatch`
  clock and B2 volatile stop obligation are not that implementation.
- B6 installed artifact and deployment attestation.
- B7 native lifecycle and fault qualification.
- Independent native Linux-host qualification; WSL2 is not native Linux.
- Same-UID hostile-process isolation.
- Hufu D4a/D4b eligibility or Hufu integration authorization.
- General-purpose or production restricted execution.

`RestrictedExecutionRuntime.Create()` remains production-unavailable. The test-only
fixture and positive results are not a production provider. Cooperative fixture
stopping is not hostile-population enforcement; domain emptiness is not complete
cleanup. R1-F02 is not globally closed by this acceptance.

## 9. Documentation delivery and validation

Authorized changes are this decision, [PROJECT-STATUS](../PROJECT-STATUS.md), and
stale current pointers in [handoff](../handoff.md) and [work queue](../work-queue.md).
Prior evidence, independent reviews, accepted decisions, source, tests, native and
CI trees remain unchanged. Existing untracked files are preserved.

Validate source/review identity and ancestry, the bounded accepted scope, every
finding's disposition, evidence provenance, M-01 closure, and NOT STARTED status for
RC2B-03-INT-01 and B4. Run `eng/verify-preparation.ps1` and `git diff --check`, and
verify only the four authorized documentation files changed. Actual check results
and final commit/tree/remote identities are returned in the completion handoff.

Commit directly to canonical `main` and push without force. No branch/worktree,
implementation, package publication or release tag is created. Stop after pushing
and verifying the remote binding.
