# Downstream obligations and allocation

Full 23-row matrix: report §22. This file records the allocation status and the
items that are not assigned to a numbered milestone.

## Accepted milestone purposes

| Milestone | Purpose | Allocation |
|---|---|---|
| E-3 | v3-pre codec | Explicit (Slice E entry owner §9) |
| E-4 | GAVA1 direct-context; fresh P4 re-audit | Explicit |
| E-5 | Fixed launcher/custody interface | Explicit; no native qualification implied |
| E-6 | Atomic enablement, R/O/C ordering, complete plan §3.5 dependencies; fresh P4 audit | Explicit |
| E-7 | Driver integration | Explicit; does not waive atomic dependencies |
| Slice N | Protected native qualification | Explicit; separate host/candidate/evidence binding |

## Atomic-set obligations

| Obligation | Allocation | Status |
|---|---|---|
| Protected K/M/F/S acquisition/readers | E-4/E-5/E-6 + Slice N; E-6 owns complete set | **Explicit** (P1 acceptance §11.6; plan §3.5) |
| Exact case policy | Same atomic set; E-6 ownership | **Explicit** (plan §3.5; Q-E3; E-1 entry owner §9) |
| Protected-root provisioning/custody | E-5/E-6 + Slice N | Explicit via atomic set |
| Publication/withdrawal ordering | E-6 (with E-5) | Inferred within atomic set |
| Live invocation authorization/challenge | E-6 (with E-4/E-5) | Explicit deferral; detailed split inferred |
| First positive validation | E-6 | Explicit |
| First protected execution | E-6/E-7 + Slice N | Explicit |
| Operational V/W/A execution and R/Wc/P issuance | D1-governed administration + E-5/E-6/E-7 | Partly explicit; owner split inferred |
| Operational refusal encodings | E-5/E-6 | Inferred from atomic set |

None of these blocks E-2 closure. No item is converted into approved scope by
this table.

## Composite Q-E1

| Item | Allocation | Status |
|---|---|---|
| GAVA1 header/wire | E-4 | Open |
| v3-pre layouts/status/echo/bootstrap | E-3 | Open |
| Release correlation | E-3/E-4 | Open |

Source: P0 acceptance §12; D1 §18. E-2 evidence proves only the E-2 subset and
must not be cited as closing composite Q-E1.

## Unassigned obligations (flagged, not invented)

| Obligation | Why unassigned | E-2 effect | Recommendation |
|---|---|---|---|
| First E-1 production consumer | No numbered milestone in accepted decisions; triggered by a future consumer decision and the F-02 gate | E-2 excludes any E-1 consumer | Future owner decision should name the gate |
| P0 external-package durability | No milestone owns preservation/independent source-history verification | Non-blocking provenance limitation | Future owner decision or explicit indefinite retention |
| Mechanism A durable permits | Only if durable grants are ever wanted | Out of E-2 scope | Separate owner decision if proposed |

The first two are governance gaps, not E-2 blockers. They are recorded so they
are not silently treated as closed by E-2 closure.
