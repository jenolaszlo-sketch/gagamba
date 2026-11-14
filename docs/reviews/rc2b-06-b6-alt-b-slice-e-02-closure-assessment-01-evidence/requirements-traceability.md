# Requirements traceability summary

Full matrix: report §20. This file records the counts, the four accepted
deferrals, and the evidence-quality distribution so a closure owner can audit
without reconstructing the package history.

## Status counts

| Status | Count |
|---|---|
| SATISFIED | 7 |
| SATISFIED WITH QUALIFICATIONS | 16 |
| DEFERRED BY ACCEPTED DECISION | 4 |
| NOT SATISFIED | 0 |
| INCONCLUSIVE | 0 |
| **Total** | **27** |

No `DEFERRED` status is used without an accepted allocation. No requirement is
missing evidence or contradicted by evidence.

## The four accepted deferrals

| ID | Requirement | Accepted allocation | Source |
|---|---|---|---|
| E2-R24 | Protected K/M/F/S readers/acquisition | Atomic set E-4/E-5/E-6 + Slice N; E-6 owns complete set | P1 acceptance §11.6; P1-Q1-F06; plan §3.5 |
| E2-R25 | Exact case policy | Same atomic set; E-6 owns complete dependencies | Plan §3.5; Q-E3; E-1 entry owner §9 |
| E2-R26 | Native/protected execution qualification | Slice N on separately authorized host | P1-Q1-F06; P2 acceptance §23; Slice E entry owner §9 |
| E2-R27 | Composite Q-E1 (GAVA1, v3-pre, release correlation) | E-3/E-4 reconciliation | P0 acceptance §12; D1 §18 |

These four are not E-2 closure conditions under the accepted boundary. The
formal closure decision must adopt that boundary explicitly (report §26.3).

## Seven unqualified satisfactions

| ID | Requirement | Basis |
|---|---|---|
| E2-R03 | P0 owner acceptance binds the exact record | P0 acceptance §5, §11 |
| E2-R07 | No delegation/standing/cross-host reuse | D1 §12, §13 item 6 |
| E2-R08 | Approval vs operational authority; F02 separate | D1 §§9–10 |
| E2-R09 | D1 adoption binds E-2 scope | D1 §§16–17 |
| E2-R13 | Deterministic refusal of malformed/noncanonical content | P1 Q1 vectors and mutations; acceptance §§10–11 |
| E2-R14 | P1 non-authoritative result boundary | P1 acceptance §12 |
| E2-R22 | P2 non-authoritative result boundary | P2 acceptance §§13–14 |

## Evidence-quality distribution

| Quality | Rows |
|---|---|
| Direct + independently qualified + owner-accepted | 19 |
| Direct + owner-accepted (governance/decision rows) | 4 |
| Direct + owner-accepted allocation/exclusion (deferred rows) | 4 |
| Limited (retained qualification attached) | 16 |

"Limited" overlaps the first category: 16 rows carry at least one retained
qualification. No row is `Missing` or `Contradictory`.

## Package-to-requirement map

| Package | Requirement IDs |
|---|---|
| P0 | E2-R01–R04 |
| D1 | E2-R05–R09 |
| P1 | E2-R10–R16 |
| P2 | E2-R17–R23 |
| Deferred | E2-R24–R27 |

The matrix preserves the original E-2 success criteria reconstructed in report
§5 (SC-1–SC-8) and does not derive them from the P1/P2 reports.
