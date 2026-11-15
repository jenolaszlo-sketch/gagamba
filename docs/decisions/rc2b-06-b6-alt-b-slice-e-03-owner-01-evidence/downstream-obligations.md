# Downstream responsibility matrix

Verbatim table from accepted E-2 closure assessment §22. Its historical dispositions and closure requirements remain controlling.

[Controlling assessment](../../reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01.md)

| Obligation | Source decision | Current status | Proposed owner | Required evidence | Trigger / prerequisite | Allocation | Blocks E-2? |
|---|---|---|---|---|---|---|---|
| E-3 v3-pre codec | Slice E entry owner §9 | Planning only | E-3 | Authoritative frozen v3-pre layouts; inherited prerequisites; own owner decision | After E-2 closure and separate E-3 entry assessment | Explicit milestone purpose | No |
| E-4 GAVA1 direct-context | Slice E entry owner §9; D1 §13 item 11 | Planning only | E-4 | Frozen header/wire layout; inherited prerequisites; fresh P4 re-audit | E-4 entry | Explicit | No |
| E-5 fixed launcher/custody | Slice E entry owner §9 | Planning only | E-5 | Accepted launcher/driver interface; provenance/approval prerequisites | E-5 entry | Explicit; no native qualification implied | No |
| E-6 atomic enablement and R/O/C ordering | Slice E entry owner §9; plan §3.5 | Planning only | E-6 | Complete plan §3.5 dependencies; ordering correction; fresh P4 audit; separate owner decision | E-6 entry | Explicit | No |
| E-7 driver integration | Slice E entry owner §9 | Planning only | E-7 | Exact dispatch/closure; separate authorization | E-7 entry | Explicit; does not waive atomic dependencies | No |
| Slice N protected native qualification | Slice E entry owner §9; plan §3.6 | `NATIVE-PENDING` | Slice N | Disposable host; independent cases; exact candidate/host/kernel/runtime bindings | Separate authorization | Explicit | No |
| Protected K/M/F/S acquisition/readers | P1 acceptance §11.6; P1-Q1-F06 | Not implemented | Atomic set **E-4/E-5/E-6 + Slice N**; E-6 owns complete §3.5 dependencies | Implemented readers/verifiers; negative controls; independent qualification; native evidence | Atomic enablement | **Explicit allocation** | No |
| Exact case policy | Plan §3.5; Q-E3; E-1 entry owner §9 | Not implemented | Same atomic set; E-6 owns complete dependencies | Accepted policy implementation and negative controls | Atomic enablement | **Explicit allocation** | No |
| Protected-root provisioning/custody | D1 §13 item 3; plan §3.5 | Unprovisioned | E-5/E-6 + Slice N | Exact roots, permissions, no-follow acquisition, custody evidence | Any protected path | Explicit via atomic set | No |
| Operational V/W/A execution and R/Wc/P issuance | D1 §§5–12; P2 acceptance §26, §27.1 | Not implemented | D1-governed administration + E-5/E-6/E-7; no single numbered milestone | Per-instance administrative records; issuance mechanism; protected path | Any real catalog approval | Partly explicit (deferred), partly inferred owner split | No |
| Publication and withdrawal ordering | D1 §11; plan §3.5 | Governance only | E-6 (with E-5) | R/Wc-before-P enforcement; fsync/no-replace; withdrawal serialization | Atomic enablement | Inferred within atomic set | No |
| Live invocation authorization and fresh challenge/response | D1 §10, §17(5); plan §3.5 | Not implemented | E-6 (with E-4/E-5) | One-response latch; GAVA1 context; invocation authorization | Atomic enablement | Explicit deferred; detailed split inferred | No |
| First positive validation | Plan §3.5; P2 acceptance §27.1 | Not implemented, unauthorized | E-6 atomic enablement | All atomic dependencies; separate owner decision | E-6 completion | Explicit | No |
| First protected execution | Plan §3.5; P4 coverage acceptance §7 | Not implemented, unauthorized | E-6/E-7 + Slice N | Atomic enablement; native qualification; P4 evidence | E-6/Slice N | Explicit | No |
| Composite Q-E1 — GAVA1 header/wire | P0 acceptance §12; D1 §18 | Open | E-4 | Accepted GAVA1 header and wire layout | E-4 entry | Explicit E-3/E-4 reconciliation | No |
| Composite Q-E1 — v3-pre layouts | P0 acceptance §12; D1 §18 | Open | E-3 | Accepted layouts, status values, echo fields, bootstrap bounds | E-3 entry | Explicit E-3/E-4 reconciliation | No |
| Composite Q-E1 — release correlation | P0 acceptance §12; D1 §18 | Open | E-3/E-4 | Accepted correlation formula | E-3/E-4 entry | Explicit E-3/E-4 reconciliation | No |
| E-4 P4 re-audit | D1 §13 item 11 | Deferred | E-4 | Fresh sink/authority audit | E-4 entry | Explicit | No |
| E-6 P4 re-audit | D1 §13 item 11; P4 coverage acceptance §7 | Deferred | E-6 | Fresh P4 audit covering atomic path | E-6 entry | Explicit | No |
| First E-1 production consumer | P0 acceptance §14; P2 acceptance §18 | Not authorized | **Unassigned numbered milestone**; separate consumer decision | F-02 gate: parity, branch dominance, verification-to-use, fail-closed, authority separation, Option A reassessment | A future decision to create the first consumer | **Unassigned — future owner decision required** | No; E-2 excludes any E-1 consumer |
| P0 external-package durability | P0-Q1 | Outstanding obligation | Unassigned; owner/repository preservation | Durable archive or independent source-history verification | Loss risk or future provenance need | **Unassigned** | No |
| Mechanism A durable permits | D1 §13 item 7 | Unselected | Separate owner decision if ever wanted | Durable-grant contract and protected permit | Any durable-grant requirement | Explicitly deferred, no milestone | No |
| Operational refusal encodings | D1 §13 item 4 | Structural encodings delivered by P1/P2; operational encodings absent | E-5/E-6 implementation | Invariant-to-refusal mapping | Protected path implementation | Inferred from atomic set | No |
