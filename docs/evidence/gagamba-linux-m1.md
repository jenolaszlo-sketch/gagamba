# Gagamba Linux M1 evidence index

Date: 2026-10-17 (Asia/Manila). Slice: internal Linux prepared-execution transaction (Milestone M1).

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
| WP03 — Delegated domain lease, persistent allocation and external recovery scope | [wp03](linux-m1/wp03.md) | Latest: G2 ACCEPTED via final whole-G2 acceptance review (non-root D1-D6 plus D5 replacement 7/7 incl. D6, NonRootQualificationTests 12/12). Gate state: G2-01–G2-11 resolved; G2 ACCEPTED; WP04 MAY PROCEED (eligible to start; not implemented or qualified). Details: WP03 section 28. |
| WP04 — Held coordinator and pinned Bubblewrap adapter | [final qualification](linux-m1/wp04-qualification-2026-10-17.md); [first incomplete attempt](linux-m1/wp04-fresh-qualification-2026-10-17.md) | WP04 ACCEPTED - HELD SANDBOX QUALIFICATION COMPLETE. Verbatim archival of the independent review awaits its exact body. WP05 release protocol, WP10 target execution, the wrong-audit-architecture KILL_THREAD versus historical KILL_PROCESS generator decision, and global-root-only cases outside WP04 remain deferred. |
| WP05–WP12 | — | Not started; WP05 release and WP10 target execution remain deferred. |

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

Independent G2-08 R6D rereview of implementation `b52742c` (docs-only HEAD
`6510285`) completed and preserved verbatim as
[reviews/gagamba-linux-m1-g2-08-r6d-rereview.md](../reviews/gagamba-linux-m1-g2-08-r6d-rereview.md):
G2-08 RESOLVED - R6D PARTIAL ACCEPTED; G2-03 REMAINS BLOCKING.
Reviewer observation preserved as an observation, not a blocker:
`ScopedAllocationContext.TryCreate` proves an acknowledged write exists,
not that it is the matching `AllocationIntent`. Independently resolved:
G2-01, G2-02, G2-04, G2-05, G2-06, G2-07, G2-08, G2-09. Still blocking:
G2-03, G2-10. G2-11 remains evidence maintenance. G2 overall remains NOT
ACCEPTED; WP04 remains blocked.

Independent R6D.2 topology review of implementation `b3cbc97` (docs/plan-only
HEAD `f6c9176`) completed and preserved verbatim as
[reviews/gagamba-linux-m1-r6d2-topology-review.md](../reviews/gagamba-linux-m1-r6d2-topology-review.md):
R6D.2 TOPOLOGY NOT ACCEPTED - CORRECTION REQUIRED. Blocker B1: the protocol
driver's wait-free `FIONREAD` reply peek aborts every owner transaction
before the owner's reply arrives, leaving D1–D6 unreachable; bounded
readability wait required at all four peek sites. Test-side bug B2: the
non-root missing-pidfd test passes `"none"` as `TestBarrier`, rejected by
the spec codec allowlist. G2-03 remains blocking; G2-10 remains blocking;
G2-11 maintenance; G2 overall NOT ACCEPTED; WP04 blocked; no correction has
begun.

Production gaps from the plan (inode/object ceilings, complete P05/P06, owner-death deployment, complete no-IP policy, P08 runtime, P09 packaging, core sinks, host breadth, Windows/macOS/VM) remain pending and are restated in each package record.
## R6D.2b topology rereview status (2026-10-14)

The independent rereview of implementation 2b6b9d67ba79b90d102d83150f079c299a601bf9, bound to evidence/docs HEAD 0589dd1716100dd5f6b4a7deda369985cf105b6a, is preserved verbatim at [gagamba-linux-m1-r6d2b-topology-rereview.md](../reviews/gagamba-linux-m1-r6d2b-topology-rereview.md). It accepts the owner-owned topology and protocol readiness for actual D1-D6 qualification only. No D1-D6 matrix was run; G2-03 remains blocking pending that qualification and independent review. Current status: G2-10 blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED.
## R6D.3a remediation review status (2026-10-14)

The independent remediation review of implementation 40e8adda46a44e29465c739d3498e7387be4ce8a, bound to evidence/docs HEAD d7f904218216df827fa461f548c3e810e4d8839b, is preserved verbatim at [gagamba-linux-m1-r6d3a-remediation-review.md](../reviews/gagamba-linux-m1-r6d3a-remediation-review.md). Disposition: R6D.3a REMEDIATION ACCEPTED - FULL D1-D6 REQUALIFICATION REQUIRED; G2-03 REMAINS BLOCKING. Corrected implementation for requalification: 40e8adda46a44e29465c739d3498e7387be4ce8a; blocked pre-remediation D3-D6 results are historical evidence only; full D1-D6 requalification must restart at D1. Current status: G2-10 blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED.
## G2-03 final rereview status (2026-10-15)

