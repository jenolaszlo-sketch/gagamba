# RC2B-02 B2 owner acceptance and next implementation handoff

Commission: GAGAMBA-RC2B-02-B2-OWNER-01.
Date recorded: 2026-10-29 (Asia/Manila). Documentation-only owner decision.

## 1. Owner disposition

**RC2B-02 B2 ACCEPTED WITH QUALIFICATIONS.**

The owner accepts the independently reviewed recovery bootstrap and process-lifetime
infrastructure within its demonstrated scope. The independent verdict is
**SUPPORTED WITH QUALIFICATIONS**. No blocking or major findings were reported for
the B2 scope. No further B2 implementation remediation is required for owner
acceptance of that scope. M-01 is accepted as a nonblocking follow-up and remains OPEN.

## 2. Verified repository binding and ancestry

| Identity | Verified value |
|---|---|
| Repository / checkout | `jenolaszlo-sketch/gagamba`, canonical checkout directly on `main` |
| Starting HEAD and remote `main` | `0602f32f94593e7eebe6e02eccbb1e71b1b7ae60` |
| B2 baseline | `e9e9791efa59474a2bb2da8d8087df5de1b0052f` |
| Initial implementation | `b85648f6931128ae3e89108eee267d8cec9e73e2` |
| Final tested implementation | `7396012bc07c357a49bbdd0c3791ce06808ca9ab` |
| Implementation root tree | `e009fb88d6cd736660c2055b4ed7743d59976b78` |
| Evidence delivery | `235b15b3d2cee828176c18b02685f427f3423ff0` |
| Independent review | `0602f32f94593e7eebe6e02eccbb1e71b1b7ae60` |
| Review root tree | `7984e0d4ce25dbabbedf9669a3a30821179bb5df` |
| Review document | [Independent B2 review](../reviews/rc2b-02-b2-independent-review.md) |
| Producer evidence | [B2 evidence and handoff](../evidence/rc2b-02-b2.md) |
| Delivery CI | [Run 40873410775](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/40873410775) |

Verified before writing: `e9e9791` -> `b85648f` -> `7396012` -> `235b15b`
-> `0602f32` is a linear chain, with each successor's sole parent equal to its
predecessor. Starting local and remote `main` agree. This decision is a documentation
child of the review commit; its final commit/tree and remote verification are returned
in the completion handoff, avoiding a self-referential commit identity.

## 3. Accepted B2 scope

- Dedicated unprivileged .NET 10 recovery executable.
- Independent recovery-process lifetime in the tested WSL2 topology.
- Verified subreaper initialization.
- Controller and owner pidfd retention.
- Authenticated private controller association.
- One-use session credentials and bounded message sequencing.
- Controller-loss detection.
- Owner-loss and recovery-loss lifeline observations.
- First-cause and original stop-deadline retention.
- Nonblocking safety-stop submission.
- No release-authority reconstruction.
- Retained-handle kill and identity-checked removal for empty native domains.
- Corrected EINTR/EAGAIN handling.
- Passed deterministic tests and documented process evidence.

The deployment assumption that an external supervisor does not kill recovery together
with the controller remains qualified to the tested topology. Installed closure and
supervisor-policy attestation remain B6 work.

## 4. Evidence accepted with provenance qualifications

The reviewer independently executed **699 passing Windows tests**: 616 focused M1,
68 Execution and 15 Runtime tests, with zero failures and zero skips in those runs.
These are the reviewer's recorded executions; this decision commission does not rerun them.

The reviewer inspected the producer's WSL2 process and native-domain evidence but did
not independently reproduce those Linux process cases. The producer reported **24
passing real-process cases, including the two empty-domain cases**. The two native
cases are included in that inventory and are not two additional distinct tests.
The producer also reported 70 passing focused WSL2 M1 tests; these overlap Windows
coverage and must not be summed as distinct tests across platforms.

