# WP05 D6.1 Amendment Reconciliation

Status: **PROPOSED - NOT APPROVED**

D6.2 note: the independent GLM D6.1 review found the R1-R4 rows only partially
addressed or carrying a provenance defect. This D6.1 register is historical;
the current design text is in
[the D6.2 precision amendment reconciliation](gagamba-linux-m1-wp05-d6-2-precision-amendment-reconciliation.md)
and the D6.2 sections of C1/C3. Do not cite this register alone as proof that
R1-R4 are resolved.

This register reconciles the D6.1 design text against GLM Q3
(`2a235c39...`, required-changes blob `65392a0f...`). `DESIGN TEXT ADDRESSED`
means the proposed contract text now contains the amendment; it does **not**
mean production remediation, approval or qualification. All C1-C4 remain
unapproved.

| ID | Finding / revised design | Affected D6.0 document / new section | Planned acceptance test | Status |
|---|---|---|---|---|
| A-C1-1 | Ambiguous release performs the journal attempt under journal serialization; after the ambiguous result is returned and `_journalGate` released, the ambiguous result is retained under `_gate`; the bounded stop is invoked as a tail action after `_gate` release, so stop may take `_gate` then `_journalGate` without inversion. Opposing-order concurrent probe required. | C1 record §9; main §4.4 | `AmbiguousReleaseStopLockOrderProbe`: release journal/committed path vs ambiguous-path stop, barriers at lock transitions, detect inversion/deadlock/second FD4. | DESIGN TEXT ADDRESSED |
| A-C1-2 | Select `ReleaseDecisionKind.AmbiguousPersistence`; `MayHaveExecuted`, `NotAttempted`, `DurableCommitted=false`, no FD4, retained. Inventory all enum consumers and require exhaustive handling. | C1 §9; C4 §8 | Consumer inventory/compile + retained ambiguous → Duplicate; all switches reviewed. | DESIGN TEXT ADDRESSED |
| A-C1-3 | C1 relies on C2 capability consumption at runtime stop acceptance. Document transient live-while-wedged window, no FD4 due sealed journal, stop-failure/death behavior, no resend. C1 declares C2 prerequisite. | C1 §9; C2 §9 | Wedge probe plus owner-death recovery; capability consumed; no second intent/FD4. | DESIGN TEXT ADDRESSED |
| A-C2-1 | Consume inside `_gate` on first stop acceptance for normal, clock-unavailable and overflow paths; duplicate stop does not re-consume; exactly-once. | C2 §9 | Normal/clock/overflow `IsLive == false`; failed durable stop; duplicate; concurrent release. | DESIGN TEXT ADDRESSED |
| A-C2-2 | Release-first: failed `TryConsume=Consumed` pairs with already-consumed capability; stop accepted; release history preserved; `ReleaseAlreadyCommitted` includes consume-before-transport. | C2 §9; C4 §8 | Release wins before/after consume/transport/commit; flag/reason/result checks. | DESIGN TEXT ADDRESSED |
| A-C2-3 | CE-03 exact declaration: multi-instance window outside accepted single-arbiter model, closed by capability contract, not composition. | C2 §9 | Two-arbiter shared capability refused after failed durable stop. | DESIGN TEXT ADDRESSED |
| A-C3-1 | Require QPC-based corroborated epoch; `OwnerDomainClock` is incompatible, `LaunchDomainClock` compatible; replace/constrain clock and reject incompatible epoch; no fabricated window. | C3 §9 | Compatible/incompatible epoch, same/cross-boot, unavailable/overflow, old Stopwatch clock fails. | DESIGN TEXT ADDRESSED |
| A-C3-2 | Distinguish attempted/unconfirmed stop (Unknown/quarantine) from operation failure (Failed), population unknown, empty, persistence failure, identity refusal; specify Reconciler aggregation amendment. | C3 §9 | Kill failure, delivered-but-populated, population unknown/empty, persistence, identity, cleanup debt. | DESIGN TEXT ADDRESSED |
| A-C3-3 | No StopRecord after restart: cleanup-only caller allowance, not a fresh/continued durable deadline; C2 prevents release authority return; no resend. | C3 §9 | `RuntimeAcceptedStopWithoutDurableRecordAfterRestart`. | DESIGN TEXT ADDRESSED |
| A-C3-4 | Adopt Option B: retain kill-first behavior, rewrite acceptance row to “no unnecessary WAIT”; record that kill may be issued before population observation. | C3 §9 | Already-empty domain records kill/no unnecessary wait; identity checked. | DESIGN TEXT ADDRESSED |
| A-C3 policy | Expired deadline: recommend Policy A (kill + one observation), but human choice remains open. Not selected/approved. | C3 §9; approval sheet | `ExpiredDeadlineUrgentStopPolicy` pending human choice. | OPEN - HUMAN POLICY |
| A-C4-1 | Bind exact R12 reason strings per class; distinguish semantic outside/gate taxonomy from lock placement; keep `Refused` + reason/metadata; use C1 ambiguous kind. | C4 §8 | Refusal taxonomy tests per reason class, concurrent/sequential/retained. | DESIGN TEXT ADDRESSED |
| A-PKG-1 | Cite original branch `review/wp05-release-plan-adversarial-review`, commit `e4b0a798...`, blob `73eb09af...`; not ancestral. | Main §1; C1/C2/C3/C4 | Documentation binding inspection. | DESIGN TEXT ADDRESSED |
| A-PKG-2 | State `ReleaseArbiter`/`Reconciler` have zero production call sites; changes affect component code but no integrated owner runtime; WP08/10 deferred. | Main §9; remediation §1 | Source-scope inspection; component-only claims. | DESIGN TEXT ADDRESSED |
| A-PKG-3 | Add composed case: ambiguous wedge then clock-unavailable stop; no FD4, runtime stop, consumed capability, no fabricated StopRecord, retained ambiguity, recovery/no resend. | C1 §9; evidence matrix | `AmbiguousWedgeThenClockUnavailableStop`. | DESIGN TEXT ADDRESSED |
| A-PKG-4 | Already satisfied: matrix declares no gate accepted/diagnostics only, matching C8/H1-H5. | Evidence matrix | Inspection only. | SATISFIED - NO CHANGE |

## Nonblocking Q3 R2 clarifications

- C3 zero-wait policy: documented as open human choice; Policy A is the
  recommended technical option but is not selected.
- C4 optional `ConcurrentRefusal` kind: not introduced; keep `Refused` plus
  discoverable reason/metadata.
- C3 duration-vs-deadline overload: optional; epoch corroboration is the
  substantive requirement.
- C1 stop authority: runtime boundary + runtime deadline + best-effort durable
  stop; no unpersistable StopRecord claim.

## Status discipline

No amendment is `PRODUCTION REMEDIATED`, `APPROVED` or `RESOLVED`. D6.1 is
design text only. C3 remains `CONTRACT CHOICE REQUIRED - HUMAN DECISION
PENDING`; C1, C2 and C4 remain subject to individual human approval.