The independent G2-03 final rereview of the fresh R6D.3 qualification (implementation 40e8adda46a44e29465c739d3498e7387be4ce8a, evidence HEAD 9fe127a98f682514138f4f5e333e47bcbeea8fe5) is preserved verbatim at [gagamba-linux-m1-g2-03-r6d3-final-rereview.md](../reviews/gagamba-linux-m1-g2-03-r6d3-final-rereview.md). Disposition: G2-03 RESOLVED - R6D.3 OWNER-DEATH QUALIFICATION ACCEPTED. Non-root D6 remains evidence debt associated with G2-10, not G2-03; historical R6B.1 F2 is superseded by current equivalent/stronger coverage; no G2-03 remediation remains. Gate state: G2-01–G2-09 resolved; G2-10 blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED.
## G2-10 qualification status (2026-10-16)

The fresh non-root pidfd-backed G2-10 qualification passed all seven rows (D1-D6 plus the D5 replacement control) under UID/GID 1000 with zero effective, permitted, inheritable, and ambient capabilities. `NonRootQualificationTests` passed 12/12; the full Linux M1 suite passed 354/366 with 12 expected skips; the canonical 873-byte vector passed with the expected SHA-256. Delegation and fixture cleanup were confirmed, WSL remained on the same boot ID, and operator keepalive PID 419 remains alive in `/init.scope` for operator-controlled shutdown. No production or test changes were made. Full bounded evidence is in [WP03 section 28](linux-m1/wp03.md).

Gate state: **G2-10 RESOLVED - UNPRIVILEGED PIDFD QUALIFICATION ACCEPTED** (independent final rereview preserved verbatim at [gagamba-linux-m1-g2-10-final-rereview.md](../reviews/gagamba-linux-m1-g2-10-final-rereview.md)); G2-01–G2-10 resolved; G2-11 maintenance only; G2 remains **NOT ACCEPTED**; WP04 remains **BLOCKED**. Non-root D6 closed the explicit outstanding evidence debt; no G2-10 remediation remains.
## G2-11 reconciliation (2026-10-16)

G2-11 was the nonblocking evidence-maintenance finding from the original G2 review (obsolete aggregate counts, SHA-less evidence headers, overstated durability/fencing/membership/composition claims, completion language needing reconciliation). It is closed by this reconciliation, not by any product change.

Current evidence lineage, all bound to exact SHAs: original G2 rejection (`1e117e0`) → per-finding remediation with independent rereviews (R6A `e5f90ce`, R6B `b80e9c2`, R6B.1 `447432c`, R6C `cbe8739`, R6D `b52742c`, R6D.2 `b3cbc97`, R6D.2a `3589a5a`, R6D.2b `2b6b9d6`, R6D.3a `40e8add`) → accepted owner-owned topology → fresh root D1-D6 qualification → non-root D1-D6+D5-replacement qualification (`40e8add`, evidence `127c8e63`) → this reconciliation. All preserved reviews remain verbatim and untouched; earlier failed/blocked/partial tranche records above stay historical and are not current acceptance evidence.

Qualification totals are reported with environment-sensitive variation stated, not smoothed: the Linux M1 suite totals 366 in all runs (354 passed + 12 skipped in author evidence; 353 passed + 13 skipped in independent reruns — one skip/pass bucket flip of environment-sensitive tests, never a differing total); Windows M1 257 passed + 109 skipped; non-root owner-death matrix 7/7; `NonRootQualificationTests` 12/12; canonical vector 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`. Superseded claims (pre-remediation D3-D6 rows, non-root D6 outstanding, historical F2 as a live regression) are marked historical where they appear and are not current acceptance evidence.

Disposition: **G2-11 RESOLVED - EVIDENCE AND STATUS RECONCILED**. Gate state: G2-01 through G2-11 resolved; G2 remains **NOT ACCEPTED** pending the final whole-G2 independent acceptance review; WP04 remains **BLOCKED**.

## Final whole-G2 acceptance (2026-10-16)

The independent final whole-G2 acceptance review of implementation `40e8add` (docs-only HEAD `5b882c1`) is preserved verbatim at [gagamba-linux-m1-g2-final-acceptance-review.md](../reviews/gagamba-linux-m1-g2-final-acceptance-review.md). Disposition: **G2 ACCEPTED - WHOLE-G2 QUALIFICATION PACKAGE ACCEPTED; WP04 MAY PROCEED**. The review confirmed the G2-11 current-status contradiction corrected with no new blocker; prior G2-01–G2-10 acceptances stand on the unchanged implementation (Linux M1 total 366; Windows M1 257+109; canonical vector 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`). Gate state: G2-01–G2-11 resolved; G2 ACCEPTED; WP04 MAY PROCEED (eligible to start; WP04 itself is not implemented or qualified). This acceptance establishes the qualified Linux M1 prepared-execution/recovery foundation only; it does not establish universal Linux portability, new macOS qualification, WP04 target execution, production restricted-offline-closure-v1, or power-loss durability beyond the documented fault-injection boundary.
