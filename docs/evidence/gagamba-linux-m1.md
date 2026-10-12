# Gagamba Linux M1 evidence index

Date: 2026-10-11 (Asia/Manila). Slice: internal Linux prepared-execution transaction (Milestone M1).

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
| WP02 — Protected copy-in and atomic preparation publication | [wp02](linux-m1/wp02.md) | Complete, no separate gate (M1 123/123 Linux with 19 native; fixture built/bound, never executed; G2 after WP02+WP03 per plan) |
| WP03 — Delegated domain lease, persistent allocation and external recovery scope | — | Not started; requires WP02 handoff then G2 chain per plan |
| WP04–WP12 | — | Not started |

Production gaps from the plan (inode/object ceilings, complete P05/P06, owner-death deployment, complete no-IP policy, P08 runtime, P09 packaging, core sinks, host breadth, Windows/macOS/VM) remain pending and are restated in each package record. They are not expanded by WP01.
