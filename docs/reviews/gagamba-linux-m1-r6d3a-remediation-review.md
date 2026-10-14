# Gagamba Linux M1 R6D.3a remediation review (preserved)

- review type: independent R6D.3a remediation review (before WP04)
- implementation under review: 40e8adda46a44e29465c739d3498e7387be4ce8a
- evidence-only commit at review time: d7f904218216df827fa461f548c3e810e4d8839b
- branch: qualification/ar7-feasibility
- disposition: R6D.3a REMEDIATION ACCEPTED - FULL D1-D6 REQUALIFICATION REQUIRED; G2-03 REMAINS BLOCKING
- scope: D1 handshake correction, pre-kill liveness hardening, D2 expectation correction; no D1-D6 qualification performed here
- G2-03 remains blocking pending full fresh D1-D6 qualification; G2-10 blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED
- The body below is the report verbatim.

# Independent R6D.3a Remediation Review

Review-only; repository untouched (HEAD `d7f9042`, tree clean; all builds/probes in `/tmp` from a blob-verified `git archive 40e8add` export).

## Target binding

- Branch `qualification/ar7-feasibility`; impl `40e8add` (5 files: 1 production + 4 test), evidence `d7f9042` (docs-only); `git diff 40e8add d7f9042 -- src/ tests/` empty.

## Production diff

Exactly one production change: `OwnerPreparationTransaction.cs` — the `after-intent` barrier moved from before `TryReceiveTransfer(StartAuthority)` to after StartAuthority receipt + parent/staging identity validation, before context creation. No other production semantic change. This is precisely the prescribed minimum correction.

## D1 original counterexample reproduction

Confirmed pre-fix mechanism from the blocked run plus this review's traces: scope sends `StartAuthority` while owner waits `Continue`; owner consumes it as the barrier reply, fails type validation, exits 25, writer disposed, fence freed — Kill then acts on an already-dead owner (verified `Kill` does not throw on dead processes, explaining the vacuous passes).

## D1 handshake correction

Ordering now: intent durable → writer live → owner sends `WriterReady` → scope sends `StartAuthority` → owner receives + validates → owner reaches after-intent → sends `BarrierReached` → waits explicit `Continue` → context/domain only after `Continue`. Strict alternation holds at every step; no crossed sends; sequencing monotonic. Narrow and sufficient.

## D1 semantic-state verification

At corrected after-intent: intent durable, writer live, StartAuthority validated, no context/domain/staging/staged/Prepared/Mint/capability. Still durably-intent-live-owner-before-allocation. StartAuthority receipt grants no allocation by itself (fds validated then held; creation requires context + domain call, both later).

## D1 live-owner proof

Decisive independent hold: barrier reached via the real scope driver, then 10×500 ms samples over 5 s — owner alive, pidfd-not-exited, writer-excluded every sample — then kill (137), exit proof, takeover from seq 1, scope alive. Genuinely blocked, not racing.

## Writer-fence proof

Exclusion while live plus takeover-after-death in repo tests (7/7 D-rows) and probes (root + gaga); no overlap; G2-05 intact.

## Pre-kill liveness hardening

All kill sites now assert barrier match, non-null non-self owner, pidfd-not-exited, refreshed `HasExited` false, and writer exclusion *before* `Kill`. `Kill` success is no longer the liveness proof.

## D2 product-semantics assessment

`Refused` on the intent-stage snapshot is correct: the reconciler cannot corroborate the live lease against durable registered identity that does not exist yet. Conservative refusal, not hidden success. Production reconciliation unchanged (no diff).

## D2 test correction

Rewritten to assert `Refused` + stability + retained-handle stop/remove with sibling precision. Matches product semantics; weakens nothing.

## Protocol regression

Consume-then-dispatch by decoded type/sequence preserved; no length dispatch, no queue rule; all bounds/checks intact. The reorder adds no new sends.

## Topology/capability regression

Owner still owns writer/domain/staging/accounting/Prepared/store/Mint; scope survives externally with validated sets, reconciles, takes over post-death only; descriptions remain handle-free. G2-06 intact. No work returned to scope.

## Linux-native results

D-rows 7/7; barriers 7/7; full transaction + protocol/fencing/pidfd tests green; full M1 **353 passed, 0 failed, 13 skipped** (366); D1 hold probe 17/17 checks; negative-dispatch probe 26/26 with exact fd neutrality; genuine-absence probe 4/4.

## Non-root results

uid/gid 1000, all caps zero, `gaga-delegated` unprovisioned: `NonRootQualificationTests` 12/12 (incl. both barrier-dispatch tests + rewritten B3); D1 hold probe fully green (real domain, splits, ordering, exclusion, kill 137, takeover).

## Windows/portable results

Solution build 0 warnings/0 errors; M1 suite 257 passed + 109 skipped; vector tests green.

## Accepted-boundary regression sample

Journal, Fencing, DomainAuthority (incl. F1/F2), Reconciler, Protocol, Preparation, Contracts all green across repeated runs. One transient parallel flake observed once each in two unrelated tests across many runs (both pass isolated/repeated; unrelated to this delta). Canonical vector unchanged: 873 bytes, SHA-256 `82161029…805d5f3`. No finding reopened.

## Findings

No blockers. The remediation is narrow, complete, and verified.

## Remediation disposition

**Accepted.** D1 counterexample eliminated (owner demonstrably live at corrected D1, exclusively fenced, pre-allocation preserved); harness rejects dead owners pre-kill; D2 expectation matches correct conservative semantics with production untouched; topology/protocol/capability boundaries intact.

## Remaining global status

- G2-03 blocking pending full fresh D1-D6 qualification (this review explicitly does not carry forward the blocked run; rerun D1→D6 against the corrected implementation);
- G2-10 blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED.

R6D.3a REMEDIATION ACCEPTED - FULL D1-D6 REQUALIFICATION REQUIRED; G2-03 REMAINS BLOCKING
