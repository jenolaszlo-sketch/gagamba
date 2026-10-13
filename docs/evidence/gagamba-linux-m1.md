# Gagamba Linux M1 evidence index

Date: 2026-10-13 (Asia/Manila). Slice: internal Linux prepared-execution transaction (Milestone M1).

This index tracks per-package evidence for the M1 milestone under
[ADR 0003](../adr/0003-ar7-linux-first-restricted-execution.md) and the
[Linux M1 implementation plan](../plans/gagamba-linux-m1-implementation-plan.md).
Each package records its exact commit, commands, result summary and
limitations in its own file. No package result qualifies production
`restricted-offline-closure-v1`; the production profile continues refusing
until all mandatory gates pass.

| Package | Evidence | Status |
|---|---|---|
| WP01 — Internal contracts, capability identity and state model | [wp01](linux-m1/wp01.md) | Complete, G1 ACCEPTED ([acceptance](../reviews/gagamba-linux-m1-g1-acceptance.md); M1 70/70, probes 13/13, native NotApplicable) |
| WP02 — Protected copy-in and atomic preparation publication | [wp02](linux-m1/wp02.md) | Implemented and remediated for G2-06/G2-07 (`a1b8e3a2da44d781b996ccc5cbe604a863611368`); G2-06 R6A `e5f90ce4c3c6a239775b4af72e26e077b292e6d5` independently resolved, G2-07 resolved |
| WP03 — Delegated domain lease, persistent allocation and external recovery scope | [wp03](linux-m1/wp03.md) | Implemented/remediated for G2-01…G2-05, G2-08…G2-10 (R2 `8a11e145e5c3fe06258a074acc13f0709630214a`, R3 `5c5a0b2243416e1602299360cda0115ac0cfc254`, R4 `0f91cc9c5433ce592fccfd45019c5b7171737e9c`, R5 `121c644f2aa774119149f598a935a181fc71e137`); G2-02 R6B `b80e9c28b462e2aa392313ff5dc58063478b955c` independently resolved (R6B.1 `447432c07558782ac8ba7dcc172d75af2ed92b9d` F1/F2 hygiene corrected); unprivileged delegated qualification passed; G2-01/02/06/07/09 resolved |
| WP04–WP12 | — | Not started; WP04 blocked pending independent G2 rereview |

The original independent G2 review is preserved verbatim at
[gagamba-linux-m1-g2-review.md](../reviews/gagamba-linux-m1-g2-review.md):
disposition `G2 NOT ACCEPTED - CORRECTION REQUIRED` at
`1e117e0a64ee473f2ba251818761afc315c8e283`, blocking G2-01…G2-10 and
nonblocking G2-11. That review is unchanged.

The independent G2 rereview of implementation
`121c644f2aa774119149f598a935a181fc71e137` is preserved verbatim at
[gagamba-linux-m1-g2-rereview.md](../reviews/gagamba-linux-m1-g2-rereview.md):
disposition `G2 NOT ACCEPTED - CORRECTION REQUIRED`. Resolved: G2-01, G2-07,
G2-09. Still blocking: G2-02, G2-03, G2-04, G2-05, G2-06, G2-08, G2-10.
Evidence maintenance: G2-11. This rereview supersedes prior local claims
that all blockers were remediated. G2 remains NOT ACCEPTED and WP04 remains
blocked; no correction has begun. Author runs and the
original reviewer runs are distinguished per package; root-native and
unprivileged-delegated evidence are classified separately in WP03 Section 18.

The independent G2-06 R6A rereview of implementation
`e5f90ce4c3c6a239775b4af72e26e077b292e6d5` (docs-only HEAD `6d6296a`) is
preserved verbatim at
[gagamba-linux-m1-g2-06-r6a-rereview.md](../reviews/gagamba-linux-m1-g2-06-r6a-rereview.md):
disposition `G2-06 RESOLVED - R6A ACCEPTED`. G2-06 is independently resolved.
The independently resolved set is now G2-01, G2-06, G2-07 and G2-09. Still
blocking: G2-02, G2-03, G2-04, G2-05, G2-08 and G2-10. Evidence maintenance:
G2-11. G2 overall remains NOT ACCEPTED and WP04 remains blocked; no correction
has begun.

The independent G2-02 R6B rereview of implementation
`b80e9c28b462e2aa392313ff5dc58063478b955c` (docs-only HEAD `d8edd3b`) is
preserved verbatim at
[gagamba-linux-m1-g2-02-r6b-rereview.md](../reviews/gagamba-linux-m1-g2-02-r6b-rereview.md):
disposition `G2-02 RESOLVED - R6B ACCEPTED`. The exact independent
`51 bytes + 0x55` counterexample with seven real cgroup descriptors refuses
via `MSG_TRUNC` before any `DomainLease` (root and uid-1000/gaga-delegated,
fd-neutral); framing, `MSG_CTRUNC`, descriptor, `CLOEXEC`, extra-record and
endpoint-lifecycle behavior were verified through the production path. Two
nonblocking findings: F1 hygiene debt (zero-length `SCM_RIGHTS` rejection
leaks received descriptors; pre-existing, fail-closed) and F2 test race
(`BootstrapEndpointClosedAfterSuccessfulHandoff` observes `/proc` after owner
exit; product property verified deterministically with a live owner). The
independently resolved set is now G2-01, G2-02, G2-06, G2-07 and G2-09; still
blocking are G2-03, G2-04, G2-05, G2-08 and G2-10; G2-11 remains evidence
maintenance; G2 overall remains NOT ACCEPTED and WP04 remains blocked.

R6B.1 hygiene corrections at
`447432c07558782ac8ba7dcc172d75af2ed92b9d` fix the two nonblocking
findings from the preserved R6B rereview: F1 (the zero-length-record
`SCM_RIGHTS` rejection now parses and closes the already-installed received
descriptors exactly once) and F2 (the live-owner endpoint test now observes
`/proc/<pid>/fd` while the owner is deterministically alive, removing the
post-exit race). Independent regression check completed: R6B.1 ACCEPTED -
F1/F2 CORRECTED (F1 verified fd-neutral single and 25× repeated, root and
uid-1000/gaga-delegated; F2 verified 6/6 as a live-owner observation; exact
`51 + 0x55` counterexample still refuses; reviewer suites from a pristine
export: root-native M1 278 passed + 17 skipped, non-root delegated 16/16).
G2-02 remains independently resolved; this patch is not a
new G2-02 acceptance. G2-03, G2-04, G2-05, G2-08 and G2-10 remain blocking;
G2-11 maintenance; G2 overall NOT ACCEPTED; WP04 blocked.

Production gaps from the plan (inode/object ceilings, complete P05/P06, owner-death deployment, complete no-IP policy, P08 runtime, P09 packaging, core sinks, host breadth, Windows/macOS/VM) remain pending and are restated in each package record.