Delivery CI completed successfully: **10 passing jobs and one conditional skipped
job**, verified against the linked run. CI does not establish independent native
Linux-host qualification or expand the native-domain qualification boundary.

## 5. Mandatory carried-forward restrictions

Acceptance does not establish:

1. Native Linux-host qualification outside WSL2.
2. Populated restricted-domain stopping.
3. Native stop enforcement when recovery dies.
4. Complete R1-F02 runtime enforcement.
5. B3 live owner preparation/release composition.
6. Implementation of R1-F03 Option A in release admission.
7. B4 protected capture and complete independent stop enforcement.
8. B5 sealed C3 deadline/reconciliation behavior.
9. B6 deployment and artifact attestation.
10. B7 full lifecycle and failure qualification.
11. Same-UID hostile-process isolation.
12. Hufu D4a/D4b eligibility.
13. Production restricted-execution activation.

The two-second B2 volatile stop-obligation deadline is not a sealed C3 durable
StopRecord. An empty-domain kill/readback is not proof of populated-domain stopping
or complete cleanup; target-execution exclusion and durable reconciliation remain
unproven. The B1 public facade remains safely disabled for live restricted execution.
No production security claim is expanded by this decision. Existing accepted B1,
RC2A and C1-C4 decisions, independent reports and evidence remain unchanged.

## 6. M-01 disposition and required follow-up

**M-01: ACCEPTED FOLLOW-UP, NONBLOCKING FOR B2. Status: OPEN.**

The independent reviewer found that `RecoveryConnection.Inspect()` and
`ObserveAsync()` lack disposed-state guards equivalent to the guard already added
to `Send()`. The required future correction must:

- Prevent `Inspect()` from operating on a disposed connection.
- Prevent `ObserveAsync()` from operating on a disposed connection.
- Verify concurrent disposal behavior.
- Add focused regression tests.
- Preserve existing cancellation and loss-observation semantics.

**M-01 must be resolved before B3 operationally consumes these connection methods.**
This commission changes no production code, supplies no implementation evidence
for that correction and does not close the finding.

## 7. Next milestone: RC2B-03 B3

**RC2B-03 B3: NEXT PROPOSED IMPLEMENTATION, NOT STARTED.**

The intended objective is to connect the accepted B1 facade and B2 recovery topology
to real owner preparation and one-shot release composition. A future B3 commission
must explicitly preserve:

- Original preparation identity and generation.
- Actual establishment evidence.
- Provider-origin preparation results.
- No target initialization before release.
- C1 ambiguous persistence behavior.
- C2 irreversible stop consumption.
- C3 original deadline authority and existing implementation limitations.
- C4 duplicate and concurrent-call semantics.
- Approved R1-F03 Option A.
- No repeated physical RELEASE.
- Recovery process never receiving release authority.
- Safe behavior under controller/owner loss.

B3 may begin under a separate bounded implementation commission with local WSL2
development. Any native qualification claim must identify the actual host and
capabilities. A suitable independent native Linux host must be selected before
claiming native Linux-host qualification. No host or deployment identity is selected
or invented here. B4/B5/B6/B7 and Hufu entry gates remain separate requirements.

## 8. Delivery and validation boundary

The authorized changes are this decision, [PROJECT-STATUS](../PROJECT-STATUS.md),
and the stale current pointers in [handoff](../handoff.md) and
[work queue](../work-queue.md). Accepted decisions, independent reports and producer
evidence are preserved. Source, tests, native code, CI, package definitions and Hufu
are outside this documentation-only commission.

Run `eng/verify-preparation.ps1` for documentation preparation and local-link
verification, and `git diff --check`; verify the final diff contains only the four
authorized documentation paths and the protected source/test trees are unchanged.
The completion handoff records the actual results and final Git binding.

Work remains directly on `main`, preserving historical untracked files. No branch,
worktree, reset, rebase, force push, package publication, release tag, Hufu change
or B3 implementation is part of this decision. Stop after committing and pushing
the documentation decision and verifying its remote identity.
