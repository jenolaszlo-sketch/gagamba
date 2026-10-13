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
| WP02 — Protected copy-in and atomic preparation publication | [wp02](linux-m1/wp02.md) | Implemented and remediated for G2-06/G2-07 (`a1b8e3a2da44d781b996ccc5cbe604a863611368`); pending G2 rereview |
| WP03 — Delegated domain lease, persistent allocation and external recovery scope | [wp03](linux-m1/wp03.md) | Implemented/remediated for G2-01…G2-05, G2-08…G2-10 (R2 `8a11e145e5c3fe06258a074acc13f0709630214a`, R3 `5c5a0b2243416e1602299360cda0115ac0cfc254`, R4 `0f91cc9c5433ce592fccfd45019c5b7171737e9c`, R5 `121c644f2aa774119149f598a935a181fc71e137`); unprivileged delegated qualification passed; pending G2 rereview |
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

Production gaps from the plan (inode/object ceilings, complete P05/P06, owner-death deployment, complete no-IP policy, P08 runtime, P09 packaging, core sinks, host breadth, Windows/macOS/VM) remain pending and are restated in each package record.
